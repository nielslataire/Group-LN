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
// _ProjectFormTabs.cshtml, TrajectSjabloonAdmin/Edit inline); ProjectTraject/Index.cshtml wrapt
// 'm in .gl-traject-tabrow en regelt zijn eigen pin-logica in traject.index.js (extra .inner-menu-
// kolom op die pagina maakt de vaste 300px/73px hieronder daar onbetrouwbaar — zie de toelichting
// daar). position:sticky bleek voor deze negative-margin-uitbraak (custom.css) zichtbaar niet te
// werken (geen "vastklikken", verschoven rustpositie) — .gl-form-shell__actions liep al tegen
// hetzelfde aan en loste het op met fixed + een expliciete left-offset i.p.v. sticky; dezelfde
// aanpak hier, alleen ingeschakeld ZODRA de rustpositie toch al voorbij de topbar zou scrollen
// (i.p.v. permanent fixed), met een live gemeten spacer zodat de rest van de pagina niet opspringt.
(function () {
    "use strict";
    var topbarPx = Math.round(parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--topbar-height")) || 72);
    var mq = window.matchMedia("(min-width: 768px)");

    // :not(.gl-traject-tabs) sluit ProjectTraject/Index.cshtml's geneste gebruik uit — die zit al
    // in .gl-traject-tabrow (met .gl-traject-stats als broer, niet de tab-inhoudskaart) en heeft
    // zijn eigen pin-logica in traject.index.js; dit zou anders een tweede, verkeerd gekoppelde
    // pin-poging op datzelfde element starten.
    document.querySelectorAll(".gl-form-shell__tabs:not(.gl-traject-tabs)").forEach(function (row) {
        var next = row.nextElementSibling;
        if (!next) return;

        var sentinel = document.createElement("div");
        sentinel.setAttribute("aria-hidden", "true");
        sentinel.style.height = "0";
        row.parentNode.insertBefore(sentinel, row);

        var spacer = document.createElement("div");
        spacer.setAttribute("aria-hidden", "true");
        spacer.style.height = "0";
        row.parentNode.insertBefore(spacer, next);

        var pinned = false;

        function pin() {
            if (pinned || !mq.matches) return;
            var gap = next.getBoundingClientRect().top - row.getBoundingClientRect().top;
            spacer.style.height = gap + "px";
            row.classList.add("gl-is-pinned");
            pinned = true;
        }

        function unpin() {
            if (!pinned) return;
            row.classList.remove("gl-is-pinned");
            spacer.style.height = "0";
            pinned = false;
        }

        if ("IntersectionObserver" in window) {
            new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) { if (entry.isIntersecting) unpin(); else pin(); });
            }, { rootMargin: "-" + topbarPx + "px 0px 0px 0px", threshold: 0 }).observe(sentinel);
        }

        if (mq.addEventListener) {
            mq.addEventListener("change", function () { if (!mq.matches) unpin(); });
        }
    });
})();
