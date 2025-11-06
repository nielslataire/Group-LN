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

    Public Property BankAccount As String
    Public Property ExtraInfo As String
    Public Property HeaderText As String
    Public Property DetailText As String

    Public Property TotalExclVat As Decimal
    Public Property TotalVat As Decimal
    Public Property TotalInclVat As Decimal
    Public Property PaidAmount As Decimal?
    Public Property Balance As Decimal?

    Public Property Lines As List(Of InvoiceLineBO)
End Class
