// PWA: service worker registreren, installatiebanner (dashboard) en terugpijl in standalone-modus.
// Raakt geen bestaande logica, routes of authenticatie.
(function () {
    "use strict";

    var standaloneMq = window.matchMedia ? window.matchMedia("(display-mode: standalone)") : null;
    var mobileMq = window.matchMedia ? window.matchMedia("(pointer: coarse) and (max-width: 768px)") : null;
    function isStandalone() { return (standaloneMq && standaloneMq.matches) || window.navigator.standalone === true; }
    function isMobile() { return !!(mobileMq && mobileMq.matches); }

    // ── Service worker (scope: hele site, /sw.js staat in de root) ─────────────────────────────
    if ("serviceWorker" in navigator) {
        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/sw.js", { scope: "/" }).catch(function () { /* geen SW = gewone site */ });
        });
    }

    // ── Installatiebanner ──────────────────────────────────────────────────────────────────────
    var KEY = "cpm_pwa_banner_dismissed_at";
    var THIRTY_DAYS = 30 * 24 * 60 * 60 * 1000;
    function dismissedRecently() {
        try { var t = parseInt(window.localStorage.getItem(KEY), 10); return !!t && Date.now() - t < THIRTY_DAYS; } catch (e) { return false; }
    }
    function rememberDismiss() { try { window.localStorage.setItem(KEY, String(Date.now())); } catch (e) { /* privé-modus */ } }

    var deferredPrompt = null;
    var banner = null;

    function eligible() { return !!banner && isMobile() && !isStandalone() && !dismissedRecently(); }
    function showBanner(mode) {
        if (!eligible()) return;
        banner.querySelector("[data-pwa-ios]").hidden = mode !== "ios";
        banner.querySelector("[data-pwa-android]").hidden = mode !== "android";
        banner.querySelector("[data-pwa-install]").hidden = mode !== "android";
        banner.hidden = false;
    }
    function hideBanner() { if (banner) banner.hidden = true; }

    // Android/Chrome: de browser-prompt opvangen i.p.v. de standaard mini-infobar. Kan vóór DOMContentLoaded
    // afgaan; dan toont de DOMContentLoaded-tak hieronder de banner alsnog.
    window.addEventListener("beforeinstallprompt", function (e) {
        e.preventDefault();
        deferredPrompt = e;
        showBanner("android");
    });
    window.addEventListener("appinstalled", function () { deferredPrompt = null; hideBanner(); });

    function isIos() {
        return /iphone|ipad|ipod/i.test(navigator.userAgent) || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
    }

    document.addEventListener("DOMContentLoaded", function () {
        banner = document.getElementById("cpm-pwa-banner");
        if (banner) {
            banner.querySelector(".cpm-pwa-close").addEventListener("click", function () { rememberDismiss(); hideBanner(); });
            banner.querySelector("[data-pwa-install]").addEventListener("click", function () {
                if (!deferredPrompt) return;
                deferredPrompt.prompt();
                deferredPrompt.userChoice.then(function () { deferredPrompt = null; hideBanner(); });
            });
            // iOS kent geen automatische prompt: enkel uitleg (Safari en Chrome iOS: Deel → Zet op beginscherm).
            if (isIos()) showBanner("ios");
            else if (deferredPrompt) showBanner("android");
        }

        // ── Terugpijl in standalone: er is dan geen browser-terugknop. Enkel op mobiel, enkel waar de
        //    pagina er zelf geen heeft (ViewData["BackUrl"]) en niet op het dashboard. ───────────────
        if (isStandalone() && isMobile()) {
            var topbar = document.querySelector(".gl-v2-topbar");
            var onDashboard = /^\/(home(\/index)?)?\/?$/i.test(window.location.pathname);
            if (topbar && !onDashboard && !topbar.querySelector(".gl-v2-topbar-back")) {
                var back = document.createElement("a");
                back.className = "gl-v2-topbar-back gl-v2-pwa-back";
                back.href = "/";
                back.setAttribute("aria-label", "Terug");
                back.innerHTML = '<i class="ph ph-caret-left" aria-hidden="true"></i>';
                back.addEventListener("click", function (e) {
                    e.preventDefault();
                    var sameOriginReferrer = document.referrer && document.referrer.indexOf(window.location.origin) === 0;
                    if (window.history.length > 1 && sameOriginReferrer) window.history.back();
                    else window.location.href = "/";
                });
                var logo = topbar.querySelector(".gl-v2-mobile-logo");
                if (logo && logo.parentNode === topbar) topbar.insertBefore(back, logo.nextSibling);
                else topbar.insertBefore(back, topbar.firstChild);
            }
        }
    });

    // ── Downloads vanuit het browservenster ────────────────────────────────────────────────────
    // Staat de app geïnstalleerd (ook op desktop), dan "vangt" de browser een klik op een link binnen de site
    // en opent die in het app-venster: de download verschijnt daar en niet in het browservenster waar je
    // klikte. Een download die via fetch + blob loopt is geen navigatie en wordt dus nooit gevangen. Enkel in een
    // gewoon browservenster (in het app-venster zelf werkt de gewone link). HTML-antwoorden (afdrukpagina's)
    // blijven een gewone navigatie; PDF's en afbeeldingen openen als blob in een nieuw tabblad; bijlagen worden bewaard.
    var DOWNLOAD_RE = //(Export[A-Za-z0-9]*|Download[A-Za-z0-9]*|[A-Za-z0-9]*Pdf[A-Za-z0-9]*|[A-Za-z0-9]*Excel[A-Za-z0-9]*|[A-Za-z0-9]*Csv[A-Za-z0-9]*|[A-Za-z0-9]*Xlsx[A-Za-z0-9]*|[A-Za-z0-9]*Zip[A-Za-z0-9]*|GuaranteeDoc)(/|?|$)/i;

    function filenameFrom(res, url) {
        var cd = res.headers.get("Content-Disposition") || "";
        var m = /filename*=UTF-8''([^;]+)/i.exec(cd);
        if (m) { try { return decodeURIComponent(m[1].trim().replace(/"/g, "")); } catch (e) { /* val terug */ } }
        m = /filename="?([^";]+)"?/i.exec(cd);
        if (m) return m[1].trim();
        var last = url.pathname.split("/").filter(Boolean).pop() || "download";
        return last;
    }

    document.addEventListener("click", function (e) {
        if (isStandalone() || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        var a = e.target && e.target.closest ? e.target.closest("a[href]") : null;
        if (!a || !window.fetch || !window.URL || !window.Blob) return;
        var url;
        try { url = new URL(a.href, window.location.href); } catch (err) { return; }
        if (url.origin !== window.location.origin) return;
        var wants = a.hasAttribute("data-gl-v2-download") || a.hasAttribute("download") || DOWNLOAD_RE.test(url.pathname);
        if (!wants) return;

        e.preventDefault();
        var openInTab = a.target === "_blank";
        fetch(url.href, { credentials: "same-origin" }).then(function (res) {
            if (!res.ok) throw new Error("status " + res.status);
            var type = (res.headers.get("Content-Type") || "").toLowerCase();
            var disposition = (res.headers.get("Content-Disposition") || "").toLowerCase();
            var isAttachment = disposition.indexOf("attachment") !== -1;
            if (!isAttachment && type.indexOf("text/html") !== -1) {
                // Geen bestand maar een pagina (bv. afdrukweergave): gewone navigatie.
                if (openInTab) window.open(url.href, "_blank"); else window.location.href = url.href;
                return null;
            }
            return res.blob().then(function (blob) {
                var objectUrl = window.URL.createObjectURL(blob);
                if (!isAttachment && (type.indexOf("pdf") !== -1 || type.indexOf("image/") !== -1)) {
                    window.open(objectUrl, "_blank");
                } else {
                    var link = document.createElement("a");
                    link.href = objectUrl;
                    link.download = filenameFrom(res, url);
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
                window.setTimeout(function () { window.URL.revokeObjectURL(objectUrl); }, 120000);
            });
        }).catch(function () {
            // Lukt het ophalen niet, dan beter de gewone link dan niets.
            if (openInTab) window.open(url.href, "_blank"); else window.location.href = url.href;
        });
    }, false);
})();
