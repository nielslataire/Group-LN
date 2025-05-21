@modeltype BO.ProjectDocBO 
@imports bo
<div id="modaladddoc" class="modal-block modal-block-primary mfp-hide">
    @Using Html.BeginForm("AddDocument", "Projecten", FormMethod.Post, New With {.id = "FormAddPhoto", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
    @Html.AntiForgeryToken()
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
        @Html.HiddenFor(Function(m) m.ClientAccountId, New With {.id = "clientaccountid"})
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Document toevoegen :</h2>
            </header>
            <div class="panel-body">
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCaption">@Html.LabelFor(Function(m) m.Name)</label>
                    <div class="col-md-9">
                       @Html.EnumDropDownListFor(Function(m) m.Type, New With {.class = "form-control", .id = "Type"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCaption">@Html.LabelFor(Function(m) m.Name)</label>
                    <div class="col-md-9">
                        @Html.TextBoxFor(Function(m) m.Name, New With {.placeholder = "Omschrijving", .class = "form-control", .id = "txtCaption"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label">Datum</label>
                    <div class="col-md-9">
                        <div class="input-group">
                            <span class="input-group-addon">
                                <i class="fa fa-calendar"></i>
                            </span>
                            @Html.TextBoxFor(Function(m) m.DocDate, New With {.class = "form-control", .data_plugin_datepicker = "", .data_date_format = "dd/mm/yyyy"})
                            @*<input type="text" data-plugin-datepicker class="form-control">*@
                        </div>
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label">Document</label>
                    <div class="col-md-9">
                        <div class="fileinput fileinput-new input-group" data-provides="fileinput">
                            <div class="form-control" data-trigger="fileinput"><i class="fa fa-file-pdf-o fileinput-exists"></i> <span class="fileinput-filename"></span></div>
                            <span class="input-group-addon btn btn-default btn-file"><span class="fileinput-new">Selecteren</span><span class="fileinput-exists">Wijzigen</span><input type="file" name="file" id="file"></span>
                            <a href="#" class="input-group-addon btn btn-default fileinput-exists" data-dismiss="fileinput">Verwijderen</a>
                        </div>
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