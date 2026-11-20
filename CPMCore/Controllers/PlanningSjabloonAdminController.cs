using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>Instellingen › Planningssjablonen: lijst en editor (blokken, taken, voorwaarden, estafette, voorgangers via codes). Migratie 146.</summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsPlanningSjablonen)]
[Route("Instellingen/Planningssjablonen")]
public class PlanningSjabloonAdminController : BaseController
{
    private readonly IPlanningSjabloonService _service;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public PlanningSjabloonAdminController(IPlanningSjabloonService service)
    {
        _service = service;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetBreadcrumbs();
        SetPageHeader("bx bx-calendar-event", "Planningssjablonen", "Instellingen",
            "Beheer de standaardplanningen die je op woningen en gebouwen toepast.");
        return View("IndexV2", await _service.BeheerLijst());
    }

    [HttpGet("Nieuw")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public IActionResult Nieuw()
    {
        SetBreadcrumbs("Nieuw sjabloon");
        SetPageHeader("bx bx-calendar-event", "Nieuw planningssjabloon", "Instellingen");
        return View("EditV2", new PlanningSjabloonBewerk());
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var sjabloon = await _service.Haal(id);
        if (sjabloon == null) return NotFound();
        SetBreadcrumbs(sjabloon.Naam);
        SetPageHeader("bx bx-calendar-event", $"Sjabloon — {sjabloon.Naam}", "Instellingen");
        return View("EditV2", sjabloon);
    }

    /// <summary>Bewaart het sjabloon uit de editor. Antwoordt altijd met JSON: ok + id, of de lijst fouten die de gebruiker moet corrigeren.</summary>
    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Opslaan([FromBody] PlanningSjabloonBewerk sjabloon)
    {
        if (sjabloon == null) return BadRequest();
        var uitkomst = await _service.Bewaar(sjabloon, UserId);
        if (uitkomst.Ok) AddMessage("success", "Het sjabloon is opgeslagen.", "Opgeslagen");
        return Json(uitkomst, JsonOpts);
    }

    /// <summary>Proefdraai van het sjabloon dat in de editor staat (nog niet bewaard): duur per blok, woningen, kritiek pad en controle. Bewaart niets.</summary>
    [HttpPost("Proef")]
    [ValidateAntiForgeryToken]
    public IActionResult Proef([FromBody] PlanningSjabloonProefVerzoek verzoek)
    {
        if (verzoek == null) return BadRequest();
        return Json(_service.Proef(verzoek), JsonOpts);
    }

    [HttpPost("{id:int}/Dupliceren")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Dupliceren(int id)
    {
        var (nieuwId, fout) = await _service.Dupliceer(id, UserId);
        if (fout != null) { AddMessage("danger", fout, "Fout"); return RedirectToAction(nameof(Index)); }
        AddMessage("success", "Het sjabloon is gedupliceerd. Pas de kopie hier aan.", "Gedupliceerd");
        return RedirectToAction(nameof(Bewerken), new { id = nieuwId });
    }

    [HttpPost("{id:int}/Actief")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Actief(int id, bool actief)
    {
        var fout = await _service.ZetActief(id, actief, UserId);
        AddMessage(fout == null ? "success" : "danger",
            fout ?? (actief ? "Het sjabloon is weer actief en kan toegepast worden." : "Het sjabloon is inactief en wordt niet meer aangeboden bij het toepassen."),
            fout == null ? (actief ? "Geactiveerd" : "Inactief gezet") : "Fout");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var fout = await _service.Verwijder(id);
        AddMessage(fout == null ? "success" : "danger",
            fout ?? "Het sjabloon is verwijderd.",
            fout == null ? "Verwijderd" : "Fout");
        return RedirectToAction(nameof(Index));
    }

    private void SetBreadcrumbs(string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var sjablonen = new MvcBreadcrumbNode("Index", "ProjectSjabloonAdmin", "Sjablonen") { Parent = instellingen };
        var lijst = new MvcBreadcrumbNode("Index", "PlanningSjabloonAdmin", "Planning") { Parent = sjablonen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "PlanningSjabloonAdmin", leaf) { Parent = lijst };
    }
}
