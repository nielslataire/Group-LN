using CPMCore.Service;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

// ── Constanten ───────────────────────────────────────────────────────────────
public static class ContractorAccessRole
{
    /// <summary>Beperkt: toegang tot eigen projecten/punten via contract.</summary>
    public const int Beperkt = 0;
    /// <summary>Volledig verantwoordelijk: ziet alle punten van het bedrijf.</summary>
    public const int VolledigVerantwoordelijk = 1;
}

// ── Request / Result ─────────────────────────────────────────────────────────
public record ContractorInviteRequest(
    int     CompanyId,
    string  Email,
    string  FirstName,
    string  LastName,
    bool    IsFullCompanyAdmin,
    string? JobFunction = null,
    string? Phone = null,
    /// <summary>
    /// Bij beperkte toegang: de ContactId van de contactpersoon.
    /// Bepaalt welke contracten (en bijbehorende punten) zichtbaar zijn in het portaal.
    /// </summary>
    int?    ContactId = null);

public record ContractorInviteResult(bool Success, string? ErrorMessage = null, int? UserId = null)
{
    public static ContractorInviteResult Ok(int userId) => new(true, null, userId);
    public static ContractorInviteResult Fail(string error) => new(false, error);
}

// ── Interface ────────────────────────────────────────────────────────────────
public interface IContractorInviteService
{
    /// <summary>
    /// Maakt een gebruiker aan (of hergebruikt een bestaande op basis van e-mail),
    /// koppelt hem aan het bedrijf en stuurt een Entra-gastuitnodiging.
    /// </summary>
    Task<ContractorInviteResult> InviteResponsibleAsync(
        ContractorInviteRequest request,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Stuurt een uitnodiging als de gebruiker nog geen actieve uitnodiging heeft.
    /// Bedoeld voor automatisch uitnodigen bij het versturen van een puntenlijst.
    /// Doet niets als de gebruiker al een actieve/accepted uitnodiging heeft.
    /// </summary>
    Task EnsureInvitedAsync(
        int userId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default);
}

// ── Implementatie ─────────────────────────────────────────────────────────────
public class ContractorInviteService : IContractorInviteService
{
    private readonly cpmRunningContext _db;
    private readonly IEntraGuestInvitationService _guestInviteService;
    private readonly ILogger<ContractorInviteService> _logger;

    public ContractorInviteService(
        cpmRunningContext db,
        IEntraGuestInvitationService guestInviteService,
        ILogger<ContractorInviteService> logger)
    {
        _db = db;
        _guestInviteService = guestInviteService;
        _logger = logger;
    }

    public async Task<ContractorInviteResult> InviteResponsibleAsync(
        ContractorInviteRequest request,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Controleer of het bedrijf bestaat
        var company = await _db.CompanyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == request.CompanyId, ct);
        if (company == null)
            return ContractorInviteResult.Fail("Bedrijf niet gevonden.");

        // Zoek of hergebruik gebruiker op e-mail
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email != null &&
                u.Email.Trim().ToLower() == normalizedEmail, ct);

        if (user == null)
        {
            // Nieuw gebruikersnummer: uniek op basis van e-mailprefix + timestamp
            var baseUsername = normalizedEmail.Split('@')[0]
                .Replace(".", "")
                .Replace("-", "")
                .Replace("_", "");
            if (baseUsername.Length > 16) baseUsername = baseUsername[..16];

            var username = await GenerateUniqueUsernameAsync(baseUsername, ct);

            var nextId = await _db.Users.AnyAsync(ct)
                ? await _db.Users.MaxAsync(u => u.Id, ct) + 1
                : 1;

            user = new Users
            {
                Id          = nextId,
                UserId      = username,
                Email       = normalizedEmail,
                Voornaam    = request.FirstName.Trim(),
                Familienaam = request.LastName.Trim(),
                Functie     = request.JobFunction?.Trim() ?? string.Empty,
                Gsm         = request.Phone?.Trim() ?? string.Empty,
                IsActive    = true,
                Password    = string.Empty,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Aannemer-gebruiker aangemaakt: {UserId} ({Email}) voor bedrijf {CompanyId}.",
                user.UserId, normalizedEmail, request.CompanyId);
        }
        else
        {
            // Bestaande gebruiker: naam bijwerken indien leeg
            var changed = false;
            if (string.IsNullOrWhiteSpace(user.Voornaam) && !string.IsNullOrWhiteSpace(request.FirstName))
            { user.Voornaam = request.FirstName.Trim(); changed = true; }
            if (string.IsNullOrWhiteSpace(user.Familienaam) && !string.IsNullOrWhiteSpace(request.LastName))
            { user.Familienaam = request.LastName.Trim(); changed = true; }
            if (changed)
                await _db.SaveChangesAsync(ct);
        }

