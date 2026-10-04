using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Leveranciers;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

/// <summary>Werfportaal-toegang voor leverancierscontacten: uitnodigen en intrekken; geen eigen views. Opgesplitst uit LeveranciersController.cs (okt. 2026, structureren) - views in Views/Leveranciers/Portal/. Zelfde partial class: alle private velden/services van LeveranciersController.cs blijven gewoon bruikbaar.</summary>
public partial class LeveranciersController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> InviteContact(int contactId, bool isFullAdmin, CancellationToken ct)
    {
        var contact = await _db.CompanyContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContactId == contactId, ct);

        if (contact == null)
        {
            TempData["Error"] = "Contact niet gevonden.";
            return RedirectToAction(nameof(Details), new { id = 0 });
        }

        if (string.IsNullOrWhiteSpace(contact.Email))
        {
            TempData["Error"] = "Dit contact heeft geen e-mailadres. Voeg eerst een e-mailadres toe.";
            return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
        }

        var appBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var invitedBy  = User.GetCpmUserId();

        var request = new ContractorInviteRequest(
            CompanyId:          contact.CompanyId,
            Email:              contact.Email,
            FirstName:          contact.ContactVoornaam ?? string.Empty,
            LastName:           contact.ContactNaam ?? string.Empty,
            IsFullCompanyAdmin: isFullAdmin,
            JobFunction:        contact.Functie,
            Phone:              contact.Gsm ?? contact.Telefoon,
            ContactId:          isFullAdmin ? null : contact.ContactId);

        var result = await _contractorInviteService.InviteResponsibleAsync(request, appBaseUrl, invitedBy, ct);

        TempData[result.Success ? "Message" : "Error"] = result.Success
            ? $"Uitnodiging verstuurd naar {contact.Email}."
            : result.ErrorMessage ?? "Uitnodiging mislukt.";

        return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> RevokeContactAccess(int contactId, CancellationToken ct)
    {
        var contact = await _db.CompanyContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContactId == contactId, ct);

        if (contact == null)
            return RedirectToAction(nameof(Details), new { id = 0 });

        var email = contact.Email?.Trim().ToLowerInvariant();
        if (email != null)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == email, ct);
            if (user != null)
            {
                var access = await _db.UserCompanyAccess
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.CompanyId == contact.CompanyId, ct);
                if (access != null)
                    _db.UserCompanyAccess.Remove(access);

                await _db.SaveChangesAsync(ct);

                // Entra-koppeling en OID wissen zodat de gebruiker niet meer kan inloggen
                var performedBy = User.GetCpmUserId();
                await _guestInvitationService.ResetRedemptionAsync(user.Id, performedBy, ct);
            }
        }

        TempData["Message"] = "Portaaltoegang ingetrokken.";
        return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
    }

}
