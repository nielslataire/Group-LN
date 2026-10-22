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
using CPMCore.Models.GlV2;
using CPMCore.Services.Verzekeringen;

namespace CPMCore.Controllers
{
    /// <summary>Verzekeringen van een project. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Insurances/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL INSURANCES ==========

        [HttpGet]
        //[Breadcrumb("Verzekeringen")]
        [Breadcrumb("Verzekeringen", FromAction = "Detail")]
        public async Task<IActionResult> DetailInsurances(int projectid, string tab = "polissen")
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

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Verzekeringen");
            if (ViewData["UseGlV2Layout"] as bool? == true)
            {
                var vm = await VerzekeringSvc.GetOverzichtAsync(projectid, tab, (bool)ViewBag.CanWriteProjectInsurances, (bool)ViewBag.CanDeleteProjectInsurances);
                ViewData["HelpScreenId"] = vm.Tab == "attesten" ? "verzekeringen-attesten" : "verzekeringen-overzicht";
                return View("DetailInsurancesV2", vm);
            }
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

                    // Nieuwe verzekering: maak de ContractActivity (142) aan op het contract van de
                    // gekozen makelaar; de verzekering hangt daar 1-op-1 aan vast.
                    if (viewmodel.Insurance.Id == 0 && viewmodel.Insurance.ContractActivityID == 0)
                    {
                        if (viewmodel.SelectedBrokerId <= 0)
                        {
                            response.AddError("Kies een makelaar.");
                        }
                        else
                        {
                            var caId = _projectService.CreateInsuranceContractActivity(viewmodel.ProjectId, viewmodel.SelectedBrokerId);
                            if (caId <= 0)
                                response.AddError("De verzekeringsactiviteit kon niet aangemaakt worden.");
                            else
                                viewmodel.Insurance.ContractActivityID = caId;
                        }
                    }

