using System;
using System.Collections.Generic;

namespace CPMCore.Models.Leveranciers;

public class SupplierIndexViewModel
{
    public IReadOnlyList<SupplierListItemViewModel> Suppliers { get; init; } = new List<SupplierListItemViewModel>();
    public IReadOnlyList<ActivityFilterItemViewModel> Activities { get; init; } = new List<ActivityFilterItemViewModel>();
    public IReadOnlyList<IssuerCompanyOptionViewModel> IssuerCompanies { get; init; } = new List<IssuerCompanyOptionViewModel>();
    public int? SelectedIssuerCompanyId { get; init; }
}

public class SupplierListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? EnterpriseNumber { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
    public int ContractCount { get; init; }
    public decimal TotalContractAmount { get; init; }
    public IReadOnlyList<int> ActivityIds { get; init; } = new List<int>();
    public IReadOnlyList<int> IssuerCompanyIds { get; init; } = new List<int>();

    public string? PrimaryPhone
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                return Phone;
            }

            return string.IsNullOrWhiteSpace(Mobile) ? null : Mobile;
        }
    }
}

public class ActivityFilterItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? GroupName { get; init; }
}
