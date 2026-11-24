using BOCore;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>Instellingen › Bouwstaten: de bedrijfsbrede standaardlijst (migratie 158). Hetzelfde recht als de sjablonen (<see cref="PermissionCodes.SettingsPlanningSjablonen"/>).</summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsPlanningSjablonen)]
[Route("Instellingen/Bouwstaten")]
public class BouwstaatAdminController : BaseController
{
    private readonly IBouwstaatService _service;

    public BouwstaatAdminController(IBouwstaatService service)
    {
        _service = service;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetBreadcrumbs();
        SetPageHeader("bx bx-target-lock", "Bouwstaten", "Instellingen",
            "De toestanden die een planning kan opleveren en waar schijven en traject naar verwijzen.");
        return View("IndexV2", await _service.Lijst());
    }

    [HttpGet("Nieuw")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Nieuw()
    {
        SetBreadcrumbs("Nieuwe bouwstaat");
        SetPageHeader("bx bx-target-lock", "Nieuwe bouwstaat", "Instellingen");
        var volgende = (await _service.Lijst()).Select(b => b.Volgorde).DefaultIfEmpty(0).Max() + 10;
        return View("EditV2", new BouwstaatBewerk { Volgorde = volgende });
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var model = await _service.Haal(id);
        if (model == null) return NotFound();
        SetBreadcrumbs(model.Naam);
        SetPageHeader("bx bx-target-lock", $"Bouwstaat — {model.Naam}", "Instellingen");
        return View("EditV2", model);
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Opslaan(BouwstaatBewerk model)
    {
        var uitkomst = await _service.Bewaar(model);
        if (uitkomst.Ok)
        {
            AddMessage("success", "De bouwstaat is opgeslagen.", "Opgeslagen");
            return RedirectToAction(nameof(Index));
        }
        foreach (var fout in uitkomst.Fouten) ModelState.AddModelError(string.Empty, fout);
        SetBreadcrumbs(model.Id == 0 ? "Nieuwe bouwstaat" : model.Naam);
        SetPageHeader("bx bx-target-lock", model.Id == 0 ? "Nieuwe bouwstaat" : $"Bouwstaat — {model.Naam}", "Instellingen");
        return View("EditV2", model);
    }

    [HttpPost("{id:int}/Actief")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Actief(int id, bool actief)
    {
        var fout = await _service.ZetActief(id, actief);
        AddMessage(fout == null ? "success" : "danger",
            fout ?? (actief ? "De bouwstaat is weer actief en kan gekozen worden." : "De bouwstaat is inactief en wordt niet meer aangeboden."),
            fout == null ? (actief ? "Geactiveerd" : "Inactief gezet") : "Fout");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var fout = await _service.Verwijder(id);
        AddMessage(fout == null ? "success" : "danger", fout ?? "De bouwstaat is verwijderd.", fout == null ? "Verwijderd" : "Niet verwijderd");
        return RedirectToAction(nameof(Index));
    }

    private void SetBreadcrumbs(string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var lijst = new MvcBreadcrumbNode("Index", "BouwstaatAdmin", "Bouwstaten") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "BouwstaatAdmin", leaf) { Parent = lijst };
    }
}
