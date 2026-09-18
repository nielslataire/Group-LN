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

    // Iconenmigratie Boxicons -> Phosphor (DESIGN.md "Icons"): dit script "corrigeerde" elk
    // icoon in een Acties-kolom dat niet met bx- begint naar een bx-*-equivalent (of, zonder fa-
    // klasse gevonden, stil terug naar het generieke bx-dots-horizontal-rounded) — precies wat
    // een migrerende pagina's ph-* iconen (bv. ph-note-pencil, ph-trash) overkwam: geen fa-klasse
    // om op te matchen, dus stille vervanging door drie puntjes. ph-* telt nu ook als "al goed",
    // zelfde behandeling als bx-* (enkel de fs-5-sizingklasse erbij, verder met rust laten).
    const replaceWithBoxIcon = ($icon) => {
        const classList = ($icon.attr('class') || '').split(/\s+/);
        const hasIconFont = classList.some((cls) => cls.startsWith('bx') || cls === 'ph' || cls.startsWith('ph-'));

        if (hasIconFont) {
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
            // Desktop-only correctie/pin — zie de uitgebreide toelichting bij dezelfde guard in
            // de .gl-traject-tabrow-IIFE verderop in dit bestand: --topbar-height blijft op
            // mobiel de desktop-waarde, dus zonder deze guard trekt transform de rij hier ook
            // fors omhoog.
            if (!mq.matches) return;
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

// Dezelfde blijft-zichtbaar-tijdens-scrollen-logica als hierboven, maar voor de
// .gl-traject-tabrow-variant (tabstrip + compacte statuspillen op één rij, binnen een
// .content-with-menu-schil met een extra .inner-menu (DetailMenu) kolom naast .inner-body).
// Die extra kolom is precies waarom dit GEEN vaste 300px/73px kan hardcoden zoals de generieke
// versie hierboven — .inner-menu's breedte/status verschilt per sidebar-variant
// (sidebar-left-sm/-xs, ingeklapt) en schuift dus .inner-body's eigen linkerrand mee, dus wordt
// live van .inner-body's getBoundingClientRect() afgelezen i.p.v. hardcoded. Oorspronkelijk enkel
// ProjectTraject/Index.cshtml's eigen initTabrowPin() (traject.index.js) — hierheen verplaatst
// (project-wide, elke pagina met deze markup) toen ProjectDossiers/Index.cshtml dezelfde
// .gl-traject-tabrow kreeg zonder deze JS geladen te hebben (dossiers.js regelt enkel tab-klik/
// toetsenbord, geen affix), waardoor de rij daar 10px te laag stond (geen .gl-traject-flush op
// .content-body — zie de CSS-toelichting bij .gl-traject-tabrow in traject.css) én nooit ging
// pinnen bij scrollen.
(function () {
    "use strict";
    var mq = window.matchMedia("(min-width: 768px)");

    function topbarPx() {
        var v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--topbar-height"));
        return isNaN(v) ? 72 : v;
    }

    document.querySelectorAll(".gl-traject-tabrow").forEach(function (row) {
        var innerBody = row.closest(".inner-body");
        var card = row.nextElementSibling;
        if (!innerBody || !card) return;

        var spacer = document.createElement("div");
        spacer.setAttribute("aria-hidden", "true");
        spacer.style.height = "0";
        row.parentNode.insertBefore(spacer, card);

        var pinned = false;
        var restTop = 0;
        var rowHeight = 0;
        var ticking = false;

        function measureRest() {
            row.style.transform = "";
            var r = row.getBoundingClientRect();
            restTop = r.top + window.scrollY;
            rowHeight = r.height;
            // Desktop-only, zelfde reden als de generieke .gl-form-shell__tabs-versie hierboven.
            if (!mq.matches) return;
            var delta = topbarPx() - r.top;
            if (Math.abs(delta) > 0.5) row.style.transform = "translateY(" + delta + "px)";
        }

        function syncHorizontal() {
            var r = innerBody.getBoundingClientRect();
            row.style.left = r.left + "px";
            row.style.width = r.width + "px";
        }

        function pin() {
            if (pinned) return;
            spacer.style.height = rowHeight + "px";
            row.style.transform = "";
            syncHorizontal();
            row.classList.add("gl-is-pinned");
            pinned = true;
        }

        function unpin() {
            if (!pinned) return;
            row.classList.remove("gl-is-pinned");
            row.style.left = "";
            row.style.width = "";
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
            if (pinned) syncHorizontal();
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
        // Sidebar-inklap/uitklap en het openen/sluiten van .inner-menu wijzigen enkel html's
        // class-attribuut, geen resize-event — MutationObserver vangt die live op.
        new MutationObserver(function () {
            if (pinned) syncHorizontal(); else measureRest();
        }).observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
    });
})();

// Generieke busy-state op modal-formulieren: elke <form> binnen een .modal met een submit-knop
// krijgt automatisch een spinner + disabled state zodra "submit" vuurt (dus na geslaagde HTML5-
// validatie, vóór de round-trip). Voorkomt een dubbele klik/dubbele POST op een trage verbinding
// en geeft de gebruiker zichtbare bevestiging dat de klik geregistreerd is — project-wide i.p.v.
// per modal herhaald, zodat nieuwe modals dit gratis krijgen.
(function () {
    "use strict";
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement) || !form.closest(".modal")) return;
        var btn = form.querySelector('button[type="submit"]');
        if (!btn || btn.disabled) return;
        btn.dataset.busyLabel = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + btn.textContent.trim();
    });
})();
