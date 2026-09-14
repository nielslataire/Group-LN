(function () {
    "use strict";

    var PROJECT_CREATED = "PROJECT_CREATED";
    var XDAGEN_VOOR_DOELDATUM = 2; // BOCore.TriggerEvent — enige event dat een offset-dagen-waarde gebruikt

    var FASE_PALET = ["#0a5a3b", "#c17d1f", "#7A8450", "#8B6B4A", "#0f7a52", "#8A7967"];

    function readJson(id, fallback) {
        var el = document.getElementById(id);
        if (!el) return fallback;
        try { return JSON.parse(el.textContent); } catch (e) { return fallback; }
    }

    function debounce(fn, wait) {
        var t;
        return function () {
            clearTimeout(t);
            var args = arguments;
            t = setTimeout(function () { fn.apply(null, args); }, wait);
        };
    }

    /** Vrij ingetypte tekst kan naar innerHTML — altijd escapen. */
    function escapeHtml(s) {
        return String(s == null ? "" : s)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    var uidSeq = 0;
    function uid() { return "u" + (++uidSeq); }

    var initial = readJson("sjabloonData", { fases: [] });
    var typeOpties = readJson("typeOpties", []);
    var rolOpties = readJson("rolOpties", []);
    var bindingOpties = readJson("bindingOpties", [{ v: 0, n: "Handmatig" }]);
    var triggerEventOpties = readJson("triggerEventOpties", []);
    var triggerActieOpties = readJson("triggerActieOpties", []);
    var bronParamOptiesByBinding = readJson("bronParamOptiesByBinding", {});
    var bronParamModeByBinding = readJson("bronParamModeByBinding", {});
    var bronParamHintByBinding = readJson("bronParamHintByBinding", {});

    var faseTree = document.getElementById("faseTree");
    var detailPane = document.getElementById("detailPane");
    if (!faseTree || !detailPane) return;

    var hasSelect2 = typeof window.jQuery === "function" && !!window.jQuery.fn.select2;
    var hasSortable = typeof window.jQuery === "function" && !!window.jQuery.fn.sortable;

    function optLabel(opties, v) {
        for (var i = 0; i < opties.length; i++) { if (opties[i].v === v) return opties[i].n; }
        return null;
    }

    function todayIso() {
        var d = new Date();
        var mm = String(d.getMonth() + 1).padStart(2, "0");
        var dd = String(d.getDate()).padStart(2, "0");
        return d.getFullYear() + "-" + mm + "-" + dd;
    }

    // ── State ────────────────────────────────────────────────────────────────

    function cloneTrigger(t) {
        return {
            _uid: uid(),
            triggerEvent: t.triggerEvent || 0,
            triggerActie: t.triggerActie || 0,
            offsetDagen: t.offsetDagen != null ? t.offsetDagen : null,
            actieParametersJson: t.actieParametersJson || "",
            magProjectWijzigen: !!t.magProjectWijzigen,
            isActief: t.isActief !== false,
            omschrijving: t.omschrijving || ""
        };
    }

    function cloneMijlpaal(m) {
        return {
            _uid: uid(),
            naam: m.naam || "",
            code: m.code || "",
            mijlpaalType: m.mijlpaalType || 0,
            scope: m.scope || 0,
            verantwoordelijkeRol: m.verantwoordelijkeRol != null ? m.verantwoordelijkeRol : null,
            doeldatumAnkerCode: m.doeldatumAnkerCode || PROJECT_CREATED,
            doeldatumOffsetDagen: m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : null,
            isVerplicht: m.isVerplicht !== false,
            bronBinding: m.bronBinding != null ? m.bronBinding : 0,
            bronParam: m.bronParam || "",
            dossierKind: m.dossierKind != null ? m.dossierKind : null,
            omschrijving: m.omschrijving || "",
            triggers: (m.triggers || []).map(cloneTrigger)
        };
    }

    function cloneFase(f, idx) {
        return {
            _uid: uid(),
            naam: f.naam || "",
            code: f.code || "",
            volgorde: f.volgorde || (idx + 1) * 10,
            kleurCode: f.kleurCode || null,
            standaardProjectStatusId: f.standaardProjectStatusId != null ? f.standaardProjectStatusId : null,
            expanded: idx === 0,
            mijlpalen: (f.mijlpalen || []).map(cloneMijlpaal)
        };
    }

    var state = {
        fases: (initial.fases || []).map(cloneFase),
        selection: null,
        simStart: todayIso(),
        dirty: false
    };
    if (state.fases.length && state.fases[0].mijlpalen.length) {
        state.selection = { type: "mijlpaal", faseUid: state.fases[0]._uid, mijlpaalUid: state.fases[0].mijlpalen[0]._uid };
    }

    function findFase(faseUid) {
        for (var i = 0; i < state.fases.length; i++) { if (state.fases[i]._uid === faseUid) return state.fases[i]; }
        return null;
    }
    function findMijlpaal(faseUid, mijlpaalUid) {
        var f = findFase(faseUid);
        if (!f) return null;
        for (var i = 0; i < f.mijlpalen.length; i++) { if (f.mijlpalen[i]._uid === mijlpaalUid) return f.mijlpalen[i]; }
        return null;
    }
    function findMijlpaalByCode(code) {
        if (!code) return null;
        var needle = code.trim().toLowerCase();
        for (var i = 0; i < state.fases.length; i++) {
            var ms = state.fases[i].mijlpalen;
            for (var j = 0; j < ms.length; j++) {
                if ((ms[j].code || "").trim().toLowerCase() === needle) return ms[j];
            }
        }
        return null;
    }

    function markDirty() {
        if (state.dirty) return;
        state.dirty = true;
        var el = document.getElementById("tsaDirty");
        if (el) el.hidden = false;
    }

    // ── Streefdag-berekening (anker + offset-keten) ─────────────────────────

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

    function computeMijlpaalDag(m) {
        var basis = resolveDag(m.doeldatumAnkerCode, []);
        if (basis == null) return null;
        return basis + (m.doeldatumOffsetDagen || 0);
    }

    var dateFmt = (typeof Intl !== "undefined")
        ? new Intl.DateTimeFormat("nl-BE", { day: "2-digit", month: "short", year: "numeric" })
        : null;

    function formatDagLabel(dag) {
        if (dag == null) return '<span class="gl-tsa-tl-onbekend">⚠ streefdag onbekend (ankerketen)</span>';
        var start = new Date(state.simStart + "T00:00:00");
        var d = new Date(start.getTime() + dag * 86400000);
        var datumTxt = dateFmt ? dateFmt.format(d) : d.toLocaleDateString();
        return "dag " + dag + " · " + escapeHtml(datumTxt);
    }

    // ── Select2 helper ───────────────────────────────────────────────────────

    function select2ify(el) {
        if (!hasSelect2 || !el) return;
        var $el = window.jQuery(el);
        if ($el.data("select2")) $el.select2("destroy");
        $el.select2({ theme: "bootstrap", width: "100%", language: "nl", dropdownAutoWidth: false, minimumResultsForSearch: 8 });
    }

    /** Select2 hangt zijn dropdown/event-handlers los van de DOM (append aan <body>) — bij een
     * innerHTML-vervanging van het paneel moet dat eerst netjes afgebroken worden, anders blijven
     * er weesreferenties naar de verdwenen <select> hangen en crasht de volgende interactie
     * ("Cannot read properties of undefined (reading 'apply')" in select2.min.js). */
    function destroySelect2Within(host) {
        if (!hasSelect2 || !host) return;
        var $ = window.jQuery;
        host.querySelectorAll('[data-select2="1"]').forEach(function (el) {
            var $el = $(el);
            if ($el.data("select2")) { try { $el.select2("destroy"); } catch (e) { /* al opgeruimd */ } }
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Boom (links): fases + mijlpalen
    // ═══════════════════════════════════════════════════════════════════════

    function faseKleur(f, idx) { return f.kleurCode || FASE_PALET[idx % FASE_PALET.length]; }

    /** Platte, zichtbare navigatievolgorde (ingeklapte fases tellen hun mijlpalen niet mee) —
     * drager van zowel het toetsenbord-rovende-tabindex-model als Alt+pijltjes-verplaatsen. */
    function getNavItems() {
        var out = [];
        state.fases.forEach(function (f) {
            out.push({ type: "fase", faseUid: f._uid });
            if (f.expanded) {
                f.mijlpalen.forEach(function (m) { out.push({ type: "mijlpaal", faseUid: f._uid, mijlpaalUid: m._uid }); });
            }
        });
        return out;
    }

    function navItemRow(item) {
        return item.type === "fase"
            ? faseTree.querySelector('.gl-tsa-fase-head[data-fase-uid="' + item.faseUid + '"]')
            : faseTree.querySelector('.gl-tsa-mijlpaal-row[data-mijlpaal-uid="' + item.mijlpaalUid + '"]');
    }

    function navItemsEqual(a, b) {
        if (!a || !b || a.type !== b.type) return false;
        return a.type === "fase" ? a.faseUid === b.faseUid : a.mijlpaalUid === b.mijlpaalUid;
    }

    /** Precies één rij in de boom is een tab-stop (die van de huidige selectie); de rest -1.
     * Zelfde patroon als de tabstrip hierboven in Edit.cshtml — voorkomt tientallen tab-stops
     * voor een boom met veel fases/mijlpalen terwijl toch alles met pijltjestoetsen bereikbaar blijft. */
    function updateRovingTabindex() {
        var items = getNavItems();
        if (!items.length) return;
        var activeIdx = state.selection ? items.findIndex(function (it) { return navItemsEqual(it, state.selection); }) : -1;
        if (activeIdx === -1) activeIdx = 0;
        items.forEach(function (it, i) {
            var row = navItemRow(it);
            if (row) row.tabIndex = i === activeIdx ? 0 : -1;
        });
    }

    function focusNavItemDom(item) {
        var row = navItemRow(item);
        if (row) row.focus();
    }

    function renderTree() {
        var html = state.fases.map(function (f, fi) {
            var isFaseSel = state.selection && state.selection.type === "fase" && state.selection.faseUid === f._uid;
            var mijlpalenHtml = f.mijlpalen.map(function (m) {
                var isSel = state.selection && state.selection.type === "mijlpaal" && state.selection.mijlpaalUid === m._uid;
                var naamHtml = m.naam ? escapeHtml(m.naam) : '<span class="is-unnamed">(naamloze mijlpaal)</span>';
                var icon = m.bronBinding ? '<i class="bx bx-bolt-circle gl-tsa-m-icon" aria-hidden="true" title="Automatische bron"></i>' : '<i class="bx bx-hand gl-tsa-m-icon" aria-hidden="true" title="Handmatig afvinken"></i>';
                var badge = (m.triggers && m.triggers.length) ? '<span class="gl-tsa-m-badge" aria-hidden="true">' + m.triggers.length + '</span>' : "";
                var naamVoorLabel = m.naam || "(naamloze mijlpaal)";
                return '<li class="gl-tsa-mijlpaal-row' + (isSel ? " is-selected" : "") + '" data-mijlpaal-uid="' + m._uid + '" data-fase-uid="' + f._uid + '"' +
                    ' role="button" tabindex="-1" aria-current="' + (isSel ? "true" : "false") + '" aria-label="Mijlpaal: ' + escapeHtml(naamVoorLabel) + (m.triggers.length ? ", " + m.triggers.length + " actie(s)" : "") + '">' +
                    '<i class="bx bx-dots-vertical-rounded gl-tsa-m-drag" aria-hidden="true"></i>' +
                    icon +
                    '<span class="gl-tsa-m-naam">' + naamHtml + '</span>' +
                    badge +
                    "</li>";
            }).join("");

            var faseLabel = f.naam || "(naamloze fase)";
            return '<div class="gl-tsa-fase' + (f.expanded ? "" : " is-collapsed") + '" data-fase-uid="' + f._uid + '">' +
                '<div class="gl-tsa-fase-head' + (isFaseSel ? " is-selected" : "") + '" data-fase-uid="' + f._uid + '"' +
                ' role="button" tabindex="-1" aria-current="' + (isFaseSel ? "true" : "false") + '" aria-label="Fase: ' + escapeHtml(faseLabel) + '">' +
                '<i class="bx bx-dots-vertical-rounded gl-tsa-fase-drag" aria-hidden="true"></i>' +
                '<span class="gl-tsa-fase-dot" style="background:' + faseKleur(f, fi) + '" aria-hidden="true"></span>' +
                '<span class="gl-tsa-fase-naam" data-fase-select="' + f._uid + '">' + (escapeHtml(f.naam) || "(naamloze fase)") + "</span>" +
                (f.code ? '<span class="gl-tsa-fase-badge">' + escapeHtml(f.code) + "</span>" : "") +
                '<button type="button" class="gl-tsa-fase-chevron" data-fase-toggle="' + f._uid + '" aria-expanded="' + (f.expanded ? "true" : "false") + '" aria-label="Fase ' + escapeHtml(faseLabel) + (f.expanded ? " inklappen" : " uitklappen") + '"><i class="bx bx-chevron-down" aria-hidden="true"></i></button>' +
                "</div>" +
                '<ul class="gl-tsa-mijlpalen" data-fase-uid="' + f._uid + '">' + mijlpalenHtml + "</ul>" +
                '<button type="button" class="gl-tsa-add-mijlpaal" data-add-mijlpaal="' + f._uid + '"><i class="bx bx-plus" aria-hidden="true"></i> Mijlpaal toevoegen</button>' +
                "</div>";
        }).join("");

        faseTree.innerHTML = html || '<p class="gl-tsa-empty-hint">Nog geen fases — voeg er hieronder een toe.</p>';
        initSortables();
        updateRovingTabindex();
    }

    function initSortables() {
        if (!hasSortable) return;
        var $ = window.jQuery;
        $(faseTree).sortable({
            items: "> .gl-tsa-fase", handle: ".gl-tsa-fase-drag", axis: "y",
            placeholder: "gl-tsa-fase-placeholder", forcePlaceholderSize: true,
            update: function () {
                var order = $(faseTree).children(".gl-tsa-fase").map(function () { return this.getAttribute("data-fase-uid"); }).get();
                state.fases.sort(function (a, b) { return order.indexOf(a._uid) - order.indexOf(b._uid); });
                state.fases.forEach(function (f, i) { f.volgorde = (i + 1) * 10; });
                markDirty();
                updateRovingTabindex();
            }
        });
        faseTree.querySelectorAll(".gl-tsa-mijlpalen").forEach(function (ul) {
            $(ul).sortable({
                items: "> .gl-tsa-mijlpaal-row", handle: ".gl-tsa-m-drag", axis: "y",
                placeholder: "gl-tsa-mijlpaal-placeholder", forcePlaceholderSize: true,
                update: function () {
                    var faseUid = ul.getAttribute("data-fase-uid");
                    var f = findFase(faseUid);
                    if (!f) return;
                    var order = $(ul).children(".gl-tsa-mijlpaal-row").map(function () { return this.getAttribute("data-mijlpaal-uid"); }).get();
                    f.mijlpalen.sort(function (a, b) { return order.indexOf(a._uid) - order.indexOf(b._uid); });
                    f.mijlpalen.forEach(function (m, i) { m.volgorde = (i + 1) * 10; });
                    markDirty();
                    updateRovingTabindex();
                }
            });
        });
    }

    function selectFase(faseUid, focusAfter) {
        state.selection = { type: "fase", faseUid: faseUid };
        renderTree();
        renderDetail();
        if (focusAfter) focusNavItemDom({ type: "fase", faseUid: faseUid });
    }

    function selectMijlpaal(faseUid, mijlpaalUid, focusAfter) {
        state.selection = { type: "mijlpaal", faseUid: faseUid, mijlpaalUid: mijlpaalUid };
        renderTree();
        renderDetail();
        if (focusAfter) focusNavItemDom({ type: "mijlpaal", faseUid: faseUid, mijlpaalUid: mijlpaalUid });
    }

    /** Verplaatst een fase of mijlpaal één plaats op in zijn eigen lijst — het toetsenbord-pad
     * naast slepen (Alt+pijl-omhoog/omlaag terwijl een boomrij focus heeft), zodat herordenen niet
     * drag-only is (zie ook de "Rangschikken"-precedent in dashboard-projectleider.css). */
    function moveItem(item, dir) {
        if (item.type === "fase") {
            var idx = state.fases.findIndex(function (f) { return f._uid === item.faseUid; });
            var newIdx = idx + dir;
            if (idx === -1 || newIdx < 0 || newIdx >= state.fases.length) return;
            var tmp = state.fases[idx]; state.fases[idx] = state.fases[newIdx]; state.fases[newIdx] = tmp;
            state.fases.forEach(function (f, i) { f.volgorde = (i + 1) * 10; });
        } else {
            var f = findFase(item.faseUid);
            if (!f) return;
            var idx2 = f.mijlpalen.findIndex(function (m) { return m._uid === item.mijlpaalUid; });
            var newIdx2 = idx2 + dir;
            if (idx2 === -1 || newIdx2 < 0 || newIdx2 >= f.mijlpalen.length) return;
            var tmp2 = f.mijlpalen[idx2]; f.mijlpalen[idx2] = f.mijlpalen[newIdx2]; f.mijlpalen[newIdx2] = tmp2;
            f.mijlpalen.forEach(function (m, i) { m.volgorde = (i + 1) * 10; });
        }
        markDirty();
        renderTree();
        focusNavItemDom(item);
    }

    faseTree.addEventListener("click", function (e) {
        var toggle = e.target.closest("[data-fase-toggle]");
        if (toggle) {
            var f1 = findFase(toggle.getAttribute("data-fase-toggle"));
            if (f1) { f1.expanded = !f1.expanded; renderTree(); }
            return;
        }
        var addBtn = e.target.closest("[data-add-mijlpaal]");
        if (addBtn) {
            var f2 = findFase(addBtn.getAttribute("data-add-mijlpaal"));
            if (!f2) return;
            var nieuw = cloneMijlpaal({});
            f2.mijlpalen.push(nieuw);
            f2.expanded = true;
            markDirty();
            selectMijlpaal(f2._uid, nieuw._uid, true);
            return;
        }
        var mRow = e.target.closest("[data-mijlpaal-uid]");
        if (mRow) { selectMijlpaal(mRow.getAttribute("data-fase-uid"), mRow.getAttribute("data-mijlpaal-uid"), true); return; }
        var fHead = e.target.closest(".gl-tsa-fase-head");
        if (fHead) { selectFase(fHead.getAttribute("data-fase-uid"), true); }
    });

    /** Toetsenbordbediening van de boom: rovende tabindex + pijltjes (net als de tabstrip
     * hierboven), Enter/Spatie selecteert, Links/Rechts klapt een fase in/uit, Alt+omhoog/omlaag
     * verplaatst de gefocuste rij. Knoppen in een rij (chevron, "Mijlpaal toevoegen") hebben hun
     * eigen, natieve toetsenbordgedrag — dit hier bemoeit zich daar niet mee. */
    faseTree.addEventListener("keydown", function (e) {
        if (e.target.closest("button")) return;
        var row = e.target.closest(".gl-tsa-fase-head, .gl-tsa-mijlpaal-row");
        if (!row) return;

        var items = getNavItems();
        var idx = items.findIndex(function (it) { return navItemRow(it) === row; });
        if (idx === -1) return;

        if (e.altKey && (e.key === "ArrowUp" || e.key === "ArrowDown")) {
            e.preventDefault();
            moveItem(items[idx], e.key === "ArrowUp" ? -1 : 1);
            return;
        }
        if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            if (items[idx].type === "fase") selectFase(items[idx].faseUid, true);
            else selectMijlpaal(items[idx].faseUid, items[idx].mijlpaalUid, true);
            return;
        }
        if (e.key === "ArrowDown") { e.preventDefault(); var next = items[Math.min(idx + 1, items.length - 1)]; if (next.type === "fase") selectFase(next.faseUid, true); else selectMijlpaal(next.faseUid, next.mijlpaalUid, true); return; }
        if (e.key === "ArrowUp") { e.preventDefault(); var prev = items[Math.max(idx - 1, 0)]; if (prev.type === "fase") selectFase(prev.faseUid, true); else selectMijlpaal(prev.faseUid, prev.mijlpaalUid, true); return; }
        if (e.key === "Home") { e.preventDefault(); var first = items[0]; if (first.type === "fase") selectFase(first.faseUid, true); else selectMijlpaal(first.faseUid, first.mijlpaalUid, true); return; }
        if (e.key === "End") { e.preventDefault(); var last = items[items.length - 1]; if (last.type === "fase") selectFase(last.faseUid, true); else selectMijlpaal(last.faseUid, last.mijlpaalUid, true); return; }
        if (e.key === "ArrowRight") {
            e.preventDefault();
            if (items[idx].type === "fase") {
                var f3 = findFase(items[idx].faseUid);
                if (f3 && !f3.expanded) { f3.expanded = true; renderTree(); focusNavItemDom(items[idx]); }
                else if (f3 && f3.mijlpalen.length) { selectMijlpaal(f3._uid, f3.mijlpalen[0]._uid, true); }
            }
            return;
        }
        if (e.key === "ArrowLeft") {
            e.preventDefault();
            if (items[idx].type === "fase") {
                var f4 = findFase(items[idx].faseUid);
                if (f4 && f4.expanded) { f4.expanded = false; renderTree(); focusNavItemDom(items[idx]); }
            } else {
                selectFase(items[idx].faseUid, true);
            }
        }
    });

    document.getElementById("btnAddFase").addEventListener("click", function () {
        var f = cloneFase({ naam: "" }, state.fases.length);
        f.expanded = true;
        state.fases.push(f);
        markDirty();
        selectFase(f._uid);
    });

    document.getElementById("btnToggleAll").addEventListener("click", function (e) {
        var allOpen = state.fases.every(function (f) { return f.expanded; });
        state.fases.forEach(function (f) { f.expanded = !allOpen; });
        e.target.textContent = allOpen ? "Alles openen" : "Alles sluiten";
        renderTree();
    });

    // ═══════════════════════════════════════════════════════════════════════
    // Detailpaneel (rechts)
    // ═══════════════════════════════════════════════════════════════════════

    function optionsHtml(opties, selected, blankLabel) {
        var html = blankLabel != null ? '<option value=""' + (selected == null ? " selected" : "") + ">" + escapeHtml(blankLabel) + "</option>" : "";
        html += opties.map(function (o) {
            return '<option value="' + o.v + '"' + (o.v === selected ? " selected" : "") + ">" + escapeHtml(o.n) + "</option>";
        }).join("");
        return html;
    }

    function anchorOptionsHtml(self) {
        var html = '<option value="' + PROJECT_CREATED + '"' + (self.doeldatumAnkerCode === PROJECT_CREATED ? " selected" : "") + '>Bij aanmaken van het project</option>';
        var known = {};
        state.fases.forEach(function (f) {
            f.mijlpalen.forEach(function (m) {
                if (m._uid === self._uid || !m.code.trim()) return;
                var label = (f.naam ? f.naam + " · " : "") + (m.naam || "(naamloze mijlpaal)") + " (" + m.code + ")";
                html += '<option value="' + escapeHtml(m.code) + '"' + (m.code === self.doeldatumAnkerCode ? " selected" : "") + ">" + escapeHtml(label) + "</option>";
                known[m.code.trim().toLowerCase()] = true;
            });
        });
        var saved = self.doeldatumAnkerCode;
        if (saved && saved !== PROJECT_CREATED && !known[saved.trim().toLowerCase()]) {
            html = '<option value="' + escapeHtml(saved) + '" selected>⚠ Onbekende mijlpaal-code: ' + escapeHtml(saved) + "</option>" + html;
        }
        return html;
    }

    function paramControlHtml(m) {
        var mode = bronParamModeByBinding[m.bronBinding] || (bronParamOptiesByBinding[m.bronBinding] ? "select" : "hidden");
        var opties = bronParamOptiesByBinding[m.bronBinding] || [];
        var hint = bronParamHintByBinding[m.bronBinding] || "";

        if (mode === "select") {
            var known = {};
            var optHtml = opties.map(function (o) { known[o.v] = true; return '<option value="' + escapeHtml(o.v) + '"' + (o.v === m.bronParam ? " selected" : "") + ">" + escapeHtml(o.n) + "</option>"; }).join("");
            if (m.bronParam && !known[m.bronParam]) {
                optHtml = '<option value="' + escapeHtml(m.bronParam) + '" selected>⚠ Huidige waarde: ' + escapeHtml(m.bronParam) + " (niet in lijst)</option>" + optHtml;
            }
            return {
                cellHtml: '<div class="gl-field"><label for="m-bronParam">Bron-parameter</label><select id="m-bronParam" class="form-select form-select-sm" data-field="bronParam" data-select2="1">' + optHtml + "</select></div>",
                hidden: false
            };
        }
        if (mode === "numeriek") {
            return {
                cellHtml: '<div class="gl-field"><label for="m-bronParam">Bron-parameter</label>' +
                    '<input id="m-bronParam" type="text" inputmode="numeric" pattern="[0-9]*" class="form-control form-control-sm" placeholder="bv. 123" data-field="bronParam" value="' + escapeHtml(m.bronParam) + '" aria-describedby="m-bronParam-hint" />' +
                    '<small id="m-bronParam-hint" class="gl-sm-hint">' + escapeHtml(hint) + "</small></div>",
                hidden: false
            };
        }
        return { cellHtml: hint ? '<div class="gl-field"><small class="gl-sm-hint">' + escapeHtml(hint) + "</small></div>" : "", hidden: !hint };
    }

    function triggerSummary(t) {
        var ev = optLabel(triggerEventOpties, t.triggerEvent) || "?";
        var ac = optLabel(triggerActieOpties, t.triggerActie) || "?";
        return escapeHtml(ev) + " &rarr; <b>" + escapeHtml(ac) + "</b>";
    }

    function renderTriggerRow(m, t, idx) {
        var showOffset = t.triggerEvent === XDAGEN_VOOR_DOELDATUM;
        return '<div class="gl-tsa-actie" data-trigger-idx="' + idx + '">' +
            '<div class="gl-tsa-actie-head">' +
            '<span class="gl-tsa-actie-num" aria-hidden="true">' + (idx + 1) + "</span>" +
            '<span class="gl-tsa-actie-summary">' + triggerSummary(t) + "</span>" +
            '<span class="gl-toggle gl-toggle-sm"><input type="checkbox" data-field="trigger-actief" data-idx="' + idx + '" aria-label="Actie ' + (idx + 1) + ' actief"' + (t.isActief ? " checked" : "") + "/><span></span></span>" +
            '<button type="button" class="btn btn-xs btn-outline-danger" data-action="delete-trigger" data-idx="' + idx + '" title="Actie ' + (idx + 1) + ' verwijderen"><i class="bx bx-x" aria-hidden="true"></i></button>' +
            "</div>" +
            '<div class="gl-tsa-actie-grid">' +
            '<div class="gl-field"><label for="m-trig-event-' + idx + '">Gebeurtenis</label><select id="m-trig-event-' + idx + '" class="form-select form-select-sm" data-field="trigger-event" data-idx="' + idx + '">' + optionsHtml(triggerEventOpties, t.triggerEvent) + "</select></div>" +
            '<div class="gl-field"><label for="m-trig-actie-' + idx + '">Actie</label><select id="m-trig-actie-' + idx + '" class="form-select form-select-sm" data-field="trigger-actie" data-idx="' + idx + '">' + optionsHtml(triggerActieOpties, t.triggerActie) + "</select></div>" +
            (showOffset ? '<div class="gl-field"><label for="m-trig-offset-' + idx + '">Dagen vooraf</label><input id="m-trig-offset-' + idx + '" type="number" class="form-control form-control-sm" data-field="trigger-offset" data-idx="' + idx + '" value="' + (t.offsetDagen != null ? t.offsetDagen : "") + '" /></div>' : "") +
            '<div class="gl-field"><label for="m-trig-params-' + idx + '">Parameters JSON</label><input id="m-trig-params-' + idx + '" type="text" class="form-control form-control-sm" placeholder=\'{"rol":5}\' data-field="trigger-params" data-idx="' + idx + '" value="' + escapeHtml(t.actieParametersJson) + '" /></div>' +
            "</div>" +
            '<div class="gl-tsa-actie-check-row">' +
            '<label for="m-trig-mag-' + idx + '"><input id="m-trig-mag-' + idx + '" type="checkbox" data-field="trigger-magwijzigen" data-idx="' + idx + '"' + (t.magProjectWijzigen ? " checked" : "") + " /> Mag project wijzigen</label>" +
            "</div>" +
            "</div>";
    }

    function renderMijlpaalDetail(f, m) {
        destroySelect2Within(detailPane);
        var isAutomatisch = !!m.bronBinding;
        var dag = computeMijlpaalDag(m);
        var bindingOptiesNonZero = bindingOpties.filter(function (o) { return o.v !== 0; });
        var param = paramControlHtml(m);

        var html = '<div class="gl-tsa-detail-head">' +
            '<div class="gl-tsa-detail-head-left">' +
            '<div class="gl-tsa-detail-icon" aria-hidden="true"><i class="bx bx-flag"></i></div>' +
            "<div>" +
            '<h3 class="gl-tsa-detail-title">' + (escapeHtml(m.naam) || "(naamloze mijlpaal)") + "</h3>" +
            '<p class="gl-tsa-detail-subtitle">' + escapeHtml(f.naam) + " · " + formatDagLabel(dag) + "</p>" +
            "</div></div>" +
            '<div class="gl-tsa-detail-actions">' +
            '<button type="button" class="btn btn-sm btn-light border" data-action="duplicate-mijlpaal" title="Mijlpaal dupliceren"><i class="bx bx-copy" aria-hidden="true"></i></button>' +
            '<button type="button" class="btn btn-sm btn-outline-danger" data-action="delete-mijlpaal" title="Mijlpaal verwijderen"><i class="bx bx-trash" aria-hidden="true"></i></button>' +
            "</div></div>" +
            '<div class="gl-tsa-detail-body">';

        // 1 — Wat is deze mijlpaal?
        html += '<section><div class="gl-tsa-section-head"><span class="gl-tsa-section-num" aria-hidden="true">1</span><div><h4 class="gl-tsa-section-title">Wat is deze mijlpaal?</h4></div></div>' +
            '<div class="gl-field-grid">' +
            '<div class="gl-field"><label for="m-naam">Naam voor de gebruiker</label><input id="m-naam" class="form-control form-control-sm" placeholder="bv. Vergunning ingediend" data-field="naam" value="' + escapeHtml(m.naam) + '" /></div>' +
            '<div class="gl-field"><div style="display:flex;justify-content:space-between;align-items:baseline">' +
            '<label for="m-code">Technische code</label>' +
            '<button type="button" class="btn btn-link btn-xs p-0" data-action="derive-code" style="font-size:.72rem">afleiden uit de naam</button></div>' +
            '<input id="m-code" class="form-control form-control-sm" placeholder="bv. VERGUNNING_INGEDIEND" data-field="code" value="' + escapeHtml(m.code) + '" aria-describedby="m-code-hint" />' +
            '<small id="m-code-hint" class="gl-sm-hint">Uniek — hiermee herkennen andere mijlpalen, bindingen en triggers deze mijlpaal.</small></div>' +
            '<div class="gl-field"><label for="m-type">Soort</label><select id="m-type" class="form-select form-select-sm" data-field="type">' + optionsHtml(typeOpties, m.mijlpaalType) + "</select></div>" +
            '<div class="gl-field"><label for="m-rol">Verantwoordelijke rol</label><select id="m-rol" class="form-select form-select-sm" data-field="rol" data-select2="1">' + optionsHtml(rolOpties, m.verantwoordelijkeRol, "Geen") + "</select></div>" +
            "</div>" +
            '<div class="gl-tsa-switch-row" style="margin-top:.9rem">' +
            '<div><span class="gl-sm-caption" id="m-scope-label" style="margin-bottom:.3rem;display:block">Geldt voor</span>' +
            '<div class="gl-segmented" role="group" aria-labelledby="m-scope-label">' +
            '<button type="button" data-action="set-scope" data-scope="0" aria-pressed="' + (m.scope === 0 ? "true" : "false") + '" class="' + (m.scope === 0 ? "is-active" : "") + '">Heel het project</button>' +
            '<button type="button" data-action="set-scope" data-scope="1" aria-pressed="' + (m.scope === 1 ? "true" : "false") + '" class="' + (m.scope === 1 ? "is-active" : "") + '">Per eenheid</button></div></div>' +
            '<div class="gl-switch-row"><span class="gl-toggle"><input id="m-verplicht" type="checkbox" data-field="verplicht"' + (m.isVerplicht ? " checked" : "") + " /><span></span></span>" +
            '<label for="m-verplicht">Verplicht <small class="text-muted">— moet bereikt worden vóór de fase afronden</small></label></div>' +
            "</div></section>";

        // 2 — Wanneer is ze bereikt?
        html += '<section><div class="gl-tsa-section-head"><span class="gl-tsa-section-num" aria-hidden="true">2</span><div><h4 class="gl-tsa-section-title" id="m-mode-label">Wanneer is ze bereikt?</h4><p class="gl-tsa-section-hint">bepaalt of CPM dit zelf detecteert</p></div></div>' +
            '<div class="gl-radiocard-row" role="group" aria-labelledby="m-mode-label">' +
            '<button type="button" class="gl-radiocard' + (!isAutomatisch ? " is-active" : "") + '" data-action="set-mode" data-mode="handmatig" aria-pressed="' + (!isAutomatisch ? "true" : "false") + '"><i class="bx bx-hand" aria-hidden="true"></i><div><div class="gl-radiocard-title">Handmatig afvinken</div><div class="gl-radiocard-desc">De verantwoordelijke zet de datum zelf.</div></div></button>' +
            '<button type="button" class="gl-radiocard' + (isAutomatisch ? " is-active" : "") + '" data-action="set-mode" data-mode="automatisch" aria-pressed="' + (isAutomatisch ? "true" : "false") + '"><i class="bx bx-bolt-circle" aria-hidden="true"></i><div><div class="gl-radiocard-title">Automatisch uit CPM</div><div class="gl-radiocard-desc">Bereikt zodra een gegeven in het project verandert.</div></div></button>' +
            "</div>";
        if (isAutomatisch) {
            html += '<div class="gl-field-grid" style="margin-top:.75rem">' +
                '<div class="gl-field"><label for="m-binding">Bron</label><select id="m-binding" class="form-select form-select-sm" data-field="binding">' + optionsHtml(bindingOptiesNonZero, m.bronBinding) + "</select></div>" +
                param.cellHtml +
                "</div>";
        }
        html += "</section>";

        // 3 — Streefdatum
        html += '<section><div class="gl-tsa-section-head"><span class="gl-tsa-section-num" aria-hidden="true">3</span><div><h4 class="gl-tsa-section-title">Streefdatum</h4><p class="gl-tsa-section-hint">bepaalt waarschuwingen en de tijdlijn</p></div></div>' +
            '<div class="gl-sm-daterule">' +
            '<div class="gl-sm-daterow-labels" style="display:flex;gap:.4rem;flex-wrap:wrap">' +
            '<label for="m-anker" class="gl-sm-caption" style="flex:1 1 260px">Telt vanaf</label>' +
            '<label for="m-offset" class="gl-sm-caption" style="flex:0 0 72px">Dagen</label>' +
            "</div>" +
            '<div class="gl-sm-daterule-row">' +
            '<select id="m-anker" class="gl-sm-anchor" data-field="anker" data-select2="1">' + anchorOptionsHtml(m) + "</select>" +
            '<span class="gl-sm-daterule-text" aria-hidden="true">+</span>' +
            '<input id="m-offset" type="number" class="form-control form-control-sm gl-sm-offset" placeholder="0" data-field="offset" value="' + (m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : "") + '" />' +
            '<span class="gl-sm-daterule-text" aria-hidden="true">dagen</span>' +
            '<span class="gl-tsa-tl-dag" id="streefdatumBadge" style="margin-left:auto">' + formatDagLabel(dag) + "</span>" +
            "</div></div></section>";

        // 4 — Acties
        html += '<section><div class="gl-tsa-section-head"><span class="gl-tsa-section-num" aria-hidden="true">4</span><div><h4 class="gl-tsa-section-title">Acties bij deze mijlpaal</h4><p class="gl-tsa-section-hint">' + (m.triggers.length ? m.triggers.length + " actie(s)" : "geen acties") + "</p></div>" +
            '<div class="gl-tsa-section-actions"><button type="button" class="btn btn-sm btn-outline-primary" data-action="add-trigger"><i class="bx bx-bell-plus" aria-hidden="true"></i> Actie toevoegen</button></div></div>' +
            (m.triggers.length ? m.triggers.map(function (t, i) { return renderTriggerRow(m, t, i); }).join("") : '<p class="gl-tsa-empty-hint">Nog geen acties bij deze mijlpaal.</p>') +
            "</section>";

        // 5 — Toelichting
        html += '<section><div class="gl-tsa-section-head"><span class="gl-tsa-section-num" aria-hidden="true">5</span><div><h4 class="gl-tsa-section-title">Toelichting</h4><p class="gl-tsa-section-hint">zichtbaar als tooltip op het project</p></div></div>' +
            '<label for="m-omschrijving" class="visually-hidden">Toelichting</label>' +
            '<textarea id="m-omschrijving" class="form-control form-control-sm" rows="3" placeholder="bv. Vergunning is definitief zodra de beroepstermijn van 35 dagen verstreken is." data-field="omschrijving">' + escapeHtml(m.omschrijving) + "</textarea></section>";

        html += "</div>";
        detailPane.innerHTML = html;
        detailPane.querySelectorAll('[data-select2="1"]').forEach(select2ify);
    }

    function renderFaseDetail(f) {
        destroySelect2Within(detailPane);
        var idx = state.fases.indexOf(f);
        var html = '<div class="gl-tsa-detail-head">' +
            '<div class="gl-tsa-detail-head-left"><div class="gl-tsa-detail-icon" aria-hidden="true" style="background:' + faseKleur(f, idx) + '22;color:' + faseKleur(f, idx) + '"><i class="bx bx-flag"></i></div>' +
            "<div><h3 class=\"gl-tsa-detail-title\">" + (escapeHtml(f.naam) || "(naamloze fase)") + "</h3><p class=\"gl-tsa-detail-subtitle\">Fase · " + f.mijlpalen.length + " mijlpalen</p></div></div>" +
            '<div class="gl-tsa-detail-actions"><button type="button" class="btn btn-sm btn-outline-danger" data-action="delete-fase" title="Fase verwijderen"><i class="bx bx-trash" aria-hidden="true"></i></button></div>' +
            "</div>" +
            '<div class="gl-tsa-detail-body"><section>' +
            '<div class="gl-field-grid">' +
            '<div class="gl-field"><label for="f-naam">Fasenaam</label><input id="f-naam" class="form-control form-control-sm" data-field="fnaam" value="' + escapeHtml(f.naam) + '" /></div>' +
            '<div class="gl-field"><label for="f-code">Code</label><input id="f-code" class="form-control form-control-sm" data-field="fcode" value="' + escapeHtml(f.code) + '" /></div>' +
            '<div class="gl-field"><label for="f-volgorde">Volgorde</label><input id="f-volgorde" type="number" class="form-control form-control-sm" data-field="fvolgorde" value="' + f.volgorde + '" /></div>' +
            '<div class="gl-field"><label for="f-status">Standaard ProjectStatusId</label><input id="f-status" type="number" class="form-control form-control-sm" data-field="fstatus" value="' + (f.standaardProjectStatusId != null ? f.standaardProjectStatusId : "") + '" aria-describedby="f-status-hint" />' +
            '<small id="f-status-hint" class="gl-sm-hint">Optioneel — koppelt deze fase aan een bestaande Project.StatusId.</small></div>' +
            "</div>" +
            '<div style="margin-top:.9rem"><span class="gl-sm-caption" id="f-kleur-label" style="margin-bottom:.4rem;display:block">Kleur in de fasenlijst</span>' +
            '<div class="gl-swatch-row" role="group" aria-labelledby="f-kleur-label">' + FASE_PALET.map(function (c) {
                return '<button type="button" class="gl-swatch' + (f.kleurCode === c ? " is-active" : "") + '" data-action="set-kleur" data-kleur="' + c + '" aria-pressed="' + (f.kleurCode === c ? "true" : "false") + '" aria-label="Kleur ' + c + '" style="background:' + c + '"></button>';
            }).join("") + "</div></div>" +
            "</section></div>";
        detailPane.innerHTML = html;
    }

    function renderDetail() {
        var sel = state.selection;
        if (!sel) {
            destroySelect2Within(detailPane);
            detailPane.innerHTML = '<div class="gl-tsa-detail-empty"><i class="bx bx-git-branch" aria-hidden="true"></i><p>Selecteer een fase of mijlpaal links,<br />of voeg een nieuwe fase toe om te beginnen.</p></div>';
            return;
        }
        if (sel.type === "fase") {
            var f = findFase(sel.faseUid);
            if (!f) { state.selection = null; return renderDetail(); }
            renderFaseDetail(f);
            return;
        }
        var f2 = findFase(sel.faseUid), m = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
        if (!f2 || !m) { state.selection = null; return renderDetail(); }
        renderMijlpaalDetail(f2, m);
    }

    // ── Live tree-label + streefdatum-badge (zonder volledige re-render, focus blijft behouden) ──

    function patchTreeLabel(uidVal, text) {
        var row = faseTree.querySelector('[data-mijlpaal-uid="' + uidVal + '"] .gl-tsa-m-naam');
        if (row) row.innerHTML = text ? escapeHtml(text) : '<span class="is-unnamed">(naamloze mijlpaal)</span>';
        var rowEl = faseTree.querySelector('[data-mijlpaal-uid="' + uidVal + '"]');
        if (rowEl) rowEl.setAttribute("aria-label", "Mijlpaal: " + (text || "(naamloze mijlpaal)"));
        var faseRow = faseTree.querySelector('[data-fase-select="' + uidVal + '"]');
        if (faseRow) faseRow.textContent = text || "(naamloze fase)";
    }

    function patchStreefdatumBadge(m) {
        var badge = document.getElementById("streefdatumBadge");
        if (badge) badge.innerHTML = formatDagLabel(computeMijlpaalDag(m));
        var subtitle = detailPane.querySelector(".gl-tsa-detail-subtitle");
        if (subtitle) {
            var f = findFase(state.selection.faseUid);
            subtitle.innerHTML = escapeHtml(f ? f.naam : "") + " · " + formatDagLabel(computeMijlpaalDag(m));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Detailpaneel — event delegation
    // ═══════════════════════════════════════════════════════════════════════

    detailPane.addEventListener("input", function (e) {
        var el = e.target;
        var field = el.getAttribute("data-field");
        if (!field) return;
        var sel = state.selection;
        if (!sel) return;

        if (sel.type === "fase") {
            var f = findFase(sel.faseUid);
            if (!f) return;
            if (field === "fnaam") { f.naam = el.value; patchTreeLabel(f._uid, f.naam); }
            else if (field === "fcode") f.code = el.value;
            else if (field === "fvolgorde") f.volgorde = parseInt(el.value, 10) || 0;
            else if (field === "fstatus") f.standaardProjectStatusId = el.value === "" ? null : parseInt(el.value, 10);
            markDirty();
            return;
        }

        var m = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
        if (!m) return;
        var idx = el.getAttribute("data-idx");

        switch (field) {
            case "naam": m.naam = el.value; patchTreeLabel(m._uid, m.naam); break;
            case "code": m.code = el.value; break;
            case "bronParam": m.bronParam = el.value; break;
            case "offset": m.doeldatumOffsetDagen = el.value === "" ? null : parseInt(el.value, 10); patchStreefdatumBadge(m); break;
            case "omschrijving": m.omschrijving = el.value; break;
            case "trigger-offset": m.triggers[idx].offsetDagen = el.value === "" ? null : parseInt(el.value, 10); break;
            case "trigger-params": m.triggers[idx].actieParametersJson = el.value; break;
        }
        markDirty();
        scheduleControleRefresh();
    });

    detailPane.addEventListener("change", function (e) {
        var el = e.target;
        var field = el.getAttribute("data-field");
        if (!field) return;
        var sel = state.selection;
        if (!sel || sel.type !== "mijlpaal") return;
        var m = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
        if (!m) return;
        var idx = el.getAttribute("data-idx");

        switch (field) {
            case "type": m.mijlpaalType = parseInt(el.value, 10) || 0; break;
            case "rol": m.verantwoordelijkeRol = el.value === "" ? null : parseInt(el.value, 10); break;
            // select2 stuurt op een keuze enkel "change" (geen "input") — bronParam heeft in select-modus dus ook hier een handler nodig.
            case "bronParam": m.bronParam = el.value; break;
            case "verplicht": m.isVerplicht = el.checked; break;
            case "binding": m.bronBinding = parseInt(el.value, 10) || 0; m.bronParam = ""; markDirty(); scheduleControleRefresh(); renderMijlpaalDetail(findFase(sel.faseUid), m); return;
            case "anker": m.doeldatumAnkerCode = el.value; patchStreefdatumBadge(m); scheduleControleRefresh(); markDirty(); return;
            case "trigger-event": m.triggers[idx].triggerEvent = parseInt(el.value, 10) || 0; markDirty(); scheduleControleRefresh(); renderMijlpaalDetail(findFase(sel.faseUid), m); return;
            case "trigger-actie": m.triggers[idx].triggerActie = parseInt(el.value, 10) || 0; break;
            case "trigger-actief": m.triggers[idx].isActief = el.checked; break;
            case "trigger-magwijzigen": m.triggers[idx].magProjectWijzigen = el.checked; break;
            default: return;
        }
        markDirty();
        scheduleControleRefresh();
        // korte samenvattingszin (Gebeurtenis/Actie) kan gewijzigd zijn
        var summaryHost = el.closest(".gl-tsa-actie");
        if (summaryHost && (field === "trigger-actie")) {
            var sumEl = summaryHost.querySelector(".gl-tsa-actie-summary");
            if (sumEl) sumEl.innerHTML = triggerSummary(m.triggers[idx]);
        }
    });

    detailPane.addEventListener("click", function (e) {
        var sel = state.selection;
        if (!sel) return;

        var scopeBtn = e.target.closest("[data-action='set-scope']");
        if (scopeBtn) {
            var m1 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (m1) { m1.scope = parseInt(scopeBtn.getAttribute("data-scope"), 10); markDirty(); renderMijlpaalDetail(findFase(sel.faseUid), m1); }
            return;
        }
        var modeBtn = e.target.closest("[data-action='set-mode']");
        if (modeBtn) {
            var m2 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!m2) return;
            if (modeBtn.getAttribute("data-mode") === "handmatig") { m2.bronBinding = 0; m2.bronParam = ""; }
            else if (!m2.bronBinding) { var first = bindingOpties.filter(function (o) { return o.v !== 0; })[0]; m2.bronBinding = first ? first.v : 0; }
            markDirty();
            renderMijlpaalDetail(findFase(sel.faseUid), m2);
            scheduleControleRefresh();
            return;
        }
        var deriveBtn = e.target.closest("[data-action='derive-code']");
        if (deriveBtn) {
            var m3 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!m3) return;
            m3.code = (m3.naam || "").toUpperCase().trim().replace(/[^A-Z0-9]+/g, "_").replace(/^_+|_+$/g, "");
            renderMijlpaalDetail(findFase(sel.faseUid), m3);
            markDirty();
            scheduleControleRefresh();
            return;
        }
        var addTrig = e.target.closest("[data-action='add-trigger']");
        if (addTrig) {
            var m4 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!m4) return;
            m4.triggers.push(cloneTrigger({}));
            renderMijlpaalDetail(findFase(sel.faseUid), m4);
            markDirty();
            var tree = findFase(sel.faseUid); if (tree) renderTree();
            return;
        }
        var delTrig = e.target.closest("[data-action='delete-trigger']");
        if (delTrig) {
            var m5 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!m5) return;
            m5.triggers.splice(parseInt(delTrig.getAttribute("data-idx"), 10), 1);
            renderMijlpaalDetail(findFase(sel.faseUid), m5);
            markDirty();
            renderTree();
            scheduleControleRefresh();
            return;
        }
        var dupBtn = e.target.closest("[data-action='duplicate-mijlpaal']");
        if (dupBtn) {
            var f6 = findFase(sel.faseUid), m6 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!f6 || !m6) return;
            var kopie = cloneMijlpaal(m6);
            kopie.naam = m6.naam ? m6.naam + " (kopie)" : "";
            kopie.code = m6.code ? m6.code + "_KOPIE" : "";
            var idxAt = f6.mijlpalen.indexOf(m6);
            f6.mijlpalen.splice(idxAt + 1, 0, kopie);
            markDirty();
            selectMijlpaal(f6._uid, kopie._uid);
            return;
        }
        var delM = e.target.closest("[data-action='delete-mijlpaal']");
        if (delM) {
            var f7 = findFase(sel.faseUid), m7 = findMijlpaal(sel.faseUid, sel.mijlpaalUid);
            if (!f7 || !m7) return;
            if (!confirm('Mijlpaal "' + (m7.naam || "(naamloos)") + '" verwijderen?')) return;
            f7.mijlpalen.splice(f7.mijlpalen.indexOf(m7), 1);
            markDirty();
            if (f7.mijlpalen.length) selectMijlpaal(f7._uid, f7.mijlpalen[0]._uid);
            else selectFase(f7._uid);
            scheduleControleRefresh();
            return;
        }
        var delF = e.target.closest("[data-action='delete-fase']");
        if (delF) {
            var f8 = findFase(sel.faseUid);
            if (!f8) return;
            if (!confirm('Fase "' + (f8.naam || "(naamloos)") + '" met ' + f8.mijlpalen.length + ' mijlpalen verwijderen?')) return;
            state.fases.splice(state.fases.indexOf(f8), 1);
            state.selection = null;
            markDirty();
            renderTree();
            renderDetail();
            scheduleControleRefresh();
            return;
        }
        var kleurBtn = e.target.closest("[data-action='set-kleur']");
        if (kleurBtn) {
            var f9 = findFase(sel.faseUid);
            if (!f9) return;
            f9.kleurCode = kleurBtn.getAttribute("data-kleur");
            markDirty();
            renderTree();
            renderFaseDetail(f9);
        }
    });

    // ═══════════════════════════════════════════════════════════════════════
    // Tijdlijn & simulatie
    // ═══════════════════════════════════════════════════════════════════════

    var simStartInput = document.getElementById("tsaSimStart");
    simStartInput.value = state.simStart;
    simStartInput.addEventListener("change", function () {
        state.simStart = simStartInput.value || todayIso();
        patchStreefdatumBadgeIfVisible();
        renderTijdlijn();
    });

    function patchStreefdatumBadgeIfVisible() {
        if (state.selection && state.selection.type === "mijlpaal") {
            var m = findMijlpaal(state.selection.faseUid, state.selection.mijlpaalUid);
            if (m) patchStreefdatumBadge(m);
        }
    }

    function renderTijdlijn() {
        var host = document.getElementById("tijdlijnContainer");
        if (!host) return;
        if (!state.fases.length) { host.innerHTML = '<p class="gl-tsa-empty-hint">Nog geen fases om te simuleren.</p>'; return; }

        var html = '<div class="gl-traject-timeline">' + state.fases.map(function (f, fi) {
            var rows = f.mijlpalen.map(function (m) {
                var dag = computeMijlpaalDag(m);
                return '<li class="gl-traject-mijlpaal" style="grid-template-columns:14px minmax(0,1fr) auto">' +
                    '<span class="gl-tm-dot" style="background:' + faseKleur(f, fi) + '" aria-hidden="true"></span>' +
                    '<span class="gl-tm-naam">' + (escapeHtml(m.naam) || "(naamloos)") + "</span>" +
                    '<span class="gl-tm-datum">' + formatDagLabel(dag) + "</span>" +
                    "</li>";
            }).join("");
            return '<div class="gl-traject-fase"><div class="gl-traject-fase-head"><span class="gl-traject-fase-naam">' + escapeHtml(f.naam) + '</span></div>' +
                '<ul class="gl-traject-mijlpalen">' + (rows || '<li class="gl-tsa-empty-hint">Geen mijlpalen in deze fase.</li>') + "</ul></div>";
        }).join("") + "</div>";
        host.innerHTML = html;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Controle
    // ═══════════════════════════════════════════════════════════════════════

    function computeIssues() {
        var issues = [];
        if (!state.fases.length) {
            issues.push({ severity: "urgent", text: "Dit sjabloon heeft nog geen fases." });
            return issues;
        }
        var codeMap = {};
        state.fases.forEach(function (f) {
            if (!f.naam.trim()) issues.push({ severity: "urgent", text: "Een fase heeft nog geen naam.", faseUid: f._uid });
            if (!f.code.trim()) issues.push({ severity: "normal", text: 'Fase "' + (f.naam || "?") + '" heeft geen code.', faseUid: f._uid });
            if (!f.mijlpalen.length) issues.push({ severity: "normal", text: 'Fase "' + (f.naam || "?") + '" heeft nog geen mijlpalen.', faseUid: f._uid });

            f.mijlpalen.forEach(function (m) {
                var loc = f.naam + " · " + (m.naam || "(naamloos)");
                if (!m.naam.trim()) issues.push({ severity: "urgent", text: "Een mijlpaal in \"" + f.naam + "\" heeft nog geen naam.", loc: f.naam, faseUid: f._uid, mijlpaalUid: m._uid });
                if (!m.code.trim()) {
                    issues.push({ severity: "urgent", text: '"' + (m.naam || "(naamloos)") + '" heeft geen technische code — kan niet als anker gebruikt worden.', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid });
                } else {
                    var key = m.code.trim().toLowerCase();
                    (codeMap[key] = codeMap[key] || []).push({ faseUid: f._uid, mijlpaalUid: m._uid, loc: loc, naam: m.naam });
                }
                var anker = m.doeldatumAnkerCode;
                if (anker && anker !== PROJECT_CREATED) {
                    if (!findMijlpaalByCode(anker)) issues.push({ severity: "urgent", text: '"' + (m.naam || "?") + '" verwijst naar een onbekende ankercode "' + anker + '".', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid });
                    else if (computeMijlpaalDag(m) == null) issues.push({ severity: "urgent", text: '"' + (m.naam || "?") + '" zit in een cirkelverwijzing van ankers.', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid });
                }
                if (m.bronBinding && bronParamModeByBinding[m.bronBinding] === "select" && !m.bronParam) {
                    issues.push({ severity: "normal", text: '"' + (m.naam || "?") + '" heeft een automatische bron gekozen maar geen bron-parameter.', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid });
                }
                (m.triggers || []).forEach(function (t, ti) {
                    if (t.triggerEvent === XDAGEN_VOOR_DOELDATUM && (t.offsetDagen == null || t.offsetDagen === "")) {
                        issues.push({ severity: "normal", text: "Actie " + (ti + 1) + ' op "' + (m.naam || "?") + '" ("X dagen voor streefdatum") heeft geen aantal dagen ingevuld.', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid });
                    }
                    if (t.actieParametersJson) {
                        try { JSON.parse(t.actieParametersJson); }
                        catch (e) { issues.push({ severity: "normal", text: "Actie " + (ti + 1) + ' op "' + (m.naam || "?") + '" heeft ongeldige Parameters JSON.', loc: loc, faseUid: f._uid, mijlpaalUid: m._uid }); }
                    }
                });
            });
        });
        Object.keys(codeMap).forEach(function (k) {
            var arr = codeMap[k];
            if (arr.length > 1) {
                arr.forEach(function (e) {
                    issues.push({ severity: "urgent", text: 'Technische code "' + k.toUpperCase() + '" komt meermaals voor — ankers/triggers kunnen de verkeerde mijlpaal raken.', loc: e.loc, faseUid: e.faseUid, mijlpaalUid: e.mijlpaalUid });
                });
            }
        });
        return issues;
    }

    var SEVERITY_META = {
        urgent: { title: "Op te lossen", icon: "bx-alert-triangle", cls: "gl-tsa-issue-urgent" },
        normal: { title: "Aandachtspunt", icon: "bx-alert-circle", cls: "gl-tsa-issue-normal" },
        info: { title: "Ter info", icon: "bx-info-circle", cls: "gl-tsa-issue-info" }
    };

    function renderControle() {
        var host = document.getElementById("controleContainer");
        var badge = document.getElementById("tsaControleBadge");
        if (!host) return;
        var issues = computeIssues();

        if (badge) {
            var count = issues.filter(function (i) { return i.severity !== "info"; }).length;
            badge.textContent = String(count);
            badge.hidden = count === 0;
        }

        if (!issues.length) {
            host.innerHTML = '<div class="gl-tsa-controle-ok"><i class="bx bx-check-circle" aria-hidden="true"></i><p>Alles in orde — geen aandachtspunten gevonden.</p></div>';
            return;
        }

        var groups = { urgent: [], normal: [], info: [] };
        issues.forEach(function (i) { groups[i.severity].push(i); });

        host.innerHTML = ["urgent", "normal", "info"].map(function (sev) {
            if (!groups[sev].length) return "";
            var meta = SEVERITY_META[sev];
            var items = groups[sev].map(function (i) {
                var jumpable = !!(i.mijlpaalUid || i.faseUid);
                var attrs = i.mijlpaalUid ? ' data-jump-fase="' + i.faseUid + '" data-jump-mijlpaal="' + i.mijlpaalUid + '"' : (i.faseUid ? ' data-jump-fase="' + i.faseUid + '"' : "");
                var tag = jumpable ? "button" : "div";
                var typeAttr = jumpable ? ' type="button"' : "";
                return "<" + tag + typeAttr + ' class="gl-tsa-issue ' + meta.cls + '"' + attrs + '><i class="bx ' + meta.icon + '" aria-hidden="true"></i><span>' + escapeHtml(i.text) + (i.loc ? '<span class="gl-tsa-issue-loc">' + escapeHtml(i.loc) + "</span>" : "") + "</span></" + tag + ">";
            }).join("");
            return '<div class="gl-tsa-issue-group"><div class="gl-tsa-issue-group-title">' + meta.title + " (" + groups[sev].length + ")</div>" + items + "</div>";
        }).join("");
    }

    document.getElementById("controleContainer").addEventListener("click", function (e) {
        var target = e.target.closest("[data-jump-fase]");
        if (!target) return;
        var faseUid = target.getAttribute("data-jump-fase");
        var mijlpaalUid = target.getAttribute("data-jump-mijlpaal");
        var f = findFase(faseUid);
        if (!f) return;
        f.expanded = true;
        var structuurTab = document.getElementById("tabbtn-structuur");
        if (structuurTab) structuurTab.click();
        if (mijlpaalUid) selectMijlpaal(faseUid, mijlpaalUid, true);
        else selectFase(faseUid, true);
    });

    var scheduleControleRefresh = debounce(renderControle, 250);

    document.addEventListener("tsa:tab", function (e) {
        if (e.detail.tab === "tijdlijn") renderTijdlijn();
        if (e.detail.tab === "controle") renderControle();
    });

    // ═══════════════════════════════════════════════════════════════════════
    // Bovenste sjabloonvelden — dirty-tracking
    // ═══════════════════════════════════════════════════════════════════════

    ["sj-naam", "sj-type", "sj-omschrijving", "sj-standaard", "sj-actief"].forEach(function (id) {
        var el = document.getElementById(id);
        if (el) { el.addEventListener("input", markDirty); el.addEventListener("change", markDirty); }
    });

    window.addEventListener("beforeunload", function (e) {
        if (!state.dirty) return;
        e.preventDefault();
        e.returnValue = "";
    });

    // ═══════════════════════════════════════════════════════════════════════
    // Opslaan — serialiseer de volledige state naar payloadJson
    // ═══════════════════════════════════════════════════════════════════════

    document.getElementById("sjabloonForm").addEventListener("submit", function () {
        var payload = {
            id: initial.id || null,
            naam: document.getElementById("sj-naam").value.trim(),
            projectType: document.getElementById("sj-type").value === "" ? null : parseInt(document.getElementById("sj-type").value, 10),
            isStandaard: document.getElementById("sj-standaard").checked,
            isActief: document.getElementById("sj-actief").checked,
            omschrijving: document.getElementById("sj-omschrijving").value,
            fases: state.fases.filter(function (f) { return f.naam.trim(); }).map(function (f, fi) {
                return {
                    naam: f.naam.trim(),
                    code: f.code.trim() || f.naam.toUpperCase().replace(/[^A-Z0-9]+/g, "_"),
                    volgorde: f.volgorde || (fi + 1) * 10,
                    kleurCode: f.kleurCode,
                    standaardProjectStatusId: f.standaardProjectStatusId,
                    mijlpalen: f.mijlpalen.filter(function (m) { return m.naam.trim(); }).map(function (m, mi) {
                        return {
                            naam: m.naam.trim(),
                            code: m.code.trim(),
                            volgorde: (mi + 1) * 10,
                            mijlpaalType: m.mijlpaalType,
                            scope: m.scope,
                            verantwoordelijkeRol: m.verantwoordelijkeRol,
                            doeldatumAnkerCode: (m.doeldatumAnkerCode && m.doeldatumAnkerCode !== PROJECT_CREATED) ? m.doeldatumAnkerCode.trim() : PROJECT_CREATED,
                            doeldatumOffsetDagen: m.doeldatumOffsetDagen,
                            isVerplicht: m.isVerplicht,
                            bronBinding: m.bronBinding || null,
                            bronParam: m.bronParam || null,
                            dossierKind: m.dossierKind,
                            omschrijving: m.omschrijving || null,
                            triggers: m.triggers.map(function (t) {
                                return {
                                    triggerEvent: t.triggerEvent,
                                    triggerActie: t.triggerActie,
                                    offsetDagen: t.offsetDagen,
                                    actieParametersJson: t.actieParametersJson || null,
                                    magProjectWijzigen: t.magProjectWijzigen,
                                    isActief: t.isActief,
                                    omschrijving: t.omschrijving || null
                                };
                            })
                        };
                    })
                };
            })
        };
        document.getElementById("payloadJson").value = JSON.stringify(payload);
        state.dirty = false;
    });

    // ── Init ─────────────────────────────────────────────────────────────────
    renderTree();
    renderDetail();
    renderControle();
})();
