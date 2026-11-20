using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>Instellingen › Schijvensjablonen: lijst en editor. Migratie 155. Hetzelfde recht als de planningssjablonen (<see cref="PermissionCodes.SettingsPlanningSjablonen"/>).</summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsPlanningSjablonen)]
[Route("Instellingen/Schijvensjablonen")]
public class SchijfSjabloonAdminController : BaseController
{
    private readonly ISchijfSjabloonService _service;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public SchijfSjabloonAdminController(ISchijfSjabloonService service)
    {
        _service = service;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetBreadcrumbs();
        SetPageHeader("bx bx-list-check", "Schijvensjablonen", "Instellingen",
            "Beheer de standaardlijsten betalingsschijven die je op een betalingsgroep toepast.");
        return View("IndexV2", await _service.BeheerLijst());
    }

    [HttpGet("Nieuw")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public IActionResult Nieuw()
    {
        SetBreadcrumbs("Nieuw sjabloon");
        SetPageHeader("bx bx-list-check", "Nieuw schijvensjabloon", "Instellingen");
        // Een voorzet die klopt met de gewoonte: de laatste schijf is de eindafrekening, vast 5 %.
        var model = new SchijfSjabloonBewerk
        {
            Schijven =
            {
                new SchijfSjabloonSchijfBewerk { Naam = "Bij het uitzetten van het gebouw", Gewicht = 1 },
                new SchijfSjabloonSchijfBewerk { Naam = "Bij de voorlopige oplevering (ontvangst van de sleutels)", VastPercentage = 5 },
            },
        };
        return View("EditV2", model);
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var sjabloon = await _service.Haal(id);
        if (sjabloon == null) return NotFound();
        SetBreadcrumbs(sjabloon.Naam);
        SetPageHeader("bx bx-list-check", $"Sjabloon — {sjabloon.Naam}", "Instellingen");
        return View("EditV2", sjabloon);
    }

    /// <summary>Bewaart het sjabloon uit de editor. Antwoordt altijd met JSON: ok + id, of de lijst fouten die de gebruiker moet corrigeren.</summary>
    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Opslaan([FromBody] SchijfSjabloonBewerk sjabloon)
    {
        if (sjabloon == null) return BadRequest();
        var uitkomst = await _service.Bewaar(sjabloon, UserId);
        if (uitkomst.Ok) AddMessage("success", "Het sjabloon is opgeslagen.", "Opgeslagen");
        return Json(uitkomst, JsonOpts);
    }

    [HttpPost("{id:int}/Dupliceren")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsPlanningSjablonen)]
    public async Task<IActionResult> Dupliceren(int id)
    {
        var (nieuwId, fout) = await _service.Dupliceer(id, UserId);
        if (fout != null) { AddMessage("danger", fout, "Fout"); return RedirectToAction(nameof(Index)); }
        AddMessage("success", "Het sjabloon is gedupliceerd. De kopie staat nog op inactief: pas ze aan en activeer ze.", "Gedupliceerd");
        return RedirectToAction(nameof(Bewerken), new { id = nieuwId });
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
        var lijst = new MvcBreadcrumbNode("Index", "SchijfSjabloonAdmin", "Schijvensjablonen") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "SchijfSjabloonAdmin", leaf) { Parent = lijst };
    }
}
