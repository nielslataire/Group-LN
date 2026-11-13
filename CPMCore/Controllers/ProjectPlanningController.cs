using System.Text.Json;
using BOCore;
using CPMCore.Attributes;
using CPMCore.Helpers;
using CPMCore.Models.Planning;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>
/// Bouwplanning van een project: taken en fases met duur in werkdagen en voorgangers, doorgerekend met de werkkalender
/// (feestdagen, bouwverlof, verlet). De pagina stuurt elke wijziging als <see cref="PlanningCommando"/> en krijgt de herberekende
/// staat terug; de rekenlogica staat in <c>ServiceCore.Planning</c>.
/// </summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsPlanning)]
[Route("Projects/{projectId:int}/Planning")]
public class ProjectPlanningController : BaseController
{
    private static readonly JsonSerializerOptions JsonOpties = new(JsonSerializerDefaults.Web);

    private readonly cpmRunningContext _db;
    private readonly IPlanningService _planning;

    public ProjectPlanningController(cpmRunningContext db, IPlanningService planning)
    {
        _db = db;
        _planning = planning;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("~/Projects/Planning")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om de planning te bekijken.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    [Breadcrumb("Planning")]
    public async Task<IActionResult> Index(int projectId)
    {
        var projectName = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync();
        if (projectName == null) return NotFound();

        // De kruimel stopt bij de projectnaam; "Planning" is al de paginatitel (zoals Traject).
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Detail", "Projecten", projectName)
        {
            Parent = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = new MvcBreadcrumbNode("Index", "Home", "Dashboard") },
            RouteValues = new { projectid = projectId }
        };
        SetPageHeader("ph ph-chart-bar-horizontal", "Planning");
        ViewData["HelpScreenId"] = "planning-index";

        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var staat = await _planning.Laad(projectId);
        return View("IndexV2", new PlanningIndexVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            CanWrite = ps.HasWrite(PermissionCodes.ProjectsPlanning),
            CanDelete = ps.HasDelete(PermissionCodes.ProjectsPlanning),
            StaatJson = JsonSerializer.Serialize(staat, JsonOpties),
        });
    }

    /// <summary>Eén wijziging aan de planning; het antwoord bevat de volledig herberekende staat.</summary>
    [HttpPost("Commando")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> Commando(int projectId, [FromBody] PlanningCommando commando)
    {
        if (commando == null) return BadRequest();

        // Verwijderen vraagt het verwijderrecht (zoals Traject en Dossiers).
        if (commando.Type == "verwijder"
            && !HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsPlanning))
            return Forbid();

        var uitkomst = await _planning.Voer(projectId, commando, UserId);
        return Json(uitkomst, JsonOpties);
    }

    [HttpGet("Staat")]
    public async Task<IActionResult> Staat(int projectId) => Json(await _planning.Laad(projectId), JsonOpties);
}
