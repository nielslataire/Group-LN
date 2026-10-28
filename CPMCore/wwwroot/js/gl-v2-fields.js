// gl-v2 — invoervelden: valuta (AutoNumeric), telefoon/gsm en spaties-trimmen. Uit gl-v2-shell.js gelicht (26/10/2026, consolidatie stap 11): zelfstandige IIFE's, geladen direct na gl-v2-shell.js.

// ── Valutavelden (EditorTemplates/GlV2Currency, .Currencymask) — AutoNumeric + currency.js laden
//    sinds de layout ze meelevert op élke gl-v2-pagina (zie _LayoutV2.cshtml); hier één keer
//    initialiseren zodat een pagina dat niet zelf hoeft te doen. currency.js se init() slaat al
//    geïnitialiseerde velden over, dus pagina's die 'm zelf ook aanroepen (na een dynamisch
//    toegevoegde rij) doen niets dubbel. ────────────────────────────────────────────────────────
(function () {
    "use strict";
    if (window.CurrencyMask && typeof window.CurrencyMask.init === "function") {
        window.CurrencyMask.init(".Currencymask");
    }
})();

// ── Telefoon-/gsm-velden (EditorTemplates/GlV2Telefoon.cshtml/GlV2Gsm.cshtml) — bewerkbaar
//    landcode-voorvoegsel, project-wijd. Het zichtbare paar (voorvoegsel-select + nummerveld)
//    bindt niet zelf naar de server; hun waarden worden hier samengevoegd in de verborgen
//    input ([data-role="phone-value"], de echte, post'ende naam) telkens wanneer één van beide
//    wijzigt — "+32 495123456", zelfde formaat als BOCore.GlV2PhonePrefixes.Combine() server-
//    side. Ook de tel:/sms:-actielink volgt mee. Shell-breed geladen (élke gl-v2-pagina), dus een
//    pagina hoeft dit voor haar eigen velden niet zelf te doen — enkel voor een NA het laden
//    dynamisch toegevoegde rij (bv. "+ Mede-eigenaar toevoegen") roept de pagina zelf
//    window.GlV2Phone.init(rowEl) aan, zelfde conventie als window.GlV2Select.init(scope). Idempotent
//    (data-gl-v2-phone-wired-marker), dus een dubbele aanroep doet niets dubbel. ─────────────────
(function () {
    "use strict";

    function initGlV2PhoneFields(scope) {
        (scope || document).querySelectorAll('[data-role="phone-box"]').forEach(function (box) {
            if (box.dataset.glV2PhoneWired) return;
            var prefixSelect = box.querySelector('[data-role="phone-prefix"]');
            var numberInput = box.querySelector('[data-role="phone-number"]');
            var hidden = box.querySelector('[data-role="phone-value"]');
            var action = box.querySelector('[data-role="phone-action"]');
            if (!prefixSelect || !numberInput || !hidden) return;
            box.dataset.glV2PhoneWired = "1";
            var scheme = action && action.getAttribute("href") && action.getAttribute("href").indexOf("sms:") === 0 ? "sms:" : "tel:";

            // Een geplakt nummer met landcode ("+32472171513", "0032 472 17 15 13") zet het voorvoegsel zelf en laat enkel het nummer staan.
            function splitPastedPrefix() {
                var m = /^\s*(?:\+|00)\s*(\d[\d\s().-]*)$/.exec(numberInput.value);
                if (!m) return;
                var digits = m[1].replace(/\D/g, "");
                var codes = Array.prototype.map.call(prefixSelect.options, function (o) { return o.value; })
                    .filter(function (c) { return /^\+\d+$/.test(c); })
                    .sort(function (a, b) { return b.length - a.length; });
                for (var i = 0; i < codes.length; i++) {
                    var cd = codes[i].slice(1);
                    if (digits.indexOf(cd) === 0 && digits.length > cd.length) {
                        prefixSelect.value = codes[i];
                        numberInput.value = digits.slice(cd.length);
                        return;
                    }
                }
            }

            function sync() {
                splitPastedPrefix();
                var number = numberInput.value.trim();
                var value = number ? (prefixSelect.value + " " + number) : "";
                hidden.value = value;
                if (action) action.setAttribute("href", scheme + value);
            }
            prefixSelect.addEventListener("change", sync);
            numberInput.addEventListener("input", sync);
        });
    }

    initGlV2PhoneFields();
    window.GlV2Phone = { init: initGlV2PhoneFields };
})();

// ── Spaties vooraan en achteraan weghalen — project-wijd, op elk tekstveld van de gl-v2-layout. Plakken uit Excel, mail of een website
//    neemt vaak een spatie mee ("Jan "), wat later bij zoeken, dubbelcontrole en exports voor onverklaarbare verschillen zorgt. Zodra je het
//    veld verlaat (of het formulier indient, ook met Enter) wordt de waarde getrimd; een wachtwoordveld en alles met data-gl-v2-no-trim blijft
//    ongemoeid. Als er iets verandert, krijgt het veld een gewoon "input"-event, zodat maskers en pagina-JS (bv. het telefoonveld) meelopen. ─────
(function () {
    "use strict";
    function trimField(el) {
        if (!el || el.readOnly || el.disabled || el.hasAttribute("data-gl-v2-no-trim")) return;
        var tag = el.tagName;
        if (tag !== "INPUT" && tag !== "TEXTAREA") return;
        if (tag === "INPUT" && !/^(text|search|tel|email|url)$/i.test(el.type || "text")) return;
        var v = el.value;
        if (typeof v !== "string") return;
        var t = v.replace(/^[\s ]+|[\s ]+$/g, "");
        if (t === v) return;
        el.value = t;
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
    }
    document.addEventListener("focusout", function (e) { trimField(e.target); });
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (form && form.querySelectorAll) Array.prototype.forEach.call(form.querySelectorAll("input, textarea"), trimField);
    }, true);
})();
