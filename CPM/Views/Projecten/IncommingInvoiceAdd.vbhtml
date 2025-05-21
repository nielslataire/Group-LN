@modeltype ProjectIncommingInvoiceAddUpdateModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName & " - Factuur toevoegen"
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
                    @Html.Partial("_ValidationSummary", ViewData.ModelState)
                    @Using Html.BeginForm("IncommingInvoiceAdd", "Projecten", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
                        @<text>
                            <input type="hidden" name="returnUrl" value="@ViewBag.returnUrl" />
                            @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                            @Html.HiddenFor(Function(m) m.ProjectName, New With {.id = "projectname"})
                            @Html.HiddenFor(Function(m) m.Type, New With {.id = "projectname"})
                            <div Class="row">

                                <section Class="panel col-md-12" id="pnlAdd">
                                    <header class="panel-heading">
                                        <h2 class="panel-title">
                                            <span class="va-middle">Factuurgegevens</span>
                                        </h2>
                                    </header>
                                    <div class="panel-body">
                                        <div id="ValSummary"></div>
                                        @If Not Model.IncommingInvoice.Id = 0 Then
                                            @Html.HiddenFor(Function(m) m.IncommingInvoice.Id)
                                        End If

                                        <div class="form-group">
                                            @If Model.Type = 0 Then
                                                @<text>
                                                    <label Class="col-sm-1 control-label" for="w4-selectedcontract">@Html.LabelFor(Function(m) m.ProjectContracts)</label>
                                                    <div Class="col-sm-10">
                                                        @Html.DropDownListFor(Function(m) m.IncommingInvoice.ContractID, New SelectList(Model.ProjectContracts, "ID", "Display", Model.IncommingInvoice.ContractID), "--- Selecteer het contract ---", New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstProjectContracts"})
                                                    </div>
                                                </text>
                                            Else
                                                @<text>
                                                    <label Class="col-sm-1 control-label" for="w4-selectedcontract">Leverancier :</label>
                                                    <div Class="col-sm-10">
                                                        @If Model.IncommingInvoice.CompanyId > 0 Then
                                                            @<text>
                                                                <p class="form-control-static">
                                                                    <strong>
                                                                        @Html.Action("GetCompanyName", New With {.companyid = Model.IncommingInvoice.CompanyId}).ToString()
                                                                        @Html.HiddenFor(Function(m) m.IncommingInvoice.CompanyId, New With {.id = "txtCompany", .class = "form-control"})


                                                                    </strong>
                                                                </p>
                                                            </text>
                                                        Else
                                                            @Html.HiddenFor(Function(m) m.IncommingInvoice.CompanyId, New With {.id = "txtCompany", .class = "form-control"})
                                                        End If
                                                    </div>
                                                </text>
                                            End If

                                        </div>


                                        <div Class="form-group">
                                            <Label Class="col-sm-1 control-label" for="w4-invoiceprice">@Html.LabelFor(Function(m) m.IncommingInvoice.InvoicePrice)</Label>
                                            <div class="col-sm-2">
                                                @Html.EditorFor(Function(m) m.IncommingInvoice.InvoicePrice)
                                            </div>
                                            <label class="col-sm-1 control-label" for="w4-invoicedate">@Html.LabelFor(Function(m) m.IncommingInvoice.IncommingInvoiceDate)</label>
                                            <div class="col-sm-2">
                                                @Html.EditorFor(Function(m) m.IncommingInvoice.IncommingInvoiceDate)
                                            </div>
                                            <label class="col-sm-1 control-label" for="w4-externalid">@Html.LabelFor(Function(m) m.IncommingInvoice.InvoiceExternalId)</label>
                                            <div class="col-sm-4">
                                                @Html.TextBoxFor(Function(m) m.IncommingInvoice.InvoiceExternalId, New With {.class = "form-control"})
                                            </div>

                                        </div>
                                        <hr>
                                        <h4 class="col-md-offset-1">Details</h4>
                                        <div class="row">
                                            <div Class="col-md-10 col-md-offset-1">
                                                <div class="input-group mb-md">
                                                    @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
                                                    @*@Html.HiddenFor(Function(m) m.SelectedActivities, New With {.id = "lstActivities", .class = "form-control", .multiple = "multiple"})*@
                                                    <span class="input-group-btn">
                                                        <button id="btnAddActivities" class="btn btn-primary">Toevoegen</button>
                                                    </span>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="row">
                                            <div Class="col-md-10 col-md-offset-1">
                                                <table class="table table-no-more table-bordered table-striped mb-none">
                                                    <thead>
                                                        <tr>
                                                            <th width="200px">Lot</th>
                                                            <th>Omschrijving</th>
                                                            <th width="150px">Prijs</th>
                                                            <th width="150px"> Type</th>
                                                            <th width="200px">WO</th>
                                                            <th width="120px">Verwijderen</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody id="DetailRows">

                                                        @For Each item In Model.IncommingInvoice.Details
                                                            Html.RenderPartial("_IncommingInvoiceDetailRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                                        Next


                                                    </tbody>
                                                    @*<tr class="primary">
                                                            <td colspan="2">Totaal :</td>
                                                            <td><div class="totaleprijs">0,00 €</div></td>
                                                        </tr>*@

                                                </table>
                                            </div>
                                        </div>
                                        <br />
                                        <br />
                                        @*<button class="btn btn-default btn-block " type="button" id="btnAddDetails">Detaillijn toevoegen</button>*@
                                    </div>

                                </section>



                            </div>

                            <Button Class="btn btn-primary btn-block">Opslaan</Button>
                            <a href="@Url.Action("Recalculation", "Projecten", New With {.projectid = Model.ProjectId})" class="btn btn-default btn-block">Annuleren</a>

                        </text>
                    End Using
                </div>
            </div>
        </section>
    </div>
</div>

@section scripts
    <script>
        jQuery(function ($) {
            $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults

        });
        //Berichten laden
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
        //Activiteiten Selecteren op basis van Bedrijf
        $('#txtCompany').on('change', function (e) {

            var data = $('#txtCompany').select2('data');

                        GetActivitiesCompany(data.id);
                    });
        ////Select2 box voor activiteiten initialiseren
        //            $("#lstActivities").select2({
        //                placeholder: 'Selecteer het lot',
        //                data: []
        //            });
        //Pagina geladen
                    $(document).ready(function () {


                        $("#lstProjectContracts").select2({

                        });
                       /* $("#lstActivities").select2('disable');*/

                        if (@Model.IncommingInvoice.ContractID > 0) {
                            GetActivities(@Model.IncommingInvoice.ContractID);
                        };
                    if ($('#txtCompany').val() != 0) {
                        var data = $('#txtCompany').select2('data');
                        GetActivitiesCompany(data.id);
                    } else {
                         $("#txtCompany").select2({

                        minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                        width: 'resolve',   // to adjust proper width of select2 wrapped elements
                        ajax: {

                            url: '@Url.Action("GetCompanys", "Projecten")',
                            cache: false,
                            traditional: true,
                            type: 'POST',
                            data: function (term) {
                                return {
                                    term: term,
                                };
                            },

                            results: function (data, page) {
                                return { results: data };
                            },
                            initSelection: function (element, callback) {

                            }
                        },
                         });
                        /*$("#lstActivities").select2('disable');*/

                    };

                    });
        //Bij selecteren contract activiteiten laden
                    $('#lstProjectContracts').on('change', function (e) {
                        var data2 = $('#lstProjectContracts').select2('data');
                        GetActivities(data2.id);
                    });
        //bij veranderen van type naar meerwerk klant
                    function change(theStatus) {
                        if (theStatus.value === "3") {
                            $(theStatus).closest('td').next().find('.wo').removeAttr('disabled');;
                           /* $(theStatus).parent().next('td').closest('.wo').attr('disabled', 'disabled');*/
                        } else {
                            $(theStatus).closest('td').next().find('.wo').attr('disabled', 'disabled');
                        }
                    }

        //Activiteiten laden bij contract
        var options = {
            tags: true,
            createTag: function (params) {
                return {
                    id: params.term,
                    text: params.term,
                    newOption: true
                }
            },
            templateResult: function (data) {
                var $result = $("<span></span>");

                $result.text(data.text);

                if (data.newOption) {
                    $result.append(" <em>(new)</em>");
                }

                return $result;
            }
        }
        function GetActivities(contractid) {


                    $.ajax({
                        type: 'POST',
                        url: '@Url.Action("GetContractActitvities", "Projecten")',
                        data: { "contractid": contractid },
                        dataType: 'json',
                        success: function (data) {

                            $.each(data, function (i, item) {
                                $("#lstActivities option[value='" + item.id + "']").remove();
                                if ($('#lstActivities optgroup[label="Contractactiviteiten"]').length > 0) {

                                    var option = $("<option></option>");
                                    option.val(item.id);
                                    option.text(item.text);

                                    $('#lstActivities optgroup[label="Contractactiviteiten"]').append(option);
                                    /*$('#lstActivities').val(item.id).trigger("change");*/


                                } else {

                                    var optgroup = $('<optgroup>');
                                    optgroup.attr('label', "Contractactiviteiten");

                                    var option = $("<option></option>");
                                    option.val(item.id);
                                    option.text(item.text);

                                    optgroup.append(option);

                                    $('#lstActivities').append(optgroup);

                                    /*$('#lstActivities').val(item.id)*/

                                }

                            });
                            $('#lstActivities').select2(options);

                        },
                        error: function () {
                        }
                    });


        };
        //Activiteiten laden bij leverancier
        function GetActivitiesCompany(companyid) {
                    $.ajax({
                        type: 'POST',
                        url: '@Url.Action("GetCompanyActivities", "Projecten")',
                        data: { "companyid": companyid },
                        dataType: 'json',
                        success: function (data) {

                            $.each(data, function (i, item) {
                                $("#lstActivities option[value='" + item.id + "']").remove();
                                if ($('#lstActivities optgroup[label="Bedrijfsactiviteiten"]').length > 0) {

                                    var option = $("<option></option>");
                                    option.val(item.id);
                                    option.text(item.text);

                                    $('#lstActivities optgroup[label="Bedrijfsactiviteiten"]').append(option);
                                    /*$('#lstActivities').val(item.id).trigger("change");*/


                                } else {

                                    var optgroup = $('<optgroup>');
                                    optgroup.attr('label', "Bedrijfsactiviteiten");

                                    var option = $("<option></option>");
                                    option.val(item.id);
                                    option.text(item.text);

                                    optgroup.append(option);

                                    $('#lstActivities').append(optgroup);

                                    /*$('#lstActivities').val(item.id)*/

                                }

                            });
                            $('#lstActivities').select2(options);


                        },
                        error: function () {
                        }
                    });
                   /* $("#lstActivities").select2('enable');*/
        };
         //toevoegen van een detail rij
        $('#btnAddActivities').click(function () {
            if (@Model.Type == 0) {

                            var el = document.getElementById('lstActivities');
                            var result = [];
                            var options = el && el.options;
                            var opt;
                            var data2 = $('#lstProjectContracts').select2('data');
                            for (var i = 0, iLen = options.length; i < iLen; i++) {
                                opt = options[i];

                                if (opt.selected) {

                                     $.ajax({
                                            url: '@Url.Action("AddIncommingInvoiceDetailRow", "Projecten")',
                                            data: { ActivityId: opt.value, ActivityName: opt.text, ContractId: data2.id, CompanyId:"0" },
                                            cache: false,
                                            traditional: true,
                                            type: 'POST',
                                            success: function (result) {

                                                $('#DetailRows').append(result);

                                            },

                                        });


                                }
                }


            } else {

                 var el = document.getElementById('lstActivities');
                            var result = [];
                            var options = el && el.options;
                            var opt;
                                if (@Model.IncommingInvoice.CompanyId != 0) {


                            for (var i = 0, iLen = options.length; i < iLen; i++) {
                                opt = options[i];

                                if (opt.selected) {

                                     $.ajax({
                                            url: '@Url.Action("AddIncommingInvoiceDetailRow", "Projecten")',
                                            data: { ActivityId: opt.value, ActivityName: opt.text, ContractId: "0", CompanyId: @Model.IncommingInvoice.CompanyId },
                                            cache: false,
                                            traditional: true,
                                            type: 'POST',
                                            success: function (result) {

                                                $('#DetailRows').append(result);

                                            },

                                        });


                                };
                                    };
                } else {
                var data2 = $('#txtCompany').select2('data');

                            for (var i = 0, iLen = options.length; i < iLen; i++) {
                                opt = options[i];

                                if (opt.selected) {

                                     $.ajax({
                                            url: '@Url.Action("AddIncommingInvoiceDetailRow", "Projecten")',
                                            data: { ActivityId: opt.value, ActivityName: opt.text, ContractId: "0", CompanyId: data2.id },
                                            cache: false,
                                            traditional: true,
                                            type: 'POST',
                                            success: function (result) {

                                                $('#DetailRows').append(result);

                                            },

                                        });


                                };
                                    };
                };

            };
            $('#lstActivities').select2("val", "");
                    return false;
                            });

                //verwijderen van een detail
                $(document).on('click', 'a.deleterow', function () { // <-- changes
                    var $row = $(this).closest('tr')
                    $(this).closest('tr').remove();
                    return false;
                });
    </script>
    <script src="~/scripts/autoNumeric/autoNumeric.js"></script>
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
