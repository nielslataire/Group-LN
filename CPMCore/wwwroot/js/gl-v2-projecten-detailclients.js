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

    // Klantkaart op gsm (design-handoff 64c): één kaart per klant — tikbare kop (avatar met initialen, naam, "Lot · type · m²"),
    // ⋯ als ronde knop van 44 (opent het bestaande rij-menu als bottom sheet), chip met de stand + e-mail, en het verkooptraject
    // als drie stappen (verkocht → akte → oplevering; vinkje = gebeurd, goud = volgende stap). Gebouwd uit de data-* van de tabelrij.
    function esc(text) { var d = document.createElement("div"); d.textContent = text == null ? "" : text; return d.innerHTML; }

    // Zoekterm markeren in de naam (64d beeld 4): <mark> om elk voorkomen, hoofdletterongevoelig.
    function highlight(text) {
        var t = text == null ? "" : String(text);
        var term = (typeof mobileTerm === "string" ? mobileTerm : "").trim();
        if (!term) return esc(t);
        var lower = t.toLowerCase(), q = term.toLowerCase(), out = "", i = 0, at;
        while ((at = lower.indexOf(q, i)) !== -1) {
            out += esc(t.slice(i, at)) + "<mark>" + esc(t.slice(at, at + q.length)) + "</mark>";
            i = at + q.length;
        }
        return out + esc(t.slice(i));
    }

    function stepHtml(label, value, state) {
        var icon = state === "ok" ? '<i class="ph ph-check" aria-hidden="true"></i>' : "";
        return '<li class="gl-v2-m-step' + (state ? " is-" + state : "") + '"><i>' + icon + '</i><span>' + esc(label) + '</span><b>' + esc(value) + '</b></li>';
    }

    function buildMobileCard(tr) {
        var d = tr.dataset;
        var detailUrl = tr.getAttribute("data-detail-url") || "#";
        var stage = d.stage || "plannen";
        var chipClass = stage === "plannen" ? "gl-v2-m-chip is-gold" : "gl-v2-m-chip is-ok";

        var card = document.createElement("article");
        card.className = "gl-v2-m-card gl-v2-m-client";
        var html =
            '<a class="gl-v2-m-client-head" href="' + esc(detailUrl) + '">' +
                '<span class="gl-v2-m-avatar">' + esc(d.initials || "?") + '</span>' +
                '<span class="gl-v2-m-client-t"><b>' + highlight(d.name) + '</b><small>' + esc(d.unitSub) + '</small></span>' +
            '</a>' +
            '<div class="gl-v2-m-client-meta"><i class="' + chipClass + '">' + esc(d.stageLabel) + '</i>' +
                (d.email ? '<span><i class="ph ph-envelope-simple" aria-hidden="true"></i>' + esc(d.email) + '</span>' : '') + '</div>';

        var deedOk = !!d.d2, soldOk = !!d.d1, deliveredOk = stage === "opgeleverd";
        html += '<ol class="gl-v2-m-steps">' +
            stepHtml("Verkocht", d.d1 || "—", soldOk ? "ok" : "") +
            stepHtml("Akte", deedOk ? d.d2 : (soldOk ? "te plannen" : "—"), deedOk ? "ok" : (soldOk ? "next" : "")) +
            stepHtml("Oplevering", d.d3 || (deedOk ? "nog niet gepland" : "—"), deliveredOk ? "ok" : (deedOk && !d.d3 ? "" : "")) +
            '</ol>';
        card.innerHTML = html;

        var menu = tr.querySelector(".gl-v2-menu");
        var trigger = tr.querySelector(".js-gl-v2-menu-trigger");
        if (menu && trigger) {
            var more = trigger.cloneNode(true);
            var menuClone = menu.cloneNode(true);
            var mobileMenuId = menu.id + "-m";
            menuClone.id = mobileMenuId;
            more.setAttribute("aria-controls", mobileMenuId);
            more.className = "gl-v2-m-roundbtn gl-v2-m-client-more js-gl-v2-menu-trigger";
            more.setAttribute("aria-label", "Acties voor " + (d.name || "klant"));
            card.appendChild(more);
            card.appendChild(menuClone);
            // Bellen-keuzeblad: de tabelrij is op gsm verborgen, dus ook dit blad moet een kopie in de kaart krijgen (en de knop moet ernaar wijzen).
            var callMenu = tr.querySelector('.gl-v2-menu[id^="gl-v2-dc-call-"]');
            if (callMenu) {
                var callClone = callMenu.cloneNode(true);
                callClone.id = callMenu.id + "-m";
                card.appendChild(callClone);
                var callBtn = menuClone.querySelector(".js-gl-v2-dc-call");
                if (callBtn) callBtn.setAttribute("data-target", callClone.id);
            }
        }
        return card;
    }

    // Filter op verkoopstand (pillen) en sortering (op lot / op naam) — enkel gsm.
    var mobileStage = "";
    var mobileSort = "lot";
    var mobileTerm = "";

    function renderMobileCards() {
        if (!mobileList || window.innerWidth >= 768) return;
        mobileList.innerHTML = "";
        // "Nog geen klant"-rijen (eenheden zonder klant) hebben geen data-stage: die blijven op tablet/desktop; de gsm-lijst is de klantenlijst.
        var rows = table.rows({ search: "applied" }).nodes().toArray().filter(function (tr) { return tr.dataset.stage; });
        if (mobileSort === "naam") rows.sort(function (x, y) { return (x.dataset.name || "").localeCompare(y.dataset.name || "", "nl"); });
        rows.forEach(function (tr) { mobileList.appendChild(buildMobileCard(tr)); });
        var count = document.getElementById("gl-v2-dc-m-count");
        var total = table.rows().nodes().toArray().filter(function (tr) { return tr.dataset.stage; }).length;
        var sortBtn = document.getElementById("gl-v2-dc-m-sort");
        if (sortBtn) sortBtn.hidden = !!mobileTerm;
        if (count) count.textContent = mobileTerm
            ? rows.length + " van " + total + (total === 1 ? " klant" : " klanten")
            : rows.length + (rows.length === 1 ? " klant" : " klanten") + " · op " + (mobileSort === "naam" ? "naam" : "lot");
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

    // Gsm-filter (64c): verkoopstand-pillen. Zelfde ext.search-mechanisme als Status/Eenheidstype hierboven.
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        if (settings.nTable !== tableElement || !mobileStage || window.innerWidth >= 768) return true;
        var row = settings.aoData[dataIndex]?.nTr;
        return !row || row.dataset.stage === mobileStage;
    });

    var mobilePills = document.getElementById("gl-v2-dc-m-pills");
    if (mobilePills) {
        mobilePills.addEventListener("click", function (e) {
            var pill = e.target.closest(".gl-v2-pill");
            if (!pill) return;
            mobileStage = pill.getAttribute("data-stage") || "";
            mobilePills.querySelectorAll(".gl-v2-pill").forEach(function (p) { p.classList.toggle("is-active", p === pill); });
            table.draw();
        });
    }
    var mobileSearch = document.getElementById("gl-v2-dc-m-search");
    var mobileTool = document.getElementById("gl-v2-dc-m-tool");
    var mobileClear = document.getElementById("gl-v2-dc-m-search-x");
    function syncSearching() {
        var on = !!mobileSearch && (document.activeElement === mobileSearch || mobileSearch.value !== "");
        if (mobileTool) mobileTool.classList.toggle("is-searching", on);
        document.body.classList.toggle("gl-v2-searching", on);
    }
    if (mobileSearch) {
        mobileSearch.addEventListener("input", function () { mobileTerm = mobileSearch.value; syncSearching(); table.search(mobileSearch.value).draw(); });
        mobileSearch.addEventListener("focus", syncSearching);
        mobileSearch.addEventListener("blur", function () { window.setTimeout(syncSearching, 0); });
    }
    if (mobileClear) mobileClear.addEventListener("mousedown", function (e) { e.preventDefault(); });
    if (mobileClear) mobileClear.addEventListener("click", function () {
        mobileSearch.value = ""; mobileTerm = ""; syncSearching(); table.search("").draw(); mobileSearch.focus();
    });

    // "Bellen" bij meerdere nummers (64d beeld 2): sluit het klantmenu en open het keuzeblad.
    document.addEventListener("click", function (e) {
        var btn = e.target.closest(".js-gl-v2-dc-call");
        if (!btn) return;
        e.preventDefault();
        var target = document.getElementById(btn.getAttribute("data-target"));
        if (!target) return;
        var ownMenu = btn.closest(".gl-v2-menu");
        var trigger = ownMenu ? document.querySelector('.js-gl-v2-menu-trigger[aria-controls="' + ownMenu.id + '"]') : null;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        if (!target.querySelector(".gl-v2-menu-cancel")) {
            var cancel = document.createElement("button");
            cancel.type = "button"; cancel.className = "gl-v2-menu-cancel"; cancel.textContent = "Annuleren";
            cancel.addEventListener("click", function () { if (window.GlV2Menu) window.GlV2Menu.closeAll(); });
            target.appendChild(cancel);
        }
        target.classList.add("is-open");
        var backdrop = document.getElementById("gl-v2-menu-backdrop");
        if (backdrop) backdrop.classList.add("is-open");
        if (trigger && window.GlV2Menu) window.GlV2Menu.reposition(trigger, target);
    });
    var mobileSortBtn = document.getElementById("gl-v2-dc-m-sort");
    if (mobileSortBtn) mobileSortBtn.addEventListener("click", function () {
        mobileSort = mobileSort === "lot" ? "naam" : "lot";
        renderMobileCards();
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


    updateFilterChips();

    // Filters (status, eenheidstype), zoekterm, sortering en pagina onthouden (projectbrede module gl-v2-table-state.js).
    if (window.GlV2TableState) {
        var tableState = window.GlV2TableState.attach({
            table: table,
            collect: function () { return { q: searchInput ? searchInput.value : "", status: selectedStatus, type: selectedType }; },
            restore: function (s) {
                if (s.q && searchInput) { searchInput.value = s.q; table.search(s.q); }
                if (s.status) statusSelectApi.select(s.status);
                if (s.type) typeSelectApi.select(s.type);
            }
        });
        table.draw();
        updateFilterChips();
        tableState.finish();
    }
})();
