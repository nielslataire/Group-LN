@ModelType List(Of WWWCOPRO.Models.Vacatures.VacatureModel)
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/vacatures.css" />
End Section

<section class="vacatures-page-header">
    <div class="container">
        <div class="vacatures-header-inner reveal">
            <p class="vacatures-header-kicker">Werken bij Group LN</p>
            <h1 class="vacatures-header-title">Bouw mee aan de plekken waar Gent morgen woont</h1>
            <p class="vacatures-header-text">Wij zijn een klein, ambitieus team dat gelooft in verantwoordelijkheid geven vanaf dag één. Bekijk onze openstaande vacatures of maak spontaan kennis met ons.</p>
        </div>
    </div>
</section>

<section class="vac-wie-section">
    <div class="container">
        <div class="vac-wie-grid">
            <div class="vac-wie-foto-wrap reveal">
                <img class="vac-wie-foto" src="@Url.Content("~/Content/img/our-office-1.jpg")" alt="Aan het werk bij Group LN" />
            </div>
            <div class="vac-wie-content reveal reveal-slide-right">
                <p class="section-kicker">Wie wij zoeken</p>
                <h2 class="vac-wie-headline">Starters die willen bouwen, niet enkel meelopen</h2>
                <p class="vac-wie-text">Group LN is een projectontwikkelaar in Vlaanderen, maar op onze werven en op kantoor zijn we vooral een klein team dat elkaar kent. Geen grote structuren, geen anonieme trajecten — wel korte lijnen met de zaakvoerder en collega's die je vanaf je eerste dag mee laten meedenken.</p>
                <p class="vac-wie-text">We zoeken geen kant-en-klare profielen. We zoeken mensen die willen leren, die initiatief durven nemen, en die evenveel fierheid halen uit een goed afgewerkt project als wij.</p>
                <div class="vac-pills">
                    <span class="vac-pill">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                        Mentor vanaf dag 1
                    </span>
                    <span class="vac-pill">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/><polyline points="17 6 23 6 23 12"/></svg>
                        Snelle doorgroei
                    </span>
                    <span class="vac-pill">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                        Hecht team van 20+
                    </span>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="vac-why-section">
    <div class="container">
        <p class="section-kicker" style="color:var(--color-cta, #C9A96E);">Waarom hier starten</p>
        <h2 class="vac-why-headline reveal">Wat je bij ons meekrijgt dat je elders niet vindt</h2>
        <div class="vac-why-grid">
            <div class="vac-why-item reveal">
                <span class="vac-why-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                </span>
                <h3>Verantwoordelijkheid vanaf dag 1</h3>
                <p>Geen jaren wachten op een echt dossier. Je krijgt vanaf de start een eigen stuk van een project, met iemand ervaren naast je.</p>
            </div>
            <div class="vac-why-item reveal">
                <span class="vac-why-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>
                </span>
                <h3>Groeien in je eigen tempo</h3>
                <p>Een klein team betekent dat je snel meer verantwoordelijkheid opneemt zodra je er klaar voor bent — niet omdat het jaar erop staat.</p>
            </div>
            <div class="vac-why-item reveal">
                <span class="vac-why-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
                </span>
                <h3>Van dichtbij leren</h3>
                <p>Je werkt rechtstreeks samen met de zaakvoerder, architecten en aannemers — geen tussenlagen die het contact met het echte werk vertragen.</p>
            </div>
            <div class="vac-why-item reveal">
                <span class="vac-why-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                </span>
                <h3>Een team dat elkaar kent</h3>
                <p>Vrijdagmiddagborrel, teamuitjes en collega's die je bij naam kennen — geen grote onpersoonlijke organisatie.</p>
            </div>
        </div>
    </div>
</section>

