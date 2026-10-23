// gl-v2 — Marktanalyse › Projectdetail (design-handoff punt 46d). Enkel geladen door Views/Marktanalyse/ProjectdetailV2.cshtml.
// Projectwissel, Units/Bronnen-schakelaar, statusfilter op de units en uitklapbare bronrijen.
(function () {
    "use strict";

    var sw = document.getElementById("pd-switch");
    if (sw) sw.addEventListener("change", function () { if (this.value) window.location.href = this.value; });
    document.querySelectorAll('.gl-v2-pd-switch a[aria-disabled="true"]').forEach(function (a) {
        a.addEventListener("click", function (e) { e.preventDefault(); });
    });

    var viewSwitch = document.getElementById("pd-view");
    var filterSwitch = document.getElementById("pd-filter");
    var paneUnits = document.getElementById("pd-pane-units");
    var paneBronnen = document.getElementById("pd-pane-bronnen");
    var table = document.getElementById("pd-units");
    if (!viewSwitch || !table) return;

    function activate(group, btn) {
        group.querySelectorAll("button").forEach(function (b) {
            var on = b === btn;
            b.classList.toggle("is-active", on);
            b.setAttribute("aria-pressed", on ? "true" : "false");
        });
    }

    viewSwitch.addEventListener("click", function (e) {
        var btn = e.target.closest("button[data-view]");
        if (!btn) return;
        activate(viewSwitch, btn);
        var units = btn.getAttribute("data-view") === "units";
        paneUnits.hidden = !units;
        paneBronnen.hidden = units;
        filterSwitch.hidden = !units;
    });

    function applyFilter(f) {
        table.querySelectorAll("tbody > tr[data-group]").forEach(function (tr) {
            var show = !f || tr.getAttribute("data-group") === f;
            if (tr.classList.contains("is-sources")) { tr.hidden = !show || !tr.classList.contains("is-shown"); return; }
            tr.hidden = !show;
            if (!show) {
                var src = document.getElementById(tr.getAttribute("data-expand") || "");
                if (src) src.hidden = true;
            }
        });
        var total = table.querySelector("tr[data-total]");
        if (total) total.hidden = !!f;
    }

    filterSwitch.addEventListener("click", function (e) {
        var btn = e.target.closest("button[data-filter]");
        if (!btn) return;
        activate(filterSwitch, btn);
        applyFilter(btn.getAttribute("data-filter"));
    });

    table.addEventListener("click", function (e) {
        if (e.target.closest("a")) return;
        var tr = e.target.closest("tr[data-expand]");
        if (!tr) return;
        var src = document.getElementById(tr.getAttribute("data-expand"));
        if (!src) return;
        var open = src.hidden;
        src.hidden = !open;
        src.classList.toggle("is-shown", open);
        tr.classList.toggle("is-open", open);
    });
})();
