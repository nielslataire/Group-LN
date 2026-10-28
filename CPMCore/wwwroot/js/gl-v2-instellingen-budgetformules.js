// gl-v2 — Instellingen › Budgetformules (design-handoff punt 53a). Drie kolommen: formules per lot, de editor met een live uitwerking en resultaat tegenover een
// testbudget, en de parameters (klik of typ @ om in te voegen). Opslaan/verwijderen gaan naar de …V2-acties (schrijfrecht, antiforgery); testen en de parameterlijst
// hergebruiken de bestaande alleen-lezen acties (BudgetFormuleTest, GetBudgetFormuleParameters). Het testen gebeurt vanzelf kort na het typen zodra er een
// testbudget gekozen is; zonder testbudget staat er een uitnodiging om te testen.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-bf");
    if (!root) return;
    var data = JSON.parse(document.getElementById("bf-data").textContent);
    var canWrite = root.dataset.canWrite === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var U = { params: root.dataset.urlParams, test: root.dataset.urlTest, save: root.dataset.urlSave, del: root.dataset.urlDel };

    var formules = data.formules.slice();
    var params = [];
    var ui = { sel: null, draft: null, q: "", lot: "", status: "", pq: "", versie: "" };   // sel: formule-id | "nieuw" | null
    var testSeq = 0, testTimer = null, lastTest = null;
    var editor = null;   // het formuleveld van de editor (voor invoegen)

    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js
    var nl2 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function fmt(v) { return v == null ? "" : nl2.format(v); }
    function lotNr(n) { return Number(n).toFixed(1); }
    function postForm(url, d) {
        var fd = new FormData();
        Object.keys(d).forEach(function (k) { fd.append(k, d[k]); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function netwerkFout() { toast("danger", "Niet gelukt", "De server gaf geen antwoord. Probeer het opnieuw."); }
    function bevestig(o) { return window.GlV2Bevestig ? window.GlV2Bevestig(o) : Promise.resolve(window.confirm(o.title)); }
    function formuleById(id) { return formules.filter(function (f) { return f.id === id; })[0]; }
    function formuleVanActiviteit(aid) { return formules.filter(function (f) { return f.activityId === aid; })[0]; }

    // Keuzelijst-markup zoals GlV2/_SelectList (basisvariant); window.GlV2Select.init bedraadt ze na het tekenen.
    function selHtml(o) {
        var items = o.items || [], cur = items.filter(function (i) { return String(i.value) === String(o.value == null ? "" : o.value); })[0];
        var emptyChosen = !cur && String(o.value == null ? "" : o.value) === "" && o.emptyText != null;
        var label = cur ? cur.text : emptyChosen ? o.emptyText : (o.placeholder || "Kies …");
        var opts = "", last = null;
        if (o.emptyText != null) opts += '<button type="button" class="gl-v2-select-option' + (emptyChosen ? " is-selected" : "") + '" role="option" data-value=""><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(o.emptyText) + "</span></button>";
        items.forEach(function (i) {
            if (i.group && i.group !== last) { last = i.group; opts += '<div class="gl-v2-select-group-header"><span class="gl-v2-select-group-label">' + esc(String(i.group).toUpperCase()) + '</span><span class="gl-v2-select-group-rule"></span></div>'; }
            opts += '<button type="button" class="gl-v2-select-option' + (cur === i ? " is-selected" : "") + '" role="option" data-value="' + esc(i.value) + '"><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(i.text) + "</span></button>";
        });
        return '<div class="gl-v2-field' + (o.label ? "" : " gl-v2-field-bare") + '">' + (o.label ? '<span class="gl-v2-field-label">' + esc(o.label) + "</span>" : "") +
            '<div class="gl-v2-select" data-gl-v2-select data-placeholder="' + esc(o.placeholder || "Kies …") + '"><input type="hidden" id="' + esc(o.id) + '" name="' + esc(o.name) + '" value="' + esc(o.value == null ? "" : o.value) + '" />' +
            '<div class="gl-v2-select-trigger' + (cur || emptyChosen ? " is-filled" : "") + '" role="button" tabindex="0" aria-haspopup="listbox" aria-expanded="false" aria-label="' + esc(o.aria || o.label || o.name) + '"><span class="gl-v2-select-trigger-label">' + esc(label) + '</span><i class="ph ph-caret-down gl-v2-select-trigger-caret" aria-hidden="true"></i></div>' +
            '<div class="gl-v2-select-panel" role="listbox">' + opts + "</div></div></div>";
    }
    function setSelect(wrapId, items, value, emptyText) { window.GlV2Select.setItems($(wrapId), items, value == null ? "" : value, emptyText == null ? null : emptyText); }

    // ── Linkerlijst ───────────────────────────────────────────────────────────────────────────────
    function lotten() {
        var m = new Map();
        formules.forEach(function (f) { m.set(f.lotNummer, f.lotNaam); });
        return Array.from(m.entries()).sort(function (a, b) { return a[0] - b[0]; });
    }
    function renderFilters() {
        setSelect("bf-lot_select", lotten().map(function (l) { return { value: l[0], text: "Lot " + lotNr(l[0]) + " · " + l[1] }; }), ui.lot, "Alle loten");
    }
    function gefilterd() {
        var q = norm(ui.q);
        return formules.filter(function (f) {
            if (ui.lot && String(f.lotNummer) !== ui.lot) return false;
            if (ui.status === "actief" && !f.actief) return false;
            if (ui.status === "inactief" && f.actief) return false;
            return !q || norm(f.activityOmschrijving).indexOf(q) >= 0 || norm(f.formule).indexOf(q) >= 0;
        });
    }
    function renderList() {
        var list = gefilterd();
        $("bf-count").textContent = list.length === formules.length ? formules.length : list.length + "/" + formules.length;
        var actief = formules.filter(function (f) { return f.actief; }).length;
        $("bf-foot").textContent = actief + " actief · " + (formules.length - actief) + " inactief";
        if (!list.length) { $("bf-items").innerHTML = '<div class="gl-v2-bf-empty">Geen formules gevonden.</div>'; return; }
        var groepen = new Map();
        list.forEach(function (f) { if (!groepen.has(f.lotNummer)) groepen.set(f.lotNummer, { naam: f.lotNaam, items: [] }); groepen.get(f.lotNummer).items.push(f); });
        var html = "";
        Array.from(groepen.keys()).sort(function (a, b) { return a - b; }).forEach(function (nr) {
            var g = groepen.get(nr);
            html += '<div class="gl-v2-bf-lot"><span>Lot ' + lotNr(nr) + " · " + esc(g.naam) + "</span><span>" + g.items.length + "</span></div>";
            g.items.forEach(function (f) {
                html += '<button type="button" class="gl-v2-bf-item' + (f.id === ui.sel ? " is-active" : "") + '" data-id="' + f.id + '"><span class="gl-v2-bf-dot ' + (f.actief ? "on" : "off") + '" title="' + (f.actief ? "Actief" : "Inactief") + '"></span>' +
                    '<span class="gl-v2-bf-item-text"><span class="t">' + esc(f.activityOmschrijving) + '</span><span class="p">' + esc(f.formule) + "</span></span></button>";
            });
        });
        $("bf-items").innerHTML = html;
    }
    $("bf-items").addEventListener("click", function (e) { var b = e.target.closest("[data-id]"); if (b) kies(parseInt(b.dataset.id, 10)); });
    $("bf-search").addEventListener("input", function () { ui.q = this.value; renderList(); });
    $("bf-search-clear").addEventListener("click", function () { $("bf-search").value = ""; ui.q = ""; renderList(); $("bf-search").focus(); });
    $("bf-lot").addEventListener("change", function () { ui.lot = this.value; renderList(); });
    $("bf-status").addEventListener("change", function () { ui.status = this.value; renderList(); });

    // ── Editor ────────────────────────────────────────────────────────────────────────────────────
    function dirty() {
        var d = ui.draft; if (!d) return false;
        if (ui.sel === "nieuw") return !!(d.formule.trim() || d.label.trim() || d.activityId);
        var f = formuleById(ui.sel); if (!f) return false;
        return d.formule !== f.formule || d.label !== (f.omschrijving || "") || d.actief !== f.actief;
    }
    function leaveGuard(thenFn) {
        if (!dirty()) { thenFn(); return; }
        bevestig({ title: "Wijzigingen niet opslaan?", desc: "Je hebt wijzigingen aan deze formule die nog niet zijn opgeslagen. Ze gaan verloren.", ok: "Niet opslaan", tone: "warning" }).then(function (ok) { if (ok) thenFn(); });
    }
    function kies(id) { if (id === ui.sel) return; leaveGuard(function () { selecteer(id); }); }
    function selecteer(id) {
        ui.sel = id;
        var f = formuleById(id);
        ui.draft = f ? { formule: f.formule, label: f.omschrijving || "", actief: f.actief, activityId: f.activityId } : null;
        lastTest = null; renderList(); renderEditor(); planTest(0);
    }
    function nieuw() {
        leaveGuard(function () {
            ui.sel = "nieuw"; ui.draft = { formule: "", label: "", actief: true, activityId: 0 }; lastTest = null; renderList(); renderEditor(); planTest(0);
        });
    }
    function leeg() {
        ui.sel = null; ui.draft = null; lastTest = null; renderList();
        $("bf-editor").innerHTML = '<div class="gl-v2-blank gl-v2-bf-blank"><i class="ph ph-function" aria-hidden="true"></i><b>Kies een formule</b><span>Selecteer links een formule' + (canWrite ? ", of maak een nieuwe aan." : ".") + "</span></div>";
        editor = null;
    }

    var OPERATORS = '<div class="gl-v2-bf-ops"><span class="k">Operatoren</span><code>+ − × ÷ ( )</code><span class="k">Voorwaarde</span><code>ALS(x &gt; 4; a; b)</code><span class="k">Combineren</span><code>EN(x; y)</code> · <code>OF(x; y)</code></div>';

    function activiteitSelect() {
        var bezet = new Set(formules.map(function (f) { return f.activityId; }));
        var items = data.activiteiten.slice().sort(function (a, b) { return a.lotNummer - b.lotNummer || a.naam.localeCompare(b.naam, "nl"); }).map(function (a) {
            return { value: a.activityId, text: a.naam + (bezet.has(a.activityId) ? " (heeft al een formule)" : ""), group: "Lot " + lotNr(a.lotNummer) + " · " + a.lotNaam };
        });
        return selHtml({ id: "bf-act", name: "activiteit", label: "Activiteit", items: items, value: ui.draft.activityId || "", placeholder: "Kies een activiteit" });
    }

    function renderEditor() {
        var d = ui.draft; if (!d) { leeg(); return; }
        var f = formuleById(ui.sel), isNew = ui.sel === "nieuw";
        var head = isNew
            ? '<span class="gl-v2-badge is-positive">Nieuw</span><h2 class="gl-v2-bf-title">Nieuwe formule</h2>'
            : '<span class="gl-v2-badge is-info">Lot ' + lotNr(f.lotNummer) + '</span><h2 class="gl-v2-bf-title">' + esc(f.activityOmschrijving) + "</h2>";
        var html = '<div class="gl-v2-bf-edhead">' + head + '<span class="sp"></span>' +
            (isNew ? "" : '<label class="gl-v2-bf-active"><input type="checkbox" class="gl-v2-toggle-switch" id="bf-actief"' + (d.actief ? " checked" : "") + (canWrite ? "" : " disabled") + ' /> <span>Actief</span></label>') + "</div>" +
            '<div class="gl-v2-bf-edbody">' +
            (isNew ? '<div class="gl-v2-bf-newrow">' + activiteitSelect() + "</div>" : "") +
            '<div class="gl-v2-field"><label class="gl-v2-field-label" for="bf-label">LABEL IN DE BUDGETWIZARD</label><div class="gl-v2-field-box"><input class="gl-v2-field-input" id="bf-label" maxlength="300" value="' + esc(d.label) + '" placeholder="bv. Platte daken"' + (canWrite ? "" : " disabled") + " /></div></div>" +
            '<div class="gl-v2-bf-flabel"><label for="bf-formule">FORMULE — TOTAAL PROJECT</label><span>typ @ voor een parameter</span></div>' +
            '<textarea class="gl-v2-bf-formula" id="bf-formule" spellcheck="false" rows="4" placeholder="bv. @mat_dakwerken_platdak * @opp_platdak"' + (canWrite ? "" : " readonly") + ">" + esc(d.formule) + "</textarea>" +
            OPERATORS +
            '<div class="gl-v2-bf-error" id="bf-error" role="alert" hidden></div>' +
            '<div class="gl-v2-bf-work"><div class="gl-v2-bf-work-head"><span>UITWERKING</span><span id="bf-work-status"></span></div><div id="bf-work"></div></div>' +
            (canWrite ? '<div class="gl-v2-bf-actions"><button type="button" class="gl-v2-btn gl-v2-btn-primary" id="bf-save"><i class="ph ph-floppy-disk" aria-hidden="true"></i>Opslaan</button>' +
                (isNew ? '<button type="button" class="gl-v2-btn gl-v2-btn-text" id="bf-cancel">Annuleren</button>' : '<button type="button" class="gl-v2-btn gl-v2-btn-text" id="bf-restore">Herstel</button>') +
                '<span class="gl-v2-bf-dirty" id="bf-dirty" hidden><i></i>Niet opgeslagen wijzigingen</span><span class="sp"></span>' +
                (isNew ? "" : '<span class="gl-v2-bf-meta">Laatst gewijzigd ' + esc(f.laatstGewijzigd) + '</span><button type="button" class="gl-v2-btn gl-v2-btn-danger" id="bf-del"><i class="ph ph-trash" aria-hidden="true"></i>Verwijderen</button>') + "</div>" : "") +
            "</div>";
        $("bf-editor").innerHTML = html;
        window.GlV2Select.init($("bf-editor"));
        editor = $("bf-formule");
        wireEditor(); renderWork(); updateDirty();
    }

    function wireEditor() {
        editor.addEventListener("input", function () { ui.draft.formule = editor.value; updateDirty(); updateSuggest(); planTest(600); });
        editor.addEventListener("click", updateSuggest);
        editor.addEventListener("blur", function () { setTimeout(sluitSuggest, 150); });
        editor.addEventListener("keydown", suggestKeys);
        var lab = $("bf-label"); if (lab) lab.addEventListener("input", function () { ui.draft.label = lab.value; updateDirty(); });
        var act = $("bf-actief"); if (act) act.addEventListener("change", function () { ui.draft.actief = act.checked; updateDirty(); });
        var sel = $("bf-act");
        if (sel) sel.addEventListener("change", function () {
            var id = parseInt(sel.value || "0", 10); ui.draft.activityId = id;
            var bestaand = id ? formuleVanActiviteit(id) : null;
            if (bestaand) {
                ui.draft.formule = bestaand.formule; ui.draft.label = bestaand.omschrijving || ""; editor.value = bestaand.formule; $("bf-label").value = ui.draft.label;
                toast("info", "Deze activiteit heeft al een formule", "Je bewerkt ze hier; opslaan vervangt de bestaande formule.");
                planTest(0);
            }
            updateDirty();
        });
        if ($("bf-save")) $("bf-save").addEventListener("click", opslaan);
        if ($("bf-restore")) $("bf-restore").addEventListener("click", function () { selecteerOpnieuw(); });
        if ($("bf-cancel")) $("bf-cancel").addEventListener("click", function () { leaveGuard(function () { var eerste = formules[0]; if (eerste) selecteer(eerste.id); else leeg(); }); });
        if ($("bf-del")) $("bf-del").addEventListener("click", verwijderen);
    }
    function selecteerOpnieuw() { var id = ui.sel; ui.sel = null; selecteer(id); }
    function updateDirty() {
        var b = $("bf-dirty"); if (b) b.hidden = !dirty();
        var r = $("bf-restore"); if (r) r.disabled = !dirty();
    }
    window.addEventListener("beforeunload", function (e) { if (dirty() && canWrite) { e.preventDefault(); e.returnValue = ""; } });

    // ── Testen en uitwerking ──────────────────────────────────────────────────────────────────────
    function chips(expr) { return esc(expr).replace(/@[A-Za-z0-9_]+/g, function (m) { return '<span class="gl-v2-bf-chip">' + m + "</span>"; }); }
    function renderWork() {
        var el = $("bf-work"); if (!el) return;
        var st = $("bf-work-status"), err = $("bf-error");
        err.hidden = true;
        if (!ui.versie) {
            st.textContent = "";
            el.innerHTML = '<div class="gl-v2-bf-work-empty"><span>Zet testen aan om de formule door te rekenen met een echt budget.</span><button type="button" class="gl-v2-btn gl-v2-btn-secondary" id="bf-test-on"><i class="ph ph-flask" aria-hidden="true"></i>Testen aan</button></div>';
            return;
        }
        if (!ui.draft.formule.trim()) { st.textContent = ""; el.innerHTML = '<div class="gl-v2-bf-work-empty"><span>Typ een formule om ze door te rekenen.</span></div>'; return; }
        if (!lastTest) { st.innerHTML = '<i class="ph ph-circle-notch gl-v2-spin" aria-hidden="true"></i> Rekent…'; el.innerHTML = ""; return; }
        if (lastTest.netwerk) { st.textContent = ""; el.innerHTML = '<div class="gl-v2-bf-work-empty"><span>Testen is niet gelukt. Probeer het opnieuw.</span></div>'; return; }
        if (!lastTest.ok) {
            st.textContent = "";
            err.hidden = false; err.innerHTML = '<i class="ph ph-warning-circle" aria-hidden="true"></i><span>' + esc(lastTest.fout || "De formule kon niet berekend worden.") + "</span>";
            el.innerHTML = '<div class="gl-v2-bf-work-empty"><span>Herstel de fout om de uitwerking te zien.</span></div>';
            return;
        }
        st.textContent = "met " + (data.testVersies.filter(function (v) { return v.waarde === ui.versie; })[0] || { tekst: "" }).tekst;
        var html = "";
        if (lastTest.onbekendeParams && lastTest.onbekendeParams.length)
            html += '<div class="gl-v2-bf-warn"><i class="ph ph-warning" aria-hidden="true"></i><span>Onbekende parameter(s): ' + esc(lastTest.onbekendeParams.join(", ")) + ". Ze tellen als 0.</span></div>";
        html += '<div class="gl-v2-bf-terms">';
        lastTest.termen.forEach(function (t) {
            html += '<div class="gl-v2-bf-term"><span class="n">' + chips(t.exprNamen) + '</span><span class="v">' + esc(t.exprWaarden) + '</span><span class="a">= € ' + fmt(t.waarde) + "</span></div>";
        });
        html += "</div>" +
            '<div class="gl-v2-bf-result"><span class="l">Resultaat</span><span class="t">€ ' + fmt(lastTest.totaal) + '</span><span class="p">÷ ' + lastTest.aantalEenheden + " eenheden = <b>€ " + fmt(lastTest.perEenheid) + "</b> per eenheid</span></div>";
        el.innerHTML = html;
    }
    function planTest(ms) {
        clearTimeout(testTimer);
        if (!ui.versie || !ui.draft || !ui.draft.formule.trim()) { renderWork(); return; }
        lastTest = null; renderWork();
        testTimer = setTimeout(doeTest, ms);
    }
    function doeTest() {
        var seq = ++testSeq, formule = ui.draft.formule;
        fetch(U.test, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ versieId: parseInt(ui.versie, 10), formule: formule }) })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (res) { if (seq === testSeq) { lastTest = res; renderWork(); } })
            .catch(function () { if (seq === testSeq) { lastTest = { netwerk: true }; renderWork(); } });
    }
    $("bf-editor").addEventListener("click", function (e) {
        if (!e.target.closest("#bf-test-on")) return;
        var eerste = data.testVersies[0];
        if (!eerste) { toast("warning", "Geen testbudget", "Er is nog geen budgetversie om mee te testen."); return; }
        setSelect("bf-versie_select", versieItems(), eerste.waarde, "Geen testbudget"); kiesVersie(eerste.waarde);
    });
    function versieItems() { return data.testVersies.map(function (v) { return { value: v.waarde, text: v.tekst }; }); }
    function kiesVersie(v) { ui.versie = v || ""; laadParameters(); planTest(0); }
    $("bf-versie").addEventListener("change", function () { kiesVersie(this.value); });

    // ── Opslaan en verwijderen ────────────────────────────────────────────────────────────────────
    function opslaan() {
        var d = ui.draft, aid = ui.sel === "nieuw" ? d.activityId : formuleById(ui.sel).activityId;
        if (!aid) { toast("warning", "Activiteit ontbreekt", "Kies eerst een activiteit."); return; }
        if (!d.formule.trim()) { toast("warning", "Formule is leeg", "Typ een formule voor je opslaat."); editor.focus(); return; }
        var btn = $("bf-save"); btn.disabled = true;
        postForm(U.save, { activityId: aid, formule: d.formule, omschrijving: d.label, actief: d.actief }).then(function (r) {
            btn.disabled = false;
            if (!r.ok) { toast("danger", "Opslaan mislukt", r.bericht || ""); return; }
            var i = formules.findIndex(function (x) { return x.activityId === r.formule.activityId; });
            if (i >= 0) formules[i] = r.formule; else formules.push(r.formule);
            toast("success", "Formule opgeslagen", r.formule.activityOmschrijving);
            renderFilters(); ui.sel = null; selecteer(r.formule.id);
        }).catch(function () { btn.disabled = false; netwerkFout(); });
    }
    function verwijderen() {
        var f = formuleById(ui.sel);
        bevestig({ title: "Formule verwijderen?", desc: f.activityOmschrijving + " valt daarna terug op de standaardberekening.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
            if (!ok) return;
            postForm(U.del, { id: f.id }).then(function (r) {
                if (!r.ok) { toast("danger", "Verwijderen mislukt", r.bericht || ""); return; }
                formules = formules.filter(function (x) { return x.id !== f.id; });
                toast("success", "Formule verwijderd", f.activityOmschrijving);
                renderFilters(); ui.sel = null; ui.draft = null; if (formules[0]) selecteer(formules[0].id); else leeg();
            }).catch(netwerkFout);
        });
    }
    if (canWrite) $("bf-new").addEventListener("click", nieuw);

    // ── Parameters ────────────────────────────────────────────────────────────────────────────────
    function laadParameters() {
        var url = ui.versie ? U.params + "?versieId=" + encodeURIComponent(ui.versie) : U.params;
        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (p) { params = p; renderParams(); })
            .catch(function () { $("bf-params").innerHTML = '<div class="gl-v2-bf-empty">Parameters laden mislukt.</div>'; });
    }
    function renderParams() {
        var q = norm(ui.pq), heeftWaarden = params.some(function (p) { return p.waarde != null; });
        var list = q ? params.filter(function (p) { return norm(p.naam).indexOf(q) >= 0 || norm(p.omschrijving).indexOf(q) >= 0; }) : params;
        if (!list.length) { $("bf-params").innerHTML = '<div class="gl-v2-bf-empty">Geen parameters gevonden.</div>'; return; }
        var groepen = new Map();
        list.forEach(function (p) { if (!groepen.has(p.categorie)) groepen.set(p.categorie, []); groepen.get(p.categorie).push(p); });
        var html = "";
        groepen.forEach(function (items, cat) {
            html += '<div class="gl-v2-bf-pcat">' + esc(cat) + "</div>";
            items.forEach(function (p) {
                html += '<button type="button" class="gl-v2-bf-param" data-naam="' + esc(p.naam) + '" title="' + esc(p.omschrijving) + '"' + (canWrite ? "" : " disabled") + '><span class="n">@' + esc(p.naam) + '</span><span class="v">' + (heeftWaarden && p.waarde != null ? fmt(p.waarde) : "") + '</span><span class="u">' + esc(p.eenheid || "") + "</span></button>";
            });
        });
        $("bf-params").innerHTML = html;
    }
    $("bf-param-search").addEventListener("input", function () { ui.pq = this.value; renderParams(); });
    $("bf-params").addEventListener("click", function (e) { var b = e.target.closest("[data-naam]"); if (b) voegIn(b.dataset.naam); });
    function voegIn(naam) {
        if (!editor || !canWrite) { toast("info", "Open eerst een formule", "Kies of maak een formule om er een parameter in te voegen."); return; }
        var s = editor.selectionStart == null ? editor.value.length : editor.selectionStart, e = editor.selectionEnd == null ? s : editor.selectionEnd;
        editor.value = editor.value.slice(0, s) + "@" + naam + editor.value.slice(e);
        var pos = s + naam.length + 1; editor.focus(); editor.setSelectionRange(pos, pos);
        ui.draft.formule = editor.value; updateDirty(); planTest(300);
    }

    // ── @-suggesties in het formuleveld ───────────────────────────────────────────────────────────
    var sug = $("bf-suggest"), sugItems = [], sugIndex = 0, sugStart = -1;
    function sluitSuggest() { sug.hidden = true; sugItems = []; sugIndex = 0; }
    function updateSuggest() {
        if (!editor) return;
        var caret = editor.selectionStart, voor = editor.value.slice(0, caret), m = voor.match(/@([a-zA-Z0-9_]*)$/);
        if (!m) { sluitSuggest(); return; }
        sugStart = caret - m[0].length;
        sugItems = params.filter(function (p) { return p.naam.toLowerCase().indexOf(m[1].toLowerCase()) >= 0; }).slice(0, 10);
        if (!sugItems.length) { sluitSuggest(); return; }
        sugIndex = 0;
        sug.innerHTML = sugItems.map(function (p, i) { return '<div class="i' + (i === 0 ? " is-active" : "") + '" role="option" data-i="' + i + '"><span class="n">@' + esc(p.naam) + '</span><span class="o">' + esc(p.omschrijving) + "</span>" + (p.waarde != null ? '<span class="v">' + fmt(p.waarde) + "</span>" : "") + "</div>"; }).join("");
        var r = editor.getBoundingClientRect();
        sug.style.left = r.left + "px"; sug.style.top = (r.bottom + 4) + "px"; sug.style.width = r.width + "px"; sug.hidden = false;
    }
    sug.addEventListener("mousedown", function (e) { var i = e.target.closest("[data-i]"); if (i) { e.preventDefault(); kiesSuggest(parseInt(i.dataset.i, 10)); } });
    function kiesSuggest(i) {
        if (i < 0 || i >= sugItems.length) return;
        var naam = sugItems[i].naam, caret = editor.selectionStart;
        editor.value = editor.value.slice(0, sugStart) + "@" + naam + editor.value.slice(caret);
        var pos = sugStart + naam.length + 1; editor.setSelectionRange(pos, pos); editor.focus(); sluitSuggest();
        ui.draft.formule = editor.value; updateDirty(); planTest(300);
    }
    function suggestKeys(e) {
        if (sug.hidden) return;
        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            e.preventDefault(); sugIndex = (sugIndex + (e.key === "ArrowDown" ? 1 : -1) + sugItems.length) % sugItems.length;
            sug.querySelectorAll(".i").forEach(function (el, i) { el.classList.toggle("is-active", i === sugIndex); });
        } else if (e.key === "Enter" || e.key === "Tab") { e.preventDefault(); kiesSuggest(sugIndex); }
        else if (e.key === "Escape") sluitSuggest();
    }

    // ── Start ─────────────────────────────────────────────────────────────────────────────────────
    setSelect("bf-versie_select", versieItems(), "", "Geen testbudget");
    renderFilters(); renderList(); laadParameters();
    if (formules.length) selecteer(formules[0].id); else leeg();
})();
