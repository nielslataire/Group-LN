Imports System.Collections.Generic

Public Class InvoiceDashboardSummaryBO
    Public Property OutstandingCount As Integer
    Public Property OutstandingAmount As Decimal
    Public Property OverdueCount As Integer
    Public Property OverdueAmount As Decimal
    Public Property TopOverdue As List(Of OverdueInvoiceLineBO) = New List(Of OverdueInvoiceLineBO)
    ''' <summary>Openstaand saldo dat nog niet vervallen is (of geen vervaldatum heeft).</summary>
    Public Property AmountNotDue As Decimal
    ''' <summary>Vervallen saldo, 1 tot 30 dagen over tijd.</summary>
    Public Property AmountOverdue1To30 As Decimal
    ''' <summary>Vervallen saldo, 31 tot 60 dagen over tijd.</summary>
    Public Property AmountOverdue31To60 As Decimal
    ''' <summary>Vervallen saldo, meer dan 60 dagen over tijd.</summary>
    Public Property AmountOverdueOver60 As Decimal
    ''' <summary>Aantal dagen over tijd van de oudst vervallen factuur (0 als er geen vervallen zijn).</summary>
    Public Property OldestOverdueDays As Integer
    ''' <summary>Openstaand en vervallen per facturatiebedrijf (alleen bedrijven met een openstaand saldo).</summary>
    Public Property ByCompany As List(Of IssuerSummaryLineBO) = New List(Of IssuerSummaryLineBO)
End Class

Public Class OverdueInvoiceLineBO
    Public Property InvoiceId As Integer
    Public Property PublicId As String
    Public Property ClientName As String
    Public Property ProjectName As String
    Public Property Balance As Decimal
    Public Property ExpirationDate As DateOnly?
    Public Property DaysOverdue As Integer
    Public Property IssuerCompanyId As Integer?
End Class

Public Class IssuerSummaryLineBO
    Public Property IssuerCompanyId As Integer
    Public Property OutstandingCount As Integer
    Public Property OutstandingAmount As Decimal
    Public Property OverdueCount As Integer
    Public Property OverdueAmount As Decimal
End Class
