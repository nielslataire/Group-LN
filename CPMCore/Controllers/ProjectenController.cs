using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using ServiceCore;
using ServiceCore.Budget;
using ServiceCore.Invoicing;
using BOCore.Budget;
using CPMCore.Models.Budget;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
//using CPMCore.Attributes;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;




namespace CPMCore.Controllers
{
    [Authorize]
    public class ProjectenController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly cpmRunningContext _db; // TODO: vervangen door service methoden (UnitExecutionPlan, Users, CompanyContacts)
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IProjectService _projectService;
        private readonly IUnitService _unitService;
        private readonly IClientService _clientService;
        private readonly ICompanyService _companyService;
        private readonly IActivityService _activityService;
        private readonly IInsuranceService _insuranceService;
        private readonly ICountryService _countryService;
        private readonly IPostalcodeService _postalcodeService;
        private readonly IProjectVoortgangService _voortgangService;
        private readonly DALCore.UnitOfWorkCore _uow;
        private readonly IBudgetService _budgetService;
        private readonly BudgetActivityService    _budgetActivityService;
        private readonly BouwIndexService            _bouwIndex;
        private readonly BudgetBerekeningService     _berekeningService;
        private readonly BudgetExcelService          _excelService;
        private readonly ServiceCore.Budget.BudgetFormulaService _formulaService;
        //private readonly IInvoicePdfService _pdf;         // QuestPDF
        //private readonly IUblService _ubl;
        private static readonly HashSet<string> _validImageTypes = new(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

        private static readonly HashSet<string> _validVideoTypes = new(StringComparer.OrdinalIgnoreCase)
            { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo", "video/avi" };

        public ProjectenController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment env, cpmRunningContext db, IProjectService projectService, IUnitService unitService, IClientService clientService, ICompanyService companyService, IActivityService activityService, IInsuranceService insuranceService, ICountryService countryService, IPostalcodeService postalcodeService, IProjectVoortgangService voortgangService, DALCore.UnitOfWorkCore uow, IBudgetService budgetService, BudgetActivityService budgetActivityService, BouwIndexService bouwIndex, BudgetBerekeningService berekeningService, BudgetExcelService excelService, ServiceCore.Budget.BudgetFormulaService formulaService)
        {
            _logger = logger;
            Configuration = configuration;
            _env = env;
            _db = db;
            _projectService = projectService;
            _unitService = unitService;
            _clientService = clientService;
            _companyService = companyService;
            _activityService = activityService;
            _insuranceService = insuranceService;
            _countryService = countryService;
            _postalcodeService = postalcodeService;
            _voortgangService = voortgangService;
            _uow = uow;
            _budgetService          = budgetService;
            _budgetActivityService  = budgetActivityService;
            _bouwIndex              = bouwIndex;
            _berekeningService      = berekeningService;
            _excelService           = excelService;
            _formulaService         = formulaService;
        }

        // ========== PROJECT DETAIL ==========
        [Breadcrumb("Projecten", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public IActionResult Index(bool showAll = false)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanReadProjectsForSale = _ps.HasRead(PermissionCodes.ProjectsForSale);
            ViewBag.CanWriteProject = _ps.HasWrite(PermissionCodes.ProjectsDetail);
            var model = new ShowProjectsModel();
            var service = _projectService;

            var response = service.GetProjectsForList();
            if (response.Success && response.Values is not null)
            {
                const int initialLimit = 30;
                const int batchSize = 12;

                var orderedProjects = response.Values
                    .OrderByDescending(m => m.DeliveryDate == null)
                    .ThenByDescending(m => m.DeliveryDate)
                    .ToList();

                model.InitialLimit = initialLimit;
                model.BatchSize = batchSize;
                model.TotalProjectCount = orderedProjects.Count;
                model.Projects = orderedProjects.Take(initialLimit).ToList();
                model.VisibleProjectCount = model.Projects.Count;


                var ids = model.Projects.Select(p => p.Id).ToList();
                if (ids.Count > 0)
                {
                    var salesResponse = service.GetProjectSalesData(ids);
                    if (salesResponse.Success && salesResponse.Values is not null)
                    {
                        model.SalesData = salesResponse.Values
                            .GroupBy(v => v.ProjectId)
                            .ToDictionary(g => g.Key, g => g.First());
                    }

                    model.Voortgang = _voortgangService.GetForProjects(ids);
                }
            }

            var statusResponse = service.GetStatuses();
            if (statusResponse.Success && statusResponse.Values is not null)
            {
                model.Statuses = statusResponse.Values;
            }

            ViewData["Title"] = "Projecten - Alle";
            ViewData["SubTitle"] = "Alle projecten";
            ViewData["SubTitleText"] = "Overzicht van alle projecten binnen CPM.";

            return View(model);
        }

        [HttpGet]
        public IActionResult QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(Array.Empty<object>());

            var results = _uow.Projects.GetNoTracking()
                .Where(p => p.ProjectName.Contains(q))
                .OrderBy(p => p.ProjectName)
                .Take(10)
                .Select(p => new { id = p.ProjectId, name = p.ProjectName })
                .ToList();

            return Json(results);
        }

        [HttpGet]
        [Breadcrumb("Eigen projecten", FromAction = nameof(Index))]
        public IActionResult ProjectsByUserId(string userId)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanReadProjectsForSale = _ps.HasRead(PermissionCodes.ProjectsForSale);
            var model = new ShowProjectsModel();
            var service = _projectService;

            var response = service.GetProjectsForList(0, 0, userId);
            if (response.Success && response.Values is not null)
            {
                var orderedProjects = response.Values
                    .OrderByDescending(m => m.DeliveryDate == null)
                    .ThenByDescending(m => m.DeliveryDate)
                    .ToList();

                model.Projects = orderedProjects;

                model.TotalProjectCount = model.Projects.Count;
                model.VisibleProjectCount = model.Projects.Count;
                model.InitialLimit = model.Projects.Count;
                model.BatchSize = 0;

                var ids = model.Projects.Select(p => p.Id).ToList();
                if (ids.Count > 0)
                {
                    var salesResponse = service.GetProjectSalesData(ids);
                    if (salesResponse.Success && salesResponse.Values is not null)
                    {
                        model.SalesData = salesResponse.Values
                            .GroupBy(v => v.ProjectId)
                            .ToDictionary(g => g.Key, g => g.First());
                    }

                    model.Voortgang = _voortgangService.GetForProjects(ids);
                }
            }

            var statusResponse = service.GetStatuses();
            if (statusResponse.Success && statusResponse.Values is not null)
            {
                model.Statuses = statusResponse.Values;
            }

            ViewData["Title"] = "Projecten - Alle";
            ViewData["SubTitle"] = "Alle projecten";
            ViewData["SubTitleText"] = "Overzicht van alle projecten binnen CPM.";

            return View("Index", model);
        }

        [HttpGet]
        public IActionResult LoadMoreProjects(int skip, int take = 3)
        {
            if (skip < 0)
            {
                skip = 0;
            }

            if (take <= 0)
            {
                take = 3;
            }

            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanReadProjectsForSale = _ps.HasRead(PermissionCodes.ProjectsForSale);
            var service = _projectService;

            var response = service.GetProjectsForList();
            if (!response.Success || response.Values is null)
            {
                return Content(string.Empty);
            }

            var orderedProjects = response.Values
                .OrderByDescending(m => m.DeliveryDate == null)
                .ThenByDescending(m => m.DeliveryDate)
                .ToList();

            var projects = orderedProjects.Skip(skip).Take(take).ToList();
            if (projects.Count == 0)
            {
                return Content(string.Empty);
            }

            var statusResponse = service.GetStatuses();
            var statuses = statusResponse.Success && statusResponse.Values is not null
                ? statusResponse.Values
                : new List<ProjectStatusBO>();

            Dictionary<int, ProjectSalesDataBO> salesData = new();
            var ids = projects.Select(p => p.Id).ToList();
            if (ids.Count > 0)
            {
                var salesResponse = service.GetProjectSalesData(ids);
                if (salesResponse.Success && salesResponse.Values is not null)
                {
                    salesData = salesResponse.Values
                        .GroupBy(v => v.ProjectId)
                        .ToDictionary(g => g.Key, g => g.First());
                }
            }

            var model = new ProjectGridRenderModel
            {
                Projects = projects,
                Statuses = statuses,
                SalesData = salesData,
                Voortgang = _voortgangService.GetForProjects(ids)
            };

            return PartialView("_ProjectGridItems", model);
        }

