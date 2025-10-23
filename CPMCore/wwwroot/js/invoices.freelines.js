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

    function cloneRow(initial = {}) {
        const html = document.getElementById('freeLineRowTpl').innerHTML.trim();
        const $row = $(html);
        Object.entries(initial).forEach(([k, v]) => {
            const $el = $row.find('[data-col="' + k + '"]');
            if (!$el.length) return;
            if ($el.is(':checkbox')) $el.prop('checked', !!v);
            else $el.val(v);
        });
        return $row[0];
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
        reindexFromDisplay();
    }

    function lastRowIsEmpty() {
        const nodes = dt.rows({ order: 'current' }).nodes();
        if (!nodes.length) return false;
        const $last = $(nodes[nodes.length - 1]);
        return (($last.find('.js-fl-text').val() || '').trim() === '');
    }

    // Init: 1 lege rij
    addEmptyRowAtEnd();

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
    });

    // Verwijderen
    $tbl.on('click', '.js-fl-del', function () {
        dt.row($(this).closest('tr')).remove().draw(false);
        if (dt.rows().count() === 0) addEmptyRowAtEnd();
        reindexFromDisplay();
        dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
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
    });

    // Exporteer minimale API voor andere modules
    window.freeLines = {
        dt,
        addEmptyRow: addEmptyRowAtEnd,
        clearAll: function () {
            dt.clear().draw(false);
            dt.one('draw', function () { if (window.rebuildInvoicePreview) window.rebuildInvoicePreview(); });
        },
        ensureOne: function () {
            if (dt.rows().count() === 0) addEmptyRowAtEnd();
        }
    };
})();
