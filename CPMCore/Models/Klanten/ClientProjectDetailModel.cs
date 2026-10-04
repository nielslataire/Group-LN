using System.ComponentModel.DataAnnotations;
using BOCore;

namespace CPMCore.Models.Klanten
{
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

        // gl-v2: zelfde vlag als ShowProjectDetail.Project.IsOnlyCoordinationProject — het
        // projectdossier-inner-menu (GlV2/_ProjectInnerMenuV2) heeft die nodig op elke pagina die het
        // meerendert, niet enkel op Projecten/DetailV2.
        public bool IsCoordinationProject { get; set; }

        // gl-v2 (DetailClientsV2, design-handoff 12d): wooneenheden/commerciële ruimtes (Type.GroupId
        // 1/4) zonder klant — ClientAccounts hierboven komt enkel via GetClientAccountsByProjectIdWith
        // Units, dat per definitie GEEN eenheden zonder klant teruggeeft. 12d toont die eenheden zelf
        // wél als "Nog geen klant"-rij (het is een verkoopoverzicht van het project, geen kaal
        // klantenregister) — vandaar deze aparte lijst, gevuld uit IUnitService.
        public List<BOCore.UnitBO> AvailableUnits { get; set; } = new();
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
