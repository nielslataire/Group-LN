@ModelType LeverancierModel



@Code
    ViewData("Title") = "Leverancier bewerken"
    
End Code
@section PageStyle
<link href="~/vendor/admin/jstree/themes/default/style.css" rel="stylesheet" />
<link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />


end section


<div class="row">
    <div class="col-xs-12">
        <section class="panel" id="pnlSearch">
            <header class="panel-heading">
                <div class="panel-actions">
                    
                </div>

                <h2 class="panel-title">@Model.Company.Bedrijfsnaam - Bewerken</h2>
               
            </header>
            <div class="panel-body">
                @Html.Partial("_ValidationSummary", ViewData.ModelState)
                <div class="tabs">
                    <ul class="nav nav-tabs" id="mytabs">
                        <li>
                            <a href="#companyinfo" data-toggle="tab">Bedrijfsinfo</a>
                        </li>
                        <li>
                            <a href="#activities" data-toggle="tab">Activiteiten</a>
                        </li>

                        <li>
                            <a href="#departments" data-toggle="tab">Afdelingen</a>
                        </li>

                        <li>
                            <a href="#contacts" data-toggle="tab">Contacten</a>
                        </li>

                    </ul>

                    @Using Html.BeginForm("Bewerken", "Leveranciers", FormMethod.Post, New With {.id = "FormEdit", .class = "form-horizontal"})
                        @<text>
                            @*<form class="form-horizontal" novalidate="novalidate">*@


                            <div class="tab-content">

                                <div id="companyinfo" class="tab-pane">
                                    <div id="MainValSummary"></div>
                                    <div class="form-group">
                                        @Html.HiddenFor(Function(m) m.Company.CompanyId)
                                        <label class="col-sm-2 control-label" for="w4-name">@Html.LabelFor(Function(m) m.Company.Bedrijfsnaam)</label>
                                        <div class="col-sm-9">
                                            @Html.TextBoxFor(Function(m) m.Company.Bedrijfsnaam, New With {.placeholder = "vb. Copro", .class = "form-control"})

                                            @*<input type="text" class="form-control" placeholder="Naam" name="name" id="w4-name">*@
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Company.Straat)</label>
                                        <div class="col-sm-5">
                                            @Html.TextBoxFor(Function(m) m.Company.Straat, New With {.placeholder = "vb. Klaverdries", .class = "form-control"})
                                        </div>
                                        <div class="col-sm-2">
                                            @Html.TextBoxFor(Function(m) m.Company.Huisnummer, New With {.placeholder = "Nr", .class = "form-control"})
                                        </div>
                                        <div class="col-sm-2">
                                            @Html.TextBoxFor(Function(m) m.Company.Busnummer, New With {.placeholder = "Bus", .class = "form-control"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-zipcode">Land</label>
                                        <div class="col-sm-3">
                                            @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                                            @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstCompanyCountries"})
                                            @*@Html.TextBoxFor(Function(m) m.Country.CountryID, New With {.placeholder = "", .class = "form-control"})*@
                                        </div>

                                        <label class="col-sm-1 control-label" for="w4-City" onload="loadCountry()">@Html.LabelFor(Function(m) m.Company.Postcode.Gemeente)</label>
                                        <div class="col-sm-5">

                                            @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtPostalcode", .class = "form-control"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label">@Html.LabelFor(Function(m) m.Company.Telefoon1)</label>
                                        <div class="col-sm-4">
                                            <div class="input-group">
                                                <span class="input-group-addon">
                                                    <i class="fa fa-phone"></i>
                                                </span>
                                                @Html.TextBoxFor(Function(m) m.Company.Telefoon1, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "999/99.99.99"})
                                            </div>
                                        </div>

                                        <label class="col-sm-1 control-label">@Html.LabelFor(Function(m) m.Company.GSM)</label>
                                        <div class="col-sm-4">
                                            <div class="input-group">
                                                <span class="input-group-addon">
                                                    <i class="fa fa-phone"></i>
                                                </span>
                                                @Html.TextBoxFor(Function(m) m.Company.GSM, New With {.placeholder = "", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "9999/99.99.99"})
                                            </div>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-email">@Html.LabelFor(Function(m) m.Company.Email)</label>
                                        <div class="col-sm-9">
                                            <div class="input-group">
                                                <span class="input-group-addon">
                                                    <i class="fa fa-at"></i>
                                                </span>

                                                @Html.TextBoxFor(Function(m) m.Company.Email, New With {.placeholder = "Email", .class = "form-control"})
                                            </div>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-url">@Html.LabelFor(Function(m) m.Company.URL)</label>
                                        <div class="col-sm-9">
                                            <div class="input-group">
                                                <span class="input-group-addon">
                                                    <i class="fa fa-link"></i>
                                                </span>
                                                @Html.TextBoxFor(Function(m) m.Company.URL, New With {.placeholder = "vb. http://www.google.be", .class = "form-control"})
                                            </div>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-sm-2 control-label" for="w4-telephone">@Html.LabelFor(Function(m) m.Company.OndNr)</label>
                                        <div class="col-sm-1">
                                            @Html.TextBoxFor(Function(m) m.Company.Postcode.Country.ISOCode, New With {.class = "form-control", .id = "txtCountryIsoCode"})
                                            @Html.HiddenFor(Function(m) m.Company.Postcode.Country.CountryID)
                                        </div>
                                        <div class="col-sm-3">
                                            @Html.TextBoxFor(Function(m) m.Company.OndNr, New With {.placeholder = "0123.456.789", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "9999.999.999"})
                                        </div>

                                        <label class="col-sm-2 control-label" for="w4-cellphone">@Html.LabelFor(Function(m) m.Company.RegNr)</label>
                                        <div class="col-sm-3">
                                            @Html.TextBoxFor(Function(m) m.Company.RegNr, New With {.placeholder = "00.00.00", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "99.99.99"})

                                        </div>
                                    </div>
                                    <div class="form-group ">
                                        <div class="col-sm-offset-8 col-sm-3">
                                            <button class=" btn btn-primary btn-block" type="submit"><i class="fa fa-save"></i> Opslaan</button>
                                        </div>
                                    </div>
                                    </div>

                                <div id="activities" class="tab-pane">

                                    @*<div class="form-group">
                                        <label class="col-md-3 control-label">Activiteiten toevoegen</label>
                                        <div class="col-md-8">
                                            @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Model.AddedActivities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
                                        </div>
                                    </div>
                                    <div class="form-group">

                                        <div class="col-md-8 col-md-offset-3 ">
                                            <button class="btn btn-primary btn-block " type="button" id="btnAddActivities">Toevoegen</button>
                                        </div>
                                    </div>


                                    <hr>*@
                                  
                                        <table class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                <tr>
                                                    <th class="hidden-xs hidden-sm">#</th>
                                                    <th>Activiteit</th>
                                                    <th>Verwijderen</th>
                                                </tr>
                                            </thead>
                                            <tbody id="ActivityRows">
                                                @Html.Partial("DepartmentRows", Model.AddedActivities)
                                                @*@For Each item In Model.AddedActivities
                    Html.RenderPartial("ActivityRow", item, New ViewDataDictionary() From {{"mode", "edit"}})
                Next*@


                                            </tbody>
                                        </table>

                                    
                                        <br />
                                        <div class="form-group">
                                            <div class="col-sm-12">
                                                <a href="#modaladdactivity" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Activiteit toevoegen" type="button" id="btnAddActivity"><i class="fa fa-plus"></i> Toevoegen</a>
                                            </div>
                                            </div>
                                       

                                </div>

                                <div id="departments" class="tab-pane">
  
                                   
                                    <h4>Afdelingen</h4>
                                   
                                        <table class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                <tr>
                                                    <th>Naam</th>
                                                    <th>Adres</th>
                                                    <th>Gemeente</th>
                                                    <th class="hidden-xs hidden-sm">Land</th>
                                                    <th>Telefoon</th>
                                                    <th>GSM</th>
                                                    <th>Email</th>
                                                    <th>Acties</th>
                                                </tr>
                                            </thead>
                                            <tbody id="DepartmentRows">

                                                @For Each item In Model.AddedDepartments
                                                Html.RenderPartial("DepartmentRow", item, New ViewDataDictionary() From {{"mode", "edit"}})
                                                Next


                                            </tbody>
                                        </table>
                                    
                                    <br />
                                    <div class="form-group">
                                        <div class="col-sm-12">
                                            <a href="#modaladddepartment" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Afdeling toevoegen" type="button" id="btnAddDepartment" data-id="@Model.Company.CompanyId "><i class="fa fa-plus"></i> Toevoegen</a>
                                            </div>
                                        </div>


                                </div>

                                <div id="contacts" class="tab-pane">

                                    
                                    <h4>Contacten</h4>
                                    
                                        <table class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                <tr>
                                                    <th>Naam</th>
                                                    <th>Functie</th>
                                                    <th>Afdeling</th>
                                                    <th>Telefoon</th>
                                                    <th>GSM</th>
                                                    <th>Email</th>
                                                    <th>Acties</th>
                                                </tr>
                                            </thead>
                                            <tbody id="ContactRows">

                                                @For Each item In Model.AddedContacts
                                                Html.RenderPartial("ContactRow", item, New ViewDataDictionary() From {{"mode", "edit"}})
                                                Next


                                            </tbody>
                                        </table>
                                   <br />
                                    <div class="form-group">
                                        <div class="col-sm-12">
                                            <a href="#modaladdcontact" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Contact toevoegen" type="button" id="btnAddContact" data-id="@Model.Company.CompanyId "><i class="fa fa-plus"></i> Toevoegen</a>
                                            </div>
                                        </div>
                                </div>





                        </text>
                    End Using

                </div>

            </div>
        </div>
        
    </section>

</div>
</div>

<div id="modaladdactivity" class="modal-block modal-block-primary mfp-hide">
@Using Html.BeginForm("EditAddActivity", "Leveranciers", FormMethod.Post, New With {.id = "FormAddActivity", .class = "form-horizontal mb-lg"})
    @<text>
    <section class="panel">
        <header class="panel-heading">
            <h2 class="panel-title">Selecteer activiteiten :</h2>
        </header>
        <div class="panel-body">

                <label class="col-md-12">Activiteiten</label>
                <div class="form-group">
                    <div class="col-md-12">
                        @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Model.AddedActivities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
                    @Html.HiddenFor(Function(m) m.Company.CompanyId)
                    </div>
                
                </div>


        </div>

        <footer class="panel-footer">
            <div class="row">
                <div class="col-md-12 text-right">
                    <button class="btn btn-primary btn-block ">Toevoegen</button>
                    <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                </div>
            </div>
        </footer>
    </section>
</text>
End Using
</div>
<div id="modaldeleteactivity" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-activity-container"></div>
</div>
<div id="modaladddepartment" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-department-container"></div>
</div>
<div id="modaldeletedepartment" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-department-container"></div>
</div>
<div id="modaladdcontact" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-contact-container"></div>
</div>
<div id="modaldeletecontact" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-contact-container"></div>
</div>
@section scripts
<script>

    $('#modaladdactivity').on('shown.bs.modal', function () {
        $('#lstActivities').select2('focus');
    })
    var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    var iDepartmentCountryId = jQuery("#lstDepartmentCountries option:selected").val();
    //toevoegen van een activiteit aan company
    function loadCountry() {
        var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    }
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

        switch(@ViewData("activetab")){
            case 0:
                $('#mytabs a[href="#companyinfo"]').tab('show');
                break;
            case 1:
                $('#mytabs a[href="#activities"]').tab('show');
                break;
            case 2:
                $('#mytabs a[href="#departments"]').tab('show');
                break;
            case 3:
                $('#mytabs a[href="#contacts"]').tab('show');
                break;
            default:
                $('#mytabs a[href="#companyinfo"]').tab('show');
        };






    });
    $(function () {
        $('#FormEdit').submit(function () {
            if ($(this).valid()) {
                $.ajax({
                    url: this.action,
                    type: this.method,
                    data: $(this).serialize(),
                    success: function (result) {
                        if(result.success === true){

                            window.location.href = result.url;

                        }
                        else{
                            $('#MainValSummary').html(result);
                        }

                    }

                });
            }
            return false;
        });
    });
    $('#btnAddActivities').click(function () {

        var el = document.getElementById('lstActivities');
        var result = [];
        var options = el && el.options;
        var opt;

        for (var i = 0, iLen = options.length; i < iLen; i++) {
            opt = options[i];

            if (opt.selected) {
                $.ajax({
                    url: 'AddSelectedActivitiy',
                    data: { ActId: opt.value, ActName: opt.text, ActGroup : $(opt).parent().attr("label") },
                    cache: false,
                    traditional: true,
                    type: 'POST',
                    success: function (result) {
                        $('#ActivityRows').append(result);
                    },

                });

            }
        }
        $("#lstActivities option:selected").attr('disabled', 'disabled');
        $("#lstActivities").val(null).trigger("change");
    });
    //verwijderen van activiteit uit company
    $('.deleteActivity').click(function () {
        var url = "/Leveranciers/DeleteActivityModal"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '?id=' + id + '&companyid=' + @(Model.Company.CompanyId)  , function (data) {
            $('#delete-activity-container').html(data);
        });
    });
    //toevoegen van een afdeling aan company
    $('#btnAddDepartment').click(function () {
        var url = "/Leveranciers/AddDepartment"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-department-container').html(data);
        });
    });
    //verwijderen van afdeling uit company - show modal before delete
    $('.deleteDepartment').click(function () {
        var url = "/Leveranciers/DeleteDepartmentModal"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-department-container').html(data);
        });
    });
    //toevoegen van een contact aan company
    $('#btnAddContact').click(function () {
        var url = "/Leveranciers/AddContact"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-contact-container').html(data);
        });
    });
    //verwijderen van contact uit company - show modal before delete
    $('.deleteContact').click(function () {
        var url = "/Leveranciers/DeleteContactModal"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-contact-container').html(data);
        });
    });
    //verwijderen van contact uit company
    $(document).on('click', 'a.deleteContactRow', function () { // <-- changes

        $(this).closest('tr').remove();
        return false;
    });
    //bij selectie land countryid aanpassen ifv de weer te geven postcodes
    $('#lstCompanyCountries').on('change', function () {

        iCountryId = this.value;
        $.ajax({
            url: 'GetCountryISOCode',
            data: { countryid: iCountryId },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {
                $('#txtCountryIsoCode').val(result);
            },

        });
    });
    //bij selectie land in department countryid aanpassen ifv de weer te geven postcodes
    $('#lstDepartmentCountries').on('change', function () {

        iDepartmentCountryId = this.value;
    });


    $(document).ready(function () {
        @*alert(@TempData("Message"));*@


        //modal popup
        $('.editDepartment').magnificPopup({
            type:  'inline',
            src:  'editDepartment',
        });
        $('.deleteDepartment').magnificPopup({
            type:  'inline',
            src:  'deleteDepartment',
        });
        $('.deleteActivity').magnificPopup({
            type:  'inline',
            src:  'deleteActivity',
        });
        $('.editContact').magnificPopup({
            type:  'inline',
            src:  'editContact',
        });
        $('.deleteContact').magnificPopup({
            type:  'inline',
            src:  'deleteContact',
        });

        // Opvullen van AddUpdateDepartment modal
        $('.editDepartment').click(function () {
            var url = "/Leveranciers/UpdateDepartment"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-department-container').html(data);


            });
        });
        $('.editContact').click(function () {
            var url = "/Leveranciers/UpdateContact"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-contact-container').html(data);


            });
        });
        // select2 dropdownlist opvullen met postcodes als er 3 char zijn ingetikt

        $("#txtPostalcode").select2({

            minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
            width:  'resolve',   // to adjust proper width of select2 wrapped elements
            ajax: {

                url:  '/Leveranciers/GetPostcodesByCountry',
                cache: false,
                traditional: true,
                type:  'POST',
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

                if (@Model.Company.Postcode.PostcodeId > 0){

                    var data = {id:@Model.Company.Postcode.PostcodeId, text: '@(Model.Company.Postcode.Postcode) - @(Model.Company.Postcode.Gemeente)' };
                    callback(data);

                }

            }



        });
        //$("#txtDepartmentPostalcode").select2({
        //    minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
        //    width: 'resolve',   // to adjust proper width of select2 wrapped elements

        //    ajax: {

        //        url: 'GetPostcodesByCountry',
        //        cache: false,
        //        traditional: true,
        //        type: 'POST',
        //        data: function (term) {
        //            return {
        //                term: term,
        //                CountryId: iDepartmentCountryId,
        //            };
        //        },

        //        results: function (data, page) {
        //            return { results: data };
        //        }
        //    }
        //});


    });


</script>

<script src="~/vendor/admin/jquery-validation/jquery.validate.js"></script>
<script src="~/vendor/admin/bootstrap-wizard/jquery.bootstrap.wizard.js"></script>
<script src="~/vendor/admin/pnotify/pnotify.custom.js"></script>
<script src="~/Scripts/admin/forms/examples.wizard.js"></script>
<script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
<script src="~/vendor/admin/jstree/jstree.js"></script>
<script src="~/Scripts/admin/ui-elements/examples.treeview.js"></script>
<script src="~/vendor/admin/select2/select2.js"></script>
<script src="~/vendor/admin/select2/select2_locale_nl.js" ></script>
<script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
<script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
End Section
