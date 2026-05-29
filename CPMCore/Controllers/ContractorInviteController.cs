using CPMCore.Helpers;
using CPMCore.Models.ContractorInvite;
using CPMCore.Services;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DALCore.Models;

namespace CPMCore.Controllers;

[Authorize(Policy = "CpmAdmin")]
[Route("Aannemer/Uitnodiging")]
public class ContractorInviteController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IContractorInviteService _inviteService;
    private readonly IEntraGuestInvitationService _guestInvitationService;
    private readonly IResendInviteUrlBuilder _resendUrlBuilder;
    private readonly IConfiguration _configuration;

    public ContractorInviteController(
        cpmRunningContext db,
        IContractorInviteService inviteService,
        IEntraGuestInvitationService guestInvitationService,
        IResendInviteUrlBuilder resendUrlBuilder,
        IConfiguration configuration)
    {
        _db = db;
        _inviteService = inviteService;
        _guestInvitationService = guestInvitationService;
        _resendUrlBuilder = resendUrlBuilder;
        _configuration = configuration;
    }

    // ── Uitnodigingsformulier ────────────────────────────────────────────────

    [HttpGet("Aanmaken")]
    public async Task<IActionResult> Aanmaken(int companyId)
    {
        var company = await _db.CompanyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId);

        if (company == null)
            return NotFound();

        var vm = new ContractorInviteVm
        {
            CompanyId   = companyId,
            CompanyName = company.BedrijfsNaam ?? $"Bedrijf {companyId}"
        };

        return View(vm);
    }

    [HttpPost("Aanmaken")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aanmaken(ContractorInviteVm model, CancellationToken ct)
    {
        // Bedrijfsnaam herstellen (niet in form)
        var company = await _db.CompanyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == model.CompanyId, ct);

        model.CompanyName = company?.BedrijfsNaam ?? $"Bedrijf {model.CompanyId}";

        if (!ModelState.IsValid)
            return View(model);

        var appBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var invitedBy  = User.GetCpmUserId();

        var request = new ContractorInviteRequest(
            CompanyId:        model.CompanyId,
            Email:            model.Email,
            FirstName:        model.FirstName,
            LastName:         model.LastName,
            IsFullCompanyAdmin: model.IsFullCompanyAdmin,
            JobFunction:      model.JobFunction,
            Phone:            model.Phone);

        var result = await _inviteService.InviteResponsibleAsync(request, appBaseUrl, invitedBy, ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Uitnodiging mislukt.");
            return View(model);
        }

        TempData["Message"] = $"Uitnodiging verstuurd naar {model.Email}.";
        return RedirectToAction(nameof(Overzicht), new { companyId = model.CompanyId });
    }

    // ── Overzicht verantwoordelijken per bedrijf ─────────────────────────────

    [HttpGet("Overzicht")]
    public async Task<IActionResult> Overzicht(int companyId)
    {
        var company = await _db.CompanyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId);

        if (company == null)
            return NotFound();

        var accesses = await _db.UserCompanyAccess
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.User)
            .ToListAsync();

        var userIds    = accesses.Select(a => a.UserId).ToList();
        var invitations = await _db.UserGuestInvitation
            .AsNoTracking()
            .Where(i => userIds.Contains(i.UserId))
            .ToDictionaryAsync(i => i.UserId);

        var rows = accesses
            .Where(a => a.User != null)
            .Select(a =>
            {
                invitations.TryGetValue(a.UserId, out var inv);
                return new ContractorAccessRow
                {
                    UserId              = a.UserId,
                    DisplayName         = FormatName(a.User!),
                    Email               = a.User!.Email ?? "",
                    IsFullCompanyAdmin  = a.Role == ContractorAccessRole.VolledigVerantwoordelijk,
                    IsActive            = a.User.IsActive,
                    InvitationStatus    = inv?.InvitationStatus,
                    LastLoginAt         = inv?.LastLoginAt
                };
            })
            .OrderBy(r => r.DisplayName)
            .ToList();

        var vm = new ContractorOverzichtVm
        {
            CompanyId   = companyId,
            CompanyName = company.BedrijfsNaam ?? $"Bedrijf {companyId}",
            Rows        = rows
        };

        return View(vm);
    }

    // ── Toegang intrekken ────────────────────────────────────────────────────

    [HttpPost("Verwijder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verwijder(int userId, int companyId, CancellationToken ct)
    {
        var access = await _db.UserCompanyAccess
            .FirstOrDefaultAsync(a => a.UserId == userId && a.CompanyId == companyId, ct);

        if (access != null)
        {
            _db.UserCompanyAccess.Remove(access);
            await _db.SaveChangesAsync(ct);
        }

        // Entra-koppeling wissen zodat de gebruiker niet meer kan inloggen
        var performedBy = User.GetCpmUserId();
        await _guestInvitationService.ResetRedemptionAsync(userId, performedBy, ct);

        TempData["Message"] = "Toegang ingetrokken.";
        return RedirectToAction(nameof(Overzicht), new { companyId });
    }

    // ── Rol wijzigen ─────────────────────────────────────────────────────────

    [HttpPost("WijzigRol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WijzigRol(int userId, int companyId, bool isFullAdmin, CancellationToken ct)
    {
        var access = await _db.UserCompanyAccess
            .FirstOrDefaultAsync(a => a.UserId == userId && a.CompanyId == companyId, ct);

        if (access == null)
            return NotFound();

        access.Role = isFullAdmin
            ? ContractorAccessRole.VolledigVerantwoordelijk
            : ContractorAccessRole.Beperkt;

        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Rol bijgewerkt.";
        return RedirectToAction(nameof(Overzicht), new { companyId });
    }

    // ── Resend uitnodiging (anoniem, via link in de puntenlijst-mail) ─────────

    [HttpGet("Hernieuw")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendInvite(string e, int c, long x, string s)
    {
        if (string.IsNullOrWhiteSpace(e) || string.IsNullOrWhiteSpace(s))
            return View("ResendInviteResult", (bool?)null);

        if (!_resendUrlBuilder.TryValidate(e, c, x, s))
            return View("ResendInviteResult", (bool?)null);

        var appBase = (_configuration["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var success = await _inviteService.ResendInviteByEmailAsync(e, c, appBase);
        return View("ResendInviteResult", (bool?)success);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatName(Users u)
    {
        var name = string.Join(' ',
            new[] { u.Voornaam, u.Familienaam }.Where(v => !string.IsNullOrWhiteSpace(v)));
        return string.IsNullOrWhiteSpace(name) ? u.UserId ?? u.Email ?? "" : name;
    }
}
