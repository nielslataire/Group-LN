''' <summary>
''' Geaggregeerde telling van de cookiebanner-uitkomst op WWWCOPRO over een periode.
''' NoDecisionCount is afgeleid (ShownCount minus de twee andere), nooit apart geteld —
''' er bestaat geen gebeurtenis voor "niet geklikt", enkel de afwezigheid van een keuze.
''' </summary>
Public Class CookieConsentStatsBO

    Public Property PeriodStartUtc As DateTime
    Public Property PeriodEndUtc As DateTime
    Public Property ShownCount As Integer
    Public Property AcceptedCount As Integer
    Public Property RejectedCount As Integer

    Public ReadOnly Property NoDecisionCount As Integer
        Get
            Return Math.Max(0, ShownCount - AcceptedCount - RejectedCount)
        End Get
    End Property

    Public ReadOnly Property AcceptedPercentage As Decimal
        Get
            Return PercentageOf(AcceptedCount)
        End Get
    End Property

    Public ReadOnly Property RejectedPercentage As Decimal
        Get
            Return PercentageOf(RejectedCount)
        End Get
    End Property

    Public ReadOnly Property NoDecisionPercentage As Decimal
        Get
            Return PercentageOf(NoDecisionCount)
        End Get
    End Property

    Private Function PercentageOf(count As Integer) As Decimal
        If ShownCount <= 0 Then Return 0D
        Return Math.Round(count * 100D / ShownCount, 1)
    End Function

End Class
