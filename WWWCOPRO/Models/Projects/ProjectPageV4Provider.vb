Imports BO
' Levert het ProjectPageV4Model aan de controller.
'
' Nu: voorbeeldinhoud uit de design-handoff ("Verkaveling Keerstraat"), zodat de view
' volledig getest kan worden. Later: vervangen door de blokconfiguratie die CPMCore per
' project bewaart (zelfde modelvorm; bv. JSON -> ProjectPageV4Model). Enkel deze klasse
' hoeft dan aan te passen, view/CSS/JS blijven ongewijzigd.
Public Class ProjectPageV4Provider

    Public Shared Function Load(slug As String) As ProjectPageV4Model
        ' TODO CPMCore: model per project ophalen (slug / project-id) en hier teruggeven.
        Dim m = BuildKeerstraatSample()
        If Not String.IsNullOrWhiteSpace(slug) Then m.Slug = slug
        Return m
    End Function

    ' ───────── Echte projectdata (zelfde bronnen als Views/Projects/Detail) ─────────
    ' Wat uit het bestaande datamodel komt: naam, adres, foto's, woningen (oppervlaktes, slaapkamers,
    ' status, prijs + afwerkingsopties), commerciële tekst en oplevering.
    ' Blokken waarvoor (nog) geen brondata bestaat (verhaal, architectuur, ligging, bouwfase, vragen)
    ' staan op Visible = False tot CPMCore ze aanlevert.
    Public Shared Function FromProject(data As ProjectBO, units As IEnumerable(Of UnitWithDetailsBO),
                                       settings As ProjectSalesSettingsBO, imageBase As String,
                                       canonicalUrl As String,
                                       Optional web As ProjectWebsiteContent = Nothing) As ProjectPageV4Model
        Dim nl = System.Globalization.CultureInfo.GetCultureInfo("nl-BE")
        Dim gemeente As String = If(data.Postalcode IsNot Nothing, data.Postalcode.Gemeente, "")
        Dim saleVisible As Boolean = settings IsNot Nothing AndAlso settings.SaleVisible

        Dim m As New ProjectPageV4Model With {
            .ProjectId = data.Id,
            .Slug = data.Slug,
            .Name = data.Name,
            .Deelgemeente = gemeente,
            .Gemeente = gemeente,
            .Street = (If(data.Street, "") & " " & If(data.HouseNumber, "")).Trim(),
            .Postcode = If(data.Postalcode IsNot Nothing, data.Postalcode.Postcode, ""),
            .CanonicalUrl = canonicalUrl
        }

        ' ── Foto's (geen video's) → linkerbeelden, per blok doorgeschoven ──
        Dim videoExts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {".mp4", ".webm", ".mov", ".avi"}
        Dim pics As New List(Of ProjectPictureBO)
        If data.DefaultPicture IsNot Nothing Then pics.Add(data.DefaultPicture)
        If data.Pictures IsNot Nothing Then
            pics.AddRange(data.Pictures.Where(Function(x) data.DefaultPicture Is Nothing OrElse x.Id <> data.DefaultPicture.Id))
        End If
        Dim urls = pics.Where(Function(x) Not String.IsNullOrWhiteSpace(x.Name) AndAlso x.MediaType <> 1 AndAlso Not videoExts.Contains(System.IO.Path.GetExtension(x.Name))).
                        Select(Function(x) New With {.Url = imageBase & "pictures/" & x.Name, .Alt = If(Not String.IsNullOrWhiteSpace(x.Caption), x.Caption, data.Name)}).ToList()
        If urls.Count > 0 Then m.OgImageUrl = urls(0).Url
        Dim keys = {"intro", "story", "architecture", "homes", "location", "buildphase", "buy", "contact"}
        Dim labels = {data.Name & ", " & gemeente, "Het project", "Architectuur", "De woningen", "Ligging", "Bouwfase", "Kopen", "Contact"}
        For i As Integer = 0 To keys.Length - 1
            Dim pic = If(urls.Count > 0, urls(i Mod urls.Count), Nothing)
            m.Media.Add(New V4Media With {.Key = keys(i), .Label = labels(i), .ImageUrl = If(pic IsNot Nothing, pic.Url, ""), .Alt = If(pic IsNot Nothing, pic.Alt, data.Name)})
        Next

        ' ── Woningen ──
        For Each u In units.Where(Function(x) x.Type IsNot Nothing AndAlso x.Type.Id = 2).OrderBy(Function(x) x.Name)
            Dim slpkRoom = u.Rooms.Where(Function(r) r.Type = BO.RoomType.Slaapkamer).FirstOrDefault()
            Dim lot As New V4Lot With {
                .Id = u.Id, .Name = u.Name, .Type = If(u.Type.Name, "Woning"),
                .LivingArea = CInt(u.Surface), .LandArea = CInt(u.GroundSurface),
                .Bedrooms = If(slpkRoom IsNot Nothing, slpkRoom.Number, 0),
                .Status = If(u.ClientAccountId <> 0, "Verkocht", If(u.IsOption, "Optie", "Beschikbaar"))
            }
            If lot.Status = "Beschikbaar" AndAlso saleVisible Then
                If u.FinishingOptions IsNot Nothing AndAlso u.FinishingOptions.Count > 0 Then
                    For Each fo In u.FinishingOptions.OrderBy(Function(f) f.SortOrder)
                        Dim price = u.LandValue + fo.TotalValue
                        If price > 0 Then lot.Finishes.Add(New V4Finish With {.ShortLabel = fo.Name, .Label = fo.Name, .Price = price})
                    Next
                ElseIf u.TotalValue > 0 Then
                    lot.Finishes.Add(New V4Finish With {.ShortLabel = "", .Label = "", .Price = u.TotalValue})
                End If
            End If
            m.Lots.Add(lot)
        Next

        ' ── Projectkaart: gekozen foto + omtrek per eenheid (CPMCore, tab "SEO & website") ──
        Dim hasAerial As Boolean = web IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(web.AerialImageName)
        Dim shapedLots As Integer = 0
        If hasAerial Then
            For Each lot In m.Lots
                Dim sh As V4LotShape = Nothing
                If web.Shapes.TryGetValue(lot.Id, sh) Then
                    lot.Polygon = sh.Polygon
                    lot.LabelX = sh.LabelX
                    lot.LabelY = sh.LabelY
                    shapedLots += 1
                End If
            Next
        End If

        ' ── Intro ──
        ' Website-inhoud uit CPMCore (tab "SEO & website") gaat voor; leeg veld = terugval op de projectgegevens.
        Dim hasWeb As Boolean = web IsNot Nothing
        With m.Intro
            .Eyebrow = If(hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.Location), web.Location, gemeente)
            .TitleLead = If(hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.Title), web.Title, data.Name)
            .TitleEmphasis = If(hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.Subtitle), web.Subtitle,
                                If(hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.Title), "", data.CommercialTitleNL))
            Dim introSource As String = If(hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.IntroText), System.Net.WebUtility.HtmlEncode(web.IntroText).Replace(vbCrLf, vbLf), data.CommercialTextNL)
            Dim plain = System.Text.RegularExpressions.Regex.Replace(If(introSource, ""), "<(br|/p|/div|/li)[^>]*>", vbLf, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            plain = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(plain, "<[^>]+>", ""))
            For Each para In plain.Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries).Select(Function(x) x.Trim()).Where(Function(x) x.Length > 0)
                .Paragraphs.Add(para)
            Next
            ' Vaste kerncijfers (altijd): beschikbaar, vanaf, bewoonbaar. Daarna de eigen kerncijfers uit CPMCore.
            If m.Lots.Count > 0 Then
                .Facts.Add(New V4Fact With {.Label = "Beschikbaar", .Value = m.Lots.Where(Function(l) l.Status = "Beschikbaar").Count() & " van " & m.Lots.Count})
                If saleVisible AndAlso m.MinPrice.HasValue Then .Facts.Add(New V4Fact With {.Label = "Vanaf", .Value = V4Format.Eur(m.MinPrice.Value)})
                .Facts.Add(New V4Fact With {.Label = "Bewoonbaar", .Value = V4Format.Range(m.Lots.Select(Function(l) CDec(l.LivingArea)), " m²")})
            End If
            If hasWeb Then .Facts.AddRange(web.Kpis)
        End With

        ' ── Verhaal ──
        Dim storyHasContent As Boolean = False
        If hasWeb Then
            With m.Story
                .Title = web.StoryTitle
                .Intro = web.StoryText
                .ShowBrochureCta = web.ShowBrochure
                For Each it In web.StoryItems
                    .Figures.Add(New V4Figure With {
                        .ImageUrl = If(String.IsNullOrWhiteSpace(it.ImageUrl), "", imageBase & "pictures/" & it.ImageUrl),
                        .Alt = If(Not String.IsNullOrWhiteSpace(it.Text), it.Text, data.Name),
                        .Text = it.Text})
                Next
            End With
            storyHasContent = Not String.IsNullOrWhiteSpace(web.StoryTitle) OrElse Not String.IsNullOrWhiteSpace(web.StoryText) OrElse web.StoryItems.Count > 0
        End If

        ' ── Architectuur ──
        Dim archHasContent As Boolean = False
        If hasWeb Then
            With m.Architecture
                .Eyebrow = web.ArchEyebrow
                .Title = web.ArchTitle
                For Each para In SplitParagraphs(web.ArchText)
                    .Paragraphs.Add(para)
                Next
                .Quotes.AddRange(web.Quotes)
                For Each d In web.Details
                    .Materials.Add(New V4Material With {
                        .Title = d.Title, .Text = d.Text,
                        .ImageUrl = If(String.IsNullOrWhiteSpace(d.ImageUrl), "", imageBase & "pictures/" & d.ImageUrl),
                        .Alt = d.Title})
                Next
                .ShowSpecCta = False
            End With
            archHasContent = Not String.IsNullOrWhiteSpace(web.ArchTitle) OrElse Not String.IsNullOrWhiteSpace(web.ArchText) OrElse
                             Not String.IsNullOrWhiteSpace(web.ArchEyebrow) OrElse web.Quotes.Count > 0 OrElse web.Details.Count > 0
        End If

        ' ── Volgorde + zichtbaarheid (CPMCore "Blokken op de pagina") ──
        Dim visibleFlags As New Dictionary(Of String, Boolean) From {{"story", storyHasContent}, {"architecture", archHasContent}, {"homes", True}, {"buy", True}}
        Dim order As New List(Of String)
        If hasWeb AndAlso Not String.IsNullOrWhiteSpace(web.BlocksJson) Then
            Try
                Dim items = New System.Web.Script.Serialization.JavaScriptSerializer().Deserialize(Of List(Of Dictionary(Of String, Object)))(web.BlocksJson)
                For Each it In items
                    Dim key As String = TryCast(it("key"), String)
                    If String.IsNullOrEmpty(key) OrElse order.Contains(key) Then Continue For
                    order.Add(key)
                    If visibleFlags.ContainsKey(key) Then visibleFlags(key) = (TypeOf it("visible") Is Boolean AndAlso CBool(it("visible")))
                Next
            Catch
                ' onleesbare configuratie: standaardvolgorde
            End Try
        End If
        For Each k In m.BlockOrder
            If Not order.Contains(k) Then order.Add(k)
        Next
        m.BlockOrder = order

        m.Story.Visible = visibleFlags("story") AndAlso storyHasContent
        m.Architecture.Visible = visibleFlags("architecture") AndAlso archHasContent
        m.Location.Visible = False
        m.BuildPhase.Visible = False
        m.Faq.Visible = False

        With m.Homes
            .Visible = m.Lots.Count > 0 AndAlso visibleFlags("homes")
            .Title = If(web IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(web.HomesTitle), web.HomesTitle, "De woningen")
            .Intro = If(web IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(web.HomesIntro), web.HomesIntro, "Bekijk de beschikbare woningen, hun oppervlaktes en prijzen.")
            .ShowMap = hasAerial AndAlso shapedLots > 0
            If hasAerial Then
                .AerialImageUrl = imageBase & "pictures/" & web.AerialImageName
                .AerialAlt = "Luchtfoto van " & data.Name
            End If
            .ShowPriceList = saleVisible
            .ShowCalculator = saleVisible
            .ShowDocuments = False
            .PriceListNote = "Prijzen excl. btw, registratie- en notariskosten."
        End With
        If Not saleVisible Then m.Intro.SecondaryCtaLabel = ""

        m.Buy.Steps.AddRange(DefaultBuySteps())
        m.Buy.Visible = visibleFlags("buy")
        Return m
    End Function

    Private Shared Function SplitParagraphs(text As String) As IEnumerable(Of String)
        If String.IsNullOrWhiteSpace(text) Then Return New String() {}
        Return text.Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries).Select(Function(x) x.Trim()).Where(Function(x) x.Length > 0)
    End Function

    Private Shared Function DefaultBuySteps() As IEnumerable(Of V4TitleText)
        Return {
            TT("Kennismaking", "Een gesprek over je wensen, budget en timing."),
            TT("Plannen & keuze", "Samen overlopen we plannen, lastenboek en prijs."),
            TT("Reservatie", "We houden je woning vrij terwijl je financiering rond raakt."),
            TT("Compromis & akte", "Wij begeleiden je tot bij de notaris."),
            TT("Afwerking kiezen", "Keuken, sanitair en vloeren bij onze partners."),
            TT("Sleutels", "Samen lopen we alles na. Welkom thuis.")
        }
    End Function

    Private Shared Function Fin(shortLabel As String, label As String, price As Decimal, desc As String) As V4Finish
        Return New V4Finish With {.ShortLabel = shortLabel, .Label = label, .Price = price, .Description = desc}
    End Function

    Private Shared Function TT(title As String, text As String) As V4TitleText
        Return New V4TitleText With {.Title = title, .Text = text}
    End Function

    Private Shared Function BuildKeerstraatSample() As ProjectPageV4Model
        Dim m As New ProjectPageV4Model With {
            .ProjectId = 0,
            .Slug = "verkaveling-keerstraat",
            .Name = "Verkaveling Keerstraat",
            .Deelgemeente = "Vlekkem",
            .Gemeente = "Erpe-Mere",
            .Provincie = "Oost-Vlaanderen",
            .Street = "Keerstraat 237",
            .Postcode = "9420",
            .Lat = 50.9155,
            .Lng = 3.939,
            .MetaTitle = "Verkaveling Keerstraat — 4 nieuwbouwwoningen in Vlekkem (Erpe-Mere) | Group LN",
            .MetaDescription = "Vier energiezuinige nieuwbouwwoningen in Vlekkem, Erpe-Mere. Lot 3 (halfopen, 200 m², 3 slaapkamers) beschikbaar vanaf € 567.000.",
            .CanonicalUrl = "https://www.groupln.be/woonprojecten/verkaveling-keerstraat"
        }

        ' ── Linkerbeelden ──
        m.Media.AddRange({
            New V4Media With {.Key = "intro", .Label = "Keerstraat, Vlekkem", .Alt = "Gevel van de woningen aan de Keerstraat bij avondlicht"},
            New V4Media With {.Key = "story", .Label = "Een dag aan de Keerstraat", .ImageUrl = "~/Content/img/about.webp", .Alt = "Interieur met veel licht en hout"},
            New V4Media With {.Key = "architecture", .Label = "Architectuur", .Alt = "Gevel, detail van de materialen"},
            New V4Media With {.Key = "homes", .Label = "De woningen", .Alt = "Render van de woningen"},
            New V4Media With {.Key = "location", .Label = "Ligging", .Alt = "Luchtfoto van de velden rond Vlekkem"},
            New V4Media With {.Key = "buildphase", .Label = "Bouwfase", .ImageUrl = "~/Content/img/grondverwerving/bouwgrond.webp", .Alt = "Werffoto"},
            New V4Media With {.Key = "buy", .Label = "Kopen", .Alt = "Leefruimte van een woning"},
            New V4Media With {.Key = "contact", .Label = "Contact", .Alt = "Het team van Group LN"}
        })

        ' ── Loten ──
        m.Lots.Add(New V4Lot With {.Id = 1, .Name = "Lot 1", .Type = "Halfopen", .LivingArea = 200, .LandArea = 467, .Bedrooms = 3, .Bathrooms = 1, .GardenOrientation = "Zuidwest", .EnergyLevel = "E20", .Status = "Verkocht",
                                   .Description = "Halfopen woning aan de straat, met een tuin op het zuidwesten.", .Polygon = "6,20 30,18 31,48 7,50", .LabelX = 18.5, .LabelY = 34})
        m.Lots.Add(New V4Lot With {.Id = 2, .Name = "Lot 2", .Type = "Gesloten", .LivingArea = 230, .LandArea = 411, .Bedrooms = 4, .Bathrooms = 2, .GardenOrientation = "Zuid", .EnergyLevel = "E20", .Status = "Verkocht",
                                   .Description = "Gesloten bebouwing in het midden van het straatfront, met vier slaapkamers.", .Polygon = "31,18 52,17 53,47 31,48", .LabelX = 41.8, .LabelY = 32.5})
        Dim lot3 As New V4Lot With {.Id = 3, .Name = "Lot 3", .Type = "Halfopen", .LivingArea = 200, .LandArea = 404, .Bedrooms = 3, .Bathrooms = 1, .GardenOrientation = "Zuidoost", .EnergyLevel = "E20", .Status = "Beschikbaar",
                                    .Description = "De laatste vrije woning. Halfopen, met een tuin op het zuidoosten en ochtendzon op het terras.", .Polygon = "53,17 74,16 75,46 53,47", .LabelX = 63.8, .LabelY = 31.5, .FloorPlanUrl = "#"}
        lot3.Finishes.Add(Fin("Afgewerkt", "Volledig afgewerkt", 567000D, "Instapklaar, afwerking naar keuze binnen budget"))
        lot3.Finishes.Add(Fin("Casco", "Casco", 512000D, "Zonder binnenafwerking, technieken voorzien"))
        m.Lots.Add(lot3)
        m.Lots.Add(New V4Lot With {.Id = 4, .Name = "Lot 4", .Type = "Open", .LivingArea = 241, .LandArea = 2081, .Bedrooms = 4, .Bathrooms = 2, .GardenOrientation = "West", .EnergyLevel = "E15", .Status = "Verkocht",
                                   .Description = "Open bebouwing achteraan, bereikbaar via een private oprit, op een perceel van ruim 2.000 m².", .Polygon = "22,58 92,55 94,92 24,95", .LabelX = 58, .LabelY = 75, .Zoom = 1.25})

        ' ── Intro ──
        With m.Intro
            .Eyebrow = "Vlekkem · Erpe-Mere · Oost-Vlaanderen"
            .TitleLead = "Vier nieuwbouwwoningen,"
            .TitleEmphasis = "waar de rust begint."
            .Paragraphs.Add("Verkaveling Keerstraat is een kleinschalig nieuwbouwproject van Group LN in Vlekkem, een landelijke deelgemeente van Erpe-Mere in Oost-Vlaanderen. Op de site aan de Keerstraat 237 bouwen we vier energiezuinige woningen, ontworpen door TARCH Architectenbureau.")
            .Paragraphs.Add("Aan de straat staan drie woningen: twee halfopen en één gesloten, op percelen van 404 tot 467 m². Achteraan, bereikbaar via een private oprit, ligt een vrijstaande woning op een perceel van 2.081 m². Lot 3, een halfopen woning van 200 m² met drie slaapkamers, is nog beschikbaar vanaf € 567.000. De oplevering is gepland in het voorjaar van 2027.")
            .Facts.AddRange({
                New V4Fact With {.Label = "Beschikbaar", .Value = "1 van 4"},
                New V4Fact With {.Label = "Vanaf", .Value = "€ 567.000"},
                New V4Fact With {.Label = "Oplevering", .Value = "Voorjaar 2027"},
                New V4Fact With {.Label = "Bewoonbaar", .Value = "200–241 m²"},
                New V4Fact With {.Label = "Percelen", .Value = "404–2.081 m²"},
                New V4Fact With {.Label = "E-peil", .Value = "≤ E20"}
            })
        End With

        ' ── Verhaal ──
        With m.Story
            .Title = "Waar het dorp ophoudt en de velden beginnen."
            .Intro = "Vlekkem ontdek je pas als je er bent. Een kerk, een school, een bakker. En aan de rand, waar de Keerstraat uitkijkt over het open land, vier nieuwe woningen — bewust bescheiden van schaal, zodat ze in het dorp passen in plaats van het te overstemmen."
            .Figures.Add(New V4Figure With {.Alt = "Straatbeeld Keerstraat met de velden", .Text = "Geen doorgaand verkeer, geen buren op je bord. Achter de tuinen begint het open land, voor de deur het dorp."})
            .Figures.Add(New V4Figure With {.ImageUrl = "~/Content/img/about.webp", .Alt = "Interieur met licht en hout", .Text = "Binnen draait alles om licht. Grote ramen openen de leefruimte naar de tuin, zodat je de seizoenen elke dag meekrijgt."})
        End With

        ' ── Architectuur ──
        With m.Architecture
            .Eyebrow = "Architectuur door TARCH"
            .Title = "Strak en doordacht. Warm waar je het aanraakt."
            .Paragraphs.Add("TARCH Architectenbureau tekende vier woningen die de schaal en het ritme van de Keerstraat respecteren. Zadeldaken en een rustige gevelopbouw sluiten aan bij de landelijke omgeving; grote raampartijen en een open plan maken ze eigentijds.")
            .Paragraphs.Add("De materialen zijn gekozen om mooi te verouderen en weinig onderhoud te vragen. Elke woning heeft een eigen oriëntatie, zodat leefruimte en tuin het beste licht van de dag krijgen.")
            .Quotes.Add(New V4Quote With {.Text = "Woningen die niet om aandacht vragen, maar die je elke dag opnieuw aangenaam verrassen.", .Person = "Group LN over Keerstraat"})
            .Materials.Add(New V4Material With {.Title = "Lichte gevelsteen", .Text = "Een zachte, lichtgrijze handvormsteen die met de jaren mooier wordt. Hij past in het landelijke straatbeeld en vraagt nauwelijks onderhoud.", .Alt = "Detail van de gevelsteen"})
            .Materials.Add(New V4Material With {.Title = "Houten lamellen", .Text = "Verticale lamellen rond ramen en garagepoort geven privacy en warmte. In de zomer temperen ze de zon, zodat het binnen aangenaam koel blijft.", .Alt = "Detail van de houten lamellen"})
            .Materials.Add(New V4Material With {.Title = "Grote raampartijen", .Text = "Aluminium schrijnwerk met driedubbele beglazing. De leefruimtes openen zich naar de tuin: veel daglicht en een directe band met buiten.", .ImageUrl = "~/Content/img/about.webp", .Alt = "Raampartij naar de tuin"})
            .ComfortTitle = "Comfort en energie"
            .ComfortIntro = "Alle woningen halen een E-peil van E20 of lager. Ze zijn klaar voor een toekomst zonder fossiele brandstoffen."
            .Techniques.AddRange({
                TT("Warmtepomp", "Lucht-waterwarmtepomp voor verwarming en sanitair warm water."),
                TT("Vloerverwarming", "Op gelijkvloers en verdieping, voor gelijkmatige warmte."),
                TT("Ventilatie D", "Mechanische ventilatie met warmterecuperatie."),
                TT("Zonnepanelen", "Dak en bekabeling zijn voorbereid voor PV-panelen."),
                TT("Isolatie", "Hoogwaardige isolatie van dak, muren en vloer; luchtdicht gebouwd."),
                TT("Regenwater", "Regenwaterput voor toilet, wasmachine en tuin.")
            })
            .SpecCtaTitle = "Alle technische details"
            .SpecCtaText = "Het lastenboek beschrijft materialen, technieken en afwerking per woning."
            .SpecCtaLabel = "⤓ Lastenboek"
            .SpecCtaUrl = "#"
        End With

        ' ── Woningen ──
        With m.Homes
            .Title = "De woningen"
            .Intro = "Vier woningen op vier percelen. Kies een lot op de projectkaart of hieronder om in te zoomen."
            .AerialAlt = "Luchtfoto van het terrein, straat bovenaan"
            .PriceListNote = "Prijzen excl. btw, registratie- en notariskosten."
            .PriceListUpdated = "22 september 2026"
            .SpecDocUrl = "#"
            .Documents.AddRange({
                New V4Doc With {.Title = "Brochure", .Meta = "Project & woningen · 12 MB", .Url = "#"},
                New V4Doc With {.Title = "Lastenboek volledig afgewerkt", .Meta = "Materialen & technieken · 3 MB", .Url = "#"},
                New V4Doc With {.Title = "Lastenboek casco", .Meta = "Wat wel en niet inbegrepen is · 2 MB", .Url = "#"},
                New V4Doc With {.Title = "Inplantingsplan", .Meta = "Percelen & oprit · 1 MB", .Url = "#"}
            })
        End With

        ' ── Ligging ──
        With m.Location
            .Intro = "Vlekkem is een van de acht deelgemeenten van Erpe-Mere, tussen Aalst en Oosterzele. Het is landelijk, maar niet afgelegen: school en bakker te voet, station Erpe-Mere met de fiets, de E40 binnen tien minuten."
            .Guide.AddRange({
                TT("Dorpsleven", "Een kerk, een basisschool, een bakker en een café. Klein en hecht: na een paar weken word je bij naam begroet."),
                TT("Natuur", "Achter het project openen zich de velden. Wandel- en fietsroutes langs de Molenbeekvallei starten zowat aan je voordeur."),
                TT("Onderwijs", "Basisschool Vlekkem ligt op 350 m. Secundaire scholen vind je in Erpe en Aalst, vlot bereikbaar met de fiets of de bus."),
                TT("Bereikbaarheid", "Station Erpe-Mere (lijn Gent–Brussel) op 2,5 km. Met de auto ben je in 12 minuten in Aalst en in een half uur in Gent.")
            })
            .Categories.AddRange({
                New V4PoiCategory With {.Id = "school", .Label = "Scholen", .Icon = "S", .DefaultOn = True},
                New V4PoiCategory With {.Id = "shop", .Label = "Winkels", .Icon = "W", .DefaultOn = True},
                New V4PoiCategory With {.Id = "ov", .Label = "Openbaar vervoer", .Icon = "OV", .DefaultOn = True},
                New V4PoiCategory With {.Id = "groen", .Label = "Groen", .Icon = "G", .DefaultOn = True},
                New V4PoiCategory With {.Id = "sport", .Label = "Sport", .Icon = "Sp", .DefaultOn = False},
                New V4PoiCategory With {.Id = "fiets", .Label = "Fietsbereik", .Icon = "◎", .DefaultOn = False}
            })
            .Pois.AddRange({
                New V4Poi With {.Category = "school", .Name = "Basisschool Vlekkem", .Lat = 50.9178, .Lng = 3.9352, .Distance = "350 m", .Time = "5 min te voet"},
                New V4Poi With {.Category = "school", .Name = "Secundaire school Erpe", .Lat = 50.929, .Lng = 3.966, .Distance = "2,4 km", .Time = "8 min fietsen"},
                New V4Poi With {.Category = "shop", .Name = "Dorpsbakker", .Lat = 50.9168, .Lng = 3.9418, .Distance = "250 m", .Time = "3 min te voet"},
                New V4Poi With {.Category = "shop", .Name = "Supermarkt Erpe-Mere", .Lat = 50.9262, .Lng = 3.9612, .Distance = "2,0 km", .Time = "7 min fietsen"},
                New V4Poi With {.Category = "ov", .Name = "Bushalte Keerstraat", .Lat = 50.9149, .Lng = 3.9362, .Distance = "200 m", .Time = "3 min te voet"},
                New V4Poi With {.Category = "ov", .Name = "Station Erpe-Mere", .Lat = 50.9272, .Lng = 3.969, .Distance = "2,5 km", .Time = "9 min fietsen"},
                New V4Poi With {.Category = "groen", .Name = "Wandelroute Molenbeekvallei", .Lat = 50.9102, .Lng = 3.9305, .Distance = "800 m", .Time = "10 min te voet"},
                New V4Poi With {.Category = "sport", .Name = "Sporthal & voetbalvelden", .Lat = 50.923, .Lng = 3.956, .Distance = "1,6 km", .Time = "6 min fietsen"}
            })
            .TravelTimes.AddRange({
                New V4Travel With {.Destination = "Aalst", .Duration = "12 min"},
                New V4Travel With {.Destination = "E40", .Duration = "8 min"},
                New V4Travel With {.Destination = "Gent", .Duration = "30 min"},
                New V4Travel With {.Destination = "Brussel", .Duration = "35 min"}
            })
            .CtaTitle = "Wil je weten of Vlekkem bij je past?"
            .CtaText = "We kennen de buurt en vertellen je er graag meer over."
        End With

        ' ── Bouwfase ──
        With m.BuildPhase
            .ProgressPercent = 72
            .Milestones.AddRange({
                New V4Milestone With {.DateText = "Mrt 2025", .Title = "Vergunning", .Status = "done"},
                New V4Milestone With {.DateText = "Sep 2025", .Title = "Start werken", .Status = "done"},
                New V4Milestone With {.DateText = "Feb 2026", .Title = "Ruwbouw", .Status = "done"},
                New V4Milestone With {.DateText = "Jun 2026", .Title = "Wind- en waterdicht", .Status = "done"},
                New V4Milestone With {.DateText = "Nu", .Title = "Afwerking", .Status = "now"},
                New V4Milestone With {.DateText = "Voorjaar 2027", .Title = "Oplevering", .Status = "next"}
            })
        End With

        ' ── Kopen ──
        m.Buy.Steps.AddRange({
            TT("Kennismaking", "Een gesprek over je wensen, budget en timing."),
            TT("Plannen & keuze", "Samen overlopen we plannen, lastenboek en prijs."),
            TT("Reservatie", "We houden je woning vrij terwijl je financiering rond raakt."),
            TT("Compromis & akte", "Wij begeleiden je tot bij de notaris."),
            TT("Afwerking kiezen", "Keuken, sanitair en vloeren bij onze partners."),
            TT("Sleutels", "Samen lopen we alles na. Welkom thuis.")
        })

        ' ── Vragen ──
        m.Faq.Items.AddRange({
            TT("Wat kost een nieuwbouwwoning aan de Keerstraat in Vlekkem?", "Lot 3, een halfopen woning van 200 m² op een perceel van 404 m², is beschikbaar vanaf € 567.000. De andere drie woningen zijn verkocht."),
            TT("Wanneer worden de woningen opgeleverd?", "De werken zitten in de afwerkingsfase. De oplevering is gepland in het voorjaar van 2027."),
            TT("Betaal ik btw of registratierechten?", "Bij nieuwbouw betaal je doorgaans btw op het constructiegedeelte en registratierechten op het grondaandeel. Je notaris maakt vooraf een exacte raming."),
            TT("Kan ik de afwerking nog zelf kiezen?", "Ja. Keuken, sanitair, vloeren en binnendeuren kies je bij onze partners, binnen een afwerkingsbudget dat in de prijs zit."),
            TT("Hoe energiezuinig zijn de woningen?", "E-peil E20 of lager, met warmtepomp, vloerverwarming, ventilatie D en voorbereiding voor zonnepanelen."),
            TT("Hoe bereikbaar is Vlekkem?", "Station Erpe-Mere ligt op 2,5 km (lijn Gent–Brussel). Aalst en de E40 bereik je met de auto in een kwartier.")
        })

        Return m
    End Function
End Class
