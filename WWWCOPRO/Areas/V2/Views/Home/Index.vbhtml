@Code
    ViewData("Title") = "Group LN | Projectontwikkelaar in gans Vlaanderen (v2 preview)"

    Dim heroOptions = CType(ViewData("HeroSearchOptions"), WWWCOPRO.HeroSearchOptionsModel)
    If heroOptions Is Nothing Then heroOptions = New WWWCOPRO.HeroSearchOptionsModel()
    Dim heroFeatured = CType(ViewData("HomeHeroFeatured"), WWWCOPRO.HomeHeroFeaturedModel)

    ' Copy 1-op-1 gekopieerd uit de live Views/Home/Index.vbhtml, inclusief de troeven-array
    ' (geen Number-weergave daar — die laat ik hier dus ook weg) en de dynamische jarenberekening.
    Dim troeven() = {
        New With {.Name = "Vakmanschap", .Description = "Onze zaakvoerders stonden zelf jarenlang als werfleider en projectleider op de werf. Die praktijkkennis van elke bouwfase — ruwbouw, technieken, afwerking — stelt ons in staat om de volledige realisatie op ons te nemen, met een oog voor detail dat je alleen opbouwt na jaren ervaring op de werf zelf."},
        New With {.Name = "Strakke opvolging", .Description = "Wij selecteren zorgvuldig de juiste vakmensen en bewaken de volledige planning — van eerste spadesteek tot oplevering. U hoeft zelf geen aannemers op te volgen of te coördineren: die verantwoordelijkheid dragen wij, tot in het kleinste detail."},
        New With {.Name = "Tijdloos ontwerp", .Description = "Samen met onze architecten vertalen we uw woonwensen naar een functioneel, tijdloos ontwerp. Diezelfde visie bewaken we ook tijdens de uitvoering, tot en met de laatste afwerking en de keuze van energiezuinige materialen — zodat wat op de tekentafel ontstond, exact zo wordt gerealiseerd."},
        New With {.Name = "Eén aanspreekpunt", .Description = "Architect, ingenieur, EPB-verslaggever, aannemer, veiligheidscoördinator: wij regisseren ze allemaal, tot in het kleinste detail. U heeft één aanspreekpunt dat het volledige traject bewaakt — van eerste ontwerp tot oplevering."}
    }
End Code
@Section PageStyle
    <link rel="stylesheet" href="@Url.Content("~/Content/v2/home.css")" />
End Section

