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
    string DisplayName,
    string UserType = "internal",   // "internal" | "contractor" | "customer"
    string Provider = AuthProviders.Entra); // zie AuthProviders

public interface ICpmUserAccessService
{
    /// <param name="tenantId">Entra tenant-id (tid claim) — optioneel, gebruikt voor gastgebruiker-koppeling.</param>
    Task<CpmUserAccessResult?> ResolveAsync(string? entraObjectId, IEnumerable<string?> emails, CancellationToken ct, string? tenantId = null);

    /// <summary>
    /// Login via Google (rechtstreeks OIDC, buiten Entra om). Enkel voor gasten met een
    /// UserGuestInvitation (aannemers/klanten); interne gebruikers worden geweigerd.
    /// Eerste login koppelt op het (geverifieerde) e-mailadres en bewaart de Google sub-claim,
    /// daarna wordt enkel nog op <see cref="Users.GoogleSubjectId"/> gematcht.
    /// </summary>
    Task<CpmUserAccessResult?> ResolveGoogleAsync(string? subject, string? email, bool emailVerified, CancellationToken ct);

    Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct);
    void ApplyClaims(ClaimsIdentity identity, CpmUserAccessResult accessResult);

    /// <summary>Profielfoto via Microsoft Graph (/me/photo) — enkel zinvol na een Entra-login.</summary>
    Task SyncUserPhotoAsync(CpmUserAccessResult accessResult, CancellationToken ct);

    /// <summary>Profielfoto via de Google "picture"-claim (publieke URL) — na een Google-login.</summary>
    Task SyncGooglePhotoAsync(CpmUserAccessResult accessResult, string? pictureUrl, CancellationToken ct);
}

public class CpmUserAccessService : ICpmUserAccessService
{
    private readonly cpmRunningContext _db;
    private readonly ILogger<CpmUserAccessService> _logger;
    private readonly GraphServiceClient? _graphClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public CpmUserAccessService(
            cpmRunningContext db,
            ILogger<CpmUserAccessService> logger,
            GraphServiceClient? graphClient,
            IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _graphClient = graphClient;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CpmUserAccessResult?> ResolveAsync(string? entraObjectId, IEnumerable<string?> emails, CancellationToken ct, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            _logger.LogWarning("Entra login zonder oid claim.");
            return null;
        }

        var normalizedEmails = emails
            .SelectMany(ExpandGuestEmail)
            .Select(NormalizeEmail)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        // Stap 1: zoek primair op EntraObjectId
        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

        // Stap 2: fallback op e-mail (interne users en gastgebruikers met status Invited/PendingAcceptance)
        if (user == null && normalizedEmails.Count > 0)
        {
            user = await FindUserByEmailAsync(normalizedEmails!, ct);

            if (user != null && string.IsNullOrWhiteSpace(user.EntraObjectId))
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

        // Gastuitnodiging bijwerken bij login
        await UpdateGuestInvitationOnLoginAsync(user.Id, AuthProviders.Entra, entraObjectId, tenantId, ct);

        return await BuildResultAsync(user, normalizedEmails.FirstOrDefault(), entraObjectId, AuthProviders.Entra, ct);
    }

    public async Task<CpmUserAccessResult?> ResolveGoogleAsync(string? subject, string? email, bool emailVerified, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("Google login zonder sub claim.");
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        // Stap 1: zoek primair op de stabiele Google-subject
        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.GoogleSubjectId == subject, ct);

        // Stap 2: eerste login — koppel op het uitgenodigde e-mailadres, maar enkel als Google het
        // adres als geverifieerd meldt (anders kan iemand met een willekeurig Google-account en een
        // zelfgekozen e-mailadres een uitnodiging kapen).
        if (user == null)
        {
            if (!emailVerified || normalizedEmail == null)
            {
                _logger.LogWarning("Google login {Subject} zonder geverifieerd e-mailadres; geen koppeling mogelijk.", subject);
                return null;
            }

            user = await FindUserByEmailAsync(new[] { normalizedEmail }, ct);
            if (user == null)
            {
                _logger.LogWarning("Google gebruiker {Subject} ({Email}) niet gekoppeld.", subject, normalizedEmail);
                return null;
            }
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Gebruiker {UserId} is gedeactiveerd.", user.Id);
            return null;
        }

        // Google-login is enkel voor portaalgasten (aannemers/klanten). Interne medewerkers hebben
        // permissies en horen via de organisatie-tenant (Entra) binnen te komen, ook al zou hun
        // e-mailadres toevallig aan een Google-account hangen.
        var isInternal = await _db.PermissionPerUser.AnyAsync(p => p.UserId == user.Id, ct);
        var hasGuestInvitation = await _db.UserGuestInvitation.AnyAsync(i => i.UserId == user.Id, ct);
        if (isInternal || !hasGuestInvitation)
        {
            _logger.LogWarning(
                "Google login geweigerd voor gebruiker {UserId}: intern={IsInternal}, gast={IsGuest}.",
                user.Id, isInternal, hasGuestInvitation);
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.GoogleSubjectId))
        {
            user.GoogleSubjectId = subject;
            await _db.SaveChangesAsync(ct);
        }

