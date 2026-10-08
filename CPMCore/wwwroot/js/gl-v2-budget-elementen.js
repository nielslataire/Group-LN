/* gl-v2 — Budgetflow stap 4 (Gevels & ramen) en 5 (Dak & afbraak): gedeeld gedrag van de elementtabellen (V2/_BwElementTabel).
   Resultaat per rij client-side (aantal × breedte × hoogte | aantal × breedte × lengte | aantal × lengte), subtotalen per tabel,
   autosave per rij (BudgetGevelElementOpslaan), toevoegen/dupliceren/verwijderen (BudgetGevelElementToevoegen?v2=true / …Verwijderen).
   De pagina geeft de URL's en een callback voor de KPI-tegels mee: GlV2BudgetElementen.init({ versieId, urls, onTotaal }). */
(function () {
    "use strict";
    var nf2 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var nf3 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 3, maximumFractionDigits: 3 });
    function num(v) { v = String(v == null ? "" : v).replace(/\s/g, "").replace(/\.(?=\d{3}(\D|$))/g, "").replace(",", "."); var n = parseFloat(v); return isNaN(n) ? 0 : n; }

    function init(cfg) {
        var locked = document.querySelector(".gl-v2-bw.is-locked") != null;
        var timers = {};

        function data(tr) {
            var d = { elementId: parseInt(tr.getAttribute("data-element-id")), versieId: cfg.versieId, elementType: tr.getAttribute("data-element-type") };
            tr.querySelectorAll(".el-input").forEach(function (inp) {
                var f = inp.getAttribute("data-field");
                d[f] = (f === "eenheidNaam" || f === "beschrijving") ? inp.value : num(inp.value);
            });
            if (!("aantal" in d)) d.aantal = 1;
            return d;
        }
        function resultaat(tr, d) {
            var h = tr.getAttribute("data-hoogte") === "1", l = tr.getAttribute("data-lengte-alleen") === "1";
            if (h) return { v: d.aantal * (d.breedte || 0) * (d.hoogte || 0), f: nf2.format(d.aantal) + " × " + nf3.format(d.breedte || 0) + " × " + nf3.format(d.hoogte || 0), e: "m²" };
            if (l) return { v: d.aantal * (d.lengte || 0), f: nf2.format(d.aantal) + " × " + nf3.format(d.lengte || 0), e: "lm" };
            return { v: d.aantal * (d.breedte || 0) * (d.lengte || 0), f: nf2.format(d.aantal) + " × " + nf3.format(d.breedte || 0) + " × " + nf3.format(d.lengte || 0), e: "m²" };
        }
        function herbereken(tr) {
            var d = data(tr), r = resultaat(tr, d), cell = tr.querySelector(".el-res");
            cell.querySelector(".el-res-value").textContent = nf2.format(r.v);
            cell.classList.toggle("is-neg", r.v < 0);
            var txt = r.f + " = " + nf2.format(r.v) + " " + r.e;
            cell.title = txt; cell.querySelector(".el-formule").textContent = txt;
            return d;
        }
        function subtotaal(sectie) {
            var s = 0; sectie.querySelectorAll(".el-body tr").forEach(function (tr) { s += resultaat(tr, data(tr)).v; });
            sectie.querySelectorAll(".el-subtotaal").forEach(function (el) { el.textContent = nf2.format(s) + " " + el.getAttribute("data-eenh"); });
            var leeg = sectie.querySelector(".el-leeg"); if (leeg) leeg.hidden = sectie.querySelectorAll(".el-body tr").length > 0;
            if (cfg.onSubtotaal) cfg.onSubtotaal(sectie.getAttribute("data-type"), s);
            return s;
        }
        async function bewaar(tr) {
            var d = data(tr);
            tr.classList.add("is-saving"); GlV2Budget.setStatus("saving");
            try {
                var r = await GlV2Budget.post(cfg.urls.save, d, true);
                tr.classList.remove("is-saving");
                if (!r.success) { tr.classList.add("is-error"); GlV2Budget.setStatus("error", r.error || "Opslaan mislukt"); return; }
                tr.classList.remove("is-error"); GlV2Budget.saved(); if (cfg.onTotaal && r.totaal) cfg.onTotaal(r.totaal);
            } catch (e) { tr.classList.remove("is-saving"); tr.classList.add("is-error"); GlV2Budget.setStatus("error"); }
        }
        function plan(tr) {
            if (locked) return;
            herbereken(tr); subtotaal(tr.closest(".bw-el-sectie")); GlV2Budget.markDirty();
            var id = tr.getAttribute("data-element-id"); clearTimeout(timers[id]); timers[id] = setTimeout(function () { bewaar(tr); }, 500);
        }
        GlV2Budget.register({ save: async function () { var rows = document.querySelectorAll(".bw-el-rij.is-error, .bw-el-rij.is-saving"); for (var i = 0; i < rows.length; i++) await bewaar(rows[i]); return true; }, dirty: function () { return false; } });

        async function voegToe(sectie, naam, besch, vulUit) {
            var type = sectie.getAttribute("data-type");
            var fd = new URLSearchParams({ versieId: cfg.versieId, elementType: type, eenheidNaam: naam || "", beschrijving: besch || "", v2: "true" });
            var r = await fetch(cfg.urls.add, { method: "POST", credentials: "same-origin", headers: { "RequestVerificationToken": GlV2Budget.token(), "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" }, body: fd.toString() });
            if (!r.ok) { GlV2Budget.toast("danger", "Toevoegen mislukt", "De rij kon niet aangemaakt worden."); return null; }
            var tmp = document.createElement("tbody"); tmp.innerHTML = await r.text();
            var tr = tmp.querySelector("tr"); if (!tr) return null;
            sectie.querySelector(".el-body").appendChild(tr);
            if (vulUit) { tr.querySelectorAll(".el-input").forEach(function (inp) { var f = inp.getAttribute("data-field"); if (f in vulUit && f !== "eenheidNaam" && f !== "beschrijving") inp.value = f === "aantal" ? nf2.format(vulUit[f]) : nf3.format(vulUit[f]); }); plan(tr); }
            else { herbereken(tr); subtotaal(sectie); tr.querySelector('[data-field="aantal"]').focus(); }
            return tr;
        }

        document.addEventListener("input", function (e) { var tr = e.target.closest(".bw-el-rij"); if (tr && e.target.classList.contains("el-input")) plan(tr); });
        document.addEventListener("click", async function (e) {
            var add = e.target.closest(".el-add");
            if (add) {
                var sectie = add.closest(".bw-el-sectie"), n = sectie.querySelector(".el-add-naam"), b = sectie.querySelector(".el-add-besch");
                await voegToe(sectie, n.value.trim(), b.value.trim(), null); n.value = ""; b.value = ""; return;
            }
            var tr = e.target.closest(".bw-el-rij"); if (!tr) return;
            if (e.target.closest(".btn-del-el")) {
                if (!await GlV2Budget.bevestig({ title: "Rij verwijderen?", desc: "Deze rij wordt uit de tabel verwijderd en het totaal wordt herberekend.", ok: "Verwijderen" })) return;
                var r = await GlV2Budget.post(cfg.urls.del, { elementId: tr.getAttribute("data-element-id"), versieId: cfg.versieId }, false);
                if (r.success) { var s = tr.closest(".bw-el-sectie"); tr.remove(); subtotaal(s); GlV2Budget.saved(); if (cfg.onTotaal && r.totaal) cfg.onTotaal(r.totaal); }
            } else if (e.target.closest(".btn-dup-el")) {
                var d = data(tr); await voegToe(tr.closest(".bw-el-sectie"), d.eenheidNaam, d.beschrijving, d);
            }
        });
        document.querySelectorAll(".bw-el-sectie").forEach(function (s) { s.querySelectorAll(".el-body tr").forEach(herbereken); subtotaal(s); });
    }
    window.GlV2BudgetElementen = { init: init, num: num };
})();
