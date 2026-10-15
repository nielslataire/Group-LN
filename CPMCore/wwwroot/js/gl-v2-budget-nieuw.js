/* gl-v2 — Nieuw budget (design-handoff 39l): startpunt leeg of als kopie van een bestaand budget; bij een kopie kies je budget en versie en vink je de stappen aan die
   overgenomen worden (de rest begint leeg). EENHEDEN en INDEX volgen die keuzes: zonder oppervlaktes uit de kopie komen de eenheden uit het project; de index is
   standaard de nieuwste S- en I-index. "Meteen actief budget maken" laat het definitieve budget ongemoeid. Markup: BudgetIndexV2.cshtml (#bw-ix-modal). */
(function () {
    "use strict";
    var modal = document.getElementById("bw-ix-modal");
    if (!modal) return;

    var bronnen = []; try { bronnen = JSON.parse(modal.getAttribute("data-bronnen") || "[]"); } catch (e) { bronnen = []; }
    var aantal = parseInt(modal.getAttribute("data-aantal"), 10) || 0, indexTekst = modal.getAttribute("data-index") || "Nieuwste S- en I-index";
    var radios = Array.prototype.slice.call(modal.querySelectorAll('input[name="startpunt"]'));
    var kopieBlok = modal.querySelector(".bw-start-kopie");
    var chips = Array.prototype.slice.call(modal.querySelectorAll(".bw-overnemen input"));

    function hid(id) { return document.getElementById(id); }
    function wrap(id) { var h = hid(id); return h ? h.closest(".gl-v2-select") : null; }
    function items(id, lijst, waarde, leeg) { var w = wrap(id); if (w && window.GlV2Select) window.GlV2Select.setItems(w, lijst, waarde || "", leeg || null); }
    function aangevinkt(k) { return chips.some(function (c) { return c.value === k && c.checked && !c.disabled; }); }

    function vulVersies() {
        var m = hid("bw-nb-budget") ? hid("bw-nb-budget").value : "";
        var bron = bronnen.filter(function (b) { return String(b.id) === String(m); })[0];
        var lijst = bron ? bron.versies : [];
        items("bw-nb-versie", lijst, lijst.length ? lijst[0].value : "", "Kies een versie…");
    }

    function bijwerken() {
        var kopie = radios.some(function (r) { return r.checked && r.value === "kopie"; });
        modal.querySelectorAll(".bw-start").forEach(function (k) { var r = k.querySelector("input"); k.classList.toggle("is-aan", r.checked); });
        if (kopieBlok) kopieBlok.hidden = !kopie;
        // zonder kopie wordt er niets meegestuurd over de bron
        var versie = hid("bw-nb-versie"), budget = hid("bw-nb-budget");
        if (versie) versie.disabled = !kopie;
        if (budget) budget.disabled = !kopie;
        chips.forEach(function (c) { c.disabled = !kopie; });

        var oppTick = kopie && aangevinkt("oppervlaktes"), gegTick = kopie && aangevinkt("gegevens");

        // EENHEDEN
        var eh = hid("bw-nb-eenheden"), ehNu = eh ? eh.value : "";
        if (oppTick) items("bw-nb-eenheden", [{ value: "kopie", text: "Eenheden uit de gekozen versie" }], "kopie");
        else items("bw-nb-eenheden", [{ value: "project", text: aantal + (aantal === 1 ? " eenheid" : " eenheden") + " uit het project" }, { value: "leeg", text: "Geen eenheden, zelf toevoegen in stap 2" }], ehNu === "leeg" ? "leeg" : "project");
        var ehHelp = modal.querySelector("#bw-nb-eenheden-help"); if (ehHelp) ehHelp.textContent = oppTick ? "de oppervlaktes komen mee uit de kopie" : "aanpasbaar in stap 2";

        // INDEX
        var ix = hid("bw-nb-index"), ixNu = ix ? ix.value : "";
        var lijst = [{ value: "actueel", text: indexTekst }]; if (gegTick) lijst.push({ value: "bron", text: "Index van de gekozen versie" });
        items("bw-nb-index", lijst, ixNu === "bron" && gegTick ? "bron" : "actueel");
    }

    radios.forEach(function (r) { r.addEventListener("change", bijwerken); });
    chips.forEach(function (c) { c.addEventListener("change", bijwerken); });
    var budget = hid("bw-nb-budget"); if (budget) budget.addEventListener("change", function () { vulVersies(); bijwerken(); });

    // Eerst het eigen project, daarna de rest: het eerste budget is vooraf gekozen
    function init() {
        var eerste = bronnen[0];
        if (eerste && budget && !budget.value) items("bw-nb-budget", bronnen.map(function (b) { return { value: String(b.id), text: b.naam, group: b.groep }; }), String(eerste.id), "Kies een budget…");
        vulVersies(); bijwerken();
    }
    modal.addEventListener("show.bs.modal", function () { radios.forEach(function (r) { r.checked = r.value === "leeg"; }); init(); });
    modal.addEventListener("shown.bs.modal", function () { var n = document.getElementById("bw-ix-naam"); if (n) n.focus(); });

    // Eén keer verzenden: knop in laadstaat (zoals de andere modals)
    var form = modal.querySelector("form");
    if (form) form.addEventListener("submit", function () { var b = form.querySelector('button[type="submit"]'); if (b) b.classList.add("is-loading"); });
    init();
})();
