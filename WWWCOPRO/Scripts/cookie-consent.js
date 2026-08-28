/* ================================================
   Cookietoestemming — eigen implementatie (geen externe library).
   - Noodzakelijke cookies: altijd actief.
   - Statistiek / Marketing: standaard uit, opt-in.
   - Keuze bewaard in de cookie "groupln_cookie_consent" (6 maanden).
   - Stuurt Google Consent Mode aan en laadt Google Tag Manager pas
     nadat statistiek of marketing is aanvaard (window.grouplnLoadGTM,
     gedefinieerd in _Layout.vbhtml).
   ================================================ */
(function () {
    'use strict';

    var COOKIE_NAME = 'groupln_cookie_consent';
    var COOKIE_DAYS = 182; // ~6 maanden

    var root = document.getElementById('ccConsent');
    var fab = document.getElementById('ccFab');
    if (!root || !fab) { return; }

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
    function openBanner() { banner.classList.add('is-open'); }
    function closeBanner() { banner.classList.remove('is-open'); }

    function openDialog() {
        var current = parseConsent(readCookie());
        var cats = current ? current.categories : { analytics: false, marketing: false };
        if (inputAnalytics) { inputAnalytics.checked = !!cats.analytics; }
        if (inputMarketing) { inputMarketing.checked = !!cats.marketing; }
        overlay.classList.add('is-open');
        dialog.classList.add('is-open');
        document.body.classList.add('cc-lock');
        var focusable = dialog.querySelector('input, button');
        if (focusable) { focusable.focus(); }
    }

    function closeDialog() {
        overlay.classList.remove('is-open');
        dialog.classList.remove('is-open');
        document.body.classList.remove('cc-lock');
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
