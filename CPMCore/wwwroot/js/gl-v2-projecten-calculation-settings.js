/* gl-v2 — Projecten/CalculationSettingsV2 (design-handoff 42d, "Budget instellen"). Bedragen invoeren,
   verschil t.o.v. het definitief budget live herrekenen, activiteiten (meerdere tegelijk) toevoegen. */
(function () {
    "use strict";
    var cfg = window.GlnCalcSettings || {};
    var heeftDef = !!cfg.heeftDef;
    var form = document.getElementById("gln-cs-form");
    if (!form) return;
    var fmt = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var fmt0 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 });

    function parse(txt) {
        var s = (txt || "").replace(/[^\d,.\-]/g, "");
        if (s.indexOf(",") >= 0) s = s.replace(/\./g, "").replace(",", ".");
        var v = parseFloat(s);
        return isNaN(v) ? 0 : v;
    }
    function eur0(v) { return "€ " + fmt0.format(v); }

    // ── Meerdere-keuze-dropdown met zoekveld (zelfde markup/gedrag als de Activiteiten-multiselect van
    //    Leveranciers/Klanten; die JS is per pagina gekopieerd, dus ook hier een eigen, compacte kopie) ──
    var backdrop = null;
    function getBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement("div");
        backdrop.className = "gl-v2-select-backdrop";
        document.body.appendChild(backdrop);
        backdrop.addEventListener("click", closeAllPanels);
        return backdrop;
    }
    function closeAllPanels() {
        document.querySelectorAll(".gl-v2-select-panel.is-open").forEach(function (p) { p.classList.remove("is-open"); });
        document.querySelectorAll(".gl-v2-select-trigger.is-open").forEach(function (t) { t.classList.remove("is-open"); });
        if (backdrop) backdrop.classList.remove("is-open");
    }
    function positionPanel(trigger, panel) {
        if (window.innerWidth < 768) { panel.style.top = ""; panel.style.bottom = ""; panel.style.left = ""; panel.style.width = ""; return; }
        var rect = trigger.getBoundingClientRect();
        var width = Math.max(rect.width, 260);
        panel.style.width = width + "px";
        panel.style.left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12)) + "px";
        var spaceBelow = window.innerHeight - rect.bottom - 16;
        var spaceAbove = rect.top - 16;
        if (spaceBelow < 160 && spaceAbove > spaceBelow) {
            panel.style.top = "";
            panel.style.bottom = (window.innerHeight - rect.top + 8) + "px";
            panel.style.maxHeight = Math.max(160, spaceAbove - 8) + "px";
        } else {
            panel.style.bottom = "";
            panel.style.top = (rect.bottom + 8) + "px";
            panel.style.maxHeight = Math.max(160, spaceBelow) + "px";
        }
    }
    function openPanel(trigger, panel) {
        closeAllPanels();
        panel.classList.add("is-open");
        trigger.classList.add("is-open");
        getBackdrop().classList.add("is-open");
        positionPanel(trigger, panel);
    }
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeAllPanels(); });
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        closeAllPanels();
    });
    window.addEventListener("resize", closeAllPanels);

    var root = document.getElementById("gln-cs-add-multiselect");
    var trigger = root.querySelector(".gl-v2-select-trigger");
    var panel = root.querySelector(".gl-v2-select-panel");
    var chipsHost = root.querySelector('[data-role="chips"]');
    var filterInput = root.querySelector('[data-role="filter"]');
    var hiddenHost = root.querySelector('[data-role="hidden-inputs"]');
    var fieldName = root.getAttribute("data-field-name");
    var addBtn = document.getElementById("gln-cs-add-btn");

    function selectedIds() {
        return Array.prototype.map.call(hiddenHost.querySelectorAll('[data-role="hidden-input"]'), function (el) { return el.value; });
    }
    function isSelected(id) { return selectedIds().indexOf(id) !== -1; }
    function options() { return panel.querySelectorAll(".gl-v2-select-option.is-multi"); }

    function renderChips() {
        chipsHost.innerHTML = "";
        options().forEach(function (opt) {
            var id = opt.getAttribute("data-id");
            opt.classList.toggle("is-selected", isSelected(id));
            if (!isSelected(id)) return;
            var chip = document.createElement("span");
            chip.className = "gl-v2-select-chip";
            var label = document.createElement("span");
            label.textContent = opt.getAttribute("data-text");
            chip.appendChild(label);
            var remove = document.createElement("button");
            remove.type = "button";
            remove.className = "gl-v2-select-chip-remove";
            remove.setAttribute("aria-label", "Verwijderen");
            remove.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
            remove.addEventListener("click", function (e) { e.stopPropagation(); toggle(id); });
            chip.appendChild(remove);
            chipsHost.appendChild(chip);
        });
        addBtn.disabled = selectedIds().length === 0;
        filterInput.placeholder = selectedIds().length ? "" : filterInput.getAttribute("data-placeholder");
    }
    filterInput.setAttribute("data-placeholder", filterInput.placeholder);

    function toggle(id) {
        var existing = hiddenHost.querySelector('[data-role="hidden-input"][value="' + id + '"]');
        if (existing) existing.remove();
        else {
            var input = document.createElement("input");
            input.type = "hidden";
            input.value = id;
            input.setAttribute("data-role", "hidden-input");
            hiddenHost.appendChild(input);
        }
        renderChips();
    }

    function filterOptions() {
        var term = filterInput.value.trim().toLowerCase();
        options().forEach(function (opt) {
            opt.hidden = term.length > 0 && (opt.getAttribute("data-text") || "").toLowerCase().indexOf(term) === -1;
        });
        // Groepskop verbergen als al haar opties verborgen zijn.
        panel.querySelectorAll("[data-group-header]").forEach(function (h) {
            var n = h.nextElementSibling, zichtbaar = false;
            while (n && !n.hasAttribute("data-group-header")) { if (!n.hidden) zichtbaar = true; n = n.nextElementSibling; }
            h.hidden = !zichtbaar;
        });
    }

    panel.addEventListener("click", function (e) {
        var opt = e.target.closest(".gl-v2-select-option.is-multi");
        if (opt) toggle(opt.getAttribute("data-id"));
    });
    trigger.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select-chip")) return;
        if (panel.classList.contains("is-open")) closeAllPanels();
        else { openPanel(trigger, panel); filterInput.focus(); }
    });
    filterInput.addEventListener("focus", function () { if (!panel.classList.contains("is-open")) openPanel(trigger, panel); });
    filterInput.addEventListener("input", function () {
        if (!panel.classList.contains("is-open")) openPanel(trigger, panel);
        filterOptions();
    });
    filterInput.addEventListener("click", function (e) { e.stopPropagation(); });

    // ── Rijen ─────────────────────────────────────────────────────────────────────────────────────
    function refresh() {
        var total = 0, totalDef = 0, changed = 0;
        document.querySelectorAll(".gln-cs-group").forEach(function (grp) {
            var sum = 0, n = 0;
            grp.querySelectorAll(".gln-cs-body .gln-cs-row").forEach(function (row) {
                var v = parse(row.querySelector(".gln-cs-amount-input").value);
                row.querySelector(".gln-cs-price-raw").value = v.toFixed(2);
                sum += v; n++;
                var defTxt = row.getAttribute("data-def");
                var def = defTxt === "" ? null : parseFloat(defTxt);
                if (def !== null) totalDef += def;
                if (heeftDef) {
                    var diff = row.querySelector(".gln-cs-diff");
                    var d = def === null ? v : v - def;
                    var rowChanged = def === null || Math.round(d * 100) !== 0;
                    diff.textContent = !rowChanged ? "—" : (d > 0 ? "+ " : "− ") + eur0(Math.abs(d));
                    diff.classList.toggle("is-up", rowChanged && d > 0);
                    diff.classList.toggle("is-down", rowChanged && d < 0);
                    row.classList.toggle("is-changed", rowChanged);
                    if (rowChanged) changed++;
                }
            });
            grp.querySelector(".gln-cs-group-desc").textContent = n === 0 ? "geen activiteiten" : n + (n === 1 ? " activiteit" : " activiteiten") + " · " + eur0(sum);
            grp.querySelector(".gln-cs-empty").hidden = n > 0;
            total += sum;
        });
        document.getElementById("gln-cs-total").textContent = "€ " + fmt.format(total);
        var hint = document.getElementById("gln-cs-total-hint");
        if (hint) {
            var dt = total - totalDef;
            hint.textContent = Math.round(dt * 100) === 0 ? "= definitief" : (dt > 0 ? "+ " : "− ") + eur0(Math.abs(dt)) + " t.o.v. definitief";
        }
        var st = document.getElementById("gln-cs-status");
        if (heeftDef) st.textContent = changed === 0 ? "Geen wijzigingen t.o.v. definitief" : changed + (changed === 1 ? " activiteit wijkt" : " activiteiten wijken") + " af van definitief";
        else st.textContent = "";
    }

    form.addEventListener("input", function (e) { if (e.target.classList.contains("gln-cs-amount-input")) refresh(); });
    form.addEventListener("focusout", function (e) {
        if (e.target.classList.contains("gln-cs-amount-input")) { e.target.value = fmt.format(parse(e.target.value)); refresh(); }
    });
    form.addEventListener("focusin", function (e) { if (e.target.classList.contains("gln-cs-amount-input")) e.target.select(); });

    function addRow(opt) {
        var grp = document.querySelector('.gln-cs-group[data-group-id="' + opt.getAttribute("data-group") + '"]');
        if (!grp) return null;
        var row = document.createElement("div");
        row.className = "gln-cs-row" + (heeftDef ? " has-def" : "") + " is-new";
        row.setAttribute("data-activity-id", opt.getAttribute("data-id"));
        row.setAttribute("data-def", "");
        var name = document.createElement("span"); name.className = "gln-cs-name"; name.textContent = opt.getAttribute("data-text");
        var cell = document.createElement("span"); cell.className = "is-num";
        cell.innerHTML = '<input type="hidden" data-f="ActivityId"><input type="hidden" data-f="GroupId"><input type="hidden" data-f="ManueelId" value="0"><input type="hidden" data-f="Price" class="gln-cs-price-raw" value="0.00">'
            + '<span class="gl-v2-field-box gln-cs-amount"><input type="text" class="gl-v2-field-input gln-cs-amount-input" inputmode="decimal" value="0,00"><span class="gl-v2-field-suffix">€</span></span>';
        cell.querySelector('[data-f="ActivityId"]').value = opt.getAttribute("data-id");
        cell.querySelector('[data-f="GroupId"]').value = opt.getAttribute("data-group");
        cell.querySelector(".gln-cs-amount-input").setAttribute("aria-label", "Budget " + opt.getAttribute("data-text"));
        row.appendChild(name); row.appendChild(cell);
        if (heeftDef) {
            var def = document.createElement("span"); def.className = "is-num gln-cs-def"; def.textContent = "—";
            var diff = document.createElement("span"); diff.className = "is-num gln-cs-diff"; diff.textContent = "—";
            row.appendChild(def); row.appendChild(diff);
        }
        grp.querySelector(".gln-cs-body").insertBefore(row, grp.querySelector(".gln-cs-empty"));
        return row;
    }

    // "Toevoegen": álle gekozen activiteiten in één keer, elk in zijn eigen categorie.
    addBtn.addEventListener("click", function () {
        var ids = selectedIds(), eerste = null;
        ids.forEach(function (id) {
            var opt = panel.querySelector('.gl-v2-select-option.is-multi[data-id="' + id + '"]');
            if (!opt) return;
            var row = addRow(opt);
            if (row && !eerste) eerste = row;
            opt.remove(); // staat nu in het budget, dus niet meer kiesbaar
        });
        hiddenHost.innerHTML = "";
        filterInput.value = "";
        filterOptions();
        renderChips();
        closeAllPanels();
        refresh();
        if (eerste) eerste.querySelector(".gln-cs-amount-input").focus();
    });

    // Bij het versturen alle rijen doorlopend hernummeren (rows[0..n]) zodat ook nieuwe rijen mee binden.
    form.addEventListener("submit", function () {
        var idx = 0;
        document.querySelectorAll(".gln-cs-body .gln-cs-row").forEach(function (row) {
            ["ActivityId", "GroupId", "ManueelId", "Price"].forEach(function (f) {
                var el = row.querySelector('[data-f="' + f + '"]') || row.querySelector('[name$=".' + f + '"]');
                if (el) el.name = "rows[" + idx + "]." + f;
            });
            idx++;
        });
    });

    renderChips();
    refresh();
})();
