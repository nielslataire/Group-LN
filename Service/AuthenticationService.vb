Imports DAL
Imports Facade
Imports BO

Public Class AuthenticationService
    Implements IAuthenticationService


    Public Function ValidateUser(userName As String, password As String) As GetResponse(Of Boolean) Implements IAuthenticationService.ValidateUser
        Dim response = New GetResponse(Of Boolean)
        response.Value = False
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUsersDAO()
        Dim _entity = dao.GetNoTracking().Where(Function(s) s.UserID = userName).FirstOrDefault()
        If (_entity IsNot Nothing) Then
            If (_entity.Password = _entity.Password) Then
                response.Value = True
            End If
        End If
        Return response
    End Function

End Class
