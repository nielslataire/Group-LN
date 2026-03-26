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
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>
            </div>
            <h3 class="contact-modal-title">Informatie aanvragen</h3>
            <div class="contact-modal-deco"></div>
            <p class="contact-modal-subtitle">Vul uw gegevens in en wij contacteren u zo snel mogelijk — vrijblijvend.</p>
        </div>

        <div class="contact-modal-body">
            <div class="contact-form-grid">
                <div>
                    <label for="txtFirstname" class="contact-form-label">Voornaam</label>
                    @Html.TextBoxFor(Function(m) m.Firstname, New With {.class = "contact-form-control", .id = "txtFirstname", .autocomplete = "given-name", .placeholder = "Voornaam"})
                </div>
                <div>
                    <label for="txtName" class="contact-form-label">Naam <span class="req-star">*</span></label>
                    @Html.TextBoxFor(Function(m) m.Name, New With {.class = "contact-form-control", .id = "txtName", .autocomplete = "family-name", .placeholder = "Achternaam"})
                </div>
                <div>
                    <label for="txtEmail" class="contact-form-label">E-mailadres <span class="req-star">*</span></label>
                    @Html.TextBoxFor(Function(m) m.Email, New With {.class = "contact-form-control", .id = "txtEmail", .type = "email", .autocomplete = "email", .placeholder = "uw@email.be"})
                </div>
                <div>
                    <label for="txtPhone" class="contact-form-label">Telefoon <span class="req-star">*</span></label>
                    @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "contact-form-control", .id = "txtPhone", .type = "tel", .autocomplete = "tel", .placeholder = "+32 ..."})
                </div>
            </div>
            <p class="contact-form-required-note"><span class="req-star">*</span> Verplichte velden</p>
        </div>

        <div class="contact-modal-footer">
            <hr class="contact-modal-footer-divider">
            <button type="submit" class="contact-btn-submit" id="btnSendMail">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg>
                Versturen
            </button>
            <div class="contact-spinner hidden" id="spinnerSendMail">
                <i class="fa fa-spinner fa-spin"></i>&nbsp; Wordt verzonden&hellip;
            </div>
            <p class="contact-privacy-note">Uw gegevens worden vertrouwelijk behandeld. <a href="/privacy">Privacybeleid &rarr;</a></p>
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
