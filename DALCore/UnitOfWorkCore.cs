using System;
using BOCore;
using Castle.Core.Resource;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace DALCore
{
    public class UnitOfWorkCore : IDisposable
    {
        private readonly cpmRunningContext _context;
        public GenericRepository<CompanyContacts> CompanyContacts { get; }
        public GenericRepository<CompanyInfo> CompanyInfo { get; }
        public GenericRepository<CompanyDepartments> Departments { get; }
        public GenericRepository<Users> Users { get; }
        public GenericRepository<Activity> Activities { get; }
        public GenericRepository<ActivityGroup> ActivityGroups { get; }
        public GenericRepository<Provincie> Provinces { get; }
        public GenericRepository<Country> Countries { get; }
        public GenericRepository<PostalCode> PostalCodes { get; }
        public GenericRepository<Project> Projects { get; }
        public GenericRepository<ProjectStatus> ProjectStatuses { get; }
        public GenericRepository<ProjectPictures> ProjectPictures { get; }
        public GenericRepository<ProjectNews> ProjectNews { get; }
        public GenericRepository<ProjectSalesSettings> ProjectSalesSettings { get; }
        public GenericRepository<WheaterStations> WheaterStations { get; }
        public GenericRepository<BadWeatherDays> BadWeatherDays { get; }
        public GenericRepository<VacationDays> VacationDays { get; }
        public GenericRepository<ProjectLevels> ProjectLevels { get; }
        public GenericRepository<ProjectDocs> ProjectDocs { get; }
        public GenericRepository<UtilityPercentage> UtilityPercentages { get; }
        public GenericRepository<IncommingInvoices> IncommingInvoices { get; }
        public GenericRepository<IncommingInvoiceDetail> IncommingInvoiceDetails { get; }
        public GenericRepository<Invoices> Invoices { get; }
        public GenericRepository<InvoicesDetails> InvoiceDetails { get; }
        public GenericRepository<InvoicingPaymentGroup> PaymentGroups { get; }
        public GenericRepository<InvoicingPaymentStages> PaymentStages { get; }
        public GenericRepository<Units> Units { get; }
        public GenericRepository<UnitRooms> UnitRooms { get; }
        public GenericRepository<UnitTypes> UnitTypes { get; }
        public GenericRepository<UnitGroupTypes> UnitGroupTypes { get; }
        public GenericRepository<UnitConstructionValue> UnitConstructionValues { get; }
        public GenericRepository<ClientAccount> ClientAccounts { get; }
        public GenericRepository<ClientContacts> ClientContacts { get; }
        public GenericRepository<ClientOwnerType> ClientOwnerTypes { get; }
        public GenericRepository<ClientGift> ClientGifts { get; }
        public GenericRepository<ClientPoa> ClientPoas { get; }
        public GenericRepository<Contract> Contracts { get; }
        public GenericRepository<ContractActivity> ContractActivities { get; }
        public GenericRepository<ProjectBudget> Budgets { get; }
        public GenericRepository<ChangeOrder> ChangeOrders { get; }
        public GenericRepository<ChangeOrderDetail> ChangeOrderDetails { get; }
        public GenericRepository<Insurances> Insurances { get; }
        public GenericRepository<InsuranceCompanies> InsuranceCompanies { get; }



        public UnitOfWorkCore(cpmRunningContext context)
        {
            _context = context;

            // Initialiseer repositories
            CompanyContacts = new GenericRepository<CompanyContacts>(_context);
            CompanyInfo = new GenericRepository<CompanyInfo>(_context);
            Departments = new GenericRepository<CompanyDepartments>(_context);
            Users = new GenericRepository<Users>(_context);
            Activities = new GenericRepository<Activity>(_context);
            ActivityGroups = new GenericRepository<ActivityGroup>(_context);
            Provinces = new GenericRepository<Provincie>(_context);
            Countries = new GenericRepository<Country>(_context);
            PostalCodes = new GenericRepository<PostalCode>(_context);
            Projects = new GenericRepository<Project>(_context);
            ProjectStatuses = new GenericRepository<ProjectStatus>(_context);
            ProjectPictures = new GenericRepository<ProjectPictures>(_context);
            ProjectNews = new GenericRepository<ProjectNews>(_context);
            ProjectSalesSettings = new GenericRepository<ProjectSalesSettings>(_context);
            WheaterStations = new GenericRepository<WheaterStations>(_context);
            BadWeatherDays = new GenericRepository<BadWeatherDays>(_context);
            VacationDays = new GenericRepository<VacationDays>(_context);
            ProjectLevels = new GenericRepository<ProjectLevels>(_context);
            ProjectDocs = new GenericRepository<ProjectDocs>(_context);
            UtilityPercentages = new GenericRepository<UtilityPercentage>(_context);
            IncommingInvoices = new GenericRepository<IncommingInvoices>(_context);
            IncommingInvoiceDetails = new GenericRepository<IncommingInvoiceDetail>(_context);
            Invoices = new GenericRepository<Invoices>(_context);
            InvoiceDetails = new GenericRepository<InvoicesDetails>(_context);
            PaymentGroups = new GenericRepository<InvoicingPaymentGroup>(_context);
            PaymentStages = new GenericRepository<InvoicingPaymentStages>(_context);
            Units = new GenericRepository<Units>(_context);
            UnitRooms = new GenericRepository<UnitRooms>(_context);
            UnitTypes = new GenericRepository<UnitTypes>(_context);
            UnitGroupTypes = new GenericRepository<UnitGroupTypes>(_context);
            UnitConstructionValues = new GenericRepository<UnitConstructionValue>(_context);
            ClientAccounts = new GenericRepository<ClientAccount>(_context);
            ClientContacts = new GenericRepository<ClientContacts>(_context);
            ClientOwnerTypes = new GenericRepository<ClientOwnerType>(_context);
            ClientGifts = new GenericRepository<ClientGift>(_context);
            ClientPoas = new GenericRepository<ClientPoa>(_context);
            Contracts = new GenericRepository<Contract>(_context);
            ContractActivities = new GenericRepository<ContractActivity>(_context);
            Budgets = new GenericRepository<ProjectBudget>(_context);
            ChangeOrders = new GenericRepository<ChangeOrder>(_context);
            ChangeOrderDetails = new GenericRepository<ChangeOrderDetail>(_context);
            Insurances = new GenericRepository<Insurances>(_context);
            InsuranceCompanies = new GenericRepository<InsuranceCompanies>(_context);

        }


        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}