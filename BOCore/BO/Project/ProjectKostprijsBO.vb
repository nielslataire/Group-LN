Public Class ProjectKostprijsBO
    Public Property Id As Integer
    Public Property ProjectId As Integer
    Public Property KostprijsMateriaalId As Integer
    Public Property CategorieNaam As String
    Public Property LotNummer As Integer?
    Public Property Naam As String
    Public Property Eenheid As String
    Public Property Toepassingsgebied As String
    Public Property IndexTypeCode As String
    Public Property Prijs As Decimal
    Public Property ReferentieDatum As DateTime
    Public Property SnapshotDatum As DateTime
    Public Property PrijsInstellingen As Decimal
    Public Property Gewijzigd As Boolean
End Class
