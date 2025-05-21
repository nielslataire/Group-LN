Imports Facade
Imports Service
Public NotInheritable Class ServiceFactory

    Private Shared _companyService As ICompanyService
    Public Shared Function GetCompanyService() As ICompanyService
        If (_companyService Is Nothing) Then _companyService = New CompanyService()
        Return _companyService
    End Function

End Class
