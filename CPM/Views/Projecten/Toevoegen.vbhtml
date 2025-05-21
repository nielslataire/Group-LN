@modeltype ProjectModel 
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project toevoegen"
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/summernote/summernote.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/summernote/summernote-bs3.css" />
End Section
<div class="row">
    <div class="col-xs-12">
        <section class="panel" id="pnlAdd">

@Using Html.BeginForm("Toevoegen", "Projecten", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
    @<text>

            <div class="panel-body">
                @Html.Partial("_ValidationSummary", ViewData.ModelState)
                   
                    <div class="form-group">
                        <label class="col-md-3 control-label" for="txtName">@Html.LabelFor(Function(m) m.Project.Name)</label>
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Project.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtName"})
                        </div>
                    </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtStreet">@Html.LabelFor(Function(m) m.Project.Street)</label>
                    <div class="col-md-6">
                        @Html.TextBoxFor(Function(m) m.Project.Street, New With {.placeholder = "Straat", .class = "form-control",.id="txtStreet"})
                    </div>
                    <div class="col-md-2">
                        @Html.TextBoxFor(Function(m) m.Project.HouseNumber, New With {.placeholder = "Huisnr.", .class = "form-control"})
                    </div>
                   
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="lstProjectCountries">Land</label>
                    <div class="col-md-3">
                        @* Hier een dropdownlist met alle landen en belgie standaard geselecteerd *@
                        @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstProjectCountries"})
                        @*@Html.TextBoxFor(Function(m) m.Country.CountryID, New With {.placeholder = "", .class = "form-control"})*@
                    </div>

                    <label class="col-md-1 control-label" for="txtPostalcode" onload="loadCountry()">@Html.LabelFor(Function(m) m.Project.Postalcode.Gemeente)</label>
                    <div class="col-md-4">
                        @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtPostalcode", .class = "form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCommTitle">@Html.LabelFor(Function(m) m.Project.CommercialTitleNL)</label>
                    <div class="col-md-8">
                        @Html.TextBoxFor(Function(m) m.Project.CommercialTitleNL, New With {.placeholder = "Titel", .class = "form-control", .id = "txtCommTitle"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label">@Html.LabelFor(Function(m) m.Project.CommercialTextNL)</label>
                    <div class="col-md-8">
                        @Html.TextAreaFor(Function(m) m.Project.CommercialTextNL, New With {.id = "txtCommText",.rows="6",.class="form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label">@Html.LabelFor(Function(m) m.Project.StartDateConstruction)</label>
                    <div class="col-md-3">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-calendar"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Project.StartDateConstruction, New With {.class = "form-control", .id = "txtStartDate", .data_plugin_datepicker = "", .value = Model.Project.StartDateConstruction.ToString})
                        </div>
                    </div>
                    <label class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Project.ExecutionDays)</label>
                    <div class="col-md-3">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-wrench"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Project.ExecutionDays, New With {.class = "form-control", .id = "txtExecutiondays", .type = "number"})
                        </div>
                    </div>
                </div>
                    <div class="form-group">
                        <label class="col-md-3 control-label">@Html.LabelFor(Function(m) m.Project.DeliveryDate)</label>
                        <div class="col-md-3">
                            <div class="input-group">
                                <span class="input-group-addon">
                                    <i class="fa fa-calendar"></i>
                                </span>
                                @Html.TextBoxFor(Function(m) m.Project.DeliveryDate, New With {.class = "form-control", .id = "txtDeliveryDate", .data_plugin_datepicker = "", .value = Model.Project.DeliveryDate.ToString})
                            </div>
                        </div>
                        <label class="col-md-1 control-label" for="txtWheaterstation">@Html.LabelFor(Function(m) m.Project.WheaterStation)</label>
                        <div class="col-md-4">
                            @Html.HiddenFor(Function(m) m.Project.WheaterStation.Id, New With {.id = "txtWheaterStation", .class = "form-control"})
                        </div>
                     </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtDeveloper">@Html.LabelFor(Function(m) m.Project.Developer)</label>
                    <div class="col-md-3">
                        @Html.HiddenFor(Function(m) m.Project.Developer.ID, New With {.id = "txtDeveloper", .class = "form-control"})
                    </div>
                    <label class="col-md-2 control-label" for="txtBuilder">@Html.LabelFor(Function(m) m.Project.Builder)</label>
                    <div class="col-md-3">
                        @Html.HiddenFor(Function(m) m.Project.Builder.ID, New With {.id = "txtBuilder", .class = "form-control"})
                    </div>

                </div>
               
          
            </div>
            <footer class="panel-footer">
                <button class="btn btn-primary">Opslaan</button>
                <button type="reset" class="btn btn-default">Annuleren</button>
            </footer>
        </text>
End Using
</section>

</div>
    </div>
@section scripts
    <script>
    var iCountryId = jQuery("#lstProjectCountries option:selected").val();
    $(window).load(function () {
        //Berichtencentrum
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
        function loadCountry() {
            var iCountryId = jQuery("#lstProjectCountries option:selected").val();
        }
        $('#lstProjectCountries').on('change', function () {

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
        $(document).ready(function () {
            //$('#txtCommText').summernote({
            //    height:120,
            //    codemirror: { "theme": "ambiance" },
            //});
            $('#txtStartDate').datepicker({
                language:'nl-BE',
                format:'dd/mm/yyyy',
                autoclose:true,
            });
            $('#txtDeliveryDate').datepicker({
                language:'nl-BE',
                format:'dd/mm/yyyy',
                autoclose:true,
            });
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

                    if (@Model.Project.Postalcode.PostcodeId > 0){

                        var data = {id:@Model.Project.Postalcode.PostcodeId, text: '@(Model.Project.Postalcode.Postcode) - @(Model.Project.Postalcode.Gemeente)' };
                        callback(data);

                    }

                }



            });
            
            $("#txtDeveloper").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de projectontwikkelaar",
                ajax: {

                    url: 'GetCompanys',
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
            $("#txtBuilder").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer de bouwheer",
                ajax: {

                    url: 'GetCompanys',
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
            $("#txtWheaterStation").select2({

                minimumInputLength: 1,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Selecteer het KMI weerstation",
                ajax: {

                    url: 'GetWheaterstations',
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
        });
    </script>
    <script src="~/vendor/admin/summernote/summernote.js"></script>
    <script src="~/vendor/admin/bootstrap-maxlength/bootstrap-maxlength.js"></script>
end section