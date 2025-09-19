Public Class IssuerBankAccountBO
    Public Property Id As Integer
    Public Property IssuerCompanyId As Integer
    Public Property Iban As String   ' we normaliseren in service
    Public Property Bic As String
    Public Property DisplayName As String
    Public Property IsDefault As Boolean
    Public Property ValidFrom As DateOnly?
    Public Property ValidTo As DateOnly?
End Class
