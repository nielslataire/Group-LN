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
    var search = document.getElementById("search-term");
    var selectedStatus = "open"; // "open" (nog werk aan) of "all"

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
        var onlyOpen = selectedStatus === "open";

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
        if (typeof updateFilterChips === "function") updateFilterChips();
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
    // ── Zoekveld ─────────────────────────────────────────────────────────────────────────────────
    if (search) {
        search.addEventListener("input", applyFilters);
        var clearSearch = document.getElementById("search-term-clear");
        if (clearSearch) clearSearch.addEventListener("click", function () { search.value = ""; applyFilters(); });
    }

    // ── Status-select (.gl-v2-select, zelfde component/recept als gl-v2-projecten-detailclients.js) ──
    function initSelect(prefix, onChange) {
        var trigger = document.getElementById(prefix + "-trigger");
        var panel = document.getElementById(prefix + "-panel");
        var backdrop = document.getElementById(prefix + "-backdrop");
        var label = document.getElementById(prefix + "-label");
        var closeBtn = document.getElementById(prefix + "-close");
        var valueInput = document.getElementById(prefix + "-value");
        if (!trigger || !panel) return { select: function () {} };

        function closeAll() {
            $$(".gl-v2-select-panel.is-open").forEach(function (p) { p.classList.remove("is-open"); });
            $$(".gl-v2-select-backdrop.is-open").forEach(function (b) { b.classList.remove("is-open"); });
            $$(".gl-v2-select-trigger[aria-expanded='true']").forEach(function (t) { t.setAttribute("aria-expanded", "false"); });
        }
        function selectValue(value, fire) {
            var option = panel.querySelector('.gl-v2-select-option[data-value="' + value + '"]');
            $$(".gl-v2-select-option", panel).forEach(function (o) { o.classList.toggle("is-selected", o === option); });
            if (valueInput) valueInput.value = value;
            if (label && option) label.textContent = option.getAttribute("data-label");
            if (fire !== false && typeof onChange === "function") onChange(value);
        }
        trigger.addEventListener("click", function () {
            var willOpen = !panel.classList.contains("is-open");
            closeAll();
            if (willOpen) {
                panel.classList.add("is-open");
                if (backdrop) backdrop.classList.add("is-open");
                trigger.setAttribute("aria-expanded", "true");
            }
        });
        if (backdrop) backdrop.addEventListener("click", closeAll);
        if (closeBtn) closeBtn.addEventListener("click", closeAll);
        $$(".gl-v2-select-option", panel).forEach(function (option) {
            option.addEventListener("click", function () { selectValue(option.getAttribute("data-value"), true); closeAll(); });
        });
        document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeAll(); });
        return { select: selectValue };
    }

    // "Open" is de standaard en telt niet als actief filter; enkel een afwijking ("Alles") toont een chip
    // en een tellertje.
    var statusSelect = initSelect("status-select", function (value) {
        selectedStatus = value || "open";
        applyFilters();
    });

    function updateFilterChips() {
        var chips = [];
        if (selectedStatus !== "open") chips.push({ type: "status", label: "Alles, ook afgerond" });
        var chipList = document.getElementById("filters-chip-list");
        if (chipList) {
            chipList.innerHTML = "";
            chips.forEach(function (chip) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-filters-chip";
                btn.setAttribute("data-filter-type", chip.type);
                btn.innerHTML = '<span></span><i class="ph ph-x" aria-hidden="true"></i>';
                btn.querySelector("span").textContent = chip.label;
                chipList.appendChild(btn);
            });
        }
        var count = chips.length;
        var countEl = document.getElementById("filters-toggle-count");
        if (countEl) { countEl.textContent = count; countEl.hidden = count === 0; }
        var toggle = document.getElementById("filters-toggle");
        if (toggle) toggle.classList.toggle("has-active", count > 0);
        var clearBtn = document.getElementById("filters-clear");
        if (clearBtn) clearBtn.hidden = count === 0;
        var sheetBadge = document.getElementById("filters-sheet-badge");
        if (sheetBadge) { sheetBadge.textContent = count + " actief"; sheetBadge.hidden = count === 0; }
        var showCount = document.getElementById("filters-show-count");
        if (showCount) showCount.textContent = $$(".gl-v2-co2-row").filter(function (r) { return !r.hidden; }).length;
    }

    document.addEventListener("click", function (e) {
        var chip = e.target.closest(".gl-v2-filters-chip");
        if (chip && chip.getAttribute("data-filter-type") === "status") { statusSelect.select("open"); return; }
        if (e.target.closest(".gl-v2-filters-clear")) {
            if (search) search.value = "";
            statusSelect.select("open", false);
            selectedStatus = "open";
            applyFilters();
        }
    });

    // ── Filters-paneel open/dicht (desktop: inklapbaar onder de zoekbalk; <768px: bottom sheet) ──────
    var filtersToggle = document.getElementById("filters-toggle");
    var filtersPanel = document.getElementById("filters-panel");
    var filtersPanelBackdrop = document.getElementById("filters-panel-backdrop");
    function setFiltersPanelOpen(isOpen) {
        if (!filtersPanel) return;
        filtersPanel.hidden = !isOpen;
        if (filtersPanelBackdrop) filtersPanelBackdrop.hidden = !isOpen;
        if (filtersToggle) { filtersToggle.classList.toggle("is-open", isOpen); filtersToggle.setAttribute("aria-expanded", isOpen.toString()); }
    }
    if (filtersToggle) filtersToggle.addEventListener("click", function () { setFiltersPanelOpen(filtersPanel.hidden); });
    if (filtersPanelBackdrop) filtersPanelBackdrop.addEventListener("click", function () { setFiltersPanelOpen(false); });
    var filtersShowBtn = document.getElementById("filters-show-btn");
    if (filtersShowBtn) filtersShowBtn.addEventListener("click", function () { setFiltersPanelOpen(false); });

    applyFilters();
})();
