// =======================================
// currency.js
// Centrale initialisatie voor geldvelden
// =======================================

// Eén keer laden: een pagina die dit script (of de layout) dubbel insluit mag het niet opnieuw definiëren.
if (!window.CurrencyMask) window.CurrencyMask = (function () {

    const defaultOptions = {
        decimalCharacter: ',',
        decimalCharacterAlternative: '.',
        digitGroupSeparator: '.',
        decimalPlaces: 2,
        decimalPlacesShownOnFocus: 2,
        decimalPlacesShownOnBlur: 2,
        unformatOnSubmit: true,   // POST altijd 128.10
        modifyValueOnWheel: false,
        outputFormat: 'number',
    };

    function init(target) {
        let elements = [];

        if (!target) {
            elements = document.querySelectorAll('.Currencymask');
        } else if (typeof target === 'string') {
            elements = document.querySelectorAll(target);
        } else if (target instanceof HTMLElement) {
            elements = [target];
        } else if (target instanceof NodeList || Array.isArray(target)) {
            elements = target;
        } else if (target && typeof target === 'object' && typeof target.toArray === 'function') {
            elements = target.toArray();
        }

        elements.forEach(el => {
            if (!el || el.disabled) return;

            const existing = AutoNumeric.getAutoNumericElement(el);
            if (existing) {
                existing.remove();
            }

            new AutoNumeric(el, defaultOptions);
            watchSuffix(el);
        });
    }

    // Een geldveld met een €-teken (.gl-v2-field-suffix) in een te smal vak: het teken verdwijnt (klasse
    // is-suffix-hidden op het vak) zodra de waarde anders niet meer volledig zichtbaar is. Het teken komt terug
    // als het vak weer breed genoeg is of de waarde korter wordt.
    function fitSuffix(el) {
        const box = el.closest('.gl-v2-field-box');
        if (!box || !box.querySelector('.gl-v2-field-suffix')) return;
        box.classList.remove('is-suffix-hidden');
        if (el.scrollWidth > el.clientWidth + 1) box.classList.add('is-suffix-hidden');
    }

    function watchSuffix(el) {
        const box = el.closest('.gl-v2-field-box');
        if (!box || !box.querySelector('.gl-v2-field-suffix') || el.dataset.suffixWatched) return;
        el.dataset.suffixWatched = '1';
        const fit = () => fitSuffix(el);
        ['input', 'change', 'blur', 'focus', 'autoNumeric:formatted', 'autoNumeric:rawValueModified'].forEach(ev => el.addEventListener(ev, fit));
        if (window.ResizeObserver) new ResizeObserver(fit).observe(box);
        fit();
    }

    // Een formulier dat via new FormData(form) verstuurd wordt (autosave) slaat de submit-unformat over: "1.500" zou als 1,5 gelezen worden.
    // Daarom krijgt elk .Currencymask-veld hier het kale getal (1500.00) in de FormData.
    document.addEventListener('formdata', function (e) {
        const form = e.target;
        if (!form || !form.querySelectorAll) return;
        form.querySelectorAll('.Currencymask').forEach(function (el) {
            if (!el.name || !e.formData.has(el.name)) return;
            const an = window.AutoNumeric && AutoNumeric.getAutoNumericElement(el);
            if (an) { const v = an.getNumericString(); e.formData.set(el.name, v == null ? '' : v); }
        });
    }, true);

    // publieke API
    return {
        init: init
    };

})();
