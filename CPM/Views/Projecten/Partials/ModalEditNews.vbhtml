@modeltype BO.ProjectNewsBO
@imports bo

    @Using Html.BeginForm("EditNews", "Projecten", FormMethod.Post, New With {.id = "FormEditNews", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
        @Html.AntiForgeryToken()
        @<text>
            @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
            @Html.HiddenFor(Function(m) m.Id, New With {.id = "newsid"})
    <section class="panel">
        <header class="panel-heading">
            <h2 class="panel-title">Nieuws bewerken :</h2>
        </header>
        <div class="panel-body">
            <div class="form-group">
                <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.TitleNL)</label>
                <div class="col-md-9">
                    @Html.TextBoxFor(Function(m) m.TitleNL, New With {.placeholder = "Titel", .class = "form-control", .id = "txtTitle"})
                </div>
            </div>
            <div class="form-group">
                <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.TextNL)</label>
                <div class="col-md-9">
                    @Html.TextAreaFor(Function(m) m.TextNL, New With {.placeholder = "Tekst", .class = "form-control", .id = "textareaAutosize", .data_plugin_textarea_autosize = ""})
                </div>
            </div>
            <div class="form-group">
                <label class="col-md-3 control-label">Datum</label>
                <div class="col-md-6">
                    <div class="input-group">
                        <span class="input-group-addon">
                            <i class="fa fa-calendar"></i>
                        </span>
                        @Html.TextBoxFor(Function(m) m.NewsDate, New With {.class = "form-control", .data_plugin_datepicker = "", .data_date_format = "dd/mm/yyyy"})
                        @*<input type="text" data-plugin-datepicker class="form-control">*@
                    </div>
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
                                <input type="file" name="file" id="file" />
                            </span>
                            <a href="#" class="btn btn-default fileupload-exists" data-dismiss="fileupload">Verwijderen</a>
                        </div>
                    </div>
                </div>
            </div>
            @If Not Model.Picture.Id = 0 Then
            @<text>
            <div class="form-group ">
                <div class="col-md-9 col-md-offset-3 ">
                    <div class="img-thumbnail">
                        @Html.HiddenFor(Function(m) m.Picture.Id)


                        <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & Model.Picture.Name)" alt="">
                    </div>
                </div>
            </div>
            </text>
            End If
           
        </div>

        <footer class="panel-footer">
            <div class="row">
                <div class="col-md-12 text-right">
                    <button class="btn btn-primary btn-block ">Bijwerken</button>
                    <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                </div>
            </div>
        </footer>
    </section>
        </text>
            End Using

