// gl-v2 — Mijn taken als persoonlijke assistent (design-handoff punt 2): timer, snel starten (Ctrl K), vastleggen, slepen naar dag en week,
// afvinken, de inbox verwerken en de wekelijkse review. Alle schrijfacties zijn JSON-posts naar MijnTakenController (antiforgery via header).
(function () {
    "use strict";
    var tokenForm = document.getElementById("mt-token");
    if (!tokenForm) return;
    var token = tokenForm.querySelector("input[name=__RequestVerificationToken]").value, base = tokenForm.dataset.base;
    var $ = function (id) { return document.getElementById(id); };
    var $$ = function (sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); };
    var toast = function (tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); };

    function post(actie, data) {
        var fd = new FormData();
        Object.keys(data || {}).forEach(function (k) { if (data[k] !== null && data[k] !== undefined && data[k] !== "") fd.append(k, data[k]); });
        return fetch(base + actie, { method: "POST", headers: { "RequestVerificationToken": token, "X-Requested-With": "XMLHttpRequest" }, body: fd })
            .then(function (r) { return r.json(); })
            .then(function (j) { if (!j.ok) { toast("danger", "Dat lukte niet", j.bericht || "Probeer opnieuw."); throw new Error(j.bericht || "fout"); } return j; },
                function (e) { if (e && e.message && e.message !== "fout") toast("danger", "Geen verbinding", "Probeer opnieuw."); throw e; });
    }
    function herlaad() { window.location.reload(); }
    function mislukt() { /* melding is al getoond */ }

    // ── Timer ────────────────────────────────────────────────────────────────────────────────────
    function fmt(s) { var h = Math.floor(s / 3600), m = Math.floor(s / 60) % 60, x = s % 60; return h + ":" + (m < 10 ? "0" : "") + m + ":" + (x < 10 ? "0" : "") + x; }
    var tikkers = $$("[data-mt-tijd][data-loopt='1']").map(function (el) { return { el: el, s: parseInt(el.dataset.sec, 10) || 0 }; });
    var chip = $("mt-timer");
    if (chip && chip.dataset.loopt === "1") tikkers.push({ el: $("mt-timer-tijd"), s: parseInt(chip.dataset.sec, 10) || 0 });
    if (tikkers.length) setInterval(function () { tikkers.forEach(function (t) { t.s += 1; t.el.textContent = fmt(t.s); }); }, 1000);

    function wisselTimer() {
        var loopt = chip ? chip.dataset.loopt === "1" : !!document.querySelector("[data-mt-timer-toggle=pauze]");
        post(loopt ? "TimerPauze" : "TimerHervat", {}).then(herlaad, mislukt);
    }
    var tg = $("mt-timer-toggle"); if (tg) tg.addEventListener("click", wisselTimer);
    $$("[data-mt-timer-toggle]").forEach(function (b) { b.addEventListener("click", wisselTimer); });
    $$("[data-mt-timer-klaar]").forEach(function (b) { b.addEventListener("click", function () { post("TimerKlaar", {}).then(herlaad, mislukt); }); });

    // Gedelegeerde klikken: ▶ starten, afvinken, uit de planning halen, taak maken van een mijlpaal.
    document.addEventListener("click", function (e) {
        var b;
        if ((b = e.target.closest("[data-mt-start]"))) { e.stopPropagation(); post("TimerStart", { taakId: b.dataset.mtStart }).then(herlaad, mislukt); }
        else if ((b = e.target.closest("[data-mt-klaar]"))) {
            e.stopPropagation(); b.classList.add("is-done");
            post("Afvinken", { id: b.dataset.mtKlaar, klaar: true }).then(function () {
                var rij = b.closest(".gl-v2-mt-act, .gl-v2-mt-wait"); if (rij) rij.classList.add("is-gone");
                setTimeout(herlaad, 350);
            }, function () { b.classList.remove("is-done"); });
        }
        else if ((b = e.target.closest("[data-mt-ontplan]"))) { e.stopPropagation(); post("Ontplan", { id: b.dataset.mtOntplan }).then(herlaad, mislukt); }
        else if ((b = e.target.closest("[data-mt-mijlpaal]"))) { b.disabled = true; post("TaakVanMijlpaal", { mijlpaalId: b.dataset.mtMijlpaal }).then(herlaad, function () { b.disabled = false; }); }
    });

    // Context-filter van de volgende acties.
    $$("#mt-ctx button").forEach(function (b) {
        b.addEventListener("click", function () {
            $$("#mt-ctx button").forEach(function (x) { x.classList.toggle("is-active", x === b); });
            $$("#mt-acts .gl-v2-mt-act").forEach(function (a) { a.hidden = !!b.dataset.ctx && a.dataset.ctx !== b.dataset.ctx; });
        });
    });

    // ── Vastleggen ───────────────────────────────────────────────────────────────────────────────
    var cap = $("mt-capture");
    function inboxBadge(aantal) {
        var tab = document.querySelector(".gl-v2-tabbar a[href$='/Inbox']"); if (!tab) return;
        var c = tab.querySelector(".gl-v2-mt-count");
        if (aantal <= 0) { if (c) c.remove(); return; }
        if (!c) { c = document.createElement("span"); c.className = "gl-v2-tabbar-count gl-v2-mt-count"; tab.appendChild(c); }
        c.textContent = aantal;
    }
    function inboxAantal() { var c = document.querySelector(".gl-v2-tabbar .gl-v2-mt-count"); return c ? parseInt(c.textContent, 10) || 0 : 0; }
    if (cap) cap.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" || !cap.value.trim()) return;
        e.preventDefault();
        var inbox = !!$("mt-inbox");
        post("Leg", { titel: cap.value.trim() }).then(function () {
            cap.value = "";
            if (inbox) return herlaad();
            inboxBadge(inboxAantal() + 1); toast("success", "Vastgelegd", "Het staat in je inbox.");
        }, mislukt);
    });

    // ── Kalender (Outlook-achtig) ──────────────────────────────────────────────────────────────────
    // Muis: slepen om te verplaatsen, randen slepen om te vergroten, klikken om te wijzigen, slepen over een leeg stuk om aan te maken.
    // Aanraking (tablet, gsm): tikken om te wijzigen, randen slepen om te vergroten, ingedrukt houden en slepen om te verplaatsen of aan te maken,
    // en de knop "Nieuwe taak" of een tik op een volgende actie om in te plannen. Taken én afspraken uit Outlook (als jij de organisator bent) gedragen zich gelijk.
    var kal = $("mt-vandaag") || $("mt-week");
    var UUR = kal ? parseInt(kal.dataset.uurVan, 10) : 7, PX = kal ? parseInt(kal.dataset.px, 10) : 64;
    var gl = document.querySelector(".gl-v2") || document.body;
    var pad = function (n) { return (n < 10 ? "0" : "") + n; };
    var hm = function (min) { return pad(Math.floor(min / 60)) + ":" + pad(min % 60); };
    var parseHM = function (v) { var m = /^(\d{1,2}):(\d{2})$/.exec(v || ""); return m ? parseInt(m[1], 10) * 60 + parseInt(m[2], 10) : null; };
    var snap = function (min) { return Math.round(min / 15) * 15; };
    var minVanY = function (y) { return UUR * 60 + y / PX * 60; };
    var yVanMin = function (min) { return (min / 60 - UUR) * PX; };
    var duurTekst = function (m) { return m < 60 ? m + " min" : (m % 60 ? Math.floor(m / 60) + "u " + pad(m % 60) + "m" : Math.floor(m / 60) + "u"); };
    var dagMin = function (iso) { return parseHM(iso.slice(11, 16)); };
    var vandaagIso = function () { var d = new Date(); return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate()); };
    var zojuist = false;
    function sluit() { zojuist = true; setTimeout(function () { zojuist = false; }, 400); }

    var dag = $("mt-vandaag"), tl = $("mt-timeline"), week = $("mt-week"), lane = $("mt-lane");

    // De kalender begint bij het werkuur van nu (of 07:00) in plaats van middernacht.
    var scroller = $("mt-scroll");
    if (scroller) {
        var nu = new Date(), doelUur = (dag || (week && week.querySelector(".gl-v2-mt-daycol.is-today"))) ? Math.max(7, nu.getHours() - 1) : 7;
        scroller.scrollTop = Math.max(0, (doelUur - UUR) * PX - 8);
        try { var bewaard = sessionStorage.getItem("mt-scroll"); if (bewaard !== null) { scroller.scrollTop = parseInt(bewaard, 10) || 0; sessionStorage.removeItem("mt-scroll"); } } catch (e) { }
    }

    // Eén plek die een taak of afspraak naar een nieuw begin zet.
    function blokInfo(b) { return { kind: b.dataset.kind, id: b.dataset.id, event: b.dataset.event, min: parseInt(b.dataset.min, 10) || 30 }; }
    function verplaats(info, datum, startMin, minuten) {
        var start = datum + "T" + hm(startMin);
        return info.kind === "afspraak" ? post("AgendaWijzig", { eventId: info.event, start: start, minuten: minuten })
                                         : post("Plan", { id: info.id, start: start, minuten: minuten });
    }
    function pasTijdAan(info, titel, datum, startMin, minuten) {
        var start = datum + "T" + hm(startMin);
        return info.kind === "afspraak" ? post("AgendaWijzig", { eventId: info.event, titel: titel, start: start, minuten: minuten })
                                         : post("PlanWijzig", { id: info.id, titel: titel, start: start, minuten: minuten });
    }
    // Het doel (dagtijdlijn of weekkolom) onder een punt, met zijn datum.
    function doelOnder(x, y) {
        var els = document.elementsFromPoint(x, y);
        for (var i = 0; i < els.length; i++) {
            if (els[i].classList.contains("gl-v2-mt-daycol")) return { el: els[i], datum: els[i].dataset.datum };
            if (els[i].classList.contains("gl-v2-mt-acts")) return { el: els[i], lijst: true };
            if (tl && els[i] === tl && dag) return { el: tl, datum: dag.dataset.datum };
        }
        return null;
    }
    function minOnder(doel, y, off) {
        var min = snap(minVanY(y - doel.getBoundingClientRect().top - (off || 0)));
        return Math.max(UUR * 60, Math.min(min, 23 * 60 + 45));
    }

    // ── Plaatshouder en onmiddellijk plaatsen ──
    // Tijdens het slepen toont de kalender op het doel een gestippeld vak op de plek waar het blok zou komen (afgerond op 15 minuten, met het uur erin).
    // Bij het loslaten staat het blok meteen op die plek; pas daarna wordt het opgeslagen. Lukt dat niet, dan springt het blok terug naar waar het stond.
    var plek = null, plek2 = null, plekVast = false;
    // Een afspraak en haar blok Reistijd (data-koppel) schuiven samen: het andere blok behoudt zijn afstand tot het versleepte blok, in minuten.
    function partnerVan(blok) {
        var k = blok && blok.dataset.koppel; if (!k) return null;
        var p = $$(".gl-v2-mt-block").filter(function (x) { return x !== blok && x.dataset.koppel === k; })[0]; if (!p) return null;
        var t = function (iso) { return new Date(iso).getTime() / 60000; };
        return { el: p, offset: Math.round(t(p.dataset.start) - t(blok.dataset.start)), minuten: Math.round(t(p.dataset.einde) - t(p.dataset.start)) };
    }
    function gastheer(doel) { return doel === tl ? (tl.querySelector(".gl-v2-mt-lane.is-main") || tl) : doel; }
    function zetPlekVak(vak, host, min, minuten) {
        if (vak.parentNode !== host) host.appendChild(vak);
        vak.style.top = yVanMin(min) + "px"; vak.style.height = Math.max(minuten / 60 * PX - 2, 20) + "px";
        vak.firstChild.textContent = hm(Math.max(0, min)) + "\u2013" + hm(Math.min(min + minuten, 24 * 60));
    }
    function nieuwVak() { var v = el("div", "gl-v2-mt-plek"); v.setAttribute("aria-hidden", "true"); v.appendChild(el("span", "m")); return v; }
    // Eén vak voor het versleepte blok, en een tweede voor het gekoppelde blok als er een is.
    function toonPlek(doel, min, minuten, partner) {
        var host = gastheer(doel);
        if (!plek) plek = nieuwVak();
        zetPlekVak(plek, host, min, minuten);
        if (partner) { if (!plek2) { plek2 = nieuwVak(); plek2.classList.add("is-koppel"); } zetPlekVak(plek2, host, min + partner.offset, partner.minuten); }
        else if (plek2) plek2.remove();
    }
    function wisPlek(ook) { if (plek && (ook || !plekVast)) { plek.remove(); if (plek2) plek2.remove(); plekVast = false; } }
    function herlaadBehoudScroll() { try { if (scroller) sessionStorage.setItem("mt-scroll", String(scroller.scrollTop)); } catch (e) { } herlaad(); }
    // Zet een bestaand blok direct op de nieuwe plek en geeft een functie terug die het weer terugzet.
    function plaatsBlok(blok, doel, datum, min, minuten) {
        var orig = { ouder: blok.parentNode, volgend: blok.nextSibling, top: blok.style.top, left: blok.style.left, width: blok.style.width,
            start: blok.dataset.start, einde: blok.dataset.einde, tekst: blok.querySelector(".m") ? blok.querySelector(".m").textContent : null };
        var eind = Math.min(min + minuten, 24 * 60);
        gastheer(doel).appendChild(blok);
        blok.style.top = yVanMin(min) + "px"; blok.style.left = "1px"; blok.style.width = "calc(100% - 3px)";
        blok.dataset.start = datum + "T" + hm(min); blok.dataset.einde = datum + "T" + hm(eind);
        var m = blok.querySelector(".m"); if (m) m.textContent = hm(min) + "\u2013" + hm(eind);
        blok.classList.add("is-bezig");
        return function () {
            blok.classList.remove("is-bezig");
            orig.ouder.insertBefore(blok, orig.volgend);
            blok.style.top = orig.top; blok.style.left = orig.left; blok.style.width = orig.width;
            blok.dataset.start = orig.start; blok.dataset.einde = orig.einde;
            if (m && orig.tekst !== null) m.textContent = orig.tekst;
        };
    }
    // Eén plek voor muis en aanraking: toon het resultaat meteen, sla dan op, en zet alles terug als het niet lukt.
    function neerzetten(info, blok, doel, datum, min) {
        var terug = null;
        if (blok) {
            wisPlek(true);
            var partner = partnerVan(blok), terugBlok = plaatsBlok(blok, doel, datum, min, info.min);
            var terugPartner = partner ? plaatsBlok(partner.el, doel, datum, min + partner.offset, partner.minuten) : null;
            terug = function () { terugBlok(); if (terugPartner) terugPartner(); };
        }
        else if (plek) { plekVast = true; plek.classList.add("is-bezig"); }   // een taak uit de lijst: de plaatshouder blijft staan tot het opgeslagen is
        return verplaats(info, datum, min, info.min).then(herlaadBehoudScroll, function () {
            if (terug) terug(); else { if (plek) plek.classList.remove("is-bezig"); wisPlek(true); }
        });
    }

    // ── Verplaatsen met de muis (HTML5-slepen) ──
    var sleep = null;
    document.addEventListener("dragstart", function (e) {
        var el = e.target.closest && e.target.closest("[draggable=true]"); if (!el) return;
        var isBlok = el.classList.contains("gl-v2-mt-block");
        if (!isBlok && !el.dataset.id) return;
        sluitPop();
        // Bij een bestaand blok onthouden we waar je het vastpakte, zodat het niet naar de cursor springt.
        sleep = { blok: isBlok, info: isBlok ? blokInfo(el) : { kind: "taak", id: el.dataset.id, min: parseInt(el.dataset.min, 10) || 30 }, off: isBlok ? e.clientY - el.getBoundingClientRect().top : 0, el: isBlok ? el : null, partner: isBlok ? partnerVan(el) : null };
        e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", el.dataset.id || el.dataset.event || "");
        el.classList.add("is-dragging");
    });
    document.addEventListener("dragend", function () { sleep = null; wisPlek(false); $$(".is-dragging, .is-dropping").forEach(function (x) { x.classList.remove("is-dragging", "is-dropping"); }); });
    function maakDoel(doel, datumFn) {
        doel.addEventListener("dragover", function (e) {
            if (!sleep) return; e.preventDefault(); e.dataTransfer.dropEffect = "move"; doel.classList.add("is-dropping");
            toonPlek(doel, minOnder(doel, e.clientY, sleep.off), sleep.info.min, sleep.partner);
        });
        doel.addEventListener("dragleave", function (e) { if (!doel.contains(e.relatedTarget)) { doel.classList.remove("is-dropping"); if (plek && gastheer(doel).contains(plek)) wisPlek(false); } });
        doel.addEventListener("drop", function (e) {
            if (!sleep) return; e.preventDefault(); doel.classList.remove("is-dropping");
            var s = sleep; sleep = null;
            neerzetten(s.info, s.el, doel, datumFn(), minOnder(doel, e.clientY, s.off));
        });
    }
    // Terugslepen naar de lijst met volgende acties: het blok is dan niet meer ingepland (enkel bij een taak, niet bij een Outlook-afspraak).
    $$(".gl-v2-mt-acts").forEach(function (lijst) {
        lijst.addEventListener("dragover", function (e) { if (!sleep || !sleep.blok || sleep.info.kind !== "taak") return; e.preventDefault(); lijst.classList.add("is-dropping"); });
        lijst.addEventListener("dragleave", function (e) { if (!lijst.contains(e.relatedTarget)) lijst.classList.remove("is-dropping"); });
        lijst.addEventListener("drop", function (e) {
            if (!sleep || !sleep.blok || sleep.info.kind !== "taak") return; e.preventDefault(); lijst.classList.remove("is-dropping");
            var s = sleep; sleep = null; post("Ontplan", { id: s.info.id }).then(herlaad, mislukt);
        });
    });
    if (dag && tl) maakDoel(tl, function () { return dag.dataset.datum; });
    if (week) $$(".gl-v2-mt-daycol", week).forEach(function (col) { maakDoel(col, function () { return col.dataset.datum; }); });

    // ── Vergroten en verkleinen (muis én aanraking): sleep de boven- of onderrand, in stappen van 15 minuten ──
    document.addEventListener("pointerdown", function (e) {
        var h = e.target.closest && e.target.closest(".gl-v2-mt-resize"); if (!h || (e.pointerType === "mouse" && e.button !== 0)) return;
        var b = h.closest(".gl-v2-mt-block"); e.preventDefault(); e.stopPropagation(); sluitPop();
        var top = h.classList.contains("is-top"), s0 = dagMin(b.dataset.start), e0 = dagMin(b.dataset.einde), y0 = e.clientY, s = s0, en = e0, info = blokInfo(b);
        var tekst = b.querySelector(".m");
        b.draggable = false;            // anders start de browser een sleepactie in plaats van het vergroten
        b.classList.add("is-resizing");
        try { h.setPointerCapture(e.pointerId); } catch (x) { }
        function beweeg(ev) {
            var dm = snap((ev.clientY - y0) / PX * 60);
            if (top) { s = Math.max(UUR * 60, Math.min(s0 + dm, e0 - 15)); } else { en = Math.min(23 * 60 + 45, Math.max(e0 + dm, s0 + 15)); }
            b.style.top = yVanMin(s) + "px"; b.style.height = Math.max((en - s) / 60 * PX - 2, 20) + "px";
            if (tekst) tekst.textContent = hm(s) + "–" + hm(en);
        }
        function los() {
            h.removeEventListener("pointermove", beweeg); h.removeEventListener("pointerup", los); h.removeEventListener("pointercancel", los);
            sluit(); b.classList.remove("is-resizing"); b.draggable = true;
            if (s === s0 && en === e0) return;
            pasTijdAan(info, "", b.dataset.start.slice(0, 10), s, en - s).then(herlaad, herlaad);
        }
        h.addEventListener("pointermove", beweeg); h.addEventListener("pointerup", los); h.addEventListener("pointercancel", los);
    });

    // ── Verplaatsen met aanraking: ingedrukt houden en slepen (blokken en volgende acties) ──
    document.addEventListener("pointerdown", function (e) {
        if (e.pointerType === "mouse" || e.button !== 0 || e.target.closest(".gl-v2-mt-resize, button, a, input, select, textarea")) return;
        var src = e.target.closest(".gl-v2-mt-block[data-edit='1'], .gl-v2-mt-act"); if (!src || !(dag || week)) return;
        var isBlok = src.classList.contains("gl-v2-mt-block");
        var info = isBlok ? blokInfo(src) : { kind: "taak", id: src.dataset.id, min: parseInt(src.dataset.min, 10) || 30 };
        var x0 = e.clientX, y0 = e.clientY, off = isBlok ? e.clientY - src.getBoundingClientRect().top : 0;
        var actief = false, ghost = null, timer = null, partnerBlok = isBlok ? partnerVan(src) : null;
        src.draggable = false;
        function blokkeer(ev) { if (actief) ev.preventDefault(); }
        function ruim() {
            clearTimeout(timer); document.removeEventListener("pointermove", beweeg); document.removeEventListener("pointerup", los); document.removeEventListener("pointercancel", ruim);
            document.removeEventListener("touchmove", blokkeer); if (ghost) ghost.remove(); src.classList.remove("is-dragging"); wisPlek(false); if (isBlok || src.dataset.id) src.draggable = true;
            $$(".is-dropping").forEach(function (x) { x.classList.remove("is-dropping"); });
        }
        timer = setTimeout(function () {
            actief = true; src.classList.add("is-dragging"); if (navigator.vibrate) navigator.vibrate(15);
            ghost = src.cloneNode(true); ghost.classList.add("gl-v2-mt-ghost"); ghost.removeAttribute("id"); ghost.style.width = src.offsetWidth + "px";
            gl.appendChild(ghost); zet(x0, y0);
            document.addEventListener("touchmove", blokkeer, { passive: false });
        }, 350);
        function zet(x, y) {
            ghost.style.left = (x - src.offsetWidth / 2) + "px"; ghost.style.top = (y - (isBlok ? off : 18)) + "px";
            $$(".is-dropping").forEach(function (z) { z.classList.remove("is-dropping"); });
            var d = doelOnder(x, y); if (d) d.el.classList.add("is-dropping");
            if (d && !d.lijst) toonPlek(d.el, minOnder(d.el, y, isBlok ? off : 0), info.min, partnerBlok); else wisPlek(false);
        }
        function beweeg(ev) {
            if (!actief) { if (Math.abs(ev.clientX - x0) > 8 || Math.abs(ev.clientY - y0) > 8) ruim(); return; }   // gewoon scrollen
            ev.preventDefault(); zet(ev.clientX, ev.clientY);
        }
        function los(ev) {
            var was = actief, d = was ? doelOnder(ev.clientX, ev.clientY) : null;
            ruim();
            if (!was) return;
            sluit();
            if (d && d.lijst) { if (isBlok && info.kind === "taak") post("Ontplan", { id: info.id }).then(herlaad, mislukt); }
            else if (d) neerzetten(info, isBlok ? src : null, d.el, d.datum, minOnder(d.el, ev.clientY, isBlok ? off : 0));
        }
        document.addEventListener("pointermove", beweeg); document.addEventListener("pointerup", los); document.addEventListener("pointercancel", ruim);
    });
    // Geen contextmenu of tekstselectie bij ingedrukt houden op een blok.
    document.addEventListener("contextmenu", function (e) { if (e.target.closest && e.target.closest(".gl-v2-mt-block, .gl-v2-mt-act") && e.pointerType !== "mouse" && "ontouchstart" in window) e.preventDefault(); });

    // ── Verwijderen ──
    // Het blok (of de twee gekoppelde blokken: afspraak en reistijd) verdwijnt meteen uit de agenda; pas daarna wordt het opgeslagen. Lukt dat niet, dan staat alles weer terug.
    function bevestig(opties) {
        if (window.GlV2Dialog) return window.GlV2Dialog.confirm(opties);
        return Promise.resolve(window.confirm(opties.title));
    }
    function verwijderBlokken(blokken, aanvraag) {
        var terug = blokken.map(function (b) { return { b: b, ouder: b.parentNode, volgend: b.nextSibling }; });
        blokken.forEach(function (b) { b.remove(); });
        return aanvraag().then(herlaadBehoudScroll, function () { terug.forEach(function (t) { if (t.ouder) t.ouder.insertBefore(t.b, t.volgend); }); });
    }
    function gekoppeldeBlokken(b) {
        var k = b.dataset.koppel;
        return k ? $$(".gl-v2-mt-block").filter(function (x) { return x.dataset.koppel === k; }) : [b];
    }
    // Wie op een afspraak of haar reistijd wijst, ziet ook het andere blok oplichten.
    document.addEventListener("mouseover", function (e) {
        var b = e.target.closest && e.target.closest(".gl-v2-mt-block[data-koppel]"); if (!b || !b.dataset.koppel) return;
        gekoppeldeBlokken(b).forEach(function (x) { x.classList.add("is-koppel-hover"); });
    });
    document.addEventListener("mouseout", function (e) {
        var b = e.target.closest && e.target.closest(".gl-v2-mt-block[data-koppel]"); if (!b) return;
        $$(".is-koppel-hover").forEach(function (x) { x.classList.remove("is-koppel-hover"); });
    });

    // ── Popover ──
    var pop = null;
    function sluitPop() { if (pop) { pop.remove(); pop = null; } }
    function maakPop(kleur, kop) {
        sluitPop();
        pop = el("div", "gl-v2-mt-pop"); pop.style.setProperty("--c", kleur || "#00532D"); pop.setAttribute("role", "dialog"); pop.setAttribute("aria-label", kop);
        var hd = el("div", "gl-v2-mt-pop-head"); hd.appendChild(el("span", "k", kop));
        var x = el("button", "gl-v2-mt-pop-close"); x.type = "button"; x.setAttribute("aria-label", "Sluiten"); x.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
        x.addEventListener("click", sluitPop); hd.appendChild(x); pop.appendChild(hd);
        var body = el("div", "gl-v2-mt-pop-body"); pop.appendChild(body); gl.appendChild(pop);   // in .gl-v2: de --gl-v2-tokens bestaan niet in <body>
        return body;
    }
    function plaats(rect) {
        if (window.innerWidth <= 600) { pop.classList.add("is-sheet"); return; }   // op een gsm: onderaan het scherm
        var w = pop.offsetWidth, h = pop.offsetHeight, vw = window.innerWidth, vh = window.innerHeight;
        var left = rect.right + 10; if (left + w > vw - 12) left = Math.max(12, rect.left - w - 10);
        var top = Math.max(12, Math.min(rect.top, vh - h - 12));
        pop.style.left = left + "px"; pop.style.top = top + "px";
    }
    function veld(label, type, waarde, id) {
        var f = el("div", "gl-v2-field"), l = el("label", "gl-v2-field-label", label); l.setAttribute("for", id);
        var bx = el("div", "gl-v2-field-box"), i = el("input", "gl-v2-field-input"); i.id = id; i.type = type; i.value = waarde || "";
        bx.appendChild(i); f.appendChild(l); f.appendChild(bx); return { f: f, i: i };
    }
    function knop(tekst, cls, icoon) { var b = el("button", "gl-v2-btn " + cls); b.type = "button"; if (icoon) { var i = el("i", "ph " + icoon); i.setAttribute("aria-hidden", "true"); b.appendChild(i); } b.appendChild(document.createTextNode(tekst)); return b; }
    function rij(icoon, tekst) { var r = el("div", "gl-v2-mt-pop-rij"); var i = el("i", "ph " + icoon); i.setAttribute("aria-hidden", "true"); r.appendChild(i); r.appendChild(document.createTextNode(tekst)); return r; }

    // Tijden-formulier voor wijzigen, aanmaken en inplannen: datum, van en tot, met de duur eronder.
    function tijdVelden(body, datum, van, tot) {
        var r = el("div", "gl-v2-mt-pop-tijden");
        var d = veld("Datum", "date", datum, "mt-pop-datum"), v = veld("Van", "time", hm(van), "mt-pop-van"), t = veld("Tot", "time", hm(tot), "mt-pop-tot");
        v.i.step = t.i.step = 300; r.appendChild(d.f); r.appendChild(v.f); r.appendChild(t.f); body.appendChild(r);
        var duur = el("div", "gl-v2-mt-pop-duur"); body.appendChild(duur);
        function toon() { var a = parseHM(v.i.value), z = parseHM(t.i.value); duur.textContent = a != null && z != null && z > a ? "Duur: " + duurTekst(z - a) : "Het einde moet na het begin liggen."; }
        v.i.addEventListener("input", toon); t.i.addEventListener("input", toon); toon();
        return { lees: function () { var a = parseHM(v.i.value), z = parseHM(t.i.value); return (a == null || z == null || z <= a || !d.i.value) ? null : { datum: d.i.value, van: a, minuten: z - a }; } };
    }
    function volgendHalfuur() { var d = new Date(), m = Math.ceil((d.getHours() * 60 + d.getMinutes() + 1) / 30) * 30; return Math.max(UUR * 60, Math.min(m, 22 * 60)); }

    // Afspraak aan een project koppelen: het label, het voorstel van CPM (altijd te bevestigen) en de keuze van een ander project.
    var afspraakModal = $("mt-afspraak-koppel-modal"), afspraakEvent = "";
    function koppelBlok(body, ac, b) {
        if (!b.dataset.event) return;
        if (b.dataset.projectId) {
            if (b.dataset.project) body.appendChild(rij("ph-buildings", b.dataset.project));
            var ander = knop("Ander project", "gl-v2-btn-secondary", "ph-link"), los = knop("Ontkoppelen", "gl-v2-btn-text", "ph-link-break");
            ac.appendChild(ander); ac.appendChild(los);
            ander.addEventListener("click", function () { openAfspraakKoppel(b); });
            los.addEventListener("click", function () { post("AfspraakOntkoppel", { eventId: b.dataset.event }).then(herlaad, mislukt); });
            return;
        }
        if (b.dataset.voorstelId) {
            var ja = knop("Koppel aan " + b.dataset.voorstel, "gl-v2-btn-primary", "ph-sparkle");
            ja.addEventListener("click", function () { ja.disabled = true; post("AfspraakKoppel", { eventId: b.dataset.event, projectId: b.dataset.voorstelId }).then(herlaad, function () { ja.disabled = false; }); });
            ac.appendChild(ja);
        }
        var kies = knop(b.dataset.voorstelId ? "Ander project" : "Koppelen aan project", "gl-v2-btn-secondary", "ph-link");
        kies.addEventListener("click", function () { openAfspraakKoppel(b); });
        ac.appendChild(kies);
    }
    function openAfspraakKoppel(b) {
        afspraakEvent = b.dataset.event; sluitPop();
        var o = $("mt-afspraak-koppel-onderwerp"); if (o) o.textContent = b.dataset.titel || "";
        var veld = afspraakModal && afspraakModal.querySelector("[name=projectId]"); if (veld) veld.value = "";
        if (afspraakModal && window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(afspraakModal).show();
    }
    if (afspraakModal) afspraakModal.querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        var veld = afspraakModal.querySelector("[name=projectId]");
        if (!veld || !veld.value) { toast("danger", "Kies eerst een project."); return; }
        post("AfspraakKoppel", { eventId: afspraakEvent, projectId: veld.value }).then(herlaad, mislukt);
    });

    function openBlok(b) {
        var afspraak = b.dataset.kind === "afspraak", bewerkbaar = b.dataset.edit === "1", info = blokInfo(b);
        var body = maakPop(afspraak ? "#2F4F7A" : getComputedStyle(b).getPropertyValue("--c"), afspraak ? "AFSPRAAK UIT OUTLOOK" : "TAAK");
        if (!bewerkbaar) {
            body.appendChild(el("div", "gl-v2-mt-pop-titel", b.dataset.titel));
            body.appendChild(rij("ph-clock", b.dataset.start.slice(8, 10) + "/" + b.dataset.start.slice(5, 7) + " · " + b.dataset.start.slice(11, 16) + "–" + b.dataset.einde.slice(11, 16)));
            if (b.dataset.plaats) body.appendChild(rij("ph-map-pin", b.dataset.plaats));
            if (b.dataset.online === "1") body.appendChild(rij("ph-video-camera", "Online vergadering"));
            body.appendChild(el("div", "gl-v2-mt-pop-noot", "Je bent niet de organisator van deze afspraak. Enkel de organisator kan ze wijzigen."));
        } else {
            var titel = veld("Titel", "text", b.dataset.titel, "mt-pop-titel"); titel.i.maxLength = 200; body.appendChild(titel.f);
            var tv = tijdVelden(body, b.dataset.start.slice(0, 10), dagMin(b.dataset.start), dagMin(b.dataset.einde));
            if (afspraak && b.dataset.plaats) body.appendChild(rij("ph-map-pin", b.dataset.plaats));
            if (afspraak && b.dataset.online === "1") body.appendChild(rij("ph-video-camera", "Online vergadering"));
            if (!afspraak && b.dataset.project) body.appendChild(rij("ph-buildings", b.dataset.project));
            if (afspraak && b.dataset.koppel) body.appendChild(rij(b.dataset.reistijd === "1" ? "ph-link-simple" : "ph-car", b.dataset.reistijd === "1" ? "Reistijd bij een afspraak. Verplaats je dit blok, dan gaat de afspraak mee." : "Heeft een blok reistijd. Het gaat mee als je de afspraak verplaatst."));
            if (afspraak && b.dataset.deelnemers === "1") body.appendChild(el("div", "gl-v2-mt-pop-noot", "Deze afspraak heeft deelnemers. Outlook stuurt hen een update als je de tijd wijzigt."));
            if (afspraak) body.appendChild(el("div", "gl-v2-mt-pop-noot", "Je wijziging staat meteen in Outlook."));
        }
        var ac = el("div", "gl-v2-mt-pop-acties");
        if (bewerkbaar) {
            var opslaan = knop("Opslaan", "gl-v2-btn-primary", "ph-floppy-disk"); ac.appendChild(opslaan);
            var bewaar = function () {
                var t = tv.lees(); if (!t) { toast("warning", "Controleer de tijden", "Het einde moet na het begin liggen."); return; }
                if (!titel.i.value.trim()) { toast("warning", "Titel ontbreekt", "Geef een titel op."); return; }
                opslaan.disabled = true; pasTijdAan(info, titel.i.value.trim(), t.datum, t.van, t.minuten).then(herlaad, function () { opslaan.disabled = false; });
            };
            opslaan.addEventListener("click", bewaar);
            titel.i.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); bewaar(); } });
        }
        if (!afspraak) {
            var start = knop("Start", "gl-v2-btn-secondary", "ph-play"), klaar = knop("Klaar", "gl-v2-btn-secondary", "ph-check"), weg = knop("Terug naar acties", "gl-v2-btn-text", "ph-arrow-u-up-left");
            ac.appendChild(start); ac.appendChild(klaar); ac.appendChild(el("span", "sp")); ac.appendChild(weg);
            var verwijder = knop("Verwijderen", "gl-v2-btn-text gl-v2-mt-verwijder", "ph-trash"); ac.appendChild(verwijder);
            verwijder.addEventListener("click", function () {
                bevestig({ title: "Deze taak verwijderen?", description: "“" + b.dataset.titel + "” verdwijnt uit je agenda en je lijsten. De tijd die je er al aan werkte, blijft bewaard.", confirmLabel: "Verwijderen", cancelLabel: "Annuleren", tone: "danger", icon: "ph-trash" }).then(function (ja) {
                    if (!ja) return; sluitPop();
                    verwijderBlokken([b], function () { return post("TaakVerwijder", { id: b.dataset.id }); });
                });
            });
            start.addEventListener("click", function () { post("TimerStart", { taakId: b.dataset.id }).then(herlaad, mislukt); });
            klaar.addEventListener("click", function () { post("Afvinken", { id: b.dataset.id, klaar: true }).then(herlaad, mislukt); });
            weg.addEventListener("click", function () { post("Ontplan", { id: b.dataset.id }).then(herlaad, mislukt); });
        } else if (b.dataset.link) {
            var a = el("a", "gl-v2-btn gl-v2-btn-secondary"); a.href = b.dataset.link; a.target = "_blank"; a.rel = "noopener";
            a.innerHTML = '<i class="ph ph-arrow-square-out" aria-hidden="true"></i>Openen in Outlook'; ac.appendChild(a);
        }
        if (afspraak && bewerkbaar) {
            var verwijderAfspraak = knop("Verwijderen", "gl-v2-btn-text gl-v2-mt-verwijder", "ph-trash"); ac.appendChild(verwijderAfspraak);
            verwijderAfspraak.addEventListener("click", function () {
                var paar = gekoppeldeBlokken(b), metBlok = paar.length > 1;
                var uitleg = "“" + b.dataset.titel + "” verdwijnt uit je Outlook-agenda." + (metBlok ? " Het gekoppelde blok reistijd gaat samen met de afspraak weg." : "")
                    + (b.dataset.deelnemers === "1" ? " Outlook stuurt de deelnemers een annulering." : "") + (b.dataset.projectId ? " De koppeling met het project verdwijnt ook." : "");
                bevestig({ title: "Deze afspraak verwijderen?", description: uitleg, confirmLabel: "Verwijderen", cancelLabel: "Annuleren", tone: "danger", icon: "ph-trash" }).then(function (ja) {
                    if (!ja) return; sluitPop();
                    verwijderBlokken(paar, function () { return post("AgendaVerwijder", { eventId: b.dataset.event }); });
                });
            });
        }
        if (afspraak) koppelBlok(body, ac, b);
        if (ac.childNodes.length) body.appendChild(ac);
        plaats(b.getBoundingClientRect()); if (bewerkbaar) { titel.i.focus(); if (!("ontouchstart" in window)) titel.i.select(); }
    }
    function openNieuw(datum, van, tot, rect) {
        var body = maakPop("#00532D", "NIEUWE TAAK");
        var titel = veld("Titel", "text", "", "mt-pop-titel"); titel.i.maxLength = 200; titel.i.placeholder = "Waar werk je aan?"; body.appendChild(titel.f);
        var tv = tijdVelden(body, datum, van, tot);
        var ac = el("div", "gl-v2-mt-pop-acties"), ok = knop("Toevoegen", "gl-v2-btn-primary", "ph-plus"), nee = knop("Annuleren", "gl-v2-btn-text");
        ac.appendChild(ok); ac.appendChild(nee); body.appendChild(ac);
        function bewaar() {
            var t = tv.lees(); if (!t) { toast("warning", "Controleer de tijden", "Het einde moet na het begin liggen."); return; }
            if (!titel.i.value.trim()) { titel.i.focus(); return; }
            ok.disabled = true; post("NieuwBlok", { titel: titel.i.value.trim(), start: t.datum + "T" + hm(t.van), minuten: t.minuten }).then(herlaad, function () { ok.disabled = false; });
        }
        ok.addEventListener("click", bewaar); nee.addEventListener("click", sluitPop);
        titel.i.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); bewaar(); } });
        plaats(rect); titel.i.focus();
    }
    // Een volgende actie inplannen via een tik of klik (werkt ook op een gsm, waar slepen tussen lijst en dag lastig is).
    function openInplannen(card) {
        var body = maakPop(getComputedStyle(card.querySelector(".gl-v2-mt-bar") || card).backgroundColor, "INPLANNEN");
        body.appendChild(el("div", "gl-v2-mt-pop-titel", card.querySelector(".t").textContent));
        var datum = (week && !week.querySelector(".gl-v2-mt-daycol.is-today")) ? week.querySelector(".gl-v2-mt-daycol").dataset.datum : (dag ? dag.dataset.datum : vandaagIso());
        var min = parseInt(card.dataset.min, 10) || 30, van = volgendHalfuur();
        var tv = tijdVelden(body, datum, van, van + min);
        var ac = el("div", "gl-v2-mt-pop-acties"), ok = knop("Inplannen", "gl-v2-btn-primary", "ph-calendar-plus"), nee = knop("Annuleren", "gl-v2-btn-text");
        var weg = knop("Verwijderen", "gl-v2-btn-text gl-v2-mt-verwijder", "ph-trash");
        ac.appendChild(ok); ac.appendChild(nee); ac.appendChild(el("span", "sp")); ac.appendChild(weg); body.appendChild(ac);
        weg.addEventListener("click", function () {
            var naam = card.querySelector(".t").textContent;
            bevestig({ title: "Deze actie verwijderen?", description: "“" + naam + "” verdwijnt uit je lijsten. De tijd die je er al aan werkte, blijft bewaard.", confirmLabel: "Verwijderen", cancelLabel: "Annuleren", tone: "danger", icon: "ph-trash" }).then(function (ja) {
                if (!ja) return; sluitPop();
                verwijderBlokken([card], function () { return post("TaakVerwijder", { id: card.dataset.id }); });
            });
        });
        ok.addEventListener("click", function () {
            var t = tv.lees(); if (!t) { toast("warning", "Controleer de tijden", "Het einde moet na het begin liggen."); return; }
            ok.disabled = true; post("PlanWijzig", { id: card.dataset.id, start: t.datum + "T" + hm(t.van), minuten: t.minuten }).then(herlaad, function () { ok.disabled = false; });
        });
        nee.addEventListener("click", sluitPop);
        plaats(card.getBoundingClientRect());
    }
    var nieuwKnop = $("mt-nieuw");
    if (nieuwKnop) nieuwKnop.addEventListener("click", function (e) {
        e.stopPropagation();
        var datum = (week && !week.querySelector(".gl-v2-mt-daycol.is-today")) ? week.querySelector(".gl-v2-mt-daycol").dataset.datum : (dag ? dag.dataset.datum : vandaagIso());
        var van = volgendHalfuur();
        openNieuw(datum, van, van + 30, nieuwKnop.getBoundingClientRect());
    });

    // Nieuwe afspraak (ontwerp "CRM Nieuwe afspraak"): komt in de Outlook-agenda; met een project is ze meteen gekoppeld, zonder project is ze persoonlijk. Enkel de gekozen deelnemers krijgen een uitnodiging.
    // Velden zijn de gedeelde componenten: titel-variant, .gl-v2-pill, _GlV2Combo (project), _DateField, _TimeField (van en tot), .gl-v2-segmented, _SuggestField, _PersonPicker.
    var nieuwAfspraakModal = $("mt-afspraak-nieuw-modal"), nieuwAfspraakKnop = $("mt-afspraak-nieuw");
    if (nieuwAfspraakModal && nieuwAfspraakKnop) (function () {
        var T = window.GlV2Time, vanVeld = T.byId("mt-an-van"), totVeld = T.byId("mt-an-tot");
        var titel = $("mt-an-titel"), projectHidden = $("mt-an-project"), projectCombo = projectHidden.closest("[data-gl-v2-combo]");
        var plaats = $("mt-an-plaats"), plaatsSuggest = plaats.closest("[data-gl-v2-suggest]"), deelnemersRoot = $("mt-an-deelnemers").closest("[data-gl-v2-people]");
        var kantoor = nieuwAfspraakModal.dataset.kantoor || "", kantoorRoute = nieuwAfspraakModal.dataset.kantoorRoute || "", adressen = {};
        try { adressen = JSON.parse(nieuwAfspraakModal.dataset.adressen || "{}"); } catch (e) { adressen = {}; }
        var soortKnoppen = $$("#mt-an-soorten [data-soort]"), duurKnoppen = $$("#mt-an-duren [data-duur]"), locKnoppen = $$("#mt-an-plaatsen [data-loc]"), herinneringKnoppen = $$("#mt-an-herinnering [data-min]");
        var state = { soort: null, project: "", projectNaam: "", loc: "office", herinnering: 15, titelAuto: "", bezet: [], bezetDatum: "", dur: 60, vanaf: "office", marge: 10, reis: 30, reisBron: "schatting" };

        // Vertrekblok: bij een adres (project, kantoor, ander adres) rekent CPM de reistijd met de auto uit (Google Routes, zonder verkeer) tussen "vanaf" (kantoor of de vorige afspraak van die dag) en de plaats.
        // Vertrek = begin - reistijd - marge. De reistijd kan je met de knoppen bijstellen; lukt de berekening niet, dan staat er 30 minuten. Aan: de reistijd komt als apart bezet blok in je agenda.
        var vertrekKaart = $("mt-an-vertrek"), vertrekKnop = $("mt-an-vertrek-knop"), reisTimer = 0, reisSleutel = "";
        var vorigeAfspraak = function () {
            var a = vanMin(), best = null;
            state.bezet.forEach(function (b) { if (b.plaats && b.tot <= (a == null ? 1440 : a) && (!best || b.tot > best.tot)) best = b; });
            return best;
        };
        var vanafAdres = function () { var v = vorigeAfspraak(); return state.vanaf === "vorige" && v ? v.plaats : kantoor; };
        var bestemming = function () { return state.loc === "proj" || state.loc === "office" || state.loc === "other" ? plaats.value.trim() : ""; };
        var herinneringMinuten = function () { return state.herinnering === "vertrek" ? state.reis + state.marge : state.herinnering; };
        var reistijdBlok = function () { return !vertrekKaart.hidden && $("mt-an-blok").checked ? state.reis + state.marge : 0; };
        function toonVertrek() {
            var a = vanMin(), naar = bestemming(), van = vanafAdres();
            var toon = !!naar && !!van && naar.toLowerCase() !== van.toLowerCase() && a != null;
            vertrekKaart.hidden = !toon;
            vertrekKnop.hidden = !toon;
            if (!toon) { if (state.herinnering === "vertrek") zetHerinnering(15); return; }
            var vorige = vorigeAfspraak();
            $$("#mt-an-vanaf [data-vanaf]").forEach(function (b) { if (b.dataset.vanaf === "vorige") b.hidden = !vorige; b.classList.toggle("is-active", b.dataset.vanaf === state.vanaf); });
            $("mt-an-vanaf-adres").textContent = van;
            var dep = Math.max(0, a - state.reis - state.marge);
            $("mt-an-vertrek-tijd").textContent = hm(dep);
            $("mt-an-reis-tekst").textContent = state.reis + " min";
            $("mt-an-reis-bron").textContent = state.reisBron === "google" ? "geschat via Google Maps" : state.reisBron === "zelf" ? "zelf ingesteld" : state.reisBron === "bezig" ? "reistijd berekenen \u2026" : "schatting, stel zelf bij";
            $("mt-an-blok-tekst").textContent = "Reistijd " + hm(dep) + "\u2013" + hm(a) + " wordt in je agenda geblokkeerd";
            var botsing = state.bezet.filter(function (b) { return b.tot > dep && b.van < dep; })[0];
            $("mt-an-vertrek-conflict").hidden = !botsing;
            if (botsing) $("mt-an-vertrek-conflict-tekst").textContent = "Om " + hm(dep) + " te vertrekken moet \u201c" + botsing.titel + "\u201d (tot " + hm(botsing.tot) + ") eerder stoppen.";
            $$("#mt-an-marge [data-marge]").forEach(function (b) { b.classList.toggle("is-active", parseInt(b.dataset.marge, 10) === state.marge); });
        }
        // Rekent de reistijd opnieuw als vertrekpunt of bestemming veranderde (met een korte wachttijd tijdens het typen). Een zelf ingestelde reistijd blijft staan zolang de route gelijk blijft.
        function planReis() {
            window.clearTimeout(reisTimer);
            reisTimer = window.setTimeout(function () {
                var naar = bestemming(), van = vanafAdres(), sleutel = van + "|" + naar;
                toonVertrek();
                if (!naar || !van || sleutel === reisSleutel || naar.toLowerCase() === van.toLowerCase()) return;
                reisSleutel = sleutel; state.reisBron = "bezig"; toonVertrek();
                // Voor de route telt enkel het adres: het kantoor zonder bedrijfsnaam, en "naam — adres" uit de suggesties als "naam, adres".
                var routeAdres = function (a) { return a === kantoor && kantoorRoute ? kantoorRoute : a.replace(/\s+—\s+/g, ", "); };
                post("AfspraakReis", { van: routeAdres(van), naar: routeAdres(naar) }).then(function (j) {
                    if (sleutel !== reisSleutel) return;
                    if (j.minuten != null) { state.reis = j.minuten; state.reisBron = "google"; } else { state.reis = 30; state.reisBron = "schatting"; }
                    toonVertrek();
                }, function () { if (sleutel === reisSleutel) { state.reis = 30; state.reisBron = "schatting"; toonVertrek(); } });
            }, 450);
        }
        function zetHerinnering(w) {
            state.herinnering = w;
            herinneringKnoppen.forEach(function (x) { x.classList.toggle("is-active", x.dataset.min === String(w)); });
        }
        var kortDuur = function (d) { return d < 60 ? d + " min" : Math.floor(d / 60) + "u" + (d % 60 ? pad(d % 60) : ""); };
        var vanMin = function () { return T.getMinutes(vanVeld); }, totMin = function () { return T.getMinutes(totVeld); };
        var datumIso = function () { return $("mt-an-datum").value; };

        // Wat er in de lijsten van Van en Tot staat, de duurknoppen en de overlapmelding volgen altijd uit datum, begin en einde.
        function ververs() {
            var a = vanMin(), z = totMin(), d = a != null && z != null && z > a ? z - a : null;
            var inBezet = function (m) { return state.bezet.filter(function (b) { return m >= b.van && m < b.tot; })[0]; };
            var vanLijst = [];
            for (var m = 7 * 60; m <= 20 * 60; m += 15) { var b = inBezet(m); vanLijst.push({ minutes: m, note: b ? b.titel : "", busy: !!b }); }
            T.setItems(vanVeld, vanLijst);
            var totLijst = [];
            if (a != null) for (var x = 15; x <= 480 && a + x <= 1439; x += x < 60 ? 15 : 30) totLijst.push({ minutes: a + x, note: kortDuur(x) });
            T.setItems(totVeld, totLijst);
            T.setNote(totVeld, d ? kortDuur(d) : "");
            duurKnoppen.forEach(function (b) { b.classList.toggle("is-active", d === parseInt(b.dataset.duur, 10)); });
            var botsing = d ? state.bezet.filter(function (b) { return a < b.tot && z > b.van; })[0] : null;
            $("mt-an-conflict").hidden = !botsing;
            $("mt-an-vrij").hidden = !!botsing || !d;
            if (botsing) $("mt-an-conflict-tekst").textContent = "Overlapt met “" + botsing.titel + "” " + hm(botsing.van) + "–" + hm(botsing.tot);
            $("mt-an-opslaan").disabled = !titel.value.trim();
            planReis();
        }
        function zetDuur(d) {
            state.dur = d;
            var a = vanMin(); if (a == null) return;
            T.setValue(totVeld, Math.min(a + d, 1439));
        }
        function laadBezet() {
            var dag = datumIso();
            if (!dag) { state.bezet = []; ververs(); return; }
            if (dag === state.bezetDatum) return;
            state.bezetDatum = dag; state.bezet = []; ververs();
            post("AfspraakDag", { datum: dag }).then(function (j) { if (dag === datumIso()) { state.bezet = j.bezet || []; ververs(); } }, function () { });
        }

        // ── plaats ─────────────────────────────────────────────────────────────────────────────
        function zetLoc(loc, behoudTekst) {
            if (loc === "proj" && !state.project) loc = "office";
            state.loc = loc;
            locKnoppen.forEach(function (b) { b.classList.toggle("is-active", b.dataset.loc === loc); if (b.dataset.loc === "proj") b.hidden = !state.project; });
            var tekst = loc === "proj" ? (adressen[state.project] || "") : loc === "office" ? kantoor : "";
            if (!behoudTekst) plaats.value = tekst;
            plaats.disabled = loc === "online";
            plaats.placeholder = loc === "phone" ? "Telefoonnummer (optioneel)" : loc === "other" ? "Zoek een adres, klant of leverancier — of typ een adres" : loc === "online" ? "Teams-link wordt automatisch toegevoegd" : "Adres of ruimte";
            window.GlV2Suggest.setEnabled(plaatsSuggest, loc === "other");
            planReis();
            $("mt-an-plaats-help").hidden = loc !== "other";
        }
        function plaatsWaarde() {
            var v = plaats.value.trim();
            if (state.loc === "online") return "";
            if (state.loc === "phone") return v ? "Telefonisch: " + v : "Telefonisch";
            return v;
        }

        // ── titel en soort ─────────────────────────────────────────────────────────────────────
        function autoTitel() { return state.soort ? state.soort + (state.projectNaam ? " " + state.projectNaam : "") : ""; }
        function zetSoort(soort, knop) {
            state.soort = soort;
            soortKnoppen.forEach(function (b) { b.classList.toggle("is-active", b === knop); b.setAttribute("aria-pressed", b === knop ? "true" : "false"); });
            if (!soort) return;
            zetDuur(parseInt(knop.dataset.duur, 10));
            zetLoc(knop.dataset.loc);
            if (!titel.value.trim() || titel.value === state.titelAuto) { state.titelAuto = autoTitel(); titel.value = state.titelAuto; }
        }
        soortKnoppen.forEach(function (b) { b.setAttribute("aria-pressed", "false"); b.addEventListener("click", function () { zetSoort(b.classList.contains("is-active") ? null : b.dataset.soort, b.classList.contains("is-active") ? null : b); ververs(); }); });
        titel.addEventListener("input", ververs);

        // ── project ────────────────────────────────────────────────────────────────────────────
        projectCombo.addEventListener("gl-v2:combo-select", function (e) {
            var d = e.detail || {};
            state.project = d.id || ""; state.projectNaam = d.id ? (d.text || "") : "";
            if (state.soort && (!titel.value.trim() || titel.value === state.titelAuto)) { state.titelAuto = autoTitel(); titel.value = state.titelAuto; }
            zetLoc(state.loc, state.loc !== "proj");   // bij Projectadres volgt het adres het gekozen project; anders blijft de tekst staan
            window.GlV2People.refresh(deelnemersRoot);
            ververs();
        });

        // ── datum en tijd ──────────────────────────────────────────────────────────────────────
        $("mt-an-datum").addEventListener("change", laadBezet);
        vanVeld.addEventListener("change", function () {
            // Het einde schuift mee zodat de duur gelijk blijft.
            var a = vanMin();
            if (a != null) T.setValue(totVeld, Math.min(a + state.dur, 1439), true);
            ververs();
        });
        totVeld.addEventListener("change", function () { var a = vanMin(), z = totMin(); if (a != null && z != null && z > a) state.dur = z - a; ververs(); });
        duurKnoppen.forEach(function (b) { b.addEventListener("click", function () { zetDuur(parseInt(b.dataset.duur, 10)); ververs(); }); });

        // ── plaats, deelnemers, herinnering, notitie ───────────────────────────────────────────
        locKnoppen.forEach(function (b) { b.addEventListener("click", function () { zetLoc(b.dataset.loc); }); });
        deelnemersRoot.addEventListener("gl-v2:people-change", function (e) {
            var n = (e.detail && e.detail.items ? e.detail.items : []).length;
            $("mt-an-uitnodiging-tekst").textContent = n ? n + (n === 1 ? " persoon krijgt" : " personen krijgen") + " een uitnodiging per mail, met een agenda-bestand voor Outlook of Google Agenda." : "Er gaat geen uitnodiging uit. De afspraak staat enkel in je eigen agenda.";
            $("mt-an-opslaan").textContent = n ? "Maken en uitnodigen" : "Afspraak maken";
        });
        herinneringKnoppen.forEach(function (b) { b.addEventListener("click", function () { zetHerinnering(b.dataset.min === "vertrek" ? "vertrek" : parseInt(b.dataset.min, 10)); }); });
        plaats.addEventListener("input", planReis); plaats.addEventListener("change", planReis);
        $("mt-an-reis-min").addEventListener("click", function () { state.reis = Math.max(0, state.reis - 5); state.reisBron = "zelf"; toonVertrek(); });
        $("mt-an-reis-plus").addEventListener("click", function () { state.reis = Math.min(600, state.reis + 5); state.reisBron = "zelf"; toonVertrek(); });
        $$("#mt-an-vanaf [data-vanaf]").forEach(function (b) { b.addEventListener("click", function () { state.vanaf = b.dataset.vanaf; state.reisBron = "schatting"; reisSleutel = ""; planReis(); }); });
        $$("#mt-an-marge [data-marge]").forEach(function (b) { b.addEventListener("click", function () { state.marge = parseInt(b.dataset.marge, 10); toonVertrek(); }); });
        $("mt-an-notitie-knop").addEventListener("click", function () { $("mt-an-notitie-rij").hidden = false; this.hidden = true; $("mt-an-notitie").focus(); });

        // ── openen ─────────────────────────────────────────────────────────────────────────────
        nieuwAfspraakKnop.addEventListener("click", function (e) {
            e.stopPropagation(); sluitPop();
            var datum = (week && !week.querySelector(".gl-v2-mt-daycol.is-today")) ? week.querySelector(".gl-v2-mt-daycol").dataset.datum : (dag ? dag.dataset.datum : vandaagIso());
            var van = Math.min(volgendHalfuur(), 23 * 60 + 15);
            state.soort = null; state.project = ""; state.projectNaam = ""; state.titelAuto = ""; state.bezet = []; state.bezetDatum = ""; state.herinnering = 15; state.dur = 60; state.vanaf = "office"; state.marge = 10; state.reis = 30; state.reisBron = "schatting"; reisSleutel = "";
            $("mt-an-blok").checked = true;
            titel.value = ""; $("mt-an-notitie").value = ""; $("mt-an-notitie-rij").hidden = true; $("mt-an-notitie-knop").hidden = false;
            soortKnoppen.forEach(function (b) { b.classList.remove("is-active"); b.setAttribute("aria-pressed", "false"); });
            herinneringKnoppen.forEach(function (b) { b.classList.toggle("is-active", b.dataset.min === "15"); });
            window.GlV2DatePicker.setValue("mt-an-datum", datum);
            window.GlV2Combo.setValue(projectCombo, "", "");
            window.GlV2People.clear(deelnemersRoot);
            T.setValue(vanVeld, van, true); T.setValue(totVeld, Math.min(van + 60, 1439), true);
            zetLoc("office");
            ververs(); laadBezet();
            if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(nieuwAfspraakModal).show();
        });
        nieuwAfspraakModal.addEventListener("shown.bs.modal", function () { titel.focus(); });
        nieuwAfspraakModal.addEventListener("keydown", function (e) { if ((e.ctrlKey || e.metaKey) && e.key === "Enter") { e.preventDefault(); nieuwAfspraakModal.querySelector("form").requestSubmit(); } });

        // ── opslaan ────────────────────────────────────────────────────────────────────────────
        nieuwAfspraakModal.querySelector("form").addEventListener("submit", function (e) {
            e.preventDefault();
            var knop = $("mt-an-opslaan"), a = vanMin(), z = totMin();
            if (!titel.value.trim()) { toast("warning", "Titel ontbreekt", "Geef de afspraak een titel."); return; }
            if (!datumIso()) { toast("warning", "Datum ontbreekt", "Kies een datum voor de afspraak."); return; }
            if (a == null || z == null || z <= a) { toast("warning", "Controleer de tijden", "Het einde moet na het begin liggen."); return; }
            var mensen = window.GlV2People.get(deelnemersRoot);
            knop.disabled = true;
            post("AfspraakMaak", {
                projectId: state.project, titel: titel.value.trim(), start: datumIso() + "T" + hm(a), minuten: z - a, plaats: plaatsWaarde(),
                online: state.loc === "online" ? "true" : "", deelnemers: mensen.map(function (m) { return m.id; }).join(","), herinnering: herinneringMinuten(), reistijd: reistijdBlok(), notitie: $("mt-an-notitie").value.trim()
            }).then(function (j) {
                if (!j.waarschuwing) return herlaad();
                toast("warning", "Afspraak gemaakt", j.waarschuwing); setTimeout(herlaad, 2500);
            }, function () { knop.disabled = false; });
        });
    })();

    // Klik of tik op een blok of een volgende actie opent de popover; Enter of spatie doet hetzelfde met het toetsenbord.
    document.addEventListener("click", function (e) {
        if (zojuist) return;
        var b = e.target.closest && e.target.closest(".gl-v2-mt-block.is-planned, .gl-v2-mt-block.is-meet");
        var kaart = e.target.closest && e.target.closest(".gl-v2-mt-act");
        if (b && !e.target.closest("a")) { e.stopPropagation(); openBlok(b); }
        else if (kaart && (dag || week) && !e.target.closest("button, a")) { e.stopPropagation(); openInplannen(kaart); }
        else if (pop && !e.target.closest(".gl-v2-mt-pop")) sluitPop();
    }, true);
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") sluitPop();
        var b = e.target.classList && e.target.classList.contains("gl-v2-mt-block") ? e.target : null;
        if (b && (e.key === "Enter" || e.key === " ") && !b.classList.contains("is-worked")) { e.preventDefault(); openBlok(b); }
    });

    // ── Nieuw blok maken: slepen over een leeg stuk (muis meteen, aanraking na ingedrukt houden) of dubbelklikken ──
    function maakSelectie(baan, datumFn) {
        baan.addEventListener("pointerdown", function (e) {
            if (e.target !== baan || (e.pointerType === "mouse" && e.button !== 0)) return;
            var aanraking = e.pointerType !== "mouse", r = baan.getBoundingClientRect(), y0 = e.clientY - r.top, x0 = e.clientX, yAbs0 = e.clientY;
            var s = snap(minVanY(y0)), en = s + 15, gesleept = false, actief = !aanraking, sel = null, timer = null;
            if (!aanraking) { e.preventDefault(); sluitPop(); }
            function toonSel() { if (!sel) { sel = el("div", "gl-v2-mt-sel"); baan.appendChild(sel); } sel.style.top = yVanMin(Math.min(s, en)) + "px"; sel.style.height = Math.abs(en - s) / 60 * PX + "px"; }
            function blokkeer(ev) { if (actief) ev.preventDefault(); }
            function ruim() {
                clearTimeout(timer); document.removeEventListener("pointermove", beweeg); document.removeEventListener("pointerup", los); document.removeEventListener("pointercancel", annuleer);
                document.removeEventListener("touchmove", blokkeer);
            }
            function annuleer() { ruim(); if (sel) sel.remove(); }
            if (aanraking) timer = setTimeout(function () { actief = true; sluitPop(); if (navigator.vibrate) navigator.vibrate(15); toonSel(); document.addEventListener("touchmove", blokkeer, { passive: false }); }, 350);
            else toonSel();
            function beweeg(ev) {
                if (!actief) { if (Math.abs(ev.clientX - x0) > 8 || Math.abs(ev.clientY - yAbs0) > 8) annuleer(); return; }
                ev.preventDefault();
                var m = snap(minVanY(ev.clientY - r.top)); if (Math.abs(ev.clientY - r.top - y0) > 4) gesleept = true;
                en = m >= s ? Math.max(m, s + 15) : m; toonSel();
            }
            function los() {
                ruim();
                if (!actief) return;
                var van = Math.max(UUR * 60, Math.min(s, en)), tot = Math.min(23 * 60 + 45, Math.max(s, en));
                if (!gesleept || tot - van < 15) { if (sel) sel.remove(); return; }
                sluit(); openNieuw(datumFn(), van, tot, sel.getBoundingClientRect());
            }
            document.addEventListener("pointermove", beweeg); document.addEventListener("pointerup", los); document.addEventListener("pointercancel", annuleer);
        });
        baan.addEventListener("dblclick", function (e) {
            if (e.target !== baan) return;
            var y = e.clientY - baan.getBoundingClientRect().top, van = Math.max(UUR * 60, Math.floor(minVanY(y) / 30) * 30);
            openNieuw(datumFn(), van, van + 30, { left: e.clientX, right: e.clientX + 10, top: e.clientY });
        });
    }
    if (dag && lane) maakSelectie(lane, function () { return dag.dataset.datum; });
    if (week) $$(".gl-v2-mt-daylane", week).forEach(function (l) { maakSelectie(l, function () { return l.closest(".gl-v2-mt-daycol").dataset.datum; }); });

    // ── Snel starten ─────────────────────────────────────────────────────────────────────────────
    var qs = $("mt-qs"), qsInput = $("mt-qs-input"), qsList = $("mt-qs-list");
    var qsState = { projecten: [], taken: [], recent: true, sel: 0, timer: null };
    function parse(v) {
        var proj = null, ctx = "", back = 0, m;
        if ((m = /(^|\s)#([^\s#@+]+)/.exec(v))) proj = m[2];
        if ((m = /(^|\s)(@[^\s#@+]+)/.exec(v))) ctx = m[2];
        if ((m = /(^|\s)\+(\d+)\s*(m|min|u|h)?(?=\s|$)/i.exec(v))) back = parseInt(m[2], 10) * (/^(u|h)$/i.test(m[3] || "") ? 60 : 1);
        var titel = v.replace(/(^|\s)#[^\s#@+]+/g, " ").replace(/(^|\s)@[^\s#@+]+/g, " ").replace(/(^|\s)\+\d+\s*(m|min|u|h)?(?=\s|$)/gi, " ").replace(/\s+/g, " ").trim();
        return { titel: titel, proj: proj, ctx: ctx, back: back };
    }
    function vindProject(tag) {
        if (!tag) return null; var t = tag.toLowerCase().replace(/[\s-]/g, "");
        return qsState.projecten.filter(function (p) { var n = p.naam.toLowerCase().replace(/[\s-]/g, ""); return n.indexOf(t) === 0; })[0]
            || qsState.projecten.filter(function (p) { return p.naam.toLowerCase().replace(/[\s-]/g, "").indexOf(t) >= 0; })[0] || null;
    }
    function el(tag, cls, tekst) { var e = document.createElement(tag); if (cls) e.className = cls; if (tekst !== undefined) e.textContent = tekst; return e; }
    function tekenQs() {
        var p = parse(qsInput.value), proj = vindProject(p.proj), rijen = [];
        qsList.textContent = "";
        if (p.titel) {
            var r = el("button", "gl-v2-mt-qs-row is-start"); r.type = "button";
            r.appendChild(el("span", "k", "START")); r.appendChild(el("span", "t", p.titel));
            if (proj) r.appendChild(el("span", "chip", proj.naam)); else if (p.proj) r.appendChild(el("span", "chip is-warn", "#" + p.proj + "?"));
            if (p.ctx) r.appendChild(el("span", "chip", p.ctx));
            if (p.back) r.appendChild(el("span", "chip", "al " + p.back + " min bezig"));
            r.addEventListener("click", function () { startNieuw(); });
            rijen.push({ el: r, go: startNieuw });
        }
        if (qsState.taken.length) {
            qsList.appendChild(el("div", "gl-v2-mt-qs-kop", p.titel ? "BESTAANDE TAKEN" : "RECENT"));
            qsState.taken.forEach(function (t) {
                var b = el("button", "gl-v2-mt-qs-row"); b.type = "button";
                var d = el("i"); d.style.background = t.kleur; b.appendChild(d);
                var tx = el("span", "t", t.titel); b.appendChild(tx);
                if (t.project) b.appendChild(el("span", "chip", t.project));
                var go = function () { post("TimerStart", { taakId: t.id, terugMinuten: p.back }).then(herlaad, mislukt); };
                b.addEventListener("click", go); rijen.push({ el: b, go: go });
            });
        }
        rijen.forEach(function (r) { qsList.appendChild(r.el); });
        qsState.rijen = rijen; qsState.sel = Math.min(qsState.sel, Math.max(0, rijen.length - 1));
        markeer();
    }
    function markeer() { (qsState.rijen || []).forEach(function (r, i) { r.el.classList.toggle("is-sel", i === qsState.sel); }); }
    function startNieuw() {
        var p = parse(qsInput.value); if (!p.titel) return;
        var proj = vindProject(p.proj);
        post("TimerStart", { titel: p.titel, projectId: proj ? proj.id : "", context: p.ctx, terugMinuten: p.back }).then(herlaad, mislukt);
    }
    function haalQs() {
        var q = parse(qsInput.value).titel;
        fetch(base + "Snel?q=" + encodeURIComponent(q), { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (j) {
            qsState.projecten = j.projecten || []; qsState.taken = j.taken || []; qsState.recent = j.recent; tekenQs();
        });
    }
    function openQs() { if (!qs) return; qs.hidden = false; qsInput.value = ""; qsState.sel = 0; haalQs(); setTimeout(function () { qsInput.focus(); }, 0); }
    function sluitQs() { if (qs) qs.hidden = true; }
    if (qs) {
        var sb = $("mt-start-btn"); if (sb) sb.addEventListener("click", openQs);
        $$("[data-mt-qs-close]").forEach(function (x) { x.addEventListener("click", sluitQs); });
        qsInput.addEventListener("input", function () { clearTimeout(qsState.timer); tekenQs(); qsState.timer = setTimeout(haalQs, 200); });
        qsInput.addEventListener("keydown", function (e) {
            if (e.key === "Escape") sluitQs();
            else if (e.key === "ArrowDown") { e.preventDefault(); qsState.sel = Math.min(qsState.sel + 1, (qsState.rijen || []).length - 1); markeer(); }
            else if (e.key === "ArrowUp") { e.preventDefault(); qsState.sel = Math.max(qsState.sel - 1, 0); markeer(); }
            else if (e.key === "Enter") { e.preventDefault(); var r = (qsState.rijen || [])[qsState.sel]; if (r) r.go(); }
        });
        document.addEventListener("keydown", function (e) {
            if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) { e.preventDefault(); qs.hidden ? openQs() : sluitQs(); }
            else if (e.key === "Escape" && !qs.hidden) sluitQs();
        });
    }

    // ── Teller bij de Inbox werkt zichzelf bij (elke minuut, enkel als het tabblad zichtbaar is; geen melding, geen geluid, de lijst blijft staan) ──
    var tellers = $$("[data-mt-inbox-count]");
    if (tellers.length) setInterval(function () {
        if (document.hidden) return;
        fetch(base + "InboxAantal", { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (j) {
            if (!j || !j.ok) return;
            tellers.forEach(function (t) { t.textContent = j.aantal; t.hidden = !(j.aantal > 0); });
        }, function () { /* netwerkfout: volgende minuut opnieuw */ });
    }, 60000);

    // ── Inbox: acties op een mail (koppelen, taak maken, afzender negeren, contact toevoegen) ──
    var koppelModal = $("mt-mail-koppel-modal"), koppelId = 0;
    function mailActie(actie, id, data, bericht) {
        var d = { id: id }; Object.keys(data || {}).forEach(function (k) { d[k] = data[k]; });
        return post(actie, d).then(function () { if (bericht) toast("success", bericht); herlaad(); }, mislukt);
    }
    $$("[data-mail-koppel]").forEach(function (b) { b.addEventListener("click", function () { mailActie("MailKoppel", b.dataset.mailKoppel, { projectId: b.dataset.project }); }); });
    $$("[data-mail-taak]").forEach(function (b) { b.addEventListener("click", function () { mailActie("MailTaak", b.dataset.mailTaak, {}, "Er staat een item in je inbox."); }); });
    $$("[data-mail-negeer]").forEach(function (b) { b.addEventListener("click", function () { mailActie("MailNegeer", b.dataset.mailNegeer, {}, "Mails van " + (b.dataset.afzender || "deze afzender") + " komen niet meer binnen."); }); });
    $$("[data-mail-contact]").forEach(function (b) {
        b.addEventListener("click", function () {
            post("MailContact", { id: b.dataset.mailContact }).then(function (j) { toast("success", j.bericht || "Toegevoegd."); }, mislukt);
        });
    });
    $$("[data-mail-open-koppel]").forEach(function (b) {
        b.addEventListener("click", function () {
            koppelId = b.dataset.mailOpenKoppel;
            var o = $("mt-mail-koppel-onderwerp"); if (o) o.textContent = b.dataset.onderwerp || "";
            var vorige = koppelModal && koppelModal.querySelector("[name=projectId]"); if (vorige) vorige.value = "";   // geen keuze van de vorige keer
            if (koppelModal && window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(koppelModal).show();
        });
    });
    if (koppelModal) koppelModal.querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        var veld = koppelModal.querySelector("[name=projectId]");
        if (!veld || !veld.value) { toast("danger", "Kies eerst een project."); return; }
        mailActie("MailKoppel", koppelId, { projectId: veld.value });
    });

    // ── Inbox: mails op afgehandeld zetten (blijven bij het project) ──────────────────────────
    $$("[data-mail-klaar]").forEach(function (b) {
        b.addEventListener("click", function () { post("MailAfgehandeld", { id: b.dataset.mailKlaar }).then(herlaad, mislukt); });
    });

    // ── Inbox verwerken ──────────────────────────────────────────────────────────────────────────
    var inbox = $("mt-inbox");
    if (inbox) {
        var lijst = $("mt-inbox-list"), panel = $("mt-verwerk"), leeg = $("mt-inbox-leeg"), ok = $("mt-v-ok");
        var actie = "plan", ctx = "";
        function actief() { return $$(".gl-v2-mt-in", lijst).filter(function (b) { return b.classList.contains("is-active"); })[0]; }
        function toon() {
            $$("[data-v]", inbox).forEach(function (f) { f.hidden = f.dataset.v.split(" ").indexOf(actie) < 0; });
            var knop = $$("#mt-gtd button").filter(function (b) { return b.dataset.actie === actie; })[0];
            ok.textContent = knop.dataset.knop;
        }
        function kies(b) {
            $$(".gl-v2-mt-in", lijst).forEach(function (x) { x.classList.toggle("is-active", x === b); });
            if (!b) { panel.hidden = true; leeg.hidden = false; return; }
            $("mt-v-titel").textContent = b.dataset.titel; $("mt-v-meta").textContent = b.dataset.bron + " · " + (b.dataset.meta || "vastgelegd");
        }
        lijst.addEventListener("click", function (e) { var b = e.target.closest(".gl-v2-mt-in"); if (b) kies(b); });
        $$("#mt-gtd button").forEach(function (b) {
            b.addEventListener("click", function () {
                actie = b.dataset.actie;
                $$("#mt-gtd button").forEach(function (x) { var on = x === b; x.classList.toggle("is-active", on); x.setAttribute("aria-checked", on ? "true" : "false"); });
                toon();
            });
        });
        $$("#mt-v-context button").forEach(function (b) {
            b.addEventListener("click", function () { ctx = b.dataset.ctx; $$("#mt-v-context button").forEach(function (x) { x.classList.toggle("is-active", x === b); }); });
        });
        ok.addEventListener("click", function () {
            var b = actief(); if (!b) return;
            var data = { id: b.dataset.id, actie: actie, projectId: $("mt-v-project").value, context: ctx, schatting: $("mt-v-schatting").value };
            if (actie === "plan") {
                if (!$("mt-v-datum").value || !$("mt-v-uur").value) { toast("warning", "Kies wanneer", "Vul een datum en een uur in."); return; }
                data.wanneer = $("mt-v-datum").value + "T" + $("mt-v-uur").value;
            }
            if (actie === "del") { data.wachtOp = $("mt-v-wachtop").value; data.wachtOpDatum = $("mt-v-wachtdatum").value; }
            ok.disabled = true;
            post("Verwerk", data).then(function () {
                var volgende = b.nextElementSibling || b.previousElementSibling;
                b.remove(); inboxBadge(inboxAantal() - 1);
                kies(volgende && volgende.classList.contains("gl-v2-mt-in") ? volgende : null);
                ok.disabled = false; toast("success", "Verwerkt", "Het item is van je inbox af.");
            }, function () { ok.disabled = false; });
        });
        toon();
    }

    // ── Uren goedkeuren (ticket #9): voorstellen uit afspraken op regieprojecten worden regie-uren; de server leest ze zelf opnieuw uit de agenda ──
    var urenKnop = $("mt-uren-goedkeur");
    if (urenKnop) {
        var telUren = function () { var n = $$("[data-mt-uur]:checked").length; urenKnop.textContent = n ? "Goedkeuren (" + n + ")" : "Goedkeuren"; urenKnop.disabled = !n; };
        $$("[data-mt-uur]").forEach(function (cb) { cb.addEventListener("change", telUren); });
        telUren();
        urenKnop.addEventListener("click", function () {
            var ids = $$("[data-mt-uur]:checked").map(function (cb) { return cb.dataset.mtUur; });
            if (!ids.length) return;
            urenKnop.disabled = true;
            post("UrenGoedkeur", { datum: document.querySelector(".gl-v2-mt-overzicht").dataset.week, events: ids.join("|") }).then(function (j) {
                toast("success", j.aantal === 1 ? "1 uur goedgekeurd" : j.aantal + " uren goedgekeurd", "Je vindt ze bij Coördinatie van het project."); setTimeout(herlaad, 1200);
            }, function () { urenKnop.disabled = false; });
        });
    }
    $$("[data-mt-uur-bijwerken]").forEach(function (b) {
        b.addEventListener("click", function () { b.disabled = true; post("UrenBijwerken", { id: b.dataset.mtUurBijwerken }).then(herlaad, function () { b.disabled = false; }); });
    });

    // ── Wekelijkse review ────────────────────────────────────────────────────────────────────────
    var ov = document.querySelector(".gl-v2-mt-overzicht");
    if (ov) $$("[data-mt-review]").forEach(function (cb) {
        cb.addEventListener("change", function () {
            post("ReviewZet", { datum: ov.dataset.week, punt: cb.dataset.mtReview, aan: cb.checked }).then(function () {
                var n = $$("[data-mt-review]:checked").length, t = $("mt-review-teller"); if (t) t.textContent = n + " / " + $$("[data-mt-review]").length;
            }, function () { cb.checked = !cb.checked; });
        });
    });
})();
