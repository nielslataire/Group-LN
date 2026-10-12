// WWWCOPRO v2 — shell + home behaviour. Plain JS, no jQuery/Porto plugins on purpose
// (that dependency is exactly what v2 is shedding — see WWWCOPRO/design-grouplnwww.md).
(function () {
    'use strict';

    if ('IntersectionObserver' in window) {
        document.documentElement.classList.add('js-reveal');
    }

    function initStickyHeader() {
        var header = document.querySelector('.v2-header');
        if (!header) return;
        var threshold = parseInt(header.getAttribute('data-sticky-start-at'), 10) || 0;
        if (threshold <= 0) {
            header.classList.add('is-solid');
            return;
        }
        function onScroll() {
            if (window.scrollY > threshold) {
                header.classList.add('is-scrolled');
            } else {
                header.classList.remove('is-scrolled');
            }
        }
        onScroll();
        window.addEventListener('scroll', onScroll, { passive: true });
    }

    function initNavOverlay() {
        var toggle = document.getElementById('v2NavToggle');
        var overlay = document.getElementById('v2NavOverlay');
        var backdrop = document.getElementById('v2NavBackdrop');
        var closeBtn = document.getElementById('v2NavClose');
        if (!toggle || !overlay) return;

        function open() {
            overlay.classList.add('is-open');
            if (backdrop) backdrop.classList.add('is-open');
            toggle.setAttribute('aria-expanded', 'true');
            overlay.setAttribute('aria-hidden', 'false');
            document.documentElement.classList.add('v2-overlay-locked');
            if (closeBtn) closeBtn.focus();
        }
        function close() {
            overlay.classList.remove('is-open');
            if (backdrop) backdrop.classList.remove('is-open');
            toggle.setAttribute('aria-expanded', 'false');
            overlay.setAttribute('aria-hidden', 'true');
            document.documentElement.classList.remove('v2-overlay-locked');
            toggle.focus();
        }
        toggle.addEventListener('click', function () {
            if (overlay.classList.contains('is-open')) { close(); } else { open(); }
        });
        if (closeBtn) closeBtn.addEventListener('click', close);
        if (backdrop) backdrop.addEventListener('click', close);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && overlay.classList.contains('is-open')) close();
        });
    }

    // Hero search bar — options come from WWWCOPRO.Helpers.HomeHeroDataHelper (real data, see
    // Areas/V2/Controllers/HomeController.vb). Submits straight to Projects/Index: each option's
    // data-value (or data-min/data-max for the price field) is copied into that field's hidden
    // input(s) on click, which the <form> then carries along as real querystring parameters.
    function initHeroSearch() {
        var toggleBtn = document.getElementById('v2SearchToggle');
        var searchBar = document.getElementById('v2HeroSearch');
        if (toggleBtn && searchBar) {
            toggleBtn.addEventListener('click', function () {
                var isOpen = searchBar.classList.toggle('is-open');
                toggleBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
            });
        }

        var fields = document.querySelectorAll('.v2-hero-search-field');
        fields.forEach(function (field) {
            var hiddenInputs = field.querySelectorAll('input[type="hidden"]');
            field.addEventListener('click', function (e) {
                var isOpen = field.classList.contains('is-open');
                fields.forEach(function (f) { f.classList.remove('is-open'); });
                if (!isOpen) field.classList.add('is-open');
                e.stopPropagation();
            });
            field.querySelectorAll('.v2-hero-search-dropdown li').forEach(function (option) {
                option.addEventListener('click', function (e) {
                    var valueEl = field.querySelector('.v2-hero-search-field-value');
                    if (valueEl) valueEl.firstChild.textContent = option.textContent.trim() + ' ';

                    if (hiddenInputs.length === 1) {
                        hiddenInputs[0].value = option.getAttribute('data-value') || '';
                    } else if (hiddenInputs.length === 2) {
                        // Prijs-veld: PriceMin + PriceMax, in die volgorde.
                        hiddenInputs[0].value = option.getAttribute('data-min') || '';
                        hiddenInputs[1].value = option.getAttribute('data-max') || '';
                    }

                    field.classList.remove('is-open');
                    e.stopPropagation();
                });
            });
        });
        document.addEventListener('click', function () {
            fields.forEach(function (f) { f.classList.remove('is-open'); });
        });
    }

    var prefersReducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Hero achtergrondvideo — poster per oriëntatie, bewegingsbewust afspelen (WCAG 2.2.2) en
    // een zichtbare pauze/afspeel-knop. Zelfde gedrag als Scripts/custom.js' initHeroVideo().
    function initHeroVideo() {
        var video = document.querySelector('#v2Hero .v2-hero-media');
        if (!video) return;
        var toggle = document.getElementById('v2HeroVideoToggle');

        var mqPortrait = window.matchMedia('(orientation: portrait)');
        function applyPoster() {
            var p = mqPortrait.matches ? video.getAttribute('data-poster-portrait') : video.getAttribute('data-poster');
            if (p) video.poster = p;
        }
        applyPoster();
        if (mqPortrait.addEventListener) mqPortrait.addEventListener('change', applyPoster);

        function play() {
            var p = video.play();
            if (p && p.catch) p.catch(function () { });
        }
        function syncToggle() {
            if (!toggle) return;
            toggle.hidden = false;
            var paused = video.paused;
            toggle.setAttribute('aria-label', paused ? 'Achtergrondvideo afspelen' : 'Achtergrondvideo pauzeren');
            var icon = toggle.querySelector('i');
            if (icon) icon.className = paused ? 'fa fa-play' : 'fa fa-pause';
        }

        if (!prefersReducedMotion) play();
        syncToggle();

        if (toggle) {
            toggle.addEventListener('click', function () {
                if (video.paused) { play(); } else { video.pause(); }
                syncToggle();
            });
        }
        video.addEventListener('play', syncToggle);
        video.addEventListener('pause', syncToggle);
    }

    // Uitgelicht project — video pas afspelen wanneer ze in beeld komt, nooit bij
    // prefers-reduced-motion. Zelfde gedrag als Scripts/custom.js' initFeaturedVideo().
    function initFeaturedVideo() {
        var video = document.querySelector('.v2-featured-media video');
        if (!video || prefersReducedMotion) return;

        function play() {
            var p = video.play();
            if (p && p.catch) p.catch(function () { });
        }

        if ('IntersectionObserver' in window) {
            var io = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) { play(); } else { video.pause(); }
                });
            }, { threshold: 0.25 });
            io.observe(video);
        } else {
            play();
        }
    }

    // Hero scroll-cue: enkel deze link smooth scrollen (geen globale scroll-behavior:smooth,
    // dat maakt normaal scrollen op de zware home-hero merkbaar traag).
    function initHeroScrollCue() {
        var cue = document.querySelector('.v2-hero-scroll-cue');
        if (!cue) return;
        cue.addEventListener('click', function (e) {
            var target = document.querySelector(cue.getAttribute('href'));
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({ behavior: prefersReducedMotion ? 'auto' : 'smooth' });
        });
    }

    function initScrollReveal() {
        var items = document.querySelectorAll('.v2-reveal');
        if (!items.length) return;
        if (!('IntersectionObserver' in window)) return;
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15 });
        items.forEach(function (item, i) {
            item.style.transitionDelay = Math.min(i, 4) * 100 + 'ms';
            observer.observe(item);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initStickyHeader();
        initNavOverlay();
        initHeroSearch();
        initHeroVideo();
        initFeaturedVideo();
        initHeroScrollCue();
        initScrollReveal();
    });
})();
