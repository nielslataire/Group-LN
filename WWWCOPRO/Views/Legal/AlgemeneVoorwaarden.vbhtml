@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/legal.css" />
End Section

@*
    TE CONTROLEREN (juridisch nazicht vereist — concept, geen juridisch advies):
    - Deze tekst regelt enkel het GEBRUIK VAN DE WEBSITE. De contractuele afspraken rond
      de aankoop of bouw van een project staan in afzonderlijke overeenkomsten en vallen
      hier niet onder. Bevestigen dat dit de bedoeling is.
    - Bevoegde rechtbank: nu ingevuld als de rechtbanken van het arrondissement
      Oost-Vlaanderen, afdeling Gent. Aanpassen indien gewenst.
    - Nazicht door jurist aanbevolen, o.a. voor de aansprakelijkheidsbeperking.
*@

<section class="legal-header">
    <div class="container">
        <h1>Algemene voorwaarden</h1>
        <p class="legal-updated">Laatst bijgewerkt op 28 augustus 2026</p>
    </div>
</section>

<div class="legal-wrap">
    <div class="container">
        <article class="legal-body">

            <p>
                Deze algemene voorwaarden regelen het gebruik van de website www.groupln.be, beheerd
                door Group LN BV. Door de website te gebruiken, ga je akkoord met deze voorwaarden.
                Ze regelen niet de contractuele afspraken rond de aankoop of realisatie van een
                project — die worden vastgelegd in afzonderlijke overeenkomsten.
            </p>

            <h2 id="identiteit">1. Wie zijn we</h2>
            <address>
                <strong>Group LN BV</strong><br />
                Klaverdries 53, 9031 Drongen, België<br />
                Ondernemingsnummer / btw: BE 0847.396.849<br />
                E-mail: <a href="mailto:info@groupln.be">info@groupln.be</a><br />
                Telefoon: <a href="tel:+3292164950">+32 (0)9 216 49 50</a>
            </address>

            <h2 id="informatie">2. Informatie op de website</h2>
            <p>
                De informatie op deze website is algemeen van aard en wordt met de nodige zorg
                samengesteld. Projectgegevens zoals prijzen, plannen, oppervlaktes, afwerkingen en
                beschikbaarheid zijn indicatief, kunnen wijzigen en gelden niet als een bindend
                aanbod. Sfeer- en 3D-beelden zijn louter illustratief. Voor concrete en actuele
                informatie over een project neem je best contact met ons op.
            </p>

            <h2 id="ip">3. Intellectuele eigendom</h2>
            <p>
                Alle inhoud op deze website — teksten, foto's, video's, plannen, grafisch materiaal,
                het logo en de vormgeving — is eigendom van Group LN BV of van haar licentiegevers en
                is beschermd door het intellectueel eigendomsrecht. Je mag deze inhoud raadplegen
                voor persoonlijk, niet-commercieel gebruik. Elke andere reproductie, verspreiding,
                wijziging of hergebruik, geheel of gedeeltelijk, vereist onze voorafgaande
                schriftelijke toestemming.
            </p>

            <h2 id="aansprakelijkheid">4. Aansprakelijkheid</h2>
            <p>
                We streven ernaar de website correct en up-to-date te houden, maar kunnen niet
                garanderen dat alle informatie volledig, juist of actueel is, of dat de website
                ononderbroken en foutloos beschikbaar is. Group LN BV is niet aansprakelijk voor
                rechtstreekse of onrechtstreekse schade die voortvloeit uit het gebruik van de
                website of uit de onbeschikbaarheid ervan, behoudens in geval van opzet of zware
                fout.
            </p>
            <p>
                De website kan links naar websites van derden bevatten. We hebben geen controle over
                die websites en zijn niet verantwoordelijk voor hun inhoud of hun privacypraktijken.
            </p>

            <h2 id="formulieren">5. Gebruik van formulieren</h2>
            <p>
                Wanneer je een formulier op de website invult, verbind je je ertoe correcte en
                volledige gegevens te verstrekken. Het is niet toegestaan de website of de
                formulieren te gebruiken voor onrechtmatige doeleinden, spam, of handelingen die de
                goede werking of de beveiliging van de website kunnen schaden.
            </p>

            <h2 id="gegevens">6. Persoonsgegevens en cookies</h2>
            <p>
                De verwerking van je persoonsgegevens is beschreven in ons
                <a href="@Url.Action("Privacybeleid", "Legal")">privacybeleid</a>. Het gebruik van
                cookies lees je in ons <a href="@Url.Action("Cookiebeleid", "Legal")">cookiebeleid</a>.
            </p>

            <h2 id="wijzigingen">7. Wijzigingen</h2>
            <p>
                We kunnen deze algemene voorwaarden aanpassen. De versie die van toepassing is, is
                die welke geldt op het moment van je gebruik van de website. De datum bovenaan deze
                pagina geeft aan wanneer de voorwaarden het laatst zijn bijgewerkt.
            </p>

            <h2 id="recht">8. Toepasselijk recht en bevoegde rechtbank</h2>
            <p>
                Op deze voorwaarden en op het gebruik van de website is het Belgisch recht van
                toepassing. Geschillen die niet in der minne kunnen worden geregeld, behoren tot de
                uitsluitende bevoegdheid van de rechtbanken van het arrondissement Oost-Vlaanderen,
                afdeling Gent.
            </p>

            <h2 id="contact">9. Contact</h2>
            <p>
                Vragen over deze voorwaarden? Mail naar
                <a href="mailto:info@groupln.be">info@groupln.be</a> of bel
                <a href="tel:+3292164950">+32 (0)9 216 49 50</a>.
            </p>

            <a class="legal-back" href="@Url.Action("Index", "Home")">&larr; Terug naar de startpagina</a>

        </article>
    </div>
</div>
