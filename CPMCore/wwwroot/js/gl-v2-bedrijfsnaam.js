/* Bedrijfsnaam na een btw-/ondernemingsopzoeking: de dienst geeft de naam met de vennootschapsvorm vooraan en in hoofdletters ("BVBA JANSSENS BOUW").
   Het voorstel haalt die vorm weg en zet de naam in kleine letters met een hoofdletter per woord ("Janssens Bouw"); de gevonden vorm wordt apart teruggegeven
   zodat de keuzelijst Bedrijfsvorm erop kan worden gezet. Gedeeld door de leveranciersformulieren, het snelformulier op een nieuw contract en het snelformulier in de facturen.
   Bewust niet in de lijst: SE (Societas Europaea) en andere afkortingen die ook een gewone naamdeel kunnen zijn. */
(function () {
    "use strict";
    var VORMEN = ["BVBA", "EBVBA", "SPRL", "BV", "NV", "SA", "CVBA", "CV", "CVOA", "SCRL", "VOF", "SNC", "GCV", "SCS", "SCA", "VZW", "ASBL", "IVZW", "AISBL", "VME", "SC", "ESV", "LV", "SRL", "GMBH", "LTD", "BVBA-SO", "CV-SO", "COMMV", "COMMVA"];

    function sleutel(t) { return String(t || "").replace(/[.,\s]/g, "").toUpperCase(); }

    /** @returns {{naam:string, vorm:string}} vorm = afkorting zonder punten ("BVBA"), leeg als er geen vorm vooraan stond */
    function splits(naam) {
        var t = String(naam || "").trim().replace(/\s+/g, " "), vorm = "";
        if (!t) return { naam: t, vorm: vorm };
        var delen = t.split(" "), eerste = sleutel(delen[0]), tweede = sleutel(delen[1]), weg = 0;
        if (eerste === "COMM" && (tweede === "V" || tweede === "VA")) { weg = 2; vorm = "COMM" + tweede; }   // "Comm. V." / "Comm. VA"
        else if (VORMEN.indexOf(eerste) >= 0) { weg = 1; vorm = eerste; }                                      // ook "B.V.B.A." en "N.V."
        if (weg && delen.length > weg) t = delen.slice(weg).join(" "); else vorm = "";                          // een naam die enkel een vorm is, blijft ongemoeid
        t = t.toLowerCase().replace(/(^|[\s\-'’\/(&.])([a-zà-ÿ])/g, function (m, voor, letter) { return voor + letter.toUpperCase(); });
        // afkortingen die in de naam blijven staan, houden hun hoofdletters ("SE Elektro", niet "Se Elektro")
        t = t.replace(/(^|\s)(Se)(?=\s|$)/g, function (m, voor) { return voor + "SE"; });
        return { naam: t, vorm: vorm };
    }

    /** Zet een keuzelijst Bedrijfsvorm op de vorm ("BVBA"); de opties zijn "Naam (BVBA)" of enkel "BVBA". Geeft true als er een optie gekozen werd. */
    function kiesVorm(select, vorm) {
        if (!select || !vorm) return false;
        var doel = sleutel(vorm), gevonden = null;
        // gl-v2-keuzelijst (GlV2/_SelectList): verborgen input met optieknoppen; een klik op de passende optie zet de waarde en meldt de wijziging
        if (!select.options) {
            var wrap = select.closest ? select.closest(".gl-v2-select") : null;
            if (!wrap) return false;
            Array.prototype.forEach.call(wrap.querySelectorAll(".gl-v2-select-option"), function (b) {
                if (gevonden || !b.getAttribute("data-value")) return;
                var tekst = b.textContent || "", m2 = /\(([^)]+)\)\s*$/.exec(tekst);
                if (sleutel(m2 ? m2[1] : tekst) === doel) gevonden = b;
            });
            if (!gevonden) return false;
            gevonden.click();
            return true;
        }
        Array.prototype.forEach.call(select.options, function (o) {
            if (gevonden || !o.value) return;
            var m = /\(([^)]+)\)\s*$/.exec(o.textContent || ""), a = sleutel(m ? m[1] : o.textContent);
            if (a === doel) gevonden = o;
        });
        if (!gevonden) return false;
        select.value = gevonden.value; select.dispatchEvent(new Event("change", { bubbles: true }));
        return true;
    }

    window.GlV2Bedrijfsnaam = { splits: splits, schoon: function (n) { return splits(n).naam; }, kiesVorm: kiesVorm };
})();
