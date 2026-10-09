/* gl-v2 — Budgetflow stap 8 · Verkoop (design-handoff 39i): rekenregels per eenheid (dezelfde als ServiceCore.Budget.VerkoopLijnRekenregel),
   hoofdrij (vraagprijs) + detailregel (code → €/m² → bedrag per zijde, bron, ruil, forfait, unit), overnemen uit voorstel/markt,
   waarschuwing onder het minimum, autosave (SaveBudgetVerkoop, alle lijnen in één POST) en doorzetten naar units.
   Bronnen (BOCore.VerkoopPrijsBron): 1 voorstel · 2 markt · 3 code · 4 manueel €/m² · 5 manueel bedrag. */
(function () {
    "use strict";
    var root = document.getElementById("bw-vk"); if (!root) return;
    var body = document.getElementById("bw-vk-body"), tabel = document.getElementById("bw-vk-tabel");
    var versieId = parseInt(root.getAttribute("data-versie")), locked = root.classList.contains("is-locked");
    var refPrijzen = JSON.parse(tabel.getAttribute("data-refprijzen") || "{}");
    var BRON = { 1: "voorstel", 2: "markt", 3: "code", 4: "manueel €/m²", 5: "manueel bedrag" }, RANG = { bedrag: 5, m2: 4, code: 3, markt: 2, voorstel: 1 };
    var nf0 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 });
    function num(el) { if (!el) return null; var v = String(el.value || "").replace(/\s/g, "").replace(/\.(?=\d{3}(\D|$))/g, "").replace(",", "."); var n = parseFloat(v); return isNaN(n) ? null : n; }
    function setNum(el, v, dec) { if (el) el.value = (v === null || v === undefined || isNaN(v)) ? "" : (dec ? v.toFixed(dec).replace(".", ",") : String(Math.round(v))); }
    function eur(v) { return "€ " + nf0.format(v); }
    function detail(tr) { var d = tr.nextElementSibling; return d && d.classList.contains("bw-vk-detail") ? d : null; }
    function hidden(sel, scope) { var s = scope.querySelector(sel); return s ? s.querySelector("input[type=hidden]") : null; }

    function bronVan(tr) {
        if (tr._eigen) return 5;
        var s = [tr._gSrc, tr._bSrc].filter(Boolean); if (!s.length) return null;
        return Math.max.apply(null, s.map(function (x) { return RANG[x] || 0; })) || null;
    }
    function zijde(tr, d, kant, opp) {
        var src = kant === "grond" ? tr._gSrc : tr._bSrc, m2 = d.querySelector("." + kant + "-m2"), bedrag = d.querySelector("." + kant + "waarde");
        if (src === "code" || src === "m2") { if (opp > 0 && num(m2) !== null) setNum(bedrag, num(m2) * opp, 0); }
        else { setNum(m2, (opp > 0 && num(bedrag) !== null) ? num(bedrag) / opp : null, 2); }
    }
    function herbereken(tr) {
        var d = detail(tr); if (!d) return;
        var gered = parseFloat(tr.getAttribute("data-gered")) || 0, grond = parseFloat(tr.getAttribute("data-grondopp")) || 0, info = tr.getAttribute("data-info") === "1";
        d.querySelector(".opp-info").textContent = info ? "gered. " + gered.toLocaleString("nl-BE", { maximumFractionDigits: 2 }) + " m² · grond " + grond.toLocaleString("nl-BE", { maximumFractionDigits: 2 }) + " m²" : "niet op stap 2: geen €/m²-berekening";
        var ruil = d.querySelector(".is-ruil").checked;
        d.querySelectorAll('[data-zijde="grond"] input, [data-zijde="grond"] .gl-v2-select-trigger').forEach(function (el) { if (el.tagName === "INPUT" && el.type !== "hidden") el.disabled = ruil; else if (el.classList.contains("gl-v2-select-trigger")) el.style.pointerEvents = ruil ? "none" : ""; });
        if (ruil) { setNum(d.querySelector(".grondwaarde"), 0, 0); d.querySelector(".grond-m2").value = ""; } else zijde(tr, d, "grond", grond);
        zijde(tr, d, "bouw", gered);
        var g = num(d.querySelector(".grondwaarde")), b = num(d.querySelector(".bouwwaarde")), f = num(d.querySelector(".extra-forfait")), vp = tr.querySelector(".vraagprijs");
        if (!tr._eigen) setNum(vp, (g === null && b === null && f === null) ? null : (g || 0) + (b || 0) + (f || 0), 0);
        var bron = bronVan(tr); d.querySelector(".prijs-bron").value = bron || "";
        var badge = d.querySelector(".bw-bron"); badge.className = "bw-bron bw-bron--" + (bron || 0); badge.textContent = bron ? BRON[bron] : "—";
        var min = parseFloat(tr.getAttribute("data-min")) || 0, v = num(vp), onder = min > 0 && v !== null && v > 0 && v < min;
        tr.classList.toggle("is-onder-min", !!onder);
        var w = tr.nextElementSibling && tr.nextElementSibling.nextElementSibling; if (w && w.classList.contains("bw-vk-waarsch-rij")) w.remove();
        if (onder) { var wr = document.createElement("tr"); wr.className = "bw-vk-waarsch-rij"; wr.innerHTML = '<td></td><td colspan="10" class="bw-vk-waarsch"><i class="ph ph-warning" aria-hidden="true"></i> ' + tr.getAttribute("data-naam") + ": vraagprijs " + eur(v) + " ligt " + eur(min - v) + " onder de minimale verkoopprijs.</td>"; d.insertAdjacentElement("afterend", wr); }
        totalen();
    }
    function totalen() {
        var som = 0, n = 0, onder = 0;
        body.querySelectorAll(".bw-vk-rij").forEach(function (tr) { var v = num(tr.querySelector(".vraagprijs")); if (v > 0) { som += v; n++; } if (tr.classList.contains("is-onder-min")) onder++; });
        document.getElementById("bw-vk-tot").textContent = n ? eur(som) : "—";
        document.getElementById("bw-vk-sum").textContent = n ? eur(som) + " (" + n + " lijn" + (n === 1 ? "" : "en") + ")" : "—";
        var min = parseFloat(document.getElementById("bw-vk-samenvatting").getAttribute("data-minimum")) || 0, m = document.getElementById("bw-vk-marge");
        if (n && min > 0) { var dd = som - min; m.textContent = (dd >= 0 ? "+" : "−") + eur(Math.abs(dd)) + " · " + (dd >= 0 ? "+" : "−") + Math.abs(dd / min * 100).toLocaleString("nl-BE", { maximumFractionDigits: 1 }) + " %"; m.style.color = dd >= 0 ? "var(--gl-v2-primary)" : "#8B2A2A"; } else { m.textContent = "—"; m.style.color = ""; }
        var o = document.getElementById("bw-vk-onder"); o.hidden = !onder; o.textContent = onder ? onder + " eenhe" + (onder === 1 ? "id" : "den") + " onder de minimumverkoopprijs" : "";
    }
    function initRij(tr) {
        var d = detail(tr); if (!d) return;
        var bron = parseInt(tr.getAttribute("data-bron")) || null;
        ["grond", "bouw"].forEach(function (k) {
            var code = hidden('[data-zijde="' + k + '"] .gl-v2-select', d), m2 = num(d.querySelector("." + k + "-m2")) !== null, bedrag = num(d.querySelector("." + k + "waarde")) !== null;
            var src = null;
            if (bron === 3) src = (code && code.value) ? "code" : m2 ? "m2" : bedrag ? "bedrag" : null;
            else if (bron === 4) src = m2 ? "m2" : bedrag ? "bedrag" : null;
            else if (bron === 2) src = bedrag ? "markt" : null; else if (bron === 1) src = bedrag ? "voorstel" : null; else src = bedrag ? "bedrag" : null;
            if (k === "grond") tr._gSrc = src; else tr._bSrc = src;
        });
        if (bron === 5) { var g = num(d.querySelector(".grondwaarde")) || 0, b = num(d.querySelector(".bouwwaarde")) || 0, f = num(d.querySelector(".extra-forfait")) || 0, vp = num(tr.querySelector(".vraagprijs")); tr._eigen = vp !== null && Math.abs(vp - (g + b + f)) >= 1; }
        herbereken(tr);
    }
    body.querySelectorAll(".bw-vk-rij").forEach(initRij);

    // ── Invoer ────────────────────────────────────────────────────────────────────────────────
    var saveTimer = null;
    function dirty() { if (locked) return; GlV2Budget.markDirty(); clearTimeout(saveTimer); saveTimer = setTimeout(bewaar, 1200); }
    body.addEventListener("input", function (e) {
        var d = e.target.closest(".bw-vk-detail"), tr = d ? d.previousElementSibling : e.target.closest(".bw-vk-rij"); if (!tr) return;
        var t = e.target;
        if (t.classList.contains("vraagprijs")) { tr._eigen = num(t) !== null; }
        else if (t.classList.contains("grond-m2")) { tr._gSrc = "m2"; tr._eigen = false; GlV2Select.setItems(d.querySelector('[data-zijde="grond"] .gl-v2-select'), itemsVan(d, "grond"), "", "— geen code —"); }
        else if (t.classList.contains("bouw-m2")) { tr._bSrc = "m2"; tr._eigen = false; GlV2Select.setItems(d.querySelector('[data-zijde="bouw"] .gl-v2-select'), itemsVan(d, "bouw"), "", "— geen code —"); }
        else if (t.classList.contains("grondwaarde")) { tr._gSrc = "bedrag"; tr._eigen = false; }
        else if (t.classList.contains("bouwwaarde")) { tr._bSrc = "bedrag"; tr._eigen = false; }
        else if (t.classList.contains("extra-forfait")) { tr._eigen = false; }
        herbereken(tr); dirty();
    });
    function itemsVan(d, kant) { return Array.prototype.map.call(d.querySelectorAll('[data-zijde="' + kant + '"] .gl-v2-select-option[data-value]:not([data-value=""])'), function (o) { return { value: o.getAttribute("data-value"), text: o.textContent.trim() }; }); }
    body.addEventListener("change", function (e) {
        var d = e.target.closest(".bw-vk-detail"); if (!d) return; var tr = d.previousElementSibling, t = e.target;
        if (t.classList.contains("is-ruil")) { tr._eigen = false; herbereken(tr); dirty(); return; }
        if (t.type === "hidden") {
            var z = t.closest("[data-zijde]"); if (z) {
                var kant = z.getAttribute("data-zijde"), prijs = refPrijzen[kant] ? refPrijzen[kant][t.value] : null;
                if (t.value && prijs != null) { setNum(d.querySelector("." + kant + "-m2"), prijs, 2); if (kant === "grond") tr._gSrc = "code"; else tr._bSrc = "code"; }
                else { var m2 = num(d.querySelector("." + kant + "-m2")) !== null ? "m2" : null; if (kant === "grond") tr._gSrc = m2; else tr._bSrc = m2; }
                tr._eigen = false; herbereken(tr);
            }
            dirty();
        }
    });
    body.addEventListener("click", function (e) {
        var tg = e.target.closest(".bw-vk-toggle");
        if (tg) { var tr = tg.closest("tr"), d = detail(tr), open = d.hidden; d.hidden = !open; tg.setAttribute("aria-expanded", open ? "true" : "false"); return; }
        var bv = e.target.closest(".btn-rij-voorstel"), bm = e.target.closest(".btn-rij-markt");
        if (bv || bm) { var tr2 = e.target.closest(".bw-vk-detail").previousElementSibling; pasToe(tr2, bm ? "markt" : "voorstel"); dirty(); }
    });
    function pasToe(tr, soort) {
        if (tr.getAttribute("data-info") !== "1") { GlV2Budget.toast("warning", "Geen voorstel", tr.getAttribute("data-naam") + " staat niet op het tabblad Oppervlaktes."); return false; }
        var d = detail(tr), vg = parseFloat(tr.getAttribute("data-vgrond")) || 0, vb = parseFloat(tr.getAttribute("data-vbouw")) || 0, markt = parseFloat(tr.getAttribute("data-markt")) || 0;
        var useMarkt = soort === "markt" && markt > 0, bouw = useMarkt ? Math.max(markt - vg, 0) : vb;
        ["grond", "bouw"].forEach(function (k) { GlV2Select.setItems(d.querySelector('[data-zijde="' + k + '"] .gl-v2-select'), itemsVan(d, k), "", "— geen code —"); });
        setNum(d.querySelector(".grondwaarde"), vg, 0); setNum(d.querySelector(".bouwwaarde"), bouw, 0);
        tr._gSrc = tr._bSrc = useMarkt ? "markt" : "voorstel"; tr._eigen = false; herbereken(tr);
        tr.classList.add("is-saving"); setTimeout(function () { tr.classList.remove("is-saving"); }, 900);
        return true;
    }
    var av = document.getElementById("bw-vk-alles-voorstel"), am = document.getElementById("bw-vk-alles-markt");
    if (av) av.addEventListener("click", function () { body.querySelectorAll('.bw-vk-rij[data-info="1"]').forEach(function (tr) { pasToe(tr, "voorstel"); }); dirty(); GlV2Budget.toast("success", "Voorstel overgenomen", "Grond- en bouwwaarde uit het voorstel staan in alle eenheden."); });
    if (am) am.addEventListener("click", function () { body.querySelectorAll('.bw-vk-rij[data-info="1"]').forEach(function (tr) { pasToe(tr, "markt"); }); dirty(); GlV2Budget.toast("success", "Markt overgenomen", "Marktprijs als vraagprijs; zonder marktreferentie het voorstel."); });

    // ── Opslaan ───────────────────────────────────────────────────────────────────────────────
    function verzamel() {
        var lijnen = [];
        body.querySelectorAll(".bw-vk-rij").forEach(function (tr) {
            var d = detail(tr); if (!d) return;
            var vp = num(tr.querySelector(".vraagprijs")), g = num(d.querySelector(".grondwaarde")), b = num(d.querySelector(".bouwwaarde")), f = num(d.querySelector(".extra-forfait"));
            var unit = hidden(".bw-vk-extra .gl-v2-select", d), cg = hidden('[data-zijde="grond"] .gl-v2-select', d), cb = hidden('[data-zijde="bouw"] .gl-v2-select', d);
            if (vp === null && g === null && b === null && f === null && !(unit && unit.value) && tr.getAttribute("data-info") === "1") return;   // onaangeroerde eenheid: geen lijn
            lijnen.push({ eenheidNaam: tr.getAttribute("data-naam"), unitId: unit ? parseInt(unit.value) || null : null, codeGrond: cg ? parseInt(cg.value) || null : null, codeBouw: cb ? parseInt(cb.value) || null : null,
                grondPrijsPerM2: num(d.querySelector(".grond-m2")), bouwPrijsPerM2: num(d.querySelector(".bouw-m2")), grondwaarde: g, bouwwaarde: b, isRuil: d.querySelector(".is-ruil").checked, extraForfait: f, vraagprijs: vp, prijsBron: parseInt(d.querySelector(".prijs-bron").value) || null });
        });
        return lijnen;
    }
    async function bewaar() {
        clearTimeout(saveTimer); GlV2Budget.setStatus("saving");
        try { var r = await GlV2Budget.post(root.getAttribute("data-url-save"), { budgetVersieId: versieId, lijnen: verzamel() }, true); if (!r.success) { GlV2Budget.setStatus("error", r.message || "Opslaan mislukt"); return false; } GlV2Budget.saved(); return true; }
        catch (e) { GlV2Budget.setStatus("error", "Opslaan mislukt: " + ((e && e.message) || e)); return false; }
    }
    GlV2Budget.register({ save: bewaar });   // geen dirty-callback: die riep GlV2Budget.isDirty() aan, dat dezelfde callback weer aanriep (oneindige lus → de knop deed niets)
    var dz = document.getElementById("bw-vk-doorzetten");
    if (dz) dz.addEventListener("click", async function () {
        var gekoppeld = Array.prototype.filter.call(body.querySelectorAll(".bw-vk-detail"), function (d) { var u = hidden(".bw-vk-extra .gl-v2-select", d); return u && parseInt(u.value) > 0; }).length;
        if (!gekoppeld) { GlV2Budget.toast("warning", "Geen unit gekoppeld", "Open de details van een eenheid en kies de Unit van het project."); return; }
        if (!await GlV2Budget.bevestig({ title: "Doorzetten naar de Units?", desc: "De lijnen worden opgeslagen en grond- en bouwwaarde van " + gekoppeld + " gekoppelde eenhe" + (gekoppeld === 1 ? "id" : "den") + " worden naar de Units geschreven. Verkochte units blijven ongemoeid.", ok: "Doorzetten", tone: "warning", icon: "ph-arrow-line-right" })) return;
        if (!await bewaar()) return;
        var r = await GlV2Budget.post(root.getAttribute("data-url-doorzetten"), { versieId: versieId }, false);
        (r.messages || []).forEach(function (m) { GlV2Budget.toast(m.type === "Error" ? "danger" : m.type === "Warning" ? "warning" : m.type === "Info" ? "info" : "success", "Doorzetten", m.text); });
    });
    totalen();
})();
