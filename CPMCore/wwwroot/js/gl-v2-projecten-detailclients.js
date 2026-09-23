// gl-v2 layout-pilot — Projecten/DetailClientsV2 (design-handoff punt 12d). Same DataTables +
// custom-filter recipe as gl-v2-leveranciers.js (search field drives table.search(), Status/
// Eenheidstype are $.fn.dataTable.ext.search filters reading data-* attributes off each <tr>) —
// copied and adapted rather than shared, same reasoning as that file's own header comment. Row
// click-to-navigate, the "···" row menu and the copy-email button are NOT reimplemented here: they're
// the generic gl-v2-shell.js behaviors (initClickableRows/initContextMenus/initTableEmailActions),
// already wired shell-wide off data-detail-url/.js-gl-v2-menu-trigger/.js-gl-v2-copy-email — this file
// only owns what's genuinely page-specific.
(function () {
    "use strict";
    var config = window.glV2DetailClientsConfig || {};

    // ── Verwijder-modal — zelfde AJAX-fragment-in-lege-modal-shell recept als Leveranciers/DetailsV2
    // en Leveranciers/IndexV2, hier tegen de al bestaande KlantenController.PartialDeleteClientModalV2. ──
    var deleteModalElement = document.getElementById("gl-v2-dc-delete-modal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-dc-delete");
        if (!trigger) return;
        e.preventDefault();
        var id = trigger.getAttribute("data-id");
        var container = document.getElementById("gl-v2-dc-delete-container");
        container.innerHTML = '<div class="p-3 text-muted">Laden…</div>';
        fetch(config.deleteUrl + "?id=" + encodeURIComponent(id))
            .then(function (r) { return r.text(); })
            .then(function (html) { container.innerHTML = html; })
            .catch(function () { container.innerHTML = '<div class="p-3 text-danger">Kon klant niet laden.</div>'; });
        if (deleteModal) deleteModal.show();
    });

    // ── Mobiel (<768px) — kaarten i.p.v. tabel (design-handoff optie 4c), zelfde
    // bouw-uit-de-tabelrij-recept als Klanten/IndexV2's eigen renderMobileCards()/buildMobileCard()
    // (gl-v2-klanten.js), hier aangepast aan deze pagina's eigen kolommen. Een "Nog geen klant"-rij
    // heeft geen .gl-v2-menu/geen data-detail-url — de kaart laat dan gewoon de menu-knop en de
    // klikbare kaart-styling weg i.p.v. een lege/kapotte trigger te tonen. ─────────────────────────
    var mobileList = document.getElementById("gl-v2-dc-mobile-list");

    function buildMobileCard(tr) {
        var cells = tr.querySelectorAll("td");
        var card = document.createElement("div");
        card.className = "gl-v2-dc-card";
        var detailUrl = tr.getAttribute("data-detail-url");
        if (detailUrl) card.setAttribute("data-detail-url", detailUrl);

        var headRow = document.createElement("div");
        headRow.className = "gl-v2-dc-card-row";
        var clientCell = cells[0].querySelector(".gl-v2-dc-client");
        if (clientCell) headRow.appendChild(clientCell.cloneNode(true));

        var menu = tr.querySelector(".gl-v2-menu");
        if (menu) {
            var trigger = tr.querySelector(".js-gl-v2-menu-trigger").cloneNode(true);
            var menuClone = menu.cloneNode(true);
            var mobileMenuId = menu.id + "-m";
            menuClone.id = mobileMenuId;
            trigger.setAttribute("aria-controls", mobileMenuId);
            headRow.appendChild(trigger);
            card.appendChild(headRow);
            card.appendChild(menuClone);
        } else {
            card.appendChild(headRow);
        }

        var unitsCell = cells[1].querySelector(".gl-v2-dc-units");
        if (unitsCell) {
            var meta = unitsCell.cloneNode(true);
            meta.className = "gl-v2-dc-card-meta";
            card.appendChild(meta);
        }

        var contact = document.createElement("div");
        contact.className = "gl-v2-dc-card-contact";
        contact.innerHTML = cells[2].innerHTML;
        card.appendChild(contact);

        var stats = document.createElement("div");
        stats.className = "gl-v2-dc-card-stats";
        [["Verkocht", 3], ["Akte", 4], ["Oplevering", 5]].forEach(function (pair) {
            var value = (cells[pair[1]].textContent || "").trim();
            var stat = document.createElement("span");
            stat.className = "gl-v2-dc-card-stat";
            stat.innerHTML = '<span class="gl-v2-dc-card-stat-label">' + pair[0] + '</span><span class="gl-v2-dc-card-stat-value"></span>';
            stat.querySelector(".gl-v2-dc-card-stat-value").textContent = value || "—";
            stats.appendChild(stat);
        });
        card.appendChild(stats);

        return card;
    }

    function renderMobileCards() {
        if (!mobileList || window.innerWidth >= 768) return;
        mobileList.innerHTML = "";
        var rows = table.rows({ search: "applied" }).nodes().toArray();
        rows.forEach(function (tr) { mobileList.appendChild(buildMobileCard(tr)); });
    }

    document.addEventListener("click", function (e) {
        if (e.target.closest("a, button")) return;
        var card = e.target.closest(".gl-v2-dc-card[data-detail-url]");
        if (card) window.location.href = card.getAttribute("data-detail-url");
    });

    var tableElement = document.getElementById("datatable-clients");
    if (!tableElement || typeof DataTable === "undefined") return;

    var table = new DataTable("#datatable-clients", {
        order: [],
        pageLength: 25,
        language: {
            search: "Zoeken:",
            lengthMenu: "Toon _MENU_",
            info: "Klant _START_ tot _END_ van _TOTAL_",
            infoEmpty: "Geen klanten",
            infoFiltered: "(gefilterd uit _MAX_ totaal)",
            zeroRecords: '<div class="gl-v2-empty-state gl-v2-empty-state-inline">' +
                '<span class="gl-v2-empty-state-icon"><i class="ph ph-users" aria-hidden="true"></i></span>' +
                '<h2 class="gl-v2-empty-state-title">Geen klanten gevonden</h2>' +
                '<p class="gl-v2-empty-state-desc">Er zijn geen klanten die aan deze filters voldoen.</p>' +
                '</div>',
            paginate: { previous: "‹", next: "›" }
        }
    });
    $(".dt-search").hide();
    table.on("draw", renderMobileCards);
    renderMobileCards();
    var mobileResizeTimer = null;
    window.addEventListener("resize", function () {
        window.clearTimeout(mobileResizeTimer);
        mobileResizeTimer = window.setTimeout(renderMobileCards, 150);
    });

    // ── Filters (Status/Eenheidstype) — zelfde ext.search-recept als gl-v2-leveranciers.js. ────────
    var selectedStatus = document.getElementById("status-select-value")?.value || "";
    var selectedType = document.getElementById("type-select-value")?.value || "";

    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        if (settings.nTable !== tableElement) return true;
        var row = settings.aoData[dataIndex]?.nTr;
        if (!row) return true;

        if (selectedStatus) {
            var rowStatuses = (row.dataset.statuses || "").split(",").filter(Boolean);
            if (!rowStatuses.includes(selectedStatus)) return false;
        }
        if (selectedType) {
            var rowTypes = (row.dataset.types || "").split(",").filter(Boolean);
            if (!rowTypes.includes(selectedType)) return false;
        }
        return true;
    });

    // ── Generieke .gl-v2-select-init, zelfde aanpak als gl-v2-leveranciers.js se initSelect(). ─────
    function initSelect(prefix, onChange) {
        var trigger = document.getElementById(prefix + "-trigger");
        var panel = document.getElementById(prefix + "-panel");
        var backdrop = document.getElementById(prefix + "-backdrop");
        var label = document.getElementById(prefix + "-label");
        var closeBtn = document.getElementById(prefix + "-close");
        var valueInput = document.getElementById(prefix + "-value");
        if (!trigger || !panel) return { select: function () {} };

        function closeAll() {
            document.querySelectorAll(".gl-v2-select-panel.is-open").forEach(function (p) { p.classList.remove("is-open"); });
            document.querySelectorAll(".gl-v2-select-backdrop.is-open").forEach(function (b) { b.classList.remove("is-open"); });
            document.querySelectorAll(".gl-v2-select-trigger[aria-expanded='true']").forEach(function (t) { t.setAttribute("aria-expanded", "false"); });
        }

        function selectValue(value, fireOnChange) {
            var option = panel.querySelector('.gl-v2-select-option[data-value="' + value + '"]');
            panel.querySelectorAll(".gl-v2-select-option").forEach(function (o) { o.classList.toggle("is-selected", o === option); });
            if (valueInput) valueInput.value = value;
            if (label && option) label.textContent = option.getAttribute("data-label");
            trigger.classList.toggle("is-filled", !!value);
            if (fireOnChange !== false && typeof onChange === "function") onChange(value);
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
        panel.querySelectorAll(".gl-v2-select-option").forEach(function (option) {
            option.addEventListener("click", function () {
                selectValue(option.getAttribute("data-value"), true);
                closeAll();
            });
        });
        document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeAll(); });

        return { select: selectValue };
    }

    var statusSelectApi = initSelect("status-select", function (value) { selectedStatus = value || ""; table.draw(); updateFilterChips(); });
    var typeSelectApi = initSelect("type-select", function (value) { selectedType = value || ""; table.draw(); updateFilterChips(); });

    function optionLabel(panelId, value) {
        var opt = document.querySelector("#" + panelId + ' .gl-v2-select-option[data-value="' + value + '"]');
        return opt ? opt.getAttribute("data-label") : value;
    }

    function updateFilterChips() {
        var chips = [];
        if (selectedStatus) chips.push({ type: "status", label: optionLabel("status-select-panel", selectedStatus) });
        if (selectedType) chips.push({ type: "type", label: optionLabel("type-select-panel", selectedType) });

        var chipList = document.getElementById("filters-chip-list");
        if (!chipList) return;
        chipList.innerHTML = "";
        chips.forEach(function (chip) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "gl-v2-filters-chip";
            btn.setAttribute("data-filter-type", chip.type);
            btn.innerHTML = "<span></span><i class=\"ph ph-x\" aria-hidden=\"true\"></i>";
            btn.querySelector("span").textContent = chip.label;
            chipList.appendChild(btn);
        });

        var count = chips.length;
        ["filters-toggle-count"].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) { el.textContent = count; el.hidden = count === 0; }
        });
        var toggle = document.getElementById("filters-toggle");
        if (toggle) toggle.classList.toggle("has-active", count > 0);
        var clearBtn = document.getElementById("filters-clear");
        if (clearBtn) clearBtn.hidden = count === 0;
        var sheetBadge = document.getElementById("filters-sheet-badge");
        if (sheetBadge) { sheetBadge.textContent = count + " actief"; sheetBadge.hidden = count === 0; }
    }

    document.addEventListener("click", function (e) {
        var chip = e.target.closest(".gl-v2-filters-chip");
        if (!chip) return;
        var type = chip.getAttribute("data-filter-type");
        if (type === "status") statusSelectApi.select("");
        else if (type === "type") typeSelectApi.select("");
    });

    function resetFilters() {
        var searchInput = document.getElementById("search-term");
        if (searchInput) searchInput.value = "";
        table.search("");
        statusSelectApi.select("", false);
        selectedStatus = "";
        typeSelectApi.select("", false);
        selectedType = "";
        table.draw();
        updateFilterChips();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-gl-v2-clear-filters, .gl-v2-filters-clear")) resetFilters();
    });

    // ── Zoekveld ─────────────────────────────────────────────────────────────────────────────────
    var searchInput = document.getElementById("search-term");
    if (searchInput) {
        searchInput.addEventListener("input", function () { table.search(searchInput.value).draw(); });
        var clearBtn = document.getElementById("search-term-clear");
        if (clearBtn) clearBtn.addEventListener("click", function () { searchInput.value = ""; table.search("").draw(); });
    }

    // ── Filters-paneel open/dicht ────────────────────────────────────────────────────────────────
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

    updateFilterChips();
})();
