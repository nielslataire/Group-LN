using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using System.Globalization;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Dashboard)]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProjectService _projectService;
    private readonly IClientService _clientService;
    private readonly IInsuranceService _insuranceService;
    private readonly IProjectVoortgangService _voortgangService;
    private readonly IConstructionIssueService _issueService;
    private readonly cpmRunningContext _db;

    public HomeController(ILogger<HomeController> logger, IProjectService projectService, IClientService clientService, IInsuranceService insuranceService, IProjectVoortgangService voortgangService, IConstructionIssueService issueService, cpmRunningContext db)
    {
        _logger = logger;
        _projectService = projectService;
        _clientService = clientService;
        _insuranceService = insuranceService;
        _voortgangService = voortgangService;
        _issueService = issueService;
        _db = db;
    }

    [DefaultBreadcrumb("Dashboard")]
    public async Task<IActionResult> Index()
    {
        SetPageHeader("bx bx-home-alt", "Dashboard");

        var model = new Models.Home.HomeModel();
        var currentUserCode = User.GetCpmUserCode() ?? string.Empty;

        var userId = User.GetCpmUserId();
        if (userId.HasValue)
        {
            var rawDashboardType = _db.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => u.DashboardType)
                .FirstOrDefault();
            if (rawDashboardType.HasValue)
                model.DashboardType = (Models.DashboardType)rawDashboardType.Value;
        }

        if (model.DashboardType == Models.DashboardType.Projectleider)
            ViewData["CoachmarkPageKey"] = "Home.Dashboard.Projectleider";

        var response = _projectService.GetProjectsForList(0, 0, currentUserCode);
        if (response.Success)
        {
            var uitvoeringStatusId = (int)ProjectStatusType.Uitvoering;
            var opgeleverdStatusId = (int)ProjectStatusType.Opgeleverd;

            model.Projects = response.Values
                .OrderBy(p => p.Status.Id == uitvoeringStatusId ? 0 : p.Status.Id == opgeleverdStatusId ? 2 : 1)
                .ThenBy(p => p.Status.Name)
                .ThenBy(p => p.Name)
                .ToList();
        }

        // Vastgezette projecten samenvoegen met de toegewezen projecten
        // (enkel projectleider-dashboard — "Mijn Werven" toont toegewezen + vastgezet).
        if (model.DashboardType == Models.DashboardType.Projectleider && userId.HasValue)
        {
            model.PinnedProjectIds = _projectService.GetPinnedProjectIds(userId.Value);
            var alreadyIn = model.Projects.Select(p => p.Id).ToHashSet();
            var toFetch = model.PinnedProjectIds.Where(id => !alreadyIn.Contains(id)).ToList();
            if (toFetch.Count > 0)
            {
                var pinnedResponse = _projectService.GetProjectsForList(ProjectIds: toFetch);
                if (pinnedResponse.Success)
                    model.Projects.AddRange(pinnedResponse.Values);
            }
        }

        var response2 = _clientService.GetClientAccountsByDateDeedofSale();
        var response3 = _projectService.GetStatuses();
        if ((response3.Success))
        {
            var uitvoeringStatusId = (int)ProjectStatusType.Uitvoering;
            var opgeleverdStatusId = (int)ProjectStatusType.Opgeleverd;

            model.Statuses = response3.Values
                .OrderBy(s => s.Id == uitvoeringStatusId ? 0 : s.Id == opgeleverdStatusId ? 2 : 1)
                .ThenBy(s => s.Name)
                .ToList();
        }
        if ((response2.Success))
            model.DeedofSaleWarnings = response2.Values;
        if (!User.IsInRole("Admin"))
        {
            var iresponse = _insuranceService.CheckInsurances(currentUserCode);
            if ((iresponse.Success))
                model.InsuranceWarnings = iresponse.Values;
        }
        else
        {
            var iresponse = _insuranceService.CheckInsurances();
            if ((iresponse.Success))
                model.InsuranceWarnings = iresponse.Values;
        }
        if (!User.IsInRole("Admin"))
        {
            var iresponse = _projectService.CheckProjectFinished(currentUserCode);
            if ((iresponse.Success))
                model.ProjectInfo = iresponse.Values;
        }
        else
        {
            var iresponse = _projectService.CheckProjectFinished();
            if ((iresponse.Success))
                model.ProjectInfo = iresponse.Values;
        }

        // Voortgang voor projectleider-dashboard
        if (model.DashboardType == Models.DashboardType.Projectleider && model.Projects.Count > 0)
        {
            var projectIds = model.Projects.Select(p => p.Id);
            model.ProjectVoortgang = _voortgangService.GetForProjects(projectIds);
        }

        // Aannemer-commentaar meldingen voor projectleider-dashboard
        if (model.DashboardType == Models.DashboardType.Projectleider && model.Projects.Count > 0)
        {
            var projectIds = model.Projects.Select(p => p.Id);
            model.ContractorCommentMeldingen = await _issueService.GetContractorCommentMeldingen(projectIds);
        }

        // KPI "Open punten" voor projectleider-dashboard
        if (model.DashboardType == Models.DashboardType.Projectleider && model.Projects.Count > 0)
        {
            var projectIds = model.Projects.Select(p => p.Id);
            model.OpenIssuesCount = await _issueService.CountOpen(projectIds);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PinProject(int projectId)
    {
        var userId = User.GetCpmUserId();
        if (!userId.HasValue)
            return Json(new { success = false, error = "Niet aangemeld." });

        var response = _projectService.PinProject(userId.Value, projectId);
        return Json(new { success = response.Success, message = response.Messages.LastOrDefault()?.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UnpinProject(int projectId)
    {
        var userId = User.GetCpmUserId();
        if (!userId.HasValue)
            return Json(new { success = false, error = "Niet aangemeld." });

        var response = _projectService.UnpinProject(userId.Value, projectId);
        return Json(new { success = response.Success, message = response.Messages.LastOrDefault()?.Message });
    }


    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}