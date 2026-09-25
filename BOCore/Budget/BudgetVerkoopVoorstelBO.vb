Namespace Budget

    ''' <summary>
    ''' Verkoopvoorstel voor één eenheid uit BudgetOppervlaktes: kostprijsaandeel,
    ''' grondwaarde en bouwwaarde (kost + marge) en de resulterende minimumverkoopprijs.
    ''' </summary>
    Public Class BudgetVerkoopVoorstelEenheidBO
        Public Property EenheidNaam As String
        Public Property GroupTypeName As String
        Public Property TypeName As String

        Public Property BewoonbareOpp As Decimal
        Public Property OppGereduceerd As Decimal
        Public Property Grondopp As Decimal

        ''' <summary>Aandeel (fractie) van deze eenheid in de bouwkost/bouwwaarde.</summary>
        Public Property AandeelBouw As Decimal
        ''' <summary>Aandeel (fractie) van deze eenheid in de grondkost/grondwaarde.</summary>
        Public Property AandeelGrond As Decimal

        Public Property KostprijsBouw As Decimal
        Public Property KostprijsGrond As Decimal
        Public Property Bouwwaarde As Decimal
        Public Property Grondwaarde As Decimal

        Public ReadOnly Property Kostprijs As Decimal
            Get
                Return KostprijsBouw + KostprijsGrond
            End Get
        End Property

        Public ReadOnly Property MinimumVerkoopprijs As Decimal
            Get
                Return Bouwwaarde + Grondwaarde
            End Get
        End Property

        Public ReadOnly Property PrijsPerM2Bewoonbaar As Decimal?
            Get
                If BewoonbareOpp > 0D Then Return MinimumVerkoopprijs / BewoonbareOpp
                Return Nothing
            End Get
        End Property

        ' ── Marktreferentie (increment 2) ─────────────────────────────────
        ''' <summary>Aantal vergelijkbare units waarop de mediaan gebaseerd is; 0 = geen referentie.</summary>
        Public Property MarktAantal As Integer
        ''' <summary>Mediaan €/m² bewoonbaar van de vergelijkbare units.</summary>
        Public Property MarktMediaanPerM2 As Decimal?
        Public Property MarktP25PerM2 As Decimal?
        Public Property MarktP75PerM2 As Decimal?
        ''' <summary>"verkocht", "te koop" of "te koop + verkocht": welke set de mediaan leverde.</summary>
        Public Property MarktBasis As String
        ''' <summary>Hoe breed vergeleken is: "zelfde type, opp. ±20 %", "zelfde type" of "alle types".</summary>
        Public Property MarktVergelijking As String

        ''' <summary>Marktprijs = mediaan €/m² × bewoonbare oppervlakte.</summary>
        Public ReadOnly Property MarktPrijs As Decimal?
            Get
                If MarktMediaanPerM2.HasValue AndAlso BewoonbareOpp > 0D Then Return Math.Round(MarktMediaanPerM2.Value * BewoonbareOpp, 0)
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property MarktPrijsLaag As Decimal?
            Get
                If MarktP25PerM2.HasValue AndAlso BewoonbareOpp > 0D Then Return Math.Round(MarktP25PerM2.Value * BewoonbareOpp, 0)
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property MarktPrijsHoog As Decimal?
            Get
                If MarktP75PerM2.HasValue AndAlso BewoonbareOpp > 0D Then Return Math.Round(MarktP75PerM2.Value * BewoonbareOpp, 0)
                Return Nothing
            End Get
        End Property

        ''' <summary>Marktprijs t.o.v. minimumverkoopprijs, als fractie (0.08 = markt ligt 8 % hoger).</summary>
        Public ReadOnly Property MarktVerschilPerc As Decimal?
            Get
                If MarktPrijs.HasValue AndAlso MinimumVerkoopprijs > 0D Then Return (MarktPrijs.Value - MinimumVerkoopprijs) / MinimumVerkoopprijs
                Return Nothing
            End Get
        End Property

        ''' <summary>Aanbevolen vraagprijs: de marktprijs, maar nooit onder de minimumverkoopprijs.</summary>
        Public ReadOnly Property AanbevolenVraagprijs As Decimal
            Get
                If MarktPrijs.HasValue AndAlso MarktPrijs.Value > MinimumVerkoopprijs Then Return MarktPrijs.Value
                Return MinimumVerkoopprijs
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Verkoopvoorstel van een budgetversie: totale kostprijs incl. grond, gesplitst in
    ''' grondkost en bouwkost, met marges vertaald naar grondwaarde en bouwwaarde en
    ''' verdeeld over de eenheden.
    ''' </summary>
    Public Class BudgetVerkoopVoorstelBO
        Public Property BudgetVersieId As Integer

        ''' <summary>Fractie, bv. 0.15.</summary>
        Public Property DoelMargePerc As Decimal
        ''' <summary>Fractie, bv. 0.10.</summary>
        Public Property GrondMargePerc As Decimal

        ''' <summary>Aankoopprijs grond + grondgebonden kosten (infrastructuur, opmeting/sondering, straight loan grond).</summary>
        Public Property GrondKost As Decimal
        ''' <summary>Alle overige kosten uit het budgetresultaat (bouwkost, erelonen, verzekeringen, financiering gebouw, onvoorzien, ...).</summary>
        Public Property BouwKost As Decimal

        Public Property Grondwaarde As Decimal
        Public Property Bouwwaarde As Decimal

        ''' <summary>Waarop de bouwwaarde verdeeld is: "gereduceerde oppervlakte", "bewoonbare oppervlakte" of "gelijk per eenheid".</summary>
        Public Property VerdeelsleutelBouw As String
        ''' <summary>Waarop de grondwaarde verdeeld is: "grondoppervlakte" of dezelfde sleutel als de bouwwaarde.</summary>
        Public Property VerdeelsleutelGrond As String

        Public Property Eenheden As New List(Of BudgetVerkoopVoorstelEenheidBO)
        Public Property Waarschuwingen As New List(Of String)

        ' ── Marktreferentie (increment 2) ─────────────────────────────────
        ''' <summary>Gemeentecijfers + vergelijkbare units; Nothing als er geen marktdata gekoppeld kon worden.</summary>
        Public Property Markt As MarktReferentieBO

        ''' <summary>Som van de marktprijzen van de eenheden mét referentie.</summary>
        Public ReadOnly Property MarktTotaal As Decimal?
            Get
                Dim metMarkt = Eenheden.Where(Function(e) e.MarktPrijs.HasValue).ToList()
                If metMarkt.Count = 0 Then Return Nothing
                Return metMarkt.Sum(Function(e) e.MarktPrijs.Value)
            End Get
        End Property

        ''' <summary>Minimumverkoopprijs van diezelfde eenheden, om MarktTotaal eerlijk mee te vergelijken.</summary>
        Public ReadOnly Property MinimumTotaalMetMarkt As Decimal?
            Get
                Dim metMarkt = Eenheden.Where(Function(e) e.MarktPrijs.HasValue).ToList()
                If metMarkt.Count = 0 Then Return Nothing
                Return metMarkt.Sum(Function(e) e.MinimumVerkoopprijs)
            End Get
        End Property

        Public ReadOnly Property AantalEenhedenMetMarkt As Integer
            Get
                Return Eenheden.Where(Function(e) e.MarktPrijs.HasValue).Count()
            End Get
        End Property

        Public ReadOnly Property TotaalAanbevolenVraagprijs As Decimal
            Get
                Return Eenheden.Sum(Function(e) e.AanbevolenVraagprijs)
            End Get
        End Property

        Public ReadOnly Property TotaalKostprijs As Decimal
            Get
                Return GrondKost + BouwKost
            End Get
        End Property

        Public ReadOnly Property TotaalVerkoopwaarde As Decimal
            Get
                Return Grondwaarde + Bouwwaarde
            End Get
        End Property

        Public ReadOnly Property MargeBedrag As Decimal
            Get
                Return TotaalVerkoopwaarde - TotaalKostprijs
            End Get
        End Property

        ''' <summary>Marge als fractie van de kostprijs.</summary>
        Public ReadOnly Property MargePerc As Decimal
            Get
                If TotaalKostprijs > 0D Then Return MargeBedrag / TotaalKostprijs
                Return 0D
            End Get
        End Property

        Public ReadOnly Property AantalEenheden As Integer
            Get
                Return Eenheden.Count
            End Get
        End Property

        Public ReadOnly Property HeeftEenheden As Boolean
            Get
                Return Eenheden.Count > 0
            End Get
        End Property
    End Class

End Namespace
