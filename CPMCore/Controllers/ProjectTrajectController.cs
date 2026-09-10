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
    public async Task<IActionResult> Index(int projectId, [FromQuery] MijlpaalFilterBO filters)
    {
        var project = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectName, p.ProjectType })
            .FirstOrDefaultAsync();
        var projectName = project?.ProjectName ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId);
        SetPageHeader("bx bx-git-branch", $"{projectName} - Traject");
        ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

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
        vm.HuidigeFase = traject.Fases.OrderBy(f => f.Volgorde)
            .FirstOrDefault(f => f.Status == (int)FaseStatus.Actief)
            ?? traject.Fases.OrderBy(f => f.Volgorde).FirstOrDefault(f => f.Status != (int)FaseStatus.Afgerond);

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
    public async Task<IActionResult> Sync(int projectId)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null)
        {
            AddMessage("warning", "Er is nog geen traject voor dit project.", "Niets te synchroniseren");
            return RedirectToAction(nameof(Index), new { projectId });
        }
        var aantal = await _instantiation.SyncMissing(traject.Id, UserId);
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
    public async Task<IActionResult> MijlpaalUpsert(int projectId, [FromForm] MijlpaalUpsertBO dto)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        dto.ProjecttrajectId = traject.Id;

        if (dto.Id is int id and > 0)
            await _mijlpaal.Update(id, dto, UserId);
        else
            await _mijlpaal.Create(dto, UserId);

        AddMessage("success", "De mijlpaal is opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Index), new { projectId });
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
    public async Task<IActionResult> MijlpaalBulk(int projectId, [FromForm] MijlpaalBulkUpdateBO dto)
    {
        var traject = await _traject.GetByProject(projectId, includeDetails: false);
        if (traject == null) return NotFound();
        var n = await _mijlpaal.BulkUpdate(traject.Id, dto, UserId);
        AddMessage("success", $"{n} mijlpalen bijgewerkt.", "Bulk-bijwerking");
        return RedirectToAction(nameof(Index), new { projectId });
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
