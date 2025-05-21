@ModelType WWWCOPRO.ProjectDetailModel
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
                    <li class="active">@Model.Data.Name</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <h1>Projectgegevens</h1>
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
                        <a href="@(Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional}))" data-tooltip data-original-title="Terug naar de woonprojecten"><i class="fa fa-th"></i></a>
                    </div>
                    <div class="col-md-10 center">
                        <h2 class="mb-none">@Model.Data.Name te @System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Model.Data.Postalcode.Gemeente.ToLower()) </h2>
                    </div>
                </div>
            </div>

            <hr class="tall">
        </div>
    </div>

    <div class="row">
        <div class="col-md-9">
            <div class="col-md-5 m-none p-none">

                <div class="owl-carousel owl-theme" data-plugin-options='{"items": 1, "margin": 0}'>
                    @If Not Model.Data.DefaultPicture Is Nothing Or Model.Data.DefaultPicture.Id = 0 Then
                        @<text>
                        <div>
                            <span class="img-thumbnail" style="border:0;padding:0;margin:0;border-radius:0;">
                                <img alt="" class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.Data.DefaultPicture.Name)">
                            </span>
                        </div>
                        </text>
                    End If
                  
                    @For Each picture In Model.Data.Pictures
                        If picture.Type = BO.PictureType.Nevenfoto Then
                        @<text>
                            <div>
                                <span class="img-thumbnail" style="border:0;padding:0;margin:0;border-radius:0;">
                                    <img alt="" class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)">
                                </span>
                            </div>
                        </text>
                        End If
                    Next

                </div>

            </div>

            <div class="col-md-7">



                <h4 class="heading-primary">@Model.Data.CommercialTitleNL</h4>
                <p class="mt-xlg">@Model.Data.CommercialTextNL</p>

                
                
               

            </div>
            @If Not Model.Data.Pictures.Count = 0 Then

            @<text>
            <div class="row">
                <div class="col-md-12">

                    <hr class="tall">

                    <h4 class="mb-md text-uppercase">Recentste <strong>Foto's</strong></h4>

                    <div class="row media-gallery">
                        <div class="row mg-files" data-sort-destination data-sort-id="media-gallery">
                            
                                @If Model.Data.Pictures.Count > 8 Then
                            @For Each picture In Model.Data.Pictures.GetRange(0, 8)
                            @<text>
                            <div class="isotope-item image col-sm-6 col-md-4 col-lg-3">
                                <div class="thumbnail ">
                                    <div class="thumb-preview">
                                        <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                                        </a>
                                        <div class="mg-thumb-options">
                                            <div class="mg-zoom"><i class="fa fa-search"></i></div>
                                            
                                        </div>
                                    </div>

                                </div>
                            </div>
                            </text>
                                Next
                                                                Else
                            @For Each picture In Model.Data.Pictures.GetRange(0, Model.Data.Pictures.Count)
                            @<text>
                            <div class="isotope-item image col-sm-6 col-md-4 col-lg-3">

                                <div class="thumbnail thumbnail-no-borders">
                                    <div class="thumb-preview">
                                        <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                                        </a>
                                        <div class="mg-thumb-options">
                                            <div class="mg-zoom"><i class="fa fa-search"></i></div>

                                        </div>
                                    </div>

                                </div>
                            </div>
                            </text>
                                Next
                            End If

      
                            </div>
                        </div>
                </div>
            </div>
            </text>
            End If
            @If Not Model.News.Count = 0 Then

                @<text>
            <div class="row">
                <div class="col-md-12">

                    <hr class="tall">

                    <h4 class="mb-md text-uppercase">Recentste <strong>berichten</strong></h4>
                    <div class="recent-posts mb-xl">
                        <div class="row">
                            @*<div class="owl-carousel owl-theme mb-none" data-plugin-options='{"items": 1}'>*@
                                <div>
                                  
                                    @If Model.News.Count > 2 Then
                                        @For Each newsitem In Model.News.GetRange(0, 2)
                                        @<text>
                                            <div class="col-md-6">
                                                <article>
                                                    <div class="date">
                                                        <span class="day">@newsitem.NewsDate.Day </span>
                                                        <span class="month">@newsitem.NewsDate.ToString("MMM")</span>
                                                    </div>
                                                    <h5 class="heading-primary"><a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id, .newsid = newsitem.Id}))">@newsitem.TitleNL </a></h5>
                                                    <p>@TrimTo(newsitem.TextNL, 200) <a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id, .newsid = newsitem.Id}))" class="read-more">read more <i class="fa fa-angle-right"></i></a></p>
                                                </article>
                                            </div>
                                        </Text>
                                        Next
                                        Else
                                        @For Each newsitem In Model.News.GetRange(0, Model.News.Count)
                                        @<text>
                                            <div class="col-md-6">
                                                <article>
                                                    <div class="date">
                                                        <span class="day">@newsitem.NewsDate.Day </span>
                                                        <span class="month">@newsitem.NewsDate.ToString("MMM")</span>
                                                    </div>
                                                    <h5 class="heading-primary"><a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))">@newsitem.TitleNL </a></h5>
                                                    <p>@TrimTo(newsitem.TextNL, 200)  <a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))" class="read-more">read more <i class="fa fa-angle-right"></i></a></p>
                                                </article>
                                            </div>
                                        </Text>
                                        Next
                                    End If

                                   
                                  
                                   
                                </div>
                               
                            @*</div>*@
                        </div>
                        </div>

                      
                    </div>
            </div>
        </text>
            End If
        </div>
        <div class="col-md-3">
            <aside class="sidebar">
                @If Not Model.Data.Pictures.Count = 0 Or Not Model.News.Count = 0 Then
                    @<text>

                        <h5 class="heading-primary text-center">Meer Info</h5>
                        <div class="divider">
                            <i class="fa fa-chevron-down"></i>
                        </div>
                        @If Not Model.Data.Pictures.Count = 0 Then
                            @<text>
                                <a href="@(Url.Action("Photos", "Projects", New With {.slug = Model.Data.Slug}))" class="btn btn-primary btn-icon btn-block "><i class="fa fa-picture-o"></i>Alle foto's</a>
                            </text>
                        End If
                        @If Not Model.News.Count = 0 Then
                            @<text>
                                <a href="@(Url.Action("News", "Projects", New With {.slug = Model.Data.Slug}))" class="btn btn-primary btn-icon btn-block mb-md  "><i class="fa fa-newspaper-o"></i>Alle nieuwsberichten</a>
                                <br />
                            </text>
                        Else
                            @<text>
                                <br />
                            </text>
                        End If

                    </text>
                End If

                
                
                <h5 class="heading-primary text-center">Bouwdirectie</h5>
                <div class="divider">
                    <i class="fa fa-chevron-down"></i>
                </div>
                <div class="toggle toggle-primary mt-lg" data-plugin-toggle="" data-plugin-options='{ "isAccordion": true }'>
                    @If Not Model.Developer Is Nothing Then
                        @<text>
                            <section class="toggle active">
                                @Html.LabelFor(Function(m) m.Developer)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.Developer)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.Builder Is Nothing Then
                        @<text>
                            <section class="toggle active">
                                @Html.LabelFor(Function(m) m.Builder)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.Builder)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.Architect Is Nothing Then
                        @<text>
                            <section class="toggle">
                                @Html.LabelFor(Function(m) m.Architect)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.Architect)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.Engineer Is Nothing Then
                        @<text>
                            <section class="toggle">
                                @Html.LabelFor(Function(m) m.Engineer)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.Engineer)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.SecurityCoordinator Is Nothing Then
                        @<text>
                            <section class="toggle">
                                @Html.LabelFor(Function(m) m.SecurityCoordinator)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.SecurityCoordinator)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.EpbReporter Is Nothing Then
                        @<text>
                            <section class="toggle">
                                @Html.LabelFor(Function(m) m.EpbReporter)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.EpbReporter)
                                </div>
                            </section>
                        </text>
                    End If

                </div>

                
              
                   
                  
            </aside>
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