''' <summary>Payload om meerdere mijlpalen tegelijk bij te werken.</summary>
Public Class MijlpaalBulkUpdateBO
    Public Property MijlpaalIds As List(Of Integer) = New List(Of Integer)
    Public Property Status As Integer?
    Public Property Doeldatum As DateOnly?
    Public Property VerantwoordelijkeRol As Integer?
    Public Property VerantwoordelijkeUserId As String
    Public Property ProjecttrajectFaseId As Integer?
End Class
