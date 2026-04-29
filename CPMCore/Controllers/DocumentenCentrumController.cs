using BOCore;
using CPMCore.Services.InvoiceEnrichment;
using CPMCore.Services.Security;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    [Authorize]
    public class DocumentenCentrumController : BaseController
    {
        private readonly IIncomingInvoiceService _incomingInvoiceService;
        private readonly IIssuerCompanyService _issuerCompanyService;
        private readonly IPermissionService _permissionService;
        private readonly IInvoiceEnrichmentPipelineService _enrichmentPipeline;
        private readonly cpmRunningContext _db;

        public DocumentenCentrumController(
            IIncomingInvoiceService incomingInvoiceService,
            IIssuerCompanyService issuerCompanyService,
            IPermissionService permissionService,
            IInvoiceEnrichmentPipelineService enrichmentPipeline,
            cpmRunningContext db)
        {
            _incomingInvoiceService = incomingInvoiceService;
            _issuerCompanyService = issuerCompanyService;
            _permissionService = permissionService;
            _enrichmentPipeline = enrichmentPipeline;
            _db = db;
        }

        // ─── Index ────────────────────────────────────────────────────────────────

        [Breadcrumb("Documentencentrum", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index(
            int? issuerCompanyId,
            string supplierName,
            string invoiceNumber,
            byte? statusId,
            string documentType,
            int? projectId,
            bool? hasWarnings,
            DateOnly? dateFrom,
            DateOnly? dateTo,
            int page = 1,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasRead(PermissionCodes.DocumentCenter) &&
                !_permissionService.HasRead(PermissionCodes.DocumentCenterByBillingCompany))
            {
                return Forbid();
            }

            var issuerCompanies = (await _issuerCompanyService.GetAllAsync())
                .OrderBy(i => i.Name)
                .ToList();

            var filter = new IncomingInvoiceFilterVm
            {
                IssuerCompanyId = issuerCompanyId,
                SupplierName = supplierName,
                InvoiceNumber = invoiceNumber,
                StatusId = statusId,
                DocumentType = documentType,
                ProjectId = projectId,
                HasWarnings = hasWarnings,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = 50
            };

            var result = await _incomingInvoiceService.GetPagedAsync(filter, ct);

            // ── Statuscounts voor de summary-kaarten (ongefilterd op bedrijf) ──────
            var baseQuery = _db.IncommingInvoices
                .Where(i => i.IssuerCompanyId != null);
            if (issuerCompanyId.HasValue)
                baseQuery = baseQuery.Where(i => i.IssuerCompanyId == issuerCompanyId.Value);

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ViewBag.CountNew          = await baseQuery.CountAsync(i => i.StatusId == IncomingInvoiceStatus.New, ct);
            ViewBag.CountAttention    = await baseQuery.CountAsync(i => i.Warnings.Any(w => !w.IsResolved), ct);
            ViewBag.CountPending      = await baseQuery.CountAsync(i => i.StatusId == IncomingInvoiceStatus.PendingApproval, ct);
            ViewBag.CountApproved     = await baseQuery.CountAsync(i => i.StatusId == IncomingInvoiceStatus.Approved
                                                                      && i.UpdatedAt >= firstOfMonth, ct);

            // ── Projecten voor filter-dropdown ─────────────────────────────────────
            var projectQuery = _db.Project
                .Where(p => p.ProjectName != null);
            if (issuerCompanyId.HasValue)
                projectQuery = projectQuery.Where(p => p.IssuerCompanyIdBuilder == issuerCompanyId.Value);
            ViewBag.Projects = new SelectList(
                await projectQuery.OrderBy(p => p.ProjectName)
                    .Select(p => new { p.ProjectId, p.ProjectName })
                    .ToListAsync(ct),
                "ProjectId", "ProjectName", projectId);

            ViewBag.IssuerCompanies      = new SelectList(issuerCompanies, "Id", "Name", issuerCompanyId);
            ViewBag.StatusOptions        = BuildStatusSelectList(statusId);
            ViewBag.DocumentTypeOptions  = BuildDocumentTypeSelectList(documentType);
            ViewBag.Filter               = filter;
            ViewBag.SelectedIssuerCompanyId = issuerCompanyId;

            return View(result);
        }

        // ─── Detail ───────────────────────────────────────────────────────────────

        [Breadcrumb("Detail", FromController = typeof(DocumentenCentrumController), FromAction = nameof(Index))]
        public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasRead(PermissionCodes.DocumentCenter) &&
                !_permissionService.HasRead(PermissionCodes.DocumentCenterByBillingCompany))
            {
                return Forbid();
            }

            var vm = await _incomingInvoiceService.GetByIdAsync(id, ct);
            if (vm == null)
                return NotFound();

            // Laad projecten gefilterd op bouwheer (= issuer company van de factuur)
            // Harde regel: enkel projecten waarbij het facturatiebedrijf bouwheer is.
            var projectQuery = _db.Project
                .Where(p => p.ProjectName != null
                         && p.IssuerCompanyIdBuilder == vm.IssuerCompanyId)
                .OrderBy(p => p.ProjectName);

            ViewBag.Projects = await projectQuery
                .Select(p => new SelectListItem { Value = p.ProjectId.ToString(), Text = p.ProjectName })
                .ToListAsync(ct);

            // Laad contracten voor het gekoppelde project (of het voorgestelde project)
            var resolvedProjectId = vm.ProjectId
                ?? vm.Enrichment?.SuggestedProject?.ProjectId
                ?? vm.Enrichment?.UserConfirmedProjectId;

            if (resolvedProjectId.HasValue)
            {
                ViewBag.Contracts = await _db.Contract
                    .Where(c => c.ProjectId == resolvedProjectId.Value)
                    .OrderBy(c => c.ContractName)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ContractName ?? $"Contract #{c.Id}" })
                    .ToListAsync(ct);
            }
            else
            {
                ViewBag.Contracts = new System.Collections.Generic.List<SelectListItem>();
            }

            return View(vm);
        }

        // ─── Koppelen aan project (AJAX POST) ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkToProject(
            int incomingInvoiceId,
            int? projectId,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _incomingInvoiceService.LinkToProjectAsync(new IncomingInvoiceLinkProjectRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    ProjectId = projectId
                }, ct);

                AddMessage("success", "Document gekoppeld aan project.", "Opgeslagen");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Status wijzigen (AJAX POST) ──────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int incomingInvoiceId,
            byte statusId,
            string notes,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _incomingInvoiceService.UpdateStatusAsync(new IncomingInvoiceUpdateStatusRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    StatusId = statusId,
                    Notes = notes
                }, ct);

                AddMessage("success", $"Status gewijzigd naar '{IncomingInvoiceStatus.Label(statusId)}'.", "Status bijgewerkt");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Sync vanuit Octopus ──────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync(int issuerCompanyId, DateOnly? dateFrom, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter) &&
                !_permissionService.HasWrite(PermissionCodes.DocumentCenterByBillingCompany))
            {
                return Forbid();
            }

            DateTime? modifiedSinceOverride = dateFrom.HasValue
                ? new DateTime(dateFrom.Value.Year, dateFrom.Value.Month, dateFrom.Value.Day, 0, 0, 0, DateTimeKind.Utc)
                : null;

            var syncResult = await _incomingInvoiceService.SyncFromOctopusAsync(issuerCompanyId, modifiedSinceOverride, ct);

            if (syncResult.Success)
            {
                var msg = $"Sync voltooid: {syncResult.NewCount} nieuw, {syncResult.UpdatedCount} bijgewerkt, " +
                          $"{syncResult.SkippedDuplicates} overgeslagen, {syncResult.AttachmentCount} bijlagen opgehaald.";
                if (syncResult.AttachmentErrorCount > 0)
                    msg += $" ({syncResult.AttachmentErrorCount} bijlagen mislukt — zie server-log)";
                if (syncResult.ErrorCount > 0)
                    msg += $" ({syncResult.ErrorCount} factuurfouten — zie server-log)";
                AddMessage("success", msg, "Sync geslaagd");
            }
            else
            {
                AddMessage("danger", syncResult.ErrorMessage ?? "Onbekende fout bij sync.", "Sync mislukt");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        // ─── Verrijking handmatig triggeren ──────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerEnrichment(int incomingInvoiceId, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _enrichmentPipeline.EnrichAsync(incomingInvoiceId, ct);
                AddMessage("success", "Verrijkingspipeline succesvol uitgevoerd.", "Verrijkt");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Verrijking mislukt");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Verrijking bevestigen (snel voorstel overnemen) ─────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEnrichment(
            int incomingInvoiceId,
            int? projectId,
            int? contractId,
            string accountingCode,
            string notes,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _enrichmentPipeline.ConfirmEnrichmentAsync(new ConfirmEnrichmentRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    ProjectId = projectId,
                    ContractId = contractId,
                    AccountingCode = accountingCode,
                    Notes = notes,
                    ReviewedBy = User.Identity?.Name
                }, ct);

                AddMessage("success", "Voorstel bevestigd — factuur staat klaar voor goedkeuring.", "Bevestigd");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Contract koppelen ────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkToContract(
            int incomingInvoiceId,
            int? contractId,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _incomingInvoiceService.LinkToContractAsync(new IncomingInvoiceLinkContractRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    ContractId = contractId
                }, ct);

                AddMessage("success", "Contract gekoppeld.", "Opgeslagen");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── PDF-factuurlijnen overnemen ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPdfLines(int incomingInvoiceId, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _enrichmentPipeline.AcceptPdfLinesAsync(new AcceptPdfLinesRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    AcceptedBy = User.Identity?.Name
                }, ct);

                AddMessage("success", "PDF-factuurlijnen overgenomen als factuurlijnen.", "Overgenomen");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Leverancierregel opslaan ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LearnSupplierRule(
            int incomingInvoiceId,
            int? issuerCompanyId,
            int? companyId,
            string supplierVatNumber,
            string ruleType,
            int? defaultProjectId,
            string defaultAccountingCode,
            bool isGeneralCost,
            bool requiresManualReview,
            bool requiresProject,
            string notes,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _enrichmentPipeline.LearnSupplierRuleAsync(new LearnSupplierRuleRequest
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    IssuerCompanyId = issuerCompanyId,
                    CompanyId = companyId,
                    SupplierVatNumber = supplierVatNumber,
                    RuleType = ruleType,
                    DefaultProjectId = defaultProjectId,
                    DefaultAccountingCode = defaultAccountingCode,
                    IsGeneralCost = isGeneralCost,
                    RequiresManualReview = requiresManualReview,
                    RequiresProject = requiresProject,
                    Notes = notes,
                    CreatedBy = User.Identity?.Name
                }, ct);

                AddMessage("success", "Leverancierregel opgeslagen.", "Onthouden");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Projectalias opslaan ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LearnProjectAlias(
            int incomingInvoiceId,
            int projectId,
            string alias,
            string aliasType,
            CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.DocumentCenter))
                return Forbid();

            try
            {
                await _enrichmentPipeline.LearnProjectAliasAsync(new LearnProjectAliasRequest
                {
                    ProjectId = projectId,
                    Alias = alias,
                    AliasType = aliasType ?? "Name",
                    CreatedBy = User.Identity?.Name
                }, ct);

                AddMessage("success", $"Alias '{alias}' opgeslagen voor project.", "Onthouden");
            }
            catch (Exception ex)
            {
                AddMessage("danger", ex.Message, "Fout");
            }

            return RedirectToAction(nameof(Detail), new { id = incomingInvoiceId });
        }

        // ─── Bijlage downloaden ───────────────────────────────────────────────────

        public async Task<IActionResult> DownloadAttachment(int attachmentId, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasRead(PermissionCodes.DocumentCenter) &&
                !_permissionService.HasRead(PermissionCodes.DocumentCenterByBillingCompany))
            {
                return Forbid();
            }

            var attachment = await _db.IncomingInvoiceAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment == null || attachment.Content == null)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

            return File(attachment.Content, contentType, attachment.FileName);
        }

        // ─── Bijlage inline weergeven (voor iframe/preview) ──────────────────────

        public async Task<IActionResult> ViewAttachment(int attachmentId, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasRead(PermissionCodes.DocumentCenter) &&
                !_permissionService.HasRead(PermissionCodes.DocumentCenterByBillingCompany))
            {
                return Forbid();
            }

            var attachment = await _db.IncomingInvoiceAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment == null || attachment.Content == null)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

            // Geen bestandsnaam meegeven → browser gebruikt Content-Disposition: inline
            return File(attachment.Content, contentType);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static SelectList BuildStatusSelectList(byte? selected)
        {
            var items = new[]
            {
                new { Value = "", Text = "Alle statussen" },
                new { Value = IncomingInvoiceStatus.New.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.New) },
                new { Value = IncomingInvoiceStatus.Enriched.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Enriched) },
                new { Value = IncomingInvoiceStatus.NeedsReview.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.NeedsReview) },
                new { Value = IncomingInvoiceStatus.PendingApproval.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.PendingApproval) },
                new { Value = IncomingInvoiceStatus.Approved.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Approved) },
                new { Value = IncomingInvoiceStatus.Rejected.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Rejected) },
                new { Value = IncomingInvoiceStatus.Booked.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Booked) },
                new { Value = IncomingInvoiceStatus.Paid.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Paid) },
                new { Value = IncomingInvoiceStatus.ManualProcessing.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.ManualProcessing) },
                new { Value = IncomingInvoiceStatus.Duplicate.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Duplicate) },
            };
            return new SelectList(items, "Value", "Text", selected?.ToString());
        }

        private static SelectList BuildDocumentTypeSelectList(string selected)
        {
            var items = new[]
            {
                new { Value = "", Text = "Alle types" },
                new { Value = "Invoice", Text = "Factuur" },
                new { Value = "CreditNote", Text = "Creditnota" },
                new { Value = "Other", Text = "Overige" },
            };
            return new SelectList(items, "Value", "Text", selected);
        }
    }
}
