using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using FacadeCore;
using ServiceCore;

namespace CPMCore8.Service
{
    public class ServiceFactory
    {
        private static IAuthenticationService? _authenticationService;
        public static IAuthenticationService GetAuthenticationService()
        {
            if ((_authenticationService == null))
                _authenticationService = new AuthenticationService();
            return _authenticationService;
        }

        private static IActivityService? _activityService;
        public static IActivityService GetActivityService()
        {
            if ((_activityService == null))
                _activityService = new ActivityService();
            return _activityService;
        }

        private static IProvinceService? _provinceService;
        public static IProvinceService GetProvinceService()
        {
            if ((_provinceService == null))
                _provinceService = new ProvinceService();
            return _provinceService;
        }

        private static ICompanyService? _companyService;
        public static ICompanyService GetCompanyService()
        {
            if ((_companyService == null))
                _companyService = new CompanyService();
            return _companyService;
        }

        private static ICountryService? _CountryService;
        public static ICountryService GetCountryService()
        {
            if ((_CountryService == null))
                _CountryService = new CountryService();
            return _CountryService;
        }

        private static IPostalcodeService? _PostalcodeService;
        public static IPostalcodeService GetPostalcodeService()
        {
            if ((_PostalcodeService == null))
                _PostalcodeService = new PostalcodeService();
            return _PostalcodeService;
        }
        private static IDepartmentService? _DepartmentService;
        public static IDepartmentService GetDepartmentService()
        {
            if ((_DepartmentService == null))
                _DepartmentService = new DepartmentService();
            return _DepartmentService;
        }
        private static IContactService? _ContactService;
        public static IContactService GetContactService()
        {
            if ((_ContactService == null))
                _ContactService = new ContactService();
            return _ContactService;
        }
        private static IProjectService? _ProjectService;
        public static IProjectService GetProjectService()
        {
            if ((_ProjectService == null))
                _ProjectService = new ProjectService();
            return _ProjectService;
        }
        private static IUnitService? _UnitService;
        public static IUnitService GetUnitService()
        {
            if ((_UnitService == null))
                _UnitService = new UnitService();
            return _UnitService;
        }
        private static IClientService? _ClientService;
        public static IClientService GetClientService()
        {
            if ((_ClientService == null))
                _ClientService = new ClientService();
            return _ClientService;
        }
        //private static IFacebookService _FacebookService;
        //public static IFacebookService GetFacebookService()
        //{
        //    if ((_FacebookService == null))
        //        _FacebookService = new FacebookService();
        //    return _FacebookService;
        //}
        private static IInvoicingService? _InvoicingService;
        public static IInvoicingService GetInvoicingService()
        {
            if ((_InvoicingService == null))
                _InvoicingService = new InvoicingService();
            return _InvoicingService;
        }
        private static IInsuranceService? _InsuranceService;
        public static IInsuranceService GetInsuranceService()
        {
            if ((_InsuranceService == null))
                _InsuranceService = new InsuranceService();
            return _InsuranceService;
        }
    }
}
