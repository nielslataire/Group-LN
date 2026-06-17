using DALCore.Models;
using FacadeCore;
using BOCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore;

public class PermissionService : IPermissionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly cpmRunningContext _db;
    private Dictionary<string, PermissionGrant> _effective = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public PermissionService(IHttpContextAccessor httpContextAccessor, cpmRunningContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public IReadOnlyDictionary<string, PermissionGrant> EffectivePermissions => _effective;

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded) return;

        var user = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirst("cpm:user-id")?.Value;
        var userId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : (int?)null;
        if (!userId.HasValue)
        {
            _loaded = true;
            return;
        }

        var isAdmin = user!.IsInRole("Admin") || user.IsInRole("Administrator");
        if (isAdmin)
        {
            _effective = PermissionCatalog.All.ToDictionary(
                x => x.Code,
                _ => new PermissionGrant(true, true, true),
                StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            if (!isAdmin)
            {
                var roleIds = await _db.SecurityUserRole.AsNoTracking()
                    .Where(x => x.UserId == userId.Value)
                    .Select(x => x.RoleId)
                    .ToListAsync(ct);

                var roleGrants = await _db.SecurityRolePermission.AsNoTracking()
                    .Where(x => roleIds.Contains(x.RoleId))
                    .ToListAsync(ct);

                foreach (var group in roleGrants.GroupBy(x => x.PermissionCode, StringComparer.OrdinalIgnoreCase))
                {
                    _effective[group.Key] = new PermissionGrant(
                        group.Any(x => x.CanRead),
                        group.Any(x => x.CanWrite),
                        group.Any(x => x.CanDelete));
                }
            }

            var overrides = await _db.SecurityUserPermissionOverride.AsNoTracking().Where(x => x.UserId == userId.Value).ToListAsync(ct);
            foreach (var ov in overrides)
            {
                _effective.TryGetValue(ov.PermissionCode, out var baseGrant);
                _effective[ov.PermissionCode] = new PermissionGrant(
                    ov.CanRead ?? baseGrant?.Read ?? false,
                    ov.CanWrite ?? baseGrant?.Write ?? false,
                    ov.CanDelete ?? baseGrant?.Delete ?? false);
            }
        }
        catch
        {
            if (isAdmin && _effective.Count == 0)
            {
                _effective = PermissionCatalog.All.ToDictionary(
                    x => x.Code,
                    _ => new PermissionGrant(true, true, true),
                    StringComparer.OrdinalIgnoreCase);
            }

            var legacy = await _db.PermissionPerUser.AsNoTracking().Include(x => x.PermissionNavigation)
                .Where(x => x.UserId == userId.Value)
                .Select(x => x.PermissionNavigation.PermissionName)
                .ToListAsync(ct);

            foreach (var role in legacy)
            {
                if (string.Equals(role, "Projecten", StringComparison.OrdinalIgnoreCase))
                {
                    GrantAll(PermissionCodes.Projects);
                    GrantAll(PermissionCodes.ProjectsDetail);
                }
                if (string.Equals(role, "Klanten", StringComparison.OrdinalIgnoreCase)) GrantAll(PermissionCodes.Customers);
                if (string.Equals(role, "Leveranciers", StringComparison.OrdinalIgnoreCase)) GrantAll(PermissionCodes.Suppliers);
                if (string.Equals(role, "Boekhouding", StringComparison.OrdinalIgnoreCase)) GrantAll(PermissionCodes.Invoicing);
            }
        }

        _loaded = true;
    }

    public bool HasRead(string code) => Has(code, PermissionAccessType.Read);
    public bool HasWrite(string code) => Has(code, PermissionAccessType.Write);
    public bool HasDelete(string code) => Has(code, PermissionAccessType.Delete);

    private bool Has(string code, PermissionAccessType access)
    {
        if (!_loaded) return false;

        var hasDirect = _effective.TryGetValue(code, out var grant);
        if (hasDirect)
        {
            // Directe entry is definitief: true = toegang, false = geweigerd.
            // Geen kind-lookup bij expliciete false: kindcodes die niet in het panel
            // staan (bv. Suppliers.All) mogen een expliciete weigering niet omzeilen.
            return access switch
            {
                PermissionAccessType.Read => grant!.Read,
                PermissionAccessType.Write => grant!.Write,
                PermissionAccessType.Delete => grant!.Delete,
                _ => false
            };
        }
        else
        {
            // Geen directe match: zoek omhoog naar parent-permissie
            // ("Suppliers.Company" → "Suppliers")
            var parent = code.Contains('.') ? code[..code.LastIndexOf('.')] : null;
            if (!string.IsNullOrWhiteSpace(parent))
                return Has(parent, access);
        }

        // Zoek omlaag: als de gebruiker een child-permissie heeft (bv. "Suppliers.Company.5")
        // dan heeft hij impliciet ook toegang tot de parent ("Suppliers").
        var prefix = code + ".";
        return _effective.Any(kv =>
            kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            access switch
            {
                PermissionAccessType.Read => kv.Value.Read,
                PermissionAccessType.Write => kv.Value.Write,
                PermissionAccessType.Delete => kv.Value.Delete,
                _ => false
            });
    }

    private void GrantAll(string code) => _effective[code] = new PermissionGrant(true, true, true);
}