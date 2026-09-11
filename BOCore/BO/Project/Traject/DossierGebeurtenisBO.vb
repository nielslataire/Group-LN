Imports System.ComponentModel.DataAnnotations

''' <summary>Payload om een gebeurtenis toe te voegen aan de tijdlijn van een dossier.</summary>
Public Class DossierGebeurtenisBO

    <Required>
    Public Property ProjectDossierId As Integer

    ''' <summary>BOCore.DossierGebeurtenisType.</summary>
    Public Property Type As Integer

    <StringLength(200)>
    Public Property Titel As String

    Public Property Tekst As String

End Class
