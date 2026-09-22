// gl-v2 layout-pilot — projectdossier inner menu (design-handoff punt 9). Laadt enkel op pagina's die
// GlV2/_ProjectInnerMenuV2 renderen (zie @section PageScripts in Projecten/DetailV2.cshtml), dus geen
// shell-brede gl-v2-shell.js-uitbreiding — zelfde "pagina-eigen JS in een eigen bestand"-conventie als
// gl-v2-invoices.js.
//
// De pagina roept de partial twee keer aan (Outer-mode in .gl-v2-project-menu-slot, Phone-mode inline
// in <main>) — beide staan dus altijd in de DOM, CSS toont enkel de ene of de andere per breedte. Om
// die reden itereert dit bestand over ALLE ".gl-v2-project-menu"-wortels i.p.v. er één te pakken.
//
// Tablet (9b) heeft geen eigen JS meer nodig: de icoonkolom bestaat enkel nog uit gewone <a>-links
// (navigatie + CSS-only tooltip via transition-delay), geen flyout-paneel meer om open/dicht te
// houden.
(function () {
    "use strict";

    var roots = document.querySelectorAll(".gl-v2-project-menu");
    if (!roots.length) return;

    roots.forEach(function (root) {
        initSearch(root);
        initPhoneSheet(root);
    });

    // ── Zoekfilter — design-handoff 9a §3: filtert de groepen, lege groepen verdwijnen. ──
    function initSearch(root) {
        var inputs = root.querySelectorAll(".js-gl-v2-pm-search");
        inputs.forEach(function (input) {
            var list = input.closest(".gl-v2-pm-desktop, .gl-v2-pm-sheet");
            if (!list) return;
            var itemSelector = ".gl-v2-pm-item, .gl-v2-pm-sheet-item";
            input.addEventListener("input", function () {
                var term = input.value.trim().toLowerCase();
                var anyVisible = false;
                list.querySelectorAll("[data-pm-group]").forEach(function (group) {
                    var groupHasMatch = false;
                    group.querySelectorAll(itemSelector).forEach(function (item) {
                        var text = item.getAttribute("data-pm-item-text") || "";
                        var matches = !term || text.indexOf(term) !== -1;
                        item.hidden = !matches;
                        if (matches) groupHasMatch = true;
                    });
                    group.hidden = !groupHasMatch;
                    if (groupHasMatch) anyVisible = true;
                });
                var empty = list.querySelector("[data-pm-empty]");
                if (empty) empty.hidden = !term || anyVisible;
            });
        });
    }

    // ── Gsm-blad (design-handoff 9c) — modaal over de volle hoogte, tikken op een ingang navigeert
    // gewoon (native <a>), sluiten hoeft dus enkel op open/dicht-toggle + achtergrond-scroll-lock. ──
    function initPhoneSheet(root) {
        var openBtn = root.querySelector(".js-gl-v2-pm-open");
        var sheet = root.querySelector(".gl-v2-pm-sheet");
        var backdrop = root.querySelector(".gl-v2-pm-sheet-backdrop");
        if (!openBtn || !sheet || !backdrop) return;

        function open() {
            sheet.hidden = false;
            backdrop.hidden = false;
            openBtn.setAttribute("aria-expanded", "true");
            document.body.style.overflow = "hidden";
            var firstFocusable = sheet.querySelector("input, a, button");
            if (firstFocusable) firstFocusable.focus({ preventScroll: true });
        }

        function close() {
            sheet.hidden = true;
            backdrop.hidden = true;
            openBtn.setAttribute("aria-expanded", "false");
            document.body.style.overflow = "";
        }

        openBtn.addEventListener("click", open);
        root.querySelectorAll(".js-gl-v2-pm-close").forEach(function (btn) {
            btn.addEventListener("click", close);
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !sheet.hidden) close();
        });
    }
})();
