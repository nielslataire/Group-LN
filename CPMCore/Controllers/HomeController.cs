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
    private readonly IInvoiceQueryService _invoiceQueryService;
    private readonly IIncomingInvoiceService _incomingInvoiceService;
    private readonly IIssuerCompanyService _issuerCompanyService;
    private readonly cpmRunningContext _db;

    public HomeController(
        ILogger<HomeController> logger,
        IProjectService projectService,
        IClientService clientService,
        IInsuranceService insuranceService,
        IProjectVoortgangService voortgangService,
        IConstructionIssueService issueService,
        IInvoiceQueryService invoiceQueryService,
        IIncomingInvoiceService incomingInvoiceService,
        IIssuerCompanyService issuerCompanyService,
        cpmRunningContext db)
    {
        _logger = logger;
        _projectService = projectService;
        _clientService = clientService;
        _insuranceService = insuranceService;
        _voortgangService = voortgangService;
        _issueService = issueService;
        _invoiceQueryService = invoiceQueryService;
        _incomingInvoiceService = incomingInvoiceService;
        _issuerCompanyService = issuerCompanyService;
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
        else if (model.DashboardType == Models.DashboardType.CeoCfo)
            ViewData["CoachmarkPageKey"] = "Home.Dashboard.CeoCfo";
        else if (model.DashboardType == Models.DashboardType.Boekhouding)
            ViewData["CoachmarkPageKey"] = "Home.Dashboard.Boekhouding";

        // CeoCfo is portfolio-oversight, niet persoonlijk toegewezen: alle actieve
        // projecten over alle bedrijven heen, geen filter op de ingelogde gebruiker.
        var response = model.DashboardType == Models.DashboardType.CeoCfo
            ? _projectService.GetProjectsForList(0, 0)
            : _projectService.GetProjectsForList(0, 0, currentUserCode);
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

            model.ProjectSortOrder = _projectService.GetProjectOrder(userId.Value);
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

        // Voortgang voor projectleider- en CeoCfo-dashboard (werf-kaarten met
        // dezelfde fysieke/financiële voortgangsbalken op beide schermen).
        if ((model.DashboardType == Models.DashboardType.Projectleider || model.DashboardType == Models.DashboardType.CeoCfo)
            && model.Projects.Count > 0)
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

        // Facturatie-overzicht — Boekhouding en CeoCfo delen dezelfde cijfers
        // (org-breed, geen filter op bedrijf: beide rollen overzien alles).
        if (model.DashboardType == Models.DashboardType.Boekhouding || model.DashboardType == Models.DashboardType.CeoCfo)
        {
            model.OutgoingInvoiceSummary = await _invoiceQueryService.GetDashboardSummaryAsync();

            // "Actie nodig" = som van de statussen die IncomingInvoiceStatus.RequiresUserAction
            // als actie-vereist beschouwt. GetPagedAsync filtert maar op één status per call,
            // dus vier kleine, goedkope paged calls (TotalCount draagt de kost, niet de items).
            var actionStatuses = new[]
            {
                IncomingInvoiceStatus.New,
                IncomingInvoiceStatus.Enriched,
                IncomingInvoiceStatus.NeedsReview,
                IncomingInvoiceStatus.PendingApproval
            };

            var actionCount = 0;
            var attention = new List<IncomingInvoiceListItemVm>();
            foreach (var statusId in actionStatuses)
            {
                var paged = await _incomingInvoiceService.GetPagedAsync(
                    new IncomingInvoiceFilterVm { StatusId = statusId, PageSize = 5 });
                actionCount += paged.TotalCount;
                attention.AddRange(paged.Items);
            }

            model.IncomingInvoiceActionCount = actionCount;
            model.IncomingInvoiceAttention = attention
                .OrderBy(i => i.DueDate ?? DateOnly.MaxValue)
                .Take(5)
                .ToList();

            var warningsPage = await _incomingInvoiceService.GetPagedAsync(
                new IncomingInvoiceFilterVm { HasWarnings = true, PageSize = 1 });
            model.IncomingInvoiceWarningCount = warningsPage.TotalCount;
        }

        // CeoCfo: bedrijfsnamen voor de badge op elke kaart in de multi-company werven-grid.
        if (model.DashboardType == Models.DashboardType.CeoCfo)
        {
            var issuers = await _issuerCompanyService.GetAllAsync();
            model.IssuerCompanyNames = issuers.ToDictionary(i => i.Id, i => i.Name);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReorderProjects(List<int> projectIds)
    {
        var userId = User.GetCpmUserId();
        if (!userId.HasValue)
            return Json(new { success = false, error = "Niet aangemeld." });

        var response = _projectService.SetProjectOrder(userId.Value, projectIds ?? new List<int>());
        return Json(new { success = response.Success, message = response.Messages.LastOrDefault()?.Message });
    }


    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}