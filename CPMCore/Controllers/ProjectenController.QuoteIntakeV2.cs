using BOCore;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using CPMCore.Services.QuoteExtraction;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 "Offerte inlezen" (design-handoff 20c) — het interactieve OCR-hulpmiddel: een
    /// kader rond een tabel leest regels in (Azure Document Intelligence, prebuilt-layout op een
    /// client-bijgesneden regio), een kader rond een foto hangt die aan een regel. Werkt op dezelfde
    /// ChangeOrder-rij als ChangeOrderDetailV2 (IsQuote=true) — opslaan hergebruikt bewust
    /// ChangeOrderDetailV2Save/SyncRows (geen tweede opslagpad voor dezelfde tabel).</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> QuoteIntakeV2(int projectid, int? clientid, int coid = 0)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            var vm = new QuoteIntakeV2Vm { ProjectId = projectid, ProjectName = project.Name, CanWrite = canWrite, IsNew = coid <= 0 };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid } };
            var listNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ChangeOrdersV2", "Projecten", "Offertes & wijzigingen") { Parent = projectDetail, RouteValues = new { projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(QuoteIntakeV2), "Projecten", "Offerte inlezen") { Parent = listNode, RouteValues = new { projectid, coid } };

            ChangeOrder co = null;
            if (coid > 0)
            {
                co = await _db.ChangeOrder.AsNoTracking().Include(c => c.ChangeOrderDetail).FirstOrDefaultAsync(c => c.Id == coid);
                if (co is null) return NotFound();
                vm.ChangeOrderId = co.Id;
            }

            vm.ClientAccountId = co?.ClientAccountId ?? clientid ?? 0;
            vm.ContractActivityId = co?.ContractActivityId ?? 0;
            vm.QuoteSupplierReference = co?.QuoteSupplierReference;
            vm.QuoteVatPercentage = co?.QuoteVatPercentage;
            vm.QuoteDate = co?.Date ?? DateOnly.FromDateTime(DateTime.Today);
            vm.ExpirationDate = co?.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30));

            if (vm.ClientAccountId > 0)
            {
                var clientResponse = _clientService.GetClientAccountById(vm.ClientAccountId);
                vm.ClientName = clientResponse.Success ? clientResponse.Values.FirstOrDefault()?.Name ?? "" : "";
                vm.UnitName = ResolveUnitNameForAccount(vm.ClientAccountId, projectid);
            }

            vm.ContractActivities = _projectService.GetProjectContractActivitiesForSelect(projectid) is { Success: true } actResp
                ? actResp.Values : new List<IdNameBO>();
            if (vm.ClientAccountId <= 0)
                vm.ClientAccounts = _clientService.GetClientAccountsByProjectIdForSelect(projectid) is { Success: true } clResp
                    ? clResp.Values.OrderBy(c => c.Display).ToList() : new List<IdNameBO>();

            if (co != null)
            {
                vm.Rows = co.ChangeOrderDetail.Select(d => new ChangeOrderDetailRowV2
                {
                    Id = d.Id,
                    Description = d.Description,
                    MeasurementType = d.MeasurementType ?? (int)MeasurementType.Forfait,
                    MeasurementUnit = d.MeasurementUnit ?? (int)MeasurementUnit.stuk,
                    Number = d.Number,
                    Price = d.Price,
                    NeedsReview = d.NeedsReview,
                    SourceImagePath = d.SourceImagePath,
                }).ToList();
            }

            var quoteService = HttpContext.RequestServices.GetRequiredService<IQuoteRegionAnalysisService>();
            vm.AzureConfigured = quoteService.IsEnabled;

            return View(vm);
        }

        /// <summary>"+ Regel" (leeg) of een nieuwe OCR-regel — rendert de vereenvoudigde 20c-rij
        /// (_QuoteIntakeRowV2, geen commissie/btw-kolommen zoals 20d se _ChangeOrderDetailRowV2, dat
        /// komt pas bij Omzetten aan bod). De client vult de velden na het inladen zelf in via JS
        /// (OCR-waarden) — dit endpoint levert enkel de lege rijstructuur met de juiste rows[i]-naam.</summary>
        [HttpGet]
        public IActionResult QuoteIntakeV2AddRow(int index)
        {
            ViewData["Index"] = index;
            return PartialView("Partials/_QuoteIntakeRowV2", new ChangeOrderDetailRowV2 { Number = 1 });
        }

        /// <summary>"Gebied selecteren" rond een tabel — de client stuurt de al-bijgesneden regio-
        /// afbeelding mee (geen server-side crop, de browser deed dat al via canvas), dus enkel de
        /// Azure-analyse zelf gebeurt hier.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuoteIntakeV2ExtractTable(IFormFile image)
        {
            if (image is null || image.Length == 0) return Json(new { success = false, message = "Geen afbeelding ontvangen." });

            var quoteService = HttpContext.RequestServices.GetRequiredService<IQuoteRegionAnalysisService>();
            if (!quoteService.IsEnabled)
                return Json(new { success = false, message = "Automatisch inlezen is niet geconfigureerd (geen AI-sleutel)." });

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var result = await quoteService.AnalyzeTableAsync(ms.ToArray(), HttpContext.RequestAborted);

            if (!result.Success)
                return Json(new { success = false, message = result.ErrorMessage ?? "Kon de tabel niet lezen." });

            return Json(new
            {
                success = true,
                lines = result.Lines.Select(l => new
                {
                    description = l.Description,
                    unit = l.Unit,
                    number = l.Number,
                    price = l.Price,
                    needsReview = l.NeedsReview,
                }),
            });
        }

        /// <summary>"Gebied selecteren" rond een foto — hangt als bijlage aan een regel (SourceImagePath).
        /// Opgeslagen via dezelfde DocStorageService als de rest van het project (map "quotes"), teruggeven
        /// als opslagnaam (voor het verborgen veld) + tijdelijke leesurl (voor het miniatuurtje).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuoteIntakeV2AttachPhoto(IFormFile image)
        {
            if (image is null || image.Length == 0) return Json(new { success = false, message = "Geen afbeelding ontvangen." });

            var storage = HttpContext.RequestServices.GetRequiredService<DocStorageService>();
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var stored = await storage.UploadAsync(ms.ToArray(), image.FileName ?? "foto.png", image.ContentType ?? "image/png", "quotes");
            if (stored is null) return Json(new { success = false, message = "Opslaan van de foto is mislukt." });

            var thumbUrl = await storage.GetSignedUrlAsync(stored, "quotes");
            return Json(new { success = true, path = stored, thumbUrl });
        }
    }
}
