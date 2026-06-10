Public Class BudgetGevelTotaalBO
    Public Property TotaalGevelNieuwbouw() As Decimal
    Public Property TotaalGevelBestaand() As Decimal
    Public Property TotaalRaamNieuwbouw() As Decimal
    Public Property TotaalRaamBestaand() As Decimal
    Public Property TotaalBallustrade() As Decimal
    Public Property TotaalZichtscherm() As Decimal
    Public Property TotaalPlatDak() As Decimal
    Public Property TotaalHellendDak() As Decimal
    Public Property TotaalGroenDak() As Decimal
    Public Property TotaalDakoversteken() As Decimal
    Public Property TotaalOnderkantDoorrit() As Decimal
    Public Property TotaalAfbraak() As Decimal
    Public Property TotaalLeien() As Decimal

    Public ReadOnly Property TotaalGevelCombineerd() As Decimal
        Get
            Return TotaalGevelNieuwbouw + TotaalGevelBestaand
        End Get
    End Property

    Public ReadOnly Property TotaalRaamCombineerd() As Decimal
        Get
            Return TotaalRaamNieuwbouw + TotaalRaamBestaand
        End Get
    End Property

    Public ReadOnly Property TotaalDakCombineerd() As Decimal
        Get
            Return TotaalPlatDak + TotaalHellendDak + TotaalGroenDak
        End Get
    End Property
End Class
