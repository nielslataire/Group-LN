// gl-v2 layout-pilot — Klanten/DetailsV2.cshtml. Eigen pagina-JS, Klanten-variant van
// gl-v2-leveranciers-details.js. Veel kleiner dan die pagina: geen Contracten/Facturen-tabellen
// (dus geen DataTables/rij-···-menu/mobiele-kaarten nodig) — enkel de tabs en de verwijder-
// bevestiging (zelfde AJAX-fragment-in-lege-modal-shell-recept, hier met PartialDeleteClientModalV2/
// DeleteClient i.p.v. Leveranciers' ModalDeleteV2).
(function () {
    "use strict";

    var config = window.glV2ClientDetailsConfig || {};

    initTabs();
    initDeleteModal();

    // ── Tabs ────────────────────────────────────────────────────────────────────────────────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-client-details-tabbar");
        if (!tabbar) return;

        function activate(key) {
            tabbar.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (tab) {
                var isActive = tab.getAttribute("data-tab") === key;
                tab.classList.toggle("is-active", isActive);
                tab.setAttribute("aria-selected", isActive ? "true" : "false");
            });
            document.querySelectorAll(".gl-v2-tab-panel").forEach(function (panel) {
                panel.hidden = panel.getAttribute("data-tab-panel") !== key;
            });
        }

        tabbar.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab) return;
            activate(tab.getAttribute("data-tab"));
        });
    }

    // ── Verwijder-bevestiging (optie 4j TYPE 1, danger) — zelfde AJAX-fragment-in-lege-modal-shell
    //    als gl-v2-leveranciers-details.js se eigen initDeleteModal(), hier met het rode icoon-
    //    knopje in de topbar als trigger. ────────────────────────────────────────────────────────
    function initDeleteModal() {
        var trigger = document.querySelector(".js-gl-v2-delete-client");
        var modalEl = document.getElementById("gl-v2-delete-client-modal");
        var container = document.getElementById("gl-v2-delete-client-container");
        if (!trigger || !modalEl || !container || !window.bootstrap || !config.deleteUrl) return;

        var modal = new window.bootstrap.Modal(modalEl, { backdrop: "static", keyboard: false });

        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            container.innerHTML = '<div class="p-3 text-muted">Laden …</div>';
            fetch(config.deleteUrl + "?id=" + encodeURIComponent(config.clientId))
                .then(function (r) { return r.ok ? r.text() : Promise.reject(); })
                .then(function (html) { container.innerHTML = html; })
                .catch(function () { container.innerHTML = '<div class="p-3 text-danger">Kon klant niet laden.</div>'; });
            modal.show();
        });
    }
})();
