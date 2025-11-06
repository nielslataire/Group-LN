namespace CPMCore.Models.Invoicing
{
    public class InvoiceListItemVM
    {
        public int Id { get; set; }
        public string PublicId { get; set; }
        public string ClientName { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public string Status { get; set; }
        public decimal? GrossTotal { get; set; }
        public decimal? Balance { get; set; }
        public int? InvoiceNumber { get; set; }
        public int? InvoiceMonth { get; set; }
        public int? InvoiceYear { get; set; }
        public long InvoiceSortValue { get; set; }
    }
}
