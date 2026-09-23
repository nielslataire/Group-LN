// gl-v2 layout-pilot — Projecten/DetailContractsV2 (design-handoff punt 14a). Same DataTables +
// custom ext.search-filter recipe as gl-v2-projecten-detailclients.js, adapted: the filter here is
// a single tabbar (Alle/Met contract/Niet getekend/Zonder contract) reading data-status off each
// <tr> instead of a .gl-v2-select dropdown. Row click-to-navigate and the "···" row menu are the
// generic gl-v2-shell.js behaviors (initClickableRows/initContextMenus) — this file only owns what's
// genuinely page-specific: the tabbar filter, the delete modal, the aannemerslijst-print dropdown,
// and the mobile card fallback.
(function () {
    "use strict";
    var config = window.glV2ProjectenContractsConfig || {};

    // ── Verwijder-modal — zelfde AJAX-fragment-in-lege-modal-shell recept als Klanten/DetailClientsV2. ──
    var deleteModalElement = document.getElementById("gl-v2-pc-delete-modal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-pc-delete");
        if (!trigger) return;
        e.preventDefault();
        var id = trigger.getAttribute("data-id");
        var container = document.getElementById("gl-v2-pc-delete-container");
        container.innerHTML = '<div class="p-3 text-muted">Laden…</div>';
        fetch(config.deleteUrl + "?id=" + encodeURIComponent(id))
            .then(function (r) { return r.text(); })
            .then(function (html) { container.innerHTML = html; })
            .catch(function () { container.innerHTML = '<div class="p-3 text-danger">Kon contract niet laden.</div>'; });
        if (deleteModal) deleteModal.show();
    });

    // ── Aannemerslijst-PDF — kolomkeuze als querystring, zelfde params als de legacy pagina. ────────
    var printBtn = document.getElementById("btnPrintSupplierList");
    if (printBtn) {
        printBtn.addEventListener("click", function () {
            var params = $.param({
                projectid: config.projectId,
                sent: $("#chkSupplierListSent").is(":checked"),
                signed: $("#chkSupplierListSigned").is(":checked"),
                vgm: $("#chkSupplierListVgm").is(":checked"),
                notification: $("#chkSupplierListNotification").is(":checked"),
                pid: $("#chkSupplierListPid").is(":checked")
            });
            window.open(config.printUrl + "?" + params, "_blank");
        });
    }

    // ── Mobiel (<768px) — kaarten i.p.v. tabel, zelfde bouw-uit-de-tabelrij-recept als
    // gl-v2-projecten-detailclients.js se buildMobileCard(). ────────────────────────────────────────
    var mobileList = document.getElementById("gl-v2-pc-mobile-list");

    function buildMobileCard(tr) {
        var cells = tr.querySelectorAll("td");
        var card = document.createElement("div");
        card.className = "gl-v2-pc-card";
        var detailUrl = tr.getAttribute("data-detail-url");
        if (detailUrl) card.setAttribute("data-detail-url", detailUrl);

        var headRow = document.createElement("div");
        headRow.className = "gl-v2-pc-card-row";
        var supplierCell = cells[1].querySelector(".gl-v2-pc-supplier");
        if (supplierCell) headRow.appendChild(supplierCell.cloneNode(true));

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

        var meta = document.createElement("div");
        meta.className = "gl-v2-pc-card-meta";
        [["Contract", 2], ["Contractprijs", 3], ["Gefactureerd", 4], ["Verstuurd", 7]].forEach(function (pair) {
            var value = (cells[pair[1]].textContent || "").trim();
            var stat = document.createElement("span");
            stat.className = "gl-v2-pc-card-stat";
            stat.innerHTML = '<span class="gl-v2-pc-card-stat-label">' + pair[0] + '</span><span class="gl-v2-pc-card-stat-value"></span>';
            stat.querySelector(".gl-v2-pc-card-stat-value").textContent = value || "—";
            meta.appendChild(stat);
        });
        card.appendChild(meta);

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
        var card = e.target.closest(".gl-v2-pc-card[data-detail-url]");
        if (card) window.location.href = card.getAttribute("data-detail-url");
    });

    var tableElement = document.getElementById("datatable-contracts");
    if (!tableElement || typeof DataTable === "undefined") return;

    var table = new DataTable("#datatable-contracts", {
        order: [],
        pageLength: 10,
        language: {
            search: "Zoeken:",
            lengthMenu: "Toon _MENU_",
            info: "Leverancier _START_ tot _END_ van _TOTAL_",
            infoEmpty: "Geen leveranciers",
            infoFiltered: "(gefilterd uit _MAX_ totaal)",
            zeroRecords: '<div class="gl-v2-empty-state gl-v2-empty-state-inline">' +
                '<span class="gl-v2-empty-state-icon"><i class="ph ph-hard-hat" aria-hidden="true"></i></span>' +
                '<h2 class="gl-v2-empty-state-title">Geen leveranciers gevonden</h2>' +
                '<p class="gl-v2-empty-state-desc">Er zijn geen leveranciers die aan deze filters voldoen.</p>' +
                '</div>',
            paginate: { previous: "‹", next: "›" }
        }
    });
    $(".dt-search").hide();
    table.on("draw", renderMobileCards);
    renderMobileCards();

    // ── Rijen per pagina aangepast aan de schermhoogte, zodat de paginering onderaan altijd
    // zichtbaar is zonder te scrollen. Eerdere versie GOKTE de rijhoogte (52px) en een vaste
    // "resterende chrome"-marge (140px) vooraf — op een groter scherm klopte die gok niet (te veel
    // rijen, paginering alsnog onder de fold), want de echte rijhoogte/footerhoogte hangen af van
    // lettergrootte/zoomniveau/browser, niet van een constante. Nu: laat de tabel één keer echt
    // renderen (pageLength:10 hierboven volstaat, er staat altijd minstens 1 rij + de footer), MEET
    // dan de echte rijhoogte en de echte footerhoogte (dt-layout-row met de paginering erin) uit de
    // DOM, en reken van daaruit exact hoeveel rijen er passen tussen de kaart-top en de onderkant
    // van het venster — inclusief die footer. Herhaalt bij resize (gedebouncet), niet enkel bij load. ──
    function adjustPageLength() {
        var card = document.querySelector(".gl-v2-detail-table-card");
        var firstRow = tableElement.querySelector("tbody tr");
        var thead = tableElement.querySelector("thead");
        var tfoot = tableElement.querySelector("tfoot");
        var footer = card ? card.querySelector(".dt-layout-row:last-child") : null;
        if (!card || !firstRow) return;

        var rowHeight = firstRow.getBoundingClientRect().height;
        // thead (kolomkoppen) en tfoot (de "Totaal"-rij) staan ALTIJD boven/onder de tbody-rijen,
        // buiten DataTables' eigen paginering — eerdere versie negeerde beide en telde enkel de
        // info/paginering-balk (footerHeight) mee, waardoor "beschikbare ruimte" stelselmatig te
        // groot uitkwam: op desktop paste de Totaal-rij dan niet meer binnen het venster en kreeg de
        // pagina alsnog een browserscrollbar, exact het gerapporteerde probleem.
        var theadHeight = thead ? thead.getBoundingClientRect().height : 0;
        var tfootHeight = tfoot ? tfoot.getBoundingClientRect().height : 0;
        var footerHeight = footer ? footer.getBoundingClientRect().height : 56;
        var cardTop = card.getBoundingClientRect().top;
        // 16px veiligheidsmarge onderaan het venster — anders raakt de laatste paginaknop precies
        // de onderrand, wat op sommige browsers/zoomniveaus alsnog een fractie afknipt.
        var available = window.innerHeight - cardTop - theadHeight - tfootHeight - footerHeight - 16;
        var rows = Math.floor(available / (rowHeight || 52));
        rows = Math.max(3, Math.min(25, rows || 10));

        if (rows !== table.page.len()) {
            table.page.len(rows).draw();
        }
    }
    adjustPageLength();
    var resizeTimer = null;
    window.addEventListener("resize", function () {
        window.clearTimeout(resizeTimer);
        resizeTimer = window.setTimeout(function () {
            adjustPageLength();
            renderMobileCards();
        }, 150);
    });

    // ── Uitklapbare contracten (design-handoff 14a, "Fluvius System Operator — 3 contracten") ──────
    // Elk contract van de rij staat al als JSON op data-contracts (server-side geserialiseerd, zelfde
    // velden als de detailkolommen zelf). table.row(tr).child(...) — DataTables' eigen mechanisme
    // voor een rij die NIET meetelt in paginering/sortering/zoeken, i.p.v. losse <tr>'s rechtstreeks
    // in de DOM te knopen (dat zou de tabel se eigen rijentelling — en dus "rijen per pagina" —
    // stil laten afwijken van wat er echt op het scherm staat).
    function buildChildRows(contracts) {
        var rows = contracts.map(function (c) {
            var statusBadge = c.signed
                ? '<span class="gl-v2-badge is-positive">GETEKEND</span>'
                : '<span class="gl-v2-badge is-attention">NIET GETEKEND</span>';
            var guaranteeWarning = c.guaranteeDocMissing
                ? '<i class="ph ph-warning-circle gl-v2-pc-warning-icon" title="Waarborgdocument nog te ontvangen" aria-hidden="true"></i>'
                : '';
            var follow = '<span class="gl-v2-pc-follow">' +
                '<span class="gl-v2-pc-follow-icon ' + (c.siteNotification ? 'is-ok' : 'is-missing') + '" title="Werfmelding — ' + (c.siteNotification ? 'in orde' : 'ontbreekt') + '"><i class="ph ph-hard-hat" aria-hidden="true"></i></span>' +
                '<span class="gl-v2-pc-follow-icon ' + (c.vgmCharter ? 'is-ok' : 'is-missing') + '" title="VGM-charter — ' + (c.vgmCharter ? 'in orde' : 'ontbreekt') + '"><i class="ph ph-shield-check" aria-hidden="true"></i></span>' +
                '</span>';
            return '<div class="gl-v2-pc-subrow" data-detail-url="' + c.editUrl + '">' +
                '<span class="gl-v2-pc-subrow-name">' + c.name + '</span>' +
                '<span>' + statusBadge + '</span>' +
                '<span class="text-end">' + c.price + '</span>' +
                '<span>' + c.paymentTerm + '</span>' +
                '<span>' + c.guarantee + guaranteeWarning + '</span>' +
                '<span>' + c.sent + '</span>' +
                follow +
                '</div>';
        }).join("");
        return '<div class="gl-v2-pc-subrows">' + rows + '</div>';
    }

    document.addEventListener("click", function (e) {
        var toggle = e.target.closest(".js-gl-v2-pc-toggle");
        if (!toggle) return;
        e.preventDefault();
        e.stopPropagation();
        var tr = toggle.closest("tr");
        var row = table.row(tr);
        var contractsJson = tr.getAttribute("data-contracts");
        if (!contractsJson) return;

        if (row.child.isShown()) {
            row.child.hide();
            toggle.setAttribute("aria-expanded", "false");
            tr.classList.remove("is-expanded");
        } else {
            row.child(buildChildRows(JSON.parse(contractsJson))).show();
            toggle.setAttribute("aria-expanded", "true");
            tr.classList.add("is-expanded");
        }
    });

    // Bewerken-link in een uitgeklapt subrij navigeert net als een gewone tabelrij — geen aparte
    // click-handler nodig buiten deze ene regel, .gl-v2-pc-subrow[data-detail-url] is geen <tr> dus
    // valt buiten de shell-brede initClickableRows() (die enkel op tr[data-detail-url] mikt).
    document.addEventListener("click", function (e) {
        if (e.target.closest("a, button")) return;
        var subrow = e.target.closest(".gl-v2-pc-subrow[data-detail-url]");
        if (subrow) window.location.href = subrow.getAttribute("data-detail-url");
    });

    // ── Statusfilter (tabbar) ────────────────────────────────────────────────────────────────────
    var selectedStatus = "";
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        if (settings.nTable !== tableElement) return true;
        if (!selectedStatus) return true;
        var row = settings.aoData[dataIndex]?.nTr;
        if (!row) return true;
        var rowStatuses = (row.dataset.status || "").split(",").filter(Boolean);
        return rowStatuses.includes(selectedStatus);
    });

    var statusTabs = document.querySelectorAll("#gl-v2-pc-status-tabs .gl-v2-tabbar-tab");
    statusTabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            statusTabs.forEach(function (t) { t.classList.remove("is-active"); t.setAttribute("aria-selected", "false"); });
            tab.classList.add("is-active");
            tab.setAttribute("aria-selected", "true");
            selectedStatus = tab.getAttribute("data-status") || "";
            table.draw();
        });
    });

    // ── Zoekveld ─────────────────────────────────────────────────────────────────────────────────
    var searchInput = document.getElementById("search-term");
    if (searchInput) {
        searchInput.addEventListener("input", function () { table.search(searchInput.value).draw(); });
        var clearBtn = document.getElementById("search-term-clear");
        if (clearBtn) clearBtn.addEventListener("click", function () { searchInput.value = ""; table.search("").draw(); });
    }
})();
