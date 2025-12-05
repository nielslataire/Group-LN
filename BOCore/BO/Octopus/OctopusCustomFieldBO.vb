Public Class OctopusCustomFieldBO
    Public Property CustomFieldKeyId As Integer
    Public Property TemplateCode As String
    Public Property DescriptionNl As String
    Public Property DescriptionFr As String
    Public Property DescriptionEn As String
    Public Property DescriptionDe As String
    Public Property CustomFieldType As Integer
End Class

Public Class OctopusCustomFieldMappingBO
    Public Property CustomFieldKeyId As Integer
    Public Property InvoiceField As String
End Class