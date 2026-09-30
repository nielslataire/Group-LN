// gl-v2 — Projecten/ChangeOrdersV2 (design-handoff 20b, "Offertes & wijzigingen"). Alles client-side:
// tabs (Alles/Offertes/Wijzigingsopdrachten), zoekveld en status-filter werken samen op dezelfde
// rij-set (data-co2-type/-status/-search), geen round-trip nodig — zelfde patroon als Facturatie's
// filterpillen.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-co2-root");
    if (!root) return;

    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }

    var tabs = document.getElementById("gl-v2-co2-tabs");
    var search = document.getElementById("gl-v2-co2-search");
    var statusFilter = document.getElementById("gl-v2-co2-status");

    var OPEN_STATUSES = ["Offerte", "Opgemaakt", "Verzonden", "Ondertekend", "Factureerbaar"];

    function applyFilters() {
        var type = tabs ? ($(".gl-v2-tabbar-tab.is-active", tabs) || {}).getAttribute && $(".gl-v2-tabbar-tab.is-active", tabs).getAttribute("data-type") : "";
        var term = (search && search.value || "").trim().toLowerCase();
        var onlyOpen = !statusFilter || statusFilter.value === "open";

        $$(".gl-v2-co2-row").forEach(function (row) {
            var matchesType = !type || row.getAttribute("data-co2-type") === type;
            var matchesSearch = !term || (row.getAttribute("data-co2-search") || "").indexOf(term) !== -1;
            var matchesStatus = !onlyOpen || OPEN_STATUSES.indexOf(row.getAttribute("data-co2-status")) !== -1;
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
