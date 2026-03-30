Public Class PlanningTaakBO

    Public Property Id As Integer
    Public Property PlanningSectieId As Integer
    Public Property Naam As String
    Public Property Beschrijving As String
    Public Property Startdatum As DateOnly?
    Public Property Einddatum As DateOnly?
    Public Property AfgerondOp As DateOnly?
    Public Property IsAfgerond As Boolean
    ''' <summary>Manuele voortgang 0-100%.</summary>
    Public Property VoortgangPct As Integer
    Public Property Volgorde As Integer

End Class
