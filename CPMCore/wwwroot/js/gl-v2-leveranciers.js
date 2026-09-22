// gl-v2 layout-pilot — Leveranciers pagina-specifieke JS. Enkel geladen door
// Views/Leveranciers/IndexV2.cshtml. Zelfde DataTable/filter-logica als het bestaande inline script
// in Views/Leveranciers/Index.cshtml (issuer/status/activiteiten-filter, zoekveld, colvis) — enkel
// verplaatst naar een los bestand en uitgebreid met gl-v2's eigen laadstaat/lege-staat/rij-···-menu/
// mobiele-kaart-gedrag (zelfde patroon als gl-v2-invoices.js voor Facturen). De verwijder-modal is
// hier ook gl-v2's Type 1-bevestigingscomponent (Bootstrap-modal) i.p.v. Index.cshtml's magnific-
// popup/.modal-block. Server-bepaalde waarden komen binnen via window.glV2LeveranciersConfig.
(function () {
    "use strict";
    var config = window.glV2LeveranciersConfig || {};
    var entity = { singular: "leverancier", plural: "leveranciers" };

    // ── Verwijder-modal (optie 4j TYPE 1, danger) — zelfde recept als gl-v2-invoices.js. ──────────
    var deleteModalElement = document.getElementById("deleteSupplierConfirmModal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    $(document).on("click", ".deleteSupplier", function (ev) {
        ev.preventDefault();
        var id = $(this).data("id");
        $("#delete-supplier-container").html('<div class="p-3 text-muted">Laden…</div>');
        $.get(config.deleteUrl, { id: id })
            .done(function (html) { $("#delete-supplier-container").html(html); })
            .fail(function () { $("#delete-supplier-container").html('<div class="p-3 text-danger">Kon leverancier niet laden.</div>'); });
        if (deleteModal) deleteModal.show();
    });

    // ── Lege staat (optie 4f) — client-side variant (wél leveranciers, nul na filteren/zoeken).
    // Bouwt zowel DataTable's eigen zeroRecords-rij (desktop/tablet) als de mobiele lege kaart
    // (renderMobileCards() hieronder) op uit dezelfde HTML — geen id's hierin, dat zou op mobiel
    // (waar de tabel se eigen — onzichtbare — zeroRecords-rij tegelijk in de DOM blijft staan) tot
    // dubbele id's leiden. ──────────────────────────────────────────────────────────────────────
    function buildEmptyStateHtml() {
        var actions = '<button type="button" class="gl-v2-btn gl-v2-btn-secondary js-gl-v2-clear-filters">Filters wissen</button>';
        if (config.canCreate && config.createUrl) {
            actions += '<a href="' + config.createUrl + '" class="gl-v2-btn gl-v2-btn-primary">+ Nieuwe ' + entity.singular + '</a>';
        }
        return '<div class="gl-v2-empty-state gl-v2-empty-state-inline">' +
            '<span class="gl-v2-empty-state-icon"><i class="ph ph-hard-hat" aria-hidden="true"></i></span>' +
            '<h2 class="gl-v2-empty-state-title">Geen ' + entity.plural + ' gevonden</h2>' +
            '<p class="gl-v2-empty-state-desc">Er zijn geen ' + entity.plural + ' die aan deze filters voldoen. Pas de filters aan of maak een nieuwe ' + entity.singular + '.</p>' +
            '<div class="gl-v2-empty-state-actions">' + actions + '</div>' +
        '</div>';
    }

    function buildLanguage(e) {
        var capSing = e.singular.charAt(0).toUpperCase() + e.singular.slice(1);
        var capPlur = e.plural.charAt(0).toUpperCase() + e.plural.slice(1);
        return {
            processing: "Bezig met verwerken…",
            search: "Zoeken:",
            lengthMenu: "Toon _MENU_",
            info: capSing + " _START_ tot _END_ van _TOTAL_ " + capPlur,
            infoEmpty: capSing + " 0 tot 0 van 0 " + capPlur,
            infoFiltered: "(gefilterd uit _MAX_ totaal)",
            loadingRecords: "Bezig met laden…",
            zeroRecords: buildEmptyStateHtml(),
            emptyTable: "Er zijn geen " + e.plural + " om weer te geven",
            paginate: { previous: "‹", next: "›" },
            aria: {
                sortAscending: ": activeer om oplopend te sorteren",
                sortDescending: ": activeer om aflopend te sorteren"
            }
        };
    }

    // ── Filters (issuer/status/activiteiten) — zelfde $.fn.dataTable.ext.search-logica als het
    // bestaande inline script in Index.cshtml, ongewijzigd overgenomen. ────────────────────────────
    var selectedActivityIds = [];
    var selectedIssuerId = document.getElementById("issuer-select-value")?.value || "";
    var selectedStatus = document.getElementById("status-select-value")?.value || "active";
    var suppliersTableElement = document.getElementById("datatable-suppliers");

    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        if (settings.nTable !== suppliersTableElement) return true;

        var rowElement = settings.aoData[dataIndex]?.nTr;

        if (selectedIssuerId) {
            var rowIssuers = (rowElement?.dataset.issuers || "").toString().split(",").filter(Boolean);
            if (!rowIssuers.includes(selectedIssuerId)) return false;
        }

        var rowIsActive = (rowElement?.dataset.isActive || "").toLowerCase() === "true";
        if (selectedStatus === "active" && !rowIsActive) return false;
        if (selectedStatus === "inactive" && rowIsActive) return false;

        if (!selectedActivityIds.length) return true;
        var rowActivities = (rowElement?.dataset.activities || "").toString().split(",").filter(Boolean);
        if (!rowActivities.length) return false;
        return selectedActivityIds.every(function (id) { return rowActivities.includes(id); });
    });

    // ── Filterbadges (optie 4f) — één badge per actief, niet-standaard filter (Facturatiebedrijf/
    // Status/elke gekozen Activiteit), elk met een eigen × om enkel dat filter te wissen. "Wissen"
    // (rechts in dezelfde rij) reset alles ineens. Status="active" is de standaard-selectie (net
    // als voorheen al het geval was), dus telt niet mee als "actief filter" — enkel afwijken daarvan
    // (Alle/Enkel inactief) krijgt een badge. ───────────────────────────────────────────────────────
    function issuerOptionLabel(value) {
        var opt = document.querySelector('#issuer-select-panel .gl-v2-select-option[data-value="' + value + '"]');
        return opt ? opt.getAttribute("data-label") : value;
    }
    function statusOptionLabel(value) {
        var opt = document.querySelector('#status-select-panel .gl-v2-select-option[data-value="' + value + '"]');
        return opt ? opt.getAttribute("data-label") : value;
    }
    function activityOptionLabel(id) {
        var opt = document.querySelector('#activity-filter option[value="' + id + '"]');
        return opt ? opt.textContent : id;
    }

    function updateFilterChips() {
        var chips = [];
        if (selectedIssuerId) chips.push({ type: "issuer", label: issuerOptionLabel(selectedIssuerId) });
        if (selectedStatus !== "active") chips.push({ type: "status", label: statusOptionLabel(selectedStatus) });
        selectedActivityIds.forEach(function (id) {
            chips.push({ type: "activity", value: id, label: activityOptionLabel(id) });
        });

        // Volledig legen/hervullen van de EIGEN, dedicated lijst-wrapper (niet .gl-v2-filters-chips
        // zelf, die ook "Wissen" draagt) — geen .find()+.remove()+.insertBefore() meer die op een
        // specifieke DOM-volgorde/selector-match moest vertrouwen.
        var $chipList = $("#filters-chip-list").empty();
        chips.forEach(function (chip) {
            var $chip = $('<button type="button" class="gl-v2-filters-chip"><span></span><i class="ph ph-x" aria-hidden="true"></i></button>')
                .attr("data-filter-type", chip.type);
            if (chip.value !== undefined) $chip.attr("data-filter-value", chip.value);
            $chip.find("span").text(chip.label);
            $chipList.append($chip);
        });

        $("#filters-toggle-count, #mobile-filters-toggle-count").text(chips.length).prop("hidden", chips.length === 0);
        $("#filters-toggle, #mobile-filters-toggle").toggleClass("has-active", chips.length > 0);
        $("#filters-clear").prop("hidden", chips.length === 0);
        // Sheet-header (optie 4f, <768px) — "N actief", zelfde telling als de twee toggle-badges
        // hierboven, enkel andere tekst (die twee staan naast een icoon/"Filters"-label dat de
        // context al geeft; deze badge zit in de sheet zelf, "actief" maakt 'm ook zonder buurtekst
        // duidelijk).
        $("#filters-sheet-badge").text(chips.length + " actief").prop("hidden", chips.length === 0);
    }

    $(document).on("click", ".gl-v2-filters-chip", function () {
        var type = $(this).data("filter-type");
        if (type === "issuer") {
            issuerSelectApi.select("");
        } else if (type === "status") {
            statusSelectApi.select("active");
        } else if (type === "activity") {
            var value = String($(this).data("filter-value"));
            var current = $("#activity-filter").val() || [];
            $("#activity-filter").val(current.filter(function (v) { return v !== value; })).trigger("change");
        }
    });

    function resetFilters() {
        $("#search-term").val("");
        table.search("");
        // fireOnChange:false op de twee eerste — hun onChange zou zelf ook al een table.draw()/
        // updateFilterChips() doen, maar we willen hier maar ÉÉN gegarandeerde redraw aan het einde
        // i.p.v. te vertrouwen op de activity-reset se "change"-event om die laatste te leveren (dat
        // bleek niet altijd te vuren, bv. wanneer er geen activiteiten geselecteerd stonden — dan was
        // "Wissen" voor issuer/status stil blijven staan terwijl een los badge-×'je wél werkte, want
        // dat roept altijd zijn eigen onChange synchroon aan).
        issuerSelectApi.select("", false);
        selectedIssuerId = "";
        var addButton = document.getElementById("add-supplier-button");
        if (addButton) {
            addButton.classList.toggle("d-none", !config.canCreate);
            addButton.href = config.createUrl;
        }
        statusSelectApi.select("active", false);
        selectedStatus = "active";
        selectedActivityIds = [];
        $("#activity-filter").val(null).trigger("change");
        table.draw();
        updateFilterChips();
    }
    $(document).on("click", ".js-gl-v2-clear-filters, .gl-v2-filters-clear", resetFilters);

    // ── Filters-knop (optie 4f) — klapt het filtervelden-/badges-blok open/dicht. Twee triggers
    // sturen hetzelfde paneel aan: de inline "Filters"-knop in de toolbar (alle breedtes) en de
    // topbar-filtericoon (enkel <768px, @section MobileTopbarAction in Home/IndexV2.cshtml resp.
    // hier Leveranciers/IndexV2.cshtml) — op mobiel wordt #filters-panel via CSS een bottom sheet
    // i.p.v. inline te blijven staan, vandaar ook de backdrop (enkel daar zichtbaar). ─────────────
    var filtersToggle = document.getElementById("filters-toggle");
    var mobileFiltersToggle = document.getElementById("mobile-filters-toggle");
    var filtersPanel = document.getElementById("filters-panel");
    var filtersPanelBackdrop = document.getElementById("filters-panel-backdrop");
    var filtersToggleButtons = [filtersToggle, mobileFiltersToggle].filter(Boolean);

    function setFiltersPanelOpen(isOpen) {
        filtersPanel.hidden = !isOpen;
        if (filtersPanelBackdrop) filtersPanelBackdrop.hidden = !isOpen;
        filtersToggleButtons.forEach(function (btn) {
            btn.classList.toggle("is-open", isOpen);
            btn.setAttribute("aria-expanded", isOpen.toString());
        });
    }

    if (filtersPanel && filtersToggleButtons.length) {
        filtersToggleButtons.forEach(function (btn) {
            btn.addEventListener("click", function () { setFiltersPanelOpen(filtersPanel.hidden); });
        });
        if (filtersPanelBackdrop) filtersPanelBackdrop.addEventListener("click", function () { setFiltersPanelOpen(false); });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !filtersPanel.hidden) setFiltersPanelOpen(false);
        });
    }

    // ── Mobiele kaarten (optie 4c) — zelfde architectuur als gl-v2-invoices.js' renderMobileCards():
    // #gl-v2-mobile-supplier-list is een lege container, gevuld telkens de tabel opnieuw tekent, dus
    // automatisch in sync met filter/zoek/sorteer/pagineer-staat. Geen proxy-checkbox nodig (geen
    // bulkselectie op deze pagina), enkel een GEKLOONDE kopie van het bestaande "···"-paneel. ──────
    var $mobileList = $("#gl-v2-mobile-supplier-list");

    function buildMobileCard(tr) {
        var $tr = $(tr);
        var $cells = $tr.children("td");
        var name = $.trim($cells.eq(0).text());
        var enterpriseNr = $.trim($cells.eq(1).text());
        var gsm = $.trim($cells.eq(2).text());
        var emailHtml = $cells.eq(3).html();
        var contractCount = $.trim($cells.eq(4).text());
        var contractTotal = $.trim($cells.eq(5).text());
        var isActive = ($tr.attr("data-is-active") || "").toLowerCase() === "true";

        var supplierId = $tr.data("id");
        var $srcMenu = $tr.find(".gl-v2-row-menu");
        var mobileMenuId = "gl-v2-row-menu-m-" + supplierId;
        var $menuClone = $srcMenu.clone().attr("id", mobileMenuId);
        $menuClone.prepend($('<div class="gl-v2-row-menu-title"></div>').text(name));

        var $card = $(
            '<div class="gl-v2-supplier-card">' +
                '<div class="gl-v2-sc-row">' +
                    '<span class="gl-v2-sc-name"></span>' +
                    '<button type="button" class="gl-v2-row-menu-trigger" aria-haspopup="true" aria-expanded="false" aria-label="Meer acties"><i class="ph ph-dots-three" aria-hidden="true"></i></button>' +
                '</div>' +
                '<span class="gl-v2-sc-meta"></span>' +
                '<div class="gl-v2-sc-row gl-v2-sc-contact">' +
                    '<span class="gl-v2-sc-contact-item gl-v2-sc-email"><i class="ph ph-envelope-simple" aria-hidden="true"></i><span></span></span>' +
                    '<span class="gl-v2-sc-contact-item gl-v2-sc-gsm"><i class="ph ph-phone" aria-hidden="true"></i><span></span></span>' +
                '</div>' +
                '<div class="gl-v2-sc-row gl-v2-sc-stats">' +
                    '<span class="gl-v2-sc-stat"><span class="gl-v2-sc-stat-label">Contracten</span><span class="gl-v2-sc-stat-value gl-v2-sc-count"></span></span>' +
                    '<span class="gl-v2-sc-stat"><span class="gl-v2-sc-stat-label">Bedrag</span><span class="gl-v2-sc-stat-value gl-v2-sc-total"></span></span>' +
                '</div>' +
            '</div>'
        );

        $card.find(".gl-v2-sc-name").text(name + (isActive ? "" : " (inactief)"));
        $card.find(".gl-v2-sc-meta").text(enterpriseNr || "–");
        $card.find(".gl-v2-sc-email span").html(emailHtml || "–");
        $card.find(".gl-v2-sc-gsm span").text(gsm || "–");
        if (!$.trim($card.find(".gl-v2-sc-email").text())) $card.find(".gl-v2-sc-email").hide();
        if (gsm === "" || gsm === "–") $card.find(".gl-v2-sc-gsm").hide();
        $card.find(".gl-v2-sc-count").text(contractCount);
        $card.find(".gl-v2-sc-total").text(contractTotal);
        $card.find(".gl-v2-row-menu-trigger").attr("aria-controls", mobileMenuId);
        $card.append($menuClone);

        return $card;
    }

    function renderMobileCards() {
        if (window.innerWidth >= 768 || !$mobileList.length) return;
        $mobileList.empty();
        // Optie 4f "LEGE STAAT" — zonder deze check bouwt dit gewoon een (kapotte) kaart uit
        // DataTables' eigen "geen resultaten"-rij (één <td> met de lege-staat-HTML erin i.p.v. de
        // 7 echte kolommen die buildMobileCard() verwacht) — dezelfde nette lege staat als
        // desktop/tablet dus ook hier tonen i.p.v. die ene rij te proberen "vertalen" naar een kaart.
        if (table.rows({ search: "applied" }).count() === 0) {
            // .gl-v2-table-card wordt op mobiel bewust transparant (elke leverancierskaart draagt
            // al haar eigen wit/schaduw) — de lege staat heeft daardoor hier, anders dan op
            // desktop/tablet, zelf een kaart nodig i.p.v. op die van de tabel te kunnen leunen.
            $mobileList.append($('<div class="gl-v2-supplier-card gl-v2-mobile-empty-card"></div>').html(buildEmptyStateHtml()));
            return;
        }
        $("#datatable-suppliers").children("tbody").children("tr").each(function () {
            $mobileList.append(buildMobileCard(this));
        });
    }

    var table = new DataTable("#datatable-suppliers", {
        order: [[0, "asc"]],
        autoWidth: false,
        language: buildLanguage(entity),
        stateSave: true,
        stateLoadParams: function (settings, data) { data.search.search = ""; },
        // Bedrijfsnaam/Ondernemingsnummer/Contracten blijven compact, GSM/Email/Bedrag krijgen
        // bewust meer ruimte (op verzoek) — die twee eerste zijn kort/vast van vorm, GSM/Email/
        // bedrag hebben elk meer variabele/langere inhoud nodig om niet af te knippen.
        columnDefs: [
            { targets: -1, orderable: false, searchable: false, width: "3%" },
            { targets: 0, width: "20%" },
            { targets: 1, width: "14%" },
            { targets: 2, width: "18%" },
            { targets: 3, width: "20%" },
            { targets: 4, width: "10%" },
            { targets: 5, width: "15%" }
        ],
        // Geen colvis-knop (topStart's default "buttons" feature krijgt gewoon geen knoppen i.p.v.
        // de default pageLength-dropdown te tonen — zelfde aanpak als Invoices' IndexV2) en geen
        // ingebouwde DataTables-zoekbalk (dubbel met het eigen #search-term-veld hierboven) — die
        // laatste wordt hieronder via jQuery verborgen (.dt-search), zelfde patroon als
        // gl-v2-invoices.js.
        layout: { topStart: { buttons: [] } },
        infoCallback: function (settings, start, end, max, total) {
            if (total === 0) return "0 " + entity.plural;
            var shown = end - start + 1;
            return shown + " van " + total + " " + (total === 1 ? entity.singular : entity.plural);
        }
    });
    table.on("draw", renderMobileCards);
    $(".dt-search").hide();

    // ── "Toon N leveranciers" (optie 4f "MOBIEL — FILTERSHEET") — enkel de knop zelf leeft in
    // gl-v2-leveranciers.css (<768px, onder de badges-rij); het cijfer telt hier live mee met elke
    // table.draw() (filteren/zoeken/pagineren maakt geen verschil, dit is het GEFILTERDE totaal
    // over alle pagina's, niet enkel de zichtbare rijen). Sluiten via #filters-show-btn is puur
    // dat: filters staan al toegepast, er is niets om nog te "bevestigen".
    var filtersShowCount = document.getElementById("filters-show-count");
    function updateFiltersShowButton() {
        if (filtersShowCount) filtersShowCount.textContent = table.rows({ search: "applied" }).count();
    }
    table.on("draw", updateFiltersShowButton);
    updateFiltersShowButton();
    var filtersShowBtn = document.getElementById("filters-show-btn");
    if (filtersShowBtn) filtersShowBtn.addEventListener("click", function () { setFiltersPanelOpen(false); });

    var MOBILE_PAGE_LENGTH = 15;

    function syncTablePageLength() {
        if (window.innerWidth < 768) {
            if (table.page.len() !== MOBILE_PAGE_LENGTH) table.page.len(MOBILE_PAGE_LENGTH).draw(false);
            return;
        }

        var appEl = document.querySelector(".gl-v2-app");
        var topbarEl = document.querySelector(".gl-v2-topbar");
        var contentEl = document.querySelector(".gl-v2-content");
        var toolbarEl = document.querySelector(".gl-v2-toolbar-card");
        var theadEl = document.querySelector("#datatable-suppliers thead");
        var dtContainer = document.querySelector("#datatable-suppliers")?.closest(".dt-container");
        if (!appEl || !topbarEl || !contentEl || !theadEl || !dtContainer) return;

        var ROW_HEIGHT = 54; // moet in sync blijven met gl-v2-leveranciers.css tbody td { height }

        var footerRow = dtContainer.querySelector(".dt-layout-row:last-child");
        var footerHeight = footerRow ? footerRow.offsetHeight : 48;
        var appStyle = getComputedStyle(appEl);
        var appVerticalPadding = (parseFloat(appStyle.paddingTop) || 0) + (parseFloat(appStyle.paddingBottom) || 0);
        var contentStyle = getComputedStyle(contentEl);
        var contentVerticalPadding = (parseFloat(contentStyle.paddingTop) || 0) + (parseFloat(contentStyle.paddingBottom) || 0);
        var contentGap = parseFloat(contentStyle.rowGap) || 16;
        var toolbarHeight = toolbarEl ? toolbarEl.offsetHeight : 0;

        var available = window.innerHeight
            - appVerticalPadding
            - topbarEl.offsetHeight
            - contentVerticalPadding
            - toolbarHeight
            - contentGap
            - theadEl.offsetHeight
            - footerHeight;
        var maxRows = Math.max(Math.floor(available / ROW_HEIGHT), 3);
        var recordsTotal = table.page.info().recordsTotal;
        var rows = recordsTotal ? Math.min(maxRows, Math.max(recordsTotal, 3)) : maxRows;

        if (rows !== table.page.len()) table.page.len(rows).draw(false);
    }

    syncTablePageLength();
    renderMobileCards();
    $(".gl-v2-table-card").addClass("is-ready");

    var pageLengthResizeTimer = null;
    $(window).on("resize", function () {
        window.clearTimeout(pageLengthResizeTimer);
        pageLengthResizeTimer = window.setTimeout(function () {
            syncTablePageLength();
            renderMobileCards();
        }, 150);
    });

    $("#search-term").val("");
    $("#search-term").on("input", function () {
        table.search($(this).val()).draw();
    });
    $("#search-term-clear").on("click", function () {
        $("#search-term").val("").trigger("focus");
        table.search("").draw();
    });

    $("#activity-filter").select2({
        theme: "bootstrap",
        placeholder: "Alle activiteiten",
        allowClear: true,
        closeOnSelect: false,
        width: "100%",
        language: "nl"
    }).on("change", function () {
        selectedActivityIds = $(this).val() || [];
        table.draw();
        updateFilterChips();
    });

    // ── Issuer/status-filters — zelfde BASIS-paneelvariant (optie 4h) als het boekjaarfilter op
    // Facturen: eigen trigger+paneel i.p.v. een gestyled <select>. Twee onafhankelijke instanties
    // (dezelfde markup/gedrag), dus generiek geïnitialiseerd i.p.v. tweemaal gekopieerd. ───────────
    function initSelect(prefix, onChange) {
        var trigger = document.getElementById(prefix + "-trigger");
        var panel = document.getElementById(prefix + "-panel");
        var backdrop = document.getElementById(prefix + "-backdrop");
        var label = document.getElementById(prefix + "-label");
        var close = document.getElementById(prefix + "-close");
        var valueEl = document.getElementById(prefix + "-value");
        if (!trigger || !panel) return;

        function closeSelect() {
            panel.classList.remove("is-open");
            if (backdrop) backdrop.classList.remove("is-open");
            trigger.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
        }
        function positionPanel() {
            if (window.innerWidth < 768) return;
            var rect = trigger.getBoundingClientRect();
            var panelWidth = panel.offsetWidth || 240;
            var left = Math.min(rect.left, window.innerWidth - panelWidth - 12);
            panel.style.left = Math.max(12, left) + "px";
            var top = rect.bottom + 6;
            var maxTop = window.innerHeight - panel.offsetHeight - 12;
            panel.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        }
        function openSelect() {
            panel.classList.add("is-open");
            if (backdrop) backdrop.classList.add("is-open");
            trigger.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            positionPanel();
        }
        // Gedeeld tussen een echte klik op een optie EN een programmatische selectie (badge-×,
        // "Wissen") — anders zou je die tweede weg de trigger-UI (label/is-filled/aria) telkens
        // apart moeten bijwerken i.p.v. één keer hier.
        function selectValue(value, fireOnChange) {
            var option = panel.querySelector('.gl-v2-select-option[data-value="' + value + '"]') || panel.querySelector('.gl-v2-select-option[data-value=""]');
            if (!option) return;
            var lbl = option.getAttribute("data-label") || "";
            panel.querySelectorAll(".gl-v2-select-option").forEach(function (o) { o.classList.toggle("is-selected", o === option); });
            if (label) label.textContent = lbl;
            trigger.classList.toggle("is-filled", value !== "");
            if (valueEl) valueEl.value = value;
            if (fireOnChange !== false) onChange(value);
        }
        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            if (panel.classList.contains("is-open")) closeSelect(); else openSelect();
        });
        if (backdrop) backdrop.addEventListener("click", closeSelect);
        if (close) close.addEventListener("click", closeSelect);
        panel.querySelectorAll(".gl-v2-select-option").forEach(function (option) {
            option.addEventListener("click", function () {
                selectValue(option.getAttribute("data-value") || "");
                closeSelect();
            });
        });
        document.addEventListener("click", function (e) {
            if (!panel.classList.contains("is-open")) return;
            if (e.target.closest("#" + prefix)) return;
            closeSelect();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) closeSelect();
        });
        window.addEventListener("resize", function () {
            if (panel.classList.contains("is-open")) positionPanel();
        });

        return { select: selectValue };
    }

    var issuerSelectApi = initSelect("issuer-select", function (value) {
        selectedIssuerId = value || "";
        table.draw();
        updateFilterChips();
        var addButton = document.getElementById("add-supplier-button");
        if (addButton) {
            var canCreate = !selectedIssuerId || (config.writableIssuerIds || []).indexOf(Number(selectedIssuerId)) !== -1;
            addButton.classList.toggle("d-none", !canCreate);
            addButton.href = selectedIssuerId ? config.createUrl + "?issuerCompanyId=" + encodeURIComponent(selectedIssuerId) : config.createUrl;
        }
    });
    var statusSelectApi = initSelect("status-select", function (value) {
        selectedStatus = value || "active";
        table.draw();
        updateFilterChips();
    });

    // Weerspiegelt de server-gerenderde beginstaat (bv. een issuerCompanyId uit de querystring) in
    // de badges-rij vanaf de eerste paint — de drie initSelect(...)-aanroepen hierboven roepen zelf
    // enkel onChange aan bij een ECHTE klik, niet bij init.
    updateFilterChips();

    // ── "···"-rijmenu (tablet/mobiel) — zelfde trigger+JS-gepositioneerd-paneel-patroon als
    // gl-v2-invoices.js/gl-v2-shell.js. ─────────────────────────────────────────────────────────
    var rowMenuBackdrop = document.getElementById("gl-v2-row-menu-backdrop");

    function closeAllRowMenus() {
        $(".gl-v2-row-menu.is-open").each(function () {
            $(this).removeClass("is-open");
            $('.gl-v2-row-menu-trigger[aria-controls="' + this.id + '"]').removeClass("is-menu-open").attr("aria-expanded", "false");
        });
        if (rowMenuBackdrop) rowMenuBackdrop.classList.remove("is-open");
    }

    function positionRowMenu(trigger, menu) {
        if (window.innerWidth < 768) return;
        var rect = trigger.getBoundingClientRect();
        var menuWidth = menu.offsetWidth || 200;
        var left = Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 12);
        menu.style.left = Math.max(12, left) + "px";
        var top = rect.bottom + 6;
        var maxTop = window.innerHeight - menu.offsetHeight - 12;
        menu.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
    }

    $(document).on("click", ".gl-v2-row-menu-trigger", function (e) {
        e.preventDefault();
        e.stopPropagation();
        var trigger = this;
        var menu = document.getElementById(trigger.getAttribute("aria-controls"));
        if (!menu) return;
        var wasOpen = menu.classList.contains("is-open");
        closeAllRowMenus();
        if (wasOpen) return;
        menu.classList.add("is-open");
        trigger.classList.add("is-menu-open");
        trigger.setAttribute("aria-expanded", "true");
        if (rowMenuBackdrop) rowMenuBackdrop.classList.add("is-open");
        positionRowMenu(trigger, menu);
    });
    $(document).on("click", ".gl-v2-row-menu .gl-v2-row-action", closeAllRowMenus);
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-row-menu") || e.target.closest(".gl-v2-row-menu-trigger")) return;
        closeAllRowMenus();
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closeAllRowMenus();
    });
})();
