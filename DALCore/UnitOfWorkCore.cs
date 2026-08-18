using BOCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DALCore
{
    public class UnitOfWorkCore : IDisposable
    {
        private readonly cpmRunningContext _context;

        // Repositories
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
        public GenericRepository<UnitFinishingOption> UnitFinishingOptions { get; }
        public GenericRepository<ClientAccount> ClientAccounts { get; }
        public GenericRepository<ClientContacts> ClientContacts { get; }
        public GenericRepository<ContactRequests> ContactRequests { get; }
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
        public GenericRepository<Payments> Payments { get; }
        public GenericRepository<PaymentAllocations> PaymentAllocations { get; }
        public GenericRepository<InvoiceStatusLookup> InvoiceStatuses { get; }
        public GenericRepository<InvoiceSeries> InvoiceSeries { get; }
        public GenericRepository<InvoiceSequence> InvoiceSequences { get; }
        public GenericRepository<InvoiceRelations> InvoiceRelations { get; }
        public GenericRepository<InvoiceEmailLog> InvoiceEmailLogs { get; }
        public GenericRepository<InvoiceAttachments> InvoiceAttachments { get; }
        public GenericRepository<InvoiceDunning> InvoiceDunning { get; }
        public GenericRepository<InvoiceUbl> InvoiceUbl { get; }
        public GenericRepository<InvoicePdfArchive> InvoicePdfArchives { get; }
        public GenericRepository<PaymentTerms> PaymentTerms { get; }
        public GenericRepository<IssuerCompany> IssuerCompanies { get; }
        public GenericRepository<IssuerBankAccount> IssuerBankAccount { get; }
        public GenericRepository<InvoiceLayoutTemplate> InvoiceLayoutTemplates { get; }
        public GenericRepository<OctopusBookyears> OctopusBookyears { get; }
        public GenericRepository<OctopusBookyearPeriods> OctopusBookyearPeriods { get; }
        public GenericRepository<OctopusJournals> OctopusJournals { get; }
        public GenericRepository<Vattype> VatTypes { get; }
        public GenericRepository<ProjectVoortgang> ProjectVoortgangs { get; }
        public GenericRepository<PlanningSectie>   PlanningSections  { get; }
        public GenericRepository<PlanningTaak>     PlanningTasks     { get; }
        public GenericRepository<ProjectContractSlice> ProjectContractSlices { get; }
        public GenericRepository<ProjectHourlyRate>    ProjectHourlyRates    { get; }
        public GenericRepository<ProjectRegieUur>      ProjectRegieUren      { get; }
        public GenericRepository<IssuerCompanyUserRate> IssuerCompanyUserRates { get; }
        public GenericRepository<ProjectMediaSection> ProjectMediaSections { get; }
        public GenericRepository<BudgetMaster>       BudgetMasters      { get; }
        public GenericRepository<BudgetVersie>       BudgetVersies      { get; }
        public GenericRepository<BudgetGegevens>     BudgetGegevens     { get; }
        public GenericRepository<BudgetOppervlaktes>   BudgetOppervlaktes   { get; }
        public GenericRepository<BudgetGevelElementen>  BudgetGevelElementen  { get; }
        public GenericRepository<BudgetSanitair>        BudgetSanitair        { get; }
        public GenericRepository<BudgetActivityLijnen>  BudgetActivityLijnen  { get; }
        public GenericRepository<BudgetParams>          BudgetParams          { get; }
        public GenericRepository<BouwIndex>              BouwIndex             { get; }
        public GenericRepository<BudgetVerkoopLijn>     BudgetVerkoopLijn     { get; }
        public GenericRepository<BudgetPrijsReferentie> BudgetPrijsReferentie { get; }
        public GenericRepository<KmIndexType>               KmIndexTypes          { get; }
        public GenericRepository<KostprijsMateriaal>        KostprijsMaterialen   { get; }
        public GenericRepository<KostprijsFormulaKoppeling> FormulaKoppelingen    { get; }
        public GenericRepository<ProjectKostprijs>   ProjectKostprijzen   { get; }
        public GenericRepository<BouwkostPercentageGroep> BouwkostPercentageGroepen { get; }
        public GenericRepository<BouwkostPercentage>      BouwkostPercentages       { get; }
        public GenericRepository<BlogArtikel>    BlogArtikelen    { get; }
        public GenericRepository<BlogArtikelBlok> BlogArtikelBlokken { get; }
        public GenericRepository<BlogArtikelFaq>  BlogArtikelFaqItems { get; }
        public GenericRepository<Vacature>        Vacaturen           { get; }
        public GenericRepository<VacatureTaak>              VacatureTaken              { get; }
        public GenericRepository<VacatureVereiste>          VacatureVereisten          { get; }
        public GenericRepository<VacatureVoordeel>          VacatureVoordelen          { get; }
        public GenericRepository<VacatureSollicitatieStap>  VacatureSollicitatieStappen{ get; }
        public GenericRepository<EmailTemplate>       EmailTemplates       { get; }
        public GenericRepository<UserEmailSignature>  UserEmailSignatures  { get; }
        public GenericRepository<EmailSendLog>        EmailSendLogs        { get; }


        // Zorg dat services bij de DbContext kunnen (Attach/Entry/IsModified)
        public DbContext Context => _context;

        // Detectie van (expliciete) transacties die hogerop gestart zijn
        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;
        public IDbContextTransaction? CurrentTransaction => _context.Database.CurrentTransaction;

        public UnitOfWorkCore(cpmRunningContext context)
        {
            _context = context;

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
            UnitFinishingOptions = new GenericRepository<UnitFinishingOption>(_context);
            ClientAccounts = new GenericRepository<ClientAccount>(_context);
            ClientContacts = new GenericRepository<ClientContacts>(_context);
            ContactRequests = new GenericRepository<ContactRequests>(_context);
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
            Payments = new GenericRepository<Payments>(_context);
            PaymentAllocations = new GenericRepository<PaymentAllocations>(_context);
            InvoiceStatuses = new GenericRepository<InvoiceStatusLookup>(_context);
            InvoiceSeries = new GenericRepository<InvoiceSeries>(_context);
            InvoiceSequences = new GenericRepository<InvoiceSequence>(_context);
            InvoiceRelations = new GenericRepository<InvoiceRelations>(_context);
            InvoiceEmailLogs = new GenericRepository<InvoiceEmailLog>(_context);
            InvoiceAttachments = new GenericRepository<InvoiceAttachments>(_context);
            InvoiceDunning = new GenericRepository<InvoiceDunning>(_context);
            InvoiceUbl = new GenericRepository<InvoiceUbl>(_context);
            InvoicePdfArchives = new GenericRepository<InvoicePdfArchive>(_context);
            PaymentTerms = new GenericRepository<PaymentTerms>(_context);
            IssuerCompanies = new GenericRepository<IssuerCompany>(_context);
            IssuerBankAccount = new GenericRepository<IssuerBankAccount>(_context);
            InvoiceLayoutTemplates = new GenericRepository<InvoiceLayoutTemplate>(_context);
            OctopusBookyears = new GenericRepository<OctopusBookyears>(_context);
            OctopusBookyearPeriods = new GenericRepository<OctopusBookyearPeriods>(_context);
            OctopusJournals = new GenericRepository<OctopusJournals>(_context);
            VatTypes = new GenericRepository<Vattype>(_context);
            ProjectVoortgangs = new GenericRepository<ProjectVoortgang>(_context);
            PlanningSections  = new GenericRepository<PlanningSectie>(_context);
            PlanningTasks     = new GenericRepository<PlanningTaak>(_context);
            ProjectContractSlices  = new GenericRepository<ProjectContractSlice>(_context);
            ProjectHourlyRates     = new GenericRepository<ProjectHourlyRate>(_context);
            ProjectRegieUren       = new GenericRepository<ProjectRegieUur>(_context);
            IssuerCompanyUserRates = new GenericRepository<IssuerCompanyUserRate>(_context);
            ProjectMediaSections   = new GenericRepository<ProjectMediaSection>(_context);
            BudgetMasters      = new GenericRepository<BudgetMaster>(_context);
            BudgetVersies      = new GenericRepository<BudgetVersie>(_context);
            BudgetGegevens     = new GenericRepository<BudgetGegevens>(_context);
            BudgetOppervlaktes   = new GenericRepository<BudgetOppervlaktes>(_context);
            BudgetGevelElementen  = new GenericRepository<BudgetGevelElementen>(_context);
            BudgetSanitair        = new GenericRepository<BudgetSanitair>(_context);
            BudgetActivityLijnen  = new GenericRepository<BudgetActivityLijnen>(_context);
            BudgetParams          = new GenericRepository<BudgetParams>(_context);
            BouwIndex             = new GenericRepository<BouwIndex>(_context);
            BudgetVerkoopLijn     = new GenericRepository<BudgetVerkoopLijn>(_context);
            BudgetPrijsReferentie = new GenericRepository<BudgetPrijsReferentie>(_context);
            KmIndexTypes          = new GenericRepository<KmIndexType>(_context);
            KostprijsMaterialen   = new GenericRepository<KostprijsMateriaal>(_context);
            FormulaKoppelingen    = new GenericRepository<KostprijsFormulaKoppeling>(_context);
            ProjectKostprijzen   = new GenericRepository<ProjectKostprijs>(_context);
            BouwkostPercentageGroepen = new GenericRepository<BouwkostPercentageGroep>(_context);
            BouwkostPercentages       = new GenericRepository<BouwkostPercentage>(_context);
            BlogArtikelen             = new GenericRepository<BlogArtikel>(_context);
            BlogArtikelBlokken        = new GenericRepository<BlogArtikelBlok>(_context);
            BlogArtikelFaqItems       = new GenericRepository<BlogArtikelFaq>(_context);
            Vacaturen                 = new GenericRepository<Vacature>(_context);
            VacatureTaken              = new GenericRepository<VacatureTaak>(_context);
            VacatureVereisten          = new GenericRepository<VacatureVereiste>(_context);
            VacatureVoordelen          = new GenericRepository<VacatureVoordeel>(_context);
            VacatureSollicitatieStappen= new GenericRepository<VacatureSollicitatieStap>(_context);
            EmailTemplates            = new GenericRepository<EmailTemplate>(_context);
            UserEmailSignatures       = new GenericRepository<UserEmailSignature>(_context);
            EmailSendLogs             = new GenericRepository<EmailSendLog>(_context);
        }

        // Eenduidige save-methodes
        public int SaveChanges() => _context.SaveChanges();
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
        public EntityEntry Entry(object entity) => _context.Entry(entity);

        // Slim saven: alleen als er geen expliciete transactie loopt
        public int SaveIfNoActiveTransaction()
            => HasActiveTransaction ? 0 : _context.SaveChanges();

        public Task<int> SaveIfNoActiveTransactionAsync(CancellationToken ct = default)
            => HasActiveTransaction ? Task.FromResult(0) : _context.SaveChangesAsync(ct);

        // Transaction helpers
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
            => _context.Database.BeginTransactionAsync(ct);

        // Optioneel: handige wrappers als je ze wil gebruiken
        public Task CommitTransactionAsync(IDbContextTransaction tx, CancellationToken ct = default)
            => tx.CommitAsync(ct);

        public Task RollbackTransactionAsync(IDbContextTransaction tx, CancellationToken ct = default)
            => tx.RollbackAsync(ct);

        public void Dispose() => _context.Dispose();
    }
}
