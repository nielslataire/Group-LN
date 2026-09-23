// gl-v2 layout-pilot — Projecten/DetailPhotosV2 (design-handoff punt 15 "Media in een project").
// Reuses the generic shell behaviors (initGlV2Select() in gl-v2-shell.js auto-wires every
// .gl-v2-select[data-gl-v2-select] on this page: the sort dropdown, the selection bar's "Verplaatsen
// naar …" picker, and the detail panel's Sectie/Eenheid fields — this file only ever *reads* their
// hidden input's value/change-event or *writes* it programmatically via setGlV2SelectValue(), it
// never re-implements the open/close/keyboard logic). initContextMenus() likewise already wires every
// .js-gl-v2-menu-trigger (the panel's "···" overflow menu).
//
// The detail panel (15b) is built once in the DOM and repopulated client-side from
// window.glV2DetailPhotos.media (one JSON payload, embedded server-side) — browsing with ‹ ›/arrow
// keys is instant, no round trip. Saving IS a round trip (UpdateMediaDetails) followed by a full
// reload — deliberately, same reasoning as every other mutation on this page (move/visibility/delete/
// upload all already reload): re-deriving every place a change could ripple to (tab counts, the hero
// card, section grid membership, the inner-menu "Media" counter) correctly without a reload is a much
// larger undertaking than this pass justifies, and every other gl-v2 page this app has shipped makes
// the same trade-off.
(function () {
    "use strict";
    var cfg = window.glV2DetailPhotos || { media: [], sections: [], urls: {} };
    var antiForgery = document.querySelector('input[name="__RequestVerificationToken"]');
    antiForgery = antiForgery ? antiForgery.value : "";

    function postJson(url, body) {
        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": antiForgery },
            body: JSON.stringify(body)
        }).then(function (r) {
            if (!r.ok) { alert("Actie mislukt (status " + r.status + "). Mogelijk ontbreken de nodige rechten."); throw new Error("postJson " + url + " failed: " + r.status); }
            return r;
        });
    }

    function esc(s) { return (s || "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }

    // Setter voor een .gl-v2-select-instantie zonder een change-event te dispatchen (gebruikt om het
    // detailpaneel te VULLEN — initGlV2Select() se eigen selectOption() blijft de enige plek die een
    // echte gebruikerskeuze afhandelt, dat gebeurt via de al-gewired klik op een optie).
    function setGlV2SelectValue(wrapEl, value, label) {
        if (!wrapEl) return;
        var hidden = wrapEl.querySelector("input[type=hidden]");
        var trigger = wrapEl.querySelector(".gl-v2-select-trigger");
        if (hidden) hidden.value = value == null ? "" : String(value);
        if (trigger) {
            var labelEl = trigger.querySelector(".gl-v2-select-trigger-label");
            if (labelEl) labelEl.textContent = label;
            trigger.classList.toggle("is-filled", !!value);
        }
        wrapEl.querySelectorAll(".gl-v2-select-option").forEach(function (opt) {
            opt.classList.toggle("is-selected", (opt.getAttribute("data-value") || "") === (value == null ? "" : String(value)));
        });
    }

    // ── Sectietabs (Alles / sectie / geen sectie) ─────────────────────────────────────────────
    var activeSectionTab = "all";
    document.querySelectorAll("#gl-v2-dp-section-tabs .gl-v2-tabbar-tab[data-section]").forEach(function (tab) {
        tab.addEventListener("click", function () {
            document.querySelectorAll("#gl-v2-dp-section-tabs .gl-v2-tabbar-tab").forEach(function (t) { t.classList.remove("is-active"); t.setAttribute("aria-selected", "false"); });
            tab.classList.add("is-active");
            tab.setAttribute("aria-selected", "true");
            activeSectionTab = tab.getAttribute("data-section");
            document.querySelectorAll(".gl-v2-dp-section[data-section-group]").forEach(function (group) {
                var g = group.getAttribute("data-section-group");
                group.hidden = !(activeSectionTab === "all" || activeSectionTab === g);
            });
        });
    });

    // ── Zoeken + Foto's/Video's + Publiek/Intern segmentcontrols ──────────────────────────────
    var typeFilter = "all";
    var visFilter = "all";
    var searchInput = document.getElementById("gl-v2-dp-search");
    var searchClear = document.getElementById("gl-v2-dp-search-clear");

    function applyCardFilters() {
        var q = (searchInput && searchInput.value || "").trim().toLowerCase();
        document.querySelectorAll(".gl-v2-dp-card").forEach(function (card) {
            var matchesType = typeFilter === "all" || (typeFilter === "photo" && card.dataset.mediatype === "0") || (typeFilter === "video" && card.dataset.mediatype === "1");
            var matchesVis = visFilter === "all" || (visFilter === "public" && card.dataset.ispublic === "true") || (visFilter === "private" && card.dataset.ispublic === "false");
            var matchesSearch = !q || card.dataset.caption.indexOf(q) !== -1 || card.dataset.filename.toLowerCase().indexOf(q) !== -1;
            card.hidden = !(matchesType && matchesVis && matchesSearch);
        });
    }
    if (searchInput) searchInput.addEventListener("input", applyCardFilters);
    if (searchClear) searchClear.addEventListener("click", function () { searchInput.value = ""; applyCardFilters(); searchInput.focus(); });

    document.querySelectorAll('.gl-v2-dp-segmented[data-segmented="type"] .gl-v2-dp-segment').forEach(function (seg) {
        seg.addEventListener("click", function () {
            document.querySelectorAll('.gl-v2-dp-segmented[data-segmented="type"] .gl-v2-dp-segment').forEach(function (s) { s.classList.remove("is-active"); });
            seg.classList.add("is-active");
            typeFilter = seg.getAttribute("data-value");
            applyCardFilters();
        });
    });
    document.querySelectorAll('.gl-v2-dp-segmented[data-segmented="visibility"] .gl-v2-dp-segment').forEach(function (seg) {
        seg.addEventListener("click", function () {
            document.querySelectorAll('.gl-v2-dp-segmented[data-segmented="visibility"] .gl-v2-dp-segment').forEach(function (s) { s.classList.remove("is-active"); });
            seg.classList.add("is-active");
            visFilter = seg.getAttribute("data-value");
            applyCardFilters();
        });
    });

    // ── Sorteren binnen elk raster ("Eigen volgorde" = de oorspronkelijke servervolgorde) ──────
    var originalGridOrder = new Map();
    document.querySelectorAll(".gl-v2-dp-grid").forEach(function (grid) {
        originalGridOrder.set(grid, Array.prototype.slice.call(grid.querySelectorAll(".gl-v2-dp-card")));
    });
    var sortValueInput = document.getElementById("gl-v2-dp-sort-value");
    if (sortValueInput) {
        sortValueInput.addEventListener("change", function () {
            var mode = sortValueInput.value;
            document.querySelectorAll(".gl-v2-dp-grid").forEach(function (grid) {
                var dropzone = grid.querySelector(".gl-v2-dp-dropzone");
                var cards = originalGridOrder.get(grid).slice();
                if (mode === "newest") cards.sort(function (a, b) { return Number(b.dataset.uploaded) - Number(a.dataset.uploaded); });
                else if (mode === "oldest") cards.sort(function (a, b) { return Number(a.dataset.uploaded) - Number(b.dataset.uploaded); });
                else if (mode === "name") cards.sort(function (a, b) { return a.dataset.caption.localeCompare(b.dataset.caption); });
                cards.forEach(function (c) { grid.insertBefore(c, dropzone || null); });
            });
        });
    }

    // ── Selectie + donkere actiebalk ───────────────────────────────────────────────────────────
    var selectedIds = new Set();
    var selectionbar = document.getElementById("gl-v2-dp-selectionbar");
    var toolbar = document.getElementById("gl-v2-dp-toolbar");

    function mediaById(id) { return cfg.media.find(function (m) { return m.id === id; }); }

    function updateSelectionbar() {
        var n = selectedIds.size;
        if (!selectionbar) return;
        selectionbar.hidden = n === 0;
        if (toolbar) toolbar.hidden = n > 0;
        if (n === 0) return;
        document.getElementById("gl-v2-dp-selectionbar-n").textContent = n;
        var items = Array.from(selectedIds).map(mediaById).filter(Boolean);
        var photos = items.filter(function (m) { return m.mediaType === 0; }).length;
        var videos = items.filter(function (m) { return m.mediaType === 1; }).length;
        var sectionNames = new Set(items.map(function (m) { return m.sectionName || "Geen sectie"; }));
        var parts = [];
        if (photos) parts.push(photos + " foto" + (photos === 1 ? "" : "'s"));
        if (videos) parts.push(videos + " video" + (videos === 1 ? "" : "'s"));
        var sub = parts.join(" · ");
        if (sectionNames.size === 1) sub += " · " + Array.from(sectionNames)[0];
        document.getElementById("gl-v2-dp-selectionbar-sub").textContent = sub;
    }

    function clearSelection() {
        selectedIds.clear();
        document.querySelectorAll(".js-gl-v2-dp-check").forEach(function (cb) { cb.checked = false; });
        document.querySelectorAll(".gl-v2-dp-card.is-selected").forEach(function (c) { c.classList.remove("is-selected"); });
        updateSelectionbar();
    }

    document.addEventListener("change", function (e) {
        if (!e.target.classList || !e.target.classList.contains("js-gl-v2-dp-check")) return;
        var id = parseInt(e.target.value, 10);
        if (e.target.checked) selectedIds.add(id); else selectedIds.delete(id);
        var card = e.target.closest(".gl-v2-dp-card");
        if (card) card.classList.toggle("is-selected", e.target.checked);
        updateSelectionbar();
    });

    var clearBtn = document.getElementById("gl-v2-dp-selectionbar-clear");
    if (clearBtn) clearBtn.addEventListener("click", clearSelection);

    var moveValueInput = document.getElementById("gl-v2-dp-move-value");
    if (moveValueInput) {
        moveValueInput.addEventListener("change", function () {
            var v = moveValueInput.value;
            postJson(cfg.urls.moveToSection, { MediaIds: Array.from(selectedIds), SectionId: v ? parseInt(v, 10) : null })
                .then(function () { location.reload(); });
        });
    }
    var publicBtn = document.getElementById("gl-v2-dp-selectionbar-public");
    if (publicBtn) publicBtn.addEventListener("click", function () {
        postJson(cfg.urls.updateVisibility, { MediaIds: Array.from(selectedIds), IsPublic: true }).then(function () { location.reload(); });
    });
    var privateBtn = document.getElementById("gl-v2-dp-selectionbar-private");
    if (privateBtn) privateBtn.addEventListener("click", function () {
        postJson(cfg.urls.updateVisibility, { MediaIds: Array.from(selectedIds), IsPublic: false }).then(function () { location.reload(); });
    });
    var downloadBtn = document.getElementById("gl-v2-dp-selectionbar-download");
    if (downloadBtn) downloadBtn.addEventListener("click", function () {
        Array.from(selectedIds).map(mediaById).filter(Boolean).forEach(function (m) {
            var a = document.createElement("a");
            a.href = m.thumbUrl; a.download = m.fileName; a.style.display = "none";
            document.body.appendChild(a); a.click(); document.body.removeChild(a);
        });
    });
    var deleteSelBtn = document.getElementById("gl-v2-dp-selectionbar-delete");
    if (deleteSelBtn) deleteSelBtn.addEventListener("click", function () {
        if (!confirm("Weet u zeker dat u " + selectedIds.size + " item(s) wil verwijderen? Dit kan niet ongedaan worden gemaakt.")) return;
        postJson(cfg.urls.bulkDelete, { MediaIds: Array.from(selectedIds) }).then(function () { location.reload(); });
    });

    // ── "Ander hoofdbeeld" — kiesmodus ─────────────────────────────────────────────────────────
    var pickingBanner = document.getElementById("gl-v2-dp-picking-banner");
    var pickingStartBtn = document.getElementById("gl-v2-dp-picking-start");
    var isPicking = false;

    function setPicking(on) {
        isPicking = on;
        document.body.classList.toggle("gl-v2-dp-is-picking", on);
        if (pickingBanner) pickingBanner.hidden = !on;
    }
    if (pickingStartBtn) pickingStartBtn.addEventListener("click", function () { setPicking(true); });
    document.querySelectorAll(".js-gl-v2-dp-picking-cancel").forEach(function (b) { b.addEventListener("click", function () { setPicking(false); }); });

    // ── Kaart klikken: kiesmodus → hoofdbeeld instellen, anders → detailpaneel openen ──────────
    document.addEventListener("click", function (e) {
        var card = e.target.closest(".js-gl-v2-dp-card");
        if (!card || e.target.closest(".gl-v2-dp-card-check")) return;
        var id = parseInt(card.getAttribute("data-id"), 10);
        if (isPicking) {
            postJson(cfg.urls.setHoofdMedia, { mediaId: id, projectId: cfg.projectId }).then(function () { location.reload(); });
            return;
        }
        openPanel(id);
    });
    document.querySelectorAll(".js-gl-v2-dp-hero-open").forEach(function (el) {
        el.addEventListener("click", function () {
            var hero = el.closest(".gl-v2-dp-hero");
            if (hero) openPanel(parseInt(hero.getAttribute("data-id"), 10));
        });
        el.addEventListener("keydown", function (e) { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); el.click(); } });
    });

    // ── Detailpaneel (15b) ──────────────────────────────────────────────────────────────────────
    var panel = document.getElementById("gl-v2-dp-panel");
    var panelBackdrop = document.getElementById("gl-v2-dp-panel-backdrop");
    var panelState = { id: null, siblings: [], index: -1 };

    function siblingsFor(item) {
        var key = item.sectionId == null ? null : item.sectionId;
        return cfg.media.filter(function (m) { return (m.sectionId == null ? null : m.sectionId) === key; });
    }

    function renderPanel(item) {
        document.getElementById("gl-v2-dp-panel-type").textContent = item.mediaType === 1 ? "VIDEO" : "FOTO";
        document.getElementById("gl-v2-dp-panel-title").textContent = item.title;
        document.getElementById("gl-v2-dp-panel-index").textContent = (panelState.index + 1) + " / " + panelState.siblings.length;
        document.getElementById("gl-v2-dp-panel-prev").disabled = panelState.index <= 0;
        document.getElementById("gl-v2-dp-panel-next").disabled = panelState.index >= panelState.siblings.length - 1;

        var mediaEl = document.getElementById("gl-v2-dp-panel-media");
        mediaEl.innerHTML = "";
        if (item.mediaType === 1) {
            var video = document.createElement("video");
            video.src = item.thumbUrl; video.controls = true; video.preload = "metadata";
            mediaEl.appendChild(video);
            if (item.posterTimestampSeconds != null) {
                var chip = document.createElement("span");
                chip.className = "gl-v2-dp-panel-poster-chip";
                chip.textContent = "posterbeeld op " + formatDuration(item.posterTimestampSeconds);
                mediaEl.appendChild(chip);
            }
        } else {
            var img = document.createElement("img");
            img.src = item.heroUrl; img.alt = item.altText || item.title;
            mediaEl.appendChild(img);
        }

        var meta = document.getElementById("gl-v2-dp-panel-meta");
        var rows = item.mediaType === 1
            ? [["FORMAAT", item.ext], ["DUUR", item.durationLabel], ["RESOLUTIE", item.widthPx ? (item.widthPx + "×" + item.heightPx) : "—"], ["GROOTTE", item.sizeLabel]]
            : [["FORMAAT", item.ext], ["AFMETING", item.widthPx ? (item.widthPx + "×" + item.heightPx) : "—"], ["GROOTTE", item.sizeLabel], ["GEÜPLOAD", item.uploadedLabel]];
        meta.innerHTML = rows.map(function (r) { return '<div class="gl-v2-dp-panel-meta-item"><span>' + r[0] + "</span><b>" + esc(r[1]) + "</b></div>"; }).join("");

        document.getElementById("gl-v2-dp-panel-title-input").value = item.title || "";
        setGlV2SelectValue(document.getElementById("gl-v2-dp-panel-section-field"), item.sectionId, item.sectionName || "— Geen sectie —");
        setGlV2SelectValue(document.getElementById("gl-v2-dp-panel-unit-field"), item.unitId, item.unitName || "— Geen eenheid —");

        var textLabel = document.getElementById("gl-v2-dp-panel-text-label");
        var textInput = document.getElementById("gl-v2-dp-panel-text-input");
        var textHelp = document.getElementById("gl-v2-dp-panel-text-help");
        if (item.mediaType === 1) {
            textLabel.textContent = "ONDERTITEL / BESCHRIJVING";
            textInput.value = item.subtitle || "";
            textInput.placeholder = "Beschrijf de video …";
            textHelp.textContent = "verschijnt onder de video op de website";
        } else {
            textLabel.textContent = "ALT-TEKST";
            textInput.value = item.altText || "";
            textInput.placeholder = "Beschrijf het beeld …";
            textHelp.textContent = "wordt voorgelezen door schermlezers en getoond als de foto niet laadt";
        }

        document.getElementById("gl-v2-dp-panel-public").checked = !!item.isPublic;
        var hoofdCb = document.getElementById("gl-v2-dp-panel-hoofdbeeld");
        hoofdCb.checked = !!item.isHoofdbeeld;
        hoofdCb.disabled = !!item.isHoofdbeeld;

        var autoplayRow = document.getElementById("gl-v2-dp-panel-autoplay-row");
        autoplayRow.hidden = item.mediaType !== 1;
        document.getElementById("gl-v2-dp-panel-autoplay").checked = item.mediaType === 1 ? !!item.autoPlayMuted : false;

        var posterBtn = document.getElementById("gl-v2-dp-panel-poster");
        if (posterBtn) posterBtn.hidden = item.mediaType !== 1;
        var dl = document.getElementById("gl-v2-dp-panel-download");
        dl.href = item.thumbUrl; dl.setAttribute("download", item.fileName || "");

        panelState.pendingPosterTimestamp = null;
    }

    function formatDuration(sec) {
        if (sec == null) return "0:00";
        var m = Math.floor(sec / 60), s = Math.floor(sec % 60);
        return m + ":" + (s < 10 ? "0" : "") + s;
    }

    function openPanel(id) {
        var item = mediaById(id);
        if (!item) return;
        panelState.id = id;
        panelState.siblings = siblingsFor(item);
        panelState.index = panelState.siblings.findIndex(function (m) { return m.id === id; });
        renderPanel(item);
        panel.hidden = false; panelBackdrop.hidden = false;
        requestAnimationFrame(function () { panel.classList.add("is-open"); });
        document.documentElement.style.overflow = "hidden";
    }
    function closePanel() {
        panel.classList.remove("is-open");
        document.documentElement.style.overflow = "";
        setTimeout(function () { panel.hidden = true; panelBackdrop.hidden = true; }, 220);
    }
    document.querySelectorAll(".js-gl-v2-dp-panel-close").forEach(function (b) { b.addEventListener("click", closePanel); });

    function navigate(delta) {
        var next = panelState.index + delta;
        if (next < 0 || next >= panelState.siblings.length) return;
        panelState.index = next;
        panelState.id = panelState.siblings[next].id;
        renderPanel(panelState.siblings[next]);
    }
    document.getElementById("gl-v2-dp-panel-prev").addEventListener("click", function () { navigate(-1); });
    document.getElementById("gl-v2-dp-panel-next").addEventListener("click", function () { navigate(1); });
    document.addEventListener("keydown", function (e) {
        if (panel.hidden) return;
        var tag = document.activeElement ? document.activeElement.tagName : "";
        if (tag === "INPUT" || tag === "TEXTAREA") { if (e.key === "Escape") closePanel(); return; }
        if (e.key === "Escape") closePanel();
        else if (e.key === "ArrowLeft") navigate(-1);
        else if (e.key === "ArrowRight") navigate(1);
    });

    var panelPosterBtn = document.getElementById("gl-v2-dp-panel-poster");
    if (panelPosterBtn) panelPosterBtn.addEventListener("click", function () {
        var video = document.querySelector("#gl-v2-dp-panel-media video");
        if (!video) return;
        panelState.pendingPosterTimestamp = video.currentTime;
        var existing = document.querySelector("#gl-v2-dp-panel-media .gl-v2-dp-panel-poster-chip");
        if (existing) existing.remove();
        var chip = document.createElement("span");
        chip.className = "gl-v2-dp-panel-poster-chip";
        chip.textContent = "posterbeeld op " + formatDuration(panelState.pendingPosterTimestamp);
        document.getElementById("gl-v2-dp-panel-media").appendChild(chip);
    });

    var panelSaveBtn = document.getElementById("gl-v2-dp-panel-save");
    if (panelSaveBtn) panelSaveBtn.addEventListener("click", function () {
        var item = mediaById(panelState.id);
        if (!item) return;
        var body = {
            MediaId: item.id,
            ProjectId: cfg.projectId,
            Title: document.getElementById("gl-v2-dp-panel-title-input").value,
            AltText: item.mediaType === 0 ? document.getElementById("gl-v2-dp-panel-text-input").value : item.altText,
            Subtitle: item.mediaType === 1 ? document.getElementById("gl-v2-dp-panel-text-input").value : item.subtitle,
            SectionId: document.getElementById("gl-v2-dp-panel-section-value").value ? parseInt(document.getElementById("gl-v2-dp-panel-section-value").value, 10) : null,
            UnitId: document.getElementById("gl-v2-dp-panel-unit-value").value ? parseInt(document.getElementById("gl-v2-dp-panel-unit-value").value, 10) : null,
            IsPublic: document.getElementById("gl-v2-dp-panel-public").checked,
            AutoPlayMuted: document.getElementById("gl-v2-dp-panel-autoplay").checked,
            IsHoofdbeeld: document.getElementById("gl-v2-dp-panel-hoofdbeeld").checked,
            PosterTimestampSeconds: panelState.pendingPosterTimestamp != null ? panelState.pendingPosterTimestamp : item.posterTimestampSeconds
        };
        var btn = this;
        btn.disabled = true;
        postJson(cfg.urls.updateDetails, body).then(function () { location.reload(); }).catch(function () { btn.disabled = false; });
    });

    // ── Detailpaneel — verwijderen (opent de gedeelde TYPE 1-bevestigmodal) ────────────────────
    var panelDeleteBtn = document.getElementById("gl-v2-dp-panel-delete");
    if (panelDeleteBtn) panelDeleteBtn.addEventListener("click", function (e) {
        e.preventDefault();
        var item = mediaById(panelState.id);
        if (!item) return;
        var modalEl = document.getElementById("gl-v2-dp-delete-modal");
        modalEl.querySelector('input[name="id"]').value = item.id;
        modalEl.querySelector('input[name="type"]').value = item.isHoofdbeeld ? "0" : "1";
        new bootstrap.Modal(modalEl).show();
    });
    // ── Detailpaneel — vervangen (upload nieuw bestand, verwijder het oude, herlaad) ───────────
    var replaceBtn = document.getElementById("gl-v2-dp-panel-replace");
    var replaceFileInput = document.getElementById("gl-v2-dp-panel-replace-file");
    if (replaceBtn && replaceFileInput) {
        replaceBtn.addEventListener("click", function (e) { e.preventDefault(); replaceFileInput.click(); });
        replaceFileInput.addEventListener("change", function () {
            if (!this.files.length) return;
            var item = mediaById(panelState.id);
            if (!item) return;
            if (!confirm('"' + item.title + '" vervangen door "' + this.files[0].name + '"?')) { this.value = ""; return; }
            var fd = new FormData();
            fd.append("file", this.files[0]);
            fd.append("UploadedBO.ProjectId", cfg.projectId);
            fd.append("UploadedBO.Type", "1");
            fd.append("UploadedBO.SectionId", item.sectionId != null ? item.sectionId : "");
            fd.append("UploadedBO.IsPublic", item.isPublic ? "true" : "false");
            fd.append("UploadedBO.Caption", item.title || "");
            fd.append("__RequestVerificationToken", antiForgery);
            fetch(cfg.urls.uploadImage, { method: "POST", body: fd })
                .then(function () {
                    var fd2 = new URLSearchParams();
                    fd2.set("id", item.id); fd2.set("projectid", cfg.projectId); fd2.set("type", item.isHoofdbeeld ? "0" : "1");
                    return fetch(cfg.urls.deletePhoto + "?" + fd2.toString(), { method: "POST", headers: { "RequestVerificationToken": antiForgery } });
                })
                .then(function () { location.reload(); });
        });
    }

    // ── Uploadmodal — dropzone/bestandskeuze (zelfde recept als de legacy pagina) ───────────────
    var uploadModalEl = document.getElementById("gl-v2-dp-upload-modal");
    var uploadModal = uploadModalEl ? new bootstrap.Modal(uploadModalEl) : null;
    var uploadForm = document.getElementById("gl-v2-dp-upload-form");
    var uploadFileInput = document.getElementById("gl-v2-dp-upload-file");
    var uploadDropzone = document.getElementById("gl-v2-dp-upload-dropzone");
    var uploadFilenameLabel = document.getElementById("gl-v2-dp-upload-filename");
    var uploadPlaceholderDesktop = document.getElementById("gl-v2-dp-upload-placeholder-desktop");
    var uploadPlaceholderTouch = document.getElementById("gl-v2-dp-upload-placeholder-touch");
    var uploadSubmit = document.getElementById("gl-v2-dp-upload-submit");
    var uploadSectionSelect = document.getElementById("gl-v2-dp-upload-section-select");

    // De "sleep hierheen"/"klik" placeholders (CSS-getoggled op viewportbreedte, zie
    // gl-v2-projecten-detailphotos.css) en de effectief-gekozen-bestandsnaam delen dezelfde plek —
    // een gekozen bestand toont altijd zijn naam, ongeacht viewportbreedte, dus dat is losse JS-
    // zichtbaarheid (hidden), niet nog een derde CSS-breakpuntvariant.
    function resetUploadFilename() {
        if (uploadFilenameLabel) { uploadFilenameLabel.hidden = true; uploadFilenameLabel.textContent = ""; }
        if (uploadPlaceholderDesktop) uploadPlaceholderDesktop.hidden = false;
        if (uploadPlaceholderTouch) uploadPlaceholderTouch.hidden = false;
    }

    function setUploadFile(file) {
        var dt = new DataTransfer();
        dt.items.add(file);
        uploadFileInput.files = dt.files;
        if (uploadPlaceholderDesktop) uploadPlaceholderDesktop.hidden = true;
        if (uploadPlaceholderTouch) uploadPlaceholderTouch.hidden = true;
        if (uploadFilenameLabel) {
            uploadFilenameLabel.hidden = false;
            uploadFilenameLabel.textContent = file.name + " (" + (file.size / 1024 / 1024).toFixed(2) + " MB)";
        }
        uploadSubmit.disabled = false;
    }

    document.querySelectorAll(".js-gl-v2-dp-upload-open").forEach(function (btn) {
        btn.addEventListener("click", function () {
            if (uploadForm) uploadForm.reset();
            resetUploadFilename();
            uploadSubmit.disabled = true;
            if (uploadModal) uploadModal.show();
        });
    });
    if (uploadDropzone) {
        uploadDropzone.addEventListener("click", function () { uploadFileInput.click(); });
        uploadDropzone.addEventListener("dragover", function (e) { e.preventDefault(); uploadDropzone.classList.add("is-drag-over"); });
        uploadDropzone.addEventListener("dragleave", function () { uploadDropzone.classList.remove("is-drag-over"); });
        uploadDropzone.addEventListener("drop", function (e) {
            e.preventDefault(); uploadDropzone.classList.remove("is-drag-over");
            if (e.dataTransfer.files.length) setUploadFile(e.dataTransfer.files[0]);
        });
    }
    if (uploadFileInput) uploadFileInput.addEventListener("change", function () { if (this.files.length) setUploadFile(this.files[0]); });
    if (uploadForm) uploadForm.addEventListener("submit", function () {
        uploadSubmit.disabled = true;
        uploadSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Bezig …';
    });

    document.querySelectorAll(".gl-v2-dp-section-add").forEach(function (btn) {
        btn.addEventListener("click", function () {
            if (uploadForm) uploadForm.reset();
            resetUploadFilename();
            uploadSubmit.disabled = true;
            if (uploadSectionSelect) uploadSectionSelect.value = btn.getAttribute("data-section-id") || "";
            if (uploadModal) uploadModal.show();
        });
    });

    // ── Sectie-dropzones (rastertegel + lege-sectierij): rechtstreeks uploaden zonder de modal te
    // openen — zet het gedeelde formulier se velden en dient het meteen in. ───────────────────────
    function quickUploadToSection(sectionId, file) {
        if (uploadSectionSelect) uploadSectionSelect.value = sectionId || "";
        var dt = new DataTransfer();
        dt.items.add(file);
        uploadFileInput.files = dt.files;
        uploadForm.submit();
    }
    document.querySelectorAll(".js-gl-v2-dp-dropzone").forEach(function (zone) {
        var sectionId = zone.getAttribute("data-section-id");
        zone.addEventListener("click", function () {
            uploadFileInput.value = "";
            var handler = function () { if (uploadFileInput.files.length) quickUploadToSection(sectionId, uploadFileInput.files[0]); uploadFileInput.removeEventListener("change", handler); };
            uploadFileInput.addEventListener("change", handler);
            uploadFileInput.click();
        });
        zone.addEventListener("dragover", function (e) { e.preventDefault(); zone.classList.add("is-drag-over"); });
        zone.addEventListener("dragleave", function () { zone.classList.remove("is-drag-over"); });
        zone.addEventListener("drop", function (e) {
            e.preventDefault(); zone.classList.remove("is-drag-over");
            if (e.dataTransfer.files.length) quickUploadToSection(sectionId, e.dataTransfer.files[0]);
        });
    });

    // ── Secties beheren ────────────────────────────────────────────────────────────────────────
    var sectionsModalEl = document.getElementById("gl-v2-dp-sections-modal");
    var sectionsModal = sectionsModalEl ? new bootstrap.Modal(sectionsModalEl) : null;
    var sectionsData = (cfg.sections || []).slice();
    var sectionsChanged = false;

    function sectionViewRow(s) {
        return '<div class="gl-v2-dp-sections-row" data-sect-id="' + s.id + '">' +
            '<div class="gl-v2-dp-sections-row-text"><span class="gl-v2-dp-sections-row-name">' + esc(s.name) + "</span>" +
            (s.description ? '<span class="gl-v2-dp-sections-row-desc">' + esc(s.description) + "</span>" : "") + "</div>" +
            '<span class="gl-v2-badge ' + (s.isPublic ? "is-positive" : "is-attention") + '">' + (s.isPublic ? "PUBLIEK" : "INTERN") + "</span>" +
            '<button type="button" class="gl-v2-icon-btn js-gl-v2-dp-sect-edit" title="Bewerken" aria-label="Bewerken"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
            '<button type="button" class="gl-v2-icon-btn js-gl-v2-dp-sect-del" title="Verwijderen" aria-label="Verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>' +
            "</div>";
    }
    function sectionEditRow(s) {
        return '<div class="gl-v2-dp-sections-edit" data-sect-id="' + s.id + '">' +
            '<div class="gl-v2-dp-panel-fields-row">' +
            '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" class="gl-v2-field-input js-name" value="' + esc(s.name) + '" placeholder="Naam *" /></div></div>' +
            '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" class="gl-v2-field-input js-desc" value="' + esc(s.description) + '" placeholder="Beschrijving" /></div></div>' +
            "</div>" +
            '<label class="gl-v2-toggle-row"><input type="checkbox" class="gl-v2-toggle-switch js-public"' + (s.isPublic ? " checked" : "") + " /><span class=\"gl-v2-toggle-text\"><span class=\"gl-v2-toggle-label\">Publiek zichtbaar</span></span></label>" +
            '<div class="gl-v2-dp-sections-edit-actions">' +
            '<button type="button" class="gl-v2-btn gl-v2-btn-primary js-gl-v2-dp-sect-save">Opslaan</button>' +
            '<button type="button" class="gl-v2-btn gl-v2-btn-text js-gl-v2-dp-sect-cancel">Annuleren</button>' +
            "</div></div>";
    }
    function renderSectionsList() {
        var list = document.getElementById("gl-v2-dp-sections-list");
        list.innerHTML = sectionsData.length
            ? sectionsData.map(sectionViewRow).join("")
            : '<p class="gl-v2-empty-state-desc" style="text-align:left;margin:0;padding:10px 2px;">Nog geen secties aangemaakt.</p>';
    }

    document.querySelectorAll(".js-gl-v2-dp-sections-open").forEach(function (btn) {
        btn.addEventListener("click", function () { renderSectionsList(); if (sectionsModal) sectionsModal.show(); });
    });
    if (sectionsModalEl) sectionsModalEl.addEventListener("hidden.bs.modal", function () { if (sectionsChanged) location.reload(); });

    document.addEventListener("click", function (e) {
        var editBtn = e.target.closest(".js-gl-v2-dp-sect-edit");
        if (editBtn) {
            var row = editBtn.closest("[data-sect-id]");
            var s = sectionsData.find(function (x) { return x.id === parseInt(row.getAttribute("data-sect-id"), 10); });
            row.outerHTML = sectionEditRow(s);
            return;
        }
        var cancelBtn = e.target.closest(".js-gl-v2-dp-sect-cancel");
        if (cancelBtn) {
            var row2 = cancelBtn.closest("[data-sect-id]");
            var s2 = sectionsData.find(function (x) { return x.id === parseInt(row2.getAttribute("data-sect-id"), 10); });
            row2.outerHTML = sectionViewRow(s2);
            return;
        }
        var saveBtn = e.target.closest(".js-gl-v2-dp-sect-save");
        if (saveBtn) {
            var row3 = saveBtn.closest("[data-sect-id]");
            var id = parseInt(row3.getAttribute("data-sect-id"), 10);
            var name = row3.querySelector(".js-name").value.trim();
            var desc = row3.querySelector(".js-desc").value.trim();
            var pub = row3.querySelector(".js-public").checked;
            if (!name) { row3.querySelector(".js-name").focus(); return; }
            postJson(cfg.urls.updateSection, { id: id, projectId: cfg.projectId, name: name, description: desc, isPublic: pub }).then(function () {
                var s3 = sectionsData.find(function (x) { return x.id === id; });
                s3.name = name; s3.description = desc; s3.isPublic = pub;
                row3.outerHTML = sectionViewRow(s3);
                sectionsChanged = true;
            });
            return;
        }
        var delBtn = e.target.closest(".js-gl-v2-dp-sect-del");
        if (delBtn) {
            var row4 = delBtn.closest("[data-sect-id]");
            var id4 = parseInt(row4.getAttribute("data-sect-id"), 10);
            var s4 = sectionsData.find(function (x) { return x.id === id4; });
            if (!confirm('Sectie "' + s4.name + '" verwijderen? Media in deze sectie wordt losgekoppeld.')) return;
            fetch(cfg.urls.deleteSection + "?id=" + id4 + "&projectId=" + cfg.projectId, { method: "POST", headers: { "RequestVerificationToken": antiForgery } })
                .then(function () { sectionsData = sectionsData.filter(function (x) { return x.id !== id4; }); sectionsChanged = true; renderSectionsList(); });
        }
    });

    var newSectAddBtn = document.getElementById("gl-v2-dp-newsection-add");
    if (newSectAddBtn) newSectAddBtn.addEventListener("click", function () {
        var name = document.getElementById("gl-v2-dp-newsection-name").value.trim();
        var desc = document.getElementById("gl-v2-dp-newsection-desc").value.trim();
        if (!name) { document.getElementById("gl-v2-dp-newsection-name").focus(); return; }
        postJson(cfg.urls.createSection, { id: 0, projectId: cfg.projectId, name: name, description: desc, isPublic: true })
            .then(function (r) { return r.json(); })
            .then(function (s) {
                sectionsData.push({ id: s.id, name: s.name, description: s.description || "", isPublic: s.isPublic });
                document.getElementById("gl-v2-dp-newsection-name").value = "";
                document.getElementById("gl-v2-dp-newsection-desc").value = "";
                sectionsChanged = true;
                renderSectionsList();
            });
    });
})();
