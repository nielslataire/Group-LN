using BOCore;
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
        private readonly cpmRunningContext _db;

        public DocumentenCentrumController(
            IIncomingInvoiceService incomingInvoiceService,
            IIssuerCompanyService issuerCompanyService,
            IPermissionService permissionService,
            cpmRunningContext db)
        {
            _incomingInvoiceService = incomingInvoiceService;
            _issuerCompanyService = issuerCompanyService;
            _permissionService = permissionService;
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
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = 50
            };

            var result = await _incomingInvoiceService.GetPagedAsync(filter, ct);

            ViewBag.IssuerCompanies = new SelectList(issuerCompanies, "Id", "Name", issuerCompanyId);
            ViewBag.StatusOptions = BuildStatusSelectList(statusId);
            ViewBag.DocumentTypeOptions = BuildDocumentTypeSelectList(documentType);
            ViewBag.Filter = filter;
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

            // Laad projecten voor koppelingsmodal
            ViewBag.Projects = await _db.Project
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem { Value = p.ProjectId.ToString(), Text = p.ProjectName })
                .ToListAsync(ct);

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
                new { Value = IncomingInvoiceStatus.PendingApproval.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.PendingApproval) },
                new { Value = IncomingInvoiceStatus.Approved.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Approved) },
                new { Value = IncomingInvoiceStatus.Rejected.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Rejected) },
                new { Value = IncomingInvoiceStatus.Booked.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Booked) },
                new { Value = IncomingInvoiceStatus.Paid.ToString(), Text = IncomingInvoiceStatus.Label(IncomingInvoiceStatus.Paid) },
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
