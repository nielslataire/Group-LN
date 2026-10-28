// gl-v2 — Instellingen › Puntmeldingen (punt 56a/56b). Overzicht: filter op uitvoeringen, "Nu versturen" en de testmails (bestaande JSON-acties). Schema bewerken: dag met één tik,
// samenvatting in gewone taal, de eerstvolgende herinneringen en de prioriteitsladder.
(function () {
    "use strict";
    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js
    var DAGEN = ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];
    var MAANDEN = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

    // ── Overzicht ──
    var root = $("gl-v2-pm");
    if (root) {
        var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
        function post(url) {
            var fd = new FormData(); fd.append("__RequestVerificationToken", token);
            return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
        }
        function meld(r, ok) { toast(r.success ? "success" : "danger", r.success ? ok : "Niet gelukt", r.message || ""); }
        [["pm-test", "Testmail verstuurd"], ["pm-test-inv", "Test uitnodigingsmail verstuurd"]].forEach(function (p) {
            var b = $(p[0]); if (!b) return;
            b.addEventListener("click", function () {
                b.disabled = true;
                post(b.dataset.url).then(function (r) { b.disabled = false; meld(r, p[1]); }).catch(function () { b.disabled = false; toast("danger", "Niet gelukt", "De server gaf geen antwoord."); });
            });
        });
        root.querySelectorAll("[data-pm-send]").forEach(function (b) {
            b.addEventListener("click", function () {
                window.GlV2Bevestig({ title: "Nu versturen?", desc: "De batch voor " + b.dataset.pmNaam + " wordt meteen verstuurd, los van het schema.", ok: "Versturen", tone: "warning" }).then(function (ok) {
                    if (!ok) return;
                    b.disabled = true;
                    post(b.dataset.pmSend).then(function (r) { b.disabled = false; meld(r, "Batch verstuurd"); }).catch(function () { b.disabled = false; toast("danger", "Niet gelukt", "De server gaf geen antwoord."); });
                });
            });
        });
        var filter = $("pm-filter");
        if (filter) filter.addEventListener("click", function (e) {
            var b = e.target.closest("[data-f]"); if (!b) return;
            this.querySelectorAll("button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
            root.querySelectorAll("[data-run]").forEach(function (r) { r.toggleAttribute("data-out", !!b.dataset.f && r.dataset.st !== b.dataset.f); });
            root.querySelectorAll("[data-gl-pager]").forEach(function (box) { box.dispatchEvent(new Event("glpager:refresh")); });
        });
    }

    // ── Schema bewerken ──
    var form = $("pm-form");
    if (!form) return;
    var dag = $("pm-dag"), freq = $("pm-freq"), uur = $("pm-uur"), avond = $("pm-avond"), avonduur = $("pm-avonduur"), actief = $("pm-actief");
    var sup = $("SupplierId"), proj = $("ProjectId");
    function num(el, d) { var v = parseInt(el && el.value, 10); return isNaN(v) ? d : v; }
    function pad(n) { return (n < 10 ? "0" : "") + n; }

    form.querySelectorAll(".gl-v2-auto-day").forEach(function (b) {
        b.addEventListener("click", function () {
            var was = b.classList.contains("is-active");
            form.querySelectorAll(".gl-v2-auto-day").forEach(function (x) { x.classList.remove("is-active"); x.setAttribute("aria-pressed", "false"); });
            if (!was) { b.classList.add("is-active"); b.setAttribute("aria-pressed", "true"); dag.value = b.dataset.d; } else dag.value = "";
            teken();
        });
    });

    // Volgende momenten: zelfde regel als de server (de gekozen dag; is het uur vandaag nog niet voorbij, dan vandaag; daarna elke N dagen).
    function volgende(n, f, d, h) {
        var nu = new Date(), uit = [], start = new Date(nu.getFullYear(), nu.getMonth(), nu.getDate(), h, 0, 0);
        var eerste;
        if (d !== null) {
            var tot = (d - nu.getDay() + 7) % 7;
            eerste = new Date(start.getTime()); eerste.setDate(eerste.getDate() + tot);
            if (tot === 0 && nu >= start) eerste.setDate(eerste.getDate() + f);
        } else { eerste = new Date(start.getTime()); if (nu >= start) eerste.setDate(eerste.getDate() + f); }
        for (var i = 0; i < n; i++) { var x = new Date(eerste.getTime()); x.setDate(x.getDate() + i * f); uit.push(x); }
        return uit;
    }
    function wie() {
        var l = (sup.value && $("SupplierId_input") ? $("SupplierId_input").value : ""), p = (proj.value && $("ProjectId_input") ? $("ProjectId_input").value : "");
        return { l: l || "Alle aannemers", p: p || "alle projecten", niveau: sup.value && proj.value ? 1 : sup.value ? 2 : proj.value ? 3 : 4 };
    }
    function teken() {
        var f = num(freq, 0), d = dag.value === "" ? null : num(dag, null), h = num(uur, 9), w = wie(), delen = [];
        $("pm-dag-wrap").hidden = f === 0; $("pm-uur-wrap").hidden = f === 0;
        $("pm-avonduur-wrap").hidden = !avond.checked;
        var onderwerp = (sup.value || proj.value) ? (w.l + " · " + w.p) : "Alle aannemers";
        if (f > 0) delen.push((f === 7 && d !== null ? "elke " + DAGEN[d] : f === 14 && d !== null ? "om de 2 weken op " + DAGEN[d] : f === 30 ? "maandelijks" + (d !== null ? " op " + DAGEN[d] : "") : "om de " + f + " dagen" + (d !== null ? " op " + DAGEN[d] : "")) + " om " + pad(h) + ":00 een herinnering");
        if (avond.checked) delen.push("elke dag om " + pad(num(avonduur, 19)) + ":00 een update van de wijzigingen");
        $("pm-sum").textContent = delen.length ? (sup.value || proj.value ? onderwerp : "Alle aannemers") + " krijgen " + delen.join(", en ") + "." + (actief.checked ? "" : " Dit schema staat uit.") : "Dit schema verstuurt niets: de herinnering is uitgeschakeld en de dagelijkse update staat uit.";
        var ul = $("pm-next"); ul.innerHTML = "";
        $("pm-next-wrap").hidden = f === 0 || !actief.checked;
        if (f > 0) volgende(3, f, d, h).forEach(function (x) { var li = document.createElement("li"); li.textContent = DAGEN[x.getDay()].slice(0, 2) + " " + x.getDate() + " " + MAANDEN[x.getMonth()] + " om " + pad(h) + ":00"; ul.appendChild(li); });
        $("pm-ladder").querySelectorAll("li").forEach(function (li) { li.classList.toggle("is-this", parseInt(li.dataset.n, 10) === w.niveau); });
    }
    [freq, uur, avond, avonduur, actief, sup, proj].forEach(function (el) { if (el) { el.addEventListener("change", teken); el.addEventListener("input", teken); } });
    // de keuzelijsten en de zoekvelden melden hun wijziging op de verborgen invoer
    document.addEventListener("gl-v2:combo-change", teken);
    teken();
})();
