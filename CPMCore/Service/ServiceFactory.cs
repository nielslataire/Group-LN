using FacadeCore;
using ServiceCore;
using DALCore.Models;
using DALCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CPMCore.Service
{
    public static class ServiceFactory
    {
        // Elke call krijgt een verse UoW/DbContext
        public static UnitOfWorkCore CreateUoW()
            => new UnitOfWorkCore(new cpmRunningContext());

        // ----- Non-cached getters (altijd nieuwe instance) -----
        public static IAuthenticationService GetAuthenticationService()
            => new AuthenticationService(CreateUoW());
        public static IActivityService GetActivityService()
            => new ActivityService(CreateUoW());
        public static IProvinceService GetProvinceService()
            => new ProvinceService(CreateUoW());
        public static ICompanyService GetCompanyService()
            => new CompanyService(CreateUoW());
        public static ICountryService GetCountryService()
            => new CountryService(CreateUoW());
        public static IPostalcodeService GetPostalcodeService()
            => new PostalcodeService(CreateUoW());
        public static IDepartmentService GetDepartmentService()
            => new DepartmentService(CreateUoW());
        public static IContactService GetContactService()
            => new ContactService(CreateUoW());
        public static IProjectService GetProjectService()
            => new ProjectService(CreateUoW());
        public static IUnitService GetUnitService()
            => new UnitService(CreateUoW());
        public static IClientService GetClientService()
            => new ClientService(CreateUoW());
        public static IInvoicingService GetInvoicingService()
            => new InvoicingService(CreateUoW());
        public static IInsuranceService GetInsuranceService()
            => new InsuranceService(CreateUoW());

        // ----- Overloads met meegegeven UoW (voor transacties) -----
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

        // Transactie-helper blijft werken
        public static async Task<(UnitOfWorkCore uow, IDbContextTransaction tx)> CreateUoWWithTransactionAsync()
        {
            var uow = CreateUoW();
            var tx = await uow.BeginTransactionAsync();
            return (uow, tx);
        }
    }
}
