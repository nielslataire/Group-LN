// gl-v2 — Terugkerende facturen in Nieuwe factuur (Invoices/CreateV2, design-handoff punt 49a en 49c). Dit bestand hangt zich aan gl-v2-invoices-create.js:
//  - stap 2: Soort (eenmalig of terugkerend), het schema (herhaal, factuurdag, eerste factuur, periode, stopt) en "Telkens als het zover is" (concept klaar, weekend, indexeren);
//  - stap 3: variabelen als {maand} en {jaar} in de omschrijving, per lijn "elke factuur of enkel de eerste" en "geïndexeerd";
//  - rechts: de planning (stap 2) en "zo komt het op de facturen" (stap 3), berekend door de server (InvoicesController.RecurringPlanV2) met dezelfde regels als bij het echt klaarzetten;
//  - stap 4: Controleren en Schema activeren (of Opslaan als draft = concept). Bewaren gaat naar InvoicesController.SaveRecurringV2.
// De server beslist over alles wat telt; de getallen hier zijn enkel voor het voorbeeld.
(function () {
    "use strict";
    var cfg = window.GlV2InvoiceCreate, api = window.GlV2InvoiceCreateApi, hooks = window.GlV2InvoiceCreateHooks;
    var rc = cfg && cfg.recurring;
    if (!cfg || !api || !hooks || !rc || !document.getElementById("ri-schema")) return;

    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function fmt(n) { return Number(n || 0).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function euro(n) { return "€ " + fmt(n); }
    function debounce(fn, ms) { var t; return function () { var a = arguments, c = this; clearTimeout(t); t = setTimeout(function () { fn.apply(c, a); }, ms); }; }
    // NL/BE: punt = duizendtal, komma = decimaal. Een lijnprijs kan als tekst ("700,00") of als getal (AutoNumeric) in de staat staan.
    function parseNum(v) {
        if (v == null) return 0;
        if (typeof v === "number") return isFinite(v) ? v : 0;
        var t = String(v).replace(/[\s  ]/g, "").replace(/−/g, "-");
        if (!t) return 0;
        var hasC = t.indexOf(",") >= 0, hasD = t.indexOf(".") >= 0;
        if (hasC && hasD) { var i = Math.max(t.lastIndexOf(","), t.lastIndexOf(".")); t = t.slice(0, i).replace(/[.,]/g, "") + "." + t.slice(i + 1).replace(/[.,]/g, ""); }
        else if (hasC) t = t.replace(/\./g, "").replace(",", ".");
        else if ((t.match(/\./g) || []).length > 1) t = t.replace(/\./g, "");
        t = t.replace(/[^\d.\-]/g, "");
        var n = parseFloat(t); return isNaN(n) ? 0 : n;
    }
    function isoToDate(iso) { var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso || ""); return m ? new Date(+m[1], +m[2] - 1, +m[3]) : null; }
    function dateToIso(d) { return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }
    function dmy(d) { return ("0" + d.getDate()).slice(-2) + "/" + ("0" + (d.getMonth() + 1)).slice(-2) + "/" + d.getFullYear(); }
    function daysIn(y, m) { return new Date(y, m + 1, 0).getDate(); }
    var MONTHS = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var s0 = rc.schedule || {};
    var today = isoToDate(rc.today) || new Date();

    var R = {
        on: false, freq: s0.frequency || 2, dayMode: s0.dayMode || 1, day: s0.dayOfMonth || 1, weekday: Math.min(5, s0.dayOfMonth || 1),
        first: s0.firstDate || "", period: s0.periodMode || 1, end: s0.endMode || 0, endCount: s0.endCount || 12, endDate: s0.endDate || "",
        lead: s0.leadDays == null ? 3 : s0.leadDays, weekend: s0.weekendRule || 1, resume: s0.resumeMode || 1, warn: s0.warnUnpaid !== false,
        idxOn: !!s0.idxEnabled, idxWhen: s0.idxWhen || 1, idxDate: s0.idxDate || "", idxBaseMode: s0.idxBase ? "custom" : "first", idxBase: s0.idxBase || "",
        idxType: s0.idxType || 1, idxPct: s0.idxPercent || "", idxBaseIndex: s0.idxBaseIndex || "", idxConfirm: s0.idxConfirm !== false,
        status: 0, nextStatus: 0, plan: null, selLine: 0
    };
    var isExisting = rc.scheduleId > 0, existingStatus = rc.status || 0;
    var form = document.getElementById("ic-form");

    // ───────────────────────── hulpjes voor de gedeelde componenten ─────────────────────────
    function setSeg(id, attr, value) {
        $$("#" + id + " button").forEach(function (b) { var on = String(b.getAttribute("data-" + attr)) === String(value); b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); });
    }
    function onSeg(id, attr, fn) {
        $$("#" + id + " button").forEach(function (b) { b.addEventListener("click", function () { var v = b.getAttribute("data-" + attr); setSeg(id, attr, v); fn(v); }); });
    }
    function setDate(id, iso) {
        var hid = document.getElementById(id), txt = document.getElementById(id + "_text"), d = isoToDate(iso);
        if (hid) hid.value = iso || "";
        if (txt) txt.value = d ? dmy(d) : "";
        if (window.GlV2DatePicker) window.GlV2DatePicker.setValue(id, iso || "");
    }
    function setSelect(id, value) {
        var wrap = document.getElementById(id + "_select"), hid = document.getElementById(id);
        if (!wrap || !hid) return;
        var opt = wrap.querySelector('.gl-v2-select-option[data-value="' + String(value) + '"]');
        if (opt) opt.click(); else hid.value = String(value);
    }
    function radio(name, value) { var r = $('input[name="' + name + '"][value="' + value + '"]'); if (r) r.checked = true; }
    function show(el, on) { if (el) el.hidden = !on; }

    // ───────────────────────── schema → scherm ─────────────────────────
    function dayHelp() {
        if (R.freq === 1) return "Elke week op " + ["maandag", "dinsdag", "woensdag", "donderdag", "vrijdag"][R.weekday - 1] + ".";
        if (R.freq === 4) { var d = isoToDate(R.first); return d ? "Elk jaar op " + d.getDate() + " " + MONTHS[d.getMonth()] + "." : ""; }
        var every = R.freq === 3 ? "Elk kwartaal" : "Elke maand";
        if (R.dayMode === 2) return every + " op de laatste dag.";
        var n = R.day, ord = (n === 1 || n === 8 || n >= 20) ? n + "ste" : n + "de";
        return n > 28 ? "In maanden met minder dan " + n + " dagen wordt dit de laatste dag van de maand." : every + " op de " + ord + ".";
    }
    function drawSchedule() {
        setSeg("ri-freq", "freq", R.freq);
        var week = R.freq === 1, year = R.freq === 4;
        show($("#ri-day-month"), !week && !year); show($("#ri-day-week"), week); show($("#ri-day-field"), !year);
        setSeg("ri-daymode", "daymode", R.dayMode);
        $$("#ri-daygrid button").forEach(function (b) { b.classList.toggle("is-active", parseInt(b.getAttribute("data-day"), 10) === R.day); });
        show($("#ri-daygrid"), R.dayMode === 1);
        setSeg("ri-weekday", "wd", R.weekday);
        $("#ri-day-help").textContent = dayHelp();
        setSeg("ri-period", "period", R.period);
        setSeg("ri-end", "end", R.end);
        show($("#ri-end-count"), R.end === 1); show($("#ri-end-date"), R.end === 2);
        $("#ri-endcount").value = R.endCount;
        $("#ri-lead-text").textContent = R.lead + (R.lead === 1 ? " dag" : " dagen");
        $("#ri-idx-on").checked = R.idxOn; show($("#ri-idx"), R.idxOn);
        radio("ri-idxwhen", R.idxWhen); show($("#ri-idxdate-field"), R.idxWhen === 3);
        setSeg("ri-idxbasemode", "basemode", R.idxBaseMode); show($("#ri-idxbase-field"), R.idxBaseMode === "custom");
        setSeg("ri-idxtype", "idxtype", R.idxType);
        show($("#ri-idxpct-field"), R.idxType !== 1); show($("#ri-idxbaseindex-field"), R.idxType === 1); show($("#ri-idxrule"), R.idxType === 1);
        $("#ri-idxconfirm").checked = R.idxConfirm; $("#ri-warn").checked = R.warn;
    }

    // De eerste factuur volgt het gekozen ritme zolang je die datum niet zelf aanpast.
    var settingFirst = false;
    function suggestFirst() {
        var d = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1);
        if (R.freq === 1) { while (d.getDay() !== R.weekday % 7) d.setDate(d.getDate() + 1); }
        else if (R.freq === 4) { var cur = isoToDate(R.first); if (cur && cur >= d) return; }
        else {
            for (var i = 0; i < 400; i++) {
                var dim = daysIn(d.getFullYear(), d.getMonth()), want = R.dayMode === 2 ? dim : Math.min(R.day, dim);
                if (d.getDate() === want) break;
                d.setDate(d.getDate() + 1);
            }
        }
        R.first = dateToIso(d); settingFirst = true; setDate("ri-first", R.first); settingFirst = false;
    }
    function afterScheduleChange(resetFirst) {
        if (resetFirst) suggestFirst();
        drawSchedule(); queuePlan(); api.draw();
    }

    onSeg("ri-freq", "freq", function (v) {
        R.freq = parseInt(v, 10);
        if (R.freq === 1) R.weekday = Math.min(5, Math.max(1, R.weekday));
        afterScheduleChange(true);
    });
    onSeg("ri-daymode", "daymode", function (v) { R.dayMode = parseInt(v, 10); afterScheduleChange(true); });
    onSeg("ri-weekday", "wd", function (v) { R.weekday = parseInt(v, 10); afterScheduleChange(true); });
    $$("#ri-daygrid button").forEach(function (b) { b.addEventListener("click", function () { R.day = parseInt(b.getAttribute("data-day"), 10); afterScheduleChange(true); }); });
    onSeg("ri-period", "period", function (v) { R.period = parseInt(v, 10); queuePlan(); });
    onSeg("ri-end", "end", function (v) { R.end = parseInt(v, 10); drawSchedule(); queuePlan(); });
    $("#ri-endcount").addEventListener("input", function () { R.endCount = parseInt(this.value.replace(/\D/g, ""), 10) || 0; queuePlan(); });
    $("#ri-first").addEventListener("change", function () {
        R.first = this.value;
        if (settingFirst) return;
        var d = isoToDate(R.first);
        if (d) {
            if (R.freq === 1) { if (d.getDay() >= 1 && d.getDay() <= 5) R.weekday = d.getDay(); }
            else if (R.dayMode === 1) R.day = d.getDate();
        }
        drawSchedule(); queuePlan();
    });
    $("#ri-enddate").addEventListener("change", function () { R.endDate = this.value; queuePlan(); });
    $("#ri-lead").addEventListener("change", function () { R.lead = parseInt(this.value, 10) || 0; drawSchedule(); queuePlan(); });
    $("#ri-weekend").addEventListener("change", function () { R.weekend = parseInt(this.value, 10) || 1; queuePlan(); });
    $("#ri-idx-on").addEventListener("change", function () { R.idxOn = this.checked; drawSchedule(); api.renderLines(); queuePlan(); });
    $$('input[name="ri-idxwhen"]').forEach(function (r) { r.addEventListener("change", function () { R.idxWhen = parseInt(r.value, 10); drawSchedule(); queuePlan(); }); });
    $("#ri-idxdate").addEventListener("input", function () { R.idxDate = this.value; queuePlan(); });
    onSeg("ri-idxbasemode", "basemode", function (v) { R.idxBaseMode = v; drawSchedule(); queuePlan(); });
    $("#ri-idxbase").addEventListener("change", function () { R.idxBase = this.value; queuePlan(); });
    onSeg("ri-idxtype", "idxtype", function (v) { R.idxType = parseInt(v, 10); drawSchedule(); queuePlan(); });
    $("#ri-idxpct").addEventListener("input", function () { R.idxPct = this.value; queuePlan(); });
    $("#ri-idxbaseindex").addEventListener("input", function () { R.idxBaseIndex = this.value; queuePlan(); });
    $("#ri-idxconfirm").addEventListener("change", function () { R.idxConfirm = this.checked; });
    $("#ri-warn").addEventListener("change", function () { R.warn = this.checked; });

    // ───────────────────────── eenmalig of terugkerend ─────────────────────────
    function setRecurring(on) {
        R.on = on;
        radio("ri-kind", on ? "rec" : "once");
        show($("#ri-schema"), on); show($("#ri-when"), on); show($("#ri-summary"), on); show($("#ri-vars"), on);
        show($("#ic-date-wrap"), !on); show($("#ic-block-modes"), !on); show($("#ic-kind-field"), !on); show($("#ic-appendix-field"), !on);
        $("#ic-data-sub").textContent = on ? "betaaltermijn geldt voor elke factuur" : "vervaldatum wordt berekend";
        var title = $("#ic-card-title"), desc = $("#ic-card-desc");
        if (title) title.textContent = on ? (isExisting ? "Terugkerende factuur bewerken" : "Nieuwe terugkerende factuur") : "Nieuwe factuur";
        if (desc) desc.textContent = on ? "schema · het CRM zet elke periode een concept klaar" : "concept · je nummert het daarna in de lijst met facturen";
        if (on) {
            // creditnota's en opbouw uit posten of offerte bestaan niet bij een schema
            var inv = $('#ic-kind button[data-kind="invoice"]'); if (inv && api.state.credit) inv.click();
            if (api.state.mode !== 1) api.setMode(1);
            if (!R.first) { suggestFirst(); }
            drawSchedule(); queuePlan();
        }
        relabelSteps(); api.renderLines(); api.show(api.state.step); api.draw();
    }
    function relabelSteps() {
        $$("[data-ic-steps]").forEach(function (bar) {
            var labels = $$(".gl-v2-steps-label", bar), subs = $$(".gl-v2-steps-sub", bar);
            if (labels[1] && subs[1]) subs[1].textContent = R.on ? "schema, opbouw" : "data, opbouw, verzenden";
            if (labels[2] && subs[2]) subs[2].textContent = R.on ? "wat factureer je" : "omschrijving en bedragen";
            if (labels[3]) labels[3].textContent = R.on ? "Controleren" : "Tekst";
            if (subs[3]) subs[3].textContent = R.on ? "schema activeren" : "teksten en bijlage";
        });
    }
    $$('input[name="ri-kind"]').forEach(function (r) { r.addEventListener("change", function () { if (r.checked) setRecurring(r.value === "rec"); }); });

    // ───────────────────────── stap 3: variabelen en opties per lijn ─────────────────────────
    function lineExtras(l, i) {
        var out = '<div class="gl-v2-ri-lineopts" data-i="' + i + '">' +
            '<button type="button" class="gl-v2-ri-opt' + (l.first ? "" : " is-on") + '" data-ri-opt="all">Elke factuur</button>' +
            '<button type="button" class="gl-v2-ri-opt' + (l.first ? " is-on" : "") + '" data-ri-opt="first">Enkel de eerste</button>';
        if (R.idxOn) out += '<button type="button" class="gl-v2-ri-opt is-ix' + (l.ix ? " is-on" : "") + '" data-ri-opt="ix">' + (l.ix ? "✓ Geïndexeerd" : "Niet indexeren") + '</button>';
        return out + '</div>';
    }
    var linesBody = $("#ic-lines-body");
    linesBody.addEventListener("click", function (e) {
        var b = e.target.closest("[data-ri-opt]"); if (!b || !R.on) return;
        var i = parseInt(b.parentNode.getAttribute("data-i"), 10), l = api.state.lines[i]; if (!l) return;
        var k = b.getAttribute("data-ri-opt");
        if (k === "all") l.first = false; else if (k === "first") l.first = true; else l.ix = !l.ix;
        api.renderLines(); markSelected(); queuePlan(); api.draw();
    });
    linesBody.addEventListener("focusin", function (e) {
        var row = e.target.closest(".gl-v2-ic-line"); if (!row) return;
        R.selLine = parseInt(row.getAttribute("data-i"), 10); markSelected();
    });
    linesBody.addEventListener("input", function (e) { if (R.on && e.target.getAttribute("data-f")) queuePlan(); });
    linesBody.addEventListener("change", function (e) { if (R.on) queuePlan(); });
    linesBody.addEventListener("autoNumeric:rawValueModified", function () { if (R.on) queuePlan(); });
    function markSelected() {
        $$(".gl-v2-ic-line", linesBody).forEach(function (row) { row.classList.toggle("is-sel", R.on && parseInt(row.getAttribute("data-i"), 10) === R.selLine); });
        var t = $("#ri-vars-target"); if (t) t.textContent = "lijn " + (R.selLine + 1);
    }
    // de variabele komt op de plaats van de cursor in de gekozen omschrijving
    $$("#ri-vars-row .gl-v2-ri-varbtn").forEach(function (b) {
        b.addEventListener("mousedown", function (e) { e.preventDefault(); });
        b.addEventListener("click", function () {
            var l = api.state.lines[R.selLine] || api.state.lines[0]; if (!l) return;
            var idx = api.state.lines.indexOf(l), row = $('.gl-v2-ic-line[data-i="' + idx + '"] input[data-f="text"]', linesBody);
            var v = b.getAttribute("data-var"), text = String(l.text || ""), pos = row && typeof row.selectionStart === "number" ? row.selectionStart : text.length;
            var before = text.slice(0, pos), after = text.slice(pos);
            l.text = before + (before && !/\s$/.test(before) ? " " : "") + v + (after && !/^\s/.test(after) ? " " : "") + after;
            if (row) { row.value = l.text; row.focus(); var np = (before + (before && !/\s$/.test(before) ? " " : "") + v).length; try { row.setSelectionRange(np, np); } catch (x) { } }
            api.draw(); queuePlan();
        });
    });

    // ───────────────────────── planning en voorbeeld (server) ─────────────────────────
    function csrf() { var t = form.querySelector('input[name="__RequestVerificationToken"]'); return t ? t.value : ""; }
    function appendLines(fd) {
        var i = 0;
        api.state.lines.forEach(function (l) {
            var text = String(l.text || "").trim(); if (!text) return;
            var vatSel = null;
            fd.append("Lines[" + i + "].Text", text);
            fd.append("Lines[" + i + "].Quantity", String(parseNum(l.qty) || 1).replace(".", ","));
            fd.append("Lines[" + i + "].UnitPrice", String(parseNum(l.price)).replace(".", ","));
            var vat = (cfg.vatTypes || []).filter(function (v) { return String(v.id) === String(l.vat != null ? l.vat : cfg.vatTypeId); })[0] || (cfg.vatTypes || [])[0] || { id: "", pct: 0 };
            fd.append("Lines[" + i + "].VatPercentage", String(vat.pct).replace(".", ","));
            if (vat.id !== "" && vat.id != null) fd.append("Lines[" + i + "].VatTypeId", vat.id);
            fd.append("Lines[" + i + "].FirstOnly", l.first ? "true" : "false");
            fd.append("Lines[" + i + "].Indexable", l.ix && R.idxOn ? "true" : "false");
            i++;
        });
    }
    function scheduleFields(fd, status) {
        fd.set("ScheduleId", rc.scheduleId || 0); fd.set("Status", status || 0);
        fd.set("Frequency", R.freq); fd.set("DayMode", R.dayMode);
        fd.set("DayOfMonth", R.freq === 1 ? R.weekday : R.freq === 4 ? ((isoToDate(R.first) || today).getDate()) : R.day);
        fd.set("FirstDate", R.first || ""); fd.set("PeriodMode", R.period);
        fd.set("EndMode", R.end); fd.set("EndCount", R.end === 1 ? R.endCount : ""); fd.set("EndDate", R.end === 2 ? R.endDate : "");
        fd.set("DraftLeadDays", R.lead); fd.set("WeekendRule", R.weekend); fd.set("ResumeMode", R.resume); fd.set("WarnUnpaid", R.warn ? "true" : "false");
        fd.set("IndexationEnabled", R.idxOn ? "true" : "false"); fd.set("IndexationType", R.idxType); fd.set("IndexationWhen", R.idxWhen);
        fd.set("IndexationBaseDate", R.idxBaseMode === "custom" ? R.idxBase : ""); fd.set("IndexationDate", R.idxDate); fd.set("IndexationPercent", R.idxType === 1 ? "" : R.idxPct); fd.set("IndexationBaseIndex", R.idxType === 1 ? R.idxBaseIndex : "");
        fd.set("IndexationConfirm", R.idxConfirm ? "true" : "false");
    }
    var planSeq = 0;
    function plan() {
        if (!R.on || !R.first) return;
        var fd = new FormData();
        fd.append("__RequestVerificationToken", csrf());
        fd.set("IssuerCompanyId", cfg.issuerId);
        fd.set("PartyType", $("#ic-party-type").value || "ClientAccount"); fd.set("PartyId", $("#ic-party-id").value || "0");
        scheduleFields(fd, 0); appendLines(fd);
        fd.append("partyName", api.partyName());
        var seq = ++planSeq;
        fetch(rc.planUrl, { method: "POST", body: fd, credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.json(); })
            .then(function (p) { if (seq !== planSeq || !p || !p.ok) return; R.plan = p; drawPlan(); drawSummary(); })
            .catch(function () { });
    }
    var queuePlan = debounce(plan, 250);
    function drawPlan() {
        var p = R.plan; if (!p) return;
        $("#ri-plan-sum").textContent = p.summary; $("#ri-plan-sum2").textContent = p.summary2;
        $("#ri-plan-list").innerHTML = (p.plan || []).map(function (x) {
            return '<li><i class="gl-v2-ri-dot' + (x.tag === "eerste" ? " is-first" : "") + '" aria-hidden="true"></i><div><b>' + esc(x.date) + '</b><small>' + esc(x.period) + '</small></div>' +
                (x.tag ? '<em class="gl-v2-ri-tag2' + (x.tag === "eerste" ? "" : " is-gold") + '">' + esc(x.tag) + '</em>' : "") + '<span>' + euro(x.amount) + '</span></li>';
        }).join("");
        $("#ri-plan-note").textContent = p.note || "";
        var sum = $("#ri-idxsum"); if (sum) sum.textContent = R.idxOn && p.indexation ? p.indexation : "";
        $("#ri-preview-list").innerHTML = (p.previews || []).map(function (c) {
            return '<article class="gl-v2-ri-prevcard"><header><b>' + esc(c.title) + '</b><time>' + esc(c.date) + '</time></header>' +
                (c.lines || []).map(function (l) { return '<div class="gl-v2-ri-prevline"><span>' + esc(l.text) + '</span><span>' + euro(l.amount) + '</span></div>'; }).join("") +
                '<footer><span>excl. ' + euro(c.excl) + '</span><span>btw ' + euro(c.vat) + '</span><b>' + euro(c.total) + '</b></footer></article>';
        }).join("");
    }

    // ───────────────────────── stappen ─────────────────────────
    function onStep(step) {
        var rec = R.on, side = $("#ri-side");
        show(side, rec && step >= 2);
        show($("#ic-sheet-head"), !(rec && step >= 2)); show($("#ic-sheet"), !(rec && step >= 2)); show($("#ic-sheet-note"), !(rec && step >= 2));
        show($("#ri-plan"), rec && step !== 3); show($("#ri-plan-info"), rec && step === 2); show($("#ri-preview"), rec && (step === 3 || step === 4));
        var activate = $("#ic-activate"); if (activate) show(activate, rec && step === 4);
        var draft = $("#ic-save-draft");
        if (draft) { draft.hidden = rec && isExisting && existingStatus !== 0; draft.textContent = rec && isExisting ? "Opslaan als concept" : "Opslaan als draft"; }
        if (activate) activate.textContent = rec && isExisting && existingStatus > 0 ? "Wijzigingen opslaan" : "Schema activeren";
        if (rec && step === 4) drawSummary();
        if (rec && step >= 2) queuePlan();
        markSelected();
    }
    function drawSummary() {
        var box = $("#ri-summary-facts"); if (!box || !R.on) return;
        var p = R.plan || {}, party = api.partyName() || "—";
        var pr = $("#ic-project-id_input"), project = pr && pr.value ? pr.value : "—";
        var rows = [
            ["KLANT", party], ["PROJECT", project],
            ["RITME", ["", "Wekelijks", "Maandelijks", "Per kwartaal", "Jaarlijks"][R.freq] + " · " + dayHelp().replace(/\.$/, "")],
            ["EERSTE FACTUUR", R.first && isoToDate(R.first) ? dmy(isoToDate(R.first)) : "—"],
            ["BEDRAG", p.summary || "—"],
            ["BIJ AANMAAK", "Ter controle · concept " + R.lead + (R.lead === 1 ? " dag" : " dagen") + " vooraf"],
            ["STOPT", R.end === 1 ? "Na " + R.endCount + " facturen" : R.end === 2 && R.endDate && isoToDate(R.endDate) ? "Op " + dmy(isoToDate(R.endDate)) : "Nooit"],
            ["INDEXERING", R.idxOn ? ["", "Gezondheidsindex (Statbel)", "Consumptieprijzen", "Vast percentage"][R.idxType] + (R.idxType !== 1 && R.idxPct ? " · " + R.idxPct + " %" : "") : "Geen"]
        ];
        box.innerHTML = rows.map(function (r) { return '<div><dt>' + r[0] + '</dt><dd><b>' + esc(r[1]) + '</b></dd></div>'; }).join("");
    }

    // ───────────────────────── controle en bewaren ─────────────────────────
    function validate() {
        var errs = [];
        if (!R.first || !isoToDate(R.first)) errs.push({ field: "Eerste factuur", message: "Vul de datum van de eerste factuur in.", location: "Instellingen", target: $("#ri-first_picker") });
        else if (!isExisting && isoToDate(R.first) < new Date(today.getFullYear(), today.getMonth(), today.getDate())) errs.push({ field: "Eerste factuur", message: "De eerste factuur moet vandaag of later zijn.", location: "Instellingen", target: $("#ri-first_picker") });
        if (R.end === 1 && !(R.endCount >= 1)) errs.push({ field: "Stopt", message: "Vul in na hoeveel facturen het schema stopt.", location: "Instellingen", target: $("#ri-endcount") });
        if (R.end === 2 && !isoToDate(R.endDate)) errs.push({ field: "Stopt", message: "Vul de einddatum van het schema in.", location: "Instellingen", target: $("#ri-enddate_picker") });
        if (R.idxOn && R.idxWhen === 3 && !/^\d{1,2}\/\d{1,2}$/.test(R.idxDate.trim())) errs.push({ field: "Indexering", message: "Vul de datum van de indexering in als dd/mm.", location: "Instellingen", target: $("#ri-idxdate") });
        if (R.idxOn && R.idxBaseMode === "custom" && !isoToDate(R.idxBase)) errs.push({ field: "Startdatum voor indexering", message: "Vul de startdatum voor de indexering in, of kies Eerste factuur.", location: "Instellingen", target: $("#ri-idxbase_picker") });
        var lines = api.state.lines.filter(function (l) { return String(l.text || "").trim(); });
        if (lines.length && lines.every(function (l) { return l.first; })) errs.push({ field: "Lijnen", message: "Minstens één lijn moet op elke factuur komen.", location: "Lijnen", target: $("#ic-lines-free") });
        return errs;
    }
    function extend(fd) {
        scheduleFields(fd, R.status);
        fd.set("IssuerCompanyId", cfg.issuerId);
    }

    hooks.rec = {
        active: function () { return R.on; },
        onStep: onStep, lineExtras: lineExtras, validate: validate, extend: extend
    };

    // Opslaan als draft = concept (Status 0); Schema activeren = Status 1. De capture-luisteraar loopt vóór die van gl-v2-invoices-create.js.
    var saveDraft = $("#ic-save-draft"), activate = $("#ic-activate");
    saveDraft.addEventListener("click", function () { R.status = R.nextStatus; R.nextStatus = 0; }, true);
    if (activate) activate.addEventListener("click", function () { R.nextStatus = 1; saveDraft.click(); });

    // ───────────────────────── start ─────────────────────────
    // bestaande schema's: de vlaggen per lijn (enkel de eerste, geïndexeerd) horen bij de lijnen die create.js al inlas
    (rc.lineFlags || []).forEach(function (f, i) { var l = api.state.lines[i]; if (l) { l.first = !!f.firstOnly; l.ix = !!f.indexable; } });
    setDate("ri-first", R.first); setDate("ri-enddate", R.endDate); setDate("ri-idxbase", R.idxBase);
    $("#ri-idxdate").value = R.idxDate; $("#ri-idxpct").value = R.idxPct; $("#ri-idxbaseindex").value = R.idxBaseIndex;
    setSelect("ri-lead", R.lead); setSelect("ri-weekend", R.weekend);
    show($("#ri-kind-block"), !isExisting);
    drawSchedule();
    if (cfg.recurringStart || rc.active) setRecurring(true); else relabelSteps();
    if (rc.locked) { $$("#ic-form input, #ic-form button").forEach(function (el) { if (el.id !== "ic-cancel") el.disabled = true; }); }
})();
