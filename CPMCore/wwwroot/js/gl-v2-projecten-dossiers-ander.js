// gl-v2 — "Ander dossier"-modal (design-handoff 31f), gedeeld door Dossiers/IndexV2 ("+ Nieuw → Ander dossier") en Dossiers/DetailsV2
// ("Bewerken" van een niet-nuts dossier). Inhoud via AJAX uit ProjectDossiersController.AnderModal; posten naar .Opslaan.
(function () {
    "use strict";

    var cfgEl = document.getElementById("gl-v2-dos-config");
    var cfg = cfgEl ? JSON.parse(cfgEl.textContent) : {};
    var modalEl = document.getElementById("gl-v2-dos-ander-modal");
    if (!modalEl || !cfg.anderUrl) return;

    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }
    function modal() { return window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(modalEl) : null; }

    function open(opts) {
        opts = opts || {};
        var m = modal(), body = document.getElementById("gl-v2-dos-ander-body");
        if (!m || !body) return;
        body.innerHTML = '<div class="gl-v2-tr-loading">Laden…</div>';
        $("#gl-v2-dos-ander-title").textContent = opts.id ? "Dossier bewerken" : "Ander dossier";
        m.show();
        var qs = "?projectId=" + encodeURIComponent(cfg.projectId);
        if (opts.id) qs += "&id=" + encodeURIComponent(opts.id);
        if (opts.kind != null) qs += "&kind=" + encodeURIComponent(opts.kind);
        fetch(cfg.anderUrl.split("?")[0] + qs, { credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) { body.innerHTML = html; wire(body); })
            .catch(function () { body.innerHTML = '<div class="gl-v2-tr-loading">Kon het formulier niet laden.</div>'; });
    }
    window.GlV2DosAnder = { open: open };

    function setSelect(id, value) {
        var wrap = document.getElementById(id + "_select");
        var opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + value + '"]');
        if (opt) opt.click();
    }

    function wire(body) {
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(body);
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        var root = $("#gl-v2-dos-ander-root", body);
        if (!root) return;
        $("#gl-v2-dos-ander-title").textContent = root.getAttribute("data-title");
        $("#gl-v2-dos-ander-sub").textContent = root.getAttribute("data-sub");

        // Type-tegels; "Andere…" neemt de waarde van de keuzelijst eronder over.
        var andere = document.getElementById("gl-v2-dos-f-andere");
        var andereWrap = document.getElementById("gl-v2-dos-f-andere-wrap");
        var andereHidden = document.getElementById("gl-v2-dos-f-andere-kind");
        function currentKind() { var r = $$('input[name="DossierKind"]', body).filter(function (x) { return x.checked; })[0]; return r ? r.value : ""; }
        function applyKind() {
            var isAndere = andere && andere.checked;
            if (andereWrap) andereWrap.hidden = !isAndere;
            if (isAndere && andereHidden && andereHidden.value) andere.value = andereHidden.value;
            hint();
        }
        $$('input[name="DossierKind"]', body).forEach(function (r) { r.addEventListener("change", applyKind); });
        if (andereHidden) andereHidden.addEventListener("change", applyKind);

        // Verwachte afhandeling: bij een omgevingsvergunning 120 dagen na de aanvraag voorstellen (wettelijke beslistermijn-richtwaarde).
        var aanvraag = document.getElementById("gl-v2-dos-f-aanvraag");
        var verwacht = document.getElementById("gl-v2-dos-f-verwacht");
        var verwachtHelp = document.getElementById("gl-v2-dos-f-verwacht-help");
        function hint() {
            if (!verwachtHelp) return;
            verwachtHelp.textContent = currentKind() === "2" ? "+ 120 dagen na de aanvraagdatum" : "";
        }
        function suggest() {
            if (currentKind() !== "2" || !aanvraag || !aanvraag.value || !verwacht || verwacht.value) return;
            var p = aanvraag.value.split("-"), d = new Date(+p[0], +p[1] - 1, +p[2] + 120);
            var iso = d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2);
            verwacht.value = iso;
            var t = document.getElementById("gl-v2-dos-f-verwacht_text"); if (t) t.value = iso.split("-").reverse().join("/");
        }
        if (aanvraag) aanvraag.addEventListener("change", suggest);
        applyKind();

        // Koppeling met een mijlpaal
        var toggle = document.getElementById("gl-v2-dos-f-koppel-toggle");
        var pick = document.getElementById("gl-v2-dos-f-koppel-pick");
        var koppelHidden = document.getElementById("gl-v2-dos-f-koppel");
        function applyKoppel() {
            if (!toggle) return;
            if (pick) pick.hidden = !toggle.checked;
            if (koppelHidden) koppelHidden.disabled = !toggle.checked;
            if (toggle.checked && koppelHidden && !koppelHidden.value && root.getAttribute("data-voorstel-mijlpaal")) setSelect("gl-v2-dos-f-koppel", root.getAttribute("data-voorstel-mijlpaal"));
        }
        if (toggle) { toggle.addEventListener("change", applyKoppel); applyKoppel(); }

        var titel = document.getElementById("gl-v2-dos-f-titel");
        if (titel) titel.addEventListener("input", function () {
            titel.closest(".gl-v2-field").classList.remove("is-error");
            var e = $('.gl-v2-tr-err[data-for="Titel"]', body); if (e) e.hidden = true;
        });
        if (titel && !titel.value) setTimeout(function () { titel.focus(); }, 250);
    }

    var form = document.getElementById("gl-v2-dos-ander-form");
    if (form) form.addEventListener("submit", function (e) {
        var titel = document.getElementById("gl-v2-dos-f-titel");
        if (!titel || !titel.value.trim()) {
            e.preventDefault();
            if (titel) {
                titel.closest(".gl-v2-field").classList.add("is-error");
                var err = $('.gl-v2-tr-err[data-for="Titel"]', form); if (err) err.hidden = false;
                titel.focus();
            }
        }
    });
    // bedrag: 1.234,56 → 1234.56 zodat de server het als decimaal leest
    if (form) form.addEventListener("submit", function () {
        var b = document.getElementById("gl-v2-dos-f-bedrag");
        if (b && b.value) b.value = b.value.replace(/\./g, "").replace(",", ".");
    });
})();
