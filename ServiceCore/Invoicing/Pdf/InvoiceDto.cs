using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ServiceCore.Invoicing.Pdf;

public class InvoiceDto
{
    public int Id { get; set; }
    public string? PublicId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Status { get; set; }

    public PartyDto Issuer { get; set; } = new();
    public PartyDto Client { get; set; } = new();
    public ProjectInfoDto Project { get; set; } = new();
    public UnitInfoDto Unit { get; set; } = new();

    public string? BankAccount { get; set; }
    public string? StructuredMessage { get; set; }
    public string? ExtraInfo { get; set; }
    public string? QrPayload { get; set; }

    public TotalsDto Totals { get; set; } = new();
    public IReadOnlyList<InvoiceLineDto> Lines { get; set; } = Array.Empty<InvoiceLineDto>();
}

public sealed class PartyDto
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
    public string? Phone2 { get; set; }
    public string? BankAccount { get; set; }
    public string? Bic { get; set; }
    public string? Website { get; set; }
}

public sealed class ProjectInfoDto
{
    public string? Name { get; set; }
}

public sealed class UnitInfoDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public sealed class TotalsDto
{
    public decimal Excl { get; set; }
    public decimal Vat { get; set; }
    public decimal Incl { get; set; }
}

public sealed class InvoiceLineDto
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public IDictionary<string, object?> Extras { get; set; } = new Dictionary<string, object?>();
}