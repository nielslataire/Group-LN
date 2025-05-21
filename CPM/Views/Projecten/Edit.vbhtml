
@modeltype EditProjectDetail
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.Project.Name
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />
End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu content-with-menu-has-toolbar">
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
                                            @Html.Partial("DetailMenu", Model.Project.Id)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>
                <div class="inner-body mg-main">
                    <div class="inner-toolbar clearfix">
                        <ul>
                            <li>
                                @Html.HtmlActionLink("<i class='fa fa-edit'></i> Bewerken</a>", "EditGeneralData", "Projecten", New With {.projectid = Model.Project.Id}, New With {.class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = True)
                            </li>
                            <li>
                                @Html.HtmlActionLink("<i class='fa fa-save'></i> Opslaan</a>", "SaveGeneralData", "Projecten", New With {.model = Model}, New With {.id = "GeneralDataSave", .class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                            </li>
                            <li>
                                @Html.HtmlActionLink("<i class='fa fa-undo'></i> Annuleren</a>", "CancelEditGeneralData", "Projecten", New With {.projectid = Model.Project.Id}, New With {.class = "btn"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                            </li>

                        </ul>
                    </div>
                    @Using Html.BeginForm("Detail", "Projecten", FormMethod.Post, New With {.id = "FormGeneralData", .class = "form-horizontal"})
                        @Html.Partial("_ValidationSummary", ViewData.ModelState)
                        @<text>
                            @Html.HiddenFor(Function(m) m.GeneralDataEditMode, New With {.id = "txtEditGeneralData"})
                            @Html.HiddenFor(Function(m) m.Project.Id, New With {.id = "txtProjectId"})
                            <div class="tabs tabs-primary ">
                                <ul class="nav nav-tabs nav-justified">
                                    <li class="active">
                                        <a href="#algemeen" data-toggle="tab" class="text-center">Algemeen</a>
                                    </li>
                                    <li>
                                        <a href="#docs" data-toggle="tab" class="text-center">Verkoopsdocumenten</a>
                                    </li>
                                </ul>
                                <div class="tab-content">
                                    <div id="algemeen" class="tab-pane active">

                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtName">@Html.LabelFor(Function(m) m.Project.Name)</label>
                                            <div class="col-md-10">
                                                @Html.TextBoxFor(Function(m) m.Project.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtName"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtStreet">@Html.LabelFor(Function(m) m.Project.Street)</label>
                                            <div class="col-md-8">
                                                @Html.TextBoxFor(Function(m) m.Project.Street, New With {.placeholder = "Straat", .class = "form-control", .id = "txtStreet"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <div class="col-md-2">
                                                @Html.TextBoxFor(Function(m) m.Project.HouseNumber, New With {.placeholder = "Huisnr.", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>

                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="lstProjectCountries">Land</label>
                                            <div class="col-md-4">
                                                @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                                                @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstProjectCountries"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                @*@Html.TextBoxFor(Function(m) m.Country.CountryID, New With {.placeholder = "", .class = "form-control"})*@
                                            </div>

                                            <label class="col-md-2 control-label" for="txtPostalcode" onload="loadCountry()">@Html.LabelFor(Function(m) m.Project.Postalcode.Gemeente)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtPostalcode", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtCommTitle">@Html.LabelFor(Function(m) m.Project.CommercialTitleNL)</label>
                                            <div class="col-md-10">
                                                @Html.TextBoxFor(Function(m) m.Project.CommercialTitleNL, New With {.placeholder = "Titel", .class = "form-control", .id = "txtCommTitle"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Project.CommercialTextNL)</label>
                                            <div class="col-md-10">
                                                @Html.TextAreaFor(Function(m) m.Project.CommercialTextNL, New With {.id = "txtCommText", .class = "form-control", .rows = "6"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Project.StartDateConstruction)</label>
                                            <div class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Project.StartDateConstruction).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                @*<div class="input-group">
                    <span class="input-group-addon">
                        <i class="fa fa-calendar"></i>
                    </span>
                    @Html.TextBoxFor(Function(m) m.Project.StartDateConstruction, New With {.class = "form-control", .id = "txtStartDate", .data_plugin_datepicker = "", .value = Model.Project.StartDateConstruction.ToString}).DisableIf(Function() Model.GeneralDataEditMode = False)
                </div>*@
                                            </div>
                                            <label class="col-md-2 control-label" for="txtExecutiondays">@Html.LabelFor(Function(m) m.Project.ExecutionDays)</label>
                                            <div class="col-md-4">
                                                <div class="input-group">
                                                    <span class="input-group-addon">
                                                        <i class="fa fa-wrench"></i>
                                                    </span>
                                                    @Html.TextBoxFor(Function(m) m.Project.ExecutionDays, New With {.class = "form-control", .id = "txtExecutiondays", .type = "number"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                </div>
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtDeliveryDate">@Html.LabelFor(Function(m) m.Project.DeliveryDate)</label>
                                            <div class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Project.DeliveryDate).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                @*<div class="input-group">
                    <span class="input-group-addon">
                        <i class="fa fa-calendar"></i>
                    </span>
                    @Html.TextBoxFor(Function(m) m.Project.DeliveryDate, New With {.class = "form-control", .id = "txtDeliveryDate", .data_plugin_datepicker = "", .value = Model.Project.DeliveryDate.ToString}).DisableIf(Function() Model.GeneralDataEditMode = False)
                </div>*@
                                            </div>
                                            <label class="col-md-2 control-label" for="txtWheaterstation">@Html.LabelFor(Function(m) m.Project.WheaterStation)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.WheaterStation.Id, New With {.id = "txtWheaterStation", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtStatus">@Html.LabelFor(Function(m) m.Project.ProjectType)</label>
                                            <div class="col-md-4">
                                                @Html.EnumDropDownListFor(Function(m) m.Project.ProjectType, New With {.id = "lstPrjectType", .class = "form-control"})
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtStatus">@Html.LabelFor(Function(m) m.Project.Status)</label>
                                            <div class="col-md-4">
                                                @Html.DropDownListFor(Function(m) m.SelectedStatus, New SelectList(Model.Statuses, "ID", "Display", Model.SelectedStatus), New With {.class = "form-control populate", .id = "lstProjectStatuses"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtStatus">@Html.LabelFor(Function(m) m.Project.AspNetUserID)</label>
                                            <div class="col-md-4">
                                                @Html.DropDownListFor(Function(m) m.Project.AspNetUserID, New SelectList(Model.Users, "ID", "Displayname", Model.Project.AspNetUserID), "Selecteer ....", New With {.class = "form-control populate", .id = "lstProjectUsers"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtStatus">@Html.LabelFor(Function(m) m.Project.TotalLandShare)</label>
                                            <div class="col-md-4">
                                                @Html.TextBoxFor(Function(m) m.Project.TotalLandShare, New With {.class = "form-control", .id = "txtLandShare", .type = "number"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtStatus">Facebook Plaats</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.SelectedFacebookPlace, New With {.id = "txtFacebookPlace", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtStatus">@Html.LabelFor(Function(m) m.Project.ProjectFolder)</label>
                                            <div class="col-md-4">
                                                @Html.TextBoxFor(Function(m) m.Project.ProjectFolder, New With {.class = "form-control", .id = "txtProjectFolder"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtDeliveryDateDef">@Html.LabelFor(Function(m) m.Project.DeliveryDateDef)</label>
                                            <div class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Project.DeliveryDateDef).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>

                                        <hr />

                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtDeveloper">@Html.LabelFor(Function(m) m.Project.Developer)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.Developer.ID, New With {.id = "txtDeveloper", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtBuilder">@Html.LabelFor(Function(m) m.Project.Builder)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.Builder.ID, New With {.id = "txtBuilder", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtArchitect">@Html.LabelFor(Function(m) m.Project.Architect)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.Architect.ID, New With {.id = "txtArchitect", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtEngineer">@Html.LabelFor(Function(m) m.Project.Engineer)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.Engineer.ID, New With {.id = "txtEngineer", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>

                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtSecurityCoordinator">@Html.LabelFor(Function(m) m.Project.SecurityCoordinator)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.SecurityCoordinator.ID, New With {.id = "txtSecurityCoordinator", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>
                                            <label class="col-md-2 control-label" for="txtEpbReporter">@Html.LabelFor(Function(m) m.Project.EpbReporter)</label>
                                            <div class="col-md-4">
                                                @Html.HiddenFor(Function(m) m.Project.EpbReporter.ID, New With {.id = "txtEpbReporter", .class = "form-control"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                            </div>

                                        </div>
                                        @If User.IsInRole("Admin") Then
                                            @<text>

                                                <br /><br />
                                                <h4 Class="col-md-offset-2">Verplichte documenten</h4>
                                                <hr />
                                                <div Class="form-group">
                                                    <Label Class="col-md-2 control-label" for="chkDocDelivery">@Html.LabelFor(Function(m) m.Project.DocDelivery)</Label>

                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocDelivery, New With {.id = "chkDocDelivery"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <label class="col-md-2 control-label" for="chkDocDefDelivery">@Html.LabelFor(Function(m) m.Project.DocDefDelivery)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocDefDelivery, New With {.id = "chkDocDefDelivery"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <label class="col-md-2 control-label" for="chkDocPID">@Html.LabelFor(Function(m) m.Project.DocPID)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocPID, New With {.id = "chkDocPID"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="form-group">
                                                    <label class="col-md-2 control-label" for="chkDocElectricalInspection">@Html.LabelFor(Function(m) m.Project.DocElectricalInspection)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocElectricalInspection, New With {.id = "chkDocElectricalInspection"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <label class="col-md-2 control-label" for="chkDocWaterInspection">@Html.LabelFor(Function(m) m.Project.DocWaterInspection)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocWaterInspection, New With {.id = "chkDocWaterInspection"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <label class="col-md-2 control-label" for="chkDocFireInspection">@Html.LabelFor(Function(m) m.Project.DocFireInspection)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocFireInspection, New With {.id = "chkDocFireInspection"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <label class="col-md-2 control-label" for="chkDocSewerInspection">@Html.LabelFor(Function(m) m.Project.DocSewerInspection)</label>
                                                    <div class="col-md-1">
                                                        <div class="checkbox">
                                                            <label>
                                                                @Html.CheckBoxFor(Function(m) m.Project.DocSewerInspection, New With {.id = "chkDocSewerInspection"}).DisableIf(Function() Model.GeneralDataEditMode = False)
                                                            </label>
                                                        </div>
                                                    </div>
                                                </div>
                                            </text>
                                        End If
                                    </div>
                                                                                                    
                                    <div id="docs" class="tab-pane">
                                        <table class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                                                                                                                                                                                                                                            <tr>
                                                                                                                                                                                                                                                                            <td>Docnr.</td>
                                                    <td>Naam</td>
                                                    <td>Bestandsnaam</td>
                                                    <td>Acties</td>
                                                </tr>

                                            </thead>
                                            <tbody>
                                                @for Each doc In Model.Docs
                                                    @<text>
                                                        <tr>
                                                                                                                                                                                                                                                                            <td>@doc.Docid</td>
                                                            <td>@doc.Name</td>
                                                            <td>@doc.Filename </td>
                                                            <td><a href="#modaldeletedoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deletedoc modal-with-form" data-id="@doc.Docid"><i class="fa fa-remove "></i></a></td>
                                                        </tr>
                                                    </text>
Next
                                            </tbody>
                                        </table>
                                        <br />
                                        <div class="form-group">
                                            <div class="col-sm-12">
                                                <a href="#modaladddoc" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Document toevoegen" type="button" id="btnAddDoc"><i class="fa fa-plus"></i> Document toevoegen</a>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                        </text>
                    End Using



                </div>

            </div>
        </section>



    </div>
</div>
@Html.Partial("ModalAddDocument", New BO.ProjectDocBO With {.ProjectId = Model.Project.Id, .Type = BO.ProjectDocType.Sales})


<div id="modaldeletedoc" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-doc-container"></div>
                                                                                                                                                                                                                                                                                    </div>
@*<div id="modaladdpicture" class="modal-block modal-block-primary mfp-hide">
        @Html.Partial("ModalAddPicture")

    </div>*@

@section scripts

    <script>
        var iCountryId = jQuery("#lstProjectCountries option:selected").val();
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
        function loadCountry() {
            var iCountryId = jQuery("#lstProjectCountries option:selected").val();
        }
        function EditGeneralData(val)
        {
            document.getElementById('txtEditGeneralData').value = val;

            //and probably call document.forms[0].submit();
        }
        $('#lstProjectCountries').on('change', function () {

            iCountryId = this.value;
            $.ajax({
                url: '@Url.Action("GetCountryISOCode", "Shared")',
                data: { countryid: iCountryId },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#txtCountryIsoCode').val(result);
                },

            });
        });
        //toevoegen van een afdeling aan company
        $('#btnAddDoc').click(function () {
            var url = "/Projecten/AddDocument"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#add-doc-container').html(data);
            });
        });
        //toevoegen van een afdeling aan company
        $('.deletedoc').click(function () {
            var url = "/Projecten/ModalDeleteDoc"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#delete-doc-container').html(data);
            });
        });
        $(document).ready(function () {

            //$('#txtStartDate').datepicker({
            //    language:'nl-BE',
            //    format:'dd/mm/yyyy',
            //    autoclose:true,
            //});
            //$('#txtDeliveryDate').datepicker({
            //    language:'nl-BE',
            //    format:'dd/mm/yyyy',
            //    autoclose:true,
            //});
            $("#txtPostalcode").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer uw gemeente",
                ajax: {

                    url: '@Url.Action("GetPostcodesByCountry", "Shared")',
                    cache: false,
                    traditional: true,
                    type: 'POST',
                    data: function (term) {
                        return {
                            term: term,
                            CountryId: iCountryId,
                        };
                    },

                    results: function (data, page) {
                        return { results: data };
                    },

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.Postalcode.PostcodeId > 0){

                        var data = {id:@Model.Project.Postalcode.PostcodeId, text: '@(Model.Project.Postalcode.Postcode) - @(Model.Project.Postalcode.Gemeente)' };
                        callback(data);

                    }

                }



            });
            //Select lijst van projectontwikkelaar
            $("#txtDeveloper").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de projectontwikkelaar",
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.Developer.ID  > 0){

                        var data = {id:@Model.Project.Developer.ID, text: '@(Model.Project.Developer.Display)' };
                        callback(data);

                    }

                }



            });
            //Select lijst van bouwheer
            $("#txtBuilder").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de bouwheer",
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.Builder.ID  > 0){

                        var data = {id:@Model.Project.Builder.ID, text: '@(Model.Project.Builder.Display)' };
                        callback(data);

                    }

                }



            });
            //Select lijst van architect


            $("#txtArchitect").select2(
                {
                    minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                    width: 'resolve',   // to adjust proper width of select2 wrapped elements
                    placeholder: "Selecteer de architect",
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

                    },
                    initSelection: function (element, callback) {

                        if (@Model.Project.Architect.ID  > 0){

                            var data = {id:@Model.Project.Architect.ID, text: '@(Model.Project.Architect.Display)' };
                            callback(data);

                        }

                    }
                }
            );


            //Select lijst van ingenieur stabiliteit
            $("#txtEngineer").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de ingenieur",
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.Engineer.ID  > 0){

                        var data = {id:@Model.Project.Engineer.ID, text: '@(Model.Project.Engineer.Display)' };
                        callback(data);

                    }

                }



            });
            //Select lijst van Veiligheidscoordinator
            $("#txtSecurityCoordinator").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de veiligheidscoordinator",
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.SecurityCoordinator.ID  > 0){

                        var data = {id:@Model.Project.SecurityCoordinator.ID, text: '@(Model.Project.SecurityCoordinator.Display)' };
                        callback(data);

                    }

                }



            });
            //Select lijst van Veiligheidscoordinator
            $("#txtEpbReporter").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de epb verslaggever",
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.EpbReporter.ID  > 0){

                        var data = {id:@Model.Project.EpbReporter.ID, text: '@(Model.Project.EpbReporter.Display)' };
                        callback(data);

                    }

                }



            });
            //Select lijst voor de weerstations
            $("#txtWheaterStation").select2({

                minimumInputLength: 1,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer het KMI weerstation",
                ajax: {
                    url: '@Url.Action("GetWheaterstations", "Projecten")',
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

                },
                initSelection: function (element, callback) {

                    if (@Model.Project.WheaterStation.Id  > 0){

                        var data = {id:@Model.Project.WheaterStation.Id, text: '@(Model.Project.WheaterStation.Name)' };
                        callback(data);

                    }

                }



            });
            $("#txtFacebookPlace").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer uw gemeente",
                ajax: {
                    url: '@Url.Action("GetFacebookPlaces", "Projecten")',
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

                },
                initSelection: function (element, callback) {

                }



            });
        });
        $("#GeneralDataSave").click(function (e) {
            e.preventDefault();
            //Show loading display here
            var form = $("#FormGeneralData");
            $.ajax({
                url: '@Url.Action("SaveGeneralData")',
                data: form.serialize(),
                type: 'POST',
                success: function (response) {
                    window.location.href = response.Url;
                }
            });
        });

    </script>
    <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
    <script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
    <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
end section

