Imports System.ComponentModel.DataAnnotations

''' <summary>
''' Payload om een nutsaansluitingsdossier aan te maken/bewerken — draagt zowel de generieke
''' dossiervelden als de nuts-specifieke velden; de service splitst dit over
''' <c>ProjectDossier</c> + <c>ProjectNutsAansluiting</c>.
''' </summary>
Public Class NutsAansluitingUpsertBO

    ''' <summary>Id van het ProjectDossier; leeg = nieuw dossier.</summary>
    Public Property Id As Integer?

    <Required>
    Public Property ProjectId As Integer

    Public Property UnitId As Integer?

    <Required, StringLength(200)>
    Public Property Titel As String = String.Empty

    <StringLength(100)>
    Public Property Referentie As String

    Public Property Status As Integer

    <StringLength(150)>
    Public Property ExterneContactNaam As String

    <StringLength(254), EmailAddress>
    Public Property ExterneContactEmail As String

    <DataType(DataType.Date)>
    Public Property AanvraagDatum As DateOnly?

    <DataType(DataType.Date)>
    Public Property VerwachteAfhandelingDatum As DateOnly?

    <DataType(DataType.Date)>
    Public Property AfgehandeldDatum As DateOnly?

    Public Property Bedrag As Decimal?

    Public Property Omschrijving As String

    ' --- Nuts-specifiek ---

    ''' <summary>BOCore.NutsType.</summary>
    Public Property NutsType As Integer

    Public Property NetbeheerderCompanyId As Integer?

    <StringLength(50)>
    Public Property Ean As String

    <StringLength(50)>
    Public Property Meternummer As String

    <StringLength(50)>
    Public Property GevraagdVermogen As String

    <DataType(DataType.Date)>
    Public Property AanvraagVerstuurdOp As DateOnly?

    Public Property AansluitkostRaming As Decimal?

    Public Property AansluitkostDefinitief As Decimal?

End Class
