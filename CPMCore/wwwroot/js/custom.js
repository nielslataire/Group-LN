///* Add here all your JS customizations */
//jQuery(function ($) {
//    $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults
//});

jQuery(function ($) {
    const iconMap = {
        'fa-eye': 'bx-eye',
        'fa-eye-slash': 'bx-hide',
        'fa-search': 'bx-show',
        'fa-edit': 'bx-edit',
        'fa-pencil': 'bx-pencil',
        'fa-pen': 'bx-pencil',
        'fa-trash': 'bx-trash',
        'fa-trash-alt': 'bx-trash',
        'fa-times': 'bx-x',
        'fa-times-circle': 'bx-x-circle',
        'fa-check': 'bx-check',
        'fa-check-circle': 'bx-check-circle',
        'fa-plus': 'bx-plus',
        'fa-plus-circle': 'bx-plus-circle',
        'fa-minus': 'bx-minus',
        'fa-download': 'bx-download',
        'fa-upload': 'bx-upload',
        'fa-file': 'bx-file',
        'fa-file-pdf': 'bx-file',
        'fa-file-excel': 'bx-file',
        'fa-print': 'bx-printer',
        'fa-info-circle': 'bx-info-circle',
        'fa-exclamation-triangle': 'bx-error',
        'fa-user': 'bx-user',
        'fa-users': 'bx-group',
        'fa-envelope': 'bx-envelope',
        'fa-phone': 'bx-phone',
        'fa-calendar': 'bx-calendar',
        'fa-sync': 'bx-refresh',
        'fa-redo': 'bx-redo',
        'fa-undo': 'bx-undo'
    };

    const normalizeIcon = (iconClass) => iconClass?.trim().toLowerCase();

    const replaceWithBoxIcon = ($icon) => {
        const classList = ($icon.attr('class') || '').split(/\s+/);
        const hasBoxIcon = classList.some((cls) => cls.startsWith('bx'));

        if (hasBoxIcon) {
            $icon.addClass('fs-5');
            return;
        }

        const faClass = classList.find((cls) => cls.startsWith('fa-'));
        const normalizedFa = normalizeIcon(faClass);
        const boxIcon = iconMap[normalizedFa] || 'bx-dots-horizontal-rounded';

        $icon.attr('class', `bx ${boxIcon} fs-5`);
    };

    const applyActionColumnStyles = (table) => {
        const $table = $(table);
        const $headers = $table.find('thead th');
        const actionIndex = $headers.toArray().findIndex((header) => $(header).text().trim().toLowerCase() === 'acties');

        if (actionIndex === -1) {
            return;
        }

        $headers.eq(actionIndex).addClass('datatable-actions');
        $table.find('tbody tr').each((_, row) => {
            const $cell = $(row).children().eq(actionIndex);
            $cell.addClass('datatable-actions');
            $cell.find('i').each((_, icon) => replaceWithBoxIcon($(icon)));
        });
    };

    const applyToAllTables = () => {
        $('table').each((_, table) => applyActionColumnStyles(table));
    };

    applyToAllTables();
    $(document).on('draw.dt', 'table.dataTable', function () {
        applyActionColumnStyles(this);
    });
});

// Formulierschil-tabstrip (.gl-form-shell__tabs) blijft zichtbaar tijdens scrollen — project-wide
// gevraagd. Werkt op elke pagina die de klasse rechtstreeks gebruikt (Projecten/Edit via
// _ProjectFormTabs.cshtml, TrajectSjabloonAdmin/Edit inline); ProjectTraject/Index.cshtml wrapt 'm
// in .gl-traject-tabrow en heeft zijn eigen, functioneel identieke pin-logica in traject.index.js
// (die pagina heeft een extra .inner-menu-kolom waardoor de vaste 300px/73px hieronder daar niet
// klopt — zie de toelichting daar). position:sticky bleek hier, net als bij .gl-traject-tabrow,
// niet te werken (geen vastklikken, zichtbare kloof t.o.v. de topbar ondanks een op papier kloppende
// -50px-marge) — in plaats van nog een keer op herrekende pixels te vertrouwen, meet dit de
// werkelijk gerenderde rustpositie en corrigeert 'm live (transform), en klikt synchroon op het
// scroll-event vast (geen IntersectionObserver: die vuurt async, één frame te laat, en gaf de eerder
// gemelde "korte sprong").
(function () {
    "use strict";
    var mq = window.matchMedia("(min-width: 768px)");

    function topbarPx() {
        var v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--topbar-height"));
        return isNaN(v) ? 72 : v;
    }

    // :not(.gl-traject-tabs) sluit ProjectTraject/Index.cshtml's geneste gebruik uit — zie hierboven.
    document.querySelectorAll(".gl-form-shell__tabs:not(.gl-traject-tabs)").forEach(function (row) {
        var next = row.nextElementSibling;
        if (!next) return;

        var spacer = document.createElement("div");
        spacer.setAttribute("aria-hidden", "true");
        spacer.style.height = "0";
        row.parentNode.insertBefore(spacer, next);

        var pinned = false;
        var restTop = 0;
        var rowHeight = 0;
        var ticking = false;

        function measureRest() {
            row.style.transform = "";
            var r = row.getBoundingClientRect();
            restTop = r.top + window.scrollY;
            rowHeight = r.height;
            var delta = topbarPx() - r.top;
            if (Math.abs(delta) > 0.5) row.style.transform = "translateY(" + delta + "px)";
        }

        function pin() {
            if (pinned) return;
            spacer.style.height = rowHeight + "px";
            row.style.transform = "";
            row.classList.add("gl-is-pinned");
            pinned = true;
        }

        function unpin() {
            if (!pinned) return;
            row.classList.remove("gl-is-pinned");
            spacer.style.height = "0";
            pinned = false;
            measureRest();
        }

        function update() {
            ticking = false;
            if (!mq.matches) { if (pinned) unpin(); return; }
            var shouldPin = window.scrollY + topbarPx() >= restTop;
            if (shouldPin && !pinned) pin();
            else if (!shouldPin && pinned) unpin();
        }

        function requestUpdate() {
            if (ticking) return;
            ticking = true;
            requestAnimationFrame(update);
        }

        measureRest();
        update();
        window.addEventListener("scroll", requestUpdate, { passive: true });
        window.addEventListener("resize", function () {
            if (!pinned) measureRest();
            requestUpdate();
        });
        new MutationObserver(function () {
            if (!pinned) measureRest();
        }).observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
    });
})();
