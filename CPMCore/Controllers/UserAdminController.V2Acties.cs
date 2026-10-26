using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Services.Security;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CPMCore.Services.Security.PermissionTreeV2;

namespace CPMCore.Controllers;

// gl-v2 — schrijfacties van Instellingen › Gebruikers en rollen (zie UserAdminController.V2.cs voor de schermen en de regels).
// Alle acties geven JSON terug ({ ok, bericht, stand }) zodat de pagina zich zonder herladen kan bijwerken.
public partial class UserAdminController
{
    private static readonly string[] CompanyFunctionCodes = { PermissionCodes.Customers, PermissionCodes.Suppliers, PermissionCodes.Invoicing };

    private static string CompanyPrefixOf(string functionCode) =>
        string.Equals(functionCode, PermissionCodes.Customers, StringComparison.OrdinalIgnoreCase) ? CustomerCompanyPrefix
        : string.Equals(functionCode, PermissionCodes.Suppliers, StringComparison.OrdinalIgnoreCase) ? SupplierCompanyPrefix
        : InvoicingCompanyPrefix;

    /// <summary>Breidt een lijst codes uit met de verborgen kindcodes (klanten → alle klanten en per facturatiebedrijf, enz.).</summary>
    private static IEnumerable<string> WithHiddenChildren(IEnumerable<string> codes)
    {
        foreach (var c in codes)
        {
            yield return c;
            if (HiddenChildren.TryGetValue(c, out var kids)) foreach (var k in kids) yield return k;
        }
    }

    private IActionResult Fout(string bericht) => Json(new { ok = false, bericht });

    // ── Gebruiker: rol, niveaus, afwijkingen ──────────────────────────────────────────────────────

