@modeltype ExportGiftsToPdfModel 
    @imports bo

        @Using Html.BeginForm("DetailClientsGiftsPdf", "Projecten", FormMethod.Post, New With {.id = "FormDetailClientsGiftsPdf", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
            @Html.AntiForgeryToken()
            @<text>
                @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                <section class="panel">
                    <header class="panel-heading">
                        <h2 class="panel-title">Selecteer de loten waarvoor u de toegiften wilt hebben :</h2>
                    </header>
                    <div class="panel-body">
                        <div class="form-group">
                           
                            <div class="col-md-12">
                                @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Nothing, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
                            </div>
                        </div>


                    </div>

                    <footer class="panel-footer">
                        <div class="row">
                            <div class="col-md-12 text-right">
                                <button class="btn btn-primary btn-block" id="btnSubmit">Toon Pdf</button>
                                <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                            </div>
                        </div>
                    </footer>
                </section>
            </text>
        End Using
<script>
    $(document).ready(function () {

        $('#lstActivities').select2({
            placeholder: 'Selecteer een of meer loten',
            width: 'resolve',

        });

    });
   
</script>
