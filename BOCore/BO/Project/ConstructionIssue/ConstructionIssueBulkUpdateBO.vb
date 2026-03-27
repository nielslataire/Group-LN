Public Class ConstructionIssueBulkUpdateBO
    Public Property IssueIds As List(Of Integer) = New List(Of Integer)
    Public Property ResponsiblePartyType As Integer?
    Public Property ResponsiblePartyId As Integer?
    Public Property ResponsibleOtherName As String
    Public Property ResponsibleOtherEmail As String
    Public Property Status As Integer?
    Public Property DueDate As DateOnly?
    Public Property PlannedDate As DateOnly?
    Public Property Priority As Integer?
    Public Property IssueType As Integer?
    Public Property IssuePhase As Integer?
    Public Property CategoryId As Integer?
End Class