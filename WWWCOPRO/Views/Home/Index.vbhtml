@Imports BO
@Code
    ViewData("Title") = "Projectontwikkelaar in Gent en Oost-Vlaanderen | Group LN"
    ViewData("HeroHeader") = True
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim heroOptions = CType(ViewData("HeroSearchOptions"), WWWCOPRO.HeroSearchOptionsModel)
    If heroOptions Is Nothing Then heroOptions = New WWWCOPRO.HeroSearchOptionsModel()
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/home-hero.css" />
    <link rel="stylesheet" href="~/Content/home-sections.css" />
End Section

<section id="homeHero" class="home-hero">
    <video class="home-hero-video" autoplay muted loop playsinline preload="auto" poster=""@Url.Content("~/Content/video/hero-poster.jpg")">
        <source src="@Url.Content("~/Content/video/hero.webm")" type="video/webm">
        <source src="@Url.Content("~/Content/video/hero.mp4")" type="video/mp4">
    </video>
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
</section>
<section class="about-section">
    <div class="container">
        <div class="about-grid">
            <div class="about-content">
                <p class="section-kicker">Wie zijn we</p>
                <h2 class="about-headline">Vijftien jaar ervaring, één aanspreekpunt.</h2>
                <p class="about-text">Group LN is een projectontwikkelaar actief in de residentiële sector, met jarenlange ervaring als werfleider en projectleider bij diverse aannemingsbedrijven. Die kennis is vandaag de garantie voor een kwalitatief afgewerkt project — van eerste plan tot sleuteloverdracht.</p>
                <p class="about-text">We gaan geen enkele uitdaging uit de weg, en werken samen met architecten die uw woonwensen vertalen naar een functioneel en tijdloos geheel.</p>
                <a class="about-btn" href="@Url.Action("Index","AboutUs")">Meer over Group LN <i class="fa fa-arrow-right"></i></a>
            </div>
            <div class="about-media">
                @* Placeholder — foto/video hier nog te vervangen *@
                <div class="about-media-placeholder"></div>
            </div>
        </div>
    </div>
</section>

<section class="troeven-section">
    <div class="container">
        <p class="section-kicker">Onze troeven</p>
        <h2 class="troeven-headline">Wat ons onderscheidt als projectontwikkelaar.</h2>
        <div class="troeven-divider"></div>
        <div class="troeven-grid">
            <div class="troeven-item">
                <span class="troeven-number">01</span>
                <h4>Kennis</h4>
                <p>Jarenlange ervaring van onze zaakvoerders als werfleider en projectleider, vertaald in kwalitatief afgewerkte projecten.</p>
            </div>
            <div class="troeven-item">
                <span class="troeven-number">02</span>
                <h4>Planning</h4>
                <p>Deskundige selectie van aannemers en een strakke coördinatie resulteren in een korte, betrouwbare bouwtermijn.</p>
            </div>
            <div class="troeven-item">
                <span class="troeven-number">03</span>
                <h4>Functioneel &amp; tijdloos</h4>
                <p>Samenwerking met architecten die woonwensen vertalen in een functioneel en tijdloos geheel met aandacht voor energie-efficiënte materialen.</p>
            </div>
            <div class="troeven-item">
                <span class="troeven-number">04</span>
                <h4>Eén aanspreekpunt</h4>
                <p>Wij coördineren tussen u, de architect, ingenieur, EPB-verslaggever, aannemer en veiligheidscoördinator — u heeft één contactpersoon.</p>
            </div>
        </div>
    </div>
</section>

@section LatestNews
    <h4>Recente <strong>berichten</strong></h4>

    <ul class="nav nav-list mb-xl">
        @For Each news In ViewData("LatestNews")
            @<text>
                <li><a title="@news.news.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.news.TitleNL</a></li>
            </text>

        Next

    </ul>
End Section
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
   