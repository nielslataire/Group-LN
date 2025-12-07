using System;
using System.Collections.Generic;

namespace ServiceCore.Invoicing.Pdf;

public class InvoiceVm
{
    public InvoiceInfoVm Invoice { get; init; } = new();
    public CompanyVm IssuerCompany { get; init; } = new();
    public CompanyVm Client { get; init; } = new();
    public ProjectVm Project { get; init; } = new();
    public UnitVm Unit { get; init; } = new();
    public TotalsVm Totals { get; init; } = new();
    public PaymentVm Payment { get; init; } = new();
    public IReadOnlyList<InvoiceLineVm> Lines { get; init; } = Array.Empty<InvoiceLineVm>();
    public IReadOnlyList<VatRateSummaryVm> VatSummary { get; init; } = Array.Empty<VatRateSummaryVm>();
    public IReadOnlyList<string> VatMentions { get; init; } = Array.Empty<string>();
    public string? ExtraInfo { get; init; }
    public string? HeaderDescription { get; init; }
    public string? DetailDescription { get; init; }
}

public sealed class InvoiceInfoVm
{
    public int Id { get; init; }
    public string? PublicId { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Status { get; init; }
}

public sealed class CompanyVm
{
    public string? Name { get; init; }
    public string? LegalName { get; init; }
    public string? VAT { get; init; }
    public string? AddressLine { get; init; }
    public string? Postal { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Phone2 { get; init; }
    public string? IBAN { get; init; }
    public string? BIC { get; init; }
    public string? Website { get; init; }
    public string? LegalFormAbbreviation { get; init; }
    public string? DefaultIban { get; init; }
    public string? DefaultBic { get; init; }
}

public sealed class ProjectVm
{
    public string? Name { get; init; }
}

public sealed class UnitVm
{
    public string? Name { get; init; }
    public string? Address { get; init; }
}

public sealed class TotalsVm
{
    public decimal Excl { get; init; }
    public decimal Vat { get; init; }
    public decimal Incl { get; init; }
}

public sealed class PaymentVm
{
    public string? Structured { get; init; }
    public string? BankAccount { get; init; }
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public bool QrEnabled { get; init; }
    public string? Terms { get; init; }
}

public sealed class VatRateSummaryVm
{
    public decimal Rate { get; init; }
    public decimal Net { get; init; }
    public decimal Vat { get; init; }
    public decimal Total => Net + Vat;
}

public sealed class InvoiceLineVm
{
    public string Key { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? LineType { get; init; }
    public string? GroupName { get; init; }
    public int? UnitId { get; init; }
    public int? PaymentStageId { get; init; }
    public int? ChangeOrderDetailId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Vat { get; init; }
    public decimal Total { get; init; }
    public IDictionary<string, object?> Extras { get; init; } = new Dictionary<string, object?>();
}