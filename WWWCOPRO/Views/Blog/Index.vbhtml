@ModelType List(Of WWWCOPRO.Models.Blog.BlogArtikelModel)
@Imports System.Globalization
@Code
    Dim pageTitle As String = "Nieuws & inspiratie over nieuwbouw in Gent | Group LN"
    Dim pageDesc As String = "Lees het laatste nieuws en inspiratie over nieuwbouw en projectontwikkeling in Gent, Drongen en Oost-Vlaanderen. Group LN deelt marktinzichten, architectuurtrends en tips voor investeren in vastgoed."
    ViewData("Title") = pageTitle
    ViewData("MetaDescription") = pageDesc
    ViewData("ogtitle") = pageTitle
    ViewData("ogtype") = "website"
    ViewData("ogdescription") = pageDesc
    ViewData("ogimage") = "https://www.groupln.be/Content/img/logoimg.jpg"
    ViewData("ogurl") = "https://www.groupln.be/Blog"
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim nlBE = New CultureInfo("nl-BE")
    Dim imgBase As String = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")
    Dim sentenceCase As Func(Of String, String) = Function(s) If(String.IsNullOrEmpty(s), s, s.Substring(0, 1).ToUpper() & s.Substring(1).ToLower())
    Dim uitgelicht = If(Model.Any(), Model.First(), Nothing)
    Dim overige = If(Model.Count > 1, Model.Skip(1).ToList(), New List(Of WWWCOPRO.Models.Blog.BlogArtikelModel)())
End Code

@section PageMeta
    <link rel="canonical" href="https://www.groupln.be/Blog" />
    <meta name="geo.region" content="BE-VOV" />
    <meta name="geo.placename" content="Drongen, Gent, Oost-Vlaanderen" />
    <meta name="geo.position" content="51.0682;3.6566" />
    <meta name="ICBM" content="51.0682, 3.6566" />
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="Nieuws & inspiratie over nieuwbouw in Gent | Group LN" />
    <meta name="twitter:description" content="Lees het laatste nieuws en inspiratie over nieuwbouw en projectontwikkeling in Gent, Drongen en Oost-Vlaanderen." />
    <meta name="twitter:image" content="https://www.groupln.be/Content/img/logoimg.jpg" />
End Section

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
                        @<img src="@(imgBase & "blog/" & uitgelicht.FotoBestand)" alt="@uitgelicht.Titel" />
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
                                    @<img src="@(imgBase & "blog/" & artikel.FotoBestand)" alt="@artikel.Titel" />
                                Else
                                    @<div class="blog-kaart-foto-placeholder"></div>
                                End If
                            </div>
                            <div class="blog-kaart-body">
                                <h3 class="blog-kaart-naam">@sentenceCase(artikel.Titel)</h3>
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
    <script type="application/ld+json">
    {
      "@@context": "https://schema.org",
      "@@graph": [
        {
          "@@type": "BreadcrumbList",
          "itemListElement": [
            {
              "@@type": "ListItem",
              "position": 1,
              "name": "Home",
              "item": "https://www.groupln.be/"
            },
            {
              "@@type": "ListItem",
              "position": 2,
              "name": "Blog",
              "item": "https://www.groupln.be/Blog"
            }
          ]
        },
        {
          "@@type": "Blog",
          "name": "Nieuws & inspiratie | Group LN",
          "description": "Marktinzichten, architectuurtrends, duurzaam bouwen, financieringstips en updates over nieuwbouwprojecten in Gent en Oost-Vlaanderen.",
          "url": "https://www.groupln.be/Blog",
          "inLanguage": "nl-BE",
          "publisher": {
            "@@type": "Organization",
            "name": "Group LN",
            "url": "https://www.groupln.be",
            "logo": {
              "@@type": "ImageObject",
              "url": "https://www.groupln.be/Content/img/logoimg.jpg"
            },
            "address": {
              "@@type": "PostalAddress",
              "streetAddress": "Klaverdries 53",
              "addressLocality": "Drongen",
              "postalCode": "9031",
              "addressCountry": "BE"
            },
            "telephone": "+3292164950",
            "email": "info@@groupln.be",
            "sameAs": [
              "https://www.facebook.com/GROUPLN",
              "https://www.linkedin.com/company/group-ln",
              "https://www.instagram.com/group.ln/"
            ]
          }
        }
      ]
    }
    </script>
End section

