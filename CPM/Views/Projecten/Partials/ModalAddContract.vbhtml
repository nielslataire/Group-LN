@modeltype ProjectAddContractModel 
@imports bo
<div id="modaladdcontract" class="modal-block modal-block-primary mfp-hide">
    @Using Html.BeginForm("AddContract", "Projecten", FormMethod.Post, New With {.id = "FormAddContract", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
    @Html.AntiForgeryToken()
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Contract toevoegen :</h2>
            </header>
            <div class="panel-body">
               

                <div class="form-group">
                    <label class="col-md-2 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Companies)</label>
                    <div class="col-md-10">
                        @Html.HiddenFor(Function(m) m.Contract.Company.ID, New With {.id = "txtCompany", .class = "form-control"})
                    </div>

                </div>
                <div class="form-group">
                    <label class="col-md-2 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Companies)</label>
                    <div class="col-md-10">
                        @Html.HiddenFor(Function(m) m.SelectedActivities, New With {.id = "lstActivities", .class = "form-control"})


                        @*@Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Model.AddedActivities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})*@
                    </div>

                </div>
            </div>


            <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block ">Toevoegen</button>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
    End Using
</div>


