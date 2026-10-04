// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Contracts". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
using BOCore;
using CPMCore.Models.Leveranciers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System;

namespace CPMCore.Models.Projecten
{
    public class ProjectCostCalculationVM
    {
        public int ProjectId { get; set; }

        // Percentages/instellingen
        public decimal VatPercent { get; set; }
        public decimal RegistrationPercent { get; set; }
        public RegistrationType RegistrationType { get; set; }
        public bool MixedVatRegistration { get; set; }

        // Invoer bovenaan
        public decimal FixedCertificateCost { get; set; }        // ⬅️ vaste aktekost (globaal)
        public decimal SurveyorFee { get; set; }                 // per unit
        public decimal ConnectionFee { get; set; }               // per unit
        public decimal BaseDeedShare { get; set; }               // per unit
        public decimal ParcelCost { get; set; }                  // per unit
        public decimal MortgageRegistrationCost { get; set; }    // ⬅️ hypotheekkantoor (globaal)

        public decimal VatPctCostsOther { get; set; }            // 21
        public decimal VatPctCostsConnection { get; set; }       // = VatPercent

        public List<UnitCostLineVM> Lines { get; set; } = new();

        public CostTotalsVM CostTotals { get; set; } = new();

        public int UnitCount => Lines?.Count ?? 0;

        // … je bestaande aggregaten (Land/Build/Base/VAT/Registration) …

        public decimal TotalLandBase => Lines?.Sum(x => x.LandBase) ?? 0m;
        public decimal TotalBuildBase => Lines?.Sum(x => x.BuildBase) ?? 0m;
        public decimal TotalLandDiscount => Lines?.Sum(x => x.LandDiscount) ?? 0m;
        public decimal TotalBuildDiscount => Lines?.Sum(x => x.BuildDiscount) ?? 0m;
        public decimal TotalNetLandBase => Lines?.Sum(x => x.NetLandBase) ?? 0m;
        public decimal TotalNetBuildBase => Lines?.Sum(x => x.NetBuildBase) ?? 0m;
        public decimal TotalBase => Lines?.Sum(x => x.BasePrice) ?? 0m;
        public decimal TotalVatOnBase => Lines?.Sum(x => x.VatAmount) ?? 0m;
        public decimal TotalRegistration => Lines?.Sum(x => x.RegistrationAmount) ?? 0m;

        // Per-rij kosten zijn zónder notaris, vaste akte & hypo → die komen via CostTotals erbij
        public decimal TotalCostsExcl => (Lines?.Sum(x => x.CostsExcl) ?? 0m) + (CostTotals?.NotaryExcl ?? 0m) + (CostTotals?.FixedActeExcl ?? 0m) + (CostTotals?.MortgageExcl ?? 0m);
        public decimal TotalCostsVat => (Lines?.Sum(x => x.CostsVat) ?? 0m) + (CostTotals?.NotaryVat ?? 0m) + (CostTotals?.FixedActeVat ?? 0m) + (CostTotals?.MortgageVat ?? 0m);
        public decimal TotalCostsIncl => TotalCostsExcl + TotalCostsVat;

        // Grand total = som van alle rij-totalen + globale posten (incl btw)
        public decimal GrandTotal => (Lines?.Sum(x => x.Total) ?? 0m) + (CostTotals?.NotaryIncl ?? 0m) + (CostTotals?.FixedActeIncl ?? 0m) + (CostTotals?.MortgageIncl ?? 0m);
    }

    public class UnitCostLineVM
    {
        public int UnitId { get; set; }
        public string Code { get; set; }

        public decimal LandBase { get; set; }
        public decimal BuildBase { get; set; }

        public decimal LandDiscount { get; set; }
        public decimal BuildDiscount { get; set; }

        public decimal NetLandBase => Math.Max(0m, LandBase - LandDiscount);
        public decimal NetBuildBase => Math.Max(0m, BuildBase - BuildDiscount);
        public decimal BasePrice => NetLandBase + NetBuildBase;

        public decimal VatAmount { get; set; }
        public decimal RegistrationAmount { get; set; }

        public bool IncludePerUnitCosts { get; set; }

        // Deze 4 per-unit kosten worden alleen gevuld als IncludePerUnitCosts = true
        public decimal CostsExcl { get; set; }
        public decimal CostsVat { get; set; }
        public decimal CostsIncl => CostsExcl + CostsVat;