<section class="vac-benefits-section">
    <div class="container">
        <p class="section-kicker">Wat wij bieden</p>
        <h2 class="vac-benefits-headline reveal">Een aanbod dat verder gaat dan je loon</h2>
        <div class="vac-benefits-grid">
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 6v12M15 9.5c0-1.4-1.3-2.5-3-2.5s-3 1.1-3 2.5 1.3 2.5 3 2.5 3 1.1 3 2.5-1.3 2.5-3 2.5-3-1.1-3-2.5"/></svg>
                </span>
                <h3>Marktconform loon &amp; voordelen</h3>
                <p>Een correct startloon, aangevuld met maaltijdcheques, netto-vergoedingen en een jaarlijkse evaluatie die ook écht iets betekent.</p>
            </div>
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                </span>
                <h3>Hospitalisatieverzekering</h3>
                <p>Van dag één gedekt, ook voor de dingen die je hoopt nooit nodig te hebben.</p>
            </div>
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="5.5" cy="17.5" r="3.5"/><circle cx="18.5" cy="17.5" r="3.5"/><path d="M15 6a1 1 0 1 0 0-2 1 1 0 0 0 0 2zM12 17.5V14l-3-3 4-3 2 3h2"/></svg>
                </span>
                <h3>Mobiliteit naar keuze</h3>
                <p>Bedrijfswagen, fietsleaseplan of mobiliteitsbudget — jij kiest wat past bij jouw functie en levensstijl.</p>
            </div>
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                </span>
                <h3>Flexibele werkuren</h3>
                <p>Glijdende uren en de mogelijkheid tot occasioneel thuiswerk voor kantoorfuncties, in overleg met je team.</p>
            </div>
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
                </span>
                <h3>Opleidingsbudget</h3>
                <p>Jaarlijks budget voor cursussen, vakbeurzen of een opleiding die jou en je vakgebied vooruit helpt.</p>
            </div>
            <div class="vac-benefit-item reveal">
                <span class="vac-benefit-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>
                </span>
                <h3>Teamevents</h3>
                <p>Van een jaarlijkse teamdag tot vrijdagmiddagborrels — we vieren graag samen wat we bouwen.</p>
            </div>
        </div>
    </div>
</section>

<section class="vac-list-section">
    <div class="container">
        <div class="vac-list-header">
            <h2 class="vac-list-headline reveal">Openstaande vacatures</h2>
            <span class="vac-list-count">@Model.Count @(If(Model.Count = 1, "openstaande vacature", "openstaande vacatures"))</span>
        </div>
        <p class="vac-list-text">Als klein team hebben we niet altijd veel vacatures open — maar wel altijd oog voor talent. Dit zijn onze actuele openingen.</p>

        @If Model.Any() Then
            @<div class="vac-cards">
                @For Each v In Model
                    @<a href="@Url.RouteUrl("VacatureDetail", New With {.slug = v.Slug})" class="vac-card reveal">
                        <div class="vac-card-top">
                            <div>
                                @If Not String.IsNullOrWhiteSpace(v.Categorie) Then
                                    @<span class="vac-card-categorie">@v.Categorie</span>
                                End If
                                <h3 class="vac-card-titel">@v.Titel</h3>
                            </div>
                        </div>
                        <div class="vac-card-meta">
                            @If Not String.IsNullOrWhiteSpace(v.Locatie) Then
                                @<span class="vac-card-meta-item">
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
                                    @v.Locatie
                                </span>
                            End If
                            @If Not String.IsNullOrWhiteSpace(v.Dienstverband) Then
                                @<span class="vac-card-meta-item">
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                                    @v.Dienstverband
                                </span>
                            End If
                        </div>
                        @If Not String.IsNullOrWhiteSpace(v.KorteBeschrijving) Then
                            @<p class="vac-card-tekst">@v.KorteBeschrijving</p>
                        End If
                        <div style="margin-top:16px;">
                            <span class="vac-card-link">
                                Bekijk vacature
                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                            </span>
                        </div>
                    </a>
                Next
            </div>
        Else
            @<div class="vac-empty">Momenteel zijn er geen openstaande vacatures. Kijk gerust later nog eens terug.</div>
        End If
    </div>
</section>

<section class="vac-cta-section">
    <div class="container reveal">
        <h2 class="vac-cta-title">Niets gevonden dat helemaal past?</h2>
        <p class="vac-cta-text">We staan altijd open voor een spontane kennismaking met mensen die graag willen bouwen — letterlijk en figuurlijk. Stuur ons je motivatie en cv.</p>
    </div>
</section>

@section scripts
    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });
    </script>
End Section
