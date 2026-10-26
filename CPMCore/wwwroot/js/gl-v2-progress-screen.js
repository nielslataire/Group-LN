// gl-v2 — Voortgangsscherm op volle pagina (design-handoff punt 44 "Verslag voorbereiden", 44d–f budget-pdf en 48 facturen): ÉÉN renderer
// voor alle pagina's die zo'n scherm tonen (Projecten/Verslagen/EditV2, Budget/BudgetPdfOpmakenV2, Invoices/Index en CreateV2).
// Opmaak: css/gl-v2/progress-screen.css (klassen .gl-v2-vsprep__*). De pagina houdt zijn eigen host-element, voortgangslogica en knoppen; dit bestand
// bouwt enkel de inhoud.
//
//   var host = document.getElementById("...");              // een <section class="gl-v2-vsprep" role="status">
//   GlV2ProgressScreen.render(host, {
//       art: "report" | "invoice" | "send" | "sync" | "ok" | "warn",
//       kicker, warn (gouden kicker), title, sub,            // gewone tekst (wordt geëscaped)
//       steps: [{ t, m, d, s: "done|act|todo|err|skip", strike, muted }],   // t = tekst, m = rechts, d = detailregel, strike = doorgestreept
//       busy: true,                                          // toont de lopende balk en de rollende tekst onderaan
//       foot: ["…", "…"],                                    // rollende teksten (alleen met busy)
//       note: "…",                                           // vaste tekst onderaan (zonder busy)
//       actions: "<html>"  of  [{ label, id, href, primary }]  // knoppen; met id → <button data-pscr="id">
//   });
//   GlV2ProgressScreen.updateSteps(host, steps);  GlV2ProgressScreen.stop(host);  GlV2ProgressScreen.stepsHtml(steps)
(function () {
    "use strict";

    var CHECK = '<svg viewBox="0 0 16 16"><path d="M3.4 8.4l3 3 6.2-6.8"/></svg>';
    var CROSS = '<svg viewBox="0 0 16 16"><path d="M8 4.4v4.4M8 11.4v.1"/></svg>';
    var MINUS = '<svg viewBox="0 0 16 16"><path d="M4.4 8h7.2"/></svg>';
    var PLANE = '<svg viewBox="0 0 24 24"><path d="M2.5 11.2 21 3.4l-5.6 17.2-4.6-6.4-6.6-2.2z"/><path d="M10.8 14.2 21 3.4" stroke="#fff" stroke-width="1.2"/></svg>';

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }

    function lines(list) {
        return list.map(function (l) { return '<span class="ln" style="width:' + l[0] + 'px;top:' + l[2] + 'px;animation-delay:' + l[1] + 's"></span>'; }).join("");
    }
    function art(kind) {
        if (kind === "ok" || kind === "warn") {
            return '<div class="gl-v2-vsprep__ok' + (kind === "warn" ? " is-warn" : "") + '"><span class="ring"></span><span class="disc">' + (kind === "warn" ? '<svg viewBox="0 0 16 16"><path d="M8 4v5M8 11.6v.1"/></svg>' : CHECK) + '</span></div>';
        }
        if (kind === "send") {
            return '<div class="gl-v2-vsprep__send"><svg class="path" viewBox="0 0 260 190"><path d="M20 165 C 90 155, 150 105, 236 30"/></svg><div class="gl-v2-vsprep__plane">' + PLANE + '</div>' +
                '<span class="gl-v2-vsprep__chip" style="left:206px;top:12px">OCT</span><span class="gl-v2-vsprep__chip" style="left:228px;top:50px">@</span><span class="gl-v2-vsprep__chip" style="left:176px;top:2px">PEP</span></div>';
        }
        if (kind === "sync") {
            return '<div class="gl-v2-vsprep__sync"><svg class="path" viewBox="0 0 260 190"><path d="M86 95 H 174"/></svg>' +
                '<div class="gl-v2-vsprep__node on" style="left:10px"><b>CRM</b><small>facturen</small></div><div class="gl-v2-vsprep__node" style="left:174px"><b>Octopus</b><small>boekhouding</small></div>' +
                '<span class="gl-v2-vsprep__mini"></span><span class="gl-v2-vsprep__mini" style="animation-delay:.6s"></span><span class="gl-v2-vsprep__mini" style="animation-delay:1.2s"></span></div>';
        }
        if (kind === "invoice") {
            return '<div class="gl-v2-vsprep__float"><div class="gl-v2-vsprep__sheet back"></div><div class="gl-v2-vsprep__sheet front"><span class="top"></span><span class="ttl" style="width:50px"></span><span class="num">0000.00</span>' +
                lines([[100, 0, 44], [84, .25, 56], [96, .5, 74], [70, .75, 86]]) + '<span class="tot"></span><span class="qr"></span><span class="scan"></span></div></div>';
        }
        // "report": blad met foto's (verslag, budget)
        return '<div class="gl-v2-vsprep__float"><div class="gl-v2-vsprep__sheet back"></div><div class="gl-v2-vsprep__sheet front"><span class="top"></span><span class="ttl"></span><span class="ph1"></span><span class="ph2"></span>' +
            lines([[100, 0, 82], [84, .25, 94], [96, .5, 112], [70, .75, 124], [90, 1, 142]]) + '<span class="scan"></span></div></div>';
    }

    function icon(st) {
        if (st === "done") return '<span class="gl-v2-vsprep__ico done">' + CHECK + '</span>';
        if (st === "err") return '<span class="gl-v2-vsprep__ico err">' + CROSS + '</span>';
        if (st === "skip") return '<span class="gl-v2-vsprep__ico skip">' + MINUS + '</span>';
        return '<span class="gl-v2-vsprep__ico ' + st + '"><i></i></span>';
    }
    function stepsHtml(steps) {
        return (steps || []).map(function (s) {
            var style = s.muted ? ' style="color:#9aa898"' : "";
            return '<div class="gl-v2-vsprep__step is-' + s.s + '"' + style + '>' + icon(s.s) + '<b>' + (s.strike ? "<s>" + esc(s.t) + "</s>" : esc(s.t)) + '</b><em' + style + '>' + esc(s.m || "") + '</em>' + (s.d ? '<small>' + esc(s.d) + '</small>' : "") + '</div>';
        }).join("");
    }

    function stop(host) { if (host && host._pscrTimer) { clearInterval(host._pscrTimer); host._pscrTimer = null; } }

    function render(host, o) {
        stop(host);
        var actions = "";
        if (typeof o.actions === "string") actions = o.actions;
        else if (o.actions && o.actions.length) {
            actions = o.actions.map(function (a) {
                var cls = "gl-v2-btn " + (a.primary ? "gl-v2-btn-primary" : "gl-v2-btn-secondary");
                return a.href ? '<a class="' + cls + '" href="' + esc(a.href) + '">' + esc(a.label) + '</a>' : '<button type="button" class="' + cls + '" data-pscr="' + esc(a.id) + '">' + esc(a.label) + '</button>';
            }).join("");
        }
        var showSteps = !!(o.steps && o.steps.length) || o.stepsAlways;
        host.innerHTML = '<div class="gl-v2-vsprep__col"><div class="gl-v2-vsprep__art">' + art(o.art) + '</div>' +
            '<div class="gl-v2-vsprep__head"><span class="gl-v2-vsprep__kicker' + (o.warn ? " is-warn" : "") + '">' + esc(o.kicker) + '</span><h2 class="gl-v2-vsprep__title">' + esc(o.title) + '</h2><p class="gl-v2-vsprep__sub">' + esc(o.sub) + '</p></div>' +
            (o.busy ? '<div class="gl-v2-vsprep__bar"><i></i></div>' : "") +
            (showSteps ? '<div class="gl-v2-vsprep__steps">' + stepsHtml(o.steps) + '</div>' : "") +
            (o.busy ? '<p class="gl-v2-vsprep__foot"><span></span></p>' : "") +
            (!o.busy && o.note ? '<p class="gl-v2-vsprep__foot">' + esc(o.note) + '</p>' : "") +
            (actions ? '<div class="gl-v2-vsprep__actions">' + actions + '</div>' : "") + '</div>';
        var span = host.querySelector(".gl-v2-vsprep__foot span");
        if (span && o.foot && o.foot.length) {
            var i = 0;
            var next = function () { span.textContent = o.foot[i++ % o.foot.length]; var c = span.cloneNode(true); span.replaceWith(c); span = c; };
            next(); host._pscrTimer = setInterval(next, 2800);
        }
        return host;
    }
    function updateSteps(host, steps) { var el = host && host.querySelector(".gl-v2-vsprep__steps"); if (el) el.innerHTML = stepsHtml(steps); }

    window.GlV2ProgressScreen = { render: render, updateSteps: updateSteps, stop: stop, stepsHtml: stepsHtml, art: art };
})();
