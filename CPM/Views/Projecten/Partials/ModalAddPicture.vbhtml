@Using Html.BeginForm("UploadImage", "Projecten", FormMethod.Post, New With {.enctype = "multipart/form-data", .data_loading_overlay = "", .id = "UploadImageForm"})
    @Html.AntiForgeryToken()
    @<text>

<section class="panel">
    <header class="panel-heading">
        <h2 class="panel-title">Voeg een algemene foto toe :</h2>
    </header>
    <div class="panel-body">
        <div class="form-group">
            <label class="col-md-3 control-label" for="inputDefault">Caption</label>
            <div class="col-md-6">
                <input type="text" class="form-control" id="caption" name="caption">
                <input type="hidden" value="@Model.Project.Id" id="projectid" name="projectid" />
            </div>
        </div>
        <div class="fileupload fileupload-new" data-provides="fileupload">
            <div class="input-append">
                <div class="uneditable-input">
                    <i class="fa fa-file fileupload-exists"></i>
                    <span class="fileupload-preview"></span>
                </div>
                <span class="btn btn-default btn-file">
                    <span class="fileupload-exists">Change</span>
                    <span class="fileupload-new">Select file</span>
                    <input type="file" name="file" />
                </span>
                <a href="#" class="btn btn-default fileupload-exists" data-dismiss="fileupload">Remove</a>
            </div>
        </div>
      


    </div>

    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                <button class="btn btn-primary btn-block ">Uploaden</button>
                <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
            </div>
        </div>
    </footer>
</section>

</text>
End Using