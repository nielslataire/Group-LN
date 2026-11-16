// gl-v2 layout-pilot — Projecten/DetailV2 (design-handoff punt 12a). Two independent, small filters,
// ported from the legacy Detail.cshtml's inline <script> (category chips on "Vraagt actie",
// eenheid/klant search on the units table) — same behavior, gl-v2 class names.
(function () {
    "use strict";

    initUnitsFilter();

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
