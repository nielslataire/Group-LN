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
    var createUrl = form.action, editUrl = panelEl.dataset.editUrl || "", editDataUrl = panelEl.dataset.editDataUrl || "", plansUrl = panelEl.dataset.plansUrl || "";
    var mode = "new", savedCount = 0, saving = false, plans = null, viewer = null;

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
    function val(id) { var e = document.getElementById(id); return e ? e.value : ""; }

    // ── plan-locatie ──
    function loadPlans() {
        if (plans) return Promise.resolve(plans);
        if (!plansUrl) { plans = []; return Promise.resolve(plans); }
        return fetch(plansUrl, { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) { plans = j || []; return plans; }).catch(function () { plans = []; return plans; });
    }
    function plansForUnit() {
        var unit = parseInt(val("pt-new-unit") || "0", 10) || 0;
        return (plans || []).filter(function (p) { return p.unitId == null || p.unitId === unit; });
    }
    function refreshPlanSelect(selectKey) {
        var wrap = document.getElementById("pt-new-plan_select");
        var list = plansForUnit();
        if (window.GlV2Select && wrap) {
            window.GlV2Select.setItems(wrap, list.map(function (p) { return { value: p.key, text: (p.unitId == null ? "Algemeen" : p.unitName) + " · " + p.name }; }), selectKey || "", "Geen plan");
        }
        var help = $("#pt-plan-help");
        if (help) help.textContent = list.length ? "Kies een plan en tik op de plaats van het punt." : "Voor deze eenheid is er nog geen plan. Upload er een via Punten › Op plan.";
        if (selectKey && !list.some(function (p) { return p.key === selectKey; })) clearPlan();
    }
    function planByKey(key) { return (plans || []).filter(function (p) { return p.key === key; })[0]; }
    function clearPlan() {
        ["pt-plan-id", "pt-plan-page", "pt-plan-x", "pt-plan-y"].forEach(function (i) { var e = document.getElementById(i); if (e) e.value = ""; });
        $("#pt-plan-box").hidden = true; $("#pt-plan-remove").hidden = true; $("#pt-plan-hint").textContent = "Tik op het plan om het punt te plaatsen.";
        if (viewer) viewer.setMarker(null);
        $("#pt-plan-clear-flag").value = mode === "edit" ? "true" : "false";
    }
    function showPlan(key, marker) {
        var plan = planByKey(key);
        if (!plan || !window.PuntPlanViewer) { clearPlan(); return; }
        $("#pt-plan-id").value = plan.planId;
        $("#pt-plan-clear-flag").value = "false";
        var box = $("#pt-plan-box"); box.hidden = false;
        if (!viewer) {
            viewer = window.PuntPlanViewer.create($("#pt-plan-viewer"), {
                onPlace: function (page, x, y) {
                    $("#pt-plan-page").value = page; $("#pt-plan-x").value = x; $("#pt-plan-y").value = y;
                    viewer.setMarker({ page: page, x: x, y: y });
                    $("#pt-plan-hint").textContent = "Locatie gekozen · pagina " + page + ". Tik opnieuw om te verplaatsen."; $("#pt-plan-remove").hidden = false;
                }
            });
        }
        viewer.setMarker(marker || null);
        viewer.load(plan.url, marker ? marker.page : 1);
        $("#pt-plan-remove").hidden = !marker;
        $("#pt-plan-hint").textContent = marker ? "Locatie gekozen · pagina " + marker.page + ". Tik opnieuw om te verplaatsen." : "Tik op het plan om het punt te plaatsen.";
    }

    // ── paneel ──
    function resetPanel() {
        ["pt-new-title", "pt-new-zone", "pt-new-desc"].forEach(function (i) { $("#" + i).value = ""; });
        $("#pt-new-files").value = ""; $("#pt-new-files-label").textContent = "Foto's toevoegen"; $("#pt-new-saved").hidden = true;
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
    function fillFrom(d) {
        $("#pt-new-title").value = d.title || ""; $("#pt-new-zone").value = d.roomOrZone || ""; $("#pt-new-desc").value = d.description || "";
        setSelect("pt-new-unit", String(d.unitId || 0)); setContractor(d.responsiblePartyId ? String(d.responsiblePartyId) : "");
        setSelect("pt-new-cat", String(d.categoryId)); setSelect("pt-new-type", String(d.issueType)); setSelect("pt-new-phase", String(d.issuePhase)); setSelect("pt-new-priority", String(d.priority));
        setDate("pt-new-due", d.dueDate || "");
    }
    function show() {
        panelEl.hidden = false; backdrop.hidden = false;
        setTimeout(function () { $("#pt-new-title").focus(); }, 30);
    }
    function close() {
        panelEl.hidden = true; backdrop.hidden = true;
        if (savedCount > 0) window.location.reload();
    }

    function open(m, id, preset) {
        resetPanel(); setMode(m);
        form.action = m === "edit" ? editUrl.replace(/\/0$/, "/" + id) : createUrl;
        preset = preset || {};
        show();
        loadPlans().then(function () {
            if (preset.unitId != null) setSelect("pt-new-unit", String(preset.unitId));
            refreshPlanSelect(preset.planKey || "");
            if (preset.planKey) {
                setSelect("pt-new-plan", preset.planKey);
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
                refreshPlanSelect("");
                if (m === "edit" && d.planDocumentId != null && d.planXnormalized != null) {
                    var key = d.unitId ? "u" + d.unitId + ":" + d.planDocumentId : "a:" + d.planDocumentId;
                    refreshPlanSelect(key); setSelect("pt-new-plan", key);
                    var mk = { page: d.planPageNumber || 1, x: Number(d.planXnormalized), y: Number(d.planYnormalized) };
                    $("#pt-plan-page").value = mk.page; $("#pt-plan-x").value = mk.x; $("#pt-plan-y").value = mk.y;
                    showPlan(key, mk);
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
                if (!next || mode === "edit") { close(); return; }
                $("#pt-new-title").value = ""; $("#pt-new-desc").value = ""; $("#pt-new-files").value = ""; $("#pt-new-files-label").textContent = "Foto's toevoegen";
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
    });
    $("#pt-new-save").addEventListener("click", function () { save(false); });
    $("#pt-new-save-next").addEventListener("click", function () { save(true); });
    $("#pt-new-title").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); save(true); } });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && !panelEl.hidden && !document.querySelector(".gl-v2-select-panel.is-open")) close(); });

    var more = $("#pt-new-more");
    more.addEventListener("click", function () {
        var o = more.getAttribute("aria-expanded") !== "true";
        more.setAttribute("aria-expanded", o ? "true" : "false"); $("#pt-new-more-body").hidden = !o;
    });
    $("#pt-new-files").addEventListener("change", function (e) {
        var n = e.target.files.length; $("#pt-new-files-label").textContent = n ? n + (n === 1 ? " foto gekozen" : " foto's gekozen") : "Foto's toevoegen";
    });
    // plan volgt de eenheid; plan kiezen toont het plan
    document.getElementById("pt-new-unit").addEventListener("change", function () { if (plans) refreshPlanSelect(val("pt-new-plan")); });
    document.getElementById("pt-new-plan").addEventListener("change", function () {
        var key = val("pt-new-plan");
        ["pt-plan-page", "pt-plan-x", "pt-plan-y"].forEach(function (i) { document.getElementById(i).value = ""; });
        if (key) showPlan(key, null); else clearPlan();
    });

    window.PuntPanel = { open: open };
})();
