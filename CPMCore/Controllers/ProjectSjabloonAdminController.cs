using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>Instellingen › Projectsjablonen: een bundel van een planningssjabloon, een trajectsjabloon en een schijvensjabloon met standaardantwoorden. Migratie 156. Hetzelfde recht als de planningssjablonen.</summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsPlanningSjablonen)]
[Route("Instellingen/Projectsjablonen")]
public class ProjectSjabloonAdminController : BaseController
{
    private readonly IProjectSjabloonService _service;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProjectSjabloonAdminController(IProjectSjabloonService service)
    {
        _service = service;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetBreadcrumbs();
        SetPageHeader("bx bx-buildings", "Projectsjablonen", "Instellingen", "Bundel een planning, een traject en schijven tot één sjabloon voor een soort project.");
        return View("IndexV2", await _service.BeheerLijst());
    }

    [HttpGet("Nieuw")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Nieuw()
    {
        SetBreadcrumbs("Nieuw sjabloon");
        SetPageHeader("bx bx-buildings", "Nieuw projectsjabloon", "Instellingen");
        ViewData["Opties"] = await _service.Opties();
        return View("EditV2", new ProjectSjabloonBewerk());
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var sjabloon = await _service.Haal(id);
        if (sjabloon == null) return NotFound();
        SetBreadcrumbs(sjabloon.Naam);
        SetPageHeader("bx bx-buildings", $"Sjabloon — {sjabloon.Naam}", "Instellingen");
        ViewData["Opties"] = await _service.Opties();
        return View("EditV2", sjabloon);
    }

    /// <summary>Kerncijfers van de gekozen onderdelen en het voorbeeld voor dit gebouw (voor de editor). Bewaart niets.</summary>
    [HttpGet("Overzicht")]
    public async Task<IActionResult> Overzicht(int? planningId, int? trajectId, int? schijfId, int verdiepingen = 1, bool kelder = false, string? dak = null)
        => Json(await _service.Overzicht(planningId, trajectId, schijfId, verdiepingen, kelder, string.IsNullOrWhiteSpace(dak) ? null : dak.Trim().ToLowerInvariant()), JsonOpts);

    /// <summary>Bewaart het formulier. Bij fouten komt dezelfde pagina terug met de melding(en) en de ingevulde waarden.</summary>
    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Opslaan([FromForm] ProjectSjabloonBewerk sjabloon)
    {
        var uitkomst = await _service.Bewaar(sjabloon, UserId);
        if (uitkomst.Ok)
        {
            AddMessage("success", "Het sjabloon is opgeslagen.", "Opgeslagen");
            return RedirectToAction(nameof(Index));
        }
        SetBreadcrumbs(string.IsNullOrWhiteSpace(sjabloon.Naam) ? "Nieuw sjabloon" : sjabloon.Naam);
        SetPageHeader("bx bx-buildings", sjabloon.Id == 0 ? "Nieuw projectsjabloon" : $"Sjabloon — {sjabloon.Naam}", "Instellingen");
        ViewData["Opties"] = await _service.Opties();
        ViewData["Fouten"] = uitkomst.Fouten;
        return View("EditV2", sjabloon);
    }

    [HttpPost("{id:int}/Actief")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Actief(int id, bool actief)
    {
        var fout = await _service.ZetActief(id, actief, UserId);
        AddMessage(fout == null ? "success" : "danger",
            fout ?? (actief ? "Het sjabloon is weer actief en kan toegepast worden." : "Het sjabloon is inactief en wordt niet meer aangeboden."),
            fout == null ? (actief ? "Geactiveerd" : "Inactief gezet") : "Fout");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var fout = await _service.Verwijder(id);
        AddMessage(fout == null ? "success" : "danger", fout ?? "Het sjabloon is verwijderd.", fout == null ? "Verwijderd" : "Fout");
        return RedirectToAction(nameof(Index));
    }

    private void SetBreadcrumbs(string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var sjablonen = new MvcBreadcrumbNode("Index", "ProjectSjabloonAdmin", "Sjablonen") { Parent = instellingen };
        var lijst = new MvcBreadcrumbNode("Index", "ProjectSjabloonAdmin", "Projectsjablonen") { Parent = sjablonen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "ProjectSjabloonAdmin", leaf) { Parent = lijst };
    }
}
