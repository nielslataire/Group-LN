// gl-v2 — Punten op gsm (design-handoff 40/41, fase 5): kaartlijst, snelactie "Foto" (camera + markeren, 41b), eenheid-chips in het
// nieuw-puntpaneel (41a stap 2), "laatst gebruikt" per project en een korte bevestiging na het bewaren.
// Het nieuw-puntpaneel zelf zit in gl-v2-punt-panel.js (window.PuntPanel); dit bestand voegt er enkel de gsm-laag aan toe.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-pt-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }
    var phone = window.matchMedia("(max-width: 767.98px)");
    var canWrite = root.dataset.canWrite === "1";

    // ── laatst gebruikt (eenheid + zone), per project ──
    var LS = "gl-v2-pt-last-" + (root.dataset.projectId || "0");
    function lastGet() { try { return JSON.parse(localStorage.getItem(LS) || "{}") || {}; } catch (e) { return {}; } }
    function lastSet(o) { try { var l = lastGet(); Object.keys(o).forEach(function (k) { l[k] = o[k]; }); localStorage.setItem(LS, JSON.stringify(l)); } catch (e) { /* privé-modus: niet erg */ } }
    window.PuntLast = { get: lastGet, set: lastSet };

    // ── korte bevestiging na het bewaren (de pagina herlaadt na opslaan) ──
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-pt-toast"; t.setAttribute("role", "status"); t.textContent = msg;
        document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 3800);
    }
    try { var pending = sessionStorage.getItem("pt-toast"); if (pending) { sessionStorage.removeItem("pt-toast"); toast(pending); } } catch (e) { /* geen sessionStorage */ }

    // ── kaartlijst (gsm): dezelfde gefilterde en gesorteerde rijen als de tabel ──
    var cardsEl = document.getElementById("pt-cards");
    function renderCards() {
        if (!cardsEl) return;
        if (!phone.matches || !window.jQuery || !window.jQuery.fn.dataTable) { cardsEl.innerHTML = ""; return; }
        var dt = window.jQuery("#datatable-punten").DataTable();
        var rows = dt.rows({ search: "applied", order: "current" }).nodes().toArray();
        if (!rows.length) { cardsEl.innerHTML = '<p class="gl-v2-pt-cards-empty">Geen punten gevonden met deze filters.</p>'; return; }
        cardsEl.innerHTML = rows.map(function (tr) {
            var tds = tr.children, title = $(".gl-v2-pt-title-main", tr), badge = $(".gl-v2-badge", tr);
            var sub = [tr.dataset.unitname, (tds[3] ? tds[3].textContent.trim() : ""), (tds[5] ? tds[5].textContent.trim() : "")].filter(function (x) { return x && x !== "—"; }).join(" · ");
            var due = tds[7] ? tds[7].textContent.trim() : "";
            var tags = $$(".gl-v2-pt-tag", tr).map(function (g) { return g.outerHTML; }).join("");
            var thumb = tr.dataset.photo ? '<img src="' + esc(tr.dataset.photo) + '" alt="" loading="lazy">' : '<i class="ph ph-image" aria-hidden="true"></i>';
            return '<a class="gl-v2-pt-cm' + (tr.dataset.closed === "1" ? " is-closed" : "") + '" href="' + esc(tr.dataset.href) + '">' +
                '<span class="gl-v2-pt-cm-thumb">' + thumb + '</span>' +
                '<span class="gl-v2-stack is-loose gl-v2-pt-cm-body"><b>' + esc(title ? title.textContent : "") + '</b><small>' + esc(sub) + '</small>' +
                '<span class="gl-v2-pt-cm-meta">' + (badge ? badge.outerHTML : "") + tags +
                (due && due !== "—" ? '<span class="gl-v2-pt-cm-due' + (tr.dataset.overdue === "1" ? " is-overdue" : "") + '">' + esc(due) + '</span>' : "") + '</span></span>' +
                '<i class="ph ph-caret-right gl-v2-pt-cm-chev" aria-hidden="true"></i></a>';
        }).join("");
    }
    if (window.jQuery) window.jQuery("#datatable-punten").on("draw.dt", renderCards);
    if (phone.addEventListener) phone.addEventListener("change", renderCards);
    renderCards();

    // ── snelactie Afdrukken (de topbarknop is op gsm verborgen) ──
    document.addEventListener("click", function (e) {
        if (e.target.closest("#pt-print-m")) { var b = document.getElementById("pt-print"); if (b) b.click(); }
    });

    // ── eenheid-chips in het paneel (41a stap 2): laatst gebruikte eerst, "+ ander" opent de gewone keuzelijst ──
    var unitHidden = document.getElementById("pt-new-unit"), unitWrap = document.getElementById("pt-new-unit_select");
    if (unitHidden && unitWrap) {
        var field = unitWrap.closest(".gl-v2-field"), chipBox = document.createElement("div");
        chipBox.className = "gl-v2-pt-unitchips"; chipBox.id = "pt-unit-chips";
        field.insertBefore(chipBox, unitWrap);
        var allUnits = $$(".gl-v2-select-option", unitWrap).map(function (o) { return { v: o.dataset.value, t: (o.querySelector("span") || o).textContent.trim() }; });
        var renderChips = function () {
            var cur = unitHidden.value || "0", lastU = String(lastGet().unitId == null ? "" : lastGet().unitId);
            var order = ["0"]; [cur, lastU].forEach(function (v) { if (v && order.indexOf(v) < 0 && allUnits.some(function (u) { return u.v === v; })) order.push(v); });
            allUnits.forEach(function (u) { if (order.length < 4 && order.indexOf(u.v) < 0) order.push(u.v); });
            chipBox.innerHTML = order.map(function (v) {
                var u = allUnits.filter(function (x) { return x.v === v; })[0]; if (!u) return "";
                return '<button type="button" class="gl-v2-pt-unitchip' + (v === cur ? " is-on" : "") + '" data-v="' + esc(v) + '">' + esc(u.t) + '</button>';
            }).join("") + (allUnits.length > order.length ? '<button type="button" class="gl-v2-pt-unitchip is-more" data-more="1">+ ander</button>' : "");
        };
        chipBox.addEventListener("click", function (e) {
            var b = e.target.closest("button"); if (!b) return;
            if (b.dataset.more) { field.classList.add("show-native"); var tr = $(".gl-v2-select-trigger", unitWrap); if (tr) tr.click(); return; }
            var opt = unitWrap.querySelector('.gl-v2-select-option[data-value="' + b.dataset.v + '"]'); if (opt) opt.click();
        });
        unitHidden.addEventListener("change", renderChips);
        renderChips();
    }

    // ── Foto (41b): camera met meerdere opnames, daarna optioneel markeren, daarna het gewone formulier ──
    var cam = null, stream = null, shots = [], cameraCb = null, cameraLabel = "";
    function whereLabel() {
        if (cameraLabel) return cameraLabel;
        var l = lastGet(); return [l.unitName, l.zone].filter(Boolean).join(" · ") || "Nieuw punt";
    }
    function buildCam() {
        cam = document.createElement("div"); cam.className = "gl-v2-ptcam"; cam.id = "pt-cam"; cam.hidden = true; cam.setAttribute("role", "dialog"); cam.setAttribute("aria-modal", "true"); cam.setAttribute("aria-label", "Foto's nemen");
        cam.innerHTML = '<div class="gl-v2-ptcam-top"><button type="button" class="gl-v2-ptcam-btn" data-act="close" aria-label="Sluiten"><i class="ph ph-x" aria-hidden="true"></i></button>' +
            '<span class="gl-v2-ptcam-where"></span><button type="button" class="gl-v2-ptcam-btn" data-act="flash" aria-label="Flits" hidden><i class="ph ph-lightning" aria-hidden="true"></i></button></div>' +
            '<video class="gl-v2-ptcam-video" playsinline muted autoplay></video><div class="gl-v2-ptcam-flash" aria-hidden="true"></div>' +
            '<p class="gl-v2-ptcam-err" hidden>De camera is niet beschikbaar. Kies foto\'s uit de galerij.</p>' +
            '<div class="gl-v2-ptcam-bottom"><div class="gl-v2-ptcam-thumbs"></div><div class="gl-v2-ptcam-row">' +
            '<label class="gl-v2-ptcam-side"><i class="ph ph-images" aria-hidden="true"></i><span>Galerij</span><input type="file" accept="image/*" multiple hidden></label>' +
            '<button type="button" class="gl-v2-ptcam-shutter" data-act="shot" aria-label="Foto nemen"></button>' +
            '<button type="button" class="gl-v2-ptcam-side is-done" data-act="done"><i class="ph ph-check" aria-hidden="true"></i><span>Klaar</span></button></div></div>';
        document.body.appendChild(cam);
        cam.addEventListener("click", function (e) {
            var b = e.target.closest("[data-act]"); if (!b) return;
            var a = b.dataset.act;
            if (a === "close") closeCam(true);
            else if (a === "shot") shoot();
            else if (a === "done") doneCam();
            else if (a === "flash") toggleTorch(b);
            else if (a === "del") { shots.splice(parseInt(b.dataset.i, 10), 1); renderShots(); }
        });
        $("input[type=file]", cam).addEventListener("change", function (e) {
            Array.prototype.forEach.call(e.target.files, function (f) { addShot(f); });
            e.target.value = "";
        });
    }
    function renderShots() {
        var box = $(".gl-v2-ptcam-thumbs", cam);
        box.innerHTML = shots.map(function (s, i) {
            return '<span class="gl-v2-ptcam-thumb"><img src="' + s.url + '" alt="Foto ' + (i + 1) + '"><button type="button" data-act="del" data-i="' + i + '" aria-label="Foto ' + (i + 1) + ' verwijderen"><i class="ph ph-x" aria-hidden="true"></i></button></span>';
        }).join("");
        $(".is-done span", cam).textContent = shots.length ? "Klaar (" + shots.length + ")" : "Klaar";
    }
    function addShot(file) { shots.push({ file: file, url: URL.createObjectURL(file), shapes: [] }); renderShots(); }
    function stopStream() { if (stream) { stream.getTracks().forEach(function (t) { t.stop(); }); stream = null; } }
    function openCam() {
        if (!cam) buildCam();
        shots = []; renderShots();
        $(".gl-v2-ptcam-where", cam).textContent = whereLabel();
        $(".gl-v2-ptcam-err", cam).hidden = true;
        $("[data-act=flash]", cam).hidden = true;
        cam.hidden = false; document.body.classList.add("gl-v2-ptcam-on");
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) { $(".gl-v2-ptcam-err", cam).hidden = false; return; }
        navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" }, width: { ideal: 1920 } }, audio: false }).then(function (s) {
            stream = s; var v = $("video", cam); v.srcObject = s; v.play().catch(function () { });
            var tr = s.getVideoTracks()[0], caps = tr && tr.getCapabilities ? tr.getCapabilities() : {};
            if (caps.torch) $("[data-act=flash]", cam).hidden = false;
        }).catch(function () { $(".gl-v2-ptcam-err", cam).hidden = false; });
    }
    function toggleTorch(btn) {
        var tr = stream && stream.getVideoTracks()[0]; if (!tr) return;
        var on = !btn.classList.contains("is-on");
        tr.applyConstraints({ advanced: [{ torch: on }] }).then(function () { btn.classList.toggle("is-on", on); }).catch(function () { });
    }
    function shoot() {
        var v = $("video", cam); if (!v || !v.videoWidth) return;
        var scale = Math.min(1, 1800 / Math.max(v.videoWidth, v.videoHeight)), c = document.createElement("canvas");
        c.width = Math.round(v.videoWidth * scale); c.height = Math.round(v.videoHeight * scale);
        c.getContext("2d").drawImage(v, 0, 0, c.width, c.height);
        var fl = $(".gl-v2-ptcam-flash", cam); fl.classList.remove("is-on"); void fl.offsetWidth; fl.classList.add("is-on");
        c.toBlob(function (b) { if (b) addShot(new File([b], "foto-" + Date.now() + ".jpg", { type: "image/jpeg" })); }, "image/jpeg", 0.86);
    }
    function closeCam(discard) {
        stopStream(); cam.hidden = true; if (!document.querySelector(".gl-v2-ptv:not([hidden])")) document.body.classList.remove("gl-v2-ptcam-on");
        if (discard) { cameraCb = null; cameraLabel = ""; }
        if (discard) { shots.forEach(function (s) { URL.revokeObjectURL(s.url); }); shots = []; }
    }
    function doneCam() {
        if (!shots.length) { closeCam(true); return; }
        stopStream(); cam.hidden = true; openMark();
    }

    // ── markeren (41b stap 3): cirkel, pijl, tekst — optioneel ──
    var mark = null, mIdx = 0, mTool = "circle", drag = null, mImgs = [];
    var INK = "#D64545";
    function buildMark() {
        mark = document.createElement("div"); mark.className = "gl-v2-ptmark"; mark.hidden = true; mark.setAttribute("role", "dialog"); mark.setAttribute("aria-modal", "true"); mark.setAttribute("aria-label", "Foto markeren");
        mark.innerHTML = '<div class="gl-v2-ptmark-top"><div><b>Foto markeren</b><small>optioneel, tik Klaar om over te slaan</small></div><span class="gl-v2-ptmark-count"></span></div>' +
            '<div class="gl-v2-ptmark-stage"><canvas></canvas><button type="button" class="gl-v2-ptmark-nav is-prev" data-act="prev" aria-label="Vorige foto"><i class="ph ph-caret-left" aria-hidden="true"></i></button>' +
            '<button type="button" class="gl-v2-ptmark-nav is-next" data-act="next" aria-label="Volgende foto"><i class="ph ph-caret-right" aria-hidden="true"></i></button></div>' +
            '<form class="gl-v2-ptmark-text" hidden><input type="text" maxlength="60" placeholder="Typ een korte tekst" aria-label="Tekst op de foto"><button type="submit" class="gl-v2-btn gl-v2-btn-primary">OK</button></form>' +
            '<div class="gl-v2-ptmark-tools"><button type="button" data-tool="circle" class="is-on"><i class="ph ph-circle" aria-hidden="true"></i>Cirkel</button><button type="button" data-tool="arrow"><i class="ph ph-arrow-up-right" aria-hidden="true"></i>Pijl</button>' +
            '<button type="button" data-tool="text"><i class="ph ph-text-aa" aria-hidden="true"></i>Tekst</button><button type="button" data-act="clear"><i class="ph ph-eraser" aria-hidden="true"></i>Wis</button>' +
            '<button type="button" data-act="undo"><i class="ph ph-arrow-counter-clockwise" aria-hidden="true"></i>Terug</button></div>' +
            '<div class="gl-v2-ptmark-foot"><button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="ok"><i class="ph ph-check" aria-hidden="true"></i>Klaar</button></div>';
        document.body.appendChild(mark);
        var cv = $("canvas", mark);
        mark.addEventListener("click", function (e) {
            var t = e.target.closest("[data-tool]"); if (t) { mTool = t.dataset.tool; $$("[data-tool]", mark).forEach(function (b) { b.classList.toggle("is-on", b === t); }); return; }
            var b = e.target.closest("[data-act]"); if (!b) return;
            var a = b.dataset.act, s = shots[mIdx];
            if (a === "prev" && mIdx > 0) { mIdx--; drawMark(); }
            else if (a === "next" && mIdx < shots.length - 1) { mIdx++; drawMark(); }
            else if (a === "clear") { s.shapes = []; drawMark(); }
            else if (a === "undo") { s.shapes.pop(); drawMark(); }
            else if (a === "ok") finishMark();
        });
        function pos(ev) { var r = cv.getBoundingClientRect(); return { x: Math.min(1, Math.max(0, (ev.clientX - r.left) / r.width)), y: Math.min(1, Math.max(0, (ev.clientY - r.top) / r.height)) }; }
        cv.addEventListener("pointerdown", function (ev) {
            ev.preventDefault(); var p = pos(ev);
            if (mTool === "text") { askText(p); return; }
            drag = { type: mTool, x1: p.x, y1: p.y, x2: p.x, y2: p.y }; cv.setPointerCapture(ev.pointerId);
        });
        cv.addEventListener("pointermove", function (ev) { if (!drag) return; var p = pos(ev); drag.x2 = p.x; drag.y2 = p.y; drawMark(); });
        cv.addEventListener("pointerup", function () {
            if (!drag) return;
            var moved = Math.abs(drag.x2 - drag.x1) + Math.abs(drag.y2 - drag.y1) > 0.02;
            if (moved) shots[mIdx].shapes.push(drag);
            else if (drag.type === "circle") shots[mIdx].shapes.push({ type: "circle", x1: drag.x1 - 0.06, y1: drag.y1 - 0.06, x2: drag.x1 + 0.06, y2: drag.y1 + 0.06 });   // een tik = een cirkel op die plek
            drag = null; drawMark();
        });
        $(".gl-v2-ptmark-text", mark).addEventListener("submit", function (ev) {
            ev.preventDefault(); var inp = $("input", ev.target), at = ev.target._at;
            if (inp.value.trim() && at) shots[mIdx].shapes.push({ type: "text", x1: at.x, y1: at.y, text: inp.value.trim() });
            inp.value = ""; ev.target.hidden = true; drawMark();
        });
    }
    function askText(p) { var f = $(".gl-v2-ptmark-text", mark); f._at = p; f.hidden = false; $("input", f).focus(); }
    function paint(ctx, w, h, shapes) {
        var lw = Math.max(4, Math.round(Math.max(w, h) / 220));
        ctx.lineCap = "round"; ctx.lineJoin = "round";
        shapes.forEach(function (s) {
            var x1 = s.x1 * w, y1 = s.y1 * h, x2 = (s.x2 == null ? s.x1 : s.x2) * w, y2 = (s.y2 == null ? s.y1 : s.y2) * h;
            [["#ffffff", lw * 2.2], [INK, lw]].forEach(function (st) {
                ctx.strokeStyle = st[0]; ctx.fillStyle = st[0]; ctx.lineWidth = st[1];
                if (s.type === "circle") { ctx.beginPath(); ctx.ellipse((x1 + x2) / 2, (y1 + y2) / 2, Math.max(2, Math.abs(x2 - x1) / 2), Math.max(2, Math.abs(y2 - y1) / 2), 0, 0, Math.PI * 2); ctx.stroke(); }
                else if (s.type === "arrow") {
                    var a = Math.atan2(y2 - y1, x2 - x1), hd = lw * 5;
                    ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();
                    ctx.beginPath(); ctx.moveTo(x2, y2); ctx.lineTo(x2 - hd * Math.cos(a - 0.45), y2 - hd * Math.sin(a - 0.45)); ctx.moveTo(x2, y2); ctx.lineTo(x2 - hd * Math.cos(a + 0.45), y2 - hd * Math.sin(a + 0.45)); ctx.stroke();
                } else if (s.type === "text") {
                    ctx.font = "700 " + Math.round(Math.max(w, h) / 22) + "px 'IBM Plex Sans', sans-serif"; ctx.textBaseline = "middle";
                    if (st[0] === "#ffffff") { ctx.lineWidth = lw * 2.4; ctx.strokeText(s.text, x1, y1); } else ctx.fillText(s.text, x1, y1);
                }
            });
        });
    }
    function drawMark() {
        var s = shots[mIdx], im = mImgs[mIdx], cv = $("canvas", mark); if (!s || !im) return;
        var scale = Math.min(1, 1400 / Math.max(im.naturalWidth, im.naturalHeight));
        cv.width = Math.round(im.naturalWidth * scale); cv.height = Math.round(im.naturalHeight * scale);
        var ctx = cv.getContext("2d"); ctx.drawImage(im, 0, 0, cv.width, cv.height);
        paint(ctx, cv.width, cv.height, drag ? s.shapes.concat([drag]) : s.shapes);
        $(".gl-v2-ptmark-count", mark).textContent = "foto " + (mIdx + 1) + " van " + shots.length;
        $(".is-prev", mark).hidden = mIdx === 0; $(".is-next", mark).hidden = mIdx >= shots.length - 1;
    }
    function openMark() {
        if (!mark) buildMark();
        mIdx = 0; mTool = "circle"; $$("[data-tool]", mark).forEach(function (b) { b.classList.toggle("is-on", b.dataset.tool === "circle"); });
        $(".gl-v2-ptmark-text", mark).hidden = true;
        mImgs = []; var loaded = 0;
        shots.forEach(function (s, i) { var im = new Image(); im.onload = function () { mImgs[i] = im; if (++loaded === shots.length) { mark.hidden = false; document.body.classList.add("gl-v2-ptcam-on"); drawMark(); } }; im.src = s.url; });
    }
    function bake(s, im) {
        return new Promise(function (res) {
            if (!s.shapes.length) { res(s.file); return; }
            var scale = Math.min(1, 2000 / Math.max(im.naturalWidth, im.naturalHeight)), c = document.createElement("canvas");
            c.width = Math.round(im.naturalWidth * scale); c.height = Math.round(im.naturalHeight * scale);
            var ctx = c.getContext("2d"); ctx.drawImage(im, 0, 0, c.width, c.height); paint(ctx, c.width, c.height, s.shapes);
            c.toBlob(function (b) { res(b ? new File([b], s.file.name.replace(/\.\w+$/, "") + "-markering.jpg", { type: "image/jpeg" }) : s.file); }, "image/jpeg", 0.88);
        });
    }
    function finishMark() {
        Promise.all(shots.map(function (s, i) { return bake(s, mImgs[i]); })).then(function (files) {
            mark.hidden = true; if (!document.querySelector(".gl-v2-ptv:not([hidden])")) document.body.classList.remove("gl-v2-ptcam-on");
            shots.forEach(function (s) { URL.revokeObjectURL(s.url); }); shots = [];
            // aangeroepen vanuit een voorstel (Inspreken/Rondgang): de foto's teruggeven in plaats van een nieuw punt te starten
            if (cameraCb) { var cb = cameraCb; cameraCb = null; cameraLabel = ""; cb(files); return; }
            var l = lastGet();
            if (window.PuntPanel) window.PuntPanel.open("new", 0, { files: files, unitId: l.unitId != null ? l.unitId : null, zone: l.zone || "" });
        });
    }

    document.addEventListener("click", function (e) {
        if (!canWrite) return;
        if (e.target.closest("#pt-qa-foto")) { cameraCb = null; cameraLabel = ""; openCam(); }
    });
    // andere schermen (Inspreken/Rondgang) openen dezelfde camera: nieuwe foto nemen of uit de galerij kiezen; cb(files) bij Klaar
    window.PuntCamera = { open: function (cb, label) { cameraCb = cb; cameraLabel = label || ""; openCam(); } };
})();
