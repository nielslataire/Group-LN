@modeltype BO.ProjectPictureBO 
@imports bo
<div id="modaladdphoto" class="modal-block modal-block-primary mfp-hide">
    @Using Html.BeginForm("UploadImage", "Projecten", FormMethod.Post, New With {.id = "FormAddPhoto", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
        @Html.AntiForgeryToken()
        @<text>
    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "PhotoProjectid"})
            <section class="panel">
                <header class="panel-heading">
                    <h2 class="panel-title">Foto toevoegen :</h2>
                </header>
                <div class="panel-body">
                    <div class="form-group">
                        <label class="col-md-3 control-label" for="txtCaption">@Html.LabelFor(Function(m) m.Caption)</label>
                        <div class="col-md-9">
                            @Html.TextBoxFor(Function(m) m.Caption, New With {.placeholder = "Omschrijving", .class = "form-control", .id = "txtCaption"})
                        </div>
                        </div>
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="Type">Selecteer type</label>
                            <div class="col-md-9">
                                @Html.EnumDropDownListFor(Function(m) m.Type, New With {.class = "form-control", .id = "Type"})
                            </div>
                        </div>
                        <div class="form-group">
                            <label class="col-md-3 control-label">Foto</label>
                            <div class="col-md-9">
                                <div class="fileupload fileupload-new" data-provides="fileupload">
                                    <div class="input-append">
                                        <div class="uneditable-input">
                                            <i class="fa fa-file fileupload-exists"></i>
                                            <span class="fileupload-preview"></span>
                                        </div>
                                        <span class="btn btn-default btn-file">
                                            <span class="fileupload-exists">Wijzigen</span>
                                            <span class="fileupload-new">Selecteren</span>
                                            <input type="file" name="files" id="files" multiple  />
                                        </span>
                                        <a href="#" class="btn btn-default fileupload-exists" data-dismiss="fileupload">Verwijderen</a>
                                    </div>
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
