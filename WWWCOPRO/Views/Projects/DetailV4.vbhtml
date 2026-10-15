@ModelType WWWCOPRO.ProjectPageV4Model
@Imports System.Web.Script.Serialization
@Imports WWWCOPRO
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"

    Dim ser As New JavaScriptSerializer()
    ' Label rechtsboven op de foto: eerst "straat, gemeente", daarna het blok dat rechts in beeld is
    Dim introLabel As String = String.Join(", ", New String() {If(Model.StreetName, ""), If(Model.Deelgemeente, "")}.Where(Function(x) Not String.IsNullOrWhiteSpace(x)))
    If introLabel = "" Then introLabel = Model.Name
    Dim allProjectsUrl As String = Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional})
    Dim p = Model
    Dim homes = p.Homes
    Dim available = p.AvailableLots
    Dim nl = System.Globalization.CultureInfo.GetCultureInfo("nl-BE")
    Dim defaultDown As Decimal = homes.CalculatorDefaultDownPayment
    Dim defaultRate As Double = homes.CalculatorDefaultRate
    Dim defaultYears As Integer = homes.CalculatorDefaultYears
    Dim rateText As String = defaultRate.ToString("0.00", nl) & " %"

    ' Alle linkerbeelden: de projectbeelden + de render per lot (voor "geselecteerd lot").
    Dim layers As New List(Of V4Media)
    layers.AddRange(p.Media)
    For Each lot In p.Lots
        layers.Add(New V4Media With {.Key = "lot-" & lot.Id, .Label = lot.Name & " — render", .ImageUrl = lot.RenderUrl, .Alt = "Render " & lot.Name})
    Next

    ' Eerste prijs van het eerste beschikbare lot (calculatorstart)
    Dim shapedLots = p.Lots.Where(Function(l) Not String.IsNullOrWhiteSpace(l.Polygon)).ToList()
    Dim calcLot As V4Lot = available.FirstOrDefault()

    Dim listTitle As String = If(homes.ShowPrices, "Prijslijst", "Beschikbaarheid")
    Dim priceNote As String = If(homes.ShowPrices,
        (If(homes.PriceListNote, "") & If(String.IsNullOrWhiteSpace(homes.PriceListUpdated), "", " Bijgewerkt op " & homes.PriceListUpdated & ".")).Trim(),
        "Overzicht van alle woningen en hun beschikbaarheid. Neem contact op voor de prijzen.")
    Dim sumLiving As String = V4Format.Range(p.Lots.Select(Function(l) CDec(l.LivingArea)), " m²")
    Dim sumLand As String = V4Format.Range(p.Lots.Select(Function(l) CDec(l.LandArea)), " m²")
    Dim sumBeds As String = V4Format.Range(p.Lots.Select(Function(l) CDec(l.Bedrooms)))
    Dim sumPrice As String = If(p.MinPriceAnyFinish.HasValue, "vanaf " & V4Format.Eur(p.MinPriceAnyFinish.Value), "—")
    Dim rateAttr As String = V4Format.Inv(defaultRate, "0.00")

    ' Data voor het script (alle logica is client-side, de HTML hieronder is server-side voor SEO)
    Dim lotData = p.Lots.Select(Function(l) New With {
        .id = l.Id, .name = l.Name, .type = l.Type, .typeLabel = l.TypeLabel,
        .living = l.LivingArea, .land = l.LandArea, .bed = l.Bedrooms, .bath = l.Bathrooms,
        .garden = l.GardenOrientation, .status = l.Status, .avail = l.IsAvailable,
        .desc = l.Description, .polygon = l.Polygon, .lx = l.LabelX, .ly = l.LabelY, .zoom = l.Zoom,
        .hasPlan = l.HasPlan, .hasPrice = l.HasPrice, .hasRender = Not String.IsNullOrWhiteSpace(l.RenderUrl),
        .finishes = l.Finishes.Select(Function(f) New With {.shortLabel = f.ShortLabel, .label = f.Label, .price = f.Price, .desc = f.Description}).ToList()
    }).ToList()
    Dim pageData = New With {
        .project = New With {.id = p.ProjectId, .name = p.Name, .lat = p.Lat, .lng = p.Lng, .address = p.Street & ", " & p.Postcode & " " & p.Deelgemeente},
        .lots = lotData,
        .calc = New With {.down = defaultDown, .rate = defaultRate, .years = defaultYears},
        .map = New With {
            .cats = p.Location.Categories.Select(Function(c) New With {.id = c.Id, .label = c.Label, .icon = c.Icon, .defaultOn = c.DefaultOn}).ToList(),
            .pois = p.Location.Pois.Select(Function(o) New With {.cat = o.Category, .name = o.Name, .lat = o.Lat, .lng = o.Lng, .dist = o.Distance, .time = o.Time}).ToList(),
            .logo = V4Html.ResolveUrl("~/Content/img/logo.png"),
            .showMap = p.Location.Visible AndAlso p.Location.ShowMap
        },
        .contact = New With {.titleMany = p.Contact.TitleMany, .fixedTitle = If(p.Contact.CustomTitle, ""), .general = p.Contact.GeneralInfoLabel, .brochureMessage = "", .buyMessage = p.Buy.CtaPrefilledMessage},
        .endpoints = New With {.contact = p.ContactPostUrl, .brochure = p.BrochurePostUrl, .updates = p.UpdatesPostUrl},
        .brochureDocId = p.BrochureDocId,
        .hasMobileBar = p.ShowMobileBar,
        .barSingle = (available.Count = 1)
    }
    Dim pageJson As String = ser.Serialize(pageData).Replace("<", "\u003c")
End Code

