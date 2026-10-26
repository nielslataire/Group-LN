// gl-v2 — Instellingen › Kostprijzen materialen (design-handoff punt 52a/52b/52c): materialen, bouwkost % en formule koppelingen. De pagina krijgt de volledige
// stand als JSON en tekent de drie tabs zelf (zoeken, filteren, sorteren, bewerken per rij, slepen); elke wijziging gaat via fetch naar InstellingenController
// (…V2-acties), die de nieuwe stand teruggeeft. De keuzelijsten en het datumveld zijn de projectbrede componenten (GlV2/_SelectList, GlV2/_DateField): voor rijen
// die in JS getekend worden bouwen we dezelfde markup en bedraadt window.GlV2Select.init / window.GlV2DatePicker.init ze daarna.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-kp");
    if (!root) return;
    var canWrite = root.dataset.canWrite === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var U = {
        matSave: root.dataset.urlMatSave, matDel: root.dataset.urlMatDel, bkSave: root.dataset.urlBkSave, grpSave: root.dataset.urlGrpSave, grpDel: root.dataset.urlGrpDel,
        grpOrder: root.dataset.urlGrpOrder, itemSave: root.dataset.urlItemSave, itemDel: root.dataset.urlItemDel, kopSave: root.dataset.urlKopSave, kopNew: root.dataset.urlKopNew, kopDel: root.dataset.urlKopDel
    };
    var stand = JSON.parse(document.getElementById("kp-data").textContent);
    var ui = {
        tab: root.dataset.startTab, lot: 0, q: "", toep: "", stale: false, sort: "naam", dir: 1, editMat: 0,
        bk: {}, fkGroep: "alle", fkQ: ""
    };
    var STALE_MAANDEN = 24;
    var TOEP = { "": "Alle types", Woning: "Woning", Appartement: "Appartement", Gemeenschappelijk: "Gemeenschappelijk deel" };
    var EENHEDEN = ["m²", "m³", "lm", "stuk", "forfait"];

    function $(id) { return document.getElementById(id); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }
    function plural(n, een, meer) { return n + " " + (n === 1 ? een : meer); }
    function eur(v) { return "€ " + Number(v).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function pct(v) { return Number(v).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function parseNl(s) {
        s = String(s || "").replace(/\s| /g, "");
        if (s.indexOf(",") >= 0) s = s.replace(/\./g, "").replace(",", ".");
        var n = parseFloat(s); return isNaN(n) ? null : n;
    }
    function fmtDate(iso) { var p = String(iso).split("-"); return p.length === 3 ? p[2] + "/" + p[1] + "/" + p[0] : ""; }
    function post(url, d) {
        var fd = new FormData();
        Object.keys(d).forEach(function (k) { [].concat(d[k]).forEach(function (v) { if (v !== null && v !== undefined) fd.append(k, v); }); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function netwerkFout() { toast("danger", "Niet gelukt", "De server gaf geen antwoord. Probeer het opnieuw."); }
    // Verwerkt een antwoord { ok, bericht, stand }: stand overnemen, bij een fout een melding. true = gelukt.
    function afhandelen(r, okTitel, okBody) {
        if (r.stand) stand = r.stand;
        if (r.ok) { if (okTitel) toast("success", okTitel, okBody); return true; }
        toast("danger", "Niet gelukt", r.bericht || "");
        return false;
    }
    function bevestig(o) { return window.GlV2Bevestig ? window.GlV2Bevestig(o) : Promise.resolve(window.confirm(o.title)); }

    function groepById(id) { return stand.groepen.filter(function (g) { return g.id === id; })[0]; }
    function matById(id) { return stand.materialen.filter(function (m) { return m.id === id; })[0]; }
    function groepLabel(g) { return g ? g.lot + " · " + g.naam : "—"; }

    // ── Keuzelijst-markup (zelfde als GlV2/_SelectList, basisvariant) ─────────────────────────────
    function selHtml(o) {
        var items = o.items || [], cur = items.filter(function (i) { return String(i.value) === String(o.value == null ? "" : o.value); })[0];
        var emptyChosen = !cur && String(o.value == null ? "" : o.value) === "" && o.emptyText != null;
        var label = cur ? cur.text : emptyChosen ? o.emptyText : (o.placeholder || "Kies …");
        var opts = "";
        if (o.emptyText != null) opts += '<button type="button" class="gl-v2-select-option' + (emptyChosen ? " is-selected" : "") + '" role="option" data-value=""><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(o.emptyText) + "</span></button>";
        var lastGroup = null;
        items.forEach(function (i) {
            if (i.group && i.group !== lastGroup) {
                lastGroup = i.group;
                opts += '<div class="gl-v2-select-group-header"><span class="gl-v2-select-group-label">' + esc(String(i.group).toUpperCase()) + '</span><span class="gl-v2-select-group-rule"></span></div>';
            }
            opts += '<button type="button" class="gl-v2-select-option' + (cur && cur === i ? " is-selected" : "") + '" role="option" data-value="' + esc(i.value) + '"><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(i.text) + "</span></button>";
        });
        return '<div class="gl-v2-field gl-v2-field-bare"><div class="gl-v2-select" data-gl-v2-select data-placeholder="' + esc(o.placeholder || "Kies …") + '"><input type="hidden" name="' + esc(o.name) + '" value="' + esc(o.value == null ? "" : o.value) + '" />' +
            '<div class="gl-v2-select-trigger' + (cur || emptyChosen ? " is-filled" : "") + '" role="button" tabindex="0" aria-haspopup="listbox" aria-expanded="false" aria-label="' + esc(o.label || o.name) + '"><span class="gl-v2-select-trigger-label">' + esc(label) + '</span><i class="ph ph-caret-down gl-v2-select-trigger-caret" aria-hidden="true"></i></div>' +
            '<div class="gl-v2-select-panel" role="listbox">' + opts + "</div></div></div>";
    }
    function dateHtml(name, iso, label) {
        return '<div class="gl-v2-field gl-v2-field-bare"><div class="gl-v2-select gl-v2-datepicker" data-gl-v2-datepicker><div class="gl-v2-field-box"><i class="ph ph-calendar" aria-hidden="true"></i>' +
            '<input type="text" class="gl-v2-field-input" data-role="date-text" value="' + esc(fmtDate(iso)) + '" placeholder="dd/mm/jjjj" autocomplete="off" inputmode="numeric" maxlength="10" aria-label="' + esc(label) + '" /></div>' +
            '<input type="hidden" name="' + esc(name) + '" data-role="date-value" value="' + esc(iso) + '" />' +
            '<div class="gl-v2-select-panel gl-v2-datepicker-panel" data-role="panel"><div class="gl-v2-datepicker-nav-row"><button type="button" tabindex="-1" class="gl-v2-datepicker-nav" data-role="prev" aria-label="Vorige maand"><i class="ph ph-caret-left" aria-hidden="true"></i></button><span class="gl-v2-datepicker-month" data-role="month-label"></span><button type="button" tabindex="-1" class="gl-v2-datepicker-nav" data-role="next" aria-label="Volgende maand"><i class="ph ph-caret-right" aria-hidden="true"></i></button></div>' +
            '<div class="gl-v2-datepicker-weekdays"><span>MA</span><span>DI</span><span>WO</span><span>DO</span><span>VR</span><span>ZA</span><span>ZO</span></div><div class="gl-v2-datepicker-days" data-role="days"></div>' +
            '<div class="gl-v2-datepicker-footer"><button type="button" tabindex="-1" class="gl-v2-datepicker-footer-link" data-role="today">Vandaag</button><button type="button" tabindex="-1" class="gl-v2-datepicker-footer-link is-muted" data-role="clear">Wissen</button></div></div></div></div>';
    }
    function wire(scope) {
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(scope);
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
    }
    function setSelect(wrapId, items, value, emptyText) { window.GlV2Select.setItems($(wrapId), items, value == null ? "" : value, emptyText == null ? null : emptyText); }
    var groepItems = function () { return stand.groepen.map(function (g) { return { value: g.id, text: groepLabel(g) }; }); };
    var indexItems = function () { return stand.indexTypes.map(function (i) { return { value: i.id, text: i.code + " · " + i.naam }; }); };
    var eenheidItems = function () { return EENHEDEN.map(function (e) { return { value: e, text: e }; }); };
    var toepItems = function () { return ["Woning", "Appartement", "Gemeenschappelijk"].map(function (t) { return { value: t, text: TOEP[t] }; }); };

    // ── Tabs ──────────────────────────────────────────────────────────────────────────────────────
    function showTab(key) {
        ui.tab = key;
        root.querySelectorAll("[data-kp-tab]").forEach(function (b) { var on = b.dataset.kpTab === key; b.classList.toggle("is-active", on); b.setAttribute("aria-selected", on ? "true" : "false"); });
        root.querySelectorAll("[data-kp-panel]").forEach(function (p) { p.hidden = p.dataset.kpPanel !== key; });
        var add = $("kp-add-open"); if (add) add.hidden = key !== "materialen";
        try { history.replaceState(null, "", "?tab=" + key); } catch (e) { }
    }
    root.querySelectorAll("[data-kp-tab]").forEach(function (b) { b.addEventListener("click", function () { showTab(b.dataset.kpTab); }); });

    // ═══ 52a Materialen ═══════════════════════════════════════════════════════════════════════════
    function isStale(m) {
        var d = new Date(m.datum + "T00:00:00"), grens = new Date(); grens.setMonth(grens.getMonth() - STALE_MAANDEN);
        return d < grens;
    }
    function jaren(m) { return Math.max(1, Math.floor((Date.now() - new Date(m.datum + "T00:00:00").getTime()) / 31557600000)); }

    function gefilterd() {
        var q = norm(ui.q);
        return stand.materialen.filter(function (m) {
            if (ui.lot && m.categorieId !== ui.lot) return false;
            if (ui.toep && m.toepassing !== ui.toep) return false;
            if (ui.stale && !isStale(m)) return false;
            return !q || norm(m.naam).indexOf(q) >= 0;
        });
    }
    function sortFn(a, b) {
        var x, y;
        if (ui.sort === "prijs") { x = a.prijs; y = b.prijs; } else if (ui.sort === "datum") { x = a.datum; y = b.datum; } else { x = norm(a.naam); y = norm(b.naam); }
        return (x < y ? -1 : x > y ? 1 : 0) * ui.dir;
    }

    function renderLots() {
        var html = '<button type="button" class="gl-v2-kp-lot' + (ui.lot === 0 ? " is-active" : "") + '" data-lot="0"><span class="n">Alle groepen</span><span class="c">' + stand.materialen.length + "</span></button>";
        stand.groepen.forEach(function (g) {
            var c = stand.materialen.filter(function (m) { return m.categorieId === g.id; }).length;
            html += '<button type="button" class="gl-v2-kp-lot' + (ui.lot === g.id ? " is-active" : "") + '" data-lot="' + g.id + '"><span class="l">' + g.lot + '</span><span class="n">' + esc(g.naam) + '</span><span class="c">' + c + "</span></button>";
        });
        $("kp-lots").innerHTML = html;
        setSelect("kp-lot-select_select", groepItems(), ui.lot || "", "Alle activiteitgroepen");
    }

    function renderMat() {
        var list = gefilterd();
        var groepen = stand.groepen.filter(function (g) { return list.some(function (m) { return m.categorieId === g.id; }); });
        var html = "";
        groepen.forEach(function (g) {
            var rows = list.filter(function (m) { return m.categorieId === g.id; }).sort(sortFn);
            html += '<div class="gl-v2-kp-group"><span class="l">' + g.lot + '</span><span class="n">' + esc(g.naam) + '</span><span class="c">' + plural(rows.length, "materiaal", "materialen") + "</span></div>";
            rows.forEach(function (m) { html += matRow(m); });
        });
        $("kp-mat-rows").innerHTML = html;
        wire($("kp-mat-rows"));

        $("kp-mat-empty").hidden = list.length > 0;
        if (!list.length) {
            var f = ui.lot || ui.q || ui.toep || ui.stale;
            $("kp-mat-empty-t").textContent = f ? "Geen materialen voor dit filter" : "Nog geen materialen";
            $("kp-mat-reset").hidden = !f;
        }
        var oud = stand.materialen.filter(isStale);
        $("kp-stale").hidden = oud.length === 0;
        if (oud.length) {
            var oudste = oud.map(function (m) { return m.datum; }).sort()[0];
            $("kp-stale-text").textContent = plural(oud.length, "referentieprijs is", "referentieprijzen zijn") + " ouder dan " + STALE_MAANDEN + " maanden (oudste: " + fmtDate(oudste) + "). Budgetten rekenen ze via de index op, maar een recente prijs is betrouwbaarder.";
            $("kp-stale-toggle").textContent = ui.stale ? "Toon alle" : "Toon enkel verouderde";
        }
        var g0 = ui.lot ? groepById(ui.lot) : null;
        $("kp-mat-info").textContent = (g0 ? "lot " + g0.lot + " · " + g0.naam + " · " : "") + plural(list.length, "materiaal", "materialen") + (list.length !== stand.materialen.length ? " van " + stand.materialen.length : "");
        $("kp-mat-foot").textContent = list.length ? plural(list.length, "materiaal", "materialen") + " getoond" : "";
        $("kp-count-mat").textContent = stand.materialen.length;
        document.querySelectorAll(".gl-v2-kp-sort").forEach(function (b) {
            var on = b.dataset.sort === ui.sort; b.classList.toggle("is-active", on);
            b.querySelector("i").className = "ph" + (on ? (ui.dir === 1 ? " ph-arrow-up" : " ph-arrow-down") : "");
        });
    }

    function matRow(m) {
        var idx = stand.indexTypes.filter(function (i) { return i.id === m.indexTypeId; })[0];
        if (canWrite && ui.editMat === m.id) {
            return '<form class="gl-v2-kp-row gl-v2-kp-grid--mat is-edit" data-edit-mat="' + m.id + '" autocomplete="off">' +
                '<div class="gl-v2-kp-stack-cell"><div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" name="naam" class="gl-v2-field-input" value="' + esc(m.naam) + '" maxlength="200" aria-label="Naam" /></div></div>' +
                selHtml({ name: "categorieId", items: groepItems(), value: m.categorieId, label: "Activiteitgroep", placeholder: "Kies een groep" }) + "</div>" +
                selHtml({ name: "eenheid", items: eenheidItems(), value: m.eenheid, label: "Eenheid" }) +
                selHtml({ name: "toepassing", items: toepItems(), value: m.toepassing, label: "Toepassing", emptyText: "Alle types" }) +
                '<div class="gl-v2-field"><div class="gl-v2-field-box"><span class="gl-v2-field-prefix">€</span><input type="text" name="prijs" inputmode="decimal" class="gl-v2-field-input is-tabular is-right" value="' + esc(pct(m.prijs)) + '" aria-label="Referentieprijs" /></div></div>' +
                dateHtml("datum", m.datum, "Referentiedatum") +
                selHtml({ name: "indexTypeId", items: indexItems(), value: m.indexTypeId, label: "Index", placeholder: "Kies een index" }) +
                '<div class="gl-v2-kp-actions"><button type="submit" class="gl-v2-row-action gl-v2-row-action-primary" title="Opslaan" aria-label="Opslaan"><i class="ph ph-check" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-row-action" data-cancel-mat title="Annuleren" aria-label="Annuleren"><i class="ph ph-x" aria-hidden="true"></i></button></div></form>';
        }
        var old = isStale(m);
        return '<div class="gl-v2-kp-row gl-v2-kp-grid--mat">' +
            '<div class="gl-v2-kp-name"><span class="t" title="' + esc(m.naam) + '">' + esc(m.naam) + "</span>" + (m.gekoppeld ? "" : '<span class="s">niet in formules</span>') + "</div>" +
            '<span class="gl-v2-kp-cell">' + esc(m.eenheid) + "</span>" +
            '<span class="gl-v2-kp-cell ' + (m.toepassing ? "" : "is-muted") + '">' + esc(TOEP[m.toepassing] || m.toepassing) + "</span>" +
            '<span class="gl-v2-kp-num">' + eur(m.prijs) + "</span>" +
            '<span class="gl-v2-kp-cell">' + fmtDate(m.datum) + (old ? ' <span class="gl-v2-kp-old" title="Ouder dan ' + STALE_MAANDEN + ' maanden">' + jaren(m) + " jr</span>" : "") + "</span>" +
            '<span><span class="gl-v2-badge is-neutral" title="' + esc(idx ? idx.naam : "") + '">' + esc(idx ? idx.code : m.indexCode || "—") + "</span></span>" +
            '<div class="gl-v2-kp-actions">' + (canWrite ?
                '<button type="button" class="gl-v2-row-action gl-v2-row-action-primary" data-edit-mat-btn="' + m.id + '" title="Bewerken" aria-label="Bewerk ' + esc(m.naam) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-mat="' + m.id + '"' + (m.gekoppeld ? " disabled" : "") + ' title="' + (m.gekoppeld ? "Dit materiaal hangt aan een formule-slot. Verwijder eerst de koppeling." : "Verwijderen") + '" aria-label="Verwijder ' + esc(m.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div></div>";
    }

    function setLot(id) { ui.lot = id; ui.editMat = 0; renderLots(); renderMat(); }
    $("kp-lots").addEventListener("click", function (e) { var b = e.target.closest("[data-lot]"); if (b) setLot(parseInt(b.dataset.lot, 10)); });
    $("kp-lot-select").addEventListener("change", function () { setLot(parseInt(this.value, 10) || 0); });
    $("kp-toep-filter").addEventListener("change", function () { ui.toep = this.value; ui.editMat = 0; renderMat(); });
    $("kp-search").addEventListener("input", function () { ui.q = this.value; renderMat(); });
    $("kp-search-clear").addEventListener("click", function () { $("kp-search").value = ""; ui.q = ""; renderMat(); $("kp-search").focus(); });
    $("kp-stale-toggle").addEventListener("click", function () { ui.stale = !ui.stale; renderMat(); });
    $("kp-mat-reset").addEventListener("click", function () {
        ui.lot = 0; ui.q = ""; ui.toep = ""; ui.stale = false; $("kp-search").value = "";
        setSelect("kp-toep-filter_select", toepItems(), "", "Alle types"); renderLots(); renderMat();
    });
    document.querySelectorAll(".gl-v2-kp-sort").forEach(function (b) {
        b.addEventListener("click", function () { var k = b.dataset.sort; if (ui.sort === k) ui.dir = -ui.dir; else { ui.sort = k; ui.dir = 1; } renderMat(); });
    });

    $("kp-mat-rows").addEventListener("click", function (e) {
        var ed = e.target.closest("[data-edit-mat-btn]");
        if (ed) { ui.editMat = parseInt(ed.dataset.editMatBtn, 10); renderMat(); var i = $("kp-mat-rows").querySelector("form input[name=naam]"); if (i) { i.focus(); i.select(); } return; }
        if (e.target.closest("[data-cancel-mat]")) { ui.editMat = 0; renderMat(); return; }
        var del = e.target.closest("[data-del-mat]");
        if (del && !del.disabled) {
            var m = matById(parseInt(del.dataset.delMat, 10));
            bevestig({ title: (m ? m.naam : "Materiaal") + " verwijderen?", desc: "Het materiaal verdwijnt uit de lijst. Budgetten die het al gebruikten, behouden hun prijs.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                if (!ok) return;
                post(U.matDel, { id: m.id }).then(function (r) { afhandelen(r, "Materiaal verwijderd"); renderAll(); }).catch(netwerkFout);
            });
        }
    });
    $("kp-mat-rows").addEventListener("keydown", function (e) { if (e.key === "Escape" && ui.editMat) { ui.editMat = 0; renderMat(); } });
    $("kp-mat-rows").addEventListener("submit", function (e) {
        e.preventDefault();
        var f = e.target, el = f.elements;
        if (!el.categorieId.value) { toast("warning", "Groep ontbreekt", "Kies een activiteitgroep."); return; }
        if (!el.indexTypeId.value) { toast("warning", "Index ontbreekt", "Kies een index."); return; }
        if (parseNl(el.prijs.value) === null) { toast("warning", "Prijs niet geldig", "Geef een bedrag in, bv. 12,50."); el.prijs.focus(); return; }
        post(U.matSave, { id: f.dataset.editMat, categorieId: el.categorieId.value, naam: el.naam.value, eenheid: el.eenheid.value, toepassing: el.toepassing.value, indexTypeId: el.indexTypeId.value, prijs: el.prijs.value, datum: el.datum.value })
            .then(function (r) { if (afhandelen(r, "Materiaal opgeslagen")) ui.editMat = 0; renderAll(); }).catch(netwerkFout);
    });

    // Materiaal toevoegen (modal)
    if (canWrite) {
        var addEl = $("kp-add-modal"), addForm = addEl.querySelector("form");
        $("kp-add-open").addEventListener("click", function () {
            addForm.reset(); $("kp-add-err").hidden = true;
            var today = new Date(), iso = today.getFullYear() + "-" + String(today.getMonth() + 1).padStart(2, "0") + "-" + String(today.getDate()).padStart(2, "0");
            var dv = addForm.querySelector('[data-role="date-value"]'), dt = addForm.querySelector('[data-role="date-text"]'); if (dv) dv.value = iso; if (dt) dt.value = fmtDate(iso);
            setSelect("kp-add-groep_select", groepItems(), ui.lot || "", null);
            setSelect("kp-add-eenheid_select", eenheidItems(), "m²", null);
            setSelect("kp-add-toep_select", toepItems(), "", "Alle types");
            setSelect("kp-add-index_select", indexItems(), "", null);
            window.bootstrap.Modal.getOrCreateInstance(addEl).show();
            addEl.addEventListener("shown.bs.modal", function once() { addEl.removeEventListener("shown.bs.modal", once); $("kp-add-naam").focus(); });
        });
        addForm.addEventListener("submit", function (e) {
            e.preventDefault();
            var el = addForm.elements;
            function fout(m, focus) { $("kp-add-err").textContent = m; $("kp-add-err").hidden = false; if (focus) focus.focus(); }
            if (!el.naam.value.trim()) return fout("Geef een naam in.", el.naam);
            if (!el.categorieId.value) return fout("Kies een activiteitgroep.");
            if (!el.indexTypeId.value) return fout("Kies een index.");
            if (parseNl(el.prijs.value) === null) return fout("Geef een geldige referentieprijs in, bv. 12,50.", el.prijs);
            if (!el.datum.value) return fout("Geef de referentiedatum in (dd/mm/jjjj).");
            $("kp-add-submit").disabled = true;
            post(U.matSave, { id: 0, categorieId: el.categorieId.value, naam: el.naam.value, eenheid: el.eenheid.value, toepassing: el.toepassing.value, indexTypeId: el.indexTypeId.value, prijs: el.prijs.value, datum: el.datum.value })
                .then(function (r) {
                    $("kp-add-submit").disabled = false;
                    if (r.ok) { stand = r.stand; window.bootstrap.Modal.getOrCreateInstance(addEl).hide(); toast("success", "Materiaal toegevoegd", el.naam.value.trim()); renderAll(); }
                    else fout(r.bericht || "Toevoegen is niet gelukt.");
                }).catch(function () { $("kp-add-submit").disabled = false; fout("De server gaf geen antwoord."); });
        });
    }

    // ═══ 52b Bouwkost % ═══════════════════════════════════════════════════════════════════════════
    var dragId = null;
    function pctRow(p, systeem) {
        var v = ui.bk[p.id] != null ? ui.bk[p.id] : pct(p.percentage);
        var dirty = ui.bk[p.id] != null && parseNl(ui.bk[p.id]) !== p.percentage;
        return '<div class="gl-v2-kp-row ' + (systeem ? "gl-v2-kp-grid--bk" : "gl-v2-kp-grid--bki") + (dirty ? " is-dirty" : "") + '">' +
            '<span class="gl-v2-kp-name"><span class="t">' + esc(p.naam) + '</span></span>' +
            '<div class="gl-v2-field gl-v2-kp-pct"><div class="gl-v2-field-box"><input type="text" inputmode="decimal" class="gl-v2-field-input is-tabular is-right" data-pct="' + p.id + '" value="' + esc(v) + '"' + (canWrite ? "" : " disabled") + ' aria-label="Percentage ' + esc(p.naam) + '" /><span class="gl-v2-field-unit">%</span></div></div>' +
            (systeem ? "" : '<div class="gl-v2-kp-actions">' + (canWrite ?
                '<button type="button" class="gl-v2-row-action gl-v2-row-action-primary" data-edit-item="' + p.id + '" title="Bewerken" aria-label="Bewerk ' + esc(p.naam) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-item="' + p.id + '" title="Verwijderen" aria-label="Verwijder ' + esc(p.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div>") + "</div>";
    }
    function renderBk() {
        var sys = stand.percentages.filter(function (p) { return p.systeem; }).sort(function (a, b) { return a.volgorde - b.volgorde; });
        $("bk-system").innerHTML = sys.length ? sys.map(function (p) { return pctRow(p, true); }).join("") : '<div class="gl-v2-kp-empty-line">Geen vaste parameters.</div>';
        var groepen = stand.pctGroepen.slice().sort(function (a, b) { return a.volgorde - b.volgorde; });
        $("bk-groups").innerHTML = groepen.map(function (g) {
            var items = stand.percentages.filter(function (p) { return !p.systeem && p.groepId === g.id; }).sort(function (a, b) { return a.volgorde - b.volgorde || a.naam.localeCompare(b.naam, "nl"); });
            var sum = items.reduce(function (s, p) { return s + (parseNl(ui.bk[p.id]) != null && ui.bk[p.id] != null ? parseNl(ui.bk[p.id]) : p.percentage); }, 0);
            var heeftAlles = stand.percentages.some(function (p) { return p.groepId === g.id; });
            return '<div class="gl-v2-kp-pgroup" data-grp="' + g.id + '"' + (canWrite ? ' draggable="true"' : "") + '>' +
                '<div class="gl-v2-kp-pgroup-head">' + (canWrite ? '<span class="gl-v2-kp-grip" aria-hidden="true" title="Sleep om de volgorde te wijzigen"><i class="ph ph-dots-six-vertical"></i></span>' : "") +
                '<span class="t">' + esc(g.naam) + '</span><span class="c">' + plural(items.length, "percentage", "percentages") + '</span><span class="sum">' + pct(sum) + ' %</span><span class="sp"></span>' +
                (canWrite ? '<button type="button" class="gl-v2-btn gl-v2-btn-text" data-add-item="' + g.id + '"><i class="ph ph-plus" aria-hidden="true"></i>Percentage</button>' +
                    '<button type="button" class="gl-v2-row-action gl-v2-row-action-primary" data-edit-grp="' + g.id + '" title="Groep hernoemen" aria-label="Hernoem ' + esc(g.naam) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
                    '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-grp="' + g.id + '"' + (heeftAlles ? " disabled" : "") + ' title="' + (heeftAlles ? "Een groep met percentages verwijder je pas als ze leeg is" : "Verwijderen") + '" aria-label="Verwijder ' + esc(g.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div>" +
                (items.length ? items.map(function (p) { return pctRow(p, false); }).join("") : '<div class="gl-v2-kp-empty-line">Nog geen percentages in deze groep.</div>') + "</div>";
        }).join("") || '<div class="gl-v2-kp-empty-line">Nog geen groepen.</div>';
        $("kp-count-bk").textContent = stand.percentages.length;
        updateBar();
    }
    function dirtyList() {
        return stand.percentages.filter(function (p) { return ui.bk[p.id] != null && parseNl(ui.bk[p.id]) !== null && parseNl(ui.bk[p.id]) !== p.percentage; });
    }
    function updateBar() {
        var n = dirtyList().length, bar = $("bk-bar");
        bar.classList.toggle("is-dirty", n > 0);
        $("bk-bar-text").textContent = n ? plural(n, "niet-opgeslagen wijziging", "niet-opgeslagen wijzigingen") : "Alles opgeslagen";
        if ($("bk-save")) { $("bk-save").disabled = !n; $("bk-reset").disabled = !n; }
    }
    root.querySelector('[data-kp-panel="bouwkost"]').addEventListener("input", function (e) {
        var i = e.target.closest("[data-pct]"); if (!i) return;
        ui.bk[i.dataset.pct] = i.value;
        var row = i.closest(".gl-v2-kp-row"), p = stand.percentages.filter(function (x) { return String(x.id) === i.dataset.pct; })[0];
        row.classList.toggle("is-dirty", parseNl(i.value) !== null && parseNl(i.value) !== p.percentage);
        updateBar();
        // somtotalen van de groep live bijwerken
        root.querySelectorAll(".gl-v2-kp-pgroup").forEach(function (gEl) {
            var gid = parseInt(gEl.dataset.grp, 10);
            var s = stand.percentages.filter(function (x) { return !x.systeem && x.groepId === gid; }).reduce(function (t, x) { var v = ui.bk[x.id] != null ? parseNl(ui.bk[x.id]) : x.percentage; return t + (v == null ? 0 : v); }, 0);
            gEl.querySelector(".sum").textContent = pct(s) + " %";
        });
    });
    root.querySelector('[data-kp-panel="bouwkost"]').addEventListener("focusout", function (e) {
        var i = e.target.closest("[data-pct]"); if (!i) return;
        var v = parseNl(i.value); if (v !== null) i.value = pct(v);
    });
    if (canWrite) {
        $("bk-reset").addEventListener("click", function () { ui.bk = {}; renderBk(); });
        $("bk-save").addEventListener("click", function () {
            var wijz = dirtyList().map(function (p) { return { id: p.id, percentage: ui.bk[p.id] }; });
            var btn = this; btn.disabled = true;
            post(U.bkSave, { wijzigingen: JSON.stringify(wijz) }).then(function (r) {
                if (afhandelen(r, "Percentages opgeslagen", plural(wijz.length, "wijziging", "wijzigingen"))) ui.bk = {};
                renderAll();
            }).catch(function () { netwerkFout(); updateBar(); });
        });
        window.addEventListener("beforeunload", function (e) { if (dirtyList().length) { e.preventDefault(); e.returnValue = ""; } });

        // Groep/percentage-modal
        var bkEl = $("bk-modal"), bkForm = bkEl.querySelector("form"), bkState = null;
        function openBk(o) {
            bkState = o; $("bk-modal-err").hidden = true;
            $("bk-modal-title").textContent = o.kind === "grp" ? (o.id ? "Groep hernoemen" : "Groep toevoegen") : (o.id ? "Percentage bewerken" : "Percentage toevoegen");
            $("bk-modal-item").hidden = o.kind === "grp";
            $("bk-modal-naam").value = o.naam || ""; $("bk-modal-pct").value = o.pct != null ? pct(o.pct) : "";
            if (o.kind === "item") setSelect("bk-modal-groep_select", stand.pctGroepen.map(function (g) { return { value: g.id, text: g.naam }; }), o.groepId || "", null);
            window.bootstrap.Modal.getOrCreateInstance(bkEl).show();
            bkEl.addEventListener("shown.bs.modal", function once() { bkEl.removeEventListener("shown.bs.modal", once); $("bk-modal-naam").focus(); $("bk-modal-naam").select(); });
        }
        $("bk-grp-add").addEventListener("click", function () { openBk({ kind: "grp", id: 0 }); });
        $("bk-groups").addEventListener("click", function (e) {
            var a = e.target.closest("[data-add-item]");
            if (a) { openBk({ kind: "item", id: 0, groepId: parseInt(a.dataset.addItem, 10) }); return; }
            var g = e.target.closest("[data-edit-grp]");
            if (g) { var gr = stand.pctGroepen.filter(function (x) { return x.id === parseInt(g.dataset.editGrp, 10); })[0]; openBk({ kind: "grp", id: gr.id, naam: gr.naam }); return; }
            var it = e.target.closest("[data-edit-item]");
            if (it) { var p = stand.percentages.filter(function (x) { return x.id === parseInt(it.dataset.editItem, 10); })[0]; openBk({ kind: "item", id: p.id, naam: p.naam, groepId: p.groepId, pct: p.percentage }); return; }
            var dg = e.target.closest("[data-del-grp]");
            if (dg && !dg.disabled) {
                var gg = stand.pctGroepen.filter(function (x) { return x.id === parseInt(dg.dataset.delGrp, 10); })[0];
                bevestig({ title: "Groep " + gg.naam + " verwijderen?", desc: "De groep is leeg, dus er gaan geen percentages verloren.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                    if (ok) post(U.grpDel, { id: gg.id }).then(function (r) { afhandelen(r, "Groep verwijderd"); renderAll(); }).catch(netwerkFout);
                });
                return;
            }
            var di = e.target.closest("[data-del-item]");
            if (di) {
                var pp = stand.percentages.filter(function (x) { return x.id === parseInt(di.dataset.delItem, 10); })[0];
                bevestig({ title: pp.naam + " verwijderen?", desc: "Het percentage verdwijnt uit de bijkomende percentages. Bestaande budgetten behouden hun waarde.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                    if (ok) post(U.itemDel, { id: pp.id }).then(function (r) { afhandelen(r, "Percentage verwijderd"); renderAll(); }).catch(netwerkFout);
                });
            }
        });
        bkForm.addEventListener("submit", function (e) {
            e.preventDefault();
            function fout(m) { $("bk-modal-err").textContent = m; $("bk-modal-err").hidden = false; }
            var naam = $("bk-modal-naam").value.trim(); if (!naam) return fout("Geef een naam in.");
            var req;
            if (bkState.kind === "grp") req = post(U.grpSave, { id: bkState.id, naam: naam });
            else {
                var gid = bkForm.elements.groepId.value; if (!gid) return fout("Kies een groep.");
                if (parseNl($("bk-modal-pct").value) === null) return fout("Geef een percentage in, bv. 2,50.");
                req = post(U.itemSave, { id: bkState.id, groepId: gid, naam: naam, percentage: $("bk-modal-pct").value });
            }
            $("bk-modal-submit").disabled = true;
            req.then(function (r) {
                $("bk-modal-submit").disabled = false;
                if (r.ok) { stand = r.stand; window.bootstrap.Modal.getOrCreateInstance(bkEl).hide(); toast("success", bkState.kind === "grp" ? "Groep opgeslagen" : "Percentage opgeslagen", naam); renderAll(); }
                else fout(r.bericht || "Opslaan is niet gelukt.");
            }).catch(function () { $("bk-modal-submit").disabled = false; fout("De server gaf geen antwoord."); });
        });

        // Groepen slepen
        var groupsEl = $("bk-groups");
        groupsEl.addEventListener("dragstart", function (e) {
            var g = e.target.closest(".gl-v2-kp-pgroup"); if (!g || e.target.closest("input")) { e.preventDefault(); return; }
            dragId = g.dataset.grp; g.classList.add("is-dragging"); e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", dragId); } catch (x) { }
        });
        groupsEl.addEventListener("dragover", function (e) {
            if (!dragId) return;
            e.preventDefault();
            var over = e.target.closest(".gl-v2-kp-pgroup"), drag = groupsEl.querySelector('[data-grp="' + dragId + '"]');
            if (!over || over === drag) return;
            var r = over.getBoundingClientRect(), after = e.clientY > r.top + r.height / 2;
            groupsEl.insertBefore(drag, after ? over.nextSibling : over);
        });
        groupsEl.addEventListener("dragend", function () {
            if (!dragId) return;
            var ids = Array.prototype.map.call(groupsEl.querySelectorAll(".gl-v2-kp-pgroup"), function (g) { return parseInt(g.dataset.grp, 10); });
            groupsEl.querySelectorAll(".is-dragging").forEach(function (g) { g.classList.remove("is-dragging"); });
            dragId = null;
            post(U.grpOrder, { ids: ids }).then(function (r) { afhandelen(r, "Volgorde bewaard"); renderBk(); }).catch(function () { netwerkFout(); renderBk(); });
        });
    }

    // ═══ 52c Formule koppelingen ══════════════════════════════════════════════════════════════════
    function matItems() {
        return stand.materialen.slice().sort(function (a, b) {
            var ga = groepById(a.categorieId), gb = groepById(b.categorieId);
            return ((ga ? ga.lot : 0) - (gb ? gb.lot : 0)) || a.naam.localeCompare(b.naam, "nl");
        }).map(function (m) { var g = groepById(m.categorieId); return { value: m.id, text: m.naam, group: g ? g.lot + " · " + g.naam : "Overige" }; });
    }
    function fkGroepVan(k) { var m = k.materiaalId ? matById(k.materiaalId) : null; return m ? m.categorieId : 0; }
    function fkFiltered() {
        var q = norm(ui.fkQ);
        return stand.koppelingen.filter(function (k) {
            if (ui.fkGroep === "ongekoppeld" && k.materiaalId) return false;
            if (ui.fkGroep !== "alle" && ui.fkGroep !== "ongekoppeld" && String(fkGroepVan(k)) !== ui.fkGroep) return false;
            if (!q) return true;
            var m = k.materiaalId ? matById(k.materiaalId) : null;
            return norm(k.sleutel).indexOf(q) >= 0 || norm(k.omschrijving).indexOf(q) >= 0 || (m && norm(m.naam).indexOf(q) >= 0);
        }).sort(function (a, b) { return a.sleutel.localeCompare(b.sleutel); });
    }
    function slugOf(naam) {
        var s = String(naam || "").trim().toLowerCase().replace(/[^a-z0-9]+/g, "_").replace(/^_+|_+$/g, "");
        if (!s) return "";
        return /^[a-z]/.test(s) ? s : "m_" + s;
    }
    function renderFk() {
        var ongekoppeld = stand.koppelingen.filter(function (k) { return !k.materiaalId; }).length;
        var perGroep = {};
        stand.koppelingen.forEach(function (k) { var g = fkGroepVan(k); if (g) perGroep[g] = (perGroep[g] || 0) + 1; });
        var html = '<button type="button" class="gl-v2-kp-lot' + (ui.fkGroep === "alle" ? " is-active" : "") + '" data-fkg="alle"><span class="n">Alle slots</span><span class="c">' + stand.koppelingen.length + "</span></button>" +
            '<button type="button" class="gl-v2-kp-lot' + (ui.fkGroep === "ongekoppeld" ? " is-active" : "") + '" data-fkg="ongekoppeld"><span class="n">Niet gekoppeld</span><span class="c">' + ongekoppeld + "</span></button>";
        stand.groepen.filter(function (g) { return perGroep[g.id]; }).forEach(function (g) {
            html += '<button type="button" class="gl-v2-kp-lot' + (ui.fkGroep === String(g.id) ? " is-active" : "") + '" data-fkg="' + g.id + '"><span class="l">' + g.lot + '</span><span class="n">' + esc(g.naam) + '</span><span class="c">' + perGroep[g.id] + "</span></button>";
        });
        $("fk-groups").innerHTML = html;

        var list = fkFiltered(), items = matItems();
        $("fk-rows").innerHTML = list.map(function (k) {
            var m = k.materiaalId ? matById(k.materiaalId) : null;
            return '<div class="gl-v2-kp-row gl-v2-kp-grid--fk' + (m ? "" : " is-open") + '" data-slot="' + esc(k.sleutel) + '">' +
                '<span class="gl-v2-kp-mono">@mat_' + esc(k.sleutel) + "</span><span class=\"gl-v2-kp-name\"><span class=\"t\">" + esc(k.omschrijving) + "</span></span>" +
                (canWrite ? selHtml({ name: "mat", items: items, value: k.materiaalId || "", label: "Gekoppeld materiaal voor " + k.omschrijving, emptyText: "— kies een materiaal —" }) : '<span class="gl-v2-kp-cell">' + esc(m ? m.naam : "— niet gekoppeld —") + "</span>") +
                '<span class="gl-v2-kp-num">' + (m ? eur(m.prijs) + ' <small>/' + esc(m.eenheid) + "</small>" : "—") + "</span>" +
                '<div class="gl-v2-kp-actions">' + (canWrite ? '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-kop="' + k.id + '" title="Koppeling verwijderen" aria-label="Verwijder ' + esc(k.omschrijving) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div></div>";
        }).join("");
        wire($("fk-rows"));
        $("fk-empty").hidden = list.length > 0;
        if (!list.length) $("fk-empty-t").textContent = stand.koppelingen.length ? "Geen koppelingen voor dit filter" : "Nog geen formule-slots";
        $("fk-foot").textContent = plural(stand.koppelingen.length, "slot", "slots") + " · " + ongekoppeld + " niet gekoppeld";
        $("kp-count-fk").textContent = stand.koppelingen.length;
        if (canWrite) {
            var cur = $("fk-add-mat").querySelector("input[type=hidden]");
            $("fk-add-mat").innerHTML = selHtml({ name: "mat", items: items, value: cur ? cur.value : "", label: "Materiaal voor het nieuwe slot", emptyText: "— kies een materiaal —" });
            wire($("fk-add-mat"));
        }
    }
    $("fk-groups").addEventListener("click", function (e) { var b = e.target.closest("[data-fkg]"); if (b) { ui.fkGroep = b.dataset.fkg; renderFk(); } });
    $("fk-search").addEventListener("input", function () { ui.fkQ = this.value; renderFk(); });
    if (canWrite) {
        $("fk-rows").addEventListener("change", function (e) {
            if (e.target.name !== "mat") return;
            var row = e.target.closest("[data-slot]");
            post(U.kopSave, { sleutel: row.dataset.slot, materiaalId: e.target.value }).then(function (r) { afhandelen(r, "Koppeling opgeslagen"); renderAll(); }).catch(function () { netwerkFout(); renderAll(); });
        });
        $("fk-rows").addEventListener("click", function (e) {
            var d = e.target.closest("[data-del-kop]"); if (!d) return;
            var k = stand.koppelingen.filter(function (x) { return x.id === parseInt(d.dataset.delKop, 10); })[0];
            bevestig({ title: "Slot @mat_" + k.sleutel + " verwijderen?", desc: "Een slot dat in een budgetformule gebruikt wordt, kan niet verwijderd worden. Pas dan eerst die formule aan in Budgetformules.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                if (ok) post(U.kopDel, { id: k.id }).then(function (r) { afhandelen(r, "Koppeling verwijderd"); renderAll(); }).catch(netwerkFout);
            });
        });
        $("fk-add-naam").addEventListener("input", function () { $("fk-add-slot").textContent = slugOf(this.value) || "…"; });
        $("fk-add").addEventListener("submit", function (e) {
            e.preventDefault();
            var naam = $("fk-add-naam").value.trim();
            if (!naam) { toast("warning", "Omschrijving ontbreekt", "Geef een omschrijving in, bv. Gordijnwand systeem."); $("fk-add-naam").focus(); return; }
            var mat = $("fk-add-mat").querySelector("input[type=hidden]").value;
            post(U.kopNew, { naam: naam, materiaalId: mat }).then(function (r) {
                if (afhandelen(r, "Slot aangemaakt", r.sleutel ? "@mat_" + r.sleutel : "")) { $("fk-add-naam").value = ""; $("fk-add-slot").textContent = "…"; ui.fkGroep = "alle"; }
                renderAll();
            }).catch(netwerkFout);
        });
    }

    function renderAll() { renderLots(); renderMat(); renderBk(); renderFk(); }
    showTab(ui.tab);
    renderAll();
})();
