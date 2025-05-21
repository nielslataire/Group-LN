using System.ComponentModel.DataAnnotations;
using BOCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using AspNetCoreGeneratedDocument;

namespace CPMCore.Models.Projecten
{
    //test
    public class ProjectModel
    {
        public ProjectModel()
        {
            _project = new ProjectBO();
            _countries = new List<IdNameBO>();
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
    }

    public class EditProjectDetail
    {
        public EditProjectDetail()
        {
            _project = new ProjectBO();
            _countries = new List<IdNameBO>();
            _facebookplaces = new List<FacebookPlaceBO>();
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
        private IEnumerable<ApplicationUser> _users;
        public IEnumerable<ApplicationUser> Users
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
    }

    public class ShowProjectsModel
    {
        public ShowProjectsModel()
        {
            _projects = new List<ProjectBO>();
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
        private IEnumerable<ApplicationUser> _users;
        public IEnumerable<ApplicationUser> Users
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

    public class DetailDocsModel
    {
        public DetailDocsModel()
        {
            _docs = new List<ProjectDocBO>();
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

    public class ProjectInvoicingModel
    {
        public ProjectInvoicingModel()
        {
            _clientaccounts = new List<ClientAccountWithInvoicableBO>();
            _clientChangeOrders = new List<ClientAccountWithInvoicableChangeOrderBO>();
            _clientUtilityCosts = new List<ClientUtilityCostBO>();
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
        private List<ClientAccountWithInvoicableBO> _clientaccounts;
        public List<ClientAccountWithInvoicableBO> ClientAccounts
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
        private List<ClientAccountWithInvoicableChangeOrderBO> _clientChangeOrders;
        public List<ClientAccountWithInvoicableChangeOrderBO> ClientChangeOrders
        {
            get
            {
                return _clientChangeOrders;
            }
            set
            {
                _clientChangeOrders = value;
            }
        }
        private List<ClientUtilityCostBO> _clientUtilityCosts;
        public List<ClientUtilityCostBO> ClientUtilityCosts
        {
            get
            {
                return _clientUtilityCosts;
            }
            set
            {
                _clientUtilityCosts = value;
            }
        }
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




