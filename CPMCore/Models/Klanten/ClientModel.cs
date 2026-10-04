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

        // gl-v2 (Klanten/DetailV2, design-handoff 12c): ongecapte, per-klant gefilterde lijsten voor
        // de Wijzigingsopdrachten-kaart/-KPI en de Gefactureerd/Openstaand-KPI's — ChangeOrders
        // hierboven blijft de gecapte (4) lijst die de legacy Detail.cshtml gebruikt, ongewijzigd.
        private List<ChangeOrderBO> _clientchangeorders = new List<ChangeOrderBO>();
        public List<ChangeOrderBO> ClientChangeOrders
        {
            get { return _clientchangeorders; }
            set { _clientchangeorders = value; }
        }
        private List<InvoiceListItemBO> _clientinvoices = new List<InvoiceListItemBO>();
        public List<InvoiceListItemBO> ClientInvoices
        {
            get { return _clientinvoices; }
            set { _clientinvoices = value; }
        }
        // gl-v2: zelfde vlag/reden als DetailClientsModel.IsCoordinationProject — _ProjectInnerMenuV2
        // heeft 'm nodig op elke pagina die het meerendert.
        public bool IsCoordinationProject { get; set; }
        // gl-v2: zelfde teller-conventie als Projecten/DetailClientsV2 (GlV2ProjectMenuVm.ItemCounts
        // ["Klanten"]) — het aantal klanten van het PROJECT, niet van deze ene klantfiche, zodat de
        // "Klanten"-ingang in het inner menu hier exact dezelfde teller toont als op de klantenlijst.
        public int ProjectClientCount { get; set; }
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
            _countries = new List<IdNameBO>();
            _listactivities = new List<IdNameBO>();
        }
        // gl-v2 (Klanten/EditProjectV2): we zitten nog altijd in het project, dus toont deze pagina
        // ook het projectdossier-inner menu — zelfde drie velden/reden als ClientModel hierboven
        // (ProjectName/ProjectClientCount/IsCoordinationProject), enkel op dit model herhaald omdat
        // EditClientModel een apart type is. Gevuld in FillInAddSelectListsEdit, dus op elk
        // redisplay-pad (GET én elke POST-validatiefout) opnieuw correct.
        public string ProjectName { get; set; } = "";
        public int ProjectClientCount { get; set; }
        public bool IsCoordinationProject { get; set; }
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
        [Display(Name = "Postcode")]
        public int? SelectedPostalcodeId { get; set; }
        [Display(Name = "Postcode")]
        public int? SelectedInvoicePostalcodeId { get; set; }
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
    }
}
