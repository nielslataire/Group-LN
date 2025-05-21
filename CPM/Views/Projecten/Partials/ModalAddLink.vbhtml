@modeltype AddUnitLinkModel
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>
        @Using Html.BeginForm("AddUnitLink", "Projecten", FormMethod.Post, New With {.id = "FormAddUnitLInk", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
            @Html.AntiForgeryToken()
            @<text>
                @Html.HiddenFor(Function(m) m.SelectedUnit.Id, New With {.id = "unitid"})
            @Html.HiddenFor(Function(m) m.SelectedUnit.ProjectId)
                <section class="panel">
                    <header class="panel-heading">
                        <h2 class="panel-title">Eenheden koppelen aan @Model.SelectedUnit.Type.Name @Model.SelectedUnit.Name  :</h2>
                    </header>
                    <div class="panel-body">
                        <div class="form-group">
                            <label class="col-md-3 control-label">Eenheden</label>
                            <div class="col-md-9">
                                @Html.DropDownListFor(Function(m) m.SelectedUnits, New SelectList(Model.Units, "ID", "Display", Model.SelectedUnits, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstLinkUnits"})
                            </div>
                        </div>
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.ConstructionValue)</label>
                            <div class="col-md-9">
                                @Html.EditorFor(Function(m) m.Unit.ConstructionValue)
                            </div>

                        </div>
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.LandValue) </label>
                            <div Class="col-md-9">
                                @Html.EditorFor(Function(m) m.Unit.LandValue)
                            </div>

                        </div>

                    </div>

                    <footer class="panel-footer">
                        <div class="row">
                            <div class="col-md-12 text-right">
                                <button class="btn btn-primary btn-block ">Koppelen</button>
                                <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                            </div>
                        </div>
                    </footer>
                </section>
            </text>
        End Using


<script>
    $(document).ready(function () {

        $('#lstLinkUnits').select2({
            placeholder: 'Selecteer Eenheid',
            width: 'resolve',

        });
    });

    </script>
