/* gl-v2 layout-pilot — Projecten/DetailDocsV2 (design-handoff 17a-17e). Bindt: zoeken/filters/groepen, het
   detailpaneel (HTML-fragmenten van Projecten/DocumentPanelV2), de upload-dialoog (nieuw of revisie), verwachte
   documenten, koppelen, delen, ondertekenen, gunnen en verwijderen. Alles wat data wijzigt gaat via fetch naar
   een POST-endpoint (antiforgery-token in de header) en herlaadt daarna de pagina met het paneel weer open —
   de meldingen komen via TempData op de volgende pagina (DocJson in ProjectenController.DocsV2.cs). */
(function () {
    "use strict";

    var cfg = window.glV2DocsConfig;
    var root = document.getElementById("gl-v2-dd");
    if (!cfg || !root) return;

    var panel = document.getElementById("gl-v2-dd-panel");
    var panelBody = document.getElementById("gl-v2-dd-panel-body");
    var backdrop = document.getElementById("gl-v2-dd-backdrop");
    var table = document.getElementById("gl-v2-dd-table");
    var wideQuery = window.matchMedia("(min-width: 1200px)");

    // ── Helpers ────────────────────────────────────────────────────────────────────────────────
    function q(sel, scope) { return (scope || document).querySelector(sel); }
    function qa(sel, scope) { return Array.prototype.slice.call((scope || document).querySelectorAll(sel)); }
    function byId(id) { return document.getElementById(id); }

    function token() {
        var input = q('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function toast(tone, title, body) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body });
    }

    function modalOf(id) {
        var el = byId(id);
        return el ? window.bootstrap.Modal.getOrCreateInstance(el) : null;
    }

    function toParams(obj) {
        var p = new URLSearchParams();
        Object.keys(obj).forEach(function (k) {
            var v = obj[k];
            if (v === undefined || v === null) return;
            if (Array.isArray(v)) { v.forEach(function (x) { p.append(k, x === null || x === undefined ? "" : x); }); }
            else p.append(k, v);
        });
        return p;
    }

    /** POST (form-encoded of FormData) → { ok, message, id }. Netwerk-/serverfouten worden een gewone fout. */
    function post(url, data) {
        var body = data instanceof FormData ? data : toParams(data || {});
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: body
        }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        }).catch(function () {
            return { ok: false, message: "De actie is mislukt (netwerk- of serverfout). Probeer opnieuw." };
        });
    }

    function pageUrl(params) {
        var u = new URL(window.location.href);
        ["open", "request"].forEach(function (k) { u.searchParams.delete(k); });
        Object.keys(params || {}).forEach(function (k) { if (params[k]) u.searchParams.set(k, params[k]); });
        return u.toString();
    }

    function reload(params) { window.location.assign(pageUrl(params)); }

    function pad2(n) { return n < 10 ? "0" + n : "" + n; }
    function isoToDisplay(iso) {
        var m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso || "");
        return m ? m[3] + "/" + m[2] + "/" + m[1] : "";
    }
    /** Zet een _DateField (verborgen ISO-waarde + tekstveld) programmatisch. */
    function setDate(id, iso) {
        var hidden = byId(id), text = byId(id + "_text");
        if (hidden) hidden.value = iso || "";
        if (text) text.value = isoToDisplay(iso);
    }

    function showError(id, msg) {
        var el = byId(id);
        if (!el) return;
        el.textContent = msg || "";
        el.hidden = !msg;
    }

    function withLoading(btn, fn) {
        if (window.GlV2Modal && btn) window.GlV2Modal.setButtonLoading(btn);
        return fn().then(function (res) {
            if (!res || !res.ok) {
                if (window.GlV2Modal && btn) window.GlV2Modal.clearButtonLoading(btn);
            }
            return res;
        });
    }

    // ── Confirm-dialoog (verwijderen, gunnen, afwijzen, ontkoppelen, handtekening) ────────────
    var confirmState = null;
    function confirmDialog(opts) {
        var m = byId("gl-v2-dd-confirm-modal");
        if (!m) return;
        var tone = opts.tone || "danger";
        var content = byId("gl-v2-dd-confirm-content");
        content.className = "modal-content is-" + tone;
        var icon = byId("gl-v2-dd-confirm-icon");
        icon.className = "gl-v2-modal-icon is-" + tone;
        icon.innerHTML = '<i class="ph ' + (opts.icon || (tone === "danger" ? "ph-trash" : tone === "warning" ? "ph-warning" : "ph-seal-check")) + '" aria-hidden="true"></i>';
        byId("gl-v2-dd-confirm-title").textContent = opts.title || "Zeker weten?";
        byId("gl-v2-dd-confirm-desc").textContent = opts.desc || "";
        var noteField = byId("gl-v2-dd-confirm-note-field");
        noteField.hidden = !opts.withNote;
        byId("gl-v2-dd-confirm-note").value = "";
        var ok = byId("gl-v2-dd-confirm-ok");
        ok.className = "gl-v2-btn " + (tone === "danger" ? "gl-v2-btn-danger" : tone === "warning" ? "gl-v2-btn-warning" : "gl-v2-btn-primary");
        ok.textContent = opts.okText || "Bevestigen";
        ok.disabled = false;
        confirmState = opts;
        modalOf("gl-v2-dd-confirm-modal").show();
    }

    (function bindConfirm() {
        var ok = byId("gl-v2-dd-confirm-ok");
        if (!ok) return;
        ok.addEventListener("click", function () {
            if (!confirmState || !confirmState.onOk) return;
            var note = byId("gl-v2-dd-confirm-note").value;
            var handler = confirmState.onOk;
            ok.disabled = true;
            handler(note, ok).then(function (res) {
                if (res && res.ok) return;
                ok.disabled = false;
                if (res) modalOf("gl-v2-dd-confirm-modal").hide(); // de foutmelding toonde done() al
            });
        });
    })();

    /** Standaard afhandeling van een mutatie: bij succes herladen (paneel open), bij fout een toast. */
    function done(res, params) {
        if (res && res.ok) { reload(params); return res; }
        toast("danger", "Niet gelukt", (res && res.message) || "Er ging iets mis.");
        return res;
    }

    // ── Filters, zoeken en groepen ────────────────────────────────────────────────────────────
    var state = { search: "", scope: "", aud: "", unit: "", perceel: "" };
    var collapsed = {};

    function refresh() {
        if (!table) return;
        var rows = qa("[data-doc-row]", table);
        var visibleByGroup = {};
        var shown = {};
        var terms = state.search.toLowerCase().split(/\s+/).filter(Boolean);
        rows.forEach(function (row) {
            var ok = true;
            var hay = row.getAttribute("data-search") || "";
            terms.forEach(function (t) { if (hay.indexOf(t) < 0) ok = false; });
            if (ok && state.scope) ok = (" " + row.getAttribute("data-scopes") + " ").indexOf(" " + state.scope + " ") >= 0;
            if (ok && state.aud) {
                var a = " " + row.getAttribute("data-audiences") + " ";
                if (state.aud === "niet") ok = a.indexOf(" klant ") < 0 && a.indexOf(" leverancier ") < 0 && row.getAttribute("data-kind") === "doc";
                else ok = a.indexOf(" " + state.aud + " ") >= 0;
            }
            if (ok && state.unit) ok = ("," + row.getAttribute("data-units") + ",").indexOf("," + state.unit + ",") >= 0;
            if (ok && state.perceel) ok = row.getAttribute("data-perceel") === state.perceel;
            var group = row.getAttribute("data-group");
            if (ok) { visibleByGroup[group] = (visibleByGroup[group] || 0) + 1; shown[row.getAttribute("data-kind") + row.getAttribute("data-id")] = 1; }
            row.hidden = !ok || !!collapsed[group];
        });
        qa("[data-group-row]", table).forEach(function (g) {
            g.hidden = !visibleByGroup[g.getAttribute("data-group")];
        });
        var none = byId("gl-v2-dd-no-results");
        var total = Object.keys(shown).length;
        if (none) none.hidden = total > 0;
        var label = byId("gl-v2-dd-shown");
        if (label && (state.search || state.scope || state.aud || state.unit || state.perceel)) label.textContent = total + " gevonden · gefilterd";
    }

    var searchInput = byId("gl-v2-dd-search");
    var searchClear = byId("gl-v2-dd-search-clear");
    if (searchInput) {
        searchInput.addEventListener("input", function () {
            state.search = searchInput.value.trim();
            if (searchClear) searchClear.hidden = !state.search;
            refresh();
        });
    }
    if (searchClear) {
        searchClear.hidden = true;
        searchClear.addEventListener("click", function () { searchInput.value = ""; state.search = ""; searchClear.hidden = true; refresh(); searchInput.focus(); });
    }

    qa(".gl-v2-dd-chipbtn").forEach(function (b) {
        b.addEventListener("click", function () {
            if (b.disabled) return;
            state.scope = b.getAttribute("data-scope") || "";
            qa(".gl-v2-dd-chipbtn").forEach(function (x) { x.classList.toggle("is-active", x === b); });
            refresh();
        });
    });

    function bindMenuFilter(menuId, attr, key, labelId, btnId, allLabel) {
        var menu = byId(menuId);
        if (!menu) return;
        menu.addEventListener("click", function (e) {
            var item = e.target.closest("[data-" + attr + "]");
            if (!item) return;
            e.preventDefault();
            var v = item.getAttribute("data-" + attr) || "";
            state[key] = v;
            var label = byId(labelId);
            if (label) label.textContent = v ? item.textContent.trim() : allLabel;
            var btn = byId(btnId);
            if (btn) btn.classList.toggle("is-set", !!v);
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            refresh();
        });
    }
    bindMenuFilter("gl-v2-dd-aud-menu", "aud", "aud", "gl-v2-dd-aud-label", "gl-v2-dd-aud-btn", "iedereen");
    bindMenuFilter("gl-v2-dd-unit-menu", "unit", "unit", "gl-v2-dd-unit-label", "gl-v2-dd-unit-btn", "alle");
    bindMenuFilter("gl-v2-dd-perceel-menu", "perceel", "perceel", "gl-v2-dd-perceel-label", "gl-v2-dd-perceel-btn", "alle");

    if (table) {
        table.addEventListener("click", function (e) {
            var toggle = e.target.closest("[data-group-toggle]");
            if (toggle) {
                var key = toggle.getAttribute("data-group-toggle");
                collapsed[key] = !collapsed[key];
                toggle.setAttribute("aria-expanded", collapsed[key] ? "false" : "true");
                refresh();
                return;
            }
            // Klik op de rij (buiten knoppen/menu's/links) opent het paneel
            if (e.target.closest(".js-gl-v2-menu-trigger, .gl-v2-menu, a")) return;
            var row = e.target.closest("[data-doc-row]");
            if (row) openPanel(row.getAttribute("data-kind"), row.getAttribute("data-id"));
        });
    }

    // ── Detailpaneel ──────────────────────────────────────────────────────────────────────────
    var panelCtx = { kind: null, id: null };

    function markSelected() {
        qa("[data-doc-row]").forEach(function (r) {
            r.classList.toggle("is-selected", r.getAttribute("data-kind") === panelCtx.kind && r.getAttribute("data-id") === String(panelCtx.id));
        });
    }

    function openPanel(kind, id) {
        if (!panel || !id) return;
        panelCtx = { kind: kind, id: id };
        markSelected();
        panelBody.innerHTML = '<div class="gl-v2-dd-panel-loading">Laden …</div>';
        panel.hidden = false;
        root.classList.add("is-panel-open");
        if (!wideQuery.matches) {
            backdrop.hidden = false;
            requestAnimationFrame(function () { panel.classList.add("is-open"); });
        } else panel.classList.add("is-open");
        var url = (kind === "request" ? cfg.urls.requestPanel : cfg.urls.panel) + "?projectid=" + cfg.projectId + "&id=" + encodeURIComponent(id);
        fetch(url, { credentials: "same-origin" }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.text();
        }).then(function (html) {
            if (panelCtx.id !== id) return;
            panelBody.innerHTML = html;
        }).catch(function () {
            panelBody.innerHTML = '<div class="gl-v2-dd-panel-loading">Het detail kon niet geladen worden.</div>';
        });
    }

    function closePanel() {
        if (!panel || panel.hidden) return;
        panel.classList.remove("is-open");
        if (backdrop) backdrop.hidden = true;
        panelCtx = { kind: null, id: null };
        markSelected();
        root.classList.remove("is-panel-open");
        if (wideQuery.matches) panel.hidden = true;
        else setTimeout(function () { if (!panel.classList.contains("is-open")) panel.hidden = true; }, 230);
    }

    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-gl-v2-dd-close")) { e.preventDefault(); closePanel(); }
        var opener = e.target.closest("[data-open-panel]");
        if (opener) {
            e.preventDefault();
            var row = opener.closest("[data-doc-row]");
            var kind = opener.getAttribute("data-kind") || (row && row.getAttribute("data-kind"));
            var id = opener.getAttribute("data-id") || (row && row.getAttribute("data-id"));
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            if (kind && id) openPanel(kind, id);
        }
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && panel && !panel.hidden && !q(".modal.show")) closePanel();
    });
    wideQuery.addEventListener("change", function () {
        // Bij wisselen van breedte de open/dicht-toestand van het paneel opnieuw rechtzetten
        if (!panel || panel.hidden) return;
        if (wideQuery.matches) { backdrop.hidden = true; panel.classList.add("is-open"); }
        else { backdrop.hidden = false; panel.classList.add("is-open"); }
    });

    // ── Upload-dialoog (17b) ──────────────────────────────────────────────────────────────────
    var up = {
        form: byId("gl-v2-dd-upload-form"),
        file: byId("gl-v2-dd-up-file"),
        drop: byId("gl-v2-dd-up-dropzone"),
        links: [],           // [{ key: "unit:12", label: "Lot 1", type: "unit" }]
        options: null        // cache van DocumentReplaceOptionsV2
    };

    function upModeRadio() { var r = q('input[name="whatIsThis"]:checked', up.form); return r ? r.value : "new"; }

    function upRenderLinks() {
        var host = byId("gl-v2-dd-up-linkchips");
        host.innerHTML = "";
        up.links.forEach(function (l) {
            var chip = document.createElement("span");
            chip.className = "gl-v2-dd-linkchip is-" + (l.type === "company" ? "company" : l.type);
            chip.appendChild(document.createTextNode(l.label));
            var b = document.createElement("button");
            b.type = "button";
            b.setAttribute("aria-label", l.label + " ontkoppelen");
            b.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
            b.addEventListener("click", function () {
                up.links = up.links.filter(function (x) { return x.key !== l.key; });
                upRenderLinks();
            });
            chip.appendChild(b);
            host.appendChild(chip);
        });
        var hasClient = up.links.some(function (l) { return l.type === "unit" || l.type === "client"; });
        var hasCompany = up.links.some(function (l) { return l.type === "company"; });
        byId("gl-v2-dd-up-share-clients-hint").textContent = hasClient ? "zichtbaar voor de koper van de gekoppelde eenheid of de gekoppelde klant" : "zonder koppeling: zichtbaar voor alle kopers van het project (bv. een brochure)";
        var sup = byId("gl-v2-dd-up-share-suppliers");
        sup.disabled = !hasCompany;
        if (!hasCompany) sup.checked = false;
        byId("gl-v2-dd-up-share-suppliers-hint").textContent = hasCompany ? "zichtbaar voor de gekoppelde leverancier(s)" : "koppel eerst een leverancier";
    }

    function upAddLink(key, label) {
        if (!key) return;
        var type = key.split(":")[0];
        if (up.links.some(function (l) { return l.key === key; })) return;
        up.links.push({ key: key, label: label, type: type });
        upRenderLinks();
    }

    function upSuggestFor(select, boxId, textId, yesId) {
        var opt = select.options[select.selectedIndex];
        var box = byId(boxId);
        if (!opt || !opt.getAttribute("data-buyer") || opt.value.indexOf("unit:") !== 0) { if (box) box.hidden = true; return null; }
        var buyerId = opt.getAttribute("data-buyer");
        var buyerName = opt.getAttribute("data-buyer-name");
        if (!buyerId || !buyerName) { if (box) box.hidden = true; return null; }
        byId(textId).textContent = buyerName + " is koper van " + (opt.getAttribute("data-label") || opt.textContent.trim()) + " — ook koppelen?";
        if (box) box.hidden = false;
        return { key: "client:" + buyerId, label: buyerName };
    }

    var upSuggestion = null;
    var upAddSelect = byId("gl-v2-dd-up-linkadd");
    if (upAddSelect) {
        upAddSelect.addEventListener("change", function () {
            var opt = upAddSelect.options[upAddSelect.selectedIndex];
            if (!opt || !opt.value) return;
            upAddLink(opt.value, opt.getAttribute("data-label") || opt.textContent.trim());
            var sug = upSuggestFor(upAddSelect, "gl-v2-dd-up-suggest", "gl-v2-dd-up-suggest-text");
            upSuggestion = sug && !up.links.some(function (l) { return l.key === sug.key; }) ? sug : null;
            if (!upSuggestion) byId("gl-v2-dd-up-suggest").hidden = true;
            upAddSelect.selectedIndex = 0;
        });
    }
    var upSuggestYes = byId("gl-v2-dd-up-suggest-yes");
    if (upSuggestYes) upSuggestYes.addEventListener("click", function () {
        if (upSuggestion) upAddLink(upSuggestion.key, upSuggestion.label);
        upSuggestion = null;
        byId("gl-v2-dd-up-suggest").hidden = true;
    });

    function upSetFile(file) {
        if (!file) return;
        var dt;
        try { dt = new DataTransfer(); dt.items.add(file); up.file.files = dt.files; } catch (err) { /* oudere browsers: enkel via klik */ }
        up.drop.classList.add("has-file");
        byId("gl-v2-dd-up-filename").textContent = file.name;
        byId("gl-v2-dd-up-filesize").textContent = file.size >= 1048576 ? (file.size / 1048576).toFixed(1).replace(".", ",") + " MB · klik om een ander bestand te kiezen" : Math.max(1, Math.round(file.size / 1024)) + " kB · klik om een ander bestand te kiezen";
        var name = byId("gl-v2-dd-up-name");
        if (name && !name.value.trim() && upModeRadio() === "new" && !byId("gl-v2-dd-up-request").value) name.value = file.name.replace(/\.[^.]+$/, "");
    }

    if (up.drop) {
        up.drop.addEventListener("click", function () { up.file.click(); });
        up.drop.addEventListener("keydown", function (e) { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); up.file.click(); } });
        up.file.addEventListener("change", function () { if (up.file.files && up.file.files[0]) upSetFile(up.file.files[0]); });
        ["dragenter", "dragover"].forEach(function (ev) { up.drop.addEventListener(ev, function (e) { e.preventDefault(); up.drop.classList.add("is-drag-over"); }); });
        ["dragleave", "drop"].forEach(function (ev) { up.drop.addEventListener(ev, function (e) { e.preventDefault(); up.drop.classList.remove("is-drag-over"); }); });
        up.drop.addEventListener("drop", function (e) { if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0]) upSetFile(e.dataTransfer.files[0]); });
    }

    /** Toont/verbergt velden naargelang nieuw / revisie / vervulling van een verwacht document. */
    function upApplyMode() {
        var revision = upModeRadio() === "revision";
        var hasDoc = !!byId("gl-v2-dd-up-document").value;
        byId("gl-v2-dd-up-replaces-field").hidden = !revision;
        // Bij een revisie van een bestaand document worden naam/map/koppelingen/delen overgenomen
        var inherit = revision && hasDoc;
        qa("[data-dd-new-only]", up.form).forEach(function (el) { el.hidden = inherit; });
        byId("gl-v2-dd-up-links-field").hidden = inherit;
        byId("gl-v2-dd-up-what-field").hidden = !!byId("gl-v2-dd-up-request").value && !revision;
        byId("gl-v2-dd-up-mode").value = inherit ? "revision" : "new";
        upApplyFolderKind();
    }

    function upApplyFolderKind() {
        var sel = byId("gl-v2-dd-up-folder");
        var opt = sel.options[sel.selectedIndex];
        var offer = (opt && opt.getAttribute("data-kind") === "offertes") || (upModeRadio() === "revision" && !!byId("gl-v2-dd-up-document").value);
        byId("gl-v2-dd-up-offer").style.display = offer ? "contents" : "none";
        var perceelWrap = byId("gl-v2-dd-up-perceel").closest(".gl-v2-field");
        if (perceelWrap) perceelWrap.hidden = upModeRadio() === "revision" && !!byId("gl-v2-dd-up-document").value;
    }

    function upLoadOptions() {
        if (up.options) return Promise.resolve(up.options);
        return fetch(cfg.urls.replaceOptions + "?projectid=" + cfg.projectId, { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (list) { up.options = list; return list; })
            .catch(function () { up.options = []; return []; });
    }

    function upFillReplaces(selected) {
        return upLoadOptions().then(function (list) {
            var sel = byId("gl-v2-dd-up-replaces");
            sel.innerHTML = '<option value="">Kies een document …</option>';
            list.forEach(function (o) {
                if (o.frozen) return;
                var opt = document.createElement("option");
                opt.value = (o.kind === "request" ? "req:" : "doc:") + o.id;
                opt.textContent = o.name + (o.missing ? " · ontbreekt" : "") + " — " + o.folder + (o.units && o.units.length ? " · " + o.units.join(", ") : "");
                opt.setAttribute("data-name", o.name);
                sel.appendChild(opt);
            });
            if (selected) sel.value = selected;
        });
    }

    var upReplaces = byId("gl-v2-dd-up-replaces");
    if (upReplaces) upReplaces.addEventListener("change", function () {
        var v = upReplaces.value;
        byId("gl-v2-dd-up-document").value = "";
        byId("gl-v2-dd-up-request").value = "";
        if (v.indexOf("doc:") === 0) byId("gl-v2-dd-up-document").value = v.substring(4);
        if (v.indexOf("req:") === 0) {
            byId("gl-v2-dd-up-request").value = v.substring(4);
            var opt = upReplaces.options[upReplaces.selectedIndex];
            byId("gl-v2-dd-up-name").value = opt.getAttribute("data-name") || "";
        }
        upApplyMode();
    });
    qa('input[name="whatIsThis"]', up.form).forEach(function (r) {
        r.addEventListener("change", function () {
            if (upModeRadio() === "revision") upFillReplaces(); else { byId("gl-v2-dd-up-document").value = ""; byId("gl-v2-dd-up-request").value = ""; }
            upApplyMode();
        });
    });
    var upFolder = byId("gl-v2-dd-up-folder");
    if (upFolder) upFolder.addEventListener("change", upApplyFolderKind);

    function openUpload(opts) {
        opts = opts || {};
        up.form.reset();
        up.links = [];
        upSuggestion = null;
        byId("gl-v2-dd-up-suggest").hidden = true;
        up.drop.classList.remove("has-file");
        byId("gl-v2-dd-up-filename").textContent = "Sleep een bestand hierheen of klik om te kiezen";
        byId("gl-v2-dd-up-filesize").textContent = "PDF, DWG, Word, Excel, afbeeldingen …";
        showError("gl-v2-dd-up-error", "");
        setDate("gl-v2-dd-up-date", new Date().getFullYear() + "-" + pad2(new Date().getMonth() + 1) + "-" + pad2(new Date().getDate()));
        setDate("gl-v2-dd-up-expires", "");
        byId("gl-v2-dd-up-document").value = opts.documentId || "";
        byId("gl-v2-dd-up-request").value = opts.requestId || "";
        byId("gl-v2-dd-up-mode").value = opts.documentId ? "revision" : "new";
        if (cfg.activeFolderId) byId("gl-v2-dd-up-folder").value = String(cfg.activeFolderId);

        // Standaard koppelen aan het actieve entiteitsfilter (bv. ?unit=12)
        if (!opts.documentId) {
            if (cfg.filterUnitId) { var uo = q('#gl-v2-dd-up-linkadd option[value="unit:' + cfg.filterUnitId + '"]'); if (uo) upAddLink(uo.value, uo.getAttribute("data-label")); }
            if (cfg.filterClientId) { var co = q('#gl-v2-dd-up-linkadd option[value="client:' + cfg.filterClientId + '"]'); if (co) upAddLink(co.value, co.getAttribute("data-label")); }
            if (cfg.filterCompanyId) { var po = q('#gl-v2-dd-up-linkadd option[value="company:' + cfg.filterCompanyId + '"]'); if (po) upAddLink(po.value, po.getAttribute("data-label")); }
        }
        upRenderLinks();

        var revisionRadio = q('input[name="whatIsThis"][value="' + (opts.documentId ? "revision" : "new") + '"]', up.form);
        if (revisionRadio) revisionRadio.checked = true;
        if (opts.documentId) {
            byId("gl-v2-dd-upload-title").textContent = "Nieuwe revisie uploaden";
            upFillReplaces("doc:" + opts.documentId).then(upApplyMode);
        } else if (opts.requestId) {
            byId("gl-v2-dd-upload-title").textContent = "Verwacht document uploaden";
            byId("gl-v2-dd-up-name").value = opts.requestName || "";
            if (opts.folderId) byId("gl-v2-dd-up-folder").value = String(opts.folderId);
            if (opts.unitId) { var ro = q('#gl-v2-dd-up-linkadd option[value="unit:' + opts.unitId + '"]'); if (ro) upAddLink(ro.value, ro.getAttribute("data-label")); }
            if (opts.companyId) { var ro2 = q('#gl-v2-dd-up-linkadd option[value="company:' + opts.companyId + '"]'); if (ro2) upAddLink(ro2.value, ro2.getAttribute("data-label")); }
            if (opts.expiryYears && opts.dateIso) { /* vervaldatum volgt server-side uit de aanvraag */ }
            upApplyMode();
        } else {
            byId("gl-v2-dd-upload-title").textContent = "Document uploaden";
            upApplyMode();
        }
        modalOf("gl-v2-dd-upload-modal").show();
    }

    if (up.form) up.form.addEventListener("submit", function (e) {
        e.preventDefault();
        showError("gl-v2-dd-up-error", "");
        var mode = byId("gl-v2-dd-up-mode").value;
        var documentId = byId("gl-v2-dd-up-document").value;
        var requestId = byId("gl-v2-dd-up-request").value;
        if (!up.file.files || !up.file.files[0]) return showError("gl-v2-dd-up-error", "Kies eerst een bestand.");
        if (upModeRadio() === "revision" && !documentId && !requestId) return showError("gl-v2-dd-up-error", "Kies het document dat je vervangt.");
        if (mode !== "revision" && !requestId && !byId("gl-v2-dd-up-name").value.trim()) return showError("gl-v2-dd-up-error", "Geef het document een naam.");
        var fd = new FormData(up.form);
        fd.delete("whatIsThis");
        up.links.forEach(function (l) { fd.append("links", l.key); });
        var btn = byId("gl-v2-dd-up-submit");
        withLoading(btn, function () { return post(cfg.urls.upload, fd); }).then(function (res) {
            if (res.ok) reload({ open: res.id });
            else showError("gl-v2-dd-up-error", res.message);
        });
    });

    // ── Verwacht document (aanvragen) ─────────────────────────────────────────────────────────
    var rq = { form: byId("gl-v2-dd-request-form") };

    function rqApplyKind() {
        var kind = byId("gl-v2-dd-rq-kind").value;
        qa("[data-rq-who]", rq.form).forEach(function (el) {
            el.hidden = (" " + el.getAttribute("data-rq-who") + " ").indexOf(" " + kind + " ") < 0;
        });
    }
    if (rq.form) byId("gl-v2-dd-rq-kind").addEventListener("change", rqApplyKind);

    function openRequest(prefill, sendNow) {
        prefill = prefill || {};
        rq.form.reset();
        showError("gl-v2-dd-rq-error", "");
        byId("gl-v2-dd-rq-id").value = prefill.id || "";
        byId("gl-v2-dd-request-title").textContent = prefill.id ? (sendNow ? "Document aanvragen" : "Aanvraag bewerken") : "Document aanvragen";
        byId("gl-v2-dd-rq-name").value = prefill.name || "";
        if (prefill.folderId || cfg.activeFolderId) byId("gl-v2-dd-rq-folder").value = String(prefill.folderId || cfg.activeFolderId);
        byId("gl-v2-dd-rq-unit").value = prefill.unitId || (prefill.id ? "" : (cfg.filterUnitId || ""));
        byId("gl-v2-dd-rq-kind").value = prefill.kind && prefill.kind !== "" ? prefill.kind : "leverancier";
        byId("gl-v2-dd-rq-company").value = prefill.companyId || (prefill.id ? "" : (cfg.filterCompanyId || ""));
        byId("gl-v2-dd-rq-client").value = prefill.clientId || (prefill.id ? "" : (cfg.filterClientId || ""));
        byId("gl-v2-dd-rq-who").value = prefill.who || "";
        setDate("gl-v2-dd-rq-due", prefill.due || "");
        byId("gl-v2-dd-rq-duelabel").value = prefill.dueLabel || "";
        byId("gl-v2-dd-rq-remind").value = prefill.remind || "";
        byId("gl-v2-dd-rq-expiry").value = prefill.expiry || "";
        byId("gl-v2-dd-rq-perceel").value = prefill.perceel || "";
        byId("gl-v2-dd-rq-note").value = prefill.note || "";
        byId("gl-v2-dd-rq-shareafter").checked = prefill.shareAfter !== "0";
        byId("gl-v2-dd-rq-sendnow").checked = !!sendNow || (!prefill.id && prefill.kind !== "intern");
        rqApplyKind();
        modalOf("gl-v2-dd-request-modal").show();
    }

    if (rq.form) rq.form.addEventListener("submit", function (e) {
        e.preventDefault();
        showError("gl-v2-dd-rq-error", "");
        if (!byId("gl-v2-dd-rq-name").value.trim()) return showError("gl-v2-dd-rq-error", "Geef het gevraagde document een naam.");
        var kind = byId("gl-v2-dd-rq-kind").value;
        if (byId("gl-v2-dd-rq-sendnow").checked && kind === "leverancier" && !byId("gl-v2-dd-rq-company").value)
            return showError("gl-v2-dd-rq-error", "Kies een leverancier om de aanvraag naar te sturen.");
        var fd = new FormData(rq.form);
        var params = new URLSearchParams();
        fd.forEach(function (v, k) { params.append(k, v); });
        withLoading(byId("gl-v2-dd-rq-submit"), function () { return post(cfg.urls.requestSave, params); }).then(function (res) {
            if (res.ok) reload({ request: res.id });
            else showError("gl-v2-dd-rq-error", res.message);
        });
    });

    // ── Gegevens bewerken ─────────────────────────────────────────────────────────────────────
    var ed = { form: byId("gl-v2-dd-edit-form") };
    function openEdit(root) {
        byId("gl-v2-dd-ed-id").value = root.getAttribute("data-doc-id");
        byId("gl-v2-dd-ed-name").value = root.getAttribute("data-doc-name") || "";
        byId("gl-v2-dd-ed-folder").value = root.getAttribute("data-folder-id") || "";
        byId("gl-v2-dd-ed-number").value = root.getAttribute("data-number") || "";
        byId("gl-v2-dd-ed-author").value = root.getAttribute("data-author") || "";
        byId("gl-v2-dd-ed-perceel").value = root.getAttribute("data-perceel") || "";
        setDate("gl-v2-dd-ed-date", root.getAttribute("data-date") || "");
        setDate("gl-v2-dd-ed-expires", root.getAttribute("data-expires") || "");
        showError("gl-v2-dd-ed-error", "");
        modalOf("gl-v2-dd-edit-modal").show();
    }
    if (ed.form) ed.form.addEventListener("submit", function (e) {
        e.preventDefault();
        if (!byId("gl-v2-dd-ed-name").value.trim()) return showError("gl-v2-dd-ed-error", "Geef het document een naam.");
        var params = new URLSearchParams(new FormData(ed.form));
        withLoading(byId("gl-v2-dd-ed-submit"), function () { return post(cfg.urls.update, params); }).then(function (res) {
            if (res.ok) reload({ open: byId("gl-v2-dd-ed-id").value }); else showError("gl-v2-dd-ed-error", res.message);
        });
    });

    // ── Koppelen ──────────────────────────────────────────────────────────────────────────────
    var lk = { form: byId("gl-v2-dd-link-form") };
    function openLink(docId) {
        byId("gl-v2-dd-lk-doc").value = docId;
        byId("gl-v2-dd-lk-target").selectedIndex = 0;
        byId("gl-v2-dd-lk-share").checked = false;
        byId("gl-v2-dd-lk-suggest").hidden = true;
        showError("gl-v2-dd-lk-error", "");
        modalOf("gl-v2-dd-link-modal").show();
    }
    var lkTarget = byId("gl-v2-dd-lk-target");
    if (lkTarget) lkTarget.addEventListener("change", function () {
        var opt = lkTarget.options[lkTarget.selectedIndex];
        var box = byId("gl-v2-dd-lk-suggest");
        if (opt && opt.value.indexOf("unit:") === 0 && opt.getAttribute("data-buyer-name")) {
            byId("gl-v2-dd-lk-suggest-text").textContent = opt.getAttribute("data-buyer-name") + " is koper van " + opt.textContent.trim() + ". Koppel het ook aan de klant als de koper het moet kunnen zien.";
            box.hidden = false;
        } else box.hidden = true;
    });
    if (lk.form) lk.form.addEventListener("submit", function (e) {
        e.preventDefault();
        var v = lkTarget.value;
        if (!v) return showError("gl-v2-dd-lk-error", "Kies waaraan je het document wil koppelen.");
        var parts = v.split(":");
        var docId = byId("gl-v2-dd-lk-doc").value;
        withLoading(byId("gl-v2-dd-lk-submit"), function () {
            return post(cfg.urls.linkAdd, { projectid: cfg.projectId, id: docId, type: parts[0], targetId: parts[1], share: byId("gl-v2-dd-lk-share").checked ? "true" : "false" });
        }).then(function (res) { if (res.ok) reload({ open: docId }); else showError("gl-v2-dd-lk-error", res.message); });
    });

    // ── Ter ondertekening sturen ──────────────────────────────────────────────────────────────
    var sg = { form: byId("gl-v2-dd-sign-form"), rows: byId("gl-v2-dd-sg-rows") };
    function sgAddRow(name, role, clientId) {
        var row = document.createElement("div");
        row.className = "gl-v2-dd-sign-row";
        row.setAttribute("data-client-id", clientId || "");
        row.innerHTML = '<div class="gl-v2-field-box"><input type="text" class="gl-v2-field-input" data-sg="name" maxlength="150" placeholder="Naam" /></div>' +
            '<div class="gl-v2-field-box"><select class="gl-v2-field-input" data-sg="role"><option value="koper">koper</option><option value="verkoper">verkoper</option><option value="notaris">notaris</option><option value="andere">andere</option></select></div>' +
            '<button type="button" class="gl-v2-icon-btn" aria-label="Verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>';
        q('[data-sg="name"]', row).value = name || "";
        q('[data-sg="role"]', row).value = role || "koper";
        q("button", row).addEventListener("click", function () { row.remove(); });
        sg.rows.appendChild(row);
    }
    function openSign(docId, signers) {
        byId("gl-v2-dd-sg-doc").value = docId;
        sg.rows.innerHTML = "";
        (signers || []).forEach(function (s) { sgAddRow(s.name, "koper", s.clientId); });
        sgAddRow(cfg.userName || "", "verkoper", "");
        showError("gl-v2-dd-sg-error", "");
        modalOf("gl-v2-dd-sign-modal").show();
    }
    var sgAdd = byId("gl-v2-dd-sg-add");
    if (sgAdd) sgAdd.addEventListener("click", function () { sgAddRow("", "koper", ""); });
    if (sg.form) sg.form.addEventListener("submit", function (e) {
        e.preventDefault();
        var names = [], roles = [], clients = [];
        qa(".gl-v2-dd-sign-row", sg.rows).forEach(function (r) {
            var n = q('[data-sg="name"]', r).value.trim();
            if (!n) return;
            names.push(n); roles.push(q('[data-sg="role"]', r).value); clients.push(r.getAttribute("data-client-id") || "");
        });
        if (!names.length) return showError("gl-v2-dd-sg-error", "Voeg minstens één ondertekenaar toe.");
        var docId = byId("gl-v2-dd-sg-doc").value;
        withLoading(byId("gl-v2-dd-sg-submit"), function () {
            return post(cfg.urls.signSend, { projectid: cfg.projectId, id: docId, names: names, roles: roles, clientIds: clients });
        }).then(function (res) { if (res.ok) reload({ open: docId }); else showError("gl-v2-dd-sg-error", res.message); });
    });

    // ── Acties in het paneel (delegated) ──────────────────────────────────────────────────────
    function simplePost(url, data, params, btn) {
        return withLoading(btn, function () { return post(url, data); }).then(function (res) { return done(res, params); });
    }

    function panelRoot() { return q("[data-panel-doc]", panelBody) || q("[data-panel-request]", panelBody); }

    function onPanelAct(el) {
        var act = el.getAttribute("data-dd-act");
        var pr = panelRoot();
        if (!pr) return;
        var docId = pr.getAttribute("data-doc-id");
        var reqId = pr.getAttribute("data-request-id");
        var reopen = docId ? { open: docId } : { request: reqId };
        var base = { projectid: cfg.projectId };

        switch (act) {
            case "edit": openEdit(pr); break;
            case "link": openLink(docId); break;
            case "unlink":
                confirmDialog({ tone: "warning", icon: "ph-link-break", title: "Ontkoppelen?", desc: "Het document blijft bestaan, maar verschijnt niet meer bij deze koppeling (en verdwijnt daar uit het portaal).", okText: "Ontkoppelen",
                    onOk: function () { return post(cfg.urls.linkRemove, Object.assign({ id: docId, linkId: el.getAttribute("data-link-id") }, base)).then(function (r) { return done(r, reopen); }); } });
                break;
            case "approve": simplePost(cfg.urls.approve, Object.assign({ revisionId: el.getAttribute("data-revision-id") }, base), reopen, el); break;
            case "reject":
                confirmDialog({ tone: "warning", icon: "ph-x-circle", title: "Revisie " + el.getAttribute("data-label") + " afwijzen?", desc: "De revisie wordt niet de huidige versie. Geef eventueel een reden mee.", okText: "Afwijzen", withNote: true,
                    onOk: function (note) { return post(cfg.urls.reject, Object.assign({ revisionId: el.getAttribute("data-revision-id"), note: note }, base)).then(function (r) { return done(r, reopen); }); } });
                break;
            case "share":
                post(cfg.urls.share, Object.assign({ id: docId, audience: el.getAttribute("data-audience"), share: el.checked ? "true" : "false" }, base)).then(function (r) { done(r, reopen); if (!r.ok) el.checked = !el.checked; });
                break;
            case "link-share":
                post(cfg.urls.linkShare, Object.assign({ id: docId, linkId: el.getAttribute("data-link-id"), share: el.checked ? "true" : "false" }, base)).then(function (r) { done(r, reopen); if (!r.ok) el.checked = !el.checked; });
                break;
            case "new-revision": openUpload({ documentId: docId }); break;
            case "sign-send":
                var signers = []; try { signers = JSON.parse(pr.getAttribute("data-signers") || "[]"); } catch (e) { signers = []; }
                openSign(docId, signers);
                break;
            case "sign-mark":
                confirmDialog({ tone: "success", icon: "ph-signature", title: "Getekend registreren?", desc: el.getAttribute("data-name") + " heeft getekend. Dit wordt handmatig geregistreerd; als alle partijen getekend hebben, is het document bevroren.", okText: "Ja, getekend",
                    onOk: function () { return post(cfg.urls.signMark, Object.assign({ signatureId: el.getAttribute("data-signature-id"), method: "manueel" }, base)).then(function (r) { return done(r, reopen); }); } });
                break;
            case "sign-remind": simplePost(cfg.urls.signRemind, Object.assign({ signatureId: el.getAttribute("data-signature-id") }, base), reopen, el); break;
            case "award":
                confirmDialog({ tone: "success", icon: "ph-seal-check", title: "Offerte gunnen?", desc: "\"" + el.getAttribute("data-name") + "\" wordt gegund. Andere ingediende offertes voor hetzelfde perceel gaan op \"Niet gegund\" en de bestelbon komt als verwacht document klaar.", okText: "Gunnen",
                    onOk: function () { return post(cfg.urls.award, Object.assign({ id: docId }, base)).then(function (r) { return done(r, reopen); }); } });
                break;
            case "delete":
                confirmDialog({ tone: "danger", title: "Document verwijderen?", desc: "\"" + pr.getAttribute("data-doc-name") + "\" en al zijn revisies en koppelingen worden verwijderd. Dit kan niet ongedaan gemaakt worden.", okText: "Verwijderen",
                    onOk: function () { return post(cfg.urls.del, Object.assign({ id: docId }, base)).then(function (r) { return done(r, {}); }); } });
                break;

            // Verwachte documenten
            case "request-edit": openRequest(requestPrefill(pr), false); break;
            case "request-send": openRequest(requestPrefill(pr), true); break;
            case "request-upload":
                openUpload({ requestId: reqId, requestName: pr.getAttribute("data-request-name"), folderId: pr.getAttribute("data-folder-id"), unitId: pr.getAttribute("data-unit-id"), companyId: pr.getAttribute("data-company-id") });
                break;
            case "request-remind": simplePost(cfg.urls.requestRemind, Object.assign({ id: reqId }, base), reopen, el); break;
            case "request-notrequired": simplePost(cfg.urls.requestNotRequired, Object.assign({ id: reqId, notRequired: el.getAttribute("data-not-required") }, base), reopen, el); break;
            case "request-delete":
                confirmDialog({ tone: "danger", title: "Verwacht document verwijderen?", desc: "\"" + pr.getAttribute("data-request-name") + "\" verdwijnt uit de lijst van verwachte documenten.", okText: "Verwijderen",
                    onOk: function () { return post(cfg.urls.requestDelete, Object.assign({ id: reqId }, base)).then(function (r) { return done(r, {}); }); } });
                break;
        }
    }

    function requestPrefill(pr) {
        return {
            id: pr.getAttribute("data-request-id"), name: pr.getAttribute("data-request-name"), folderId: pr.getAttribute("data-folder-id"),
            unitId: pr.getAttribute("data-unit-id"), kind: pr.getAttribute("data-kind"), companyId: pr.getAttribute("data-company-id"),
            clientId: pr.getAttribute("data-client-id"), who: pr.getAttribute("data-who"), due: pr.getAttribute("data-due"),
            dueLabel: pr.getAttribute("data-due-label"), remind: pr.getAttribute("data-remind"), expiry: pr.getAttribute("data-expiry"),
            perceel: pr.getAttribute("data-perceel"), note: pr.getAttribute("data-note"), shareAfter: pr.getAttribute("data-share-after")
        };
    }

    if (panelBody) {
        panelBody.addEventListener("click", function (e) {
            var el = e.target.closest("[data-dd-act]");
            if (!el || el.type === "checkbox") return;
            e.preventDefault();
            onPanelAct(el);
        });
        panelBody.addEventListener("change", function (e) {
            var el = e.target.closest('input[data-dd-act]');
            if (el && el.type === "checkbox") onPanelAct(el);
        });
    }

    // ── Knoppen buiten het paneel (topbar, tabelmenu, lege staat) ─────────────────────────────
    document.addEventListener("click", function (e) {
        var t;
        if ((t = e.target.closest(".js-gl-v2-dd-open-upload"))) { e.preventDefault(); openUpload({}); return; }
        if ((t = e.target.closest(".js-gl-v2-dd-open-request"))) { e.preventDefault(); openRequest({}, false); return; }
        if ((t = e.target.closest(".js-gl-v2-dd-new-revision"))) { e.preventDefault(); if (window.GlV2Menu) window.GlV2Menu.closeAll(); openUpload({ documentId: t.getAttribute("data-doc-id") }); return; }
        if ((t = e.target.closest(".js-gl-v2-dd-upload-for-request"))) {
            e.preventDefault(); if (window.GlV2Menu) window.GlV2Menu.closeAll();
            var row = t.closest("[data-doc-row]");
            openUpload({ requestId: t.getAttribute("data-request-id"), requestName: row ? row.querySelector(".gl-v2-dd-doc-name").textContent : "" });
            return;
        }
        if ((t = e.target.closest(".js-gl-v2-dd-delete-doc"))) {
            e.preventDefault(); if (window.GlV2Menu) window.GlV2Menu.closeAll();
            var docId = t.getAttribute("data-doc-id");
            confirmDialog({ tone: "danger", title: "Document verwijderen?", desc: "\"" + t.getAttribute("data-doc-name") + "\" en al zijn revisies en koppelingen worden verwijderd. Dit kan niet ongedaan gemaakt worden.", okText: "Verwijderen",
                onOk: function () { return post(cfg.urls.del, { projectid: cfg.projectId, id: docId }).then(function (r) { return done(r, {}); }); } });
            return;
        }
        if ((t = e.target.closest(".js-gl-v2-dd-generate"))) {
            e.preventDefault();
            simplePost(cfg.urls.generate, { projectid: cfg.projectId }, {}, t);
        }
    });

    // ── Start ─────────────────────────────────────────────────────────────────────────────────
    refresh();
    var openDoc = root.getAttribute("data-open-doc");
    var openReq = root.getAttribute("data-open-request");
    if (openDoc) openPanel("doc", openDoc);
    else if (openReq) openPanel("request", openReq);
})();
