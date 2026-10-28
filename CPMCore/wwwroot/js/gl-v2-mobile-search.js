// gl-v2 — mobiele zoekervaring (GlV2MobileSearch). Uit gl-v2-shell.js gelicht (26/10/2026, consolidatie stap 11): zelfstandige IIFE, geladen direct na gl-v2-shell.js.

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
            var spacer = document.createElement("div");
            spacer.className = "gl-v2-search-spacer";
            // De vastgezette balk valt uit de flow en de ruimte die ze innam verdwijnt volledig (geen lege plek meer midden in de pagina); de spacer
            // is enkel nog het scrolldoel en houdt via scroll-margin-top precies de hoogte van de vastgezette balk vrij, zodat de resultaten er direct onder starten.
            spacer.style.height = "0";
            host.parentNode.insertBefore(spacer, host);
            host.classList.add("gl-v2-search-pinned");
            var pinnedH = host.getBoundingClientRect().height;
            spacer.style.scrollMarginTop = pinnedH + "px";
            // Ruimte voor de vastgezette balk bovenaan de pagina (zie mobile-search.css, --gl-v2-pinned-h): anders komt de bovenkant van de pagina achter de balk te staan.
            root.style.setProperty("--gl-v2-pinned-h", pinnedH + "px");
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
        root.style.removeProperty("--gl-v2-pinned-h");
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