@Section PageMeta
    @Code
        Dim ld As New List(Of Object)
        Dim shareImage As String = CStr(If(ViewData("ogimage"), ""))
        Dim seoDescription As String = CStr(If(ViewData("MetaDescription"), ""))
        ' Provincie -> ISO 3166-2:BE voor de geo-metatags
        Dim geoRegions As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"Oost-Vlaanderen", "BE-VOV"}, {"West-Vlaanderen", "BE-VWV"}, {"Antwerpen", "BE-VAN"}, {"Limburg", "BE-VLI"}, {"Vlaams-Brabant", "BE-VBR"},
            {"Waals-Brabant", "BE-WBR"}, {"Henegouwen", "BE-WHT"}, {"Luik", "BE-WLG"}, {"Luxemburg", "BE-WLX"}, {"Namen", "BE-WNA"}, {"Brussel", "BE-BRU"}}
        Dim geoRegion As String = Nothing
        If Not String.IsNullOrWhiteSpace(p.Provincie) Then geoRegions.TryGetValue(p.Provincie.Trim(), geoRegion)
        Dim hasGeo As Boolean = (p.Lat <> 0 AndAlso p.Lng <> 0)
        Dim geoPos As String = If(hasGeo, p.Lat.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) & ";" & p.Lng.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture), "")
        ld.Add(New Dictionary(Of String, Object) From {
            {"@context", "https://schema.org"}, {"@type", "BreadcrumbList"},
            {"itemListElement", New Object() {
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 1}, {"name", "Home"}, {"item", "https://www.groupln.be/"}},
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 2}, {"name", "Woonprojecten"}, {"item", "https://www.groupln.be/woonprojecten"}},
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 3}, {"name", p.Name}, {"item", p.CanonicalUrl}}
            }}
        })
        ' Zelfde RealEstateListing als op de gewone projectpagina (naam, beschrijving, url, beeld, adres), aangevuld met de
        ' coördinaten (mainEntity) en de beschikbare woningen met prijs (offers -> itemOffered).
        Dim address As New Dictionary(Of String, Object) From {
            {"@type", "PostalAddress"}, {"streetAddress", p.Street}, {"postalCode", p.Postcode},
            {"addressLocality", p.Deelgemeente}, {"addressRegion", p.Provincie}, {"addressCountry", "BE"}}
        Dim place As New Dictionary(Of String, Object) From {{"@type", "Residence"}, {"name", p.Name}, {"address", address}}
        If hasGeo Then place("geo") = New Dictionary(Of String, Object) From {{"@type", "GeoCoordinates"}, {"latitude", p.Lat}, {"longitude", p.Lng}}
        Dim offers As New List(Of Object)
        For Each lot In available.Where(Function(x) x.HasPrice)
            Dim item As New Dictionary(Of String, Object) From {{"@type", "SingleFamilyResidence"}, {"name", lot.Name & " — " & lot.Type.ToLowerInvariant() & " woning, " & p.Name & " " & p.Deelgemeente}}
            If lot.LivingArea > 0 Then item("floorSize") = New Dictionary(Of String, Object) From {{"@type", "QuantitativeValue"}, {"value", lot.LivingArea}, {"unitCode", "MTK"}}
            If lot.Bedrooms > 0 Then item("numberOfBedrooms") = lot.Bedrooms
            offers.Add(New Dictionary(Of String, Object) From {
                {"@type", "Offer"}, {"price", lot.FromPrice.Value}, {"priceCurrency", "EUR"}, {"availability", "https://schema.org/InStock"},
                {"url", p.CanonicalUrl}, {"itemOffered", item},
                {"seller", New Dictionary(Of String, Object) From {{"@type", "Organization"}, {"@id", "https://www.groupln.be/#organization"}, {"name", "Group LN"}}}})
        Next
        Dim listing As New Dictionary(Of String, Object) From {
            {"@context", "https://schema.org"}, {"@type", "RealEstateListing"}, {"name", CStr(If(ViewData("ogtitle"), p.Name))},
            {"description", seoDescription}, {"url", p.CanonicalUrl}, {"image", shareImage}, {"address", address}, {"mainEntity", place}}
        If offers.Count > 0 Then listing("offers") = offers
        ld.Add(listing)
        If p.Faq.Visible AndAlso p.Faq.Items.Count > 0 Then
            ld.Add(New Dictionary(Of String, Object) From {
                {"@context", "https://schema.org"}, {"@type", "FAQPage"},
                {"mainEntity", p.Faq.Items.Select(Function(f) New Dictionary(Of String, Object) From {
                    {"@type", "Question"}, {"name", f.Title},
                    {"acceptedAnswer", New Dictionary(Of String, Object) From {{"@type", "Answer"}, {"text", V4Html.PlainText(f.Text)}}}}).ToList()}
            })
        End If
    End Code
    @Code
        ' lege waarden (geen foto/omschrijving) niet in de JSON-LD opnemen
        For Each item In ld
            Dim d = TryCast(item, Dictionary(Of String, Object))
            If d IsNot Nothing Then
                For Each k In d.Where(Function(kv) kv.Value Is Nothing OrElse (TypeOf kv.Value Is String AndAlso String.IsNullOrWhiteSpace(CStr(kv.Value)))).Select(Function(kv) kv.Key).ToList()
                    d.Remove(k)
                Next
            End If
        Next
    End Code
    @* Testfase: de v4-pagina is nog niet bedoeld voor zoekmachines (canonical wijst naar /woonprojecten/{slug}). Verwijderen bij livegang. *@
    <meta name="robots" content="noindex, follow" />
    @If Not String.IsNullOrEmpty(geoRegion) Then
        @<meta name="geo.region" content="@geoRegion" />
    End If
    @If Not String.IsNullOrWhiteSpace(p.Deelgemeente) Then
        @<meta name="geo.placename" content="@(p.Deelgemeente & If(String.IsNullOrWhiteSpace(p.Provincie), "", ", " & p.Provincie))" />
    End If
    @If hasGeo Then
        @<text>
            <meta name="geo.position" content="@geoPos" />
            <meta name="ICBM" content="@geoPos.Replace(";", ", ")" />
        </text>
    End If
    @For Each item In ld
        @<script type="application/ld+json">@Html.Raw(ser.Serialize(item).Replace("<", "\u003c"))</script>
    Next
    @If layers.Count > 0 AndAlso layers(0).Pic IsNot Nothing AndAlso layers(0).Pic.AvailableWidths().Count > 0 Then
        Dim heroPic = layers(0).Pic
        Dim heroSet As String = String.Join(", ", heroPic.AvailableWidths().Select(Function(w) heroPic.VariantUrl(w) & " " & w & "w"))
        @<link rel="preload" as="image" fetchpriority="high" imagesrcset="@heroSet" imagesizes="(max-width: 999px) 100vw, 50vw" />
    End If
End Section

@Section PageStyle
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" media="print" onload="this.media='all'" />
    <noscript><link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" /></noscript>
    <link rel="stylesheet" href="@Url.Content("~/Content/project-detail-v4.css")?v=@(System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/project-detail-v4.css")).Ticks)" />
End Section

