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
        });
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
