@modeltype WWWBCO.ProjectSendDocModel 

@Using Html.BeginForm("SendPlanConfirm", "Projects", FormMethod.Post, New With {.id = "FormSendPlan2", .Class = "form-horizontal mb-lg"})
@Html.AntiForgeryToken()
@<text>

    @Html.HiddenFor(Function(m) m.DocId, New With {.id = "docid"})
    <section class="panel" id="modalsenddocpanel">

        <div class="panel-body">
            <div class="row">
                <div class="col-md-12 text-right">
                    <a href="" class="modal-dismiss fa fa-close" style="color:#CCC"></a>
                </div>
            </div><div class="modal-wrapper">
                <div class="modal-icon center">
                    <i class="modal-icon-featured fa fa-file"></i>
                </div>
                <div class="modal-text center">
                    <h4>Documenten opvragen</h4>

                </div>
            </div>
            <hr />
            <div class="col-md-12 m-md">
                <h4 class="mb-xs">Vul hieronder uw gegevens is.</h4>
                <p>Het gevraagde document wordt u direct per mail toegestuurd.</p>
                <div class="form-group">
                    <label class="col-md-3 control-label">@Html.LabelFor(Function(m) m.Email)</label>
                    <div class="col-md-8">
                        @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control", .id = "txtEmail"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label">@Html.LabelFor(Function(m) m.Phone)</label>
                    <div class="col-md-8">
                        @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control", .id = "txtPhone"})
                    </div>
                </div>
            </div>
        </div>

        <footer class="panel-footer">
            <div class="row">
                <div class="col-md-12 text-right">
                    <button class="btn btn-primary btn-block " id="btn2SendPlan"><i class="fa fa-envelope"></i>&nbsp;&nbsp;&nbsp;Verzenden</button>

                </div>
            </div>
        </footer>
    </section>

</text>
End Using
    <script>
        $("#FormSendPlan2").submit(function (event) {

            /* stop form from submitting normally */
            event.preventDefault();
            $.ajax({
                url: '@Url.Action("SendDoc", "Projects")',
                data:  $('#FormSendPlan2').serialize(),
                type: 'POST',
                success: function (result) {
                    $("#modalsenddocpanel").html(result);
                },
            });
        });
        function OnSuccess(response) {
            alert(response);
        }

        function OnFailure(response) {
            alert("Whoops! That didn't go so well did it?");
        }
    </script>



