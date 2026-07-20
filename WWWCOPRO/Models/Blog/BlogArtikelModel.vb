Namespace Models.Blog

    Public Class BlogArtikelModel
        Public Property ID As Integer
        Public Property Titel As String
        Public Property Slug As String
        Public Property PreviewTekst As String
        Public Property DetailTitel As String
        Public Property DetailTitelTekst As String
        Public Property FotoBestand As String
        Public Property Datum As DateTime
        Public Property IsGepubliceerd As Boolean
        Public Property LeestijdMinuten As Integer = 0
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
        Public Property Blokken As New List(Of BlogArtikelBlokModel)
        Public Property FaqItems As New List(Of BlogArtikelFaqModel)
        Public Property OntdekMeer As New List(Of OntdekMeerItemModel)
    End Class

    Public Class OntdekMeerItemModel
        Public Property ItemType As String
        Public Property Titel As String
        Public Property Slug As String
        Public Property FotoUrl As String
        Public Property IsVideo As Boolean
        Public Property VideoUrl As String
        ' artikel
        Public Property Datum As Nullable(Of DateTime)
        Public Property PreviewTekst As String
        ' project
        Public Property Street As String
        Public Property Gemeente As String
        Public Property MinSlaapkamers As Nullable(Of Integer)
        Public Property MaxSlaapkamers As Nullable(Of Integer)
        Public Property AantalEenheden As String
        Public Property VanafPrijs As Nullable(Of Decimal)
        Public Property IsCasco As Boolean
    End Class

    Public Class BlogArtikelFaqModel
        Public Property ID As Integer
        Public Property SortOrder As Integer
        Public Property Vraag As String
        Public Property Antwoord As String
    End Class

    Public Class BlogArtikelBlokModel
        Public Property ID As Integer
        Public Property SortOrder As Integer
        Public Property BlokType As String = "tekst"
        Public Property Titel As String
        Public Property RijkeTekst As String
        Public Property FotoBestand As String
    End Class

End Namespace
