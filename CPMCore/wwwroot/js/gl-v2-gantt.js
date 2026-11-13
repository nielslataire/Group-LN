/* gl-v2 — Gantt-tijdlijn (projectwijd component, nov. 2026; ontwerp: design-handoff "CRM Planning" 59a).
   Tekent ENKEL de grafiek (tijdschaal, balken, afhankelijkheidspijlen, niet-werkdagen, vandaag en deadline). De rijen links (lijst) zijn
   de zaak van de pagina; elke rij is rowHeight hoog zodat lijst en grafiek naast elkaar lopen.

   Gebruik:
     var g = GlV2Gantt.render(containerEl, model, { onOpen: function (key) {}, onSelect: function (key) {} });
     g.scrollToToday(); g.scrollToKey("t12");

   model = {
     rows:  [{ key, type: "task"|"phase"|"milestone", start, end, progress (0-100), critical, done, pinned, shifted, label, aria,
               baseStart, baseEnd, hidden }]      // data als "yyyy-mm-dd"; rows met hidden:true krijgen geen balk maar houden hun rij niet bij (de pagina filtert ze weg)
     links: [{ from: key, to: key, type: "FS"|"SS"|"FF"|"SF", critical }],
     nonWorking: [{ date, kind: "verlof"|"verlet" }],   // weekends tekent de component zelf
     today: "yyyy-mm-dd", deadline: "yyyy-mm-dd"|null,
     scale: "dag"|"week"|"maand", showCritical: bool, showBase: bool, rowHeight: 34 } */
