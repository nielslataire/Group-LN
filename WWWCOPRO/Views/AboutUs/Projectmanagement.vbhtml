@Code
    ' Subpagina van de Over ons-hub. Route: /projectbegeleiding (naam "Projectbegeleiding").
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageMeta
    @Code
        ' "Over ons"-item wijst naar de definitieve /over-ons; tijdens de preview
        ' serveert die URL nog de oude pagina. Na livegang klopt alles.
        Dim _breadcrumbJson As String = "{" & Environment.NewLine &
            "  ""@context"": ""https://schema.org""," & Environment.NewLine &
            "  ""@type"": ""BreadcrumbList""," & Environment.NewLine &
            "  ""itemListElement"": [" & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 1, ""name"": ""Home"", ""item"": ""https://www.groupln.be/"" }," & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 2, ""name"": ""Over ons"", ""item"": ""https://www.groupln.be/over-ons"" }," & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 3, ""name"": ""Projectbegeleiding"", ""item"": ""https://www.groupln.be/projectbegeleiding"" }" & Environment.NewLine &
            "  ]" & Environment.NewLine &
            "}"
    End Code
    <script type="application/ld+json">@Html.Raw(_breadcrumbJson)</script>
End Section

@section PageStyle
    <link rel="stylesheet" href="~/Content/home-sections.css" />
    <link rel="stylesheet" href="~/Content/about.css" />
End Section

<section class="about-hero">
    <div class="container">
        <div class="about-hero-inner">
            <ul class="about-breadcrumb">
                <li><a href="@Url.Action("Index", "Home")">Home</a></li>
                <li><a href="@Url.RouteUrl("OverOnsHub")">Over ons</a></li>
                <li>Projectbegeleiding</li>
            </ul>
            <p class="about-hero-kicker">Projectbegeleiding</p>
            <h1 class="about-hero-title">Uw bouwproject in ervaren handen</h1>
            <p class="about-hero-text">Group LN begeleidt bouwprojecten van bouwheren en ontwikkelaars die de opvolging niet zelf willen of kunnen doen.</p>
        </div>
    </div>
</section>

<section class="about-story">
    <div class="container">
        <div class="about-grid">
            <div class="about-media reveal">
                <img class="about-media-foto" src="@Url.Content("~/Content/img/projectmanagement.webp")" alt="Group LN aan het werk" width="500" height="600" />
            </div>
            <div class="about-content reveal reveal-slide-right">
                <p class="section-kicker">Wat het inhoudt</p>
                <h2 class="about-headline">Van haalbaarheidsstudie tot definitieve oplevering</h2>
                <p class="about-text">Wij verzorgen het projectmanagement van onze eigen promoties en van bouwprojecten van andere bouwheren en promotoren. Het pakket is flexibel: bij voorkeur stappen we vroeg in, maar we verzorgen evengoed enkel de klantenbegeleiding of de opleveringen.</p>
                <p class="about-text">Veel bouwheren ervaren dat de kost voor de projectbegeleiding terugverdiend wordt door een kortere bouwtermijn, betere voorwaarden bij de contractanten en efficiëntere uitvoeringsmethoden.</p>
            </div>
        </div>
    </div>
</section>

<section class="about-fases">
    <div class="container">
        <div class="about-section-head reveal">
            <p class="section-kicker">Hoe we werken</p>
            <h2>Drie fases, één verantwoordelijke</h2>
            <p>Of het nu gaat om een residentieel, commercieel of industrieel project: wij nemen het volledige traject op ons, zodat u één aanspreekpunt heeft van eerste ontwerp tot nazorg.</p>
        </div>
        <div class="about-fase-grid">
            <div class="about-fase reveal">
                <span class="about-fase-num">01</span>
                <h3>Voorbereiding en ontwerp</h3>
                <ul class="about-check-list">
                    <li><strong>Ontwerp</strong>Begeleiding bij het aanstellen van architect, ingenieur, veiligheidscoördinator en EPB-verslaggever, plus een haalbaarheidsstudie en budgetbepaling.</li>
                    <li><strong>Aanspreekpunt</strong>Coördinatie tussen architect, ingenieur, veiligheidscoördinator en aannemers, en contactpersoon tussen opdrachtgever en bouwteam.</li>
                    <li><strong>Aanbesteding</strong>Selectie van aannemers, aanbesteding van de loten, vergelijking van de offertes en de contractonderhandelingen.</li>
                </ul>
            </div>
            <div class="about-fase reveal">
                <span class="about-fase-num">02</span>
                <h3>Uitvoering</h3>
                <ul class="about-check-list">
                    <li><strong>Coördinatie van de bouwloten</strong>Controle op kwaliteit, conformiteit en ritme van de werken, en afstemming tussen de aannemers.</li>
                    <li><strong>Werfopvolging</strong>Aanwezigheid op de wekelijkse werfvergadering en bewaking van de opvolging van de werfverslagen.</li>
                    <li><strong>Planning</strong>Een algemene projectplanning en bewaking van de contractuele termijnen van elke aannemer.</li>
                    <li><strong>Projectrekeningen</strong>Financieel overzicht, controle van vorderingsstaten en facturen, met protest of creditnota's waar nodig.</li>
                </ul>
            </div>
            <div class="about-fase reveal">
                <span class="about-fase-num">03</span>
                <h3>Klant en oplevering</h3>
                <ul class="about-check-list">
                    <li><strong>Klanten- en gebruikersopvolging</strong>Begeleiding van kopers of huurders bij hun keuzes, coördinatie met de showrooms en de afrekening van de klantenkeuzes.</li>
                    <li><strong>Opleveringen</strong>Voorlopige oplevering samen met bouwheer, kopers of huurders, raad van beheer, syndicus en architect, inclusief de nodige documenten.</li>
                    <li><strong>Nazorg</strong>Opvolging van de opmerkingen tot en met de definitieve oplevering.</li>
                </ul>
            </div>
        </div>
    </div>
</section>

<section class="about-eind-cta">
    <div class="container reveal">
        <h2>Een project dat begeleiding kan gebruiken?</h2>
        <p>Vraag vrijblijvend een offerte voor de begeleiding van uw project.</p>
        <div class="about-cta-actions">
            <a class="cta-btn" href="@Url.Action("Index", "Contact", New With {.onderwerp = "Projectbegeleiding"})">Neem contact op <i class="fa fa-arrow-right"></i></a>
        </div>
        <span class="about-cta-mail">of mail rechtstreeks naar <a href="mailto:info@groupln.be">info@groupln.be</a></span>
    </div>
</section>
