// gl-v2 — Punten, Op plan (design-handoff 40b/40k): lijst van plannen, het plan met pins in statuskleur, detail van de gekozen pin.
// Klikken op het plan opent het gedeelde zijpaneel (gl-v2-punt-panel.js) met eenheid + plan + locatie al ingevuld.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-pp-root"), dataEl = document.getElementById("pp-data");
    if (!root || !dataEl) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }

    var data = JSON.parse(dataEl.textContent || "{}");
    var plans = data.plans || [], pins = data.pins || [];
    var activeKey = null, activePin = null, currentPage = 1, totalPages = 0, viewer = null;

    function pinsOf(key) { return pins.filter(function (p) { return p.planKey === key; }); }
    function planByKey(k) { return plans.filter(function (p) { return p.key === k; })[0]; }
    function planTitle(p) { return p.unitId == null ? p.name : p.unitName + " · " + p.name; }

    // ── lijst links ──
    function renderList() {
        var q = ($("#pp-search").value || "").trim().toLowerCase(), host = $("#pp-list"), html = "", lastGroup = null, shown = 0;
        plans.forEach(function (p) {
            if (q && (p.name + " " + p.unitName).toLowerCase().indexOf(q) < 0) return;
            var group = p.unitId == null ? "Algemeen" : p.unitName;
            if (group !== lastGroup) { html += '<div class="gl-v2-pp-group">' + esc(group) + "</div>"; lastGroup = group; }
            html += '<button type="button" class="gl-v2-pp-item' + (p.key === activeKey ? " is-active" : "") + '" data-key="' + esc(p.key) + '"><span class="gl-v2-pp-item-name">' + esc(p.name) + '</span><span class="gl-v2-pp-item-count">' + pinsOf(p.key).length + "</span></button>";
            shown++;
        });
        host.innerHTML = shown ? html : '<div class="gl-v2-pp-empty">' + (plans.length ? "Geen plan gevonden." : "Nog geen plannen. Upload een PDF om punten op een plan aan te duiden.") + "</div>";
    }

    // ── midden ──
    function updateHead() {
        var p = planByKey(activeKey);
        $("#pp-title").textContent = p ? planTitle(p) : "Op plan";
        var onPage = pinsOf(activeKey).filter(function (x) { return (x.page || 1) === currentPage; }).length;
        $("#pp-sub").textContent = p ? (totalPages ? "pagina " + currentPage + " van " + totalPages + " · " : "") + onPage + (onPage === 1 ? " punt" : " punten") : "Kies een plan links.";
    }
    function viewerPins() {
        return pinsOf(activeKey).map(function (p) { return { id: p.id, page: p.page, x: p.x, y: p.y, status: p.status, label: p.nr + " · " + p.title, selected: activePin && activePin.id === p.id }; });
    }
    function selectPlan(key, startPage) {
        activeKey = key; activePin = null; currentPage = startPage || 1;
        renderList(); renderDetail();
        var p = planByKey(key);
        if (!viewer) {
            viewer = window.PuntPlanViewer.create($("#pp-viewer"), {
                onPick: function (pin) { pickPin(pin.id); },
                onPlace: data.canWrite ? function (page, x, y) {
                    var pl = planByKey(activeKey); if (!pl || !window.PuntPanel) return;
                    viewer.setMarker({ page: page, x: x, y: y });
                    window.PuntPanel.open("new", 0, { unitId: pl.unitId == null ? 0 : pl.unitId, planKey: pl.key, page: page, x: x, y: y });
                } : null,
                onPage: function (page, total) { currentPage = page; totalPages = total; updateHead(); }
            });
        }
        viewer.setMarker(null);
        if (!p) { viewer.load(null); updateHead(); return; }
        viewer.setPins(viewerPins());
        viewer.load(p.url, currentPage).then(function () { viewer.setPins(viewerPins()); });
        updateHead();
    }

    // ── detail rechts ──
    function pickPin(id) {
        var pin = pins.filter(function (p) { return p.id === id; })[0]; if (!pin) return;
        activePin = pin;
        if (pin.planKey !== activeKey) { selectPlan(pin.planKey, pin.page); activePin = pin; renderDetail(); return; }
        if (viewer) { viewer.setPins(viewerPins()); if ((pin.page || 1) !== currentPage) viewer.goto(pin.page || 1); }
        renderDetail();
    }
    function renderDetail() {
        var box = $("#pp-detail");
        if (!activePin) { box.innerHTML = '<p class="gl-v2-pp-detail-empty">' + (activeKey ? "Klik op een pin om het punt te bekijken." : "Kies een plan om te beginnen.") + "</p>"; return; }
        var p = activePin, html = "";
        html += '<div class="gl-v2-pp-d-head"><span class="gl-v2-pp-d-nr">' + esc(p.nr) + '</span><span class="gl-v2-badge ' + esc(p.statusTone) + '">' + esc(p.statusLabel) + "</span></div>";
        html += '<h3 class="gl-v2-pp-d-title">' + esc(p.title) + "</h3>";
        if (p.photos && p.photos.length) html += '<div class="gl-v2-pp-d-photos">' + p.photos.slice(0, 3).map(function (u) { return '<a href="' + esc(u) + '" target="_blank" rel="noopener"><img src="' + esc(u) + '" alt="" loading="lazy"></a>'; }).join("") + "</div>";
        html += '<dl class="gl-v2-pp-d-dl"><div><dt>Eenheid</dt><dd>' + esc(p.unit) + "</dd></div><div><dt>Kamer / zone</dt><dd>" + esc(p.zone || "—") + "</dd></div><div><dt>Aannemer</dt><dd>" + esc(p.contractor || "—") + "</dd></div><div><dt>Deadline</dt><dd>" + esc(p.due || "—") + "</dd></div></dl>";
        if (p.status === 11) html += '<p class="gl-v2-pp-d-note">Aannemer meldde dit uitgevoerd. Controleer ter plaatse.</p>';
        html += '<div class="gl-v2-pp-d-actions">';
        if (data.canWrite && p.status === 11) html += '<button type="button" class="gl-v2-btn gl-v2-btn-secondary js-pp-status" data-status="10">Heropenen</button><button type="button" class="gl-v2-btn gl-v2-btn-primary js-pp-status" data-status="5">Goedkeuren &amp; afsluiten</button>';
        html += '<a class="gl-v2-btn gl-v2-btn-' + (p.status === 11 ? "text" : "secondary") + '" href="' + esc(p.url) + '">Openen</a></div>';
        box.innerHTML = html;
    }

    // ── events ──
    $("#pp-list").addEventListener("click", function (e) { var b = e.target.closest(".gl-v2-pp-item"); if (b) selectPlan(b.dataset.key, 1); });
    $("#pp-search").addEventListener("input", renderList);
    $("#pp-detail").addEventListener("click", function (e) {
        var b = e.target.closest(".js-pp-status"); if (!b || !activePin) return;
        $("#pp-status-id").value = activePin.id; $("#pp-status-value").value = b.dataset.status; $("#pp-status-form").submit();
    });

    // ── plan uploaden ──
    var upModal = document.getElementById("pp-upload-modal");
    function openUpload() { if (upModal && window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(upModal).show(); }
    ["pp-upload-open", "pp-upload-open-2"].forEach(function (id) { var b = document.getElementById(id); if (b) b.addEventListener("click", openUpload); });
    var upForm = document.getElementById("pp-upload-form");
    if (upForm) upForm.addEventListener("submit", function (e) {
        e.preventDefault();
        var file = $("#pp-upload-file"), err = $("#pp-upload-err"); err.hidden = true;
        if (!file.files.length) { err.textContent = "Kies een PDF-bestand."; err.hidden = false; return; }
        var btn = $("#pp-upload-submit"); btn.disabled = true;
        fetch(upForm.action, { method: "POST", body: new FormData(upForm), credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
            if (!j.ok) { err.textContent = j.error || "Uploaden mislukt."; err.hidden = false; return; }
            window.location.href = window.location.pathname + "?key=" + encodeURIComponent(j.key);
        }).catch(function () { err.textContent = "Uploaden mislukt."; err.hidden = false; }).then(function () { btn.disabled = false; });
    });

    renderList();
    if (data.selectedKey) {
        selectPlan(data.selectedKey, 1);
        if (data.selectedIssueId) pickPin(data.selectedIssueId);
    } else { renderDetail(); updateHead(); }
})();
