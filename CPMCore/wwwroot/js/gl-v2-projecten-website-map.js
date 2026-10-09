// Projecten/EditV2 — tab "SEO & website": editor voor de interactieve projectkaart.
// Kies een foto, teken per eenheid een omtrek. Coördinaten zijn in % van de foto (0-100), zodat de publieke
// pagina (WWWCOPRO) exact dezelfde omtrek over dezelfde foto legt (beide: 4:3-vlak, foto "cover").
//
// Tekenen: klik punten, sluit af door op het eerste punt te klikken of Enter/"Afronden".
// Snappen: een nieuw of versleept punt springt naar een bestaand punt (van een eenheid) binnen 14 px, zodat
//   buurvlakken exact aansluiten. Alt ingedrukt = niet snappen.
// Slepen: punten van de geselecteerde omtrek verslepen; punten die exact samenvallen met een punt van een andere
//   omtrek schuiven mee (gedeelde hoek). Op een middenpunt van een zijde slepen voegt een punt toe.
//   Delete / rechtsklik op een punt verwijdert het (minstens 3 blijven).
(function () {
    "use strict";
    var root = document.getElementById("websiteMapCard");
    if (!root) return;

    var SNAP_PX = 14;
    var stage = document.getElementById("wmStage");
    var img = document.getElementById("wmImg");
    var svg = document.getElementById("wmSvg");
    var polysG = document.getElementById("wmPolys");
    var draftLine = document.getElementById("wmDraft");
    var handlesEl = document.getElementById("wmHandles");
    var snapEl = document.getElementById("wmSnap");
    var hintEl = document.getElementById("wmHint");
    var editor = document.getElementById("websiteMapEditor");
    var photoInput = document.getElementById("Website_AerialImageName");
    var jsonInput = document.getElementById("Website_LotShapesJson");
    var unitList = document.getElementById("wmUnits");
    var imageBase = root.getAttribute("data-image-base") || "";
    if (!stage || !jsonInput || !unitList) return;

    var shapes = {};          // unitId -> { pts: [[x,y],...], lx, ly }
    var selected = 0;         // geselecteerde eenheid
    var drawing = false;      // tekenmodus voor de geselecteerde eenheid
    var draft = [];           // punten tijdens het tekenen
    var cursor = null;        // laatste muispositie (%)
    var selVertex = -1;       // geselecteerd punt (voor Delete)

    function round2(n) { return Math.round(n * 100) / 100; }
    function clamp(n) { return Math.min(100, Math.max(0, n)); }

    function loadInitial() {
        var raw = jsonInput.value;
        if (!raw) return;
        try {
            JSON.parse(raw).forEach(function (s) {
                var pts = String(s.polygon || "").split(" ").map(function (p) { var a = p.split(","); return [parseFloat(a[0]), parseFloat(a[1])]; })
                    .filter(function (p) { return isFinite(p[0]) && isFinite(p[1]); });
                var hasLabel = isFinite(s.labelX) && isFinite(s.labelY);
                if (pts.length >= 3) shapes[s.unitId] = { pts: pts, lx: s.labelX, ly: s.labelY, auto: !hasLabel };
            });
        } catch (e) { /* ongeldige JSON: begin leeg */ }
    }

    function centroid(pts) {
        var x = 0, y = 0;
        pts.forEach(function (p) { x += p[0]; y += p[1]; });
        return [round2(x / pts.length), round2(y / pts.length)];
    }

    // Labelpositie: automatisch (zwaartepunt) tot de gebruiker het label versleept.
    function labelPos(s) { return s.auto ? centroid(s.pts) : [s.lx, s.ly]; }

    function serialize() {
        var out = [];
        Object.keys(shapes).forEach(function (id) {
            var s = shapes[id];
            var c = labelPos(s);
            out.push({
                unitId: +id,
                polygon: s.pts.map(function (p) { return round2(p[0]) + "," + round2(p[1]); }).join(" "),
                labelX: c[0], labelY: c[1]
            });
        });
        jsonInput.value = JSON.stringify(out);
        jsonInput.dispatchEvent(new Event("input", { bubbles: true }));
    }

    // ── Foto kiezen ──────────────────────────────────────────────────────────────
    function setPhoto(name) {
        photoInput.value = name || "";
        if (name) {
            img.src = imageBase + name;
            editor.hidden = false;
        } else {
            img.removeAttribute("src");
            editor.hidden = true;
        }
        document.querySelectorAll(".gl-v2-wm-photo").forEach(function (b) {
            b.setAttribute("aria-checked", b.getAttribute("data-name") === name ? "true" : "false");
        });
    }
    document.getElementById("wmPhotos").addEventListener("click", function (e) {
        var b = e.target.closest(".gl-v2-wm-photo");
        if (!b) return;
        var name = b.getAttribute("data-name");
        setPhoto(photoInput.value === name ? "" : name);
        photoInput.dispatchEvent(new Event("input", { bubbles: true }));
    });

    // ── Geometrie ────────────────────────────────────────────────────────────────
    function toPct(ev) {
        var r = stage.getBoundingClientRect();
        return [clamp((ev.clientX - r.left) / r.width * 100), clamp((ev.clientY - r.top) / r.height * 100)];
    }
    function pxDist(a, b) {
        var r = stage.getBoundingClientRect();
        var dx = (a[0] - b[0]) / 100 * r.width, dy = (a[1] - b[1]) / 100 * r.height;
        return Math.sqrt(dx * dx + dy * dy);
    }
    // Dichtstbijzijnde bestaande hoekpunt (van alle omtrekken + het concept), met uitsluitingen.
    function nearestVertex(p, skip) {
        var best = null, bestD = SNAP_PX;
        function test(q) {
            var d = pxDist(p, q);
            if (d <= bestD) { bestD = d; best = q; }
        }
        Object.keys(shapes).forEach(function (id) {
            shapes[id].pts.forEach(function (q, i) {
                if (skip && skip.some(function (s) { return s.id === +id && s.i === i; })) return;
                test(q);
            });
        });
        if (drawing) draft.forEach(function (q, i) { if (!(skip && skip.draft === i)) test(q); });
        return best;
    }
    function snap(p, ev, skip) {
        if (ev && ev.altKey) return { p: [round2(p[0]), round2(p[1])], snapped: false };
        var n = nearestVertex(p, skip);
        return n ? { p: [n[0], n[1]], snapped: true } : { p: [round2(p[0]), round2(p[1])], snapped: false };
    }

    // ── Tekenen ──────────────────────────────────────────────────────────────────
    function startDrawing() {
        if (!selected) return;
        drawing = true; draft = []; selVertex = -1;
        render();
    }
    function cancelDrawing() { drawing = false; draft = []; render(); }
    function finishDrawing() {
        if (draft.length < 3) return;
        var c = centroid(draft);
        shapes[selected] = { pts: draft.slice(), lx: c[0], ly: c[1], auto: true };
        drawing = false; draft = [];
        serialize(); render();
    }

    stage.addEventListener("pointerdown", function (e) {
        if (e.target.closest(".gl-v2-wm-handle") || e.target.closest(".gl-v2-wm-mid")) return;
        if (!selected) return;
        if (drawing) {
            e.preventDefault();
            var s = snap(toPct(e), e);
            if (draft.length >= 3 && pxDist(s.p, draft[0]) <= SNAP_PX) { finishDrawing(); return; }
            draft.push(s.p);
            render();
            return;
        }
        // klik op een leeg stuk: niets selecteren van punten
        selVertex = -1; renderHandles();
    });
    stage.addEventListener("pointermove", function (e) {
        cursor = toPct(e);
        if (!drawing) { snapEl.hidden = true; return; }
        var s = snap(cursor, e);
        cursor = s.p;
        if (s.snapped) { snapEl.style.left = s.p[0] + "%"; snapEl.style.top = s.p[1] + "%"; snapEl.hidden = false; } else { snapEl.hidden = true; }
        renderDraft();
    });
    stage.addEventListener("pointerleave", function () { snapEl.hidden = true; });
    stage.addEventListener("dblclick", function () { if (drawing) { draft.pop(); finishDrawing(); } });

    // ── Punten slepen / toevoegen / verwijderen ─────────────────────────────────
    function startDrag(ev, id, idx) {
        ev.preventDefault();
        selVertex = idx;
        var s = shapes[id], start = s.pts[idx].slice();
        // punten van andere omtrekken die exact samenvallen schuiven mee (gedeelde hoek)
        var linked = [];
        Object.keys(shapes).forEach(function (oid) {
            if (+oid === id) return;
            shapes[oid].pts.forEach(function (q, qi) { if (q[0] === start[0] && q[1] === start[1]) linked.push({ id: +oid, i: qi }); });
        });
        var skip = [{ id: id, i: idx }].concat(linked);
        var moved = false;
        function move(e) {
            moved = true;
            var r = snap(toPct(e), e, skip);
            s.pts[idx] = r.p;
            linked.forEach(function (l) { shapes[l.id].pts[l.i] = r.p.slice(); });
            if (r.snapped) { snapEl.style.left = r.p[0] + "%"; snapEl.style.top = r.p[1] + "%"; snapEl.hidden = false; } else { snapEl.hidden = true; }
            renderPolys(); positionHandles();
        }
        function up() {
            document.removeEventListener("pointermove", move);
            document.removeEventListener("pointerup", up);
            snapEl.hidden = true;
            if (moved) { serialize(); }
            render();
        }
        document.addEventListener("pointermove", move);
        document.addEventListener("pointerup", up);
    }
    function removeVertex(id, idx) {
        var s = shapes[id];
        if (!s || s.pts.length <= 3) return;
        s.pts.splice(idx, 1);
        selVertex = -1;
        serialize(); render();
    }
    document.addEventListener("keydown", function (e) {
        if (!root.offsetParent) return;
        var tag = (e.target.tagName || "").toLowerCase();
        if (tag === "input" || tag === "textarea" || tag === "select") return;
        if (drawing) {
            if (e.key === "Enter") { e.preventDefault(); finishDrawing(); }
            else if (e.key === "Escape") { e.preventDefault(); cancelDrawing(); }
            else if (e.key === "Backspace") { e.preventDefault(); draft.pop(); render(); }
            return;
        }
        if ((e.key === "Delete" || e.key === "Backspace") && selected && selVertex >= 0 && stage.matches(":hover, :focus-within")) {
            e.preventDefault(); removeVertex(selected, selVertex);
        }
    });

    // ── Rendering ────────────────────────────────────────────────────────────────
    function ptsAttr(pts) { return pts.map(function (p) { return p[0] + "," + p[1]; }).join(" "); }
    function renderPolys() {
        var html = "";
        Object.keys(shapes).forEach(function (id) {
            html += '<polygon data-unit="' + id + '" class="gl-v2-wm-poly' + (+id === selected ? " is-selected" : "") + '" points="' + ptsAttr(shapes[id].pts) + '"></polygon>';
        });
        polysG.innerHTML = html;
        // labels (HTML, zodat ze niet vervormen)
        var labels = "";
        Object.keys(shapes).forEach(function (id) {
            var c = labelPos(shapes[id]), name = unitName(+id);
            labels += '<span class="gl-v2-wm-label' + (+id === selected ? " is-selected" : "") + '" data-unit="' + id + '" title="Sleep om het label te verplaatsen — dubbelklik voor automatisch" style="left:' + c[0] + '%;top:' + c[1] + '%">' + esc(name) + "</span>";
        });
        document.getElementById("wmLabels").innerHTML = labels;
    }
    function renderDraft() {
        if (!drawing || !draft.length) { draftLine.setAttribute("points", ""); return; }
        var pts = draft.slice();
        if (cursor) pts.push(cursor);
        draftLine.setAttribute("points", ptsAttr(pts));
    }
    function positionHandles() {
        handlesEl.querySelectorAll("[data-x]").forEach(function (h) {
            h.style.left = h.getAttribute("data-x") + "%"; h.style.top = h.getAttribute("data-y") + "%";
        });
        // posities volgen de huidige punten
        if (!drawing && shapes[selected]) {
            var pts = shapes[selected].pts;
            handlesEl.querySelectorAll(".gl-v2-wm-handle").forEach(function (h) {
                var i = +h.getAttribute("data-i");
                if (pts[i]) { h.style.left = pts[i][0] + "%"; h.style.top = pts[i][1] + "%"; }
            });
            handlesEl.querySelectorAll(".gl-v2-wm-mid").forEach(function (h) {
                var i = +h.getAttribute("data-i"), a = pts[i], b = pts[(i + 1) % pts.length];
                if (a && b) { h.style.left = (a[0] + b[0]) / 2 + "%"; h.style.top = (a[1] + b[1]) / 2 + "%"; }
            });
        }
    }
    function renderHandles() {
        handlesEl.innerHTML = "";
        if (drawing) {
            draft.forEach(function (p, i) {
                var d = document.createElement("span");
                d.className = "gl-v2-wm-dot" + (i === 0 && draft.length >= 3 ? " is-first" : "");
                d.style.left = p[0] + "%"; d.style.top = p[1] + "%";
                handlesEl.appendChild(d);
            });
            return;
        }
        var s = shapes[selected];
        if (!s) return;
        s.pts.forEach(function (p, i) {
            var h = document.createElement("button");
            h.type = "button";
            h.className = "gl-v2-wm-handle" + (i === selVertex ? " is-selected" : "");
            h.setAttribute("data-i", i);
            h.setAttribute("aria-label", "Punt " + (i + 1) + " verslepen");
            h.style.left = p[0] + "%"; h.style.top = p[1] + "%";
            h.addEventListener("pointerdown", function (e) { if (e.button === 0) startDrag(e, selected, i); });
            h.addEventListener("contextmenu", function (e) { e.preventDefault(); removeVertex(selected, i); });
            handlesEl.appendChild(h);
        });
        s.pts.forEach(function (p, i) {
            var b = s.pts[(i + 1) % s.pts.length];
            var m = document.createElement("button");
            m.type = "button";
            m.className = "gl-v2-wm-mid";
            m.setAttribute("data-i", i);
            m.setAttribute("aria-label", "Punt toevoegen op zijde " + (i + 1));
            m.style.left = (p[0] + b[0]) / 2 + "%"; m.style.top = (p[1] + b[1]) / 2 + "%";
            m.addEventListener("pointerdown", function (e) {
                if (e.button !== 0) return;
                var mid = [round2((p[0] + b[0]) / 2), round2((p[1] + b[1]) / 2)];
                s.pts.splice(i + 1, 0, mid);
                renderHandles();
                startDrag(e, selected, i + 1);
            });
            handlesEl.appendChild(m);
        });
    }
    function esc(s) { return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/"/g, "&quot;"); }
    function unitName(id) {
        var b = unitList.querySelector('[data-unit="' + id + '"]');
        return b ? b.getAttribute("data-name") : "Eenheid " + id;
    }
    function renderUnits() {
        unitList.querySelectorAll("[data-unit]").forEach(function (row) {
            var id = +row.getAttribute("data-unit"), has = !!shapes[id];
            row.classList.toggle("is-selected", id === selected);
            row.classList.toggle("is-drawn", has);
            var st = row.querySelector(".gl-v2-wm-unit-state");
            if (st) st.textContent = (id === selected && drawing) ? "tekenen… (" + draft.length + " punten)" : (has ? "omtrek getekend" : "nog geen omtrek");
            var draw = row.querySelector("[data-act='draw']");
            if (draw) draw.textContent = has ? "Opnieuw tekenen" : "Tekenen";
            var clear = row.querySelector("[data-act='clear']");
            if (clear) clear.hidden = !has;
        });
        hintEl.textContent = drawing
            ? "Klik de hoekpunten aan. Sluit af door op het eerste punt te klikken, of druk Enter. Backspace = laatste punt weg, Esc = annuleren. Alt = niet snappen."
            : (selected ? (shapes[selected] ? "Sleep de punten om aan te passen; sleep het bolletje midden op een zijde om een punt toe te voegen. Rechtsklik op een punt verwijdert het." : "Kies “Tekenen” om de omtrek van deze eenheid aan te duiden.") : "Kies links een eenheid om de omtrek te tekenen.");
        stage.classList.toggle("is-drawing", drawing);
    }
    function render() { renderPolys(); renderDraft(); renderHandles(); renderUnits(); }

    unitList.addEventListener("click", function (e) {
        var row = e.target.closest("[data-unit]");
        if (!row) return;
        var id = +row.getAttribute("data-unit");
        var act = e.target.closest("[data-act]");
        if (drawing && id !== selected) cancelDrawing();
        selected = id; selVertex = -1;
        if (act && act.getAttribute("data-act") === "clear") {
            delete shapes[id]; serialize(); drawing = false; draft = [];
        } else if (act && act.getAttribute("data-act") === "draw") {
            drawing = true; draft = [];
        } else if (!shapes[id]) {
            drawing = true; draft = [];
        } else {
            drawing = false; draft = [];
        }
        render();
    });
    // klik op een omtrek in de kaart selecteert die eenheid
    polysG.addEventListener("pointerdown", function (e) {
        var poly = e.target.closest("polygon");
        if (!poly || drawing) return;
        selected = +poly.getAttribute("data-unit"); selVertex = -1; render();
    });
    // Label verslepen (de plek waar de naam van de eenheid op de website komt); dubbelklik = weer automatisch.
    var labelsEl = document.getElementById("wmLabels");
    labelsEl.addEventListener("pointerdown", function (e) {
        var el = e.target.closest(".gl-v2-wm-label");
        if (!el || drawing || e.button !== 0) return;
        e.preventDefault();
        var id = +el.getAttribute("data-unit"), s = shapes[id];
        if (!s) return;
        selected = id; selVertex = -1;
        function move(ev) {
            var p = toPct(ev);
            s.auto = false; s.lx = round2(p[0]); s.ly = round2(p[1]);
            el.style.left = s.lx + "%"; el.style.top = s.ly + "%";
        }
        function up() {
            document.removeEventListener("pointermove", move);
            document.removeEventListener("pointerup", up);
            serialize(); render();
        }
        document.addEventListener("pointermove", move);
        document.addEventListener("pointerup", up);
    });
    labelsEl.addEventListener("dblclick", function (e) {
        var el = e.target.closest(".gl-v2-wm-label");
        if (!el || drawing) return;
        var s = shapes[+el.getAttribute("data-unit")];
        if (s) { s.auto = true; serialize(); render(); }
    });
    var doneBtn = document.getElementById("wmFinish");
    if (doneBtn) doneBtn.addEventListener("click", function () { if (drawing) finishDrawing(); });
    var cancelBtn = document.getElementById("wmCancel");
    if (cancelBtn) cancelBtn.addEventListener("click", function () { if (drawing) cancelDrawing(); });

    loadInitial();
    setPhoto(photoInput.value);
    render();
})();
