Public Class ProjectRegieUurBO

    Public Property Id As Integer
    Public Property ProjectId As Integer
    Public Property UserId As String
    Public Property UserFullName As String
    Public Property [Date] As DateOnly
    Public Property Hours As Decimal
    Public Property WithTravel As Boolean
    Public Property TravelKm As Decimal?
    Public Property Description As String

    ''' <summary>Null = nog niet gefactureerd; gevuld = gefactureerd op die factuur.</summary>
    Public Property InvoiceId As Integer?
    Public Property InvoicePublicId As String

    ''' <summary>Uurtarief op het moment van factureren; Nothing zolang de prestatie nog niet gefactureerd is.</summary>
    Public Property HourlyRateInvoiced As Decimal?

End Class
