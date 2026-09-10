$(function () {

    // ============================================================
    // Helpers
    // ============================================================
    var FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

    function focusableWithin(container) {
        if (!container) { return []; }
        return Array.prototype.filter.call(container.querySelectorAll(FOCUSABLE), function (el) {
            return el.offsetWidth > 0 || el.offsetHeight > 0 || el === document.activeElement;
        });
    }

    var prefersReducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ============================================================
    // Hero achtergrondvideo — poster per oriëntatie, bewegingsbewust
    // afspelen (WCAG 2.2.2) en een zichtbare pauze/afspeel-knop.
    // ============================================================
    (function initHeroVideo() {
        var video = document.querySelector('#homeHero .home-hero-video');
        if (!video) { return; }
        var toggle = document.getElementById('heroVideoToggle');

        var mqPortrait = window.matchMedia('(orientation: portrait)');
        function applyPoster() {
            var p = mqPortrait.matches
                ? video.getAttribute('data-poster-portrait')
                : video.getAttribute('data-poster');
            if (p) { video.poster = p; }
        }
        applyPoster();
        if (mqPortrait.addEventListener) { mqPortrait.addEventListener('change', applyPoster); }

        function play() {
            var p = video.play();
            if (p && p.catch) { p.catch(function () { }); }
        }
        function syncToggle() {
            if (!toggle) { return; }
            toggle.hidden = false;
            var paused = video.paused;
            toggle.setAttribute('aria-label', paused ? 'Achtergrondvideo afspelen' : 'Achtergrondvideo pauzeren');
            var icon = toggle.querySelector('i');
            if (icon) { icon.className = paused ? 'fa fa-play' : 'fa fa-pause'; }
        }

        if (!prefersReducedMotion) { play(); }
        syncToggle();

        if (toggle) {
            toggle.addEventListener('click', function () {
                if (video.paused) { play(); } else { video.pause(); }
                syncToggle();
            });
        }
        video.addEventListener('play', syncToggle);
        video.addEventListener('pause', syncToggle);
    })();

    // ============================================================
    // Uitgelicht project — video pas laden/afspelen wanneer ze in
    // beeld komt, en nooit bij prefers-reduced-motion.
    // ============================================================
    (function initFeaturedVideo() {
        var video = document.querySelector('.featured-project-video');
        if (!video || prefersReducedMotion) { return; }

        function play() {
            var p = video.play();
            if (p && p.catch) { p.catch(function () { }); }
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
    })();

    // ============================================================
    // Hero scroll-cue: enkel deze klik smooth scrollen (geen globale
    // scroll-behavior:smooth, dat maakt normaal scrollen op de zware
    // home-hero merkbaar traag)
    // ============================================================
    $('.hero-scroll-cue').on('click', function (e) {
        var targetId = $(this).attr('href');
        var $target = $(targetId);
        if ($target.length) {
            e.preventDefault();
            $target.get(0).scrollIntoView({ behavior: prefersReducedMotion ? 'auto' : 'smooth' });
        }
    });

    // ============================================================
    // Hamburger nav-overlay (site-breed, markup zit in _Layout.vbhtml)
    // ============================================================
    var $navOverlay = $('#navOverlay');
    var navPanel = document.querySelector('.nav-overlay-panel');
    var navToggleBtn = document.getElementById('navOverlayToggle');
    var navBackgroundRegions = ['header', 'main', 'footer']
        .map(function (id) { return document.getElementById(id); })
        .filter(Boolean);
    var navLastFocus = null;

    function positionNavOverlayClose() {
        var hamburger = document.getElementById('navOverlayToggle');
        var closeBtn = document.getElementById('navOverlayClose');
        if (!hamburger || !closeBtn) { return; }
        var rect = hamburger.getBoundingClientRect();
        closeBtn.style.top = rect.top + 'px';
        closeBtn.style.left = rect.left + 'px';
        closeBtn.style.right = 'auto';
        closeBtn.style.width = rect.width + 'px';
        closeBtn.style.height = rect.height + 'px';
    }

    function positionNavOverlayPanel() {
        var panel = document.querySelector('.nav-overlay-panel');
        var hamburger = document.getElementById('navOverlayToggle');
        if (!panel || !hamburger) { return; }

        if (window.innerWidth >= 992) {
            var rect = hamburger.getBoundingClientRect();
            panel.style.top = (rect.bottom + 12) + 'px';
            panel.style.right = (window.innerWidth - rect.right) + 'px';
        } else {
            panel.style.top = '';
            panel.style.right = '';
        }
    }

    function setBackgroundInert(on) {
        navBackgroundRegions.forEach(function (el) {
            if (on) {
                el.setAttribute('inert', '');
                el.setAttribute('aria-hidden', 'true');
            } else {
                el.removeAttribute('inert');
                el.removeAttribute('aria-hidden');
            }
        });
    }

    function openNavOverlay() {
        navLastFocus = document.activeElement;
        positionNavOverlayClose();
        positionNavOverlayPanel();
        $navOverlay.addClass('is-open').attr('aria-hidden', 'false');
        $('#navOverlayToggle').attr('aria-expanded', 'true').attr('aria-label', 'Menu sluiten');
        $('body').addClass('nav-overlay-locked');
        setBackgroundInert(true);

        var focusables = focusableWithin(navPanel);
        var first = document.getElementById('navOverlayClose') || focusables[0];
        if (first) { first.focus(); }
    }

    function closeNavOverlay() {
        $navOverlay.removeClass('is-open').attr('aria-hidden', 'true');
        $('#navOverlayToggle').attr('aria-expanded', 'false').attr('aria-label', 'Menu openen');
        $('body').removeClass('nav-overlay-locked');
        setBackgroundInert(false);
        if (navLastFocus && typeof navLastFocus.focus === 'function') {
            navLastFocus.focus();
        } else if (navToggleBtn) {
            navToggleBtn.focus();
        }
        navLastFocus = null;
    }

    $('#navOverlayToggle').on('click', function () {
        if ($navOverlay.hasClass('is-open')) {
            closeNavOverlay();
        } else {
            openNavOverlay();
        }
    });

    $('#navOverlayClose, .nav-overlay-backdrop, [data-nav-overlay-close]').on('click', function () {
        closeNavOverlay();
    });

    // Focus vasthouden binnen het open menu
    if (navPanel) {
        navPanel.addEventListener('keydown', function (e) {
            if (e.key !== 'Tab' || !$navOverlay.hasClass('is-open')) { return; }
            var focusables = focusableWithin(navPanel);
            if (!focusables.length) { return; }
            var first = focusables[0];
            var last = focusables[focusables.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        });
    }

    $(document).on('keyup', function (e) {
        if (e.key === 'Escape' && $navOverlay.hasClass('is-open')) {
            closeNavOverlay();
        }
    });

    // ============================================================
    // Hero-zoekbalk (homepage): inklapbare filterbalk. De trigger
    // ("Zoek in het aanbod") staat naast de primaire CTA en klapt de
    // Regio/Prijs/Type-balk ter plekke open — op elk schermformaat.
    // ============================================================
    var $heroSearch = $('#homeHeroSearch');
    var heroSearchToggleEl = document.getElementById('heroSearchToggle');

    function openHeroSearch() {
        $heroSearch.addClass('is-open');
        if (heroSearchToggleEl) { heroSearchToggleEl.setAttribute('aria-expanded', 'true'); }
        var firstTrigger = $heroSearch.find('.hero-dropdown-trigger').get(0);
        if (firstTrigger) { firstTrigger.focus(); }
    }
    function closeHeroSearch(returnFocus) {
        $heroSearch.removeClass('is-open');
        if (heroSearchToggleEl) {
            heroSearchToggleEl.setAttribute('aria-expanded', 'false');
            if (returnFocus) { heroSearchToggleEl.focus(); }
        }
        heroDropdowns.forEach(function (d) { d.close(); });
    }

    $('#heroSearchToggle').on('click', function () {
        if ($heroSearch.hasClass('is-open')) { closeHeroSearch(false); } else { openHeroSearch(); }
    });

    $heroSearch.on('keydown', function (e) {
        if (e.key === 'Escape' && $heroSearch.hasClass('is-open')) {
            var anyDropdownOpen = heroDropdowns.some(function (d) { return d.isOpen(); });
            if (!anyDropdownOpen) {
                e.stopPropagation();
                closeHeroSearch(true);
            }
        }
    });

    // ── Herbruikbare custom dropdown voor de hero-zoekbalk ──
    // Toetsenbord: Enter/Spatie/pijl opent; in het menu bewegen pijlen de
    // actieve optie, Home/End springen, Enter/Spatie kiest, Esc sluit.
    function initHeroDropdown(rootId, hiddenInputId) {
        var dropdown = document.getElementById(rootId);
        if (!dropdown) { return null; } // bv. Type-veld niet gerenderd (ShowTypeField=False)

        var trigger = dropdown.querySelector('.hero-dropdown-trigger');
        var menu = dropdown.querySelector('.hero-dropdown-menu');
        var valueLabel = dropdown.querySelector('.hero-dropdown-value');
        var hiddenInput = document.getElementById(hiddenInputId);
        var options = Array.prototype.slice.call(menu.querySelectorAll('.hero-dropdown-option'));
        var activeIndex = 0;
        options.forEach(function (o, i) { if (o.classList.contains('is-selected')) { activeIndex = i; } });

        var menuId = rootId + '-menu';
        menu.id = menuId;
        menu.setAttribute('tabindex', '-1');
        trigger.setAttribute('aria-controls', menuId);
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        options.forEach(function (opt, i) { opt.id = rootId + '-opt-' + i; });

        function position() {
            var rect = dropdown.getBoundingClientRect();
            menu.style.left = rect.left + 'px';
            menu.style.width = rect.width + 'px';
            menu.style.bottom = (window.innerHeight - rect.top + 12) + 'px';
        }

        function setActive(i) {
            if (i < 0) { i = 0; }
            if (i > options.length - 1) { i = options.length - 1; }
            activeIndex = i;
            options.forEach(function (opt, idx) { opt.classList.toggle('is-active', idx === i); });
            menu.setAttribute('aria-activedescendant', options[i].id);
            options[i].scrollIntoView({ block: 'nearest' });
        }

        function open() {
            position();
            menu.classList.add('is-open');
            trigger.setAttribute('aria-expanded', 'true');
            setActive(activeIndex);
            menu.focus();
        }
        function close(focusTrigger) {
            menu.classList.remove('is-open');
            trigger.setAttribute('aria-expanded', 'false');
            if (focusTrigger) { trigger.focus(); }
        }
        function isOpen() { return menu.classList.contains('is-open'); }

        function choose(opt) {
            if (hiddenInput) { hiddenInput.value = opt.getAttribute('data-value'); }
            if (valueLabel) { valueLabel.textContent = opt.textContent; }
            options.forEach(function (o) {
                o.classList.remove('is-selected');
                o.setAttribute('aria-selected', 'false');
            });
            opt.classList.add('is-selected');
            opt.setAttribute('aria-selected', 'true');
        }

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            if (isOpen()) { close(); } else { open(); }
        });

        trigger.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
                e.preventDefault();
                if (!isOpen()) { open(); }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                if (!isOpen()) { open(); setActive(options.length - 1); }
            }
        });

        menu.addEventListener('keydown', function (e) {
            switch (e.key) {
                case 'ArrowDown': e.preventDefault(); setActive(activeIndex + 1); break;
                case 'ArrowUp': e.preventDefault(); setActive(activeIndex - 1); break;
                case 'Home': e.preventDefault(); setActive(0); break;
                case 'End': e.preventDefault(); setActive(options.length - 1); break;
                case 'Enter':
                case ' ':
                case 'Spacebar':
                    e.preventDefault();
                    choose(options[activeIndex]);
                    close(true);
                    break;
                case 'Escape':
                    e.preventDefault();
                    e.stopPropagation();
                    close(true);
                    break;
                default:
                    break;
            }
        });

        // Focus verlaat de dropdown (bv. via Tab) -> menu sluiten zonder focus te stelen
        menu.addEventListener('focusout', function (e) {
            if (!dropdown.contains(e.relatedTarget)) { close(false); }
        });

        menu.addEventListener('click', function (e) {
            var opt = e.target.closest('.hero-dropdown-option');
            if (!opt) { return; }
            choose(opt);
            setActive(options.indexOf(opt));
            close(true);
        });

        return {
            element: dropdown,
            isOpen: isOpen,
            position: position,
            close: function () { close(false); }
        };
    }

    var heroDropdowns = [
        initHeroDropdown('heroRegioDropdown', 'heroSearchGemeente'),
        initHeroDropdown('heroPrijsDropdown', 'heroSearchPrice'),
        initHeroDropdown('heroTypeDropdown', 'heroSearchUnitCategory')
    ].filter(function (d) { return d !== null; });

    $(window).on('resize', function () {
        if ($navOverlay.hasClass('is-open')) {
            positionNavOverlayClose();
            positionNavOverlayPanel();
        }
        heroDropdowns.forEach(function (d) {
            if (d.isOpen()) { d.position(); }
        });
    });

    $(document).on('click', function (e) {
        heroDropdowns.forEach(function (d) {
            if (d.isOpen() && !$(e.target).closest(d.element).length) {
                d.close();
            }
        });
    });

    // Hero-zoekbalk (homepage): combineert Regio/Prijs/Eenheidstype tot één querystring
    $('#heroSearchForm').on('submit', function (e) {
        e.preventDefault();

        var params = {};

        var gemeente = $('#heroSearchGemeente').val();
        if (gemeente) { params.Gemeente = gemeente; }

        var priceVal = $('#heroSearchPrice').val(); // "min,max" — lege kant = geen grens
        if (priceVal) {
            var parts = priceVal.split(',');
            if (parts[0]) { params.PriceMin = parts[0]; }
            if (parts[1]) { params.PriceMax = parts[1]; }
        }

        var unitCategory = $('#heroSearchUnitCategory').val();
        if (unitCategory) { params.UnitCategory = unitCategory; }

        var qs = $.param(params);
        var baseUrl = $(this).attr('action');
        window.location.href = baseUrl + (qs ? '?' + qs : '');
    });

});
