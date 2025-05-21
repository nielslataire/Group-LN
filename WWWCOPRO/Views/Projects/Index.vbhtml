@ModelType WWWCOPRO.ProjectModel
@Imports wwwcopro.extensions
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
end section
<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <ul class="breadcrumb"> 
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">
                        @If Model.Projects.FirstOrDefault.ProjectType = BO.ProjectType.Woonproject Then @<text>Woonproject</text> else @<text>Commercieel</text> End if </li>
                                        </ul>
                                    </div>
                                </div>
                                <div Class="row">
                                    <div Class="col-md-12">
                                        <h1> @If Model.Projects.FirstOrDefault.ProjectType = BO.ProjectType.Woonproject Then@<text>Woonprojecten</text> else @<text>Commerciële projecten</text>End if </h1>
                                    </div>
                                </div>
                            </div>
</section>

<div Class="container">
                            <div Class="row">

                                <ul Class="properties-listing sort-destination p-none" data-sort-id="portfolio">
                        @For Each project In Model.Projects
                        @<text>
<li class="col-md-4 col-sm-6 col-xs-12 p-md isotope-item">
                
                    <div class="listing-item">
                        <a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = project.Slug}))">
                            @If project.DefaultPicture.Name IsNot Nothing Then
                                @<text>
                                    <span class="thumb-info thumb-info-lighten">
                                        <span class="thumb-info-wrapper m-none">
                                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & project.DefaultPicture.Name)" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                            <span class="thumb-info-listing-type background-color-primary text-uppercase text-color-light font-weight-semibold p-xs pl-md pr-md">
                                                @project.Postalcode.Gemeente
                                            </span>
                                            <span Class="custom-thumb-info-title b-normal p-md">
                                                <span Class="thumb-info-inner text-weight-bold text-uppercase" >@project.Name</span>
                                            </span>
                                        </span>              
                                    </span>
                                    
                                </text>
                            Else
                                @<text>
                                    <span class="thumb-info thumb-info-lighten">
                                        <span class="thumb-info-wrapper m-none">
                                            <img src="@Url.Content("~/content/img/no_image.jpg")" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                            <span class="thumb-info-listing-type background-color-primary text-uppercase text-color-light font-weight-semibold p-xs pl-md pr-md">
                                                @project.Postalcode.Gemeente
                                            </span>
                                            <span Class="custom-thumb-info-title b-normal p-md">
                                                <span Class="thumb-info-inner text-md">@project.Name</span>
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
                <li><a title="@news.news.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.news.TitleNL</a></li>
            </text>

        Next

    </ul>
End Section