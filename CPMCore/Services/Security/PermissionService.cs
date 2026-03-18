using CPMCore.Helpers;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Security;

public record PermissionGrant(bool Read, bool Write, bool Delete);

public interface IPermissionService
{
    Task EnsureLoadedAsync(CancellationToken ct = default);
    bool HasRead(string code);
    bool HasWrite(string code);
    bool HasDelete(string code);
    IReadOnlyDictionary<string, PermissionGrant> EffectivePermissions { get; }
}

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
        var userId = user?.GetCpmUserId();
        if (userId == null)
        {
            _loaded = true;
            return;
        }

        if (user!.IsInRole("Admin") || user.IsInRole("Administrator"))
        {
            _effective = PermissionCatalog.All.ToDictionary(x => x.Code, _ => new PermissionGrant(true, true, true), StringComparer.OrdinalIgnoreCase);
            _loaded = true;
            return;
        }

        var hasSecurityTables = await _db.Database.CanConnectAsync(ct);

        if (hasSecurityTables)
        {
            try
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
                    _effective[group.Key] = new PermissionGrant(group.Any(x => x.CanRead), group.Any(x => x.CanWrite), group.Any(x => x.CanDelete));
                }

                var overrides = await _db.SecurityUserPermissionOverride.AsNoTracking()
                    .Where(x => x.UserId == userId.Value)
                    .ToListAsync(ct);

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
                // fallback to legacy role names below
            }
        }

        if (_effective.Count == 0)
        {
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

        if (_effective.TryGetValue(code, out var grant))
        {
            return access switch
            {
                PermissionAccessType.Read => grant.Read,
                PermissionAccessType.Write => grant.Write,
                PermissionAccessType.Delete => grant.Delete,
                _ => false
            };
        }

        var parent = code.Contains('.') ? code[..code.LastIndexOf('.')] : null;
        if (!string.IsNullOrWhiteSpace(parent))
            return Has(parent, access);

        return false;
    }

    private void GrantAll(string code)
    {
        _effective[code] = new PermissionGrant(true, true, true);
    }
}