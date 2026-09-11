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
[CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsDossiers)]
[Route("Projects/{projectId:int}/Dossiers")]
public class ProjectDossiersController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IProjectDossierService _dossiers;
    private readonly INutsAansluitingService _nuts;

    public ProjectDossiersController(cpmRunningContext db, IProjectDossierService dossiers, INutsAansluitingService nuts)
    {
        _db = db;
        _dossiers = dossiers;
        _nuts = nuts;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("~/Projects/Dossiers")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om dossiers te bekijken.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int projectId, [FromQuery] DossierFilterBO filters)
    {
        var projectName = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync()
            ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId);
        SetPageHeader("bx bx-folder-open", $"{projectName} - Dossiers");
        ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

        filters ??= new DossierFilterBO();
        var vm = new DossierIndexVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Filter = filters,
            Dossiers = await _dossiers.Search(projectId, filters),
            ProjectUnits = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync()
        };
        return View(vm);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int projectId, int id)
    {
        var dossier = await _dossiers.GetById(projectId, id);
        if (dossier == null) return NotFound();

        var projectName = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync()
            ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId, dossier.Titel);
        SetPageHeader("bx bx-folder-open", dossier.Titel, projectName);

        var mijlpaalIds = await _db.ProjectDossierMijlpaal.Where(x => x.ProjectDossierId == id).Select(x => x.MijlpaalId).ToListAsync();
        var gekoppeld = await _db.Mijlpaal.Where(m => mijlpaalIds.Contains(m.Id)).ToListAsync();

        int? projecttrajectId = await _db.Projecttraject.Where(t => t.ProjectId == projectId).Select(t => t.Id).FirstOrDefaultAsync();
        var beschikbaar = projecttrajectId is int ptid
            ? await _db.Mijlpaal.Where(m => m.ProjecttrajectId == ptid && !mijlpaalIds.Contains(m.Id))
                .OrderBy(m => m.Volgorde).ToListAsync()
            : new List<Mijlpaal>();

        var vm = new DossierDetailsVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Dossier = dossier,
            GekoppeldeMijlpalen = gekoppeld,
            BeschikbareMijlpalen = beschikbaar
        };

        if (dossier.DossierKind == (int)DossierKind.NutsAansluiting)
            vm.Nuts = await _nuts.GetById(projectId, id);
        vm.ProjectUnits = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync();

        return View(vm);
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Opslaan(int projectId, [FromForm] DossierUpsertBO dto)
    {
        dto.ProjectId = projectId;
        if (dto.Id is int id and > 0)
            await _dossiers.Update(id, dto, UserId);
        else
            await _dossiers.Create(dto, UserId);

        AddMessage("success", "Het dossier is opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Nuts/Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsOpslaan(int projectId, [FromForm] NutsAansluitingUpsertBO dto)
    {
        dto.ProjectId = projectId;
        ProjectNutsAansluiting saved;
        if (dto.Id is int id and > 0)
            saved = await _nuts.Update(id, dto, UserId) ?? throw new InvalidOperationException("Nutsaansluiting niet gevonden.");
        else
            saved = await _nuts.Create(dto, UserId);

        AddMessage("success", "Het nutsaansluitingsdossier is opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Details), new { projectId, id = saved.ProjectDossierId });
    }

    [HttpGet("Units/{unitId:int}/Meterdata")]
    public async Task<IActionResult> UnitMeterdata(int projectId, int unitId)
    {
        var (eanGas, eanElek, watermeter) = await _nuts.GetUnitMeterData(unitId);
        return Json(new { eanGas, eanElek, watermeter });
    }

    [HttpPost("{id:int}/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Status(int projectId, int id, int nieuweStatus, string? opmerking)
    {
        var ok = await _dossiers.ChangeStatus(projectId, id, nieuweStatus, UserId, opmerking);
        if (!ok) AddMessage("danger", "Dossier niet gevonden.", "Fout");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Substap/{substapId:int}/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> SubstapStatus(int projectId, int id, int substapId, int nieuweStatus, DateOnly? datum)
    {
        await _dossiers.ChangeSubstapStatus(id, substapId, nieuweStatus, datum, UserId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Gebeurtenis")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Gebeurtenis(int projectId, int id, [FromForm] DossierGebeurtenisBO dto)
    {
        dto.ProjectDossierId = id;
        await _dossiers.AddGebeurtenis(dto, UserId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/KoppelMijlpaal")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> KoppelMijlpaal(int projectId, int id, int mijlpaalId)
    {
        await _dossiers.LinkMijlpaal(id, mijlpaalId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/OntkoppelMijlpaal")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> OntkoppelMijlpaal(int projectId, int id, int mijlpaalId)
    {
        await _dossiers.UnlinkMijlpaal(id, mijlpaalId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Verwijderen(int projectId, int id)
    {
        await _dossiers.Delete(projectId, id, UserId);
        AddMessage("success", "Het dossier is verwijderd.", "Verwijderd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    private void SetBreadcrumbs(string projectName, int projectId, string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
        var detail = new MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten, RouteValues = new { projectid = projectId } };
        var lijst = new MvcBreadcrumbNode("Index", "ProjectDossiers", "Dossiers") { Parent = detail, RouteValues = new { projectId } };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Details", "ProjectDossiers", leaf) { Parent = lijst, RouteValues = new { projectId } };
    }
}
