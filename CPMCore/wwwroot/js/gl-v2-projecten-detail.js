// gl-v2 layout-pilot — Projecten/DetailV2 (design-handoff punt 12a). Two independent, small filters,
// ported from the legacy Detail.cshtml's inline <script> (category chips on "Vraagt actie",
// eenheid/klant search on the units table) — same behavior, gl-v2 class names.
(function () {
    "use strict";

    initAttentionFilters();
    initUnitsFilter();

    function initAttentionFilters() {
        var card = document.querySelector(".gl-v2-pd-attention-card");
        if (!card) return;
        var chips = card.querySelectorAll(".js-gl-v2-pd-chip");
        var body = card.querySelector(".js-gl-v2-pd-attention-body");
        var status = document.getElementById("gl-v2-pd-filter-status");

        // In-/uitklappen: ingeklapt blijft enkel de kopband met de samenvatting over.
        var toggle = card.querySelector(".js-gl-v2-pd-att-toggle");
        if (toggle) {
            toggle.addEventListener("click", function () {
                var collapsed = card.classList.toggle("is-collapsed");
                toggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
                toggle.setAttribute("aria-label", collapsed ? "Uitklappen" : "Inklappen");
                toggle.setAttribute("title", collapsed ? "Uitklappen" : "Inklappen");
                var icon = toggle.querySelector("i");
                if (icon) icon.className = "ph " + (collapsed ? "ph-caret-down" : "ph-caret-up");
                var open = card.querySelector(".js-gl-v2-pd-att-sub-open");
                var closed = card.querySelector(".js-gl-v2-pd-att-sub-closed");
                if (open) open.hidden = collapsed;
                if (closed) closed.hidden = !collapsed;
            });
        }

        // Bundel open/dicht (3 of meer meldingen van hetzelfde type).
        card.querySelectorAll(".js-gl-v2-pd-att-bundle").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var wrap = btn.closest(".gl-v2-pd-att-bundle");
                var sub = wrap && wrap.querySelector(".gl-v2-pd-att-sub");
                if (!sub) return;
                var open = btn.getAttribute("aria-expanded") !== "true";
                btn.setAttribute("aria-expanded", open ? "true" : "false");
                btn.setAttribute("aria-label", open ? "Lijst inklappen" : "Lijst uitklappen");
                btn.setAttribute("title", open ? "Lijst inklappen" : "Lijst uitklappen");
                sub.hidden = !open;
            });
        });

        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                var cat = chip.getAttribute("data-cat");

                if (body) {
                    body.classList.add("is-refiltering");
                    window.setTimeout(function () { body.classList.remove("is-refiltering"); }, 140);
                    // Categoriewissel begint weer bovenaan de (mogelijk scrollende) lijst.
                    body.scrollTop = 0;
                }

                chips.forEach(function (c) {
                    var active = c === chip;
                    c.classList.toggle("is-active", active);
                    c.setAttribute("aria-selected", active ? "true" : "false");
                });

                var items = card.querySelectorAll(".js-gl-v2-pd-attention-item");
                var shown = 0;
                items.forEach(function (item) {
                    var matches = cat === "alle" || item.getAttribute("data-cat") === cat;
                    item.hidden = !matches;
                    if (matches) shown += parseInt(item.getAttribute("data-count") || "1", 10);
                });

                card.querySelectorAll(".js-gl-v2-pd-attention-group").forEach(function (group) {
                    var visible = 0;
                    group.querySelectorAll(".js-gl-v2-pd-attention-item").forEach(function (item) {
                        if (!item.hidden) visible += parseInt(item.getAttribute("data-count") || "1", 10);
                    });
                    group.hidden = visible === 0;
                    var n = group.querySelector(".js-gl-v2-pd-sec-n");
                    if (n) n.textContent = String(visible);
                });

                if (status) {
                    status.textContent = (cat === "alle" ? "Alle meldingen" : "Categorie " + cat)
                        + ": " + shown + (shown === 1 ? " melding" : " meldingen") + " getoond.";
                }
            });
        });
    }

    function initUnitsFilter() {
        var input = document.getElementById("gl-v2-pd-units-filter");
        if (!input) return;
        var scroll = input.closest(".gl-v2-section-card")?.querySelector(".gl-v2-pd-units-scroll");
        var tbody = scroll?.querySelector("tbody");
        if (!tbody) return;
        var rows = Array.prototype.slice.call(tbody.querySelectorAll("tr[data-unit-row]"));
        var noResults = document.getElementById("gl-v2-pd-units-empty");
        var status = document.getElementById("gl-v2-pd-units-filter-status");

        input.addEventListener("input", function () {
            var q = input.value.trim().toLowerCase();
            var anyVisible = false;
            var shown = 0;
            rows.forEach(function (row) {
                var hay = (row.getAttribute("data-search") || "").toLowerCase();
                var match = q === "" || hay.indexOf(q) !== -1;
                row.hidden = !match;
                if (match) { anyVisible = true; shown++; }
            });
            if (noResults) noResults.hidden = anyVisible || q === "";
            if (status) {
                status.textContent = q === ""
                    ? "Alle eenheden getoond."
                    : shown + (shown === 1 ? " eenheid" : " eenheden") + " gevonden voor \"" + input.value.trim() + "\".";
            }
        });
    }
})();
