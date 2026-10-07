// gl-v2 — Dossiers/NutsFormV2 (design-handoff 31c "Nieuwe nutsaansluiting"). De status rechts volgt uit de datums (Stappenplan,
// verticaal) en wordt live bijgewerkt; titel en EAN worden voorgesteld uit type en eenheid; "Niet-opgeslagen wijzigingen"-badge;
// verplichte titel. De server is de bron van waarheid voor de status (NutsChecklistSpiegel.BerekenStatus).
(function () {
    "use strict";
    var form = document.getElementById("gl-v2-nuts-form");
    if (!form) return;
    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }

    var isNieuw = form.getAttribute("data-is-nieuw") === "1";
    var autoTitel = form.getAttribute("data-auto-titel") === "1";
    var checklistKlaar = form.getAttribute("data-checklist-klaar") === "1";
    var titel = document.getElementById("gl-v2-nuts-titel");
    var unitHidden = document.getElementById("gl-v2-nuts-unit");
    var unitWrap = document.getElementById("gl-v2-nuts-unit_select");
    var dirty = document.getElementById("gl-v2-dos-dirty-badge");
    var eanInput = document.getElementById("gl-v2-nuts-ean");
    var meterInput = document.getElementById("gl-v2-nuts-meter");
    var eanHelp = document.getElementById("gl-v2-nuts-ean-help");
    var eanLabel = document.getElementById("gl-v2-nuts-ean-label");

    function nutsType() { var r = $$('input[name="NutsType"]', form).filter(function (x) { return x.checked; })[0]; return r ? r.value : "0"; }
    function typeNaam() { var r = $$('input[name="NutsType"]', form).filter(function (x) { return x.checked; })[0]; return r ? r.parentNode.textContent.trim() : "Nutsaansluiting"; }
    function unitNaam() {
        if (!unitHidden || !unitHidden.value) return "";
        var opt = unitWrap && unitWrap.querySelector('.gl-v2-select-option[data-value="' + unitHidden.value + '"]');
        return opt ? opt.textContent.trim() : "";
    }

    // ── Titel voorstellen uit type en eenheid ─────────────────────────────────────────────────────
    function suggestTitel() {
        if (!autoTitel || !titel) return;
        var u = unitNaam();
        titel.value = typeNaam() + " — " + (u || "algemene aansluiting");
        var h = document.getElementById("gl-v2-nuts-titel-help"); if (h) h.textContent = "voorgesteld uit type en eenheid";
    }
    if (titel) titel.addEventListener("input", function () {
        autoTitel = false;
        var h = document.getElementById("gl-v2-nuts-titel-help"); if (h) h.textContent = "";
        titel.closest(".gl-v2-field").classList.remove("is-error");
        var e = document.getElementById("gl-v2-nuts-titel-err"); if (e) e.hidden = true;
    });

    // ── EAN / meternummer overnemen uit de eenheid (alleen als het veld nog leeg is) ───────────────────
    var meterUrl = form.getAttribute("data-meterdata-url");
    function prefillMeter() {
        if (!unitHidden || !unitHidden.value || !meterUrl) { if (eanHelp) eanHelp.textContent = ""; return; }
        var t = nutsType();
        eanLabel.textContent = t === "2" ? "Watermeter" : "EAN";
        fetch(meterUrl.replace(/\/Units\/0\//, "/Units/" + unitHidden.value + "/"), { credentials: "same-origin" })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (j) {
                if (!j) return;
                var val = t === "0" ? j.eanElek : t === "1" ? j.eanGas : null;
                if (t === "2") { if (meterInput && !meterInput.value && j.watermeter) { meterInput.value = j.watermeter; if (eanHelp) eanHelp.textContent = ""; } return; }
                if (val && eanInput && !eanInput.value) { eanInput.value = val; if (eanHelp) eanHelp.textContent = "overgenomen van " + unitNaam(); }
            }).catch(function () { });
    }

    $$('input[name="NutsType"]', form).forEach(function (r) { r.addEventListener("change", function () { suggestTitel(); prefillMeter(); markDirty(); }); });
    if (unitHidden) unitHidden.addEventListener("change", function () { suggestTitel(); prefillMeter(); markDirty(); });
    if (isNieuw) { suggestTitel(); prefillMeter(); }

    // ── Verwachte offertedatum: klein veld achter de link ─────────────────────────────────────────
    var vBtn = document.getElementById("gl-v2-nuts-verwacht-btn"), vWrap = document.getElementById("gl-v2-nuts-verwacht-wrap"), vHidden = document.getElementById("gl-v2-nuts-verwacht");
    if (vBtn && vWrap) {
        vBtn.addEventListener("click", function () {
            var open = vWrap.hidden; vWrap.hidden = !open; vBtn.setAttribute("aria-expanded", open ? "true" : "false");
            if (open) { var t = document.getElementById("gl-v2-nuts-verwacht_text"); if (t) t.focus(); }
        });
        if (vHidden) vHidden.addEventListener("change", function () {
            vBtn.textContent = vHidden.value ? vHidden.value.split("-").reverse().join("/") : "datum kiezen";
        });
    }

    // ── Status rechts: volgt uit de datums ────────────────────────────────────────────────────────
    var labels = ["Aanvraag verstuurd", "Offerte ontvangen", "Offerte goedgekeurd", "Uitvoering gevraagd", "Uitgevoerd", "Keuring & overdracht"];
    var dateIds = ["gl-v2-nuts-d0", "gl-v2-nuts-d1", "gl-v2-nuts-d2", "gl-v2-nuts-d3", "gl-v2-nuts-d4"];
    var list = document.querySelector("#gl-v2-nuts-steps .gl-v2-steps-list");
    var checkSvg = '<svg viewBox="0 0 16 16" class="gl-v2-steps-check"><path d="M3.4 8.4l3 3 6.2-6.8"></path></svg>';
    function steps() {
        var has = dateIds.map(function (id) { var h = document.getElementById(id); return !!(h && h.value); });
        var current = 0; while (current < 5 && has[current]) current++;
        var allDone = has.every(Boolean) && checklistKlaar;
        // rij-vinkjes in de werkstroom
        $$(".gl-v2-dos-flow-row[data-step]", form).forEach(function (row) {
            var i = +row.getAttribute("data-step");
            if (i > 4) return;
            row.classList.toggle("is-done", has[i]);
            var dot = $(".gl-v2-dos-flow-dot", row);
            dot.innerHTML = has[i] ? '<i class="ph ph-check"></i>' : "<b>" + (i + 1) + "</b>";
        });
        if (!list) return;
        var html = "";
        for (var i = 0; i < 6; i++) {
            var done = i < 5 ? has[i] : allDone;
            var cur = !done && (i === current || (i === 5 && current === 5 && !allDone));
            var st = done ? "is-done" : cur ? "is-current" : "is-todo";
            var dot = done ? checkSvg : '<span class="gl-v2-steps-glyph">' + (i + 1) + "</span>";
            html += '<li><span class="gl-v2-steps-vitem ' + st + '"' + (cur ? ' aria-current="step"' : "") + ">" +
                '<span class="gl-v2-steps-vrail" aria-hidden="true"><span class="gl-v2-steps-dot ' + st + '">' + dot + "</span>" +
                (i < 5 ? '<span class="gl-v2-steps-vline ' + (done ? "is-done" : "") + '"></span>' : "") + "</span>" +
                '<span class="gl-v2-steps-vbody"><span class="gl-v2-steps-vlabel-row"><span class="gl-v2-steps-label">' + labels[i] + "</span></span></span></span></li>";
        }
        list.innerHTML = html;
    }
    dateIds.forEach(function (id) { var h = document.getElementById(id); if (h) h.addEventListener("change", function () { steps(); markDirty(); }); });
    var cancel = document.getElementById("gl-v2-nuts-cancel");
    if (cancel) cancel.addEventListener("change", function () { var c = document.getElementById("gl-v2-nuts-status"); if (c) c.classList.toggle("is-cancelled", cancel.checked); });
    if (cancel && cancel.checked) { var c0 = document.getElementById("gl-v2-nuts-status"); if (c0) c0.classList.add("is-cancelled"); }
    steps();

    // ── Niet-opgeslagen wijzigingen ───────────────────────────────────────────────────────────────
    function markDirty() { if (dirty) dirty.hidden = false; }
    form.addEventListener("input", markDirty);
    form.addEventListener("change", markDirty);

    // ── Opslaan: titel verplicht, bedragen als getal ──────────────────────────────────────────────
    form.addEventListener("submit", function (e) {
        if (!titel || !titel.value.trim()) {
            e.preventDefault();
            titel.closest(".gl-v2-field").classList.add("is-error");
            var err = document.getElementById("gl-v2-nuts-titel-err"); if (err) err.hidden = false;
            titel.focus(); titel.scrollIntoView({ block: "center", behavior: "smooth" });
            return;
        }
        ["gl-v2-nuts-raming", "gl-v2-nuts-def"].forEach(function (id) {
            var i = document.getElementById(id); if (i && i.value) i.value = i.value.replace(/\s|€/g, "").replace(/\.(?=\d{3}(\D|$))/g, "").replace(",", ".");
        });
        window.removeEventListener("beforeunload", warn);
    });
    function warn(e) { if (dirty && !dirty.hidden) { e.preventDefault(); e.returnValue = ""; } }
    window.addEventListener("beforeunload", warn);
})();
