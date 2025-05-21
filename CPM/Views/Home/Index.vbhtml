@Modeltype HomeModel
@Code
    ViewData("Title") = "Home"
End Code
@section PageStyle
    <link href="~/vendor/admin/jstree/themes/default/style.css" rel="stylesheet" />
    <link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
end section
<div class="col-md-8">
    <div class="row">

        <div class="col-md-12"><h4>Uw actuele projecten</h4></div>

        <ul class="portfolio-list sort-destination" data-sort-id="portfolio">
            @For Each project In Model.Projects
                    @<text>
                        <li class="col-lg-4 col-md-6 col-sm-12 isotope-item Status@(project.Status.Id)">
                            <div class="portfolio-item">

                                <a href="@(Url.Action("Detail", "Projecten", New With {.projectid = project.Id}))">
                                    @If project.DefaultPicture.Name Is Nothing Then
                                        @<text>
                                            <span class="thumb-info thumb-info-lighten ">
                                                <span class="thumb-info-wrapper">

                                                    <img src="@Url.Content("~/img/no_image.jpg")" class="img-responsive" alt="">
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
    <div class="row">

        <div class="col-md-12"><h4>Uw afgewerkte projecten</h4></div>

        <ul class="portfolio-list sort-destination" data-sort-id="portfolio">
            @For Each project In Model.OldProjects.OrderByDescending(Function(m) m.DeliveryDate)
                @<text>
                    <li class="col-lg-4 col-md-6 col-sm-12 isotope-item Status@(project.Status.Id)">
                        <div class="portfolio-item">

                            <a href="@(Url.Action("Detail", "Projecten", New With {.projectid = project.Id}))">
                                @If project.DefaultPicture.Name Is Nothing Then
                                    @<text>
                                        <span class="thumb-info thumb-info-lighten ">
                                            <span class="thumb-info-wrapper">

                                                <img src="@Url.Content("~/img/no_image.jpg")" class="img-responsive" alt="">
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



</div>
<div class="col-md-4">
    <div class="row">
        <h4>Snelzoeken</h4>

        @*@Html.HiddenFor(Function(m) m.SelectedSearch.ID, New With {.id = "txtGeneralSearch", .class = "form-control"})*@
        @Html.HiddenFor(Function(m) m.SelectedSearch.ID, New With {.id = "txtGeneralSearch", .class = "form-control populate"})


    </div>
    <br />
    <div class="row">
                <h4>Berichtencentrum</h4>
        @If User.IsInRole("Admin") Then
        @For Each account In Model.DeedofSaleWarnings
            @<text>
                <div Class="alert alert-danger">
                    <Button Class="close" aria-hidden="true" type="button" data-dismiss="alert">×</Button>
                    Akte <strong><a class="alert-danger" href="@Url.Action("Detail", "Klanten", New With {Key .clientid = account.Id})">@account.DisplayName </a></strong> te verlijden voor <strong> @account.DateSalesAgreement.Value.AddMonths(4).ToShortDateString  </strong>.
                </div>

            </text>
        Next
                 End If
        @For Each warning In Model.InsuranceWarnings
            @<text>
                <div Class="alert alert-@warning.Type">
                    <Button Class="close" aria-hidden="true" type="button" data-dismiss="alert">×</Button>
                    <strong><a class="alert-@warning.Type" href="@Url.Action("DetailInsurances", "Projecten", New With {Key .projectid = warning.ProjectId})">@warning.Display </a></strong>
                </div>

            </text>
        Next
        @For Each info In Model.ProjectInfo.OrderBy(Function(m) m.Type)
            @<text>
                <div Class="alert alert-@info.Type">
                    <Button Class="close" aria-hidden="true" type="button" data-dismiss="alert">×</Button>
                    <a class="alert-@info.Type" href="@Url.Action("Detail", "Projecten", New With {Key .projectid = info.ProjectId})">@info.Display </a>
                </div>
            </text>
        Next

    </div>
</div>



@section scripts
    <script>
        $(window).load(function () {
            //Berichtencentrum
      @If Not TempData("Message") Is Nothing Then
      @<text>

      new PNotify({
          title:      '@TempData("MessageTitle")',
          text:       '@TempData("Message")',
          type:       '@TempData("MessageType")'
      });
            </text>
End If
        });
        $(document).ready(function () {

            $("#txtGeneralSearch").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Zoeken ....",
                ajax: {

                    url: '@Url.Action("GetGeneralSearchList","Home")',
                    cache: false,
                    traditional: true,
type:                   'POST',
                    data: function (term) {
                        return {
                            term: term,
                        };
                    },

                    results: function (data, page) {
                        return { results: data };
                    },

                },
            });
        });
        $("#txtGeneralSearch").on("change", function () {

            
            $.ajax({
                url: '@Url.Action("SelectSearchItem", "Home")',
                data: { id: $("#txtGeneralSearch").select2('data').id, type: $("#txtGeneralSearch").select2('data').extra },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    window.location.href = result;
                },

            });
        });
     </script>
<script src="~/vendor/admin/select2/select2.js"></script>
<script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
<script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
<script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
    end section

