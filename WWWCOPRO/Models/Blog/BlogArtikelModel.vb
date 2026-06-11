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
        Public Property Blokken As New List(Of BlogArtikelBlokModel)
    End Class

    Public Class BlogArtikelBlokModel
        Public Property ID As Integer
        Public Property SortOrder As Integer
        Public Property Titel As String
        Public Property RijkeTekst As String
        Public Property FotoBestand As String
    End Class

End Namespace
