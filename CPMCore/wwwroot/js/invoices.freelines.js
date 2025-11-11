// Vrije lijnen (DataTable + RowReorder)
(function () {
    const $tbl = $('#freeLinesTable');
    const dt = $tbl.DataTable({
        paging: false, searching: false, info: false,
        ordering: true, order: [[1, 'asc']],
        rowReorder: { selector: 'td.reorder-handle', dataSrc: 1 },
        columnDefs: [
            { targets: [0, 2, 3, 4, 5], orderable: false },
            { targets: 1, visible: false, searchable: false }
        ],
        autoWidth: false,
        language: { emptyTable: 'Geen vrije lijnen' }
    });

    const { nf, parseLocaleNumber } = window.InvoicesUtil || {
        nf: new Intl.NumberFormat('nl-BE'),
        parseLocaleNumber: s => {
            if (s == null) return 0;
            s = String(s).trim();
            if (!s) return 0;
            // NL/BE: . = thousands, , = decimal
            s = s.replace(/\./g, '').replace(',', '.');
            const x = parseFloat(s);
            return isNaN(x) ? 0 : x;
        }
    };
    function formatPriceInput($input) {
        const raw = $input.val();
        // Leeg laten als leeg
        if (raw == null || String(raw).trim() === '') { return; }
        const num = parseLocaleNumber(raw);
        $input.val(nf.format(num));
    }
    function cloneRow(initial = {}) {
        const html = document.getElementById('freeLineRowTpl').innerHTML.trim();
        const $row = $(html);

        // BTW-select vullen + default kiezen (globaal), tenzij initial.VatTypeId/Percentage is meegegeven
        const $vatSel = $row.find('.js-fl-vat-select');
        fillVatOptions($vatSel);

        if (initial.VatTypeId != null) {
            $vatSel.val(String(initial.VatTypeId));
        } else if (initial.VatPercentage != null) {
            // kies de optie met matching percentage
            let matched = false;
            $vatSel.children('option').each(function () {
                const pct = parseFloat(String($(this).data('pct') || '0').replace(',', '.')) || 0;
                if (pct === Number(initial.VatPercentage)) { $vatSel.val($(this).val()); matched = true; return false; }
            });
            if (!matched) setRowVatToGlobal($row);
        } else {
            setRowVatToGlobal($row);
        }

        // overige initiële velden
        Object.entries(initial).forEach(([k, v]) => {
            if (k === 'VatTypeId' || k === 'VatPercentage') return;
            const $el = $row.find('[data-col="' + k + '"]');
            if (!$el.length) return;
            if ($el.is(':checkbox')) $el.prop('checked', !!v);
            else $el.val(v);
        });

        return $row[0];
    }

    function notifyStateChange() {
        if (window.updateSaveButtonState) window.updateSaveButtonState();
    }

    function fillVatOptions($sel) {
        $sel.empty();
        $('#VatTypeId option').each(function () {
            // we kopiëren value + data-pct + tekst
            const $opt = $('<option>')
                .val($(this).val())
                .attr('data-pct', $(this).data('pct'))
                .text($(this).text());
            $sel.append($opt);
        });
    }


    function setRowVatToGlobal($row) {
        const globalId = String($('#VatTypeId').val() || '');
        const $sel = $row.find('.js-fl-vat-select');
        if ($sel.length) {
            if ($sel.children('option').length === 0) fillVatOptions($sel);
            $sel.val(globalId);
        }
    }
    function getNextOrder() {
        const data = dt.column(1, { search: 'applied' }).data();
        let max = 0;
        for (let i = 0; i < data.length; i++) {
            const n = parseInt(data[i], 10) || 0;
            if (n > max) max = n;
        }
        return max + 1;
    }

    function reindexFromDisplay() {
        let i = 0;
        dt.rows({ order: 'current' }).every(function () {
            dt.cell(this.index(), 1).data(String(++i));
        });
        dt.cells(null, 1).invalidate('dom');
    }

    function addEmptyRowAtEnd() {
        const node = cloneRow();
        node.cells[1].textContent = String(getNextOrder());
        dt.row.add(node).draw(false);
        // direct na draw: bedragveld formatteren (blijft leeg als leeg)
        dt.one('draw', function () {
            const $last = $(dt.rows({ order: 'current' }).nodes()).last();
            $last.find('.js-fl-price').each(function () { formatPriceInput($(this)); });
        });

        reindexFromDisplay();
        notifyStateChange();
    }
    function loadInitialRows(rows) {
        if (!Array.isArray(rows)) return;
        dt.clear();
        rows.forEach((row, idx) => {
            const node = cloneRow({
                Text: row.text,
                Price: row.price != null ? nf.format(row.price) : '',
                VatTypeId: row.vatTypeId,
                VatPercentage: row.vatPercentage
            });
            node.cells[1].textContent = String(idx + 1);
            dt.row.add(node);
        });
        dt.draw(false);
        reindexFromDisplay();
        dt.one('draw', function () {
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        });
        notifyStateChange();
    }

    function lastRowIsEmpty() {
        const nodes = dt.rows({ order: 'current' }).nodes();
        if (!nodes.length) return false;
        const $last = $(nodes[nodes.length - 1]);
        return (($last.find('.js-fl-text').val() || '').trim() === '');
    }

    const initialLines = window.InitialInvoice && Array.isArray(window.InitialInvoice.lines)
        ? window.InitialInvoice.lines
        : null;

    if (initialLines && initialLines.length > 0) {
        loadInitialRows(initialLines);
    } else {
        addEmptyRowAtEnd();
    }

    // Drag → hernummeren + preview na redraw
    $tbl.on('row-reorder.dt', function () {
        reindexFromDisplay();
        const curOrder = dt.order();
        if (curOrder.length && curOrder[0][0] === 1) dt.order([1, 'asc']).draw(false); else dt.draw(false);
        dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
    });

    // Toevoegen-knop
    $('#addFreeLine').on('click', function () {
        addEmptyRowAtEnd();
        dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
        notifyStateChange();
    });

    // Verwijderen
    $tbl.on('click', '.js-fl-del', function () {
        dt.row($(this).closest('tr')).remove().draw(false);
        if (dt.rows().count() === 0) addEmptyRowAtEnd();
        reindexFromDisplay();
        dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
        notifyStateChange();
    });

    // Typen in omschrijving → trailing lege rij
    $tbl.on('input', 'tbody .js-fl-text', function () {
        const active = this;
        const selStart = typeof active.selectionStart === 'number' ? active.selectionStart : null;
        const selEnd = typeof active.selectionEnd === 'number' ? active.selectionEnd : null;
        if (!lastRowIsEmpty()) {
            addEmptyRowAtEnd();
            setTimeout(() => {
                if (document.body.contains(active)) {
                    active.focus();
                    if (selStart != null && selEnd != null && typeof active.setSelectionRange === 'function') {
                        try { active.setSelectionRange(selStart, selEnd); } catch { }
                    }
                }
            }, 0);
        }
        notifyStateChange();
    });
    // helper: is een rij "leeg" (geen omschrijving en geen bedrag)?
    function rowIsEmpty($tr) {
        const txt = ($tr.find('.js-fl-text').val() || '').trim();
        const price = ($tr.find('.js-fl-price').val() || '').trim();
        return txt === '' && price === '';
    }

    // Globale BTW-type wijzigt -> pas alle lege vrije-lijn-rijen aan
    $(document).on('change', '#VatTypeId', function () {
        if (!(window.freeLines && window.freeLines.dt)) return;
        const dt = window.freeLines.dt;

        dt.rows().every(function () {
            const $tr = $(this.node());
            if (rowIsEmpty($tr)) setRowVatToGlobal($tr);
        });

        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });

    // Bedrag formatten wanneer gebruiker klaar is
    $tbl.on('blur change', 'tbody .js-fl-price', function () {
        formatPriceInput($(this));
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        notifyStateChange();
    });
    // Na elke draw: alle zichtbare bedragen netjes formatteren
    $tbl.on('draw.dt', function () {
        $tbl.find('tbody .js-fl-price').each(function () {
            formatPriceInput($(this));
        });
    });
    // optioneel: alleen toegestane tekens tijdens input
    $tbl.on('input', 'tbody .js-fl-price', function () {
        let v = $(this).val();
        // laat cijfers, punten en komma's toe; verwijder overige
        v = v.replace(/[^\d.,]/g, '');
        // niet meerdere komma's
        const parts = v.split(',');
        if (parts.length > 2) {
            v = parts.shift() + ',' + parts.join('');
        }
        $(this).val(v);
        notifyStateChange();
    });
    // wijzig je per-lijn btw → meteen preview heropbouwen
    $tbl.on('change', 'tbody .js-fl-vat', function () {
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });





    // Exporteer minimale API voor andere modules
    window.freeLines = {
        dt,
        addEmptyRow: addEmptyRowAtEnd,
        clearAll: function () {
            dt.clear().draw(false);
            dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
            notifyStateChange();
        },
        ensureOne: function () {
            if (dt.rows().count() === 0) addEmptyRowAtEnd();
            notifyStateChange();
        },
        loadInitial: function (rows) {
            loadInitialRows(rows);
        }
    };

    if (Array.isArray(window.pendingInitialLines) && window.pendingInitialLines.length > 0) {
        loadInitialRows(window.pendingInitialLines);
        window.pendingInitialLines = null;
    }
})();
