using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Traject;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsTrajectSjablonen)]
[Route("Instellingen/Trajectsjablonen")]
public class TrajectSjabloonAdminController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly ITrajectSjabloonService _service;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public TrajectSjabloonAdminController(cpmRunningContext db, ITrajectSjabloonService service)
    {
        _db = db;
        _service = service;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetBreadcrumbs();
        SetPageHeader("bx bx-git-branch", "Trajectsjablonen", "Instellingen",
            "Beheer de standaardtrajecten met fases en mijlpalen per projecttype.");

        var sjablonen = await _service.GetAll(includeInactive: true);
        var counts = await _db.Projecttraject
            .Where(t => t.TrajectSjabloonId != null)
            .GroupBy(t => t.TrajectSjabloonId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        return View(new TrajectSjabloonListVm
        {
            Sjablonen = sjablonen,
            AantalTrajectenPerSjabloon = counts.ToDictionary(x => x.Key, x => x.Count)
        });
    }

    [HttpGet("Nieuw")]
    public IActionResult Nieuw()
    {
        SetBreadcrumbs("Nieuw sjabloon");
        SetPageHeader("bx bx-git-branch", "Nieuw trajectsjabloon", "Instellingen");
        return View("Edit", new TrajectSjabloonEditVm { Sjabloon = null });
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var sjabloon = await _service.GetById(id, includeDetails: true);
        if (sjabloon == null) return NotFound();

        SetBreadcrumbs(sjabloon.Naam);
        SetPageHeader("bx bx-git-branch", $"Sjabloon — {sjabloon.Naam}", "Instellingen");
        return View("Edit", new TrajectSjabloonEditVm { Sjabloon = sjabloon });
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsTrajectSjablonen)]
    public async Task<IActionResult> Opslaan([FromForm] string payloadJson)
    {
        TrajectSjabloonBO? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TrajectSjabloonBO>(payloadJson ?? "", JsonOpts);
        }
        catch (JsonException)
        {
            AddMessage("danger", "Ongeldige gegevens ontvangen.", "Opslaan mislukt");
            return RedirectToAction(nameof(Index));
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Naam))
        {
            AddMessage("danger", "Geef minstens een naam op voor het sjabloon.", "Opslaan mislukt");
            return RedirectToAction(nameof(Index));
        }

        var saved = await _service.Upsert(dto, UserId);
        AddMessage("success", "Het sjabloon is opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Bewerken), new { id = saved.Id });
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsTrajectSjablonen)]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var ok = await _service.Delete(id, UserId);
        AddMessage(ok ? "success" : "danger",
            ok ? "Het sjabloon is verwijderd of gedeactiveerd." : "Sjabloon niet gevonden.",
            ok ? "Verwijderd" : "Fout");
        return RedirectToAction(nameof(Index));
    }

    private void SetBreadcrumbs(string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var lijst = new MvcBreadcrumbNode("Index", "TrajectSjabloonAdmin", "Trajectsjablonen") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "TrajectSjabloonAdmin", leaf) { Parent = lijst };
    }
}
