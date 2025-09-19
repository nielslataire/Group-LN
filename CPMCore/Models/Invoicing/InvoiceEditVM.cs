namespace CPMCore.Models.Invoicing
{
    public class InvoiceEditVM
    {
        public int InvoiceId { get; set; }
        public int IssuerCompanyId { get; set; }
        public string? PublicId { get; set; }
        public byte? StatusId { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }

        public int? ClientType { get; set; } // 1=ClientAccount,2=ClientContact
        public int? ClientId { get; set; }
        public int? CompanyId { get; set; }
    }
}
