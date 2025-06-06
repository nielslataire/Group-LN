using System.ComponentModel.DataAnnotations;
using BOCore;

namespace CPMCore.Models.Klanten
{
    public class ClientModel
    {
        public ClientModel()
        {
            _clientAccount = new ClientAccountBO();
            _unitsgrouped = new List<GroupUnitsBO>();
            _gifts = new List<ClientGiftBO>();
            _poas = new List<ClientPoaBO>();
            _changeorders = new List<ChangeOrderBO>();
        }
        private ClientAccountBO _clientAccount;
        public ClientAccountBO Client
        {
            get
            {
                return _clientAccount;
            }
            set
            {
                _clientAccount = value;
            }
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
        private List<ClientGiftBO> _gifts;
        public List<ClientGiftBO> Gifts
        {
            get
            {
                return _gifts;
            }
            set
            {
                _gifts = value;
            }
        }
        private List<ClientPoaBO> _poas;
        public List<ClientPoaBO> Poas
        {
            get
            {
                return _poas;
            }
            set
            {
                _poas = value;
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
        private string _folder;
        public string Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                _folder = value;
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
    }

    public class EditClientModel
    {
        public EditClientModel()
        {
            _clientAccount = new ClientAccountBO();
            _units = new List<UnitBO>();
            _ownertypes = new List<IdNameBO>();
            _selectedPostalCode = new PostalcodeModel();
            _selectedInvoicePostalCode = new PostalcodeModel();
            _gifts = new List<ClientGiftBO>();
            _poas = new List<ClientPoaBO>();
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
        private ClientAccountBO _clientAccount;
        public ClientAccountBO Client
        {
            get
            {
                return _clientAccount;
            }
            set
            {
                _clientAccount = value;
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
        private List<IdNameBO> _ownertypes;
        public List<IdNameBO> OwnerTypes
        {
            get
            {
                return _ownertypes;
            }
            set
            {
                _ownertypes = value;
            }
        }
        private List<ClientGiftBO> _gifts;
        public List<ClientGiftBO> Gifts
        {
            get
            {
                return _gifts;
            }
            set
            {
                _gifts = value;
            }
        }
        private List<ClientPoaBO> _poas;
        public List<ClientPoaBO> Poas
        {
            get
            {
                return _poas;
            }
            set
            {
                _poas = value;
            }
        }
        private PostalcodeModel _selectedPostalCode;
        [UIHint("Postalcode")]
        public PostalcodeModel SelectedPostalcode
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
        private PostalcodeModel _selectedInvoicePostalCode;
        [UIHint("Postalcode")]
        public PostalcodeModel SelectedInvoicePostalcode
        {
            get
            {
                return _selectedInvoicePostalCode;
            }
            set
            {
                _selectedInvoicePostalCode = value;
            }
        }
        private bool _iscompany;
        [Display(Name = "Eigenaar is een onderneming")]
        public bool IsCompany
        {
            get
            {
                if (Client is not null)
                {
                    if (Client.CompanyName != null)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            set
            {
                _iscompany = value;
            }
        }
    }

    public class AddClientAccountModel
    {
        public AddClientAccountModel()
        {
            _countries = new List<IdNameBO>();
            _clientaccount = new ClientAccountBO();
            _ownertypes = new List<IdNameBO>();
            _availableunits = new List<IdNameBO>();
            _selectedUnits = new List<int>();
            _addedUnits = new List<UnitBO>();
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
        private List<IdNameBO> _ownertypes;
        public List<IdNameBO> OwnerTypes
        {
            get
            {
                return _ownertypes;
            }
            set
            {
                _ownertypes = value;
            }
        }
        private List<IdNameBO> _availableunits;
        public List<IdNameBO> AvailableUnits
        {
            get
            {
                return _availableunits;
            }
            set
            {
                _availableunits = value;
            }
        }
        private int _selectedcoownertype;
        public int SelectedCoOwnerType
        {
            get
            {
                return _selectedcoownertype;
            }
            set
            {
                _selectedcoownertype = value;
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

        private int _selectedInvoiceCountry;
        public int SelectedInvoiceCountry
        {
            get
            {
                return _selectedInvoiceCountry;
            }
            set
            {
                _selectedInvoiceCountry = value;
            }
        }
        private int _selectedInvoicePostalCode;
        public int SelectedInvoicePostalcode
        {
            get
            {
                return _selectedInvoicePostalCode;
            }
            set
            {
                _selectedInvoicePostalCode = value;
            }
        }
        private int _selectedCoOwnerCountry;
        public int SelectedCoOwnerCountry
        {
            get
            {
                return _selectedCoOwnerCountry;
            }
            set
            {
                _selectedCoOwnerCountry = value;
            }
        }
        private int _selectedCoOwnerPostalCode;
        public int SelectedCoOwnerPostalCode
        {
            get
            {
                return _selectedCoOwnerPostalCode;
            }
            set
            {
                _selectedCoOwnerPostalCode = value;
            }
        }
        private int _selectedCoOwnerInvoiceCountry;
        public int SelectedCoOwnerInvoiceCountry
        {
            get
            {
                return _selectedCoOwnerInvoiceCountry;
            }
            set
            {
                _selectedCoOwnerInvoiceCountry = value;
            }
        }
        private int _selectedCoOwnerInvoicePostalCode;
        public int SelectedCoOwnerInvoicePostalCode
        {
            get
            {
                return _selectedCoOwnerInvoicePostalCode;
            }
            set
            {
                _selectedCoOwnerInvoicePostalCode = value;
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
        private List<UnitBO> _addedUnits;
        public List<UnitBO> AddedUnits
        {
            get
            {
                return _addedUnits;
            }
            set
            {
                _addedUnits = value;
            }
        }
        private Salutation _salutations;
        public Salutation Salutations
        {
            get
            {
                return _salutations;
            }
            set
            {
                _salutations = value;
            }
        }
    }

    public class AddUpdateClientCoOwnerModel
    {
        public AddUpdateClientCoOwnerModel()
        {
            _coowner = new ClientContactBO();
            _ownertypes = new List<IdNameBO>();
            _selectedCoOwnerPostalCode = new PostalcodeModel();
            _selectedCoOwnerInvoicePostalCode = new PostalcodeModel();
        }
        private ClientContactBO _coowner;
        public ClientContactBO CoOwner
        {
            get
            {
                return _coowner;
            }
            set
            {
                _coowner = value;
            }
        }
        private int _selectedcoownertype;
        public int SelectedCoOwnerType
        {
            get
            {
                return _selectedcoownertype;
            }
            set
            {
                _selectedcoownertype = value;
            }
        }


        private List<IdNameBO> _ownertypes;
        public List<IdNameBO> OwnerTypes
        {
            get
            {
                return _ownertypes;
            }
            set
            {
                _ownertypes = value;
            }
        }

        private PostalcodeModel _selectedCoOwnerPostalCode;
        [UIHint("Postalcode")]
        public PostalcodeModel SelectedCoOwnerPostalCode
        {
            get
            {
                return _selectedCoOwnerPostalCode;
            }
            set
            {
                _selectedCoOwnerPostalCode = value;
            }
        }

        private PostalcodeModel _selectedCoOwnerInvoicePostalCode;
        [UIHint("Postalcode")]
        public PostalcodeModel SelectedCoOwnerInvoicePostalCode
        {
            get
            {
                return _selectedCoOwnerInvoicePostalCode;
            }
            set
            {
                _selectedCoOwnerInvoicePostalCode = value;
            }
        }
        private bool _iscompany;
        [Display(Name = "Mede-eigenaar is een onderneming")]
        public bool IsCompany
        {
            get
            {
                if (CoOwner.CompanyName != null)
                    return true;
                else
                    return false;
            }
            set
            {
                _iscompany = value;
            }
        }
        private decimal _maxcoownerpercentage;
        public decimal MaxCoOwnerPercentage
        {
            get
            {
                return _maxcoownerpercentage;
            }
            set
            {
                _maxcoownerpercentage = value;
            }
        }
    }

    public class AddUnitToClientModel
    {
        public AddUnitToClientModel()
        {
            _unit = new UnitBO();
            _availableunits = new List<IdNameBO>();
            _availableprojects = new List<IdNameBO>();
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

        private int _selectedunit;
        public int SelectedUnit
        {
            get
            {
                return _selectedunit;
            }
            set
            {
                _selectedunit = value;
            }
        }
        private List<IdNameBO> _availableunits;
        public List<IdNameBO> AvailableUnits
        {
            get
            {
                return _availableunits;
            }
            set
            {
                _availableunits = value;
            }
        }
        private int _selectedproject;
        public int SelectedProject
        {
            get
            {
                return _selectedproject;
            }
            set
            {
                _selectedproject = value;
            }
        }
        private List<IdNameBO> _availableprojects;
        public List<IdNameBO> AvailableProjects
        {
            get
            {
                return _availableprojects;
            }
            set
            {
                _availableprojects = value;
            }
        }
    }

    public class AddGiftToClientModel
    {
        public AddGiftToClientModel()
        {
            _gift = new ClientGiftBO();
            _listactivities = new List<IdNameBO>();
            _selectedActivities = new List<int>();
        }
        private ClientGiftBO _gift;
        public ClientGiftBO Gift
        {
            get
            {
                return _gift;
            }
            set
            {
                _gift = value;
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
    }

    public class AddPoaToClientModel
    {
        public AddPoaToClientModel()
        {
            _poa = new ClientPoaBO();
            _listactivities = new List<IdNameBO>();
            _selectedActivities = new List<int>();
        }
        private ClientPoaBO _poa;
        public ClientPoaBO POA
        {
            get
            {
                return _poa;
            }
            set
            {
                _poa = value;
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
    }

    public class DetailClientsModel
    {
        public DetailClientsModel()
        {
            _clientaccounts = new List<ClientAccountWithUnitsBO>();
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

        private List<ClientAccountWithUnitsBO> _clientaccounts;
        public List<ClientAccountWithUnitsBO> ClientAccounts
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
    }

    public class ClientCalendarModel
    {
        public ClientCalendarModel()
        {
            _days = new List<CalendarDayBO>();
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
        private int _executiondays;
        [Display(Name = "Uitvoeringstermijn")]
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
        [UIHint("Date")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Display(Name = "Aanvangsdatum")]
        public DateOnly Startdate
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

        private int _weatherstationid;
        public int WeatherStationId
        {
            get
            {
                return _weatherstationid;
            }
            set
            {
                _weatherstationid = value;
            }
        }
        private List<CalendarDayBO> _days;
        public List<CalendarDayBO> Days
        {
            get
            {
                return _days;
            }
            set
            {
                _days = value;
            }
        }
    }

    public class DetailClientsExportModel
    {
        public DetailClientsExportModel()
        {
            _clientaccounts = new List<ClientAccountWithUnitsBO>();
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

        private List<ClientAccountWithUnitsBO> _clientaccounts;
        public List<ClientAccountWithUnitsBO> ClientAccounts
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
        private List<UnitTypeBO> _unitTypes;
        public List<UnitTypeBO> UnitTypes
        {
            get
            {
                return _unitTypes;
            }
            set
            {
                _unitTypes = value;
            }
        }
    }

    public class DetailClientsGiftsModel
    {
        public DetailClientsGiftsModel()
        {
            _clientgifts = new List<ClientGiftWithAccountDetailsBO>();
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

        private List<ClientGiftWithAccountDetailsBO> _clientgifts;
        public List<ClientGiftWithAccountDetailsBO> ClientGifts
        {
            get
            {
                return _clientgifts;
            }
            set
            {
                _clientgifts = value;
            }
        }
        private List<ActivityBO> _selectedactivities;
        public List<ActivityBO> SelectedActivities
        {
            get
            {
                return _selectedactivities;
            }
            set
            {
                _selectedactivities = value;
            }
        }
    }

    public class DetailClientsPoasModel
    {
        public DetailClientsPoasModel()
        {
            _clientpoas = new List<ClientPoaWithAccountDetailsBO>();
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

        private List<ClientPoaWithAccountDetailsBO> _clientpoas;
        public List<ClientPoaWithAccountDetailsBO> ClientPoas
        {
            get
            {
                return _clientpoas;
            }
            set
            {
                _clientpoas = value;
            }
        }
        private List<ActivityBO> _selectedactivities;
        public List<ActivityBO> SelectedActivities
        {
            get
            {
                return _selectedactivities;
            }
            set
            {
                _selectedactivities = value;
            }
        }
    }

    public class DetailInvoicingModel
    {
        public DetailInvoicingModel()
        {
            _invoices = new List<InvoiceBO>();
            _client = new ClientAccountBO();
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

    public class ExportGiftsToPdfModel
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
        private List<int> _selectedactivities;
        public List<int> SelectedActivities
        {
            get
            {
                return _selectedactivities;
            }
            set
            {
                _selectedactivities = value;
            }
        }
    }

    public class ExportPoasToPdfModel
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
        private List<int> _selectedactivities;
        public List<int> SelectedActivities
        {
            get
            {
                return _selectedactivities;
            }
            set
            {
                _selectedactivities = value;
            }
        }
    }
    // Opleveringsgegevens opslaan
    public class DeliveryModel
    {
        private int _clientid;
        public int ClientId
        {
            get
            {
                return _clientid;
            }
            set
            {
                _clientid = value;
            }
        }
        private string _projectid;
        public string ProjectId
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
        private DateTime? _deliverydate;
        [UIHint("Date")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Opleverdatum")]
        public DateTime? DeliveryDate
        {
            get
            {
                return _deliverydate;
            }
            set
            {
                _deliverydate = value;
            }
        }
        private string _deliverydoc;
        public string DeliveryDoc
        {
            get
            {
                return _deliverydoc;
            }
            set
            {
                _deliverydoc = value;
            }
        }
    }

}
