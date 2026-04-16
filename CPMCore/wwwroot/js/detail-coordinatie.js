'use strict';

(function () {
    const form           = document.getElementById('coordInvoiceForm');
    const btnInvoiceTop  = document.getElementById('btnCoordInvoice');
    const selectionCard  = document.getElementById('sliceSelectionCard');
    const selectionCount = document.getElementById('selectionCount');
    const selectionTotal = document.getElementById('selectionTotal');
    const btnWis         = document.getElementById('btnWisSelectie');
    const btnCardSubmit  = document.getElementById('btnCoordInvoiceCard');
    const chkSelectAll   = document.getElementById('chkSelectAll');

    function getCheckedBoxes() {
        return Array.from(document.querySelectorAll('.chkCoordSlice:checked'));
    }

    function formatEuro(amount) {
        return '€ ' + amount.toLocaleString('nl-BE', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
    }

    function updateUI() {
        const checked = getCheckedBoxes();
        const hasSelection = checked.length > 0;

        // Totaalbedrag geselecteerde schijven
        const total = checked.reduce(function (sum, cb) {
            return sum + (parseFloat(cb.dataset.amount) || 0);
        }, 0);

        // Top-knop
        if (btnInvoiceTop) btnInvoiceTop.disabled = !hasSelection;

        // Kaart-knop
        if (btnCardSubmit) btnCardSubmit.disabled = !hasSelection;

        // Selectiekaart tonen/verbergen
        if (selectionCard) {
            if (hasSelection) {
                selectionCard.classList.remove('d-none');
                selectionCard.classList.add('d-flex');
            } else {
                selectionCard.classList.add('d-none');
                selectionCard.classList.remove('d-flex');
            }
        }

        if (selectionCount) selectionCount.textContent = checked.length;
        if (selectionTotal) selectionTotal.textContent  = formatEuro(total);

        // Header checkbox synchroniseren
        if (chkSelectAll) {
            const allBoxes = document.querySelectorAll('.chkCoordSlice');
            chkSelectAll.checked       = hasSelection && checked.length === allBoxes.length;
            chkSelectAll.indeterminate = hasSelection && checked.length < allBoxes.length;
        }
    }

    // Luister op alle checkbox-wijzigingen
    document.addEventListener('change', function (e) {
        if (e.target.classList.contains('chkCoordSlice')) {
            updateUI();
        }
    });

    // Header checkbox: selecteer/deselecteer alles
    if (chkSelectAll) {
        chkSelectAll.addEventListener('change', function () {
            document.querySelectorAll('.chkCoordSlice').forEach(function (cb) {
                cb.checked = chkSelectAll.checked;
            });
            updateUI();
        });
    }

    // Wis selectie
    if (btnWis) {
        btnWis.addEventListener('click', function () {
            document.querySelectorAll('.chkCoordSlice').forEach(function (cb) {
                cb.checked = false;
            });
            updateUI();
        });
    }

    // Individuele "Factureer" knop: selecteer enkel die schijf en submit
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-factureer-slice');
        if (!btn) return;
        // Deselecteer alle checkboxes, selecteer enkel deze
        document.querySelectorAll('.chkCoordSlice').forEach(function (cb) {
            cb.checked = false;
        });
        const sliceId = btn.dataset.sliceId;
        const cb = document.querySelector('.chkCoordSlice[value="' + sliceId + '"]');
        if (cb) cb.checked = true;
        updateUI();
        // Submit het factuurformulier
        if (form) form.requestSubmit ? form.requestSubmit() : form.submit();
    });

    // Formulier submit bescherming
    if (form) {
        form.addEventListener('submit', function (e) {
            if (!getCheckedBoxes().length) {
                e.preventDefault();
                return;
            }
            if (form.dataset.submitting === 'true') {
                e.preventDefault();
                return;
            }
            form.dataset.submitting = 'true';
            if (btnInvoiceTop) btnInvoiceTop.disabled = true;
            if (btnCardSubmit) btnCardSubmit.disabled = true;

            var modalEl = document.getElementById('coordInvoiceBusyModal');
            if (modalEl && typeof bootstrap !== 'undefined') {
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            }
        });
    }

    // Initiële staat
    updateUI();
})();
