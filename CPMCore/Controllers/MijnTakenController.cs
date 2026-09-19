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
        SetPageHeader("ph ph-list-checks", "Mijn taken");
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var rollen = await BepaalRollen(userId);
        var projectIds = await ZichtbareProjectIds();

        filters ??= new TaakFilterBO();
        var taken = await _taken.SearchMijnTaken(userId, rollen, projectIds, filters);

        var toegewezenIds = taken.Where(t => !string.IsNullOrEmpty(t.ToegewezenAanUserId))
            .Select(t => t.ToegewezenAanUserId).Distinct().ToList();
        var gebruikersNamen = await _db.Users.AsNoTracking()
            .Where(u => toegewezenIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => $"{u.Voornaam} {u.Familienaam}".Trim());

        var vm = new Models.Traject.MijnTakenIndexVm
        {
            Filter = filters,
            Taken = taken,
            GebruikersNamen = gebruikersNamen,
            Projecten = await _db.Project.AsNoTracking()
                .Where(p => projectIds.Contains(p.ProjectId))
                .Select(p => new IdNameBO { ID = p.ProjectId, Display = p.ProjectName })
                .OrderBy(p => p.Display).ToListAsync()
        };
        return View(vm);
    }

    /// <summary>Gebruikerszoeklijst voor de "Toegewezen aan"-select2 — zelfde ajax/select2-patroon
    /// als ProjectenController.GetCompanys (Netbeheerder-picker).</summary>
    [HttpPost("GetGebruikers")]
    public async Task<JsonResult> GetGebruikers(string term, bool activeOnly = false)
    {
        var q = _db.Users.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(term))
            q = q.Where(u => u.Voornaam.Contains(term) || u.Familienaam.Contains(term) || u.Email.Contains(term));

        var results = await q.OrderBy(u => u.Voornaam).ThenBy(u => u.Familienaam).Take(20)
            .Select(u => new { id = u.UserId, text = (u.Voornaam + " " + u.Familienaam).Trim() })
            .ToListAsync();
        return Json(results);
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Opslaan([FromForm] TaakUpsertBO dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            AddMessage("error", "De taak kon niet opgeslagen worden. Controleer de ingevulde velden.", "Fout");
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

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
        AddMessage("success", $"Status gezet op '{((TaakStatus)nieuweStatus).GetDisplayName()}'.", "Opgeslagen");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("StatusBulk")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.MijnTaken)]
    public async Task<IActionResult> StatusBulk(List<int> ids, int nieuweStatus)
    {
        var aantal = await _taken.ChangeStatusBulk(ids ?? new List<int>(), nieuweStatus, UserId);
        var statusLabel = ((TaakStatus)nieuweStatus).GetDisplayName();
        AddMessage(aantal > 0 ? "success" : "warning",
            aantal > 0 ? $"{aantal} taak/taken gezet op '{statusLabel}'." : "Geen taken geselecteerd.",
            "Status bijgewerkt");
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
            DashboardType.Ontwikkelaar => InterneRol.Projectontwikkelaar,
            DashboardType.Verkoper => InterneRol.Verkoper,
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
