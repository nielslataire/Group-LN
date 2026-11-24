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
public partial class TrajectSjabloonAdminController : BaseController
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
    public async Task<IActionResult> Index(bool classic = false)
    {
        if (classic) ViewData["UseGlV2Layout"] = false;
        SetBreadcrumbs();
        SetPageHeader("bx bx-git-branch", "Trajectsjablonen", "Instellingen",
            "Beheer de standaardtrajecten met fases en mijlpalen per projecttype.");

        var sjablonen = await _service.GetAll(includeInactive: true);
        var counts = await _db.Projecttraject
            .Where(t => t.TrajectSjabloonId != null)
            .GroupBy(t => t.TrajectSjabloonId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var lijst = new TrajectSjabloonListVm
        {
            Sjablonen = sjablonen,
            AantalTrajectenPerSjabloon = counts.ToDictionary(x => x.Key, x => x.Count),
            Info = IsV2 ? await BouwInfo(sjablonen) : new()
        };
        return IsV2 ? View("IndexV2", lijst) : View(lijst);
    }

    [HttpGet("Nieuw")]
    public IActionResult Nieuw()
    {
        SetBreadcrumbs("Nieuw sjabloon");
        SetPageHeader("bx bx-git-branch", "Nieuw trajectsjabloon", "Instellingen");
        return View(IsV2 ? "EditV2" : "Edit", new TrajectSjabloonEditVm { Sjabloon = null });
    }

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Bewerken(int id)
    {
        var sjabloon = await _service.GetById(id, includeDetails: true);
        if (sjabloon == null) return NotFound();

        SetBreadcrumbs(sjabloon.Naam);
        SetPageHeader("bx bx-git-branch", $"Sjabloon — {sjabloon.Naam}", "Instellingen");

        return View(IsV2 ? "EditV2" : "Edit", new TrajectSjabloonEditVm { Sjabloon = sjabloon });
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

    /// <summary>Zet een sjabloon actief of inactief (⋯-menu van de lijst). Een inactief sjabloon wordt niet meer voorgesteld bij een nieuw project.</summary>
    [HttpPost("{id:int}/Actief")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsTrajectSjablonen)]
    public async Task<IActionResult> Actief(int id, bool actief)
    {
        var s = await _db.TrajectSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null)
        {
            AddMessage("danger", "Sjabloon niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }
        s.IsActief = actief;
        s.ModifiedDate = DateTime.UtcNow;
        s.ModifiedByUserId = UserId;
        await _db.SaveChangesAsync();
        AddMessage("success", actief ? "Het sjabloon is weer actief." : "Het sjabloon is inactief en wordt niet meer voorgesteld.", actief ? "Geactiveerd" : "Inactief gezet");
        return RedirectToAction(nameof(Index));
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

    private async Task<Dictionary<int, TrajectSjabloonInfo>> BouwInfo(List<TrajectSjabloon> sjablonen)
    {
        var gebruikt = (await _db.ProjectSjabloon.AsNoTracking().Where(p => p.TrajectSjabloonId != null).Select(p => new { Id = p.TrajectSjabloonId!.Value, p.Naam }).ToListAsync())
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Select(x => x.Naam).OrderBy(n => n).ToList());
        var ids = sjablonen.Select(s => s.ModifiedByUserId ?? s.CreatedByUserId).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        var namen = ids.Count == 0 ? new Dictionary<string, string>()
            : (await _db.AspNetUsers1.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.Forename, u.Name }).ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.Forename) ? (u.Name ?? "") : u.Forename);
        var uit = new Dictionary<int, TrajectSjabloonInfo>();
        foreach (var s in sjablonen)
        {
            var mijlpalen = s.Fases.SelectMany(f => f.Mijlpalen).ToList();
            var door = s.ModifiedByUserId ?? s.CreatedByUserId;
            uit[s.Id] = new TrajectSjabloonInfo
            {
                AantalAutomatisch = mijlpalen.Count(m => (m.BronBinding ?? 0) != 0),
                DuurDagen = DuurInDagen(mijlpalen),
                GebruiktIn = gebruikt.TryGetValue(s.Id, out var g) ? g : new List<string>(),
                GewijzigdOp = s.ModifiedDate ?? s.CreatedDate,
                GewijzigdDoor = door != null && namen.TryGetValue(door, out var n) ? n : null,
            };
        }
        return uit;
    }

    /// <summary>Langste keten van streefdatums: een mijlpaal telt vanaf zijn anker (+ dagen); zonder (geldig) anker vanaf de projectstart. Draait een keten rond, dan stopt die daar.</summary>
    private static int DuurInDagen(List<TrajectSjabloonMijlpaal> mijlpalen)
    {
        var perCode = mijlpalen.Where(m => !string.IsNullOrEmpty(m.Code)).GroupBy(m => m.Code).ToDictionary(g => g.Key, g => g.First());
        var memo = new Dictionary<string, int>();
        int Dag(TrajectSjabloonMijlpaal m, HashSet<string> pad)
        {
            if (!string.IsNullOrEmpty(m.Code) && memo.TryGetValue(m.Code, out var bekend)) return bekend;
            var basis = 0;
            if (!string.IsNullOrEmpty(m.DoeldatumAnkerCode) && perCode.TryGetValue(m.DoeldatumAnkerCode, out var anker) && pad.Add(m.Code ?? ""))
                basis = Dag(anker, pad);
            var v = basis + (m.DoeldatumOffsetDagen ?? 0);
            if (!string.IsNullOrEmpty(m.Code)) memo[m.Code] = v;
            return v;
        }
        return mijlpalen.Count == 0 ? 0 : mijlpalen.Max(m => Dag(m, new HashSet<string>()));
    }

    private void SetBreadcrumbs(string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var sjablonen = new MvcBreadcrumbNode("Index", "ProjectSjabloonAdmin", "Sjablonen") { Parent = instellingen };
        var lijst = new MvcBreadcrumbNode("Index", "TrajectSjabloonAdmin", "Traject") { Parent = sjablonen };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Bewerken", "TrajectSjabloonAdmin", leaf) { Parent = lijst };
    }
}
