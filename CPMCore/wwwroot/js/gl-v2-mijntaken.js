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

    // ── Verplaatsen met de muis (HTML5-slepen) ──
    var sleep = null;
    document.addEventListener("dragstart", function (e) {
        var el = e.target.closest && e.target.closest("[draggable=true]"); if (!el) return;
        var isBlok = el.classList.contains("gl-v2-mt-block");
        if (!isBlok && !el.dataset.id) return;
        sluitPop();
        // Bij een bestaand blok onthouden we waar je het vastpakte, zodat het niet naar de cursor springt.
        sleep = { blok: isBlok, info: isBlok ? blokInfo(el) : { kind: "taak", id: el.dataset.id, min: parseInt(el.dataset.min, 10) || 30 }, off: isBlok ? e.clientY - el.getBoundingClientRect().top : 0 };
        e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", el.dataset.id || el.dataset.event || "");
        el.classList.add("is-dragging");
    });
    document.addEventListener("dragend", function () { sleep = null; $$(".is-dragging, .is-dropping").forEach(function (x) { x.classList.remove("is-dragging", "is-dropping"); }); });
    function maakDoel(doel, datumFn) {
        doel.addEventListener("dragover", function (e) { if (!sleep) return; e.preventDefault(); e.dataTransfer.dropEffect = "move"; doel.classList.add("is-dropping"); });
        doel.addEventListener("dragleave", function (e) { if (e.target === doel) doel.classList.remove("is-dropping"); });
        doel.addEventListener("drop", function (e) {
            if (!sleep) return; e.preventDefault(); doel.classList.remove("is-dropping");
            var s = sleep; sleep = null;
            verplaats(s.info, datumFn(), minOnder(doel, e.clientY, s.off), s.info.min).then(herlaad, mislukt);
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
        var actief = false, ghost = null, timer = null;
        src.draggable = false;
        function blokkeer(ev) { if (actief) ev.preventDefault(); }
        function ruim() {
            clearTimeout(timer); document.removeEventListener("pointermove", beweeg); document.removeEventListener("pointerup", los); document.removeEventListener("pointercancel", ruim);
            document.removeEventListener("touchmove", blokkeer); if (ghost) ghost.remove(); src.classList.remove("is-dragging"); if (isBlok || src.dataset.id) src.draggable = true;
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
            else if (d) verplaats(info, d.datum, minOnder(d.el, ev.clientY, isBlok ? off : 0), info.min).then(herlaad, mislukt);
        }
        document.addEventListener("pointermove", beweeg); document.addEventListener("pointerup", los); document.addEventListener("pointercancel", ruim);
    });
    // Geen contextmenu of tekstselectie bij ingedrukt houden op een blok.
    document.addEventListener("contextmenu", function (e) { if (e.target.closest && e.target.closest(".gl-v2-mt-block, .gl-v2-mt-act") && e.pointerType !== "mouse" && "ontouchstart" in window) e.preventDefault(); });

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
            start.addEventListener("click", function () { post("TimerStart", { taakId: b.dataset.id }).then(herlaad, mislukt); });
            klaar.addEventListener("click", function () { post("Afvinken", { id: b.dataset.id, klaar: true }).then(herlaad, mislukt); });
            weg.addEventListener("click", function () { post("Ontplan", { id: b.dataset.id }).then(herlaad, mislukt); });
        } else if (b.dataset.link) {
            var a = el("a", "gl-v2-btn gl-v2-btn-secondary"); a.href = b.dataset.link; a.target = "_blank"; a.rel = "noopener";
            a.innerHTML = '<i class="ph ph-arrow-square-out" aria-hidden="true"></i>Openen in Outlook'; ac.appendChild(a);
        }
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
        ac.appendChild(ok); ac.appendChild(nee); body.appendChild(ac);
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
