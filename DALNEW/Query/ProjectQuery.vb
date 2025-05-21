Imports System.Linq.Expressions
Imports BO
Public Class ProjectQuery
    Public Shared Function GetNameQuery(name As String) As Expression(Of Func(Of Project, Boolean))
        If String.IsNullOrWhiteSpace(name) Then Return Nothing
        Return Function(w) w.ProjectName.Contains(name)
    End Function
End Class
