Public Class InvoiceListItemBO
    Public Property Id As Integer
    Public Property PublicId As String
    Public Property ClientName As String
    Public Property InvoiceDate As DateOnly
    Public Property StatusId As Integer?
    Public Property StatusName As String
    Public Property GrossTotal As Decimal?
    Public Property Paid As Decimal
    Public Property Balance As Decimal?
    Public Property IsCreditNote As Boolean
    Public Property RequiresDigitalInvoice As Boolean
    Public Property HasEmail As Boolean
    Public Property ClientType As Integer?
    Public Property IsSupplier As Boolean
    Public Property HasCompanyName As Boolean
    Public Property OctopusWorkflowState As String

End Class
