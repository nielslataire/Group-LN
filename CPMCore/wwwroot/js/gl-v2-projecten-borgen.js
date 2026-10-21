/* gl-v2 — Nacalculatie › Borgen (design-handoff 42i). Filterchips, zoeken, selectie en actiebalk "Vrijgeven". */
(function () {
    "use strict";
    var root = document.getElementById("gln-bg-borgen");
    if (!root) return;
    var rijen = Array.prototype.slice.call(root.querySelectorAll("[data-row]"));
    var chips = root.querySelectorAll(".gln-bg-chip");
    var zoek = document.getElementById("gln-bg-zoek");
    var leeg = document.getElementById("gln-bg-leeg");
    var balk = document.getElementById("gln-bg-form");
    var waarschuwing = document.getElementById("gln-bg-punten");
    var filter = "open";
    var fmt = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function pas() {
        var term = (zoek && zoek.value || "").trim().toLowerCase();
        var zichtbaar = 0;
        rijen.forEach(function (r) {
            var past = r.getAttribute("data-" + filter) === "1" && (!term || r.getAttribute("data-zoek").indexOf(term) !== -1);
            r.hidden = !past;
            if (past) zichtbaar++;
        });
        if (leeg) leeg.hidden = zichtbaar > 0;
    }
    chips.forEach(function (c) {
        c.addEventListener("click", function () {
            chips.forEach(function (x) { x.classList.remove("is-active"); });
            c.classList.add("is-active");
            filter = c.getAttribute("data-filter");
            pas();
        });
    });
    if (zoek) zoek.addEventListener("input", pas);

    function selectie() {
        return rijen.filter(function (r) { var c = r.querySelector(".gln-bg-check"); return c && c.checked && !c.disabled; });
    }
    function bedragVan(r) {
        var inp = r.querySelector(".gln-bg-bedrag");
        var max = parseFloat(r.getAttribute("data-nuvrij") || "0");
        var v = inp ? parseFloat(inp.value) : max;
        if (isNaN(v) || v < 0) v = 0;
        return Math.min(v, max);
    }
    function ververs() {
        if (!balk) return;
        var gekozen = selectie();
        // Bij een geselecteerde rij mag het te nemen bedrag aangepast worden (kleiner dan het vrij te geven bedrag).
        rijen.forEach(function (r) {
            var box = r.querySelector(".gln-bg-bedrag-box");
            var tekst = r.querySelector(".gln-bg-nuvrij-tekst");
            if (!box || !tekst) return;
            var aan = gekozen.indexOf(r) !== -1;
            box.hidden = !aan;
            tekst.hidden = aan;
        });
        var totaal = gekozen.reduce(function (s, r) { return s + bedragVan(r); }, 0);
        balk.hidden = gekozen.length === 0;
        document.getElementById("gln-bg-sel-aantal").textContent = gekozen.length + " geselecteerd";
        document.getElementById("gln-bg-sel-bedrag").textContent = "€ " + fmt.format(totaal);
        var ids = document.getElementById("gln-bg-ids");
        ids.innerHTML = "";
        gekozen.forEach(function (r) {
            var sleutel = r.getAttribute("data-sleutel");
            var i = document.createElement("input");
            i.type = "hidden"; i.name = "sleutels"; i.value = sleutel;
            ids.appendChild(i);
            var b = document.createElement("input");
            b.type = "hidden"; b.name = "bedrag_" + sleutel; b.value = bedragVan(r).toFixed(2);
            ids.appendChild(b);
        });
        // Open punten tonen (blokkeert de vrijgave niet).
        if (waarschuwing) {
            var metPunten = gekozen.filter(function (r) { return parseInt(r.getAttribute("data-punten") || "0", 10) > 0; });
            waarschuwing.hidden = metPunten.length === 0;
            if (metPunten.length) {
                var eerste = metPunten[0];
                var n = parseInt(eerste.getAttribute("data-punten"), 10);
                document.getElementById("gln-bg-punten-titel").textContent = eerste.getAttribute("data-bedrijf") + " heeft nog " + n + " open punt" + (n === 1 ? "" : "en") +
                    (metPunten.length > 1 ? " (en " + (metPunten.length - 1) + " andere aannemer" + (metPunten.length > 2 ? "s" : "") + " ook)" : "") + ".";
            }
        }
    }
    root.addEventListener("input", function (e) { if (e.target.classList.contains("gln-bg-bedrag")) ververs(); });
    root.addEventListener("change", function (e) { if (e.target.classList.contains("gln-bg-check")) ververs(); });
    var wis = document.getElementById("gln-bg-sel-wis");
    if (wis) wis.addEventListener("click", function () {
        rijen.forEach(function (r) { var c = r.querySelector(".gln-bg-check"); if (c) c.checked = false; });
        ververs();
    });
    // Vrijgaves van een rij tonen/verbergen; "Ongedaan maken" vraagt eerst bevestiging.
    root.addEventListener("click", function (e) {
        var knop = e.target.closest("[data-vrijgaves]");
        if (!knop) return;
        var detail = knop.closest("[data-row]").querySelector(".gln-bg-detail");
        if (!detail) return;
        detail.hidden = !detail.hidden;
        knop.setAttribute("aria-expanded", detail.hidden ? "false" : "true");
    });
    root.addEventListener("submit", function (e) {
        var tekst = e.target.getAttribute && e.target.getAttribute("data-bevestig");
        if (tekst && !window.confirm(tekst)) e.preventDefault();
    });
    pas();
    ververs();
})();
