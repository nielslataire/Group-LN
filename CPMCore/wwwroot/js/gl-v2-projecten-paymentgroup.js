// gl-v2 — Betalingsgroep bewerken / Nieuwe betalingsgroep (design-handoff punt 26a-d).
// Twee onderdelen in één bestand, omdat beide pagina's (PaymentStagesV2 en de bewerkpagina) de 26a-modal
// nodig hebben:
//   1. de "Nieuwe betalingsgroep"-modal (26a): AJAX-inhoud, zoeken, voorbeeld, naam/btw voorinvullen;
//   2. de bewerkpagina (26b/26c): schijven slepen/dupliceren/verwijderen/toevoegen/overnemen, eenheden-
//      chips, totaalbolletje, opslaan geblokkeerd tot het totaal 100 % is, niet-opgeslagen-wijzigingen.
// De DOM-volgorde van de rijen IS de schijfvolgorde; de server wijst er Id-slots aan toe.
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-pg-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function bsModal(el) { return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }

    // nl-BE: "5,5" of "5.5"; leeg/ongeldig → NaN. Afgerond op 2 decimalen zoals de server.
    function parsePct(v) {
        var s = String(v == null ? "" : v).replace(/[%\s ]/g, "").replace(",", ".");
        if (s === "") return NaN;
        var n = parseFloat(s);
        return isNaN(n) ? NaN : Math.round(n * 100) / 100;
    }
    function fmtPct(n) { return (Math.round(n * 100) / 100).toFixed(2).replace(".", ","); }
    function fmtShort(n) { return String(Math.round(n * 100) / 100).replace(".", ","); }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    // 1. 26a — Nieuwe betalingsgroep (modal)
    // ══════════════════════════════════════════════════════════════════════════════════════════
    var npgModalEl = document.getElementById("gl-v2-npg-modal");
    var npgBody = document.getElementById("gl-v2-npg-body");
    var npgConfirm = document.getElementById("gl-v2-npg-confirm");
    var npgSub = document.getElementById("gl-v2-npg-header-sub");

    function openNewGroupModal(preselectGroupId) {
        if (!npgModalEl || !npgBody) return;
        npgBody.innerHTML = '<div class="gl-v2-npg-loading">Bezig met laden…</div>';
        if (npgConfirm) npgConfirm.disabled = true;
        if (npgSub) npgSub.textContent = "";
        var m = bsModal(npgModalEl);
        if (m) m.show();
        var url = cfg.newGroupModalUrl + "?projectId=" + encodeURIComponent(cfg.projectId) +
            (preselectGroupId ? "&sourceGroupId=" + encodeURIComponent(preselectGroupId) : "");
        fetch(url, { credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(r.status); return r.text(); })
            .then(function (html) {
                npgBody.innerHTML = html;
                if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(npgBody);
                wireNewGroupForm();
            })
            .catch(function () {
                npgBody.innerHTML = '<div class="gl-v2-npg-loading">De groepen konden niet geladen worden. Sluit dit venster en probeer opnieuw.</div>';
            });
    }

    // Zoeken op groep, project of gemeente (data-search) met sectielabels die verdwijnen zonder resultaat —
    // gedeeld door de 26a-modal en "Schijven overnemen".
    function wireSourceSearch(search, scope, emptyMsg) {
        if (!search) return;
        search.addEventListener("input", function () {
            var q = search.value.trim().toLowerCase();
            var items = $$("[data-npg-item]", scope), visible = 0;
            items.forEach(function (i) {
                var show = q === "" || (i.getAttribute("data-search") || "").indexOf(q) !== -1;
                i.hidden = !show;
                if (show) visible++;
            });
            $$("[data-npg-label]", scope).forEach(function (lab) {
                var n = lab.nextElementSibling, any = false;
                while (n && !n.hasAttribute("data-npg-label") && !n.classList.contains("gl-v2-npg-list-empty")) { if (n.hasAttribute("data-npg-item") && !n.hidden) any = true; n = n.nextElementSibling; }
                lab.hidden = !any;
            });
            if (emptyMsg) emptyMsg.hidden = visible !== 0;
        });
    }

    function wireNewGroupForm() {
        var form = document.getElementById("gl-v2-npg-form");
        if (!form) return;
        if (npgSub) npgSub.textContent = form.getAttribute("data-project-name") || "";

        var modeRadios = $$('input[name="npgMode"]', form);
        var copyPanel = $("#gl-v2-npg-copy", form);
        var sourceInput = $("#gl-v2-npg-source", form);
        var nameInput = $("#gl-v2-npg-name", form);
        var vatInput = $("#gl-v2-npg-vat", form);
        var vatHelp = $("#gl-v2-npg-vat-help", form);
        var search = $("#gl-v2-npg-search", form);
        var items = $$("[data-npg-item]", form);
        var emptyMsg = $("#gl-v2-npg-list-empty", form);
        var previewEmpty = $("#gl-v2-npg-preview-empty", form);
        var previewBox = $("#gl-v2-npg-preview-box", form);
        var previewTitle = $("#gl-v2-npg-preview-title", form);
        var previewTotal = $("#gl-v2-npg-preview-total", form);
        var previewRows = $("#gl-v2-npg-preview-rows", form);
        var nameTouched = false;
        var defaultVatId = vatInput ? vatInput.value : "";
        var defaultVatHelp = vatHelp ? vatHelp.textContent : "";

        function mode() { var r = modeRadios.filter(function (x) { return x.checked; })[0]; return r ? r.value : "empty"; }
        function selectedItem() { return items.filter(function (i) { return $("input", i).checked; })[0] || null; }

        function setVat(id, fromGroup) {
            if (!vatInput) return;
            var panel = $(".gl-v2-select-panel", form);
            var opt = id ? $('.gl-v2-select-option[data-value="' + id + '"]', panel) : null;
            if (opt) { opt.click(); if (vatHelp) vatHelp.textContent = fromGroup ? "overgenomen uit de gekozen groep" : defaultVatHelp; }
            else if (vatHelp) vatHelp.textContent = fromGroup ? "btw-type van de gekozen groep bestaat niet voor dit project — standaard behouden" : defaultVatHelp;
        }

        function refresh() {
            var copy = mode() === "copy";
            if (copyPanel) copyPanel.hidden = !copy;
            if (sourceInput) sourceInput.disabled = !copy;
            var sel = copy ? selectedItem() : null;
            if (sourceInput) sourceInput.value = sel ? sel.getAttribute("data-group-id") : "";
            var ok = (nameInput.value.trim() !== "") && (!copy || !!sel);
            if (npgConfirm) npgConfirm.disabled = !ok;
        }

        function showPreview(item) {
            if (!item) { previewBox.hidden = true; previewEmpty.hidden = false; return; }
            var stages = [];
            try { stages = JSON.parse(item.getAttribute("data-stages") || "[]"); } catch (e) { }
            var total = 0;
            previewRows.innerHTML = "";
            stages.forEach(function (s, i) {
                total += s.p;
                var li = document.createElement("li");
                var n = document.createElement("span"); n.className = "gl-v2-npg-preview-num"; n.textContent = String(i + 1);
                var t = document.createElement("span"); t.className = "gl-v2-npg-preview-name"; t.textContent = s.n;
                var p = document.createElement("span"); p.className = "gl-v2-npg-preview-pct"; p.textContent = fmtShort(s.p) + " %";
                li.appendChild(n); li.appendChild(t); li.appendChild(p);
                previewRows.appendChild(li);
            });
            previewTitle.textContent = "Voorbeeld · " + stages.length + (stages.length === 1 ? " schijf" : " schijven");
            previewTotal.textContent = fmtShort(total) + " %";
            previewEmpty.hidden = true;
            previewBox.hidden = false;
        }

        function onPick() {
            var item = selectedItem();
            showPreview(item);
            if (item) {
                if (!nameTouched || nameInput.value.trim() === "") { nameInput.value = item.getAttribute("data-name") + " (kopie)"; nameTouched = false; }
                setVat(item.getAttribute("data-vat-type-id"), true);
            }
            refresh();
        }

        modeRadios.forEach(function (r) {
            r.addEventListener("change", function () {
                if (mode() === "empty") { setVat(defaultVatId, false); showPreview(null); }
                else onPick();
                refresh();
            });
        });
        items.forEach(function (i) { $("input", i).addEventListener("change", onPick); });
        nameInput.addEventListener("input", function () { nameTouched = true; refresh(); });

        wireSourceSearch(search, form, emptyMsg);

        // "Kopiëren en bewerken" is een GET: loze keuzevelden niet mee in de querystring.
        form.addEventListener("submit", function (e) {
            if (npgConfirm && npgConfirm.disabled) { e.preventDefault(); return; }
            $$('input[name="npgMode"], input[name="npgSource"], input[type="search"]', form).forEach(function (x) { x.disabled = true; });
        });

        // Voorselectie ("Dupliceer groep"): de gekozen groep staat al aangevinkt en in beeld.
        var pre = form.getAttribute("data-preselect");
        if (pre) {
            var match = items.filter(function (i) { return i.getAttribute("data-group-id") === pre; })[0];
            if (match) { $("input", match).checked = true; match.scrollIntoView({ block: "nearest" }); onPick(); return; }
        }
        refresh();
    }

    $$(".js-npg-open").forEach(function (b) {
        b.addEventListener("click", function (e) { e.preventDefault(); openNewGroupModal(0); });
    });
    var dupGroupBtn = document.getElementById("gl-v2-pg-duplicate-group");
    if (dupGroupBtn) {
        dupGroupBtn.addEventListener("click", function () { openNewGroupModal(dupGroupBtn.getAttribute("data-group-id")); });
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    // 2. 26b/26c — bewerkpagina
    // ══════════════════════════════════════════════════════════════════════════════════════════
    var form = document.getElementById("gl-v2-pg-form");
    if (!form) return;

    var rowsEl = document.getElementById("gl-v2-pg-rows");
    var rowTpl = document.getElementById("gl-v2-pg-row-template");
    var chipTpl = document.getElementById("gl-v2-pg-chip-template");
    var nameInput = document.getElementById("gl-v2-pg-name");
    var vatInput = document.getElementById("gl-v2-pg-vat");
    var saveBtn = document.getElementById("gl-v2-pg-save");
    var totalPill = document.getElementById("gl-v2-pg-total");
    var totalValue = document.getElementById("gl-v2-pg-total-value");
    var statusEl = document.getElementById("gl-v2-pg-status");
    var statusText = document.getElementById("gl-v2-pg-status-text");
    var statusIcon = statusEl ? statusEl.querySelector("i") : null;
    var selBar = document.getElementById("gl-v2-pg-selbar");
    var selCount = document.getElementById("gl-v2-pg-selbar-count");
    var dirtyBadge = document.getElementById("gl-v2-pg-dirty-badge");
    var dirty = false;
    var submitting = false;

    function rows() { return $$("#gl-v2-pg-rows > li"); }
    function isLocked(r) { return r.getAttribute("data-locked") === "1"; }
    function pctInput(r) { return $(".gl-v2-pg-pct-input", r); }
    function nameOf(r) { return $(".gl-v2-pg-name-input", r); }

    function markDirty() {
        dirty = true;
        if (dirtyBadge) dirtyBadge.hidden = false;
    }

    function renumber() {
        var list = rows();
        list.forEach(function (r, i) {
            var n = $(".gl-v2-pg-num", r); if (n) n.textContent = String(i + 1);
            var b = $(".gl-v2-pg-badge-final", r); if (b) b.hidden = i !== list.length - 1;
        });
    }

    function total() {
        return rows().reduce(function (acc, r) { var v = parsePct(pctInput(r).value); return acc + (isNaN(v) ? 0 : v); }, 0);
    }

    // Opslaan kan pas als alles klopt; de statusregel zegt wat er nog ontbreekt (26d: bolletje rood bij
    // te veel/te weinig, groen bij 100 %).
    function refreshState() {
        var t = Math.round(total() * 100) / 100;
        var diff = Math.round((t - 100) * 100) / 100;
        var ok = Math.abs(diff) < 0.005;
        totalValue.textContent = fmtShort(t);
        totalPill.setAttribute("data-state", ok ? "ok" : "bad");

        var reason = "";
        var hasVatOptions = !!$(".gl-v2-pg-field-vat .gl-v2-select-option", form);
        var list = rows();
        if (nameInput.value.trim() === "") reason = "Vul de naam van de groep in";
        else if (hasVatOptions && !vatInput.value) reason = "Kies een btw-type";
        else if (list.length === 0) reason = "Voeg minstens één schijf toe";
        else if (list.some(function (r) { return nameOf(r).value.trim() === ""; })) reason = "Elke schijf heeft een omschrijving nodig";
        else if (list.some(function (r) { var v = parsePct(pctInput(r).value); return isNaN(v) || v <= 0; })) reason = "Elke schijf heeft een percentage boven 0 nodig";
        else if (!ok) reason = "Opslaan kan pas als het totaal 100 % is — nog " + fmtShort(Math.abs(diff)) + " % te " + (diff > 0 ? "veel" : "weinig");

        saveBtn.disabled = reason !== "";
        if (statusEl) statusEl.setAttribute("data-state", reason === "" ? "ok" : "bad");
        if (statusText) statusText.textContent = reason === "" ? "Totaal is 100 %" : reason;
        if (statusIcon) statusIcon.className = "ph " + (reason === "" ? "ph-check-circle" : "ph-warning-circle");

        var label = totalPill.querySelector(".gl-v2-pg-total-label");
        if (label) label.setAttribute("title", ok ? "Het totaal klopt" : "Nog " + fmtShort(Math.abs(diff)) + " % te " + (diff > 0 ? "veel" : "weinig"));
    }

    function refreshSelection() {
        var sel = rows().filter(function (r) { return $(".gl-v2-pg-sel", r).checked; });
        rows().forEach(function (r) { r.classList.toggle("is-selected", $(".gl-v2-pg-sel", r).checked); });
        selBar.hidden = sel.length === 0;
        selCount.textContent = sel.length + (sel.length === 1 ? " schijf geselecteerd" : " schijven geselecteerd");
    }

    function changed() { renumber(); refreshState(); refreshSelection(); markDirty(); }

    function newRow(name, pct, isCopy) {
        var frag = rowTpl.content.cloneNode(true);
        var li = frag.firstElementChild;
        nameOf(li).value = name || "";
        pctInput(li).value = pct == null || isNaN(pct) ? "" : fmtPct(pct);
        if (isCopy) { li.classList.add("is-copy"); var note = $(".is-copy-note", li); if (note) note.hidden = false; }
        return li;
    }

    // Nieuwe/overgenomen schijven komen vóór de eindafrekening, tenzij die vastligt (dan achteraan).
    function insertBeforeFinal(li) {
        var list = rows();
        var last = list[list.length - 1];
        if (last && !isLocked(last)) rowsEl.insertBefore(li, last); else rowsEl.appendChild(li);
    }

    // ── rij-acties ────────────────────────────────────────────────────────────────────────────
    function duplicateRows(list) {
        if (!list.length) return;
        var anchor = list[list.length - 1];
        var copies = list.map(function (r) {
            var p = parsePct(pctInput(r).value);
            return newRow(nameOf(r).value, p, true);
        });
        var ref = anchor.nextElementSibling;
        copies.forEach(function (c) { rowsEl.insertBefore(c, ref); });
        changed();
    }

    function deleteRows(list) {
        list.filter(function (r) { return !isLocked(r); }).forEach(function (r) { r.remove(); });
        changed();
    }

    rowsEl.addEventListener("click", function (e) {
        var row = e.target.closest("li.gl-v2-pg-row");
        if (!row) return;
        if (e.target.closest(".js-pg-dup")) { duplicateRows([row]); var c = row.nextElementSibling; if (c) { var inp = nameOf(c); if (inp) inp.focus(); } }
        else if (e.target.closest(".js-pg-del")) deleteRows([row]);
    });
    rowsEl.addEventListener("change", function (e) {
        if (e.target.classList.contains("gl-v2-pg-sel")) refreshSelection();
    });
    rowsEl.addEventListener("input", function (e) {
        var row = e.target.closest("li.gl-v2-pg-row");
        if (!row) return;
        if (row.classList.contains("is-copy")) {
            row.classList.remove("is-copy");
            var note = $(".is-copy-note", row); if (note) note.hidden = true;
        }
        markDirty();
        refreshState();
    });
    rowsEl.addEventListener("focusout", function (e) {
        var inp = e.target;
        if (!inp.classList || !inp.classList.contains("gl-v2-pg-pct-input")) return;
        var row = inp.closest("li.gl-v2-pg-row");
        var v = parsePct(inp.value);
        if (isNaN(v)) return;
        // Een vaste (gefactureerde) schijf mag niet lager dan het gefactureerde percentage.
        var min = parseFloat(row.getAttribute("data-min-pct") || "0");
        if (isLocked(row) && v < min) v = min;
        inp.value = fmtPct(v);
        refreshState();
    });

    $("#gl-v2-pg-sel-duplicate").addEventListener("click", function () {
        var sel = rows().filter(function (r) { return $(".gl-v2-pg-sel", r).checked; });
        rows().forEach(function (r) { $(".gl-v2-pg-sel", r).checked = false; });
        duplicateRows(sel);
    });
    $("#gl-v2-pg-sel-delete").addEventListener("click", function () {
        var sel = rows().filter(function (r) { return $(".gl-v2-pg-sel", r).checked; });
        deleteRows(sel);
    });
    $("#gl-v2-pg-sel-clear").addEventListener("click", function () {
        rows().forEach(function (r) { $(".gl-v2-pg-sel", r).checked = false; });
        refreshSelection();
    });

    $("#gl-v2-pg-add-stage").addEventListener("click", function () {
        var li = newRow("", NaN, false);
        insertBeforeFinal(li);
        changed();
        nameOf(li).focus();
        li.scrollIntoView({ block: "nearest" });
    });

    // ── slepen (desktop) + toetsenbord (pijltjes op het handvat) ─────────────────────────────
    var dragging = null;
    rowsEl.addEventListener("dragstart", function (e) {
        var handle = e.target.closest ? e.target.closest(".gl-v2-pg-handle") : null;
        if (!handle) { e.preventDefault(); return; }
        dragging = handle.closest("li.gl-v2-pg-row");
        if (!dragging || isLocked(dragging)) { e.preventDefault(); dragging = null; return; }
        dragging.classList.add("is-dragging");
        e.dataTransfer.effectAllowed = "move";
        try { e.dataTransfer.setData("text/plain", "stage"); } catch (err) { }
        try { e.dataTransfer.setDragImage(dragging, 20, 20); } catch (err) { }
    });
    rowsEl.addEventListener("dragover", function (e) {
        if (!dragging) return;
        e.preventDefault();
        var others = rows().filter(function (r) { return r !== dragging; });
        var target = null;
        for (var i = 0; i < others.length; i++) {
            var box = others[i].getBoundingClientRect();
            if (e.clientY < box.top + box.height / 2) { target = others[i]; break; }
        }
        if (target) { if (dragging.nextElementSibling !== target) rowsEl.insertBefore(dragging, target); }
        else if (rowsEl.lastElementChild !== dragging) rowsEl.appendChild(dragging);
    });
    function endDrag() {
        if (!dragging) return;
        dragging.classList.remove("is-dragging");
        dragging = null;
        changed();
    }
    rowsEl.addEventListener("drop", function (e) { e.preventDefault(); endDrag(); });
    rowsEl.addEventListener("dragend", endDrag);
    rowsEl.addEventListener("keydown", function (e) {
        var handle = e.target.closest ? e.target.closest(".gl-v2-pg-handle") : null;
        if (!handle || (e.key !== "ArrowUp" && e.key !== "ArrowDown")) return;
        var row = handle.closest("li.gl-v2-pg-row");
        e.preventDefault();
        if (e.key === "ArrowUp" && row.previousElementSibling) rowsEl.insertBefore(row, row.previousElementSibling);
        else if (e.key === "ArrowDown" && row.nextElementSibling) rowsEl.insertBefore(row.nextElementSibling, row);
        handle.focus();
        changed();
    });

    // ── schijven overnemen uit andere groep ──────────────────────────────────────────────────
    var importBtn = document.getElementById("gl-v2-pg-import");
    var importModal = document.getElementById("gl-v2-pg-import-modal");
    var importConfirm = document.getElementById("gl-v2-pg-import-confirm");
    if (importBtn && importModal) {
        importBtn.addEventListener("click", function () {
            $$('input[name="gl-v2-pg-import-source"]', importModal).forEach(function (r) { r.checked = false; });
            var impSearch = document.getElementById("gl-v2-pg-import-search");
            if (impSearch) { impSearch.value = ""; impSearch.dispatchEvent(new Event("input")); }
            importConfirm.disabled = true;
            bsModal(importModal).show();
        });
        wireSourceSearch(document.getElementById("gl-v2-pg-import-search"), importModal, document.getElementById("gl-v2-pg-import-empty"));
        importModal.addEventListener("change", function (e) {
            if (e.target.name === "gl-v2-pg-import-source") importConfirm.disabled = false;
        });
        importConfirm.addEventListener("click", function () {
            var picked = $$('input[name="gl-v2-pg-import-source"]', importModal).filter(function (r) { return r.checked; })[0];
            if (!picked) return;
            var stages = [];
            try { stages = JSON.parse(picked.closest("label").getAttribute("data-stages") || "[]"); } catch (err) { }
            // Allemaal vóór dezelfde eindafrekening → blijven in de brongroep-volgorde staan.
            stages.forEach(function (s) { insertBeforeFinal(newRow(s.n, s.p, true)); });
            bsModal(importModal).hide();
            changed();
        });
    }

    // ── eenheden (chips + "+ eenheid") ───────────────────────────────────────────────────────
    var chips = document.getElementById("gl-v2-pg-chips");
    var addUnitBtn = document.getElementById("gl-v2-pg-add-unit");
    var unitModal = document.getElementById("gl-v2-pg-unit-modal");
    var unitList = document.getElementById("gl-v2-pg-unit-list");
    var unitConfirm = document.getElementById("gl-v2-pg-unit-confirm");

    function syncAddUnit() { if (addUnitBtn) addUnitBtn.hidden = !$("label.gl-v2-pg-pick-item", unitList); }

    chips.addEventListener("click", function (e) {
        var rm = e.target.closest(".gl-v2-pg-chip-remove");
        if (!rm) return;
        var chip = rm.closest(".gl-v2-pg-chip");
        var id = chip.getAttribute("data-unit-id");
        var name = $(".gl-v2-pg-chip-name", chip).textContent;
        chip.remove();
        // Weer kiesbaar achter "+ eenheid".
        var label = document.createElement("label");
        label.className = "gl-v2-pg-pick-item";
        label.setAttribute("data-unit-id", id);
        label.setAttribute("data-unit-name", name);
        label.innerHTML = '<input type="checkbox" value=""><span class="gl-v2-pg-pick-text"><span class="gl-v2-pg-pick-name"></span></span>';
        $("input", label).value = id;
        $(".gl-v2-pg-pick-name", label).textContent = name;
        unitList.appendChild(label);
        syncAddUnit();
        markDirty();
    });
    if (addUnitBtn && unitModal) {
        addUnitBtn.addEventListener("click", function () {
            $$("input", unitList).forEach(function (i) { i.checked = false; });
            bsModal(unitModal).show();
        });
        unitConfirm.addEventListener("click", function () {
            $$("label.gl-v2-pg-pick-item", unitList).forEach(function (l) {
                if (!$("input", l).checked) return;
                var chip = chipTpl.content.cloneNode(true).firstElementChild;
                chip.setAttribute("data-unit-id", l.getAttribute("data-unit-id"));
                $("input", chip).value = l.getAttribute("data-unit-id");
                $(".gl-v2-pg-chip-name", chip).textContent = l.getAttribute("data-unit-name");
                $(".gl-v2-pg-chip-remove", chip).setAttribute("aria-label", "Eenheid " + l.getAttribute("data-unit-name") + " ontkoppelen");
                chips.insertBefore(chip, addUnitBtn);
                l.remove();
                markDirty();
            });
            syncAddUnit();
            bsModal(unitModal).hide();
        });
    }

    // ── velden, opslaan en niet-opgeslagen wijzigingen ───────────────────────────────────────
    nameInput.addEventListener("input", function () { markDirty(); refreshState(); });
    vatInput.addEventListener("change", function () { markDirty(); refreshState(); });

    form.addEventListener("submit", function (e) {
        refreshState();
        if (saveBtn.disabled) { e.preventDefault(); return; }
        submitting = true;
        $$(".gl-v2-pg-pct-input", form).forEach(function (i) { var v = parsePct(i.value); if (!isNaN(v)) i.value = fmtPct(v); });
    });
    // Enter in een tekstveld mag niet per ongeluk opslaan terwijl de rijen nog niet kloppen.
    form.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && e.target.tagName === "INPUT" && e.target.type === "text") { e.preventDefault(); }
    });

    var discardModal = document.getElementById("gl-v2-pg-discard-modal");
    var discardConfirm = document.getElementById("gl-v2-pg-discard-confirm");
    var cancelLink = document.getElementById("gl-v2-pg-cancel-link");
    var backLink = document.getElementById("gl-v2-topbar-back-link");
    if (discardModal && discardConfirm && window.bootstrap) {
        [cancelLink, backLink].filter(Boolean).forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                if (!dirty) return;
                e.preventDefault();
                discardConfirm.href = trigger.href;
                bsModal(discardModal).show();
            });
        });
        discardConfirm.addEventListener("click", function () { dirty = false; });
    }
    window.addEventListener("beforeunload", function (e) {
        if (dirty && !submitting) { e.preventDefault(); e.returnValue = ""; }
    });

    // Een kopie die uit de 26a-modal komt (GET met bron) is nog niet bewaard: meteen als gewijzigd tonen,
    // anders gaat "Annuleren" zonder vraag weg terwijl er wel een heel voorbereid dossier staat.
    if (form.getAttribute("data-group-id") === "0" && document.querySelector(".gl-v2-notice-expanded")) markDirty();

    renumber();
    refreshState();
    refreshSelection();
    syncAddUnit();
})();
