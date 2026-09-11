Imports System.ComponentModel.DataAnnotations

''' <summary>Payload om een generiek projectdossier aan te maken of te bewerken.</summary>
Public Class DossierUpsertBO

    Public Property Id As Integer?

    <Required>
    Public Property ProjectId As Integer

    Public Property UnitId As Integer?

    ''' <summary>BOCore.DossierKind.</summary>
    <Required>
    Public Property DossierKind As Integer

    <Required, StringLength(200)>
    Public Property Titel As String = String.Empty

    <StringLength(100)>
    Public Property Referentie As String

    Public Property Status As Integer

    Public Property VerantwoordelijkePartijType As Integer?

    Public Property VerantwoordelijkePartijId As Integer?

    <StringLength(128)>
    Public Property VerantwoordelijkeUserId As String

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

End Class
