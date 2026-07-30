$(function () {

    // Hamburger nav-overlay toggle (site-breed, markup zit in _Layout.vbhtml)
    var $navOverlay = $('#navOverlay');

    // Sluitknop exact op de plek van de hamburger-knop plaatsen (afmeting + positie
    // hangen af van headerhoogte/padding per breakpoint, dus we meten live i.p.v. te hardcoderen)
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

    // >991px: geen fullscreen paneel, maar een dropdown-menu onder de hamburger-knop —
    // top/right live berekend zodat de rechter-inset exact die van de hamburger-knop volgt.
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

    function openNavOverlay() {
        positionNavOverlayClose();
        positionNavOverlayPanel();
        $navOverlay.addClass('is-open').attr('aria-hidden', 'false');
        $('#navOverlayToggle').attr('aria-expanded', 'true');
        $('body').addClass('nav-overlay-locked');
    }

    function closeNavOverlay() {
        $navOverlay.removeClass('is-open').attr('aria-hidden', 'true');
        $('#navOverlayToggle').attr('aria-expanded', 'false');
        $('body').removeClass('nav-overlay-locked');
    }

    $('#navOverlayToggle').on('click', function () {
        if ($navOverlay.hasClass('is-open')) {
            closeNavOverlay();
        } else {
            openNavOverlay();
        }
    });

    $('#navOverlayClose, .nav-overlay-backdrop').on('click', function () {
        closeNavOverlay();
    });

    $(document).on('keyup', function (e) {
        if (e.key === 'Escape' && $navOverlay.hasClass('is-open')) {
            closeNavOverlay();
        }
    });

    // Hero-zoekbalk (homepage, gsm-formaat): vierkante toggle-knop opent/sluit de volledige zoekbox
    $('#heroSearchToggle').on('click', function () {
        var $search = $('#homeHeroSearch');
        var isOpen = $search.toggleClass('is-open').hasClass('is-open');
        $(this).attr('aria-expanded', isOpen ? 'true' : 'false');
        $(this).find('i')
            .toggleClass('fa-search', !isOpen)
            .toggleClass('fa-times', isOpen);
    });

    // Herbruikbare custom dropdown voor de hero-zoekbalk (Regio/Prijs/Type — opent boven de
    // zoekbalk). De zoekbalk zelf heeft overflow:hidden (voor haar eigen afgeronde hoeken),
    // dus het menu wordt position:fixed en live gepositioneerd i.p.v. relatief t.o.v. het veld.
    function initHeroDropdown(rootId, hiddenInputId) {
        var $dropdown = $('#' + rootId);
        if (!$dropdown.length) { return null; } // bv. Type-veld niet gerenderd (ShowTypeField=False)

        var $trigger = $dropdown.find('.hero-dropdown-trigger');
        var $menu = $dropdown.find('.hero-dropdown-menu');
        var $hiddenInput = $('#' + hiddenInputId);

        function position() {
            var rect = $dropdown[0].getBoundingClientRect();
            $menu.css({
                left: rect.left + 'px',
                width: rect.width + 'px',
                bottom: (window.innerHeight - rect.top + 12) + 'px'
            });
        }

        function close() {
            $menu.removeClass('is-open');
            $trigger.attr('aria-expanded', 'false');
        }

        $trigger.on('click', function (e) {
            e.stopPropagation();
            var willOpen = !$menu.hasClass('is-open');
            if (willOpen) {
                position();
            }
            $menu.toggleClass('is-open');
            $trigger.attr('aria-expanded', willOpen ? 'true' : 'false');
        });

        $menu.on('click', '.hero-dropdown-option', function () {
            var $option = $(this);
            $hiddenInput.val($option.data('value'));
            $trigger.find('.hero-dropdown-value').text($option.text());
            $menu.find('.hero-dropdown-option').removeClass('is-selected').attr('aria-selected', 'false');
            $option.addClass('is-selected').attr('aria-selected', 'true');
            close();
        });

        return {
            element: $dropdown[0],
            isOpen: function () { return $menu.hasClass('is-open'); },
            position: position,
            close: close
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
