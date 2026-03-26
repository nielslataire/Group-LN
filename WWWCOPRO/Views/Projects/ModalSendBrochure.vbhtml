@modeltype WWWCOPRO.ProjectSendBrochureModel

@Using Html.BeginForm("SendBrochure", "Projects", FormMethod.Post, New With {.id = "FormSendBrochure", .Class = "mb-none"})
    @Html.AntiForgeryToken()
    @<text>

        @Html.HiddenFor(Function(m) m.DocId, New With {.id = "brochuredocid"})
        <div class="contact-modal-container" id="modalsendbrochurepanel">

            <div class="contact-modal-header">
                <button type="button" class="modal-dismiss contact-modal-close" aria-label="Sluiten">
                    <i class="fa fa-times"></i>
                </button>
                <div class="contact-modal-icon-wrap">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>
                </div>
                <h3 class="contact-modal-title">Brochure aanvragen</h3>
                <div class="contact-modal-deco"></div>
                <p class="contact-modal-subtitle">We bezorgen u de brochure onmiddellijk via e-mail.</p>
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
                <button type="submit" class="contact-btn-submit" id="btnBrochureSend">
                    <i class="fa fa-spinner fa-spin hidden" id="btnBrochureSpinner"></i>
                    <svg id="btnBrochureIcon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg>
                    Versturen
                </button>
                <p class="contact-privacy-note">Uw gegevens worden vertrouwelijk behandeld. <a href="/privacy">Privacybeleid &rarr;</a></p>
            </div>

        </div>

    </text>
End Using
<script>
    $("#FormSendBrochure").submit(function (event) {
        event.preventDefault();
        var $button = $("#btnBrochureSend");
        var $spinner = $("#btnBrochureSpinner");
        var $icon = $("#btnBrochureIcon");
        if (typeof saveContactInfoToCookie === 'function') {
            saveContactInfoToCookie({
                firstname: $("#txtFirstname").val(),
                name: $("#txtName").val(),
                email: $("#txtEmail").val(),
                phone: $("#txtPhone").val()
            });
        }
        window.dataLayer = window.dataLayer || [];
        window.dataLayer.push({ event: 'download_form_submit' });
        $button.prop("disabled", true);
        $spinner.removeClass("hidden");
        $icon.addClass("hidden");
        $.ajax({
            url: '@Url.Action("SendBrochure", "Projects")',
            data: $('#FormSendBrochure').serialize(),
            type: 'POST',
            cache: false,
            success: function (result) {
                $("#modalsendbrochurepanel").html(result);
            },
            error: function () {
                $("#modalsendbrochurepanel").html('<div class="contact-modal-header contact-modal-header-error"><h3 class="contact-modal-title">Er is iets misgegaan</h3></div><div class="contact-modal-body contact-modal-body-centered"><p class="contact-result-message">Probeer het opnieuw of bel ons op <a href="tel:+3292164950">09/216.49.50</a>.</p></div>');
            },
            complete: function () {
                $button.prop("disabled", false);
                $spinner.addClass("hidden");
                $icon.removeClass("hidden");
            }
        });
    });
</script>
