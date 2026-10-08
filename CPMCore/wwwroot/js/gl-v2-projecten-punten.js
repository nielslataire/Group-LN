// gl-v2 — Punten, lijst (design-handoff 40a) + selectiebalk (40n).
// Echte DataTable (zelfde recept als Leveranciers/Facturen): sortering + paginering van DataTables, paginagrootte uit de
// beschikbare schermhoogte. Filters/zoeken zijn een $.fn.dataTable.ext.search op de data-* van de rijen (alle punten van het
// project zijn al geladen); de selectiebalk post naar ProjectsIssues/BulkV2 (status, deadline, aannemer, goedkeuren, verwijderen).
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-pt-root");
    if (!root) return;

    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }

    var tableEl = document.getElementById("datatable-punten");
    var body = $("#pt-body");
    var table = null;
    function allRows() { return table ? table.rows().nodes().toArray() : $$(".gl-v2-pt-row", body); }
    function appliedRows() { return table.rows({ search: "applied" }).nodes().toArray(); }
    var search = $("#pt-search"), searchClear = $("#pt-search-clear");
    var toggle = $("#pt-filters-toggle"), panel = $("#pt-filters-panel"), countBadge = $("#pt-filters-count");
    var chips = $("#pt-chips"), clearBtn = $("#pt-filters-clear");
    var filters = {
        status: { el: $("#pt-f-status"), def: "open", name: "Status" },
        unit: { el: $("#pt-f-unit"), def: "", name: "Eenheid" },
        contractor: { el: $("#pt-f-contractor"), def: "", name: "Aannemer" },
        phase: { el: $("#pt-f-phase"), def: "", name: "Fase" },
        due: { el: $("#pt-f-due"), def: "", name: "Deadline" },
        priority: { el: $("#pt-f-priority"), def: "", name: "Prioriteit" },
        plan: { el: $("#pt-f-plan"), def: "", name: "Op plan" }
    };
    var sortKey = "", sortDir = 1;

    function val(k) { var f = filters[k]; return f.el ? f.el.value : f.def; }
    function labelOf(k) {
        var wrap = filters[k].el && filters[k].el.closest("[data-gl-v2-select]");
        var l = wrap && wrap.querySelector(".gl-v2-select-trigger-label");
        return l ? l.textContent.trim() : val(k);
    }
    function isoToday() { var d = new Date(); return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }
    function isoPlus(days) { var d = new Date(); d.setDate(d.getDate() + days); return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }

    function rowVisible(tr, q) {
        var st = val("status");
        if (st === "open") { if (tr.dataset.closed === "1") return false; }
        else if (st !== "all" && tr.dataset.status !== st) return false;
        if (val("unit") !== "" && tr.dataset.unit !== val("unit")) return false;
        if (val("contractor") !== "" && tr.dataset.contractor !== val("contractor")) return false;
        if (val("phase") !== "" && tr.dataset.phase !== val("phase")) return false;
        if (val("priority") !== "" && tr.dataset.priority !== val("priority")) return false;
        var pl = val("plan"); if (pl === "yes" && tr.dataset.plan !== "1") return false; if (pl === "no" && tr.dataset.plan === "1") return false;
        var due = val("due"), d = tr.dataset.due || "";
        if (due === "over" && tr.dataset.overdue !== "1") return false;
        if (due === "week" && !(d && d >= isoToday() && d <= isoPlus(6))) return false;
        if (due === "none" && d) return false;
        if (q && tr.dataset.search.indexOf(q) < 0) return false;
        return true;
    }
    $.fn.dataTable.ext.search.push(function (settings, data, idx) {
        if (settings.nTable !== tableEl) return true;
        var tr = settings.aoData[idx] && settings.aoData[idx].nTr;
        return tr ? rowVisible(tr, search.value.trim().toLowerCase()) : true;
    });

    function refreshChrome() {
        if (searchClear) searchClear.hidden = !search.value;
        var active = Object.keys(filters).filter(function (k) { return val(k) !== filters[k].def; });
        countBadge.textContent = active.length; countBadge.hidden = active.length === 0;
        toggle.classList.toggle("has-active", active.length > 0);
        chips.innerHTML = "";
        active.forEach(function (k) {
            var c = document.createElement("button"); c.type = "button"; c.className = "gl-v2-filters-chip";
            c.setAttribute("aria-label", filters[k].name + "-filter verwijderen");
            var t = document.createElement("span"); t.textContent = filters[k].name + ": " + labelOf(k); c.appendChild(t);
            var i = document.createElement("i"); i.className = "ph ph-x"; i.setAttribute("aria-hidden", "true"); c.appendChild(i);
            c.addEventListener("click", function () { setFilter(k, filters[k].def); });
            chips.appendChild(c);
        });
        clearBtn.hidden = active.length === 0;
    }
    function apply() {
        // verborgen rijen mogen niet geselecteerd blijven
        allRows().forEach(function (tr) {
            if (!rowVisible(tr, search.value.trim().toLowerCase())) { var cb = $(".js-pt-check", tr); if (cb) cb.checked = false; }
        });
        refreshChrome();
        table.draw();
        syncPageLength();
    }

    function setFilter(k, v) {
        var wrap = filters[k].el && filters[k].el.closest("[data-gl-v2-select]");
        var opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + v + '"]');
        if (opt) opt.click();
        else if (filters[k].el) { filters[k].el.value = v; apply(); }
    }

    Object.keys(filters).forEach(function (k) { if (filters[k].el) filters[k].el.addEventListener("change", apply); });
    search.addEventListener("input", apply);
    if (searchClear) searchClear.addEventListener("click", function () { search.value = ""; search.focus(); apply(); });
    toggle.addEventListener("click", function () {
        var open = panel.hidden; panel.hidden = !open;
        toggle.classList.toggle("is-open", open); toggle.setAttribute("aria-expanded", open ? "true" : "false");
        syncPageLength();
    });
    clearBtn.addEventListener("click", function () { Object.keys(filters).forEach(function (k) { if (val(k) !== filters[k].def) setFilter(k, filters[k].def); }); });

    // ── Rij openen ──
    body.addEventListener("click", function (e) {
        var tr = e.target.closest(".gl-v2-pt-row"); if (!tr) return;
        if (e.target.closest("input, button, a, .gl-v2-menu, .gl-v2-pt-actions")) return;
        window.location.href = tr.dataset.href;
    });

    // ── Selectie (40n) ──
    var bar = $("#pt-selbar"), checkAll = $("#pt-check-all");
    function selected() { return appliedRows().filter(function (tr) { var cb = $(".js-pt-check", tr); return cb && cb.checked; }); }
    function updateSelection() {
        if (!bar) return;
        var sel = selected();
        allRows().forEach(function (tr) { var cb = $(".js-pt-check", tr); tr.classList.toggle("is-selected", !!(cb && cb.checked)); });
        bar.hidden = sel.length === 0;
        $("#pt-sel-n").textContent = sel.length;
        $$(".js-pt-sel-n", bar).forEach(function (n) { n.textContent = sel.length; });
        if (sel.length === 0) closePops();
        if (checkAll) {
            var vis = appliedRows().length;
            checkAll.checked = vis > 0 && sel.length === vis;
            checkAll.indeterminate = sel.length > 0 && sel.length < vis;
        }
    }
    body.addEventListener("change", function (e) { if (e.target.classList.contains("js-pt-check")) updateSelection(); });
    if (checkAll) checkAll.addEventListener("change", function () {
        appliedRows().forEach(function (tr) { var cb = $(".js-pt-check", tr); if (cb) cb.checked = checkAll.checked; });
        updateSelection();
    });
    var selClear = $("#pt-sel-clear");
    if (selClear) selClear.addEventListener("click", function () { allRows().forEach(function (tr) { var cb = $(".js-pt-check", tr); if (cb) cb.checked = false; }); updateSelection(); });

    function closePops() { $$(".gl-v2-pt-pop").forEach(function (p) { p.hidden = true; }); $$(".js-pt-pop").forEach(function (b) { b.classList.remove("is-open"); }); }
    document.addEventListener("click", function (e) {
        var open = e.target.closest(".js-pt-pop");
        if (open) {
            var p = document.getElementById(open.dataset.pop), wasOpen = !p.hidden;
            closePops(); if (!wasOpen) { p.hidden = false; open.classList.add("is-open"); }
            return;
        }
        if (e.target.closest(".js-pt-pop-close")) { closePops(); return; }
        if (!e.target.closest(".gl-v2-pt-pop") && !e.target.closest(".gl-v2-datepicker-panel")) closePops();
    });

    function submitBulk(op, extra) {
        var ids = selected().map(function (tr) { return tr.dataset.id; });
        if (!ids.length) return;
        $("#pt-bulk-op").value = op;
        $("#pt-bulk-status").value = extra.status || ""; $("#pt-bulk-due").value = extra.due || ""; $("#pt-bulk-contractor").value = extra.contractor || "";
        var holder = $("#pt-bulk-ids"); holder.innerHTML = "";
        ids.forEach(function (id) { var i = document.createElement("input"); i.type = "hidden"; i.name = "issueIds"; i.value = id; holder.appendChild(i); });
        $("#pt-bulk-form").submit();
    }
    if (bar) {
        bar.addEventListener("click", function (e) {
            var b = e.target.closest("[data-op]"); if (!b) return;
            var op = b.dataset.op;
            if (op === "approve") {
                var q = selected().map(function (tr) { return "ids=" + tr.dataset.id; }).join("&");
                window.location.href = (root.dataset.sendUrl || "") + (q ? "?" + q : "");
            }
            else if (op === "status") submitBulk("status", { status: b.dataset.status });
            else if (op === "contractor") submitBulk("contractor", { contractor: b.dataset.contractor });
            else if (op === "deadline") { var d = $("#pt-due-input").value; if (d) submitBulk("deadline", { due: d }); }
        });
        var del = $("#pt-sel-delete");
        if (del) del.addEventListener("click", function () {
            var n = selected().length;
            var m = document.getElementById("pt-delete-modal"); if (!m || !window.bootstrap) return;
            m.dataset.bulk = "1";
            $(".gl-v2-modal-title", m).textContent = n + " punten verwijderen?";
            m.querySelector("form").action = $("#pt-bulk-form").action;
            m.querySelectorAll("input[data-pt]").forEach(function (i) { i.remove(); });
            $(".gl-v2-modal-desc", m).textContent = "De geselecteerde punten worden definitief verwijderd, met foto's en historiek.";
            var f = m.querySelector("form");
            [["op", "delete"]].concat(selected().map(function (tr) { return ["issueIds", tr.dataset.id]; })).forEach(function (kv) {
                var i = document.createElement("input"); i.type = "hidden"; i.name = kv[0]; i.value = kv[1]; i.setAttribute("data-pt", ""); f.appendChild(i);
            });
            window.bootstrap.Modal.getOrCreateInstance(m).show();
        });
    }

    // ── Eén punt verwijderen ──
    var delModal = document.getElementById("pt-delete-modal");
    document.addEventListener("click", function (e) {
        var d = e.target.closest(".js-pt-delete"); if (!d || !delModal || !window.bootstrap) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        var f = delModal.querySelector("form");
        f.querySelectorAll("input[data-pt]").forEach(function (i) { i.remove(); });
        f.action = f.dataset.singleAction.replace(/\/\d+$/, "/" + d.dataset.id);
        $(".gl-v2-modal-title", delModal).textContent = "Punt verwijderen?";
        $(".gl-v2-modal-desc", delModal).textContent = d.dataset.title + " wordt definitief verwijderd, met foto's en historiek.";
        window.bootstrap.Modal.getOrCreateInstance(delModal).show();
    });
    if (delModal) { var df = delModal.querySelector("form"); df.dataset.singleAction = df.action; }

    // ── Afdrukken: selectie, anders wat zichtbaar en open is ──
    var printBtn = $("#pt-print");
    if (printBtn) printBtn.addEventListener("click", function () {
        var sel = selected();
        var list = sel.length ? sel : appliedRows().filter(function (tr) { return tr.dataset.closed !== "1"; });
        if (!list.length) return;
        var holder = $("#pt-print-ids"); holder.innerHTML = "";
        list.forEach(function (tr) { var i = document.createElement("input"); i.type = "hidden"; i.name = "issueIds"; i.value = tr.dataset.id; holder.appendChild(i); });
        $("#pt-print-form").submit();
    });

    // ── Snel ingeven (40c) ──
    var panelEl = $("#pt-panel"), backdrop = $("#pt-panel-backdrop"), newForm = $("#pt-new-form");
    var savedCount = 0, saving = false;
    function openPanel() {
        if (!panelEl) return;
        panelEl.hidden = false; backdrop.hidden = false;
        setTimeout(function () { $("#pt-new-title").focus(); }, 30);
    }
    function closePanel() {
        if (!panelEl) return;
        panelEl.hidden = true; backdrop.hidden = true;
        if (savedCount > 0) window.location.reload();
    }
    function setSelect(id, v) {
        var wrap = document.getElementById(id + "_select");
        var opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + v + '"]');
        if (opt) opt.click();
    }
    function savePoint(next) {
        if (saving) return;
        var title = $("#pt-new-title"), help = $("#pt-new-title-help");
        if (!title.value.trim()) { title.closest(".gl-v2-field").classList.add("is-error"); help.textContent = "Geef een titel in."; title.focus(); return; }
        title.closest(".gl-v2-field").classList.remove("is-error");
        saving = true;
        [$("#pt-new-save"), $("#pt-new-save-next")].forEach(function (b) { b.disabled = true; });
        fetch(newForm.action, { method: "POST", body: new FormData(newForm), credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (j) {
                if (!j.ok) { help.textContent = j.error || "Opslaan mislukt."; title.closest(".gl-v2-field").classList.add("is-error"); return; }
                savedCount++;
                if (!next) { closePanel(); return; }
                title.value = ""; $("#pt-new-desc").value = ""; $("#pt-new-files").value = ""; $("#pt-new-files-label").textContent = "Foto's toevoegen";
                help.textContent = "kort en concreet — wat moet er gebeuren";
                var note = $("#pt-new-saved"); note.hidden = false; $("span", note).textContent = j.nr + " bewaard als concept. Eenheid, zone en aannemer blijven staan.";
                title.focus();
            })
            .catch(function () { help.textContent = "Opslaan mislukt. Controleer je verbinding."; title.closest(".gl-v2-field").classList.add("is-error"); })
            .then(function () { saving = false; [$("#pt-new-save"), $("#pt-new-save-next")].forEach(function (b) { b.disabled = false; }); });
    }
    if (panelEl) {
        document.addEventListener("click", function (e) {
            if (e.target.closest(".js-pt-new")) { openPanel(); return; }
            if (e.target.closest(".js-pt-panel-close") || e.target === backdrop) { closePanel(); return; }
            var z = e.target.closest(".js-pt-zone"); if (z) { $("#pt-new-zone").value = z.dataset.zone; return; }
        });
        $("#pt-new-save").addEventListener("click", function () { savePoint(false); });
        $("#pt-new-save-next").addEventListener("click", function () { savePoint(true); });
        $("#pt-new-title").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); savePoint(true); } });
        document.addEventListener("keydown", function (e) { if (e.key === "Escape" && !panelEl.hidden && !document.querySelector(".gl-v2-select-panel.is-open")) closePanel(); });
        var more = $("#pt-new-more");
        more.addEventListener("click", function () {
            var open = more.getAttribute("aria-expanded") !== "true";
            more.setAttribute("aria-expanded", open ? "true" : "false"); $("#pt-new-more-body").hidden = !open;
        });
        $("#pt-new-files").addEventListener("change", function (e) {
            var n = e.target.files.length; $("#pt-new-files-label").textContent = n ? n + (n === 1 ? " foto gekozen" : " foto's gekozen") : "Foto's toevoegen";
        });
    }

    // ── DataTable + paginagrootte uit de beschikbare hoogte (zelfde rekenwerk als Leveranciers/Facturen) ──
    var card = $(".gl-v2-table-card");
    table = new DataTable("#datatable-punten", {
        order: [[1, "desc"]],
        autoWidth: false,
        columnDefs: [{ targets: [0, 8], orderable: false, searchable: false }],
        layout: { topStart: { buttons: [] } },
        language: {
            zeroRecords: "Geen punten gevonden met deze filters.", emptyTable: "Nog geen punten in dit project.",
            paginate: { previous: "‹", next: "›" },
            aria: { sortAscending: ": oplopend sorteren", sortDescending: ": aflopend sorteren" }
        },
        infoCallback: function (settings, start, end, max, total) {
            if (total === 0) return "0 punten";
            return (end - start + 1) + " van " + total + " punten" + (total !== max ? " · gefilterd" : "");
        }
    });
    $(".dt-search").hide();
    table.on("draw", function () { updateSelection(); });

    var ROW_HEIGHT = 54; // in sync met gl-v2-projecten-punten.css tbody td { height }
    function syncPageLength() {
        if (!table) return;
        var contentEl = document.querySelector(".gl-v2-content"), thead = $("#datatable-punten thead"), dtc = tableEl.closest(".dt-container");
        if (!contentEl || !thead || !dtc || !card) return;
        var footer = dtc.querySelector(".dt-layout-row:last-child");
        var cs = getComputedStyle(contentEl);
        var top = card.getBoundingClientRect().top - contentEl.getBoundingClientRect().top + contentEl.scrollTop;
        var available = contentEl.clientHeight - (parseFloat(cs.paddingBottom) || 0) - top - thead.offsetHeight - (footer ? footer.offsetHeight : 48) - 16;
        var maxRows = Math.max(Math.floor(available / ROW_HEIGHT), 5);
        var n = table.rows({ search: "applied" }).count();
        var len = n ? Math.min(maxRows, Math.max(n, 5)) : maxRows;
        if (table.page.len() !== len) table.page.len(len).draw(false);
    }
    var resizeTimer = null;
    window.addEventListener("resize", function () { clearTimeout(resizeTimer); resizeTimer = setTimeout(syncPageLength, 150); });

    refreshChrome();
    table.draw();
    syncPageLength();
    if (card) card.classList.add("is-ready");
})();
