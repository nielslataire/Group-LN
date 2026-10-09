// gl-v2 — Verslagen, lijst (design-handoff 40i): segmenten filteren de rijen, een rij kiest het verslag (samenvatting rechts).
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-vs-root");
    if (!root) return;
    var rows = Array.prototype.slice.call(root.querySelectorAll(".gl-v2-vs-row"));
    root.querySelector(".gl-v2-vs-segments").addEventListener("click", function (e) {
        var b = e.target.closest(".gl-v2-vs-seg"); if (!b) return;
        root.querySelectorAll(".gl-v2-vs-seg").forEach(function (s) { s.classList.toggle("is-active", s === b); });
        var t = b.dataset.type;
        rows.forEach(function (r) { r.hidden = t !== "" && r.dataset.type !== t; });
    });
    rows.forEach(function (r) { r.addEventListener("click", function () { window.location.href = r.dataset.href; }); });

    var open = document.getElementById("vs-oplev-open"), modal = document.getElementById("vs-oplev-modal");
    if (open && modal && window.bootstrap) open.addEventListener("click", function () { window.bootstrap.Modal.getOrCreateInstance(modal).show(); });
})();
