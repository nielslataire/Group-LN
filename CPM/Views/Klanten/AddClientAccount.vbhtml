@modeltype AddClientAccountModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">
<link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />

End Section
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>
<div class="row">
    <div class="col-xs-12 col-sm-12">
        <!-- start: page -->


                    <section class="panel form-wizard" id="w4">

                        <header class="panel-heading">
                            <div class="panel-actions">
                                <a href="#" class="panel-action panel-action-toggle" data-panel-toggle></a>
                                <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss></a>
                            </div>

                            <h2 class="panel-title">Klantenaccount toevoegen aan @Model.ProjectName</h2>
                        </header>

                        <div class="panel-body">

                            @Html.Partial("_ValidationSummary", ViewData.ModelState)
                            <div class="wizard-progress wizard-progress-md">
                                <div class="steps-progress">
                                    <div class="progress-indicator"></div>
                                </div>
                                <ul class="wizard-steps">
                                    <li class="active">
                                        <a href="#w4-info" data-toggle="tab"><span>1</span>Gegevens</a>
                                    </li>
                                    <li>
                                        <a href="#w4-contacts" data-toggle="tab"><span>2</span>Eigenaars</a>
                                    </li>
                                    <li>
                                        <a href="#w4-units" data-toggle="tab"><span>3</span>Entiteiten</a>
                                    </li>
                                    @*<li>
                                        <a href="#w4-contacts" data-toggle="tab"><span>4</span>Contacten</a>
                                    </li>
                                   
                                    <li>
                                        <a href="#w4-save" data-toggle="tab"><span>5</span>Opslaan</a>
                                    </li>*@
                                </ul>
                            </div>

                            @Using Html.BeginForm("AddClientAccount", "Klanten", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
                            @<text>

                            <div class="tab-content">
                                         <div id="w4-info" class="tab-pane active">
                                    <div class="form-group">
                                        <div class="col-md-4  col-md-offset-2">
                                            <div class="checkbox-custom checkbox-default">
                                                <input type="checkbox" id="checkboxCompany">
                                                <label for="checkboxCompany">Eigenaar is een onderneming</label>
                                            </div>

                                        </div>
                                    </div>
                                    <div class="form-group">
                                        @Html.HiddenFor(Function(m) m.ProjectId)
                                        @Html.HiddenFor(Function(m) m.ProjectName)
                                        <label class="col-md-2 control-label" for="w4-name">@Html.LabelFor(Function(m) m.ClientAccount.Name)</label>
                                        <div class="col-md-2">
                                            @Html.EnumDropDownListFor(Function(m) m.ClientAccount.Salutation, New With {.class = "form-control", .id = "lstSalutationAccount"})
                                        </div>
                                        <div class="col-md-7">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtinputname", .autocomplete = "name"})

                                            @*<input type="text" class="form-control" placeholder="Naam" name="name" id="w4-name">*@
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-2 control-label">Bedrijfsgegevens</label>
                                        <div class="col-md-5">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.CompanyName, New With {.placeholder = "Bedrijfsnaam", .class = "form-control", .id = "txtCompanyName", .disabled = ""})
                                        </div>
                                        <div class="col-md-4">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.VATnumber, New With {.placeholder = "BTW nummer", .class = "form-control", .id = "txtVatNumber", .disabled = ""})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.ClientAccount.Street)</label>
                                        <div class="col-md-5">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.Street, New With {.placeholder = "Straat", .class = "form-control"})
                                        </div>
                                        <div class="col-md-2">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.Housenumber, New With {.placeholder = "Nr", .class = "form-control", .autocomplete = "off"})
                                        </div>
                                        <div class="col-md-2">
                                            @Html.TextBoxFor(Function(m) m.ClientAccount.Busnumber, New With {.placeholder = "Bus", .class = "form-control"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                        <div class="col-md-3">
                                            @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                                            @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstCompanyCountries"})
                                            @*@Html.TextBoxFor(Function(m) m.Country.CountryID, New With {.placeholder = "", .class = "form-control"})*@
                                        </div>

                                        <label class="col-md-1 control-label" for="w4-City" onload="loadCountry()">@Html.LabelFor(Function(m) m.ClientAccount.Postalcode.Gemeente)</label>
                                        <div class="col-md-5">

                                            @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtPostalcode", .class = "form-control"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <div class="col-md-4 col-md-offset-2 ">
                                            <div class="checkbox">
                                                <label>@Html.CheckBoxFor(Function(m) m.ClientAccount.InvoiceAddress, New With {.id = "checkboxInvoiceAddress"})Facturatieadres verschilt van adres</label>
                                            </div>
                                        </div>
                                        </div>
                                    <div class="InvoiceAddress" hidden="hidden">
                                        <hr />
                                        <h5 class="col-md-offset-2 text-uppercase">Facturatiegegevens</h5>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.ClientAccount.InvoiceStreet)</label>
                                            <div class="col-md-5">
                                                @Html.TextBoxFor(Function(m) m.ClientAccount.InvoiceStreet, New With {.placeholder = "Straat", .class = "form-control"})
                                            </div>
                                            <div class="col-md-2">
                                                @Html.TextBoxFor(Function(m) m.ClientAccount.InvoiceHousenumber, New With {.placeholder = "Nr", .class = "form-control"})
                                            </div>
                                            <div class="col-md-2">
                                                @Html.TextBoxFor(Function(m) m.ClientAccount.InvoiceBusnumber, New With {.placeholder = "Bus", .class = "form-control"})
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                            <div class="col-md-3">
                                                @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                                                @Html.DropDownListFor(Function(m) m.SelectedInvoiceCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedInvoiceCountry), New With {.class = "form-control populate", .id = "lstClientInvoiceCountries"})
                                            </div>
                                            <label class="col-md-1 control-label" for="w4-City" onload="loadCountry2()">@Html.LabelFor(Function(m) m.ClientAccount.InvoicePostalcode.Gemeente)</label>
                                            <div class="col-md-5">

                                                @Html.HiddenFor(Function(m) m.SelectedInvoicePostalcode, New With {.id = "txtInvoicePostalcode", .class = "form-control"})
                                            </div>
                                        </div>
                                        <hr />
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-2 control-label" for="w4-owner">Type eigenaar</label>
                                        <div class="col-md-3">

                                            @Html.DropDownListFor(Function(m) m.ClientAccount.OwnerType.Id, New SelectList(Model.OwnerTypes, "ID", "Display", Model.ClientAccount.OwnerType.Id), New With {.class = "form-control populate", .id = "lstOwnerTypes"})

                                        </div>
                                        <label class="col-md-1 control-label" for="w4-DateSalesAgreement">@Html.LabelFor(Function(m) m.ClientAccount.DateSalesAgreement)</label>
                                        <div class="col-md-5">

                                            @Html.EditorFor(Function(m) m.ClientAccount.DateSalesAgreement)
                                        </div>
                                    </div>

                                    <div id="editorRows">
                                        @for Each item In Model.ClientAccount.Contacts

                                            Html.RenderPartial("_ContactRow", item)

                                        Next

                                    </div>
                                    <hr />
                                    <div class="col-md-3 col-md-offset-8">
                                        <div class="btn-group-vertical pull-right">
                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Contactgegevens toevoegen" href="@Url.Action("BlankContactRow", "Klanten", Nothing)" class="btn btn-default pull-right" id="addContact"><i class="fa fa-plus"></i> Contactgegevens toevoegen</a>
                                        </div>
                                    </div>
                                </div>
                                        <div id="w4-contacts" class="tab-pane">
                                            <h4>Mede-eigenaar toevoegen</h4>
                                            <div class="form-group">
                                                <div class="col-md-4  col-md-offset-2">
                                                    <div class="checkbox-custom checkbox-default">
                                                        <input type="checkbox" id="checkboxCoOwnerCompany" disabled="">
                                                        <label for="checkboxCoOwnerCompany">Mede-eigenaar is een onderneming</label>
                                                    </div>

                                                </div>
                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label" for="w4-street">Naam</label>
                                                <div class="col-md-1">
                                                    @Html.EnumDropDownListFor(Function(m) m.Salutations, New With {.class = "form-control", .id = "lstSalutationCoOwner"})

                                                </div>
                                                <div class="col-md-4">
                                                    <input type="text" class="form-control" placeholder="Naam" name="txtCoOwnerName" id="txtCoOwnerName" disabled="">

                                                </div>
                                                <div class="col-md-4">
                                                    <input type="text" class="form-control" placeholder="Voornaam" name="txtCoOwnerForeName" id="txtCoOwnerForeName" disabled="">
                                                </div>
                                                <div class="col-md-1">
                                                </div>
                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label">Bedrijfsgegevens</label>
                                                <div class="col-md-5">
                                                    <input type="text" class="form-control" placeholder="Bedrijfsnaam" name="txtCoOwnerCompanyName" id="txtCoOwnerCompanyName" disabled="">
                                                </div>
                                                <div class="col-md-5">
                                                    <input type="text" class="form-control" placeholder="BTW nummer" name="txtCoOwnerVATNumber" id="txtCoOwnerVATNumber" disabled="">
                                                </div>

                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label">Contactgegevens</label>
                                                <div Class="col-md-5">
                                                    <div class="input-group">
                                                        <span class="input-group-addon">
                                                            <i class="fa fa-at"></i>
                                                        </span>
                                                        <input type="email" class="form-control" placeholder="Email" name="txtCoOwnerEmail" id="txtCoOwnerEmail" disabled="">
                                                    </div>
                                                </div>
                                                <div class="col-md-2">
                                                    <div class="input-group">
                                                        <span class="input-group-addon">
                                                            <i class="fa fa-phone"></i>
                                                        </span>
                                                        <input type="text" class="form-control" placeholder="Telefoon" data-plugin-masked-input="" data-input-mask="999/99.99.99" name="txtCoOwnerPhone" id="txtCoOwnerPhone" disabled="">
                                                    </div>
                                                </div>
                                                <div Class="col-md-2">
                                                    <div class="input-group">
                                                        <span class="input-group-addon">
                                                            <i class="fa fa-mobile"></i>
                                                        </span>
                                                        <input type="text" class="form-control" placeholder="Mobiel" data-plugin-masked-input="" data-input-mask="9999/99.99.99" name="txtCoOwnerCellphone" id="txtCoOwnerCellphone" disabled="">
                                                    </div>
                                                </div>

                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label" for="w4-street">Straat</label>
                                                <div class="col-md-5">
                                                    <input type="text" class="form-control" placeholder="vb. Klaverdries" name="txtCoOwnerStreet" id="txtCoOwnerStreet" disabled="">
                                                </div>
                                                <div class="col-md-2">
                                                    <input type="text" class="form-control" placeholder="Nr" name="txtCoOwnerHousenumber" id="txtCoOwnerHousenumber" disabled="">
                                                </div>
                                                <div class="col-md-2">
                                                    <input type="text" class="form-control" placeholder="Bus" name="txtCoOwnerBusnumber" id="txtCoOwnerBusnumber" disabled="">
                                                </div>
                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                                <div class="col-md-2">
                                                    @Html.DropDownListFor(Function(m) m.SelectedCoOwnerCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCoOwnerCountry), New With {.class = "form-control populate", .id = "lstCoOwnerCountries", .disabled = ""})
                                                </div>

                                                <label class="col-md-1 control-label" for="w4-City">Gemeente</label>
                                                <div class="col-md-6">
                                                    @Html.HiddenFor(Function(m) m.SelectedCoOwnerPostalCode, New With {.id = "txtCoOwnerPostalcode", .class = "form-control", .disabled = ""})
                                                </div>
                                            </div>
                                            <div class="form-group">
                                                <div class="col-md-4 col-md-offset-2 ">
                                                    <div class="checkbox">
                                                        <label><input type="checkbox" id="checkboxCoOnwerInvoiceAddress" />Facturatieadres verschilt van adres</label>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="CoOwnerInvoiceAddress" hidden="hidden">
                                                <hr />
                                                <h5 class="col-md-offset-2 ">Facturatiegegevens</h5>
                                                <div class="form-group">
                                                    <label class="col-md-2 control-label" for="w4-street">Straat</label>
                                                    <div class="col-md-5">
                                                        <input type="text" class="form-control" placeholder="vb. Klaverdries" name="txtCoOwnerInvoiceStreet" id="txtCoOwnerInvoiceStreet" disabled="">
                                                    </div>
                                                    <div class="col-md-2">
                                                        <input type="text" class="form-control" placeholder="Nr" name="txtCoOwnerInvoiceHousenumber" id="txtCoOwnerInvoiceHousenumber" disabled="">
                                                    </div>
                                                    <div class="col-md-2">
                                                        <input type="text" class="form-control" placeholder="Bus" name="txtCoOwnerInvoiceBusnumber" id="txtCoOwnerInvoiceBusnumber" disabled="">
                                                    </div>
                                                </div>
                                                <div class="form-group">
                                                    <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                                    <div class="col-md-2">
                                                        @Html.DropDownListFor(Function(m) m.SelectedCoOwnerInvoiceCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCoOwnerInvoiceCountry), New With {.class = "form-control populate", .id = "lstCoOwnerInvoiceCountries", .disabled = ""})
                                                    </div>

                                                    <label class="col-md-1 control-label" for="w4-City">Gemeente</label>
                                                    <div class="col-md-6">
                                                        @Html.HiddenFor(Function(m) m.SelectedCoOwnerInvoicePostalCode, New With {.id = "txtCoOwnerInvoicePostalcode", .class = "form-control", .disabled = ""})
                                                    </div>
                                                </div>
                                                <hr />
                                            </div>
                                            <div class="form-group">
                                                <label class="col-md-2 control-label" for="w4-street">Type eigenaar</label>
                                                <div class="col-md-2">
                                                    @Html.DropDownListFor(Function(m) m.SelectedCoOwnerType, New SelectList(Model.OwnerTypes, "ID", "Display", Model.SelectedCoOwnerType), New With {.class = "form-control populate", .id = "lstCoOwnerType", .disabled = ""})
                                                </div>

                                                <label class="col-md-1 control-label" for="txtCoOwnerPercentage">Percentage</label>

                                                <div class="col-md-2">
                                                    <input type="number" step="0.01" min="0.01" max="99.99" class="form-control" name="txtCoOwnerPercentage" id="txtCoOwnerPercentage" disabled="">
                                                </div>
                                                <div class="col-md-4 ">
                                                    <button class="btn btn-primary btn-block " type="button" id="btnAddCoOwner" disabled=""><i class="fa fa-save"></i> Opslaan</button>
                                                </div>
                                            </div>

                                            <hr />

                                            <h4>Eigenaars</h4>

                                            <table id="CoOwnerTable" class="table table-no-more table-bordered table-striped mb-none ">
                                                <thead>
                                                    <tr>
                                                        <th>Naam</th>
                                                        <th>Type</th>
                                                        <th>%</th>
                                                        <th></th>
                                                    </tr>
                                                </thead>
                                                <tbody id="CoOwnerRows">
                                                    <tr>
                                                        <td data-title='Naam'>
                                                            <label id="txtOwnerName" name="txtOwnerName" />
                                                        </td>
                                                        <td data-title='Type'>
                                                            <label id="txtOwnerType" name="txtOwnerType">Volle Eigenaar</label>
                                                        </td>
                                                        <td data-title='%' id="ownerpercentage">
                                                            100,00
                                                        </td>
                                                        <td data-title=''></td>
                                                    </tr>

                                                    @For Each item In Model.ClientAccount.CoOwners
                                                        Html.RenderPartial("_CoOwnerRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                                    Next


                                                </tbody>
                                            </table>
                                        </div>
                                        <div id="w4-units" class="tab-pane">
                                            <div class="form-group">
                                                <label class="col-md-3 control-label">Entiteiten toevoegen</label>
                                                <div class="col-md-8">
                                                    @Html.ListBoxFor(Function(m) m.SelectedUnits, New SelectList(Model.AvailableUnits, "ID", "Display", "Group", Model.SelectedUnits, Model.AddedUnits, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstUnits"})
                                                </div>
                                            </div>
                                            <div class="form-group">

                                                <div class="col-md-8 col-md-offset-3 ">
                                                    <button class="btn btn-primary btn-block " type="button" id="btnAddUnits">Toevoegen</button>
                                                </div>
                                            </div>


                                            <hr>
                                            <h4>Toegevoegde entiteiten</h4>

                                            <table class="table table-no-more table-bordered table-striped mb-none">
                                                <thead>
                                                    <tr>
                                                        <th class="col-md-1">#</th>
                                                        <th class="col-md-1">Entiteit</th>
                                                        <th class="col-md-4">Grondwaarde verkoop</th>
                                                        <th class="col-md-4">Bouwwaarde verkoop</th>
                                                        <th class="col-md-2">Verwijderen</th>
                                                    </tr>
                                                </thead>
                                                <tbody id="UnitRows">

                                                    @For Each item In Model.AddedUnits
                                                        Html.RenderPartial("_UnitRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                                    Next


                                                </tbody>
                                            </table>
                                        </div>

                                    </div>

                            </text>
End Using

                            </div>

                <div class="panel-footer">
                    <ul class="pager">
                        <li class="previous disabled">
                            <a><i class="fa fa-angle-left"></i> Vorige</a>
                        </li>
                        <li class="finish hidden pull-right">
                            <button class="btn btn-primary" type="submit">Klantenaccount opslaan</button>
                        </li>
                        <li class="next">
                            <a>Volgende <i class="fa fa-angle-right"></i></a>
                        </li>
                    </ul>
                </div>

        </section>





        <!-- end: page -->

    </div>
</div>

<div id="ModalDeleteUnit" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-unit-container"></div>
</div>
<div id="ModalAddLevel" class="modal-block modal-block-primary mfp-hide">
    <div id="add-level-container"></div>
</div>
<div id="ModalEditNews" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-news-container"></div>
</div>

@section scripts

<script>
    $('#checkboxCompany').change(function () {
        if ($(this).is(":checked")) {
            $('#lstSalutationAccount').attr("disabled", "disabled");
            $('#txtinputname').attr("disabled", "disabled");
            $('#txtVatNumber').removeAttr("disabled");
            $('#txtCompanyName').removeAttr("disabled");
            return;
        }
        $('#lstSalutationAccount').removeAttr("disabled");
        $('#txtinputname').removeAttr("disabled");
        $('#txtVatNumber').attr("disabled", "disabled");
        $('#txtCompanyName').attr("disabled", "disabled");
    });
    $('#checkboxCoOwnerCompany').change(function () {
        if ($(this).is(":checked")) {
            $('#lstCoOwnerSalutation').attr("disabled", "disabled");
            $('#txtCoOwnerName').attr("disabled", "disabled");
            $('#txtCoOwnerForeName').attr("disabled", "disabled");
            $('#txtCoOwnerVATNumber').removeAttr("disabled");
            $('#txtCoOwnerCompanyName').removeAttr("disabled");

            return;
        }
        $('#lstCoOwnerSalutation').removeAttr("disabled");
        $('#txtCoOwnerName').removeAttr("disabled");
        $('#txtCoOwnerForeName').removeAttr("disabled");
        $('#txtCoOwnerVATNumber').attr("disabled", "disabled");
        $('#txtCoOwnerCompanyName').attr("disabled", "disabled");
    });
    $('#checkboxInvoiceAddress').change(function () {
        if ($(this).is(":checked")) {
            $('.InvoiceAddress').removeAttr("hidden");
            return;
        }
        $('.InvoiceAddress').attr("hidden", "hidden");
    });
    $('#checkboxCoOnwerInvoiceAddress').change(function () {
        if ($(this).is(":checked")) {
            $('.CoOwnerInvoiceAddress').removeAttr("hidden");
            return;
        }
        $('.CoOwnerInvoiceAddress').attr("hidden", "hidden");
    });
    //selecteren van een land
    var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    function loadCountry() {
        var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    }

    $('#lstCompanyCountries').on('change', function () {

        iCountryId = this.value;
    });
    //selecteren van een land bij facturatiegegevens
    var iClientInvoiceCountryId = jQuery("#lstClientInvoiceCountries option:selected").val();
    function loadCountry2() {
        var iClientInvoiceCountryId = jQuery("#lstClientInvoiceCountries option:selected").val();
    }
    $('#lstClientInvoiceCountries').on('change', function () {
        iClientInvoiceCountryId = this.value;
    });
    //selecteren van een land bij mede-eigenaar
    var iCoOwnerCountryId = jQuery("#lstCoOwnerCountries option:selected").val();
    $('#lstCoOwnerCountries').on('change', function () {
        iCoOwnerCountryId = this.value;
    });
    //selecteren van een land invoice bij mede-eigenaar
    var iCoOwnerInvoiceCountryId = jQuery("#lstCoOwnerInvoiceCountries option:selected").val();
    $('#lstCoOwnerInvoiceCountries').on('change', function () {
        iCoOwnerInvoiceCountryId = this.value;
    });
    //Messagesystem
    $(window).load(function () {
        @If Not TempData("Message") Is Nothing Then
        @<text>

        new PNotify({
            title:      '@TempData("MessageTitle")',
            text:       '@TempData("Message")',
            type:       '@TempData("MessageType")'
        });
        </text>                          End If
    });
    //nieuwe rij toevoegen aan de contacten op tab 1
  $("#addContact").click(function(){
      $.ajax({
          url:this.href,
          cache:false,
          success:function(html) {$("#editorRows").append(html);}
      });
      return false;
  });

  //verwijderen van een contact
  $(document).on('click', 'a.deleteContact', function () { // <-- changes

      $(this).closest('.contactRow').remove();
      return false;
  });
    //toevoegen van een entiteit
    $('#btnAddUnits').click(function () {

        var el = document.getElementById('lstUnits');
        var result = [];
        var options = el && el.options;
        var opt;

        for (var i = 0, iLen = options.length; i < iLen; i++) {
            opt = options[i];

            if (opt.selected) {

                $.ajax({
                    url: '@Url.Action("AddSelectedUnits", "Klanten")',
                    data: { UnitId: opt.value, UnitName: opt.text, UnitGroup : $(opt).parent().attr("label") },
                    cache: false,
                    traditional: true,
                    type: 'POST',
                    success: function (result) {

                        $('#UnitRows').append(result);
                    },

                });

            }
        }
        $("#lstUnits option:selected").attr('disabled', 'disabled');
        $("#lstUnits").val(null).trigger("change");
    });
    //verwijderen van entiteit uit een klantenaccount
    $(document).on('click', 'a.deleterow', function () { // <-- changes
        var $row = $(this).closest('tr')
        var $id = $row.find('td').eq(0).text();
        $("#lstUnits option[value=" + $id + "]").removeAttr('disabled').change();
        $(this).closest('tr').remove();
        return false;
    });
    //toevoegen van een mede-eigenaar
    $('#btnAddCoOwner').click(function () {
        var sum = 100.00;
        $('.percentage').each(function(){
            var percentage = $(this);

            sum -= parseFloat(percentage.text()).toFixed(2);

        });
        if (!$('#txtCoOwnerName').val()) {
            $('#txtCoOwnerName').parent('div').addClass('has-error');
        }
        else if (!$('#txtCoOwnerPercentage').val()) {
            $('#txtCoOwnerPercentage').parent('div').addClass('has-error');
        }
        else if (!$('#txtCoOwnerStreet').val()) {
            $('#txtCoOwnerStreet').parent('div').addClass('has-error');
        }
        else if (!$('#txtCoOwnerHousenumber').val()) {
            $('#txtCoOwnerHousenumber').parent('div').addClass('has-error');
        }
        else if (!$('#txtCoOwnerPostalcode').val()) {
            $('#txtCoOwnerPostalcode').parent('div').addClass('has-error');
        }
        else if (sum <=0) {
            $('#txtCoOwnerPercentage').parent('div').addClass('has-error');
        }
        else {
            $.ajax({
                url: '/Klanten/AddCoOwner',
                data: {
                    Name: $('#txtCoOwnerName').val(),
                    Forename: $('#txtCoOwnerForeName').val(),
                    Salutation : $('#lstSalutationCoOwner').val(),
                    Street: $('#txtCoOwnerStreet').val(),
                    Housenumber: $('#txtCoOwnerHousenumber').val(),
                    Busnumber: $('#txtCoOwnerBusnumber').val(),
                    Zipcode: $('#txtCoOwnerPostalcode').val(),
                    Phone: $('#txtCoOwnerPhone').val(),
                    Cellphone: $('#txtCoOwnerCellPhone').val(),
                    Email: $('#txtCoOwnerEmail').val(),
                    OwnerType: $('#lstCoOwnerType').val(),
                    OwnerPercentage : $('#txtCoOwnerPercentage').val(),
                    VatNumber :  $('#txtCoOwnerVATNumber').val(),
                    CompanyName : $('#txtCoOwnerCompanyName').val(),
                    InvoiceAddress : $('#checkboxCoOwnerInvoiceAddress').val(),
                    InvoiceStreet: $('#txtCoOwnerInvoiceStreet').val(),
                    InvoiceHousenumber: $('#txtCoOwnerInvoiceHousenumber').val(),
                    InvoiceBusnumber: $('#txtCoOwnerInvoiceBusnumber').val(),
                    InvoiceZipcode: $('#txtCoOwnerInvoicePostalcode').val()
                },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $("#txtOwnerPercentage").text($("#txtinputname").val());
                    $('#CoOwnerRows').append(result);
                    $('#txtCoOwnerName').val(null);
                    $('#txtCoOwnerForename').val(null);
                    $('#txtCoOwnerStreet').val(null);
                    $('#txtCoOwnerHousenumber').val(null);
                    $('#txtCoOwnerBusnumber').val(null);
                    $('#txtCoOwnerEmail').val(null);
                    $('#txtCoOwnerPhone').val(null);
                    $('#txtCoOwnerCellphone').val(null);
                    $('#txtCoOwnerPercentage').val(null);
                    $('#txtCoOwnerVATNumber').val(null);
                    $('#txtCoOwnerCompanyName').val(null);
                    $('#lstCoOwnerSalutation').val(0);
                    $('#lstCoOwnerType').val('1');
                    $('#lstCoOwnerCountries').val('19');
                    $('#txtCoOwnerName').parent('div').removeClass('has-error');
                    $('#txtCoOwnerPercentage').parent('div').removeClass('has-error');
                    $('#txtCoOwnerStreet').parent('div').removeClass('has-error');
                    $('#txtCoOwnerHousenumber').parent('div').removeClass('has-error');
                    $('#txtCoOwnerPostalcode').parent('div').removeClass('has-error');
                    $('#txtCoOwnerInvoiceStreet').val(null);
                    $('#txtCoOwnerInvoiceHousenumber').val(null);
                    $('#txtCoOwnerInvoiceBusnumber').val(null);
                    $('#lstCoOwnerInvoiceCountries').val('19');
                    sum = 100.00;
                    $('.percentage').each(function(){
                        var percentage = $(this);
                        var p = percentage.text().replace(",",".");
                        sum -= parseFloat(p).toFixed(2);
                    });
                    $('#txtCoOwnerPercentage').attr("max",sum-0.01)
                    $('#ownerpercentage').html(sum.toLocaleString('nl-BE',{minimumFractionDigits:2}));

                },

            });
        }


    });
    //verwijderen van mede-eigenaar
    $(document).on('click', 'a.deleteCoOwnerRow', function () { // <-- changes

        $(this).closest('tr').remove();
        sum = 100.00;
        $('.percentage').each(function(){
            var percentage = $(this);
            var p = percentage.text().replace(",",".");
            sum -= parseFloat(p).toFixed(2);
        });
        $('#txtCoOwnerPercentage').attr("max",sum-0.01)
        $('#ownerpercentage').html(sum.toLocaleString('nl-BE',{minimumFractionDigits:2}));
        return false;
    });
    //naam update in tweede tabblad na wijzigen
    $("#txtinputname").on('input',function(){
        $("#txtOwnerName").text($("#txtinputname").val());
    });
    //Type eigenaar update in tweede tabblad na wijzigen
    $('#lstOwnerTypes').on('change', function () {
        if($('#lstOwnerTypes option:selected').val() == 1){
            $('#txtCoOwnerName').attr("disabled","disabled");
            $('#txtCoOwnerForename').attr("disabled","disabled");
            $('#txtCoOwnerStreet').attr("disabled","disabled");
            $('#txtCoOwnerHousenumber').attr("disabled","disabled");
            $('#txtCoOwnerBusnumber').attr("disabled","disabled");
            $('#txtCoOwnerEmail').attr("disabled","disabled");
            $('#txtCoOwnerPhone').attr("disabled","disabled");
            $('#txtCoOwnerCellphone').attr("disabled","disabled");
            $('#txtCoOwnerPercentage').attr("disabled","disabled");
            $('#txtCoOwnerPostalcode').select2('disable');
            $('#txtCoOwnerInvoicePostalcode').select2('disable');
            $('#lstCoOwnerSalutation').attr("disabled","disabled");
            $('#lstCoOwnerType').attr("disabled","disabled");
            $('#lstCoOwnerCountries').attr("disabled","disabled");
            $('#lstCoOwnerInvoiceCountries').attr("disabled","disabled");
            $('#btnAddCoOwner').attr("disabled","disabled");
            $('#checkboxCoOwnerCompany').attr("disabled","disabled");
            $('#txtCoOwnerVATNumber').attr("disabled", "disabled");
            $('#txtCoOwnerCompanyName').attr("disabled", "disabled");
            $('#txtCoOwnerInvoiceStreet').attr("disabled","disabled");
            $('#txtCoOwnerInvoiceHousenumber').attr("disabled","disabled");
            $('#txtCoOwnerInvoiceBusnumber').attr("disabled","disabled");
        } else{
            $('#txtCoOwnerName').removeAttr("disabled");
            $('#txtCoOwnerForename').removeAttr("disabled");
            $('#txtCoOwnerStreet').removeAttr("disabled");
            $('#txtCoOwnerHousenumber').removeAttr("disabled");
            $('#txtCoOwnerBusnumber').removeAttr("disabled");
            $('#txtCoOwnerEmail').removeAttr("disabled");
            $('#txtCoOwnerPhone').removeAttr("disabled");
            $('#txtCoOwnerCellphone').removeAttr("disabled");
            $('#txtCoOwnerPercentage').removeAttr("disabled");
            $('#txtCoOwnerPostalcode').select2('enable');
            $('#txtCoOwnerInvoicePostalcode').select2('enable');
            $('#lstCoOwnerSalutation').removeAttr("disabled");
            $('#lstCoOwnerType').removeAttr("disabled");
            $('#lstCoOwnerCountries').removeAttr("disabled");
            $('#lstCoOwnerInvoiceCountries').removeAttr("disabled");
            $('#btnAddCoOwner').removeAttr("disabled");
            $('#checkboxCoOwnerCompany').removeAttr("disabled");
            $('#txtCoOwnerInvoiceStreet').removeAttr("disabled");
            $('#txtCoOwnerInvoiceHousenumber').removeAttr("disabled");
            $('#txtCoOwnerInvoiceBusnumber').removeAttr("disabled");
            $('#lstCoOwnerType option[value=1]').each(function(){
                $(this).remove();
            });
            if ($('#checkboxCoOwnerCompany').is(":checked")) {
                $('#lstCoOwnerSalutation').attr("disabled", "disabled");
                $('#txtCoOwnerName').attr("disabled", "disabled");
                $('#txtCoOwnerForeName').attr("disabled", "disabled");
                $('#txtCoOwnerVATNumber').removeAttr("disabled");
                $('#txtCoOwnerCompanyName').removeAttr("disabled");
            } else {
                $('#lstCoOwnerSalutation').removeAttr("disabled");
                $('#txtCoOwnerName').removeAttr("disabled");
                $('#txtCoOwnerForeName').removeAttr("disabled");
                $('#txtCoOwnerVATNumber').attr("disabled", "disabled");
                $('#txtCoOwnerCompanyName').attr("disabled", "disabled");
            }

        }
        $("#txtOwnerType").text($("#lstOwnerTypes option:selected").text());
    });

    $(document).ready(function () {
        //select2 postcode tabblad 1
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

        });
        //select2 postcode invoice tabblad 1
        $("#txtInvoicePostalcode").select2({

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
                        CountryId: iClientInvoiceCountryId,
                    };
                },

                results: function (data, page) {
                    return { results: data };
                },

            },

        });
        //select2 postcode tabblad 2
        $("#txtCoOwnerPostalcode").select2({

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
                        CountryId: iCoOwnerCountryId,
                    };
                },

                results: function (data, page) {
                    return { results: data };
                },

            },
            @If Model.SelectedPostalcode > 0 Then
                @<text>
                initSelection: function (element, callback) {
                    var data = {id:@Model.SelectedPostalcode, text: '@Model.ClientAccount.Postalcode.Postcode  - @Model.ClientAccount.Postalcode.Gemeente' };
                    callback(data);
                }
            </text>
            End If



        });
        $("#txtCoOwnerInvoicePostalcode").select2({

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
                        CountryId: iCoOwnerInvoiceCountryId,
                    };
                },

                results: function (data, page) {
                    return { results: data };
                },

            },
           @If Model.SelectedPostalcode > 0 Then
               @<text>
            initSelection: function (element, callback) {
                var data = {id:@Model.SelectedPostalcode, text: '@Model.ClientAccount.Postalcode.Postcode  - @Model.ClientAccount.Postalcode.Gemeente' };
                callback(data);
            }
        </text>
           End If



    });

    });



</script>
<script src="~/vendor/admin/jquery-validation/jquery.validate.js"></script>
<script src="~/vendor/admin/bootstrap-wizard/jquery.bootstrap.wizard.js"></script>
<script src="~/vendor/admin/pnotify/pnotify.custom.js"></script>
<script src="~/Scripts/admin/forms/examples.wizard.js"></script>
<script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
<script src="~/scripts/admin/tables/examples.datatables.editable.js"></script>

end section

