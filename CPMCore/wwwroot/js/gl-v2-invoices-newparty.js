// gl-v2 — Nieuwe klant snel aanmaken vanuit de factuur (design-handoff punt 47e–g). Gedrag van de modal in Views/Invoices/Partials/_NewPartyModalV2.cshtml
// (gedeelde onderdelen: GlV2/_Segmented, GlV2/_GlV2Combo, GlV2/_SelectList). Onderneming (47e): ondernemingsnummer eerst, Controleren vult naam en adres in
// en toont de Peppol-status; particulier (47f): naam en e-mail; nummer niet gevonden (47g): zelf invullen blijft mogelijk.
// Opslaan gebeurt via de BESTAANDE actie Klanten/QuickCreate (zelfde velden als de oude modal); de controle via de bestaande Klanten/ValidateVat
// en Klanten/FindPostalMatch.
//
//   GlV2NewParty.open({ issuerId, issuerName, urls: { quickCreate, validateVat, findPostal, countries, peppolCheck }, token, term, onCreated(party), onSixPercent() })
(function () {
    "use strict";
    var modalEl = null, modal = null, opts = null, countries = [], st = null, bound = false;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function $(s) { return modalEl.querySelector(s); }
    function $$(s) { return Array.prototype.slice.call(modalEl.querySelectorAll(s)); }
    function getJson(url) { return fetch(url, { credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); }); }
    function val(id) { var el = $("#" + id); return el ? el.value.trim() : ""; }

    // postcode-combo (GlV2Combo): id = het deel vóór "-id", bv. np-pc
    function pcRoot(id) { var el = document.getElementById(id + "-id"); return el ? el.closest("[data-gl-v2-combo]") : null; }
    function pcSet(id, value, text) { var r = pcRoot(id); if (r && window.GlV2Combo) window.GlV2Combo.setValue(r, value == null ? "" : String(value), text || ""); }

    function segReset(id, v) { $$("#" + id + " button").forEach(function (b) { var on = b.getAttribute("data-v") === v; b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); }); }
    function segHandler(id, fn) {
        $$("#" + id + " button").forEach(function (b) { b.addEventListener("click", function () { segReset(id, b.getAttribute("data-v")); fn(b.getAttribute("data-v")); }); });
    }

    function bind() {
        modalEl = document.getElementById("ic-newparty-modal");
        modal = window.bootstrap ? new window.bootstrap.Modal(modalEl, { backdrop: "static" }) : null;
        segHandler("np-kind", function (v) { st.kind = v; refresh(); });
        segHandler("np-type", function (v) { st.type = v; refresh(); });
        segHandler("np-sal", function (v) { st.sal = v; });
        segHandler("np-inv-mode", function (v) { st.invMode = v; $("#np-inv-box").hidden = v !== "other"; });
        $("#np-useinv").addEventListener("change", function () { $("#np-invaddr").hidden = !this.checked; });
        $("#np-check").addEventListener("click", check);
        $("#np-number").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); check(); } });
        $("#np-save").addEventListener("click", save);
        modalEl.addEventListener("keydown", function (e) { if (e.key === "Enter" && e.target.tagName === "INPUT" && e.target.type !== "checkbox") e.preventDefault(); });
        // een ander land: de gekozen postcodes horen daar niet meer bij
        $("#np-country").addEventListener("change", function () { pcSet("np-pc", "", ""); pcSet("np-ipc", "", ""); });
        bound = true;
    }

    function refresh() {
        var company = st.type === "company";
        $("#np-company").hidden = !company; $("#np-person").hidden = company;
        $("#np-title").textContent = st.kind === "supplier" ? "Nieuwe leverancier" : st.kind === "both" ? "Nieuwe klant en leverancier" : "Nieuwe klant";
        // een leverancier heeft geen facturatieadres; het 6 %-voorstel is er enkel voor een particuliere klant
        $("#np-useinv").closest("label").hidden = st.kind === "supplier";
        if (st.kind === "supplier") { $("#np-useinv").checked = false; $("#np-invaddr").hidden = true; }
        $("#np-six-row").hidden = company || st.kind === "supplier";
        if (company) $("#np-six").checked = false;
    }

    function showError(msg) { var e = $("#np-error"); e.textContent = msg || ""; e.hidden = !msg; if (msg) e.scrollIntoView({ block: "nearest" }); }
    function result(kind, title, text) {
        var r = $("#np-result"); r.className = "gl-v2-ic-np-result is-" + kind; r.hidden = false;
        r.innerHTML = '<i class="ph ' + (kind === "ok" ? "ph-check-circle" : kind === "bad" ? "ph-warning" : "ph-circle-notch") + '" aria-hidden="true"></i><span><b>' + esc(title) + '</b>' + (text ? ' ' + esc(text) : "") + '<span id="np-peppol"></span></span>';
    }

    // ondernemingsnummer controleren: vult naam en adres in (bestaande ValidateVat); daarna Peppol
    function check() {
        showError("");
        var number = val("np-number");
        if (!number) { showError("Vul eerst het ondernemingsnummer in."); return; }
        var btn = $("#np-check"); btn.disabled = true; var label = btn.textContent; btn.textContent = "Controleren…";
        result("busy", "Controleren…", "");
        getJson(opts.urls.validateVat + "?vatCountryCode=BE&vatNumber=" + encodeURIComponent(number)).then(function (d) {
            if (!d || d.valid === false || !d.name) { result("bad", "Niet gevonden", "Controleer het nummer (cijfers omgewisseld?) of vul de gegevens hieronder zelf in. Je kan de klant toch aanmaken."); st.found = false; return; }
            st.found = true;
            $("#np-company-name").value = window.GlV2Bedrijfsnaam ? window.GlV2Bedrijfsnaam.schoon(d.name) : d.name;
            var a = d.address || {};
            if (a.street) $("#np-street").value = a.street;
            if (a.number) $("#np-house").value = a.number;
            result("ok", "Gevonden", "Naam en adres zijn ingevuld. Je kan alles nog aanpassen.");
            if (a.zip && a.city) {
                getJson(opts.urls.findPostal + "?postalCode=" + encodeURIComponent(a.zip) + "&city=" + encodeURIComponent(a.city) + "&countryIso=BE").then(function (m) { if (m && m.id) pcSet("np-pc", m.id, m.text); }).catch(function () { });
            }
            getJson(opts.urls.peppolCheck + "?number=" + encodeURIComponent(number)).then(function (p) {
                var el = $("#np-peppol"); if (!el) return;
                el.innerHTML = p && p.found ? '<em class="is-on"><i class="ph ph-check-circle" aria-hidden="true"></i> Bereikbaar via Peppol</em>' : '<em><i class="ph ph-minus-circle" aria-hidden="true"></i> Niet gevonden op Peppol: de factuur gaat per e-mail</em>';
            }).catch(function () { });
        }).catch(function () { result("bad", "De controle is mislukt", "Probeer het later opnieuw of vul de gegevens zelf in."); st.found = false; })
            .then(function () { btn.disabled = false; btn.textContent = label; });
    }

    function post(extra) {
        var company = st.type === "company", fd = new FormData();
        fd.append("__RequestVerificationToken", opts.token);
        fd.append("IssuerCompanyId", opts.issuerId);
        fd.append("IsCompany", company); fd.append("IsSupplier", extra.supplier);
        if (company) { fd.append("CompanyName", val("np-company-name")); fd.append("EnterpriseNumber", val("np-number")); fd.append("EnterpriseNumberCountryCode", "BE"); }
        else { fd.append("Salutation", st.sal || ""); fd.append("Forename", val("np-forename")); fd.append("Name", val("np-name")); }
        var email = company ? val("np-email") : val("np-email-p");
        fd.append("Email", email);
        fd.append("InvoiceEmail", company && st.invMode === "other" ? val("np-inv-email") : "");
        fd.append("RequiresDigitalInvoice", "false");
        fd.append("Street", val("np-street")); fd.append("HouseNumber", val("np-house")); fd.append("BusNumber", val("np-bus"));
        fd.append("PostalCodeId", val("np-pc-id") || "0");
        var useInv = !extra.supplier && $("#np-useinv").checked;
        fd.append("UseInvoiceAddress", useInv);
        if (useInv) { fd.append("InvoiceStreet", val("np-istreet")); fd.append("InvoiceHouseNumber", val("np-ihouse")); fd.append("InvoiceBusNumber", val("np-ibus")); fd.append("InvoicePostalCodeId", val("np-ipc-id") || "0"); }
        return fetch(opts.urls.quickCreate, { method: "POST", body: fd, credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } }).then(function (r) {
            if ((r.headers.get("content-type") || "").indexOf("json") < 0) throw new Error("De server gaf geen geldig antwoord (" + r.status + "). Je hebt mogelijk geen recht om klanten aan te maken.");
            return r.json();
        }).then(function (d) { if (!d.success) throw new Error(d.error || "Er is een fout opgetreden."); return d; });
    }
    function save() {
        showError("");
        var company = st.type === "company";
        if (company && !val("np-company-name")) { showError("Vul de bedrijfsnaam in."); return; }
        if (!company && !val("np-name") && !val("np-forename")) { showError("Vul de naam in."); return; }
        var email = company ? val("np-email") : val("np-email-p");
        if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { showError("Dit e-mailadres lijkt niet te kloppen."); return; }
        var btn = $("#np-save"); btn.disabled = true; var label = btn.textContent; btn.textContent = "Aanmaken…";
        var chain = Promise.resolve(null), created = null;
        // Beide = eerst de klant, daarna dezelfde gegevens als leverancier (twee aparte records, zoals in de rest van de app)
        if (st.kind !== "supplier") chain = chain.then(function () { return post({ supplier: false }); }).then(function (d) { created = d; });
        if (st.kind !== "client") chain = chain.then(function () { return post({ supplier: true }); }).then(function (d) { if (!created) created = d; });
        chain.then(function () {
            if (modal) modal.hide();
            if (!$("#np-six-row").hidden && $("#np-six").checked && opts.onSixPercent) opts.onSixPercent();
            opts.onCreated({ id: created.id, text: created.display || created.text || created.name });
        }).catch(function (e) {
            showError((created ? "De klant is aangemaakt, maar de leverancier niet: " : "") + e.message);
            if (created) { if (modal) modal.hide(); opts.onCreated({ id: created.id, text: created.display || created.text }); }
        }).then(function () { btn.disabled = false; btn.textContent = label; });
    }

    // landenlijst in de gedeelde keuzelijst (GlV2Select.setItems); België is de standaard
    function fillCountries() {
        var wrap = document.getElementById("np-country_select"); if (!wrap || !window.GlV2Select || !countries.length) return;
        var be = countries.filter(function (c) { return c.iso === "BE"; })[0] || countries[0];
        window.GlV2Select.setItems(wrap, countries.map(function (c) { return { value: String(c.id), text: c.name }; }), String(be.id));
        $("#np-country").value = String(be.id);
    }

    function open(o) {
        opts = o;
        if (!bound) { st = {}; bind(); }
        st = { kind: "client", type: "company", sal: "", invMode: "same", found: null };
        ["np-number", "np-company-name", "np-email", "np-inv-email", "np-forename", "np-name", "np-email-p", "np-street", "np-house", "np-bus", "np-istreet", "np-ihouse", "np-ibus"].forEach(function (id) { $("#" + id).value = ""; });
        pcSet("np-pc", "", ""); pcSet("np-ipc", "", "");
        $("#np-useinv").checked = false; $("#np-six").checked = false; $("#np-invaddr").hidden = true; $("#np-inv-box").hidden = true; $("#np-result").hidden = true; showError("");
        segReset("np-kind", "client"); segReset("np-type", "company"); segReset("np-sal", ""); segReset("np-inv-mode", "same");
        $("#np-sub").textContent = "Wordt aangemaakt voor " + (o.issuerName || "dit bedrijf") + " en meteen op deze factuur gezet.";
        // een ingetypte zoekterm zonder resultaat: als naam (of als ondernemingsnummer) meenemen
        if (o.term) { if (/^[\d.\s]{9,}$/.test(o.term)) $("#np-number").value = o.term; else { $("#np-company-name").value = o.term; $("#np-name").value = o.term; } }
        refresh();
        var done = countries.length ? Promise.resolve() : getJson(o.urls.countries).then(function (r) { countries = r.countries || []; });
        done.catch(function () { }).then(fillCountries);
        if (modal) modal.show();
        setTimeout(function () { var f = $("#np-number"); if (f && !$("#np-company").hidden) f.focus(); }, 300);
    }

    window.GlV2NewParty = { open: open };
})();
