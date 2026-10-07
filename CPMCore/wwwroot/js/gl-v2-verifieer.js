// Verificatiepagina (design-handoff 37b/37c): controleert een eigen PDF tegen de twee vingerafdrukken van het dossier.
// De SHA-256 wordt in de browser berekend (Web Crypto) — het bestand wordt nergens naartoe gestuurd. Vanilla JS, geen CDN
// (Content-Security-Policy op /verifieer staat enkel 'self' toe).
(function () {
    "use strict";

    var section = document.getElementById("gl-ver-check");
    if (!section) return;

    var original = (section.getAttribute("data-original") || "").toLowerCase();
    var final = (section.getAttribute("data-final") || "").toLowerCase();
    var issuer = section.getAttribute("data-issuer") || "";
    var drop = document.getElementById("gl-ver-drop");
    var input = document.getElementById("gl-ver-file");
    var result = document.getElementById("gl-ver-result");
    if (!drop || !input || !result) return;

    var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var MAX_BYTES = 150 * 1024 * 1024;
    var PATH_CHECK = "M3.4 8.4l3 3 6.2-6.8", PATH_CROSS = "M5 5l6 6 M11 5l-6 6", PATH_CLOCK = "M8 4.6V8l2.2 1.4";

    function wait(ms) { return new Promise(function (res) { setTimeout(res, ms); }); }

    function hex(buffer) {
        var bytes = new Uint8Array(buffer), out = "";
        for (var i = 0; i < bytes.length; i++) out += (bytes[i] < 16 ? "0" : "") + bytes[i].toString(16);
        return out;
    }

    function el(tag, cls, text) {
        var n = document.createElement(tag);
        if (cls) n.className = cls;
        if (text !== undefined) n.textContent = text;
        return n;
    }

    function show(tone, path, title, text, hash) {
        result.hidden = false;
        result.innerHTML = "";
        var box = el("div", "gl-v2-ver-result" + (tone === "ok" ? "" : " is-" + tone));
        var icon = el("span", "gl-v2-ver-result-icon");
        icon.innerHTML = '<svg viewBox="0 0 16 16" aria-hidden="true"><path d="' + path + '"></path></svg>';
        var body = el("div", "gl-v2-ver-result-body");
        body.appendChild(el("span", "gl-v2-ver-result-title", title));
        body.appendChild(el("span", "gl-v2-ver-result-text", text));
        if (hash) {
            body.appendChild(el("span", "gl-v2-ver-result-hash", hash));
            if (!reduced) box.classList.add("is-reveal");
        }
        box.appendChild(icon);
        box.appendChild(body);
        var again = el("button", "gl-v2-ver-again", "Ander bestand");
        again.type = "button";
        again.addEventListener("click", reset);
        box.appendChild(again);
        result.appendChild(box);
        if (!reduced) { box.style.animation = "glSignRise .36s cubic-bezier(0.16, 1, 0.3, 1) both"; }
        again.focus({ preventScroll: true });
    }

    function reset() {
        result.hidden = true;
        result.innerHTML = "";
        drop.hidden = false;
        input.value = "";
        input.focus();
    }

    async function check(file) {
        if (!file) return;
        drop.hidden = true;
        if (!(window.crypto && window.crypto.subtle)) {
            show("neutral", PATH_CLOCK, "Je browser kan dit hier niet controleren", "Open deze pagina via een beveiligde verbinding (https) of probeer een andere browser.", "");
            return;
        }
        if (file.size > MAX_BYTES) {
            show("neutral", PATH_CLOCK, "Dit bestand is te groot om hier te controleren", "Kies de PDF zelf (niet een groter archief).", "");
            return;
        }
        show("neutral", PATH_CLOCK, "Vingerafdruk berekenen…", file.name, "");
        try {
            var started = Date.now();
            var digest = hex(await window.crypto.subtle.digest("SHA-256", await file.arrayBuffer()));
            var left = 380 - (Date.now() - started);   // korte pauze zodat het berekenen leesbaar blijft
            if (!reduced && left > 0) await wait(left);

            var size = file.size >= 1048576 ? (file.size / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(file.size / 1024)) + " kB";
            if (final && digest === final) {
                show("ok", PATH_CHECK, "Komt overeen met de ondertekende PDF (waarde 2)", file.name + " · " + size + " · niet aangepast sinds ondertekening.", digest);
            } else if (original && digest === original) {
                show("ok", PATH_CHECK, "Komt overeen met het origineel (waarde 1)", file.name + " · " + size + " · dit is het document zoals het was vóór ondertekening.", digest);
            } else {
                show("error", PATH_CROSS, "Komt niet overeen",
                    "Dit bestand is gewijzigd of het is een ander document. Gebruik de PDF uit je bevestigingsmail of neem contact op met " + (issuer || "Group LN") + ".", digest);
            }
        } catch (e) {
            show("neutral", PATH_CLOCK, "Dit bestand kon niet gelezen worden", "Probeer het opnieuw of kies een ander bestand.", "");
        }
    }

    input.addEventListener("change", function () { check(input.files && input.files[0]); });

    ["dragenter", "dragover"].forEach(function (t) {
        drop.addEventListener(t, function (e) { e.preventDefault(); drop.classList.add("is-over"); });
    });
    ["dragleave", "drop"].forEach(function (t) {
        drop.addEventListener(t, function (e) { e.preventDefault(); drop.classList.remove("is-over"); });
    });
    drop.addEventListener("drop", function (e) {
        var f = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
        if (f) check(f);
    });
})();
