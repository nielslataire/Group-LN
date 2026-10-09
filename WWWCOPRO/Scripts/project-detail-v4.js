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
    function isPhone() { return mq('(max-width: 639px)'); }
    function isNarrow() { return mq('(max-width: 999px)'); }

    function scrollToId(id) {
        var el = document.getElementById(id);
        if (!el) { return; }
        window.scrollTo({ top: el.getBoundingClientRect().top + window.scrollY - HEADER_OFFSET, behavior: 'smooth' });
    }

    /* Hoogte van de (sticky) site-header → linkerbeeld blijft er netjes onder */
    (function () {
        var page = $('.pdv4-page'), header = document.getElementById('header');
        if (!page || !header) { return; }
        var ticking = false;
        var last = -1;
        function sync() {
            ticking = false;
            var bottom = Math.max(0, Math.round(header.getBoundingClientRect().bottom));
            if (bottom === last) { return; } // enkel herberekenen als de headerhoogte echt wijzigt
            last = bottom;
            page.style.setProperty('--pdv4-header-h', bottom + 'px');
        }
        function queue() { if (!ticking) { ticking = true; window.requestAnimationFrame(sync); } }
        window.addEventListener('scroll', queue, { passive: true });
        window.addEventListener('resize', queue);
        sync();
    })();

    var state = {
        media: 'intro', sel: 0, listFilter: 'alle',
        calcOpt: null, down: D.calc.down, rate: D.calc.rate, years: D.calc.years,
        interest: [], layers: []
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
        if (mediaLabel && target) {
            mediaLabel.textContent = target.getAttribute('data-label') || '';
        }
        var bar = $('#pdv4Bar');
        if (bar) {
            var hide = !isNarrow() || state.media === 'contact' || (isPhone() && state.sel);
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
        var from = av ? lot.finishes[0].price : 0;
        card.setAttribute('aria-label', lot.name);
        card.innerHTML =
            '<div class="pdv4-lotcard__head">' +
            '<div class="pdv4-lotcard__title"><div class="pdv4-lotcard__type">' + esc(lot.typeLabel) + '</div><h3>' + esc(lot.name) + '</h3></div>' +
            '<div class="pdv4-lotcard__right"><span class="pdv4-pill' + (av ? '' : ' pdv4-pill--sold') + '"><span></span>' + esc(lot.status) + '</span>' +
            '<button type="button" class="pdv4-lotcard__close" aria-label="Sluiten" data-close>✕</button></div></div>' +
            (lot.desc ? '<p class="pdv4-lotcard__desc">' + esc(lot.desc) + '</p>' : '') +
            '<dl><div><dt>Woning</dt><dd>' + num(lot.living) + ' m²</dd></div><div><dt>Perceel</dt><dd>' + num(lot.land) + ' m²</dd></div>' +
            '<div><dt>Slpk</dt><dd>' + lot.bed + '</dd></div><div><dt>Tuin</dt><dd>' + esc(lot.garden || '—') + '</dd></div></dl>' +
            '<div class="pdv4-lotcard__foot"><span class="pdv4-lotcard__price' + (av ? '' : ' is-sold') + '">' + (av ? 'Vanaf ' + eur(from) : 'Verkocht') + '</span>' +
            '<div class="pdv4-lotcard__btns">' +
            (av && lot.plan ? '<a class="is-plan" href="' + esc(lot.plan) + '">Grondplan ⤓</a>' : '') +
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
            z.addEventListener('keydown', function (e) { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setSelection(id); } });
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
                var b = $('button', it), p = $('p', it), open = it === item && willOpen;
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
        if (!title) { return; }
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
    var phoneRe = /^(\+32|0032|0)\s?[1-9][\d\s./-]{6,12}$/;
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
            var phMsg = !ph ? 'Vul je gsm-nummer in.' : (!phoneRe.test(ph) ? 'Gebruik een Belgisch nummer (+32 … of 04…).' : '');
            fieldError(f.phone, phMsg); if (phMsg) { ok = false; }
            var ce = $('#pdv4ConsentError');
            if (!f.consent.checked) { ce.textContent = 'Je toestemming is nodig om je aanvraag te kunnen behandelen.'; ce.removeAttribute('hidden'); ok = false; }
            else { ce.setAttribute('hidden', ''); }
            if (!ok) { var first = form.querySelector('.has-error'); if (first) { first.focus(); } return; }

            var btn = form.querySelector('button[type=submit]'); btn.disabled = true;
            var errEl = $('#pdv4FormError'); errEl.setAttribute('hidden', '');
            post(D.endpoints.contact, {
                project: D.project.name, interest: state.interest,
                firstName: f.firstName.value.trim(), lastName: f.lastName.value.trim(),
                email: mail, phone: ph, message: f.message.value.trim()
            }).then(function () {
                form.setAttribute('hidden', ''); $('#pdv4Thanks').removeAttribute('hidden');
            }).catch(function () {
                btn.disabled = false;
                showFormError(errEl, 'Het versturen is niet gelukt. Probeer het opnieuw of bel ons.');
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
                btn.disabled = false; showFormError(err, 'Het versturen is niet gelukt. Probeer het opnieuw.');
            });
        });
    });

    /* ───────── Buurtkaart (Leaflet) ───────── */
    (function () {
        var el = $('#pdv4Leaflet');
        if (!el || !D.map.showMap || !window.L) { return; }
        var L = window.L, P = [D.project.lat, D.project.lng];
        var m = L.map(el, { scrollWheelZoom: false }).setView(P, 15);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap-bijdragers', maxZoom: 19 }).addTo(m);
        function pin(text) {
            return L.divIcon({ className: '', iconSize: [30, 30], iconAnchor: [15, 15], html: '<div class="pdv4-pin">' + esc(text) + '</div>' });
        }
        var projectIcon = L.divIcon({ className: '', iconSize: [52, 52], iconAnchor: [26, 26],
            html: '<div class="pdv4-pin pdv4-pin--project"><img src="' + esc(D.map.logo) + '" alt=""></div>' });
        L.marker(P, { icon: projectIcon, zIndexOffset: 1000 }).addTo(m)
            .bindPopup('<b>' + esc(D.project.name) + '</b><br>' + esc(D.project.address));

        var groups = {};
        D.map.cats.forEach(function (c) { groups[c.id] = L.layerGroup(); if (c.defaultOn) { state.layers.push(c.id); } });
        D.map.pois.forEach(function (p) {
            var cat = D.map.cats.filter(function (c) { return c.id === p.cat; })[0];
            if (!cat || !groups[p.cat]) { return; }
            groups[p.cat].addLayer(L.marker([p.lat, p.lng], { icon: pin(cat.icon) }).bindPopup('<b>' + esc(p.name) + '</b><br>' + esc(p.dist) + ' · ' + esc(p.time)));
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
