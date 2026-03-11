using AspNetCoreGeneratedDocument;
using BOCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System;

namespace CPMCore.Models.Projecten
{
    //test
    public class ProjectModel
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
    }

    public class EditProjectDetail
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
    }

    public class ProjectGridRenderModel
    {
        public IEnumerable<ProjectBO> Projects { get; set; } = new List<ProjectBO>();

        public List<ProjectStatusBO> Statuses { get; set; } = new List<ProjectStatusBO>();

        public Dictionary<int, ProjectSalesDataBO> SalesData { get; set; } = new Dictionary<int, ProjectSalesDataBO>();
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
            _latestpicture = new ProjectPictureBO();
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

        private ProjectPictureBO _latestpicture;
        public ProjectPictureBO LatestPicture
        {
            get
            {
                return _latestpicture;
            }
            set
            {
                _latestpicture = value;
            }
        }

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
    }

    public class DetailPhotosModel
    {
        public DetailPhotosModel()
        {
            _photos = new List<ProjectPictureBO>();
        }
        private List<ProjectPictureBO> _photos;
        public List<ProjectPictureBO> Photos
        {
            get
            {
                return _photos;
            }
            set
            {
                _photos = value;
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

    public class DetailNewsModel
    {
        public DetailNewsModel()
        {
            _news = new List<ProjectNewsBO>();
        }
        private List<ProjectNewsBO> _news;
        public List<ProjectNewsBO> News
        {
            get
            {
                return _news;
            }
            set
            {
                _news = value;
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

    public class DetailUnitsModel
    {
        public DetailUnitsModel()
        {
            _projectunits = new List<UnitBO>();
            _unitsgrouped = new List<GroupUnitsBO>();
            _addunit = new UnitBO();
            _constructionvalues = new List<UnitConstructionValueBO>();
            _grouptypes = new List<UnitGroupTypeBO>();
            _types = new List<UnitTypeBO>();

            _units = new List<IdNameBO>();
            _attachableunits = new List<IdNameBO>();
            _paymentgroups = new List<IdNameBO>();
        }
        private List<GroupUnitsBO> _unitsgrouped;
        public List<GroupUnitsBO> UnitsGrouped
        {
            get
            {
                return _unitsgrouped;
            }
            set
            {
                _unitsgrouped = value;
            }
        }
        private List<UnitBO> _projectunits;
        public List<UnitBO> ProjectUnits
        {
            get
            {
                return _projectunits;
            }
            set
            {
                _projectunits = value;
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
        private int _projectlandshare;
        public int ProjectLandShare
        {
            get
            {
                return _projectlandshare;
            }
            set
            {
                _projectlandshare = value;
            }
        }

        private UnitBO _addunit;
        public UnitBO AddUnit
        {
            get
            {
                return _addunit;
            }
            set
            {
                _addunit = value;
            }
        }
        private List<UnitConstructionValueBO> _constructionvalues;
        public List<UnitConstructionValueBO> ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }
        private List<UnitGroupTypeBO> _grouptypes;
        public List<UnitGroupTypeBO> GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO> _types;
        public List<UnitTypeBO> Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
            }
        }

        private List<IdNameBO> _units;
        public List<IdNameBO> Units
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
        private List<IdNameBO> _attachableunits;
        public List<IdNameBO> AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<int> _selectedUnits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
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

        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
    }
    public class DetailContactsModel
    {
        public DetailContactsModel()
        {
            _contacts = new List<ContactRequestBO>();
            _contactGroups = new List<ContactGroupModel>();
            _stats = new ContactStatsModel();
        }

        private List<ContactRequestBO> _contacts;
        public List<ContactRequestBO> Contacts
        {
            get { return _contacts; }
            set { _contacts = value; }
        }
        private List<ContactGroupModel> _contactGroups;
        public List<ContactGroupModel> ContactGroups
        {
            get { return _contactGroups; }
            set { _contactGroups = value; }
        }

        private ContactStatsModel _stats;
        public ContactStatsModel Stats
        {
            get { return _stats; }
            set { _stats = value; }
        }

        private int _projectid;
        public int ProjectId
        {
            get { return _projectid; }
            set { _projectid = value; }
        }

        private string _projectname;
        public string ProjectName
        {
            get { return _projectname; }
            set { _projectname = value; }
        }
    }
    public class ContactGroupModel
    {
        public string GroupKey { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime LatestContactAt { get; set; }
        public string LatestRequestType { get; set; }
        public string LatestSourceSite { get; set; }
        public string LatestOrigin { get; set; }
        public int TotalRequests { get; set; }
        public string LatestStatus { get; set; }
        public string LatestStatusComment { get; set; }
        public DateTime? LatestStatusAt { get; set; }
    }

    public class ContactStatsModel
    {
        public int TotalContacts { get; set; }
        public int ActiveContacts { get; set; }
        public int NewContactsWeek { get; set; }
        public int NewContactsMonth { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ResponseRate { get; set; }
    }

    public class ContactDetailsModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public ContactGroupModel Contact { get; set; }
        public List<ContactRequestBO> Requests { get; set; } = new List<ContactRequestBO>();
        public ContactActionInputModel NewAction { get; set; } = new ContactActionInputModel();
        public ContactStatusInputModel NewStatus { get; set; } = new ContactStatusInputModel();
    }

    public class ContactActionInputModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string ActionType { get; set; }
        public DateTime ActionDate { get; set; }
        public TimeSpan ActionTime { get; set; }
        public string Comment { get; set; }
    }

    public class ContactStatusInputModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime StatusDate { get; set; }
        public TimeSpan StatusTime { get; set; }
        public string Comment { get; set; }
    }

    public class ContactEditModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string NewEmail { get; set; }
        public string NewPhone { get; set; }
        public DateTime? ContactDate { get; set; }
        public string ContactMethod { get; set; }

    }

    public class ContactDeleteModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
    }
    public class ContactAddModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Comment { get; set; }
        public DateTime? ContactDate { get; set; }
        public string ContactMethod { get; set; }
    }
    public class DetailDocsModel
    {
        public DetailDocsModel()
        {
            _docs = new List<ProjectDocBO>();
            Clients = Array.Empty<IdNameBO>();
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
        private ProjectDocBO _uploaddoc;
        public ProjectDocBO UploadDoc
        {
            get
            {
                return _uploaddoc;
            }
            set
            {
                _uploaddoc = value;
            }
        }
        public IReadOnlyList<IdNameBO> Clients { get; set; }
    }
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
    }

    // UNITS
    public class AddUnitModel
    {
        public AddUnitModel()
        {
            _addunit = new UnitBO();
            _constructionvalues = new List<UnitConstructionValueBO>();
            _grouptypes = new List<UnitGroupTypeBO>();
            _types = new List<UnitTypeBO>();
            _attachableunits = new List<IdNameBO>();
            _paymentgroups = new List<IdNameBO>();
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
        private int _projectlandshare;
        public int ProjectLandShare
        {
            get
            {
                return _projectlandshare;
            }
            set
            {
                _projectlandshare = value;
            }
        }

        private UnitBO _addunit;
        public UnitBO AddUnit
        {
            get
            {
                return _addunit;
            }
            set
            {
                _addunit = value;
            }
        }
        private List<UnitConstructionValueBO> _constructionvalues;
        public List<UnitConstructionValueBO> ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }
        private List<UnitGroupTypeBO> _grouptypes;
        public List<UnitGroupTypeBO> GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO> _types;
        public List<UnitTypeBO> Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
            }
        }

        private List<IdNameBO> _attachableunits;
        public List<IdNameBO> AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<int> _selectedUnits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
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

        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
    }
    public class EditUnitModel
    {
        public EditUnitModel()
        {
            _units = new List<IdNameBO>();
            _selectedUnits = new List<int>();
            _attachableunits = new List<IdNameBO>();
            _rooms = new List<RoomBO>();
            _executionPlans = new List<UnitExecutionPlanVm>();
        }
        private UnitBO _unit;
        public UnitBO Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                _unit = value;
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

        private List<UnitGroupTypeBO>? _grouptypes;
        public List<UnitGroupTypeBO>? GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO>? _types;
        public List<UnitTypeBO>? Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
            }
        }

        private List<IdNameBO>? _units;
        public List<IdNameBO>? Units
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
        private List<int>? _selectedUnits;
        public List<int>? SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
            }
        }
        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        private List<IdNameBO>? _attachableunits;
        public List<IdNameBO>? AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<RoomBO>? _rooms;
        public List<RoomBO>? Rooms
        {
            get
            {
                return _rooms;
            }
            set
            {
                _rooms = value;
            }
        }
        private List<UnitExecutionPlanVm> _executionPlans;
        public List<UnitExecutionPlanVm> ExecutionPlans
        {
            get { return _executionPlans; }
            set { _executionPlans = value; }
        }
        // PaymentGroup
        private int? _selectedpaymentgroup;

        public int? SelectedPaymentGroup
        {
            get
            {
                return _selectedpaymentgroup;
            }
            set
            {
                _selectedpaymentgroup = value;
            }
        }
        private List<IdNameBO>? _PaymentGroups;
        public List<IdNameBO>? PaymentGroups
        {
            get
            {
                return _PaymentGroups;
            }
            set
            {
                _PaymentGroups = value;
            }
        }
        private List<UnitConstructionValueBO>? _constructionvalues;
        public List<UnitConstructionValueBO>? ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }
    }

    public class UnitExecutionPlanVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string? Url { get; set; }
    }


    public class AddUnitLinkModel
    {
        public AddUnitLinkModel()
        {
            _units = new List<IdNameBO>();
            _selectedunits = new List<int>();
        }
        private UnitBO _selectedUnit;
        public UnitBO SelectedUnit
        {
            get
            {
                return _selectedUnit;
            }
            set
            {
                _selectedUnit = value;
            }
        }
        private List<IdNameBO> _units;
        public List<IdNameBO> Units
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
        private List<int> _selectedunits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedunits;
            }
            set
            {
                _selectedunits = value;
            }
        }
        private UnitBO _unit;
        public UnitBO Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                _unit = value;
            }
        }
    }

    public class AddUnitConstructionValueModel
    {
        public AddUnitConstructionValueModel()
        {
            _constructionvalue = new UnitConstructionValueBO();
            _paymentgroups = new List<IdNameBO>();
        }
        private UnitConstructionValueBO _constructionvalue;
        public UnitConstructionValueBO ConstructionValue
        {
            get
            {
                return _constructionvalue;
            }
            set
            {
                _constructionvalue = value;
            }
        }
        private List<IdNameBO> _paymentgroups;
        public List<IdNameBO> Paymentgroups
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

    public class ProjectVacationDaysModel
    {
        public ProjectVacationDaysModel()
        {
        }
        private int _projectId;
        public int ProjectID
        {
            get
            {
                return _projectId;
            }
            set
            {
                _projectId = value;
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

    public class BWDModel
    {
        public BWDModel()
        {
            _weatherstations = new List<IdNameBO>();
        }
        private List<IdNameBO> _weatherstations;
        public List<IdNameBO> WeatherStations
        {
            get
            {
                return _weatherstations;
            }
            set
            {
                _weatherstations = value;
            }
        }
        private int _selectedweatherstation;
        public int SelectedWeatherStation
        {
            get
            {
                return _selectedweatherstation;
            }
            set
            {
                _selectedweatherstation = value;
            }
        }
    }
    // SALES
    public class ProjectSalesModel
    {
        public ProjectSalesModel()
        {
            _unitsgrouped = new List<GroupUnitsWithAttachedUnitsBO>();
            _projectunits = new List<UnitWithAttachedUnitsBO>();
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

        private List<GroupUnitsWithAttachedUnitsBO> _unitsgrouped;
        public List<GroupUnitsWithAttachedUnitsBO> UnitsGrouped
        {
            get
            {
                return _unitsgrouped;
            }
            set
            {
                _unitsgrouped = value;
            }
        }
        private List<UnitWithAttachedUnitsBO> _projectunits;
        public List<UnitWithAttachedUnitsBO> ProjectUnits
        {
            get
            {
                return _projectunits;
            }
            set
            {
                _projectunits = value;
            }
        }
    }
    public class ProjectSalesSettingsModel
    {
        public ProjectSalesSettingsModel()
        {

        }

        public List<SelectListItem> BankAccounts { get; set; } = new();
        public string? NewBankAccountIban { get; set; }
        public bool MissingBuilder { get; set; }
        public string? BuilderWarning { get; set; }
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
        private ProjectSalesSettingsBO _settings;
        public ProjectSalesSettingsBO Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
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


    }

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

    // Spiegel je BO-enum:
    public enum RegistrationType
    {
        Vat = 0,            // enkel BTW (op grond + bouw)
        Registration = 1,   // enkel registratierechten (op grond + bouw)
        Mixed = 2           // BTW op bouw + registratie op grond
    }

    public class ProjectSalesExportModel
    {
        public ProjectSalesExportModel()
        {
            _unitsgrouped = new List<GroupUnitsWithAttachedUnitsWithDetailsBO>();
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
        private List<GroupUnitsWithAttachedUnitsWithDetailsBO> _unitsgrouped;
        public List<GroupUnitsWithAttachedUnitsWithDetailsBO> UnitsGrouped
        {
            get
            {
                return _unitsgrouped;
            }
            set
            {
                _unitsgrouped = value;
            }
        }
        private List<RoomType> _surfacetypes;
        public List<RoomType> SurfaceTypes
        {
            get
            {
                return _surfacetypes;
            }
            set
            {
                _surfacetypes = value;
            }
        }
    }

    public class ProjectSalesSelectForPriceModel
    {
        public ProjectSalesSelectForPriceModel()
        {
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
        private List<IdNameBO> _units;
        public List<IdNameBO> Units
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
        private List<int> _selectedunits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedunits;
            }
            set
            {
                _selectedunits = value;
            }
        }
    }

    public class ProjectSalesCalculatePrice
    {
        public ProjectSalesCalculatePrice()
        {
            _units = new List<UnitWithReductionBO>();
            _reductions = new List<ConstructionReductionBO>();
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
        private List<UnitWithReductionBO> _units;
        public List<UnitWithReductionBO> Units
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
        private List<ConstructionReductionBO> _reductions;
        public List<ConstructionReductionBO> Reductions
        {
            get
            {
                return _reductions;
            }
            set
            {
                _reductions = value;
            }
        }
        private bool _abatement;
        public bool Abatement
        {
            get
            {
                return _abatement;
            }
            set
            {
                _abatement = value;
            }
        }
        private bool _raisedabatement;
        public bool RaisedAbatement
        {
            get
            {
                return _raisedabatement;
            }
            set
            {
                _raisedabatement = value;
            }
        }
        private bool _oneandownhome;
        public bool OneAndOwnHome
        {
            get
            {
                return _oneandownhome;
            }
            set
            {
                _oneandownhome = value;
            }
        }
    }

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
    }

    public class ProjectIncommingInvoiceAddUpdateModel
    {
        public ProjectIncommingInvoiceAddUpdateModel()
        {
            _projectContracts = new List<IdNameBO>();
            _incomminginvoice = new IncommingInvoiceBO();
            _activities = new List<Select2DTO>();
            _listactivities = new List<IdNameBO>();
            _selectedActivities = new List<int>();
        }
        private int _type;
        public int Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        private string? _companyname;
        public string? CompanyName
        {
            get
            {
                return _companyname;
            }
            set
            {
                _companyname = value;
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
        private IncommingInvoiceBO _incomminginvoice;
        public IncommingInvoiceBO IncommingInvoice
        {
            get
            {
                return _incomminginvoice;
            }
            set
            {
                _incomminginvoice = value;
            }
        }
        private List<IdNameBO> _projectContracts;
        [Display(Name = "Contracten")]
        public List<IdNameBO> ProjectContracts
        {
            get
            {
                return _projectContracts;
            }
            set
            {
                _projectContracts = value;
            }
        }
        private int _selectedcontract;
        public int SelectedContract
        {
            get
            {
                return _selectedcontract;
            }
            set
            {
                _selectedcontract = value;
            }
        }
        private List<Select2DTO> _activities;
        [Display(Name = "Activiteiten")]
        public List<Select2DTO> Activities
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
        private List<IdNameBO> _listactivities;
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
        public decimal TotaalPrijsLijnen
        {
            get
            {
                return IncommingInvoice.Details.Sum(m => m.Price);
            }
        }
    }
    public class ProjectIncommingInvoiceModel
    {
        public ProjectIncommingInvoiceModel()
        {
            _incomminginvoice = new IncommingInvoiceBO();
            _company = new CompanyBO();
            _contract = new ContractBO();
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
        private IncommingInvoiceBO _incomminginvoice;
        public IncommingInvoiceBO IncommingInvoice
        {
            get
            {
                return _incomminginvoice;
            }
            set
            {
                _incomminginvoice = value;
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

    public class DetailInsurancesModel
    {
        public DetailInsurancesModel()
        {
            _insurances = new List<InsuranceBO>();
        }
        private List<InsuranceBO> _insurances;
        public List<InsuranceBO> Insurances
        {
            get
            {
                return _insurances;
            }
            set
            {
                _insurances = value;
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

    public class ProjectAddInsurancesModel
    {
        public ProjectAddInsurancesModel()
        {
            _insurance = new InsuranceBO();
            _brokers = new List<IdNameBO>();
            _companies = new List<IdNameBO>();
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
        private List<IdNameBO> _companies;
        [Display(Name = "Maatschappij")]
        public List<IdNameBO> Companies
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
        private List<IdNameBO> _brokers;
        [Display(Name = "Makelaar")]
        public List<IdNameBO> Brokers
        {
            get
            {
                return _brokers;
            }
            set
            {
                _brokers = value;
            }
        }
    }

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
        public ContractBO Contract { get; set; }
        public IdNameBO Company { get; set; }
        public decimal TotalInvoiced { get; set; }
        public bool HasContract => Contract != null;
    }

    public class ProjectContractDetailModel
    {
        public ProjectContractDetailModel()
        {
            _contract = new ContractBO();
            _company = new CompanyBO();
            _incommingInvoices = new List<IncommingInvoiceBO>();
        }

        public bool HasContract { get; set; }

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
}




