@ModelType WWWBCO.ProjectPhotosModel
@Imports WWWBCO.extensions
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
<section class="page-header page-header-light page-header-more-padding">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>Foto's</h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional}))">Woonprojecten</a></li>
                    <li><a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = Model.ProjectSlug}))">@Model.ProjectName </a></li>
                    <li class="active">Foto's</li>
                </ul>
            </div>
        </div>
    </div>
</section>


<div class="container">

    <div class="row">
        <div class="col-md-12">
            <div class="portfolio-title">
                <div class="row">
                    <div class="portfolio-nav-all col-md-1">
                        <a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = Model.ProjectSlug}))" class="portfolio-nav-prev" data-tooltip data-original-title="Terug naar @Model.ProjectName"><i class="fa fa-chevron-left"></i></a>
                    </div>
                    <div class="col-md-10 center">
                        <h2 class="mb-none">@Model.ProjectName te @System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Model.ProjectCity.ToLower()) </h2>
                    </div>
                </div>
            </div>

            <hr class="tall">
        </div>
    </div>

    <div class="row">
        <section class="media-gallery">
            <div class="inner-body mg-main">
                <div class="row mg-files" data-sort-destination data-sort-id="media-gallery">
                    @For Each picture In Model.Photos
                        @<text>
                            <div class="isotope-item image col-sm-4 col-md-3 col-lg-3">
                                <div class="thumbnail">
                                    <div class="thumb-preview">
                                        <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                                        </a>
                                        <div class="mg-thumb-options">
                                            <div class="mg-zoom"><i class="fa fa-search"></i></div>
                                           <div class="mg-toolbar">
                                                @*<div class="mg-option checkbox-custom checkbox-inline">
                                            <input type="checkbox" id="check_@picture.Id" value="@picture.Id">
                                            <label for="check_@picture.Id">SELECT</label>
                                        </div>*@
                                                <div class="mg-group">
                                                    <h5 class="text-light ">@picture.Caption</h5>
                                                    <div class="minitext">@picture.DateTimeUploaded.ToString("dd/MM/yyyy")</div>

                                                   
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    @*<div class="mg-title small ">@picture.Caption </div>*@
                                    @*<div class="mg-description">
                                        <small class="pull-left text-muted">@picture.Type</small>
                                        <small class="pull-right text-muted">@picture.DateTimeUploaded.ToShortDateString</small>
                                    </div>*@
                                </div>
                            </div>
                        </text>
                    Next

                </div> 

            </div>
        </section>
    </div>
   
   
</div>

@section scripts

    <script src="~/Scripts/examples.mediagallery.js"></script>


    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });

    </script>
End section
@section LatestNews
    <h4>Recente <strong>berichten</strong></h4>

    <ul class="nav nav-list mb-xl">
        @For Each news In ViewData("LatestNews")
            @<text>
                <li><a title="@news.news.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.news.TitleNL</a></li>
            </text>

        Next

    </ul>
End Section