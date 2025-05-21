@modeltype SelectStageDocModel
@imports bo

    @Using Html.BeginForm("SelectStageDoc", "Projecten", FormMethod.Post, New With {.id = "FormAddStageDoc", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
        @Html.AntiForgeryToken()
        @<text>
    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
    @Html.HiddenFor(Function(m) m.StageId)
            <section class="panel">
                <header class="panel-heading">
                    <h2 class="panel-title">Attest toevoegen aan betalingsschijf :</h2>
                </header>
                <div class="panel-body">
                    <div class="form-group">
                        <div class="col-md-12">
                            @Html.DropDownListFor(Function(m) m.DocId, New SelectList(Model.Docs, "ID", "Display", Nothing, Model.DocId, Nothing), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstSelectForDocs"})
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

