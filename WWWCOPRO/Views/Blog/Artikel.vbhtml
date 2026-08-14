@ModelType WWWCOPRO.Models.Blog.BlogArtikelModel
@Imports System.Globalization
@Imports System.Configuration
@Code
    Dim nlBE = New CultureInfo("nl-BE")
    Dim titel As String = If(Not String.IsNullOrEmpty(Model.DetailTitel), Model.DetailTitel, Model.Titel)
    Dim metaTitelStr As String = If(Not String.IsNullOrEmpty(Model.MetaTitel), Model.MetaTitel, titel & " | Group LN")
    Dim metaDescStr As String = If(Not String.IsNullOrEmpty(Model.MetaOmschrijving), Model.MetaOmschrijving, Model.PreviewTekst)
    Dim fotoTeller As Integer = 0
    ViewData("Title") = metaTitelStr
    ViewData("MetaDescription") = metaDescStr
    ViewData("ogtitle") = metaTitelStr
    ViewData("ogtype") = "article"
    ViewData("ogdescription") = metaDescStr
    ViewData("ogimage") = If(Not String.IsNullOrEmpty(Model.FotoBestand), Model.FotoBestand, "https://www.groupln.be/Content/img/logoimg.jpg")
    ' routes.LowercaseUrls = True zorgt dat elke intern gegenereerde link naar dit artikel
    ' kleine letters gebruikt — de canonical moet dus ook op de kleine-letter-slug wijzen,
    ' anders wijkt hij af van de URL die Google via interne links/sitemap ontdekt (net het
    ' "canonical naar andere URL"-probleem dat gemeld werd voor dit exacte artikel, waarvan
    ' de Slug in de database met een hoofdletter begint).
    Dim canonicalSlug As String = If(Model.Slug, "").ToLowerInvariant()
    ViewData("ogurl") = "https://www.groupln.be/blog/" & canonicalSlug
    ViewData("canonical") = "https://www.groupln.be/blog/" & canonicalSlug
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageMeta
    <meta property="article:published_time" content="@Model.Datum.ToString("yyyy-MM-dd")" />
    <meta property="article:author" content="Group LN" />
    @If Not String.IsNullOrEmpty(Model.MetaKeywords) Then
        @<meta name="keywords" content="@Model.MetaKeywords" />
    End If
    @If Not String.IsNullOrEmpty(Model.GeoRegio) Then
        @<meta name="geo.region" content="@Model.GeoRegio" />
    End If
    @If Not String.IsNullOrEmpty(Model.GeoPlaatsnaam) Then
        @<meta name="geo.placename" content="@Model.GeoPlaatsnaam" />
    End If
    @If Not String.IsNullOrEmpty(Model.GeoPositie) Then
        @<text>
            <meta name="geo.position" content="@Model.GeoPositie" />
            <meta name="ICBM" content="@Model.GeoPositie.Replace(";", ", ")" />
        </text>
    End If
End Section

@section PageStyle
    <link rel="stylesheet" href="~/Content/blog-index.css" />
    <link rel="stylesheet" href="~/Content/blog-artikel.css" />
End Section

@* ── Voorvertoning banner ── *@
@If ViewData("IsVoorvertoning") IsNot Nothing Then
    @<div style="background:#fef3c7;border-bottom:2px solid #d97706;padding:12px 0;position:sticky;top:0;z-index:9999;">
        <div class="container">
            <div style="display:flex;align-items:center;gap:10px;font-size:13px;font-weight:600;color:#92400e;">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="flex-shrink:0;"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                VOORVERTONING &mdash; Dit artikel is nog niet gepubliceerd. Enkel zichtbaar via deze beveiligde link.
            </div>
        </div>
    </div>
End If

@* ── Header: breadcrumbs + titel + meta ── *@
<section class="artikel-header">
    <div class="container reveal">
        <ul class="breadcrumb">
            <li><a href="@Url.Action("Index", "Home")">Home</a></li>
            <li><a href="@Url.RouteUrl("Blog")">Blog</a></li>
            <li class="active">@Model.Titel</li>
        </ul>
        <h1 class="artikel-titel">@titel</h1>
        <div class="artikel-meta">
            <span>Gepubliceerd op @Model.Datum.ToString("d MMMM yyyy", nlBE)</span>
            @If Model.LeestijdMinuten > 0 Then
                @<text>
                    <span class="artikel-meta-sep">&middot;</span>
                    <span>@Model.LeestijdMinuten min. leestijd</span>
                </text>
            End If
        </div>
    </div>
