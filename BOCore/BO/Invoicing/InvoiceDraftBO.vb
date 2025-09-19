Public Class InvoiceDraftBO

    Public Property IssuerCompanyId As Integer
    Public Property ClientType As Integer?
    Public Property ClientId As Integer?
    Public Property CompanyId As Integer?
    Public Property InvoiceDate As DateOnly
    Public Property ExpirationDate As DateOnly?
    Public Property Lines As List(Of InvoiceLineBO) = New List(Of InvoiceLineBO)()

End Class


