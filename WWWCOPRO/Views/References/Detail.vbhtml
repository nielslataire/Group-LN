@ModelType WWWCOPRO.ReferenceDetailModel
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
                    <li><a href="@(Url.Action("Index", "References", New With {.id = UrlParameter.Optional}))">Realisaties</a></li>
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
                        <a href="@(Url.Action("Index", "References", New With {.id = UrlParameter.Optional}))" data-tooltip data-original-title="Terug naar onze realisaties"><i class="fa fa-th"></i></a>
                    </div>
                    <div class="col-md-10 center">
                        <h2 class="mb-none">@Model.Data.Name te @System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Model.Data.Postalcode.Gemeente.ToLower()) </h2>
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
        <div class="col-md-9">
            <div class="col-md-5">

                <div class="owl-carousel owl-theme" data-plugin-options='{"items": 1, "margin": 10}'>
                            @If Not Model.Data.DefaultPicture Is Nothing Or Model.Data.DefaultPicture.Id = 0 Then
                                @<text>
                                    <div>
                                        <span class="img-thumbnail">
                                            <img alt="" class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.Data.DefaultPicture.Name)">
                                        </span>
                                    </div>
                                </text>
                            End If

                            @For Each picture In Model.Data.Pictures
                                If picture.Type = BO.PictureType.Nevenfoto Then
                                @<text>
                                    <div>
                                        <span class="img-thumbnail">
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

        </div>
        <div class="col-md-3">
            <aside class="sidebar">
                <h5 class="heading-primary text-center">Ligging</h5>
                <div class="divider">
                    <i class="fa fa-chevron-down"></i>
                </div>
                <div class="col-md-offset-1 col-md-11">
                    <ul class="list list-icons">
                        <li class="mb-none">
                            <i class="fa fa-map-marker"></i>@Model.Data.Street @Model.Data.HouseNumber <br />@Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente
                        </li>
                    </ul>
                </div>
                <br />
                <h5 class="heading-primary text-center">Bouwdirectie</h5>
                <div class="divider">
                    <i class="fa fa-chevron-down"></i>
                </div>
                <div class="toggle toggle-primary mt-lg" data-plugin-toggle>
                    @If Not Model.Developer Is Nothing Then
                        @<text>
                            <section class="toggle">
                                @Html.LabelFor(Function(m) m.Developer)
                                <div class="toggle-content">
                                    @Html.Partial("Adressblock", Model.Developer)
                                </div>
                            </section>
                        </text>
                    End If
                    @If Not Model.Builder Is Nothing Then
                        @<text>
                            <section class="toggle">
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