Imports System.Linq.Expressions
Imports BO
Public Class ClientAccountQuery
    Public Shared Function GetNameQuery(name As String) As Expression(Of Func(Of ClientAccount, Boolean))
        If String.IsNullOrWhiteSpace(name) Then Return Nothing
        Return Function(w) w.CompanyName.Contains(name) Or w.Name.Contains(name)
    End Function
End Class
