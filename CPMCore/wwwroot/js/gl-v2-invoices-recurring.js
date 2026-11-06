// gl-v2 — Facturen › Terugkerend (Invoices/RecurringIndexV2, design-handoff punt 50b/50c): statusfilter, zoeken, jaar, klikbare rijen en het zijpaneel Instellingen.
// Gewone JS zonder DataTables: het overzicht telt enkele tientallen schema's, dus alles wordt in de browser gefilterd.
(function () {
    "use strict";
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function toast(tone, title, body) { if (window.GlV2Toast && window.GlV2Toast.show) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }

    // ───────────────────────── filter, zoeken, jaar ─────────────────────────
    var table = $("#ri-table");
    if (table) {
        var rows = $$("tbody tr", table), filter = "all", term = "", year = (window.GlV2RecurringIndex && window.GlV2RecurringIndex.defaultYear) || "";
        var search = $("#ri-search"), clear = $("#ri-search-clear"), yearInput = $("#ri-year"), noMatch = $("#ri-nomatch");
        function apply() {
            var shown = 0;
            rows.forEach(function (tr) {
                var st = tr.getAttribute("data-status");
                var okStatus = filter === "all" ? st !== "ended" : st === filter;
                var okTerm = !term || tr.getAttribute("data-search").indexOf(term) >= 0;
                var okYear = true;
                if (year) {
                    var y = parseInt(year, 10), first = parseInt(tr.getAttribute("data-first-year"), 10), last = parseInt(tr.getAttribute("data-last-year"), 10);
                    okYear = first <= y && (isNaN(last) || last >= y);
                }
                var on = okStatus && okTerm && okYear;
                tr.hidden = !on; if (on) shown++;
            });
            if (noMatch) noMatch.hidden = shown !== 0;
            if (clear) clear.hidden = !term;
        }
        $$("[data-ri-filter]").forEach(function (b) {
            b.addEventListener("click", function () {
                filter = b.getAttribute("data-ri-filter");
                $$("[data-ri-filter]").forEach(function (x) { x.classList.toggle("is-active", x === b); });
                apply();
            });
        });
        if (search) search.addEventListener("input", function () { term = search.value.trim().toLowerCase(); apply(); });
        if (clear) clear.addEventListener("click", function () { search.value = ""; term = ""; apply(); search.focus(); });
        if (yearInput) yearInput.addEventListener("change", function () { year = yearInput.value; apply(); });
        table.addEventListener("click", function (e) { var tr = e.target.closest("tr[data-href]"); if (tr) window.location.assign(tr.getAttribute("data-href")); });
        table.addEventListener("keydown", function (e) { if (e.key !== "Enter") return; var tr = e.target.closest("tr[data-href]"); if (tr) window.location.assign(tr.getAttribute("data-href")); });
        apply();
    }

    // ───────────────────────── zijpaneel Instellingen (50c) ─────────────────────────
    var panel = $("#ri-panel"), backdrop = $("#ri-panel-backdrop"), form = $("#ri-settings-form");
    if (!panel || !form) return;
    var lastFocus = null;
    function open() {
        lastFocus = document.activeElement;
        panel.classList.add("is-open"); backdrop.classList.add("is-open"); panel.setAttribute("aria-hidden", "false");
        document.body.classList.add("gl-v2-ri-panel-open");
        var first = $("input:not([disabled]):not([type=hidden]), button", panel); if (first) first.focus();
    }
    function close() {
        panel.classList.remove("is-open"); backdrop.classList.remove("is-open"); panel.setAttribute("aria-hidden", "true");
        document.body.classList.remove("gl-v2-ri-panel-open");
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }
    ["ri-open-settings", "ri-open-settings-mobile"].forEach(function (id) { var b = document.getElementById(id); if (b) b.addEventListener("click", open); });
    ["ri-panel-close", "ri-panel-cancel"].forEach(function (id) { var b = document.getElementById(id); if (b) b.addEventListener("click", close); });
    backdrop.addEventListener("click", close);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && panel.classList.contains("is-open")) close(); });

    // gekozen collega's in de personenkiezer zetten (e-mailadressen, komma-gescheiden)
    try {
        var init = JSON.parse(($("#ri-settings-initial") || {}).textContent || "{}");
        var root = $("[data-gl-v2-people]", panel);
        if (init.notify && root && window.GlV2People) window.GlV2People.set(root, init.notify.split(",").map(function (m) { return { id: m, text: m, sub: "" }; }));
    } catch (e) { /* zonder voorinvulling werkt het paneel nog */ }

    // gezondheidsindex nu ophalen (anders gebeurt dat op de laatste dag van de maand)
    var hiBtn = $("#ri-hi-refresh");
    if (hiBtn) hiBtn.addEventListener("click", function () {
        var fd = new FormData(); var t = form.querySelector("input[name=__RequestVerificationToken]"); fd.append("__RequestVerificationToken", t ? t.value : "");
        hiBtn.disabled = true;
        fetch(hiBtn.getAttribute("data-url"), { method: "POST", body: fd, credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.json(); })
            .then(function (r) { if (!r.ok) throw new Error(r.error || "Ophalen mislukt."); $("#ri-hi-text").textContent = r.text; toast("success", "Gezondheidsindex opgehaald", r.rows ? r.rows + " nieuwe of gewijzigde rijen." : "Er was niets nieuws."); })
            .catch(function (err) { toast("danger", "Niet gelukt", err.message); })
            .then(function () { hiBtn.disabled = false; });
    });

    function showError(msg) { var box = $("#ri-settings-error"); box.hidden = !msg; if (msg) box.querySelector("span").textContent = msg; }
    form.addEventListener("submit", function (e) {
        e.preventDefault(); showError("");
        var save = $("#ri-panel-save"); save.disabled = true;
        fetch(form.getAttribute("data-url"), { method: "POST", body: new FormData(form), credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.json(); })
            .then(function (r) { if (!r.ok) throw new Error(r.error || "De instellingen konden niet bewaard worden."); close(); toast("success", "Instellingen bewaard", "Ze gelden voor nieuwe schema’s."); })
            .catch(function (err) { showError(err.message); })
            .then(function () { save.disabled = false; });
    });
})();
