@Code
    ' Landingspagina grond- en pandverwerving. Route: /grond-of-pand-aanbieden (naam "Grondverwerving").
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim jarenErvaring As Integer = DateTime.Now.Year - 1999

    ' Eén bron voor zowel het zichtbare FAQ-accordeon als het FAQPage-schema.
    Dim faqItems As New List(Of Tuple(Of String, String)) From {
        Tuple.Create(
            "Wat is grond- en pandverwerving?",
            "Grond- en pandverwerving is het proces waarbij een projectontwikkelaar bouwgrond, woningen of panden aankoopt (of ruilt) met het oog op een nieuwbouw- of renovatieproject."),
        Tuple.Create(
            "Welke soorten eigendommen komen in aanmerking?",
            "Bouwgronden, oude woningen die in aanmerking komen voor vervangbouw, verouderde handels- of kantoorpanden, en opbrengsteigendommen die u wil ruilen tegen nieuwbouw."),
        Tuple.Create(
            "Hoeveel is mijn grond of pand waard voor een projectontwikkelaar?",
            "Dat hangt af van de ligging, de bestemming en het toegelaten bouwvolume. Na uw aanbod maken wij een voorstudie op om een concurrentiële, marktconforme prijs te bepalen."),
        Tuple.Create(
            "Hoe lang duurt het traject van aanbod tot voorstel?",
            "Doorgaans 2 tot 6 weken, afhankelijk van de complexiteit van het dossier."),
        Tuple.Create(
            "Kan ik mijn pand ruilen in plaats van verkopen?",
            "Ja. Bij een opbrengsteigendom kan u kiezen om te ruilen tegen een of meerdere eenheden in het nieuwbouwproject, in plaats van een aankoopprijs te ontvangen. Dit is een systeem koop-verkoop waarbij u grondaandelen in het project behoudt en constructiewaardes koopt met het geld dat u ontvangt."),
        Tuple.Create(
            "Is het aanbieden van mijn grond of pand vrijblijvend?",
            "Ja, volledig. Wij onderzoeken de mogelijkheden en komen bij u terug met een voorstel — u beslist zelf of u daarop ingaat.")
    }
End Code
@Code
    ' JSON-LD opbouwen (zelfde bron als het zichtbare accordeon). "@@type" enz. staan
    ' met dubbele at zoals elders in dit project (Razor zet @@ om naar @).
    Dim _ser As New System.Web.Script.Serialization.JavaScriptSerializer()

    Dim _faqSb As New System.Text.StringBuilder()
    For i As Integer = 0 To faqItems.Count - 1
        _faqSb.Append("{""@@type"":""Question"",""name"":" & _ser.Serialize(faqItems(i).Item1) &
                      ",""acceptedAnswer"":{""@@type"":""Answer"",""text"":" & _ser.Serialize(faqItems(i).Item2) & "}}")
        If i < faqItems.Count - 1 Then _faqSb.Append(",")
    Next
    Dim _faqJson As String = "{""@@context"":""https://schema.org"",""@@type"":""FAQPage"",""mainEntity"":[" &
                             _faqSb.ToString() & "]}"

    Dim _breadcrumbJson As String =
        "{""@@context"":""https://schema.org"",""@@type"":""BreadcrumbList"",""itemListElement"":[" &
        "{""@@type"":""ListItem"",""position"":1,""name"":""Home"",""item"":""https://www.groupln.be/""}," &
        "{""@@type"":""ListItem"",""position"":2,""name"":""Over ons"",""item"":""https://www.groupln.be/over-ons""}," &
        "{""@@type"":""ListItem"",""position"":3,""name"":""Grond- en pandverwerving"",""item"":""https://www.groupln.be/grond-of-pand-aanbieden""}" &
        "]}"
End Code

<script type="application/ld+json">@Html.Raw(_faqJson)</script>
<script type="application/ld+json">@Html.Raw(_breadcrumbJson)</script>

@section PageStyle
    <link rel="stylesheet" href="~/Content/home-sections.css" />
    <link rel="stylesheet" href="~/Content/about.css" />
End Section

@*
    Fotomateriaal — plaats de bestanden in ~/Content/img/grondverwerving/ :
      hero.jpg              Luchtfoto van een recent gerealiseerd project (bouwkraan / gevel)
      zoeken.jpg            Verouderde woning of braakliggend terrein met potentieel ("voor"-beeld)
      bouwgrond.jpg         Luchtfoto van een leeg/bouwrijp perceel
      oude-woning.jpg       Karakteristieke, verouderde eengezinswoning
      verouderd-pand.jpg    Leegstaand handels- of kantoorpand
      opbrengsteigendom.jpg Bestaand appartementsgebouw / meergezinswoning
    Ontbreekt een bestand, dan valt de hero terug op het groene kleurverloop en tonen de
    kaarten een lege beeldruimte — de pagina blijft werken.
