/* gl-v2 — Instellingen › Planningssjablonen › bewerken (design-handoff 63b).
   Eén document in het geheugen (#pls-data). Structurele wijzigingen tekenen de blokken opnieuw; getypte velden schrijven rechtstreeks naar het document en verversen enkel wat ervan afhangt.
   Het voorbeeld, de proefdraai en de controle komen van de server (POST Proef: dezelfde uitrol en calculator als bij het toepassen), met een korte vertraging na elke wijziging.
   Opslaan stuurt het hele document naar PlanningSjabloonAdminController.Opslaan; de server valideert en geeft de lijst fouten terug. */
(function () {
    "use strict";
    var root = document.getElementById("pls-root");
    if (!root) return;
    var canWrite = root.getAttribute("data-can-write") === "1";
    var host = document.getElementById("pls-blokken");
    var dirtyBadge = document.getElementById("pls-dirty");
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {};
    var raw = {}, reageertOpties = [];
    try { raw = JSON.parse(document.getElementById("pls-data").textContent); } catch (e) { raw = {}; }
    try { reageertOpties = JSON.parse(document.getElementById("pls-opties").textContent); } catch (e) { reageertOpties = [{ v: "", t: "Niets — handmatig toepassen" }]; }

    var VOORWAARDEN = [
        { v: "", t: "Altijd" },
        { v: "kelder:ja", t: "Alleen met kelder" },
        { v: "dak:hellend", t: "Alleen bij hellend dak" },
        { v: "dak:plat", t: "Alleen bij plat dak" },
        { v: "terras:ja", t: "Alleen bij terrassen of luifels (per bouwlaag)" }
    ];
    // Voorwaarde per taak: één blok per bouwlaag kan taken bevatten die enkel op het gelijkvloers, zonder kelder, bij terrassen ... voorkomen.
    // Herhaling van een blok: niet herhaald, een fase per bouwlaag, of één fase met de taken per bouwlaag erin
    var HERHALINGEN = [
        { v: "0", t: "Niet per bouwlaag" },
        { v: "1", t: "Een fase per bouwlaag" },
        { v: "2", t: "Eén fase, taken per bouwlaag" }
    ];
    var TAAK_VOORWAARDEN = [
        { v: "", t: "Altijd" },
        { v: "laag:eerste", t: "Alleen op het gelijkvloers" },
        { v: "laag:eerste,kelder:nee", t: "Gelijkvloers, enkel zonder kelder" },
        { v: "laag:eerste,kelder:ja", t: "Gelijkvloers, enkel met kelder" },
        { v: "laag:boven", t: "Alleen boven het gelijkvloers" },
        { v: "laag:laatste", t: "Alleen op de bovenste bouwlaag" },
        { v: "grond:ja", t: "Gelijkvloers zonder kelder (op de grond)" },
        { v: "grond:nee", t: "Niet op de grond (met kelder, of een verdieping)" },
        { v: "laag:eenmalig", t: "Eén keer, na de bouwlagen (geen bouwlaag in de naam)" },
        { v: "laag:eenmalig,dak:plat", t: "Eén keer, enkel bij plat dak" },
        { v: "terras:ja", t: "Alleen bij terrassen of luifels" },
        { v: "kelder:ja", t: "Alleen met kelder" },
        { v: "kelder:nee", t: "Alleen zonder kelder" },
        { v: "dak:hellend", t: "Alleen bij hellend dak" },
        { v: "dak:plat", t: "Alleen bij plat dak" }
    ];
    function taakVoorwaardeItems(huidig) {
        var items = TAAK_VOORWAARDEN.slice();
        if (huidig && !items.some(function (i) { return i.v === huidig; })) items.push({ v: huidig, t: "Aangepast: " + huidig });
        return items;
    }
    var TYPES = [["FS", "na einde"], ["SS", "samen starten"], ["FF", "samen eindigen"], ["SF", "start → einde"]];

    // ── Document ──
    function leesDoc(r) {
        return {
            id: r.id || 0, naam: r.naam || "", beschrijving: r.beschrijving || "", isActief: r.isActief !== false, reageertOp: r.reageertOp || "",
            blokken: (r.blokken || []).map(function (b) {
                return {
                    code: b.code || "", naam: b.naam || "", herhaling: b.herhaling === 2 ? 2 : (b.herhaling === 1 ? 1 : 0), volgt: !!b.volgtVorigeVerdieping,
                    voorwaarde: b.voorwaardeVariabele ? b.voorwaardeVariabele + ":" + (b.voorwaardeWaarde || "") : "", open: true,
                    taken: (b.taken || []).map(function (t) {
                        return {
                            code: t.code || "", naam: t.naam || "", beschrijving: t.beschrijving || "", duur: t.duur == null ? 1 : t.duur, estafette: !!t.estafette, wacht: t.estafetteWacht || 0, voorwaarde: t.voorwaarde || "",
                            voorg: (t.voorgangers || []).map(function (v) { return { code: v.code, type: v.type || "FS", lag: v.wachtDagen || 0 }; })
                        };
                    })
                };
            })
        };
    }
    var doc = leesDoc(raw);
    var dirty = false;
    var proef = null, woningen = 1, verdiepingen = 2;
    var pick = null; // { b, t, pi }

    function markDirty() { dirty = true; if (dirtyBadge) dirtyBadge.hidden = false; vraagProef(); }

    // ── Hulpfuncties ──
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c]; }); }
    function slug(tekst) {
        var s = String(tekst || "").normalize("NFD").replace(/[̀-ͯ]/g, "").toUpperCase().replace(/[^A-Z0-9]+/g, "_").replace(/^_+|_+$/g, "");
        if (!s) s = "ITEM";
        if (/^[0-9]/.test(s)) s = "N" + s;
        return s.length > 40 ? s.slice(0, 40).replace(/_+$/, "") : s;
    }
    function alleCodes() {
        var codes = {};
        doc.blokken.forEach(function (b) { if (b.code) codes[b.code.toUpperCase()] = 1; b.taken.forEach(function (t) { if (t.code) codes[t.code.toUpperCase()] = 1; }); });
        return codes;
    }
    // Een nieuwe, unieke code uit de naam; bestaande codes blijven altijd staan, ook als je de naam later wijzigt
    function geefCode(naam) {
        var gebruikt = alleCodes(), basis = slug(naam), c = basis, n = 2;
        while (gebruikt[c]) c = basis + "_" + (n++);
        return c;
    }
    function zoekCode(code) {
        var up = String(code || "").toUpperCase().replace(/^VORIGE:/, "");
        for (var i = 0; i < doc.blokken.length; i++) {
            if ((doc.blokken[i].code || "").toUpperCase() === up) return { blok: doc.blokken[i], bi: i, taak: null };
            for (var j = 0; j < doc.blokken[i].taken.length; j++) if ((doc.blokken[i].taken[j].code || "").toUpperCase() === up) return { blok: doc.blokken[i], bi: i, taak: doc.blokken[i].taken[j], ti: j };
        }
        return null;
    }
    function naamVan(code) {
        var z = zoekCode(code), vorige = /^VORIGE:/i.test(String(code || ""));
        if (!z) return code;
        if (vorige) return "Vorige bouwlaag: " + (z.taak ? (z.taak.naam || z.taak.code) : (z.blok.naam || z.blok.code));
        return z.taak ? (z.taak.naam || z.taak.code) : (z.blok.naam || z.blok.code) + " (hele blok)";
    }
    function relTekst(v) { return (v.type !== "FS" ? v.type : "") + (v.lag ? (v.lag > 0 ? "+" : "−") + Math.abs(v.lag) + "d" : ""); }
    function zin(v) {
        var naam = naamVan(v.code), tijd = v.lag ? (v.lag > 0 ? ", " + v.lag + " werkdagen later" : ", " + (-v.lag) + " werkdagen eerder") : "";
        var basis = v.type === "FS" ? "Start na het einde van " : v.type === "SS" ? "Start samen met " : v.type === "FF" ? "Eindigt samen met " : "Eindigt na de start van ";
        return basis + naam + tijd;
    }
    function nieuwBlok() {
        var naam = "Nieuw blok", b = { code: geefCode(naam), naam: naam, herhaling: 0, volgt: false, voorwaarde: "", open: true, taken: [] };
        return b;
    }
    function nieuweTaak(blok) {
        var naam = "Nieuwe taak", vorige = blok.taken[blok.taken.length - 1];
        return { code: geefCode(naam), naam: naam, beschrijving: "", duur: 5, estafette: false, wacht: 0, voorg: vorige ? [{ code: vorige.code, type: "FS", lag: 0 }] : [] };
    }

    // GlV2-keuzelijst in JS-markup (zelfde structuur als GlV2/_SelectList, basisvariant); GlV2Select.init bedraadt ze na het tekenen
    function sel(attrs, items, waarde, placeholder, label, uit) {
        var cur = items.filter(function (i) { return i.v === waarde; })[0];
        return "<div class=\"gl-v2-select\" data-gl-v2-select data-placeholder=\"" + esc(placeholder || "") + "\"><input type=\"hidden\" " + attrs + " value=\"" + esc(waarde) + "\" />" +
            "<div class=\"gl-v2-select-trigger" + (cur ? " is-filled" : "") + "\" role=\"button\" tabindex=\"" + (uit ? -1 : 0) + "\" aria-haspopup=\"listbox\" aria-expanded=\"false\" aria-label=\"" + esc(label || "") + "\"" + (uit ? " aria-disabled=\"true\"" : "") + "><span class=\"gl-v2-select-trigger-label\">" + esc(cur ? cur.t : (placeholder || "")) + "</span><i class=\"ph ph-caret-down gl-v2-select-trigger-caret\" aria-hidden=\"true\"></i></div>" +
            "<div class=\"gl-v2-select-panel\" role=\"listbox\">" + items.map(function (i) {
                return "<button type=\"button\" class=\"gl-v2-select-option" + (i.v === waarde ? " is-selected" : "") + "\" role=\"option\" data-value=\"" + esc(i.v) + "\"><i class=\"ph ph-check\" aria-hidden=\"true\"></i><span>" + esc(i.t) + "</span></button>";
            }).join("") + "</div></div>";
    }
    function toggle(attrs, aan, tekst, hulp) {
        return "<label class=\"gl-v2-toggle-row\"><input type=\"checkbox\" class=\"gl-v2-toggle-switch\" " + attrs + (aan ? " checked" : "") + (canWrite ? "" : " disabled") + " /><span class=\"gl-v2-toggle-text\"><span class=\"gl-v2-toggle-label\">" + esc(tekst) + "</span>" + (hulp ? "<span class=\"gl-v2-toggle-desc\">" + esc(hulp) + "</span>" : "") + "</span></label>";
    }
    var SVG_WONINGEN = "<svg viewBox=\"0 0 20 20\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M2 9.5 6 6l4 3.5V15H2z\"/><path d=\"M10 11l4-3.5 4 3.5V15h-8\"/></svg>";

    // ── Blokken tekenen ──
    function chipHtml(v, bi, ti, pi) {
        var z = zoekCode(v.code), slecht = !z, rel = relTekst(v);
        return "<span class=\"gl-v2-pls-chip" + (slecht ? " is-bad" : "") + "\"><button type=\"button\" class=\"gl-v2-pls-cb\" data-act=\"v-bewerk\" data-b=\"" + bi + "\" data-t=\"" + ti + "\" data-p=\"" + pi + "\" data-code=\"" + esc(v.code) + "\" title=\"Code: " + esc(v.code) + (slecht ? " (bestaat niet)" : "") + "\"" + (canWrite ? "" : " disabled") + "><span class=\"gl-v2-pls-cl\">" + esc(slecht ? v.code : naamVan(v.code)) + "</span>" + (rel ? "<em>" + esc(rel) + "</em>" : "") + "</button>" +
            (canWrite ? "<button type=\"button\" class=\"gl-v2-pls-cx\" data-act=\"v-weg\" data-b=\"" + bi + "\" data-t=\"" + ti + "\" data-p=\"" + pi + "\" aria-label=\"" + esc(naamVan(v.code)) + " als voorganger weghalen\"><i class=\"ph ph-x\" aria-hidden=\"true\"></i></button>" : "") + "</span>";
    }
    function taakHtml(b, bi, t, ti) {
        var id = "data-b=\"" + bi + "\" data-t=\"" + ti + "\"";
        var chips = t.voorg.map(function (v, pi) { return chipHtml(v, bi, ti, pi); }).join("");
        var vgHtml = chips +
            (canWrite ? "<button type=\"button\" class=\"gl-v2-pls-add\" data-act=\"v-toevoegen\" " + id + "><i class=\"ph ph-plus\" aria-hidden=\"true\"></i>voorganger</button>" : "") +
            (!t.voorg.length && !canWrite ? "<span class=\"gl-v2-pls-geen\">geen</span>" : "");
        return "<div class=\"gl-v2-pls-tr\" data-taak-rij " + id + ">" +
            "<span class=\"gl-v2-pls-grip\" data-grip title=\"Sleep om te herschikken\" aria-hidden=\"true\"><i class=\"ph ph-dots-six-vertical\"></i></span>" +
            "<div class=\"gl-v2-pls-nm\"><div class=\"gl-v2-field-box\"><input type=\"text\" class=\"gl-v2-field-input\" data-f=\"t-naam\" " + id + " value=\"" + esc(t.naam) + "\" maxlength=\"200\" placeholder=\"Naam van de taak\" aria-label=\"Naam van de taak\"" + (canWrite ? "" : " disabled") + " /></div><small data-code-van=\"" + esc(t.code) + "\">" + esc(t.code) + "</small>" +
            "<div class=\"gl-v2-pls-tw\">" + sel("data-f=\"t-voorwaarde\" " + id, taakVoorwaardeItems(t.voorwaarde), t.voorwaarde || "", "Altijd", "Komt voor", !canWrite) + "</div></div>" +
            "<div class=\"gl-v2-field-box gl-v2-pls-dur\"><input type=\"text\" inputmode=\"numeric\" maxlength=\"4\" autocomplete=\"off\" class=\"gl-v2-field-input\" data-f=\"t-duur\" " + id + " value=\"" + esc(t.duur) + "\" aria-label=\"Duur in werkdagen\"" + (canWrite ? "" : " disabled") + " /><span class=\"gl-v2-field-unit\">d</span></div>" +
            "<div class=\"gl-v2-pls-ps\">" + vgHtml + "</div>" +
            "<span class=\"gl-v2-pls-st\" data-start-van=\"" + esc(t.code) + "\"></span>" +
            "<button type=\"button\" class=\"gl-v2-pls-est" + (t.estafette ? " is-on" : "") + "\" data-act=\"t-est\" " + id + " aria-pressed=\"" + (t.estafette ? "true" : "false") + "\" title=\"" + esc(t.estafette ? "Wacht op de vorige woning" + (t.wacht ? " + " + t.wacht + " d" : "") : "Estafette uit") + "\"" + (canWrite ? "" : " disabled") + ">" + SVG_WONINGEN + "</button>" +
            (canWrite ? "<button type=\"button\" class=\"gl-v2-icon-btn gl-v2-pls-menubtn js-gl-v2-menu-trigger\" data-kind=\"t\" " + id + " aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"pls-menu\" aria-label=\"Acties voor " + esc(t.naam) + "\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button>" : "<span></span>") +
            "</div>";
    }
    function blokHtml(b, bi) {
        var id = "data-b=\"" + bi + "\"";
        var voorwaarde = VOORWAARDEN.filter(function (v) { return v.v === b.voorwaarde; })[0] || VOORWAARDEN[0];
        return "<article class=\"gl-v2-section-card gl-v2-pls-blok" + (b.open ? " is-open" : "") + "\" " + id + ">" +
            "<header class=\"gl-v2-pls-bh\">" +
            "<button type=\"button\" class=\"gl-v2-pls-chev\" data-act=\"b-open\" " + id + " aria-expanded=\"" + (b.open ? "true" : "false") + "\" aria-label=\"" + (b.open ? "Blok inklappen" : "Blok uitklappen") + "\"><i class=\"ph " + (b.open ? "ph-caret-down" : "ph-caret-right") + "\" aria-hidden=\"true\"></i></button>" +
            "<div class=\"gl-v2-pls-bt\"><input type=\"text\" class=\"gl-v2-pls-bn\" data-f=\"b-naam\" " + id + " value=\"" + esc(b.naam) + "\" maxlength=\"200\" placeholder=\"Naam van het blok\" aria-label=\"Naam van het blok\"" + (canWrite ? "" : " disabled") + " />" +
            "<span><code data-code-van=\"" + esc(b.code) + "\">" + esc(b.code) + "</code><span class=\"gl-v2-pls-meta\" data-meta=\"" + bi + "\">" + b.taken.length + (b.taken.length === 1 ? " taak" : " taken") + "</span></span></div>" +
            "<span class=\"gl-v2-pls-spacer\"></span>" +
            "<div class=\"gl-v2-pls-when\"><span class=\"gl-v2-pls-when-l\">Komt voor</span>" + sel("data-f=\"b-voorwaarde\" " + id, VOORWAARDEN, b.voorwaarde, "Altijd", "Komt voor", !canWrite) + "</div>" +
            "<div class=\"gl-v2-pls-floor\">" + sel("data-f=\"b-herhaling\" " + id, HERHALINGEN, String(b.herhaling || 0), "Niet per bouwlaag", "Herhaling per bouwlaag", !canWrite) + "</div>" +
            (canWrite ? "<button type=\"button\" class=\"gl-v2-icon-btn gl-v2-pls-menubtn js-gl-v2-menu-trigger\" data-kind=\"b\" " + id + " aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"pls-menu\" aria-label=\"Acties voor blok " + esc(b.naam) + "\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button>" : "") +
            "</header>" +
            (b.open ? "<div class=\"gl-v2-pls-th\"><span></span><span>TAAK</span><span class=\"r\">DUUR</span><span>VOORGANGERS</span><span>START</span><span>ESTAF.</span><span></span></div>" +
                (b.taken.length ? b.taken.map(function (t, ti) { return taakHtml(b, bi, t, ti); }).join("") : "<div class=\"gl-v2-pls-leeg\">Nog geen taken in dit blok.</div>") +
                "<div class=\"gl-v2-pls-bf\">" + (canWrite ? "<button type=\"button\" class=\"gl-v2-pls-addt\" data-act=\"t-toevoegen\" " + id + "><i class=\"ph ph-plus\" aria-hidden=\"true\"></i>Taak toevoegen</button>" : "") +
                (b.herhaling === 1 ? toggle("data-f=\"b-volgt\" " + id, b.volgt, "Elke verdieping start na de vorige") : "") + "</div>" : "") +
            "</article>";
    }
    function render() {
        if (!doc.blokken.length) host.innerHTML = "<div class=\"gl-v2-section-card gl-v2-pls-leeg is-big\"><i class=\"ph ph-stack\" aria-hidden=\"true\"></i><b>Nog geen blokken</b><span>Een blok is een fase van de planning, bijvoorbeeld Fundering of Ruwbouw. Voeg er één toe en zet er taken in.</span></div>";
        else host.innerHTML = doc.blokken.map(blokHtml).join("");
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(host);
        toonProef();
    }
    // In-place verversen na een proefdraai of naamwijziging, zonder focus te verliezen
    function verversNamen() {
        host.querySelectorAll(".gl-v2-pls-cb[data-code]").forEach(function (b) {
            var l = b.querySelector(".gl-v2-pls-cl"), c = b.getAttribute("data-code"), z = zoekCode(c);
            if (l) l.textContent = z ? naamVan(c) : c;
        });
    }

    // ── Rechts: voorbeeld, proefdraai en controle ──
    var timer = null, volgnr = 0;
    function vraagProef() {
        window.clearTimeout(timer);
        timer = window.setTimeout(doeProef, 350);
    }
    function token() { var t = document.querySelector("input[name=\"__RequestVerificationToken\"]"); return t ? t.value : ""; }
    function payload() {
        return {
            id: doc.id, naam: doc.naam, beschrijving: doc.beschrijving, isActief: doc.isActief, reageertOp: doc.reageertOp,
            blokken: doc.blokken.map(function (b) {
                var v = b.voorwaarde ? b.voorwaarde.split(":") : [];
                return {
                    code: b.code, naam: b.naam, herhaling: b.herhaling, volgtVorigeVerdieping: b.herhaling === 1 && b.volgt, voorwaardeVariabele: v[0] || null, voorwaardeWaarde: v[1] || null,
                    taken: b.taken.map(function (t) {
                        return { code: t.code, naam: t.naam, beschrijving: t.beschrijving, duur: t.duur, estafette: t.estafette, estafetteWacht: t.estafette ? t.wacht : 0, voorwaarde: t.voorwaarde || null,
                            voorgangers: t.voorg.map(function (v2) { return { code: v2.code, type: v2.type, wachtDagen: v2.lag }; }) };
                    })
                };
            })
        };
    }
    function doeProef() {
        var nr = ++volgnr;
        fetch(root.getAttribute("data-proef-url"), {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify({ sjabloon: payload(), woningen: woningen, verdiepingen: verdiepingen })
        }).then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
          .then(function (u) { if (nr !== volgnr) return; proef = u; toonProef(); })
          .catch(function () { if (nr === volgnr) { proef = null; toonProef(true); } });
    }
    function tekst(id, t) { var el = document.getElementById(id); if (el) el.textContent = t; }
    function balk(start, einde, totaal, extra) {
        var l = totaal > 0 ? ((start - 1) / totaal * 100) : 0, w = totaal > 0 ? Math.max(1.2, (einde - start + 1) / totaal * 100) : 0;
        return "<span class=\"gl-v2-pls-pb" + (extra || "") + "\"><i style=\"left:" + l.toFixed(2) + "%;width:" + w.toFixed(2) + "%\"></i></span>";
    }
    function toonProef(fout) {
        var p = proef, nT = 0;
        doc.blokken.forEach(function (b) { nT += b.taken.length; });
        tekst("pls-vb-sub", doc.blokken.length + (doc.blokken.length === 1 ? " blok · " : " blokken · ") + nT + (nT === 1 ? " taak" : " taken") + " · 1 woning");

        // Start per taak en meta per blok, in de lijst zelf
        host.querySelectorAll("[data-start-van]").forEach(function (el) { el.innerHTML = ""; });
        host.querySelectorAll("[data-meta]").forEach(function (el) { var bi = parseInt(el.getAttribute("data-meta"), 10), b = doc.blokken[bi]; el.textContent = b.taken.length + (b.taken.length === 1 ? " taak" : " taken"); });
        if (p) {
            p.taken.forEach(function (t) {
                host.querySelectorAll("[data-start-van=\"" + t.code + "\"]").forEach(function (el) { el.innerHTML = "dag " + t.start + (t.kritiek ? " <b class=\"gl-v2-pls-k\" title=\"Op het kritieke pad\">K</b>" : ""); });
            });
            p.blokken.forEach(function (pb) {
                var bi = doc.blokken.findIndex(function (b) { return b.code === pb.code; });
                var el = bi >= 0 ? host.querySelector("[data-meta=\"" + bi + "\"]") : null;
                if (el) { var b = doc.blokken[bi]; el.textContent = b.taken.length + (b.taken.length === 1 ? " taak · " : " taken · ") + pb.duur + " werkdagen" + (b.herhaling === 1 ? " per verdieping" : (b.herhaling === 2 ? " per bouwlaag" : "")); }
            });
        }

        // Voorbeeld
        if (!p) {
            tekst("pls-vb-tot", fout ? "Voorbeeld niet beschikbaar" : "—"); tekst("pls-vb-mnd", "");
            document.getElementById("pls-vb-blokken").innerHTML = ""; document.getElementById("pls-vb-woningen").innerHTML = "";
            tekst("pls-vb-alle", ""); document.getElementById("pls-vb-crit").hidden = true;
            document.getElementById("pls-ct").innerHTML = fout ? "<div class=\"gl-v2-pls-iss is-goud\"><b>Controle niet beschikbaar</b><span>De server gaf geen antwoord. Opslaan controleert alles opnieuw.</span></div>" : "";
            tekst("pls-ct-sub", "");
            return;
        }
        tekst("pls-vb-tot", p.werkdagen + " werkdagen");
        tekst("pls-vb-mnd", p.werkdagen ? "± " + p.maanden + (p.maanden === 1 ? " maand" : " maanden") : "");
        var html = "";
        p.blokken.forEach(function (pb) {
            var b = doc.blokken.filter(function (x) { return x.code === pb.code; })[0] || {};
            html += "<div class=\"gl-v2-pls-pr\"><span class=\"gl-v2-pls-pn\">" + esc(pb.naam) + (b.voorwaarde ? "<i>voorwaarde</i>" : "") + (b.herhaling === 1 ? "<i>× verd.</i>" : (b.herhaling === 2 ? "<i>per laag</i>" : "")) + "</span>" + balk(pb.start, pb.einde, p.werkdagen) + "<span class=\"gl-v2-pls-pd\">" + pb.duur + " d</span></div>";
        });
        document.getElementById("pls-vb-blokken").innerHTML = html || "<div class=\"gl-v2-pls-geen\">Nog niets om te tonen.</div>";
        var crit = document.getElementById("pls-vb-crit");
        crit.hidden = !p.kritiek.length;
        crit.innerHTML = p.kritiek.length ? "<i class=\"ph ph-flag-banner\" aria-hidden=\"true\"></i><span><b>Kritiek pad</b> · " + p.kritiek.map(esc).join(" → ") + "</span>" : "";

        // Proefdraaien
        var w = "";
        p.woningen.forEach(function (x, i) { w += "<div class=\"gl-v2-pls-pr\"><span class=\"gl-v2-pls-pn\">Woning " + (i + 1) + "</span>" + balk(x.start, x.einde, p.werkdagenAlle, " is-u") + "<span class=\"gl-v2-pls-ud\">dag " + x.start + " – " + x.einde + "</span></div>"; });
        document.getElementById("pls-vb-woningen").innerHTML = w;
        tekst("pls-vb-alle", p.werkdagenAlle + " werkdagen voor " + woningen + (woningen === 1 ? " woning" : " woningen"));

        // Controle
        var rood = p.meldingen.filter(function (m) { return m.ernst === "rood"; }), goud = p.meldingen.filter(function (m) { return m.ernst !== "rood"; });
        var ct = document.getElementById("pls-ct");
        if (!p.meldingen.length) { ct.innerHTML = "<div class=\"gl-v2-pls-ok\"><i class=\"ph ph-check-circle\" aria-hidden=\"true\"></i>Geen kringen, geen onbekende codes, alles hangt aan elkaar.</div>"; tekst("pls-ct-sub", "alles in orde"); }
        else {
            ct.innerHTML = rood.concat(goud).map(function (m) { return "<div class=\"gl-v2-pls-iss " + (m.ernst === "rood" ? "is-rood" : "is-goud") + "\"><b>" + esc(m.titel) + "</b>" + (m.tekst ? "<span>" + esc(m.tekst) + "</span>" : "") + "</div>"; }).join("");
            tekst("pls-ct-sub", p.meldingen.length + (p.meldingen.length === 1 ? " aandachtspunt" : " aandachtspunten"));
        }
    }
    function segment(hostId, waarden, huidig, zet) {
        var el = document.getElementById(hostId);
        el.innerHTML = waarden.map(function (n) { return "<button type=\"button\" data-n=\"" + n + "\" aria-pressed=\"" + (n === huidig ? "true" : "false") + "\" class=\"" + (n === huidig ? "is-active" : "") + "\">" + n + "</button>"; }).join("");
        el.onclick = function (e) {
            var b = e.target.closest("[data-n]"); if (!b) return;
            zet(parseInt(b.getAttribute("data-n"), 10));
            segment(hostId, waarden, parseInt(b.getAttribute("data-n"), 10), zet);
            doeProef();
        };
    }
    segment("pls-seg-woningen", [1, 2, 3, 4], woningen, function (n) { woningen = n; });
    segment("pls-seg-verdiepingen", [1, 2, 3], verdiepingen, function (n) { verdiepingen = n; });
    document.getElementById("pls-uitleg-kop").addEventListener("click", function () {
        var kop = document.getElementById("pls-uitleg-kop"), lijst = document.getElementById("pls-uitleg-lijst"), open = lijst.hidden;
        lijst.hidden = !open; kop.setAttribute("aria-expanded", open ? "true" : "false");
        kop.classList.toggle("is-open", open);
    });

    // ── Velden en knoppen in de blokken ──
    function blokVan(el) { return doc.blokken[parseInt(el.getAttribute("data-b"), 10)]; }
    function taakVan(el) { var b = blokVan(el); return b ? b.taken[parseInt(el.getAttribute("data-t"), 10)] : null; }
    function schuif(lijst, i, d) { var j = i + d; if (j < 0 || j >= lijst.length) return false; var x = lijst[i]; lijst[i] = lijst[j]; lijst[j] = x; return true; }
    function weg(titel, tekst2, doe) {
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: titel, desc: tekst2, ok: "Verwijderen", tone: "danger", icon: "ph-trash" }).then(function (ok) { if (ok) doe(); });
        else doe();
    }

    host.addEventListener("input", function (e) {
        if (!canWrite) return;
        var f = e.target.getAttribute("data-f"); if (!f) return;
        if (f === "b-naam") { blokVan(e.target).naam = e.target.value; verversNamen(); }
        else if (f === "t-naam") { taakVan(e.target).naam = e.target.value; verversNamen(); }
        else if (f === "t-duur") { var cijfers = e.target.value.replace(/\D/g, ""); if (cijfers !== e.target.value) e.target.value = cijfers; taakVan(e.target).duur = cijfers === "" ? 0 : parseInt(cijfers, 10); }
        else return;
        markDirty();
    });
    host.addEventListener("change", function (e) {
        if (!canWrite) return;
        var f = e.target.getAttribute("data-f"); if (!f) return;
        if (f === "t-naam" || f === "b-naam") {
            // Een nog automatische standaardcode (NIEUWE_TAAK, NIEUW_BLOK, met nummer) volgt de naam; een andere code blijft altijd staan.
            var isB = f === "b-naam", item = isB ? blokVan(e.target) : taakVan(e.target), oud = item.code;
            if (!new RegExp("^" + (isB ? "NIEUW_BLOK" : "NIEUWE_TAAK") + "(_\\d+)?$").test(String(oud || "").toUpperCase()) || !String(item.naam || "").trim()) return;
            var nieuw = geefCode(item.naam);
            if (nieuw.toUpperCase() === String(oud).toUpperCase()) return;
            item.code = nieuw;
            doc.blokken.forEach(function (bl) { bl.taken.forEach(function (tk) { (tk.voorg || []).forEach(function (v) { if (String(v.code).toUpperCase() === String(oud).toUpperCase()) v.code = nieuw; }); }); });
            markDirty(); render(); return;
        }
        if (f === "b-voorwaarde") blokVan(e.target).voorwaarde = e.target.value;
        else if (f === "t-voorwaarde") { var tw = taakVan(e.target); if (tw) tw.voorwaarde = e.target.value; }
        else if (f === "b-herhaling") { var b = blokVan(e.target); b.herhaling = parseInt(e.target.value, 10) || 0; if (b.herhaling !== 1) b.volgt = false; render(); }
        else if (f === "b-volgt") blokVan(e.target).volgt = e.target.checked;
        else return;
        markDirty();
    });
    host.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-act]"); if (!btn || btn.disabled) return;
        var act = btn.getAttribute("data-act"), bi = parseInt(btn.getAttribute("data-b"), 10), b = doc.blokken[bi];
        if (!b) return;
        if (act === "b-open") { b.open = !b.open; render(); return; }
        if (!canWrite) return;
        var ti = parseInt(btn.getAttribute("data-t"), 10), t = b.taken[ti], pi = parseInt(btn.getAttribute("data-p"), 10);
        if (act === "t-toevoegen") {
            b.taken.push(nieuweTaak(b)); b.open = true; markDirty(); render();
            var rijen = host.querySelectorAll("[data-b=\"" + bi + "\"][data-taak-rij] input[data-f=\"t-naam\"]"), l = rijen[rijen.length - 1];
            if (l) { l.focus(); l.select(); }
        }
        else if (act === "t-est") { t.estafette = !t.estafette; if (!t.estafette) t.wacht = 0; markDirty(); render(); }
        else if (act === "v-toevoegen") openPick(bi, ti, -1);
        else if (act === "v-bewerk") openPick(bi, ti, pi);
        else if (act === "v-weg") { t.voorg.splice(pi, 1); markDirty(); render(); }
    });

    // ── Voorganger kiezen of aanpassen (modal) ──
    var pickEl = document.getElementById("pls-pick"), pickModal = null;
    function modal() { if (!pickModal && window.bootstrap) pickModal = window.bootstrap.Modal.getOrCreateInstance(pickEl); return pickModal; }
    function openPick(bi, ti, pi) {
        pick = { b: bi, t: ti, p: pi };
        document.getElementById("pls-pick-q").value = "";
        tekstPick();
        var m = modal(); if (m) m.show();
        window.setTimeout(function () { document.getElementById("pls-pick-q").focus(); }, 300);
    }
    function tekstPick() {
        if (!pick) return;
        var t = doc.blokken[pick.b].taken[pick.t], v = pick.p >= 0 ? t.voorg[pick.p] : null;
        document.getElementById("pls-pick-titel").textContent = t.naam || t.code;
        var rel = document.getElementById("pls-pick-rel"), z = document.getElementById("pls-pick-zin");
        rel.hidden = !v; z.hidden = !v;
        if (v) {
            document.getElementById("pls-pick-types").innerHTML = TYPES.map(function (x) { return "<button type=\"button\" data-type=\"" + x[0] + "\" class=\"" + (v.type === x[0] ? "is-active" : "") + "\" aria-pressed=\"" + (v.type === x[0] ? "true" : "false") + "\" title=\"" + x[1] + "\">" + x[0] + "</button>"; }).join("");
            document.getElementById("pls-pick-lag").value = v.lag;
            z.textContent = zin(v);
        }
        renderPickLijst();
    }
    function renderPickLijst() {
        if (!pick) return;
        var eigen = doc.blokken[pick.b], t = eigen.taken[pick.t], v = pick.p >= 0 ? t.voorg[pick.p] : null;
        var q = document.getElementById("pls-pick-q").value.trim().toLowerCase(), html = "";
        var gebruikt = {}; t.voorg.forEach(function (x, i) { if (i !== pick.p) gebruikt[x.code.toUpperCase()] = 1; });
        doc.blokken.forEach(function (b, bi) {
            var items = [];
            if (bi !== pick.b && b.code) items.push({ code: b.code, n: "Hele blok " + b.naam, blk: true });
            b.taken.forEach(function (x) { if (x.code && x !== t) items.push({ code: x.code, n: x.naam || x.code }); });
            // Een taak in een blok dat per verdieping terugkomt kan ook als "dezelfde taak in de vorige bouwlaag" voorganger zijn (bv. de afdek van de vorige bouwlaag).
            if (b.herhaling >= 1 && eigen.herhaling >= 1) b.taken.forEach(function (x) { if (x.code) items.push({ code: "VORIGE:" + x.code, n: "Vorige bouwlaag: " + (x.naam || x.code) }); });
            items = items.filter(function (x) { return !gebruikt[x.code.toUpperCase()] && (!q || x.n.toLowerCase().indexOf(q) >= 0 || x.code.toLowerCase().indexOf(q) >= 0); });
            if (!items.length) return;
            html += "<div class=\"gl-v2-pls-pg\">" + esc(b.naam) + "</div>" + items.map(function (x) {
                var aan = v && v.code.toUpperCase() === x.code.toUpperCase();
                return "<button type=\"button\" class=\"gl-v2-pls-po" + (aan ? " is-on" : "") + (x.blk ? " is-blk" : "") + "\" data-code=\"" + esc(x.code) + "\"><span>" + esc(x.n) + "</span><code>" + esc(x.code) + "</code></button>";
            }).join("");
        });
        document.getElementById("pls-pick-lijst").innerHTML = html || "<div class=\"gl-v2-pls-geen\">Niets gevonden.</div>";
    }
    document.getElementById("pls-pick-q").addEventListener("input", renderPickLijst);
    document.getElementById("pls-pick-lijst").addEventListener("click", function (e) {
        var o = e.target.closest("[data-code]"); if (!o || !pick) return;
        var t = doc.blokken[pick.b].taken[pick.t], code = o.getAttribute("data-code");
        if (pick.p >= 0) { t.voorg[pick.p].code = code; tekstPick(); }
        else { t.voorg.push({ code: code, type: "FS", lag: 0 }); var m = modal(); if (m) m.hide(); }
        markDirty(); render();
    });
    document.getElementById("pls-pick-types").addEventListener("click", function (e) {
        var b = e.target.closest("[data-type]"); if (!b || !pick || pick.p < 0) return;
        doc.blokken[pick.b].taken[pick.t].voorg[pick.p].type = b.getAttribute("data-type");
        tekstPick(); markDirty(); render();
    });
    document.getElementById("pls-pick-lag").addEventListener("input", function (e) {
        if (!pick || pick.p < 0) return;
        var v = doc.blokken[pick.b].taken[pick.t].voorg[pick.p];
        v.lag = e.target.value === "" || e.target.value === "-" ? 0 : (parseInt(e.target.value, 10) || 0);
        document.getElementById("pls-pick-zin").textContent = zin(v);
        markDirty(); render();
    });
    pickEl.addEventListener("hidden.bs.modal", function () { pick = null; });

    // ── ⋯-menu's (blok en taak) ──
    var menu = document.getElementById("pls-menu"), menuCtx = null;
    function mi(act, icon, label, opts) {
        opts = opts || {};
        return "<button type=\"button\" class=\"gl-v2-menu-item" + (opts.danger ? " is-danger" : "") + (opts.uit ? " is-disabled" : "") + "\" role=\"menuitem\" data-menu=\"" + act + "\"" + (opts.uit ? " disabled" : "") + "><i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span>" + (opts.sub ? "<span class=\"gl-v2-menu-item-sub\">" + esc(opts.sub) + "</span>" : "") + "</span></button>";
    }
    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-menu-trigger[aria-controls=\"pls-menu\"]");
        if (!trigger) return;
        menuCtx = { kind: trigger.getAttribute("data-kind"), b: parseInt(trigger.getAttribute("data-b"), 10), t: parseInt(trigger.getAttribute("data-t"), 10) };
        var b = doc.blokken[menuCtx.b]; if (!b) return;
        var h = "";
        if (menuCtx.kind === "b") {
            h += mi("omhoog", "ph-arrow-up", "Blok omhoog", { uit: menuCtx.b === 0 }) + mi("omlaag", "ph-arrow-down", "Blok omlaag", { uit: menuCtx.b === doc.blokken.length - 1 });
            h += mi("dupliceer", "ph-copy", "Dupliceren", { sub: "met eigen codes" }) + mi("weg", "ph-trash", "Blok verwijderen", { danger: true });
        } else {
            var t = b.taken[menuCtx.t];
            h += mi("omhoog", "ph-arrow-up", "Omhoog", { uit: menuCtx.t === 0 }) + mi("omlaag", "ph-arrow-down", "Omlaag", { uit: menuCtx.t === b.taken.length - 1 });
            if (t.estafette) {
                h += mi("est-plus", "ph-plus-circle", "Estafette + 1 werkdag wachten", { sub: t.wacht ? "nu " + t.wacht + " werkdagen extra" : "" });
                if (t.wacht) h += mi("est-nul", "ph-minus-circle", "Extra wachtdagen wissen");
            }
            h += mi("weg", "ph-trash", "Taak verwijderen", { danger: true });
        }
        menu.innerHTML = h;
    }, true);
    menu.addEventListener("click", function (e) {
        var o = e.target.closest("[data-menu]"); if (!o || o.disabled || !menuCtx || !canWrite) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        var act = o.getAttribute("data-menu"), bi = menuCtx.b, ti = menuCtx.t, b = doc.blokken[bi];
        if (menuCtx.kind === "b") {
            if (act === "omhoog") schuif(doc.blokken, bi, -1);
            else if (act === "omlaag") schuif(doc.blokken, bi, 1);
            else if (act === "dupliceer") {
                var kopie = JSON.parse(JSON.stringify(b)), kaart = {};
                kopie.naam = b.naam + " (kopie)"; kopie.code = geefCode(kopie.naam);
                var bezet = alleCodes(); bezet[kopie.code.toUpperCase()] = 1;
                kopie.taken.forEach(function (t) {
                    var basis = slug(t.naam), c = basis, n = 2;
                    while (bezet[c]) c = basis + "_" + (n++);
                    bezet[c] = 1; kaart[t.code] = c; t.code = c;
                });
                kopie.taken.forEach(function (t) { t.voorg.forEach(function (v) { if (kaart[v.code]) v.code = kaart[v.code]; }); });
                doc.blokken.splice(bi + 1, 0, kopie);
                toast("success", "Blok gedupliceerd", "Met eigen codes.");
            } else if (act === "weg") {
                weg("Blok verwijderen?", "Het blok “" + (b.naam || "zonder naam") + "” en zijn " + b.taken.length + (b.taken.length === 1 ? " taak" : " taken") + " worden uit het sjabloon gehaald. Voorgangers die ernaar verwijzen worden rood, tot je ze aanpast of weghaalt.",
                    function () { doc.blokken.splice(bi, 1); markDirty(); render(); });
                return;
            }
        } else {
            var t2 = b.taken[ti];
            if (act === "omhoog") schuif(b.taken, ti, -1);
            else if (act === "omlaag") schuif(b.taken, ti, 1);
            else if (act === "est-plus") t2.wacht = (t2.wacht || 0) + 1;
            else if (act === "est-nul") t2.wacht = 0;
            else if (act === "weg") {
                weg("Taak verwijderen?", "De taak “" + (t2.naam || "zonder naam") + "” wordt uit het blok gehaald. Voorgangers die ernaar verwijzen worden rood.", function () { b.taken.splice(ti, 1); markDirty(); render(); });
                return;
            }
        }
        markDirty(); render();
    });

    // ── Herschikken met slepen (taken binnen een blok, via het handvat) ──
    var sleep = null;
    host.addEventListener("pointerdown", function (e) {
        var grip = e.target.closest("[data-grip]"); if (!grip || !canWrite) return;
        var rij = grip.closest("[data-taak-rij]"); if (rij) rij.setAttribute("draggable", "true");
    });
    host.addEventListener("dragstart", function (e) {
        var rij = e.target.closest && e.target.closest("[data-taak-rij]"); if (!rij || rij.getAttribute("draggable") !== "true") { return; }
        sleep = { b: parseInt(rij.getAttribute("data-b"), 10), t: parseInt(rij.getAttribute("data-t"), 10) };
        rij.classList.add("is-sleep");
        if (e.dataTransfer) { e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", "taak"); } catch (x) { /* oudere browsers */ } }
    });
    host.addEventListener("dragover", function (e) {
        if (!sleep) return;
        var rij = e.target.closest("[data-taak-rij]"); if (!rij || parseInt(rij.getAttribute("data-b"), 10) !== sleep.b) return;
        e.preventDefault();
        host.querySelectorAll(".is-doel").forEach(function (x) { x.classList.remove("is-doel"); });
        rij.classList.add("is-doel");
    });
    host.addEventListener("drop", function (e) {
        if (!sleep) return;
        var rij = e.target.closest("[data-taak-rij]"); if (!rij || parseInt(rij.getAttribute("data-b"), 10) !== sleep.b) return;
        e.preventDefault();
        var naar = parseInt(rij.getAttribute("data-t"), 10), lijst = doc.blokken[sleep.b].taken;
        if (naar !== sleep.t) { var x = lijst.splice(sleep.t, 1)[0]; lijst.splice(naar, 0, x); sleep = null; markDirty(); render(); }
    });
    host.addEventListener("pointerup", function () { if (!sleep) host.querySelectorAll("[data-taak-rij][draggable]").forEach(function (x) { x.removeAttribute("draggable"); }); });
    host.addEventListener("dragend", function () { sleep = null; host.querySelectorAll(".is-sleep,.is-doel").forEach(function (x) { x.classList.remove("is-sleep", "is-doel"); x.removeAttribute("draggable"); }); });

    // ── Kop: naam, omschrijving, reageert op, actief ──
    document.getElementById("pls-blok-toevoegen") && document.getElementById("pls-blok-toevoegen").addEventListener("click", function () {
        if (!canWrite) return;
        var b = nieuwBlok(); b.taken.push(nieuweTaak(b)); doc.blokken.push(b); markDirty(); render();
        var rijen = host.querySelectorAll("input[data-f=\"b-naam\"]"), l = rijen[rijen.length - 1]; if (l) { l.focus(); l.select(); }
    });
    document.getElementById("pls-alles-open").addEventListener("click", function () { doc.blokken.forEach(function (b) { b.open = true; }); render(); });
    document.getElementById("pls-alles-dicht").addEventListener("click", function () { doc.blokken.forEach(function (b) { b.open = false; }); render(); });
    ["pls-naam", "pls-beschrijving"].forEach(function (id) {
        var el = document.getElementById(id);
        el.value = id === "pls-naam" ? doc.naam : doc.beschrijving;
        el.disabled = !canWrite;
        el.addEventListener("input", function () { if (id === "pls-naam") doc.naam = el.value; else doc.beschrijving = el.value; dirty = true; if (dirtyBadge) dirtyBadge.hidden = false; });
    });
    var actief = document.getElementById("pls-actief");
    actief.checked = doc.isActief; actief.disabled = !canWrite;
    actief.addEventListener("change", function () { doc.isActief = actief.checked; dirty = true; if (dirtyBadge) dirtyBadge.hidden = false; });
    var trigHost = document.getElementById("pls-trig-host");
    trigHost.innerHTML = sel("data-f=\"reageert\"", reageertOpties, doc.reageertOp || "", reageertOpties[0].t, "Reageert op", !canWrite);
    if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(trigHost);
    trigHost.addEventListener("change", function (e) { if (e.target.getAttribute("data-f") === "reageert") { doc.reageertOp = e.target.value; dirty = true; if (dirtyBadge) dirtyBadge.hidden = false; } });

    // ── Opslaan en annuleren ──
    var fouten = document.getElementById("pls-fouten"), lijstEl = document.getElementById("pls-fouten-lijst");
    function toonFouten(lijst) {
        lijstEl.innerHTML = lijst.map(function (f) { return "<li>" + esc(f) + "</li>"; }).join("");
        fouten.hidden = false;
        fouten.scrollIntoView({ block: "center", behavior: "smooth" });
    }
    document.getElementById("pls-fouten-close").addEventListener("click", function () { fouten.hidden = true; });
    var knop = document.getElementById("pls-opslaan");
    var verlaatEl = document.getElementById("pls-verlaat"), verlaatModal = null, doelUrl = null;
    function verlaatM() { if (!verlaatModal && window.bootstrap && verlaatEl) verlaatModal = window.bootstrap.Modal.getOrCreateInstance(verlaatEl); return verlaatModal; }
    function sluitVerlaat() { var m = verlaatM(); if (m) m.hide(); }

    // Bewaart het sjabloon; met naUrl gaat de pagina daarna naar die plek (verlaten), anders naar de bewerkpagina van het sjabloon.
    function bewaar(naUrl) {
        var rood = proef ? proef.meldingen.filter(function (m) { return m.ernst === "rood"; }) : [];
        if (!(doc.naam || "").trim()) { sluitVerlaat(); toonFouten(["Geef het sjabloon een naam."]); document.getElementById("pls-naam").focus(); return; }
        if (rood.length) { sluitVerlaat(); toonFouten(rood.map(function (m) { return m.titel + (m.tekst ? ": " + m.tekst : ""); })); toast("danger", "Los eerst de rode punten op", ""); return; }
        if (knop) knop.disabled = true;
        fetch(root.getAttribute("data-opslaan-url"), {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(payload())
        }).then(function (r) {
            if (r.status === 403) throw new Error("Je hebt hier geen recht toe.");
            if (!r.ok) throw new Error("Opslaan mislukt. Probeer het zo opnieuw.");
            return r.json();
        }).then(function (u) {
            if (u.ok) { dirty = false; window.location.href = naUrl || root.getAttribute("data-bewerken-url").replace(/\/0\/Bewerken/i, "/" + u.sjabloonId + "/Bewerken"); return; }
            if (knop) knop.disabled = false;
            sluitVerlaat();
            toonFouten(u.fouten && u.fouten.length ? u.fouten : ["Het sjabloon kon niet bewaard worden."]);
        }).catch(function (err) { if (knop) knop.disabled = false; sluitVerlaat(); toonFouten([err.message || "Opslaan mislukt. Controleer je verbinding."]); });
    }
    if (knop) knop.addEventListener("click", function () { bewaar(null); });

    // Weggaan met niet-bewaarde wijzigingen (terugpijl, Annuleren, broodkruimel, menu): eerst een eigen modal "Wijzigingen bewaren?"
    document.addEventListener("click", function (e) {
        if (!dirty) return;
        var a = e.target.closest("a[href]");
        if (!a || a.closest("#pls-verlaat")) return;
        var h = a.getAttribute("href");
        if (!h || h.charAt(0) === "#" || a.target === "_blank" || /^(javascript|mailto|tel):/i.test(h)) return;
        e.preventDefault(); e.stopPropagation();
        doelUrl = a.href;
        var m = verlaatM(); if (m) m.show(); else if (window.confirm("Je wijzigingen zijn nog niet bewaard. Toch weggaan?")) { dirty = false; window.location.href = doelUrl; }
    }, true);
    if (verlaatEl) verlaatEl.addEventListener("click", function (e) {
        var b = e.target.closest("[data-verlaat]"); if (!b) return;
        var wat = b.getAttribute("data-verlaat");
        if (wat === "weg") { dirty = false; window.location.href = doelUrl; }
        else if (wat === "bewaar") bewaar(doelUrl);
    });
    window.addEventListener("beforeunload", function (e) { if (dirty) { e.preventDefault(); e.returnValue = ""; } });

    render();
    doeProef();
})();
