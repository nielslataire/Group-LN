using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    // ─── Waardemodel per veld met bronvermelding ──────────────────────────────

    public class EnrichedFieldValue<T>
    {
        public T Value { get; set; }

        /// <summary>Ubl / PdfText / AzureOcr / RuleEngine / User / Octopus</summary>
        public string Source { get; set; }

        /// <summary>High / Medium / Low</summary>
        public string Confidence { get; set; }

        public bool NeedsReview { get; set; }

        /// <summary>Conflictbeschrijving als UBL en PDF afwijken.</summary>
        public string ConflictNote { get; set; }
    }

    // ─── Projectmatch ─────────────────────────────────────────────────────────

    public class ProjectMatchResult
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int Score { get; set; }

        /// <summary>High / Medium / Low</summary>
        public string ConfidenceLevel { get; set; }

        public string MatchReason { get; set; }

        /// <summary>Welke velden gematcht: Name / Street / City / Reference / Alias / Other</summary>
        public string MatchedFields { get; set; }

        /// <summary>Ubl / PdfText / AzureOcr / Combined</summary>
        public string Source { get; set; }

        public string Street { get; set; }
        public string City { get; set; }

        /// <summary>StatusId uit ProjectStatusType (1=Opgeleverd, 2=Uitvoering, …)</summary>
        public int? ProjectStatusId { get; set; }

        /// <summary>Leesbare statuslabel, bijv. "Opgeleverd" of "Uitvoering"</summary>
        public string ProjectStatusLabel { get; set; }

        /// <summary>True als het project niet actief is (Opgeleverd of Stopgezet).</summary>
        public bool IsInactiveProject { get; set; }

        /// <summary>True als het project voldoet aan alle businessfilters (bouwheer, …).</summary>
        public bool AllowedByFilter { get; set; } = true;

        /// <summary>True als het project geblokkeerd is door een businessregel maar inhoudelijk sterk matcht.</summary>
        public bool BlockedByBusinessRule { get; set; }

        /// <summary>MissingIssuerCompanyRelation / DifferentIssuerCompany</summary>
        public string BlockReason { get; set; }
    }

    // ─── Contractmatch ───────────────────────────────────────────────────────

    public class ContractMatchResult
    {
        public int ContractId { get; set; }
        public string ContractName { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int Score { get; set; }
        public string ConfidenceLevel { get; set; }
        public string MatchReason { get; set; }
        public string SupplierName { get; set; }
    }

    // ─── Kostenrekening ───────────────────────────────────────────────────────

    public class AccountingSuggestion
    {
        public string AccountingCode { get; set; }
        public string Description { get; set; }

        /// <summary>SupplierRule / ProjectType / Description / Fallback</summary>
        public string Source { get; set; }

        public bool IsGeneralCost { get; set; }
        public string Confidence { get; set; }
    }

    // ─── Leverancier-matching ─────────────────────────────────────────────────

    public class SupplierMatchInfo
    {
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string VatNumber { get; set; }
        public string Iban { get; set; }
        public string Confidence { get; set; }
        public string MatchReason { get; set; }
        public bool RequiresManualReview { get; set; }
        public bool IsAlwaysGeneralCost { get; set; }
    }

    // ─── Waarschuwing ────────────────────────────────────────────────────────

    public class InvoiceWarningVm
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public string Source { get; set; }
        public bool IsResolved { get; set; }
    }

    // ─── Voorgestelde PDF-factuurregel ───────────────────────────────────────

    public class ProposedInvoiceLine
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? LineTotal { get; set; }

        /// <summary>PdfText / PdfTable / AzureOcr</summary>
        public string Source { get; set; }

        /// <summary>High / Medium / Low</summary>
        public string Confidence { get; set; }
    }

    // ─── Weergavelijn (UBL of PDF fallback, uniforme lijst) ───────────────────

    public class DisplayInvoiceLine
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? LineTotal { get; set; }

        /// <summary>UBL / PDF</summary>
        public string Source { get; set; }
    }

    // ─── Volledig verrijkingsresultaat voor de UI ─────────────────────────────

    public class InvoiceEnrichmentVm
    {
        public int IncomingInvoiceId { get; set; }
        public DateTime? EnrichedAt { get; set; }

        // Documentclassificatie
        public string DocumentClassification { get; set; }

        // Leverancier
        public SupplierMatchInfo SupplierMatch { get; set; }

        // Projectvoorstel
        public ProjectMatchResult SuggestedProject { get; set; }
        public IReadOnlyList<ProjectMatchResult> AlternativeProjects { get; set; } = new List<ProjectMatchResult>();

        // Contractvoorstel
        public ContractMatchResult SuggestedContract { get; set; }
        public IReadOnlyList<ContractMatchResult> AlternativeContracts { get; set; } = new List<ContractMatchResult>();

        // Kostenrekening
        public AccountingSuggestion AccountingSuggestion { get; set; }

        // Reason flags (bitmask van InvoiceReasonFlags)
        public long ReasonFlags { get; set; }

        // Duplicaatcontrole
        public bool IsDuplicateSuspected { get; set; }
        public int? DuplicateSuspectInvoiceId { get; set; }

        // Waarschuwingen
        public IReadOnlyList<InvoiceWarningVm> Warnings { get; set; } = new List<InvoiceWarningVm>();

        // Gebruikersbeslissingen
        public DateTime? UserReviewedAt { get; set; }
        public string UserReviewedBy { get; set; }
        public int? UserConfirmedProjectId { get; set; }
        public int? UserConfirmedContractId { get; set; }
        public string UserConfirmedAccountingCode { get; set; }
        public string UserNotes { get; set; }

        // Verrijkte veldwaarden (per veld bron + confidence)
        public EnrichedFieldValue<string> InvoiceNumber { get; set; }
        public EnrichedFieldValue<DateOnly?> DueDate { get; set; }
        public EnrichedFieldValue<string> DocumentType { get; set; }

        // PDF-factuurlijnen (voorstel wanneer UBL-lijnen onvoldoende zijn)
        public bool HasWeakUblLines { get; set; }
        public IReadOnlyList<ProposedInvoiceLine> ProposedPdfLines { get; set; } = new List<ProposedInvoiceLine>();

        // Weergavelijn: uniforme lijst (UBL als bruikbaar, anders PDF-voorstel)
        public IReadOnlyList<DisplayInvoiceLine> DisplayLines { get; set; } = new List<DisplayInvoiceLine>();

        /// <summary>True als DisplayLines gevuld zijn vanuit PDF (niet UBL).</summary>
        public bool DisplayLinesFromPdf { get; set; }
    }

    // ─── Requests ─────────────────────────────────────────────────────────────

    public class ConfirmEnrichmentRequest
    {
        public int IncomingInvoiceId { get; set; }
        public int? ProjectId { get; set; }
        public int? ContractId { get; set; }
        public string AccountingCode { get; set; }
        public string Notes { get; set; }
        public string ReviewedBy { get; set; }
    }

    public class LearnSupplierRuleRequest
    {
        public int IncomingInvoiceId { get; set; }
        public int? IssuerCompanyId { get; set; }
        public int? CompanyId { get; set; }
        public string SupplierVatNumber { get; set; }

        /// <summary>DefaultProject / DefaultAccount / AlwaysManualReview / AlwaysGeneralCost / RequiresProject</summary>
        public string RuleType { get; set; }

        public int? DefaultProjectId { get; set; }
        public string DefaultAccountingCode { get; set; }
        public bool IsGeneralCost { get; set; }
        public bool RequiresManualReview { get; set; }
        public bool RequiresProject { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
    }

    public class LearnProjectAliasRequest
    {
        public int ProjectId { get; set; }
        public string Alias { get; set; }

        /// <summary>Name / Street / Reference / Code / City</summary>
        public string AliasType { get; set; }

        public string CreatedBy { get; set; }
    }

    // ─── Interface ────────────────────────────────────────────────────────────

    public class AcceptPdfLinesRequest
    {
        public int IncomingInvoiceId { get; set; }
        public string AcceptedBy { get; set; }
    }

    public interface IInvoiceEnrichmentPipelineService
    {
        /// <summary>
        /// Voer de volledige verrijkingspipeline uit voor één factuur.
        /// Slaat het resultaat op in IncomingInvoiceEnrichment en past de facturstatus aan.
        /// </summary>
        Task<InvoiceEnrichmentVm> EnrichAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>Haal het opgeslagen verrijkingsresultaat op.</summary>
        Task<InvoiceEnrichmentVm> GetEnrichmentAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>Sla gebruikersbeslissingen op en zet status naar PendingApproval.</summary>
        Task ConfirmEnrichmentAsync(ConfirmEnrichmentRequest request, CancellationToken ct = default);

        /// <summary>
        /// Vervang de UBL-factuurlijnen door de PDF-voorgestelde lijnen.
        /// Enkel uitvoeren als HasWeakUblLines=true en ProposedPdfLines beschikbaar zijn.
        /// </summary>
        Task AcceptPdfLinesAsync(AcceptPdfLinesRequest request, CancellationToken ct = default);

        /// <summary>Sla een nieuwe leverancierregel op (leren van correcties).</summary>
        Task LearnSupplierRuleAsync(LearnSupplierRuleRequest request, CancellationToken ct = default);

        /// <summary>Voeg een projectalias toe (leren van correcties).</summary>
        Task LearnProjectAliasAsync(LearnProjectAliasRequest request, CancellationToken ct = default);
    }

    // ─── Matching services (gedeclareerd in FacadeCore voor DI) ───────────────

    public interface IProjectMatchingService
    {
        /// <summary>Zoek projectmatches op basis van tekstinhoud (gescoord).</summary>
        Task<IReadOnlyList<ProjectMatchResult>> FindMatchesAsync(
            string combinedText,
            int? issuerCompanyId = null,
            CancellationToken ct = default);
    }

    public interface IContractMatchingService
    {
        /// <summary>Zoek open contracten voor een project + leverancier.</summary>
        Task<IReadOnlyList<ContractMatchResult>> FindMatchesAsync(
            int projectId,
            int? companyId,
            string invoiceDescription = null,
            CancellationToken ct = default);
    }

    public interface IAccountingSuggestionService
    {
        /// <summary>Stel een kostenrekening voor op basis van context.</summary>
        Task<AccountingSuggestion> SuggestAsync(
            int? companyId,
            int? projectId,
            int? issuerCompanyId,
            string description,
            bool isGeneralCostContext,
            CancellationToken ct = default);
    }
}
