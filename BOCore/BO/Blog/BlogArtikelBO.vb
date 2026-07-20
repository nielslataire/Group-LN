Public Class BlogArtikelBO

    Public Sub New()
        Blokken = New List(Of BlogArtikelBlokBO)
        FaqItems = New List(Of BlogArtikelFaqBO)
    End Sub

    Public Property ID As Integer
    Public Property Titel As String
    Public Property Slug As String
    Public Property PreviewTekst As String
    Public Property DetailTitel As String
    Public Property DetailTitelTekst As String
    Public Property FotoBestand As String
    Public Property Datum As DateTime
    Public Property IsGepubliceerd As Boolean
    Public Property SortOrder As Integer
    Public Property AangemaaktOp As DateTime
    Public Property GewijzigdOp As DateTime
    Public Property MetaTitel As String
    Public Property MetaOmschrijving As String
    Public Property MetaKeywords As String
    Public Property GeoRegio As String
    Public Property GeoPlaatsnaam As String
    Public Property GeoPositie As String
    Public Property Link1Type As String
    Public Property Link1Id As Nullable(Of Integer)
    Public Property Link2Type As String
    Public Property Link2Id As Nullable(Of Integer)
    Public Property Link3Type As String
    Public Property Link3Id As Nullable(Of Integer)
    Public Property Blokken As List(Of BlogArtikelBlokBO)
    Public Property FaqItems As List(Of BlogArtikelFaqBO)

End Class
