// gl-v2 — Account › Mijn profiel: tabbladen, werkdagen, afwezigheid, meldingenmatrix, handtekening (Quill of ruwe HTML met controle en
// live voorbeeld) en één bewaking voor niet-opgeslagen wijzigingen over alle tabbladen heen.
(function () {
    "use strict";
    var form = document.getElementById("FormMijnProfiel");
    if (!form) return;
    var $ = function (id) { return document.getElementById(id); };
    var toast = function (tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); };

    // ── Tabbladen ────────────────────────────────────────────────────────────────────────────────
    var tabInput = $("mp-tab-input"), tabBtns = form.querySelectorAll("[data-mp-tab]");
    function showTab(key) {
        tabBtns.forEach(function (b) { var on = b.dataset.mpTab === key; b.classList.toggle("is-active", on); b.setAttribute("aria-selected", on ? "true" : "false"); });
        ["profiel", "handtekening", "meldingen", "beveiliging"].forEach(function (k) { $("mp-pane-" + k).hidden = k !== key; });
        tabInput.value = key;
        try { history.replaceState(null, "", "?tab=" + key); } catch (e) { }
        if (key === "handtekening") toonVoorbeeld();
    }
    tabBtns.forEach(function (b) { b.addEventListener("click", function () { showTab(b.dataset.mpTab); }); });

    // ── Werkdagen en werkuren ────────────────────────────────────────────────────────────────────
    var werkdagen = $("mp-werkdagen");
    $("mp-days").addEventListener("click", function (e) {
        var b = e.target.closest(".gl-v2-mp-day"); if (!b) return;
        var on = !b.classList.contains("is-on");
        b.classList.toggle("is-on", on); b.setAttribute("aria-pressed", on ? "true" : "false");
        var m = 0; form.querySelectorAll(".gl-v2-mp-day.is-on").forEach(function (x) { m |= parseInt(x.dataset.bit, 10); });
        werkdagen.value = m; verander();
    });
    function werkuren() { $("mp-werkuren-tekst").textContent = $("mp-werk-van").value + " – " + $("mp-werk-tot").value; }
    $("mp-werk-van").addEventListener("input", werkuren); $("mp-werk-tot").addEventListener("input", werkuren);

    // ── Afwezigheid ──────────────────────────────────────────────────────────────────────────────
    $("mp-away").addEventListener("change", function () { $("mp-away-body").hidden = !this.checked; });
    var awayOpts = $("mp-afwezig-opties");
    $("mp-away-opts").addEventListener("change", function () {
        var m = 0; this.querySelectorAll("input[data-bit]:checked").forEach(function (x) { m |= parseInt(x.dataset.bit, 10); });
        awayOpts.value = m; verander();
    });

    // ── Meldingen en herinnering ─────────────────────────────────────────────────────────────────
    var meld = $("mp-meldingen-json");
    $("mp-matrix").addEventListener("change", function () {
        var o = {};
        this.querySelectorAll(".gl-v2-mp-matrix-row").forEach(function (r) {
            var c = r.querySelectorAll("input[type=checkbox]"); o[r.dataset.key] = [c[0].checked, c[1].checked];
        });
        meld.value = JSON.stringify(o); verander();
    });
    document.querySelectorAll("#mp-remind > button").forEach(function (b) {
        b.addEventListener("click", function () {
            $("mp-herinnering").value = b.dataset.id;
            document.querySelectorAll("#mp-remind > button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
            verander();
        });
    });

    // ── Handtekening ─────────────────────────────────────────────────────────────────────────────
    var hidden = $("mh-signature-input"), formatInput = $("mh-format-input"), source = $("mh-html-source"), preview = $("mh-preview");
    var paneVis = $("mh-pane-vis"), paneHtml = $("mh-pane-html"), mail = $("mh-mail"), note = $("mh-note"), hint = $("mh-mode-hint"), checkList = $("mh-check-list");
    var modeBtns = document.querySelectorAll("#mh-mode > button"), themeBtns = document.querySelectorAll("#mh-theme > button");
    var quill = null, quillHasContent = form.dataset.htmlFormat !== "1", mode = quillHasContent ? "vis" : "html", theme = "light";

    if (typeof Quill !== "undefined") {
        // Inline stijl i.p.v. CSS-klasse, zodat het lettertype ook bewaard blijft in de verstuurde e-mail.
        var Font = Quill.import("attributors/style/font");
        Font.whitelist = ["Arial", "Georgia", "Times New Roman", "Courier New", "Verdana", "Avenir, sans-serif"];
        Quill.register(Font, true);
        quill = new Quill("#quill-signature", {
            theme: "snow",
            modules: { toolbar: [[{ font: Font.whitelist }], ["bold", "italic", "underline"], [{ color: [] }], [{ list: "ordered" }, { list: "bullet" }], ["link", "image"], ["clean"]] }
        });
        // Quill-toolbarknoppen zijn <button> zonder type: in een form zouden ze het formulier versturen.
        form.querySelectorAll(".ql-toolbar button, .ql-toolbar .ql-picker-label").forEach(function (el) { el.setAttribute("type", "button"); });
        // BELANGRIJK: enkel in Quill laden als de handtekening ook via Quill ("Visual") is opgeslagen. Quill vereenvoudigt complexe HTML
        // (tabellen, MSO-opmaak van Outlook) zodra de editor de DOM herleest.
        if (quillHasContent) quill.root.innerHTML = hidden.value || "";
        quill.on("text-change", verander);
    }

    function quillHtml() { return quill && (quill.getText().trim() || quill.root.querySelector("img")) ? quill.root.innerHTML : ""; }
    function sigHtml() { return mode === "html" ? source.value : quillHtml(); }

    function toonVoorbeeld() {
        var h = sigHtml(), dark = theme === "dark";
        var leeg = '<p style="color:#9aa898;font-family:sans-serif;font-size:13px">(leeg)</p>';
        preview.srcdoc = '<!doctype html><html><head><meta charset="utf-8"><base target="_blank"><style>body{margin:0;font:12.5px/1.5 Arial,Helvetica,sans-serif;'
            + (dark ? "background:#1F2421;color:#E8ECE6" : "background:#fff;color:#222") + '}a{color:' + (dark ? "#7FCB97" : "#0D7A3E") + '}</style></head><body>' + (h || leeg) + "</body></html>";
        mail.classList.toggle("is-dark", dark);
        note.textContent = dark
            ? "Zo ziet een ontvanger met donkere modus je handtekening. Afbeeldingen met een eigen achtergrond blijven leesbaar."
            : "Zo ziet een ontvanger je mail in Outlook of Gmail.";
    }
    preview.addEventListener("load", function () {
        try { preview.style.height = (preview.contentDocument.documentElement.scrollHeight + 4) + "px"; } catch (e) { }
    });

    function controle() {
        if (mode !== "html") return;
        var h = source.value, rows = [];
        if (!h.trim()) { checkList.innerHTML = ""; return; }
        var local = [];
        (h.match(/<img\b[^>]*>/gi) || []).forEach(function (tag) {
            var m = /\bsrc\s*=\s*["']?([^"'\s>]+)/i.exec(tag);
            if (m && !/^(https?:|data:|cid:|\/\/)/i.test(m[1])) { var f = m[1].split("/").pop().split("?")[0]; try { f = decodeURIComponent(f); } catch (e) { } local.push(f); }
        });
        if (local.length) rows.push(["err", local.length === 1
            ? "1 afbeelding verwijst naar een bestand op je eigen computer (" + local[0] + "). Ontvangers zien dan een leeg vak. Gebruik een afbeelding die online staat."
            : local.length + " afbeeldingen verwijzen naar bestanden op je eigen computer (" + local.slice(0, 2).join(", ") + (local.length > 2 ? ", …" : "") + "). Ontvangers zien dan een leeg vak. Gebruik afbeeldingen die online staan."]);
        if (/xmlns:v=|<v:|<!--\[if gte mso/i.test(h)) rows.push(["warn", "Outlook-code (VML) gevonden. Ze wordt bewaard, maar sommige mailprogramma’s negeren ze."]);
        if (/<table\b/i.test(h)) rows.push(["ok", "Tabelopmaak blijft behouden."]);
        if (!rows.length) rows.push(["ok", "Geen problemen gevonden."]);
        checkList.innerHTML = "";
        rows.forEach(function (r) {
            var d = document.createElement("div"); d.className = "gl-v2-mh-check" + (r[0] === "err" ? " is-err" : r[0] === "warn" ? " is-warn" : "");
            var i = document.createElement("i"), s = document.createElement("span"); s.textContent = r[1]; d.appendChild(i); d.appendChild(s); checkList.appendChild(d);
        });
    }

    function setMode(m) {
        mode = m;
        modeBtns.forEach(function (b) { var on = b.dataset.mode === m; b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); });
        paneVis.hidden = m !== "vis"; paneHtml.hidden = m !== "html";
        hint.textContent = m === "vis" ? "Aanbevolen" : "Voor een bestaande Outlook-handtekening";
        verander();
    }
    modeBtns.forEach(function (b) {
        b.addEventListener("click", function () {
            var m = b.dataset.mode;
            if (m === mode) return;
            if (m === "html") {
                // Opbouwen -> HTML is altijd veilig (loutere weergave als brontekst).
                if (quillHasContent) source.value = quillHtml();
                setMode("html"); return;
            }
            // HTML -> Opbouwen kan complexe opmaak (tabellen) vereenvoudigen: enkel na bevestiging, nooit automatisch.
            var inQuill = quillHasContent ? quillHtml() : "";
            if (!source.value.trim() || source.value === inQuill) {
                if (!source.value.trim() && quill) quill.root.innerHTML = "";
                quillHasContent = true; setMode("vis"); return;
            }
            window.GlV2Bevestig({ title: "Overschakelen naar Opbouwen?", desc: "Dit kan complexe opmaak (bv. tabellen) uit je geplakte HTML vereenvoudigen.", ok: "Doorgaan", tone: "warning" })
                .then(function (ok) { if (!ok) return; quill.root.innerHTML = source.value; quillHasContent = true; setMode("vis"); });
        });
    });
    themeBtns.forEach(function (b) {
        b.addEventListener("click", function () {
            theme = b.dataset.theme;
            themeBtns.forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
            toonVoorbeeld();
        });
    });
    source.addEventListener("input", verander);

    $("mh-copy").addEventListener("click", function () {
        var h = sigHtml();
        if (!h) { toast("warning", "Niets te kopiëren", "Je handtekening is nog leeg."); return; }
        var done = function () { toast("success", "Gekopieerd", "Plak ze in Outlook onder Handtekeningen."); };
        var fail = function () { toast("danger", "Kopiëren mislukt", "Selecteer de tekst en kopieer ze zelf."); };
        if (navigator.clipboard && window.ClipboardItem) {
            navigator.clipboard.write([new ClipboardItem({ "text/html": new Blob([h], { type: "text/html" }), "text/plain": new Blob([h], { type: "text/plain" }) })]).then(done, fail);
        } else if (navigator.clipboard) navigator.clipboard.writeText(h).then(done, fail);
        else fail();
    });

    // ── Niet-opgeslagen wijzigingen ──────────────────────────────────────────────────────────────
    var badge = $("mp-dirty-badge");
    function stand() {
        var delen = [];
        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el.name || el.name === "__RequestVerificationToken" || el.name === "Tab" || el.name === "SignatureHtml" || el.name === "Format") return;
            delen.push(el.name + "=" + (el.type === "checkbox" ? (el.checked ? "1" : "0") : el.value));
        });
        delen.push("sig=" + mode + ":" + sigHtml());
        return delen.join("\u0001");
    }
    var start = "";
    function verander() { toonVoorbeeld(); controle(); badge.hidden = stand() === start; }
    form.addEventListener("input", verander);
    form.addEventListener("change", verander);

    form.addEventListener("submit", function () {
        hidden.value = sigHtml();
        formatInput.value = mode === "html" ? "Html" : "Visual";
        badge.hidden = true; start = stand();   // anders blokkeert de browser het wegnavigeren na het opslaan
    });
    window.addEventListener("beforeunload", function (e) { if (stand() !== start) { e.preventDefault(); e.returnValue = ""; } });

    setMode(mode);
    start = stand();
    verander();
    badge.hidden = true;
})();
