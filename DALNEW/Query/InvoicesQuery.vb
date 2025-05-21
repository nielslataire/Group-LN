Imports System.Linq.Expressions
Imports BO
Public Class InvoicesQuery


    Public Shared Function GetUnitsQuery(units As List(Of Integer)) As Expression(Of Func(Of Invoices, Boolean))
        If units Is Nothing OrElse units.Count = 0 Then Return Nothing
        Dim query As Expression(Of Func(Of Invoices, Boolean)) = Nothing
        For Each unit In units
            Dim id As Integer = unit
            query = query.Or(Function(w) w.InvoicesDetails.Any(Function(a) a.UnitId = id))
        Next
        Return query
    End Function


End Class
