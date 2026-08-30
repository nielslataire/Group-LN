@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/legal.css" />
End Section

@*
    TE CONTROLEREN (juridisch nazicht vereist — dit is een concept, geen juridisch advies):
    - Exacte lijst actieve tags in Google Tag Manager (Analytics? Meta/LinkedIn pixel? Ads?).
    - Serverlocatie SmarterASP.NET en of er een verwerkersovereenkomst is.
    - Verwerkersovereenkomsten met Microsoft en Google.
    - Bewaartermijn serverlogs (nu ingevuld als 12 maanden).
    - Of nieuwsbriefinschrijvingen ergens bewaard worden (nu enkel per e-mail doorgestuurd).
*@

<section class="legal-header">
    <div class="container">
        <h1>Privacybeleid</h1>
        <p class="legal-updated">Laatst bijgewerkt op 28 augustus 2026</p>
    </div>
</section>

<div class="legal-wrap">
    <div class="container">
        <article class="legal-body">

            <p>
                Dit privacybeleid legt uit hoe Group LN BV je persoonsgegevens verwerkt wanneer je
                onze website bezoekt of een van onze formulieren gebruikt. We verwerken je gegevens
                in overeenstemming met de Algemene Verordening Gegevensbescherming (AVG/GDPR) en de
                Belgische privacywetgeving.
            </p>

            <div class="legal-toc">
                <p class="legal-toc-title">Inhoud</p>
                <ol>
                    <li><a href="#verantwoordelijke">Wie is verantwoordelijk voor je gegevens</a></li>
                    <li><a href="#welke-gegevens">Welke gegevens we verwerken en waarom</a></li>
                    <li><a href="#rechtsgronden">Rechtsgronden</a></li>
                    <li><a href="#bewaartermijn">Hoe lang we je gegevens bewaren</a></li>
                    <li><a href="#ontvangers">Met wie we gegevens delen</a></li>
                    <li><a href="#buiten-eer">Doorgifte buiten de Europese Economische Ruimte</a></li>
                    <li><a href="#beveiliging">Beveiliging</a></li>
                    <li><a href="#rechten">Je rechten</a></li>
                    <li><a href="#cookies">Cookies</a></li>
                    <li><a href="#wijzigingen">Wijzigingen aan dit privacybeleid</a></li>
                    <li><a href="#contact">Contact</a></li>
                </ol>
            </div>

            <h2 id="verantwoordelijke">1. Wie is verantwoordelijk voor je gegevens</h2>
            <p>De verwerkingsverantwoordelijke is:</p>
            <address>
                <strong>Group LN BV</strong><br />
                Klaverdries 53, 9031 Drongen, België<br />
                Ondernemingsnummer / btw: BE 0847.396.849<br />
                E-mail: <a href="mailto:info@groupln.be">info@groupln.be</a><br />
                Telefoon: <a href="tel:+3292164950">+32 (0)9 216 49 50</a>
            </address>

            <h2 id="welke-gegevens">2. Welke gegevens we verwerken en waarom</h2>

            <h3>Contactformulier</h3>
            <p>
                Wanneer je het contactformulier invult, verwerken we je voornaam, achternaam,
                e-mailadres, telefoonnummer, het gekozen onderwerp en je bericht. We gebruiken deze
                gegevens uitsluitend om je vraag te behandelen en je te antwoorden. De inhoud wordt
                per e-mail bezorgd op ons info-adres.
            </p>

            <h3>Sollicitatieformulier</h3>
            <p>
                Wanneer je solliciteert op een vacature, verwerken we je voornaam, achternaam,
                e-mailadres, telefoonnummer, je motivatie en het cv-bestand dat je oplaadt. Deze
                gegevens worden per e-mail bezorgd op ons info-adres <strong>en opgeslagen in onze
                database</strong>, zodat we je kandidatuur kunnen opvolgen. We gebruiken ze enkel
                in het kader van de betrokken sollicitatieprocedure.
            </p>

            <h3>Aanvraag van een brochure, plan of documenten</h3>
            <p>
                Wanneer je via een projectpagina een brochure, plan of ander document opvraagt,
                verwerken we je naam en e-mailadres om je het gevraagde te bezorgen en je desgevraagd
                verder te informeren over het project.
            </p>

            <h3>Inschrijving op de nieuwsbrief</h3>
            <p>
                Wanneer je je inschrijft op onze nieuwsbrief, verwerken we je e-mailadres om je
                nieuws over onze projecten te sturen. Je kan je op elk moment uitschrijven.
            </p>

            <h3>Automatisch verzamelde gegevens</h3>
            <p>
                Bij een bezoek aan de website verwerken we technische gegevens zoals je IP-adres,
                het type browser en apparaat, de bezochte pagina's en het tijdstip van je bezoek.
                Dit gebeurt via serverlogbestanden en via cookies en gelijkaardige technieken.
                Meer daarover lees je in ons <a href="@Url.Action("Cookiebeleid", "Legal")">cookiebeleid</a>.
            </p>
            <p>
                Onze formulieren zijn beveiligd met Google reCAPTCHA om spam en misbruik tegen te
                gaan. Daarbij verwerkt Google gegevens over je apparaat en je gedrag op de pagina.
                Op dit gebruik zijn het
                <a href="https://policies.google.com/privacy" target="_blank" rel="noopener">privacybeleid</a>
                en de
                <a href="https://policies.google.com/terms" target="_blank" rel="noopener">gebruiksvoorwaarden</a>
                van Google van toepassing.
            </p>

            <h2 id="rechtsgronden">3. Rechtsgronden</h2>
            <p>Naargelang de verwerking baseren we ons op een van de volgende rechtsgronden:</p>
            <ul>
                <li>
                    <strong>Uitvoering van (precontractuele) maatregelen op jouw verzoek</strong> —
                    voor de behandeling van je contactvraag, je sollicitatie of je aanvraag van
                    documenten.
                </li>
                <li>
                    <strong>Je toestemming</strong> — voor de inschrijving op de nieuwsbrief en voor
                    het plaatsen van niet-noodzakelijke cookies. Je kan je toestemming altijd
                    intrekken.
                </li>
                <li>
                    <strong>Ons gerechtvaardigd belang</strong> — voor de beveiliging en goede
                    werking van de website, de bestrijding van spam en misbruik, en beperkte
                    statistiek over het gebruik van de site.
                </li>
                <li>
                    <strong>Wettelijke verplichting</strong> — wanneer we gegevens moeten bewaren of
                    meedelen op grond van de wet.
                </li>
            </ul>

            <h2 id="bewaartermijn">4. Hoe lang we je gegevens bewaren</h2>
            <ul>
                <li>Contactvragen en aanvragen van documenten: tot <strong>5 jaar</strong> na het laatste contact.</li>
                <li>Sollicitaties en cv's: tot <strong>5 jaar</strong>, zodat we je kunnen contacteren bij een passende functie. Laat je ons weten dat je dit niet wenst, dan verwijderen we je gegevens eerder.</li>
                <li>Nieuwsbriefinschrijving: tot je je uitschrijft.</li>
                <li>Serverlogbestanden: <!-- TE CONTROLEREN -->tot 12 maanden.</li>
            </ul>
            <p>Daarna worden je gegevens verwijderd of onomkeerbaar geanonimiseerd, tenzij we ze langer moeten bewaren om te voldoen aan een wettelijke verplichting.</p>

            <h2 id="ontvangers">5. Met wie we gegevens delen</h2>
            <p>
                We verkopen je gegevens nooit. We doen wel een beroep op externe dienstverleners die
                in onze opdracht gegevens verwerken (verwerkers) of die als zelfstandige
                verantwoordelijke optreden voor een specifiek onderdeel:
            </p>
            <ul>
                <li><strong>SmarterASP.NET</strong> — hosting van de website, database en geüploade bestanden (waaronder cv's) en serverlogs.</li>
                <li><strong>Microsoft (Microsoft 365 / Exchange Online)</strong> — e-mailverkeer, waaronder de e-mails die vanuit de formulieren worden verstuurd.</li>
                <li><strong>Google</strong> — Google Tag Manager en Google Analytics (statistiek), Google reCAPTCHA (beveiliging van formulieren) en Google Fonts (lettertypes). Google ontvangt hierbij onder meer je IP-adres.</li>
                <li><!-- TE CONTROLEREN: overwegen dit lettertype-/icoonbestand zelf te hosten --><strong>unpkg (Cloudflare)</strong> — levering van een icoonlettertype; je IP-adres wordt hierbij aan deze dienst doorgegeven.</li>
            </ul>
            <p>Daarnaast kunnen we gegevens meedelen aan overheidsinstanties wanneer we daartoe wettelijk verplicht zijn.</p>

            <h2 id="buiten-eer">6. Doorgifte buiten de Europese Economische Ruimte</h2>
            <p>
                Bij het gebruik van diensten van Google en Microsoft kunnen je gegevens verwerkt
                worden op servers buiten de Europese Economische Ruimte, onder meer in de Verenigde
                Staten. Deze doorgifte gebeurt op basis van de standaardcontractbepalingen van de
                Europese Commissie en/of het EU-VS Data Privacy Framework, aangevuld met passende
                bijkomende waarborgen.
            </p>

            <h2 id="beveiliging">7. Beveiliging</h2>
            <p>
                We nemen passende technische en organisatorische maatregelen om je gegevens te
                beschermen tegen verlies, misbruik en ongeoorloofde toegang. De website werkt
                volledig over een beveiligde verbinding (HTTPS) en de toegang tot persoonsgegevens
                is beperkt tot de medewerkers die ze nodig hebben voor hun taak.
            </p>

            <h2 id="rechten">8. Je rechten</h2>
            <p>Je hebt met betrekking tot je persoonsgegevens het recht op:</p>
            <ul>
                <li>inzage in de gegevens die we over je verwerken;</li>
                <li>verbetering van onjuiste of onvolledige gegevens;</li>
                <li>verwijdering van je gegevens ("recht op vergetelheid");</li>
                <li>beperking van de verwerking;</li>
                <li>overdraagbaarheid van de gegevens die je zelf hebt verstrekt;</li>
                <li>bezwaar tegen een verwerking op basis van gerechtvaardigd belang;</li>
                <li>intrekking van je toestemming, zonder dat dit afbreuk doet aan de rechtmatigheid van de verwerking vóór die intrekking.</li>
            </ul>
            <p>
                Je oefent deze rechten uit door een e-mail te sturen naar
                <a href="mailto:info@groupln.be">info@groupln.be</a>. We kunnen je vragen je
                identiteit te bevestigen en antwoorden binnen de wettelijke termijn van één maand.
            </p>
            <p>
                Ben je van mening dat we je gegevens niet correct verwerken, dan kan je klacht
                indienen bij de Gegevensbeschermingsautoriteit, Drukpersstraat 35, 1000 Brussel —
                <a href="https://www.gegevensbeschermingsautoriteit.be" target="_blank" rel="noopener">www.gegevensbeschermingsautoriteit.be</a>,
                <a href="mailto:contact@apd-gba.be">contact@apd-gba.be</a>.
            </p>

            <h2 id="cookies">9. Cookies</h2>
            <p>
                Onze website gebruikt cookies en gelijkaardige technieken. Welke dat precies zijn,
                waarvoor ze dienen en hoe je je voorkeuren beheert, lees je in ons
                <a href="@Url.Action("Cookiebeleid", "Legal")">cookiebeleid</a>.
            </p>

            <h2 id="wijzigingen">10. Wijzigingen aan dit privacybeleid</h2>
            <p>
                We kunnen dit privacybeleid van tijd tot tijd aanpassen, bijvoorbeeld wanneer onze
                website of de wetgeving wijzigt. De datum bovenaan deze pagina geeft aan wanneer het
                beleid het laatst is bijgewerkt.
            </p>

            <h2 id="contact">11. Contact</h2>
            <p>
                Vragen over dit privacybeleid of over de verwerking van je gegevens? Mail naar
                <a href="mailto:info@groupln.be">info@groupln.be</a> of bel
                <a href="tel:+3292164950">+32 (0)9 216 49 50</a>.
            </p>

            <a class="legal-back" href="@Url.Action("Index", "Home")">&larr; Terug naar de startpagina</a>

        </article>
    </div>
</div>
