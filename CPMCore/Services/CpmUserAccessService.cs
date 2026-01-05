using System.Security.Claims;
using CPMCore.Helpers;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

public record CpmUserAccessResult(
    Users User,
    IReadOnlyList<string> Permissions,
    string? Email,
    string? EntraObjectId,
    string DisplayName);

public interface ICpmUserAccessService
{
    Task<CpmUserAccessResult?> ResolveAsync(string? entraObjectId, IEnumerable<string?> emails, CancellationToken ct);
    Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct);
    void ApplyClaims(ClaimsIdentity identity, CpmUserAccessResult accessResult);
}

public class CpmUserAccessService : ICpmUserAccessService
{
    private readonly cpmRunningContext _db;
    private readonly ILogger<CpmUserAccessService> _logger;

    public CpmUserAccessService(cpmRunningContext db, ILogger<CpmUserAccessService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CpmUserAccessResult?> ResolveAsync(string? entraObjectId, IEnumerable<string?> emails, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            _logger.LogWarning("Entra login zonder oid claim.");
            return null;
        }

        var normalizedEmails = emails
            .Select(NormalizeEmail)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

        if (user == null && normalizedEmails.Count > 0)
        {
            user = await _db.Users
                .Where(u => u.Email != null)
                .FirstOrDefaultAsync(u => normalizedEmails.Contains(u.Email.Trim().ToLower()), ct);

            if (user != null)
            {
                user.EntraObjectId = entraObjectId;
                await _db.SaveChangesAsync(ct);
            }
        }

        // CPMCore-specific authorization: access is granted only to linked, active users.
        if (user == null)
        {
            _logger.LogWarning("Entra gebruiker {EntraObjectId} niet gekoppeld.", entraObjectId);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Gebruiker {UserId} is gedeactiveerd.", user.Id);
            return null;
        }

        var permissions = await GetPermissionsAsync(user.Id, ct);
        var displayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
            .Where(v => !string.IsNullOrWhiteSpace(v)));

        return new CpmUserAccessResult(
            user,
            permissions,
            normalizedEmails.FirstOrDefault(),
            entraObjectId,
            string.IsNullOrWhiteSpace(displayName) ? user.UserId : displayName);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct)
    {
        return await _db.PermissionPerUser
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.PermissionNavigation.PermissionName)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);
    }

    public void ApplyClaims(ClaimsIdentity identity, CpmUserAccessResult accessResult)
    {
        identity.AddClaim(new Claim(CpmClaims.UserId, accessResult.User.Id.ToString()));
        identity.AddClaim(new Claim(CpmClaims.UserCode, accessResult.User.UserId ?? string.Empty));
        identity.AddClaim(new Claim(CpmClaims.DisplayName, accessResult.DisplayName));

        if (!string.IsNullOrWhiteSpace(accessResult.Email))
        {
            identity.AddClaim(new Claim(CpmClaims.Email, accessResult.Email));
            identity.AddClaim(new Claim(ClaimTypes.Email, accessResult.Email));
        }

        identity.AddClaim(new Claim(CpmClaims.EntraObjectId, accessResult.EntraObjectId ?? string.Empty));
        identity.AddClaim(new Claim(ClaimTypes.Name, accessResult.DisplayName));

        foreach (var permission in accessResult.Permissions)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, permission));
        }
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}