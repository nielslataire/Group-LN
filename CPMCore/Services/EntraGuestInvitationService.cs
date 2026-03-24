using Azure.Identity;
using CPMCore.Service;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace CPMCore.Services;

// ── Resultaat-type ──────────────────────────────────────────────────────────
public record GuestInviteResult(bool Success, string? ErrorMessage = null)
{
    public static GuestInviteResult Ok => new(true);
}

// ── Status-constanten ───────────────────────────────────────────────────────
public static class GuestInvitationStatus
{
    public const string Draft             = "Draft";
    public const string Invited           = "Invited";
    public const string PendingAcceptance = "PendingAcceptance";
    public const string Redeemed          = "Redeemed";
    public const string Active            = "Active";
    public const string Failed            = "Failed";
    public const string Revoked           = "Revoked";
}

// ── Audit-acties ────────────────────────────────────────────────────────────
public static class GuestAuditAction
{
    public const string InviteSent      = "InviteSent";
    public const string InviteResent    = "InviteResent";
    public const string InviteReset     = "InviteReset";
    public const string Redeemed        = "Redeemed";
    public const string FirstLogin      = "FirstLogin";
    public const string LoginUpdated    = "LoginUpdated";
    public const string Linked          = "Linked";
    public const string Unlinked        = "Unlinked";
    public const string UserDeactivated = "UserDeactivated";
}

// ── Interface ───────────────────────────────────────────────────────────────
public interface IEntraGuestInvitationService
{
    /// <summary>
    /// Nodigt een gebruiker uit als Entra B2B gast en stuurt branded e-mail.
    /// Idempotent: bestaand record wordt bijgewerkt.
    /// </summary>
    Task<GuestInviteResult> InviteGuestAsync(int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default);

    /// <summary>Maakt een nieuw Graph-invite (nieuw redeemUrl) en stuurt branded e-mail opnieuw.</summary>
    Task<GuestInviteResult> ResendInvitationAsync(int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default);

    /// <summary>Zet status terug op Revoked en wist OID-koppeling zodat beheerder opnieuw kan uitnodigen.</summary>
    Task<GuestInviteResult> ResetRedemptionAsync(int userId, int? performedByUserId, CancellationToken ct = default);

    /// <summary>Verwijdert het uitnodigingsrecord en de Entra OID-koppeling volledig.</summary>
    Task<GuestInviteResult> UnlinkGuestAsync(int userId, int? performedByUserId, CancellationToken ct = default);
}

// ── Implementatie ────────────────────────────────────────────────────────────
public class EntraGuestInvitationService : IEntraGuestInvitationService
{
    private readonly cpmRunningContext _db;
    private readonly GraphServiceClient? _graphClient;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EntraGuestInvitationService> _logger;

    public EntraGuestInvitationService(
        cpmRunningContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<EntraGuestInvitationService> logger)
    {
        _db            = db;
        _emailSender   = emailSender;
        _configuration = configuration;
        _logger        = logger;
        _graphClient   = CreateAppOnlyGraphClient(configuration, logger);
    }

