@modeltype BO.ProjectLevelBO
    @imports bo

        @Using Html.BeginForm("AddLevel", "Projecten", FormMethod.Post, New With {.id = "FormAddLevel", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
            @Html.AntiForgeryToken()
            @<text>
                @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                <section class="panel">
                    <header class="panel-heading">
                        <h2 class="panel-title">Niveau toevoegen :</h2>
                    </header>
                    <div class="panel-body">
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="txtLevel">@Html.LabelFor(Function(m) m.Name)</label>
                            <div class="col-md-9">
                                @Html.TextBoxFor(Function(m) m.Name, New With {.placeholder = "Niveau", .class = "form-control", .id = "txtLevel"})
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

