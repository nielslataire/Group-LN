@ModelType List(Of WWWCOPRO.Models.Blog.BlogArtikelModel)
@Imports System.Globalization
@Code
    ViewData("Title") = "Nieuws & inspiratie | Group LN"
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim nlBE = New CultureInfo("nl-BE")
    Dim uitgelicht = If(Model.Any(), Model.First(), Nothing)
    Dim overige = If(Model.Count > 1, Model.Skip(1).ToList(), New List(Of WWWCOPRO.Models.Blog.BlogArtikelModel)())
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/blog-index.css" />
End Section

@* ── Paginakoptekst ── *@
<section class="blog-page-header">
    <div class="container">
        <ul class="breadcrumb">
            <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
            <li class="active">Blog</li>
        </ul>
        <h1>Nieuws &amp; inspiratie</h1>
        <p class="page-subtitle">Vastgoed is meer dan bouwen alleen. Op onze blog delen we onze kennis en ervaring over nieuwbouw, projectontwikkeling en investeren in vastgoed. Ontdek marktinzichten, architectuurtrends, duurzaam bouwen, financieringstips en updates over onze lopende en toekomstige projecten.</p>
    </div>
</section>

<div class="container" style="padding-top: 48px; padding-bottom: 64px;">

    @If Not Model.Any() Then
        @<div class="blog-leeg"><p>Er zijn momenteel geen artikelen beschikbaar.</p></div>
    Else

        @* ── Uitgelicht artikel (meest recente) ── *@
        @<text>
            <div class="sectie-kop">Uitgelicht artikel</div>
            <a href="@Url.RouteUrl("BlogArtikel", New With {.slug = uitgelicht.Slug})" class="blog-uitgelicht">
                <div class="blog-uitgelicht-foto">
                    @If Not String.IsNullOrEmpty(uitgelicht.FotoBestand) Then
                        @<img src="@uitgelicht.FotoBestand" alt="@uitgelicht.Titel" />
                    Else
                        @<div class="blog-uitgelicht-foto-placeholder"></div>
                    End If
                </div>
                <div class="blog-uitgelicht-info">
                    <div class="blog-uitgelicht-label">Uitgelicht artikel</div>
                    <h2 class="blog-uitgelicht-naam">@uitgelicht.Titel</h2>
                    <div class="blog-uitgelicht-meta">
                        @uitgelicht.Datum.ToString("d MMMM yyyy", nlBE)
                        @If uitgelicht.LeestijdMinuten > 0 Then
                            @<text> &middot; @uitgelicht.LeestijdMinuten min. leestijd</text>
                        End If
                    </div>
                    @If Not String.IsNullOrEmpty(uitgelicht.PreviewTekst) Then
                        @<p class="blog-uitgelicht-tekst">@uitgelicht.PreviewTekst</p>
                    End If
                    <span class="blog-uitgelicht-cta">
                        Lees artikel
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                    </span>
                </div>
            </a>
        </text>

        @* ── Alle artikels ── *@
        @If overige.Any() Then
            @<text>
                <div class="sectie-kop">Alle artikels</div>
                <div class="blog-grid">
                    @For Each artikel In overige
                        @<a href="@Url.RouteUrl("BlogArtikel", New With {.slug = artikel.Slug})" class="blog-kaart">
                            <div class="blog-kaart-foto">
                                @If Not String.IsNullOrEmpty(artikel.FotoBestand) Then
                                    @<img src="@artikel.FotoBestand" alt="@artikel.Titel" />
                                Else
                                    @<div class="blog-kaart-foto-placeholder"></div>
                                End If
                            </div>
                            <div class="blog-kaart-body">
                                <h3 class="blog-kaart-naam">@artikel.Titel</h3>
                                <div class="blog-kaart-datum">@artikel.Datum.ToString("d MMMM yyyy", nlBE)</div>
                                @If Not String.IsNullOrEmpty(artikel.PreviewTekst) Then
                                    @<p class="blog-kaart-tekst">@artikel.PreviewTekst</p>
                                End If
                            </div>
                            <div class="blog-kaart-footer">
                                <span class="blog-kaart-link">
                                    Lees verder
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                                </span>
                            </div>
                        </a>
                    Next
                </div>
            </text>
        End If

    End If

</div>

@section scripts
    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });
    </script>
End section

