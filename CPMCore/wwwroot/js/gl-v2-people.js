// gl-v2 — kiezer voor meerdere personen (Views/Shared/GlV2/_PersonPicker.cshtml, wwwroot/css/gl-v2/pickers.css). Elke `[data-gl-v2-people]` wordt vanzelf bedraad.
// Gekozen mensen staan als chips met initialen in het invoervak. Het paneel toont wat de server teruggeeft (met kopjes); een volledig getypt e-mailadres kan altijd toegevoegd worden.
//
//   data-lookup-url       POST { term, context } → [{ id (= e-mailadres), text, sub, group }]  (250 ms debounce; ook zonder zoekterm, zodat de collega's meteen zichtbaar zijn)
//   data-context-source   CSS-selector van een veld waarvan de waarde als `context` meegaat (bv. het gekozen project)
//   data-placeholder / data-placeholder-more / data-empty-text   teksten
//
// Toetsen: ↑↓ lopen door de lijst, Enter kiest (of voegt een getypt e-mailadres toe), Backspace in een leeg vak haalt de laatste chip weg, Esc sluit de lijst.
// Events (bubbelen): `change` op de verborgen input (waarde = e-mailadressen, komma-gescheiden) en `gl-v2:people-change` {items}.
// API: window.GlV2People = { init(scope), get(root) → [{id,text,sub}], set(root, items), clear(root) }  (root = het element met data-gl-v2-people)
(function () {
    "use strict";

    var openRoot = null;
    var EMAIL = /^[^\s@,;]+@[^\s@,;]+\.[^\s@,;]+$/;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
    function initials(text) {
        var t = String(text || "");
        if (t.indexOf("@") > 0 && t.indexOf(" ") < 0) return t.charAt(0).toUpperCase();
        return t.replace(/[^A-Za-zÀ-ÿ ]/g, "").split(" ").filter(Boolean).slice(0, 2).map(function (w) { return w.charAt(0); }).join("").toUpperCase() || "·";
    }

    function wire(root) {
        if (!root || root.hasAttribute("data-gl-v2-wired")) return;
        var hidden = root.querySelector('[data-role="people-value"]'), input = root.querySelector('[data-role="input"]');
        var box = root.querySelector('[data-role="box"]'), panel = root.querySelector('[data-role="panel"]');
        if (!hidden || !input || !box || !panel) return;
        root.setAttribute("data-gl-v2-wired", "1");

        var url = root.getAttribute("data-lookup-url"), contextSel = root.getAttribute("data-context-source");
        var ph = root.getAttribute("data-placeholder") || "", phMore = root.getAttribute("data-placeholder-more") || ph;
        var emptyText = root.getAttribute("data-empty-text") || "Niemand gevonden.";
        var chosen = [], results = [], hi = 0, term = "", debounce = 0, seq = 0, loaded = false;

        function context() { var el = contextSel ? document.querySelector(contextSel) : null; return el ? el.value : ""; }
        function has(id) { return chosen.some(function (c) { return c.id.toLowerCase() === String(id).toLowerCase(); }); }

        function drawChips() {
            Array.prototype.slice.call(box.querySelectorAll(".gl-v2-people-chip")).forEach(function (c) { c.remove(); });
            chosen.forEach(function (c) {
                var chip = document.createElement("span");
                chip.className = "gl-v2-people-chip";
                chip.title = c.id;
                chip.innerHTML = '<span class="gl-v2-people-chip-av">' + esc(initials(c.text)) + '</span><span class="gl-v2-people-chip-n">' + esc(c.text) + "</span>";
                var x = document.createElement("button");
                x.type = "button"; x.className = "gl-v2-people-chip-x"; x.setAttribute("aria-label", c.text + " verwijderen");
                x.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
                x.addEventListener("mousedown", function (e) { e.preventDefault(); });
                x.addEventListener("click", function () { remove(c.id); input.focus(); });
                chip.appendChild(x);
                box.insertBefore(chip, input);
            });
            input.placeholder = chosen.length ? phMore : ph;
        }
        function changed() {
            hidden.value = chosen.map(function (c) { return c.id; }).join(",");
            drawChips();
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
            root.dispatchEvent(new CustomEvent("gl-v2:people-change", { bubbles: true, detail: { items: chosen.slice() } }));
        }
        function add(it) {
            if (!it || !it.id || has(it.id)) return;
            chosen.push({ id: String(it.id), text: it.text || String(it.id), sub: it.sub || "" });
            input.value = ""; term = "";
            changed();
            search(true);
        }
        function remove(id) {
            chosen = chosen.filter(function (c) { return c.id.toLowerCase() !== String(id).toLowerCase(); });
            changed();
            if (panel.classList.contains("is-open")) search(true);
        }

        // ── paneel ─────────────────────────────────────────────────────────────────────────────
        function position() {
            if (window.innerWidth < 768) { panel.style.top = panel.style.bottom = panel.style.left = panel.style.width = panel.style.maxHeight = ""; panel.style.left = "8px"; panel.style.right = "8px"; panel.style.bottom = "8px"; panel.style.maxHeight = "60vh"; return; }
            var r = box.getBoundingClientRect();
            panel.style.right = "";
            panel.style.width = r.width + "px";
            panel.style.left = Math.max(8, r.left) + "px";
            var onder = window.innerHeight - r.bottom - 14, boven = r.top - 14, nodig = Math.min(panel.scrollHeight, 300);
            if (nodig > onder && boven > onder) { panel.style.top = ""; panel.style.bottom = (window.innerHeight - r.top + 6) + "px"; panel.style.maxHeight = Math.max(160, Math.min(300, boven)) + "px"; }
            else { panel.style.bottom = ""; panel.style.top = (r.bottom + 6) + "px"; panel.style.maxHeight = Math.max(160, Math.min(300, onder)) + "px"; }
        }
        function open() {
            if (openRoot && openRoot !== root && openRoot._glV2People) openRoot._glV2People.close();
            openRoot = root;
            root.classList.add("is-open");
            panel.classList.add("is-open");
            input.setAttribute("aria-expanded", "true");
            position();
        }
        function close() {
            if (openRoot === root) openRoot = null;
            root.classList.remove("is-open");
            panel.classList.remove("is-open");
            input.setAttribute("aria-expanded", "false");
            window.clearTimeout(debounce);
        }
        function visible() { return results.filter(function (r) { return !has(r.id); }); }
        function render() {
            var list = visible(), html = "", last = null;
            if (term) html += '<div class="gl-v2-people-group">' + list.length + " gevonden</div>";
            list.forEach(function (it, k) {
                if (!term && it.group && it.group !== last) html += '<div class="gl-v2-people-group">' + esc(it.group) + "</div>";
                last = it.group || null;
                html += '<button type="button" tabindex="-1" role="option" class="gl-v2-people-option' + (k === hi ? " is-active" : "") + '" data-index="' + k + '">' +
                    '<span class="gl-v2-people-av">' + esc(initials(it.text)) + '</span><span class="gl-v2-people-text"><span class="gl-v2-people-name">' + esc(it.text) +
                    '</span><span class="gl-v2-people-sub">' + esc(it.sub || "") + "</span></span></button>";
            });
            if (!list.length) html += '<div class="gl-v2-people-empty">' + (loaded ? esc(emptyText) : "Even zoeken …") + "</div>";
            panel.innerHTML = html;
            var a = panel.querySelector(".is-active"); if (a && a.scrollIntoView) a.scrollIntoView({ block: "nearest" });
            position();
        }
        function search(now) {
            window.clearTimeout(debounce);
            if (!url) { results = []; loaded = true; render(); return; }
            var mySeq = ++seq;
            debounce = window.setTimeout(function () {
                var body = new URLSearchParams();
                body.set("term", term);
                body.set("context", context());
                fetch(url, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: body.toString(), credentials: "same-origin" })
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .catch(function () { return []; })
                    .then(function (data) {
                        if (mySeq !== seq) return;
                        results = (data || []).map(function (d) { return { id: String(d.id), text: d.text, sub: d.sub || "", group: d.group || "" }; });
                        loaded = true; hi = 0;
                        render();
                    });
            }, now ? 0 : 250);
        }

        // ── invoer ─────────────────────────────────────────────────────────────────────────────
        box.addEventListener("mousedown", function (e) { if (e.target !== input && !e.target.closest("button")) { e.preventDefault(); input.focus(); } });
        input.addEventListener("focus", function () { open(); render(); search(true); });
        input.addEventListener("click", function () { if (!panel.classList.contains("is-open")) { open(); render(); search(true); } });
        input.addEventListener("input", function () { term = input.value.trim(); hi = 0; open(); search(false); });
        input.addEventListener("blur", function () { window.setTimeout(function () { if (document.activeElement !== input) close(); }, 120); });
        panel.addEventListener("mousedown", function (e) { e.preventDefault(); });
        panel.addEventListener("click", function (e) {
            var o = e.target.closest(".gl-v2-people-option"); if (!o) return;
            add(visible()[parseInt(o.getAttribute("data-index"), 10)]);
            input.focus();
        });
        panel.addEventListener("mousemove", function (e) {
            var o = e.target.closest(".gl-v2-people-option"); if (!o) return;
            var k = parseInt(o.getAttribute("data-index"), 10);
            if (k !== hi) { hi = k; panel.querySelectorAll(".gl-v2-people-option").forEach(function (x) { x.classList.toggle("is-active", x === o); }); }
        });
        input.addEventListener("keydown", function (e) {
            var list = visible();
            if (e.key === "ArrowDown") { e.preventDefault(); open(); hi = Math.min(list.length - 1, hi + 1); render(); }
            else if (e.key === "ArrowUp") { e.preventDefault(); hi = Math.max(0, hi - 1); render(); }
            else if (e.key === "Enter") {
                if (!panel.classList.contains("is-open") && !input.value.trim()) return;
                e.preventDefault();   // Enter in dit vak dient het formulier nooit in
                var typed = input.value.trim();
                if (term && list[hi]) add(list[hi]);
                else if (EMAIL.test(typed)) add({ id: typed, text: typed, sub: "Extern e-mailadres" });
                else if (list[hi]) add(list[hi]);
            } else if (e.key === "Backspace" && !input.value && chosen.length) { remove(chosen[chosen.length - 1].id); }
            else if (e.key === "Escape") { if (panel.classList.contains("is-open")) { e.preventDefault(); e.stopPropagation(); close(); } }
            else if (e.key === "," || e.key === ";") { var t2 = input.value.trim(); if (EMAIL.test(t2)) { e.preventDefault(); add({ id: t2, text: t2, sub: "Extern e-mailadres" }); } }
        });
        window.addEventListener("resize", function () { if (panel.classList.contains("is-open")) position(); });
        window.addEventListener("scroll", function (e) { if (panel.classList.contains("is-open") && !panel.contains(e.target)) position(); }, true);

        root._glV2People = {
            close: close,
            get: function () { return chosen.slice(); },
            set: function (items) { chosen = (items || []).map(function (c) { return { id: String(c.id), text: c.text || String(c.id), sub: c.sub || "" }; }); input.value = ""; term = ""; changed(); },
            // De lijst hangt af van het project: na een andere keuze moet hij opnieuw opgehaald worden.
            refresh: function () { loaded = false; results = []; if (panel.classList.contains("is-open")) search(true); }
        };
        drawChips();
    }

    function init(scope) { (scope || document).querySelectorAll("[data-gl-v2-people]").forEach(wire); }

    document.addEventListener("click", function (e) { if (openRoot && !e.target.closest(".gl-v2-people") && !e.target.closest(".gl-v2-people-panel") && openRoot._glV2People) openRoot._glV2People.close(); });

    window.GlV2People = {
        init: init,
        get: function (root) { return root && root._glV2People ? root._glV2People.get() : []; },
        set: function (root, items) { if (root && root._glV2People) root._glV2People.set(items); },
        clear: function (root) { if (root && root._glV2People) root._glV2People.set([]); },
        refresh: function (root) { if (root && root._glV2People) root._glV2People.refresh(); }
    };

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", function () { init(document); }); else init(document);
})();
