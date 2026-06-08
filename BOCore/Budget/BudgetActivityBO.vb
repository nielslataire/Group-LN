Namespace Budget

    Public Class BudgetActivityLijnBO

        Public Property Id As Integer
        Public Property BudgetVersieId As Integer
        Public Property ActivityId As Integer
        Public Property ActivityOmschrijving As String
        Public Property LotNummer As Decimal
        Public Property LotNaam As String
        Public Property GroupId As Integer

        Public Property AlternatievePrijsPerEenheid As Decimal
        Public Property NacalcPrijsPerEenheid As Decimal
        Public Property Correctiefactor As Decimal = 1D
        Public Property IsManueel As Boolean

        Public Property SIndexStart As Decimal
        Public Property SIndexHuidig As Decimal
        Public Property IIndexStart As Decimal
        Public Property IIndexHuidig As Decimal
        Public Property GewogenIndexFactor As Decimal = 1D

        ''' <summary>Gevuld door service — niet opgeslagen in DB.</summary>
        Public Property AantalEenheden As Integer

        ''' <summary>Totale GBA m², gevuld door service.</summary>
        Public Property TotaalOppervlakte As Decimal

        ''' <summary>Automatisch berekende suggestie voor AlternatievePrijsPerEenheid. Niet opgeslagen in DB.</summary>
        Public Property VoorgesteldePrijsPerEenheid As Decimal?

        Public Property VoorstelRuwbouwPrijs As Decimal?
        Public Property VoorstelRuwbouwOpp As Decimal?
        Public Property VoorstelTerrasPrijs As Decimal?
        Public Property VoorstelTerrasOpp As Decimal?
        Public Property VoorstelGevelPrijs As Decimal?
        Public Property VoorstelGevelOpp As Decimal?
        Public Property VoorstelAantalEenheden As Integer?

        Public ReadOnly Property HeeftVoorstel As Boolean
            Get
                Return VoorgesteldePrijsPerEenheid.HasValue AndAlso VoorgesteldePrijsPerEenheid.Value > 0
            End Get
        End Property

        Public ReadOnly Property NacalcGeindexeerd As Decimal
            Get
                Dim factor As Decimal = If(GewogenIndexFactor = 0, 1D, GewogenIndexFactor)
                Return NacalcPrijsPerEenheid * Correctiefactor * factor
            End Get
        End Property

        Public ReadOnly Property TotaalAlternatief As Decimal
            Get
                Return AlternatievePrijsPerEenheid * AantalEenheden
            End Get
        End Property

        Public ReadOnly Property TotaalNacalc As Decimal
            Get
                Return NacalcGeindexeerd * AantalEenheden
            End Get
        End Property

        Public ReadOnly Property Verschil As Decimal
            Get
                Return TotaalAlternatief - TotaalNacalc
            End Get
        End Property

    End Class

    Public Class BudgetLotGroepBO

        Public Property LotNummer As Decimal
        Public Property LotNaam As String
        Public Property Lijnen As New List(Of BudgetActivityLijnBO)

        Public ReadOnly Property TotaalAlternatief As Decimal
            Get
                Return Lijnen.Sum(Function(l) l.TotaalAlternatief)
            End Get
        End Property

        Public ReadOnly Property TotaalNacalc As Decimal
            Get
                Return Lijnen.Sum(Function(l) l.TotaalNacalc)
            End Get
        End Property

    End Class

End Namespace
