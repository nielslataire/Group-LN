using BOCore;
using FacadeCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace CPMCore.Models.Home
{
    public class HomeModel
    {
        public HomeModel()
        {
            _projects = new List<ProjectBO>();
            _statuses = new List<ProjectStatusBO>();    
            _oldprojects = new List<ProjectBO>();
            m_DeedofSaleWarnings = new List<ClientAccountBO>();
        }
        private List<ProjectBO> _projects;
        public List<ProjectBO> Projects
        {
            get
            {
                return _projects;
            }
            set
            {
                _projects = value;
            }
        }
        private List<ProjectBO> _oldprojects;
        public List<ProjectBO> OldProjects
        {
            get
            {
                return _oldprojects;
            }
            set
            {
                _oldprojects = value;
            }
        }
        private IdNameBO? m_selectedsearch;
        public IdNameBO? SelectedSearch
        {
            get
            {
                return m_selectedsearch;
            }
            set
            {
                m_selectedsearch = value;
            }
        }
        private List<ClientAccountBO> m_DeedofSaleWarnings;
        public List<ClientAccountBO> DeedofSaleWarnings
        {
            get
            {
                return m_DeedofSaleWarnings;
            }
            set
            {
                m_DeedofSaleWarnings = value;
            }
        }
        private List<WarningBO>? _insurancewarnings;
        public List<WarningBO>? InsuranceWarnings
        {
            get
            {
                return _insurancewarnings;
            }
            set
            {
                _insurancewarnings = value;
            }
        }
        private List<WarningBO>? _projectInfo;
        public List<WarningBO>? ProjectInfo
        {
            get
            {
                return _projectInfo;
            }
            set
            {
                _projectInfo = value;
            }
        }
        public List<WarningBO>? ContractorCommentMeldingen { get; set; }
        public DashboardType? DashboardType { get; set; }

        /// <summary>Aantal open punten over de projecten van de gebruiker (KPI-strip projectleider-dashboard).</summary>
        public int OpenIssuesCount { get; set; }

        /// <summary>
        /// Project-ID's die de gebruiker heeft vastgezet op het projectleider-dashboard,
        /// ook als het project niet aan hem/haar is toegewezen. Deze projecten worden
        /// samengevoegd met <see cref="Projects"/>; deze set laat de view weten welke
        /// kaarten een "vastgezet" toggle moeten tonen i.p.v. een gewone toegewezen kaart.
        /// </summary>
        public HashSet<int> PinnedProjectIds { get; set; } = new();

        /// <summary>Voortgang per project-id, geladen voor het projectleider-dashboard.</summary>
        public Dictionary<int, ProjectVoortgangBO> ProjectVoortgang { get; set; } = new();

        /// <summary>
        /// Handmatige volgorde (project-id -> 0-gebaseerde positie) die de gebruiker heeft
        /// ingesteld voor "Mijn Werven" via de Rangschikken-modus. Projecten die hier niet
        /// in voorkomen zijn nooit expliciet gesorteerd en sluiten aan achteraan.
        /// </summary>
        public Dictionary<int, int> ProjectSortOrder { get; set; } = new();

        // ── Boekhouding & CeoCfo: uitgaande facturen (openstaand/vervallen) ────
        public InvoiceDashboardSummaryBO OutgoingInvoiceSummary { get; set; }

        /// <summary>Aantal inkomende facturen die actie vragen (nieuw/verrijkt/te keuren/vraagt aandacht).</summary>
        public int IncomingInvoiceActionCount { get; set; }

        /// <summary>Kleine steekproef van de dringendste inkomende facturen, voor de aandachtspaneel.</summary>
        public List<IncomingInvoiceListItemVm> IncomingInvoiceAttention { get; set; } = new();

        /// <summary>Inkomende facturen met onopgeloste documentwaarschuwingen.</summary>
        public int IncomingInvoiceWarningCount { get; set; }

        // ── CeoCfo: bedrijfsnamen (IssuerCompanyId → naam) voor de multi-company werven-grid ──
        public Dictionary<int, string> IssuerCompanyNames { get; set; } = new();

        private List<ProjectStatusBO> _statuses;
        public List<ProjectStatusBO> Statuses
        {
            get
            {
                return _statuses;
            }
            set
            {
                _statuses = value;
            }
        }

    }
}