</section>

<div class="container artikel-container">

    @* ── Hero foto ── *@
    @If Not String.IsNullOrEmpty(Model.FotoBestand) Then
        @<div class="artikel-hero-foto reveal">
            <img src="@Model.FotoBestand" alt="@titel" />
        </div>
    End If

    @* ── Twee-kolom layout ── *@
    <div class="artikel-body">

        @* Links: artikel inhoud *@
        <article class="artikel-inhoud">

            @* Intro tekst (DetailTitelTekst) *@
            @If Not String.IsNullOrEmpty(Model.DetailTitelTekst) Then
                @<p class="artikel-intro reveal">@Model.DetailTitelTekst</p>
            End If

            @* Inhoudsblokken *@
            @For Each blok In Model.Blokken
                @<div class="artikel-blok reveal">
                    @If blok.BlokType = "quote" Then
                        @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                            @<div class="artikel-pullquote">@Html.Raw(blok.RijkeTekst)</div>
                        End If
                    ElseIf blok.BlokType = "tip" Then
                        @<div class="artikel-callout">
                            @If Not String.IsNullOrEmpty(blok.Titel) Then
                                @<h4>@blok.Titel</h4>
                            End If
                            @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                                @<div class="artikel-blok-tekst">@Html.Raw(blok.RijkeTekst)</div>
                            End If
                        </div>
                    ElseIf Not String.IsNullOrEmpty(blok.FotoBestand) Then
                        fotoTeller = fotoTeller + 1
                        @If fotoTeller Mod 2 = 1 Then
                            @<text>
                                <div class="artikel-blok-split">
                                    <div class="artikel-blok-split-foto">
                                        <img src="@blok.FotoBestand" alt="@(If(Not String.IsNullOrEmpty(blok.Titel), blok.Titel, titel))" />
                                    </div>
                                    <div class="artikel-blok-split-body">
                                        @If Not String.IsNullOrEmpty(blok.Titel) Then
                                            @<h2 class="artikel-blok-split-titel">@blok.Titel</h2>
                                        End If
                                        @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                                            @<div class="artikel-blok-tekst">@Html.Raw(blok.RijkeTekst)</div>
                                        End If
                                    </div>
                                </div>
                            </text>
                        Else
                            @<text>
                                <div class="artikel-blok-split">
                                    <div class="artikel-blok-split-body">
                                        @If Not String.IsNullOrEmpty(blok.Titel) Then
                                            @<h2 class="artikel-blok-split-titel">@blok.Titel</h2>
                                        End If
                                        @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                                            @<div class="artikel-blok-tekst">@Html.Raw(blok.RijkeTekst)</div>
                                        End If
                                    </div>
                                    <div class="artikel-blok-split-foto">
                                        <img src="@blok.FotoBestand" alt="@(If(Not String.IsNullOrEmpty(blok.Titel), blok.Titel, titel))" />
                                    </div>
                                </div>
                            </text>
                        End If
                    Else
                        @If Not String.IsNullOrEmpty(blok.Titel) Then
                            @<text>
                                <h2 class="artikel-blok-titel">@blok.Titel</h2>
                                <hr class="artikel-blok-lijn" />
                            </text>
                        End If
                        @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                            @<div class="artikel-blok-tekst">@Html.Raw(blok.RijkeTekst)</div>
                        End If
                    End If
                </div>
            Next

            @* ── FAQ ── *@
            @If Model.FaqItems.Count > 0 Then
                @<section class="artikel-faq reveal">
                    <h2 class="artikel-faq-titel">Veelgestelde vragen</h2>
                    <hr class="artikel-blok-lijn" />
                    <dl class="artikel-faq-lijst" id="artikelFaqLijst">
                        @For i As Integer = 0 To Model.FaqItems.Count - 1
                            Dim faq = Model.FaqItems(i)
                            Dim panelId = "faq-antwoord-" & faq.ID.ToString()
                            @<div class="artikel-faq-item">
                                <dt class="artikel-faq-vraag" aria-expanded="false" aria-controls="@panelId">
                                    <span>@faq.Vraag</span>
                                    <button class="artikel-faq-btn" type="button" tabindex="-1" aria-hidden="true">
                                        <svg class="faq-icoon-plus" width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="7" y1="1" x2="7" y2="13"/><line x1="1" y1="7" x2="13" y2="7"/></svg>
                                        <svg class="faq-icoon-min" width="14" height="2" viewBox="0 0 14 2" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="1" y1="1" x2="13" y2="1"/></svg>
                                    </button>
                                </dt>
                                <dd class="artikel-faq-antwoord" id="@panelId">
                                    <div class="artikel-faq-antwoord-inner">@faq.Antwoord</div>
                                </dd>
                            </div>
                        Next
                    </dl>
                </section>
            End If

            <div class="artikel-footer">
                <a href="@Url.RouteUrl("Blog")" class="artikel-terug">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/></svg>
                    Terug naar alle artikels
                </a>
                <div class="artikel-delen">
                    <span class="artikel-delen-label">Delen</span>
                    <a href="https://www.facebook.com/sharer/sharer.php?u=https://www.groupln.be/Blog/@Model.Slug" target="_blank" rel="noopener" class="artikel-deel-btn" title="Delen via Facebook" aria-label="Delen via Facebook">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"/></svg>
                    </a>
                    <a href="https://www.linkedin.com/sharing/share-offsite/?url=https://www.groupln.be/Blog/@Model.Slug" target="_blank" rel="noopener" class="artikel-deel-btn" title="Delen via LinkedIn" aria-label="Delen via LinkedIn">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z"/><rect x="2" y="9" width="4" height="12"/><circle cx="4" cy="4" r="2"/></svg>
                    </a>
                    <a href="https://wa.me/?text=https://www.groupln.be/Blog/@Model.Slug" target="_blank" rel="noopener" class="artikel-deel-btn" title="Delen via WhatsApp" aria-label="Delen via WhatsApp">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z"/><path d="M12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.123 1.532 5.852L0 24l6.338-1.51A11.954 11.954 0 0 0 12 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.807 9.807 0 0 1-5.031-1.388l-.36-.214-3.732.889.934-3.62-.235-.372A9.808 9.808 0 0 1 2.182 12C2.182 6.57 6.57 2.182 12 2.182S21.818 6.57 21.818 12 17.43 21.818 12 21.818z"/></svg>
                    </a>
                    <a href="mailto:?subject=@Uri.EscapeDataString(titel)&body=https://www.groupln.be/Blog/@Model.Slug" class="artikel-deel-btn" title="Delen via e-mail" aria-label="Delen via e-mail">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>
                    </a>
                    <button type="button" class="artikel-deel-btn" id="btnKopieer" title="Kopieer link" aria-label="Kopieer link">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
                    </button>
                </div>
            </div>

        </article>

        @* Rechts: sticky contactformulier *@
        <aside class="artikel-sidebar">
            <div class="artikel-contact-blok reveal" id="contactBlok">
                <div class="artikel-contact-header">
                    <p class="artikel-contact-titel">Interesse<br />of een vraag?</p>
                    <p class="artikel-contact-sub">Laat je gegevens achter en we contacteren je snel — vrijblijvend.</p>
                </div>
                <form id="blogContactForm" class="artikel-contact-form" novalidate>
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="artikelTitel" value="@Model.Titel" />
                    <div style="position:absolute;left:-9999px;top:-9999px;opacity:0;pointer-events:none;" aria-hidden="true">
                        <input type="text" name="website_url" id="bc_hp" tabindex="-1" autocomplete="off" value="" />
                    </div>
                    <div class="artikel-contact-veld">
                        <label for="bc_naam">NAAM <span class="req">*</span></label>
                        <input type="text" id="bc_naam" name="naam" autocomplete="family-name" placeholder="Janssen" />
                    </div>
                    <div class="artikel-contact-veld">
                        <label for="bc_voornaam">VOORNAAM</label>
                        <input type="text" id="bc_voornaam" name="voornaam" autocomplete="given-name" placeholder="Jan" />
                    </div>
                    <div class="artikel-contact-veld">
                        <label for="bc_email">E-MAILADRES <span class="req">*</span></label>
                        <input type="email" id="bc_email" name="email" autocomplete="email" placeholder="jan@email.be" />
                    </div>
                    <div class="artikel-contact-veld">
                        <label for="bc_tel">TELEFOONNUMMER</label>
                        <input type="tel" id="bc_tel" name="telefoon" autocomplete="tel" placeholder="+32 4xx xx xx xx" />
                    </div>
                    <div class="artikel-contact-veld">
                        <label for="bc_bericht">BERICHT <span class="req">*</span></label>
                        <textarea id="bc_bericht" name="bericht" rows="4" placeholder="Je vraag of opmerking..."></textarea>
                    </div>
                    @*<div class="artikel-contact-check">
                        <input type="checkbox" id="bc_nieuwsbrief" name="nieuwsbrief" value="1" />
                        <label for="bc_nieuwsbrief" style="font-size:13px;letter-spacing:0;text-transform:none;color:var(--tekst-sub);margin:0;">Ja, ik schrijf me in voor de nieuwsbrief.</label>
                    </div>*@
                    <button type="submit" class="artikel-contact-btn" id="bcSubmit">VERZENDEN &rarr;</button>
                    <div class="artikel-contact-spinner hidden" id="bcSpinner">
                        <i class="fa fa-spinner fa-spin"></i> Wordt verzonden&hellip;
                    </div>
                </form>
                <div class="artikel-contact-direct">
                    <div class="artikel-contact-direct-titel">LIEVER DIRECT CONTACT?</div>
                    <a href="tel:+3292164950" class="artikel-contact-direct-item">
                        <span class="artikel-contact-direct-icon">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.61 3.4 2 2 0 0 1 3.6 1.21h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L7.91 8.81a16 16 0 0 0 6 6l.91-.91a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 21.73 16z"/></svg>
                        </span>
                        <span>
                            <strong>+32 (0)9 216 49 50</strong>
                            <small>Bel ons direct</small>
                        </span>
                    </a>
                    <a href="mailto:info@groupln.be" class="artikel-contact-direct-item">
                        <span class="artikel-contact-direct-icon">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>
                        </span>
                        <span>
                            <strong>info@groupln.be</strong>
                            <small>Stuur ons een mail</small>
                        </span>
                    </a>
                </div>
            </div>
        </aside>

    </div>

    @* ── Ontdek meer ── *@
    @If Model.OntdekMeer.Count > 0 Then
        @<section class="ontdek-meer">
            <div class="sectie-kop">Ontdek meer</div>
            <div class="blog-grid">
                @For Each item In Model.OntdekMeer
                    Dim itemHref As String = String.Empty
                    If item.ItemType = "artikel" Then
                        itemHref = Url.RouteUrl("BlogArtikel", New With {.slug = item.Slug})
                    ElseIf Not String.IsNullOrEmpty(item.Slug) Then
                        itemHref = Url.RouteUrl("ProjectBySlug", New With {.slug = item.Slug})
                    Else
                        itemHref = Url.RouteUrl("ProjectById")
                    End If
                    Dim slaapLabel As String = Nothing
                    Dim eenhedenMeta As String = Nothing
                    Dim prijsLabel As String = Nothing
                    If item.ItemType = "project" Then
                        If item.MinSlaapkamers.HasValue AndAlso item.MaxSlaapkamers.HasValue Then
                            If item.MinSlaapkamers.Value = item.MaxSlaapkamers.Value Then
                                slaapLabel = item.MinSlaapkamers.Value.ToString() & " slaapkamer" & If(item.MinSlaapkamers.Value = 1, "", "s")
                            Else
                                slaapLabel = item.MinSlaapkamers.Value.ToString() & ChrW(8211) & item.MaxSlaapkamers.Value.ToString() & " slaapkamers"
                            End If
                        ElseIf item.MinSlaapkamers.HasValue Then
                            slaapLabel = item.MinSlaapkamers.Value.ToString() & " slaapkamer" & If(item.MinSlaapkamers.Value = 1, "", "s")
                        End If
                        If Not String.IsNullOrEmpty(slaapLabel) AndAlso Not String.IsNullOrEmpty(item.AantalEenheden) Then
                            eenhedenMeta = slaapLabel & " · " & item.AantalEenheden
                        ElseIf Not String.IsNullOrEmpty(slaapLabel) Then
                            eenhedenMeta = slaapLabel
                        ElseIf Not String.IsNullOrEmpty(item.AantalEenheden) Then
                            eenhedenMeta = item.AantalEenheden
                        End If
                        If item.VanafPrijs.HasValue Then
                            prijsLabel = "Vanaf € " & item.VanafPrijs.Value.ToString("N0", New System.Globalization.CultureInfo("nl-BE")) & "<small>" & If(item.IsCasco, " casco", " incl. afwerking") & "</small>"
                        End If
                    End If
                    @<a href="@itemHref" class="blog-kaart reveal">
                        <div class="blog-kaart-foto">
                            @If item.IsVideo AndAlso Not String.IsNullOrEmpty(item.VideoUrl) Then
                                @<video class="blog-kaart-video" autoplay="autoplay" muted="muted" loop="loop" playsinline="playsinline">
                                    <source src="@item.VideoUrl" type="video/mp4" />
                                </video>
                            ElseIf Not String.IsNullOrEmpty(item.FotoUrl) Then
                                @<img src="@item.FotoUrl" alt="@item.Titel" loading="lazy" />
                            Else
                                @<div class="blog-kaart-foto-placeholder"></div>
                            End If
                            @If item.ItemType = "artikel" Then
                                @<span class="ontdek-badge ontdek-badge--artikel">ARTIKEL</span>
                            Else
                                @<span class="ontdek-badge ontdek-badge--project">PAND &middot; PROJECT</span>
                            End If
                        </div>
                        <div class="blog-kaart-body">
                            @If item.ItemType = "artikel" Then
                                @<h3 class="blog-kaart-naam">@item.Titel</h3>
                                @If item.Datum.HasValue Then
                                    @<div class="blog-kaart-datum">@item.Datum.Value.ToString("d MMMM yyyy", New System.Globalization.CultureInfo("nl-BE"))</div>
                                End If
                                @If Not String.IsNullOrEmpty(item.PreviewTekst) Then
                                    @<p class="blog-kaart-tekst">@item.PreviewTekst</p>
                                End If
                            Else
                                @If Not String.IsNullOrEmpty(item.Street) Then
                                    @<div class="blog-kaart-datum">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
                                        @item.Street
                                    </div>
                                End If
                                @<h3 class="blog-kaart-naam">@item.Titel</h3>
                                @If Not String.IsNullOrEmpty(eenhedenMeta) Then
                                    @<div class="blog-kaart-datum">@eenhedenMeta</div>
                                End If
                                @If Not String.IsNullOrEmpty(prijsLabel) Then
                                    @<p class="ontdek-kaart-tekst">@Html.Raw(prijsLabel)</p>
                                End If
                            End If
                        </div>
                        <div class="blog-kaart-footer">
                            <span class="blog-kaart-link">
                                @If item.ItemType = "artikel" Then @<text>Lees artikel</text> Else @<text>Ontdek project</text> End If
                                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12" /><polyline points="12 5 19 12 12 19" /></svg>
                            </span>
                        </div>
                    </a>
                Next
            </div>
        </section>
    End If

