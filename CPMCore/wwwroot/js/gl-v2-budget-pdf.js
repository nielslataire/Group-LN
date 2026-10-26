/* Budget-PDF opmaken (design-handoff 44d intern, 44e bank, 44f klaar). Volle pagina: PDF opvragen, hoofdstukken tonen, PDF openen in een nieuw tabblad. */
(function () {
    "use strict";
    var host = document.getElementById("bw-pdf-prep"); if (!host) return;
    var excel = host.dataset.excel === "1", intern = host.dataset.intern === "1", url = host.dataset.url, token = host.dataset.token, terug = host.dataset.terug;
    var MSG = ["Gemiddeld 10 à 20 seconden", "De kostprijs wordt nog even nagerekend…", "Cijfers worden netjes in kolommen gezet…", "Nog even de waterpas erover…", "De marge krijgt haar laatste komma…"];
    var STAPPEN = excel
        ? [["Kostenoverzicht en verkoop"], ["Activiteiten per lot"], ["Oppervlaktes"], ["Excel-bestand opbouwen"]]
        : intern
        ? [["Samenvatting en resultaat"], ["Bouwkosten per activiteit"], ["Grondkost, financiering, verkoopkosten"], ["Onvoorzien en publiciteit"], ["Oppervlaktes en verkooptabel"]]
        : [["Samenvatting en resultaat"], ["Bouwkosten per activiteit"], ["Grondkost en verkoopkosten"], ["Financiering, onvoorzien, publiciteit", "niet opgenomen", true], ["Oppervlaktes en verkooptabel"]];
    var bewaard = false, timer = null, msgTimer = null, blobUrl = null, bestand = "budget.pdf";

    // klaar (44f): de doorgestreepte hoofdstukken vallen weg; bezig (44e): ze staan er grijs en doorgestreept bij
    function stappen(actief, klaar) {
        var lijst = STAPPEN.filter(function (s) { return !(klaar && s[2]); });
        return lijst.map(function (s, i) {
            var uit = !!s[2], st = klaar || (!uit && i < actief) ? "done" : (!uit && i === actief) ? "act" : "todo";
            return { t: s[0], m: s[1] || "", s: st, strike: uit, muted: uit };
        });
    }
    // De opmaak en inhoud komen uit het gedeelde GlV2ProgressScreen (gl-v2-progress-screen.js)
    function toon(o) {
        window.GlV2ProgressScreen.render(host, { art: o.klaar ? "ok" : "report", kicker: o.kicker, title: o.title, sub: o.sub, steps: o.fout ? [] : stappen(o.actief || 0, o.klaar), busy: !(o.klaar || o.fout), foot: MSG, actions: o.actions });
    }
    function bezig(actief) {
        toon({
            actief: actief,
            kicker: excel ? "Budget als Excel · vertrouwelijk" : intern ? "Intern budget · vertrouwelijk" : "Budget voor de bank",
            title: excel ? "De Excel van het budget wordt opgemaakt" : intern ? "Het interne budget wordt opgemaakt" : "Het budget voor de bank wordt opgemaakt",
            sub: excel ? "Kostenoverzicht, activiteiten, oppervlaktes en verkoop. Het bestand wordt vanzelf gedownload zodra het klaar is." : intern ? "Alle kosten, oppervlaktes, verkoop en resultaat. De PDF opent vanzelf zodra hij klaar is."
                : "Zonder financieringskosten, onvoorzien en publiciteit. De PDF opent vanzelf zodra hij klaar is."
        });
    }
    function fout(melding) {
        clearInterval(timer);
        toon({ fout: true, kicker: "Niet gelukt", title: "De PDF kon niet worden opgemaakt", sub: "Probeer het opnieuw. Blijft het mislukken, controleer dan of het budget volledig is ingevuld." + (melding ? " Technische melding: " + melding : ""),
            actions: '<a class="gl-v2-btn gl-v2-btn-secondary" href="' + terug + '">Terug naar stap 9</a><button type="button" class="gl-v2-btn gl-v2-btn-primary" id="bw-pdf-retry">Opnieuw proberen</button>' });
    }
    function klaar(aantal) {
        clearInterval(timer);
        var nu = new Date(), p = function (n) { return (n < 10 ? "0" : "") + n; };
        toon({ klaar: true, kicker: "Opgemaakt · " + p(nu.getDate()) + "/" + p(nu.getMonth() + 1) + "/" + nu.getFullYear() + " · " + p(nu.getHours()) + ":" + p(nu.getMinutes()),
            title: excel ? "De Excel is klaar" : intern ? "Intern budget is klaar" : "Budget voor de bank is klaar",
            sub: bestand + (aantal ? " · " + aantal + " pagina's" : "") + (bewaard ? " · ook bewaard in Documenten › Budgetten." : (excel ? " · gedownload. Niet bewaard in Documenten." : " · opent in een nieuw tabblad. Niet bewaard in Documenten.")),
            actions: '<a class="gl-v2-btn gl-v2-btn-secondary" href="' + terug + '">Terug naar stap 9</a><a class="gl-v2-btn gl-v2-btn-primary" id="bw-pdf-download" href="' + blobUrl + '" download="' + bestand + '"><i class="ph ph-download-simple" aria-hidden="true"></i>' + (excel ? "Download Excel" : "Download PDF") + '</a>' });
    }

    function start() {
        var actief = 0; bezig(actief);
        clearInterval(timer);
        // de server meldt geen voortgang: de hoofdstukken lopen op tot het laatste, dat actief blijft tot de PDF binnen is
        timer = setInterval(function () {
            var laatste = STAPPEN.length - 1;
            if (actief < laatste) { actief++; window.GlV2ProgressScreen.updateSteps(host, stappen(actief, false)); }
        }, 2200);
        var form = new FormData(); form.append("__RequestVerificationToken", token || "");
        fetch(url, { method: "POST", body: form, credentials: "same-origin" }).then(function (r) {
            if (!r.ok) return r.text().then(function (t) { throw new Error((t || ("status " + r.status)).slice(0, 300)); });
            var aantal = r.headers.get("X-Pagina-Aantal"), n = r.headers.get("X-Bestandsnaam");
            if (n) bestand = n;
            bewaard = r.headers.get("X-Bewaard") === "1";
            return r.blob().then(function (b) { return { blob: b, aantal: aantal }; });
        }).then(function (res) {
            if (blobUrl) URL.revokeObjectURL(blobUrl);
            blobUrl = URL.createObjectURL(res.blob);
            klaar(res.aantal);
            if (excel) download(); // Excel kan niet in een tabblad openen: meteen downloaden
            else window.open(blobUrl, "_blank"); // kan door een popup-blokker tegengehouden worden: Download PDF blijft staan
        }).catch(function (e) { fout(e && e.message); });
    }
    // Downloaden via een losse (niet in de pagina hangende) link: andere klikafhandeling van de shell (laadindicatie, leave-guard) kan een blob-link in de pagina onderscheppen.
    function download() {
        if (!blobUrl) return;
        var a = document.createElement("a"); a.href = blobUrl; a.download = bestand; a.style.display = "none";
        document.body.appendChild(a); a.click(); setTimeout(function () { document.body.removeChild(a); }, 1000);
    }
    document.addEventListener("click", function (ev) {
        if (ev.target.closest("#bw-pdf-download")) { ev.preventDefault(); ev.stopImmediatePropagation(); download(); }
    }, true);
    document.addEventListener("click", function (ev) { if (ev.target.closest("#bw-pdf-retry")) start(); });
    start();
})();
