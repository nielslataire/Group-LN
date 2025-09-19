Public Class CreateInvoiceRequest
    Public Property ClientAccountId As Integer
    Public Property ProjectId As Integer?
    Public Property InvoiceDate As DateOnly
    Public Property DueDate As DateOnly
    Public Property Note As String
End Class
