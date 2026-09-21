// gl-v2 layout-pilot — Facturen-BCO pagina-specifieke JS (design-handoff optie 4a). Enkel geladen
// door Views/Invoices/IndexV2.cshtml, niet elders. Grotendeels dezelfde logica als de bestaande
// Views/Invoices/Index.cshtml-pagina (DataTable-init, boeken-checkboxes, nummeren-modal) — enkel
// verplaatst naar een los bestand, geen gedragswijziging daar. De verwijder-modal is sinds optie
// 4j wél bewust anders: gl-v2's eigen Type 1-bevestigingscomponent (Bootstrap-modal) i.p.v.
// Index.cshtml's magnific-popup/.modal-block. De server-bepaalde waarden (delete-URL naar
// ModalDeleteV2, standaard boekjaar) komen binnen via window.glV2InvoicesConfig, gezet in een
// klein inline scriptje in IndexV2.cshtml vlak voor deze file geladen wordt.
(function () {
    "use strict";
    var config = window.glV2InvoicesConfig || {};
    var entity = { singular: "factuur", plural: "facturen" };

    // Optie 4j TYPE 1 (danger) — gewone Bootstrap-modalinstantie i.p.v. magnific-popup/.modal-block
    // (zie Index.cshtml voor de oude, niet-gl-v2-versie): #delete-invoice-container IS hier de
    // .modal-content zelf (IndexV2.cshtml), de AJAX-respons (Modals/_ModalDeleteInvoiceV2.cshtml)
    // levert enkel .modal-body/.modal-footer als kinderen. Zelfde backdrop:"static",keyboard:false
    // als issueModal hieronder — bewust geen klik-buiten/Esc-dismiss op een bevestigingsmodal,
    // enkel de knoppen zelf.
    var deleteModalElement = document.getElementById("deleteInvoiceConfirmModal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    $(document).on("click", ".deleteInvoice", function (ev) {
        ev.preventDefault();
        var id = $(this).data("id");
        var issuerId = $(this).data("issuer");
        $("#delete-invoice-container").html('<div class="p-3 text-muted">Laden…</div>');
        $.get(config.deleteUrl, { id: id, issuerCompanyId: issuerId })
            .done(function (html) { $("#delete-invoice-container").html(html); })
            .fail(function () { $("#delete-invoice-container").html('<div class="p-3 text-danger">Kon factuur niet laden.</div>'); });

        if (deleteModal) deleteModal.show();
    });

    var processingModalElement = document.getElementById("invoiceProcessingModal");
    var processingModal = processingModalElement
        ? new bootstrap.Modal(processingModalElement, { backdrop: "static", keyboard: false })
        : null;
    var issueModalElement = document.getElementById("issueInvoiceConfirmModal");
    var issueModal = issueModalElement
        ? new bootstrap.Modal(issueModalElement, { backdrop: "static", keyboard: false })
        : null;
    var issueTarget = null;

    function showProcessingModal(message) {
        if (!processingModal) return;
        $("#invoiceProcessingMessage").text(message || "Factuur wordt verwerkt...");
        processingModal.show();
    }

    $(document).on("click", ".js-issue-invoice", function (ev) {
        ev.preventDefault();
        issueTarget = $(this);
        if (issueModal) {
            issueModal.show();
        } else if (issueTarget) {
            var href = issueTarget.attr("href");
            if (href) { showProcessingModal(issueTarget.data("processing-message")); window.location = href; }
        }
    });

    $("#confirmIssueInvoice").on("click", function () {
        if (!issueTarget) return;
        var href = issueTarget.attr("href");
        var message = issueTarget.data("processing-message");
        if (issueModal) issueModal.hide();
        showProcessingModal(message);
        if (href) window.location = href;
    });

    $(document).on("click", ".js-invoice-processing-link", function (ev) {
        ev.preventDefault();
        var message = $(this).data("processing-message");
        var href = $(this).attr("href");
        showProcessingModal(message);
        if (href) window.location = href;
    });

    $(document).on("submit", ".js-invoice-processing-form", function () {
        showProcessingModal($(this).data("processing-message"));
    });

    function getBookingCheckboxes() {
        return $('.invoice-book-checkbox[data-bookable="true"]')
            .toArray()
            .sort(function (a, b) {
                var seqA = parseInt($(a).data("seq") || 0, 10);
                var seqB = parseInt($(b).data("seq") || 0, 10);
                return seqA - seqB;
            })
            .map(function (cb) { return $(cb); });
    }

    function syncBookButton() {
        var checkedCount = $('.invoice-book-checkbox[data-bookable="true"]:checked:enabled').length;
        var hasSelection = checkedCount > 0;
        // .js-book-invoices-btn: shared by the topbar button (desktop), the mobile quick-actions
        // tile, and the selection-toolbar button (IndexV2.cshtml) — a class on three elements, not
        // three ids, since duplicate ids would be invalid HTML and would only let one of the three
        // respond to this toggle.
        $(".js-book-invoices-btn").prop("disabled", !hasSelection);

        // Optie 4e selectie-toolbar: enkel zichtbaar met een selectie, telt exact dezelfde
        // checkboxes als hierboven (geen apart selectiemodel, dit IS de boeken-selectie).
        $("#gl-v2-selection-toolbar").prop("hidden", !hasSelection);
        $("#gl-v2-selection-toolbar-count").text(
            checkedCount + " " + (checkedCount === 1 ? entity.singular : entity.plural) + " geselecteerd"
        );
    }

    // Optie 4e GESELECTEERD-rijstaat: elke <tr> met een aangevinkte boeken-checkbox krijgt
    // .is-selected (tint + inset-rand, zie gl-v2-invoices.css) — geen los selectiemodel, gewoon
    // dezelfde checkbox-state ook zichtbaar op de rij zelf i.p.v. enkel op de checkbox. Aangevinkt
    // + disabled (al geboekt) is GEEN actieve selectie — de checkbox blijft zelf wel groen tonen
    // ("dit is geboekt"), maar die rij krijgt de default rijstaat, geen tint/streep, anders lijkt
    // elke al-geboekte factuur permanent "actief geselecteerd".
    function syncSelectedRows() {
        $(".invoice-book-checkbox").each(function () {
            $(this).closest("tr").toggleClass("is-selected", this.checked && !this.disabled);
        });
    }

    // Optie 4c: houdt de proxy-checkboxes op de mobiele kaarten (renderMobileCards() verderop) in
    // sync met de echte checkboxes hierboven — draait mee in elke applyBookingRules()-aanroep
    // (incl. de cascaderende "alles tot en met deze"-selectie), zodat een tik op één kaart ook de
    // andere kaarten bijwerkt zonder de hele mobiele lijst opnieuw op te bouwen. Geen effect als de
    // kaarten nog niet bestaan (desktop/tablet, of vóór de eerste renderMobileCards()-aanroep) —
    // .gl-v2-invoice-card selecteert dan gewoon niets.
    function syncMobileCardCheckboxStates() {
        $(".gl-v2-invoice-card").each(function () {
            var $card = $(this);
            var $src = $('.invoice-book-checkbox[value="' + $card.attr("data-invoice-id") + '"]');
            if (!$src.length) return;
            var checked = $src.is(":checked");
            var disabled = $src.is(":disabled");
            $card.find(".gl-v2-mc-checkbox").toggleClass("is-checked", checked).prop("disabled", disabled);
            $card.toggleClass("is-selected", checked && !disabled);
        });
    }

    function applyBookingRules() {
        getBookingCheckboxes().forEach(function ($cb) {
            if ($cb.data("booked")) return;
            $cb.prop("disabled", false);
        });
        syncBookButton();
        syncSelectedRows();
        syncMobileCardCheckboxStates();
    }

    $(document).on("change", ".invoice-book-checkbox", function () {
        var $current = $(this);
        if ($current.data("booked") || !$current.data("bookable")) return;

        var currentSeq = parseInt($current.data("seq") || 0, 10);
        var allBookable = getBookingCheckboxes();

        if (this.checked) {
            allBookable.forEach(function ($cb) {
                var seq = parseInt($cb.data("seq") || 0, 10);
                if (seq <= currentSeq && !$cb.data("booked")) $cb.prop("checked", true);
            });
        } else {
            allBookable.forEach(function ($cb) {
                var seq = parseInt($cb.data("seq") || 0, 10);
                if (seq > currentSeq) $cb.prop("checked", false);
            });
        }
        applyBookingRules();
    });

    // Optie 4f "LEGE STAAT" — client-side variant: er bestaan wel facturen, maar de huidige
    // zoekterm/boekjaarfilter levert nul rijen op. DataTables rendert language.zeroRecords als
    // HTML (niet als platte tekst), dus dezelfde .gl-v2-empty-state-opmaak als de server-side
    // variant in IndexV2.cshtml kan hier gewoon als string opgebouwd worden. "Filters wissen"
    // krijgt zijn gedrag verderop via een gedelegeerde click-handler (het element bestaat immers
    // enkel zolang er nul resultaten zijn — DataTables verwijdert 'm weer zodra dat niet meer zo is).
    function buildEmptyStateHtml() {
        var actions = '<button type="button" class="gl-v2-btn gl-v2-btn-secondary js-gl-v2-clear-filters">Filters wissen</button>';
        if (config.canWrite && config.createUrl) {
            actions += '<a href="' + config.createUrl + '" class="gl-v2-btn gl-v2-btn-primary">+ Nieuwe ' + entity.singular + '</a>';
        }
        return '<div class="gl-v2-empty-state gl-v2-empty-state-inline">' +
            '<span class="gl-v2-empty-state-icon"><i class="ph ph-file" aria-hidden="true"></i></span>' +
            '<h2 class="gl-v2-empty-state-title">Geen ' + entity.plural + ' gevonden</h2>' +
            '<p class="gl-v2-empty-state-desc">Er zijn geen ' + entity.plural + ' die aan deze filters voldoen. Pas de filters aan of maak een nieuwe ' + entity.singular + '.</p>' +
            '<div class="gl-v2-empty-state-actions">' + actions + '</div>' +
        '</div>';
    }

    function resetFilters() {
        $("#search-term").val("");
        table.search("").draw();

        // bookyearPanel/-Trigger/-Label worden pas verderop toegekend (var-hoisting) — resetFilters
        // zelf wordt enkel later, via een klik, uitgevoerd, dus zijn ze dan al gezet.
        var defaultValue = config.defaultBookyear || "";
        if (bookyearPanel) {
            var matched = null;
            bookyearPanel.querySelectorAll(".gl-v2-select-option").forEach(function (o) {
                var isMatch = (o.getAttribute("data-value") || "") === defaultValue;
                o.classList.toggle("is-selected", isMatch);
                if (isMatch) matched = o;
            });
            if (matched && bookyearLabel) bookyearLabel.textContent = matched.getAttribute("data-label") || "";
            if (bookyearTrigger) bookyearTrigger.classList.toggle("is-filled", defaultValue !== "");
        }
        applyBookyearFilter(defaultValue);
    }
    $(document).on("click", ".js-gl-v2-clear-filters", resetFilters);

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
            // Optie 4a: pagination-pijltjes als kale chevrons i.p.v. de "Previous"/"Next"-tekst.
            paginate: { previous: "‹", next: "›" },
            aria: {
                sortAscending: ": activeer om oplopend te sorteren",
                sortDescending: ": activeer om aflopend te sorteren"
            }
        };
    }

    // Optie 4c "Mobiel — facturen als kaarten": #gl-v2-mobile-invoice-list (IndexV2.cshtml, na de
    // tabel) is een lege container, gevuld door renderMobileCards() hieronder telkens de tabel
    // opnieuw tekent (zoeken/sorteren/pagineren/init, zie table.on("draw", ...) verderop) — dus
    // automatisch in sync met wat er op de huidige pagina staat, zonder die filter/sorteer-logica
    // hier te moeten overdoen. Checkbox en "···"-acties blijven hun ÉÉN echte exemplaar in de
    // (nu <768px onzichtbare) tabelrij: de kaart krijgt een PROXY-checkbox die een klik doorstuurt
    // naar de echte (geen dubbele name="invoiceIds", geen dubbel-tellende selectie) en een
    // GEKLOONDE kopie van het bestaande "···"-paneel — de gedelegeerde click-handlers verderop
    // (.deleteInvoice, .js-issue-invoice, .gl-v2-row-menu-trigger) matchen op klasse, niet op het
    // originele element, en werken dus ook op de kloon.
    var $mobileList = $("#gl-v2-mobile-invoice-list");

    function buildMobileCard(tr) {
        var $tr = $(tr);
        var $cells = $tr.children("td");
        var nr = $.trim($cells.eq(2).text()) || " ";
        var date = $.trim($cells.eq(3).text());
        var client = $.trim($cells.eq(4).text());
        var total = $.trim($cells.eq(5).text());
        var statusHtml = $cells.eq(7).html();

        var $srcCheckbox = $tr.find(".invoice-book-checkbox");
        var invoiceId = $srcCheckbox.val();

        // Optie 4j type 3: titel bovenaan de mobiele sheet ("Factuur {nr}") — het tablet/desktop-
        // paneel heeft die niet (geen ruimte/noodzaak in een klein zwevend paneel), dus enkel hier
        // toegevoegd, niet iets dat al op het geklonede paneel stond.
        var $srcMenu = $tr.find(".gl-v2-row-menu");
        var mobileMenuId = "gl-v2-row-menu-m-" + invoiceId;
        var $menuClone = $srcMenu.clone().attr("id", mobileMenuId);
        $menuClone.prepend($('<div class="gl-v2-row-menu-title"></div>').text("Factuur " + nr));

        var $card = $(
            '<div class="gl-v2-invoice-card">' +
                '<button type="button" class="gl-v2-mc-checkbox" aria-label="Boeken"></button>' +
                '<div class="gl-v2-mc-body">' +
                    '<div class="gl-v2-mc-row"><span class="gl-v2-mc-nr"></span><span class="gl-v2-mc-status"></span></div>' +
                    '<div class="gl-v2-mc-row"><span class="gl-v2-mc-client"></span><span class="gl-v2-mc-total"></span></div>' +
                    '<div class="gl-v2-mc-row gl-v2-mc-row-date"><span class="gl-v2-mc-date"></span></div>' +
                '</div>' +
                '<button type="button" class="gl-v2-row-menu-trigger" aria-haspopup="true" aria-expanded="false" aria-label="Meer acties"><i class="ph ph-dots-three" aria-hidden="true"></i></button>' +
            '</div>'
        );

        $card.attr("data-invoice-id", invoiceId);
        $card.find(".gl-v2-mc-nr").text(nr);
        $card.find(".gl-v2-mc-status").html(statusHtml);
        $card.find(".gl-v2-mc-client").text(client).attr("title", client);
        $card.find(".gl-v2-mc-total").text(total);
        $card.find(".gl-v2-mc-date").text(date);
        $card.find(".gl-v2-row-menu-trigger").attr("aria-controls", mobileMenuId);
        $card.append($menuClone);

        $card.find(".gl-v2-mc-checkbox").on("click", function () {
            if (this.disabled) return;
            $srcCheckbox.prop("checked", !$srcCheckbox.prop("checked")).trigger("change");
        });

        return $card;
    }

    function renderMobileCards() {
        if (window.innerWidth >= 768 || !$mobileList.length) return;
        $mobileList.empty();
        $("#datatable-invoice-list").children("tbody").children("tr").each(function () {
            $mobileList.append(buildMobileCard(this));
        });
        syncMobileCardCheckboxStates();
    }

    var table = new DataTable("#datatable-invoice-list", {
        order: [[2, "desc"]],
        autoWidth: false,
        language: buildLanguage(entity),
        layout: { topStart: { buttons: [] } },
        columnDefs: [
            { targets: 10, orderable: false, searchable: false, width: "1%" },
            { targets: 11, visible: false },
            { targets: [0, 1, 2, 4, 5, 6, 7, 8, 9, 10], width: "1%" },
            { targets: 4, width: "100%" }
        ],
        // Optie 4a "10 van 42 facturen" — een aantal-op-deze-pagina/totaal, geen bereik
        // (_START_ tot _END_). infoCallback wint van language.info/infoEmpty hierboven.
        infoCallback: function (settings, start, end, max, total) {
            if (total === 0) return "0 " + entity.plural;
            var shown = end - start + 1;
            return shown + " van " + total + " " + (total === 1 ? entity.singular : entity.plural);
        }
    });
    table.on("draw", applyBookingRules);
    table.on("draw", renderMobileCards);
    applyBookingRules();

    // Optie 4c: kaarten hebben een variabele/hogere hoogte dan de 54px vaste tabelrij hieronder,
    // dus geen zin om zoals optie 4a exact de resterende schermhoogte te vullen — de mobiele
    // kaartenlijst is geen eigen scrollcontainer meer (de tabel zelf staat op display:none), de
    // pagina scrollt er gewoon overheen zoals een gewone lijst. Gewoon een ruime vaste
    // paginagrootte i.p.v. een tweede hoogte-gok. Moet vóór de eerste
    // syncTablePageLength()-aanroep hieronder gedeclareerd staan — anders is deze nog undefined op
    // het moment dat de functie voor het eerst effectief draait.
    var MOBILE_PAGE_LENGTH = 15;

    // Optie 4a: de tabel neemt altijd de volledige beschikbare hoogte in i.p.v. een vaste
    // pagina-grootte (10/25/50) — het aantal rijen per pagina wordt berekend uit de echte
    // beschikbare hoogte, niet omgekeerd. Zie syncTablePageLength() verderop.
    syncTablePageLength();
    renderMobileCards();

    // Optie 4f "LADEN": de skeleton (zichtbaar sinds de eerste paint, zie gl-v2-invoices.css) maakt
    // hier plaats voor de echte, nu correct gepagineerde tabel — pas NA de eerste
    // syncTablePageLength() zodat er geen tussenstap zichtbaar is met het verkeerde rijaantal.
    $(".gl-v2-table-card").addClass("is-ready");
    var pageLengthResizeTimer = null;
    $(window).on("resize", function () {
        window.clearTimeout(pageLengthResizeTimer);
        pageLengthResizeTimer = window.setTimeout(function () {
            // syncTablePageLength() roept zelf al table.draw() (→ renderMobileCards() via
            // table.on("draw", ...) hierboven) zodra de paginagrootte verandert — maar bij het
            // kruisen van de 768px-grens kan die toevallig al hetzelfde getal zijn (bv. mobiel
            // stond al op MOBILE_PAGE_LENGTH), dan vuurt er geen "draw" en zou de kaartenlijst niet
            // meebewegen. renderMobileCards() zelf bevat al een width-guard, dus een extra aanroep
            // hier is altijd veilig (no-op op desktop/tablet).
            syncTablePageLength();
            renderMobileCards();
        }, 150);
    });

    function syncTablePageLength() {
        if (window.innerWidth < 768) {
            if (table.page.len() !== MOBILE_PAGE_LENGTH) table.page.len(MOBILE_PAGE_LENGTH).draw(false);
            return;
        }

        var cardEl = document.querySelector(".gl-v2-table-card");
        var theadEl = document.querySelector("#datatable-invoice-list thead");
        var dtContainer = document.querySelector("#datatable-invoice-list")?.closest(".dt-container");
        if (!cardEl || !theadEl || !dtContainer) return;

        var ROW_HEIGHT = 54; // moet in sync blijven met gl-v2-invoices.css tbody td { height }
        // Altijd gereserveerd, ook als de selectie-toolbar nu net niet zichtbaar is — anders
        // verspringt het aantal rijen per pagina elke keer je een factuur aan-/uitvinkt.
        var SELECTION_TOOLBAR_HEIGHT = 56;

        var footerRow = dtContainer.querySelector(".dt-layout-row:last-child");
        var footerHeight = footerRow ? footerRow.offsetHeight : 48;

        var available = cardEl.clientHeight - theadEl.offsetHeight - footerHeight - SELECTION_TOOLBAR_HEIGHT;
        var rows = Math.max(Math.floor(available / ROW_HEIGHT), 3);

        if (rows !== table.page.len()) {
            table.page.len(rows).draw(false);
        }
    }

    $(".buttons-excel").hide();
    $(".buttons-pdf").hide();
    $(".dt-search").hide();

    $("#btnExportExcel").on("click", function () { $(".buttons-excel").trigger("click"); });
    $("#btnExportPdf").on("click", function () { $(".buttons-pdf").trigger("click"); });

    $("#search-term").on("keyup", function () {
        table.search($(this).val()).draw();
    });

    // Optie 4i "×"-knop: enkel getoond zolang het veld inhoud heeft (CSS, :placeholder-shown, zie
    // gl-v2-shell.css) — hier enkel de klik zelf afhandelen, focus teruggeven aan het veld zoals
    // een gebruiker zou verwachten na "wissen".
    $("#search-term-clear").on("click", function () {
        $("#search-term").val("").trigger("focus");
        table.search("").draw();
    });

    var bookyearColumnIndex = 11;
    var defaultBookyear = config.defaultBookyear || "";

    function applyBookyearFilter(value) {
        table.column(bookyearColumnIndex).search(value || "").draw();
    }

    // Optie 4h: eigen trigger+paneel i.p.v. een <select> — een native <select> tekent z'n open
    // lijst zelf, buiten CSS-bereik, dus kan de referentiesheet se paneelstijl nooit tonen. Zelfde
    // trigger+JS-gepositioneerd-paneel-patroon als de rail-flyouts/rij-···-menu's. Op mobiel
    // (gl-v2-shell.css, <768px) wordt hetzelfde paneel via CSS een bottom sheet i.p.v. een zwevend
    // paneel — positionBookyearPanel() slaat de JS-positie dan gewoon over, CSS zet 'm vast onderaan.
    var bookyearTrigger = document.getElementById("bookyear-select-trigger");
    var bookyearPanel = document.getElementById("bookyear-select-panel");
    var bookyearBackdrop = document.getElementById("bookyear-select-backdrop");
    var bookyearLabel = document.getElementById("bookyear-select-label");
    var bookyearClose = document.getElementById("bookyear-select-close");

    if (bookyearTrigger && bookyearPanel) {
        var closeBookyearSelect = function () {
            bookyearPanel.classList.remove("is-open");
            if (bookyearBackdrop) bookyearBackdrop.classList.remove("is-open");
            bookyearTrigger.classList.remove("is-open");
            bookyearTrigger.setAttribute("aria-expanded", "false");
        };

        var positionBookyearPanel = function () {
            if (window.innerWidth < 768) return;
            var rect = bookyearTrigger.getBoundingClientRect();
            var panelWidth = bookyearPanel.offsetWidth || 240;
            var left = Math.min(rect.left, window.innerWidth - panelWidth - 12);
            bookyearPanel.style.left = Math.max(12, left) + "px";
            var top = rect.bottom + 6;
            var maxTop = window.innerHeight - bookyearPanel.offsetHeight - 12;
            bookyearPanel.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        };

        var openBookyearSelect = function () {
            bookyearPanel.classList.add("is-open");
            if (bookyearBackdrop) bookyearBackdrop.classList.add("is-open");
            bookyearTrigger.classList.add("is-open");
            bookyearTrigger.setAttribute("aria-expanded", "true");
            positionBookyearPanel();
        };

        bookyearTrigger.addEventListener("click", function (e) {
            e.preventDefault();
            if (bookyearPanel.classList.contains("is-open")) closeBookyearSelect();
            else openBookyearSelect();
        });

        if (bookyearBackdrop) bookyearBackdrop.addEventListener("click", closeBookyearSelect);
        if (bookyearClose) bookyearClose.addEventListener("click", closeBookyearSelect);

        bookyearPanel.querySelectorAll(".gl-v2-select-option").forEach(function (option) {
            option.addEventListener("click", function () {
                var value = option.getAttribute("data-value") || "";
                var label = option.getAttribute("data-label") || "";
                bookyearPanel.querySelectorAll(".gl-v2-select-option").forEach(function (o) {
                    o.classList.toggle("is-selected", o === option);
                });
                bookyearLabel.textContent = label;
                bookyearTrigger.classList.toggle("is-filled", value !== "");
                closeBookyearSelect();
                applyBookyearFilter(value);
            });
        });

        document.addEventListener("click", function (e) {
            if (!bookyearPanel.classList.contains("is-open")) return;
            if (e.target.closest("#bookyear-select")) return;
            closeBookyearSelect();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && bookyearPanel.classList.contains("is-open")) closeBookyearSelect();
        });
        window.addEventListener("resize", function () {
            if (bookyearPanel.classList.contains("is-open")) positionBookyearPanel();
        });
    }

    if (defaultBookyear) applyBookyearFilter(defaultBookyear);

    // Optie 4f "TABLET — ···-MENU IN DE RIJ": zelfde trigger+JS-gepositioneerd-paneel-patroon als
    // de rail-flyouts in gl-v2-shell.js (closeAll/positionFlyout daar ↔ closeAllRowMenus/
    // positionRowMenu hier) — enkel actief <1024px via gl-v2-invoices.css, deze JS doet verder
    // niets anders op desktop/mobiel (de trigger is daar display:none, dus krijgt nooit een klik).
    var rowMenuCloseTimer = null;
    var rowMenuBackdrop = document.getElementById("gl-v2-row-menu-backdrop");

    function closeAllRowMenus() {
        $(".gl-v2-row-menu.is-open").each(function () {
            $(this).removeClass("is-open");
            $('.gl-v2-row-menu-trigger[aria-controls="' + this.id + '"]')
                .removeClass("is-menu-open")
                .attr("aria-expanded", "false");
        });
        if (rowMenuBackdrop) rowMenuBackdrop.classList.remove("is-open");
    }

    // Optie 4j type 3: <768px wordt het paneel een bottom sheet (CSS pint 'm vast onderaan) —
    // zelfde "sla de JS-positionering gewoon over" patroon als positionBookyearPanel() hierboven,
    // anders zou een hier gezette inline top/left de CSS-positie (die geen !important gebruikt)
    // overstemmen.
    function positionRowMenu(trigger, menu) {
        if (window.innerWidth < 768) return;
        var rect = trigger.getBoundingClientRect();
        var menuWidth = menu.offsetWidth || 212;
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

    // Een actie kiezen (navigatie, of een modal zoals .deleteInvoice/.js-issue-invoice hierboven
    // opent) sluit het paneel meteen mee — anders blijft het zichtbaar staan achter de modal.
    $(document).on("click", ".gl-v2-row-menu .gl-v2-row-action", closeAllRowMenus);

    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-row-menu") || e.target.closest(".gl-v2-row-menu-trigger")) return;
        closeAllRowMenus();
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closeAllRowMenus();
    });
    $(window).on("resize", function () {
        window.clearTimeout(rowMenuCloseTimer);
        rowMenuCloseTimer = window.setTimeout(closeAllRowMenus, 100);
    });
})();
