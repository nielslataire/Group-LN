// gl-v2 — Instellingen › Vacatures › Sollicitaties (punt 55e): lijst met filter, kandidaat openen in het zijpaneel (markeert als gelezen), dubbele sollicitaties gemarkeerd.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-sol");
    var dataEl = document.getElementById("sol-data");
    if (!root || !dataEl) return;
    var rows = JSON.parse(dataEl.textContent), canWrite = root.dataset.canWrite === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var filter = "", sel = null;
    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function init(naam) { var p = String(naam || "").trim().split(/\s+/); return ((p[0] || "")[0] || "").toUpperCase() + ((p.length > 1 ? p[p.length - 1][0] : "") || "").toUpperCase(); }
    function byId(id) { return rows.filter(function (r) { return r.id === id; })[0]; }
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js

    function tel() {
        var n = rows.filter(function (r) { return !r.gelezen; }).length;
        $("sol-c-all").textContent = rows.length; $("sol-c-nieuw").textContent = n; $("sol-c-gelezen").textContent = rows.length - n;
    }
    function badge(r) { return '<span class="gl-v2-badge ' + (r.gelezen ? "is-neutral" : "is-info") + '">' + (r.gelezen ? "Gelezen" : "Nieuw") + "</span>"; }
    function lijst() {
        var list = rows.filter(function (r) { return !filter || (filter === "nieuw" ? !r.gelezen : r.gelezen); });
        if (!list.length) { $("sol-rows").innerHTML = '<div class="gl-v2-inh-sol-none">Geen sollicitaties met deze status.</div>'; return; }
        $("sol-rows").innerHTML = list.map(function (r) {
            return '<button type="button" class="gl-v2-inh-sol-row' + (r.id === sel ? " is-active" : "") + (r.gelezen ? "" : " is-new") + '" data-id="' + r.id + '">' +
                '<span class="gl-v2-inh-sol-cand"><span class="av">' + esc(init(r.naam)) + '</span><span class="tx"><span class="n">' + esc(r.naam) + (r.dubbel ? ' <span class="gl-v2-badge is-attention" title="Dubbele sollicitatie">2×</span>' : "") + '</span><span class="m">' + esc(r.vacature || r.email) + "</span></span></span>" +
                '<span class="gl-v2-inh-sol-date">' + esc(r.datum) + "<span>" + esc(r.tijd) + "</span></span>" + badge(r) + "</button>";
        }).join("");
    }

    function detail() {
        var r = byId(sel), el = $("sol-detail");
        if (!r) { el.innerHTML = '<div class="gl-v2-blank gl-v2-inh-sol-blank"><i class="ph ph-user" aria-hidden="true"></i><b>Kies een kandidaat</b><span>Klik links op een sollicitatie om ze te openen.</span></div>'; return; }
        var andere = r.dubbel ? rows.filter(function (x) { return x.id !== r.id && x.vacatureId === r.vacatureId && (x.email || "").toLowerCase() === (r.email || "").toLowerCase(); })[0] : null;
        var h = '<div class="gl-v2-inh-sol-who"><span class="av lg">' + esc(init(r.naam)) + "</span><div><h2>" + esc(r.naam) + "</h2><span>" + esc(r.vacature || "") + " · ontvangen " + esc(r.datum) + " om " + esc(r.tijd) + "</span></div></div>";
        if (andere) h += '<div class="gl-v2-notice gl-v2-notice-compact is-warning"><i class="ph ph-warning" aria-hidden="true"></i><span>Deze persoon solliciteerde al eerder voor deze vacature. <button type="button" class="gl-v2-inh-link" data-open="' + andere.id + '">Bekijk de andere sollicitatie</button></span></div>';
        h += '<div class="gl-v2-inh-sol-sec"><span class="k">STATUS</span><span class="gl-v2-inh-sol-status">' + badge(r) + (canWrite ? ' <button type="button" class="gl-v2-inh-link" id="sol-toggle">' + (r.gelezen ? "Markeer als nieuw" : "Markeer als gelezen") + "</button>" : "") + "</span></div>";
        h += '<div class="gl-v2-inh-sol-sec"><span class="k">CONTACT</span>' +
            (r.email ? '<a class="gl-v2-inh-sol-line" href="mailto:' + esc(r.email) + '"><i class="ph ph-envelope-simple" aria-hidden="true"></i>' + esc(r.email) + "</a>" : "") +
            (r.telefoon ? '<a class="gl-v2-inh-sol-line" href="tel:' + esc(r.telefoon) + '"><i class="ph ph-phone" aria-hidden="true"></i>' + esc(r.telefoon) + "</a>" : "") + "</div>";
        h += '<div class="gl-v2-inh-sol-sec"><span class="k">BIJLAGEN</span>' + (r.cv ? '<a class="gl-v2-inh-sol-file" href="' + esc(root.dataset.urlCv) + "?id=" + r.id + '"><i class="ph ph-file-pdf" aria-hidden="true"></i><span>' + esc(r.cv) + "</span><b>Openen</b></a>" : '<span class="gl-v2-inh-sol-muted">Geen cv bijgevoegd.</span>') + "</div>";
        h += '<div class="gl-v2-inh-sol-sec"><span class="k">MOTIVATIE</span>' + (r.motivatie ? '<p class="gl-v2-inh-sol-text">' + esc(r.motivatie) + "</p>" : '<span class="gl-v2-inh-sol-muted">Geen motivatie ingevuld.</span>') + "</div>";
        h += '<div class="gl-v2-inh-sol-actions">' + (r.email ? '<a class="gl-v2-btn gl-v2-btn-secondary" href="mailto:' + esc(r.email) + '"><i class="ph ph-envelope-simple" aria-hidden="true"></i>Mail kandidaat</a>' : "") +
            (r.telefoon ? '<a class="gl-v2-btn gl-v2-btn-secondary" href="tel:' + esc(r.telefoon) + '"><i class="ph ph-phone" aria-hidden="true"></i>Bellen</a>' : "") +
            '<span class="sp"></span>' + (canWrite ? '<button type="button" class="gl-v2-btn gl-v2-btn-danger" id="sol-del"><i class="ph ph-trash" aria-hidden="true"></i>Verwijderen</button>' : "") + "</div>";
        el.innerHTML = h;
    }

    function zetGelezen(r, gelezen) {
        var fd = new FormData(); fd.append("id", r.id); fd.append("gelezen", gelezen ? "true" : "false"); fd.append("__RequestVerificationToken", token);
        return fetch(root.dataset.urlGelezen, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd })
            .then(function (x) { if (!x.ok) throw new Error("HTTP " + x.status); return x.json(); })
            .then(function (j) {
                if (j.ok) { r.gelezen = gelezen; tel(); lijst(); detail(); }
                else toast("danger", "Niet gelukt", "De status kon niet gewijzigd worden.");
            })
            .catch(function () { toast("danger", "Niet gelukt", "De server gaf geen antwoord."); });
    }
    function open(id) {
        sel = id; var r = byId(id); lijst(); detail();
        if (r && !r.gelezen && canWrite) zetGelezen(r, true);
    }

    $("sol-rows").addEventListener("click", function (e) { var b = e.target.closest("[data-id]"); if (b) open(parseInt(b.dataset.id, 10)); });
    $("sol-detail").addEventListener("click", function (e) {
        var o = e.target.closest("[data-open]"); if (o) { open(parseInt(o.dataset.open, 10)); return; }
        var r = byId(sel); if (!r) return;
        if (e.target.closest("#sol-toggle")) zetGelezen(r, !r.gelezen);
        if (e.target.closest("#sol-del")) window.GlV2Bevestig({ title: "Sollicitatie verwijderen?", desc: "De sollicitatie van " + r.naam + " wordt verwijderd. Je kunt dit niet ongedaan maken.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
            if (!ok) return;
            var f = document.createElement("form"); f.method = "post"; f.action = root.dataset.urlDel;
            [["id", r.id], ["vacatureId", root.dataset.vacatureId || ""], ["__RequestVerificationToken", token]].forEach(function (p) { var i = document.createElement("input"); i.type = "hidden"; i.name = p[0]; i.value = p[1]; f.appendChild(i); });
            document.body.appendChild(f); f.submit();
        });
    });
    $("sol-filter").addEventListener("click", function (e) {
        var b = e.target.closest("[data-f]"); if (!b) return;
        filter = b.dataset.f;
        this.querySelectorAll("button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-pressed", on ? "true" : "false"); });
        lijst();
    });
    tel(); lijst(); detail();
    var eerste = rows.filter(function (r) { return !r.gelezen; })[0] || rows[0];
    if (eerste && window.matchMedia("(min-width: 1025px)").matches) { sel = eerste.id; lijst(); detail(); }
})();
