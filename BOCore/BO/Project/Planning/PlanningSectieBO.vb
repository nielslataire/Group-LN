Public Class PlanningSectieBO

    Public Property Id As Integer
    Public Property ProjectId As Integer
    Public Property ActivityGroupId As Integer?
    Public Property Naam As String
    Public Property Volgorde As Integer
    Public Property IsActief As Boolean
    Public Property PlanningTaak As List(Of PlanningTaakBO) = New List(Of PlanningTaakBO)()

    ''' <summary>Gemiddelde voortgang over alle taken (0-100).</summary>
    Public ReadOnly Property GemiddeldeVoortgangPct As Integer
        Get
            If PlanningTaak Is Nothing OrElse PlanningTaak.Count = 0 Then Return 0
            Return CInt(PlanningTaak.Average(Function(t) t.VoortgangPct))
        End Get
    End Property

End Class
