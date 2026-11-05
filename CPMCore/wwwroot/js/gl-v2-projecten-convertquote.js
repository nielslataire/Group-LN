// gl-v2 — de 21c-modal "Omzetten naar wijzigingsopdracht" en de kopie-modal (design-handoff 21c/29b),
// gedeeld door de lijst (ChangeOrdersV2, 20b) en het offertescherm (ChangeOrderDetailV2). Twee kleine API's:
//
//   GlV2ConvertQuote.open({ url })
//     laadt de modal-inhoud (Modals/_ModalConvertQuoteV2) en toont ze. De offerte is op dat moment altijd
//     bewaard: alle bedragen komen van de server, het formulier post naar ChangeOrderConvertV2 en maakt
//     daar een nieuwe wijzigingsopdracht.
//
//   GlV2CopyChangeOrder.open({ url })
//     laadt de kopie-modal (Modals/_ModalCopyChangeOrderV2) — een gewoon formulier naar ChangeOrderCopyV2.
(function () {
    "use strict";

    function $(sel, root) { return (root || document).querySelector(sel); }
    function bsModal(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }

    var PLAN = {
        "laatste-schijf": {
            hint: "het volledige bedrag bij de laatste betalingsschijf",
            conditions: "Het bedrag van deze wijzigingsopdracht wordt gefactureerd bij de laatste betalingsschijf.",
        },
        "voorschot-saldo": {
            hint: "30 % na ondertekening, 70 % bij de laatste betalingsschijf — aanpasbaar op het opmaakscherm",
            conditions: "30 % voorschot wordt gefactureerd na ondertekening, het saldo bij de laatste betalingsschijf.",
        },
        "eigen": {
            hint: "je verdeelt de termijnen zelf op het opmaakscherm",
            conditions: "De facturatie volgt het facturatieplan van deze wijzigingsopdracht.",
        },
    };

    // ── 21c — Omzetten ──────────────────────────────────────────────────────────────────────────────
    function wireConvert(body) {
        var data = $("#gl-v2-cq-data", body);
        var subtitleEl = document.getElementById("gl-v2-cq-subtitle");
        if (subtitleEl && data) subtitleEl.textContent = data.getAttribute("data-subtitle") || "";

        var canConvert = !!data && data.getAttribute("data-can-convert") === "true";
        ["gl-v2-cq-open", "gl-v2-cq-send"].forEach(function (id) {
            var btn = document.getElementById(id);
            if (btn) btn.disabled = !canConvert;
        });

        var planHint = $("#gl-v2-cq-plan-hint", body);
        var conditions = $("#gl-v2-cq-conditions", body);
        var description = $("#gl-v2-cq-description", body);
        // De voorwaarden volgen het gekozen plan zolang de gebruiker ze zelf niet aanpaste (of ze leeg zijn).
        var conditionsTouched = !!(conditions && conditions.value.trim() !== "");

        function refresh() {
            var checked = $('input[name="convertPlan"]:checked', body);
            var plan = PLAN[checked ? checked.value : "laatste-schijf"];
            if (planHint && plan) planHint.textContent = plan.hint;
            if (conditions && plan && !conditionsTouched) conditions.value = plan.conditions;
        }

        if (conditions) conditions.addEventListener("input", function () { conditionsTouched = true; });
        body.querySelectorAll('input[name="convertPlan"]').forEach(function (radio) { radio.addEventListener("change", refresh); });
        refresh();
        if (description && !description.value) description.focus();
    }

    function openConvert(opts) {
        var m = bsModal("gl-v2-cq-modal");
        var body = document.getElementById("gl-v2-cq-body");
        if (!m || !body) return;
        body.innerHTML = '<div class="gl-v2-cq-loading">Laden…</div>';
        ["gl-v2-cq-open", "gl-v2-cq-send"].forEach(function (id) {
            var btn = document.getElementById(id);
            if (btn) btn.disabled = true;
        });
        m.show();
        fetch(opts.url)
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) {
                body.innerHTML = html;
                wireConvert(body);
            })
            .catch(function () { body.innerHTML = '<div class="gl-v2-cq-loading">Kon het formulier niet laden.</div>'; });
    }

    window.GlV2ConvertQuote = { open: openConvert };

    // ── Kopie van een wijzigingsopdracht ────────────────────────────────────────────────────────────
    function openCopy(opts) {
        var m = bsModal("gl-v2-cc-modal");
        var body = document.getElementById("gl-v2-cc-body");
        if (!m || !body) return;
        body.innerHTML = '<div class="gl-v2-cq-loading">Laden…</div>';
        m.show();
        fetch(opts.url)
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) {
                body.innerHTML = html;
                if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(body); // AJAX-inhoud: keuzelijsten zelf bedraden
                // Soort van de kopie: een offerte kopieer je enkel als offerte (omzetten gaat via "Omzetten →");
                // een wijzigingsopdracht kan als offerte of als wijzigingsopdracht, standaard dezelfde soort.
                var source = $("#gl-v2-cc-order", body);
                function chosen(hiddenInput) { // gekozen optieknop van de gedeelde keuzelijst
                    var wrap = hiddenInput ? hiddenInput.closest(".gl-v2-select") : null;
                    return wrap && hiddenInput.value ? wrap.querySelector(".gl-v2-select-option.is-selected") : null;
                }
                var asQuote = $("#gl-v2-cc-as-quote", body);
                var asWo = $("#gl-v2-cc-as-wo", body);
                function applySource() {
                    var opt = chosen(source);
                    var isQuote = !!opt && opt.getAttribute("data-quote") === "true";
                    if (!asQuote || !asWo) return;
                    asWo.disabled = isQuote;
                    if (isQuote) asQuote.checked = true;
                    else if (opt) asWo.checked = true;
                }
                if (source) { source.addEventListener("change", applySource); applySource(); }
                var client = $("#gl-v2-cc-client", body);
                var hint = $("#gl-v2-cc-client-hint", body);
                if (client && hint) {
                    client.addEventListener("change", function () {
                        var opt = chosen(client);
                        hint.textContent = opt ? "btw klant: " + opt.getAttribute("data-vat-label") : "btw klant volgt de betalingsgroep van de gekozen eenheid";
                    });
                }
                var form = body.closest("form");
                if (form) form.addEventListener("submit", function (e) { // verplichte keuzes (de verborgen input van de keuzelijst kent geen "required")
                    var empty = ["gl-v2-cc-order", "gl-v2-cc-client"].filter(function (id) { var h = document.getElementById(id); return h && !h.value; })[0];
                    if (!empty) return;
                    e.preventDefault();
                    var trig = document.getElementById(empty + "_trigger"); if (trig) trig.focus();
                    if (window.GlV2Toast) window.GlV2Toast.show({ tone: "warning", title: "Nog niet compleet", body: "Kies een offerte of wijzigingsopdracht en een eenheid." });
                });
            })
            .catch(function () { body.innerHTML = '<div class="gl-v2-cq-loading">Kon het formulier niet laden.</div>'; });
    }

    window.GlV2CopyChangeOrder = { open: openCopy };
})();
