/* gl-v2 — Gantt-tijdlijn (projectwijd component, nov. 2026; ontwerp: design-handoff "CRM Planning" 59a).
   Tekent ENKEL de grafiek (tijdschaal, balken, afhankelijkheidspijlen, niet-werkdagen, vandaag en deadline). De rijen links (lijst) zijn
   de zaak van de pagina; elke rij is rowHeight hoog zodat lijst en grafiek naast elkaar lopen.

   Gebruik:
     var g = GlV2Gantt.render(containerEl, model, { onOpen: function (key) {}, onSelect: function (key) {} });
     g.scrollToToday(scroller); g.scrollToKey("t12", scroller); g.hover("t12"); g.select("t12");

   De kop (twee rijen, 48px) is position:sticky; top:0 en de maand-/jaarlabels plakken links in beeld (sticky). Dat werkt enkel als de
   pagina de grafiek in EEN scrollvlak met begrensde hoogte zet (overflow:auto, zie gl-v2-projecten-planning.css). De pagina zet op dat scrollvlak
   de variabele --gl-v2-gantt-stick = breedte van de vastgeplakte lijst, zodat labels niet onder de lijst verdwijnen.

   model = {
     rows:  [{ key, type: "task"|"phase"|"milestone", start, end, progress (0-100), critical, done, pinned, shifted, label, aria,
               baseStart, baseEnd }]                 // data als "yyyy-mm-dd"
     links: [{ from: key, to: key, type: "FS"|"SS"|"FF"|"SF", critical }],
     nonWorking: [{ date, kind: "verlof"|"verlet" }],   // weekends tekent de component zelf
     today: "yyyy-mm-dd", deadline: "yyyy-mm-dd"|null, // enkel getekend als ze dicht bij de planning liggen (anders rekt de grafiek onnodig uit)
     scale: "dag"|"week"|"maand"|"alles", fitWidth: px (enkel bij "alles": de hele planning past in deze breedte),
     showCritical: bool, showBase: bool, rowHeight: 34 } */
