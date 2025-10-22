using BOCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using UblSharp.CommonAggregateComponents;

namespace CPMCore.Models.Invoicing
{
    // VM-items voor dropdown rendering
    public record IssuerItemVM(int Id, string Name, int? DefaultPaymentTermId);
    public record PaymentTermItemVM(int Id, string Name, int Days);
    public enum StartStatus { Draft = 0, Invoice = 1 } // Invoice = meteen uitgeven
    public class InvoiceComposeVM
    {
        public int IssuerCompanyId { get; set; }
        public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public int? PartyId { get; set; }
        public InvoicePartyType? PartyType { get; set; }

        public StartStatus StartAs { get; set; } = StartStatus.Invoice;

        public InvoiceMode Mode { get; set; } = InvoiceMode.Free;
        public string? HeaderDescription { get; set; }

        public int? ProjectId { get; set; }
        public int? SupplierContractId { get; set; }
        public int? PaymentTermId { get; set; }


        // Voor vrije lijnen
        public List<InvoiceLineVM> Lines { get; set; } = new() { new() };


        public int? PaymentGroupId { get; set; }
        public List<int> StageIds { get; set; } = new();


        // UI lists
        public IEnumerable<IssuerItemVM> Issuers { get; set; } = Enumerable.Empty<IssuerItemVM>();
        public IEnumerable<PaymentTermItemVM> PaymentTerms { get; set; } = Enumerable.Empty<PaymentTermItemVM>();
        public IEnumerable<SelectListItem> Projects { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> SupplierContracts { get; set; } = Array.Empty<SelectListItem>();
    }
}
