// gl-v2 — Elektronisch ondertekenen, interne schermen. Drie kleine, pagina-eigen gedragingen; alles
// wat generiek is (⋯-menu's, klikbare rijen, toasts, modals) komt uit gl-v2-shell.js.
// 1. Start: ondertekenaars toevoegen/verwijderen/uitvinken, met hernummering van Parties[i].* zodat
//    de MVC-modelbinder een aaneengesloten lijst krijgt, en een teller in de actiebalk.
// 2. Index: de tabbar filtert client-side op data-status (open / ondertekend / gesloten).
// 3. Dossier: de annuleermodal zet focus op de reden en weigert een lege reden vóór de POST.
(function () {
    "use strict";

    // ── 1. Start ────────────────────────────────────────────────────────────────────────────────
    var list = document.getElementById("gl-v2-sg-parties");
    if (list) {
        var template = document.getElementById("gl-v2-sg-party-template");
        var addBtn = document.getElementById("gl-v2-sg-add-party");
        var counter = document.getElementById("gl-v2-sg-party-count");
        var emptyMsg = document.getElementById("gl-v2-sg-parties-empty");

        function renumber() {
            var rows = list.querySelectorAll("[data-party-row]");
            rows.forEach(function (row, i) {
                row.querySelectorAll("[name]").forEach(function (el) {
                    el.name = el.name.replace(/Parties\[\d+\]/, "Parties[" + i + "]");
                });
                var order = row.querySelector(".gl-v2-sg-party-order");
                if (order) order.textContent = String(i + 1);
            });
            var included = list.querySelectorAll(".js-gl-v2-sg-include:checked").length;
            if (counter) {
                counter.textContent = included === 0
                    ? "Nog geen ondertekenaar gekozen"
                    : included + " " + (included === 1 ? counter.dataset.singular : counter.dataset.plural) + " tekent" + (included === 1 ? "" : " mee");
                counter.classList.toggle("is-error", included === 0);
            }
            if (emptyMsg) emptyMsg.hidden = rows.length > 0;
        }

        list.addEventListener("change", function (e) {
            var cb = e.target.closest(".js-gl-v2-sg-include");
            if (!cb) return;
            var row = cb.closest("[data-party-row]");
            row.classList.toggle("is-excluded", !cb.checked);
            row.querySelectorAll(".gl-v2-field-input").forEach(function (input) {
                // Uitgevinkt = niet verplicht, anders blokkeert de browser-validatie op een lege e-mail
                // van iemand die toch niet meetekent.
                if (input.dataset.wasRequired === undefined) input.dataset.wasRequired = input.required ? "1" : "0";
                input.required = cb.checked && input.dataset.wasRequired === "1";
            });
            var label = cb.closest(".gl-v2-sg-party-include");
            if (label) label.title = cb.checked ? "Tekent mee" : "Tekent niet mee";
            renumber();
        });

        list.addEventListener("click", function (e) {
            var btn = e.target.closest(".js-gl-v2-sg-remove");
            if (!btn) return;
            var row = btn.closest("[data-party-row]");
            if (row) row.remove();
            renumber();
        });

        if (addBtn && template) {
            addBtn.addEventListener("click", function () {
                var index = list.querySelectorAll("[data-party-row]").length;
                var html = template.innerHTML.replace(/__i__/g, String(index)).replace(/__n__/g, String(index + 1));
                var wrapper = document.createElement("div");
                wrapper.innerHTML = html.trim();
                var row = wrapper.firstElementChild;
                list.appendChild(row);
                renumber();
                var first = row.querySelector("input[type=text]");
                if (first) first.focus();
            });
        }

        var form = document.getElementById("gl-v2-sg-start-form");
        if (form) {
            form.addEventListener("submit", function (e) {
                var included = list.querySelectorAll(".js-gl-v2-sg-include:checked").length;
                if (included === 0) {
                    e.preventDefault();
                    if (window.GlV2Toast) window.GlV2Toast.show({ tone: "error", title: "Geen ondertekenaar", body: "Kies minstens één ondertekenaar." });
                    return;
                }
                // Eén verzending per klik: de service maakt anders een tweede dossier niet (unieke index),
                // maar de gebruiker zou wel een foutmelding zien.
                var submit = document.getElementById("gl-v2-sg-submit");
                if (submit) { submit.classList.add("is-loading"); submit.disabled = true; }
            });
        }
        renumber();
    }

    // ── 2. Index: statusfilter ──────────────────────────────────────────────────────────────────
    var tabs = document.getElementById("gl-v2-sg-status-tabs");
    if (tabs) {
        var rows = document.querySelectorAll(".gl-v2-sg-index-table tbody tr[data-status]");
        var empty = document.getElementById("gl-v2-sg-empty-filter");
        tabs.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab) return;
            tabs.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (t) {
                var active = t === tab;
                t.classList.toggle("is-active", active);
                t.setAttribute("aria-selected", active ? "true" : "false");
            });
            var status = tab.dataset.status || "";
            var visible = 0;
            rows.forEach(function (row) {
                var show = !status || row.dataset.status === status;
                row.hidden = !show;
                if (show) visible++;
            });
            if (empty) empty.hidden = visible > 0 || rows.length === 0;
        });
    }

    // ── 3. Dossier: annuleermodal ───────────────────────────────────────────────────────────────
    var cancelModal = document.getElementById("gl-v2-sg-cancel-modal");
    if (cancelModal) {
        var reason = document.getElementById("gl-v2-sg-cancel-reason");
        cancelModal.addEventListener("shown.bs.modal", function () { if (reason) reason.focus(); });
        var cancelForm = document.getElementById("gl-v2-sg-cancel-form");
        if (cancelForm) {
            cancelForm.addEventListener("submit", function (e) {
                if (reason && !reason.value.trim()) {
                    e.preventDefault();
                    reason.focus();
                    if (window.GlV2Toast) window.GlV2Toast.show({ tone: "error", title: "Reden verplicht", body: "Geef een reden op; die komt in de audit trail." });
                }
            });
        }
    }
})();
