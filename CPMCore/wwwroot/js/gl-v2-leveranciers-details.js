// gl-v2 layout-pilot — Leveranciers/DetailsV2.cshtml. Eigen pagina-JS, zelfde "eigen <pagina>.js"-
// conventie als gl-v2-leveranciers-edit.js. De Contracten-/Facturen-tabellen moeten exact dezelfde
// opmaak/gedrag hebben als Leveranciers/IndexV2 se eigen tabel (paginering, dezelfde rij-actie-
// iconen, GEEN "···"-contextmenu op desktop, kaarten i.p.v. een tabel op mobiel) — vandaar een echte
// DataTable per tabel (zelfde CDN-bundel als IndexV2), het exacte .gl-v2-row-menu/-trigger/-action-
// trigger+paneel-patroon van gl-v2-leveranciers.js, en diezelfde renderMobileCards()-architectuur
// (<768px: aparte kaartenlijst, gevuld uit de actuele tabelrijen bij elke DataTable-"draw", dus
// automatisch in sync met paginering/sortering) — alles hier zelfstandig herbouwd (klein genoeg, en
// zo blijft DetailsV2 los van die zwaardere, filter-/DataTable-lijstpagina-specifieke file). Elke
// DataTable wordt pas geïnitialiseerd de EERSTE keer dat haar tab echt geopend wordt —
// initialiseren terwijl het tabpaneel nog [hidden] staat geeft DataTables een 0px-brede container en
// dus kapotte kolombreedtes, een bekende valkuil.
(function () {
    "use strict";

    var config = window.glV2SupplierDetailsConfig || {};
    var initializedTabs = {};
    // <768px toont een vaste kaartenlijst-lengte (net als IndexV2 se eigen MOBILE_PAGE_LENGTH) i.p.v.
    // een viewporthoogte-berekening — op mobiel is er toch geen aparte tabelrij-hoogte om tegen te
    // meten (kaarten variëren zelf al in hoogte), zie syncTablePageLength() hieronder.
    var MOBILE_PAGE_LENGTH = 15;

    initTabs();
    initDeleteModal();
    initRowMenus();

    // ── Tabs — activeert de tab en initialiseert (eenmalig, lazy) de bijhorende DataTable. ─────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-supplier-details-tabbar");
        if (!tabbar) return;

        function activate(key) {
            tabbar.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (tab) {
                var isActive = tab.getAttribute("data-tab") === key;
                tab.classList.toggle("is-active", isActive);
                tab.setAttribute("aria-selected", isActive ? "true" : "false");
            });
            document.querySelectorAll(".gl-v2-tab-panel").forEach(function (panel) {
                panel.hidden = panel.getAttribute("data-tab-panel") !== key;
            });
            if (!initializedTabs[key]) {
                initializedTabs[key] = true;
                if (key === "contracten") initContractsTable();
                if (key === "facturen") initInvoicesTable();
            }
        }

        tabbar.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab) return;
            activate(tab.getAttribute("data-tab"));
        });
    }

    // ── DataTables — zelfde init-vorm als gl-v2-leveranciers.js se eigen #datatable-suppliers:
    //    geen zoekbalk/knoppen (eerste .dt-layout-row wordt CSS-only verborgen, zie
    //    gl-v2-leveranciers-details.css), enkel paginering + "N van M"-teller. Geen eigen
    //    lege-staat-tekst nodig — de tabel wordt server-side enkel gerenderd als er al rijen zijn
    //    (zie DetailsV2.cshtml se eigen @@if (...Any()) — anders staat er al een .gl-v2-card-empty).
    //    buildMobileCard(tr) bouwt de <768px-kaartvariant van één rij — zie initContractsTable()/
    //    initInvoicesTable() hieronder voor de twee tabel-eigen implementaties. ─────────────────────
    function initDataTable(tableId, mobileListId, singular, plural, orderColumn, orderDir, buildMobileCard) {
        var el = document.getElementById(tableId);
        var mobileList = document.getElementById(mobileListId);
        if (!el || typeof DataTable === "undefined") return;

        function renderMobileCards() {
            if (window.innerWidth >= 768 || !mobileList) return;
            mobileList.innerHTML = "";
            el.querySelectorAll("tbody tr").forEach(function (tr) {
                mobileList.appendChild(buildMobileCard(tr));
            });
        }

        var table = new DataTable("#" + tableId, {
            order: [[orderColumn, orderDir]],
            autoWidth: false,
            columnDefs: [{ targets: -1, orderable: false, searchable: false }],
            language: {
                paginate: { previous: "‹", next: "›" },
                aria: {
                    sortAscending: ": activeer om oplopend te sorteren",
                    sortDescending: ": activeer om aflopend te sorteren"
                }
            },
            infoCallback: function (settings, start, end, max, total) {
                var shown = end - start + 1;
                return shown + " van " + total + " " + (total === 1 ? singular : plural);
            }
        });

        // Zelfde recept als gl-v2-leveranciers.js se eigen syncTablePageLength(): hoeveel rijen
        // passen er in de beschikbare hoogte onder de tabbar/boven de onderkant van het scherm? Is
        // er minder data dan die ruimte toelaat, dan krimpt de paginalengte gewoon mee tot het echte
        // aantal rijen (Math.max(recordsTotal,3)) — de kaart blijft dan zo kort als haar inhoud i.p.v.
        // kunstmatig tot onderaan uitgerekt (zie de toelichting bij .gl-v2-detail-table-card in
        // gl-v2-leveranciers-details.css). Rijhoogte wordt LIVE gemeten (offsetHeight van de eerste
        // gerenderde rij) i.p.v. een hardgecodeerde constante — hoeft dan niet met de CSS in sync
        // gehouden te worden zoals IndexV2 se eigen ROW_HEIGHT dat wel moet.
        function syncTablePageLength() {
            if (window.innerWidth < 768) {
                if (table.page.len() !== MOBILE_PAGE_LENGTH) table.page.len(MOBILE_PAGE_LENGTH).draw(false);
                return;
            }

            var appEl = document.querySelector(".gl-v2-app");
            var topbarEl = document.querySelector(".gl-v2-topbar");
            var contentEl = document.querySelector(".gl-v2-content");
            var tabbarEl = document.getElementById("gl-v2-supplier-details-tabbar");
            var theadEl = el.querySelector("thead");
            var firstRow = el.querySelector("tbody tr");
            var cardEl = el.closest(".gl-v2-detail-table-card");
            var dtContainer = el.closest(".dt-container");
            if (!appEl || !topbarEl || !contentEl || !tabbarEl || !theadEl || !firstRow || !cardEl || !dtContainer) return;

            var footerRow = dtContainer.querySelector(".dt-layout-row:last-child");
            var footerHeight = footerRow ? footerRow.offsetHeight : 48;
            var rowHeight = firstRow.offsetHeight || 44;

            var appStyle = getComputedStyle(appEl);
            var appVerticalPadding = (parseFloat(appStyle.paddingTop) || 0) + (parseFloat(appStyle.paddingBottom) || 0);
            var contentStyle = getComputedStyle(contentEl);
            var contentVerticalPadding = (parseFloat(contentStyle.paddingTop) || 0) + (parseFloat(contentStyle.paddingBottom) || 0);
            var contentGap = parseFloat(contentStyle.rowGap) || 16;
            var cardPaddingBottom = parseFloat(getComputedStyle(cardEl).paddingBottom) || 0;

            var available = window.innerHeight
                - appVerticalPadding
                - topbarEl.offsetHeight
                - contentVerticalPadding
                - tabbarEl.offsetHeight
                - contentGap
                - theadEl.offsetHeight
                - footerHeight
                - cardPaddingBottom;

            var maxRows = Math.max(Math.floor(available / rowHeight), 3);
            var recordsTotal = table.page.info().recordsTotal;
            var rows = recordsTotal ? Math.min(maxRows, Math.max(recordsTotal, 3)) : maxRows;

            if (rows !== table.page.len()) table.page.len(rows).draw(false);
        }

        table.on("draw", renderMobileCards);
        renderMobileCards();
        syncTablePageLength();

        var resizeTimer = null;
        window.addEventListener("resize", function () {
            window.clearTimeout(resizeTimer);
            resizeTimer = window.setTimeout(function () {
                syncTablePageLength();
                renderMobileCards();
            }, 150);
        });
    }

    // Kloont het rij-eigen ···-menu (trigger + paneel, Detail/Bewerken) in de mobiele kaart — zelfde
    // "···"-JS werkt er vanzelf op door (event-delegatie op document, zie initRowMenus()), enkel de
    // id's moeten uniek blijven t.o.v. het origineel in de (op mobiel toch verborgen) tabelrij.
    function cloneRowMenu(tr) {
        var srcMenu = tr.querySelector(".gl-v2-row-menu");
        var srcTrigger = tr.querySelector(".gl-v2-row-menu-trigger");
        if (!srcMenu || !srcTrigger) return null;
        var menu = srcMenu.cloneNode(true);
        menu.id = srcMenu.id + "-mobile";
        var trigger = srcTrigger.cloneNode(true);
        trigger.setAttribute("aria-controls", menu.id);
        return { trigger: trigger, menu: menu };
    }

    function initContractsTable() {
        initDataTable("gl-v2-supplier-contracts-table", "gl-v2-supplier-contracts-mobile-list", "contract", "contracten", 0, "asc", function (tr) {
            var cells = tr.querySelectorAll("td");
            var cloned = cloneRowMenu(tr);

            var card = document.createElement("div");
            card.className = "gl-v2-detail-mobile-card";

            var row = document.createElement("div");
            row.className = "gl-v2-dmc-row";
            var title = document.createElement("span");
            title.className = "gl-v2-dmc-title";
            title.textContent = cells[0].textContent.trim();
            row.appendChild(title);
            if (cloned) row.appendChild(cloned.trigger);
            card.appendChild(row);

            var meta = document.createElement("span");
            meta.className = "gl-v2-dmc-meta";
            meta.textContent = "Contract #" + cells[1].textContent.trim();
            card.appendChild(meta);

            var stats = document.createElement("div");
            stats.className = "gl-v2-dmc-row gl-v2-dmc-stats";
            stats.innerHTML =
                '<span class="gl-v2-dmc-stat"><span class="gl-v2-dmc-stat-label">Activiteiten</span><span class="gl-v2-dmc-stat-value"></span></span>' +
                '<span class="gl-v2-dmc-stat"><span class="gl-v2-dmc-stat-label">Totale waarde</span><span class="gl-v2-dmc-stat-value"></span></span>';
            stats.querySelectorAll(".gl-v2-dmc-stat-value")[0].textContent = cells[2].textContent.trim();
            stats.querySelectorAll(".gl-v2-dmc-stat-value")[1].textContent = cells[3].textContent.trim();
            card.appendChild(stats);

            if (cloned) card.appendChild(cloned.menu);
            return card;
        });
    }

    function initInvoicesTable() {
        initDataTable("gl-v2-supplier-invoices-table", "gl-v2-supplier-invoices-mobile-list", "factuur", "facturen", 0, "desc", function (tr) {
            var cells = tr.querySelectorAll("td");
            var cloned = cloneRowMenu(tr);
            var externalRef = cells[1].textContent.trim();

            var card = document.createElement("div");
            card.className = "gl-v2-detail-mobile-card";

            var row = document.createElement("div");
            row.className = "gl-v2-dmc-row";
            var title = document.createElement("span");
            title.className = "gl-v2-dmc-title";
            title.textContent = cells[0].textContent.trim();
            row.appendChild(title);
            if (cloned) row.appendChild(cloned.trigger);
            card.appendChild(row);

            var meta = document.createElement("span");
            meta.className = "gl-v2-dmc-meta";
            meta.textContent = (externalRef === "—" ? "Geen externe referentie" : externalRef) + " · " + cells[2].textContent.trim();
            card.appendChild(meta);

            var stats = document.createElement("div");
            stats.className = "gl-v2-dmc-row gl-v2-dmc-stats";
            stats.innerHTML = '<span class="gl-v2-dmc-stat"><span class="gl-v2-dmc-stat-label">Bedrag</span><span class="gl-v2-dmc-stat-value"></span></span>';
            stats.querySelector(".gl-v2-dmc-stat-value").textContent = cells[3].textContent.trim();
            card.appendChild(stats);

            if (cloned) card.appendChild(cloned.menu);
            return card;
        });
    }

    // ── Rij-"···"-menu (enkel ≤1023.98px, zie de CSS) — 1-op-1 hetzelfde trigger+JS-gepositioneerd-
    //    paneel-patroon als gl-v2-leveranciers.js se eigen rijmenu, hier zelfstandig (geen jQuery-
    //    afhankelijkheid nodig voor dit stukje). Op desktop doet dit niets zichtbaars: .gl-v2-row-menu
    //    is daar gewoon een inline rij altijd-zichtbare .gl-v2-row-action-knoppen (CSS), geen
    //    "···"-contextmenu. ──────────────────────────────────────────────────────────────────────────
    function initRowMenus() {
        var backdrop = document.getElementById("gl-v2-row-menu-backdrop");

        function closeAll() {
            document.querySelectorAll(".gl-v2-row-menu.is-open").forEach(function (menu) {
                menu.classList.remove("is-open");
                var trigger = document.querySelector('.gl-v2-row-menu-trigger[aria-controls="' + menu.id + '"]');
                if (trigger) {
                    trigger.classList.remove("is-menu-open");
                    trigger.setAttribute("aria-expanded", "false");
                }
            });
            if (backdrop) backdrop.classList.remove("is-open");
        }

        function position(trigger, menu) {
            var rect = trigger.getBoundingClientRect();
            var menuWidth = menu.offsetWidth || 200;
            var left = Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 12);
            menu.style.left = Math.max(12, left) + "px";
            var top = rect.bottom + 6;
            var maxTop = window.innerHeight - menu.offsetHeight - 12;
            menu.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        }

        document.addEventListener("click", function (e) {
            var trigger = e.target.closest(".gl-v2-row-menu-trigger");
            if (!trigger) return;
            e.preventDefault();
            e.stopPropagation();
            var menu = document.getElementById(trigger.getAttribute("aria-controls"));
            if (!menu) return;
            var wasOpen = menu.classList.contains("is-open");
            closeAll();
            if (wasOpen) return;
            menu.classList.add("is-open");
            trigger.classList.add("is-menu-open");
            trigger.setAttribute("aria-expanded", "true");
            if (backdrop) backdrop.classList.add("is-open");
            position(trigger, menu);
        });
        document.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-row-menu") || e.target.closest(".gl-v2-row-menu-trigger")) return;
            closeAll();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") closeAll();
        });
        if (backdrop) backdrop.addEventListener("click", closeAll);
        window.addEventListener("resize", closeAll);
    }

    // ── Verwijder-bevestiging (optie 4j TYPE 1, danger) — zelfde AJAX-fragment-in-lege-modal-shell
    //    als gl-v2-leveranciers.js se eigen .deleteSupplier-handler op IndexV2, hier met het rode
    //    icoon-knopje in de topbar als trigger i.p.v. een rijknop. ─────────────────────────────────
    function initDeleteModal() {
        var trigger = document.querySelector(".js-gl-v2-delete-supplier");
        var modalEl = document.getElementById("gl-v2-delete-supplier-modal");
        var container = document.getElementById("gl-v2-delete-supplier-container");
        if (!trigger || !modalEl || !container || !window.bootstrap || !config.deleteUrl) return;

        var modal = new window.bootstrap.Modal(modalEl, { backdrop: "static", keyboard: false });

        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            container.innerHTML = '<div class="p-3 text-muted">Laden …</div>';
            fetch(config.deleteUrl + "?id=" + encodeURIComponent(config.supplierId))
                .then(function (r) { return r.ok ? r.text() : Promise.reject(); })
                .then(function (html) { container.innerHTML = html; })
                .catch(function () { container.innerHTML = '<div class="p-3 text-danger">Kon leverancier niet laden.</div>'; });
            modal.show();
        });
    }
})();
