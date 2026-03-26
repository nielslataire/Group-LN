@modeltype WWWCOPRO.ProjectSendPlanModel

@Using Html.BeginForm("SendPlanConfirm", "Projects", FormMethod.Post, New With {.id = "FormSendPlan2", .Class = "mb-none"})
@Html.AntiForgeryToken()
@<text>

    @Html.HiddenFor(Function(m) m.UnitId, New With {.id = "unitid"})
    <div class="contact-modal-container" id="modalsendplanpanel">

        <div class="contact-modal-header">
            <button type="button" class="modal-dismiss contact-modal-close" aria-label="Sluiten">
                <i class="fa fa-times"></i>
            </button>
            <div class="contact-modal-icon-wrap">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><polygon points="1 6 1 22 8 18 16 22 23 18 23 2 16 6 8 2 1 6"/><line x1="8" y1="2" x2="8" y2="18"/><line x1="16" y1="6" x2="16" y2="22"/></svg>
            </div>
            <h3 class="contact-modal-title">Plan opvragen</h3>
            <div class="contact-modal-deco"></div>
            <p class="contact-modal-subtitle">Het gevraagde plan wordt u direct per e-mail toegestuurd.</p>
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
            <button type="submit" class="contact-btn-submit" id="btn2SendPlan">
                <i class="fa fa-spinner fa-spin hidden" id="btn2SendPlanSpinner"></i>
                <svg id="btn2SendPlanIcon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg>
                Versturen
            </button>
            <p class="contact-privacy-note">Uw gegevens worden vertrouwelijk behandeld. <a href="/privacy">Privacybeleid &rarr;</a></p>
        </div>

    </div>

</text>
End Using
<script>
    $("#FormSendPlan2").submit(function (event) {
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
        window.dataLayer = window.dataLayer || [];
        window.dataLayer.push({ event: 'download_form_submit' });
        $button.prop("disabled", true);
        $spinner.removeClass("hidden");
        $icon.addClass("hidden");
        $.ajax({
            url: '@Url.Action("SendPlan", "Projects")',
            data: $('#FormSendPlan2').serialize(),
            type: 'POST',
            success: function (result) {
                $("#modalsendplanpanel").html(result);
            },
            complete: function () {
                $button.prop("disabled", false);
                $spinner.addClass("hidden");
                $icon.removeClass("hidden");
            }
        });
    });
</script>
