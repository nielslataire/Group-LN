Public Class InvoiceLineBO

    Public Property Id As Integer
    Public Property Text As String = ""
    Public Property Price As Decimal              ' lijnbedrag
    Public Property VatPercentage As Decimal      ' 0 / 6 / 12 / 21 …

    Public Property DiscountPercent As Decimal?   ' 0..100
    Public Property DiscountAmount As Decimal?    ' in dezelfde munt

    Public Property UnitId As Integer?
    Public Property PaymentStageId As Integer?
    Public Property LineType As String            ' nvarchar(20)
    Public Property GroupName As String           ' nvarchar(200)
    Public Property UtilityCost As Boolean        ' bit
    Public Property UtilityIsAdvance As Boolean
    Public Property ChangeOrderDetailId As Integer?

    Public Property VatTypeId As Integer?
    Public Property VatCode As String

End Class