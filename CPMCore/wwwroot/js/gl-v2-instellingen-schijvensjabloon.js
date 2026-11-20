/* gl-v2 — Instellingen › Schijvensjablonen › bewerken. Rijen opbouwen, volgorde, controle op het totaal en bewaren (één JSON naar Opslaan). */
(function () {
    "use strict";
    var root = document.getElementById("sjs-root");
    if (!root) return;
    var canWrite = root.getAttribute("data-can-write") === "1";
    var data = {};
    try { data = JSON.parse(document.getElementById("sjs-data").textContent || "{}"); } catch (e) { }

    var $ = function (id) { return document.getElementById(id); };
    var rijen = $("sjs-rijen"), tpl = $("sjs-rij-template"), dirty = false, bezig = false;

    function fmt(n) { return n == null || isNaN(n) ? "" : String(Math.round(n * 100) / 100).replace(".", ","); }
    function parse(s) {
        s = String(s == null ? "" : s).trim().replace(/\s/g, "").replace("%", "").replace(",", ".");
        if (s === "") return null;
        var n = parseFloat(s);
        return isNaN(n) ? NaN : n;
    }
    function markeer() { dirty = true; $("sjs-dirty").hidden = false; som(); }

    function rijEl(s) {
        var li = tpl.content.firstElementChild.cloneNode(true);
        li.querySelector(".js-naam").value = s.naam || "";
        li.querySelector(".js-code").value = s.code || "";
        li.querySelector(".js-vast").value = s.vast == null ? "" : fmt(s.vast);
        li.querySelector(".js-gewicht").value = fmt(s.gewicht == null ? 1 : s.gewicht);
        li.querySelector(".js-verd").checked = !!s.perVerdieping;
        pasToe(li);
        if (!canWrite) li.querySelectorAll("input,button").forEach(function (x) { x.disabled = true; });
        return li;
    }
    // Met een vast percentage telt het gewicht niet mee
    function pasToe(li) {
        var vast = parse(li.querySelector(".js-vast").value);
        var g = li.querySelector(".js-gewicht");
        g.disabled = !canWrite || (vast != null && !isNaN(vast));
        g.closest(".gl-v2-field-box").classList.toggle("is-uit", g.disabled);
    }
    function alle() { return Array.prototype.slice.call(rijen.children); }
    function nummer() {
        var l = alle();
        l.forEach(function (li, i) {
            var eind = i === l.length - 1;
            li.classList.toggle("is-eind", eind);
            li.querySelector(".js-omhoog").disabled = !canWrite || i === 0;
            li.querySelector(".js-omlaag").disabled = !canWrite || eind;
            li.querySelector(".js-wis").disabled = !canWrite || l.length === 1;
        });
    }
    function som() {
        var vast = 0, gewogen = 0, ongeldig = false;
        alle().forEach(function (li) {
            var v = parse(li.querySelector(".js-vast").value);
            if (isNaN(v)) ongeldig = true;
            else if (v != null) vast += v; else gewogen++;
        });
        var el = $("sjs-som");
        if (ongeldig) { el.textContent = "Een percentage is geen getal"; el.dataset.state = "fout"; return; }
        if (gewogen === 0) {
            el.textContent = "Vaste percentages samen " + fmt(vast) + " %" + (Math.abs(vast - 100) > 0.005 ? " (moet 100 % zijn)" : "");
            el.dataset.state = Math.abs(vast - 100) > 0.005 ? "fout" : "ok";
        } else {
            el.textContent = "Vast " + fmt(vast) + " %, de overige " + fmt(100 - vast) + " % naar gewicht";
            el.dataset.state = vast >= 100 ? "fout" : "ok";
        }
    }

    // ── opbouw ──
    $("sjs-naam").value = data.naam || "";
    $("sjs-beschrijving").value = data.beschrijving || "";
    $("sjs-minimum").value = fmt(data.minimum == null ? 5 : data.minimum);
    $("sjs-actief").checked = data.isActief !== false;
    (data.schijven && data.schijven.length ? data.schijven : [{ naam: "", gewicht: 1 }]).forEach(function (s) { rijen.appendChild(rijEl(s)); });
    nummer(); som();
    if (!canWrite) { ["sjs-naam", "sjs-beschrijving", "sjs-minimum", "sjs-actief", "sjs-toevoegen"].forEach(function (id) { $(id).disabled = true; }); }

    // ── bewerken ──
    $("sjs-toevoegen").addEventListener("click", function () {
        // Nieuwe schijf vóór de eindafrekening (de laatste rij)
        var li = rijEl({ naam: "", gewicht: 1 }), l = alle();
        if (l.length > 1) rijen.insertBefore(li, l[l.length - 1]); else rijen.insertBefore(li, l[0]);
        nummer(); markeer();
        li.querySelector(".js-naam").focus();
    });
    rijen.addEventListener("click", function (e) {
        var li = e.target.closest("li"); if (!li || !canWrite) return;
        if (e.target.closest(".js-omhoog") && li.previousElementSibling) rijen.insertBefore(li, li.previousElementSibling);
        else if (e.target.closest(".js-omlaag") && li.nextElementSibling) rijen.insertBefore(li.nextElementSibling, li);
        else if (e.target.closest(".js-wis") && alle().length > 1) li.remove();
        else return;
        nummer(); markeer();
    });
    rijen.addEventListener("input", function (e) {
        var li = e.target.closest("li"); if (li) pasToe(li);
        markeer();
    });
    rijen.addEventListener("change", markeer);
    ["sjs-naam", "sjs-beschrijving", "sjs-minimum"].forEach(function (id) { $(id).addEventListener("input", markeer); });
    $("sjs-actief").addEventListener("change", markeer);

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
            schijven: alle().map(function (li) {
                var vast = parse(li.querySelector(".js-vast").value), gew = parse(li.querySelector(".js-gewicht").value);
                return {
                    naam: li.querySelector(".js-naam").value.trim(),
                    planningCode: li.querySelector(".js-code").value.trim(),
                    vastPercentage: vast == null || isNaN(vast) ? null : vast,
                    gewicht: gew == null || isNaN(gew) ? 1 : gew,
                    perVerdieping: li.querySelector(".js-verd").checked
                };
            })
        };
    }

    var opslaan = $("sjs-opslaan");
    if (opslaan) opslaan.addEventListener("click", function () {
        if (bezig) return;
        var m = model(), lokaal = [];
        alle().forEach(function (li, i) {
            [".js-vast", ".js-gewicht"].forEach(function (k) {
                var v = parse(li.querySelector(k).value);
                if (isNaN(v)) lokaal.push("Schijf " + (i + 1) + ": een percentage of gewicht is geen getal.");
            });
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
