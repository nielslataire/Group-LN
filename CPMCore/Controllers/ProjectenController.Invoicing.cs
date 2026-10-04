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
    /// <summary>Facturatie van een project (legacy): facturatieoverzicht, betalingsschijven, groepskoppeling, facturen aanmaken. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Invoicing/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Betalingsschijven");
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
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {(model.Group.Id == 0 ? "Betalingsgroep toevoegen" : "Betalingsgroep bewerken")}");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaymentStagesAddUpdate(ProjectPaymentStagesAddUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.VatTypes = GetVatTypeSelectList(model.ProjectId);
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {(model.Group.Id == 0 ? "Betalingsgroep toevoegen" : "Betalingsgroep bewerken")}");
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
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {(model.Group.Id == 0 ? "Betalingsgroep toevoegen" : "Betalingsgroep bewerken")}");
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Betalingsgroep koppelen");
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

                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Betalingsgroep koppelen");
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
                    var selectedRows = request.Invoices.Where(i => i.ClientAccountId == client.Id).ToList();

                    // Mede-eigenaars krijgen elk hun eigen factuur voor hun aandeel (zelfde patroon als
                    // MakeInvoices voor schijven, Niels 2026-09-29): het hoofdaccount krijgt 100% min de
                    // mede-eigenaars-percentages, elke mede-eigenaar zijn CoOwnerPercentage.
                    var coowners = ((DALCore.Models.cpmRunningContext)uow.Context).ClientContacts
                       .AsNoTracking()
                       .Where(cc => cc.ClientAccountId == client.Id && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue)
                       .Select(cc => new { cc.Id, cc.CoOwnerPercentage })
                       .ToList();
                    var coOwnerTotal = coowners.Sum(c => c.CoOwnerPercentage ?? 0m);
                    var mainOwnerShare = Math.Max(0m, 100m - coOwnerTotal);

                    var mainDraft = BuildChangeOrderInvoiceDraft(
                        issuerCompanyId, client.Id, null, mainOwnerShare,
                        changeOrders, selectedRows, alreadyInvoicedByDetail, project);
                    if (mainDraft != null)
                    {
                        try { await cmd.CreateWithLinesAsync(mainDraft, issueNow: false); }
                        catch (Exception ex) { response.AddError(ex.Message); }
                    }

                    foreach (var coowner in coowners)
                    {
                        if (coowner.CoOwnerPercentage.GetValueOrDefault() <= 0m)
                            continue;

                        var coownerDraft = BuildChangeOrderInvoiceDraft(
                            issuerCompanyId, null, coowner.Id, coowner.CoOwnerPercentage ?? 0m,
                            changeOrders, selectedRows, alreadyInvoicedByDetail, project);
                        if (coownerDraft != null)
                        {
                            try { await cmd.CreateWithLinesAsync(coownerDraft, issueNow: false); }
                            catch (Exception ex) { response.AddError(ex.Message); }
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

    }
}
