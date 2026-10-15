/* Budget-PDF opmaken (design-handoff 44d intern, 44e bank, 44f klaar). Volle pagina: PDF opvragen, hoofdstukken tonen, PDF openen in een nieuw tabblad. */
(function () {
    "use strict";
    var host = document.getElementById("bw-pdf-prep"); if (!host) return;
    var intern = host.dataset.intern === "1", url = host.dataset.url, token = host.dataset.token, terug = host.dataset.terug;
    var CHECK = '<svg viewBox="0 0 16 16"><path d="M3.4 8.4l3 3 6.2-6.8"/></svg>';
    var MSG = ["Gemiddeld 10 à 20 seconden", "De kostprijs wordt nog even nagerekend…", "Cijfers worden netjes in kolommen gezet…", "Nog even de waterpas erover…", "De marge krijgt haar laatste komma…"];
    var STAPPEN = intern
        ? [["Samenvatting en resultaat"], ["Bouwkosten per activiteit"], ["Grondkost, financiering, verkoopkosten"], ["Onvoorzien en publiciteit"], ["Oppervlaktes en verkooptabel"]]
        : [["Samenvatting en resultaat"], ["Bouwkosten per activiteit"], ["Grondkost en verkoopkosten"], ["Financiering, onvoorzien, publiciteit", "niet opgenomen", true], ["Oppervlaktes en verkooptabel"]];
    var bewaard = false, timer = null, msgTimer = null, blobUrl = null, bestand = "budget.pdf";

    function art(klaar) {
        if (klaar) return '<div class="gl-v2-vsprep__ok"><span class="ring"></span><span class="disc">' + CHECK + '</span></div>';
        var ln = [[100, 0, 82], [84, .25, 94], [96, .5, 112], [70, .75, 124], [90, 1, 142]].map(function (l) { return '<span class="ln" style="width:' + l[0] + 'px;top:' + l[2] + 'px;animation-delay:' + l[1] + 's"></span>'; }).join("");
        return '<div class="gl-v2-vsprep__float"><div class="gl-v2-vsprep__sheet back"></div><div class="gl-v2-vsprep__sheet front"><span class="top"></span><span class="ttl"></span><span class="ph1"></span><span class="ph2"></span>' + ln + '<span class="scan"></span></div></div>';
    }
    // klaar (44f): de doorgestreepte hoofdstukken vallen weg; bezig (44e): ze staan er grijs en doorgestreept bij
    function stappen(actief, klaar) {
        var lijst = STAPPEN.filter(function (s) { return !(klaar && s[2]); });
        return lijst.map(function (s, i) {
            var uit = !!s[2], st = klaar || (!uit && i < actief) ? "done" : (!uit && i === actief) ? "act" : "todo";
            var stijl = uit ? ' style="color:#9aa898"' : "";
            var ico = st === "done" ? '<span class="gl-v2-vsprep__ico done">' + CHECK + '</span>' : '<span class="gl-v2-vsprep__ico ' + st + '"><i></i></span>';
            return '<div class="gl-v2-vsprep__step is-' + st + '"' + stijl + '>' + ico + '<b>' + (uit ? '<s>' + s[0] + '</s>' : s[0]) + '</b><em' + stijl + '>' + (s[1] || "") + '</em></div>';
        }).join("");
    }
    function toon(o) {
        host.innerHTML = '<div class="gl-v2-vsprep__col"><div class="gl-v2-vsprep__art">' + art(o.klaar) + '</div>' +
            '<div class="gl-v2-vsprep__head"><span class="gl-v2-vsprep__kicker">' + o.kicker + '</span><h2 class="gl-v2-vsprep__title">' + o.title + '</h2><p class="gl-v2-vsprep__sub">' + o.sub + '</p></div>' +
            (o.klaar || o.fout ? '' : '<div class="gl-v2-vsprep__bar"><i></i></div>') +
            (o.fout ? '' : '<div class="gl-v2-vsprep__steps">' + stappen(o.actief || 0, o.klaar) + '</div>') +
            (o.klaar || o.fout ? '' : '<p class="gl-v2-vsprep__foot"><span></span></p>') +
            (o.actions ? '<div class="gl-v2-vsprep__actions">' + o.actions + '</div>' : '') + '</div>';
        clearInterval(msgTimer);
        var span = host.querySelector(".gl-v2-vsprep__foot span");
        if (span) {
            var k = 0, volgende = function () { span.textContent = MSG[k++ % MSG.length]; var c = span.cloneNode(true); span.replaceWith(c); span = c; };
            volgende(); msgTimer = setInterval(volgende, 2800);
        }
    }
    function bezig(actief) {
        toon({
            actief: actief,
            kicker: intern ? "Intern budget · vertrouwelijk" : "Budget voor de bank",
            title: intern ? "Het interne budget wordt opgemaakt" : "Het budget voor de bank wordt opgemaakt",
            sub: intern ? "Alle kosten, oppervlaktes, verkoop en resultaat. De PDF opent vanzelf zodra hij klaar is."
                : "Zonder financieringskosten, onvoorzien en publiciteit. De PDF opent vanzelf zodra hij klaar is."
        });
    }
    function fout() {
        clearInterval(timer);
        toon({ fout: true, kicker: "Niet gelukt", title: "De PDF kon niet worden opgemaakt", sub: "Probeer het opnieuw. Blijft het mislukken, controleer dan of het budget volledig is ingevuld.",
            actions: '<a class="gl-v2-btn gl-v2-btn-secondary" href="' + terug + '">Terug naar stap 9</a><button type="button" class="gl-v2-btn gl-v2-btn-primary" id="bw-pdf-retry">Opnieuw proberen</button>' });
    }
    function klaar(aantal) {
        clearInterval(timer);
        var nu = new Date(), p = function (n) { return (n < 10 ? "0" : "") + n; };
        toon({ klaar: true, kicker: "Opgemaakt · " + p(nu.getDate()) + "/" + p(nu.getMonth() + 1) + "/" + nu.getFullYear() + " · " + p(nu.getHours()) + ":" + p(nu.getMinutes()),
            title: intern ? "Intern budget is klaar" : "Budget voor de bank is klaar",
            sub: bestand + (aantal ? " · " + aantal + " pagina's" : "") + (bewaard ? " · ook bewaard in Documenten › Budgetten." : " · opent in een nieuw tabblad. Niet bewaard in Documenten."),
            actions: '<a class="gl-v2-btn gl-v2-btn-secondary" href="' + terug + '">Terug naar stap 9</a><a class="gl-v2-btn gl-v2-btn-primary" id="bw-pdf-download" href="' + blobUrl + '" download="' + bestand + '"><i class="ph ph-download-simple" aria-hidden="true"></i>Download PDF</a>' });
    }

    function start() {
        var actief = 0; bezig(actief);
        clearInterval(timer);
        // de server meldt geen voortgang: de hoofdstukken lopen op tot het laatste, dat actief blijft tot de PDF binnen is
        timer = setInterval(function () {
            var laatste = STAPPEN.length - 1;
            if (actief < laatste) { actief++; var el = host.querySelector(".gl-v2-vsprep__steps"); if (el) el.innerHTML = stappen(actief, false); }
        }, 2200);
        var form = new FormData(); form.append("__RequestVerificationToken", token || "");
        fetch(url, { method: "POST", body: form, credentials: "same-origin" }).then(function (r) {
            if (!r.ok) throw new Error("pdf");
            var aantal = r.headers.get("X-Pagina-Aantal"), n = r.headers.get("X-Bestandsnaam");
            if (n) bestand = n;
            bewaard = r.headers.get("X-Bewaard") === "1";
            return r.blob().then(function (b) { return { blob: b, aantal: aantal }; });
        }).then(function (res) {
            if (blobUrl) URL.revokeObjectURL(blobUrl);
            blobUrl = URL.createObjectURL(res.blob);
            klaar(res.aantal);
            window.open(blobUrl, "_blank"); // kan door een popup-blokker tegengehouden worden: Download PDF blijft staan
        }).catch(fout);
    }
    document.addEventListener("click", function (ev) { if (ev.target.closest("#bw-pdf-retry")) start(); });
    start();
})();
