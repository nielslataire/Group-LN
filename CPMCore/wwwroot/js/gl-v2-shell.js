// gl-v2 layout-pilot — shell-wide JS (design-handoff/). Loaded once from _LayoutV2.cshtml, so it
// runs on every gl-v2 page: rail flyouts (hover/klik-open, Esc/buiten-klik-toe, optie 2b) and het
// mobiele hoofdmenu-paneel (optie 3b). Puur navigatie-chrome, geen pagina-specifieke logica —
// die hoort in een eigen <pagina>.js (zie gl-v2-invoices.js voor het patroon).
(function () {
    "use strict";

    initRailFlyouts();
    initMobileMenu();
    initGlV2TopbarPin();
    initMobileQuickActions();
    initModalButtonLoading();
    initToasts();
    initKpiToggle();
    initContextMenus();
    initTableEmailActions();
    initClickableRows();
    initGlV2Select();
    initGlV2DatePicker();
    initGlV2Steps();

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

    // Vastgeprikte terug-/menuknop (gsm, okt. 2026): de topbar zelf scrollt gewoon mee (geen
    // position:sticky/fixed), dus deze twee ronde knoppen zijn een visueel duplicaat dat pas
    // verschijnt zodra .gl-v2-topbar volledig uit beeld gescrold is — zelfde IntersectionObserver-
    // patroon als de "weggescrold"-foutenbalk (gl-v2-error-summary.js se refreshStickyVisibility).
    // De geprikte terugknop is een gewone <a href>, geen JS nodig; de geprikte hamburger stuurt zijn
    // klik gewoon door naar de echte knop i.p.v. het open/dicht-gedrag hier te dupliceren.
    function initGlV2TopbarPin() {
        var pin = document.getElementById("gl-v2-topbar-pin");
        var topbar = document.querySelector(".gl-v2-topbar");
        if (!pin || !topbar) return;

        var pinHamburger = document.getElementById("gl-v2-topbar-pin-hamburger-btn");
        var realHamburger = document.getElementById("gl-v2-hamburger-btn");
        if (pinHamburger && realHamburger) {
            pinHamburger.addEventListener("click", function () { realHamburger.click(); });
        }

        if (!("IntersectionObserver" in window)) return;
        var observer = new IntersectionObserver(function (entries) {
            pin.classList.toggle("is-visible", !entries[0].isIntersecting);
        }, { threshold: 0 });
        observer.observe(topbar);
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
                // Niet vastgrijpen als de aanraking op het kruisje/de actielink zelf begint — anders
                // herleidt setPointerCapture (hieronder) de bijhorende pointerup/click naar `el` i.p.v.
                // die knop, en lijkt het kruisje niets te doen (het handje/CSS was altijd al in orde,
                // enkel de klik zelf kwam nooit aan — projectwijd bug, gevonden via Niels, 2026-09-30).
                if (e.target.closest(".gl-v2-toast-close, .gl-v2-toast-action")) return;
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

    // Rij/kaart → detail, knoppen erbinnen zijn de uitzondering. Opt-in via data-detail-url — niet
    // enkel op een <tr> (Leveranciers/Klanten-tabellen), ook op eender welk ander element (bv. een
    // .gl-v2-set-card op Instellingen/IndexV2, punt 24a "de hele kaart is klikbaar") — elke klik die
    // binnen een <a>/<button>/form-element van dat element gebeurt (naam-link, "···"-rijmenu en z'n
    // items, een kaart se eigen chip) doet gewoon haar eigen ding, het element navigeert dan niet
    // nog eens extra.
    function initClickableRows() {
        document.addEventListener("click", function (e) {
            var row = e.target.closest("[data-detail-url]");
            if (!row) return;
            if (e.target.closest("a, button, input, select, textarea, label")) return;
            window.location.href = row.getAttribute("data-detail-url");
        });
    }

    // Stappenplan (design-handoff punt 27, zie DESIGN.md "Stappenplan"): een klikbare stap zonder
    // Href rendert als <button data-gl-v2-steps-step> (Views/Shared/GlV2/_Steps.cshtml) — hier enkel
    // een afgevaardigde listener die dat doorgeeft als een CustomEvent op de wrapper
    // (#id van GlV2StepsVm), nooit zelf navigerend. Een toekomstige in-paginawizard (geen losse
    // pagina per stap) luistert daarop, bv. document.getElementById('mijn-stappen')
    // .addEventListener('gl-v2-steps:step', function (e) { ... e.detail.index ... }). Vandaag heeft
    // nog geen enkele pagina dat nodig (elke stap is ofwel louter weergave, ofwel een echte Href) —
    // dit is enkel het aansluitpunt, klaar om te gebruiken.
    function initGlV2Steps() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest("[data-gl-v2-steps-step]");
            if (!btn) return;
            var wrap = btn.closest(".gl-v2-steps");
            var index = parseInt(btn.getAttribute("data-gl-v2-steps-step"), 10);
            if (wrap) wrap.dispatchEvent(new CustomEvent("gl-v2-steps:step", { bubbles: true, detail: { index: index, id: wrap.id } }));
        });
    }

    // ── GlV2Select — design-handoff optie 4h "Dropdowns — gesloten veld, basis" (zie DESIGN.md
    // "Select / Dropdown" — component al volledig gespecificeerd, tot nu toe enkel per pagina als
    // FILTER gebouwd, elke keer opnieuw dezelfde kleine initSelect()-JS gekopieerd (gl-v2-leveranciers.js,
    // gl-v2-projecten-detailclients.js, …). Dit is diezelfde open/kies-logica, nu ÉÉN keer geschreven
    // en shell-breed, zodat een FORMULIERVELD (bv. EditorTemplates/GlV2Select.cshtml, een echt aan
    // een model gebonden waarde) hem gratis meekrijgt zonder dat de aanroepende pagina zelf nog een
    // initSelect()-kopie hoeft te schrijven — de bestaande filter-dropdowns blijven ongewijzigd op
    // hun eigen page-local kopie, dit vervangt ze niet met terugwerkende kracht.
    // Verwacht per instantie: een .gl-v2-select[data-gl-v2-select] met daarbinnen een verborgen
    // <input> (de gebonden waarde), een .gl-v2-select-trigger en een .gl-v2-select-panel met
    // .gl-v2-select-option[data-value]-knoppen. Bij een keuze: hidden input se waarde + trigger-
    // label bijwerken, .is-selected verplaatsen, een echte "change"-event op de hidden input
    // dispatchen (zodat bestaande $('#id').on('change', …)-logica op diezelfde pagina, bv. een
    // afhankelijk veld in/uitschakelen, gewoon blijft werken — hetzelfde contract als een native
    // <select>'s eigen change-event).
    // Client-side opties vervangen op een bestaand .gl-v2-select[data-gl-v2-select] (afhankelijke
    // keuzelijst). items: [{value, text, group?, disabled?}]. De gekozen waarde blijft behouden als ze
    // nog voorkomt, anders wordt de trigger weer een placeholder. Geen "change" — de aanroeper beslist.
    window.GlV2Select = {
        setItems: function (wrap, items, value, emptyText) {
            if (!wrap) return;
            var hidden = wrap.querySelector("input[type=hidden]");
            var trigger = wrap.querySelector(".gl-v2-select-trigger");
            var panel = wrap.querySelector(".gl-v2-select-panel");
            if (!hidden || !trigger || !panel) return;
            var wanted = value == null ? hidden.value : String(value);
            panel.innerHTML = "";
            function addOption(v, text, isSelected, disabled) {
                var b = document.createElement("button");
                b.type = "button";
                b.className = "gl-v2-select-option" + (isSelected ? " is-selected" : "");
                b.setAttribute("role", "option");
                b.setAttribute("data-value", v);
                if (disabled) b.disabled = true;
                var i = document.createElement("i");
                i.className = "ph ph-check";
                i.setAttribute("aria-hidden", "true");
                var span = document.createElement("span");
                span.textContent = text;
                b.appendChild(i);
                b.appendChild(span);
                panel.appendChild(b);
            }
            var found = null;
            if (emptyText) addOption("", emptyText, wanted === "", false);
            var lastGroup = null;
            items.forEach(function (item) {
                if (item.group && item.group !== lastGroup) {
                    lastGroup = item.group;
                    var h = document.createElement("div");
                    h.className = "gl-v2-select-group-header";
                    h.innerHTML = "<span class=\"gl-v2-select-group-label\"></span><span class=\"gl-v2-select-group-rule\"></span>";
                    h.firstChild.textContent = item.group.toUpperCase();
                    panel.appendChild(h);
                }
                var sel = String(item.value) === wanted;
                if (sel) found = item;
                addOption(String(item.value), item.text, sel, !!item.disabled);
            });
            var label = trigger.querySelector(".gl-v2-select-trigger-label");
            if (found) {
                hidden.value = String(found.value);
                if (label) label.textContent = found.text;
                trigger.classList.add("is-filled");
            } else if (emptyText && wanted === "") {
                hidden.value = "";
                if (label) label.textContent = emptyText;
                trigger.classList.add("is-filled");
            } else {
                hidden.value = "";
                if (label) label.textContent = wrap.getAttribute("data-placeholder") || "Kies …";
                trigger.classList.remove("is-filled");
            }
        }
    };

    var glV2SelectListenersBound; // bewust zonder = false: initGlV2Select() draait al bovenaan, vóór deze regel

    function glV2SelectCloseAll() {
        document.querySelectorAll(".gl-v2-select-panel.is-open[data-gl-v2-owned]").forEach(function (p) {
            p.classList.remove("is-open");
            var trig = p.previousElementSibling;
            if (trig && trig.classList.contains("gl-v2-select-trigger")) {
                trig.classList.remove("is-open");
                trig.setAttribute("aria-expanded", "false");
            }
        });
    }

    function glV2SelectPosition(trigger, panel) {
        var rect = trigger.getBoundingClientRect();
        panel.style.left = rect.left + "px";
        panel.style.top = (rect.bottom + 4) + "px";
        panel.style.width = Math.max(rect.width, 200) + "px";
    }

    // Bedraadt elke nog niet bedraade .gl-v2-select[data-gl-v2-select] binnen scope. Ook publiek
    // (window.GlV2Select.init) voor keuzelijsten die pas na het laden in de DOM komen (bv. een
    // nieuwe rij in een herhaalbare lijst) — de document-brede luisteraars staan er maar één keer.
    function initGlV2Select(scope) {
        var root = scope && scope.querySelectorAll ? scope : document;
        var instances = root.querySelectorAll(".gl-v2-select[data-gl-v2-select]");

        instances.forEach(function (wrap) {
            if (wrap.hasAttribute("data-gl-v2-wired")) return;
            var hidden = wrap.querySelector("input[type=hidden]");
            var trigger = wrap.querySelector(".gl-v2-select-trigger");
            var panel = wrap.querySelector(".gl-v2-select-panel");
            if (!hidden || !trigger || !panel) return;
            wrap.setAttribute("data-gl-v2-wired", "");
            panel.setAttribute("data-gl-v2-owned", "");

            function selectOption(option, fireChange) {
                var value = option.getAttribute("data-value");
                var label = option.querySelector("span") ? option.querySelector("span").textContent : option.textContent;
                panel.querySelectorAll(".gl-v2-select-option").forEach(function (o) { o.classList.toggle("is-selected", o === option); });
                if (hidden.value !== value) {
                    hidden.value = value;
                    if (fireChange !== false) {
                        hidden.dispatchEvent(new Event("change", { bubbles: true }));
                    }
                }
                var labelEl = trigger.querySelector(".gl-v2-select-trigger-label");
                if (labelEl) labelEl.textContent = label;
                trigger.classList.add("is-filled");
            }

            trigger.addEventListener("click", function () {
                if (trigger.getAttribute("aria-disabled") === "true") return;
                var willOpen = !panel.classList.contains("is-open");
                glV2SelectCloseAll();
                if (willOpen) {
                    glV2SelectPosition(trigger, panel);
                    panel.classList.add("is-open");
                    trigger.classList.add("is-open");
                    trigger.setAttribute("aria-expanded", "true");
                }
            });
            trigger.addEventListener("keydown", function (e) {
                if (e.key === "Enter" || e.key === " ") { e.preventDefault(); trigger.click(); }
                if (e.key === "Escape") glV2SelectCloseAll();
            });
            // Gedelegeerd i.p.v. één listener per optie: window.GlV2Select.setItems() vervangt de
            // opties van een afhankelijke keuzelijst (type → subtype) en mag niet opnieuw bedraad
            // hoeven worden. Niet-kiesbare opties (disabled) doen niets.
            panel.addEventListener("click", function (e) {
                var option = e.target.closest(".gl-v2-select-option");
                if (!option || option.disabled || !panel.contains(option)) return;
                selectOption(option, true);
                glV2SelectCloseAll();
                trigger.focus();
            });
        });

        if (glV2SelectListenersBound) return;
        glV2SelectListenersBound = true;
        document.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-select[data-gl-v2-select]")) return;
            glV2SelectCloseAll();
        });
        window.addEventListener("resize", glV2SelectCloseAll);
        // Scrollen BINNEN een open lijst (lange lijst, bv. alle hoofdeenheden) mag die niet sluiten.
        window.addEventListener("scroll", function (e) {
            if (e.target && e.target.closest && e.target.closest(".gl-v2-select-panel")) return;
            glV2SelectCloseAll();
        }, true);
    }
    window.GlV2Select.init = initGlV2Select;

    // Zelfde "opnieuw aanroepbaar voor AJAX-geladen inhoud"-conventie als GlV2Select.init hierboven —
    // eerste gebruik: Projecten/PaymentStagesV2's 21h-modal (Niels, 2026-09-30). Enkel veilig op een
    // pagina zonder een AL bestaand GlV2Date-veld (geen dubbel-init-wacht ingebouwd, zie initGlV2Select
    // voor dat patroon); voor deze modal-only-pagina's is dat geen probleem.
    window.GlV2DatePicker = { init: function () { initGlV2DatePicker(); } };

    // ── Datumkiezer (design-handoff punt 14d "6 · DATUM") — GlV2DateTime.cshtml. Shell-breed net
    //    als initGlV2Select() hierboven (generieke EditorTemplate), maar met een eigen closeAll()/
    //    eigen [data-gl-v2-dp-owned]-marker: de trigger hier is gewoon .gl-v2-field-box, geen
    //    .gl-v2-select-trigger, dus initGlV2Select() se eigen closeAll() zou 'm niet herkennen. ────
    function initGlV2DatePicker() {
        var instances = document.querySelectorAll(".gl-v2-select[data-gl-v2-datepicker]");
        if (!instances.length) return;

        var monthNames = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];

        function closeAll() {
            document.querySelectorAll(".gl-v2-select-panel.is-open[data-gl-v2-dp-owned]").forEach(function (p) {
                p.classList.remove("is-open");
            });
        }

        function positionPanel(trigger, panel) {
            // < 768px: het gedeelde .gl-v2-select-panel-mobielgedrag (bottom sheet, gl-v2-shell.css)
            // moet zijn werk kunnen doen — géén inline top/left, anders overschrijft die inline top de
            // sheet-CSS (top:auto + bottom:0) en rekt de kalender uit tot onderaan het scherm.
            if (window.innerWidth < 768) {
                panel.style.top = "";
                panel.style.bottom = "";
                panel.style.left = "";
                panel.style.maxHeight = "";
                return;
            }
            var rect = trigger.getBoundingClientRect();
            panel.style.left = Math.max(8, Math.min(rect.left, window.innerWidth - 252 - 8)) + "px";
            // Onderaan te weinig plaats? Dan naar bóven openklappen (zelfde regel als de zoekende
            // selects in gl-v2-<pagina>.js) i.p.v. buiten beeld te vallen.
            var spaceBelow = window.innerHeight - rect.bottom - 14;
            var spaceAbove = rect.top - 14;
            if (spaceBelow < 320 && spaceAbove > spaceBelow) {
                panel.style.top = "";
                panel.style.bottom = (window.innerHeight - rect.top + 6) + "px";
            } else {
                panel.style.bottom = "";
                panel.style.top = (rect.bottom + 6) + "px";
            }
        }

        function pad2(n) { return n < 10 ? "0" + n : "" + n; }
        function daysInMonth(y, m) { return new Date(y, m, 0).getDate(); }

        instances.forEach(function (root) {
            var box = root.querySelector(".gl-v2-field-box");
            var textInput = root.querySelector('[data-role="date-text"]');
            var hiddenInput = root.querySelector('[data-role="date-value"]');
            var panel = root.querySelector('[data-role="panel"]');
            var monthLabel = root.querySelector('[data-role="month-label"]');
            var daysHost = root.querySelector('[data-role="days"]');
            var prevBtn = root.querySelector('[data-role="prev"]');
            var nextBtn = root.querySelector('[data-role="next"]');
            var todayBtn = root.querySelector('[data-role="today"]');
            var clearBtn = root.querySelector('[data-role="clear"]');
            // Foutslot van het veld — hernoemd naar het generieke [data-role="field-error"] (het gedeelde
            // Foutoverzicht, punt 24, hergebruikt datzelfde element; zie GlV2/_DateField.cshtml).
            var errorEl = root.querySelector('[data-role="field-error"]');
            var fieldEl = root.closest(".gl-v2-field");
            if (!box || !textInput || !hiddenInput || !panel || !daysHost) return;
            panel.setAttribute("data-gl-v2-dp-owned", "");

            var today = new Date();
            var selected = parseIso(hiddenInput.value);
            var view = selected ? { y: selected.y, m: selected.m } : { y: today.getFullYear(), m: today.getMonth() + 1 };

            function parseIso(v) {
                var parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v || "");
                return parts ? { y: +parts[1], m: +parts[2], d: +parts[3] } : null;
            }
            function parseDisplay(v) {
                var parts = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec((v || "").trim());
                return parts ? { d: +parts[1], m: +parts[2], y: +parts[3] } : null;
            }
            function isValidMonth(m) { return m >= 1 && m <= 12; }
            function isValidDate(y, m, d) { return isValidMonth(m) && d >= 1 && d <= daysInMonth(y, m); }

            function setError(msg) {
                if (fieldEl) fieldEl.classList.add("is-error");
                if (errorEl) { errorEl.textContent = msg; errorEl.hidden = false; }
            }
            function clearFieldError() {
                if (fieldEl) fieldEl.classList.remove("is-error");
                if (errorEl) { errorEl.textContent = ""; errorEl.hidden = true; }
            }

            function setValue(y, m, d) {
                hiddenInput.value = y + "-" + pad2(m) + "-" + pad2(d);
                textInput.value = pad2(d) + "/" + pad2(m) + "/" + y;
                selected = { y: y, m: m, d: d };
                view = { y: y, m: m };
                clearFieldError();
                hiddenInput.dispatchEvent(new Event("change", { bubbles: true }));
                renderCalendar();
            }
            function clearValue() {
                hiddenInput.value = "";
                textInput.value = "";
                selected = null;
                clearFieldError();
                hiddenInput.dispatchEvent(new Event("change", { bubbles: true }));
                renderCalendar();
            }

            function renderCalendar() {
                if (monthLabel) monthLabel.textContent = monthNames[view.m - 1] + " " + view.y;
                daysHost.innerHTML = "";
                var firstWeekday = (new Date(view.y, view.m - 1, 1).getDay() + 6) % 7;
                var total = daysInMonth(view.y, view.m);
                var prevMonth = view.m === 1 ? 12 : view.m - 1;
                var prevYear = view.m === 1 ? view.y - 1 : view.y;
                var prevTotal = daysInMonth(prevYear, prevMonth);
                var nextMonth = view.m === 12 ? 1 : view.m + 1;
                var nextYear = view.m === 12 ? view.y + 1 : view.y;

                var cells = [];
                for (var i = 0; i < firstWeekday; i++) {
                    cells.push({ y: prevYear, m: prevMonth, d: prevTotal - firstWeekday + 1 + i, outside: true });
                }
                for (var d = 1; d <= total; d++) {
                    cells.push({ y: view.y, m: view.m, d: d, outside: false });
                }
                var trailing = (7 - (cells.length % 7)) % 7;
                for (var t = 1; t <= trailing; t++) {
                    cells.push({ y: nextYear, m: nextMonth, d: t, outside: true });
                }

                cells.forEach(function (cell) {
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "gl-v2-datepicker-day" + (cell.outside ? " is-outside" : "");
                    btn.textContent = cell.d;
                    if (cell.y === today.getFullYear() && cell.m === today.getMonth() + 1 && cell.d === today.getDate()) {
                        btn.classList.add("is-today");
                    }
                    if (selected && cell.y === selected.y && cell.m === selected.m && cell.d === selected.d) {
                        btn.classList.add("is-selected");
                    }
                    btn.addEventListener("click", function () {
                        setValue(cell.y, cell.m, cell.d);
                        closeAll();
                        textInput.focus();
                    });
                    daysHost.appendChild(btn);
                });
            }

            function openPanel() {
                closeAll();
                view = selected ? { y: selected.y, m: selected.m } : { y: today.getFullYear(), m: today.getMonth() + 1 };
                renderCalendar();
                positionPanel(box, panel);
                panel.classList.add("is-open");
            }

            box.addEventListener("click", function () {
                if (!panel.classList.contains("is-open")) openPanel();
            });
            if (prevBtn) prevBtn.addEventListener("click", function () {
                view.m -= 1;
                if (view.m < 1) { view.m = 12; view.y -= 1; }
                renderCalendar();
            });
            if (nextBtn) nextBtn.addEventListener("click", function () {
                view.m += 1;
                if (view.m > 12) { view.m = 1; view.y += 1; }
                renderCalendar();
            });
            if (todayBtn) todayBtn.addEventListener("click", function () {
                setValue(today.getFullYear(), today.getMonth() + 1, today.getDate());
                closeAll();
                textInput.focus();
            });
            if (clearBtn) clearBtn.addEventListener("click", function () {
                clearValue();
                textInput.focus();
            });

            textInput.addEventListener("focus", function () {
                if (!panel.classList.contains("is-open")) openPanel();
            });
            textInput.addEventListener("input", function () {
                var digits = textInput.value.replace(/[^\d]/g, "").slice(0, 8);
                var out = digits;
                if (digits.length > 4) { out = digits.slice(0, 2) + "/" + digits.slice(2, 4) + "/" + digits.slice(4); }
                else if (digits.length > 2) { out = digits.slice(0, 2) + "/" + digits.slice(2); }
                textInput.value = out;
                if (!panel.classList.contains("is-open")) openPanel();
            });
            textInput.addEventListener("blur", function () {
                var text = textInput.value.trim();
                if (!text) { clearValue(); return; }
                var parsed = parseDisplay(text);
                if (!parsed || !isValidDate(parsed.y, parsed.m, parsed.d)) {
                    if (!parsed || !isValidMonth(parsed.m)) {
                        setError("Ongeldige datum — gebruik dd/mm/jjjj.");
                    } else {
                        var mn = monthNames[parsed.m - 1];
                        setError(mn.charAt(0).toUpperCase() + mn.slice(1) + " heeft " + daysInMonth(parsed.y, parsed.m) + " dagen.");
                    }
                    return;
                }
                setValue(parsed.y, parsed.m, parsed.d);
            });
            textInput.addEventListener("keydown", function (e) {
                if (e.key === "Enter") { e.preventDefault(); textInput.blur(); closeAll(); }
                if (e.key === "Escape") { closeAll(); }
            });

            renderCalendar();
        });

        document.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-select[data-gl-v2-datepicker]")) return;
            closeAll();
        });
        window.addEventListener("resize", closeAll);
        window.addEventListener("scroll", closeAll, true);
    }
})();

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

            function sync() {
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

// ── Meldingskaders (design-handoff punt 25, gl-v2-notice*, zie DESIGN.md "Meldingskaders") — de
//    enige gedragslogica die dit component nodig heeft: een sluitknop verbergt haar eigen kaart.
//    25d "fout en waarschuwing blijven staan tot het opgelost is; info en succes mogen sluitbaar
//    zijn" is een MARKUP-regel (een pagina voegt een sluitknop enkel toe op is-info/is-success),
//    geen JS-regel — dit bestand dwingt dat dus niet af, het maakt een aanwezige knop enkel
//    functioneel. Gedelegeerd op document (shell-breed, ook voor een later dynamisch toegevoegde
//    kaart) i.p.v. één listener per knop. ───────────────────────────────────────────────────────
(function () {
    "use strict";
    document.addEventListener("click", function (e) {
        var closeBtn = e.target.closest(".gl-v2-notice-close");
        if (!closeBtn) return;
        var notice = closeBtn.closest(".gl-v2-notice");
        if (notice) notice.remove();
    });
})();

// ── Mobiele zoekervaring (iPhone/Safari + Chrome iOS, ook Android) — één centrale module voor élk
//    zoek-/typeahead-veld in de gl-v2-layout, geen per-pagina-kopie. Zie DESIGN.md "Mobiele
//    zoekervaring" voor de lijst met velden, de redenen en wat enkel op een echt toestel te testen valt.
//
//    Wat het doet, enkel op touch/mobiel (MQ hieronder; desktop blijft exact zoals het was):
//    B  decoreert elk zoekveld met type=search, inputmode/enterkeyhint=search en alle auto-correctie
//       uit (ook velden die pas later door pagina-JS worden aangemaakt, bv. het zoekveld in het
//       postcode-paneel — MutationObserver);
//    C  bij focus: body.is-searching ("zoekmodus": CSS verbergt de topbar en de vaste actiebalk/
//       snelactiebalk), het veld wordt bovenaan vastgezet en in beeld gescrold (na 300 ms, als het
//       toetsenbord open is);
//    D  de lijst met resultaten blijft zichtbaar BOVEN het toetsenbord: maat uit window.visualViewport
//       (resize + scroll), fallback 50dvh;
//    E  een "Annuleer"-knop (>= 44x44 px) naast het veld die leegmaakt, blur't en de modus verlaat;
//       Enter/"Zoek" op het toetsenbord blur't (toetsenbord dicht) maar laat de resultaten staan.
//    Bestaande zoeklogica/debounce/API-calls blijven onaangeroerd: dit luistert enkel naar
//    focus/blur/keydown en dispatcht bij Annuleer één gewone "input"-event op het veld.
//
//    Soorten veld (bepaalt hoe de resultaten "boven het toetsenbord" komen):
//      page    zoekbalk op een lijstpagina (resultaten = de pagina zelf): vastgezet bovenaan, een
//              spacer houdt de plek vrij (position:sticky werkt hier niet: .gl-v2-body heeft
//              overflow:hidden en de zoekbalk-kaart heeft een te kleine containing block);
//      panel   postcode-/eenhedenkiezer: het veld zit IN het bottom-sheet-paneel, dat achter het
//              toetsenbord zou vallen -> paneel wordt aan de bovenkant van het zichtbare gebied gezet;
//      trigger meervoudige kiezer: het filterveld zit in de trigger, het paneel (opties) eronder ->
//              paneel verankerd onder de trigger, hoogte tot de bovenkant van het toetsenbord;
//      modal   zoekmodal (volledig scherm op gsm): enkel de hoogte van de resultatenlijst.
//    Een veld binnen een al vaste laag (mobiel hoofdmenu) krijgt enkel de attributen.
(function () {
    "use strict";

    var MQ = "((pointer: coarse) and (max-width: 1023.98px)), (max-width: 767.98px)";
    var mql = window.matchMedia ? window.matchMedia(MQ) : null;
    function isMobile() { return !!(mql && mql.matches); }

    var SEARCH_SELECTOR = [
        ".gl-v2-toolbar-search input",
        ".gl-v2-modal-search-input",
        ".gl-v2-select-search-field input",
        '.gl-v2-select-panel-search input[data-role="input"]',
        ".gl-v2-select-trigger-multi-input",
        ".gl-v2-combo-input",
        "#gl-v2-pd-units-filter", "#gl-v2-co2-search", "#gl-v2-dd-search", "#gl-v2-du-search", "#gl-v2-dp-search",
        "#gl-v2-mobile-search", ".js-gl-v2-pm-search", "#gl-punt-search", "#gl-pin-search",
        "input[data-gl-v2-search]"
    ].join(",");

    var root = document.documentElement;
    var active = null;

    function setAttr(el, name, value) { if (el.getAttribute(name) !== value) el.setAttribute(name, value); }

    // ── B: attributen ──────────────────────────────────────────────────────────────────────────
    function decorate(input) {
        if (!input || input.nodeType !== 1 || input.tagName !== "INPUT" || input.getAttribute("data-gl-v2-search-ready")) return;
        input.setAttribute("data-gl-v2-search-ready", "1");
        if (input.type === "text") input.type = "search";
        setAttr(input, "inputmode", "search");
        setAttr(input, "enterkeyhint", "search");
        setAttr(input, "autocomplete", "off");
        setAttr(input, "autocorrect", "off");
        setAttr(input, "autocapitalize", "off");
        setAttr(input, "spellcheck", "false");
    }
    function decorateTree(node) {
        if (node.matches && node.matches(SEARCH_SELECTOR)) decorate(node);
        if (node.querySelectorAll) Array.prototype.forEach.call(node.querySelectorAll(SEARCH_SELECTOR), decorate);
    }

    var pending = [];
    var scheduled = false;
    function flushPending() {
        scheduled = false;
        var nodes = pending;
        pending = [];
        if (isMobile()) nodes.forEach(decorateTree);
    }
    if (window.MutationObserver) {
        new MutationObserver(function (mutations) {
            mutations.forEach(function (m) {
                Array.prototype.forEach.call(m.addedNodes, function (n) { if (n.nodeType === 1) pending.push(n); });
            });
            if (pending.length && !scheduled) { scheduled = true; window.requestAnimationFrame(flushPending); }
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
    if (isMobile()) decorateTree(document);
    if (mql && mql.addEventListener) {
        mql.addEventListener("change", function () {
            if (isMobile()) decorateTree(document); else if (active) exit();
        });
    }

    // ── Zichtbaar gebied (visualViewport: het deel van het scherm dat het toetsenbord vrij laat) ──
    function metrics() {
        var vv = window.visualViewport;
        if (vv) return { top: vv.offsetTop, height: vv.height, bottom: vv.offsetTop + vv.height, ok: true };
        return { top: 0, height: window.innerHeight * 0.5, bottom: window.innerHeight * 0.5, ok: false }; // fallback 50dvh
    }

    function insideFixed(el) {
        for (var n = el.parentElement; n && n !== document.body; n = n.parentElement) {
            if (window.getComputedStyle(n).position === "fixed") return true;
        }
        return false;
    }

    // ── E: annuleerknop ────────────────────────────────────────────────────────────────────────
    function ensureCancel(input) {
        var box = input.closest(".gl-v2-field-box, .gl-v2-select-search-field, .gl-v2-select-trigger-multi, .gl-v2-combo-field");
        if (!box) return null;
        var btn = box.querySelector(".gl-v2-search-cancel");
        if (!btn) {
            btn = document.createElement("button");
            btn.type = "button";
            btn.className = "gl-v2-search-cancel";
            btn.textContent = "Annuleer";
            btn.setAttribute("aria-label", "Zoeken annuleren");
            // Focus bij het aantikken niet verliezen (anders blur't het veld vóór de klik aankomt).
            btn.addEventListener("pointerdown", function (e) { e.preventDefault(); });
            btn.addEventListener("mousedown", function (e) { e.preventDefault(); });
            btn.addEventListener("click", function (e) { e.preventDefault(); e.stopPropagation(); cancel(); });
            box.appendChild(btn);
        }
        btn.hidden = false;
        return btn;
    }

    function cancel() {
        if (!active) return;
        var a = active;
        var needsBackdrop = a.kind === "panel" || a.kind === "trigger";
        a.input.value = "";
        a.input.dispatchEvent(new Event("input", { bubbles: true }));
        a.input.blur();
        exit();
        // Bij een paneel/keuzelijst betekent "annuleer" ook: het paneel sluiten (hun eigen backdrop-klik).
        if (needsBackdrop) {
            var backdrop = document.querySelector(".gl-v2-select-backdrop.is-open");
            if (backdrop) backdrop.click();
        }
    }

    // ── C/D: modus aan/uit ─────────────────────────────────────────────────────────────────────
    function enter(input) {
        if (!isMobile()) return;
        if (active && active.input === input) { window.clearTimeout(active.hideTimer); return; }
        if (active) exit();

        var panelEl = input.closest(".gl-v2-select-panel");
        var kind;
        if (insideFixed(input) && !panelEl && !input.closest(".modal")) kind = "static";
        else if (panelEl) kind = "panel";
        else if (input.closest(".modal")) kind = "modal";
        else if (input.closest(".gl-v2-select-trigger-multi, .gl-v2-combo-field")) kind = "trigger";
        else kind = "page";

        active = { input: input, kind: kind, startWidth: window.innerWidth, hideTimer: 0 };
        if (kind === "static") return;

        document.body.classList.add("is-searching");
        if (kind === "panel") {
            active.panel = panelEl;
            panelEl.classList.add("is-keyboard-pinned");
            // Sluit het paneel zich (backdrop/keuze), dan hoort de vastzetting mee te verdwijnen.
            if (window.MutationObserver) {
                active.observer = new MutationObserver(function () { if (!panelEl.classList.contains("is-open")) exit(); });
                active.observer.observe(panelEl, { attributes: true, attributeFilter: ["class"] });
            }
        } else if (kind === "trigger") {
            var sel = input.closest(".gl-v2-select, .gl-v2-combo");
            active.panel = sel && sel.querySelector(".gl-v2-select-panel, .gl-v2-combo-panel");
            active.anchor = input.closest(".gl-v2-select-trigger, .gl-v2-combo-field") || input;
            if (active.panel) active.panel.classList.add("is-keyboard-anchored");
        } else if (kind === "page") {
            var host = input.closest(".gl-v2-toolbar-card") || input.closest(".gl-v2-field") || input.parentElement;
            var rect = host.getBoundingClientRect();
            var cs = window.getComputedStyle(host);
            var spacer = document.createElement("div");
            spacer.className = "gl-v2-search-spacer";
            spacer.style.height = rect.height + "px";
            spacer.style.marginTop = cs.marginTop;
            spacer.style.marginBottom = cs.marginBottom;
            host.parentNode.insertBefore(spacer, host);
            host.classList.add("gl-v2-search-pinned");
            active.host = host;
            active.spacer = spacer;
        }
        if (kind !== "modal") active.cancelBtn = ensureCancel(input);

        var vv = window.visualViewport;
        active.onViewport = function () { update(); };
        if (vv) { vv.addEventListener("resize", active.onViewport); vv.addEventListener("scroll", active.onViewport); }
        window.addEventListener("resize", active.onViewport);
        update();

        // Na ~300 ms (toetsenbord is dan open en de viewport gestabiliseerd) het veld bovenaan in beeld.
        window.setTimeout(function () {
            if (!active || active.input !== input) return;
            var target = active.spacer || active.anchor;
            if (target && target.scrollIntoView) target.scrollIntoView({ block: "start", behavior: "smooth" });
            update();
        }, 300);
    }

    function update() {
        if (!active || active.kind === "static") return;
        var m = metrics();
        root.style.setProperty("--gl-v2-vv-top", m.top + "px");
        root.style.setProperty("--gl-v2-vv-h", m.ok ? m.height + "px" : "50dvh");
        if (active.kind === "trigger" && active.panel) {
            var r = active.anchor.getBoundingClientRect();
            active.panel.style.setProperty("--gl-v2-panel-top", Math.max(0, r.bottom + 4) + "px");
            active.panel.style.setProperty("--gl-v2-panel-max", m.ok ? Math.max(120, m.bottom - r.bottom - 12) + "px" : "50dvh");
        }
        // Resultatenlijsten die niet zelf een paneel zijn (zoekmodal, opt-in [data-gl-v2-search-results]).
        var lists = document.querySelectorAll(active.kind === "modal"
            ? ".modal.show .gl-v2-modal-search-results, .modal.show [data-gl-v2-search-results]"
            : "[data-gl-v2-search-results]");
        Array.prototype.forEach.call(lists, function (el) {
            var top = el.getBoundingClientRect().top;
            el.style.maxHeight = m.ok ? Math.max(120, m.bottom - top - 12) + "px" : "50dvh";
        });
    }

    function exit() {
        if (!active) return;
        var a = active;
        active = null;
        window.clearTimeout(a.hideTimer);
        if (a.observer) a.observer.disconnect();
        var vv = window.visualViewport;
        if (a.onViewport) {
            if (vv) { vv.removeEventListener("resize", a.onViewport); vv.removeEventListener("scroll", a.onViewport); }
            window.removeEventListener("resize", a.onViewport);
        }
        if (a.kind === "static") return;
        document.body.classList.remove("is-searching");
        if (a.panel) {
            a.panel.classList.remove("is-keyboard-pinned", "is-keyboard-anchored");
            a.panel.style.removeProperty("--gl-v2-panel-top");
            a.panel.style.removeProperty("--gl-v2-panel-max");
        }
        if (a.host) a.host.classList.remove("gl-v2-search-pinned");
        if (a.spacer && a.spacer.parentNode) a.spacer.parentNode.removeChild(a.spacer);
        if (a.cancelBtn) a.cancelBtn.hidden = true;
        root.style.removeProperty("--gl-v2-vv-top");
        root.style.removeProperty("--gl-v2-vv-h");
        Array.prototype.forEach.call(document.querySelectorAll("[data-gl-v2-search-results], .gl-v2-modal-search-results"), function (el) {
            el.style.maxHeight = "";
        });
    }

    document.addEventListener("focusin", function (e) {
        var t = e.target;
        if (!t || t.tagName !== "INPUT" || !t.matches(SEARCH_SELECTOR)) return;
        if (!isMobile()) return;
        decorate(t);
        enter(t);
    });
    // Kleine vertraging bij blur: een tik op een resultaat blur't het veld eerst — de klik moet nog
    // aankomen vóór de lijst/het paneel verdwijnt.
    document.addEventListener("focusout", function (e) {
        if (!active || e.target !== active.input) return;
        var a = active;
        window.clearTimeout(a.hideTimer);
        a.hideTimer = window.setTimeout(function () {
            if (active === a && document.activeElement !== a.input) exit();
        }, 250);
    });
    // E: Enter/"Zoek" sluit het toetsenbord maar laat de resultaten staan. Bubbling + defaultPrevented-
    // check: een eigen Enter-afhandeling van het veld (bv. het gemarkeerde resultaat kiezen) gaat voor.
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" || !active || e.target !== active.input || e.defaultPrevented) return;
        e.preventDefault();
        active.input.blur();
    });
    // Een resize die enkel de HOOGTE wijzigt terwijl het toetsenbord open is (Android-Chrome met
    // interactive-widget=resizes-content krimpt de layout-viewport) is géén reden voor de paginascripts
    // om hun panelen te sluiten (hun window-resize-luisteraars doen dat) — in de capture-fase,
    // vóór die luisteraars (deze module laadt vóór alle paginascripts), tegengehouden.
    window.addEventListener("resize", function (e) {
        if (active && active.kind !== "static" && window.innerWidth === active.startWidth) e.stopImmediatePropagation();
    }, true);

    // Open-klik-vangnet (gevonden tijdens het testen van deze module, en de échte oorzaak van "op gsm
    // gaat het veld gemeente/postcode niet open"): een zoekende keuzelijst opent bij FOCUS (mousedown),
    // en toont op <768px meteen een schermvullende backdrop. De daaropvolgende mouseup landt dan op
    // die backdrop i.p.v. op de trigger — mousedown- en mouseup-doel verschillen, dus gaat de "click"
    // naar hun gemeenschappelijke voorouder (<body>), en het "klik erbuiten sluit"-document-luisteraar
    // van elke pagina (5 kopieën) sluit het paneel dat net opende. Hier één keer, centraal, in de
    // capture-fase (vóór die luisteraars): een click zonder doel binnen de keuzelijst, binnen 450 ms
    // na een focus BINNEN een keuzelijst, is geen "klik erbuiten" maar de staart van diezelfde tik.
    var lastSelectFocusAt = 0;
    document.addEventListener("focusin", function (e) {
        if (e.target && e.target.closest && e.target.closest(".gl-v2-select")) lastSelectFocusAt = Date.now();
    }, true);
    document.addEventListener("click", function (e) {
        if (!isMobile() || Date.now() - lastSelectFocusAt > 450) return;
        if (e.target && e.target.closest && e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        e.stopImmediatePropagation();
    }, true);

    window.GlV2MobileSearch = {
        isMobile: isMobile,
        isActive: function () { return !!active; },
        decorate: decorate,
        fit: update
    };
})();
