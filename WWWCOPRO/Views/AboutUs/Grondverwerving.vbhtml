@Code
    ' Subpagina van de Over ons-hub. Route: /grond-of-pand-aanbieden (naam "Grondverwerving").
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
            "    { ""@type"": ""ListItem"", ""position"": 3, ""name"": ""Grond- en pandverwerving"", ""item"": ""https://www.groupln.be/grond-of-pand-aanbieden"" }" & Environment.NewLine &
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
                <li>Grond- en pandverwerving</li>
            </ul>
            <p class="about-hero-kicker">Grond- en pandverwerving</p>
            <h1 class="about-hero-title">Grond of pand met ontwikkelingspotentieel?</h1>
            <p class="about-hero-text">Wij zijn steeds op zoek naar bouwgronden en bestaande panden waar een nieuwbouw- of verbouwproject kan komen.</p>
        </div>
    </div>
</section>

<section class="about-story">
    <div class="container">
        <div class="about-grid">
            <div class="about-media reveal">
                <img class="about-media-foto" src="@Url.Content("~/Content/img/office-2.jpg")" alt="Group LN kantoor" width="500" height="600" />
            </div>
            <div class="about-content reveal reveal-slide-right">
                <p class="section-kicker">Wat we zoeken</p>
                <h2 class="about-headline">Van bouwgrond tot verouderd pand</h2>
                <p class="about-text">Als projectontwikkelaar zoeken we voortdurend nieuwe locaties: een bouwgrond, een oude woning of een verouderd handelspand met potentieel.</p>
                <p class="about-text">Op basis van uw aanbod en gegevens van dienst Stedenbouw, kadaster en notaris vormen we een correct beeld van de mogelijkheden en maken we een voorstudie op.</p>
            </div>
        </div>
    </div>
</section>

<section class="about-pijlers-section">
    <div class="container">
        <div class="about-section-head reveal">
            <p class="section-kicker">Onze aanpak</p>
            <h2>Hoe een samenwerking verloopt</h2>
        </div>
        <div class="about-pijlers">
            <div class="about-pijler reveal">
                <span class="about-pijler-num">01</span>
                <div class="about-pijler-body">
                    <h3>Uw aanbod</h3>
                    <p>U bezorgt ons de gegevens van de grond of het pand. Wij bekijken de ligging, de bestemming en de bestaande vergunningen.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">02</span>
                <div class="about-pijler-body">
                    <h3>Voorstudie</h3>
                    <p>Via dienst Stedenbouw, kadaster en notaris onderzoeken we wat er mogelijk is en maken we een voorstudie met een inschatting van het rendement.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">03</span>
                <div class="about-pijler-body">
                    <h3>Voorstel</h3>
                    <p>Op basis van die studie bieden we een concurrentiële prijs voor de aankoop of de ruil van uw eigendom.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">04</span>
                <div class="about-pijler-body">
                    <h3>Realisatie</h3>
                    <p>Bij akkoord nemen wij het volledige traject op ons, van vergunningsaanvraag tot oplevering.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="about-aanbod">
    <div class="container">
        <div class="about-section-head reveal">
            <h2>Wat u ons kunt aanbieden</h2>
        </div>
        <div class="about-aanbod-grid">
            <div class="about-aanbod-item reveal">
                <h3>Bouwgrond</h3>
                <p>Een perceel of meerdere aangrenzende percelen, met of zonder bestaande vergunning.</p>
            </div>
            <div class="about-aanbod-item reveal">
                <h3>Oude woning</h3>
                <p>Een bestaande woning die in aanmerking komt voor vervangbouw of een grondige renovatie.</p>
            </div>
            <div class="about-aanbod-item reveal">
                <h3>Verouderd pand</h3>
                <p>Een handels- of kantoorpand dat aan een herbestemming toe is.</p>
            </div>
            <div class="about-aanbod-item reveal">
                <h3>Opbrengsteigendom</h3>
                <p>Een eigendom dat u wil ruilen tegen een of meer nieuwbouwentiteiten.</p>
            </div>
        </div>
    </div>
</section>

<section class="about-eind-cta on-white">
    <div class="container reveal">
        <h2>Grond of pand aanbieden?</h2>
        <p>Bezorg ons vrijblijvend de gegevens. Wij onderzoeken de mogelijkheden en komen bij u terug.</p>
        <div class="about-cta-actions">
            <a class="cta-btn" href="@Url.Action("Index", "Contact")">Grond of pand aanbieden <i class="fa fa-arrow-right"></i></a>
        </div>
        <span class="about-cta-mail">of mail rechtstreeks naar <a href="mailto:info@groupln.be">info@groupln.be</a></span>
    </div>
</section>
