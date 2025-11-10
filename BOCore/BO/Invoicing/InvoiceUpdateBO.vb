Public Class InvoiceUpdateBO
    Public Sub New()
        Lines = New List(Of InvoiceLineUpdateBO)()
    End Sub

    Public Property InvoiceId As Integer
    Public Property HeaderDescription As String
    Public Property DetailDescription As String
    Public Property FooterDescription As String
    Public Property BankAccount As String
    Public Property ExpirationDate As DateOnly?
    Public Property IsDraft As Boolean
    Public Property Lines As List(Of InvoiceLineUpdateBO)
End Class