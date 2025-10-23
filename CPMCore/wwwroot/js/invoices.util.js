// Utilities die overal gebruikt worden
window.InvoicesUtil = (function () {
    const nf = new Intl.NumberFormat('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function parseLocaleNumber(val) {
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
    }

    function esc(s) {
        return $('<div/>').text(s || '').html();
    }

    function addDays(dmy, days) {
        if (!dmy) return '—';
        const [dd, mm, yyyy] = dmy.split('/');
        const d = new Date(+yyyy, (+mm) - 1, +dd);
        d.setDate(d.getDate() + (days || 0));
        const dd2 = ('0' + d.getDate()).slice(-2);
        const mm2 = ('0' + (d.getMonth() + 1)).slice(-2);
        return `${dd2}/${mm2}/${d.getFullYear()}`;
    }

    return { nf, parseLocaleNumber, esc, addDays };
})();