<div class="pdv4-page">
<div class="pdv4-split">

    @* ═════════ LINKS: sticky beeld ═════════ *@
    <aside class="pdv4-media" aria-label="Beelden van het project">
        <div class="pdv4-media__layers">
            @For i As Integer = 0 To layers.Count - 1
                Dim m = layers(i)
                @<div class="pdv4-layer@(If(i = 0, " is-active", ""))" data-key="@m.Key" data-label="@m.Label">
                    @V4Html.Img(m.ImageUrl, m.Alt, i > 0, m.Pic, "(max-width: 999px) 100vw, 50vw", i = 0)
                </div>
            Next
        </div>
        <div class="pdv4-media__shade"></div>
        <div class="pdv4-media__top">
            <a href="@allProjectsUrl" class="pdv4-back pdv4-back--photo"><svg width="16" height="12" viewBox="0 0 16 12" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M15 6H1M5.5 1.5 1 6l4.5 4.5"/></svg><span>Alle woonprojecten</span></a>
            <span id="pdv4MediaLabel" class="pdv4-media__label" aria-hidden="true">@introLabel</span>
        </div>
        <div class="pdv4-media__bottom">
            @If available.Count > 0 OrElse Not String.IsNullOrWhiteSpace(p.HeroBadgeOverride) Then
                @<span class="pdv4-badge"><span class="pdv4-badge__dot"></span>@p.HeroBadge</span>
            End If
            <div class="pdv4-media__name">@p.Name</div>
            <div class="pdv4-media__meta">
                <div>
                    <div class="pdv4-media__line">@p.HeroLine</div>
                    <div class="pdv4-media__price">@p.HeroPrice</div>
                </div>
                <a href="#contact" class="pdv4-btn pdv4-btn--white">Informatie aanvragen</a>
            </div>
        </div>
    </aside>

    @* ═════════ RECHTS: scrollverhaal ═════════ *@
    <div class="pdv4-content">

        @If p.Intro.Visible Then
            @<section id="intro" class="pdv4-sec" data-block-label="@introLabel" data-media="@p.Intro.MediaKey" aria-labelledby="pdv4-h1">
                <p class="pdv4-eyebrow">@p.Intro.Eyebrow</p>
                @Code
                    Dim titleMain As String = If(p.Intro.TitleLead, "").Trim().TrimEnd(","c).Trim()
                    Dim titleSub As String = If(p.Intro.TitleEmphasis, "").Trim()
                End Code
                <div class="pdv4-h1 pdv4-titleline">
                    <h1 id="pdv4-h1" class="pdv4-titleline__h1@(If(titleSub <> "", " has-sub", ""))">@V4Html.NoBreakHyphens(titleMain)</h1>@If titleSub <> "" Then
                        @<text> <span class="pdv4-titleline__sub"><em>@V4Html.NoBreakHyphens(titleSub)</em></span></text>
                    End If
                </div>
                @For i As Integer = 0 To p.Intro.Paragraphs.Count - 1
                    @<p class="@(If(i = 0, "pdv4-lead", "pdv4-text"))">@p.Intro.Paragraphs(i)</p>
                Next
                @If p.Intro.Facts.Count > 0 Then
                    @<dl class="pdv4-facts">
                        @For Each f In p.Intro.Facts
                            @<div class="pdv4-facts__item"><dt>@f.Label</dt><dd>@f.Value</dd></div>
                        Next
                    </dl>
                End If
                <div class="pdv4-actions">
                    <a href="#contact" class="pdv4-btn pdv4-btn--primary">@p.Intro.PrimaryCtaLabel</a>
                    @If homes.Visible AndAlso homes.ShowPriceList Then
                        @<a href="#prijslijst" class="pdv4-btn pdv4-btn--outline">@p.Intro.SecondaryCtaLabel</a>
                    End If
                </div>
            </section>
        End If

        @For Each blockKey In p.BlockOrder
        @If blockKey = "story" AndAlso p.Story.Visible Then
            @<section id="verhaal" class="pdv4-sec pdv4-sec--white" data-block-label="Verhaal" data-media="@p.Story.MediaKey" aria-labelledby="pdv4-verhaal-h">
                <h2 id="pdv4-verhaal-h" class="pdv4-h2">@p.Story.Title</h2>
                <div class="pdv4-lead-stack">@V4Html.Paragraphs(p.Story.Intro, "pdv4-lead pdv4-lead--m")</div>
                <div class="pdv4-figures">
                    @For Each fig In p.Story.Figures
                        @<figure class="pdv4-figure">
                            <div class="pdv4-figure__img pdv4-figure__img--32">@V4Html.Img(fig.ImageUrl, fig.Alt, True, fig.Pic, "(max-width: 999px) 100vw, 45vw")</div>
                            <figcaption>@fig.Text</figcaption>
                        </figure>
                    Next
                </div>
                @If p.Story.ShowBrochureCta Then
                    @<div class="pdv4-cta-green" id="brochure">
                        <p class="pdv4-eyebrow pdv4-eyebrow--gold">@p.Story.BrochureEyebrow</p>
                        <h3 class="pdv4-h3 pdv4-h3--inv">@p.Story.BrochureTitle</h3>
                        <div class="pdv4-bro__ask" id="pdv4BroAsk">
                            <p>@p.Story.BrochureText</p>
                            <button type="button" class="pdv4-btn pdv4-btn--gold" data-open-brochure>@p.Story.BrochureButton</button>
                        </div>
                        <div class="pdv4-bro-done" id="pdv4BroDone" role="status" hidden>
                            <span class="pdv4-bro-done__icon" aria-hidden="true"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg></span>
                            <div class="pdv4-bro-done__text">
                                <strong>De brochure is onderweg.</strong>
                                <p>We stuurden ze naar <b data-bro-email></b>. Niets ontvangen binnen enkele minuten? Kijk even bij je reclame of <button type="button" class="pdv4-bro-done__reset" data-bro-reset>gebruik een ander adres</button>.</p>
                            </div>
                        </div>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "architecture" AndAlso p.Architecture.Visible Then
            @<section id="architectuur" class="pdv4-sec" data-block-label="Architectuur" data-media="@p.Architecture.MediaKey" aria-labelledby="pdv4-arch-h">
                <p class="pdv4-eyebrow">@p.Architecture.Eyebrow</p>
                <h2 id="pdv4-arch-h" class="pdv4-h2">@p.Architecture.Title</h2>
                @For Each para In p.Architecture.Paragraphs
                    @<p class="pdv4-text">@para</p>
                Next
                @For Each qt In p.Architecture.Quotes
                    @<blockquote class="pdv4-quote">
                        <p>“@qt.Text”</p>
                        @If Not String.IsNullOrWhiteSpace(qt.Person) Then
                            @<footer>@qt.Person</footer>
                        End If
                    </blockquote>
                Next
                @If p.Architecture.Materials.Count > 0 Then
                    @<div class="pdv4-materials">
                        @For Each mt In p.Architecture.Materials
                            @<figure class="pdv4-material">
                                <div class="pdv4-figure__img pdv4-figure__img--45">@V4Html.Img(mt.ImageUrl, mt.Alt, True, mt.Pic, "(max-width: 639px) 100vw, (max-width: 999px) 33vw, 16vw")</div>
                                <figcaption>
                                    <h3 class="pdv4-h3 pdv4-h3--sm">@mt.Title</h3>
                                    <p>@mt.Text</p>
                                </figcaption>
                            </figure>
                        Next
                    </div>
                End If
            </section>
        End If

        @If blockKey = "comfort" AndAlso p.Comfort.Visible Then
            @<section id="comfort" class="pdv4-sec pdv4-sec--flush" data-block-label="Comfort en energie" data-media="@p.Comfort.MediaKey" aria-label="Comfort en energie">
                @If Not String.IsNullOrWhiteSpace(p.Comfort.Title) Then
                    @<h2 class="pdv4-h2 pdv4-h2--tight">@p.Comfort.Title</h2>
                End If
                @If Not String.IsNullOrWhiteSpace(p.Comfort.Subtitle) Then
                    @<p class="pdv4-lead pdv4-lead--s">@p.Comfort.Subtitle</p>
                End If
                @If p.Comfort.Items.Count > 0 Then
                    @<dl class="pdv4-tech">
                        @For Each t In p.Comfort.Items
                            @<div class="pdv4-tech__item"><dt>@t.Title</dt><dd>@t.Text</dd></div>
                        Next
                    </dl>
                End If
                @If p.Comfort.SpecDocId > 0 Then
                    @<div class="pdv4-cta-row pdv4-cta-row--card">
                        <div class="pdv4-cta-row__text"><strong>@p.Comfort.SpecCtaTitle</strong><span>@p.Comfort.SpecCtaText</span></div>
                        <a href="#" class="pdv4-btn pdv4-btn--outline" data-open-brochure>@p.Comfort.SpecCtaLabel</a>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "homes" AndAlso homes.Visible Then
            @<section id="woningen" class="pdv4-sec pdv4-sec--homes" data-block-label="De woningen" data-media="@homes.MediaKey" aria-labelledby="pdv4-woningen-h">
                <h2 id="pdv4-woningen-h" class="pdv4-h2 pdv4-h2--tight">@homes.Title</h2>
                <p class="pdv4-lead pdv4-lead--s">@homes.Intro</p>

                @If homes.ShowMap Then
                    @<div role="tablist" aria-label="Kies een woning" class="pdv4-lottabs" id="pdv4LotTabs">
                        @For Each lot In p.Lots
                            @<button type="button" role="tab" aria-selected="false" class="pdv4-lottab" data-lot="@lot.Id">
                                <span class="pdv4-dot@(If(lot.IsAvailable, " pdv4-dot--avail", ""))"></span>@lot.Name
                            </button>
                        Next
                    </div>
                    @<div class="pdv4-mapwrap">
                        <div class="pdv4-map" id="pdv4ProjectMap">
                            <div class="pdv4-map__stage" id="pdv4Stage">
                                @V4Html.Img(homes.AerialImageUrl, homes.AerialAlt, True, homes.AerialPic, "(max-width: 999px) 200vw, 100vw")
                                <svg viewBox="0 0 100 100" preserveAspectRatio="none" class="pdv4-map__svg" aria-hidden="false">
                                    <rect x="0" y="0" width="100" height="100" fill="transparent" data-bg></rect>
                                    @For Each lot In shapedLots
                                        @<polygon points="@lot.Polygon" data-lot="@lot.Id" class="pdv4-zone@(If(lot.IsAvailable, " is-avail", ""))" tabindex="0" role="button" aria-label="@lot.Name, @lot.Status.ToLowerInvariant()"></polygon>
                                    Next
                                </svg>
                                @For Each lot In shapedLots
                                    @<span class="pdv4-zonelabel" data-lot="@lot.Id" style="left:@(V4Format.Inv(lot.LabelX))%;top:@(V4Format.Inv(lot.LabelY))%">
                                        <span class="pdv4-zonelabel__tag@(If(lot.IsAvailable, " is-avail", ""))">@lot.Name</span>
                                        <span class="pdv4-zonelabel__sub">@(If(lot.IsAvailable, "Beschikbaar", If(lot.IsSold, "Verkocht", lot.Status)))</span>
                                    </span>
                                Next
                            </div>
                            <span class="pdv4-map__hint" id="pdv4MapHint"><span class="pdv4-hint-desktop">Klik op een eenheid</span><span class="pdv4-hint-touch">Tik op een eenheid</span></span>
                        </div>
                        <div class="pdv4-lotcard" id="pdv4LotCard" role="region" hidden></div>
                    </div>
                End If

                @If homes.ShowPriceList Then
                    @<div id="prijslijst" class="pdv4-pricelist" data-block-label="@listTitle">
                        <div class="pdv4-pricelist__head">
                            <div>
                                <h3 class="pdv4-h3 pdv4-h3--lg">@listTitle</h3>
                                <p class="pdv4-small pdv4-small--tight">@priceNote</p>
                            </div>
                            <div role="radiogroup" aria-label="Filter" class="pdv4-seg" id="pdv4ListFilter">
                                <button type="button" role="radio" aria-checked="false" data-filter="vrij">Beschikbaar</button>
                                <button type="button" role="radio" aria-checked="true" data-filter="alle">Alle woningen</button>
                            </div>
                        </div>

                        <div class="pdv4-table" role="table" aria-label="@listTitle @p.Name" id="pdv4Table">
                            <div class="pdv4-table__row pdv4-table__row--head" role="row">
                                <span role="columnheader">Woning</span>
                                <span role="columnheader" class="r">Bewoonbaar</span>
                                <span role="columnheader" class="r">Grond</span>
                                <span role="columnheader" title="Slaapkamers">Slaapk.</span>
                                <span role="columnheader" class="r">Prijs</span>
                                <span role="columnheader" class="c">Grondplan</span>
                            </div>
                            @For Each lot In p.Lots
                                Dim multi = lot.Finishes.Count > 1
                                @<div class="pdv4-table__row@(If(lot.IsAvailable, "", " is-sold"))" role="row" data-lot="@lot.Id" data-avail="@(If(lot.IsAvailable, "1", "0"))">
                                    <span role="cell" class="pdv4-lotname"><strong>@lot.Name</strong><small>@lot.TypeLabel</small></span>
                                    <span role="cell" class="r num">@lot.LivingArea m²</span>
                                    <span role="cell" class="r num">@V4Format.Num(lot.LandArea) m²</span>
                                    <span role="cell" class="pdv4-bedrooms" aria-label="@lot.Bedrooms slaapkamers">
                                        @For b As Integer = 1 To 4
                                            @<span class="pdv4-bed@(If(b <= lot.Bedrooms, " is-on", ""))"></span>
                                        Next
                                        <span class="pdv4-bedrooms__n">@lot.Bedrooms</span>
                                    </span>
                                    <span role="cell" class="pdv4-pricecell">
                                        @If Not lot.IsAvailable Then
                                            @<span class="pdv4-pill pdv4-pill--sold"><span></span>@(If(lot.IsSold, "Verkocht", If(lot.Status = "Optie", "In optie", "Op aanvraag")))</span>
                                        ElseIf Not lot.HasPrice Then
                                            @<span class="pdv4-pill pdv4-pill--ask"><span></span>Op aanvraag</span>
                                        Else
                                            @For Each f In lot.Finishes
                                                @<span class="pdv4-priceline">
                                                    @If multi Then
                                                        @<span class="pdv4-priceline__label">@f.ShortLabel</span>
                                                    End If
                                                    <span class="pdv4-priceline__v@(If(multi, " is-small", ""))">@V4Format.Eur(f.Price)</span>
                                                </span>
                                            Next
                                            If homes.ShowCalculator Then
                                                @<button type="button" class="pdv4-linkbtn" data-tocalc="@lot.Id|0" title="Bereken je maandlast">≈ <span data-monthly="@lot.Id|0">@V4Format.Eur(V4Format.MonthlyPayment(lot.Finishes.First().Price, defaultDown, defaultRate, defaultYears))</span> / maand</button>
                                            End If
                                        End If
                                    </span>
                                    <span role="cell" class="c">
                                        @If lot.IsAvailable AndAlso lot.HasPlan Then
                                            @<a href="#" data-plan-id="@lot.Id" class="pdv4-iconbtn" aria-label="Grondplan @lot.Name downloaden (PDF)" title="Grondplan @lot.Name downloaden (PDF)">
                                                <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2.5v8M4.5 7 8 10.5 11.5 7M3 13.5h10"></path></svg>
                                            </a>
                                        End If
                                    </span>
                                </div>
                            Next
                        </div>

                        <div class="pdv4-cards" id="pdv4Cards">
                            @For Each lot In p.Lots
                                Dim multi = lot.Finishes.Count > 1
                                @<article class="pdv4-card@(If(lot.IsAvailable, "", " is-sold"))" data-lot="@lot.Id" data-avail="@(If(lot.IsAvailable, "1", "0"))">
                                    <div class="pdv4-card__top">
                                        <h4>@lot.Name</h4>
                                        @If lot.IsAvailable AndAlso lot.HasPrice AndAlso Not multi Then
                                            @<span class="pdv4-card__price">@V4Format.Eur(lot.Finishes.First().Price)</span>
                                        ElseIf lot.IsAvailable AndAlso Not lot.HasPrice Then
                                            @<span class="pdv4-card__price">Op aanvraag</span>
                                        ElseIf Not lot.IsAvailable Then
                                            @<span class="pdv4-card__price is-sold">@(If(lot.IsSold, "Verkocht", If(lot.Status = "Optie", "In optie", "Op aanvraag")))</span>
                                        End If
                                    </div>
                                    <p class="pdv4-card__sub">@lot.Type · @lot.Bedrooms slpk · perceel @V4Format.Num(lot.LandArea) m² · @lot.LivingArea m²</p>
                                    @If lot.IsAvailable AndAlso multi Then
                                        @<div class="pdv4-card__opts">
                                            @For Each f In lot.Finishes
                                                @<div><span>@f.Label</span><strong>@V4Format.Eur(f.Price)</strong></div>
                                            Next
                                        </div>
                                    End If
                                    @If lot.IsAvailable Then
                                        If homes.ShowCalculator AndAlso lot.HasPrice Then
                                            @<button type="button" class="pdv4-linkbtn pdv4-linkbtn--l" data-tocalc="@lot.Id|0">≈ <span data-monthly="@lot.Id|0">@V4Format.Eur(V4Format.MonthlyPayment(lot.Finishes.First().Price, defaultDown, defaultRate, defaultYears))</span> / maand · bereken</button>
                                        End If
                                        @<div class="pdv4-card__btns">
                                            @If lot.HasPlan Then
                                                @<a href="#" data-plan-id="@lot.Id"><svg class="pdv4-ico" width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2v8M4.5 6.5 8 10l3.5-3.5M2.5 13.5h11"/></svg>Grondplan</a>
                                            End If
                                            @If p.Comfort.SpecDocId > 0 Then
                                                @<a href="#" data-open-brochure><svg class="pdv4-ico" width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2v8M4.5 6.5 8 10l3.5-3.5M2.5 13.5h11"/></svg>Projectbrochure</a>
                                            End If
                                        </div>
                                    End If
                                </article>
                            Next
                        </div>

                        <dl class="pdv4-summary">
                            <div><dt>Woningen</dt><dd>@p.Lots.Count</dd><dd class="s">@available.Count beschikbaar</dd></div>
                            <div><dt>Bewoonbaar</dt><dd>@sumLiving</dd><dd class="s">per woning</dd></div>
                            <div><dt>Grond</dt><dd>@sumLand</dd><dd class="s">per perceel</dd></div>
                            <div><dt>Slaapkamers</dt><dd>@sumBeds</dd><dd class="s">per woning</dd></div>
                            @If homes.ShowPrices Then
                                @<div><dt>Prijsrange</dt><dd>@sumPrice</dd><dd class="s">beschikbare loten</dd></div>
                            End If
                        </dl>
                    </div>
                End If

                @If homes.ShowCalculator AndAlso calcLot IsNot Nothing Then
                    @<div id="maandlast" class="pdv4-calc" data-block-label="Maandlast">
                        <div class="pdv4-calc__top">
                            <div>
                                <h3 class="pdv4-h3 pdv4-h3--lg">@homes.CalculatorTitle</h3>
                                <p class="pdv4-small pdv4-small--tight">@homes.CalculatorIntro</p>
                            </div>
                            <div role="radiogroup" aria-label="Kies een woning" class="pdv4-calc__chips" id="pdv4CalcChips">
                                @For Each lot In available
                                    Dim multi = lot.Finishes.Count > 1
                                    For fi As Integer = 0 To lot.Finishes.Count - 1
                                        Dim f = lot.Finishes(fi)
                                        @<button type="button" role="radio" aria-checked="false" class="pdv4-chip" data-opt="@lot.Id|@fi">
                                            <span>@lot.Name@(If(multi, " · " & f.ShortLabel.ToLowerInvariant(), ""))</span>
                                            <small>@V4Format.Eur(f.Price)</small>
                                        </button>
                                    Next
                                Next
                            </div>
                            <div class="pdv4-calc__sliders">
                                <label><span>Eigen inbreng<strong id="pdv4DownTxt">@V4Format.Eur(defaultDown)</strong></span><input type="range" id="pdv4Down" min="0" max="400000" step="5000" value="@CInt(defaultDown)" /></label>
                                <label><span>Rentevoet<strong id="pdv4RateTxt">@rateText</strong></span><input type="range" id="pdv4Rate" min="1.5" max="5.5" step="0.05" value="@rateAttr" /></label>
                            </div>
                            <div>
                                <span class="pdv4-small">Looptijd</span>
                                <div class="pdv4-years" id="pdv4Years">
                                    @For Each y In New Integer() {15, 20, 25, 30}
                                        @<button type="button" data-years="@y" aria-pressed="@(If(y = defaultYears, "true", "false"))">@y j</button>
                                    Next
                                </div>
                            </div>
                        </div>
                        <div class="pdv4-calc__result">
                            <div class="pdv4-calc__resulttop">
                                <div>
                                    <div class="pdv4-calc__label">Geschatte maandlast · <span id="pdv4CalcTitle"></span></div>
                                    <div class="pdv4-calc__amount" id="pdv4CalcAmount">&nbsp;</div>
                                </div>
                                <div class="pdv4-calc__loan">Lening <span id="pdv4CalcLoan"></span><br /><span id="pdv4CalcYears"></span> jaar aan <span id="pdv4CalcRate"></span></div>
                            </div>
                            <div class="pdv4-calc__cta">
                                <button type="button" class="pdv4-btn pdv4-btn--gold" id="pdv4CalcCta">Vraag een persoonlijke simulatie</button>
                                <span>Vrijblijvend · antwoord binnen één werkdag</span>
                            </div>
                        </div>
                        <p class="pdv4-calc__disclaimer">@homes.CalculatorDisclaimer</p>
                    </div>
                End If

                @If homes.ShowDocuments AndAlso homes.Documents.Count > 0 Then
                    @<h3 class="pdv4-h3 pdv4-h3--docs">Documenten</h3>
                    @<div class="pdv4-docs">
                        @For Each d In homes.Documents
                            @<a href="@d.Url" class="pdv4-doc">
                                <span class="pdv4-doc__icon">PDF</span>
                                <span class="pdv4-doc__text"><strong>@d.Title</strong><small>@d.Meta</small></span>
                                <span class="pdv4-doc__dl" aria-hidden="true"><svg class="pdv4-ico" width="18" height="18" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2v8M4.5 6.5 8 10l3.5-3.5M2.5 13.5h11"/></svg></span>
                            </a>
                        Next
                    </div>
                End If
            </section>
        End If

        @If blockKey = "documents" AndAlso p.Documents.Visible Then
            @<section id="documenten" class="pdv4-sec pdv4-sec--flush" data-block-label="Documenten" data-media="@p.Documents.MediaKey" aria-label="Documenten">
                <h2 class="pdv4-h2 pdv4-h2--tight">@p.Documents.Title</h2>
                <div class="pdv4-docs">
                    @For Each d In p.Documents.Docs
                        @<a href="#" class="pdv4-doc" data-doc-id="@d.Id" data-doc-name="@d.Title"@(If(d.IsBrochure, " data-open-brochure", ""))>
                            <span class="pdv4-doc__icon">PDF</span>
                            <span class="pdv4-doc__text"><strong>@d.Title</strong><small>@d.Meta</small></span>
                            <span class="pdv4-doc__dl" aria-hidden="true"><svg class="pdv4-ico" width="18" height="18" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2v8M4.5 6.5 8 10l3.5-3.5M2.5 13.5h11"/></svg></span>
                        </a>
                    Next
                </div>
            </section>
        End If

        @If blockKey = "location" AndAlso p.Location.Visible Then
            @<section id="ligging" class="pdv4-sec pdv4-sec--white" data-block-label="Ligging" data-media="@p.Location.MediaKey" aria-labelledby="pdv4-ligging-h">
                <h2 id="pdv4-ligging-h" class="pdv4-h2 pdv4-h2--tight">@p.Location.Title</h2>
                <p class="pdv4-lead pdv4-lead--s">@p.Location.Intro</p>
                @If p.Location.Guide.Count > 0 Then
                    @<div class="pdv4-guide">
                        @For Each g In p.Location.Guide
                            @<div><h3 class="pdv4-h3 pdv4-h3--md">@g.Title</h3><p>@g.Text</p></div>
                        Next
                    </div>
                End If
                @If p.Location.ShowMap Then
                    @<div class="pdv4-chips" id="pdv4LayerChips">
                        @For Each c In p.Location.Categories
                            @<button type="button" class="pdv4-togglechip" data-cat="@c.Id" aria-pressed="@(If(c.DefaultOn, "true", "false"))">@c.Label</button>
                        Next
                    </div>
                    @<div class="pdv4-leaflet"><div id="pdv4Leaflet" role="application" aria-label="Kaart met voorzieningen in de buurt"></div></div>
                End If
                @If p.Location.TravelTimes.Count > 0 Then
                    @<div class="pdv4-travel">
                        @For Each t In p.Location.TravelTimes
                            @<div><span>@t.Destination</span><strong>@t.Duration</strong></div>
                        Next
                    </div>
                End If
                @If p.Location.ShowCta Then
                    @<div class="pdv4-cta-row pdv4-cta-row--soft">
                        <div class="pdv4-cta-row__text"><strong>@p.Location.CtaTitle</strong><span>@p.Location.CtaText</span></div>
                        <div class="pdv4-actions pdv4-actions--tight">
                            <a href="tel:@p.PhoneHref" class="pdv4-btn pdv4-btn--outline pdv4-only-mobile">Bel ons</a>
                            <button type="button" class="pdv4-btn pdv4-btn--primary" data-goto-question>Stel je vraag</button>
                        </div>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "buildphase" AndAlso p.BuildPhase.Visible Then
            @<section id="bouwfase" class="pdv4-sec" data-block-label="Bouwfase" data-media="@p.BuildPhase.MediaKey" aria-labelledby="pdv4-bouw-h">
                <div class="pdv4-phase__head">
                    <h2 id="pdv4-bouw-h" class="pdv4-h2 pdv4-h2--flat">@p.BuildPhase.Title</h2>
                    <span class="pdv4-phase__pct">@p.BuildPhase.ProgressPercent%</span>
                </div>
                <div class="pdv4-progress" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="@p.BuildPhase.ProgressPercent" aria-label="Bouwvoortgang"><div style="width:@(p.BuildPhase.ProgressPercent)%"></div></div>
                <ul class="pdv4-timeline">
                    @For Each mile In p.BuildPhase.Milestones
                        @<li class="is-@mile.Status">
                            <span>@(If(mile.Status = "done", "✓ ", If(mile.Status = "now", "● ", "○ ")))@mile.Title</span>
                            <span class="pdv4-timeline__date">@mile.DateText</span>
                        </li>
                    Next
                </ul>
                @If p.BuildPhase.ShowUpdatesCta Then
                    @<div class="pdv4-cta-card">
                        <div><strong>@p.BuildPhase.UpdatesTitle</strong><span>@p.BuildPhase.UpdatesText</span></div>
                        <form class="pdv4-inline-form pdv4-inline-form--light" data-form="updates" novalidate>
                            <input type="email" name="email" aria-label="E-mailadres" placeholder="jouw@email.be" autocomplete="email" required />
                            <button type="submit" class="pdv4-btn pdv4-btn--primary">@p.BuildPhase.UpdatesButton</button>
                            <p class="pdv4-form-error" role="alert" hidden></p>
                        </form>
                        <p class="pdv4-inline-done pdv4-inline-done--green" hidden>@p.BuildPhase.UpdatesSuccess</p>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "buy" AndAlso p.Buy.Visible Then
            @<section id="kopen" class="pdv4-sec pdv4-sec--white" data-block-label="Aankoop" data-media="@p.Buy.MediaKey" aria-labelledby="pdv4-kopen-h">
                <h2 id="pdv4-kopen-h" class="pdv4-h2 pdv4-h2--flat">@p.Buy.Title</h2>
                <ol class="pdv4-steps">
                    @For Each s In p.Buy.Steps
                        @<li><span class="pdv4-steps__dot"></span><div><strong>@s.Title</strong><span> — @s.Text</span></div></li>
                    Next
                </ol>
                <div class="pdv4-cta-line">
                    <p>@p.Buy.CtaText</p>
                    <button type="button" class="pdv4-btn pdv4-btn--primary" data-goto-meeting>@p.Buy.CtaLabel</button>
                </div>
            </section>
        End If

        @If blockKey = "faq" AndAlso p.Faq.Visible AndAlso p.Faq.Items.Count > 0 Then
            @<section id="vragen" class="pdv4-sec" data-block-label="Vragen" data-media="@p.Faq.MediaKey" aria-labelledby="pdv4-vragen-h">
                <h2 id="pdv4-vragen-h" class="pdv4-h2 pdv4-h2--flat">@p.Faq.Title</h2>
                <div class="pdv4-faq">
                    @For i As Integer = 0 To p.Faq.Items.Count - 1
                        Dim q = p.Faq.Items(i)
                        @<div class="pdv4-faq__item@(If(i = 0, " is-open", ""))">
                            <h3><button type="button" aria-expanded="@(If(i = 0, "true", "false"))" aria-controls="pdv4-faq-a@(i)" id="pdv4-faq-q@(i)"><span>@q.Title</span><span class="pdv4-faq__sign" aria-hidden="true">@(If(i = 0, "−", "+"))</span></button></h3>
                            <div id="pdv4-faq-a@(i)" class="pdv4-faq__answer" role="region" aria-labelledby="pdv4-faq-q@(i)" @(If(i = 0, "", "hidden"))>@V4Html.SafeHtml(q.Text)</div>
                        </div>
                    Next
                </div>
                <div class="pdv4-cta-line pdv4-cta-line--plain">
                    <p>@p.Faq.CtaText</p>
                    <button type="button" class="pdv4-btn pdv4-btn--outline" data-goto-question>@p.Faq.CtaLabel</button>
                </div>
            </section>
        End If

        Next

        @If p.Contact.Visible Then
            @<section id="contact" class="pdv4-sec pdv4-sec--contact" data-block-label="Contact" data-media="@p.Contact.MediaKey" aria-labelledby="pdv4-contact-h">
                <h2 id="pdv4-contact-h" class="pdv4-h2">@p.ContactTitleDefault</h2>
                <p class="pdv4-lead">@p.Contact.Intro</p>
                <form class="pdv4-form" id="pdv4ContactForm" novalidate>
                    <fieldset>
                        <legend>Ik heb interesse in</legend>
                        <div class="pdv4-form__chips" id="pdv4Interest">
                            @For Each lot In available
                                @<button type="button" class="pdv4-togglechip" data-interest="@lot.Name" aria-pressed="false">@lot.Name</button>
                            Next
                            <button type="button" class="pdv4-togglechip" data-interest="@p.Contact.GeneralInfoLabel" aria-pressed="false">@p.Contact.GeneralInfoLabel</button>
                        </div>
                    </fieldset>
                    <div class="pdv4-form__grid">
                        <label>Voornaam<input name="firstName" autocomplete="given-name" required /><em class="pdv4-field-error" hidden></em></label>
                        <label>Naam<input name="lastName" autocomplete="family-name" required /><em class="pdv4-field-error" hidden></em></label>
                        <label>E-mail<input name="email" type="email" autocomplete="email" placeholder="naam@voorbeeld.be" required /><em class="pdv4-field-error" hidden></em></label>
                        <label>Gsm<input name="phone" type="tel" autocomplete="tel" placeholder="+32 4…" required /><em class="pdv4-field-error" hidden></em></label>
                    </div>
                    <label><span>Bericht <span class="pdv4-opt">(optioneel)</span></span><textarea name="message" rows="3" id="pdv4Message"></textarea></label>
                    <label class="pdv4-check"><input type="checkbox" name="consent" required />@p.Contact.ConsentText</label>
                    <p class="pdv4-field-error pdv4-field-error--block" id="pdv4ConsentError" hidden></p>
                    <p class="pdv4-form-error" role="alert" id="pdv4FormError" hidden></p>
                    <button type="submit" class="pdv4-btn pdv4-btn--primary pdv4-btn--xl">@p.Contact.SubmitLabel</button>
                </form>
                @Code
                    Dim thanksStreet As Boolean = V4Html.IsStreetName(p.Name)
                    Dim thanksPlace As String = If(thanksStreet, "aan de " & p.Name, "bij " & p.Name)
                    Dim thanksLive As String = If(thanksStreet, "wonen aan de " & p.Name, p.Name)
                End Code
                <div class="pdv4-thanks" id="pdv4Thanks" role="status" tabindex="-1" hidden>
                    <div class="pdv4-thanks__main">
                        <span class="pdv4-thanks__check" aria-hidden="true"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12.5 10 17.5 19 7.5"/></svg></span>
                        <p class="pdv4-thanks__eyebrow">Aanvraag ontvangen</p>
                        <h3 class="pdv4-thanks__title">Je eerste stap naar een thuis @thanksPlace.</h3>
                        <p class="pdv4-thanks__text">Bedankt voor je interesse. Een van onze projectadviseurs bezorgt je binnen één werkdag de informatie die voor jou relevant is en beantwoordt je vragen. Zo kan je rustig nagaan of @thanksLive bij jou past.</p>
                    </div>
                    <div class="pdv4-thanks__next">
                        <p class="pdv4-thanks__eyebrow">Wat er nu gebeurt</p>
                        <ul class="pdv4-thanks__steps">
                            <li class="pdv4-thanks__step pdv4-thanks__step--1"><strong>We mailen je de informatie</strong><span>Je ontvangt de documenten die bij je aanvraag horen, samen met een antwoord op je vragen.</span></li>
                            <li class="pdv4-thanks__step pdv4-thanks__step--2"><strong>Rustig doornemen</strong><span>Bekijk alles op je eigen tempo. Heb je nog vragen, dan staat je projectadviseur klaar.</span></li>
                            <li class="pdv4-thanks__step pdv4-thanks__step--3"><strong>Een gesprek, als je dat wil</strong><span>Wil je verder, dan plannen we een vrijblijvend gesprek op kantoor of ter plaatse.</span></li>
                        </ul>
                        <a href="#intro" class="pdv4-thanks__more">Lees verder over het project <span aria-hidden="true">&rarr;</span></a>
                    </div>
                </div>
                <p class="pdv4-call">Liever meteen iemand spreken? Bel <a href="tel:@p.PhoneHref">@p.Phone</a></p>
            </section>
        End If
    </div>
