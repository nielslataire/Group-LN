// Publieke ondertekenpagina (fase 2, ONDERTEKENEN_VOORSTEL.md §5.2). Vanilla JS, geen CDN
// (Content-Security-Policy op /tekenen staat enkel 'self' toe). Praat met OndertekenenController
// via JSON-fetch; het antiforgery-token gaat als header mee (Program.cs zet HeaderName op
// "RequestVerificationToken" precies hiervoor).
//
// Volgorde bewust in twee stappen (feedback Niels, 2026-09-28): eerst Akkoord/Weigeren, zonder
// code of handtekeningvak in beeld. Pas na "Akkoord" verschijnt de verificatiecode (indien nodig)
// en het handtekeningvak; "Ondertekenen" blijft uitgeschakeld tot er getekend is (en, indien nodig,
// een code is ingevuld) — controller dwingt de handtekening ook server-side af.
(function () {
    "use strict";

    var card = document.getElementById("gl-tekenen-actiekaart");
    if (!card) return;

    var otpRequired = card.getAttribute("data-otp-required") === "1";
    var destination = card.getAttribute("data-destination") || "uw e-mailadres";

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

    var statusBadge = document.getElementById("gl-tekenen-status-badge");
    var geweigerdBevestiging = document.getElementById("gl-tekenen-geweigerd-bevestiging");
    var weigerModal = document.getElementById("gl-tekenen-weiger-modal");
    var weigerReden = document.getElementById("gl-tekenen-weiger-reden");
    var weigerAnnuleerBtn = document.getElementById("gl-tekenen-weiger-annuleer");
    var weigerBevestigBtn = document.getElementById("gl-tekenen-weiger-bevestig");

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

    // ── Handtekeningvak ──────────────────────────────────────────────────────────────────────
    // Eén klik/tik zonder te bewegen mag niet als handtekening tellen (feedback Niels, 2026-09-28):
    // hasInk wordt pas true zodra de opgetelde lengte van de getekende lijn(en) een minimum haalt,
    // niet al bij het eerste contactpunt.
    var MIN_INK_LENGTH = 80;
    var inkLength = 0;

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
        if (!canvas || !ctx) return;
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
            if (stap1) stap1.style.display = "none";
            if (stap2) stap2.style.display = "block";
            wireCanvas();
            if (otpRequired) {
                if (otpBlock) otpBlock.style.display = "block";
                vraagCode();
            }
            updateTekenEnabled();
        });
    }

    if (opnieuwCodeBtn) {
        opnieuwCodeBtn.addEventListener("click", function () {
            opnieuwCodeBtn.disabled = true;
            vraagCode().then(function () { opnieuwCodeBtn.disabled = false; });
        });
    }

    if (codeInput) {
        codeInput.addEventListener("input", updateTekenEnabled);
    }

    // ── Weigeren: eigen gl-v2-modal (geen window.prompt, geen Bootstrap — deze pagina laadt geen
    // gl-v2-shell.css/JS, zie gl-v2-public.css). Na een geslaagde weigering geen reload: DeclineAsync
    // trekt de sessietoken meteen in (RevokeTokens), dus een herlaadpoging zou altijd op de neutrale
    // "sessie verlopen"-pagina uitkomen i.p.v. een echte bevestiging tonen.
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

    function weigerModalEscape(e) {
        if (e.key === "Escape") weigerModalSluiten();
    }

    if (weigerBtn) weigerBtn.addEventListener("click", weigerModalOpenen);
    if (weigerAnnuleerBtn) weigerAnnuleerBtn.addEventListener("click", weigerModalSluiten);
    if (weigerModal) {
        weigerModal.addEventListener("click", function (e) {
            if (e.target === weigerModal) weigerModalSluiten();
        });
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
                    var actiekaart = document.getElementById("gl-tekenen-actiekaart");
                    if (actiekaart) actiekaart.style.display = "none";
                    if (geweigerdBevestiging) geweigerdBevestiging.hidden = false;
                    if (statusBadge) {
                        statusBadge.className = "gl-v2-public-badge is-blocked";
                        statusBadge.textContent = "Geweigerd";
                    }
                } else {
                    melding_toon(r.error || "Weigeren is niet gelukt.", true);
                }
            }).catch(function () {
                weigerBevestigBtn.disabled = false;
                melding_toon("Er ging iets mis. Probeer het opnieuw.", true);
            });
        });
    }

    if (tekenBtn) {
        tekenBtn.addEventListener("click", function () {
            if (!hasInk) { melding_toon("Teken uw volledige handtekening in het vak.", true); return; }
            tekenBtn.disabled = true;
            melding_toon(null);

            var verify = otpRequired && codeInput
                ? post("document/verifieer", { code: codeInput.value.trim() })
                : Promise.resolve({ ok: true });

            verify.then(function (v) {
                if (!v.ok) {
                    melding_toon(v.error || "De code is niet juist.", true);
                    updateTekenEnabled();
                    return;
                }
                var signatureImagePng = canvas ? canvas.toDataURL("image/png") : null;
                post("document/tekenen", {
                    consentAccepted: true,
                    idempotencyKey: idempotencyKey(),
                    signatureImagePng: signatureImagePng
                }).then(function (r) {
                    if (r.ok) {
                        window.location.reload();
                    } else {
                        updateTekenEnabled();
                        melding_toon(r.error || "Ondertekenen is niet gelukt.", true);
                    }
                }).catch(function () {
                    updateTekenEnabled();
                    melding_toon("Er ging iets mis. Probeer het opnieuw.", true);
                });
            }).catch(function () {
                melding_toon("Er ging iets mis. Probeer het opnieuw.", true);
                updateTekenEnabled();
            });
        });
    }
})();