        public decimal Total => BasePrice + VatAmount + RegistrationAmount + CostsIncl;
    }

    public class CostTotalsVM
    {
        // Globale kosten
        public decimal NotaryExcl { get; set; }          // Notariskosten (schijven)
        public decimal NotaryVat { get; set; }
        public decimal NotaryIncl => NotaryExcl + NotaryVat;

        public decimal FixedActeExcl { get; set; }       // Vaste aktekost (input)
        public decimal FixedActeVat { get; set; }
        public decimal FixedActeIncl => FixedActeExcl + FixedActeVat;

        public decimal MortgageExcl { get; set; }        // Hypotheekkantoor
        public decimal MortgageVat { get; set; }
        public decimal MortgageIncl => MortgageExcl + MortgageVat;

        // Per eenheid, samengevoegd tot totalen
        public decimal SurveyorExcl { get; set; }
        public decimal SurveyorVat { get; set; }
        public decimal SurveyorIncl => SurveyorExcl + SurveyorVat;

        public decimal ConnectionExcl { get; set; }
        public decimal ConnectionVat { get; set; }
        public decimal ConnectionIncl => ConnectionExcl + ConnectionVat;

        public decimal BaseDeedExcl { get; set; }
        public decimal BaseDeedVat { get; set; }
        public decimal BaseDeedIncl => BaseDeedExcl + BaseDeedVat;

        public decimal ParcelExcl { get; set; }
        public decimal ParcelVat { get; set; }
        public decimal ParcelIncl => ParcelExcl + ParcelVat;

        // Totalen kosten
        public decimal AllExcl => NotaryExcl + FixedActeExcl + MortgageExcl + SurveyorExcl + ConnectionExcl + BaseDeedExcl + ParcelExcl;
        public decimal AllVat => NotaryVat + FixedActeVat + MortgageVat + SurveyorVat + ConnectionVat + BaseDeedVat + ParcelVat;
        public decimal AllIncl => AllExcl + AllVat;
    }

    public class ProjectContractsModel
    {
        public ProjectContractsModel()
        {
            _contracts = new List<ContractBO>();
            _budgetActivities = new List<BudgetActivityBO>();
            _IncommingInvoicesActivities = new List<IncommingInvoiceActivityBO>();
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private List<ContractBO> _contracts;
        public List<ContractBO> Contracts
        {
            get
            {
                return _contracts;
            }
            set
            {
                _contracts = value;
            }
        }
        private List<ActivityGroupBO> _activityGroups;
        public List<ActivityGroupBO> ActivityGroups
        {
            get
            {
                return _activityGroups;
            }
            set
            {
                _activityGroups = value;
            }
        }
        private List<BudgetActivityBO> _budgetActivities;
        public List<BudgetActivityBO> BudgetActivities
        {
            get
            {
                return _budgetActivities;
            }
            set
            {
                _budgetActivities = value;
            }
        }
        private List<IncommingInvoiceActivityBO> _IncommingInvoicesActivities;
        public List<IncommingInvoiceActivityBO> IncommingInvoicesActivities
        {
            get
            {
                return _IncommingInvoicesActivities;
            }
            set
            {
                _IncommingInvoicesActivities = value;
            }
        }
    }

    public class ProjectAddContractModel
    {
        public ProjectAddContractModel()
        {
            _contract = new ContractBO();
            _companies = new List<IdNameBO>();
            _activities = new List<IdNameBO>();
            _contractactivities = new List<ContractActivityBO>();
            _insurance = new InsuranceBO();
            _siteManagers = new List<IdNameBO>();
        }

