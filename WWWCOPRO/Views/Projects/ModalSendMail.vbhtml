@modeltype WWWCOPRO.ProjectSendMailModel

@Using Html.BeginForm("SendMail", "Projects", FormMethod.Post, New With {.id = "FormSendMail", .Class = "mb-none"})
@Html.AntiForgeryToken()
@<text>

    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})

    <div style="position:absolute;left:-9999px;top:-9999px;opacity:0;pointer-events:none;" aria-hidden="true">
        <label for="website_url">Website</label>
        <input type="text" name="website_url" id="website_url" tabindex="-1" autocomplete="off" value="" />
    </div>

    <div class="contact-modal-container" id="modalsendmailpanel">


        <div class="contact-modal-header">
            <button type="button" class="modal-dismiss contact-modal-close" aria-label="Sluiten">
                <i class="fa fa-times"></i>
            </button>
            <div class="contact-modal-icon-wrap">
                <i class="fa fa-envelope"></i>
            </div>
            <h3 class="contact-modal-title">Informatie opvragen</h3>
            <p class="contact-modal-subtitle">Vul uw gegevens in en wij contacteren u zo snel mogelijk.</p>
        </div>


        <div class="contact-modal-body">
            <div class="row">
                <div class="col-sm-6">
                    <div class="contact-form-group">
                        <label for="txtFirstname" class="contact-form-label">Voornaam</label>
                        @Html.TextBoxFor(Function(m) m.Firstname, New With {.class = "contact-form-control", .id = "txtFirstname", .autocomplete = "given-name", .placeholder = "Voornaam"})
                    </div>
                </div>
                <div class="col-sm-6">
                    <div class="contact-form-group">
                        <label for="txtName" class="contact-form-label">Naam <span class="req-star">*</span></label>
                        @Html.TextBoxFor(Function(m) m.Name, New With {.class = "contact-form-control", .id = "txtName", .autocomplete = "family-name", .placeholder = "Achternaam"})
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col-sm-6">
                    <div class="contact-form-group">
                        <label for="txtEmail" class="contact-form-label">E-mailadres <span class="req-star">*</span></label>
                        @Html.TextBoxFor(Function(m) m.Email, New With {.class = "contact-form-control", .id = "txtEmail", .type = "email", .autocomplete = "email", .placeholder = "uw@email.be"})
                    </div>
                </div>
                <div class="col-sm-6">
                    <div class="contact-form-group">
                        <label for="txtPhone" class="contact-form-label">Telefoon <span class="req-star">*</span></label>
                        @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "contact-form-control", .id = "txtPhone", .type = "tel", .autocomplete = "tel", .placeholder = "+32 ..."})
                    </div>
                </div>
            </div>
            <p class="contact-form-required-note"><span class="req-star">*</span> Verplichte velden</p>
        </div>

        <div class="contact-modal-footer">
            <button type="submit" class="contact-btn-submit" id="btnSendMail">
                <i class="fa fa-paper-plane"></i>&nbsp; Versturen
            </button>
            <div class="contact-spinner hidden" id="spinnerSendMail">
                <i class="fa fa-spinner fa-spin"></i>&nbsp; Wordt verzonden&hellip;
            </div>
        </div>

    </div>

</text>
End Using

<script>
    $("#FormSendMail").submit(function (event) {
        event.preventDefault();

        // Honeypot check client-side (extra laag)
        if ($("#website_url").val() !== "") { return; }

        $("#btnSendMail").addClass("hidden");
        $("#spinnerSendMail").removeClass("hidden");

        // Sla contactgegevens op in cookie voor autofill
        var contactData = {
            firstname: $("#txtFirstname").val(),
            name: $("#txtName").val(),
            email: $("#txtEmail").val(),
            phone: $("#txtPhone").val()
        };
        if (typeof saveContactInfoToCookie === 'function') {
            saveContactInfoToCookie(contactData);
        }

        $.ajax({
            url: '@Url.Action("SendMail", "Projects")',
            data: $("#FormSendMail").serialize(),
            type: "POST",
            success: function (result) {
                $("#modalsendmailpanel").html(result);
            },
            error: function () {
                $("#btnSendMail").removeClass("hidden");
                $("#spinnerSendMail").addClass("hidden");
            }
        });
    });
</script>
