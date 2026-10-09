@ModelType WWWCOPRO.ProjectPageV4Model
@Imports System.Web.Script.Serialization
@Imports WWWCOPRO
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"

    Dim ser As New JavaScriptSerializer()
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

    Dim priceNote As String = (If(homes.PriceListNote, "") & If(String.IsNullOrWhiteSpace(homes.PriceListUpdated), "", " Bijgewerkt op " & homes.PriceListUpdated & ".")).Trim()
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
        .plan = l.FloorPlanUrl, .hasRender = Not String.IsNullOrWhiteSpace(l.RenderUrl),
        .finishes = l.Finishes.Select(Function(f) New With {.shortLabel = f.ShortLabel, .label = f.Label, .price = f.Price, .desc = f.Description}).ToList()
    }).ToList()
    Dim pageData = New With {
        .project = New With {.name = p.Name, .lat = p.Lat, .lng = p.Lng, .address = p.Street & ", " & p.Postcode & " " & p.Deelgemeente},
        .lots = lotData,
        .calc = New With {.down = defaultDown, .rate = defaultRate, .years = defaultYears},
        .map = New With {
            .cats = p.Location.Categories.Select(Function(c) New With {.id = c.Id, .label = c.Label, .icon = c.Icon, .defaultOn = c.DefaultOn}).ToList(),
            .pois = p.Location.Pois.Select(Function(o) New With {.cat = o.Category, .name = o.Name, .lat = o.Lat, .lng = o.Lng, .dist = o.Distance, .time = o.Time}).ToList(),
            .logo = V4Html.ResolveUrl("~/Content/img/logo.png"),
            .showMap = p.Location.Visible AndAlso p.Location.ShowMap
        },
        .contact = New With {.titleMany = p.Contact.TitleMany, .general = p.Contact.GeneralInfoLabel, .brochureMessage = "", .buyMessage = p.Buy.CtaPrefilledMessage},
        .endpoints = New With {.contact = p.ContactPostUrl, .brochure = p.BrochurePostUrl, .updates = p.UpdatesPostUrl},
        .hasMobileBar = p.ShowMobileBar,
        .barSingle = (available.Count = 1)
    }
    Dim pageJson As String = ser.Serialize(pageData).Replace("<", "<")
End Code

@Section PageMeta
    @Code
        Dim ld As New List(Of Object)
        ld.Add(New Dictionary(Of String, Object) From {
            {"@context", "https://schema.org"}, {"@type", "BreadcrumbList"},
            {"itemListElement", New Object() {
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 1}, {"name", "Home"}, {"item", "https://www.groupln.be/"}},
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 2}, {"name", "Woonprojecten"}, {"item", "https://www.groupln.be/woonprojecten"}},
                New Dictionary(Of String, Object) From {{"@type", "ListItem"}, {"position", 3}, {"name", p.Name}, {"item", p.CanonicalUrl}}
            }}
        })
        ld.Add(New Dictionary(Of String, Object) From {
            {"@context", "https://schema.org"}, {"@type", "Residence"}, {"name", p.Name}, {"url", p.CanonicalUrl},
            {"address", New Dictionary(Of String, Object) From {
                {"@type", "PostalAddress"}, {"streetAddress", p.Street}, {"postalCode", p.Postcode},
                {"addressLocality", p.Deelgemeente}, {"addressRegion", p.Provincie}, {"addressCountry", "BE"}}},
            {"geo", New Dictionary(Of String, Object) From {{"@type", "GeoCoordinates"}, {"latitude", p.Lat}, {"longitude", p.Lng}}}
        })
        For Each lot In available
            ld.Add(New Dictionary(Of String, Object) From {
                {"@context", "https://schema.org"}, {"@type", "Offer"},
                {"name", lot.Name & " — " & lot.Type.ToLowerInvariant() & " woning, " & p.Name & " " & p.Deelgemeente},
                {"price", lot.FromPrice.Value}, {"priceCurrency", "EUR"}, {"availability", "https://schema.org/InStock"},
                {"url", p.CanonicalUrl},
                {"itemOffered", New Dictionary(Of String, Object) From {
                    {"@type", "SingleFamilyResidence"}, {"name", lot.Name},
                    {"floorSize", New Dictionary(Of String, Object) From {{"@type", "QuantitativeValue"}, {"value", lot.LivingArea}, {"unitCode", "MTK"}}},
                    {"numberOfRooms", lot.Bedrooms}}},
                {"seller", New Dictionary(Of String, Object) From {{"@type", "Organization"}, {"name", "Group LN"}, {"telephone", "+32 9 216 49 50"}}}
            })
        Next
        If p.Faq.Visible AndAlso p.Faq.Items.Count > 0 Then
            ld.Add(New Dictionary(Of String, Object) From {
                {"@context", "https://schema.org"}, {"@type", "FAQPage"},
                {"mainEntity", p.Faq.Items.Select(Function(f) New Dictionary(Of String, Object) From {
                    {"@type", "Question"}, {"name", f.Title},
                    {"acceptedAnswer", New Dictionary(Of String, Object) From {{"@type", "Answer"}, {"text", f.Text}}}}).ToList()}
            })
        End If
    End Code
    @For Each item In ld
        @<script type="application/ld+json">@Html.Raw(ser.Serialize(item).Replace("<", "<"))</script>
    Next
