using BOCore;
using FacadeCore;
using CPMCore.Helpers;
using CPMCore.Services.Security;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CPMCore.Services.Security.PermissionTreeV2;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen › Gebruikers en rollen (design-handoff punt 49a/49b/49c, "CRM Instellingen.dc.html"). Drie schermen op de bestaande
// beveiligingstabellen (SecurityRole, SecurityUserRole, SecurityRolePermission, SecurityUserPermissionOverride, UserGuestInvitation):
//   - IndexV2    : één lijst voor interne gebruikers, aannemers en gasten (49a)
//   - Rechten    : één gebruiker — account links, effectieve rechten (rol + afwijkingen) rechts (49b)
//   - Rollen     : rollen met bedrijfstoegang, leden en rechtenboom, wijzigingen pas na Opslaan (49c)
// Een recht is één niveau per functie (Geen · Lezen · Bewerken · Verwijderen, zie PermissionTreeV2); de database bewaart drie vlaggen.
// Een gebruiker heeft één rol; een afwijking is een rij in SecurityUserPermissionOverride en verdwijnt zodra ze gelijk is aan de rol.
public partial class UserAdminController
{
    private const string AdminRoleName = "Admin";

    /// <summary>Kruimelpad voor de gl-v2-schermen: Dashboard › Instellingen › Gebruikers en rollen [› leaf]. Enkel ViewData["BreadcrumbNode"] wordt getekend (zie .claude/skills/groupln).</summary>
    private void SetCrumb(string? leaf = null, string? leafAction = null, object? leafRoute = null)
    {
        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var gebruikers = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "UserAdmin", "Gebruikers en rollen") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? gebruikers
            : new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(leafAction ?? "Index", "UserAdmin", leaf) { Parent = gebruikers, RouteValues = leafRoute };
    }

    /// <summary>De gl-v2-schermen tekenen zichzelf in JS uit één JSON-blok: het model van de view is die JSON als tekst.</summary>
    private ViewResult ViewJson(string view, object data) =>
        View(view, System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));

    private static bool IsAdminRole(string? name) =>
        string.Equals(name, AdminRoleName, StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Administrator", StringComparison.OrdinalIgnoreCase);

    // ── Gedeelde berekeningen ─────────────────────────────────────────────────────────────────────

    /// <summary>De verborgen codes (Customers.All, Suppliers.ByBillingCompany, …) staan niet in het oude rechtenpaneel; ze moeten wel als permissie bestaan
    /// vóór er een rol- of afwijkingsrij naar verwijst.</summary>
    private async Task EnsureHiddenCodesAsync()
    {
        var bestaand = (await _db.SecurityPermission.AsNoTracking().Where(x => HiddenCodes.Contains(x.Code)).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nieuw = false;
        foreach (var d in PermissionCatalog.All.Where(d => HiddenCodes.Contains(d.Code, StringComparer.OrdinalIgnoreCase) && !bestaand.Contains(d.Code)))
        {
            _db.SecurityPermission.Add(new SecurityPermission { Code = d.Code, Name = d.Name, ParentCode = d.ParentCode, SortOrder = d.SortOrder, IsActive = true });
            nieuw = true;
        }
        if (nieuw) await _db.SaveChangesAsync();
    }

    private sealed record IssuerRef(int Id, string Name);

    private async Task<List<IssuerRef>> ActiveIssuersAsync() =>
        await _db.IssuerCompany.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.Name).Select(i => new IssuerRef(i.Id, i.Name)).ToListAsync();

    /// <summary>De rechtstreekse rijen van een rol als kaart code → vlaggen. De rol Admin heeft altijd alles (zoals PermissionService).</summary>
    private static Dictionary<string, Flags> RoleMap(IEnumerable<SecurityRolePermission> rows, bool admin)
    {
        var map = NewMap();
        if (admin)
        {
            foreach (var d in PermissionCatalog.All) map[d.Code] = new Flags(true, true, true);
            return map;
        }
        foreach (var g in rows.GroupBy(r => r.PermissionCode, StringComparer.OrdinalIgnoreCase))
            map[g.Key] = new Flags(g.Any(r => r.CanRead), g.Any(r => r.CanWrite), g.Any(r => r.CanDelete));
        return map;
    }

    /// <summary>Basis van een gebruiker (rol, of de samenvoeging van zijn rollen) met zijn afwijkingen er bovenop.</summary>
    private static Dictionary<string, Flags> UserMap(Dictionary<string, Flags> roleMap, IEnumerable<SecurityUserPermissionOverride> overrides)
    {
        var map = new Dictionary<string, Flags>(roleMap, StringComparer.OrdinalIgnoreCase);
        foreach (var ov in overrides)
        {
            map.TryGetValue(ov.PermissionCode, out var b);
            map[ov.PermissionCode] = new Flags(ov.CanRead ?? b.Read, ov.CanWrite ?? b.Write, ov.CanDelete ?? b.Delete);
        }
        return map;
    }

    private static Dictionary<string, int> LevelsOf(Dictionary<string, Flags> map, List<Item> items) =>
        items.ToDictionary(i => i.Code, i => Effective(map, i.Code).Level, StringComparer.OrdinalIgnoreCase);

    /// <summary>Facturatiebedrijven waartoe de kaart toegang geeft: leesrecht op minstens één van de drie bedrijfscodes (klanten, leveranciers, facturatie).</summary>
    private static List<int> CompanyAccess(Dictionary<string, Flags> map, List<IssuerRef> issuers) =>
        issuers.Where(i => Effective(map, CustomerCompanyPrefix + i.Id).Read || Effective(map, SupplierCompanyPrefix + i.Id).Read || Effective(map, InvoicingCompanyPrefix + i.Id).Read)
               .Select(i => i.Id).ToList();

    private static string Initials(string? voornaam, string? naam, string? fallback)
    {
        var a = (voornaam ?? "").Trim(); var b = (naam ?? "").Trim();
        if (a.Length > 0 && b.Length > 0) return (a[..1] + b[..1]).ToUpperInvariant();
        var s = (a + b).Trim();
        if (s.Length == 0) s = (fallback ?? "?").Trim();
        return s.Length >= 2 ? s[..2].ToUpperInvariant() : s.ToUpperInvariant();
    }

    private static string DisplayName(Users u)
    {
        var n = string.Join(' ', new[] { u.Voornaam, u.Familienaam }.Where(v => !string.IsNullOrWhiteSpace(v)));
        return string.IsNullOrWhiteSpace(n) ? (u.UserId ?? u.Email ?? "") : n;
    }

    /// <summary>
    /// Het type van elke gebruiker. Bewijs voor "extern": een aannemerskoppeling (UserCompanyAccess) of een gastuitnodiging. Een gebruiker met dat bewijs blijft
    /// extern zolang al zijn rollen portaalrollen zijn; heeft hij ook een rol die niet voor aannemers of gasten bedoeld is, dan is hij Intern en gaat dat voor.
    /// Een portaalrol is een rol waarvan alle leden extern bewijs hebben (zo telt een rol als "Projecten", met interne collega's erin, nooit als portaalrol).
    /// </summary>
    private async Task<(Dictionary<int, string> Types, HashSet<int> PortalRoles)> UserTypesAsync()
    {
        var contractorIds = (await _db.UserCompanyAccess.AsNoTracking().Select(a => a.UserId).Distinct().ToListAsync()).ToHashSet();
        var guestIds = new HashSet<int>();
        try { guestIds = (await _db.UserGuestInvitation.AsNoTracking().Where(i => i.UserType != "internal").Select(i => i.UserId).ToListAsync()).ToHashSet(); } catch { /* tabel bestaat nog niet */ }
        var evidence = contractorIds.Union(guestIds).ToHashSet();
        var activeRoles = (await _db.SecurityRole.AsNoTracking().Where(r => r.IsActive).Select(r => r.RoleId).ToListAsync()).ToHashSet();
        var userRoles = (await _db.SecurityUserRole.AsNoTracking().ToListAsync()).Where(x => activeRoles.Contains(x.RoleId)).ToList();
        var portalRoles = userRoles.GroupBy(x => x.RoleId).Where(g => g.All(x => evidence.Contains(x.UserId))).Select(g => g.Key).ToHashSet();
        var rolesOf = userRoles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());
        var types = new Dictionary<int, string>();
        foreach (var id in await _db.Users.AsNoTracking().Select(u => u.Id).ToListAsync())
        {
            var extern_ = evidence.Contains(id) && (!rolesOf.TryGetValue(id, out var r) || r.All(portalRoles.Contains));
            types[id] = !extern_ ? "intern" : contractorIds.Contains(id) ? "aannemer" : "gast";
        }
        return (types, portalRoles);
    }


    private static string StatusKey(Users u, UserGuestInvitation? invite) =>
        !u.IsActive ? "uitgeschakeld"
        : invite != null && invite.InvitationStatus is GuestInvitationStatus.Draft or GuestInvitationStatus.Invited or GuestInvitationStatus.PendingAcceptance ? "uitgenodigd"
        : "actief";

    // ── 49a Gebruikers ────────────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> IndexV2()
    {
        await GetPermissionDefinitionsAsync();   // zorgt dat alle permissiecodes (ook per bedrijf) bestaan
        var items = Items();
        var issuers = await ActiveIssuersAsync();
        var me = User.GetCpmUserId();

        var roles = await _db.SecurityRole.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var userRoles = await _db.SecurityUserRole.AsNoTracking().ToListAsync();
        var rolePerms = await _db.SecurityRolePermission.AsNoTracking().ToListAsync();
        var overrides = await _db.SecurityUserPermissionOverride.AsNoTracking().ToListAsync();
        var invites = new Dictionary<int, UserGuestInvitation>();
        try { invites = (await _db.UserGuestInvitation.AsNoTracking().ToListAsync()).GroupBy(i => i.UserId).ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.UpdatedAt).First()); } catch { /* tabel bestaat nog niet */ }
        var contractorAccess = await _db.UserCompanyAccess.AsNoTracking().Include(a => a.Company).ToListAsync();
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam).ToListAsync();

        var roleMaps = roles.ToDictionary(r => r.RoleId, r => RoleMap(rolePerms.Where(p => p.RoleId == r.RoleId), IsAdminRole(r.Name)));
        var roleById = roles.ToDictionary(r => r.RoleId);
        var overridesByUser = overrides.ToLookup(o => o.UserId);
        var (userTypes, portalRoles) = await UserTypesAsync();

        var list = users.Select(u =>
        {
            invites.TryGetValue(u.Id, out var inv);

            var myRoles = userRoles.Where(r => r.UserId == u.Id && roleById.ContainsKey(r.RoleId)).Select(r => roleById[r.RoleId]).OrderBy(r => r.Name).ToList();
            var role = myRoles.FirstOrDefault();
            var baseMap = NewMap();
            foreach (var r in myRoles)
                foreach (var kv in roleMaps[r.RoleId])
                    baseMap[kv.Key] = baseMap.TryGetValue(kv.Key, out var cur) ? new Flags(cur.Read || kv.Value.Read, cur.Write || kv.Value.Write, cur.Delete || kv.Value.Delete) : kv.Value;
            var map = UserMap(baseMap, overridesByUser[u.Id]);
            var ovs = overridesByUser[u.Id].ToList();
            var afwijkingen = ovs.Count(o => !IsCompanyCode(o.PermissionCode)) + (ovs.Any(o => IsCompanyCode(o.PermissionCode)) ? 1 : 0);
            var isAannemer = userTypes.GetValueOrDefault(u.Id, "intern") == "aannemer";
            var firmIds = isAannemer ? new List<int>() : CompanyAccess(map, issuers);
            string bedrijven = isAannemer
                ? string.Join(", ", contractorAccess.Where(a => a.UserId == u.Id).Select(a => a.Company?.BedrijfsNaam).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
                : firmIds.Count == 0 ? "—" : firmIds.Count == issuers.Count && issuers.Count > 1 ? $"Alle {issuers.Count}" : string.Join(", ", issuers.Where(i => firmIds.Contains(i.Id)).Select(i => i.Name));
            return new
            {
                id = u.Id,
                naam = DisplayName(u),
                email = u.Email ?? "",
                initialen = Initials(u.Voornaam, u.Familienaam, u.UserId),
                type = userTypes.GetValueOrDefault(u.Id, "intern"),
                roleId = role?.RoleId,
                rol = role?.Name,
                afwijkingen,
                bedrijven = string.IsNullOrWhiteSpace(bedrijven) ? "—" : bedrijven,
                entra = !string.IsNullOrWhiteSpace(u.EntraObjectId) || !string.IsNullOrWhiteSpace(inv?.ExternalObjectId),
                status = StatusKey(u, inv),
                laatsteLogin = inv?.LastLoginAt?.ToString("o"),
                uitnodiging = inv?.InvitationStatus,
                ikzelf = me == u.Id
            };
        }).ToList();

        var rolesJson = roles.Select(r =>
        {
            var members = list.Where(l => l.roleId == r.RoleId).ToList();
            return new { id = r.RoleId, naam = r.Name, leden = members.Count, portaal = portalRoles.Contains(r.RoleId) };
        }).ToList();

        SetCrumb();
        ViewData["BackUrl"] = Url.Action("Index", "Instellingen");
        return ViewJson("IndexV2", new
        {
            gebruikers = list,
            rollen = rolesJson,
            bedrijven = issuers.Select(i => new { id = i.Id, naam = i.Name }),
            kanSchrijven = HttpContext.RequestServices.GetRequiredService<FacadeCore.IPermissionService>().HasWrite(PermissionCodes.SettingsUsers),
            kanRollen = HttpContext.RequestServices.GetRequiredService<FacadeCore.IPermissionService>().HasRead(PermissionCodes.SettingsRoles)
        });
    }

    // ── 49b Gebruiker · rechten ───────────────────────────────────────────────────────────────────

    private async Task<(Users user, List<SecurityRole> roles, Dictionary<string, Flags> baseMap, List<SecurityUserPermissionOverride> ovs)?> LoadUserRights(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;
        var roleIds = await _db.SecurityUserRole.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync();
        var roles = await _db.SecurityRole.AsNoTracking().Where(r => roleIds.Contains(r.RoleId) && r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var rids = roles.Select(r => r.RoleId).ToList();
        var rows = await _db.SecurityRolePermission.AsNoTracking().Where(p => rids.Contains(p.RoleId)).ToListAsync();
        var baseMap = RoleMap(rows, roles.Any(r => IsAdminRole(r.Name)));
        var ovs = await _db.SecurityUserPermissionOverride.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
        return (user, roles, baseMap, ovs);
    }

    private async Task<object> RechtenState(int userId)
    {
        var data = await LoadUserRights(userId);
        if (data == null) return new { };
        var (user, roles, baseMap, ovs) = data.Value;
        var items = Items();
        var issuers = await ActiveIssuersAsync();
        var map = UserMap(baseMap, ovs);
        var role = roles.FirstOrDefault();
        return new
        {
            roleId = role?.RoleId,
            rolNaam = role?.Name,
            admin = roles.Any(r => IsAdminRole(r.Name)),
            basis = LevelsOf(baseMap, items),
            niveaus = LevelsOf(map, items),
            afwijkend = ovs.Where(o => !IsCompanyCode(o.PermissionCode)).Select(o => o.PermissionCode).ToList(),
            bedrijvenRol = CompanyAccess(baseMap, issuers),
            bedrijven = CompanyAccess(map, issuers),
            bedrijvenAfwijkend = ovs.Any(o => IsCompanyCode(o.PermissionCode)),
            actief = user.IsActive
        };
    }

    [HttpGet]
    public async Task<IActionResult> Rechten(int id)
    {
        if (ViewData["UseGlV2Layout"] as bool? != true) return RedirectToAction(nameof(Edit), new { id });
        await GetPermissionDefinitionsAsync();
        await EnsureHiddenCodesAsync();
        var data = await LoadUserRights(id);
        if (data == null) return NotFound();
        var (user, roles, _, _) = data.Value;
        var invite = await _db.UserGuestInvitation.AsNoTracking().Where(i => i.UserId == id).OrderByDescending(i => i.UpdatedAt).FirstOrDefaultAsync();
        var (userTypes, _) = await UserTypesAsync();
        var allRoles = await _db.SecurityRole.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var perms = HttpContext.RequestServices.GetRequiredService<FacadeCore.IPermissionService>();

        SetCrumb(DisplayName(user), "Rechten", new { id });
        ViewData["BackUrl"] = Url.Action("Index", "UserAdmin");
        return ViewJson("RechtenV2", new
        {
            gebruiker = new
            {
                id = user.Id,
                naam = DisplayName(user),
                email = user.Email ?? "",
                initialen = Initials(user.Voornaam, user.Familienaam, user.UserId),
                type = userTypes.GetValueOrDefault(user.Id, "intern"),
                status = StatusKey(user, invite),
                entraId = user.EntraObjectId ?? invite?.ExternalObjectId,
                laatsteLogin = invite?.LastLoginAt?.ToString("o"),
                uitnodiging = invite?.InvitationStatus,
                ikzelf = User.GetCpmUserId() == user.Id
            },
            rollen = allRoles.Select(r => new { id = r.RoleId, naam = r.Name }),
            boom = Items().Select(i => new { code = i.Code, naam = i.Name, ouder = i.Parent, bedrijf = i.PerCompany, info = Info.GetValueOrDefault(i.Code) }),
            bedrijven = (await ActiveIssuersAsync()).Select(i => new { id = i.Id, naam = i.Name }),
            stand = await RechtenState(id),
            kanSchrijven = perms.HasWrite(PermissionCodes.SettingsUsers)
        });
    }

    // ── 49c Rollen ────────────────────────────────────────────────────────────────────────────────

    private async Task<object> RolState(int roleId)
    {
        var role = await _db.SecurityRole.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == roleId && r.IsActive);
        if (role == null) return new { };
        var items = Items();
        var issuers = await ActiveIssuersAsync();
        var rows = await _db.SecurityRolePermission.AsNoTracking().Where(p => p.RoleId == roleId).ToListAsync();
        var map = RoleMap(rows, IsAdminRole(role.Name));
        var memberIds = await _db.SecurityUserRole.AsNoTracking().Where(x => x.RoleId == roleId).Select(x => x.UserId).ToListAsync();
        var members = await _db.Users.AsNoTracking().Where(u => memberIds.Contains(u.Id)).OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam).ToListAsync();
        var withOverrides = (await _db.SecurityUserPermissionOverride.AsNoTracking().Where(o => memberIds.Contains(o.UserId)).Select(o => o.UserId).Distinct().ToListAsync()).ToHashSet();
        return new
        {
            id = role.RoleId,
            naam = role.Name,
            admin = IsAdminRole(role.Name),
            niveaus = LevelsOf(map, items),
            bedrijven = CompanyAccess(map, issuers),
            leden = members.Select(m => new { id = m.Id, naam = DisplayName(m), initialen = Initials(m.Voornaam, m.Familienaam, m.UserId), afwijkend = withOverrides.Contains(m.Id) })
        };
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> Rollen(int? id)
    {
        if (ViewData["UseGlV2Layout"] as bool? != true) return RedirectToAction(nameof(Index));
        await GetPermissionDefinitionsAsync();
        await EnsureHiddenCodesAsync();
        var roles = await _db.SecurityRole.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var counts = (await _db.SecurityUserRole.AsNoTracking().ToListAsync()).GroupBy(x => x.RoleId).ToDictionary(g => g.Key, g => g.Count());
        var userCount = await _db.Users.CountAsync();
        var (_, portalRoles) = await UserTypesAsync();
        var allUserRoles = await _db.SecurityUserRole.AsNoTracking().ToListAsync();
        var selected = roles.FirstOrDefault(r => r.RoleId == id) ?? roles.FirstOrDefault();
        var perms = HttpContext.RequestServices.GetRequiredService<FacadeCore.IPermissionService>();

        SetCrumb("Rollen", "Rollen");
        ViewData["BackUrl"] = Url.Action("Index", "Instellingen");
        return ViewJson("RollenV2", new
        {
            rollen = roles.Select(r =>
            {
                var members = allUserRoles.Where(x => x.RoleId == r.RoleId).Select(x => x.UserId).ToList();
                return new { id = r.RoleId, naam = r.Name, leden = members.Count, portaal = portalRoles.Contains(r.RoleId) };
            }),
            aantalGebruikers = userCount,
            boom = Items().Select(i => new { code = i.Code, naam = i.Name, ouder = i.Parent, bedrijf = i.PerCompany, info = Info.GetValueOrDefault(i.Code) }),
            bedrijven = (await ActiveIssuersAsync()).Select(i => new { id = i.Id, naam = i.Name }),
            geselecteerd = selected?.RoleId,
            stand = selected == null ? (object)new { } : await RolState(selected.RoleId),
            kanSchrijven = perms.HasWrite(PermissionCodes.SettingsRoles)
        });
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolStateV2(int id) => Json(await RolState(id));
}
