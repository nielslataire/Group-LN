// gl-v2 layout-pilot — Projecten/DetailContractV2 (design-handoff punt 14b). Only page-specific
// piece: the delete-confirmation modal, same AJAX-fragment-in-empty-modal-shell recipe as every
// other gl-v2 delete modal this pass (Klanten/DetailClientsV2, Projecten/DetailContractsV2). The
// ⋯-menu is the generic .js-gl-v2-menu-trigger/.gl-v2-menu/initContextMenus() from gl-v2-shell.js.
(function () {
    "use strict";
    var config = window.glV2ProjectenContractDetailConfig || {};

    var deleteModalElement = document.getElementById("gl-v2-pcd-delete-modal");
    var deleteModal = deleteModalElement
        ? new bootstrap.Modal(deleteModalElement, { backdrop: "static", keyboard: false })
        : null;

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-gl-v2-pcd-delete");
        if (!trigger) return;
        e.preventDefault();
        var id = trigger.getAttribute("data-id");
        var container = document.getElementById("gl-v2-pcd-delete-container");
        container.innerHTML = '<div class="p-3 text-muted">Laden…</div>';
        fetch(config.deleteUrl + "?id=" + encodeURIComponent(id))
            .then(function (r) { return r.text(); })
            .then(function (html) { container.innerHTML = html; })
            .catch(function () { container.innerHTML = '<div class="p-3 text-danger">Kon contract niet laden.</div>'; });
        if (deleteModal) deleteModal.show();
    });

    // Werfleider aannemer (design-handoff 14i): gsm of e-mail kopiëren; het icoon wordt kort een vinkje.
    document.addEventListener("click", function (e) {
        var btn = e.target.closest(".js-gl-v2-pcd-copy");
        if (!btn) return;
        var text = btn.getAttribute("data-copy") || "";
        var done = function () {
            var icon = btn.querySelector("i");
            if (!icon) return;
            icon.className = "ph ph-check";
            setTimeout(function () { icon.className = "ph ph-copy"; }, 1400);
        };
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(done, function () {});
        } else {
            var ta = document.createElement("textarea");
            ta.value = text; document.body.appendChild(ta); ta.select();
            try { document.execCommand("copy"); done(); } catch (err) {}
            document.body.removeChild(ta);
        }
    });

    // Bij >1 contract (design-handoff 14e): elk contract is een eigen collapsible paneel. Enkel
    // een klik/toets op dít paneel se eigen header togglet — geen accordion (meerdere mogen open
    // staan), zelfde onafhankelijke-panelen-keuze als 14e se eigen "klik op een rij".
    document.addEventListener("click", function (e) {
        var header = e.target.closest(".js-gl-v2-pcd-contract-toggle");
        if (!header) return;
        var panel = header.closest(".gl-v2-pcd-contract-panel");
        if (!panel) return;
        var expanded = header.getAttribute("aria-expanded") === "true";
        header.setAttribute("aria-expanded", expanded ? "false" : "true");
        panel.classList.toggle("is-expanded", !expanded);
    });
})();
