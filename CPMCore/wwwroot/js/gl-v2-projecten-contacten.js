// gl-v2 — Contacten, lijst (design-handoff 44a) en lijsten & export (44d). Echte DataTable; tabs, zoeken en filters zijn een ext.search op de data-* van de rijen
// (alle contacten van het project zijn al geladen). Selectiebalk: status wijzigen, toewijzen, opslaan als lijst en exporteren.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-ct-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }
    var canWrite = root.dataset.canWrite === "1", me = root.dataset.me || "";
    var tableEl = document.getElementById("datatable-contacten"), body = $("#ct-body"), table = null;
    function allRows() { return table ? table.rows().nodes().toArray() : $$(".gl-v2-ct-row", body); }
    function appliedRows() { return table.rows({ search: "applied" }).nodes().toArray(); }
    function token() { var i = document.querySelector("input[name=__RequestVerificationToken]"); return i ? i.value : ""; }
    function post(url, obj) {
        var fd = new FormData(); fd.append("__RequestVerificationToken", token());
        Object.keys(obj || {}).forEach(function (k) { if (obj[k] != null) fd.append(k, obj[k]); });
        return fetch(url, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-ct-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 4500);
    }
    function modal(id) { var m = document.getElementById(id); return m && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(m) : null; }

    // ── filters ──
    var tab = "all";
    var search = $("#ct-search"), searchClear = $("#ct-search-clear"), toggle = $("#ct-filters-toggle"), panel = $("#ct-filters-panel"), countBadge = $("#ct-filters-count"), chips = $("#ct-chips"), clearBtn = $("#ct-filters-clear");
    var filters = {
        source: { el: $("#ct-f-source"), name: "Bron" }, unit: { el: $("#ct-f-unit"), name: "Interesse" }, who: { el: $("#ct-f-who"), name: "Wie" },
        next: { el: $("#ct-f-next"), name: "Volgende actie" }, created: { el: $("#ct-f-created"), name: "Binnengekomen" }, consent: { el: $("#ct-f-consent"), name: "Toestemming" }
    };
    function val(k) { return filters[k].el ? filters[k].el.value : ""; }
    function labelOf(k) { var w = filters[k].el && filters[k].el.closest("[data-gl-v2-select]"), l = w && w.querySelector(".gl-v2-select-trigger-label"); return l ? l.textContent.trim() : val(k); }
    var TABS = { all: null, "new": [0], follow: [1, 2], option: [3], sold: [4], lost: [5] };
    // één rij tegenover een bewaarde of actieve filterstand
    function rowMatches(tr, st) {
        var f = st.f, s = parseInt(tr.dataset.status, 10), t = TABS[st.tab];
        if (t && t.indexOf(s) < 0) return false;
        if (f.source && tr.dataset.source !== f.source) return false;
        if (f.unit && tr.dataset.units.indexOf("," + f.unit + ",") < 0) return false;
        if (f.who) { var who = tr.dataset.who; if (f.who === "me" ? who !== me : f.who === "none" ? who !== "" : who !== f.who) return false; }
        if (f.next) { var n = tr.dataset.next; if (f.next === "week") { if (n !== "today" && n !== "week" && n !== "overdue") return false; } else if (n !== f.next) return false; }
        if (f.created && parseInt(tr.dataset.days, 10) > parseInt(f.created, 10)) return false;
        if (f.consent) { var c = tr.dataset.consent; if (f.consent === "contact" && c.charAt(0) !== "1") return false; if (f.consent === "news" && c.charAt(1) !== "1") return false; if (f.consent === "none" && c.charAt(0) === "1") return false; }
        if (st.q && tr.dataset.search.indexOf(st.q) < 0) return false;
        return true;
    }
    function state() { var f = {}; Object.keys(filters).forEach(function (k) { f[k] = val(k); }); return { tab: tab, q: (search.value || "").trim().toLowerCase(), f: f }; }
    window.jQuery.fn.dataTable.ext.search.push(function (settings, data, idx) {
        if (settings.nTable !== tableEl) return true;
        var tr = settings.aoData[idx] && settings.aoData[idx].nTr;
        return tr ? rowMatches(tr, state()) : true;
    });

    function refreshChrome() {
        searchClear.hidden = !search.value;
        var active = Object.keys(filters).filter(function (k) { return val(k) !== ""; });
        countBadge.textContent = active.length; countBadge.hidden = active.length === 0;
        toggle.classList.toggle("has-active", active.length > 0);
        chips.innerHTML = "";
        active.forEach(function (k) {
            var c = document.createElement("button"); c.type = "button"; c.className = "gl-v2-filters-chip"; c.setAttribute("aria-label", filters[k].name + "-filter verwijderen");
            c.innerHTML = "<span>" + esc(filters[k].name + ": " + labelOf(k)) + '</span><i class="ph ph-x" aria-hidden="true"></i>';
            c.addEventListener("click", function () { setFilter(k, ""); });
            chips.appendChild(c);
        });
        clearBtn.hidden = active.length === 0;
        $$("[data-kpi-filter]").forEach(function (c) { c.classList.toggle("is-active", kpiKey() === c.dataset.kpiFilter); c.setAttribute("aria-pressed", kpiKey() === c.dataset.kpiFilter ? "true" : "false"); });
        $$("[data-ct-tab]").forEach(function (a) { var on = a.dataset.ctTab === tab; a.classList.toggle("is-active", on); a.setAttribute("aria-selected", on ? "true" : "false"); });
    }
    function kpiKey() {
        var f = state().f, others = Object.keys(f).filter(function (k) { return k !== "next" && f[k] !== ""; });
        if (others.length || search.value) return "";
        if (f.next === "today" && tab === "all") return "next:today";
        if (f.next === "overdue" && tab === "all") return "next:overdue";
        if (!f.next && tab === "follow") return "status:2";
        return "";
    }
    function apply() {
        allRows().forEach(function (tr) { if (!rowMatches(tr, state())) { var cb = $(".js-ct-check", tr); if (cb) cb.checked = false; } });
        refreshChrome(); table.draw(); syncPageLength(); updateSelection(); renderLists();
    }
    function setFilter(k, v) {
        var w = filters[k].el && filters[k].el.closest("[data-gl-v2-select]"), o = w && w.querySelector('.gl-v2-select-option[data-value="' + v + '"]');
        if (o) o.click(); else if (filters[k].el) { filters[k].el.value = v; apply(); }
    }
    function setState(st) {
        tab = st.tab || "all"; search.value = st.q || "";
        Object.keys(filters).forEach(function (k) { var v = (st.f && st.f[k]) || ""; var w = filters[k].el && filters[k].el.closest("[data-gl-v2-select]"), o = w && w.querySelector('.gl-v2-select-option[data-value="' + v + '"]'); if (o) { if (filters[k].el.value !== v) o.click(); } else if (filters[k].el) filters[k].el.value = v; });
        apply();
    }
    Object.keys(filters).forEach(function (k) { if (filters[k].el) filters[k].el.addEventListener("change", apply); });
    search.addEventListener("input", apply);
    searchClear.addEventListener("click", function () { search.value = ""; search.focus(); apply(); });
    toggle.addEventListener("click", function () { var open = panel.hidden; panel.hidden = !open; toggle.classList.toggle("is-open", open); toggle.setAttribute("aria-expanded", open ? "true" : "false"); syncPageLength(); });
    clearBtn.addEventListener("click", function () { Object.keys(filters).forEach(function (k) { if (val(k) !== "") setFilter(k, ""); }); });
    document.addEventListener("click", function (e) {
        var t = e.target.closest("[data-ct-tab]"); if (t) { e.preventDefault(); tab = t.dataset.ctTab; apply(); return; }
        var kp = e.target.closest("[data-kpi-filter]");
        if (kp) {
            var key = kp.dataset.kpiFilter, on = kpiKey() === key;
            Object.keys(filters).forEach(function (k) { if (val(k) !== "") setFilter(k, ""); }); search.value = ""; tab = "all";
            if (!on) { if (key.indexOf("next:") === 0) setFilter("next", key.slice(5)); else if (key === "status:2") tab = "follow"; }
            apply();
        }
    });

    // ── rij openen ──
    body.addEventListener("click", function (e) {
        var tr = e.target.closest(".gl-v2-ct-row"); if (!tr) return;
        if (e.target.closest("input, button, a, .gl-v2-menu")) return;
        window.location.href = tr.dataset.href;
    });

    // ── selectiebalk ──
    var bar = $("#ct-selbar"), checkAll = $("#ct-check-all");
    function selected() { return appliedRows().filter(function (tr) { var cb = $(".js-ct-check", tr); return cb && cb.checked; }); }
    function updateSelection() {
        if (!bar) return;
        var sel = selected();
        allRows().forEach(function (tr) { var cb = $(".js-ct-check", tr); tr.classList.toggle("is-selected", !!(cb && cb.checked)); });
        var wasHidden = bar.hidden;
        bar.hidden = sel.length === 0;
        if (wasHidden !== bar.hidden && table) syncPageLength();
        $("#ct-sel-n").textContent = sel.length; $("#ct-sel-of").textContent = appliedRows().length;
        $$(".js-ct-sel-n", bar).forEach(function (n) { n.textContent = sel.length; });
        if (sel.length === 0) closePops();
        if (checkAll) { var vis = appliedRows().length; checkAll.checked = vis > 0 && sel.length === vis; checkAll.indeterminate = sel.length > 0 && sel.length < vis; }
    }
    body.addEventListener("change", function (e) { if (e.target.classList.contains("js-ct-check")) updateSelection(); });
    if (checkAll) checkAll.addEventListener("change", function () { appliedRows().forEach(function (tr) { var cb = $(".js-ct-check", tr); if (cb) cb.checked = checkAll.checked; }); updateSelection(); });
    var selClear = $("#ct-sel-clear"); if (selClear) selClear.addEventListener("click", function () { allRows().forEach(function (tr) { var cb = $(".js-ct-check", tr); if (cb) cb.checked = false; }); updateSelection(); });
    function closePops() { $$(".gl-v2-ct-pop").forEach(function (p) { p.hidden = true; }); $$(".js-ct-pop").forEach(function (b) { b.classList.remove("is-open"); }); }
    document.addEventListener("click", function (e) {
        var open = e.target.closest(".js-ct-pop");
        if (open) { var p = document.getElementById(open.dataset.pop), was = !p.hidden; closePops(); if (!was) { p.hidden = false; open.classList.add("is-open"); } return; }
        if (!e.target.closest(".gl-v2-ct-pop")) closePops();
    });
    function ids(list) { return list.map(function (tr) { return tr.dataset.id; }).join(","); }
    function bulk(url, data) { return post(url, data).then(function (j) { if (j.ok) window.location.reload(); else toast(j.error || "Wijzigen mislukt."); }); }
    var pendingIds = "";
    document.addEventListener("click", function (e) {
        var d = e.target.closest(".js-ct-do"); if (!d) return;
        var sel = selected(); if (!sel.length) return;
        if (d.dataset.op === "status") {
            var s = parseInt(d.dataset.status, 10);
            if (s === 5) { pendingIds = ids(sel); closePops(); var m = modal("ct-reason-modal"); if (m) m.show(); return; }
            bulk(root.dataset.statusUrl, { ids: ids(sel), status: s });
        } else if (d.dataset.op === "assign") bulk(root.dataset.assignUrl, { ids: ids(sel), assignee: d.dataset.user || "" });
    });
    var ro = $("#ct-reason-ok"); if (ro) ro.addEventListener("click", function () { bulk(root.dataset.statusUrl, { ids: pendingIds, status: 5, reason: $("#ct-reason").value }); });

    // ── opgeslagen lijsten (44d) ──
    var lists = []; try { lists = JSON.parse(root.dataset.lists || "[]"); } catch (e) { lists = []; }
    lists.forEach(function (l) { try { l.st = JSON.parse(l.filters || "{}"); } catch (e) { l.st = {}; } });
    var activeList = 0;
    function summary(st) {
        var bits = [], f = st.f || {};
        var TL = { "new": "status Nieuw", follow: "status In opvolging", option: "status Optie", sold: "status Verkocht", lost: "status Afgevallen" };
        if (TL[st.tab]) bits.push(TL[st.tab]);
        if (f.source) bits.push("bron " + f.source); if (f.unit) { var u = $('#ct-f-unit_select .gl-v2-select-option[data-value="' + f.unit + '"]'); bits.push("interesse " + (u ? u.textContent.trim() : "eenheid")); }
        if (f.who) bits.push(f.who === "me" ? "mijn contacten" : f.who === "none" ? "niet toegewezen" : "toegewezen");
        if (f.next) bits.push({ today: "actie vandaag", overdue: "achterstallig", week: "actie ≤ 7 dagen", none: "zonder actie" }[f.next] || "");
        if (f.created) bits.push("laatste " + f.created + " dagen");
        if (f.consent) bits.push({ contact: "mag gecontacteerd worden", news: "toestemming nieuwsbrief", none: "zonder toestemming" }[f.consent] || "");
        if (st.q) bits.push("zoek “" + st.q + "”");
        return bits.filter(Boolean).join(" · ") || "alle contacten";
    }
    function countFor(st) { var n = 0; allRows().forEach(function (tr) { if (rowMatches(tr, st)) n++; }); return n; }
    function renderLists() {
        var box = $("#ct-lists-items"); if (!box) return;
        box.innerHTML = lists.map(function (l) {
            return '<div class="gl-v2-ct-list' + (activeList === l.id ? " is-on" : "") + '" role="button" tabindex="0" data-list="' + l.id + '"><span class="gl-v2-ct-list-text"><b>' + esc(l.name) + '</b><small>' + esc(summary(l.st)) + '</small></span><span class="gl-v2-ct-list-n">' + countFor(l.st) + '</span>' +
                (canWrite ? '<button type="button" class="gl-v2-ct-list-x" data-del="' + l.id + '" aria-label="Lijst ' + esc(l.name) + ' verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + '</div>';
        }).join("");
        $("#ct-lists-empty").hidden = lists.length > 0;
        var cnt = $("#ct-lists-count"); cnt.textContent = lists.length; cnt.hidden = lists.length === 0;
    }
    var listsToggle = $("#ct-lists-toggle"), listsPanel = $("#ct-lists");
    function showLists(on) { listsPanel.hidden = !on; listsToggle.classList.toggle("is-open", on); listsToggle.setAttribute("aria-expanded", on ? "true" : "false"); }
    listsToggle.addEventListener("click", function () { showLists(listsPanel.hidden); });
    listsPanel.addEventListener("click", function (e) {
        var del = e.target.closest("[data-del]");
        if (del) { var l = lists.filter(function (x) { return String(x.id) === del.dataset.del; })[0]; if (!l) return; deleteList = l; $("#ct-list-del-name").textContent = l.name; var m = modal("ct-list-del-modal"); if (m) m.show(); return; }
        var it = e.target.closest("[data-list]"); if (!it) return;
        var l2 = lists.filter(function (x) { return String(x.id) === it.dataset.list; })[0]; if (!l2) return;
        if (activeList === l2.id) { activeList = 0; setState({ tab: "all", f: {} }); } else { activeList = l2.id; setState(l2.st); }
    });
    var deleteList = null;
    var savelist = $("#ct-savelist"), selSave = $("#ct-sel-savelist");
    function openSave() { $("#ct-list-name").value = ""; $("#ct-list-help").textContent = "De filters worden bewaard. De lijst blijft automatisch bij: nieuwe contacten die erbij passen, verschijnen er vanzelf in."; var m = modal("ct-savelist-modal"); if (m) { m.show(); setTimeout(function () { $("#ct-list-name").focus(); }, 300); } }
    if (savelist) savelist.addEventListener("click", openSave);
    if (selSave) selSave.addEventListener("click", function () { openSave(); });
    var lok = $("#ct-list-ok");
    if (lok) lok.addEventListener("click", function () {
        var name = $("#ct-list-name").value.trim(); if (!name) { $("#ct-list-help").textContent = "Geef de lijst een naam."; return; }
        post(root.dataset.listSaveUrl, { name: name, filters: JSON.stringify(state()) }).then(function (j) {
            if (!j.ok) { $("#ct-list-help").textContent = j.error || "Opslaan mislukt."; return; }
            var st = state(); lists.push({ id: j.id, name: j.name, filters: JSON.stringify(st), st: st }); lists.sort(function (a, b) { return a.name.localeCompare(b.name); });
            activeList = j.id; var m = modal("ct-savelist-modal"); if (m) m.hide(); showLists(true); renderLists(); toast("Lijst “" + j.name + "” opgeslagen");
        });
    });
    var ld = $("#ct-list-del-ok");
    if (ld) ld.addEventListener("click", function () {
        if (!deleteList) return;
        post(root.dataset.listDeleteUrl, { id: deleteList.id }).then(function (j) {
            if (!j.ok) { toast("Verwijderen mislukt."); return; }
            lists = lists.filter(function (x) { return x !== deleteList; }); if (activeList === deleteList.id) activeList = 0; deleteList = null;
            var m = modal("ct-list-del-modal"); if (m) m.hide(); renderLists();
        });
    });

    // ── export (44d) ──
    var exportSet = [];
    // Zonder toestemming om te contacteren (of, bij een nieuwsbriefexport, zonder toestemming voor de nieuwsbrief) valt een contact weg.
    function refreshExportNote() {
        var news = $("#ct-x-news").checked, idx = news ? 1 : 0;
        var skipped = exportSet.filter(function (tr) { return tr.dataset.consent.charAt(idx) !== "1"; }).length, n = exportSet.length - skipped;
        var what = news ? " zonder toestemming voor de nieuwsbrief" : " zonder toestemming";
        $("#ct-x-note").textContent = (skipped > 0 ? skipped + (skipped === 1 ? " contact" + what + " wordt" : " contacten" + what + " worden") + " niet meegenomen. " : "") + "Elke export wordt gelogd (wie, wanneer, welke lijst).";
        $("#ct-x-go-text").textContent = n + (n === 1 ? " contact exporteren" : " contacten exporteren"); $("#ct-x-go").disabled = n === 0;
    }
    function openExport() {
        var sel = selected(), set = sel.length ? sel : appliedRows(), listName = activeList ? (lists.filter(function (l) { return l.id === activeList; })[0] || {}).name : "";
        exportSet = set;
        $("#ct-x-news").checked = false; refreshExportNote();
        $("#ct-export-sub").textContent = (sel.length ? "selectie" : listName ? 'lijst "' + listName + '"' : "huidige lijst") + " · " + set.length + (set.length === 1 ? " contact" : " contacten");
        $("#ct-x-list").value = listName || "";
        var m = modal("ct-export-modal"); if (m) m.show();
    }
    ["ct-export", "ct-export-m", "ct-sel-export"].forEach(function (i) { var b = document.getElementById(i); if (b) b.addEventListener("click", openExport); });
    var fmt = "xlsx";
    $("#ct-x-news").addEventListener("change", refreshExportNote);
    $("#ct-x-formats").addEventListener("click", function (e) {
        var b = e.target.closest("[data-format]"); if (!b) return; fmt = b.dataset.format;
        $$("#ct-x-formats button").forEach(function (x) { var on = x === b; x.classList.toggle("is-on", on); x.setAttribute("aria-checked", on ? "true" : "false"); });
        $("#ct-x-pdfhint").hidden = fmt !== "pdf";
    });
    $("#ct-x-go").addEventListener("click", function () {
        $("#ct-x-ids").value = ids(exportSet); $("#ct-x-newsletter").value = $("#ct-x-news").checked ? "true" : "false"; $("#ct-x-format").value = fmt;
        $("#ct-x-columns").value = $$("#ct-x-cols input:checked").map(function (c) { return c.value; }).join(",");
        $("#ct-export-form").submit();
        var m = modal("ct-export-modal"); if (m) setTimeout(function () { m.hide(); }, 400);
    });

    // ── DataTable + paginagrootte ──
    var card = $(".gl-v2-ct-card");
    table = new DataTable("#datatable-contacten", {
        order: [[6, "asc"]], autoWidth: false, columnDefs: [{ targets: [0, 8], orderable: false, searchable: false }], layout: { topStart: { buttons: [] } },
        language: { zeroRecords: "Geen contacten gevonden met deze filters.", emptyTable: "Nog geen contacten in dit project.", paginate: { previous: "‹", next: "›" }, aria: { sortAscending: ": oplopend sorteren", sortDescending: ": aflopend sorteren" } },
        infoCallback: function (settings, start, end, max, total) { if (total === 0) return "0 contacten"; return (end - start + 1) + " van " + total + " contacten" + (total !== max ? " · gefilterd" : ""); }
    });
    var dtSearch = document.querySelector(".dt-search"); if (dtSearch) dtSearch.style.display = "none";
    table.on("draw", function () { updateSelection(); });
    var ROW_HEIGHT = 55;   // td 54px + 1px rand, zie gl-v2-projecten-contacten.css
    function syncPageLength() {
        if (!table) return;
        var contentEl = document.querySelector(".gl-v2-content"), thead = $("#datatable-contacten thead"), dtc = tableEl.closest(".dt-container");
        if (!contentEl || !thead || !dtc || !card) return;
        var footer = dtc.querySelector(".dt-layout-row:last-child"), cs = getComputedStyle(contentEl);
        var top = card.getBoundingClientRect().top - contentEl.getBoundingClientRect().top + contentEl.scrollTop;
        var available = contentEl.clientHeight - (parseFloat(cs.paddingBottom) || 0) - top - thead.offsetHeight - (footer ? footer.offsetHeight + 4 : 52) - 8 - (bar && !bar.hidden ? bar.offsetHeight + 8 : 0);   // de selectiebalk neemt onderaan plaats in zodra ze zichtbaar is
        var maxRows = Math.max(Math.floor(available / ROW_HEIGHT), 5), n = table.rows({ search: "applied" }).count();
        var len = n ? Math.min(maxRows, Math.max(n, 5)) : maxRows;
        if (table.page.len() !== len) table.page.len(len).draw(false);
    }
    var rt = null; window.addEventListener("resize", function () { clearTimeout(rt); rt = setTimeout(syncPageLength, 150); });
    refreshChrome(); table.draw(); syncPageLength(); renderLists();
    if (card) card.classList.add("is-ready");
    if (/[?&]lijsten=1/.test(location.search)) showLists(true);
})();
