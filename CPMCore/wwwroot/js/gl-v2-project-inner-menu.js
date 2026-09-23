// gl-v2 layout-pilot — projectdossier inner menu (design-handoff punt 9). Laadt enkel op pagina's die
// GlV2/_ProjectInnerMenuV2 renderen (zie @section PageScripts in Projecten/DetailV2.cshtml), dus geen
// shell-brede gl-v2-shell.js-uitbreiding — zelfde "pagina-eigen JS in een eigen bestand"-conventie als
// gl-v2-invoices.js.
//
// De pagina roept de partial twee keer aan (Outer-mode in .gl-v2-project-menu-slot, Phone-mode inline
// in <main>) — beide staan dus altijd in de DOM, CSS toont enkel de ene of de andere per breedte. Om
// die reden itereert dit bestand over ALLE ".gl-v2-project-menu"-wortels i.p.v. er één te pakken.
//
// Tablet (9b): de icoonkolom is gewone <a>-links met een CSS-only hover/focus-tooltip
// (transition-delay). Touch heeft geen hover-status, dus initTabletRailPeek hieronder wapent de
// eerste tik op een echt touch-toestel i.p.v. meteen te navigeren — pas de tweede tik op dezelfde
// icoon laat de link zijn ding doen. Toetsenbord/muis-gebruikers (waar :focus-visible/:hover al
// werken) raken dit pad niet, de check is (hover: none) and (pointer: coarse).
(function () {
    "use strict";

    var roots = document.querySelectorAll(".gl-v2-project-menu");
    if (!roots.length) return;

    roots.forEach(function (root) {
        initSearch(root);
        initPhoneSheet(root);
        initTabletRailPeek(root);
    });

    // ── Tablet iconenkolom (9b) — eerste tik toont het label, tweede tik navigeert. ──
    function initTabletRailPeek(root) {
        var rail = root.querySelector(".gl-v2-pm-tablet");
        if (!rail) return;
        var isTouch = window.matchMedia && window.matchMedia("(hover: none) and (pointer: coarse)").matches;
        if (!isTouch) return;

        var armed = null;

        function disarm() {
            if (armed) armed.classList.remove("is-peeking");
            armed = null;
        }

        rail.querySelectorAll(".gl-v2-pm-rail-icon").forEach(function (icon) {
            icon.addEventListener("click", function (e) {
                if (armed === icon) {
                    // Tweede tik op dezelfde icoon: laat de <a> gewoon navigeren.
                    armed = null;
                    return;
                }
                e.preventDefault();
                disarm();
                icon.classList.add("is-peeking");
                armed = icon;
            });
        });

        document.addEventListener("click", function (e) {
            if (armed && !armed.contains(e.target)) disarm();
        });
    }

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
