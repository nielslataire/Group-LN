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

    [HttpPost("{id:int}/Dupliceren")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsTrajectSjablonen)]
    public async Task<IActionResult> Dupliceren(int id)
    {
        var bron = await _service.GetById(id, includeDetails: true);
        if (bron == null) return NotFound();

        var kopie = new TrajectSjabloonBO
        {
            Id = null,
            Naam = $"{bron.Naam} (kopie)",
            ProjectType = bron.ProjectType,
            IsStandaard = false, // twee standaardsjablonen voor hetzelfde projecttype zou dubbelzinnig zijn
            IsActief = bron.IsActief,
            Omschrijving = bron.Omschrijving,
            Fases = bron.Fases.OrderBy(f => f.Volgorde).Select(f => new TrajectSjabloonFaseBO
            {
                Id = null,
                Naam = f.Naam,
                Code = f.Code,
                Volgorde = f.Volgorde,
                KleurCode = f.KleurCode,
                StandaardProjectStatusId = f.StandaardProjectStatusId,
                Mijlpalen = f.Mijlpalen.OrderBy(m => m.Volgorde).Select(m => new TrajectSjabloonMijlpaalBO
                {
                    Id = null,
                    Naam = m.Naam,
                    Code = m.Code,
                    Volgorde = m.Volgorde,
                    MijlpaalType = m.MijlpaalType,
                    Scope = m.Scope,
                    VerantwoordelijkeRol = m.VerantwoordelijkeRol,
                    DoeldatumAnkerCode = m.DoeldatumAnkerCode,
                    DoeldatumOffsetDagen = m.DoeldatumOffsetDagen,
                    IsVerplicht = m.IsVerplicht,
                    BronBinding = m.BronBinding,
                    BronParam = m.BronParam,
                    DossierKind = m.DossierKind,
                    Omschrijving = m.Omschrijving,
                    Triggers = m.Triggers.Select(t => new TrajectSjabloonMijlpaalTriggerBO
                    {
                        Id = null,
                        TriggerEvent = t.TriggerEvent,
                        TriggerActie = t.TriggerActie,
                        OffsetDagen = t.OffsetDagen,
                        ActieParametersJson = t.ActieParametersJson,
                        MagProjectWijzigen = t.MagProjectWijzigen,
                        IsActief = t.IsActief,
                        Omschrijving = t.Omschrijving
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        var saved = await _service.Upsert(kopie, UserId);
        AddMessage("success", $"'{bron.Naam}' is gedupliceerd naar '{saved.Naam}'.", "Gedupliceerd");
        return RedirectToAction(nameof(Bewerken), new { id = saved.Id });
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsTrajectSjablonen)]
    public async Task<IActionResult> Opslaan([FromForm] string payloadJson)
    {
        // De JS-kant blokkeert een lege naam al vóór de POST (zie trajectsjabloon.admin.js, submit-
        // handler op #sjabloonForm) — dit hier is enkel het vangnet voor wie dat client-side pad
        // omzeilt. Bij een falende validatie NIET naar Index redirecten: dat gooide voorheen de hele
        // sessie (fases/mijlpalen/acties, enkel in de browser opgebouwd) weg met niets dan een
        // toastmelding. Terug naar de bewerkpagina van hetzelfde sjabloon (of Nieuw voor een nog
        // niet bewaard sjabloon) is geen volledig herstel — de niet-bewaarde wijzigingen blijven wel
        // verloren — maar laat de gebruiker tenminste in de juiste context verder werken i.p.v. naar
        // de lijst gestuurd te worden.
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
            return dto?.Id is int existingId and > 0
                ? RedirectToAction(nameof(Bewerken), new { id = existingId })
                : RedirectToAction(nameof(Nieuw));
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
