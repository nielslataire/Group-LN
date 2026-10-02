// gl-v2 — Projecten/ChangeOrdersV2 (design-handoff 20b/29a, "Offertes & wijzigingen"). Alles client-side:
// tabs (Alles/Offertes/Wijzigingsopdrachten), zoekveld en status-filter werken samen op dezelfde
// rij-set (data-co2-type/-status/-search), geen round-trip nodig — zelfde patroon als Facturatie's
// filterpillen. Daarnaast de ingangen van de flow (29b): "Omzetten →" (21c-modal) en "Kopie maken"
// (kopie-modal) — beide modals komen uit gl-v2-projecten-convertquote.js; het "+ Nieuw"-menu en de
// rij-"···"-menu's zijn het gedeelde contextmenu uit gl-v2-shell.js.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-co2-root");
    if (!root) return;

    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }

    var tabs = document.getElementById("gl-v2-co2-tabs");
    var search = document.getElementById("gl-v2-co2-search");
    var statusFilter = document.getElementById("gl-v2-co2-status");

    // Welke rijen "open" zijn (nog werk aan) bepaalt de server (data-co2-open): een ingetrokken WO is weer
    // een concept, een gefactureerde wacht op betaling, een omgezette of verlopen offerte is afgesloten.

    var configEl = document.getElementById("gl-v2-co2-config");
    var cfg = configEl ? JSON.parse(configEl.textContent) : {};

    // ── Rij openen: de hele rij is klikbaar, behalve de actiekolom en alles wat zelf klikbaar is. ────
    root.addEventListener("click", function (e) {
        if (e.target.closest("a, button, .gl-v2-menu, .gl-v2-co2-actions")) return;
        var row = e.target.closest(".gl-v2-co2-row");
        if (row && row.getAttribute("data-href")) window.location.href = row.getAttribute("data-href");
    });

    // ── 21c "Omzetten →" op een offerte-rij en "Kopie maken" (rij-menu en "+ Nieuw") ────────────────
    document.addEventListener("click", function (e) {
        var convert = e.target.closest(".js-co2-convert");
        if (convert && window.GlV2ConvertQuote) {
            window.GlV2ConvertQuote.open({
                url: cfg.convertModalUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&changeOrderId=" + encodeURIComponent(convert.getAttribute("data-co-id")),
            });
            return;
        }
        var copy = e.target.closest(".js-co2-copy");
        if (copy && !copy.disabled && window.GlV2CopyChangeOrder) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            window.GlV2CopyChangeOrder.open({
                url: cfg.copyModalUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&changeOrderId=" + encodeURIComponent(copy.getAttribute("data-co-id") || 0),
            });
        }
    });

    function applyFilters() {
        var type = tabs ? ($(".gl-v2-tabbar-tab.is-active", tabs) || {}).getAttribute && $(".gl-v2-tabbar-tab.is-active", tabs).getAttribute("data-type") : "";
        var term = (search && search.value || "").trim().toLowerCase();
        var onlyOpen = !statusFilter || statusFilter.value === "open";

        $$(".gl-v2-co2-row").forEach(function (row) {
            var matchesType = !type || row.getAttribute("data-co2-type") === type;
            var matchesSearch = !term || (row.getAttribute("data-co2-search") || "").indexOf(term) !== -1;
            var matchesStatus = !onlyOpen || row.getAttribute("data-co2-open") === "true";
            row.hidden = !(matchesType && matchesSearch && matchesStatus);
        });

        // Secties met enkel verborgen rijen ook verbergen (kop + tabel niet leeg tonen).
        $$("[data-co2-section]").forEach(function (section) {
            var visible = $$(".gl-v2-co2-row", section).some(function (r) { return !r.hidden; });
            section.hidden = !visible;
        });
    }

    if (tabs) {
        tabs.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab) return;
            $$(".gl-v2-tabbar-tab", tabs).forEach(function (t) {
                t.classList.toggle("is-active", t === tab);
                t.setAttribute("aria-selected", t === tab ? "true" : "false");
            });
            applyFilters();
        });
    }
    if (search) search.addEventListener("input", applyFilters);
    if (statusFilter) statusFilter.addEventListener("change", applyFilters);

    applyFilters();
})();
