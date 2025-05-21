@modeltype AddUpdateClientCoOwnerModel

<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

    @Using Html.BeginForm("AddUpdateCoOwner", "Klanten", FormMethod.Post, New With {.id = "FormAddCoOwner", .class = "form-horizontal mb-lg"})

    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Mede-eigenaar 
                @If Not Model.CoOwner.Id = 0 Then
                @:Bewerken
                Else
                @:Toevoegen
                End If
                :</h2>
              
            </header>
            <div class="panel-body">
            @Html.Partial("_ValidationSummary", ViewData.ModelState)
            @If Not Model.CoOwner.Id = 0 Then
            @Html.HiddenFor(Function(m) m.CoOwner.Id)

            End If 
                @Html.HiddenFor(Function(m) m.CoOwner.AccountId)
                @Html.HiddenFor(Function(m) m.CoOwner.IsCoOwner)
                <div class="form-group">
                    <div class="col-md-12">
                        <div class="checkbox-custom checkbox-default">
                        @Html.BasicCheckBoxFor(Function(m) m.IsCompany)
                            @Html.LabelFor(Function(m) m.IsCompany)
                            @*<input type="checkbox" id="checkboxCoOwnerCompany" @If Model.IsCompany = True Then @<text> checked="checked" </text>      End if>
    @Html.LabelFor(Function(m) m.IsCompany)*@
                                            </div>

                    </div>
                </div>
                <div class="form-group">
                    <div class="col-md-3">
                        @Html.EnumDropDownListFor(Function(m) m.CoOwner.Salutation, New With {.class = "form-control", .id = "lstSalutationCoOwner"})

                    </div>
                    <div class="col-md-5">
                        @Html.EditorFor(Function(m) m.CoOwner.Name)
                        
                    </div>
                    <div class="col-md-4">
                        @Html.EditorFor(Function(m) m.CoOwner.Firstname)
                    </div>
                </div>
                <div class="form-group">
                    <div class="col-md-6">
                        @Html.EditorFor(Function(m) m.CoOwner.CompanyName)
                    </div>
                    <div class="col-md-6">
                        @Html.EditorFor(Function(m) m.CoOwner.VATnumber)
                       
                    </div>
                </div>
                <div class="form-group">
                    
                    <div Class="col-md-12">
                        @Html.EditorFor(Function(m) m.CoOwner.Email)
                    </div>
                    </div>
                <div class="form-group">
                    <div class="col-md-6">
                        @Html.EditorFor(Function(m) m.CoOwner.Phone)
                     
                    </div>
                    <div Class="col-md-6">
                        @Html.EditorFor(Function(m) m.CoOwner.Cellphone)
                    </div>

                </div>
                <div class="form-group">
                    
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.CoOwner.Street)
                    </div>
                    <div class="col-md-2">
                        @Html.EditorFor(Function(m) m.CoOwner.Housenumber)
                    </div>
                    <div class="col-md-2">
                        @Html.EditorFor(Function(m) m.CoOwner.Busnumber)
                    </div>
                </div>
                <div class="form-group">
                    
                    @*<div class="col-md-6">
                        @Html.DropDownListFor(Function(m) m.SelectedCoOwnerPostalCode.SelectedCountry, New SelectList(Model.SelectedCoOwnerPostalCode.Countries, "ID", "Display", Model.SelectedCoOwnerPostalCode.SelectedCountry), New With {.class = "form-control populate", .id = "lstCoOwnerCountries"})
                    </div>*@

                   
                    <div class="col-md-12 p-none">
                        @Html.EditorFor(Function(m) m.SelectedCoOwnerPostalCode, New With {.PostcodeId = Model.CoOwner.Postalcode.PostcodeId, .Postcode = Model.CoOwner.Postalcode.Postcode, .Gemeente = Model.CoOwner.Postalcode.Gemeente})
                        @*@Html.HiddenFor(Function(m) m.SelectedCoOwnerPostalCode.SelectedPostalcode, New With {.id = "txtCoOwnerPostalcode", .class = "form-control", .placeholder = "gemeente"})*@
                    </div>
                </div>
                <hr />
                <h5>Facturatiegegevens</h5>
                <div class="form-group">

                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.CoOwner.InvoiceStreet)
                    </div>
                    <div class="col-md-2">
                        @Html.EditorFor(Function(m) m.CoOwner.InvoiceHousenumber)
                    </div>
                    <div class="col-md-2">
                        @Html.EditorFor(Function(m) m.CoOwner.InvoiceBusnumber)
                    </div>
                </div>
                <div class="form-group">
                    <div class="col-md-12 p-none">
                        @Html.EditorFor(Function(m) m.SelectedCoOwnerInvoicePostalCode, New With {.PostcodeId = Model.CoOwner.InvoicePostalcode.PostcodeId, .Postcode = Model.CoOwner.InvoicePostalcode.Postcode, .Gemeente = Model.CoOwner.InvoicePostalcode.Gemeente})
                    </div>
                </div>
                <hr />
                <div class="form-group">
                    
                    <div class="col-md-8">
                        @Html.DropDownListFor(Function(m) m.SelectedCoOwnerType, New SelectList(Model.OwnerTypes, "ID", "Display", Model.SelectedCoOwnerType), New With {.class = "form-control populate", .id = "lstCoOwnerType"})
                    </div>

                  

                    <div class="col-md-4">
                         @Html.TextBoxFor(Function(m) m.CoOwner.CoOwnerPercentage, New With {.Class = "form-control", .placeHolder = "% mede-eigenaar", .data_a_sep = ".", .data_v_max = Model.MaxCoOwnerPercentage, .Data_a_dec = ",", .data_v_min = "0.00"})
                    </div>
                    
                </div>


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

  
<script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
<script>
    
      $('#CoOwner_CoOwnerPercentage').autoNumeric('init');  //autoNumeric with defaults
   
     $('#IsCompany').change(function () {
        if ($(this).is(":checked")) {
            $('#lstSalutationCoOwner').attr("disabled", "disabled");
            $('#CoOwner_Name').attr("disabled", "disabled");
            $('#CoOwner_Firstname').attr("disabled", "disabled");
            $('#CoOwner_VATnumber').removeAttr("disabled");
            $('#CoOwner_CompanyName').removeAttr("disabled");

            return;
        }
        $('#lstSalutationCoOwner').removeAttr("disabled");
        $('#CoOwner_Name').removeAttr("disabled");
        $('#CoOwner_Firstname').removeAttr("disabled");
        $('#CoOwner_VATnumber').attr("disabled", "disabled");
        $('#CoOwner_CompanyName').attr("disabled", "disabled");
    });
    //$(function () {
    //    $('#FormAddCoOwner').submit(function () {
          
    //            $.ajax({
    //                url: this.action,
    //                type: this.method,
    //                data: $(this).serialize(),
    //                success: function (result) {
    //                    if(result.success === true){
    //                        window.location.href = result.url;
                            
    //                    }
    //                    else{
    //                        $('#ValSummary').html(result);
    //                    }
                        
    //                }

    //            });
           
    //    });
    //});
    $(document).ready(function () {
        if ($('#IsCompany').is(":checked")) {
            $('#lstSalutationCoOwner').attr("disabled", "disabled");
            $('#CoOwner_Name').attr("disabled", "disabled");
            $('#CoOwner_Firstname').attr("disabled", "disabled");
        } else {
            $('#CoOwner_VATnumber').attr("disabled", "disabled");
            $('#CoOwner_CompanyName').attr("disabled", "disabled");
        }
    });
   
  </script>