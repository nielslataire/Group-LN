// gl-v2 — Projecten/PaymentStagesV2 (design-handoff 21g/21h, "Betalingsschijven"/"Schijf bereikt
// aanduiden"). Eén gedraging: de "Bereikt aanduiden"-knop op een schijf-rij laadt de 21h-modal via AJAX
// (MarkStageReachedModal, zelfde patroon als Facturatie's "Facturen opmaken"), laat toe alle/geen
// eenheden in één klik te (de)selecteren (Niels, 2026-09-30 — "gemakkelijk voor alle loten klaarzetten"),
// en post't de keuze naar MarkStageReached (een gewoon formulier-redirect, geen JSON — de pagina
// herlaadt zelf en toont de nieuwe status via de normale server-render).
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-ps-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function token() { var el = $('input[name="__RequestVerificationToken"]'); return el ? el.value : ""; }
    function bsModal(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }

    var body = document.getElementById("gl-v2-ps-mark-body");
    var confirmBtn = document.getElementById("gl-v2-ps-mark-confirm");
    var currentStageId = null;

    function updateConfirm() {
        var checked = $$(".js-ps-mark-unit", body).filter(function (b) { return b.checked; });
        if (confirmBtn) {
            confirmBtn.disabled = checked.length === 0;
            confirmBtn.textContent = "Bereikt voor " + checked.length + " " + (checked.length === 1 ? "eenheid" : "eenheden");
        }
        var infoText = $("#gl-v2-ps-mark-info-text", body);
        if (infoText) {
            infoText.textContent = checked.length === 0
                ? "Kies minstens één eenheid."
                : checked.length + " " + (checked.length === 1 ? "post komt" : "posten komen") + " in Facturatie onder “Te factureren”. Er wordt nog niets gefactureerd.";
        }
    }

    body.addEventListener("change", function (e) {
        if (e.target.classList.contains("js-ps-mark-unit")) updateConfirm();
    });

    body.addEventListener("click", function (e) {
        var toggle = e.target.closest("#gl-v2-ps-mark-toggle-all");
        if (!toggle) return;
        var boxes = $$(".js-ps-mark-unit", body).filter(function (b) { return !b.disabled; });
        var selectAll = toggle.getAttribute("data-state") !== "all";
        boxes.forEach(function (b) { b.checked = selectAll; });
        toggle.setAttribute("data-state", selectAll ? "all" : "none");
        toggle.textContent = selectAll ? "Geen selecteren" : "Alles selecteren";
        updateConfirm();
    });

    var headerSub = document.getElementById("gl-v2-ps-mark-header-sub");

    $$(".js-ps-mark-reached").forEach(function (btn) {
        btn.addEventListener("click", function () {
            currentStageId = btn.getAttribute("data-stage-id");
            body.innerHTML = '<div class="gl-v2-field-full"><div class="gl-v2-ps-preview-empty">Bezig met laden…</div></div>';
            if (confirmBtn) { confirmBtn.disabled = true; confirmBtn.textContent = "Bereikt voor 0 eenheden"; }
            if (headerSub) {
                headerSub.textContent = "Schijf " + btn.getAttribute("data-stage-number") + " · " +
                    btn.getAttribute("data-stage-name") + " · " + btn.getAttribute("data-stage-percentage") + " %";
            }
            var m = bsModal("gl-v2-ps-mark-modal");
            if (m) m.show();
            fetch(cfg.modalUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&stageId=" + encodeURIComponent(currentStageId))
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    body.innerHTML = html;
                    if (window.GlV2DatePicker) window.GlV2DatePicker.init();
                    updateConfirm();
                })
                .catch(function () { body.innerHTML = '<div class="gl-v2-field-full"><div class="gl-v2-ps-preview-empty">Kon niet geladen worden.</div></div>'; });
        });
    });

    function submitMarkReached(stageId, unitIds, reachedDate, proofMediaId) {
        var form = document.createElement("form");
        form.method = "post";
        form.action = cfg.submitUrl;
        form.style.display = "none";
        var fields = [["__RequestVerificationToken", token()], ["projectId", cfg.projectId], ["stageId", stageId], ["reachedDate", reachedDate]];
        if (proofMediaId) fields.push(["proofMediaId", proofMediaId]);
        fields.forEach(function (pair) {
            var input = document.createElement("input");
            input.type = "hidden"; input.name = pair[0]; input.value = pair[1];
            form.appendChild(input);
        });
        unitIds.forEach(function (id) {
            var input = document.createElement("input");
            input.type = "hidden"; input.name = "unitIds"; input.value = id;
            form.appendChild(input);
        });
        document.body.appendChild(form);
        form.submit();
    }

    if (confirmBtn) {
        confirmBtn.addEventListener("click", function () {
            if (!currentStageId || confirmBtn.disabled) return;
            var unitIds = $$(".js-ps-mark-unit", body).filter(function (b) { return b.checked; }).map(function (b) { return b.value; });
            if (unitIds.length === 0) return;
            var dateInput = $("#gl-v2-ps-mark-date", body);
            var reachedDate = dateInput && dateInput.value ? dateInput.value : new Date().toISOString().slice(0, 10);
            var proofInput = $("#gl-v2-ps-mark-proof-id", body);
            var proofMediaId = proofInput ? proofInput.value : "";

            confirmBtn.disabled = true;
            confirmBtn.classList.add("is-loading");
            submitMarkReached(currentStageId, unitIds, reachedDate, proofMediaId);
        });
    }

    // 21h "BEWIJS" — "Werffoto's kiezen uit Media": opent het geneste picker-modal, selectie sluit het
    // meteen en zet trigger/hidden-input in de eerste modal terug.
    var proofBody = document.getElementById("gl-v2-ps-proof-picker-body");
    body.addEventListener("click", function (e) {
        var trigger = e.target.closest("#gl-v2-ps-proof-trigger");
        var remove = e.target.closest(".gl-v2-ps-proof-trigger-remove");
        if (remove) {
            e.stopPropagation();
            setProof(null, null);
            return;
        }
        if (!trigger || !proofBody) return;
        proofBody.innerHTML = '<div class="gl-v2-ps-preview-empty">Bezig met laden…</div>';
        var pm = bsModal("gl-v2-ps-proof-modal");
        if (pm) pm.show();
        fetch(cfg.proofPickerUrl + "?projectId=" + encodeURIComponent(cfg.projectId))
            .then(function (r) { return r.text(); })
            .then(function (html) { proofBody.innerHTML = html; })
            .catch(function () { proofBody.innerHTML = '<div class="gl-v2-ps-preview-empty">Kon niet geladen worden.</div>'; });
    });

    if (proofBody) {
        proofBody.addEventListener("click", function (e) {
            var card = e.target.closest(".js-ps-proof-pick");
            if (!card) return;
            setProof(card.getAttribute("data-media-id"), card.getAttribute("data-thumb"), card.getAttribute("data-title"));
            var pm = bsModal("gl-v2-ps-proof-modal");
            if (pm) pm.hide();
        });
    }

    function setProof(mediaId, thumbUrl, title) {
        var idInput = $("#gl-v2-ps-mark-proof-id", body);
        var trigger = $("#gl-v2-ps-proof-trigger", body);
        if (!idInput || !trigger) return;
        idInput.value = mediaId || "";
        trigger.textContent = "";
        if (mediaId) {
            trigger.classList.add("has-photo");
            var thumbSpan = document.createElement("span");
            thumbSpan.className = "gl-v2-ps-proof-trigger-thumb";
            var img = document.createElement("img");
            img.src = thumbUrl; img.alt = "";
            var label = document.createElement("span");
            label.textContent = title || "Foto gekozen";
            thumbSpan.appendChild(img); thumbSpan.appendChild(label);
            var removeBtn = document.createElement("button");
            removeBtn.type = "button"; removeBtn.className = "gl-v2-ps-proof-trigger-remove"; removeBtn.title = "Verwijderen";
            removeBtn.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
            trigger.appendChild(thumbSpan); trigger.appendChild(removeBtn);
        } else {
            trigger.classList.remove("has-photo");
            trigger.innerHTML = '<i class="ph ph-image" aria-hidden="true"></i><span id="gl-v2-ps-proof-trigger-label">Werffoto\'s kiezen uit Media</span>';
        }
    }

    // Eén bol rechtstreeks aanklikken = dat ene lot voor die schijf als bereikt aanduiden (Niels,
    // 2026-09-30) — geen modal, datum = vandaag, zelfde server-actie als de "Bereikt aanduiden"-knop
    // maar met exact 1 eenheid. Alleen aanwezig op cellen die nog niet bereikt/gefactureerd zijn
    // (zie PaymentStagesV2.cshtml: .js-ps-mark-one wordt enkel gerenderd voor state "not-reached").
    function markOneUnit(dot) {
        if (dot.classList.contains("is-loading")) return;
        dot.classList.add("is-loading");
        var stageId = dot.getAttribute("data-stage-id");
        var unitId = dot.getAttribute("data-unit-id");
        var today = new Date().toISOString().slice(0, 10);
        submitMarkReached(stageId, [unitId], today);
    }

    document.addEventListener("click", function (e) {
        var dot = e.target.closest(".js-ps-mark-one");
        if (dot) markOneUnit(dot);
    });
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" && e.key !== " ") return;
        var dot = e.target.closest(".js-ps-mark-one");
        if (dot) { e.preventDefault(); markOneUnit(dot); }
    });

    // Schijf terug uitzetten voor één eenheid (Niels, 2026-09-30) — enkel op cellen die "reached" tonen
    // ómdat er een eigen UnitPaymentStageReached-rij bestaat (zie .js-ps-unmark-one in
    // PaymentStagesV2.cshtml, niet gerenderd zolang de groep-brede Invoicable-vlag aanstaat).
    function unmarkOneUnit(dot) {
        if (dot.classList.contains("is-loading") || !cfg.unmarkUrl) return;
        dot.classList.add("is-loading");
        var form = document.createElement("form");
        form.method = "post";
        form.action = cfg.unmarkUrl;
        form.style.display = "none";
        [["__RequestVerificationToken", token()], ["projectId", cfg.projectId],
         ["stageId", dot.getAttribute("data-stage-id")], ["unitId", dot.getAttribute("data-unit-id")]]
            .forEach(function (pair) {
                var input = document.createElement("input");
                input.type = "hidden"; input.name = pair[0]; input.value = pair[1];
                form.appendChild(input);
            });
        document.body.appendChild(form);
        form.submit();
    }

    document.addEventListener("click", function (e) {
        var dot = e.target.closest(".js-ps-unmark-one");
        if (dot) unmarkOneUnit(dot);
    });
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" && e.key !== " ") return;
        var dot = e.target.closest(".js-ps-unmark-one");
        if (dot) { e.preventDefault(); unmarkOneUnit(dot); }
    });

    // Groep-pillen — wisselt welke kaart zichtbaar is, geen round-trip (zelfde client-side patroon als
    // Facturatie's filterpillen).
    var pillBar = document.querySelector(".gl-v2-ps-pills");
    if (pillBar) {
        pillBar.addEventListener("click", function (e) {
            var pill = e.target.closest(".gl-v2-ps-pill");
            if (!pill) return;
            var groupId = pill.getAttribute("data-ps-pill-group");
            $$(".gl-v2-ps-pill", pillBar).forEach(function (p) { p.classList.toggle("is-active", p === pill); });
            $$("[data-ps-group-card]").forEach(function (card) {
                card.hidden = card.getAttribute("data-ps-group-card") !== groupId;
            });
        });
    }
})();
