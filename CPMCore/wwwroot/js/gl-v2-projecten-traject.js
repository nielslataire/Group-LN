// gl-v2 — Projecten/IndexV2 van Traject (design-handoff punt 30). Alles wat de server niet zelf rendert:
//   tabs (Tijdlijn/Mijlpalen/Per eenheid/Kalender, onthouden via ?tab=), fases in-/uitklappen, bolletje = bereikt,
//   mijlpaalformulier (30e) en sync-voorbeeld (30f) in een modal via AJAX, tabel met zoek/filter/sortering/kolommen/
//   selectiebalk (30b), matrix met popover op de cel (30c) en de kalender met Maand/Kwartaal/Jaar/Agenda (30d).
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-tr-root");
    if (!root) return;

    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }

    var cfgEl = document.getElementById("gl-v2-tr-config");
    var cfg = cfgEl ? JSON.parse(cfgEl.textContent) : {};
    var canWrite = root.getAttribute("data-can-write") === "1";
    var currentTab = root.getAttribute("data-start-tab") || "tijdlijn";

    function tokenValue() {
        var t = $('input[name="__RequestVerificationToken"]');
        return t ? t.value : "";
    }
    function modalFor(id) {
        var el = document.getElementById(id);
        return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null;
    }
    function reloadWith(params) {
        var url = new URL(window.location.href);
        url.searchParams.set("tab", currentTab);
        url.searchParams.delete("hl");
        url.searchParams.delete("nieuw");
        Object.keys(params || {}).forEach(function (k) {
            if (params[k] == null) url.searchParams.delete(k); else url.searchParams.set(k, params[k]);
        });
        window.location.href = url.toString();
    }
    function toast(tone, title, body) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body });
    }

    // ── Tabs ───────────────────────────────────────────────────────────────────────────────────────
    var tabs = $$("#gl-v2-tr-tabs [data-tr-tab]");
    var onShow = { kalender: function () { calRender(); } };
    function showTab(name, push) {
        currentTab = name;
        tabs.forEach(function (b) {
            var on = b.getAttribute("data-tr-tab") === name;
            b.classList.toggle("is-active", on);
            b.setAttribute("aria-selected", on ? "true" : "false");
        });
        $$("[data-tr-panel]").forEach(function (p) { p.hidden = p.getAttribute("data-tr-panel") !== name; });
        if (push !== false && window.history && history.replaceState) {
            var url = new URL(window.location.href);
            url.searchParams.set("tab", name);
            url.searchParams.delete("hl");
            url.searchParams.delete("nieuw");
            history.replaceState(null, "", url.toString());
        }
        if (onShow[name]) onShow[name]();
    }
    tabs.forEach(function (b) {
        b.addEventListener("click", function () { showTab(b.getAttribute("data-tr-tab")); });
        b.addEventListener("keydown", function (e) {
            var i = tabs.indexOf(b), n = null;
            if (e.key === "ArrowRight") n = tabs[(i + 1) % tabs.length];
            else if (e.key === "ArrowLeft") n = tabs[(i - 1 + tabs.length) % tabs.length];
            if (n) { e.preventDefault(); n.focus(); n.click(); }
        });
    });

    // ── Fases in-/uitklappen + Stappenplan klikt naar de fase ─────────────────────────────────────────
    function toggleFase(section, open) {
        var head = $(".gl-v2-tr-fase-head", section), body = $(".gl-v2-tr-fase-body", section);
        if (!head || !body) return;
        var want = open == null ? !section.classList.contains("is-open") : open;
        section.classList.toggle("is-open", want);
        head.setAttribute("aria-expanded", want ? "true" : "false");
        body.hidden = !want;
    }
    root.addEventListener("click", function (e) {
        var head = e.target.closest(".gl-v2-tr-fase-head");
        if (head) toggleFase(head.closest(".gl-v2-tr-fase"));
    });
    var steps = document.getElementById("gl-v2-tr-steps");
    if (steps) steps.addEventListener("gl-v2-steps:step", function (e) {
        var fases = $$(".gl-v2-tr-fase");
        var f = fases[e.detail.index];
        if (!f) return;
        toggleFase(f, true);
        f.scrollIntoView({ behavior: "smooth", block: "start" });
    });
    root.addEventListener("click", function (e) {
        var go = e.target.closest("[data-tr-goto]");
        if (!go) return;
        showTab(go.getAttribute("data-tr-goto"));
        var f = go.getAttribute("data-tr-filter");
        if (f) setStatusFilter(f);
    });

    // ── Snelle acties: bolletje en "Bereikt" ──────────────────────────────────────────────────────────
    function snel(id, fields) {
        var body = new URLSearchParams();
        Object.keys(fields).forEach(function (k) { if (fields[k] != null && fields[k] !== "") body.set(k, fields[k]); });
        return fetch(cfg.snelUrl.replace(/\/0\/Snel/, "/" + id + "/Snel"), {
            method: "POST",
            headers: { "RequestVerificationToken": tokenValue(), "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" },
            body: body.toString(),
            credentials: "same-origin"
        }).then(function (r) { if (!r.ok) throw new Error(); return r.json(); });
    }
    document.addEventListener("click", function (e) {
        var t = e.target.closest(".js-tr-toggle");
        if (!t || !canWrite || t.disabled) return;
        e.stopPropagation();
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        var id = t.getAttribute("data-ms-id");
        var wasDone = t.getAttribute("data-done") === "1";
        t.disabled = true;
        snel(id, { bereikt: wasDone ? "false" : "true" })
            .then(function () { reloadWith({ hl: id }); })
            .catch(function () { t.disabled = false; toast("danger", "Niet gelukt", "De mijlpaal kon niet bijgewerkt worden."); });
    });

    // ── Rij/knop → formulier (30e) ────────────────────────────────────────────────────────────────────
    function openForm(opts) {
        var m = modalFor("gl-v2-tr-modal");
        var body = document.getElementById("gl-v2-tr-modal-body");
        if (!m || !body) return;
        body.innerHTML = '<div class="gl-v2-tr-loading">Laden…</div>';
        $("#gl-v2-tr-modal-title").textContent = opts.id ? "Mijlpaal bewerken" : "Mijlpaal toevoegen";
        $("#gl-v2-tr-modal-sub").textContent = "";
        $("#gl-v2-tr-modal-savenew").hidden = !!opts.id;
        m.show();
        var qs = "?projectId=" + encodeURIComponent(cfg.projectId);
        if (opts.id) qs += "&id=" + encodeURIComponent(opts.id);
        if (opts.faseId) qs += "&faseId=" + encodeURIComponent(opts.faseId);
        if (opts.unitId) qs += "&unitId=" + encodeURIComponent(opts.unitId);
        fetch(cfg.modalUrl.split("?")[0] + qs, { credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) {
                body.innerHTML = html;
                wireForm(body, opts);
            })
            .catch(function () { body.innerHTML = '<div class="gl-v2-tr-loading">Kon het formulier niet laden.</div>'; });
    }
    root.addEventListener("click", function (e) {
        if (e.target.closest("a, button, input, label, .gl-v2-menu, select")) return;
        var row = e.target.closest(".gl-v2-tr-ms[data-editable], .gl-v2-tr-trow");
        if (row && canWrite) openForm({ id: row.getAttribute("data-ms-id") });
    });
    document.addEventListener("click", function (e) {
        var ed = e.target.closest(".js-tr-edit");
        if (ed && canWrite) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            openForm({ id: ed.getAttribute("data-ms-id") });
            return;
        }
        var add = e.target.closest(".js-tr-add");
        if (add && canWrite) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            var fid = add.getAttribute("data-fase-id");
            openForm({ faseId: fid && fid !== "0" ? fid : null, geldt: add.getAttribute("data-geldt") });
        }
    });

    function setSelect(wrapId, value) {
        var wrap = document.getElementById(wrapId + "_select");
        if (!wrap) return;
        var opt = wrap.querySelector('.gl-v2-select-option[data-value="' + String(value).replace(/"/g, "") + '"]');
        if (opt) opt.click();
    }
    function hiddenOf(id) { return document.getElementById(id); }

    function wireForm(body, opts) {
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(body);
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        var rootEl = $("#gl-v2-tr-form-root", body);
        if (!rootEl) return;
        $("#gl-v2-tr-modal-title").textContent = rootEl.getAttribute("data-title");
        $("#gl-v2-tr-modal-sub").textContent = rootEl.getAttribute("data-sub");
        var isNew = rootEl.getAttribute("data-nieuw") === "1";
        var mijlpalen = [];
        try { mijlpalen = JSON.parse(rootEl.getAttribute("data-mijlpalen") || "[]"); } catch (x) { }

        // Plaats in de fase: opties volgen de gekozen fase (30e "vervangt het veld Volgorde").
        var plaatsWrap = document.getElementById("gl-v2-tr-f-plaats_select");
        function refreshPlaats() {
            if (!plaatsWrap) return;
            var fase = (hiddenOf("gl-v2-tr-f-fase") || {}).value;
            var items = [];
            if (!isNew) items.push({ value: "", text: "Huidige plaats houden" });
            items.push({ value: "eerste", text: "Als eerste" });
            mijlpalen.filter(function (m) { return String(m.faseId) === String(fase); })
                .forEach(function (m) { items.push({ value: "na:" + m.id, text: "Na " + m.naam }); });
            items.push({ value: "laatste", text: "Als laatste" });
            window.GlV2Select.setItems(plaatsWrap, items, isNew ? "laatste" : "", null);
            // setItems laat een niet-gevonden waarde leeg; zet de standaard expliciet
            var h = hiddenOf("gl-v2-tr-f-plaats");
            if (h && !h.value && isNew) setSelect("gl-v2-tr-f-plaats", "laatste");
        }
        var faseHidden = hiddenOf("gl-v2-tr-f-fase");
        if (faseHidden) faseHidden.addEventListener("change", refreshPlaats);
        refreshPlaats();
        if (!isNew) {
            // bij bewerken: sub-titel met de fase volgt de keuze niet; laten staan.
        }

        // Geldt voor
        var geldt = $$('input[name="geldtVoor"]', body);
        var unitWrap = document.getElementById("gl-v2-tr-f-unitwrap");
        var geldtHelp = document.getElementById("gl-v2-tr-f-geldt-help");
        function applyGeldt() {
            var v = (geldt.filter(function (r) { return r.checked; })[0] || {}).value;
            if (unitWrap) unitWrap.hidden = v !== "unit";
            if (geldtHelp) geldtHelp.hidden = v !== "alle";
            var unitInput = hiddenOf("gl-v2-tr-f-unit");
            if (unitInput) unitInput.disabled = v !== "unit";
        }
        geldt.forEach(function (r) { r.addEventListener("change", applyGeldt); });
        if (opts && opts.geldt === "alle") {
            var alle = geldt.filter(function (r) { return r.value === "alle"; })[0];
            if (alle && !alle.disabled) alle.checked = true;
        }
        applyGeldt();

        // Status ↔ "Bereikt op"
        var statusRadios = $$('input[name="Status"]', body);
        var werkWrap = document.getElementById("gl-v2-tr-f-werkwrap");
        var werkHelp = document.getElementById("gl-v2-tr-f-werk-help");
        function applyStatus() {
            var v = (statusRadios.filter(function (r) { return r.checked; })[0] || {}).value;
            var on = v === "2";
            if (werkWrap) werkWrap.classList.toggle("is-off", !on);
            if (werkHelp) werkHelp.hidden = on;
            var inp = hiddenOf("gl-v2-tr-f-werk_text");
            if (inp) inp.disabled = !on;
            if (!on) {
                var h = hiddenOf("gl-v2-tr-f-werk"); if (h) h.value = "";
                if (inp) inp.value = "";
            }
        }
        statusRadios.forEach(function (r) { r.addEventListener("change", applyStatus); });
        applyStatus();

        // Relatief: bij een aantal dagen zonder anker kiezen we de vorige mijlpaal in de fase.
        var offset = document.getElementById("gl-v2-tr-f-offset");
        var ankerWrap = $(".gl-v2-tr-rel-anker", body);
        if (offset && ankerWrap) offset.addEventListener("input", function () {
            var anker = hiddenOf("gl-v2-tr-f-anker");
            var vorige = ankerWrap.getAttribute("data-vorige");
            if (anker && !anker.value && offset.value !== "" && vorige) setSelect("gl-v2-tr-f-anker", vorige);
        });

        // Opslaan: naam verplicht; terugTab meesturen
        var form = document.getElementById("gl-v2-tr-modal-form");
        var naam = document.getElementById("gl-v2-tr-f-naam");
        if (naam) naam.addEventListener("input", function () {
            var err = $('.gl-v2-tr-err[data-for="Naam"]', body); if (err) err.hidden = true;
            naam.closest(".gl-v2-field").classList.remove("is-error");
        });
        if (naam && !naam.value) setTimeout(function () { naam.focus(); }, 250);
        form.onsubmit = function (ev) {
            if (naam && !naam.value.trim()) {
                ev.preventDefault();
                naam.closest(".gl-v2-field").classList.add("is-error");
                var err = $('.gl-v2-tr-err[data-for="Naam"]', body); if (err) err.hidden = false;
                naam.focus();
                return false;
            }
            var old = $('input[name="terugTab"]', form); if (old) old.remove();
            var t = document.createElement("input"); t.type = "hidden"; t.name = "terugTab"; t.value = currentTab; form.appendChild(t);
            // Eenheid-keuze enkel meesturen bij "Eén eenheid"
            return true;
        };
    }

    // ── Sync met sjabloon (30f) ───────────────────────────────────────────────────────────────────────
    document.addEventListener("click", function (e) {
        var s = e.target.closest(".js-tr-sync");
        if (!s || !canWrite) return;
        var m = modalFor("gl-v2-tr-sync-modal");
        var content = document.getElementById("gl-v2-tr-sync-content");
        if (!m || !content) return;
        content.innerHTML = '<div class="modal-body"><div class="gl-v2-tr-loading">Laden…</div></div>';
        m.show();
        fetch(cfg.syncUrl, { credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) { content.innerHTML = html; wireSync(content); })
            .catch(function () { content.innerHTML = '<div class="modal-body"><div class="gl-v2-tr-loading">Kon de wijzigingen niet laden.</div></div>'; });
    });
    function wireSync(content) {
        var boxes = $$(".js-tr-sync-key", content);
        var count = document.getElementById("gl-v2-tr-sync-sel");
        var apply = document.getElementById("gl-v2-tr-sync-apply");
        var label = document.getElementById("gl-v2-tr-sync-apply-label");
        function update() {
            var n = boxes.filter(function (b) { return b.checked; }).length;
            if (count) count.textContent = n;
            if (label) label.textContent = n + " " + (n === 1 ? "wijziging" : "wijzigingen") + " toepassen";
            if (apply) apply.disabled = n === 0;
            var cnt = $(".gl-v2-tr-sync-count", content);
            if (cnt) cnt.lastChild.textContent = " " + (n === 1 ? "wijziging" : "wijzigingen") + " geselecteerd";
        }
        boxes.forEach(function (b) { b.addEventListener("change", update); });
    }

    // ── Verwijderen (één of bulk) ─────────────────────────────────────────────────────────────────────
    function askDelete(ids, naam) {
        var m = modalFor("gl-v2-tr-del-modal");
        if (!m) return;
        $("#gl-v2-tr-del-title").textContent = ids.length === 1 ? "Mijlpaal verwijderen?" : ids.length + " mijlpalen verwijderen?";
        $("#gl-v2-tr-del-desc").textContent = (ids.length === 1 && naam ? "“" + naam + "” wordt verwijderd. " : "") + "Dit kan niet ongedaan gemaakt worden.";
        var holder = document.getElementById("gl-v2-tr-del-ids");
        holder.innerHTML = "";
        ids.forEach(function (id) {
            var i = document.createElement("input"); i.type = "hidden"; i.name = "mijlpaalIds"; i.value = id; holder.appendChild(i);
        });
        m.show();
    }
    document.addEventListener("click", function (e) {
        var d = e.target.closest(".js-tr-delete");
        if (!d) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        askDelete([d.getAttribute("data-ms-id")], d.getAttribute("data-naam"));
    });

    // ── 30b · Tabel: zoek, filters, sortering, kolommen, selectie ─────────────────────────────────────
    var table = document.getElementById("gl-v2-tr-table");
    var statusFilter = "all";
    function setStatusFilter(v) {
        statusFilter = v;
        $$("#gl-v2-tr-statusfilter button").forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-tr-status") === v); });
        applyFilters();
    }
    function applyFilters() {
        if (!table) return;
        var q = (($("#gl-v2-tr-search") || {}).value || "").trim().toLowerCase();
        var fase = (document.getElementById("gl-v2-tr-fasefilter") || {}).value || "";
        var shown = 0;
        $$("tbody .gl-v2-tr-trow", table).forEach(function (tr) {
            var soort = tr.getAttribute("data-soort");
            var ok = true;
            if (statusFilter === "todo") ok = soort !== "done" && soort !== "nvt";
            else if (statusFilter === "late") ok = soort === "late";
            else if (statusFilter === "done") ok = soort === "done";
            if (ok && fase && tr.getAttribute("data-fase-id") !== fase) ok = false;
            if (ok && q && (tr.getAttribute("data-naam") + " " + tr.getAttribute("data-wie")).indexOf(q) < 0) ok = false;
            tr.hidden = !ok;
            if (ok) shown++; else { tr.classList.remove("is-selected"); var cb = $(".js-tr-select", tr); if (cb) cb.checked = false; }
        });
        var none = document.getElementById("gl-v2-tr-none"); if (none) none.hidden = shown !== 0;
        var c = document.getElementById("gl-v2-tr-count"); if (c) c.textContent = shown + (shown === 1 ? " mijlpaal" : " mijlpalen");
        updateSelection();
    }
    if (table) {
        var search = document.getElementById("gl-v2-tr-search"), clear = document.getElementById("gl-v2-tr-search-clear");
        if (search) search.addEventListener("input", function () { if (clear) clear.style.display = search.value ? "" : "none"; applyFilters(); });
        if (clear) clear.addEventListener("click", function () { search.value = ""; clear.style.display = "none"; applyFilters(); search.focus(); });
        if (clear) clear.style.display = "none";
        $$("#gl-v2-tr-statusfilter button").forEach(function (b) { b.addEventListener("click", function () { setStatusFilter(b.getAttribute("data-tr-status")); }); });
        var faseFilter = document.getElementById("gl-v2-tr-fasefilter");
        if (faseFilter) faseFilter.addEventListener("change", applyFilters);
        $$('[data-tr-col]').forEach(function (cb) {
            cb.addEventListener("change", function () { table.classList.toggle("hide-" + cb.getAttribute("data-tr-col"), !cb.checked); });
        });
        // sortering
        var sortKey = "streef", sortDir = 1;
        $$("th[data-tr-sort]", table).forEach(function (th) {
            th.addEventListener("click", function () {
                var k = th.getAttribute("data-tr-sort");
                if (sortKey === k) sortDir = -sortDir; else { sortKey = k; sortDir = 1; }
                $$("th[data-tr-sort]", table).forEach(function (o) {
                    o.classList.toggle("is-sorted", o === th);
                    var ic = $("i", o); if (ic) ic.remove();
                });
                var icon = document.createElement("i");
                icon.className = "ph " + (sortDir === 1 ? "ph-arrow-up" : "ph-arrow-down");
                icon.setAttribute("aria-hidden", "true");
                th.appendChild(document.createTextNode(" ")); th.appendChild(icon);
                var tbody = $("tbody", table);
                var rows = $$(".gl-v2-tr-trow", tbody);
                rows.sort(function (a, b) {
                    var x = a.getAttribute("data-sort-" + k) || "", y = b.getAttribute("data-sort-" + k) || "";
                    return x < y ? -sortDir : x > y ? sortDir : 0;
                });
                var none = document.getElementById("gl-v2-tr-none");
                rows.forEach(function (r) { tbody.insertBefore(r, none); });
            });
        });
        // selectie
        table.addEventListener("change", function (e) {
            if (e.target.id === "gl-v2-tr-selall") {
                $$(".gl-v2-tr-trow:not([hidden]) .js-tr-select", table).forEach(function (cb) { cb.checked = e.target.checked; });
            }
            updateSelection();
        });
    }
    function selectedIds() { return $$(".js-tr-select:checked").map(function (c) { return c.value; }); }
    function updateSelection() {
        var bar = document.getElementById("gl-v2-tr-selbar");
        $$(".gl-v2-tr-trow").forEach(function (tr) {
            var cb = $(".js-tr-select", tr); tr.classList.toggle("is-selected", !!(cb && cb.checked));
        });
        if (!bar) return;
        var n = selectedIds().length;
        bar.hidden = n === 0;
        var c = document.getElementById("gl-v2-tr-selcount"); if (c) c.textContent = n;
        var all = document.getElementById("gl-v2-tr-selall");
        if (all) { var visible = $$(".gl-v2-tr-trow:not([hidden]) .js-tr-select"); all.checked = n > 0 && visible.length > 0 && visible.every(function (b) { return b.checked; }); }
    }
    var selbar = document.getElementById("gl-v2-tr-selbar");
    if (selbar) selbar.addEventListener("click", function (e) {
        var b = e.target.closest("[data-tr-bulk]");
        if (!b) return;
        var kind = b.getAttribute("data-tr-bulk"), ids = selectedIds();
        if (kind === "clear") { $$(".js-tr-select").forEach(function (c) { c.checked = false; }); var all = document.getElementById("gl-v2-tr-selall"); if (all) all.checked = false; updateSelection(); return; }
        if (kind === "verwijder") { askDelete(ids); return; }
        openBulk(kind, ids);
    });
    function openBulk(kind, ids) {
        var form = document.getElementById("gl-v2-tr-bulk-form");
        var holder = document.getElementById("gl-v2-tr-bulk-ids");
        holder.innerHTML = "";
        ids.forEach(function (id) { var i = document.createElement("input"); i.type = "hidden"; i.name = "MijlpaalIds"; i.value = id; holder.appendChild(i); });
        $$("[data-bulk-field]", form).forEach(function (f) { f.hidden = true; });
        var dateHidden = hiddenOf("gl-v2-tr-bulk-datum"), faseHidden = hiddenOf("gl-v2-tr-bulk-fase");
        if (dateHidden) dateHidden.disabled = true;
        if (faseHidden) faseHidden.disabled = true;
        var oud = $('input[name="Status"]', form); if (oud) oud.remove();
        var sub = ids.length + (ids.length === 1 ? " mijlpaal geselecteerd" : " mijlpalen geselecteerd");
        $("#gl-v2-tr-bulk-sub").textContent = sub;
        if (kind === "bereikt") {
            var s = document.createElement("input"); s.type = "hidden"; s.name = "Status"; s.value = "2"; form.appendChild(s);
            form.submit();
            return;
        }
        if (kind === "datum") {
            $("#gl-v2-tr-bulk-title").textContent = "Streefdatum aanpassen";
            $('[data-bulk-field="datum"]', form).hidden = false;
            if (dateHidden) dateHidden.disabled = false;
            if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        } else {
            $("#gl-v2-tr-bulk-title").textContent = "Naar een andere fase";
            $('[data-bulk-field="fase"]', form).hidden = false;
            if (faseHidden) faseHidden.disabled = false;
        }
        modalFor("gl-v2-tr-bulk-modal").show();
    }
    var bulkForm = document.getElementById("gl-v2-tr-bulk-form");
    if (bulkForm) bulkForm.addEventListener("submit", function (e) {
        var d = hiddenOf("gl-v2-tr-bulk-datum");
        if (d && !d.disabled && !d.value) { e.preventDefault(); toast("warning", "Kies een datum", "Geef de nieuwe streefdatum in."); return; }
        var t = document.createElement("input"); t.type = "hidden"; t.name = "terugTab"; t.value = "mijlpalen"; bulkForm.appendChild(t);
    });

    // ── 30c · Matrix: fasefilter + popover op de cel ──────────────────────────────────────────────────
    $$("#gl-v2-tr-ehfase button").forEach(function (b) {
        b.addEventListener("click", function () {
            $$("#gl-v2-tr-ehfase button").forEach(function (o) { o.classList.toggle("is-active", o === b); });
            var f = b.getAttribute("data-tr-eh-fase");
            $$("#gl-v2-tr-matrix [data-fase-id]").forEach(function (c) {
                c.classList.toggle("is-fase-hidden", !!f && c.getAttribute("data-fase-id") !== f);
            });
        });
    });
    var pop = document.getElementById("gl-v2-tr-pop"), popCell = null;
    function closePop() { if (pop) pop.hidden = true; popCell = null; }
    function setDate(id, iso) {
        var h = hiddenOf(id), t = hiddenOf(id + "_text");
        if (h) h.value = iso || "";
        if (t) t.value = iso ? iso.split("-").reverse().join("/") : "";
    }
    document.addEventListener("click", function (e) {
        var c = e.target.closest(".js-tr-cell");
        if (c && pop && canWrite) {
            e.stopPropagation();
            popCell = c;
            $("#gl-v2-tr-pop-title").textContent = c.getAttribute("data-naam") + " · " + c.getAttribute("data-unit");
            var sub = $("#gl-v2-tr-pop-sub");
            var soort = c.getAttribute("data-soort"), streef = c.getAttribute("data-streef");
            sub.textContent = (streef ? "streefdatum " + streef.split("-").reverse().slice(0, 2).join("/") : "geen streefdatum") + (soort === "late" ? " · " + c.getAttribute("data-sub") : soort === "done" ? " · bereikt" : "");
            sub.className = soort === "late" ? "is-late" : "";
            setDate("gl-v2-tr-pop-streef", streef);
            setDate("gl-v2-tr-pop-werk", c.getAttribute("data-werk"));
            pop.hidden = false;
            var r = c.getBoundingClientRect(), w = pop.offsetWidth;
            if (window.innerWidth >= 768) {
                pop.style.left = Math.max(12, Math.min(r.left, window.innerWidth - w - 12)) + "px";
                var top = r.bottom + 6;
                pop.style.top = (top + pop.offsetHeight > window.innerHeight - 12 ? Math.max(12, r.top - pop.offsetHeight - 6) : top) + "px";
            } else { pop.style.left = ""; pop.style.top = ""; }
            if (window.GlV2DatePicker) window.GlV2DatePicker.init();
            return;
        }
        if (pop && !pop.hidden && !e.target.closest("#gl-v2-tr-pop") && !e.target.closest(".gl-v2-datepicker-panel")) closePop();
    });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closePop(); });
    function popSave(extra) {
        if (!popCell) return;
        var id = popCell.getAttribute("data-ms-id");
        var fields = {};
        var s = (hiddenOf("gl-v2-tr-pop-streef") || {}).value, w = (hiddenOf("gl-v2-tr-pop-werk") || {}).value;
        if (s && s !== popCell.getAttribute("data-streef")) fields.streefdatum = s;
        if (extra === "today") { fields.bereikt = "true"; }
        else if (w && w !== popCell.getAttribute("data-werk")) { fields.bereikt = "true"; fields.bereiktOp = w; }
        else if (!w && popCell.getAttribute("data-werk")) { fields.bereikt = "false"; }
        if (!Object.keys(fields).length) { closePop(); return; }
        snel(id, fields).then(function () { reloadWith({ hl: id }); })
            .catch(function () { toast("danger", "Niet gelukt", "De mijlpaal kon niet bijgewerkt worden."); });
    }
    var pt = document.getElementById("gl-v2-tr-pop-today"), ps = document.getElementById("gl-v2-tr-pop-save"), po = document.getElementById("gl-v2-tr-pop-open");
    if (pt) pt.addEventListener("click", function () { popSave("today"); });
    if (ps) ps.addEventListener("click", function () { popSave(); });
    if (po) po.addEventListener("click", function () { var id = popCell && popCell.getAttribute("data-ms-id"); closePop(); if (id) openForm({ id: id }); });

    // ── 30d · Kalender ────────────────────────────────────────────────────────────────────────────────
    var cal = cfg.calendar || { mijlpalen: [], fases: [] };
    var DAG = ["MA", "DI", "WO", "DO", "VR", "ZA", "ZO"];
    var MAAND = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var MAAND_KORT = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    function parse(iso) { var p = iso.split("-"); return new Date(+p[0], +p[1] - 1, +p[2]); }
    function iso(d) { return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }
    function nl(d) { return ("0" + d.getDate()).slice(-2) + "/" + ("0" + (d.getMonth() + 1)).slice(-2); }
    function cap(s) { return s.charAt(0).toUpperCase() + s.slice(1); }
    function addMonths(d, n) { return new Date(d.getFullYear(), d.getMonth() + n, 1); }
    var today = cal.vandaag ? parse(cal.vandaag) : new Date();
    var calState = { view: "maand", cursor: new Date(today.getFullYear(), today.getMonth(), 1), filter: "all", sel: iso(today), fasesOpen: true };
    var byDate = {};
    (cal.mijlpalen || []).forEach(function (m) { (byDate[m.datum] = byDate[m.datum] || []).push(m); });
    var order = { late: 0, geblokkeerd: 1, bezig: 2, open: 3, nvt: 4, done: 5 };
    function passes(m) {
        var f = calState.filter;
        if (f === "all") return true;
        if (f === "todo") return m.soort !== "done" && m.soort !== "nvt";
        if (f === "late") return m.soort === "late";
        return m.soort === "done";
    }
    function itemsOn(isoDate) { return (byDate[isoDate] || []).filter(passes).sort(function (a, b) { return (order[a.soort] - order[b.soort]) || a.naam.localeCompare(b.naam); }); }
    function period() {
        var c = calState.cursor, v = calState.view;
        if (v === "kwartaal") { var qs = new Date(c.getFullYear(), Math.floor(c.getMonth() / 3) * 3, 1); return { start: qs, end: new Date(qs.getFullYear(), qs.getMonth() + 3, 0) }; }
        if (v === "jaar") return { start: new Date(c.getFullYear(), 0, 1), end: new Date(c.getFullYear(), 11, 31) };
        return { start: new Date(c.getFullYear(), c.getMonth(), 1), end: new Date(c.getFullYear(), c.getMonth() + 1, 0) };
    }
    function inPeriod(p) {
        var out = [];
        (cal.mijlpalen || []).forEach(function (m) { var d = parse(m.datum); if (d >= p.start && d <= p.end) out.push(m); });
        return out;
    }
    function el(tag, cls, text) { var e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; }
    function dotIcon(soort) { return { done: "ph-check", late: "ph-exclamation-mark", nvt: "ph-minus", bezig: "ph-clock", geblokkeerd: "ph-prohibit" }[soort] || ""; }

    function monthGrid(first, compact) {
        var grid = el("div", "gl-v2-tr-month");
        DAG.forEach(function (d) { grid.appendChild(el("div", "gl-v2-tr-month-dow", d)); });
        var start = new Date(first.getFullYear(), first.getMonth(), 1);
        var offset = (start.getDay() + 6) % 7;
        var cur = new Date(start.getFullYear(), start.getMonth(), 1 - offset);
        var last = new Date(first.getFullYear(), first.getMonth() + 1, 0);
        while (cur <= last || cur.getDay() !== 1) {
            var d = new Date(cur), key = iso(d);
            var btn = el("button", "gl-v2-tr-day");
            btn.type = "button";
            btn.setAttribute("data-day", key);
            if (d.getMonth() !== first.getMonth()) btn.classList.add("is-out");
            if (key === iso(today)) btn.classList.add("is-today");
            if (key === calState.sel) btn.classList.add("is-selected");
            btn.appendChild(el("span", "gl-v2-tr-day-n", String(d.getDate())));
            var its = d.getMonth() === first.getMonth() ? itemsOn(key) : [];
            var max = 3;
            its.slice(0, max).forEach(function (m) { btn.appendChild(el("span", "gl-v2-tr-chipms is-" + (m.soort === "done" ? "done" : m.soort === "late" ? "late" : "open"), m.naam)); });
            if (its.length > max) {
                var rest = its.slice(max), allDone = rest.every(function (m) { return m.soort === "done"; });
                btn.appendChild(el("span", "gl-v2-tr-chipms is-more", "+" + rest.length + (allDone ? " bereikt" : " meer")));
            }
            grid.appendChild(btn);
            cur.setDate(cur.getDate() + 1);
        }
        return grid;
    }

    function renderFases(p) {
        var host = document.getElementById("gl-v2-tr-cal-fases");
        host.innerHTML = "";
        if (calState.view === "agenda" || !(cal.fases || []).length) { host.hidden = true; return; }
        var days = Math.round((p.end - p.start) / 86400000) + 1;
        var rows = [];
        cal.fases.forEach(function (f) {
            if (!f.start || !f.eind) return;
            var s = parse(f.start), e = parse(f.eind);
            if (e < p.start || s > p.end) return;
            rows.push({ f: f, s: s, e: e });
        });
        if (!rows.length) { host.hidden = true; return; }
        host.hidden = false;
        var head = el("div", "gl-v2-tr-cal-fases-head");
        head.appendChild(el("span", null, calState.view === "jaar" ? "FASES DIT JAAR" : calState.view === "kwartaal" ? "FASES DIT KWARTAAL" : "FASES DEZE MAAND"));
        host.appendChild(head);
        rows.forEach(function (r) {
            var row = el("div", "gl-v2-tr-cal-frow");
            row.appendChild(el("span", null, r.f.naam));
            var track = el("div", "gl-v2-tr-cal-track");
            var s = r.s < p.start ? p.start : r.s, e = r.e > p.end ? p.end : r.e;
            var left = (Math.round((s - p.start) / 86400000) / days) * 100;
            var width = ((Math.round((e - s) / 86400000) + 1) / days) * 100;
            var cls = "gl-v2-tr-cal-bar";
            var label;
            if (r.f.teLaat > 0 && r.f.status !== 2) { cls += " is-late"; label = r.f.teLaat + " te laat"; }
            else if (r.f.status === 2) label = "ok";
            else if (r.f.actief) { cls += " is-actief"; label = "actief"; }
            else { cls += " is-gepland"; label = "gepland"; }
            if (r.f.actief && !(r.f.teLaat > 0)) cls += " is-actief";
            if (r.e > p.end) label += " · loopt tot " + MAAND_KORT[r.e.getMonth()] + " ’" + String(r.e.getFullYear()).slice(2);
            var bar = el("div", cls, label);
            bar.style.left = left + "%"; bar.style.width = width + "%";
            track.appendChild(bar); row.appendChild(track); host.appendChild(row);
        });
    }

    function renderBody(p) {
        var body = document.getElementById("gl-v2-tr-cal-body");
        body.innerHTML = "";
        var v = calState.view;
        if (v === "maand") body.appendChild(monthGrid(calState.cursor));
        else if (v === "kwartaal") {
            for (var i = 0; i < 3; i++) {
                var mo = new Date(p.start.getFullYear(), p.start.getMonth() + i, 1);
                var sec = el("div", "gl-v2-tr-ymonth"); sec.style.padding = "12px 12px 4px";
                sec.appendChild(el("h3", null, cap(MAAND[mo.getMonth()]) + " " + mo.getFullYear()));
                sec.appendChild(monthGrid(mo));
                body.appendChild(sec);
            }
        } else if (v === "jaar") {
            var yr = el("div", "gl-v2-tr-year");
            for (var mth = 0; mth < 12; mth++) {
                var first = new Date(p.start.getFullYear(), mth, 1), box = el("div", "gl-v2-tr-ymonth");
                box.appendChild(el("h3", null, MAAND[mth]));
                var g = el("div", "gl-v2-tr-ygrid"), off = (first.getDay() + 6) % 7, dim = new Date(p.start.getFullYear(), mth + 1, 0).getDate();
                for (var k = 0; k < off; k++) g.appendChild(el("span", "gl-v2-tr-yday is-out"));
                for (var dd = 1; dd <= dim; dd++) {
                    var dkey = iso(new Date(p.start.getFullYear(), mth, dd)), its = itemsOn(dkey);
                    var b = el("button", "gl-v2-tr-yday", String(dd)); b.type = "button"; b.setAttribute("data-day", dkey); b.setAttribute("data-jump", "1");
                    if (its.length) b.classList.add(its.some(function (m) { return m.soort === "late"; }) ? "has-late" : its.every(function (m) { return m.soort === "done"; }) ? "has-done" : "has-open");
                    if (dkey === iso(today)) b.classList.add("is-today");
                    if (its.length) b.title = its.length + (its.length === 1 ? " mijlpaal" : " mijlpalen");
                    g.appendChild(b);
                }
                box.appendChild(g); yr.appendChild(box);
            }
            body.appendChild(yr);
        } else {
            var ag = el("div", "gl-v2-tr-agenda"), any = false;
            for (var d = new Date(p.start); d <= p.end; d.setDate(d.getDate() + 1)) {
                var key = iso(d), items = itemsOn(key);
                if (!items.length) continue;
                any = true;
                var day = el("div", "gl-v2-tr-agenda-day" + (key === iso(today) ? " is-today" : ""));
                var dt = el("div", "gl-v2-tr-agenda-date", ("0" + d.getDate()).slice(-2) + " " + MAAND_KORT[d.getMonth()]);
                dt.appendChild(el("small", null, d.toLocaleDateString("nl-BE", { weekday: "long" })));
                day.appendChild(dt);
                var list = el("div");
                items.forEach(function (m) { list.appendChild(dayRow(m)); });
                day.appendChild(list); ag.appendChild(day);
            }
            if (!any) ag.appendChild(el("div", "gl-v2-tr-empty-line", "Geen mijlpalen in deze periode voor dit filter."));
            body.appendChild(ag);
        }
    }
    function dayRow(m) {
        var row = el("div", "gl-v2-tr-cal-dayrow" + (m.soort === "late" ? " is-late" : ""));
        var dot = el("span", "gl-v2-tr-dot is-" + m.soort); var ic = dotIcon(m.soort);
        if (ic) { var i = el("i", "ph " + ic); i.setAttribute("aria-hidden", "true"); dot.appendChild(i); }
        row.appendChild(dot);
        row.appendChild(el("span", null, m.naam));
        if (m.fase) row.appendChild(el("small", null, m.fase));
        return row;
    }

    function renderSide(p, list) {
        var nums = document.getElementById("gl-v2-tr-cal-numbers");
        nums.innerHTML = "";
        var done = 0, late = 0, open = 0, soon = 0, t0 = iso(today), t14 = iso(new Date(today.getFullYear(), today.getMonth(), today.getDate() + 14));
        list.forEach(function (m) {
            if (m.soort === "done") done++; else if (m.soort === "late") late++; else if (m.soort !== "nvt") open++;
        });
        (cal.mijlpalen || []).forEach(function (m) { if (m.soort !== "done" && m.soort !== "nvt" && m.datum >= t0 && m.datum <= t14) soon++; });
        [[done, "bereikt", ""], [late, "achterstallig", late ? "is-late" : ""], [open, "nog te doen", ""], [soon, "binnen 14 dagen", ""]].forEach(function (n) {
            var c = el("div", "gl-v2-tr-cal-num " + n[2]); c.appendChild(el("b", null, String(n[0]))); c.appendChild(el("span", null, n[1])); nums.appendChild(c);
        });
        document.getElementById("gl-v2-tr-cal-period-count").textContent = list.length + (list.length === 1 ? " mijlpaal" : " mijlpalen");
        // gekozen dag
        var sel = parse(calState.sel);
        document.getElementById("gl-v2-tr-cal-day-title").textContent = sel.getDate() + " " + MAAND[sel.getMonth()];
        document.getElementById("gl-v2-tr-cal-day-sub").textContent = sel.toLocaleDateString("nl-BE", { weekday: "long" });
        var host = document.getElementById("gl-v2-tr-cal-day-list");
        host.innerHTML = "";
        var items = itemsOn(calState.sel);
        if (!items.length) host.appendChild(el("div", "gl-v2-tr-empty-line", "Geen mijlpalen op deze dag."));
        items.slice(0, 4).forEach(function (m) { host.appendChild(dayRow(m)); });
        if (items.length > 4) { var more = el("div", "gl-v2-tr-cal-more"); var bt = el("button", "gl-v2-tr-linkbtn", "+ " + (items.length - 4) + " meer"); bt.type = "button"; bt.addEventListener("click", function () { host.innerHTML = ""; items.forEach(function (m) { host.appendChild(dayRow(m)); }); }); more.appendChild(bt); host.appendChild(more); }
    }

    function calRender() {
        var p = period(), v = calState.view, list = inPeriod(p);
        var title = v === "jaar" ? String(p.start.getFullYear()) : v === "kwartaal" ? (Math.floor(p.start.getMonth() / 3) + 1) + "e kwartaal " + p.start.getFullYear() : cap(MAAND[p.start.getMonth()]) + " " + p.start.getFullYear();
        document.getElementById("gl-v2-tr-cal-title").textContent = title;
        document.getElementById("gl-v2-tr-cal-sub").textContent = nl(p.start) + " – " + nl(p.end) + " · " + list.length + (list.length === 1 ? " mijlpaal" : " mijlpalen");
        renderFases(p);
        renderBody(p);
        renderSide(p, list);
    }
    function calStep(dir) {
        var v = calState.view, c = calState.cursor;
        calState.cursor = v === "jaar" ? new Date(c.getFullYear() + dir, 0, 1) : v === "kwartaal" ? addMonths(c, 3 * dir) : addMonths(c, dir);
        calRender();
    }
    var calRoot = document.getElementById("gl-v2-tr-panel-kalender");
    if (calRoot) {
        document.getElementById("gl-v2-tr-cal-prev").addEventListener("click", function () { calStep(-1); });
        document.getElementById("gl-v2-tr-cal-next").addEventListener("click", function () { calStep(1); });
        document.getElementById("gl-v2-tr-cal-today").addEventListener("click", function () { calState.cursor = new Date(today.getFullYear(), today.getMonth(), 1); calState.sel = iso(today); calRender(); });
        $$("#gl-v2-tr-cal-filter button").forEach(function (b) { b.addEventListener("click", function () { calState.filter = b.getAttribute("data-cal-filter"); $$("#gl-v2-tr-cal-filter button").forEach(function (o) { o.classList.toggle("is-active", o === b); }); calRender(); }); });
        $$("#gl-v2-tr-cal-view button").forEach(function (b) { b.addEventListener("click", function () { calState.view = b.getAttribute("data-cal-view"); $$("#gl-v2-tr-cal-view button").forEach(function (o) { o.classList.toggle("is-active", o === b); }); calRender(); }); });
        document.getElementById("gl-v2-tr-cal-body").addEventListener("click", function (e) {
            var d = e.target.closest("[data-day]");
            if (!d) return;
            calState.sel = d.getAttribute("data-day");
            if (d.getAttribute("data-jump")) { var s = parse(calState.sel); calState.view = "maand"; calState.cursor = new Date(s.getFullYear(), s.getMonth(), 1); $$("#gl-v2-tr-cal-view button").forEach(function (o) { o.classList.toggle("is-active", o.getAttribute("data-cal-view") === "maand"); }); }
            calRender();
        });
    }

    // ── Start ─────────────────────────────────────────────────────────────────────────────────────────
    showTab(currentTab, false);
    var hl = root.getAttribute("data-highlight");
    if (hl) {
        var target = document.getElementById("gl-v2-tr-ms-" + hl) || $('.gl-v2-tr-trow[data-ms-id="' + hl + '"]');
        if (target) {
            var sec = target.closest(".gl-v2-tr-fase"); if (sec) toggleFase(sec, true);
            target.classList.add("is-highlight");
            setTimeout(function () { target.scrollIntoView({ block: "center", behavior: "smooth" }); }, 120);
        }
    }
    if (cfg.openNieuweInFase != null && canWrite) openForm({ faseId: cfg.openNieuweInFase || null });
})();
