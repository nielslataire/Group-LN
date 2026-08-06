@Imports BO
@Code
    ViewData("Title") = "Projectontwikkelaar in Gent en Oost-Vlaanderen | Group LN"
    ViewData("HeroHeader") = True
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim heroOptions = CType(ViewData("HeroSearchOptions"), WWWCOPRO.HeroSearchOptionsModel)
    If heroOptions Is Nothing Then heroOptions = New WWWCOPRO.HeroSearchOptionsModel()
    Dim heroFeatured = CType(ViewData("HomeHeroFeatured"), WWWCOPRO.HomeHeroFeaturedModel)

    ' Troeven: bron voor de zichtbare "Onze troeven"-sectie hieronder.
    Dim troeven() = {
        New With {.Number = "01", .Name = "Vakmanschap", .Description = "Onze zaakvoerders stonden zelf jarenlang als werfleider en projectleider op de werf. Die praktijkkennis van elke bouwfase — ruwbouw, technieken, afwerking — stelt ons in staat om de volledige realisatie op ons te nemen, met een oog voor detail dat je alleen opbouwt na jaren ervaring op de werf zelf."},
        New With {.Number = "02", .Name = "Strakke opvolging", .Description = "Wij selecteren zorgvuldig de juiste vakmensen en bewaken de volledige planning — van eerste spadesteek tot oplevering. U hoeft zelf geen aannemers op te volgen of te coördineren: die verantwoordelijkheid dragen wij, tot in het kleinste detail."},
        New With {.Number = "03", .Name = "Tijdloos ontwerp", .Description = "Samen met onze architecten vertalen we uw woonwensen naar een functioneel, tijdloos ontwerp. Diezelfde visie bewaken we ook tijdens de uitvoering, tot en met de laatste afwerking en de keuze van energiezuinige materialen — zodat wat op de tekentafel ontstond, exact zo wordt gerealiseerd."},
        New With {.Number = "04", .Name = "Eén aanspreekpunt", .Description = "Architect, ingenieur, EPB-verslaggever, aannemer, veiligheidscoördinator: wij regisseren ze allemaal, tot in het kleinste detail. U heeft één aanspreekpunt dat het volledige traject bewaakt — van eerste ontwerp tot oplevering."}
    }
