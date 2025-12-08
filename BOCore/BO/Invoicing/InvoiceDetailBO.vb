Imports System
Imports System.Collections.Generic
Public Class InvoiceDetailBO
    Public Sub New()
        Lines = New List(Of InvoiceLineBO)()
    End Sub

    Public Property Id As Integer
    Public Property PublicId As String
    Public Property InvoiceDate As DateOnly
    Public Property ExpirationDate As DateOnly?
    Public Property StatusName As String

    Public Property IssuerCompanyId As Integer
    Public Property IssuerName As String
    Public Property IssuerLegalName As String
    Public Property IssuerVatNumber As String
    Public Property IssuerAddressLine1 As String
    Public Property IssuerAddressLine2 As String
    Public Property IssuerPostalCode As String
    Public Property IssuerCity As String
    Public Property IssuerCountryCode As String
    Public Property IssuerEmail As String
    Public Property IssuerPhone As String
    Public Property IssuerDefaultIban As String
    Public Property IssuerDefaultBic As String
    Public Property IssuerLegalFormAbbreviation As String
    Public Property StructuredMessage As String
    Public Property QrPayLoad As String

    Public Property ClientName As String
    Public Property ClientAddress As String
    Public Property ClientPostalCode As String
    Public Property ClientCity As String
    Public Property ClientCountryName As String
    Public Property ClientVatNumber As String
    Public Property ClientEnterpriseNumber As String
    Public Property ClientEmail As String
    Public Property RequiresDigitalInvoice As Boolean
    Public Property AttachUblByDefault As Boolean
    Public Property IsSupplier As Boolean

    Public Property BankAccount As String
    Public Property ExtraInfo As String
    Public Property HeaderText As String
    Public Property DetailText As String

    Public Property TotalExclVat As Decimal
    Public Property TotalVat As Decimal
    Public Property TotalInclVat As Decimal
    Public Property PaidAmount As Decimal?
    Public Property Balance As Decimal?
    Public Property IsCreditNote As Boolean
    Public Property Currency As String

    Public Property ClientId As Integer?
    Public Property ClientType As Integer?
    Public Property CompanyId As Integer?
    Public Property InvoiceMode As InvoiceMode?
    Public Property ProjectId As Integer?
    Public Property SupplierContractId As Integer?
    Public Property PaymentTermId As Integer?

    Public Property OctopusBookyearId As Integer?
    Public Property OctopusJournalKey As String
    Public Property OctopusDocumentSequenceNr As Integer?
    Public Property OctopusWorkflowState As String
    Public Property OctopusWorkflowUpdatedAt As Date?
    Public Property OctopusDeliveryState As String
    Public Property OctopusDeliveryComment As String
    Public Property OctopusDeliveryDateTime As Date?
    Public Property OctopusDeliveryUpdatedAt As Date?
    Public Property OctopusBookedAt As Date?
    Public Property OctopusBookedBy As String

    Public Property Lines As List(Of InvoiceLineBO)
End Class
