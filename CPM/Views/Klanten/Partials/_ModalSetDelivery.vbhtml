@modeltype DeliveryModel
@imports bo

    @Using Html.BeginForm("SetDelivery", "Klanten", FormMethod.Post, New With {.id = "FormSetDelivery", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
        @Html.AntiForgeryToken()
        @<text>
    @Html.HiddenFor(Function(m) m.ClientId, New With {.id = "clientid"})
@Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
            <section class="panel">
                <header class="panel-heading">
                    <h2 class="panel-title">Document toevoegen :</h2>
                </header>
                <div class="panel-body">
                    <div class="form-group">
                        <label class="col-md-3 control-label" for="txtCaption">@Html.LabelFor(Function(m) m.DeliveryDate)</label>
                        <div class="col-md-9">
                            
                                @Html.EditorFor(Function(m) m.DeliveryDate)
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

