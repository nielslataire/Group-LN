using Azure.Identity;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace ServiceCore;

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
        int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default, string loginPath = "/Account/Login")
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return new GuestInviteResult(false, "Gebruiker niet gevonden.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return new GuestInviteResult(false, "Gebruiker heeft geen e-mailadres.");

        // Contractors gaan altijd naar /aannemer, ongeacht hoe de aanroep is gedaan
        if (loginPath == "/Account/Login")
        {
            var inv = await _db.UserGuestInvitation.AsNoTracking()
                .Where(i => i.UserId == userId)
                .Select(i => new { i.UserType })
                .FirstOrDefaultAsync(ct);
            if (inv?.UserType == "contractor")
                loginPath = "/Werfportaal";
        }

        var (redeemUrl, objectId, graphError) = await CallGraphInviteAsync(user.Email, appBaseUrl, loginPath, ct);
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

        await SendBrandedEmailAsync(user, redeemUrl, appBaseUrl, loginPath, ct);

        _logger.LogInformation(
            "Gastuitnodiging verstuurd voor gebruiker {UserId} ({Email}).", userId, user.Email);

        return GuestInviteResult.Ok;
    }

    // ── ResendInvitationAsync ───────────────────────────────────────────────
    public async Task<GuestInviteResult> ResendInvitationAsync(
        int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default, string loginPath = "/Account/Login")
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

        // Contractors gaan altijd naar /aannemer, ongeacht hoe de aanroep is gedaan
        if (loginPath == "/Account/Login" && invitation.UserType == "contractor")
            loginPath = "/aannemer";

        var (redeemUrl, objectId, graphError) = await CallGraphInviteAsync(user.Email, appBaseUrl, loginPath, ct);
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

        await SendBrandedEmailAsync(user, redeemUrl, appBaseUrl, loginPath, ct);

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

    // ── SendTestInviteEmailAsync ────────────────────────────────────────────
    public async Task<GuestInviteResult> SendTestInviteEmailAsync(
        string toEmail, string appBaseUrl, CancellationToken ct = default)
    {
        var fakeUser = new Users { Email = toEmail, Voornaam = "Testgebruiker" };
        await SendBrandedEmailAsync(fakeUser, redeemUrl: null, appBaseUrl, "/Werfportaal", ct);
        return GuestInviteResult.Ok;
    }

    // ── Privé hulpmethoden ──────────────────────────────────────────────────

    /// <summary>
    /// Roept Microsoft Graph POST /invitations aan (sendInvitationMessage=false).
    /// Retourneert (redeemUrl, objectId, errorMessage).
    /// </summary>
    private async Task<(string? RedeemUrl, string? ObjectId, string? Error)> CallGraphInviteAsync(
        string email, string appBaseUrl, string loginPath, CancellationToken ct)
    {
        if (_graphClient == null)
        {
            _logger.LogWarning(
                "Graph client niet geconfigureerd; invite verstuurd zonder Microsoft-redeemUrl.");
            return (null, null, null);   // zachte fout: branded e-mail wordt toch verstuurd
        }

        var loginUrl = appBaseUrl.TrimEnd('/') + loginPath;

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
        Users user, string? redeemUrl, string appBaseUrl, string loginPath, CancellationToken ct)
    {
        var loginUrl    = appBaseUrl.TrimEnd('/') + loginPath;
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
