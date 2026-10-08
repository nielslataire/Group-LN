// gl-v2 — Punt, detail (design-handoff 40o): statuswissel (afwijzen vraagt een reden, "in de wacht" opent de wacht-modal),
// zichtbaarheid van het bericht (intern / naar aannemer).
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-pd-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function modal(id) { var m = document.getElementById(id); return m && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(m) : null; }

    function postStatus(status, comment) {
        var f = $("#pd-status-form"); if (!f) return;
        $("#pd-status-value").value = status; $("#pd-status-comment").value = comment || "";
        f.submit();
    }
    document.addEventListener("click", function (e) {
        var s = e.target.closest(".js-pd-status");
        if (s) {
            if (window.GlV2Menu) window.GlV2Menu.closeAll();
            var st = s.dataset.status;
            if (st === "12") { var h = modal("pd-hold-modal"); if (h) h.show(); }
            else if (st === "6") { var r = modal("pd-reject-modal"); if (r) r.show(); }
            else postStatus(st, "");
            return;
        }
        if (e.target.closest(".js-pd-hold")) { var hm = modal("pd-hold-modal"); if (hm) hm.show(); return; }
        var seg = e.target.closest(".gl-v2-pd-seg-btn");
        if (seg) {
            document.querySelectorAll(".gl-v2-pd-seg-btn").forEach(function (b) { b.classList.toggle("is-active", b === seg); });
            $("#pd-internal").value = seg.dataset.internal;
        }
    });
    var rc = $("#pd-reject-confirm");
    if (rc) rc.addEventListener("click", function () {
        var reason = $("#pd-reject-reason").value.trim();
        if (!reason) { $("#pd-reject-reason").focus(); return; }
        postStatus("6", reason);
    });
})();
