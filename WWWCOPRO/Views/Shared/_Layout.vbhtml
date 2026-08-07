@imports bo
@Code
    Dim _metaDesc As String = If(Not String.IsNullOrEmpty(CStr(ViewData("MetaDescription"))), CStr(ViewData("MetaDescription")), "Projectontwikkeling en Bouwcoördinatie van alle residentiële bouwprojecten.")
    Dim _ogTitle As String = If(Not String.IsNullOrEmpty(CStr(ViewData("ogtitle"))), CStr(ViewData("ogtitle")), CStr(ViewData("Title")))
    Dim _ogType As String = If(Not String.IsNullOrEmpty(CStr(ViewData("ogtype"))), CStr(ViewData("ogtype")), "website")
    Dim _ogDescription As String = If(Not String.IsNullOrEmpty(CStr(ViewData("ogdescription"))), CStr(ViewData("ogdescription")), _metaDesc)
    Dim _ogImage As String = If(Not String.IsNullOrEmpty(CStr(ViewData("ogimage"))), CStr(ViewData("ogimage")), "https://www.groupln.be/Content/img/logoimg.jpg")
    Dim _defaultCanonical As String = "https://www.groupln.be" & Request.Url.AbsolutePath.ToLowerInvariant().TrimEnd("/"c)
    If _defaultCanonical = "https://www.groupln.be" Then _defaultCanonical &= "/"
    Dim _ogUrl As String = If(Not String.IsNullOrEmpty(CStr(ViewData("ogurl"))), CStr(ViewData("ogurl")), _defaultCanonical)
    Dim _canonical As String = If(Not String.IsNullOrEmpty(CStr(ViewData("canonical"))), CStr(ViewData("canonical")), _defaultCanonical)
    Dim _heroHeader As Boolean = CBool(If(ViewData("HeroHeader"), False))
End Code
<!DOCTYPE html>
<html lang="nl">
<head>
    <!-- Google Tag Manager -->
    <script>
        (function (w, d, s, l, i) {
            w[l] = w[l] || []; w[l].push({
                'gtm.start':
                    new Date().getTime(), event: 'gtm.js'
            }); var f = d.getElementsByTagName(s)[0],
                j = d.createElement(s), dl = l != 'dataLayer' ? '&l=' + l : ''; j.async = true; j.src =
                    'https://www.googletagmanager.com/gtm.js?id=' + i + dl; f.parentNode.insertBefore(j, f);
        })(window, document, 'script', 'dataLayer', 'GTM-KG5HPVWR');</script>
    <!-- End Google Tag Manager -->
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@ViewData("Title")</title>
    <meta name="keywords" content="bouw appartement ontwikkeling appartementen coordinatie coördinatie opvolging project woning woningen budget controle werfopvolging werf bouwwerf bouwproject appartementsbouw vlaanderen oost-vlaanderen drongen klaverdries bouwteam copro" />
    <meta name="description" content="@_metaDesc" />
    <meta name="author" content="Group LN">
    <link rel="icon" href="@Url.Content("~/content/img/favicon.ico")" type="image/x-icon" />
    <meta property="og:title" content="@_ogTitle" />
    <meta property="og:type" content="@_ogType" />
    <meta property="og:description" content="@_ogDescription" />
    <meta property="og:image" content="@_ogImage" />
    <meta property="og:url" content="@_ogUrl" />
    <meta property="og:locale" content="nl_BE" />
    <meta property="og:site_name" content="Group LN" />
    @If Not String.IsNullOrEmpty(CStr(ViewData("twittercard"))) Then
        @<meta name="twitter:card" content="@ViewData("twittercard")" />
        @<meta name="twitter:title" content="@ViewData("twittertitle")" />
        @<meta name="twitter:description" content="@ViewData("twitterdescription")" />
        @<meta name="twitter:image" content="@ViewData("twitterimage")" />
    End If
    <link rel="canonical" href="@_canonical" />
    <script type="application/ld+json">
               {
                 "@@id": "https://www.groupln.be/#organization",
                 "@@context": "https://schema.org",
                 "@@type": [
                   "Organization",
                   "LocalBusiness"
                 ],
                 "name": "Group LN",
                 "url": "https://www.groupln.be/",
                 "logo": "https://www.groupln.be/Content/img/logoimg.jpg",
                 "image": "https://www.groupln.be/Content/img/logoimg.jpg",
                 "telephone": "+32 9 216 49 50",
                 "email": "info@groupln.be",
                 "address": {
                   "@@type": "PostalAddress",
                   "streetAddress": "Klaverdries 53",
                   "postalCode": "9031",
                   "addressLocality": "Drongen",
                   "addressRegion": "Oost-Vlaanderen",
                   "addressCountry": {
                     "@@type": "Country",
                     "name": "BE"
                   }
                 },
                 "areaServed": [
                   {
                     "@@type": "AdministrativeArea",
                     "name": "Oost-Vlaanderen"
                   },
                   {
                     "@@type": "AdministrativeArea",
                     "name": "West-Vlaanderen"
                   },
                   {
                      "@@type": "AdministrativeArea",
                      "name": "Antwerpen"
                    }
                 ],
                 "contactPoint": {
                   "@@type": "ContactPoint",
                   "telephone": "+32 9 216 49 50",
                   "email": "info@groupln.be",
                   "contactType": "sales",
                   "areaServed": "BE",
                   "availableLanguage": [
                     "nl",
                     "fr",
                     "en"
                   ]
                 },
                 "sameAs": [
                   "https://www.facebook.com/GROUPLN",
                   "https://www.linkedin.com/company/group-ln",
                   "https://www.instagram.com/group.ln/"
                 ]
               }
    </script>
    <!-- Web Fonts  -->
    <link href="https://fonts.googleapis.com/css2?family=Open+Sans:wght@300;400;600;700;800&family=Playfair+Display:wght@400;500;600&display=swap" rel="stylesheet" type="text/css">
    <link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet" />
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    @Styles.Render("~/Vendor/css")
    @Styles.Render("~/Content/theme")
    @Styles.Render("~/Content/skin")
    <link rel="stylesheet" href="@Url.Content("~/Content/footer.css")" />
    <link rel="stylesheet" href="@Url.Content("~/Content/reveal.css")" />
    <script>
        if ('IntersectionObserver' in window) {
            document.documentElement.classList.add('js-reveal');
        }
    </script>
    @RenderSection("PageMeta", required:=False)
    @RenderSection("PageStyle", required:=False)

