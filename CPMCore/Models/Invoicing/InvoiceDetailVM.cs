using System;
using System.Collections.Generic;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceDetailVM
    {
        public int Id { get; set; }
        public string? PublicId { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unknown;
        public InvoicePartyVM Issuer { get; set; } = new();
        public InvoicePartyVM Client { get; set; } = new();
        public string? BankAccount { get; set; }
        public string? HeaderText { get; set; }
        public string? DetailText { get; set; }
        public string? ExtraInfo { get; set; }
        public decimal TotalExclVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalInclVat { get; set; }
        public decimal? PaidAmount { get; set; }
        public bool IsCreditNote { get; set; }
        public decimal? Balance { get; set; }
        public int? OctopusBookyearId { get; set; }
        public string? OctopusJournalKey { get; set; }
        public int? OctopusDocumentSequenceNr { get; set; }
        public string? OctopusWorkflowState { get; set; }
        public DateTime? OctopusWorkflowUpdatedAt { get; set; }
        public string? OctopusDeliveryState { get; set; }
        public string? OctopusDeliveryComment { get; set; }
        public DateTime? OctopusDeliveryDateTime { get; set; }
        public DateTime? OctopusDeliveryUpdatedAt { get; set; }
        public IReadOnlyList<InvoiceDetailLineVM> Lines { get; set; } = Array.Empty<InvoiceDetailLineVM>();
        public IReadOnlyList<InvoiceEmailLogItemVM> EmailLogs { get; set; } = Array.Empty<InvoiceEmailLogItemVM>();
        public DateTime? LastEmailSentAt { get; set; }
        public string StatusLabel => Status.GetDisplayName();
    }

    public class InvoicePartyVM
    {
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? VatNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class InvoiceDetailLineVM
    {
        public string Text { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public decimal VatRate { get; set; }
        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}