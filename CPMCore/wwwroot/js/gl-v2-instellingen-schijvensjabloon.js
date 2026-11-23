/* gl-v2 — Instellingen › Sjablonen › Schijven › bewerken (design-handoff 66b).
   Rijen met per schijf Gewicht of Vast %, de regel die het uitrekent, een voorbeeldpaneel (verdiepingen, kelder, dak, constructiewaarde), de controle van de codes tegen de planningssjablonen
   en bewaren (één JSON naar Opslaan). Het uitrekenen volgt SchijfSjabloonUitrol.cs (ServiceCore): vast eerst, de rest naar gewicht, afronding op de laatste schijf met gewicht, een schijf per
   verdieping gelijk verdeeld. Wijzig je die regels, doe het op beide plaatsen. */
(function () {
    "use strict";
    var root = document.getElementById("sjs-root");
    if (!root) return;
    var canWrite = root.getAttribute("data-can-write") === "1";
    var data = {};
    try { data = JSON.parse(document.getElementById("sjs-data").textContent || "{}"); } catch (e) { }

    var $ = function (id) { return document.getElementById(id); };
    var rijenEl = $("sjs-rijen"), dirty = false, bezig = false, menuIdx = -1;
    var rij = [];                       // { naam, code, soort: "gewicht"|"vast", waarde: "13,5", perVerd, voorw }
    var codes = data.planningCodes || [];
    var voorbeeld = { verd: 2, kelder: false, dak: "", waarde: 300000 };

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c]; }); }
    function fmt(n) { return n == null || isNaN(n) ? "" : String(Math.round(n * 100) / 100).replace(".", ","); }
    function fmt2(n) { return (Math.round(n * 100) / 100).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function euro(n) { return "€ " + Math.round(n).toLocaleString("nl-BE"); }
    function parse(s) {
        s = String(s == null ? "" : s).trim().replace(/\s/g, "").replace("%", "").replace(",", ".");
        if (s === "") return null;
        var n = parseFloat(s);
        return isNaN(n) ? NaN : n;
    }
    function r2(n) { return Math.round(n * 100) / 100; }
    function rang(n) { return n + (n === 1 || n === 8 ? "ste" : "de"); }
    function markeer() { dirty = true; $("sjs-dirty").hidden = false; }
    function slug(s) { return String(s || "").normalize("NFD").replace(/[̀-ͯ]/g, "").toUpperCase().replace(/[^A-Z0-9]+/g, "_").replace(/^_|_$/g, ""); }

    // ── uitrekenen (spiegel van SchijfSjabloonUitrol.Rol) ──
    function voorwaardeKlopt(v, kelder, dak) {
        if (!v) return true;
        var d = v.split(":"), w = d[1] || "";
        if (d[0] === "kelder") return w === "ja" ? kelder : !kelder;
        if (d[0] === "dak") return (dak || "") === w;
        return true;
    }
    function laagNaam(b) { return b <= 1 ? "gelijkvloers" : rang(b - 1) + " verdieping"; }   // 1 = gelijkvloers, 2 = 1ste verdieping, ...
    function naamUit(naam, verd) {
        var t = verd == null ? String(naam).replace(/\{verdieping\}/gi, "")
            : /\{verdieping\}/i.test(naam) ? String(naam).replace(/\{verdieping\}/gi, laagNaam(verd)) : naam + " " + laagNaam(verd);
        return t.replace(/\s{2,}/g, " ").trim();
    }
    function rol(n, kelder, dak) {
        var items = [], ongeldig = false;
        rij.forEach(function (s, idx) {
            if (!voorwaardeKlopt(s.voorw, kelder, dak)) return;
            var w = parse(s.waarde), vast = s.soort === "vast", gewicht = vast ? 1 : (w == null || isNaN(w) || w <= 0 ? 1 : w);
            if (isNaN(w) || (vast && w == null)) ongeldig = true;
            if (s.voorw === "laag:boven") {
                for (var b = 2; b <= n; b++) items.push({ rij: idx, naam: naamUit(s.naam, b), vast: vast ? w : null, gewicht: gewicht });
            } else if (s.perVerd && n > 1) {
                var deel = vast && w != null ? r2(w / n) : null;
                for (var i = 1; i <= n; i++) {
                    var v = vast ? (i === n ? r2(w - deel * (n - 1)) : deel) : null;
                    items.push({ rij: idx, naam: naamUit(s.naam, i), vast: v, gewicht: gewicht / n });
                }
            } else items.push({ rij: idx, naam: naamUit(s.naam, null), vast: vast ? w : null, gewicht: gewicht });
        });
        var vasteSom = 0; items.forEach(function (i) { if (i.vast != null && !isNaN(i.vast)) vasteSom += i.vast; });
        var gewogen = items.filter(function (i) { return i.vast == null; }), totGew = 0;
        gewogen.forEach(function (i) { totGew += i.gewicht; });
        items.forEach(function (i) { i.p = i.vast != null && !isNaN(i.vast) ? i.vast : 0; });
        var rest = 100 - vasteSom;
        if (gewogen.length && rest > 0) {
            // Spiegel van SchijfSjabloonUitrol: in stappen van 5 %, minstens één stap per schijf, de grootste afrondingsrest eerst.
            var STAP = 5, stappen = Math.floor(rest / STAP + 1e-9);
            var ideaal = gewogen.map(function (i) { return rest * i.gewicht / totGew / STAP; });
            var aantal = ideaal.map(function (x) { return Math.floor(x + 1e-9); });
            var nog = stappen - aantal.reduce(function (a, b) { return a + b; }, 0);
            ideaal.map(function (x, k) { return k; }).sort(function (a, b) { return (ideaal[b] - aantal[b]) - (ideaal[a] - aantal[a]) || a - b; }).slice(0, Math.max(0, nog)).forEach(function (k) { aantal[k]++; });
            for (var k0 = 0; k0 < aantal.length; k0++) { while (aantal[k0] < 1 && Math.max.apply(null, aantal) > 1) { aantal[aantal.indexOf(Math.max.apply(null, aantal))]--; aantal[k0]++; } }
            var verdeeld = 0;
            gewogen.forEach(function (i, k) { var p = k === gewogen.length - 1 ? r2(rest - verdeeld) : aantal[k] * STAP; i.p = p; verdeeld += p; });
        }
        var totaal = 0; items.forEach(function (i) { totaal += i.p; });
        return { items: items, totaal: totaal, vasteSom: vasteSom, gewogen: gewogen.length, totGew: totGew, rest: rest, ongeldig: ongeldig };
    }

    // ── code in planning: controle tegen de planningssjablonen ──
    function codeStatus(s) {
        var c = (s.code || "").trim();
        if (!c || !codes.length) return null;
        var up = c.toUpperCase(), gevonden = null;
        codes.some(function (x) { if (String(x.code).toUpperCase() === up) { gevonden = x; return true; } return false; });
        var leesbaar = /^[A-Z][A-Z0-9_]*$/.test(up) && !/^NIEUWE?_(TAAK|BLOK)(_\d+)?$/.test(up) && c === up;
        if (gevonden) {
            if (leesbaar) return { t: "ok", tekst: "gevonden · " + gevonden.naam };
            return { t: "gold", tekst: "taak “" + gevonden.naam + "” — geef ze een leesbare code", naam: gevonden.naam };
        }
        var sug = null;
        codes.some(function (x) { var u = String(x.code).toUpperCase(); if (u.indexOf(up) === 0 || up.indexOf(u) === 0 || u.indexOf(up) >= 0) { sug = x; return true; } return false; });
        return sug ? { t: "bad", tekst: "niet gevonden — bedoel je " + sug.code + "?", sug: sug } : { t: "bad", tekst: "niet gevonden in een planningssjabloon" };
    }

    // ── rijen ──
    var VOORW = [["", "Altijd"], ["kelder:ja", "Met kelder"], ["kelder:nee", "Zonder kelder"], ["dak:hellend", "Hellend dak"], ["dak:plat", "Plat dak"], ["laag:boven", "Elke bouwlaag boven het gelijkvloers"]];
    function rijHtml(s, i, laatste) {
        var vast = s.soort === "vast";
        var h = "<li class=\"gl-v2-sjs-rij" + (laatste ? " is-eind" : "") + "\" data-i=\"" + i + "\">" +
            "<span class=\"gl-v2-sjs-grip\" title=\"Sleep om de volgorde te wijzigen\"><i class=\"ph ph-dots-six-vertical\" aria-hidden=\"true\"></i></span>" +
            "<span class=\"gl-v2-sjs-nr\">" + (i + 1) + "</span>" +
            "<span class=\"gl-v2-sjs-om\"><span class=\"gl-v2-field-box\"><input type=\"text\" class=\"gl-v2-field-input js-naam\" maxlength=\"250\" autocomplete=\"off\" aria-label=\"Omschrijving op factuur\" value=\"" + esc(s.naam) + "\" /></span>" +
            "<span class=\"gl-v2-sjs-omsub\"></span></span>" +
            "<span class=\"gl-v2-sjs-cw\"><span class=\"gl-v2-field-box\"><input type=\"text\" class=\"gl-v2-field-input js-code\" maxlength=\"100\" autocomplete=\"off\" aria-label=\"Code in de planning\" placeholder=\"optioneel\" value=\"" + esc(s.code) + "\" /></span><small class=\"gl-v2-sjs-codestatus\"></small></span>" +
            "<span class=\"gl-v2-sjs-soort\" role=\"group\" aria-label=\"Soort\"><button type=\"button\" data-soort=\"gewicht\" class=\"" + (vast ? "" : "is-on") + "\">Gewicht</button><button type=\"button\" data-soort=\"vast\" class=\"" + (vast ? "is-on" : "") + "\">Vast %</button></span>" +
            "<span class=\"gl-v2-field-box gl-v2-sjs-waarde\"><input type=\"text\" class=\"gl-v2-field-input is-tabular js-waarde\" inputmode=\"decimal\" autocomplete=\"off\" aria-label=\"" + (vast ? "Vast percentage" : "Gewicht") + "\" value=\"" + esc(s.waarde) + "\" />" + (vast ? "<span class=\"gl-v2-field-suffix\">%</span>" : "") + "</span>" +
            "<span class=\"gl-v2-sjs-check\"><input type=\"checkbox\" class=\"js-verd\" aria-label=\"Per verdieping herhalen\"" + (s.perVerd ? " checked" : "") + " /></span>" +
            "<span class=\"gl-v2-field-box gl-v2-sjs-gv\"><select class=\"gl-v2-field-input js-voorw\" aria-label=\"Enkel bij\">" + VOORW.map(function (o) { return "<option value=\"" + o[0] + "\"" + (o[0] === s.voorw ? " selected" : "") + ">" + o[1] + "</option>"; }).join("") + "</select></span>" +
            "<span class=\"gl-v2-sjs-res\"></span>" +
            "<span class=\"gl-v2-sjs-menucel\"><button type=\"button\" class=\"gl-v2-icon-btn js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"sjs-rijmenu\" aria-label=\"Acties voor schijf " + (i + 1) + "\" title=\"Meer acties\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button></span></li>";
        return h;
    }
    function renderRijen() {
        rijenEl.innerHTML = rij.map(function (s, i) { return rijHtml(s, i, i === rij.length - 1); }).join("");
        if (!canWrite) rijenEl.querySelectorAll("input,select,button").forEach(function (x) { x.disabled = true; });
        afgeleid();
    }
    function liVan(i) { return rijenEl.children[i]; }

    // alles wat uit de rijen volgt: regel, voorbeeldkolom, codechecks, paneel, controle
    function afgeleid() {
        var uit = rol(voorbeeld.verd, voorbeeld.kelder, voorbeeld.dak);
        regel();
        rij.forEach(function (s, i) {
            var li = liVan(i); if (!li) return;
            li.classList.toggle("is-eind", i === rij.length - 1);
            var sub = li.querySelector(".gl-v2-sjs-omsub"), tags = "";
            if (i === rij.length - 1) tags += "<em class=\"gl-v2-sjs-eind\">eindafrekening</em>";
            if (/\{verdieping\}/i.test(s.naam)) tags += "<em class=\"gl-v2-sjs-tok\">{verdieping}</em>" + (s.perVerd ? "" : "<em class=\"gl-v2-sjs-tokwarn\">zet “per verdieping” aan</em>");
            sub.innerHTML = tags;
            var cs = codeStatus(s), cl = li.querySelector(".gl-v2-sjs-codestatus");
            cl.className = "gl-v2-sjs-codestatus" + (cs ? " is-" + cs.t : "");
            cl.innerHTML = cs ? "<i class=\"ph " + (cs.t === "ok" ? "ph-check-circle" : cs.t === "bad" ? "ph-x-circle" : "ph-warning") + "\" aria-hidden=\"true\"></i><span>" + esc(cs.tekst) + "</span>" : "";
            var mijn = uit.items.filter(function (x) { return x.rij === i; }), res = li.querySelector(".gl-v2-sjs-res");
            if (!mijn.length) { res.textContent = "—"; res.title = "Geldt niet voor dit gebouw"; res.classList.add("is-uit"); }
            else {
                res.classList.remove("is-uit"); res.title = "";
                var gelijk = mijn.every(function (x) { return Math.abs(x.p - mijn[0].p) < 0.006; });
                res.textContent = mijn.length > 1 ? (gelijk ? mijn.length + " × " + fmt2(mijn[0].p) + " %" : fmt2(mijn.reduce(function (a, x) { return a + x.p; }, 0)) + " %") : fmt2(mijn[0].p) + " %";
            }
            li.querySelector(".js-waarde").closest(".gl-v2-field-box").classList.toggle("is-fout", isNaN(parse(s.waarde)));
        });
        paneel(uit);
        controle();
    }
    function regel() {
        var u = rol(1, false, ""), el = $("sjs-regel");
        var vast = 0, gewogen = 0, gew = 0, ongeldig = false;
        rij.forEach(function (s) {
            var w = parse(s.waarde);
            if (isNaN(w) || (s.soort === "vast" && w == null)) { ongeldig = true; return; }
            if (s.soort === "vast") vast += w; else { gewogen++; gew += (w == null || w <= 0 ? 1 : w); }
        });
        var h;
        if (ongeldig) { el.dataset.state = "fout"; el.innerHTML = "<span>Een percentage of gewicht is geen getal.</span>"; return; }
        if (!gewogen) {
            var ok = Math.abs(vast - 100) <= 0.005;
            el.dataset.state = ok ? "ok" : "fout";
            h = "<b class=\"gl-v2-sjs-rb is-vast\">vast " + fmt(vast) + " %</b><span>" + (ok ? "samen precies 100 %" : "samen moet het 100 % zijn, nu " + fmt(vast) + " %") + "</span>";
        } else {
            var rest = 100 - vast, bad = rest <= 0;
            el.dataset.state = bad ? "fout" : "ok";
            h = (vast > 0 ? "<b class=\"gl-v2-sjs-rb is-vast\">vast " + fmt(vast) + " %</b>" : "") +
                "<b class=\"gl-v2-sjs-rb is-gewicht\">" + fmt(rest) + " % verdeeld over gewicht " + fmt(gew) + "</b>" +
                (bad ? "<span>er blijft niets over voor de schijven met een gewicht</span>" : "<span>= " + fmt2(rest / gew) + " % per gewicht · afronding op de laatste schijf met gewicht · <code>{verdieping}</code> wordt “1ste verdieping”, “2de verdieping” …</span>");
        }
        el.innerHTML = h;
    }
    function paneel(uit) {
        $("sjs-uitrol").innerHTML = uit.items.map(function (x, k) {
            return "<li><span class=\"nr\">" + (k + 1) + "</span><span class=\"t\">" + esc(x.naam) + "</span><b>" + fmt2(x.p) + " %</b><span class=\"e\">" + euro(x.p / 100 * voorbeeld.waarde) + "</span></li>";
        }).join("");
        var tot = uit.totaal, goed = Math.abs(tot - 100) <= 0.005;
        $("sjs-totaal").className = "gl-v2-sjs-totaal" + (goed ? "" : " is-fout");
        $("sjs-totaal").innerHTML = "<span>" + uit.items.length + " schijven</span><b>" + fmt2(tot) + " %</b><span class=\"e\">" + euro(tot / 100 * voorbeeld.waarde) + "</span>";
    }
    function controle() {
        var punten = [], minimum = parse($("sjs-minimum").value);
        if (isNaN(minimum) || minimum == null) minimum = 5;
        rij.forEach(function (s, i) {
            var cs = codeStatus(s); if (!cs || cs.t === "ok") return;
            if (cs.t === "bad") punten.push({ t: "bad", titel: "Code " + s.code + " niet gevonden", tekst: "De schijf hangt niet aan de planning." + (cs.sug ? " In " + cs.sug.sjabloon + " heet de taak " + cs.sug.code + "." : "") });
            else punten.push({ t: "gold", titel: "Code " + s.code + " is niet leesbaar", tekst: "Geef “" + cs.naam + "” in het planningssjabloon een code zoals " + (slug(cs.naam) || "BINNENDEUREN") + "." });
        });
        var u = rol(1, false, "");
        if (u.ongeldig) punten.unshift({ t: "bad", titel: "Een percentage of gewicht is geen getal", tekst: "Pas het aan voor je bewaart." });
        else if (u.gewogen === 0 && Math.abs(u.vasteSom - 100) > 0.005) punten.unshift({ t: "bad", titel: "Vaste percentages samen " + fmt(u.vasteSom) + " %", tekst: "Zonder schijven met een gewicht moeten ze samen 100 % zijn." });
        else if (u.gewogen > 0 && u.vasteSom >= 100) punten.unshift({ t: "bad", titel: "Vaste percentages samen " + fmt(u.vasteSom) + " %", tekst: "Er blijft niets over voor de schijven met een gewicht." });
        var onder = null;
        if (!u.ongeldig) {
            for (var n = 1; n <= 5; n++) [false, true].forEach(function (k) {
                if (onder) return;
                var r = rol(n, k, ""); r.items.some(function (x) { if (x.p < minimum - 0.0001) { onder = { naam: x.naam, p: x.p, n: n, k: k }; return true; } return false; });
            });
            if (onder) punten.push({ t: "gold", titel: "“" + onder.naam + "” zakt onder " + fmt(minimum) + " %", tekst: "Dat gebeurt vanaf " + onder.n + " " + (onder.n === 1 ? "verdieping" : "verdiepingen") + (onder.k ? " met kelder" : "") + ": " + fmt2(onder.p) + " %. Dat geeft een waarschuwing bij het toepassen." });
        }
        var el = $("sjs-controle"), n = punten.length;
        $("sjs-controle-n").textContent = n === 0 ? "in orde" : n + " aandachtspunt" + (n === 1 ? "" : "en");
        el.innerHTML = punten.map(function (p) { return "<div class=\"gl-v2-sjs-iss is-" + p.t + "\"><b>" + esc(p.titel) + "</b><span>" + esc(p.tekst) + "</span></div>"; }).join("") +
            (onder || u.ongeldig ? "" : "<div class=\"gl-v2-sjs-ok\"><i class=\"ph ph-check-circle\" aria-hidden=\"true\"></i>Alle schijven ≥ " + fmt(minimum) + " % tot 5 verdiepingen</div>");
    }

    // ── opbouw ──
    $("sjs-naam").value = data.naam || "";
    $("sjs-beschrijving").value = data.beschrijving || "";
    $("sjs-minimum").value = fmt(data.minimum == null ? 5 : data.minimum);
    $("sjs-actief").checked = data.isActief !== false;
    (data.schijven && data.schijven.length ? data.schijven : [{ naam: "", gewicht: 1 }]).forEach(function (s) {
        var vast = s.vast != null;
        rij.push({ naam: s.naam || "", code: s.code || "", soort: vast ? "vast" : "gewicht", waarde: fmt(vast ? s.vast : (s.gewicht == null ? 1 : s.gewicht)), perVerd: !!s.perVerdieping, voorw: s.voorwaarde || "" });
    });
    function segKnoppen() {
        $("sjs-verd").innerHTML = [1, 2, 3, 4].map(function (n) { return "<button type=\"button\" data-v=\"" + n + "\" class=\"" + (voorbeeld.verd === n ? "is-on" : "") + "\">" + n + "</button>"; }).join("");
        $("sjs-kelder").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-on", (b.getAttribute("data-v") === "ja") === voorbeeld.kelder); });
        $("sjs-dak").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-on", b.getAttribute("data-v") === voorbeeld.dak); });
    }
    segKnoppen(); renderRijen();
    if (!canWrite) { ["sjs-naam", "sjs-beschrijving", "sjs-minimum", "sjs-actief", "sjs-toevoegen"].forEach(function (id) { $(id).disabled = true; }); }

    // ── voorbeeldpaneel ──
    $("sjs-verd").addEventListener("click", function (e) { var b = e.target.closest("button"); if (!b) return; voorbeeld.verd = parseInt(b.getAttribute("data-v"), 10); segKnoppen(); afgeleid(); });
    $("sjs-kelder").addEventListener("click", function (e) { var b = e.target.closest("button"); if (!b) return; voorbeeld.kelder = b.getAttribute("data-v") === "ja"; segKnoppen(); afgeleid(); });
    $("sjs-dak").addEventListener("click", function (e) { var b = e.target.closest("button"); if (!b) return; voorbeeld.dak = b.getAttribute("data-v"); segKnoppen(); afgeleid(); });
    $("sjs-waarde").addEventListener("input", function () {
        var n = parseInt(String(this.value).replace(/[^\d]/g, ""), 10);
        voorbeeld.waarde = isNaN(n) ? 0 : n; afgeleid();
    });
    $("sjs-waarde").addEventListener("blur", function () { this.value = voorbeeld.waarde.toLocaleString("nl-BE"); });

    // ── bewerken ──
    function verplaats(van, naar) {
        if (naar < 0 || naar >= rij.length || van === naar) return;
        var x = rij.splice(van, 1)[0]; rij.splice(naar, 0, x);
        renderRijen(); markeer();
    }
    $("sjs-toevoegen").addEventListener("click", function () {
        // Nieuwe schijf vóór de eindafrekening (de laatste rij)
        var s = { naam: "", code: "", soort: "gewicht", waarde: "1", perVerd: false, voorw: "" };
        if (rij.length > 1) rij.splice(rij.length - 1, 0, s); else rij.unshift(s);
        renderRijen(); markeer();
        var li = liVan(rij.length > 1 ? rij.length - 2 : 0); if (li) li.querySelector(".js-naam").focus();
    });
    rijenEl.addEventListener("input", function (e) {
        var li = e.target.closest("li"); if (!li || !canWrite) return;
        var s = rij[+li.getAttribute("data-i")];
        if (e.target.classList.contains("js-naam")) s.naam = e.target.value;
        else if (e.target.classList.contains("js-code")) s.code = e.target.value;
        else if (e.target.classList.contains("js-waarde")) s.waarde = e.target.value;
        else return;
        markeer(); afgeleid();
    });
    rijenEl.addEventListener("change", function (e) {
        var li = e.target.closest("li"); if (!li || !canWrite) return;
        var s = rij[+li.getAttribute("data-i")];
        if (e.target.classList.contains("js-verd")) s.perVerd = e.target.checked;
        else if (e.target.classList.contains("js-voorw")) s.voorw = e.target.value;
        else return;
        markeer(); afgeleid();
    });
    rijenEl.addEventListener("click", function (e) {
        var b = e.target.closest("[data-soort]"); if (!b || !canWrite) return;
        var li = b.closest("li"), s = rij[+li.getAttribute("data-i")], nieuw = b.getAttribute("data-soort");
        if (s.soort === nieuw) return;
        s.soort = nieuw; s.waarde = nieuw === "vast" ? "" : "1";
        renderRijen(); markeer();
        var inp = liVan(+li.getAttribute("data-i")).querySelector(".js-waarde"); if (inp) inp.focus();
    });
    ["sjs-naam", "sjs-beschrijving"].forEach(function (id) { $(id).addEventListener("input", markeer); });
    $("sjs-minimum").addEventListener("input", function () { markeer(); controle(); });
    $("sjs-actief").addEventListener("change", markeer);

    // sleepgreep (muis); op touch en met toetsenbord kan het via ⋯ › Omhoog / Omlaag
    var sleep = -1;
    rijenEl.addEventListener("mousedown", function (e) { var g = e.target.closest(".gl-v2-sjs-grip"); if (g && canWrite) g.closest("li").draggable = true; });
    rijenEl.addEventListener("dragstart", function (e) { var li = e.target.closest("li"); if (!li) return; sleep = +li.getAttribute("data-i"); li.classList.add("is-sleep"); try { e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", String(sleep)); } catch (x) { } });
    rijenEl.addEventListener("dragover", function (e) { if (sleep < 0) return; e.preventDefault(); var li = e.target.closest("li"); Array.prototype.forEach.call(rijenEl.children, function (c) { c.classList.toggle("is-doel", c === li && +c.getAttribute("data-i") !== sleep); }); });
    rijenEl.addEventListener("drop", function (e) { if (sleep < 0) return; e.preventDefault(); var li = e.target.closest("li"); if (li) verplaats(sleep, +li.getAttribute("data-i")); sleep = -1; });
    rijenEl.addEventListener("dragend", function () { sleep = -1; Array.prototype.forEach.call(rijenEl.children, function (c) { c.classList.remove("is-sleep", "is-doel"); c.draggable = false; }); });

    // ⋯-menu per schijf (gedeeld .gl-v2-menu; de shell opent het, wij vullen het eerst in de capture-fase)
    var menu = $("sjs-rijmenu");
    function item(act, icon, label, opts) {
        opts = opts || {};
        return "<button type=\"button\" class=\"gl-v2-menu-item" + (opts.danger ? " is-danger" : "") + "\" role=\"menuitem\" data-menu=\"" + act + "\"" + (opts.uit ? " disabled aria-disabled=\"true\"" : "") + "><i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span></span></button>";
    }
    document.addEventListener("click", function (e) {
        var t = e.target.closest(".js-gl-v2-menu-trigger[aria-controls=\"sjs-rijmenu\"]"); if (!t) return;
        menuIdx = +t.closest("li").getAttribute("data-i");
        menu.innerHTML = item("omhoog", "ph-arrow-up", "Omhoog", { uit: !canWrite || menuIdx === 0 }) + item("omlaag", "ph-arrow-down", "Omlaag", { uit: !canWrite || menuIdx === rij.length - 1 }) +
            item("dupliceer", "ph-copy", "Dupliceren", { uit: !canWrite }) + item("wis", "ph-trash", "Schijf verwijderen", { danger: true, uit: !canWrite || rij.length === 1 });
    }, true);
    menu.addEventListener("click", function (e) {
        var b = e.target.closest("[data-menu]"); if (!b || b.disabled || menuIdx < 0) return;
        var act = b.getAttribute("data-menu"), i = menuIdx;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        if (act === "omhoog") verplaats(i, i - 1);
        else if (act === "omlaag") verplaats(i, i + 1);
        else if (act === "dupliceer") { rij.splice(i + 1, 0, JSON.parse(JSON.stringify(rij[i]))); rij[i + 1].code = ""; renderRijen(); markeer(); }
        else if (act === "wis" && rij.length > 1) {
            var weg = rij.splice(i, 1)[0]; renderRijen(); markeer();
            if (window.GlV2Toast && window.GlV2Toast.notify) window.GlV2Toast.notify("success", "Schijf verwijderd", "“" + (weg.naam || "Schijf " + (i + 1)) + "” verwijderd. Annuleren zet alles terug.");
        }
    });

    // ── bewaren ──
    function fouten(lijst) {
        var ul = $("sjs-fouten-lijst"); ul.innerHTML = "";
        lijst.forEach(function (t) { var li = document.createElement("li"); li.textContent = t; ul.appendChild(li); });
        $("sjs-fouten").hidden = lijst.length === 0;
        if (lijst.length) $("sjs-fouten").scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
    $("sjs-fouten-close").addEventListener("click", function () { fouten([]); });

    function model() {
        var min = parse($("sjs-minimum").value);
        return {
            id: data.id || 0,
            naam: $("sjs-naam").value.trim(),
            beschrijving: $("sjs-beschrijving").value.trim(),
            minimumPercentage: min == null || isNaN(min) ? 5 : min,
            isActief: $("sjs-actief").checked,
            schijven: rij.map(function (s) {
                var w = parse(s.waarde), vast = s.soort === "vast";
                return {
                    naam: s.naam.trim(),
                    planningCode: s.code.trim(),
                    vastPercentage: vast ? (w == null || isNaN(w) ? null : w) : null,
                    gewicht: vast ? 1 : (w == null || isNaN(w) ? 1 : w),
                    perVerdieping: !!s.perVerd,
                    voorwaardeVariabele: (s.voorw.split(":")[0] || null),
                    voorwaardeWaarde: (s.voorw.split(":")[1] || null)
                };
            })
        };
    }

    var opslaan = $("sjs-opslaan");
    if (opslaan) opslaan.addEventListener("click", function () {
        if (bezig) return;
        var m = model(), lokaal = [];
        rij.forEach(function (s, i) {
            var w = parse(s.waarde);
            if (isNaN(w)) lokaal.push("Schijf " + (i + 1) + ": een percentage of gewicht is geen getal.");
            else if (s.soort === "vast" && w == null) lokaal.push("Schijf " + (i + 1) + ": vul het vaste percentage in, of kies Gewicht.");
        });
        if (isNaN(parse($("sjs-minimum").value))) lokaal.push("Het minimum per schijf is geen getal.");
        if (lokaal.length) { fouten(lokaal); return; }
        bezig = true; opslaan.disabled = true;
        var token = (document.querySelector("input[name=__RequestVerificationToken]") || {}).value || "";
        fetch(root.getAttribute("data-opslaan-url"), {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token, "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(m)
        }).then(function (r) { if (!r.ok) throw new Error("Opslaan mislukt. Probeer het zo opnieuw."); return r.json(); })
            .then(function (u) {
                bezig = false; opslaan.disabled = false;
                if (!u.ok) { fouten(u.fouten && u.fouten.length ? u.fouten : ["Het sjabloon is niet bewaard."]); return; }
                dirty = false;
                window.location.href = root.getAttribute("data-lijst-url");
            })
            .catch(function (e) { bezig = false; opslaan.disabled = false; fouten([e.message || "Controleer je verbinding en probeer opnieuw."]); });
    });

    window.addEventListener("beforeunload", function (e) { if (dirty && !bezig) { e.preventDefault(); e.returnValue = ""; } });
})();
