@Code
    ' Landingspagina grond- en pandverwerving. Route: /grond-of-pand-aanbieden (naam "Grondverwerving").
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim jarenErvaring As Integer = DateTime.Now.Year - 1999

    ' Formulier onderaan (ContactController.GrondAanbieden): bij een fout komt de pagina terug met de ingevulde
    ' waarden en de foutmeldingen per veld; na een geslaagde verzending (post-redirect-get) staat er een bevestiging.
    Dim grondForm As wwwcopro.MailModel = TryCast(ViewData("GrondForm"), wwwcopro.MailModel)
    If grondForm Is Nothing Then grondForm = New wwwcopro.MailModel()
    Dim grondFout As String = TryCast(ViewData("GrondFout"), String)
    Dim grondSuccesNaam As String = TryCast(TempData("GrondSuccessNaam"), String)
    Dim grondSuccesEmail As String = TryCast(TempData("GrondSuccessEmail"), String)
    Dim grondSucces As Boolean = Not String.IsNullOrEmpty(grondSuccesEmail)
    Dim veldFout As Func(Of String, String) =
        Function(sleutel As String) As String
            If ViewData.ModelState.ContainsKey(sleutel) AndAlso ViewData.ModelState(sleutel).Errors.Count > 0 Then
                Return ViewData.ModelState(sleutel).Errors(0).ErrorMessage
            End If
            Return Nothing
        End Function

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
    <link rel="stylesheet" href="~/Content/contact.css" />
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
            <div class="about-hero-cta">
                <a class="cta-btn" href="#grond-formulier">Uw grond of pand nu aanbieden <i class="fa fa-arrow-right"></i></a>
            </div>
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

@* ── Formulier: grond of pand aanbieden (onderwerp staat vast, zie ContactController.GrondAanbieden) ── *@
<section class="grond-aanvraag" id="grond-formulier">
    <div class="container">
        <div class="grond-aanvraag-inner reveal">
            @If grondSucces Then
                @<div class="contact-success">
                    <div class="contact-success-check">
                        <svg viewBox="0 0 64 64" width="56" height="56">
                            <circle class="contact-success-check-circle" cx="32" cy="32" r="28" fill="none" stroke-width="3" />
                            <path class="contact-success-check-mark" fill="none" stroke-width="4" d="M20 33l8 8 16-17" />
                        </svg>
                    </div>
                    <h2 class="contact-form-title">Bedankt@(If(Not String.IsNullOrWhiteSpace(grondSuccesNaam), ", " & grondSuccesNaam, ""))!</h2>
                    <p class="contact-form-sub">We hebben uw aanbod ontvangen. We onderzoeken de mogelijkheden en laten binnen 7 werkdagen iets horen op @grondSuccesEmail.</p>
                    <script>window.dataLayer = window.dataLayer || []; window.dataLayer.push({ event: 'grond_form_submit' });</script>
                </div>
            Else
                @<text>
                    <h2 class="contact-form-title">Grond of pand aanbieden?</h2>
                    <p class="contact-form-sub">Bezorg ons vrijblijvend de gegevens van uw grond of pand: waar ligt het, hoe groot is het en wat is de huidige toestand? Wij onderzoeken de mogelijkheden en komen binnen 7 werkdagen bij u terug.</p>

                    @If Not String.IsNullOrEmpty(grondFout) Then
                        @<p class="contact-field-error grond-aanvraag-fout">@grondFout</p>
                    End If

                    @Using Html.BeginForm("GrondAanbieden", "Contact", FormMethod.Post, New With {.id = "grondForm", .autocomplete = "on"})
                        @<text>
                            @Html.AntiForgeryToken()

                            @* Honeypot: enkel bots vullen dit verborgen veld in *@
                            <div style="position:absolute;left:-9999px;top:-9999px;opacity:0;" aria-hidden="true">
                                <input type="text" name="website_url" tabindex="-1" autocomplete="off" />
                            </div>
                            <input type="hidden" name="g-recaptcha-response" id="gRecaptchaResponse" />

                            <div class="contact-form-grid">
                                <div class="contact-field">
                                    <label for="Voornaam">Voornaam<span class="req">*</span></label>
                                    <input type="text" class="form-control" id="Voornaam" name="Voornaam" value="@grondForm.Voornaam" autocomplete="given-name" required="required" />
                                    @If veldFout("Voornaam") IsNot Nothing Then
                                        @<span class="contact-field-error">@veldFout("Voornaam")</span>
                                    End If
                                </div>
                                <div class="contact-field">
                                    <label for="Achternaam">Achternaam<span class="req">*</span></label>
                                    <input type="text" class="form-control" id="Achternaam" name="Achternaam" value="@grondForm.Achternaam" autocomplete="family-name" required="required" />
                                    @If veldFout("Achternaam") IsNot Nothing Then
                                        @<span class="contact-field-error">@veldFout("Achternaam")</span>
                                    End If
                                </div>
                                <div class="contact-field">
                                    <label for="EmailTo">E-mailadres<span class="req">*</span></label>
                                    <input type="email" class="form-control" id="EmailTo" name="EmailTo" value="@grondForm.EmailTo" autocomplete="email" required="required" />
                                    @If veldFout("EmailTo") IsNot Nothing Then
                                        @<span class="contact-field-error">@veldFout("EmailTo")</span>
                                    End If
                                </div>
                                <div class="contact-field">
                                    <label for="Phone">Telefoonnummer</label>
                                    <input type="text" class="form-control" id="Phone" name="Phone" value="@grondForm.Phone" autocomplete="tel" />
                                    @If veldFout("Phone") IsNot Nothing Then
                                        @<span class="contact-field-error">@veldFout("Phone")</span>
                                    End If
                                </div>
                                <div class="contact-field contact-field-full">
                                    <label for="Message">Uw grond of pand<span class="req">*</span></label>
                                    <textarea class="form-control" id="Message" name="Message" rows="5" placeholder="Adres of ligging, oppervlakte, huidige toestand en eventuele bestaande vergunningen" required="required">@grondForm.Message</textarea>
                                    @If veldFout("Message") IsNot Nothing Then
                                        @<span class="contact-field-error">@veldFout("Message")</span>
                                    End If
                                </div>
                            </div>

                            <div class="contact-privacy">
                                <input type="checkbox" id="PrivacyAkkoord" name="PrivacyAkkoord" value="true" checked="@(If(grondForm.PrivacyAkkoord, "checked", Nothing))" />
                                <input type="hidden" name="PrivacyAkkoord" value="false" />
                                <label for="PrivacyAkkoord">Ik ga akkoord dat Group LN mijn gegevens gebruikt om mijn aanbod te beantwoorden, conform het <a href="@Url.Action("Privacybeleid", "Legal")" target="_blank" rel="noopener">privacybeleid</a>.</label>
                            </div>
                            @If veldFout("PrivacyAkkoord") IsNot Nothing Then
                                @<span class="contact-field-error">@veldFout("PrivacyAkkoord")</span>
                            End If

                            <button type="submit" class="contact-submit-btn">Aanbod versturen <i class="fa fa-arrow-right"></i></button>
                        </text>
                    End Using
                    <span class="about-cta-mail">of mail rechtstreeks naar <a href="mailto:info@groupln.be">info@groupln.be</a></span>
                </text>
            End If
        </div>
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

