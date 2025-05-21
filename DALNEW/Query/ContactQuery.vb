Imports System.Linq.Expressions
Imports BO
Public Class ContactQuery
    Public Shared Function GetNameQuery(name As String) As Expression(Of Func(Of CompanyContacts, Boolean))
        If String.IsNullOrWhiteSpace(name) Then Return Nothing
        Return Function(w) w.ContactNaam.Contains(name) Or w.ContactVoornaam.Contains(name)
    End Function
End Class
