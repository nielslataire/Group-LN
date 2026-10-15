// gl-v2 TYPE 1 "Bevestiging" met wisselende inhoud (DESIGN.md → Modals): vervangt de browser-pop-ups confirm()/alert().
//
// Zelfde opbouw en klassen als Views/Shared/GlV2/_ModalConfirm.cshtml (Bootstrap-modal + .gl-v2-modal-confirm: 38px icoon,
// serif titel, gedempte omschrijving, footer met hairline, knoppen rechts), maar de inhoud wordt per aanroep ingesteld — de
// statische partial is voor "één vast doel per pagina". Bootstrap regelt tonen/verbergen, focus-trap, Esc, scroll-lock en backdrop.
//
//   GlV2Dialog.confirm({ title, description, confirmLabel, cancelLabel, tone, icon }) -> Promise<boolean>
//   GlV2Dialog.alert({ title, description, closeLabel, tone, icon })                  -> Promise<void>
//
// tone: "danger" (standaard; destructief of fout), "warning" (onomkeerbaar maar niet destructief), "success".
(function () {
    "use strict";

    var TONES = {
        danger:  { cls: "is-danger",  btn: "gl-v2-btn-danger",  icon: "ph-trash" },
        warning: { cls: "is-warning", btn: "gl-v2-btn-warning", icon: "ph-warning" },
        success: { cls: "is-success", btn: "gl-v2-btn-primary", icon: "ph-check-circle" }
    };

    function el(tag, className, text) {
        var n = document.createElement(tag);
        if (className) n.className = className;
        if (text != null) n.textContent = text;
        return n;
    }

    function open(opts, withCancel) {
        return new Promise(function (resolve) {
            var tone = TONES[opts.tone] || TONES.danger;
            var icon = opts.icon || tone.icon;

            var modal = el("div", "modal fade gl-v2-modal-confirm");
            modal.tabIndex = -1;
            modal.setAttribute("aria-hidden", "true");
            modal.setAttribute("role", "alertdialog");

            var dialog = el("div", "modal-dialog modal-dialog-centered");
            var content = el("div", "modal-content " + tone.cls);
            var body = el("div", "modal-body");
            var iconWrap = el("span", "gl-v2-modal-icon " + tone.cls);
            var i = el("i", "ph " + icon);
            i.setAttribute("aria-hidden", "true");
            iconWrap.appendChild(i);
            var text = el("div", "gl-v2-modal-text");
            var title = el("h2", "gl-v2-modal-title", opts.title || "");
            var titleId = "gl-v2-dialog-title-" + Date.now();
            title.id = titleId;
            modal.setAttribute("aria-labelledby", titleId);
            var desc = el("p", "gl-v2-modal-desc", opts.description || "");
            text.appendChild(title);
            if (opts.description) text.appendChild(desc);
            body.appendChild(iconWrap);
            body.appendChild(text);

            var footer = el("div", "modal-footer");
            var result = false;
            var cancel = null;
            if (withCancel) {
                cancel = el("button", "gl-v2-btn gl-v2-btn-text", opts.cancelLabel || "Annuleren");
                cancel.type = "button";
                cancel.setAttribute("data-bs-dismiss", "modal");
                footer.appendChild(cancel);
            }
            var ok = el("button", withCancel ? "gl-v2-btn " + tone.btn : "gl-v2-btn gl-v2-btn-text",
                withCancel ? (opts.confirmLabel || "Bevestigen") : (opts.closeLabel || "Sluiten"));
            ok.type = "button";
            if (withCancel) ok.addEventListener("click", function () { result = true; instance.hide(); });
            else ok.setAttribute("data-bs-dismiss", "modal");
            footer.appendChild(ok);

            content.appendChild(body);
            content.appendChild(footer);
            dialog.appendChild(content);
            modal.appendChild(dialog);
            // binnen de .gl-v2-wrapper laten staan zodat de gescopete stijlen gelden
            (document.querySelector(".gl-v2") || document.body).appendChild(modal);

            var instance = new bootstrap.Modal(modal);
            modal.addEventListener("shown.bs.modal", function () { (withCancel && cancel ? cancel : ok).focus(); });
            modal.addEventListener("hidden.bs.modal", function () {
                instance.dispose();
                modal.remove();
                resolve(withCancel ? result : undefined);
            });
            instance.show();
        });
    }

    window.GlV2Dialog = {
        confirm: function (opts) { return open(opts || {}, true); },
        alert: function (opts) { return open(opts || {}, false); }
    };
})();
