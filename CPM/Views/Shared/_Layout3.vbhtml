<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">	
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewBag.Title - Copro Project Management</title>
    <link href="http://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800%7CShadows+Into+Light" rel="stylesheet" type="text/css">

    @Styles.Render("~/Content/css")
    @Styles.Render("~/Vendor/css")
    @Styles.Render("~/Theme/css")
    @Scripts.Render("~/bundles/modernizr")

</head>
<body>

    
    @*<div class="navbar navbar-inverse navbar-fixed-top">
        <div class="container">
            <div class="navbar-header">
                <button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-collapse">
                    <span class="icon-bar"></span>
                    <span class="icon-bar"></span>
                    <span class="icon-bar"></span>
                </button>
                @Html.ActionLink("Copro Project Management", "Index", "Home", New With {.area = ""}, New With {.class = "navbar-brand"})
            </div>
            <div class="navbar-collapse collapse">
                <ul class="nav navbar-nav">
                    <li>@Html.ActionLink("Home", "Index", "Home")</li>
                    <li>@Html.ActionLink("About", "About", "Home")</li>
                    <li>@Html.ActionLink("Contact", "Contact", "Home")</li>
                    <li>@Html.ActionLink("Leveranciers", "Index", "Leveranciers")</li>
                </ul>
                @Html.Partial("_LoginPartial")
            </div>
        </div>
    </div>
    <div class="container body-content">
        @RenderBody()
        <hr />
        <footer>
            <p>&copy; @DateTime.Now.Year - My ASP.NET Application</p>
        </footer>
    </div>*@

    <div class="body">
        <header id="header" class="flat-menu single-menu">
            <div class="container">
                <div class="logo">
                    <a href="index.html">
                        <img alt="Porto" width="185" height="75" data-sticky-width="82" data-sticky-height="40" src="~/img/logo.png">
                    </a>
                </div>
                <button class="btn btn-responsive-nav btn-inverse" data-toggle="collapse" data-target=".nav-main-collapse">
                    <i class="fa fa-bars"></i>
                </button>
            </div>
            <div class="navbar-collapse nav-main-collapse collapse">
                <div class="container">
                    <ul class="social-icons">
                        <li class="facebook"><a href="http://www.facebook.com/bouwteamcopro" target="_blank" title="Facebook">Facebook</a></li>
                        <li class="twitter"><a href="http://www.twitter.com/" target="_blank" title="Twitter">Twitter</a></li>
                        <li class="linkedin"><a href="http://www.linkedin.com/" target="_blank" title="Linkedin">Linkedin</a></li>
                    </ul>
                    @Html.Partial("_MenuPartial")
                </div>
            </div>
        </header>

        <div role="main" class="main">
            <section class="page-header">
                <div class="container">
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="breadcrumb">
                                <li>@Html.ActionLink("Home", "Index", "Home")</li>
                                @If (ViewContext.RouteData.Values("controller").ToString() <> "Home" AndAlso ViewContext.RouteData.Values("action") <> "Index") Then
                                    @:<li>
                                        @Html.ActionLink(ViewContext.RouteData.Values("controller").ToString(), "Index", ViewContext.RouteData.Values("controller").ToString())
                                        @:</li>


                                End If
                                <li class="active">@ViewBag.SubTitle</li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <h1>@ViewBag.Title</h1>
                        </div>
                    </div>
                </div>
            </section>
            <div class="container">
            @RenderBody()
                </div>
        </div>

        <footer id="footer">
            <div class="container">
                <div class="row">
                    <div class="footer-ribbon">
                        <span>Informatiepaneel</span>
                    </div>
                    <div class="col-md-12">
                        <div class="newsletter">
                            <h4 class="heading-primary">To do</h4>
                            <p>Eventueel in een later stadium kan hier een communicatiepaneel komen met wat er nog moet gedaan worden of reminders die er in gestoken kunnen worden ...</p>

                           
                        </div>
                    </div>
                    
                </div>
            </div>
            <div class="footer-copyright">
                <div class="container">
                    <div class="row">
                       
                        <div class="col-md-7">
                            <p>© Copyright 2015. All Rights Reserved.</p>
                        </div>
                        
                    </div>
                </div>
            </div>
        </footer>
    </div>
    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/bootstrap")
    @Scripts.Render("~/bundles/vendor")
    @Scripts.Render("~/bundles/theme")
    @RenderSection("scripts", required:=False)

</body>
</html>





   



    <!-- Google Analytics: Change UA-XXXXX-X to be your site's ID. Go to http://www.google.com/analytics/ for more information.
    <script type="text/javascript">

        var _gaq = _gaq || [];
        _gaq.push(['_setAccount', 'UA-12345678-1']);
        _gaq.push(['_trackPageview']);

        (function() {
        var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
        ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
        var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
        })();

    </script>
     -->

