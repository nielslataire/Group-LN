// gl-v2 — Punten op gsm: laag boven het "Nieuw plan"-formulier (40t). Segmentknop Eenheid / Algemeen, "Foto van plan" naast "Bestand kiezen",
// een Terug-knop (kop en voet) en de veldvolgorde van het ontwerp. De logica (opslaan, plan kiezen) blijft in gl-v2-punt-panel.js;
// dit bestand verandert enkel de opmaak en zet waarden in dezelfde velden. Op breder dan gsm is alles verborgen via CSS.
(function () {
    "use strict";
    var modal = document.getElementById("pt-plan-add-modal");
    if (!modal) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function field(id) { var e = document.getElementById(id); return e && e.closest(".gl-v2-field"); }

    var unitHidden = document.getElementById("pt-plan-add-unit"), unitWrap = document.getElementById("pt-plan-add-unit_select");
    var fUnit = field("pt-plan-add-unit"), fSoort = field("pt-plan-add-soort"), fNaam = field("pt-plan-add-name"), fNiveau = field("pt-plan-add-niveau");
    var filePick = document.getElementById("pt-add-file-pick"), fFile = filePick && filePick.closest(".gl-v2-field"), fileInput = document.getElementById("pt-plan-add-file");
    if (!unitHidden || !unitWrap || !fUnit || !fFile || !fileInput) return;
    if (fUnit) fUnit.classList.add("pt-f-unit"); if (fSoort) fSoort.classList.add("pt-f-soort"); if (fNiveau) fNiveau.classList.add("pt-f-niveau");
    if (fNaam) fNaam.classList.add("pt-f-naam"); fFile.classList.add("pt-f-file");

    // ── Hoort bij: Eenheid / Algemeen ──
    var lbl = document.createElement("span"); lbl.className = "gl-v2-field-label pt-unit-lbl"; lbl.innerHTML = 'Eenheid <span style="color:#8B2A2A">*</span>';
    unitWrap.parentNode.insertBefore(lbl, unitWrap);
    var seg = document.createElement("div"); seg.className = "gl-v2-pt-seg2"; seg.setAttribute("role", "group"); seg.setAttribute("aria-label", "Hoort bij");
    seg.innerHTML = '<button type="button" data-seg="unit">Eenheid</button><button type="button" data-seg="all">Algemeen</button>';
    var firstLabel = fUnit.querySelector(".gl-v2-field-label"); firstLabel.parentNode.insertBefore(seg, firstLabel.nextSibling);
    function pick(v) { var o = unitWrap.querySelector('.gl-v2-select-option[data-value="' + v + '"]'); if (o) o.click(); }
    function renderSeg() {
        var all = (unitHidden.value || "0") === "0";
        fUnit.classList.toggle("is-all", all);
        Array.prototype.forEach.call(seg.children, function (b) { b.classList.toggle("is-on", (b.dataset.seg === "all") === all); });
    }
    seg.addEventListener("click", function (e) {
        var b = e.target.closest("button"); if (!b) return;
        if (b.dataset.seg === "all") { pick("0"); return; }
        if ((unitHidden.value || "0") !== "0") { renderSeg(); return; }
        // terug naar Eenheid: de eenheid van het punt, anders de eerste eenheid uit de lijst
        var fromPoint = (document.getElementById("pt-new-unit") || {}).value;
        var first = Array.prototype.map.call(unitWrap.querySelectorAll(".gl-v2-select-option"), function (o) { return o.dataset.value; }).filter(function (v) { return v && v !== "0"; })[0];
        var target = fromPoint && fromPoint !== "0" ? fromPoint : first;
        if (target) pick(target); else renderSeg();
    });
    unitHidden.addEventListener("change", renderSeg);
    modal.addEventListener("show.bs.modal", function () { setTimeout(renderSeg, 0); });
    renderSeg();

    // ── Bestand: Foto van plan + Bestand kiezen ──
    var row = document.createElement("div"); row.className = "gl-v2-pt-filebtns";
    row.innerHTML = '<label for="pt-plan-add-photo"><i class="ph ph-camera" aria-hidden="true"></i>Foto van plan</label><button type="button" id="pt-add-file-pick-m"><i class="ph ph-upload-simple" aria-hidden="true"></i>Bestand kiezen</button>' +
        '<input type="file" id="pt-plan-add-photo" accept="image/*" capture="environment" hidden>';
    var hint = document.createElement("span"); hint.className = "gl-v2-pt-hint-m"; hint.textContent = "PDF, PNG of JPG · foto wordt rechtgezet";
    var errEl = document.getElementById("pt-plan-add-err");
    fFile.insertBefore(row, errEl || null); fFile.insertBefore(hint, errEl || null);
    row.querySelector("#pt-add-file-pick-m").addEventListener("click", function () { fileInput.click(); });

    function setFile(f) {
        if (!window.DataTransfer) return;
        var dt = new DataTransfer(); dt.items.add(f); fileInput.files = dt.files;
        fileInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    // ── Foto van plan: recht zetten (EXIF) en scheef getrokken plan rechttrekken (vier hoeken -> rechthoek) ──
    var dk = null, dkState = null;
    function dist(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }
    // homografie die de hoeken van de uitvoerrechthoek op de vier bronpunten afbeeldt (8x8 stelsel, Gauss-eliminatie)
    function homography(dst, src) {
        var A = [], B = [], i;
        for (i = 0; i < 4; i++) {
            var x = dst[i].x, y = dst[i].y, u = src[i].x, v = src[i].y;
            A.push([x, y, 1, 0, 0, 0, -u * x, -u * y]); B.push(u);
            A.push([0, 0, 0, x, y, 1, -v * x, -v * y]); B.push(v);
        }
        var n = 8, M = A.map(function (r, k) { return r.concat([B[k]]); }), c, r2, k2;
        for (c = 0; c < n; c++) {
            var piv = c; for (r2 = c + 1; r2 < n; r2++) if (Math.abs(M[r2][c]) > Math.abs(M[piv][c])) piv = r2;
            var t = M[c]; M[c] = M[piv]; M[piv] = t;
            if (Math.abs(M[c][c]) < 1e-12) return null;
            for (r2 = c + 1; r2 < n; r2++) { var f = M[r2][c] / M[c][c]; for (k2 = c; k2 <= n; k2++) M[r2][k2] -= f * M[c][k2]; }
        }
        var h = new Array(n);
        for (c = n - 1; c >= 0; c--) { var sum = M[c][n]; for (k2 = c + 1; k2 < n; k2++) sum -= M[c][k2] * h[k2]; h[c] = sum / M[c][c]; }
        return h;
    }
    function warp(bmp, q) {
        var w = Math.max(dist(q[0], q[1]), dist(q[3], q[2])), h = Math.max(dist(q[0], q[3]), dist(q[1], q[2]));
        var s = Math.min(1, 3000 / Math.max(w, h)); w = Math.max(2, Math.round(w * s)); h = Math.max(2, Math.round(h * s));
        var sc = document.createElement("canvas"); sc.width = bmp.width; sc.height = bmp.height;
        var sctx = sc.getContext("2d"); sctx.drawImage(bmp, 0, 0);
        var src = sctx.getImageData(0, 0, sc.width, sc.height), sd = src.data, sw = sc.width, sh = sc.height;
        var H = homography([{ x: 0, y: 0 }, { x: w, y: 0 }, { x: w, y: h }, { x: 0, y: h }], q); if (!H) return null;
        var out = document.createElement("canvas"); out.width = w; out.height = h;
        var octx = out.getContext("2d"), img = octx.createImageData(w, h), od = img.data;
        for (var y = 0; y < h; y++) {
            for (var x = 0; x < w; x++) {
                var den = H[6] * x + H[7] * y + 1, u = (H[0] * x + H[1] * y + H[2]) / den, v = (H[3] * x + H[4] * y + H[5]) / den;
                var o = (y * w + x) * 4;
                if (u < 0 || v < 0 || u > sw - 1 || v > sh - 1) { od[o] = od[o + 1] = od[o + 2] = 255; od[o + 3] = 255; continue; }
                var x0 = u | 0, y0 = v | 0, x1 = Math.min(x0 + 1, sw - 1), y1 = Math.min(y0 + 1, sh - 1), fx = u - x0, fy = v - y0;
                var i00 = (y0 * sw + x0) * 4, i10 = (y0 * sw + x1) * 4, i01 = (y1 * sw + x0) * 4, i11 = (y1 * sw + x1) * 4;
                for (var ch = 0; ch < 3; ch++) {
                    od[o + ch] = (sd[i00 + ch] * (1 - fx) + sd[i10 + ch] * fx) * (1 - fy) + (sd[i01 + ch] * (1 - fx) + sd[i11 + ch] * fx) * fy;
                }
                od[o + 3] = 255;
            }
        }
        octx.putImageData(img, 0, 0);
        return out;
    }
    function buildDeskew() {
        dk = document.createElement("div"); dk.className = "gl-v2-ptdeskew"; dk.hidden = true; dk.setAttribute("role", "dialog"); dk.setAttribute("aria-modal", "true"); dk.setAttribute("aria-label", "Plan rechttrekken");
        dk.innerHTML = '<div class="gl-v2-ptdeskew-top"><b>Plan rechttrekken</b><small>sleep de vier hoeken naar de hoeken van het plan</small></div>' +
            '<div class="gl-v2-ptdeskew-stage"><canvas></canvas></div>' +
            '<div class="gl-v2-ptdeskew-foot"><button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="skip">Overslaan</button><button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="go"><i class="ph ph-crop" aria-hidden="true"></i>Rechttrekken</button></div>';
        document.body.appendChild(dk);
        var cv = dk.querySelector("canvas");
        function pos(ev) { var r = cv.getBoundingClientRect(); return { x: (ev.clientX - r.left) / r.width * cv.width, y: (ev.clientY - r.top) / r.height * cv.height }; }
        cv.addEventListener("pointerdown", function (ev) {
            if (!dkState) return; ev.preventDefault();
            var p = pos(ev), best = -1, bd = 1e9, r = cv.getBoundingClientRect(), reach = 44 * cv.width / r.width;
            dkState.q.forEach(function (c, i) { var d = Math.hypot(c.x * dkState.k - p.x, c.y * dkState.k - p.y); if (d < bd) { bd = d; best = i; } });
            if (bd <= reach) { dkState.drag = best; cv.setPointerCapture(ev.pointerId); }
        });
        cv.addEventListener("pointermove", function (ev) {
            if (!dkState || dkState.drag < 0) return; var p = pos(ev);
            dkState.q[dkState.drag] = { x: Math.min(dkState.bmp.width, Math.max(0, p.x / dkState.k)), y: Math.min(dkState.bmp.height, Math.max(0, p.y / dkState.k)) };
            drawDeskew();
        });
        function end() { if (dkState) dkState.drag = -1; }
        cv.addEventListener("pointerup", end); cv.addEventListener("pointercancel", end);
        dk.addEventListener("click", function (ev) {
            var b = ev.target.closest("[data-act]"); if (!b || !dkState) return;
            var st = dkState; dkState = null; dk.hidden = true; document.body.classList.remove("gl-v2-ptcam-on");
            if (b.dataset.act === "skip") { st.done(null); return; }
            var out = warp(st.bmp, st.q); st.done(out);
        });
    }
    function drawDeskew() {
        var st = dkState, cv = dk.querySelector("canvas"), ctx = cv.getContext("2d");
        ctx.drawImage(st.bmp, 0, 0, cv.width, cv.height);
        var pts = st.q.map(function (c) { return { x: c.x * st.k, y: c.y * st.k }; });
        ctx.fillStyle = "rgba(0, 83, 45, .14)"; ctx.strokeStyle = "#00B45A"; ctx.lineWidth = 3; ctx.lineJoin = "round";
        ctx.beginPath(); pts.forEach(function (p, i) { if (i) ctx.lineTo(p.x, p.y); else ctx.moveTo(p.x, p.y); }); ctx.closePath(); ctx.fill(); ctx.stroke();
        pts.forEach(function (p) { ctx.beginPath(); ctx.arc(p.x, p.y, 15, 0, Math.PI * 2); ctx.fillStyle = "#fff"; ctx.fill(); ctx.lineWidth = 4; ctx.strokeStyle = "#00532D"; ctx.stroke(); });
    }
    // resolve(canvas | null): null = overslaan (de foto blijft zoals ze is)
    function deskew(bmp) {
        return new Promise(function (done) {
            if (!dk) buildDeskew();
            var cv = dk.querySelector("canvas"), k = Math.min(1, 1400 / Math.max(bmp.width, bmp.height));
            cv.width = Math.round(bmp.width * k); cv.height = Math.round(bmp.height * k);
            var m = 0.06;   // beginstand: net binnen de rand van de foto
            dkState = { bmp: bmp, k: k, drag: -1, done: done, q: [{ x: bmp.width * m, y: bmp.height * m }, { x: bmp.width * (1 - m), y: bmp.height * m }, { x: bmp.width * (1 - m), y: bmp.height * (1 - m) }, { x: bmp.width * m, y: bmp.height * (1 - m) }] };
            dk.hidden = false; document.body.classList.add("gl-v2-ptcam-on");
            drawDeskew();
        });
    }
    function toFile(canvas, fallback) {
        return new Promise(function (res) { canvas.toBlob(function (b) { res(b ? new File([b], "plan-foto-" + Date.now() + ".jpg", { type: "image/jpeg" }) : fallback); }, "image/jpeg", 0.9); });
    }
    row.querySelector("#pt-plan-add-photo").addEventListener("change", function (e) {
        var f = e.target.files[0]; e.target.value = ""; if (!f) return;
        if (!window.createImageBitmap) { setFile(f); return; }
        // eerst rechtzetten volgens de EXIF-draaiing en verkleinen, daarna eventueel de vier hoeken rechttrekken
        window.createImageBitmap(f, { imageOrientation: "from-image" }).then(function (bmp0) {
            var s = Math.min(1, 3200 / Math.max(bmp0.width, bmp0.height)), c = document.createElement("canvas");
            c.width = Math.round(bmp0.width * s); c.height = Math.round(bmp0.height * s);
            c.getContext("2d").drawImage(bmp0, 0, 0, c.width, c.height);
            return deskew(c).then(function (out) { return toFile(out || c, f); });
        }).then(setFile).catch(function () { setFile(f); });
    });

    // ── Terug (kop en voet): naar de plannenlijst als je daar vandaan kwam, anders sluiten ──
    document.addEventListener("click", function (e) {
        if (!e.target.closest(".js-pt-add-terug")) return;
        var back = document.getElementById("pt-add-back");
        if (back && !back.hidden) { back.click(); return; }
        if (window.bootstrap) { var m = window.bootstrap.Modal.getInstance(modal); if (m) m.hide(); }
    });
})();
