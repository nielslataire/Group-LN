// invoices.util.js
; (function (w) {
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

    w.CPM = w.CPM || {};
    w.CPM.util = w.CPM.util || {};
    w.CPM.util.parseLocaleNumber = parseLocaleNumber;

    // kleine helper om safe een functie aan te roepen
    w.CPM.util.safeCall = fn => { try { if (typeof fn === 'function') fn(); } catch { } };
})(window);
