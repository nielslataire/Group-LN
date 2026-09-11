Imports System.ComponentModel.DataAnnotations

''' <summary>Payload om een persoonlijke/interne taak (<c>ProjectTaak</c>) aan te maken of te bewerken.</summary>
Public Class TaakUpsertBO

    Public Property Id As Integer?

    Public Property ProjectId As Integer?

    Public Property UnitId As Integer?

    Public Property MijlpaalId As Integer?

    Public Property ProjectDossierId As Integer?

    Public Property ConstructionIssueId As Integer?

    <Required, StringLength(200)>
    Public Property Titel As String = String.Empty

    Public Property Omschrijving As String

    ''' <summary>BOCore.TaakStatus.</summary>
    Public Property Status As Integer

    ''' <summary>BOCore.TaakPrioriteit.</summary>
    Public Property Prioriteit As Integer = 1

    <StringLength(128)>
    Public Property ToegewezenAanUserId As String

    ''' <summary>BOCore.InterneRol.</summary>
    Public Property ToegewezenAanRol As Integer?

    <DataType(DataType.Date)>
    Public Property Vervaldatum As DateOnly?

    ''' <summary>BOCore.TaakHerkomst.</summary>
    Public Property Herkomst As Integer = 0

End Class
