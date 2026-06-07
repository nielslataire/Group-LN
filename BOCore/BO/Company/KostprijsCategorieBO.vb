Public Class KostprijsCategorieBO
    Public Property Id As Integer
    Public Property Naam As String
    Public Property LotNummer As Integer?
    Public Property Volgorde As Integer
    Public Property Materialen As New List(Of KostprijsMateriaalBO)
End Class
