using FacadeCore;
using ServiceCore;
using DALCore.Models;
using DALCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CPMCore.Service
{
    public static class ServiceFactory
    {
        // De gedeelde (legacy) UoW voor singleton-getters.
        // Gebruik voor transacties liever CreateUoW() en de CreateXxxService(uow) overloads.
        private static UnitOfWorkCore _uow = new UnitOfWorkCore(new cpmRunningContext());

        // Singleton velden
        private static IAuthenticationService? _authenticationService;
        private static IActivityService? _activityService;
        private static IProvinceService? _provinceService;
        private static ICompanyService? _companyService;
        private static ICountryService? _countryService;
        private static IPostalcodeService? _postalcodeService;
        private static IDepartmentService? _departmentService;
        private static IContactService? _contactService;
        private static IProjectService? _projectService;
        private static IUnitService? _unitService;
        private static IClientService? _clientService;
        private static IInvoicingService? _invoicingService;
        private static IInsuranceService? _insuranceService;


        // Generieke helper om duplicatie te vermijden
        private static TService GetOrCreate<TService>(ref TService? field, Func<UnitOfWorkCore, TService> factory)
            where TService : class
        {
            return field ??= factory(_uow);
        }

        // ====== Legacy singleton getters (gebruiken de gedeelde _uow) ======
        public static IAuthenticationService GetAuthenticationService()
            => GetOrCreate(ref _authenticationService, uow => new AuthenticationService(uow));

        public static IActivityService GetActivityService()
            => GetOrCreate(ref _activityService, uow => new ActivityService(uow));

        public static IProvinceService GetProvinceService()
            => GetOrCreate(ref _provinceService, uow => new ProvinceService(uow));

        public static ICompanyService GetCompanyService()
            => GetOrCreate(ref _companyService, uow => new CompanyService(uow));

        public static ICountryService GetCountryService()
            => GetOrCreate(ref _countryService, uow => new CountryService(uow));

        public static IPostalcodeService GetPostalcodeService()
            => GetOrCreate(ref _postalcodeService, uow => new PostalcodeService(uow));

        public static IDepartmentService GetDepartmentService()
            => GetOrCreate(ref _departmentService, uow => new DepartmentService(uow));

        public static IContactService GetContactService()
            => GetOrCreate(ref _contactService, uow => new ContactService(uow));

        public static IProjectService GetProjectService()
            => GetOrCreate(ref _projectService, uow => new ProjectService(uow));

        public static IUnitService GetUnitService()
            => GetOrCreate(ref _unitService, uow => new UnitService(uow));

        public static IClientService GetClientService()
            => GetOrCreate(ref _clientService, uow => new ClientService(uow));

        public static IInvoicingService GetInvoicingService()
            => GetOrCreate(ref _invoicingService, uow => new InvoicingService(uow));

        public static IInsuranceService GetInsuranceService()
            => GetOrCreate(ref _insuranceService, uow => new InsuranceService(uow));

        // ====== Overloads voor transacties: instanties met een meegegeven UoW ======
        public static UnitOfWorkCore CreateUoW()
            => new UnitOfWorkCore(new cpmRunningContext());

        public static IAuthenticationService CreateAuthenticationService(UnitOfWorkCore uow)
            => new AuthenticationService(uow);
        public static IActivityService CreateActivityService(UnitOfWorkCore uow)
            => new ActivityService(uow);
        public static IProvinceService CreateProvinceService(UnitOfWorkCore uow)
            => new ProvinceService(uow);
        public static ICompanyService CreateCompanyService(UnitOfWorkCore uow)
            => new CompanyService(uow);
        public static ICountryService CreateCountryService(UnitOfWorkCore uow)
            => new CountryService(uow);
        public static IPostalcodeService CreatePostalcodeService(UnitOfWorkCore uow)
            => new PostalcodeService(uow);
        public static IDepartmentService CreateDepartmentService(UnitOfWorkCore uow)
            => new DepartmentService(uow);
        public static IContactService CreateContactService(UnitOfWorkCore uow)
            => new ContactService(uow);
        public static IProjectService CreateProjectService(UnitOfWorkCore uow)
            => new ProjectService(uow);
        public static IUnitService CreateUnitService(UnitOfWorkCore uow)
            => new UnitService(uow);
        public static IClientService CreateClientService(UnitOfWorkCore uow)
            => new ClientService(uow);
        public static IInvoicingService CreateInvoicingService(UnitOfWorkCore uow)
            => new InvoicingService(uow);
        public static IInsuranceService CreateInsuranceService(UnitOfWorkCore uow)
            => new InsuranceService(uow);

        // ====== Optioneel: helpers om gedeelde UoW te wisselen/cleanen ======
        /// <summary>
        /// Vervang de gedeelde UoW (alle toekomstige singleton-getters gaan de nieuwe UoW gebruiken).
        /// Let op: bestaande singleton services houden hun oude UoW vast tot je ResetSingletons() aanroept.
        /// </summary>
        public static void ReplaceUoW(UnitOfWorkCore newUow)
        {
            _uow = newUow ?? throw new ArgumentNullException(nameof(newUow));
        }

        /// <summary>
        /// Reset alle singleton service-instanties zodat ze bij de volgende GetXxxService()-aanroep
        /// met de huidige _uow opnieuw worden aangemaakt.
        /// </summary>
        public static void ResetSingletons()
        {
            _authenticationService = null;
            _activityService = null;
            _provinceService = null;
            _companyService = null;
            _countryService = null;
            _postalcodeService = null;
            _departmentService = null;
            _contactService = null;
            _projectService = null;
            _unitService = null;
            _clientService = null;
            _invoicingService = null;
            _insuranceService = null;
        }
        public static async Task<(UnitOfWorkCore uow, IDbContextTransaction tx)> CreateUoWWithTransactionAsync()
        {
            var uow = CreateUoW();
            var tx = await uow.BeginTransactionAsync();
            return (uow, tx);
        }
    }
}
