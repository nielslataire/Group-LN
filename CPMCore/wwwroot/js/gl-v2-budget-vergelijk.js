/* gl-v2 — Budgetten vergelijken (design-handoff 39m/39n): de modal "Budgetten vergelijken" (overzicht + "Versie toevoegen") en de bediening van de
   vergelijkingspagina (groepen uit-/inklappen, "Enkel verschillen"). De modal staat in Views/Projecten/Budget/V2/_BwVergelijkModal.cshtml.
   Regels: 2 of 3 versies, de volgorde van aanvinken telt, de basis is standaard het definitieve budget, anders de eerste gekozen versie. */
(function () {
    "use strict";

    // ── Modal ─────────────────────────────────────────────────────────────────────────────────────
    var el = document.getElementById("bw-vg-modal");
    if (el) {
        var rijen = Array.prototype.slice.call(el.querySelectorAll(".bw-vg-rij"));
        var teller = document.getElementById("bw-vg-teller"), go = document.getElementById("bw-vg-go"), basisSel = document.getElementById("bw-vg-basis"), modusSel = document.getElementById("bw-vg-modus");
        var volgorde = [];   // ids in de volgorde van aanvinken
        var wensBasis = el.getAttribute("data-basis") || "";

        function naamVan(id) { var r = rijen.filter(function (x) { return x.getAttribute("data-id") === String(id); })[0]; return r ? r.getAttribute("data-naam") + (r.getAttribute("data-def") === "1" ? " · Definitief" : "") : ""; }

        function synchroniseer() {
            // volgorde bijhouden: nieuw aangevinkte achteraan, uitgevinkte eruit
            var aan = rijen.filter(function (r) { return r.querySelector(".bw-vg-cb").checked; }).map(function (r) { return r.getAttribute("data-id"); });
            volgorde = volgorde.filter(function (id) { return aan.indexOf(id) >= 0; });
            aan.forEach(function (id) { if (volgorde.indexOf(id) < 0) volgorde.push(id); });
            var vol = volgorde.length >= 3;
            rijen.forEach(function (r) {
                var cb = r.querySelector(".bw-vg-cb"), geblokkeerd = vol && !cb.checked;
                cb.disabled = geblokkeerd; r.classList.toggle("is-aan", cb.checked); r.classList.toggle("is-uit", geblokkeerd);
            });
            teller.textContent = volgorde.length + " gekozen";
            go.disabled = volgorde.length < 2;

            // basis: standaard het definitieve budget als dat gekozen is, anders de eerste gekozen versie; een eerdere keuze blijft staan
            var huidig = basisSel.value || wensBasis;
            var def = el.getAttribute("data-definitief") || "";
            var kies = volgorde.indexOf(huidig) >= 0 ? huidig : (def && volgorde.indexOf(def) >= 0 ? def : (volgorde[0] || ""));
            var wrap = document.getElementById("bw-vg-basis_select");
            basisSel.value = kies;
            if (window.GlV2Select && wrap) window.GlV2Select.setItems(wrap, volgorde.map(function (id) { return { value: id, text: naamVan(id) }; }), kies, null);
            wrap.querySelector(".gl-v2-select-trigger").setAttribute("aria-disabled", volgorde.length === 0 ? "true" : "false");
        }

        rijen.forEach(function (r) { r.querySelector(".bw-vg-cb").addEventListener("change", function () { wensBasis = ""; synchroniseer(); }); });
        basisSel.addEventListener("change", function () { wensBasis = basisSel.value; });

        go.addEventListener("click", function () {
            if (volgorde.length < 2) return;
            var basis = basisSel.value || volgorde[0];
            var ids = [basis].concat(volgorde.filter(function (id) { return id !== basis; }));
            var url = el.getAttribute("data-url") + "?projectId=" + encodeURIComponent(el.getAttribute("data-project")) + "&versies=" + ids.join(",") + "&basis=" + encodeURIComponent(basis) + "&modus=" + encodeURIComponent(modusSel.value);
            go.classList.add("is-loading"); window.location.href = url;
        });

        // openen: met een voorselectie (id's), anders wat er in data-gekozen staat; het definitieve budget staat standaard aangevinkt
        function openModal(voor) {
            var ids = (voor && voor.length ? voor : (el.getAttribute("data-gekozen") || "").split(",").filter(Boolean)).map(String);
            rijen.forEach(function (r) { r.querySelector(".bw-vg-cb").checked = ids.indexOf(r.getAttribute("data-id")) >= 0; });
            volgorde = ids.filter(function (id) { return naamVan(id) !== ""; });
            basisSel.value = ""; synchroniseer();
            bootstrap.Modal.getOrCreateInstance(el).show();
        }
        window.GlV2BudgetVergelijk = { open: openModal };

        document.addEventListener("click", function (e) {
            var knop = e.target.closest("[data-bw-vergelijk]"); if (!knop) return;
            e.preventDefault();
            var def = el.getAttribute("data-definitief"), extra = (knop.getAttribute("data-bw-vergelijk") || "").split(",").filter(Boolean);
            var voor = knop.hasAttribute("data-bw-vergelijk-huidig") ? (el.getAttribute("data-gekozen") || "").split(",").filter(Boolean) : extra;
            if (!voor.length && def) voor = [def];
            wensBasis = knop.getAttribute("data-bw-basis") || "";
            openModal(voor);
        });

        var auto = el.getAttribute("data-auto");
        if (auto) openModal(auto.split(",").filter(Boolean));
    }

    // ── Vergelijkingspagina ───────────────────────────────────────────────────────────────────────
    var pagina = document.getElementById("bw-vg-pagina");
    if (pagina) {
        var enkel = document.getElementById("bw-vg-enkel");
        function filter() {
            var alleen = enkel && enkel.checked;
            pagina.classList.toggle("is-enkel", !!alleen);
            pagina.querySelectorAll(".bw-vg-groep").forEach(function (g) {
                var heeft = g.getAttribute("data-diff") === "1";
                g.hidden = !!alleen && !heeft;
            });
            pagina.querySelectorAll(".bw-vg-lijn").forEach(function (l) {
                var open = l.closest(".bw-vg-groep").classList.contains("is-open");
                l.hidden = !open || (!!alleen && l.getAttribute("data-diff") !== "1");
            });
        }
        if (enkel) enkel.addEventListener("change", filter);
        pagina.querySelectorAll(".bw-vg-groepkop").forEach(function (k) {
            k.addEventListener("click", function () {
                var g = k.closest(".bw-vg-groep"), open = !g.classList.contains("is-open");
                g.classList.toggle("is-open", open); k.setAttribute("aria-expanded", open ? "true" : "false"); filter();
            });
        });
        filter();
    }
})();