</div>

<section class="pdv4-more" aria-labelledby="pdv4-more-h">
    <div class="container">
    <div class="pdv4-more__inner">
        <div class="pdv4-more__text">
            <p class="pdv4-more__eyebrow">Nog niet gevonden wat je zoekt?</p>
            <h2 id="pdv4-more-h" class="pdv4-more__title">Ontdek onze andere woonprojecten</h2>
        </div>
        <a href="@allProjectsUrl" class="pdv4-btn pdv4-btn--outline">Alle woonprojecten <span aria-hidden="true">&rarr;</span></a>
    </div>
    </div>
</section>

@If p.ShowMobileBar AndAlso available.Count > 0 Then
    @<div class="pdv4-bar" id="pdv4Bar" hidden>
        <span class="pdv4-bar__text"><strong>@p.MobileBarLine</strong><span>@p.Name</span></span>
        <a href="tel:@p.PhoneHref" class="pdv4-bar__call" aria-label="Bel ons">✆</a>
        <a href="#contact" class="pdv4-btn pdv4-btn--primary">Info aanvragen</a>
    </div>
End If

<div id="pdv4DocModal" class="pdv4-modal" hidden>
    <div class="pdv4-modal__backdrop" data-doc-close></div>
    <div class="pdv4-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="pdv4DocTitle">
        <button type="button" class="pdv4-modal__close" data-doc-close aria-label="Sluiten">&times;</button>
        <p class="pdv4-eyebrow" data-doc-eyebrow>Document @p.Name</p>
        <h2 id="pdv4DocTitle" class="pdv4-modal__title" data-doc-title>Ontvang het document in je mailbox</h2>
        <p class="pdv4-modal__intro">Vul je gegevens in en we sturen je <span data-doc-name-intro>het document</span> meteen per e-mail.</p>
        <form id="pdv4DocForm" class="pdv4-modal__form" novalidate>
            <input type="hidden" name="DocId" value="" />
            <input type="hidden" name="g-recaptcha-response" id="pdv4DocToken" />
            <div class="pdv4-hp" aria-hidden="true"><label>Laat dit veld leeg<input type="text" name="website_url" tabindex="-1" autocomplete="off" /></label></div>
            <div class="pdv4-modal__grid">
                <label><span class="pdv4-lbl">Voornaam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Firstname" autocomplete="given-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Naam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Name" autocomplete="family-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">E-mail <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Email" type="email" inputmode="email" autocomplete="email" placeholder="naam@voorbeeld.be" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Telefoon <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Phone" type="tel" inputmode="tel" autocomplete="tel" placeholder="+32 4…" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
            </div>
            <p class="pdv4-modal__note"><span class="pdv4-req" aria-hidden="true">*</span> Verplichte velden</p>
            <p class="pdv4-modal__privacy">Group LN gebruikt je gegevens enkel om je het document te bezorgen en je over dit project te informeren. Lees meer in ons <a href="@Url.Action("Privacybeleid", "Legal")" target="_blank" rel="noopener">privacybeleid</a>.</p>
            <p class="pdv4-form-error" role="alert" id="pdv4DocError" hidden></p>
            <button type="submit" class="pdv4-btn pdv4-btn--primary pdv4-btn--xl pdv4-modal__submit" id="pdv4DocSubmit">Stuur mij het document</button>
        </form>
        <div class="pdv4-modal__done" role="status" hidden>
            <strong>Het document is onderweg.</strong>
            <p>We stuurden het naar <b data-done-email></b>. Niets ontvangen binnen enkele minuten? Kijk even bij je reclame.</p>
            <button type="button" class="pdv4-btn pdv4-btn--primary" data-doc-close>Sluiten</button>
        </div>
    </div>
