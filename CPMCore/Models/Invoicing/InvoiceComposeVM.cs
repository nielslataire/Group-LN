using BOCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using UblSharp.CommonAggregateComponents;

namespace CPMCore.Models.Invoicing
{
    // VM-items voor dropdown rendering
    public record IssuerItemVM(int Id, string Name, int? DefaultPaymentTermId, int? DefaultVatTypeId);
    public record PaymentTermItemVM(int Id, string Name, int Days, PaymentTermType TermType, PaymentTermDisplayMode DisplayMode, string? DisplayText);
    public class VatTypeVM
    {
        public int Id { get; set; }
        public decimal BasePercentage { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Type { get; set; }
        public int? DefaultSellBookingAccountNr { get; set; }
        public string? InvoiceMention { get; set; }
    }

    public enum StartStatus { Draft = 0, Invoice = 1 } // Invoice = meteen uitgeven
    public class InvoiceComposeVM
    {
        public int IssuerCompanyId { get; set; }
        public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public int? PartyId { get; set; }
        public InvoicePartyType? PartyType { get; set; }

        public StartStatus StartAs { get; set; } = StartStatus.Draft;
        public bool IsCreditNote { get; set; }
        public bool IsPrepaid { get; set; }

        public InvoiceMode Mode { get; set; } = InvoiceMode.Free;
        public string? HeaderDescription { get; set; }
        public string? DetailDescription { get; set; }
        public string? FooterDescription { get; set; }

        public int? ProjectId { get; set; }
        public int? SupplierContractId { get; set; }
        public int? PaymentTermId { get; set; }
        public int? VatTypeId { get; set; }
        public int? IssuerBankAccountId { get; set; }
        public IFormFile? PdfAppendix { get; set; }
        public List<VatTypeVM> VatTypes { get; set; } = new();


        // Voor vrije lijnen
        public List<InvoiceLineVM> Lines { get; set; } = new() { new() };

        // Voor Schijven
        public int? PaymentGroupId { get; set; }
        public List<int> StageIds { get; set; } = new();


        // UI lists
        public IEnumerable<IssuerItemVM> Issuers { get; set; } = Enumerable.Empty<IssuerItemVM>();
        public IEnumerable<PaymentTermItemVM> PaymentTerms { get; set; } = Enumerable.Empty<PaymentTermItemVM>();
        public IEnumerable<SelectListItem> Projects { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> SupplierContracts { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> IssuerBankAccounts { get; set; } = Array.Empty<SelectListItem>();
    }
}
