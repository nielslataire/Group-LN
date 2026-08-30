@Code
    ' Nieuwe "Over ons" hoofdpagina (hub). Draait voorlopig op /over-ons-nieuw zodat de
    ' bestaande /over-ons (AboutUs/Index) ongewijzigd blijft. Bij livegang:
    '  1. RouteTranslations.vb: "over-ons" naar deze actie (Overzicht) verleggen
    '  2. AboutUs/Index + Views/AboutUs/Index.vbhtml verwijderen
    '  3. de <meta robots="noindex"> hieronder schrappen
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageMeta
    @* Preview-pagina: nog niet laten indexeren zolang /over-ons de oude versie toont *@
    <meta name="robots" content="noindex, follow" />
End Section

@section PageStyle
    <link rel="stylesheet" href="~/Content/home-sections.css" />
    <link rel="stylesheet" href="~/Content/about.css" />
End Section

<section class="about-hero">
    <div class="container">
        <div class="about-hero-inner">
            <p class="about-hero-kicker">Over Group LN</p>
            <h1 class="about-hero-title">Bouwen aan meer dan een dak boven je hoofd</h1>
            <p class="about-hero-text">Group LN ontwikkelt en begeleidt bouwprojecten in Gent en Oost-Vlaanderen, van eerste ontwerp tot definitieve oplevering.</p>
        </div>
    </div>
</section>

<section class="about-story about-story--sticky">
    <div class="container">
        <div class="about-grid">
            <div class="about-media reveal">
                <img class="about-media-foto" src="@Url.Content("~/Content/img/about.webp")" alt="Group LN" width="500" height="600" />
            </div>
            <div class="about-content reveal reveal-slide-right">
                <p class="section-kicker">Ons verhaal</p>
                <h2 class="about-headline">Een familiebedrijf, gebouwd voor de lange termijn</h2>
                <p class="about-text">@(DateTime.Now.Year - 1999) jaar geleden richtte Ignace Lataire het bedrijf op, met de ervaring van jarenlang werfleider en projectleider zijn bij diverse aannemingsbedrijven al stevig in de rugzak. Die praktijkkennis — weten hoe een werf écht draait, van de eerste funderingen tot de laatste afwerking — vormde meteen het fundament van wat vandaag Group LN is.</p>
                <p class="about-text">Nele bouwde eerst zelf ervaring op bij een totaalaannemer, voor ze de stap zette naar het familiebedrijf. Niels vervoegde later de firma. Samen groeiden ze mee met elk project, en leerden ze het vak met dezelfde aandacht voor kwaliteit en langetermijnvisie die Ignace als pater familias had voorgeleefd.</p>
                <p class="about-text">Vandaag staan Nele en Niels zelf aan het roer van Group LN. Ze zetten de zaak van Ignace voort met dezelfde combinatie van bedrijfscontinuïteit en praktijkervaring — aangevuld met hun eigen blik op wonen vandaag en morgen.</p>
                <p class="about-text">Elk project is anders. Daarom werken we nauw samen met architecten om uw woonwensen te vertalen naar een ontwerp dat vandaag functioneel is, en morgen nog steeds klopt.</p>
                <a class="about-btn" href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional})">Bekijk onze woonprojecten <i class="fa fa-arrow-right"></i></a>
            </div>
        </div>
    </div>
</section>