        await UpdateGuestInvitationOnLoginAsync(user.Id, AuthProviders.Google, subject, tenantId: null, ct);

        return await BuildResultAsync(user, normalizedEmail, user.EntraObjectId, AuthProviders.Google, ct);
    }

    /// <summary>
    /// 2a. match op Users.Email; 2b. fallback op UserGuestInvitation.ExternalEmail (gast die nog niet
    /// eerder inlogde). Gedeeld door de Entra- en Google-flow.
    /// </summary>
    private async Task<Users?> FindUserByEmailAsync(IReadOnlyCollection<string> normalizedEmails, CancellationToken ct)
    {
        var user = await _db.Users
            .Where(u => u.Email != null)
            .FirstOrDefaultAsync(u => normalizedEmails.Contains(u.Email.Trim().ToLower()), ct);

        if (user != null)
            return user;

        var guestInvite = await _db.UserGuestInvitation
            .Where(i => normalizedEmails.Contains(i.ExternalEmail) &&
                        (i.InvitationStatus == "Invited" || i.InvitationStatus == "PendingAcceptance"))
            .FirstOrDefaultAsync(ct);

        return guestInvite == null
            ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.Id == guestInvite.UserId, ct);
    }

    private async Task<CpmUserAccessResult> BuildResultAsync(
        Users user, string? email, string? entraObjectId, string provider, CancellationToken ct)
    {
        var permissions = await GetPermissionsAsync(user.Id, ct);

        // Interne gebruikers hebben permissies in PermissionPerUser.
        // Iemand met interne permissies is altijd "internal", ook als er een
        // contractor- of customer-gastuitnodiging bestaat voor diezelfde login.
        string userType;
        if (permissions.Any())
        {
            userType = "internal";
        }
        else
        {
            // Geen interne permissies → bepaal type via gastuitnodiging
            var guestType = await _db.UserGuestInvitation
                .AsNoTracking()
                .Where(i => i.UserId == user.Id)
                .Select(i => i.UserType)
                .FirstOrDefaultAsync(ct);
            userType = guestType?.ToLowerInvariant() switch
            {
                "contractor" => "contractor",
                "customer"   => "customer",
                _            => "internal"
            };
        }
        var displayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
            .Where(v => !string.IsNullOrWhiteSpace(v)));

        return new CpmUserAccessResult(
            user,
            permissions,
            email,
            entraObjectId,
            string.IsNullOrWhiteSpace(displayName) ? user.UserId : displayName,
            userType,
            provider);
    }

    /// <summary>
    /// Werkt het gastuitnodigingsrecord bij na een succesvolle login:
    /// provider/externe id/TID invullen, status naar Active, timestamps bijhouden.
    /// </summary>
    private async Task UpdateGuestInvitationOnLoginAsync(
        int userId, string provider, string externalId, string? tenantId, CancellationToken ct)
    {
        try
        {
            var invitation = await _db.UserGuestInvitation
                .FirstOrDefaultAsync(i => i.UserId == userId, ct);

            if (invitation == null)
                return;

            var now = DateTime.UtcNow;
            var changed = false;

            if (string.IsNullOrWhiteSpace(invitation.ExternalObjectId))
            {
                invitation.ExternalObjectId = externalId;
                invitation.Provider         = provider;
                changed = true;
            }
            else if (!string.Equals(invitation.Provider, provider, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(invitation.ExternalObjectId, externalId, StringComparison.Ordinal))
            {
                // Zelfde externe id, maar Provider stond nog op de kolomdefault ('MicrosoftEntra').
                invitation.Provider = provider;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(invitation.ExternalTenantId) &&
                !string.IsNullOrWhiteSpace(tenantId))
            {
                invitation.ExternalTenantId = tenantId;
                changed = true;
            }

            // Eerste redemptie
            if (invitation.InvitationStatus is "Invited" or "PendingAcceptance")
            {
                invitation.InvitationRedeemedAt ??= now;
                invitation.InvitationStatus = "Active";

                _db.UserGuestInvitationAudit.Add(new UserGuestInvitationAudit
                {
                    UserId      = userId,
                    Action      = "FirstLogin",
                    Details     = $"Eerste login via {provider}. Id={externalId}",
                    PerformedBy = null,
                    PerformedAt = now
                });
                changed = true;
            }
            else
            {
                _db.UserGuestInvitationAudit.Add(new UserGuestInvitationAudit
                {
                    UserId      = userId,
                    Action      = "LoginUpdated",
                    Details     = $"Login via {provider}. Id={externalId}",
                    PerformedBy = null,
                    PerformedAt = now
                });
                changed = true;
            }

            invitation.LastLoginAt = now;
            invitation.UpdatedAt   = now;

            if (changed)
                await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Nooit de login blokkeren door een audit-fout
            _logger.LogWarning(ex, "Kon gastuitnodiging niet bijwerken bij login voor gebruiker {UserId}.", userId);
        }
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
        identity.AddClaim(new Claim(CpmClaims.UserType, accessResult.UserType));
        identity.AddClaim(new Claim(CpmClaims.AuthProvider, accessResult.Provider));

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
            await StorePhotoIfChangedAsync(accessResult, buffer.ToArray(), "image/jpeg", ct);
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

    public async Task SyncGooglePhotoAsync(CpmUserAccessResult accessResult, string? pictureUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl)
            || !Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
            return;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var response = await client.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode)
                return;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return;

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0 || bytes.Length > 2 * 1024 * 1024)
                return;

            await StorePhotoIfChangedAsync(accessResult, bytes, contentType, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Foto is cosmetisch: nooit de login blokkeren.
            _logger.LogDebug(ex, "Kon Google-profielfoto niet ophalen voor gebruiker {UserId}.", accessResult.User.Id);
        }
    }

    private async Task StorePhotoIfChangedAsync(CpmUserAccessResult accessResult, byte[] photoBytes, string contentType, CancellationToken ct)
    {
        if (photoBytes.Length == 0)
            return;

        var hash = ComputeHash(photoBytes);
        if (string.Equals(accessResult.User.PhotoHash, hash, StringComparison.Ordinal))
            return;

        accessResult.User.Photo = photoBytes;
        accessResult.User.PhotoHash = hash;
        accessResult.User.PhotoContentType = contentType;

        await _db.SaveChangesAsync(ct);
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    /// <summary>
    /// Voor B2B-gastgebruikers retourneert Microsoft het echte e-mailadres niet rechtstreeks.
    /// In plaats daarvan is preferred_username/upn in het formaat:
    ///   {localpart}_{domain}#EXT#@{hosttenant}
    /// waarbij de @ in het originele adres vervangen is door _.
    /// Deze methode voegt het afgeleid origineel e-mailadres toe als extra kandidaat.
    /// </summary>
    private static IEnumerable<string?> ExpandGuestEmail(string? email)
    {
        yield return email;

        if (string.IsNullOrWhiteSpace(email))
            yield break;

        var extIdx = email.IndexOf("#EXT#", StringComparison.OrdinalIgnoreCase);
        if (extIdx <= 0)
            yield break;

        // Deel vóór #EXT# is {localpart}_{domain} waarbij de echte @ vervangen is door de laatste _
        var localAndDomain = email[..extIdx];
        var lastUnderscore = localAndDomain.LastIndexOf('_');
        if (lastUnderscore <= 0)
            yield break;

        var derived = localAndDomain[..lastUnderscore] + "@" + localAndDomain[(lastUnderscore + 1)..];
        yield return derived;
    }

    private static string ComputeHash(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}
