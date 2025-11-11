using BOCore;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceDraftEditVM : InvoiceComposeVM
    {
        public int InvoiceId { get; set; }
        public string? PublicId { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? IssuerName { get; set; }
        public string? PartyDisplayName { get; set; }
        public string? PartyLookupValue { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public decimal TotalExclVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalInclVat { get; set; }
        public string? BankAccountIban { get; set; }
        public string? ReturnUrl { get; set; }

        public string DisplayId => string.IsNullOrWhiteSpace(PublicId)
            ? $"Factuur #{InvoiceId}"
            : PublicId;

        public string ModeLabel => Mode switch
        {
            InvoiceMode.Free => "Vrije lijnen",
            InvoiceMode.Stages => "Schijven",
            InvoiceMode.ChangeOrders => "Wijzigingsopdrachten",
            InvoiceMode.Utilities => "Nutsaansluitingen",
            _ => "Onbekend"
        };
    }
}
