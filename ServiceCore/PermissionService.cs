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

        // Direct toegekend, of overgeërfd van een bovenliggende code (echte inheritance:
        // "Suppliers" true ⇒ ook "Suppliers.Company.5"). Een directe entry is definitief,
        // óók wanneer die op false staat.
        if (HasGrantedOrInherited(code, access))
            return true;

        // Toegang tot een containerpagina zodra de gebruiker minstens één concreet
        // (in het rechtenpaneel getoond) kindrecht heeft — bv. iemand met enkel
        // "Settings.Users" moet op Instellingen kunnen, ook al staat "Settings" zelf
        // expliciet op geweigerd. De pagina toont daarna enkel de toegestane onderdelen.
        // Verborgen aggregatie-codes (".All", ".ByBillingCompany") cascaden mee met de
        // parent en mogen een expliciete weigering NIET omzeilen.
        return HasAnyChildGrant(code, access);
    }

    /// <summary>
    /// True als <paramref name="code"/> rechtstreeks is toegekend, of als een
    /// bovenliggende code is toegekend. Een rechtstreekse entry is definitief
    /// (ook een expliciete false stopt de zoektocht omhoog).
    /// </summary>
    private bool HasGrantedOrInherited(string code, PermissionAccessType access)
    {
        if (_effective.TryGetValue(code, out var grant))
            return Granted(grant, access);

        var dot = code.LastIndexOf('.');
        return dot > 0 && HasGrantedOrInherited(code[..dot], access);
    }

    /// <summary>
    /// True als de gebruiker minstens één concreet kindrecht onder <paramref name="code"/>
    /// heeft (bv. "Settings.Users" onder "Settings", of "Suppliers.Company.5" onder
    /// "Suppliers"). De verborgen aggregatie-codes ".All" en ".ByBillingCompany" tellen
    /// hier bewust niet mee.
    /// </summary>
    private bool HasAnyChildGrant(string code, PermissionAccessType access)
    {
        var prefix = code + ".";
        return _effective.Any(kv =>
            kv.Key.Length > prefix.Length &&
            kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            !IsAggregateChildSuffix(kv.Key.AsSpan(prefix.Length)) &&
            Granted(kv.Value, access));
    }

    private static bool IsAggregateChildSuffix(ReadOnlySpan<char> suffix) =>
        suffix.Equals("All", StringComparison.OrdinalIgnoreCase) ||
        suffix.Equals("ByBillingCompany", StringComparison.OrdinalIgnoreCase);

    private static bool Granted(PermissionGrant grant, PermissionAccessType access) => access switch
    {
        PermissionAccessType.Read => grant.Read,
        PermissionAccessType.Write => grant.Write,
        PermissionAccessType.Delete => grant.Delete,
        _ => false
    };

    private void GrantAll(string code) => _effective[code] = new PermissionGrant(true, true, true);
}