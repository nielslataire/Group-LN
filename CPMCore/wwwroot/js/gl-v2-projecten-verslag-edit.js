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
    // Punten in concept/ter goedkeuring mogen niet onafgewerkt mee in een verstuurd verslag (zie Finish
    // in ProjectVerslagenController). Basisaantal komt van de server (bij page load); elk nieuw punt
    // start altijd als concept, dus +1 per geslaagde addPoint(). Een goedkeuring via het zijpaneel zonder
    // herladen telt hier niet af — de server controleert dit sowieso opnieuw vóór het effectief verstuurt.
    var planPages = data.planPages || [];

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

    // ── verlaat-bewaking: "niet-opgeslagen" = een wijziging die nog niet bevestigd is bewaard (wachtend, onderweg of mislukt)
    //    of een nieuw punt waarvan de titel getypt maar nog niet toegevoegd is. De badge + modal worden door gl-v2-leave-guard.js afgehandeld.
    var editRev = 0, savedRev = 0, pointDirty = {}, pointTr = {};
    function isDirty() {
        if (readOnly) return false;
        var t = document.getElementById("vs-new-title");
        return editRev !== savedRev || Object.keys(pointDirty).length > 0 || !!(t && t.value.trim());
    }
    function refreshDirty() { var b = document.getElementById("vs-dirty-badge"); if (b) b.hidden = !isDirty(); }
    setInterval(refreshDirty, 400);
    window.addEventListener("beforeunload", function (e) {
        var b = document.getElementById("vs-dirty-badge");
        // verborgen = de eigen bevestigingsmodal is al doorlopen (gl-v2-leave-guard.js)
        if (!window.__vsLeaving && isDirty() && !(b && b.hidden)) { e.preventDefault(); e.returnValue = ""; }
    });

    // ── algemene velden ──
    var saveTimer = null;
    function saveNow() {
        var rev = editRev;
        return post("Save", {
            datum: $("#vs-datum").value, uur: $("#vs-uur").value, weer: $("#vs-weer").value, opmerkingen: $("#vs-opm").value,
            volgendDatum: $("#vs-volgend").value, volgendUur: $("#vs-volgend-uur").value, aanwezigenJson: JSON.stringify(aanwezigen)
        }).then(function (j) { flash(j.ok ? "Bewaard" : (j.error || "Opslaan mislukt"), !j.ok); if (j.ok) { savedRev = Math.max(savedRev, rev); refreshDirty(); } return j; }).catch(function () { flash("Opslaan mislukt", true); return { ok: false }; });
    }
    function saveGeneral() {
        if (readOnly) return;
        editRev++; refreshDirty();
        clearTimeout(saveTimer);
        saveTimer = setTimeout(saveNow, 600);
    }
    // "Opslaan & terug": eerst nog openstaande wijzigingen wegschrijven, dan naar de lijst
    var sb = document.getElementById("vs-save-back");
    if (sb) sb.addEventListener("click", function (ev) {
        ev.preventDefault(); clearTimeout(saveTimer);
        function leave() { window.__vsLeaving = true; window.location.href = sb.getAttribute("href"); }
        saveNow().then(function (j) {
            if (!(j && j.ok === false)) { leave(); return; }
            // opslaan mislukt: eigen bevestigingsmodal (geen browserdialoog)
            var m = document.getElementById("vs-savefail-modal"), c = document.getElementById("vs-savefail-confirm");
            if (!m || !c || !window.bootstrap) { leave(); return; }
            var fresh = c.cloneNode(true); c.parentNode.replaceChild(fresh, c);
            fresh.addEventListener("click", leave);
            window.bootstrap.Modal.getOrCreateInstance(m).show();
        });
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
        var on = tr.querySelector(".gl-v2-vs-ter .is-on"), id = tr.dataset.issue, key = JSON.stringify([on ? on.dataset.v : "", tr.querySelector(".gl-v2-vs-opm").value]);
        pointDirty[id] = key; pointTr[id] = tr; refreshDirty();
        return post("Point", { issueId: id, terPlaatse: on ? on.dataset.v : "", opmerking: tr.querySelector(".gl-v2-vs-opm").value })
            .then(function (j) { if (j.ok) { if (pointDirty[id] === key) { delete pointDirty[id]; refreshDirty(); } tr.classList.toggle("is-checked", !!on); updateSummary(j.gecontroleerd); } else flash("Opslaan mislukt", true); })
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
            pointDirty[tr.dataset.issue] = "wacht"; pointTr[tr.dataset.issue] = tr; refreshDirty();
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

    // ── plannen rasteren (pdf.js) met de pins, voor voorbeeld en afronden ──
    // De bijlage van het verslag toont elk plan met genummerde bolletjes; de browser maakt die afbeelding (de server kan geen PDF-plan renderen).
    var pdfCache = {};
    function loadImage(url) { return new Promise(function (res, rej) { var im = new Image(); im.onload = function () { res(im); }; im.onerror = rej; im.src = url; }); }
    function drawPins(ctx, cw, ch, pins) {
        var r = Math.max(14, cw * 0.011);
        (pins || []).forEach(function (p) {
            var x = p.x * cw, y = p.y * ch;
            ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI * 2); ctx.fillStyle = p.kritiek ? "#8B2A2A" : (p.label === "O" ? "#8A6A32" : "#00532D"); ctx.fill();
            ctx.lineWidth = Math.max(2, r * 0.18); ctx.strokeStyle = "#ffffff"; ctx.stroke();
            ctx.fillStyle = "#ffffff"; ctx.font = "700 " + Math.round(r * 1.05) + "px sans-serif"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
            ctx.fillText(p.label, x, y + r * 0.04);
        });
    }
    function rasterize(def) {
        var lib = window.pdfjsLib || window["pdfjs-dist/build/pdf"];
        var canvas = document.createElement("canvas"), ctx = canvas.getContext("2d");
        function finish(w, h) { drawPins(ctx, w, h, def.pins); return canvas.toDataURL("image/jpeg", 0.85); }
        if (def.kind === "image") {
            return loadImage(def.url).then(function (im) {
                var scale = Math.min(1, 2000 / im.naturalWidth); canvas.width = Math.round(im.naturalWidth * scale); canvas.height = Math.round(im.naturalHeight * scale);
                ctx.fillStyle = "#fff"; ctx.fillRect(0, 0, canvas.width, canvas.height); ctx.drawImage(im, 0, 0, canvas.width, canvas.height);
                return finish(canvas.width, canvas.height);
            });
        }
        if (!lib) return Promise.reject(new Error("pdf.js ontbreekt"));
        lib.GlobalWorkerOptions.workerSrc = "https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.worker.min.js";
        var docP = pdfCache[def.url] || (pdfCache[def.url] = lib.getDocument({ url: def.url, withCredentials: true }).promise);
        return docP.then(function (doc) { return doc.getPage(def.page || 1); }).then(function (pg) {
            var base = pg.getViewport({ scale: 1 }), scale = 2000 / base.width, vp = pg.getViewport({ scale: scale });
            canvas.width = Math.floor(vp.width); canvas.height = Math.floor(vp.height);
            ctx.fillStyle = "#fff"; ctx.fillRect(0, 0, canvas.width, canvas.height);
            return pg.render({ canvasContext: ctx, viewport: vp }).promise.then(function () { return finish(canvas.width, canvas.height); });
        });
    }
    // ── Verslag voorbereiden (design-handoff 44): volle pagina in het contentvlak, geen overlay ──
    var PREP_MSG = [
        "Gemiddeld 20 – 40 seconden",
        "De architect tekent de laatste lijnen bij…",
        "Pins worden netjes op het plan geprikt…",
        "Foto's worden recht gehangen aan de bouwkeet…",
        "De werfleider zet nog snel een kruisje bij ‘opgelost’…",
        "Hardhoed op, het verslag wordt opgemaakt…",
        "Nog even de waterpas erover…"
    ], prepTimer = null, prepUrl = null, prepFoot = 0;
    var CHECK = '<svg viewBox="0 0 16 16"><path d="M3.4 8.4l3 3 6.2-6.8"/></svg>';
    function prepArt(klaar) {
        if (klaar) return '<div class="gl-v2-vsprep__ok"><span class="ring"></span><span class="disc">' + CHECK + '</span></div>';
        var ln = [[100, 0, 82], [84, .25, 94], [96, .5, 112], [70, .75, 124], [90, 1, 142]].map(function (l) { return '<span class="ln" style="width:' + l[0] + 'px;top:' + l[2] + 'px;animation-delay:' + l[1] + 's"></span>'; }).join("");
        return '<div class="gl-v2-vsprep__float"><div class="gl-v2-vsprep__sheet back"></div><div class="gl-v2-vsprep__sheet front"><span class="top"></span><span class="ttl"></span><span class="ph1"></span><span class="ph2"></span>' + ln + '<span class="scan"></span></div></div>';
    }
    function prepSteps(steps) {
        return steps.map(function (s) {
            var ico = s[2] === "done" ? '<span class="gl-v2-vsprep__ico done">' + CHECK + '</span>' : '<span class="gl-v2-vsprep__ico ' + s[2] + '"><i></i></span>';
            return '<div class="gl-v2-vsprep__step is-' + s[2] + '">' + ico + '<b>' + s[0] + '</b><em>' + (s[1] || "") + '</em></div>';
        }).join("");
    }
    // o: { klaar, kicker, title, sub, steps:[[tekst, meta, done|act|todo]], actions }
    function prepShow(o) {
        var host = document.getElementById("vs-prep");
        if (!host) {
            host = document.createElement("section"); host.id = "vs-prep"; host.className = "gl-v2-vsprep"; host.setAttribute("role", "status"); host.setAttribute("aria-live", "polite");
            var form = document.getElementById("gl-v2-vs-edit"); form.parentNode.insertBefore(host, form.nextSibling);
        }
        host.innerHTML = '<div class="gl-v2-vsprep__col"><div class="gl-v2-vsprep__art">' + prepArt(o.klaar) + '</div>' +
            '<div class="gl-v2-vsprep__head"><span class="gl-v2-vsprep__kicker">' + o.kicker + '</span><h2 class="gl-v2-vsprep__title">' + o.title + '</h2><p class="gl-v2-vsprep__sub">' + o.sub + '</p></div>' +
            (o.klaar ? '' : '<div class="gl-v2-vsprep__bar"><i></i></div>') +
            '<div class="gl-v2-vsprep__steps">' + prepSteps(o.steps) + '</div>' +
            (o.klaar ? '' : '<p class="gl-v2-vsprep__foot"><span></span></p>') +
            (o.actions ? '<div class="gl-v2-vsprep__actions">' + o.actions + '</div>' : '') + '</div>';
        host.hidden = false; document.body.classList.add("gl-v2-vsprep-on");
        clearInterval(prepTimer);
        var span = host.querySelector(".gl-v2-vsprep__foot span");
        if (span) {
            var next = function () { span.textContent = PREP_MSG[prepFoot++ % PREP_MSG.length]; var c = span.cloneNode(true); span.replaceWith(c); span = c; };
            next(); prepTimer = setInterval(next, 2800);
        }
        window.scrollTo(0, 0);
    }
    function prepHide() {
        clearInterval(prepTimer);
        var host = document.getElementById("vs-prep"); if (host) host.hidden = true;
        document.body.classList.remove("gl-v2-vsprep-on");
        if (prepUrl) { URL.revokeObjectURL(prepUrl); prepUrl = null; }
    }
    function prepCounts() {
        var nieuw = $("#vs-new-list") ? $("#vs-new-list").children.length : 0, open = $$("#vs-open-body tr[data-issue]").length;
        return (nieuw + open) + " punten · " + planPages.length + " plan(nen)";
    }
    function prepSteps3(planDone, pdfAct, extra) {
        var n = planPages.length, nieuw = $("#vs-new-list") ? $("#vs-new-list").children.length : 0, open = $$("#vs-open-body tr[data-issue]").length;
        var s = [["Punten verzamelen", String(nieuw + open), "done"],
        ["Plannen met aanduidingen", n ? Math.min(planDone, n) + " / " + n : "geen", planDone >= n ? "done" : "act"],
        [extra ? "PDF opmaken en versturen" : "PDF opmaken", "", planDone >= n ? (pdfAct ? "act" : "todo") : "todo"]];
        return s;
    }
    window.addEventListener("pageshow", function (e) { if (e.persisted) prepHide(); });

    function buildPlanImages(onProgress) {
        var map = {}, chain = Promise.resolve(), n = 0;
        planPages.forEach(function (def) {
            chain = chain.then(function () { return rasterize(def).then(function (u) { map[def.key] = u; }).catch(function () { /* plan niet te laden: de bijlage laat dit plan weg */ }); })
                .then(function () { n++; if (onProgress) onProgress(n); });
        });
        return chain.then(function () { return map; });
    }
    function prepUpdateSteps(steps) { var el = document.querySelector("#vs-prep .gl-v2-vsprep__steps"); if (el) el.innerHTML = prepSteps(steps); }
    function fillForm(form, map) {
        $$("input[data-planimg]", form).forEach(function (i) { i.remove(); });
        Object.keys(map).forEach(function (k) {
            var i = document.createElement("input"); i.type = "hidden"; i.name = "planimg:" + k; i.value = map[k]; i.setAttribute("data-planimg", ""); form.appendChild(i);
        });
    }

    // Voorbeeld en afronden lezen het verslag uit de database: eerst alles wegschrijven wat nog wacht.
    function flushAll() {
        var jobs = [];
        clearTimeout(saveTimer); if (editRev !== savedRev) jobs.push(saveNow());
        Object.keys(pointDirty).forEach(function (id) { if (pointTr[id]) jobs.push(savePoint(pointTr[id])); });
        return Promise.all(jobs);
    }

    // ── voorbeeld PDF: voorbereiden op de pagina zelf; bij klaar een knop om de PDF te openen (design 44a/44c) ──
    var prevBtn = document.getElementById("vs-preview");
    function prepFail(msg) { prepHide(); if (prevBtn) prevBtn.disabled = false; window.alert(msg || "Het voorbeeld kon niet worden opgemaakt."); }
    if (prevBtn) prevBtn.addEventListener("click", function () {
        prevBtn.disabled = true;
        prepShow({ kicker: "Voorbeeld · voorbereiden", title: "Het voorbeeld wordt voorbereid", sub: prepCounts() + ". Het voorbeeld verschijnt zodra de PDF klaar is.", steps: prepSteps3(0, false, false) });
        flushAll().then(function () { return buildPlanImages(function (n) { prepUpdateSteps(prepSteps3(n, false, false)); }); }).then(function (map) {
            var f = document.getElementById("vs-preview-form"); fillForm(f, map);
            prepUpdateSteps(prepSteps3(planPages.length, true, false));
            return fetch(f.action, { method: "POST", body: new FormData(f), credentials: "same-origin" });
        }).then(function (r) { if (!r.ok) throw new Error("pdf"); return r.blob(); }).then(function (blob) {
            prepUrl = URL.createObjectURL(blob); prevBtn.disabled = false;
            prepShow({ klaar: true, kicker: "Voorbeeld klaar", title: "Voorbeeld klaar", sub: "Het voorbeeld is opgemaakt. Er is nog niets goedgekeurd of verstuurd.",
                steps: [["Punten verzamelen", "", "done"], ["Plannen met aanduidingen", "", "done"], ["PDF opmaken", "", "done"]],
                actions: '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" id="vs-prep-back">Terug naar verslag</button><button type="button" class="gl-v2-btn gl-v2-btn-primary" id="vs-prep-open">Voorbeeld openen</button>' });
        }).catch(function () { prepFail(); });
    });
    document.addEventListener("click", function (ev) {
        if (ev.target.closest("#vs-prep-back")) prepHide();
        else if (ev.target.closest("#vs-prep-open") && prepUrl) window.open(prepUrl, "_blank");
    });

    // ── afronden ──
    function openFinish() {
        var m = document.getElementById("vs-finish-modal"); if (!m || !window.bootstrap) return;
        var open = $$("#vs-open-body tr[data-issue]"), unchecked = open.filter(function (r) { return !r.querySelector(".gl-v2-vs-ter .is-on"); }).length;
        var mailable = aanwezigen.filter(function (a) { return a.email; }).length, nieuw = $("#vs-new-list").children.length;
        $("#vs-finish-desc").textContent = nieuw + " nieuw(e) punt(en) worden goedgekeurd" + (unchecked ? "; " + unchecked + " openstaand punt(en) zijn niet nagekeken en blijven zoals ze zijn" : "") +
            ". Het verslag (met foto's en plannen) gaat als PDF naar " + mailable + " aanwezige(n) met een e-mailadres.";
        window.bootstrap.Modal.getOrCreateInstance(m).show();
    }
    document.addEventListener("click", function (ev) { if (ev.target.closest(".js-vs-finish") || ev.target.closest("#vs-finish-open")) openFinish(); });
    var finishForm = document.getElementById("vs-finish-form");
    if (finishForm) finishForm.addEventListener("submit", function (ev) {
        if (finishForm.dataset.ready === "1") return;
        ev.preventDefault();
        var btn = document.getElementById("vs-finish-submit"); btn.disabled = true;
        var m = document.getElementById("vs-finish-modal"); if (m && window.bootstrap) { var inst = window.bootstrap.Modal.getInstance(m); if (inst) inst.hide(); }
        prepShow({ kicker: "Stap 1 van 2 · voorbereiden", title: "Het verslag wordt voorbereid", sub: prepCounts() + ". De punten worden goedgekeurd en het verslag gaat daarna naar de aanwezigen.", steps: prepSteps3(0, false, true) });
        flushAll().then(function () { return buildPlanImages(function (n) { prepUpdateSteps(prepSteps3(n, false, true)); }); }).then(function (map) {
            fillForm(finishForm, map); prepUpdateSteps(prepSteps3(planPages.length, true, true));
            finishForm.dataset.ready = "1"; window.__vsLeaving = true; finishForm.submit();
        }, function () { prepHide(); btn.disabled = false; });
    });
})();
