using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using CPMCore.Helpers;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

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
    Task SyncUserPhotoAsync(CpmUserAccessResult accessResult, CancellationToken ct);
}

public class CpmUserAccessService : ICpmUserAccessService
{
    private readonly cpmRunningContext _db;
    private readonly ILogger<CpmUserAccessService> _logger;
    private readonly GraphServiceClient? _graphClient;

    public CpmUserAccessService(
            cpmRunningContext db,
            ILogger<CpmUserAccessService> logger,
            GraphServiceClient? graphClient)
    {
        _db = db;
        _logger = logger;
        _graphClient = graphClient;
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
            .Select(p => p.PermissionNavigation.PermissionName.Trim())
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
            var normalized = permission?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            identity.AddClaim(new Claim(CpmClaims.Permission, normalized));
            identity.AddClaim(new Claim(ClaimTypes.Role, normalized));
        }
    }

    public async Task SyncUserPhotoAsync(CpmUserAccessResult accessResult, CancellationToken ct)
    {
        if (_graphClient == null)
        {
            _logger.LogDebug("Graph client niet geconfigureerd; gebruikersfoto wordt niet opgehaald.");
            return;
        }

        try
        {
            using var photoStream = await _graphClient.Me.Photo.Content.Request().GetAsync(ct);
            if (photoStream == null)
                return;

            await using var buffer = new MemoryStream();
            await photoStream.CopyToAsync(buffer, ct);
            var photoBytes = buffer.ToArray();
            if (photoBytes.Length == 0)
                return;

            var hash = ComputeHash(photoBytes);
            if (string.Equals(accessResult.User.PhotoHash, hash, StringComparison.Ordinal))
                return;

            accessResult.User.Photo = photoBytes;
            accessResult.User.PhotoHash = hash;
            accessResult.User.PhotoContentType = "image/jpeg";

            await _db.SaveChangesAsync(ct);
        }
        catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Geen foto gevonden voor gebruiker {UserId}.", accessResult.User.Id);
        }
        catch (ServiceException ex)
        {
            _logger.LogWarning(ex, "Kon gebruikersfoto niet ophalen voor gebruiker {UserId}.", accessResult.User.Id);
        }
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string ComputeHash(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}