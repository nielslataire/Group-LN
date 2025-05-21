@modeltype AddUnitToClientModel

<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

    @Using Html.BeginForm("AddUnit", "Klanten", FormMethod.Post, New With {.id = "FormAddUnit", .class = "form-horizontal mb-lg"})

    @<text>  
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Eenheid Toevoegen</h2>
            </header>
            <div class="panel-body">
            @Html.Partial("_ValidationSummary", ViewData.ModelState)
            @Html.HiddenFor(Function(m) m.Unit.ClientAccountId)
            <div Class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            Project
                        </div>
                        <div class="col-md-10">
                            @Html.DropDownListFor(Function(m) m.SelectedProject, New SelectList(Model.AvailableProjects, "ID", "Display", "Group", Model.SelectedProject), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstProjects"})
                        </div>

                    </div>
                </div>
                <div Class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            Eenheid
                        </div>
                        <div class="col-md-10">
                            @Html.DropDownListFor(Function(m) m.SelectedUnit, New SelectList(Model.AvailableUnits, "ID", "Display", "Group", Model.SelectedUnit), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstUnits"})
                        </div>

                    </div>
                </div>
                <div class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            @Html.LabelFor(Function(m) m.Unit.LandValueSold)
                        </div>
                        <div class="col-md-10">
                            @Html.EditorFor(Function(m) m.Unit.LandValueSold)
                        </div>
                    </div>

                </div>
                <div class="form-group">
                    <div Class="col-md-12">
                        <div class="col-md-2">
                            @Html.LabelFor(Function(m) m.Unit.ConstructionValueSold)
                        </div>
                        <div class="col-md-10">
                            @Html.EditorFor(Function(m) m.Unit.ConstructionValueSold)
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

        $('#lstProjects').select2({
            placeholder: 'Selecteer Project',
            width: 'resolve',

        });
        $('#lstUnits').select2({
            placeholder: 'Selecteer Eenheid',
            width: 'resolve',

        });
    });


    $('#lstProjects').on("change", function (e) {
        $("#lstUnits").empty();
        $.ajax({
            type: 'POST',
            url: '@Url.Action("GetAvailableUnitsByProjectId", "Shared")',
            dataType: 'json',
            data: { id: $("#lstProjects").val() },
            success: function (units) {
                var $prevGroup, prevGroupName;
                $.each(units, function (i, unit) {
                    if (prevGroupName != unit.Group) {
                        $prevGroup = $('<optgroup />').prop('label', unit.Group).appendTo('#lstUnits');
                    }
                    $("<option />").val(unit.ID).text(unit.Display).appendTo($prevGroup);
                    prevGroupName = unit.Group;
                    //$("#lstUnits").append('<option value="' + unit.id + '">' + unit.text + '</option>');
                });
            },

            error: function (ex) {

                alert('Failed to retrieve units.' + ex);

            }
        });

        return false;

    })
    $(function () {
        $('#FormAddUnit').submit(function () {
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