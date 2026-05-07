using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    // ─── ViewModels / DTOs ────────────────────────────────────────────────────

    public class IncomingInvoiceFilterVm
    {
        public int? IssuerCompanyId { get; set; }
        public string SupplierName { get; set; }
        public string InvoiceNumber { get; set; }
        public byte? StatusId { get; set; }
        public string DocumentType { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public int? ProjectId { get; set; }
        /// <summary>Wanneer true: enkel documenten met onopgeloste waarschuwingen.</summary>
        public bool? HasWarnings { get; set; }
        /// <summary>Filter op kostcontext (BOCore.CostContextType constante).</summary>
        public string CostContextType { get; set; }
        /// <summary>Filter op toegewezen gebruiker (Users.UserId).</summary>
        public string AssignedToUserId { get; set; }
        /// <summary>Paginering — 1-based.</summary>
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class IncomingInvoiceListItemVm
    {
        public int Id { get; set; }
        public int IssuerCompanyId { get; set; }
        public string IssuerCompanyName { get; set; }
        public string SupplierName { get; set; }
        public string SupplierVatNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal TotalAmountInclVat { get; set; }
        public string CurrencyCode { get; set; }
        public byte StatusId { get; set; }
        public string StatusLabel { get; set; }
        public string StatusBadgeClass { get; set; }
        public string DocumentType { get; set; }
        public string Source { get; set; }
        /// <summary>Bevestigde projectkoppeling op de factuur zelf.</summary>
        public string ProjectName { get; set; }
        /// <summary>Voorstel van de verrijkingspipeline (nog niet bevestigd).</summary>
        public string SuggestedProjectName { get; set; }
        /// <summary>True als de verrijkingspipeline al eens gelopen heeft voor dit document.</summary>
        public bool IsEnriched { get; set; }
        /// <summary>Werkelijk toegewezen gebruiker (AssignedToName op de factuur).</summary>
        public string AssignedToName { get; set; }
        public string AssignedToUserId { get; set; }
        public bool HasAttachments { get; set; }
        /// <summary>Id van de eerste PDF-bijlage (niet UBL), null als geen PDF aanwezig.</summary>
        public int? FirstPdfAttachmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SyncedAt { get; set; }
        /// <summary>Reason-flags van de laatste verrijkingsrun (bitmask van InvoiceReasonFlags).</summary>
        public long ReasonFlags { get; set; }
        public bool HasWarnings { get; set; }

        // Cost context
        public string CostContextType { get; set; }
        public string CostContextLabel { get; set; }
        public string CostContextBadgeClass { get; set; }

        /// <summary>Korte waarschuwingsberichten voor de inbox-rij.</summary>
        public IReadOnlyList<string> WarningChips { get; set; } = Array.Empty<string>();

        // Contract-budgetdata (batch geladen na pagina-query)
        public int? ContractId { get; set; }
        public string ContractName { get; set; }
        public decimal? ContractTotalAmount { get; set; }
        /// <summary>Som van alle andere facturen op hetzelfde contract (excl. deze factuur).</summary>
        public decimal? ContractInvoicedBefore { get; set; }
    }

    public class IncomingInvoicePagedResultVm
    {
        public IReadOnlyList<IncomingInvoiceListItemVm> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    public class IncomingInvoiceDetailVm
    {
        public int Id { get; set; }
        public int IssuerCompanyId { get; set; }
        public string IssuerCompanyName { get; set; }
        public string SupplierName { get; set; }
        public string SupplierVatNumber { get; set; }
        public int? SupplierCompanyInfoId { get; set; }
        public int? OctopusSupplierRelationKey { get; set; }
        public string InvoiceNumber { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal TotalAmountInclVat { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? NetAmount { get; set; }
        public string CurrencyCode { get; set; }
        public byte StatusId { get; set; }
        public string StatusLabel { get; set; }
        public string StatusBadgeClass { get; set; }
        public string DocumentType { get; set; }
        public string Source { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ContractId { get; set; }
        public string Notes { get; set; }
        public string OctopusExternalId { get; set; }
        public DateTime? SyncedAt { get; set; }
        public string SyncError { get; set; }

        // Cost context
        public string CostContextType { get; set; }
        public string AssignedToUserId { get; set; }
        public string AssignedToName { get; set; }

        // Octopus booking-info (fase 2)
        public int? OctopusBookyearId { get; set; }
        public string OctopusJournalKey { get; set; }
        public int? OctopusDocumentSequenceNr { get; set; }
        public DateTime? OctopusBookedAt { get; set; }
        public string OctopusBookedBy { get; set; }
        public string OctopusBookingStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public IReadOnlyList<IncomingInvoiceAttachmentVm> Attachments { get; set; } = new List<IncomingInvoiceAttachmentVm>();
        public IReadOnlyList<IncomingInvoiceLineVm> Lines { get; set; } = new List<IncomingInvoiceLineVm>();

        /// <summary>Verrijkingsresultaat van de pipeline (null als de pipeline nog niet gedraaid heeft).</summary>
        public InvoiceEnrichmentVm Enrichment { get; set; }

        // Intelligence data (geladen in service)
        public decimal? ContractTotalAmount { get; set; }
        public decimal? ContractInvoicedTotal { get; set; }
        public int PreviousInvoicesOnProject { get; set; }
        public decimal? AverageInvoiceAmountSupplier { get; set; }
        public decimal? InvoiceDeviationPercent { get; set; }
        public int PreviousGeneralInvoicesSupplier { get; set; }
    }

    public class IncomingInvoiceLineVm
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
    }

    public class IncomingInvoiceAttachmentVm
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string AttachmentType { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class IncomingInvoiceLinkProjectRequest
    {
        public int IncomingInvoiceId { get; set; }
        public int? ProjectId { get; set; }
    }

    public class IncomingInvoiceUpdateStatusRequest
    {
        public int IncomingInvoiceId { get; set; }
        public byte StatusId { get; set; }
        public string Notes { get; set; }
    }

    public class IncomingInvoiceSyncResultVm
    {
        public bool Success { get; set; }
        public int NewCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedDuplicates { get; set; }
        public int ErrorCount { get; set; }
        public int AttachmentCount { get; set; }
        public int AttachmentErrorCount { get; set; }
        public int EnrichedCount { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }

    // ─── Service interface ────────────────────────────────────────────────────

    public class IncomingInvoiceLinkContractRequest
    {
        public int IncomingInvoiceId { get; set; }
        public int? ContractId { get; set; }
    }

    public class BulkActionRequest
    {
        public IReadOnlyList<int> Ids { get; set; } = Array.Empty<int>();
        public byte StatusId { get; set; }
        public string Notes { get; set; }
    }

    public class BulkActionResult
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }

    public class AssignToUserRequest
    {
        public int IncomingInvoiceId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    public class UploadManualRequest
    {
        public int IssuerCompanyId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierVatNumber { get; set; }
        public string DocumentType { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal TotalAmountInclVat { get; set; }
        public string Notes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] FileContent { get; set; }
        public string UploadedBy { get; set; }
    }

    public class SetCostContextRequest
    {
        public int IncomingInvoiceId { get; set; }
        public string CostContextType { get; set; }
    }

    public interface IIncomingInvoiceService
    {
        Task<IncomingInvoicePagedResultVm> GetPagedAsync(IncomingInvoiceFilterVm filter, CancellationToken ct = default);

        Task<IncomingInvoiceDetailVm> GetByIdAsync(int id, CancellationToken ct = default);

        Task LinkToProjectAsync(IncomingInvoiceLinkProjectRequest request, CancellationToken ct = default);

        Task LinkToContractAsync(IncomingInvoiceLinkContractRequest request, CancellationToken ct = default);

        Task UpdateStatusAsync(IncomingInvoiceUpdateStatusRequest request, CancellationToken ct = default);

        Task<byte[]> GetAttachmentContentAsync(int attachmentId, CancellationToken ct = default);

        Task<IncomingInvoiceSyncResultVm> SyncFromOctopusAsync(int issuerCompanyId, DateTime? modifiedSinceOverride = null, CancellationToken ct = default);

        Task<BulkActionResult> BulkUpdateStatusAsync(BulkActionRequest request, CancellationToken ct = default);

        Task AssignToUserAsync(AssignToUserRequest request, CancellationToken ct = default);

        Task<IncomingInvoiceDetailVm> UploadManualAsync(UploadManualRequest request, CancellationToken ct = default);

        Task SetCostContextAsync(SetCostContextRequest request, CancellationToken ct = default);
    }
}
