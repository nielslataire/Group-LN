@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/legal.css" />
End Section

@*
    TE CONTROLEREN (juridisch nazicht vereist — concept, geen juridisch advies):
    - Exacte cookienamen en bewaartermijnen: open de site met de browser-devtools (tab
      Application > Cookies) en vul de tabel aan met wat er effectief geplaatst wordt.
    - Welke tags staan actief in Google Tag Manager? (Google Analytics 4? Google Ads?
      Meta-pixel? LinkedIn Insight Tag?) Voeg per marketingtag een rij toe.
    - Deze pagina gaat ervan uit dat er nog GEEN cookiebanner is. Zodra de consent-banner
      (punt 1e) live staat, moet de tekst bij "Je toestemming beheren" worden aangepast.
*@

<section class="legal-header">
    <div class="container">
        <h1>Cookiebeleid</h1>
        <p class="legal-updated">Laatst bijgewerkt op 28 augustus 2026</p>
    </div>
</section>

<div class="legal-wrap">
    <div class="container">
        <article class="legal-body">

            <p>
                Dit cookiebeleid legt uit welke cookies en gelijkaardige technieken de website van
                Group LN BV gebruikt, waarvoor ze dienen en hoe je je voorkeuren beheert. Lees dit
                samen met ons <a href="@Url.Action("Privacybeleid", "Legal")">privacybeleid</a>.
            </p>

            <h2 id="wat">1. Wat zijn cookies</h2>
            <p>
                Cookies zijn kleine tekstbestanden die bij een bezoek aan een website op je toestel
                worden geplaatst. Ze zorgen er onder meer voor dat een website goed werkt, onthouden
                je voorkeuren of helpen ons het gebruik van de site te meten. We gebruiken ook
                gelijkaardige technieken zoals pixels en lokale opslag; waar we in dit beleid
                "cookies" schrijven, bedoelen we ook die technieken.
            </p>

            <h2 id="categorieen">2. Welke cookies we gebruiken</h2>

            <h3>Noodzakelijke cookies</h3>
            <p>
                Deze cookies zijn nodig om de website en de formulieren te laten werken en om je
                keuze rond cookies te onthouden. Ze worden altijd geplaatst; hiervoor is geen
                toestemming vereist.
            </p>
            <div class="legal-table-wrap">
                <table>
                    <thead>
                        <tr><th>Cookie</th><th>Doel</th><th>Bewaartermijn</th></tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>ASP.NET_SessionId</td>
                            <td>Houdt je sessie op de server bij tijdens je bezoek.</td>
                            <td>Einde van de sessie</td>
                        </tr>
                        <tr>
                            <td>__RequestVerificationToken</td>
                            <td>Beveiligt formulieren tegen misbruik (anti-CSRF).</td>
                            <td>Einde van de sessie</td>
                        </tr>
                        <tr>
                            <td>groupln_cookie_consent</td>
                            <td>Onthoudt je cookievoorkeuren.</td>
                            <td>6 maanden</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <h3>Analytische cookies</h3>
            <p>
                Via Google Tag Manager laden we Google Analytics om geanonimiseerde statistieken bij
                te houden over hoe bezoekers onze site gebruiken. Deze cookies worden alleen
                geplaatst als je daarmee instemt.
            </p>
            <div class="legal-table-wrap">
                <table>
                    <thead>
                        <tr><th>Cookie</th><th>Aanbieder</th><th>Doel</th><th>Bewaartermijn</th></tr>
                    </thead>
                    <tbody>
                        <tr><td>_ga</td><td>Google</td><td>Onderscheidt bezoekers.</td><td>2 jaar</td></tr>
                        <tr><td>_ga_&lt;container-id&gt;</td><td>Google</td><td>Houdt de sessiestatus bij.</td><td>2 jaar</td></tr>
                        <tr><td>_gid</td><td>Google</td><td>Onderscheidt bezoekers.</td><td>24 uur</td></tr>
                    </tbody>
                </table>
            </div>

            <h3>Beveiligingscookies (reCAPTCHA)</h3>
            <p>
                Onze contact- en sollicitatieformulieren gebruiken Google reCAPTCHA om spam en
                geautomatiseerd misbruik tegen te gaan. Google kan hierbij een cookie plaatsen en
                gegevens over je apparaat en gedrag verwerken.
            </p>
            <div class="legal-table-wrap">
                <table>
                    <thead>
                        <tr><th>Cookie</th><th>Aanbieder</th><th>Doel</th><th>Bewaartermijn</th></tr>
                    </thead>
                    <tbody>
                        <tr><td>_GRECAPTCHA</td><td>Google</td><td>Onderscheidt mensen van bots.</td><td>6 maanden</td></tr>
                    </tbody>
                </table>
            </div>

            @*
                TE CONTROLEREN: staan er marketing-/advertentietags in Google Tag Manager
                (Google Ads, Meta-pixel, LinkedIn Insight Tag ...)? Voeg dan hier een kop
                "Marketingcookies" toe met een tabel per aanbieder.
            *@

            <h2 id="derden">3. Cookies van derden</h2>
            <p>
                De analytische en beveiligingscookies hierboven worden geplaatst door Google. Op die
                verwerking zijn het
                <a href="https://policies.google.com/privacy" target="_blank" rel="noopener">privacybeleid van Google</a>
                en de
                <a href="https://policies.google.com/technologies/cookies" target="_blank" rel="noopener">informatie van Google over cookies</a>
                van toepassing. We hebben geen controle over de cookies die derden plaatsen.
            </p>

            <h2 id="toestemming">4. Je toestemming beheren</h2>
            <p>
                Bij je eerste bezoek verschijnt een cookiemelding waarin je kan kiezen welke cookies
                we mogen plaatsen. Niet-noodzakelijke cookies (statistiek en marketing) plaatsen we
                pas nadat je ze daar hebt aanvaard.
            </p>
            <p>
                Je kan je keuze op elk moment wijzigen of intrekken via het cookie-icoon linksonder op
                elke pagina, of via onderstaande knop:
            </p>
            <p>
                <button type="button" class="legal-consent-btn" onclick="if(window.grouplnCookieConsent){window.grouplnCookieConsent.open();}">Cookievoorkeuren openen</button>
            </p>
            <p>
                Daarnaast kan je cookies altijd beheren of verwijderen via de instellingen van je
                browser. Meer uitleg vind je bij
                <a href="https://support.google.com/chrome/answer/95647" target="_blank" rel="noopener">Chrome</a>,
                <a href="https://support.mozilla.org/nl/kb/cookies-verwijderen-gegevens-wissen-websites-opgeslagen" target="_blank" rel="noopener">Firefox</a>,
                <a href="https://support.apple.com/nl-be/guide/safari/sfri11471/mac" target="_blank" rel="noopener">Safari</a> en
                <a href="https://support.microsoft.com/nl-nl/microsoft-edge/cookies-verwijderen-in-microsoft-edge-63947406-40ac-c3b8-57b9-2a946a29ae09" target="_blank" rel="noopener">Edge</a>.
                Als je alle cookies weigert, werken bepaalde onderdelen van de site mogelijk niet
                optimaal.
            </p>

            <h2 id="wijzigingen">5. Wijzigingen aan dit cookiebeleid</h2>
            <p>
                We kunnen dit cookiebeleid aanpassen wanneer onze website of de gebruikte diensten
                wijzigen. De datum bovenaan deze pagina geeft aan wanneer het beleid het laatst is
                bijgewerkt.
            </p>

            <h2 id="contact">6. Contact</h2>
            <p>
                Vragen over dit cookiebeleid? Mail naar
                <a href="mailto:info@groupln.be">info@groupln.be</a>.
            </p>

            <a class="legal-back" href="@Url.Action("Index", "Home")">&larr; Terug naar de startpagina</a>

        </article>
    </div>
</div>
