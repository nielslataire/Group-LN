@ModelType LeverancierModel



@Code
    ViewData("Title") = "Leverancier toevoegen"
    
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
    <div class="col-xs-12 col-sm-12">
        
        <section class="panel form-wizard" id="w4">
           
            <header class="panel-heading">
                <div class="panel-actions">
                    <a href="#" class="panel-action panel-action-toggle" data-panel-toggle></a>
                    <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss></a>
                </div>

                <h2 class="panel-title">Wizard leverancier toevoegen</h2>
            </header>
            
            <div class="panel-body">
                
                @Html.Partial("_ValidationSummary", ViewData.ModelState)
                <div class="wizard-progress wizard-progress-md">
                    <div class="steps-progress">
                        <div class="progress-indicator"></div>
                    </div>
                    <ul class="wizard-steps">
                        <li class="active">
                            <a href="#w4-info" data-toggle="tab"><span>1</span>Bedrijfsinfo</a>
                        </li>
                        <li>
                            <a href="#w4-activity" data-toggle="tab"><span>2</span>Activiteiten</a>
                        </li>
                        <li>
                            <a href="#w4-departments" data-toggle="tab"><span>3</span>Afdelingen</a>
                        </li>
                        <li>
                            <a href="#w4-contacts" data-toggle="tab"><span>4</span>Contacten</a>
                        </li>
                        @*<li>
                            <a href="#w4-save" data-toggle="tab"><span>5</span>Opslaan</a>
                        </li>*@
                    </ul>
                </div>

                    @Using Html.BeginForm("Toevoegen", "Leveranciers", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
                        @<text>
                            @*<form class="form-horizontal" novalidate="novalidate">*@
                                   
                   
                    <div class="tab-content">
                        <div id="w4-info" class="tab-pane active">
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-telephone">@Html.LabelFor(Function(m) m.Company.OndNR)</label>
                                <div class="col-md-1">
                                    @Html.TextBoxFor(Function(m) m.Company.Postcode.Country.ISOCode, New With {.class = "form-control", .id = "txtCountryIsoCode"})
                                    @Html.HiddenFor(Function(m) m.Company.Postcode.Country.CountryID)
                                </div>
                                <div class="col-md-3">
                                    @Html.TextBoxFor(Function(m) m.Company.OndNr, New With {.placeholder = "0123.456.789", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "9999.999.999", .id = "txtVAT"})
                                </div>
                                <div class="col-md-5">
                                    <button class="btn btn-primary btn-block " type="button" id="btnCheckCompany">Onderneming opzoeken</button>
                                </div>

                            </div>
                            <div class="form-group">

                                <label class="col-md-2 control-label" for="w4-name">@Html.LabelFor(Function(m) m.Company.Bedrijfsnaam)</label>
                                <div class="col-md-9">
                                    @Html.TextBoxFor(Function(m) m.Company.Bedrijfsnaam, New With {.placeholder = "vb. Copro", .class = "form-control", .id = "txtCompanyName"})
                                   
                                    @*<input type="text" class="form-control" placeholder="Naam" name="name" id="w4-name">*@
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Company.Straat)</label>
                                <div class="col-md-5">
                                    @Html.TextBoxFor(Function(m) m.Company.Straat, New With {.placeholder = "vb. Klaverdries", .class = "form-control", .id = "txtCompanyStreet"})
                                </div>
                                <div class="col-md-2">
                                    @Html.TextBoxFor(Function(m) m.Company.Huisnummer, New With {.placeholder = "Nr", .class = "form-control", .id = "txtCompanyHousenumber"})
                                </div>
                                <div class="col-md-2">
                                    @Html.TextBoxFor(Function(m) m.Company.Busnummer, New With {.placeholder = "Bus", .class = "form-control"})
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                <div class="col-md-3">
                                    @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                                    @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstCompanyCountries"})
                                    @*@Html.TextBoxFor(Function(m) m.Country.CountryID, New With {.placeholder = "", .class = "form-control"})*@
                                </div>

                                <label class="col-md-1 control-label" for="w4-City" onload="loadCountry()">@Html.LabelFor(Function(m) m.Company.Postcode.Gemeente)</label>
                                <div class="col-md-5">
                                   
                                    @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtPostalcode", .class = "form-control"})
