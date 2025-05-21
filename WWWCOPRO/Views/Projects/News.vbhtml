@ModelType WWWCOPRO.ProjectNewsModel
@Imports wwwcopro.extensions
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@Imports wwwcopro.extensions
<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <ul class="breadcrumb">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional}))">Woonprojecten</a></li>
                    <li><a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = Model.ProjectSlug}))">@Model.ProjectName </a></li>
                    <li class="active">Nieuws</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <h1>Nieuws</h1>
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
                        <a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = Model.ProjectSlug}))" class="portfolio-nav-prev" data-tooltip data-original-title="Terug naar @Model.ProjectName "><i class="fa fa-chevron-left"></i></a>
                    </div>
                    <div class="col-md-10 center">
                        <h2 class="mb-none">@Model.ProjectName te @System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Model.ProjectCity.ToLower()) </h2>
                    </div>
                    @*<div class="portfolio-nav col-md-1">
                        <a href="portfolio-single-project.html" class="portfolio-nav-prev" data-tooltip data-original-title="Previous"><i class="fa fa-chevron-left"></i></a>
                        <a href="portfolio-single-project.html" class="portfolio-nav-next" data-tooltip data-original-title="Next"><i class="fa fa-chevron-right"></i></a>
                    </div>*@
                </div>
            </div>

            <hr class="tall">
        </div>
    </div>

    <div class="row">
        <div class="blog-posts">
        @For Each news In Model.News.Where(Function(m) m.Id = Model.NewsId)
            @<text>


                            <article class="post post-large blog-single-post">
                                <div class="post-image">

                                    <div>
                                        <div class="img-thumbnail">
                                            <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & news.Picture.Name)" alt="">
                                        </div>
                                    </div>

                                </div>

                                <div class="post-date">
                                    <span class="day">@news.NewsDate.Day </span>
                                    <span class="month">@news.NewsDate.ToString("MMM")</span>
                                </div>

                                <div class="post-content">

                                    <h2>@news.TitleNL</h2>

                                    <div class="post-meta">
                                        <span><i class="fa fa-user"></i> Door @news.Author  </span>
                                    </div>
                                    <p>@news.TextNL </p>
                                </div>
                            </article>

            <h3 Class="mb-lg mt-lg text-uppercase">Meer <strong>nieuws</strong></h3>
            </text>

        Next
            @For Each news In Model.News
                If Not news.Id = Model.NewsId Then


                    If news.Picture.Id = 0 Then
                @<text>
                    <article class="post post-large">

                        <div class="post-date">
                            <span class="day">@news.NewsDate.Day </span>
                            <span class="month">@news.NewsDate.ToString("MMM")</span>
                        </div>

                        <div class="post-content">
                            <h2>@news.TitleNL </h2>
                            <p>@news.TextNL </p>

                        </div>

                    </article>
                </text>
        Else
                @<text>
                    <article class="post post-medium">
                        <div class="row">
                            <div class="col-md-7">
                                <div class="post-date">
                                    <span class="day">@news.NewsDate.Day </span>
                                    <span class="month">@news.NewsDate.ToString("MMM")</span>
                                </div>
                                <div class="post-content">

                                    <h4>@news.TitleNL</h4>
                                    <p>@news.TextNL </p>

                                </div>

                            </div>
                            <div class="col-md-5">
                                <div class="post-image">
                                    @*<div class="owl-carousel owl-theme" data-plugin-options='{"items":1}'>
                                    <div>*@
                                    <div class="img-thumbnail">
                                        <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & news.Picture.Name)" alt="">
                                    </div>
                                    @*</div>
                                        <div>
                                            <div class="img-thumbnail">
                                                <img class="img-responsive" src="img/blog/blog-image-2.jpg" alt="">
                                            </div>
                                        </div>
                                    </div>*@
                                </div>
                            </div>



                        </div>

                    </article>
                </text>
        End If
    Else

    End If
Next
        </div>
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