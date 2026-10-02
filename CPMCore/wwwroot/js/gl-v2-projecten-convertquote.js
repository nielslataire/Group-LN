// gl-v2 — de 21c-modal "Omzetten naar wijzigingsopdracht" en de kopie-modal (design-handoff 21c/29b),
// gedeeld door de lijst (ChangeOrdersV2, 20b) en Offerte inlezen (QuoteIntakeV2, 20c). Twee kleine API's:
//
//   GlV2ConvertQuote.open({ url, cost, subtitle, clientId, description, onConfirm })
//     laadt de modal-inhoud (Modals/_ModalConvertQuoteV2) en toont ze. Zonder onConfirm post het eigen
//     formulier naar ChangeOrderConvertV2 (lijst: de offerte is al opgeslagen). Mét onConfirm (20c) krijgt
//     de pagina de gekozen waarden en verzendt ze haar eigen formulier — opslaan + omzetten in één POST,
//     zodat ook nog niet bewaarde regels meegaan. cost/subtitle overschrijven dan de serverwaarden
//     (op 20c staan de actuele regels in het paginaformulier, niet in de databank).
//
//   GlV2CopyChangeOrder.open({ url, changeOrderId })
//     laadt de kopie-modal (Modals/_ModalCopyChangeOrderV2) — een gewoon formulier naar ChangeOrderCopyV2.
(function () {
    "use strict";

    function $(sel, root) { return (root || document).querySelector(sel); }
    function bsModal(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }
    function num(v) { return parseFloat((v == null ? "" : String(v)).replace(",", ".")) || 0; }
    function formatEUR(n) {
        return (n < 0 ? "− € " : "€ ") + Math.abs(n).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    function formatPct(n) { return n.toLocaleString("nl-BE", { maximumFractionDigits: 2 }); }

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
    var current = null; // de opties van de lopende open()-aanroep

    function wireConvert(body, opts) {
        var data = $("#gl-v2-cq-data", body);
        var cost = opts.cost != null ? opts.cost : num(data && data.getAttribute("data-cost"));
        var subtitle = opts.subtitle || (data && data.getAttribute("data-subtitle")) || "";
        var subtitleEl = document.getElementById("gl-v2-cq-subtitle");
        if (subtitleEl) {
            subtitleEl.textContent = [subtitle, "kostprijs " + formatEUR(cost)]
                .filter(function (s) { return !!s; }).join(" · ");
        }

        var client = $("#gl-v2-cq-client", body);
        var clientHint = $("#gl-v2-cq-client-hint", body);
        var vatLabel = $("#gl-v2-cq-vat-label", body);
        var commission = $("#gl-v2-cq-commission", body);
        var commissionHint = $("#gl-v2-cq-commission-hint", body);
        var planHint = $("#gl-v2-cq-plan-hint", body);
        var conditions = $("#gl-v2-cq-conditions", body);
        var description = $("#gl-v2-cq-description", body);
        var conditionsTouched = false;

        if (opts.clientId && client && !client.value) client.value = String(opts.clientId);
        if (opts.description && description && !description.value) description.value = opts.description;

        // Minwerk wordt aan kostprijs verrekend: commissie vast op 0 %.
        var isMinwerk = cost < 0;
        if (isMinwerk && commission) {
            commission.value = "0";
            commission.readOnly = true;
            if (commissionHint) commissionHint.textContent = "minwerk — aan kostprijs verrekend, commissie vergrendeld op 0 %";
        }

        function selectedPlan() {
            var checked = $('input[name="convertPlan"]:checked', body);
            return checked ? checked.value : "laatste-schijf";
        }

        function refresh() {
            var opt = client && client.selectedIndex > 0 ? client.options[client.selectedIndex] : null;
            var vat = opt ? num(opt.getAttribute("data-vat")) : null;
            if (vatLabel) vatLabel.textContent = opt ? opt.getAttribute("data-vat-label") : "kies eerst een eenheid";
            if (clientHint) clientHint.textContent = opt && opt.getAttribute("data-owners")
                ? opt.getAttribute("data-owners") + " — alle eigenaars tekenen"
                : "de eigenaars van deze eenheid tekenen de wijzigingsopdracht";

            var pct = isMinwerk ? 0 : num(commission && commission.value);
            var commissionAmt = cost * pct / 100;
            var excl = cost + commissionAmt;
            var set = function (id, text) { var el = document.getElementById(id); if (el) el.textContent = text; };
            set("gl-v2-cq-sum-cost", formatEUR(cost));
            set("gl-v2-cq-sum-commission", formatEUR(commissionAmt));
            set("gl-v2-cq-sum-excl", formatEUR(excl));
            set("gl-v2-cq-sum-incl-label", vat != null ? "INCL. " + formatPct(vat) + " % BTW" : "INCL. BTW");
            set("gl-v2-cq-sum-incl", vat != null ? formatEUR(excl * (1 + vat / 100)) : "—");

            var plan = PLAN[selectedPlan()];
            if (planHint && plan) planHint.textContent = plan.hint;
            if (conditions && plan && !conditionsTouched) conditions.value = plan.conditions;
        }

        if (client) client.addEventListener("change", refresh);
        if (commission) commission.addEventListener("input", refresh);
        if (conditions) conditions.addEventListener("input", function () { conditionsTouched = true; });
        body.querySelectorAll('input[name="convertPlan"]').forEach(function (radio) { radio.addEventListener("change", refresh); });
        refresh();
        if (description && !description.value) description.focus();
    }

    function openConvert(opts) {
        var m = bsModal("gl-v2-cq-modal");
        var body = document.getElementById("gl-v2-cq-body");
        if (!m || !body) return;
        current = opts;
        body.innerHTML = '<div class="gl-v2-cq-loading">Laden…</div>';
        var subtitleEl = document.getElementById("gl-v2-cq-subtitle");
        if (subtitleEl) subtitleEl.textContent = opts.subtitle || "";
        m.show();
        fetch(opts.url)
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) {
                body.innerHTML = html;
                wireConvert(body, opts);
            })
            .catch(function () { body.innerHTML = '<div class="gl-v2-cq-loading">Kon het formulier niet laden.</div>'; });
    }

    var convertForm = document.getElementById("gl-v2-cq-form");
    if (convertForm) {
        convertForm.addEventListener("submit", function (e) {
            if (!current || typeof current.onConfirm !== "function") return; // lijst: gewone POST
            e.preventDefault();
            var body = document.getElementById("gl-v2-cq-body");
            var plan = $('input[name="convertPlan"]:checked', body);
            current.onConfirm({
                clientAccountId: ($("#gl-v2-cq-client", body) || {}).value || "",
                commission: ($("#gl-v2-cq-commission", body) || {}).value || "0",
                plan: plan ? plan.value : "laatste-schijf",
                description: ($("#gl-v2-cq-description", body) || {}).value || "",
                conditions: ($("#gl-v2-cq-conditions", body) || {}).value || "",
                afterSave: e.submitter && e.submitter.value === "send" ? "send" : "open",
            });
        });
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
                var client = $("#gl-v2-cc-client", body);
                var hint = $("#gl-v2-cc-client-hint", body);
                if (client && hint) {
                    client.addEventListener("change", function () {
                        var opt = client.selectedIndex > 0 ? client.options[client.selectedIndex] : null;
                        hint.textContent = opt ? "btw klant: " + opt.getAttribute("data-vat-label") : "btw klant volgt de betalingsgroep van de gekozen eenheid";
                    });
                }
            })
            .catch(function () { body.innerHTML = '<div class="gl-v2-cq-loading">Kon het formulier niet laden.</div>'; });
    }

    window.GlV2CopyChangeOrder = { open: openCopy };
})();
