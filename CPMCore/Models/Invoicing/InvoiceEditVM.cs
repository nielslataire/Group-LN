using System;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceEditVM
    {
        public int InvoiceId { get; set; }
        public int IssuerCompanyId { get; set; }
        public string? IssuerName { get; set; }
        public string? PublicId { get; set; }

        public DateOnly InvoiceDate { get; set; }
        [Display(Name = "Vervaldatum")]
        public DateOnly? ExpirationDate { get; set; }

        public string? ClientName { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unknown;
        public string StatusLabel => Status.GetDisplayName();
        public bool IsCreditNote { get; set; }

        [Display(Name = "Omschrijving (optioneel)")]
        [DataType(DataType.MultilineText)]
        public string? HeaderDescription { get; set; }

        [Display(Name = "Detailomschrijving")]
        [DataType(DataType.MultilineText)]
        public string? DetailDescription { get; set; }

        [Display(Name = "Footer")]
        [DataType(DataType.MultilineText)]
        public string? FooterDescription { get; set; }

        [Display(Name = "Bankrekening")]
        public string? BankAccount { get; set; }

        public decimal TotalExclVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalInclVat { get; set; }

        public List<InvoiceLineEditVM> Lines { get; set; } = new();
        public string? ReturnUrl { get; set; }

        public string DisplayId => string.IsNullOrWhiteSpace(PublicId)
            ? $"Factuur #{InvoiceId}"
            : PublicId;

        public string DocumentType => IsCreditNote ? "Creditnota" : "Factuur";

        public bool IsDraft => Status == InvoiceStatus.Draft;
        public bool IsLimitedEdit => !IsDraft;
    }
}