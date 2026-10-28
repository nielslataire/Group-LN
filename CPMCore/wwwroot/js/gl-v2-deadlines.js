// gl-v2 — Deadlines (design-handoff punt 1b): filteren (openstaand, achterstallig, alles), enkel mijn rollen, zoeken, projecten in- en uitklappen,
// bereikte mijlpalen tonen en een taak maken van een mijlpaal (naar Mijn taken).
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-dl");
    if (!root) return;
    var $ = function (id) { return document.getElementById(id); };
    var $$ = function (sel, r) { return Array.prototype.slice.call((r || root).querySelectorAll(sel)); };
    var tokenForm = $("dl-token");
    var token = tokenForm.querySelector("input[name=__RequestVerificationToken]").value, base = tokenForm.dataset.base;
    var toast = function (tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); };

    var state = { f: "open", mine: false, zoek: "", toonKlaar: {} };

    function tegen() {
        var zichtbaarTotaal = 0;
        $$("[data-project]").forEach(function (p, idx) {
            var aantal = 0, klaarTotaal = 0, klaarZichtbaar = 0;
            $$("[data-row]", p).forEach(function (r) {
                var klaar = r.dataset.klaar === "1", laat = r.dataset.laat === "1";
                if (klaar) klaarTotaal++;
                var toon = state.f === "late" ? laat : (state.f === "all" || state.toonKlaar[idx] ? true : !klaar);
                if (toon && state.mine && r.dataset.mijn !== "1") toon = false;
                if (toon && state.zoek && r.dataset.naam.indexOf(state.zoek) < 0) toon = false;
                r.hidden = !toon;
                if (toon) { aantal++; if (klaar) klaarZichtbaar++; }
            });
            // "Toon N bereikte mijlpalen" enkel in de weergave Openstaand.
            var kn = p.querySelector("[data-dl-done]");
            if (kn) {
                kn.hidden = state.f !== "open" || klaarTotaal === 0;
                kn.textContent = state.toonKlaar[idx] ? "Bereikte mijlpalen verbergen" : "Toon " + kn.dataset.aantal + " bereikte mijlpalen";
            }
            var filtert = state.mine || state.zoek || state.f === "late";
            p.hidden = filtert && aantal === 0;
            if (!p.hidden) zichtbaarTotaal++;
        });
        $("dl-leeg").hidden = zichtbaarTotaal > 0 || $$("[data-project]").length === 0;
    }

    $$("#dl-filter > button").forEach(function (b) {
        b.addEventListener("click", function () {
            state.f = b.dataset.f;
            $$("#dl-filter > button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
            tegen();
        });
    });
    var mine = $("dl-mine");
    function zetMine(aan) { state.mine = aan; mine.setAttribute("aria-pressed", aan ? "true" : "false"); tegen(); }
    mine.addEventListener("click", function () { zetMine(!state.mine); });
    $("dl-alle-rollen").addEventListener("click", function () { zetMine(false); });
    $("dl-zoek").addEventListener("input", function () { state.zoek = this.value.trim().toLowerCase(); tegen(); });

    $$("[data-dl-toggle]").forEach(function (b) {
        b.addEventListener("click", function () {
            var open = b.getAttribute("aria-expanded") !== "true";
            b.setAttribute("aria-expanded", open ? "true" : "false");
            var p = b.closest("[data-project]");
            p.querySelector("[data-dl-rows]").hidden = !open;
            var kn = p.querySelector("[data-dl-done]"); if (kn) kn.style.display = open ? "" : "none";
        });
    });
    $$("[data-dl-done]").forEach(function (b) {
        b.addEventListener("click", function () {
            var idx = $$("[data-project]").indexOf(b.closest("[data-project]"));
            state.toonKlaar[idx] = !state.toonKlaar[idx]; tegen();
        });
    });

    root.addEventListener("click", function (e) {
        var b = e.target.closest("[data-dl-taak]"); if (!b) return;
        b.disabled = true;
        var fd = new FormData(); fd.append("mijlpaalId", b.dataset.dlTaak);
        fetch(base + "TaakVanMijlpaal", { method: "POST", headers: { "RequestVerificationToken": token, "X-Requested-With": "XMLHttpRequest" }, body: fd })
            .then(function (r) { return r.json(); })
            .then(function (j) {
                if (!j.ok) { b.disabled = false; toast("danger", "Taak maken lukte niet", j.bericht || "Probeer opnieuw."); return; }
                var span = document.createElement("span"); span.className = "gl-v2-dl-made";
                span.innerHTML = '<i class="ph ph-check-circle" aria-hidden="true"></i>In Mijn taken';
                b.replaceWith(span); toast("success", "Taak gemaakt", "Je vindt ze in Mijn taken.");
            }, function () { b.disabled = false; toast("danger", "Geen verbinding", "Probeer opnieuw."); });
    });

    tegen();
})();
