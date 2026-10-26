// gl-v2 — Dropzone: bestanden slepen of kiezen (projectbreed component, markup GlV2/_Dropzone, opmaak css/gl-v2/dropzone.css).
// Bedraadt elke [data-gl-v2-dropzone] vanzelf (ook later toegevoegde zones); een pagina hoeft niets aan te roepen en luistert enkel naar de events.
//   data-accept   komma-lijst, bv. "application/pdf,.pdf" of "image/*,video/*": extensies (.pdf), mime-types of groepen (image/*)
//   data-max-mb   maximale grootte per bestand in MB (browsercontrole; de server controleert zelf opnieuw)
//   data-mode     row (bestandsrij met Verwijderen) | inline (naam en grootte op de plaats van de tekst) | select (de zone blijft staan, de pagina toont wat gekozen is)
//                 | list (meerdere bestanden onder de zone, kruisje en eventueel een naamveld per bestand; nieuwe bestanden komen erbij)
//   data-preview  miniatuur van een gekozen afbeelding (row en list)       data-paste  een bestand of schermafdruk uit het klembord (Ctrl+V) wordt gekozen
//   data-item-name-field  (list) name van het tekstveld per bestand       data-item-name-placeholder
// Slepen: de zone vangt de drop zelf op, controleert elk bestand en zet de geldige in het echte bestandsveld (ook bij meerdere bestanden); daarna volgt
// een gewoon "change"-event op dat veld, dus bestaande pagina-code die op input.change luistert blijft werken.
// Events (bubbelen vanaf de zone): gl-v2:dropzone-change { root, file, files }   (file = eerste File of null; files = array)
//                                  gl-v2:dropzone-remove-current { root }         (het bestaande bestand werd verwijderd)
//                                  gl-v2:dropzone-progress { root, loaded, total, percent }  /  gl-v2:dropzone-uploaded { root, response }  /  gl-v2:dropzone-upload-error { root, error }
// API: GlV2Dropzone = { init(scope), clear(root), reset(root), getFile(root), getFiles(root), setFiles(root, files), setStatus(root, text, tone), setError(root, text),
//                       upload(root, { url, method, fields, fileField, formData, headers }) → Promise<{ status, ok, text, url }>, cancel(root) }
//   upload: zonder formData bouwt het component zelf een FormData (bestanden onder fileField of de name van het veld, de namen van een lijst, fields en het
//   antiforgery-token); met formData wordt die ongewijzigd verstuurd. Tijdens het uploaden staat er een voortgangsbalk in de zone.
(function () {
    "use strict";

    function size(bytes) { return bytes >= 1048576 ? (bytes / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(bytes / 1024)) + " kB"; }
    function total(list) { return list.reduce(function (s, f) { return s + f.size; }, 0); }
    function accepted(file, accept) {
        if (!accept) return true;
        var name = file.name.toLowerCase(), type = (file.type || "").toLowerCase();
        return accept.split(",").map(function (a) { return a.trim().toLowerCase(); }).filter(Boolean).some(function (a) {
            if (a.charAt(0) === ".") return name.slice(-a.length) === a;
            if (a.slice(-2) === "/*") return type.indexOf(a.slice(0, -1)) === 0;
            return type === a;
        });
    }
    function ext(file) { var m = /\.([a-z0-9]+)$/i.exec(file.name); return m ? m[1].toUpperCase().slice(0, 4) : "BEST."; }
    function baseName(file) { return file.name.replace(/\.[^.]+$/, ""); }
    function isImage(file) { return /^image\//.test(file.type || ""); }
    function same(a, b) { return a.name === b.name && a.size === b.size && a.lastModified === b.lastModified; }
    function el(tag, cls, text) { var e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; }

    function wire(root) {
        if (root._glV2Dropzone) return;
        var input = root.querySelector("input[type=file]:not([data-role=camera-input])"); if (!input) return;
        var q = function (r) { return root.querySelector('[data-role="' + r + '"]'); };
        var empty = q("empty"), cur = q("current"), fileRow = q("file"), nameEl = q("file-name"), metaEl = q("file-meta"), iconEl = q("file-icon"), titleEl = q("title"), subEl = q("sub"), statusEl = q("status");
        var thumb = q("thumb"), thumbImg = thumb ? thumb.querySelector("img") : null, listEl = q("list"), cameraBtn = q("camera"), cameraInput = q("camera-input");
        var err = (function () { var e = input.getAttribute("aria-describedby"); return e ? document.getElementById(e) : null; })();
        var accept = root.getAttribute("data-accept") || "", maxMb = parseFloat(root.getAttribute("data-max-mb") || "") || 0, mode = root.getAttribute("data-mode") || "row";
        var changeText = root.getAttribute("data-change-text") || "", preview = root.hasAttribute("data-preview"), nameField = root.getAttribute("data-item-name-field") || "";
        var namePlaceholder = root.getAttribute("data-item-name-placeholder") || "Naam";
        var currentRemoved = false, queue = [], names = [], thumbUrls = [], xhr = null;
        var idleTitle = titleEl ? titleEl.innerHTML : "", idleSub = subEl ? subEl.textContent : "";
        var internal = false;

        function files() { return input.files ? Array.prototype.slice.call(input.files) : []; }
        function error(msg) {
            if (err) { err.textContent = msg || ""; err.hidden = !msg; }
            root.classList.toggle("has-error", !!msg);
            if (msg) input.setAttribute("aria-invalid", "true"); else input.removeAttribute("aria-invalid");
        }
        function emit() { var f = files(); root.dispatchEvent(new CustomEvent("gl-v2:dropzone-change", { bubbles: true, detail: { root: root, file: f[0] || null, files: f } })); }
        function fireChange() { internal = true; try { input.dispatchEvent(new Event("change", { bubbles: true })); } finally { internal = false; } }
        function summary(list) { return list.length === 1 ? list[0].name : list.length + " bestanden"; }
        function revokeThumbs() { thumbUrls.forEach(function (u) { URL.revokeObjectURL(u); }); thumbUrls = []; }
        function thumbFor(file) { var u = URL.createObjectURL(file); thumbUrls.push(u); return u; }
        function setInputFiles(list) {
            try { var dt = new DataTransfer(); list.forEach(function (f) { dt.items.add(f); }); input.files = dt.files; return true; }
            catch (e) { if (!list.length) { try { input.value = ""; } catch (e2) { /* niet erg */ } } return false; }
        }

        // ── lijst (Mode list): een rij per bestand, met een naamveld als het formulier dat vraagt ──
        function renderList(list) {
            if (!listEl) return;
            // namen bewaren die de gebruiker al typte, per bestand
            Array.prototype.forEach.call(listEl.querySelectorAll("[data-name-input]"), function (inp) { var i = parseInt(inp.getAttribute("data-name-input"), 10); if (queue[i]) names[i] = inp.value; });
            listEl.innerHTML = ""; listEl.hidden = !list.length;
            list.forEach(function (f, i) {
                var row = el("div", "gl-v2-dropzone-item");
                if (preview && isImage(f)) { var t = el("span", "gl-v2-dropzone-thumb"), im = document.createElement("img"); im.alt = ""; im.src = thumbFor(f); t.appendChild(im); row.appendChild(t); }
                else row.appendChild(el("span", "gl-v2-dropzone-type", ext(f)));
                if (nameField) {
                    var field = el("div", "gl-v2-field gl-v2-dropzone-item-name"), box = el("div", "gl-v2-field-box"), inp = document.createElement("input");
                    inp.type = "text"; inp.name = nameField; inp.className = "gl-v2-field-input"; inp.autocomplete = "off"; inp.placeholder = namePlaceholder;
                    inp.setAttribute("aria-label", namePlaceholder + " van " + f.name); inp.setAttribute("data-name-input", String(i));
                    inp.value = names[i] != null ? names[i] : baseName(f);
                    inp.addEventListener("input", function () { names[i] = inp.value; });
                    box.appendChild(inp); field.appendChild(box); row.appendChild(field);
                    var small = el("small", "gl-v2-dropzone-item-meta", f.name + " · " + size(f.size)); row.appendChild(small);
                } else {
                    var txt = el("span", "gl-v2-dropzone-item-text"); txt.appendChild(el("b", null, f.name)); txt.appendChild(el("small", null, size(f.size))); row.appendChild(txt);
                }
                var rm = el("button", "gl-v2-icon-btn is-warning"); rm.type = "button"; rm.setAttribute("aria-label", "Verwijderen: " + f.name); rm.title = "Verwijderen";
                var ic = el("i", "ph ph-trash"); ic.setAttribute("aria-hidden", "true"); rm.appendChild(ic);
                rm.addEventListener("click", function () {
                    if (xhr) return;
                    // de ingetypte namen blijven bij hun bestand
                    Array.prototype.forEach.call(listEl.querySelectorAll("[data-name-input]"), function (inp2) { var k = parseInt(inp2.getAttribute("data-name-input"), 10); names[k] = inp2.value; });
                    queue.splice(i, 1); names.splice(i, 1);
                    setInputFiles(queue); error(""); render(); emit(); fireChange();
                });
                row.appendChild(rm); listEl.appendChild(row);
            });
        }

        function render() {
            var list = mode === "list" ? queue.slice() : files(), has = list.length > 0;
            revokeThumbs();
            root.classList.toggle("has-file", has && mode !== "select" && mode !== "list");
            root.classList.toggle("has-picked", has);
            root.classList.toggle("is-current-removed", currentRemoved);
            if (mode === "row") {
                var showCurrent = !!cur && !currentRemoved && !has;
                if (cur) cur.hidden = !showCurrent;
                if (fileRow) fileRow.hidden = !has;
                if (empty) empty.hidden = has || showCurrent;
                if (has) {
                    nameEl.textContent = summary(list);
                    metaEl.textContent = list.length === 1 ? size(list[0].size) : size(total(list)) + " in totaal";
                    if (thumb && thumbImg) { var img = list.length === 1 && isImage(list[0]); thumb.hidden = !img; if (img) thumbImg.src = thumbFor(list[0]); if (iconEl) iconEl.hidden = img; }
                    var rmb = q("remove"); if (rmb) rmb.setAttribute("aria-label", "Verwijderen: " + summary(list));
                } else if (iconEl) iconEl.hidden = false;
            } else if (mode === "inline") {
                if (titleEl) titleEl.innerHTML = has ? "" : idleTitle;
                if (has && titleEl) titleEl.textContent = summary(list) + (!subEl && changeText ? " · " + changeText : "");
                if (subEl) subEl.textContent = has ? (list.length === 1 ? size(list[0].size) : size(total(list)) + " in totaal") + (changeText ? " · " + changeText : "") : idleSub;
            } else if (mode === "list") {
                renderList(list);
            }
        }

        // ── bestanden controleren en in het echte veld zetten (slepen, kiezen, plakken); ongeldige worden geweigerd met een melding ──
        // Bij kiezen via het venster laten we het eigen change-event van het veld ongemoeid (pagina-code luistert daarop); alleen als er iets
        // geweigerd, bijgesneden of toegevoegd werd, zetten we de geldige bestanden opnieuw in het veld en sturen we zelf een change.
        function take(list, fromNative) {
            var ok = [], bad = [];
            list.forEach(function (f) {
                if (!accepted(f, accept)) bad.push(f.name + ": dit bestandstype wordt niet aanvaard" + (accept ? " (" + accept.replace(/,\s*/g, ", ") + ")" : ""));
                else if (maxMb && f.size > maxMb * 1048576) bad.push(f.name + ": te groot (maximaal " + maxMb + " MB)");
                else ok.push(f);
            });
            var rewrite = !fromNative || ok.length !== list.length;
            if (mode === "list") {
                ok.forEach(function (f) { if (!queue.some(function (g) { return same(g, f); })) { queue.push(f); names.push(null); } });
                setInputFiles(queue); rewrite = true;
            } else {
                if (!input.multiple) ok = ok.slice(0, 1);
                if (rewrite) setInputFiles(ok);
            }
            error(bad.join(" · "));
            render(); emit();
            if (rewrite && (ok.length || mode === "list")) fireChange();
        }

        input.addEventListener("change", function () {
            if (internal) return;
            var list = files();
            if (!list.length) { error(""); if (mode !== "list") { render(); emit(); } return; }
            take(list, true);
        });
        ["dragenter", "dragover"].forEach(function (n) { root.addEventListener(n, function (e) { if (xhr) return; e.preventDefault(); root.classList.add("is-over"); }); });
        root.addEventListener("dragleave", function (e) { if (!root.contains(e.relatedTarget)) root.classList.remove("is-over"); });
        root.addEventListener("drop", function (e) {
            e.preventDefault(); root.classList.remove("is-over");
            if (xhr) return;
            var dropped = e.dataTransfer && e.dataTransfer.files ? Array.prototype.slice.call(e.dataTransfer.files) : [];
            if (dropped.length) take(dropped, false);
        });

        // camera (tablet en gsm): een eigen verborgen veld met capture; het resultaat gaat door dezelfde controle
        if (cameraBtn && cameraInput) {
            cameraBtn.addEventListener("click", function () { cameraInput.click(); });
            cameraInput.addEventListener("change", function () {
                var taken = cameraInput.files ? Array.prototype.slice.call(cameraInput.files) : [];
                try { cameraInput.value = ""; } catch (e) { /* niet erg */ }
                if (taken.length) take(taken, false);
            });
        }

        // plakken (Ctrl+V): een schermafdruk of gekopieerd bestand wordt het gekozen bestand, zolang de zone zichtbaar is
        if (root.hasAttribute("data-paste")) {
            document.addEventListener("paste", function (e) {
                if (!root.isConnected || xhr || root.hidden || !(root.offsetParent || root.getClientRects().length)) return;
                var cd = e.clipboardData; if (!cd) return;
                var found = Array.prototype.slice.call(cd.files || []);
                if (!found.length) Array.prototype.forEach.call(cd.items || [], function (it) { if (it.kind === "file") { var f = it.getAsFile(); if (f) found.push(f); } });
                found = found.filter(function (f) { return accepted(f, accept); });
                if (!found.length) return;
                e.preventDefault(); take(found.slice(0, mode === "list" || input.multiple ? found.length : 1), false);
            });
        }

        function clear() { queue = []; names = []; try { input.value = ""; } catch (e) { /* niet erg */ } error(""); render(); emit(); }
        var rm = q("remove"); if (rm) rm.addEventListener("click", function () { if (!xhr) clear(); });
        var rc = q("remove-current");
        if (rc) rc.addEventListener("click", function () {
            currentRemoved = true; render();
            root.dispatchEvent(new CustomEvent("gl-v2:dropzone-remove-current", { bubbles: true, detail: { root: root } }));
            emit();
        });

        // ── uploaden met voortgang (XMLHttpRequest): de pagina bepaalt de url; antiforgery-token en eventuele namen gaan mee ──
        function progressUi() {
            var p = q("progress");
            if (!p) { p = el("div", "gl-v2-dropzone-progress"); p.setAttribute("data-role", "progress"); p.setAttribute("role", "progressbar"); p.setAttribute("aria-valuemin", "0"); p.setAttribute("aria-valuemax", "100"); p.appendChild(el("i")); p.appendChild(el("span")); root.appendChild(p); }
            return p;
        }
        function upload(opts) {
            opts = opts || {};
            var list = mode === "list" ? queue.slice() : files();
            if (!list.length) return Promise.reject(new Error("Kies eerst een bestand."));
            if (xhr) return Promise.reject(new Error("Er loopt al een upload."));
            var fd = opts.formData;   // een pagina met een eigen formulier geeft het kant-en-klaar mee (inclusief de bestanden)
            if (!fd) {
                var field = opts.fileField || input.name || "file";
                fd = new FormData();
                list.forEach(function (f) { fd.append(field, f); });
                if (nameField && listEl) Array.prototype.forEach.call(listEl.querySelectorAll("[data-name-input]"), function (inp) { fd.append(nameField, inp.value); });
                Object.keys(opts.fields || {}).forEach(function (k) { fd.append(k, opts.fields[k]); });
                var tok = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tok && !fd.has("__RequestVerificationToken")) fd.append("__RequestVerificationToken", tok.value);
            }
            var bar = progressUi(); bar.hidden = false; bar.firstChild.style.transform = "scaleX(0)"; bar.lastChild.textContent = "0 %"; bar.setAttribute("aria-valuenow", "0");
            root.classList.add("is-uploading"); root.setAttribute("aria-busy", "true"); error("");
            return new Promise(function (resolve, reject) {
                var x = new XMLHttpRequest(); xhr = x;
                x.open(opts.method || "POST", opts.url);
                x.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                Object.keys(opts.headers || {}).forEach(function (h) { x.setRequestHeader(h, opts.headers[h]); });
                x.upload.onprogress = function (e) {
                    if (!e.lengthComputable) return;
                    var pct = Math.round(e.loaded / e.total * 100);
                    bar.firstChild.style.transform = "scaleX(" + (pct / 100) + ")"; bar.lastChild.textContent = pct + " %"; bar.setAttribute("aria-valuenow", String(pct));
                    root.dispatchEvent(new CustomEvent("gl-v2:dropzone-progress", { bubbles: true, detail: { root: root, loaded: e.loaded, total: e.total, percent: pct } }));
                };
                function done() { xhr = null; root.classList.remove("is-uploading"); root.removeAttribute("aria-busy"); bar.hidden = true; }
                x.onload = function () {
                    done();
                    var res = { status: x.status, ok: x.status >= 200 && x.status < 300, text: x.responseText, url: x.responseURL };
                    if (res.ok) { root.dispatchEvent(new CustomEvent("gl-v2:dropzone-uploaded", { bubbles: true, detail: { root: root, response: res } })); resolve(res); }
                    else { var e1 = new Error("De upload is mislukt (" + x.status + ")."); error(e1.message); root.dispatchEvent(new CustomEvent("gl-v2:dropzone-upload-error", { bubbles: true, detail: { root: root, error: e1 } })); reject(e1); }
                };
                x.onerror = function () { done(); var e2 = new Error("De upload is mislukt. Controleer je verbinding en probeer opnieuw."); error(e2.message); root.dispatchEvent(new CustomEvent("gl-v2:dropzone-upload-error", { bubbles: true, detail: { root: root, error: e2 } })); reject(e2); };
                x.onabort = function () { done(); reject(new Error("De upload is geannuleerd.")); };
                x.send(fd);
            });
        }
        function cancel() { if (xhr) xhr.abort(); }

        root._glV2Dropzone = {
            clear: clear,
            reset: function () { currentRemoved = false; queue = []; names = []; try { input.value = ""; } catch (e) { /* niet erg */ } error(""); render(); if (statusEl) { statusEl.hidden = true; statusEl.textContent = ""; } },
            files: function () { return mode === "list" ? queue.slice() : files(); },
            setFiles: function (list) { take(Array.prototype.slice.call(list || []), false); },
            setError: function (msg) { error(msg); },
            setStatus: function (text, tone) { if (!statusEl) return; statusEl.textContent = text || ""; statusEl.hidden = !text; statusEl.className = "gl-v2-dropzone-status" + (tone ? " is-" + tone : ""); },
            upload: upload, cancel: cancel
        };
    }
    function init(scope) { Array.prototype.forEach.call((scope || document).querySelectorAll("[data-gl-v2-dropzone]"), wire); }
    function api(root) { return root && root._glV2Dropzone; }

    window.GlV2Dropzone = {
        init: init,
        clear: function (root) { var a = api(root); if (a) a.clear(); },
        reset: function (root) { var a = api(root); if (a) a.reset(); },
        getFile: function (root) { var a = api(root); return a ? (a.files()[0] || null) : null; },
        getFiles: function (root) { var a = api(root); return a ? a.files() : []; },
        setFiles: function (root, files) { var a = api(root); if (a) a.setFiles(files); },
        setStatus: function (root, text, tone) { var a = api(root); if (a) a.setStatus(text, tone); },
        setError: function (root, msg) { var a = api(root); if (a) a.setError(msg); },
        upload: function (root, opts) { var a = api(root); return a ? a.upload(opts) : Promise.reject(new Error("Geen dropzone.")); },
        cancel: function (root) { var a = api(root); if (a) a.cancel(); }
    };
    init(document);
    function boot() {
        init(document);
        if (window.MutationObserver) {
            new MutationObserver(function (ms) {
                ms.forEach(function (m) { Array.prototype.forEach.call(m.addedNodes, function (n) { if (n.nodeType !== 1) return; if (n.matches && n.matches("[data-gl-v2-dropzone]")) wire(n); if (n.querySelectorAll) init(n); }); });
            }).observe(document.documentElement, { childList: true, subtree: true });
        }
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot); else boot();
})();