*@

<section class="about-hero about-hero--foto" style="background-image: linear-gradient(180deg, rgba(7,92,52,0.82) 0%, rgba(0,83,45,0.86) 48%, rgba(0,61,33,0.92) 100%), url('@Url.Content("~/Content/img/grondverwerving/hero.jpg")');">
    <div class="container">
        <div class="about-hero-inner">
            <ul class="about-breadcrumb">
                <li><a href="@Url.Action("Index", "Home")">Home</a></li>
                <li><a href="@Url.RouteUrl("OverOnsHub")">Over ons</a></li>
                <li>Grond- en pandverwerving</li>
            </ul>
            <p class="about-hero-kicker">Grond- en pandverwerving</p>
            <h1 class="about-hero-title">Uw grond of pand, ons volgende project</h1>
            <p class="about-hero-text">Heeft u een bouwgrond, een oude woning of een verouderd pand in bezit? Wij vertalen het naar een concreet ontwikkelingsvoorstel — met een correcte prijs, een duidelijk traject en zonder verplichtingen vooraf.</p>
        </div>
    </div>
</section>

@* ── Wat we zoeken ── *@
<section class="about-story">
    <div class="container">
        <div class="about-grid">
            <div class="about-media reveal">
                <img class="about-media-foto" src="@Url.Content("~/Content/img/grondverwerving/zoeken.webp")"
                     alt="Verouderde woning met ontwikkelingspotentieel" width="500" height="600" loading="lazy" />
            </div>
            <div class="about-content reveal reveal-slide-right">
                <p class="section-kicker">Wat we zoeken</p>
                <h2 class="about-headline">Van bouwgrond tot verouderd pand: wij kijken verder dan de huidige staat</h2>
                <p class="about-text">Als projectontwikkelaar zoeken wij voortdurend naar locaties met potentieel: een perceel bouwgrond, een verouderde woning die aan het einde van haar levensduur is, of een handelspand dat niet langer aansluit bij de huidige noden van de buurt. Waar u misschien enkel de huidige toestand ziet, zien wij de mogelijkheden voor morgen.</p>
                <p class="about-text">Op basis van de gegevens die u ons bezorgt — samen met informatie van de dienst Stedenbouw, het kadaster en de notaris — brengen we de stedenbouwkundige mogelijkheden nauwkeurig in kaart. Zo weten we snel of, en hoe, een nieuwbouw- of verbouwproject haalbaar is, en maken we daar een grondige voorstudie van.</p>
            </div>
        </div>
    </div>
</section>

@* ── Onze aanpak ── *@
<section class="about-pijlers-section">
    <div class="container">
        <div class="about-section-head reveal">
            <p class="section-kicker">Onze aanpak</p>
            <h2>Hoe een samenwerking met Group LN verloopt</h2>
        </div>
        <div class="about-pijlers">
            <div class="about-pijler reveal">
                <span class="about-pijler-num">01</span>
                <div class="about-pijler-body">
                    <h3>Uw aanbod</h3>
                    <p>U bezorgt ons de gegevens van uw grond of pand: ligging, oppervlakte, bestemming en eventuele bestaande vergunningen. Een eerste inschatting maken we doorgaans binnen enkele werkdagen.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">02</span>
                <div class="about-pijler-body">
                    <h3>Voorstudie</h3>
                    <p>Via de dienst Stedenbouw, het kadaster en de notaris onderzoeken we wat er stedenbouwkundig mogelijk is. We stellen een voorstudie op met een inschatting van het bouwvolume en het verwachte rendement.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">03</span>
                <div class="about-pijler-body">
                    <h3>Voorstel</h3>
                    <p>Op basis van die studie doen we u een concurrentieel voorstel: aankoop tegen een marktconforme prijs, of ruil tegen een of meerdere nieuwbouwentiteiten. U beslist zonder enige verplichting.</p>
                </div>
            </div>
            <div class="about-pijler reveal">
                <span class="about-pijler-num">04</span>
                <div class="about-pijler-body">
                    <h3>Realisatie</h3>
                    <p>Gaat u akkoord, dan nemen wij het volledige traject over: van vergunningsaanvraag tot sloop, bouw en oplevering. U hoeft zich nergens zorgen over te maken.</p>
                </div>
            </div>
        </div>
    </div>
</section>

