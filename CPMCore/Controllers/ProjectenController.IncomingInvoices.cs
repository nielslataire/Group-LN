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
    /// <summary>Inkomende (aankoop)facturen van een project. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/IncomingInvoices/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur toevoegen");
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

                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur toevoegen");
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
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur toevoegen");
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


            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur bewerken");
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

                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur bewerken");
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
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur bewerken");
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
            // Kruimelpad stopt bij "Nacalculatie" (wáár dit zit) i.p.v. nog een vierde "Inkomende
            // factuur"-knoop toe te voegen die letterlijk de titel herhaalt — design-handoff punt 13
            // "Topbar met lange namen": titel = wát het is, kruimelpad = wáár het zit, en het laatste
            // kruimelitem is nooit de titel zelf. Zelfde reden waarom de titel hieronder niet meer
            // begint met de projectnaam (die staat al in het pad).
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
            ViewData["BreadcrumbNode"] = projectContracts;
            var _psInv = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _psInv.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _psInv.HasDelete(PermissionCodes.ProjectsSuppliers);

            SetPageHeader("bx bx-building-house", BuildIncommingInvoiceDisplayTitle(model.IncommingInvoice.InvoiceExternalId, invoiceid));
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "IncommingInvoiceDetailV2" : "IncommingInvoiceDetail", model);
        }

        // Titel = wát het is (design-handoff punt 13): type + de leveranciersreferentie, nooit de
        // projectnaam (die staat al in het kruimelpad). Zelfde "#id"-terugval als
        // InvoicesController.BuildInvoiceDisplayTitle voor een factuur zonder referentie.
        private static string BuildIncommingInvoiceDisplayTitle(string externalId, int invoiceId)
        {
            var reference = string.IsNullOrWhiteSpace(externalId) ? $"#{invoiceId}" : externalId.Trim();
            return $"Inkomende factuur {reference}";
        }

        // gl-v2 layout-pilot — eigen partial/markup, niet gedeeld met de legacy magnific-popup-versie
        // (ModalDeleteIncommingInvoice hierboven), zelfde patroon als InvoicesController.ModalDeleteV2.
        // GET-link i.p.v. POST+antiforgery: DeleteIncommingInvoice zelf is (bewust ongewijzigd hier)
        // ook al een GET-actie, geen nieuw contract verzonnen voor deze knop alleen.
        [HttpGet]
        public ActionResult ModalDeleteIncommingInvoiceV2(int id, string companyname)
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

            return PartialView("Modals/_ModalDeleteIncommingInvoiceV2", viewModel);
        }

    }
}
