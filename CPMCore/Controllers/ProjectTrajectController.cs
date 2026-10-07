using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Traject;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsTraject)]
[Route("Projects/{projectId:int}/Traject")]
public class ProjectTrajectController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IProjecttrajectService _traject;
    private readonly IMijlpaalService _mijlpaal;
    private readonly ITrajectSjabloonService _sjablonen;
    private readonly ITrajectInstantiationService _instantiation;

    public ProjectTrajectController(
        cpmRunningContext db,
        IProjecttrajectService traject,
        IMijlpaalService mijlpaal,
        ITrajectSjabloonService sjablonen,
        ITrajectInstantiationService instantiation)
    {
        _db = db;
        _traject = traject;
        _mijlpaal = mijlpaal;
        _sjablonen = sjablonen;
        _instantiation = instantiation;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("~/Projects/Traject")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om het traject te bekijken.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int projectId, [FromQuery] MijlpaalFilterBO filters, string? tab = null, int? hl = null, int? nieuw = null)
    {
        var project = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectName, p.ProjectType })
            .FirstOrDefaultAsync();
        var projectName = project?.ProjectName ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId);
        SetPageHeader("ph ph-git-branch", $"{projectName} - Traject");
        if (UseGlV2)
        {
            // gl-v2: de kruimel stopt bij de projectnaam — "Traject" is al de paginatitel (design-handoff punt 13, regel 2).
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = new MvcBreadcrumbNode("Index", "Home", "Dashboard") },
                RouteValues = new { projectid = projectId }
            };
        }
        ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
        // Neutraliseert theme.css' html.modern.fixed .content-body{margin-top:10px} — de tabbar
        // (.gl-traject-tabrow, traject.css) breekt uit tot vlak onder de topbar en rekent zelf al
        // het volledige .inner-body-recept (border-top/margin-top/padding) na; deze extra 10px op
        // de gedeelde .content-body-ouder zat daar nog niet in verrekend.
        ViewBag.ContentBodyClass = "gl-traject-flush";

        filters ??= new MijlpaalFilterBO();
        var traject = await _traject.GetByProject(projectId, includeDetails: true);

        var vm = new TrajectIndexVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Traject = traject,
            Filter = filters
        };

        if (traject == null)
        {
            vm.Sjablonen = await _sjablonen.GetAll();
            var voorstel = await _sjablonen.GetStandaardVoorProjectType(project?.ProjectType);
            vm.VoorgesteldSjabloonId = voorstel?.Id;
            if (UseGlV2)
            {
                vm.CanWrite = HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsTraject);
                return View("IndexV2", vm);
            }
            return View(vm);
        }

        var mijlpalen = await _mijlpaal.Search(traject.Id, filters);
        vm.Mijlpalen = mijlpalen;
        vm.Fases = traject.Fases.OrderBy(f => f.Volgorde).ToList();
        vm.ProjectUnits = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == projectId)
            .OrderBy(u => u.Name)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var alle = traject.Mijlpalen;
        vm.AantalBereikt = alle.Count(m => m.Status == (int)MijlpaalStatus.Bereikt);
        vm.AantalAchterstallig = alle.Count(m =>
            m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing
            && (m.Doeldatum ?? m.DoeldatumBerekend) is DateOnly d && d < today);
        vm.AantalBinnen14Dagen = alle.Count(m =>
            m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing
            && (m.Doeldatum ?? m.DoeldatumBerekend) is DateOnly d && d >= today && d <= today.AddDays(14));
        // Punten (ConstructionIssue) zichtbaar als gekoppelde component op de mijlpaal die ze
        // helpen bereiken — enkel taken die zowel aan een mijlpaal als aan een punt hangen.
        var mijlpaalIds = mijlpalen.Select(m => m.Id).ToList();
        vm.PuntenPerMijlpaal = mijlpaalIds.Count > 0
            ? await _db.ProjectTaak.AsNoTracking()
                .Where(t => t.MijlpaalId.HasValue && mijlpaalIds.Contains(t.MijlpaalId.Value) && t.ConstructionIssueId.HasValue)
                .GroupBy(t => t.MijlpaalId!.Value)
                .Select(g => new { MijlpaalId = g.Key, Aantal = g.Select(t => t.ConstructionIssueId).Distinct().Count() })
                .ToDictionaryAsync(g => g.MijlpaalId, g => g.Aantal)
            : new Dictionary<int, int>();

        // Triggers apart geladen — MijlpaalService.Search() include't ze niet (zie TrajectIndexVm).
        vm.TriggersPerMijlpaal = mijlpaalIds.Count > 0
            ? (await _db.MijlpaalTrigger.AsNoTracking()
                .Where(t => mijlpaalIds.Contains(t.MijlpaalId) && t.IsActief)
                .ToListAsync())
                .GroupBy(t => t.MijlpaalId)
                .ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<int, List<DALCore.Models.MijlpaalTrigger>>();

        vm.HuidigeFase = traject.Fases.OrderBy(f => f.Volgorde)
            .FirstOrDefault(f => f.Status == (int)FaseStatus.Actief)
            ?? traject.Fases.OrderBy(f => f.Volgorde).FirstOrDefault(f => f.Status != (int)FaseStatus.Afgerond);

        if (UseGlV2)
        {
            vm.StartTab = tab;
            vm.HighlightMijlpaalId = hl;
            await VulV2Data(vm, traject);
            ViewBag.OpenNieuweInFase = nieuw;
            return View("IndexV2", vm);
        }
        return View(vm);
    }

    [HttpPost("Aanmaken")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> Aanmaken(int projectId, int? sjabloonId, DateOnly? startdatum)
    {
        try
        {
            await _instantiation.Instantiate(new TrajectInstantiatieBO
            {
                ProjectId = projectId,
                TrajectSjabloonId = sjabloonId,
                Startdatum = startdatum
            }, UserId);
            AddMessage("success", "Het traject is aangemaakt op basis van het sjabloon.", "Traject aangemaakt");
        }
        catch (InvalidOperationException ex)
        {
            AddMessage("danger", ex.Message, "Kon traject niet aanmaken");
        }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Sync")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> Sync(int projectId, [FromForm] List<string>? keys)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null)
        {
            AddMessage("warning", "Er is nog geen traject voor dit project.", "Niets te synchroniseren");
            return RedirectToAction(nameof(Index), new { projectId });
        }
        // gl-v2 (30f) stuurt de gekozen wijzigingen mee; de legacy knop synchroniseert alles wat ontbreekt.
        var aantal = keys is { Count: > 0 }
            ? await _instantiation.ApplySync(traject.Id, keys, UserId)
            : await _instantiation.SyncMissing(traject.Id, UserId);
        AddMessage("success", aantal == 0
            ? "Het traject was al in lijn met het sjabloon."
            : $"{aantal} mijlpaal/mijlpalen toegevoegd of bijgewerkt vanuit het sjabloon.", "Synchronisatie voltooid");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> Verwijderen(int projectId)
    {
        await _traject.Delete(projectId, UserId);
        AddMessage("success", "Het traject is verwijderd.", "Traject verwijderd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpGet("Mijlpaal/{id:int}/Data")]
    public async Task<IActionResult> MijlpaalData(int projectId, int id)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        var m = await _mijlpaal.GetById(traject.Id, id);
        if (m == null) return NotFound();
        return Json(new
        {
            m.Id,
            m.ProjecttrajectId,
            m.ProjecttrajectFaseId,
            m.UnitId,
            m.Naam,
            m.Code,
            m.Volgorde,
            m.MijlpaalType,
            m.Status,
            Doeldatum = m.Doeldatum?.ToString("yyyy-MM-dd"),
            DoeldatumBerekend = m.DoeldatumBerekend?.ToString("yyyy-MM-dd"),
            WerkelijkeDatum = m.WerkelijkeDatum?.ToString("yyyy-MM-dd"),
            m.VerantwoordelijkeRol,
            m.VerantwoordelijkeUserId,
            m.IsVerplicht,
            m.Opmerking
        });
    }

    [HttpPost("Mijlpaal")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalUpsert(int projectId, [FromForm] MijlpaalUpsertBO dto,
        string? geldtVoor = null, bool opslaanEnNieuw = false, string? terugTab = null)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        dto.ProjecttrajectId = traject.Id;

        // Status ↔ "Bereikt op": bereikt zonder datum = vandaag, nog te doen = geen datum (zoals ChangeStatus).
        if (dto.Status == (int)MijlpaalStatus.Bereikt) dto.WerkelijkeDatum ??= DateOnly.FromDateTime(DateTime.Today);
        else if (dto.Status == (int)MijlpaalStatus.Open || dto.Status == (int)MijlpaalStatus.Bezig) dto.WerkelijkeDatum = null;

        int? opgeslagenId = null;
        if (dto.Id is int id and > 0)
        {
            await _mijlpaal.Update(id, dto, UserId);
            opgeslagenId = id;
        }
        else
        {
            // "Geldt voor" (design 30e): projectniveau · één mijlpaal met een eigen datum per eenheid · één eenheid.
            if (geldtVoor == "alle")
            {
                var unitIds = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => u.Id).ToListAsync();
                foreach (var uid in unitIds)
                {
                    dto.UnitId = uid;
                    var m = await _mijlpaal.Create(dto, UserId);
                    opgeslagenId ??= m.Id;
                }
                if (unitIds.Count == 0)
                {
                    AddMessage("warning", "Dit project heeft nog geen eenheden — de mijlpaal werd niet aangemaakt.", "Geen eenheden");
                    return RedirectToAction(nameof(Index), new { projectId, tab = terugTab });
                }
            }
            else
            {
                if (geldtVoor != "unit") dto.UnitId = null;
                opgeslagenId = (await _mijlpaal.Create(dto, UserId)).Id;
            }
        }

        AddMessage("success", "De mijlpaal is opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Index), new
        {
            projectId,
            tab = terugTab,
            hl = opgeslagenId,
            nieuw = opslaanEnNieuw ? (dto.ProjecttrajectFaseId ?? 0) : (int?)null
        });
    }

    [HttpPost("Mijlpaal/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalStatusWijzigen(int projectId, [FromForm] MijlpaalStatusChangeBO dto)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        var ok = await _mijlpaal.ChangeStatus(traject.Id, dto, UserId);
        if (!ok) AddMessage("danger", "Mijlpaal niet gevonden.", "Fout");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Mijlpaal/Bulk")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalBulk(int projectId, [FromForm] MijlpaalBulkUpdateBO dto, string? terugTab = null)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        var n = await _mijlpaal.BulkUpdate(traject.Id, dto, UserId);
        AddMessage("success", $"{n} mijlpalen bijgewerkt.", "Bulk-bijwerking");
        return RedirectToAction(nameof(Index), new { projectId, tab = terugTab ?? "mijlpalen" });
    }

    [HttpPost("Mijlpaal/{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalVerwijderen(int projectId, int id)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        await _mijlpaal.Delete(traject.Id, id, UserId);
        AddMessage("success", "De mijlpaal is verwijderd.", "Verwijderd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    // ── gl-v2 (design-handoff 30) ───────────────────────────────────────────────────────────────────

    private bool UseGlV2 => ViewData["UseGlV2Layout"] as bool? == true;

    /// <summary>Vult wat enkel de gl-v2 pagina nodig heeft: rechten, sync-teller per fase en het dossier achter een achterstallige mijlpaal.</summary>
    private async Task VulV2Data(TrajectIndexVm vm, Projecttraject traject)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        vm.CanWrite = ps.HasWrite(PermissionCodes.ProjectsTraject);
        vm.CanDelete = ps.HasDelete(PermissionCodes.ProjectsTraject);

        var preview = await _instantiation.PreviewSync(traject.Id);
        if (preview != null)
        {
            vm.SyncOpenstaand = preview.Items.Count(i => i.Toepasbaar);
            foreach (var g in preview.Items.Where(i => i.Soort == TrajectSyncSoort.Nieuw).GroupBy(i => i.Fase))
                vm.SyncNieuwPerFaseNaam[g.Key] = g.Count();
        }

        var userIds = traject.Mijlpalen.Select(m => m.VerantwoordelijkeUserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Voornaam, u.Familienaam }).ToListAsync();
            foreach (var u in users)
            {
                var naam = $"{u.Voornaam} {u.Familienaam}".Trim();
                var init = $"{(string.IsNullOrEmpty(u.Voornaam) ? "" : u.Voornaam[..1])}{(string.IsNullOrEmpty(u.Familienaam) ? "" : u.Familienaam[..1])}".ToUpperInvariant();
                vm.Gebruikers[u.UserId] = (naam, init);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var teLaat = traject.Mijlpalen
            .Where(m => m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing
                        && (m.Doeldatum ?? m.DoeldatumBerekend) is DateOnly d && d < today)
            .Select(m => m.Id).ToList();
        if (teLaat.Count > 0)
        {
            var koppelingen = await _db.ProjectDossierMijlpaal.AsNoTracking()
                .Where(k => teLaat.Contains(k.MijlpaalId) && k.ProjectDossier.Status != (int)DossierStatus.Afgehandeld && k.ProjectDossier.Status != (int)DossierStatus.Geannuleerd)
                .Include(k => k.ProjectDossier).ThenInclude(d => d.NutsAansluiting)
                .Include(k => k.ProjectDossier).ThenInclude(d => d.Unit)
                .Include(k => k.ProjectDossier).ThenInclude(d => d.Substappen)
                .ToListAsync();
            foreach (var k in koppelingen)
                vm.DossierPerMijlpaal.TryAdd(k.MijlpaalId, k.ProjectDossier);
        }
    }

    [HttpGet("Mijlpaal/Modal")]
    public async Task<IActionResult> MijlpaalModal(int projectId, int? id, int? faseId, int? unitId)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: true);
        if (traject == null) return NotFound();
        var vm = new MijlpaalModalV2Vm
        {
            ProjectId = projectId,
            FaseId = faseId,
            UnitId = unitId,
            Fases = traject.Fases.OrderBy(f => f.Volgorde).ToList(),
            Mijlpalen = traject.Mijlpalen.OrderBy(m => m.Volgorde).ToList(),
            ActieveFaseId = traject.Fases.OrderBy(f => f.Volgorde).FirstOrDefault(f => f.Status == (int)FaseStatus.Actief)?.Id,
            Units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync()
        };
        if (id is int mid and > 0)
        {
            vm.Bestaand = traject.Mijlpalen.FirstOrDefault(m => m.Id == mid);
            if (vm.Bestaand == null) return NotFound();
            vm.FaseId = vm.Bestaand.ProjecttrajectFaseId;
            vm.UnitId = vm.Bestaand.UnitId;
        }
        else vm.FaseId ??= vm.ActieveFaseId;
        return PartialView("Modals/_ModalMijlpaalV2", vm);
    }

    /// <summary>Snelle acties zonder modal (design 30b/30c): bolletje aanvinken, datum in de popover van een cel.
    /// Antwoordt met JSON; de pagina laadt daarna opnieuw (zelfde tab).</summary>
    [HttpPost("Mijlpaal/{id:int}/Snel")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalSnel(int projectId, int id, bool? bereikt, DateOnly? bereiktOp, DateOnly? streefdatum)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        if (await _mijlpaal.GetById(traject.Id, id) == null) return NotFound();

        if (streefdatum.HasValue)
            await _mijlpaal.BulkUpdate(traject.Id, new MijlpaalBulkUpdateBO { MijlpaalIds = new List<int> { id }, Doeldatum = streefdatum }, UserId);
        if (bereikt == true)
            await _mijlpaal.ChangeStatus(traject.Id, new MijlpaalStatusChangeBO { MijlpaalId = id, NieuweStatus = (int)MijlpaalStatus.Bereikt, WerkelijkeDatum = bereiktOp ?? DateOnly.FromDateTime(DateTime.Today) }, UserId);
        else if (bereikt == false)
            await _mijlpaal.ChangeStatus(traject.Id, new MijlpaalStatusChangeBO { MijlpaalId = id, NieuweStatus = (int)MijlpaalStatus.Open }, UserId);
        return Json(new { ok = true });
    }

    [HttpPost("Mijlpaal/BulkVerwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsTraject)]
    public async Task<IActionResult> MijlpaalBulkVerwijderen(int projectId, [FromForm] List<int> mijlpaalIds)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        int n = 0;
        foreach (var id in (mijlpaalIds ?? new List<int>()).Distinct())
            if (await _mijlpaal.Delete(traject.Id, id, UserId)) n++;
        AddMessage("success", n == 1 ? "1 mijlpaal verwijderd." : $"{n} mijlpalen verwijderd.", "Verwijderd");
        return RedirectToAction(nameof(Index), new { projectId, tab = "mijlpalen" });
    }

    /// <summary>Design 30f: eerst tonen wat een sync verandert, dan kiezen.</summary>
    [HttpGet("Sync/Voorbeeld")]
    public async Task<IActionResult> SyncVoorbeeld(int projectId)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        var preview = await _instantiation.PreviewSync(traject.Id);
        if (preview == null) return NotFound();
        ViewBag.ProjectId = projectId;
        return PartialView("Modals/_ModalSyncV2", preview);
    }

    private void SetBreadcrumbs(string projectName, int projectId)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
        var detail = new MvcBreadcrumbNode("Detail", "Projecten", projectName)
        {
            Parent = projecten,
            RouteValues = new { projectid = projectId }
        };
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Index", "ProjectTraject", "Traject")
        {
            Parent = detail,
            RouteValues = new { projectId }
        };
    }
}
