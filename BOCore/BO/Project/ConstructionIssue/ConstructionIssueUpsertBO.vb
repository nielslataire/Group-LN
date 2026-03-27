Imports System.ComponentModel.DataAnnotations

Public Class ConstructionIssueUpsertBO
    <Required, StringLength(200)>
    Public Property Title As String = String.Empty

    Public Property Description As String

    Public Property LocationText As String = String.Empty

    <Required>
    Public Property CategoryId As Integer

    Public Property BuildingPart As Integer?

    <StringLength(100)>
    Public Property RoomOrZone As String

    Public Property UnitId As Integer?
    Public Property IssueType As Integer
    Public Property IssuePhase As Integer
    Public Property Priority As Integer
    Public Property Status As Integer
    Public Property ResponsiblePartyType As Integer
    Public Property ResponsiblePartyId As Integer?

    <StringLength(200)>
    Public Property ResponsibleOtherName As String

    <StringLength(254), EmailAddress>
    Public Property ResponsibleOtherEmail As String

    <DataType(DataType.Date)>
    Public Property DueDate As DateOnly?

    <DataType(DataType.Date)>
    Public Property PlannedDate As DateOnly?

    Public Property PlanDocumentId As Integer?
    Public Property PlanPageNumber As Integer?

    <Range(0, 1)>
    Public Property PlanXNormalized As Decimal?

    <Range(0, 1)>
    Public Property PlanYNormalized As Decimal?
End Class