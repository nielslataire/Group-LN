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

// Tabbladen Contract · Facturen · Leverancier · Portaaltoegang (design-handoff 14f-14h): alle vier staan in dezelfde
// pagina, de tabbar wisselt enkel welk paneel zichtbaar is. De gekozen tab staat in de url (#facturen), zodat een
// verversing, een uitnodiging of een gedeelde link op hetzelfde tabblad terechtkomt.
(function () {
    "use strict";
    var tabs = Array.prototype.slice.call(document.querySelectorAll(".js-gl-v2-pcd-tab"));
    var panels = Array.prototype.slice.call(document.querySelectorAll(".gl-v2-pcd-panel[data-tab-panel]"));
    if (!tabs.length || !panels.length) return;

    function show(key, focus) {
        if (!panels.some(function (p) { return p.getAttribute("data-tab-panel") === key; })) key = "contract";
        tabs.forEach(function (t) {
            var on = t.getAttribute("data-tab") === key;
            t.classList.toggle("is-active", on);
            t.setAttribute("aria-selected", on ? "true" : "false");
            t.tabIndex = on ? 0 : -1;
            if (on && focus) t.focus();
        });
        panels.forEach(function (p) { p.hidden = p.getAttribute("data-tab-panel") !== key; });
        try { history.replaceState(null, "", key === "contract" ? location.pathname + location.search : "#" + key); } catch (e) { }
    }

    tabs.forEach(function (t, i) {
        t.addEventListener("click", function () { show(t.getAttribute("data-tab")); });
        t.addEventListener("keydown", function (e) {
            var next = e.key === "ArrowRight" ? i + 1 : e.key === "ArrowLeft" ? i - 1 : -1;
            if (next < 0) return;
            e.preventDefault();
            var target = tabs[(next + tabs.length) % tabs.length];
            show(target.getAttribute("data-tab"), true);
        });
    });
    show((location.hash || "").replace("#", "") || "contract");
    // Een link naar een ander tabblad (bv. "#leverancier" in Vraagt actie) wisselt mee.
    window.addEventListener("hashchange", function () { var k = (location.hash || "").replace("#", ""); if (k) show(k); });

    // Filter op het tabblad Facturen: Alles · Open · Betaald. Afgekeurde facturen staan enkel onder Alles.
    var filter = document.querySelector(".js-gl-v2-pcd-inv-filter");
    if (filter) {
        filter.addEventListener("click", function (e) {
            var btn = e.target.closest("button[data-filter]");
            if (!btn) return;
            var wanted = btn.getAttribute("data-filter");
            Array.prototype.forEach.call(filter.querySelectorAll("button"), function (b) { b.classList.toggle("is-active", b === btn); });
            Array.prototype.forEach.call(document.querySelectorAll(".js-gl-v2-pcd-inv-row"), function (row) {
                row.hidden = wanted !== "alle" && row.getAttribute("data-group") !== wanted;
            });
        });
    }
})();

// Gsm (64j): "Werfleider kiezen" — de hoofdknop noemt de gekozen persoon en staat uit tot er een keuze is.
(function () {
    "use strict";
    document.querySelectorAll(".js-gl-v2-m-sm-form").forEach(function (form) {
        var btn = form.querySelector(".js-gl-v2-m-sm-submit");
        var label = form.querySelector(".js-gl-v2-m-sm-label");
        function sync() {
            var picked = form.querySelector('input[name="contactid"]:checked');
            btn.disabled = !picked;
            label.textContent = picked ? picked.getAttribute("data-first") + " als werfleider" : "Kies een werfleider";
        }
        form.addEventListener("change", sync);
        sync();
    });
})();

// Gsm (64k): factuurpillen (Alle · Te controleren · Te betalen · Betaald) en "Nog N facturen" / "Minder tonen".
(function () {
    "use strict";
    var card = document.getElementById("gl-v2-m-inv-card");
    if (!card) return;
    var rows = Array.prototype.slice.call(card.querySelectorAll("[data-m-inv]"));
    var pills = document.getElementById("gl-v2-m-inv-pills");
    var more = document.getElementById("gl-v2-m-inv-more");
    var LIMIT = 5, filter = "", expanded = false;
    function apply() {
        var match = rows.filter(function (r) { return !filter || r.getAttribute("data-group") === filter; });
        rows.forEach(function (r) { r.hidden = true; });
        var all = expanded || !!filter;
        match.forEach(function (r, i) { r.hidden = !(all || i < LIMIT); });
        if (more) {
            more.hidden = !!filter || match.length <= LIMIT;
            more.innerHTML = expanded ? 'Minder tonen<i class="ph ph-caret-up" aria-hidden="true"></i>' : 'Nog ' + (match.length - LIMIT) + ' facturen<i class="ph ph-caret-down" aria-hidden="true"></i>';
        }
    }
    if (pills) pills.addEventListener("click", function (e) {
        var p = e.target.closest(".gl-v2-pill");
        if (!p) return;
        filter = p.getAttribute("data-filter") || "";
        pills.querySelectorAll(".gl-v2-pill").forEach(function (x) { x.classList.toggle("is-active", x === p); });
        apply();
    });
    if (more) more.addEventListener("click", function () { expanded = !expanded; apply(); });
    apply();
})();
