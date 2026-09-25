// gl-v2 layout-pilot — Projecten/DetailUnitsV2 (design-handoff punt 16a/16d, koppeldialoog 16c).
//
// Anders dan gl-v2-projecten-detailclients.js/gl-v2-leveranciers.js staat hier GEEN DataTable onder:
// de lijst is een boom (gekoppelde eenheden onder hun hoofdeenheid) met groepskoppen en een
// totaalregel, en een plugin die rijen los van elkaar sorteert/pagineert zou die relatie stil breken.
// Zoeken, de chips en het in-/uitklappen van een groep zijn dus eigen filter-JS over de rijen —
// hetzelfde recept als de Eenheden-tabel op Projecten/DetailV2 (#gl-v2-pd-units-filter), hier
// uitgebreid met de chips en met de boomregel hieronder.
//
// Boomregel bij filteren: een gekoppelde eenheid is zichtbaar wanneer ze zélf aan de filters voldoet;
// een hoofdeenheid is zichtbaar wanneer ze zélf voldoet óf wanneer een van haar gekoppelde eenheden
// zichtbaar blijft — anders zou filteren op "Bergingen" ingesprongen rijen tonen zonder de eenheid
// waaraan ze hangen, precies de verwarring die 16a §1 wegneemt.
//
// Niet hier herbouwd: het ⋯-rijmenu (.js-gl-v2-menu-trigger) is de generieke shell-component
// (initContextMenus in gl-v2-shell.js).
(function () {
    "use strict";

    var config = window.glV2DetailUnitsConfig || {};

    // Een rij-actie zit in het ⋯-menu (.gl-v2-menu, z-index 99997) — dat paneel staat ónder de
    // modal-backdrop (100000), dus zonder dit blijft er een gedimd, half zichtbaar menu achter de
    // dialoog staan. De shell stelt closeAll() daar zelf voor open.
    function closeRowMenus() {
        if (window.GlV2Menu && window.GlV2Menu.closeAll) window.GlV2Menu.closeAll();
    }

    // ── Filteren ─────────────────────────────────────────────────────────────────────────────────
    var searchInput = document.getElementById("gl-v2-du-search");
    var searchClear = document.getElementById("gl-v2-du-search-clear");
    var noResults = document.getElementById("gl-v2-du-no-results");
    var rows = Array.prototype.slice.call(document.querySelectorAll("[data-unit-row]"));
    var cards = Array.prototype.slice.call(document.querySelectorAll("[data-unit-card]"));
    var attachRows = Array.prototype.slice.call(document.querySelectorAll("[data-attach-row]"));
    var groupRows = Array.prototype.slice.call(document.querySelectorAll("[data-group-row]"));
    var cardGroupLabels = Array.prototype.slice.call(document.querySelectorAll("[data-card-group-label]"));

    var activeType = "";
    var activeStatus = "";
    var collapsedGroups = Object.create(null);

    function matches(el) {
        var term = (searchInput && searchInput.value ? searchInput.value : "").trim().toLowerCase();
        if (term && (el.getAttribute("data-search") || "").indexOf(term) === -1) return false;
        if (activeType && el.getAttribute("data-type") !== activeType) return false;
        if (activeStatus && el.getAttribute("data-status") !== activeStatus) return false;
        return true;
    }

    /** Bepaalt per rij-id of die zichtbaar blijft, met de boomregel uit de kop van dit bestand. */
    function computeVisibility(items) {
        var self = Object.create(null);
        var childVisible = Object.create(null);
        items.forEach(function (el) {
            var id = el.getAttribute("data-row-id");
            var parentId = el.getAttribute("data-parent-id");
            var ok = matches(el);
            self[id] = ok;
            if (parentId && ok) childVisible[parentId] = true;
        });
        var visible = Object.create(null);
        items.forEach(function (el) {
            var id = el.getAttribute("data-row-id");
            var parentId = el.getAttribute("data-parent-id");
            visible[id] = parentId ? self[id] : (self[id] || !!childVisible[id]);
        });
        return visible;
    }

    function applyFilters() {
        var rowVisible = computeVisibility(rows);
        var cardVisible = cards.length ? computeVisibility(cards) : rowVisible;
        var shownPerGroup = Object.create(null);

        rows.forEach(function (tr) {
            var group = tr.getAttribute("data-group");
            var show = rowVisible[tr.getAttribute("data-row-id")];
            if (show) shownPerGroup[group] = (shownPerGroup[group] || 0) + 1;
            tr.hidden = !show || !!collapsedGroups[group];
        });

        // De koppelregel hoort bij haar hoofdeenheid: verdwijnt mee wanneer die wegvalt of de groep
        // dicht is.
        attachRows.forEach(function (tr) {
            var group = tr.getAttribute("data-group");
            tr.hidden = !rowVisible[tr.getAttribute("data-parent-id")] || !!collapsedGroups[group];
        });

        groupRows.forEach(function (tr) {
            tr.hidden = !shownPerGroup[tr.getAttribute("data-group")];
        });

        cards.forEach(function (card) {
            var group = card.getAttribute("data-group");
            card.hidden = !cardVisible[card.getAttribute("data-row-id")] || !!collapsedGroups[group];
        });
        cardGroupLabels.forEach(function (label) {
            label.hidden = !shownPerGroup[label.getAttribute("data-card-group-label")];
        });

        var total = 0;
        Object.keys(shownPerGroup).forEach(function (k) { total += shownPerGroup[k]; });
        // De wisknop zelf verschijnt/verdwijnt via de shell-CSS (:placeholder-shown op het veld) —
        // hier niets te doen, en een hidden-attribuut zou daar toch van verliezen.
        if (noResults) noResults.hidden = total > 0;
    }

    if (searchInput) searchInput.addEventListener("input", applyFilters);
    if (searchClear) {
        searchClear.addEventListener("click", function () {
            searchInput.value = "";
            applyFilters();
            searchInput.focus();
        });
    }

    // Chips: per groep (type/status) één actieve keuze; opnieuw klikken zet de filter uit — zelfde
    // gedrag als de aandacht-chips op Projecten/DetailV2.
    document.addEventListener("click", function (e) {
        var chip = e.target.closest(".gl-v2-du-chip");
        if (!chip) return;
        var kind = chip.getAttribute("data-chip-kind");
        var value = chip.getAttribute("data-chip-value") || "";
        if (kind === "type") activeType = activeType === value ? "" : value;
        else if (kind === "status") activeStatus = activeStatus === value ? "" : value;
        else return;

        document.querySelectorAll('.gl-v2-du-chip[data-chip-kind="' + kind + '"]').forEach(function (c) {
            var v = c.getAttribute("data-chip-value") || "";
            var current = kind === "type" ? activeType : activeStatus;
            c.classList.toggle("is-active", v === current);
        });
        applyFilters();
    });

    // Groep in-/uitklappen.
    document.addEventListener("click", function (e) {
        var toggle = e.target.closest("[data-group-toggle]");
        if (!toggle) return;
        var key = toggle.getAttribute("data-group-toggle");
        collapsedGroups[key] = !collapsedGroups[key];
        toggle.setAttribute("aria-expanded", collapsedGroups[key] ? "false" : "true");
        applyFilters();
    });

    // Exporteren (16a §5) heeft hier GEEN eigen JS: beide menu-items zijn gewone links naar een echte
    // server-side actie — ExportUnitsExcel (.xlsx via ClosedXML) en PrintUnitList (opgemaakte PDF via
    // UnitListDocument/QuestPDF, zelfde huisstijlbasis als de klantenlijst). De @media print-regels in
    // gl-v2-projecten-detailunits.css blijven bestaan voor wie gewoon Ctrl+P gebruikt.

    // ── Koppeldialoog (16c) — fragment in een lege modal-shell, zelfde AJAX-recept als de
    //    verwijdermodal op Projecten/DetailClientsV2. ───────────────────────────────────────────────
    var attachModalElement = document.getElementById("gl-v2-du-attach-modal");
    var attachModal = attachModalElement && window.bootstrap
        ? new bootstrap.Modal(attachModalElement, { backdrop: "static", keyboard: false })
        : null;

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-du-attach");
        if (!trigger) return;
        e.preventDefault();
        closeRowMenus();
        var unitId = trigger.getAttribute("data-unit-id");
        var container = document.getElementById("gl-v2-du-attach-container");
        container.innerHTML = '<div class="modal-body"><div class="gl-v2-du-picker-empty">Laden…</div></div>';
        if (attachModal) attachModal.show();
        fetch(config.attachModalUrl + "?unitid=" + encodeURIComponent(unitId))
            .then(function (r) { return r.text(); })
            .then(function (html) { container.innerHTML = html; initAttachForm(container); })
            .catch(function () {
                container.innerHTML = '<div class="modal-body"><div class="gl-v2-du-picker-empty">De koppelgegevens konden niet geladen worden.</div></div>';
            });
    });

    /** Bedrag tonen zoals de rest van de pagina: nl-BE, geen centen (de lijst toont ook hele euro's). */
    function formatEuro(value) {
        try {
            return value.toLocaleString("nl-BE", { style: "currency", currency: "EUR", maximumFractionDigits: 0 });
        } catch (err) {
            return "€ " + Math.round(value);
        }
    }

    /** 16c: eerst wát je koppelt, dan rekenen — de keuze vult bouwwaarde/grondwaarde en de nieuwe
     *  prijs van het lot, i.p.v. te beginnen met twee lege bedragvelden. */
    function initAttachForm(container) {
        var form = container.querySelector("form[data-attach-form]");
        if (!form) return;

        var unitInput = form.querySelector("[data-attach-unit-id]");
        var landInput = form.querySelector("[data-attach-land-value]");
        var buildValue = form.querySelector("[data-attach-build-value]");
        var resultNew = form.querySelector("[data-attach-result-new]");
        var resultOld = form.querySelector("[data-attach-result-old]");
        var submitBtn = form.querySelector("[data-attach-submit]");
        var lotPrice = parseFloat(form.getAttribute("data-lot-price") || "0") || 0;

        function select(option) {
            form.querySelectorAll(".gl-v2-du-picker-option").forEach(function (o) { o.classList.remove("is-selected"); });
            option.classList.add("is-selected");
            var price = parseFloat(option.getAttribute("data-price") || "0") || 0;
            if (unitInput) unitInput.value = option.getAttribute("data-unit-id");
            // data-land-value staat al in nl-BE-notatie ("1.234,56"): dit is de waarde die de gebruiker
            // te zien krijgt én die terug gepost wordt, en ParseAmountV2 in de controller leest beide
            // notaties. data-price/data-build-value blijven invariant — die zijn enkel om te rekenen.
            if (landInput) landInput.value = option.getAttribute("data-land-value") || "0,00";
            if (buildValue) buildValue.textContent = formatEuro(parseFloat(option.getAttribute("data-build-value") || "0") || 0);
            if (resultOld) resultOld.hidden = false;
            if (resultNew) resultNew.textContent = formatEuro(lotPrice + price);
            if (submitBtn) submitBtn.disabled = false;
        }

        form.querySelectorAll(".gl-v2-du-picker-option:not(.is-disabled)").forEach(function (option) {
            option.addEventListener("click", function () { select(option); });
        });

        // Eén kiesbare eenheid: die staat meteen goed, zodat je enkel nog hoeft te bevestigen.
        var only = form.querySelectorAll(".gl-v2-du-picker-option:not(.is-disabled)");
        if (only.length === 1) select(only[0]);
    }

    // ── Ontkoppelen en verwijderen ────────────────────────────────────────────────────────────────
    // Ontkoppelen is een POST (het wijzigt data) en gaat via het verborgen formulier met
    // antiforgery-token in de view — een GET-link zou door een prefetch of een linkchecker
    // uitgevoerd kunnen worden.
    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-du-detach");
        if (!trigger) return;
        e.preventDefault();
        closeRowMenus();
        var form = document.getElementById("gl-v2-du-detach-form");
        if (!form) return;
        form.querySelector("[name='unitId']").value = trigger.getAttribute("data-unit-id");
        form.submit();
    });

    // Verwijderen: één modal in de pagina die per rij zijn tekst en doel-URL krijgt, i.p.v. een
    // aparte modal per eenheid (of een AJAX-fragment voor een bevestiging zonder eigen inhoud).
    var deleteModalElement = document.getElementById("gl-v2-du-delete-modal");
    var deleteModal = deleteModalElement && window.bootstrap
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-du-delete");
        if (!trigger) return;
        e.preventDefault();
        closeRowMenus();
        var name = document.getElementById("gl-v2-du-delete-name");
        var confirmLink = document.getElementById("gl-v2-du-delete-confirm");
        if (name) name.textContent = trigger.getAttribute("data-unit-name") || "";
        if (confirmLink) confirmLink.setAttribute("href", trigger.getAttribute("data-delete-url") || "#");
        if (deleteModal) deleteModal.show();
    });

    applyFilters();
})();
