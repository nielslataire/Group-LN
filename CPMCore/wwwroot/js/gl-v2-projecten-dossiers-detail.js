// gl-v2 — Dossiers/DetailsV2 (design-handoff 31d). Checklist afvinken (zonder pagina-herlaad), eigen item toevoegen, mijlpaal koppelen,
// opmerking toevoegen, "Bewerken" van een ander dossier (31f-modal) en documenten opladen naar het documentencentrum.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-dos-detail");
    if (!root) return;
    function $(sel, r) { return (r || document).querySelector(sel); }
    function $$(sel, r) { return Array.prototype.slice.call((r || document).querySelectorAll(sel)); }
    var cfg = JSON.parse((document.getElementById("gl-v2-dos-config") || { textContent: "{}" }).textContent);
    var canWrite = root.getAttribute("data-can-write") === "1";
    function token() { var t = $('input[name="__RequestVerificationToken"]'); return t ? t.value : ""; }

    // ── Checklist afvinken ────────────────────────────────────────────────────────────────────────
    function updateCount() {
        var boxes = $$(".js-dos-chk"), klaar = boxes.filter(function (b) { return b.checked; }).length;
        var c = document.getElementById("gl-v2-dos-chk-count"); if (c) c.textContent = klaar + " / " + boxes.length;
    }
    document.addEventListener("change", function (e) {
        var cb = e.target.closest(".js-dos-chk");
        if (!cb || !canWrite) return;
        var li = cb.closest(".gl-v2-dos-chk-item");
        li.classList.add("is-busy");
        var body = new URLSearchParams();
        body.set("nieuweStatus", cb.checked ? "2" : "0");
        fetch(cfg.substapUrl.replace(/\/Substap\/0\//, "/Substap/" + cb.getAttribute("data-substap-id") + "/"), {
            method: "POST",
            headers: { "RequestVerificationToken": token(), "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" },
            body: body.toString(), credentials: "same-origin"
        }).then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
          .then(function () { li.classList.remove("is-busy"); li.classList.toggle("is-done", cb.checked); updateCount(); window.location.reload(); })
          .catch(function () { li.classList.remove("is-busy"); cb.checked = !cb.checked; if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Niet gelukt", body: "De stap kon niet bijgewerkt worden." }); });
    });

    // ── Item toevoegen / mijlpaal koppelen / opmerking ────────────────────────────────────────────
    var addBtn = document.getElementById("gl-v2-dos-chk-add-btn"), addCancel = document.getElementById("gl-v2-dos-chk-add-cancel");
    if (addBtn) {
        var addRow = $(".gl-v2-dos-chk-add-row", document.getElementById("gl-v2-dos-chk-add"));
        addBtn.addEventListener("click", function () { addRow.hidden = false; addBtn.hidden = true; var i = $("input", addRow); if (i) i.focus(); });
        if (addCancel) addCancel.addEventListener("click", function () { addRow.hidden = true; addBtn.hidden = false; });
    }
    function toggler(btnId, formId) {
        var b = document.getElementById(btnId), f = document.getElementById(formId);
        if (!b || !f) return;
        b.addEventListener("click", function () {
            var open = f.hidden; f.hidden = !open; b.setAttribute("aria-expanded", open ? "true" : "false");
            if (open) { var i = $("input[type=text]", f); if (i) i.focus(); }
        });
    }
    toggler("gl-v2-dos-koppel-btn", "gl-v2-dos-koppel-form");
    toggler("gl-v2-dos-note-btn", "gl-v2-dos-note-form");

    // ── Bewerken van een ander dossier (31f) ──────────────────────────────────────────────────────
    document.addEventListener("click", function (e) {
        var b = e.target.closest(".js-dos-edit-ander");
        if (b && canWrite && window.GlV2DosAnder) window.GlV2DosAnder.open({ id: b.getAttribute("data-dossier-id") });
    });

    // ── Documenten opladen (documentencentrum, koppeling "dossier") ───────────────────────────────
    var upBtn = document.getElementById("gl-v2-dos-upload-btn"), upInput = document.getElementById("gl-v2-dos-upload-input");
    if (upBtn && upInput) {
        upBtn.addEventListener("click", function () { upInput.click(); });
        upInput.addEventListener("change", function () {
            var file = upInput.files && upInput.files[0];
            if (!file) return;
            var fd = new FormData();
            fd.append("file", file);
            upBtn.disabled = true;
            var list = $(".gl-v2-dos-docs"); if (list) list.classList.add("is-uploading");
            fetch(cfg.uploadUrl, { method: "POST", headers: { "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" }, body: fd, credentials: "same-origin" })
                .then(function (r) { return r.json().catch(function () { return { ok: false }; }); })
                .then(function (j) {
                    if (j && (j.ok || j.success)) { window.location.reload(); return; }
                    upBtn.disabled = false; if (list) list.classList.remove("is-uploading"); upInput.value = "";
                    if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Opladen mislukt", body: (j && (j.message || j.error)) || "Het document kon niet opgeladen worden." });
                })
                .catch(function () { upBtn.disabled = false; if (list) list.classList.remove("is-uploading"); if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Opladen mislukt", body: "Controleer je verbinding en probeer opnieuw." }); });
        });
    }
})();
