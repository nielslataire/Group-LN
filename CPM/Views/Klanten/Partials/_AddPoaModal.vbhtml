@modeltype AddPoaToClientModel

<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

    @Using Html.BeginForm("AddUpdatePoa", "Klanten", FormMethod.Post, New With {.id = "FormAddPoa", .class = "form-horizontal mb-lg"})

    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Aandachtspunt toevoegen</h2>
            </header>
            <div class="panel-body">
            @Html.Partial("_ValidationSummary", ViewData.ModelState)
            @Html.HiddenFor(Function(m) m.POA.AccountId)
                @Html.HiddenFor(Function(m) m.ProjectId)
                @Html.HiddenFor(Function(m) m.POA.Id)
                <div Class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            Omschrijving
                        </div>
                        <div class="col-md-10">
                            @Html.TextAreaFor(Function(m) m.POA.Description, New With {.class = "form-control populate"})

                        </div>

                    </div>
                </div>
                <div Class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            Loten
                        </div>
                        <div class="col-md-10">
                            @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Nothing, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
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
    $(document).ready(function () {

        $('#lstActivities').select2({
            placeholder: 'Selecteer een of meer loten',
            width: 'resolve',
            
        });

    });


    $(function () {
        $('#FormAddPoa').submit(function () {
            if ($(this).valid()) {
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
            }
            return false;
        });
    });


  </script>