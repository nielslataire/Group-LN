<!DOCTYPE html>
<html>
<head>
    <!-- Global site tag (gtag.js) - Google Analytics -->
    <script async src="https://www.googletagmanager.com/gtag/js?id=UA-169400120-1"></script>
    <script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());

  gtag('config', 'UA-169400120-1');
    </script>

    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width" />
    <title>@ViewBag.MetaTitle</title>
    <meta name="keywords" content="bouw en constructie appartement ontwikkeling appartementen coordinatie coördinatie opvolging project woning woningen budget controle werfopvolging werf bouwwerf bouwproject appartementsbouw vlaanderen oost-vlaanderen drongen klaverdries bco" />
    <meta name="title" content="@ViewBag.MetaTitle">
    <meta name="description" content="@ViewBag.MetaSubtitle">
    <meta name="author" content="BCO">
    <meta name="HandheldFriendly" content="true">
    <meta http-equiv="cleartype" content="on">
    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
    <meta property="og:locale" content="nl_BE">
    <meta property="og:site_name" content="BCO">
    <meta property="og:title" content="@ViewBag.MetaTitle">
    <meta property="og:description" content="@ViewBag.MetaSubtitle">
    <meta property="og:url" content="@ViewBag.MetaURL">
    <meta property="og:image" content="@ViewBag.MetaImageURL">
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:site" content="bouwenconstructie"/>
    <meta name="twitter:creator" content="bouwenconstructie"/>
    <meta name="twitter:title" content="@ViewBag.MetaTitle" />
    <meta name="twitter:description" content="@ViewBag.MetaSubtitle" />
    <meta name="twitter:image:alt" content="@ViewBag.MetaDescription" />

    <link rel="icon" href="@Url.Content("~/content/img/favicon.ico")" type="image/x-icon" />
    <link rel="canonical" href="@ViewBag.MetaURL" />
    <link rel="alternate" hreflang="nl" href="@ViewBag.MetaURL" />
    <!-- Mobile Metas -->
    <meta name="viewport" content="width=device-width, minimum-scale=1.0, maximum-scale=1.0, user-scalable=no">

    <!-- Web Fonts  -->
    <link href="http://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800%7CShadows+Into+Light" rel="stylesheet" type="text/css">
    <script src="https://www.google.com/recaptcha/api.js"></script>
    @Styles.Render("~/Vendor/css")
    @Styles.Render("~/Content/theme")
    @Styles.Render("~/Content/skin")
    @RenderSection("PageStyle", required:=False)
    @*@Scripts.Render("~/Vendor/js")
    @Scripts.Render("~/Scripts/js")*@
    <!-- Facebook Pixel Code -->
    <script>
  !function(f,b,e,v,n,t,s)
  {if(f.fbq)return;n=f.fbq=function(){n.callMethod?
  n.callMethod.apply(n,arguments):n.queue.push(arguments)};
  if(!f._fbq)f._fbq=n;n.push=n;n.loaded=!0;n.version='2.0';
  n.queue=[];t=b.createElement(e);t.async=!0;
  t.src=v;s=b.getElementsByTagName(e)[0];
  s.parentNode.insertBefore(t,s)}(window, document,'script',
  'https://connect.facebook.net/en_US/fbevents.js');
  fbq('init', '191433198784073');
  fbq('track', 'PageView');
    </script>
    <noscript>
        <img height="1" width="1" style="display:none"
             src="https://www.facebook.com/tr?id=191433198784073&ev=PageView&noscript=1" />
    </noscript>
    <!-- End Facebook Pixel Code -->
</head>
<body>
    <div class="body">
        <header id="header" class="header-no-border-bottom" data-plugin-options='{"stickyEnabled": true, "stickyEnableOnBoxed": true, "stickyEnableOnMobile": true, "stickyStartAt": 148, "stickySetTop": "-148px", "stickyChangeLogo": false}'>
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
                                <a href="@Url.Action("Index", "Home")">
                                    <img alt="Copro" height="80" data-sticky-width="82" data-sticky-height="40" data-sticky-top="33" src="@Url.Content("~/Content/img/logo-default.png")">
                                </a>
                            </div>
                        </div>
                        <div class="header-column">
                            <ul class="header-extra-info hidden-xs ">
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
                                <li>
                                    <div class="feature-box feature-box-style-3">
                                        <div class="feature-box-icon">
                                            <a href="mailto:info@bouwenconstructie.be"><i class="fa fa-envelope"></i></a>
                                        </div>
                                        <div class="feature-box-info">
                                            <h4 class="mb-none">info@bouwenconstructie.be</h4>
                                            <p><small>Of stuur ons een mail</small></p>
                                        </div>
                                    </div>
                                </li>

                            </ul>
                        </div>
                    </div>
                </div>
                <div class="header-container header-nav header-nav-bar header-nav-bar-primary" >
                    <div class=" container" >
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
                                @*<li>
                                    <a href="@Url.Action("Index", "AboutUs")">
                                        Over ons
                                    </a>
                                </li>*@
                                <li>
                                    <a href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional})">
                                        Woonprojecten
                                    </a>
                                </li>
                                <li>
                                    <a href="@Url.Action("Index", "References", New With {.id = UrlParameter.Optional})">
                                        Realisaties
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
        <footer id="footer" class="light">
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
                    </div>
                    <div class="col-md-4">
                        <div class="contact-details">
                            <h4><strong>Contacteer</strong> Ons</h4>
                            <ul class="contact">
                                <li><p><i class="fa fa-map-marker"></i> <strong>Adres:</strong> Klaverdries 53, 9031 Drongen, België</p></li>
                                <li><p><i class="fa fa-phone"></i> <strong>Telefoon:</strong> +32 (0)9 216 49 50</p></li>
                                <li><p><i class="fa fa-envelope"></i> <strong>Email:</strong> <a href="mailto:info@bouwenconstructie.be">info@bouwenconstructie.be</a></p></li>
                            </ul>
                        </div>
                    </div>
                    <div class="col-md-2">
                        <h4><strong>Sociale</strong> Media</h4>
                        <ul class="social-icons">
                            <li class="social-icons-facebook"><a href="http://www.facebook.com/GROUPLN" target="_blank" title="Facebook"><i class="fa fa-facebook"></i></a></li>
                            @*<li class="social-icons-twitter"><a href="http://www.twitter.com/" target="_blank" title="Twitter"><i class="fa fa-twitter"></i></a></li>*@
                            <li class="social-icons-linkedin"><a href="http://www.linkedin.com/company/bouwteam-copro" target="_blank" title="Linkedin"><i class="fa fa-linkedin"></i></a></li>
                        </ul>
                    </div>
                </div>
            </div>
            <div class="footer-copyright" style="background-image:url(@Url.Content("~/content/skins/pattern.png"));height:45px;">
                <div class="container">
                    <div class="row">

                        <div class="col-md-8">
                            <p>© Copyright 2020. All Rights Reserved.</p>
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
    @Scripts.Render("~/bundles/jqueryval")
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
