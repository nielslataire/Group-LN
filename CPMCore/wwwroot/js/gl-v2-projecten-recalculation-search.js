// gl-v2 — Nacalculatie: "Factuur zoeken" in de topbalk (design-handoff 42a + 42m).
// Eén zoekvak; na 1 teken verschijnen de beste 6 facturen van het project, ↵ opent de bovenste, ↑↓ kiest,
// Esc sluit. De server (ProjectenController.RecalculationInvoiceSearch) raadt wat je typt (nummer,
// leverancier of categorie, datum, bedrag) en zegt het in "Zoekt op". Leeg vak = recent geopende
// facturen (per project in localStorage) + voorbeelden om te proberen.
(function () {
    "use strict";

    var root = document.getElementById("gln-nc-fs");
    if (!root) return;
    var input = root.querySelector(".gln-nc-fs-input");
    var panel = root.querySelector(".gln-nc-fs-panel");
    var box = root.querySelector(".gl-v2-field-box");
    var url = root.getAttribute("data-url");
    var projectId = root.getAttribute("data-project-id");
    var RECENT_KEY = "gln-nc-recent-" + projectId;

    var rows = [];        // navigeerbare opties: { kind: "inv"|"sup", ... }
    var hi = 0, open = false, seq = 0, timer = 0, cachedTips = null, showAll = false, lastData = null;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
    function rx(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"); }
    function mark(text, terms) {
        var t = esc(text);
        (terms || []).filter(function (x) { return x && x.length > 0; }).forEach(function (term) {
            t = t.replace(new RegExp("(" + rx(esc(term)) + ")", "ig"), "<mark>$1</mark>");
        });
        return t;
    }
    function readRecent() { try { return JSON.parse(localStorage.getItem(RECENT_KEY) || "[]"); } catch (e) { return []; } }
    function writeRecent(item) {
        try {
            var list = readRecent().filter(function (r) { return r.id !== item.id; });
            list.unshift({ id: item.id, url: item.url, nummer: item.nummer, intern: item.intern, leverancier: item.leverancier, datum: item.datum, bedrag: item.bedrag, type: item.type, tone: item.tone });
            localStorage.setItem(RECENT_KEY, JSON.stringify(list.slice(0, 5)));
        } catch (e) { /* localStorage kan geblokkeerd zijn: recent is een gemak, geen vereiste */ }
    }

    function position() {
        var r = box.getBoundingClientRect();
        if (window.innerWidth < 768) { panel.style.left = "8px"; panel.style.right = "8px"; panel.style.width = "auto"; }
        else {
            var w = Math.min(620, window.innerWidth - 16);
            var left = Math.max(8, Math.min(r.left, window.innerWidth - w - 8));
            panel.style.right = "auto"; panel.style.left = left + "px"; panel.style.width = w + "px";
        }
        panel.style.top = (r.bottom + 6) + "px";
    }
    function show() { if (open) { position(); return; } open = true; panel.classList.add("is-open"); input.setAttribute("aria-expanded", "true"); position(); }
    function hide() { open = false; panel.classList.remove("is-open"); input.setAttribute("aria-expanded", "false"); input.removeAttribute("aria-activedescendant"); window.clearTimeout(timer); }

    function invRow(it, k, terms, nrTerms) {
        var nameHtml = mark(it.leverancier || "—", terms);
        var nrHtml = mark(it.nummer, nrTerms);
        var dat = it.dMatch ? "<b class=\"is-hit\">" + esc(it.datum) + "</b>" : esc(it.datum);
        var amt = it.aMatch ? "<b class=\"is-hit\">" + esc(it.bedrag) + "</b>" : esc(it.bedrag);
        return '<button type="button" tabindex="-1" role="option" id="gln-nc-fs-o' + k + '" class="gln-nc-fs-opt' + (k === hi ? " is-active" : "") + '" aria-selected="' + (k === hi) + '" data-k="' + k + '">' +
            '<span class="gln-nc-fs-ico" aria-hidden="true"><i class="ph ph-receipt"></i></span>' +
            '<span class="gln-nc-fs-main"><span class="gln-nc-fs-line1"><span class="gln-nc-fs-name">' + nameHtml + '</span><span class="gln-nc-fs-nr">' + nrHtml + '</span></span>' +
            '<span class="gln-nc-fs-sub">' + dat + " · intern " + esc(it.intern) + "</span></span>" +
            '<span class="gln-nc-fs-side"><span class="gln-nc-fs-amount">' + amt + '</span><span class="gl-v2-badge ' + esc(it.tone) + '">' + esc(it.type) + "</span></span></button>";
    }

    function render(data, q) {
        lastData = data;
        rows = [];
        var html = "";
        var terms = data.textTerms || [], nrTerms = data.nrTerms || [];

        if (q && data.chips && data.chips.length) {
            html += '<div class="gln-nc-fs-chips"><span>Zoekt op</span>' + data.chips.map(function (c) {
                return '<span class="gln-nc-fs-chip"><svg aria-hidden="true" viewBox="0 0 16 16"><path d="' + esc(c.icon) + '"></path></svg>' + esc(c.label) + "</span>";
            }).join("") + "</div>";
        }

        var items = data.items || [];
        if (!q) {
            items = readRecent();
            if (items.length) html += '<div class="gln-nc-fs-head" role="presentation">Recent geopend</div>';
        } else if (items.length) {
            html += '<div class="gln-nc-fs-head" role="presentation">' + (data.total > items.length ? "Beste " + items.length + " van " + data.total + " facturen" : (data.total === 1 ? "1 factuur" : data.total + " facturen")) + "</div>";
        }
        items.forEach(function (it) { var k = rows.length; rows.push({ kind: "inv", item: it }); html += invRow(it, k, terms, nrTerms); });

        var sups = data.suppliers || [];
        if (q && sups.length) {
            html += '<div class="gln-nc-fs-head is-sep" role="presentation">Leveranciers</div>';
            sups.forEach(function (s) {
                var k = rows.length; rows.push({ kind: "sup", item: s });
                html += '<button type="button" tabindex="-1" role="option" id="gln-nc-fs-o' + k + '" class="gln-nc-fs-opt' + (k === hi ? " is-active" : "") + '" aria-selected="' + (k === hi) + '" data-k="' + k + '">' +
                    '<span class="gln-nc-fs-ico is-sup" aria-hidden="true"><i class="ph ph-truck"></i></span>' +
                    '<span class="gln-nc-fs-main"><span class="gln-nc-fs-name">' + mark(s.name, terms) + "</span></span>" +
                    '<span class="gln-nc-fs-sub">' + s.count + (s.count === 1 ? " factuur" : " facturen") + '</span><span class="gln-nc-fs-link">alle facturen →</span></button>';
            });
        }

        if (!q) {
            var tips = data.tips && data.tips.length ? data.tips : (cachedTips || []);
            if (tips.length) {
                html += '<div class="gln-nc-fs-tips"><span>Probeer</span>' + tips.map(function (t) {
                    return '<button type="button" class="gln-nc-fs-tip" tabindex="-1" data-tip="' + esc(t.q) + '"><code>' + esc(t.q) + "</code><span>" + esc(t.l) + "</span></button>";
                }).join("") + "</div>";
            }
            if (!items.length && !tips.length) html += '<div class="gln-nc-fs-none"><strong>Zoek een factuur van dit project</strong><span>Typ een deel van het nummer, de naam van de leverancier, een datum (10/2026, okt) of een bedrag (7670 of € 7.670,00).</span></div>';
        } else if (!items.length && !sups.length) {
            html += '<div class="gln-nc-fs-none"><strong>Geen factuur gevonden voor “' + esc(q) + '”</strong><span>Zoek op een deel van het nummer, de naam van de leverancier, een datum (10/2026, okt) of een bedrag (7670 of € 7.670,00). Ook gefactureerd in een ander project? Dan staat ze daar.</span></div>';
        }

        var all = q && data.total > items.length && !showAll
            ? '<button type="button" class="gln-nc-fs-all" tabindex="-1">Alle ' + data.total + " resultaten</button>" : "";
        html += '<div class="gln-nc-fs-foot">' + all + '<span class="gln-nc-fs-spacer"></span><span class="gln-nc-fs-keys"><kbd>↑</kbd><kbd>↓</kbd> kiezen &nbsp; <kbd>↵</kbd> openen &nbsp; <kbd>Esc</kbd> sluiten</span></div>';

        if (hi >= rows.length) hi = 0;
        panel.innerHTML = html;
        show();
        var active = rows.length ? document.getElementById("gln-nc-fs-o" + hi) : null;
        if (active) input.setAttribute("aria-activedescendant", active.id); else input.removeAttribute("aria-activedescendant");
    }

    function query() {
        var q = input.value.trim();
        var my = ++seq;
        var u = url + (url.indexOf("?") === -1 ? "?" : "&") + "q=" + encodeURIComponent(q) + "&take=" + (showAll ? 25 : 6);
        fetch(u, { headers: { Accept: "application/json" }, credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
            .then(function (data) {
                if (my !== seq) return;
                if (!q && data.tips && data.tips.length) cachedTips = data.tips;
                hi = 0;
                render(data, q);
            })
            .catch(function () {
                if (my !== seq) return;
                panel.innerHTML = '<div class="gln-nc-fs-none"><strong>Zoeken lukt nu niet</strong><span>Probeer het zo meteen opnieuw. Blijft het fout gaan, laat het weten.</span></div>';
                show();
            });
    }
    function schedule() { window.clearTimeout(timer); timer = window.setTimeout(query, input.value.trim() ? 140 : 0); }

    function choose(k) {
        var r = rows[k];
        if (!r) return;
        if (r.kind === "inv") { writeRecent(r.item); hide(); window.location.href = r.item.url; return; }
        // Leverancier: alle facturen van die leverancier tonen.
        input.value = r.item.name; showAll = true; hi = 0; query(); input.focus();
    }
    function setActive(k) {
        if (!rows.length) return;
        hi = (k + rows.length) % rows.length;
        Array.prototype.forEach.call(panel.querySelectorAll(".gln-nc-fs-opt"), function (el) {
            var on = Number(el.getAttribute("data-k")) === hi;
            el.classList.toggle("is-active", on); el.setAttribute("aria-selected", on ? "true" : "false");
            if (on) { input.setAttribute("aria-activedescendant", el.id); el.scrollIntoView({ block: "nearest" }); }
        });
    }

    input.addEventListener("focus", function () { showAll = false; schedule(); });
    input.addEventListener("click", function () { if (!open) schedule(); });
    input.addEventListener("input", function () { showAll = false; hi = 0; schedule(); });
    input.addEventListener("keydown", function (e) {
        if (e.key === "ArrowDown") { e.preventDefault(); if (!open) schedule(); else setActive(hi + 1); }
        else if (e.key === "ArrowUp") { e.preventDefault(); if (open) setActive(hi - 1); }
        else if (e.key === "Enter") { if (open && rows.length) { e.preventDefault(); choose(hi); } }
        else if (e.key === "Escape") { if (open) { e.preventDefault(); hide(); } else if (input.value) { input.value = ""; } }
        else if (e.key === "Tab") { hide(); }
    });

    // mousedown i.p.v. click: het vak verliest anders zijn focus (en dus de lijst) vóór de klik aankomt.
    panel.addEventListener("mousedown", function (e) {
        e.preventDefault();
        var opt = e.target.closest(".gln-nc-fs-opt");
        if (opt) { choose(Number(opt.getAttribute("data-k"))); return; }
        var tip = e.target.closest(".gln-nc-fs-tip");
        if (tip) { input.value = tip.getAttribute("data-tip"); showAll = false; hi = 0; query(); return; }
        if (e.target.closest(".gln-nc-fs-all")) { showAll = true; hi = 0; query(); }
    });
    panel.addEventListener("mousemove", function (e) {
        var opt = e.target.closest(".gln-nc-fs-opt");
        if (opt) { var k = Number(opt.getAttribute("data-k")); if (k !== hi) setActive(k); }
    });
    document.addEventListener("mousedown", function (e) { if (open && !root.contains(e.target) && !panel.contains(e.target)) hide(); });
    input.addEventListener("blur", function () { window.setTimeout(function () { if (document.activeElement !== input) hide(); }, 120); });
    window.addEventListener("resize", function () { if (open) position(); });
    window.addEventListener("scroll", function () { if (open) position(); }, true);
})();
