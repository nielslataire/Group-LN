// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "InvoicingLegacy". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    // INVOICING
    public class ProjectBillingVM
    {
        public int ProjectId { get; set; }
        public int ClientAccountId { get; set; }

        public string ProjectName { get; set; } = "";
        public string ClientName { get; set; } = "";

        public List<BillableUnitVM> Units { get; set; } = new();
        public List<FreeLineVM> FreeLines { get; set; } = new();

        public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly? DueDate { get; set; }
        public string? Note { get; set; }
    }

    public class BillableUnitVM
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = "";
        public List<BillableStageVM> Stages { get; set; } = new();
    }

    public class BillableStageVM
    {
        public int StageId { get; set; }
        public int GroupId { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; } = "";
        public decimal Percentage { get; set; }
        public decimal AmountExcl { get; set; }   // berekend
        public decimal VatRate { get; set; }      // ingevuld uit Stage.VatPercentage of project setting
        public string? GroupName { get; set; }
    }

    public class FreeLineVM
    {
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; } = 1m;
        public decimal UnitPrice { get; set; }
        public decimal VatRate { get; set; } = 21m;
        public string Unit { get; set; } = "st";
    }

    public class ProjectInvoicingModel
    {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = "";
        public int? IssuerCompanyIdBuilder { get; set; }
        public int? IssuerCompanyIdLandOwner { get; set; }

        public List<ClientAccountWithInvoicableBO> ClientAccounts { get; set; } = new();
        public List<ClientAccountWithInvoicableChangeOrderBO> ClientChangeOrders { get; set; } = new();
        public List<ChangeOrderInvoicingClientVM> ChangeOrderInvoicingClients { get; set; } = new();
        public List<ClientUtilityCostBO> ClientUtilityCosts { get; set; } = new();
    }

    public class ChangeOrderInvoicingClientVM
    {
        public ClientAccountBO Client { get; set; } = new();
        public List<ChangeOrderInvoicingRowVM> Rows { get; set; } = new();
    }

    public class ChangeOrderInvoicingRowVM
    {
        public int ChangeOrderId { get; set; }
        public int ChangeOrderDetailId { get; set; }
        public string ChangeOrderDescription { get; set; } = string.Empty;
        public string DetailDescription { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal InvoicedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal MaxPercentage { get; set; }
        public decimal DefaultPercentage { get; set; }
        public decimal VatPercentage { get; set; }
    }

    public class ProjectPaymentStagesModel
    {
        public ProjectPaymentStagesModel()
        {
            _groups = new List<ProjectPaymentGroupBO>();
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
        private List<ProjectPaymentGroupBO> _groups;
        public List<ProjectPaymentGroupBO> Groups
        {
            get
            {
                return _groups;
            }
            set
            {
                _groups = value;
            }
        }
    }

    public class ProjectPaymentStagesAddUpdateModel
    {
        public ProjectPaymentStagesAddUpdateModel()
        {
            _group = new ProjectPaymentGroupBO();
            _stages = new List<ProjectPaymentStageBO>();
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

        private ProjectPaymentGroupBO _group;
        public ProjectPaymentGroupBO Group
        {
            get
            {
                return _group;
            }
            set
            {
                _group = value;
            }
        }
        private List<ProjectPaymentStageBO> _stages;
        public List<ProjectPaymentStageBO> Stages
        {
            get
            {
                return _stages;
            }
            set
            {
                _stages = value;
            }
        }
    }

    public class ProjectPaymentGroupLinkModel
    {
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
        private string _projectName;
        public string ProjectName
        {
            get
            {
                return _projectName;
            }
            set
            {
                _projectName = value;
            }
        }
        private List<UnitBO> _units;
        public List<UnitBO> Units
        {
            get
            {
                return _units;
            }
            set
            {
                _units = value;
            }
        }
        private List<IdNameBO> _paymentgroups;
        public List<IdNameBO> PaymentGroups
        {
            get
            {
                return _paymentgroups;
            }
            set
            {
                _paymentgroups = value;
            }
        }
    }

    public class AddStageDocModel
    {
        public AddStageDocModel()
        {
            _doc = new ProjectDocBO();
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
        private int _stageid;
        public int StageId
        {
            get
            {
                return _stageid;
            }
            set
            {
                _stageid = value;
            }
        }
        private ProjectDocBO _doc;
        public ProjectDocBO Doc
        {
            get
            {
                return _doc;
            }
            set
            {
                _doc = value;
            }
        }
    }

    public class SelectStageDocModel
    {
        public SelectStageDocModel()
        {
            _docs = new List<IdNameBO>();
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
        private int _stageid;
        public int StageId
        {
            get
            {
                return _stageid;
            }
            set
            {
                _stageid = value;
            }
        }
        private int _docid;
        public int DocId
        {
            get
            {
                return _docid;
            }
            set
            {
                _docid = value;
            }
        }
        private List<IdNameBO> _docs;
        public List<IdNameBO> Docs
        {
            get
            {
                return _docs;
            }
            set
            {
                _docs = value;
            }
        }
    }

    public class DeleteStageDocModel
    {
        public DeleteStageDocModel()
        {
            _doc = new ProjectDocBO();
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
        private int _stageid;
        public int StageId
        {
            get
            {
                return _stageid;
            }
            set
            {
                _stageid = value;
            }
        }
        private ProjectDocBO _doc;
        public ProjectDocBO Doc
        {
            get
            {
                return _doc;
            }
            set
            {
                _doc = value;
            }
        }
    }

    public class ModalPrintInvoiceListModel
    {
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
        private List<IdNameBO> _clients;
        public List<IdNameBO> Client
        {
            get
            {
                return _clients;
            }
            set
            {
                _clients = value;
            }
        }
        private int _selectedclient;
        public int SelectedClient
        {
            get
            {
                return _selectedclient;
            }
            set
            {
                _selectedclient = value;
            }
        }
    }

    public class PrintInvoiceListModel
    {
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
        private ClientAccountBO _client;
        public ClientAccountBO Client
        {
            get
            {
                return _client;
            }
            set
            {
                _client = value;
            }
        }
        private List<InvoiceBO> _invoices;
        public List<InvoiceBO> Invoices
        {
            get
            {
                return _invoices;
            }
            set
            {
                _invoices = value;
            }
        }
        private List<UnitWithStagesBO> _unitswithstages;
        public List<UnitWithStagesBO> UnitsWithStages
        {
            get
            {
                return _unitswithstages;
            }
            set
            {
                _unitswithstages = value;
            }
        }
        private ProjectSalesSettingsBO _salessettings;
        public ProjectSalesSettingsBO SalesSettings
        {
            get
            {
                return _salessettings;
            }
            set
            {
                _salessettings = value;
            }
        }
    }

    // CONTRACTS
}
