@modeltype WWWBCO.ProjectSendMailModel 

@Using Html.BeginForm("SendMailConfirm", "Projects", FormMethod.Post, New With {.id = "FormSendPlan2", .Class = "form-horizontal mb-lg"})
@Html.AntiForgeryToken()
@<text>

    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
    <section class="panel" id="modalsendmailpanel">

        <div class="panel-body">
            <div class="row">
                <div class="col-md-12 text-right">
                    <a href="" class="modal-dismiss fa fa-close" style="color:#CCC"></a>
                </div>
            </div><div class="modal-wrapper">
                <div class="modal-icon center">
                    <i class="modal-icon-featured fa fa-envelope"></i>
                </div>
                <div class="modal-text center">
                    <h4>Informatie opvragen</h4>

                </div>
            </div>
            <hr />
            <div class="col-md-12 ml-none">
                <h4 class="mb-xs">Vul hieronder uw gegevens is.</h4>
                <p>Wij contacteren u zo snel mogelijk.</p>
                <div class="col-md-6 ml-none pl-none">
                    <div class="form-group">
                        @Html.LabelFor(Function(m) m.Name, New With {.class = "col-md-4 text-sm"})
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control", .id = "txtName", .autocomplete = "family-name"})
                        </div>
                    </div>
                    <div class="form-group">
                        @Html.LabelFor(Function(m) m.Firstname, New With {.class = "col-md-4 text-sm"})
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Firstname, New With {.class = "form-control", .id = "txtFirstname"})
                        </div>
                    </div>
                    <div class="form-group">
                        @Html.LabelFor(Function(m) m.Email, New With {.class = "col-md-4 text-sm"})
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control input-sm", .id = "txtEmail"})
                        </div>
                    </div>
                    <div class="form-group">
                        @Html.LabelFor(Function(m) m.Phone, New With {.class = "col-md-4 text-sm"})
                        <div class="col-md-8">
                            @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control", .id = "txtPhone"})
                        </div>
                    </div>
                </div>
                <div class="col-md-6 pr-none mr-none" style="height:100%">
                    <div class="form-group">
                       @Html.LabelFor(Function(m) m.Question, New With {.class = "col-md-12 text-sm"})
                        <div class="col-md-12" style="height:100%">
                            @Html.TextAreaFor(Function(m) m.Question, New With {.class = "form-control", .id = "txtQuestion", .rows = "7", .style = "height:100%"})
                        </div>
                    </div>
                </div>
        </div>
</div>
        <footer class="panel-footer">
            <div class="row">
                <div class="col-md-12 text-right">
                    <button class="btn btn-primary btn-block " id="btn2SendPlan"><i class="fa fa-envelope"></i>&nbsp;&nbsp;&nbsp;Verzenden</button>

                </div>
                <div class="col-md-12 text-center text-primary text-color-dark hidden" id="spinner">Uw vraag wordt verzonden <i class="fa fa-spinner text-color-dark"></i></div>
            </div>
        </footer>
    </section>

</text>
End Using
    <script>
        $("#FormSendPlan2").submit(function (event) {

            /* stop form from submitting normally */
            event.preventDefault();
            $("#btn2SendPlan").addClass("hidden");
            $("#spinner").removeClass("hidden");
            $.ajax({
                url: '@Url.Action("SendMail", "Projects")',
                data:  $('#FormSendPlan2').serialize(),
                type: 'POST',
                success: function (result) {
                    $("#modalsendmailpanel").html(result);
                },
            });
        });



    </script>



