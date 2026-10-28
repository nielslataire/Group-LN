// gl-v2 — Instellingen › Blogartikelen › Bewerken (punt 55b): tellers, Google-voorbeeld, ontdek-meer-keuzes, inhoudsblokken en vragen (gedeelde modals, verwijderen met bevestiging,
// herschikken naar de bestaande …Volgorde-acties) en een waarschuwing bij niet-opgeslagen wijzigingen.
(function () {
    "use strict";
    var form = document.getElementById("bl-form");
    if (!form) return;
    var data = JSON.parse(document.getElementById("bl-data").textContent);
    var token = (form.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var canWrite = form.dataset.canWrite === "1";
    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js

    // ── Tellers ──
    form.querySelectorAll("[data-tel]").forEach(function (el) {
        var inp = $(el.dataset.tel), max = parseInt(el.dataset.max, 10);
        function t() { var n = inp.value.length; el.textContent = n + " / " + max; el.classList.toggle("is-over", n > max); }
        inp.addEventListener("input", t); t();
    });

    // ── Google-voorbeeld ──
    function google() {
        var titel = $("bl-mtitel").value.trim() || $("bl-titel").value.trim() || "Titel van het artikel";
        var tekst = $("bl-momschr").value.trim() || $("bl-preview").value.trim() || "Omschrijving van het artikel.";
        var slug = $("bl-slug").value.trim() || ($("bl-titel").value.trim().toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, ""));
        $("bl-g-url").textContent = "groupln.be › blog" + (slug ? " › " + (slug.length > 28 ? slug.slice(0, 28) + "…" : slug) : "");
        $("bl-g-titel").textContent = titel.length > 62 ? titel.slice(0, 62) + "…" : titel;
        $("bl-g-tekst").textContent = tekst.length > 160 ? tekst.slice(0, 160) + "…" : tekst;
    }
    ["bl-mtitel", "bl-titel", "bl-momschr", "bl-preview", "bl-slug"].forEach(function (id) { $(id).addEventListener("input", google); });
    google();

    // ── Vuile staat, publicatie, omslagfoto ──
    function snap() { return JSON.stringify(Array.from(form.elements).filter(function (e) { return e.name && e.type !== "file" && e.name !== "__RequestVerificationToken" && (e.type !== "checkbox" || e.checked); }).map(function (e) { return e.name + "=" + e.value; }).concat([($("bl-foto").files || []).length])); }
    var start = snap(), badge = $("bl-dirty"), submitting = false;
    function dirty() { return snap() !== start; }
    function update() { if (badge) badge.hidden = !dirty(); }
    form.addEventListener("input", update); form.addEventListener("change", update);
    $("bl-pub").addEventListener("change", function () { var b = $("bl-pub-badge"); b.textContent = this.checked ? "Gepubliceerd" : "Concept"; b.className = "gl-v2-badge " + (this.checked ? "is-positive" : "is-neutral"); });
    form.addEventListener("submit", function () { submitting = true; });
    window.addEventListener("beforeunload", function (e) { if (canWrite && !submitting && dirty()) { e.preventDefault(); e.returnValue = ""; } });
    if (badge) badge.hidden = true;
    $("bl-foto_zone").addEventListener("gl-v2:dropzone-remove-current", function () { $("bl-foto-cur").value = ""; var p = $("bl-foto-pv"); if (p) p.hidden = true; update(); });

    // ── Ontdek meer: de keuzelijst volgt het type ──
    [1, 2, 3].forEach(function (n) {
        var t = $("bl-l" + n + "t"), i = $("bl-l" + n + "i"), wrap = $("bl-l" + n + "i_select");
        if (!t || !i || !wrap || !window.GlV2Select) return;
        t.addEventListener("change", function () {
            var items = this.value === "artikel" ? data.artikelen : this.value === "project" ? data.projecten : [];
            window.GlV2Select.setItems(wrap, items.map(function (o) { return { value: o.value, text: o.text }; }), "", null);
            i.value = "";
            var trig = $("bl-l" + n + "i_trigger"); if (trig) { if (this.value) { trig.removeAttribute("aria-disabled"); trig.tabIndex = 0; } else { trig.setAttribute("aria-disabled", "true"); trig.tabIndex = -1; } }
            update();
        });
    });

    // ── Artikel verwijderen ──
    var delArt = $("bl-delete");
    if (delArt) delArt.addEventListener("click", function () {
        window.GlV2Bevestig({ title: "Artikel verwijderen?", desc: "“" + $("bl-titel").value + "” en alle blokken en vragen verdwijnen. Je kunt dit niet ongedaan maken.", ok: "Verwijderen", tone: "danger" })
            .then(function (ok) { if (ok) { submitting = true; $("bl-del-art").submit(); } });
    });

    // ── Blokken en vragen ──
    if (!$("bl-blok-modal")) return;
    var Bs = window.bootstrap;
    var blokModal = Bs ? Bs.Modal.getOrCreateInstance($("bl-blok-modal")) : null, faqModal = Bs ? Bs.Modal.getOrCreateInstance($("bl-faq-modal")) : null;
    var blokForm = $("bl-blok-form"), faqForm = $("bl-faq-form"), delForm = $("bl-del-form"), delId = $("bl-del-id");

    var quill = null;
    if (typeof Quill !== "undefined") {
        quill = new Quill("#bl-blok-quill", { theme: "snow", modules: { toolbar: [["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], [{ header: [2, 3, 4, false] }], ["link"], ["clean"]] } });
        $("bl-blok-modal").querySelectorAll(".ql-toolbar button, .ql-toolbar .ql-picker-label").forEach(function (el) { el.setAttribute("type", "button"); });
    }
    var LABELS = { tekst: "tekstblok", quote: "quote", tip: "tip", foto: "foto", knop: "knop" };
    function velden(type) {
        var toon = { titel: type !== "quote" && type !== "knop", tekst: type !== "foto" && type !== "knop", foto: type === "tekst" || type === "foto", knop: type === "knop" };
        blokForm.querySelectorAll("[data-veld]").forEach(function (el) { el.hidden = !toon[el.dataset.veld]; });
        $("bl-blok-titel-label").textContent = type === "foto" ? "Bijschrift" : "Bloktitel";
    }
    function nextSort(listEl) {
        return Array.from(listEl.querySelectorAll(".gl-v2-inh-item")).reduce(function (m, x) { return Math.max(m, parseInt(x.dataset.sort || "0", 10)); }, -10) + 10;
    }
    function openBlok(type, id) {
        var b = id ? data.blokken.filter(function (x) { return x.id === id; })[0] : null, list = document.querySelector('[data-bl-list="blok"]');
        blokForm.elements.ID.value = b ? b.id : "0";
        blokForm.elements.SortOrder.value = b ? b.sort : (data.blokken.reduce(function (m, x) { return Math.max(m, x.sort); }, -10) + 10);
        type = b ? b.type : type;
        blokForm.elements.BlokType.value = type;
        $("bl-blok-titel").value = b ? (b.titel || "") : "";
        $("bl-blok-knoptekst").value = b ? (b.knopTekst || "") : "";
        $("bl-blok-knopurl").value = b ? (b.knopUrl || "") : "";
        $("bl-blok-foto-cur").value = b ? (b.foto || "") : "";
        var pv = $("bl-blok-foto-pv"); pv.hidden = !(b && b.foto); if (b && b.foto) pv.src = b.foto;
        if (window.GlV2Dropzone) window.GlV2Dropzone.reset($("bl-blok-foto_zone") || $("bl-blok-foto"));
        if (quill) { quill.setContents([]); if (b && b.tekst) quill.clipboard.dangerouslyPasteHTML(b.tekst, "silent"); }
        velden(type);
        $("bl-blok-title").textContent = (b ? "Bewerken: " : "Nieuw: ") + (LABELS[type] || "blok");
        if (blokModal) blokModal.show();
    }
    blokForm.addEventListener("submit", function (e) {
        var type = blokForm.elements.BlokType.value;
        $("bl-blok-tekst").value = quill && quill.getText().trim() ? quill.root.innerHTML : "";
        if (type === "knop" && !$("bl-blok-knoptekst").value.trim()) { e.preventDefault(); toast("warning", "Knoptekst ontbreekt", "Vul de tekst van de knop in."); return; }
        submitting = true;
    });
    $("bl-blok-foto_zone").addEventListener("gl-v2:dropzone-remove-current", function () { $("bl-blok-foto-cur").value = ""; $("bl-blok-foto-pv").hidden = true; });

    function openFaq(id) {
        var f = id ? data.faq.filter(function (x) { return x.id === id; })[0] : null;
        faqForm.elements.ID.value = f ? f.id : "0";
        faqForm.elements.SortOrder.value = f ? f.sort : (data.faq.reduce(function (m, x) { return Math.max(m, x.sort); }, -10) + 10);
        $("bl-faq-vraag").value = f ? f.vraag : ""; $("bl-faq-antwoord").value = f ? (f.antwoord || "") : "";
        $("bl-faq-title").textContent = f ? "Vraag bewerken" : "Vraag toevoegen";
        if (faqModal) faqModal.show();
    }
    faqForm.addEventListener("submit", function (e) { if (!$("bl-faq-vraag").value.trim()) { e.preventDefault(); toast("warning", "Vraag ontbreekt", "Vul de vraag in voor je opslaat."); return; } submitting = true; });

    document.querySelectorAll("[data-bl-add]").forEach(function (b) { b.addEventListener("click", function () { var k = b.dataset.blAdd; if (k === "faq") openFaq(0); else openBlok(k, 0); }); });
    document.querySelectorAll("[data-bl-list]").forEach(function (list) {
        var kind = list.dataset.blList, ul = list.querySelector("[data-bl-items]"), leeg = list.querySelector("[data-bl-empty]");
        ul.addEventListener("click", function (e) {
            var li = e.target.closest(".gl-v2-inh-item"); if (!li) return;
            var id = parseInt(li.dataset.id, 10);
            if (e.target.closest("[data-bl-edit]")) { if (kind === "faq") openFaq(id); else openBlok("", id); }
            if (e.target.closest("[data-bl-del]")) {
                var naam = li.querySelector(".t").textContent;
                window.GlV2Bevestig({ title: kind === "faq" ? "Vraag verwijderen?" : "Blok verwijderen?", desc: "“" + (naam.length > 80 ? naam.slice(0, 80) + "…" : naam) + "” verdwijnt uit het artikel.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                    if (!ok) return;
                    delForm.action = kind === "faq" ? form.dataset.urlFaqDel : form.dataset.urlBlokDel; delId.name = kind === "faq" ? "faqId" : "blokId"; delId.value = id; submitting = true; delForm.submit();
                });
            }
        });
        if (leeg) leeg.hidden = ul.children.length > 0;

        // Herschikken (HTML5 slepen) → BlokVolgorde / FaqVolgorde.
        if (!ul.querySelector("[draggable]")) return;
        var drag = null, vorig = null;
        ul.addEventListener("dragstart", function (e) { var li = e.target.closest(".gl-v2-inh-item"); if (!li) return; drag = li; vorig = Array.from(ul.children); li.classList.add("is-dragging"); e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", li.dataset.id); } catch (x) { /* oudere browsers */ } });
        ul.addEventListener("dragover", function (e) {
            if (!drag) return; e.preventDefault();
            var over = e.target.closest(".gl-v2-inh-item"); if (!over || over === drag) return;
            var r = over.getBoundingClientRect(); ul.insertBefore(drag, e.clientY > r.top + r.height / 2 ? over.nextSibling : over);
        });
        ul.addEventListener("dragend", function () {
            if (!drag) return;
            drag.classList.remove("is-dragging");
            var ids = Array.from(ul.children).map(function (x) { return x.dataset.id; }), oud = vorig.map(function (x) { return x.dataset.id; });
            drag = null;
            if (ids.join() === oud.join()) return;
            var fd = new FormData(); fd.append("ArtikelId", form.dataset.artikel); fd.append("__RequestVerificationToken", token);
            ids.forEach(function (id) { fd.append("SortedIds", id); });
            fetch(kind === "faq" ? form.dataset.urlFaqOrd : form.dataset.urlBlokOrd, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd })
                .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
                .then(function (j) { if (!j.success) throw new Error("mislukt"); })
                .catch(function () { vorig.forEach(function (x) { ul.appendChild(x); }); toast("danger", "Volgorde niet bewaard", "De volgorde kon niet opgeslagen worden. Probeer het opnieuw."); });
        });
    });
})();
