@ModelType WWWBCO.ReferencesModel
@Code
    ViewData("Title") = "BCO - Realisaties"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
<section class="page-header page-header-light page-header-more-padding">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>Realisaties</h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">Realisaties</li>
                </ul>
            </div>
        </div>
    </div>
</section>

<div class="container">
    <div class="row">
        <ul class="portfolio-list portfolio-list-no-margins sort-destination" data-sort-id="portfolio">
            @For Each group In Model.Projects.GroupBy(Function(m) m.DeliveryDate.Value.Year)
                @<text>



                    @*<li class="col-md-3 col-sm-6 col-xs-12 isotope-item websites m-none p-none">
                        <div class="portfolio-item ">



                            <span class="thumb-info thumb-info-no-borders  thumb-info-centered-info2">
                                <span class="thumb-info-wrapper2">
                                    <img src="@Url.Content("~/content/img/gallery-year.png")" class="img-responsive">
                                    <span class="thumb-info-title2">
                                        <span class="thumb-info-inner">@group.Key</span>

                                    </span>

                                </span>
                            </span>



                        </div>
                    </li>*@

                    @For Each project In group
                        @<text>

                            <li class="col-md-4 col-sm-6 col-xs-12 p-md isotope-item">
                                <div class="portfolio-item">
                                    <a href="@(Url.Action("ReferenceBySlug", "References", New With {.slug = project.Slug}))">
                                        @If project.DefaultPicture.Name IsNot Nothing Then
                                            @<text>
                                                <span class="thumb-info thumb-info-no-borders thumb-info-lighten">
                                                    <span class="thumb-info-wrapper">
                                                        <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & project.DefaultPicture.Name)" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                                        <span class="thumb-info-title">
                                                            <span class="thumb-info-inner">@project.Name</span>
                                                            <span class="thumb-info-type">@project.Postalcode.Gemeente</span>
                                                        </span>
                                                        
                                                            <span class="thumb-info-action">
                                                                <span class="thumb-info-action-icon"><i class="fa fa-link"></i></span>
                                                            </span>
                                                        </span>
                                                </span>
                                            </text>
                                        Else
                                            @<text>
                                                <span class="thumb-info thumb-info-no-borders thumb-info-lighten">
                                                    <span class="thumb-info-wrapper">
                                                        <img src="@Url.Content("~/content/img/no_image.jpg")" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                                        <span class="thumb-info-title">
                                                            <span class="thumb-info-inner">@project.Name</span>
                                                            <span class="thumb-info-type">@project.Postalcode.Gemeente</span>
                                                        </span>
                                                        <span class="thumb-info-action">
                                                            <span class="thumb-info-action-icon"><i class="fa fa-link"></i></span>
                                                        </span>
                                                    </span>
                                                </span>
                                            </text>
                                        End If
                                    </a>
                                </div>
                            </li>
                        </text>
                    Next




                </text>
            Next
        </ul>

    </div>
</div>

@section scripts
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
                <li><a title="@news.News.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.News.TitleNL</a></li>
            </text>

        Next

    </ul>
End Section
