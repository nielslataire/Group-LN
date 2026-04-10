'use strict';

(function () {
    const form = document.getElementById('coordInvoiceForm');
    const btn  = document.getElementById('btnCoordInvoice');
    if (!form || !btn) return;

    function hasSelection() {
        return document.querySelector('.chkCoordSlice:checked') !== null;
    }

    function updateBtnState() {
        btn.disabled = !hasSelection();
    }

    // Luister op alle checkbox-wijzigingen in het document (ook in partials).
    document.addEventListener('change', function (e) {
        if (e.target.classList.contains('chkCoordSlice')) {
            updateBtnState();
        }
    });

    form.addEventListener('submit', function (e) {
        if (!hasSelection()) {
            e.preventDefault();
            return;
        }

        // Dubbele submit blokkeren.
        if (form.dataset.submitting === 'true') {
            e.preventDefault();
            return;
        }
        form.dataset.submitting = 'true';
        btn.disabled = true;

        // Loading modal openen.
        var modalEl = document.getElementById('coordInvoiceBusyModal');
        if (modalEl && typeof bootstrap !== 'undefined') {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    });

    // Initiële staat bij laden.
    updateBtnState();
})();
