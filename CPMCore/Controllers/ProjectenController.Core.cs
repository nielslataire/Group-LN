using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Leveranciers;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore;
using DALCore.Models;
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
using ClosedXML.Excel;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    /// <summary>Het project zelf: overzicht/zoeken, toevoegen, detail, bewerken, standaardfoto. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Core/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL ==========
        [Breadcrumb("Projecten", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public IActionResult Index(bool showAll = false)
        {
            SetPageHeader("bx bx-building-house", "Projecten");
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

                // Voortgang voor álle projecten: de standaardsortering (fase, van
                // begin naar einde) leunt erop, niet enkel de zichtbare pagina.
                var voortgangAll = _voortgangService.GetForProjects(response.Values.Select(p => p.Id));
                var orderedProjects = OrderProjectsForList(response.Values, voortgangAll);

                model.InitialLimit = initialLimit;
                model.BatchSize = batchSize;
                model.TotalProjectCount = orderedProjects.Count;
                model.Projects = orderedProjects.Take(initialLimit).ToList();
                model.VisibleProjectCount = model.Projects.Count;
                model.Voortgang = voortgangAll;

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
                }
            }

            var statusResponse = service.GetStatuses();
            if (statusResponse.Success && statusResponse.Values is not null)
            {
                model.Statuses = statusResponse.Values;
            }

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "IndexV2" : "Index", model);
        }

        [HttpGet]
        public IActionResult QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(Array.Empty<object>());

            var results = _uow.Projects.GetNoTracking()
                .Where(p => p.ProjectName.Contains(q)
                            || p.Street.Contains(q)
                            || p.PostalCode.Gemeente.Contains(q)
                            || p.PostalCode.Postcode.Contains(q))
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
            SetPageHeader("bx bx-building-house", "Projecten");
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

            ViewData["SubTitle"] = "Alle projecten";
            ViewData["SubTitleText"] = "Overzicht van alle projecten binnen CPM.";

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "IndexV2" : "Index", model);
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

            // Zelfde sortering als Index, anders springt "Laad meer" door elkaar.
            var voortgangAll = _voortgangService.GetForProjects(response.Values.Select(p => p.Id));
            var orderedProjects = OrderProjectsForList(response.Values, voortgangAll);

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
                Voortgang = voortgangAll
            };

            return PartialView(ViewData["UseGlV2Layout"] as bool? == true ? "_ProjectGridItemsV2" : "_ProjectGridItems", model);
        }

        /// <summary>
        /// Standaardvolgorde voor de projectenlijst: eerst op fase (in uitvoering →
        /// opstart → afgewerkt → opgeleverd → stopgezet, zie <see cref="ProjectPhase"/>),
        /// daarbinnen projecten zonder opleverdatum eerst en dan op opleverdatum aflopend.
        /// </summary>
        private static List<ProjectBO> OrderProjectsForList(
            IEnumerable<ProjectBO> projects, IDictionary<int, ProjectVoortgangBO> voortgang)
        {
            return projects
                .OrderBy(p => ProjectPhase.SortKey(
                    p, voortgang.TryGetValue(p.Id, out var vg) ? vg : null))
                .ThenByDescending(p => p.DeliveryDate == null)
                .ThenByDescending(p => p.DeliveryDate)
                .ToList();
        }

        [HttpGet]
        [Breadcrumb("Project toevoegen", FromAction = nameof(Index))]
        public IActionResult Toevoegen()
        {
            SetPageHeader("bx bx-building-house", "Project toevoegen");
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = string.IsNullOrEmpty(referrer)
                ? Url.Action("Index", "Projecten")
                : referrer;

            var model = new ProjectModel();
            model.Project.Postalcode.Country.CountryId = 19;
            model.Project.Postalcode.Country.ISOCode = "BE";
            model.SelectedCountry = 19;

            FillInAddSelectLists(model);
            FillInAvailableUsers(model);
            model.Users = GetOrderedUsers();

            SetToevoegenBreadcrumbV2();
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "ToevoegenV2" : "Toevoegen", model);
        }

        // gl-v2 (design-handoff punt 19): titel "Nieuw project" — de kruimel stopt bij "Projecten" i.p.v.
        // "Project toevoegen" te herhalen (zelfde titel/kruimel-regel als de rest van gl-v2).
        private void SetToevoegenBreadcrumbV2()
        {
            if (ViewData["UseGlV2Layout"] as bool? != true) return;
            var home = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
            // "Projecten" is hier de laatste kruimel, maar niet de huidige pagina — dus klikbaar (→ Projecten/Index).
            ViewData["BreadcrumbLastIsLink"] = true;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Project toevoegen", FromAction = nameof(Index))]
        public async Task<IActionResult> Toevoegen(ProjectModel model)
        {
            SetPageHeader("bx bx-building-house", "Project toevoegen");
            if (!ModelState.IsValid)
            {
                FillInAddSelectLists(model);
                FillInAvailableUsers(model);
                model.Users = GetOrderedUsers();
                SetToevoegenBreadcrumbV2();
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "ToevoegenV2" : "Toevoegen", model);
            }

            model.Project.Postalcode.Country.CountryId = model.SelectedCountry;
            // Geen gemeente gekozen -> null bewaren i.p.v. 0 (0 is geen geldige FK en
            // liet Detail crashen op (int)PostcodeId).
            model.Project.Postalcode.PostcodeId = model.SelectedPostalcode > 0 ? model.SelectedPostalcode : (int?)null;
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

                // Minimale aanmaak -> meteen door naar Bewerken om de rest aan te vullen.
                AddMessage("success", $"Project {model.Project.Name} is aangemaakt — vul de overige gegevens aan.", "Geslaagd!");
                return RedirectToAction("Edit", new { projectid = newProjectId });
            }

            AddMessage("error", $"Het project {model.Project.Name} is NIET toegevoegd", "Fout!");

            FillInAddSelectLists(model);
            FillInAvailableUsers(model);
            model.Users = GetOrderedUsers();
            SetToevoegenBreadcrumbV2();
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "ToevoegenV2" : "Toevoegen", model);
        }


        [HttpGet]
        //[Breadcrumb("Info")]
        [Breadcrumb("Info", FromAction = "Index")]
        public async Task<ActionResult> Detail(int projectid, bool EditGeneralData = false)
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
            model.SelectedPostalcode = model.Project.Postalcode.PostcodeId ?? 0;
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
            var response4 = Service.GetPicturesByProjectId(projectid);
            if ((response4.Success))
            {
                var allPics = response4.Values.OrderByDescending(p => p.DateTimeUploaded).ToList();
                model.LatestPictures = allPics.Take(4).ToList();
                model.TotalPictureCount = allPics.Count;
            }
            var response5 = Service.GetLatestProjectDocs(5, projectid);
            if ((response5.Success))
                model.LatestDocs = response5.Values;

            // Voortgang: dezelfde databron als de projectenlijst (Index) en het
            // projectleider-dashboard, hier voor het eerst op de project-hub zelf
            // getoond i.p.v. enkel op de kaart-overzichten.
            _voortgangService.GetForProjects(new[] { projectid }).TryGetValue(projectid, out var voortgang);
            model.Voortgang = voortgang;

            // Punten: enkel de statussen die elders in de app al als "actief" gelden
            // (zie ProjectIssuesController.SendPreview's activeStatuses) — Search()
            // zonder statusfilter geeft alles terug, inclusief Opgelost/Afgesloten/
            // Afgewezen, die geen aandacht meer vragen op een opvolgingspaneel.
            var activeIssueStatuses = new HashSet<int> { 0, 2, 3, 7 }; // Open, Gepland, WaitingInspection, Reopened
            var allIssues = await _issueService.Search(projectid, new ConstructionIssueFilterBO());
            model.OpenIssues = allIssues.Where(i => activeIssueStatuses.Contains(i.Status))
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.DueDate)
                .ToList();
            model.OpenIssuesCount = model.OpenIssues.Count;

            // Mijlpalen: enkel de nog niet bereikte/n.v.t. mijlpalen van dit project, voor het
            // aandachtspaneel (achterstallig = urgent, binnen 14 dagen = op te lossen) — zelfde
            // statusuitsluiting als ProjectTraject/Index.cshtml en _MijnKeypointsWidget.
            var alleMijlpalen = await _mijlpaalService.SearchPortfolio(new[] { projectid }, new MijlpaalFilterBO { ProjectId = projectid });
            model.AttentionMijlpalen = alleMijlpalen
                .Where(m => m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing)
                .Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) != null)
                .ToList();

            // Contracten: opvolging die nog actie vraagt (niet getekend / waarborg ontbreekt).
            var contractsResp = Service.GetProjectContracts(projectid);
            var contracts = contractsResp.Success ? contractsResp.Values : new List<ContractBO>();
            model.UnsignedContracts = contracts.Where(c => !c.ContractSigned).ToList();
            model.GuaranteeMissingContracts = contracts.Where(c => c.GuaranteeDocumentMissing).ToList();

            // Verzekeringen: CheckInsurances() heeft geen project-specifieke overload,
            // dus alle projecten ophalen en hier filteren (zelfde aanpak als HomeController
            // voor het projectleider-dashboard).
            var insuranceResp = _insuranceService.CheckInsurances();
            model.ProjectInsuranceWarnings = insuranceResp.Success
                ? insuranceResp.Values.Where(w => w.ProjectId == projectid).ToList()
                : new List<WarningBO>();

            // Verzekeringen: de 3 reële polistypes (ABR/Brand/10-jarige) met hun
            // eigen start-/einddatum, voor de kaart met status-chips op de hub
            // (los van de globale waarschuwingenlijst hierboven).
            var insurancesResp = Service.GetProjectInsurances(projectid);
            model.Insurances = insurancesResp.Success ? insurancesResp.Values : new List<InsuranceBO>();

            // Verkoop-KPI's: hergebruikt dezelfde aggregatie als de projectenlijst
            // (Index) — percentage/waarde verkocht, geen eigen berekening nodig.
            var salesDataResp = Service.GetProjectSalesData(new List<int> { projectid });
            model.SalesData = salesDataResp.Success ? salesDataResp.Values.FirstOrDefault() : null;

            // Eenheden & verkoopstatus: per-lot rij met status (Beschikbaar/Optie/
            // Verkocht/Akte verleden) en klantnaam, via een reverse unit->klant
            // lookup (er bestaat geen klaar-voor-gebruik unit->klant join).
            var unitsResp = _unitService.GetUnitsWithAttachedByProjectId(projectid);
            if (unitsResp.Success && unitsResp.Values is not null)
            {
                var clientsWithUnitsResp = cservice.GetClientAccountsByProjectIdWithUnits(projectid);
                var clientByUnitId = new Dictionary<int, ClientAccountWithUnitsBO>();
                if (clientsWithUnitsResp.Success && clientsWithUnitsResp.Values is not null)
                {
                    foreach (var cwu in clientsWithUnitsResp.Values)
                        foreach (var u in cwu.Units ?? new List<UnitBO>())
                            clientByUnitId[u.Id] = cwu;
                }

                // Namen van de afwerkingen in één query (de rijen tonen ze per eenheid).
                var rowUnitIds = unitsResp.Values.Select(x => x.Unit.Id).ToList();
                var optionNames = _db.UnitFinishingOption.AsNoTracking()
                    .Where(o => rowUnitIds.Contains(o.UnitId))
                    .ToDictionary(o => o.Id, o => o.Name);

                model.UnitRows = unitsResp.Values.Select(u =>
                {
                    // Verkocht: exact dezelfde waarde als op Klanten/Detail per eenheid
                    // (UnitBO.TotalValueSold = grondwaarde verkocht + som van alle
                    // ValueSold-bouwwaarden). Bij een verkocht pand zit de afwerking
                    // al in ValueSold verrekend, dus geen aparte afwerkingsopties.
                    // Nog te koop: basis-bouwwaarde (rijen zonder FinishingOptionId)
                    // apart van de afwerkingsopties (rijen mét FinishingOptionId),
                    // die elk apart getoond worden.
                    bool isSold = u.Unit.ClientAccountId is not null;
                    var constructionValues = u.Unit.ConstructionValues ?? new List<UnitConstructionValueBO>();
                    // Constructieprijs volgens ServiceCore.Helpers.UnitPricing (dezelfde regel als de
                    // publieke site): zonder afwerkingen alle constructieprijzen samen; met afwerkingen is
                    // elke afwerking een volledig alternatief. De views tellen Vraagprijs + Afwerking.Cost
                    // op, dus bij afwerkingen is Vraagprijs enkel de grondwaarde en Cost het totaal van
                    // die afwerking (i.p.v. één losse bouwwaarderegel).
                    var pricing = ServiceCore.Helpers.UnitPricing.Compute(constructionValues.Select(cv => (cv.FinishingOptionId, cv.Value ?? 0m)));

                    decimal prijs = isSold
                        ? u.Unit.TotalValueSold
                        : (u.Unit.LandValue ?? 0m) + (pricing.HasOptions ? 0m : pricing.From);

                    var afwerkingen = isSold || !pricing.HasOptions
                        ? new List<(string Description, decimal Cost)>()
                        : constructionValues
                            .Where(cv => cv.FinishingOptionId is not null)
                            .GroupBy(cv => cv.FinishingOptionId!.Value)
                            .Select(g => (
                                Description: optionNames.TryGetValue(g.Key, out var optionName) && !string.IsNullOrWhiteSpace(optionName) ? optionName : "Afwerking",
                                Cost: g.Sum(cv => cv.Value ?? 0m)))
                            .OrderBy(a => a.Cost)
                            .ToList();

                    clientByUnitId.TryGetValue(u.Unit.Id, out var clientWithUnits);
                    var client = clientWithUnits?.Client;

                    // Geen van deze statussen is een fout, dus geen alarm-rood:
                    // Beschikbaar = Sage (open), In optie = Oker (opvolgen),
                    // Verkocht = groen (goede afloop), Akte verleden = Ink (afgesloten).
                    string status; string statusVariant;
                    if (client is not null && client.DateDeedOfSale.HasValue) { status = "Akte verleden"; statusVariant = "dark"; }
                    else if (isSold) { status = "Verkocht"; statusVariant = "primary"; }
                    else if (u.Unit.IsOption) { status = "In optie"; statusVariant = "warning"; }
                    else { status = "Beschikbaar"; statusVariant = "secondary"; }

                    return new ProjectDetailUnitRowVM
                    {
                        UnitId = u.Unit.Id,
                        Naam = u.Unit.Name,
                        TypeName = u.Unit.Type?.Name,
                        Oppervlakte = u.Unit.Surface,
                        Vraagprijs = prijs,
                        Afwerkingen = afwerkingen,
                        Status = status,
                        StatusVariant = statusVariant,
                        KlantNaam = client?.DisplayName,
                        ClientId = client?.Id
                    };
                }).ToList();
            }

            // Facturatie: recentste vorderingsstaten van dit project + openstaand
            // bedrag (dezelfde "openstaand"-definitie als de Boekhouding/CEO-
            // dashboards, hier voor één project).
            model.RecentInvoices = (await _invoiceQueryService.GetByProjectAsync(projectid))
                .Take(5).ToList();
            model.ProjectInvoiceSummary = await _invoiceQueryService.GetDashboardSummaryForProjectAsync(projectid);

            //BREADCRUMBS
            // Kruimelpad stopt bij "Projecten" (wáár dit zit) i.p.v. nog een "Detail"-knoop toe te
            // voegen die letterlijk model.Project.Name herhaalt — exact wat SetPageHeader hieronder al
            // als paginatitel zet. Design-handoff punt 13 "Topbar met lange namen", regel 2: het
            // laatste kruimelitem is nooit de titel zelf. Dit is de projecthub-pagina zelf: er is geen
            // aparte "wát"-identiteit onder de projectnaam (in tegenstelling tot bv. een factuur met
            // een eigen nummer) — de titel IS al "waar je naar kijkt", het pad hoeft dat niet nog eens
            // te zeggen. Gevonden bij een projectbrede sweep na dezelfde fix op Invoices/DetailV2 en
            // Projecten/IncommingInvoiceDetailV2 — dit was de eerste en meest gebruikte pagina die de
            // regel nog schond.
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };

            ViewData["BreadcrumbNode"] = projectenIndex;

            SetPageHeader("bx bx-building-house", model.Project.Name);

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailV2" : "Detail", model);
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
            model.Docs = service.GetProjectDocs(projectid).Values;
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

            // BREADCRUMBS: Home / Projecten / {projectnaam} / Gegevens bewerken
            var bcHome = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var bcProjecten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = bcHome };
            var bcDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.Project.Name)
            {
                Parent = bcProjecten,
                RouteValues = new { projectid = projectid }
            };
            var bcEdit = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Edit", "Projecten", "Gegevens bewerken")
            {
                Parent = bcDetail,
                RouteValues = new { projectid = projectid }
            };
            // gl-v2 (punt 19): titel "Project bewerken" — de kruimel stopt bij de projectnaam i.p.v. een
            // "Gegevens bewerken"-blad toe te voegen dat de titel herhaalt.
            ViewData["BreadcrumbNode"] = ViewData["UseGlV2Layout"] as bool? == true ? bcDetail : bcEdit;
            ViewData["ProjectUnitCount"] = _db.Set<DALCore.Models.Units>().Count(u => u.ProjectId == projectid);

            SetPageHeader("bx bx-building-house", $"{model.Project.Name} — gegevens bewerken");

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditV2" : "Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Project bewerken", FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(EditProjectDetail model)
        {
            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-building-house", $"Project - {model.Project.Name}");
                FillInAddSelectListsDetailEdit(model);
                FillInAvailableUsers(model);
                model.Users = GetOrderedUsers();
                SetEditBreadcrumbV2(model);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditV2" : "Edit", model);
            }

            model.Project.Postalcode.Country.CountryId = model.SelectedCountry;
            // Geen gemeente gekozen -> null bewaren i.p.v. 0 (0 is geen geldige FK).
            model.Project.Postalcode.PostcodeId = model.SelectedPostalcode > 0 ? model.SelectedPostalcode : (int?)null;
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
                    Id = s.Id, // behoudt factuurkoppeling/Progress; de lijstpositie wordt de SortOrder
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
            SetEditBreadcrumbV2(model);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditV2" : "Edit", model);
        }

        // Herweergave na een mislukte Edit-POST: de gl-v2-view heeft de kruimel (tot de projectnaam) en
        // het eenhedenaantal voor het dossiermenu opnieuw nodig — de GET zet ze, de POST niet.
        private void SetEditBreadcrumbV2(EditProjectDetail model)
        {
            if (ViewData["UseGlV2Layout"] as bool? != true) return;
            var pid = model.Project.Id;
            var home = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var list = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.Project.Name ?? "Project")
            {
                Parent = list,
                RouteValues = new { projectid = pid }
            };
            ViewData["ProjectUnitCount"] = _db.Set<DALCore.Models.Units>().Count(u => u.ProjectId == pid);
            model.Docs = _projectService.GetProjectDocs(pid).Values;
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProject = ps.HasWrite(PermissionCodes.ProjectsDetail);
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
            // Alle actieve interne medewerkers (interne gebruiker = geen
            // UserCompanyAccess, d.w.z. geen externe aannemer-/leverancierslogin).
            // Vroeger beperkt tot wie een PermissionPerUser-rij had — dat sloot
            // geldige medewerkers (bv. verkoopverantwoordelijken) ten onrechte uit.
            var users = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && !u.UserCompanyAccess.Any())
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


    }
}
