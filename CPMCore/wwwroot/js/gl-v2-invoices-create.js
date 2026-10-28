// gl-v2 — Nieuwe factuur (Invoices/CreateV2, design-handoff punt 47a–d): vier stappen met een live A4-voorbeeld. Alles is gewone JS zonder
// DataTables of select2. Het formulier post via fetch naar InvoicesController.SaveDraftV2 met dezelfde veldnamen als het oude scherm
// (InvoiceComposeVM), daarna eventueel naar het voortgangsscherm van punt 48 (gl-v2-invoices-flow.js). De server beslist over alles wat telt:
// de bedragen die hier getoond worden zijn enkel voor het voorbeeld.
(function () {
    "use strict";
    var cfg = window.GlV2InvoiceCreate, form = document.getElementById("ic-form");
    if (!cfg || !form) return;

    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function round2(n) { return Math.round((n + Number.EPSILON) * 100) / 100; }
    function fmt(n) { return Number(n || 0).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function euro(n) { return "€ " + fmt(n); }
    function debounce(fn, ms) { var t; return function () { var a = arguments, c = this; clearTimeout(t); t = setTimeout(function () { fn.apply(c, a); }, ms); }; }
    // NL/BE: punt = duizendtal, komma = decimaal (zoals InvoicesUtil.parseLocaleNumber van het oude scherm)
    function parseNum(v) {
        if (v == null) return 0;
        var s = String(v).replace(/[\s   ]/g, "").replace(/−/g, "-");
        if (!s) return 0;
        var hasC = s.indexOf(",") >= 0, hasD = s.indexOf(".") >= 0;
        if (hasC && hasD) { var lc = s.lastIndexOf(","), ld = s.lastIndexOf("."), i = Math.max(lc, ld); s = s.slice(0, i).replace(/[.,]/g, "") + "." + s.slice(i + 1).replace(/[.,]/g, ""); }
        else if (hasC) s = s.replace(/\./g, "").replace(",", ".");
        else if ((s.match(/\./g) || []).length > 1) s = s.replace(/\./g, "");
        s = s.replace(/[^\d.\-]/g, "");
        var n = parseFloat(s); return isNaN(n) ? 0 : n;
    }
    function postNum(n, d) { return Number(n).toFixed(d == null ? 2 : d).replace(".", ","); }
    function getJson(url, params) {
        var q = Object.keys(params || {}).filter(function (k) { return params[k] != null && params[k] !== ""; }).map(function (k) { return encodeURIComponent(k) + "=" + encodeURIComponent(params[k]); }).join("&");
        return fetch(url + (q ? (url.indexOf("?") >= 0 ? "&" : "?") + q : ""), { credentials: "same-origin", headers: { Accept: "application/json" } }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function isoToDate(iso) { var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso || ""); return m ? new Date(+m[1], +m[2] - 1, +m[3]) : null; }
    function dmy(d) { return ("0" + d.getDate()).slice(-2) + "/" + ("0" + (d.getMonth() + 1)).slice(-2) + "/" + d.getFullYear(); }
    var DAYS = ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];

    var PARTY_TYPE = { ca: "ClientAccount", cc: "ClientContact", su: "Supplier" };
    var edit = cfg.edit || null, locked = !!(edit && edit.locked);
    // Nutsaansluitingen (opbouw 4) bestaat niet in de lijst met opbouwen: zo'n factuur wordt als vrije lijnen overgenomen.
    var startMode = cfg.mode === 4 || !cfg.mode ? 1 : cfg.mode;
    // Lijnen van een kopie, creditnota of concept die aan een schijf of wijzigingsopdracht hangen
    var carriedStages = startMode === 2 ? (cfg.lines || []).filter(function (l) { return l.paymentStageId; }).map(function (l) { return { stageId: l.paymentStageId, unitId: l.unitId, text: l.text, unitName: l.unitName, groupName: l.groupName, amount: Number(l.price), vat: Number(l.vatPct), carried: true }; }) : [];
    var carriedOrders = startMode === 3 ? (cfg.lines || []).filter(function (l) { return l.changeOrderDetailId; }) : [];
    var state = {
        step: 1, credit: !!cfg.isCreditNote, mode: startMode, party: null, detail: null, delivery: null, project: null, contract: null,
        lines: [], stages: null, orders: null, selStages: {}, selOrders: {}, orderPct: {}, issuer: null, file: null, busy: false
    };
    var CP = { ClientAccount: "ca", ClientContact: "cc", Supplier: "su" };

    // ───────────────────────── stappen ─────────────────────────
    var TOTAL = 4, prev = $("#ic-prev"), next = $("#ic-next"), saveDraft = $("#ic-save-draft"), now = $("#ic-step-now");
    function show(n) {
        state.step = Math.max(1, Math.min(TOTAL, n));
        $$("[data-ic-panel]").forEach(function (p) { p.hidden = parseInt(p.getAttribute("data-ic-panel"), 10) !== state.step; });
        $$("[data-ic-steps]").forEach(function (p) { p.hidden = parseInt(p.getAttribute("data-ic-steps"), 10) !== state.step; });
        now.textContent = state.step;
        prev.disabled = state.step === 1;
        next.hidden = state.step === TOTAL;
        // op de laatste stap is bewaren de hoofdactie; nummeren gebeurt nooit hier maar in de lijst met facturen
        saveDraft.classList.toggle("gl-v2-btn-primary", state.step === TOTAL);
        saveDraft.classList.toggle("gl-v2-btn-secondary", state.step !== TOTAL);
        var body = $(".gl-v2-ic-body"); if (body && body.scrollIntoView) body.scrollIntoView({ block: "nearest" });
    }
    prev.addEventListener("click", function () { show(state.step - 1); });
    next.addEventListener("click", function () { show(state.step + 1); });
    document.addEventListener("gl-v2-steps:step", function (e) { var d = e.detail || {}; if (typeof d.index === "number") show(d.index + 1); });
    form.addEventListener("keydown", function (e) { if (e.key === "Enter" && e.target.tagName === "INPUT" && e.target.type !== "file") e.preventDefault(); });

    // ───────────────────────── soort (factuur / creditnota) ─────────────────────────
    $$("#ic-kind button").forEach(function (b) {
        b.addEventListener("click", function () {
            state.credit = b.getAttribute("data-kind") === "credit";
            $("#ic-credit").value = state.credit ? "true" : "false";
            $$("#ic-kind button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
            draw();
        });
    });
    $("#IsPrepaid").addEventListener("change", draw);

    // ───────────────────────── facturatiebedrijf en rekening ─────────────────────────
    var issuerInput = $("#IssuerCompanyId");
    if (issuerInput) issuerInput.addEventListener("change", function () {
        var id = issuerInput.value;
        if (id && String(id) !== String(cfg.issuerId)) window.location.assign(cfg.urls.create.replace(/issuerId=\d+/, "issuerId=" + encodeURIComponent(id)));
    });
    var bankInput = $("#IssuerBankAccountId");
    if (bankInput) bankInput.addEventListener("change", function () { setTimeout(draw, 0); });
    function bankIban() {
        var t = $("#IssuerBankAccountId_trigger"); var txt = t ? t.textContent.trim() : "";
        var m = /\(([A-Z]{2}\d{2}[A-Z0-9 ]+)\)\s*$/.exec(txt); if (m) return m[1];
        var m2 = /([A-Z]{2}\d{2}[A-Z0-9 ]{10,})/.exec(txt); return m2 ? m2[1] : "";
    }
    function ibanGroups(s) { var c = String(s || "").replace(/\s+/g, "").toUpperCase(); return c.replace(/(.{4})/g, "$1 ").trim(); }

    // ───────────────────────── keuzelijsten met zoeken (klant, project, contract) ─────────────────────────
    // Het projectbrede combo-component (GlV2/_GlV2Combo, punt 34): het paneel is position:fixed boven alles en wordt dus nooit afgesneden.
    // De combo's bedraden zichzelf (gl-v2-combo.js); hier luisteren we enkel naar hun events.
    function comboRoot(inner) { var el = document.getElementById(inner); return el ? el.closest("[data-gl-v2-combo]") : null; }
    var partyCombo = comboRoot("ic-party-sel"), projectCombo = comboRoot("ic-project-id"), contractCombo = comboRoot("ic-contract-id");
    function comboSet(root, id, text) { if (root && window.GlV2Combo) window.GlV2Combo.setValue(root, id == null ? "" : String(id), text || ""); }
    var syncing = false;
    document.addEventListener("gl-v2:combo-select", function (e) {
        var d = e.detail || {}, r = e.target;
        if (syncing) return;
        if (r === partyCombo) setParty({ id: d.id, text: d.text });
        else if (r === projectCombo) setProject({ id: d.id, name: d.text }, true);
        else if (r === contractCombo) { state.contract = { id: d.id, name: d.text }; }
    });
    document.addEventListener("gl-v2:combo-new", function (e) { if (e.target === partyCombo) openNewParty((e.detail && e.detail.term) || ""); });
    // wissen met het kruisje: het combo zet de verborgen id leeg
    $("#ic-project-id").addEventListener("change", function () { if (!syncing && !this.value && state.project) setProject(null, true); });
    $("#ic-contract-id").addEventListener("change", function () { if (!syncing && !this.value) state.contract = null; });
    // — nieuwe klant (47e–g): modal met de bestaande QuickCreate-actie —
    function openNewParty(term) {
        if (!window.GlV2NewParty) { window.open(cfg.urls.newCustomer, "_blank"); return; }
        var tok = form.querySelector('input[name="__RequestVerificationToken"]');
        window.GlV2NewParty.open({
            issuerId: cfg.issuerId, issuerName: cfg.issuerName, term: term || "", token: tok ? tok.value : "",
            urls: { quickCreate: cfg.urls.quickCreate, validateVat: cfg.urls.validateVat, findPostal: cfg.urls.findPostal, postcodes: cfg.urls.postcodes, countries: cfg.urls.countries, peppolCheck: cfg.urls.peppolCheck },
            onCreated: function (p) { setParty({ id: p.id, text: p.text }); },
            onSixPercent: function () {
                // particulier met woning ouder dan 10 jaar: 6 % als standaard btw-tarief van deze factuur
                var v = cfg.vatTypes.filter(function (x) { return Number(x.pct) === 6; })[0], wrap = $("#VatTypeId_select"), hid = $("#VatTypeId");
                if (!v || !wrap || !hid || !window.GlV2Select) return;
                window.GlV2Select.setItems(wrap, cfg.vatTypes.map(function (x) { return { value: String(x.id), text: x.code + " · " + x.description + " (" + String(x.pct).replace(".", ",") + " %)" }; }), String(v.id));
                hid.value = String(v.id); hid.dispatchEvent(new Event("change", { bubbles: true }));
            }
        });
    }

    function partyKind() { return state.party ? state.party.id.split(":")[0] : ""; }
    function partyNum() { return state.party ? parseInt(state.party.id.split(":")[1], 10) : 0; }
    function isClient() { var k = partyKind(); return k === "ca" || k === "cc"; }

    function initials(name) { var w = String(name || "").trim().split(/\s+/).filter(Boolean); return ((w[0] || "?")[0] + (w.length > 1 ? w[w.length - 1][0] : "")).toUpperCase(); }
    function setParty(p) {
        if (state.party && state.party.id !== p.id) { carriedStages.length = 0; carriedOrders.length = 0; }
        state.party = p; state.detail = null; state.delivery = null; state.stages = null; state.orders = null; state.selStages = {}; state.selOrders = {};
        syncing = true; state.project = null; comboSet(projectCombo, "", ""); state.contract = null; comboSet(contractCombo, "", ""); syncing = false;
        var kind = partyKind();
        $("#ic-party-id").value = partyNum(); $("#ic-party-type").value = PARTY_TYPE[kind] || "";
        $("#ic-party-pick").hidden = true; $("#ic-recent").hidden = true;
        syncing = true; comboSet(partyCombo, "", ""); syncing = false;
        $("#ic-project-field").hidden = !isClient(); $("#ic-contract-field").hidden = kind !== "su";
        renderPartyCard();
        // Openstaande posten en offertes bestaan enkel voor klanten
        $$("#ic-modes button").forEach(function (b) { b.disabled = b.getAttribute("data-mode") !== "1" && !isClient(); });
        if (!isClient() && state.mode !== 1) setMode(1);
        $("#ic-mode-help").textContent = isClient() ? "Kies hoe de lijnen gevuld worden." : "Openstaande posten en offertes zijn er enkel voor klanten. Kies eerst een klant.";
        getJson(cfg.urls.partyDetails, { id: p.id }).then(function (d) { if (state.party && state.party.id === p.id) { state.detail = d; renderPartyCard(); draw(); } }).catch(function () { });
        getJson(cfg.urls.partyDelivery, { id: p.id }).then(function (d) { if (state.party && state.party.id === p.id) { state.delivery = d; renderPartyCard(); renderChannels(); } }).catch(function () { });
        if (isClient()) loadCounts();
        renderChannels(); draw();
    }
    function clearParty() {
        carriedStages.length = 0; carriedOrders.length = 0;
        state.party = null; state.detail = null; state.delivery = null; state.stages = null; state.orders = null; state.selStages = {}; state.selOrders = {};
        $("#ic-party-id").value = ""; $("#ic-party-type").value = "";
        $("#ic-party-pick").hidden = false; $("#ic-party-card").hidden = true;
        $("#ic-project-field").hidden = false; $("#ic-contract-field").hidden = true;
        $$("#ic-modes button").forEach(function (b) { b.disabled = b.getAttribute("data-mode") !== "1"; });
        setCount(2, 0); setCount(3, 0);
        if (state.mode !== 1) setMode(1);
        loadRecents(); renderChannels(); draw();
        var pin = $("#ic-party-sel_input"); if (pin) pin.focus();
    }
    function addressLine(d) {
        if (!d) return "";
        var street = [d.street, d.houseNumber].filter(Boolean).join(" "); if (d.busNumber) street += " bus " + d.busNumber;
        var city = [d.postalCode, d.city].filter(Boolean).join(" ");
        return [street, city].filter(Boolean).join(", ");
    }
    function renderPartyCard() {
        var card = $("#ic-party-card");
        if (!state.party) { card.hidden = true; return; }
        var d = state.detail, del = state.delivery, name = (d && d.name) || state.party.text;
        var bits = [addressLine(d), d && d.vatNumber ? "BTW " + d.vatNumber : "", del && del.email ? del.email : ""].filter(Boolean);
        var peppol = del ? (del.peppolId ? '<span class="gl-v2-ic-peppol"><i class="ph ph-check-circle" aria-hidden="true"></i>Bereikbaar via Peppol</span>' : '<span class="gl-v2-ic-peppol is-off"><i class="ph ph-minus-circle" aria-hidden="true"></i>Niet gevonden op Peppol</span>') : "";
        card.innerHTML = '<span class="gl-v2-ic-avatar" aria-hidden="true">' + esc(initials(name)) + '</span><span class="gl-v2-stack gl-v2-ic-party-text"><b>' + esc(name) + '</b>' + (bits.length ? '<span>' + esc(bits.join(" · ")) + '</span>' : "") + peppol + '</span>' + (locked ? "" : '<button type="button" class="gl-v2-btn gl-v2-btn-text" id="ic-party-change">Wijzigen</button>') + '';
        card.hidden = false;
        var chg = $("#ic-party-change"); if (chg) chg.addEventListener("click", clearParty);
    }
    function loadRecents() {
        getJson(cfg.urls.recentParties, { issuerId: cfg.issuerId, take: 4 }).then(function (r) {
            var res = r.results || []; var box = $("#ic-recent"), items = $("#ic-recent-items");
            if (!res.length || state.party) { box.hidden = true; return; }
            items.innerHTML = res.map(function (x, i) { return '<button type="button" class="gl-v2-ic-chip" data-i="' + i + '">' + esc(x.text) + '</button>'; }).join("");
            box.hidden = false;
            $$("button", items).forEach(function (b) { b.addEventListener("click", function () { var x = res[parseInt(b.getAttribute("data-i"), 10)]; setParty({ id: x.id, text: x.text }); }); });
        }).catch(function () { });
    }

    // — project —
    function setProject(p, fromCombo) {
        state.project = p;
        if (!fromCombo) { syncing = true; comboSet(projectCombo, p ? p.id : "", p ? p.name : ""); syncing = false; }
        if (state.mode === 3) loadOrders(true);
        draw();
    }

    // ───────────────────────── data en betaaltermijn ─────────────────────────
    var dateInput = $("#ic-date"), termInput = $("#PaymentTermId"), vatInput = $("#VatTypeId");
    function currentTerm() { var id = termInput ? termInput.value : ""; return cfg.terms.filter(function (t) { return String(t.id) === String(id); })[0] || null; }
    function dueDate() {
        var d = isoToDate(dateInput.value), t = currentTerm(); if (!d) return null;
        var days = t ? (t.days || 0) : 0, r;
        if (t && t.type === 1) { r = new Date(d.getFullYear(), d.getMonth() + 1, 0); r.setDate(r.getDate() + days); }
        else { r = new Date(d.getFullYear(), d.getMonth(), d.getDate() + days); }
        return r;
    }
    function drawDue() { var r = dueDate(); $("#ic-due-help").innerHTML = r ? "vervalt op " + DAYS[r.getDay()] + " " + dmy(r) : "&nbsp;"; }
    [dateInput, termInput, vatInput].forEach(function (el) { if (el) el.addEventListener("change", function () { drawDue(); draw(); }); });
    if (vatInput) vatInput.addEventListener("change", function () { if (state.mode === 1) renderFreeLines(); });
    function vatById(id) { return cfg.vatTypes.filter(function (v) { return String(v.id) === String(id); })[0] || null; }
    function defaultVat() { return vatById(vatInput ? vatInput.value : cfg.vatTypeId) || cfg.vatTypes[0] || { id: "", code: "", pct: 0 }; }

    // ───────────────────────── opbouw ─────────────────────────
    function selectCarried() { carriedStages.forEach(function (c) { state.selStages[c.stageId] = true; }); }
    function setCount(mode, n) {
        var b = $('#ic-modes button[data-mode="' + mode + '"]'); if (!b) return;
        var em = b.querySelector("em");
        if (!n) { if (em) em.remove(); return; }
        if (!em) { em = document.createElement("em"); b.appendChild(em); }
        em.textContent = n;
    }
    function setMode(m) {
        if (locked) m = 2;
        state.mode = m; $("#ic-mode").value = m;
        $$("#ic-modes button").forEach(function (b) { var on = parseInt(b.getAttribute("data-mode"), 10) === m; b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); });
        if (m === 2) loadStages(); else if (m === 3) loadOrders();
        renderLinesPanel(); draw();
    }
    $$("#ic-modes button").forEach(function (b) { b.addEventListener("click", function () { if (!b.disabled) setMode(parseInt(b.getAttribute("data-mode"), 10)); }); });
    function loadCounts() { loadStages(true); loadOrders(false, true); }
    // De schijven van de lijst plus die van de overgenomen factuur of het concept (die staan er niet meer in als ze al gefactureerd zijn).
    // Bij een concept uit schijven (vergrendeld) zijn het precies de schijven van het concept, met hun bedragen zoals bewaard.
    function stageRows() {
        var carried = {}; carriedStages.forEach(function (c) { carried[c.stageId] = c; });
        if (locked) return carriedStages.slice();
        var rows = (state.stages || []).map(function (s) { return carried[s.stageId] || s; });
        var have = {}; rows.forEach(function (s) { have[s.stageId] = 1; });
        carriedStages.forEach(function (c) { if (!have[c.stageId]) rows.push(c); });
        return rows;
    }
    function loadStages(silent) {
        if (!isClient()) return Promise.resolve();
        if (locked && !state.stages) state.stages = [];
        if (state.stages) { if (!silent) renderLinesPanel(); return Promise.resolve(); }
        var id = state.party.id;
        return getJson(cfg.urls.stageLines, { clientId: partyNum() }).then(function (r) {
            if (!state.party || state.party.id !== id) return;
            state.stages = r.lines || []; setCount(2, state.stages.length);
            if (state.mode === 2) { renderLinesPanel(); draw(); }
        }).catch(function () { state.stages = []; if (state.mode === 2) renderLinesPanel(); });
    }
    function loadOrders(force, silent) {
        if (!isClient()) return Promise.resolve();
        if (state.orders && !force) { if (!silent) renderLinesPanel(); return Promise.resolve(); }
        var id = state.party.id;
        return getJson(cfg.urls.changeOrderLines, { clientId: partyNum(), projectId: state.project ? state.project.id : "" }).then(function (r) {
            if (!state.party || state.party.id !== id) return;
            state.orders = r.lines || []; state.selOrders = {}; setCount(3, state.orders.length);
            // regels van de overgenomen factuur of het concept die nog beschikbaar zijn, meteen aanvinken met hun percentage
            carriedOrders.forEach(function (c) {
                if (state.orders.some(function (o) { return String(o.detailId) === String(c.changeOrderDetailId); })) { state.selOrders[c.changeOrderDetailId] = true; if (c.stagePercentage) state.orderPct[c.changeOrderDetailId] = c.stagePercentage; }
            });
            if (state.mode === 3) { renderLinesPanel(); draw(); }
        }).catch(function () { state.orders = []; if (state.mode === 3) renderLinesPanel(); });
    }

    // ───────────────────────── verzendkanalen (stap 2) ─────────────────────────
    function renderChannels() {
        var ul = $("#ic-channels"), d = state.delivery;
        if (!state.party) { ul.innerHTML = '<li class="is-muted"><i class="ph ph-paper-plane-tilt" aria-hidden="true"></i><span>Kies eerst een klant om te zien hoe de factuur verstuurd wordt.</span></li>'; return; }
        if (!d) { ul.innerHTML = '<li class="is-muted"><i class="ph ph-circle-notch" aria-hidden="true"></i><span>De gegevens van de klant worden opgehaald…</span></li>'; return; }
        var rows = [];
        rows.push(d.peppolId ? '<li><i class="ph ph-check-circle" aria-hidden="true"></i><span>Peppol, via Octopus</span><small>' + esc(d.peppolId) + ' · de klant is bereikbaar</small></li>' : '<li class="is-muted"><i class="ph ph-minus-circle" aria-hidden="true"></i><span>Peppol</span><small>klant niet gevonden op Peppol</small></li>');
        rows.push(d.email ? '<li><i class="ph ph-envelope-simple" aria-hidden="true"></i><span>E-mail met pdf</span><small>' + esc(d.email) + '</small></li>' : '<li class="is-muted"><i class="ph ph-envelope-simple" aria-hidden="true"></i><span>E-mail met pdf</span><small>geen e-mailadres bij de klant</small></li>');
        rows.push('<li><i class="ph ph-arrows-left-right" aria-hidden="true"></i><span>Doorboeken naar Octopus</span><small>gebeurt bij het nummeren</small></li>');
        ul.innerHTML = rows.join("");
    }

    // ───────────────────────── lijnen ─────────────────────────
    var seqLine = 0;
    function newLine(src) { src = src || {}; return { k: ++seqLine, text: src.text || "", qty: src.quantity != null ? src.quantity : 1, price: src.unitPrice != null ? src.unitPrice : "", vat: src.vatTypeId != null ? String(src.vatTypeId) : null }; }
    state.lines = (cfg.lines && cfg.lines.length ? cfg.lines : [null]).map(function (l) { return newLine(l || {}); });

    function lineVat(l) { return (l.vat != null && vatById(l.vat)) || defaultVat(); }
    function lineNet(l) { return round2((parseNum(l.qty) || 1) * parseNum(l.price)); }

    function vatOptions(selected) {
        return cfg.vatTypes.map(function (v) { return '<option value="' + esc(v.id) + '"' + (String(v.id) === String(selected) ? " selected" : "") + '>' + esc(v.code + " · " + v.pct + " %") + '</option>'; }).join("");
    }
    function renderFreeLines() {
        var body = $("#ic-lines-body");
        body.innerHTML = state.lines.map(function (l, i) {
            return '<div class="gl-v2-ic-line" data-i="' + i + '">' +
                '<button type="button" class="gl-v2-ic-grip" draggable="true" aria-label="Lijn ' + (i + 1) + ' verslepen"><i class="ph ph-dots-six-vertical" aria-hidden="true"></i></button>' +
                '<div class="gl-v2-field-box"><input class="gl-v2-field-input" data-f="text" maxlength="200" autocomplete="off" aria-label="Omschrijving van lijn ' + (i + 1) + '" value="' + esc(l.text) + '" /></div>' +
                '<div class="gl-v2-field-box"><input class="gl-v2-field-input is-num" data-f="qty" inputmode="decimal" autocomplete="off" aria-label="Aantal van lijn ' + (i + 1) + '" value="' + esc(l.qty === "" ? "" : String(l.qty).replace(".", ",")) + '" /></div>' +
                '<div class="gl-v2-field-box"><input class="gl-v2-field-input is-num" data-f="price" inputmode="decimal" autocomplete="off" aria-label="Eenheidsprijs van lijn ' + (i + 1) + '" placeholder="0,00" value="' + esc(l.price === "" ? "" : fmt(parseNum(l.price))) + '" /></div>' +
                '<select class="gl-v2-ic-vat" data-f="vat" aria-label="Btw van lijn ' + (i + 1) + '">' + vatOptions(lineVat(l).id) + '</select>' +
                '<span class="is-total" data-total>' + euro(lineNet(l)) + '</span>' +
                '<button type="button" class="gl-v2-ic-del" aria-label="Lijn ' + (i + 1) + ' verwijderen"' + (state.lines.length === 1 ? " disabled" : "") + '><i class="ph ph-trash" aria-hidden="true"></i></button></div>';
        }).join("");
    }
    var linesBody = $("#ic-lines-body");
    linesBody.addEventListener("input", function (e) {
        var row = e.target.closest(".gl-v2-ic-line"), f = e.target.getAttribute("data-f"); if (!row || !f) return;
        var l = state.lines[parseInt(row.getAttribute("data-i"), 10)];
        if (f === "text") l.text = e.target.value; else if (f === "qty") l.qty = e.target.value; else if (f === "price") l.price = e.target.value;
        $("[data-total]", row).textContent = euro(lineNet(l)); draw();
    });
    linesBody.addEventListener("change", function (e) {
        var row = e.target.closest(".gl-v2-ic-line"); if (!row) return;
        var l = state.lines[parseInt(row.getAttribute("data-i"), 10)], f = e.target.getAttribute("data-f");
        if (f === "vat") { l.vat = e.target.value; $("[data-total]", row).textContent = euro(lineNet(l)); draw(); }
    });
    linesBody.addEventListener("focusout", function (e) {
        var f = e.target.getAttribute && e.target.getAttribute("data-f"); if (f !== "price" && f !== "qty") return;
        var row = e.target.closest(".gl-v2-ic-line"), l = state.lines[parseInt(row.getAttribute("data-i"), 10)];
        if (f === "price" && String(e.target.value).trim() !== "") { l.price = parseNum(e.target.value); e.target.value = fmt(l.price); }
        if (f === "qty") { if (String(e.target.value).trim() === "") { l.qty = 1; e.target.value = "1"; } else { l.qty = parseNum(e.target.value); e.target.value = String(l.qty).replace(".", ","); } }
    });
    linesBody.addEventListener("keydown", function (e) {
        // Enter in de prijs van de laatste lijn voegt een lijn toe (Tab werkt gewoon)
        if (e.key !== "Enter" || e.target.getAttribute("data-f") !== "price") return;
        var row = e.target.closest(".gl-v2-ic-line"), i = parseInt(row.getAttribute("data-i"), 10);
        if (i === state.lines.length - 1) { e.preventDefault(); addLine(); }
    });
    linesBody.addEventListener("click", function (e) {
        var del = e.target.closest(".gl-v2-ic-del"); if (!del || del.disabled) return;
        var i = parseInt(del.closest(".gl-v2-ic-line").getAttribute("data-i"), 10);
        state.lines.splice(i, 1); if (!state.lines.length) state.lines.push(newLine()); renderFreeLines(); draw();
    });
    // herschikken
    var dragFrom = null;
    linesBody.addEventListener("dragstart", function (e) { var g = e.target.closest(".gl-v2-ic-grip"); if (!g) return; var row = g.closest(".gl-v2-ic-line"); dragFrom = parseInt(row.getAttribute("data-i"), 10); row.classList.add("is-dragging"); try { e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", String(dragFrom)); } catch (x) { } });
    linesBody.addEventListener("dragover", function (e) { if (dragFrom == null) return; e.preventDefault(); $$(".gl-v2-ic-line.is-over", linesBody).forEach(function (r) { r.classList.remove("is-over"); }); var row = e.target.closest(".gl-v2-ic-line"); if (row) row.classList.add("is-over"); });
    linesBody.addEventListener("drop", function (e) {
        if (dragFrom == null) return; e.preventDefault();
        var row = e.target.closest(".gl-v2-ic-line"); if (!row) return;
        var to = parseInt(row.getAttribute("data-i"), 10), moved = state.lines.splice(dragFrom, 1)[0]; state.lines.splice(to, 0, moved);
        dragFrom = null; renderFreeLines(); draw();
    });
    linesBody.addEventListener("dragend", function () { dragFrom = null; $$(".gl-v2-ic-line", linesBody).forEach(function (r) { r.classList.remove("is-dragging", "is-over"); }); });
    function addLine() { state.lines.push(newLine()); renderFreeLines(); draw(); var rows = $$(".gl-v2-ic-line", linesBody); var last = rows[rows.length - 1]; if (last) $("[data-f=text]", last).focus(); }
    $("#ic-add-line").addEventListener("click", addLine);

    // — openstaande posten en offerte: kiezen —
    function groupBy(arr, key) { var out = [], idx = {}; arr.forEach(function (x) { var k = x[key] || ""; if (idx[k] == null) { idx[k] = out.length; out.push({ name: k, items: [] }); } out[idx[k]].items.push(x); }); return out; }
    function renderPick() {
        var host = $("#ic-lines-pick");
        if (!isClient()) { host.innerHTML = '<div class="gl-v2-ic-empty">Kies eerst een klant in stap 1.</div>'; return; }
        if (state.mode === 2) {
            if (state.stages == null) { host.innerHTML = '<div class="gl-v2-ic-empty">Openstaande posten worden geladen…</div>'; return; }
            var rowsAll = stageRows();
            if (!rowsAll.length) { host.innerHTML = '<div class="gl-v2-ic-empty">Geen factureerbare schijven gevonden voor deze klant.</div>'; return; }
            host.innerHTML = (locked ? '<div class="gl-v2-notice gl-v2-notice-compact is-info"><span class="gl-v2-notice-icon"><i class="ph ph-lock-simple" aria-hidden="true"></i></span><span class="gl-v2-notice-text">Dit concept is opgebouwd uit schijven. Bedrijf, klant, project, opbouw, btw en de schijven liggen vast.</span></div>' : "") + groupBy(rowsAll, "groupName").map(function (g, gi) {
                var all = g.items.every(function (s) { return state.selStages[s.stageId]; });
                return '<div class="gl-v2-ic-pickgroup"><div class="gl-v2-ic-pickgroup-head"><input type="checkbox" data-all="' + gi + '"' + (all ? " checked" : "") + (locked ? " disabled" : "") + ' aria-label="Alles kiezen in ' + esc(g.name || "deze groep") + '" /><span>' + esc(g.name || "Schijven") + '</span><small>' + g.items.length + ' post(en)</small></div>' +
                    g.items.map(function (s) { var on = !!state.selStages[s.stageId]; return '<label class="gl-v2-ic-pickrow' + (on ? " is-on" : "") + '"><input type="checkbox" data-stage="' + s.stageId + '"' + (on ? " checked" : "") + (locked ? " disabled" : "") + ' /><span>' + esc(s.text) + (s.unitName ? '<small>' + esc(s.unitName) + '</small>' : "") + '</span><span class="is-num">' + esc(String(s.vat).replace(".", ",")) + ' %</span><span class="is-num">' + euro(s.amount) + '</span></label>'; }).join("") + '</div>';
            }).join("");
        } else {
            if (state.orders == null) { host.innerHTML = '<div class="gl-v2-ic-empty">Offertes worden geladen…</div>'; return; }
            var missingOrders = carriedOrders.filter(function (c) { return !state.orders.some(function (o) { return String(o.detailId) === String(c.changeOrderDetailId); }); });
            var orderNote = missingOrders.length ? '<div class="gl-v2-notice gl-v2-notice-compact is-warning"><span class="gl-v2-notice-icon"><i class="ph ph-warning" aria-hidden="true"></i></span><span class="gl-v2-notice-text">' + missingOrders.length + (missingOrders.length === 1 ? " regel uit de oorspronkelijke factuur is" : " regels uit de oorspronkelijke factuur zijn") + ' niet meer beschikbaar om te factureren en staat niet in de lijst.</span></div>' : "";
            if (!state.orders.length) { host.innerHTML = orderNote + '<div class="gl-v2-ic-empty">Geen goedgekeurde wijzigingsopdrachten om te factureren' + (state.project ? " voor dit project" : "") + '.</div>'; return; }
            host.innerHTML = orderNote + groupBy(state.orders, "groupName").map(function (g) {
                return '<div class="gl-v2-ic-pickgroup"><div class="gl-v2-ic-pickgroup-head"><span>' + esc(g.name) + '</span><small>' + g.items.length + ' regel(s)</small></div>' +
                    g.items.map(function (s) {
                        var on = !!state.selOrders[s.detailId], pct = state.orderPct[s.detailId] != null ? state.orderPct[s.detailId] : 100;
                        return '<div class="gl-v2-ic-pickrow' + (on ? " is-on" : "") + '" style="grid-template-columns:22px minmax(0,1fr) 84px 110px"><input type="checkbox" data-order="' + s.detailId + '"' + (on ? " checked" : "") + ' aria-label="' + esc(s.text) + '" /><span>' + esc(s.text) + (s.unitName ? '<small>' + esc(s.unitName) + ' · saldo ' + euro(s.baseAmount) + '</small>' : '<small>saldo ' + euro(s.baseAmount) + '</small>') + '</span>' +
                            '<span><input class="gl-v2-ic-pct" data-pct="' + s.detailId + '" inputmode="decimal" value="' + esc(String(pct).replace(".", ",")) + '" aria-label="Percentage voor ' + esc(s.text) + '"' + (on ? "" : " disabled") + ' /></span><span class="is-num" data-calc="' + s.detailId + '">' + euro(orderAmount(s)) + '</span></div>';
                    }).join("") + '</div>';
            }).join("");
        }
    }
    function orderAmount(s) { var p = state.orderPct[s.detailId]; p = p == null ? 100 : parseNum(p); p = Math.min(100, Math.max(0, p)); return round2(s.baseAmount * p / 100); }
    var pickHost = $("#ic-lines-pick");
    pickHost.addEventListener("change", function (e) {
        var t = e.target;
        if (t.hasAttribute("data-stage")) { state.selStages[t.getAttribute("data-stage")] = t.checked; }
        else if (t.hasAttribute("data-all")) { var g = groupBy(stageRows(), "groupName")[parseInt(t.getAttribute("data-all"), 10)]; g.items.forEach(function (s) { state.selStages[s.stageId] = t.checked; }); }
        else if (t.hasAttribute("data-order")) { state.selOrders[t.getAttribute("data-order")] = t.checked; }
        else return;
        renderPick(); draw();
    });
    pickHost.addEventListener("input", function (e) {
        var id = e.target.getAttribute && e.target.getAttribute("data-pct"); if (!id) return;
        state.orderPct[id] = e.target.value;
        var s = state.orders.filter(function (x) { return String(x.detailId) === String(id); })[0];
        var cell = $('[data-calc="' + id + '"]', pickHost); if (cell && s) cell.textContent = euro(orderAmount(s));
        draw();
    });

    function renderLinesPanel() {
        var free = state.mode === 1;
        $("#ic-lines-free").hidden = free ? false : true; $("#ic-lines-pick").hidden = free;
        $("#ic-lines-title").textContent = free ? "Lijnen" : state.mode === 2 ? "Uit openstaande posten" : "Uit offerte";
        $("#ic-lines-sub").textContent = free ? "sleep om te herschikken · Tab springt naar het volgende veld" : state.mode === 2 ? "vink de posten aan die op deze factuur komen" : "vink de regels aan en kies welk deel je factureert";
        if (free) renderFreeLines(); else renderPick();
    }

    // alle lijnen die op de factuur komen (voor totalen, voorbeeld en posten)
    function activeLines() {
        var out = [];
        if (state.mode === 1) {
            state.lines.forEach(function (l) { var net = lineNet(l); if (!String(l.text).trim() && net === 0) return; var v = lineVat(l); out.push({ text: l.text, qty: parseNum(l.qty) || 1, unit: parseNum(l.price), net: net, pct: v.pct, vatId: v.id, vatCode: v.code, group: "" }); });
        } else if (state.mode === 2) {
            stageRows().forEach(function (s) { if (state.selStages[s.stageId]) out.push({ text: s.text, qty: 1, unit: s.amount, net: s.amount, pct: s.vat, group: s.groupName || "Schijven", stage: s }); });
        } else {
            (state.orders || []).forEach(function (s) { if (state.selOrders[s.detailId]) { var a = orderAmount(s); if (a !== 0) out.push({ text: s.text, qty: 1, unit: a, net: a, pct: s.vat, group: "Wijzigingsopdrachten", order: s }); } });
        }
        return out;
    }
    function totals(lines) {
        var by = {}, order = [];
        lines.forEach(function (l) { var k = String(l.pct); if (!by[k]) { by[k] = { pct: l.pct, net: 0 }; order.push(k); } by[k].net = round2(by[k].net + l.net); });
        var rates = order.map(function (k) { var r = by[k]; r.vat = round2(r.net * r.pct / 100); return r; }).sort(function (a, b) { return a.pct - b.pct; });
        var excl = round2(rates.reduce(function (s, r) { return s + r.net; }, 0)), vat = round2(rates.reduce(function (s, r) { return s + r.vat; }, 0));
        return { rates: rates, excl: excl, vat: vat, incl: round2(excl + vat) };
    }
    function drawTotals(t) {
        var rows = t.rates.map(function (r) { return '<div><span>BTW ' + String(r.pct).replace(".", ",") + ' % op ' + euro(r.net) + '</span><span>' + euro(r.vat) + '</span></div>'; }).join("");
        $("#ic-totals").innerHTML = '<div><span>Subtotaal</span><span>' + euro(t.excl) + '</span></div>' + (rows || '<div><span>BTW</span><span>' + euro(0) + '</span></div>') + '<div class="is-grand"><span>Te betalen</span><span>' + euro(t.incl) + '</span></div>';
    }

    // ───────────────────────── tekstvelden en bijlage ─────────────────────────
    ["HeaderDescription", "DetailDescription", "FooterDescription"].forEach(function (id) { var el = document.getElementById(id); if (el) el.addEventListener("input", draw); });
    // PDF-bijlage: het gedeelde Dropzone-component (GlV2/_Dropzone) regelt kiezen, slepen, controleren en wissen; wij volgen enkel de events.
    document.addEventListener("gl-v2:dropzone-change", function (e) { if (e.target.id === "PdfAppendix_zone") { state.file = e.detail.file; draw(); } });
    document.addEventListener("gl-v2:dropzone-remove-current", function (e) { if (e.target.id === "PdfAppendix_zone") { var f = $("#ic-remove-appendix"); if (f) f.value = "true"; draw(); } });

    // ───────────────────────── live voorbeeld ─────────────────────────
    function validHex(c) { return /^#[0-9a-fA-F]{6}$/.test(String(c || "").trim()); }
    function nl2br(s) { return esc(s); }
    function sheet() {
        var iss = state.issuer || {}, accent = validHex(iss.color) ? iss.color.trim() : "#00532D";
        var lines = activeLines(), t = totals(lines), credit = state.credit, d = state.detail;
        var title = credit ? "Proforma creditnota" : "Proforma factuur";
        var due = dueDate(), date = isoToDate(dateInput.value);
        var meta = [["Type", title], [credit ? "Creditnotanummer" : "Factuurnummer", "volgt bij nummeren"], [credit ? "Datum" : "Factuurdatum", date ? dmy(date) : "—"], ["Vervaldatum", due ? dmy(due) : "—"]];
        if (state.project) meta.push(["Project", state.project.name]);
        var clientName = d && d.name ? d.name : (state.party ? state.party.text : "");
        var clientLines = [d && [d.street, d.houseNumber].filter(Boolean).join(" ") + (d && d.busNumber ? " bus " + d.busNumber : ""), d && [d.postalCode, d.city].filter(Boolean).join(" "), d && d.vatNumber].filter(function (x) { return x && String(x).trim(); });
        var groups = groupBy(lines, "group"), rows = "";
        groups.forEach(function (g) {
            if (groups.length > 1 && g.name) rows += '<div class="gl-v2-ps-group">' + esc(g.name) + '</div>';
            g.items.forEach(function (l) { rows += '<div class="gl-v2-ps-tr"><span class="d">' + esc(l.text) + '</span><span class="r">' + esc(String(l.qty).replace(".", ",")) + '</span><span class="r">' + euro(l.unit) + '</span><span class="r"><i class="gl-v2-ps-pill" style="background:#F0EDE6">' + esc(String(l.pct).replace(".", ",")) + ' %</i></span><span class="r">' + euro(l.net) + '</span></div>'; });
        });
        if (!rows) rows = '<div class="gl-v2-ps-tr"><span class="d" style="color:#7a8a78">lijnen verschijnen hier in stap 3</span><span></span><span></span><span></span><span></span></div>';
        var sums;
        if (t.rates.length > 1) {
            sums = '<div class="gl-v2-ps-tarief is-head"><span>TARIEF</span><span class="r">MAATSTAF</span><span class="r">BTW</span><span class="r">TOTAAL</span></div>' +
                t.rates.map(function (r) { return '<div class="gl-v2-ps-tarief"><span class="s">' + esc(String(r.pct).replace(".", ",")) + ' %</span><span class="r">' + euro(r.net) + '</span><span class="r">' + euro(r.vat) + '</span><span class="r">' + euro(r.net + r.vat) + '</span></div>'; }).join("") +
                '<div class="gl-v2-ps-tarief is-total"><span class="s">Totaal</span><span class="r">' + euro(t.excl) + '</span><span class="r">' + euro(t.vat) + '</span><span class="r">' + euro(t.incl) + '</span></div>';
        } else {
            sums = '<div class="gl-v2-ps-sum"><span>Totaal excl. btw</span><span>' + euro(t.excl) + '</span></div><div class="gl-v2-ps-sum"><span>' + (t.rates.length === 1 ? "Btw " + esc(String(t.rates[0].pct).replace(".", ",")) + " %" : "Btw") + '</span><span>' + euro(t.vat) + '</span></div>';
        }
        var head = $("#HeaderDescription").value, detail = $("#DetailDescription").value, foot = $("#FooterDescription").value;
        var pay = [["Bedrag", euro(t.incl), true], ["Begunstigde", [iss.legalName || iss.name, iss.legalForm].filter(Boolean).join(" "), false], ["IBAN", ibanGroups(bankIban()), true]].filter(function (p) { return p[1]; });
        var addr1 = [iss.addressLine1, iss.addressLine2].filter(Boolean).join(", "), addr2 = [iss.postalCode, iss.city].filter(Boolean).join(" ");
        var contact = [[iss.phone, iss.email].filter(Boolean).join(" · "), iss.website].filter(Boolean).join("<br>");
        var legal = [[iss.legalName || iss.name, iss.legalForm].filter(Boolean).join(" "), iss.vat ? "btw " + iss.vat : ""].filter(Boolean).join(" · ");
        var host = $("#ic-sheet");
        host.innerHTML = '<div class="gl-v2-ps" style="--accent:' + esc(accent) + ';--pill6:#EEF3EA;--paytint:#F2F7EE"><div class="gl-v2-ps-band"></div>' +
            '<div class="gl-v2-ps-head"><div class="gl-v2-ps-logo">' + (iss.logo ? '<img src="' + esc(iss.logo) + '" alt="" />' : '<b style="font-size:calc(var(--pt)*14)">' + esc(iss.name || cfg.issuerName) + '</b>') + '</div><div class="gl-v2-ps-title"><span class="gl-v2-ps-kicker">VERKOOP</span><span class="gl-v2-ps-h1">' + esc(title) + '</span></div></div>' +
            '<div class="gl-v2-ps-content"><div class="gl-v2-ps-meta"><div class="gl-v2-ps-metatable">' + meta.map(function (r) { return '<b>' + esc(r[0]) + '</b><span>' + esc(r[1]) + '</span>'; }).join("") + '</div>' +
            '<div class="gl-v2-ps-addr">' + (clientName ? '<b>' + esc(clientName.toUpperCase()) + '</b>' + clientLines.map(function (l) { return '<span>' + esc(l) + '</span>'; }).join("") : '<span style="color:#7a8a78">klant volgt in stap 1</span>') + '</div></div>' +
            ((head.trim() || detail.trim()) ? '<div class="gl-v2-ps-desc"><span class="gl-v2-ps-label">BESCHRIJVING</span><div class="gl-v2-ps-header">' + nl2br(head) + '</div><div class="gl-v2-ps-detail">' + nl2br(detail) + '</div></div>' : "") +
            '<div class="gl-v2-ps-table"><div class="gl-v2-ps-tr is-head"><span>OMSCHRIJVING</span><span class="r">AANTAL</span><span class="r">EENHEIDSPRIJS</span><span class="r">BTW</span><span class="r">TOTAAL EXCL.</span></div>' + rows + '</div>' +
            '<div class="gl-v2-ps-totals"><div class="gl-v2-ps-mentions"></div><div class="gl-v2-ps-sums">' + sums + '<div class="gl-v2-ps-grand"><span>Te betalen</span><b>' + euro(t.incl) + '</b></div></div></div>' +
            (foot.trim() ? '<div class="gl-v2-ps-extra"><span class="gl-v2-ps-label">BIJKOMENDE VERMELDING</span><div>' + nl2br(foot) + '</div></div>' : "") +
            (state.file ? '<div class="gl-v2-ps-extra"><div>bijlage: ' + esc(state.file.name) + '</div></div>' : "") +
            '<span class="gl-v2-ps-fill"></span><div class="gl-v2-ps-pay"><div class="gl-v2-ps-pay-main"><span class="gl-v2-ps-label">BETALINGSGEGEVENS</span><div class="gl-v2-ps-pay-grid">' + pay.map(function (p) { return '<div><small>' + esc(p[0].toUpperCase()) + '</small><span class="' + (p[2] ? "g" : "") + '">' + esc(p[1]) + '</span></div>'; }).join("") + '</div><em>' + (credit ? "Proforma creditnota" : "Proforma factuur") + ' — dit is nog geen definitieve factuur en nog niet te betalen. Het nummer en de gestructureerde mededeling volgen bij het nummeren.</em></div></div></div>' +
            '<div class="gl-v2-ps-foot"><div class="gl-v2-ps-footrow"><span class="c1">' + esc(addr1) + '<br>' + esc(addr2) + '</span><span class="c2">' + contact.replace(/&/g, "&amp;") + '</span><span class="c3">' + esc(legal) + '</span><span class="c4">1 / 1</span></div><div class="gl-v2-ps-gap"></div><div class="gl-v2-ps-band"></div></div></div>';
        drawTotals(t);
    }
    var raf = 0;
    function draw() { if (raf) return; raf = requestAnimationFrame(function () { raf = 0; sheet(); }); }

    // ───────────────────────── controle en bewaren ─────────────────────────
    // Het Foutoverzicht (punt 24, GlV2ErrorSummary): dezelfde kaart, sticky balk en regels als elk ander gl-v2-formulier. Onze eigen controles
    // komen als validator mee; een klik op een fout springt naar de stap met het veld. De server controleert alles opnieuw (BuildInvoiceDraftBoAsync).
    function validate() {
        var errs = [];
        if (!state.party) errs.push({ field: "Klant of leverancier", message: "Kies een klant of leverancier.", location: "Partijen", target: $("#ic-party-field") });
        if (!dateInput.value) errs.push({ field: "Factuurdatum", message: "Vul de factuurdatum in.", location: "Instellingen", target: $("#ic-date_picker") });
        var lines = activeLines(), lt = state.mode === 1 ? $("#ic-lines-free") : $("#ic-lines-pick");
        if (state.mode === 1) {
            if (!lines.length) errs.push({ field: "Lijnen", message: "Voeg minstens één lijn met een omschrijving of bedrag toe.", location: "Lijnen", target: lt });
            else if (lines.some(function (l) { return !String(l.text).trim(); })) errs.push({ field: "Lijnen", message: "Elke lijn heeft een omschrijving nodig.", location: "Lijnen", target: lt });
        } else if (!lines.length) errs.push({ field: "Lijnen", message: state.mode === 2 ? "Kies minstens één openstaande post." : "Kies minstens één regel uit de offerte met een bedrag.", location: "Lijnen", target: lt });
        if (state.file && !/\.pdf$/i.test(state.file.name) && state.file.type !== "application/pdf") errs.push({ field: "PDF-bijlage", message: "De bijlage moet een PDF-bestand zijn.", location: "Tekst", target: $("#PdfAppendix_zone") });
        return errs;
    }
    var errorSummary = window.GlV2ErrorSummary ? window.GlV2ErrorSummary.init({
        form: form,
        container: document.getElementById("gl-v2-error-summary"),
        validators: [function (errors) { validate().forEach(function (e) { errors.push(e); }); }],
        // geen tabbar: de stap met het veld openen
        onNavigate: function (target) { var panel = target && target.closest ? target.closest("[data-ic-panel]") : null; if (panel) show(parseInt(panel.getAttribute("data-ic-panel"), 10)); }
    }) : null;
    function blocked() { var errs = errorSummary ? errorSummary.validate() : validate(); return errs.length > 0; }
    function serverError(message) { if (errorSummary) errorSummary.render([{ field: "", message: message, location: "", target: null }]); else window.alert(message); }

    function buildData() {
        var fd = new FormData(form);
        fd.set("StartAs", "0");
        fd.set("IsCreditNote", state.credit ? "true" : "false");
        var lines = activeLines(), i = 0;
        function add(k, v) { fd.append("Lines[" + i + "]." + k, v); }
        lines.forEach(function (l) {
            if (state.mode === 1) {
                add("Text", String(l.text).trim()); add("Quantity", postNum(l.qty, 3)); add("UnitPrice", postNum(l.unit)); add("Price", postNum(l.net));
                add("VatPercentage", postNum(l.pct)); if (l.vatId !== "" && l.vatId != null) add("VatTypeId", l.vatId); if (l.vatCode) add("VatCode", l.vatCode);
                add("IsSelected", "true"); add("LineType", "Free"); add("GroupName", "Vrije lijnen"); add("UtilityCost", "false");
            } else if (state.mode === 2) {
                var s = l.stage; add("IsSelected", "true"); add("Text", s.text); add("Price", postNum(s.amount)); add("VatPercentage", postNum(s.vat));
                if (s.unitId != null) add("UnitId", s.unitId); add("PaymentStageId", s.stageId); add("LineType", "Stages"); if (s.groupName) add("GroupName", s.groupName); add("UtilityCost", "false");
            } else {
                var o = l.order, p = state.orderPct[o.detailId]; p = p == null ? 100 : Math.min(100, Math.max(0, parseNum(p)));
                add("IsSelected", "true"); add("Text", o.text); add("Price", postNum(l.net)); add("VatPercentage", postNum(o.vat));
                add("LineType", "ChangeOrders"); add("GroupName", "Wijzigingsopdrachten"); add("ChangeOrderId", o.changeOrderId); add("ChangeOrderDetailId", o.detailId);
                if (o.unitId != null) add("UnitId", o.unitId); add("StagePercentage", postNum(p));
            }
            i++;
        });
        return fd;
    }
    function busy(on) { state.busy = on;[saveDraft, next, prev].forEach(function (b) { if (b) b.disabled = on || (b === prev && state.step === 1); }); }
    function save() {
        busy(true);
        return fetch(cfg.urls.save, { method: "POST", body: buildData(), credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } }).then(function (r) {
            if ((r.headers.get("content-type") || "").indexOf("json") < 0) throw new Error("De server gaf geen geldig antwoord (" + r.status + "). Meld je zo nodig opnieuw aan en probeer opnieuw.");
            return r.json();
        }).then(function (r) { busy(false); if (!r.ok) throw new Error(r.error || "De factuur kon niet bewaard worden."); return r; }, function (e) { busy(false); throw e; });
    }
    saveDraft.addEventListener("click", function () {
        if (blocked()) return;
        save().then(function (r) {
            if (edit) window.location.assign(r.returnUrl || (cfg.urls.detail + "?id=" + encodeURIComponent(r.invoiceId) + "&issuerCompanyId=" + encodeURIComponent(r.issuerCompanyId)));
            else window.location.assign(cfg.urls.index);
        }).catch(function (e) { serverError(e.message); });
    });
    // ───────────────────────── start ─────────────────────────
    getJson(cfg.urls.issuerSheet, { issuerId: cfg.issuerId }).then(function (d) { state.issuer = d; draw(); }).catch(function () { });
    if (cfg.partyId && cfg.partyType) {
        var prefix = CP[cfg.partyType] || "ca";
        setParty({ id: prefix + ":" + cfg.partyId, text: cfg.partyName || "" });
        selectCarried();
        getJson(cfg.urls.partyDetails, { id: prefix + ":" + cfg.partyId }).then(function (d) { if (d && state.party) { state.party.text = d.name || ""; } });
    } else { loadRecents(); }
    if (cfg.projectId) getJson(cfg.urls.projectName, { id: cfg.projectId }).then(function (r) { if (r && r.name) setProject({ id: r.id, name: r.name }); });
    if (cfg.contractId) { state.contract = { id: cfg.contractId, name: "Gekoppeld contract" }; syncing = true; comboSet(contractCombo, cfg.contractId, "Gekoppeld contract (#" + cfg.contractId + ")"); syncing = false; }
    $$("#ic-modes button").forEach(function (b) { b.disabled = (b.getAttribute("data-mode") !== "1" && !isClient()) || locked; });
    setMode(state.mode === 1 || isClient() ? state.mode : 1);
    if (locked) {
        // concept uit schijven: klant, project, contract en opbouw liggen vast (zoals in het oude EditDraft)
        $("#ic-party-pick").hidden = true; $("#ic-recent").hidden = true; var nl = $(".gl-v2-ic-newparty"); if (nl) nl.hidden = true;
        if (window.GlV2Combo) { window.GlV2Combo.setDisabled(projectCombo, true); window.GlV2Combo.setDisabled(contractCombo, true); }
        $("#ic-mode-help").textContent = "De opbouw ligt vast bij een concept uit schijven.";
    }
    drawDue(); show(1); draw();
})();
