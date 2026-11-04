/* Verzendstatus voor formulierknoppen. glBusy(knop, true, 'Verzenden…') toont een draaiende spinner met label
   en blokkeert de knop; glBusy(knop, false) zet de oorspronkelijke inhoud terug. */
(function () {
    'use strict';
    window.glBusy = function (btn, on, label) {
        if (!btn) { return; }
        if (on) {
            if (!btn.classList.contains('is-sending')) { btn.setAttribute('data-busy-html', btn.innerHTML); }
            btn.classList.add('is-sending');
            btn.disabled = true;
            btn.setAttribute('aria-busy', 'true');
            var spin = document.createElement('span');
            spin.className = 'gl-spin';
            spin.setAttribute('aria-hidden', 'true');
            var text = document.createElement('span');
            text.textContent = label || 'Verzenden…';
            btn.textContent = '';
            btn.appendChild(spin);
            btn.appendChild(text);
        } else {
            var html = btn.getAttribute('data-busy-html');
            btn.classList.remove('is-sending');
            btn.disabled = false;
            btn.removeAttribute('aria-busy');
            if (html !== null) { btn.innerHTML = html; btn.removeAttribute('data-busy-html'); }
        }
    };
})();
