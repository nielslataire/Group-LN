@ModelType WWWBCO.ProjectModel
@Imports extensions
@Code
    ViewData("Title") = "BCO - Woonprojecten"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
end section
<section class="page-header page-header-light page-header-more-padding">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>Woonprojecten</h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">Woonprojecten</li>
                </ul>
            </div>
        </div>
    </div>
</section>


<div class="container ">
    <div class="row">

        <ul class="properties-listing sort-destination p-none">
            @For Each project In Model.Projects
                @<text>
                    <li class="col-md-4 col-sm-6 col-xs-12 p-md isotope-item">

                        <div class="listing-item">
                            <a href="@(Url.Action("ProjectBySlug", "Projects", New With {.slug = project.Slug}))" class="text-decoration-none">
                                @If project.DefaultPicture.Name IsNot Nothing Then
                                    @<text>
                                        <span class="thumb-info thumb-info-lighten">
                                                                                <span class="thumb-info-wrapper m-none">
                                                                                    @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().LivingUnits = 0 Or Model.SalesSettings.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().SaleVisible = False Then
                                                                                    @<text>
                                                                                        <span class="feature-tag background-color-primary" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 10px 90px; position: absolute; right: -24%; top: 6%; transform: rotate(45deg); transition: none; text-align: inherit; line-height: 21px; border-width: 0px; margin: 0px; letter-spacing: 0px; font-weight: 400; font-size: 12px;">
                                                                                            BINNENKORT
                                                                                        </span>
                                                                                    </text>
                                                                                    ElseIf Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold < 15 Then

                                                                                        @<text>
                                                                                            <span class="feature-tag background-color-primary" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 10px 95px; position: absolute; right: -24%; top: 6%; transform: rotate(45deg); transition: none; text-align: inherit; line-height: 21px; border-width: 0px; margin: 0px; letter-spacing: 0px; font-weight: 400; font-size: 12px;">
                                                                                                LANCERING
                                                                                            </span>
                                                                                        </text>
                                                                                 
                                                                                    ElseIf Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold = 100 Then

                                                                                        @<text>
                                                                                            <span class="feature-tag background-color-primary font-weight-bold " data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 10px 90px; position: absolute; right: -24%; top: 6%; transform: rotate(45deg); transition: none; text-align: inherit; line-height: 21px; border-width: 0px; margin: 0px; letter-spacing: 0px; font-weight: 400; font-size: 12px;">
                                                                                                UITVERKOCHT
                                                                                            </span>
                                                                                        </text>

                                                                                    Else
                                                                                        @<text>
                                                                                            <span class="feature-tag background-color-secondary text-md" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 20px 87px;padding-bottom:10px; position: absolute; right: -25%; top: -1%; transform: rotate(45deg); transition: none; text-align: center; line-height: 15px; border-width: 0px; margin: 0px; letter-spacing: 0px;">
                                                                                                <span class="font-weight-bold">@String.Format("{0:n0}", Math.Ceiling(Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold / 5) * 5) %</span><br /><span class="text-xs">verkocht</span>
                                                                                            </span>
                                                                                        </text>
                                                                                    End If
                                                                                    <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & project.DefaultPicture.Name)" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                                                                    <span class="thumb-info-listing-type background-color-primary text-uppercase text-color-light font-weight-semibold p-xs pl-md pr-md">
                                                                                        @project.Name
                                                                                    </span>
                                                                                    <span class="thumb-info-price background-color-secondary text-color-light text-mg p-sm pl-md pr-md">
                                                                                        @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().StartingPrice > 0 Then
                                                                                            @<text>
                                                                                                Vanaf @Html.DisplayFor(Function(i) Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().StartingPrice)
                                                                                            </text>
                                                                                        Else
                                                                                            @<text>
                                                                                                &nbsp;
                                                                                            </text>
                                                                                        End If
                                                                                        <i Class="fa fa-caret-right text-color-light pull-right"></i>
                                                                                    </span>

                                                                                    <span Class="custom-thumb-info-title b-normal p-md">
                                                                                        <span Class="thumb-info-inner text-md text-uppercase">@project.CommercialTitleNL</span>
                                                                                        <span Class="thumb-info-inner text-md text-uppercase font-weight-bold">@project.Postalcode.Gemeente </span><br />
                                                                                        <span class="text-color-dark"><p>@project.CommercialTextNL</p></span>
                                                                                        <ul Class="accommodations text-uppercase p-none font-weight-bold text-sm">
                                                                                            @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberAppartments > 0 Then
                                                                                                @<text>
                                                                                                    <li>
                                                                                                        <span Class="accomodation-title">
                                                                                                            Appartementen:
                                                                                                        </span>
                                                                                                        <span Class="accomodation-value text-color-secondary ">
                                                                                                            @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberAppartments
                                                                                                        </span>
                                                                                                    </li>
                                                                                                </text>
                                                                                            End If
                                                                                            @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberCommercial > 0 Then
                                                                                                @<text>
                                                                                                    <li>
                                                                                                        <span Class="accomodation-title">
                                                                                                            Handelspanden:
                                                                                                        </span>
                                                                                                        <span Class="accomodation-value text-color-secondary">
                                                                                                            @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberCommercial
                                                                                                        </span>
                                                                                                    </li>
                                                                                                </text>

                                                                                            End If
                                                                                            @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberHouses > 0 Then
                                                                                                @<text>
                                                                                                    <li>
                                                                                                        <span Class="accomodation-title">
                                                                                                            Woningen:
                                                                                                        </span>
                                                                                                        <span Class="accomodation-value text-color-secondary">
                                                                                                            @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberHouses
                                                                                                        </span>
                                                                                                    </li>
                                                                                                </text>

                                                                                            End If
                                                                                            @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().LivingUnits = 0 Then
                                                                                                @<text>
                                                                                                    &nbsp;
                                                                                                </text>

                                                                                            End If
                                                                                        </ul>
                                                                                    </span>

                                                                                </span>
                                        </span>
                                    </text>
                                Else
                                @<text>

                                    <span class="thumb-info thumb-info-lighten">
                                        <span class="thumb-info-wrapper m-none">
                                            @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold < 15 Then

                                                @<text>
                                                    <span class="feature-tag background-color-primary" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 10px 95px; position: absolute; right: -24%; top: 6%; transform: rotate(45deg); transition: none; text-align: inherit; line-height: 21px; border-width: 0px; margin: 0px; letter-spacing: 0px; font-weight: 400; font-size: 12px;">
                                                        LANCERING
                                                    </span>
                                                </text>
                                            ElseIf Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold = 100 Then

                                                @<text>
                                                    <span class="feature-tag background-color-primary" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 10px 90px; position: absolute; right: -24%; top: 6%; transform: rotate(45deg); transition: none; text-align: inherit; line-height: 21px; border-width: 0px; margin: 0px; letter-spacing: 0px; font-weight: 400; font-size: 12px;">
                                                        UITVERKOCHT
                                                    </span>
                                                </text>
                                            Else
                                                @<text>
                                                    <span class="feature-tag background-color-secondary text-md" data-width="40" data-height="50" style="color: rgb(255, 255, 255); text-transform: uppercase; padding: 20px 87px;padding-bottom:10px; position: absolute; right: -25%; top: -1%; transform: rotate(45deg); transition: none; text-align: center; line-height: 15px; border-width: 0px; margin: 0px; letter-spacing: 0px;">
                                                        <span class="font-weight-bold">@String.Format("{0:n0}", Math.Ceiling(Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().PercentageLivingUnitsSold / 5) * 5) %</span><br /><span class="text-xs">verkocht</span>
                                                    </span>
                                                </text>
                                            End If
                                            <img src="@Url.Content("~/content/img/no_image.jpg")" class="img-responsive" alt="@project.DefaultPicture.Caption ">
                                            <span class="thumb-info-listing-type background-color-primary text-uppercase text-color-light font-weight-semibold p-xs pl-md pr-md">
                                                @project.Postalcode.Gemeente
                                            </span>
                                            <span class="thumb-info-price background-color-secondary text-color-light text-md p-sm pl-md pr-md">
                                                @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().StartingPrice > 0 Then
                                                    @<text>
                                                        Vanaf @Html.DisplayFor(Function(i) Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().StartingPrice)
                                                    </text>
                                                Else
                                                    @<text>
                                                        &nbsp;
                                                    </text>
                                                End If
                                                <i Class="fa fa-caret-right text-color-light pull-right"></i>
                                            </span>
                                            <span Class="custom-thumb-info-title b-normal p-md">
                                                <span Class="thumb-info-inner text-md">@project.Name</span>
                                                <ul Class="accommodations text-uppercase p-none font-weight-bold text-xs">
                                                    @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberAppartments > 0 Then
                                                        @<text>
                                                            <li>
                                                                <span Class="accomodation-title">
                                                                    Appartementen:
                                                                </span>
                                                                <span Class="accomodation-value text-color-secondary ">
                                                                    @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberAppartments
                                                                </span>
                                                            </li>
                                                        </text>

                                                    End If
                                                    @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberCommercial > 0 Then
                                                        @<text>
                                                            <li>
                                                                <span Class="accomodation-title">
                                                                    Handelspanden:
                                                                </span>
                                                                <span Class="accomodation-value text-color-secondary">
                                                                    @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberCommercial
                                                                </span>
                                                            </li>
                                                        </text>

                                                    End If
                                                    @If Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberHouses > 0 Then
                                                        @<text>
                                                            <li>
                                                                <span Class="accomodation-title">
                                                                    Woningen:
                                                                </span>
                                                                <span Class="accomodation-value text-color-secondary">
                                                                    @Model.SalesData.Where(Function(m) m.ProjectId = project.Id).FirstOrDefault().NumberHouses
                                                                </span>
                                                            </li>
                                                        </text>

                                                    End If
                                                </ul>
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
    <script src="~/scripts/real-estate.js"></script>
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