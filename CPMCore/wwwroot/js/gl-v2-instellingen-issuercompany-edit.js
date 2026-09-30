// gl-v2 — Instellingen/IssuerCompaniesEditV2 ("Bedrijf bewerken", design-handoff punt 24c). Twee
// dingen: tabwissel (klik toont het bijhorende paneel, andere panelen hidden — "wisselen van tab
// bewaart je invoer" is hier gratis, elk paneel blijft gewoon in de DOM staan) en het logo-sleepvak
// (klik/sleep opent de bestaande <input type="file">, en toont de gekozen bestandsnaam als
// bevestiging). De landcode-/rechtsvorm-keuzelijst hoeft hier niets eigens: initGlV2Select()
// (gl-v2-shell.js) initialiseert elke [data-gl-v2-select] al shell-breed.
(function () {
    "use strict";

    var tabbar = document.getElementById("gl-v2-ice-tabbar");
    if (tabbar) {
        var tabs = Array.prototype.slice.call(tabbar.querySelectorAll("[data-ice-tab]"));
        var panels = Array.prototype.slice.call(document.querySelectorAll("[data-tab-panel]"));
        tabs.forEach(function (tab) {
            tab.addEventListener("click", function () {
                var key = tab.getAttribute("data-ice-tab");
                tabs.forEach(function (t) {
                    var active = t === tab;
                    t.classList.toggle("is-active", active);
                    t.setAttribute("aria-selected", active ? "true" : "false");
                });
                panels.forEach(function (p) { p.hidden = p.getAttribute("data-tab-panel") !== key; });
            });
        });
    }

    var dropZone = document.querySelector(".gl-v2-ice-logo-drop");
    var fileInput = document.getElementById("LogoUpload");
    if (dropZone && fileInput) {
        var titleEl = dropZone.querySelector(".gl-v2-ice-logo-drop-title");
        var defaultTitle = titleEl ? titleEl.textContent : "";

        function showChosenFile(file) {
            if (titleEl && file) titleEl.textContent = file.name;
        }

        fileInput.addEventListener("change", function () {
            if (fileInput.files && fileInput.files[0]) showChosenFile(fileInput.files[0]);
        });
        dropZone.addEventListener("dragover", function (e) {
            e.preventDefault();
            dropZone.classList.add("is-dragover");
        });
        dropZone.addEventListener("dragleave", function () { dropZone.classList.remove("is-dragover"); });
        dropZone.addEventListener("drop", function (e) {
            e.preventDefault();
            dropZone.classList.remove("is-dragover");
            if (!e.dataTransfer || !e.dataTransfer.files || !e.dataTransfer.files[0]) return;
            fileInput.files = e.dataTransfer.files;
            showChosenFile(e.dataTransfer.files[0]);
        });
        // <label for="LogoUpload"> opent het bestandsdialoog al bij een klik (native gedrag) — enkel
        // het slepen hierboven is extra.
        if (defaultTitle) { /* niets — bewust geen reset-knop in deze eerste ronde */ }
    }
})();
