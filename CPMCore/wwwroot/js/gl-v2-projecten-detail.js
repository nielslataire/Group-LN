// gl-v2 layout-pilot — Projecten/DetailV2 (design-handoff punt 12a). Two independent, small filters,
// ported from the legacy Detail.cshtml's inline <script> (category chips on "Vraagt actie",
// eenheid/klant search on the units table) — same behavior, gl-v2 class names.
(function () {
    "use strict";

    initAttentionFilters();
    initUnitsFilter();

    function initAttentionFilters() {
        var chips = document.querySelectorAll(".js-gl-v2-pd-chip");
        if (!chips.length) return;
        var body = document.querySelector(".js-gl-v2-pd-attention-body");
        var status = document.getElementById("gl-v2-pd-filter-status");

        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                var cat = chip.getAttribute("data-cat");

                if (body) {
                    body.classList.add("is-refiltering");
                    window.setTimeout(function () { body.classList.remove("is-refiltering"); }, 140);
                }

                chips.forEach(function (c) { c.classList.toggle("is-active", c === chip); });

                var items = document.querySelectorAll(".js-gl-v2-pd-attention-item");
                items.forEach(function (item) {
                    var matches = cat === "alle" || item.getAttribute("data-cat") === cat;
                    item.hidden = !matches;
                });

                document.querySelectorAll(".js-gl-v2-pd-attention-group").forEach(function (group) {
                    var visible = Array.prototype.some.call(
                        group.querySelectorAll(".js-gl-v2-pd-attention-item"),
                        function (item) { return !item.hidden; }
                    );
                    group.hidden = !visible;
                });

                if (status) {
                    var shown = Array.prototype.filter.call(items, function (item) { return !item.hidden; }).length;
                    status.textContent = (cat === "alle" ? "Alle aandachtspunten" : "Categorie " + cat)
                        + ": " + shown + (shown === 1 ? " punt" : " punten") + " getoond.";
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

        input.addEventListener("input", function () {
            var q = input.value.trim().toLowerCase();
            var anyVisible = false;
            rows.forEach(function (row) {
                var hay = (row.getAttribute("data-search") || "").toLowerCase();
                var match = q === "" || hay.indexOf(q) !== -1;
                row.hidden = !match;
                if (match) anyVisible = true;
            });
            if (noResults) noResults.hidden = anyVisible || q === "";
        });
    }
})();
