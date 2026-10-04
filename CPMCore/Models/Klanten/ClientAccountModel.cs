using System.ComponentModel.DataAnnotations;
using BOCore;

namespace CPMCore.Models.Klanten
{
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

        // gl-v2 (Klanten/AddClientAccountV2, design-handoff 23a/23b): de eenhedenkiezer toont ALLE
        // eenheden van het project — beschikbare kiesbaar, verkochte/in optie zichtbaar maar niet
        // kiesbaar mét de reden (koper/optiehouder). AvailableUnits hierboven blijft de legacy lijst
        // (enkel beschikbare) voor de niet-gl-v2 view.
        public List<UnitChoiceVm> UnitChoices { get; set; } = new();

        // gl-v2 (Klanten/AddClientAccountV2): zelfde afgeleide vlag/patroon als EditClientModel.IsCompany
        // hierboven — "Dit is een bedrijf"-schakelaar, bepaald uit CompanyName (geen eigen kolom nodig).
        private bool _iscompany;
        [Display(Name = "Eigenaar is een onderneming")]
        public bool IsCompany
        {
            get
            {
                if (ClientAccount is not null)
                    return ClientAccount.CompanyName != null;
                return false;
            }
            set
            {
                _iscompany = value;
            }
        }
    }

    /// <summary>Eén regel in de eenhedenkiezer van Klanten/AddClientAccountV2 (design-handoff 23b:
    /// "verkochte eenheden zichtbaar maar niet kiesbaar"). Gebouwd in KlantenController.BuildUnitChoices.</summary>
    public class UnitChoiceVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        /// <summary>"open bebouwing · 2.081 m² · Woning 6 % btw" — type, oppervlakte, betalingsgroep.</summary>
        public string Sub { get; set; } = "";
        public decimal Price { get; set; }
        public bool Available { get; set; }
        /// <summary>BESCHIKBAAR / VERKOCHT / IN OPTIE.</summary>
        public string StatusLabel { get; set; } = "";
        /// <summary>Bij niet-beschikbaar: de koper/optiehouder — dé reden waarom je 'm hier niet kan kiezen.</summary>
        public string? Reason { get; set; }
        /// <summary>Type-groep (Wooneenheden, Nevenruimtes …) voor de sortering/groepering.</summary>
        public string? Group { get; set; }
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
}
