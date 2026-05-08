// Vrije lijnen (DataTable + RowReorder)
(function () {
    const $tbl = $('#freeLinesTable');
    const dt = $tbl.DataTable({
        paging: false, searching: false, info: false,
        ordering: true, order: [[1, 'asc']],
        rowReorder: { selector: 'td.gl-reorder-handle', dataSrc: 1 },
        columnDefs: [
            { targets: [0, 2, 3, 4, 5, 6], orderable: false },
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
        const normalized = String(raw).replace(/\u2212/g, '-'); // sta ook typografische min-teken toe
        const num = parseLocaleNumber(normalized);
        $input.val(nf.format(num));
    }
    function cloneRow(initial = {}) {
        const html = document.getElementById('freeLineRowTpl').innerHTML.trim();
        const $row = $(html);
        const initialVatTypeId = initial.vatTypeId ?? initial.VatTypeId;
        const initialVatCode = initial.vatCode ?? initial.VatCode;
        const initialVatPercentage = initial.vatPercentage ?? initial.VatPercentage;

        // BTW-select vullen + default kiezen (globaal), tenzij initial.VatTypeId/Percentage is meegegeven
        const $vatSel = $row.find('.js-fl-vat-select');
        fillVatOptions($vatSel);
        if (initialVatTypeId != null) {
            $vatSel.val(String(initialVatTypeId));
        } else if (initialVatCode) {
            const codeUpper = String(initialVatCode).toUpperCase();
            const $matchByCode = $vatSel.children('option').filter(function () {
                const optCode = ($(this).data('code') || '').toString().toUpperCase();
                return optCode === codeUpper;
            }).first();
            if ($matchByCode.length) {
                $vatSel.val($matchByCode.val());
            } else if (initialVatPercentage != null) {
                // fallback op percentage wanneer code niet gevonden werd
                let matched = false;
                $vatSel.children('option').each(function () {
                    const pct = parseFloat(String($(this).data('pct') || '0').replace(',', '.')) || 0;
                    if (pct === Number(initialVatPercentage)) { $vatSel.val($(this).val()); matched = true; return false; }
                });
                if (!matched) setRowVatToGlobal($row);
            }
        } else if (initialVatPercentage != null) {
            // kies de optie met matching percentage
            let matched = false;
            $vatSel.children('option').each(function () {
                const pct = parseFloat(String($(this).data('pct') || '0').replace(',', '.')) || 0;
                if (pct === Number(initialVatPercentage)) { $vatSel.val($(this).val()); matched = true; return false; }
            });
            if (!matched) setRowVatToGlobal($row);
        } else {
            setRowVatToGlobal($row);
        }

         
        // overige initiële velden
        Object.entries(initial).forEach(([k, v]) => {
            const keyLower = String(k || '').toLowerCase();
            if (keyLower === 'vattypeid' || keyLower === 'vatpercentage' || keyLower === 'vatcode') return;
            const $el = $row.find('[data-col]').filter(function () {
                const col = ($(this).data('col') || '').toString().toLowerCase();
                return col === keyLower;
            });
            if (!$el.length) return;
            if ($el.is(':checkbox')) $el.prop('checked', !!v);
            else $el.val(v);
        });

        return $row[0];
    }

    function notifyStateChange() {
        if (window.updateSaveButtonState) window.updateSaveButtonState();
    }

    function initVatSelect2($sel) {
        if ($sel.data('select2')) return;
        $sel.select2({
            theme: 'bootstrap',
            minimumResultsForSearch: Infinity,
            width: '100%',
            dropdownAutoWidth: true,
            templateSelection: function (data) {
                const code = $(data.element).data('code') || data.text;
                return document.createTextNode(code);
            },
            templateResult: function (data) {
                return document.createTextNode(data.text);
            },
            dropdownParent: $('body')
        });
    }

    function fillVatOptions($sel) {
        $sel.empty();

        const $templateOptions = $('#freeLineVatOptions option');
        const $source = $templateOptions.length ? $templateOptions : $('#VatTypeId option');

        $source.each(function () {
            // kopieer value + data-attributen rechtstreeks uit het DOM
            const pct = $(this).attr('data-pct');
            let code = $(this).attr('data-code');

            if (!code) {
                const optText = ($(this).text() || '').trim();
                const dash = optText.indexOf('-');
                code = dash >= 0 ? optText.slice(0, dash).trim() : optText;
            }

            const $opt = $('<option>')
                .val($(this).val())
                .attr('data-pct', pct)
                .attr('data-code', code)
                .text($(this).text());
            $sel.append($opt);
        });
    }


    function setRowVatToGlobal($row) {
        const globalId = String($('#VatTypeId').val() || '');
        const $sel = $row.find('.js-fl-vat-select');
        if ($sel.length) {
            if ($sel.children('option').length === 0) fillVatOptions($sel);
            $sel.val(globalId).trigger('change');
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
            const unitPrice = row.unitPrice != null ? row.unitPrice : row.price;
            const node = cloneRow({
                Text: row.text,
                Quantity: row.quantity != null ? nf.format(row.quantity) : '',
                Price: unitPrice != null ? nf.format(unitPrice) : '',
                VatTypeId: row.vatTypeId,
                VatPercentage: row.vatPercentage,
                VatCode: row.vatCode
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

    // Aantal formatten wanneer gebruiker klaar is
    $tbl.on('blur change', 'tbody .js-fl-qty', function () {
        formatPriceInput($(this));
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        notifyStateChange();
    });
    $tbl.on('input', 'tbody .js-fl-qty', function () {
        let v = $(this).val();
        v = v.replace(/[^\d.,]/g, '');
        const parts = v.split(',');
        if (parts.length > 2) v = parts.shift() + ',' + parts.join('');
        $(this).val(v);
        notifyStateChange();
    });

    // Bedrag formatten wanneer gebruiker klaar is
    $tbl.on('blur change', 'tbody .js-fl-price', function () {
        formatPriceInput($(this));
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        notifyStateChange();
    });
    // Na elke draw: bedragen formatteren + Select2 initialiseren op VAT-selects
    $tbl.on('draw.dt', function () {
        $tbl.find('tbody .js-fl-price').each(function () {
            formatPriceInput($(this));
        });
        $tbl.find('tbody .js-fl-vat-select').each(function () {
            initVatSelect2($(this));
        });
    });
    // optioneel: alleen toegestane tekens tijdens input
    $tbl.on('input', 'tbody .js-fl-price', function () {
        let v = $(this).val();
        // laat cijfers, minteken, punten en komma's toe; verwijder overige
        v = v.replace(/[^\d.,-]/g, '');

        // Zorg dat het minteken enkel vooraan voorkomt
        const hasMinus = v.includes('-');
        v = v.replace(/-/g, '');
        if (hasMinus) {
            v = '-' + v;
        }

        // niet meerdere komma's
        const sign = v.startsWith('-') ? '-' : '';
        let numeric = sign ? v.slice(1) : v;
        const parts = numeric.split(',');
        if (parts.length > 2) {
            numeric = parts.shift() + ',' + parts.join('');
        }

        $(this).val(sign + numeric);
        notifyStateChange();
    });
    // wijzig je per-lijn btw → meteen preview heropbouwen
    $tbl.on('change', 'tbody .js-fl-vat', function () {
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });

    // Voorkom dat DataTables toetsenbord-events wegvangt terwijl Select2 actief is
    $tbl.on('keydown', '.select2-container', function (e) {
        e.stopPropagation();
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
