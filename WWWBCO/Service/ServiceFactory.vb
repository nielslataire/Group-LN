Imports Facade
Imports Service

Public Class ServiceFactory

    
    Private Shared _ProjectService As IProjectService
    Public Shared Function GetProjectService() As IProjectService
        If (_ProjectService Is Nothing) Then
            _ProjectService = New ProjectService()
        End If
        Return _ProjectService
    End Function
    Private Shared _CompanyService As ICompanyService
    Public Shared Function GetCompanyService() As ICompanyService
        If (_CompanyService Is Nothing) Then
            _CompanyService = New CompanyService()
        End If
        Return _CompanyService
    End Function
    Private Shared _UnitService As IUnitService
    Public Shared Function GetUnitService() As IUnitService
        If (_UnitService Is Nothing) Then
            _UnitService = New UnitService()
        End If
        Return _UnitService
    End Function
End Class
