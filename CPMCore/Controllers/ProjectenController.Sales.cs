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
    /// <summary>Verkoop van een project: te-koop-overzicht, verkoopinstellingen, verkooplijst-PDF, kostprijsberekening. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Sales/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Verkoop");
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

            SetPageHeader("bx bx-building-house", $"{viewModel.Project.Name} - Verkoopsinstellingen");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SalesSettings(ProjectSalesSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBankAccountsAsync(model);
                SetPageHeader("bx bx-building-house", $"{(model.Project?.Name ?? _projectService.GetProjectNameById(model.ProjectId))} - Verkoopsinstellingen");
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
                    SetPageHeader("bx bx-building-house", $"{(model.Project?.Name ?? _projectService.GetProjectNameById(model.ProjectId))} - Verkoopsinstellingen");
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



    }
}
