@modeltype WWWBCO.ProjectSendBrochureModel

@Using Html.BeginForm("SendBrochure", "Projects", FormMethod.Post, New With {.id = "FormSendBrochure", .Class = "form-horizontal mb-lg"})
    @Html.AntiForgeryToken()
    @<text>

        @Html.HiddenFor(Function(m) m.DocId, New With {.id = "brochuredocid"})
        <section class="panel" id="modalsendbrochurepanel">

            <div class="panel-body">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <a href="" class="modal-dismiss fa fa-close" style="color:#CCC"></a>
                    </div>
                </div><div class="modal-wrapper">
                    <div class="modal-icon center">
                        <i class="modal-icon-featured fa fa-file-text-o"></i>
                    </div>
                    <div class="modal-text center">
                        <h4>Download brochure</h4>

                    </div>
                </div>
                <hr />
                <div class="col-md-12 m-md">
                    <h4 class="mb-xs">Vul hieronder uw gegevens in.</h4>
                    <p>We bezorgen u meteen de brochure via mail.</p>
                    <div class="form-group">
                        <label class="col-md-4 control-label">@Html.LabelFor(Function(m) m.Firstname)</label>
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Firstname, New With {.class = "form-control", .id = "txtFirstname", .autocomplete = "given-name"})
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-4 control-label">@Html.LabelFor(Function(m) m.Name)</label>
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control", .id = "txtName", .autocomplete = "family-name"})
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-4 control-label">@Html.LabelFor(Function(m) m.Email)</label>
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control", .id = "txtEmail", .autocomplete = "email"})
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-4 control-label">@Html.LabelFor(Function(m) m.Phone)</label>
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control", .id = "txtPhone", .autocomplete = "tel"})
                        </div>
                    </div>
                </div>
            </div>

            <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block " id="btnBrochureSend"><i class="fa fa-envelope"></i>&nbsp;&nbsp;&nbsp;Verzenden</button>

                    </div>
                </div>
            </footer>
        </section>

    </text>
End Using
<script>
        $("#FormSendBrochure").submit(function (event) {

            event.preventDefault();
            $.ajax({
                url: '@Url.Action("SendBrochure", "Projects")',
                data:  $('#FormSendBrochure').serialize(),
                type: 'POST',
                success: function (result) {
                    $("#modalsendbrochurepanel").html(result);
                },
            });
        });
</script>
