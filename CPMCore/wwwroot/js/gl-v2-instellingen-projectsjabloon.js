/* gl-v2 — Instellingen › Sjablonen › Projectsjablonen › bewerken (design-handoff 66d).
   De drie keuzelijsten (planning, traject, schijven), het standaardgebouw (stapper + keuzeknoppen) en het voorbeeld "Zo wordt het toegepast": bij elke wijziging vraagt dit script
   Overzicht (JSON, ProjectSjabloonService.Overzicht) en tekent de kerncijfers per kaart en het voorbeeld rechts. Het formulier zelf blijft een gewone POST. */
(function () {
    "use strict";
    var form = document.getElementById("pjs-vorm");
    if (!form) return;
    var $ = function (id) { return document.getElementById(id); };
    var url = form.getAttribute("data-overzicht-url");
    var openUrl = { planning: form.getAttribute("data-planning-url"), traject: form.getAttribute("data-traject-url"), schijven: form.getAttribute("data-schijven-url") };
    var timer = 0, teller = 0;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c]; }); }
    function nl(n) { return (Math.round(n * 100) / 100).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function sel(naam) { return $("pjs-" + naam); }
    function verd() { return Math.max(1, Math.min(20, parseInt($("pjs-verd").value, 10) || 1)); }
    function dak() { return $("pjs-dak-waarde").value || ""; }
    function kelder() { return $("pjs-kelder-waarde").value === "true"; }
    function gebouwTekst() {
        var d = [verd() + (verd() === 1 ? " verdieping" : " verdiepingen")];
        if (dak() === "hellend") d.push("hellend dak"); else if (dak() === "plat") d.push("plat dak");
        if (kelder()) d.push("kelder");
        return "voor " + d.join(" · ");
    }
    function enkelvoud(n, een, meer) { return n + " " + (n === 1 ? een : meer); }
    function andere(naam) { var s = sel(naam); return Math.max(0, s.options.length - 1 - (s.value ? 1 : 0)); }

    function feiten(deel, ov) {
        var el = form.querySelector("[data-deel=\"" + deel + "\"] [data-feiten]"), geen = form.querySelector("[data-deel=\"" + deel + "\"]").getAttribute("data-geen");
        var open = form.querySelector("[data-deel=\"" + deel + "\"] .gl-v2-pjs-open"), s = sel(deel);
        var h = "", a = andere(deel);
        if (!s.value) { h = "<li class=\"is-leeg\">overgeslagen bij het toepassen</li>"; open.hidden = true; }
        else {
            open.hidden = false; open.href = (openUrl[deel] || "#").replace("__ID__", s.value);
            if (!ov) h = "<li class=\"is-leeg\">laden …</li>";
            else if (deel === "planning" && ov.planning) h = "<li><b>" + enkelvoud(ov.planning.blokken, "blok", "blokken") + " · " + enkelvoud(ov.planning.taken, "taak", "taken") + "</b></li><li>" + ov.planning.werkdagen + " werkdagen voor 1 woning</li><li>per verdieping: " + (ov.planning.perVerdieping ? "ja" : "geen") + "</li>";
            else if (deel === "traject" && ov.traject) h = "<li><b>" + enkelvoud(ov.traject.mijlpalen, "mijlpaal", "mijlpalen") + "</b></li>" + (ov.traject.omschrijving ? "<li>" + esc(ov.traject.omschrijving) + "</li>" : "") + (sel("planning").value ? "<li>volgt de planning: " + enkelvoud(ov.traject.koppelingen, "koppeling", "koppelingen") + "</li>" : "");
            else if (deel === "schijven" && ov.schijven) {
                h = "<li><b>" + enkelvoud(ov.schijven.aantal, "schijf", "schijven") + (ov.schijven.vast ? " · " + esc(ov.schijven.vast) : "") + "</b></li>" + (ov.schijven.toelichting ? "<li>" + esc(ov.schijven.toelichting) + "</li>" : "");
                if (ov.schijven.codesNietInPlanning && ov.schijven.codesNietInPlanning.length)
                    h += "<li class=\"is-goud\">" + enkelvoud(ov.schijven.codesNietInPlanning.length, "code", "codes") + " niet in planning" + (ov.planning ? " " + esc(ov.planning.naam) : "") + "</li>";
            } else h = "<li class=\"is-leeg\">dit sjabloon bestaat niet meer</li>";
        }
        if (a > 0) h += "<li class=\"is-leeg\">" + a + (a === 1 ? " ander" : " andere") + " · of “" + esc(geen) + "”</li>";
        el.innerHTML = h;
    }

    function uitkomst(ov) {
        var h = "", u = $("pjs-uitkomst");
        $("pjs-voor").textContent = gebouwTekst();
        if (!ov || (!ov.planning && !ov.traject && !ov.schijven)) { u.innerHTML = "<p class=\"gl-v2-sjs-leeg\">Kies een onderdeel om te zien wat er gebeurt.</p>"; return; }
        if (ov.planning) h += "<div class=\"gl-v2-pjs-uit\"><div class=\"gl-v2-pjs-uit-kop\"><i class=\"ph ph-calendar-check\" aria-hidden=\"true\"></i><b>Planning</b></div><p><b>" + enkelvoud(ov.planning.fases, "fase", "fases") + " · " + enkelvoud(ov.planning.taken, "taak", "taken") + " · " + ov.planning.werkdagen + " wd</b></p>" + (ov.planning.verloop ? "<p class=\"sub\">" + esc(ov.planning.verloop) + "</p>" : "") + "</div>";
        if (ov.traject) h += "<div class=\"gl-v2-pjs-uit\"><div class=\"gl-v2-pjs-uit-kop\"><i class=\"ph ph-git-branch\" aria-hidden=\"true\"></i><b>Traject</b></div><p><b>" + enkelvoud(ov.traject.mijlpalen, "mijlpaal", "mijlpalen") + "</b></p><p class=\"sub\">Mijlpalen die aan de planning hangen schuiven mee.</p></div>";
        if (ov.schijven) {
            h += "<div class=\"gl-v2-pjs-uit\"><div class=\"gl-v2-pjs-uit-kop\"><i class=\"ph ph-list-checks\" aria-hidden=\"true\"></i><b>Schijven</b></div><p><b>" + enkelvoud(ov.schijven.voorstel.length, "schijf", "schijven") + "</b></p><ul class=\"gl-v2-pjs-lijst\">" +
                ov.schijven.voorstel.map(function (x) { return "<li><span>" + esc(x.naam) + "</span><b>" + nl(x.percentage) + " %</b></li>"; }).join("") + "</ul>" +
                (Math.abs(ov.schijven.totaal - 100) > 0.005 ? "<p class=\"sub is-fout\">Samen " + nl(ov.schijven.totaal) + " % in plaats van 100 %: pas het schijvensjabloon aan.</p>" : "") + "</div>";
        }
        u.innerHTML = h;
    }

    function ververs() {
        var mijn = ++teller;
        var q = "planningId=" + encodeURIComponent(sel("planning").value) + "&trajectId=" + encodeURIComponent(sel("traject").value) + "&schijfId=" + encodeURIComponent(sel("schijven").value) +
            "&verdiepingen=" + verd() + "&kelder=" + kelder() + "&dak=" + encodeURIComponent(dak());
        ["planning", "traject", "schijven"].forEach(function (d) { feiten(d, null); });
        $("pjs-voor").textContent = gebouwTekst();
        fetch(url + "?" + q, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
            .then(function (ov) {
                if (mijn !== teller) return;
                ["planning", "traject", "schijven"].forEach(function (d) { feiten(d, ov); });
                var m = $("pjs-mismatch");
                if (ov.meldingen && ov.meldingen.length) { m.hidden = false; m.innerHTML = "<b>Schijven en planning passen niet helemaal</b><span>" + ov.meldingen.map(esc).join(" ") + "</span>"; }
                else m.hidden = true;
                uitkomst(ov);
            })
            .catch(function () { if (mijn === teller) { ["planning", "traject", "schijven"].forEach(function (d) { var el = form.querySelector("[data-deel=\"" + d + "\"] [data-feiten]"); if (!sel(d).value) feiten(d, {}); else el.innerHTML = "<li class=\"is-leeg\">kerncijfers niet beschikbaar</li>"; }); } });
    }
    function straks() { clearTimeout(timer); timer = setTimeout(ververs, 150); }

    ["planning", "traject", "schijven"].forEach(function (d) { sel(d).addEventListener("change", ververs); });
    function stap(delta) {
        var n = Math.max(1, Math.min(20, verd() + delta));
        $("pjs-verd").value = n; $("pjs-verd-tekst").textContent = n; straks();
    }
    $("pjs-min").addEventListener("click", function () { stap(-1); });
    $("pjs-plus").addEventListener("click", function () { stap(1); });
    function keuze(id, veld) {
        $(id).addEventListener("click", function (e) {
            var b = e.target.closest("button"); if (!b || b.disabled) return;
            $(veld).value = b.getAttribute("data-v");
            Array.prototype.forEach.call($(id).children, function (c) { c.classList.toggle("is-on", c === b); });
            straks();
        });
    }
    keuze("pjs-dak", "pjs-dak-waarde");
    keuze("pjs-kelder", "pjs-kelder-waarde");
    ververs();
})();
