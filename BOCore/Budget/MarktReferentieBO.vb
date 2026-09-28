Namespace Budget

    ''' <summary>
    ''' Eén vergelijkbare unit uit de marktdata (project-unit of los pand) in de gemeente van het project.
    ''' Geleverd door CPMCore (enige laag met toegang tot de MarketData-database); de statistiek
    ''' erop gebeurt in ServiceCore (VerkoopVoorstelService.VerrijkMetMarkt).
    ''' </summary>
    Public Class MarktReferentieUnitBO
        ''' <summary>"Appartement", "Woning" of "Overig".</summary>
        Public Property PropertyType As String
        Public Property LivingArea As Decimal?
        Public Property Prijs As Decimal?
        Public Property PrijsPerM2 As Decimal?
        Public Property IsVerkocht As Boolean
        Public Property VerkochtOp As Date?
        Public Property DoorlooptijdDagen As Integer?
        Public Property IsProjectUnit As Boolean
        ''' <summary>True voor een eigen verkochte Unit uit CPM (werkelijke verkoopprijs, geen vraagprijs).</summary>
        Public Property IsEigenVerkoop As Boolean
    End Class

    ''' <summary>
    ''' Marktreferentie voor een project: de vergelijkbare units in dezelfde gemeente plus de
    ''' afgeleide gemeentecijfers (aanbod, verkopen in de periode, absorptie, doorlooptijd).
    ''' </summary>
    Public Class MarktReferentieBO
        Public Property GemeenteNaam As String
        Public Property Postcode As String
        Public Property GeoMunicipalityId As Integer?
        Public Property PeriodeMaanden As Integer
        ''' <summary>Uitleg over de herkomst, of waarom er geen referentie is (geen postcode, geen marktdata, ...).</summary>
        Public Property Bron As String
        Public Property Units As New List(Of MarktReferentieUnitBO)

        ' ── Gemeentecijfers (ingevuld door ServiceCore) ───────────────────
        Public Property AantalTeKoop As Integer
        Public Property VerkochtInPeriode As Integer
        Public Property AbsorptiePerMaand As Decimal?
        Public Property MediaanDoorlooptijdDagen As Integer?
        Public Property MediaanPrijsPerM2TeKoop As Decimal?
        Public Property MediaanPrijsPerM2Verkocht As Decimal?

        ''' <summary>Eigen verkochte Units (CPM) in dezelfde postcode: werkelijke verkoopprijzen.</summary>
        Public Property EigenVerkopen As Integer
        Public Property MediaanPrijsPerM2EigenVerkoop As Decimal?

        Public ReadOnly Property HeeftData As Boolean
            Get
                Return Units.Count > 0
            End Get
        End Property
    End Class

End Namespace