</div>

@section scripts
    <script src="https://www.google.com/recaptcha/api.js?render=@ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")"></script>
    <script>
        (function () {
            var form = document.getElementById('blogContactForm');
            if (!form) return;

            var recaptchaSiteKey = '@ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")';

            form.addEventListener('submit', function (e) {
                e.preventDefault();

                if (document.getElementById('bc_hp').value !== '') return;

                var naam    = document.getElementById('bc_naam').value.trim();
                var email   = document.getElementById('bc_email').value.trim();
                var bericht = document.getElementById('bc_bericht').value.trim();

                if (!naam || !email || !bericht) {
                    alert('Vul alle verplichte velden in (Naam, E-mailadres en Bericht).');
                    return;
                }

                var voornaam = document.getElementById('bc_voornaam').value.trim();
                var telefoon = document.getElementById('bc_tel').value.trim();
                var honeypot = document.getElementById('bc_hp').value;
                var artikelTitel = form.querySelector('input[name="artikelTitel"]').value;
                var token = form.querySelector('input[name="__RequestVerificationToken"]').value;

                document.getElementById('bcSubmit').classList.add('hidden');
                document.getElementById('bcSpinner').classList.remove('hidden');

                function verstuur(captchaToken) {
                    $.ajax({
                        url: '@Url.Action("Send", "Contact")',
                        type: 'POST',
                        data: {
                            Voornaam: voornaam || naam,
                            Achternaam: naam,
                            EmailTo: email,
                            Phone: telefoon,
                            Title: 'Vraag via blog: ' + artikelTitel,
                            Message: bericht,
                            PrivacyAkkoord: true,
                            website_url: honeypot,
                            'g-recaptcha-response': captchaToken || '',
                            __RequestVerificationToken: token
                        },
                        complete: function () {
                            var f = document.getElementById('blogContactForm');
                            if (f) {
                                f.outerHTML =
                                    '<div class="artikel-contact-succes">' +
                                    '<div class="artikel-contact-succes-icon"><i class="fa fa-check-circle"></i></div>' +
                                    '<strong>Bedankt voor je bericht!</strong><br>We contacteren je zo snel mogelijk.' +
                                    '</div>';
                            }
                        }
                    });
                }

                if (recaptchaSiteKey && typeof grecaptcha !== 'undefined') {
                    grecaptcha.ready(function () {
                        grecaptcha.execute(recaptchaSiteKey, { action: 'contact' }).then(verstuur, function () { verstuur(''); });
                    });
                } else {
                    verstuur('');
                }
            });
        })();

        (function () {
            var lijst = document.getElementById('artikelFaqLijst');
            if (!lijst) return;

            var items = lijst.querySelectorAll('.artikel-faq-item');

            function openItem(item) {
                var antwoord = item.querySelector('.artikel-faq-antwoord');
                var vraag    = item.querySelector('.artikel-faq-vraag');
                item.classList.add('is-open');
                vraag.setAttribute('aria-expanded', 'true');
                antwoord.style.maxHeight = antwoord.scrollHeight + 'px';
            }

            function sluitItem(item) {
                var antwoord = item.querySelector('.artikel-faq-antwoord');
                var vraag    = item.querySelector('.artikel-faq-vraag');
                item.classList.remove('is-open');
                vraag.setAttribute('aria-expanded', 'false');
                antwoord.style.maxHeight = '0';
            }

            items.forEach(function (item) {
                var vraag = item.querySelector('.artikel-faq-vraag');

                vraag.addEventListener('click', function () {
                    var isOpen = item.classList.contains('is-open');
                    // Sluit alle items
                    items.forEach(function (other) { sluitItem(other); });
                    // Open dit item als het gesloten was
                    if (!isOpen) openItem(item);
                });
            });
        })();

        (function () {
            var btn = document.getElementById('btnKopieer');
            if (!btn) return;
            btn.addEventListener('click', function () {
                var url = window.location.href;
                if (navigator.clipboard && window.isSecureContext) {
                    navigator.clipboard.writeText(url).then(function () { toonGekopieerd(btn); });
                } else {
                    var ta = document.createElement('textarea');
                    ta.value = url;
                    ta.style.position = 'fixed';
                    ta.style.opacity = '0';
                    document.body.appendChild(ta);
                    ta.select();
                    try { document.execCommand('copy'); } catch (e) {}
                    document.body.removeChild(ta);
                    toonGekopieerd(btn);
                }
            });
            function toonGekopieerd(btn) {
                btn.classList.add('gekopieerd');
                btn.title = 'Gekopieerd!';
                setTimeout(function () {
                    btn.classList.remove('gekopieerd');
                    btn.title = 'Kopieer link';
                }, 2000);
            }
        })();
    </script>
End Section
