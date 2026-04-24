Public Class IssuerCompanyBO
    Public Property Id As Integer
    Public Property Name As String
    Public Property LegalName As String
    Public Property VatNumber As String
    Public Property EnterpriseNumber As String
    Public Property AddressLine1 As String
    Public Property AddressLine2 As String
    Public Property PostalCode As String
    Public Property City As String
    Public Property CountryCode As String
    Public Property Email As String
    Public Property InvoiceSendEmail As String
    Public Property Phone As String
    Public Property Phone2 As String
    Public Property LogoPath As String
    Public Property Website As String
    Public Property TemplateKey As String
    Public Property TemplateJson As String
    Public Property BrandPrimaryColor As String
    Public Property BrandSecondaryColor As String
    Public Property FontFamily As String
    Public Property LogoBytes As Byte()
    Public Property DefaultPaymentTermId As Integer?
    Public Property DefaultVatTypeId As Integer?
    Public Property IsActive As Boolean

    Public Property EInvoiceEnabled As Boolean
    Public Property PeppolParticipantId As String
    Public Property UblAttachPdf As Boolean

    Public Property EmailSubjectTemplate As String
    Public Property EmailBodyTemplate As String
    Public Property EmailPaidBodyTemplate As String
    Public Property InvoiceFooterHtml As String
    Public Property DefaultLanguage As String
    Public Property DefaultCurrency As String
    Public Property InvoiceNumberPattern As String

    Public Property FooterLegalText As String
    Public Property PeppolEnabled As Boolean

    Public Property EpcQrEnabled As Boolean
    Public Property EpcBeneficiaryName As String
    Public Property EpcIban As String
    Public Property EpcBic As String
    Public Property EpcRemittanceType As String   ' "CHAR" | "SCOR"
    Public Property EpcRemittanceTemplate As String   ' bv. "Factuur {PublicId}"

    Public Property CompanyLegalFormId As Integer?
    Public Property CompanyLegalFormName As String
    Public Property CompanyLegalFormAbbreviation As String

    Public Property DefaultBankAccountIban As String
    Public Property DefaultBankAccountBic As String

    Public Property OctopusUsername As String
    Public Property OctopusPassword As String
    Public Property OctopusAuthenticateToken As String
    Public Property OctopusAuthenticateTokenValidUntil As Date?
    Public Property OctopusDossierToken As String
    Public Property OctopusDossierTokenValidUntil As Date?
    Public Property OctopusDossierNumber As String
    Public Property OctopusPurchaseJournalKeyPrefix As String
    Public Property OctopusCustomFieldsJson As String
    Public Property OctopusCustomFieldMappingsJson As String
    Public Property OctopusDownloadLinkCustomFieldKeyId As Integer?

    ' Km-tarief verplaatsing (voor coördinatieprojecten)
    Public Property RatePerKm As Decimal?

End Class