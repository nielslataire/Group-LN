@ModelType WWWCOPRO.ReferencesModel
@Code
    ViewData("Title") = "Group LN - Realisaties"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
end section
<section class="page-header page-header-light page-header-more-padding">
    
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <ul class="breadcrumb">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">Realisaties</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <h1>Realisaties</h1>
            </div>
        </div>
    </div>
</section>

<div class="container">
    <div class="row">
        <ul class="properties-listing sort-destination p-none">
            @For Each group In Model.Projects.GroupBy(Function(m) m.DeliveryDate.Value.Year)
            @<text>
                @For Each project In group
            @<text>

            <li class="col-md-4 col-sm-6 col-xs-12 p-md isotope-item">
                <div class="listing-item">
                    <a href="@(Url.Action("ReferenceBySlug", "References", New With {.slug = project.Slug}))">
                        @If project.DefaultPicture.Name IsNot Nothing Then
                            @<text>
                                <span class="thumb-info thumb-info-lighten">
                                    <span class="thumb-info-wrapper m-none">
                                        <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & project.DefaultPicture.Name)" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                        <span class="thumb-info-listing-type background-color-primary text-uppercase text-color-light font-weight-semibold p-xs pl-md pr-md">
                                            @project.Postalcode.Gemeente
                                        </span>
                                        <span class="thumb-info-price background-color-secondary text-color-light text-md p-sm pl-md pr-md">
                                            @project.Name
                                            <i Class="fa fa-caret-right text-color-light pull-right"></i>
                                        </span>

                                        <span Class="custom-thumb-info-title b-normal p-md">
                                            <span Class="thumb-info-inner text-uppercase">Opgeleverd : @project.DeliveryDate.Value.Year</span>
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
