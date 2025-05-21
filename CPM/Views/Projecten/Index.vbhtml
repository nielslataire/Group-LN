@modeltype ShowProjectsModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
<ul class="nav nav-pills sort-source" data-sort-id="portfolio" data-option-key="filter">

    <li data-option-value="*" class="active"><a href="#">Toon Alles</a></li>
    @For Each status In Model.Statuses
        @<text>
    <li data-option-value=".Status@(status.Id)" ><a href="#">@status.Name</a></li>
    @*<li data-option-value=".Finished"><a href="#">Opgeleverd</a></li>
    <li data-option-value=".Design"><a href="#">Ontwerp</a></li>*@
</text>
    Next
</ul>

<hr>

<div class="row">

    <ul id="portfolio" class="portfolio-list sort-destination" data-sort-id="portfolio">
        @For Each project In Model.Projects
            @<text>
        <li class="col-md-3 col-sm-6 col-xs-12 isotope-item Status@(project.Status.Id)">
            <div class="portfolio-item">

                <a href="@(Url.Action("Detail", "Projecten", New With {.projectid = project.Id}))">
                @If project.DefaultPicture.Name Is Nothing Then
                @<text>
                    <span class="thumb-info thumb-info-lighten ">
                        <span class="thumb-info-wrapper">

                            <img src="@Url.Content("~/img/no_image.jpg")" class="img-responsive" alt="" >
                            <span class="thumb-info-title ">
                                <span class="thumb-info-inner">@project.Name </span>
                                <span class="thumb-info-type">@project.Postalcode.Gemeente </span>
                            </span>
                            <span class="thumb-info-action">
                                <span class="thumb-info-action-icon"><i class="fa fa-link"></i></span>
                            </span>
                        </span>
                    </span>
                    </text>
            Else
                    @<text>
                        <span class="thumb-info thumb-info-lighten ">
                            <span class="thumb-info-wrapper">

                                <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & project.DefaultPicture.Name)" class="img-responsive" alt="">
                                <span class="thumb-info-title">
                                    <span class="thumb-info-inner">@project.Name </span>
                                    <span class="thumb-info-type">@project.Postalcode.Gemeente </span>
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

    </ul>
</div>
@section scripts
<script src="~/vendor/isotope/jquery.isotope.js"></script>
    @*<script>
        $(document).ready(function () {
            $("#portfolio").isotope({ filter: ".Status2" });
        });

    </script>*@
End Section