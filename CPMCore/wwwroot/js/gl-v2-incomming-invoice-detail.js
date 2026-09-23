// gl-v2 layout-pilot — Inkomende (aankoop)factuur detailpagina. Enkel geladen door
// Views/Projecten/IncommingInvoiceDetailV2.cshtml. De ⋯-menu is de generieke .js-gl-v2-menu-
// trigger/.gl-v2-menu/initContextMenus() uit gl-v2-shell.js — geen page-eigen JS nodig daarvoor.
// Hier: de verwijder-bevestigingsmodal (zelfde AJAX-fragment-in-lege-.modal-content-shell-recept
// als Invoices/DetailV2.cshtml) en de inklap-toggle voor Leverancier/Documentgegevens (zelfde
// klein, page-eigen stukje als gl-v2-invoices-detail.js — niet gedeeld, zelfde discipline als de
// rest van gl-v2).
(function () {
    "use strict";
    var config = window.glV2IncommingInvoiceDetailConfig || {};

    var deleteModalElement = document.getElementById("deleteIncommingInvoiceConfirmModal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    $(document).on("click", ".deleteIncommingInvoice", function (ev) {
        ev.preventDefault();
        var id = $(this).data("id");
        var companyName = $(this).data("company");
        $("#delete-incomming-invoice-container").html('<div class="p-3 text-muted">Laden…</div>');
        $.get(config.deleteUrl, { id: id, companyname: companyName })
            .done(function (html) { $("#delete-incomming-invoice-container").html(html); })
            .fail(function () { $("#delete-incomming-invoice-container").html('<div class="p-3 text-danger">Kon factuur niet laden.</div>'); });

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
