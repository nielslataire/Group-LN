// gl-v2 — Contactdetail met opvolging (design-handoff 44b): statusbalk, composer (notitie, gesprek, e-mail, bezichtiging, optie + volgende actie), toewijzen, mailen,
// verzetten, omzetten naar klant en verwijderen. Alle acties posten via fetch en herladen daarna de pagina.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-cd-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var id = root.dataset.id, canWrite = root.dataset.canWrite === "1";
    function token() { var i = document.querySelector("input[name=__RequestVerificationToken]"); return i ? i.value : ""; }
    function post(url, obj) {
        var fd = new FormData(); fd.append("__RequestVerificationToken", token());
        Object.keys(obj || {}).forEach(function (k) { if (obj[k] != null && obj[k] !== "") fd.append(k, obj[k]); });
        return fetch(url, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function val(i) { var e = document.getElementById(i); return e ? e.value : ""; }
    function modal(idm) { var m = document.getElementById(idm); return m && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(m) : null; }
    function done() { window.location.reload(); }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-pt-toast gl-v2-cd-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 5000);
    }
    function fail(el, msg) { if (!el) { toast(msg); return; } el.textContent = msg; el.hidden = false; }
    if (!canWrite) return;

    // ── composer ──
    var kind = "note", PH = { note: "Wat werd er genoteerd?", call: "Wat werd er besproken?", mail: "Wat werd er gemaild?", visit: "Notitie bij de bezichtiging (optioneel)", option: "Notitie bij de optie (optioneel)" };
    function setKind(k) {
        kind = k;
        $$("#cd-composer .gl-v2-cd-ktabs button").forEach(function (b) { b.classList.toggle("is-on", b.dataset.kind === k); });
        $$("#cd-composer .gl-v2-cd-extra").forEach(function (x) { x.hidden = x.dataset.for !== k; });
        $("#cd-text").placeholder = PH[k]; var e = $("#cd-err"); if (e) e.hidden = true;
    }
    $("#cd-composer").addEventListener("click", function (ev) { var b = ev.target.closest(".gl-v2-cd-ktabs button"); if (b) setKind(b.dataset.kind); });
    ["cd-option-btn", "cd-option-btn-m"].forEach(function (i) {
        var b = document.getElementById(i); if (b) b.addEventListener("click", function () { setKind("option"); $("#cd-composer").scrollIntoView({ behavior: "smooth", block: "center" }); setTimeout(function () { var d = document.getElementById("cd-option-until_text"); if (d) d.focus(); }, 350); });
    });
    $("#cd-save").addEventListener("click", function () {
        var err = $("#cd-err"), btn = this; err.hidden = true;
        var data = { kind: kind, text: val("cd-text"), nextText: val("cd-next-text"), nextDate: val("cd-next-date"), nextUserId: val("cd-next-user") };
        if (kind === "call") data.duration = val("cd-duration");
        if (kind === "visit") { data.visitDate = val("cd-visit-date"); data.visitTime = val("cd-visit-time"); data.unit = val("cd-visit-unit"); }
        if (kind === "option") { data.optionUntil = val("cd-option-until"); data.unit = val("cd-option-unit"); }
        btn.disabled = true;
        post(root.dataset.eventUrl, Object.assign({ id: id }, data)).then(function (j) { if (j.ok) done(); else { btn.disabled = false; fail(err, j.error || "Opslaan mislukt."); } })
            .catch(function () { btn.disabled = false; fail(err, "Opslaan mislukt. Controleer je verbinding."); });
    });
    setKind("note");

    // ── statusbalk ──
    var pendingStatus = null;
    function setStatus(s, reason) {
        post(root.dataset.statusUrl, { ids: id, status: s, reason: reason || "" }).then(function (j) { if (j.ok) done(); else toast(j.error || "Wijzigen mislukt."); });
    }
    $("#cd-steps").addEventListener("click", function (ev) {
        var b = ev.target.closest("[data-status]"); if (!b || b.disabled) return;
        var s = parseInt(b.dataset.status, 10);
        if (s === parseInt(root.dataset.status, 10)) return;
        if (s === 5) { pendingStatus = s; var m = modal("cd-reason-modal"); if (m) m.show(); return; }
        setStatus(s);
    });
    var ok = $("#cd-reason-ok"); if (ok) ok.addEventListener("click", function () { setStatus(pendingStatus, val("cd-reason")); });

    // ── toewijzen ──
    var asg = document.getElementById("cd-assigned");
    if (asg) asg.addEventListener("change", function () { post(root.dataset.assignUrl, { ids: id, assignee: asg.value }).then(function (j) { if (!j.ok) toast(j.error || "Toewijzen mislukt."); }); });

    // ── volgende actie afvinken ──
    var nd = $("#cd-next-done"); if (nd) nd.addEventListener("click", function () { post(root.dataset.nextUrl, { id: id, done: "true" }).then(function (j) { if (j.ok) done(); }); });

    // ── verzetten / verlengen ──
    var reschedMode = "visit";
    function openResched(mode) {
        reschedMode = mode;
        $("#cd-resched-title").textContent = mode === "visit" ? "Bezichtiging verzetten" : "Optie verlengen";
        $("#cd-resched-time-field").hidden = mode !== "visit";
        var e = $("#cd-resched-err"); e.hidden = true;
        var m = modal("cd-resched-modal"); if (m) m.show();
    }
    var rv = $("#cd-resched-visit"); if (rv) rv.addEventListener("click", function () { openResched("visit"); });
    var ro = $("#cd-resched-option"); if (ro) ro.addEventListener("click", function () { openResched("option"); });
    $("#cd-resched-ok").addEventListener("click", function () {
        var d = val("cd-resched-date"), err = $("#cd-resched-err");
        if (!d) { fail(err, "Kies een datum."); return; }
        var data = { id: id };
        if (reschedMode === "visit") { data.visitDate = d; data.visitTime = val("cd-resched-time"); } else data.optionUntil = d;
        post(root.dataset.reschedUrl, data).then(function (j) { if (j.ok) done(); else fail(err, j.error || "Bewaren mislukt."); });
    });

    // ── mailen ──
    $("#cd-mail-send").addEventListener("click", function () {
        var err = $("#cd-mail-err"), t = val("cd-template"); err.hidden = true;
        if (!t) { fail(err, "Kies een sjabloon."); return; }
        var btn = this; btn.disabled = true;
        post(root.dataset.mailUrl, { id: id, templateId: t, includeSignature: $("#cd-signature").checked ? "true" : "false" })
            .then(function (j) { if (j.ok) done(); else { btn.disabled = false; fail(err, j.error || "Versturen mislukt."); } })
            .catch(function () { btn.disabled = false; fail(err, "Versturen mislukt."); });
    });

    // ── omzetten naar klant en verwijderen (eigen bevestigingen) ──
    function onConfirm(modalId, fn) {
        var m = document.getElementById(modalId); if (!m) return;
        var f = $("form", m); if (f) f.addEventListener("submit", function (ev) { ev.preventDefault(); fn(); });
    }
    onConfirm("cd-convert-modal", function () {
        post(root.dataset.statusUrl, { ids: id, status: 4 }).then(function (j) { if (j.ok) window.location.href = root.dataset.clientUrl; else toast(j.error || "Omzetten mislukt."); });
    });
    onConfirm("cd-delete-modal", function () {
        var m = document.getElementById("cd-delete-modal"), btn = $('button[type="submit"]', m);
        function fail(msg) { if (window.GlV2Modal && btn) window.GlV2Modal.clearButtonLoading(btn); var mi = modal("cd-delete-modal"); if (mi) mi.hide(); toast(msg); }
        post(root.dataset.deleteUrl, { ids: id }).then(function (j) {
            if (j.ok && j.count > 0) window.location.href = root.dataset.listUrl; else fail(j.error || "Verwijderen mislukt: het contact werd niet gevonden.");
        }).catch(function () { fail("Verwijderen mislukt. Probeer het opnieuw."); });
    });
})();
