// gl-v2 — gedeeld open/dicht van het filterpaneel (#filters-panel) op lijstpagina's (Klanten, Leveranciers, Projecten, Wijzigingsopdrachten, Detailclients).
// Markup-contract (zie gl-v2/filters.css): #filters-toggle (toolbar) en #mobile-filters-toggle (topbar, <768px) openen/sluiten het paneel; op mobiel is het
// paneel een bottom sheet met #filters-panel-backdrop en sluit #filters-show-btn ("Toon N …") het; Esc sluit. Paginaspecifiek blijft: welke filters er zijn,
// de chips/teller (#filters-toggle-count, #filters-sheet-badge) en het aantal op #filters-show-count.
// Delegatie op document: geen volgorde-afhankelijkheid met de pagina-JS. Pagina-JS kan het paneel sluiten via window.GlV2Filters.setOpen(false).
(function () {
    "use strict";
    var TOGGLES = ["filters-toggle", "mobile-filters-toggle"];

    function panel() { return document.getElementById("filters-panel"); }

    function setOpen(isOpen) {
        var p = panel();
        if (!p) return;
        p.hidden = !isOpen;
        var backdrop = document.getElementById("filters-panel-backdrop");
        if (backdrop) backdrop.hidden = !isOpen;
        TOGGLES.forEach(function (id) {
            var btn = document.getElementById(id);
            if (!btn) return;
            btn.classList.toggle("is-open", isOpen);
            btn.setAttribute("aria-expanded", isOpen.toString());
        });
    }

    document.addEventListener("click", function (e) {
        var p = panel();
        if (!p || !e.target.closest) return;
        if (e.target.closest("#filters-toggle, #mobile-filters-toggle")) { setOpen(p.hidden); return; }
        if (e.target.closest("#filters-panel-backdrop, #filters-show-btn")) setOpen(false);
    });
    document.addEventListener("keydown", function (e) {
        var p = panel();
        if (e.key === "Escape" && p && !p.hidden) setOpen(false);
    });

    window.GlV2Filters = { setOpen: setOpen };
})();
