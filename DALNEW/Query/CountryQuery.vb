Imports System.Linq.Expressions
Imports BO
Public Class CountryQuery
    Public Shared Function GetVisibleQuery(visible As Boolean) As Expression(Of Func(Of Country, Boolean))
        Return Function(f) f.Selectable = visible
    End Function
End Class