</div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Company.Telefoon1)</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        @Html.TextBoxFor(Function(m) m.Company.Telefoon1, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "999/99.99.99"})
                                    </div>
                                </div>

                                <label class="col-md-1 control-label">@Html.LabelFor(Function(m) m.Company.GSM)</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        @Html.TextBoxFor(Function(m) m.Company.GSM, New With {.placeholder = "", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "9999/99.99.99"})
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-email">@Html.LabelFor(Function(m) m.Company.Email)</label>
                                <div class="col-md-9">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-at"></i>
                                        </span>

                                        @Html.TextBoxFor(Function(m) m.Company.Email, New With {.placeholder = "Email", .class = "form-control"})
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-url">@Html.LabelFor(Function(m) m.Company.URL)</label>
                                <div class="col-md-9">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-link"></i>
                                        </span>
                                        @Html.TextBoxFor(Function(m) m.Company.URL, New With {.placeholder = "vb. http://www.google.be", .class = "form-control"})
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                 <label class="col-md-2 control-label" for="w4-cellphone">@Html.LabelFor(Function(m) m.Company.RegNr)</label>
                                <div class="col-md-3">
                                    @Html.TextBoxFor(Function(m) m.Company.RegNr, New With {.placeholder = "00.00.00", .class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "99.99.99"})
                                </div>
                            </div>
                        </div>
                        <div id="w4-activity" class="tab-pane">
                            <div class="form-group">
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


                            <hr>
                            <h4>Toegevoegde activiteiten</h4>
                            
                                <table class="table table-no-more table-bordered table-striped mb-none">
                                    <thead>
                                        <tr>
                                            <th>#</th>
                                            <th>Activiteit</th>
                                            <th>Verwijderen</th>
                                        </tr>
                                    </thead>
                                    <tbody id="ActivityRows">

                                        @For Each item In Model.AddedActivities
                                            Html.RenderPartial("ActivityRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                        Next


                                    </tbody>
                                </table>

                          


                            </div>

                        <div id="w4-departments" class="tab-pane">
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-departement">Afdeling</label>
                                <div class="col-md-9">
                                    <input type="text" class="form-control" placeholder="bv. Verkoop" name="department" id="w4-department">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-street">Adres</label>
                                <div class="col-md-5">
                                    <input type="text" class="form-control" placeholder="Straat" name="departmentstreet" id="w4-departmentstreet">
                                </div>
                                <div class="col-md-2">
                                    <input type="text" class="form-control" placeholder="Nr." name="departmenthousenumber" id="w4-departmenthousenumber">
                                </div>
                                <div class="col-md-2">
                                    <input type="text" class="form-control" placeholder="Bus" name="departmenthousebusnumber" id="w4-departmenthousebusnumber">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-zipcode">Land</label>
                                <div class="col-md-2">
                                    @Html.DropDownListFor(Function(m) m.SelectedDepartmentCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedDepartmentCountry), New With {.class = "form-control populate", .id = "lstDepartmentCountries"})
                                </div>

                                <label class="col-md-1 control-label" for="w4-City">Gemeente</label>
                                <div class="col-md-6">
                                    @Html.HiddenFor(Function(m) m.SelectedDepartmentPostalcode, New With {.id = "txtDepartmentPostalcode", .class = "form-control"})
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label">Telefoon</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        <input id="departmentphone" data-plugin-masked-input data-input-mask="999/99.99.99" placeholder="050/12.34.56" class="form-control">
                                    </div>
                                </div>

                                <label class="col-md-1 control-label">GSM</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        <input id="departmentcellphone" data-plugin-masked-input data-input-mask="9999/99.99.99" placeholder="0495/12.34.56" class="form-control">
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-email">Email</label>
                                <div class="col-md-9">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-at"></i>
                                        </span>
                                        <input type="email" class="form-control" placeholder="Email" name="departmentemail" id="w4-departmentemail">
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <div class="col-md-8 col-md-offset-3 ">
                                    <button class="btn btn-primary btn-block " type="button" id="btnAddDepartment"><i class="fa fa-save"></i> Opslaan</button>
                                </div>
                                
                            </div>
                            <hr>
                            <h4>Toegevoegde Afdelingen</h4>
                            
                                <table class="table table-no-more table-bordered table-striped mb-none ">
                                    <thead>
                                        <tr>
                                            <th>Naam</th>
                                            <th>Adres</th>
                                            <th>Gemeente</th>
                                            <th>Land</th>
                                            <th>Telefoon</th>
                                            <th>GSM</th>
                                            <th>Email</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody id="DepartmentRows">

                                        @For Each item In Model.AddedDepartments
                                        Html.RenderPartial("DepartmentRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                        Next


                                    </tbody>
                                </table>
                          



                        </div>
                        <div id="w4-contacts" class="tab-pane">
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-contactname">Naam</label>
                                <div class="col-md-2">
                                    <select class="form-control mb-md" id="lstSalutation" >
                                        <option selected="selected" value="0" >Dhr.</option>
                                        <option value="1">Mevr.</option>
                                        <option value="2">Familie</option>
                                    </select>
                                </div>
                                <div class="col-md-4">
                                    <input type="text" class="form-control" placeholder="Naam" name="contactname" id="w4-contactname">
                                </div>
                                <div class="col-md-3">
                                    <input type="text" class="form-control" placeholder="Voornaam" name="contactfirstname" id="w4-contactfirstname">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-contactjobfunction">Functie</label>
                                <div class="col-md-9">

                                    <input type="text" class="form-control" placeholder="bv. zaakvoerder" name="contactjobfunction" id="w4-contactjobfunction">

                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label">Telefoon</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        <input id="contactphone" data-plugin-masked-input data-input-mask="999/99.99.99" placeholder="050/12.34.56" class="form-control">
                                    </div>
                                </div>

                                <label class="col-md-1 control-label">GSM</label>
                                <div class="col-md-4">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-phone"></i>
                                        </span>
                                        <input id="contactcellphone" data-plugin-masked-input data-input-mask="9999/99.99.99" placeholder="0495/12.34.56" class="form-control">
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-contactemail">Email</label>
                                <div class="col-md-9">
                                    <div class="input-group">
                                        <span class="input-group-addon">
                                            <i class="fa fa-at"></i>
                                        </span>
                                        <input type="email" class="form-control" placeholder="Email" name="contactemail" id="w4-contactemail">
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <div class="col-md-8 col-md-offset-3 ">
                                    <button class="btn btn-primary btn-block " type="button" id="btnAddContact"><i class="fa fa-save"></i> Opslaan</button>
                                </div>
                            </div>
                            <hr>
                            <h4>Toegevoegde Contacten</h4>
                            
                                <table class="table table-no-more table-bordered table-striped mb-none">
                                    <thead>
                                        <tr>
                                            <th>Naam</th>
                                            <th>Functie</th>
                                            <th>Afdeling</th>
                                            <th>Telefoon</th>
                                            <th>GSM</th>
                                            <th>Email</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody id="ContactRows">

                                        @For Each item In Model.AddedContacts
                                        Html.RenderPartial("ContactRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                        Next


                                    </tbody>
                                </table>
                            

                        </div>


            </text>
                    End Using

            @*<div id="w4-save" class="tab-pane">
        <div class="form-group">
            <label class="col-sm-3 control-label" for="w4-username">Username</label>
            <div class="col-sm-9">
                <input type="text" class="form-control" name="username" id="w4-username" required>
            </div>
        </div>
        <div class="form-group">
            <label class="col-sm-3 control-label" for="w4-password">Password</label>
            <div class="col-sm-9">
                <input type="password" class="form-control" name="password" id="w4-password" required minlength="6">
            </div>
        </div>
    </div>*@
            </div>

            </div>
            <div class="panel-footer">
                <ul class="pager">
                    <li class="previous disabled">
                        <a><i class="fa fa-angle-left"></i> Vorige</a>
                    </li>
                    <li class="finish hidden pull-right">
                        <button class="btn btn-primary" type="submit">Leverancier opslaan</button>
                    </li>
                    <li class="next">
                        <a>Volgende <i class="fa fa-angle-right"></i></a>
                    </li>
                </ul>
            </div>
                 
        </section>
    
    </div>
</div>
@section scripts
    <script>
    var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    var iDepartmentCountryId = jQuery("#lstDepartmentCountries option:selected").val();
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
    //toevoegen van een activiteit aan company
    function loadCountry() {
        var iCountryId = jQuery("#lstCompanyCountries option:selected").val();
    }
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
    //opzoeken van onderneming
    $('#btnCheckCompany').click(function () {
                $.ajax({
                    url: 'CheckCompany',
                    data: {
                        Countryid: $('#txtCountryIsoCode').val(),
                        VAT: $('#txtVAT').val()
                    },
                    cache: false,
                    traditional: true,

                    type: 'POST',
                    success: function (result) {
                        var json = jQuery.parseJSON(result);

                        if (json.valid == true) {
                            $("#txtCompanyName").val(json.name);
                            $("#txtCompanyStreet").val(json.address.street);
                            $("#txtCompanyHousenumber").val(json.address.number);
                            $('#txtCountryIsoCode').val(json.countryCode);
                            $.ajax({
                                url: 'GetPostalcode',
                                data: {
                                    Postalcode: json.address.zip_code,
                                    City: json.address.City,
                                    CountryCode: $('#txtCountryIsoCode').val()
                                },
                                cache: false,
                                traditional: true,

                                type: 'POST',
                                success: function (result) {
                                    //nog aan te vullen nadat service is gemaakt !!!!
                                },
                            });
                          

                        } else {
                            alert("het btw nummer bestaat niet");
                        }


                    },

                });


    });

    //verwijderen van activiteit uit company
    $(document).on('click', 'a.deleterow', function () { // <-- changes
        var $row = $(this).closest('tr')
        var $id = $row.find('td').eq(0).text();
        $("#lstActivities option[value=" + $id + "]").removeAttr('disabled').change();
        $(this).closest('tr').remove();
        return false;
    });
    //toevoegen van een afdeling aan company
    $('#btnAddDepartment').click(function () {


        if (!$('#w4-department').val()) {
            $('#w4-department').parent().parent('div').addClass('has-error');
        }
        else {
            $.ajax({
                url: 'AddDepartment',
                data: {
                    Name: $('#w4-department').val(),
                    Street: $('#w4-departmentstreet').val(),
                    Housenumber: $('#w4-departmenthousenumber').val(),
                    Busnumber: $('#w4-departmenthousebusnumber').val(),
                    Zipcode: $('#txtDepartmentPostalcode').val(),
                    Phone: $('#departmentphone').val(),
                    Cellphone: $('#departmentcellphone').val(),
                    Email: $('#w4-departmentemail').val()
                },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#DepartmentRows').append(result);
                    $('#w4-department').val(null);
                    $('#w4-departmentstreet').val(null);
                    $('#w4-departmenthousenumber').val(null);
                    $('#w4-departmenthousebusnumber').val(null);
                    //$('#txtDepartmentPostalcode').select2('data', { id: null, text: '' });
                    $('#departmentphone').val(null);
                    $('#departmentcellphone').val(null);
                    $('#w4-departmentemail').val(null);
                },

            });
        }


    });
    //verwijderen van afdeling uit company
    $(document).on('click', 'a.deleteDepartmentRow', function () { // <-- changes

        $(this).closest('tr').remove();
        return false;
    });
    //toevoegen van een contact aan company
    $('#btnAddContact').click(function () {


        if (!$('#w4-contactname').val()) {
            $('#w4-contactname').parent().parent('div').addClass('has-error');
        }
        else {
            $.ajax({
                url: 'AddContact',
                data: {
                    Salutation: $('#lstSalutation :selected').text(),
                    Name: $('#w4-contactname').val(),
                    Firstname: $('#w4-contactfirstname').val(),
                    Jobfunction: $('#w4-contactjobfunction').val(),
                    Phone: $('#contactphone').val(),
                    Cellphone: $('#contactcellphone').val(),
                    Email: $('#w4-contactemail').val()
                },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#ContactRows').append(result);
                    $('#lstSalutation').val(0);
                    $('#w4-contactname').val(null);
                    $('#w4-contactfirstname').val(null);
                    $('#w4-contactjobfunction').val(null);
                    $('#contactphone').val(null);
                    $('#contactcellphone').val(null);
                    $('#w4-contactemail').val(null);
                },

            });
        }


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

    // select2 dropdownlist opvullen met postcodes als er 3 char zijn ingetikt

    $(document).ready(function () {

        $("#txtPostalcode").select2({

            minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
            width: 'resolve',   // to adjust proper width of select2 wrapped elements
            placeholder: "Selecteer uw gemeente",
            ajax: {

                url: 'GetPostcodesByCountry',
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

                if (@Model.Company.Postcode.PostcodeId > 0){

                    var data = {id:@Model.Company.Postcode.PostcodeId, text: '@(Model.Company.Postcode.Postcode) - @(Model.Company.Postcode.Gemeente)' };
                    callback(data);

                }

            }



        });
        $("#txtDepartmentPostalcode").select2({
            minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
            width: 'resolve',   // to adjust proper width of select2 wrapped elements

            ajax: {

                url: 'GetPostcodesByCountry',
                cache: false,
                traditional: true,
                type: 'POST',
                data: function (term) {
                    return {
                        term: term,
                        CountryId: iDepartmentCountryId,
                    };
                },

                results: function (data, page) {
                    return { results: data };
                }
            }
        });

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
