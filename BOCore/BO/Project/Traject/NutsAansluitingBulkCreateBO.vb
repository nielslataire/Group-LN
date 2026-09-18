Imports System.ComponentModel.DataAnnotations

''' <summary>Payload om hetzelfde type nutsaansluiting in één keer voor meerdere eenheden aan te maken.</summary>
Public Class NutsAansluitingBulkCreateBO

    <Required>
    Public Property ProjectId As Integer

    <Required>
    Public Property UnitIds As List(Of Integer) = New List(Of Integer)

    ''' <summary>BOCore.NutsType.</summary>
    <Required>
    Public Property NutsType As Integer

    Public Property NetbeheerderCompanyId As Integer?

    <StringLength(50)>
    Public Property GevraagdVermogen As String

    <StringLength(150)>
    Public Property ExterneContactNaam As String

    <StringLength(254), EmailAddress>
    Public Property ExterneContactEmail As String

    <DataType(DataType.Date)>
    Public Property AanvraagDatum As DateOnly?

    <DataType(DataType.Date)>
    Public Property VerwachteAfhandelingDatum As DateOnly?

    Public Property Omschrijving As String

    ''' <summary>Optionele titel-prefix per dossier; default is "{NutsType} — {UnitNaam}".</summary>
    Public Property TitelPrefix As String

End Class
