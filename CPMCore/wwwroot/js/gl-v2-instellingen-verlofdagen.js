// gl-v2 — Instellingen/Verlofdagen (design-handoff punt 50). Gebruikt het projectbrede jaarkalender-component (js/gl-v2-yearcal.js).
// De pagina haalt de algemene verlofdagen één keer op en houdt ze bij in `days` (iso -> {id, kind}); een klik past eerst de lijst aan
// (zodat de kalender meteen reageert) en bevestigt daarna bij de server, die altijd de volledige lijst terugstuurt.
(function () {
    "use strict";

    var config = window.glV2VerlofdagenConfig || {};
    var calEl = document.getElementById("vl-cal");
    if (!calEl || !window.GlV2YearCal) return;
    var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

    var KIND_OF = { B: 0, F: 1 };
    var KEY_OF = { 0: "B", 1: "F" };
    var days = new Map();           // "2026-07-21" -> { id, kind: "B"|"F" }
    var armed = "B";
    var loaded = false;

    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js
    function setText(id, v) { var el = document.getElementById(id); if (el) el.textContent = v; }
    function post(url, data) {
        var fd = new FormData();
        Object.keys(data).forEach(function (k) {
            [].concat(data[k]).forEach(function (v) { fd.append(k, v); });
        });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        });
    }
    function applyList(list) {
        days = new Map();
        (list || []).forEach(function (d) {
            days.set(GlV2YearCal.isoOf(d.year, d.month, d.day), { id: d.id, kind: KEY_OF[d.kind] || "B" });
        });
    }

    var GOLD = { bg: "var(--gl-v2-gold-tint)", fg: "var(--gl-v2-warning)" };
    var GREEN = { bg: "var(--gl-v2-primary-tint)", fg: "var(--gl-v2-primary)" };

    function weekdaysIn(year) {
        var n = 0;
        for (var d = new Date(year, 0, 1); d.getFullYear() === year; d.setDate(d.getDate() + 1)) {
            var w = d.getDay();
            if (w !== 0 && w !== 6) n++;
        }
        return n;
    }

    var cal = GlV2YearCal.create(calEl, {
        year: config.initialYear,
        readonly: !config.canWrite,
        armed: "B",
        categories: {
            B: { label: "Bouwverlof", bg: GREEN.bg, fg: GREEN.fg },
            F: { label: "Feestdag", bg: GOLD.bg, fg: GOLD.fg }
        },
        getCats: function (iso) { var d = days.get(iso); return d ? [d.kind] : []; },
        onToggle: function (iso, key) {
            var d = days.get(iso);
            if (d && d.kind === key) wis(iso, d);
            else zet([iso], key);
        },
        onRange: function (isos, key) {
            var nieuw = isos.filter(function (iso) { var d = days.get(iso); return !d || d.kind !== key; });
            if (nieuw.length) zet(nieuw, key);
        },
        onRender: function (info) {
            setText("vl-year", info.year);
            var b = info.counts.B || 0, f = info.counts.F || 0;
            setText("vl-year-B", b);
            setText("vl-year-F", f);
            setText("vl-count-B", b);
            setText("vl-count-F", f);
            setText("vl-year-werk", weekdaysIn(info.year) - b - f);
            // Nog niets ingesteld voor het getoonde jaar (ook geen feestdag in het weekend): melding met de snelkoppeling.
            var prefix = info.year + "-", heeft = false;
            days.forEach(function (_, iso) { if (iso.indexOf(prefix) === 0) heeft = true; });
            var notice = document.getElementById("vl-empty");
            if (notice) {
                notice.hidden = heeft || !loaded;
                setText("vl-empty-year", info.year);
            }
        }
    });

    window.GlV2YearCal.renderLegend(document.getElementById("vl-legend"), [
        { text: "21", label: "bouwverlof", bg: GREEN.bg, fg: GREEN.fg },
        { text: "11", label: "feestdag", bg: GOLD.bg, fg: GOLD.fg },
        { text: "17", label: "weekend, telt niet mee", kind: "weekend" },
        { text: "15", label: "feestdag in het weekend", kind: "weekend-dot", fg: GOLD.fg }
    ], config.canWrite ? "klik zet · nog eens klikken wist · andere modus overschrijft · slepen zet een reeks" : "");

    function herlaad(list) { applyList(list); cal.render(); }

    function zet(isos, key) {
        // Meteen tonen, daarna bevestigen: de server geeft de echte lijst (met id's) terug.
        isos.forEach(function (iso) { var d = days.get(iso); days.set(iso, { id: d ? d.id : 0, kind: key }); });
        cal.render();
        post(config.setUrl, { dagen: isos, kind: KIND_OF[key] }).then(function (r) {
            if (r.dagen) herlaad(r.dagen);
            if (!r.ok) toast("danger", "Opslaan mislukt", r.bericht || "");
        }).catch(function () { toast("danger", "Opslaan mislukt", "De server gaf geen antwoord."); loadList(); });
    }

    function wis(iso, d) {
        days.delete(iso);
        cal.render();
        if (!d.id) { loadList(); return; }
        post(config.deleteUrl, { id: d.id }).then(function (r) {
            if (r.dagen) herlaad(r.dagen);
            if (!r.ok) toast("danger", "Wissen mislukt", r.bericht || "");
        }).catch(function () { toast("danger", "Wissen mislukt", "De server gaf geen antwoord."); loadList(); });
    }

    function loadList() {
        return fetch(config.listUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (list) { loaded = true; herlaad(list); })
            .catch(function () { cal.setMessage("Kon de verlofdagen niet laden."); });
    }

    // Schakelaar "wat zet je bij een klik"
    var buttons = document.querySelectorAll("#vl-kind .gl-v2-vl-option");
    buttons.forEach(function (btn) {
        btn.addEventListener("click", function () {
            armed = btn.dataset.kind;
            buttons.forEach(function (b) { var on = b === btn; b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); });
            cal.setArmed(armed);
        });
    });

    document.getElementById("vl-prev").addEventListener("click", function () { cal.prev(); });
    document.getElementById("vl-next").addEventListener("click", function () { cal.next(); });

    // Wettelijke feestdagen invullen voor het getoonde jaar
    function vulFeestdagen() {
        var jaar = cal.getYear();
        post(config.fillUrl, { jaar: jaar }).then(function (r) {
            if (r.dagen) herlaad(r.dagen);
            if (r.ok) toast("success", "Feestdagen ingevuld", r.aantal + " wettelijke feestdagen voor " + jaar + ".");
            else toast("danger", "Invullen mislukt", r.bericht || "");
        }).catch(function () { toast("danger", "Invullen mislukt", "De server gaf geen antwoord."); });
    }
    ["vl-fill", "vl-empty-fill"].forEach(function (id) {
        var b = document.getElementById(id);
        if (b) b.addEventListener("click", vulFeestdagen);
    });

    loadList();
})();
