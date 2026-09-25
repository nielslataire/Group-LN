// gl-v2 — Projecten/LandsharesV2 ("Aandelen basisakte verdelen"), de bestemming van de waarschuwing op
// Projecten/DetailUnitsV2 (design-handoff 16a §4).
//
// Drie taken: de som live meerekenen tegen het projecttotaal, één keer proportioneel verdelen op
// oppervlakte, en niet weglopen met niet-opgeslagen wijzigingen. Dat laatste is woordelijk hetzelfde
// recept als gl-v2-projecten-editcontract.js (markDirty/isFormDirty/initDiscardChangesModal) —
// gekopieerd en aangepast, dezelfde per-pagina-kopie-conventie die dat bestand zelf documenteert.
(function () {
    "use strict";

    var form = document.getElementById("gl-v2-ls-form");
    if (!form) return;

    var totalBadge = document.getElementById("gl-v2-ls-total-badge");
    var sumEl = document.getElementById("gl-v2-ls-sum");
    var sumPctEl = document.getElementById("gl-v2-ls-sum-pct");
    var statusEl = document.getElementById("gl-v2-ls-status");
    var statusTextEl = document.getElementById("gl-v2-ls-status-text");
    var rows = Array.prototype.slice.call(form.querySelectorAll("[data-ls-row]"));
    var projectTotal = parseFloat((totalBadge && totalBadge.getAttribute("data-total")) || "0") || 0;

    // Velden staan in nl-BE-notatie op het scherm ("1.234,56") en worden zo ook teruggepost; de
    // controller (ParseAmountV2) leest beide notaties. Hier alleen lezen om te kunnen rekenen.
    function parseAmount(text) {
        if (!text) return null;
        var cleaned = String(text).trim();
        if (!cleaned) return null;
        if (cleaned.indexOf(",") !== -1) cleaned = cleaned.replace(/\./g, "").replace(",", ".");
        var value = parseFloat(cleaned);
        return isNaN(value) ? null : value;
    }

    function formatAmount(value) {
        try {
            return value.toLocaleString("nl-BE", { maximumFractionDigits: 2 });
        } catch (err) {
            return String(value);
        }
    }

    function inputOf(row) { return row.querySelector(".gl-v2-ls-input"); }
    function surfaceOf(row) { return parseFloat(row.getAttribute("data-surface") || "0") || 0; }

    function recalc() {
        var sum = 0;
        rows.forEach(function (row) {
            var value = parseAmount(inputOf(row).value) || 0;
            sum += value;
            var pctEl = row.querySelector(".gl-v2-ls-pct");
            if (pctEl) {
                pctEl.textContent = projectTotal > 0 && value > 0
                    ? formatAmount(Math.round(value / projectTotal * 10000) / 100) + " %"
                    : "—";
            }
        });

        if (sumEl) sumEl.textContent = formatAmount(Math.round(sum * 100) / 100);
        if (sumPctEl) {
            sumPctEl.textContent = projectTotal > 0
                ? formatAmount(Math.round(sum / projectTotal * 10000) / 100) + " %"
                : "—";
        }

        // Tolerantie van een cent: aandelen mogen decimaal zijn, en een som van floats hoeft niet
        // exact te landen om als "klopt" te gelden.
        var diff = Math.round((projectTotal - sum) * 100) / 100;
        var ok = projectTotal > 0 && Math.abs(diff) < 0.005;

        if (sumEl) sumEl.classList.toggle("is-warning", !ok);
        if (totalBadge) {
            totalBadge.textContent = formatAmount(Math.round(sum * 100) / 100) + " / " + formatAmount(projectTotal);
            totalBadge.classList.toggle("is-positive", ok);
            totalBadge.classList.toggle("is-attention", !ok);
            totalBadge.classList.toggle("is-neutral", false);
        }
        if (statusEl) statusEl.classList.toggle("is-error", !ok);
        if (statusTextEl) {
            if (projectTotal <= 0) {
                statusTextEl.textContent = "Dit project heeft nog geen totaal aandeel basisakte — stel dat eerst in bij de projectinstellingen.";
            } else if (ok) {
                statusTextEl.textContent = "De verdeling klopt: " + formatAmount(projectTotal) + " aandelen volledig verdeeld.";
            } else if (diff > 0) {
                statusTextEl.textContent = "Nog " + formatAmount(diff) + " aandelen te verdelen.";
            } else {
                statusTextEl.textContent = formatAmount(Math.abs(diff)) + " aandelen te veel verdeeld.";
            }
        }
    }

    // ── Niet-opgeslagen wijzigingen ───────────────────────────────────────────────────────────────
    function markDirty() {
        var badge = document.getElementById("gl-v2-ls-dirty-badge");
        if (badge) badge.hidden = false;
    }
    function isFormDirty() {
        var badge = document.getElementById("gl-v2-ls-dirty-badge");
        return !!badge && !badge.hidden;
    }

    form.addEventListener("input", function (e) {
        if (e.target.classList.contains("gl-v2-ls-input")) { markDirty(); recalc(); }
    });

    var discardModal = document.getElementById("gl-v2-ls-discard-modal");
    var discardConfirm = document.getElementById("gl-v2-ls-discard-confirm");
    var cancelLink = document.getElementById("gl-v2-ls-cancel-link");
    var backLink = document.getElementById("gl-v2-topbar-back-link");
    if (discardModal && discardConfirm && window.bootstrap) {
        if (cancelLink) discardConfirm.href = cancelLink.href;
        [cancelLink, backLink].filter(Boolean).forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                if (!isFormDirty()) return;
                e.preventDefault();
                discardConfirm.href = trigger.href;
                window.bootstrap.Modal.getOrCreateInstance(discardModal).show();
            });
        });
    }

    // ── Verdelen op oppervlakte ───────────────────────────────────────────────────────────────────
    // Naar rato van de oppervlakte, afgerond op hele aandelen, en de afrondingsrest gaat naar de
    // grootste eenheid — zo komt de som exact op het projecttotaal uit i.p.v. er een paar aandelen
    // naast te blijven staan, wat de hele reden is dat dit scherm bestaat.
    var distributeBtn = document.getElementById("gl-v2-ls-distribute");
    if (distributeBtn) {
        distributeBtn.addEventListener("click", function () {
            var withSurface = rows.filter(function (r) { return surfaceOf(r) > 0; });
            var surfaceSum = withSurface.reduce(function (acc, r) { return acc + surfaceOf(r); }, 0);
            if (surfaceSum <= 0 || projectTotal <= 0) return;

            var assigned = 0;
            var largest = null;
            withSurface.forEach(function (row) {
                var share = Math.round(projectTotal * surfaceOf(row) / surfaceSum);
                inputOf(row).value = formatAmount(share);
                assigned += share;
                if (!largest || surfaceOf(row) > surfaceOf(largest)) largest = row;
            });
            // Eenheden zonder oppervlakte krijgen expliciet 0 in plaats van leeg te blijven: na een
            // verdeling is "nog geen aandeel" geen eerlijke omschrijving meer.
            rows.forEach(function (row) {
                if (surfaceOf(row) <= 0) inputOf(row).value = "0";
            });

            var rest = Math.round(projectTotal - assigned);
            if (rest !== 0 && largest) {
                var corrected = (parseAmount(inputOf(largest).value) || 0) + rest;
                inputOf(largest).value = formatAmount(corrected);
            }

            markDirty();
            recalc();
        });
    }

    var clearBtn = document.getElementById("gl-v2-ls-clear");
    if (clearBtn) {
        clearBtn.addEventListener("click", function () {
            rows.forEach(function (row) { inputOf(row).value = ""; });
            markDirty();
            recalc();
        });
    }

    recalc();
})();
