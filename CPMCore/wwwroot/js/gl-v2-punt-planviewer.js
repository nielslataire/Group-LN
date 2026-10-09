// gl-v2 — planweergave voor Punten (design-handoff 40b/40c/40k): een PDF-plan (pdf.js) met pins in statuskleur.
// Gebruik:  var v = PuntPlanViewer.create(containerEl, { onPlace(page, x, y), onPick(pin), onPage(page, total) });
//           v.load(url, page) · v.setPins([{id, page, x, y, status, label, selected}]) · v.setMarker({page, x, y} | null) · v.refresh()
// x en y zijn genormaliseerd (0..1) ten opzichte van de pagina, dus onafhankelijk van zoom of schermgrootte.
(function () {
    "use strict";
    var WORKER = "https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.worker.min.js";

    function el(tag, cls, html) { var e = document.createElement(tag); if (cls) e.className = cls; if (html) e.innerHTML = html; return e; }

    function create(host, opts) {
        opts = opts || {};
        var lib = window.pdfjsLib || window["pdfjs-dist/build/pdf"];
        host.innerHTML = "";
        host.classList.add("gl-v2-pv");

        var bar = el("div", "gl-v2-pv-bar");
        var prev = el("button", "gl-v2-pv-btn", '<i class="ph ph-caret-left" aria-hidden="true"></i>'); prev.type = "button"; prev.setAttribute("aria-label", "Vorige pagina");
        var pageLbl = el("span", "gl-v2-pv-page", "—");
        var next = el("button", "gl-v2-pv-btn", '<i class="ph ph-caret-right" aria-hidden="true"></i>'); next.type = "button"; next.setAttribute("aria-label", "Volgende pagina");
        var spacer = el("span", "gl-v2-pv-spacer");
        var zout = el("button", "gl-v2-pv-btn", '<i class="ph ph-minus" aria-hidden="true"></i>'); zout.type = "button"; zout.setAttribute("aria-label", "Uitzoomen");
        var zLbl = el("span", "gl-v2-pv-zoom", "100%");
        var zin = el("button", "gl-v2-pv-btn", '<i class="ph ph-plus" aria-hidden="true"></i>'); zin.type = "button"; zin.setAttribute("aria-label", "Inzoomen");
        [prev, pageLbl, next, spacer, zout, zLbl, zin].forEach(function (n) { bar.appendChild(n); });

        var scroll = el("div", "gl-v2-pv-scroll");
        var stage = el("div", "gl-v2-pv-stage");
        var canvas = el("canvas", "gl-v2-pv-canvas");
        var layer = el("div", "gl-v2-pv-pins");
        var msg = el("div", "gl-v2-pv-msg", "Kies een plan.");
        stage.appendChild(canvas); stage.appendChild(layer); scroll.appendChild(stage); scroll.appendChild(msg);
        host.appendChild(bar); host.appendChild(scroll);

        var pdf = null, page = 1, zoom = 1, pins = [], marker = null, renderToken = 0;

        function status(t) { msg.textContent = t; msg.hidden = !t; stage.style.visibility = t ? "hidden" : "visible"; }

        function drawPins() {
            layer.innerHTML = "";
            pins.filter(function (p) { return (p.page || 1) === page; }).forEach(function (p) {
                var b = el("button", "gl-v2-pv-pin" + (p.selected ? " is-selected" : ""));
                b.type = "button"; b.dataset.status = p.status; b.style.left = (p.x * 100) + "%"; b.style.top = (p.y * 100) + "%";
                b.title = p.label || ""; b.setAttribute("aria-label", p.label || "Punt");
                b.addEventListener("click", function (e) { e.stopPropagation(); if (opts.onPick) opts.onPick(p); });
                layer.appendChild(b);
            });
            if (marker && (marker.page || 1) === page) {
                var m = el("span", "gl-v2-pv-marker"); m.style.left = (marker.x * 100) + "%"; m.style.top = (marker.y * 100) + "%"; layer.appendChild(m);
            }
        }

        function updateBar() {
            var total = pdf ? pdf.numPages : 0;
            pageLbl.textContent = total ? "Pagina " + page + " van " + total : "—";
            prev.disabled = page <= 1; next.disabled = !pdf || page >= total;
            zLbl.textContent = Math.round(zoom * 100) + "%";
            zout.disabled = zoom <= 0.5; zin.disabled = zoom >= 4;
        }

        function render() {
            if (!pdf) return;
            var token = ++renderToken;
            pdf.getPage(page).then(function (pg) {
                if (token !== renderToken) return;
                var w = scroll.clientWidth || host.clientWidth || 600;
                var base = pg.getViewport({ scale: 1 });
                var scale = (w - 2) / base.width * zoom;
                var vp = pg.getViewport({ scale: scale });
                var dpr = window.devicePixelRatio || 1;
                canvas.width = Math.floor(vp.width * dpr); canvas.height = Math.floor(vp.height * dpr);
                canvas.style.width = vp.width + "px"; canvas.style.height = vp.height + "px";
                stage.style.width = vp.width + "px"; stage.style.height = vp.height + "px";
                var ctx = canvas.getContext("2d");
                ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
                return pg.render({ canvasContext: ctx, viewport: vp }).promise.then(function () { if (token === renderToken) { status(""); drawPins(); } });
            }).catch(function () { status("Het plan kon niet getoond worden."); });
            updateBar();
        }

        function setPage(n) {
            if (!pdf) return;
            page = Math.max(1, Math.min(pdf.numPages, n));
            render();
            if (opts.onPage) opts.onPage(page, pdf.numPages);
        }

        prev.addEventListener("click", function () { setPage(page - 1); });
        next.addEventListener("click", function () { setPage(page + 1); });
        zin.addEventListener("click", function () { zoom = Math.min(4, +(zoom + 0.25).toFixed(2)); render(); });
        zout.addEventListener("click", function () { zoom = Math.max(0.5, +(zoom - 0.25).toFixed(2)); render(); });

        stage.addEventListener("click", function (e) {
            if (!opts.onPlace || !pdf) return;
            var r = stage.getBoundingClientRect();
            var x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
            if (x < 0 || x > 1 || y < 0 || y > 1) return;
            opts.onPlace(page, +x.toFixed(5), +y.toFixed(5));
        });

        var resizeTimer = null;
        window.addEventListener("resize", function () { clearTimeout(resizeTimer); resizeTimer = setTimeout(function () { if (host.offsetParent) render(); }, 150); });

        updateBar();
        return {
            load: function (url, startPage) {
                pdf = null; marker = marker; zoom = 1; layer.innerHTML = "";
                if (!url) { status("Kies een plan."); updateBar(); return Promise.resolve(); }
                if (!lib) { status("De plan-weergave (pdf.js) kon niet geladen worden."); return Promise.resolve(); }
                lib.GlobalWorkerOptions.workerSrc = WORKER;
                status("Plan laden…");
                return lib.getDocument({ url: url, withCredentials: true }).promise.then(function (doc) {
                    pdf = doc; page = Math.max(1, Math.min(doc.numPages, startPage || 1));
                    render();
                    if (opts.onPage) opts.onPage(page, doc.numPages);
                }).catch(function () { status("Het plan kon niet geladen worden."); });
            },
            setPins: function (list) { pins = list || []; drawPins(); },
            setMarker: function (m) { marker = m; if (m && pdf && (m.page || 1) !== page) setPage(m.page || 1); else drawPins(); },
            goto: setPage,
            refresh: render,
            page: function () { return page; }
        };
    }

    window.PuntPlanViewer = { create: create };
})();
