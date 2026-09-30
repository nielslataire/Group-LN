// gl-v2 — Projecten/QuoteIntakeV2 (design-handoff 20c, "Offerte inlezen"). Interactief OCR-hulpmiddel:
// 1) bron laden (PDF via pdf.js, foto/plakken rechtstreeks) op een canvas;
// 2) een kader tekenen over die canvas — modus "tabel" stuurt de bijgesneden regio naar Azure Document
//    Intelligence (server, prebuilt-layout) en zet de teruggekregen regels om in rijen; modus "foto"
//    stuurt de regio naar de server-opslag en hangt de teruggekregen foto aan een gekozen regel;
// 3) de rijentabel (Herkende regels) blijft daarna een gewone, rechtstreeks bewerkbare tabel — Opslaan/
//    Omzetten posten naar dezelfde acties als ChangeOrderDetailV2 (20d), dit scherm is enkel de intake.
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-qi-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function token() { var el = $('input[name="__RequestVerificationToken"]'); return el ? el.value : ""; }
    function getNum(el) { if (!el) return 0; var v = (el.value || "").toString().trim(); return parseFloat(v.replace(",", ".")) || 0; }
    function formatEUR(n) { return n.toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " €"; }

    if (window.pdfjsLib) window.pdfjsLib.GlobalWorkerOptions.workerSrc = cfg.pdfWorkerUrl;

    // ── 1. Bron laden ────────────────────────────────────────────────────────────────────────────────
    var emptyState = document.getElementById("gl-v2-qi-empty");
    var stage = document.getElementById("gl-v2-qi-stage");
    var canvas = document.getElementById("gl-v2-qi-canvas");
    var overlay = document.getElementById("gl-v2-qi-overlay");
    var ctx = canvas.getContext("2d");
    var octx = overlay.getContext("2d");
    var statusEl = document.getElementById("gl-v2-qi-status");

    function setStatus(text) { if (statusEl) statusEl.textContent = text || ""; }

    function showStage() { emptyState.hidden = true; stage.hidden = false; }

    function syncOverlaySize() {
        overlay.width = canvas.width;
        overlay.height = canvas.height;
    }

    function renderImageSource(imgEl) {
        var maxW = 1400; // client-side cap, ruim genoeg voor scherpe kaders en tekst
        var scale = imgEl.naturalWidth > maxW ? maxW / imgEl.naturalWidth : 1;
        canvas.width = Math.round(imgEl.naturalWidth * scale);
        canvas.height = Math.round(imgEl.naturalHeight * scale);
        ctx.drawImage(imgEl, 0, 0, canvas.width, canvas.height);
        syncOverlaySize();
        showStage();
        setStatus("Bron geladen — teken een kader.");
    }

    function loadImageFile(file) {
        var url = URL.createObjectURL(file);
        var img = new Image();
        img.onload = function () { renderImageSource(img); URL.revokeObjectURL(url); };
        img.onerror = function () { setStatus("Kon de afbeelding niet laden."); URL.revokeObjectURL(url); };
        img.src = url;
    }

    function loadPdfFile(file) {
        if (!window.pdfjsLib) { setStatus("PDF-weergave is niet beschikbaar."); return; }
        setStatus("PDF laden…");
        file.arrayBuffer().then(function (buf) {
            return window.pdfjsLib.getDocument({ data: buf }).promise;
        }).then(function (pdf) {
            return pdf.getPage(1);
        }).then(function (page) {
            var viewport = page.getViewport({ scale: 1.8 });
            canvas.width = viewport.width;
            canvas.height = viewport.height;
            return page.render({ canvasContext: ctx, viewport: viewport }).promise;
        }).then(function () {
            syncOverlaySize();
            showStage();
            setStatus("PDF geladen (pagina 1) — teken een kader.");
        }).catch(function (err) {
            setStatus("Kon de PDF niet weergeven.");
            console.error(err);
        });
    }

    function loadSource(file) {
        if (!file) return;
        if (file.type === "application/pdf") loadPdfFile(file);
        else if (file.type.indexOf("image/") === 0) loadImageFile(file);
        else setStatus("Enkel PDF of afbeeldingen worden ondersteund.");
    }

    var fileInput = document.getElementById("gl-v2-qi-file");
    if (fileInput) fileInput.addEventListener("change", function () { if (fileInput.files[0]) loadSource(fileInput.files[0]); });

    var cameraBtn = document.getElementById("gl-v2-qi-camera");
    var cameraInput = document.getElementById("gl-v2-qi-camera-input");
    if (cameraBtn && cameraInput) {
        cameraBtn.addEventListener("click", function () { cameraInput.click(); });
        cameraInput.addEventListener("change", function () { if (cameraInput.files[0]) loadSource(cameraInput.files[0]); });
    }

    document.addEventListener("paste", function (e) {
        var items = (e.clipboardData || {}).items || [];
        for (var i = 0; i < items.length; i++) {
            if (items[i].type.indexOf("image/") === 0) {
                var blob = items[i].getAsFile();
                if (blob) { loadSource(blob); e.preventDefault(); }
                return;
            }
        }
    });

    // ── 2. Kader-selectie (tabel/foto) ──────────────────────────────────────────────────────────────
    var mode = "table";
    var toolTable = document.getElementById("gl-v2-qi-tool-select");
    var toolPhoto = document.getElementById("gl-v2-qi-tool-photo");
    function setMode(next) {
        mode = next;
        if (toolTable) toolTable.classList.toggle("is-active", mode === "table");
        if (toolPhoto) toolPhoto.classList.toggle("is-active", mode === "photo");
    }
    if (toolTable) toolTable.addEventListener("click", function () { setMode("table"); });
    if (toolPhoto) toolPhoto.addEventListener("click", function () { setMode("photo"); });
    setMode("table");

    var dragging = false, startX = 0, startY = 0;

    function canvasPoint(e) {
        var rect = overlay.getBoundingClientRect();
        var scaleX = overlay.width / rect.width;
        var scaleY = overlay.height / rect.height;
        return { x: (e.clientX - rect.left) * scaleX, y: (e.clientY - rect.top) * scaleY };
    }

    function drawRect(x, y, w, h, isPhoto) {
        octx.clearRect(0, 0, overlay.width, overlay.height);
        octx.lineWidth = 2;
        octx.strokeStyle = isPhoto ? "#C9A96E" : "#00532D";
        octx.setLineDash(isPhoto ? [6, 4] : []);
        octx.strokeRect(x, y, w, h);
        octx.fillStyle = isPhoto ? "rgba(201,169,110,.12)" : "rgba(0,83,45,.08)";
        octx.fillRect(x, y, w, h);
    }

    overlay.addEventListener("pointerdown", function (e) {
        if (stage.hidden) return;
        dragging = true;
        var p = canvasPoint(e);
        startX = p.x; startY = p.y;
        overlay.setPointerCapture(e.pointerId);
    });
    overlay.addEventListener("pointermove", function (e) {
        if (!dragging) return;
        var p = canvasPoint(e);
        var x = Math.min(startX, p.x), y = Math.min(startY, p.y);
        var w = Math.abs(p.x - startX), h = Math.abs(p.y - startY);
        drawRect(x, y, w, h, mode === "photo");
    });
    overlay.addEventListener("pointerup", function (e) {
        if (!dragging) return;
        dragging = false;
        var p = canvasPoint(e);
        var x = Math.min(startX, p.x), y = Math.min(startY, p.y);
        var w = Math.abs(p.x - startX), h = Math.abs(p.y - startY);
        octx.clearRect(0, 0, overlay.width, overlay.height);
        if (w < 12 || h < 12) return; // te klein, waarschijnlijk een per ongeluk-klikje
        var blob = cropToBlob(x, y, w, h);
        if (mode === "table") handleTableRegion(blob);
        else handlePhotoRegion(blob, e.clientX, e.clientY);
    });

    function cropToBlob(x, y, w, h) {
        var crop = document.createElement("canvas");
        crop.width = w; crop.height = h;
        crop.getContext("2d").drawImage(canvas, x, y, w, h, 0, 0, w, h);
        return crop;
    }

    // ── 3a. Modus "tabel" — Azure prebuilt-layout op de bijgesneden regio ──────────────────────────
    function handleTableRegion(cropCanvas) {
        setStatus("Tabel lezen…");
        cropCanvas.toBlob(function (blob) {
            var fd = new FormData();
            fd.append("image", blob, "regio.png");
            fd.append("__RequestVerificationToken", token());
            fetch(cfg.extractTableUrl, { method: "POST", body: fd })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (!res.success) { setStatus(res.message || "Kon de tabel niet lezen."); return; }
                    if (!res.lines || res.lines.length === 0) { setStatus("Geen regels herkend in dit gebied."); return; }
                    addOcrRows(res.lines).then(function () {
                        setStatus(res.lines.length + " " + (res.lines.length === 1 ? "regel" : "regels") + " ingelezen.");
                    });
                })
                .catch(function () { setStatus("Kon de tabel niet lezen."); });
        }, "image/png");
    }

    function addOcrRows(lines) {
        var chain = Promise.resolve();
        lines.forEach(function (line) {
            chain = chain.then(function () {
                return addBlankRow().then(function (tr) {
                    if (!tr) return;
                    $(".js-qi-description", tr).value = line.description || "";
                    if (line.unit) {
                        var unitSelect = $(".js-qi-munit", tr);
                        var opt = Array.prototype.find.call(unitSelect.options, function (o) { return o.text.toLowerCase() === (line.unit || "").toLowerCase(); });
                        if (opt) unitSelect.value = opt.value;
                    }
                    if (line.number != null) $(".js-qi-number", tr).value = line.number;
                    if (line.price != null) $(".js-qi-price", tr).value = line.price;
                    setRowNeedsReview(tr, !!line.needsReview);
                    recomputeTotal();
                });
            });
        });
        return chain;
    }

    function setRowNeedsReview(tr, needsReview) {
        tr.classList.toggle("is-needs-review", needsReview);
        var hidden = $(".js-qi-needsreview", tr);
        if (hidden) hidden.value = needsReview ? "true" : "false";
        var existingFlag = $(".js-qi-confirm-row", tr);
        if (needsReview && !existingFlag) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "gl-v2-co-review-flag js-qi-confirm-row";
            btn.title = "Klikken om te bevestigen";
            btn.innerHTML = '<i class="ph ph-warning" aria-hidden="true"></i>controleer';
            $(".js-qi-description", tr).insertAdjacentElement("afterend", btn);
        } else if (!needsReview && existingFlag) {
            existingFlag.remove();
        }
    }

    // ── 3b. Modus "foto" — hangt een bijgesneden foto aan een gekozen regel ────────────────────────
    function handlePhotoRegion(cropCanvas, clientX, clientY) {
        setStatus("Foto opslaan…");
        cropCanvas.toBlob(function (blob) {
            var fd = new FormData();
            fd.append("image", blob, "foto.png");
            fd.append("__RequestVerificationToken", token());
            fetch(cfg.attachPhotoUrl, { method: "POST", body: fd })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (!res.success) { setStatus(res.message || "Kon de foto niet opslaan."); return; }
                    setStatus("Foto opgeslagen — kies aan welke regel ze hangt.");
                    showPhotoPicker(res.path, res.thumbUrl, clientX, clientY);
                })
                .catch(function () { setStatus("Kon de foto niet opslaan."); });
        }, "image/png");
    }

    function showPhotoPicker(path, thumbUrl, clientX, clientY) {
        var existing = document.querySelector(".gl-v2-qi-photo-picker");
        if (existing) existing.remove();

        var rows = $$(".js-qi-row", document.getElementById("gl-v2-qi-rows-body"));
        var picker = document.createElement("div");
        picker.className = "gl-v2-qi-photo-picker";
        picker.style.left = Math.min(clientX, window.innerWidth - 240) + "px";
        picker.style.top = Math.min(clientY, window.innerHeight - 200) + "px";

        var label = document.createElement("div");
        label.className = "gl-v2-qi-photo-picker-label";
        label.textContent = "HANG AAN WELKE REGEL?";
        picker.appendChild(label);

        function addOption(text, onClick) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "gl-v2-qi-photo-picker-item";
            item.textContent = text;
            item.addEventListener("click", onClick);
            picker.appendChild(item);
        }

        rows.forEach(function (tr, idx) {
            var desc = $(".js-qi-description", tr).value || ("Regel " + (idx + 1));
            addOption(desc, function () {
                attachPhotoToRow(tr, path, thumbUrl);
                picker.remove();
            });
        });
        addOption("+ Nieuwe regel", function () {
            addBlankRow().then(function (tr) {
                if (tr) attachPhotoToRow(tr, path, thumbUrl);
                picker.remove();
            });
        });

        document.body.appendChild(picker);
        setTimeout(function () {
            document.addEventListener("click", function closePicker(ev) {
                if (!picker.contains(ev.target)) { picker.remove(); document.removeEventListener("click", closePicker); }
            });
        }, 0);
    }

    function attachPhotoToRow(tr, path, thumbUrl) {
        var hidden = $(".js-qi-sourceimage", tr);
        if (hidden) hidden.value = path;
        var thumb = $(".gl-v2-qi-row-thumb", tr);
        if (thumb) {
            thumb.classList.add("is-photo");
            thumb.innerHTML = thumbUrl ? '<img src="' + thumbUrl + '" alt="" style="width:100%;height:100%;object-fit:cover;border-radius:6px;" />' : '<i class="ph ph-image" aria-hidden="true"></i>';
        }
    }

    // ── 4. Rijenbeheer ───────────────────────────────────────────────────────────────────────────────
    var rowsBody = document.getElementById("gl-v2-qi-rows-body");

    function addBlankRow() {
        var index = $$(".js-qi-row", rowsBody).length;
        return fetch(cfg.addRowUrl + "?index=" + index)
            .then(function (r) { return r.text(); })
            .then(function (html) {
                rowsBody.insertAdjacentHTML("beforeend", html);
                return rowsBody.lastElementChild;
            })
            .catch(function () { return null; });
    }

    function recomputeTotal() {
        var total = 0;
        $$(".js-qi-row", rowsBody).forEach(function (tr) {
            total += getNum($(".js-qi-number", tr)) * getNum($(".js-qi-price", tr));
        });
        var el = document.getElementById("gl-v2-qi-sum-total");
        if (el) el.textContent = formatEUR(total);
    }

    rowsBody.addEventListener("input", function (e) {
        if (e.target.classList.contains("js-qi-number") || e.target.classList.contains("js-qi-price")) recomputeTotal();
    });
    rowsBody.addEventListener("click", function (e) {
        var del = e.target.closest(".js-qi-delete-row");
        if (del) { del.closest(".js-qi-row").remove(); recomputeTotal(); return; }
        var confirmBtn = e.target.closest(".js-qi-confirm-row");
        if (confirmBtn) { setRowNeedsReview(confirmBtn.closest(".js-qi-row"), false); }
    });

    var addRowBtn = document.getElementById("gl-v2-qi-add-row");
    if (addRowBtn) addRowBtn.addEventListener("click", function () { addBlankRow(); });

    recomputeTotal();

    // ── 5. Opslaan / Omzetten ────────────────────────────────────────────────────────────────────────
    var form = document.getElementById("gl-v2-qi-form");
    var descHidden = document.getElementById("gl-v2-qi-description-hidden");
    if (form && descHidden) {
        form.addEventListener("submit", function () {
            // Deze pagina heeft geen apart "omschrijving voor de klant"-veld (dat komt pas bij Omzetten
            // op 20d) — gebruik het offertenummer/leverancier als voorlopige omschrijving zodat de rij
            // niet naamloos in de lijst (20b) verschijnt.
            var ref = $("#gl-v2-qi-quote-ref");
            descHidden.value = (ref && ref.value) ? ("Offerte " + ref.value) : "Offerte";
        });
    }

    var convertBtn = document.getElementById("gl-v2-qi-convert");
    var convertFlag = document.getElementById("gl-v2-qi-convert-after-save");
    if (convertBtn) {
        convertBtn.addEventListener("click", function () {
            if (cfg.changeOrderId > 0) {
                var f = document.createElement("form");
                f.method = "post"; f.action = cfg.convertUrl; f.style.display = "none";
                [["__RequestVerificationToken", token()], ["projectId", cfg.projectId], ["changeOrderId", cfg.changeOrderId]]
                    .forEach(function (pair) {
                        var input = document.createElement("input");
                        input.type = "hidden"; input.name = pair[0]; input.value = pair[1];
                        f.appendChild(input);
                    });
                document.body.appendChild(f);
                f.submit();
            } else {
                if (convertFlag) convertFlag.value = "true";
                form.requestSubmit ? form.requestSubmit() : form.submit();
            }
        });
    }
})();