End Code
@Code
    ' JSON-LD additionalProperty opbouwen vanuit dezelfde troeven-array
    ' die ook de zichtbare "Onze troeven"-sectie voedt (single source of truth).
    Dim additionalPropertyJson As New System.Text.StringBuilder()
    For i As Integer = 0 To troeven.Length - 1
        Dim t = troeven(i)
        Dim escapedValue As String = t.Description.Replace("\", "\\").Replace(Chr(34), "\" & Chr(34))
        additionalPropertyJson.Append("{""@@type"":""PropertyValue"",""name"":""" & t.Name & """,""value"":""" & escapedValue & """}")
        If i < troeven.Length - 1 Then additionalPropertyJson.Append(",")
    Next
End Code

<script type="application/ld+json">
{
  "@@context": "https://schema.org",
  "@@type": "Organization",
  "@@id": "https://www.groupln.be/#organization",
  "additionalProperty": [@Html.Raw(additionalPropertyJson.ToString())]
}
</script>
@section PageStyle
    <link rel="stylesheet" href="~/Content/home-hero.css" />
    <link rel="stylesheet" href="~/Content/home-sections.css" />
    <link rel="stylesheet" href="~/Content/home-featured-project.css" />
End Section

<section id="homeHero" class="home-hero">
    <video class="home-hero-video" autoplay muted loop playsinline preload="auto">
        <source src="@Url.Content("~/Content/video/hero-portrait.webm")" type="video/webm" media="(orientation: portrait)">
        <source src="@Url.Content("~/Content/video/hero-portrait.mp4")" type="video/mp4" media="(orientation: portrait)">
        <source src="@Url.Content("~/Content/video/hero.webm")" type="video/webm">
        <source src="@Url.Content("~/Content/video/hero.mp4")" type="video/mp4">
    </video>
    <script>
        (function () {
            var heroVideo = document.currentScript.previousElementSibling;
            if (!heroVideo || heroVideo.tagName !== 'VIDEO') return;
            heroVideo.poster = window.matchMedia('(orientation: portrait)').matches
                ? '@Url.Content("~/Content/video/hero-poster-portrait.jpg")'
                : '@Url.Content("~/Content/video/hero-poster.jpg")';
        })();
    </script>
    <div class="home-hero-overlay"></div>
    <div class="home-hero-content">
        <p class="hero-kicker"><span class="hero-rule"></span>PROJECTONTWIKKELING & PROJECTCOÖRDINATIE<span class="hero-rule"></span></p>
        <p class="hero-headline">Bijzondere plekken, doordacht ontwikkeld.</p>
        <h1 class="hero-subtext">Group LN is projectontwikkelaar van tijdloze appartementen en woningen op de mooiste locaties in Vlaanderen.</h1>
    </div>
    <button type="button" id="heroSearchToggle" class="hero-search-toggle" aria-expanded="false" aria-controls="homeHeroSearch" aria-label="Zoeken">
        <i class="fa fa-search"></i>
    </button>
    <div class="home-hero-search" id="homeHeroSearch">
        <form id="heroSearchForm" method="get" action="@Url.Action("Index", "Projects")">
            <div class="hero-search-field hero-search-term" style="display:none;">
                <label class="hero-search-field-label" for="heroSearchTerm">Zoekterm</label>
                <input type="text" id="heroSearchTerm" name="q" placeholder="Vind een pand, gemeente of project" disabled />
            </div>
            <div class="hero-search-field hero-search-regio hero-dropdown" id="heroRegioDropdown">
                <span class="hero-search-field-label">Regio</span>
                <button type="button" class="hero-dropdown-trigger" aria-haspopup="listbox" aria-expanded="false">
                    <span class="hero-dropdown-value">Alle regio's</span>
                </button>
                <ul class="hero-dropdown-menu" role="listbox">
                    <li class="hero-dropdown-option is-all is-selected" data-value="" role="option" aria-selected="true">Alle regio's</li>
                    @For Each regio In heroOptions.Regios
                        @<li class="hero-dropdown-option" data-value="@regio" role="option" aria-selected="false">@regio</li>
                    Next
                </ul>
                <input type="hidden" id="heroSearchGemeente" value="" />
            </div>
            <div class="hero-search-field hero-search-prijs hero-dropdown" id="heroPrijsDropdown">
                <span class="hero-search-field-label">Prijs</span>
                <button type="button" class="hero-dropdown-trigger" aria-haspopup="listbox" aria-expanded="false">
                    <span class="hero-dropdown-value">Alle prijzen</span>
                </button>
                <ul class="hero-dropdown-menu" role="listbox">
                    <li class="hero-dropdown-option is-all is-selected" data-value="," role="option" aria-selected="true">Alle prijzen</li>
                    @For Each bracket In heroOptions.PriceBrackets
                        @<li class="hero-dropdown-option" data-value="@(bracket.MinValue),@(bracket.MaxValue)" role="option" aria-selected="false">@bracket.Label</li>
                    Next
                </ul>
                <input type="hidden" id="heroSearchPrice" value="" />
            </div>
            @If heroOptions.ShowTypeField Then
                @<div class="hero-search-field hero-search-type hero-dropdown" id="heroTypeDropdown">
                    <span class="hero-search-field-label">Type</span>
                    <button type="button" class="hero-dropdown-trigger" aria-haspopup="listbox" aria-expanded="false">
                        <span class="hero-dropdown-value">Alle types</span>
                    </button>
                    <ul class="hero-dropdown-menu" role="listbox">
                        <li class="hero-dropdown-option is-all is-selected" data-value="" role="option" aria-selected="true">Alle types</li>
                        @For Each cat In heroOptions.UnitCategories
                            @<li class="hero-dropdown-option" data-value="@cat.Key" role="option" aria-selected="false">@cat.Label</li>
                        Next
                    </ul>
                    <input type="hidden" id="heroSearchUnitCategory" value="" />
                </div>
            End If
            <button type="submit" class="hero-search-btn" aria-label="Zoeken"><i class="fa fa-search"></i></button>
        </form>
    </div>
    <div class="home-hero-disclaimer" style="display:none;">
        <p>Door verder te gaan, gaat u akkoord met ons privacybeleid.</p>
    </div>
    <a href="#aboutSection" class="hero-scroll-cue" aria-label="Scroll naar beneden">
        <i class="fa fa-chevron-down"></i>
    </a>
</section>
<section id="aboutSection" class="about-section">
    <div class="container">
        <div class="about-grid">
            <div class="about-content">
                <p class="section-kicker">Wie zijn we</p>
                <h2 class="about-headline">@(DateTime.Now.Year - 1999) jaar ervaring, één aanspreekpunt.</h2>
                <p class="about-text">Group LN bestaat 27 jaar als projectontwikkelaar in de residentiële sector.</p>
                <p class="about-text">Onze zaakvoerders brachten bij de oprichting al jarenlange ervaring mee als werfleider en projectleider bij diverse aannemingsbedrijven — ervaring die sindsdien alleen maar is gegroeid.</p>
                <p class="about-text">Die combinatie van bedrijfscontinuïteit en praktijkkennis is vandaag de garantie voor een kwalitatief afgewerkt project, van eerste ontwerp tot oplevering.</p>
                <p class="about-text">Elk project is anders. Daarom werken we nauw samen met architecten om uw woonwensen te vertalen naar een ontwerp dat vandaag functioneel is, en morgen nog steeds klopt.</p>
                <a class="about-btn" href="@Url.Action("Index","AboutUs")">Meer over Group LN <i class="fa fa-arrow-right"></i></a>
            </div>
            <div class="about-media">
                <img class="about-media-foto" src="@Url.Content("~/Content/img/about.webp")" alt="Group LN" />
            </div>
        </div>
    </div>
</section>

<section class="troeven-section">
    <div class="container">
        <p class="section-kicker">Onze troeven</p>
        <h2 class="troeven-headline">Eén partner, van eerste ontwerp tot laatste afwerkingsdetail.</h2>
        <div class="troeven-divider"></div>
        <div class="troeven-grid">
            @For Each t In troeven
                @<div class="troeven-item">
                    <span class="troeven-number">@t.Number</span>
                    <h3>@t.Name</h3>
                    <p>@t.Description</p>
                </div>
            Next
        </div>
    </div>
</section>

@If heroFeatured IsNot Nothing Then
    @<section class="featured-project-section">
        <div class="featured-project-media">
            @If heroFeatured.IsVideo Then
                @<video src="@heroFeatured.VideoSrc" autoplay muted loop playsinline></video>
            Else
                @<img src="@heroFeatured.ImageSrc" alt="@heroFeatured.ProjectTitel">
            End If
        </div>
        <div class="featured-project-overlay"></div>
        <div class="featured-project-inner">
            <div class="container">
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Kicker) Then
                    @<p class="section-kicker featured-project-kicker">@heroFeatured.Kicker</p>
                End If
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Titel) Then
                    @<h2 class="featured-project-title">@heroFeatured.Titel</h2>
                End If
                @If Not String.IsNullOrWhiteSpace(heroFeatured.Tekst) Then
                    @<p class="featured-project-text">@heroFeatured.Tekst</p>
                End If
                <a class="about-btn featured-project-btn" href="@heroFeatured.DetailUrl">Ontdek @heroFeatured.ProjectTitel <i class="fa fa-arrow-right"></i></a>
            </div>
        </div>
    </section>
End If

<section class="cta-section">
    <div class="container">
        <h2 class="cta-title">Grond of pand met ontwikkelingspotentieel?</h2>
        <p class="cta-text">Of het nu gaat om een perceel, een oude woning of een verouderd pand — wij onderzoeken graag vrijblijvend de mogelijkheden voor een samenwerking of overname, en ontzorgen u doorheen het volledige traject.</p>
        <a class="cta-btn" href="@Url.Action("Index", "Contact")">Neem contact op <i class="fa fa-arrow-right"></i></a>
    </div>
</section>

@section scripts
    <script>
        $(document).ready(function () {
            //alert(this.location.pathname);
            if (this.location.pathname == '/Home/Index') {
                $('a[href="/"]').parent().addClass('active');
            };
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
            $('a.hero-nav-item[href="' + this.location.pathname + '"]').addClass('active');
        });

    </script>
End section