        [HttpGet]
        [Breadcrumb("Project toevoegen", FromAction = nameof(Index))]
        public IActionResult Toevoegen()
        {
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = string.IsNullOrEmpty(referrer)
                ? Url.Action("Index", "Projecten")
                : referrer;

            var model = new ProjectModel();
            model.Project.Postalcode.Country.CountryId = 19;
            model.Project.Postalcode.Country.ISOCode = "BE";

            FillInAddSelectLists(model);
            FillInAvailableUsers(model);

            ViewData["Title"] = "Project toevoegen";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Project toevoegen", FromAction = nameof(Index))]
        public async Task<IActionResult> Toevoegen(ProjectModel model)
        {
            if (!ModelState.IsValid)
            {
                FillInAddSelectLists(model);
                FillInAvailableUsers(model);
                return View(model);
            }

            model.Project.Postalcode.Country.CountryId = model.SelectedCountry;
            model.Project.Postalcode.PostcodeId = model.SelectedPostalcode;
            model.Project.Status.Id = model.Project.Status.Id == 0 ? 1 : model.Project.Status.Id;
            model.Project.Slug = GetSlugForPostcodeId(model.SelectedPostalcode, model.Project.Name ?? string.Empty);

            // Routeberekening voor coördinatieproject
            if (model.Project.CoordinationIssuerCompanyId.HasValue && model.SelectedPostalcode > 0)
            {
                var (distKm, durSec) = await _projectService.CalculateRouteAsync(
                    model.Project.CoordinationIssuerCompanyId.Value,
                    model.SelectedPostalcode);
                if (distKm.HasValue)
                {
                    model.Project.ProjectDistanceKm    = distKm;
                    model.Project.RouteDurationSeconds = durSec;
                }
            }

            if (model.StandardFotoUpload != null && model.StandardFotoUpload.Length > 0)
            {
                var stdFilename = await ProcessStandardFotoUploadAsync(model.StandardFotoUpload);
                if (stdFilename != null)
                    model.Project.StandardFotoName = stdFilename;
                else
                    AddMessage("warning", "Standaard foto kon niet worden opgeslagen.", "Waarschuwing");
            }

            var service = _projectService;
            var response = service.InsertUpdate(model.Project);

            if (response.Success)
            {
                var newProjectId = model.Project.Id;

                // Sla contract schijven op
                if (model.Project.IsCoordinationProject && model.ContractSlices?.Count > 0)
                    service.SaveContractSlices(newProjectId, model.ContractSlices.Select(s => new ProjectContractSliceBO
                    {
                        Description = s.Description,
                        Percentage = s.Percentage
                    }).ToList());

                // Sla uurtarieven op
                if (model.Project.IsCoordinationProject && model.HourlyRates?.Count > 0)
                    service.SaveProjectHourlyRates(newProjectId, model.HourlyRates.Select(r => new ProjectHourlyRateBO
                    {
                        UserId = r.UserId,
                        HourlyRate = r.HourlyRate
                    }).ToList());

                AddMessage("success", $"Het project {model.Project.Name} is toegevoegd", "Geslaagd!");
                return RedirectToAction("Detail", new { projectid = newProjectId });
            }

            AddMessage("error", $"Het project {model.Project.Name} is NIET toegevoegd", "Fout!");

            FillInAddSelectLists(model);
            FillInAvailableUsers(model);
            return View(model);
        }


        [HttpGet]
        //[Breadcrumb("Info")]
        [Breadcrumb("Info", FromAction = "Index")]
        public ActionResult Detail(int projectid, bool EditGeneralData = false)
        {


            //NEXT

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProject = _ps.HasWrite(PermissionCodes.ProjectsDetail);
            ViewBag.CanDeleteProject = _ps.HasDelete(PermissionCodes.ProjectsDetail);
            ShowProjectDetail model = new ShowProjectDetail();
            var Service = _projectService;
            var cservice = _clientService;
            var response = Service.GetProjectByID(projectid);
            if ((response.Success))
                model.Project = response.Value;
            model.Project.Postalcode.Country.CountryId = 19;
            model.Project.Postalcode.Country.ISOCode = "BE";
            model.ProjectName = model.Project.Name;
            FillInAddSelectListsDetail(ref model);
            model.GeneralDataEditMode = EditGeneralData;
            model.SelectedPostalcode = (int)model.Project.Postalcode.PostcodeId;
            model.Docs = Service.GetProjectDocs(projectid).Values;
            model.Users = GetOrderedUsers();
            if (!model.Project.ExecutionDays.HasValue || model.Project.ExecutionDays.Value == 0)
                model.ExecutionDays = Service.GetProjectExecutionDays(model.Project.Id);
            else
                model.ExecutionDays = model.Project.ExecutionDays.Value;
            if (model.Project.StartDateConstruction is not null)
                model.StartDate = (DateOnly)model.Project.StartDateConstruction;
            else
                model.StartDate = Service.GetProjectStartDateConstruction(model.Project.Id);
            model.WorkingDaysLeft = -9999;
            if (model.ExecutionDays != 0 && model.StartDate != DateOnly.MinValue)
            {
                model.FinalConstructionDate = Service.GetFinalConstructionDay(model.Project.Id, model.StartDate, model.ExecutionDays);
                if (model.FinalConstructionDate != DateOnly.MinValue)
                    model.WorkingDaysLeft = Service.GetWorkingDaysLeft(model.FinalConstructionDate, model.Project.Id);
            }
            var response2 = cservice.GetClientAccountsByProjectIdLast5(projectid);
            if ((response2.Success))
                model.RecentClients = response2.Values;
            var response3 = Service.GetLatestProjectNews(1, projectid);
            if ((response3.Success))
                model.LatestNews = response3.Values.FirstOrDefault();
            if (model.LatestNews is not null)
            {
                if (model.LatestNews.TextNL is not null & model.LatestNews.TextNL.Length > 250)
                    model.LatestNews.TextNL = model.LatestNews.TextNL.Substring(0, 250).ToString() + " ...";
            }
            var response4 = Service.GetLatestProjectPictures(1, projectid);
            if ((response4.Success))
                model.LatestPicture = response4.Values.FirstOrDefault();
            var response5 = Service.GetLatestProjectDocs(5, projectid);
            if ((response5.Success))
                model.LatestDocs = response5.Values;
            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };

            // leaf: Detail met dynamische titel + id in de link
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.Project.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }               // ← zorgt voor /Projecten/Detail/{id}
            };

            ViewData["BreadcrumbNode"] = projectDetail;

            return View(model);
        }
        [HttpGet]
        public IActionResult ModalDeleteProject(int id)
        {
            var model = new ProjectBO();

            if (id != 0)
            {
                var service = _projectService;
                var response = service.GetProjectByID(id);

                if (response.Success && response.Value is not null)
                {
                    model = response.Value;
                }
                else
                {
                    model.Id = id;
                }
            }

            return PartialView("Modals/_ModalDeleteProject", model);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsDetail)]
        public IActionResult DeleteProject(int id)
        {
            if (id == 0)
            {
                AddMessage("error", "Het project kon niet verwijderd worden.", "Fout!");
                return RedirectToAction("Index");
            }

            var service = _projectService;
            var response = service.Delete(new List<int> { id });

            if (response.Success)
            {
                AddMessage("success", "Het project is verwijderd", "Geslaagd!");
                return RedirectToAction("Index");
            }

            AddMessage("error", "Het project kon niet verwijderd worden.", "Fout!");
            return RedirectToAction("Detail", new { projectid = id });
        }

        [HttpGet]
        [Breadcrumb("Project bewerken", FromAction = nameof(Index))]
        public IActionResult Edit(int projectid)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProject = _ps.HasWrite(PermissionCodes.ProjectsDetail);

            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = string.IsNullOrEmpty(referrer)
                ? Url.Action("Detail", "Projecten", new { projectid })
                : referrer;

            var model = new EditProjectDetail();
            var service = _projectService;
            var response = service.GetProjectByID(projectid);

            if (!response.Success || response.Value == null)
            {
                AddMessage("error", "Project niet gevonden", "Fout!");
                return RedirectToAction("Index");
            }

            model.Project = response.Value;
            model.Project.Postalcode.Country.CountryId = model.Project.Postalcode.Country.CountryId == 0 ? 19 : model.Project.Postalcode.Country.CountryId;
            model.Project.Postalcode.Country.ISOCode = string.IsNullOrWhiteSpace(model.Project.Postalcode.Country.ISOCode) ? "BE" : model.Project.Postalcode.Country.ISOCode;
            model.SelectedCountry = model.Project.Postalcode.Country.CountryId;
            model.SelectedPostalcode = model.Project.Postalcode.PostcodeId.HasValue ? (int)model.Project.Postalcode.PostcodeId.Value : 0;
            model.SelectedStatus = model.Project.Status.Id;

            FillInAddSelectListsDetailEdit(model);
            FillInAvailableUsers(model);

            model.Users = GetOrderedUsers();

            // Laad schijven en uurtarieven
            if (model.Project.CoordinationIssuerCompanyId.HasValue)
            {
                var slicesResp = _projectService.GetContractSlices(projectid);
                if (slicesResp.Success)
                    model.ContractSlices = slicesResp.Values.Select(s => new ProjectContractSliceVM
                    {
                        Id = s.Id,
                        Description = s.Description,
                        Percentage = s.Percentage
                    }).ToList();

                var ratesResp = _projectService.GetProjectHourlyRates(projectid);
                if (ratesResp.Success)
                    model.HourlyRates = ratesResp.Values.Select(r => new ProjectHourlyRateVM
                    {
                        UserId = r.UserId,
                        UserFullName = r.UserFullName,
                        HourlyRate = r.HourlyRate
                    }).ToList();
            }

            ViewData["Title"] = $"Project - {model.Project.Name}";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Project bewerken", FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(EditProjectDetail model)
        {
            if (!ModelState.IsValid)
            {
                FillInAddSelectListsDetailEdit(model);
                FillInAvailableUsers(model);
                model.Users = GetOrderedUsers();
                return View(model);
            }

            model.Project.Postalcode.Country.CountryId = model.SelectedCountry;
            model.Project.Postalcode.PostcodeId = model.SelectedPostalcode;
            model.Project.Status.Id = model.SelectedStatus;
            model.Project.Slug = GetSlugForPostcodeId(model.SelectedPostalcode, model.Project.Name ?? string.Empty);

            // Routeberekening bij wijziging coördinatieproject-instellingen
            if (model.Project.CoordinationIssuerCompanyId.HasValue && model.SelectedPostalcode > 0)
            {
                var (distKm, durSec) = await _projectService.CalculateRouteAsync(
                    model.Project.CoordinationIssuerCompanyId.Value,
                    model.SelectedPostalcode);
                if (distKm.HasValue)
                {
                    model.Project.ProjectDistanceKm    = distKm;
                    model.Project.RouteDurationSeconds = durSec;
                }
            }

            if (model.StandardFotoUpload != null && model.StandardFotoUpload.Length > 0)
            {
                var stdFilename = await ProcessStandardFotoUploadAsync(model.StandardFotoUpload);
                if (stdFilename != null)
                    model.Project.StandardFotoName = stdFilename;
                else
                    AddMessage("warning", "Standaard foto kon niet worden opgeslagen.", "Waarschuwing");
            }

            var service = _projectService;
            var response = service.InsertUpdate(model.Project);

            if (response.Success)
            {
                var projectId = model.Project.Id;

                // Sla contract schijven op
                service.SaveContractSlices(projectId, model.ContractSlices?.Select(s => new ProjectContractSliceBO
                {
                    Description = s.Description,
                    Percentage = s.Percentage
                }).ToList() ?? new List<ProjectContractSliceBO>());

                // Sla uurtarieven op
                service.SaveProjectHourlyRates(projectId, model.HourlyRates?.Select(r => new ProjectHourlyRateBO
                {
                    UserId = r.UserId,
                    HourlyRate = r.HourlyRate
                }).ToList() ?? new List<ProjectHourlyRateBO>());

                AddMessage("success", $"{model.Project.Name} is bijgewerkt", "Geslaagd!");
                return RedirectToAction("Detail", new { projectid = model.Project.Id });
            }

            AddMessage("error", $"{model.Project.Name} is NIET bijgewerkt", "Fout!");

            FillInAddSelectListsDetailEdit(model);
            FillInAvailableUsers(model);
            model.Users = GetOrderedUsers();
            return View(model);
        }
        private async Task<string?> ProcessStandardFotoUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            if (!_validImageTypes.Contains(file.ContentType)) return null;

            var ts       = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-pictures");
            Directory.CreateDirectory(tempRoot);

            var rawPath  = Path.Combine(tempRoot, $"std_raw_{ts}{Path.GetExtension(file.FileName)}");
            var path447  = Path.Combine(tempRoot, $"std_447_{ts}.webp");
            var path800  = Path.Combine(tempRoot, $"std_800_{ts}.webp");
            var pathFull = Path.Combine(tempRoot, $"std_full_{ts}.webp");
            var webpName = $"std_{ts}.webp";

            try
            {
                using (var stream = System.IO.File.Create(rawPath))
                    await file.CopyToAsync(stream);

                System.IO.File.Copy(rawPath, path447,  overwrite: true);
                System.IO.File.Copy(rawPath, path800,  overwrite: true);
                System.IO.File.Copy(rawPath, pathFull, overwrite: true);

                ScaleAndCropImage(path447, 447, 447);
                ScaleAndCropImage(path800, 800, 800);
                ScaleImage(pathFull, 1280, 960);

                path447  = Path.ChangeExtension(path447,  ".webp");
                path800  = Path.ChangeExtension(path800,  ".webp");
                pathFull = Path.ChangeExtension(pathFull, ".webp");

                var uploadFull = await UploadAssetFileToStorageAsync(pathFull, "pictures",     webpName, "image/webp");
                var upload447  = await UploadAssetFileToStorageAsync(path447,  "pictures/447", webpName, "image/webp");
                var upload800  = await UploadAssetFileToStorageAsync(path800,  "pictures/800", webpName, "image/webp");

                if (string.IsNullOrWhiteSpace(uploadFull) || string.IsNullOrWhiteSpace(upload447) || string.IsNullOrWhiteSpace(upload800))
                    return null;

                return webpName;
            }
            finally
            {
                TryDeleteTempFile(rawPath);
                TryDeleteTempFile(path447);
                TryDeleteTempFile(path800);
                TryDeleteTempFile(pathFull);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProjectStandardFoto(int projectId, IFormFile file)
        {
            var filename = await ProcessStandardFotoUploadAsync(file);
            if (filename == null)
                return Json(new { success = false, error = "Upload mislukt of ongeldig bestandstype (jpg/png/webp vereist)." });

            var response = _projectService.GetProjectByID(projectId);
            if (!response.Success || response.Value == null)
                return Json(new { success = false, error = "Project niet gevonden." });

            var project = response.Value;
            project.StandardFotoName = filename;
            _projectService.InsertUpdate(project);

            return Json(new { success = true, filename, url = Configuration["URL:ImageWebUrl"] + "pictures/447/" + filename });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveProjectStandardFoto(int projectId)
        {
            var response = _projectService.GetProjectByID(projectId);
            if (!response.Success || response.Value == null)
                return Json(new { success = false, error = "Project niet gevonden." });

            var project = response.Value;
            project.StandardFotoName = null;
            _projectService.InsertUpdate(project);

            return Json(new { success = true });
        }

        private IEnumerable<CpmUserOption> GetOrderedUsers()
        {
            var internalUserIds = _db.PermissionPerUser.Select(p => p.UserId).Distinct();
            var users = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && internalUserIds.Contains(u.Id))
                .OrderBy(user => user.Familienaam)
                .ThenBy(user => user.Voornaam)
                .Select(user => new
                {
                    user.UserId,
                    user.Voornaam,
                    user.Familienaam
                })
                .ToList();

            return users.Select(user => new CpmUserOption
            {
                Id = user.UserId ?? string.Empty,
                DisplayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
                        .Where(value => !string.IsNullOrWhiteSpace(value)))
            })
                .ToList();
        }


        // ========== PROJECT DETAIL KLANTEN ==========

        [HttpGet]
        //[Breadcrumb("Klanten")]
        [Breadcrumb("Klanten", FromAction = "Detail")]
        public ActionResult DetailClients(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectCustomers = _ps.HasWrite(PermissionCodes.ProjectsCustomers);
            ViewBag.CanDeleteProjectCustomers = _ps.HasDelete(PermissionCodes.ProjectsCustomers);
            DetailClientsModel model = new DetailClientsModel();
            var service = _clientService;
            var service2 = _projectService;
            var response = service.GetClientAccountsByProjectIdWithUnits(projectid);
            if ((response.Success))
                model.ClientAccounts = response.Values;

            if (model.ClientAccounts.SelectMany(m => m.Units.Where(i => i.Type.GroupId == 1)).Count() > 0)
                model.ClientAccounts = model.ClientAccounts.OrderBy(m => m.Units.Where(a => a.Type.GroupId == 1).Count() > 0 ? m.Units.Where(a => a.Type.GroupId == 1).FirstOrDefault().Name : "", new ServiceCore.Helpers.AlphanumComparator()).ToList();
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);
            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }              
            };
            var projectKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }            
            };
            ViewData["BreadcrumbNode"] = projectKlanten;
            return View(model);
        }

        // ========== PROJECT DETAIL EENHEDEN ==========

        [HttpGet]
        //[Breadcrumb("Eenheden")]
        [Breadcrumb("Eenheden", FromAction = "Detail")]
        public ActionResult DetailUnits(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            DetailUnitsModel model = new DetailUnitsModel();
            model = FillDetailUnitModel(projectid);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectUnits;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectUnits = _ps.HasWrite(PermissionCodes.ProjectsUnits);
            ViewBag.CanDeleteProjectUnits = _ps.HasDelete(PermissionCodes.ProjectsUnits);
            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contacten", FromAction = "Detail")]
        public ActionResult DetailContacts(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var model = new DetailContactsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid)
            };

            var response = service.GetProjectContactRequests(projectid);
            if (response.Success) model.Contacts = response.Values;

            var contacts = model.Contacts ?? new List<ContactRequestBO>();
            model.ContactGroups = BuildContactGroups(contacts);
            model.Stats = BuildContactStats(model.ContactGroups, contacts);


            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectContacts;
            var _psContacts = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectContacts = _psContacts.HasWrite(PermissionCodes.ProjectsContacts);
            ViewBag.CanDeleteProjectContacts = _psContacts.HasDelete(PermissionCodes.ProjectsContacts);

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Contactdetails", FromAction = "DetailContacts")]
        public ActionResult ContactDetails(int projectid, string email, string fullname, string phone)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var response = service.GetProjectContactRequests(projectid);
            var contacts = response.Success ? response.Values : new List<ContactRequestBO>();

            var groupContacts = FilterContactGroup(contacts, email, fullname, phone);
            var groupModel = BuildContactGroup(groupContacts);

            var model = new ContactDetailsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid),
                Contact = groupModel,
                Requests = groupContacts.OrderByDescending(c => c.CreatedAt).ToList(),
                NewAction = new ContactActionInputModel
                {
                    ProjectId = projectid,
                    Email = groupModel?.Email,
                    Fullname = groupModel?.DisplayName,
                    Phone = groupModel?.Phone,
                    ActionDate = DateTime.Now,
                    ActionTime = DateTime.Now.TimeOfDay
                },
                NewStatus = new ContactStatusInputModel
                {
                    ProjectId = projectid,
                    Email = groupModel?.Email,
                    Fullname = groupModel?.DisplayName,
                    Phone = groupModel?.Phone,
                    StatusDate = DateTime.Now,
                    StatusTime = DateTime.Now.TimeOfDay
                }
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var contactDetails = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ContactDetails", "Projecten", "Contactdetails")
            {
                Parent = projectContacts,
                RouteValues = new { projectid = projectid, email, fullname, phone }
            };
            ViewData["BreadcrumbNode"] = contactDetails;
            var _psContactDet = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectContacts = _psContactDet.HasWrite(PermissionCodes.ProjectsContacts);

            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contact toevoegen", FromAction = "DetailContacts")]
        public ActionResult AddContact(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var projectName = service.GetProjectNameById(projectid);

            var model = new ContactAddModel
            {
                ProjectId = projectid,
                ProjectName = projectName,
                ContactDate = DateTime.Today
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var addContact = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddContact", "Projecten", "Contact toevoegen")
            {
                Parent = projectContacts,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = addContact;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContact(ContactAddModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Firstname = NormalizeContactName(model.Firstname),
                Lastname = NormalizeContactName(model.Lastname),
                Fullname = BuildFullname(model.Firstname, model.Lastname),
                Email = model.Email,
                Phone = model.Phone,
                RequestType = "Contact",
                Question = model.Comment,
                CreatedAt = model.ContactDate?.Date ?? DateTime.Now,
                SourceSite = model.ContactMethod,
                Origin = User?.Identity?.Name ?? "Onbekende gebruiker"
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact toegevoegd" : "Contact niet toegevoegd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }
        [HttpGet]
        [Breadcrumb("Contact bewerken", FromAction = "DetailContacts")]
        public ActionResult EditContact(int projectid, string email, string fullname, string phone)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var response = service.GetProjectContactRequests(projectid);
            var contacts = response.Success ? response.Values : new List<ContactRequestBO>();
            var groupContacts = FilterContactGroup(contacts, email, fullname, phone);
            var latestContact = groupContacts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            var fullName = GetContactDisplayName(latestContact);

            var model = new ContactEditModel
            {
                ProjectId = projectid,
                Email = email,
                Fullname = fullname ?? fullName,
                Phone = phone ?? latestContact?.Phone,
                Firstname = latestContact?.Firstname,
                Lastname = latestContact?.Lastname,
                NewEmail = latestContact?.Email,
                NewPhone = latestContact?.Phone,
                ContactDate = latestContact?.CreatedAt.Date,
                ContactMethod = latestContact?.SourceSite
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditContact(ContactEditModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var service = _projectService;
            var updatedValues = new ContactRequestBO
            {
                Firstname = NormalizeContactName(model.Firstname),
                Lastname = NormalizeContactName(model.Lastname),
                Fullname = BuildFullname(model.Firstname, model.Lastname),
                Email = model.NewEmail,
                Phone = model.NewPhone,
                CreatedAt = model.ContactDate?.Date ?? default,
                SourceSite = model.ContactMethod
            };

            var response = service.UpdateContactRequestGroup(model.ProjectId, model.Email, model.Fullname, model.Phone, updatedValues);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact bijgewerkt" : "Contact niet bijgewerkt", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteContact(ContactDeleteModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var service = _projectService;
            var response = service.DeleteContactRequestGroup(model.ProjectId, model.Email, model.Fullname, model.Phone);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact verwijderd" : "Contact niet verwijderd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContactAction(ContactActionInputModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var actionTime = model.ActionTime == default ? DateTime.Now.TimeOfDay : model.ActionTime;
            var actionDateTime = model.ActionDate.Date.Add(actionTime);
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Email = model.Email,
                Fullname = model.Fullname,
                Phone = model.Phone,
                RequestType = InternalActionRequestType,
                Subject = model.ActionType,
                Question = model.Comment,
                CreatedAt = actionDateTime,
                SourceSite = InternalSourceSite,
                Origin = userName
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Actie toegevoegd" : "Actie niet toegevoegd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("ContactDetails", new { projectid = model.ProjectId, email = model.Email, fullname = model.Fullname, phone = model.Phone });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContactStatus(ContactStatusInputModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var statusTime = model.StatusTime == default ? DateTime.Now.TimeOfDay : model.StatusTime;
            var statusDateTime = model.StatusDate.Date.Add(statusTime);
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Email = model.Email,
                Fullname = model.Fullname,
                Phone = model.Phone,
                RequestType = StatusUpdateRequestType,
                Subject = model.Status,
                Question = model.Comment,
                CreatedAt = statusDateTime,
                SourceSite = InternalSourceSite,
                Origin = userName
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Status bijgewerkt" : "Status niet bijgewerkt", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("ContactDetails", new { projectid = model.ProjectId, email = model.Email, fullname = model.Fullname, phone = model.Phone });
        }

        private const string InternalActionRequestType = "Interne actie";
        private const string StatusUpdateRequestType = "Status update";
        private const string InternalSourceSite = "CPM";
        private const string InternalOrigin = "CPM";

        private static List<ContactGroupModel> BuildContactGroups(List<ContactRequestBO> contacts)
        {
            return contacts
                .GroupBy(BuildContactKey)
                .Select(group => BuildContactGroup(group.ToList()))
                .Where(group => group != null)
                .OrderByDescending(group => group.LatestContactAt)
                .ToList();
        }

        private static ContactStatsModel BuildContactStats(List<ContactGroupModel> groups, List<ContactRequestBO> allContacts)
        {
            var stats = new ContactStatsModel();
            var now = DateTime.Now;
            var groupedContacts = allContacts.GroupBy(BuildContactKey)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

            stats.TotalContacts = groups.Count;
            stats.ActiveContacts = groups.Count(g => string.IsNullOrWhiteSpace(g.LatestStatus) || string.Equals(g.LatestStatus, "Actief", StringComparison.OrdinalIgnoreCase));
            stats.NewContactsWeek = groups.Count(g => GetFirstContactDate(groupedContacts, g.GroupKey) >= now.Date.AddDays(-7));
            stats.NewContactsMonth = groups.Count(g => GetFirstContactDate(groupedContacts, g.GroupKey) >= now.Date.AddDays(-30));

            stats.ConversionRate = stats.TotalContacts > 0
                ? Math.Round((decimal)stats.ActiveContacts / stats.TotalContacts * 100, 2)
                : 0;

            var responseRateData = BuildResponseRate(groups, allContacts);
            stats.ResponseRate = responseRateData.TotalWithInternalAction > 0
                ? Math.Round((decimal)responseRateData.TotalWithResponse / responseRateData.TotalWithInternalAction * 100, 2)
                : 0;

            return stats;
        }

        private static (int TotalWithInternalAction, int TotalWithResponse) BuildResponseRate(List<ContactGroupModel> groups, List<ContactRequestBO> allContacts)
        {
            var groupedContacts = allContacts.GroupBy(BuildContactKey)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).ToList());

            var totalWithInternal = 0;
            var totalWithResponse = 0;

            foreach (var group in groups)
            {
                if (!groupedContacts.TryGetValue(group.GroupKey, out var contactRequests))
                    continue;

                var internalActions = contactRequests
                    .Where(c => IsInternalRequestType(c.RequestType) && string.Equals(c.RequestType, InternalActionRequestType, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                if (!internalActions.Any())
                    continue;

                totalWithInternal += 1;
                var latestActionDate = internalActions.First().CreatedAt;

                var responseAfterAction = contactRequests.Any(c => !IsInternalRequestType(c.RequestType) && c.CreatedAt > latestActionDate);
                if (responseAfterAction)
                    totalWithResponse += 1;
            }

            return (totalWithInternal, totalWithResponse);
        }

        private static DateTime GetFirstContactDate(Dictionary<string, List<ContactRequestBO>> groupedContacts, string groupKey)
        {
            if (groupedContacts == null || string.IsNullOrWhiteSpace(groupKey) || !groupedContacts.TryGetValue(groupKey, out var contacts))
                return DateTime.MinValue;

            var firstExternal = contacts.FirstOrDefault(c => !IsInternalRequestType(c.RequestType));
            return firstExternal?.CreatedAt ?? contacts.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue;
        }

        private static ContactGroupModel BuildContactGroup(List<ContactRequestBO> contacts)
        {
            if (contacts == null || contacts.Count == 0)
                return null;

            var latestExternal = contacts
                .Where(c => !IsInternalRequestType(c.RequestType))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            var latestOverall = contacts.OrderByDescending(c => c.CreatedAt).First();
            var referenceContact = latestExternal ?? latestOverall;
            var displayName = GetContactDisplayName(referenceContact);

            var latestStatus = contacts
                .Where(c => string.Equals(c.RequestType, StatusUpdateRequestType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            return new ContactGroupModel
            {
                GroupKey = BuildContactKey(referenceContact),
                DisplayName = displayName,
                Email = referenceContact.Email,
                Phone = referenceContact.Phone,
                LatestContactAt = (latestExternal ?? latestOverall).CreatedAt,
                LatestRequestType = referenceContact.RequestType,
                LatestSourceSite = referenceContact.SourceSite,
                LatestOrigin = referenceContact.Origin,
                TotalRequests = contacts.Count,
                LatestStatus = latestStatus?.Subject,
                LatestStatusComment = latestStatus?.Question,
                LatestStatusAt = latestStatus?.CreatedAt
            };
        }

        private static List<ContactRequestBO> FilterContactGroup(List<ContactRequestBO> contacts, string email, string fullname, string phone)
        {
            var normalizedEmail = NormalizeContactValue(email);
            var normalizedName = NormalizeContactValue(fullname);
            var normalizedPhone = NormalizeContactValue(phone);

            return contacts
                .Where(c => MatchesContactGroup(c, normalizedEmail, normalizedName, normalizedPhone))
                .ToList();
        }

        private static bool MatchesContactGroup(ContactRequestBO contact, string normalizedEmail, string normalizedName, string normalizedPhone)
        {
            var contactEmail = NormalizeContactValue(contact.Email);
            var contactName = NormalizeContactValue(GetContactDisplayName(contact));
            var contactPhone = NormalizeContactValue(contact.Phone);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
                return contactEmail == normalizedEmail;

            return contactName == normalizedName && contactPhone == normalizedPhone;
        }

        private static string BuildContactKey(ContactRequestBO contact)
        {
            var email = NormalizeContactValue(contact?.Email);
            if (!string.IsNullOrWhiteSpace(email))
                return email;
            var name = NormalizeContactValue(GetContactDisplayName(contact));
            var phone = NormalizeContactValue(contact?.Phone);
            return $"{email}|{name}|{phone}";
        }

        private static string GetContactDisplayName(ContactRequestBO contact)
        {
            if (contact == null)
                return "-";

            var fullname = !string.IsNullOrWhiteSpace(contact.Fullname)
                  ? contact.Fullname
                  : $"{contact.Firstname} {contact.Lastname}".Trim();
            if (!string.IsNullOrWhiteSpace(fullname))
                return ToTitleCase(fullname);


            var fallback = $"{contact.Firstname} {contact.Lastname}".Trim();
            return string.IsNullOrWhiteSpace(fallback) ? "-" : fallback;
        }

        private static bool IsInternalRequestType(string requestType)
        {
            return string.Equals(requestType, InternalActionRequestType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(requestType, StatusUpdateRequestType, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeContactValue(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        private static string NormalizeContactName(string value)
          => string.IsNullOrWhiteSpace(value) ? string.Empty : ToTitleCase(value.Trim());

        private static string BuildFullname(string firstname, string lastname)
        {
            var normalizedFirstname = NormalizeContactName(firstname);
            var normalizedLastname = NormalizeContactName(lastname);
            return $"{normalizedFirstname} {normalizedLastname}".Trim();
        }

        private static string ToTitleCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var culture = CultureInfo.CurrentCulture;
            return culture.TextInfo.ToTitleCase(value.ToLower(culture));
        }
        [HttpGet]
        //[Breadcrumb("Eenheid toevoegen")]
        [Breadcrumb("Eenheid toevoegen", FromAction = "DetailUnits")]
        public ActionResult AddUnit(int projectid)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            ViewData["Referrer"] = referrer;
            var model = new AddUnitModel();
            var service = _unitService;
            var service2 = _projectService;

            // Get Units for attached unit select
            var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(projectid);
            if (u2response.Success) model.AttachableUnits = u2response.Values;

            // Get GroupTypes
            var responsegroup = service.GetUnitGroupTypes();
            if (responsegroup.Success) model.GroupTypes = responsegroup.Values;

            // Get Subtypes
            var responsetypes = service.GetUnitTypesByGroupId(model.SelectedGroupType);
            if (responsetypes.Success) model.Types = responsetypes.Values;

            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);
            model.ProjectLandShare = (int)service2.GetProjectLandshareById(projectid);

            var constval = new UnitConstructionValueBO();
            constval.PaymentGroupId = 0;
            model.ConstructionValues.Add(constval);

            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            if (responsepaymentgroups.Success) model.PaymentGroups = responsepaymentgroups.Values;
            ViewBag.paymentgroups = model.PaymentGroups;


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddUnit", "Projecten", "Eenheden toevoegen")
            {
                Parent = projectUnits,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        [HttpPost]
        public ActionResult AddUnit(DetailUnitsModel Model)
        {
            if (Model.SelectedType == 0)
                return RedirectToAction("DetailUnits", Model);
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            Model.AddUnit.ProjectId = Model.ProjectId;
            Model.AddUnit.Type.Id = Model.SelectedType;

            if (Model.AddUnit.AttachedUnitsId == 0)
                Model.AddUnit.AttachedUnitsId = null;
            var service = _unitService;
            var service2 = _projectService;
            var response = service.InsertUpdateUnit(Model.AddUnit);
            if (response.Success == true)
            {
                foreach (var item in Model.ConstructionValues)
                {
                    item.UnitId = response.InsertedId;
                    var responseConst = service.InsertUpdateConstructionValue(item);
                }
                Model.AddUnit.Name = "";
                var response2 = service.GetGroupedUnitsByProjectId(Model.ProjectId);
                Model.UnitsGrouped = response2.Values;
                // Get GroupTypes
                var responsegroup = service.GetUnitGroupTypes();
                if ((responsegroup.Success))
                    Model.GroupTypes = responsegroup.Values;
                // Get Subtypes
                var responsetypes = service.GetUnitTypesByGroupId(Model.SelectedGroupType);
                if ((responsetypes.Success))
                    Model.Types = responsetypes.Values;

                Model.ProjectName = service2.GetProjectNameById(Model.ProjectId);

                AddMessage("success", "De eenheid is aan het project toegevoegd", "Geslaagd!");
                return RedirectToAction("DetailUnits", new { projectid = Model.ProjectId });
            }
            else
            {
                AddMessage("error", "De eenheid is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                return RedirectToAction("DetailUnits", new { projectid = Model.ProjectId });
            }
        }
        [HttpGet]
        [Breadcrumb("Eenheid bewerken", FromAction = "DetailUnits")]
        //[Breadcrumb("Eenheid bewerken")]
        public async Task<ActionResult> EditUnit(int projectid, int unitid)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            EditUnitModel model = new EditUnitModel();
            var service = _unitService;
            var service2 = _projectService;

            // Get Unit
            var response = service.GetUnitById(unitid);
            if ((response.Success))
                model.Unit = response.Value;
            // linkedunits
            foreach (var u in model.Unit.LinkedUnits)
                model.SelectedUnits.Add(u.Id);
            if (model.Unit.IsLink == true)
                model.Type = EditUnitModel.EnumType.Koppeling;
            else
                model.Type = EditUnitModel.EnumType.Eenheid;
            // Get Units for select
            var uresponse = service.GetUnitsByProjectIdForSelect(model.Unit.ProjectId, model.Unit.Type.Id);
            if ((uresponse.Success))
                model.Units = uresponse.Values;
            // Get Units for attached unit select
            var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(model.Unit.ProjectId, unitid);
            if ((u2response.Success))
                model.AttachableUnits = u2response.Values;
            // Get GroupTypes
            var responsegroup = service.GetUnitGroupTypes();
            if ((responsegroup.Success))
                model.GroupTypes = responsegroup.Values;
            model.SelectedGroupType = model.Unit.Type.GroupId;
            // Get Rooms
            var responserooms = service.GetRooms(unitid);
            if ((responserooms.Success))
                model.Rooms = responserooms.Values;
            model.Rooms = model.Rooms.OrderBy(m => m.Type).ToList();
            // Get Constructionvalues
            var responseconstructionvalues = service.GetConstructionValues(unitid);
            if ((responseconstructionvalues.Success))
                model.ConstructionValues = responseconstructionvalues.Values;

            // Get FinishingOptions (auto-migrate legacy CVs on first open)
            service.EnsureDefaultFinishingOption(unitid);
            var responseFinishing = service.GetFinishingOptions(unitid);
            model.FinishingOptions = responseFinishing.Success ? responseFinishing.Values : new List<UnitFinishingOptionBO>();

            // Get Subtypes
            var responsetypes = service.GetUnitTypesByGroupId(model.Unit.Type.GroupId);
            if ((responsetypes.Success))
                model.Types = responsetypes.Values;
            model.SelectedType = model.Unit.Type.Id;

            // Get PaymentGroups
            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            if ((responsepaymentgroups.Success))
                model.PaymentGroups = responsepaymentgroups.Values;

            ViewBag.PaymentGroups = model.PaymentGroups
            .Select(pg => new SelectListItem { Value = pg.ID.ToString(), Text = pg.Display })
            .ToList();

            if (model.Unit.PaymentGroupId is not null)
                model.SelectedPaymentGroup = model.Unit.PaymentGroupId;
            else
                model.SelectedPaymentGroup = 0;

            model.ProjectId = model.Unit.ProjectId;
            model.ProjectName = service2.GetProjectNameById(model.Unit.ProjectId);
            model.ExecutionPlans = await BuildUnitExecutionPlansVm(model.Unit.Id);


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddUnit", "Projecten", model.Unit.Name + " - Bewerken")
            {
                Parent = projectUnits,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        [HttpPost]
        public async Task<ActionResult> EditUnit(EditUnitModel Model, IFormFile? file, List<IFormFile>? executionPlanFiles, List<string>? executionPlanNames, List<int>? deleteExecutionPlanIds)
        {
            //Referrer
            var Referrer = TempData["Referrer"];

            StringCollection validtypes = new StringCollection();
            validtypes.Add("application/pdf");
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".pdf";
            if (file != null && file.Length > 0)
            {
                if ((!validtypes.Contains(file.ContentType)))
                    ModelState.AddModelError("PdfUpload", "Verkeerd type gekozen, kies een pdf");
            }
            if (executionPlanFiles != null)
            {
                foreach (var planFile in executionPlanFiles.Where(f => f != null && f.Length > 0))
                {
                    if (!validtypes.Contains(planFile.ContentType))
                    {
                        ModelState.AddModelError("ExecutionPlansUpload", "Alle uitvoeringsplannen moeten PDF bestanden zijn.");
                        break;
                    }
                }
            }
            if (ModelState.IsValid)
            {
                if ((file != null && file.Length > 0))
                {
                    var uploadedFileName = await UploadAssetToStorageAsync(file, "plans");
                    if (string.IsNullOrWhiteSpace(uploadedFileName))
                    {
                        ModelState.AddModelError("PdfUpload", "Plan upload naar storage API mislukt.");
                        Model.ExecutionPlans = await BuildUnitExecutionPlansVm(Model.Unit.Id);
                        return View(Model);
                    }

                    Model.Unit.Plan = uploadedFileName;
                }



                ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
                Model.Unit.ProjectId = Model.ProjectId;
                Model.Unit.Type.Id = Model.SelectedType;
                if (Model.SelectedPaymentGroup != 0 && Model.SelectedPaymentGroup is not null)
                    Model.Unit.PaymentGroupId = Model.SelectedPaymentGroup;
                if ((Model.Unit.IsLink))
                {
                    foreach (var i in Model.SelectedUnits)
                    {
                        UnitBO bo = new UnitBO();
                        bo.Id = i;
                        Model.Unit.LinkedUnits.Add(bo);
                    }
                    Model.Unit.Name = "KOPPELING";
                }
                var service = _unitService;
                var service2 = _projectService;
                try
                {
                    var response = service.InsertUpdateUnit(Model.Unit);
                    if (response.Success == false)
                        throw new ApplicationException(response.Messages.SingleOrDefault().Message);
                    Response response2 = new Response();
                    foreach (var room in Model.Rooms)
                        response2 = service.InsertUpdateRoom(room);
                    if (response2.Success == false)
                    {
                        foreach (var message in response2.Messages)
                            throw new ApplicationException(message.Message);
                    }
                    Response response3 = new Response();
                    foreach (var constructionvalue in Model.ConstructionValues)
                    {
                        response3 = service.InsertUpdateConstructionValue(constructionvalue);
                        if (response3.Success)
                            constructionvalue.Id = response3.InsertedId;
                    }
                    if (response3.Success == false)
                    {
                        foreach (var message2 in response3.Messages)
                            throw new ApplicationException(message2.Message);
                    }
                    List<UnitConstructionValueBO> tableresult = new List<UnitConstructionValueBO>();
                    var responsetable = service.GetConstructionValues(Model.Unit.Id);
                    if ((responsetable.Success))
                        tableresult = responsetable.Values;
                    List<int> deleteids = new List<int>();
                    foreach (var result in tableresult)
                    {
                        if (Model.ConstructionValues.Exists(m => m.Id == result.Id))
                        {
                        }
                        else
                            deleteids.Add(result.Id);
                    }
                    var response4 = service.DeleteConstructionValues(deleteids);
                    if (response4.Success == false)
                        throw new ApplicationException(response4.Messages.SingleOrDefault().Message);
                    if (deleteExecutionPlanIds != null && deleteExecutionPlanIds.Count > 0)
                    {
                        var plansToDelete = await _db.UnitExecutionPlan
                            .Where(x => x.UnitId == Model.Unit.Id && deleteExecutionPlanIds.Contains(x.Id))
                            .ToListAsync();

                        if (plansToDelete.Count > 0)
                        {
                            _db.UnitExecutionPlan.RemoveRange(plansToDelete);
                        }
                    }

                    if (executionPlanFiles != null && executionPlanFiles.Count > 0)
                    {
                        var names = executionPlanNames ?? new List<string>();
                        var index = 0;
                        foreach (var executionPlanFile in executionPlanFiles.Where(f => f != null && f.Length > 0))
                        {
                            var uploadedPlanFileName = await UploadAssetToStorageAsync(executionPlanFile, "plans");
                            if (string.IsNullOrWhiteSpace(uploadedPlanFileName))
                                throw new ApplicationException("Uitvoeringsplan upload naar storage API mislukt.");

                            var enteredName = index < names.Count ? names[index] : null;
                            var planName = string.IsNullOrWhiteSpace(enteredName)
                                ? Path.GetFileNameWithoutExtension(executionPlanFile.FileName)
                                : enteredName.Trim();

                            _db.UnitExecutionPlan.Add(new UnitExecutionPlan
                            {
                                UnitId = Model.Unit.Id,
                                Name = planName,
                                FileId = uploadedPlanFileName,
                                CreatedDate = DateTime.UtcNow,
                                CreatedByUserId = User.FindFirst(CpmClaims.UserId)?.Value
                            });
                            index++;
                        }
                    }

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    AddMessage("error", "De eenheid is NIET volledig bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                }
                finally
                {
                    AddMessage("success", "De eenheid is met succes bijgewerkt", "Geslaagd!");
                }
                if (Referrer != null)
                {
                    return Redirect(Referrer.ToString());
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            Model.ExecutionPlans = await BuildUnitExecutionPlansVm(Model.Unit.Id);
            return View(Model);
        }

        private async Task<List<UnitExecutionPlanVm>> BuildUnitExecutionPlansVm(int unitId)
        {
            var plans = await _db.UnitExecutionPlan
                .Where(x => x.UnitId == unitId && x.DeletedDate == null)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return plans.Select(x => new UnitExecutionPlanVm
            {
                Id = x.Id,
                Name = x.Name,
                FileId = x.FileId,
                Url = GetSignedAssetUrlByFileName(x.FileId, "plans")
            }).ToList();
        }

        public DetailUnitsModel FillDetailUnitModel(int id)
        {
            var model = new DetailUnitsModel();
            var service = _unitService;
            var service2 = _projectService;



            //Get Units
            var responseu = service.GetUnitsByProjectId(id);
            if (responseu.Success) model.ProjectUnits = responseu.Values;


            //// Get Units for attached unit select
            //var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(id);
            //if (u2response.Success) model.AttachableUnits = u2response.Values;

            //// Get Units
            //var response = service.GetGroupedUnitsByProjectId(id);
            //model.UnitsGrouped = response.Values;

            //// Get GroupTypes
            //var responsegroup = service.GetUnitGroupTypes();
            //if (responsegroup.Success) model.GroupTypes = responsegroup.Values;
            //model.SelectedGroupType = 1;

            //// Get Subtypes
            //var responsetypes = service.GetUnitTypesByGroupId(model.SelectedGroupType);
            //if (responsetypes.Success) model.Types = responsetypes.Values;

            model.ProjectId = id;
            model.ProjectName = service2.GetProjectNameById(id);
            model.ProjectLandShare = (int)service2.GetProjectLandshareById(id);

            //var constval = new UnitConstructionValueBO();
            //constval.PaymentGroupId = 0;
            //model.ConstructionValues.Add(constval);

            //var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(id);
            //if (responsepaymentgroups.Success) model.PaymentGroups = responsepaymentgroups.Values;
            //ViewBag.paymentgroups = model.PaymentGroups;

            return model;
        }
        [HttpPost]
        public PartialViewResult BlankConstructionValueRow(int unitid, int projectid, int? finishingOptionId = null)
        {
            UnitConstructionValueBO bo = new UnitConstructionValueBO();
            bo.UnitId = unitid;
            bo.PaymentGroupId = 0;
            bo.FinishingOptionId = finishingOptionId;
            var service2 = _projectService;
            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            ViewBag.paymentgroups = responsepaymentgroups.Values;
            return PartialView("_ConstructionValueRow", bo);
        }

        [HttpPost]
        public JsonResult AddFinishingOption(int unitId, string name)
        {
            var bo = new UnitFinishingOptionBO { UnitId = unitId, Name = name, SortOrder = 0 };
            var resp = _unitService.InsertUpdateFinishingOption(bo);
            if (!resp.Success)
                return Json(new { error = "Kon optie niet opslaan" });
            return Json(new { id = resp.InsertedId, name });
        }

        [HttpPost]
        public JsonResult RenameFinishingOption(int id, string name)
        {
            var resp = _unitService.GetFinishingOptionById(id);
            if (!resp.Success) return Json(new { error = "Niet gevonden" });
            resp.Value.Name = name;
            _unitService.InsertUpdateFinishingOption(resp.Value);
            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult RemoveFinishingOption(int id)
        {
            _unitService.DeleteFinishingOption(id);
            return Json(new { ok = true });
        }

        [HttpPost]
        public PartialViewResult BlankFinishingOptionSection(int optionId, int unitId, int projectId)
        {
            var resp = _unitService.GetFinishingOptionById(optionId);
            var bo = resp.Success ? resp.Value : new UnitFinishingOptionBO { Id = optionId, UnitId = unitId };
            var pgResp = _projectService.GetProjectPaymentGroupsForSelect(projectId);
            ViewBag.PaymentGroups = pgResp.Values.Select(pg => new SelectListItem { Value = pg.ID.ToString(), Text = pg.Display }).ToList();
            ViewBag.UnitId = unitId;
            ViewBag.ProjectId = projectId;
            return PartialView("_FinishingOptionSection", bo);
        }
        [HttpPost]
        public PartialViewResult BlankRoomRow(int unitid)
        {
            RoomBO bo = new RoomBO();
            bo.UnitId = unitid;
            bo.Number = 1;
            return PartialView("_RoomEditorRow", bo);
        }
        [HttpGet]
        public ActionResult ModalDeleteUnit(int id)
        {
            var viewModel = new UnitBO();
            if (id != 0)
            {
                var dservice = _unitService;
                viewModel = dservice.GetUnitById(id).Value;
            }
            return PartialView("_ModalDeleteUnit", viewModel);
        }
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsUnits)]
        public ActionResult DeleteUnit(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = _unitService;
                List<int> ids = new List<int>();
                ids.Add(id);
                var response = service.DeleteUnit(ids);
                if (response.Success == true)
                {
                    AddMessage("success", "De eenheid is verwijderd", "Geslaagd!");
                    return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "De eenheid is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
                }
            }
            return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
        }
        [HttpGet]
        public ActionResult ModalAddUnitLink(int id)
        {
            var viewModel = new AddUnitLinkModel();
            var service = _unitService;
            // Get Unit
            var response = service.GetUnitById(id);
            if ((response.Success))
                viewModel.SelectedUnit = response.Value;
            List<int> ids = new List<int>();
            ids.Add(id);
            var response2 = service.GetUnitsByProjectIdForSelect(viewModel.SelectedUnit.ProjectId, viewModel.SelectedUnit.Type.Id);
            if ((response2.Success))
                viewModel.Units = response2.Values;
            viewModel.Units.Remove(viewModel.Units.Find(m => m.ID == id));

            return PartialView("_ModalAddLink", viewModel);
        }
        [HttpPost]
        public ActionResult AddUnitLink(AddUnitLinkModel model)
        {
            Response response = new Response();
            if (ModelState.IsValid)
            {
                var service = _unitService;
                model.SelectedUnits.Add(model.SelectedUnit.Id);
                foreach (var i in model.SelectedUnits)
                {
                    UnitBO bo = new UnitBO();
                    bo.Id = i;
                    model.Unit.LinkedUnits.Add(bo);
                }
                model.Unit.Name = "KOPPELING";
                model.Unit.ProjectId = model.SelectedUnit.ProjectId;
                model.Unit.IsLink = true;

                response = service.InsertUpdateUnit(model.Unit);
            }
            if (response.Success == true)
            {
                AddMessage("success", "De koppeling is geslaagd", "Geslaagd!");
                return RedirectToAction("DetailUnits", "Projecten", new { projectid = model.SelectedUnit.ProjectId });
            }
            else
            {
                AddMessage("error", "De koppeling is NIET geslaagd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                return RedirectToAction("DetailUnits", "Projecten", new { projectid = model.SelectedUnit.ProjectId });
            }
        }

        // ========== PROJECT DETAIL CONTRACTEN ==========
        [HttpGet]
        //[Breadcrumb("Leveranciers")]
        [Breadcrumb("Leveranciers", FromAction = "Detail")]
        public ActionResult DetailContracts(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailContractsModel();
            var service = _projectService;
            var response = service.GetProjectContracts(projectid);

            if (response.Success)
            {
                model.Contracts = response.Values;
            }

            var invoiceSummaryResponse = service.GetProjectIncommingInvoiceCompanySummaries(projectid);
            var invoiceSummaries = invoiceSummaryResponse.Success ? invoiceSummaryResponse.Values : new List<CompanyInvoiceSummaryBO>();
            var contractCompanyIds = model.Contracts
                .Where(c => c.Company != null)
                .Select(c => c.Company.ID)
                .ToHashSet();

            var rows = new List<ContractSupplierRowModel>();

            // Groepeer contracten per leverancier (één rij per bedrijf)
            var contractsByCompany = model.Contracts
                .Where(c => c.Company != null)
                .GroupBy(c => c.Company.ID)
                .ToList();

            foreach (var group in contractsByCompany)
            {
                var company = group.First().Company;
                var summary = invoiceSummaries.FirstOrDefault(s => s.Company?.ID == company.ID);
                rows.Add(new ContractSupplierRowModel
                {
                    Contracts = group.ToList(),
                    Company = company,
                    TotalInvoiced = summary?.TotalInvoiced ?? 0
                });
            }

            foreach (var summary in invoiceSummaries.Where(s => s.Company != null && !contractCompanyIds.Contains(s.Company.ID)))
            {
                rows.Add(new ContractSupplierRowModel
                {
                    Contracts = new List<ContractBO>(),
                    Company = summary.Company,
                    TotalInvoiced = summary.TotalInvoiced
                });
            }

            model.SupplierRows = rows.OrderBy(r => r.Company?.Display).ToList();


            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectContracts;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _ps.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _ps.HasDelete(PermissionCodes.ProjectsSuppliers);
            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Leverancier detail", FromAction = "DetailContracts")]
        public ActionResult DetailContract(int projectid, int contractid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectContractDetailModel();
            var projectService = _projectService;
            var companyService = _companyService;

            model.ProjectId = projectid;
            model.ProjectName = projectService.GetProjectNameById(projectid);

            var contractResponse = projectService.GetContract(contractid);
            if (contractResponse.Success)
            {
                model.Contract = contractResponse.Value;
            }
            else
            {
                model.Contract = null;
            }

            var companyId = model.Contract?.Company?.ID ?? 0;
            if (companyId > 0)
            {
                var companyResponse = companyService.GetCompanyByID(companyId);
                if (companyResponse.Success)
                {
                    model.Company = companyResponse.Value;
                }

                var invoiceResponse = projectService.GetProjectIncommingInvoicesByCompany(projectid, companyId);
                if (invoiceResponse.Success)
                {
                    model.IncommingInvoices = invoiceResponse.Values
                        .OrderByDescending(m => m.IncommingInvoiceDate)
                        .ToList();
                }

                // Laad alle contracten van deze leverancier voor dit project
                var allContractsResponse = projectService.GetProjectContracts(projectid);
                if (allContractsResponse.Success)
                {
                    model.Contracts = allContractsResponse.Values
                        .Where(c => c.Company?.ID == companyId)
                        .ToList();
                }
            }

            model.HasContract = model.Contracts.Any();

            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var contractDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContract", "Projecten", model.Contract?.Company?.Display ?? "Contract detail")
            {
                Parent = projectContracts,
                RouteValues = new { projectid = projectid, contractid = contractid }
            };
            ViewData["BreadcrumbNode"] = contractDetail;

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Leverancier detail", FromAction = "DetailContracts")]
        public ActionResult DetailSupplier(int projectid, int companyid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectContractDetailModel();
            var projectService = _projectService;
            var companyService = _companyService;

            model.ProjectId = projectid;
            model.ProjectName = projectService.GetProjectNameById(projectid);
            model.Contract = null;

            var companyResponse = companyService.GetCompanyByID(companyid);
            if (companyResponse.Success)
            {
                model.Company = companyResponse.Value;
            }

            var invoiceResponse = projectService.GetProjectIncommingInvoicesByCompany(projectid, companyid);
            if (invoiceResponse.Success)
            {
                model.IncommingInvoices = invoiceResponse.Values
                    .OrderByDescending(m => m.IncommingInvoiceDate)
                    .ToList();
            }

            // Laad alle contracten van deze leverancier voor dit project
            var allContractsResponse = projectService.GetProjectContracts(projectid);
            if (allContractsResponse.Success)
            {
                model.Contracts = allContractsResponse.Values
                    .Where(c => c.Company?.ID == companyid)
                    .ToList();
            }
            model.HasContract = model.Contracts.Any();

            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var supplierDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSupplier", "Projecten", model.Company?.Bedrijfsnaam ?? "Leverancier detail")
            {
                Parent = projectContracts,
                RouteValues = new { projectid = projectid, companyid = companyid }
            };
            ViewData["BreadcrumbNode"] = supplierDetail;

            return View("DetailContract", model);
        }

        [HttpGet]
        //[Breadcrumb("Nacalculatie")]
        [Breadcrumb("Nacalculatie", FromAction = "Detail")]
        public ActionResult Recalculation(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ProjectContractsModel model = new ProjectContractsModel();
            var service = _projectService;
            var aservice = _activityService;
            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);
            // Get Units
            var response = aservice.GetActivityGroups();
            model.ActivityGroups = response.Values;
            var response2 = service.GetProjectContracts(projectid);
            model.Contracts = response2.Values;
            var response3 = service.GetProjectBudget(projectid);
            model.BudgetActivities = response3.Values;
            var response4 = service.GetProjectIncommingInvoicesForRecalculation(projectid);
            model.IncommingInvoicesActivities = response4.Values;

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectRecalc;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectCalculation = _ps.HasWrite(PermissionCodes.ProjectsPostCalculation);

            return View(model);
        }
        [HttpGet]
        public IActionResult PrintRecalculation(int projectid)
        {
            // 1) Haal het model op zoals je Recalculation-view dat ook doet
            ProjectContractsModel model = new ProjectContractsModel();
            var service = _projectService;
            var aservice = _activityService;
            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);
            // Get Units
            var response = aservice.GetActivityGroups();
            model.ActivityGroups = response.Values;
            var response2 = service.GetProjectContracts(projectid);
            model.Contracts = response2.Values;
            var response3 = service.GetProjectBudget(projectid);
            model.BudgetActivities = response3.Values;
            var response4 = service.GetProjectIncommingInvoicesForRecalculation(projectid);
            model.IncommingInvoicesActivities = response4.Values;
            if (model == null)
                return NotFound();

            // 2) Logo laden
            byte[] logoBytes = null;
            var logoPath = Path.Combine(_env.WebRootPath, "Img", "logo.png");
            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            // 3) Optioneel: Avenir registreren (indien TTF’s aanwezig)
            //    Plaats je TTF’s in: wwwroot/fonts/avenir/
            //    Pas bestandsnamen aan indien nodig.
            string fontFamily = null;

            var fontsRoot = Path.Combine(_env.WebRootPath, "fonts");
            var regular = Path.Combine(fontsRoot, "Avenir-Light.ttf");
            var bold = Path.Combine(fontsRoot, "Avenir-Heavy.ttf");
            var italic = Path.Combine(fontsRoot, "Avenir-LightOblique.ttf");
            var boldIt = Path.Combine(fontsRoot, "Avenir-HeavyOblique.ttf");


            try
            {
                if (System.IO.File.Exists(regular))
                    using (var stream = System.IO.File.OpenRead(regular))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(bold))
                    using (var stream = System.IO.File.OpenRead(bold))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(italic))
                    using (var stream = System.IO.File.OpenRead(italic))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(boldIt))
                    using (var stream = System.IO.File.OpenRead(boldIt))
                        FontManager.RegisterFont(stream);

                fontFamily = "Avenir";   // moet exact overeenkomen met de internal name in het TTF
            }
            catch
            {
                // fallback naar default font
            }

            // 4) Document genereren (landscape + logo + fontFamily)
            var document = new RecalculationReportDocument(model, logoBytes, fontFamily);
            var pdfBytes = document.GeneratePdf();

            var safeProject = (model.ProjectName ?? "Project").Replace(Path.GetInvalidFileNameChars(), '_');
            var fileName = $"Nacalculatie_{safeProject}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        [Breadcrumb("Nacalculatie Detail", FromAction = "Recalculation")]
        //[Breadcrumb("Nacalculatie detail")]
        public ActionResult RecalculationDetail(int projectId, int activityId, int groupid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectRecalculationDetailModel();
            var projectService = _projectService;
            var activityService = _activityService;

            model.ProjectId = projectId;
            model.ActivityID = activityId;
            model.GroupID = groupid;
            model.ProjectName = projectService.GetProjectNameById(projectId);

            //var node = SiteMaps.Current.CurrentNode;
            //if (node?.ParentNode?.ParentNode?.ParentNode != null)
            //{
            //    node.ParentNode.ParentNode.Title = projectService.GetProjectNameById(model.ProjectId);
            //}

            var activityResponse = activityService.GetActivitybyId(activityId);
            model.Activity = activityResponse.Value;

            var groupResponse = activityService.GetActivityGroups();
            model.ActivityGroups = groupResponse.Values;
            //if (node != null)
            //{
            //    node.Title = model.Activity.Name;
            //}

            var invoicesResponse = projectService.GetProjectIncommingInvoicesByGroup(projectId, groupid);
            model.IncommingInvoicesActivities = invoicesResponse.Values;
            var response2 = projectService.GetProjectContracts(projectId);
            model.Contracts = response2.Values;
            var response3 = projectService.GetProjectBudget(projectId);
            model.BudgetActivities = response3.Values;

            var contractsResponse = projectService.GetProjectContractsWithoutInvoices(projectId, activityId);
            model.ContractsWithoutInvoices = contractsResponse.Values;

            var contractActivitiesResponse = projectService.GetProjectContractActivitiesByActivityId(projectId, activityId);
            if (contractActivitiesResponse.Success)
            {
                model.ContractActivities = contractActivitiesResponse.Values;
            }

            string breadcrumbTitle;
                var group = model.ActivityGroups?.FirstOrDefault(g => g.ID == groupid);
                breadcrumbTitle = group != null
                    ? group.Name
                    : "Groep";

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectId }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectId }
            };
            var projectRecalcAct = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("RecalculationDetail", "Projecten", breadcrumbTitle)
            {
                Parent = projectRecalc,
                RouteValues = new {
                    projectId = projectId,
                    activityId = activityId,
                    groupid = groupid
                }
            };
            ViewData["BreadcrumbNode"] = projectRecalcAct;

            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contract toevoegen", FromAction = "DetailContracts")]
        //[Breadcrumb("Contract toevoegen")]
        public ActionResult AddContract(int projectid, int contractid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            //Fill model
            ProjectAddContractModel model = new ProjectAddContractModel();
            var service = _projectService;
            model.ProjectId = projectid;

            if (contractid == 0)
            {
                model.Contract.ProjectId = projectid;
                model.Contract.GuaranteeType = ContractGuaranteeType.NoGuarantee;
            }
            else
            {
                var cresponse = service.GetContract(contractid);
                if (cresponse.Success)
                    model.Contract = cresponse.Value;
            }
            model.ProjectName = service.GetProjectNameById(projectid);

            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            var iservice = _insuranceService;
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectContractsAdd = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddContract", "Projecten", "Toevoegen")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = projectContractsAdd;

            return View(model);
        }
        [HttpPost]
        public ActionResult AddContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders)
        {

            var errors = new Dictionary<string, List<string>>();
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);

            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    errors[key] = state.Errors.Select(e => e.ErrorMessage).ToList();
                }
            }

            if ((!ModelState.IsValid))
                return View(model);
            if ((ModelState.IsValid))
            {
                //Referrer
                var referrer = TempData["Referrer"] as string
                    ?? Url.Action("DetailContracts", "Projecten", new { projectid = model.ProjectId });
                foreach (var contractactivity in activities)
                {
                    if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                    {
                        InsuranceBO i = new InsuranceBO();
                        i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                        contractactivity.InsuranceData = i;
                    }
                    model.Contract.Activities.Add(contractactivity);
                }

                var service = _projectService;
                var response = service.InsertUpdateProjectContract(model.Contract);
                if (response.Success)
                {
                    AddMessage("success", "Het contract is toegevoegd aan het project " + model.ProjectName, "Geslaagd!");
                    return Redirect(referrer);
                }
                else
                {
                    AddMessage("error", "Het contract is NIET toegevoegd aan het project " + model.ProjectName, "Fout!");
                    return View(model);
                }
            }
            else
                return View(model);
        }
        [HttpGet]
        //[Breadcrumb("Contract bewerken")]
        [Breadcrumb("Contract bewerken", FromAction = "DetailContracts")]
        public ActionResult EditContract(int projectid, int contractid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            //Fill model
            ProjectAddContractModel model = new ProjectAddContractModel();
            var service = _projectService;
            model.ProjectId = projectid;

            if (contractid == 0)
            {
                model.Contract.ProjectId = projectid;
                model.Contract.GuaranteeType = ContractGuaranteeType.NoGuarantee;
            }
            else
            {
                var cresponse = service.GetContract(contractid);
                if (cresponse.Success)
                    model.Contract = cresponse.Value;
            }
            model.ProjectName = service.GetProjectNameById(projectid);
            var pservice = _companyService;
            var presponse = pservice.GetCompanyActivities(model.Contract.Company.ID);
            var activitiesList = new List<IdNameBO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Values)
                {
                    var singleActivity = new IdNameBO
                    {
                        ID = selectedActivity.ID,
                        Display = selectedActivity.Name,
                        Group = "-Bedrijfsactiviteit-"
                    };
                    activitiesList.Add(singleActivity);
                }
            }
            model.Activities = activitiesList;
            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);
            var iservice = _insuranceService;
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectContractsEdit = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditContract", "Projecten", "Toevoegen")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = projectContractsEdit;

            return View(model);
        }
        [HttpPost]
        public ActionResult EditContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders)
        {

            var errors = new Dictionary<string, List<string>>();
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);

            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    errors[key] = state.Errors.Select(e => e.ErrorMessage).ToList();
                }
            }

            if ((!ModelState.IsValid))
                return View(model);
            if ((ModelState.IsValid))
            {
                //Referrer
                var referrer = TempData["Referrer"] as string
                    ?? Url.Action("DetailContracts", "Projecten", new { projectid = model.ProjectId });
                foreach (var contractactivity in activities)
                {
                    if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                    {
                        InsuranceBO i = new InsuranceBO();
                        i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                        contractactivity.InsuranceData = i;
                    }
                    model.Contract.Activities.Add(contractactivity);
                }

                var service = _projectService;
                var response = service.InsertUpdateProjectContract(model.Contract);
                if (response.Success)
                {
                    AddMessage("success", "Het contract is bijgewerkt voor project " + model.ProjectName, "Geslaagd!");
                    return Redirect(referrer);
                }
                else
                {
                    AddMessage("error", "Het contract is NIET bijgewerkt voor project " + model.ProjectName, "Fout!");
                    return View(model);
                }
            }
            else
                return View(model);
        }
        [HttpGet]
        public ActionResult ModalDeleteContract(int id)
        {
            var viewModel = new ContractBO();

            if (id != 0)
            {
                var dservice = _projectService;
                var response = dservice.GetContract(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = GetCompanyName(viewModel.Company.ID);
                }
            }

            return PartialView("_ModalDeleteContract", viewModel);
        }
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsSuppliers)]
        public ActionResult DeleteContract(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = _projectService;
                var ids = new List<int> { id };
                var response = service.DeleteContracts(ids);

                if (response.Success)
                {
                    AddMessage("success", "Het contract is verwijderd", "Geslaagd!");
                    return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "Het contract is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
                }
            }

            return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
        }

        public ActionResult GetSubType(int id)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            var service2 = _unitService;

            var responselevels = service2.GetUnitTypesByGroupId(id);
            List<IdNameBO> iList = new List<IdNameBO>();
            IdNameBO bo = new IdNameBO();
            if ((responselevels.Success))
            {
                foreach (var type in responselevels.Values)
                {
                    bo = new IdNameBO();
                    bo.ID = type.Id;
                    bo.Display = type.Name;
                    iList.Add(bo);
                }
            }
            return Json(iList);
        }
        [HttpPost]
        public JsonResult GetCompanyActivities(int companyid)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyActivities(companyid);

            var activitiesList = new List<Select2DTO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Values)
                {
                    var singleActivity = new Select2DTO
                    {
                        id = selectedActivity.ID,
                        text = selectedActivity.Name,
                        group = "-Bedrijfsactiviteit-"
                    };
                    activitiesList.Add(singleActivity);
                }
            }

            return Json(activitiesList);
        }

        [HttpPost]
        public JsonResult GetContractActivities(int contractid)
        {
            var pservice = _projectService;
            var presponse = pservice.GetContract(contractid);

            var activitiesList = new List<Select2DTO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Value.Activities)
                {
                    var singleActivity = new Select2DTO
                    {
                        id = selectedActivity.ContractActivityId,
                        text = selectedActivity.Activity.Name
                    };
                    activitiesList.Add(singleActivity);
                }
            }

            return Json(activitiesList);
        }
        [HttpPost]
        public PartialViewResult AddSelectedActivities(int ActivityId, string ActivityName)
        {
            var nContractActivity = new ContractActivityBO();
            var nActivity = new ActivityBO
            {
                ID = ActivityId,
                Name = ActivityName
            };
            nContractActivity.Activity = nActivity;

            ViewData["mode"] = "add";
            return PartialView("_ActivityRow", nContractActivity);
        }
        [HttpPost]
        public PartialViewResult AddAdditionalOrders(int contractActivityId, string activityName)
        {
            var nAdditionalOrder = new ContractAdditionalOrderBO
            {
                ContractActivityId = contractActivityId,
                ActivityName = activityName
            };

            ViewData["mode"] = "add";
            return PartialView("_AdditionalOrderRow", nAdditionalOrder);
        }
        [Breadcrumb("Budget instellen", FromAction = "DetailContracts")]
        [HttpGet]
        public IActionResult CalculationSettings(int projectid)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectCalculationSettings
            {
                ProjectId = projectid,
                ProjectName = _projectService.GetProjectNameById(projectid)
            };

            // Groups
            var aservice = _activityService;
            var groupsResp = aservice.GetActivityGroups();
            model.ActivityGroups = groupsResp.Values?.ToList() ?? new();

            // Reeds ingestelde budgetregels
            var pservice = _projectService;
            var budgetResp = pservice.GetProjectBudget(projectid);
            model.BudgetActivities = budgetResp.Values?.ToList() ?? new();

            // Select2-lijst (als optgroups)
            var listResp = aservice.GetActivitiesForSelect();
            if (listResp.Success)
            {
                model.ListActivities = listResp.Values.Select(x => new IdNameBO
                {
                    ID = x.ID,
                    Display = x.Display,
                    Group = x.Group,
                    GroupId = x.GroupId
                }).ToList();
            }

            // Preselecteer wat al op budget staat
            if (model.BudgetActivities.Any())
                model.SelectedActivities = model.BudgetActivities.Select(b => b.Activity.ID).Distinct().ToList();

            ViewData["Title"] = $"Project - {model.ProjectName}";

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalcAct = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("CalculationSettings", "Projecten", "Budget instellen")
            {
                Parent = projectRecalc,
                RouteValues = new
                {
                    projectId = projectid
                }
            };
            ViewData["BreadcrumbNode"] = projectRecalcAct;


            return View(model);
        }

        [HttpPost]
        public IActionResult CalculationSettings(ProjectCalculationSettings model, List<BudgetActivityBO> budgetactivities)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ProjectId meegeven aan alle regels
            if (budgetactivities != null)
            {
                // verwijder null items
                budgetactivities = budgetactivities
                    .Where(b => b != null)
                    .ToList();

                // projectId invullen
                budgetactivities.ForEach(b => b.ProjectId = model.ProjectId);
            }

            var service = _projectService;
            var resp = service.InsertUpdateProjectBudgetActivities(budgetactivities ?? new(), model.ProjectId);

            if (resp.Success)
            {
                TempData["MessageType"] = "success";
                TempData["MessageTitle"] = "Geslaagd!";
                TempData["Message"] = "De activiteiten zijn aan het budget toegevoegd.";
                return RedirectToAction("Recalculation", "Projecten", new { projectid = model.ProjectId });
            }

            TempData["MessageType"] = "error";
            TempData["MessageTitle"] = "Fout!";
            TempData["Message"] = "De activiteiten zijn NIET aan het budget toegevoegd.";
            return View(model);
        }

        [HttpPost]
        public IActionResult AddBudgetActivity(int actId)
        {
            var aservice = _activityService;
            var response = aservice.GetActivitybyId(actId);
            var act = response.Value;

            var budget = new BudgetActivityBO
            {
                Activity = act
            };

            return PartialView("_BudgetActivityRow", budget);
        }

        // ========== PROJECT - INKOMENDE FACTUREN ==========

        [HttpGet]
        //[Breadcrumb("Inkomende factuur toevoegen")]
        [Breadcrumb("Inkomende factuur toevoegen", FromAction = "DetailContracts")]
        public ActionResult AddIncommingInvoice(int projectid, int type, int invoiceid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectIncommingInvoiceAddUpdateModel();

            var service2 = _projectService;
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            // Get the activities
            var service = _activityService;
            var response = service.GetActivitiesForSelect();

            if (response.Success)
            {
                model.ListActivities = response.Values;
            }

            model.ListActivities = model.ListActivities.OrderBy(m => m.Group).ToList();

            if (type == 0)
            {
                var i = new IdNameBO { Group = "Contractactiviteiten" };
                model.ListActivities.Insert(0, i);
            }
            else if (type == 1)
            {
                var i = new IdNameBO { Group = "Bedrijfsactiviteiten" };
                model.ListActivities.Insert(0, i);
            }

            //if (invoiceid != 0)
            //{
            //    var chresponse = service2.GetIncommingInvoice(invoiceid);
            //    if (chresponse.Success)
            //    {
            //        model.IncommingInvoice = chresponse.Values.FirstOrDefault();
            //    }
            //    model.Type = type;
            //}
            //else
            //{
            model.Type = type;
            model.IncommingInvoice = new IncommingInvoiceBO();
            model.IncommingInvoice.IncommingInvoiceDate = DateOnly.FromDateTime(DateTime.Now);
            model.IncommingInvoice.ContractID = 0;
            //}

            IncommingInvoiceFillInSelectList(model);


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddIncommingInvoice", "Projecten", "Factuur toevoegen")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        [HttpPost]
        public ActionResult AddIncommingInvoice(ProjectIncommingInvoiceAddUpdateModel model, List<IncommingInvoiceDetailBO> details)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .Select(x => new {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                    })
                    .ToList();
            }
            // Koppel elk detail aan de factuur en voeg toe aan het model
            foreach (var invoiceRow in details)
            {
                invoiceRow.IncommingInvoiceID = model.IncommingInvoice.Id;
                model.IncommingInvoice.Details.Add(invoiceRow);
            }

            if (model.IncommingInvoice.ContractID is null && model.IncommingInvoice.CompanyId is null)
            {
                ModelState.AddModelError("CustomError", "Er is geen bedrijf geselecteerd");
            }

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.InvoicePrice != details.Sum(m => m.Price))
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuuronderdelen komt niet overeen met de totale factuurprijs");
            }
            if (model.IncommingInvoice.InvoicePrice == 0)
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuur is niet ingegeven");
            }

            // Als het model ongeldig is, toon de view opnieuw met foutmeldingen
            if (!ModelState.IsValid)
            {

                ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
                IncommingInvoiceFillInSelectList(model);

                if (model.IncommingInvoice.ContractID != 0)
                {
                    // Indien er ooit logica nodig is om contractactiviteiten op te halen, hier plaatsen
                }

                var activityService = _activityService;
                var response = activityService.GetActivitiesForSelect();
                if (response.Success)
                {
                    model.ListActivities = response.Values.OrderBy(m => m.Group).ToList();
                }

                var label = model.Type == 0 ? "Contractactiviteiten" : "Bedrijfsactiviteiten";
                model.ListActivities.Insert(0, new IdNameBO { Group = label });

                return View(model);
            }
            var Referrer = TempData["Referrer"];
            // Als het model wél geldig is, bewaar de factuur
            var projectService = _projectService;
            model.IncommingInvoice.ProjectId = model.ProjectId;

            var saveResponse = projectService.InsertUpdateProjectIncommingInvoice(model.IncommingInvoice);

            if (saveResponse.Success)
            {
                AddMessage("success", $"De factuur is toegevoegd aan het project {model.ProjectName}", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }
            else
            {
                AddMessage("error", $"De factuur is NIET toegevoegd aan het project {model.ProjectName}", "Fout!");
                return View(model);
            }
        }
        [HttpGet]
        //[Breadcrumb("Inkomende factuur bewerken")]
        [Breadcrumb("Inkomende factuur bewerken", FromAction = "DetailContracts")]
        public ActionResult EditIncommingInvoice(int projectid, int invoiceid)
        {
            // Referrer bijhouden

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectIncommingInvoiceAddUpdateModel
            {
                ProjectId = projectid
            };

            // Projectgegevens ophalen
            var projectService = _projectService;
            model.ProjectName = projectService.GetProjectNameById(projectid);

            // Activiteiten ophalen
            var activityService = _activityService;
            var activityResponse = activityService.GetActivitiesForSelect();
            if (activityResponse.Success)
            {
                model.ListActivities = activityResponse.Values
                    .OrderBy(a => a.Group)
                    .ToList();
            }

            // Factuur ophalen
            var invoiceResponse = projectService.GetIncommingInvoice(invoiceid);
            if (invoiceResponse.Success)
            {
                model.IncommingInvoice = invoiceResponse.Values.FirstOrDefault();
            }

            // Type bepalen en optionele group header toevoegen
            if (model.IncommingInvoice?.ContractID is not null)
            {
                model.Type = 0;
                model.ListActivities.Insert(0, new IdNameBO { Group = "Contractactiviteiten" });
            }
            else
            {
                model.Type = 1;
                model.ListActivities.Insert(0, new IdNameBO { Group = "Bedrijfsactiviteiten" });
            }

            // Bedrijfsnaam bepalen op basis van contract of companyId
            var companyService = _companyService;
            if (model.IncommingInvoice?.ContractID is null || model.IncommingInvoice.ContractID == 0)
            {
                model.CompanyName = companyService.GetCompanyNameById((int)model.IncommingInvoice.CompanyId);
            }
            else
            {
                model.CompanyName = companyService.GetCompanyNameByContractId((int)model.IncommingInvoice.ContractID);
            }

            // Selectielijsten vullen
            IncommingInvoiceFillInSelectList(model);


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditIncommingInvoice", "Projecten", "Factuur bewerken")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;


            return View(model);
        }
        [HttpPost]
        public ActionResult EditIncommingInvoice(ProjectIncommingInvoiceAddUpdateModel model, List<IncommingInvoiceDetailBO> details)
        {



            // Koppel elk detail aan de factuur en voeg toe aan het model
            foreach (var invoiceRow in details)
            {
                invoiceRow.IncommingInvoiceID = model.IncommingInvoice.Id;
                model.IncommingInvoice.Details.Add(invoiceRow);
            }

            DecimalNormalizationHelper.NormalizeDecimals(model, Request.Form);

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.ContractID is null && model.IncommingInvoice.CompanyId is null)
            {
                ModelState.AddModelError("CustomError", "Er is geen bedrijf geselecteerd");
            }

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.InvoicePrice != details.Sum(m => m.Price))
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuuronderdelen komt niet overeen met de totale factuurprijs");
            }
            if (model.IncommingInvoice.InvoicePrice == 0)
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuur is niet ingegeven");
            }

            // Als het model ongeldig is, toon de view opnieuw met foutmeldingen
            if (!ModelState.IsValid)
            {


                IncommingInvoiceFillInSelectList(model);

                if (model.IncommingInvoice.ContractID != 0)
                {
                    // Indien er ooit logica nodig is om contractactiviteiten op te halen, hier plaatsen
                }

                var activityService = _activityService;
                var response = activityService.GetActivitiesForSelect();
                if (response.Success)
                {
                    model.ListActivities = response.Values.OrderBy(m => m.Group).ToList();
                }

                var label = model.Type == 0 ? "Contractactiviteiten" : "Bedrijfsactiviteiten";
                model.ListActivities.Insert(0, new IdNameBO { Group = label });

                return View(model);
            }
            var Referrer = TempData["Referrer"];
            // Als het model wél geldig is, bewaar de factuur
            var projectService = _projectService;
            model.IncommingInvoice.ProjectId = model.ProjectId;

            var saveResponse = projectService.InsertUpdateProjectIncommingInvoice(model.IncommingInvoice);

            if (saveResponse.Success)
            {
                AddMessage("success", $"De factuur is aangepast aan het project {model.ProjectName}", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }
            else
            {
                AddMessage("error", $"De factuur is NIET aangepast aan het project {model.ProjectName}", "Fout!");
                return View(model);
            }
        }
        [HttpPost]
        public PartialViewResult AddIncommingInvoiceDetailRow(int ActivityId, string ActivityName, int ContractId, int CompanyId)
        {
            var nIncommingInvoiceDetail = new IncommingInvoiceDetailBO();
            var service = _projectService;
            var response = service.GetContractChangeOrdersForSelect(ContractId);

            if (response.Success)
            {
                nIncommingInvoiceDetail.ChangeOrders = response.Values;
            }

            if (ContractId == 0)
            {
                nIncommingInvoiceDetail.ActivityID = ActivityId;
                nIncommingInvoiceDetail.ContractActivityText = ActivityName;
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Geen_Contract;
            }
            else
            {
                nIncommingInvoiceDetail.ContractActivityID = ActivityId;
                nIncommingInvoiceDetail.ContractActivityText = ActivityName;
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Contract;
            }

            ViewData["mode"] = "add";

            return PartialView("_IncommingInvoiceDetailRow", nIncommingInvoiceDetail);
        }
        [HttpGet]
        public ActionResult ModalDeleteIncommingInvoice(int id, string companyname)
        {
            var viewModel = new IncommingInvoiceBO();

            if (id != 0)
            {
                var dservice = _projectService;
                var response = dservice.GetIncommingInvoice(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = companyname;
                }
            }

            return PartialView("_ModalDeleteIncommingInvoice", viewModel);
        }
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsSuppliers)]
        public ActionResult DeleteIncommingInvoice(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = _projectService;
                var ids = new List<int> { id };
                var response = service.DeleteIncommingInvoices(ids);

                if (response.Success)
                {
                    AddMessage("success", "De factuur is verwijderd", "Geslaagd!");
                    return RedirectToAction("Recalculation", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "De factuur is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("Recalculation", "Projecten", new { projectid = projectid });
                }
            }
            var Referrer = TempData["Referrer"];
            return Redirect(Referrer.ToString());
        }
        [HttpGet]
        //[Breadcrumb("Inkomende factuur")]
        [Breadcrumb("Inkomende factuur", FromAction = "DetailContracts")]
        public ActionResult IncommingInvoiceDetail(int projectid, int invoiceid)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectIncommingInvoiceModel();

            var service = _companyService;
            var service2 = _projectService;
            int companyid = 0;
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            var response = service2.GetIncommingInvoice(invoiceid);
            if (response.Success)
            {
                model.IncommingInvoice = response.Value;
            }
            companyid = model.IncommingInvoice.CompanyId ?? companyid;
            if (model.IncommingInvoice.ContractID is not null)
            {
                var response3 = service2.GetContract((int)model.IncommingInvoice.ContractID);
                if (response3.Success)
                {
                    model.Contract = response3.Value;
                }
            }
            if (companyid == 0)
            {
                companyid = model.Contract?.Company?.ID ?? companyid;
            }

            var response2 = service.GetCompanyByID(companyid);
            if (response2.Success)
            {
                model.Company = response2.Value;
            }

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddIncommingInvoice", "Projecten", "Inkomende factuur")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _psInv = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _psInv.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _psInv.HasDelete(PermissionCodes.ProjectsSuppliers);

            return View(model);
        }

        // ========== WIJZIGINGSOPDRACHTEN KLANTEN/PROJECTEN ==========

        [HttpGet]
        public ActionResult DetailsChangeOrder(int? projectid, int? clientid)
        {
            if ((projectid ?? 0) <= 0 && (clientid ?? 0) <= 0)
            {
                AddMessage("error", "Gelieve een project of klant te selecteren.", "Fout!");
                return RedirectToAction("Index", "Projecten");
            }

            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Referrer"] = refHeader;
            }

            var model = new DetailChangeOrderModel();
            var projectService = _projectService;
            var clientService = _clientService;

            if ((projectid ?? 0) > 0)
            {
                model.ProjectId = projectid!.Value;
                model.ProjectName = projectService.GetProjectNameById(model.ProjectId);

                var clientsResponse = clientService.GetClientAccountsByProjectIdForSelect(model.ProjectId);
                if (clientsResponse.Success)
                {
                    model.Clients = clientsResponse.Values;
                }
            }

            if ((clientid ?? 0) > 0)
            {
                model.ClientAccountId = clientid!.Value;
                model.ClientName = clientService.GetClientAccountNameById(model.ClientAccountId);
            }

            if ((projectid ?? 0) > 0)
            {
                var response = projectService.GetProjectChangeOrders(model.ProjectId);
                if (response.Success)
                {
                    model.CO = (clientid ?? 0) > 0
                        ? response.Values.Where(co => co.ClientAccountID == model.ClientAccountId).ToList()
                        : response.Values;
                }
            }
            else if ((clientid ?? 0) > 0)
            {
                var response = projectService.GetClientChangeOrders(0, model.ClientAccountId);
                if (response.Success)
                {
                    model.CO = response.Values;
                }
            }

            var unitsLookup = new Dictionary<int, string>();
            foreach (var changeOrder in model.CO)
            {
                if (changeOrder.ClientAccountID <= 0 || unitsLookup.ContainsKey(changeOrder.ClientAccountID))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(changeOrder.ClientName))
                {
                    changeOrder.ClientName = clientService.GetClientAccountNameById(changeOrder.ClientAccountID);
                }

                unitsLookup[changeOrder.ClientAccountID] =
                    clientService.GetClientAccountUnitsNameById(changeOrder.ClientAccountID);
            }

            model.ClientUnits = unitsLookup;

            // BREADCRUMBS: Home / Projectnaam / Wijzigingsopdrachten
            if (model.ProjectId > 0)
            {
                var homeNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Home");
                var projectNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
                {
                    Parent = homeNode,
                    RouteValues = new { projectid = model.ProjectId }
                };
                var changeOrdersNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailsChangeOrder", "Projecten", "Wijzigingsopdrachten")
                {
                    Parent = projectNode,
                    RouteValues = new { projectid = model.ProjectId, clientid = model.ClientAccountId > 0 ? (int?)model.ClientAccountId : null }
                };

                ViewData["BreadcrumbNode"] = changeOrdersNode;
            }

            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectChangeOrders = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);
            ViewBag.CanDeleteProjectChangeOrders = _ps.HasDelete(PermissionCodes.ProjectsChangeOrders);

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Wijzigingsopdracht toevoegen", FromController = typeof(KlantenController), FromAction = nameof(KlantenController.Detail))]
        //[Breadcrumb("Wijzigingsopdracht toevoegen")]
        public ActionResult AddChangeOrder(int projectid, int type, int clientaccountid = 0)
        {
            // 1) Veilige referrer voor je "terug"-link (enkel van dezelfde host)
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Referrer"] = refHeader;
            }

            // 2) Services ophalen
            var projectService = _projectService;
            var clientService = _clientService;

            // 3) Model opbouwen (zorgt ervoor dat ChangeOrder nooit null is)
            var model = new ProjectChangeOrderAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid),
                ChangeOrder = new ChangeOrderBO
                {
                    // Als je 'type' wil mappen naar een enum, zie comment onderaan.
                    ChangeOrderConditions = DefaultChangeOrderConditions,
                    // DateOnly: gebruik Today voor datum‑zonder‑tijd
                    ChangeOrderDate = DateOnly.FromDateTime(DateTime.Today),
                    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
                }
            };

            if (clientaccountid > 0)
            {
                model.ChangeOrder.ClientAccountID = clientaccountid;
                model.ClientName = clientService.GetClientAccountNameById(clientaccountid);

            }



            // 4) Minstens één detailrij
            if (model.ChangeOrder.Details == null || model.ChangeOrder.Details.Count == 0)
            {
                model.ChangeOrder.Details = new List<ChangeOrderDetailBO>
        {
            new ChangeOrderDetailBO
            {
                MeasurementType = MeasurementType.Vermoedelijk,
                MeasurementUnit = MeasurementUnit.stuk,
                Number = 1,
                Commision = 20,
                VatPercentage = 21m,

            }
        };
            }

            // 5) Dropdowns / selects vullen
            ChangeOrderFillInSelectList(model);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddChangeOrder", "Projecten", "Wijzingsopdracht toevoegen")
            {
                Parent = projectKlanten,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        private const string DefaultChangeOrderConditions =
            "Het bedrag zal verrekend worden bij de laatste facturatieschijf 'voorlopige oplevering'";

        [HttpPost]
        public ActionResult AddChangeOrder(ProjectChangeOrderAddUpdateModel model, List<ChangeOrderDetailBO> Details)
        {

            // Merge posted details
            if (Details != null)
            {
                foreach (var d in Details)
                    model.ChangeOrder.Details.Add(d);
            }
            if (model.ChangeOrder.ClientAccountID <= 0)
            {
                ModelState.AddModelError(nameof(model.ChangeOrder.ClientAccountID), "Gelieve een klant te selecteren.");
            }

            // Early return on invalid model
            if (!ModelState.IsValid)
            {
                ChangeOrderFillInSelectList(model);
                return View(model);
            }

            var service = _projectService;
            var response = service.InsertUpdateProjectChangeOrder(model.ChangeOrder);

            if (response.Success)
            {
                var Referrer = TempData["Referrer"];
                AddMessage("success", "De wijzigingsopdracht is toegevoegd", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }

            AddMessage("error", "De wijzigingsopdracht is NIET toegevoegd", "Fout!");
            ChangeOrderFillInSelectList(model);
            return View(model);
        }

        [HttpGet]
        public PartialViewResult AddChangeOrderDetailRow()
        {
            var model = new ChangeOrderDetailBO
            {
                MeasurementType = MeasurementType.Vermoedelijk,
                MeasurementUnit = MeasurementUnit.stuk,
                Number = 1,
                Price = 0m,
                Commision = 20,
                VatPercentage = 21m,
            };
            return PartialView("_ChangeOrderDetailRow", model);
        }

        [HttpGet]
        public ActionResult DuplicateChangeOrder(int projectid, int coid)
        {
            if (projectid <= 0 || coid <= 0)
            {
                AddMessage("error", "Ongeldige wijzigingsopdracht.", "Fout!");
                return RedirectToAction("Detail", "Projecten", new { projectid });
            }

            var projectService = _projectService;
            var clientService = _clientService;

            var model = new ProjectChangeOrderAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid)
            };

            var resp = projectService.GetChangeOrder(coid);
            if (resp?.Success == true)
            {
                var co = resp.Values?.FirstOrDefault();
                if (co != null)
                {
                    co.Id = 0;
                    co.DateSendToClient = null;
                    co.DateAgreement = null;
                    if (co.Details != null)
                    {
                        foreach (var detail in co.Details)
                        {
                            detail.Id = 0;
                            detail.ChangeOrderID = 0;
                        }
                    }
                    model.ChangeOrder = co;
                    model.ClientName = clientService.GetClientAccountNameById(co.ClientAccountID);
                }
            }
            else
            {
                AddMessage("error", "Kon de wijzigingsopdracht niet dupliceren.", "Fout!");
            }

            ChangeOrderFillInSelectList(model);
            TempData["Referrer"] = Url.Action("DetailsChangeOrder", "Projecten", new { projectid });
            return View("AddChangeOrder", model);
        }

        [HttpGet]
        //[Breadcrumb("Wijzigingsopdracht bewerken")]
        [Breadcrumb("Wijzigingsopdracht bewerken", FromController = typeof(KlantenController), FromAction = nameof(KlantenController.Detail))]
        public ActionResult EditChangeOrder(int projectid, int clientid, int coid)
        {
            // 0) Basisvalidatie
            if (projectid <= 0)
            {
                AddMessage("error", "Ongeldig project.", "Fout!");
                return RedirectToAction("Index", "Projecten");
            }

            // 1) Veilige referrer (relative URL bewaren) + fallback
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
                Url.IsLocalUrl(refUri.PathAndQuery))
            {
                TempData["Referrer"] = refUri.PathAndQuery; // relative is veiliger
            }
            else
            {
                // Fallback als er geen geldige referrer is
                TempData["Referrer"] = Url.Action("Detail", "Projecten", new { projectid, clientid });
            }

            // 2) Services
            var projectService = _projectService;
            var clientService = _clientService;

            // 3) Model opbouwen met veilige defaults
            var model = new ProjectChangeOrderAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid) ?? string.Empty,
                ChangeOrder = new ChangeOrderBO
                {
                    ProjectId = projectid,
                    ClientAccountID = clientid
                }
            };

            // 4) Bestaande CO ophalen (indien coid > 0)
            if (coid > 0)
            {
                var resp = projectService.GetChangeOrder(coid);
                if (resp?.Success == true)
                {
                    var co = resp.Values?.FirstOrDefault();
                    if (co != null)
                    {
                        model.ChangeOrder = co;
                    }
                    else
                    {
                        AddMessage("warning", "De gevraagde wijzigingsopdracht werd niet gevonden.", "Opgelet");
                    }
                }
                else
                {
                    AddMessage("error", "Kon de wijzigingsopdracht niet ophalen.", "Fout!");
                }
            }
            if (string.IsNullOrWhiteSpace(model.ClientName))
            {
                model.ClientName = clientService.GetClientAccountNameById(model.ChangeOrder.ClientAccountID);
            }

            // 5) Dropdowns / Selects vullen
            ChangeOrderFillInSelectList(model);

            // BREADCRUMBS: Home / Projectnaam / Wijzigingsopdrachten / Klantnaam / Omschrijving / Bewerken
            var homeNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Home");
            var projectNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = homeNode,
                RouteValues = new { projectid }
            };
            var changeOrdersNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailsChangeOrder", "Projecten", "Wijzigingsopdrachten")
            {
                Parent = projectNode,
                RouteValues = new { projectid, clientid = model.ChangeOrder.ClientAccountID }
            };
            var clientNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditChangeOrder", "Projecten", model.ClientName)
            {
                Parent = changeOrdersNode,
                RouteValues = new { projectid, clientid = model.ChangeOrder.ClientAccountID, coid = model.ChangeOrder.Id }
            };
            var descriptionNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditChangeOrder", "Projecten", string.IsNullOrWhiteSpace(model.ChangeOrder.Description) ? "Wijzigingsopdracht" : model.ChangeOrder.Description)
            {
                Parent = clientNode,
                RouteValues = new { projectid, clientid = model.ChangeOrder.ClientAccountID, coid = model.ChangeOrder.Id }
            };
            var editNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditChangeOrder", "Projecten", "Bewerken")
            {
                Parent = descriptionNode,
                RouteValues = new { projectid, clientid = model.ChangeOrder.ClientAccountID, coid = model.ChangeOrder.Id }
            };
            ViewData["BreadcrumbNode"] = editNode;

            return View(model);
        }

        [HttpPost]
        public ActionResult EditChangeOrder(ProjectChangeOrderAddUpdateModel model, List<ChangeOrderDetailBO> Details)
        {

            // Merge posted details
            if (Details != null)
            {
                foreach (var d in Details)
                    model.ChangeOrder.Details.Add(d);
            }
            if (model.ChangeOrder.ClientAccountID <= 0)
            {
                ModelState.AddModelError(nameof(model.ChangeOrder.ClientAccountID), "Gelieve een klant te selecteren.");
            }


            // Early return on invalid model
            if (!ModelState.IsValid)
            {
                ChangeOrderFillInSelectList(model);
                return View(model);
            }

            var service = _projectService;
            var response = service.InsertUpdateProjectChangeOrder(model.ChangeOrder);

            if (response.Success)
            {
                var Referrer = TempData["Referrer"];
                AddMessage("success", "De wijzigingsopdracht is bewerkt", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }

            AddMessage("error", "De wijzigingsopdracht is NIET bewerkt", "Fout!");
            ChangeOrderFillInSelectList(model);
            return View(model);
        }
        [HttpGet]
        public ActionResult ModalDeleteChangeOrder(int id)
        {

            var viewModel = new ChangeOrderBO();

            if (id != 0)
            {
                var dservice = _projectService;
                var response = dservice.GetChangeOrder(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                }
            }

            return PartialView("_ModalDeleteChangeOrder", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteChangeOrder(int id)
        {
            if (id == 0)
                return Json(new { success = false, message = "Ongeldig ID." });

            var service = _projectService;
            var response = service.DeleteChangeOrders(new List<int> { id });

            if (response.Success)
            {
                // Optioneel: server-side toast registreren
                AddMessage("success", "De wijzigingsopdracht is verwijderd", "Geslaagd!");

                // Eenvoudigste aanpak: laat de client de pagina (of enkel de lijst) herladen
                return Json(new { success = true, message = "Verwijderd." });
            }

            AddMessage("error", "De wijzigingsopdracht is niet verwijderd. Probeer opnieuw.", "Fout!");
            return Json(new { success = false, message = "Verwijderen mislukt." });
        }
        [HttpGet]
        public IActionResult ChangeOrderPDF(int changeorderid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectChangeOrderExportModel();
            var clientService = _clientService;
            var projectService = _projectService;

            var changeOrderResponse = projectService.GetChangeOrder(changeorderid);
            if (changeOrderResponse.Success)
                model.ChangeOrder = changeOrderResponse.Values.FirstOrDefault();

            var projectResponse = projectService.GetProjectByID(model.ChangeOrder.ProjectId);
            if (projectResponse.Success)
                model.Project = projectResponse.Values.FirstOrDefault();

            var salesSettingsResponse = projectService.GetSalesSettings(model.Project.Id);
            if (salesSettingsResponse.Success)
                model.ProjectSalesSettings = salesSettingsResponse.Values.FirstOrDefault();

            var clientResponse = clientService.GetClientAccountById(model.ChangeOrder.ClientAccountID);
            if (clientResponse.Success)
                model.ClientAccount = clientResponse.Values.FirstOrDefault();

            model.Units = clientService.GetClientAccountUnitsNameById(model.ChangeOrder.ClientAccountID);


            // Gebruik juiste constructor om model + view te combineren
            return new ViewAsPdf("ChangeOrderPDF", model)
            {
                PageOrientation = Orientation.Portrait,
                PageMargins = new Margins(10, 5, 0, 5),
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                FileName = $"Wijzigingsopdracht - {model.Project.Name} {DateTime.Now:yyyyMMdd}_{model.ChangeOrder.Id}.pdf",
                //CustomSwitches = $"--footer-html {Url.Action("ChangeOrderFooter", "Projecten", new { text = model.ChangeOrder.ChangeOrderConditions }, "http")} --footer-spacing 0"
            };
        }
        [AllowAnonymous]
        [HttpGet]
        public PartialViewResult ChangeOrderFooter(string text)
        {
            return PartialView("ChangeOrderFooter", text);
        }
        public IActionResult MinimalTestPDF()
        {
            var pdf = new ViewAsPdf("MinimalTestPDF", "Dit is een test.")
            {
                PageOrientation = Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Margins(10, 5, 40, 5),
                FileName = "MinimalTest.pdf"
            };
            return pdf;
        }

        // ========== WEERVERLET ==========

        [HttpGet]
        //[Breadcrumb("Weerverlet")]
        [Breadcrumb("Weerverlet", FromAction = "Index")]
        public ActionResult Weather()
        {
            var model = new BWDModel();
            var service = _projectService;
            var response = service.GetWheaterstationsSelect();
            if (response.Success)
            {
                model.WeatherStations = response.Values;
            }
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectWeather = _ps.HasWrite(PermissionCodes.ProjectsWeatherDelay);
            return View(model);
        }
        [HttpGet]
        public IActionResult GetCalendarBundle(int weatherstationid, int year)
        {
            var service = _projectService;


            var rain = new List<object>();
            var wind = new List<object>();
            var vacation = new List<object>();

            var rainResponse = service.GetBadWeatherDays(weatherstationid, 0);
            if (rainResponse.Success)
            {
                rain = rainResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Regen/Vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var windResponse = service.GetBadWeatherDays(weatherstationid, 1);
            if (windResponse.Success)
            {
                wind = windResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Wind",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var vacationResponse = service.GetVacationDays();
            if (vacationResponse.Success)
            {
                vacation = vacationResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "verlofdag",
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day,
                    color = "#777"
                }).Cast<object>().ToList();
            }
            return Ok(new { rain = rain, wind = wind, vacation = vacation });
        }

        [HttpGet]
        public JsonResult GetBadWeatherDays(int type, int weatherstationid, int year)
        {
            var service = _projectService;
            var response = service.GetBadWeatherDays(weatherstationid, type);

            var rows = new List<object>();
            var vacationRows = new List<object>();

            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    id = b.Id,
                    title = "vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();

                var response2 = service.GetVacationDays();
                if (response2.Success)
                {
                    vacationRows = response2.Values.Select(b => new
                    {
                        id = b.Id,
                        title = "verlofdag",
                        year = b.VacationDay.Year,
                        month = b.VacationDay.Month,
                        day = b.VacationDay.Day,
                        color = "#777"
                    }).Cast<object>().ToList();
                }
            }

            var allResults = new List<object>(rows.Count + vacationRows.Count);
            allResults.AddRange(rows);
            allResults.AddRange(vacationRows);


            return new JsonResult(allResults);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // in Core ook geldig
        public int AddBadWeatherDay(DateOnly dag, int weatherstationid, int type)
        {
            var bwd = new BadWeatherDayBO
            {
                BWDate = dag,
                WeatherStationId = weatherstationid,
                Type = type
            };

            var service = _projectService;
            var response = service.InsertUpdateBadWeatherDay(bwd);

            if (!response.Success) return 0;

            var msg = response.Messages.FirstOrDefault()?.Message;
            return int.TryParse(msg, out var id) ? id : 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public bool DeleteBadWeatherDay(int id)
        {
            var list = new List<int> { id };
            var service = _projectService;
            var response = service.DeleteBadWeatherDays(list);
            return response.Success;
        }

        [HttpGet]
        public JsonResult GetVacationDays()
        {
            var service = _projectService;
            var response = service.GetVacationDays();

            var rows = new List<object>();
            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day
                }).Cast<object>().ToList();
            }

            // Classic MVC:
            return Json(rows);

            // ASP.NET Core alternatief:
            // return new JsonResult(rows);
            // of: return Ok(rows);
        }

        // ========== PROJECT DETAIL FOTO'S ==========

        [HttpGet]
        //[Breadcrumb("Foto's")]
        [Breadcrumb("Media", FromAction = "Detail")]
        public ActionResult DetailPhotos(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var imgBase = (Configuration["URL:ImageWebURL"] ?? "").TrimEnd('/');
            var vidBase = (Configuration["URL:VideoWebURL"] ?? imgBase).TrimEnd('/');
            ViewBag.ImageWebURL = imgBase + "/";
            ViewBag.VideoWebURL = vidBase + "/";

            var model   = new DetailPhotosModel();
            var service = _projectService;
            var response = service.GetPicturesByProjectId(projectid);

            if (response.Success)
                model.Photos = response.Values.OrderBy(m => m.SortOrder).ThenByDescending(m => m.DateTimeUploaded).ToList();

            // Secties laden
            model.Sections = _db.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == projectid)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .Select(s => new ProjectMediaSectionVM
                {
                    Id          = s.Id,
                    Name        = s.Name,
                    Description = s.Description,
                    SortOrder   = s.SortOrder,
                    IsPublic    = s.IsPublic,
                    MediaCount  = s.ProjectPictures.Count,
                    PhotoCount  = s.ProjectPictures.Count(m => m.MediaType == 0),
                    VideoCount  = s.ProjectPictures.Count(m => m.MediaType == 1)
                })
                .ToList();

            model.ProjectId   = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail  = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
                { Parent = projectenIndex, RouteValues = new { projectid } };
            var projectRecalc  = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailPhotos", "Projecten", "Media")
                { Parent = projectDetail, RouteValues = new { projectid } };
            ViewData["BreadcrumbNode"] = projectRecalc;

            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectPhotos  = _ps.HasWrite(PermissionCodes.ProjectsPhotos);
            ViewBag.CanDeleteProjectPhotos = _ps.HasDelete(PermissionCodes.ProjectsPhotos);

            return View(model);
        }
        [HttpGet]
        public ActionResult ModalAddPhoto(int id)
        {
            var viewModel = new ProjectPictureBO();
            viewModel.ProjectId = id;
            viewModel.Type = PictureType.Werffoto;
            return PartialView("_ModalAddPhoto", viewModel);
        }

        [HttpGet]
        public ActionResult ModalDeletePhoto(int id)
        {
            var viewModel = new ProjectPictureBO();

            if (id != 0)
            {
                var dservice = _projectService;
                viewModel = dservice.GetPictureById(id).Value;
            }

            return PartialView("_ModalDeletePhoto", viewModel);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsPhotos)]
        public ActionResult DeletePhoto(int id, int projectid, PictureType type)
        {
            if (id != 0 && projectid != 0)
            {
                if (type == PictureType.Hoofdfoto)
                {
                    var service = _projectService;
                    var ids = new List<int> { id };

                    var response1 = service.SetDefaultProjectPicture(projectid, 0);
                    if (response1.Success)
                    {
                        DeletePictureFile(id);
                        var response2 = service.DeletePicture(ids);

                        if (response2.Success)
                        {
                            AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                        else
                        {
                            AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
                else
                {
                    DeletePictureFile(id);
                    var service = _projectService;
                    var ids = new List<int> { id };
                    var response = service.DeletePicture(ids);

                    if (response.Success)
                    {
                        AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
        }

        public ActionResult UpdatePhotoType(int id, PictureType type)
        {
            var service = _projectService;
            var picture = service.GetPictureById(id).Value;

            if (picture != null)
            {
                if (type != PictureType.Hoofdfoto)
                {
                    picture.Type = type;
                    var response = service.InsertUpdatePicture(picture);

                    if (response.Success)
                    {
                        AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                    else
                    {
                        AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                }
                else
                {
                    picture.Type = type;

                    var response1 = service.SetDefaultProjectPicture(picture.ProjectId, picture.Id);
                    if (response1.Success)
                    {
                        var response = service.InsertUpdatePicture(picture);
                        if (response.Success)
                        {
                            AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                        else
                        {
                            AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                    }
                }
            }

            // fallback: als picture null is, vermijden we NullReference
            var fallbackProjectId = picture != null ? picture.ProjectId : 0;
            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = fallbackProjectId });
        }


        // ========== PROJECT DETAIL NIEUWS ==========

        [HttpGet]
        [Breadcrumb("Nieuws", FromAction = "Detail")]
        //[Breadcrumb("Nieuws")]
        public IActionResult DetailNews(int projectId)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";


            var model = new DetailNewsModel
            {
                ProjectId = projectId,

                ProjectName = _projectService.GetProjectNameById(projectId),
                News = _projectService.GetNewsByProjectId(projectId).Success
                              ? _projectService.GetNewsByProjectId(projectId).Values
                              : new List<ProjectNewsBO>()
            };
            ViewData["NewsBaseUrl"] = $"{Configuration["URL:ImageWebUrl"]?.TrimEnd('/')}/issues/";



            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectId }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailNews", "Projecten", "Nieuws")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectId }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectNews = _ps.HasWrite(PermissionCodes.ProjectsNews);
            ViewBag.CanDeleteProjectNews = _ps.HasDelete(PermissionCodes.ProjectsNews);

            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddNews(int id)
        {
            var vm = new ProjectNewsBO
            {
                NewsDate = DateOnly.FromDateTime(DateTime.Now),
                ProjectId = id 
            };

            return PartialView("_ModalAddNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNews(ProjectNewsBO newsItem, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            if (file is not null && file.Length > 0 && IsValidImage(file))
            {

                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-news");
                Directory.CreateDirectory(tempRoot);

                var mainPath = Path.Combine(tempRoot, $"news_{filename}");
                var originalPath = Path.Combine(tempRoot, $"news_original_{filename}");
                var smallPath = Path.Combine(tempRoot, $"news_800_{filename}");

                try
                {
                    using (var stream = System.IO.File.Create(mainPath))
                        file.CopyTo(stream);

                    System.IO.File.Copy(mainPath, originalPath, overwrite: true);
                    System.IO.File.Copy(mainPath, smallPath, overwrite: true);

                    ScaleAndCropImage(mainPath, 1280, 500);
                    ScaleImage(smallPath, 800, 800);

                    var uploadMain = UploadAssetFileToStorageAsync(mainPath, "pictures/News", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadOriginal = UploadAssetFileToStorageAsync(originalPath, "pictures/News/Original", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadSmall = UploadAssetFileToStorageAsync(smallPath, "pictures/News/800", filename, "image/jpeg").GetAwaiter().GetResult();

                    if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(uploadOriginal) || string.IsNullOrWhiteSpace(uploadSmall))
                    {
                        AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                    }
                }
                finally
                {
                    TryDeleteTempFile(mainPath);
                    TryDeleteTempFile(originalPath);
                    TryDeleteTempFile(smallPath);
                }

                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };
                newsItem.Picture = picture;
            }

            // auteur (gebruik wat voorhanden is)
            newsItem.Author = (string?)ViewData["fullname"] ?? User?.Identity?.Name ?? "onbekend";

            var response = _projectService.InsertUpdateNews(newsItem);
            if (response.Success)
            {
                AddMessage("success", "Het nieuwsbericht is toegevoegd.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET toegevoegd. Probeer opnieuw of contacteer de administrator.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }

        [HttpGet]
        public IActionResult ModalDeleteNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteNews", vm);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsNews)]
        public ActionResult DeleteNews(int id, int projectId, int pictureId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailNews", new { projectId });

            var resp = _projectService.DeleteNews(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het nieuwsitem is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId });
            }

            // eventueel gekoppelde foto verwijderen
            if (pictureId > 0)
            {
                try { DeletePictureFile(pictureId); } catch { /* log indien gewenst */ }

                var respPic = _projectService.DeletePicture(new List<int> { pictureId });
                if (!respPic.Success)
                {
                    AddMessage("error", "Nieuws verwijderd maar foto kon niet verwijderd worden.", "Opgelet");
                    return RedirectToAction("DetailNews", new { projectId });
                }
            }

            AddMessage("success", "Het nieuwsitem is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailNews", new { projectId });
        }

        [HttpGet]
        public IActionResult ModalEditNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalEditNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNews(ProjectNewsBO newsItem, IFormFile? file, bool RemovePicture = false)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }



            // Bewaar referentie naar de (mogelijke) bestaande foto om na succes op te ruimen
            var oldPicId = newsItem.Picture?.Id ?? 0;
            var oldPicName = newsItem.Picture?.Name;

            // Validatie op bestandstype indien er een upload is
            if (file is not null && file.Length > 0 && !IsValidImage(file))
            {
                AddMessage("error", "Verkeerd bestandstype. Kies een JPG/PNG/GIF.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            // Nieuwe upload?
            ProjectPictureBO? newPicture = null;
            if (file is not null && file.Length > 0)
            {
                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-news");
                Directory.CreateDirectory(tempRoot);

                var mainPath = Path.Combine(tempRoot, $"news_{filename}");
                var originalPath = Path.Combine(tempRoot, $"news_original_{filename}");

                try
                {
                    using (var stream = System.IO.File.Create(mainPath))
                        file.CopyTo(stream);
                    System.IO.File.Copy(mainPath, originalPath, overwrite: true);

                    ScaleAndCropImage(mainPath, 1280, 500);

                    var uploadMain = UploadAssetFileToStorageAsync(mainPath, "pictures/News", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadOriginal = UploadAssetFileToStorageAsync(originalPath, "pictures/News/Original", filename, "image/jpeg").GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(uploadOriginal))
                    {
                        AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                    }
                }
                finally
                {
                    TryDeleteTempFile(mainPath);
                    TryDeleteTempFile(originalPath);
                }

                newPicture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };

                newsItem.Picture = newPicture;
            }
            else if (RemovePicture)
            {
                // Alleen verwijderen (geen nieuwe upload)
                newsItem.Picture = null;               // verwijder koppeling
                                                       // de eigenlijke oude foto (record + bestand) ruimen we op ná een geslaagde update
            }
            // Else: niets doen → bestaande foto blijft gekoppeld

            var response = _projectService.InsertUpdateNews(newsItem);

            if (response.Success)
            {
                // Als we een nieuwe foto hebben gezet of expliciet verwijderen, oude foto opruimen
                if ((newPicture is not null || RemovePicture) && oldPicId > 0)
                {
                    try { DeletePictureFile(oldPicId); } catch { /* loggen indien gewenst */ }
                    _projectService.DeletePicture(new List<int> { oldPicId });
                }

                AddMessage("success", "Het nieuwsbericht is bijgewerkt.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET bijgewerkt.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }

        // ========== PROJECT DETAIL DOCS ==========

        [HttpGet]
        [Breadcrumb("Documenten", FromAction = "Detail")]
        //[Breadcrumb("Documenten")]
        public IActionResult DetailDocs(int projectid, int? clientaccountid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ViewBag.DocWebUrl = Configuration["URL:DocWebUrl"];

            var service = _projectService;
            var cservice = _clientService;
            var model = new DetailDocsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid),

            };
            var clientsResponse = cservice.GetClientAccountsByProjectIdForSelect(projectid);
            model.Clients = clientsResponse.Success ? clientsResponse.Values : Array.Empty<IdNameBO>();

            // Als er een client is: clientdocs, anders projectdocs
            if (clientaccountid.HasValue && clientaccountid.Value > 0)
            {

                var respClient = service.GetClientDocs(clientaccountid.Value);
                model.ClientAccountId = (int)clientaccountid;
                model.ClientName = cservice.GetClientAccountNameById(model.ClientAccountId);


                if (respClient.Success)
                    model.Docs = respClient.Values;
                else
                    model.Docs = new List<ProjectDocBO>();
            }
            else
            {
                var respProj = service.GetProjectDocs(projectid);
                if (respProj.Success)
                    model.Docs = respProj.Values;
                else
                    model.Docs = new List<ProjectDocBO>();


            }


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailDocs", "Projecten", "Documenten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _psDocs = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectDocs = _psDocs.HasWrite(PermissionCodes.ProjectsDocuments);
            ViewBag.CanDeleteProjectDocs = _psDocs.HasDelete(PermissionCodes.ProjectsDocuments);

            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddDoc(int id, int? clientaccountid)
        {
            var clientService = _clientService;
            var clientsResponse = clientService.GetClientAccountsByProjectIdForSelect(id);
            var vm = new CPMCore.Models.Projecten.ProjectDocModalVM
            {
                Document = new ProjectDocBO
                {
                    ProjectId = id,
                    ClientAccountId = clientaccountid ?? 0
                },
                Clients = clientsResponse.Success ? clientsResponse.Values : new List<IdNameBO>(),
                Target = clientaccountid.HasValue && clientaccountid.Value > 0 ? "Client" : "Project",
                SelectedClientAccountId = clientaccountid,
                IsEditMode = false
            };
            return PartialView("Modals/_ModalAddDoc", vm);
        }

        [HttpGet]
        public IActionResult ModalEditDoc(int id)
        {
            var projectService = _projectService;
            var response = projectService.GetProjectDoc(id);
            if (!response.Success || response.Value is null)
                return NotFound();

            var doc = response.Value;
            var clientService = _clientService;
            var clientsResponse = clientService.GetClientAccountsByProjectIdForSelect(doc.ProjectId);
            var hasClient = doc.ClientAccountId.HasValue && doc.ClientAccountId.Value > 0;

            var vm = new CPMCore.Models.Projecten.ProjectDocModalVM
            {
                Document = doc,
                Clients = clientsResponse.Success ? clientsResponse.Values : new List<IdNameBO>(),
                Target = hasClient ? "Client" : "Project",
                SelectedClientAccountId = hasClient ? doc.ClientAccountId : null,
                IsEditMode = true
            };

            if (!string.IsNullOrWhiteSpace(doc.Filename))
            {
                vm.DocumentUrl = GetSignedAssetUrlByFileName(doc.Filename, "docs");
                var thumbFileName = BuildDocThumbFileName(doc.Filename);
                vm.ThumbnailUrl = GetSignedAssetUrlByFileName(thumbFileName, "docs") ?? vm.DocumentUrl;
            }

            return PartialView("Modals/_ModalAddDoc", vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddDocument(CPMCore.Models.Projecten.ProjectDocModalVM vm, IFormFile file)
        {
            var model = vm.Document ?? new ProjectDocBO();
            if (!string.Equals(vm.Target, "Client", StringComparison.OrdinalIgnoreCase))
            {
                model.ClientAccountId = null;
            }
            else
            {
                model.ClientAccountId = vm.SelectedClientAccountId;
            }

            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("Upload", "U moet een bestand kiezen");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }
            if (model.ClientAccountId is int i && i <= 0)
                model.ClientAccountId = null;
            var ext = Path.GetExtension(file.FileName) ?? string.Empty;
            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;

            var uploadedFileName = await UploadAssetToStorageAsync(file, "docs");
            if (string.IsNullOrWhiteSpace(uploadedFileName))
            {
                AddMessage("error", "Upload naar storage API is mislukt.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            // Zet filename in het BO vóór de service call
            model.Filename = uploadedFileName;

            await GenerateDocThumbnailViaStorageAsync(uploadedFileName);


            var service = _projectService;
            var response = service.InsertUpdateProjectDoc(model);

            var clientIdVal = (model.ClientAccountId is int v && v > 0) ? v : 0;

            if (response.Success)
            {
                AddMessage("success", "Het document is toegevoegd / bijgewerkt", "Gelukt!");
                return clientIdVal > 0
                    ? RedirectToAction("Detail", "Klanten", new { clientid = clientIdVal, projectid = model.ProjectId })
                    : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            AddMessage("error", "Het document is NIET toegevoegd / bijgewerkt, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            return clientIdVal > 0
                ? RedirectToAction("DetailDocs", new { clientaccountid = clientIdVal, projectid = model.ProjectId })
                : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAllDocThumbnails(int projectid, int? clientaccountid)
        {
            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            {
                AddMessage("error", "Storage API is niet correct geconfigureerd.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
            }

            var endpointCandidates = BuildStorageEndpointCandidates(
                baseUrl,
                "/api/assets/docs/generate-thumbnails",
                "/assets/docs/generate-thumbnails",
                "/docs/generate-thumbnails");

            try
            {
                using var httpClient = CreateStorageHttpClient(writeKey, TimeSpan.FromMinutes(5));

                HttpStatusCode? lastStatusCode = null;
                string lastResponseBody = string.Empty;
                string lastEndpoint = endpointCandidates.FirstOrDefault() ?? baseUrl;

                foreach (var endpoint in endpointCandidates)
                {
                    var response = await httpClient.PostAsync(endpoint, content: null);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (IsLikelyAuthRedirectOrLoginPage(response, responseBody))
                    {
                        _logger.LogError("Bulk doc thumbnail generation hit auth/login page. Endpoint: {Endpoint}. Status: {StatusCode}. BodySnippet: {BodySnippet}",
                            endpoint, (int)response.StatusCode, responseBody.Length > 220 ? responseBody[..220] : responseBody);

                        AddMessage("error", "Storage API call werd omgeleid naar een loginpagina. Controleer `StorageApi:BaseUrl` (moet de storage service URL zijn, niet CPM) en reverse proxy authenticatie.", "Fout!");
                        return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
                    }

                    if (response.IsSuccessStatusCode && IsValidBulkThumbnailResponse(responseBody))
                    {
                        AddMessage("success", "Thumbnail generatie voor alle documenten is gestart/uitgevoerd.", "Gelukt!");
                        return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Bulk doc thumbnail generation got a non-storage success response. Endpoint: {Endpoint}. Body: {Body}", endpoint, responseBody);
                    }

                    lastStatusCode = response.StatusCode;
                    lastResponseBody = responseBody;
                    lastEndpoint = endpoint;

                    _logger.LogWarning("Bulk doc thumbnail generation attempt failed. Status: {StatusCode}. Endpoint: {Endpoint}. Body: {Body}",
                        (int)response.StatusCode, endpoint, responseBody);

                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        break;
                    }
                }

                var shortBody = string.IsNullOrWhiteSpace(lastResponseBody)
                    ? "geen foutdetails ontvangen"
                    : lastResponseBody.Length > 220
                        ? $"{lastResponseBody[..220]}..."
                        : lastResponseBody;

                AddMessage("error",
                    $"Thumbnail generatie is mislukt (HTTP {(int)(lastStatusCode ?? HttpStatusCode.InternalServerError)} - {lastStatusCode}). Endpoint: {lastEndpoint}. Details: {shortBody}",
                    "Fout!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling storage bulk thumbnail endpoints. BaseUrl: {BaseUrl}", baseUrl);
                AddMessage("error", $"Thumbnail generatie kon niet worden gestart: {ex.Message}", "Fout!");
            }

            return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
        }



        [HttpPost]
        public IActionResult EditDocument(CPMCore.Models.Projecten.ProjectDocModalVM vm)
        {
            var model = vm.Document ?? new ProjectDocBO();
            if (model.Docid <= 0)
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });

            var service = _projectService;
            var existingResp = service.GetProjectDoc(model.Docid);
            if (!existingResp.Success || existingResp.Value is null)
            {
                AddMessage("error", "Het document kon niet worden gevonden.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            var existingDoc = existingResp.Value;
            model.ProjectId = existingDoc.ProjectId;
            model.Filename = existingDoc.Filename;

            if (!string.Equals(vm.Target, "Client", StringComparison.OrdinalIgnoreCase))
                model.ClientAccountId = null;
            else
                model.ClientAccountId = vm.SelectedClientAccountId;

            if (model.ClientAccountId is int i && i <= 0)
                model.ClientAccountId = null;

            var response = service.InsertUpdateProjectDoc(model);
            if (response.Success)
            {
                AddMessage("success", "Het document is bijgewerkt.", "Gelukt!");
            }
            else
            {
                AddMessage("error", "Het document kon niet worden bijgewerkt.", "Fout!");
            }

            var clientIdVal = (model.ClientAccountId is int v && v > 0) ? v : 0;
            return clientIdVal > 0
                ? RedirectToAction("DetailDocs", new { clientaccountid = clientIdVal, projectid = model.ProjectId })
                : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
        }

        [HttpGet]
        public IActionResult ViewDoc(int id)
        {
            var signedUrl = GetSignedAssetUrl(id, "docs");
            if (string.IsNullOrWhiteSpace(signedUrl)) return NotFound();
            return Redirect(signedUrl);
        }

        [HttpGet]
        public IActionResult DownloadDoc(int id, string? asName = null)
        {
            var signedUrl = GetSignedAssetUrl(id, "docs");
            if (string.IsNullOrWhiteSpace(signedUrl)) return NotFound();
            return Redirect(signedUrl);
        }


        [HttpGet]
        public IActionResult ModalDeleteDoc(int id)
        {
            var vm = new ProjectDocBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetProjectDoc(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteDoc", vm);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsDocuments)]
        public ActionResult DeleteDoc(int id, int projectId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailDocs", new { projectId });

            var resp = _projectService.DeleteProjectDoc(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het document is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectId });
            }

            AddMessage("success", "Het document is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailDocs", new { projectId });
        }

        // ========== PROJECT DETAIL INSURANCES ==========

        [HttpGet]
        //[Breadcrumb("Verzekeringen")]
        [Breadcrumb("Verzekeringen", FromAction = "Detail")]
        public IActionResult DetailInsurances(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailInsurancesModel();
            var service = _projectService;
            var response = service.GetProjectInsurances(projectid);

            if (response.Success)
                model.Insurances = response.Values;

            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailInsurances", "Projecten", "Verzekeringen")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectInsurances = _ps.HasWrite(PermissionCodes.ProjectsInsurances);
            ViewBag.CanDeleteProjectInsurances = _ps.HasDelete(PermissionCodes.ProjectsInsurances);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditInsurance(ProjectAddInsurancesModel viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                if (viewmodel?.Insurance == null)
                {
                    response.AddError("Insurance model is leeg.");
                }
                else
                {
                    viewmodel.Insurance.ProjectID = viewmodel.ProjectId;
                    var service = _insuranceService;
                    response = service.InsertUpdate(viewmodel.Insurance);
                }
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is toegevoegd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET toegevoegd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel?.Insurance?.ProjectID ?? viewmodel?.ProjectId });
        }


        [HttpGet]
        public IActionResult ModalDeleteInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel = dservice.GetInsuranceById(id).Value;
            }

            return PartialView("_ModalDeleteInsurance", viewModel);
        }

        [HttpGet]
        public IActionResult ModalEndInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel = dservice.GetInsuranceById(id).Value;

                if (viewModel != null)
                {
                    if (viewModel.Type == InsuranceType.ABR && viewModel.Startdate.HasValue)
                    {
                        var start = viewModel.Startdate.Value;
                        viewModel.Enddate = start.AddMonths(
                            (viewModel.Period ?? 0) + (viewModel.ExtensionPeriod ?? 0) + (viewModel.GuaranteePeriod ?? 0)
                        );
                    }
                    else
                    {
                        viewModel.Enddate = DateOnly.FromDateTime(DateTime.Now);
                    }
                }
            }

            return PartialView("_ModalStopInsurance", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndInsurance(InsuranceBO viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                var service = _insuranceService;
                response = service.InsertUpdate(viewmodel);
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is beëindigd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET beëindigd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel.ProjectID });
        }

        [HttpGet]
        public IActionResult ModalEditInsurance(int id)
        {
            var viewModel = new ProjectAddInsurancesModel();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel.Insurance = dservice.GetInsuranceById(id).Value;
            }

            // zeker dat ProjectId gezet is
            viewModel.ProjectId = viewModel.Insurance?.ProjectID ?? viewModel.ProjectId;

            var service = _insuranceService;
            var cservice = _companyService;

            var cresponse = cservice.GetCompanyForSelectByActivity(142);
            if (cresponse.Success) viewModel.Brokers = cresponse.Values;

            var response = service.GetInsuranceCompaniesForSelect();
            if (response.Success) viewModel.Companies = response.Values;

            // Zelfde partial als Add
            return PartialView("_ModalEditInsurance", viewModel);
        }

        // ========== PROJECT DETAIL SALES ==========

        [HttpGet]
        //[Breadcrumb("Verkoop")]
        [Breadcrumb("Verkoop", FromAction = "Detail")]
        public IActionResult DetailSales(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectSalesModel();
            var service = _projectService;
            var uservice = _unitService;

            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            var response = uservice.GetUnitsWithAttachedByProjectId(projectid);
            model.ProjectUnits = response.Values;


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSales", "Projecten", "Verkoop")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSales = _ps.HasWrite(PermissionCodes.ProjectsForSale);

            return View(model);
        }

        [HttpPost]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsForSale)]
        public IActionResult SetUnitIsOption(int unitId, bool isOption)
        {
            var response = _unitService.SetUnitIsOption(unitId, isOption);
            if (!response.Success)
                return Json(new { success = false, message = string.Join(", ", response.Messages.Where(m => m.Type == MessageType.Error).Select(m => m.Message)) });
            return Json(new { success = true, isOption });
        }

        [HttpGet]
        [Breadcrumb("Coördinatie", FromAction = "Detail")]
        public IActionResult DetailCoordinatie(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;
            var model = new Models.Projecten.ProjectCoordinatieModel
            {
                ProjectId                   = projectid,
                ProjectName                 = proj.Name,
                ContractType                = proj.ContractType,
                ProjectDistanceKm           = proj.ProjectDistanceKm,
                KmAllowance                 = proj.KmAllowance,
                CoordinationIssuerCompanyId = proj.CoordinationIssuerCompanyId,
                ProjectManagerUserId        = proj.AspNetUserID,
            };

            model.ContractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault();

            var slicesResp = _projectService.GetContractSlices(projectid);
            if (slicesResp.Success)
            {
                var contractPrice = model.ContractPrice ?? 0m;
                model.ContractSlices = slicesResp.Values.Select(s => new Models.Projecten.ProjectContractSliceVM
                {
                    Id               = s.Id,
                    Description      = s.Description,
                    Percentage       = s.Percentage,
                    Amount           = Math.Round(contractPrice * s.Percentage / 100m, 2, MidpointRounding.AwayFromZero),
                    InvoiceId        = s.InvoiceId,
                    InvoicePublicId  = s.InvoicePublicId
                }).ToList();
            }

            // Gefactureerd bedrag:
            // 1) Directe schijf-factuurkoppeling (InvoiceId op schijf) — meest nauwkeurig
            var linkedInvoiceIds = model.ContractSlices
                .Where(s => s.InvoiceId.HasValue)
                .Select(s => s.InvoiceId!.Value)
                .ToList();

            model.InvoicedAmount = model.ContractSlices
                .Where(s => s.IsInvoiced)
                .Sum(s => s.Amount);

            // 2) Fallback voor bestaande facturen zonder directe schijfkoppeling
            if (model.CoordinationIssuerCompanyId.HasValue)
            {
                var fallbackQuery = _db.Invoices
                    .Where(i => i.ProjectId == projectid
                             && i.IssuerCompanyId == model.CoordinationIssuerCompanyId.Value
                             && i.StatusId != 7); // 7 = Cancelled

                if (linkedInvoiceIds.Count > 0)
                    fallbackQuery = fallbackQuery.Where(i => !linkedInvoiceIds.Contains(i.Id));

                model.InvoicedAmount += fallbackQuery
                    .SelectMany(i => i.InvoicesDetails)
                    .Where(d => d.LineType == "detail")
                    .Sum(d => (decimal?)d.Price) ?? 0m;
            }

            var ratesResp = _projectService.GetProjectHourlyRates(projectid);
            if (ratesResp.Success)
                model.HourlyRates = ratesResp.Values.Select(r => new Models.Projecten.ProjectHourlyRateVM
                {
                    UserId       = r.UserId,
                    UserFullName = r.UserFullName,
                    HourlyRate   = r.HourlyRate
                }).ToList();

            var regieResp = _projectService.GetRegieUren(projectid);
            if (regieResp.Success)
            {
                var rateMap = model.HourlyRates.ToDictionary(r => r.UserId, r => r.HourlyRate);
                model.RegieUren = regieResp.Values.Select(r => new Models.Projecten.ProjectRegieUurVM
                {
                    Id              = r.Id,
                    UserId          = r.UserId,
                    UserFullName    = r.UserFullName,
                    HourlyRate      = rateMap.TryGetValue(r.UserId, out var rate) ? rate : 0m,
                    Date            = r.Date,
                    Hours           = r.Hours,
                    WithTravel      = r.WithTravel,
                    TravelKm        = r.TravelKm,
                    Description     = r.Description,
                    InvoiceId       = r.InvoiceId,
                    InvoicePublicId = r.InvoicePublicId
                }).ToList();
            }

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", proj.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailCoordinatie", "Projecten", "Coördinatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Coördinatie-instellingen", FromAction = "DetailCoordinatie")]
        public IActionResult CoordinatieInstellingen(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;
            var model = new Models.Projecten.CoordinatieInstellingenVM
            {
                ProjectId                   = projectid,
                ProjectName                 = proj.Name,
                CoordinationIssuerCompanyId = proj.CoordinationIssuerCompanyId,
                ContractType                = proj.ContractType,
                KmAllowance                 = proj.KmAllowance,
                CoordinationReference       = proj.CoordinationReference,
                ProjectDistanceKm           = proj.ProjectDistanceKm,
                RouteDurationSeconds        = proj.RouteDurationSeconds,
                IssuerCompanies             = GetIssuerCompanies(),
            };

            FillInAvailableUsersForCoord(model);

            var slicesResp = _projectService.GetContractSlices(projectid);
            if (slicesResp.Success)
                model.ContractSlices = slicesResp.Values.Select(s => new Models.Projecten.ProjectContractSliceVM
                {
                    Id          = s.Id,
                    Description = s.Description,
                    Percentage  = s.Percentage
                }).ToList();

            var ratesResp = _projectService.GetProjectHourlyRates(projectid);
            if (ratesResp.Success)
                model.HourlyRates = ratesResp.Values.Select(r => new Models.Projecten.ProjectHourlyRateVM
                {
                    UserId       = r.UserId,
                    UserFullName = r.UserFullName,
                    HourlyRate   = r.HourlyRate
                }).ToList();

            // Laad contractprijs van het coördinatiecontract (lot 277 = projectcoordinatie)
            model.ContractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault();

            SetCoordinatieBreadcrumb(projectid, proj.Name);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoordinatieInstellingen(Models.Projecten.CoordinatieInstellingenVM vm)
        {
            var projectid = vm.ProjectId;

            // Laad het bestaande project om alle velden te bewaren
            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;

            // Coördinatie-velden bijwerken
            proj.IsCoordinationProject      = true;
            proj.CoordinationIssuerCompanyId = vm.CoordinationIssuerCompanyId;
            proj.ContractType               = vm.ContractType;
            proj.KmAllowance                = vm.KmAllowance;
            proj.CoordinationReference      = string.IsNullOrWhiteSpace(vm.CoordinationReference) ? null : vm.CoordinationReference.Trim();

            // Route herberekenen indien coördinatiebedrijf en postcode beschikbaar
            if (proj.CoordinationIssuerCompanyId.HasValue && proj.Postalcode?.PostcodeId > 0)
            {
                var (distKm, durSec) = await _projectService.CalculateRouteAsync(
                    proj.CoordinationIssuerCompanyId.Value, (int)proj.Postalcode.PostcodeId.Value);
                if (distKm.HasValue)
                {
                    proj.ProjectDistanceKm    = distKm;
                    proj.RouteDurationSeconds = durSec;
                }
            }

            _projectService.InsertUpdate(proj);

            // Schijven en uurtarieven opslaan
            _projectService.SaveContractSlices(projectid, (vm.ContractSlices ?? new()).Select(s => new BOCore.ProjectContractSliceBO
            {
                Description = s.Description,
                Percentage  = s.Percentage
            }).ToList());

            _projectService.SaveProjectHourlyRates(projectid, (vm.HourlyRates ?? new())
                .Where(r => !string.IsNullOrWhiteSpace(r.UserId))
                .Select(r => new BOCore.ProjectHourlyRateBO
                {
                    UserId    = r.UserId,
                    HourlyRate = r.HourlyRate
                }).ToList());

            // Coördinatiecontract aanmaken/bijwerken indien facturatiebedrijf geselecteerd
            if (vm.CoordinationIssuerCompanyId.HasValue)
            {
                // Gebruik LegacyCompanyInfoId als directe link; anders fallback via CompanyIssuerCompany
                var linkedCompanyId = _db.IssuerCompany
                    .AsNoTracking()
                    .Where(ic => ic.Id == vm.CoordinationIssuerCompanyId.Value)
                    .Select(ic => ic.LegacyCompanyInfoId)
                    .FirstOrDefault()
                    ?? _db.CompanyIssuerCompany
                           .Where(c => c.IssuerCompanyId == vm.CoordinationIssuerCompanyId.Value)
                           .Select(c => (int?)c.CompanyId)
                           .FirstOrDefault();

                if (linkedCompanyId.HasValue)
                {
                    var existingContract = _db.Contract
                        .Include(c => c.ContractActivity)
                        .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                        .FirstOrDefault();

                    if (existingContract == null)
                    {
                        var contractBo = new BOCore.ContractBO
                        {
                            ProjectId      = projectid,
                            VatPercentage  = 21,
                            PaymentTerm    = 14,
                            ContractSigned = true,
                            GuaranteeType  = BOCore.ContractGuaranteeType.NoGuarantee
                        };
                        contractBo.Company.ID = linkedCompanyId.Value;
                        contractBo.Activities.Add(new BOCore.ContractActivityBO
                        {
                            Activity = new BOCore.ActivityBO { ID = 277 },
                            Price    = vm.ContractPrice
                        });
                        _projectService.InsertUpdateProjectContract(contractBo);
                    }
                    else
                    {
                        existingContract.CompanyId      = linkedCompanyId.Value;
                        existingContract.VatPercentage  = 21;
                        existingContract.PaymentTerm    = 14;
                        existingContract.ContractSigned = true;
                        var coordActivity = existingContract.ContractActivity.FirstOrDefault(a => a.ActivityId == 277);
                        if (coordActivity != null)
                            coordActivity.Price = vm.ContractPrice;
                        _db.SaveChanges();
                    }
                }
            }

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid });
        }

        [HttpGet]
        public IActionResult GetIssuerCompanyDefaults(int issuerCompanyId)
        {
            var company = _db.IssuerCompany
                .AsNoTracking()
                .Where(c => c.Id == issuerCompanyId)
                .Select(c => new
                {
                    ratePerKm = c.RatePerKm,
                    userRates = c.IssuerCompanyUserRate.Select(r => new { userId = r.UserId, hourlyRate = r.HourlyRate }).ToList()
                })
                .FirstOrDefault();

            if (company == null)
                return NotFound();

            return Json(company);
        }

        [HttpGet]
        public PartialViewResult BlankSliceRow()
        {
            var viewData = new ViewDataDictionary<Models.Projecten.ProjectContractSliceVM>(
                ViewData, new Models.Projecten.ProjectContractSliceVM());
            return new PartialViewResult
            {
                ViewName = "Partials/_SliceRow",
                ViewData = viewData
            };
        }

        [HttpGet]
        public PartialViewResult BlankRateRow()
        {
            var internalUserIds = _db.PermissionPerUser.Select(p => p.UserId).Distinct();
            var users = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && internalUserIds.Contains(u.Id))
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserId,
                    Text = (u.Voornaam + " " + u.Familienaam).Trim()
                })
                .ToList();

            var viewData = new ViewDataDictionary<Models.Projecten.ProjectHourlyRateVM>(
                ViewData, new Models.Projecten.ProjectHourlyRateVM())
            {
                { "AvailableUsers", users }
            };
            return new PartialViewResult
            {
                ViewName = "Partials/_RateRow",
                ViewData = viewData
            };
        }

        [HttpGet]
        public IActionResult SalesListPdf(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectSalesExportModel();
            var service2 = _projectService;
            var service3 = _unitService;

            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            // Get Units
            var response = service3.GetGroupedUnitsForSaleWithDetailsByProjectId(projectid);
            model.UnitsGrouped = response.Values;

            // Get SurfaceTypes
            model.SurfaceTypes = service3.GetUniqueRoomTypesInProjectByProjectId(projectid).Values;

            // PDF
            // Belangrijk: model via constructor doorgeven
            var pdf = new ViewAsPdf("SalesListPDF", model)
            {
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageSize = Rotativa.AspNetCore.Options.Size.A3,
                FileName = $"Verkooplijst - {model.ProjectName} {DateTime.Now:yyyyMMdd}.pdf"
            };

            // var pdfBytes = a.BuildPdf(ControllerContext); // kan, maar niet nodig om te returnen
            return pdf;
            // return File(pdfBytes, "application/pdf"); // alternatief
        }

        [HttpGet]
        [Breadcrumb("Verkoopsinstellingen", FromAction = "DetailSales")]
        //[Breadcrumb("Verkoopsinstellingen")]
        public async Task<IActionResult> SalesSettings(int projectid)
        {
            // 1) Veilige referrer voor je "terug"-link (enkel van dezelfde host)
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Referrer"] = refHeader;
            }
            var viewModel = new ProjectSalesSettingsModel();
            if (projectid != 0)
            {
                viewModel.ProjectId = projectid;
            }

            var service = _projectService;
            var response = service.GetSalesSettings(projectid);
            if (response.Success) viewModel.Settings = response.Value;
            else viewModel.Settings = new ProjectSalesSettingsBO { ProjectId = projectid };

            var presponse = service.GetProjectByID(projectid);
            if (presponse.Success)
            {
                viewModel.Project = presponse.Value;
            }

            await PopulateBankAccountsAsync(viewModel);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", viewModel.Project.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectSales = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSales", "Projecten", "Verkoop")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("SalesSettings", "Projecten", "Instellingen")
            {
                Parent = projectSales,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SalesSettings(ProjectSalesSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBankAccountsAsync(model);
                return View(model);
            }

            var service = _projectService;

            if (model.Project == null || model.Project.Id == 0)
            {
                var projectResponse = service.GetProjectByID(model.ProjectId);
                if (projectResponse.Success)
                {
                    model.Project = projectResponse.Value;
                }
            }

            bool hasBuilder = model.Project?.IssuerCompanyIdBuilder != null;

            if (!hasBuilder)
            {
                model.MissingBuilder = true;
                model.BuilderWarning = "Er is geen bouwer gekoppeld aan dit project. Kies eerst een bouwer om een projectrekening te selecteren.";
            }
            else
            {
                await EnsureBankAccountAsync(model);

                if (model.Settings?.BankAccountId == null && string.IsNullOrWhiteSpace(model.NewBankAccountIban))
                {
                    ModelState.AddModelError("Settings.BankAccountId", "Selecteer of maak een projectrekening aan.");
                    await PopulateBankAccountsAsync(model);
                    return View(model);
                }
            }

            // Eerste bewerking
            var response1 = service.InsertUpdateSalesSettings(model.Settings);

            // Tweede bewerking
            var response2 = service.InsertUpdateSalesText(model.Project);

            // Beide resultaten samen beoordelen
            if (response1.Success && response2.Success)
            {
                AddMessage("success", "De instellingen voor de verkoop zijn aangepast", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De instellingen voor de verkoop zijn NIET aangepast", "Fout!");
            }

            return RedirectToAction("DetailSales", "Projecten", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculateCosts(int projectId, [FromBody] int[] unitIds)
        {
            if (unitIds == null || unitIds.Length == 0)
                return BadRequest("Geen eenheden geselecteerd.");

            var ids = unitIds.Distinct().ToList();

            // Settings ophalen
            var ps = _projectService;
            var settingsResp = ps.GetSalesSettings(projectId); // Response<ProjectSalesSettingsBO>
            if (!settingsResp.Success || settingsResp.Value == null)
                return BadRequest("Geen verkoopinstellingen gevonden.");
            var settings = settingsResp.Value;

            // Units ophalen
            var uservice = _unitService;
            var unitsResp = uservice.GetUnitsById(ids);
            var units = unitsResp.Success && unitsResp.Values != null
                ? unitsResp.Values.ToList()
                : new List<UnitBO>();
            if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

            // Eerste render: kortingen 0, geen overrides (null)
            var vm = BuildCalculationVM(projectId, settings, units, discounts: null, overrides: null);

            return PartialView("_CostCalculationCard", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecalculateCosts(int projectId, [FromBody] RecalculateRequest payload)
        {
            if (payload == null || payload.Units == null || payload.Units.Count == 0)
                return BadRequest("Ongeldige herberekening.");

            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var projectService = scope.ServiceProvider.GetRequiredService<FacadeCore.IProjectService>();
                var unitService = scope.ServiceProvider.GetRequiredService<FacadeCore.IUnitService>();

                var ids = payload.Units.Select(x => x.UnitId).Distinct().ToList();

                var settingsResp = projectService.GetSalesSettings(projectId);
                if (!settingsResp.Success || settingsResp.Value == null)
                    return BadRequest("Geen verkoopinstellingen gevonden.");

                var unitsResp = unitService.GetUnitsById(ids);
                var units = unitsResp.Success && unitsResp.Values != null
                    ? unitsResp.Values.ToList()
                    : new List<UnitBO>();
                if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

                var discounts = payload.Units.ToDictionary(k => k.UnitId, v => v);
                var vm = BuildCalculationVM(projectId, settingsResp.Value, units, discounts, payload);

                return PartialView("_CostCalculationCard", vm);
            }


           


        }

        private async Task PopulateBankAccountsAsync(ProjectSalesSettingsModel model)
        {
            if (model.Settings == null)
            {
                model.Settings = new ProjectSalesSettingsBO { ProjectId = model.ProjectId };
            }

            if (model.Project == null || model.Project.Id == 0)
            {
                var projectResponse = _projectService.GetProjectByID(model.ProjectId);
                if (projectResponse.Success)
                {
                    model.Project = projectResponse.Value;
                }
            }

            var builderId = model.Project?.IssuerCompanyIdBuilder;
            if (builderId == null)
            {
                model.MissingBuilder = true;
                model.BuilderWarning ??= "Er is geen bouwer gekoppeld aan dit project. Kies eerst een bouwer om een projectrekening te selecteren.";
                model.BankAccounts = new List<SelectListItem>();
                return;
            }

            using var scope = HttpContext.RequestServices.CreateScope();
            var bankService = scope.ServiceProvider.GetRequiredService<IIssuerBankAccountService>();
            var accounts = await bankService.ListByIssuerAsync(builderId.Value);
            model.MissingBuilder = false;

            IssuerBankAccountBO? selectedAccount = null;
            if (model.Settings?.BankAccountId is int selectedId)
            {
                selectedAccount = accounts.FirstOrDefault(a => a.Id == selectedId) ?? await bankService.GetAsync(selectedId);
            }

            model.Settings.BankAccountNumber ??= selectedAccount?.Iban;
            model.BankAccounts = BuildBankAccountSelectList(accounts, model.Settings.BankAccountId, selectedAccount);
        }

        private static List<SelectListItem> BuildBankAccountSelectList(
            IEnumerable<IssuerBankAccountBO> accounts,
            int? selectedId,
            IssuerBankAccountBO? selectedAccount = null)
        {
            var list = accounts
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.DisplayName)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(a.DisplayName)
                        ? a.Iban
                        : $"{a.DisplayName} ({a.Iban}){(a.IsDefault ? " - standaard" : string.Empty)}",
                    Selected = selectedId.HasValue && a.Id == selectedId.Value
                })
                .ToList();

            if (selectedAccount != null && list.All(l => l.Value != selectedAccount.Id.ToString()))
            {
                list.Insert(0, new SelectListItem
                {
                    Value = selectedAccount.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(selectedAccount.DisplayName)
                        ? selectedAccount.Iban
                        : $"{selectedAccount.DisplayName} ({selectedAccount.Iban})",
                    Selected = true
                });
            }

            return list;
        }

        private async Task EnsureBankAccountAsync(ProjectSalesSettingsModel model)
        {
            var builderId = model.Project?.IssuerCompanyIdBuilder;
            var iban = model.NewBankAccountIban?.Trim();
            var selectedId = model.Settings?.BankAccountId;

            if (builderId == null)
            {
                await PopulateBankAccountsAsync(model);
                return;
            }

            using var scope = HttpContext.RequestServices.CreateScope();
            var bankService = scope.ServiceProvider.GetRequiredService<IIssuerBankAccountService>();
            var db = scope.ServiceProvider.GetRequiredService<DALCore.Models.cpmRunningContext>();

            var accounts = await bankService.ListByIssuerAsync(builderId.Value);

            IssuerBankAccountBO? selectedAccount = null;
            if (selectedId.HasValue)
            {
                selectedAccount = accounts.FirstOrDefault(a => a.Id == selectedId.Value) ?? await bankService.GetAsync(selectedId.Value);
            }

            if (selectedAccount == null && !string.IsNullOrWhiteSpace(iban))
            {
                var existingAccount = db.IssuerBankAccount.FirstOrDefault(a => a.Iban == iban);
                if (existingAccount == null)
                {
                    var newAccount = new IssuerBankAccountBO
                    {
                        IssuerCompanyId = 1,
                        Iban = iban,
                        Bic = string.Empty,
                        DisplayName = string.IsNullOrWhiteSpace(model.Project?.Name)
                            ? $"Project {model.ProjectId}"
                            : model.Project!.Name,
                        IsDefault = false
                    };

                    var newId = await bankService.CreateAsync(newAccount);
                    selectedId = newId;
                    selectedAccount = await bankService.GetAsync(newId);
                }
                else
                {
                    selectedId = existingAccount.Id;
                    selectedAccount = await bankService.GetAsync(existingAccount.Id);
                }
            }

            model.Settings.BankAccountId = selectedId;
            model.Settings.BankAccountNumber = selectedAccount?.Iban ?? model.Settings.BankAccountNumber;
            model.BankAccounts = BuildBankAccountSelectList(accounts, model.Settings.BankAccountId, selectedAccount);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PrintCalculation(int projectId, [FromBody] RecalculateRequest payload)
        {
            if (payload == null || payload.Units == null || payload.Units.Count == 0)
                return BadRequest("Geen eenheden opgegeven.");

            // Zorg voor verse scoped services (concurrency-safe)
            using var scope = HttpContext.RequestServices.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<FacadeCore.IProjectService>();
            var unitService = scope.ServiceProvider.GetRequiredService<FacadeCore.IUnitService>();

            var unitIds = payload.Units.Select(x => x.UnitId).Distinct().ToList();

            var settingsResp = projectService.GetSalesSettings(projectId);
            if (!settingsResp.Success || settingsResp.Value == null)
                return BadRequest("Geen verkoopinstellingen gevonden.");

            var unitsResp = unitService.GetUnitsById(unitIds);
            var units = unitsResp.Success ? unitsResp.Values.ToList() : new List<UnitBO>();
            if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

            var discounts = payload.Units.ToDictionary(k => k.UnitId, v => v);
            var vm = BuildCalculationVM(projectId, settingsResp.Value, units, discounts, payload);

            var doc = new CostCalculationDocument(vm, $"Kostencalculatie – {vm.UnitCount} eenheid(en)");
            var pdf = doc.GeneratePdf();

            var fileName = $"Kostencalculatie_{projectId}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        private ProjectCostCalculationVM BuildCalculationVM(
            int projectId,
            ProjectSalesSettingsBO settings,
            List<UnitBO> units,
            Dictionary<int, UnitDiscountInput> discounts,
            RecalculateRequest overrides)
        {
            var vatPctBase = overrides?.VatPercent ?? settings.VatPercentage ?? 0m;
            var regPctBase = overrides?.RegistrationPercent ?? settings.RegistrationPercentage ?? 0m;

            // per-unit kosten
            var surveyorEx = overrides?.SurveyorCost ?? settings.SurveyorCost ?? 0m;
            var connectionEx = overrides?.ConnectionFees ?? settings.ConnectionFees ?? 0m;
            var baseDeedEx = overrides?.BaseCertificateCost ?? settings.BaseCertificateCost ?? 0m;
            var parcelEx = overrides?.ParcelCost ?? settings.ParcelCost ?? 0m;

            // globale kosten
            var fixedActeEx = overrides?.FixedCertificateCost ?? settings.FixedCertificateCost ?? 0m;
            var mortgageEx = overrides?.MortageRegistrationCost ?? settings.MortageRegistrationCost ?? 0m;

            var vatPctCostsOther = 21m;
            var vatPctCostsConnection = vatPctBase;

            var vm = new ProjectCostCalculationVM
            {
                ProjectId = projectId,
                VatPercent = vatPctBase,
                RegistrationPercent = regPctBase,
                RegistrationType = (Models.Projecten.RegistrationType)settings.RegistrationType,
                MixedVatRegistration = settings.MixedVatRegistration ?? false,

                FixedCertificateCost = fixedActeEx,
                SurveyorFee = surveyorEx,
                ConnectionFee = connectionEx,
                BaseDeedShare = baseDeedEx,
                ParcelCost = parcelEx,
                MortgageRegistrationCost = mortgageEx,

                VatPctCostsOther = vatPctCostsOther,
                VatPctCostsConnection = vatPctCostsConnection
            };

            var mode = vm.RegistrationType;
            if (vm.MixedVatRegistration && mode != Models.Projecten.RegistrationType.Mixed) mode = Models.Projecten.RegistrationType.Mixed;

            decimal totalNetLand = 0m, totalNetBuild = 0m;
            int countIncluded = 0;

            foreach (var u in units)
            {
                var land = u.LandValue ?? 0m;
                var build = (u.ConstructionValues?.Sum(x => x.Value ?? 0m)) ?? 0m;

                decimal landDisc = 0m, buildDisc = 0m;
                bool include = ShouldDefaultInclude(u);

                if (discounts != null && discounts.TryGetValue(u.Id, out var d))
                {
                    if (d.LandDiscount > 0) landDisc = Math.Min(d.LandDiscount.Value, land);
                    if (d.BuildDiscount > 0) buildDisc = Math.Min(d.BuildDiscount.Value, build);
                    if (d.IncludePerUnitCosts.HasValue) include = d.IncludePerUnitCosts.Value;
                    if (d.IncludePerUnitCosts.HasValue)
                        include = d.IncludePerUnitCosts.Value;
                }

                var netLand = Math.Max(0m, land - landDisc);
                var netBuild = Math.Max(0m, build - buildDisc);
                var baseSum = netLand + netBuild;

                totalNetLand += netLand;
                totalNetBuild += netBuild;

                decimal vatOnBase = 0m, regOnBase = 0m;
                switch (mode)
                {
                    case Models.Projecten.RegistrationType.Vat:
                        vatOnBase = Percent(baseSum, vatPctBase); break;
                    case Models.Projecten.RegistrationType.Registration:
                        regOnBase = Percent(baseSum, regPctBase); break;
                    case Models.Projecten.RegistrationType.Mixed:
                        vatOnBase = Percent(netBuild, vatPctBase);
                        regOnBase = Percent(netLand, regPctBase);
                        break;
                }

                decimal costsExcl = 0m, costsVat = 0m;
                if (include)
                {
                    countIncluded++;
                    costsExcl = surveyorEx + connectionEx + baseDeedEx + parcelEx;
                    costsVat = Percent(surveyorEx + baseDeedEx + parcelEx, vatPctCostsOther)
                              + Percent(connectionEx, vatPctCostsConnection);
                }

                vm.Lines.Add(new UnitCostLineVM
                {
                    UnitId = u.Id,
                    Code = string.IsNullOrWhiteSpace(u.Name) ? $"U{u.Id}" : u.Name,

                    LandBase = land,
                    BuildBase = build,
                    LandDiscount = landDisc,
                    BuildDiscount = buildDisc,

                    VatAmount = vatOnBase,
                    RegistrationAmount = regOnBase,

                    IncludePerUnitCosts = include,
                    CostsExcl = costsExcl,
                    CostsVat = costsVat
                });
            }

            // Globaal: notaris (schijven) + 21% btw
            var notaryEx = CalculateNotaryFeesFromTotals(totalNetLand, totalNetBuild, mode == Models.Projecten.RegistrationType.Mixed);
            var notaryVat = Percent(notaryEx, 21m);

            // Globaal: vaste akte + hypo (21% btw)
            var fixedActeVat = Percent(fixedActeEx, 21m);
            var mortgageVat = Percent(mortgageEx, 21m);

            // Per-unit totalen (alleen de inbegrepen rijen)
            vm.CostTotals = new CostTotalsVM
            {
                NotaryExcl = notaryEx,
                NotaryVat = notaryVat,
                FixedActeExcl = fixedActeEx,
                FixedActeVat = fixedActeVat,
                MortgageExcl = mortgageEx,
                MortgageVat = mortgageVat,

                SurveyorExcl = surveyorEx * countIncluded,
                SurveyorVat = Percent(surveyorEx, vatPctCostsOther) * countIncluded,

                ConnectionExcl = connectionEx * countIncluded,
                ConnectionVat = Percent(connectionEx, vatPctCostsConnection) * countIncluded,

                BaseDeedExcl = baseDeedEx * countIncluded,
                BaseDeedVat = Percent(baseDeedEx, vatPctCostsOther) * countIncluded,

                ParcelExcl = parcelEx * countIncluded,
                ParcelVat = Percent(parcelEx, vatPctCostsOther) * countIncluded
            };

            return vm;
        }



        // ========== PROJECT DETAIL INVOICING ==========

        // GET: /Projecten/Invoicing?projectid=123
        [HttpGet]
        public IActionResult Invoicing(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectInvoicing = _ps.HasWrite(PermissionCodes.ProjectsInvoicing);


            // In je bestaande code gaat dit via ServiceFactory.
            var projectService = _projectService;
            var clientService = _clientService;
            var unitService = _unitService;

            var model = new ProjectInvoicingModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid)
            };

            var projectResponse = projectService.GetProjectByID(projectid);
            if (projectResponse.Success && projectResponse.Value is not null)
            {
                model.ProjectName = projectResponse.Value.Name;
                model.IssuerCompanyIdBuilder = projectResponse.Value.IssuerCompanyIdBuilder;
                model.IssuerCompanyIdLandOwner = projectResponse.Value.IssuerCompanyIdLandOwner;
            }

            // ===== Schijven (vordering der werken) =====
            var respUnits = projectService.GetProjectInvoicableUnits(projectid);
            if (respUnits.Success)
            {
                var accountIds = respUnits.Values.Select(v => v.Unit.ClientAccountId)
                                                 .Where(id => id.HasValue).Select(id => id!.Value)
                                                 .Distinct().ToList();

                var respClients = clientService.GetClientAccountByIds(accountIds);
                if (respClients.Success)
                {
                    foreach (var client in respClients.Values)
                    {
                        var group = new ClientAccountWithInvoicableBO { Client = client };
                        foreach (var u in respUnits.Values.Where(v => v.Unit.ClientAccountId == client.Id))
                            group.Units.Add(u);
                        model.ClientAccounts.Add(group);
                    }
                }
            }

            // ===== Meer-/minwerken =====
            var respCo = projectService.GetProjectInvoicableChangeOrders(projectid);
            if (respCo.Success)
            {
                var accountIds = respCo.Values.Select(v => v.ClientAccountID).Distinct().ToList();
                var respClients = clientService.GetClientAccountByIds(accountIds);

                var coUow = _uow;
                var db = (cpmRunningContext)coUow.Context;
                var detailIds = respCo.Values
                    .SelectMany(co => co.Details ?? new List<ChangeOrderDetailBO>())
                    .Select(d => d.Id)
                    .Distinct()
                    .ToList();

                var invoicedAmounts = db.InvoicesDetails
                    .AsNoTracking()
                    .Where(d => d.LineType == "ChangeOrders" && d.ChangeOrderDetailId.HasValue && detailIds.Contains(d.ChangeOrderDetailId.Value))
                    .GroupBy(d => d.ChangeOrderDetailId!.Value)
                    .Select(g => new { DetailId = g.Key, Amount = g.Sum(x => x.Price ?? 0m) })
                    .ToDictionary(x => x.DetailId, x => x.Amount);

                if (respClients.Success)
                {
                    foreach (var client in respClients.Values)
                    {
                        var group = new ClientAccountWithInvoicableChangeOrderBO { Client = client };
                        foreach (var co in respCo.Values.Where(v => v.ClientAccountID == client.Id))
                            group.ChangeOrders.Add(co);
                        model.ClientChangeOrders.Add(group);
                        var rows = new List<ChangeOrderInvoicingRowVM>();
                        foreach (var co in group.ChangeOrders)
                        {
                            foreach (var detail in co.Details.Where(d => d.Invoicable != false))
                            {
                                var total = detail.Totaal;
                                invoicedAmounts.TryGetValue(detail.Id, out var invoiced);
                                var remaining = Math.Round(total - invoiced, 2, MidpointRounding.AwayFromZero);
                                var maxPct = total == 0m ? 0m : Math.Round((remaining / total) * 100m, 2, MidpointRounding.AwayFromZero);
                                if (maxPct < 0m) maxPct = 0m;
                                if (maxPct > 100m) maxPct = 100m;
                                if (remaining <= 0m) continue;

                                rows.Add(new ChangeOrderInvoicingRowVM
                                {
                                    ChangeOrderId = co.Id,
                                    ChangeOrderDetailId = detail.Id,
                                    ChangeOrderDescription = co.Description ?? string.Empty,
                                    DetailDescription = detail.Description ?? string.Empty,
                                    TotalAmount = total,
                                    InvoicedAmount = invoiced,
                                    RemainingAmount = remaining,
                                    MaxPercentage = maxPct,
                                    DefaultPercentage = maxPct,
                                    VatPercentage = detail.VatPercentage ?? 21m
                                });
                            }
                        }

                        model.ChangeOrderInvoicingClients.Add(new ChangeOrderInvoicingClientVM
                        {
                            Client = client,
                            Rows = rows
                        });
                    }
                }
            }

            // Sortering zoals je VB: op eerste woon-unitnaam (GroupId 1) anders eerste unitnaam
            model.ClientAccounts = model.ClientAccounts
                .OrderBy(m =>
                {
                    var woon = m.Units.FirstOrDefault(a => a.Unit.Type.GroupId == 1)?.Unit.Name
                               ?? m.Units.FirstOrDefault()?.Unit.Name
                               ?? "";
                    return woon;
                }, new ServiceCore.Helpers.AlphanumComparator()) // jouw bestaande comparer
                .ToList();

            //// ===== Nuts (optioneel) =====
            //var respClientUnits = clientService.GetClientAccountsByProjectIdWithUnits(projectid);
            //if (respClientUnits.Success)
            //{
            //    foreach (var client in respClientUnits.Values)
            //    {
            //        if (client.Units.Any(u => u.Type.GroupId == 1 || u.Type.GroupId == 4))
            //        {
            //            // Deze methode heb je al in jouw code
            //            var cuc = projectService.GetClientUtilityCost(client.Client.Id, projectid); // moet ClientUtilityCostVM opleveren
            //            if (cuc != null) model.ClientUtilityCosts.Add(cuc);
            //        }
            //    }
            //}



            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Invoicing", "Projecten", "Facturatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;


            return View(model);
        }

        [HttpGet]
        public IActionResult PaymentStages(int projectid)
        {
            var projectService = _projectService;

            var model = new ProjectPaymentStagesModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid)
            };

            var response = projectService.GetProjectPaymentGroups(projectid);
            if (response.Success)
                model.Groups = response.Values;

            var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = dashboard,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentStages), "Projecten", "Betalingsschijven")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        [HttpGet]
        public IActionResult PaymentStagesAddUpdate(int projectid, int groupid = 0)
        {
            var projectService = _projectService;
            var model = new ProjectPaymentStagesAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid)
            };

            if (groupid == 0)
            {
                model.Stages.Add(new ProjectPaymentStageBO());
            }
            else
            {
                var response = projectService.GetProjectPaymentGroup(groupid);
                if (response.Success && response.Value is not null)
                {
                    model.Group = response.Value;
                    foreach (var stage in model.Group.PaymentStages)
                    {
                        model.Stages.Add(stage);
                    }
                }

                if (model.Stages.Count == 0)
                    model.Stages.Add(new ProjectPaymentStageBO());
            }

            ViewBag.VatTypes = GetVatTypeSelectList(projectid);

            var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = dashboard,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var paymentStages = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentStages), "Projecten", "Betalingsschijven")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentStagesAddUpdate), "Projecten", model.Group.Id == 0 ? "Betalingsgroep toevoegen" : "Betalingsgroep bewerken")
            {
                Parent = paymentStages,
                RouteValues = new { projectid = projectid, groupid = groupid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaymentStagesAddUpdate(ProjectPaymentStagesAddUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.VatTypes = GetVatTypeSelectList(model.ProjectId);
                return View(model);
            }

            foreach (var stage in model.Stages)
            {
                stage.GroupId = model.Group.Id;
                model.Group.PaymentStages.Add(stage);
            }

            var service = _projectService;
            model.Group.ProjectId = model.ProjectId;
            var response = service.InsertUpdateProjectPaymentGroup(model.Group);

            if (response.Success)
            {
                AddMessage("success", $"De betalingsschijven zijn met succes aan het project {model.ProjectName} toegevoegd", "Geslaagd!");
                return RedirectToAction("PaymentStages", new { projectid = model.ProjectId });
            }

            AddMessage("error", $"De betalingsschijven zijn NIET aan het project {model.ProjectName} toegevoegd", "Fout!");
            ViewBag.VatTypes = GetVatTypeSelectList(model.ProjectId);
            return View(model);
        }

        [HttpPost]
        public PartialViewResult BlankStageRow()
        {
            return PartialView("Partials/_PaymentStageRow", new ProjectPaymentStageBO());
        }

        [HttpGet]
        public IActionResult PaymentGroupLink(int projectid)
        {
            var projectService = _projectService;
            var unitService = _unitService;

            var model = new ProjectPaymentGroupLinkModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid),
                Units = new List<UnitBO>(),
                PaymentGroups = new List<IdNameBO>()
            };

            var response = projectService.GetProjectPaymentGroupsForSelect(projectid);
            if (response.Success)
                model.PaymentGroups = response.Values;

            var responseUnits = unitService.GetUnitsByProjectId(projectid);
            if (responseUnits.Success)
                model.Units = responseUnits.Values;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaymentGroupLink(ProjectPaymentGroupLinkModel model)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("Error", "De betalingsschijven zijn NIET gelinkt", "Fout!");
                var projectService = _projectService;
                var unitService = _unitService;

                var responseGroups = projectService.GetProjectPaymentGroupsForSelect(model.ProjectId);
                if (responseGroups.Success)
                    model.PaymentGroups = responseGroups.Values;

                var responseUnits = unitService.GetUnitsByProjectId(model.ProjectId);
                if (responseUnits.Success)
                    model.Units = responseUnits.Values;

                return View(model);
            }

            var service = _projectService;
            model.Units ??= new List<UnitBO>();
            foreach (var unit in model.Units)
            {
                if (unit.PaymentGroupId.HasValue && unit.PaymentGroupId.Value > 0)
                    service.LinkPaymentGroupToUnit(unit.Id, unit.PaymentGroupId.Value);
            }

            AddMessage("success", "De betalingsschijven zijn met succes gelinkt", "Geslaagd!");
            return RedirectToAction("PaymentStages", new { projectid = model.ProjectId });
        }

        [HttpPost]
        public JsonResult PaymentStagesInvoicable(int stageid, bool value)
        {
            var service = _projectService;
            var response = service.UpdateProjectPaymentStageInvoicable(stageid, value);
            return Json(new { success = response.Success });
        }

        private List<SelectListItem> GetVatTypeSelectList(int projectId)
        {
            var vatTypes = new List<SelectListItem>();
            var projectService = _projectService;
            var projectResponse = projectService.GetProjectByID(projectId);
            var issuerId = projectResponse.Success ? projectResponse.Value?.IssuerCompanyIdBuilder : null;

            if (!issuerId.HasValue)
                return vatTypes;

            var issuerService = new IssuerCompanyService(_uow);
            var issuerVatTypes = issuerService.ListVatTypeAsync(issuerId.Value).GetAwaiter().GetResult();
            vatTypes = issuerVatTypes
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(CultureInfo.InvariantCulture),
                    Text = string.IsNullOrWhiteSpace(v.Code)
                        ? $"{v.BasePercentage:0.##}%"
                        : $"{v.Code} ({v.BasePercentage:0.##}%)"
                })
                .ToList();

            return vatTypes;
        }
        // POST: schijven factureren
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeInvoices([FromBody] MakeInvoicesRequest request)
        {
            if (request?.Invoices == null || request.Invoices.Count == 0)
                return Json(new { projectid = 0 });

            var clientService = _clientService;
            var unitService = _unitService;
            var projectService = _projectService;

            var response = new Response();
            int projectId = 0;

            var drafts = new List<InvoiceDraftBO>();
            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));
            var db = (DALCore.Models.cpmRunningContext)uow.Context;

            // Alle betrokken klanten ophalen
            var clientIds = request.Invoices.Select(x => x.ClientAccountId).Distinct().ToList();
            var clientResp = clientService.GetClientAccountByIds(clientIds);
            var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();

         

            foreach (var client in clientAccounts)
            {
                var iu = new List<UnitWithStagesBO>();
                var stageMap = new List<ProjectPaymentStageBO>();

                // Units + stages
                foreach (var unitId in request.Invoices.Where(i => i.ClientAccountId == client.Id).Select(i => i.Unitid).Distinct())
                {
                    var unitbo = new UnitWithStagesBO();
                    var ru = unitService.GetUnitById(unitId);
                    if (ru.Success)
                    {
                        unitbo.Unit = ru.Value;
                        foreach (var item in request.Invoices.Where(i => i.Unitid == unitbo.Unit.Id))
                        {
                            var rStage = projectService.GetProjectPaymentStage(item.StageId);
                            if (rStage.Success)
                            {
                                unitbo.PaymentStages.Add(rStage.Value);
                                stageMap.Add(rStage.Value);
                            }
                        }
                        iu.Add(unitbo);
                    }
                }

                // Project + salessettings
                var project = new ProjectBO();
                var settings = new ProjectSalesSettingsBO();
                if (iu.Count > 0)
                {
                    var pr = projectService.GetProjectByID(iu.First().Unit.ProjectId);
                    if (pr.Success)
                    {
                        project = pr.Value;
                        var sr = projectService.GetSalesSettings(project.Id);
                        if (sr.Success) settings = sr.Value;
                    }
                }

                var stageIds = request.Invoices
                     .Where(i => i.ClientAccountId == client.Id)
                     .Select(i => i.StageId)
                     .Distinct()
                     .ToList();

                if (stageIds.Count > 0 && project.Id > 0)
                {
                    var coowners = db.ClientContacts
                       .AsNoTracking()
                       .Where(cc => cc.ClientAccountId == client.Id && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue)
                       .Select(cc => new { cc.Id, cc.CoOwnerPercentage })
                       .ToList();

                    var coOwnerTotal = coowners.Sum(c => c.CoOwnerPercentage ?? 0m);
                    var mainOwnerShare = Math.Max(0m, 100m - coOwnerTotal);
                    var issuerCompanyId = project.IssuerCompanyIdBuilder ?? request.Invoices.First().CompanyId;
                    if (issuerCompanyId <= 0)
                    {
                        response.AddError("Geen facturatiebedrijf geselecteerd voor het project.");
                        continue;
                    }
                    var paymentGroupId = stageMap.FirstOrDefault(s => stageIds.Contains(s.Id))?.GroupId;
                    var mainDraft = BuildStageInvoiceDraft(
                       issuerCompanyId,
                       client.Id,
                       null,
                       stageIds,
                       project,
                       settings,
                       iu,
                       paymentGroupId,
                       mainOwnerShare,
                       db);

                    if (mainDraft != null)
                        drafts.Add(mainDraft);

                    foreach (var coowner in coowners)
                    {
                        if (coowner.CoOwnerPercentage.GetValueOrDefault() <= 0m)
                            continue;

                        var coownerDraft = BuildStageInvoiceDraft(
                            issuerCompanyId,
                            null,
                            coowner.Id,
                            stageIds,
                            project,
                            settings,
                            iu,
                            paymentGroupId,
                            coowner.CoOwnerPercentage ?? 0m,
                            db);

                        if (coownerDraft != null)
                            drafts.Add(coownerDraft);
                    }
                }

                    projectId = project.Id;
            }
            foreach (var draft in drafts)
            {
                try
                {
                    await cmd.CreateWithLinesAsync(draft, issueNow: false);
                }
                catch (Exception ex)
                {
                    response.AddError(ex.Message);
                }
            }


            if (response.Success)
            {
                AddMessage("success", "De conceptfacturen zijn aangemaakt", "Gelukt!");
                return Json(new { projectid = projectId });
            }
            else
            {
                AddMessage("Error", "Niet alle facturen zijn aangemaakt. Probeer opnieuw of contacteer de administrator.", "Fout!");
                return Json(new { projectid = projectId });
            }
        }

        // POST: meerwerken factureren
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeInvoicesCO([FromBody] MakeInvoicesCoRequest request)
        {
            if (request?.Invoices == null || request.Invoices.Count == 0)
                return Json(new { projectid = 0 });

            request.Invoices = request.Invoices
                .Where(i => i.ChangeOrderDetailId > 0 && i.Percentage > 0m)
                .ToList();

            if (request.Invoices.Count == 0)
                return Json(new { projectid = 0 });

            var clientService = _clientService;
            var unitService = _unitService;
            var projectService = _projectService;

            var response = new Response();
            int projectId = 0;

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var clientIds = request.Invoices.Select(x => x.ClientAccountId).Distinct().ToList();
            var clientResp = clientService.GetClientAccountByIds(clientIds);
            var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();
            var selectedDetailIds = request.Invoices.Select(i => i.ChangeOrderDetailId).Distinct().ToList();
            var alreadyInvoicedByDetail = uow.Context.Set<InvoicesDetails>()
                .AsNoTracking()
                .Where(d => d.LineType == "ChangeOrders" && d.ChangeOrderDetailId.HasValue && selectedDetailIds.Contains(d.ChangeOrderDetailId.Value))
                .GroupBy(d => d.ChangeOrderDetailId!.Value)
                .Select(g => new { DetailId = g.Key, Amount = g.Sum(x => x.Price ?? 0m) })
                .ToDictionary(x => x.DetailId, x => x.Amount);


            foreach (var client in clientAccounts)
            {
                var respUnits = unitService.GetUnitsByAccountId(client.Id);
                var units = respUnits.Success ? respUnits.Values : new List<UnitBO>();

                var changeOrders = new List<ChangeOrderBO>();
                foreach (var coId in request.Invoices.Where(i => i.ClientAccountId == client.Id).Select(i => i.ChangeOrderId).Distinct())
                {
                    var rco = projectService.GetChangeOrder(coId);
                    if (rco.Success)
                    {
                        var co = rco.Value;
                        // Filter alleen de aangevinkte details
                        for (int i = co.Details.Count - 1; i >= 0; i--)
                        {
                            var keep = request.Invoices.Any(l => l.ChangeOrderDetailId == co.Details[i].Id);
                            if (!keep) co.Details.RemoveAt(i);
                        }
                        changeOrders.Add(co);
                    }
                }

                var project = new ProjectBO();
                if (changeOrders.Count > 0)
                {
                    var pr = projectService.GetProjectByID(changeOrders.First().ProjectId);
                    if (pr.Success)
                    {
                        project = pr.Value;

                    }
                }

                if (changeOrders.Count > 0 && project.Id > 0)
                {
                    var issuerCompanyId = project.IssuerCompanyIdBuilder ?? request.Invoices.FirstOrDefault()?.CompanyId;
                    if (!issuerCompanyId.HasValue || issuerCompanyId.Value <= 0)
                    {
                        response.AddError("Geen facturatiebedrijf geselecteerd voor het project.");
                        continue;
                    }
                    var coDraft = BuildChangeOrderInvoiceDraft(
                        issuerCompanyId,
                        client.Id,
                        changeOrders,
                        request.Invoices.Where(i => i.ClientAccountId == client.Id).ToList(),
                        alreadyInvoicedByDetail,
                        project);

                    if (coDraft != null)
                    {
                        try
                        {
                            await cmd.CreateWithLinesAsync(coDraft, issueNow: false);
                        }
                        catch (Exception ex)
                        {
                            response.AddError(ex.Message);
                        }
                    }
                }

                projectId = project.Id;
            }

            if (response.Success)
            {
                AddMessage("success", "De conceptfacturen zijn aangemaakt", "Gelukt!");
                return Json(new { projectid = projectId });
            }
            else
            {
                AddMessage("Error", "Niet alle facturen zijn aangemaakt. Probeer opnieuw of contacteer de administrator.", "Fout!");
                return Json(new { projectid = projectId });
            }
        }

        // ====== DTO’s voor de JSON-body ======
        public class MakeInvoicesRequest
        {
            public List<ClientAccountUnitInvoiceBO> Invoices { get; set; } = new();
        }

        public class MakeInvoicesCoRequest
        {
            public List<ClientAccountChangeOrderInvoiceBO> Invoices { get; set; } = new();
        }

        public class MakeCoordSliceInvoicesRequest
        {
            public int ProjectId { get; set; }
            public List<int> SliceIds { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCoordSliceInvoices(int projectId, List<int> sliceIds)
        {
            if (sliceIds == null || sliceIds.Count == 0)
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            var proj = _db.Project
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => new { p.ProjectId, p.CoordinationIssuerCompanyId, p.BuilderId })
                .FirstOrDefault();

            if (proj == null || !proj.CoordinationIssuerCompanyId.HasValue)
            {
                AddMessage("error", "Geen coördinatiebedrijf ingesteld.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            if (!proj.BuilderId.HasValue)
            {
                AddMessage("error", "Geen bouwheer ingesteld voor dit project.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            var issuerCompanyId = proj.CoordinationIssuerCompanyId.Value;

            // Ontvanger: bouwheer van het project
            var linkedCompanyId = proj.BuilderId;

            // Contractprijs ophalen
            var contractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault() ?? 0m;

            // Geselecteerde schijven ophalen
            var slices = _db.ProjectContractSlice
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && sliceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Description, s.Percentage })
                .ToList();

            if (!slices.Any())
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId,
                InvoiceDate     = DateOnly.FromDateTime(DateTime.Today),
                Mode            = InvoiceMode.Free,
                ProjectId       = projectId,
                CompanyId       = linkedCompanyId
            };

            foreach (var slice in slices)
            {
                var amount = contractPrice * slice.Percentage / 100m;
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text           = $"Projectcoördinatie – {slice.Description} ({slice.Percentage:0.##}%)",
                    Price          = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                    VatPercentage  = 21m,
                    LineType       = "detail"
                });
            }

            try
            {
                var (invoiceId, _) = await cmd.CreateWithLinesAsync(draft, issueNow: false);

                // Koppel de nieuwe factuur aan de geselecteerde schijven
                var sliceEntities = _db.ProjectContractSlice
                    .Where(s => s.ProjectId == projectId && sliceIds.Contains(s.Id))
                    .ToList();
                foreach (var s in sliceEntities)
                    s.InvoiceId = invoiceId;
                await _db.SaveChangesAsync();

                AddMessage("success", "Het conceptfactuur is aangemaakt.", "Gelukt!");
            }
            catch (Exception ex)
            {
                AddMessage("error", $"Factuur kon niet worden aangemaakt: {ex.Message}", "Fout!");
            }

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        // ── Regie-uren factureren ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCoordRegieInvoice(int projectId, List<int> regieUurIds)
        {
            if (regieUurIds == null || regieUurIds.Count == 0)
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            var proj = _db.Project
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => new {
                    p.CoordinationIssuerCompanyId, p.KmAllowance, p.ProjectDistanceKm,
                    p.BuilderId, p.CoordinationReference, p.ProjectName
                })
                .FirstOrDefault();

            if (proj == null || !proj.CoordinationIssuerCompanyId.HasValue)
            {
                AddMessage("error", "Geen coördinatiebedrijf ingesteld.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            if (!proj.BuilderId.HasValue)
            {
                AddMessage("error", "Geen bouwheer ingesteld voor dit project.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            var issuerCompanyId = proj.CoordinationIssuerCompanyId.Value;
            var kmAllowance     = proj.KmAllowance ?? 0m;
            var roundTripKm     = (proj.ProjectDistanceKm ?? 0m) * 2m;

            // Niet-gefactureerde regie-uren ophalen
            var entries = _db.ProjectRegieUur
                .Include(r => r.User)
                .Where(r => r.ProjectId == projectId && regieUurIds.Contains(r.Id) && r.InvoiceId == null)
                .OrderBy(r => r.Date).ThenBy(r => r.UserId)
                .ToList();

            if (!entries.Any())
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            // Uurtarieven ophalen
            var ratesResp = _projectService.GetProjectHourlyRates(projectId);
            var rateMap   = ratesResp.Success
                ? ratesResp.Values.ToDictionary(r => r.UserId, r => r.HourlyRate)
                : new Dictionary<string, decimal>();

            // Cutoff datum = meest recente datum in de selectie
            var cutoffDate = entries.Max(e => e.Date);
            var cutoffStr  = cutoffDate.ToString("dd/MM/yyyy");

            // Groepeer per medewerker
            var perUser = entries
                .GroupBy(e => e.UserId)
                .Select(g =>
                {
                    var hourlyRate = rateMap.TryGetValue(g.Key, out var r) ? r : 0m;
                    var fullName   = $"{g.First().User?.Voornaam} {g.First().User?.Familienaam}".Trim();
                    return new
                    {
                        FullName   = fullName,
                        HourlyRate = hourlyRate,
                        TotalHours = g.Sum(e => e.Hours),
                        Entries    = g.OrderBy(e => e.Date).ToList()
                    };
                })
                .ToList();

            // Verplaatsingen — gebruik TravelKm per rij; fallback op roundTripKm voor oude rijen
            var travelEntries = entries.Where(e => e.WithTravel || (e.TravelKm.HasValue && e.TravelKm > 0)).ToList();
            var tripCount    = travelEntries.Count;
            var totalKm      = travelEntries.Sum(e => e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : roundTripKm);
            var totalKmCost  = Math.Round(totalKm * kmAllowance, 2, MidpointRounding.AwayFromZero);

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId   = issuerCompanyId,
                InvoiceDate       = DateOnly.FromDateTime(DateTime.Today),
                Mode              = InvoiceMode.Free,
                ProjectId         = projectId,
                CompanyId         = proj.BuilderId,
                HeaderDescription = $"Prestaties voor het project {proj.ProjectName} tot {cutoffStr}",
                DetailDescription = string.IsNullOrWhiteSpace(proj.CoordinationReference) ? null : proj.CoordinationReference.Trim()
            };

            // Één lijn per medewerker
            foreach (var u in perUser)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"Prestaties {u.FullName}",
                    Quantity      = u.TotalHours,
                    UnitPrice     = u.HourlyRate,
                    Price         = Math.Round(u.TotalHours * u.HourlyRate, 2, MidpointRounding.AwayFromZero),
                    VatPercentage = 21m,
                    LineType      = "detail"
                });
            }

            // Km-lijn (enkel indien er verplaatsingen zijn)
            if (tripCount > 0 && totalKm > 0)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"{tripCount} verplaatsing{(tripCount == 1 ? "" : "en")} \u2013 {totalKm:0}\u00a0km totaal aan {kmAllowance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}\u20ac/km",
                    Quantity      = totalKm,
                    UnitPrice     = kmAllowance,
                    Price         = totalKmCost,
                    VatPercentage = 21m,
                    LineType      = "detail"
                });
            }

            try
            {
                var (invoiceId, _) = await cmd.CreateWithLinesAsync(draft, issueNow: false);
                _projectService.MarkRegieUrenAsInvoiced(entries.Select(e => e.Id).ToList(), invoiceId);

                // Detailbijlage genereren en opslaan
                var appendixBytes = BuildRegieAppendix(
                    proj.ProjectName, cutoffStr, perUser.Select(u => new RegieAppendixUser
                    {
                        FullName   = u.FullName,
                        HourlyRate = u.HourlyRate,
                        TotalHours = u.TotalHours,
                        Entries    = u.Entries.Select(e => new RegieAppendixEntry
                        {
                            Date        = e.Date,
                            Hours       = e.Hours,
                            EntryKm     = e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : (e.WithTravel ? roundTripKm : 0m),
                            HourAmount  = e.Hours * (rateMap.TryGetValue(e.UserId, out var hr) ? hr : 0m)
                        }).ToList()
                    }).ToList(),
                    tripCount, kmAllowance, totalKmCost);

                var invoice = await _db.Invoices.FindAsync(invoiceId);
                if (invoice != null && appendixBytes.Length > 0)
                {
                    invoice.PdfAppendixFileName = $"Prestatielijst_{cutoffDate:yyyyMMdd}.pdf";
                    invoice.PdfAppendixContent  = appendixBytes;
                    await _db.SaveChangesAsync();
                }

                AddMessage("success", $"Conceptfactuur aangemaakt voor {entries.Count} prestaties.", "Gelukt!");
            }
            catch (Exception ex)
            {
                AddMessage("error", $"Factuur kon niet worden aangemaakt: {ex.Message}", "Fout!");
            }

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        // ── Regie-factuur bijlage (QuestPDF) ─────────────────────────────────

        private sealed class RegieAppendixUser
        {
            public string  FullName   { get; set; }
            public decimal HourlyRate { get; set; }
            public decimal TotalHours { get; set; }
            public List<RegieAppendixEntry> Entries { get; set; } = new();
        }

        private sealed class RegieAppendixEntry
        {
            public DateOnly Date       { get; set; }
            public decimal  Hours      { get; set; }
            public decimal  EntryKm    { get; set; }   // 0 = geen verplaatsing
            public decimal  HourAmount { get; set; }
        }

        private static byte[] BuildRegieAppendix(
            string projectName, string cutoffStr,
            List<RegieAppendixUser> users,
            int tripCount, decimal kmAllowance, decimal totalKmCost)
        {
            var culture = new System.Globalization.CultureInfo("nl-BE");
            string Eur(decimal v)  => "\u20ac\u00a0" + v.ToString("N2", culture);
            string Num(decimal v)  => v.ToString("N2", culture);

            var headerColor = QuestPDF.Helpers.Colors.Grey.Lighten3;
            var borderColor = QuestPDF.Helpers.Colors.Grey.Medium;

            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Prestatielijst \u2013 {projectName}")
                            .Bold().FontSize(13);
                        col.Item().Text($"Periode tot en met {cutoffStr}")
                            .FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(borderColor);
                    });

                    page.Content().PaddingTop(12).Column(main =>
                    {
                        // ── Per medewerker ─────────────────────────────────
                        foreach (var user in users)
                        {
                            main.Item().PaddingBottom(12).Column(userCol =>
                            {
                                // Sectieheader medewerker
                                userCol.Item().Background(headerColor).Padding(4)
                                    .Text(user.FullName).Bold().FontSize(10);

                                // Tabel
                                userCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2);  // Datum
                                        c.RelativeColumn(1);  // Uren
                                        c.RelativeColumn(2);  // Verplaatsing
                                        c.RelativeColumn(2);  // Bedrag uren
                                        c.RelativeColumn(2);  // Totaal
                                    });

                                    // Header rij
                                    void HeaderCell(string text, bool right = false)
                                    {
                                        var cell = table.Cell().Background(headerColor).Padding(3);
                                        if (right) cell.AlignRight().Text(text).Bold().FontSize(8);
                                        else       cell.Text(text).Bold().FontSize(8);
                                    }

                                    HeaderCell("Datum");
                                    HeaderCell("Uren");
                                    HeaderCell("Verplaatsing");
                                    HeaderCell("Bedrag uren", right: true);
                                    HeaderCell("Totaal",      right: true);

                                    // Datarijen
                                    foreach (var e in user.Entries)
                                    {
                                        void DataCell(string text, bool right = false)
                                        {
                                            var cell = table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(3);
                                            if (right) cell.AlignRight().Text(text);
                                            else       cell.Text(text);
                                        }

                                        DataCell(e.Date.ToString("dd/MM/yyyy"));
                                        DataCell(Num(e.Hours));         // Uren: links uitgelijnd
                                        DataCell(e.EntryKm > 0 ? $"{e.EntryKm:0}\u00a0km" : "\u2014");
                                        DataCell(Eur(e.HourAmount), right: true);
                                        DataCell(Eur(e.HourAmount), right: true);  // Totaal = alleen uursbedrag
                                    }

                                    // Subtotaalrij (totaal = som uursbedragen)
                                    var subTotal = user.Entries.Sum(e => e.HourAmount);
                                    table.Cell().ColumnSpan(4).Padding(3).AlignRight()
                                         .Text($"Subtotaal {user.FullName}:").Bold();
                                    table.Cell().Padding(3).AlignRight()
                                         .Text(Eur(subTotal)).Bold();
                                });
                            });
                        }

                        // ── Verplaatsingen ─────────────────────────────────
                        if (tripCount > 0 && totalKmCost > 0)
                        {
                            main.Item().PaddingBottom(12).Column(kmCol =>
                            {
                                kmCol.Item().Background(headerColor).Padding(4)
                                    .Text("Verplaatsingen").Bold().FontSize(10);
                                kmCol.Item().Table(kmTable =>
                                {
                                    kmTable.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                    });

                                    void KH(string t, bool right = false)
                                    {
                                        var cell = kmTable.Cell().Background(headerColor).Padding(3);
                                        if (right) cell.AlignRight().Text(t).Bold().FontSize(8);
                                        else       cell.Text(t).Bold().FontSize(8);
                                    }
                                    KH("Omschrijving"); KH("Aantal", right: true); KH("Prijs/eenheid", right: true); KH("Totaal", right: true);

                                    var totalKmAll = users.SelectMany(u => u.Entries).Sum(e => e.EntryKm);
                                    kmTable.Cell().Padding(3).Text($"{totalKmAll:0}\u00a0km totaal");
                                    kmTable.Cell().Padding(3).AlignRight().Text($"{tripCount}\u00d7");
                                    kmTable.Cell().Padding(3).AlignRight().Text($"{Eur(kmAllowance)}/km");
                                    kmTable.Cell().Padding(3).AlignRight().Text(Eur(totalKmCost)).Bold();
                                });
                            });
                        }

                        // ── Eindtotaal ─────────────────────────────────────
                        var grandTotal = users.Sum(u => u.Entries.Sum(e => e.HourAmount)) + totalKmCost;
                        main.Item().PaddingTop(4).LineHorizontal(1).LineColor(borderColor);
                        main.Item().PaddingTop(6).AlignRight()
                            .Text($"Totaal excl. btw: {Eur(grandTotal)}").Bold().FontSize(11);
                    });

                    page.Footer().AlignRight()
                        .Text(t =>
                        {
                            t.Span("Pagina ").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.CurrentPageNumber().FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.Span(" van ").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.TotalPages().FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                });
            }).GeneratePdf();
        }

        // ── Regie-uren AJAX endpoints ─────────────────────────────────────────

        public class AddRegieUurRequest
        {
            public int ProjectId { get; set; }
            public string UserId { get; set; }
            public string Date { get; set; }
            public decimal Hours { get; set; }
            public decimal? TravelKm { get; set; }
            public string Description { get; set; }
        }

        [HttpPost]
        public IActionResult AddRegieUur([FromBody] AddRegieUurRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || req.Hours <= 0)
                return BadRequest(new { error = "Ongeldige invoer" });

            if (!DateOnly.TryParse(req.Date, out var date))
                return BadRequest(new { error = "Ongeldige datum" });

            var ratesResp = _projectService.GetProjectHourlyRates(req.ProjectId);
            var hourlyRate = ratesResp.Success
                ? ratesResp.Values.FirstOrDefault(r => r.UserId == req.UserId)?.HourlyRate ?? 0m
                : 0m;

            var bo = new BOCore.ProjectRegieUurBO
            {
                ProjectId   = req.ProjectId,
                UserId      = req.UserId,
                Date        = date,
                Hours       = req.Hours,
                TravelKm    = req.TravelKm.HasValue && req.TravelKm > 0 ? req.TravelKm : null,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
            };

            var resp = _projectService.AddRegieUur(bo);
            if (!resp.Success || !resp.Values.Any())
                return StatusCode(500, new { error = "Opslaan mislukt" });

            var saved = resp.Values.First();
            return Ok(new
            {
                id           = saved.Id,
                userId       = saved.UserId,
                userFullName = saved.UserFullName,
                date         = saved.Date.ToString("yyyy-MM-dd"),
                hours        = saved.Hours,
                travelKm     = saved.TravelKm,
                description  = saved.Description,
                hourlyRate   = hourlyRate
            });
        }

        [HttpPost]
        public IActionResult DeleteRegieUur(int id)
        {
            var resp = _projectService.DeleteRegieUur(id);
            if (resp.Success)
                return Ok();
            var msg = resp.Messages?.FirstOrDefault()?.Message ?? "Verwijderen mislukt";
            return StatusCode(resp.Messages?.Any(m => m.Message?.Contains("gefactureerd") == true) == true ? 409 : 500,
                new { error = msg });
        }

        private static InvoiceDraftBO? BuildStageInvoiceDraft(
                                  int issuerCompanyId,
                                  int? clientAccountId,
                                  int? clientContactId,
                                  IEnumerable<int> stageIds,
                                  ProjectBO project,
                                  ProjectSalesSettingsBO settings,
                                  IEnumerable<UnitWithStagesBO> units,
                                  int? paymentGroupId,
                                  decimal? ownerPercentageOverride,
                                  DALCore.Models.cpmRunningContext db)
        {
            var groupedStageIds = stageIds?.Distinct().ToList() ?? new List<int>();

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                Mode = InvoiceMode.Stages,
                StageIds = groupedStageIds,
                ProjectId = project.Id,
                PaymentGroupId = paymentGroupId
            };

            decimal ownerPercentage = ownerPercentageOverride ?? 100m;
            string? invoiceExtra = null;

            if (clientAccountId.HasValue)
            {
                (draft.CompanyId, draft.ClientType, draft.ClientId) = (null, (int)InvoicePartyType.ClientAccount, clientAccountId);

                var accountInfo = db.ClientAccount
                    .AsNoTracking()
                    .Where(c => c.Id == clientAccountId.Value)
                    .Select(c => new { c.OwnerPercentage, c.InvoiceExtra })
                    .FirstOrDefault();

                if (!ownerPercentageOverride.HasValue)
                    ownerPercentage = accountInfo?.OwnerPercentage ?? 100m;
                invoiceExtra = accountInfo?.InvoiceExtra;
            }
            else if (clientContactId.HasValue)
            {
                (draft.CompanyId, draft.ClientType, draft.ClientId) = (null, (int)InvoicePartyType.ClientContact, clientContactId);

                var contactInfo = db.ClientContacts
                    .AsNoTracking()
                    .Where(c => c.Id == clientContactId.Value)
                    .Select(c => new
                    {
                        c.CoOwnerPercentage,
                        AccountOwnerPct = c.ClientAccount.OwnerPercentage,
                        AccountInvoiceExtra = c.ClientAccount.InvoiceExtra
                    })
                    .FirstOrDefault();

                if (!ownerPercentageOverride.HasValue)
                    ownerPercentage = contactInfo?.CoOwnerPercentage ?? contactInfo?.AccountOwnerPct ?? 100m;
                invoiceExtra = contactInfo?.AccountInvoiceExtra;
            }

            if (!string.IsNullOrWhiteSpace(invoiceExtra))
                draft.FooterDescription = invoiceExtra;

            if (ownerPercentage <= 0m)
                return null;

            int? issuerBankAccountId = settings?.BankAccountId;
            if (!issuerBankAccountId.HasValue && !string.IsNullOrWhiteSpace(settings?.BankAccountNumber))
            {
                issuerBankAccountId = db.IssuerBankAccount
                    .Where(b => b.IssuerCompanyId == issuerCompanyId && b.Iban == settings.BankAccountNumber)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefault();
            }

            draft.IssuerBankAccountId = issuerBankAccountId;

            var group = paymentGroupId.HasValue
                ? db.InvoicingPaymentGroup.FirstOrDefault(g => g.Id == paymentGroupId.Value)
                : null;

            if (group?.VatTypeId is int vatTypeId)
                draft.SelectedVatTypeId = vatTypeId;

            var groupVat = group?.VatPercentage;
            var stageLines = new List<InvoiceLineBO>();
            var detailLines = new List<string>();
            string? headerUnitDescription = null;
            string? headerAddress = null;
            string? headerCity = project?.Postalcode?.Gemeente;

            foreach (var unit in units)
            {
                var unitName = unit.Unit?.Type != null
                    ? $"{unit.Unit.Type.Name} {unit.Unit.Name}".Trim()
                    : unit.Unit?.Name ?? string.Empty;

                var unitBaseValue = unit.Unit?.ConstructionValues?
                    .Where(v => v.ValueSold > 0 && v.PaymentGroupId == paymentGroupId)
                    .Sum(v => v.ValueSold ?? 0m) ?? 0m;

                if (unitBaseValue > 0)
                {
                    var ownerPortion = Math.Round(unitBaseValue * ownerPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    detailLines.Add($"{ownerPercentage:0.##}% van de bouwwaarde van {unitName} : {ownerPortion:N2} €");

                    headerUnitDescription ??= unitName;
                    headerAddress ??= string.Join(" ", new[]
                    {
                        unit.Unit?.Street,
                        unit.Unit?.HouseNumber,
                        unit.Unit?.BusNumber
                    }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
                }

                foreach (var cv in unit.Unit?.ConstructionValues?.Where(v => v.ValueSold > 0 && v.PaymentGroupId == paymentGroupId) ?? Enumerable.Empty<UnitConstructionValueBO>())
                {
                    foreach (var stage in unit.PaymentStages.Where(s => groupedStageIds.Contains(s.Id) && s.GroupId == paymentGroupId))
                    {
                        var stagePercentage = stage.Percentage;
                        var price = Math.Round(
                            (cv.ValueSold ?? 0m) * ownerPercentage / 100m * stagePercentage / 100m,
                            2,
                            MidpointRounding.AwayFromZero);
                        var vatPct = stage.VatPercentage.HasValue && stage.VatPercentage.Value != 0
                            ? stage.VatPercentage.Value
                            : (groupVat ?? 21m);

                        stageLines.Add(new InvoiceLineBO
                        {
                            Text = $"{stage.Percentage:0.##}% - {stage.Name}",
                            Price = price,
                            VatPercentage = vatPct,
                            VatTypeId = draft.SelectedVatTypeId,
                            PaymentStageId = stage.Id,
                            GroupName = unitName,
                            LineType = "Stages",
                            UnitId = unit.Unit?.Id
                        });
                    }
                }
            }

            if (stageLines.Count > 0)
            {
                draft.Lines = stageLines;

                var unitDescriptor = headerUnitDescription ?? string.Empty;
                var projectPart = string.IsNullOrWhiteSpace(project?.Name) ? string.Empty : $" in project {project.Name}";
                var addressPart = string.IsNullOrWhiteSpace(headerAddress) ? string.Empty : $", {headerAddress}";
                var cityPart = string.IsNullOrWhiteSpace(headerCity) ? string.Empty : $" te {headerCity}";
                var headerText = $"Voor de bouwwaarde van {unitDescriptor}{projectPart}{addressPart}{cityPart} ingevolge verkoopsovereenkomst.".Trim();
                draft.HeaderDescription = headerText;
                draft.DetailDescription = string.Join("\n", detailLines);
            }

            return draft;
        }

        private static InvoiceDraftBO? BuildChangeOrderInvoiceDraft(
            int? issuerCompanyId,
            int clientAccountId,
            IEnumerable<ChangeOrderBO> changeOrders,
 IEnumerable<ClientAccountChangeOrderInvoiceBO> selectedRows,
            IDictionary<int, decimal> alreadyInvoicedByDetail,
            ProjectBO project)
        {
            if (!issuerCompanyId.HasValue || issuerCompanyId.Value <= 0)
                return null;

            var selectedByDetail = selectedRows
               .Where(x => x.ChangeOrderDetailId > 0)
               .GroupBy(x => x.ChangeOrderDetailId)
               .ToDictionary(g => g.Key, g => g.First());
            var lines = new List<InvoiceLineBO>();


            foreach (var order in changeOrders)
            {
                var detailRows = new List<(int DetailId, string Description, decimal Amount, decimal Vat)>();

                foreach (var detail in order.Details.Where(d => selectedByDetail.ContainsKey(d.Id)))
                {
                    var selection = selectedByDetail[detail.Id];
                    var percentage = Math.Min(100m, Math.Max(0m, selection.Percentage));
                    var totalAmount = detail.Totaal;
                    alreadyInvoicedByDetail.TryGetValue(detail.Id, out var alreadyInvoiced);
                    var remaining = Math.Round(totalAmount - alreadyInvoiced, 2, MidpointRounding.AwayFromZero);
                    if (remaining <= 0m)
                        continue;

                    var maxPercentage = totalAmount == 0m
                        ? 0m
                        : Math.Round((remaining / totalAmount) * 100m, 4, MidpointRounding.AwayFromZero);

                    if (percentage > maxPercentage)
                        percentage = maxPercentage;

                    var selectedAmount = Math.Round(totalAmount * (percentage / 100m), 2, MidpointRounding.AwayFromZero);
                    if (selectedAmount <= 0m)
                        continue;

                    if (selectedAmount > remaining)
                        selectedAmount = remaining;

                    detailRows.Add((
                        DetailId: detail.Id,
                        Description: string.IsNullOrWhiteSpace(detail.Description) ? order.Description : detail.Description,
                        Amount: selectedAmount,
                        Vat: detail.VatPercentage ?? 21m));
                }

                var groupedByVat = detailRows
                    .GroupBy(r => r.Vat)
                    .Select(g => new
                    {
                        Vat = g.Key,
                        Total = g.Sum(x => x.Amount),
                        Details = g.ToList()
                    })
                    .ToList();

                foreach (var vatGroup in groupedByVat)
                {
                    var orderDescription = string.IsNullOrWhiteSpace(order.Description)
                        ? $"Wijzigingsopdracht #{order.Id}"
                        : order.Description;
                    var lineText = groupedByVat.Count == 1
                        ? orderDescription
                        : $"{orderDescription} ({vatGroup.Vat:0.##}% BTW)";

                    var detailIds = vatGroup.Details.Select(d => d.DetailId).Distinct().ToList();

                    lines.Add(new InvoiceLineBO
                    {
                        Text = lineText,
                        Price = vatGroup.Total,
                        VatPercentage = vatGroup.Vat,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        ChangeOrderDetailId = detailIds.Count == 1 ? detailIds[0] : null
                    });
                }
            }

            if (lines.Count == 0)
                return null;

            return new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId.Value,
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                Mode = InvoiceMode.ChangeOrders,
                ClientType = (int)InvoicePartyType.ClientAccount,
                ClientId = clientAccountId,
                Lines = lines,
                ProjectId = project.Id
            };
        }

        //SHARED
        public void ChangeOrderFillInSelectList(ProjectChangeOrderAddUpdateModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var projectService = _projectService;
            var clientService = _clientService;

            var cresponse = clientService.GetClientAccountsByProjectIdForSelect(model.ProjectId);
            model.ClientAccounts = cresponse.Success
                ? cresponse.Values.OrderBy(c => c.Display).ToList()
                : model.ClientAccounts;

            var aresponse = projectService.GetProjectContractActivitiesForSelect(model.ProjectId);
            model.ProjectContractActivities = aresponse.Success ? aresponse.Values : model.ProjectContractActivities;
        }
        private void FillInAddSelectLists(ProjectModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.IssuerCompanies = GetIssuerCompanies();

            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if (cresponse.Success && cresponse.Values is not null)
            {
                model.Countries = cresponse.Values;
                var defCountry = model.Countries.FirstOrDefault(m => m.Group == "19");
                if (defCountry != null)
                {
                    model.SelectedCountry = defCountry.ID;
                }
            }
        }

        private void FillInAddSelectListsDetailEdit(EditProjectDetail model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.IssuerCompanies = GetIssuerCompanies();

            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if (cresponse.Success && cresponse.Values is not null)
            {
                model.Countries = cresponse.Values;
                var defCountry = model.Countries.FirstOrDefault(m => m.Group == "19");
                if (defCountry != null && model.SelectedCountry == 0)
                {
                    model.SelectedCountry = defCountry.ID;
                }
            }

            var service = _projectService;
            var response = service.GetStatusesForSelect();
            if (response.Success && response.Values is not null)
            {
                model.Statuses = response.Values;
            }

        }

        private List<ProjectIssuerCompanyOptionVM> GetIssuerCompanies()
        {
            var uow = _uow;
            return uow.IssuerCompanies.GetNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new ProjectIssuerCompanyOptionVM { Id = i.Id, Name = i.Name })
                .ToList();
        }

        private void FillInAvailableUsers(ProjectModel model)
        {
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void FillInAvailableUsers(EditProjectDetail model)
        {
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void FillInAvailableUsersForCoord(Models.Projecten.CoordinatieInstellingenVM model)
        {
            var internalUserIds = _db.PermissionPerUser.Select(p => p.UserId).Distinct();
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && internalUserIds.Contains(u.Id))
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void SetCoordinatieBreadcrumb(int projectid, string projectName)
        {
            var idx = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projIdx = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = idx };
            var detail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = projIdx,
                RouteValues = new { projectid }
            };
            var coord = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailCoordinatie", "Projecten", "Coördinatie")
            {
                Parent = detail,
                RouteValues = new { projectid }
            };
            var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("CoordinatieInstellingen", "Projecten", "Instellingen")
            {
                Parent = coord,
                RouteValues = new { projectid }
            };
            ViewData["BreadcrumbNode"] = instellingen;
        }

        private void FillInAddSelectListsDetail(ref ShowProjectDetail model)
        {
            // get the activities
            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.Group == "19").FirstOrDefault();
            if ((defCountry != null))
                model.SelectedCountry = defCountry.ID;
            // Get statuses
            var service = _projectService;
            var response = service.GetStatusesForSelect();
            if ((response.Success))
                model.Statuses = response.Values;
            model.SelectedStatus = model.Project.Status.Id;
        }
        public void IncommingInvoiceFillInSelectList(ProjectIncommingInvoiceAddUpdateModel model)
        {
            var service2 = _projectService;
            var aresponse = service2.GetProjectContractsForSelect(model.ProjectId);
            if (aresponse.Success)
            {
                model.ProjectContracts = aresponse.Values;
            }
        }
        private List<IdNameBO> GetSiteManagersForCompany(int? companyId)
        {
            if (!companyId.HasValue || companyId.Value <= 0)
                return new List<IdNameBO>();

            return _db.CompanyContacts
                .Where(c => c.CompanyId == companyId.Value)
                .OrderBy(c => c.ContactNaam)
                .ThenBy(c => c.ContactVoornaam)
                .Select(c => new IdNameBO
                {
                    ID = c.ContactId,
                    Display = (c.ContactNaam ?? string.Empty) + (string.IsNullOrWhiteSpace(c.ContactVoornaam) ? string.Empty : $" {c.ContactVoornaam}")
                })
                .ToList();
        }

        public string GetCompanyName(int companyid)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyNameById(companyid);
            return presponse;
        }
        [HttpPost]
        public JsonResult GetCountryIsoCode(int countryid)
        {
            var pservice = _countryService;
            var presponse = pservice.GetCountryById(countryid);
            var country = presponse.Success ? presponse.Values.FirstOrDefault() : null;
            return Json(country?.ISOCode ?? string.Empty);
        }
        [HttpPost]
        public async Task<JsonResult> GetPostcodesByCountry(string term, int countryId)
        {
            var pservice = _postalcodeService;
            var presponse = await pservice.GetPostalcodeByCountryAndSearchstring(countryId, term ?? string.Empty);

            var list = new List<SelectBO>();
            if (presponse.Success && presponse.Values is not null)
            {
                foreach (var selectedPostalcode in presponse.Values)
                {
                    list.Add(new SelectBO
                    {
                        id = selectedPostalcode.PostcodeId ?? 0,
                        text = $"{selectedPostalcode.Postcode} - {selectedPostalcode.Gemeente}"
                    });
                }
            }

            return Json(list);
        }
        [HttpPost]
        public JsonResult GetCompanys(string term)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyForSearchList(term);
            var iList = new List<SelectBO>();

            if (presponse.Success)
            {
                iList = presponse.Values;
            }

            return Json(iList);
        }
        [HttpPost]
        public JsonResult GetCompanyContacts(int companyid)
        {
            var contacts = GetSiteManagersForCompany(companyid)
                .Select(x => new SelectBO { id = x.ID, text = x.Display })
                .ToList();
            return Json(contacts);
        }
        [HttpPost]
        public JsonResult GetWheaterstations(string term)
        {
            var pservice = _projectService;
            var presponse = pservice.GetWheaterstations(term ?? string.Empty);
            var list = new List<SelectBO>();

            if (presponse.Success && presponse.Values is not null)
            {
                foreach (var station in presponse.Values)
                {
                    list.Add(new SelectBO
                    {
                        id = station.Id,
                        text = station.Name,
                        extra = station.Visible?.ToString()
                    });
                }
            }

            return Json(list);
        }
        public string GetSlugForPostcodeId(int id, string name)
        {
            var city = string.Empty;
            if (id != 0)
            {
                var cityService = _postalcodeService;
                var postalcode = cityService.GetPostalcodeById(id);
                if (postalcode.Success && postalcode.Value is not null)
                {
                    city = postalcode.Value.Gemeente;
                }
            }

            var projectService = _projectService;
            return projectService.GenerateSlug($"{name} {city}".Trim());
        }
        public class Select2DTO
        {
            // Select2 expects objects with 'id' and 'text' fields
            public int id { get; set; }
            public string text { get; set; }
            public string group { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(ProjectPictureBO UploadedBO, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var isVideo = _validVideoTypes.Contains(file.ContentType);
            var isImage = _validImageTypes.Contains(file.ContentType);

            if (!isImage && !isVideo)
            {
                AddMessage("error", "Ongeldig bestandstype. Kies een afbeelding (jpg, png, webp) of video (mp4, webm).", "Fout!");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            string storedFilename;

            if (isVideo)
            {
                // Video: upload direct zonder verwerking
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = ".mp4";
                var videoFilename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-videos");
                Directory.CreateDirectory(tempRoot);
                var tempPath = Path.Combine(tempRoot, videoFilename);

                try
                {
                    using (var stream = System.IO.File.Create(tempPath))
                        await file.CopyToAsync(stream);

                    var uploaded = await UploadAssetFileToStorageAsync(tempPath, "videos", videoFilename, file.ContentType);
                    if (string.IsNullOrWhiteSpace(uploaded))
                    {
                        AddMessage("error", "Video upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                    }
                    storedFilename = uploaded;
                }
                finally
                {
                    TryDeleteTempFile(tempPath);
                }

                var videoPicture = new ProjectPictureBO
                {
                    Name            = storedFilename,
                    Caption         = UploadedBO.Caption,
                    ProjectId       = UploadedBO.ProjectId,
                    Type            = UploadedBO.Type,
                    SectionId       = UploadedBO.SectionId,
                    IsPublic        = UploadedBO.IsPublic,
                    MediaType       = 1, // Video
                    FileSizeBytes   = file.Length,
                    DateTimeUploaded = DateTime.Now
                };
                _projectService.InsertUpdatePicture(videoPicture);
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // Image: schalen, bijsnijden en opslaan als WebP
            var ts         = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var tempRootImg = Path.Combine(Path.GetTempPath(), "cpmcore-pictures");
            Directory.CreateDirectory(tempRootImg);

            var rawPath   = Path.Combine(tempRootImg, $"raw_{ts}{Path.GetExtension(file.FileName)}");
            var main447   = Path.Combine(tempRootImg, $"447_{ts}.webp");
            var main800   = Path.Combine(tempRootImg, $"800_{ts}.webp");
            var mainFull  = Path.Combine(tempRootImg, $"main_{ts}.webp");
            var webpName  = $"{ts}.webp";

            try
            {
                using (var stream = System.IO.File.Create(rawPath))
                    await file.CopyToAsync(stream);

                // Lees afmetingen voor de DB
                int imgWidth = 0, imgHeight = 0;
                using (var img = SixLabors.ImageSharp.Image.Load(rawPath))
                {
                    imgWidth  = img.Width;
                    imgHeight = img.Height;
                }

                System.IO.File.Copy(rawPath, main447, overwrite: true);
                System.IO.File.Copy(rawPath, main800, overwrite: true);
                System.IO.File.Copy(rawPath, mainFull, overwrite: true);

                ScaleAndCropImage(main447, 447, 447);
                ScaleAndCropImage(main800, 800, 800);
                ScaleImage(mainFull, 1280, 960);

                // Na ScaleAndCropImage worden paden omgezet naar .webp indien nodig
                main447  = Path.ChangeExtension(main447,  ".webp");
                main800  = Path.ChangeExtension(main800,  ".webp");
                mainFull = Path.ChangeExtension(mainFull, ".webp");

                var uploadMain = await UploadAssetFileToStorageAsync(mainFull, "pictures",     webpName, "image/webp");
                var upload447  = await UploadAssetFileToStorageAsync(main447,  "pictures/447", webpName, "image/webp");
                var upload800  = await UploadAssetFileToStorageAsync(main800,  "pictures/800", webpName, "image/webp");

                if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(upload447) || string.IsNullOrWhiteSpace(upload800))
                {
                    AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                    return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                }

                storedFilename = webpName;

                var picture = new ProjectPictureBO
                {
                    Name            = storedFilename,
                    Caption         = UploadedBO.Caption,
                    ProjectId       = UploadedBO.ProjectId,
                    Type            = UploadedBO.Type,
                    SectionId       = UploadedBO.SectionId,
                    IsPublic        = UploadedBO.IsPublic,
                    MediaType       = 0, // Photo
                    FileSizeBytes   = file.Length,
                    WidthPx         = imgWidth,
                    HeightPx        = imgHeight,
                    DateTimeUploaded = DateTime.Now
                };

                var service  = _projectService;
                var response = service.InsertUpdatePicture(picture);

                if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                            _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                    }
                }
            }
            finally
            {
                TryDeleteTempFile(rawPath);
                TryDeleteTempFile(main447);
                TryDeleteTempFile(main800);
                TryDeleteTempFile(mainFull);
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
        }

        // ── Media Sections API ────────────────────────────────────────────────

        [HttpGet]
        public IActionResult GetMediaSections(int projectId)
        {
            var ctx = _db;
            var sections = ctx.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .Select(s => new {
                    s.Id, s.Name, s.Description, s.SortOrder, s.IsPublic,
                    MediaCount = s.ProjectPictures.Count,
                    PhotoCount = s.ProjectPictures.Count(m => m.MediaType == 0),
                    VideoCount = s.ProjectPictures.Count(m => m.MediaType == 1)
                })
                .ToList();
            return Json(sections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMediaSection([FromBody] ProjectMediaSectionRequest req)
        {
            if (req == null || req.ProjectId <= 0 || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Ongeldige invoer." });

            var ctx = _db;
            var maxOrder = ctx.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == req.ProjectId)
                .Select(s => (int?)s.SortOrder).Max() ?? -1;

            var section = new DALCore.Models.ProjectMediaSection
            {
                ProjectId   = req.ProjectId,
                Name        = req.Name.Trim(),
                Description = req.Description?.Trim(),
                SortOrder   = maxOrder + 1,
                IsPublic    = req.IsPublic
            };
            ctx.Set<DALCore.Models.ProjectMediaSection>().Add(section);
            ctx.SaveChanges();
            return Json(new { section.Id, section.Name, section.Description, section.SortOrder, section.IsPublic, MediaCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaSection([FromBody] ProjectMediaSectionRequest req)
        {
            if (req == null || req.Id <= 0) return BadRequest();
            var ctx = _db;
            var section = ctx.Set<DALCore.Models.ProjectMediaSection>().Find(req.Id);
            if (section == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name))        section.Name        = req.Name.Trim();
            if (req.Description != null)                       section.Description = req.Description.Trim();
            section.IsPublic = req.IsPublic;
            ctx.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMediaSection(int id, int projectId)
        {
            var ctx = _db;
            var section = ctx.Set<DALCore.Models.ProjectMediaSection>().Find(id);
            if (section == null || section.ProjectId != projectId) return NotFound();

            // Media in deze sectie -> sectie leegmaken (SectionId = null)
            var media = ctx.Set<DALCore.Models.ProjectPictures>().Where(p => p.SectionId == id).ToList();
            foreach (var m in media) m.SectionId = null;

            ctx.Set<DALCore.Models.ProjectMediaSection>().Remove(section);
            ctx.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveMediaToSection([FromBody] MoveMediaRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            var media = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();
            foreach (var m in media) m.SectionId = req.SectionId;
            ctx.SaveChanges();
            return Ok(new { moved = media.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaVisibility([FromBody] VisibilityRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            var media = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();
            foreach (var m in media) m.IsPublic = req.IsPublic;
            ctx.SaveChanges();
            return Ok(new { updated = media.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteMedia([FromBody] BulkDeleteRequest req)
        {
            if (req == null || req.MediaIds == null || req.MediaIds.Count == 0) return BadRequest();
            var ctx   = _db;
            var items = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();

            var baseUrl     = (Configuration["StorageApi:BaseUrl"] ?? "").TrimEnd('/');
            var writeApiKey = Configuration["StorageApi:WriteApiKey"] ?? "";

            foreach (var item in items)
            {
                // Verwijder fysieke bestanden via Storage API (best-effort)
                if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(item.Name))
                {
                    var folder = item.MediaType == 1 ? "videos" : "pictures";
                    _ = DeleteStorageFileAsync(baseUrl, writeApiKey, folder, item.Name);
                    if (item.MediaType == 0)
                    {
                        _ = DeleteStorageFileAsync(baseUrl, writeApiKey, "pictures/447", item.Name);
                        _ = DeleteStorageFileAsync(baseUrl, writeApiKey, "pictures/800", item.Name);
                    }
                }
                ctx.Set<DALCore.Models.ProjectPictures>().Remove(item);
            }
            await ctx.SaveChangesAsync();
            return Ok(new { deleted = items.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaSortOrder([FromBody] SortOrderRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            foreach (var item in req.Items)
            {
                var m = ctx.Set<DALCore.Models.ProjectPictures>().Find(item.Id);
                if (m != null) { m.SortOrder = item.Order; m.SectionId = item.SectionId; }
            }
            ctx.SaveChanges();
            return Ok();
        }

        private async Task DeleteStorageFileAsync(string baseUrl, string apiKey, string folder, string fileName)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                await client.DeleteAsync($"{baseUrl}/api/assets/{folder}/{fileName}");
            }
            catch { /* best-effort */ }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetHoofdMedia([FromBody] SetHoofdMediaRequest req)
        {
            if (req.MediaId <= 0 || req.ProjectId <= 0) return BadRequest();

            var pic = _db.Set<DALCore.Models.ProjectPictures>()
                .FirstOrDefault(p => p.Id == req.MediaId && p.ProjectId == req.ProjectId);
            if (pic == null) return NotFound();

            // Reset alle huidige hoofdfoto's van dit project naar nevenfoto
            _db.Set<DALCore.Models.ProjectPictures>()
                .Where(p => p.ProjectId == req.ProjectId && p.Type == (int)BOCore.PictureType.Hoofdfoto)
                .ToList()
                .ForEach(p => p.Type = (int)BOCore.PictureType.Nevenfoto);

            pic.Type = (int)BOCore.PictureType.Hoofdfoto;

            var project = _db.Project.FirstOrDefault(p => p.ProjectId == req.ProjectId);
            if (project != null) project.DefaultPictureId = req.MediaId;

            _db.SaveChanges();
            return Ok(new { success = true });
        }

        public record ProjectMediaSectionRequest(int Id, int ProjectId, string Name, string? Description, bool IsPublic);
        public record MoveMediaRequest(List<int> MediaIds, int? SectionId);
        public record VisibilityRequest(List<int> MediaIds, bool IsPublic);
        public record BulkDeleteRequest(List<int> MediaIds);
        public record SetHoofdMediaRequest(int MediaId, int ProjectId);
        public record UpdateCaptionRequest(int MediaId, string? Caption);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaCaption([FromBody] UpdateCaptionRequest req)
        {
            if (req.MediaId <= 0) return BadRequest();
            var pic = _db.Set<DALCore.Models.ProjectPictures>().FirstOrDefault(p => p.Id == req.MediaId);
            if (pic == null) return NotFound();
            pic.Caption = req.Caption?.Trim();
            _db.SaveChanges();
            return Ok(new { success = true });
        }

        public record SortOrderItem(int Id, int Order, int? SectionId);
        public record SortOrderRequest(List<SortOrderItem> Items);

        private string? GetSignedAssetUrl(int docId, string folder)
        {
            var svc = _projectService;
            var resp = svc.GetProjectDoc(docId);
            if (!resp.Success || resp.Value == null) return null;

            var fileName = Path.GetFileName(resp.Value.Filename ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var readKey = Configuration["StorageApi:ReadApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey))
                return null;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", readKey);

            var signUrl = $"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(fileName)}/sign";
            var response = httpClient.PostAsync(signUrl, content: null).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return null;

            var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var jsonDoc = JsonDocument.Parse(payload);
            if (!jsonDoc.RootElement.TryGetProperty("url", out var urlElement)) return null;

            var relativeOrAbsolute = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;

            return relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? relativeOrAbsolute
                : $"{baseUrl}{relativeOrAbsolute}";
        }

        private string? GetSignedAssetUrlByFileName(string fileName, string folder)
        {
            var safeFileName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName)) return null;

            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var readKey = Configuration["StorageApi:ReadApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey))
                return null;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", readKey);

            var signUrl = $"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safeFileName)}/sign";
            var response = httpClient.PostAsync(signUrl, content: null).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return null;

            var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var jsonDoc = JsonDocument.Parse(payload);
            if (!jsonDoc.RootElement.TryGetProperty("url", out var urlElement)) return null;

            var relativeOrAbsolute = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;

            return relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? relativeOrAbsolute
                : $"{baseUrl}{relativeOrAbsolute}";
        }

        private static string BuildDocThumbFileName(string sourceFileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
            return $"thumb_{baseName}.jpg";
        }

        private async Task GenerateDocThumbnailViaStorageAsync(string sourceFileName)
        {
            var safeFileName = Path.GetFileName(sourceFileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName))
                return;

            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
                return;

            using var httpClient = CreateStorageHttpClient(writeKey, TimeSpan.FromMinutes(2));

            var endpointCandidates = BuildStorageEndpointCandidates(
                baseUrl,
                $"/api/assets/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail",
                $"/assets/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail",
                $"/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail");

            foreach (var endpoint in endpointCandidates)
            {
                var response = await httpClient.PostAsync(endpoint, content: null);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (IsLikelyAuthRedirectOrLoginPage(response, responseBody))
                {
                    _logger.LogError("Single doc thumbnail generation hit auth/login page. Endpoint: {Endpoint}. Status: {StatusCode}.", endpoint, (int)response.StatusCode);
                    return;
                }

                if (response.IsSuccessStatusCode && IsValidSingleThumbnailResponse(responseBody))
                {
                    return;
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Single doc thumbnail generation got a non-storage success response. Endpoint: {Endpoint}. Body: {Body}", endpoint, responseBody);
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return;
                }
            }
        }

        private async Task<string?> UploadAssetFileToStorageAsync(string localPath, string folder, string originalFileName, string contentType)
        {
            await using var stream = System.IO.File.OpenRead(localPath);
            return await UploadAssetToStorageAsync(stream, originalFileName, contentType, folder);
        }

        private async Task<string?> UploadAssetToStorageAsync(IFormFile file, string folder)
        {
            await using var fileStream = file.OpenReadStream();
            return await UploadAssetToStorageAsync(fileStream, file.FileName, file.ContentType, folder);
        }

        private async Task<string?> UploadAssetToStorageAsync(Stream fileStream, string originalFileName, string? contentType, string folder)
        {
            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
                return null;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", writeKey);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(folder), "folder");

            var fileContent = new StreamContent(fileStream);
            if (!string.IsNullOrWhiteSpace(contentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", originalFileName);

            var response = await httpClient.PostAsync($"{baseUrl}/api/assets/upload", content);
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(payload);
            if (!jsonDoc.RootElement.TryGetProperty("fileName", out var fileNameElement)) return null;
            return fileNameElement.GetString();
        }

        private static HttpClient CreateStorageHttpClient(string apiKey, TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = timeout
            };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            return httpClient;
        }

        private static bool IsLikelyAuthRedirectOrLoginPage(HttpResponseMessage response, string responseBody)
        {
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                return true;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return true;
            }

            return responseBody.Contains("Sign in to your account", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("login", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("<html", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidBulkThumbnailResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                return jsonDoc.RootElement.TryGetProperty("docsProcessed", out _)
                    && jsonDoc.RootElement.TryGetProperty("thumbnailsGenerated", out _)
                    && jsonDoc.RootElement.TryGetProperty("thumbnails", out _);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidSingleThumbnailResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                return jsonDoc.RootElement.TryGetProperty("thumbFileName", out _);
            }
            catch
            {
                return false;
            }
        }

        private static List<string> BuildStorageEndpointCandidates(string baseUrl, params string[] suffixes)
        {
            var candidates = new List<string>();
            var normalizedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');

            foreach (var suffix in suffixes)
            {
                var normalizedSuffix = suffix.StartsWith("/", StringComparison.Ordinal) ? suffix : $"/{suffix}";
                candidates.Add($"{normalizedBaseUrl}{normalizedSuffix}");
            }

            if (Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri))
            {
                var origin = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                var basePath = baseUri.AbsolutePath.Trim('/');

                foreach (var suffix in suffixes)
                {
                    var normalizedSuffix = suffix.StartsWith("/", StringComparison.Ordinal) ? suffix : $"/{suffix}";
                    candidates.Add($"{origin}{normalizedSuffix}");

                    if (!string.IsNullOrWhiteSpace(basePath))
                    {
                        candidates.Add($"{origin}/{basePath}{normalizedSuffix}");
                    }
                }
            }

            return candidates
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void TryDeleteTempFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImageFtp(ProjectPictureBO UploadedBO, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif"
    };

            if (!validTypes.Contains(photo.ContentType))
            {
                ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // 1) Lokaal temp-pad
            var tempRoot = (Configuration["URL:LocalTempPath"] ?? Path.Combine(Path.GetTempPath(), "project-pictures")).Trim();
            Directory.CreateDirectory(tempRoot);

            // 2) FTP root-pad
            var baseFtp = (Configuration["URL:PicturesFtpPath"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseFtp))
            {
                ModelState.AddModelError("", "FTP-pad ontbreekt (URL:PicturesFtpPath).");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            await using var ftp = await ConnectAsync();
            if (ftp == null)
            {
                ModelState.AddModelError("", "FTP-verbinding mislukt.");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".jpg";

            // 3) Lokaal opslaan
            var localOriginal = Path.Combine(tempRoot, filename);
            var local447 = Path.Combine(tempRoot, "447_" + filename);
            var local800 = Path.Combine(tempRoot, "800_" + filename);

            try
            {
                using (var fs = new FileStream(localOriginal, FileMode.Create))
                {
                    await photo.CopyToAsync(fs);
                }

                System.IO.File.Copy(localOriginal, local447, true);
                System.IO.File.Copy(localOriginal, local800, true);

                // 4) Bewerken (met ImageSharp helpers)
                ScaleAndCropImage(local447, 447, 447);
                ScaleAndCropImage(local800, 800, 800);
                ScaleImage(localOriginal, 1280, 960);

                // 5) Zorg dat remote directories bestaan
                var remoteRoot = $"{baseFtp}";
                var remote447 = $"{baseFtp}/447";
                var remote800 = $"{baseFtp}/800";

                await EnsureDirectoryAsync(ftp, remoteRoot);
                await EnsureDirectoryAsync(ftp, remote447);
                await EnsureDirectoryAsync(ftp, remote800);

                // 6) Upload bestanden
                var ok1 = await UploadAsync(ftp, localOriginal, $"{remoteRoot}/{filename}");
                var ok2 = await UploadAsync(ftp, local447, $"{remote447}/{filename}");
                var ok3 = await UploadAsync(ftp, local800, $"{remote800}/{filename}");

                if (!(ok1 && ok2 && ok3))
                {
                    await DeleteFileAsync(ftp, $"{remoteRoot}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote447}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote800}/{filename}");

                    ModelState.AddModelError("ImageUpload", "Upload naar server is mislukt.");
                    return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                }

                // 7) DB opslaan
                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = UploadedBO.Caption,
                    ProjectId = UploadedBO.ProjectId,
                    Type = UploadedBO.Type,
                    DateTimeUploaded = DateTime.Now
                };

                var service = _projectService;
                var response = service.InsertUpdatePicture(picture);

                if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                        {
                            _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                        }
                    }
                }
            }
            finally
            {
                TryDelete(localOriginal);
                TryDelete(local447);
                TryDelete(local800);
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            // --- helpers ---
            void TryDelete(string p)
            {
                try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { /* ignore/log */ }
            }
        }

        public void ScaleAndCropImage(string imagePath, int maxWidth, int maxHeight, int quality = 80)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio  = Math.Max(ratioX, ratioY);

            int newWidth  = (int)(image.Width  * ratio);
            int newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(newWidth, newHeight));

            var cropX = (newWidth  - maxWidth)  / 2;
            var cropY = (newHeight - maxHeight) / 2;
            image.Mutate(x => x.Crop(new Rectangle(cropX, cropY, maxWidth, maxHeight)));

            var webpPath = Path.ChangeExtension(imagePath, ".webp");
            image.Save(webpPath, new WebpEncoder { Quality = quality });
            if (!string.Equals(imagePath, webpPath, StringComparison.OrdinalIgnoreCase))
                TryDeleteTempFile(imagePath);
        }

        public void ScaleImage(string imagePath, int maxWidth, int maxHeight, int quality = 80)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            double ratioX = (double)maxWidth  / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio  = Math.Min(ratioX, ratioY); // Min = fit inside, geen bijsnijden

            int newWidth  = (int)(image.Width  * ratio);
            int newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(newWidth, newHeight));

            var webpPath = Path.ChangeExtension(imagePath, ".webp");
            image.Save(webpPath, new WebpEncoder { Quality = quality });
            if (!string.Equals(imagePath, webpPath, StringComparison.OrdinalIgnoreCase))
                TryDeleteTempFile(imagePath);
        }
        private static bool IsValidImage(IFormFile file) => _validImageTypes.Contains(file.ContentType);


        private static void EnsureDir(string dir)
        {
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        // ==== FTP CONNECT ====
        public async Task<AsyncFtpClient?> ConnectToFtpAsync()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            // Config
            client.Config.DataConnectionType = FtpDataConnectionType.PASV; // passive
            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;  // AUTH TLS
                client.Config.DataConnectionEncryption = true;

                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            try
            {
                await client.Connect();  // AsyncFtpClient: async connect heet 'Connect()'
                return client;
            }
            catch (Exception ex)
            {
                // jouw logging hier
                Console.WriteLine("FTP connect failed: " + ex.Message);
                await client.DisposeAsync();
                return null;
            }
        }

        // ==== LOCAL DIR CHECK ====
        public void CheckDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        // ==== REMOTE DIR CHECK/CREATE ====
        public async Task CheckDirFtpAsync(string remoteDir, AsyncFtpClient ftp)
        {
            // Maakt recursief aan; geen chdir nodig bij FluentFTP
            await ftp.CreateDirectory(remoteDir, true);
        }

        // ==== LOKALE FOTO'S VERWIJDEREN (ongewijzigd, puur filesystem) ====
        public void DeletePictureFile(int id)
        {
            // Bestanden worden beheerd door de Storage API; lokale cleanup is niet meer van toepassing.
        }

        // ==== REMOTE FILE DELETE ====
        public async Task<bool> DeleteFtpFileAsync(string remoteDir, string filename, AsyncFtpClient ftp)
        {
            try
            {
                var remotePath = $"{remoteDir.TrimEnd('/')}/{filename}";
                if (await ftp.FileExists(remotePath))
                {
                    await ftp.DeleteFile(remotePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP delete failed: {ex.Message}");
                return false;
            }
        }


        //FTP HELPERS
        private AsyncFtpClient CreateClient()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;   // FTPS (AUTH TLS)
                client.Config.DataConnectionEncryption = true;
                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            client.Config.DataConnectionType = FtpDataConnectionType.PASV;  // passive

            return client;
        }

        public async Task<AsyncFtpClient> ConnectAsync()
        {
            var client = CreateClient();
            await client.Connect();   // AsyncFtpClient: async method heet Connect()
            return client;
        }

        public async Task EnsureDirectoryAsync(AsyncFtpClient client, string remoteDir)
            => await client.CreateDirectory(remoteDir, true);   // async

        public async Task<bool> UploadAsync(AsyncFtpClient client, string localPath, string remoteFullPath)
        {
            var status = await client.UploadFile(localPath, remoteFullPath,
                FtpRemoteExists.Overwrite, true);
            return status == FtpStatus.Success;
        }

        public async Task<bool> DeleteFileAsync(AsyncFtpClient client, string remoteFullPath)
        {
            try
            {
                if (await client.FileExists(remoteFullPath))
                    await client.DeleteFile(remoteFullPath);
                return true;
            }
            catch { return false; }
        }

        //SALES HELPERS
        private static decimal Percent(decimal amount, decimal pct)
=> Math.Round(amount * (pct / 100m), 2, MidpointRounding.AwayFromZero);

        private static decimal SafePos(decimal? v) => v.HasValue && v.Value > 0 ? v.Value : 0m;

        private static decimal SafeLand(UnitBO u) => u.LandValue ?? 0m;

        private static decimal SafeConstruction(UnitBO u) =>
            (u.ConstructionValues?.Sum(x => x.Value ?? 0m)) ?? 0m;

        private static decimal CalculateNotaryFeesFromTotals(decimal totalNetLand, decimal totalNetBuild, bool mixedVatRegistration)
        {
            decimal baseValue = mixedVatRegistration
                ? (totalNetLand + totalNetBuild)
                : (totalNetLand + (totalNetBuild / 2m));

            if (baseValue <= 0m) return 0m;

            var parts = new (decimal amount, decimal pct)[]
            {
        (  7500m, 4.56m),
        ( 10000m, 2.85m),
        ( 12500m, 2.28m),
        ( 15495m, 1.71m),
        ( 18600m, 1.14m),
        (186000m, 0.57m),
            };
            const decimal pctRest = 0.057m;

            decimal remaining = baseValue;
            decimal fee = 0m;

            foreach (var (amount, pct) in parts)
            {
                if (remaining <= 0m) break;
                var take = Math.Min(remaining, amount);
                fee += take * (pct / 100m);
                remaining -= take;
            }

            if (remaining > 0m) fee += remaining * (pctRest / 100m);

            return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        }
        private static bool ShouldDefaultInclude(UnitBO u)
        {
            // Zorg dat UnitBO.Type.GroupId gevuld is in je translator
            var gid = u?.Type?.GroupId ?? 0;
            return gid == 1 || gid == 4; // woning/appartement of commerciële ruimte
        }

        // ── Budget Wizard ────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult BudgetIndex(int projectId)
        {
            var projectResponse = _projectService.GetProjectByID(projectId);
            if (!projectResponse.Success)
                return NotFound();

            var mastersResponse = _budgetService.GetBudgetMasters(projectId);

            var model = new BudgetIndexModel
            {
                ProjectId    = projectId,
                ProjectName  = projectResponse.Value?.Name,
                BudgetMasters = mastersResponse.Success ? mastersResponse.Values : new List<BudgetMasterBO>()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",      nameof(HomeController.Index),        "Home",       true),
                new Breadcrumb("Projecten", nameof(ProjectenController.Index),   "Projecten",  true),
                new Breadcrumb("Detail",    nameof(ProjectenController.Detail),  "Projecten",  true),
                new Breadcrumb("Budgetten", nameof(BudgetIndex),                 "Projecten",  false),
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult BudgetMasterAanmaken(int projectId)
        {
            var projectResponse = _projectService.GetProjectByID(projectId);
            if (!projectResponse.Success)
                return NotFound();

            var model = new BudgetMasterAanmakenModel
            {
                ProjectId   = projectId,
                ProjectName = projectResponse.Value?.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetMasterAanmaken(BudgetMasterAanmakenModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _db.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Id ?? 0;

            var bo = new BudgetMasterBO
            {
                ProjectId   = model.ProjectId,
                Naam        = model.Naam,
                Omschrijving = model.Omschrijving
            };

            var response = _budgetService.CreateBudgetMaster(bo, userId);
            if (!response.Success)
            {
                foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                    ModelState.AddModelError(string.Empty, msg.Message);
                return View(model);
            }

            return RedirectToAction(nameof(BudgetGegevens), new { versieId = response.InsertedId });
        }

        [HttpGet]
        public async Task<IActionResult> BudgetGegevens(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null)
                return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success)
                return NotFound();

            var gegevensResponse = _budgetService.GetBudgetGegevens(versieId);
            var gegevens = gegevensResponse.Success ? gegevensResponse.Value : new BudgetGegevensBO();

            if (gegevens.SIndexHuidig == null || gegevens.SIndexHuidig == 0)
                gegevens.SIndexHuidig = await _bouwIndex.GetActieveIndexAsync("S");
            if (gegevens.IIndexHuidig == null || gegevens.IIndexHuidig == 0)
                gegevens.IIndexHuidig = await _bouwIndex.GetActieveIndexAsync("I");

            var bouwheerOptions = new List<SelectListItem>
            {
                new SelectListItem("— geen —", "")
            };
            var companyList = _uow.CompanyInfo.GetNoTracking()
                .OrderBy(c => c.BedrijfsNaam)
                .Select(c => new { c.CompanyId, c.BedrijfsNaam })
                .ToList();
            bouwheerOptions.AddRange(companyList.Select(c => new SelectListItem(c.BedrijfsNaam, c.CompanyId.ToString())));

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId    = versieEntity.ProjectId,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam,
                Status       = versieEntity.Status,
                IsHuidig     = versieEntity.IsHuidig,
                CreatedAt    = versieEntity.CreatedAt
            };

            var formulaCtx = await _formulaService.BuildContextAsync(versieId, gegevens);
            var formulaVoorstellingen = _formulaService.BerekenAlle(formulaCtx);

            var model = new BudgetGegevensModel
            {
                VersieId      = versieId,
                MasterId      = versieEntity.BudgetMasterId,
                ProjectId     = versieEntity.ProjectId,
                ProjectName   = projectResponse.Value?.Name,
                VersieLabel   = versieBO.VersieLabel,
                VersieStatus  = versieEntity.Status,
                MasterNaam    = versieEntity.BudgetMaster?.Naam,
                VersieCreatedAt = versieEntity.CreatedAt,
                Gegevens      = gegevens,
                BouwheerOptions = bouwheerOptions,
                FormulaVoorstellingen = formulaVoorstellingen
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",      nameof(HomeController.Index),        "Home",       true),
                new Breadcrumb("Projecten", nameof(ProjectenController.Index),   "Projecten",  true),
                new Breadcrumb("Detail",    nameof(ProjectenController.Detail),  "Projecten",  true),
                new Breadcrumb("Budgetten", nameof(BudgetIndex),                 "Projecten",  true),
                new Breadcrumb("Gegevens",  nameof(BudgetGegevens),              "Projecten",  false),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetGegevens(BudgetGegevensModel model, string submitAction)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = _budgetService.SaveBudgetGegevens(model.Gegevens, model.VersieId);
            if (!response.Success)
            {
                foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                    ModelState.AddModelError(string.Empty, msg.Message);
                return View(model);
            }

            if (submitAction == "next")
                return RedirectToAction(nameof(BudgetOppervlaktes), new { versieId = model.VersieId });

            return RedirectToAction(nameof(BudgetGegevens), new { versieId = model.VersieId });
        }

        // ── BudgetOppervlaktes ────────────────────────────────────────────────

        [HttpGet]
        public IActionResult BudgetOppervlaktes(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success) return NotFound();

            var rijResp     = _budgetService.GetBudgetOppervlaktes(versieId);
            var totaalResp  = _budgetService.GetBudgetOppervlaktesTotaal(versieId);

            var groupOptions = new List<SelectListItem> { new SelectListItem("— kies type —", "") };
            var dbGroupTypes = _uow.UnitGroupTypes.GetNoTracking()
                .Where(g => g.Selectable)
                .OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.Name })
                .ToList();
            groupOptions.AddRange(dbGroupTypes.Select(g => new SelectListItem(g.Name, g.Id.ToString())));

            var allTypesBos = _uow.UnitTypes.GetNoTracking()
                .Where(t => t.Selectable != false)
                .Select(t => new UnitTypeBO { Id = t.Id, Name = t.Name, Shortcode = t.Shortcode, GroupId = t.GroupId })
                .ToList();

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId    = versieEntity.ProjectId,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam,
                Status       = versieEntity.Status,
                IsHuidig     = versieEntity.IsHuidig,
                CreatedAt    = versieEntity.CreatedAt
            };

            var model = new BudgetOppervlaktesModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectResponse.Value?.Name,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Rijen       = rijResp.Success ? rijResp.Values : new List<BudgetOppervlaktesBO>(),
                Totalen     = totaalResp.Success ? totaalResp.Value : new BudgetOppervlaktesTotaalBO(),
                GroupTypes  = groupOptions,
                AllTypes    = allTypesBos
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",          nameof(HomeController.Index),       "Home",       true),
                new Breadcrumb("Projecten",     nameof(ProjectenController.Index),  "Projecten",  true),
                new Breadcrumb("Detail",        nameof(ProjectenController.Detail), "Projecten",  true),
                new Breadcrumb("Budgetten",     nameof(BudgetIndex),                "Projecten",  true),
                new Breadcrumb("Oppervlaktes",  nameof(BudgetOppervlaktes),         "Projecten",  false),
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijToevoegen(int versieId, string eenheidNaam, int? groupTypeId, int? typeId)
        {
            var rij = new BudgetOppervlaktesBO
            {
                BudgetVersieId  = versieId,
                EenheidNaam     = eenheidNaam ?? "",
                UnitGroupTypeId = groupTypeId,
                UnitTypeId      = typeId
            };

            if (groupTypeId.HasValue)
            {
                var grp = _uow.UnitGroupTypes.GetNoTracking().SingleOrDefault(g => g.Id == groupTypeId.Value);
                rij.GroupTypeName = grp?.Name;
            }
            if (typeId.HasValue)
            {
                var tp = _uow.UnitTypes.GetNoTracking().SingleOrDefault(t => t.Id == typeId.Value);
                rij.TypeName      = tp?.Name;
                rij.TypeShortcode = tp?.Shortcode;
            }

            var response = _budgetService.AddBudgetOppervlaktesRij(rij, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            rij.Id = response.InsertedId;
            return PartialView("Partials/_BudgetOppervlaktesRij", rij);
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijOpslaan([FromBody] BudgetOppervlaktesRijModel model)
        {
            var bo = new BudgetOppervlaktesBO
            {
                Id                      = model.RijId,
                BudgetVersieId          = model.VersieId,
                EenheidNaam             = model.EenheidNaam ?? "",
                UnitGroupTypeId         = model.UnitGroupTypeId,
                UnitTypeId              = model.UnitTypeId,
                BewoonbareOpp           = model.BewoonbareOpp,
                Tuin                    = model.Tuin,
                TerrasPrefab            = model.TerrasPrefab,
                TerrasGelijkvloers      = model.TerrasGelijkvloers,
                Dakterras               = model.Dakterras,
                GaragesParkingsBovenGr  = model.GaragesParkingsBovenGr,
                GarBergOndergronds      = model.GarBergOndergronds,
                BergGelijkvloers        = model.BergGelijkvloers,
                Carports                = model.Carports,
                DoorritGVL              = model.DoorritGVL,
                Zolder                  = model.Zolder,
                GemeenschappelijkeDelen = model.GemeenschappelijkeDelen,
                Wegenis                 = model.Wegenis,
                Grondopp                = model.Grondopp
            };

            var response = _budgetService.UpdateBudgetOppervlaktesRij(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totalen = _budgetService.GetBudgetOppervlaktesTotaal(model.VersieId);
            var t = totalen.Value ?? new BudgetOppervlaktesTotaalBO();

            return Json(new
            {
                success           = true,
                oppGereduceerd    = bo.OppGereduceerd,
                formula           = bo.FormulaOppGereduceerd,
                totalen = new
                {
                    aantalWooneenheden    = t.AantalWooneenheden,
                    aantalParkeerplaatsen = t.AantalParkeerplaatsen,
                    aantalCommercieel     = t.AantalCommercieel,
                    aantalTotaal          = t.AantalTotaal,
                    totaalBewoonbaar      = t.TotaalBewoonbaar,
                    totaalGereduceerd     = t.TotaalGereduceerd,
                    gemiddeldeM2PerEenheid = t.GemiddeldeM2PerEenheid,
                    totaalGrondopp        = t.TotaalGrondopp
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijVerwijderen(int rijId, int versieId)
        {
            var response = _budgetService.DeleteBudgetOppervlaktesRij(rijId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totalen = _budgetService.GetBudgetOppervlaktesTotaal(versieId);
            var t = totalen.Value ?? new BudgetOppervlaktesTotaalBO();

            return Json(new
            {
                success = true,
                totalen = new
                {
                    aantalWooneenheden    = t.AantalWooneenheden,
                    aantalParkeerplaatsen = t.AantalParkeerplaatsen,
                    aantalCommercieel     = t.AantalCommercieel,
                    aantalTotaal          = t.AantalTotaal,
                    totaalBewoonbaar      = t.TotaalBewoonbaar,
                    totaalGereduceerd     = t.TotaalGereduceerd,
                    gemiddeldeM2PerEenheid = t.GemiddeldeM2PerEenheid,
                    totaalGrondopp        = t.TotaalGrondopp
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesVolgorde(int versieId, [FromBody] int[] orderedIds)
        {
            var response = _budgetService.ReorderBudgetOppervlaktes(orderedIds?.ToList() ?? new List<int>(), versieId);
            return Json(new { success = response.Success });
        }

        [HttpPost]
        public IActionResult BudgetNieuweVersie(int masterId, string versieNaam, string notitie)
        {
            var userId = _db.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Id ?? 0;
            var response = _budgetService.CreateNieuweVersie(masterId, versieNaam, notitie, userId);

            var versienummer = 0;
            if (response.Success)
            {
                var versie = _uow.BudgetVersies.GetNoTracking()
                    .SingleOrDefault(v => v.Id == response.InsertedId);
                versienummer = versie?.Versienummer ?? 0;
            }

            return Json(new { success = response.Success, newVersieId = response.InsertedId, versienummer });
        }

        [HttpPost]
        public IActionResult BudgetVersieActiveren(int versieId)
        {
            var response = _budgetService.ActiveerVersie(versieId);
            return Json(new { success = response.Success });
        }

        // ── BudgetSanitair ────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult BudgetSanitair(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success) return NotFound();

            _budgetService.SyncSanitairVanOppervlaktes(versieId);

            var rijResp    = _budgetService.GetBudgetSanitair(versieId);
            var totaalResp = _budgetService.GetBudgetSanitairTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id             = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId      = versieEntity.ProjectId,
                Versienummer   = versieEntity.Versienummer,
                VersieNaam     = versieEntity.VersieNaam,
                Status         = versieEntity.Status,
                IsHuidig       = versieEntity.IsHuidig,
                CreatedAt      = versieEntity.CreatedAt
            };

            var model = new BudgetSanitairModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectResponse.Value?.Name,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Rijen       = rijResp.Success ? rijResp.Values : new List<BudgetSanitairBO>(),
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetSanitairTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",       nameof(HomeController.Index),      "Home",       true),
                new Breadcrumb("Projecten",  nameof(ProjectenController.Index), "Projecten",  true),
                new Breadcrumb("Detail",     nameof(ProjectenController.Detail),"Projecten",  true),
                new Breadcrumb("Budgetten",  nameof(BudgetIndex),               "Projecten",  true),
                new Breadcrumb("Sanitair",   nameof(BudgetSanitair),            "Projecten",  false),
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijToevoegen(int versieId, string eenheidNaam, int? unitTypeId)
        {
            var rij = new BudgetSanitairBO
            {
                BudgetVersieId = versieId,
                EenheidNaam    = eenheidNaam ?? "",
                UnitTypeId     = unitTypeId
            };

            var response = _budgetService.AddBudgetSanitairRij(rij, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            rij.Id = response.InsertedId;
            return PartialView("Partials/_BudgetSanitairRij", rij);
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijOpslaan([FromBody] BudgetSanitairRijModel model)
        {
            var bo = new BudgetSanitairBO
            {
                Id                  = model.RijId,
                BudgetVersieId      = model.VersieId,
                EenheidNaam         = model.EenheidNaam,
                UnitTypeId          = model.UnitTypeId,
                Badkamer            = model.Badkamer,
                ToiletInBadkamer    = model.ToiletInBadkamer,
                AfzonderlijkToilet  = model.AfzonderlijkToilet,
                DoucheInBadkamer    = model.DoucheInBadkamer,
                Douchekamer         = model.Douchekamer
            };

            var response = _budgetService.UpdateBudgetSanitairRij(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaalResp = _budgetService.GetBudgetSanitairTotaal(model.VersieId);
            var t = totaalResp.Value ?? new BudgetSanitairTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalBadkamers             = t.TotaalBadkamers,
                    totaalToilettenInBadkamer   = t.TotaalToilettenInBadkamer,
                    totaalAfzonderlijkeToiletten = t.TotaalAfzonderlijkeToiletten,
                    totaalDouchesInBadkamer     = t.TotaalDouchesInBadkamer,
                    totaalDouchekamers          = t.TotaalDouchekamers,
                    aantalEenheden              = t.AantalEenheden
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijVerwijderen(int rijId, int versieId)
        {
            var response = _budgetService.DeleteBudgetSanitairRij(rijId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaalResp = _budgetService.GetBudgetSanitairTotaal(versieId);
            var t = totaalResp.Value ?? new BudgetSanitairTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalBadkamers             = t.TotaalBadkamers,
                    totaalToilettenInBadkamer   = t.TotaalToilettenInBadkamer,
                    totaalAfzonderlijkeToiletten = t.TotaalAfzonderlijkeToiletten,
                    totaalDouchesInBadkamer     = t.TotaalDouchesInBadkamer,
                    totaalDouchekamers          = t.TotaalDouchekamers,
                    aantalEenheden              = t.AantalEenheden
                }
            });
        }

        // ── BudgetGevels ─────────────────────────────────────────────────────

        private static readonly string[] GevelTypes = { "GevelNieuwbouw", "GevelBestaand", "RaamNieuwbouw", "RaamBestaand", "Ballustrade", "Zichtscherm" };

        [HttpGet]
        public IActionResult BudgetGevels(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success) return NotFound();

            var elementenResp = _budgetService.GetBudgetGevelElementen(versieId);
            var totaalResp    = _budgetService.GetBudgetGevelTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam
            };

            var elementen = (elementenResp.Success ? elementenResp.Values : new List<BudgetGevelElementBO>())
                .Where(e => GevelTypes.Contains(e.ElementType))
                .GroupBy(e => e.ElementType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new BudgetGevelsDakModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectResponse.Value?.Name,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Elementen   = elementen,
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetGevelTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",       nameof(HomeController.Index),      "Home",       true),
                new Breadcrumb("Projecten",  nameof(ProjectenController.Index), "Projecten",  true),
                new Breadcrumb("Detail",     nameof(ProjectenController.Detail),"Projecten",  true),
                new Breadcrumb("Budgetten",  nameof(BudgetIndex),               "Projecten",  true),
                new Breadcrumb("Gevels",     nameof(BudgetGevels),              "Projecten",  false),
            };

            return View(model);
        }

        // ── BudgetDakAfbraak ──────────────────────────────────────────────────

        private static readonly string[] DakAfbraakTypes = { "PlatDak", "HellendDak", "GroenDak", "Dakoversteken", "OnderkantDoorrit", "Afbraak" };

        [HttpGet]
        public IActionResult BudgetDakAfbraak(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success) return NotFound();

            var elementenResp = _budgetService.GetBudgetGevelElementen(versieId);
            var totaalResp    = _budgetService.GetBudgetGevelTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam
            };

            var elementen = (elementenResp.Success ? elementenResp.Values : new List<BudgetGevelElementBO>())
                .Where(e => DakAfbraakTypes.Contains(e.ElementType))
                .GroupBy(e => e.ElementType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new BudgetGevelsDakModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectResponse.Value?.Name,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Elementen   = elementen,
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetGevelTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",         nameof(HomeController.Index),        "Home",       true),
                new Breadcrumb("Projecten",    nameof(ProjectenController.Index),   "Projecten",  true),
                new Breadcrumb("Detail",       nameof(ProjectenController.Detail),  "Projecten",  true),
                new Breadcrumb("Budgetten",    nameof(BudgetIndex),                 "Projecten",  true),
                new Breadcrumb("Dak & Afbraak",nameof(BudgetDakAfbraak),            "Projecten",  false),
            };

            return View(model);
        }

        // ── BudgetGevelsDak ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult BudgetGevelsDak(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectResponse = _projectService.GetProjectByID(versieEntity.ProjectId);
            if (!projectResponse.Success) return NotFound();

            var elementenResp = _budgetService.GetBudgetGevelElementen(versieId);
            var totaalResp    = _budgetService.GetBudgetGevelTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam
            };

            var elementen = (elementenResp.Success ? elementenResp.Values : new List<BudgetGevelElementBO>())
                .GroupBy(e => e.ElementType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new BudgetGevelsDakModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectResponse.Value?.Name,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Elementen   = elementen,
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetGevelTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",          nameof(HomeController.Index),       "Home",       true),
                new Breadcrumb("Projecten",     nameof(ProjectenController.Index),  "Projecten",  true),
                new Breadcrumb("Detail",        nameof(ProjectenController.Detail), "Projecten",  true),
                new Breadcrumb("Budgetten",     nameof(BudgetIndex),                "Projecten",  true),
                new Breadcrumb("Gevels & Dak",  nameof(BudgetGevelsDak),            "Projecten",  false),
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetGevelElementToevoegen(int versieId, string elementType,
            string eenheidNaam, string beschrijving)
        {
            var bo = new BudgetGevelElementBO
            {
                BudgetVersieId = versieId,
                ElementType    = elementType ?? "GevelNieuwbouw",
                EenheidNaam    = eenheidNaam,
                Beschrijving   = beschrijving,
                Aantal         = 1m
            };

            var response = _budgetService.AddBudgetGevelElement(bo, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            bo.Id = response.InsertedId;
            ViewBag.VersieId = versieId;
            return PartialView("Partials/_BudgetGevelRij", bo);
        }

        [HttpPost]
        public IActionResult BudgetGevelElementOpslaan([FromBody] BudgetGevelElementModel model)
        {
            var bo = new BudgetGevelElementBO
            {
                Id           = model.ElementId,
                BudgetVersieId = model.VersieId,
                ElementType  = model.ElementType,
                EenheidNaam  = model.EenheidNaam,
                Beschrijving = model.Beschrijving,
                Aantal       = model.Aantal,
                Breedte      = model.Breedte,
                Hoogte       = model.Hoogte,
                Lengte       = model.Lengte
            };

            var response = _budgetService.UpdateBudgetGevelElement(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaal = _budgetService.GetBudgetGevelTotaal(model.VersieId);
            var t = totaal.Value ?? new BudgetGevelTotaalBO();

            return Json(new
            {
                success      = true,
                resultaatM2  = bo.ResultaatM2,
                resultaatLm  = bo.ResultaatLm,
                formula      = bo.FormulaResultaat,
                totaal = new
                {
                    totaalGevelNieuwbouw   = t.TotaalGevelNieuwbouw,
                    totaalGevelBestaand    = t.TotaalGevelBestaand,
                    totaalRaamNieuwbouw    = t.TotaalRaamNieuwbouw,
                    totaalRaamBestaand     = t.TotaalRaamBestaand,
                    totaalBallustrade      = t.TotaalBallustrade,
                    totaalZichtscherm      = t.TotaalZichtscherm,
                    totaalPlatDak          = t.TotaalPlatDak,
                    totaalHellendDak       = t.TotaalHellendDak,
                    totaalGroenDak         = t.TotaalGroenDak,
                    totaalDakoversteken    = t.TotaalDakoversteken,
                    totaalOnderkantDoorrit = t.TotaalOnderkantDoorrit,
                    totaalAfbraak          = t.TotaalAfbraak,
                    totaalGevelCombineerd  = t.TotaalGevelCombineerd,
                    totaalRaamCombineerd   = t.TotaalRaamCombineerd,
                    totaalDakCombineerd    = t.TotaalDakCombineerd
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetGevelElementVerwijderen(int elementId, int versieId)
        {
            var response = _budgetService.DeleteBudgetGevelElement(elementId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaal = _budgetService.GetBudgetGevelTotaal(versieId);
            var t = totaal.Value ?? new BudgetGevelTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalGevelNieuwbouw   = t.TotaalGevelNieuwbouw,
                    totaalGevelBestaand    = t.TotaalGevelBestaand,
                    totaalRaamNieuwbouw    = t.TotaalRaamNieuwbouw,
                    totaalRaamBestaand     = t.TotaalRaamBestaand,
                    totaalBallustrade      = t.TotaalBallustrade,
                    totaalZichtscherm      = t.TotaalZichtscherm,
                    totaalPlatDak          = t.TotaalPlatDak,
                    totaalHellendDak       = t.TotaalHellendDak,
                    totaalGroenDak         = t.TotaalGroenDak,
                    totaalDakoversteken    = t.TotaalDakoversteken,
                    totaalOnderkantDoorrit = t.TotaalOnderkantDoorrit,
                    totaalAfbraak          = t.TotaalAfbraak,
                    totaalGevelCombineerd  = t.TotaalGevelCombineerd,
                    totaalRaamCombineerd   = t.TotaalRaamCombineerd,
                    totaalDakCombineerd    = t.TotaalDakCombineerd
                }
            });
        }

        // ── BudgetActivityLijnen ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetActivityLijnen(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .SingleOrDefault();

            if (versie == null) return NotFound();

            var projectResp = _projectService.GetProjectByID(versie.BudgetMaster.ProjectId);

            var lotGroepen = await _budgetActivityService.GetLotGroepenAsync(versieId);

            var aantalEenheden = _uow.BudgetOppervlaktes.GetNoTracking()
                .Count(o => o.BudgetVersieId == versieId);

            var totaalGBA = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId)
                .Sum(o => (decimal?)o.BewoonbareOpp) ?? 0m;

            var beschikbareProjecten = await _budgetActivityService.GetProjectenVoorNacalcAsync();

            var model = new BudgetActivityLijnenModel
            {
                BudgetVersieId       = versieId,
                ProjectId            = versie.BudgetMaster.ProjectId,
                ProjectName          = projectResp.Success ? projectResp.Value?.Name : string.Empty,
                BudgetNaam           = versie.BudgetMaster.Naam,
                Versienummer         = versie.Versienummer,
                VersieLabel          = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                           ? $"v{versie.Versienummer}"
                                           : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus         = versie.Status,
                LotGroepen           = lotGroepen,
                AantalEenheden       = aantalEenheden,
                OppervlakteGBA       = totaalGBA,
                SIndexStart          = versie.BudgetGegevens?.SIndexStart  ?? 0m,
                SIndexHuidig         = versie.BudgetGegevens?.SIndexHuidig ?? 0m,
                IIndexStart          = versie.BudgetGegevens?.IIndexStart  ?? 0m,
                IIndexHuidig         = versie.BudgetGegevens?.IIndexHuidig ?? 0m,
                BeschikbareProjecten = beschikbareProjecten
            };

            ViewData["Referrer"] = Request.Headers["Referer"].ToString();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveActivityLijnen([FromBody] SaveActivityLijnenRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "Ongeldig verzoek." });

            var lijnen = request.Lijnen?.Select(dto => new BudgetActivityLijnBO
            {
                ActivityId                  = dto.ActivityId,
                AlternatievePrijsPerEenheid = dto.AlternatievePrijsPerEenheid ?? 0m,
                NacalcPrijsPerEenheid       = dto.NacalcPrijsPerEenheid       ?? 0m,
                Correctiefactor             = dto.Correctiefactor,
                IsManueel                   = dto.IsManueel
            }).ToList() ?? new List<BudgetActivityLijnBO>();

            var response = await _budgetActivityService.SaveLijnenAsync(request.BudgetVersieId, lijnen);

            return Json(new { success = response.Success, message = response.Messages.FirstOrDefault()?.Message });
        }

        [HttpPost]
        public IActionResult ImportNacalcVanProject(int bronProjectId, int doelVersieId)
        {
            return Json(new { success = false, message = "Nog niet geïmplementeerd." });
        }

        [HttpGet]
        public async Task<IActionResult> GetBouwIndexen(string type)
        {
            var lijst = await _bouwIndex.GetAlleIndexenAsync(type);
            return Json(lijst.Select(x => new {
                id = x.Id,
                jaar = x.Jaar,
                maand = x.Maand,
                indexWaarde = x.IndexWaarde,
                isActief = x.IsActief
            }));
        }

        // ── BudgetParams (stap 7) ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetParams(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var budgetParams = await _berekeningService.GetOrCreateParamsAsync(versieId);

            var aantalEenh = _uow.BudgetOppervlaktes.GetNoTracking()
                .Count(o => o.BudgetVersieId == versieId);

            var totaalBouw = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId)
                .AsEnumerable()
                .Sum(l => (l.AlternatievePrijsPerEenheid ?? 0m) * aantalEenh);

            var model = new BudgetParamsModel
            {
                BudgetVersieId = versieId,
                ProjectId      = versie.ProjectId,
                ProjectName    = versie.BudgetMaster?.Naam ?? string.Empty,
                BudgetNaam     = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer   = versie.Versienummer,
                VersieLabel    = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                     ? $"v{versie.Versienummer}"
                                     : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus   = versie.Status,
                Params         = budgetParams,
                TotaalBouw     = totaalBouw,
                AantalEenheden = aantalEenh,
                AantalLiften   = versie.BudgetGegevens?.AantalLiften ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetParams(BudgetParamsModel model)
        {
            var bestaand = await _db.BudgetParams
                .FirstOrDefaultAsync(p => p.BudgetVersieId == model.BudgetVersieId);

            if (bestaand == null)
            {
                model.Params.BudgetVersieId = model.BudgetVersieId;
                _db.BudgetParams.Add(model.Params);
            }
            else
            {
                bestaand.ProjectcoordinatiePerc  = model.Params.ProjectcoordinatiePerc;
                bestaand.ArchitectPerc           = model.Params.ArchitectPerc;
                bestaand.VeiligheidscoordEPBPerc = model.Params.VeiligheidscoordEPBPerc;
                bestaand.VentVerslaggeverForfait  = model.Params.VentVerslaggeverForfait;
                bestaand.StudieIRPerc             = model.Params.StudieIRPerc;
                bestaand.OpmetingSonderingForfait = model.Params.OpmetingSonderingForfait;
                bestaand.DecennaleGeslRuwbouwPerc = model.Params.DecennaleGeslRuwbouwPerc;
                bestaand.ABRPlaatsbeschrPerc      = model.Params.ABRPlaatsbeschrPerc;
                bestaand.InfrastructuurForfait    = model.Params.InfrastructuurForfait;
                bestaand.LiftPrijsPerStuk         = model.Params.LiftPrijsPerStuk;
                bestaand.WetBreynePerc            = model.Params.WetBreynePerc;
                bestaand.WetBreyneMaanden         = model.Params.WetBreyneMaanden;
                bestaand.StraightloanGebouwPerc   = model.Params.StraightloanGebouwPerc;
                bestaand.StraightloanGebouwMaanden= model.Params.StraightloanGebouwMaanden;
                bestaand.StraightloanGrondPerc    = model.Params.StraightloanGrondPerc;
                bestaand.StraightloanGrondMaanden = model.Params.StraightloanGrondMaanden;
                bestaand.AankoopprijsGrond        = model.Params.AankoopprijsGrond;
                bestaand.OnvoorzienPerc           = model.Params.OnvoorzienPerc;
                bestaand.PubliciteitForfait       = model.Params.PubliciteitForfait;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(BudgetVerkoop), new { versieId = model.BudgetVersieId });
        }

        // ── BudgetVerkoop (stap 8) ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetVerkoop(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var lijnen = await _db.BudgetVerkoopLijn
                .Where(l => l.BudgetVersieId == versieId)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            var eenheden = await _db.BudgetOppervlaktes
                .Where(o => o.BudgetVersieId == versieId)
                .Select(o => o.EenheidNaam)
                .Distinct()
                .ToListAsync();

            var model = new BudgetVerkoopModel
            {
                BudgetVersieId        = versieId,
                ProjectId             = versie.ProjectId,
                ProjectName           = versie.BudgetMaster?.Naam ?? string.Empty,
                BudgetNaam            = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer          = versie.Versienummer,
                VersieLabel           = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                            ? $"v{versie.Versienummer}"
                                            : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus          = versie.Status,
                Lijnen                = lijnen,
                PrijsReferentiesBouw  = await _db.BudgetPrijsReferentie
                                            .Where(p => p.PrijsType == "Bouw" &&
                                                       (p.ProjectId == null || p.ProjectId == versie.ProjectId))
                                            .OrderBy(p => p.Code).ToListAsync(),
                PrijsReferentiesGrond = await _db.BudgetPrijsReferentie
                                            .Where(p => p.PrijsType == "Grond" &&
                                                       (p.ProjectId == null || p.ProjectId == versie.ProjectId))
                                            .OrderBy(p => p.Code).ToListAsync(),
                BeschikbareEenheden   = eenheden
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBudgetVerkoop([FromBody] SaveVerkoopRequest req)
        {
            try
            {
                var bestaand = await _db.BudgetVerkoopLijn
                    .Where(l => l.BudgetVersieId == req.BudgetVersieId).ToListAsync();
                _db.BudgetVerkoopLijn.RemoveRange(bestaand);

                for (int i = 0; i < req.Lijnen.Count; i++)
                {
                    req.Lijnen[i].Id             = 0;
                    req.Lijnen[i].BudgetVersieId = req.BudgetVersieId;
                    req.Lijnen[i].SortOrder      = i;
                    req.Lijnen[i].BudgetVersie   = null;
                    req.Lijnen[i].Unit           = null;
                }
                _db.BudgetVerkoopLijn.AddRange(req.Lijnen);
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BlankVerkoopRij(int versieId, string eenheidNaam)
        {
            var lijn = new BudgetVerkoopLijn
            {
                BudgetVersieId = versieId,
                EenheidNaam    = eenheidNaam
            };
            return PartialView("Partials/_VerkoopRij", lijn);
        }

        // ── BudgetResultaat (stap 9) ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetResultaat(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);
            resultaat.VersieNaam   = versie.VersieNaam   ?? string.Empty;
            resultaat.Versienummer = versie.Versienummer;

            var aantalEenh = _uow.BudgetOppervlaktes.GetNoTracking()
                .Count(o => o.BudgetVersieId == versieId);

            var altBouw = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId)
                .AsEnumerable()
                .Sum(l => (l.AlternatievePrijsPerEenheid ?? 0m) * aantalEenh);

            var nacBouw = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId)
                .AsEnumerable()
                .Sum(l => (l.NacalcPrijsPerEenheid ?? 0m) * aantalEenh);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var andereVersies = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == versie.BudgetMasterId && v.Id != versieId)
                .OrderByDescending(v => v.Versienummer)
                .ToList();

            var model = new BudgetResultaatModel
            {
                BudgetVersieId       = versieId,
                ProjectId            = versie.ProjectId,
                ProjectName          = versie.BudgetMaster?.Naam ?? string.Empty,
                BudgetNaam           = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer         = versie.Versienummer,
                VersieLabel          = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                           ? $"v{versie.Versienummer}"
                                           : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus         = versie.Status,
                BudgetMasterId       = versie.BudgetMasterId,
                Resultaat            = resultaat,
                TotaalBouwAlternatief = altBouw,
                TotaalBouwNacalc     = nacBouw * gewogen,
                GewogenFactor        = gewogen,
                AndereVersies        = andereVersies
            };

            return View(model);
        }

        // ── BudgetVergelijken ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetVergelijken(int masterId)
        {
            var master = await _db.BudgetMaster
                .Include(m => m.BudgetVersies)
                .FirstOrDefaultAsync(m => m.Id == masterId);

            if (master == null) return NotFound();

            var project = await _db.Project.FindAsync(master.ProjectId);

            var model = new BudgetVergelijkenModel
            {
                BudgetMasterId = masterId,
                ProjectId      = master.ProjectId,
                ProjectName    = project?.ProjectName ?? string.Empty,
                BudgetNaam     = master.Naam,
                AlleVersies    = master.BudgetVersies
                                    .OrderByDescending(v => v.Versienummer)
                                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VergelijkenResultaat([FromBody] VergelijkRequest req)
        {
            if (req?.VersieIds == null || !req.VersieIds.Any())
                return Json(new { success = false, message = "Geen versies geselecteerd." });

            var resultaten = await _berekeningService.GetVergelijkingAsync(req.VersieIds);
            return Json(new { success = true, resultaten });
        }

        [HttpPost]
        public async Task<IActionResult> HerstelVersie(int versieId)
        {
            var bron = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (bron == null)
                return Json(new { success = false, message = "Versie niet gevonden." });

            var maxVersie = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == bron.BudgetMasterId)
                .Max(v => (int?)v.Versienummer) ?? 0;

            var nieuw = new BudgetVersie
            {
                BudgetMasterId = bron.BudgetMasterId,
                ProjectId      = bron.ProjectId,
                Versienummer   = maxVersie + 1,
                VersieNaam     = $"Herstel van v{bron.Versienummer}",
                Status         = "Concept",
                IsHuidig       = false,
                CreatedAt      = DateTime.Now
            };
            _db.BudgetVersie.Add(nieuw);
            await _db.SaveChangesAsync();

            // Gegevens
            if (bron.BudgetGegevens != null)
            {
                var g = bron.BudgetGegevens;
                _db.BudgetGegevens.Add(new BudgetGegevens
                {
                    BudgetVersieId                 = nieuw.Id,
                    Naam                           = g.Naam,
                    Adres                          = g.Adres,
                    SIndexStart                    = g.SIndexStart,
                    SIndexHuidig                   = g.SIndexHuidig,
                    IIndexStart                    = g.IIndexStart,
                    IIndexHuidig                   = g.IIndexHuidig,
                    NacalcBasisprijs               = g.NacalcBasisprijs,
                    NacalcBasisJaar                = g.NacalcBasisJaar,
                    AantalLiften                   = g.AantalLiften,
                    AantalBinnentrappen            = g.AantalBinnentrappen,
                    AantalBovengrondseVerdiepingen = g.AantalBovengrondseVerdiepingen,
                    AantalVerdiepingenOndergronds  = g.AantalVerdiepingenOndergronds,
                    TypePoorten                    = g.TypePoorten,
                    TypeDak                        = g.TypeDak,
                    GevelLeienSidings              = g.GevelLeienSidings,
                    OppFunderingen                 = g.OppFunderingen,
                    M3Grondwerk                    = g.M3Grondwerk,
                    LmBerlinerwanden               = g.LmBerlinerwanden,
                    LmSecanpalen                   = g.LmSecanpalen,
                    GevelMetselwerkPrijsPerM2      = g.GevelMetselwerkPrijsPerM2,
                    GipswerkenPrijsPerM2           = g.GipswerkenPrijsPerM2
                });
            }

            // Oppervlaktes
            var oppLijnen = await _db.BudgetOppervlaktes
                .Where(o => o.BudgetVersieId == versieId).ToListAsync();
            foreach (var o in oppLijnen)
                _db.BudgetOppervlaktes.Add(new BudgetOppervlaktes
                {
                    BudgetVersieId          = nieuw.Id,
                    EenheidNaam             = o.EenheidNaam,
                    UnitGroupTypeId         = o.UnitGroupTypeId,
                    UnitTypeId              = o.UnitTypeId,
                    SortOrder               = o.SortOrder,
                    BewoonbareOpp           = o.BewoonbareOpp,
                    Tuin                    = o.Tuin,
                    TerrasPrefab            = o.TerrasPrefab,
                    TerrasGelijkvloers      = o.TerrasGelijkvloers,
                    Dakterras               = o.Dakterras,
                    GaragesParkingsBovenGr  = o.GaragesParkingsBovenGr,
                    GarBergOndergronds      = o.GarBergOndergronds,
                    BergGelijkvloers        = o.BergGelijkvloers,
                    Carports                = o.Carports,
                    DoorritGVL              = o.DoorritGVL,
                    Zolder                  = o.Zolder,
                    GemeenschappelijkeDelen = o.GemeenschappelijkeDelen,
                    Wegenis                 = o.Wegenis,
                    Grondopp                = o.Grondopp
                });

            // Sanitair
            var sanitairLijnen = await _db.BudgetSanitair
                .Where(s => s.BudgetVersieId == versieId).ToListAsync();
            foreach (var s in sanitairLijnen)
                _db.BudgetSanitair.Add(new BudgetSanitair
                {
                    BudgetVersieId       = nieuw.Id,
                    EenheidNaam          = s.EenheidNaam,
                    UnitTypeId           = s.UnitTypeId,
                    SortOrder            = s.SortOrder,
                    Badkamer             = s.Badkamer,
                    ToiletInBadkamer     = s.ToiletInBadkamer,
                    AfzonderlijkToilet   = s.AfzonderlijkToilet,
                    DoucheInBadkamer     = s.DoucheInBadkamer,
                    Douchekamer          = s.Douchekamer
                });

            // Gevelelementen
            var gevelLijnen = await _db.BudgetGevelElementen
                .Where(g => g.BudgetVersieId == versieId).ToListAsync();
            foreach (var g in gevelLijnen)
                _db.BudgetGevelElementen.Add(new BudgetGevelElementen
                {
                    BudgetVersieId = nieuw.Id,
                    ElementType    = g.ElementType,
                    EenheidNaam    = g.EenheidNaam,
                    Beschrijving   = g.Beschrijving,
                    Aantal         = g.Aantal,
                    Breedte        = g.Breedte,
                    Hoogte         = g.Hoogte,
                    Lengte         = g.Lengte,
                    SortOrder      = g.SortOrder
                });

            // Activiteitslijnen
            var actLijnen = await _db.BudgetActivityLijnen
                .Where(l => l.BudgetVersieId == versieId).ToListAsync();
            foreach (var l in actLijnen)
                _db.BudgetActivityLijnen.Add(new BudgetActivityLijnen
                {
                    BudgetVersieId              = nieuw.Id,
                    ActivityId                  = l.ActivityId,
                    AlternatievePrijsPerEenheid = l.AlternatievePrijsPerEenheid,
                    NacalcPrijsPerEenheid       = l.NacalcPrijsPerEenheid,
                    Correctiefactor             = l.Correctiefactor,
                    IsManueel                   = l.IsManueel,
                    VerhogingsPerc              = l.VerhogingsPerc,
                    Omschrijving                = l.Omschrijving
                });

            // Params
            var bronParams = await _db.BudgetParams
                .FirstOrDefaultAsync(p => p.BudgetVersieId == versieId);
            if (bronParams != null)
                _db.BudgetParams.Add(new BudgetParams
                {
                    BudgetVersieId          = nieuw.Id,
                    ProjectcoordinatiePerc  = bronParams.ProjectcoordinatiePerc,
                    ArchitectPerc           = bronParams.ArchitectPerc,
                    VeiligheidscoordEPBPerc = bronParams.VeiligheidscoordEPBPerc,
                    VentVerslaggeverForfait = bronParams.VentVerslaggeverForfait,
                    StudieIRPerc            = bronParams.StudieIRPerc,
                    OpmetingSonderingForfait= bronParams.OpmetingSonderingForfait,
                    DecennaleGeslRuwbouwPerc= bronParams.DecennaleGeslRuwbouwPerc,
                    ABRPlaatsbeschrPerc     = bronParams.ABRPlaatsbeschrPerc,
                    InfrastructuurForfait   = bronParams.InfrastructuurForfait,
                    LiftPrijsPerStuk        = bronParams.LiftPrijsPerStuk,
                    WetBreynePerc           = bronParams.WetBreynePerc,
                    WetBreyneMaanden        = bronParams.WetBreyneMaanden,
                    StraightloanGebouwPerc  = bronParams.StraightloanGebouwPerc,
                    StraightloanGebouwMaanden = bronParams.StraightloanGebouwMaanden,
                    StraightloanGrondPerc   = bronParams.StraightloanGrondPerc,
                    StraightloanGrondMaanden= bronParams.StraightloanGrondMaanden,
                    AankoopprijsGrond       = bronParams.AankoopprijsGrond,
                    OnvoorzienPerc          = bronParams.OnvoorzienPerc,
                    PubliciteitForfait      = bronParams.PubliciteitForfait
                });

            // Verkooplijnen
            var verkoopLijnen = await _db.BudgetVerkoopLijn
                .Where(v => v.BudgetVersieId == versieId).ToListAsync();
            foreach (var v in verkoopLijnen)
                _db.BudgetVerkoopLijn.Add(new BudgetVerkoopLijn
                {
                    BudgetVersieId = nieuw.Id,
                    EenheidNaam    = v.EenheidNaam,
                    UnitId         = v.UnitId,
                    CodeBouw       = v.CodeBouw,
                    CodeGrond      = v.CodeGrond,
                    OppTuin        = v.OppTuin,
                    OppTerras      = v.OppTerras,
                    OppDakterras   = v.OppDakterras,
                    IsRuil         = v.IsRuil,
                    ExtraForfait   = v.ExtraForfait,
                    SortOrder      = v.SortOrder
                });

            await _db.SaveChangesAsync();
            return Json(new { success = true, nieuweVersieId = nieuw.Id, versienummer = nieuw.Versienummer });
        }

        // ── DownloadBudgetPDF ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DownloadBudgetPDF(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var activiteitLijnen = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId).ToList();

            var activiteiten = _uow.Activities.GetNoTracking().ToList();
            var groepen      = _uow.ActivityGroups.GetNoTracking().OrderBy(g => g.Lot).ToList();
            var oppervlaktes = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId).OrderBy(o => o.SortOrder).ToList();

            var document = new BudgetDocument(resultaat, versie, activiteitLijnen, activiteiten, groepen, oppervlaktes, gewogen);
            var pdfBytes = document.GeneratePdf();

            var bestandsnaam = $"Budget_{versie.VersieNaam}_v{versie.Versienummer}_{DateTime.Now:yyyyMMdd}.pdf"
                .Replace(Path.GetInvalidFileNameChars(), '_');

            return File(pdfBytes, "application/pdf", bestandsnaam);
        }

        // ── DownloadBudgetExcel ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DownloadBudgetExcel(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var activiteitLijnen = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId).ToList();

            var activiteiten = _uow.Activities.GetNoTracking().ToList();
            var groepen      = _uow.ActivityGroups.GetNoTracking().OrderBy(g => g.Lot).ToList();
            var oppervlaktes = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId).OrderBy(o => o.SortOrder).ToList();

            var excelBytes = _excelService.GenereerBudgetExcel(
                resultaat, versie, activiteitLijnen, activiteiten, groepen, oppervlaktes, gewogen);

            var bestandsnaam = $"Budget_{versie.VersieNaam}_v{versie.Versienummer}_{DateTime.Now:yyyyMMdd}.xlsx"
                .Replace(Path.GetInvalidFileNameChars(), '_');

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                bestandsnaam);
        }

    }
}

// ── Request DTO voor BudgetVergelijken ────────────────────────────────────────
public class VergelijkRequest
{
    public List<int> VersieIds { get; set; } = new();
}

// ── Request DTOs voor BudgetVerkoop ───────────────────────────────────────────
public class SaveVerkoopRequest
{
    public int BudgetVersieId { get; set; }
    public List<BudgetVerkoopLijn> Lijnen { get; set; } = new();
}

// ── Request DTOs voor BudgetActivityLijnen ────────────────────────────────────
public class SaveActivityLijnenRequest
{
    public int                         BudgetVersieId { get; set; }
    public List<ActivityLijnUpdateDto> Lijnen         { get; set; } = new();
}

public class ActivityLijnUpdateDto
{
    public int      ActivityId                  { get; set; }
    public decimal? AlternatievePrijsPerEenheid { get; set; }
    public decimal? NacalcPrijsPerEenheid       { get; set; }
    public decimal  Correctiefactor             { get; set; } = 1m;
    public bool     IsManueel                   { get; set; }
}

// Payloads voor herberekenen
public class RecalculateRequest
{
    // overrides voor instellingen
    public decimal? VatPercent { get; set; }
    public decimal? RegistrationPercent { get; set; }

    public decimal? FixedCertificateCost { get; set; }
    public decimal? SurveyorCost { get; set; }
    public decimal? ConnectionFees { get; set; }
    public decimal? BaseCertificateCost { get; set; }
    public decimal? ParcelCost { get; set; }
    public decimal? MortageRegistrationCost { get; set; }

    public List<UnitDiscountInput> Units { get; set; }
}

public class UnitDiscountInput
{
    public int UnitId { get; set; }
    public decimal? LandDiscount { get; set; }
    public decimal? BuildDiscount { get; set; }
    public bool? IncludePerUnitCosts { get; set; }
}
internal static class PathExtensions
{
    public static string Replace(this string text, char[] invalidChars, char replacement)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var ch in invalidChars)
            text = text.Replace(ch, replacement);
        return text;
    }
}