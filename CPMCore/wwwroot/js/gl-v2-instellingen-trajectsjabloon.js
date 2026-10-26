// gl-v2 — Instellingen › Trajectsjablonen › Sjabloon bewerken (design-handoff punt 56e). Dezelfde logica en dezelfde opslag (payloadJson naar TrajectSjabloonAdmin/Opslaan) als de
// klassieke editor (trajectsjabloon.admin.js), met gl-v2-onderdelen: fases links als boom, de mijlpaal rechts in vijf genummerde stappen ("wanneer bereikt" als twee keuzekaarten,
// streefdatum met berekende datum), een tijdlijn met simulatie en een controle. Selects zijn de projectbrede keuzelijst (GlV2Select); herschikken kan slepen of Alt + pijl.
(function () {
    "use strict";

    var PROJECT_CREATED = "PROJECT_CREATED";
    var XDAGEN_VOOR_DOELDATUM = 2; // BOCore.TriggerEvent — enige event dat een offset-dagen-waarde gebruikt
    var FASE_PALET = ["#0a5a3b", "#c17d1f", "#7A8450", "#8B6B4A", "#0f7a52", "#8A7967"];

    function $(id) { return document.getElementById(id); }
    function readJson(id, fallback) { var el = $(id); if (!el) return fallback; try { return JSON.parse(el.textContent); } catch (e) { return fallback; } }
    function debounce(fn, wait) { var t; return function () { clearTimeout(t); var a = arguments; t = setTimeout(function () { fn.apply(null, a); }, wait); }; }
    function esc(s) { return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;"); }
    var uidSeq = 0;
    function uid() { return "u" + (++uidSeq); }
    function bevestig(title, html, ok) {
        return window.GlV2Bevestig({ title: title, desc: String(html).replace(/<[^>]+>/g, ""), ok: ok, tone: "danger" });
    }

    var initial = readJson("sjabloonData", { fases: [] });
    var typeOpties = readJson("typeOpties", []);
    var rolOpties = readJson("rolOpties", []);
    var bindingOpties = readJson("bindingOpties", [{ v: 0, n: "Handmatig" }]);
    var triggerEventOpties = readJson("triggerEventOpties", []);
    var triggerActieOpties = readJson("triggerActieOpties", []);
    var dossierKindOpties = readJson("dossierKindOpties", []);
    var projectVlagOpties = readJson("projectVlagOpties", []);
    var bronParamOptiesByBinding = readJson("bronParamOptiesByBinding", {});
    var bronParamModeByBinding = readJson("bronParamModeByBinding", {});
    var bronParamHintByBinding = readJson("bronParamHintByBinding", {});

    var faseTree = $("faseTree"), detailPane = $("detailPane");
    if (!faseTree || !detailPane) return;

    function optLabel(opties, v) { for (var i = 0; i < opties.length; i++) { if (opties[i].v === v) return opties[i].n; } return null; }
    function todayIso() { var d = new Date(); return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0"); }

    // ── Bouwstenen in de projectbrede stijl ─────────────────────────────────────────────────────
    /** Keuzelijst (markup van GlV2/_SelectList); de verborgen invoer draagt data-field e.d. zodat de bestaande delegatie blijft werken. */
    function selHtml(o) {
        var items = o.items || [], val = o.value == null ? "" : String(o.value);
        var cur = items.filter(function (i) { return String(i.v) === val; })[0];
        var emptyChosen = !cur && val === "" && o.blank != null;
        var label = cur ? cur.n : emptyChosen ? o.blank : (o.placeholder || "Kies …");
        var opts = "";
        if (o.blank != null) opts += '<button type="button" class="gl-v2-select-option' + (emptyChosen ? " is-selected" : "") + '" role="option" data-value=""><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(o.blank) + "</span></button>";
        items.forEach(function (i) { opts += '<button type="button" class="gl-v2-select-option' + (cur === i ? " is-selected" : "") + '" role="option" data-value="' + esc(i.v) + '"><i class="ph ph-check" aria-hidden="true"></i><span>' + esc(i.n) + "</span></button>"; });
        var data = "";
        Object.keys(o.attrs || {}).forEach(function (k) { data += " data-" + k + '="' + esc(o.attrs[k]) + '"'; });
        return '<div class="gl-v2-field">' + (o.label ? '<span class="gl-v2-field-label">' + esc(o.label) + "</span>" : "") +
            '<div class="gl-v2-select" data-gl-v2-select data-placeholder="' + esc(o.placeholder || "Kies …") + '"><input type="hidden" id="' + esc(o.id) + '" data-field="' + esc(o.field) + '"' + data + ' value="' + esc(val) + '" />' +
            '<div class="gl-v2-select-trigger' + (cur || emptyChosen ? " is-filled" : "") + '" role="button" tabindex="0" aria-haspopup="listbox" aria-expanded="false" aria-label="' + esc(o.aria || o.label || o.field) + '"><span class="gl-v2-select-trigger-label">' + esc(label) + '</span><i class="ph ph-caret-down gl-v2-select-trigger-caret" aria-hidden="true"></i></div>' +
            '<div class="gl-v2-select-panel" role="listbox">' + opts + "</div></div>" +
            (o.hint ? '<span class="gl-v2-field-help">' + o.hint + "</span>" : "") + "</div>";
    }
    function inpHtml(o) {
        var data = "";
        Object.keys(o.attrs || {}).forEach(function (k) { data += " data-" + k + '="' + esc(o.attrs[k]) + '"'; });
        return '<div class="gl-v2-field">' + (o.label ? '<label class="gl-v2-field-label" for="' + o.id + '">' + esc(o.label) + "</label>" : "") +
            '<div class="gl-v2-field-box"><input id="' + o.id + '" type="' + (o.type || "text") + '" class="gl-v2-field-input' + (o.mono ? " gl-v2-tsa-mono" : "") + '" data-field="' + o.field + '"' + data + ' value="' + esc(o.value) + '"' +
            (o.placeholder ? ' placeholder="' + esc(o.placeholder) + '"' : "") + (o.inputmode ? ' inputmode="' + o.inputmode + '"' : "") + (o.hintId ? ' aria-describedby="' + o.hintId + '"' : "") + " /></div>" +
            (o.hint ? '<span class="gl-v2-field-help"' + (o.hintId ? ' id="' + o.hintId + '"' : "") + ">" + o.hint + "</span>" : "") + "</div>";
    }
    function toggleHtml(o) {
        var data = "";
        Object.keys(o.attrs || {}).forEach(function (k) { data += " data-" + k + '="' + esc(o.attrs[k]) + '"'; });
        return '<label class="gl-v2-toggle-row"' + (o.id ? ' for="' + o.id + '"' : "") + '><input type="checkbox" class="gl-v2-toggle-switch"' + (o.id ? ' id="' + o.id + '"' : "") + ' data-field="' + o.field + '"' + data + (o.checked ? " checked" : "") + (o.aria ? ' aria-label="' + esc(o.aria) + '"' : "") + " />" +
            (o.text ? '<span class="gl-v2-toggle-text"><span class="gl-v2-toggle-label">' + esc(o.text) + "</span></span>" : "") + "</label>";
    }
    function initSelects(host) { if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(host); }

    // ── State ───────────────────────────────────────────────────────────────────────────────────
    function cloneTrigger(t) {
        return { _uid: uid(), triggerEvent: t.triggerEvent || 0, triggerActie: t.triggerActie || 0, offsetDagen: t.offsetDagen != null ? t.offsetDagen : null, actieParametersJson: t.actieParametersJson || "",
            magProjectWijzigen: !!t.magProjectWijzigen, isActief: t.isActief !== false, omschrijving: t.omschrijving || "" };
    }
    function cloneMijlpaal(m) {
        return { _uid: uid(), naam: m.naam || "", code: m.code || "", mijlpaalType: m.mijlpaalType || 0, scope: m.scope || 0, verantwoordelijkeRol: m.verantwoordelijkeRol != null ? m.verantwoordelijkeRol : null,
            doeldatumAnkerCode: m.doeldatumAnkerCode || PROJECT_CREATED, doeldatumOffsetDagen: m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : null, isVerplicht: m.isVerplicht !== false,
            bronBinding: m.bronBinding != null ? m.bronBinding : 0, bronParam: m.bronParam || "", dossierKind: m.dossierKind != null ? m.dossierKind : null, omschrijving: m.omschrijving || "", triggers: (m.triggers || []).map(cloneTrigger) };
    }
    function cloneFase(f, idx) {
        return { _uid: uid(), naam: f.naam || "", code: f.code || "", volgorde: f.volgorde || (idx + 1) * 10, kleurCode: f.kleurCode || null,
            standaardProjectStatusId: f.standaardProjectStatusId != null ? f.standaardProjectStatusId : null, expanded: idx === 0, mijlpalen: (f.mijlpalen || []).map(cloneMijlpaal) };
    }
    var state = { fases: (initial.fases || []).map(cloneFase), selection: null, simStart: todayIso(), dirty: false, tlMode: "dagen" };
    if (state.fases.length && state.fases[0].mijlpalen.length) state.selection = { type: "mijlpaal", faseUid: state.fases[0]._uid, mijlpaalUid: state.fases[0].mijlpalen[0]._uid };

    function findFase(u) { for (var i = 0; i < state.fases.length; i++) { if (state.fases[i]._uid === u) return state.fases[i]; } return null; }
    function findMijlpaal(fu, mu) { var f = findFase(fu); if (!f) return null; for (var i = 0; i < f.mijlpalen.length; i++) { if (f.mijlpalen[i]._uid === mu) return f.mijlpalen[i]; } return null; }
    function findMijlpaalByCode(code) {
        if (!code) return null;
        var needle = code.trim().toLowerCase();
        for (var i = 0; i < state.fases.length; i++) { var ms = state.fases[i].mijlpalen; for (var j = 0; j < ms.length; j++) { if ((ms[j].code || "").trim().toLowerCase() === needle) return ms[j]; } }
        return null;
    }
    var dirtyEl = $("tsaDirty");
    function markDirty() { if (state.dirty) return; state.dirty = true; if (dirtyEl) dirtyEl.hidden = false; }

    // ── Streefdag-berekening (anker + offset-keten) ─────────────────────────────────────────────
    function resolveDag(code, guard) {
        if (!code || code === PROJECT_CREATED) return 0;
        if (guard.indexOf(code) !== -1) return null;
        var m = findMijlpaalByCode(code);
        if (!m) return null;
        guard.push(code);
        var basis = resolveDag(m.doeldatumAnkerCode, guard);
        guard.pop();
        if (basis == null) return null;
        return basis + (m.doeldatumOffsetDagen || 0);
    }
    function computeMijlpaalDag(m) { var b = resolveDag(m.doeldatumAnkerCode, []); return b == null ? null : b + (m.doeldatumOffsetDagen || 0); }
    var dateFmt = typeof Intl !== "undefined" ? new Intl.DateTimeFormat("nl-BE", { day: "numeric", month: "short", year: "numeric" }) : null;
    var monthFmt = typeof Intl !== "undefined" ? new Intl.DateTimeFormat("nl-BE", { month: "short", year: "numeric" }) : null;
    function dagDatum(dag) { return new Date(new Date(state.simStart + "T00:00:00").getTime() + dag * 86400000); }
    function formatDagLabel(dag) {
        if (dag == null) return '<span class="gl-v2-tsa-onbekend">streefdag onbekend (ankerketen)</span>';
        return "dag " + dag + " · " + esc(dateFmt ? dateFmt.format(dagDatum(dag)) : dagDatum(dag).toLocaleDateString());
    }

    // ═══ Boom (links) ═══════════════════════════════════════════════════════════════════════════
    function faseKleur(f, idx) { return f.kleurCode || FASE_PALET[idx % FASE_PALET.length]; }
    function getNavItems() {
        var out = [];
        state.fases.forEach(function (f) {
            out.push({ type: "fase", faseUid: f._uid });
            if (f.expanded) f.mijlpalen.forEach(function (m) { out.push({ type: "mijlpaal", faseUid: f._uid, mijlpaalUid: m._uid }); });
        });
        return out;
    }
    function navItemRow(item) {
        return item.type === "fase" ? faseTree.querySelector('.gl-v2-tsa-fase-head[data-fase-uid="' + item.faseUid + '"]') : faseTree.querySelector('.gl-v2-tsa-m-row[data-mijlpaal-uid="' + item.mijlpaalUid + '"]');
    }
    function navItemsEqual(a, b) { if (!a || !b || a.type !== b.type) return false; return a.type === "fase" ? a.faseUid === b.faseUid : a.mijlpaalUid === b.mijlpaalUid; }
    function updateRovingTabindex() {
        var items = getNavItems(); if (!items.length) return;
        var active = state.selection ? items.findIndex(function (it) { return navItemsEqual(it, state.selection); }) : -1;
        if (active === -1) active = 0;
        items.forEach(function (it, i) { var row = navItemRow(it); if (row) row.tabIndex = i === active ? 0 : -1; });
    }
    function focusNavItemDom(item) { var row = navItemRow(item); if (row) row.focus(); }

    function renderTree() {
        var html = state.fases.map(function (f, fi) {
            var isFaseSel = state.selection && state.selection.type === "fase" && state.selection.faseUid === f._uid;
            var mhtml = f.mijlpalen.map(function (m) {
                var isSel = state.selection && state.selection.type === "mijlpaal" && state.selection.mijlpaalUid === m._uid;
                var naam = m.naam ? esc(m.naam) : '<span class="is-unnamed">(naamloze mijlpaal)</span>';
                var icon = m.bronBinding ? '<i class="ph ph-lightning gl-v2-tsa-m-icon" aria-hidden="true" title="Automatische bron"></i>' : '<i class="ph ph-hand-pointing gl-v2-tsa-m-icon" aria-hidden="true" title="Handmatig afvinken"></i>';
                var dag = computeMijlpaalDag(m);
                var badge = m.triggers.length ? '<span class="gl-v2-tsa-m-badge" title="' + m.triggers.length + ' actie(s)">' + m.triggers.length + "</span>" : "";
                return '<li class="gl-v2-tsa-m-row' + (isSel ? " is-selected" : "") + '" data-mijlpaal-uid="' + m._uid + '" data-fase-uid="' + f._uid + '" draggable="true" role="button" tabindex="-1" aria-current="' + (isSel ? "true" : "false") +
                    '" aria-label="Mijlpaal: ' + esc(m.naam || "(naamloze mijlpaal)") + (m.triggers.length ? ", " + m.triggers.length + " actie(s)" : "") + '">' +
                    '<i class="ph ph-dots-six-vertical gl-v2-tsa-grip" aria-hidden="true"></i>' + icon + '<span class="gl-v2-tsa-m-naam">' + naam + "</span>" + badge +
                    '<span class="gl-v2-tsa-m-dag">' + (dag == null ? "?" : "dag " + dag) + "</span></li>";
            }).join("");
            var faseLabel = f.naam || "(naamloze fase)";
            return '<div class="gl-v2-tsa-fase' + (f.expanded ? "" : " is-collapsed") + '" data-fase-uid="' + f._uid + '">' +
                '<div class="gl-v2-tsa-fase-head' + (isFaseSel ? " is-selected" : "") + '" data-fase-uid="' + f._uid + '" draggable="true" role="button" tabindex="-1" aria-current="' + (isFaseSel ? "true" : "false") + '" aria-label="Fase: ' + esc(faseLabel) + '">' +
                '<i class="ph ph-dots-six-vertical gl-v2-tsa-grip" aria-hidden="true"></i><span class="gl-v2-tsa-dot" style="background:' + faseKleur(f, fi) + '" aria-hidden="true"></span>' +
                '<span class="gl-v2-tsa-fase-naam" data-fase-select="' + f._uid + '">' + esc(faseLabel) + "</span>" + (f.code ? '<span class="gl-v2-tsa-fase-code">' + esc(f.code) + "</span>" : "") +
                '<button type="button" class="gl-v2-tsa-chevron" data-fase-toggle="' + f._uid + '" aria-expanded="' + (f.expanded ? "true" : "false") + '" aria-label="Fase ' + esc(faseLabel) + (f.expanded ? " inklappen" : " uitklappen") + '"><i class="ph ph-caret-down" aria-hidden="true"></i></button></div>' +
                '<ul class="gl-v2-tsa-mlist" data-fase-uid="' + f._uid + '">' + mhtml + "</ul>" +
                '<button type="button" class="gl-v2-tsa-add-m" data-add-mijlpaal="' + f._uid + '"><i class="ph ph-plus" aria-hidden="true"></i>Mijlpaal</button></div>';
        }).join("");
        faseTree.innerHTML = html || '<p class="gl-v2-tsa-empty">Nog geen fases. Voeg er hieronder een toe.</p>';
        updateRovingTabindex();
    }

    function selectFase(u, focus) { state.selection = { type: "fase", faseUid: u }; renderTree(); renderDetail(); if (focus) focusNavItemDom({ type: "fase", faseUid: u }); }
    function selectMijlpaal(fu, mu, focus) { state.selection = { type: "mijlpaal", faseUid: fu, mijlpaalUid: mu }; renderTree(); renderDetail(); if (focus) focusNavItemDom({ type: "mijlpaal", faseUid: fu, mijlpaalUid: mu }); }
    function selectItem(it, focus) { if (it.type === "fase") selectFase(it.faseUid, focus); else selectMijlpaal(it.faseUid, it.mijlpaalUid, focus); }

    function moveItem(item, dir) {
        if (item.type === "fase") {
            var idx = state.fases.findIndex(function (f) { return f._uid === item.faseUid; }), n = idx + dir;
            if (idx === -1 || n < 0 || n >= state.fases.length) return;
            var t = state.fases[idx]; state.fases[idx] = state.fases[n]; state.fases[n] = t;
            state.fases.forEach(function (f, i) { f.volgorde = (i + 1) * 10; });
        } else {
            var f = findFase(item.faseUid); if (!f) return;
            var i2 = f.mijlpalen.findIndex(function (m) { return m._uid === item.mijlpaalUid; }), n2 = i2 + dir;
            if (i2 === -1 || n2 < 0 || n2 >= f.mijlpalen.length) return;
            var t2 = f.mijlpalen[i2]; f.mijlpalen[i2] = f.mijlpalen[n2]; f.mijlpalen[n2] = t2;
            f.mijlpalen.forEach(function (m, i) { m.volgorde = (i + 1) * 10; });
        }
        markDirty(); renderTree(); focusNavItemDom(item);
    }

    faseTree.addEventListener("click", function (e) {
        var toggle = e.target.closest("[data-fase-toggle]");
        if (toggle) { var f1 = findFase(toggle.getAttribute("data-fase-toggle")); if (f1) { f1.expanded = !f1.expanded; renderTree(); } return; }
        var addBtn = e.target.closest("[data-add-mijlpaal]");
        if (addBtn) {
            var f2 = findFase(addBtn.getAttribute("data-add-mijlpaal")); if (!f2) return;
            var nieuw = cloneMijlpaal({}); f2.mijlpalen.push(nieuw); f2.expanded = true; markDirty(); selectMijlpaal(f2._uid, nieuw._uid, true); return;
        }
        var mRow = e.target.closest("[data-mijlpaal-uid]");
        if (mRow) { selectMijlpaal(mRow.getAttribute("data-fase-uid"), mRow.getAttribute("data-mijlpaal-uid"), true); return; }
        var fHead = e.target.closest(".gl-v2-tsa-fase-head");
        if (fHead) selectFase(fHead.getAttribute("data-fase-uid"), true);
    });

    faseTree.addEventListener("keydown", function (e) {
        if (e.target.closest("button")) return;
        var row = e.target.closest(".gl-v2-tsa-fase-head, .gl-v2-tsa-m-row"); if (!row) return;
        var items = getNavItems(), idx = items.findIndex(function (it) { return navItemRow(it) === row; }); if (idx === -1) return;
        if (e.altKey && (e.key === "ArrowUp" || e.key === "ArrowDown")) { e.preventDefault(); moveItem(items[idx], e.key === "ArrowUp" ? -1 : 1); return; }
        if (e.key === "Enter" || e.key === " ") { e.preventDefault(); selectItem(items[idx], true); return; }
        if (e.key === "ArrowDown") { e.preventDefault(); selectItem(items[Math.min(idx + 1, items.length - 1)], true); return; }
        if (e.key === "ArrowUp") { e.preventDefault(); selectItem(items[Math.max(idx - 1, 0)], true); return; }
        if (e.key === "Home") { e.preventDefault(); selectItem(items[0], true); return; }
        if (e.key === "End") { e.preventDefault(); selectItem(items[items.length - 1], true); return; }
        if (e.key === "ArrowRight" && items[idx].type === "fase") {
            e.preventDefault(); var f3 = findFase(items[idx].faseUid);
            if (f3 && !f3.expanded) { f3.expanded = true; renderTree(); focusNavItemDom(items[idx]); } else if (f3 && f3.mijlpalen.length) selectMijlpaal(f3._uid, f3.mijlpalen[0]._uid, true);
            return;
        }
        if (e.key === "ArrowLeft") {
            e.preventDefault();
            if (items[idx].type === "fase") { var f4 = findFase(items[idx].faseUid); if (f4 && f4.expanded) { f4.expanded = false; renderTree(); focusNavItemDom(items[idx]); } }
            else selectFase(items[idx].faseUid, true);
        }
    });

    // Slepen (HTML5): fases onderling, mijlpalen binnen hun fase
    var drag = null;
    faseTree.addEventListener("dragstart", function (e) {
        var m = e.target.closest(".gl-v2-tsa-m-row"), f = e.target.closest(".gl-v2-tsa-fase-head");
        if (m) drag = { type: "mijlpaal", faseUid: m.getAttribute("data-fase-uid"), uid: m.getAttribute("data-mijlpaal-uid"), el: m };
        else if (f) drag = { type: "fase", uid: f.getAttribute("data-fase-uid"), el: f.parentElement };
        else return;
        drag.el.classList.add("is-dragging"); e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", drag.uid); } catch (x) { /* oudere browsers */ }
    });
    faseTree.addEventListener("dragover", function (e) {
        if (!drag) return;
        var over = drag.type === "fase" ? e.target.closest(".gl-v2-tsa-fase") : e.target.closest(".gl-v2-tsa-m-row");
        if (!over || over === drag.el) return;
        if (drag.type === "mijlpaal" && over.getAttribute("data-fase-uid") !== drag.faseUid) return;
        e.preventDefault();
        var r = over.getBoundingClientRect();
        over.parentElement.insertBefore(drag.el, e.clientY > r.top + r.height / 2 ? over.nextSibling : over);
    });
    faseTree.addEventListener("dragend", function () {
        if (!drag) return;
        drag.el.classList.remove("is-dragging");
        if (drag.type === "fase") {
            var order = Array.prototype.map.call(faseTree.querySelectorAll(":scope > .gl-v2-tsa-fase"), function (n) { return n.getAttribute("data-fase-uid"); });
            state.fases.sort(function (a, b) { return order.indexOf(a._uid) - order.indexOf(b._uid); });
            state.fases.forEach(function (f, i) { f.volgorde = (i + 1) * 10; });
        } else {
            var fase = findFase(drag.faseUid);
            if (fase) {
                var ul = faseTree.querySelector('.gl-v2-tsa-mlist[data-fase-uid="' + drag.faseUid + '"]');
                var o2 = Array.prototype.map.call(ul.children, function (n) { return n.getAttribute("data-mijlpaal-uid"); });
                fase.mijlpalen.sort(function (a, b) { return o2.indexOf(a._uid) - o2.indexOf(b._uid); });
                fase.mijlpalen.forEach(function (m, i) { m.volgorde = (i + 1) * 10; });
            }
        }
        drag = null; markDirty(); renderTree(); scheduleControleRefresh();
    });

    $("btnAddFase").addEventListener("click", function () { var f = cloneFase({ naam: "" }, state.fases.length); f.expanded = true; state.fases.push(f); markDirty(); selectFase(f._uid); });
    $("btnToggleAll").addEventListener("click", function (e) {
        var allOpen = state.fases.every(function (f) { return f.expanded; });
        state.fases.forEach(function (f) { f.expanded = !allOpen; });
        e.currentTarget.textContent = allOpen ? "Alles openen" : "Alles sluiten"; renderTree();
    });

    // ═══ Detail (rechts) ════════════════════════════════════════════════════════════════════════
    function anchorItems(self) {
        var items = [{ v: PROJECT_CREATED, n: "Bij aanmaken van het project" }], known = {};
        state.fases.forEach(function (f) {
            f.mijlpalen.forEach(function (m) {
                if (m._uid === self._uid || !m.code.trim()) return;
                items.push({ v: m.code, n: (f.naam ? f.naam + " · " : "") + (m.naam || "(naamloze mijlpaal)") + " (" + m.code + ")" });
                known[m.code.trim().toLowerCase()] = true;
            });
        });
        var saved = self.doeldatumAnkerCode;
        if (saved && saved !== PROJECT_CREATED && !known[saved.trim().toLowerCase()]) items.unshift({ v: saved, n: "Onbekende mijlpaal-code: " + saved });
        return items;
    }

    function paramControlHtml(m) {
        var mode = bronParamModeByBinding[m.bronBinding] || (bronParamOptiesByBinding[m.bronBinding] ? "select" : "hidden");
        var opties = bronParamOptiesByBinding[m.bronBinding] || [], hint = bronParamHintByBinding[m.bronBinding] || "";
        if (mode === "select") {
            var items = opties.slice();
            if (m.bronParam && !items.some(function (o) { return o.v === m.bronParam; })) items.unshift({ v: m.bronParam, n: "Huidige waarde: " + m.bronParam + " (niet in lijst)" });
            return selHtml({ id: "m-bronParam", field: "bronParam", label: "Bron-parameter", items: items, value: m.bronParam, placeholder: "Kies een parameter" });
        }
        if (mode === "numeriek") return inpHtml({ id: "m-bronParam", field: "bronParam", label: "Bron-parameter", value: m.bronParam, placeholder: "bv. 123", inputmode: "numeric", hint: esc(hint), hintId: "m-bronParam-hint" });
        return hint ? '<div class="gl-v2-field"><span class="gl-v2-field-help">' + esc(hint) + "</span></div>" : "";
    }

    var TRIGGER_ACTIE_PARAM_SCHEMA = {
        0: [{ key: "rol", label: "Rol (optioneel)", type: "select-int", options: rolOpties, blank: "(rol van de mijlpaal)" }, { key: "email", label: "E-mailadres, override (optioneel)", type: "text", placeholder: "naam@bedrijf.be" }],
        1: [{ key: "userId", label: "Gebruiker-Id (optioneel)", type: "text", placeholder: "(verantwoordelijke van de mijlpaal)" }],
        2: [{ key: "titel", label: "Titel (optioneel)", type: "text", placeholder: "(mijlpaalnaam)" }, { key: "omschrijving", label: "Omschrijving (optioneel)", type: "text" },
            { key: "userId", label: "Toegewezen gebruiker-Id (optioneel)", type: "text" }, { key: "rol", label: "Of toegewezen rol (optioneel)", type: "select-int", options: rolOpties, blank: "(geen)" },
            { key: "offsetDagen", label: "Vervaldag, dagen na bereiken (optioneel)", type: "number", placeholder: "7" }],
        3: [{ key: "kind", label: "Soort dossier (optioneel)", type: "select-int", options: dossierKindOpties, blank: "Vrij dossier" }, { key: "titel", label: "Titel (optioneel)", type: "text", placeholder: "(mijlpaalnaam)" }],
        4: [{ key: "vlag", label: "Vlag", type: "select-str", options: projectVlagOpties, required: true, blank: "Kies een vlag" }, { key: "waarde", label: "Waarde", type: "toggle", default: true }],
        5: [{ key: "status", label: "Nieuwe Project.StatusId", type: "number", required: true, hint: "Bekende waarden: 1 Opgeleverd, 2 Uitvoering, 3 Ontwerp, 4 Bouwaanvraag, 5 Voorverkoop, 6 Stopgezet. Mogelijk bestaan er meer." }],
        6: [{ key: "userId", label: "Gebruiker-Id (optioneel)", type: "text", placeholder: "(verantwoordelijke van de mijlpaal)" }],
        7: [], 8: []
    };
    function paramsToObject(json) { if (!json) return {}; try { var o = JSON.parse(json); return (o && typeof o === "object" && !Array.isArray(o)) ? o : {}; } catch (e) { return {}; } }
    /** Herbouwt actieParametersJson met één sleutel bijgewerkt; een leeg optioneel veld valt weg zodat het standaardgedrag van de server blijft gelden. */
    function updateTriggerParam(trigger, key, raw, type) {
        var obj = paramsToObject(trigger.actieParametersJson);
        if (type === "toggle") obj[key] = !!raw;
        else if (type === "number" || type === "select-int") { if (raw === "" || raw == null) delete obj[key]; else obj[key] = parseInt(raw, 10); }
        else { if (raw === "" || raw == null) delete obj[key]; else obj[key] = raw; }
        trigger.actieParametersJson = Object.keys(obj).length ? JSON.stringify(obj) : "";
    }
    function renderTriggerParamFields(t, idx) {
        var schema = TRIGGER_ACTIE_PARAM_SCHEMA[t.triggerActie] || [];
        if (!schema.length) return '<p class="gl-v2-tsa-empty">Geen parameters nodig voor deze actie.</p>';
        var values = paramsToObject(t.actieParametersJson);
        return '<div class="gl-v2-tsa-grid">' + schema.map(function (f) {
            var id = "m-trig-param-" + idx + "-" + f.key, val = values[f.key], attrs = { idx: idx, key: f.key, type: f.type };
            if (f.type === "select-int" || f.type === "select-str") return selHtml({ id: id, field: "trigger-param", label: f.label, items: f.options, value: val != null ? val : "", blank: f.blank || null, attrs: attrs });
            if (f.type === "toggle") return '<div class="gl-v2-field"><span class="gl-v2-field-label">' + esc(f.label) + "</span>" + toggleHtml({ id: id, field: "trigger-param", checked: val != null ? !!val : !!f.default, attrs: attrs, aria: f.label }) + "</div>";
            return inpHtml({ id: id, field: "trigger-param", label: f.label, value: val != null ? val : "", placeholder: f.placeholder, type: f.type === "number" ? "number" : "text", attrs: attrs, hint: f.hint ? esc(f.hint) : "" });
        }).join("") + "</div>";
    }
    function renderParamsReference() {
        var rows = triggerActieOpties.map(function (o) {
            var schema = TRIGGER_ACTIE_PARAM_SCHEMA[o.v] || [];
            return "<tr><td>" + esc(o.n) + "</td><td>" + esc(schema.length ? schema.map(function (f) { return f.label.replace(/ \(optioneel\)$/, ""); }).join(", ") : "Geen parameters nodig.") + "</td></tr>";
        }).join("");
        return '<details class="gl-v2-tsa-ref"><summary>Naslag: welke velden gebruikt elke actie?</summary><table><thead><tr><th>Actie</th><th>Velden</th></tr></thead><tbody>' + rows + "</tbody></table></details>";
    }
    function triggerSummary(t) { return esc(optLabel(triggerEventOpties, t.triggerEvent) || "?") + " &rarr; <b>" + esc(optLabel(triggerActieOpties, t.triggerActie) || "?") + "</b>"; }
    function renderTriggerRow(m, t, idx) {
        return '<div class="gl-v2-tsa-actie"><div class="gl-v2-tsa-actie-head"><span class="gl-v2-tsa-nr" aria-hidden="true">' + (idx + 1) + '</span><span class="gl-v2-tsa-actie-sum">' + triggerSummary(t) + "</span>" +
            toggleHtml({ field: "trigger-actief", checked: t.isActief, attrs: { idx: idx }, aria: "Actie " + (idx + 1) + " actief" }) +
            '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-action="delete-trigger" data-idx="' + idx + '" title="Actie ' + (idx + 1) + ' verwijderen" aria-label="Actie ' + (idx + 1) + ' verwijderen"><i class="ph ph-x" aria-hidden="true"></i></button></div>' +
            '<div class="gl-v2-tsa-grid">' +
            selHtml({ id: "m-trig-event-" + idx, field: "trigger-event", label: "Gebeurtenis", items: triggerEventOpties, value: t.triggerEvent, attrs: { idx: idx } }) +
            selHtml({ id: "m-trig-actie-" + idx, field: "trigger-actie", label: "Actie", items: triggerActieOpties, value: t.triggerActie, attrs: { idx: idx } }) +
            (t.triggerEvent === XDAGEN_VOOR_DOELDATUM ? inpHtml({ id: "m-trig-offset-" + idx, field: "trigger-offset", label: "Dagen vooraf", type: "number", value: t.offsetDagen != null ? t.offsetDagen : "", attrs: { idx: idx } }) : "") +
            "</div>" + renderTriggerParamFields(t, idx) +
            '<div class="gl-v2-tsa-actie-foot">' + toggleHtml({ id: "m-trig-mag-" + idx, field: "trigger-magwijzigen", checked: t.magProjectWijzigen, attrs: { idx: idx }, text: "Mag het project wijzigen" }) + "</div></div>";
    }

    function sectionHead(n, title, hint, extra) {
        return '<div class="gl-v2-tsa-sec-head"><span class="gl-v2-tsa-nr" aria-hidden="true">' + n + '</span><div><h4>' + title + "</h4>" + (hint ? "<p>" + hint + "</p>" : "") + "</div>" + (extra || "") + "</div>";
    }

    function renderMijlpaalDetail(f, m) {
        var isAuto = !!m.bronBinding, dag = computeMijlpaalDag(m), bindingNonZero = bindingOpties.filter(function (o) { return o.v !== 0; });
        var html = '<div class="gl-v2-tsa-detail-head"><span class="gl-v2-tsa-detail-icon" aria-hidden="true"><i class="ph ph-flag"></i></span><div class="gl-v2-tsa-detail-titles"><h3>' + (esc(m.naam) || "(naamloze mijlpaal)") + "</h3>" +
            '<p class="gl-v2-tsa-detail-sub">' + esc(f.naam) + " · " + formatDagLabel(dag) + "</p></div>" +
            '<span class="gl-v2-tsa-detail-acts"><button type="button" class="gl-v2-row-action" data-action="duplicate-mijlpaal" title="Mijlpaal dupliceren" aria-label="Mijlpaal dupliceren"><i class="ph ph-copy" aria-hidden="true"></i></button>' +
            '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-action="delete-mijlpaal" title="Mijlpaal verwijderen" aria-label="Mijlpaal verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button></span></div>' +
            '<div class="gl-v2-tsa-detail-body">';

        html += "<section>" + sectionHead(1, "Wat is deze mijlpaal?") + '<div class="gl-v2-tsa-grid is-3">' +
            inpHtml({ id: "m-naam", field: "naam", label: "Naam", value: m.naam, placeholder: "bv. Vergunning ingediend" }) +
            inpHtml({ id: "m-code", field: "code", label: "Technische code", value: m.code, placeholder: "bv. VERGUNNING_INGEDIEND", mono: true, hintId: "m-code-hint",
                hint: 'Uniek: bindingen en triggers gebruiken deze code. <button type="button" class="gl-v2-tsa-link" data-action="derive-code">afleiden uit de naam</button>' }) +
            selHtml({ id: "m-type", field: "type", label: "Soort", items: typeOpties, value: m.mijlpaalType }) +
            selHtml({ id: "m-rol", field: "rol", label: "Verantwoordelijke rol", items: rolOpties, value: m.verantwoordelijkeRol, blank: "Geen" }) +
            '<div class="gl-v2-field"><span class="gl-v2-field-label" id="m-scope-label">Geldt voor</span><div class="gl-v2-segmented is-sm" role="group" aria-labelledby="m-scope-label">' +
            '<button type="button" data-action="set-scope" data-scope="0" aria-pressed="' + (m.scope === 0) + '" class="' + (m.scope === 0 ? "is-active" : "") + '">Heel het project</button>' +
            '<button type="button" data-action="set-scope" data-scope="1" aria-pressed="' + (m.scope === 1) + '" class="' + (m.scope === 1 ? "is-active" : "") + '">Per eenheid</button></div></div>' +
            '<div class="gl-v2-field"><span class="gl-v2-field-label">Verplicht</span>' + toggleHtml({ id: "m-verplicht", field: "verplicht", checked: m.isVerplicht, text: "Vóór de fase afgerond kan worden" }) + "</div>" +
            "</div></section>";

        html += "<section>" + sectionHead(2, "Wanneer is ze bereikt?", "bepaalt of CPM dit zelf detecteert") +
            '<div class="gl-v2-tsa-cards" role="group" aria-label="Wanneer is ze bereikt?">' +
            '<button type="button" class="gl-v2-tsa-card' + (!isAuto ? " is-active" : "") + '" data-action="set-mode" data-mode="handmatig" aria-pressed="' + !isAuto + '"><i class="ph ph-hand-pointing" aria-hidden="true"></i><span><b>Handmatig afvinken</b><em>De verantwoordelijke zet de datum zelf.</em></span></button>' +
            '<button type="button" class="gl-v2-tsa-card' + (isAuto ? " is-active" : "") + '" data-action="set-mode" data-mode="automatisch" aria-pressed="' + isAuto + '"><i class="ph ph-lightning" aria-hidden="true"></i><span><b>Automatisch uit CPM</b><em>Bereikt zodra een gegeven in het project verandert.</em></span></button></div>';
        if (isAuto) html += '<div class="gl-v2-tsa-grid">' + selHtml({ id: "m-binding", field: "binding", label: "Bron", items: bindingNonZero, value: m.bronBinding }) + paramControlHtml(m) + "</div>";
        html += "</section>";

        html += "<section>" + sectionHead(3, "Streefdatum", "bepaalt waarschuwingen en de tijdlijn") + '<div class="gl-v2-tsa-daterule">' +
            selHtml({ id: "m-anker", field: "anker", label: "Telt vanaf", items: anchorItems(m), value: m.doeldatumAnkerCode, placeholder: "Kies een anker" }) +
            inpHtml({ id: "m-offset", field: "offset", label: "+ Dagen", type: "number", value: m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : "", placeholder: "0" }) +
            '<div class="gl-v2-field"><span class="gl-v2-field-label">= Streefdatum</span><span class="gl-v2-tsa-dag" id="streefdatumBadge">' + formatDagLabel(dag) + '</span><span class="gl-v2-field-help">Voorbeeld voor een project aangemaakt op de gekozen simulatiedatum.</span></div></div></section>';

        html += "<section>" + sectionHead(4, "Acties bij deze mijlpaal", m.triggers.length ? m.triggers.length + " actie(s)" : "geen acties",
            '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-action="add-trigger"><i class="ph ph-plus" aria-hidden="true"></i>Actie</button>') +
            renderParamsReference() + (m.triggers.length ? m.triggers.map(function (t, i) { return renderTriggerRow(m, t, i); }).join("") : '<p class="gl-v2-tsa-empty">Nog geen acties bij deze mijlpaal.</p>') + "</section>";

        html += "<section>" + sectionHead(5, "Toelichting", "zichtbaar als tooltip op het project") +
            '<div class="gl-v2-field"><div class="gl-v2-field-box gl-v2-field-box-textarea"><textarea id="m-omschrijving" class="gl-v2-field-input" rows="3" data-field="omschrijving" aria-label="Toelichting" placeholder="bv. Vergunning is definitief zodra de beroepstermijn van 35 dagen verstreken is.">' + esc(m.omschrijving) + "</textarea></div></div></section>";

        detailPane.innerHTML = html + "</div>";
        initSelects(detailPane);
    }

    function renderFaseDetail(f) {
        var idx = state.fases.indexOf(f), kleur = faseKleur(f, idx);
        var html = '<div class="gl-v2-tsa-detail-head"><span class="gl-v2-tsa-detail-icon" aria-hidden="true" style="background:' + kleur + '22;color:' + kleur + '"><i class="ph ph-flag-banner"></i></span><div class="gl-v2-tsa-detail-titles"><h3>' + (esc(f.naam) || "(naamloze fase)") + "</h3>" +
            '<p class="gl-v2-tsa-detail-sub">Fase · ' + f.mijlpalen.length + ' mijlpalen</p></div><span class="gl-v2-tsa-detail-acts"><button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-action="delete-fase" title="Fase verwijderen" aria-label="Fase verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button></span></div>' +
            '<div class="gl-v2-tsa-detail-body"><section><div class="gl-v2-tsa-grid">' +
            inpHtml({ id: "f-naam", field: "fnaam", label: "Fasenaam", value: f.naam }) + inpHtml({ id: "f-code", field: "fcode", label: "Code", value: f.code, mono: true }) +
            inpHtml({ id: "f-volgorde", field: "fvolgorde", label: "Volgorde", type: "number", value: f.volgorde }) +
            inpHtml({ id: "f-status", field: "fstatus", label: "Standaard ProjectStatusId", type: "number", value: f.standaardProjectStatusId != null ? f.standaardProjectStatusId : "", hint: "Optioneel: koppelt deze fase aan een bestaande Project.StatusId.", hintId: "f-status-hint" }) +
            '</div><div class="gl-v2-field"><span class="gl-v2-field-label" id="f-kleur-label">Kleur in de fasenlijst</span><div class="gl-v2-tsa-swatches" role="group" aria-labelledby="f-kleur-label">' +
            FASE_PALET.map(function (c) { return '<button type="button" class="gl-v2-tsa-swatch' + (f.kleurCode === c ? " is-active" : "") + '" data-action="set-kleur" data-kleur="' + c + '" aria-pressed="' + (f.kleurCode === c) + '" aria-label="Kleur ' + c + '" style="background:' + c + '"></button>'; }).join("") +
            "</div></div></section></div>";
        detailPane.innerHTML = html;
    }

    function renderDetail() {
        var sel = state.selection;
        if (!sel) { detailPane.innerHTML = '<div class="gl-v2-tsa-detail-empty"><i class="ph ph-git-branch" aria-hidden="true"></i><p>Selecteer een fase of mijlpaal links,<br />of voeg een nieuwe fase toe om te beginnen.</p></div>'; return; }
        if (sel.type === "fase") { var f = findFase(sel.faseUid); if (!f) { state.selection = null; return renderDetail(); } renderFaseDetail(f); return; }
        var f2 = findFase(sel.faseUid), m = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
        if (!f2 || !m) { state.selection = null; return renderDetail(); }
        renderMijlpaalDetail(f2, m);
    }

    function patchTreeLabel(u, text) {
        var row = faseTree.querySelector('[data-mijlpaal-uid="' + u + '"] .gl-v2-tsa-m-naam');
        if (row) row.innerHTML = text ? esc(text) : '<span class="is-unnamed">(naamloze mijlpaal)</span>';
        var rowEl = faseTree.querySelector('[data-mijlpaal-uid="' + u + '"]'); if (rowEl) rowEl.setAttribute("aria-label", "Mijlpaal: " + (text || "(naamloze mijlpaal)"));
        var fr = faseTree.querySelector('[data-fase-select="' + u + '"]'); if (fr) fr.textContent = text || "(naamloze fase)";
        var title = detailPane.querySelector(".gl-v2-tsa-detail-titles h3"); if (title) title.textContent = text || (state.selection && state.selection.type === "fase" ? "(naamloze fase)" : "(naamloze mijlpaal)");
    }
    function patchStreefdatumBadge(m) {
        var badge = $("streefdatumBadge"); if (badge) badge.innerHTML = formatDagLabel(computeMijlpaalDag(m));
        var sub = detailPane.querySelector(".gl-v2-tsa-detail-sub");
        if (sub && state.selection) { var f = findFase(state.selection.faseUid); sub.innerHTML = esc(f ? f.naam : "") + " · " + formatDagLabel(computeMijlpaalDag(m)); }
        var dagEl = faseTree.querySelector('[data-mijlpaal-uid="' + m._uid + '"] .gl-v2-tsa-m-dag'); if (dagEl) { var d = computeMijlpaalDag(m); dagEl.textContent = d == null ? "?" : "dag " + d; }
    }

    // ── Delegatie op het detailpaneel ───────────────────────────────────────────────────────────
    detailPane.addEventListener("input", function (e) {
        var el = e.target, field = el.getAttribute("data-field"), sel = state.selection;
        if (!field || !sel || el.type === "hidden") return;
        if (sel.type === "fase") {
            var f = findFase(sel.faseUid); if (!f) return;
            if (field === "fnaam") { f.naam = el.value; patchTreeLabel(f._uid, f.naam); }
            else if (field === "fcode") f.code = el.value;
            else if (field === "fvolgorde") f.volgorde = parseInt(el.value, 10) || 0;
            else if (field === "fstatus") f.standaardProjectStatusId = el.value === "" ? null : parseInt(el.value, 10);
            markDirty(); return;
        }
        var m = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m) return;
        var idx = el.getAttribute("data-idx");
        switch (field) {
            case "naam": m.naam = el.value; patchTreeLabel(m._uid, m.naam); break;
            case "code": m.code = el.value; break;
            case "bronParam": m.bronParam = el.value; break;
            case "offset": m.doeldatumOffsetDagen = el.value === "" ? null : parseInt(el.value, 10); patchStreefdatumBadge(m); break;
            case "omschrijving": m.omschrijving = el.value; break;
            case "trigger-offset": m.triggers[idx].offsetDagen = el.value === "" ? null : parseInt(el.value, 10); break;
            case "trigger-param": if (el.getAttribute("data-type") !== "toggle") updateTriggerParam(m.triggers[idx], el.getAttribute("data-key"), el.value, el.getAttribute("data-type")); break;
            default: return;
        }
        markDirty(); scheduleControleRefresh();
    });

    detailPane.addEventListener("change", function (e) {
        var el = e.target, field = el.getAttribute("data-field"), sel = state.selection;
        if (!field || !sel || sel.type !== "mijlpaal") return;
        var m = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m) return;
        var idx = el.getAttribute("data-idx");
        switch (field) {
            case "type": m.mijlpaalType = parseInt(el.value, 10) || 0; break;
            case "rol": m.verantwoordelijkeRol = el.value === "" ? null : parseInt(el.value, 10); break;
            case "bronParam": m.bronParam = el.value; break;
            case "verplicht": m.isVerplicht = el.checked; break;
            case "binding": m.bronBinding = parseInt(el.value, 10) || 0; m.bronParam = ""; markDirty(); scheduleControleRefresh(); renderMijlpaalDetail(findFase(sel.faseUid), m); renderTree(); return;
            case "anker": m.doeldatumAnkerCode = el.value; patchStreefdatumBadge(m); scheduleControleRefresh(); markDirty(); return;
            case "trigger-event": m.triggers[idx].triggerEvent = parseInt(el.value, 10) || 0; markDirty(); scheduleControleRefresh(); renderMijlpaalDetail(findFase(sel.faseUid), m); return;
            case "trigger-actie": m.triggers[idx].triggerActie = parseInt(el.value, 10) || 0; markDirty(); scheduleControleRefresh(); renderMijlpaalDetail(findFase(sel.faseUid), m); return;
            case "trigger-param": updateTriggerParam(m.triggers[idx], el.getAttribute("data-key"), el.type === "checkbox" ? el.checked : el.value, el.getAttribute("data-type")); break;
            case "trigger-actief": m.triggers[idx].isActief = el.checked; break;
            case "trigger-magwijzigen": m.triggers[idx].magProjectWijzigen = el.checked; break;
            default: return;
        }
        markDirty(); scheduleControleRefresh();
    });

    detailPane.addEventListener("click", function (e) {
        var sel = state.selection; if (!sel) return;
        var b;
        if ((b = e.target.closest("[data-action='set-scope']"))) { var m1 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (m1) { m1.scope = parseInt(b.getAttribute("data-scope"), 10); markDirty(); renderMijlpaalDetail(findFase(sel.faseUid), m1); } return; }
        if ((b = e.target.closest("[data-action='set-mode']"))) {
            var m2 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m2) return;
            if (b.getAttribute("data-mode") === "handmatig") { m2.bronBinding = 0; m2.bronParam = ""; }
            else if (!m2.bronBinding) { var first = bindingOpties.filter(function (o) { return o.v !== 0; })[0]; m2.bronBinding = first ? first.v : 0; }
            markDirty(); renderMijlpaalDetail(findFase(sel.faseUid), m2); renderTree(); scheduleControleRefresh(); return;
        }
        if (e.target.closest("[data-action='derive-code']")) {
            var m3 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m3) return;
            m3.code = (m3.naam || "").toUpperCase().trim().replace(/[^A-Z0-9]+/g, "_").replace(/^_+|_+$/g, "");
            renderMijlpaalDetail(findFase(sel.faseUid), m3); markDirty(); scheduleControleRefresh(); return;
        }
        if (e.target.closest("[data-action='add-trigger']")) { var m4 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m4) return; m4.triggers.push(cloneTrigger({})); renderMijlpaalDetail(findFase(sel.faseUid), m4); markDirty(); renderTree(); return; }
        if ((b = e.target.closest("[data-action='delete-trigger']"))) {
            var m5 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!m5) return;
            m5.triggers.splice(parseInt(b.getAttribute("data-idx"), 10), 1); renderMijlpaalDetail(findFase(sel.faseUid), m5); markDirty(); renderTree(); scheduleControleRefresh(); return;
        }
        if (e.target.closest("[data-action='duplicate-mijlpaal']")) {
            var f6 = findFase(sel.faseUid), m6 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!f6 || !m6) return;
            var kopie = cloneMijlpaal(m6); kopie.naam = m6.naam ? m6.naam + " (kopie)" : ""; kopie.code = m6.code ? m6.code + "_KOPIE" : "";
            f6.mijlpalen.splice(f6.mijlpalen.indexOf(m6) + 1, 0, kopie); markDirty(); selectMijlpaal(f6._uid, kopie._uid); return;
        }
        if (e.target.closest("[data-action='delete-mijlpaal']")) {
            var f7 = findFase(sel.faseUid), m7 = findMijlpaal(sel.faseUid, sel.mijlpaalUid); if (!f7 || !m7) return;
            bevestig("Mijlpaal verwijderen?", "Mijlpaal “" + (m7.naam || "(naamloos)") + "” en " + m7.triggers.length + " bijhorende actie(s) verwijderen? Dit kan niet ongedaan gemaakt worden.", "Verwijderen").then(function (ok) {
                if (!ok) return;
                f7.mijlpalen.splice(f7.mijlpalen.indexOf(m7), 1); markDirty();
                if (f7.mijlpalen.length) selectMijlpaal(f7._uid, f7.mijlpalen[0]._uid); else selectFase(f7._uid);
                scheduleControleRefresh();
            });
            return;
        }
        if (e.target.closest("[data-action='delete-fase']")) {
            var f8 = findFase(sel.faseUid); if (!f8) return;
            bevestig("Fase verwijderen?", "Fase “" + (f8.naam || "(naamloos)") + "” met " + f8.mijlpalen.length + " mijlpalen verwijderen? Dit kan niet ongedaan gemaakt worden.", "Verwijderen").then(function (ok) {
                if (!ok) return;
                state.fases.splice(state.fases.indexOf(f8), 1); state.selection = null; markDirty(); renderTree(); renderDetail(); scheduleControleRefresh();
            });
            return;
        }
        if ((b = e.target.closest("[data-action='set-kleur']"))) { var f9 = findFase(sel.faseUid); if (!f9) return; f9.kleurCode = b.getAttribute("data-kleur"); markDirty(); renderTree(); renderFaseDetail(f9); }
    });

    // ═══ Tijdlijn & simulatie ═══════════════════════════════════════════════════════════════════
    var simStart = $("tsaSimStart");
    simStart.value = state.simStart;
    simStart.addEventListener("change", function () {
        state.simStart = simStart.value || todayIso();
        if (state.selection && state.selection.type === "mijlpaal") { var m = findMijlpaal(state.selection.faseUid, state.selection.mijlpaalUid); if (m) patchStreefdatumBadge(m); }
        renderTijdlijn();
    });
    var tlMode = $("tsaTlMode");
    tlMode.addEventListener("click", function (e) {
        var btn = e.target.closest("button[data-mode]"); if (!btn) return;
        state.tlMode = btn.getAttribute("data-mode");
        tlMode.querySelectorAll("button").forEach(function (x) { var on = x === btn; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
        renderTijdlijn();
    });
    function tlScalePct(dag, maxDag) { if (!maxDag || maxDag <= 0) return 0; return Math.sqrt(Math.min(Math.max(dag || 0, 0), maxDag) / maxDag) * 100; }
    var TL_AXIS = [0, 7, 14, 30, 60, 90, 180, 270, 365, 540, 730, 915, 1095, 1460];
    function tlTicks(maxDag) { var t = TL_AXIS.filter(function (d) { return d <= maxDag; }); if (!t.length || t[t.length - 1] < maxDag) t.push(maxDag); return t; }
    function tlAxisLabel(dag) { return state.tlMode !== "datums" ? "d" + dag : (monthFmt ? monthFmt.format(dagDatum(dag)) : dagDatum(dag).toLocaleDateString()); }
    function tlDagLabel(dag) { if (dag == null) return "onbekend"; return state.tlMode === "datums" ? (dateFmt ? dateFmt.format(dagDatum(dag)) : dagDatum(dag).toLocaleDateString()) : "dag " + dag; }

    function renderTijdlijn() {
        var host = $("tijdlijnContainer"); if (!host) return;
        if (!state.fases.length) { host.innerHTML = '<p class="gl-v2-tsa-empty">Nog geen fases om te simuleren.</p>'; return; }
        var all = [0]; state.fases.forEach(function (f) { f.mijlpalen.forEach(function (m) { var d = computeMijlpaalDag(m); all.push(d == null ? 0 : d); }); });
        var maxDag = Math.max.apply(null, all), ticks = tlTicks(maxDag);
        var axis = '<div class="gl-v2-tsa-tl-row"><div></div><div class="gl-v2-tsa-tl-axis">' + ticks.map(function (d) {
            return '<span class="gl-v2-tsa-tl-tick" style="left:' + tlScalePct(d, maxDag).toFixed(2) + '%"><span class="l" aria-hidden="true"></span><span class="t">' + esc(tlAxisLabel(d)) + "</span></span>";
        }).join("") + "</div></div>";
        var rows = state.fases.map(function (f, fi) {
            var markers = f.mijlpalen.slice().sort(function (a, b) { return (computeMijlpaalDag(a) || 0) - (computeMijlpaalDag(b) || 0); }), last = null;
            var mh = markers.map(function (m) {
                var raw = computeMijlpaalDag(m), onb = raw == null, dag = onb ? 0 : raw, pct = tlScalePct(dag, maxDag);
                var show = last == null || Math.abs(pct - last) >= 5; if (show) last = pct;
                var cls = "gl-v2-tsa-tl-marker" + (onb ? " is-onbekend" : m.bronBinding ? " is-auto" : " is-handmatig") + (show ? "" : " is-tick-only");
                return '<div class="' + cls + '" style="left:' + pct.toFixed(2) + '%" title="' + esc((m.naam || "(naamloze mijlpaal)") + (onb ? " (geen geldig anker, teruggevallen op projectstart)" : "")) + '"><span class="d" aria-hidden="true"></span>' +
                    (m.triggers.length ? '<span class="a" aria-hidden="true"></span>' : "") +
                    (show ? '<span class="lb"><span class="n">' + esc(m.naam || "(naamloos)") + '</span><span class="g">' + esc(tlDagLabel(onb ? null : dag)) + "</span></span>" : "") + "</div>";
            }).join("");
            return '<div class="gl-v2-tsa-tl-row"><div class="gl-v2-tsa-tl-label"><span class="n">' + esc(f.naam || "(naamloze fase)") + '</span><span class="m">' + (f.mijlpalen.length === 1 ? "1 mijlpaal" : f.mijlpalen.length + " mijlpalen") + (f.code ? " · " + esc(f.code) : "") + "</span></div>" +
                '<div class="gl-v2-tsa-tl-track"><span class="line" style="background:' + faseKleur(f, fi) + '33" aria-hidden="true"></span>' + mh + "</div></div>";
        }).join("");
        host.innerHTML = '<div class="gl-v2-tsa-tl">' + rows + axis + '</div><div class="gl-v2-tsa-tl-legend"><span><i class="d is-auto"></i>automatisch bereikt</span><span><i class="d is-handmatig"></i>handmatig aan te vinken</span><span><i class="a"></i>heeft acties</span><span><i class="d is-onbekend"></i>anker onbekend, valt terug op projectstart</span></div>';
    }

    // ═══ Controle ═══════════════════════════════════════════════════════════════════════════════
    function computeIssues() {
        var issues = [];
        if (!state.fases.length) { issues.push({ severity: "urgent", text: "Dit sjabloon heeft nog geen fases." }); return issues; }
        var codeMap = {};
        state.fases.forEach(function (f) {
            if (!f.naam.trim()) issues.push({ severity: "urgent", text: "Een fase heeft nog geen naam.", faseUid: f._uid });
            if (!f.code.trim()) issues.push({ severity: "normal", text: 'Fase "' + (f.naam || "?") + '" heeft geen code.', faseUid: f._uid });
            if (!f.mijlpalen.length) issues.push({ severity: "normal", text: 'Fase "' + (f.naam || "?") + '" heeft nog geen mijlpalen.', faseUid: f._uid });
            f.mijlpalen.forEach(function (m) {
                var loc = f.naam + " · " + (m.naam || "(naamloos)"), ref = { loc: loc, faseUid: f._uid, mijlpaalUid: m._uid };
                function add(sev, text) { issues.push({ severity: sev, text: text, loc: ref.loc, faseUid: ref.faseUid, mijlpaalUid: ref.mijlpaalUid }); }
                if (!m.naam.trim()) add("urgent", 'Een mijlpaal in "' + f.naam + '" heeft nog geen naam.');
                if (!m.code.trim()) add("urgent", '"' + (m.naam || "(naamloos)") + '" heeft geen technische code en kan niet als anker gebruikt worden.');
                else { var key = m.code.trim().toLowerCase(); (codeMap[key] = codeMap[key] || []).push({ faseUid: f._uid, mijlpaalUid: m._uid, loc: loc }); }
                var anker = m.doeldatumAnkerCode;
                if (anker && anker !== PROJECT_CREATED) {
                    if (!findMijlpaalByCode(anker)) add("urgent", '"' + (m.naam || "?") + '" verwijst naar een onbekende ankercode "' + anker + '".');
                    else if (computeMijlpaalDag(m) == null) add("urgent", '"' + (m.naam || "?") + '" zit in een cirkelverwijzing van ankers.');
                }
                if (m.bronBinding && bronParamModeByBinding[m.bronBinding] === "select" && !m.bronParam) add("normal", '"' + (m.naam || "?") + '" heeft een automatische bron gekozen maar geen bron-parameter.');
                (m.triggers || []).forEach(function (t, ti) {
                    if (t.triggerEvent === XDAGEN_VOOR_DOELDATUM && (t.offsetDagen == null || t.offsetDagen === "")) add("normal", "Actie " + (ti + 1) + ' op "' + (m.naam || "?") + '" ("X dagen voor streefdatum") heeft geen aantal dagen ingevuld.');
                    if (t.actieParametersJson) { try { JSON.parse(t.actieParametersJson); } catch (x) { add("normal", "Actie " + (ti + 1) + ' op "' + (m.naam || "?") + '" heeft ongeldige parameters.'); } }
                    var vals = paramsToObject(t.actieParametersJson);
                    (TRIGGER_ACTIE_PARAM_SCHEMA[t.triggerActie] || []).filter(function (fld) { return fld.required; }).forEach(function (fld) {
                        if (vals[fld.key] == null || vals[fld.key] === "") add("urgent", "Actie " + (ti + 1) + ' op "' + (m.naam || "?") + '" mist een verplicht veld: ' + fld.label.replace(/ \(optioneel\)$/, "") + ".");
                    });
                });
            });
        });
        Object.keys(codeMap).forEach(function (k) {
            if (codeMap[k].length > 1) codeMap[k].forEach(function (x) { issues.push({ severity: "urgent", text: 'Technische code "' + k.toUpperCase() + '" komt meermaals voor: ankers en triggers kunnen de verkeerde mijlpaal raken.', loc: x.loc, faseUid: x.faseUid, mijlpaalUid: x.mijlpaalUid }); });
        });
        return issues;
    }
    var SEV = { urgent: { title: "Op te lossen", icon: "ph-warning", tone: "is-danger" }, normal: { title: "Aandachtspunt", icon: "ph-warning-circle", tone: "is-warning" }, info: { title: "Ter info", icon: "ph-info", tone: "is-info" } };
    function renderControle() {
        var host = $("controleContainer"), badge = $("tsaControleBadge"); if (!host) return;
        var issues = computeIssues();
        if (badge) { var c = issues.filter(function (i) { return i.severity !== "info"; }).length; badge.textContent = String(c); badge.hidden = c === 0; }
        if (!issues.length) { host.innerHTML = '<div class="gl-v2-tsa-ok"><i class="ph ph-check-circle" aria-hidden="true"></i><p>Alles in orde. Geen aandachtspunten gevonden.</p></div>'; return; }
        host.innerHTML = ["urgent", "normal", "info"].map(function (sev) {
            var list = issues.filter(function (i) { return i.severity === sev; }); if (!list.length) return "";
            return '<div class="gl-v2-tsa-issues"><div class="gl-v2-tsa-issues-title">' + SEV[sev].title + " (" + list.length + ")</div>" + list.map(function (i) {
                var jump = !!(i.mijlpaalUid || i.faseUid), attrs = i.mijlpaalUid ? ' data-jump-fase="' + i.faseUid + '" data-jump-mijlpaal="' + i.mijlpaalUid + '"' : (i.faseUid ? ' data-jump-fase="' + i.faseUid + '"' : "");
                return "<" + (jump ? 'button type="button"' : "div") + ' class="gl-v2-tsa-issue ' + SEV[sev].tone + '"' + attrs + '><i class="ph ' + SEV[sev].icon + '" aria-hidden="true"></i><span>' + esc(i.text) + (i.loc ? '<span class="loc">' + esc(i.loc) + "</span>" : "") + "</span></" + (jump ? "button" : "div") + ">";
            }).join("") + "</div>";
        }).join("");
    }
    $("controleContainer").addEventListener("click", function (e) {
        var target = e.target.closest("[data-jump-fase]"); if (!target) return;
        var fu = target.getAttribute("data-jump-fase"), mu = target.getAttribute("data-jump-mijlpaal"), f = findFase(fu); if (!f) return;
        f.expanded = true; activate("structuur");
        if (mu) selectMijlpaal(fu, mu, true); else selectFase(fu, true);
    });
    var scheduleControleRefresh = debounce(renderControle, 250);

    // ═══ Tabs ═══════════════════════════════════════════════════════════════════════════════════
    var tabs = Array.prototype.slice.call(document.querySelectorAll("#tsaTabs [data-tab]"));
    function activate(name) {
        tabs.forEach(function (t) { var on = t.dataset.tab === name; t.classList.toggle("is-active", on); t.setAttribute("aria-selected", on ? "true" : "false"); t.tabIndex = on ? 0 : -1; var p = $("tab-" + t.dataset.tab); if (p) p.hidden = !on; });
        if (name === "tijdlijn") renderTijdlijn();
        if (name === "controle") renderControle();
    }
    tabs.forEach(function (t) {
        t.addEventListener("click", function () { activate(t.dataset.tab); });
        t.addEventListener("keydown", function (e) {
            var i = tabs.indexOf(t), next = null;
            if (e.key === "ArrowRight") next = tabs[(i + 1) % tabs.length]; else if (e.key === "ArrowLeft") next = tabs[(i - 1 + tabs.length) % tabs.length];
            if (next) { e.preventDefault(); next.focus(); activate(next.dataset.tab); }
        });
    });

    // ═══ Sjabloonvelden, dupliceren, opslaan ════════════════════════════════════════════════════
    ["sj-naam", "sj-type", "sj-omschrijving", "sj-standaard", "sj-actief"].forEach(function (id) { var el = $(id); if (el) { el.addEventListener("input", markDirty); el.addEventListener("change", markDirty); } });
    $("sj-naam").addEventListener("input", function () { if (!this.value.trim()) return; var er = $("sj-naam-error"); if (er) er.hidden = true; });

    var dupForm = $("duplicerenForm");
    if (dupForm) dupForm.addEventListener("submit", function (e) {
        if (dupForm.dataset.confirmed) return;
        e.preventDefault();
        window.GlV2Bevestig({ title: "Sjabloon dupliceren?", desc: "Dit sjabloon dupliceren naar een nieuwe kopie? Niet-opgeslagen wijzigingen komen niet mee.", ok: "Dupliceren", tone: "warning" }).then(function (ok) {
            if (!ok) return; dupForm.dataset.confirmed = "1"; state.dirty = false; if (dupForm.requestSubmit) dupForm.requestSubmit(); else dupForm.submit();
        });
    });
    window.addEventListener("beforeunload", function (e) { if (!state.dirty) return; e.preventDefault(); e.returnValue = ""; });

    $("sjabloonForm").addEventListener("submit", function (e) {
        var naamEl = $("sj-naam"), err = $("sj-naam-error");
        if (!naamEl.value.trim()) { e.preventDefault(); if (err) err.hidden = false; activate("structuur"); naamEl.focus(); return; }
        if (err) err.hidden = true;
        var payload = {
            id: initial.id || null, naam: naamEl.value.trim(), projectType: $("sj-type").value === "" ? null : parseInt($("sj-type").value, 10),
            isStandaard: $("sj-standaard").checked, isActief: $("sj-actief").checked, omschrijving: $("sj-omschrijving").value,
            fases: state.fases.filter(function (f) { return f.naam.trim(); }).map(function (f, fi) {
                return { naam: f.naam.trim(), code: f.code.trim() || f.naam.toUpperCase().replace(/[^A-Z0-9]+/g, "_"), volgorde: f.volgorde || (fi + 1) * 10, kleurCode: f.kleurCode, standaardProjectStatusId: f.standaardProjectStatusId,
                    mijlpalen: f.mijlpalen.filter(function (m) { return m.naam.trim(); }).map(function (m, mi) {
                        return { naam: m.naam.trim(), code: m.code.trim(), volgorde: (mi + 1) * 10, mijlpaalType: m.mijlpaalType, scope: m.scope, verantwoordelijkeRol: m.verantwoordelijkeRol,
                            doeldatumAnkerCode: (m.doeldatumAnkerCode && m.doeldatumAnkerCode !== PROJECT_CREATED) ? m.doeldatumAnkerCode.trim() : PROJECT_CREATED, doeldatumOffsetDagen: m.doeldatumOffsetDagen, isVerplicht: m.isVerplicht,
                            bronBinding: m.bronBinding || null, bronParam: m.bronParam || null, dossierKind: m.dossierKind, omschrijving: m.omschrijving || null,
                            triggers: m.triggers.map(function (t) { return { triggerEvent: t.triggerEvent, triggerActie: t.triggerActie, offsetDagen: t.offsetDagen, actieParametersJson: t.actieParametersJson || null, magProjectWijzigen: t.magProjectWijzigen, isActief: t.isActief, omschrijving: t.omschrijving || null }; }) };
                    }) };
            })
        };
        $("payloadJson").value = JSON.stringify(payload);
        state.dirty = false;
    });

    renderTree(); renderDetail(); renderControle();
})();