End Section

@Section PageStyle
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <link rel="stylesheet" href="@Url.Content("~/Content/project-detail-v4.css")" />
End Section

<div class="pdv4-page">
<div class="pdv4-split">

    @* ═════════ LINKS: sticky beeld ═════════ *@
    <aside class="pdv4-media" aria-label="Beelden van het project">
        <div class="pdv4-media__layers">
            @For i As Integer = 0 To layers.Count - 1
                Dim m = layers(i)
                @<div class="pdv4-layer@(If(i = 0, " is-active", ""))" data-key="@m.Key" data-label="@m.Label">
                    @V4Html.Img(m.ImageUrl, m.Alt, i > 0)
                </div>
            Next
        </div>
        <div class="pdv4-media__shade"></div>
        <div class="pdv4-media__top"><span id="pdv4MediaLabel" class="pdv4-media__label">@(If(layers.Count > 0, layers(0).Label, ""))</span></div>
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
            @<section id="intro" class="pdv4-sec" data-media="@p.Intro.MediaKey" aria-labelledby="pdv4-h1">
                <p class="pdv4-eyebrow">@p.Intro.Eyebrow</p>
                <h1 id="pdv4-h1" class="pdv4-h1">@p.Intro.TitleLead <em>@p.Intro.TitleEmphasis</em></h1>
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
            @<section id="verhaal" class="pdv4-sec pdv4-sec--white" data-media="@p.Story.MediaKey" aria-labelledby="pdv4-verhaal-h">
                <h2 id="pdv4-verhaal-h" class="pdv4-h2">@p.Story.Title</h2>
                <p class="pdv4-lead pdv4-lead--m">@p.Story.Intro</p>
                <div class="pdv4-figures">
                    @For Each fig In p.Story.Figures
                        @<figure class="pdv4-figure">
                            <div class="pdv4-figure__img pdv4-figure__img--32">@V4Html.Img(fig.ImageUrl, fig.Alt)</div>
                            <figcaption>@fig.Text</figcaption>
                        </figure>
                    Next
                </div>
                @If p.Story.ShowBrochureCta Then
                    @<div class="pdv4-cta-green" id="brochure">
                        <p class="pdv4-eyebrow pdv4-eyebrow--gold">@p.Story.BrochureEyebrow</p>
                        <h3 class="pdv4-h3 pdv4-h3--inv">@p.Story.BrochureTitle</h3>
                        <form class="pdv4-inline-form" data-form="brochure" novalidate>
                            <input type="email" name="email" aria-label="E-mailadres" placeholder="jouw@email.be" autocomplete="email" required />
                            <button type="submit" class="pdv4-btn pdv4-btn--gold">@p.Story.BrochureButton</button>
                            <p class="pdv4-form-error" role="alert" hidden></p>
                        </form>
                        <p class="pdv4-inline-done" hidden>@p.Story.BrochureSuccess</p>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "architecture" AndAlso p.Architecture.Visible Then
            @<section id="architectuur" class="pdv4-sec" data-media="@p.Architecture.MediaKey" aria-labelledby="pdv4-arch-h">
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
                                <div class="pdv4-figure__img pdv4-figure__img--45">@V4Html.Img(mt.ImageUrl, mt.Alt)</div>
                                <figcaption>
                                    <h3 class="pdv4-h3 pdv4-h3--sm">@mt.Title</h3>
                                    <p>@mt.Text</p>
                                </figcaption>
                            </figure>
                        Next
                    </div>
                End If
                @If p.Architecture.Techniques.Count > 0 Then
                    @<h3 class="pdv4-h3 pdv4-h3--mt">@p.Architecture.ComfortTitle</h3>
                    @<p class="pdv4-small">@p.Architecture.ComfortIntro</p>
                    @<dl class="pdv4-tech">
                        @For Each t In p.Architecture.Techniques
                            @<div class="pdv4-tech__item"><dt>@t.Title</dt><dd>@t.Text</dd></div>
                        Next
                    </dl>
                End If
                @If p.Architecture.ShowSpecCta Then
                    @<div class="pdv4-cta-row pdv4-cta-row--card">
                        <div class="pdv4-cta-row__text"><strong>@p.Architecture.SpecCtaTitle</strong><span>@p.Architecture.SpecCtaText</span></div>
                        <a href="@p.Architecture.SpecCtaUrl" class="pdv4-btn pdv4-btn--outline">@p.Architecture.SpecCtaLabel</a>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "homes" AndAlso homes.Visible Then
            @<section id="woningen" class="pdv4-sec pdv4-sec--homes" data-media="@homes.MediaKey" aria-labelledby="pdv4-woningen-h">
                <h2 id="pdv4-woningen-h" class="pdv4-h2 pdv4-h2--tight">@homes.Title</h2>
                <p class="pdv4-lead pdv4-lead--s">@homes.Intro</p>

                @If homes.ShowMap Then
                    @<div role="tablist" aria-label="Kies een lot" class="pdv4-lottabs" id="pdv4LotTabs">
                        @For Each lot In p.Lots
                            @<button type="button" role="tab" aria-selected="false" class="pdv4-lottab" data-lot="@lot.Id">
                                <span class="pdv4-dot@(If(lot.IsAvailable, " pdv4-dot--avail", ""))"></span>@lot.Name
                            </button>
                        Next
                    </div>
                    @<div class="pdv4-mapwrap">
                        <div class="pdv4-map" id="pdv4ProjectMap">
                            <div class="pdv4-map__stage" id="pdv4Stage">
                                @V4Html.Img(homes.AerialImageUrl, homes.AerialAlt, False)
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
                            <span class="pdv4-map__hint" id="pdv4MapHint"><span class="pdv4-hint-desktop">Klik op een perceel</span><span class="pdv4-hint-touch">Tik op een perceel</span></span>
                        </div>
                        <div class="pdv4-lotcard" id="pdv4LotCard" role="dialog" hidden></div>
                    </div>
                End If

                @If homes.ShowPriceList Then
                    @<div id="prijslijst" class="pdv4-pricelist">
                        <div class="pdv4-pricelist__head">
                            <div>
                                <h3 class="pdv4-h3 pdv4-h3--lg">Prijslijst</h3>
                                <p class="pdv4-small pdv4-small--tight">@priceNote</p>
                            </div>
                            <div role="radiogroup" aria-label="Filter" class="pdv4-seg" id="pdv4ListFilter">
                                <button type="button" role="radio" aria-checked="false" data-filter="vrij">Beschikbaar</button>
                                <button type="button" role="radio" aria-checked="true" data-filter="alle">Alle woningen</button>
                            </div>
                        </div>

                        <div class="pdv4-table" role="table" aria-label="Prijslijst @p.Name" id="pdv4Table">
                            <div class="pdv4-table__row pdv4-table__row--head" role="row">
                                <span role="columnheader">Lot</span>
                                <span role="columnheader" class="r">Bewoonbaar</span>
                                <span role="columnheader" class="r">Grond</span>
                                <span role="columnheader">Slpk</span>
                                <span role="columnheader" class="r">Prijs</span>
                                <span role="columnheader" class="c">Plan</span>
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
                                        @If lot.IsAvailable AndAlso Not String.IsNullOrWhiteSpace(lot.FloorPlanUrl) Then
                                            @<a href="@lot.FloorPlanUrl" class="pdv4-iconbtn" aria-label="Grondplan @lot.Name downloaden (PDF)" title="Grondplan @lot.Name downloaden (PDF)">
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
                                        @If lot.IsAvailable AndAlso Not multi Then
                                            @<span class="pdv4-card__price">@V4Format.Eur(lot.Finishes.First().Price)</span>
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
                                        If homes.ShowCalculator Then
                                            @<button type="button" class="pdv4-linkbtn pdv4-linkbtn--l" data-tocalc="@lot.Id|0">≈ <span data-monthly="@lot.Id|0">@V4Format.Eur(V4Format.MonthlyPayment(lot.Finishes.First().Price, defaultDown, defaultRate, defaultYears))</span> / maand · bereken</button>
                                        End If
                                        @<div class="pdv4-card__btns">
                                            @If Not String.IsNullOrWhiteSpace(lot.FloorPlanUrl) Then
                                                @<a href="@lot.FloorPlanUrl">⤓ Grondplan</a>
                                            End If
                                            @If Not String.IsNullOrWhiteSpace(homes.SpecDocUrl) Then
                                                @<a href="@homes.SpecDocUrl">⤓ Lastenboek</a>
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
                            <div><dt>Prijsrange</dt><dd>@sumPrice</dd><dd class="s">beschikbare loten</dd></div>
                        </dl>
                    </div>
                End If

                @If homes.ShowCalculator AndAlso calcLot IsNot Nothing Then
                    @<div id="maandlast" class="pdv4-calc">
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
                                <span class="pdv4-doc__dl" aria-hidden="true">⤓</span>
                            </a>
                        Next
                    </div>
                End If
            </section>
        End If

        @If blockKey = "location" AndAlso p.Location.Visible Then
            @<section id="ligging" class="pdv4-sec pdv4-sec--white" data-media="@p.Location.MediaKey" aria-labelledby="pdv4-ligging-h">
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
                            <a href="tel:@p.PhoneHref" class="pdv4-btn pdv4-btn--outline">Bel ons</a>
                            <button type="button" class="pdv4-btn pdv4-btn--primary" data-goto-question>Stel je vraag</button>
                        </div>
                    </div>
                End If
            </section>
        End If

        @If blockKey = "buildphase" AndAlso p.BuildPhase.Visible Then
            @<section id="bouwfase" class="pdv4-sec" data-media="@p.BuildPhase.MediaKey" aria-labelledby="pdv4-bouw-h">
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
            @<section id="kopen" class="pdv4-sec pdv4-sec--white" data-media="@p.Buy.MediaKey" aria-labelledby="pdv4-kopen-h">
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
            @<section id="vragen" class="pdv4-sec" data-media="@p.Faq.MediaKey" aria-labelledby="pdv4-vragen-h">
                <h2 id="pdv4-vragen-h" class="pdv4-h2 pdv4-h2--flat">@p.Faq.Title</h2>
                <div class="pdv4-faq">
                    @For i As Integer = 0 To p.Faq.Items.Count - 1
                        Dim q = p.Faq.Items(i)
                        @<div class="pdv4-faq__item@(If(i = 0, " is-open", ""))">
                            <h3><button type="button" aria-expanded="@(If(i = 0, "true", "false"))" aria-controls="pdv4-faq-a@(i)" id="pdv4-faq-q@(i)"><span>@q.Title</span><span class="pdv4-faq__sign" aria-hidden="true">@(If(i = 0, "−", "+"))</span></button></h3>
                            <p id="pdv4-faq-a@(i)" role="region" aria-labelledby="pdv4-faq-q@(i)" @(If(i = 0, "", "hidden"))>@q.Text</p>
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
            @<section id="contact" class="pdv4-sec pdv4-sec--contact" data-media="@p.Contact.MediaKey" aria-labelledby="pdv4-contact-h">
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
                <div class="pdv4-thanks" id="pdv4Thanks" hidden>
                    <strong>@p.Contact.ThanksTitle</strong>
                    <span>@p.Contact.ThanksText</span>
                </div>
                <p class="pdv4-call">Liever meteen iemand spreken? Bel <a href="tel:@p.PhoneHref">@p.Phone</a></p>
            </section>
        End If
    </div>
</div>

@If p.ShowMobileBar AndAlso available.Count > 0 Then
    @<div class="pdv4-bar" id="pdv4Bar" hidden>
        <span class="pdv4-bar__text"><strong>@p.MobileBarLine</strong><span>@p.Name</span></span>
        <a href="tel:@p.PhoneHref" class="pdv4-bar__call" aria-label="Bel ons">✆</a>
        <a href="#contact" class="pdv4-btn pdv4-btn--primary">Info aanvragen</a>
    </div>
End If
</div>

<script type="application/json" id="pdv4-data">@Html.Raw(pageJson)</script>

@Section scripts
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script src="@Url.Content("~/Scripts/project-detail-v4.js")"></script>
End Section
