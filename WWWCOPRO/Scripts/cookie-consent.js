/* ================================================
   Cookietoestemming — eigen implementatie (geen externe library).
   - Noodzakelijke cookies: altijd actief.
   - Statistiek / Marketing: standaard uit, opt-in.
   - Keuze bewaard in de cookie "groupln_cookie_consent" (6 maanden).
   - Stuurt Google Consent Mode aan en laadt Google Tag Manager pas
     nadat statistiek of marketing is aanvaard (window.grouplnLoadGTM,
     gedefinieerd in _Layout.vbhtml).
   - Anonieme telling van de banner-uitkomst (Shown/Accepted/Rejected/
     Abandoned) via POST /cookie-consent-event, zie logEvent() hieronder.
   ================================================ */
(function () {
    'use strict';

    var COOKIE_NAME = 'groupln_cookie_consent';
    var COOKIE_DAYS = 182; // ~6 maanden
    var EVENT_URL = '/cookie-consent-event';
    var SHOWN_LOGGED_KEY = 'groupln_cc_shown_logged';
    var ABANDONED_LOGGED_KEY = 'groupln_cc_abandoned_logged';

    var root = document.getElementById('ccConsent');
    var fab = document.getElementById('ccFab');
    if (!root || !fab) { return; }

    // ── Anonieme meting van de banner-uitkomst (zie migratie 033_CookieConsentEvents,
    //    zichtbaar in CPMCore/Instellingen). Los van Google Analytics — die laadt zelf pas
    //    ná toestemming en kan dus nooit "geweigerd" of "geen keuze" meten. Best-effort:
    //    een netwerkfout hier mag nooit iets anders op de pagina breken. ──
    function logEvent(eventType) {
        try {
            var body = JSON.stringify({ EventType: eventType });
            if (navigator.sendBeacon) {
                navigator.sendBeacon(EVENT_URL, new Blob([body], { type: 'application/json' }));
            } else {
                fetch(EVENT_URL, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: body, keepalive: true })
                    .catch(function () { });
            }
        } catch (e) { /* meting mag de pagina nooit breken */ }
    }

    // "Shown" en "Abandoned" tellen één keer per browser (localStorage), niet per sessie of
    // tabblad: een aarzelaar die pas bij zijn derde bezoek kiest, is één "getoond" en één keuze,
    // geen drie "getoond". Bij een keuze worden de vlaggen gewist, zodat de volgende cyclus
    // (cookie vervallen na 6 maanden, cookies gewist) opnieuw als één weergave telt.
    function flagStore() {
        try { if (window.localStorage) { return window.localStorage; } } catch (e) { /* geblokkeerd */ }
        try { if (window.sessionStorage) { return window.sessionStorage; } } catch (e) { /* geblokkeerd */ }
        return null;
    }
    function flagIsSet(key) {
        try { var st = flagStore(); return !!(st && st.getItem(key)); } catch (e) { return false; }
    }
    function flagSet(key) {
        try { var st = flagStore(); if (st) { st.setItem(key, '1'); } } catch (e) { /* privé-modus e.d.: dan liever te vaak dan nooit tellen */ }
    }
    function flagClear(key) {
        try { var st = flagStore(); if (st) { st.removeItem(key); } } catch (e) { /* niets te wissen */ }
    }

    function logShownOnce() {
        if (flagIsSet(SHOWN_LOGGED_KEY)) { return; }
        flagSet(SHOWN_LOGGED_KEY);
        logEvent('Shown');
    }

    // ── "Verlaten": de bezoeker sluit het tabblad, gaat terug of klikt weg naar een andere site
    //    terwijl de banner nog open staat. pagehide vuurt óók bij navigatie binnen de site, dus
    //    een klik op een interne link of een formulierverzending zet eerst een vlag: dan is het
    //    geen verlaten, de banner komt op de volgende pagina gewoon terug. ──
    var internalNavPending = false;

    function markInternalNavigation(e) {
        var a = e.target && e.target.closest ? e.target.closest('a[href]') : null;
        if (!a) { return; }
        var target = (a.getAttribute('target') || '').toLowerCase();
        if (target && target !== '_self') { return; } // nieuw tabblad: deze pagina blijft open
        var href = a.getAttribute('href') || '';
        if (/^(mailto|tel|javascript):/i.test(href)) { return; }
        try {
            if (new URL(href, location.href).origin === location.origin) { internalNavPending = true; }
        } catch (err) { /* ongeldige href: als extern behandelen */ }
    }

    function logAbandonedOnce() {
        var open = banner.classList.contains('is-open') || dialog.classList.contains('is-open');
        if (!open) { return; }
        if (parseConsent(readCookie())) { return; }
        if (internalNavPending) { internalNavPending = false; return; }
        if (flagIsSet(ABANDONED_LOGGED_KEY)) { return; }
        flagSet(ABANDONED_LOGGED_KEY);
        logEvent('Abandoned');
    }

    var banner = root.querySelector('.cc-banner');
    var dialog = root.querySelector('.cc-dialog');
    var overlay = root.querySelector('.cc-overlay');
    var inputAnalytics = document.getElementById('ccCatAnalytics');
    var inputMarketing = document.getElementById('ccCatMarketing');

    // ── Cookie-helpers ───────────────────────────────────────────────
    function writeCookie(value) {
        var d = new Date();
        d.setTime(d.getTime() + COOKIE_DAYS * 24 * 60 * 60 * 1000);
        document.cookie = COOKIE_NAME + '=' + encodeURIComponent(value) +
            ';expires=' + d.toUTCString() + ';path=/;SameSite=Lax';
    }

    function readCookie() {
        var m = document.cookie.match(new RegExp('(?:^|; )' + COOKIE_NAME + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : null;
    }

    function clearCookiesByPrefix(prefixes) {
        var host = location.hostname;
        var domains = ['', host, '.' + host];
        var parts = host.split('.');
        if (parts.length > 2) { domains.push('.' + parts.slice(-2).join('.')); }

        document.cookie.split(';').forEach(function (raw) {
            var name = raw.split('=')[0].trim();
            for (var i = 0; i < prefixes.length; i++) {
                if (name.indexOf(prefixes[i]) === 0) {
                    domains.forEach(function (dm) {
                        document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/' +
                            (dm ? ';domain=' + dm : '');
                    });
                }
            }
        });
    }

    function parseConsent(raw) {
        if (!raw) { return null; }
        try {
            var o = JSON.parse(raw);
            if (o && o.categories) { return o; }
        } catch (e) { /* ongeldige inhoud negeren */ }
        return null;
    }

    // ── Consent toepassen ────────────────────────────────────────────
    function applyConsent(state, isChange) {
        var analytics = !!state.categories.analytics;
        var marketing = !!state.categories.marketing;

        if (typeof window.gtag === 'function') {
            window.gtag('consent', 'update', {
                analytics_storage: analytics ? 'granted' : 'denied',
                ad_storage: marketing ? 'granted' : 'denied',
                ad_user_data: marketing ? 'granted' : 'denied',
                ad_personalization: marketing ? 'granted' : 'denied'
            });
        }

        if ((analytics || marketing) && typeof window.grouplnLoadGTM === 'function') {
            window.grouplnLoadGTM();
        }

        // Bij het intrekken van toestemming de bijhorende cookies opruimen.
        if (isChange && !analytics) { clearCookiesByPrefix(['_ga', '_gid', '_gat']); }
        if (isChange && !marketing) { clearCookiesByPrefix(['_gcl', '_fbp', '_uetsid', '_uetvid', 'fr']); }
    }

    function save(categories, isChange) {
        var state = {
            v: 1,
            date: new Date().toISOString(),
            categories: {
                necessary: true,
                analytics: !!categories.analytics,
                marketing: !!categories.marketing
            }
        };
        writeCookie(JSON.stringify(state));
        applyConsent(state, isChange);
    }

    // ── UI ───────────────────────────────────────────────────────────
    // Overlay + scroll-lock zijn gedeeld tussen banner en voorkeurenpaneel: zolang één
    // van beide open staat, blijft de rest van de pagina afgeschermd. De banner sluit
    // niet bij een klik op de overlay — dat zou de verplichte keuze omzeilen.
    function updateOverlay() {
        var open = banner.classList.contains('is-open') || dialog.classList.contains('is-open');
        overlay.classList.toggle('is-open', open);
        document.body.classList.toggle('cc-lock', open);
        // Het voorkeurenpaneel komt voor de banner: die verdwijnt zolang het paneel open staat
        // en komt terug als het paneel gesloten wordt zonder keuze.
        root.classList.toggle('cc-dialog-open', dialog.classList.contains('is-open'));
    }

    function openBanner() { banner.classList.add('is-open'); updateOverlay(); logShownOnce(); }
    function closeBanner() { banner.classList.remove('is-open'); updateOverlay(); }

    function openDialog() {
        var current = parseConsent(readCookie());
        var cats = current ? current.categories : { analytics: false, marketing: false };
        if (inputAnalytics) { inputAnalytics.checked = !!cats.analytics; }
        if (inputMarketing) { inputMarketing.checked = !!cats.marketing; }
        dialog.classList.add('is-open');
        updateOverlay();
        var focusable = dialog.querySelector('input, button');
        if (focusable) { focusable.focus(); }
    }

    function closeDialog() {
        dialog.classList.remove('is-open');
        updateOverlay();
    }

    function showFab() { fab.classList.add('is-visible'); }

    function decide(kind) {
        var isChange = !!parseConsent(readCookie());
        var cats;
        if (kind === 'accept') {
            cats = { analytics: true, marketing: true };
        } else if (kind === 'reject') {
            cats = { analytics: false, marketing: false };
        } else { // 'save'
            cats = {
                analytics: inputAnalytics ? inputAnalytics.checked : false,
                marketing: inputMarketing ? inputMarketing.checked : false
            };
        }
        save(cats, isChange);
        // Enkel de allereerste keuze (nog geen cookie) telt mee als "Aanvaard"/"Geweigerd" —
        // een latere wijziging via het cookie-icoon is geen antwoord op de banner meer.
        if (!isChange) { logEvent((cats.analytics || cats.marketing) ? 'Accepted' : 'Rejected'); }
        flagClear(SHOWN_LOGGED_KEY);
        flagClear(ABANDONED_LOGGED_KEY);
        closeDialog();
        closeBanner();
        showFab();
    }

    // ── Events ───────────────────────────────────────────────────────
    root.addEventListener('click', function (e) {
        // Klik op de donkere achtergrond rond het paneel sluit het paneel.
        if (e.target === dialog || e.target === overlay) { closeDialog(); return; }
        var t = e.target.closest('[data-cc-action], [data-cc-close-prefs]');
        if (!t) { return; }
        if (t.hasAttribute('data-cc-close-prefs')) { closeDialog(); return; }
        var action = t.getAttribute('data-cc-action');
        if (action === 'prefs') { openDialog(); }
        else if (action === 'accept' || action === 'reject' || action === 'save') { decide(action); }
    });

    fab.addEventListener('click', openDialog);

    document.addEventListener('click', markInternalNavigation, true);
    document.addEventListener('submit', function () { internalNavPending = true; }, true);
    window.addEventListener('pagehide', logAbandonedOnce);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && dialog.classList.contains('is-open')) { closeDialog(); }
    });

    // Laat de cookiebeleid-pagina het paneel openen.
    window.grouplnCookieConsent = { open: openDialog };

    // ── Init ─────────────────────────────────────────────────────────
    var existing = parseConsent(readCookie());
    if (existing) {
        applyConsent(existing, false);
        showFab();
    } else {
        openBanner();
    }
})();
