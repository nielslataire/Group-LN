@modeltype ContactModel


    @Using Html.BeginForm("AddUpdateContact", "Leveranciers", FormMethod.Post, New With {.id = "FormAddContact", .class = "form-horizontal mb-lg"})
  
    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Contact 
                @If Not Model.Contact.Id = 0 Then
                @:Bewerken
                Else
                @:Toevoegen
                End If
                :</h2>
              
            </header>
            <div class="panel-body">
                <div id="ValSummary"></div>
            @If Not Model.Contact.Id = 0 Then
            @Html.HiddenFor(Function(m) m.Contact.Id)
            End If 
                @Html.HiddenFor(Function(m) m.Contact.Company.ID)
                @Html.HiddenFor(Function(m) m.Contact.Company.Display)
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.Contact.Salutation)</label>
                    <div class="col-sm-9">
                        @Html.DropDownListFor(Function(m) m.Contact.Salutation, New SelectList(ViewData("SalutationList"), "Value", "Text", Model.Contact.Salutation), New With {.class = "form-control mb-md", .id = "lstSalutation"})
                        @*<select class="form-control mb-md" id="lstSalutation">
                            <option selected="selected" value="0">Dhr.</option>
                            <option value="1">Mevr.</option>
                            <option value="2">Familie</option>
                        </select>*@
                    </div>
                    </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-contactname">@Html.LabelFor(Function(m) m.Contact.Name)</label>
                    <div class="col-sm-5">
                        @Html.TextBoxFor(Function(m) m.Contact.Name, New With {.placeholder = "Naam", .class = "form-control"})
                    </div>
                    <div class="col-sm-4">
                        @Html.TextBoxFor(Function(m) m.Contact.Firstname, New With {.placeholder = "Voornaam", .class = "form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-contactjobfunction">@Html.LabelFor(Function(m) m.Contact.JobFunction)</label>
                    <div class="col-sm-9">

                        @Html.TextBoxFor(Function(m) m.Contact.JobFunction, New With {.placeholder = "Naam", .class = "form-control"})


                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-contactjobfunction">@Html.LabelFor(Function(m) m.Contact.Department.Display)</label>
                    <div class="col-sm-9">
                        @Html.DropDownListFor(Function(m) m.Contact.Department.ID, New SelectList(Model.SelectableDepartments, "ID", "Display", Model.Contact.Department.ID), New With {.class = "form-control mb-md", .id = "lstSalutation"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label">@Html.LabelFor(Function(m) m.Contact.Phone)</label>
                    <div class="col-sm-9">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-phone"></i>
                            </span>

                            @Html.TextBoxFor(Function(m) m.Contact.Phone, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "999/99.99.99"})
                        </div>
                    </div>
                  </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label">@Html.LabelFor(Function(m) m.Contact.CellPhone)</label>
                    <div class="col-sm-9">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-phone"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Contact.CellPhone, New With {.class = "form-control", .data_plugin_masked_input = "", .data_input_mask = "999/99.99.99"})
                        </div>
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-sm-2 control-label" for="w4-contactemail">@Html.LabelFor(Function(m) m.Contact.Email)</label>
                    <div class="col-sm-9">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-at"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Contact.Email, New With {.placeholder = "Email", .class = "form-control"})
                        </div>
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
<script>
   
    $(function () {
        $('#FormAddContact').submit(function () {
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
                            $('#ValSummary').html(result);
                        }
                        
                    }

                });
            }
            return false;
        });
    });
   
   
  </script>