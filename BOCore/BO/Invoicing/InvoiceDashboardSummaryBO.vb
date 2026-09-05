Imports System.Collections.Generic

Public Class InvoiceDashboardSummaryBO
    Public Property OutstandingCount As Integer
    Public Property OutstandingAmount As Decimal
    Public Property OverdueCount As Integer
    Public Property OverdueAmount As Decimal
    Public Property TopOverdue As List(Of OverdueInvoiceLineBO) = New List(Of OverdueInvoiceLineBO)
End Class

Public Class OverdueInvoiceLineBO
    Public Property InvoiceId As Integer
    Public Property PublicId As String
    Public Property ClientName As String
    Public Property ProjectName As String
    Public Property Balance As Decimal
    Public Property ExpirationDate As DateOnly?
    Public Property DaysOverdue As Integer
End Class
