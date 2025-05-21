@Imports Microsoft.AspNet.Identity
<!DOCTYPE html>
<html class="fixed sidebar-light @ViewBag.sidebarcollapsed">
<head>

    <!-- Basic -->
    <meta charset="UTF-8">

    <title>@ViewData("Title")</title>
    <meta name="keywords" content="HTML5 Admin Template" />
    <meta name="description" content="Porto Admin - Responsive HTML5 Template">
    <meta name="author" content="okler.net">
    <link rel="icon" href="@Url.Content("~/img/favicon.ico")" type="image/x-icon" />
    <!-- Mobile Metas -->
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <link href="http://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800|Shadows+Into+Light" rel="stylesheet" type="text/css">
    
    
    @Styles.Render("~/Vendor/Admin/css")
    @RenderSection("PageStyle", required:=False)
    @Styles.Render("~/Theme/Admin/css")
    @Scripts.Render("~/bundles/admin/modernizr")
    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/admin/vendor")
    @*@section PageStyle
    <link href="~/vendor/admin/jstree/themes/default/style.css" rel="stylesheet" />
    <link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
    end section*@

</head>
<body data-loading-overlay="">
    <div class="loading-overlay dark">
        <div class="loader white"></div>
    </div>
    <section class="body">

        <!-- start: header -->
        <header class="header">
            <div class="logo-container">
                <a href="../" class="logo">
                    <img src="~/img/logo-white.png" height="35" alt="CPM" />
                </a>
                <div class="visible-xs toggle-sidebar-left" data-toggle-class="sidebar-left-opened" data-target="html" data-fire-event="sidebar-left-opened">
                    <i class="fa fa-bars" aria-label="Toggle sidebar"></i>
                </div>
            
     
            </div>

            <!-- start: search & user box -->
            <div class="header-right">

                <span class="separator"></span>
                <div id="userbox" class="userbox">
                    <a href="#" data-toggle="dropdown">
                        @*<figure class="profile-picture">
                            <img src="assets/images/!logged-user.jpg" alt="Joseph Doe" class="img-circle" data-lock-picture="assets/images/!logged-user.jpg" />
                        </figure>*@
                        <div class="profile-info" data-lock-name="@User.Identity.Name" data-lock-email="@ViewData("Email")">
                            <span class="name">@ViewData("Fullname")</span>
                            <span class="role">@ViewData("Jobfunction")</span>
                        </div>

                        <i class="fa custom-caret"></i>
                    </a>

                    <div class="dropdown-menu">
                        <ul class="list-unstyled">
                            <li class="divider"></li>
                            @*<li>
                                <a role="menuitem" tabindex="-1" href="pages-user-profile.html"><i class="fa fa-user"></i> Mijn Profiel</a>
                            </li>
                            <li>
                                <a role="menuitem" tabindex="-1" href="#" data-lock-screen="true"><i class="fa fa-lock"></i> Scherm vergrendelen</a>
                            </li>*@
                            <li>
                               @Using Html.BeginForm("LogOff", "Account", New With {.area = ""}, FormMethod.Post, New With {.id = "logoutForm"})
                                @Html.AntiForgeryToken()
                                @<a role="menuitem" tabindex="-1" href="javascript:document.getElementById('logoutForm').submit()"><i class="fa fa-power-off"></i>Uitloggen</a>
                               End Using
                             @*<a role="menuitem" tabindex="-1" href="@Url.Action("LogOff", "Account")"><i class="fa fa-power-off"></i>Uitloggen</a>*@
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
            <!-- end: search & user box -->
        </header>
        <!-- end: header -->

        <div class="inner-wrapper">
            <!-- start: sidebar -->
       @Html.Partial("_LeftSidebarPartial")
            <!-- end: sidebar -->

            <section role="main" class="content-body">
                <header class="page-header">
                    <h2>@ViewBag.Title</h2>

                    <div class="right-wrapper pull-right pr-md">
                        @*<ol class="breadcrumbs">
                            <li>@Html.ActionLink("Home", "Index", "Home")</li>
                            @If (ViewContext.RouteData.Values("controller").ToString() <> "Home" AndAlso ViewContext.RouteData.Values("action") <> "Index") Then
                                @:<li>
                                    @Html.ActionLink(ViewContext.RouteData.Values("controller").ToString(), "Index", ViewContext.RouteData.Values("controller").ToString())

                                @:</li>
                                @:<li class="active"><span>@ViewBag.Title</span></li>
                            End If
                           

                        </ol>*@
                        @Html.MvcSiteMap().SiteMapPath()
                        
                        @*<a class="sidebar-right-toggle" data-open="sidebar-right"><i class="fa fa-chevron-left"></i></a>*@
                    </div>
                </header>

                <!-- start: page -->
                <div class="row">
                    <div class="col-md-12 col-lg-12 col-xl-12">
                       
                        <div id="#layoutbody">
                            @RenderBody()

                        </div>
                        
                    </div>
                </div>





                <!-- end: page -->
            </section>
        </div>

        @*<aside id="sidebar-right" class="sidebar-right">
            <div class="nano">
                <div class="nano-content">
                    <a href="#" class="mobile-close visible-xs">
                        Collapse <i class="fa fa-chevron-right"></i>
                    </a>

                    <div class="sidebar-right-wrapper">

                        <div class="sidebar-widget widget-calendar">
                            <h6>Upcoming Tasks</h6>
                            <div data-plugin-datepicker data-plugin-skin="dark"></div>

                            <ul>
                                <li>
                                    <time datetime="2014-04-19T00:00+00:00">04/19/2014</time>
                                    <span>Company Meeting</span>
                                </li>
                            </ul>
                        </div>

                        <div class="sidebar-widget widget-friends">
                            <h6>Friends</h6>
                            <ul>
                                <li class="status-online">
                                    <figure class="profile-picture">
                                        <img src="assets/images/!sample-user.jpg" alt="Joseph Doe" class="img-circle">
                                    </figure>
                                    <div class="profile-info">
                                        <span class="name">Joseph Doe Junior</span>
                                        <span class="title">Hey, how are you?</span>
                                    </div>
                                </li>
                                <li class="status-online">
                                    <figure class="profile-picture">
                                        <img src="assets/images/!sample-user.jpg" alt="Joseph Doe" class="img-circle">
                                    </figure>
                                    <div class="profile-info">
                                        <span class="name">Joseph Doe Junior</span>
                                        <span class="title">Hey, how are you?</span>
                                    </div>
                                </li>
                                <li class="status-offline">
                                    <figure class="profile-picture">
                                        <img src="assets/images/!sample-user.jpg" alt="Joseph Doe" class="img-circle">
                                    </figure>
                                    <div class="profile-info">
                                        <span class="name">Joseph Doe Junior</span>
                                        <span class="title">Hey, how are you?</span>
                                    </div>
                                </li>
                                <li class="status-offline">
                                    <figure class="profile-picture">
                                        <img src="assets/images/!sample-user.jpg" alt="Joseph Doe" class="img-circle">
                                    </figure>
                                    <div class="profile-info">
                                        <span class="name">Joseph Doe Junior</span>
                                        <span class="title">Hey, how are you?</span>
                                    </div>
                                </li>
                            </ul>
                        </div>

                    </div>
                </div>
            </div>
        </aside>*@
    </section>

    @RenderSection("scripts", required:=False)
    @Scripts.Render("~/bundles/admin/theme")

    @section scripts
        <script>
            $(document).ready(function () {

                $("#txtGeneralSearch").select2({

                    minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                    width: 'resolve',   // to adjust proper width of select2 wrapped elements
                    placeholder: "Zoeken ....",
                    ajax: {

                        url: 'GetGeneralSearchList',
                        cache: false,
                        traditional: true,
                        type: 'POST',
                        data: function (term) {
                            return {
                                term: term,
                            };
                        },

                        results: function (data, page) {
                            return { results: data };
                        },

                    },
                });
            });
            $("#txtGeneralSearch").on("change", function () {


                $.ajax({
                    url: 'SelectSearchItem',
                    data: { id: $("#txtGeneralSearch").select2('data').id },
                    cache: false,
                    traditional: true,
                    type: 'POST',
                    success: function (result) {
                        window.location.href = result;
                    },

                });
            });
        </script>
    end section
</body>
</html>
