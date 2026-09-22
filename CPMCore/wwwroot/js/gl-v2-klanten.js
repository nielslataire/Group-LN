// gl-v2 layout-pilot — Klanten pagina-specifieke JS. Enkel geladen door Views/Klanten/IndexV2.cshtml.
// Letterlijke aanpassing van gl-v2-leveranciers.js (op verzoek: "volledig zoals Leveranciers/Index")
// — zelfde laadstaat/lege-staat/filters-sheet/rij-···-menu/mobiele-kaart-architectuur, enkel de
// tabel-id, kolommen en het filterveldenaantal verschillen (Klanten heeft geen Status/Activiteit,
// enkel Facturatiebedrijf). Server-bepaalde waarden komen binnen via window.glV2KlantenConfig.
(function () {
    "use strict";
    var config = window.glV2KlantenConfig || {};
    var entity = { singular: "klant", plural: "klanten" };

    // ── Verwijder-modal (optie 4j TYPE 1, danger) — zelfde recept als gl-v2-leveranciers.js. ───────
    var deleteModalElement = document.getElementById("deleteClientConfirmModal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    $(document).on("click", ".deleteClient", function (ev) {
        ev.preventDefault();
        var id = $(this).data("id");
        $("#delete-client-container").html('<div class="p-3 text-muted">Laden…</div>');
        $.get(config.deleteUrl, { id: id })
            .done(function (html) { $("#delete-client-container").html(html); })
            .fail(function () { $("#delete-client-container").html('<div class="p-3 text-danger">Kon klant niet laden.</div>'); });
        if (deleteModal) deleteModal.show();
    });

    // ── Lege staat (optie 4f) — client-side variant. Geen id's hierin (zie gl-v2-leveranciers.js se
    // toelichting: de tabel se eigen — op mobiel onzichtbare — zeroRecords-rij blijft tegelijk in de
    // DOM staan naast de mobiele kaart-variant, dubbele id's zouden dat botsen). ────────────────────
    function buildEmptyStateHtml() {
        var actions = '<button type="button" class="gl-v2-btn gl-v2-btn-secondary js-gl-v2-clear-filters">Filters wissen</button>';
        if (config.canCreate && config.createUrl) {
            actions += '<a href="' + config.createUrl + '" class="gl-v2-btn gl-v2-btn-primary">+ Nieuwe ' + entity.singular + '</a>';
        }
        return '<div class="gl-v2-empty-state gl-v2-empty-state-inline">' +
            '<span class="gl-v2-empty-state-icon"><i class="ph ph-users" aria-hidden="true"></i></span>' +
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

    // ── Filter (enkel Facturatiebedrijf — Klanten kent geen Status/Activiteit-onderscheid). ────────
    var selectedIssuerId = document.getElementById("issuer-select-value")?.value || "";
    var clientsTableElement = document.getElementById("datatable-clients");

    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        if (settings.nTable !== clientsTableElement) return true;
        if (!selectedIssuerId) return true;
        var rowElement = settings.aoData[dataIndex]?.nTr;
        var rowIssuers = (rowElement?.dataset.issuers || "").toString().split(",").filter(Boolean);
        return rowIssuers.includes(selectedIssuerId);
    });

    // ── Filterbadge (optie 4f) — hooguit één badge (Facturatiebedrijf), met een eigen × om 'm te
    // wissen. "Wissen" ernaast reset hetzelfde (er is hier maar één filter om te wissen). ──────────
    function issuerOptionLabel(value) {
        var opt = document.querySelector('#issuer-select-panel .gl-v2-select-option[data-value="' + value + '"]');
        return opt ? opt.getAttribute("data-label") : value;
    }

    function updateFilterChips() {
        var chips = [];
        if (selectedIssuerId) chips.push({ type: "issuer", label: issuerOptionLabel(selectedIssuerId) });

        var $chipList = $("#filters-chip-list").empty();
        chips.forEach(function (chip) {
            var $chip = $('<button type="button" class="gl-v2-filters-chip"><span></span><i class="ph ph-x" aria-hidden="true"></i></button>')
                .attr("data-filter-type", chip.type);
            $chip.find("span").text(chip.label);
            $chipList.append($chip);
        });

        $("#filters-toggle-count, #mobile-filters-toggle-count").text(chips.length).prop("hidden", chips.length === 0);
        $("#filters-toggle, #mobile-filters-toggle").toggleClass("has-active", chips.length > 0);
        $("#filters-clear").prop("hidden", chips.length === 0);
        $("#filters-sheet-badge").text(chips.length + " actief").prop("hidden", chips.length === 0);
    }

    $(document).on("click", ".gl-v2-filters-chip", function () {
        var type = $(this).data("filter-type");
        if (type === "issuer") issuerSelectApi.select("");
    });

    function resetFilters() {
        $("#search-term").val("");
        table.search("");
        issuerSelectApi.select("", false);
        selectedIssuerId = "";
        var addButton = document.getElementById("add-client-button");
        if (addButton) {
            addButton.classList.toggle("d-none", !config.canCreate);
            addButton.href = config.createUrl;
        }
        table.draw();
        updateFilterChips();
    }
    $(document).on("click", ".js-gl-v2-clear-filters, .gl-v2-filters-clear", resetFilters);

    // ── Filters-knop (optie 4f) — klapt het filtervelden-/badge-blok open/dicht. Twee triggers
    // sturen hetzelfde paneel aan: de inline "Filters"-knop in de toolbar en de topbar-filtericoon
    // (enkel <768px, @section MobileTopbarAction in IndexV2.cshtml) — op mobiel wordt #filters-panel
    // via CSS een bottom sheet i.p.v. inline te blijven staan. ─────────────────────────────────────
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

    // ── Mobiele kaarten (optie 4c) — zelfde architectuur als gl-v2-leveranciers.js. ─────────────────
    var $mobileList = $("#gl-v2-mobile-client-list");

    function buildMobileCard(tr) {
        var $tr = $(tr);
        var $cells = $tr.children("td");
        var name = $.trim($cells.eq(0).text());
        var enterpriseNr = $.trim($cells.eq(1).text());
        var city = $.trim($cells.eq(2).text());
        var contactHtml = $cells.eq(3).html();
        var issuers = $.trim($cells.eq(4).text());

        var clientId = $tr.data("id");
        var $srcMenu = $tr.find(".gl-v2-row-menu");
        var mobileMenuId = "gl-v2-row-menu-m-" + clientId;
        var $menuClone = $srcMenu.clone().attr("id", mobileMenuId);
        $menuClone.prepend($('<div class="gl-v2-row-menu-title"></div>').text(name));

        var $card = $(
            '<div class="gl-v2-client-card">' +
                '<div class="gl-v2-cc-row">' +
                    '<span class="gl-v2-cc-name"></span>' +
                    '<button type="button" class="gl-v2-row-menu-trigger" aria-haspopup="true" aria-expanded="false" aria-label="Meer acties"><i class="ph ph-dots-three" aria-hidden="true"></i></button>' +
                '</div>' +
                '<span class="gl-v2-cc-meta"></span>' +
                '<div class="gl-v2-cc-row gl-v2-cc-contact">' +
                    '<span class="gl-v2-cc-contact-item gl-v2-cc-contact-value"><i class="ph ph-envelope-simple" aria-hidden="true"></i><span></span></span>' +
                '</div>' +
                '<div class="gl-v2-cc-row gl-v2-cc-stats">' +
                    '<span class="gl-v2-cc-stat"><span class="gl-v2-cc-stat-label">Locatie</span><span class="gl-v2-cc-stat-value gl-v2-cc-city"></span></span>' +
                    '<span class="gl-v2-cc-stat"><span class="gl-v2-cc-stat-label">Facturatiebedrijf</span><span class="gl-v2-cc-stat-value gl-v2-cc-issuers"></span></span>' +
                '</div>' +
            '</div>'
        );

        $card.find(".gl-v2-cc-name").text(name);
        $card.find(".gl-v2-cc-meta").text(enterpriseNr || "–");
        $card.find(".gl-v2-cc-contact-value span").html(contactHtml || "–");
        if (!$.trim($card.find(".gl-v2-cc-contact-value").text())) $card.find(".gl-v2-cc-contact-value").hide();
        $card.find(".gl-v2-cc-city").text(city || "–");
        $card.find(".gl-v2-cc-issuers").text(issuers || "–").attr("title", issuers || "");
        $card.find(".gl-v2-row-menu-trigger").attr("aria-controls", mobileMenuId);
        $card.append($menuClone);

        return $card;
    }

    function renderMobileCards() {
        if (window.innerWidth >= 768 || !$mobileList.length) return;
        $mobileList.empty();
        if (table.rows({ search: "applied" }).count() === 0) {
            $mobileList.append($('<div class="gl-v2-client-card gl-v2-mobile-empty-card"></div>').html(buildEmptyStateHtml()));
            return;
        }
        $("#datatable-clients").children("tbody").children("tr").each(function () {
            $mobileList.append(buildMobileCard(this));
        });
    }

    var table = new DataTable("#datatable-clients", {
        order: [[0, "asc"]],
        autoWidth: false,
        language: buildLanguage(entity),
        stateSave: true,
        stateLoadParams: function (settings, data) { data.search.search = ""; },
        columnDefs: [
            { targets: -1, orderable: false, searchable: false, width: "3%" },
            { targets: 0, width: "26%" },
            { targets: 1, width: "16%" },
            { targets: 2, width: "16%" },
            { targets: 3, width: "20%" },
            { targets: 4, width: "19%" }
        ],
        layout: { topStart: { buttons: [] } },
        infoCallback: function (settings, start, end, max, total) {
            if (total === 0) return "0 " + entity.plural;
            var shown = end - start + 1;
            return shown + " van " + total + " " + (total === 1 ? entity.singular : entity.plural);
        }
    });
    table.on("draw", renderMobileCards);
    $(".dt-search").hide();

    // ── "Toon N klanten" (optie 4f "MOBIEL — FILTERSHEET"). ─────────────────────────────────────
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
        var theadEl = document.querySelector("#datatable-clients thead");
        var dtContainer = document.querySelector("#datatable-clients")?.closest(".dt-container");
        if (!appEl || !topbarEl || !contentEl || !theadEl || !dtContainer) return;

        var ROW_HEIGHT = 54; // moet in sync blijven met gl-v2-klanten.css tbody td { height }

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

    // ── Facturatiebedrijf-filter — zelfde BASIS-paneelvariant (optie 4h) als het boekjaarfilter op
    // Facturen: eigen trigger+paneel i.p.v. een gestyled <select>. ─────────────────────────────────
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
        var addButton = document.getElementById("add-client-button");
        if (addButton) {
            var canCreate = !selectedIssuerId || (config.writableIssuerIds || []).indexOf(Number(selectedIssuerId)) !== -1;
            addButton.classList.toggle("d-none", !canCreate);
            addButton.href = selectedIssuerId ? config.createUrl + "?issuerCompanyId=" + encodeURIComponent(selectedIssuerId) : config.createUrl;
        }
    });

    // Weerspiegelt de server-gerenderde beginstaat (bv. issuerCompanyId uit de querystring) in de
    // badge vanaf de eerste paint.
    updateFilterChips();

    // ── "···"-rijmenu (tablet/mobiel) — zelfde trigger+JS-gepositioneerd-paneel-patroon als
    // gl-v2-leveranciers.js/gl-v2-shell.js. ─────────────────────────────────────────────────────
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