    /// <summary>Schrijft de gewenste vlaggen als afwijking ten opzichte van de rol: een gelijk niveau verwijdert de afwijking (ze verdwijnt vanzelf).</summary>
    private async Task ApplyUserDesired(int userId, Dictionary<string, Flags> baseMap, Dictionary<string, Flags> desired)
    {
        var rows = await _db.SecurityUserPermissionOverride.Where(o => o.UserId == userId).ToListAsync();
        foreach (var (code, flags) in desired)
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.PermissionCode, code, StringComparison.OrdinalIgnoreCase));
            if (flags == Effective(baseMap, code))
            {
                if (row != null) { _db.SecurityUserPermissionOverride.Remove(row); rows.Remove(row); }
                continue;
            }
            if (row == null)
            {
                await EnsurePermissionCodeExistsAsync(code);
                row = new SecurityUserPermissionOverride { UserId = userId, PermissionCode = code };
                _db.SecurityUserPermissionOverride.Add(row);
                rows.Add(row);
            }
            row.CanRead = flags.Read; row.CanWrite = flags.Write; row.CanDelete = flags.Delete;
        }
        await _db.SaveChangesAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerRolV2(int userId, int roleId)
    {
        if (User.GetCpmUserId() == userId) return Fout("Je kunt je eigen rol niet wijzigen.");
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) return Fout("Deze gebruiker bestaat niet meer.");
        if (!await _db.SecurityRole.AnyAsync(r => r.RoleId == roleId && r.IsActive)) return Fout("Deze rol bestaat niet meer.");
        var bestaand = await _db.SecurityUserRole.Where(x => x.UserId == userId).ToListAsync();
        var gewijzigd = bestaand.Count != 1 || bestaand[0].RoleId != roleId;
        if (gewijzigd)
        {
            _db.SecurityUserRole.RemoveRange(bestaand);
            _db.SecurityUserRole.Add(new SecurityUserRole { UserId = userId, RoleId = roleId });
            // een andere rol begint zonder afwijkingen (zoals het oude scherm)
            _db.SecurityUserPermissionOverride.RemoveRange(await _db.SecurityUserPermissionOverride.Where(o => o.UserId == userId).ToListAsync());
            await _db.SaveChangesAsync();
        }
        return Json(new { ok = true, stand = await RechtenState(userId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerNiveauV2(int userId, string[]? codes, int niveau)
    {
        if (niveau is < 0 or > 3) return Fout("Onbekend niveau.");
        var data = await LoadUserRights(userId);
        if (data == null) return Fout("Deze gebruiker bestaat niet meer.");
        var (_, _, baseMap, ovs) = data.Value;
        var items = Items();
        var geldig = items.Select(i => i.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var functies = (codes ?? Array.Empty<string>()).Where(geldig.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (functies.Count == 0) return Fout("Geen geldige functie gekozen.");
        if (User.GetCpmUserId() == userId && functies.Any(f => AdminProtectedCodes.Contains(f, StringComparer.OrdinalIgnoreCase)) && niveau < 1)
            return Fout("Je kunt jezelf niet buitensluiten uit Gebruikers en rollen.");

        var desired = NewMap();
        foreach (var code in WithHiddenChildren(functies)) desired[code] = Flags.FromLevel(niveau);

        // Functies per bedrijf: de bedrijfsrijen volgen het niveau, voor de bedrijven waartoe de gebruiker toegang heeft.
        var issuers = await ActiveIssuersAsync();
        var chips = CompanyAccess(UserMap(baseMap, ovs), issuers);
        foreach (var fn in CompanyFunctionCodes.Where(f => functies.Contains(f, StringComparer.OrdinalIgnoreCase)))
            foreach (var i in issuers) desired[CompanyPrefixOf(fn) + i.Id] = chips.Contains(i.Id) ? Flags.FromLevel(niveau) : Flags.None;

        await ApplyUserDesired(userId, baseMap, desired);
        return Json(new { ok = true, stand = await RechtenState(userId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerResetV2(int userId, string[]? codes)
    {
        var rows = _db.SecurityUserPermissionOverride.Where(o => o.UserId == userId);
        if (codes is { Length: > 0 })
        {
            var set = WithHiddenChildren(codes).ToHashSet(StringComparer.OrdinalIgnoreCase);
            _db.SecurityUserPermissionOverride.RemoveRange(await rows.Where(o => set.Contains(o.PermissionCode)).ToListAsync());
        }
        else _db.SecurityUserPermissionOverride.RemoveRange(await rows.ToListAsync());
        await _db.SaveChangesAsync();
        return Json(new { ok = true, stand = await RechtenState(userId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerBedrijfV2(int userId, int issuerId, bool toegang)
    {
        var data = await LoadUserRights(userId);
        if (data == null) return Fout("Deze gebruiker bestaat niet meer.");
        var (_, _, baseMap, ovs) = data.Value;
        if (!(await ActiveIssuersAsync()).Any(i => i.Id == issuerId)) return Fout("Dit facturatiebedrijf bestaat niet meer.");
        var map = UserMap(baseMap, ovs);
        var desired = NewMap();
        foreach (var fn in CompanyFunctionCodes)
            desired[CompanyPrefixOf(fn) + issuerId] = toegang ? Flags.FromLevel(Effective(map, fn).Level) : Flags.None;
        await ApplyUserDesired(userId, baseMap, desired);
        return Json(new { ok = true, stand = await RechtenState(userId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerToegangV2(int userId, bool actief)
    {
        if (User.GetCpmUserId() == userId && !actief) return Fout("Je kunt je eigen toegang niet uitschakelen.");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Fout("Deze gebruiker bestaat niet meer.");
        user.IsActive = actief;
        await _db.SaveChangesAsync();
        return Json(new { ok = true, actief, stand = await RechtenState(userId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerKopierenV2(int userId, int vanGebruikerId)
    {
        if (userId == vanGebruikerId) return Fout("Kies een andere gebruiker.");
        if (User.GetCpmUserId() == userId) return Fout("Je kunt je eigen rechten niet overschrijven.");
        if (!await _db.Users.AnyAsync(u => u.Id == userId) || !await _db.Users.AnyAsync(u => u.Id == vanGebruikerId)) return Fout("Deze gebruiker bestaat niet meer.");

        _db.SecurityUserRole.RemoveRange(await _db.SecurityUserRole.Where(x => x.UserId == userId).ToListAsync());
        _db.SecurityUserPermissionOverride.RemoveRange(await _db.SecurityUserPermissionOverride.Where(o => o.UserId == userId).ToListAsync());
        foreach (var r in await _db.SecurityUserRole.AsNoTracking().Where(x => x.UserId == vanGebruikerId).ToListAsync())
            _db.SecurityUserRole.Add(new SecurityUserRole { UserId = userId, RoleId = r.RoleId });
        foreach (var o in await _db.SecurityUserPermissionOverride.AsNoTracking().Where(x => x.UserId == vanGebruikerId).ToListAsync())
            _db.SecurityUserPermissionOverride.Add(new SecurityUserPermissionOverride { UserId = userId, PermissionCode = o.PermissionCode, CanRead = o.CanRead, CanWrite = o.CanWrite, CanDelete = o.CanDelete });
        await _db.SaveChangesAsync();
        return Json(new { ok = true, stand = await RechtenState(userId) });
    }

    [HttpGet]
    public async Task<IActionResult> RechtenStateV2(int id) => Json(await RechtenState(id));

    [HttpGet]
    public async Task<IActionResult> GebruikerKandidatenV2(int exclude)
    {
        var users = await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Id != exclude).OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam).ToListAsync();
        return Json(users.Select(u => new { id = u.Id, naam = DisplayName(u), email = u.Email ?? "", initialen = Initials(u.Voornaam, u.Familienaam, u.UserId) }));
    }

    // ── Rollen ────────────────────────────────────────────────────────────────────────────────────

    private sealed record NiveauWijziging(string Code, int Niveau);

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolOpslaanV2(int roleId, string? naam, string? wijzigingen, int[]? bedrijven, bool bedrijvenGewijzigd)
    {
        var role = await _db.SecurityRole.FirstOrDefaultAsync(r => r.RoleId == roleId && r.IsActive);
        if (role == null) return Fout("Deze rol bestaat niet meer.");
        var admin = IsAdminRole(role.Name);

        List<NiveauWijziging> changes;
        try { changes = string.IsNullOrWhiteSpace(wijzigingen) ? new() : JsonSerializer.Deserialize<List<NiveauWijziging>>(wijzigingen, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new(); }
        catch (JsonException) { return Fout("De wijzigingen konden niet gelezen worden."); }
        if (admin && (changes.Count > 0 || bedrijvenGewijzigd)) return Fout("De rol Admin heeft altijd alle rechten en kan niet aangepast worden.");

        var nieuweNaam = naam?.Trim();
        if (!string.IsNullOrWhiteSpace(nieuweNaam) && !string.Equals(nieuweNaam, role.Name, StringComparison.Ordinal))
        {
            if (admin) return Fout("De rol Admin kan niet hernoemd worden.");
            if (nieuweNaam.Length > 100) return Fout("De naam mag maximaal 100 tekens hebben.");
            if (await _db.SecurityRole.AnyAsync(x => x.RoleId != roleId && x.IsActive && x.Name == nieuweNaam)) return Fout("Er bestaat al een rol met deze naam.");
            role.Name = nieuweNaam;
        }

        var items = Items();
        var geldig = items.Select(i => i.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = await _db.SecurityRolePermission.Where(p => p.RoleId == roleId).ToListAsync();
        var map = RoleMap(rows, false);
        var issuers = await ActiveIssuersAsync();
        var chips = bedrijvenGewijzigd ? (bedrijven ?? Array.Empty<int>()).ToList() : CompanyAccess(map, issuers);

        var geraakt = new List<string>();
        foreach (var w in changes.Where(c => geldig.Contains(c.Code) && c.Niveau is >= 0 and <= 3))
            foreach (var code in WithHiddenChildren(new[] { w.Code }))
            {
                map[code] = Flags.FromLevel(w.Niveau);
                geraakt.Add(code);
            }

        // Functies per bedrijf: de bedrijfsrijen volgen het niveau van hun functie, beperkt tot de bedrijfstoegang van de rol.
        var desired = NewMap();
        foreach (var kv in map.Where(k => geraakt.Contains(k.Key, StringComparer.OrdinalIgnoreCase))) desired[kv.Key] = kv.Value;
        if (bedrijvenGewijzigd || geraakt.Any(g => CompanyFunctionCodes.Contains(g, StringComparer.OrdinalIgnoreCase)))
            foreach (var fn in CompanyFunctionCodes)
                foreach (var i in issuers)
                {
                    var code = CompanyPrefixOf(fn) + i.Id;
                    var flags = chips.Contains(i.Id) ? Flags.FromLevel(Effective(map, fn).Level) : Flags.None;
                    map[code] = flags;
                    desired[code] = flags;
                    geraakt.Add(code);
                }

        foreach (var (code, flags) in desired)
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.PermissionCode, code, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                await EnsurePermissionCodeExistsAsync(code);
                row = new SecurityRolePermission { RoleId = roleId, PermissionCode = code };
                _db.SecurityRolePermission.Add(row);
                rows.Add(row);
            }
            row.CanRead = flags.Read; row.CanWrite = flags.Write; row.CanDelete = flags.Delete;
        }
        await _db.SaveChangesAsync();

        var members = await _db.SecurityUserRole.AsNoTracking().Where(x => x.RoleId == roleId).Select(x => x.UserId).ToListAsync();
        var behouden = geraakt.Count == 0 ? 0 : await _db.SecurityUserPermissionOverride.AsNoTracking()
            .Where(o => members.Contains(o.UserId) && geraakt.Contains(o.PermissionCode)).Select(o => o.UserId).Distinct().CountAsync();
        return Json(new { ok = true, leden = members.Count, afwijkingenBehouden = behouden, stand = await RolState(roleId) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolNieuwV2(string? naam, int? kopieVan)
    {
        var n = naam?.Trim();
        if (string.IsNullOrWhiteSpace(n)) return Fout("Geef een naam in voor de rol.");
        if (n.Length > 100) return Fout("De naam mag maximaal 100 tekens hebben.");
        if (await _db.SecurityRole.AnyAsync(x => x.IsActive && x.Name == n)) return Fout("Er bestaat al een rol met deze naam.");
        var role = new SecurityRole { Name = n, IsActive = true };
        _db.SecurityRole.Add(role);
        await _db.SaveChangesAsync();
        if (kopieVan.HasValue)
        {
            var bron = await _db.SecurityRole.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == kopieVan.Value);
            // de rol Admin heeft zijn rechten niet als rijen: een kopie ervan begint daarom met alle rechten op Verwijderen
            var rijen = bron != null && IsAdminRole(bron.Name)
                ? PermissionCatalog.All.Select(d => new SecurityRolePermission { PermissionCode = d.Code, CanRead = true, CanWrite = true, CanDelete = true }).ToList()
                : await _db.SecurityRolePermission.AsNoTracking().Where(x => x.RoleId == kopieVan.Value).ToListAsync();
            foreach (var p in rijen)
                _db.SecurityRolePermission.Add(new SecurityRolePermission { RoleId = role.RoleId, PermissionCode = p.PermissionCode, CanRead = p.CanRead, CanWrite = p.CanWrite, CanDelete = p.CanDelete });
            await _db.SaveChangesAsync();
        }
        return Json(new { ok = true, id = role.RoleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolVerwijderV2(int roleId)
    {
        var role = await _db.SecurityRole.FirstOrDefaultAsync(r => r.RoleId == roleId);
        if (role == null) return Fout("Deze rol bestaat niet meer.");
        if (IsAdminRole(role.Name)) return Fout("De rol Admin kan niet verwijderd worden.");
        if (await _db.SecurityUserRole.AnyAsync(x => x.RoleId == roleId)) return Fout("Een rol met leden kan pas verwijderd worden nadat de leden een andere rol hebben.");
        _db.SecurityRolePermission.RemoveRange(await _db.SecurityRolePermission.Where(x => x.RoleId == roleId).ToListAsync());
        _db.SecurityRole.Remove(role);
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    /// <summary>Zet de gekozen gebruikers in deze rol (een gebruiker heeft één rol; wie van rol wisselt, begint zonder afwijkingen).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolLedenV2(int roleId, int[]? gebruikers)
    {
        if (!await _db.SecurityRole.AnyAsync(r => r.RoleId == roleId && r.IsActive)) return Fout("Deze rol bestaat niet meer.");
        var me = User.GetCpmUserId();
        foreach (var uid in (gebruikers ?? Array.Empty<int>()).Distinct())
        {
            if (uid == me) continue;
            var bestaand = await _db.SecurityUserRole.Where(x => x.UserId == uid).ToListAsync();
            if (bestaand.Count == 1 && bestaand[0].RoleId == roleId) continue;
            _db.SecurityUserRole.RemoveRange(bestaand);
            _db.SecurityUserRole.Add(new SecurityUserRole { UserId = uid, RoleId = roleId });
            _db.SecurityUserPermissionOverride.RemoveRange(await _db.SecurityUserPermissionOverride.Where(o => o.UserId == uid).ToListAsync());
        }
        await _db.SaveChangesAsync();
        return Json(new { ok = true, stand = await RolState(roleId) });
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsRoles)]
    public async Task<IActionResult> RolKandidatenV2(int roleId)
    {
        var rolNamen = await _db.SecurityRole.AsNoTracking().ToDictionaryAsync(r => r.RoleId, r => r.Name);
        var userRoles = (await _db.SecurityUserRole.AsNoTracking().ToListAsync()).GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());
        var users = await _db.Users.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam).ToListAsync();
        return Json(users.Select(u => new
        {
            id = u.Id,
            naam = DisplayName(u),
            email = u.Email ?? "",
            initialen = Initials(u.Voornaam, u.Familienaam, u.UserId),
            rol = userRoles.TryGetValue(u.Id, out var r) && r.Count > 0 && rolNamen.TryGetValue(r[0], out var rn) ? rn : null,
            inRol = userRoles.TryGetValue(u.Id, out var rr) && rr.Count == 1 && rr[0] == roleId
        }));
    }
}
