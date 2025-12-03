Public Class InvoiceDraftBO

    Public Property IssuerCompanyId As Integer
    Public Property ClientType As Integer?
    Public Property ClientId As Integer?
    Public Property CompanyId As Integer?

    Public Property Mode As InvoiceMode = InvoiceMode.Free

    Public Property ProjectId As Integer?
    Public Property SupplierContractId As Integer?

    Public Property InvoiceDate As DateOnly
    Public Property ExpirationDate As DateOnly?

    Public Property HeaderDescription As String
    Public Property DetailDescription As String
    Public Property FooterDescription As String

    Public Property Lines As List(Of InvoiceLineBO) = New List(Of InvoiceLineBO)()
    Public Property SelectedVatTypeId As Integer?

    Public Property PaymentGroupId As Integer?
    Public Property IssuerBankAccountId As Integer?
    Public Property PaymentTermId As Integer?

    Public Property StageIds As List(Of Integer) = New List(Of Integer)
    Public Property IsCreditNote As Boolean

End Class


