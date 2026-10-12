@Code
    Dim _stickyStartAt As Integer = If(ViewData("StickyStartAt") IsNot Nothing, CInt(ViewData("StickyStartAt")), 175)
End Code
<!DOCTYPE html>
<html lang="nl">
<head>
    @* Google Consent Mode v2 — bewust 1-op-1 gekopieerd uit Views/Shared/_Layout.vbhtml, niet
       gerefactored naar één gedeeld bestand, om de live layout niet aan te raken. Als dit blok
       daar wijzigt, hier ook bijwerken. *@
    <script>
        window.dataLayer = window.dataLayer || [];
        function gtag() { dataLayer.push(arguments); }
        gtag('consent', 'default', {
            ad_storage: 'denied',
            ad_user_data: 'denied',
            ad_personalization: 'denied',
            analytics_storage: 'denied',
            functionality_storage: 'granted',
            security_storage: 'granted',
            wait_for_update: 500
        });
        gtag('set', 'ads_data_redaction', true);
        window.grouplnLoadGTM = function () {
            if (window.__grouplnGtmLoaded) { return; }
            window.__grouplnGtmLoaded = true;
            (function (w, d, s, l, i) {
                w[l] = w[l] || []; w[l].push({ 'gtm.start': new Date().getTime(), event: 'gtm.js' });
                var f = d.getElementsByTagName(s)[0], j = d.createElement(s),
                    dl = l != 'dataLayer' ? '&l=' + l : '';
                j.async = true; j.src = 'https://www.googletagmanager.com/gtm.js?id=' + i + dl;
                f.parentNode.insertBefore(j, f);
            })(window, document, 'script', 'dataLayer', 'GTM-KG5HPVWR');
        };
    </script>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>@ViewData("Title")</title>
    <link rel="icon" href="@Url.Content("~/content/img/favicon.ico")" type="image/x-icon" />
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;500;600&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">
    <link rel="stylesheet" href="@Url.Content("~/Content/v2/tokens.css")" />
    <link rel="stylesheet" href="@Url.Content("~/Content/v2/layout.css")" />
    <link rel="stylesheet" href="@Url.Content("~/Content/cookie-consent.css")" />
    @RenderSection("PageStyle", required:=False)
    @* Cookie-consent BANNER MARKUP is bewust nog niet overgezet (cookie-consent.js no-opt zonder
       #ccConsent/#ccFab in de DOM, zie de guard op regel 23 van dat bestand) — geen consent-UI
       hier is prima zolang dit niet live staat, maar dit MOET erbij vóór de eerste echte V2-pagina
       publiek gaat. Zie migratielog in WWWCOPRO/design-grouplnwww.md. *@
</head>
<body class="wwwcopro-v2">
    <a class="v2-skip-link" href="#v2Main">Naar de inhoud</a>

    <header class="v2-header" data-sticky-start-at="@_stickyStartAt">
        <div class="v2-header-row">
            <div class="v2-header-left">
                <a href="@Url.Action("Index", "Home", New With {.area = ""})" class="v2-logo">
                    <img src="@Url.Content("~/Content/img/logo.png")" alt="Group LN">
                    <span class="v2-logo-divider"></span>
                    <span class="v2-logo-text">
                        <span class="v2-logo-name">Group LN</span>
                        <span class="v2-logo-sub">Projectontwikkeling</span>
                    </span>
                </a>
                <a href="tel:+3292164950" class="v2-phone">
                    <span class="v2-phone-icon"><i class="fa fa-phone" aria-hidden="true"></i></span>
                    <span class="v2-phone-text">
                        <span class="v2-phone-label">Bel ons</span>
                        <span class="v2-phone-number">+32 (0)9 216 49 50</span>
                    </span>
                </a>
            </div>
            <div class="v2-header-right">
                <nav class="v2-nav">
                    <a href="@Url.Action("Index", "Projects", New With {.area = ""})">Te koop</a>
                    <a href="@Url.Action("Index", "AboutUs", New With {.area = ""})">Over ons</a>
                    <a href="@Url.RouteUrl("Vacatures")">Vacatures</a>
                    <a href="@Url.Action("Index", "Contact", New With {.area = ""})">Contact</a>
                </nav>
                <button type="button" id="v2NavToggle" class="v2-menu-btn" aria-label="Menu openen" aria-expanded="false" aria-controls="v2NavOverlay">
                    <span class="v2-menu-btn-lines" aria-hidden="true"><span></span><span></span><span></span></span>
                </button>
            </div>
        </div>
    </header>

    <div id="v2NavBackdrop" class="v2-overlay-backdrop"></div>
    <div id="v2NavOverlay" class="v2-overlay v2-shadow-overlay" aria-hidden="true" role="dialog" aria-modal="true" aria-label="Menu">
        <button type="button" id="v2NavClose" class="v2-menu-btn" aria-label="Menu sluiten" style="align-self:flex-end;margin-bottom:12px">&times;</button>
        <nav class="v2-overlay-nav">
            <a href="@Url.Action("Index", "Home", New With {.area = ""})" aria-current="page">Home</a>
            <a href="@Url.Action("Index", "Projects", New With {.area = ""})">Te koop</a>
            <a href="@Url.Action("Index", "AboutUs", New With {.area = ""})">Over ons</a>
            <a href="@Url.Action("Index", "Blog", New With {.area = ""})">Blog</a>
            <a href="@Url.RouteUrl("Vacatures")">Vacatures</a>
            <a href="@Url.Action("Index", "Contact", New With {.area = ""})">Contact</a>
        </nav>
        <div class="v2-overlay-info">
            <div>
                <h4>Contact</h4>
                <p><a href="tel:+3292164950">+32 (0)9 216 49 50</a></p>
                <p><a href="mailto:info@groupln.be">info@groupln.be</a></p>
            </div>
            <div>
                <h4>Adres</h4>
                <p>Klaverdries 53, 9031 Drongen</p>
            </div>
        </div>
    </div>

    <main id="v2Main" tabindex="-1">
        @RenderBody()
    </main>

    <footer class="v2-footer">
        <div class="v2-container">
            <p class="v2-footer-quote">Bouwen aan plekken waar mensen graag thuiskomen.</p>
            <div class="v2-footer-divider"></div>
            <div class="v2-footer-columns">
                <div class="v2-footer-col-brand">
                    <a href="@Url.Action("Index", "Home", New With {.area = ""})" class="v2-footer-brand">
                        <img src="@Url.Content("~/Content/img/logo.png")" alt="Group LN">
                        <span>
                            <span class="v2-footer-brand-name">Group LN</span>
                            <span class="v2-footer-brand-sub">Projectontwikkeling</span>
                        </span>
                    </a>
                    <ul class="v2-footer-social">
                        <li><a href="https://www.instagram.com/group.ln/" target="_blank" rel="noopener" aria-label="Instagram"><i class="fa fa-instagram" aria-hidden="true"></i></a></li>
                        <li><a href="https://www.linkedin.com/company/group-ln" target="_blank" rel="noopener" aria-label="LinkedIn"><i class="fa fa-linkedin" aria-hidden="true"></i></a></li>
                        <li><a href="https://www.facebook.com/GROUPLN" target="_blank" rel="noopener" aria-label="Facebook"><i class="fa fa-facebook" aria-hidden="true"></i></a></li>
                    </ul>
                </div>
                <div class="v2-footer-col-projecten">
                    <h4 class="v2-footer-col-title">Projecten</h4>
                    <ul class="v2-footer-links">
                        <li><a href="@Url.Action("Index", "Projects", New With {.area = ""})">Woonprojecten</a></li>
                        <li><a href="@Url.Action("Index", "References", New With {.area = ""})">Realisaties</a></li>
                    </ul>
                </div>
                <div class="v2-footer-col-groupln">
                    <h4 class="v2-footer-col-title">Group LN</h4>
                    <ul class="v2-footer-links">
                        <li><a href="@Url.Action("Index", "AboutUs", New With {.area = ""})">Over ons</a></li>
                        <li><a href="@Url.Action("Index", "Blog", New With {.area = ""})">Blog</a></li>
                        <li><a href="@Url.Action("Index", "Contact", New With {.area = ""})">Contact</a></li>
                    </ul>
                </div>
                <div>
                    <h4 class="v2-footer-col-title">Gegevens</h4>
                    <ul class="v2-footer-links">
                        <li>Klaverdries 53</li>
                        <li>9031 Drongen</li>
                        <li><a href="tel:+3292164950">+32 (0)9 216 49 50</a></li>
                        <li><a href="mailto:info@groupln.be">info@groupln.be</a></li>
                    </ul>
                </div>
            </div>
        </div>
        <div class="v2-footer-bottom">
            <div class="v2-container v2-footer-bottom-inner">
                <p class="v2-footer-copyright">© @DateTime.Now.Year Group LN · Projectontwikkeling</p>
                <ul class="v2-footer-legal">
                    <li><a href="@Url.Action("Privacybeleid", "Legal", New With {.area = ""})">Privacybeleid</a></li>
                    <li><a href="@Url.Action("Cookiebeleid", "Legal", New With {.area = ""})">Cookiebeleid</a></li>
                    <li><a href="@Url.Action("AlgemeneVoorwaarden", "Legal", New With {.area = ""})">Algemene voorwaarden</a></li>
                </ul>
                <p class="v2-footer-ai-note"><i class="fa fa-clock-o" aria-hidden="true"></i> Onderdelen van deze site zijn met AI-ondersteuning gebouwd</p>
            </div>
        </div>
    </footer>

    <script src="@Url.Content("~/Scripts/v2/site.js")"></script>
    @RenderSection("scripts", required:=False)
    <script src="@Url.Content("~/Scripts/cookie-consent.js")"></script>
</body>
</html>
