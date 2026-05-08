using BOCore;   
namespace CPMCore.Models.Invoicing
{
    public class InvoiceListItemVM
    {
        public int Id { get; set; }
        public string PublicId { get; set; }
        public string ClientName { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unknown;
        public decimal? GrossTotal { get; set; }
        public decimal? NetTotal { get; set; }
        public decimal? Balance { get; set; }
        public int? InvoiceNumber { get; set; }
        public int? InvoiceMonth { get; set; }
        public int? InvoiceYear { get; set; }
        public bool IsCreditNote { get; set; }
        public long InvoiceSortValue { get; set; }
        public string StatusLabel => Status.GetDisplayName();
        public bool RequiresDigitalInvoice { get; set; }
        public bool HasEmail { get; set; }
        public int? ClientType { get; set; }
        public bool IsSupplier { get; set; }
        public bool HasCompanyName { get; set; }
        public string OctopusWorkflowState { get; set; }
        public bool DigitallySent { get; set; }
        public bool DigitalSendSkipped { get; set; }
        public bool ShowPrintButton { get; set; }
        public string? ProjectName { get; set; }
        public string SendMethod { get; set; } = "Niet verzonden";
        public string? BookyearLabel { get; set; }
        public int? OctopusBookyearId { get; set; }
        public string? OctopusJournalKey { get; set; }
        public int? OctopusDocumentSequenceNr { get; set; }
        public DateTime? OctopusBookedAt { get; set; }
    }
}
