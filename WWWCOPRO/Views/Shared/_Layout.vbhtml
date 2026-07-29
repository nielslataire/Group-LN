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
    <script src="https://www.google.com/recaptcha/api.js" async defer></script>
    @Styles.Render("~/Vendor/css")
    @Styles.Render("~/Content/theme")
    @Styles.Render("~/Content/skin")
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
        <header id="header" class="header-no-border-bottom" data-plugin-options='{"stickyEnabled": true, "stickyEnableOnBoxed": true, "stickyEnableOnMobile": true, "stickyStartAt": 175, "stickySetTop": "-175px", "stickyChangeLogo": false}'>
            <div class="header-body">
                <!--
                <div class="header-top header-top-style-2">
                    <div class="container">
                        <p class="pull-left hidden-xs">
                            The #1 Selling HTML Site Template on ThemeForest.
                        </p>
                        <p class="pull-right">
                            <i class="fa fa-map-marker"></i> 1234 Street Name, City Name, US
                        </p>
                    </div>
                </div>
                    -->
                <div class="header-container container">
                    <div class="header-row">
                        <div class="header-column">
                            <div class="header-logo">
                                <a href="@Url.Action("Index", "Home")" class="header-logo-brand">
                                    <img alt="Group LN" class="header-brand-img" data-sticky-width="36" data-sticky-height="36" data-sticky-top="22" src="@Url.Content("~/Content/img/logoimg.jpg")">
                                    <span class="header-brand-text">
                                        <span class="header-brand-name">GROUP LN</span>
                                        <span class="header-brand-sub">Projectontwikkeling</span>
                                        <span class="header-brand-tagline">Appartementen &middot; Woningen</span>
                                    </span>
                                    <span class="header-brand-divider"></span>
                                </a>
                            </div>
                        </div>
                        <div class="header-column">
                            <ul class="header-extra-info hidden-xs header-contact-list">
                                <li>
                                    <div class="feature-box feature-box-style-3">
                                        <div class="feature-box-icon">
                                            <i class="fa fa-phone"></i>
                                        </div>
                                        <div class="feature-box-info">
                                            <h4 class="mb-none">+32 (0)9 216 49 50</h4>
                                            <p><small>Neem telefonisch contact op</small></p>
                                        </div>
                                    </div>
                                </li>
                                <li class="header-contact-divider"></li>
                                <li>
                                    <div class="feature-box feature-box-style-3">
                                        <div class="feature-box-icon">
                                            <a href="mailto:info@groupln.be"><i class="fa fa-envelope"></i></a>
                                        </div>
                                        <div class="feature-box-info">
                                            <h4 class="mb-none">info@groupln.be</h4>
                                            <p><small>Of stuur ons een mail</small></p>
                                        </div>
                                    </div>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
                <div class="header-container header-nav header-nav-bar header-nav-bar-primary">
                    <div class=" container">
                        <button class="btn header-btn-collapse-nav" data-toggle="collapse" data-target=".header-nav-main">
                            <i class="fa fa-bars"></i>
                        </button>
                        <div class="header-nav-main header-nav-main-light  header-nav-main-effect-1 header-nav-main-sub-effect-1 collapse">
                            <nav>
                                <ul class="nav nav-pills" id="mainNav">
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
                        </div>
                    </div>
                </div>
            </div>
        </header>
        <div role="main" class="main">
            @RenderBody()
        </div>
        <footer id="footer" class="dark footer-primary">
            <div class="container">
                <div class="row">
                    <div class="footer-ribbon">
                        <span>Volg ons</span>
                    </div>
                    @*<div class="col-md-3">
                            <div class="newsletter">
                                @Html.Partial("Newsletter")

                            </div>
                            @RenderSection("LatestPictures", False)

                        </div>*@
                    <div class="col-md-4">
                        @RenderSection("LatestNews", False)
                        @*<h4>Laaste Facebook Posts</h4>
                            <div id="tweet" class="twitter" data-plugin-tweets data-plugin-options='{"username": "", "count": 2}'>
                                <p>Please wait...</p>
                            </div>*@
                    </div>
                    <div class="col-md-4">
                        <div class="contact-details">
                            <h4><strong>Contacteer</strong> Ons</h4>
                            <ul class="contact">
                                <li><p><i class="fa fa-map-marker"></i> <strong>Adres:</strong> Klaverdries 53, 9031 Drongen, België</p></li>
                                <li><p><i class="fa fa-phone"></i> <strong>Telefoon:</strong> +32 (0)9 216 49 50</p></li>
                                <li><p><i class="fa fa-envelope"></i> <strong>Email:</strong> <a href="mailto:info@groupln.be">info@groupln.be</a></p></li>
                            </ul>
                        </div>
                    </div>
                    <div class="col-md-2">
                        <h4><strong>Sociale</strong> Media</h4>
                        <ul class="social-icons">
                            <li class="social-icons-facebook"><a href="https://www.facebook.com/GROUPLN" target="_blank" title="Facebook"><i class="fa fa-facebook"></i></a></li>
                            @*<li class="social-icons-twitter"><a href="http://www.twitter.com/" target="_blank" title="Twitter"><i class="fa fa-twitter"></i></a></li>*@
                            <li class="social-icons-linkedin"><a href="https://www.linkedin.com/company/group-ln" target="_blank" title="Linkedin"><i class="fa fa-linkedin"></i></a></li>
                            <li class="social-icons-instagram"><a href="https://www.instagram.com/group.ln/" target="_blank" title="Linkedin"><i class="fa fa-instagram"></i></a></li>
                        </ul>
                    </div>
                </div>
            </div>
            <div class="footer-copyright footer-copyright-primary">
                <div class="container">
                    <div class="row">

                        <div class="col-md-8">
                            <p>© Copyright 2026 Group LN. All Rights Reserved.</p>
                        </div>
                        @*<div class="col-md-4">
                                <nav id="sub-menu">
                                    <ul style="color:#FFF">
                                        <li><a href="page-faq.html">FAQ's</a></li>
                                        <li><a href="sitemap.html">Sitemap</a></li>
                                        <li><a href="contact-us.html">Contact</a></li>
                                    </ul>
                                </nav>
                            </div>*@
                    </div>
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
