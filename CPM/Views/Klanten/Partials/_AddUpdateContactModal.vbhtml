@modeltype BO.ClientContactBO 


    @Using Html.BeginForm("AddUpdateContact", "Klanten", FormMethod.Post, New With {.id = "FormAddContact", .class = "form-horizontal mb-lg"})

    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Contact 
                @If Not Model.Id = 0 Then
                @:Bewerken
                Else
                @:Toevoegen
                End If
                :</h2>
              
            </header>
            <div class="panel-body">
                <div id="ValSummary"></div>
            @If Not Model.Id = 0 Then
            @Html.HiddenFor(Function(m) m.Id)
            End If 
                @Html.HiddenFor(Function(m) m.AccountId)
                <div class="form-group">
                    <div class="col-md-3">
                        @Html.EnumDropDownListFor(Function(m) m.Salutation, New With {.class = "form-control", .id = "lstSalutation"})
                    </div>
                    <div class="col-md-5">
                        @Html.TextBoxFor(Function(m) m.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtName"})
                    </div>
                    <div class="col-md-4">
                        @Html.TextBoxFor(Function(m) m.Firstname, New With {.placeholder = "Voornaam", .class = "form-control", .id = "txtForeName"})
                    </div>
                </div>

                <div class="form-group">
                    <div Class="col-md-12">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-at"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Email, New With {.placeholder = "Email", .class = "form-control", .id = "txtEmail", .type = "email"})
                        </div>
                    </div>
                    </div>
                <div class="form-group">
                    <div class="col-md-6">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-phone"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control Phonemask", .placeholder = "Telefoon"})
                        </div>
                    </div>
                    <div Class="col-md-6">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-mobile"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.Cellphone, New With {.class = "form-control Cellphonemask", .placeholder = "Mobiel"})
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

<script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
<script>
    jQuery(function ($) {
        $(".Phonemask").mask("999/99.99.99");
        $(".Cellphonemask").mask("9999/99.99.99");
    });
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