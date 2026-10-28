/* gl-v2 — "Extra inhouding afhandelen" (design-handoff 42j/42l). Opent het venster rechts, rekent de
   samenvatting live en haalt bij "Verrekenen" de vrije creditnota's van de leverancier op. */
(function () {
    "use strict";
    var drawer = document.getElementById("gln-ih-drawer");
    if (!drawer) return;
    var overlay = document.getElementById("gln-ih-overlay");
    var form = document.getElementById("gln-ih-form");
    var info = null, creditnotas = [];
    var fmt = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function geld(v) { return "€ " + fmt.format(v || 0); }
    function num(el) { var v = parseFloat(el.value); return isNaN(v) ? 0 : v; }
    function r2(v) { return Math.round(v * 100) / 100; }

    var modus = "vrij";
    var elVrij = document.getElementById("gln-ih-bedrag-vrij");
    var elVerreken = document.getElementById("gln-ih-bedrag-verreken");
    var elCn = document.getElementById("gln-ih-cn");
    var elRest = document.getElementById("gln-ih-rest-actie");
    var elSam = document.getElementById("gln-ih-samenvatting");
    var elBevestig = document.getElementById("gln-ih-bevestig");

    function zetModus(m) {
        modus = m;
        document.getElementById("gln-ih-modus").value = m;
        drawer.querySelectorAll("[data-modus]").forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-modus") === m); });
        document.getElementById("gln-ih-paneel-vrij").hidden = m !== "vrij";
        document.getElementById("gln-ih-paneel-verreken").hidden = m !== "verreken";
        // Disabled velden worden niet gepost: enkel het zichtbare paneel telt.
        elVrij.disabled = m !== "vrij";
        elVerreken.disabled = m !== "verreken"; elCn.disabled = m !== "verreken"; elRest.disabled = m !== "verreken";
        document.getElementById("gln-ih-voet").textContent = m === "verreken"
            ? "Bij “Verrekenen met creditnota” kies je de ontvangen creditnota; er wordt niets betaald en er gaat niets naar de borg."
            : "";
        herreken();
    }

    function gekozenCn() {
        var id = parseInt(elCn.value, 10);
        return creditnotas.filter(function (c) { return c.id === id; })[0] || null;
    }

    function rij(label, waarde, sub, sterk) {
        return '<div class="gln-ih-sam-rij' + (sterk ? ' is-sterk' : '') + '"><dt>' + label + (sub ? '<small>' + sub + '</small>' : '') + '</dt><dd>' + waarde + '</dd></div>';
    }

    function herreken() {
        if (!info) return;
        var pct = info.borgPct != null ? Number(info.borgPct) : 0;
        var html = "", tekst = "Bevestigen";
        if (modus === "vrij") {
            var b = r2(num(elVrij));
            var borg = r2(b * pct / 100);
            html += rij("Vrijgegeven", geld(b));
            if (pct > 0) html += rij("Borg " + pct.toLocaleString("nl-BE") + " % (uit contract)", "– " + geld(borg), "gaat naar de borg");
            html += rij("Te betalen", geld(b - borg), "", true);
            tekst = geld(b - borg) + " vrijgeven";
            elBevestig.disabled = !(b > 0 && b <= info.open + 0.005);
        } else {
            var cn = gekozenCn();
            var max = cn ? Math.min(info.open, cn.bedrag) : info.open;
            var v = r2(num(elVerreken));
            var rest = r2(info.open - v);
            var restActie = elRest.value;
            document.getElementById("gln-ih-rest-label").textContent = "Rest (" + geld(Math.max(rest, 0)) + ")";
            document.getElementById("gln-ih-max-verreken").textContent = "max. " + geld(max) + " · deels kan";
            var restVrij = restActie === "vrij" ? Math.max(rest, 0) : 0;
            var restBorg = r2(restVrij * pct / 100);
            html += rij("Verrekend met " + (cn ? cn.nummer : "creditnota"), geld(v));
            if (restActie === "vrij" && rest > 0) html += rij("Rest vrijgegeven", geld(restVrij));
            if (restActie === "afboeken" && rest > 0) html += rij("Rest afgeboekt", geld(rest), "niet betaald");
            if (restActie === "vrij" && pct > 0 && rest > 0) html += rij("Borg " + pct.toLocaleString("nl-BE") + " % op vrijgave", "– " + geld(restBorg));
            html += rij("Te betalen", geld(restVrij - restBorg), "", true);
            html += rij("Nog open na deze stap", geld(restActie === "open" ? Math.max(rest, 0) : 0));
            tekst = restVrij > 0 ? "Verrekenen & " + geld(restVrij - restBorg) + " vrijgeven" : "Verrekenen";
            elBevestig.disabled = !(cn && v > 0 && v <= max + 0.005);
        }
        elSam.innerHTML = html;
        elBevestig.textContent = tekst;
    }

    function laadCreditnotas() {
        elCn.innerHTML = '<option value="">— kies een creditnota —</option>';
        creditnotas = [];
        if (!info || !info.companyId) { document.getElementById("gln-ih-cn-hint").textContent = "Geen leverancier bekend voor deze factuur."; return; }
        fetch(drawer.getAttribute("data-cn-url") + "?projectId=" + encodeURIComponent(info.projectId) + "&companyId=" + encodeURIComponent(info.companyId), { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (lijst) {
                creditnotas = lijst;
                lijst.forEach(function (c) {
                    var o = document.createElement("option");
                    o.value = c.id;
                    o.textContent = c.nummer + " · " + c.datum + " · " + geld(c.bedrag) + " excl. btw";
                    elCn.appendChild(o);
                });
                document.getElementById("gln-ih-cn-hint").textContent = lijst.length
                    ? "enkel creditnota’s van deze leverancier die nog niet verrekend zijn"
                    : "Er is nog geen onverrekende creditnota van deze leverancier. Voeg eerst de creditnota toe als factuur (type Creditnota).";
                herreken();
            });
    }

    function open(i) {
        info = i;
        document.getElementById("gln-ih-id").value = i.id;
        document.getElementById("gln-ih-terug").value = i.terug || "";
        document.getElementById("gln-ih-sub").textContent = i.bedrijf + " · factuur " + i.factuur + " · nog open " + geld(i.open);
        elVrij.value = i.open.toFixed(2);
        elVerreken.value = "";
        elRest.value = "open";
        document.getElementById("gln-ih-opmerking").value = "";
        document.getElementById("gln-ih-max-vrij").textContent = "max. " + geld(i.open) + " · deels kan";
        zetModus("vrij");
        overlay.hidden = false; drawer.hidden = false;
        document.body.classList.add("gln-ih-open");
        laadCreditnotas();
        elVrij.focus();
    }
    function sluit() { drawer.hidden = true; overlay.hidden = true; document.body.classList.remove("gln-ih-open"); }

    drawer.querySelectorAll("[data-modus]").forEach(function (b) { b.addEventListener("click", function () { zetModus(b.getAttribute("data-modus")); }); });
    [elVrij, elVerreken].forEach(function (e) { e.addEventListener("input", herreken); });
    elRest.addEventListener("change", herreken);
    elCn.addEventListener("change", function () {
        var cn = gekozenCn();
        if (cn && info) elVerreken.value = Math.min(info.open, cn.bedrag).toFixed(2);
        herreken();
    });
    document.getElementById("gln-ih-sluit").addEventListener("click", sluit);
    document.getElementById("gln-ih-annuleer").addEventListener("click", sluit);
    overlay.addEventListener("click", sluit);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && !drawer.hidden) sluit(); });

    document.addEventListener("click", function (e) {
        var knop = e.target.closest("[data-afhandelen]");
        if (!knop) return;
        e.preventDefault();
        var i = JSON.parse(knop.getAttribute("data-afhandelen"));
        i.terug = knop.getAttribute("data-terug") || "";
        open(i);
    });

    // "Ongedaan maken" bij een afhandeling in de historiek (staat binnen het factuurformulier, dus geen geneste form):
    // na bevestiging een eigen formulier opbouwen en posten.
    document.addEventListener("click", function (e) {
        var knop = e.target.closest("[data-afhandeling-intrekken]");
        if (!knop) return;
        e.preventDefault();
        var vraag = window.GlV2Bevestig
            ? window.GlV2Bevestig({ title: "Afhandeling ongedaan maken?", desc: knop.getAttribute("data-bevestig") || "", ok: "Ongedaan maken", tone: "warning" })
            : Promise.resolve(window.confirm(knop.getAttribute("data-bevestig") || "Ongedaan maken?"));
        vraag.then(function (ja) {
        if (!ja) return;
        var token = form.querySelector('input[name="__RequestVerificationToken"]');
        var f = document.createElement("form");
        f.method = "post";
        f.action = drawer.getAttribute("data-intrek-url");
        [["__RequestVerificationToken", token ? token.value : ""], ["projectId", knop.getAttribute("data-project")],
         ["id", knop.getAttribute("data-afhandeling-intrekken")], ["terug", window.location.pathname + window.location.search]].forEach(function (kv) {
            var i = document.createElement("input"); i.type = "hidden"; i.name = kv[0]; i.value = kv[1]; f.appendChild(i);
        });
        document.body.appendChild(f);
        f.submit();
        });
    });

    window.GlnInhouding = { open: open, sluit: sluit };
})();
