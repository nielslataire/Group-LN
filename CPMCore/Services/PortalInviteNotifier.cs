using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CPMCore.Services;

namespace CPMCore.Services;

/// <summary>
/// Implementatie van IPortalInviteNotifier voor gebruik door ServiceCore-services.
/// Selecteert relevante portal-gebruikers per bedrijf+project en stelt uitnodigingen
/// in via IContractorInviteService.
/// </summary>
public class PortalInviteNotifier : IPortalInviteNotifier
{
    private readonly cpmRunningContext _db;
    private readonly IContractorInviteService _inviteService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortalInviteNotifier> _logger;

    public PortalInviteNotifier(
        cpmRunningContext db,
        IContractorInviteService inviteService,
        IConfiguration configuration,
        ILogger<PortalInviteNotifier> logger)
    {
        _db = db;
        _inviteService = inviteService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureRecipientInvitedAsync(
        int companyId,
        string email,
        string firstName,
        string lastName,
        bool isFullAdmin,
        int? contactId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        try
        {
            var normalized = email.Trim().ToLowerInvariant();

            // Kijk of de gebruiker al bestaat met een actieve uitnodiging voor dit bedrijf
            var existingUser = await _db.Users
                .AsNoTracking()
                .Where(u => u.Email != null && u.Email.Trim().ToLower() == normalized)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(ct);

            if (existingUser != null)
            {
                var hasAccess = await _db.UserCompanyAccess
                    .AnyAsync(a => a.UserId == existingUser.Id && a.CompanyId == companyId, ct);

                if (hasAccess)
                {
                    // Gebruiker bestaat al met toegang: enkel uitnodigen als nog niet actief
                    await _inviteService.EnsureInvitedAsync(existingUser.Id, appBaseUrl, invitedByUserId, ct);
                    return;
                }
            }

            // Gebruiker bestaat nog niet of heeft nog geen toegang: aanmaken + uitnodigen
            var request = new ContractorInviteRequest(
                CompanyId:          companyId,
                Email:              email,
                FirstName:          firstName,
                LastName:           lastName,
                IsFullCompanyAdmin: isFullAdmin,
                JobFunction:        null,
                Phone:              null,
                ContactId:          isFullAdmin ? null : contactId);

            await _inviteService.InviteResponsibleAsync(request, appBaseUrl, invitedByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Automatisch uitnodigen mislukt voor e-mail {Email} (bedrijf {CompanyId}).",
                email, companyId);
        }
    }

    public async Task EnsureRelevantUsersInvitedAsync(
        int companyId,
        int projectId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default)
    {
        // Find the SiteManagerContactId for this company+project contract
        var siteManagerContactId = await _db.Contract
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.CompanyId == companyId)
            .Select(c => c.SiteManagerContactId)
            .FirstOrDefaultAsync(ct);

        // Load all portal users for this company
        var accesses = await _db.UserCompanyAccess
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .ToListAsync(ct);

        foreach (var access in accesses)
        {
            var shouldInvite = access.Role == ContractorAccessRole.VolledigVerantwoordelijk
                || (access.Role == ContractorAccessRole.Beperkt
                    && siteManagerContactId.HasValue
                    && access.ContactId == siteManagerContactId.Value);

            if (!shouldInvite) continue;

            try
            {
                await _inviteService.EnsureInvitedAsync(access.UserId, appBaseUrl, invitedByUserId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Automatisch uitnodigen mislukt voor gebruiker {UserId} (bedrijf {CompanyId}, project {ProjectId}).",
                    access.UserId, companyId, projectId);
            }
        }
    }
}
