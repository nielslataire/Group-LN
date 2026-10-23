// gl-v2 — Eenheid berekenen (design-handoff 45c). Alles rekent live in de browser en er wordt niets bewaard. Dezelfde regels als VerkoopService.Bereken
// (grond + registratierechten + bouw + btw + kosten koper incl. btw + notaris, tarief 2026) en VerkoopResultaat (verkoopkosten makelaar, btw enkel op het grondaandeel).
// Aanpasbaar: grondwaarde, bouwwaarde, registratierechten en btw; de rest is afgeleid (uitgeschakeld).
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-vb-root");
    if (!root) return;
    var cfg = JSON.parse(root.dataset.config);
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var nf0 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 });
    var nf1 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 1 });
    var nf2 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function eur(v) { return (v < 0 ? "– " : "") + "€ " + nf0.format(Math.round(Math.abs(v))); }
    function num(s) {
        s = String(s || "").replace(/[€\s ]/g, "");
        if (s.indexOf(",") >= 0) s = s.replace(/\./g, "").replace(",", ".");
        else if ((s.match(/\./g) || []).length > 1) s = s.replace(/\./g, "");
        var n = parseFloat(s); return isFinite(n) ? n : 0;
    }
    function el(id) { return document.getElementById(id); }
    function val(id) { return num(el(id).value); }
    function text(id, t) { var e = el(id); if (e) e.textContent = t; }
    function round2(v) { return Math.round(v * 100) / 100; }

    // ── notaris (Tarief 2026, art. 17 nr. 81 2° en art. 2 §2) ──
    var J = [[7500, 2.5], [10000, 2.5], [12500, 1.75], [15495, 1.71], [18595, 1.14], [186005, 0.57], [249905, 0.20], [null, 0.20]];
    var JBIS = [[7500, 2.5], [10000, 2.5], [12500, 2.0], [15495, 1.5], [18595, 0.5], [186005, 0.485], [249905, 0.3], [null, 0.2]];
    function notaris(grond, bouw, jbis, overschrijving) {
        var basis = Math.round(grond + bouw), r = { basis: basis, fee: 0, vac: 855, uit: 309, ovs: overschrijving, rog: 100, rob: 100, btw: 0, totaal: 0, jbis: jbis };
        if (basis <= 0) { r.vac = r.uit = r.rog = r.rob = 0; r.totaal = overschrijving; return r; }
        var rest = basis, fee = jbis ? 257 : 285;
        (jbis ? JBIS : J).forEach(function (s) { if (rest <= 0) return; var d = s[0] == null ? rest : Math.min(rest, s[0]); fee += d * s[1] / 100; rest -= d; });
        r.fee = round2(fee);
        r.btw = round2((r.fee + r.vac + r.uit + r.rog) * 0.21);
        r.totaal = r.fee + r.vac + r.uit + r.ovs + r.rog + r.rob + r.btw;
        return r;
    }

    function row(key, label, note, amount) { text("vb-pv-" + key + "-l", label); text("vb-pv-" + key + "-n", note || ""); text("vb-pv-" + key, amount); }

    function render() {
        var grond = val("vb-grond-bedrag"), bouw = val("vb-bouw-bedrag"), regPct = val("vb-reg"), vatPct = val("vb-vat"), rt = cfg.rt;
        // afgeleide velden: prijs per m²
        el("vb-grond-pm2").value = nf2.format(cfg.perceel > 0 ? grond / cfg.perceel : 0);
        el("vb-bouw-pm2").value = nf2.format(cfg.gereduceerd > 0 ? bouw / cfg.gereduceerd : 0);

        var reg = 0, vat = 0;
        if (rt === 2) { reg = grond * regPct / 100; vat = bouw * vatPct / 100; } else if (rt === 1) { reg = (grond + bouw) * regPct / 100; } else { vat = (grond + bouw) * vatPct / 100; }
        reg = round2(reg); vat = round2(vat);
        var kosten = 0, kostenBtw = 0, posten = 0;
        $$("#vb-kosten [data-btw]").forEach(function (c) {
            var a = num(c.querySelector("input").value), pct = c.dataset.btw === "vat" ? vatPct : 21;
            if (a !== 0) posten++;
            kosten += a; kostenBtw += round2(a * pct / 100);
        });
        el("vb-k-btw").value = nf2.format(kostenBtw);
        var not = notaris(grond, bouw, el("vb-enige").checked, cfg.overschrijving);
        ["fee", "vac", "uit", "ovs", "rog", "rob", "btw"].forEach(function (k) { el("vb-n-" + k).value = nf2.format(not[k]); }); el("vb-n-tot").value = nf2.format(not.totaal);
        text("vb-n-schaal", "schaal " + (not.jbis ? "Jbis" : "J") + " · tarief 2026");
        text("vb-n-grondslag", not.basis > 0 ? "op een grondslag van " + eur(not.basis) : "");
        var totaal = grond + reg + bouw + vat + kosten + kostenBtw + not.totaal;

        row("grond", "Grond", cfg.perceel > 0 ? nf0.format(cfg.perceel) + " × " + nf2.format(val("vb-grond-pm2")) : "", eur(grond));
        row("reg", "Registratierechten", nf0.format(regPct) + " %", eur(reg));
        row("bouw", "Bouw", cfg.gereduceerd > 0 ? nf1.format(cfg.gereduceerd) + " × " + nf2.format(val("vb-bouw-pm2")) : "", eur(bouw));
        row("vat", "Btw", nf0.format(vatPct) + " %", eur(vat));
        row("kosten", "Kosten koper", posten + (posten === 1 ? " post" : " posten") + " incl. btw", eur(kosten + kostenBtw));
        row("not", "Notaris", "schaal " + (not.jbis ? "Jbis" : "J"), eur(not.totaal));
        el("vb-pv-reg").parentElement.hidden = rt === 0; el("vb-pv-vat").parentElement.hidden = rt === 1;
        text("vb-pv-totaal", eur(totaal));
        var vraag = grond + bouw; text("vb-pv-vraag", eur(vraag));

        var bud = cfg.budget;
        if (bud && el("vb-b-vraag")) {
            text("vb-b-kost", eur(bud.Kostprijs)); text("vb-b-min", eur(bud.Minimum)); text("vb-b-min-n", "marge " + nf1.format(bud.MargePerc * 100) + " %");
            text("vb-b-vraag", eur(vraag));
            var diff = vraag - bud.Minimum, drow = el("vb-b-diff-row");
            text("vb-b-diff-l", diff >= 0 ? "Boven minimum" : "Onder minimum"); text("vb-b-diff", (diff >= 0 ? "+ " : "") + eur(diff));
            drow.classList.toggle("is-good", diff >= 0); drow.classList.toggle("is-bad", diff < 0);
            var com = bud.Commissie, mak = round2(com * grond) + round2(round2(com * grond) * 0.21) + round2(com * bouw);
            text("vb-b-mak-n", nf1.format(com * 100) + " %"); text("vb-b-mak", "– " + eur(mak));
            var net = vraag - mak - bud.Kostprijs;
            text("vb-b-net-l", "Netto marge" + (bud.Kostprijs > 0 ? " · " + nf1.format(net / bud.Kostprijs * 100) + " %" : "")); text("vb-b-net", eur(net));
            var v = el("vb-b-verdict"); v.textContent = diff >= 0 ? "Vraagprijs ligt boven het minimum uit het budget." : "Vraagprijs ligt onder het minimum uit het budget."; v.classList.toggle("is-warn", diff < 0);
        }
        var ch = [];
        if (Math.abs(grond - cfg.grond) > 0.5) ch.push("grond gewijzigd · " + eur(cfg.grond) + " → " + eur(grond));
        if (Math.abs(bouw - cfg.bouw) > 0.5) ch.push("bouw gewijzigd · " + eur(cfg.bouw) + " → " + eur(bouw));
        text("vb-changes", ch.length ? ch.join(" · ") + " · niets bewaard" : "");
    }
    root.addEventListener("input", function (e) { if (e.target.classList.contains("vb-num") && !e.target.disabled) render(); });
    root.addEventListener("focusout", function (e) { if (e.target.classList && e.target.classList.contains("vb-num") && !e.target.disabled && e.target.value.trim() !== "") { e.target.value = nf2.format(num(e.target.value)); render(); } });
    el("vb-enige").addEventListener("change", render);
    el("vb-reset").addEventListener("click", function () { location.reload(); });
    render();
})();
