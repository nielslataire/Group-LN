using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Leveranciers;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using CPMCore.Services.InvoiceExtraction;
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
        public ActionResult AddIncommingInvoice(int projectid, int type, int invoiceid = 0, bool focusUpload = false, int companyId = 0, int contractId = 0, int duplicateFrom = 0)
        {
            //Referrer
            // Na "Opslaan en nieuwe" blijft de oorspronkelijke herkomst (de nacalculatie) behouden, anders zou
            // Annuleren/Opslaan naar het zonet gebruikte lege formulier terugkeren.
            if (TempData["KeepReferrer"] as bool? != true)
            {
                var referrer = Request.Headers["Referer"].ToString();
                TempData["Referrer"] = referrer;
            }
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ViewData["FocusUpload"] = focusUpload;
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

            // Vanuit Nacalculatie > leverancier: leverancier (zonder contract) of contract (met contract) meteen ingevuld.
            if (contractId > 0 && type == 0) model.IncommingInvoice.ContractID = contractId;
            if (companyId > 0 && type != 0)
            {
                model.IncommingInvoice.CompanyId = companyId;
                model.CompanyName = _companyService.GetCompanyNameById(companyId);
            }

            // Dupliceren: alles van de bronfactuur overnemen (leverancier/contract, bedrag, soort, regels) behalve factuurnummer en factuurdatum,
            // die blijven leeg (datum = DateOnly.MinValue) en moeten ingevuld worden.
            if (duplicateFrom > 0)
            {
                var srcResponse = service2.GetIncommingInvoice(duplicateFrom);
                var src = srcResponse.Success ? srcResponse.Values.FirstOrDefault() : null;
                if (src != null && src.ProjectId == projectid)
                {
                    var copy = new IncommingInvoiceBO
                    {
                        ProjectId = projectid,
                        ContractID = src.ContractID is > 0 ? src.ContractID : 0,
                        CompanyId = src.CompanyId,
                        InvoiceExternalId = "",
                        InvoicePrice = src.InvoicePrice,
                        Kind = src.Kind,
                        IncommingInvoiceDate = DateOnly.MinValue
                    };
                    foreach (var d in src.Details)
                    {
                        copy.Details.Add(new IncommingInvoiceDetailBO
                        {
                            Id = 0,
                            IncommingInvoiceID = 0,
                            Description = d.Description,
                            ContractActivityID = d.ContractActivityID,
                            ContractActivityText = d.ContractActivityText,
                            Price = d.Price,
                            IncommingInvoiceType = d.IncommingInvoiceType,
                            ChangeOrderId = d.ChangeOrderId,
                            ChangeOrders = d.ChangeOrders,
                            ActivityID = d.ActivityID
                        });
                    }
                    model.IncommingInvoice = copy;
                    model.Type = copy.ContractID is > 0 ? 0 : 1;
                    if (model.Type == 1 && copy.CompanyId is > 0) model.CompanyName = _companyService.GetCompanyNameById(copy.CompanyId.Value);
                    model.ListActivities.RemoveAll(a => a.ID == 0);
                    model.ListActivities.Insert(0, new IdNameBO { Group = model.Type == 0 ? "Contractactiviteiten" : "Bedrijfsactiviteiten" });
                }
            }

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
            if (ViewData["UseGlV2Layout"] as bool? == true)
                return View("AddIncommingInvoiceV2", model);
            return View(model);
        }
        [HttpPost]
        public ActionResult AddIncommingInvoice(ProjectIncommingInvoiceAddUpdateModel model, List<IncommingInvoiceDetailBO> details, bool saveAndNew = false)
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
                if (ViewData["UseGlV2Layout"] as bool? == true)
                    return View("AddIncommingInvoiceV2", model);
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
                if (saveAndNew && ViewData["UseGlV2Layout"] as bool? == true)
                {
                    // Opslaan en nieuwe (design-handoff 42c): leeg formulier voor hetzelfde project en dezelfde
                    // leverancier/contract; Annuleren/Opslaan keert daarna nog steeds naar de herkomst terug.
                    TempData["Referrer"] = Referrer;
                    TempData["KeepReferrer"] = true;
                    return RedirectToAction("AddIncommingInvoice", "Projecten", new
                    {
                        projectid = model.ProjectId,
                        type = model.Type,
                        companyId = model.IncommingInvoice.CompanyId ?? 0,
                        contractId = model.IncommingInvoice.ContractID ?? 0
                    });
                }
                return Redirect(Referrer.ToString());
            }
            else
            {
                AddMessage("error", $"De factuur is NIET toegevoegd aan het project {model.ProjectName}", "Fout!");
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur toevoegen");
                if (ViewData["UseGlV2Layout"] as bool? == true)
                    return View("AddIncommingInvoiceV2", model);
                return View(model);
            }
        }
        [HttpGet]
        //[Breadcrumb("Inkomende factuur bewerken")]
        [Breadcrumb("Inkomende factuur bewerken", FromAction = "DetailContracts")]
        public ActionResult EditIncommingInvoice(int projectid, int invoiceid)
        {
            // Referrer bijhouden: de POST redirect hierheen terug na het opslaan.
            TempData["Referrer"] = Request.Headers["Referer"].ToString();

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
            if (ViewData["UseGlV2Layout"] as bool? == true)
                return View("EditIncommingInvoiceV2", model);
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
                if (ViewData["UseGlV2Layout"] as bool? == true)
                    return View("EditIncommingInvoiceV2", model);
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
                var terug = Referrer?.ToString();
                return string.IsNullOrWhiteSpace(terug)
                    ? RedirectToAction("Recalculation", "Projecten", new { projectid = model.ProjectId })
                    : Redirect(terug);
            }
            else
            {
                AddMessage("error", $"De factuur is NIET aangepast aan het project {model.ProjectName}", "Fout!");
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Factuur bewerken");
                if (ViewData["UseGlV2Layout"] as bool? == true)
                    return View("EditIncommingInvoiceV2", model);
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

        // ── gl-v2 AddIncommingInvoiceV2 (design-handoff 42c) — drie kleine, gl-v2-eigen aanvullingen op
        // de hierboven ongewijzigde Add/EditIncommingInvoice-actiepaar. Geen van de drie raakt een
        // bestaand legacy-formulierveld of -actie. ──────────────────────────────────────────────────

        /// <summary>Contractinfo voor de rechterzijbalk (42c) — dezelfde velden als de al bestaande
        /// read-only kaart op IncommingInvoiceDetailV2.cshtml, hier als JSON zodat de zijbalk live
        /// bijwerkt zodra de gebruiker een ander contract kiest (dat kan daar niet, het contract staat
        /// er al vast).</summary>
        [HttpPost]
        public ActionResult GetContractInfoForInvoiceFormV2(int contractId, int invoiceId = 0)
        {
            if (contractId == 0) return Json(new { success = false });
            var response = _projectService.GetContract(contractId);
            if (!response.Success || response.Value == null) return Json(new { success = false });
            var c = response.Value;
            var bedrag = c.Activities?.Sum(a => a.EffectivePrice) ?? 0m;

            // Reeds gefactureerd/meerwerk (design-handoff 42c, contractkaart) — zelfde filterpatroon als
            // RecalcLeverancierKaartV2 (ProjectenController.Contracts.cs): "gefactureerd" = Contract +
            // Meerwerk samen (Meerwerk_Klant telt niet mee, dat wordt aan de klant doorgerekend, niet
            // aan dit contract), "meerwerk (WO)" is daarbinnen het Meerwerk-deel apart.
            var facturenResponse = _projectService.GetProjectIncommingInvoicesForRecalculation(c.ProjectId);
            var facturenContract = facturenResponse.Success
                ? facturenResponse.Values.Where(f => f.ContractId == contractId && (invoiceId == 0 || f.InvoiceId != invoiceId)).ToList() // bij bewerken telt de factuur zelf niet mee: de "deze"-som in de kaart komt van de regels in het formulier
                : new List<IncommingInvoiceActivityBO>();
            var meerwerkBedrag = facturenContract.Where(f => f.IncommingInvoiceType == IncommingInvoiceType.Meerwerk).Sum(f => f.SignedPrice);
            var gefactureerdBedrag = facturenContract
                .Where(f => f.IncommingInvoiceType == IncommingInvoiceType.Contract || f.IncommingInvoiceType == IncommingInvoiceType.Meerwerk)
                .Sum(f => f.SignedPrice);

            return Json(new
            {
                success = true,
                companyNaam = c.Company?.Display,
                // Nothing als er geen echte contractnaam is — geen verzonnen "Contract #id"-fallback;
                // de JS laat de rij dan gewoon weg i.p.v. een betekenisloos nummer te tonen.
                contractNaam = string.IsNullOrWhiteSpace(c.ContractName) ? null : c.ContractName,
                contractSigned = c.ContractSigned,
                contractBedrag = bedrag,
                meerwerkBedrag,
                gefactureerdBedrag,
                vatPercentage = c.VatPercentage,
                paymentTerm = c.PaymentTerm,
                cashDiscount = c.CashDiscount,
                cashDiscountPercentage = c.CashDiscountPercentage,
                cashDiscountPaymentTerm = c.CashDiscountPaymentTerm,
                guaranteeType = c.GuaranteeType.ToString(),
                guaranteePercentage = c.GuaranteePercentage,
            });
        }

        /// <summary>PDF (of foto) van de factuur inlezen (design-handoff 42e punt 06) — zelfde Azure
        /// Document Intelligence-dienst (prebuilt-invoice) die de Octopus-verrijkingspijplijn al gebruikt
        /// (CPMCore/Services/InvoiceExtraction), hier voor het eerst interactief aangeroepen. Slaat het
        /// originele bestand ook op (DocStorageService, map "invoices") zodat het later als bijlage kan
        /// dienen — momenteel enkel het pad teruggeven, geen apart bijlageveld op IncommingInvoiceBO.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> AddIncommingInvoiceV2ExtractPdf(IFormFile file)
        {
            if (file is null || file.Length == 0) return Json(new { success = false, message = "Geen bestand ontvangen." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var storage = HttpContext.RequestServices.GetRequiredService<DocStorageService>();
            var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "factuur.pdf" : Path.GetFileName(file.FileName);
            var stored = await storage.UploadAsync(bytes, fileName, file.ContentType ?? "application/pdf", "invoices");

            var invoiceService = HttpContext.RequestServices.GetRequiredService<IAzureInvoiceAnalysisService>();
            if (!invoiceService.IsEnabled)
                return Json(new { success = false, message = "Automatisch inlezen is niet geconfigureerd (geen Azure-sleutel).", path = stored, fileName });

            var result = await invoiceService.AnalyzeAsync(bytes, HttpContext.RequestAborted);
            if (!result.Success)
                return Json(new { success = false, message = result.ErrorMessage ?? "Kon de factuur niet lezen.", path = stored, fileName });

            return Json(new
            {
                success = true,
                path = stored,
                fileName,
                vendorName = result.VendorName,
                invoiceNumber = result.InvoiceId,
                invoiceDate = result.InvoiceDate?.ToString("yyyy-MM-dd"),
                dueDate = result.DueDate?.ToString("yyyy-MM-dd"),
                isCreditNote = string.Equals(result.DocumentType, "CreditNote", StringComparison.OrdinalIgnoreCase),
                amount = result.Lines?.Sum(l => l.Amount ?? 0m),
            });
        }

        /// <summary>Zelfde als AddIncommingInvoiceDetailRow, enkel de gl-v2-partial — de binding
        /// (Html.BeginCollectionItem("details")) en de business-regels (ChangeOrders per contract,
        /// Contract- vs Geen_Contract-default) blijven identiek.</summary>
        [HttpPost]
        public PartialViewResult AddIncommingInvoiceDetailRowV2(int ActivityId, string ActivityName, int ContractId, int CompanyId)
        {
            var nIncommingInvoiceDetail = new IncommingInvoiceDetailBO();
            var response = _projectService.GetContractChangeOrdersForSelect(ContractId);
            if (response.Success)
                nIncommingInvoiceDetail.ChangeOrders = response.Values;

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

            return PartialView("_IncommingInvoiceDetailRowV2", nIncommingInvoiceDetail);
        }
    }
}
