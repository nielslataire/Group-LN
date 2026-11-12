// gl-v2 — Punten afdrukken (design-handoff 40u). Modal vanuit "Afdrukken" (#pt-print, op gsm ook #pt-print-m): welke punten, soort afdruk,
// groeperen, sorteren, opnemen + een voorbeeld dat meeverandert. De pdf komt van ProjectsIssues/PrintPdf; "Afdrukken" toont hem in het
// afdrukvenster van de browser, "Download PDF" bewaart hem. De lijst zelf leest dit via window.GlV2PuntenList (gl-v2-projecten-punten.js).
(function () {
    "use strict";
    var modalEl = document.getElementById("pt-print-modal");
    var openBtn = document.getElementById("pt-print");
    if (!modalEl || !openBtn) return;

    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function list() { return window.GlV2PuntenList || { applied: function () { return []; }, all: function () { return []; }, checked: function () { return []; }, statusFilter: function () { return "open"; }, filtersText: function () { return ""; } }; }

    var PREF_KEY = "gl-v2-punten-print";
    var TYPES = {
        lijst: { name: "puntenlijst", fmt: "A4 liggend", hint: "Kolommen zoals de lijst op het scherm.", landscape: true },
        aannemer: { name: "per aannemer", fmt: "A4 staand · 1 pagina per aannemer", hint: "Groeperen staat vast op aannemer.", landscape: false },
        rondgang: { name: "rondgang", fmt: "A4 staand", hint: "Vakje per punt en een lijn voor je notitie.", landscape: false }
    };
    var state = { scope: "sel", type: "lijst", group: "geen", sort: "deadline", inc: { photos: true, plans: true, history: false, closed: false, internal: false } };

    // ── keuzes onthouden voor de volgende keer (per browser; scope en "afgesloten" hangen van de lijst af en blijven niet staan) ──
    function loadPrefs() {
        try {
            var p = JSON.parse(localStorage.getItem(PREF_KEY) || "null");
            if (!p) return;
            if (TYPES[p.type]) state.type = p.type;
            if (["geen", "aannemer", "eenheid", "fase"].indexOf(p.group) >= 0) state.group = p.group;
            if (["deadline", "nr", "eenheid", "status"].indexOf(p.sort) >= 0) state.sort = p.sort;
            if (p.inc) ["photos", "plans", "history", "internal"].forEach(function (k) { if (typeof p.inc[k] === "boolean") state.inc[k] = p.inc[k]; });
        } catch (e) { /* geen opslag: standaardkeuzes */ }
    }
    function savePrefs() {
        try { localStorage.setItem(PREF_KEY, JSON.stringify({ type: state.type, group: state.group, sort: state.sort, inc: { photos: state.inc.photos, plans: state.inc.plans, history: state.inc.history, internal: state.inc.internal } })); } catch (e) { /* idem */ }
    }

    // ── welke rijen ──
    function isClosed(tr) { return tr.dataset.closed === "1"; }
    function scopeRows() {
        var rows = state.scope === "open" ? list().all().filter(function (tr) { return !isClosed(tr); })
            : state.scope === "check" ? list().checked()
            : list().applied();
        return state.inc.closed ? rows : rows.filter(function (tr) { return !isClosed(tr); });
    }
    function plural(n, one, many) { return n + " " + (n === 1 ? one : many); }
    function groupEffective() { return state.type === "aannemer" ? "aannemer" : state.group; }

    // ── scherm bijwerken ──
    function setRadio(container, attr, value) {
        $$("[" + attr + "]", container).forEach(function (b) { b.setAttribute("aria-checked", b.getAttribute(attr) === value ? "true" : "false"); });
    }
    function render() {
        var n = scopeRows().length;
        var checkedN = list().checked().length;
        var withSel = list().applied().length, openN = list().all().filter(function (tr) { return !isClosed(tr); }).length;
        $("[data-scope-hint='sel']", modalEl).textContent = plural(withSel, "punt", "punten") + " · met je filters";
        $("[data-scope-hint='open']", modalEl).textContent = plural(openN, "punt", "punten");
        $("[data-scope-hint='check']", modalEl).textContent = checkedN ? plural(checkedN, "punt", "punten") : "geen punten aangevinkt";
        var checkBtn = $("[data-scope='check']", modalEl);
        checkBtn.setAttribute("aria-disabled", checkedN ? "false" : "true");
        if (!checkedN && state.scope === "check") state.scope = "sel";
        setRadio($("#pt-print-scope"), "data-scope", state.scope);
        setRadio($("#pt-print-types"), "data-type", state.type);

        var g = groupEffective();
        $$("#pt-print-group button").forEach(function (b) {
            var on = b.getAttribute("data-group") === g;
            b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false");
            b.disabled = state.type === "aannemer" && !on;
        });
        $$("[data-inc]", modalEl).forEach(function (cb) { cb.checked = !!state.inc[cb.getAttribute("data-inc")]; });
        $("#pt-print-warn").hidden = !state.inc.internal;

        n = scopeRows().length;
        var T = TYPES[state.type];
        // Voorbeeld: aantal pagina's (grove schatting, de echte pdf bepaalt het) + planbijlagen
        var perPage = state.type === "lijst" ? (state.inc.photos ? 9 : 14) : state.type === "rondgang" ? (state.inc.photos ? 6 : 9) : 99;
        var pages = state.type === "aannemer"
            ? Math.max(1, new Set(scopeRows().map(function (tr) { return tr.dataset.contractor || "0"; })).size)
            : Math.max(1, Math.ceil(n / perPage));
        if (state.inc.history) pages += Math.ceil(n / 12);
        var plans = 0;
        if (state.inc.plans) plans = new Set(scopeRows().filter(function (tr) { return tr.dataset.plan === "1"; }).map(function (tr) { return tr.dataset.unit || "0"; })).size;
        var total = n ? pages + plans : 0;
        $("#pt-print-pages").textContent = plural(total, "pagina", "pagina's");
        $("#pt-print-format").textContent = T.fmt + (plans ? " + " + plural(plans, "plan", "plannen") : "");
        $("#pt-print-hint").textContent = T.hint;
        $("#pt-print-sum").textContent = n + " punten · " + T.name + (g !== "geen" && state.type !== "aannemer" ? " · per " + g : "");
        $("#pt-print-go").disabled = $("#pt-print-download").disabled = n === 0;
        renderStage(n, g);
    }

    function renderStage(n, g) {
        var stage = $("#pt-print-stage");
        var T = TYPES[state.type];
        var widths = ["78%", "64%", "86%", "58%", "72%", "80%", "62%"];
        var html = '<div class="gl-v2-pp-page ' + (T.landscape ? "is-landscape" : "is-portrait") + '"><span class="gl-v2-pp-band"></span><span class="gl-v2-pp-tt"></span><span class="gl-v2-pp-st"></span>';
        var w = 0, rows = Math.min(state.type === "lijst" ? 7 : 6, Math.max(n, 1));
        for (var r = 0; r < rows; r++) {
            if (g !== "geen" && (r === 0 || r === 3)) html += '<div class="gl-v2-pv-r is-group"><span class="gl-v2-pv-l" style="width:40%"></span></div>';
            html += '<div class="gl-v2-pv-r">' + (state.type === "rondgang" ? '<span class="gl-v2-pv-ck"></span>' : "") + (state.inc.photos ? '<span class="gl-v2-pv-ph"></span>' : "")
                + '<span class="gl-v2-pv-l" style="width:' + widths[w++ % widths.length] + (state.type === "rondgang" ? ";max-width:52%" : "") + '"></span>'
                + (state.type === "rondgang" ? '<span class="gl-v2-pv-nt"></span>' : "") + "</div>";
        }
        html += "</div>";
        if (state.inc.plans) html += '<div class="gl-v2-pp-page2"><span class="gl-v2-pp-band"></span><span class="gl-v2-pv-plan"><i style="left:30%;top:40%"></i><i style="left:62%;top:28%"></i><i class="is-key" style="left:48%;top:66%"></i></span></div>';
        stage.innerHTML = html;
    }

    // ── bediening ──
    modalEl.addEventListener("click", function (e) {
        var s = e.target.closest("[data-scope]");
        if (s && modalEl.contains(s)) { if (s.getAttribute("aria-disabled") !== "true") { state.scope = s.getAttribute("data-scope"); render(); } return; }
        var t = e.target.closest("[data-type]");
        if (t && modalEl.contains(t)) {
            state.type = t.getAttribute("data-type");
            if (state.type === "aannemer") state.group = "aannemer";
            else if (state.group === "aannemer" && t.getAttribute("data-type") !== "aannemer") state.group = "geen";
            savePrefs(); render(); return;
        }
        var g = e.target.closest("[data-group]");
        if (g && modalEl.contains(g)) { if (state.type !== "aannemer") { state.group = g.getAttribute("data-group"); savePrefs(); render(); } }
    });
    modalEl.addEventListener("change", function (e) {
        var cb = e.target.closest("[data-inc]");
        if (cb) { state.inc[cb.getAttribute("data-inc")] = cb.checked; savePrefs(); render(); return; }
        if (e.target.id === "pt-print-sort") { state.sort = e.target.value || "deadline"; savePrefs(); render(); }
    });

    function setSortUi() {
        var wrap = document.getElementById("pt-print-sort_select");
        var hidden = document.getElementById("pt-print-sort");
        if (!wrap || !hidden || !window.GlV2Select) return;
        var items = $$(".gl-v2-select-option", wrap).filter(function (o) { return o.getAttribute("data-value") !== ""; })
            .map(function (o) { return { value: o.getAttribute("data-value"), text: o.textContent.trim() }; });
        window.GlV2Select.setItems(wrap, items, state.sort);
    }

    function open() {
        loadPrefs();
        var status = list().statusFilter();
        // Zonder aangevinkte punten: wat de filter toont. Met aangevinkte punten: die, zoals de oude knop deed.
        state.scope = list().checked().length ? "check" : "sel";
        // Wie op "Afgesloten" (of alles) filtert wil die punten ook op papier.
        state.inc.closed = status === "5" || status === "all";
        var f = list().filtersText();
        $("#pt-print-sub").textContent = (modalEl.getAttribute("data-project-name") || "") + " · " + (f ? "filters nu: " + f : "geen filters actief");
        setSortUi();
        render();
        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }
    openBtn.addEventListener("click", open);

    // ── pdf ophalen ──
    function buildBody() {
        var fd = new FormData();
        var token = document.querySelector("#pt-print-form input[name='__RequestVerificationToken']");
        if (token) fd.append("__RequestVerificationToken", token.value);
        scopeRows().forEach(function (tr) { fd.append("IssueIds", tr.dataset.id); });
        fd.append("Type", state.type);
        fd.append("Group", groupEffective());
        fd.append("Sort", state.sort);
        fd.append("Photos", state.inc.photos ? "true" : "false");
        fd.append("Plans", state.inc.plans ? "true" : "false");
        fd.append("History", state.inc.history ? "true" : "false");
        fd.append("Closed", state.inc.closed ? "true" : "false");
        fd.append("Internal", state.inc.internal ? "true" : "false");
        return fd;
    }
    function fileNameFrom(res) {
        var cd = res.headers.get("Content-Disposition") || "";
        var m = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(cd);
        return m ? decodeURIComponent(m[1]) : "puntenlijst.pdf";
    }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body }); }
    function fetchPdf(btn) {
        btn.classList.add("is-busy");
        return fetch(modalEl.getAttribute("data-url"), { method: "POST", body: buildBody(), credentials: "same-origin" })
            .then(function (res) {
                if (!res.ok) throw new Error("status " + res.status);
                return res.blob().then(function (blob) { return { blob: blob, name: fileNameFrom(res) }; });
            })
            .catch(function () { toast("danger", "Afdrukken mislukt", "De pdf kon niet gemaakt worden. Probeer opnieuw."); return null; })
            .then(function (r) { btn.classList.remove("is-busy"); return r; });
    }
    $("#pt-print-download").addEventListener("click", function () {
        var btn = this;
        fetchPdf(btn).then(function (r) {
            if (!r) return;
            var a = document.createElement("a");
            a.href = URL.createObjectURL(r.blob); a.download = r.name;
            document.body.appendChild(a); a.click(); a.remove();
            setTimeout(function () { URL.revokeObjectURL(a.href); }, 10000);
            toast("success", "Pdf bewaard", r.name);
        });
    });
    $("#pt-print-go").addEventListener("click", function () {
        var btn = this;
        fetchPdf(btn).then(function (r) {
            if (!r) return;
            var url = URL.createObjectURL(r.blob);
            var frame = document.createElement("iframe");
            frame.style.cssText = "position:fixed;right:0;bottom:0;width:0;height:0;border:0;";
            frame.onload = function () {
                try { frame.contentWindow.focus(); frame.contentWindow.print(); }
                catch (e) { window.open(url, "_blank"); }
            };
            frame.src = url;
            document.body.appendChild(frame);
            setTimeout(function () { frame.remove(); URL.revokeObjectURL(url); }, 120000);
            window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            toast("success", "Afdrukvenster geopend", "Kies je printer, of bewaar als pdf.");
        });
    });
})();
