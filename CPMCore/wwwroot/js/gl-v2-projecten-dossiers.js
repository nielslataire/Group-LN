// gl-v2 — Projecten/Dossiers/IndexV2 (design-handoff 31a/31b/31e). Tabs (Nutsaanvragen / Alle dossiers, onthouden via ?tab=),
// "+ Nieuw"-ingangen (Bulk per eenheid, Ander dossier), de lijst met zoek/type/open/kolommen en de bulk-modal die per eenheid toont
// of er een dossier aangemaakt of overgeslagen wordt.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-dos-root");
    if (!root) return;
    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }
    var cfg = JSON.parse((document.getElementById("gl-v2-dos-config") || { textContent: "{}" }).textContent);
    var canWrite = root.getAttribute("data-can-write") === "1";
    function modalFor(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }

    // ── Tabs ───────────────────────────────────────────────────────────────────────────────────────
    var tabs = $$("#gl-v2-dos-tabs [data-dos-tab]");
    function showTab(name, push) {
        tabs.forEach(function (b) {
            var on = b.getAttribute("data-dos-tab") === name;
            b.classList.toggle("is-active", on);
            b.setAttribute("aria-selected", on ? "true" : "false");
        });
        $$("[data-dos-panel]").forEach(function (p) { p.hidden = p.getAttribute("data-dos-panel") !== name; });
        if (push !== false && window.history && history.replaceState) {
            var url = new URL(window.location.href);
            url.searchParams.set("tab", name); url.searchParams.delete("hl"); url.searchParams.delete("kind");
            history.replaceState(null, "", url.toString());
        }
    }
    tabs.forEach(function (b) {
        b.addEventListener("click", function () { showTab(b.getAttribute("data-dos-tab")); });
        b.addEventListener("keydown", function (e) {
            var i = tabs.indexOf(b), n = null;
            if (e.key === "ArrowRight") n = tabs[(i + 1) % tabs.length];
            else if (e.key === "ArrowLeft") n = tabs[(i - 1 + tabs.length) % tabs.length];
            if (n) { e.preventDefault(); n.focus(); n.click(); }
        });
    });

    // ── "+ Nieuw" ──────────────────────────────────────────────────────────────────────────────────
    document.addEventListener("click", function (e) {
        var b = e.target.closest(".js-dos-bulk");
        if (b && canWrite && !b.disabled) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            openBulk();
            return;
        }
        var a = e.target.closest(".js-dos-ander");
        if (a && canWrite && window.GlV2DosAnder) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            window.GlV2DosAnder.open({});
        }
    });

    // ── 31b · lijst ────────────────────────────────────────────────────────────────────────────────
    var table = document.getElementById("gl-v2-dos-table");
    var groep = root.getAttribute("data-start-groep") || "", openFilter = "alles";
    function applyFilters() {
        if (!table) return;
        var q = (($("#gl-v2-dos-search") || {}).value || "").trim().toLowerCase();
        var shown = 0;
        $$("tbody .gl-v2-dos-row", table).forEach(function (tr) {
            var ok = true;
            if (groep && tr.getAttribute("data-groep") !== groep) ok = false;
            if (ok && openFilter === "open" && tr.getAttribute("data-open") !== "1") ok = false;
            if (ok && q && tr.getAttribute("data-zoek").indexOf(q) < 0) ok = false;
            tr.hidden = !ok;
            if (ok) shown++;
        });
        var none = document.getElementById("gl-v2-dos-none"); if (none) none.hidden = shown !== 0;
        var c = document.getElementById("gl-v2-dos-count"); if (c) c.textContent = shown + (shown === 1 ? " dossier" : " dossiers");
    }
    $$("#gl-v2-dos-typefilter button").forEach(function (b) {
        b.addEventListener("click", function () {
            groep = b.getAttribute("data-dos-groep");
            $$("#gl-v2-dos-typefilter button").forEach(function (o) { o.classList.toggle("is-active", o === b); });
            applyFilters();
        });
    });
    $$("#gl-v2-dos-openfilter button").forEach(function (b) {
        b.addEventListener("click", function () {
            openFilter = b.getAttribute("data-dos-open");
            $$("#gl-v2-dos-openfilter button").forEach(function (o) { o.classList.toggle("is-active", o === b); });
            applyFilters();
        });
    });
    var search = document.getElementById("gl-v2-dos-search"), clear = document.getElementById("gl-v2-dos-search-clear");
    if (search) search.addEventListener("input", function () { if (clear) clear.style.display = search.value ? "" : "none"; applyFilters(); });
    if (clear) { clear.style.display = "none"; clear.addEventListener("click", function () { search.value = ""; clear.style.display = "none"; applyFilters(); search.focus(); }); }
    $$("[data-dos-col]").forEach(function (cb) { cb.addEventListener("change", function () { if (table) table.classList.toggle("hide-" + cb.getAttribute("data-dos-col"), !cb.checked); }); });
    if (table) table.addEventListener("click", function (e) {
        if (e.target.closest("a, button")) return;
        var row = e.target.closest(".gl-v2-dos-row");
        if (row) window.location.href = row.getAttribute("data-href");
    });
    if (groep) {
        var gb = $('#gl-v2-dos-typefilter [data-dos-groep="' + groep + '"]');
        if (gb) { $$("#gl-v2-dos-typefilter button").forEach(function (o) { o.classList.toggle("is-active", o === gb); }); }
        else groep = "";
    }
    applyFilters();

    // ── 31e · bulk per eenheid ─────────────────────────────────────────────────────────────────────
    var TYPES = { "0": { naam: "Elektriciteit", ean: "ean-elek", kop: "EAN (UIT EENHEID)" }, "1": { naam: "Gas", ean: "ean-gas", kop: "EAN (UIT EENHEID)" }, "2": { naam: "Water", ean: "meter-water", kop: "METERNUMMER (UIT EENHEID)" } };
    var bestaand = {}; (cfg.bestaand || []).forEach(function (k) { bestaand[k] = true; });
    var bulkForm = document.getElementById("gl-v2-dos-bulk-form");
    var titelEdited = false;
    function bulkType() { var r = $$('input[name="NutsType"]', bulkForm).filter(function (x) { return x.checked; })[0]; return r ? r.value : "0"; }
    function refreshBulk() {
        var t = bulkType(), info = TYPES[t] || TYPES["0"], aan = 0, skip = 0, skipNames = [];
        $("#gl-v2-dos-bulk-eanh").textContent = info.kop;
        $$(".gl-v2-dos-bulk-unit", bulkForm).forEach(function (row) {
            var cb = $("input", row), uid = row.getAttribute("data-unit-id");
            var exists = !!bestaand[uid + ":" + t];
            var val = row.getAttribute("data-" + info.ean);
            var eanEl = $(".gl-v2-dos-bulk-ean", row);
            eanEl.textContent = val || "nog niet gekend";
            eanEl.classList.toggle("is-unknown", !val);
            var state = $(".gl-v2-dos-bulk-state", row);
            row.classList.remove("is-skip", "is-off");
            if (exists) { state.textContent = "wordt overgeslagen"; row.classList.add("is-skip"); }
            else if (!cb.checked) { state.textContent = "niet aangevinkt"; row.classList.add("is-off"); }
            else { state.textContent = "wordt aangemaakt"; }
            if (cb.checked) { if (exists) { skip++; skipNames.push($(".gl-v2-dos-bulk-name", row).textContent); } else aan++; }
        });
        var note = document.getElementById("gl-v2-dos-bulk-skip-note");
        if (skip > 0) {
            note.hidden = false;
            note.textContent = "Kies je " + info.naam + ", dan worden " + skipNames.join(" en ") + " overgeslagen: ze hebben al een open " + info.naam.toLowerCase() + "dossier.";
        } else note.hidden = true;
        $("#gl-v2-dos-bulk-count").innerHTML = "<b>" + aan + " " + (aan === 1 ? "dossier" : "dossiers") + "</b> " + (aan === 1 ? "wordt" : "worden") + " aangemaakt · " + skip + " overgeslagen";
        $("#gl-v2-dos-bulk-submit-label").textContent = aan + " " + (aan === 1 ? "dossier" : "dossiers") + " aanmaken";
        $("#gl-v2-dos-bulk-submit").disabled = aan === 0;
        var titel = document.getElementById("gl-v2-dos-bulk-titel");
        if (!titelEdited) titel.value = info.naam + " — {eenheid}";
        var help = document.getElementById("gl-v2-dos-bulk-titel-help");
        var namen = $$(".gl-v2-dos-bulk-name", bulkForm).slice(0, 2).map(function (n) { return '"' + titel.value.replace(/\{eenheid\}/gi, n.textContent) + '"'; });
        help.textContent = /\{eenheid\}/i.test(titel.value) ? "wordt " + namen.join(", ") + (namen.length > 1 ? "…" : "") : "";
    }
    function openBulk() {
        if (!bulkForm) return;
        titelEdited = false;
        var nb = cfg.netbeheerders && cfg.netbeheerders[bulkType()];
        var m = modalFor("gl-v2-dos-bulk-modal");
        if (m) m.show();
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        applyNetbeheerder();
        refreshBulk();
    }
    function applyNetbeheerder() {
        // Hetzelfde type heeft in dit project meestal dezelfde netbeheerder: stel de meest gebruikte voor.
        var nb = cfg.netbeheerders && cfg.netbeheerders[bulkType()];
        var root = document.getElementById("gl-v2-dos-bulk-nb"); // hidden input van het combo-veld
        if (!root || !nb || root.value) return;
        var combo = root.closest("[data-gl-v2-combo]");
        if (combo && window.GlV2Combo) window.GlV2Combo.setValue(combo, nb.id, nb.naam);
    }
    if (bulkForm) {
        bulkForm.addEventListener("change", function (e) {
            if (e.target.name === "NutsType") { var root = document.getElementById("gl-v2-dos-bulk-nb"); if (root) root.value = ""; applyNetbeheerder(); }
            refreshBulk();
        });
        document.getElementById("gl-v2-dos-bulk-titel").addEventListener("input", function () { titelEdited = true; refreshBulk(); });
        bulkForm.addEventListener("click", function (e) {
            var a = e.target.closest("[data-bulk-all]");
            if (!a) return;
            var on = a.getAttribute("data-bulk-all") === "1";
            $$('input[name="UnitIds"]', bulkForm).forEach(function (cb) { cb.checked = on; });
            refreshBulk();
        });
        bulkForm.addEventListener("submit", function (e) {
            var aan = $$(".gl-v2-dos-bulk-unit", bulkForm).filter(function (r) { return $("input", r).checked && !r.classList.contains("is-skip"); }).length;
            if (aan === 0) e.preventDefault();
        });
    }

    // ── Start ──────────────────────────────────────────────────────────────────────────────────────
    showTab(root.getAttribute("data-start-tab") || "nuts", false);
    var hl = root.getAttribute("data-highlight");
    if (hl) {
        var target = $('.gl-v2-dos-row[data-dossier-id="' + hl + '"]');
        if (target) { target.classList.add("is-highlight"); setTimeout(function () { target.scrollIntoView({ block: "center", behavior: "smooth" }); }, 120); }
    }
})();
