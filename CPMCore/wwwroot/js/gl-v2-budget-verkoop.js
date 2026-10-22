/* gl-v2 — Budgetflow stap 8 · Verkoop (design-handoff 39o): rekenregels per eenheid (dezelfde als ServiceCore.Budget.VerkoopLijnRekenregel), hoofdrij (grond- en bouwcode,
   min. prijs, markt, vraagprijs met bron) + open rij (grond, bouw, uitzonderingen, vraagprijs volgt), bulkbalk bij selectie, waarschuwing onder het minimum
   (Op minimum zetten / Zo laten), live samenvatting, autosave (SaveBudgetVerkoop, alle lijnen in één POST) en doorzetten naar units.
   Bronnen (BOCore.VerkoopPrijsBron): 1 voorstel · 2 markt · 3 code · 4 manueel €/m² · 5 manueel bedrag.
   Waarden staan in invoervelden met data-veld="…" (grondwaarde, bouwwaarde, grond-m2, bouw-m2, grond2-m2, grond2waarde, forfait, vraagprijs); de hoofdrij en de open
   rij hebben van sommige velden een kopie, die hier gelijk gehouden wordt. */
(function () {
    "use strict";
    var root = document.getElementById("bw-vk"); if (!root) return;
    var body = document.getElementById("bw-vp-body"), tabel = document.getElementById("bw-vp-tabel");
    var versieId = parseInt(root.getAttribute("data-versie")), locked = root.classList.contains("is-locked");
    var refPrijzen = JSON.parse(tabel.getAttribute("data-refprijzen") || "{}");
    var RATIO = parseFloat(tabel.getAttribute("data-agr-ratio")) || 0.2;
    var BRON = { 1: "VOORSTEL", 2: "MARKT", 3: "CODE", 4: "EIGEN", 5: "EIGEN" }, RANG = { bedrag: 5, m2: 4, code: 3, markt: 2, voorstel: 1 };
    var nf0 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 }), nf2 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function num(el) { if (!el) return null; var v = String(el.value == null ? "" : el.value).replace(/\s/g, "").replace(/\.(?=\d{3}(\D|$))/g, "").replace(",", "."); var n = parseFloat(v); return isNaN(n) ? null : n; }
    function fmt(v, dec) { return (v === null || v === undefined || isNaN(v)) ? "" : (dec ? v.toFixed(dec).replace(".", ",") : String(Math.round(v))); }
    function eur(v) { return "€ " + nf0.format(v); }
    function D(tr) { var d = tr.nextElementSibling; return d && d.classList.contains("bw-vp-detail") ? d : null; }
    function W(tr) { var d = D(tr), w = d && d.nextElementSibling; return w && w.classList.contains("bw-vp-waarsch") ? w : null; }
    function els(tr, n) { var d = D(tr), a = Array.prototype.slice.call(tr.querySelectorAll('[data-veld="' + n + '"]')); return d ? a.concat(Array.prototype.slice.call(d.querySelectorAll('[data-veld="' + n + '"]'))) : a; }
    function get(tr, n) { return num(els(tr, n)[0]); }
    function put(tr, n, v, dec) { var t = fmt(v, dec); els(tr, n).forEach(function (e) { e.value = t; }); }
    function sel(tr, kant) { var c = tr.querySelector('.bw-vp-zijde[data-zijde="' + kant + '"]'); return c ? c.querySelector(".gl-v2-select") : null; }
    function codeVan(tr, kant) { var s = sel(tr, kant), h = s ? s.querySelector("input[type=hidden]") : null; return h ? h.value : ""; }
    function itemsVan(w) { return Array.prototype.map.call(w.querySelectorAll(".gl-v2-select-option[data-value]:not([data-value=\"\"])"), function (o) { return { value: o.getAttribute("data-value"), text: o.textContent.trim() }; }); }
    function zetSelect(w, value) { if (w && window.GlV2Select) GlV2Select.setItems(w, itemsVan(w), value || "", "Eigen waarde"); }
    function codeTekst(tr, kant, v) { var w = sel(tr, kant); if (!w || !v) return ""; var o = w.querySelector('.gl-v2-select-option[data-value="' + v + '"]'); return o ? o.textContent.trim().replace(/\s*·P$/, "") : ""; }
    var pre = function (k) { return k === "grond" ? "G" : "B"; };

    function bronVan(tr) {
        if (tr._eigen) return 5;
        var s = [tr._gSrc, tr._bSrc].filter(Boolean);
        if (!s.length) return tr._eigenKeuze ? 4 : null;
        var m = Math.max.apply(null, s.map(function (x) { return RANG[x] || 0; })) || null;
        return tr._eigenKeuze && m && m < 4 ? 4 : m;
    }
    // per zijde: code/€/m² → bedrag = €/m² × oppervlakte; bedrag → €/m² afgeleid
    function zijde(tr, kant, opp) {
        var src = kant === "grond" ? tr._gSrc : tr._bSrc, m2 = get(tr, kant + "-m2"), bedrag = get(tr, kant === "grond" ? "grondwaarde" : "bouwwaarde");
        var nBed = kant === "grond" ? "grondwaarde" : "bouwwaarde";
        if (src === "code" || src === "m2") { if (opp > 0 && m2 !== null) put(tr, nBed, m2 * opp, 0); }
        else put(tr, kant + "-m2", (opp > 0 && bedrag !== null) ? bedrag / opp : null, 2);
    }
    function modus(tr, kant) { return (kant === "grond" ? tr._gMod : tr._bMod) || "eigen"; }
    function zetModus(tr, kant, m) { if (kant === "grond") tr._gMod = m; else tr._bMod = m; }

    function herbereken(tr) {
        var d = D(tr); if (!d) return;
        var gered = parseFloat(tr.getAttribute("data-gered")) || 0, grond = parseFloat(tr.getAttribute("data-grondopp")) || 0, opp2 = parseFloat(tr.getAttribute("data-grondopp2")) || 0, info = tr.getAttribute("data-info") === "1";
        var ruil = d.querySelector(".is-ruil").checked;
        tr.querySelector('.bw-vp-zijde[data-zijde="grond"]').classList.toggle("is-ruil", ruil);
        if (ruil) { put(tr, "grondwaarde", 0, 0); put(tr, "grond-m2", null, 2); } else zijde(tr, "grond", grond);
        zijde(tr, "bouw", gered);
        // tweede grondzone: eigen €/m²; ingevulde prijs blijft, anders volgt ze de eerste zone (relatieve waarde uit Parameters)
        var g2 = 0;
        if (opp2 > 0 && !ruil) {
            if (!tr._g2Eigen) { var m1 = get(tr, "grond-m2"); put(tr, "grond2-m2", m1 !== null ? m1 * RATIO : null, 2); }
            var p2 = get(tr, "grond2-m2"); g2 = p2 !== null ? p2 * opp2 : 0; put(tr, "grond2waarde", p2 !== null ? g2 : null, 0);
        } else { put(tr, "grond2-m2", null, 2); put(tr, "grond2waarde", null, 0); }
        var g = get(tr, "grondwaarde"), b = get(tr, "bouwwaarde"), f = get(tr, "forfait");
        if (!tr._eigen) put(tr, "vraagprijs", (g === null && b === null && f === null && !g2) ? null : (g || 0) + g2 + (b || 0) + (f || 0), 0);
        var bron = bronVan(tr); d.querySelector(".prijs-bron").value = bron || "";
        toonRij(tr, { gered: gered, grond: grond, opp2: opp2, g: g || 0, g2: g2, b: b || 0, f: f || 0, ruil: ruil, info: info, bron: bron });
        waarschuw(tr);
        totalen();
    }

    // ── weergave van de rij en de open rij ────────────────────────────────────────────────────
    function toonRij(tr, x) {
        ["grond", "bouw"].forEach(function (k) {
            var mod = modus(tr, k), src = k === "grond" ? tr._gSrc : tr._bSrc, cel = tr.querySelector('.bw-vp-zijde[data-zijde="' + k + '"]'), blok = D(tr).querySelector('.bw-vp-blok[data-zijde="' + k + '"]');
            var waarde = k === "grond" ? x.g + x.g2 : x.b, m2 = get(tr, k + "-m2");
            cel.classList.toggle("is-eigen", mod === "eigen");
            // geen code gekozen in de code-modus: de lijst vraagt om een keuze (i.p.v. "Eigen waarde" te tonen)
            var trig = cel.querySelector(".gl-v2-select-trigger"), lbl = trig ? trig.querySelector(".gl-v2-select-trigger-label") : null;
            if (lbl && mod === "code" && !codeVan(tr, k)) { lbl.textContent = "Kies code"; trig.classList.remove("is-filled"); }
            cel.querySelector(".bw-vp-chip").textContent = src === "voorstel" ? "VOORSTEL" : src === "markt" ? "MARKT" : "EIGEN";
            var sub = cel.querySelector(".bw-vp-subw");
            sub.textContent = mod === "eigen" ? (k === "bouw" && m2 !== null ? "= € " + nf2.format(m2) + "/m²" : (k === "grond" && x.g2 > 0 ? "incl. zone 2" : "")) : eur(waarde) + (k === "grond" && x.g2 > 0 ? " · incl. zone 2" : "");
            // open rij
            blok.querySelectorAll(".bw-seg-knop").forEach(function (b) { b.classList.toggle("is-aan", b.getAttribute("data-modus") === mod); });
            blok.classList.toggle("is-eigen", mod === "eigen");
            blok.querySelector(".bw-opp-m2").textContent = (k === "grond" ? x.grond : x.gered).toLocaleString("nl-BE", { maximumFractionDigits: 2 }) + " m²";
            var code = codeVan(tr, k), tekst = codeTekst(tr, k, code);
            blok.querySelector(".bw-code-tekst").textContent = tekst ? "× " + tekst : "× geen code gekozen";
            blok.querySelector(".bw-zone1-waarde").textContent = eur(k === "grond" ? x.g : x.b);
            blok.querySelector(".bw-zijde-totaal").textContent = eur(waarde);
            var z2w = blok.querySelector(".bw-zone2-waarde"); if (z2w) { blok.querySelector(".bw-vp-zone2").hidden = !(x.opp2 > 0); z2w.textContent = eur(x.g2); }
            // €/m² en bedrag: welke is "berekend" en welke "ingevuld"
            var src2 = src; blok.querySelector(".bw-eigen-m2-lbl").textContent = src2 === "m2" ? "ingevuld" : "berekend"; blok.querySelector(".bw-eigen-bedrag-lbl").textContent = src2 === "bedrag" || src2 === "voorstel" || src2 === "markt" || !src2 ? "ingevuld" : "berekend";
        });
        var d = D(tr);
        // uitzonderingen
        var forfaitVeld = d.querySelector(".bw-vp-forfait"), heeftF = x.ruil || x.f !== 0; forfaitVeld.hidden = !heeftF; d.querySelector(".bw-ruil-hint").hidden = heeftF;
        // vraagprijs: bron, volgt-knoppen en uitleg
        var vpCel = tr.querySelector(".bw-vp-vp"), bron = x.bron;
        vpCel.querySelector(".bw-vp-bron").textContent = bron ? BRON[bron] : ""; vpCel.querySelector(".bw-vp-bron").setAttribute("data-bron", bron || "");
        var volgt = bron === 1 ? "voorstel" : bron === 2 ? "markt" : "eigen";
        d.querySelectorAll("[data-volgt]").forEach(function (b) { b.classList.toggle("is-aan", b.getAttribute("data-volgt") === volgt); });
        var min = parseFloat(tr.getAttribute("data-min")) || 0, kost = parseFloat(tr.getAttribute("data-kost")) || 0, marge = parseFloat(tr.getAttribute("data-marge")) || 0, markt = parseFloat(tr.getAttribute("data-markt")) || 0;
        var uitleg = d.querySelector(".bw-vp-uitleg");
        if (!x.info) uitleg.textContent = "Vrije lijn: geen voorstel of markt beschikbaar.";
        else if (volgt === "voorstel") uitleg.textContent = "kostprijs " + eur(kost) + " + marge " + (marge * 100).toLocaleString("nl-BE", { maximumFractionDigits: 1 }) + " % = minimum " + eur(min) + " → voorgestelde vraagprijs " + eur(x.g + x.g2 + x.b + x.f);
        else if (volgt === "markt") uitleg.textContent = markt > 0 ? "marktprijs " + eur(markt) + " (mediaan " + (tr.getAttribute("data-markt-m2") ? eur(parseFloat(tr.getAttribute("data-markt-m2"))) + "/m² · " : "") + tr.getAttribute("data-marktaantal") + " vergelijkbare units); de bouwwaarde vult aan op de grondwaarde" : "geen marktprijs beschikbaar: het voorstel wordt gebruikt";
        else uitleg.textContent = "eigen waarden: grond " + eur(x.g + x.g2) + " + bouw " + eur(x.b) + (x.f ? " + forfait " + eur(x.f) : "") + " = " + eur(x.g + x.g2 + x.b + x.f) + (tr._eigen ? " — of je typt zelf een vraagprijs" : "");
    }

    // ── waarschuwing onder het minimum (goud, mag verder) ─────────────────────────────────────
    function waarschuw(tr) {
        var min = parseFloat(tr.getAttribute("data-min")) || 0, v = get(tr, "vraagprijs"), genegeerd = tr.getAttribute("data-genegeerd") === "1";
        var onder = min > 0 && v !== null && v > 0 && v < min;
        tr.classList.toggle("is-onder-min", onder);
        tr.classList.toggle("is-genegeerd", onder && genegeerd);
        var st = tr.querySelector(".bw-vp-status");
        st.className = "ph bw-vp-status " + (onder ? (genegeerd ? "ph-warning is-grijs" : "ph-warning is-warn") : (v !== null && v > 0 ? "ph-check is-ok" : ""));
        var w = W(tr); if (!w) return;
        var toon = onder && !genegeerd; w.hidden = !toon;
        if (toon) w.querySelector(".bw-vp-waarsch-t").textContent = "Vraagprijs " + eur(v) + " ligt " + eur(min - v) + " onder het minimum " + eur(min);
    }
    function totalen() {
        var som = 0, n = 0, min = 0, onder = [];
        body.querySelectorAll(".bw-vp-rij").forEach(function (tr) {
            var v = get(tr, "vraagprijs"); if (v > 0) { som += v; n++; }
            if (tr.classList.contains("is-onder-min") && tr.getAttribute("data-genegeerd") !== "1") onder.push({ naam: tr.getAttribute("data-naam"), diff: (parseFloat(tr.getAttribute("data-min")) || 0) - v });
        });
        var kpi = document.getElementById("bw-vp-kpi-vraag"); min = parseFloat(kpi ? kpi.getAttribute("data-minimum") : "0") || 0;
        document.getElementById("bw-vp-tot").textContent = n ? eur(som) : "—";
        document.getElementById("bw-vp-sum").textContent = n ? eur(som) : "—";
        var sub = document.getElementById("bw-vp-sum-sub");
        if (n && min > 0) { var dd = som - min; sub.textContent = (dd >= 0 ? "+ " : "− ") + eur(Math.abs(dd)) + " · " + (dd >= 0 ? "+" : "−") + Math.abs(dd / min * 100).toLocaleString("nl-BE", { maximumFractionDigits: 1 }) + " % t.o.v. minimum"; }
        else sub.textContent = n ? n + " lijn" + (n === 1 ? "" : "en") : "nog geen vraagprijzen";
        var al = document.getElementById("bw-vp-alert"); if (al) {
            al.hidden = !onder.length;
            document.getElementById("bw-vp-alert-t").textContent = onder.length + " eenhe" + (onder.length === 1 ? "id" : "den") + " onder minimum";
            document.getElementById("bw-vp-alert-s").textContent = onder.slice(0, 2).map(function (o) { return o.naam + " · − " + eur(o.diff); }).join(" · ") + (onder.length > 2 ? " · +" + (onder.length - 2) : "");
        }
    }

    // ── beginstand van elke lijn ──────────────────────────────────────────────────────────────
    function initRij(tr) {
        var d = D(tr); if (!d) return;
        var bron = parseInt(tr.getAttribute("data-bron")) || null;
        ["grond", "bouw"].forEach(function (k) {
            var code = codeVan(tr, k), m2 = get(tr, k + "-m2") !== null, bedrag = get(tr, k === "grond" ? "grondwaarde" : "bouwwaarde") !== null, src = null;
            if (bron === 3) src = code ? "code" : m2 ? "m2" : bedrag ? "bedrag" : null;
            else if (bron === 4) src = m2 ? "m2" : bedrag ? "bedrag" : null;
            else if (bron === 2) src = bedrag ? "markt" : null; else if (bron === 1) src = bedrag ? "voorstel" : null; else src = bedrag ? "bedrag" : null;
            if (code && !src) src = "code";
            if (k === "grond") tr._gSrc = src; else tr._bSrc = src;
            zetModus(tr, k, (code || !src) ? "code" : "eigen"); if (code) mem(tr)[k].code = code;   // een lege zijde begint met de keuzelijst
        });
        tr._g2Eigen = get(tr, "grond2-m2") !== null;
        if (bron === 5) { var g = (get(tr, "grondwaarde") || 0) + (get(tr, "grond2waarde") || 0), b = get(tr, "bouwwaarde") || 0, f = get(tr, "forfait") || 0, vp = get(tr, "vraagprijs"); tr._eigen = vp !== null && Math.abs(vp - (g + b + f)) > 0.5; }
        herbereken(tr);
    }
    var afwijkt = false;   // initRij herrekent elke lijn met de huidige oppervlaktes: wijkt dat af van wat bewaard was, dan bewaren we het meteen
    body.querySelectorAll(".bw-vp-rij").forEach(function (tr) { var voor = get(tr, "vraagprijs"); initRij(tr); var na = get(tr, "vraagprijs"); if (tr.getAttribute("data-info") === "1" && Math.abs((voor || 0) - (na || 0)) >= 1) afwijkt = true; });

    // ── invoer ────────────────────────────────────────────────────────────────────────────────
    var saveTimer = null;
    function dirty() { if (locked) return; GlV2Budget.markDirty(); clearTimeout(saveTimer); saveTimer = setTimeout(bewaar, 1200); }
    function rijVan(el) { var d = el.closest(".bw-vp-detail"); return d ? d.previousElementSibling : el.closest(".bw-vp-rij"); }
    body.addEventListener("input", function (e) {
        var t = e.target, tr = rijVan(t); if (!tr || !t.getAttribute) return;
        var n = t.getAttribute("data-veld"); if (!n) return;
        els(tr, n).forEach(function (o) { if (o !== t) o.value = t.value; });   // kopie in hoofdrij/open rij gelijk houden
        if (n === "vraagprijs") { tr._eigen = get(tr, "vraagprijs") !== null; }
        else if (n === "grond-m2") { tr._gSrc = "m2"; zetModus(tr, "grond", "eigen"); tr._eigen = false; zetSelect(sel(tr, "grond"), ""); }
        else if (n === "bouw-m2") { tr._bSrc = "m2"; zetModus(tr, "bouw", "eigen"); tr._eigen = false; zetSelect(sel(tr, "bouw"), ""); }
        else if (n === "grond2-m2") { tr._g2Eigen = get(tr, "grond2-m2") !== null; tr._eigen = false; }
        else if (n === "grondwaarde") { tr._gSrc = "bedrag"; zetModus(tr, "grond", "eigen"); tr._eigen = false; zetSelect(sel(tr, "grond"), ""); }
        else if (n === "bouwwaarde") { tr._bSrc = "bedrag"; zetModus(tr, "bouw", "eigen"); tr._eigen = false; zetSelect(sel(tr, "bouw"), ""); }
        else if (n === "forfait") { tr._eigen = false; }
        herbereken(tr); dirty();
    });
    // een code kiezen (rij of bulk): €/m² uit de code, bedrag volgt
    // Wisselen tussen Code en Eigen is verliesvrij: de stand van de kant die je verlaat wordt onthouden (de gekozen code of de eigen €/m² en het bedrag, en bij grond de tweede zone)
    // en komt terug als je terugkeert. Dit geldt voor de keuzelijst, het chipje EIGEN en de schakelaar in de open rij.
    function mem(tr) { return tr._mem || (tr._mem = { grond: { code: "", eigen: null, codeSnap: null }, bouw: { code: "", eigen: null, codeSnap: null } }); }
    function snapshot(tr, k) {
        return { m2: get(tr, k + "-m2"), bedrag: get(tr, k === "grond" ? "grondwaarde" : "bouwwaarde"), src: k === "grond" ? tr._gSrc : tr._bSrc, g2m2: k === "grond" ? get(tr, "grond2-m2") : null, g2e: k === "grond" ? !!tr._g2Eigen : false };
    }
    function herstel(tr, k, sn) {
        if (!sn) return;
        put(tr, k + "-m2", sn.m2, 2); put(tr, k === "grond" ? "grondwaarde" : "bouwwaarde", sn.bedrag, 0);
        if (k === "grond") { put(tr, "grond2-m2", sn.g2m2, 2); tr._g2Eigen = sn.g2e; }
    }
    function wisselModus(tr, k, m, code) {
        var M = mem(tr)[k], huidig = modus(tr, k), cur = codeVan(tr, k);
        if (m === huidig && !code) return;
        if (huidig === "eigen") M.eigen = snapshot(tr, k); else { M.codeSnap = snapshot(tr, k); if (cur) M.code = cur; }
        if (m === "code") {
            var c = code || M.code;
            if (c) {
                zetSelect(sel(tr, k), c); M.code = c;
                if (M.codeSnap && M.codeSnap.src === "code" && !code) herstel(tr, k, M.codeSnap);   // dezelfde code als daarvoor: ook de tweede grondzone zoals ze was
                var prijs = refPrijzen[k] ? refPrijzen[k][c] : null;
                if (prijs != null) { put(tr, k + "-m2", prijs, 2); if (k === "grond") tr._gSrc = "code"; else tr._bSrc = "code"; }
            }
            zetModus(tr, k, "code");
        } else {
            zetSelect(sel(tr, k), "");
            if (M.eigen) { herstel(tr, k, M.eigen); if (k === "grond") tr._gSrc = M.eigen.src && M.eigen.src !== "code" ? M.eigen.src : "bedrag"; else tr._bSrc = M.eigen.src && M.eigen.src !== "code" ? M.eigen.src : "bedrag"; }
            else {   // eerste keer eigen: de waarden van de code blijven staan en worden de eigen waarden
                var src = k === "grond" ? tr._gSrc : tr._bSrc;
                if (src === "code" || !src) { var n = get(tr, k + "-m2") !== null ? "m2" : (get(tr, k === "grond" ? "grondwaarde" : "bouwwaarde") !== null ? "bedrag" : null); if (k === "grond") tr._gSrc = n; else tr._bSrc = n; }
            }
            zetModus(tr, k, "eigen");
        }
        tr._eigen = false; tr._eigenKeuze = false; herbereken(tr);
    }
    // Vraagprijs volgt: Voorstel en Markt overschrijven de waarden van de rij; wat je zelf had ingevuld (codes, €/m², bedragen, zone 2, forfait, vraagprijs) wordt onthouden en komt terug bij Eigen.
    function rijSnap(tr) {
        var o = { gMod: tr._gMod, bMod: tr._bMod, gSrc: tr._gSrc, bSrc: tr._bSrc, g2Eigen: tr._g2Eigen, eigen: tr._eigen, eigenKeuze: tr._eigenKeuze, codeG: codeVan(tr, "grond"), codeB: codeVan(tr, "bouw"), mem: JSON.parse(JSON.stringify(mem(tr))), v: {} };
        ["grond-m2", "bouw-m2", "grondwaarde", "bouwwaarde", "grond2-m2", "forfait", "vraagprijs"].forEach(function (n) { o.v[n] = get(tr, n); });
        return o;
    }
    function rijHerstel(tr, o) {
        tr._gMod = o.gMod; tr._bMod = o.bMod; tr._gSrc = o.gSrc; tr._bSrc = o.bSrc; tr._g2Eigen = o.g2Eigen; tr._eigen = o.eigen; tr._eigenKeuze = o.eigenKeuze; tr._mem = JSON.parse(JSON.stringify(o.mem));
        zetSelect(sel(tr, "grond"), o.codeG); zetSelect(sel(tr, "bouw"), o.codeB);
        Object.keys(o.v).forEach(function (n) { put(tr, n, o.v[n], /m2$/.test(n) ? 2 : (n === "forfait" ? 2 : 0)); });
        herbereken(tr);
    }
    function kiesCode(tr, kant, value) {
        if (value) { mem(tr)[kant].code = value; wisselModus(tr, kant, "code", value); }
        else wisselModus(tr, kant, "eigen");
    }
    body.addEventListener("change", function (e) {
        var t = e.target, tr = rijVan(t); if (!tr) return;
        if (t.classList.contains("is-ruil")) { tr._eigen = false; herbereken(tr); dirty(); return; }
        if (t.classList.contains("bw-vp-sel")) { selectie(); return; }
        if (t.type === "hidden") {
            var z = t.closest("[data-zijde]"), isUnit = !!t.closest(".bw-vp-unit");
            if (z && !isUnit && z.classList.contains("bw-vp-zijde")) { kiesCode(tr, z.getAttribute("data-zijde"), t.value); }
            dirty();
        }
    });
    body.addEventListener("click", function (e) {
        var tr0 = e.target.closest(".bw-vp-rij,.bw-vp-detail");
        var tg = e.target.closest(".bw-vp-toggle");
        if (tg) { var tr = tg.closest("tr"), d = D(tr), open = d.hidden; d.hidden = !open; tg.setAttribute("aria-expanded", open ? "true" : "false"); tr.classList.toggle("is-open", open); return; }
        var chip = e.target.closest(".bw-vp-chip");   // terug naar een prijscode: de keuzelijst komt in beeld
        if (chip) { var r = chip.closest(".bw-vp-rij"), k = chip.closest(".bw-vp-zijde").getAttribute("data-zijde"); wisselModus(r, k, "code"); var s = sel(r, k); if (s) { var tr2 = s.querySelector(".gl-v2-select-trigger"); if (tr2) tr2.focus(); } return; }
        var seg = e.target.closest(".bw-seg-knop[data-modus]");
        if (seg) {
            var d2 = seg.closest(".bw-vp-detail"), r2 = d2.previousElementSibling, k2 = seg.closest(".bw-vp-blok").getAttribute("data-zijde"), m = seg.getAttribute("data-modus");
            wisselModus(r2, k2, m); dirty(); return;
        }
        var vg = e.target.closest("[data-volgt]");
        if (vg) {
            var r3 = vg.closest(".bw-vp-detail").previousElementSibling, w = vg.getAttribute("data-volgt");
            if (w === "eigen") { if (r3._volgtEigen && (bronVan(r3) === 1 || bronVan(r3) === 2)) { rijHerstel(r3, r3._volgtEigen); r3._volgtEigen = null; } r3._eigenKeuze = true; herbereken(r3); var vpi = els(r3, "vraagprijs")[0]; if (vpi) vpi.focus(); }
            else pasToe(r3, w);
            dirty(); return;
        }
        var om = e.target.closest(".bw-vp-opmin");
        if (om) { var rw = om.closest(".bw-vp-waarsch").previousElementSibling.previousElementSibling; put(rw, "vraagprijs", parseFloat(rw.getAttribute("data-min")) || 0, 0); rw._eigen = true; herbereken(rw); dirty(); return; }
        var zl = e.target.closest(".bw-vp-zolaten");
        if (zl) { zoLaten(zl.closest(".bw-vp-waarsch").previousElementSibling.previousElementSibling); return; }
        void tr0;
    });
    async function zoLaten(tr) {
        if (!await bewaar()) return;
        var r = await GlV2Budget.post(root.getAttribute("data-url-negeer"), { versieId: versieId, sleutel: tr.getAttribute("data-sleutel"), bevestigd: "true" }, false);
        if (r && r.success) { tr.setAttribute("data-genegeerd", "1"); waarschuw(tr); totalen(); GlV2Budget.toast("success", "Waarschuwing genegeerd", tr.getAttribute("data-naam") + " blijft onder het minimum staan; dat staat nu zo bewaard."); }
        else GlV2Budget.toast("danger", "Negeren mislukt", "Probeer het opnieuw.");
    }

    // vraagprijs = voorstel of markt: grond- en bouwwaarde uit het voorstel (of marktprijs: bouw vult aan)
    function pasToe(tr, soort) {
        if (tr.getAttribute("data-info") !== "1") { GlV2Budget.toast("warning", "Geen voorstel", tr.getAttribute("data-naam") + " staat niet op het tabblad Oppervlaktes."); return false; }
        var huidigeBron = bronVan(tr); if (huidigeBron === null || huidigeBron >= 3) tr._volgtEigen = rijSnap(tr);   // eigen waarden onthouden voor Vraagprijs volgt › Eigen
        var vg = parseFloat(tr.getAttribute("data-vgrond")) || 0, vb = parseFloat(tr.getAttribute("data-vbouw")) || 0, markt = parseFloat(tr.getAttribute("data-markt")) || 0;
        var useMarkt = soort === "markt" && markt > 0, bouw = useMarkt ? Math.max(markt - vg, 0) : vb;
        zetSelect(sel(tr, "grond"), ""); zetSelect(sel(tr, "bouw"), "");
        var o1 = parseFloat(tr.getAttribute("data-grondopp")) || 0, o2 = parseFloat(tr.getAttribute("data-grondopp2")) || 0;
        if (o2 > 0 && o1 + o2 * RATIO > 0) {   // het voorstel verdeelt de grond over beide zones naar relatieve waarde: eerste zone en eigen €/m² van de tweede zone
            var pu = vg / (o1 + o2 * RATIO); put(tr, "grondwaarde", pu * o1, 0); put(tr, "grond2-m2", pu * RATIO, 2); tr._g2Eigen = true;
        } else put(tr, "grondwaarde", vg, 0);
        put(tr, "bouwwaarde", bouw, 0);
        tr._gSrc = tr._bSrc = useMarkt ? "markt" : "voorstel"; zetModus(tr, "grond", "eigen"); zetModus(tr, "bouw", "eigen"); tr._eigen = false; tr._eigenKeuze = false;
        herbereken(tr);
        tr.classList.add("is-saving"); setTimeout(function () { tr.classList.remove("is-saving"); }, 900);
        return true;
    }
    var av = document.getElementById("bw-vp-alles-voorstel");
    if (av) av.addEventListener("click", function () { body.querySelectorAll('.bw-vp-rij[data-info="1"]').forEach(function (tr) { pasToe(tr, "voorstel"); }); dirty(); GlV2Budget.toast("success", "Voorstel overgenomen", "Grond- en bouwwaarde uit het voorstel staan in elke eenheid; de vraagprijs volgt het voorstel."); });

    // ── selectie en bulkbalk ──────────────────────────────────────────────────────────────────
    var bulk = document.getElementById("bw-vp-bulk"), alles = document.getElementById("bw-vp-selall");
    function gekozen() { return Array.prototype.filter.call(body.querySelectorAll(".bw-vp-rij"), function (tr) { return tr.querySelector(".bw-vp-sel").checked; }); }
    function selectie() {
        var g = gekozen(), n = g.length, tot = body.querySelectorAll(".bw-vp-rij").length;
        bulk.hidden = !n; document.getElementById("bw-vp-bulk-n").textContent = n + " geselecteerd";
        body.querySelectorAll(".bw-vp-rij").forEach(function (tr) { tr.classList.toggle("is-geselecteerd", tr.querySelector(".bw-vp-sel").checked); });
        if (alles) { alles.checked = n > 0 && n === tot; alles.indeterminate = n > 0 && n < tot; }
    }
    if (alles) alles.addEventListener("change", function () { body.querySelectorAll(".bw-vp-sel").forEach(function (c) { c.checked = alles.checked; }); selectie(); });
    ["grond", "bouw"].forEach(function (k) {
        var w = document.getElementById("bw-vp-bulk-" + k + "_select"), h = w ? w.querySelector("input[type=hidden]") : null; if (!h) return;
        h.addEventListener("change", function () {
            if (!h.value) return; var v = h.value;
            gekozen().forEach(function (tr) { kiesCode(tr, k, v); }); dirty();
            GlV2Budget.toast("success", "Code toegepast", (k === "grond" ? "Grondcode " : "Bouwcode ") + pre(k) + "-" + ("0" + v).slice(-2) + " staat nu in " + gekozen().length + " eenhe" + (gekozen().length === 1 ? "id" : "den") + ".");
            zetSelect(w, "");
        });
    });
    var bv = document.getElementById("bw-vp-bulk-voorstel"), bm = document.getElementById("bw-vp-bulk-markt"), bx = document.getElementById("bw-vp-bulk-sluit");
    if (bv) bv.addEventListener("click", function () { gekozen().forEach(function (tr) { pasToe(tr, "voorstel"); }); dirty(); });
    if (bm) bm.addEventListener("click", function () { gekozen().forEach(function (tr) { pasToe(tr, "markt"); }); dirty(); });
    if (bx) bx.addEventListener("click", function () { body.querySelectorAll(".bw-vp-sel").forEach(function (c) { c.checked = false; }); selectie(); });
    selectie();

    // ── opslaan ───────────────────────────────────────────────────────────────────────────────
    function hiddenIn(scope, sel2) { var s = scope.querySelector(sel2); return s ? s.querySelector("input[type=hidden]") : null; }
    function verzamel() {
        var lijnen = [];
        body.querySelectorAll(".bw-vp-rij").forEach(function (tr) {
            var d = D(tr); if (!d) return;
            var vp = get(tr, "vraagprijs"), g = get(tr, "grondwaarde"), b = get(tr, "bouwwaarde"), f = get(tr, "forfait");
            var unit = hiddenIn(d, ".bw-vp-unit .gl-v2-select"), cg = codeVan(tr, "grond"), cb = codeVan(tr, "bouw");
            if (vp === null && g === null && b === null && f === null && !(unit && unit.value) && tr.getAttribute("data-info") === "1") return;   // onaangeroerde eenheid: geen lijn
            lijnen.push({ eenheidNaam: tr.getAttribute("data-naam"), unitId: unit ? parseInt(unit.value) || null : null, codeGrond: parseInt(cg) || null, codeBouw: parseInt(cb) || null,
                grondPrijsPerM2: get(tr, "grond-m2"), bouwPrijsPerM2: get(tr, "bouw-m2"), grondAgrPrijsPerM2: get(tr, "grond2-m2"), grondwaardeAgrarisch: get(tr, "grond2waarde"), grondwaarde: g, bouwwaarde: b,
                isRuil: d.querySelector(".is-ruil").checked, extraForfait: f, vraagprijs: vp, prijsBron: parseInt(d.querySelector(".prijs-bron").value) || null });
        });
        return lijnen;
    }
    async function bewaar() {
        clearTimeout(saveTimer); GlV2Budget.setStatus("saving");
        try { var r = await GlV2Budget.post(root.getAttribute("data-url-save"), { budgetVersieId: versieId, lijnen: verzamel() }, true); if (!r.success) { GlV2Budget.setStatus("error", r.message || "Opslaan mislukt"); return false; } GlV2Budget.saved(); return true; }
        catch (e) { GlV2Budget.setStatus("error", "Opslaan mislukt: " + ((e && e.message) || e)); return false; }
    }
    GlV2Budget.register({ save: bewaar });   // geen dirty-callback: die riep GlV2Budget.isDirty() aan, dat dezelfde callback weer aanriep (oneindige lus → de knop deed niets)
    // unit koppelen telt ook als wijziging
    var dz = document.getElementById("bw-vp-doorzetten");
    if (dz) dz.addEventListener("click", async function () {
        var gekoppeld = Array.prototype.filter.call(body.querySelectorAll(".bw-vp-detail"), function (d) { var u = hiddenIn(d, ".bw-vp-unit .gl-v2-select"); return u && parseInt(u.value) > 0; }).length;
        if (!gekoppeld) { GlV2Budget.toast("warning", "Geen unit gekoppeld", "Open een eenheid en kies onder Vraagprijs volgt de Unit van het project."); return; }
        if (!await GlV2Budget.bevestig({ title: "Doorzetten naar de Units?", desc: "De lijnen worden opgeslagen en grond- en bouwwaarde van " + gekoppeld + " gekoppelde eenhe" + (gekoppeld === 1 ? "id" : "den") + " worden naar de Units geschreven. Verkochte units blijven ongemoeid.", ok: "Doorzetten", tone: "warning", icon: "ph-arrow-line-right" })) return;
        if (!await bewaar()) return;
        var r = await GlV2Budget.post(root.getAttribute("data-url-doorzetten"), { versieId: versieId }, false);
        (r.messages || []).forEach(function (m) { GlV2Budget.toast(m.type === "Error" ? "danger" : m.type === "Warning" ? "warning" : m.type === "Info" ? "info" : "success", "Doorzetten", m.text); });
    });
    totalen();
    if (afwijkt) dirty();
})();