<section id="v2Hero" class="v2-hero">
    <video class="v2-hero-media" muted loop playsinline preload="metadata"
           poster="@Url.Content("~/Content/video/hero-poster.jpg")"
           data-poster="@Url.Content("~/Content/video/hero-poster.jpg")"
           data-poster-portrait="@Url.Content("~/Content/video/hero-poster-portrait.jpg")">
        <source src="@Url.Content("~/Content/video/hero-portrait.webm")" type="video/webm" media="(orientation: portrait)">
        <source src="@Url.Content("~/Content/video/hero-portrait.mp4")" type="video/mp4" media="(orientation: portrait)">
        <source src="@Url.Content("~/Content/video/hero.webm")" type="video/webm">
        <source src="@Url.Content("~/Content/video/hero.mp4")" type="video/mp4">
    </video>
    <button type="button" id="v2HeroVideoToggle" class="v2-hero-video-toggle" aria-label="Achtergrondvideo pauzeren" hidden>
        <i class="fa fa-pause" aria-hidden="true"></i>
    </button>
    <div class="v2-hero-scrim"></div>
    <div class="v2-hero-gradient"></div>
    <div class="v2-hero-content">
        <p class="v2-hero-kicker"><span></span>PROJECTONTWIKKELING &amp; PROJECTCOÖRDINATIE<span></span></p>
        <p class="v2-hero-title">Bijzondere plekken, doordacht ontwikkeld.</p>
        <h1 class="v2-hero-text">Group LN is projectontwikkelaar van tijdloze appartementen en woningen op de mooiste locaties in Vlaanderen.</h1>
    </div>
    <div class="v2-hero-actions">
        <a href="@Url.Action("Index", "Projects", New With {.area = ""})" class="v2-btn v2-btn-light">Bekijk ons aanbod te koop <i class="fa fa-arrow-right" aria-hidden="true"></i></a>
        <button type="button" id="v2SearchToggle" class="v2-btn v2-btn-ghost" aria-expanded="false" aria-controls="v2HeroSearch"><i class="fa fa-search" aria-hidden="true"></i> Zoek in het aanbod</button>
    </div>

    @* Zoekbalk-opties komen uit WWWCOPRO.HomeHeroDataHelper.BuildHeroSearchOptions() — zelfde data
       als de live homepage. Submit gaat rechtstreeks naar Projects/Index met de echte
       queryparameters (Gemeente/PriceMin/PriceMax/UnitCategory), zonder de indirecte
       hidden-inputnamen + JS-vertaling van de live pagina. *@
    <div id="v2HeroSearch" class="v2-hero-search">
        <form class="v2-hero-search-form" method="get" action="@Url.Action("Index", "Projects", New With {.area = ""})">
            <div class="v2-hero-search-field" data-field="Gemeente">
                <span class="v2-hero-search-field-label">Regio</span>
                <span class="v2-hero-search-field-value">Alle regio's <i class="fa fa-angle-down" aria-hidden="true"></i></span>
                <ul class="v2-hero-search-dropdown">
                    <li data-value="">Alle regio's</li>
                    @For Each regio In heroOptions.Regios
                        @<li data-value="@regio">@regio</li>
                    Next
                </ul>
                <input type="hidden" name="Gemeente" value="" />
            </div>
            <div class="v2-hero-search-field" data-field="Prijs">
                <span class="v2-hero-search-field-label">Prijs</span>
                <span class="v2-hero-search-field-value">Alle prijzen <i class="fa fa-angle-down" aria-hidden="true"></i></span>
                <ul class="v2-hero-search-dropdown">
                    <li data-min="" data-max="">Alle prijzen</li>
                    @For Each bracket In heroOptions.PriceBrackets
                        @<li data-min="@bracket.MinValue" data-max="@bracket.MaxValue">@bracket.Label</li>
                    Next
                </ul>
                <input type="hidden" name="PriceMin" value="" />
                <input type="hidden" name="PriceMax" value="" />
            </div>
            @If heroOptions.ShowTypeField Then
                @<div class="v2-hero-search-field" data-field="UnitCategory">
                    <span class="v2-hero-search-field-label">Type</span>
                    <span class="v2-hero-search-field-value">Alle types <i class="fa fa-angle-down" aria-hidden="true"></i></span>
                    <ul class="v2-hero-search-dropdown">
                        <li data-value="">Alle types</li>
                        @For Each cat In heroOptions.UnitCategories
                            @<li data-value="@cat.Key">@cat.Label</li>
                        Next
                    </ul>
                    <input type="hidden" name="UnitCategory" value="" />
                </div>
            End If
            <button type="submit" class="v2-hero-search-submit" aria-label="Zoeken"><i class="fa fa-search" aria-hidden="true"></i></button>
        </form>
    </div>

    <a href="#v2About" class="v2-hero-scroll-cue" aria-label="Scroll naar beneden voor meer">
        <span class="v2-hero-scroll-label">Ontdek meer</span>
        <span class="v2-hero-scroll-dot"><i class="fa fa-chevron-down" aria-hidden="true"></i></span>
    </a>
</section>

