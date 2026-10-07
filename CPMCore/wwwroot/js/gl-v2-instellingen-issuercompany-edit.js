// gl-v2 — Instellingen/IssuerCompaniesEditV2 ("Bedrijf bewerken", design-handoff punt 24c). Zes tabs in één form:
//   tabs (onthouden via ?tab=) · wijzigingen per tab tellen ("2 wijzigingen in Algemeen · 1 in Facturatie") · logo-sleepvak met voorbeeld ·
//   bankrekening-/nummerreeks-modals (posten naar de bestaande acties) · betaaltermijnen en uurtarieven als lijst + modal (client-side → verborgen
//   PaymentTerms[i].*/UserRates[i].*-velden) · Peppol-voorstel · EPC-QR-voorbeeld · kleuren, sjabloon-JSON-validatie, e-mail-editor met veld-chips en
//   een live PDF-voorbeeld (bestaande preview-actie) · Octopus (dossier kiezen koppelt meteen om) · verlaat-bewaking.
(function () {
    "use strict";

    var form = document.getElementById("gl-v2-ice-form");
    if (!form) return;

    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }
    function modalFor(id) { var el = document.getElementById(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function token() { var t = form.querySelector('input[name="__RequestVerificationToken"]'); return t ? t.value : ""; }
    function setSelect(id, value) {
        var wrap = document.getElementById(id + "_select");
        var opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + String(value).replace(/"/g, "") + '"]');
        if (opt) opt.click();
    }
    function setDate(id, iso) {
        var h = document.getElementById(id), t = document.getElementById(id + "_text");
        if (h) h.value = iso || "";
        if (t) t.value = iso ? iso.split("-").reverse().join("/") : "";
    }
    function fire(el, type) { el.dispatchEvent(new Event(type, { bubbles: true })); }
    function postTo(url) {
        var f = document.createElement("form");
        f.method = "post"; f.action = url; f.style.display = "none";
        var t = document.createElement("input"); t.type = "hidden"; t.name = "__RequestVerificationToken"; t.value = token(); f.appendChild(t);
        document.body.appendChild(f); f.submit();
    }

    // ── Tabs ───────────────────────────────────────────────────────────────────────────────────────
    var tabs = $$("#gl-v2-ice-tabbar [data-ice-tab]");
    var panels = $$("[data-tab-panel]");
    var currentTab = form.getAttribute("data-start-tab") || "algemeen";
    var onTab = {};
    function showTab(key, push) {
        currentTab = key;
        tabs.forEach(function (t) { var on = t.getAttribute("data-ice-tab") === key; t.classList.toggle("is-active", on); t.setAttribute("aria-selected", on ? "true" : "false"); });
        panels.forEach(function (p) { p.hidden = p.getAttribute("data-tab-panel") !== key; });
        if (push !== false && window.history && history.replaceState) {
            var u = new URL(window.location.href); u.searchParams.set("tab", key); history.replaceState(null, "", u.toString());
        }
        if (onTab[key]) onTab[key]();
    }
    tabs.forEach(function (t) {
        t.addEventListener("click", function () { showTab(t.getAttribute("data-ice-tab")); });
        t.addEventListener("keydown", function (e) {
            var i = tabs.indexOf(t), n = null;
            if (e.key === "ArrowRight") n = tabs[(i + 1) % tabs.length]; else if (e.key === "ArrowLeft") n = tabs[(i - 1 + tabs.length) % tabs.length];
            if (n) { e.preventDefault(); n.focus(); n.click(); }
        });
    });

    // ── Wijzigingen per tab ───────────────────────────────────────────────────────────────────────
    var tabLabels = { algemeen: "Algemeen", facturatie: "Facturatie", bank: "Bankrekeningen", boekhouding: "Boekhouding", layout: "Lay-out & e-mail", uurtarieven: "Uurtarieven" };
    var initial = {}, dirty = {}, listKeys = {};
    function valueOf(el) { return el.type === "checkbox" ? (el.checked ? "1" : "0") : el.type === "file" ? (el.files && el.files.length ? "f" : "") : el.value; }
    function panelOf(el) { var p = el.closest("[data-tab-panel]"); return p ? p.getAttribute("data-tab-panel") : null; }
    function snapshot() {
        $$("[data-tab-panel] [name]").forEach(function (el) {
            if (el.closest("[data-ice-skip]") || el.name === "__RequestVerificationToken") return;
            initial[el.name + "|" + (el.type === "checkbox" ? "c" : "v")] = valueOf(el);
        });
    }
    function track(el) {
        var tab = panelOf(el); if (!tab || el.closest("[data-ice-skip]") || !el.name) return;
        var key = el.name + "|" + (el.type === "checkbox" ? "c" : "v");
        dirty[tab] = dirty[tab] || {};
        if (initial.hasOwnProperty(key) && initial[key] === valueOf(el)) delete dirty[tab][key]; else dirty[tab][key] = 1;
        refreshDirty();
    }
    function markList(tab, key, changed) { dirty[tab] = dirty[tab] || {}; if (changed) dirty[tab]["list:" + key] = 1; else delete dirty[tab]["list:" + key]; refreshDirty(); }
    function refreshDirty() {
        var parts = [], total = 0;
        Object.keys(tabLabels).forEach(function (k) {
            var n = dirty[k] ? Object.keys(dirty[k]).length : 0; total += n;
            var dot = $('[data-ice-dirty-dot="' + k + '"]'); if (dot) dot.hidden = n === 0;
            if (n > 0) parts.push(n + (n === 1 ? " wijziging" : " wijzigingen") + " in " + tabLabels[k]);
        });
        var badge = document.getElementById("ice-dirty-badge"); if (badge) badge.hidden = total === 0;
        var txt = document.getElementById("ice-changes"); if (txt) txt.textContent = parts.join(" · ");
    }
    form.addEventListener("input", function (e) { if (e.target.name) track(e.target); });
    form.addEventListener("change", function (e) { if (e.target.name) track(e.target); });
    function isDirty() { return Object.keys(dirty).some(function (k) { return Object.keys(dirty[k]).length > 0; }); }
    function warn(e) { if (isDirty()) { e.preventDefault(); e.returnValue = ""; } }
    window.addEventListener("beforeunload", warn);

    // ── Logo: sleepvak + voorbeeld ────────────────────────────────────────────────────────────────
    var dropZone = $(".gl-v2-ice-logo-drop"), fileInput = document.getElementById("LogoUpload");
    if (dropZone && fileInput) {
        var titleEl = $(".gl-v2-ice-logo-drop-title", dropZone), preview = document.getElementById("ice-logo-preview");
        function showFile(file) {
            if (!file) return;
            if (titleEl) titleEl.textContent = file.name;
            if (preview && /^image\//.test(file.type)) {
                var r = new FileReader();
                r.onload = function () { preview.innerHTML = '<img alt="Nieuw logo" src="' + r.result + '">'; };
                r.readAsDataURL(file);
            }
        }
        fileInput.addEventListener("change", function () { if (fileInput.files && fileInput.files[0]) showFile(fileInput.files[0]); });
        dropZone.addEventListener("dragover", function (e) { e.preventDefault(); dropZone.classList.add("is-dragover"); });
        dropZone.addEventListener("dragleave", function () { dropZone.classList.remove("is-dragover"); });
        dropZone.addEventListener("drop", function (e) {
            e.preventDefault(); dropZone.classList.remove("is-dragover");
            if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0]) { fileInput.files = e.dataTransfer.files; showFile(e.dataTransfer.files[0]); fire(fileInput, "change"); }
        });
    }

    // ── Bankrekeningen ────────────────────────────────────────────────────────────────────────────
    var bankModalEl = document.getElementById("ice-bank-modal");
    function todayIso() { var d = new Date(); return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }
    // "Rekening afgesloten" = Geldig tot op vandaag (of eerder); een afgesloten rekening kan niet de standaard zijn.
    var closedToggle = document.getElementById("ice-bank-closed");
    if (closedToggle) closedToggle.addEventListener("change", function () {
        var to = document.getElementById("ice-bank-to");
        if (closedToggle.checked) {
            if (!to.value || to.value > todayIso()) setDate("ice-bank-to", todayIso());
            $("#ice-bank-default").checked = false;
        } else if (to.value && to.value <= todayIso()) setDate("ice-bank-to", "");
    });
    var bankDefault = document.getElementById("ice-bank-default");
    if (bankDefault) bankDefault.addEventListener("change", function () {
        if (bankDefault.checked && closedToggle && closedToggle.checked) { closedToggle.checked = false; setDate("ice-bank-to", ""); }
    });
    function openBank(data) {
        var f = $("form", bankModalEl), isEdit = !!data;
        $("#ice-bank-title").textContent = isEdit ? "Bankrekening bewerken" : "Nieuwe bankrekening";
        f.action = isEdit ? bankModalEl.getAttribute("data-edit-url").replace(/\/0$/, "/" + data.id) : bankModalEl.getAttribute("data-create-url");
        f.elements.Id.value = isEdit ? data.id : 0;
        $("#ice-bank-name").value = isEdit ? data.name || "" : "";
        $("#ice-bank-iban").value = isEdit ? data.iban || "" : "";
        $("#ice-bank-bic").value = isEdit ? data.bic || "" : "";
        $("#ice-bank-default").checked = isEdit ? data.def === "1" : false;
        setDate("ice-bank-from", isEdit ? data.from : ""); setDate("ice-bank-to", isEdit ? data.to : "");
        $("#ice-bank-iban-err").hidden = true;
        $("#ice-bank-closed").checked = isEdit && !!data.to && data.to <= todayIso();
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        modalFor("ice-bank-modal").show();
    }
    document.addEventListener("click", function (e) {
        var n = e.target.closest(".js-ice-bank-new"); if (n) { openBank(null); return; }
        var ed = e.target.closest(".js-ice-bank-edit");
        if (ed) openBank({ id: ed.dataset.id, iban: ed.dataset.iban, bic: ed.dataset.bic, name: ed.dataset.name, def: ed.dataset.default, from: ed.dataset.from, to: ed.dataset.to });
    });
    if (bankModalEl) $("form", bankModalEl).addEventListener("submit", function (e) {
        var iban = $("#ice-bank-iban");
        if (!iban.value.trim()) { e.preventDefault(); $("#ice-bank-iban-err").hidden = false; iban.focus(); }
        else window.removeEventListener("beforeunload", warn);
    });
    var bankSearch = document.getElementById("ice-bank-search");
    var bankStatusInput = document.getElementById("ice-bank-status");
    var bankPanel = document.getElementById("ice-bank-filters-panel"), bankToggle = document.getElementById("ice-bank-filters-toggle");
    var bankStatusLabels = { actief: "Actief", standaard: "Standaard", afgesloten: "Afgesloten" };
    function applyBankFilters() {
        var q = bankSearch ? bankSearch.value.trim().toLowerCase().replace(/\s+/g, "") : "", st = bankStatusInput ? bankStatusInput.value : "", shown = 0;
        $$("#ice-bank-table tbody tr[data-search]").forEach(function (tr) {
            var s = tr.getAttribute("data-status");
            var okStatus = !st || s === st || (st === "actief" && s === "standaard");
            var ok = okStatus && (!q || tr.getAttribute("data-search").replace(/\s+/g, "").indexOf(q) >= 0);
            tr.hidden = !ok; if (ok) shown++;
        });
        var none = document.getElementById("ice-bank-none"); if (none) none.hidden = shown !== 0;
        var count = st ? 1 : 0, badge = document.getElementById("ice-bank-filters-count");
        if (badge) { badge.textContent = count; badge.hidden = !count; }
        if (bankToggle) bankToggle.classList.toggle("has-active", !!count);
        var chips = document.getElementById("ice-bank-chips"), clear = document.getElementById("ice-bank-filters-clear");
        if (chips) {
            chips.innerHTML = "";
            if (st) {
                var c = document.createElement("button"); c.type = "button"; c.className = "gl-v2-filters-chip";
                c.setAttribute("aria-label", "Filter status verwijderen");
                c.innerHTML = "<span></span><i class=\"ph ph-x\" aria-hidden=\"true\"></i>"; c.firstChild.textContent = "Status: " + bankStatusLabels[st];
                c.addEventListener("click", function () { setBankStatus(""); });
                chips.appendChild(c);
            }
        }
        if (clear) clear.hidden = !count;
    }
    function setBankStatus(v) {
        if (!bankStatusInput) return;
        var panel = document.getElementById("ice-bank-status_select");
        var opt = panel && panel.querySelector('.gl-v2-select-option[data-value="' + v + '"]');
        if (opt) opt.click(); else { bankStatusInput.value = v; applyBankFilters(); }
    }
    if (bankSearch) bankSearch.addEventListener("input", applyBankFilters);
    if (bankStatusInput) bankStatusInput.addEventListener("change", applyBankFilters);
    var bankClear = document.getElementById("ice-bank-filters-clear"); if (bankClear) bankClear.addEventListener("click", function () { setBankStatus(""); });
    if (bankToggle && bankPanel) bankToggle.addEventListener("click", function () {
        var open = bankPanel.hidden; bankPanel.hidden = !open;
        bankToggle.classList.toggle("is-open", open); bankToggle.setAttribute("aria-expanded", open ? "true" : "false");
    });

    // ── Nummerreeksen ─────────────────────────────────────────────────────────────────────────────
    var seriesModalEl = document.getElementById("ice-series-modal");
    function openSeries(data) {
        var f = $("form", seriesModalEl), isEdit = !!data;
        $("#ice-series-title").textContent = isEdit ? "Nummerreeks bewerken" : "Nieuwe nummerreeks";
        f.action = isEdit ? seriesModalEl.getAttribute("data-edit-url").replace(/\/0$/, "/" + data.id) : seriesModalEl.getAttribute("data-create-url");
        f.elements.Id.value = isEdit ? data.id : 0;
        $("#ice-series-code").value = isEdit ? data.code : ""; $("#ice-series-desc").value = isEdit ? data.description || "" : "";
        $("#ice-series-credit").checked = isEdit ? data.credit === "1" : false;
        $("#ice-series-active").checked = isEdit ? data.active === "1" : true;
        $("#ice-series-active-row").hidden = !isEdit;
        $("#ice-series-code-err").hidden = true;
        modalFor("ice-series-modal").show();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-ice-series-new")) { openSeries(null); return; }
        var ed = e.target.closest(".js-ice-series-edit");
        if (ed) { if (window.GlV2Menu) window.GlV2Menu.closeAll(); openSeries({ id: ed.dataset.id, code: ed.dataset.code, description: ed.dataset.description, credit: ed.dataset.credit, active: ed.dataset.active }); }
    });
    if (seriesModalEl) $("form", seriesModalEl).addEventListener("submit", function (e) {
        var code = $("#ice-series-code");
        if (!code.value.trim()) { e.preventDefault(); $("#ice-series-code-err").hidden = false; code.focus(); }
        else window.removeEventListener("beforeunload", warn);
    });

    // ── Verwijderen / direct posten (menu-acties) ─────────────────────────────────────────────────
    document.addEventListener("click", function (e) {
        var p = e.target.closest(".js-ice-post");
        if (p) { if (window.GlV2Menu) window.GlV2Menu.closeAll(); window.removeEventListener("beforeunload", warn); postTo(p.dataset.url); return; }
        var d = e.target.closest(".js-ice-delete");
        if (d) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            $("#ice-delete-title").textContent = d.dataset.title; $("#ice-delete-desc").textContent = d.dataset.desc;
            $("#ice-delete-confirm").textContent = d.dataset.confirm || "Verwijderen";
            $("#ice-delete-form").action = d.dataset.url;
            modalFor("ice-delete-modal").show();
        }
    });
    var delForm = document.getElementById("ice-delete-form");
    if (delForm) delForm.addEventListener("submit", function () { window.removeEventListener("beforeunload", warn); });

    // ── Betaaltermijnen & uurtarieven: lijst + modal → verborgen velden ───────────────────────────
    function money(n) { return "€ " + Number(n || 0).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function emptyLine(text) { return '<li class="is-empty">' + esc(text) + "</li>"; }

    var termsList = document.getElementById("ice-terms-list"), termsFields = document.getElementById("ice-terms-fields");
    var terms = [], termsInitial = "", termEditing = -1;
    if (termsList) {
        try { terms = JSON.parse(termsList.getAttribute("data-terms") || "[]"); } catch (x) { terms = []; }
        terms.forEach(function (t) { t.deleted = false; });
        termsInitial = JSON.stringify(terms);
        termsFields.setAttribute("data-ice-skip", "");
    }
    function termType(t) { return t.termType === 1 ? t.days + " d. na einde maand" : t.termType === 2 ? "tekst" : t.days + " dagen"; }
    function termOnInvoice(t) { return t.displayMode === 1 ? '"' + (t.displayText || "") + '"' : "vervaldatum"; }
    function hid(name, value) { return '<input type="hidden" name="' + name + '" value="' + esc(value) + '">'; }
    function renderTerms() {
        if (!termsList) return;
        var fields = "", html = "", idx = 0;
        terms.forEach(function (t, i) {
            if (t.deleted && !t.id) return;
            var p = "PaymentTerms[" + idx + "].";
            fields += hid(p + "Id", t.id || 0) + hid(p + "Name", t.name) + hid(p + "TermType", t.termType) + hid(p + "Days", t.days) + hid(p + "DisplayMode", t.displayMode) +
                hid(p + "DisplayText", t.displayText) + hid(p + "Description", t.description) + hid(p + "IsDeleted", t.deleted ? "true" : "false");
            idx++;
            if (!t.deleted) html += '<li><span class="gl-v2-ice-list-t"><b>' + esc(t.name || "—") + "</b><small>Op factuur: " + esc(termOnInvoice(t)) + '</small></span><span class="gl-v2-badge is-neutral">' + esc(termType(t).toUpperCase()) +
                '</span><button type="button" class="gl-v2-btn gl-v2-btn-secondary js-ice-term-edit" data-i="' + i + '">Bewerk</button></li>';
        });
        termsFields.innerHTML = fields;
        termsList.innerHTML = html || emptyLine("Nog geen betaaltermijnen toegevoegd.");
        markList("facturatie", "terms", JSON.stringify(terms) !== termsInitial);
    }
    function openTerm(i) {
        termEditing = i; var t = i >= 0 ? terms[i] : { name: "", termType: 0, days: 30, displayMode: 0, displayText: "", description: "" };
        $("#ice-term-title").textContent = i >= 0 ? "Betaaltermijn bewerken" : "Betaaltermijn toevoegen";
        $("#ice-term-name").value = t.name; $("#ice-term-days").value = t.days; $("#ice-term-text").value = t.displayText; $("#ice-term-desc").value = t.description;
        setSelect("ice-term-type", t.termType); setSelect("ice-term-mode", t.displayMode);
        $("#ice-term-name-err").hidden = true; $("#ice-term-remove").hidden = i < 0;
        modalFor("ice-term-modal").show();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-ice-term-new")) { openTerm(-1); return; }
        var ed = e.target.closest(".js-ice-term-edit"); if (ed) openTerm(+ed.dataset.i);
    });
    var termSave = document.getElementById("ice-term-save");
    if (termSave) {
        termSave.addEventListener("click", function () {
            var name = $("#ice-term-name").value.trim();
            if (!name) { $("#ice-term-name-err").hidden = false; $("#ice-term-name").focus(); return; }
            var t = termEditing >= 0 ? terms[termEditing] : { id: 0, deleted: false };
            t.name = name; t.termType = +$("#ice-term-type").value || 0; t.days = Math.max(0, +$("#ice-term-days").value || 0);
            t.displayMode = +$("#ice-term-mode").value || 0; t.displayText = $("#ice-term-text").value.trim(); t.description = $("#ice-term-desc").value.trim();
            if (termEditing < 0) terms.push(t);
            modalFor("ice-term-modal").hide(); renderTerms();
        });
        $("#ice-term-remove").addEventListener("click", function () { if (termEditing >= 0) { terms[termEditing].deleted = true; modalFor("ice-term-modal").hide(); renderTerms(); } });
        renderTerms();
    }

    var ratesList = document.getElementById("ice-rates-list"), ratesFields = document.getElementById("ice-rates-fields");
    var rates = [], ratesInitial = "", rateEditing = -1;
    if (ratesList) {
        try { rates = JSON.parse(ratesList.getAttribute("data-rates") || "[]"); } catch (x) { rates = []; }
        ratesInitial = JSON.stringify(rates);
        ratesFields.setAttribute("data-ice-skip", "");
    }
    function userName(id) { var o = $('#ice-rate-user_select .gl-v2-select-option[data-value="' + String(id).replace(/"/g, "") + '"]'); return o ? o.textContent.trim() : ""; }
    function renderRates() {
        if (!ratesList) return;
        var fields = "", html = "";
        rates.forEach(function (r, i) {
            var p = "UserRates[" + i + "].";
            fields += hid(p + "UserId", r.userId) + hid(p + "UserFullName", r.name) + hid(p + "HourlyRate", String(r.rate));
            html += '<li><span class="gl-v2-ice-list-t"><b>' + esc(r.name || r.userId) + "</b></span><span class=\"gl-v2-ice-rate\">" + esc(money(r.rate)) + ' <small>per uur</small></span><button type="button" class="gl-v2-btn gl-v2-btn-secondary js-ice-rate-edit" data-i="' + i + '">Bewerk</button></li>';
        });
        ratesFields.innerHTML = fields;
        ratesList.innerHTML = html || emptyLine("Nog geen tarieven. Zonder tarief kan een medewerker niet in regie gefactureerd worden.");
        markList("uurtarieven", "rates", JSON.stringify(rates) !== ratesInitial);
    }
    function openRate(i) {
        rateEditing = i; var r = i >= 0 ? rates[i] : { userId: "", rate: "" };
        $("#ice-rate-title").textContent = i >= 0 ? "Uurtarief bewerken" : "Tarief toevoegen";
        setSelect("ice-rate-user", r.userId); if (!r.userId) { var h = document.getElementById("ice-rate-user"); if (h) h.value = ""; var lab = $("#ice-rate-user_select .gl-v2-select-trigger-label"); if (lab) lab.textContent = "Kies een medewerker"; var tr = $("#ice-rate-user_select .gl-v2-select-trigger"); if (tr) tr.classList.remove("is-filled"); }
        $("#ice-rate-value").value = r.rate === "" ? "" : r.rate; $("#ice-rate-user-err").hidden = true; $("#ice-rate-remove").hidden = i < 0;
        modalFor("ice-rate-modal").show();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-ice-rate-new")) { openRate(-1); return; }
        var ed = e.target.closest(".js-ice-rate-edit"); if (ed) openRate(+ed.dataset.i);
    });
    var rateSave = document.getElementById("ice-rate-save");
    if (rateSave) {
        rateSave.addEventListener("click", function () {
            var uid = (document.getElementById("ice-rate-user") || {}).value;
            if (!uid) { $("#ice-rate-user-err").hidden = false; return; }
            if (rates.some(function (r, i) { return r.userId === uid && i !== rateEditing; })) { $("#ice-rate-user-err").textContent = "Deze medewerker heeft al een tarief."; $("#ice-rate-user-err").hidden = false; return; }
            var rate = Math.max(0, parseFloat($("#ice-rate-value").value) || 0);
            var r = rateEditing >= 0 ? rates[rateEditing] : {};
            r.userId = uid; r.name = userName(uid); r.rate = rate;
            if (rateEditing < 0) rates.push(r);
            modalFor("ice-rate-modal").hide(); renderRates();
        });
        $("#ice-rate-remove").addEventListener("click", function () { if (rateEditing >= 0) { rates.splice(rateEditing, 1); modalFor("ice-rate-modal").hide(); renderRates(); } });
        renderRates();
    }

    // ── Peppol + EPC-QR ───────────────────────────────────────────────────────────────────────────
    var einv = form.querySelector('input[type="checkbox"][name="EInvoiceEnabled"]'), peppolId = form.elements.PeppolParticipantId, entNr = form.elements.EnterpriseNumber;
    function peppolUi() {
        if (!einv) return; // gekoppeld aan Octopus: geen Peppol-blok
        var on = einv && einv.checked, b = document.getElementById("ice-peppol-badge");
        if (b) { b.textContent = on ? "AAN" : "UIT"; b.classList.toggle("is-positive", !!on); b.classList.toggle("is-neutral", !on); }
        var tb = document.getElementById("ice-badge-facturatie"); if (tb) tb.hidden = !!on;
    }
    if (einv) einv.addEventListener("change", function () {
        peppolUi();
        if (einv.checked && peppolId && !peppolId.value.trim() && entNr) {
            var digits = (entNr.value || "").replace(/\D/g, "");
            if (digits.length >= 9) { peppolId.value = "0208:" + (digits.length === 9 ? "0" + digits : digits); fire(peppolId, "input"); }
        }
    });
    peppolUi();

    var qrBox = document.getElementById("ice-qr-box"), qrWrap = document.getElementById("ice-qr-preview");
    var epcOn = form.querySelector('input[type="checkbox"][name="EpcQrEnabled"]');
    function epcPayload() {
        var iban = ((form.elements.EpcIban || {}).value || (qrWrap && qrWrap.getAttribute("data-default-iban")) || "").replace(/\s+/g, "").toUpperCase();
        if (!iban) return null;
        var name = ((form.elements.EpcBeneficiaryName || {}).value || (form.elements.LegalName || {}).value || (form.elements.Name || {}).value || "").trim().slice(0, 70);
        var bic = ((form.elements.EpcBic || {}).value || "").trim();
        var type = (form.elements.EpcRemittanceType || {}).value || "CHAR";
        var tpl = ((form.elements.EpcRemittanceTemplate || {}).value || "Factuur {PublicId}").replace(/\{\{?\s*(Invoice\.)?PublicId\s*\}?\}/g, "2026-0001");
        return ["BCD", "002", "1", "SCT", bic, name, iban, "EUR123.45", "", type === "SCOR" ? tpl : "", type === "SCOR" ? "" : tpl, ""].join("\n");
    }
    var qrObj = null;
    function renderQr() {
        if (!qrBox || !qrWrap) return;
        var payload = epcOn && epcOn.checked && window.QRCode ? epcPayload() : null;
        qrWrap.hidden = !payload; if (!payload) return;
        qrBox.innerHTML = ""; qrObj = new window.QRCode(qrBox, { text: payload, width: 132, height: 132, correctLevel: window.QRCode.CorrectLevel.M });
    }
    ["EpcIban", "EpcBic", "EpcBeneficiaryName", "EpcRemittanceType", "EpcRemittanceTemplate", "LegalName", "Name"].forEach(function (n) {
        $$('[name="' + n + '"]', form).forEach(function (el) { el.addEventListener("input", renderQr); el.addEventListener("change", renderQr); });
    });
    if (epcOn) epcOn.addEventListener("change", renderQr);
    onTab.facturatie = renderQr;
    if (currentTab === "facturatie") renderQr();

    // ── Boekhouding ───────────────────────────────────────────────────────────────────────────────
    var more = document.getElementById("ice-bookyears-more"), old = document.getElementById("ice-bookyears-old");
    if (more && old) more.addEventListener("click", function () { var open = old.hidden; old.hidden = !open; more.setAttribute("aria-expanded", open ? "true" : "false"); });
    var dossierHidden = document.getElementById("ice-octopus-dossier"), connectBtn = document.getElementById("ice-octopus-connect"), currentDossier = (form.elements.OctopusDossierNumber || {}).value || "";
    if (dossierHidden && connectBtn) dossierHidden.addEventListener("change", function () {
        if (dossierHidden.value && dossierHidden.value !== currentDossier) { window.removeEventListener("beforeunload", warn); connectBtn.click(); }
    });

    // ── Lay-out & e-mail ──────────────────────────────────────────────────────────────────────────
    $$("[data-ice-color-for]").forEach(function (sw) {
        var txt = document.getElementById(sw.getAttribute("data-ice-color-for"));
        sw.addEventListener("input", function () { txt.value = sw.value; fire(txt, "input"); });
        txt.addEventListener("input", function () { if (/^#([0-9a-f]{6})$/i.test(txt.value)) sw.value = txt.value; });
    });
    var advToggle = document.getElementById("ice-adv-toggle"), adv = document.getElementById("ice-adv");
    function openAdv(open) { adv.hidden = !open; advToggle.setAttribute("aria-expanded", open ? "true" : "false"); }
    if (advToggle) advToggle.addEventListener("click", function () { openAdv(adv.hidden); });

    var defaults = {}, schemaObj = null, validateFn = null;
    try { defaults = JSON.parse((document.getElementById("invoice-layout-defaults") || { textContent: "{}" }).textContent || "{}"); } catch (x) { }
    try { var sEl = document.getElementById("invoice-layout-schema"); if (sEl) schemaObj = JSON.parse(sEl.textContent || "{}"); } catch (x) { }
    if (schemaObj && typeof window.Ajv === "function") { try { validateFn = new window.Ajv({ allErrors: true }).compile(schemaObj); } catch (x) { validateFn = null; } }
    var jsonArea = document.getElementById("TemplateJson"), jsonErr = document.getElementById("ice-json-error"), tplKey = document.getElementById("ice-template-key");
    function validateJson() {
        if (!jsonArea) return true;
        var v = jsonArea.value.trim(), msg = null;
        if (v) {
            try { var parsed = JSON.parse(v); if (validateFn && !validateFn(parsed)) msg = "Schema-validatie mislukt: " + (validateFn.errors || []).map(function (e) { return e.message || "ongeldige waarde"; }).join(", "); }
            catch (err) { msg = "JSON is ongeldig: " + err.message; }
        }
        if (jsonErr) { jsonErr.hidden = !msg; $("span", jsonErr).textContent = msg || ""; }
        return !msg;
    }
    if (jsonArea) { jsonArea.addEventListener("input", validateJson); validateJson(); }
    var lastKey = tplKey ? tplKey.value || "layoutA" : "layoutA";
    if (tplKey) tplKey.addEventListener("change", function () {
        var next = tplKey.value || "layoutA", cur = defaults[lastKey], nxt = defaults[next], val = jsonArea ? jsonArea.value.trim() : "";
        if (jsonArea && nxt && (!val || (cur && val === cur.trim()))) { jsonArea.value = nxt; fire(jsonArea, "input"); }
        lastKey = next;
    });
    var resetBtn = document.getElementById("ice-layout-reset");
    if (resetBtn) resetBtn.addEventListener("click", function () {
        var k = (tplKey && tplKey.value) || "layoutA";
        if (jsonArea && defaults[k]) { jsonArea.value = defaults[k]; fire(jsonArea, "input"); }
    });

    // e-mail: tabs Open factuur / Reeds betaald
    $$("[data-ice-mail]").forEach(function (b) {
        b.addEventListener("click", function () {
            var key = b.getAttribute("data-ice-mail");
            $$("[data-ice-mail]").forEach(function (o) { var on = o === b; o.classList.toggle("is-active", on); o.setAttribute("aria-selected", on ? "true" : "false"); });
            $$("[data-ice-mail-panel]").forEach(function (p) { p.hidden = p.getAttribute("data-ice-mail-panel") !== key; });
        });
    });

    // e-mail: editor met veld-chips ({{expr}} ↔ chip)
    var layoutRoot = document.getElementById("ice-layout-root"), tokenCatalog = [];
    try { tokenCatalog = JSON.parse(layoutRoot ? layoutRoot.getAttribute("data-tokens") || "[]" : "[]"); } catch (x) { }
    function labelFor(expr) { var m = tokenCatalog.filter(function (t) { return t.expr === expr; })[0]; return m ? m.label : expr; }
    function chipHtml(expr, label) { return '<span class="gl-v2-ice-chip" contenteditable="false" data-expr="' + esc(expr) + '">' + esc(label || labelFor(expr)) + "</span>"; }
    function toChips(html) { return html.replace(/\{\{\s*([^}]+?)\s*\}\}/g, function (m, expr) { return chipHtml(expr.trim()); }); }
    function serialize(ed) {
        var clone = ed.cloneNode(true);
        $$(".gl-v2-ice-chip", clone).forEach(function (c) { c.replaceWith(document.createTextNode("{{" + c.getAttribute("data-expr") + "}}")); });
        if (ed.getAttribute("data-rte") === "line") return clone.textContent.replace(/\s+/g, " ").trim();
        var html = clone.innerHTML.replace(/&nbsp;/g, " ").trim();
        return clone.textContent.trim() === "" && !/<(a|ul|ol|li|img)/i.test(html) ? "" : html;
    }
    var editors = $$("[data-rte]"), lastEditor = {};
    editors.forEach(function (ed) {
        var hidden = form.elements[ed.getAttribute("data-target")], raw = hidden ? hidden.value : "";
        ed.innerHTML = ed.getAttribute("data-rte") === "line" ? toChips(esc(raw)) : toChips(raw);
        ed.addEventListener("input", function () { if (hidden) { hidden.value = serialize(ed); fire(hidden, "input"); } });
        ed.addEventListener("focus", function () { lastEditor[ed.closest("[data-ice-mail-panel]") ? ed.closest("[data-ice-mail-panel]").getAttribute("data-ice-mail-panel") : "open"] = ed; lastEditor.any = ed; });
        if (ed.getAttribute("data-rte") === "line") ed.addEventListener("keydown", function (e) { if (e.key === "Enter") e.preventDefault(); });
        ed.addEventListener("paste", function (e) { e.preventDefault(); var t = (e.clipboardData || window.clipboardData).getData("text/plain"); document.execCommand("insertText", false, ed.getAttribute("data-rte") === "line" ? t.replace(/\s+/g, " ") : t); });
    });
    $$(".gl-v2-ice-rte-bar [data-cmd]").forEach(function (b) {
        b.addEventListener("mousedown", function (e) { e.preventDefault(); });
        b.addEventListener("click", function () {
            var wrap = b.closest(".gl-v2-ice-rte-wrap"), ed = $("[data-rte]", wrap); ed.focus();
            var cmd = b.getAttribute("data-cmd");
            if (cmd === "link") { var url = window.prompt("Link (https://…)", "https://"); if (url && /^https?:\/\//i.test(url)) document.execCommand("createLink", false, url); }
            else document.execCommand(cmd, false, null);
            fire(ed, "input");
        });
    });
    $$(".gl-v2-ice-rte-insert").forEach(function (b) { b.addEventListener("mousedown", function (e) { e.preventDefault(); }); });
    document.addEventListener("click", function (e) {
        var t = e.target.closest(".js-ice-token"); if (!t) return;
        var activeTab = $("[data-ice-mail].is-active"), panel = activeTab ? activeTab.getAttribute("data-ice-mail") : "open";
        var subject = $('[data-target="EmailSubjectTemplate"]');
        // Chip in het onderwerp als dat het laatst focus had, anders in het bericht van het zichtbare tabblad.
        var ed = lastEditor.any === subject ? subject : $('[data-ice-mail-panel="' + panel + '"] [data-rte]');
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        ed.focus();
        document.execCommand("insertHTML", false, chipHtml(t.dataset.expr, t.dataset.label) + "&#8203;");
        fire(ed, "input");
    });

    // live PDF-voorbeeld
    var frame = document.getElementById("ice-preview-frame"), pState = document.getElementById("ice-preview-state"), openBtn = document.getElementById("ice-preview-open");
    var previewTimer = null, blobUrl = null, previewing = false, previewDirty = true;
    function previewMsg(icon, title, text) { if (!pState) return; pState.hidden = false; if (frame) frame.hidden = true; pState.innerHTML = '<i class="ph ' + icon + '" aria-hidden="true"></i><b>' + esc(title) + "</b>" + (text ? "<span>" + esc(text) + "</span>" : ""); }
    function refreshPreview() {
        if (!frame || !layoutRoot) return;
        if (previewing) { previewDirty = true; return; }
        if (!validateJson()) { previewMsg("ph-warning", "Voorbeeld niet beschikbaar", "Los eerst de fout in de sjabloon-JSON op."); return; }
        previewing = true; previewDirty = false;
        var fd = new FormData();
        fd.append("__RequestVerificationToken", token()); fd.append("IssuerCompanyId", layoutRoot.getAttribute("data-issuer-id")); fd.append("InvoiceId", layoutRoot.getAttribute("data-invoice-id"));
        ["TemplateKey", "TemplateJson", "BrandPrimaryColor", "BrandSecondaryColor", "FontFamily", "FooterLegalText"].forEach(function (n) { if (form.elements[n]) fd.append(n, form.elements[n].value || ""); });
        if (epcOn) fd.append("EpcQrEnabled", epcOn.checked ? "true" : "false");
        fetch(layoutRoot.getAttribute("data-preview-url"), { method: "POST", body: fd, credentials: "same-origin" })
            .then(function (r) {
                if (!r.ok) return r.text().then(function (t) { var m = "Het voorbeeld kon niet gemaakt worden."; try { var j = JSON.parse(t); if (j && j.message) m = j.message; } catch (x) { } throw new Error(m); });
                return r.blob();
            })
            .then(function (blob) {
                if (blobUrl) URL.revokeObjectURL(blobUrl);
                blobUrl = URL.createObjectURL(blob); frame.src = blobUrl + "#toolbar=0&navpanes=0&view=FitH"; frame.hidden = false; if (pState) pState.hidden = true; if (openBtn) openBtn.disabled = false;
            })
            .catch(function (err) { previewMsg("ph-warning", "Voorbeeld niet beschikbaar", err.message); })
            .then(function () { previewing = false; if (previewDirty) schedulePreview(); });
    }
    function schedulePreview() { window.clearTimeout(previewTimer); previewTimer = window.setTimeout(refreshPreview, 900); }
    if (frame && layoutRoot) {
        var wasShown = false;
        onTab.layout = function () { if (!wasShown) { wasShown = true; refreshPreview(); } };
        ["TemplateKey", "TemplateJson", "BrandPrimaryColor", "BrandSecondaryColor", "FontFamily", "FooterLegalText", "EpcQrEnabled"].forEach(function (n) {
            // form.elements[n] is een lijst bij een checkbox met verborgen "false"-veld (EpcQrEnabled) — dus per naam alle velden pakken.
            $$('[name="' + n + '"]', form).forEach(function (el) {
                el.addEventListener("input", function () { if (wasShown) schedulePreview(); }); el.addEventListener("change", function () { if (wasShown) schedulePreview(); });
            });
        });
        if (openBtn) openBtn.addEventListener("click", function () { if (blobUrl) window.open(blobUrl, "_blank", "noopener"); });
        if (currentTab === "layout") onTab.layout();
    }

    // ── Relaties-modal, opslaan ───────────────────────────────────────────────────────────────────
    var rel = document.getElementById("ice-relations-modal"); if (rel && rel.getAttribute("data-show-on-load")) modalFor("ice-relations-modal").show();

    form.addEventListener("submit", function (e) {
        // formaction-knoppen (Octopus) slaan de klassieke route over: geen validatie, wel de tab-waarschuwing uit
        var sub = e.submitter;
        if (sub && sub.getAttribute("formaction")) { window.removeEventListener("beforeunload", warn); return; }
        var name = form.elements.Name;
        if (name && !name.value.trim()) { e.preventDefault(); showTab("algemeen"); name.focus(); return; }
        if (!validateJson()) { e.preventDefault(); showTab("layout"); openAdv(true); return; }
        editors.forEach(function (ed) { var h = form.elements[ed.getAttribute("data-target")]; if (h) h.value = serialize(ed); });
        window.removeEventListener("beforeunload", warn);
    });

    // ── Start ─────────────────────────────────────────────────────────────────────────────────────
    snapshot();
    showTab(currentTab, false);
})();
