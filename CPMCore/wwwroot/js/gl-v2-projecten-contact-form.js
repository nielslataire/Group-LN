// gl-v2 — Contact toevoegen of bewerken (design-handoff 44c): bron kiezen (de velden passen zich aan), eenheden als knoppen, dubbel-check op e-mail en telefoon,
// toestemming. Opslaan post via fetch naar ProjectenController.ContactSaveV2.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-cf-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function val(i) { var e = document.getElementById(i); return e ? e.value : ""; }
    var id = root.dataset.id || "", source = (($(".gl-v2-cf-source.is-on", root) || {}).dataset || {}).source || "Telefoon";
    function token() { var i = document.querySelector("input[name=__RequestVerificationToken]"); return i ? i.value : ""; }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-cd-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 4200);
    }

    // ── bron: toont of verbergt Makelaar en Gezien via ──
    $("#cf-sources").addEventListener("click", function (e) {
        var b = e.target.closest("[data-source]"); if (!b) return;
        source = b.dataset.source;
        $$(".gl-v2-cf-source", root).forEach(function (x) { x.classList.toggle("is-on", x === b); });
        $("#cf-broker-field").hidden = source !== "Makelaar";
        $("#cf-seen-field").hidden = source !== "Werfbord";
    });
    $("#cf-units").addEventListener("click", function (e) { var b = e.target.closest("[data-unit]"); if (b) b.classList.toggle("is-on"); });

    // ── dubbel-check (rechts) ──
    var timer = null, seq = 0;
    function check() {
        var email = val("Email").trim(), phone = val("Phone").trim(), my = ++seq;
        if (!email && phone.replace(/\D/g, "").length < 6) { $("#cf-dup").hidden = true; return; }
        var u = root.dataset.dupUrl + (root.dataset.dupUrl.indexOf("?") >= 0 ? "&" : "?") + "email=" + encodeURIComponent(email) + "&phone=" + encodeURIComponent(phone) + (id ? "&exclude=" + id : "");
        fetch(u, { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (list) {
            if (my !== seq) return;
            var box = $("#cf-dup");
            if (!list || !list.length) { box.hidden = true; return; }
            var c = list[0];
            $("#cf-dup-text").textContent = "Er is al een contact met dit " + c.match + ": " + c.name + " · via " + c.source + " · status " + c.status.toLowerCase() + (list.length > 1 ? " (en " + (list.length - 1) + " andere)" : "") + ".";
            $("#cf-dup-link").href = c.url; box.hidden = false;
        }).catch(function () { /* geen melding */ });
    }
    ["Email", "Phone_number"].forEach(function (i) { var e = document.getElementById(i); if (e) e.addEventListener("input", function () { clearTimeout(timer); timer = setTimeout(check, 450); }); });
    var phoneSel = $('[data-role="phone-prefix"]'); if (phoneSel) phoneSel.addEventListener("change", function () { clearTimeout(timer); timer = setTimeout(check, 450); });
    check();

    // ── opslaan ──
    function err(msg, field) {
        var e = $("#cf-err"), q = $("#cf-req"); e.textContent = msg || ""; e.hidden = !msg; q.hidden = !!msg;
        ["cf-lastname", "Phone_number", "Email"].forEach(function (i) { var f = document.getElementById(i); if (f) f.closest(".gl-v2-field").classList.remove("is-error"); });
        if (msg && field) { var f = document.getElementById(field); if (f) { f.closest(".gl-v2-field").classList.add("is-error"); f.focus(); } }
    }
    function reset() {
        ["cf-lastname", "cf-firstname", "cf-partner", "Phone_number", "Phone", "Email", "cf-street", "cf-budget", "cf-financing", "cf-own", "cf-wish", "cf-note", "cf-next-text", "cf-seen"].forEach(function (i) { var e = document.getElementById(i); if (e) e.value = ""; });
        ["cf-broker", "cf-postcode"].forEach(function (i) { var h = document.getElementById(i), r = h && h.closest("[data-gl-v2-combo]"); if (window.GlV2Combo && r) window.GlV2Combo.setValue(r, "", ""); else if (h) h.value = ""; });
        $$("#cf-units .is-on").forEach(function (b) { b.classList.remove("is-on"); });
        var d = document.getElementById("cf-next-date"), dt = document.getElementById("cf-next-date_text"); if (d) d.value = ""; if (dt) dt.value = "";
        $("#cf-dup").hidden = true; document.getElementById("cf-lastname").focus();
    }
    function save(another) {
        err("");
        var last = val("cf-lastname").trim(), first = val("cf-firstname").trim();
        if (!last && !first) { err("Geef minstens een naam in.", "cf-lastname"); return; }
        if (!val("Phone_number").trim() && !val("Email").trim()) { err("Geef een telefoonnummer of een e-mailadres in.", "Phone_number"); return; }
        var fd = new FormData(); fd.append("__RequestVerificationToken", token());
        function add(k, v) { if (v != null && v !== "") fd.append(k, v); }
        if (id) fd.append("id", id);
        add("FirstName", first); add("LastName", last); add("PartnerName", val("cf-partner")); add("Email", val("Email")); add("Phone", val("Phone_number").trim() ? val("Phone") : "");
        add("Street", val("cf-street")); add("PostcodeId", val("cf-postcode")); add("Source", source); if (source === "Makelaar") add("BrokerCompanyId", val("cf-broker")); add("SeenVia", val("cf-seen"));
        add("ContactDate", val("cf-date")); add("Assignee", val("cf-assigned")); add("Budget", val("cf-budget")); add("Financing", val("cf-financing")); add("OwnHome", val("cf-own")); add("Wish", val("cf-wish"));
        add("FirstNote", val("cf-note")); add("NextText", val("cf-next-text")); add("NextDate", val("cf-next-date"));
        fd.append("ConsentContact", $("#cf-consent-contact").checked ? "true" : "false"); fd.append("ConsentNewsletter", $("#cf-consent-news").checked ? "true" : "false");
        fd.append("unitIds", $$("#cf-units .is-on").map(function (b) { return b.dataset.unit; }).join(","));
        var btns = [$("#cf-save"), $("#cf-save-new")].filter(Boolean); btns.forEach(function (b) { b.disabled = true; });
        fetch(root.dataset.saveUrl, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
            if (!j.ok) { err(j.error || "Opslaan mislukt.", j.field === "lastname" ? "cf-lastname" : j.field === "phone" ? "Phone_number" : j.field === "email" ? "Email" : null); return; }
            if (another && !id) { toast("Contact bewaard"); reset(); return; }
            window.location.href = j.url;
        }).catch(function () { err("Opslaan mislukt. Controleer je verbinding."); }).then(function () { btns.forEach(function (b) { b.disabled = false; }); });
    }
    // Het foutoverzicht (punt 24) controleert telefoon/e-mail bij het indienen en blokkeert bij fouten (stopImmediatePropagation); daarna volgt de eigen controle en het opslaan.
    var form = document.getElementById("cf-form"), another = false;
    if (window.GlV2ErrorSummary) window.GlV2ErrorSummary.init({ form: form, container: document.getElementById("gl-v2-error-summary") });
    form.addEventListener("submit", function (e) { e.preventDefault(); var a = another; another = false; save(a); });
    var sn = $("#cf-save-new"); if (sn) sn.addEventListener("click", function () { another = true; if (form.requestSubmit) form.requestSubmit(); else form.dispatchEvent(new Event("submit", { cancelable: true })); });
})();
