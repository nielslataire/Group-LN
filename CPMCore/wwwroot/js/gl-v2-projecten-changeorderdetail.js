// gl-v2 — Projecten/ChangeOrderDetailV2 (design-handoff 20d, "Wijzigingsopdracht"). Drie losse stukken:
// 1) Regels-tabel: live herberekening (prijs klant/totaal/samenvatting), zelfde formule als de legacy
//    EditChangeOrder.cshtml (base=aantal*prijs, commissie=base*comm%, btw=(base+commissie)*btw%).
// 2) Facturatieplan: snelkeuzes bouwen de termijnrijen opnieuw op via AddTerm (server rendert de juiste
//    rijstructuur per Kind — Saldo heeft een vergrendeld %-veld, geen los kind-veld om client-side te
//    hertekenen), "+ Tussentijdse termijn" voegt één losse rij toe.
// 3) "Omzetten" — in-place actie (geen JSON, gewoon een POST-redirect, zelfde stijl als
//    gl-v2-projecten-paymentstages.js se submitMarkReached).
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-co-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function token() { var el = $('input[name="__RequestVerificationToken"]'); return el ? el.value : ""; }

    function getNum(el) {
        if (!el) return 0;
        var v = (el.value || "").toString().trim();
        return parseFloat(v.replace(",", ".")) || 0;
    }
    function formatEUR(n) { return n.toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " €"; }

    // ── 1. Regels: herberekening ────────────────────────────────────────────────────────────────────
    var rowsBody = document.getElementById("gl-v2-co-rows-body");

    function recalcRow(tr) {
        var number = getNum($(".js-co-number", tr));
        var price = getNum($(".js-co-price", tr));
        var commission = getNum($(".js-co-commission", tr));
        var priceKlant = price * (1 + commission / 100);
        var total = number * priceKlant;
        var $priceKlant = $(".js-co-priceklant", tr);
        var $total = $(".js-co-total", tr);
        if ($priceKlant) $priceKlant.textContent = formatEUR(priceKlant);
        if ($total) $total.textContent = formatEUR(total);
    }

    function recomputeTotals() {
        if (!rowsBody) return;
        var cost = 0, commissionTotal = 0, vatTotal = 0;
        $$(".js-co-row", rowsBody).forEach(function (tr) {
            var qty = getNum($(".js-co-number", tr));
            var price = getNum($(".js-co-price", tr));
            var commission = getNum($(".js-co-commission", tr));
            var vat = getNum($(".js-co-vat", tr));
            var base = qty * price;
            var commissionAmt = base * (commission / 100);
            var vatAmt = (base + commissionAmt) * (vat / 100);
            cost += base;
            commissionTotal += commissionAmt;
            vatTotal += vatAmt;
        });
        var excl = cost + commissionTotal;
        var grand = excl + vatTotal;
        var set = function (id, v) { var el = document.getElementById(id); if (el) el.textContent = formatEUR(v); };
        set("gl-v2-co-sum-cost", cost);
        set("gl-v2-co-sum-commission", commissionTotal);
        set("gl-v2-co-sum-excl", excl);
        set("gl-v2-co-sum-vat", vatTotal);
        set("gl-v2-co-sum-total", grand);
        recomputeTermAmounts(excl);
    }

    if (rowsBody) {
        rowsBody.addEventListener("input", function (e) {
            var tr = e.target.closest(".js-co-row");
            if (!tr) return;
            recalcRow(tr);
            recomputeTotals();
        });
        rowsBody.addEventListener("click", function (e) {
            var del = e.target.closest(".js-co-delete-row");
            if (!del) return;
            del.closest(".js-co-row").remove();
            recomputeTotals();
        });
        $$(".js-co-row", rowsBody).forEach(recalcRow);
    }

    var addRowBtn = document.getElementById("gl-v2-co-add-row");
    if (addRowBtn) {
        addRowBtn.addEventListener("click", function () {
            var index = $$(".js-co-row", rowsBody).length;
            fetch(cfg.addRowUrl + "?index=" + index + "&vatKlant=" + encodeURIComponent(cfg.vatKlant))
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    rowsBody.insertAdjacentHTML("beforeend", html);
                    var tr = rowsBody.lastElementChild;
                    recalcRow(tr);
                    recomputeTotals();
                });
        });
    }

    // ── 2. Facturatieplan ───────────────────────────────────────────────────────────────────────────
    var termsBody = document.getElementById("gl-v2-co-terms-body");
    var termsTotalPct = document.getElementById("gl-v2-co-terms-total-pct");
    var termsTotalBadge = document.getElementById("gl-v2-co-terms-total-badge");

    function termAmount(pct, excl) { return Math.round(excl * (pct || 0) / 100 * 100) / 100; }

    function recomputeTermAmounts(excl) {
        if (!termsBody) return;
        var totalPct = 0;
        $$(".js-co-term-row", termsBody).forEach(function (tr) {
            var pctInput = $(".js-co-term-pct", tr);
            var pct = getNum(pctInput);
            totalPct += pct;
            var amountCell = $(".js-co-term-amount", tr);
            if (amountCell) amountCell.textContent = formatEUR(termAmount(pct, excl));
        });
        if (termsTotalPct) termsTotalPct.textContent = Math.round(totalPct * 100) / 100 + " %";
        if (termsTotalBadge) {
            var ok = Math.abs(totalPct - 100) < 0.01;
            termsTotalBadge.textContent = ok ? "klopt" : "moet 100 % zijn";
            termsTotalBadge.className = "gl-v2-badge " + (ok ? "is-positive" : "is-attention");
        }
    }

    if (termsBody) {
        termsBody.addEventListener("input", function (e) {
            if (e.target.classList.contains("js-co-term-pct")) recomputeTotals();
        });
        termsBody.addEventListener("change", function (e) {
            if (e.target.classList.contains("js-co-term-trigger")) {
                var tr = e.target.closest(".js-co-term-row");
                var stageSelect = $(".js-co-term-stage", tr);
                if (stageSelect) stageSelect.classList.toggle("is-hidden", e.target.value !== "2");
            }
        });
        termsBody.addEventListener("click", function (e) {
            var del = e.target.closest(".js-co-delete-term");
            if (!del) return;
            del.closest(".js-co-term-row").remove();
            recomputeTotals();
        });
    }

    function addTermRow(kind, percentage) {
        var index = termsBody ? $$(".js-co-term-row", termsBody).length : 0;
        var url = cfg.addTermUrl + "?index=" + index + "&projectId=" + cfg.projectId + "&kind=" + kind +
            (percentage != null ? "&percentage=" + percentage : "");
        return fetch(url).then(function (r) { return r.text(); }).then(function (html) {
            termsBody.insertAdjacentHTML("beforeend", html);
        });
    }

    var addTermBtn = document.getElementById("gl-v2-co-add-term");
    if (addTermBtn) {
        addTermBtn.addEventListener("click", function () {
            addTermRow(2, null).then(recomputeTotals);
        });
    }

    var presetBar = document.getElementById("gl-v2-co-presets");
    if (presetBar) {
        presetBar.addEventListener("click", function (e) {
            var btn = e.target.closest(".gl-v2-co-preset");
            if (!btn) return;
            $$(".gl-v2-co-preset", presetBar).forEach(function (b) { b.classList.toggle("is-active", b === btn); });
            var preset = btn.getAttribute("data-preset");
            if (preset === "eigen") return; // laat de bestaande rijen ongemoeid

            termsBody.innerHTML = "";
            var chain;
            if (preset === "laatste-schijf") {
                chain = addTermRow(3, 100);
            } else if (preset === "voorschot-saldo") {
                chain = addTermRow(1, 30).then(function () { return addTermRow(3, 70); });
            } else if (preset === "na-ondertekening") {
                chain = addTermRow(1, 100).then(function () { return addTermRow(3, 0); });
            }
            if (chain) chain.then(recomputeTotals);
        });
    }

    // ── 3. Omzetten (21c, in-place) ─────────────────────────────────────────────────────────────────
    var convertBtn = document.getElementById("gl-v2-co-convert");
    if (convertBtn) {
        convertBtn.addEventListener("click", function () {
            if (convertBtn.disabled) return;
            var form = document.createElement("form");
            form.method = "post";
            form.action = cfg.convertUrl;
            form.style.display = "none";
            [["__RequestVerificationToken", token()], ["projectId", cfg.projectId], ["changeOrderId", cfg.changeOrderId]]
                .forEach(function (pair) {
                    var input = document.createElement("input");
                    input.type = "hidden"; input.name = pair[0]; input.value = pair[1];
                    form.appendChild(input);
                });
            document.body.appendChild(form);
            form.submit();
        });
    }

    recomputeTotals();
})();
