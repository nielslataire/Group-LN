; (() => {
    function colReport() {
        const $tbl = $('#freeLinesTable');
        const headCols = $tbl.find('thead th').length;
        const bodyCols = $tbl.find('tbody tr:first td').length;
        console.log('[DEBUG] thead cols =', headCols, 'tbody cols =', bodyCols);
        if (headCols !== bodyCols) {
            console.warn('%cKolom mismatch!', 'color:red;font-weight:bold');
        } else { console.log('%cKolomtelling OK', 'color:green;font-weight:bold'); }
    }
    function dumpOrder() {
        const rows = [];
        $('#freeLinesTable tbody tr.free-row').each(function (i) {
            rows.push({
                domIndex: i + 1,
                text: ($(this).find('.js-fl-text').val() || '').trim()
            });
        });
        console.table(rows);
    }
    function hookEvents() {
        const $tbl = $('#freeLinesTable');
        $tbl.on('row-reorder.dt', function (e, diff, edit) {
            console.log('[DEBUG] row-reorder fired. diff:', diff, 'edit:', edit);
            dumpOrder();
        });
        $tbl.on('draw.dt order.dt', function () {
            console.log('[DEBUG] draw/order event'); dumpOrder();
        });
    }
    window.CPM = window.CPM || {};
    window.CPM.debug = { colReport, dumpOrder };
    $(function () { hookEvents(); setTimeout(colReport, 0); });
})();
