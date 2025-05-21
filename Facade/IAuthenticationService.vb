Imports BO

Public Interface IAuthenticationService
    Function ValidateUser(userName As String, password As String) As GetResponse(Of Boolean)
End Interface
