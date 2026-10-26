// gl-v2 — Instellingen › Home hero (punt 55g): live voorbeeld naast het formulier en "Overnemen uit project" (bestaande GET ProjectTekst).
(function () {
    "use strict";
    var form = document.getElementById("hh-form");
    if (!form) return;
    var f = { project: document.getElementById("hh-project"), kicker: document.getElementById("hh-kicker"), titel: document.getElementById("hh-titel"), tekst: document.getElementById("hh-tekst"), ptitel: document.getElementById("hh-ptitel") };
    var pv = { project: document.getElementById("hh-pv-project"), kicker: document.getElementById("hh-pv-kicker"), titel: document.getElementById("hh-pv-titel"), tekst: document.getElementById("hh-pv-tekst") };

    function leeg(el, txt, ph) { el.textContent = txt.trim() || ph; el.classList.toggle("is-placeholder", !txt.trim()); }
    function teken() {
        leeg(pv.kicker, f.kicker.value, "Kicker");
        leeg(pv.titel, f.titel.value, "Titel van de hero");
        leeg(pv.tekst, f.tekst.value, "Tekst onder de titel");
        leeg(pv.project, f.ptitel.value, "het project");
    }
    ["kicker", "titel", "tekst", "ptitel"].forEach(function (k) { f[k].addEventListener("input", teken); });
    teken();

    // Standaardfoto of -video van het project achter de tekst, zoals op de homepage van de website
    var hero = document.getElementById("hh-pv-hero"), mediaEl = document.getElementById("hh-pv-media");
    function zetMedia(m) {
        mediaEl.innerHTML = "";
        if (!m || !m.url) return;
        var el = document.createElement(m.video ? "video" : "img");
        el.src = m.url;
        if (m.video) { el.muted = true; el.loop = true; el.autoplay = true; el.playsInline = true; el.setAttribute("aria-hidden", "true"); } else { el.alt = ""; }
        mediaEl.appendChild(el);
    }
    try { if (hero.dataset.media) zetMedia(JSON.parse(hero.dataset.media)); } catch (e) { /* geen voorbeeldfoto */ }
    f.project.addEventListener("change", function () {
        // Een ander project gekozen: foto meteen ophalen (zonder de tekst te overschrijven)
        if (!f.project.value) { zetMedia(null); return; }
        fetch(form.dataset.urlTekst + "?projectId=" + encodeURIComponent(f.project.value), { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.ok ? r.json() : null; }).then(function (d) { zetMedia(d && d.media); }).catch(function () { zetMedia(null); });
    });

    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }
    var knop = document.getElementById("hh-overnemen");
    if (!knop) return;

    function haal() {
        fetch(form.dataset.urlTekst + "?projectId=" + encodeURIComponent(f.project.value), { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (d) {
                f.titel.value = d.titel || ""; f.tekst.value = d.tekst || ""; f.ptitel.value = d.projectTitel || "";
                teken(); zetMedia(d.media); toast("success", "Tekst overgenomen", "De tekst van het project staat in het formulier. Vergeet niet te bewaren.");
            })
            .catch(function () { toast("danger", "Niet gelukt", "De tekst van dit project kon niet opgehaald worden."); });
    }
    knop.addEventListener("click", function () {
        if (!f.project.value) { toast("warning", "Kies eerst een project", "Selecteer het project waarvan je de tekst wil overnemen."); return; }
        if (!(f.titel.value || f.tekst.value || f.ptitel.value)) { haal(); return; }
        window.GlV2Bevestig({ title: "Tekst overschrijven?", desc: "Titel, tekst en projecttitel worden vervangen door de tekst van dit project.", ok: "Overschrijven", tone: "warning" })
            .then(function (ok) { if (ok) haal(); });
    });
})();
