using BOCore;
using CPMCore.Helpers;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

/// <summary>Portfolio-brede mijlpalen/deadlines-overzicht over alle zichtbare projecten.</summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.PortfolioDeadlines)]
[Route("Deadlines")]
public class DeadlinesController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IMijlpaalService _mijlpalen;
    private readonly IProjectService _projectService;
    private readonly CPMCore.Services.MijnTaken.MijnTakenAssistent _assistent;

    public DeadlinesController(cpmRunningContext db, IMijlpaalService mijlpalen, IProjectService projectService, CPMCore.Services.MijnTaken.MijnTakenAssistent assistent)
    {
        _db = db;
        _mijlpalen = mijlpalen;
        _projectService = projectService;
        _assistent = assistent;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] MijlpaalFilterBO filters, bool classic = false)
    {
        SetPageHeader("bx bx-calendar-check", "Deadlines");
        if (classic) ViewData["UseGlV2Layout"] = false;

        // gl-v2 (design-handoff punt 1b): mijlpalen per project met voortgang; ?classic=true toont de oude tabel.
        if (ViewData["UseGlV2Layout"] as bool? == true)
        {
            var c = await _assistent.Context(User);
            if (c == null) return Forbid();
            ViewData["HelpScreenId"] = "deadlines-overzicht";
            return View("IndexV2", await _assistent.DeadlinesOverzicht(c));
        }
        filters ??= new MijlpaalFilterBO();

        var currentUserCode = User.GetCpmUserCode() ?? string.Empty;
        var response = _projectService.GetProjectsForList(0, 0, currentUserCode);
        var projectIds = response.Success ? response.Values.Select(p => p.Id).ToList() : new List<int>();

        var mijlpalen = projectIds.Count > 0
            ? await _mijlpalen.SearchPortfolio(projectIds, filters)
            : new List<Mijlpaal>();

        var vm = new Models.Traject.DeadlinesIndexVm
        {
            Filter = filters,
            Mijlpalen = mijlpalen,
            Projecten = await _db.Project.AsNoTracking()
                .Where(p => projectIds.Contains(p.ProjectId))
                .Select(p => new IdNameBO { ID = p.ProjectId, Display = p.ProjectName })
                .OrderBy(p => p.Display).ToListAsync()
        };
        return View(vm);
    }
}
