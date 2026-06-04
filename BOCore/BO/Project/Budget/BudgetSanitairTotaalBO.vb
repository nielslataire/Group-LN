Public Class BudgetSanitairTotaalBO

    Private _totaalBadkamers As Integer
    Public Property TotaalBadkamers() As Integer
        Get
            Return _totaalBadkamers
        End Get
        Set(ByVal value As Integer)
            _totaalBadkamers = value
        End Set
    End Property

    Private _totaalToilettenInBadkamer As Integer
    Public Property TotaalToilettenInBadkamer() As Integer
        Get
            Return _totaalToilettenInBadkamer
        End Get
        Set(ByVal value As Integer)
            _totaalToilettenInBadkamer = value
        End Set
    End Property

    Private _totaalAfzonderlijkeToiletten As Integer
    Public Property TotaalAfzonderlijkeToiletten() As Integer
        Get
            Return _totaalAfzonderlijkeToiletten
        End Get
        Set(ByVal value As Integer)
            _totaalAfzonderlijkeToiletten = value
        End Set
    End Property

    Private _totaalDouchesInBadkamer As Integer
    Public Property TotaalDouchesInBadkamer() As Integer
        Get
            Return _totaalDouchesInBadkamer
        End Get
        Set(ByVal value As Integer)
            _totaalDouchesInBadkamer = value
        End Set
    End Property

    Private _totaalDouchekamers As Integer
    Public Property TotaalDouchekamers() As Integer
        Get
            Return _totaalDouchekamers
        End Get
        Set(ByVal value As Integer)
            _totaalDouchekamers = value
        End Set
    End Property

    Private _aantalEenheden As Integer
    Public Property AantalEenheden() As Integer
        Get
            Return _aantalEenheden
        End Get
        Set(ByVal value As Integer)
            _aantalEenheden = value
        End Set
    End Property

End Class