(function () {
    "use strict";

    var DAY = 86400000;
    var MONTHS = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    var MONTHS_LONG = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var PX = { dag: 28, week: 10, maand: 3 };

    function toN(iso) { var p = String(iso).split("-"); return Math.round(Date.UTC(+p[0], +p[1] - 1, +p[2]) / DAY); }
    function toD(n) { return new Date(n * DAY); }
    function dow(n) { return (toD(n).getUTCDay() + 6) % 7; } // 0 = maandag
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;" }[c]; }); }
    function isoWeek(n) {
        var d = toD(n); var day = (d.getUTCDay() + 6) % 7;
        var th = new Date(d.getTime() + (3 - day) * DAY);
        var jan4 = Date.UTC(th.getUTCFullYear(), 0, 4);
        return 1 + Math.round(((th.getTime() - jan4) / DAY - 3 + ((new Date(jan4).getUTCDay() + 6) % 7)) / 7);
    }

    function render(el, model, opts) {
        opts = opts || {};
        var rh = model.rowHeight || 34;
        var scale = PX[model.scale] ? model.scale : "dag";
        var px = PX[scale];
        var rows = model.rows || [];

        // ── bereik ──
        var lo = Infinity, hi = -Infinity;
        function span(iso) { if (!iso) return; var n = toN(iso); if (n < lo) lo = n; if (n > hi) hi = n; }
        rows.forEach(function (r) { span(r.start); span(r.end); if (model.showBase) { span(r.baseStart); span(r.baseEnd); } });
        span(model.today); span(model.deadline);
        if (!isFinite(lo)) { lo = toN(model.today || "2026-01-01"); hi = lo + 60; }
        lo -= 7; hi += 28;
        lo -= dow(lo); // op een maandag beginnen
        var total = hi - lo + 1;
        var width = total * px;
        var height = rows.length * rh;
        var idx = {};
        rows.forEach(function (r, i) { idx[r.key] = i; });

        function x(n) { return (n - lo) * px; }

        // ── kop ──
        var top = "", bottom = "";
        if (scale === "maand") {
            var cur = toD(lo), y = cur.getUTCFullYear(), m = cur.getUTCMonth();
            var yStart = lo;
            var guard = 0;
            while (guard++ < 400) {
                var mStart = Math.round(Date.UTC(y, m, 1) / DAY), mEnd = Math.round(Date.UTC(y, m + 1, 1) / DAY) - 1;
                var s = Math.max(mStart, lo), e = Math.min(mEnd, hi);
                if (s > hi) break;
                bottom += "<span class=\"gl-v2-gantt-h2\" style=\"left:" + x(s) + "px;width:" + (e - s + 1) * px + "px\">" + MONTHS[m] + "</span>";
                if (m === 0 || s === lo) {
                    var yEnd = Math.min(Math.round(Date.UTC(y + 1, 0, 1) / DAY) - 1, hi);
                    top += "<span class=\"gl-v2-gantt-h1\" style=\"left:" + x(s) + "px;width:" + (yEnd - s + 1) * px + "px\">" + y + "</span>";
                }
                m++; if (m > 11) { m = 0; y++; }
            }
        } else {
            var c2 = toD(lo), y2 = c2.getUTCFullYear(), m2 = c2.getUTCMonth(), g2 = 0;
            while (g2++ < 400) {
                var ms = Math.round(Date.UTC(y2, m2, 1) / DAY), me = Math.round(Date.UTC(y2, m2 + 1, 1) / DAY) - 1;
                var s2 = Math.max(ms, lo), e2 = Math.min(me, hi);
                if (s2 > hi) break;
                top += "<span class=\"gl-v2-gantt-h1\" style=\"left:" + x(s2) + "px;width:" + (e2 - s2 + 1) * px + "px\">" + MONTHS_LONG[m2] + " " + y2 + "</span>";
                m2++; if (m2 > 11) { m2 = 0; y2++; }
            }
            if (scale === "dag") {
                for (var n = lo; n <= hi; n++) {
                    var wd = dow(n);
                    bottom += "<span class=\"gl-v2-gantt-h2" + (wd > 4 ? " is-weekend" : "") + "\" style=\"left:" + x(n) + "px;width:" + px + "px\">" + toD(n).getUTCDate() + "</span>";
                }
            } else {
                for (var w = lo; w <= hi; w += 7) {
                    bottom += "<span class=\"gl-v2-gantt-h2\" style=\"left:" + x(w) + "px;width:" + 7 * px + "px\">w" + isoWeek(w) + "</span>";
                }
            }
        }

        // ── niet-werkdagen ──
        var shade = "";
        if (scale !== "maand") {
            for (var d = lo; d <= hi; d++) {
                if (dow(d) === 5) { shade += "<span class=\"gl-v2-gantt-nw is-weekend\" style=\"left:" + x(d) + "px;width:" + 2 * px + "px\"></span>"; d++; }
                else if (dow(d) === 6) { shade += "<span class=\"gl-v2-gantt-nw is-weekend\" style=\"left:" + x(d) + "px;width:" + px + "px\"></span>"; }
            }
            (model.nonWorking || []).forEach(function (nw) {
                var n2 = toN(nw.date);
                if (n2 < lo || n2 > hi) return;
                shade += "<span class=\"gl-v2-gantt-nw is-" + esc(nw.kind) + "\" style=\"left:" + x(n2) + "px;width:" + px + "px\" title=\"" + (nw.kind === "verlet" ? "Verletdag" : "Feestdag of bouwverlof") + "\"></span>";
            });
        }

        // ── balken ──
        var bars = "", labels = "", bases = "";
        rows.forEach(function (r, i) {
            if (!r.start || !r.end) return;
            var s = toN(r.start), e = toN(r.end);
            var yTop = i * rh;
            var cls = "gl-v2-gantt-bar is-" + (r.type || "task")
                + (r.critical && model.showCritical ? " is-critical" : "")
                + (r.done ? " is-done" : "")
                + (r.shifted ? " is-shifted" : "");
            var left = x(s), w = Math.max((e - s + 1) * px, 3);
            var aria = esc(r.aria || r.label || "");
            if (r.type === "milestone") {
                left = x(s) + px / 2 - 8;
                bars += "<button type=\"button\" class=\"" + cls + "\" data-key=\"" + esc(r.key) + "\" style=\"left:" + left + "px;top:" + (yTop + (rh - 16) / 2) + "px\" aria-label=\"" + aria + "\"></button>";
                labels += "<span class=\"gl-v2-gantt-label\" style=\"left:" + (left + 22) + "px;top:" + (yTop + (rh - 16) / 2) + "px\">" + esc(r.label) + "</span>";
            } else {
                var h = r.type === "phase" ? 10 : 20;
                var t = yTop + (rh - h) / 2 - (model.showBase && (r.baseStart && r.baseEnd) ? 2 : 0);
                bars += "<button type=\"button\" class=\"" + cls + "\" data-key=\"" + esc(r.key) + "\" style=\"left:" + left + "px;width:" + w + "px;top:" + t + "px;height:" + h + "px\" aria-label=\"" + aria + "\">"
                    + (r.progress > 0 ? "<span class=\"gl-v2-gantt-progress\" style=\"width:" + Math.min(100, r.progress) + "%\"></span>" : "")
                    + (r.pinned ? "<i class=\"ph ph-push-pin gl-v2-gantt-pin\" aria-hidden=\"true\"></i>" : "")
                    + "</button>";
                labels += "<span class=\"gl-v2-gantt-label\" style=\"left:" + (left + w + 6) + "px;top:" + (t + (h - 16) / 2) + "px\">" + esc(r.label) + "</span>";
            }
            if (model.showBase && r.baseStart && r.baseEnd) {
                var bs = toN(r.baseStart), be = toN(r.baseEnd);
                if (r.type === "milestone") bases += "<span class=\"gl-v2-gantt-base-ms\" style=\"left:" + (x(bs) + px / 2 - 5) + "px;top:" + (yTop + rh - 9) + "px\"></span>";
                else bases += "<span class=\"gl-v2-gantt-base\" style=\"left:" + x(bs) + "px;width:" + Math.max((be - bs + 1) * px, 3) + "px;top:" + (yTop + rh - 8) + "px\"></span>";
            }
        });

        // ── afhankelijkheden ──
        var paths = "";
        (model.links || []).forEach(function (l) {
            var a = rows[idx[l.from]], b = rows[idx[l.to]];
            if (!a || !b || !a.start || !b.start) return;
            var type = l.type || "FS";
            var ay = idx[l.from] * rh + rh / 2, by = idx[l.to] * rh + rh / 2;
            var aEnd = type === "FS" || type === "FF";            // vertrekt aan het einde van de voorganger
            var bStart = type === "FS" || type === "SS";          // komt aan bij het begin van de opvolger
            var ax = aEnd ? x(toN(a.end)) + (a.type === "milestone" ? px / 2 + 8 : px) : x(toN(a.start)) + (a.type === "milestone" ? px / 2 - 8 : 0);
            var bx = bStart ? x(toN(b.start)) + (b.type === "milestone" ? px / 2 - 8 : 0) : x(toN(b.end)) + (b.type === "milestone" ? px / 2 + 8 : px);
            var mid = ay + (by >= ay ? 1 : -1) * (rh / 2);
            if (by === ay) mid = ay + rh / 2;
            var p;
            if (type === "FS") {
                var xo = ax + 7, xi = bx - 7;
                p = xi >= xo ? "M" + ax + "," + ay + " H" + xo + " V" + by + " H" + bx
                             : "M" + ax + "," + ay + " H" + xo + " V" + mid + " H" + xi + " V" + by + " H" + bx;
            } else if (type === "SS") {
                var ch = Math.min(ax, bx) - 7;
                p = "M" + ax + "," + ay + " H" + ch + " V" + by + " H" + bx;
            } else if (type === "FF") {
                var ch2 = Math.max(ax, bx) + 7;
                p = "M" + ax + "," + ay + " H" + ch2 + " V" + by + " H" + bx;
            } else { // SF
                p = "M" + ax + "," + ay + " H" + (ax - 7) + " V" + mid + " H" + (bx + 7) + " V" + by + " H" + bx;
            }
            paths += "<path d=\"" + p + "\" class=\"gl-v2-gantt-link" + (l.critical && model.showCritical ? " is-critical" : "") + "\" marker-end=\"url(#gl-v2-gantt-arrow" + (l.critical && model.showCritical ? "-c" : "") + ")\" />";
        });

        // ── vandaag en deadline ──
        var marks = "";
        if (model.today) { var tn = toN(model.today); if (tn >= lo && tn <= hi) marks += "<span class=\"gl-v2-gantt-today\" style=\"left:" + (x(tn) + px / 2) + "px\"><b>vandaag</b></span>"; }
        if (model.deadline) { var dn = toN(model.deadline); if (dn >= lo && dn <= hi) marks += "<span class=\"gl-v2-gantt-deadline\" style=\"left:" + (x(dn) + px) + "px\"><b>deadline</b></span>"; }

        el.innerHTML =
            "<div class=\"gl-v2-gantt-inner\" style=\"width:" + width + "px\">" +
            "<div class=\"gl-v2-gantt-head\" style=\"width:" + width + "px\"><div class=\"gl-v2-gantt-head-top\">" + top + "</div><div class=\"gl-v2-gantt-head-bottom\">" + bottom + "</div></div>" +
            "<div class=\"gl-v2-gantt-body\" style=\"width:" + width + "px;height:" + height + "px;--gl-v2-gantt-rh:" + rh + "px\">" +
            shade + marks +
            "<svg class=\"gl-v2-gantt-svg\" width=\"" + width + "\" height=\"" + height + "\" aria-hidden=\"true\">" +
            "<defs><marker id=\"gl-v2-gantt-arrow\" markerWidth=\"7\" markerHeight=\"7\" refX=\"6\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" class=\"gl-v2-gantt-arrow\"/></marker>" +
            "<marker id=\"gl-v2-gantt-arrow-c\" markerWidth=\"7\" markerHeight=\"7\" refX=\"6\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" class=\"gl-v2-gantt-arrow is-critical\"/></marker></defs>" +
            paths + "</svg>" + bases + bars + labels +
            "</div></div>";

        var inner = el.querySelector(".gl-v2-gantt-inner");
        el.classList.add("gl-v2-gantt");
        el.setAttribute("data-scale", scale);

        el.onclick = function (ev) {
            var bar = ev.target.closest(".gl-v2-gantt-bar");
            if (!bar) return;
            if (opts.onSelect) opts.onSelect(bar.getAttribute("data-key"));
            if (opts.onOpen) opts.onOpen(bar.getAttribute("data-key"));
        };

        return {
            width: width,
            scrollToToday: function (scroller) {
                if (!model.today) return;
                var tx = x(toN(model.today));
                (scroller || el).scrollLeft = Math.max(0, tx - 120);
            },
            xOf: function (iso) { return x(toN(iso)); },
        };
    }

    window.GlV2Gantt = { render: render };
})();
