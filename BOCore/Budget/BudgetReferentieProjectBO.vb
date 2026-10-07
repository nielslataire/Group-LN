Namespace Budget

    ''' <summary>
    ''' Referentieproject voor de nacalculatie in de budgetwizard (stap 6): een afgewerkt project met zijn werkelijke kost per
    ''' activiteit, ingeladen uit de app (contracten/inkomende facturen) of uit Excel (projecten van vóór de app).
    ''' De bedragen zijn totalen per activiteit op de peildatum; indexatie naar vandaag gebeurt met SIndex/IIndex van die datum.
    ''' </summary>
    Public Class BudgetReferentieProjectBO
        Public Property Id As Integer
        Public Property Naam As String
        Public Property ProjectId As Integer?
        Public Property ProjectNaam As String
        ''' <summary>Peildatum van de bedragen (oplevering / einde uitvoering).</summary>
        Public Property Datum As Date?
        ''' <summary>Woon-/commerciële eenheden: deler voor de prijs per eenheid.</summary>
        Public Property AantalEenheden As Integer
        ''' <summary>Bewoonbare oppervlakte (GBA) in m²: deler voor de prijs per m².</summary>
        Public Property OppervlakteGBA As Decimal?
        Public Property SIndex As Decimal?
        Public Property IIndex As Decimal?
        ''' <summary>"Excel" of "Project".</summary>
        Public Property Bron As String
        Public Property Opmerking As String
        Public Property CreatedAt As Date

        Public Property Lijnen As New List(Of BudgetReferentieLijnBO)

        Public ReadOnly Property TotaalBedrag As Decimal
            Get
                Return Lijnen.Sum(Function(l) l.Bedrag)
            End Get
        End Property

        Public ReadOnly Property PrijsPerEenheid As Decimal?
            Get
                If AantalEenheden > 0 Then Return TotaalBedrag / AantalEenheden
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property PrijsPerM2 As Decimal?
            Get
                If OppervlakteGBA.HasValue AndAlso OppervlakteGBA.Value > 0D Then Return TotaalBedrag / OppervlakteGBA.Value
                Return Nothing
            End Get
        End Property
    End Class

    Public Class BudgetReferentieLijnBO
        Public Property Id As Integer
        Public Property ActivityId As Integer
        Public Property ActivityOmschrijving As String
        Public Property LotNummer As Decimal
        Public Property LotNaam As String
        ''' <summary>Werkelijke kost van de activiteit in het referentieproject (excl. btw, niet geïndexeerd).</summary>
        Public Property Bedrag As Decimal
        Public Property Opmerking As String
    End Class

    ''' <summary>
    ''' Nacalc-referentie voor één activiteit: wat de gekozen referentieprojecten er gemiddeld voor betaalden,
    ''' geïndexeerd naar de huidige index van de budgetversie.
    ''' </summary>
    Public Class NacalcReferentieBO
        Public Property ActivityId As Integer
        ''' <summary>Aantal referentieprojecten met een bedrag voor deze activiteit.</summary>
        Public Property AantalProjecten As Integer
        ''' <summary>Σ geïndexeerd bedrag ÷ Σ eenheden van die projecten.</summary>
        Public Property PrijsPerEenheid As Decimal
        ''' <summary>Σ geïndexeerd bedrag ÷ Σ GBA van de projecten mét oppervlakte; leeg zonder oppervlaktes.</summary>
        Public Property PrijsPerM2 As Decimal?
        ''' <summary>Laagste en hoogste geïndexeerde prijs per eenheid over de projecten (spreiding).</summary>
        Public Property MinPerEenheid As Decimal
        Public Property MaxPerEenheid As Decimal
    End Class

End Namespace
