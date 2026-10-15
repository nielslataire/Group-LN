/* Projectpagina v4 (split) — gedrag voor Views/Projects/DetailV4.vbhtml.
   Alle data komt uit <script type="application/json" id="pdv4-data"> (server-side model).
   Vanilla JS, geen afhankelijkheden behalve Leaflet (optioneel, voor de buurtkaart). */
(function () {
    'use strict';

    var dataEl = document.getElementById('pdv4-data');
    if (!dataEl) { return; }
    var D = JSON.parse(dataEl.textContent);
    var HEADER_OFFSET = 100;

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function eur(n) { return '€ ' + Math.round(n).toLocaleString('nl-BE'); }
    function num(n) { return Math.round(n).toLocaleString('nl-BE'); }
    function lotById(id) { return D.lots.filter(function (l) { return l.id === +id; })[0]; }
    function mq(q) { return window.matchMedia(q).matches; }

    // Getekende iconen (geen tekstglyphs)
    var ICON_DL = '<svg class="pdv4-ico" width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 2v8M4.5 6.5 8 10l3.5-3.5M2.5 13.5h11"/></svg>';
    var ICON_X = '<svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="M3.5 3.5l9 9M12.5 3.5l-9 9"/></svg>';

    // reCAPTCHA v3 wordt pas geladen wanneer iemand echt een formulier verstuurt (niet bij elk paginabezoek).
    var recaptchaLoading = null;
    function loadRecaptcha() {
        if (window.grecaptcha) { return Promise.resolve(); }
        if (!window.reCaptchaSiteKey) { return Promise.reject(); }
        if (!recaptchaLoading) {
            recaptchaLoading = new Promise(function (resolve, reject) {
                var s = document.createElement('script');
                s.src = 'https://www.google.com/recaptcha/api.js?render=' + encodeURIComponent(window.reCaptchaSiteKey);
                s.async = true; s.onload = resolve; s.onerror = function () { recaptchaLoading = null; reject(); };
                document.head.appendChild(s);
            });
        }
        return recaptchaLoading;
    }
    // Geeft altijd een token (leeg als reCAPTCHA niet beschikbaar is, de server beslist dan zelf)
    function getRecaptchaToken(action) {
        return loadRecaptcha().then(function () {
            return new Promise(function (resolve) {
                window.grecaptcha.ready(function () {
                    window.grecaptcha.execute(window.reCaptchaSiteKey, { action: action }).then(resolve, function () { resolve(''); });
                });
            });
        }).catch(function () { return ''; });
    }
    function isPhone() { return mq('(max-width: 639px)'); }
    function isNarrow() { return mq('(max-width: 999px)'); }

    function scrollToId(id) {
        var el = document.getElementById(id);
        if (!el) { return; }
        // onder de (vaste) header blijven + wat lucht, zodat de titel van het blok zichtbaar is
        var page = document.querySelector('.pdv4-page');
        var h = page ? parseInt(getComputedStyle(page).getPropertyValue('--pdv4-header-h'), 10) : 0;
        var offset = (h || HEADER_OFFSET) + 28;
        window.scrollTo({ top: el.getBoundingClientRect().top + window.scrollY - offset, behavior: 'smooth' });
    }

    /* Hoogte van de (sticky) site-header → linkerbeeld blijft er netjes onder */
    (function () {
        var page = $('.pdv4-page'), header = document.getElementById('header');
        if (!page || !header) { return; }
        var ticking = false;
        var last = -1, lastGap = 0;
        function sync() {
            ticking = false;
            // Het thema maakt bij scrollen de .header-body vast (position: fixed) terwijl #header zelf kan krimpen:
            // meet daarom beide en neem de onderste rand.
            var body = header.querySelector('.header-body');
            var bottom = Math.max(0, Math.round(header.getBoundingClientRect().bottom),
                                  body ? Math.round(body.getBoundingClientRect().bottom) : 0);
            // Op gsm/tablet loopt het beeld gewoon na de header (geen sticky offset). Reserveert het thema in #header meer ruimte dan de
            // zichtbare, vaste .header-body (bv. hoogte gemeten vóór het logo geladen was), dan ontstaat er een spleet: die trekken we weg.
            var gap = 0;
            if (isNarrow() && body && window.getComputedStyle(body).position === 'fixed') {
                gap = Math.round(header.getBoundingClientRect().bottom - body.getBoundingClientRect().bottom);
                if (gap < 3 || gap > 160) { gap = 0; }
            }
            if (bottom === last && gap === lastGap) { return; } // enkel herberekenen als de headerhoogte echt wijzigt
            last = bottom; lastGap = gap;
            page.style.setProperty('--pdv4-header-h', bottom + 'px');
            page.style.marginTop = gap ? (-gap) + 'px' : '';
        }
        function queue() { if (!ticking) { ticking = true; window.requestAnimationFrame(sync); } }
        window.addEventListener('scroll', queue, { passive: true });
        window.addEventListener('resize', queue);
        window.addEventListener('load', function () { queue(); setTimeout(queue, 700); setTimeout(queue, 2000); });
        sync();
    })();

    var state = {
        media: 'intro', sel: 0, listFilter: 'alle',
        calcOpt: null, down: D.calc.down, rate: D.calc.rate, years: D.calc.years,
        interest: [], layers: [], scrolled: false
    };

    /* Ankerlinks (#contact, #prijslijst, …) soepel scrollen, zonder globale smooth-scroll */
    document.addEventListener('click', function (e) {
        var a = e.target.closest ? e.target.closest('a[href^="#"]') : null;
        if (!a || a.getAttribute('href') === '#' || !a.closest('.pdv4-page')) { return; }
        var id = a.getAttribute('href').slice(1);
        if (!document.getElementById(id)) { return; }
        e.preventDefault();
        scrollToId(id);
    });

    /* ───────── Grondplan aanvragen: zelfde formulier/modal als op de gewone projectpagina (Projects/SendPlan) ───────── */
    var contactCookie = 'bco_contact_info';
    window.saveContactInfoToCookie = window.saveContactInfoToCookie || function (contact) {
        try { document.cookie = contactCookie + '=' + encodeURIComponent(JSON.stringify(contact)) + ';path=/;max-age=' + (60 * 60 * 24 * 365); } catch (e) { /* cookies geblokkeerd */ }
    };
    function applySavedContact($c) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + contactCookie + '=([^;]*)'));
        if (!m) { return; }
        try {
            var saved = JSON.parse(decodeURIComponent(m[1]));
            if (saved.firstname) { $c.find('#txtFirstname').val(saved.firstname); }
            if (saved.name) { $c.find('#txtName').val(saved.name); }
            if (saved.email) { $c.find('#txtEmail').val(saved.email); }
            if (saved.phone) { $c.find('#txtPhone').val(saved.phone); }
        } catch (e) { /* ongeldige cookie */ }
    }
    // ───── Aanvraagmodals (brochure + grondplan): eigen opmaak, versturen via de bestaande acties Projects/SendBrochure en Projects/SendPlan ─────
    var broAsk = document.getElementById('pdv4BroAsk');
    var broDone = document.getElementById('pdv4BroDone');
    function showBrochureDone(email) {
        if (!broAsk || !broDone) { return; }
        var em = broDone.querySelector('[data-bro-email]');
        if (em) { em.textContent = email || ''; }
        broAsk.setAttribute('hidden', '');
        broDone.removeAttribute('hidden');
    }

    function fieldError(input, msg) {
        var em = input.parentNode.querySelector('.pdv4-field-error');
        input.setAttribute('aria-invalid', msg ? 'true' : 'false');
        if (em) { em.textContent = msg || ''; if (msg) { em.removeAttribute('hidden'); } else { em.setAttribute('hidden', ''); } }
    }
    // Validatie "in de mate van het mogelijke": namen met letters, e-mail met geldig formaat + typfout-hint, Belgisch of internationaal nummer.
    var nameRe = /^[A-Za-zÀ-ÖØ-öø-ÿĀ-ſ][A-Za-zÀ-ÖØ-öø-ÿĀ-ſ .'’-]*$/;
    var typoDomains = { 'gmial.com': 'gmail.com', 'gmai.com': 'gmail.com', 'gamil.com': 'gmail.com', 'gmail.be': 'gmail.com', 'hotmial.com': 'hotmail.com', 'hotmal.com': 'hotmail.com', 'hotmail.be': 'hotmail.com', 'outlok.com': 'outlook.com', 'telenet.com': 'telenet.be', 'skynet.com': 'skynet.be', 'yahooo.com': 'yahoo.com', 'yaho.com': 'yahoo.com' };
    function checkName(v, label) {
        v = v.trim();
        if (!v) { return label + ' is verplicht.'; }
        if (v.length < 2) { return label + ' lijkt te kort.'; }
        if (!nameRe.test(v)) { return label + ' mag enkel letters bevatten.'; }
        return '';
    }
    function checkEmail(v) {
        v = v.trim();
        if (!v) { return 'Je e-mailadres is verplicht.'; }
        if (/\s/.test(v) || !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(v) || /\.\./.test(v) || /^\.|\.@|@\./.test(v)) { return 'Dit e-mailadres lijkt niet te kloppen.'; }
        var dom = v.split('@')[1].toLowerCase();
        if (typoDomains[dom]) { return 'Bedoelde je ' + v.split('@')[0] + '@' + typoDomains[dom] + '?'; }
        return '';
    }
    function checkPhone(v) {
        v = v.trim();
        if (!v) { return 'Je telefoonnummer is verplicht.'; }
        if (!/^[+\d][\d\s./()-]*$/.test(v)) { return 'Een telefoonnummer bevat enkel cijfers, spaties en een +.'; }
        var d = v.replace(/[^\d+]/g, '');
        if (/^(\+32|0032)/.test(d)) { d = '0' + d.replace(/^(\+32|0032)/, ''); }
        if (d.charAt(0) === '0') {
            var digits = d.length;
            if (d.charAt(1) === '0' || (digits !== 9 && digits !== 10)) { return 'Gebruik een geldig Belgisch nummer, bv. 0475 12 34 56 of +32 475 12 34 56.'; }
            if (d.charAt(1) === '4' && digits !== 10) { return 'Een gsm-nummer heeft 10 cijfers, bv. 0475 12 34 56.'; }
            return '';
        }
        if (d.charAt(0) === '+') {
            var n = d.length - 1;
            if (n < 8 || n > 15) { return 'Dit internationale nummer lijkt niet te kloppen.'; }
            return '';
        }
        return 'Begin met 0 of +32, bv. 0475 12 34 56.';
    }
    var checks = {
        Firstname: function (v) { return checkName(v, 'Je voornaam'); },
        Name: function (v) { return checkName(v, 'Je naam'); },
        Email: checkEmail,
        Phone: checkPhone
    };

    function initRequestModal(cfg) {
        var modal = document.getElementById(cfg.modal), form = document.getElementById(cfg.form);
        if (!modal || !form) { return null; }
        var dialog = modal.querySelector('.pdv4-modal__dialog'), lastFocus = null;

        function validate(only) {
            var firstBad = null;
            Object.keys(checks).forEach(function (k) {
                if (only && only !== k) { return; }
                var input = form.elements[k], msg = checks[k](input.value);
                fieldError(input, msg);
                if (msg && !firstBad) { firstBad = input; }
            });
            return firstBad;
        }
        function trap(e) {
            if (e.key !== 'Tab' || modal.hasAttribute('hidden')) { return; }
            var f = Array.prototype.slice.call(modal.querySelectorAll('button, input:not([type=hidden]):not([tabindex="-1"]), a[href]')).filter(function (x) { return x.offsetParent !== null; });
            if (!f.length) { return; }
            var first = f[0], last = f[f.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
        function open() {
            lastFocus = document.activeElement;
            var m = document.cookie.match(new RegExp('(?:^|; )' + contactCookie + '=([^;]*)'));
            if (m) {
                try {
                    var sv = JSON.parse(decodeURIComponent(m[1]));
                    [['Firstname', 'firstname'], ['Name', 'name'], ['Email', 'email'], ['Phone', 'phone']].forEach(function (p) {
                        if (sv[p[1]] && !form.elements[p[0]].value) { form.elements[p[0]].value = sv[p[1]]; }
                    });
                } catch (e) { /* ongeldige cookie */ }
            }
            dialog.classList.remove('is-done');
            var err = document.getElementById(cfg.error); if (err) { err.setAttribute('hidden', ''); }
            modal.removeAttribute('hidden');
            var first = form.elements.Firstname.value ? form.elements.Email : form.elements.Firstname;
            setTimeout(function () { first.focus(); }, 30);
        }
        function close() {
            if (modal.hasAttribute('hidden')) { return; }
            modal.setAttribute('hidden', '');
            if (lastFocus && lastFocus.focus) { lastFocus.focus(); }
        }
        document.addEventListener('click', function (e) {
            if (e.target.closest && e.target.closest(cfg.closeSel)) { e.preventDefault(); close(); }
        });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { close(); } trap(e); });
        Object.keys(checks).forEach(function (k) {
            form.elements[k].addEventListener('blur', function () { if (this.value.trim() || this.getAttribute('aria-invalid') === 'true') { validate(k); } });
            form.elements[k].addEventListener('input', function () { if (this.getAttribute('aria-invalid') === 'true') { validate(k); } });
        });
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var bad = validate();
            if (bad) { bad.focus(); return; }
            var btn = document.getElementById(cfg.submit), err = document.getElementById(cfg.error);
            err.setAttribute('hidden', '');
            btn.disabled = true; btn.textContent = 'Even geduld…';
            var email = form.elements.Email.value.trim();
            function reset() { btn.disabled = false; btn.textContent = cfg.submitLabel; }
            function fail() {
                reset();
                err.textContent = 'Het versturen is niet gelukt. Probeer het opnieuw of bel ons op 09/216.49.50.';
                err.removeAttribute('hidden');
            }
            function done() {
                window.saveContactInfoToCookie({ firstname: form.elements.Firstname.value.trim(), name: form.elements.Name.value.trim(), email: email, phone: form.elements.Phone.value.trim() });
                window.dataLayer = window.dataLayer || [];
                window.dataLayer.push({ event: 'download_form_submit' });
                reset();
                cfg.onDone(email, dialog, close);
            }
            function send(token) {
                document.getElementById(cfg.token).value = token || '';
                if (cfg.simulate && cfg.simulate()) { done(); return; }
                fetch(cfg.url, {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: new URLSearchParams(new FormData(form))
                }).then(function (r) { return r.ok ? r.text() : Promise.reject(); })
                  .then(function (html) { if (html.indexOf('contact-modal-header-success') > -1) { done(); } else { fail(); } })
                  .catch(fail);
            }
            getRecaptchaToken(cfg.action).then(send);
        });
        return { open: open, close: close, form: form, modal: modal };
    }

    // Brochure
    var broModal = initRequestModal({
        modal: 'pdv4BroModal', form: 'pdv4BroForm', token: 'pdv4BroToken', error: 'pdv4BroError', submit: 'pdv4BroSubmit',
        submitLabel: 'Stuur mij de brochure', url: '/Projects/SendBrochure', action: 'sendbrochure', closeSel: '[data-bro-close]',
        simulate: function () { return !D.brochureDocId; }, // voorbeeldpagina zonder echt document
        onDone: function (email, dialog, close) { close(); showBrochureDone(email); }
    });
    if (broModal) {
        document.addEventListener('click', function (e) {
            if (e.target.closest && e.target.closest('[data-open-brochure]')) { e.preventDefault(); broModal.open(); }
        });
        if (broDone) {
            broDone.addEventListener('click', function (e) {
                if (!e.target.closest('[data-bro-reset]')) { return; }
                broDone.setAttribute('hidden', '');
                broAsk.removeAttribute('hidden');
                broModal.form.elements.Email.value = '';
                broModal.open();
            });
        }
    }

    // Document (lastenboek, brochure, inplantingsplan …) per e-mail aanvragen: Projects/SendDoc
    var docModal = initRequestModal({
        modal: 'pdv4DocModal', form: 'pdv4DocForm', token: 'pdv4DocToken', error: 'pdv4DocError', submit: 'pdv4DocSubmit',
        submitLabel: 'Stuur mij het document', url: '/Projects/SendDoc', action: 'senddoc', closeSel: '[data-doc-close]',
        simulate: function () { return !docModal || !docModal.form.elements.DocId.value || docModal.form.elements.DocId.value === '0'; }, // voorbeeldpagina
        onDone: function (email, dialog) {
            var em = dialog.querySelector('[data-done-email]'); if (em) { em.textContent = email; }
            dialog.classList.add('is-done');
            var btn = dialog.querySelector('.pdv4-modal__done .pdv4-btn'); if (btn) { btn.focus(); }
        }
    });
    if (docModal) {
        document.addEventListener('click', function (e) {
            var a = e.target.closest ? e.target.closest('[data-doc-id]') : null;
            if (!a || a.hasAttribute('data-open-brochure')) { return; } // de projectbrochure opent het brochureformulier
            e.preventDefault();
            e.stopPropagation();
            var name = a.getAttribute('data-doc-name') || 'het document';
            docModal.form.elements.DocId.value = a.getAttribute('data-doc-id');
            docModal.modal.querySelector('[data-doc-name-intro]').textContent = name;
            docModal.modal.querySelector('[data-doc-title]').textContent = 'Ontvang ' + name.charAt(0).toLowerCase() + name.slice(1) + ' in je mailbox';
            docModal.modal.querySelector('[data-doc-eyebrow]').textContent = 'Document ' + D.project.name;
            docModal.open();
        }, true);
    }

    // Grondplan
    var planModal = initRequestModal({
        modal: 'pdv4PlanModal', form: 'pdv4PlanForm', token: 'pdv4PlanToken', error: 'pdv4PlanError', submit: 'pdv4PlanSubmit',
        submitLabel: 'Stuur mij het grondplan', url: '/Projects/SendPlan', action: 'sendplan', closeSel: '[data-plan-close]',
        onDone: function (email, dialog) {
            var em = dialog.querySelector('[data-done-email]'); if (em) { em.textContent = email; }
            dialog.classList.add('is-done');
            var btn = dialog.querySelector('.pdv4-modal__done .pdv4-btn'); if (btn) { btn.focus(); }
        }
    });
    if (planModal) {
        document.addEventListener('click', function (e) {
            var a = e.target.closest ? e.target.closest('[data-plan-id]') : null;
            if (!a) { return; }
            e.preventDefault();
            e.stopPropagation();
            var id = a.getAttribute('data-plan-id'), lot = lotById(id);
            planModal.form.elements.UnitId.value = id;
            var name = lot ? lot.name : 'deze woning';
            planModal.modal.querySelector('[data-plan-lot]').textContent = name;
            planModal.modal.querySelector('[data-plan-eyebrow]').textContent = 'Grondplan ' + name + ' · ' + D.project.name;
            planModal.open();
        }, true);
    }

    /* ───────── Menu ───────── */
    (function () {
        var btn = $('#pdv4Menu'), panel = $('#pdv4MenuPanel');
        if (!btn || !panel) { return; }
        btn.addEventListener('click', function () {
            var open = panel.hasAttribute('hidden');
            if (open) { panel.removeAttribute('hidden'); } else { panel.setAttribute('hidden', ''); }
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !panel.hasAttribute('hidden')) { panel.setAttribute('hidden', ''); btn.setAttribute('aria-expanded', 'false'); btn.focus(); }
        });
    })();

    /* ───────── Linkerbeeld: crossfade per zichtbare sectie ───────── */
    var layers = $$('.pdv4-layer');
    var mediaLabel = $('#pdv4MediaLabel');
    function layerFor(key) {
        if (state.sel && key === 'homes') {
            var lotLayer = layers.filter(function (l) { return l.getAttribute('data-key') === 'lot-' + state.sel; })[0];
            if (lotLayer && lotLayer.querySelector('img') && !isNarrow()) { return lotLayer; }
        }
        return layers.filter(function (l) { return l.getAttribute('data-key') === key; })[0] || layers[0];
    }
    function applyMedia() {
        var target = layerFor(state.media);
        layers.forEach(function (l) { l.classList.toggle('is-active', l === target); });
        var bar = $('#pdv4Bar');
        if (bar) {
            // op gsm/tablet pas tonen zodra er gescrold is: het eerste zicht is enkel het beeld, tot onderaan het scherm
            var hide = !isNarrow() || !state.scrolled || state.media === 'contact' || (isPhone() && state.sel);
            if (hide) { bar.setAttribute('hidden', ''); } else { bar.removeAttribute('hidden'); }
        }
    }
    (function () {
        var secs = $$('[data-media]');
        if (!secs.length || !('IntersectionObserver' in window)) { return; }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) { state.media = e.target.getAttribute('data-media'); applyMedia(); }
            });
        }, { rootMargin: '-45% 0px -45% 0px' });
        secs.forEach(function (s) { io.observe(s); });
    })();
    window.addEventListener('resize', applyMedia);
    (function () {
        var tick = false;
        function check() {
            tick = false;
            var s = (window.pageYOffset || document.documentElement.scrollTop || 0) > 8;
            if (s !== state.scrolled) { state.scrolled = s; applyMedia(); }
        }
        window.addEventListener('scroll', function () { if (!tick) { tick = true; window.requestAnimationFrame(check); } }, { passive: true });
        check();
    })();

    /* Label rechtsboven op de foto: de naam van het (diepste) blok dat rechts in beeld is */
    (function () {
        var els = $$('[data-block-label]');
        if (!els.length || !mediaLabel || !('IntersectionObserver' in window)) { return; }
        var inView = [];
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                var i = inView.indexOf(e.target);
                if (e.isIntersecting && i === -1) { inView.push(e.target); }
                if (!e.isIntersecting && i > -1) { inView.splice(i, 1); }
            });
            if (!inView.length) { return; }
            // diepste element = laatste in documentvolgorde (bv. Prijslijst binnen De woningen)
            var best = inView.reduce(function (a, b) { return (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING) ? b : a; });
            var text = best.getAttribute('data-block-label') || '';
            if (mediaLabel.textContent !== text) { mediaLabel.textContent = text; }
        }, { rootMargin: '-45% 0px -45% 0px' });
        els.forEach(function (el) { io.observe(el); });
    })();

    /* ───────── Projectkaart (luchtfoto + percelen) ───────── */
    var map = $('#pdv4ProjectMap');
    var stage = $('#pdv4Stage');
    var card = $('#pdv4LotCard');
    var tabs = $$('.pdv4-lottab');

    function setSelection(id) {
        state.sel = state.sel === id ? 0 : id;
        renderSelection();
        applyMedia();
    }
    function clearSelection() {
        if (!state.sel) { return; }
        state.sel = 0;
        renderSelection();
        applyMedia();
    }
    function renderSelection() {
        if (!map) { return; }
        var id = state.sel, lot = id ? lotById(id) : null;
        map.classList.toggle('has-selection', !!lot);
        $$('.pdv4-zone', map).forEach(function (z) { z.classList.toggle('is-selected', +z.getAttribute('data-lot') === id); });
        $$('.pdv4-zonelabel', map).forEach(function (z) { z.classList.toggle('is-selected', +z.getAttribute('data-lot') === id); });
        tabs.forEach(function (t) { t.setAttribute('aria-selected', +t.getAttribute('data-lot') === id ? 'true' : 'false'); });

        var k = 1, tx = 0, ty = 0;
        if (lot && lot.polygon) {
            k = lot.zoom > 0 ? lot.zoom : 2.1;
            var cy = isPhone() ? 50 : (lot.zoom > 0 && lot.zoom < 2 ? 28 : 34);
            var clampV = function (v) { return Math.min(0, Math.max(100 - 100 * k, v)); };
            tx = clampV(50 - lot.lx * k);
            ty = isPhone() ? clampV(cy - lot.ly * k) : Math.min(0, cy - lot.ly * k);
        }
        stage.style.transform = 'translate(' + tx + '%,' + ty + '%) scale(' + k + ')';
        stage.style.setProperty('--pdv4-inv', (1 / k).toFixed(3));
        renderCard(lot);
    }
    function renderCard(lot) {
        if (!card) { return; }
        if (!lot) { card.setAttribute('hidden', ''); card.innerHTML = ''; return; }
        var av = lot.avail;
        var from = av && lot.hasPrice ? lot.finishes[0].price : 0;
        card.setAttribute('aria-label', lot.name);
        card.setAttribute('tabindex', '-1');
        card.innerHTML =
            '<div class="pdv4-lotcard__head">' +
            '<div class="pdv4-lotcard__title"><div class="pdv4-lotcard__type">' + esc(lot.typeLabel) + '</div><h3>' + esc(lot.name) + '</h3></div>' +
            '<div class="pdv4-lotcard__right"><span class="pdv4-pill' + (av ? '' : ' pdv4-pill--sold') + '"><span></span>' + esc(lot.status) + '</span>' +
            '<button type="button" class="pdv4-lotcard__close" aria-label="Sluiten" data-close>'+ICON_X+'</button></div></div>' +
            (lot.desc ? '<p class="pdv4-lotcard__desc">' + esc(lot.desc) + '</p>' : '') +
            '<dl><div><dt>Woning</dt><dd>' + num(lot.living) + ' m²</dd></div><div><dt>Perceel</dt><dd>' + num(lot.land) + ' m²</dd></div>' +
            '<div><dt>Slpk</dt><dd>' + lot.bed + '</dd></div><div><dt>Tuin</dt><dd>' + esc(lot.garden || '—') + '</dd></div></dl>' +
            '<div class="pdv4-lotcard__foot"><span class="pdv4-lotcard__price' + (av ? '' : ' is-sold') + '">' + (av ? (lot.hasPrice ? 'Vanaf ' + eur(from) : 'Op aanvraag') : 'Verkocht') + '</span>' +
            '<div class="pdv4-lotcard__btns">' +
            (av && lot.hasPlan ? '<a class="is-plan" href="#" data-plan-id="' + lot.id + '">'+ICON_DL+'Grondplan</a>' : '') +
            '<a class="is-main" href="#prijslijst" data-close>' + (av ? 'Prijs &amp; documenten' : 'Gelijkaardige woning?') + '</a></div></div>';
        card.removeAttribute('hidden');
    }
    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; });
    }

    if (map) {
        $$('.pdv4-zone', map).forEach(function (z) {
            var id = +z.getAttribute('data-lot');
            z.addEventListener('click', function (e) { e.stopPropagation(); setSelection(id); });
            z.addEventListener('keydown', function (e) { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setSelection(id); if (card && !card.hasAttribute('hidden')) { card.focus({ preventScroll: true }); } } });
        });
        var bg = $('[data-bg]', map);
        if (bg) { bg.addEventListener('click', clearSelection); }
        tabs.forEach(function (t) { t.addEventListener('click', function () { setSelection(+t.getAttribute('data-lot')); }); });
        card.addEventListener('click', function (e) {
            if (e.target.closest('[data-close]')) { clearSelection(); }
        });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { clearSelection(); } });
        window.addEventListener('resize', function () { if (state.sel) { renderSelection(); } });
        renderSelection();
    }

    /* ───────── Prijslijst: filter ───────── */
    (function () {
        var seg = $('#pdv4ListFilter');
        if (!seg) { return; }
        function apply() {
            $$('[data-avail]').forEach(function (r) {
                var show = state.listFilter === 'alle' || r.getAttribute('data-avail') === '1';
                if (show) { r.removeAttribute('hidden'); } else { r.setAttribute('hidden', ''); }
            });
            $$('button', seg).forEach(function (b) { b.setAttribute('aria-checked', b.getAttribute('data-filter') === state.listFilter ? 'true' : 'false'); });
        }
        seg.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-filter]');
            if (!b) { return; }
            state.listFilter = b.getAttribute('data-filter');
            apply();
        });
        apply();
    })();

    /* ───────── Maandlastcalculator ───────── */
    function monthly(price, down, ratePct, years) {
        var loan = Math.max(0, price - down);
        if (!loan) { return 0; }
        var r = ratePct / 100 / 12, n = years * 12;
        return r === 0 ? loan / n : loan * r / (1 - Math.pow(1 + r, -n));
    }
    var calc = $('#maandlast');
    var opts = [];
    D.lots.forEach(function (l) {
        if (!l.avail) { return; }
        l.finishes.forEach(function (f, i) {
            opts.push({ key: l.id + '|' + i, lot: l.name, finish: f, multi: l.finishes.length > 1, price: f.price });
        });
    });
    function optLabel(o) { return o.lot + (o.multi ? ' (' + o.finish.label.toLowerCase() + ')' : ''); }
    function renderCalc() {
        // "≈ … / maand" in de prijslijst volgt dezelfde parameters (eerste afwerking per lot)
        $$('[data-monthly]').forEach(function (el) {
            var o = opts.filter(function (x) { return x.key === el.getAttribute('data-monthly'); })[0];
            if (o) { el.textContent = eur(monthly(o.price, state.down, state.rate, state.years)); }
        });
        if (!calc || !opts.length) { return; }
        var cur = opts.filter(function (o) { return o.key === state.calcOpt; })[0] || opts[0];
        state.calcOpt = cur.key;
        $$('#pdv4CalcChips .pdv4-chip').forEach(function (c) { c.setAttribute('aria-checked', c.getAttribute('data-opt') === cur.key ? 'true' : 'false'); });
        var rateTxt = state.rate.toFixed(2).replace('.', ',') + ' %';
        $('#pdv4DownTxt').textContent = eur(state.down);
        $('#pdv4RateTxt').textContent = rateTxt;
        $('#pdv4CalcTitle').textContent = cur.lot + (cur.multi ? ' · ' + cur.finish.label.toLowerCase() : '');
        $('#pdv4CalcAmount').textContent = eur(monthly(cur.price, state.down, state.rate, state.years));
        $('#pdv4CalcLoan').textContent = eur(Math.max(0, cur.price - state.down));
        $('#pdv4CalcYears').textContent = state.years;
        $('#pdv4CalcRate').textContent = rateTxt;
        $$('#pdv4Years button').forEach(function (b) { b.setAttribute('aria-pressed', +b.getAttribute('data-years') === state.years ? 'true' : 'false'); });
    }
    if (calc) {
        $('#pdv4CalcChips').addEventListener('click', function (e) {
            var c = e.target.closest('.pdv4-chip'); if (!c) { return; }
            state.calcOpt = c.getAttribute('data-opt'); renderCalc();
        });
        $('#pdv4Down').addEventListener('input', function (e) { state.down = +e.target.value; renderCalc(); });
        $('#pdv4Rate').addEventListener('input', function (e) { state.rate = +e.target.value; renderCalc(); });
        $('#pdv4Years').addEventListener('click', function (e) {
            var b = e.target.closest('button[data-years]'); if (!b) { return; }
            state.years = +b.getAttribute('data-years'); renderCalc();
        });
        $('#pdv4CalcCta').addEventListener('click', function () {
            var cur = opts.filter(function (o) { return o.key === state.calcOpt; })[0] || opts[0];
            if (!cur) { return; }
            goContact({
                interest: [cur.lot],
                message: 'Ik wil graag een persoonlijke simulatie voor ' + cur.lot + (cur.multi ? ' (' + cur.finish.label.toLowerCase() + ')' : '') +
                    ' aan ' + eur(cur.price) + ', met ' + eur(state.down) + ' eigen inbreng over ' + state.years + ' jaar.'
            });
        });
    }
    $$('[data-tocalc]').forEach(function (b) {
        b.addEventListener('click', function () { state.calcOpt = b.getAttribute('data-tocalc'); renderCalc(); scrollToId('maandlast'); });
    });
    renderCalc();

    /* ───────── FAQ (één item open) ───────── */
    $$('.pdv4-faq__item button').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var item = btn.closest('.pdv4-faq__item');
            var willOpen = btn.getAttribute('aria-expanded') !== 'true';
            $$('.pdv4-faq__item').forEach(function (it) {
                var b = $('button', it), p = $('.pdv4-faq__answer', it), open = it === item && willOpen;
                b.setAttribute('aria-expanded', open ? 'true' : 'false');
                $('.pdv4-faq__sign', it).textContent = open ? '−' : '+';
                it.classList.toggle('is-open', open);
                if (open) { p.removeAttribute('hidden'); } else { p.setAttribute('hidden', ''); }
            });
        });
    });

    /* ───────── Contactformulier ───────── */
    var form = $('#pdv4ContactForm');
    var title = $('#pdv4-contact-h');
    var message = $('#pdv4Message');
    function availableNames() { return D.lots.filter(function (l) { return l.avail; }).map(function (l) { return l.name; }); }
    function renderContactTitle() {
        if (!title || D.contact.fixedTitle) { return; }
        var av = availableNames();
        var picked = av.filter(function (n) { return state.interest.indexOf(n) > -1; });
        var t;
        if (picked.length === 1) { t = picked[0] + ' kan de jouwe zijn.'; }
        else if (picked.length > 1) { t = picked.slice(0, -1).join(', ') + ' of ' + picked[picked.length - 1].toLowerCase() + ' — welke wordt de jouwe?'; }
        else if (av.length === 1) { t = av[0] + ' kan de jouwe zijn.'; }
        else { t = D.contact.titleMany; }
        title.textContent = t;
    }
    function renderInterest() {
        $$('#pdv4Interest [data-interest]').forEach(function (b) {
            b.setAttribute('aria-pressed', state.interest.indexOf(b.getAttribute('data-interest')) > -1 ? 'true' : 'false');
        });
        renderContactTitle();
    }
    function goContact(patch) {
        if (patch.interest) { state.interest = patch.interest.slice(); }
        if (typeof patch.message === 'string' && message) { message.value = patch.message; }
        renderInterest();
        scrollToId('contact');
    }
    if (form) {
        $('#pdv4Interest').addEventListener('click', function (e) {
            var b = e.target.closest('[data-interest]'); if (!b) { return; }
            var n = b.getAttribute('data-interest'), i = state.interest.indexOf(n);
            if (i > -1) { state.interest.splice(i, 1); } else { state.interest.push(n); }
            renderInterest();
        });
        renderContactTitle();
    }
    $$('[data-goto-question]').forEach(function (b) { b.addEventListener('click', function () { goContact({ interest: [D.contact.general] }); }); });
    $$('[data-goto-meeting]').forEach(function (b) { b.addEventListener('click', function () { goContact({ message: D.contact.buyMessage }); }); });

    /* Validatie + verzenden */
    var emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
    // nationaal (0478 12 34 56) of internationaal (+32 …, +31 …, 0031 …): enkel de vorm toetsen, niet het land
    var phoneRe = /^(?:(?:\+|00)[1-9]\d{0,2}|0)[\s./-]?[1-9][\d\s./-]{5,13}$/;
    function fieldError(input, msg) {
        var em = input.parentNode.querySelector('.pdv4-field-error');
        input.classList.toggle('has-error', !!msg);
        input.setAttribute('aria-invalid', msg ? 'true' : 'false');
        if (em) { em.textContent = msg || ''; if (msg) { em.removeAttribute('hidden'); } else { em.setAttribute('hidden', ''); } }
    }
    function post(url, payload) {
        if (!url) { return Promise.resolve(); } // nog geen endpoint gekoppeld: enkel de bevestiging tonen
        return fetch(url, {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
            body: JSON.stringify(payload)
        }).then(function (r) { if (!r.ok) { throw new Error('http ' + r.status); } });
    }
    // Het contactformulier gebruikt dezelfde server-actie als de oude projectpagina (Projects/SendMail): mail naar de klant,
    // interne melding en opslag van de aanvraag. De gekozen woningen en het bericht gaan samen in het veld "Question".
    function sendContact(v) {
        if (!D.project.id) { return Promise.resolve(); } // voorbeeldpagina zonder echt project: enkel de bevestiging tonen
        var lines = [];
        if (state.interest.length) { lines.push('Interesse in: ' + state.interest.join(', ')); }
        if (v.message) { lines.push(v.message); }
        return getRecaptchaToken('sendmail').then(function (token) {
            var body = new URLSearchParams();
            body.set('ProjectId', D.project.id);
            body.set('Firstname', v.firstName); body.set('Name', v.lastName);
            body.set('Email', v.email); body.set('Phone', v.phone);
            body.set('Question', lines.join('\n\n'));
            body.set('website_url', '');
            body.set('g-recaptcha-response', token || '');
            return fetch('/Projects/SendMail', {
                method: 'POST', credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: body
            });
        }).then(function (r) { return r.ok ? r.text() : Promise.reject(new Error('http ' + r.status)); })
          .then(function (html) { if (html.indexOf('contact-modal-header-success') === -1) { throw new Error('mail'); } });
    }
    function showFormError(el, msg) { if (!el) { return; } el.textContent = msg; el.removeAttribute('hidden'); }

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var f = form.elements, ok = true;
            ['firstName', 'lastName'].forEach(function (n) {
                var v = f[n].value.trim();
                fieldError(f[n], v ? '' : 'Dit veld is verplicht.'); if (!v) { ok = false; }
            });
            var mail = f.email.value.trim();
            var mailMsg = !mail ? 'Vul je e-mailadres in.' : (!emailRe.test(mail) ? 'Dit e-mailadres lijkt niet te kloppen.' : '');
            fieldError(f.email, mailMsg); if (mailMsg) { ok = false; }
            var ph = f.phone.value.trim();
            var phMsg = !ph ? 'Vul je gsm-nummer in.' : (!phoneRe.test(ph) ? 'Controleer je nummer, bv. 0478 12 34 56 of +31 6 12 34 56 78.' : '');
            fieldError(f.phone, phMsg); if (phMsg) { ok = false; }
            var ce = $('#pdv4ConsentError');
            if (!f.consent.checked) { ce.textContent = 'Je toestemming is nodig om je aanvraag te kunnen behandelen.'; ce.removeAttribute('hidden'); ok = false; }
            else { ce.setAttribute('hidden', ''); }
            if (!ok) { var first = form.querySelector('.has-error'); if (first) { first.focus(); } return; }

            var btn = form.querySelector('button[type=submit]'); btn.disabled = true;
            var errEl = $('#pdv4FormError'); errEl.setAttribute('hidden', '');
            sendContact({
                firstName: f.firstName.value.trim(), lastName: f.lastName.value.trim(),
                email: mail, phone: ph, message: f.message.value.trim()
            }).then(function () {
                var thanks = $('#pdv4Thanks'), sec = $('#contact');
                form.setAttribute('hidden', ''); thanks.removeAttribute('hidden');
                if (sec) { sec.classList.add('is-sent'); }
                try { thanks.scrollIntoView({ behavior: 'smooth', block: 'nearest' }); thanks.focus({ preventScroll: true }); } catch (err) { /* oudere browsers */ }
            }).catch(function () {
                btn.disabled = false;
                showFormError(errEl, 'Het versturen is niet gelukt. Probeer het opnieuw of bel ons op 09/216.49.50.');
            });
        });
    }

    /* Brochure- en werfupdate-formulieren (één e-mailveld) */
    $$('form[data-form]').forEach(function (fm) {
        fm.addEventListener('submit', function (e) {
            e.preventDefault();
            var kind = fm.getAttribute('data-form'), input = fm.elements.email, err = $('.pdv4-form-error', fm);
            var mail = input.value.trim();
            if (!emailRe.test(mail)) { showFormError(err, 'Vul een geldig e-mailadres in.'); input.focus(); return; }
            err.setAttribute('hidden', '');
            var btn = $('button[type=submit]', fm); btn.disabled = true;
            post(D.endpoints[kind], { project: D.project.name, email: mail }).then(function () {
                fm.setAttribute('hidden', '');
                var done = fm.parentNode.querySelector('.pdv4-inline-done'); if (done) { done.removeAttribute('hidden'); }
            }).catch(function () {
                btn.disabled = false; showFormError(err, 'Het versturen is niet gelukt. Probeer het opnieuw of bel ons op 09/216.49.50.');
            });
        });
    });

    /* ───────── Buurtkaart (Leaflet) ───────── */
    (function () {
        var el = $('#pdv4Leaflet');
        if (!el || !D.map.showMap || !window.L) { return; }
        var L = window.L, P = [D.project.lat, D.project.lng];
        var POPUP_OPTS = { minWidth: 190, maxWidth: 300, autoPanPadding: [24, 24] };
        var m = L.map(el, { scrollWheelZoom: false }).setView(P, 15);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap-bijdragers', maxZoom: 19 }).addTo(m);
        function pin(text) {
            return L.divIcon({ className: '', iconSize: [32, 32], iconAnchor: [16, 16], html: '<div class="pdv4-pin">' + esc(text) + '</div>' });
        }
        var projectIcon = L.divIcon({ className: '', iconSize: [52, 52], iconAnchor: [26, 26],
            html: '<div class="pdv4-pin pdv4-pin--project"><img src="' + esc(D.map.logo) + '" alt=""></div>' });
        L.marker(P, { icon: projectIcon, zIndexOffset: 1000 }).addTo(m)
            .bindPopup('<strong class="pdv4-pop__name">' + esc(D.project.name) + '</strong><span class="pdv4-pop__meta">' + esc(D.project.address) + '</span>', POPUP_OPTS);

        var groups = {};
        D.map.cats.forEach(function (c) { groups[c.id] = L.layerGroup(); if (c.defaultOn) { state.layers.push(c.id); } });
        D.map.pois.forEach(function (p) {
            var cat = D.map.cats.filter(function (c) { return c.id === p.cat; })[0];
            if (!cat || !groups[p.cat]) { return; }
            groups[p.cat].addLayer(L.marker([p.lat, p.lng], { icon: pin(cat.icon) }).bindPopup('<strong class="pdv4-pop__name">' + esc(p.name) + '</strong><span class="pdv4-pop__meta">' + esc(p.dist) + ' · ' + esc(p.time) + '</span>', POPUP_OPTS));
        });
        // Fietsbereik: 5 / 10 / 15 minuten ≈ 1,3 / 2,6 / 3,9 km
        if (groups.fiets) {
            [[1300, '5 min'], [2600, '10 min'], [3900, '15 min']].forEach(function (r) {
                groups.fiets.addLayer(L.circle(P, { radius: r[0], color: '#00532D', weight: 1.2, dashArray: '4 6', fillColor: '#00532D', fillOpacity: 0.04 }).bindTooltip(r[1] + ' fietsen'));
            });
        }
        function syncLayers() {
            D.map.cats.forEach(function (c) {
                var on = state.layers.indexOf(c.id) > -1, g = groups[c.id];
                if (on && !m.hasLayer(g)) { g.addTo(m); }
                if (!on && m.hasLayer(g)) { m.removeLayer(g); }
            });
        }
        syncLayers();
        $('#pdv4LayerChips').addEventListener('click', function (e) {
            var b = e.target.closest('[data-cat]'); if (!b) { return; }
            var id = b.getAttribute('data-cat'), i = state.layers.indexOf(id);
            if (i > -1) { state.layers.splice(i, 1); } else { state.layers.push(id); }
            b.setAttribute('aria-pressed', i > -1 ? 'false' : 'true');
            syncLayers();
            if (id === 'fiets' && i === -1) { m.flyTo(P, 13, { duration: 0.6 }); }
        });
    })();

    applyMedia();
})();
