// Publieke ondertekenpagina (fase 2, ONDERTEKENEN_VOORSTEL.md §5.2; opmaak design-handoff 36). Vanilla JS, geen CDN
// (Content-Security-Policy op /tekenen staat enkel 'self' toe). Praat met OndertekenenController via JSON-fetch;
// het antiforgery-token gaat als header mee (Program.cs zet HeaderName op "RequestVerificationToken" precies hiervoor).
//
// Volgorde bewust in twee stappen (feedback Niels, 2026-09-28): eerst Akkoord/Weigeren, zonder code of
// handtekeningvak in beeld. Pas na "Akkoord" verschijnt de verificatiecode (indien nodig) en het handtekeningvak;
// "Ondertekenen" blijft uitgeschakeld tot er getekend is (en, indien nodig, een code is ingevuld) — de controller
// dwingt de handtekening ook server-side af.
//
// Beweging (impeccable animate) — één geschreven moment: het VERZEGELEN. Na "Ondertekenen" lopen de vijf stappen van
// design 36b na elkaar af. Elke stap blijft minstens even lang zichtbaar (DWELL) zodat het leesbaar is, maar wacht nooit
// langer dan de echte antwoorden van de server: de stappen volgen wat er werkelijk gebeurt (code geverifieerd → handtekening
// vastgelegd → bij de laatste ondertekenaar ook verzegeld, blad toegevoegd, bevestiging verstuurd). Daarna herlaadt de pagina
// naar de server-toestand (36c) en speelt dezelfde stappen daar nog één keer snel in, zodat het geen sprong is.
// prefers-reduced-motion: geen beweging, kortere pauzes; kleur/status blijven de voortgang tonen.
(function () {
    "use strict";

    var root = document.getElementById("gl-sign");
    var card = document.getElementById("gl-tekenen-actiekaart");
    if (!root || !card) return;

    var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var DWELL = reduced ? 140 : 520;          // minimale zichtbaarheid van een stap
    var ARRIVE_STAGGER = reduced ? 0 : 110;   // inspeelsnelheid na het herladen

    var otpRequired = card.getAttribute("data-otp-required") === "1";
    var destination = card.getAttribute("data-destination") || "je e-mailadres";

    var tokenInput = card.querySelector('input[name="__RequestVerificationToken"]');
    var melding = document.getElementById("gl-tekenen-melding");
    var stap1 = document.getElementById("gl-tekenen-stap1");
    var stap2 = document.getElementById("gl-tekenen-stap2");
    var otpBlock = document.getElementById("gl-tekenen-otp");
    var codeInput = document.getElementById("gl-tekenen-code-input");
    var akkoordBtn = document.getElementById("gl-tekenen-akkoord");
    var weigerBtn = document.getElementById("gl-tekenen-weiger");
    var opnieuwCodeBtn = document.getElementById("gl-tekenen-opnieuw-code");
    var tekenBtn = document.getElementById("gl-tekenen-teken");
    var wisBtn = document.getElementById("gl-tekenen-wis");
    var canvas = document.getElementById("gl-tekenen-canvas");
    var ctx = canvas ? canvas.getContext("2d") : null;
    var hasInk = false;

    var chip = document.getElementById("gl-tekenen-status-badge");
    var result = document.getElementById("gl-sign-result");
    var banner = document.getElementById("gl-sign-banner");
    var bannerTitle = document.getElementById("gl-sign-banner-title");
    var bannerText = document.getElementById("gl-sign-banner-text");
    var bannerPath = document.getElementById("gl-sign-banner-path");
    var stepsList = document.getElementById("gl-sign-steps");
    var actions = document.getElementById("gl-sign-actions");
    var docToggle = document.getElementById("gl-sign-doc-toggle");
    var weigerModal = document.getElementById("gl-tekenen-weiger-modal");
    var weigerReden = document.getElementById("gl-tekenen-weiger-reden");
    var weigerAnnuleerBtn = document.getElementById("gl-tekenen-weiger-annuleer");
    var weigerBevestigBtn = document.getElementById("gl-tekenen-weiger-bevestig");

    var PATH_CHECK = "M3.4 8.4l3 3 6.2-6.8", PATH_CROSS = "M5 5l6 6 M11 5l-6 6", PATH_CLOCK = "M8 4.6V8l2.2 1.4";
    var CHIP = { todo: "Te ondertekenen", processing: "Wordt verwerkt", waiting: "Ondertekend door jou", done: "Volledig ondertekend", declined: "Geweigerd", error: "Niet ondertekend", closed: "Niet meer beschikbaar" };

    function wait(ms) { return new Promise(function (res) { setTimeout(res, ms); }); }

    function melding_toon(tekst, isFout) {
        if (!melding) return;
        melding.innerHTML = "";
        if (!tekst) return;
        var div = document.createElement("div");
        div.className = "gl-v2-public-notice" + (isFout ? " is-warning" : "");
        div.textContent = tekst;
        melding.appendChild(div);
    }

    function post(pad, data) {
        return fetch(pad, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            },
            body: JSON.stringify(data || {})
        }).then(function (r) { return r.json(); });
    }

    function idempotencyKey() {
        if (window.crypto && window.crypto.randomUUID) return window.crypto.randomUUID();
        return "key-" + Date.now() + "-" + Math.random().toString(16).slice(2);
    }

    // ── Toestand van de pagina (data-state stuurt chip, document en kaart via CSS) ─────────────────
    function setState(name) {
        root.setAttribute("data-state", name);
        if (chip) chip.textContent = CHIP[name] || "";
    }

    function setBanner(tone, path, title, text, animate) {
        if (!banner) return;
        banner.className = "gl-v2-sign-banner is-" + tone;
        if (bannerPath) bannerPath.setAttribute("d", path);
        if (bannerTitle) bannerTitle.textContent = title;
        if (bannerText) bannerText.textContent = text;
        if (animate) { banner.classList.remove("is-entering"); void banner.offsetWidth; banner.classList.add("is-entering"); }
    }

    function stepEl(i) { return stepsList ? stepsList.querySelector('[data-step="' + i + '"]') : null; }
    function setStep(i, s, time) {
        var el = stepEl(i);
        if (!el) return;
        el.setAttribute("data-s", s);
        var t = el.querySelector(".gl-v2-sign-step-time");
        if (t && time !== undefined) t.textContent = time;
    }
    function nowTime() {
        var d = new Date();
        function p(n) { return (n < 10 ? "0" : "") + n; }
        return p(d.getHours()) + ":" + p(d.getMinutes()) + ":" + p(d.getSeconds());
    }
    function resetSteps() {
        for (var i = 1; i <= 5; i++) setStep(i, "todo", "");
    }

    function showResult() {
        if (!result) return;
        result.hidden = false;
        result.classList.remove("is-leaving");
        if (stepsList) stepsList.hidden = false;
        if (actions) { actions.hidden = true; actions.innerHTML = ""; }
        var behavior = reduced ? "auto" : "smooth";
        try { result.scrollIntoView({ block: "nearest", behavior: behavior }); } catch (e) { /* oudere browsers */ }
    }

    // Een blok laten verdwijnen met een korte, snelle uitgang (uitgang sneller dan de ingang).
    function leave(el) {
        if (!el || el.hidden) return Promise.resolve();
        if (reduced) { el.hidden = true; return Promise.resolve(); }
        el.classList.add("is-leaving");
        return wait(180).then(function () { el.hidden = true; el.classList.remove("is-leaving"); });
    }
    function enter(el) {
        if (!el) return;
        el.hidden = false;
        el.classList.remove("is-entering");
        void el.offsetWidth;
        el.classList.add("is-entering");
    }

    // ── Handtekeningvak ──────────────────────────────────────────────────────────────────────
    // Eén klik/tik zonder te bewegen mag niet als handtekening tellen (feedback Niels, 2026-09-28):
    // hasInk wordt pas true zodra de opgetelde lengte van de getekende lijn(en) een minimum haalt.
    var MIN_INK_LENGTH = 80;
    var inkLength = 0;
    var canvasWired = false;

    function initCanvas() {
        if (!ctx) return;
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.strokeStyle = "#1f2a22";
        ctx.lineWidth = 2.4;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
    }

    function canvasPos(e) {
        var rect = canvas.getBoundingClientRect();
        var scaleX = canvas.width / rect.width;
        var scaleY = canvas.height / rect.height;
        return { x: (e.clientX - rect.left) * scaleX, y: (e.clientY - rect.top) * scaleY };
    }

    function wireCanvas() {
        if (!canvas || !ctx || canvasWired) return;
        canvasWired = true;
        initCanvas();
        var drawing = false;
        var last = null;

        canvas.addEventListener("pointerdown", function (e) {
            drawing = true;
            last = canvasPos(e);
            ctx.beginPath();
            ctx.moveTo(last.x, last.y);
        });
        canvas.addEventListener("pointermove", function (e) {
            if (!drawing) return;
            var p = canvasPos(e);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            if (last) inkLength += Math.hypot(p.x - last.x, p.y - last.y);
            last = p;
            if (!hasInk && inkLength >= MIN_INK_LENGTH) {
                hasInk = true;
                updateTekenEnabled();
            }
        });
        function stop() { drawing = false; last = null; }
        canvas.addEventListener("pointerup", stop);
        canvas.addEventListener("pointerleave", stop);
        canvas.addEventListener("pointercancel", stop);
    }

    if (wisBtn) {
        wisBtn.addEventListener("click", function () {
            hasInk = false;
            inkLength = 0;
            initCanvas();
            updateTekenEnabled();
        });
    }

    // ── Stap-logica ──────────────────────────────────────────────────────────────────────────
    function updateTekenEnabled() {
        if (!tekenBtn) return;
        var codeOk = !otpRequired || (codeInput && codeInput.value.trim().length > 0);
        tekenBtn.disabled = !(hasInk && codeOk);
    }

    function vraagCode() {
        return post("document/code", {}).then(function (r) {
            if (r.ok) melding_toon("Er is een code verstuurd naar " + (r.destinationMasked || destination) + ".", false);
            else melding_toon(r.error || "De code kon niet verstuurd worden.", true);
            return r.ok;
        }).catch(function () {
            melding_toon("Er ging iets mis bij het versturen van de code. Probeer het opnieuw.", true);
            return false;
        });
    }

    if (akkoordBtn) {
        akkoordBtn.addEventListener("click", function () {
            akkoordBtn.disabled = true;
            melding_toon(null);
            leave(stap1).then(function () {
                enter(stap2);
                wireCanvas();
                if (otpRequired) {
                    if (otpBlock) otpBlock.hidden = false;
                    vraagCode();
                    if (codeInput) codeInput.focus();
                }
                updateTekenEnabled();
            });
        });
    }

    if (opnieuwCodeBtn) {
        opnieuwCodeBtn.addEventListener("click", function () {
            opnieuwCodeBtn.disabled = true;
            vraagCode().then(function () { opnieuwCodeBtn.disabled = false; });
        });
    }

    if (codeInput) codeInput.addEventListener("input", updateTekenEnabled);

    // ── Document in- en uitklappen (na het tekenen staat het als strook; "Toon volledig" zet het terug) ──
    if (docToggle) {
        docToggle.addEventListener("click", function () {
            var open = root.getAttribute("data-doc-expanded") === "true";
            root.setAttribute("data-doc-expanded", open ? "false" : "true");
            docToggle.setAttribute("aria-expanded", open ? "false" : "true");
            docToggle.textContent = open ? "Toon volledig" : "Toon minder";
        });
    }

    // ── Weigeren: eigen gl-v2-modal (geen window.prompt, geen Bootstrap — deze pagina laadt geen gl-v2-shell). ──
    // Na een geslaagde weigering geen reload: DeclineAsync trekt de sessietoken meteen in, dus een herlaadpoging zou
    // altijd op de neutrale "sessie verlopen"-pagina uitkomen i.p.v. een echte bevestiging te tonen.
    function weigerModalOpenen() {
        if (!weigerModal) return;
        weigerModal.hidden = false;
        if (weigerReden) { weigerReden.value = ""; weigerReden.focus(); }
        document.addEventListener("keydown", weigerModalEscape);
    }
    function weigerModalSluiten() {
        if (!weigerModal) return;
        weigerModal.hidden = true;
        document.removeEventListener("keydown", weigerModalEscape);
    }
    function weigerModalEscape(e) { if (e.key === "Escape") weigerModalSluiten(); }

    if (weigerBtn) weigerBtn.addEventListener("click", weigerModalOpenen);
    if (weigerAnnuleerBtn) weigerAnnuleerBtn.addEventListener("click", weigerModalSluiten);
    if (weigerModal) {
        weigerModal.addEventListener("click", function (e) { if (e.target === weigerModal) weigerModalSluiten(); });
    }

    if (weigerBevestigBtn) {
        weigerBevestigBtn.addEventListener("click", function () {
            var reason = weigerReden ? weigerReden.value.trim() : "";
            if (!reason) { melding_toon("Geef een reden op.", true); if (weigerReden) weigerReden.focus(); return; }
            weigerBevestigBtn.disabled = true;
            post("document/weigeren", { reason: reason }).then(function (r) {
                weigerBevestigBtn.disabled = false;
                if (r.ok) {
                    weigerModalSluiten();
                    melding_toon(null);
                    Promise.all([leave(stap1), leave(stap2)]).then(function () {
                        setState("declined");
                        showResult();
                        if (stepsList) stepsList.hidden = true;
                        setBanner("error", PATH_CROSS, "Je hebt geweigerd te ondertekenen", "Group LN neemt hierover contact met je op. Je kan deze pagina nu sluiten.", true);
                    });
                } else {
                    melding_toon(r.error || "Weigeren is niet gelukt.", true);
                }
            }).catch(function () {
                weigerBevestigBtn.disabled = false;
                melding_toon("Er ging iets mis. Probeer het opnieuw.", true);
            });
        });
    }

    // ── Ondertekenen: het verzegelen (36b) ─────────────────────────────────────────────────────
    function fail(stepIndex, title, message) {
        setStep(stepIndex, "err", "");
        setState("error");
        setBanner("error", PATH_CROSS, title, message + " Er is niets ondertekend.", true);
        if (!actions) return;
        actions.hidden = false;
        actions.innerHTML = "";
        var retry = document.createElement("button");
        retry.type = "button";
        retry.className = "gl-v2-sign-btn is-solid";
        retry.textContent = "Opnieuw proberen";
        retry.addEventListener("click", retryFromError);
        var contact = document.createElement("a");
        contact.className = "gl-v2-sign-btn is-outline";
        contact.href = "mailto:info@groupln.be";
        contact.textContent = "Contact opnemen";
        actions.appendChild(retry);
        actions.appendChild(contact);
    }

    function retryFromError() {
        melding_toon(null);
        leave(result).then(function () {
            resetSteps();
            setState("todo");
            if (tekenBtn) tekenBtn.disabled = false;
            if (codeInput) { codeInput.value = ""; }
            updateTekenEnabled();
            enter(stap2);
            if (otpBlock && otpRequired) { otpBlock.hidden = false; if (codeInput) codeInput.focus(); }
        });
    }

    function runSigning() {
        var code = codeInput ? codeInput.value.trim() : "";
        var signatureImagePng = canvas ? canvas.toDataURL("image/png") : null;
        if (tekenBtn) tekenBtn.disabled = true;
        melding_toon(null);

        // Het vak schuift weg, de stappen komen ervoor in de plaats; het document dimt en klapt in (via data-state).
        leave(stap2).then(function () {
            setState("processing");
            resetSteps();
            showResult();
            setBanner("ok", PATH_CLOCK, "Je handtekening wordt verwerkt", "Sluit dit venster niet. Dit duurt meestal minder dan een minuut — je krijgt daarna ook een e-mail.", true);
            setStep(1, "act", "");

            var verifyP = otpRequired
                ? post("document/verifieer", { code: code }).catch(function () { return { ok: false, error: "Er ging iets mis. Probeer het opnieuw." }; })
                : Promise.resolve({ ok: true });

            return Promise.all([verifyP, wait(DWELL)]).then(function (a) {
                var v = a[0];
                if (!v.ok) { fail(2, "We konden je identiteit niet bevestigen", v.error || "De code is niet juist."); return null; }
                setStep(1, "done", nowTime());
                return wait(reduced ? 0 : 180).then(function () {
                    setStep(2, "act", "");
                    return wait(DWELL);
                }).then(function () {
                    setStep(2, "done", nowTime());
                    setStep(3, "act", "");
                    var signP = post("document/tekenen", { consentAccepted: true, idempotencyKey: idempotencyKey(), signatureImagePng: signatureImagePng })
                        .catch(function () { return { ok: false, error: "Er ging iets mis. Probeer het opnieuw." }; });
                    return Promise.all([signP, wait(DWELL)]);
                }).then(function (b) {
                    var r = b[0];
                    if (!r.ok) { fail(3, "Ondertekenen is niet gelukt", r.error || "Probeer het opnieuw."); return null; }
                    setStep(3, "done", nowTime());
                    if (!r.caseCompleted) return wait(DWELL);   // andere eigenaars moeten nog tekenen: stap 4 en 5 volgen later
                    // Laatste ondertekenaar: de service verzegelt, voegt het blad toe en verstuurt de bevestiging.
                    setStep(4, "act", "");
                    return wait(DWELL * 0.8).then(function () {
                        setStep(4, "done", nowTime());
                        setStep(5, "act", "");
                        return wait(DWELL * 0.8);
                    }).then(function () { setStep(5, "done", nowTime()); return wait(DWELL * 0.6); });
                }).then(function (finished) {
                    if (finished === null) return;
                    try { window.sessionStorage.setItem("glSignArrived", "1"); } catch (e) { /* privé-modus */ }
                    window.location.reload();
                });
            });
        });
    }

    if (tekenBtn) {
        tekenBtn.addEventListener("click", function () {
            if (!hasInk) { melding_toon("Teken je volledige handtekening in het vak.", true); return; }
            runSigning();
        });
    }

    // ── Na het herladen: de server-toestand (36c) speelt zijn stappen nog één keer snel in ──────────
    (function arrival() {
        var flag = null;
        try { flag = window.sessionStorage.getItem("glSignArrived"); window.sessionStorage.removeItem("glSignArrived"); } catch (e) { /* ignore */ }
        if (!flag || !stepsList) return;
        var state = root.getAttribute("data-state");
        if (state !== "done" && state !== "waiting") return;
        var items = Array.prototype.slice.call(stepsList.querySelectorAll(".gl-v2-sign-step"));
        var finalStates = items.map(function (li) { return li.getAttribute("data-s"); });
        if (banner) banner.classList.add("is-entering");
        if (reduced) return;
        items.forEach(function (li) { li.setAttribute("data-s", "todo"); });
        void stepsList.offsetWidth;
        items.forEach(function (li, i) {
            setTimeout(function () { li.setAttribute("data-s", finalStates[i]); }, 140 + i * ARRIVE_STAGGER);
        });
    })();
})();