                    if (response.Success)
                    {
                        var service = _insuranceService;
                        response = service.InsertUpdate(viewmodel.Insurance);
                    }
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
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsInsurances)]
        public IActionResult DeleteInsurance(int id, int projectId)
        {
            var response = _insuranceService.Delete(id);
            AddMessage(response.Success ? "success" : "error",
                response.Success ? "De verzekering is verwijderd." : "De verzekering kon niet verwijderd worden.",
                response.Success ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = projectId });
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
        public IActionResult ModalEditInsurance(int id, int projectid = 0)
        {
            var viewModel = new ProjectAddInsurancesModel();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel.Insurance = dservice.GetInsuranceById(id).Value;
                // zeker dat ProjectId gezet is
                viewModel.ProjectId = viewModel.Insurance?.ProjectID ?? viewModel.ProjectId;
            }
            else
            {
                // Nieuwe verzekering: project komt via de route mee (staat niet in een bestaand record).
                viewModel.ProjectId = projectid;
            }

            var service = _insuranceService;
            var cservice = _companyService;

            var cresponse = cservice.GetCompanyForSelectByActivity(142);
            if (cresponse.Success) viewModel.Brokers = cresponse.Values;

            var response = service.GetInsuranceCompaniesForSelect();
            if (response.Success) viewModel.Companies = response.Values;

            // Zelfde partial als Add
            return PartialView("_ModalEditInsurance", viewModel);
        }

        // ========== gl-v2 (design-handoff 43): verzekering toevoegen/bewerken, beëindigen, attesten van aannemers ==========

        private VerzekeringService VerzekeringSvc => HttpContext.RequestServices.GetRequiredService<VerzekeringService>();

        private async Task<VzPolisFormVm> BouwPolisFormAsync(int projectid, int? id, VzPolisInput? prefill)
        {
            var brokers = _companyService.GetCompanyForSelectByActivity(142);
            var companies = _insuranceService.GetInsuranceCompaniesForSelect();
            return await VerzekeringSvc.GetFormAsync(projectid, id, prefill, brokers.Success ? brokers.Values : new List<IdNameBO>(), companies.Success ? companies.Values : new List<IdNameBO>());
        }

        private void ZetPolisFormPagina(VzPolisFormVm vm)
        {
            var projectid = vm.Input.ProjectId;
            vm.LostOp = VerzekeringService.LostOp(vm.Input.Type, vm.Input.Building);
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid } };
            var lijst = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailInsurances", "Projecten", "Verzekeringen") { Parent = projectDetail, RouteValues = new { projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddInsurance", "Projecten", vm.IsNew ? "Verzekering toevoegen" : "Verzekering bewerken") { Parent = lijst };
            SetPageHeader("bx bx-building-house", $"{vm.ProjectName} - " + (vm.IsNew ? "Verzekering toevoegen" : "Verzekering bewerken"));
            ViewData["HelpScreenId"] = "verzekering-toevoegen";
        }

        [HttpGet]
        [Breadcrumb("Verzekering toevoegen", FromAction = "DetailInsurances")]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> AddInsurance(int projectid, int? id, int type = 1, int? building = null, int? unit = null, string? from = null)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var prefill = new VzPolisInput { ProjectId = projectid, Type = type is >= 1 and <= 5 ? type : 1, Building = building, UnitId = unit, UnitIds = unit.HasValue ? new List<int> { unit.Value } : null, From = from };
            // Een brandverzekering voor nieuwbouw start standaard bij de voorlopige oplevering.
            if (prefill.Type == 2 && building != 0) { prefill.Building = 1; prefill.StartAtDelivery = true; }
            var vm = await BouwPolisFormAsync(projectid, id, prefill);
            ZetPolisFormPagina(vm);
            return View("AddInsuranceV2", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> SaveInsurance(VzPolisInput input, IFormFile? polisFile)
        {
            int? documentId = null;
            if (polisFile is { Length: > 0 })
                documentId = await BewaarVerzekeringDocumentAsync(input.ProjectId, polisFile, $"Polis {VerzekeringService.TypeNaam(input.Type)}" + (string.IsNullOrWhiteSpace(input.Polisnummer) ? "" : " " + input.Polisnummer.Trim()));
            var (ok, fout, newId) = await VerzekeringSvc.SavePolisAsync(input, documentId);
            if (!ok)
            {
                var vm = await BouwPolisFormAsync(input.ProjectId, input.Id > 0 ? input.Id : null, input);
                vm.Input = input;
                vm.Fout = fout;
                ZetPolisFormPagina(vm);
                return View("AddInsuranceV2", vm);
            }
            AddMessage("success", input.Requested && string.IsNullOrWhiteSpace(input.Polisnummer) ? "De verzekering is bewaard als aangevraagd." : "De verzekering is opgeslagen.", "Geslaagd!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = input.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> EndInsuranceV2(int projectid, int id, DateOnly? enddate, string? reason)
        {
            var ok = await VerzekeringSvc.BeeindigAsync(projectid, id, enddate ?? DateOnly.FromDateTime(DateTime.Today), reason);
            AddMessage(ok ? "success" : "error", ok ? "De verzekering is beëindigd." : "De verzekering kon niet beëindigd worden.", ok ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsInsurances)]
        public IActionResult DeleteInsuranceV2(int projectid, int id)
        {
            var response = _insuranceService.Delete(id);
            AddMessage(response.Success ? "success" : "error", response.Success ? "De verzekering is verwijderd." : "De verzekering kon niet verwijderd worden.", response.Success ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> InsuranceAttestSave(int projectid, int contractId, int kind, int status, List<int>? unitIds, DateOnly? validUntil,
            string? insurer, string? policyNumber, string? note, IFormFile? attestFile)
        {
            int? documentId = null;
            if (attestFile is { Length: > 0 })
                documentId = await BewaarVerzekeringDocumentAsync(projectid, attestFile, kind == VerzekeringService.AttestBa ? "Attest BA-verzekering" : "Attest tienjarige aansprakelijkheid");
            var (ok, fout, gekoppeld) = await VerzekeringSvc.SaveAttestAsync(projectid, contractId, kind, status, unitIds, validUntil, insurer, policyNumber, note, documentId);
            var tekst = ok
                ? (gekoppeld > 0 ? $"Het attest is bijgewerkt en aan {gekoppeld} koppeling{(gekoppeld == 1 ? "" : "en")} in Documenten gehangen." : "Het attest is bijgewerkt.")
                : (fout ?? "Het attest kon niet bewaard worden.");
            AddMessage(ok ? "success" : "error", tekst, ok ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid, tab = "attesten" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> InsuranceTienjarigeSave(int projectid, int? bron, string? opmerking)
        {
            var ok = await VerzekeringSvc.SaveProjectTienjarigeAsync(projectid, bron, opmerking);
            AddMessage(ok ? "success" : "error", ok ? "De keuze voor de tienjarige is bewaard." : "De keuze kon niet bewaard worden.", ok ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid, tab = "attesten" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> InsuranceAttestKoppelKopers(int projectid)
        {
            var n = await VerzekeringSvc.KoppelKopersAsync(projectid);
            AddMessage("success", n > 0 ? $"De tienjarige attesten zijn gekoppeld aan de kopers ({n} koppeling{(n == 1 ? "" : "en")}) en gedeeld in het klantenportaal." : "Alle attesten waren al aan hun kopers gekoppeld.", "Geslaagd!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> InsuranceAttestMails(int projectid, List<int> contractIds)
        {
            var mails = await VerzekeringSvc.BouwMailsAsync(projectid, contractIds ?? new List<int>(), DocUserName ?? "");
            return Json(mails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsInsurances)]
        public async Task<IActionResult> InsuranceAttestMarkRequested(int projectid, int contractId)
        {
            var n = await VerzekeringSvc.MarkeerOpgevraagdAsync(projectid, new[] { contractId }, DocUserName);
            return Json(new { ok = true, aantal = n });
        }

        /// <summary>Bewaart een polis of attest in de documentenmodule (map Verzekeringen, of Overige als die map nog niet bestaat).</summary>
        private async Task<int?> BewaarVerzekeringDocumentAsync(int projectId, IFormFile file, string naam)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var stored = await UploadAssetToStorageAsync(stream, file.FileName, file.ContentType, "docs");
                if (string.IsNullOrWhiteSpace(stored)) return null;
                try { await GenerateDocThumbnailViaStorageAsync(stored); }
                catch (Exception ex) { _logger.LogWarning(ex, "Thumbnail voor {File} kon niet gemaakt worden.", stored); }
                var map = await _db.DocumentFolders.AsNoTracking().Where(f => f.Code == "verzekeringen").Select(f => (int?)f.Id).FirstOrDefaultAsync();
                var up = await DocsSvc.Upload(new FacadeCore.DocUploadDto
                {
                    ProjectId = projectId, Mode = "new", Name = naam, FolderId = map,
                    DocDate = DateOnly.FromDateTime(DateTime.Today), UploaderKind = DocumentUploaderKind.Intern,
                    StoredFilename = stored, OriginalFilename = file.FileName, SizeBytes = file.Length,
                    Status = DocumentStatus.Goedgekeurd, UserId = DocUserId, UserName = DocUserName
                });
                return up.Ok ? up.Id : null;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Document van een verzekering kon niet in Documenten bewaard worden."); return null; }
        }

    }
}
