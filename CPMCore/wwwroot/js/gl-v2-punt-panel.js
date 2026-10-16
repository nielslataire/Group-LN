// gl-v2 — Punten: zijpaneel nieuw / bewerken / dupliceren (design-handoff 40c) met "Locatie op plan" (40c/40k).
// Gedeeld door de puntenlijst en de plan-pagina. Publiek: window.PuntPanel.open(mode, id, preset)
//   mode: "new" | "edit" | "dup" · id: punt-id (edit/dup) · preset: { unitId, planKey, page, x, y } (nieuw punt vanaf een plan, 40k)
// Na opslaan wordt de pagina herladen (zodat lijst/plan de wijziging tonen).
(function () {
    "use strict";
    var panelEl = document.getElementById("pt-panel");
    if (!panelEl) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }

    var backdrop = $("#pt-panel-backdrop"), form = $("#pt-new-form");
    var deleteMediaUrl = panelEl.dataset.deleteMediaUrl || "", uploadPlanUrl = panelEl.dataset.uploadPlanUrl || "", createUrl = form.action, editUrl = panelEl.dataset.editUrl || "", editDataUrl = panelEl.dataset.editDataUrl || "", plansUrl = panelEl.dataset.plansUrl || "";
    var planOnlyCb = null;   // "Op plan" vanuit Inspreken/Rondgang: enkel plan kiezen + punt aanduiden, het formulier blijft verborgen
    var hook = null;   // voorstellen uit Inspreken/Rondgang: na bewaren geen herlaad maar een terugmelding
    var mode = "new", savedCount = 0, saving = false, plans = null, viewer = null, bigViewer = null, currentPlanKey = "", pendingPlace = null;

    // ── hulpjes ──
    function setSelect(id, v) {
        var wrap = document.getElementById(id + "_select");
        var opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + v + '"]');
        if (opt) opt.click();
    }
    // aannemer = keuzelijst met zoekveld (enkel aannemers met een contract); de verborgen input #pt-new-contractor draagt de waarde
    function setContractor(id) {
        var hid = document.getElementById("pt-new-contractor"), root = hid && hid.closest("[data-gl-v2-combo]");
        if (!root || !window.GlV2Combo) return;
        var opts = []; try { opts = JSON.parse(root.getAttribute("data-options") || "[]"); } catch (e) { opts = []; }
        var o = opts.filter(function (x) { return String(x.id) === String(id); })[0];
        window.GlV2Combo.setValue(root, id ? String(id) : "", o ? o.text : "", "");
    }
    function setDate(id, iso) {
        var h = document.getElementById(id), t = document.getElementById(id + "_text");
        if (h) h.value = iso || "";
        if (t) t.value = iso ? iso.split("-").reverse().join("/") : "";
    }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }
    function val(id) { var e = document.getElementById(id); return e ? e.value : ""; }

    // ── plan-locatie (design-handoff 40c/40p/40q/40r) ──
    // Geen plan gekozen → lege staat met "Plan kiezen" (modal 40p) en "Nieuw plan" (modal 40q); plan gekozen → voorbeeld met de pin,
    // "Ander plan", en klikken op het voorbeeld vergroot het plan om het punt te plaatsen.
    function loadPlans() {
        if (plans) return Promise.resolve(plans);
        if (!plansUrl) { plans = []; return Promise.resolve(plans); }
        return fetch(plansUrl, { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) { plans = j || []; return plans; }).catch(function () { plans = []; return plans; });
    }
    function planByKey(key) { return (plans || []).filter(function (p) { return p.key === key; })[0]; }
    function planTitle(p) { return (p.unitId == null ? "Algemeen" : p.unitName) + " · " + p.name; }
    function currentUnitId() { return parseInt(val("pt-new-unit") || "0", 10) || 0; }
    function currentUnitName() {
        var trig = document.querySelector("#pt-new-unit_select .gl-v2-select-trigger-label");
        return trig ? trig.textContent.trim() : "deze eenheid";
    }
    function unitPlans() { var u = currentUnitId(); return (plans || []).filter(function (p) { return p.isLatest !== false && (u === 0 ? p.unitId == null : p.unitId === u); }); }

    function renderPlanState() {
        var has = !!currentPlanKey;
        $("#pt-plan-empty").hidden = has;
        $("#pt-plan-box").hidden = !has;
        if (!has) {
            var n = unitPlans().length, name = currentUnitId() === 0 ? "het project (algemeen)" : currentUnitName();
            $("#pt-plan-empty-text").textContent = n
                ? "Voor " + name + " " + (n === 1 ? "is 1 plan" : "zijn " + n + " plannen") + " beschikbaar. Kies er één om het punt aan te duiden — of bewaar zonder plan."
                : "Voor " + name + " is er nog geen plan. Voeg er een toe om het punt aan te duiden — of bewaar zonder plan.";
        }
    }
    function clearPlan() {
        ["pt-plan-id", "pt-plan-page", "pt-plan-x", "pt-plan-y"].forEach(function (i) { var e = document.getElementById(i); if (e) e.value = ""; });
        currentPlanKey = "";
        if (viewer) viewer.setMarker(null);
        $("#pt-plan-clear-flag").value = mode === "edit" ? "true" : "false";
        renderPlanState();
    }
    function setHint(marker) {
        $("#pt-plan-hint").textContent = marker
            ? "Locatie gekozen · pagina " + marker.page + ". Klik op het plan om te vergroten of te verplaatsen."
            : "Klik op het plan om het te vergroten en de plaats van het punt aan te duiden.";
        $("#pt-plan-remove").hidden = !marker;
    }
    function showPlan(key, marker) {
        var plan = planByKey(key);
        if (!plan || !window.PuntPlanViewer) { clearPlan(); return; }
        currentPlanKey = key;
        $("#pt-plan-id").value = plan.planId;
        $("#pt-plan-clear-flag").value = "false";
        renderPlanState();
        $("#pt-plan-caption").textContent = planTitle(plan);
        if (!viewer) {
            // klein voorbeeld: klikken vergroot het plan (grote weergave) om het punt te plaatsen
            viewer = window.PuntPlanViewer.create($("#pt-plan-viewer"), { onPlace: function () { openBig(); } });
        }
        viewer.setMarker(marker || null);
        viewer.load(plan.url, marker ? marker.page : 1, plan.kind);
        setHint(marker);
    }

    // ── overlays boven het paneel ──
    function onShowOver(el) {
        el.addEventListener("show.bs.modal", function () { document.body.classList.add("pt-over"); });
        el.addEventListener("hidden.bs.modal", function () { if (!document.querySelector(".modal.show.gl-v2-pt-over")) document.body.classList.remove("pt-over"); });
    }
    function modalOf(id) { var m = document.getElementById(id); return m && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(m) : null; }

    // ── plan vergroten: punt plaatsen ──
    function openBig() {
        var plan = planByKey(currentPlanKey), bm = modalOf("pt-plan-big");
        if (!plan || !bm || !window.PuntPlanViewer) return;
        var cur = $("#pt-plan-x").value !== "" ? { page: parseInt($("#pt-plan-page").value, 10) || 1, x: parseFloat($("#pt-plan-x").value), y: parseFloat($("#pt-plan-y").value) } : null;
        pendingPlace = cur;
        $("#pt-plan-big-ok").disabled = !cur;
        $("#pt-plan-big-sub").textContent = planTitle(plan) + " — klik op het plan om het punt neer te zetten.";
        if (!bigViewer) {
            bigViewer = window.PuntPlanViewer.create($("#pt-plan-big-viewer"), {
                onPlace: function (page, x, y) { pendingPlace = { page: page, x: x, y: y }; bigViewer.setMarker(pendingPlace); $("#pt-plan-big-ok").disabled = false; }
            });
        }
        bigViewer.setMarker(cur);
        bm.show();
        setTimeout(function () { bigViewer.load(plan.url, cur ? cur.page : 1, plan.kind); }, 250);   // pas laden als de modal zichtbaar is (breedte)
    }
    function confirmBig() {
        if (!pendingPlace) return;
        $("#pt-plan-page").value = pendingPlace.page; $("#pt-plan-x").value = pendingPlace.x; $("#pt-plan-y").value = pendingPlace.y;
        if (viewer) viewer.setMarker(pendingPlace);
        setHint(pendingPlace);
        if (planOnlyCb) {
            var cb = planOnlyCb, pl = planByKey(currentPlanKey); planOnlyCb = null;
            cb({ planId: parseInt($("#pt-plan-id").value, 10), key: pl ? pl.key : "", page: pendingPlace.page, x: pendingPlace.x, y: pendingPlace.y, name: pl ? planTitle(pl) : "" });
        }
        var bm = modalOf("pt-plan-big"); if (bm) bm.hide();
    }

    // ── plan kiezen (40p) ──
    var pickKey = "", pickSeg = "unit";
    function pickSub(p) { return (p.soort || "plan") + (p.niveau ? " · " + p.niveau : "") + " · v" + (p.versie || 1) + " · " + p.datum; }
    function renderPick() {
        var q = ($("#pt-pick-search").value || "").trim().toLowerCase(), u = currentUnitId();
        var list = (plans || []).filter(function (p) {
            if (p.isLatest === false) return false;
            if (pickSeg === "unit" && !(p.unitId == null || p.unitId === u)) return false;
            return !q || (p.name + " " + (p.soort || "") + " " + (p.niveau || "") + " " + p.unitName).toLowerCase().indexOf(q) >= 0;
        });
        var html = "", last = null;
        // eerst de gekozen eenheid, dan de andere eenheden, algemeen laatst
        list.sort(function (a, b) {
            function rank(p) { return p.unitId == null ? 2 : (p.unitId === u ? 0 : 1); }
            return rank(a) - rank(b) || a.unitName.localeCompare(b.unitName) || a.name.localeCompare(b.name);
        });
        list.forEach(function (p) {
            var g = p.unitId == null ? "Algemeen" : p.unitName;
            if (g !== last) { html += '<div class="gl-v2-pt-pick-group">' + esc(g) + "</div>"; last = g; }
            html += '<button type="button" class="gl-v2-pt-pick-item' + (p.key === pickKey ? " is-active" : "") + '" data-key="' + esc(p.key) + '"><span class="gl-v2-pt-pick-ico"><i class="ph ph-map-trifold" aria-hidden="true"></i></span><span class="gl-v2-pt-pick-text"><b>' + esc(p.name) + "</b><small>" + esc(pickSub(p)) + '</small></span><span class="gl-v2-pt-pick-count">' + (p.count ? p.count : "") + '</span><i class="ph ph-check gl-v2-pt-pick-check" aria-hidden="true"></i></button>';
        });
        $("#pt-pick-list").innerHTML = html || '<div class="gl-v2-pt-pick-empty">Geen plan gevonden.</div>';
        $("#pt-pick-ok").disabled = !pickKey;
    }
    function openPick() {
        var pm = modalOf("pt-plan-pick-modal"); if (!pm) return;
        pickKey = currentPlanKey; pickSeg = "unit"; $("#pt-pick-search").value = "";
        var t = $("#pt-new-title").value.trim();
        $("#pt-pick-sub").textContent = t ? 'voor punt "' + t + '"' : "voor dit punt";
        var u = currentUnitId(), name = u === 0 ? "Algemeen" : currentUnitName();
        $$("#pt-pick-seg button").forEach(function (b) { b.classList.toggle("is-active", b.dataset.seg === "unit"); });
        $("#pt-pick-seg-unit").textContent = u === 0 ? "Algemeen" : name + " + algemeen";
        renderPick();
        pm.show();
    }
    function confirmPick() {
        if (!pickKey) return;
        var changed = pickKey !== currentPlanKey;
        var plan = planByKey(pickKey);
        // een plan van een andere eenheid? dan volgt de eenheid van het punt
        if (plan && plan.unitId != null && plan.unitId !== currentUnitId()) setSelect("pt-new-unit", String(plan.unitId));
        if (changed) { ["pt-plan-page", "pt-plan-x", "pt-plan-y"].forEach(function (i) { $("#" + i).value = ""; }); }
        showPlan(pickKey, changed ? null : (currentMarker()));
        var pm = modalOf("pt-plan-pick-modal"); if (pm) pm.hide();
        if (planOnlyCb) setTimeout(openBig, 320);   // na het sluiten van de lijst: het plan groot om het punt aan te duiden
    }
    function currentMarker() { return $("#pt-plan-x").value !== "" ? { page: parseInt($("#pt-plan-page").value, 10) || 1, x: parseFloat($("#pt-plan-x").value), y: parseFloat($("#pt-plan-y").value) } : null; }

    // ── nieuw plan (40q) ──
    var nameTouched = false;
    function setAddUnit(u) {
        var wrap = document.getElementById("pt-plan-add-unit_select"), opt = wrap && wrap.querySelector('.gl-v2-select-option[data-value="' + u + '"]');
        if (opt) opt.click();
    }
    function addUnitLabel() { var l = document.querySelector("#pt-plan-add-unit_select .gl-v2-select-trigger-label"); var t = l ? l.textContent.trim() : ""; return /^Algemeen/.test(t) ? "Algemeen" : t; }
    function autoName() {
        if (nameTouched) return;
        var niv = ($("#pt-plan-add-niveau") || {}).value || "";
        $("#pt-plan-add-name").value = niv ? addUnitLabel() + " · " + niv : "";
    }
    function refreshAddInfo() {
        var l = addUnitLabel();
        $("#pt-plan-add-info").textContent = "Het plan wordt toegevoegd aan de plannen van het project en is daarna voor elk punt van " + (l === "Algemeen" || l === "" ? "het project" : l) + " te kiezen. Bestaat er al een plan met dezelfde naam, dan wordt dit een nieuwe versie.";
    }
    function openAdd(fromPick) {
        var am = modalOf("pt-plan-add-modal"); if (!am) return;
        nameTouched = false;
        $("#pt-plan-add-name").value = ""; $("#pt-plan-add-file").value = ""; $("#pt-plan-add-err").hidden = true;
        $("#pt-add-file-card").hidden = true; $("#pt-add-file-pick").hidden = false;
        $("#pt-plan-add-modal").dataset.fromPick = fromPick ? "1" : "";
        $("#pt-add-back").hidden = !fromPick;
        setAddUnit(String(currentUnitId()));
        var pm = document.getElementById("pt-plan-pick-modal");
        if (pm && pm.classList.contains("show")) { var pmi = modalOf("pt-plan-pick-modal"); if (pmi) pmi.hide(); }
        setTimeout(function () { refreshAddInfo(); am.show(); }, pm && pm.classList.contains("show") ? 300 : 0);
    }
    function fmtMb(bytes) { return (bytes / 1048576).toFixed(1).replace(".", ",") + " MB"; }
    function paperOf(wPt, hPt) {
        var w = wPt * 25.4 / 72, h = hPt * 25.4 / 72, s = [Math.min(w, h), Math.max(w, h)];
        var sizes = [["A0", 841, 1189], ["A1", 594, 841], ["A2", 420, 594], ["A3", 297, 420], ["A4", 210, 297]];
        for (var i = 0; i < sizes.length; i++) if (Math.abs(s[0] - sizes[i][1]) < 12 && Math.abs(s[1] - sizes[i][2]) < 16) return sizes[i][0];
        return Math.round(s[0]) + "×" + Math.round(s[1]) + " mm";
    }
    function showFile(file) {
        $("#pt-add-file-pick").hidden = true; $("#pt-add-file-card").hidden = false;
        $("#pt-add-file-name").textContent = file.name;
        $("#pt-add-file-meta").textContent = fmtMb(file.size);
        var isPdf = /\.pdf$/i.test(file.name), lib = window.pdfjsLib || window["pdfjs-dist/build/pdf"];
        if (isPdf && lib) {
            lib.GlobalWorkerOptions.workerSrc = "https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.worker.min.js";
            file.arrayBuffer().then(function (buf) { return lib.getDocument({ data: buf }).promise; }).then(function (doc) {
                return doc.getPage(1).then(function (pg) { var vp = pg.getViewport({ scale: 1 }); $("#pt-add-file-meta").textContent = fmtMb(file.size) + " · " + doc.numPages + (doc.numPages === 1 ? " pagina" : " pagina's") + " · " + paperOf(vp.width, vp.height); });
            }).catch(function () { });
        } else if (!isPdf) {
            var url = URL.createObjectURL(file), img = new Image();
            img.onload = function () { $("#pt-add-file-meta").textContent = fmtMb(file.size) + " · afbeelding · " + img.naturalWidth + "×" + img.naturalHeight + " px"; URL.revokeObjectURL(url); };
            img.src = url;
        }
    }
    function submitAdd(ev) {
        ev.preventDefault();
        var f = $("#pt-plan-add-form"), file = $("#pt-plan-add-file"), err = $("#pt-plan-add-err"); err.hidden = true;
        if (!$("#pt-plan-add-name").value.trim()) { err.textContent = "Geef het plan een naam."; err.hidden = false; $("#pt-plan-add-name").focus(); return; }
        if (!file.files.length) { err.textContent = "Kies een bestand (PDF, PNG of JPG)."; err.hidden = false; return; }
        var btn = $("#pt-plan-add-submit"); btn.disabled = true;
        fetch(f.action || uploadPlanUrl, { method: "POST", body: new FormData(f), credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
            if (!j.ok) { err.textContent = j.error || "Uploaden mislukt."; err.hidden = false; return; }
            var chosenUnit = val("pt-plan-add-unit") || "0";
            // het punt volgt de eenheid van het plan, zodat het plan bij dit punt hoort
            if (chosenUnit !== "0" && chosenUnit !== String(currentUnitId())) setSelect("pt-new-unit", chosenUnit);
            plans = null;
            return loadPlans().then(function () {
                ["pt-plan-page", "pt-plan-x", "pt-plan-y"].forEach(function (i) { $("#" + i).value = ""; });
                showPlan(j.key, null);
                var am = modalOf("pt-plan-add-modal"); if (am) am.hide();
                if (planOnlyCb) setTimeout(openBig, 320);
            });
        }).catch(function () { err.textContent = "Uploaden mislukt."; err.hidden = false; }).then(function () { btn.disabled = false; });
    }

    // ── paneel ──
    function resetPanel() {
        ["pt-new-title", "pt-new-zone", "pt-new-desc"].forEach(function (i) { $("#" + i).value = ""; });
        $("#pt-new-files").value = ""; $("#pt-new-files-label").textContent = "Foto's toevoegen"; $("#pt-new-saved").hidden = true;
        renderNewThumbs();
        renderPhotos([], 0);
        var ap = $("#pt-new-approve"); if (ap) ap.checked = false;
        setDate("pt-new-due", "");
        setSelect("pt-new-unit", "0"); setContractor(""); setSelect("pt-new-priority", "1"); setSelect("pt-new-type", "0"); setSelect("pt-new-phase", "0");
        clearPlan();
    }
    function setMode(m, nr) {
        mode = m;
        var title = $("#pt-panel-title") || document.createElement("span"), sub = $("#pt-panel-sub") || document.createElement("span"), note = $("#pt-panel-note") || document.createElement("span");
        var next = $("#pt-new-save-next"), save = $("#pt-new-save"), approveRow = $("#pt-approve-row");
        if (approveRow) approveRow.hidden = m === "edit";
        if (m === "edit") {
            title.textContent = "Punt bewerken"; sub.textContent = nr || ""; next.hidden = true; save.className = "gl-v2-btn gl-v2-btn-primary";
            note.textContent = "Status en historiek blijven ongewijzigd.";
        } else {
            title.textContent = m === "dup" ? "Punt dupliceren" : "Nieuw punt";
            sub.textContent = m === "dup" ? "Foto's worden niet meegekopieerd" : "Enter = opslaan & volgende";
            next.hidden = false; save.className = "gl-v2-btn gl-v2-btn-secondary";
            note.innerHTML = "Zonder goedkeuring bewaard als <b>concept</b>; pas na goedkeuring gaat het naar de aannemer.";
        }
    }
    // bestaande foto's van het punt (bewerken): miniaturen met verwijderknop
    // eigen bevestigingsmodal voor "foto verwijderen" (geen browserdialoog); de bevestigingsknop voert de meegegeven actie uit
    function askDeletePhoto(run) {
        var m = document.getElementById("pt-photo-delete-modal"), bm = modalOf("pt-photo-delete-modal");
        if (!m || !bm) { if (window.confirm("Deze foto verwijderen?")) run(function () { }); return; }
        var btn = document.getElementById("pt-photo-delete-confirm"), fresh = btn.cloneNode(true);   // oude klikhandlers van een vorige foto weg
        btn.parentNode.replaceChild(fresh, btn);
        fresh.disabled = false;
        fresh.addEventListener("click", function () { fresh.disabled = true; run(function () { bm.hide(); }); });
        bm.show();
    }
    function renderPhotos(list, issueId) {
        var box = $("#pt-existing-photos"); if (!box) return;
        box.innerHTML = ""; box.hidden = !list || list.length === 0;
        (list || []).forEach(function (m) {
            var w = document.createElement("span"); w.className = "gl-v2-pt-photo";
            w.innerHTML = '<a href="' + esc(m.url) + '" target="_blank" rel="noopener"><img src="' + esc(m.url) + '" alt="" loading="lazy"></a><button type="button" aria-label="Foto verwijderen"><i class="ph ph-x" aria-hidden="true"></i></button>';
            w.querySelector("button").addEventListener("click", function () {
                askDeletePhoto(function (done) {
                    var fd = new FormData(); fd.append("__RequestVerificationToken", (form.querySelector("input[name=__RequestVerificationToken]") || {}).value || "");
                    fetch(deleteMediaUrl.replace(/\/0\/0$/, "/" + issueId + "/" + m.id), { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
                        if (j.ok) { w.remove(); savedCount++; box.hidden = !box.children.length; }
                    }).then(done, done);
                });
            });
            box.appendChild(w);
        });
    }
    function fillFrom(d) {
        renderPhotos(d.media, d.id);
        $("#pt-new-title").value = d.title || ""; $("#pt-new-zone").value = d.roomOrZone || ""; $("#pt-new-desc").value = d.description || "";
        setSelect("pt-new-unit", String(d.unitId || 0)); setContractor(d.responsiblePartyId ? String(d.responsiblePartyId) : "");
        setSelect("pt-new-cat", String(d.categoryId)); setSelect("pt-new-type", String(d.issueType)); setSelect("pt-new-phase", String(d.issuePhase)); setSelect("pt-new-priority", String(d.priority));
        setDate("pt-new-due", d.dueDate || "");
    }
    // gekozen (nog niet bewaarde) foto's als miniaturen, met de mogelijkheid er één weg te halen
    var thumbUrls = [];
    function renderNewThumbs() {
        var box = $("#pt-new-thumbs"), inp = $("#pt-new-files"); if (!box || !inp) return;
        thumbUrls.forEach(function (u) { URL.revokeObjectURL(u); }); thumbUrls = [];
        var files = Array.prototype.slice.call(inp.files || []);
        box.innerHTML = ""; box.hidden = files.length === 0;
        files.forEach(function (f, i) {
            var url = URL.createObjectURL(f); thumbUrls.push(url);
            var w = document.createElement("span"); w.className = "gl-v2-pt-photo";
            w.innerHTML = '<img src="' + url + '" alt="Foto ' + (i + 1) + '"><button type="button" aria-label="Foto ' + (i + 1) + ' weghalen"><i class="ph ph-x" aria-hidden="true"></i></button>';
            w.querySelector("button").addEventListener("click", function () { setFiles(files.filter(function (x, j) { return j !== i; })); });
            box.appendChild(w);
        });
    }
    function setFiles(list) {
        var inp = $("#pt-new-files"); if (!inp) return;
        if (window.DataTransfer) { var dt = new DataTransfer(); list.forEach(function (f) { dt.items.add(f); }); inp.files = dt.files; }
        var n = list.length; $("#pt-new-files-label").textContent = n ? n + (n === 1 ? " foto gekozen" : " foto's gekozen") : "Foto's toevoegen";
        renderNewThumbs();
    }
    function show() {
        panelEl.hidden = false; backdrop.hidden = false;
        setTimeout(function () { $("#pt-new-title").focus(); }, 30);
    }
    function close() {
        hook = null;
        panelEl.hidden = true; backdrop.hidden = true;
        if (savedCount > 0) window.location.reload();
    }

    function open(m, id, preset) {
        resetPanel(); setMode(m);
        form.action = m === "edit" ? editUrl.replace(/\/0$/, "/" + id) : createUrl;
        preset = preset || {};
        hook = preset.onSaved || null;
        show();
        if (preset.files && preset.files.length) setFiles(preset.files);   // foto-eerst-flow (gsm): de genomen foto's zitten al in het formulier
        if (preset.zone) $("#pt-new-zone").value = preset.zone;
        loadPlans().then(function () {
            if (preset.unitId != null) setSelect("pt-new-unit", String(preset.unitId));
            if (preset.title) $("#pt-new-title").value = preset.title;
            if (preset.description) $("#pt-new-desc").value = preset.description;
            if (preset.contractorId) setContractor(String(preset.contractorId));
            if (preset.priority != null) setSelect("pt-new-priority", String(preset.priority));
            renderPlanState();
            if (preset.planKey) {
                var marker = preset.x != null ? { page: preset.page || 1, x: preset.x, y: preset.y } : null;
                if (marker) { $("#pt-plan-page").value = marker.page; $("#pt-plan-x").value = marker.x; $("#pt-plan-y").value = marker.y; }
                showPlan(preset.planKey, marker);
            }
            if (!id) return;
            return fetch(editDataUrl.replace(/\/0$/, "/" + id), { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (d) {
                fillFrom(d);
                if (m === "edit") {
                    var row = document.querySelector('[data-id="' + id + '"][data-nr]');
                    setMode("edit", row && row.dataset.nr ? "P-" + ("00" + row.dataset.nr).slice(-3) : "");
                }
                if (window.GlV2DatePicker && window.GlV2DatePicker.init) window.GlV2DatePicker.init();
                renderPlanState();
                if (m === "edit" && d.planDocumentId != null) {
                    var pl = (plans || []).filter(function (x) { return x.planId === d.planDocumentId; })[0], key = pl ? pl.key : "";
                    var mk = d.planXnormalized != null ? { page: d.planPageNumber || 1, x: Number(d.planXnormalized), y: Number(d.planYnormalized) } : null;
                    if (mk) { $("#pt-plan-page").value = mk.page; $("#pt-plan-x").value = mk.x; $("#pt-plan-y").value = mk.y; }
                    if (key) showPlan(key, mk);
                }
            });
        });
    }

    function save(next) {
        if (saving) return;
        var title = $("#pt-new-title"), help = $("#pt-new-title-help"), fieldEl = title.closest(".gl-v2-field");
        if (!title.value.trim()) { fieldEl.classList.add("is-error"); help.textContent = "Geef een titel in."; title.focus(); return; }
        fieldEl.classList.remove("is-error");
        saving = true;
        [$("#pt-new-save"), $("#pt-new-save-next")].forEach(function (b) { b.disabled = true; });
        fetch(form.action, { method: "POST", body: new FormData(form), credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (j) {
                if (!j.ok) { help.textContent = j.error || "Opslaan mislukt."; fieldEl.classList.add("is-error"); return; }
                savedCount++;
                // onthouden voor de volgende keer (eenheid + zone) en een korte bevestiging na het herladen
                if (window.PuntLast && mode !== "edit") window.PuntLast.set({ unitId: currentUnitId(), unitName: currentUnitId() ? currentUnitName() : "Algemeen", zone: val("pt-new-zone") });
                if ((!next || mode === "edit") && !hook) try { var ap = $("#pt-new-approve"); sessionStorage.setItem("pt-toast", (j.nr || "Punt") + (mode === "edit" ? " bijgewerkt" : (ap && ap.checked ? " bewaard en goedgekeurd" : " bewaard als concept"))); } catch (e) { /* geen sessionStorage */ }
                if (j.warning) { help.textContent = j.warning; fieldEl.classList.add("is-error"); return; }   // het paneel blijft open zodat de melding gelezen wordt
                if (hook) { var h = hook; hook = null; savedCount = 0; close(); h(j); return; }
                if (!next || mode === "edit") { close(); return; }
                $("#pt-new-title").value = ""; $("#pt-new-desc").value = ""; $("#pt-new-files").value = ""; $("#pt-new-files-label").textContent = "Foto's toevoegen"; renderNewThumbs();
                clearPlan();
                help.textContent = "kort en concreet — wat moet er gebeuren";
                var note = $("#pt-new-saved"); note.hidden = false; $("span", note).textContent = j.nr + " bewaard als concept. Eenheid, zone en aannemer blijven staan.";
                $("#pt-new-title").focus();
            })
            .catch(function () { help.textContent = "Opslaan mislukt. Controleer je verbinding."; fieldEl.classList.add("is-error"); })
            .then(function () { saving = false; [$("#pt-new-save"), $("#pt-new-save-next")].forEach(function (b) { b.disabled = false; }); });
    }

    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-pt-new")) { open("new", 0); return; }
        var ed = e.target.closest(".js-pt-edit"); if (ed) { open("edit", ed.dataset.id); return; }
        var du = e.target.closest(".js-pt-dup"); if (du) { open("dup", du.dataset.id); return; }
        if (e.target.closest(".js-pt-panel-close") || e.target === backdrop) { close(); return; }
        var z = e.target.closest(".js-pt-zone"); if (z) { $("#pt-new-zone").value = z.dataset.zone; return; }
        if (e.target.closest("#pt-plan-remove")) { clearPlan(); $("#pt-plan-box").hidden = false; }
        if (e.target.closest(".js-pt-plan-pick")) { openPick(); return; }
        if (e.target.closest(".js-pt-plan-new")) { openAdd(false); return; }
        if (e.target.closest("#pt-plan-big-ok")) { confirmBig(); return; }
        if (e.target.closest("#pt-pick-ok")) { confirmPick(); return; }
        if (e.target.closest("#pt-pick-newplan")) { openAdd(true); return; }
        if (e.target.closest("#pt-add-back")) { var am0 = modalOf("pt-plan-add-modal"); if (am0) am0.hide(); setTimeout(openPick, 300); return; }
        var pi = e.target.closest(".gl-v2-pt-pick-item"); if (pi) { pickKey = pi.dataset.key; renderPick(); if (window.matchMedia("(max-width: 767.98px)").matches) confirmPick(); return; }   // gsm (40s): een tik kiest meteen
        var ps = e.target.closest("#pt-pick-seg button"); if (ps) { pickSeg = ps.dataset.seg; $$("#pt-pick-seg button").forEach(function (b) { b.classList.toggle("is-active", b === ps); }); renderPick(); return; }
        if (e.target.closest("#pt-add-file-btn") || e.target.closest("#pt-add-file-pick")) { $("#pt-plan-add-file").click(); return; }
    });
    $("#pt-new-save").addEventListener("click", function () { save(false); });
    $("#pt-new-save-next").addEventListener("click", function () { save(true); });
    $("#pt-new-title").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); save(true); } });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && !panelEl.hidden && !document.body.classList.contains("pt-over") && !document.querySelector(".gl-v2-select-panel.is-open")) close(); });

    var more = $("#pt-new-more");
    more.addEventListener("click", function () {
        var o = more.getAttribute("aria-expanded") !== "true";
        more.setAttribute("aria-expanded", o ? "true" : "false"); $("#pt-new-more-body").hidden = !o;
    });
    $("#pt-new-files").addEventListener("change", function (e) {
        var n = e.target.files.length; $("#pt-new-files-label").textContent = n ? n + (n === 1 ? " foto gekozen" : " foto's gekozen") : "Foto's toevoegen";
        renderNewThumbs();
    });
    // het plan volgt de eenheid: hoort het gekozen plan bij een andere eenheid, dan vervalt het
    document.getElementById("pt-new-unit").addEventListener("change", function () {
        var p = planByKey(currentPlanKey);
        if (p && p.unitId != null && p.unitId !== currentUnitId()) clearPlan(); else renderPlanState();
    });
    var pickSearch = $("#pt-pick-search"); if (pickSearch) pickSearch.addEventListener("input", renderPick);
    var addForm = document.getElementById("pt-plan-add-form"); if (addForm) {
        addForm.addEventListener("submit", submitAdd);
        $("#pt-plan-add-file").addEventListener("change", function (e) { if (e.target.files.length) showFile(e.target.files[0]); });
        $("#pt-plan-add-name").addEventListener("input", function () { nameTouched = true; });
        $("#pt-plan-add-niveau").addEventListener("change", autoName);
        $("#pt-plan-add-unit").addEventListener("change", function () { autoName(); refreshAddInfo(); });
    }
    ["pt-plan-add-modal", "pt-plan-big", "pt-plan-pick-modal", "pt-photo-delete-modal"].forEach(function (id) { var m = document.getElementById(id); if (m) onShowOver(m); });

    // Enkel een plan kiezen en een punt aanduiden voor een voorstel (Inspreken/Rondgang): toont de plannen van de eenheid (en de algemene); is er precies één plan van
    // de eenheid zelf, dan opent dat meteen. cb({planId,page,x,y,name}) bij gelukt, cb(null, melding) als er geen plan is, cb(null) als de gebruiker annuleert.
    function pickPlan(preset, cb) {
        preset = preset || {};
        resetPanel(); setMode("new"); hook = null; planOnlyCb = cb;
        loadPlans().then(function () {
            if (preset.unitId != null) setSelect("pt-new-unit", String(preset.unitId));
            if (preset.title) $("#pt-new-title").value = preset.title;
            var list = unitPlans();
            if (!list.length) { planOnlyCb = null; cb(null, "Er zijn nog geen plannen voor " + (currentUnitId() === 0 ? "dit project" : currentUnitName()) + ". Voeg er een toe via Op plan."); return; }
            var own = list.filter(function (x) { return x.unitId != null; });
            if (own.length === 1 || list.length === 1) { pickKey = (own.length === 1 ? own[0] : list[0]).key; confirmPick(); return; }
            openPick();
        });
    }
    // geannuleerd (lijst of groot plan gesloten zonder te plaatsen)
    ["pt-plan-pick-modal", "pt-plan-big"].forEach(function (id) {
        var m = document.getElementById(id);
        if (m) m.addEventListener("hidden.bs.modal", function () {
            if (!planOnlyCb) return;
            setTimeout(function () {
                var open = document.querySelector("#pt-plan-pick-modal.show, #pt-plan-big.show, #pt-plan-add-modal.show");
                if (planOnlyCb && !open) { var cb = planOnlyCb; planOnlyCb = null; cb(null); }
            }, 450);
        });
    });

    window.PuntPanel = { open: open, pickPlan: pickPlan };
})();
