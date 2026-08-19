Namespace Models.Vacatures
    Public Class VacatureModel
        Public Property ID As Integer
        Public Property Titel As String
        Public Property Slug As String
        Public Property Categorie As String
        Public Property Locatie As String
        Public Property Dienstverband As String
        Public Property Opleiding As String
        Public Property Start As String
        Public Property KorteBeschrijving As String
        Public Property Beschrijving As String
        Public Property SortOrder As Integer
        Public Property AangemaaktOp As Date

        Public Property Takenpakket As New List(Of String)
        Public Property MustHaves As New List(Of String)
        Public Property MooiMeegenomen As New List(Of String)
        Public Property Voordelen As New List(Of String)
        Public Property SollicitatieStappen As New List(Of VacatureSollicitatieStapModel)
        Public Property AndereVacatures As New List(Of VacatureModel)
    End Class

    Public Class VacatureSollicitatieStapModel
        Public Property Titel As String
        Public Property Tekst As String
    End Class
End Namespace
