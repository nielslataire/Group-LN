// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "ChangeOrdersLegacy". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class DetailChangeOrderModel
    {
        public DetailChangeOrderModel()
        {
            _co = new List<ChangeOrderBO>();
        }
        private List<ChangeOrderBO> _co;
        public List<ChangeOrderBO> CO
        {
            get
            {
                return _co;
            }
            set
            {
                _co = value;
            }
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
        private int _clientaccountid;
        public int ClientAccountId
        {
            get
            {
                return _clientaccountid;
            }
            set
            {
                _clientaccountid = value;
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
        private string _clientname;
        public string ClientName
        {
            get
            {
                return _clientname;
            }
            set
            {
                _clientname = value;
            }
        }

        public IReadOnlyList<IdNameBO> Clients { get; set; } = Array.Empty<IdNameBO>();
        public IDictionary<int, string> ClientUnits { get; set; } = new Dictionary<int, string>();

        // Elektronisch ondertekenen (fase 1): het recentste dossier per wijzigingsopdracht, zodat de
        // lijst status en ingang kan tonen. Leeg wanneer Features:EnableSigning uit staat.
        public bool SigningEnabled { get; set; }
        public bool CanStartSigning { get; set; }
        public IDictionary<int, FacadeCore.Signing.CaseStatusView> SigningCases { get; set; } = new Dictionary<int, FacadeCore.Signing.CaseStatusView>();
    }

    public class ProjectChangeOrderModel
    {
        public ProjectChangeOrderModel()
        {
            _changeorders = new List<ChangeOrderBO>();
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
        private List<ChangeOrderBO> _changeorders;
        public List<ChangeOrderBO> ChangeOrders
        {
            get
            {
                return _changeorders;
            }
            set
            {
                _changeorders = value;
            }
        }
        private decimal _vatPercentage;
        [UIHint("Percentage")]
        public decimal VatPercentage
        {
            get
            {
                return _vatPercentage;
            }
            set
            {
                _vatPercentage = value;
            }
        }
    }

    public class ProjectChangeOrderAddUpdateModel
    {
        public ProjectChangeOrderAddUpdateModel()
        {
            _clientaccounts = new List<IdNameBO>();
            _projectContractActivities = new List<IdNameBO>();
            _changeorder = new ChangeOrderBO();
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
        private string? _projectname;
        public string? ProjectName
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
        private string? _clientName;
        public string? ClientName
        {
            get
            {
                return _clientName;
            }
            set
            {
                _clientName = value;
            }
        }
        private List<IdNameBO> _clientaccounts;
        [Display(Name = "Klanten")]
        public List<IdNameBO> ClientAccounts
        {
            get
            {
                return _clientaccounts;
            }
            set
            {
                _clientaccounts = value;
            }
        }
        private List<IdNameBO> _projectContractActivities;
        [Display(Name = "Contracten")]
        public List<IdNameBO> ProjectContractActivities
        {
            get
            {
                return _projectContractActivities;
            }
            set
            {
                _projectContractActivities = value;
            }
        }
        private int _selectedcontractactivity;
        public int SelectedContractActivity
        {
            get
            {
                return _selectedcontractactivity;
            }
            set
            {
                _selectedcontractactivity = value;
            }
        }
        private ChangeOrderBO _changeorder;
        public ChangeOrderBO ChangeOrder
        {
            get
            {
                return _changeorder;
            }
            set
            {
                _changeorder = value;
            }
        }
    }

    public class ProjectChangeOrderExportModel
    {
        public ProjectChangeOrderExportModel()
        {
            _project = new ProjectBO();
            _changeorder = new ChangeOrderBO();
            _projectsalessettings = new ProjectSalesSettingsBO();
            _clientaccount = new ClientAccountBO();
        }
        private ProjectBO _project;
        public ProjectBO Project
        {
            get
            {
                return _project;
            }
            set
            {
                _project = value;
            }
        }
        private ProjectSalesSettingsBO _projectsalessettings;
        public ProjectSalesSettingsBO ProjectSalesSettings
        {
            get
            {
                return _projectsalessettings;
            }
            set
            {
                _projectsalessettings = value;
            }
        }
        private ClientAccountBO _clientaccount;
        public ClientAccountBO ClientAccount
        {
            get
            {
                return _clientaccount;
            }
            set
            {
                _clientaccount = value;
            }
        }
        private string _units;
        public string Units
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

        private ChangeOrderBO _changeorder;
        public ChangeOrderBO ChangeOrder
        {
            get
            {
                return _changeorder;
            }
            set
            {
                _changeorder = value;
            }
        }

        /// <summary>Digitale handtekeningen (SigningService) die in het handtekeningblok komen — leeg op het gewone PDF.</summary>
        public List<FacadeCore.SignatureEvidence> Signatures { get; set; } = new List<FacadeCore.SignatureEvidence>();
        /// <summary>Dit PDF gaat digitaal ter ondertekening (link per e-mail): in plaats van een lege handtekeningcel een korte uitleg.</summary>
        public bool DigitalSigningPending { get; set; }
    }
}