@* ── Wat u ons kunt aanbieden ── *@
<section class="about-aanbod">
    <div class="container">
        <div class="about-section-head reveal">
            <h2>Wat u ons kunt aanbieden</h2>
        </div>
        <div class="about-aanbod-grid about-aanbod-grid--foto">
            <article class="about-aanbod-item reveal">
                <img class="about-aanbod-foto" src="@Url.Content("~/Content/img/grondverwerving/bouwgrond.webp")"
                     alt="Bouwgrond met ontwikkelingspotentieel" width="600" height="450" loading="lazy" />
                <h3>Bouwgrond</h3>
                <p>Een perceel of meerdere aangrenzende percelen, met of zonder bestaande vergunning. Ook onbebouwde restgronden met woonuitbreidingspotentieel komen in aanmerking.</p>
            </article>
            <article class="about-aanbod-item reveal">
                <img class="about-aanbod-foto" src="@Url.Content("~/Content/img/grondverwerving/oude-woning.webp")"
                     alt="Oude woning geschikt voor vervangbouw" width="600" height="450" loading="lazy" />
                <h3>Oude woning</h3>
                <p>Een bestaande woning die in aanmerking komt voor vervangbouw of een grondige renovatie — bijvoorbeeld omdat ze bouwtechnisch verouderd is of niet langer voldoet aan de huidige EPC-normen.</p>
            </article>
            <article class="about-aanbod-item reveal">
                <img class="about-aanbod-foto" src="@Url.Content("~/Content/img/grondverwerving/verouderd-pand.webp")"
                     alt="Verouderd handelspand voor herbestemming" width="600" height="450" loading="lazy" />
                <h3>Verouderd pand</h3>
                <p>Een handels-, kantoor- of bedrijfspand dat aan herbestemming toe is: leegstand, verkeerde ligging voor de huidige functie, of gewoon einde levensduur.</p>
            </article>
            <article class="about-aanbod-item reveal">
                <img class="about-aanbod-foto" src="@Url.Content("~/Content/img/grondverwerving/opbrengsteigendom.webp")"
                     alt="Opbrengsteigendom ruilen tegen nieuwbouw" width="600" height="450" loading="lazy" />
                <h3>Opbrengsteigendom</h3>
                <p>Een eigendom dat u wil ruilen tegen een of meer nieuwbouwentiteiten — een fiscaal en praktisch interessant alternatief voor wie wil herinvesteren zonder cashuitstap.</p>
            </article>
        </div>
    </div>
</section>

@* ── CTA: grond of pand aanbieden ── *@
<section class="about-eind-cta on-white">
    <div class="container reveal">
        <h2>Grond of pand aanbieden?</h2>
        <p>Bezorg ons vrijblijvend de gegevens van uw grond of pand. Wij onderzoeken de mogelijkheden en komen binnen 7 werkdagen bij u terug.</p>
        <div class="about-cta-actions">
            <a class="cta-btn" href="@Url.Action("Index", "Contact", New With {.onderwerp = "Grond of pand aanbieden"})">Grond of pand aanbieden <i class="fa fa-arrow-right"></i></a>
        </div>
        <span class="about-cta-mail">of mail rechtstreeks naar <a href="mailto:info@groupln.be">info@groupln.be</a></span>
    </div>
</section>

@* ── Waarom eigenaars voor ons kiezen ── *@
<section class="about-waarom">
    <div class="container">
        <div class="about-section-head reveal">
            <p class="section-kicker">Waarom eigenaars voor ons kiezen</p>
            <h2>Een correcte prijs, een duidelijk traject en volledige ontzorging</h2>
        </div>
        <div class="about-waarom-grid">
            <div class="about-waarom-item reveal">
                <h3>Snel en duidelijk traject</h3>
                <p>U weet binnen enkele weken of uw grond of pand ontwikkelingspotentieel heeft.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>Marktconform bod, onderbouwd</h3>
                <p>Geen losse schatting, maar een prijs gebaseerd op een voorstudie en stedenbouwkundig onderzoek.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>Volledige ontzorging</h3>
                <p>Van eerste contact tot sleuteloverdracht van het nieuwbouwproject nemen wij alles op ons.</p>
            </div>
            <div class="about-waarom-item reveal">
                <h3>@jarenErvaring jaar ervaring</h3>
                <p>Ruim een kwarteeuw ervaring en een uitgebreide portfolio van gerealiseerde projecten in Vlaanderen.</p>
            </div>
        </div>
    </div>
</section>

@* ── Veelgestelde vragen ── *@
<section class="about-faq">
    <div class="container">
        <div class="about-section-head reveal">
            <h2>Veelgestelde vragen over grond- en pandverwerving</h2>
        </div>
        <div class="about-faq-lijst reveal">
            @For Each faq In faqItems
                @<details class="about-faq-item">
                    <summary class="about-faq-vraag">
                        <span>@faq.Item1</span>
                        <svg class="about-faq-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                    </summary>
                    <div class="about-faq-antwoord">@faq.Item2</div>
                </details>
            Next
        </div>
    </div>
</section>
