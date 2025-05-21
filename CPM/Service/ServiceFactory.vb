Imports Facade
Imports Service

Public Class ServiceFactory

    Private Shared _authenticationService As IAuthenticationService
    Public Shared Function GetAuthenticationService() As IAuthenticationService
        If (_authenticationService Is Nothing) Then
            _authenticationService = New AuthenticationService()
        End If
        Return _authenticationService
    End Function

    Private Shared _activityService As IActivityService
    Public Shared Function GetActivityService() As IActivityService
        If (_activityService Is Nothing) Then
            _activityService = New ActivityService()
        End If
        Return _activityService
    End Function

    Private Shared _provinceService As IProvinceService
    Public Shared Function GetProvinceService() As IProvinceService
        If (_provinceService Is Nothing) Then
            _provinceService = New ProvinceService()
        End If
        Return _provinceService
    End Function

    Private Shared _companyService As ICompanyService
    Public Shared Function GetCompanyService() As ICompanyService
        If (_companyService Is Nothing) Then
            _companyService = New CompanyService()
        End If
        Return _companyService
    End Function

    Private Shared _CountryService As ICountryService
    Public Shared Function GetCountryService() As ICountryService
        If (_CountryService Is Nothing) Then
            _CountryService = New CountryService()
        End If
        Return _CountryService
    End Function

    Private Shared _PostalcodeService As IPostalcodeService
    Public Shared Function GetPostalcodeService() As IPostalcodeService
        If (_PostalcodeService Is Nothing) Then
            _PostalcodeService = New PostalcodeService()
        End If
        Return _PostalcodeService
    End Function
    Private Shared _DepartmentService As IDepartmentService
    Public Shared Function GetDepartmentService() As IDepartmentService
        If (_DepartmentService Is Nothing) Then
            _DepartmentService = New DepartmentService()
        End If
        Return _DepartmentService
    End Function
    Private Shared _ContactService As IContactService
    Public Shared Function GetContactService() As IContactService
        If (_ContactService Is Nothing) Then
            _ContactService = New ContactService()
        End If
        Return _ContactService
    End Function
    Private Shared _ProjectService As IProjectService
    Public Shared Function GetProjectService() As IProjectService
        If (_ProjectService Is Nothing) Then
            _ProjectService = New ProjectService()
        End If
        Return _ProjectService
    End Function
    Private Shared _UnitService As IUnitService
    Public Shared Function GetUnitService() As IUnitService
        If (_UnitService Is Nothing) Then
            _UnitService = New UnitService()
        End If
        Return _UnitService
    End Function
    Private Shared _ClientService As IClientService
    Public Shared Function GetClientService() As IClientService
        If (_ClientService Is Nothing) Then
            _ClientService = New ClientService()
        End If
        Return _ClientService
    End Function
    Private Shared _FacebookService As IFacebookService
    Public Shared Function GetFacebookService() As IFacebookService
        If (_FacebookService Is Nothing) Then
            _FacebookService = New FacebookService()
        End If
        Return _FacebookService
    End Function
    Private Shared _InvoicingService As IInvoicingService
    Public Shared Function GetInvoicingService() As IInvoicingService
        If (_InvoicingService Is Nothing) Then
            _InvoicingService = New InvoicingService()
        End If
        Return _InvoicingService
    End Function
    Private Shared _InsuranceService As IInsuranceService
    Public Shared Function GetInsuranceService() As IInsuranceService
        If (_InsuranceService Is Nothing) Then
            _InsuranceService = New InsuranceService()
        End If
        Return _InsuranceService
    End Function
    Private Shared _LogService As ILogService
    Public Shared Function GetLogService() As ILogService
        If (_LogService Is Nothing) Then
            _LogService = New LogService()
        End If
        Return _LogService
    End Function
End Class
