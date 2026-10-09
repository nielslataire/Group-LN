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
        ''' <summary>Nacalc-referentie per eenheid zoals bewaard bij de lijn: al geïndexeerd naar de huidige index van de versie
        ''' (gevuld bij opslaan vanuit <see cref="ReferentiePrijsPerEenheid"/>).</summary>
        Public Property NacalcPrijsPerEenheid As Decimal
        Public Property Correctiefactor As Decimal = 1D
        Public Property IsManueel As Boolean
        ''' <summary>Opmerking bij de correctie (39k: correctie ≠ 100 % zonder opmerking is een waarschuwing); bewaard in BudgetActivityLijnen.Omschrijving.</summary>
        Public Property Opmerking As String

        ' ── Nacalc-referentie (okt. 2026): gemiddelde werkelijke kost van de gekozen referentieprojecten, niet opgeslagen ──
        ''' <summary>Geïndexeerde referentieprijs per eenheid uit de gekozen referentieprojecten; leeg zonder referentie.</summary>
        Public Property ReferentiePrijsPerEenheid As Decimal?
        Public Property ReferentiePrijsPerM2 As Decimal?
        Public Property ReferentieAantalProjecten As Integer
        Public Property ReferentieMinPerEenheid As Decimal?
        Public Property ReferentieMaxPerEenheid As Decimal?

        Public ReadOnly Property HeeftReferentie As Boolean
            Get
                Return ReferentiePrijsPerEenheid.HasValue AndAlso ReferentiePrijsPerEenheid.Value > 0D
            End Get
        End Property

        ''' <summary>Referentiekost voor dit project = referentie per eenheid × aantal eenheden.</summary>
        Public ReadOnly Property ReferentieTotaal As Decimal?
            Get
                If HeeftReferentie Then Return ReferentiePrijsPerEenheid.Value * AantalEenheden
                Return Nothing
            End Get
        End Property

        ''' <summary>Budget t.o.v. referentie als fractie (+0,08 = budget ligt 8 % boven de referentie).</summary>
        Public ReadOnly Property ReferentieVerschilPerc As Decimal?
            Get
                If ReferentieTotaal.HasValue AndAlso ReferentieTotaal.Value > 0D Then Return (TotaalAlternatief - ReferentieTotaal.Value) / ReferentieTotaal.Value
                Return Nothing
            End Get
        End Property

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

        Public Property VoorstelGipsPrijs As Decimal?
        Public Property VoorstelGipsOpp As Decimal?
        Public Property VoorstelGipsAantalEenheden As Integer?

        ' Generiek voorstel voor enkelvoudige activiteiten (prijs × hoeveelheid)
        Public Property VoorstelEnkelPrijs As Decimal?
        Public Property VoorstelEnkelHoeveelheid As Decimal?
        Public Property VoorstelEnkelEenheid As String
        Public Property VoorstelEnkelLabel As String
        Public Property VoorstelEnkelDetail As String

        Public ReadOnly Property HeeftVoorstel As Boolean
            Get
                Return VoorgesteldePrijsPerEenheid.HasValue AndAlso VoorgesteldePrijsPerEenheid.Value > 0
            End Get
        End Property

        ''' <summary>De bewaarde nacalc is al geïndexeerd naar de huidige index van de versie (zie NacalcPrijsPerEenheid);
        ''' er wordt dus niet nog eens met de start→huidig-factor vermenigvuldigd.</summary>
        Public ReadOnly Property NacalcGeindexeerd As Decimal
            Get
                Return NacalcPrijsPerEenheid
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
