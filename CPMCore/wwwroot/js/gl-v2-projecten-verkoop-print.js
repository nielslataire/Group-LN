// gl-v2 — Verkoop: Berekening afdrukken (design-handoff 45d). Opent vanuit een knop met .js-vk-print; de eenheden komen uit data-vk-print-ids
// (Eenheid berekenen) of uit de aangevinkte rijen (Te koop). Op Eenheid berekenen gaan de eigen bedragen mee naar de PDF.
(function () {
    "use strict";
    var m = document.getElementById("vk-print-modal");
    if (!m) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var eigen = null; // { grond, bouw } enkel vanuit Eenheid berekenen
    function ids() { return $$("#vk-p-units input:checked", m).map(function (c) { return c.value; }); }
    function refresh() {
        var n = ids().length, intern = $("input[name=vk-p-versie]:checked", m).value === "intern";
        var ov = $("#vk-p-overzicht"); ov.disabled = n < 2; ov.closest("label").classList.toggle("is-disabled", n < 2);
        $("#vk-p-sub").textContent = n + (n === 1 ? " eenheid" : " eenheden") + " · " + (intern ? "interne versie" : "versie voor de koper");
        $("#vk-p-note").textContent = n === 0 ? "Kies minstens één eenheid." : n + (n === 1 ? " pagina" : " pagina's") + (n > 1 && ov.checked ? " + overzicht" : "") + ", één per eenheid" + (eigen && n === 1 ? " · met de bedragen die je nu invult" : "") + ".";
        $("#vk-p-go").disabled = n === 0;
        $("#vk-p-go-text").textContent = "Download PDF" + (n > 1 ? " (" + n + " pagina's)" : "");
    }
    document.addEventListener("click", function (e) {
        var b = e.target.closest(".js-vk-print"); if (!b) return;
        var pre = b.dataset.vkPrintIds ? b.dataset.vkPrintIds.split(",") : $$(".js-vk-check:checked").map(function (c) { return c.value; });
        $$("#vk-p-units input", m).forEach(function (c) { c.checked = pre.indexOf(c.value) >= 0; });
        var g = document.getElementById("vb-grond-bedrag"), bo = document.getElementById("vb-bouw-bedrag");
        eigen = b.dataset.vkPrintIds && g && bo ? { grond: g.value, bouw: bo.value } : null;
        refresh();
        if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(m).show();
    });
    m.addEventListener("change", refresh);
    $("#vk-p-go").addEventListener("click", function () {
        var q = new URLSearchParams({ ids: ids().join(","), intern: $("input[name=vk-p-versie]:checked", m).value === "intern", kostenKoper: $("#vk-p-kosten").checked, detail: $("#vk-p-detail").checked, overzicht: $("#vk-p-overzicht").checked && ids().length > 1 });
        if (eigen && ids().length === 1) { q.set("grond", eigen.grond); q.set("bouw", eigen.bouw); }
        var url = m.dataset.url; window.open(url + (url.indexOf("?") < 0 ? "?" : "&") + q.toString(), "_blank", "noopener");
        if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(m).hide();
    });
})();
