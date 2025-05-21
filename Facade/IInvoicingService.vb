Imports BO
Public Interface IInvoicingService
    Function GetInvoices() As GetResponse(Of InvoiceBO)
    Function GetClientInvoices(id As Integer, Optional itype As Integer = 1) As GetResponse(Of InvoiceBO)

    Function GetInvoiceByID(id As Integer) As GetResponse(Of InvoiceBO)
    Function GetInvoiceFileByFilename(name As String) As GetResponse(Of InvoiceFileBO)

End Interface
