// gl-v2 layout-pilot — Facturen (verkoop) detailpagina. Enkel geladen door
// Views/Invoices/DetailV2.cshtml. De ⋯-menu (topbar + mobiele topbar-plek) is de generieke
// .js-gl-v2-menu-trigger/.gl-v2-menu/initContextMenus() uit gl-v2-shell.js — geen page-eigen JS
// nodig daarvoor. Hier enkel de verwijder-bevestigingsmodal, zelfde AJAX-fragment-in-lege-
// .modal-content-shell-recept als Views/Invoices/IndexV2.cshtml (gl-v2-invoices.js), maar niet
// vanuit dat bestand hergebruikt: die laadt ook DataTables/mobiele-kaart-rendering die deze pagina
// niet heeft, dus een eigen klein bestand i.p.v. de zwaardere IndexV2-file mee te slepen.
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
})();
