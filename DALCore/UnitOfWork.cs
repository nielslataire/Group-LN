using BOCore;
using Microsoft.EntityFrameworkCore;
using DALCore.Models;

namespace DALCore
{
    public class UnitOfWork :IDisposable 
    {
        private bool disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }
            }
            this.disposedValue = true;
        }

        // TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        // Protected Overrides Sub Finalize()
        // ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        // Dispose(False)
        // MyBase.Finalize()
        // End Sub

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private cpmRunningContext  _context;

        /// <summary>
        /// Detectchanges = true
        /// </summary>
        /// <remarks></remarks>
        public UnitOfWork()
        {
            _context = new cpmRunningContext();
        }

        //public UnitOfWork(bool detectChanges)
        //{
        //    _context = new TestdbEntities(detectChanges);
        //}

        //public UnitOfWork(bool detectChanges, string connString)
        //{
        //    _context = new TestdbEntities(detectChanges, connString);
        //}

        public List<Message> SaveChanges()
        {
            List<Message> messages = new List<Message>();
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Message m = new Message();
                m.Type = MessageType.Error;
                m.Message = ex.Message;
                messages.Add(m);
            }
            return messages;
        }
        public List<Message> DetachEntity(object entity)
        {
            List<Message> messages = new List<Message>();
            try
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
            catch (Exception ex)
            {
                Message m = new Message();
                m.Type = MessageType.Error;
                m.Message = ex.Message;
                messages.Add(m);
            }
            return messages;
        }
        private GenericDAO<CompanyContacts> _CompanyContactsDAO;
        public GenericDAO<CompanyContacts> GetCompanyContactsDAO()
        {
            if ((_CompanyContactsDAO == null))
            {
                _CompanyContactsDAO = new GenericDAO<CompanyContacts>();
                _CompanyContactsDAO.Context = _context;
            }
            return _CompanyContactsDAO;
        }
        private GenericDAO<CompanyInfo> _CompanyInfoDAO;
        public GenericDAO<CompanyInfo> GetCompanyInfoDAO()
        {
            if ((_CompanyInfoDAO == null))
            {
                _CompanyInfoDAO = new GenericDAO<CompanyInfo>();
                _CompanyInfoDAO.Context = _context;
            }
            return _CompanyInfoDAO;
        }
        private GenericDAO<CompanyDepartments> _DepartmentDAO;
        public GenericDAO<CompanyDepartments> GetDepartmentDAO()
        {
            if ((_DepartmentDAO == null))
            {
                _DepartmentDAO = new GenericDAO<CompanyDepartments>();
                _DepartmentDAO.Context = _context;
            }
            return _DepartmentDAO;
        }
        private GenericDAO<Users> _UserDAO;
        public GenericDAO<Users> GetUsersDAO()
        {
            if ((_UserDAO == null))
            {
                _UserDAO = new GenericDAO<Users>();
                _UserDAO.Context = _context;
            }
            return _UserDAO;
        }

        private GenericDAO<Activity> _ActivityDAO;
        public GenericDAO<Activity> GetActivityDAO()
        {
            if ((_ActivityDAO == null))
            {
                _ActivityDAO = new GenericDAO<Activity>();
                _ActivityDAO.Context = _context;
            }
            return _ActivityDAO;
        }
        private GenericDAO<ActivityGroup> _ActivityGroupDAO;
        public GenericDAO<ActivityGroup> GetActivityGroupDAO()
        {
            if ((_ActivityGroupDAO == null))
            {
                _ActivityGroupDAO = new GenericDAO<ActivityGroup>();
                _ActivityGroupDAO.Context = _context;
            }
            return _ActivityGroupDAO;
        }
        // --------POSTALCODE--------------
        // --------------------------------
        private GenericDAO<Provincie> _ProvinceDAO;
        public GenericDAO<Provincie> GetProvinceDAO()
        {
            if ((_ProvinceDAO == null))
            {
                _ProvinceDAO = new GenericDAO<Provincie>();
                _ProvinceDAO.Context = _context;
            }
            return _ProvinceDAO;
        }
        private GenericDAO<Country> _CountryDAO;
        public GenericDAO<Country> GetCountryDAO()
        {
            if ((_CountryDAO == null))
            {
                _CountryDAO = new GenericDAO<Country>();
                _CountryDAO.Context = _context;
            }
            return _CountryDAO;
        }
        private GenericDAO<PostalCode> _PostalcodeDAO;
        public GenericDAO<PostalCode> GetPostalcodeDAO()
        {
            if ((_PostalcodeDAO == null))
            {
                _PostalcodeDAO = new GenericDAO<PostalCode>();
                _PostalcodeDAO.Context = _context;
            }
            return _PostalcodeDAO;
        }
        // ----------PROJECTS---------------
        // ---------------------------------
        private GenericDAO<Project> _ProjectDAO;
        public GenericDAO<Project> GetProjectDAO()
        {
            if ((_ProjectDAO == null))
            {
                _ProjectDAO = new GenericDAO<Project>();
                _ProjectDAO.Context = _context;
            }
            return _ProjectDAO;
        }
        private GenericDAO<ProjectStatus> _ProjectStatusDAO;
        public GenericDAO<ProjectStatus> GetProjectStatusDAO()
        {
            if ((_ProjectStatusDAO == null))
            {
                _ProjectStatusDAO = new GenericDAO<ProjectStatus>();
                _ProjectStatusDAO.Context = _context;
            }
            return _ProjectStatusDAO;
        }
        private GenericDAO<ProjectPictures> _ProjectPicturesDAO;
        public GenericDAO<ProjectPictures> GetProjectPicturesDAO()
        {
            if ((_ProjectPicturesDAO == null))
            {
                _ProjectPicturesDAO = new GenericDAO<ProjectPictures>();
                _ProjectPicturesDAO.Context = _context;
            }
            return _ProjectPicturesDAO;
        }
        private GenericDAO<ProjectNews> _ProjectNewsDAO;
        public GenericDAO<ProjectNews> GetProjectNewsDAO()
        {
            if ((_ProjectNewsDAO == null))
            {
                _ProjectNewsDAO = new GenericDAO<ProjectNews>();
                _ProjectNewsDAO.Context = _context;
            }
            return _ProjectNewsDAO;
        }
        private GenericDAO<ProjectSalesSettings> _ProjectSalesSettingsDAO;
        public GenericDAO<ProjectSalesSettings> GetProjectSalesSettingsDAO()
        {
            if ((_ProjectSalesSettingsDAO == null))
            {
                _ProjectSalesSettingsDAO = new GenericDAO<ProjectSalesSettings>();
                _ProjectSalesSettingsDAO.Context = _context;
            }
            return _ProjectSalesSettingsDAO;
        }
        private GenericDAO<WheaterStations> _WheaterstationsDAO;
        public GenericDAO<WheaterStations> GetWheaterstationsDAO()
        {
            if ((_WheaterstationsDAO == null))
            {
                _WheaterstationsDAO = new GenericDAO<WheaterStations>();
                _WheaterstationsDAO.Context = _context;
            }
            return _WheaterstationsDAO;
        }
        private GenericDAO<BadWeatherDays> _BadWeatherDaysDAO;
        public GenericDAO<BadWeatherDays> GetBadWeatherDaysDAO()
        {
            if ((_BadWeatherDaysDAO == null))
            {
                _BadWeatherDaysDAO = new GenericDAO<BadWeatherDays>();
                _BadWeatherDaysDAO.Context = _context;
            }
            return _BadWeatherDaysDAO;
        }
        private GenericDAO<VacationDays> _VacationDaysDAO;
        public GenericDAO<VacationDays> GetVacationDaysDAO()
        {
            if ((_VacationDaysDAO == null))
            {
                _VacationDaysDAO = new GenericDAO<VacationDays>();
                _VacationDaysDAO.Context = _context;
            }
            return _VacationDaysDAO;
        }
        private GenericDAO<ProjectLevels> _ProjectLevelsDAO;
        public GenericDAO<ProjectLevels> GetProjectLevelsDAO()
        {
            if ((_ProjectLevelsDAO == null))
            {
                _ProjectLevelsDAO = new GenericDAO<ProjectLevels>();
                _ProjectLevelsDAO.Context = _context;
            }
            return _ProjectLevelsDAO;
        }
        private GenericDAO<ProjectDocs> _ProjectDocsDAO;
        public GenericDAO<ProjectDocs> GetProjectDocsDAO()
        {
            if ((_ProjectDocsDAO == null))
            {
                _ProjectDocsDAO = new GenericDAO<ProjectDocs>();
                _ProjectDocsDAO.Context = _context;
            }
            return _ProjectDocsDAO;
        }
        private GenericDAO<UtilityPercentage> _UtilityPercentageDAO;
        public GenericDAO<UtilityPercentage> GetUtilityPercentageDAO()
        {
            if ((_UtilityPercentageDAO == null))
            {
                _UtilityPercentageDAO = new GenericDAO<UtilityPercentage>();
                _UtilityPercentageDAO.Context = _context;
            }
            return _UtilityPercentageDAO;
        }

        // -------------INCOMMING INVOICES-----------
        // ------------------------------------------

        private GenericDAO<IncommingInvoices> _IncommingInvoices;
        public GenericDAO<IncommingInvoices> GetIncommingInvoicesDAO()
        {
            if ((_IncommingInvoices == null))
            {
                _IncommingInvoices = new GenericDAO<IncommingInvoices>();
                _IncommingInvoices.Context = _context;
            }
            return _IncommingInvoices;
        }
        private GenericDAO<IncommingInvoiceDetail> _IncommingInvoiceDetail;
        public GenericDAO<IncommingInvoiceDetail> GetIncommingInvoicesDetailDAO()
        {
            if ((_IncommingInvoiceDetail == null))
            {
                _IncommingInvoiceDetail = new GenericDAO<IncommingInvoiceDetail>();
                _IncommingInvoiceDetail.Context = _context;
            }
            return _IncommingInvoiceDetail;
        }

        // -------------INVOICING-----------
        // ---------------------------------
        private GenericDAO<Invoices> _Invoices;
        public GenericDAO<Invoices> GetInvoicesDAO()
        {
            if ((_Invoices == null))
            {
                _Invoices = new GenericDAO<Invoices>();
                _Invoices.Context = _context;
            }
            return _Invoices;
        }
        private GenericDAO<InvoicesDetails> _InvoicesDetails;
        public GenericDAO<InvoicesDetails> GetInvoicesDetailsDAO()
        {
            if ((_InvoicesDetails == null))
            {
                _InvoicesDetails = new GenericDAO<InvoicesDetails>();
                _InvoicesDetails.Context = _context;
            }
            return _InvoicesDetails;
        }
        private GenericDAO<InvoicingPaymentGroup> _ProjectPaymentGroups;
        public GenericDAO<InvoicingPaymentGroup> GetProjectPaymentGroupsDAO()
        {
            if ((_ProjectPaymentGroups == null))
            {
                _ProjectPaymentGroups = new GenericDAO<InvoicingPaymentGroup>();
                _ProjectPaymentGroups.Context = _context;
            }
            return _ProjectPaymentGroups;
        }
        private GenericDAO<InvoicingPaymentStages> _ProjectPaymentStages;
        public GenericDAO<InvoicingPaymentStages> GetProjectPaymentStagesDAO()
        {
            if ((_ProjectPaymentStages == null))
            {
                _ProjectPaymentStages = new GenericDAO<InvoicingPaymentStages>();
                _ProjectPaymentStages.Context = _context;
            }
            return _ProjectPaymentStages;
        }

        // ------------UNITS---------------
        // --------------------------------
        private GenericDAO<Units> _UnitsDAO;
        public GenericDAO<Units> GetUnitsDAO()
        {
            if ((_UnitsDAO == null))
            {
                _UnitsDAO = new GenericDAO<Units>();
                _UnitsDAO.Context = _context;
            }
            return _UnitsDAO;
        }
        private GenericDAO<UnitRooms> _UnitRoomsDAO;
        public GenericDAO<UnitRooms> GetUnitRoomsDAO()
        {
            if ((_UnitRoomsDAO == null))
            {
                _UnitRoomsDAO = new GenericDAO<UnitRooms>();
                _UnitRoomsDAO.Context = _context;
            }
            return _UnitRoomsDAO;
        }
        private GenericDAO<UnitTypes> _UnitTypesDAO;
        public GenericDAO<UnitTypes> GetUnitTypesDAO()
        {
            if ((_UnitTypesDAO == null))
            {
                _UnitTypesDAO = new GenericDAO<UnitTypes>();
                _UnitTypesDAO.Context = _context;
            }
            return _UnitTypesDAO;
        }
        private GenericDAO<UnitGroupTypes> _UnitGroupTypesDAO;
        public GenericDAO<UnitGroupTypes> GetUnitGroupTypesDAO()
        {
            if ((_UnitGroupTypesDAO == null))
            {
                _UnitGroupTypesDAO = new GenericDAO<UnitGroupTypes>();
                _UnitGroupTypesDAO.Context = _context;
            }
            return _UnitGroupTypesDAO;
        }
        private GenericDAO<UnitConstructionValue> _UnitConstructionValuesDAO;
        public GenericDAO<UnitConstructionValue> GetUnitConstructionValuesDAO()
        {
            if ((_UnitConstructionValuesDAO == null))
            {
                _UnitConstructionValuesDAO = new GenericDAO<UnitConstructionValue>();
                _UnitConstructionValuesDAO.Context = _context;
            }
            return _UnitConstructionValuesDAO;
        }
        // ------------CLIENTS--------------
        // ---------------------------------
        private GenericDAO<ClientAccount> _ClientAccountDAO;
        public GenericDAO<ClientAccount> GetClientAccountDAO()
        {
            if ((_ClientAccountDAO == null))
            {
                _ClientAccountDAO = new GenericDAO<ClientAccount>();
                _ClientAccountDAO.Context = _context;
            }
            return _ClientAccountDAO;
        }
        private GenericDAO<ClientContacts> _ClientContactsDAO;
        public GenericDAO<ClientContacts> GetClientContactsDAO()
        {
            if ((_ClientContactsDAO == null))
            {
                _ClientContactsDAO = new GenericDAO<ClientContacts>();
                _ClientContactsDAO.Context = _context;
            }
            return _ClientContactsDAO;
        }
        private GenericDAO<ClientOwnerType> _ClientOwnerTypeDAO;
        public GenericDAO<ClientOwnerType> GetClientOwnerTypeDAO()
        {
            if ((_ClientOwnerTypeDAO == null))
            {
                _ClientOwnerTypeDAO = new GenericDAO<ClientOwnerType>();
                _ClientOwnerTypeDAO.Context = _context;
            }
            return _ClientOwnerTypeDAO;
        }
        private GenericDAO<ClientGift> _ClientGiftDAO;
        public GenericDAO<ClientGift> GetClientGiftDAO()
        {
            if ((_ClientGiftDAO == null))
            {
                _ClientGiftDAO = new GenericDAO<ClientGift>();
                _ClientGiftDAO.Context = _context;
            }
            return _ClientGiftDAO;
        }
        private GenericDAO<ClientPoa> _ClientPoaDAO;
        public GenericDAO<ClientPoa> GetClientPoaDAO()
        {
            if ((_ClientPoaDAO == null))
            {
                _ClientPoaDAO = new GenericDAO<ClientPoa>();
                _ClientPoaDAO.Context = _context;
            }
            return _ClientPoaDAO;
        }
        // ------------CONTRACTS------------
        // ---------------------------------
        private GenericDAO<Contract> _ContractDAO;
        public GenericDAO<Contract> GetContractDAO()
        {
            if ((_ContractDAO == null))
            {
                _ContractDAO = new GenericDAO<Contract>();
                _ContractDAO.Context = _context;
            }
            return _ContractDAO;
        }
        private GenericDAO<ContractActivity> _ContractActivityDAO;
        public GenericDAO<ContractActivity> GetContractActivityDAO()
        {
            if ((_ContractActivityDAO == null))
            {
                _ContractActivityDAO = new GenericDAO<ContractActivity>();
                _ContractActivityDAO.Context = _context;
            }
            return _ContractActivityDAO;
        }
        // ------------BUDGET---------------
        // ---------------------------------
        private GenericDAO<ProjectBudget> _BudgetDAO;
        public GenericDAO<ProjectBudget> GetBudgetDAO()
        {
            if ((_BudgetDAO == null))
            {
                _BudgetDAO = new GenericDAO<ProjectBudget>();
                _BudgetDAO.Context = _context;
            }
            return _BudgetDAO;
        }
        // ------------CHANGEORDER----------
        // ---------------------------------
        private GenericDAO<ChangeOrder> _ChangeOrderDAO;
        public GenericDAO<ChangeOrder> GetChangeOrderDAO()
        {
            if ((_ChangeOrderDAO == null))
            {
                _ChangeOrderDAO = new GenericDAO<ChangeOrder>();
                _ChangeOrderDAO.Context = _context;
            }
            return _ChangeOrderDAO;
        }
        private GenericDAO<ChangeOrderDetail> _ChangeOrderDetailDAO;
        public GenericDAO<ChangeOrderDetail> GetChangeOrderDetailDAO()
        {
            if ((_ChangeOrderDetailDAO == null))
            {
                _ChangeOrderDetailDAO = new GenericDAO<ChangeOrderDetail>();
                _ChangeOrderDetailDAO.Context = _context;
            }
            return _ChangeOrderDetailDAO;
        }

        // ------------INSURANCE----------
        // ---------------------------------
        private GenericDAO<Insurances> _InsuranceDAO;
        public GenericDAO<Insurances> GetInsuranceDAO()
        {
            if ((_InsuranceDAO == null))
            {
                _InsuranceDAO = new GenericDAO<Insurances>();
                _InsuranceDAO.Context = _context;
            }
            return _InsuranceDAO;
        }
        private GenericDAO<InsuranceCompanies> _InsuranceCompaniesDAO;
        public GenericDAO<InsuranceCompanies> GetInsuranceCompaniesDAO()
        {
            if ((_InsuranceCompaniesDAO == null))
            {
                _InsuranceCompaniesDAO = new GenericDAO<InsuranceCompanies>();
                _InsuranceCompaniesDAO.Context = _context;
            }
            return _InsuranceCompaniesDAO;
        }
    }
}
