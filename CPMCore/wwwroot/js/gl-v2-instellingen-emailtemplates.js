// gl-v2 — Instellingen › E-mailtemplates (punt 55f): Quill-editor, klikbare tokens, live voorbeeld met een voorbeeldcontact en een waarschuwing bij niet-opgeslagen wijzigingen.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-et");
    var form = document.getElementById("et-form");
    if (!root || !form || typeof Quill === "undefined") return;

    var body = document.getElementById("et-body"), onderwerp = document.getElementById("et-onderwerp"), naam = document.getElementById("et-naam"), actief = document.getElementById("et-actief");
    var canWrite = !onderwerp.disabled;
    var VOORBEELD = { "{Voornaam}": "Sofie", "{Naam}": "Declercq", "{ProjectNaam}": "Verkaveling Keerstraat", "{Gemeente}": "Vlekkem" };

    // Inline stijl i.p.v. CSS-klasse, zodat het lettertype ook bewaard blijft wanneer de HTML als losstaande e-mail wordt verstuurd.
    var Font = Quill.import("attributors/style/font");
    Font.whitelist = ["Arial", "Georgia", "Times New Roman", "Courier New", "Verdana", "Avenir, sans-serif"];
    Quill.register(Font, true);
    var quill = new Quill("#et-quill", {
        theme: "snow", readOnly: !canWrite,
        modules: { toolbar: [[{ font: Font.whitelist }], ["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], ["link"], ["clean"]] }
    });
    if (body.value) quill.clipboard.dangerouslyPasteHTML(body.value, "silent");
    // Quill-toolbarknoppen zijn <button> zonder type: in een form zouden ze het formulier versturen.
    root.querySelectorAll(".ql-toolbar button, .ql-toolbar .ql-picker-label").forEach(function (el) { el.setAttribute("type", "button"); });

    function html() { return quill.getText().trim() ? quill.root.innerHTML : ""; }
    function vul(s) { return String(s || "").replace(/\{(Voornaam|Naam|ProjectNaam|Gemeente)\}/g, function (m) { return VOORBEELD[m]; }); }
    function toonVoorbeeld() {
        document.getElementById("et-pv-onderwerp").textContent = vul(onderwerp.value) || "(geen onderwerp)";
        // Het voorbeeld toont de opgemaakte tekst; de tokens worden in de tekstknopen ingevuld, de HTML zelf is die van de eigen editor.
        document.getElementById("et-pv-body").innerHTML = vul(quill.root.innerHTML) || "<p class='gl-v2-et-muted'>(geen tekst)</p>";
    }

    var start = { naam: naam.value, onderwerp: onderwerp.value, actief: actief.checked, body: html() };
    var dirtyEl = document.getElementById("et-dirty");
    function dirty() { return naam.value !== start.naam || onderwerp.value !== start.onderwerp || actief.checked !== start.actief || html() !== start.body; }
    function update() { body.value = html(); toonVoorbeeld(); if (dirtyEl) dirtyEl.hidden = !dirty(); }
    quill.on("text-change", update);
    [naam, onderwerp].forEach(function (el) { el.addEventListener("input", update); });
    actief.addEventListener("change", update);
    update();
    if (dirtyEl) dirtyEl.hidden = true;

    // Tokens: in het onderwerp als dat het laatst de focus had, anders in de tekst op de cursor.
    var laatst = "tekst";
    onderwerp.addEventListener("focus", function () { laatst = "onderwerp"; });
    quill.on("selection-change", function (r) { if (r) laatst = "tekst"; });
    root.querySelectorAll("[data-token]").forEach(function (b) {
        b.addEventListener("mousedown", function (e) { e.preventDefault(); });
        b.addEventListener("click", function () {
            var t = b.dataset.token;
            if (laatst === "onderwerp") {
                var s = onderwerp.selectionStart == null ? onderwerp.value.length : onderwerp.selectionStart, e = onderwerp.selectionEnd == null ? s : onderwerp.selectionEnd;
                onderwerp.value = onderwerp.value.slice(0, s) + t + onderwerp.value.slice(e);
                onderwerp.focus(); onderwerp.setSelectionRange(s + t.length, s + t.length);
            } else {
                var sel = quill.getSelection(true);
                quill.insertText(sel.index, t, "user"); quill.setSelection(sel.index + t.length, 0);
            }
            update();
        });
    });

    form.addEventListener("submit", function () { body.value = html(); start = { naam: naam.value, onderwerp: onderwerp.value, actief: actief.checked, body: body.value }; });

    // Weggaan met niet-opgeslagen wijzigingen: bevestigen bij een klik op een andere template.
    root.querySelectorAll("a.gl-v2-et-item").forEach(function (a) {
        a.addEventListener("click", function (e) {
            if (!dirty() || !window.GlV2Bevestig) return;
            e.preventDefault();
            window.GlV2Bevestig({ title: "Wijzigingen niet opslaan?", desc: "Je hebt wijzigingen die nog niet zijn opgeslagen. Ze gaan verloren.", ok: "Niet opslaan", tone: "warning" })
                .then(function (ok) { if (ok) { start = { naam: naam.value, onderwerp: onderwerp.value, actief: actief.checked, body: html() }; window.location.href = a.href; } });
        });
    });
    window.addEventListener("beforeunload", function (e) { if (canWrite && dirty()) { e.preventDefault(); e.returnValue = ""; } });

    var del = document.getElementById("et-del");
    if (del) del.addEventListener("click", function () {
        window.GlV2Bevestig({ title: "Template verwijderen?", desc: "“" + naam.value + "” wordt verwijderd. Je kunt dit niet ongedaan maken.", ok: "Verwijderen", tone: "danger" })
            .then(function (ok) { if (ok) { start = { naam: naam.value, onderwerp: onderwerp.value, actief: actief.checked, body: html() }; document.getElementById("et-del-form").submit(); } });
    });
})();
