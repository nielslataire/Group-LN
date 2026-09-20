// gl-v2 layout-pilot — shell-wide JS (design-handoff/). Loaded once from _LayoutV2.cshtml, so it
// runs on every gl-v2 page: rail flyouts (hover/klik-open, Esc/buiten-klik-toe, optie 2b) and het
// mobiele hoofdmenu-paneel (optie 3b). Puur navigatie-chrome, geen pagina-specifieke logica —
// die hoort in een eigen <pagina>.js (zie gl-v2-invoices.js voor het patroon).
(function () {
    "use strict";

    initRailFlyouts();
    initMobileMenu();
    initMobileQuickActions();

    function initRailFlyouts() {
        var closeTimer = null;

        function closeAll(exceptId) {
            document.querySelectorAll(".gl-v2-flyout.is-open").forEach(function (fly) {
                if (fly.id === exceptId) return;
                fly.classList.remove("is-open");
                var trigger = document.querySelector('[data-flyout="' + fly.id + '"]');
                if (trigger) {
                    trigger.setAttribute("aria-expanded", "false");
                    // .is-flyout-open drives the "tile stays active-green while its panel is
                    // open" treatment (design-handoff 5a/5b) — separate from .is-active (the real
                    // current-page state, server-rendered), so closing a flyout never touches
                    // whether this item is actually the current page.
                    trigger.classList.remove("is-flyout-open");
                }
            });
        }

        function positionFlyout(trigger, fly) {
            var rect = trigger.getBoundingClientRect();
            fly.style.left = (rect.right + 8) + "px";
            var top = rect.top;
            var maxTop = window.innerHeight - fly.offsetHeight - 12;
            fly.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        }

        function openFlyout(trigger) {
            var id = trigger.getAttribute("data-flyout");
            var fly = document.getElementById(id);
            if (!fly) return;
            closeAll(id);
            fly.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            trigger.classList.add("is-flyout-open");
            positionFlyout(trigger, fly);
        }

        document.querySelectorAll(".js-gl-v2-flyout-trigger").forEach(function (trigger) {
            // Klik zorgt altijd dat het paneel OPEN is — geen toggle-to-close hier. Met een echte
            // muis (ook bij tablet-emulatie in devtools) vuurt mouseenter al vóór de klik en opent
            // het paneel dus al; een toggle op click zou het dan onmiddellijk weer sluiten, zodat
            // je na de klik enkel de hover-tooltip nog zag i.p.v. het submenu. Sluiten gebeurt via
            // de bestaande buiten-klik/Esc/mouseleave-afhandeling hieronder.
            trigger.addEventListener("click", function (e) {
                e.preventDefault();
                openFlyout(trigger);
            });
            trigger.addEventListener("mouseenter", function () {
                window.clearTimeout(closeTimer);
                openFlyout(trigger);
            });
            trigger.addEventListener("mouseleave", function () {
                closeTimer = window.setTimeout(function () { closeAll(null); }, 250);
            });
        });

        document.querySelectorAll(".gl-v2-flyout").forEach(function (fly) {
            fly.addEventListener("mouseenter", function () { window.clearTimeout(closeTimer); });
            fly.addEventListener("mouseleave", function () {
                closeTimer = window.setTimeout(function () { closeAll(null); }, 250);
            });
        });

        document.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-flyout") || e.target.closest(".js-gl-v2-flyout-trigger")) return;
            closeAll(null);
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") closeAll(null);
        });
    }

    function initMobileMenu() {
        var menu = document.getElementById("gl-v2-mobile-menu");
        var backdrop = document.getElementById("gl-v2-mobile-backdrop");
        var hamburger = document.getElementById("gl-v2-hamburger-btn");
        var closeBtn = document.getElementById("gl-v2-mobile-menu-close");
        var searchInput = document.getElementById("gl-v2-mobile-search");
        if (!menu || !backdrop || !hamburger) return;

        function openMenu() {
            menu.hidden = false;
            backdrop.hidden = false;
            hamburger.setAttribute("aria-expanded", "true");
            document.body.style.overflow = "hidden";
        }

        function closeMenu() {
            menu.hidden = true;
            backdrop.hidden = true;
            hamburger.setAttribute("aria-expanded", "false");
            document.body.style.overflow = "";
        }

        hamburger.addEventListener("click", openMenu);
        if (closeBtn) closeBtn.addEventListener("click", closeMenu);
        backdrop.addEventListener("click", closeMenu);
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !menu.hidden) closeMenu();
        });

        menu.querySelectorAll(".gl-v2-mobile-group-trigger").forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                var group = document.getElementById(trigger.getAttribute("aria-controls"));
                if (!group) return;
                var willOpen = group.hidden;
                group.hidden = !willOpen;
                trigger.setAttribute("aria-expanded", willOpen ? "true" : "false");
            });
        });

        if (searchInput) {
            searchInput.addEventListener("input", function () {
                var term = searchInput.value.trim().toLowerCase();
                menu.querySelectorAll(".js-gl-v2-mobile-searchable").forEach(function (item) {
                    var label = (item.textContent || "").trim().toLowerCase();
                    var groupId = item.getAttribute("aria-controls");
                    var matches = !term || label.indexOf(term) !== -1;
                    item.style.display = matches ? "" : "none";
                    if (groupId && matches && term) {
                        var group = document.getElementById(groupId);
                        if (group) { group.hidden = false; }
                    }
                });
            });
        }
    }

    // Optie 6b: elke pagina die meer dan 4 @section MobileQuickActions-tegels meegeeft, ziet enkel
    // de eerste 4 als icoon — tegel 5+ verhuist naar een sheet die achter een 5de "Meer"-tegel zit.
    // Puur generiek chrome-gedrag (geen Facturen-specifieke logica), dus werkt ongewijzigd op elke
    // gl-v2-pagina die toevallig meer dan 4 snelacties heeft.
    function initMobileQuickActions() {
        var MAX_VISIBLE = 4;
        var bar = document.getElementById("gl-v2-mobile-quickactions");
        var sheet = document.getElementById("gl-v2-quickactions-sheet");
        var backdrop = document.getElementById("gl-v2-quickactions-backdrop");
        var list = document.getElementById("gl-v2-quickactions-sheet-list");
        var closeBtn = document.getElementById("gl-v2-quickactions-sheet-close");
        if (!bar || !sheet || !backdrop || !list) return;

        var items = Array.prototype.slice.call(bar.querySelectorAll(".gl-v2-mobile-quickaction"));
        if (items.length <= MAX_VISIBLE) return;

        var more = document.createElement("button");
        more.type = "button";
        more.className = "gl-v2-mobile-quickaction gl-v2-mobile-quickaction-more";
        more.setAttribute("aria-haspopup", "dialog");
        more.setAttribute("aria-expanded", "false");
        more.setAttribute("aria-controls", "gl-v2-quickactions-sheet");
        more.innerHTML =
            '<i class="ph ph-dots-three" aria-hidden="true" data-role="rest-icon"></i>' +
            '<i class="ph ph-x" aria-hidden="true" data-role="open-icon" hidden></i>' +
            '<span data-role="rest-label">Meer</span>' +
            '<span data-role="open-label" hidden>Sluiten</span>';

        function setOpen(isOpen) {
            sheet.hidden = !isOpen;
            backdrop.hidden = !isOpen;
            bar.classList.toggle("has-open-sheet", isOpen);
            more.classList.toggle("is-open", isOpen);
            more.setAttribute("aria-expanded", isOpen ? "true" : "false");
            more.querySelector('[data-role="rest-icon"]').hidden = isOpen;
            more.querySelector('[data-role="open-icon"]').hidden = !isOpen;
            more.querySelector('[data-role="rest-label"]').hidden = isOpen;
            more.querySelector('[data-role="open-label"]').hidden = !isOpen;
            document.body.style.overflow = isOpen ? "hidden" : "";
        }

        function buildSheetRow(original) {
            var iconEl = original.querySelector("i");
            var label = (original.textContent || "").trim();
            var subtitle = original.getAttribute("data-subtitle") || "";

            var row = document.createElement("button");
            row.type = "button";
            row.className = "gl-v2-quickactions-sheet-row";

            var iconSpan = document.createElement("span");
            iconSpan.className = "gl-v2-quickactions-sheet-row-icon";
            var icon = document.createElement("i");
            icon.className = iconEl ? iconEl.className : "ph ph-circle";
            icon.setAttribute("aria-hidden", "true");
            iconSpan.appendChild(icon);

            var textSpan = document.createElement("span");
            textSpan.className = "gl-v2-quickactions-sheet-row-text";
            var titleSpan = document.createElement("span");
            titleSpan.className = "gl-v2-quickactions-sheet-row-title";
            titleSpan.textContent = label;
            textSpan.appendChild(titleSpan);
            if (subtitle) {
                var subtitleSpan = document.createElement("span");
                subtitleSpan.className = "gl-v2-quickactions-sheet-row-subtitle";
                subtitleSpan.textContent = subtitle;
                textSpan.appendChild(subtitleSpan);
            }

            var chevron = document.createElement("i");
            chevron.className = "ph ph-caret-right gl-v2-quickactions-sheet-row-chevron";
            chevron.setAttribute("aria-hidden", "true");

            row.appendChild(iconSpan);
            row.appendChild(textSpan);
            row.appendChild(chevron);

            // Delegeert naar het originele (verborgen) element i.p.v. href/form-submit te
            // dupliceren — respecteert zo vanzelf een disabled knop (click() op disabled doet
            // niets) zonder de disabled-state hier apart te moeten bijhouden.
            row.addEventListener("click", function () {
                setOpen(false);
                original.click();
            });
            return row;
        }

        items.slice(MAX_VISIBLE).forEach(function (item) {
            item.hidden = true;
            list.appendChild(buildSheetRow(item));
        });
        bar.appendChild(more);

        more.addEventListener("click", function () { setOpen(sheet.hidden); });
        backdrop.addEventListener("click", function () { setOpen(false); });
        if (closeBtn) closeBtn.addEventListener("click", function () { setOpen(false); });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !sheet.hidden) setOpen(false);
        });
    }
})();
