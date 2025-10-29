using System;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceDeleteConfirmVM
    {
        public int Id { get; set; }
        public int IssuerCompanyId { get; set; }
        public string DisplayId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
