@imports BO
@modeltype ShowProjectDetail
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.Project.Name
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">

End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu">
            <div class="content-with-menu-container">
                <div class="inner-menu-toggle">
                    <a href="#" class="inner-menu-expand" data-open="inner-menu">
                        Toon Menu <i class="fa fa-chevron-right"></i>
                    </a>
                </div>
                <menu id="content-menu" class="inner-menu" role="menu">
                    <div class="nano">
                        <div class="nano-content">

                            <div class="inner-menu-toggle-inside">
                                <a href="#" class="inner-menu-collapse">
                                    <i class="fa fa-chevron-up visible-xs-inline"></i><i class="fa fa-chevron-left hidden-xs-inline"></i> Verberg Menu
                                </a>
                                <a href="#" class="inner-menu-expand" data-open="inner-menu">
                                    Toon Menu <i class="fa fa-chevron-down"></i>
                                </a>
                            </div>
                            <div class="inner-menu-content">
                                <div class="sidebar-widget m-none">
                                    <div class="widget-header clearfix">
                                        <a href="#Project" data-toggle="tab"> <h5 class="title pull-left mt-xs">Projectmenu</h5></a>
                                    </div>
                                    <div class="widget-content">
                                        <ul class="mg-folders">
                                            @Html.Partial("DetailMenu", Model.Project.Id)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>
                <div class="inner-body mg-main">
                    @*<div class="inner-toolbar clearfix">
                            <ul>
                                <li>
                                    @Html.HtmlActionLink("<i class='fa fa-edit'></i> Bewerken</a>", "EditGeneralData", "Projecten", New With {.projectid = Model.Project.Id}, New With {.class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = True)
                                </li>
                                <li>
                                    @Html.HtmlActionLink("<i class='fa fa-save'></i> Opslaan</a>", "SaveGeneralData", "Projecten", New With {.model = Model}, New With {.id = "GeneralDataSave", .class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                </li>
                                <li>
                                    @Html.HtmlActionLink("<i class='fa fa-undo'></i> Annuleren</a>", "CancelEditGeneralData", "Projecten", New With {.projectid = Model.Project.Id}, New With {.class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                </li>

                            </ul>
                        </div>*@
                    <div class="row">
                        <div class="col-sm-12 text-right ">
                            <div class="btn-group ">
                                <button type="button" data-toggle="tooltip" data-placement="top" title="Bewerken" onclick="location.href='@Url.Action("Edit", "Projecten", New With {.projectid = Model.Project.Id, .EditGeneralData = True})'" class="btn btn-default ml-xs mb-xs"><i class="fa fa-edit"></i></button>
                                @*<button type="button" data-toggle="tooltip" data-placement="top" title="Naar PDF" onclick="location.href='@Url.Action("DetailExportToPdf", "Klanten", New With {.id = Model.Client.Id, .projectid = Model.ProjectId})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-file-pdf-o"></i></button>*@
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4 col-lg-3">

                            <section class="panel">
                                <div class="panel-body">
                                    <div class="thumb-info mb-md">
                                        <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.Project.DefaultPicture.Name)" class="img-responsive" alt="John Doe">
                                        <div class="thumb-info-title">
                                            <span class="thumb-info-inner">@Model.Project.Name</span>
                                            <span class="thumb-info-type">@Model.Project.Postalcode.Gemeente </span>
                                        </div>
                                    </div>
                                    <ul class="list list-icons">
                                        <li>
                                            <i class="fa fa-map-marker"></i>
                                            <strong>Adres: </strong>@Model.Project.Street @Model.Project.HouseNumber

                                            <br />
                                            @Model.Project.Postalcode.Postcode @Model.Project.Postalcode.Gemeente.ToUpper<br />
                                            @Model.Project.Postalcode.Country.Name

                                        </li>
                                        @if Model.Project.Status IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-spinner"></i>
                                                    <strong> Status : </strong>@Model.Project.Status.Name
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.WheaterStation IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-sun-o"></i>
                                                    <strong> Weerstation : </strong>@Model.Project.WheaterStation.Name
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.Developer IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-calculator"></i>
                                                    <strong> Ontwikkelaar : </strong>@Model.Project.Developer.Display
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.Builder IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-building"></i>
                                                    <strong> Aannemer : </strong>@Model.Project.Builder.Display
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.Architect IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-male"></i>
                                                    <strong> Architect : </strong>@Model.Project.Architect.Display
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.Engineer IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-male"></i>
                                                    <strong> Ingenieur stabiliteit : </strong>@Model.Project.Engineer.Display
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.SecurityCoordinator IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-male"></i>
                                                    <strong> Veiligheidscoördinator : </strong>@Model.Project.SecurityCoordinator.Display
                                                </li>
                                            </text>
                                        End If
                                        @if Model.Project.EpbReporter IsNot Nothing Then
                                            @<text>
                                                <li>
                                                    <i Class="fa fa-male"></i>
                                                    <strong> Epb-verslaggever : </strong>@Model.Project.EpbReporter.Display
                                                </li>
                                            </text>
                                        End If

                                    </ul>

                                </div>
                            </section>

                        </div>
                        <div Class="col-md-4 col-lg-3">
                            <section class="panel">
                                <header class="panel-heading">
                                    <div class="panel-actions">
                                        <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                                        <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
                                    </div>

                                    <h2 class="panel-title">
                                        <span class="va-middle">Klanten</span>
                                    </h2>
                                </header>
                                <div class="panel-body">
                                    <div class="content">
                                        <ul>
                                            @If Model.RecentClients IsNot Nothing And Model.RecentClients.Count > 0 Then
                                                @For Each item In Model.RecentClients
                                                    @<text>
                                                        <li>
                                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Klantgegevens" href="@Url.Action("Detail", "Klanten", New With {.clientid = item.ID, .projectid = Model.Project.Id})">
                                                                <span Class="title">@item.Display </span>
                                                            </a>
                                                        </li>
                                                    </text>
                                                Next
                                            End If


                                        </ul>
                                        <div Class="text-right">
                                            <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Klanten" href="@Url.Action("DetailClients", "Projecten", New With {.projectid = Model.Project.Id})">
                                                (Toon Alle)
                                            </a>
                                        </div>
                                        <hr Class="dotted short">
                                        <div class="social-icons-list">
                                            <a rel="tooltip" data-placement="bottom" href="@Url.Action("AddClientAccount", "Klanten", New With {.id = Model.Project.Id})" data-original-title="Klant toevoegen"><i class="fa fa-plus"></i><span>Klant toevoegen</span></a>
                                        </div>
                                    </div>
                                </div>

                            </section>
                            <section class="panel">
                                <header class="panel-heading">
                                    <div class="panel-actions">
                                        <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                                        <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
                                    </div>

                                    <h2 class="panel-title">
                                        <span class="va-middle">Nieuws</span>
                                    </h2>
                                </header>
                                <div class="panel-body">
                                    <div class="content">
                                        @If Model.LatestNews IsNot Nothing Then
                                            @<text>
                                                <div class="blog-posts single-post">
                                                    <article class="post post-large blog-single-post ">
                                                        <div class="post-image">
                                                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & Model.LatestNews.Picture.Name)" class="img-responsive" alt="@Model.LatestNews.TitleNL">

                                                        </div>
                                                        <div class="post-date">
                                                            <span class="day">@Model.LatestNews.NewsDate.Day </span>
                                                            <span class="month">@Model.LatestNews.NewsDate.ToString("MMM")</span>
                                                        </div>
                                                        <div class="post-content">

                                                            <h4><a href="blog-post.html">@Model.LatestNews.TitleNL </a></h4>
                                                            <p class="small">@Model.LatestNews.TextNL</p>

                                                        </div>
                                                    </article>
                                                </div>
                                            </text>
                                        End If
                                        <div Class="text-right">
                                            <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Nieuws" href="@Url.Action("DetailNews", "Projecten", New With {.projectid = Model.Project.Id})">
                                                (Toon Alle)
                                            </a>
                                        </div>
                                        <hr Class="dotted short">
                                        <div class="social-icons-list">
                                            <a rel="tooltip" data-placement="bottom" class="modal-with-form" id="btnAddNews" href="#modaladdnews" data-original-title="Foto toevoegen"><i class="fa fa-plus"></i><span>Foto toevoegen</span></a>
                                        </div>
                                    </div>
                                    @Html.Partial("ModalAddNews", New BO.ProjectNewsBO With {.ProjectId = Model.Project.Id, .NewsDate = Date.Now().Date})
                                </div>

                            </section>
                        </div>
                        <div Class="col-md-4 col-lg-3">
                            <section class="panel">
                                <header class="panel-heading">
                                    <div class="panel-actions">
                                        <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                                        <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
                                    </div>

                                    <h2 class="panel-title">
                                        <span class="va-middle">Foto's</span>
                                    </h2>
                                </header>
                                <div class="panel-body">
                                    <div class="content">
                                        @If Model.LatestPicture IsNot Nothing Then
                                            @<text>
                                                <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.LatestPicture.Name)" Class="img-responsive" alt="John Doe">
                                            </text>
                                        End If
                                        <br />
                                        <div Class="text-right">
                                            <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Fotos" href="@Url.Action("DetailPhotos", "Projecten", New With {.projectid = Model.Project.Id})">
                                                (Toon Alle)
                                            </a>
                                        </div>
                                    </div>
                                    <hr Class="dotted short">
                                    <div Class="social-icons-list">
                                        <a rel="tooltip" data-placement="bottom" Class="modal-with-form" id="btnAddPhoto" href="#modaladdphoto" data-original-title="Foto toevoegen"><i Class="fa fa-plus"></i><span>Foto toevoegen</span></a>
                                    </div>
                                </div>
                                @Html.Partial("ModalAddPhoto", New BO.ProjectPictureBO With {.ProjectId = Model.Project.Id, .Type = BO.PictureType.Werffoto})
                            </section>

                        </div>
                        <div Class="col-md-4 col-lg-3">
                            @If Model.Project.DeliveryDate Is Nothing Then
                                @<text>

                                    <h4>
                                        Termijnen
                                    </h4>

                                    <section Class="panel">

                                        <div Class="panel-body bg-primary">
                                            <div Class="widget-summary widget-summary-md">
                                                <div Class="widget-summary-col widget-summary-col-icon">
                                                    <div Class="summary-icon">
                                                        <i Class="fa fa-building"></i>
                                                    </div>
                                                </div>
                                                <div Class="widget-summary-col">
                                                    <div Class="summary">
                                                        <h4 Class="title">Werkdagen resterend</h4>
                                                        <div Class="info">
                                                            @If Model.WorkingDaysLeft = -9999 Then
                                                                @<text>
                                                                    Kan nog geen werkdagen berekenen, gelieve de startdatum en werkdagen In te geven
                                                                </text>
                                                            Else
                                                                @<text>
                                                                    <strong Class="amount">@Model.WorkingDaysLeft</strong>
                                                                </text>
                                                            End If

                                                        </div>
                                                    </div>

                                                </div>
                                            </div>
                                        </div>
                                    </section>

                                    <section class="panel">
                                        <div class="panel-body bg-primary">
                                            <div class="widget-summary widget-summary-md">
                                                <div class="widget-summary-col widget-summary-col-icon">
                                                    <div class="summary-icon">
                                                        <i class="fa fa-calendar"></i>
                                                    </div>
                                                </div>
                                                <div class="widget-summary-col">
                                                    <div class="summary">
                                                        <h4 class="title">Uiterste einddatum</h4>
                                                        <div class="info">
                                                            @If Model.FinalConstructionDate = DateTime.MinValue Then
                                                                @<text>
                                                                    Kan nog geen datum berekenen, gelieve de startdatum en werkdagen in te geven
                                                                </text>
                                                            Else
                                                                @<text>
                                                                    <strong Class="amount ">@Model.FinalConstructionDate.ToString("ddd dd MMMM yyyy")</strong>
                                                                </text>
                                                            End If

                                                        </div>
                                                    </div>

                                                </div>
                                            </div>
                                        </div>

                                    </section>
                                </text>
                            End If
                            <section class="panel">
                                <header class="panel-heading">
                                    <div class="panel-actions">
                                        <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                                        <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
                                    </div>

                                    <h2 class="panel-title">
                                        <span class="va-middle">Documenten</span>
                                    </h2>
                                </header>
                                <div class="panel-body">
                                    <div class="content">
                                        <ul class="simple-">
                                            @For Each doc In Model.LatestDocs
                                                @<text>
                                                    <li>

                                                        @if doc.Name Is Nothing Or doc.Name = "" Then
                                                            @<text>
                                                                <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"><span Class="title">@doc.Type.GetDisplayName()</span></a>
                                                                @If doc.DocDate.HasValue Then
                                                                    @<text>
                                                                        <span Class="message truncate">@doc.DocDate.Value.ToLongDateString</span>
                                                                    </text>
                                                                Else

                                                                End If

                                                            </text>
                                                        Else
                                                            @<text>
                                                                <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Name</span></a>
                                                                @If doc.DocDate.HasValue Then
                                                                    @<text>
                                                                        <span Class="message truncate">@doc.Type.GetDisplayName() - @doc.DocDate.Value.ToLongDateString</span>
                                                                    </text>
                                                                Else
                                                                    @<text>
                                                                        <span Class="message truncate">@doc.Type.GetDisplayName()</span>
                                                                    </text>
                                                                End If

                                                            </text>

                                                        End If
                                                    </li>
                                                </text>
                                            Next

                                        </ul>


                                        <div Class="text-right">
                                            <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Documenten" href="@Url.Action("DetailDocs", "Projecten", New With {.projectid = Model.Project.Id})">
                                                (Toon Alle)
                                            </a>
                                        </div>
                                        <hr Class="dotted short">
                                        <div class="social-icons-list">
                                            <a rel="tooltip" data-placement="bottom" class="modal-with-form" id="btnAddPhoto" href="#modaladddoc" data-original-title="Document toevoegen"><i class="fa fa-plus"></i><span>Document toevoegen</span></a>
                                        </div>
                                        @Html.Partial("ModalAddDocument", New BO.ProjectDocBO With {.ProjectId = Model.Project.Id, .Type = BO.ProjectDocType.Sales})
                                    </div>
                                </div>

                            </section>
                        </div>

                    </div>









                </div>

            </div>
        </section>



    </div>
</div>


@section scripts

    <script>

        $(window).load(function () {
            @If Not TempData("Message") Is Nothing Then
        @<text>

            new PNotify({
                title: '@TempData("MessageTitle")',
                text: '@TempData("Message")',
                type: '@TempData("MessageType")'
            });
            </text>
            End If
        });


    </script>
    <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
    <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
    <script src="~/Scripts/admin/pages/examples.mediagallery.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
    <script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.charts.js"></script>
end section

