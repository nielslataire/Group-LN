(function () {
    "use strict";

    var prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // ===== Levend decor: aurora-achtergrond pauzeert buiten beeld/tabblad =====
    var aurora = document.querySelector(".gl-aurora");
    if (aurora) {
        if (prefersReducedMotion) {
            aurora.style.animationPlayState = "paused";
        } else {
            document.addEventListener("visibilitychange", function () {
                aurora.classList.toggle("is-paused", document.hidden);
            });
        }
    }

    // ===== Levend decor: cursor-reactieve kanteling + spotlight op de tegels =====
    // Enkel bij een echte muis (hover:hover + pointer:fine) — geen effect nodig op
    // touch, en volledig uit bij prefers-reduced-motion.
    var featuresBar = document.querySelector(".gl-features-bar");
    var tiles = document.querySelectorAll(".gl-tile");
    if (featuresBar && tiles.length && !prefersReducedMotion &&
        window.matchMedia("(hover: hover) and (pointer: fine)").matches) {
        var ticking = false;
        var lastEvent = null;

        function applyTilt() {
            ticking = false;
            if (!lastEvent) return;
            tiles.forEach(function (tile) {
                var r = tile.getBoundingClientRect();
                var mx = (lastEvent.clientX - r.left) / r.width;
                var my = (lastEvent.clientY - r.top) / r.height;
                if (mx >= -0.15 && mx <= 1.15 && my >= -0.15 && my <= 1.15) {
                    tile.style.setProperty("--mx", mx.toFixed(3));
                    tile.style.setProperty("--my", my.toFixed(3));
                    tile.setAttribute("data-tilt", "");
                } else {
                    tile.removeAttribute("data-tilt");
                }
            });
        }

        featuresBar.addEventListener("mousemove", function (e) {
            lastEvent = e;
            if (!ticking) {
                ticking = true;
                requestAnimationFrame(applyTilt);
            }
        });
        featuresBar.addEventListener("mouseleave", function () {
            tiles.forEach(function (tile) { tile.removeAttribute("data-tilt"); });
        });
    }

    // ===== Aankomstmoment: de SSO-knop "opent" het scherm i.p.v. een harde sprong =====
    var ssoBtn = document.querySelector(".btn-gl-ms");
    var loginRoot = document.querySelector(".gl-login");
    if (ssoBtn && loginRoot && !prefersReducedMotion && typeof ssoBtn.animate === "function") {
        ssoBtn.addEventListener("click", function (e) {
            // Middelklik / ctrl+klik / etc. moeten gewoon normaal blijven werken
            // (nieuw tabblad openen) — enkel de gewone linkerklik onderscheppen.
            if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            if (ssoBtn.dataset.launching === "1") { e.preventDefault(); return; }
            e.preventDefault();
            ssoBtn.dataset.launching = "1";

            var href = ssoBtn.href;
            var rect = ssoBtn.getBoundingClientRect();
            var startRadius = parseFloat(getComputedStyle(ssoBtn).borderRadius) || 8;

            // Kloon i.p.v. de knop zelf verhuizen: ontsnapt gegarandeerd aan elke
            // voorouder (kaart, grid) naar de volledige viewport, ongeacht eventuele
            // transform/stacking-eigenaardigheden van tussenliggende elementen.
            var clone = ssoBtn.cloneNode(true);
            clone.classList.add("gl-launch-morph");
            clone.style.position = "fixed";
            clone.style.top = rect.top + "px";
            clone.style.left = rect.left + "px";
            clone.style.width = rect.width + "px";
            clone.style.height = rect.height + "px";
            clone.style.margin = "0";
            clone.removeAttribute("href");
            document.body.appendChild(clone);
            ssoBtn.style.visibility = "hidden";

            loginRoot.classList.add("is-launching");

            var vw = window.innerWidth;
            var vh = window.innerHeight;
            var scale = Math.max(vw / rect.width, vh / rect.height) * 1.05;
            var originX = rect.left + rect.width / 2;
            var originY = rect.top + rect.height / 2;
            var dx = (vw / 2) - originX;
            var dy = (vh / 2) - originY;

            var anim = clone.animate([
                { transform: "translate(0px, 0px) scale(1)", borderRadius: startRadius + "px" },
                { transform: "translate(" + dx + "px, " + dy + "px) scale(" + scale + ")", borderRadius: "0px" }
            ], { duration: 620, easing: "cubic-bezier(.65,0,.35,1)", fill: "forwards" });

            // De knoptekst/iconen zitten IN de klonende doos, dus schalen anders gigantisch
            // en vervormd mee terwijl die uitdijt. Boxicons-glyphs gebruiken (net als tekst)
            // de "color"-eigenschap voor hun vulkleur, dus deze ene overgang naar transparant
            // verbergt beide tegelijk, snel, terwijl het groene vlak zelf gewoon doorgroeit.
            clone.animate(
                [{ color: getComputedStyle(clone).color }, { color: "transparent" }],
                { duration: 160, easing: "ease-in", fill: "forwards" }
            );

            // Merkteken dat in het vlak verschijnt zodra dat (bijna) het hele scherm dekt —
            // zonder dit was er na de knop-morph enkel een kleurvlak te zien, geen inhoud.
            var logoSrc = document.querySelector(".gl-logo img");
            var mark = document.createElement("img");
            mark.src = logoSrc ? logoSrc.currentSrc || logoSrc.src : "";
            mark.alt = "";
            mark.setAttribute("aria-hidden", "true");
            mark.className = "gl-launch-mark";
            document.body.appendChild(mark);
            mark.animate(
                [
                    { opacity: 0, transform: "translate(-50%, -50%) scale(.7)" },
                    { opacity: 1, transform: "translate(-50%, -50%) scale(1)" }
                ],
                { duration: 380, delay: 320, easing: "cubic-bezier(.16,1,.3,1)", fill: "forwards" }
            );

            var navigated = false;
            function go() {
                if (navigated) return;
                navigated = true;
                window.location.href = href;
            }
            // Kleine pauze ná de expansie: geeft het merkteken een moment om echt gezien te
            // worden voor de browser de eigenlijke navigatie start, i.p.v. er meteen overheen
            // te navigeren zodra het vlak de viewport vult.
            anim.addEventListener("finish", function () { setTimeout(go, 220); });
            // Veiligheidsnet: navigeer sowieso, ook als de animatie om een of andere
            // reden nooit "finish" meldt — een gebruiker mag hier nooit op vastlopen.
            setTimeout(go, 1200);
        });
    }
})();