        // UserCompanyAccess: upsert
        var access = await _db.UserCompanyAccess
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.CompanyId == request.CompanyId, ct);

        var role = request.IsFullCompanyAdmin
            ? ContractorAccessRole.VolledigVerantwoordelijk
            : ContractorAccessRole.Beperkt;

        if (access == null)
        {
            _db.UserCompanyAccess.Add(new UserCompanyAccess
            {
                UserId    = user.Id,
                CompanyId = request.CompanyId,
                Role      = role,
                ContactId = request.IsFullCompanyAdmin ? null : request.ContactId
            });
        }
        else
        {
            access.Role      = role;
            access.ContactId = request.IsFullCompanyAdmin ? null : request.ContactId;
        }
        await _db.SaveChangesAsync(ct);

        // UserGuestInvitation: zet UserType = "contractor"
        var invitation = await _db.UserGuestInvitation
            .FirstOrDefaultAsync(i => i.UserId == user.Id, ct);

        if (invitation == null)
        {
            _db.UserGuestInvitation.Add(new UserGuestInvitation
            {
                UserId            = user.Id,
                Provider          = "Entra",
                ExternalEmail     = normalizedEmail,
                UserType          = "contractor",
                InvitationStatus  = GuestInvitationStatus.Draft,
                InvitedByUserId   = invitedByUserId,
                CreatedAt         = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        else if (invitation.UserType != "contractor")
        {
            invitation.UserType = "contractor";
            await _db.SaveChangesAsync(ct);
        }

        // Verstuur Entra-gastuitnodiging (redirect naar aannemersportaal)
        var inviteResult = await _guestInviteService.InviteGuestAsync(
            user.Id, invitedByUserId, appBaseUrl, ct, loginPath: "/Portaal");

        if (!inviteResult.Success)
            return ContractorInviteResult.Fail(inviteResult.ErrorMessage ?? "Uitnodiging mislukt.");

        return ContractorInviteResult.Ok(user.Id);
    }

    public async Task EnsureInvitedAsync(
        int userId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default)
    {
        var invitation = await _db.UserGuestInvitation
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserId == userId, ct);

        // Alleen uitnodigen als er nog geen actieve/accepted uitnodiging is
        var skipStatuses = new[] {
            GuestInvitationStatus.Active,
            GuestInvitationStatus.Redeemed
        };

        if (invitation != null && skipStatuses.Contains(invitation.InvitationStatus))
            return;

        var result = await _guestInviteService.InviteGuestAsync(userId, invitedByUserId, appBaseUrl, ct, loginPath: "/Portaal");
        if (!result.Success)
            _logger.LogWarning(
                "Automatisch uitnodigen mislukt voor gebruiker {UserId}: {Error}",
                userId, result.ErrorMessage);
    }

    private async Task<string> GenerateUniqueUsernameAsync(string base_, CancellationToken ct)
    {
        var candidate = base_;
        var counter = 1;
        while (await _db.Users.AnyAsync(u => u.UserId == candidate, ct))
        {
            candidate = $"{base_}{counter++}";
        }
        return candidate;
    }
}
