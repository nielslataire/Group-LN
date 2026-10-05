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
    /// <summary>Wijzigingsopdrachten (legacy lijst/formulieren/PDF; de gl-v2-flow staat in ProjectenController.ChangeOrdersV2/ChangeOrderDetailV2/ChangeOrderFlowV2/QuoteIntakeV2/ChangeOrderSign.cs). Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/ChangeOrders/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== WIJZIGINGSOPDRACHTEN KLANTEN/PROJECTEN ==========

        [HttpGet]
        public async Task<ActionResult> DetailsChangeOrder(int? projectid, int? clientid)
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

            // Elektronisch ondertekenen (fase 1): recentste dossier per wijzigingsopdracht voor de
            // statuskolom en de ingang "Elektronisch laten ondertekenen". Enkel als de module aanstaat.
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var signingFeatures = HttpContext.RequestServices.GetRequiredService<IOptions<CPMCore.Configuration.FeatureFlagsOptions>>().Value;
            if (signingFeatures.EnableSigning && model.CO.Count > 0)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                var cases = await signing.ListCasesAsync(model.ProjectId > 0 ? model.ProjectId : null, null, 1000, HttpContext.RequestAborted);
                var coIds = model.CO.Select(c => c.Id).ToHashSet();
                model.SigningCases = cases
                    .Where(c => c.DocumentType == CPMCore.Services.Signing.ChangeOrderSigningSource.Key && coIds.Contains(c.SourceEntityId))
                    .GroupBy(c => c.SourceEntityId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());
                model.SigningEnabled = true;
                model.CanStartSigning = _ps.HasWrite(PermissionCodes.Signing);
            }

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

            ViewBag.CanWriteProjectChangeOrders = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);
            ViewBag.CanDeleteProjectChangeOrders = _ps.HasDelete(PermissionCodes.ProjectsChangeOrders);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName ?? model.ClientName} - Wijzigingsopdrachten");
            return View(model);
        }

        /// <summary>
        /// Elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md §9.2): zolang er een dossier Draft/Open is
        /// voor deze wijzigingsopdracht, is ze vergrendeld — bewerken en verwijderen weigeren, met
        /// verwijzing naar het dossier. Afgeleid van het actieve dossier, geen vlag op de ChangeOrder.
        /// Module uit = nooit vergrendeld (en geen query naar de signingtabellen).
        /// </summary>
        private async Task<FacadeCore.Signing.CaseStatusView?> ActiveSigningCaseAsync(int changeOrderId)
        {
            if (changeOrderId <= 0) return null;
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<CPMCore.Configuration.FeatureFlagsOptions>>().Value;
            if (!features.EnableSigning) return null;
            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            return await signing.GetActiveCaseForSourceAsync(CPMCore.Services.Signing.ChangeOrderSigningSource.Key, changeOrderId, HttpContext.RequestAborted);
        }

        private const string SigningLockedMessage = "Er loopt een elektronische ondertekening voor deze wijzigingsopdracht. Annuleer die eerst in het ondertekendossier.";

        /// <summary>Verwijderen blokkeren zodra deze wijzigingsopdracht ooit rechtsgeldig ondertekend werd
        /// (Niels, 2026-09-28): de hash-ketting van het dossier is met opzet niet-verwijderbaar, en
        /// `ProjectDocs.ChangeOrderId` heeft geen ON DELETE CASCADE — verwijderen zou anders op een
        /// FK-fout stuklopen. Enkel een applicatie-check, geen DB-constraint: rechtstreeks via SQL blijft
        /// het mogelijk voor een beheerder die dat toch nodig heeft.</summary>
        private async Task<FacadeCore.Signing.CaseStatusView?> CompletedSigningCaseAsync(int changeOrderId)
        {
            if (changeOrderId <= 0) return null;
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<CPMCore.Configuration.FeatureFlagsOptions>>().Value;
            if (!features.EnableSigning) return null;
            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            return await signing.GetCompletedCaseForSourceAsync(CPMCore.Services.Signing.ChangeOrderSigningSource.Key, changeOrderId, HttpContext.RequestAborted);
        }

        private const string SigningCompletedLockedMessage = "Deze wijzigingsopdracht is elektronisch ondertekend en kan niet meer verwijderd worden. Het ondertekenbewijs (audit trail) blijft bewaard.";

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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht toevoegen");
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
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht toevoegen");
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
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht toevoegen");
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
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht toevoegen");
            return View("AddChangeOrder", model);
        }

        [HttpGet]
        //[Breadcrumb("Wijzigingsopdracht bewerken")]
        [Breadcrumb("Wijzigingsopdracht bewerken", FromController = typeof(KlantenController), FromAction = nameof(KlantenController.Detail))]
        public async Task<ActionResult> EditChangeOrder(int projectid, int clientid, int coid)
        {
            // 0) Basisvalidatie
            if (projectid <= 0)
            {
                AddMessage("error", "Ongeldig project.", "Fout!");
                return RedirectToAction("Index", "Projecten");
            }

            // 0b) Vergrendeld door een lopende elektronische ondertekening?
            var activeSigning = await ActiveSigningCaseAsync(coid);
            if (activeSigning is not null)
            {
                AddMessage("warning", SigningLockedMessage, "Vergrendeld");
                return RedirectToAction("Dossier", "SigningAdmin", new { id = activeSigning.CaseId });
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht bewerken");
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> EditChangeOrder(ProjectChangeOrderAddUpdateModel model, List<ChangeOrderDetailBO> Details)
        {
            // Vergrendeld door een lopende elektronische ondertekening? (ook server-side, niet enkel de knop)
            var activeSigning = await ActiveSigningCaseAsync(model.ChangeOrder?.Id ?? 0);
            if (activeSigning is not null)
            {
                AddMessage("error", SigningLockedMessage, "Niet bewaard");
                return RedirectToAction("Dossier", "SigningAdmin", new { id = activeSigning.CaseId });
            }

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
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht bewerken");
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
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Wijzigingsopdracht bewerken");
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
        public async Task<ActionResult> DeleteChangeOrder(int id)
        {
            if (id == 0)
                return Json(new { success = false, message = "Ongeldig ID." });

            if (await ActiveSigningCaseAsync(id) is not null)
            {
                AddMessage("error", SigningLockedMessage, "Vergrendeld");
                return Json(new { success = false, message = SigningLockedMessage });
            }
            if (await CompletedSigningCaseAsync(id) is not null)
            {
                AddMessage("error", SigningCompletedLockedMessage, "Vergrendeld");
                return Json(new { success = false, message = SigningCompletedLockedMessage });
            }

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
        /// <summary>
        /// De wijzigingsopdracht als PDF. Sinds signing fase 1 via QuestPDF (Documents/ChangeOrderDocument,
        /// geladen door ChangeOrderPdfBuilder) i.p.v. de Rotativa-view ChangeOrderPDF.cshtml — hetzelfde
        /// bestand dat een klant elektronisch ondertekent, zodat papier en dossier nooit verschillen.
        /// De oude view blijft voorlopig staan als referentie; ze wordt nergens meer gerenderd.
        /// Sinds de gl-v2-documentlayout (DOCUMENTLAYOUT_VOORTGANG.md) kiest <c>ChangeOrderPdfBuilder.Render</c>
        /// hier op <c>ViewData["UseGlV2Layout"]</c> tussen die legacy opmaak en <c>ChangeOrderDocumentV2</c>;
        /// de ondertekenflow (<c>ChangeOrderSigningSource</c>) blijft altijd de legacy opmaak gebruiken.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeOrderPDF(int changeorderid)
        {
            var builder = HttpContext.RequestServices.GetRequiredService<CPMCore.Services.Signing.ChangeOrderPdfBuilder>();
            var model = await builder.LoadAsync(changeorderid, HttpContext.RequestAborted);
            if (model is null) return NotFound();

            byte[] pdfBytes;
            try
            {
                pdfBytes = builder.Render(model, useGlV2Layout: ViewData["UseGlV2Layout"] as bool? == true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Wijzigingsopdracht-PDF genereren mislukt voor {ChangeOrderId}", changeorderid);
                return StatusCode(500, "De wijzigingsopdracht kon niet worden opgemaakt: " + ex.Message);
            }

            return File(pdfBytes, "application/pdf", CPMCore.Services.Signing.ChangeOrderPdfBuilder.FileName(model));
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

    }
}
