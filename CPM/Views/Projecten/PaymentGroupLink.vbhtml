@modeltype ProjectPaymentGroupLinkModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />

End Section

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
                    @Using Html.BeginForm("PaymentGroupLink", "Projecten", FormMethod.Post, New With {.id = "FormGroupLink", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
                        @Html.AntiForgeryToken()
                        @<text>
                    @Html.HiddenFor(Function(m) m.ProjectId)
                  
                    <div class="row mt-xl">
                        <h3>Betalingsschijven koppelen</h3>
                    </div>
                    <hr />
                    <div class="row">
                        @for value As Integer = 0 To Model.Units.Count - 1
                            @<text>
                            @Html.HiddenFor(Function(m) m.Units(value).Id)
                            <div class="form-group">
                                <label class="col-md-2 control-label">@Html.DisplayFor(Function(m) m.Units(value).Type.Name) @Html.DisplayFor(Function(m) m.Units(value).Name)</label>
                                <div Class="col-md-10">
                                    @Html.DropDownListFor(Function(m) m.Units(value).PaymentGroupId, New SelectList(Model.PaymentGroups, "ID", "Display", Model.Units(value).PaymentGroupId), "Selecteer ....", New With {.class = "form-control populate", .id = "lstGroupTypes"})
                                </div>
                                
                            </div>
                            </text>
                        Next
                    </div>
                    <br />
                    <div class="row">
                        <div class="col-md-12 text-right">
                            <button class="btn btn-primary btn-block ">Opslaan</button>
                        </div>
                    </div>
                    </text>
                    End Using






        <!-- end Page -->

    </div>
</div>

@section scripts
    <script>
     
       $(window).load(function () {
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


    </script>
            <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
            <script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
            <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
            <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
End Section