@section scripts
    <script src="https://www.google.com/recaptcha/api.js?render=@ViewBag.ReCaptchaSiteKey"></script>
    <script>
        // Hero-knop: soepel naar het formulier scrollen, onder de vaste header (zoals de ankerlinks op de projectpagina).
        // Geen globale scroll-behavior: smooth, dat maakt gewoon scrollen traag.
        (function () {
            var cta = document.querySelector('.about-hero-cta a[href^="#"]');
            if (!cta) return;
            cta.addEventListener('click', function (e) {
                var target = document.getElementById(cta.getAttribute('href').slice(1));
                if (!target) return;
                e.preventDefault();
                var reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
                var top = target.getBoundingClientRect().top + window.pageYOffset - 100;
                window.scrollTo({ top: top, behavior: reduced ? 'auto' : 'smooth' });
                if (history.replaceState) history.replaceState(null, '', '#' + target.id);
            });
        })();

        // reCAPTCHA v3-token ophalen vlak voor het versturen (onzichtbaar voor de bezoeker), zoals op de contactpagina
        (function () {
            var form = document.getElementById('grondForm');
            if (!form) return;
            if (window.glBusy) {
                form.addEventListener('submit', function () { window.glBusy(form.querySelector('button[type=submit]'), true, 'Bezig met verzenden…'); });
            }
            var siteKey = '@ViewBag.ReCaptchaSiteKey';
            var action = '@ViewBag.ReCaptchaAction';
            var input = document.getElementById('gRecaptchaResponse');
            if (!siteKey || typeof grecaptcha === 'undefined') return;

            form.addEventListener('submit', function (e) {
                if (form.dataset.captchaReady === '1') return;
                e.preventDefault();
                grecaptcha.ready(function () {
                    grecaptcha.execute(siteKey, { action: action || 'contact' }).then(function (token) {
                        input.value = token;
                        form.dataset.captchaReady = '1';
                        form.submit();
                    }, function () {
                        form.dataset.captchaReady = '1';
                        form.submit();
                    });
                });
            });
        })();
    </script>
End Section