<section id="v2About" class="v2-about">
    <div class="v2-container">
        <div class="v2-about-grid">
            <div class="v2-about-content v2-reveal">
                <p class="v2-section-kicker">Wie zijn we</p>
                <h2 class="v2-about-headline">@(DateTime.Now.Year - 1999) jaar ervaring, één aanspreekpunt.</h2>
                <p class="v2-about-text">Group LN bestaat 27 jaar als projectontwikkelaar in de residentiële sector.</p>
                <p class="v2-about-text">Onze zaakvoerders brachten bij de oprichting al jarenlange ervaring mee als werfleider en projectleider bij diverse aannemingsbedrijven — ervaring die sindsdien alleen maar is gegroeid.</p>
                <p class="v2-about-text">Die combinatie van bedrijfscontinuïteit en praktijkkennis is vandaag de garantie voor een kwalitatief afgewerkt project, van eerste ontwerp tot oplevering.</p>
                <p class="v2-about-text">Elk project is anders. Daarom werken we nauw samen met architecten om uw woonwensen te vertalen naar een ontwerp dat vandaag functioneel is, en morgen nog steeds klopt.</p>
                <a class="v2-btn v2-btn-outline" href="@Url.Action("Index", "AboutUs", New With {.area = ""})">Meer over Group LN <i class="fa fa-arrow-right" aria-hidden="true"></i></a>
            </div>
            <div class="v2-about-media v2-reveal">
                <img src="@Url.Content("~/Content/img/about.webp")" alt="Group LN" width="1000" height="1200" loading="lazy" decoding="async" />
            </div>
        </div>
    </div>
</section>

<section class="v2-troeven-section">
    <div class="v2-container">
        <p class="v2-section-kicker">Onze troeven</p>
        <h2 class="v2-troeven-headline v2-reveal">Eén partner, van eerste ontwerp tot laatste afwerkingsdetail.</h2>
        <div class="v2-troeven-divider"></div>
        <div class="v2-troeven">
            @For Each t In troeven
                @<div class="v2-troef v2-reveal">
                    <h3 class="v2-troef-name">@t.Name</h3>
                    <p class="v2-troef-desc">@t.Description</p>
                </div>
            Next
        </div>
    </div>
</section>

@If heroFeatured IsNot Nothing Then
    @<section class="v2-featured">
        <div class="v2-featured-media">
            @If heroFeatured.IsVideo Then
                @<video src="@heroFeatured.VideoSrc" muted loop playsinline preload="none"></video>
            Else
                @<img src="@heroFeatured.ImageSrc" alt="@heroFeatured.ProjectTitel" loading="lazy" decoding="async">
            End If
        </div>
        <div class="v2-featured-overlay"></div>
        <div class="v2-featured-inner">
            <div class="v2-container v2-reveal">
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Kicker) Then
                    @<p class="v2-section-kicker v2-featured-kicker">@heroFeatured.Kicker</p>
                End If
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Titel) Then
                    @<h2 class="v2-featured-title">@heroFeatured.Titel</h2>
                End If
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Tekst) Then
                    @<p class="v2-featured-text">@heroFeatured.Tekst</p>
                End If
                <a class="v2-btn v2-btn-outline v2-featured-btn" href="@heroFeatured.DetailUrl">Ontdek @heroFeatured.ProjectTitel <i class="fa fa-arrow-right" aria-hidden="true"></i></a>
            </div>
        </div>
    </section>
End If

<section class="v2-cta-section">
    <div class="v2-container v2-reveal">
        <h2 class="v2-cta-title">Grond of pand met ontwikkelingspotentieel?</h2>
        <p class="v2-cta-text">Of het nu gaat om een perceel, een oude woning of een verouderd pand — wij onderzoeken graag vrijblijvend de mogelijkheden voor een samenwerking of overname, en ontzorgen u doorheen het volledige traject.</p>
        <a href="@Url.Action("Index", "Contact", New With {.area = ""})" class="v2-btn v2-btn-primary">Neem contact op <i class="fa fa-arrow-right" aria-hidden="true"></i></a>
    </div>
</section>
