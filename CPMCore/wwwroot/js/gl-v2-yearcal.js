// gl-v2 projectbrede component — jaarkalender (12 maandkaarten) waarop dagen in een "modus" gezet worden.
// Uit de weerverlet-kalender (design-handoff 10a) gehaald zodat meerdere pagina's ze kunnen hergebruiken
// (Weerverlet: regen/wind; Instellingen/Verlofdagen, punt 50: bouwverlof/feestdag). Opmaak: css/gl-v2/yearcal.css,
// gedocumenteerd in DESIGN.md "Jaarkalender (GlV2YearCal)". Het component kent geen server: de pagina bewaart zelf
// de dagen en zegt via getCats()/getLock() wat er op een dag staat. Het component tekent, telt en vangt klik/sleep op.
//
//   var cal = GlV2YearCal.create(document.getElementById("mijn-kalender"), {
//       year: 2026,
//       categories: { rain: { label: "Regendag", bg: "#D7E6EF", fg: "#2E5F7E" }, ... },   // modi die je kunt zetten
//       locks:      { vacation: { label: "Verlofdag", bg: "...", fg: "..." } },          // dagen die vastliggen (niet klikbaar)
//       summary:    ["rain", "wind"],                  // welke tellers in de maandkop staan (standaard alle categorieën)
//       armed:      "rain",                            // de modus die een klik zet
//       readonly:   false,
//       getCats:    function (iso) { return ["rain"]; },   // categorieën op deze dag (array van sleutels)
//       getLock:    function (iso) { return null; },       // sleutel uit `locks`, of null
//       onToggle:   function (iso, armedKey) { },          // één klik op een klikbare dag: pagina zet of wist
//       onRange:    function (isos, armedKey) { },         // slepen over meerdere dagen: pagina zet alle dagen
//       onRender:   function (info) { }                    // { year, counts: {sleutel: n}, total }
//   });
//   cal.render();  cal.setYear(2027);  cal.prev();  cal.next();  cal.setArmed("wind");  cal.setReadonly(true);
//
// Weekends zijn nooit klikbaar en tellen niet mee; staat er toch een categorie op een weekenddag (bv. een feestdag),
// dan toont de cel een stipje in die kleur. Een enkele klik toggelt; slepen (muis/pen, niet aanraking) zet een reeks.
(function () {
    "use strict";

    var DOW = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
    var MONTHS = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var INK = "#2C3B2A";

    function pad(n) { return String(n).padStart(2, "0"); }
    function isoOf(y, m, d) { return y + "-" + pad(m) + "-" + pad(d); }

    function create(root, options) {
        var o = Object.assign({
            year: new Date().getFullYear(), categories: {}, locks: {}, summary: null, armed: null, readonly: false,
            getCats: function () { return []; }, getLock: function () { return null; },
            onToggle: null, onRange: null, onRender: null
        }, options || {});
        var armed = o.armed || Object.keys(o.categories)[0] || null;
        var year = o.year;
        var suppressNextClick = false;
        var drag = null;

        root.classList.add("gl-v2-yc");

        function summaryKeys() { return o.summary || Object.keys(o.categories); }
        function armedFg() { return armed && o.categories[armed] ? o.categories[armed].fg : INK; }

        function cellStyle(cell, cats, lock, weekend) {
            if (lock && o.locks[lock]) {
                cell.style.background = o.locks[lock].bg;
                cell.style.color = o.locks[lock].fg;
                return;
            }
            var known = cats.filter(function (k) { return o.categories[k]; });
            if (!known.length) return;
            if (weekend) { cell.style.setProperty("--yc-dot", o.categories[known[0]].fg); return; }
            if (known.length === 1) {
                cell.style.background = o.categories[known[0]].bg;
                cell.style.color = o.categories[known[0]].fg;
            } else {
                cell.style.background = "linear-gradient(135deg, " + o.categories[known[0]].bg + " 0 50%, " + o.categories[known[1]].bg + " 50% 100%)";
                cell.style.color = INK;
            }
        }

        function renderMonth(frag, mi, totals) {
            var card = document.createElement("div");
            card.className = "gl-v2-yc-month";

            var head = document.createElement("div");
            head.className = "gl-v2-yc-month-head";
            var name = document.createElement("span");
            name.className = "gl-v2-yc-month-name";
            name.textContent = MONTHS[mi];
            head.appendChild(name);
            var countEls = {};
            summaryKeys().forEach(function (k) {
                var c = document.createElement("span");
                c.className = "gl-v2-yc-month-count";
                if (o.categories[k]) c.style.color = o.categories[k].fg;
                countEls[k] = c;
                head.appendChild(c);
            });
            card.appendChild(head);

            var grid = document.createElement("div");
            grid.className = "gl-v2-yc-grid";
            DOW.forEach(function (l) {
                var dow = document.createElement("div");
                dow.className = "gl-v2-yc-dow";
                dow.textContent = l;
                grid.appendChild(dow);
            });

            var offset = (new Date(year, mi, 1).getDay() + 6) % 7;      // maandag eerst
            var days = new Date(year, mi + 1, 0).getDate();
            var counts = {};
            for (var i = 0; i < 42; i++) {
                var dayNum = i - offset + 1;
                var cell = document.createElement("div");
                cell.className = "gl-v2-yc-day";
                if (dayNum < 1 || dayNum > days) {
                    cell.classList.add("is-blank");
                    grid.appendChild(cell);
                    continue;
                }
                var dow0 = new Date(year, mi, dayNum).getDay();
                var weekend = dow0 === 0 || dow0 === 6;
                var key = isoOf(year, mi + 1, dayNum);
                var cats = o.getCats(key) || [];
                var lock = o.getLock(key);
                cell.textContent = String(dayNum);
                cell.dataset.date = key;
                if (lock) cell.classList.add("is-locked");
                else if (weekend) { cell.classList.add("is-weekend"); if (cats.length) cell.classList.add("has-cats"); }
                else if (cats.length) cell.classList.add("has-cats");
                if (!lock && !weekend) cell.classList.add("is-eligible");
                cellStyle(cell, cats, lock, weekend && !lock);

                var label = dayNum + " " + MONTHS[mi];
                var parts = cats.map(function (k) { return o.categories[k] ? o.categories[k].label : k; });
                if (lock && o.locks[lock]) parts.unshift(o.locks[lock].label);
                if (weekend && !lock) parts.push("weekend");
                if (parts.length) { cell.title = label + " · " + parts.join(", "); cell.setAttribute("aria-label", cell.title); }

                if (!lock && !weekend) cats.forEach(function (k) { counts[k] = (counts[k] || 0) + 1; });
                grid.appendChild(cell);
            }
            summaryKeys().forEach(function (k) {
                countEls[k].textContent = String(counts[k] || 0);
                totals[k] = (totals[k] || 0) + (counts[k] || 0);
            });
            card.appendChild(grid);
            frag.appendChild(card);
        }

        function render() {
            root.removeAttribute("aria-busy");
            root.style.setProperty("--yc-armed", armedFg());
            var totals = {};
            var frag = document.createDocumentFragment();
            for (var m = 0; m < 12; m++) renderMonth(frag, m, totals);
            root.innerHTML = "";
            root.appendChild(frag);
            var total = 0;
            Object.keys(totals).forEach(function (k) { total += totals[k]; });
            if (o.onRender) o.onRender({ year: year, counts: totals, total: total });
        }

        function eligible(cell) { return !!cell && cell.classList.contains("is-eligible"); }

        function onClick(e) {
            if (o.readonly) return;
            if (suppressNextClick) { suppressNextClick = false; return; }
            var cell = e.target.closest(".gl-v2-yc-day");
            if (!eligible(cell) || !o.onToggle) return;
            o.onToggle(cell.dataset.date, armed);
        }
        function onDown(e) {
            if (o.readonly || e.pointerType === "touch") return;
            var cell = e.target.closest(".gl-v2-yc-day");
            if (!eligible(cell)) return;
            e.preventDefault();
            drag = { pointerId: e.pointerId, cells: [cell.dataset.date] };
            cell.classList.add("is-drag-preview");
            root.setPointerCapture(e.pointerId);
        }
        function onMove(e) {
            if (!drag || drag.pointerId !== e.pointerId) return;
            var t = document.elementFromPoint(e.clientX, e.clientY);
            var cell = t && t.closest ? t.closest(".gl-v2-yc-day") : null;
            if (!eligible(cell) || !root.contains(cell)) return;
            if (drag.cells.indexOf(cell.dataset.date) === -1) { drag.cells.push(cell.dataset.date); cell.classList.add("is-drag-preview"); }
        }
        function onUp(e) {
            if (!drag || drag.pointerId !== e.pointerId) return;
            var cells = drag.cells;
            drag = null;
            root.querySelectorAll(".gl-v2-yc-day.is-drag-preview").forEach(function (c) { c.classList.remove("is-drag-preview"); });
            // preventDefault in pointerdown onderdrukt de daaropvolgende "click" voor muis/pen: de enkele klik wordt hier afgehandeld.
            suppressNextClick = true;
            if (cells.length > 1) { if (o.onRange) o.onRange(cells, armed); }
            else if (cells.length === 1 && o.onToggle) o.onToggle(cells[0], armed);
        }
        root.addEventListener("click", onClick);
        root.addEventListener("pointerdown", onDown);
        root.addEventListener("pointermove", onMove);
        root.addEventListener("pointerup", onUp);
        root.addEventListener("pointercancel", onUp);

        root.classList.toggle("is-readonly", !!o.readonly);

        return {
            render: render,
            getYear: function () { return year; },
            setYear: function (y) { year = y; render(); },
            prev: function () { year--; render(); },
            next: function () { year++; render(); },
            getArmed: function () { return armed; },
            setArmed: function (k) { armed = k; root.style.setProperty("--yc-armed", armedFg()); },
            setReadonly: function (b) { o.readonly = !!b; root.classList.toggle("is-readonly", !!b); },
            setMessage: function (html) { root.innerHTML = '<div class="gl-v2-yc-message">' + html + "</div>"; },
            root: root
        };
    }

    // Legende-rij: items { text, label, bg, fg, kind: "weekend" | "weekend-dot" | "hover" }. Eén stijl voor alle pagina's.
    function renderLegend(el, items, hint) {
        el.classList.add("gl-v2-yc-legend");
        var html = '<span class="gl-v2-yc-legend-label">Legende</span>';
        items.forEach(function (it) {
            var cls = "gl-v2-yc-swatch" + (it.kind ? " is-" + it.kind : "");
            var style = it.kind === "weekend-dot" ? ' style="--yc-dot:' + it.fg + '"' :
                it.kind === "hover" ? ' style="color:' + it.fg + '"' :
                    it.bg ? ' style="background:' + it.bg + ";color:" + (it.fg || INK) + '"' : "";
            html += '<span class="gl-v2-yc-legend-item"><span class="' + cls + '"' + style + ">" + (it.text || "") + "</span>" + it.label + "</span>";
        });
        if (hint) html += '<span class="gl-v2-yc-legend-spacer"></span><span class="gl-v2-yc-legend-hint">' + hint + "</span>";
        el.innerHTML = html;
    }

    window.GlV2YearCal = { create: create, renderLegend: renderLegend, months: MONTHS, isoOf: isoOf };
})();
