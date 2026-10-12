// Gl-v2 — contextueel help-zijpaneel (design-handoff punt 41d). Toggle van de "?"-topbarknop,
// altijd op elke gl-v2-pagina geladen (zie _LayoutV2.cshtml) — zelfde eenvoudige
// open/dicht-klasse-patroon als de andere gl-v2-overlays, geen aparte libary nodig.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var btn = document.getElementById("gl-v2-help-button");
        var panel = document.getElementById("glV2HelpPanel");
        var backdrop = document.getElementById("glV2HelpPanelBackdrop");
        if (!btn || !panel) return;

        function tokenValue() {
            var t = document.querySelector('input[name="__RequestVerificationToken"]');
            return t ? t.value : "";
        }

        function setOpen(open) {
            panel.classList.toggle("is-open", open);
            if (backdrop) backdrop.classList.toggle("is-open", open);
            btn.setAttribute("aria-expanded", open ? "true" : "false");
        }

        btn.addEventListener("click", function () {
            setOpen(!panel.classList.contains("is-open"));
        });

        panel.querySelectorAll(".js-gl-v2-hp-close").forEach(function (el) {
            el.addEventListener("click", function () { setOpen(false); });
        });
        if (backdrop) backdrop.addEventListener("click", function () { setOpen(false); });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) setOpen(false);
        });

        // ── "Vraag stellen aan support" (41a/41d) ──────────────────────────────────────────────
        var supportTrigger = panel.querySelector("[data-help-support-trigger]");
        var supportForm = panel.querySelector("[data-help-support-form]");
        var supportSend = panel.querySelector("[data-help-support-send]");
        var supportMessage = panel.querySelector("[data-help-support-message]");

        // Design-handoff 41a — de "Vraag stellen aan support"-knop in de Help-topbar zelf (enkel
        // op /Help) opent hetzelfde paneel/formulier, geen tweede flow.
        var homeCta = document.getElementById("gl-v2-help-support-cta");
        if (homeCta) {
            homeCta.addEventListener("click", function () {
                setOpen(true);
                if (supportForm) {
                    supportForm.hidden = false;
                    if (supportMessage) supportMessage.focus();
                }
            });
        }

        if (supportTrigger && supportForm) {
            supportTrigger.addEventListener("click", function () {
                supportForm.hidden = !supportForm.hidden;
                if (!supportForm.hidden && supportMessage) supportMessage.focus();
            });
        }

        if (supportSend && supportMessage) {
            supportSend.addEventListener("click", function () {
                var message = supportMessage.value.trim();
                if (!message) { supportMessage.focus(); return; }

                supportSend.disabled = true;
                var body = new URLSearchParams();
                body.set("message", message);

                fetch("/Help/Support", {
                    method: "POST",
                    headers: {
                        "RequestVerificationToken": tokenValue(),
                        "Content-Type": "application/x-www-form-urlencoded",
                        "X-Requested-With": "XMLHttpRequest"
                    },
                    body: body.toString(),
                    credentials: "same-origin"
                }).then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
                    .then(function () {
                        supportForm.innerHTML = '<span class="gl-v2-hp-support-sent"><i class="ph ph-check-circle" aria-hidden="true"></i> Verstuurd — we nemen contact op.</span>';
                    })
                    .catch(function () { supportSend.disabled = false; });
            });
        }
    });
})();
