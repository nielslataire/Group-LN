// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Core". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    //test
    public class ProjectModel : IProjectFormModel
    {
        public ProjectModel()
        {
            _project = new ProjectBO();
            _countries = new List<IdNameBO>();
            IssuerCompanies = new List<ProjectIssuerCompanyOptionVM>();
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
        private List<IdNameBO> _countries;
        public List<IdNameBO> Countries
        {
            get
            {
                return _countries;
            }
            set
            {
                _countries = value;
            }
        }
        private int _selectedCountry;
        public int SelectedCountry
        {
            get
            {
                return _selectedCountry;
            }
            set
            {
                _selectedCountry = value;
            }
        }
        private int _selectedPostalCode;
        public int SelectedPostalcode
        {
            get
            {
                return _selectedPostalCode;
            }
            set
            {
                _selectedPostalCode = value;
            }
        }
        public List<ProjectIssuerCompanyOptionVM> IssuerCompanies { get; set; }

        [ValidateNever]
        public IFormFile? StandardFotoUpload { get; set; }

        // Coördinatieproject - contract schijven (JSON serialized voor form binding)
        [ValidateNever]
        public List<ProjectContractSliceVM> ContractSlices { get; set; } = new List<ProjectContractSliceVM>();

        // Coördinatieproject - uurtarieven (JSON serialized voor form binding)
        [ValidateNever]
        public List<ProjectHourlyRateVM> HourlyRates { get; set; } = new List<ProjectHourlyRateVM>();

        // Beschikbare gebruikers voor uurtarieven
        [ValidateNever]
        public List<IdNameBO> AvailableUsers { get; set; } = new List<IdNameBO>();

        // Interne gebruikers voor projectleider/verkoopverantwoordelijke-keuzelijsten
        [ValidateNever]
        public IEnumerable<CpmUserOption> Users { get; set; }
    }

    public class EditProjectDetail : IProjectFormModel
    {
        public EditProjectDetail()
        {
            _project = new ProjectBO();
            _countries = new List<IdNameBO>();
            _facebookplaces = new List<FacebookPlaceBO>();
            IssuerCompanies = new List<ProjectIssuerCompanyOptionVM>();
        }
        // Projectgegevens
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
        private ImageBO _image;
        [ValidateNever]
        public ImageBO Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }
        private IFormFile _imageupload;
        [DataType(DataType.Upload)]
        [ValidateNever]
        public IFormFile ImageUpload
        {
            get
            {
                return _imageupload;
            }
            set
            {
                _imageupload = value;
            }
        }
        private List<IdNameBO> _countries;
        [ValidateNever]
        public List<IdNameBO> Countries
        {
            get
            {
                return _countries;
            }
            set
            {
                _countries = value;
            }
        }
        private IEnumerable<CpmUserOption> _users;
        [ValidateNever]
        public IEnumerable<CpmUserOption> Users
        {
            get
            {
                return _users;
            }
            set
            {
                _users = value;
            }
        }
        private int _selectedCountry;
        public int SelectedCountry
        {
            get
            {
                return _selectedCountry;
            }
            set
            {
                _selectedCountry = value;
            }
        }
        private int _selectedPostalCode;
        public int SelectedPostalcode
        {
            get
            {
                return _selectedPostalCode;
            }
            set
            {
                _selectedPostalCode = value;
            }
        }
        private bool _generaldataeditmode;
        public bool GeneralDataEditMode
        {
            get
            {
                return _generaldataeditmode;
            }
            set
            {
                _generaldataeditmode = value;
            }
        }
        private int _selectedStatus;
        public int SelectedStatus
        {
            get
            {
                return _selectedStatus;
            }
            set
            {
                _selectedStatus = value;
            }
        }
        private List<IdNameBO> _statuses;
        [ValidateNever]
        public List<IdNameBO> Statuses
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
        private List<FacebookPlaceBO> _facebookplaces;
        [ValidateNever]
        public List<FacebookPlaceBO> FacebookPlaces
        {
            get
            {
                return _facebookplaces;
            }
            set
            {
                _facebookplaces = value;
            }
        }
        private FacebookPlaceBO _selectedfacebookplace;
        public List<ProjectIssuerCompanyOptionVM> IssuerCompanies { get; set; }

        [ValidateNever]
        public IFormFile? StandardFotoUpload { get; set; }

        [ValidateNever]
        public FacebookPlaceBO SelectedFacebookPlace
        {
            get
            {
                return _selectedfacebookplace;
            }
            set
            {
                _selectedfacebookplace = value;
            }
        }
        private List<ProjectDocBO> _docs;
        [ValidateNever]
        public List<ProjectDocBO> Docs
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

        // Coördinatieproject - contract schijven
        [ValidateNever]
        public List<ProjectContractSliceVM> ContractSlices { get; set; } = new List<ProjectContractSliceVM>();

        // Coördinatieproject - uurtarieven
        [ValidateNever]
        public List<ProjectHourlyRateVM> HourlyRates { get; set; } = new List<ProjectHourlyRateVM>();

        // Website-inhoud (publieke projectpagina): locatie, titel, subtitel, inleiding, eigen kerncijfers.
        // Eigen tabellen (ProjectWebsite/ProjectWebsiteKpi); Website.Present = true wanneer het formulier ze meestuurt.
        [ValidateNever]
        public FacadeCore.ProjectWebsiteDto Website { get; set; } = new FacadeCore.ProjectWebsiteDto();

        // Beschikbare gebruikers voor uurtarieven
        [ValidateNever]
        public List<IdNameBO> AvailableUsers { get; set; } = new List<IdNameBO>();
    }

    public class ShowProjectsModel
    {
        public ShowProjectsModel()
        {
            _projects = new List<ProjectBO>();
            _salesData = new Dictionary<int, ProjectSalesDataBO>();
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
        private Dictionary<int, ProjectSalesDataBO> _salesData;
        public Dictionary<int, ProjectSalesDataBO> SalesData
        {
            get
            {
                return _salesData;
            }
            set
            {
                _salesData = value;
            }
        }
        public int TotalProjectCount { get; set; }

        public int VisibleProjectCount { get; set; }

        public int InitialLimit { get; set; }

        public int BatchSize { get; set; }

        public bool HasMoreProjects => TotalProjectCount > VisibleProjectCount;

        public int RemainingProjectCount => Math.Max(0, TotalProjectCount - VisibleProjectCount);

        public Dictionary<int, ProjectVoortgangBO> Voortgang { get; set; } = new Dictionary<int, ProjectVoortgangBO>();
    }

    public class ProjectGridRenderModel
    {
        public IEnumerable<ProjectBO> Projects { get; set; } = new List<ProjectBO>();

        public List<ProjectStatusBO> Statuses { get; set; } = new List<ProjectStatusBO>();

        public Dictionary<int, ProjectSalesDataBO> SalesData { get; set; } = new Dictionary<int, ProjectSalesDataBO>();

        public Dictionary<int, ProjectVoortgangBO> Voortgang { get; set; } = new Dictionary<int, ProjectVoortgangBO>();
    }

    // Detail
    public class ShowProjectDetail
    {
        public ShowProjectDetail()
        {
            _project = new ProjectBO();
            _countries = new List<IdNameBO>();
            _facebookplaces = new List<FacebookPlaceBO>();
            _docs = new List<ProjectDocBO>();
            _recentclients = new List<IdNameBO>();
            _LatestNews = new ProjectNewsBO();
            _latestDocs = new List<ProjectDocBO>();
        }
        // Projectgegevens
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
        private ImageBO _image;
        public ImageBO Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }
        private IFormFile _imageupload;
        [DataType(DataType.Upload)]
        public IFormFile ImageUpload
        {
            get
            {
                return _imageupload;
            }
            set
            {
                _imageupload = value;
            }
        }
        private List<IdNameBO> _countries;
        public List<IdNameBO> Countries
        {
            get
            {
                return _countries;
            }
            set
            {
                _countries = value;
            }
        }
        private IEnumerable<CpmUserOption> _users;
        public IEnumerable<CpmUserOption> Users
        {
            get
            {
                return _users;
            }
            set
            {
                _users = value;
            }
        }
        private int _selectedCountry;
        public int SelectedCountry
        {
            get
            {
                return _selectedCountry;
            }
            set
            {
                _selectedCountry = value;
            }
        }
        private int _selectedPostalCode;
        public int SelectedPostalcode
        {
            get
            {
                return _selectedPostalCode;
            }
            set
            {
                _selectedPostalCode = value;
            }
        }
        private bool _generaldataeditmode;
        public bool GeneralDataEditMode
        {
            get
            {
                return _generaldataeditmode;
            }
            set
            {
                _generaldataeditmode = value;
            }
        }
        private int _selectedStatus;
        public int SelectedStatus
        {
            get
            {
                return _selectedStatus;
            }
            set
            {
                _selectedStatus = value;
            }
        }
        private List<IdNameBO> _statuses;
        public List<IdNameBO> Statuses
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
        private List<FacebookPlaceBO> _facebookplaces;
        public List<FacebookPlaceBO> FacebookPlaces
        {
            get
            {
                return _facebookplaces;
            }
            set
            {
                _facebookplaces = value;
            }
        }
        private FacebookPlaceBO _selectedfacebookplace;
        public FacebookPlaceBO SelectedFacebookPlace
        {
            get
            {
                return _selectedfacebookplace;
            }
            set
            {
                _selectedfacebookplace = value;
            }
        }
        private List<ProjectDocBO> _docs;
        public List<ProjectDocBO> Docs
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
        private int _executiondays;
        public int ExecutionDays
        {
            get
            {
                return _executiondays;
            }
            set
            {
                _executiondays = value;
            }
        }
        private DateOnly _startdate;
        public DateOnly StartDate
        {
            get
            {
                return _startdate;
            }
            set
            {
                _startdate = value;
            }
        }
        private DateOnly _finalconstructiondate;
        public DateOnly FinalConstructionDate
        {
            get
            {
                return _finalconstructiondate;
            }
            set
            {
                _finalconstructiondate = value;
            }
        }
        private int _workingdaysleft;
        public int WorkingDaysLeft
        {
            get
            {
                return _workingdaysleft;
            }
            set
            {
                _workingdaysleft = value;
            }
        }
        private List<IdNameBO> _recentclients;
        public List<IdNameBO> RecentClients
        {
            get
            {
                return _recentclients;
            }
            set
            {
                _recentclients = value;
            }
        }
        private ProjectNewsBO _LatestNews;
        public ProjectNewsBO LatestNews
        {
            get
            {
                return _LatestNews;
            }
            set
            {
                _LatestNews = value;
            }
        }

        /// <summary>Recentste project-foto's (max. 4), voor de media-grid op de hub.</summary>
        public List<ProjectPictureBO> LatestPictures { get; set; } = new();

        /// <summary>Totaal aantal foto's/video's van dit project (voor de "+N"-tegel op de media-grid).</summary>
        public int TotalPictureCount { get; set; }

        private List<ProjectDocBO> _latestDocs;
        public List<ProjectDocBO> LatestDocs
        {
            get
            {
                return _latestDocs;
            }
            set
            {
                _latestDocs = value;
            }
        }

        /// <summary>Fysieke/financiële voortgang van dit project; null als er nog geen berekening bestaat.</summary>
        public ProjectVoortgangBO Voortgang { get; set; }

        /// <summary>Aantal openstaande punten (construction issues) op dit project.</summary>
        public int OpenIssuesCount { get; set; }

        /// <summary>Openstaande punten (actieve statussen), hoogste prioriteit eerst — voor het aandachtspaneel.</summary>
        public List<DALCore.Models.ConstructionIssue> OpenIssues { get; set; } = new();

        /// <summary>Contracten op dit project die nog niet getekend zijn.</summary>
        public List<ContractBO> UnsignedContracts { get; set; } = new();

        /// <summary>Contracten op dit project met een ontbrekend waarborgdocument.</summary>
        public List<ContractBO> GuaranteeMissingContracts { get; set; } = new();

        /// <summary>Verzekeringswaarschuwingen voor dit project.</summary>
        public List<WarningBO> ProjectInsuranceWarnings { get; set; } = new();

        /// <summary>Nog niet bereikte/n.v.t. trajectmijlpalen van dit project — voor het aandachtspaneel.</summary>
        public List<DALCore.Models.Mijlpaal> AttentionMijlpalen { get; set; } = new();

        /// <summary>De 3 reële polissen (ABR/Brand/10-jarige) van dit project, voor de Verzekeringen-kaart.</summary>
        public List<InsuranceBO> Insurances { get; set; } = new();

        /// <summary>Verkoop-aggregaat (verkocht/potentieel, in aantal en waarde) — zelfde bron als de projectenlijst.</summary>
        public ProjectSalesDataBO SalesData { get; set; }

        /// <summary>Eén rij per eenheid met status en klant, voor de Eenheden &amp; verkoopstatus-tabel.</summary>
        public List<ProjectDetailUnitRowVM> UnitRows { get; set; } = new();

        /// <summary>Recentste vorderingsstaten/facturen van dit project.</summary>
        public List<InvoiceListItemBO> RecentInvoices { get; set; } = new();

        /// <summary>Openstaand/vervallen-overzicht van dit project (zelfde definitie als de Boekhouding/CEO-dashboards).</summary>
        public InvoiceDashboardSummaryBO ProjectInvoiceSummary { get; set; }
    }

    /// <summary>Bewerken/Verwijderen-knoppen voor een project, gedeeld tussen de topbar
    /// (@section PageActions) en de mobiele fallback-rij in de content-body.</summary>
    public class ProjectActionButtonsVM
    {
        public int ProjectId { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>Eén rij van de Eenheden &amp; verkoopstatus-tabel op de Detail-hub.</summary>
    public class ProjectDetailUnitRowVM
    {
        public int UnitId { get; set; }
        public string Naam { get; set; }
        public string TypeName { get; set; }
        public decimal? Oppervlakte { get; set; }
        /// <summary>Verkoopprijs (grond + constructie, ValueSold-velden) als verkocht, anders vraagprijs.
        /// Zonder afwerkingen: grond + alle constructieprijzen. Met afwerkingen: enkel de grondwaarde —
        /// het volledige bedrag per afwerking staat in Afwerkingen (Vraagprijs + Cost). Zie
        /// ServiceCore.Helpers.UnitPricing.</summary>
        public decimal Vraagprijs { get; set; }
        /// <summary>De afwerkingen van de eenheid (leeg zonder afwerkingen of als verkocht): naam +
        /// het TOTAAL van hun constructieprijzen, elk een volledig alternatief bovenop Vraagprijs.</summary>
        public List<(string Description, decimal Cost)> Afwerkingen { get; set; } = new();
        /// <summary>"Beschikbaar" | "In optie" | "Verkocht" | "Akte verleden".</summary>
        public string Status { get; set; }
        /// <summary>Bootstrap-badge-variant voor Status: success/warning/danger/primary.</summary>
        public string StatusVariant { get; set; }
        public string KlantNaam { get; set; }
        public int? ClientId { get; set; }
    }
}

namespace CPMCore.Models.Projecten
{
    // Eén rij in de lijsten van de tab "SEO & website" (verhaalbeeld, quote, detail) — zie Views/Projecten/Core/_WebsiteRow.cshtml.
    public class WebsiteRowVm
    {
        public string Kind { get; set; }          // "story" | "quote" | "detail"
        public string Prefix { get; set; }        // bv. "Website.StoryItems"
        public string Index { get; set; }         // rijnummer of "__i__" voor de template
        public string ImageName { get; set; }
        public string ImageBase { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string Person { get; set; }
    }
}
