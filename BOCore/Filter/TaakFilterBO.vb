''' <summary>Filtercriteria voor het opvragen van persoonlijke/interne taken ("Mijn taken").</summary>
Public Class TaakFilterBO
    Public Property Status As Integer?
    Public Property Prioriteit As Integer?
    Public Property ProjectId As Integer?
    Public Property ToegewezenAanUserId As String
    Public Property AlleenAchterstallig As Boolean?
    Public Property Text As String
End Class
