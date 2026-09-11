using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.MijnTaken)]
[Route("MijnTaken")]
public class MijnTakenController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IProjectTaakService _taken;
    private readonly IProjectService _projectService;

    public MijnTakenController(cpmRunningContext db, IProjectTaakService taken, IProjectService projectService)
    {
        _db = db;
        _taken = taken;
        _projectService = projectService;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TaakFilterBO filters)
    {
        SetPageHeader("bx bx-task", "Mijn taken");
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var rollen = await BepaalRollen(userId);
        var projectIds = await ZichtbareProjectIds();

        filters ??= new TaakFilterBO();
        var taken = await _taken.SearchMijnTaken(userId, rollen, projectIds, filters);

        var vm = new Models.Traject.MijnTakenIndexVm
        {
            Filter = filters,
            Taken = taken,
            Projecten = await _db.Project.AsNoTracking()
                .Where(p => projectIds.Contains(p.ProjectId))
                .Select(p => new IdNameBO { ID = p.ProjectId, Display = p.ProjectName })
                .OrderBy(p => p.Display).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Opslaan([FromForm] TaakUpsertBO dto, string? returnUrl = null)
    {
        if (dto.Id is int id and > 0)
            await _taken.Update(id, dto, UserId);
        else
        {
            dto.ToegewezenAanUserId ??= UserId;
            await _taken.Create(dto, UserId);
        }
        AddMessage("success", "De taak is opgeslagen.", "Opgeslagen");
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Status(int id, int nieuweStatus)
    {
        await _taken.ChangeStatus(id, nieuweStatus, UserId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Toewijzen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Toewijzen(int id, string? toegewezenAanUserId, int? toegewezenAanRol)
    {
        await _taken.Reassign(id, toegewezenAanUserId, toegewezenAanRol, UserId);
        AddMessage("success", "De taak is toegewezen.", "Opgeslagen");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Verwijderen(int id)
    {
        await _taken.Delete(id, UserId);
        AddMessage("success", "De taak is verwijderd.", "Verwijderd");
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<int>> BepaalRollen(string userId)
    {
        var dashboardType = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == userId).Select(u => u.DashboardType).FirstOrDefaultAsync();

        InterneRol? rol = dashboardType.HasValue ? (DashboardType)dashboardType.Value switch
        {
            DashboardType.Projectleider => InterneRol.Projectleider,
            DashboardType.CeoCfo => InterneRol.CeoCfo,
            DashboardType.Boekhouding => InterneRol.Boekhouder,
            _ => (InterneRol?)null
        } : null;

        return rol.HasValue ? new List<int> { (int)rol.Value } : new List<int>();
    }

    private async Task<List<int>> ZichtbareProjectIds()
    {
        var currentUserCode = User.GetCpmUserCode() ?? string.Empty;
        var response = _projectService.GetProjectsForList(0, 0, currentUserCode);
        return response.Success ? response.Values.Select(p => p.Id).ToList() : new List<int>();
    }
}