</head>
<body>
    <!-- Google Tag Manager (noscript) -->
    <noscript>
        <iframe src="https://www.googletagmanager.com/ns.html?id=GTM-KG5HPVWR"
                height="0" width="0" style="display:none;visibility:hidden"></iframe>
    </noscript>
    <!-- End Google Tag Manager (noscript) -->
    <div class="body">
        <header id="header" class="header-no-border-bottom header-full-width @(If(_heroHeader, "header-transparent header-transparent-brand", ""))" data-plugin-options='{"stickyEnabled": true, "stickyEnableOnBoxed": true, "stickyEnableOnMobile": true, "stickyStartAt": @(If(_heroHeader, 175, 0)), "stickySetTop": 0, "stickyChangeLogo": false}'>
            <div class="header-body">
                <div class="header-container container-fluid">
                    <div class="header-row header-row-redesign">
                        <div class="header-column header-logo-col">
                            <div class="header-logo">
                                <a href="@Url.Action("Index", "Home")" class="header-logo-brand">
                                    <img alt="Group LN" class="header-brand-img" src="@Url.Content("~/Content/img/logo.png")">
                                    <span class="header-brand-text">
                                        <span class="header-brand-name">GROUP LN</span>
                                        <span class="header-brand-sub">Projectontwikkeling</span>
                                    </span>
                                  
                                </a>
                            </div>
                            <a href="tel:+3292164950" class="header-phone">
                                <span class="header-phone-icon"><i class="fa fa-phone"></i></span>
                                <span class="header-phone-text">
                                    <span class="header-phone-label">Bel ons</span>
                                    <span class="header-phone-number">+32 (0)9 216 49 50</span>
                                </span>
                            </a>
                        </div>
                        <div class="header-column header-nav-col">
                            <nav class="hero-nav-items" id="heroNavItems">
                                <a class="hero-nav-item" href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Woonproject})">Woonprojecten</a>
                                <a class="hero-nav-item" href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Commerciëel})">Commercieel</a>
                                <a class="hero-nav-item" href="@Url.Action("Index", "Blog")">Blog</a>
                                <a class="hero-nav-item" href="@Url.Action("Index", "Contact")">Contact</a>
                            </nav>
                            <button type="button" id="navOverlayToggle" class="hamburger-circle-btn" aria-expanded="false" aria-controls="navOverlay">
                                <span class="hamburger-lines"><span></span><span></span><span></span></span>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </header>

        <div id="navOverlay" class="nav-overlay" aria-hidden="true">
            <div class="nav-overlay-backdrop"></div>
            <div class="nav-overlay-panel">
                <button type="button" id="navOverlayClose" class="nav-overlay-close" aria-label="Sluiten">&times;</button>
                <nav>
                    <ul class="nav-overlay-list" id="mainNav">
                        <li>
                            <a href="@Url.Action("Index", "Home")">
                                Home
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "AboutUs")">
                                Over ons
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Woonproject})">
                                Woonprojecten
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Commerciëel})">
                                Commercieel
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "References", New With {.id = UrlParameter.Optional})">
                                Realisaties
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "Blog")">
                                Blog
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "Team")">
                                Team
                            </a>
                        </li>
                        <li>
                            <a href="@Url.Action("Index", "Contact")">
                                Contact
                            </a>
                        </li>
                    </ul>
                </nav>
                <div class="nav-overlay-info">
                    <div class="nav-overlay-contact">
                        <h4>Contact</h4>
                        <p><a href="tel:+3292164950">+32 (0)9 216 49 50</a></p>
                        <p><a href="mailto:info@groupln.be">info@groupln.be</a></p>
                    </div>
                    <div class="nav-overlay-address">
                        <h4>Adres</h4>
                        <p>Klaverdries 53, 9031 Drongen</p>
                    </div>
                </div>
            </div>
        </div>
        <div role="main" class="main">
            @RenderBody()
        </div>
        <footer id="footer" class="site-footer">
            <div class="container">
                <p class="footer-quote">Bouwen aan plekken waar mensen graag thuiskomen.</p>
            </div>
            <div class="container">
                <div class="footer-divider"></div>
            </div>
            <div class="container">
                <div class="footer-columns">
                    <div class="footer-col footer-col-brand">
                        <a href="@Url.Action("Index", "Home")" class="footer-logo-brand">
                            <img alt="Group LN" class="footer-brand-img" src="@Url.Content("~/Content/img/logo.png")">
                            <span class="footer-brand-text">
                                <span class="footer-brand-name">GROUP LN</span>
                                <span class="footer-brand-sub">Projectontwikkeling</span>
                            </span>
                        </a>
                        <ul class="footer-social">
                            <li><a href="https://www.instagram.com/group.ln/" target="_blank" rel="noopener" aria-label="Instagram"><i class="bx bxl-instagram"></i></a></li>
                            <li><a href="https://www.linkedin.com/company/group-ln" target="_blank" rel="noopener" aria-label="LinkedIn"><i class="bx bxl-linkedin"></i></a></li>
                            <li><a href="https://www.facebook.com/GROUPLN" target="_blank" rel="noopener" aria-label="Facebook"><i class="bx bxl-facebook"></i></a></li>
                            <li><a href="@("https://www.tiktok.com/@groupln_")" target="_blank" rel="noopener" aria-label="TikTok"><i class="bx bxl-tiktok"></i></a></li>
                            <li><a href="@("https://www.youtube.com/@Group_LN")" target="_blank" rel="noopener" aria-label="YouTube"><i class="bx bxl-youtube"></i></a></li>
                        </ul>
                    </div>
                    <div class="footer-col footer-col-projecten">
                        <h4 class="footer-col-title">Projecten</h4>
                        <ul class="footer-links">
                            <li><a href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Woonproject})">Woonprojecten</a></li>
                            <li><a href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Commerciëel})">Commercieel</a></li>
                            <li><a href="@Url.Action("Index", "References", New With {.id = UrlParameter.Optional})">Realisaties</a></li>
                        </ul>
                    </div>
                    <div class="footer-col footer-col-groupln">
                        <h4 class="footer-col-title">Group LN</h4>
                        <ul class="footer-links">
                            <li><a href="@Url.Action("Index", "AboutUs")">Over ons</a></li>
                            <li><a href="@Url.Action("Index", "Blog")">Blog</a></li>
                            <li style="display:none;"><a href="#">FAQ</a></li>
                            <li><a href="@Url.Action("Index", "Contact")">Contact</a></li>
                        </ul>
                    </div>
                    <div class="footer-col">
                        <h4 class="footer-col-title">Gegevens</h4>
                        <ul class="footer-links">
                            <li>Klaverdries 53</li>
                            <li>9031 Drongen</li>
                            <li><a href="tel:+3292164950">+32 (0)9 216 49 50</a></li>
                            <li><a href="mailto:info@groupln.be">info@groupln.be</a></li>
                        </ul>
                    </div>
                </div>
            </div>
            <div class="footer-bottom">
                <div class="container footer-bottom-inner">
                    <p class="footer-copyright">© @DateTime.Now.Year Group LN · Projectontwikkeling</p>
                    <ul class="footer-legal">
                        <li style="display:none;"><a href="#">Privacybeleid</a></li>
                        <li style="display:none;"><a href="#">Cookies</a></li>
                        <li style="display:none;"><a href="#">Algemene voorwaarden</a></li>
                    </ul>
                    <p class="footer-ai-note"><i class="bx bx-time-five"></i> Onderdelen van deze site zijn met AI-ondersteuning gebouwd</p>
                </div>
            </div>
        </footer>
    </div>
    @Scripts.Render("~/Vendor/jquerybundle")
    @Scripts.Render("~/Vendor/headlibs")
    @Scripts.Render("~/Vendor/jsbundle")
    @Scripts.Render("~/Scripts/jsbundle")
    @RenderSection("scripts", required:=False)
    @*@section scripts
            <script>
                $(function () {
                    $('#FormNewsletter').submit(function () {
                        if ($(this).valid()) {
                            $.ajax({
                                url: this.action,
                                type: this.method,
                                data: $(this).serialize(),
                                success: function (result) {

                                    if(result.success === true){
                                        //$.post(result.url,function(partial){
                                        //    $('#DepartmentRows').html(partial);
                                        //});

                                    }
                                    else{
                                        //$.post(result.url,function(partial){
                                        $('#ValSummary').html(partial);
                                        //});
                                    }

                                }

                            });
                        }
                        return false;
                    });
                });
            </script>
        End Section*@
</body>
</html>
