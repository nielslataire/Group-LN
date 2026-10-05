// gl-v2 — Keuzelijst met zoekveld en knop (design-handoff punt 34). Eén implementatie voor het hele project:
// elke `[data-gl-v2-combo]` wordt automatisch bedraad (bij het laden én voor later toegevoegde rijen).
// Markup en CSS: Views/Shared/Partials/_GlV2Combo.cshtml, wwwroot/css/gl-v2/combo.css; contract in DESIGN.md
// ("Keuzelijst met zoekveld en knop — punt 34").
//
//   data-lookup-url      POST {term, countryId} → [{id, text, extra|sub}]   (server-zoekopdracht, 250 ms debounce)
//   data-options         JSON [{id, text, sub}]                              (statische lijst, filtert lokaal)
//   data-min-chars       minimum tekens voor een server-zoekopdracht (standaard 2; statisch: 0)
//   data-country-source  CSS-selector van een landveld (of `[data-gl-v2-address-block]` + `[data-role=country-select]`)
//   data-allow-new       toont "Nieuw" + de "… aanmaken"-rij; klik → event `gl-v2:combo-new` {term}
//   data-new-entity      woord in "… aanmaken als nieuw <bedrijf>"
//   data-avatar          "initials" → initialen-vlakje per rij
//   data-list-label      kop boven een statische lijst zonder zoekterm ("ALLE BEDRIJVEN")
//   data-empty-value     waarde van de verborgen id als er niets gekozen is ("" of "0")
//
// Events (alle bubbelen): `change` + `input` op de verborgen id-input, `gl-v2:combo-select` {id,text,item},
// `gl-v2:postal-selected` (zelfde detail — compatibiliteit met de postcode-/gemeentepagina's), `gl-v2:combo-new`.
// API: window.GlV2Combo = { init(scope), wire(root), setOptions(root, items), setValue(root, id, text, sub), setDisabled(root, bool) }.
(function () {
    "use strict";

    var backdrop = null;
    var openCombo = null;

    function qs(root, sel) { return root.querySelector(sel); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
    function initials(text) {
        return String(text || "").replace(/[^A-Za-z ]/g, "").split(" ").filter(Boolean).slice(0, 2).map(function (w) { return w[0]; }).join("").toUpperCase() || "·";
    }
    function highlight(text, term) {
        var t = String(text || "");
        if (!term) return esc(t);
        var i = t.toLowerCase().indexOf(term);
        if (i < 0) return esc(t);
        return esc(t.slice(0, i)) + "<b>" + esc(t.slice(i, i + term.length)) + "</b>" + esc(t.slice(i + term.length));
    }

    function getBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement("div");
        backdrop.className = "gl-v2-select-backdrop";
        document.body.appendChild(backdrop);
        // Op gsm (en voor de Annuleer-knop van GlV2MobileSearch) sluit een klik op de backdrop de lijst.
        backdrop.addEventListener("click", function () { if (openCombo) openCombo.close(true); });
        return backdrop;
    }

    function wire(root) {
        if (!root || root.hasAttribute("data-gl-v2-wired")) return;
        var hidden = qs(root, '[data-role="id-hidden"]');
        var textHidden = qs(root, '[data-role="text-hidden"]');
        var input = qs(root, ".gl-v2-combo-input");
        var field = qs(root, ".gl-v2-combo-field");
        var lead = qs(root, ".gl-v2-combo-lead");
        var clearBtn = qs(root, '[data-role="clear"]');
        var spinner = qs(root, '[data-role="spinner"]');
        var panel = qs(root, '[data-role="panel"]');
        var newBtn = qs(root, '[data-role="new"]');
        if (!hidden || !input || !field || !panel) return;
        root.setAttribute("data-gl-v2-wired", "1");

        var url = root.getAttribute("data-lookup-url");
        var allowNew = root.hasAttribute("data-allow-new");
        var newEntity = root.getAttribute("data-new-entity") || "item";
        var avatar = root.getAttribute("data-avatar") === "initials";
        var listLabel = root.getAttribute("data-list-label") || "";
        var emptyValue = root.hasAttribute("data-empty-value") ? root.getAttribute("data-empty-value") : "";
        var leadIcon = root.getAttribute("data-icon") || "ph-magnifying-glass";
        var options = null;
        try { options = root.getAttribute("data-options") ? JSON.parse(root.getAttribute("data-options")) : null; } catch (e) { options = null; }
        var minChars = parseInt(root.getAttribute("data-min-chars") || (options ? "0" : "2"), 10);

        var selected = { id: "", text: input.value || "", sub: "" };
        var typing = false, term = "", items = [], hi = 0, debounce = 0, seq = 0, explicitHi = false, pointerDown = false;
        var helpEl = root.parentElement ? root.parentElement.querySelector('[data-role="combo-help"]') : null;
        var helpDefault = helpEl ? helpEl.textContent : "";

        function hasValue() { return hidden.value !== "" && hidden.value !== emptyValue; }
        function isLocked() { return root.classList.contains("is-readonly") || root.classList.contains("is-disabled"); }
        function setLead(searching) {
            if (!lead) return;
            lead.className = "ph " + (searching ? "ph-magnifying-glass" : leadIcon) + " gl-v2-combo-lead";
        }
        function refreshClear() { if (clearBtn) clearBtn.hidden = isLocked() || !(typing ? input.value : (hasValue() && input.value)); }
        function setHelp(text, isDefault) {
            if (!helpEl) return;
            helpEl.textContent = text || helpDefault;
            helpEl.hidden = !(text || helpDefault);
        }
        function countryId() {
            var src = root.getAttribute("data-country-source");
            var el = src ? document.querySelector(src) : null;
            if (!el) {
                var block = root.closest("[data-gl-v2-address-block]");
                el = block && block.querySelector('[data-role="country-select"]');
            }
            return el ? el.value : "";
        }

        // ── paneel ─────────────────────────────────────────────────────────────────────────────────
        function position() {
            if (window.innerWidth < 768) { panel.style.top = panel.style.left = panel.style.width = panel.style.maxHeight = ""; return; }   // gsm: CSS + GlV2MobileSearch bepalen de plek
            var r = field.getBoundingClientRect();
            var full = root.getBoundingClientRect();
            var width = Math.max(full.width, 240);
            panel.style.width = width + "px";
            panel.style.left = Math.max(8, Math.min(full.left, window.innerWidth - width - 8)) + "px";
            panel.style.top = (r.bottom + 4) + "px";
            panel.style.maxHeight = Math.max(160, Math.min(340, window.innerHeight - r.bottom - 16)) + "px";
        }
        function open() {
            if (isLocked()) return;
            if (openCombo && openCombo !== api) openCombo.close(false);
            openCombo = api;
            root.classList.add("is-open");
            panel.classList.add("is-open");
            input.setAttribute("aria-expanded", "true");
            if (window.innerWidth < 768) getBackdrop().classList.add("is-open");
            position();
        }
        function close(restore) {
            if (openCombo === api) openCombo = null;
            root.classList.remove("is-open");
            panel.classList.remove("is-open", "is-keyboard-anchored");
            input.setAttribute("aria-expanded", "false");
            if (backdrop) backdrop.classList.remove("is-open");
            window.clearTimeout(debounce);
            if (spinner) spinner.hidden = true;
            if (restore) { typing = false; term = ""; input.value = selected.text; setLead(false); refreshClear(); }
        }

        function render() {
            var html = "";
            var total = items.length;
            if (!typing && !term) {
                if (options && listLabel) html += '<div class="gl-v2-combo-head"><span>' + esc(listLabel) + " · " + options.length + '</span><span class="gl-v2-combo-head-keys">↑↓ kiezen · Enter</span></div>';
            } else if (term) {
                html += '<div class="gl-v2-combo-head"><span>' + total + ' gevonden</span><span class="gl-v2-combo-head-keys">↑↓ kiezen · Enter</span></div>';
            }
            if (!total && term && (url ? term.length >= minChars : true)) {
                html += '<div class="gl-v2-combo-empty">Niets gevonden voor “' + esc(term) + '”.</div>';
            } else if (url && !options && term.length < minChars && !total) {
                html += '<div class="gl-v2-combo-hint">' + (term ? "Typ nog minstens " + (minChars - term.length) + " teken(s) …" : "Typ om te zoeken …") + "</div>";
            }
            items.forEach(function (it, k) {
                var isSel = hasValue() && String(it.id) === String(hidden.value);
                html += '<button type="button" tabindex="-1" role="option" class="gl-v2-combo-option' + (k === hi ? " is-active" : "") + (isSel ? " is-selected" : "") + '" data-index="' + k + '">' +
                    (avatar ? '<span class="gl-v2-combo-avatar">' + esc(initials(it.text)) + "</span>" : "") +
                    '<span class="gl-v2-combo-text"><span class="gl-v2-combo-name">' + highlight(it.text, term) + "</span>" +
                    (it.sub ? '<span class="gl-v2-combo-sub">' + esc(it.sub) + "</span>" : "") + "</span>" +
                    (isSel ? '<i class="ph ph-check gl-v2-combo-check" aria-hidden="true"></i>' : "") + "</button>";
            });
            if (allowNew && term) {
                html += '<div class="gl-v2-combo-sep"></div><button type="button" tabindex="-1" class="gl-v2-combo-option gl-v2-combo-create' + (hi >= total ? " is-active" : "") + '" data-role="create">' +
                    '<span class="gl-v2-combo-avatar"><i class="ph ph-plus" aria-hidden="true"></i></span><span class="gl-v2-combo-name">“' + esc(term) + "” aanmaken als nieuw " + esc(newEntity) + "</span></button>";
            }
            panel.innerHTML = html;
            var active = panel.querySelector(".is-active");
            if (active && active.scrollIntoView) active.scrollIntoView({ block: "nearest" });
        }

        function filterStatic() {
            var t = term;
            items = (options || []).filter(function (o) { return !t || (o.text + " " + (o.sub || "")).toLowerCase().indexOf(t) !== -1; });
        }

        function search() {
            window.clearTimeout(debounce);
            if (options) { filterStatic(); render(); return; }
            if (!url || term.length < minChars) { items = []; if (spinner) spinner.hidden = true; render(); return; }
            if (spinner) spinner.hidden = false;
            var mySeq = ++seq;
            debounce = window.setTimeout(function () {
                var body = new URLSearchParams();
                body.set("term", term);
                body.set("countryId", countryId());
                body.set("activeOnly", "true");
                fetch(url, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: body.toString(), credentials: "same-origin" })
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .catch(function () { return []; })
                    .then(function (data) {
                        if (mySeq !== seq) return;
                        items = (data || []).map(function (d) { return { id: d.id, text: d.text, sub: d.sub || d.extra || "" }; });
                        if (spinner) spinner.hidden = true;
                        hi = 0; explicitHi = false;
                        render();
                    });
            }, 250);
        }

        // ── keuze ──────────────────────────────────────────────────────────────────────────────────
        function fire(detail) {
            hidden.dispatchEvent(new Event("input", { bubbles: true }));
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
            root.dispatchEvent(new CustomEvent("gl-v2:combo-select", { bubbles: true, detail: detail }));
            root.dispatchEvent(new CustomEvent("gl-v2:postal-selected", { bubbles: true, detail: detail }));
        }
        function apply(id, text, sub, silent) {
            selected = { id: id, text: text || "", sub: sub || "" };
            hidden.value = id === "" || id == null ? emptyValue : String(id);
            if (textHidden) textHidden.value = text || "";
            input.value = text || "";
            typing = false; term = "";
            root.classList.remove("is-unknown");
            setLead(false); refreshClear();
            setHelp(sub || "");
            var field_ = root.closest(".gl-v2-field");
            if (field_ && hasValue()) field_.classList.remove("is-error");
            if (!silent) fire({ id: hasValue() ? hidden.value : "", text: text || "", item: { id: id, text: text, sub: sub } });
        }
        function choose(it) {
            apply(it.id, it.text, it.sub || "");
            close(false);
        }
        function createNew() {
            var t = term;
            close(true);
            root.dispatchEvent(new CustomEvent("gl-v2:combo-new", { bubbles: true, detail: { term: t } }));
        }

        // ── invoer ─────────────────────────────────────────────────────────────────────────────────
        function onType() {
            typing = true;
            term = input.value.trim().toLowerCase();
            hi = 0; explicitHi = false;
            setLead(true); refreshClear();
            if (hasValue()) { hidden.value = emptyValue; if (textHidden) textHidden.value = ""; selected = { id: "", text: "", sub: "" }; setHelp(""); }
            root.classList.remove("is-unknown");
            open();
            search();
        }
        input.addEventListener("input", onType);
        input.addEventListener("focus", function () {
            if (isLocked()) return;
            if (!typing) { term = ""; if (options) { items = options.slice(); } else { items = []; } hi = Math.max(0, items.findIndex(function (it) { return hasValue() && String(it.id) === String(hidden.value); })); render(); }
            open();
            if (hasValue() && input.select) window.setTimeout(function () { try { input.select(); } catch (e) { } }, 0);
        });
        input.addEventListener("click", function () {
            // Het veld heeft al focus maar de lijst is dicht (na een keuze/Esc): een klik opent hem opnieuw.
            if (!isLocked() && !panel.classList.contains("is-open")) { if (!typing) { items = options ? options.slice() : []; } open(); render(); }
        });
        input.addEventListener("blur", function () {
            // Een klik in het paneel mag het veld niet eerst sluiten: pointerdown houdt de lijst vast.
            if (pointerDown) return;
            var unknown = typing && input.value.trim() !== "" && !hasValue();
            close(!unknown);
            if (unknown) { root.classList.add("is-unknown"); setHelp(newBtn ? "Niet gevonden — kies uit de lijst of klik Nieuw" : "Niet gevonden — kies uit de lijst"); }
        });
        field.addEventListener("mousedown", function (e) { if (e.target !== input && !e.target.closest("button")) { e.preventDefault(); input.focus(); } });
        panel.addEventListener("pointerdown", function () { pointerDown = true; window.setTimeout(function () { pointerDown = false; }, 400); });
        panel.addEventListener("mousedown", function (e) { e.preventDefault(); });
        panel.addEventListener("click", function (e) {
            var opt = e.target.closest(".gl-v2-combo-option");
            if (!opt) return;
            pointerDown = false;
            if (opt.getAttribute("data-role") === "create") { createNew(); return; }
            var it = items[parseInt(opt.getAttribute("data-index"), 10)];
            if (it) choose(it);   // het veld houdt de focus (mousedown in het paneel is onderdrukt): Tab loopt gewoon verder
        });
        panel.addEventListener("mousemove", function (e) {
            var opt = e.target.closest(".gl-v2-combo-option");
            if (!opt) return;
            var idx = opt.getAttribute("data-role") === "create" ? items.length : parseInt(opt.getAttribute("data-index"), 10);
            if (idx !== hi) { hi = idx; explicitHi = true; panel.querySelectorAll(".gl-v2-combo-option").forEach(function (o) { o.classList.toggle("is-active", o === opt); }); }
        });
        input.addEventListener("keydown", function (e) {
            if (isLocked()) return;
            var max = items.length + (allowNew && term ? 1 : 0) - 1;
            if (e.key === "ArrowDown") { e.preventDefault(); open(); hi = Math.min(max, hi + 1); explicitHi = true; render(); }
            else if (e.key === "ArrowUp") { e.preventDefault(); hi = Math.max(0, hi - 1); explicitHi = true; render(); }
            else if (e.key === "Enter") {
                if (!panel.classList.contains("is-open")) return;
                e.preventDefault();   // Enter in een keuzelijst dient het formulier nooit in
                if (hi < items.length && items[hi]) choose(items[hi]);
                else if (allowNew && term) createNew();
            } else if (e.key === "Escape") { if (panel.classList.contains("is-open")) { e.preventDefault(); e.stopPropagation(); close(true); } }
            else if (e.key === "Tab") {
                // Tab kiest het uitdrukkelijk gekozen (of het enige) resultaat en gaat gewoon door naar het volgende veld.
                var pick = panel.classList.contains("is-open") && typing && items.length ? ((explicitHi && items[hi]) || (items.length === 1 ? items[0] : null)) : null;
                if (pick) choose(pick); else close(!(typing && input.value.trim() !== "" && !hasValue()));
            }
        });
        if (clearBtn) {
            clearBtn.addEventListener("mousedown", function (e) { e.preventDefault(); });
            clearBtn.addEventListener("click", function () {
                var had = hasValue();
                apply("", "", "", true);
                typing = true; term = "";
                input.value = "";
                if (had) fire({ id: "", text: "", item: null });
                if (options) items = options.slice(); else items = [];
                hi = 0; setLead(false); refreshClear(); input.focus(); open(); render();
            });
        }
        if (newBtn) {
            newBtn.addEventListener("mousedown", function (e) { e.preventDefault(); });
            newBtn.addEventListener("click", function () { var t = typing ? input.value.trim() : ""; root.dispatchEvent(new CustomEvent("gl-v2:combo-new", { bubbles: true, detail: { term: t } })); });
        }
        window.addEventListener("resize", function () { if (panel.classList.contains("is-open")) position(); });
        window.addEventListener("scroll", function (e) {
            if (!panel.classList.contains("is-open")) return;
            if (e.target && e.target.nodeType === 1 && panel.contains(e.target)) return;
            if (window.innerWidth < 768) return;   // gsm: het paneel hangt vast onder het veld
            position();
        }, true);

        var api = {
            root: root,
            close: close,
            setOptions: function (list) { options = list || []; if (!url) minChars = 0; if (panel.classList.contains("is-open")) { filterStatic(); render(); } },
            setValue: function (id, text, sub) { apply(id, text, sub, true); },
            setDisabled: function (flag) { root.classList.toggle("is-disabled", !!flag); input.disabled = !!flag; refreshClear(); }
        };
        root._glV2Combo = api;

        // beginstaat
        if (hasValue()) { selected = { id: hidden.value, text: input.value, sub: helpEl && helpEl.textContent !== helpDefault ? helpEl.textContent : "" }; }
        setLead(false);
        refreshClear();
        if (root.classList.contains("is-disabled") || root.hasAttribute("data-disabled")) { root.classList.add("is-disabled"); input.disabled = true; }
        if (root.classList.contains("is-readonly")) input.readOnly = true;
    }

    function init(scope) {
        (scope || document).querySelectorAll("[data-gl-v2-combo]").forEach(wire);
    }

    document.addEventListener("click", function (e) {
        if (openCombo && !e.target.closest(".gl-v2-combo") && !e.target.closest(".gl-v2-combo-panel")) openCombo.close(true);
    });

    window.GlV2Combo = {
        init: init,
        wire: wire,
        setOptions: function (root, items) { if (root && root._glV2Combo) root._glV2Combo.setOptions(items); else if (root) root.setAttribute("data-options", JSON.stringify(items || [])); },
        setValue: function (root, id, text, sub) { if (root && root._glV2Combo) root._glV2Combo.setValue(id, text, sub); },
        setDisabled: function (root, flag) { if (root && root._glV2Combo) root._glV2Combo.setDisabled(flag); }
    };

    function boot() {
        init(document);
        if (window.MutationObserver) {
            new MutationObserver(function (mutations) {
                mutations.forEach(function (m) {
                    Array.prototype.forEach.call(m.addedNodes, function (n) {
                        if (n.nodeType !== 1) return;
                        if (n.matches && n.matches("[data-gl-v2-combo]")) wire(n);
                        if (n.querySelectorAll) init(n);
                    });
                });
            }).observe(document.documentElement, { childList: true, subtree: true });
        }
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot); else boot();
})();
