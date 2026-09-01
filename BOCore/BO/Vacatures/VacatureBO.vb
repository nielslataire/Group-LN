Public Class VacatureBO

    Public Sub New()
        TaakItems = New List(Of VacatureTaakBO)
        VereisteItems = New List(Of VacatureVereisteBO)
        VoordeelItems = New List(Of VacatureVoordeelBO)
        SollicitatieStapItems = New List(Of VacatureSollicitatieStapBO)
    End Sub

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
    Public Property VideoBestand As String
    Public Property VideoPosterBestand As String
    Public Property IsGepubliceerd As Boolean
    Public Property SortOrder As Integer
    Public Property AangemaaktOp As DateTime
    Public Property GewijzigdOp As DateTime
    Public Property TaakItems As List(Of VacatureTaakBO)
    Public Property VereisteItems As List(Of VacatureVereisteBO)
    Public Property VoordeelItems As List(Of VacatureVoordeelBO)
    Public Property SollicitatieStapItems As List(Of VacatureSollicitatieStapBO)

End Class
