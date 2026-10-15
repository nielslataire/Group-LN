// gl-v2 — Projecten/DetailPhotosV2: foto's binnen een sectie herschikken door te slepen. De volgorde wordt meteen bewaard
// (ProjectPictures.SortOrder, 1..n binnen de sectie). De publieke projectpagina gebruikt dezelfde volgorde voor de foto's
// van de sectie "Galerij" (hoofdfoto altijd eerst). Enkel actief bij de sortering "Eigen volgorde" en met schrijfrechten.
(function () {
    "use strict";
    var cfg = window.glV2DetailPhotos;
    if (!cfg || !cfg.canWrite || !cfg.urls || !cfg.urls.updateSortOrder) return;

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    token = token ? token.value : "";
    var sortInput = document.getElementById("gl-v2-dp-sort-value");
    var dragged = null;
    var startOrder = "";

    function isManualOrder() { return !sortInput || sortInput.value === "default"; }
    function cardsOf(grid) { return Array.prototype.slice.call(grid.querySelectorAll(".js-gl-v2-dp-card")); }
    function orderKey(grid) { return cardsOf(grid).map(function (c) { return c.getAttribute("data-id"); }).join(","); }

    function setDraggable() {
        var on = isManualOrder();
        document.querySelectorAll(".gl-v2-dp-grid .js-gl-v2-dp-card").forEach(function (c) { c.draggable = on; });
    }
    setDraggable();
    if (sortInput) sortInput.addEventListener("change", setDraggable);

    document.addEventListener("dragstart", function (e) {
        var card = e.target.closest && e.target.closest(".gl-v2-dp-grid .js-gl-v2-dp-card");
        if (!card || !isManualOrder()) return;
        dragged = card;
        startOrder = orderKey(card.parentNode);
        e.dataTransfer.effectAllowed = "move";
        try { e.dataTransfer.setData("text/plain", card.getAttribute("data-id")); } catch (err) { /* oudere browsers */ }
        setTimeout(function () { card.classList.add("is-dragging"); }, 0);
    });

    document.addEventListener("dragover", function (e) {
        if (!dragged) return;
        var over = e.target.closest && e.target.closest(".js-gl-v2-dp-card");
        if (!over || over === dragged || over.parentNode !== dragged.parentNode) return;
        e.preventDefault();
        var r = over.getBoundingClientRect();
        // volgorde in een rij: links van het midden = ervoor, rechts = erna
        var before = (e.clientX - r.left) < r.width / 2;
        var ref = before ? over : over.nextElementSibling;
        if (ref !== dragged) over.parentNode.insertBefore(dragged, ref);
    });

    document.addEventListener("drop", function (e) { if (dragged) e.preventDefault(); });

    document.addEventListener("dragend", function () {
        if (!dragged) return;
        var card = dragged, grid = card.parentNode;
        card.classList.remove("is-dragging");
        dragged = null;
        if (orderKey(grid) === startOrder) return;
        var sectionAttr = grid.getAttribute("data-section-grid");
        var sectionId = sectionAttr && sectionAttr !== "none" ? Number(sectionAttr) : null;
        var items = cardsOf(grid).map(function (c, i) {
            return { id: Number(c.getAttribute("data-id")), order: i + 1, sectionId: sectionId };
        });
        grid.classList.add("is-saving");
        fetch(cfg.urls.updateSortOrder, {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
            body: JSON.stringify({ items: items })
        }).then(function (r) {
            if (!r.ok) throw new Error(String(r.status));
        }).catch(function () {
            if (window.GlV2Dialog) {
                GlV2Dialog.alert({ tone: "danger", icon: "ph-warning-circle", title: "Volgorde niet bewaard", description: "De nieuwe volgorde kon niet bewaard worden. Herlaad de pagina en probeer het opnieuw." });
            }
        }).then(function () { grid.classList.remove("is-saving"); });
    });
})();
