// gl-v2 — Instellingen › Vacatures › Bewerken (punt 55d): Quill voor de lange beschrijving, video/poster verwijderen, de sleepbare puntenlijsten (toevoegen/bewerken in één gedeelde modal,
// verwijderen met bevestiging, herschikken naar de bestaande …Volgorde-acties) en een waarschuwing bij niet-opgeslagen wijzigingen.
(function () {
    "use strict";
    var form = document.getElementById("vc-form");
    if (!form) return;
    var token = (form.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js
    var canWrite = !$("vc-titel").disabled;

    // ── Lange beschrijving (Quill) ──
    var area = $("vc-lang"), quill = null;
    if (typeof Quill !== "undefined") {
        quill = new Quill("#vc-quill", {
            theme: "snow", readOnly: !canWrite,
            modules: { toolbar: [["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], [{ header: [2, 3, 4, false] }], ["link"], ["clean"]] }
        });
        if (area.value) quill.clipboard.dangerouslyPasteHTML(area.value, "silent");
        form.querySelectorAll(".ql-toolbar button, .ql-toolbar .ql-picker-label").forEach(function (el) { el.setAttribute("type", "button"); });
    }
    function lang() { return quill ? (quill.getText().trim() ? quill.root.innerHTML : "") : area.value; }

    // ── Vuile staat ──
    function snap() { return JSON.stringify([$("vc-titel").value, $("vc-cat").value, $("vc-loc").value, $("vc-dienst").value, $("vc-opl").value, $("vc-start").value, $("vc-kort").value, lang(), $("vc-pub").checked, $("vc-slug").value, $("vc-sort").value, $("vc-video-cur").value, $("vc-poster-cur").value, ($("vc-video").files || []).length, ($("vc-poster").files || []).length]); }
    var start = snap(), badge = $("vc-dirty"), submitting = false;
    function dirty() { return snap() !== start; }
    function update() { area.value = lang(); $("vc-sort-hidden").value = $("vc-sort").value; if (badge) badge.hidden = !dirty(); }
    if (quill) quill.on("text-change", update);
    form.addEventListener("input", update); form.addEventListener("change", update);
    $("vc-pub").addEventListener("change", function () { var b = $("vc-pub-badge"); b.textContent = this.checked ? "Gepubliceerd" : "Concept"; b.className = "gl-v2-badge " + (this.checked ? "is-positive" : "is-neutral"); });
    form.addEventListener("submit", function () { submitting = true; area.value = lang(); $("vc-sort-hidden").value = $("vc-sort").value; });
    window.addEventListener("beforeunload", function (e) { if (canWrite && !submitting && dirty()) { e.preventDefault(); e.returnValue = ""; } });
    update(); if (badge) badge.hidden = true;

    // ── Video en poster: het bestaande bestand verwijderen ──
    $("vc-video_zone").addEventListener("gl-v2:dropzone-remove-current", function () { $("vc-video-cur").value = ""; var v = $("vc-video-pv"); v.removeAttribute("src"); v.hidden = true; update(); });
    $("vc-poster_zone").addEventListener("gl-v2:dropzone-remove-current", function () { $("vc-poster-cur").value = ""; var p = $("vc-poster-pv"); if (p) p.hidden = true; update(); });
    $("vc-video_zone").addEventListener("gl-v2:dropzone-change", function (e) {
        var f = e.detail && e.detail.file, v = $("vc-video-pv");
        if (f) { try { v.src = URL.createObjectURL(f); v.hidden = false; v.load(); } catch (x) { /* voorbeeld niet beschikbaar */ } }
        update();
    });

    // ── Vacature verwijderen ──
    var delVac = $("vc-delete");
    if (delVac) delVac.addEventListener("click", function () {
        window.GlV2Bevestig({ title: "Vacature verwijderen?", desc: "“" + $("vc-titel").value + "” verdwijnt van de website. Je kunt dit niet ongedaan maken.", ok: "Verwijderen", tone: "danger" })
            .then(function (ok) { if (ok) { submitting = true; $("vc-del-vac").submit(); } });
    });

    // ── Puntenlijsten ──
    var modalEl = $("vc-item-modal");
    if (!modalEl) return;
    var modal = window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(modalEl) : null;
    var itemForm = $("vc-item-form"), delForm = $("vc-del-form"), delId = $("vc-del-id");

    function nrs(list) { list.querySelectorAll("[data-vc-nr]").forEach(function (n, i) { n.textContent = i + 1; }); }
    function tel(list) {
        var items = list.querySelectorAll(".gl-v2-inh-item");
        list.querySelector("[data-vc-count]").textContent = items.length;
        list.querySelector("[data-vc-empty]").hidden = items.length > 0;
        nrs(list);
    }
    function open(list, li) {
        var kind = list.dataset.kind, stap = kind === "stap", nieuw = !li;
        itemForm.action = list.dataset.save;
        itemForm.elements.ID.value = nieuw ? "0" : li.dataset.id;
        itemForm.elements.Categorie.value = list.dataset.cat || "";
        var max = Array.from(list.querySelectorAll(".gl-v2-inh-item")).reduce(function (m, x) { return Math.max(m, parseInt(x.dataset.sort, 10) || 0); }, -10);
        itemForm.elements.SortOrder.value = nieuw ? max + 10 : li.dataset.sort;
        $("vc-item-titel-wrap").hidden = !stap;
        $("vc-item-titel").required = stap;
        $("vc-item-titel").value = nieuw ? "" : (li.dataset.titel || "");
        $("vc-item-tekst").value = nieuw ? "" : (li.dataset.tekst || "");
        $("vc-item-tekst").maxLength = stap ? 1000 : 500;
        $("vc-item-tekst").required = !stap;
        $("vc-item-tekst-label").textContent = stap ? "Tekst (optioneel)" : "Tekst";
        $("vc-item-title").textContent = (nieuw ? "Toevoegen: " : "Bewerken: ") + list.dataset.title;
        if (modal) modal.show();
        modalEl.addEventListener("shown.bs.modal", function f() { modalEl.removeEventListener("shown.bs.modal", f); (stap ? $("vc-item-titel") : $("vc-item-tekst")).focus(); });
    }
    itemForm.addEventListener("submit", function (e) {
        var stap = !$("vc-item-titel-wrap").hidden;
        if (stap ? !$("vc-item-titel").value.trim() : !$("vc-item-tekst").value.trim()) { e.preventDefault(); toast("warning", stap ? "Titel ontbreekt" : "Tekst ontbreekt", "Vul het veld in voor je opslaat."); return; }
        submitting = true;
    });

    document.querySelectorAll("[data-vc-list]").forEach(function (list) {
        var ul = list.querySelector("[data-vc-items]");
        tel(list);
        var add = list.querySelector("[data-vc-add]"); if (add) add.addEventListener("click", function () { open(list, null); });
        ul.addEventListener("click", function (e) {
            var li = e.target.closest(".gl-v2-inh-item"); if (!li) return;
            if (e.target.closest("[data-vc-edit]")) open(list, li);
            if (e.target.closest("[data-vc-del]")) {
                var naam = li.dataset.titel || li.dataset.tekst || "dit punt";
                window.GlV2Bevestig({ title: "Punt verwijderen?", desc: "“" + (naam.length > 80 ? naam.slice(0, 80) + "…" : naam) + "” verdwijnt uit de lijst.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                    if (!ok) return;
                    delForm.action = list.dataset.del; delId.name = list.dataset.delField; delId.value = li.dataset.id; submitting = true; delForm.submit();
                });
            }
        });

        // Herschikken (HTML5 slepen) → bestaande …Volgorde-actie.
        if (!ul.querySelector("[draggable]")) return;
        var drag = null, vorig = null;
        ul.addEventListener("dragstart", function (e) { var li = e.target.closest(".gl-v2-inh-item"); if (!li) return; drag = li; vorig = Array.from(ul.children); li.classList.add("is-dragging"); e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", li.dataset.id); } catch (x) { /* oudere browsers */ } });
        ul.addEventListener("dragover", function (e) {
            if (!drag) return; e.preventDefault();
            var over = e.target.closest(".gl-v2-inh-item"); if (!over || over === drag) return;
            var r = over.getBoundingClientRect(), na = e.clientY > r.top + r.height / 2;
            ul.insertBefore(drag, na ? over.nextSibling : over);
        });
        ul.addEventListener("dragend", function () {
            if (!drag) return;
            drag.classList.remove("is-dragging");
            var ids = Array.from(ul.children).map(function (x) { return x.dataset.id; }), oud = vorig.map(function (x) { return x.dataset.id; });
            var li = drag; drag = null;
            if (ids.join() === oud.join()) return;
            var fd = new FormData(); fd.append("VacatureId", itemForm.elements.VacatureId.value); fd.append("__RequestVerificationToken", token);
            ids.forEach(function (id) { fd.append("SortedIds", id); });
            fetch(list.dataset.order, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd })
                .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
                .then(function (j) { if (!j.success) throw new Error("mislukt"); nrs(list); })
                .catch(function () { vorig.forEach(function (x) { ul.appendChild(x); }); nrs(list); toast("danger", "Volgorde niet bewaard", "De volgorde kon niet opgeslagen worden. Probeer het opnieuw."); });
        });
    });
})();
