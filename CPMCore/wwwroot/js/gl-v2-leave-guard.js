// gl-v2 — "Wijzigingen niet opslaan?" bij elke link die een formulierpagina verlaat (zijmenu/rail, kruimelpad,
// projectmenu, flyouts, Annuleren, terugpijl). Projectbreed, zonder pagina-JS: een pagina doet mee via de
// bestaande conventie — een dirty-badge (id eindigt op "dirty-badge", zichtbaar zodra er niet-opgeslagen
// wijzigingen zijn) en een bevestigingsmodal (.modal met een id dat "discard" bevat en op "-modal" eindigt)
// met een bevestigingslink (<a> met id "discard" … "-confirm"). Bij een link-klik terwijl de badge zichtbaar is,
// toont dit de eigen modal i.p.v. de browserdialoog; de bevestigingslink gaat dan naar de aangeklikte bestemming.
// Wie bevestigt, verbergt de badges zodat er geen browserwaarschuwing meer volgt.
(function () {
    "use strict";

    function isDirty() {
        var badges = document.querySelectorAll('[id$="dirty-badge"]');
        for (var i = 0; i < badges.length; i++) if (!badges[i].hidden) return true;
        return false;
    }
    function findModal() { return document.querySelector('.modal[id*="discard"][id$="-modal"]'); }

    document.addEventListener("click", function (e) {
        if (!isDirty() || !window.bootstrap) return;
        var a = e.target && e.target.closest ? e.target.closest("a[href]") : null;
        if (!a || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        if (a.target === "_blank" || a.hasAttribute("download") || a.hasAttribute("data-bs-toggle") || a.closest(".modal")) return;
        var href = a.getAttribute("href");
        if (!href || href.charAt(0) === "#" || /^javascript:/i.test(href)) return;
        var modal = findModal();
        var confirm = modal && modal.querySelector('a[id*="discard"][id$="-confirm"]');
        if (!modal || !confirm) return;   // pagina zonder eigen modal: laat de browser beslissen
        e.preventDefault();
        e.stopPropagation();
        confirm.href = a.href;
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
    }, true);

    document.addEventListener("click", function (e) {
        var c = e.target && e.target.closest ? e.target.closest('a[id*="discard"][id$="-confirm"]') : null;
        if (!c) return;
        document.querySelectorAll('[id$="dirty-badge"]').forEach(function (b) { b.hidden = true; });
    }, true);
})();
