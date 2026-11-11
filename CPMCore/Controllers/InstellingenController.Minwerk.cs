using BOCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen › Facturatie › Minwerk · standaard schadevergoeding (design-handoff 28v). Het percentage is het voorstel dat een nieuwe
// schadevergoeding op een minwerk krijgt in Offertes & wijzigingen; per regel blijft het aan te passen (snelknoppen 10 · 20 · 25 %).
// Eén rij in ChangeOrderSettings (migratie 142). Gebruikt dezelfde rechten als de andere facturatie-instellingen.
public partial class InstellingenController
{
    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsBillingCompanies)]
    public async Task<IActionResult> Minwerk(CancellationToken ct)
    {
        SetPageHeader("bx bx-cog", "Minwerk");
        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Minwerk", "Instellingen", "Minwerk") { Parent = instellingen };
        ViewData["HelpScreenId"] = "instellingen-minwerk";

        var db = HttpContext.RequestServices.GetRequiredService<cpmRunningContext>();
        var percentage = await db.ChangeOrderSettings.AsNoTracking().Where(s => s.Id == 1).Select(s => (decimal?)s.MinworkDamagePercentage).FirstOrDefaultAsync(ct) ?? 25m;
        return View("MinwerkV2", percentage);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsBillingCompanies)]
    public async Task<IActionResult> MinwerkOpslaan(string? percentage, CancellationToken ct)
    {
        var raw = (percentage ?? "").Trim().Replace(" ", "");
        if (raw.Contains(',')) raw = raw.Replace(".", "").Replace(',', '.');
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0 || value > 100)
        {
            AddMessage("error", "Geef een percentage tussen 0 en 100 in.", "Niet bewaard");
            return RedirectToAction(nameof(Minwerk));
        }

        var db = HttpContext.RequestServices.GetRequiredService<cpmRunningContext>();
        var row = await db.ChangeOrderSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null) db.ChangeOrderSettings.Add(new ChangeOrderSettings { Id = 1, MinworkDamagePercentage = Math.Round(value, 2) });
        else row.MinworkDamagePercentage = Math.Round(value, 2);
        await db.SaveChangesAsync(ct);

        AddMessage("success", "De standaard schadevergoeding is bewaard.", "Bewaard");
        return RedirectToAction(nameof(Minwerk));
    }
}
