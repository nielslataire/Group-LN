''' <summary>
''' Geaggregeerde telling van de cookiebanner-uitkomst op WWWCOPRO over een periode.
''' Alle vier de tellers zijn echte gebeurtenissen (zie migraties 033 en 059). "Verlaten"
''' (AbandonedCount) wordt sinds 059 apart gemeten: de bezoeker sloot het tabblad, ging terug
''' of klikte weg naar een andere site terwijl de banner nog open stond. Het is geen restwaarde
''' meer, dus Shown hoeft niet gelijk te zijn aan de som van de drie andere: wie eerst verlaat
''' en later toch kiest, telt in beide.
''' </summary>
Public Class CookieConsentStatsBO

    Public Property PeriodStartUtc As DateTime
    Public Property PeriodEndUtc As DateTime
    Public Property ShownCount As Integer
    Public Property AcceptedCount As Integer
    Public Property RejectedCount As Integer
    Public Property AbandonedCount As Integer

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

    Public ReadOnly Property AbandonedPercentage As Decimal
        Get
            Return PercentageOf(AbandonedCount)
        End Get
    End Property

    Private Function PercentageOf(count As Integer) As Decimal
        If ShownCount <= 0 Then Return 0D
        Return Math.Round(count * 100D / ShownCount, 1)
    End Function

End Class