        /// <summary>Aantal leveranciers in dit project — voor het inner-menu "Leveranciers"-teller,
        /// zelfde telling als DetailContracts' SupplierRows.</summary>
        public int SupplierCount { get; set; }

        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private ContractBO _contract;
        public ContractBO Contract
        {
            get
            {
                return _contract;
            }
            set
            {
                _contract = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private List<IdNameBO>? _companies;
        [Display(Name = "Bedrijfsnaam")]
        public List<IdNameBO>? Companies
        {
            get
            {
                return _companies;
            }
            set
            {
                _companies = value;
            }
        }
        private List<IdNameBO>? _siteManagers;
        public List<IdNameBO>? SiteManagers
        {
            get
            {
                return _siteManagers;
            }
            set
            {
                _siteManagers = value;
            }
        }
        private int _selectedCompany;
        public int SelectedCompany
        {
            get
            {
                return _selectedCompany;
            }
            set
            {
                _selectedCompany = value;
            }
        }
        private List<IdNameBO>? _activities;
        [Display(Name = "Activiteiten")]
        public List<IdNameBO>? Activities
        {
            get
            {
                return _activities;
            }
            set
            {
                _activities = value;
            }
        }
        private List<int>? _selectedActivities;
        public List<int>? SelectedActivities
        {
            get
            {
                return _selectedActivities;
            }
            set
            {
                _selectedActivities = value;
            }
        }
        private List<int>? _selectedActivitiesaddorders;
        public List<int>? SelectedActivitiesAddOrders
        {
            get
            {
                return _selectedActivitiesaddorders;
            }
            set
            {
                _selectedActivitiesaddorders = value;
            }
        }
        private InsuranceBO _insurance;
        public InsuranceBO Insurance
        {
            get
            {
                return _insurance;
            }
            set
            {
                _insurance = value;
            }
        }
        private List<IdNameBO>? _insurancecompanies;
        [Display(Name = "Maatschappij")]
        public List<IdNameBO>? InsuranceCompanies
        {
            get
            {
                return _insurancecompanies;
            }
            set
            {
                _insurancecompanies = value;
            }
        }
        private List<ContractActivityBO> _contractactivities;
        public List<ContractActivityBO> ContractActivities
        {
            get
            {
                return _contractactivities;
            }
            set
            {
                _contractactivities = value;
            }
        }

        public List<ActivityFilterItemViewModel> AllActivities { get; set; } = new();
        public List<IdNameBO> LegalForms { get; set; } = new();
        public List<IdNameBO> Countries { get; set; } = new();
    }

    public class ProjectCalculationSettings
    {
        public ProjectCalculationSettings()
        {
            _budgetActivities = new List<BudgetActivityBO>();
            _listactivities = new List<IdNameBO>();
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private string _projectname;
        [ValidateNever]
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private List<BudgetActivityBO> _budgetActivities;
        public List<BudgetActivityBO> BudgetActivities
        {
            get
            {
                return _budgetActivities;
            }
            set
            {
                _budgetActivities = value;
            }
        }
        private List<ActivityGroupBO> _activityGroups;
        [ValidateNever]
        public List<ActivityGroupBO> ActivityGroups
        {
            get
            {
                return _activityGroups;
            }
            set
            {
                _activityGroups = value;
            }
        }
        private List<IdNameBO> _listactivities;
        [ValidateNever]
        public List<IdNameBO> ListActivities
        {
            get
            {
                return _listactivities;
            }
            set
            {
                _listactivities = value;
            }
        }
        private List<int> _selectedActivities;
        [ValidateNever]
        public List<int> SelectedActivities
        {
            get
            {
                return _selectedActivities;
            }
            set
            {
                _selectedActivities = value;
            }
        }
        public decimal TotalBudget => BudgetActivities?.Sum(b => b.Price) ?? 0m;
    }

    // RECALCULATIOn
    public class ProjectRecalculationDetailModel
    {
        public ProjectRecalculationDetailModel()
        {
            _IncommingInvoicesActivities = new List<IncommingInvoiceActivityBO>();
            _activityGroups = new List<ActivityGroupBO>();
            _contracts = new List<ContractBO>();
            _budgetActivities = new List<BudgetActivityBO>();
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private int _activityid;
        public int ActivityID
        {
            get
            {
                return _activityid;
            }
            set
            {
                _activityid = value;
            }
        }
        private int _groupid;
        public int GroupID
        {
            get
            {
                return _groupid;
            }
            set
            {
                _groupid = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private ActivityBO _activity;
        public ActivityBO Activity
        {
            get
            {
                return _activity;
            }
            set
            {
                _activity = value;
            }
        }

        private List<IncommingInvoiceActivityBO> _IncommingInvoicesActivities;
        public List<IncommingInvoiceActivityBO> IncommingInvoicesActivities
        {
            get
            {
                return _IncommingInvoicesActivities;
            }
            set
            {
                _IncommingInvoicesActivities = value;
            }
        }
        private List<ContractBO> _ContractsWithoutInvoices;
        public List<ContractBO> ContractsWithoutInvoices
        {
            get
            {
                return _ContractsWithoutInvoices;
            }
            set
            {
                _ContractsWithoutInvoices = value;
            }
        }

        private List<ContractActivityBO> _ContractActivities;
        public List<ContractActivityBO> ContractActivities
        {
            get
            {
                return _ContractActivities;
            }
            set
            {
                _ContractActivities = value;
            }
        }
        private List<ContractBO> _contracts;
        public List<ContractBO> Contracts
        {
            get
            {
                return _contracts;
            }
            set
            {
                _contracts = value;
            }
        }
        private List<ActivityGroupBO> _activityGroups;
        public List<ActivityGroupBO> ActivityGroups
        {
            get
            {
                return _activityGroups;
            }
            set
            {
                _activityGroups = value;
            }
        }
        private List<BudgetActivityBO> _budgetActivities;
        public List<BudgetActivityBO> BudgetActivities
        {
            get
            {
                return _budgetActivities;
            }
            set
            {
                _budgetActivities = value;
            }
        }
       

    }

    // INSURANCES

    // CONTRACTS
    public class DetailContractsModel
    {
        public DetailContractsModel()
        {
            _contracts = new List<ContractBO>();
            _supplierRows = new List<ContractSupplierRowModel>();
        }
        private List<ContractBO> _contracts;
        public List<ContractBO> Contracts
        {
            get
            {
                return _contracts;
            }
            set
            {
                _contracts = value;
            }
        }
        private List<ContractSupplierRowModel> _supplierRows;
        public List<ContractSupplierRowModel> SupplierRows
        {
            get { return _supplierRows; }
            set { _supplierRows = value; }
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
    }

    public class ContractSupplierRowModel
    {
        public ContractSupplierRowModel()
        {
            Contracts = new List<ContractBO>();
        }
        public List<ContractBO> Contracts { get; set; }
        public ContractBO Contract => Contracts?.FirstOrDefault();
        public IdNameBO Company { get; set; }
        public decimal TotalInvoiced { get; set; }
        public bool HasContract => Contracts?.Any() == true;
        public bool AllContractsSigned => Contracts?.Any() == true && Contracts.All(c => c.ContractSigned);
        // Effectieve lotprijs = basisprijs + bijbestellingen op dat lot.
        public decimal TotalContractPrice => Contracts?.Sum(c => c.Activities?.Sum(a => a.EffectivePrice) ?? 0) ?? 0;
    }

    /// <summary>Welke optionele kolommen mee op de aannemerslijst-PDF komen.</summary>
    public sealed record SupplierListColumns(bool Sent, bool Signed, bool Vgm, bool Notification, bool Pid);

    /// <summary>Eén regel op de aannemerslijst: één (contract-)activiteit met de aannemer/partij erbij.</summary>
    public sealed class SupplierListRow
    {
        public int GroupLot { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Vat { get; set; }
        public string Address { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        /// <summary>True = contactgegevens komen van de aannemer zelf (geen aparte werfleider).</summary>
        public bool ContactIsGeneral { get; set; }
        public DateTime? SentDate { get; set; }
        public string SentNote { get; set; }
        public bool Signed { get; set; }
        public bool Vgm { get; set; }
        public bool Notification { get; set; }
        public bool Pid { get; set; }
        /// <summary>True = rij afgeleid uit een projectveld (geen contract) → statuskolommen tonen "n.v.t.".</summary>
        public bool IsSynthesized { get; set; }
    }

    /// <summary>De PROJECTFICHE bovenaan de aannemerslijst-PDF.</summary>
    public sealed class SupplierListProjectInfo
    {
        public string ProjectName { get; set; } = string.Empty;
        public string AddressLine { get; set; }
        public string CityLine { get; set; }
        public string OpdrachtgeverName { get; set; }
        public string OpdrachtgeverAddress { get; set; }
        public string ProjectcoordinatieName { get; set; }
        public string ProjectcoordinatiePhone { get; set; }
        public string ProjectcoordinatieEmail { get; set; }
        public string VeiligheidscoordinatorName { get; set; }
        public string VeiligheidscoordinatorAddress { get; set; }
        public string VeiligheidscoordinatorEmail { get; set; }
        public string AardVanDeWerken { get; set; }
        public DateOnly? StartDatumWerf { get; set; }
        public DateOnly? WerfmeldingDate { get; set; }
        public string WerfmeldingDossier { get; set; }
        public int AantalPartijen { get; set; }
        public string LaatstBijgewerktDoor { get; set; }
        public DateTime LaatstBijgewerkt { get; set; } = DateTime.Now;
    }

    public sealed class SupplierListModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public SupplierListProjectInfo Project { get; set; } = new();
        public List<SupplierListRow> Rows { get; set; } = new();
    }

    /// <summary>Eén persoon op de klantenlijst-PDF: contactpersoon of mede-eigenaar van een klant.</summary>
    public sealed class ClientListPerson
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>Enkel ingevuld voor mede-eigenaars; contactpersonen tonen geen apart adres.</summary>
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Cellphone { get; set; }
        public string Email { get; set; }
    }

    /// <summary>Eén klant op de klantenlijst-PDF, met zijn eenheden, contactpersonen en mede-eigenaars.</summary>
    public sealed class ClientListRow
    {
        public string ClientName { get; set; } = string.Empty;
        public string Address { get; set; }
        public List<string> Units { get; set; } = new();
        public List<ClientListPerson> Contacts { get; set; } = new();
        public List<ClientListPerson> CoOwners { get; set; } = new();
    }

    /// <summary>De PROJECTFICHE bovenaan de klantenlijst-PDF.</summary>
    public sealed class ClientListProjectInfo
    {
        public string ProjectName { get; set; } = string.Empty;
        public string AddressLine { get; set; }
        public string CityLine { get; set; }
        public string OpdrachtgeverName { get; set; }
        public string OpdrachtgeverAddress { get; set; }
        public int AantalKlanten { get; set; }
        public int AantalEenhedenTotaal { get; set; }
        public int AantalEenhedenVerkocht { get; set; }
        public string LaatstBijgewerktDoor { get; set; }
        public DateTime LaatstBijgewerkt { get; set; } = DateTime.Now;
    }

    public sealed class ClientListModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public ClientListProjectInfo Project { get; set; } = new();
        public List<ClientListRow> Rows { get; set; } = new();
    }

    public class ProjectContractDetailModel
    {
        public ProjectContractDetailModel()
        {
            _contract = new ContractBO();
            _company = new CompanyBO();
            _incommingInvoices = new List<IncommingInvoiceBO>();
            Contracts = new List<ContractBO>();
        }

        public List<ContractBO> Contracts { get; set; }
        public bool HasContract { get; set; }

        /// <summary>Aantal leveranciers in dit project (contract of enkel factuur) — voor het
        /// inner-menu "Leveranciers"-teller, zelfde telling als DetailContracts' SupplierRows.</summary>
        public int SupplierCount { get; set; }

        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }

        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }

        private ContractBO _contract;
        public ContractBO Contract
        {
            get
            {
                return _contract;
            }
            set
            {
                _contract = value;
            }
        }

        private CompanyBO _company;
        public CompanyBO Company
        {
            get
            {
                return _company;
            }
            set
            {
                _company = value;
            }
        }

        private List<IncommingInvoiceBO> _incommingInvoices;
        public List<IncommingInvoiceBO> IncommingInvoices
        {
            get
            {
                return _incommingInvoices;
            }
            set
            {
                _incommingInvoices = value;
            }
        }
    }

    public class ProjectIssuerCompanyOptionVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // SHARED
    public class Select2DTO
    {
        // as select2 is formed like id and text so we used DTO
        public int id
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }
        private int m_id;
        public string text
        {
            get
            {
                return m_text;
            }
            set
            {
                m_text = value;
            }
        }
        private string m_text;
    }

    // ── Coördinatieproject helper ViewModels ──────────────────────────────────

    public class ProjectContractSliceVM
    {
        public int Id { get; set; }

        [Display(Name = "Omschrijving")]
        public string Description { get; set; }

        [Display(Name = "Percentage (%)")]
        [Range(0.01, 100, ErrorMessage = "Percentage moet tussen 0,01 en 100 liggen.")]
        public decimal Percentage { get; set; }

        public decimal Amount { get; set; }

        public int? InvoiceId { get; set; }

        public string InvoicePublicId { get; set; }

        public bool IsInvoiced => InvoiceId.HasValue;
    }

    public class ProjectHourlyRateVM
    {
        public string UserId { get; set; }

        [Display(Name = "Naam")]
        public string UserFullName { get; set; }

        [Display(Name = "Uurtarief (€)")]
        [Range(0, 9999.99, ErrorMessage = "Uurtarief moet positief zijn.")]
        public decimal HourlyRate { get; set; }
    }
}
