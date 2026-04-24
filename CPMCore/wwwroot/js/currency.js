// =======================================
// currency.js
// Centrale initialisatie voor geldvelden
// =======================================

window.CurrencyMask = (function () {

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

    // publieke API
    return {
        init: init
    };

})();