    /// <summary>
    /// Maakt een app-only GraphServiceClient met ClientSecretCredential.
    /// Vereist AzureAd:TenantId, AzureAd:ClientId en AzureAd:ClientSecret.
    /// Retourneert null als de configuratie ontbreekt (service degradeert graceful).
    /// </summary>
    private static GraphServiceClient? CreateAppOnlyGraphClient(
        IConfiguration configuration, ILogger logger)
    {
        var tenantId     = configuration["AzureAd:TenantId"];
        var clientId     = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogWarning(
                "EntraGuestInvitationService: AzureAd-configuratie ontbreekt " +
                "(TenantId={TenantId}, ClientId={ClientId}). Graph-uitnodigingen uitgeschakeld.",
                tenantId ?? "<null>",
                clientId ?? "<null>");
            return null;
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    // ── InviteGuestAsync ────────────────────────────────────────────────────
    public async Task<GuestInviteResult> InviteGuestAsync(
        int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return new GuestInviteResult(false, "Gebruiker niet gevonden.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return new GuestInviteResult(false, "Gebruiker heeft geen e-mailadres.");

        var (redeemUrl, objectId, graphError) = await CallGraphInviteAsync(user.Email, appBaseUrl, ct);
        if (graphError != null)
            return new GuestInviteResult(false, graphError);

        var invitation = await _db.UserGuestInvitation
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

        var now = DateTime.UtcNow;

        if (invitation == null)
        {
            invitation = new UserGuestInvitation
            {
                UserId        = userId,
                ExternalEmail = user.Email.Trim().ToLowerInvariant(),
                CreatedAt     = now
            };
            _db.UserGuestInvitation.Add(invitation);
        }

        invitation.InvitationStatus = GuestInvitationStatus.Invited;
        invitation.InvitationSentAt = now;
        invitation.InviteRedeemUrl  = redeemUrl;
        invitation.InvitedByUserId  = invitedByUserId;
        invitation.UpdatedAt        = now;

        if (!string.IsNullOrWhiteSpace(objectId))
            invitation.ExternalObjectId = objectId;

        await _db.SaveChangesAsync(ct);

        await AddAuditAsync(userId, GuestAuditAction.InviteSent, invitedByUserId,
            $"Uitnodiging verstuurd naar {user.Email}.", ct);

        await SendBrandedEmailAsync(user, redeemUrl, appBaseUrl, ct);

        _logger.LogInformation(
            "Gastuitnodiging verstuurd voor gebruiker {UserId} ({Email}).", userId, user.Email);

        return GuestInviteResult.Ok;
    }

    // ── ResendInvitationAsync ───────────────────────────────────────────────
    public async Task<GuestInviteResult> ResendInvitationAsync(
        int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return new GuestInviteResult(false, "Gebruiker niet gevonden.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return new GuestInviteResult(false, "Gebruiker heeft geen e-mailadres.");

        var invitation = await _db.UserGuestInvitation
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

        if (invitation == null)
            return new GuestInviteResult(false,
                "Geen bestaande uitnodiging gevonden. Gebruik 'Uitnodigen' eerst.");

        var (redeemUrl, objectId, graphError) = await CallGraphInviteAsync(user.Email, appBaseUrl, ct);
        if (graphError != null)
            return new GuestInviteResult(false, graphError);

        var now = DateTime.UtcNow;
        invitation.InvitationStatus = GuestInvitationStatus.Invited;
        invitation.InvitationSentAt = now;
        invitation.InviteRedeemUrl  = redeemUrl;
        invitation.InvitedByUserId  = invitedByUserId;
        invitation.UpdatedAt        = now;

        if (!string.IsNullOrWhiteSpace(objectId))
            invitation.ExternalObjectId = objectId;

        await _db.SaveChangesAsync(ct);

        await AddAuditAsync(userId, GuestAuditAction.InviteResent, invitedByUserId,
            $"Uitnodiging opnieuw verstuurd naar {user.Email}.", ct);

        await SendBrandedEmailAsync(user, redeemUrl, appBaseUrl, ct);

        _logger.LogInformation(
            "Gastuitnodiging herverstuurd voor gebruiker {UserId}.", userId);

        return GuestInviteResult.Ok;
    }

    // ── ResetRedemptionAsync ────────────────────────────────────────────────
    public async Task<GuestInviteResult> ResetRedemptionAsync(
        int userId, int? performedByUserId, CancellationToken ct = default)
    {
        var invitation = await _db.UserGuestInvitation
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

        if (invitation == null)
            return new GuestInviteResult(false,
                "Geen uitnodigingsrecord gevonden voor deze gebruiker.");

        invitation.InvitationStatus     = GuestInvitationStatus.Revoked;
        invitation.InvitationRedeemedAt = null;
        invitation.ExternalObjectId     = null;
        invitation.ExternalTenantId     = null;
        invitation.UpdatedAt            = DateTime.UtcNow;

        // Wis ook EntraObjectId zodat de gebruiker niet meer kan inloggen vóór nieuw invite.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
            user.EntraObjectId = null;

        await _db.SaveChangesAsync(ct);

        await AddAuditAsync(userId, GuestAuditAction.InviteReset, performedByUserId,
            "Uitnodiging gereset; OID-koppeling gewist.", ct);

        _logger.LogInformation("Gastuitnodiging gereset voor gebruiker {UserId}.", userId);
        return GuestInviteResult.Ok;
    }

    // ── UnlinkGuestAsync ────────────────────────────────────────────────────
    public async Task<GuestInviteResult> UnlinkGuestAsync(
        int userId, int? performedByUserId, CancellationToken ct = default)
    {
        var invitation = await _db.UserGuestInvitation
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

        if (invitation != null)
            _db.UserGuestInvitation.Remove(invitation);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
            user.EntraObjectId = null;

        await _db.SaveChangesAsync(ct);

        await AddAuditAsync(userId, GuestAuditAction.Unlinked, performedByUserId,
            "Entra gastuitnodiging verwijderd en OID-koppeling verbroken.", ct);

        _logger.LogInformation("Gastkoppeling verbroken voor gebruiker {UserId}.", userId);
        return GuestInviteResult.Ok;
    }

    // ── Privé hulpmethoden ──────────────────────────────────────────────────

    /// <summary>
    /// Roept Microsoft Graph POST /invitations aan (sendInvitationMessage=false).
    /// Retourneert (redeemUrl, objectId, errorMessage).
    /// </summary>
    private async Task<(string? RedeemUrl, string? ObjectId, string? Error)> CallGraphInviteAsync(
        string email, string appBaseUrl, CancellationToken ct)
    {
        if (_graphClient == null)
        {
            _logger.LogWarning(
                "Graph client niet geconfigureerd; invite verstuurd zonder Microsoft-redeemUrl.");
            return (null, null, null);   // zachte fout: branded e-mail wordt toch verstuurd
        }

        var loginUrl = appBaseUrl.TrimEnd('/') + "/Account/Login";

        try
        {
            var result = await _graphClient.Invitations
                .Request()
                .AddAsync(new Invitation
                {
                    InvitedUserEmailAddress = email,
                    InviteRedirectUrl       = loginUrl,
                    SendInvitationMessage   = false
                }, ct);

            _logger.LogInformation(
                "Graph uitnodiging aangemaakt voor {Email}: status={Status}, oid={Oid}",
                email,
                result?.Status ?? "<null>",
                result?.InvitedUser?.Id ?? "<null>");

            return (result?.InviteRedeemUrl, result?.InvitedUser?.Id, null);
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph uitnodiging mislukt voor {Email}.", email);
            return (null, null, $"Microsoft Graph fout ({ex.StatusCode}): {ex.Error?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onverwachte fout bij Graph uitnodiging voor {Email}.", email);
            return (null, null, $"Onverwachte fout: {ex.Message}");
        }
    }

    private async Task SendBrandedEmailAsync(
        Users user, string? redeemUrl, string appBaseUrl, CancellationToken ct)
    {
        var loginUrl    = appBaseUrl.TrimEnd('/') + "/Account/Login";
        var firstName   = HtmlEncode(user.Voornaam?.Trim());
        var companyName = HtmlEncode(_configuration["Branding:CompanyName"] ?? "CPMCore");

        var redeemSection = string.IsNullOrWhiteSpace(redeemUrl)
            ? string.Empty
            : $@"<p style=""font-size:.85em;color:#888;margin-top:24px;"">
    Werkt de knop niet? Gebruik dan de directe Microsoft-uitnodigingslink:<br/>
    <a href=""{redeemUrl}"">{redeemUrl}</a>
  </p>";

        var body = $@"<html><body style=""font-family:Arial,sans-serif;color:#333;max-width:600px;"">
  <h2 style=""color:#0a5a3b ;"">Welkom bij {companyName}</h2>
  <p>Beste {firstName},</p>
  <p>U bent uitgenodigd als externe gebruiker voor <strong>{companyName}</strong>.
     Via onderstaande knop kunt u zich aanmelden met uw Microsoft-account.</p>
  <p style=""margin:32px 0;"">
    <a href=""{loginUrl}"" style=""background:#0a5a3b ;color:#fff;padding:12px 28px;
       border-radius:4px;text-decoration:none;font-weight:bold;display:inline-block;"">
      Aanmelden bij {companyName}
    </a>
  </p>
  {redeemSection}
  <hr style=""border:none;border-top:1px solid #eee;margin-top:32px;""/>
  <p style=""font-size:.8em;color:#aaa;"">
    U ontvangt dit bericht omdat een beheerder u heeft uitgenodigd.
    Als u geen toegang verwacht, kunt u dit bericht negeren.
  </p>
</body></html>";

        try
        {
            await _emailSender.SendEmailAsync(
                toEmail:     user.Email!,
                subject:     $"Uitnodiging: toegang tot {_configuration["Branding:CompanyName"] ?? "CPMCore"}",
                htmlMessage: body);
        }
        catch (Exception ex)
        {
            // Geen harde fout: Graph-invite is al aangemaakt.
            _logger.LogWarning(
                ex, "Branded e-mail kon niet verstuurd worden naar {Email}.", user.Email);
        }
    }

    private async Task AddAuditAsync(
        int userId, string action, int? performedBy, string? details, CancellationToken ct)
    {
        _db.UserGuestInvitationAudit.Add(new UserGuestInvitationAudit
        {
            UserId      = userId,
            Action      = action,
            Details     = details,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static string HtmlEncode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
