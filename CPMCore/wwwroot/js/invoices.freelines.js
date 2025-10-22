// invoices.freelines.js
// Vrije lijnen met DataTables v2 + RowReorder, geforceerde ordering op verborgen kolom 7

(function ($) {
    'use strict';

    // ====== helpers ======
    function ensureGlobalParseLocaleNumber() {
        if (typeof window.parseLocaleNumber === 'function') return;
        window.parseLocaleNumber = function (val) {
            if (val == null) return 0;
            let s = String(val).trim();
            if (!s) return 0;
            s = s.replace(/[\s\u00A0\u202F\u2009]/g, '');
            const hasComma = s.includes(','), hasDot = s.includes('.');
            if (hasComma && hasDot) {
                const lc = s.lastIndexOf(','), ld = s.lastIndexOf('.');
                const dec = lc > ld ? ',' : '.';
                const idx = dec === ',' ? lc : ld;
                const left = s.slice(0, idx).replace(/[.,]/g, '');
                const right = s.slice(idx + 1).replace(/[.,]/g, '');
                s = left + '.' + right;
            } else if (hasComma) {
                s = s.replace(/\./g, '').replace(',', '.');
            } else {
                s = s.replace(/,/g, '');
            }
            s = s.replace(/[^\d.\-]/g, '');
            const n = parseFloat(s);
            return isNaN(n) ? 0 : n;
        };
    }

    // Kloneer <template id="freeLineRowTpl">
    function cloneRow(initial) {
        const tpl = document.getElementById('freeLineRowTpl');
        if (!tpl) throw new Error('freeLineRowTpl ontbreekt');
        const html = tpl.innerHTML.trim();
        const $row = $(html);

        if (initial && typeof initial === 'object') {
            for (const [k, v] of Object.entries(initial)) {
                const $el = $row.find(`[data-col="${k}"]`);
                if (!$el.length) continue;
                if ($el.is(':checkbox')) $el.prop('checked', !!v);
                else $el.val(v);
            }
        }
        return $row[0];
    }

    // Check of rij enige waarde heeft (om trailing lege rij te beheren)
    function rowHasAnyValue($tr) {
        const t = ($tr.find('.js-fl-text').val() || '').trim();
        const p = window.parseLocaleNumber($tr.find('.js-fl-price').val());
        const v = window.parseLocaleNumber($tr.find('.js-fl-vat').val());
        const d = window.parseLocaleNumber($tr.find('.js-fl-disc').val());
        return t !== '' || p !== 0 || v !== 0 || d !== 0;
    }

    $(function () {
        ensureGlobalParseLocaleNumber();

        const $tbl = $('#freeLinesTable');
        const $tbody = $tbl.find('tbody');

        // ====== DataTable init ======
        // Let op: theme/css kan ordering global uitzetten; we forceren het hier opnieuw.
        const dt = $tbl.DataTable({
            paging: false,
            searching: false,
            info: false,

            ordering: true,                 // <-- cruciaal
            order: [[7, 'asc']],            // sorteer altijd op verborgen kolom 7 (Order)

            rowReorder: {
                selector: 'td.drag-handle',
                dataSrc: 7                    // lees/schrijf de volgorde uit kolom 7
            },

            columnDefs: [
                { targets: 0, orderable: false },               // drag
                { targets: [1, 2, 3, 4, 5], orderable: false }, // inputs + actie
                { targets: 6, visible: false, orderable: false }, // verborgen model
                { targets: 7, visible: false, orderable: true, type: 'num' } // ⬅️ make-or-break
            ],

            language: { emptyTable: 'Geen vrije lijnen' }
        });

        // Eventuele globals
        window.freeLinesDT = dt;

        // Debug sanity check
        (function () {
            const st = dt.settings()[0];
            console.log('[FreeLines] ordering?', st?.oFeatures?.bSort);
            console.log('[FreeLines] col7 orderable?', st?.aoColumns?.[7]?.bSortable, st?.aoColumns?.[7]);
        })();

        // ====== Reindex & Order sync ======
        function reindexNamesAndOrder() {
            let i = 0, orderVal = 1;

            // loop in toegepaste volgorde (na slepen/sorteren)
            dt.rows({ order: 'applied' }).nodes().each(function (row) {
                const $tr = $(row);

                // name="Lines[i].X" aanpassen obv zichtbare volgorde
                $tr.find('[data-col]').each(function () {
                    const col = $(this).data('col');
                    this.name = `Lines[${i}].${col}`;
                });

                // schrijf de Order-waarde zowel:
                // - in de verborgen js-order-cell (kolom 7, dataSrc) -> zorgt voor UI-positie
                // - in het verborgen modelveld data-col="Order"       -> wordt gepost naar server
                $tr.find('.js-order-cell').text(orderVal);
                const $orderInput = $tr.find('[data-col="Order"]');
                if ($orderInput.length) $orderInput.val(orderVal);

                i += 1;
                orderVal += 1;
            });

            // Laat DT kolom 7 opnieuw uit DOM lezen en sorteer opnieuw op kolom 7
            dt.cells(null, 7).invalidate('dom');
            dt.order([7, 'asc']).draw(false);
        }

        // ====== trailing lege rij ======
        let adding = false;
        function ensureTrailingEmpty(keepFocusEl) {
            if (adding) return;

            const $rows = $tbody.children('tr.free-row');
            const needNew = ($rows.length === 0) || rowHasAnyValue($rows.last());
            if (!needNew) { reindexNamesAndOrder(); return; }

            // focus bewaren
            const active = keepFocusEl && document.body.contains(keepFocusEl) ? keepFocusEl : document.activeElement;
            const nameKeep = active && active.name ? active.name : null;
            let selStart = null, selEnd = null;
            if (active && typeof active.selectionStart === 'number') {
                selStart = active.selectionStart; selEnd = active.selectionEnd;
            }

            adding = true;
            dt.row.add(cloneRow());

            dt.one('draw', function () {
                adding = false;
                reindexNamesAndOrder();

                // focus herstellen
                if (nameKeep) {
                    const $again = $(`input[name="${CSS.escape(nameKeep)}"]`);
                    if ($again.length) {
                        const el = $again[0];
                        el.focus();
                        if (selStart != null && selEnd != null && typeof el.setSelectionRange === 'function') {
                            try { el.setSelectionRange(selStart, selEnd); } catch { /* ignore */ }
                        }
                    }
                }
                if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
            });

            dt.draw(false);
        }

        // ====== start: minstens 1 lege rij ======
        dt.row.add(cloneRow()).draw(false);
        reindexNamesAndOrder();

        // ====== events ======
        // RowReorder -> reindex + preview
        $tbl.on('row-reorder.dt', function () {
            reindexNamesAndOrder();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        });

        // Typen/wijzigen -> trailing rij + preview
        $tbody.on('input change', 'tr.free-row input', function () {
            ensureTrailingEmpty(this);
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        });

        // Verwijderen
        $tbody.on('click', '.js-fl-del', function () {
            dt.row($(this).closest('tr')).remove().draw(false);
            if ($tbody.children('tr.free-row').length === 0) {
                dt.row.add(cloneRow()).draw(false);
            }
            reindexNamesAndOrder();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        });

        // Handmatig toevoegen
        $('#addFreeLine').on('click', function () {
            dt.row.add(cloneRow()).draw(false);
            reindexNamesAndOrder();
        });

        // Voor de zekerheid: vóór form submit alles netjes reindexen
        $('#invoiceForm').on('submit', function () {
            reindexNamesAndOrder();
            return true;
        });
    });
})(jQuery);
