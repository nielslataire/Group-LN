// gl-v2 layout-pilot — Facturen (verkoop) detailpagina. Enkel geladen door
// Views/Invoices/DetailV2.cshtml. De ⋯-menu (topbar) is de generieke .js-gl-v2-menu-trigger/
// .gl-v2-menu/initContextMenus() uit gl-v2-shell.js — geen page-eigen JS nodig daarvoor. Hier: de
// verwijder-bevestigingsmodal (zelfde AJAX-fragment-in-lege-.modal-content-shell-recept als
// Views/Invoices/IndexV2.cshtml, gl-v2-invoices.js — niet vanuit dat bestand hergebruikt, dat laadt
// ook DataTables/mobiele-kaart-rendering die deze pagina niet heeft) en de inklap-toggle voor Onze
// onderneming/Klant (enkel <768px zichtbaar effect, zie gl-v2-invoices-detail.css) — het eerste echte
// gebruik van .gl-v2-section-card.is-collapsible/[aria-expanded], het CSS-mechanisme bestond al
// project-breed maar had nog "de klik-toggle is ongewired JS" staan (gl-v2-shell.css se eigen
// toelichting).
(function () {
    "use strict";
    var config = window.glV2InvoiceDetailConfig || {};

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

    function toggleCollapsible(header) {
        var expanded = header.getAttribute("aria-expanded") === "true";
        header.setAttribute("aria-expanded", (!expanded).toString());
    }
    document.querySelectorAll(".gl-v2-section-card.is-collapsible > .gl-v2-section-card-header").forEach(function (header) {
        header.addEventListener("click", function () { toggleCollapsible(header); });
        header.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                toggleCollapsible(header);
            }
        });
    });
})();