</div>

<div id="pdv4PlanModal" class="pdv4-modal" hidden>
    <div class="pdv4-modal__backdrop" data-plan-close></div>
    <div class="pdv4-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="pdv4PlanTitle">
        <button type="button" class="pdv4-modal__close" data-plan-close aria-label="Sluiten">&times;</button>
        <p class="pdv4-eyebrow" data-plan-eyebrow>Grondplan @p.Name</p>
        <h2 id="pdv4PlanTitle" class="pdv4-modal__title">Ontvang het grondplan in je mailbox</h2>
        <p class="pdv4-modal__intro">Vul je gegevens in en we sturen je het grondplan van <span data-plan-lot>deze woning</span> meteen per e-mail.</p>
        <form id="pdv4PlanForm" class="pdv4-modal__form" novalidate>
            <input type="hidden" name="UnitId" value="" />
            <input type="hidden" name="g-recaptcha-response" id="pdv4PlanToken" />
            <div class="pdv4-hp" aria-hidden="true"><label>Laat dit veld leeg<input type="text" name="website_url" tabindex="-1" autocomplete="off" /></label></div>
            <div class="pdv4-modal__grid">
                <label><span class="pdv4-lbl">Voornaam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Firstname" autocomplete="given-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Naam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Name" autocomplete="family-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">E-mail <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Email" type="email" inputmode="email" autocomplete="email" placeholder="naam@voorbeeld.be" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Telefoon <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Phone" type="tel" inputmode="tel" autocomplete="tel" placeholder="+32 4…" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
            </div>
            <p class="pdv4-modal__note"><span class="pdv4-req" aria-hidden="true">*</span> Verplichte velden</p>
            <p class="pdv4-modal__privacy">Group LN gebruikt je gegevens enkel om je het grondplan te bezorgen en je over dit project te informeren. Lees meer in ons <a href="@Url.Action("Privacybeleid", "Legal")" target="_blank" rel="noopener">privacybeleid</a>.</p>
            <p class="pdv4-form-error" role="alert" id="pdv4PlanError" hidden></p>
            <button type="submit" class="pdv4-btn pdv4-btn--primary pdv4-btn--xl pdv4-modal__submit" id="pdv4PlanSubmit">Stuur mij het grondplan</button>
        </form>
        <div class="pdv4-modal__done" role="status" hidden>
            <strong>Het grondplan is onderweg.</strong>
            <p>We stuurden het naar <b data-done-email></b>. Niets ontvangen binnen enkele minuten? Kijk even bij je reclame.</p>
            <button type="button" class="pdv4-btn pdv4-btn--primary" data-plan-close>Sluiten</button>
        </div>
    </div>
