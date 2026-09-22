// gl-v2 layout-pilot — shell-wide JS (design-handoff/). Loaded once from _LayoutV2.cshtml, so it
// runs on every gl-v2 page: rail flyouts (hover/klik-open, Esc/buiten-klik-toe, optie 2b) and het
// mobiele hoofdmenu-paneel (optie 3b). Puur navigatie-chrome, geen pagina-specifieke logica —
// die hoort in een eigen <pagina>.js (zie gl-v2-invoices.js voor het patroon).
(function () {
    "use strict";

    initRailFlyouts();
    initMobileMenu();
    initMobileQuickActions();
    initModalButtonLoading();
    initToasts();
    initKpiToggle();
    initContextMenus();
    initTableEmailActions();
    initClickableRows();

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

    // Design-handoff 4j's "bezig"-knopstaat, generiek toegepast op elke Type 1/2-modal: een submit
    // op een <form> binnen een `.gl-v2-modal-confirm`/`.gl-v2-modal-form` zet z'n eigen submit-knop
    // automatisch op "bezig" (label + .is-loading + disabled), geen per-pagina JS nodig. Gedelegeerd
    // op `document` i.p.v. bij het laden eenmalig querySelectorAll — TYPE 1's meest voorkomende
    // gebruik (bv. de facturen-verwijdermodal) laadt zijn `<form>` pas via AJAX ná een klik, dus die
    // bestaat nog niet wanneer dit script initieel draait; een gedelegeerde listener vangt 'm alsnog.
    function initModalButtonLoading() {
        document.addEventListener("submit", function (e) {
            var form = e.target;
            if (!(form.closest(".gl-v2-modal-confirm") || form.closest(".gl-v2-modal-form"))) return;
            var btn = form.querySelector('button[type="submit"]');
            if (btn) setButtonLoading(btn);
        });
    }

    function setButtonLoading(btn, label) {
        if (btn.dataset.glV2Loading === "1") return;
        btn.dataset.glV2Loading = "1";
        btn.dataset.glV2RestoreLabel = btn.textContent;
        btn.textContent = label || "Bezig …";
        btn.classList.add("is-loading");
        btn.disabled = true;
    }

    function clearButtonLoading(btn) {
        if (btn.dataset.glV2Loading !== "1") return;
        btn.textContent = btn.dataset.glV2RestoreLabel || btn.textContent;
        btn.classList.remove("is-loading");
        btn.disabled = false;
        delete btn.dataset.glV2Loading;
    }

    // Zelfde helper beschikbaar voor knop-getriggerde (niet-formulier) bevestigacties in een
    // pagina-eigen script (zie gl-v2-invoices.js voor het patroon) — bv. #confirmIssueInvoice, dat
    // via AJAX afhandelt i.p.v. een echte form-submit en dus niet door de listener hierboven gevangen
    // wordt.
    window.GlV2Modal = { setButtonLoading: setButtonLoading, clearButtonLoading: clearButtonLoading };

    // Design-handoff 4g "MELDINGEN — TOASTS" — generiek, projectbreed meldingensysteem. Container
    // (#gl-v2-toast-container) leeft eenmalig in _LayoutV2.cshtml; elke gl-v2-pagina (of _LayoutV2
    // zelf, voor de bestaande TempData-meldingen) roept enkel window.GlV2Toast.show({...}) aan.
    function initToasts() {
        var MAX_VISIBLE = 3;
        var AUTO_DISMISS_MS = 5000;
        var container = document.getElementById("gl-v2-toast-container");
        if (!container) return;

        var TONE_ICONS = { success: "ph-check-circle", danger: "ph-warning-circle", warning: "ph-warning", info: "ph-info" };

        function show(opts) {
            opts = opts || {};
            var tone = TONE_ICONS[opts.tone] ? opts.tone : "info";

            var el = document.createElement("div");
            el.className = "gl-v2-toast is-" + tone;
            el.setAttribute("role", "status");

            var icon = document.createElement("span");
            icon.className = "gl-v2-toast-icon";
            var iconGlyph = document.createElement("i");
            iconGlyph.className = "ph " + (opts.icon || TONE_ICONS[tone]);
            iconGlyph.setAttribute("aria-hidden", "true");
            icon.appendChild(iconGlyph);
            el.appendChild(icon);

            var title = document.createElement("span");
            title.className = "gl-v2-toast-title";
            title.textContent = opts.title || "";
            el.appendChild(title);

            var body = document.createElement("span");
            body.className = "gl-v2-toast-body";
            body.textContent = opts.body || "";
            el.appendChild(body);

            if (opts.action) {
                var action = document.createElement("button");
                action.type = "button";
                action.className = "gl-v2-toast-action";
                action.textContent = opts.action;
                action.addEventListener("click", function () {
                    dismiss(el);
                    if (typeof opts.onAction === "function") opts.onAction();
                });
                el.appendChild(action);
            }

            var close = document.createElement("button");
            close.type = "button";
            close.className = "gl-v2-toast-close";
            close.setAttribute("aria-label", "Sluiten");
            var closeGlyph = document.createElement("i");
            closeGlyph.className = "ph ph-x";
            closeGlyph.setAttribute("aria-hidden", "true");
            close.appendChild(closeGlyph);
            close.addEventListener("click", function () { dismiss(el); });
            el.appendChild(close);

            enableSwipeDismiss(el);

            // column-reverse op de container: als laatste kind toegevoegd betekent hier "onderaan de
            // stapel, dicht bij de hoek" — precies waar een nieuwe melding hoort te verschijnen.
            container.appendChild(el);
            enforceMax();

            // Fouten blijven staan tot ze gesloten worden (4g's eigen regel); elke andere toon
            // verdwijnt na 5 sec. vanzelf.
            if (tone !== "danger" && !opts.sticky) {
                el.dataset.glV2Timer = window.setTimeout(function () { dismiss(el); }, AUTO_DISMISS_MS);
            }

            return el;
        }

        function dismiss(el) {
            if (!el || !el.isConnected) return;
            window.clearTimeout(Number(el.dataset.glV2Timer));
            el.remove();
        }

        function enforceMax() {
            var toasts = container.querySelectorAll(".gl-v2-toast");
            for (var idx = 0; idx < toasts.length - MAX_VISIBLE; idx++) {
                dismiss(toasts[idx]);
            }
        }

        // Tablet/mobiel tonen geen kruisje (gl-v2-shell.css) — "vegen naar rechts sluit" is op die
        // formaten de enige manier om een melding (met name een blijvende foutmelding) handmatig weg
        // te doen. Werkt via Pointer Events (muis én touch in één handler) — op desktop is dit een
        // bonus naast het altijd-aanwezige kruisje, geen vervanging.
        function enableSwipeDismiss(el) {
            var startX = null;
            var dx = 0;

            el.addEventListener("pointerdown", function (e) {
                startX = e.clientX;
                dx = 0;
                el.setPointerCapture(e.pointerId);
                el.style.transition = "none";
            });
            el.addEventListener("pointermove", function (e) {
                if (startX === null) return;
                dx = e.clientX - startX;
                if (dx > 0) el.style.transform = "translateX(" + dx + "px)";
            });
            function end() {
                if (startX === null) return;
                el.style.transition = "";
                if (dx > 80) {
                    el.style.transform = "translateX(120%)";
                    el.style.opacity = "0";
                    window.setTimeout(function () { dismiss(el); }, 150);
                } else {
                    el.style.transform = "";
                }
                startX = null;
                dx = 0;
            }
            el.addEventListener("pointerup", end);
            el.addEventListener("pointercancel", end);
        }

        window.GlV2Toast = { show: show, dismiss: dismiss };
    }

    // Design-handoff 7a's mobiele "Toon alles" — gedelegeerd op document, generiek voor elke
    // .gl-v2-kpi-strip op elke gl-v2-pagina (niet dashboard-specifiek, dezelfde reden als de andere
    // gedelegeerde listeners hierboven: werkt ook op een strip die pas later in de DOM verschijnt).
    function initKpiToggle() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".js-gl-v2-kpi-toggle");
            if (!btn) return;
            var strip = btn.closest(".gl-v2-kpi-strip");
            if (!strip) return;
            var expanded = strip.classList.toggle("is-expanded");
            btn.setAttribute("aria-expanded", expanded ? "true" : "false");
            var expandLabel = btn.querySelector('[data-role="expand-label"]');
            var collapseLabel = btn.querySelector('[data-role="collapse-label"]');
            if (expandLabel) expandLabel.hidden = expanded;
            if (collapseLabel) collapseLabel.hidden = !expanded;
        });
    }

    // Generiek contextmenu (extractie van Invoices' "···"-rijmenu-patroon, zie gl-v2-shell.css voor
    // de volledige uitleg) — trigger: elk element met .js-gl-v2-menu-trigger + aria-controls="<menu-
    // id>". Enkel open/positie/sluiten hoort hier; WAT er in het paneel staat (en of een klik op een
    // item het meteen moet sluiten, bv. "Eigen datum kiezen" swapt liever van inhoud dan te sluiten)
    // is aan de pagina-eigen JS — window.GlV2Menu.closeAll() staat daarvoor open.
    function initContextMenus() {
        var resizeTimer = null;
        var backdrop = document.getElementById("gl-v2-menu-backdrop");

        function closeAllMenus() {
            document.querySelectorAll(".gl-v2-menu.is-open").forEach(function (menu) {
                menu.classList.remove("is-open");
                var trigger = document.querySelector('.js-gl-v2-menu-trigger[aria-controls="' + menu.id + '"]');
                if (trigger) {
                    trigger.classList.remove("is-menu-open");
                    trigger.setAttribute("aria-expanded", "false");
                }
            });
            if (backdrop) backdrop.classList.remove("is-open");
        }

        // <768px: CSS zet het paneel zelf vast als bottom sheet (geen !important nodig) — JS slaat
        // positionering dan gewoon over, zelfde "sla het gewoon over" aanpak als overal elders in
        // gl-v2 (boekjaar-paneel, rij-···-menu's) i.p.v. een inline top/left te zetten die de sheet-
        // CSS zou moeten overstemmen. Moet de vorige keer se inline top/left WEL expliciet wissen
        // (niet enkel geen nieuwe zetten): een venster dat op tablet-breedte al eens gepositioneerd
        // werd en dan smaller wordt zonder herlaad (devtools-resize, geen page reload) hield anders
        // die oude inline waarden vast — inline style wint altijd van de sheet-CSS, ongeacht
        // specificiteit, dus de bottom-sheet-regels leken dan niets te doen terwijl de kaart in
        // werkelijkheid nog op haar oude, te-smalle tablet-positie/-breedte vastzat.
        function positionMenu(trigger, menu) {
            if (window.innerWidth < 768) {
                menu.style.top = "";
                menu.style.left = "";
                return;
            }
            var rect = trigger.getBoundingClientRect();
            var menuWidth = menu.offsetWidth || 224;
            var left = Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 12);
            menu.style.left = Math.max(12, left) + "px";
            var top = rect.bottom + 6;
            var maxTop = window.innerHeight - menu.offsetHeight - 12;
            menu.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        }

        document.addEventListener("click", function (e) {
            var trigger = e.target.closest(".js-gl-v2-menu-trigger");
            if (!trigger) return;
            e.preventDefault();
            e.stopPropagation();
            var menu = document.getElementById(trigger.getAttribute("aria-controls"));
            if (!menu) return;
            var wasOpen = menu.classList.contains("is-open");
            closeAllMenus();
            if (wasOpen) return;
            menu.classList.add("is-open");
            trigger.classList.add("is-menu-open");
            trigger.setAttribute("aria-expanded", "true");
            if (backdrop) backdrop.classList.add("is-open");
            positionMenu(trigger, menu);
        });

        document.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-menu") || e.target.closest(".js-gl-v2-menu-trigger")) return;
            closeAllMenus();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") closeAllMenus();
        });
        if (backdrop) backdrop.addEventListener("click", closeAllMenus);
        window.addEventListener("resize", function () {
            window.clearTimeout(resizeTimer);
            resizeTimer = window.setTimeout(closeAllMenus, 100);
        });

        // reposition: voor pagina-eigen JS dat de INHOUD van een al-open paneel verandert (bv.
        // snooze-opties -> eigen-datum-kalender, andere hoogte/breedte) en de zwevende positie
        // opnieuw wil laten berekenen zonder het paneel te moeten sluiten/heropenen.
        window.GlV2Menu = { closeAll: closeAllMenus, reposition: positionMenu };
    }

    // Tabel: e-mail-cel (design-handoff 8g) — de "kopiëren"-knop is de enige interactieve stap die
    // geen native navigatie is (mailen is gewoon een mailto:-link), vandaar de enige echte handler
    // hier. Gedelegeerd op document, werkt dus ook voor rijen die een DataTable later injecteert.
    function initTableEmailActions() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".js-gl-v2-copy-email");
            if (!btn) return;
            e.preventDefault();
            e.stopPropagation();
            var email = btn.getAttribute("data-email");
            if (!email || !navigator.clipboard) return;
            navigator.clipboard.writeText(email).then(function () {
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "success", title: "Gekopieerd", body: email });
            });
        });
    }

    // Rij → detail, knoppen in de rij zijn de uitzondering. Opt-in via data-detail-url op de <tr>
    // (Leveranciers/Klanten-tabellen vandaag) — elke klik die binnen een <a>/<button>/form-element
    // van de rij gebeurt (naam-link, "···"-rijmenu en z'n items) doet gewoon haar eigen ding, de rij
    // navigeert dan niet nog eens extra.
    function initClickableRows() {
        document.addEventListener("click", function (e) {
            var row = e.target.closest("tr[data-detail-url]");
            if (!row) return;
            if (e.target.closest("a, button, input, select, textarea, label")) return;
            window.location.href = row.getAttribute("data-detail-url");
        });
    }
})();
