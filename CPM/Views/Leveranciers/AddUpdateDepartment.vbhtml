@modeltype DepartmentModel


    @Using Html.BeginForm("AddUpdateDepartment", "Leveranciers", FormMethod.Post, New With {.id = "FormAddDepartment", .class = "form-horizontal mb-lg"})
  
  @*@Using Ajax.BeginForm("AddUpdateDepartement", "Leveranciers", New AjaxOptions With {.HttpMethod = "POST", .InsertionMode = InsertionMode.Replace, .OnSuccess = "updateAddressSuccess()", .OnFailure = "dialogFailure()", .OnBegin = "dialogBegin()", .OnComplete = "dialogComplete()", .UpdateTargetId = "ValSummary"})*@
    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Afdeling 
                @If Not Model.Department.ID = 0 Then
                @:Bewerken
                Else
                @:Toevoegen
                End If
                :</h2>
              
            </header>
            <div class="panel-body">
                <div id="ValSummary"></div>
                @Html.HiddenFor(Function(m) m.Department.ID)
                @Html.HiddenFor(Function(m) m.Department.Company.ID)
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-departement">Afdeling</label>
                    <div class="col-sm-9">
                        @Html.TextBoxFor(Function(m) m.Department.Name, New With {.placeholder = "Naam afdeling", .class = "form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-street">Adres</label>
                    <div class="col-sm-5">
                        @Html.TextBoxFor(Function(m) m.Department.Street, New With {.placeholder = "Straat", .class = "form-control"})
                    </div>
                    <div class="col-sm-2">
                        @Html.TextBoxFor(Function(m) m.Department.Housenumber, New With {.placeholder = "Nr.", .class = "form-control"})
                    </div>
                    <div class="col-sm-2">
                        @Html.TextBoxFor(Function(m) m.Department.Busnumber, New With {.placeholder = "Bus", .class = "form-control"})

                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-zipcode">Land</label>
                    <div class="col-sm-9">
                        @Html.DropDownListFor(Function(m) m.SelectedCountry, New SelectList(Model.Countries, "ID", "Display", Model.SelectedCountry), New With {.class = "form-control populate", .id = "lstDepartmentCountries"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-City">Gemeente</label>
                    <div class="col-sm-9">
                        @Html.HiddenFor(Function(m) m.SelectedPostalcode, New With {.id = "txtDepartmentPostalcode", .class = "form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label">Telefoon</label>
                    <div class="col-sm-4">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-phone"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Department.Phone, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "999/99.99.99"})
                        </div>
                    </div>

                    <label class="col-sm-1 control-label">GSM</label>
                    <div class="col-sm-4">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-phone"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Department.CellPhone, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "9999/99.99.99"})
                        </div>
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-email">Email</label>
                    <div class="col-sm-9">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-at"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Department.Email, New With {.placeholder = "Email", .class = "form-control"})
                        </div>
                    </div>
                </div>


            </div>

            <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block">Opslaan</button>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
            </text>
End Using
<script>
    var iDepartmentCountryId = jQuery("#lstDepartmentCountries option:selected").val();
    $('#lstDepartmentCountries').on('change', function () {
        iDepartmentCountryId = this.value;
        $("#txtDepartmentPostalcode").select2('val','');
    });
    $(function () {
        $('#FormAddDepartment').submit(function () {
            if ($(this).valid()) {
                $.ajax({
                    url: this.action,
                    type: this.method,
                    data: $(this).serialize(),
                    success: function (result) {
                       
                        if(result.success === true){
                            //$.post(result.url,function(partial){
                            //    $('#DepartmentRows').html(partial);
                            //});
                           

                            window.location.href = result.url;
                        }
                        else{
                            //$.post(result.url,function(partial){
                                $('#ValSummary').html(partial);
                            //});
                        }

                    }

                });
            }
            return false;
        });
    });

    $(document).ready(function () {


        $("#txtDepartmentPostalcode").select2({
            minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
            width: 'resolve',   // to adjust proper width of select2 wrapped elements

            ajax: {

                url: '/Leveranciers/GetPostcodesByCountry',
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
            },
            initSelection: function (element, callback) {

                if (@Model.Department.Postalcode.PostcodeId > 0){

                    var data = {id:@Model.Department.Postalcode.PostcodeId , text: '@(Model.department.Postalcode.Postcode ) - @(Model.Department.Postalcode.Gemeente)' };
                    callback(data);

                }

            }
        });
    })
</script>