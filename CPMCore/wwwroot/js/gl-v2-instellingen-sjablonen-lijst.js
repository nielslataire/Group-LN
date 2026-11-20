/* gl-v2 — Instellingen › Sjablonen (lijsten van planning, schijven en project): vult het gedeelde ⋯-menu (.gl-v2-menu) per rij en stuurt de verborgen formulieren door.
   De shell opent en positioneert het menu (js-gl-v2-menu-trigger + aria-controls); wij zetten de inhoud eerst, in de capture-fase van dezelfde klik. */
(function () {
    "use strict";
    var menu = document.getElementById("sj-lijst-menu");
    if (!menu) return;
    var cfg = window.SjLijst || {};
    var rij = null;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c]; }); }
    function item(act, icon, label, opts) {
        opts = opts || {};
        return "<button type=\"button\" class=\"gl-v2-menu-item" + (opts.danger ? " is-danger" : "") + (opts.uit ? " is-disabled" : "") + "\" role=\"menuitem\" data-menu=\"" + act + "\"" + (opts.uit ? " disabled aria-disabled=\"true\"" : "") + ">" +
            "<i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span>" +
            (opts.sub ? "<span class=\"gl-v2-menu-item-sub\">" + esc(opts.sub) + "</span>" : "") + "</span></button>";
    }
    function bouw(r) {
        var actief = r.getAttribute("data-actief") === "1", gebruikt = parseInt(r.getAttribute("data-gebruikt"), 10) || 0, h = "";
        h += item("bewerk", "ph-pencil-simple", "Bewerken");
        if (cfg.canWrite) {
            if (r.querySelector("form[data-actie=\"dupliceer\"]")) h += item("dupliceer", "ph-copy", "Dupliceren", { sub: "Kopie met dezelfde inhoud, nog op inactief" });
            h += item("actief", actief ? "ph-eye-slash" : "ph-eye", actief ? "Inactief zetten" : "Weer actief zetten", { sub: actief ? "Wordt niet meer aangeboden bij het toepassen" : "" });
        }
        if (cfg.canDelete) h += item("verwijder", "ph-trash", "Verwijderen", { danger: true, uit: gebruikt > 0, sub: gebruikt > 0 ? "In gebruik: zet het op inactief" : "" });
        menu.innerHTML = h;
    }

    // Capture-fase: loopt vóór de shell die het menu opent
    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-menu-trigger[aria-controls=\"sj-lijst-menu\"]");
        if (!trigger) return;
        rij = trigger.closest(".gl-v2-auto-row");
        if (rij) bouw(rij);
    }, true);

    menu.addEventListener("click", function (e) {
        var b = e.target.closest("[data-menu]");
        if (!b || b.disabled || !rij) return;
        var act = b.getAttribute("data-menu");
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        if (act === "bewerk") { window.location.href = rij.getAttribute("data-row-href"); return; }
        var vorm = rij.querySelector("form[data-actie=\"" + act + "\"]");
        if (vorm) { if (vorm.requestSubmit) vorm.requestSubmit(); else vorm.submit(); }
    });
})();