</div>

<div id="pdv4BroModal" class="pdv4-modal" hidden>
    <div class="pdv4-modal__backdrop" data-bro-close></div>
    <div class="pdv4-modal__dialog" role="dialog" aria-modal="true" aria-labelledby="pdv4BroTitle">
        <button type="button" class="pdv4-modal__close" data-bro-close aria-label="Sluiten">&times;</button>
        <p class="pdv4-eyebrow">Brochure @p.Name</p>
        <h2 id="pdv4BroTitle" class="pdv4-modal__title">Ontvang de brochure in je mailbox</h2>
        <p class="pdv4-modal__intro">Vul je gegevens in en we sturen je de brochure meteen per e-mail, met alle materialen, afwerkingen en de volledige beschrijving van het project.</p>
        <form id="pdv4BroForm" class="pdv4-modal__form" novalidate>
            <input type="hidden" name="DocId" value="@p.BrochureDocId" />
            <input type="hidden" name="g-recaptcha-response" id="pdv4BroToken" />
            <div class="pdv4-hp" aria-hidden="true"><label>Laat dit veld leeg<input type="text" name="website_url" tabindex="-1" autocomplete="off" /></label></div>
            <div class="pdv4-modal__grid">
                <label><span class="pdv4-lbl">Voornaam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Firstname" autocomplete="given-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Naam <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Name" autocomplete="family-name" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">E-mail <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Email" type="email" inputmode="email" autocomplete="email" placeholder="naam@voorbeeld.be" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
                <label><span class="pdv4-lbl">Telefoon <span class="pdv4-req" aria-hidden="true">*</span></span><input name="Phone" type="tel" inputmode="tel" autocomplete="tel" placeholder="+32 4…" required aria-required="true" /><em class="pdv4-field-error" hidden></em></label>
            </div>
            <p class="pdv4-modal__note"><span class="pdv4-req" aria-hidden="true">*</span> Verplichte velden</p>
            <p class="pdv4-modal__privacy">Group LN gebruikt je gegevens enkel om je de brochure te bezorgen en je over dit project te informeren. Lees meer in ons <a href="@Url.Action("Privacybeleid", "Legal")" target="_blank" rel="noopener">privacybeleid</a>.</p>
            <p class="pdv4-form-error" role="alert" id="pdv4BroError" hidden></p>
            <button type="submit" class="pdv4-btn pdv4-btn--primary pdv4-btn--xl pdv4-modal__submit" id="pdv4BroSubmit">Stuur mij de brochure</button>
        </form>
    </div>
</div>
</div>


<script type="application/json" id="pdv4-data">@Html.Raw(pageJson)</script>

@Section scripts
    <script>
        // Site-wide beschikbaar voor de modal-partials (SendPlan e.d.), die via AJAX in de pagina komen.
        window.reCaptchaSiteKey = '@ViewBag.ReCaptchaSiteKey';
    </script>
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script src="@Url.Content("~/Scripts/project-detail-v4.js")?v=@(System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Scripts/project-detail-v4.js")).Ticks)"></script>
End Section
