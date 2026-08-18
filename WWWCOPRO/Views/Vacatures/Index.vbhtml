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
            <h1 class="vacatures-header-title">Bouw mee aan de plekken waar Vlaanderen morgen woont</h1>
            <p class="vacatures-header-text">Wij zijn een familiebedrijf uit Gent (Drongen) dat gelooft in verantwoordelijkheid geven vanaf dag één. Bekijk onze openstaande vacatures of maak spontaan kennis met ons.</p>
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
                <h2 class="vac-wie-headline">Mensen die willen bouwen, en meteen mee vooruit denken</h2>
                <p class="vac-wie-text">Group LN is een familiebedrijf: de zaakvoerders staan zelf op de werf en op kantoor, en kennen elk project en elke collega persoonlijk. Die korte lijnen zorgen ervoor dat je rechtstreeks contact hebt met de zaakvoerders en vanaf je eerste dag mee mag nadenken over de projecten waar je aan werkt.</p>
                <p class="vac-wie-text">Of je nu net start of al jaren ervaring hebt: we zoeken mensen die willen leren, die initiatief durven nemen, en die evenveel fierheid halen uit een goed afgewerkt project als wij.</p>
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
                <h3>Verantwoordelijkheid vanaf dag één</h3>
                <p>een jaren wachten op een echt dossier. Je krijgt van bij de start een eigen project, met de zaakvoerders binnen handbereik voor overleg.</p>
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
                <p>Je werkt rechtstreeks samen met de zaakvoerders, architecten en aannemers — dezelfde mensen die al 27 jaar het volledige traject van ontwerp tot oplevering opvolgen.</p>
            </div>
            <div class="vac-why-item reveal">
                <span class="vac-why-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                </span>
                <h3>Een team dat elkaar kent</h3>
                <p>Klein team, korte lijnen: je kent iedereen bij naam en weet waar elk project staat.</p>
            </div>
        </div>
    </div>
</section>

<section class="vac-list-section">
    <div class="container">
        <div class="vac-list-header">
            <h2 class="vac-list-headline reveal">Openstaande vacatures</h2>
            @If Model.Count > 1 Then
                @<span class="vac-list-count">@Model.Count openstaande vacatures</span>
            End If
        </div>
        <p class="vac-list-text">Als klein team hebben we niet altijd veel vacatures open — maar wel altijd oog voor talent. Dit zijn onze actuele openingen.</p>

        @If Model.Any() Then
            @<div class="vac-cards">
                @For Each v In Model
                    @<a href="@Url.RouteUrl("VacatureDetail", New With {.slug = v.Slug})" class="vac-card reveal">
                        <div class="vac-card-body">
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
                        </div>
                        <span class="vac-card-link">
                            Bekijk vacature
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                        </span>
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
