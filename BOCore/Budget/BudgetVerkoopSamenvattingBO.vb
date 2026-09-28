Namespace Budget

    ''' <summary>
    ''' Verkoopkant van een budgetversie in één blok, naast de kostenkant (BudgetResultaatBO):
    ''' minimale verkoopwaarde uit het voorstel, de vastgelegde vraagprijzen op de verkooplijnen,
    ''' en de marge. Gebruikt op stap 9, in de versievergelijking en in PDF/Excel.
    ''' </summary>
    Public Class BudgetVerkoopSamenvattingBO
        ''' <summary>Kostprijs uit het budget + aankoopprijs grond.</summary>
        Public Property TotaalKostprijsInclGrond As Decimal
        Public Property Grondwaarde As Decimal
        Public Property Bouwwaarde As Decimal
        ''' <summary>Grondwaarde + bouwwaarde = kostprijs + marges.</summary>
        Public Property MinimaleVerkoopwaarde As Decimal
        Public Property MarktTotaal As Decimal?
        Public Property TotaalAanbevolenVraagprijs As Decimal
        Public Property AantalEenheden As Integer

        ''' <summary>Som van de vraagprijzen die op de verkooplijnen vastgelegd zijn; Nothing als er geen zijn.</summary>
        Public Property VraagprijzenLijnen As Decimal?
        Public Property AantalLijnenMetVraagprijs As Integer

        ''' <summary>Vastgelegde vraagprijzen als die er zijn, anders de aanbevolen vraagprijzen uit het voorstel.</summary>
        Public ReadOnly Property Opbrengst As Decimal
            Get
                If VraagprijzenLijnen.HasValue Then Return VraagprijzenLijnen.Value
                Return TotaalAanbevolenVraagprijs
            End Get
        End Property

        Public ReadOnly Property OpbrengstBron As String
            Get
                If VraagprijzenLijnen.HasValue Then Return $"vraagprijzen verkooplijnen ({AantalLijnenMetVraagprijs})"
                Return "aanbevolen vraagprijzen uit het voorstel"
            End Get
        End Property

        Public ReadOnly Property Marge As Decimal
            Get
                Return Opbrengst - TotaalKostprijsInclGrond
            End Get
        End Property

        Public ReadOnly Property MargePerc As Decimal
            Get
                If TotaalKostprijsInclGrond > 0D Then Return Marge / TotaalKostprijsInclGrond
                Return 0D
            End Get
        End Property
    End Class

    ''' <summary>Eén regel voor "Doorzetten naar units": welke Unit welke grond-/bouwwaarde krijgt.</summary>
    Public Class UnitBudgetWaardeBO
        Public Property UnitId As Integer
        Public Property EenheidNaam As String
        Public Property Grondwaarde As Decimal?
        Public Property Bouwwaarde As Decimal?
    End Class

End Namespace
