@modeltype BO.UnitBO

<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

    @Using Html.BeginForm("UpdateUnit", "Klanten", FormMethod.Post, New With {.id = "FormUpdateUnit", .class = "form-horizontal mb-lg"})

    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">@Model.Type.Name @Html.Raw(" ") @Model.Name bewerken van de klant</h2>
            </header>
            <div class="panel-body">
            @Html.Partial("_ValidationSummary", ViewData.ModelState)
            @Html.HiddenFor(Function(m) m.Id)
            @Html.HiddenFor(Function(m) m.ClientAccountId)
        
                <div class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            @Html.LabelFor(Function(m) m.LandValueSold)
                        </div>
                        <div class="col-md-10">
                        @Html.EditorFor(Function(m) m.LandValueSold)

                            @*@Html.EditorFor(Function(m) m.LandValueSold)*@
                        </div>
                    </div>

                </div>
                <div class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            @Html.LabelFor(Function(m) m.ConstructionValueSold)
                        </div>
                        <div class="col-md-10">
                            @Html.EditorFor(Function(m) m.ConstructionValueSold)
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
   

    //$('#LandValueSold').autoNumeric('init');  //autoNumeric with defaults
    $(function () {
        $('#FormUpdateUnit').submit(function () {
            
                $.ajax({
                    url: this.action,
                    type: this.method,
                    data: $(this).serialize(),
                    success: function (result) {
                        if (result.success === true) {
                            window.location.href = result.url;

                        }
                        else {
                            $('#ValSummary').html(result);
                        }

                    }

                });
            
            
        });
    });


  </script>