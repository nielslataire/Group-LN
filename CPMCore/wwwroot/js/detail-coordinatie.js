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
    const infoBar        = document.getElementById('sliceInfoBar');

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

        // Selectiekaart tonen/verbergen; informatiebalk omgekeerd
        if (selectionCard) {
            if (hasSelection) {
                selectionCard.classList.remove('d-none');
                selectionCard.classList.add('d-flex');
            } else {
                selectionCard.classList.add('d-none');
                selectionCard.classList.remove('d-flex');
            }
        }
        if (infoBar) {
            infoBar.classList.toggle('d-none', hasSelection);
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

// ── Regie-uren snelrij (AJAX) ─────────────────────────────────────────────────
(function () {
    const configEl = document.getElementById('regie-config');
    if (!configEl) return;

    const cfg           = JSON.parse(configEl.textContent);
    const projectId     = cfg.projectId;
    const roundTrip     = parseFloat(cfg.roundTripKm) || 0;
    const kmPrice       = parseFloat(cfg.kmAllowance)  || 0;
    const addUrl        = cfg.addUrl;
    const deleteUrl     = cfg.deleteUrl;
    const defaultUserId = cfg.defaultUserId || '';

    const workerSel      = document.getElementById('regieWorker');
    const dateInput      = document.getElementById('regieDate');
    const hoursInput     = document.getElementById('regieHours');
    const travelChk      = document.getElementById('regieTravelChk');
    const travelKmInput  = document.getElementById('regieTravelKm');
    const descInput      = document.getElementById('regieDescription');
    const addBtn         = document.getElementById('btnRegieAdd');
    const bodyOpen       = document.getElementById('regieBodyOpen');
    const emptyRow       = document.getElementById('regieEmptyRow');
    const subtotalRow    = document.getElementById('regieSubtotalOpen');
    const tfoot          = document.getElementById('regieTfoot');

    // Travel checkbox → km input koppeling
    if (travelChk && travelKmInput) {
        travelChk.addEventListener('change', function () {
            travelKmInput.disabled = !travelChk.checked;
            if (travelChk.checked && (!travelKmInput.value || parseFloat(travelKmInput.value) <= 0)) {
                travelKmInput.value = Math.ceil(roundTrip);
            }
        });
    }

    // Projectbeheerder vooraf selecteren indien aanwezig in de lijst
    if (workerSel && defaultUserId) {
        var opt = workerSel.querySelector('option[value="' + defaultUserId + '"]');
        if (opt) workerSel.value = defaultUserId;
    }

    function formatEuro(val) {
        return '€\u00a0' + val.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatNum(val, decimals) {
        return val.toLocaleString('nl-BE', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
    }

    function formatDate(iso) {
        if (!iso) return '';
        var parts = iso.split('-');
        return parts[2] + '/' + parts[1] + '/' + parts[0];
    }

    function sumSection(selector) {
        var hours = 0, km = 0, amount = 0;
        document.querySelectorAll(selector).forEach(function (tr) {
            hours  += parseFloat(tr.dataset.hours)  || 0;
            km     += parseFloat(tr.dataset.km)     || 0;
            amount += parseFloat(tr.dataset.amount) || 0;
        });
        return { hours: hours, km: km, amount: amount };
    }

    function recalcTotals() {
        var open     = sumSection('#regieBodyOpen tr[data-regie-row]');
        var invoiced = sumSection('#regieBodyInvoiced tr[data-regie-row]');
        var hasOpen  = document.querySelectorAll('#regieBodyOpen tr[data-regie-row]').length > 0;

        // Lege-staat voor open sectie
        if (emptyRow)    emptyRow.classList.toggle('d-none', hasOpen);
        if (subtotalRow) subtotalRow.classList.toggle('d-none', !hasOpen);

        // Open subtotaal
        var elOH = document.getElementById('regieOpenTotalHours');
        var elOK = document.getElementById('regieOpenTotalKm');
        var elOA = document.getElementById('regieOpenTotalAmount');
        if (elOH) elOH.textContent = formatNum(open.hours, 2);
        if (elOK) elOK.textContent = open.km > 0 ? formatNum(open.km, 0) + '\u00a0km' : '—';
        if (elOA) elOA.textContent = formatEuro(open.amount);

        // Totaalregel (open + gefactureerd)
        var totalHours  = open.hours  + invoiced.hours;
        var totalKm     = open.km     + invoiced.km;
        var totalAmount = open.amount + invoiced.amount;
        var hasAny      = totalHours > 0 || document.querySelectorAll('[data-regie-row]').length > 0;

        if (tfoot) tfoot.classList.toggle('d-none', !hasAny);
        var elTH = document.getElementById('regieTotalHours');
        var elTK = document.getElementById('regieTotalKm');
        var elTA = document.getElementById('regieTotalAmount');
        if (elTH) elTH.textContent = formatNum(totalHours, 2);
        if (elTK) elTK.textContent = totalKm > 0 ? formatNum(totalKm, 0) + '\u00a0km' : '—';
        if (elTA) elTA.textContent = formatEuro(totalAmount);

        // Stat-kaarten bijwerken
        var elST = document.getElementById('regieStatTotal');
        var elSI = document.getElementById('regieStatInvoiced');
        var elSO = document.getElementById('regieStatOpen');
        var elSA = document.getElementById('regieStatAmount');
        if (elST) elST.textContent = formatNum(totalHours, 2) + '\u00a0u';
        if (elSI) elSI.textContent = formatNum(invoiced.hours, 2) + '\u00a0u';
        if (elSO) elSO.textContent = formatNum(open.hours, 2) + '\u00a0u';
        if (elSA) elSA.textContent = formatEuro(open.amount);
    }

    function appendRow(entry) {
        var km     = (entry.travelKm != null && entry.travelKm > 0) ? parseFloat(entry.travelKm) : 0;
        var amount = entry.hours * entry.hourlyRate + km * kmPrice;

        var tr = document.createElement('tr');
        tr.setAttribute('data-regie-row', entry.id);
        tr.setAttribute('data-invoiced', 'false');
        tr.setAttribute('data-date',   entry.date);
        tr.setAttribute('data-hours',  entry.hours);
        tr.setAttribute('data-km',     km);
        tr.setAttribute('data-amount', amount);
        tr.innerHTML =
            '<td class="align-middle">' + formatDate(entry.date) + '</td>' +
            '<td class="align-middle">' + entry.userFullName + '</td>' +
            '<td class="align-middle text-muted small">' + (entry.description || '') + '</td>' +
            '<td class="align-middle text-end text-nowrap">' + formatNum(entry.hours, 2) + '</td>' +
            '<td class="align-middle text-end text-nowrap">' + (km > 0 ? formatNum(km, 0) + '\u00a0km' : '—') + '</td>' +
            '<td class="align-middle text-end text-nowrap fw-semibold">' + formatEuro(amount) + '</td>' +
            '<td class="align-middle text-end">' +
                '<a href="#" class="btn-regie-delete text-danger" data-id="' + entry.id + '" title="Verwijder">' +
                    '<i class="bx bx-trash fs-5"></i>' +
                '</a>' +
            '</td>';
        // Bovenaan invoegen (meest recente datum eerst)
        if (bodyOpen) bodyOpen.prepend(tr);
        recalcTotals();
    }

    function getToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    if (addBtn) {
        addBtn.addEventListener('click', function () {
            var opt = workerSel ? workerSel.options[workerSel.selectedIndex] : null;
            if (!opt || !opt.value) {
                workerSel && workerSel.classList.add('is-invalid');
                return;
            }
            workerSel.classList.remove('is-invalid');

            var hours = parseFloat((hoursInput ? hoursInput.value : '0').replace(',', '.')) || 0;
            if (hours <= 0) {
                hoursInput && hoursInput.classList.add('is-invalid');
                return;
            }
            hoursInput.classList.remove('is-invalid');

            addBtn.disabled = true;

            var travelKm = null;
            if (travelChk && travelChk.checked && travelKmInput) {
                var parsedKm = parseFloat((travelKmInput.value || '0').replace(',', '.'));
                if (parsedKm > 0) travelKm = parsedKm;
            }

            fetch(addUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getToken()
                },
                body: JSON.stringify({
                    projectId:   projectId,
                    userId:      opt.value,
                    date:        dateInput ? dateInput.value : '',
                    hours:       hours,
                    travelKm:    travelKm,
                    description: descInput ? descInput.value.trim() : null
                })
            })
            .then(function (r) { return r.ok ? r.json() : Promise.reject(r); })
            .then(function (entry) {
                appendRow(entry);
                if (hoursInput)    hoursInput.value    = 0;
                if (descInput)     descInput.value     = '';
                if (travelChk)     travelChk.checked   = false;
                if (travelKmInput) { travelKmInput.value = Math.ceil(roundTrip); travelKmInput.disabled = true; }
            })
            .catch(function () {
                alert('Opslaan mislukt. Probeer opnieuw.');
            })
            .finally(function () {
                addBtn.disabled = false;
            });
        });
    }

    // ── Verwijder bevestiging via Magnific Popup ─────────────────────────────
    var pendingDeleteId = null;

    function doDeleteRegie(id) {
        fetch(deleteUrl + '?id=' + id, {
            method: 'POST',
            headers: { 'RequestVerificationToken': getToken() }
        })
        .then(function (r) {
            if (r.ok) {
                var tr = document.querySelector('tr[data-regie-row="' + id + '"]');
                if (tr) tr.remove();
                recalcTotals();
                updatePeriodSelection(currentCutoff);
            } else if (r.status === 409) {
                alert('Dit regie-uur is al gefactureerd en kan niet worden verwijderd.');
            } else {
                alert('Verwijderen mislukt. Probeer opnieuw.');
            }
        });
    }

    // Verwijder-knop klik → popup openen
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.btn-regie-delete');
        if (!link) return;
        e.preventDefault();
        var id = link.dataset.id;
        if (!id) return;
        pendingDeleteId = id;
        if (typeof $ !== 'undefined' && $.magnificPopup) {
            $.magnificPopup.open({ items: { src: '#regieDeleteModal' }, type: 'inline', preloader: false });
        }
    });

    // Bevestig verwijderen
    var confirmDeleteBtn = document.getElementById('btnRegieDeleteConfirm');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function () {
            if (!pendingDeleteId) return;
            var id = pendingDeleteId;
            pendingDeleteId = null;
            if (typeof $ !== 'undefined' && $.magnificPopup) $.magnificPopup.close();
            doDeleteRegie(id);
        });
    }

    // ── Factuurperiode-selectie ────────────────────────────────────────────────
    var currentCutoff    = null;
    var invoiceBar       = document.getElementById('regieInvoiceBar');
    var btnUntilToday    = document.getElementById('btnRegieUntilToday');
    var cutoffInput      = document.getElementById('regieCutoffDate');
    var btnClearPeriod   = document.getElementById('btnRegieClearPeriod');
    var summaryRow       = document.getElementById('regieSummaryRow');
    var summaryText      = document.getElementById('regieSummaryText');
    var invoiceIdsDiv    = document.getElementById('regieInvoiceIds');
    var regieInvoiceForm = document.getElementById('regieInvoiceForm');

    function setBarStyle(active) {
        if (!invoiceBar) return;
        var bg     = active ? '#d1f0dc' : '#e8f4fd';
        var border = active ? '1px solid #a3d9b5' : '1px solid #bee5eb';
        invoiceBar.style.backgroundColor = bg;
        invoiceBar.style.border          = border;
        var body = invoiceBar.querySelector('.card-body');
        if (body) body.style.backgroundColor = bg;
    }

    // Initiële blauwe stijl toepassen zodra de balk aanwezig is
    setBarStyle(false);

    function updatePeriodSelection(cutoff) {
        currentCutoff = cutoff;

        if (!cutoff) {
            setBarStyle(false);
            if (summaryRow)     summaryRow.classList.add('d-none');
            if (btnClearPeriod) btnClearPeriod.classList.add('d-none');
            return;
        }

        var totalHours = 0, totalAmount = 0, selectedIds = [];
        document.querySelectorAll('#regieBodyOpen tr[data-regie-row]').forEach(function (tr) {
            var rowDate = tr.dataset.date; // yyyy-MM-dd — string comparison is safe for ISO dates
            if (rowDate && rowDate <= cutoff) {
                selectedIds.push(tr.dataset.regieRow);
                totalHours  += parseFloat(tr.dataset.hours)  || 0;
                totalAmount += parseFloat(tr.dataset.amount) || 0;
            }
        });

        // Update hidden form inputs
        if (invoiceIdsDiv) {
            invoiceIdsDiv.innerHTML = '';
            selectedIds.forEach(function (id) {
                var inp = document.createElement('input');
                inp.type  = 'hidden';
                inp.name  = 'regieUurIds';
                inp.value = id;
                invoiceIdsDiv.appendChild(inp);
            });
        }

        if (btnClearPeriod) btnClearPeriod.classList.remove('d-none');

        if (selectedIds.length === 0) {
            setBarStyle(false);
            if (summaryRow) summaryRow.classList.add('d-none');
            return;
        }

        // Datum opmaken als dd/MM/yyyy
        var parts = cutoff.split('-');
        var fmtDate = parts[2] + '/' + parts[1] + '/' + parts[0];

        if (summaryText) {
            summaryText.textContent =
                formatNum(totalHours, 2) + '\u00a0u geselecteerd tot en met ' +
                fmtDate + '\u00a0\u2013\u00a0' + formatEuro(totalAmount);
        }

        if (summaryRow) summaryRow.classList.remove('d-none');
        setBarStyle(true);
    }

    if (btnUntilToday) {
        btnUntilToday.addEventListener('click', function () {
            var today = new Date().toISOString().split('T')[0];
            if (cutoffInput) cutoffInput.value = today;
            updatePeriodSelection(today);
        });
    }

    if (cutoffInput) {
        cutoffInput.addEventListener('change', function () {
            updatePeriodSelection(cutoffInput.value || null);
        });
    }

    if (btnClearPeriod) {
        btnClearPeriod.addEventListener('click', function () {
            if (cutoffInput) cutoffInput.value = '';
            updatePeriodSelection(null);
        });
    }

    // Busy-modal tonen bij submit
    if (regieInvoiceForm) {
        regieInvoiceForm.addEventListener('submit', function (e) {
            if (!invoiceIdsDiv || !invoiceIdsDiv.querySelector('input')) {
                e.preventDefault();
                return;
            }
            var modalEl = document.getElementById('regieInvoiceBusyModal');
            if (modalEl && typeof bootstrap !== 'undefined') {
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            }
        });
    }
})();
