Public Class VacatureSollicitatieBO

    Public Property ID As Integer
    Public Property VacatureId As Integer?
    Public Property VacatureTitelSnapshot As String
    Public Property Voornaam As String
    Public Property Achternaam As String
    Public Property Email As String
    Public Property Telefoon As String
    Public Property Motivatie As String
    Public Property CvBestandsnaam As String
    Public Property CvBestandType As String
    Public Property CvBestand As Byte()
    Public Property IsGelezen As Boolean
    Public Property AangemaaktOp As DateTime

    Public ReadOnly Property FullName As String
        Get
            Return (Voornaam & " " & Achternaam).Trim()
        End Get
    End Property

End Class
