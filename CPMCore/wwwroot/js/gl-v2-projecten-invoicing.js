// gl-v2 — Projecten/InvoicingV2 (design-handoff 20a, "Facturatie"). Vijf gedragingen, in volgorde
// van de pagina:
//   1. Tabs (Te factureren/Gefactureerd) — zelfde mechanisme als gl-v2-projecten-detailcoordinatie.js:
//      client-side tonen/verbergen, geen AJAX per tab.
//   2. Filterpillen (Alles/Schijven/Meer- en minwerken) binnen "Te factureren" — client-side op
//      data-kind, geen round-trip.
//   3. Selectie + sticky selectiebalk over alle accounts/kaarten heen — inclusief de eindafrekening-
//      vergrendeling (Niels, 2026-09-30): de structureel laatste schijf van een groep (data-last-stage)
//      trekt bij het aanvinken alle nog openstaande meerwerken/minwerken van hetzelfde account verplicht
//      mee (geforceerd aangevinkt + disabled) en wordt geweigerd zodra dat account een niet-ondertekende
//      WO heeft (elders in de kaart blijft zo'n WO gewoon zichtbaar, niet-blokkerend — enkel de laatste
//      schijf zelf wordt geblokkeerd). "Facturen opmaken" → AJAX-voorstel (PreviewInvoices) in een modal,
//      bevestigen roept de bestaande MakeInvoices/MakeInvoicesCO aan (allebei als de selectie beide
//      types bevat) en herlaadt.
//   4. Scherm 21b: 4 opties. Herinneren/Weigering registreren zijn per-ondertekenaar acties die niet
//      met enkel een caseId kunnen — die openen SigningAdmin/Dossier waar dat al per rij werkt (Part A
//      van dit bouwplan). Getekende versie opladen/Annuleren zijn dossier-brede acties en gebeuren hier
//      rechtstreeks.
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-inv-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function fmtNum(v, dec) { return v.toLocaleString("nl-BE", { minimumFractionDigits: dec, maximumFractionDigits: dec }); }
    function fmtEur(v) { return "€ " + fmtNum(v, 2); }
    function num(v) { var n = parseFloat(String(v == null ? "" : v).replace(",", ".")); return isNaN(n) ? 0 : n; }
    function token() { var el = $('input[name="__RequestVerificationToken"]'); return el ? el.value : ""; }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body }); }
    function bsModal(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }

    // ── 1. Tabs ─────────────────────────────────────────────────────────────────────────────────────
    var tabs = $$("[data-inv-tab]");
    var panels = $$("[data-inv-panel]");
    function showTab(name) {
        if (!tabs.some(function (t) { return t.getAttribute("data-inv-tab") === name; })) name = "open";
        tabs.forEach(function (t) {
            var on = t.getAttribute("data-inv-tab") === name;
            t.classList.toggle("is-active", on);
            t.setAttribute("aria-selected", on ? "true" : "false");
        });
        panels.forEach(function (p) { p.hidden = p.getAttribute("data-inv-panel") !== name; });
    }
    tabs.forEach(function (t) {
        t.addEventListener("click", function () {
            var name = t.getAttribute("data-inv-tab");
            showTab(name);
            if (window.history && window.history.replaceState) window.history.replaceState(null, "", "#" + name);
        });
    });
    showTab((window.location.hash || "").replace("#", "") || "open");

    // ── 2. Filterpillen ─────────────────────────────────────────────────────────────────────────────
    var filterBar = $(".gl-v2-inv-filters");
    if (filterBar) {
        filterBar.addEventListener("click", function (e) {
            var btn = e.target.closest(".gl-v2-inv-filter");
            if (!btn) return;
            $$(".gl-v2-inv-filter", filterBar).forEach(function (b) { b.classList.toggle("is-active", b === btn); });
            var kinds = (btn.getAttribute("data-inv-filter") || "").split(",").filter(Boolean);
            $$("[data-inv-row]").forEach(function (row) {
                row.hidden = kinds.length > 0 && kinds.indexOf(row.getAttribute("data-kind")) === -1;
            });
        });
    }

    // ── 3. Selectie + selectiebalk ──────────────────────────────────────────────────────────────────
    var selbar = document.getElementById("gl-v2-inv-selbar");

    function postBoxes() { return $$(".js-inv-post"); }
    function checkedBoxes() { return postBoxes().filter(function (b) { return b.checked; }); }

    function updateSelection() {
        var checked = checkedBoxes();
        var total = 0;
        checked.forEach(function (b) { total += num(b.getAttribute("data-amount")); });

        postBoxes().forEach(function (b) {
            var tr = b.closest("tr");
            if (tr) tr.classList.toggle("is-selected", b.checked);
        });

        // "Alles"-checkbox per account bijhouden (indeterminate/volledig).
        $$(".js-inv-account-all").forEach(function (all) {
            var card = all.closest(".gl-v2-inv-card");
            var boxes = card ? $$(".js-inv-post", card) : [];
            var checkedInCard = boxes.filter(function (b) { return b.checked; });
            all.checked = boxes.length > 0 && checkedInCard.length === boxes.length;
            all.indeterminate = checkedInCard.length > 0 && checkedInCard.length < boxes.length;
        });

        if (!selbar) return;
        selbar.hidden = checked.length === 0;
        var part = $("[data-inv-sel-count]", selbar);
        if (part) part.innerHTML = "<b>" + checked.length + "</b> " + (checked.length === 1 ? "post" : "posten");
        var totalEl = $("[data-inv-sel-total]", selbar);
        if (totalEl) totalEl.textContent = fmtEur(total);
    }

    // Eindafrekening (Niels, 2026-09-30): de structureel laatste schijf van een eenheid trekt alle
    // andere, nog open schijven van diezelfde eenheid EN alle nog niet gefactureerde meerwerken/
    // minwerken van hetzelfde account verplicht mee, en wordt geweigerd zodra dat account een niet-
    // ondertekende WO heeft — elders in de kaart blijft zo'n WO gewoon (niet-blokkerend) zichtbaar, de
    // blokkade geldt enkel voor het aanvinken van de laatste schijf zelf.
    function applyLastStageLock(card) {
        var lastBoxes = $$('.js-inv-stage[data-last-stage="true"]', card);
        if (lastBoxes.some(function (b) { return b.checked; }) && card.querySelector(".gl-v2-inv-row.is-blocked")) {
            lastBoxes.forEach(function (b) { b.checked = false; });
            // Rechtsonder als toast (Niels, 2026-09-30 — expliciet niet inline/modal voor dit geval).
            toast("danger", "Nog niet mogelijk", "Deze schijf is de eindafrekening en trekt de andere schijven en wijzigingsopdrachten van dit account mee — eerst de niet-ondertekende wijzigingsopdracht hieronder beslissen.");
        }

        // Per eenheid: welke eenheden hebben hun laatste schijf aangevinkt?
        var checkedUnitIds = {};
        lastBoxes.forEach(function (b) { if (b.checked) checkedUnitIds[b.getAttribute("data-unit-id")] = true; });
        var anyLastChecked = Object.keys(checkedUnitIds).length > 0;

        $$(".js-inv-stage", card).forEach(function (b) {
            if (b.getAttribute("data-last-stage") === "true") return; // blijft zelf altijd bedienbaar
            if (checkedUnitIds[b.getAttribute("data-unit-id")]) { b.checked = true; b.disabled = true; }
            else { b.disabled = false; }
        });
        $$(".js-inv-co", card).forEach(function (b) {
            if (anyLastChecked) { b.checked = true; b.disabled = true; }
            else { b.disabled = false; }
        });
        card.classList.toggle("is-eindafrekening", anyLastChecked);
    }

    document.addEventListener("change", function (e) {
        if (e.target.classList.contains("js-inv-post")) {
            var accountCard = e.target.closest(".gl-v2-inv-card");
            if (accountCard) applyLastStageLock(accountCard);
            updateSelection();
        }
        if (e.target.classList.contains("js-inv-account-all")) {
            var card = e.target.closest(".gl-v2-inv-card");
            $$(".js-inv-post", card).forEach(function (b) { if (!b.disabled) b.checked = e.target.checked; });
            applyLastStageLock(card);
            updateSelection();
        }
    });

    var clearBtn = document.getElementById("gl-v2-inv-sel-clear");
    if (clearBtn) {
        clearBtn.addEventListener("click", function () {
            postBoxes().forEach(function (b) { b.checked = false; });
            updateSelection();
        });
    }

    function selectionPayload() {
        var stages = [], changeOrders = [];
        checkedBoxes().forEach(function (b) {
            if (b.classList.contains("js-inv-stage")) {
                stages.push({ ClientAccountId: num(b.getAttribute("data-account-id")), Unitid: num(b.getAttribute("data-unit-id")), StageId: num(b.getAttribute("data-stage-id")), CompanyId: 0 });
            } else if (b.classList.contains("js-inv-co")) {
                changeOrders.push({ ClientAccountId: num(b.getAttribute("data-account-id")), ChangeOrderId: num(b.getAttribute("data-co-id")), ChangeOrderDetailId: num(b.getAttribute("data-detail-id")), Percentage: 100, CompanyId: 0 });
            }
        });
        return { Stages: stages, ChangeOrders: changeOrders };
    }

    var previewGoBtn = document.getElementById("gl-v2-inv-preview-go");
    var previewBody = document.getElementById("gl-v2-inv-preview-body");
    var previewConfirmBtn = document.getElementById("gl-v2-inv-preview-confirm");
    var lastPayload = null;

    if (previewGoBtn) {
        previewGoBtn.addEventListener("click", function () {
            lastPayload = selectionPayload();
            if (lastPayload.Stages.length === 0 && lastPayload.ChangeOrders.length === 0) return;
            var m = bsModal("gl-v2-inv-preview-modal");
            if (m) m.show();
            previewBody.innerHTML = '<div class="gl-v2-inv-preview-empty">Bezig met berekenen…</div>';
            if (previewConfirmBtn) previewConfirmBtn.disabled = true;
            fetch(cfg.previewUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
                body: JSON.stringify(lastPayload)
            })
                .then(function (r) { return r.text(); })
                .then(function (html) { previewBody.innerHTML = html; if (previewConfirmBtn) previewConfirmBtn.disabled = false; })
                .catch(function () {
                    previewBody.innerHTML = '<div class="gl-v2-inv-preview-empty">Het voorstel kon niet berekend worden.</div>';
                });
        });
    }

    if (previewConfirmBtn) {
        previewConfirmBtn.addEventListener("click", function () {
            if (!lastPayload || previewConfirmBtn.disabled) return;
            previewConfirmBtn.disabled = true;
            previewConfirmBtn.classList.add("is-loading");
            var calls = [];
            if (lastPayload.Stages.length > 0) {
                calls.push(fetch(cfg.makeInvoicesUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
                    body: JSON.stringify({ Invoices: lastPayload.Stages })
                }));
            }
            if (lastPayload.ChangeOrders.length > 0) {
                calls.push(fetch(cfg.makeInvoicesCoUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
                    body: JSON.stringify({ Invoices: lastPayload.ChangeOrders })
                }));
            }
            Promise.all(calls)
                .then(function () { window.location.reload(); })
                .catch(function () {
                    toast("error", "Niet gelukt", "De facturen konden niet aangemaakt worden. Probeer opnieuw.");
                    previewConfirmBtn.disabled = false;
                    previewConfirmBtn.classList.remove("is-loading");
                });
        });
    }

    // ── 4. Scherm 21b ────────────────────────────────────────────────────────────────────────────────
    var modal21b = document.getElementById("gl-v2-inv-21b-modal");
    if (modal21b) {
        var descEl = document.getElementById("gl-v2-inv-21b-desc");
        var confirmBtn = document.getElementById("gl-v2-inv-21b-confirm");
        var currentCaseId = null, currentCoId = null;
        var labels = { remind: "Naar dossier", upload: "Opladen en afronden", decline: "Naar dossier", cancel: "Dossier annuleren" };

        function selectedOption() { var el = $('input[name="gl-v2-inv-21b-option"]:checked'); return el ? el.value : "remind"; }
        function refreshExtras() {
            var opt = selectedOption();
            $$("[data-inv-21b-extra]", modal21b).forEach(function (box) { box.hidden = box.getAttribute("data-inv-21b-extra") !== opt; });
            if (confirmBtn) confirmBtn.textContent = labels[opt] || "Bevestigen";
        }
        modal21b.addEventListener("change", function (e) { if (e.target.name === "gl-v2-inv-21b-option") refreshExtras(); });

        $$(".js-inv-21b-open").forEach(function (btn) {
            btn.addEventListener("click", function (e) {
                e.preventDefault();
                currentCaseId = btn.getAttribute("data-case-id");
                currentCoId = btn.getAttribute("data-co-id");
                if (descEl) descEl.textContent = btn.getAttribute("data-description") || "";
                refreshExtras();
                var m = bsModal("gl-v2-inv-21b-modal");
                if (m) m.show();
            });
        });

        if (confirmBtn) {
            confirmBtn.addEventListener("click", function () {
                if (!currentCaseId) return;
                var opt = selectedOption();
                if (opt === "remind" || opt === "decline") {
                    window.location.href = cfg.dossierUrl.replace("__CASEID__", currentCaseId);
                    return;
                }
                confirmBtn.disabled = true;
                confirmBtn.classList.add("is-loading");
                if (opt === "cancel") {
                    var reason = ($("#gl-v2-inv-21b-cancel-reason") || {}).value || "";
                    var body = new URLSearchParams({ caseId: currentCaseId, reason: reason });
                    fetch(cfg.cancelUrl, { method: "POST", headers: { "RequestVerificationToken": token() }, body: body })
                        .then(function () { window.location.reload(); })
                        .catch(function () { toast("error", "Niet gelukt", "Het dossier kon niet geannuleerd worden."); confirmBtn.disabled = false; confirmBtn.classList.remove("is-loading"); });
                } else if (opt === "upload") {
                    var fileInput = $("#gl-v2-inv-21b-file");
                    var file = fileInput && fileInput.files && fileInput.files[0];
                    if (!file) { toast("error", "Kies een bestand", "Kies eerst het ondertekende PDF."); confirmBtn.disabled = false; confirmBtn.classList.remove("is-loading"); return; }
                    var form = new FormData();
                    form.append("caseId", currentCaseId);
                    form.append("file", file);
                    fetch(cfg.uploadSignedUrl, { method: "POST", headers: { "RequestVerificationToken": token() }, body: form })
                        .then(function () { window.location.reload(); })
                        .catch(function () { toast("error", "Niet gelukt", "Het document kon niet opgeladen worden."); confirmBtn.disabled = false; confirmBtn.classList.remove("is-loading"); });
                }
            });
        }
    }

    updateSelection();
})();
