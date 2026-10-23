// gl-v2 — Factuur bewerken (Invoices/EditV2, design-handoff 11f): twee stappen (Omschrijvingen, Lijnen) en een live
// voorbeeld waarin gewijzigde blokken gemarkeerd worden. Het formulier post naar InvoicesController.Edit.
(function () {
    "use strict";
    var form = document.getElementById("invoiceEditForm");
    if (!form) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }

    // ── foutoverzicht (server-fouten staan er al; de component houdt ze bij) ──
    if (window.GlV2ErrorSummary) window.GlV2ErrorSummary.init({ form: form, container: document.getElementById("gl-v2-error-summary") });

    // ── stappen ──
    var TOTAL = 2, step = 1;
    var prev = $("#ie-prev"), next = $("#ie-next"), save = $("#ie-save"), now = $("#ie-step-now");
    function show(n) {
        step = Math.max(1, Math.min(TOTAL, n));
        $$("[data-ie-panel]").forEach(function (p) { p.hidden = parseInt(p.dataset.iePanel, 10) !== step; });
        $$("[data-ie-steps]").forEach(function (p) { p.hidden = parseInt(p.dataset.ieSteps, 10) !== step; });
        now.textContent = step;
        prev.disabled = step === 1;
        next.hidden = step === TOTAL;
        save.hidden = step !== TOTAL;
        var body = $(".gl-v2-ie-body"); if (body && body.scrollIntoView) body.scrollIntoView({ block: "nearest" });
    }
    prev.addEventListener("click", function () { show(step - 1); });
    next.addEventListener("click", function () { show(step + 1); });
    document.addEventListener("gl-v2-steps:step", function (e) { var d = e.detail || {}; if (typeof d.index === "number") show(d.index + 1); });
    // Enter in een lijnveld bewaart niet per ongeluk de hele factuur vanaf stap 1
    form.addEventListener("keydown", function (e) { if (e.key === "Enter" && e.target.tagName === "INPUT" && step !== TOTAL) e.preventDefault(); });
    show(1);

    // ── live voorbeeld ──
    function bind(textarea, target) {
        if (!textarea || !target) return;
        var initial = (target.getAttribute("data-initial") || "").trim();
        function update() {
            var v = textarea.value;
            target.textContent = v;
            target.classList.toggle("is-changed", v.trim() !== initial);
        }
        textarea.addEventListener("input", update);
        update();
    }
    bind($('[data-ie-bind="header"]'), $("#pv-header"));
    bind($('[data-ie-bind="detail"]'), $("#pv-detail"));
    var footer = $('[data-ie-bind="footer"]');
    bind(footer, $("#pv-footer"));
    // blokken zonder tekst verdwijnen uit het voorbeeld, net als op de pdf
    function toggles() {
        var h = (($('[data-ie-bind="header"]') || {}).value || "").trim(), d = (($('[data-ie-bind="detail"]') || {}).value || "").trim();
        var desc = $("#pv-desc"); if (desc) desc.hidden = !h && !d;
        var hd = $("#pv-header"), dt = $("#pv-detail"); if (hd) hd.hidden = !h; if (dt) dt.hidden = !d;
        var ex = $("#pv-extra"); if (ex) ex.hidden = !((footer && footer.value) || "").trim();
    }
    $$("[data-ie-bind]").forEach(function (t) { t.addEventListener("input", toggles); });
    toggles();
    $$("[data-ie-line]").forEach(function (inp) { bind(inp, $("#pv-line-" + inp.getAttribute("data-ie-line"))); });

    // dubbele-submit blokkade
    form.addEventListener("submit", function () { if (save && !save.hidden) { save.disabled = true; save.classList.add("is-loading"); } });
})();
