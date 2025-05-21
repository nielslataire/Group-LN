@modeltype ProjectPaymentStagesAddUpdateModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />

End Section
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>
<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu media-gallery">
            <div class="content-with-menu-container">
                <div class="inner-menu-toggle">
                    <a href="#" class="inner-menu-expand" data-open="inner-menu">
                        Toon Menu <i class="fa fa-chevron-right"></i>
                    </a>
                </div>

                <menu id="content-menu" class="inner-menu" role="menu">
                    <div class="nano">
                        <div class="nano-content">

                            <div class="inner-menu-toggle-inside">
                                <a href="#" class="inner-menu-collapse">
                                    <i class="fa fa-chevron-up visible-xs-inline"></i><i class="fa fa-chevron-left hidden-xs-inline"></i> Verberg Menu
                                </a>
                                <a href="#" class="inner-menu-expand" data-open="inner-menu">
                                    Toon Menu <i class="fa fa-chevron-down"></i>
                                </a>
                            </div>
                            <div class="inner-menu-content">
                                <div class="sidebar-widget m-none">
                                    <div class="widget-header clearfix">
                                        <a href="#Project" data-toggle="tab"> <h5 class="title pull-left mt-xs">Projectmenu</h5></a>
                                    </div>
                                    <div class="widget-content">
                                        <ul class="mg-folders">
                                            @Html.Partial("DetailMenu", Model.ProjectId)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>

                <div class="inner-body mg-main">

                    <header class="panel-heading">
                        <div class="panel-actions">
                            <a href="#" class="panel-action panel-action-toggle" data-panel-toggle></a>
                            <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss></a>
                        </div>

                        <h2 class="panel-title">Betalingschijven toevoegen</h2>
                    </header>

                    <div class="panel-body">
                        @Html.Partial("_ValidationSummary", ViewData.ModelState)
                        @Using Html.BeginForm("PaymentStagesAddUpdate", "Projecten", FormMethod.Post, New With {.id = "frmAddStage", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
                        @Html.AntiForgeryToken()
                            @Html.HiddenFor(Function(m) m.ProjectId)
                            @Html.HiddenFor(Function(m) m.ProjectName)
                            @Html.HiddenFor(Function(m) m.Group.Id)
                        @<text>
                            <div Class="form-group">
                                <Label Class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Group.Name)</Label>
                                <div class="col-md-6">
                                    @Html.TextBoxFor(Function(m) m.Group.Name, New With {.class = "form-control", .id = "txtName"})
                                </div>
                                <Label Class="col-md-1 control-label">@Html.LabelFor(Function(m) m.Group.VatPercentage)</Label>
                                <div class="col-md-2">
                                    @Html.EditorFor(Function(m) m.Group.VatPercentage, New With {.class = "form-control", .id = "txtpercentage"})
                                </div>
                            </div>
                            <hr />
                            <h4 class="text-center text-capitalize  ">betalingsschijven</h4>
                            <div class="form-group">
                                <div class="col-md-2 control-label">
                                    
                                </div>
                                <div class="col-md-7">
                                   Naam
                                </div>
                                <div class="col-md-2">
                                   Percentage
                                </div>
                                <div class="col-md-1">
                                    
                                </div>
                            </div>
                            <div class="form-group">

                                <div id="editorRows">
                                    @For Each item In Model.Stages
                                        Html.RenderPartial("_PaymentStageRow", item)
                                    Next

                                </div>
                            </div>
                            <div class="form-group">
                                <div class="col-md-12 align-right">
                                    <button class="btn btn-default btn-block " type="button" id="btnAddStage"><i class="fa fa-plus mr-md"></i>Schijf toevoegen</button>
                                </div>
                            </div>
                            <div class="form-group">
                                <div class="col-md-12 align-right">
                                    <button class="btn btn-primary btn-block">Betalingsschijven opslaan</button>
                                </div>
                            </div>
                        </text>
                        End Using
                    </div>









                </div>

            </div>


            @section scripts
            <script>
                    //Add stage row
                    $("#btnAddStage").click(function () {
                        $.ajax({
                            url: "BlankStageRow",
                            cache: false,
                            traditional: true,
                            type: 'POST',
                            success: function (html) {
                                $("#editorRows").append(html);

                            }
                        });
                        return false;
                    });
                    //Delete Stage Row
                    $(document).on('click', 'a.deleteRow', function () { // <-- changes
                        $(this).closest('.form-group').remove();
                        return false;
                    });

                    $(window).load(function () {
                        @If Not TempData("Message") Is Nothing Then
                        @<text>

                        new PNotify({
                            title: '@TempData("MessageTitle")',
                            text: '@TempData("Message")',
                            type: '@TempData("MessageType")'
                        });
                        </text>
                        End If
                    });


            </script>
                <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
                <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
                <script src="~/Scripts/admin/pages/examples.mediagallery.js"></script>
                <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
                <script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
                <script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
                <script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
                <script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
                <script src="~/scripts/admin/tables/examples.datatables.default.js"></script>
                <script src="~/vendor/admin/select2/select2.js"></script>
                <script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
                <script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
                <script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
            End Section
