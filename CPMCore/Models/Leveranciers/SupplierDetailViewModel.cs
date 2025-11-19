using System.Collections.Generic;

namespace CPMCore.Models.Leveranciers;

public class SupplierDetailViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? EnterpriseNumber { get; init; }
    public string? LegalForm { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? BusNumber { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? CountryName { get; init; }
    public string? CountryCode { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
    public string? InvoiceEmail { get; init; }
    public string? WebUrl { get; init; }
    public bool RequiresDigitalInvoice { get; init; }
    public bool AttachUblByDefault { get; init; }
    public IReadOnlyList<string> Activities { get; init; } = new List<string>();
    public IReadOnlyList<SupplierDepartmentDetailViewModel> Departments { get; init; } = new List<SupplierDepartmentDetailViewModel>();
    public IReadOnlyList<SupplierContactDetailViewModel> Contacts { get; init; } = new List<SupplierContactDetailViewModel>();
    public IReadOnlyList<SupplierContractDetailViewModel> Contracts { get; init; } = new List<SupplierContractDetailViewModel>();
}

public class SupplierDepartmentDetailViewModel
{
    public string? Name { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? Bus { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? CountryName { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
}

public class SupplierContactDetailViewModel
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Function { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
    public string? DepartmentName { get; init; }
}

public class SupplierContractDetailViewModel
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public int ActivityCount { get; init; }
}