(function () {
    "use strict";

    var DAY = 86400000;
    var MONTHS = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    var MONTHS_LONG = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var PX = { dag: 28, week: 10, maand: 3 };
    var HEAD_H = 48;
    var NEAR = 90; // vandaag/deadline tellen enkel mee in het bereik als ze hoogstens zoveel dagen buiten de planning vallen

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
    function reduced() { return window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches; }

    function render(el, model, opts) {
        opts = opts || {};
        var rh = model.rowHeight || 34;
        var rows = model.rows || [];

        // ── bereik: de taken, plus vandaag en de deadline als die er dicht bij liggen ──
        var lo = Infinity, hi = -Infinity;
        function span(iso) { if (!iso) return; var n = toN(iso); if (n < lo) lo = n; if (n > hi) hi = n; }
        rows.forEach(function (r) { span(r.start); span(r.end); if (model.showBase) { span(r.baseStart); span(r.baseEnd); } });
        var haveRange = isFinite(lo);
        function near(iso) { if (!iso) return false; if (!haveRange) return true; var n = toN(iso); return n >= lo - NEAR && n <= hi + NEAR; }
        var todayN = near(model.today) ? toN(model.today) : null;
        var deadN = near(model.deadline) ? toN(model.deadline) : null;
        if (todayN != null) { if (todayN < lo) lo = todayN; if (todayN > hi) hi = todayN; }
        if (deadN != null) { if (deadN < lo) lo = deadN; if (deadN > hi) hi = deadN; }
        if (!isFinite(lo)) { lo = toN(model.today || "2026-01-01"); hi = lo + 60; }
        lo -= 7; hi += 28;
        lo -= dow(lo); // op een maandag beginnen
        var total = hi - lo + 1;

        // ── schaal: vaste stappen, of "alles" = de hele planning in de beschikbare breedte ──
        var px, scale, tail = 0; // tail = ruimte rechts voor het label van de laatste balk (enkel bij 'alles', anders loopt het label buiten het beeld)
        if (model.scale === "alles") {
            tail = 200;
            px = Math.min(28, Math.max(0.4, ((model.fitWidth || 800) - tail) / total));
            scale = px >= 20 ? "dag" : px >= 6 ? "week" : "maand";
        } else {
            scale = PX[model.scale] ? model.scale : "dag";
            px = PX[scale];
        }
        var width = Math.round(total * px) + tail;
        var height = rows.length * rh;
        var idx = {};
        rows.forEach(function (r, i) { idx[r.key] = i; });

        function x(n) { return (n - lo) * px; }
        function r1(v) { return Math.round(v * 10) / 10; }

        // ── kop ──
        var top = "", bottom = "";
        function lbl(left, w, text, cls) {
            return "<span class=\"gl-v2-gantt-h1" + (cls || "") + "\" style=\"left:" + r1(left) + "px;width:" + r1(w) + "px\"><i class=\"gl-v2-gantt-lbl\">" + text + "</i></span>";
        }
        if (scale === "maand") {
            var cur = toD(lo), y = cur.getUTCFullYear(), m = cur.getUTCMonth();
            var guard = 0;
            while (guard++ < 400) {
                var mStart = Math.round(Date.UTC(y, m, 1) / DAY), mEnd = Math.round(Date.UTC(y, m + 1, 1) / DAY) - 1;
                var s = Math.max(mStart, lo), e = Math.min(mEnd, hi);
                if (s > hi) break;
                bottom += "<span class=\"gl-v2-gantt-h2\" style=\"left:" + r1(x(s)) + "px;width:" + r1((e - s + 1) * px) + "px\">" + (px >= 2.2 ? MONTHS[m] : MONTHS[m].charAt(0)) + "</span>";
                if (m === 0 || s === lo) {
                    var yEnd = Math.min(Math.round(Date.UTC(y + 1, 0, 1) / DAY) - 1, hi);
                    top += lbl(x(s), (yEnd - s + 1) * px, y);
                }
                m++; if (m > 11) { m = 0; y++; }
            }
        } else {
            var c2 = toD(lo), y2 = c2.getUTCFullYear(), m2 = c2.getUTCMonth(), g2 = 0;
            while (g2++ < 400) {
                var ms = Math.round(Date.UTC(y2, m2, 1) / DAY), me = Math.round(Date.UTC(y2, m2 + 1, 1) / DAY) - 1;
                var s2 = Math.max(ms, lo), e2 = Math.min(me, hi);
                if (s2 > hi) break;
                top += lbl(x(s2), (e2 - s2 + 1) * px, MONTHS_LONG[m2] + " " + y2);
                m2++; if (m2 > 11) { m2 = 0; y2++; }
            }
            if (scale === "dag") {
                for (var n = lo; n <= hi; n++) {
                    var wd = dow(n);
                    bottom += "<span class=\"gl-v2-gantt-h2" + (wd > 4 ? " is-weekend" : "") + "\" style=\"left:" + r1(x(n)) + "px;width:" + r1(px) + "px\">" + toD(n).getUTCDate() + "</span>";
                }
            } else {
                for (var w = lo; w <= hi; w += 7) {
                    bottom += "<span class=\"gl-v2-gantt-h2\" style=\"left:" + r1(x(w)) + "px;width:" + r1(7 * px) + "px\">w" + isoWeek(w) + "</span>";
                }
            }
        }

        // ── maandgrenzen (doorlopende lijn) en niet-werkdagen ──
        var lines = "";
        (function () {
            var c = toD(lo), yy = c.getUTCFullYear(), mm = c.getUTCMonth(), g = 0;
            while (g++ < 400) {
                var st = Math.round(Date.UTC(yy, mm, 1) / DAY);
                if (st > hi) break;
                if (st > lo) lines += "<span class=\"gl-v2-gantt-vl" + (mm === 0 ? " is-year" : "") + "\" style=\"left:" + r1(x(st)) + "px\"></span>";
                mm++; if (mm > 11) { mm = 0; yy++; }
            }
        })();

        var shade = "";
        if (px >= 4) {
            for (var d = lo; d <= hi; d++) {
                if (dow(d) === 5) { shade += "<span class=\"gl-v2-gantt-nw is-weekend\" style=\"left:" + r1(x(d)) + "px;width:" + r1(2 * px) + "px\"></span>"; d++; }
                else if (dow(d) === 6) { shade += "<span class=\"gl-v2-gantt-nw is-weekend\" style=\"left:" + r1(x(d)) + "px;width:" + r1(px) + "px\"></span>"; }
            }
            (model.nonWorking || []).forEach(function (nw) {
                var n2 = toN(nw.date);
                if (n2 < lo || n2 > hi) return;
                shade += "<span class=\"gl-v2-gantt-nw is-" + esc(nw.kind) + "\" style=\"left:" + r1(x(n2)) + "px;width:" + r1(px) + "px\" title=\"" + (nw.kind === "verlet" ? "Verletdag" : "Feestdag of bouwverlof") + "\"></span>";
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
                bars += "<button type=\"button\" class=\"" + cls + "\" data-key=\"" + esc(r.key) + "\" style=\"left:" + r1(left) + "px;top:" + (yTop + (rh - 16) / 2) + "px\" aria-label=\"" + aria + "\"></button>";
                labels += "<span class=\"gl-v2-gantt-label\" style=\"left:" + r1(left + 22) + "px;top:" + (yTop + (rh - 16) / 2) + "px\">" + esc(r.label) + "</span>";
            } else {
                var h = r.type === "phase" ? 10 : 20;
                var t = yTop + (rh - h) / 2 - (model.showBase && (r.baseStart && r.baseEnd) ? 2 : 0);
                bars += "<button type=\"button\" class=\"" + cls + "\" data-key=\"" + esc(r.key) + "\" style=\"left:" + r1(left) + "px;width:" + r1(w) + "px;top:" + t + "px;height:" + h + "px\" aria-label=\"" + aria + "\">"
                    + (r.progress > 0 ? "<span class=\"gl-v2-gantt-progress\" style=\"width:" + Math.min(100, r.progress) + "%\"></span>" : "")
                    + (r.pinned ? "<i class=\"ph ph-push-pin gl-v2-gantt-pin\" aria-hidden=\"true\"></i>" : "")
                    + "</button>";
                labels += "<span class=\"gl-v2-gantt-label\" style=\"left:" + r1(left + w + 6) + "px;top:" + (t + (h - 16) / 2) + "px\">" + esc(r.label) + "</span>";
            }
            if (model.showBase && r.baseStart && r.baseEnd) {
                var bs = toN(r.baseStart), be = toN(r.baseEnd);
                if (r.type === "milestone") bases += "<span class=\"gl-v2-gantt-base-ms\" style=\"left:" + r1(x(bs) + px / 2 - 5) + "px;top:" + (yTop + rh - 9) + "px\"></span>";
                else bases += "<span class=\"gl-v2-gantt-base\" style=\"left:" + r1(x(bs)) + "px;width:" + r1(Math.max((be - bs + 1) * px, 3)) + "px;top:" + (yTop + rh - 8) + "px\"></span>";
            }
        });

        // ── afhankelijkheden (bij een heel kleine schaal enkel ruis: dan niet tekenen) ──
        var paths = "";
        if (px >= 4) (model.links || []).forEach(function (l) {
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
                p = xi >= xo ? "M" + r1(ax) + "," + ay + " H" + r1(xo) + " V" + by + " H" + r1(bx)
                             : "M" + r1(ax) + "," + ay + " H" + r1(xo) + " V" + mid + " H" + r1(xi) + " V" + by + " H" + r1(bx);
            } else if (type === "SS") {
                var ch = Math.min(ax, bx) - 7;
                p = "M" + r1(ax) + "," + ay + " H" + r1(ch) + " V" + by + " H" + r1(bx);
            } else if (type === "FF") {
                var ch2 = Math.max(ax, bx) + 7;
                p = "M" + r1(ax) + "," + ay + " H" + r1(ch2) + " V" + by + " H" + r1(bx);
            } else { // SF
                p = "M" + r1(ax) + "," + ay + " H" + r1(ax - 7) + " V" + mid + " H" + r1(bx + 7) + " V" + by + " H" + r1(bx);
            }
            paths += "<path d=\"" + p + "\" class=\"gl-v2-gantt-link" + (l.critical && model.showCritical ? " is-critical" : "") + "\" marker-end=\"url(#gl-v2-gantt-arrow" + (l.critical && model.showCritical ? "-c" : "") + ")\" />";
        });

        // ── vandaag en deadline: de lijn loopt door de hele grafiek, het label plakt onder de kop ──
        var marks = "";
        var flags = "";
        if (todayN != null) {
            marks += "<span class=\"gl-v2-gantt-today\" style=\"left:" + r1(x(todayN) + px / 2) + "px\"></span>";
            flags += "<b class=\"gl-v2-gantt-flag is-today\" style=\"left:" + r1(x(todayN) + px / 2) + "px\">vandaag</b>";
        }
        if (deadN != null) {
            marks += "<span class=\"gl-v2-gantt-deadline\" style=\"left:" + r1(x(deadN) + px) + "px\"></span>";
            flags += "<b class=\"gl-v2-gantt-flag is-deadline\" style=\"left:" + r1(x(deadN) + px) + "px\">deadline</b>";
        }

        el.innerHTML =
            "<div class=\"gl-v2-gantt-inner\" style=\"width:" + width + "px\">" +
            "<div class=\"gl-v2-gantt-head\" style=\"width:" + width + "px\"><div class=\"gl-v2-gantt-head-top\">" + top + "</div><div class=\"gl-v2-gantt-head-bottom\">" + bottom + "</div>" + flags + "</div>" +
            "<div class=\"gl-v2-gantt-body" + (scale === "maand" ? " is-coarse" : "") + "\" style=\"width:" + width + "px;height:" + height + "px;--gl-v2-gantt-rh:" + rh + "px;--gl-v2-gantt-wk:" + r1(7 * px) + "px\">" +
            "<i class=\"gl-v2-gantt-band is-hover\" hidden style=\"height:" + rh + "px\"></i><i class=\"gl-v2-gantt-band is-sel\" hidden style=\"height:" + rh + "px\"></i>" +
            shade + lines + marks +
            "<svg class=\"gl-v2-gantt-svg\" width=\"" + width + "\" height=\"" + height + "\" aria-hidden=\"true\">" +
            "<defs><marker id=\"gl-v2-gantt-arrow\" markerWidth=\"7\" markerHeight=\"7\" refX=\"6\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" class=\"gl-v2-gantt-arrow\"/></marker>" +
            "<marker id=\"gl-v2-gantt-arrow-c\" markerWidth=\"7\" markerHeight=\"7\" refX=\"6\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" class=\"gl-v2-gantt-arrow is-critical\"/></marker></defs>" +
            paths + "</svg>" + bases + bars + labels +
            "</div></div>";

        el.classList.add("gl-v2-gantt");
        el.setAttribute("data-scale", scale);
        var body = el.querySelector(".gl-v2-gantt-body");
        var bandHover = el.querySelector(".gl-v2-gantt-band.is-hover");
        var bandSel = el.querySelector(".gl-v2-gantt-band.is-sel");

        el.onclick = function (ev) {
            var bar = ev.target.closest(".gl-v2-gantt-bar");
            if (!bar) return;
            if (opts.onSelect) opts.onSelect(bar.getAttribute("data-key"));
            if (opts.onOpen) opts.onOpen(bar.getAttribute("data-key"));
        };

        function place(band, key) {
            var i = key == null ? undefined : idx[key];
            if (i == null) { band.hidden = true; return; }
            band.style.top = (i * rh) + "px"; band.hidden = false;
        }
        function smooth() { return reduced() ? "auto" : "smooth"; }
        function listW() { return el.offsetLeft; } // de lijst staat vóór de grafiek in hetzelfde scrollvlak

        return {
            width: width, px: px, scale: scale, hasToday: todayN != null,
            xOf: function (iso) { return x(toN(iso)); },
            hover: function (key) { place(bandHover, key); },
            select: function (key) { place(bandSel, key); },
            /** Welke rij ligt onder deze schermhoogte (clientY)? */
            keyAt: function (clientY) {
                var i = Math.floor((clientY - body.getBoundingClientRect().top) / rh);
                return i >= 0 && i < rows.length ? rows[i].key : null;
            },
            /** Datum (yyyy-mm-dd) bij een x-positie in de grafiek; voor "blijf op dezelfde datum staan" na een zoomwissel. */
            dateAt: function (cx) {
                var d = toD(Math.floor(lo + Math.max(0, cx) / px)), p = function (v) { return (v < 10 ? "0" : "") + v; };
                return d.getUTCFullYear() + "-" + p(d.getUTCMonth() + 1) + "-" + p(d.getUTCDate());
            },
            scrollToToday: function (scroller, instant) {
                if (todayN == null) return false;
                (scroller || el).scrollTo({ left: Math.max(0, x(todayN) - 160), behavior: instant ? "auto" : smooth() });
                return true;
            },
            scrollToStart: function (scroller, instant) {
                var first = Infinity;
                rows.forEach(function (r) { if (r.start) { var n = toN(r.start); if (n < first) first = n; } });
                (scroller || el).scrollTo({ left: isFinite(first) ? Math.max(0, x(first) - 60) : 0, behavior: instant ? "auto" : smooth() });
            },
            /** Brengt de rij in beeld, onder de vaste kop en naast de vaste lijst. */
            scrollToKey: function (key, scroller) {
                var i = idx[key], r = rows[i], sc = scroller || el;
                if (i == null || !r) return;
                var visW = sc.clientWidth - listW();
                var out = { behavior: smooth() };
                if (r.start && r.end) {
                    var l = x(toN(r.start)), rt = x(toN(r.end)) + px;
                    if (l < sc.scrollLeft + 24 || rt > sc.scrollLeft + visW - 24) out.left = Math.max(0, l - Math.min(120, visW / 4));
                }
                var rowTop = HEAD_H + i * rh;
                if (rowTop < sc.scrollTop + HEAD_H) out.top = Math.max(0, i * rh - rh);
                else if (rowTop + rh > sc.scrollTop + sc.clientHeight) out.top = rowTop + rh * 2 - sc.clientHeight;
                if (out.left != null || out.top != null) sc.scrollTo(out);
            }
        };
    }

    window.GlV2Gantt = { render: render, HEAD_HEIGHT: HEAD_H };
})();