<section class="about-pijlers-section">
    <div class="container">
        <div class="about-section-head reveal">
            <p class="section-kicker">Wat wij doen</p>
            <h2>Van eerste steen tot laatste sleutel</h2>
            <p>Onze werking steunt op drie pijlers.</p>
        </div>
        <div class="about-pijlers">
            <div class="about-pijler reveal">
                <span class="about-pijler-num">01</span>
                <div class="about-pijler-body">
                    <h3>Projectontwikkeling</h3>
                    <p>Vanuit onze thuisbasis in Drongen (Gent) zoeken wij actief naar de mooiste locaties in heel Vlaanderen en ontwikkelen er nieuwbouwappartementen en woningen met een duidelijke visie. Van de eerste schets tot de laatste steen werken we hand in hand met onze architecten: we zijn nauw betrokken bij elke ontwerpkeuze, met blijvende aandacht voor esthetiek, functionaliteit en energie-efficiëntie. Zo garanderen we woonprojecten die niet alleen vandaag aanspreken, maar ook op lange termijn hun waarde behouden.</p>
                    <a class="about-pijler-link" href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional})">Bekijk onze woonprojecten <i class="fa fa-arrow-right"></i></a>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">02</span>
                <div class="about-pijler-body">
                    <h3>Projectbegeleiding</h3>
                    <p>Wij begeleiden ook bouwprojecten van andere bouwheren en ontwikkelaars, zowel residentieel als commercieel en industrieel. Van ontwerp tot oplevering nemen we het volledige traject mee op: aanbesteding, coördinatie van de bouwloten, werfopvolging, budgetbewaking en klantenopvolging. Dankzij onze jarenlange ervaring als projectontwikkelaar kennen we elke fase van dichtbij, en vertalen we dat inzicht naar een efficiënte en transparante begeleiding op maat van uw project — ongeacht de schaal of het type gebouw.</p>
                    <a class="about-pijler-link" href="@Url.RouteUrl("Projectbegeleiding")">Ontdek onze projectbegeleiding <i class="fa fa-arrow-right"></i></a>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">03</span>
                <div class="about-pijler-body">
                    <h3>Grond- en pandverwerving</h3>
                    <p>Heeft u een bouwgrond, een oude woning of een verouderd pand met potentieel? Wij onderzoeken vrijblijvend de mogelijkheden voor aankoop, ruil of samenwerking. Onze kennis van de regio en onze langetermijnvisie op vastgoed laten ons toe om ook op moeilijkere of atypische locaties kansen te zien — in het belang van u als eigenaar én van de buurt.</p>
                    <a class="about-pijler-link" href="@Url.RouteUrl("Grondverwerving")">Grond of pand aanbieden <i class="fa fa-arrow-right"></i></a>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="about-waarom">
    <div class="container">
        <p class="section-kicker reveal">Waarom Group LN</p>
        <h2 class="reveal">Waarom bouwheren en kopers voor ons kiezen</h2>
        <div class="about-waarom-grid">
            <div class="about-waarom-item reveal">
                <h3>Van A tot Z betrokken</h3>
                <p>Van ontwerp tot definitieve oplevering blijven wij actief betrokken bij elke fase van het project.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>Kwaliteit die blijft</h3>
                <p>Onze projecten worden ontworpen met het oog op de komende decennia, niet enkel op de dag van oplevering.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>Eén aanspreekpunt</h3>
                <p>Architect, ingenieur, EPB-verslaggever, aannemer en veiligheidscoördinator: wij coördineren ze allemaal. U heeft één contactpersoon.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>Praktijkkennis van de werf</h3>
                <p>Onze zaakvoerders stonden zelf jarenlang als werfleider en projectleider op de werf. Die kennis zit in elk detail.</p>
            </div>
        </div>
    </div>
</section>

<section class="about-eind-cta">
    <div class="container reveal">
        <h2>Leer ons graag beter kennen?</h2>
        <p>Ontdek ons team, bekijk onze realisaties of neem vrijblijvend contact op.</p>
        <div class="about-cta-actions">
            <a class="cta-btn" href="@Url.Action("Index", "Team")">Bekijk ons team <i class="fa fa-arrow-right"></i></a>
            <a class="about-btn" href="@Url.Action("Index", "References", New With {.id = UrlParameter.Optional})">Bekijk onze realisaties <i class="fa fa-arrow-right"></i></a>
            <a class="about-btn" href="@Url.Action("Index", "Contact")">Contacteer ons <i class="fa fa-arrow-right"></i></a>
        </div>
    </div>
</section>
