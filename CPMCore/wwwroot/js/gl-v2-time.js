// gl-v2 — tijdveld (Views/Shared/GlV2/_TimeField.cshtml, wwwroot/css/gl-v2/time.css). Eén implementatie voor het hele project: elke `[data-gl-v2-time]` wordt vanzelf bedraad.
// Je tikt het uur gewoon in (9, 930, 9:30, 9u30, 09.30 → "09:30") of kiest het uit de lijst. De gebonden waarde staat in een verborgen "HH:mm"-input die een bubbelend `change` vuurt
// (zelfde patroon als het datumveld). Pijl omhoog/omlaag verschuift de tijd met één stap, Enter bevestigt wat je typte, Esc sluit de lijst zonder te wijzigen.
//
//   data-step   stap van de standaardlijst in minuten (15)
//   data-from   eerste tijd van de standaardlijst ("07:00");  data-to  laatste ("20:00")
//
// API: window.GlV2Time = {
//   init(scope), byId(id),                       // byId("mt-an-van") → het veld (de wrapper met id "<id>_picker")
//   getMinutes(root) → minuten sinds middernacht of null,
//   setValue(root, "HH:mm" | minuten | null),    // zonder `change` te vuren mag via setValue(root, v, true)
//   setItems(root, [{ minutes, note, busy }]),   // vervangt de lijst; `note` staat rechts, `busy` kleurt de note als waarschuwing. null = terug naar de standaardlijst
//   setNote(root, tekst)                         // tekst in het vak zelf, rechts van het uur (bv. de duur "1u")
// }
(function () {
    "use strict";

    var openRoot = null;

    function pad(n) { return n < 10 ? "0" + n : "" + n; }
    function fmt(min) { return pad(Math.floor(min / 60)) + ":" + pad(min % 60); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }

    // "9", "930", "0930", "9:30", "9.30", "9u30", "9h" → minuten sinds middernacht, of null als het geen geldig uur is.
    function parse(text) {
        var t = String(text || "").trim().toLowerCase().replace(/\s+/g, "");
        if (!t) return null;
        var h, m, r = /^(\d{1,2})[:.hu](\d{0,2})$/.exec(t);
        if (r) { h = +r[1]; m = r[2] === "" ? 0 : +r[2]; }
        else if (/^\d{1,2}$/.test(t)) { h = +t; m = 0; }
        else if (/^\d{3}$/.test(t)) { h = +t.charAt(0); m = +t.slice(1); }
        else if (/^\d{4}$/.test(t)) { h = +t.slice(0, 2); m = +t.slice(2); }
        else return null;
        return h < 24 && m < 60 ? h * 60 + m : null;
    }
    function parseHM(v) { var p = /^(\d{2}):(\d{2})$/.exec(v || ""); return p ? +p[1] * 60 + +p[2] : null; }

    function closeOpen() { if (openRoot && openRoot._glV2Time) openRoot._glV2Time.close(); }

    function wire(root) {
        if (!root || root.hasAttribute("data-gl-v2-time-wired")) return;
        var box = root.querySelector(".gl-v2-field-box"), input = root.querySelector('[data-role="time-text"]'), hidden = root.querySelector('[data-role="time-value"]');
        var panel = root.querySelector('[data-role="panel"]'), noteEl = root.querySelector('[data-role="note"]');
        var fieldEl = root.closest(".gl-v2-field"), errorEl = fieldEl && fieldEl.querySelector('[data-role="field-error"]');
        if (!box || !input || !hidden || !panel) return;
        root.setAttribute("data-gl-v2-time-wired", "");

        var step = parseInt(root.getAttribute("data-step") || "15", 10) || 15;
        var from = parseHM(root.getAttribute("data-from")); if (from == null) from = 7 * 60;
        var to = parseHM(root.getAttribute("data-to")); if (to == null) to = 20 * 60;
        var value = parseHM(hidden.value);
        var custom = null;   // [{minutes, note, busy}] van de pagina; null = standaardlijst

        function setError(msg) {
            if (fieldEl) fieldEl.classList.add("is-error");
            if (errorEl) { errorEl.textContent = msg; errorEl.hidden = false; }
        }
        function clearError() {
            if (fieldEl) fieldEl.classList.remove("is-error");
            if (errorEl) { errorEl.textContent = ""; errorEl.hidden = true; }
        }
        function items() {
            if (custom) return custom;
            var l = [];
            for (var m = from; m <= to; m += step) l.push({ minutes: m });
            return l;
        }
        function render() {
            panel.innerHTML = items().map(function (it) {
                return '<button type="button" tabindex="-1" role="option" class="gl-v2-time-option' + (it.minutes === value ? " is-selected" : "") + '" data-minutes="' + it.minutes + '">' +
                    '<span class="gl-v2-time-option-t">' + fmt(it.minutes) + "</span>" +
                    '<span class="gl-v2-time-option-n' + (it.busy ? " is-busy" : "") + '">' + esc(it.note || "") + "</span></button>";
            }).join("");
        }
        function position() {
            if (window.innerWidth < 768) { panel.style.top = panel.style.bottom = panel.style.left = panel.style.maxHeight = ""; return; }   // gsm: het gedeelde bottom-sheet-gedrag van .gl-v2-select-panel
            var r = box.getBoundingClientRect();
            panel.style.left = Math.max(8, Math.min(r.left, window.innerWidth - 230 - 8)) + "px";
            var onder = window.innerHeight - r.bottom - 14, boven = r.top - 14;
            if (onder < 240 && boven > onder) { panel.style.top = ""; panel.style.bottom = (window.innerHeight - r.top + 6) + "px"; panel.style.maxHeight = Math.max(160, Math.min(280, boven)) + "px"; }
            else { panel.style.bottom = ""; panel.style.top = (r.bottom + 6) + "px"; panel.style.maxHeight = Math.max(160, Math.min(280, onder)) + "px"; }
        }
        function open() {
            if (openRoot && openRoot !== root) closeOpen();
            openRoot = root;
            render();
            position();
            panel.classList.add("is-open");
            root.classList.add("is-open");
            input.setAttribute("aria-expanded", "true");
            var sel = panel.querySelector(".is-selected");
            if (sel) panel.scrollTop = Math.max(0, sel.offsetTop - panel.clientHeight / 2 + sel.offsetHeight / 2);
            else if (value != null) {   // geen exacte treffer (bv. 09:10): scroll naar het eerstvolgende
                var next = Array.prototype.find.call(panel.querySelectorAll(".gl-v2-time-option"), function (b) { return +b.dataset.minutes >= value; });
                if (next) panel.scrollTop = Math.max(0, next.offsetTop - panel.clientHeight / 2);
            }
        }
        function close() {
            if (openRoot === root) openRoot = null;
            panel.classList.remove("is-open");
            root.classList.remove("is-open");
            input.setAttribute("aria-expanded", "false");
        }
        function set(min, silent) {
            value = min == null ? null : Math.max(0, Math.min(1439, min));
            hidden.value = value == null ? "" : fmt(value);
            input.value = hidden.value;
            clearError();
            if (!silent) hidden.dispatchEvent(new Event("change", { bubbles: true }));
        }
        // Wat er getypt staat vastleggen: leeg wist de waarde, een ongeldige tekst geeft een foutmelding en laat de vorige waarde staan.
        function commit() {
            var txt = input.value.trim();
            if (!txt) { if (value != null) set(null); return true; }
            var m = parse(txt);
            if (m == null) { setError("Geef een uur in, zoals 09:30."); return false; }
            if (m !== value || input.value !== hidden.value) set(m);
            return true;
        }

        box.addEventListener("mousedown", function (e) { if (e.target !== input && !e.target.closest("button")) { e.preventDefault(); input.focus(); } });
        input.addEventListener("focus", function () { open(); try { input.select(); } catch (e) { } });
        input.addEventListener("click", function () { if (!panel.classList.contains("is-open")) open(); });
        input.addEventListener("input", function () {
            input.value = input.value.replace(/[^0-9:.hu]/gi, "");
            clearError();
            var m = parse(input.value);   // meeliften in de lijst zonder al te committen
            var cur = panel.querySelector(".is-selected"); if (cur) cur.classList.remove("is-selected");
            if (m != null) { var hit = panel.querySelector('[data-minutes="' + m + '"]'); if (hit) { hit.classList.add("is-selected"); panel.scrollTop = Math.max(0, hit.offsetTop - panel.clientHeight / 2); } }
        });
        input.addEventListener("blur", function () {
            window.setTimeout(function () { if (document.activeElement !== input && !panel.matches(":hover")) { commit(); close(); } }, 120);
        });
        input.addEventListener("keydown", function (e) {
            if (e.key === "Enter") { if (commit()) { e.preventDefault(); close(); } else e.preventDefault(); }
            else if (e.key === "Escape") { if (panel.classList.contains("is-open")) { e.preventDefault(); e.stopPropagation(); input.value = hidden.value; clearError(); close(); } }
            else if (e.key === "ArrowUp" || e.key === "ArrowDown") {
                e.preventDefault();
                var base = parse(input.value); if (base == null) base = value != null ? value : from;
                set(base + (e.key === "ArrowUp" ? -step : step));
                if (panel.classList.contains("is-open")) open();
            } else if (e.key === "Tab") { commit(); close(); }
        });
        panel.addEventListener("mousedown", function (e) { e.preventDefault(); });   // het veld houdt de focus bij een klik in de lijst
        panel.addEventListener("click", function (e) {
            var b = e.target.closest(".gl-v2-time-option"); if (!b) return;
            set(+b.dataset.minutes);
            close();
            input.focus();
        });
        window.addEventListener("resize", function () { if (panel.classList.contains("is-open")) position(); });
        window.addEventListener("scroll", function (e) { if (panel.classList.contains("is-open") && !panel.contains(e.target)) position(); }, true);

        root._glV2Time = {
            close: close,
            getMinutes: function () { return value; },
            setValue: function (v, silent) { set(typeof v === "number" ? v : (v ? parseHM(v) : null), silent); },
            setItems: function (list) { custom = list && list.length ? list : null; if (panel.classList.contains("is-open")) { render(); } },
            setNote: function (text) { if (!noteEl) return; noteEl.textContent = text || ""; noteEl.hidden = !text; }
        };
    }

    function init(scope) { (scope || document).querySelectorAll("[data-gl-v2-time]").forEach(wire); }

    document.addEventListener("click", function (e) { if (openRoot && !e.target.closest(".gl-v2-timefield") && !e.target.closest(".gl-v2-time-panel")) closeOpen(); });

    window.GlV2Time = {
        init: init,
        byId: function (id) { return document.getElementById(id + "_picker"); },
        getMinutes: function (root) { return root && root._glV2Time ? root._glV2Time.getMinutes() : null; },
        setValue: function (root, v, silent) { if (root && root._glV2Time) root._glV2Time.setValue(v, silent); },
        setItems: function (root, list) { if (root && root._glV2Time) root._glV2Time.setItems(list); },
        setNote: function (root, text) { if (root && root._glV2Time) root._glV2Time.setNote(text); },
        parse: parse
    };

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", function () { init(document); }); else init(document);
})();
