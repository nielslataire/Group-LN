''' <summary>Filtercriteria voor het opvragen van mijlpalen binnen een traject (spiegelt ConstructionIssueFilterBO).</summary>
Public Class MijlpaalFilterBO
    Public Property Status As Integer?
    Public Property FaseId As Integer?
    Public Property UnitId As Integer?
    ''' <summary>True = enkel projectniveau-mijlpalen (UnitId is null); False = enkel per-eenheid.</summary>
    Public Property AlleenProjectniveau As Boolean?
    Public Property MijlpaalType As Integer?
    Public Property VerantwoordelijkeRol As Integer?
    Public Property VerantwoordelijkeUserId As String
    Public Property Overdue As Boolean?
    Public Property AlleenVerplicht As Boolean?
    Public Property Text As String
End Class
