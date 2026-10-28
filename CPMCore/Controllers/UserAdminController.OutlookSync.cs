using CPMCore.Services.OutlookSync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen › Gebruikers en rollen › Outlook-sync (spec #2, ticket #6): per gebruiker met de koppeling aan of de sync loopt. Enkel status en
// foutcode, nooit mail- of agenda-inhoud.
public partial class UserAdminController
{
    public sealed record OutlookSyncRij(string Naam, string Email, OutlookSyncWeergave Weergave, int FoutenOpRij);

    [HttpGet]
    public async Task<IActionResult> OutlookSync(CancellationToken ct = default)
    {
        var nu = DateTime.UtcNow;
        var rijen = await (from r in _db.OutlookSyncStatus.AsNoTracking()
                           join u in _db.Users.AsNoTracking() on r.UserId equals u.Id
                           where r.Aan
                           orderby u.Voornaam, u.Familienaam
                           select new { Rij = r, u.Voornaam, u.Familienaam, u.Email }).ToListAsync(ct);

        var model = rijen.Select(x => new OutlookSyncRij($"{x.Voornaam} {x.Familienaam}".Trim(), x.Email ?? "", OutlookSyncService.Weergave(x.Rij, nu), x.Rij.FoutenOpRij)).ToList();
        SetCrumb("Outlook-sync", "OutlookSync");
        ViewData["BackUrl"] = Url.Action("Index", "UserAdmin");
        ViewData["UseGlV2Layout"] = true;
        return View("OutlookSync", model);
    }
}
