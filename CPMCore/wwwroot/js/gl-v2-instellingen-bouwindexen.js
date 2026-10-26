// gl-v2 — Instellingen/Bouwindexen (design-handoff punt 48a-d). Gedrag van de pagina: tabs, uitklapbare jaren (S-index), schakelaar tussen
// I2021 en Index I+, CSV-export van het raster, waarde toevoegen/bewerken in één modal (S-index en I2021), inline invoer voor ABEX,
// geldend maken, verwijderen met bevestiging en de synchronisatie. Alle schrijfacties gaan via fetch naar InstellingenController
// (BouwindexOpslaanV2/-VerwijderV2/-GeldendV2) en herladen daarna de pagina met een toast (sessionStorage), zodat de Δ's, de
// kerncijfers en het "Geldend"-label altijd uit de database komen i.p.v. in JS nagebouwd te worden.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-bwi");
    if (!root) return;

    var canWrite = root.getAttribute("data-can-write") === "1";
    var tokenInput = root.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : "";
    var urls = {
        opslaan: root.getAttribute("data-url-opslaan"),
        verwijder: root.getAttribute("data-url-verwijder"),
        geldend: root.getAttribute("data-url-geldend"),
        syncS: root.getAttribute("data-url-sync-s"),
        syncI: root.getAttribute("data-url-sync-i")
    };

    function $(sel, ctx) { return (ctx || root).querySelector(sel); }
    function $$(sel, ctx) { return Array.prototype.slice.call((ctx || root).querySelectorAll(sel)); }

    function toast(tone, title, body) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" });
    }

    // Melding na een herlading (de pagina wordt opnieuw opgebouwd, dus de toast reist mee via sessionStorage).
    try {
        var flash = sessionStorage.getItem("bwi-flash");
        if (flash) {
            sessionStorage.removeItem("bwi-flash");
            var f = JSON.parse(flash);
            toast(f.tone, f.title, f.body);
        }
    } catch (e) { /* sessionStorage kan geblokkeerd zijn: dan geen toast, de pagina zelf is wel bijgewerkt */ }

    function reloadWith(tone, title, body) {
        try { sessionStorage.setItem("bwi-flash", JSON.stringify({ tone: tone, title: title, body: body || "" })); } catch (e) { }
        window.location.reload();
    }

    function post(url, data) {
        var fd = new FormData();
        Object.keys(data || {}).forEach(function (k) { if (data[k] !== null && data[k] !== undefined) fd.append(k, data[k]); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        });
    }

    function closeMenus() { if (window.GlV2Menu) window.GlV2Menu.closeAll(); }

    // ── Tabs: panelen tonen/verbergen, niets herladen ──────────────────────────────────────────────
    $$("[data-bwi-tab]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var key = btn.getAttribute("data-bwi-tab");
            $$("[data-bwi-tab]").forEach(function (b) {
                var on = b === btn;
                b.classList.toggle("is-active", on);
                b.setAttribute("aria-selected", on ? "true" : "false");
            });
            $$("[data-bwi-panel]").forEach(function (p) { p.hidden = p.getAttribute("data-bwi-panel") !== key; });
            try { history.replaceState(null, "", "?tab=" + key); } catch (e) { }
        });
    });

    // ── Jaargroepen (S-index) ──────────────────────────────────────────────────────────────────────
    $$("[data-bwi-year]").forEach(function (head) {
        head.addEventListener("click", function () {
            var open = head.getAttribute("aria-expanded") === "true";
            head.setAttribute("aria-expanded", open ? "false" : "true");
            var body = head.nextElementSibling;
            if (body) body.hidden = open;
        });
    });

    // ── I2021 ↔ Index I+ ───────────────────────────────────────────────────────────────────────────
    function activeGrid() {
        var sw = $("[data-bwi-grid-switch] .is-active");
        return sw ? sw.getAttribute("data-grid") : "I2021";
    }
    $$("[data-bwi-grid-switch] button").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var key = btn.getAttribute("data-grid");
            $$("[data-bwi-grid-switch] button").forEach(function (b) { b.classList.toggle("is-active", b === btn); });
            $$("[data-bwi-grid]").forEach(function (g) { g.hidden = g.getAttribute("data-bwi-grid") !== key; });
        });
    });

    // ── Exporteren: het zichtbare raster als CSV (puntkomma, nl-BE-getallen, met BOM voor Excel) ──
    var exportBtn = $("[data-bwi-export]");
    if (exportBtn) exportBtn.addEventListener("click", function () {
        var key = activeGrid();
        var grid = $('[data-bwi-grid="' + key + '"]');
        if (!grid) return;
        var maanden = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
        var lines = [["Jaar"].concat(maanden).join(";")];
        $$("[data-bwi-matrix-row]", grid).forEach(function (row) {
            var cells = [row.getAttribute("data-bwi-matrix-row")];
            for (var m = 1; m <= 12; m++) {
                var c = $('[data-maand="' + m + '"]', row);
                cells.push(c && c.getAttribute("data-waarde") ? c.getAttribute("data-waarde") : "");
            }
            lines.push(cells.join(";"));
        });
        if (lines.length === 1) { toast("info", "Niets om te exporteren", "Er zijn nog geen waarden."); return; }
        var blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
        var a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "bouwindexen-" + (key === "I+" ? "index-i-plus" : "i2021") + ".csv";
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
    });

    // ── Synchroniseren (bestaande acties SyncSIndex / SyncI2021) ───────────────────────────────────
    $$("[data-bwi-sync]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            if (btn.disabled) return;
            var isS = btn.getAttribute("data-bwi-sync") === "S";
            var bron = isS ? "arch-index.be" : "FOD Economie";
            var label = btn.innerHTML;
            btn.disabled = true;
            btn.classList.add("is-loading");
            btn.innerHTML = '<i class="ph ph-arrows-clockwise" aria-hidden="true"></i>Bezig met ophalen…';
            post(isS ? urls.syncS : urls.syncI, {}).then(function (r) {
                if (r.ok) { reloadWith("success", "Gesynchroniseerd met " + bron, r.bericht); return; }
                toast(r.waarschuwing ? "info" : "danger", r.waarschuwing ? "Geen nieuwe waarden" : "Synchroniseren mislukt", r.bericht);
            }).catch(function () {
                toast("danger", "Synchroniseren mislukt", "De server gaf geen antwoord. Probeer het later opnieuw.");
            }).then(function () {
                btn.disabled = false;
                btn.classList.remove("is-loading");
                btn.innerHTML = label;
            });
        });
    });

    if (!canWrite) return;

    // ── Waarde toevoegen/bewerken (S-index en I2021): één modal ────────────────────────────────────
    var modalEl = document.getElementById("bwi-modal");
    var modal = null;
    var form = modalEl ? $("form", modalEl) : null;
    var state = { id: 0, type: "S", jaar: "", maand: "", geldend: false, label: "" };
    var TYPE_LABEL = { "S": "S-index", "I2021": "I2021", "I+": "Index I+" };

    function showModalError(msg) {
        var err = document.getElementById("bwi-modal-err");
        if (!err) return;
        err.textContent = msg || "";
        err.hidden = !msg;
    }

    // De maandkeuze is de projectbrede keuzelijst (GlV2/_SelectList): de waarde zetten via GlV2Select.setItems met de bestaande opties.
    function setMaand(v) {
        var wrap = document.getElementById("bwi-maand_select");
        var items = Array.prototype.map.call(wrap.querySelectorAll(".gl-v2-select-option"), function (b) { return { value: b.getAttribute("data-value"), text: b.querySelector("span").textContent }; });
        window.GlV2Select.setItems(wrap, items, String(v), null);
    }

    function openModal(o) {
        if (!modalEl || !form) return;
        modal = modal || window.bootstrap.Modal.getOrCreateInstance(modalEl);
        var bestaand = o.id > 0;
        var vast = bestaand || !!o.lock;       // jaar/maand zijn de sleutel van een waarde: enkel bij een nieuwe, vrije waarde aanpasbaar
        state = { id: o.id, type: o.type, jaar: o.jaar, maand: o.maand, geldend: !!o.geldend, label: o.label || "" };
        document.getElementById("bwi-modal-title").textContent = (bestaand ? "Waarde bewerken" : "Waarde toevoegen") + " · " + (TYPE_LABEL[o.type] || o.type);
        form.elements.id.value = o.id;
        form.elements.indexType.value = o.type;
        var jaar = form.elements.jaar, maand = form.elements.maand;
        jaar.value = o.jaar || new Date().getFullYear();
        setMaand(o.maand || "");
        jaar.readOnly = vast;
        var mt = document.getElementById("bwi-maand_trigger"); mt.setAttribute("aria-disabled", vast ? "true" : "false"); mt.tabIndex = vast ? -1 : 0;
        form.elements.waarde.value = o.waarde || "";
        form.elements.opmerking.value = o.opmerking || "";
        document.getElementById("bwi-waarde-hint").textContent = o.type === "S" ? "Vier decimalen, bv. 38,8970." : "Twee decimalen, bv. 155,10.";
        var extra = $("[data-bwi-modal-extra]", modalEl);
        if (extra) extra.hidden = !bestaand;
        var g = $("[data-bwi-modal-geldend]", modalEl);
        if (g) g.hidden = !bestaand || state.geldend;
        showModalError("");
        modal.show();
        modalEl.addEventListener("shown.bs.modal", function once() {
            modalEl.removeEventListener("shown.bs.modal", once);
            var target = vast ? form.elements.waarde : form.elements.jaar;
            target.focus();
            if (target.select) target.select();
        });
    }

    function fromDataset(el) {
        var d = el.dataset;
        return {
            id: parseInt(d.id || "0", 10), type: d.type, jaar: d.jaar, maand: d.maand, waarde: d.waarde,
            opmerking: d.opmerking, geldend: d.geldend === "1", label: d.label,
            lock: d.id === "0"
        };
    }

    $$("[data-bwi-add]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var type = btn.getAttribute("data-bwi-add");
            if (type === "I2021") type = activeGrid();
            openModal({ id: 0, type: type, jaar: "", maand: "" });
        });
    });

    root.addEventListener("click", function (e) {
        var cell = e.target.closest("[data-bwi-cell]");
        if (cell && !cell.hasAttribute("data-readonly")) { openModal(fromDataset(cell)); return; }
        var edit = e.target.closest("[data-bwi-edit]");
        if (edit) { closeMenus(); openModal(fromDataset(edit)); }
    });

    if (form) form.addEventListener("submit", function (e) {
        e.preventDefault();
        var jaar = state.id > 0 ? state.jaar : form.elements.jaar.value.trim();
        var maand = state.id > 0 ? state.maand : form.elements.maand.value;
        var waarde = form.elements.waarde.value.trim();
        if (!/^\d{4}$/.test(String(jaar))) { showModalError("Geef een jaar van vier cijfers in."); form.elements.jaar.focus(); return; }
        if (!maand) { showModalError("Kies een maand."); document.getElementById("bwi-maand_trigger").focus(); return; }
        if (!waarde) { showModalError("Geef de indexwaarde in."); form.elements.waarde.focus(); return; }
        var submit = $('button[type="submit"]', form);
        submit.disabled = true;
        post(urls.opslaan, { id: state.id, indexType: state.type, jaar: jaar, maand: maand, waarde: waarde, opmerking: form.elements.opmerking.value })
            .then(function (r) {
                if (r.ok) reloadWith("success", "Waarde opgeslagen", (TYPE_LABEL[state.type] || state.type) + " " + String(maand).padStart(2, "0") + "/" + jaar);
                else { showModalError(r.bericht || "Opslaan is niet gelukt."); submit.disabled = false; }
            })
            .catch(function () { showModalError("De server gaf geen antwoord. Probeer het opnieuw."); submit.disabled = false; });
    });

    // ── Geldend maken en verwijderen (rijmenu, ABEX-menu en modal delen dezelfde functies) ─────────
    function maakGeldend(id, type, label) {
        closeMenus();
        post(urls.geldend, { id: id, indexType: type }).then(function (r) {
            if (r.ok) reloadWith("success", "Geldende waarde aangepast", label + " is nu de geldende waarde. Nieuwe berekeningen gebruiken ze meteen.");
            else toast("danger", "Aanpassen is niet gelukt", "");
        }).catch(function () { toast("danger", "Aanpassen is niet gelukt", "De server gaf geen antwoord."); });
    }

    function verwijder(id, label) {
        closeMenus();
        var vraag = window.GlV2Bevestig
            ? window.GlV2Bevestig({ title: label + " verwijderen?", desc: "De waarde verdwijnt uit de lijst. Bestaande budgetten veranderen niet. Is dit de geldende waarde, dan geldt voortaan de meest recente.", ok: "Verwijderen", tone: "danger" })
            : Promise.resolve(window.confirm(label + " verwijderen?"));
        vraag.then(function (ok) {
            if (!ok) return;
            post(urls.verwijder, { id: id }).then(function (r) {
                if (r.ok) reloadWith("success", "Waarde verwijderd", label);
                else toast("danger", "Verwijderen is niet gelukt", "De waarde bestaat niet meer. Vernieuw de pagina.");
            }).catch(function () { toast("danger", "Verwijderen is niet gelukt", "De server gaf geen antwoord."); });
        });
    }

    root.addEventListener("click", function (e) {
        var g = e.target.closest("[data-bwi-geldend]");
        if (g) { maakGeldend(g.getAttribute("data-id"), g.getAttribute("data-type"), g.getAttribute("data-label")); return; }
        var d = e.target.closest("[data-bwi-delete]");
        if (d) verwijder(d.getAttribute("data-id"), d.getAttribute("data-label"));
    });

    if (modalEl) {
        var mg = $("[data-bwi-modal-geldend]", modalEl);
        var md = $("[data-bwi-modal-delete]", modalEl);
        var label = function () { return (TYPE_LABEL[state.type] || state.type) + " " + String(state.maand).padStart(2, "0") + "/" + state.jaar; };
        if (mg) mg.addEventListener("click", function () { modal.hide(); maakGeldend(state.id, state.type, label()); });
        if (md) md.addEventListener("click", function () {
            var id = state.id, l = label();
            modal.hide();
            // de bevestiging is zelf een modal: pas openen als deze helemaal dicht is (Bootstrap ondersteunt geen twee tegelijk)
            modalEl.addEventListener("hidden.bs.modal", function once() { modalEl.removeEventListener("hidden.bs.modal", once); verwijder(id, l); });
        });
    }

    // ── ABEX: inline rij voor een nieuw jaar en per rij bewerken ───────────────────────────────────
    function parseNl(s) {
        s = String(s || "").replace(/\s| /g, "");
        if (s.indexOf(",") >= 0) s = s.replace(/\./g, "").replace(",", ".");
        var n = parseFloat(s);
        return isNaN(n) ? null : n;
    }
    function fmtPct(p) {
        return (p >= 0 ? "+" : "−") + Math.abs(p).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " %";
    }

    // jaar → waarde van de bestaande rijen, voor de live Δ in het formulier
    function abexWaarden() {
        var map = {};
        $$("[data-bwi-abex-row]").forEach(function (row) {
            var jaar = parseInt($(".gl-v2-bwi-strong", row).textContent, 10);
            var w = parseNl($(".gl-v2-bwi-num", row).textContent);
            if (!isNaN(jaar) && w !== null) map[jaar] = w;
        });
        return map;
    }

    function liveDelta(f) {
        var out = $("[data-bwi-live-delta]", f);
        if (!out) return;
        var jaar = parseInt(f.elements.jaar.value, 10), nu = parseNl(f.elements.waarde.value);
        var map = abexWaarden(), vorig = null, vorigJaar = -1;
        Object.keys(map).forEach(function (k) { k = parseInt(k, 10); if (k < jaar && k > vorigJaar && !(k === jaar)) { vorigJaar = k; vorig = map[k]; } });
        out.textContent = (nu && vorig) ? fmtPct((nu / vorig - 1) * 100) : "—";
    }

    function sluitAbexForms() {
        $$("[data-bwi-abex-form]").forEach(function (f) {
            if (f.hidden) return;
            f.hidden = true;
            var vorige = f.previousElementSibling;
            if (vorige && vorige.hasAttribute("data-bwi-abex-row")) vorige.hidden = false;
        });
    }

    function openAbexNieuw() {
        sluitAbexForms();
        var f = $('[data-bwi-abex-form][data-id="0"]');
        if (!f) return;
        f.hidden = false;
        liveDelta(f);
        f.scrollIntoView({ block: "nearest", behavior: "smooth" });
        f.elements.waarde.focus();
    }

    $$("[data-bwi-abex-new]").forEach(function (b) { b.addEventListener("click", openAbexNieuw); });

    root.addEventListener("click", function (e) {
        var edit = e.target.closest("[data-bwi-abex-edit]");
        if (edit) {
            closeMenus();
            sluitAbexForms();
            var row = edit.closest("[data-bwi-abex-row]");
            var f = row && row.nextElementSibling;
            if (!f || !f.hasAttribute("data-bwi-abex-form")) return;
            row.hidden = true;
            f.hidden = false;
            liveDelta(f);
            f.elements.waarde.focus();
            f.elements.waarde.select();
            return;
        }
        if (e.target.closest("[data-bwi-abex-cancel]")) sluitAbexForms();
    });

    $$("[data-bwi-abex-form]").forEach(function (f) {
        f.addEventListener("input", function () { liveDelta(f); });
        f.addEventListener("keydown", function (e) { if (e.key === "Escape") { e.preventDefault(); sluitAbexForms(); } });
        f.addEventListener("submit", function (e) {
            e.preventDefault();
            var id = parseInt(f.getAttribute("data-id") || "0", 10);
            var jaar = f.elements.jaar.value.trim();
            if (!/^\d{4}$/.test(jaar)) { toast("warning", "Jaar niet geldig", "Geef een jaar van vier cijfers in."); f.elements.jaar.focus(); return; }
            if (!f.elements.waarde.value.trim()) { toast("warning", "Waarde ontbreekt", "Geef de ABEX-waarde in."); f.elements.waarde.focus(); return; }
            var submit = $('button[type="submit"]', f);
            submit.disabled = true;
            post(urls.opslaan, { id: id, indexType: "ABEX", jaar: jaar, waarde: f.elements.waarde.value, opmerking: f.elements.opmerking.value })
                .then(function (r) {
                    if (r.ok) reloadWith("success", "ABEX opgeslagen", "ABEX " + jaar);
                    else { toast("danger", "Opslaan is niet gelukt", r.bericht || ""); submit.disabled = false; }
                })
                .catch(function () { toast("danger", "Opslaan is niet gelukt", "De server gaf geen antwoord."); submit.disabled = false; });
        });
    });
})();
