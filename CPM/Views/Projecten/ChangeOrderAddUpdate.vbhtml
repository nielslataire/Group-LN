@modeltype ProjectChangeOrderAddUpdateModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/vendor/admin/summernote/summernote.css" />
    <link rel="stylesheet" href="~/vendor/admin/summernote/summernote-bs3.css" />

End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu  content-with-menu-has-toolbar media-gallery">
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
                    @Html.Partial("_ValidationSummary", ViewData.ModelState)
                    @Using Html.BeginForm("ChangeOrderAddUpdate", "Projecten", FormMethod.Post, New With {.id = "FormAddChangeOrder", .class = "form-horizontal mb-lg"})

                        @<text>
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">
                                        Wijzigingsopdracht
                                        @If Not Model.ChangeOrder.Id = 0 Then
                                            @:Bewerken
                                        Else
                                            @:Toevoegen
                                        End If
                                        :
                                    </h2>

                                </header>
                                <div class="panel-body">
                                    <div id="ValSummary"></div>
                                    @If Not Model.ChangeOrder.Id = 0 Then
                                        @Html.HiddenFor(Function(m) m.ChangeOrder.Id)
                                    End If
                                    @Html.HiddenFor(Function(m) m.ProjectId)
                                    @Html.HiddenFor(Function(m) m.ProjectName)
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ClientAccounts)</label>
                                        <div class="col-sm-10">
                                            @Html.DropDownListFor(Function(m) m.ChangeOrder.ClientAccountID, New SelectList(Model.ClientAccounts, "ID", "Display", Model.ChangeOrder.ClientAccountID), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstClients"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ProjectContractActivities)</label>
                                        <div class="col-sm-10">
                                            @Html.DropDownListFor(Function(m) m.ChangeOrder.ContractActivityID, New SelectList(Model.ProjectContractActivities, "ID", "Display", Model.ChangeOrder.ContractActivityID), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstProjectActivities"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.Description)</label>
                                        <div class="col-sm-10">
                                            @Html.TextBoxFor(Function(m) m.ChangeOrder.Description, New With {.placeholder = "Omschrijving", .class = "form-control"})
                                        </div>

                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.ChangeOrderDate)</label>
                                        <div class="col-sm-4">
                                            @Html.EditorFor(Function(m) m.ChangeOrder.ChangeOrderDate)
                                        </div>
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.ExpirationDate)</label>
                                        <div class="col-sm-4">
                                            @Html.EditorFor(Function(m) m.ChangeOrder.ExpirationDate)
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.DateSendToClient)</label>
                                        <div class="col-sm-4">
                                            @Html.EditorFor(Function(m) m.ChangeOrder.DateSendToClient)
                                        </div>
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.DateAgreement)</label>
                                        <div class="col-sm-4">
                                            @Html.EditorFor(Function(m) m.ChangeOrder.DateAgreement)
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.ChangeOrderConditions)</label>
                                        <div class="col-sm-10">

                                            @Html.TextBoxFor(Function(m) m.ChangeOrder.ChangeOrderConditions, New With {.class = "form-control"})
                                        </div>

                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.Comment)</label>
                                        <div class="col-sm-10">

                                            @Html.TextAreaFor(Function(m) m.ChangeOrder.Comment, New With {.id = "txtcomment", .name = "txtcomment"})
                                        </div>

                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.ChangeOrder.Invoiceable)</label>
                                        <div class="col-sm-10">
                                            @Html.CheckBoxFor(Function(m) m.ChangeOrder.Invoiceable)


                                        </div>

                                    </div>
                                    <hr>
                                    <h4>Details</h4>

                                    <table class="table table-no-more table-bordered table-striped mb-none">
                                        <thead>
                                            <tr>
                                                <th>Omschrijving</th>
                                                <th colspan="2">Eenheid</th>
                                                <th>Aantal</th>
                                                <th>Prijs</th>
                                                <th>Commissie</th>
                                                <th>Verwijderen</th>
                                            </tr>
                                        </thead>
                                        <tbody id="DetailRows">

                                            @For Each item In Model.ChangeOrder.Details
                                                Html.RenderPartial("_ChangeOrderDetailRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                            Next


                                        </tbody>
                                    </table>
                                    <br />
                                    <button class="btn btn-default btn-block " type="button" id="btnAddDetails">Detaillijn toevoegen</button>
                                </div>
                                <footer class="panel-footer">
                                    <div class="row">
                                        <div class="col-md-12 text-right">
                                            <button class="btn btn-primary btn-block ">Opslaan</button>
                                            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                                        </div>
                                    </div>
                                </footer>
                            </section>
                        </text>
                    End Using

                </div>
            </div>
        </section>
    </div>
</div>
@section scripts
    <script src="~/vendor/admin/summernote/summernote.js"></script>
    <script>

                $(function () {
                    //$('#FormAddChangeOrder').submit(function () {
                    //    if ($(this).valid()) {
                    //        $.ajax({
                    //            url: this.action,
                    //            type: this.method,
                    //            data: $(this).serialize(),
                    //            success: function (result) {
                    //                if (result.success === true) {
                    //                    window.location.href = result.url;

                    //                }
                    //                else {
                    //                    $('#ValSummary').html(result);
                    //                }

                    //            }

                    //        });
                    //    }
                    //    return false;
                    //});
                });
                $(document).ready(function () {

                    $("#lstClients").select2({

                    });
                    $("#lstProjectActivities").select2({

                    });
                    $('#txtcomment').summernote({});
                });
                //toevoegen van een detail rij
                $('#btnAddDetails').click(function () {
                        $.ajax({
                            url: '@Url.Action("AddChangeOrderDetailRow", "Projecten")',
                            cache: false,
                            traditional: true,
                            type: 'POST',
                            success: function (result) {
                                $('#DetailRows').append(result);
                            },
                        });
                });
                //verwijderen van een detail
                $(document).on('click', 'a.deleterow', function () { // <-- changes
                    var $row = $(this).closest('tr')
                    $(this).closest('tr').remove();
                    return false;
                });

    </script>


    <script src="~/scripts/autoNumeric/autoNumeric.js"></script>
end section