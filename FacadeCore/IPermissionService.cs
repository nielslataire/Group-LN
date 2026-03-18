namespace FacadeCore;
using BOCore;

public interface IPermissionService
{
    Task EnsureLoadedAsync(CancellationToken ct = default);
    bool HasRead(string code);
    bool HasWrite(string code);
    bool HasDelete(string code);
    IReadOnlyDictionary<string, PermissionGrant> EffectivePermissions { get; }
}