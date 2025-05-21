Imports System.Net
Imports System.Web.Http
Imports DAL
Imports Facade
Imports BO
Imports System.Net.Http

Namespace Controllers
    Public Class CompanyController
        Inherits ApiController

        Public Function [Get](ByVal id As Integer) As CompanyBO
            Dim service = ServiceFactory.GetCompanyService()
            Dim response = service.GetCompanyByID(id)
            If (response.Success) Then
                Return response.Value
            Else
                'return errors
                Return Nothing
            End If
        End Function



        Public Sub [Delete](ByVal id As Integer)
            Dim uow As New UnitOfWork(False)
            Dim dao = uow.GetCompanyInfoDAO()

            ''UPDATE
            Dim dbobject = dao.GetById(id)
            If Not dbobject Is Nothing Then
                dao.DeleteObject(dbobject)
                uow.SaveChanges()
            End If
        End Sub

    End Class
End Namespace