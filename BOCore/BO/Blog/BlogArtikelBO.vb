Public Class BlogArtikelBO

    Public Sub New()
        Blokken = New List(Of BlogArtikelBlokBO)
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
    Public Property Blokken As List(Of BlogArtikelBlokBO)

End Class
