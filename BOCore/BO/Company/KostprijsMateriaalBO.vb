Public Class KostprijsMateriaalBO
    Public Property Id As Integer
    Public Property CategorieId As Integer
    Public Property CategorieNaam As String
    Public Property LotNummer As Integer?
    Public Property Naam As String
    Public Property Eenheid As String
    Public Property Toepassingsgebied As String
    Public Property KmIndexTypeId As Integer
    Public Property IndexTypeCode As String
    Public Property IndexTypeNaam As String
    Public Property ReferentiePrijs As Decimal
    Public Property ReferentieDatum As DateTime
    Public Property Volgorde As Integer
End Class
