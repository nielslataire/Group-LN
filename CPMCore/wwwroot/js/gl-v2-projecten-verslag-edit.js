// gl-v2 — Werfverslag bewerken (design-handoff 40l): alles wordt tijdens het werken bewaard.
//   algemene velden + aanwezigen → POST Save (debounced) · ter plaatse/opmerking per punt → POST Point · nieuw punt → POST AddPoint
//   Afronden gebeurt met een gewone form-post (Finish) vanuit de bevestigingsmodal.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-vs-edit"), dataEl = document.getElementById("vs-data");
    if (!root || !dataEl) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }

    var data = JSON.parse(dataEl.textContent || "{}");
    var base = root.dataset.baseUrl;               // …/Verslagen/{id}
    var token = ($("#vs-token-form input[name=__RequestVerificationToken]") || {}).value || "";
    var aanwezigen = (data.aanwezigen || []).map(function (a) { return { name: a.name, email: a.email || "" }; });
    var readOnly = !!data.readOnly;

    function post(path, fields) {
        var fd = new FormData(); fd.append("__RequestVerificationToken", token);
        Object.keys(fields || {}).forEach(function (k) { if (fields[k] != null) fd.append(k, fields[k]); });
        return fetch(base + "/" + path, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function flash(msg, isErr) {
        var s = $("#vs-saved"); if (!s) return;
        s.textContent = msg; s.className = "gl-v2-vs-saved" + (isErr ? " is-err" : " is-ok");
        clearTimeout(flash.t); flash.t = setTimeout(function () { s.textContent = ""; }, 2500);
    }

    // ── algemene velden ──
    var saveTimer = null;
    function saveNow() {
        return post("Save", {
            datum: $("#vs-datum").value, uur: $("#vs-uur").value, weer: $("#vs-weer").value, opmerkingen: $("#vs-opm").value,
            volgendDatum: $("#vs-volgend").value, volgendUur: $("#vs-volgend-uur").value, aanwezigenJson: JSON.stringify(aanwezigen)
        }).then(function (j) { flash(j.ok ? "Bewaard" : (j.error || "Opslaan mislukt"), !j.ok); return j; }).catch(function () { flash("Opslaan mislukt", true); return { ok: false }; });
    }
    function saveGeneral() {
        if (readOnly) return;
        clearTimeout(saveTimer);
        saveTimer = setTimeout(saveNow, 600);
    }
    // "Opslaan & terug": eerst nog openstaande wijzigingen wegschrijven, dan naar de lijst
    var sb = document.getElementById("vs-save-back");
    if (sb) sb.addEventListener("click", function (ev) {
        ev.preventDefault(); clearTimeout(saveTimer);
        saveNow().then(function (j) { if (j && j.ok === false) { if (!window.confirm("Opslaan is niet gelukt. Toch terug naar de lijst?")) return; } window.location.href = sb.getAttribute("href"); });
    });
    ["vs-uur", "vs-weer", "vs-opm", "vs-volgend-uur"].forEach(function (id) { var e = document.getElementById(id); if (e) e.addEventListener("input", saveGeneral); });
    ["vs-datum", "vs-volgend"].forEach(function (id) {
        var h = document.getElementById(id), t = document.getElementById(id + "_text");
        if (h) { h.addEventListener("change", saveGeneral); h.addEventListener("input", saveGeneral); }
        if (t) t.addEventListener("blur", function () { setTimeout(saveGeneral, 50); });
    });

    // ── aanwezigen (chips) ──
    function renderChips() {
        var box = $("#vs-chips");
        box.innerHTML = aanwezigen.length ? "" : '<span class="gl-v2-field-help">Nog niemand aangeduid.</span>';
        aanwezigen.forEach(function (a, i) {
            var c = document.createElement("span"); c.className = "gl-v2-vs-chip";
            c.innerHTML = esc(a.name) + (a.email ? "" : ' <small title="Zonder e-mailadres krijgt deze persoon het verslag niet">geen mail</small>') + (readOnly ? "" : '<button type="button" data-i="' + i + '" aria-label="Verwijder ' + esc(a.name) + '"><i class="ph ph-x" aria-hidden="true"></i></button>');
            box.appendChild(c);
        });
    }
    // zoekveld: interne personen + personen/bedrijven van de werf; het e-mailadres wordt overgenomen
    var picked = null;   // { name, email } van de laatst gekozen zoekresultaat
    function addPerson() {
        var input = $("#vs-add_input"), e = $("#vs-add-email");
        var name = (picked ? picked.name : (input ? input.value : "")).trim();
        if (!name) { if (input) input.focus(); return; }
        if (aanwezigen.some(function (a) { return a.name.toLowerCase() === name.toLowerCase(); })) { resetAdd(); return; }
        aanwezigen.push({ name: name, email: (e.value || (picked ? picked.email : "") || "").trim() });
        resetAdd(); renderChips(); saveGeneral();
    }
    function resetAdd() {
        picked = null; $("#vs-add-email").value = "";
        var combo = document.querySelector("#vs-add") ? document.querySelector("#vs-add").closest("[data-gl-v2-combo]") : null;
        if (combo && window.GlV2Combo) window.GlV2Combo.setValue(combo, "", "", "");
        var input = $("#vs-add_input"); if (input) { input.value = ""; input.focus(); }
    }
    document.addEventListener("gl-v2:combo-select", function (ev) {
        if (!ev.target.closest || !ev.target.closest("#vs-add") && !(ev.target.querySelector && ev.target.querySelector("#vs-add"))) return;
        var item = (ev.detail && ev.detail.item) || {};
        var id = String(item.id || "");
        var email = id.indexOf("@") > 0 ? id : "";
        picked = { name: item.text || ev.detail.text || "", email: email };
        $("#vs-add-email").value = email;
    });
    if (!readOnly) {
        $("#vs-add-btn").addEventListener("click", addPerson);
        $("#vs-add-email").addEventListener("keydown", function (ev) { if (ev.key === "Enter") { ev.preventDefault(); addPerson(); } });
        $("#vs-chips").addEventListener("click", function (ev) {
            var b = ev.target.closest("button[data-i]"); if (!b) return;
            aanwezigen.splice(parseInt(b.dataset.i, 10), 1); renderChips(); saveGeneral();
        });
    }
    renderChips();

    // ── openstaande punten: ter plaatse + opmerking ──
    function updateSummary(count) {
        var rows = $$("#vs-open-body tr[data-issue]");
        var n = typeof count === "number" ? count : rows.filter(function (r) { return r.querySelector(".gl-v2-vs-ter .is-on"); }).length;
        var el = $("#vs-summary"); if (el) el.textContent = "Concept · " + n + " van " + rows.length + " punten nagekeken";
    }
    function savePoint(tr) {
        var on = tr.querySelector(".gl-v2-vs-ter .is-on");
        post("Point", { issueId: tr.dataset.issue, terPlaatse: on ? on.dataset.v : "", opmerking: tr.querySelector(".gl-v2-vs-opm").value })
            .then(function (j) { if (j.ok) { tr.classList.toggle("is-checked", !!on); updateSummary(j.gecontroleerd); } else flash("Opslaan mislukt", true); })
            .catch(function () { flash("Opslaan mislukt", true); });
    }
    if (!readOnly) {
        $("#vs-open-body").addEventListener("click", function (ev) {
            var b = ev.target.closest(".gl-v2-vs-ter button"); if (!b) return;
            var tr = b.closest("tr"), was = b.classList.contains("is-on");
            $$(".gl-v2-vs-ter button", tr).forEach(function (x) { x.classList.remove("is-on"); });
            if (!was) b.classList.add("is-on");
            savePoint(tr);
        });
        var optTimers = {};
        $("#vs-open-body").addEventListener("input", function (ev) {
            if (!ev.target.classList.contains("gl-v2-vs-opm")) return;
            var tr = ev.target.closest("tr"); clearTimeout(optTimers[tr.dataset.issue]);
            optTimers[tr.dataset.issue] = setTimeout(function () { savePoint(tr); }, 700);
        });
    }
    $("#vs-open-seg").addEventListener("click", function (ev) {
        var b = ev.target.closest(".gl-v2-vs-seg"); if (!b) return;
        $$("#vs-open-seg .gl-v2-vs-seg").forEach(function (s) { s.classList.toggle("is-active", s === b); });
        $$("#vs-open-body tr[data-issue]").forEach(function (tr) { tr.classList.toggle("is-hidden", b.dataset.f === "reported" && tr.dataset.canon !== "11"); });
    });
    updateSummary();

    // ── nieuw punt ──
    function addPoint() {
        var inp = $("#vs-new-title"), err = $("#vs-new-err"); err.hidden = true;
        if (!inp.value.trim()) { inp.focus(); return; }
        var cInp = document.getElementById("vs-new-contractor");
        post("AddPoint", { title: inp.value, contractorId: cInp ? cInp.value : "" }).then(function (j) {
            if (!j.ok) { err.textContent = j.error || "Toevoegen mislukt."; err.hidden = false; return; }
            var li = document.createElement("li"); li.dataset.issue = j.issueId;
            li.innerHTML = '<span class="gl-v2-badge is-neutral">Concept</span><span><b>' + esc(j.title) + "</b> <small>" + esc(j.nr) + " · Algemeen" + (j.contractor ? " · " + esc(j.contractor) : "") + "</small></span>" +
                '<button type="button" class="gl-v2-icon-btn js-pt-edit" data-id="' + j.issueId + '" title="Eenheid, aannemer en details kiezen" aria-label="Bewerken ' + esc(j.nr) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>';
            $("#vs-new-list").appendChild(li);
            $("#vs-new-count").textContent = $("#vs-new-list").children.length;
            inp.value = ""; inp.focus();
        }).catch(function () { err.textContent = "Toevoegen mislukt."; err.hidden = false; });
    }
    if (!readOnly) {
        $("#vs-new-btn").addEventListener("click", addPoint);
        $("#vs-new-title").addEventListener("keydown", function (ev) { if (ev.key === "Enter") { ev.preventDefault(); addPoint(); } });
    }

    // ── afronden ──
    function openFinish() {
        var m = document.getElementById("vs-finish-modal"); if (!m || !window.bootstrap) return;
        var open = $$("#vs-open-body tr[data-issue]"), unchecked = open.filter(function (r) { return !r.querySelector(".gl-v2-vs-ter .is-on"); }).length;
        var mailable = aanwezigen.filter(function (a) { return a.email; }).length, nieuw = $("#vs-new-list").children.length;
        $("#vs-finish-desc").textContent = nieuw + " nieuw(e) punt(en) blijven concept: je kan er nog foto's en details aan toevoegen en ze daarna goedkeuren." +
            (unchecked ? " " + unchecked + " openstaand punt(en) zijn niet nagekeken en blijven zoals ze zijn." : "") +
            " Het verslag gaat als PDF naar " + mailable + " aanwezige(n) met een e-mailadres.";
        window.bootstrap.Modal.getOrCreateInstance(m).show();
    }
    document.addEventListener("click", function (ev) { if (ev.target.closest(".js-vs-finish") || ev.target.closest("#vs-finish-open")) openFinish(); });
})();
