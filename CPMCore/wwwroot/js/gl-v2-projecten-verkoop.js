// gl-v2 — Verkoop: Te koop (design-handoff 45a). Gewone tabel zonder DataTables (een project heeft enkele tot enkele tientallen eenheden):
// tabs en zoeken verbergen rijen, de totaalrij telt de zichtbare rijen, de selectiebalk past de status of het websitevinkje aan.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-vk-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var canWrite = root.dataset.canWrite === "1";
    var rows = $$(".gl-v2-vk-row"), tab = "all";
    var search = $("#vk-search"), searchClear = $("#vk-search-clear"), selbar = $("#vk-selbar"), checkAll = $("#vk-check-all");
    var nf = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 });
    function eur(v) { return "€ " + nf.format(Math.round(v)); }
    function token() { var i = document.querySelector("input[name=__RequestVerificationToken]"); return i ? i.value : ""; }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-vk-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 4500);
    }
    function post(obj) {
        var fd = new FormData(); fd.append("__RequestVerificationToken", token());
        Object.keys(obj).forEach(function (k) { if (obj[k] != null) fd.append(k, obj[k]); });
        return fetch(root.dataset.bulkUrl, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); });
    }

    function matches(tr) {
        if (tab !== "all" && tr.dataset.status !== tab) return false;
        var q = (search.value || "").trim().toLowerCase();
        return !q || tr.dataset.search.indexOf(q) >= 0;
    }
    function apply() {
        var g = 0, b = 0, t = 0, n = 0;
        rows.forEach(function (tr) {
            var ok = matches(tr); tr.hidden = !ok;
            if (!ok) { var cb = $(".js-vk-check", tr); if (cb) { cb.checked = false; tr.classList.remove("is-selected"); } return; }
            n++; g += parseFloat(tr.dataset.grond) || 0; b += parseFloat(tr.dataset.bouw) || 0; t += parseFloat(tr.dataset.totaal) || 0;
        });
        $("#vk-foot-label").textContent = "Totaal · " + n + (n === 1 ? " eenheid" : " eenheden");
        $("#vk-foot-grond").textContent = eur(g); $("#vk-foot-bouw").textContent = eur(b); $("#vk-foot-totaal").textContent = eur(t);
        $("#vk-empty").hidden = n > 0;
        searchClear.hidden = !search.value;
        $$("[data-vk-tab]").forEach(function (a) { var on = a.dataset.vkTab === tab; a.classList.toggle("is-active", on); a.setAttribute("aria-selected", on ? "true" : "false"); });
        updateSelection();
    }
    document.addEventListener("click", function (e) {
        var t = e.target.closest("[data-vk-tab]"); if (t) { e.preventDefault(); tab = t.dataset.vkTab; apply(); }
    });
    search.addEventListener("input", apply);
    searchClear.addEventListener("click", function () { search.value = ""; search.focus(); apply(); });

    // ── selectie ──
    function selected() { return $$(".js-vk-check:checked").map(function (c) { return c.value; }); }
    function updateSelection() {
        if (!canWrite || !selbar) return;
        var ids = selected();
        $$(".gl-v2-vk-row").forEach(function (tr) { var cb = $(".js-vk-check", tr); tr.classList.toggle("is-selected", !!(cb && cb.checked)); });
        $("#vk-sel-n").textContent = ids.length; $$(".js-vk-sel-n").forEach(function (s) { s.textContent = ids.length; });
        selbar.hidden = ids.length === 0;
        var calc = $("#vk-sel-calc"); if (calc) { var rij = ids.length === 1 ? document.querySelector('.gl-v2-vk-row[data-id="' + ids[0] + '"]') : null; calc.hidden = !rij || rij.dataset.status === "Verkocht"; if (rij) calc.href = root.dataset.calcUrl + (root.dataset.calcUrl.indexOf("?") < 0 ? "?" : "&") + "unitId=" + ids[0]; }
        if (ids.length === 0) closePops();
        var zichtbaar = rows.filter(function (r) { return !r.hidden; });
        checkAll.checked = zichtbaar.length > 0 && ids.length === zichtbaar.length;
    }
    function closePops() { $$(".gl-v2-vk-pop").forEach(function (p) { p.hidden = true; }); }
    if (canWrite) {
        document.addEventListener("change", function (e) {
            if (e.target.classList.contains("js-vk-check")) updateSelection();
            if (e.target === checkAll) { rows.forEach(function (tr) { var cb = $(".js-vk-check", tr); if (cb && !tr.hidden) cb.checked = checkAll.checked; }); updateSelection(); }
        });
        $("#vk-sel-clear").addEventListener("click", function () { $$(".js-vk-check").forEach(function (c) { c.checked = false; }); updateSelection(); });
        document.addEventListener("click", function (e) {
            var pop = e.target.closest(".js-vk-pop");
            if (pop) { var el = document.getElementById(pop.dataset.pop), open = el.hidden; closePops(); el.hidden = !open; return; }
            if (!e.target.closest(".gl-v2-vk-pop")) closePops();
        });
        function run(obj) {
            obj.unitIds = selected().join(",");
            post(obj).then(function (j) {
                if (!j.ok) { toast(j.error || "Aanpassen is mislukt."); return; }
                toast(j.aantal + (j.aantal === 1 ? " eenheid" : " eenheden") + " aangepast" + (j.overgeslagen ? " · " + j.overgeslagen + " verkocht en overgeslagen" : ""));
                setTimeout(function () { location.reload(); }, 900);
            }).catch(function () { toast("Aanpassen is mislukt."); });
        }
        $$(".js-vk-do").forEach(function (b) { b.addEventListener("click", function () { closePops(); run({ op: b.dataset.op }); }); });
        var om = document.getElementById("vk-optie-modal");
        $(".js-vk-optie").addEventListener("click", function () { closePops(); if (om && window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(om).show(); });
        $("#vk-optie-ok").addEventListener("click", function () {
            if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(om).hide();
            run({ op: "optie", until: $("#vk-optie-until").value, holder: $("#vk-optie-holder").value, via: $("#vk-optie-via").value });
        });
    }

    // ── exporteren (45f): PDF-prijslijst; Excel (45g) volgt ──
    var xm = document.getElementById("vk-export-modal");
    if (xm) {
        var xScope = function () { return (xm.querySelector("input[name=vk-x-scope]:checked") || {}).value || "all"; };
        var xIds = function (scope) {
            if (scope === "selection") return selected();
            var list = scope === "filter" ? rows.filter(function (r) { return !r.hidden; }) : rows;
            return list.map(function (r) { return r.dataset.id; });
        };
        var xRefresh = function () {
            var sel = selected().length, fil = rows.filter(function (r) { return !r.hidden; }).length;
            $("#vk-x-n-all").textContent = "(" + rows.length + ")"; $("#vk-x-n-sel").textContent = "(" + sel + ")"; $("#vk-x-n-filter").textContent = "(" + fil + ")";
            var selInput = xm.querySelector("input[name=vk-x-scope][value=selection]"); selInput.disabled = sel === 0; selInput.closest("label").classList.toggle("is-disabled", sel === 0);
            if (selInput.disabled && selInput.checked) xm.querySelector("input[name=vk-x-scope][value=all]").checked = true;
            var n = xIds(xScope()).length, xl = xFmt === "xlsx";
            $$("[data-vk-for]", xm).forEach(function (el) { el.hidden = el.dataset.vkFor !== xFmt; });
            $$("[data-vk-format]", xm).forEach(function (b) { var on = b.dataset.vkFormat === xFmt; b.classList.toggle("is-on", on); b.setAttribute("aria-checked", on ? "true" : "false"); });
            $("#vk-x-note").textContent = xl
                ? n + (n === 1 ? " eenheid" : " eenheden") + " in het werkblad · " + (1 + ($("#vk-x-berekening").checked ? 1 : 0) + ($("#vk-x-kosten").checked ? 1 : 0)) + " tabbladen. Bedragen staan als getallen, om verder te rekenen."
                : n + (n === 1 ? " eenheid" : " eenheden") + " in de prijslijst · " + (xm.querySelector("input[name=vk-x-layout]:checked").value === "1" ? "A4 liggend" : "A4 staand") + ". Prijzen staan onder voorbehoud van verkoop.";
            $("#vk-x-go-text").textContent = xl ? "Download Excel" : "Download PDF";
            $("#vk-x-go-icon").className = "ph " + (xl ? "ph-file-xls" : "ph-file-pdf");
            $("#vk-x-go").disabled = n === 0;
        };
        var xFmt = "pdf";
        xm.addEventListener("click", function (e) { var b = e.target.closest("[data-vk-format]"); if (b) { xFmt = b.dataset.vkFormat; xRefresh(); } });
        document.addEventListener("click", function (e) {
            var b = e.target.closest(".js-vk-export"); if (!b) return;
            closePops();
            var pickSel = b.dataset.vkExportScope === "selection" && selected().length > 0;
            xRefresh();
            if (pickSel) xm.querySelector("input[name=vk-x-scope][value=selection]").checked = true;
            xRefresh();
            if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(xm).show();
        });
        xm.addEventListener("change", xRefresh);
        $("#vk-x-go").addEventListener("click", function () {
            var scope = xScope(), ids = scope === "all" ? [] : xIds(scope);
            var q = xFmt === "xlsx"
                ? new URLSearchParams({ prekad: $("#vk-x-prekad").checked, koper: $("#vk-x-koper").checked, totaalKoper: $("#vk-x-xtotaal").checked, berekening: $("#vk-x-berekening").checked, kosten: $("#vk-x-kosten").checked })
                : new URLSearchParams({ verkochtZonderPrijs: $("#vk-x-sold").checked, totaalKoper: $("#vk-x-total").checked, optieAlsTeKoop: $("#vk-x-option").checked, liggend: xm.querySelector("input[name=vk-x-layout]:checked").value === "1" });
            if (ids.length) q.set("ids", ids.join(","));
            var url = xFmt === "xlsx" ? xm.dataset.xlsxUrl : xm.dataset.url; window.open(url + (url.indexOf("?") < 0 ? "?" : "&") + q.toString(), "_blank", "noopener");
            if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(xm).hide();
        });
    }
    apply();
})();
