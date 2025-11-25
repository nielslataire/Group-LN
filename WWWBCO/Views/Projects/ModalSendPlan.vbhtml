@modeltype WWWBCO.ProjectSendPlanModel

@Using Html.BeginForm("SendPlanConfirm", "Projects", FormMethod.Post, New With {.id = "FormSendPlan2", .Class = "form-horizontal mb-lg"})
@Html.AntiForgeryToken()
@<text>

    @Html.HiddenFor(Function(m) m.UnitId, New With {.id = "unitid"})
    <section class="panel" id="modalsendplanpanel">

        <div class="panel-body">
            <div class="row">
                <div class="col-md-12 text-right">
                    <a href="" class="modal-dismiss fa fa-close" style="color:#CCC"></a>
                </div>
            </div><div class="modal-wrapper">
                <div class="modal-icon center">
                    <i class="modal-icon-featured fa fa-map-o"></i>
                </div>
                <div class="modal-text center">
                    <h4>Plan opvragen</h4>

                </div>
            </div>
            <hr />
            <div class="col-md-12 m-md">
                <h4 class="mb-xs">Vul hieronder uw gegevens in.</h4>
                <p>Het gevraagde plan wordt u direct per mail toegestuurd.</p>
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
                        @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control", .id = "txtEmail", .type = "email", .required = "required", .autocomplete = "email"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label">@Html.LabelFor(Function(m) m.Phone)</label>
                    <div class="col-md-8">
                        @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control", .id = "txtPhone", .type = "tel", .required = "required", .autocomplete = "tel", .inputmode = "tel"})
                    </div>
                </div>
            </div>
        </div>

        <footer class="panel-footer">
            <div class="row">
                <div class="col-md-12 text-right">
                    <button class="btn btn-primary btn-block " id="btn2SendPlan"><i class="fa fa-spinner fa-spin" id="btn2SendPlanSpinner" style="display:none"></i><i class="fa fa-envelope" id="btn2SendPlanIcon"></i>&nbsp;&nbsp;&nbsp;Verzenden</button>

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
            var $button = $("#btn2SendPlan");
            var $spinner = $("#btn2SendPlanSpinner");
            var $icon = $("#btn2SendPlanIcon");
            if (typeof saveContactInfoToCookie === 'function') {
                saveContactInfoToCookie({
                    firstname: $("#txtFirstname").val(),
                    name: $("#txtName").val(),
                    email: $("#txtEmail").val(),
                    phone: $("#txtPhone").val()
                });
            }
            $button.prop("disabled", true);
            $spinner.show();
            $icon.hide();
            $.ajax({
                url: '@Url.Action("SendPlan", "Projects")',
                data:  $('#FormSendPlan2').serialize(),
                type: 'POST',
                success: function (result) {
                    $("#modalsendplanpanel").html(result);
                },
                complete: function () {
                    $button.prop("disabled", false);
                    $spinner.hide();
                    $icon.show();
                }
            });
        });
        function OnSuccess(response) {
            alert(response);
        }

        function OnFailure(response) {
            alert("Whoops! That didn't go so well did it?");
        }
</script>


