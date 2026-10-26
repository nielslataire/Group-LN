// gl-v2 — Instellingen/Activiteiten en Activiteitgroepen (design-handoff punt 51a/51b). De pagina krijgt de volledige stand (groepen +
// activiteiten) als JSON mee en tekent de lijsten zelf: zoeken, filter op lot, sorteren, paginering en bewerken per rij gebeuren in de
// browser; elke wijziging gaat via fetch naar InstellingenController (Activiteit…V2 / Activiteitgroep…V2), die de nieuwe stand teruggeeft.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-act");
    if (!root) return;

    var canWrite = root.getAttribute("data-can-write") === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var urls = {
        actSave: root.getAttribute("data-url-act-save"), actDelete: root.getAttribute("data-url-act-delete"),
        grpSave: root.getAttribute("data-url-grp-save"), grpDelete: root.getAttribute("data-url-grp-delete")
    };
    var PAGE = 25;

    var stand = JSON.parse(document.getElementById("act-data").textContent);
    var ui = { tab: root.getAttribute("data-start-tab"), lot: 0, q: "", sort: "naam", dir: 1, page: 1, editAct: 0, editGrp: 0 };

    function $(id) { return document.getElementById(id); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    function plural(n, een, meer) { return n + " " + (n === 1 ? een : meer); }

    function groupById(id) { return stand.groepen.filter(function (g) { return g.id === id; })[0]; }
    function nrOf(a) { var g = groupById(a.groepId); return g && a.volgnr != null ? g.lot + "." + a.volgnr : (g ? g.lot + ".–" : "–"); }
    function nrSort(a) { var g = groupById(a.groepId); return (g ? g.lot : 0) * 100000 + (a.volgnr || 0); }

    function post(url, data) {
        var fd = new FormData();
        Object.keys(data).forEach(function (k) { if (data[k] !== null && data[k] !== undefined) fd.append(k, data[k]); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        });
    }

    // Verwerkt het antwoord van een schrijfactie: nieuwe stand overnemen, melding tonen. Geeft true terug bij succes.
    function afhandelen(r, okTitel, okBody) {
        if (r.stand) stand = r.stand;
        if (r.ok) { if (okTitel) toast("success", okTitel, okBody); return true; }
        toast("danger", "Niet gelukt", r.bericht || "");
        return false;
    }
    function netwerkFout() { toast("danger", "Niet gelukt", "De server gaf geen antwoord. Probeer het opnieuw."); }

    // ── Tabs ──────────────────────────────────────────────────────────────────────────────────────
    function showTab(key) {
        ui.tab = key;
        root.querySelectorAll("[data-act-tab]").forEach(function (b) {
            var on = b.getAttribute("data-act-tab") === key;
            b.classList.toggle("is-active", on);
            b.setAttribute("aria-selected", on ? "true" : "false");
        });
        root.querySelectorAll("[data-act-panel]").forEach(function (p) { p.hidden = p.getAttribute("data-act-panel") !== key; });
        try { history.replaceState(null, "", "?tab=" + key); } catch (e) { }
    }
    root.querySelectorAll("[data-act-tab]").forEach(function (b) { b.addEventListener("click", function () { showTab(b.getAttribute("data-act-tab")); }); });

    // ── Activiteiten ──────────────────────────────────────────────────────────────────────────────
    function filtered() {
        var q = norm(ui.q);
        var list = stand.activiteiten.filter(function (a) {
            if (ui.lot && a.groepId !== ui.lot) return false;
            if (!q) return true;
            var g = groupById(a.groepId);
            return norm(a.naam).indexOf(q) >= 0 || norm(nrOf(a)).indexOf(q) >= 0 || (g && norm(g.naam).indexOf(q) >= 0);
        });
        list.sort(function (a, b) {
            var x, y;
            if (ui.sort === "nr") { x = nrSort(a); y = nrSort(b); }
            else if (ui.sort === "groep") { var ga = groupById(a.groepId), gb = groupById(b.groepId); x = ga ? ga.lot : 0; y = gb ? gb.lot : 0; if (x === y) { x = norm(a.naam); y = norm(b.naam); } }
            else { x = norm(a.naam); y = norm(b.naam); }
            return (x < y ? -1 : x > y ? 1 : 0) * ui.dir;
        });
        return list;
    }

    function renderLots() {
        var html = '<button type="button" class="gl-v2-act-lot' + (ui.lot === 0 ? " is-active" : "") + '" data-lot="0"><span class="n">Alle loten</span><span class="c">' + stand.activiteiten.length + "</span></button>";
        stand.groepen.forEach(function (g) {
            html += '<button type="button" class="gl-v2-act-lot' + (ui.lot === g.id ? " is-active" : "") + '" data-lot="' + g.id + '"><span class="l">' + g.lot + '</span><span class="n">' + esc(g.naam) + '</span><span class="c">' + g.aantal + "</span></button>";
        });
        $("act-lots").innerHTML = html;
        // Keuzelijsten zijn de projectbrede keuzelijst (GlV2/_SelectList, window.GlV2Select).
        var items = stand.groepen.map(function (g) { return { value: g.id, text: g.lot + " · " + g.naam }; });
        window.GlV2Select.setItems($("act-lot-select_select"), items, ui.lot || "", "Alle activiteitgroepen");
        if ($("act-add-groep")) {
            var cur = parseInt($("act-add-groep").value, 10) || 0;
            var sel = ui.lot || cur || (stand.groepen[0] ? stand.groepen[0].id : 0);
            window.GlV2Select.setItems($("act-add-groep_select"), items, sel, null);
            updateAddNr();
        }
    }

    function updateAddNr() {
        var el = $("act-add-nr"); if (!el) return;
        var gid = parseInt($("act-add-groep").value, 10) || 0, g = groupById(gid);
        el.textContent = g ? g.lot + ".–" : "—";
    }

    // Keuzelijst voor de groep in een bewerkrij: zelfde markup als GlV2/_SelectList (basisvariant); window.GlV2Select.init() bedraadt hem na het tekenen.
    function groupSelectHtml(selected) {
        var cur = groupById(selected);
        var opts = stand.groepen.map(function (g) {
            return '<button type="button" class="gl-v2-select-option' + (g.id === selected ? " is-selected" : "") + '" role="option" data-value="' + g.id + '"><i class="ph ph-check" aria-hidden="true"></i><span>' + g.lot + " · " + esc(g.naam) + "</span></button>";
        }).join("");
        return '<div class="gl-v2-select" data-gl-v2-select data-placeholder="Kies een groep"><input type="hidden" name="groepId" value="' + (cur ? cur.id : "") + '" />' +
            '<div class="gl-v2-select-trigger' + (cur ? " is-filled" : "") + '" role="button" tabindex="0" aria-haspopup="listbox" aria-expanded="false" aria-label="Activiteitgroep"><span class="gl-v2-select-trigger-label">' + (cur ? cur.lot + " · " + esc(cur.naam) : "Kies een groep") + '</span><i class="ph ph-caret-down gl-v2-select-trigger-caret" aria-hidden="true"></i></div>' +
            '<div class="gl-v2-select-panel" role="listbox">' + opts + "</div></div>";
    }

    function renderActs() {
        var list = filtered();
        var total = list.length, pages = Math.max(1, Math.ceil(total / PAGE));
        if (ui.page > pages) ui.page = pages;
        var from = (ui.page - 1) * PAGE, slice = list.slice(from, from + PAGE);

        var html = "";
        slice.forEach(function (a) {
            var g = groupById(a.groepId);
            if (canWrite && ui.editAct === a.id) {
                html += '<form class="gl-v2-act-row gl-v2-act-grid--act is-edit" data-edit-act="' + a.id + '" autocomplete="off">' +
                    '<div class="gl-v2-field"><div class="gl-v2-field-box"><span class="gl-v2-act-prefix" id="edit-prefix">' + (g ? g.lot : "") + '.</span><span class="gl-v2-field-divider"></span><input type="text" inputmode="numeric" name="volgnr" class="gl-v2-field-input is-tabular" value="' + (a.volgnr == null ? "" : a.volgnr) + '" maxlength="4" aria-label="Volgnummer" /></div></div>' +
                    '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" name="naam" class="gl-v2-field-input" value="' + esc(a.naam) + '" maxlength="250" aria-label="Naam" /></div></div>' +
                    '<div class="gl-v2-field gl-v2-field-bare">' + groupSelectHtml(a.groepId) + '</div>' +
                    '<div class="gl-v2-act-actions"><button type="submit" class="gl-v2-row-action gl-v2-row-action-primary" title="Opslaan" aria-label="Opslaan"><i class="ph ph-check" aria-hidden="true"></i></button>' +
                    '<button type="button" class="gl-v2-row-action" data-cancel-act title="Annuleren" aria-label="Annuleren"><i class="ph ph-x" aria-hidden="true"></i></button></div></form>';
            } else {
                html += '<div class="gl-v2-act-row gl-v2-act-grid--act"><span class="gl-v2-act-nr">' + esc(nrOf(a)) + "</span>" +
                    '<span class="gl-v2-act-name" title="' + esc(a.naam) + '">' + esc(a.naam) + "</span>" +
                    '<span class="gl-v2-act-group">' + (g ? '<span class="gl-v2-badge is-neutral">' + g.lot + " · " + esc(g.naam) + "</span>" : "—") + "</span>" +
                    '<div class="gl-v2-act-actions">' + (canWrite ?
                        '<button type="button" class="gl-v2-row-action gl-v2-row-action-primary" data-edit-act-btn="' + a.id + '" title="Bewerken" aria-label="Bewerk ' + esc(a.naam) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
                        '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-act="' + a.id + '" title="Verwijderen" aria-label="Verwijder ' + esc(a.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div></div>";
            }
        });
        $("act-rows").innerHTML = html;
        window.GlV2Select.init($("act-rows"));

        var leeg = total === 0;
        $("act-empty").hidden = !leeg;
        if (leeg) {
            var gefilterd = ui.lot || ui.q;
            $("act-empty-t").textContent = gefilterd ? "Geen activiteiten voor dit filter" : "Nog geen activiteiten";
            $("act-empty-reset").hidden = !gefilterd;
        }
        var g0 = ui.lot ? groupById(ui.lot) : null;
        $("act-info").textContent = (ui.lot && g0 ? "lot " + g0.lot + " · " + g0.naam + " · " : "") + plural(stand.activiteiten.filter(function (a) { return !ui.lot || a.groepId === ui.lot; }).length, "activiteit", "activiteiten") + (ui.lot ? "" : " in " + plural(stand.groepen.length, "groep", "groepen"));
        $("act-foot").textContent = total ? (from + 1) + "–" + (from + slice.length) + " van " + plural(total, "activiteit", "activiteiten") : "";
        renderPager(pages);
        $("act-count-tab").textContent = stand.activiteiten.length;
        document.querySelectorAll(".gl-v2-act-sort").forEach(function (b) {
            var on = b.getAttribute("data-sort") === ui.sort;
            b.classList.toggle("is-active", on);
            var i = b.querySelector("i"); i.className = "ph" + (on ? (ui.dir === 1 ? " ph-arrow-up" : " ph-arrow-down") : "");
        });
    }

    function renderPager(pages) {
        var el = $("act-pager");
        if (pages <= 1) { el.innerHTML = ""; return; }
        var nums = [], p = ui.page;
        for (var i = 1; i <= pages; i++) if (i === 1 || i === pages || Math.abs(i - p) <= 1) nums.push(i);
        var html = '<button type="button" data-page="' + (p - 1) + '"' + (p === 1 ? " disabled" : "") + ' aria-label="Vorige pagina">‹</button>', last = 0;
        nums.forEach(function (n) {
            if (n - last > 1) html += '<span class="gap">…</span>';
            html += '<button type="button" data-page="' + n + '"' + (n === p ? ' class="is-active" aria-current="page"' : "") + ">" + n + "</button>";
            last = n;
        });
        html += '<button type="button" data-page="' + (p + 1) + '"' + (p === pages ? " disabled" : "") + ' aria-label="Volgende pagina">›</button>';
        el.innerHTML = html;
    }

    function setLot(id) { ui.lot = id; ui.page = 1; ui.editAct = 0; renderAll(); }

    $("act-lots").addEventListener("click", function (e) { var b = e.target.closest("[data-lot]"); if (b) setLot(parseInt(b.getAttribute("data-lot"), 10)); });
    $("act-lot-select").addEventListener("change", function () { setLot(parseInt(this.value, 10) || 0); });
    $("act-search").addEventListener("input", function () { ui.q = this.value; ui.page = 1; renderActs(); });
    $("act-search-clear").addEventListener("click", function () { $("act-search").value = ""; ui.q = ""; ui.page = 1; renderActs(); $("act-search").focus(); });
    $("act-empty-reset").addEventListener("click", function () { $("act-search").value = ""; ui.q = ""; ui.lot = 0; ui.page = 1; renderAll(); });
    $("act-pager").addEventListener("click", function (e) { var b = e.target.closest("[data-page]"); if (b && !b.disabled) { ui.page = parseInt(b.getAttribute("data-page"), 10); ui.editAct = 0; renderActs(); } });
    document.querySelectorAll(".gl-v2-act-sort").forEach(function (b) {
        b.addEventListener("click", function () {
            var k = b.getAttribute("data-sort");
            if (ui.sort === k) ui.dir = -ui.dir; else { ui.sort = k; ui.dir = 1; }
            ui.page = 1; renderActs();
        });
    });

    $("act-rows").addEventListener("click", function (e) {
        var ed = e.target.closest("[data-edit-act-btn]");
        if (ed) { ui.editAct = parseInt(ed.getAttribute("data-edit-act-btn"), 10); renderActs(); var f = $("act-rows").querySelector("form input[name=naam]"); if (f) { f.focus(); f.select(); } return; }
        if (e.target.closest("[data-cancel-act]")) { ui.editAct = 0; renderActs(); return; }
        var del = e.target.closest("[data-del-act]");
        if (del) verwijderActiviteit(parseInt(del.getAttribute("data-del-act"), 10));
    });
    $("act-rows").addEventListener("keydown", function (e) { if (e.key === "Escape" && ui.editAct) { ui.editAct = 0; renderActs(); } });
    $("act-rows").addEventListener("change", function (e) {
        if (e.target.name !== "groepId") return;
        var g = groupById(parseInt(e.target.value, 10)), p = $("edit-prefix"); if (g && p) p.textContent = g.lot + ".";
    });
    $("act-rows").addEventListener("submit", function (e) {
        e.preventDefault();
        var f = e.target, id = parseInt(f.getAttribute("data-edit-act"), 10);
        var volgnr = f.elements.volgnr.value.trim();
        if (volgnr && !/^\d+$/.test(volgnr)) { toast("warning", "Nummer niet geldig", "Gebruik cijfers, bv. 11."); f.elements.volgnr.focus(); return; }
        post(urls.actSave, { id: id, naam: f.elements.naam.value, groepId: f.elements.groepId.value, volgnr: volgnr })
            .then(function (r) { if (afhandelen(r, "Activiteit opgeslagen")) ui.editAct = 0; renderAll(); })
            .catch(netwerkFout);
    });

    function verwijderActiviteit(id) {
        var a = stand.activiteiten.filter(function (x) { return x.id === id; })[0]; if (!a) return;
        var vraag = window.GlV2Bevestig ? window.GlV2Bevestig({ title: "Activiteit verwijderen?", desc: nrOf(a) + " " + a.naam + " verdwijnt uit de lijst. Een activiteit die al in een budget, contract of factuur staat, kan niet verwijderd worden.", ok: "Verwijderen", tone: "danger" }) : Promise.resolve(confirm("Verwijderen?"));
        vraag.then(function (ok) {
            if (!ok) return;
            post(urls.actDelete, { id: id }).then(function (r) { afhandelen(r, "Activiteit verwijderd"); renderAll(); }).catch(netwerkFout);
        });
    }

    if (canWrite) {
        $("act-add-groep").addEventListener("change", updateAddNr);
        $("act-add").addEventListener("submit", function (e) {
            e.preventDefault();
            var naam = $("act-add-naam").value.trim(), gid = $("act-add-groep").value;
            if (!naam) { toast("warning", "Naam ontbreekt", "Geef de naam van de activiteit in."); $("act-add-naam").focus(); return; }
            post(urls.actSave, { id: 0, naam: naam, groepId: gid, volgnr: "" }).then(function (r) {
                if (afhandelen(r, "Activiteit toegevoegd")) { $("act-add-naam").value = ""; ui.sort = "naam"; }
                renderAll(); $("act-add-naam").focus();
            }).catch(netwerkFout);
        });
    }

    // ── Exporteren (csv) ──────────────────────────────────────────────────────────────────────────
    $("act-export").addEventListener("click", function () {
        var lines = ["Lot;Naam;Lotnummer;Activiteitgroep"];
        stand.activiteiten.slice().sort(function (a, b) { return nrSort(a) - nrSort(b); }).forEach(function (a) {
            var g = groupById(a.groepId);
            lines.push([g ? g.lot : "", '"' + String(a.naam).replace(/"/g, '""') + '"', nrOf(a), g ? '"' + String(g.naam).replace(/"/g, '""') + '"' : ""].join(";"));
        });
        var blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
        var a = document.createElement("a"); a.href = URL.createObjectURL(blob); a.download = "activiteiten.csv";
        document.body.appendChild(a); a.click(); setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
    });

    // ── Activiteitgroepen ─────────────────────────────────────────────────────────────────────────
    function nextLot() { var max = 0; stand.groepen.forEach(function (g) { if (g.lot > max) max = g.lot; }); return max + 1; }

    function renderGroups() {
        var html = "";
        stand.groepen.forEach(function (g) {
            if (canWrite && ui.editGrp === g.id) {
                html += '<form class="gl-v2-act-row gl-v2-act-grid--grp is-edit" data-edit-grp="' + g.id + '" autocomplete="off">' +
                    '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" inputmode="numeric" name="lot" class="gl-v2-field-input is-tabular" value="' + g.lot + '" maxlength="3" aria-label="Lot" /></div></div>' +
                    '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" name="naam" class="gl-v2-field-input" value="' + esc(g.naam) + '" maxlength="100" aria-label="Naam" /></div></div>' +
                    '<span class="gl-v2-act-hint">' + plural(g.aantal, "activiteit", "activiteiten") + "</span>" +
                    '<div class="gl-v2-act-actions"><button type="submit" class="gl-v2-row-action gl-v2-row-action-primary" title="Opslaan" aria-label="Opslaan"><i class="ph ph-check" aria-hidden="true"></i></button>' +
                    '<button type="button" class="gl-v2-row-action" data-cancel-grp title="Annuleren" aria-label="Annuleren"><i class="ph ph-x" aria-hidden="true"></i></button></div></form>';
            } else {
                var kanWeg = g.aantal === 0;
                html += '<div class="gl-v2-act-row gl-v2-act-grid--grp"><span class="gl-v2-act-nr is-lot">' + g.lot + "</span>" +
                    '<span class="gl-v2-act-name">' + esc(g.naam) + "</span>" +
                    '<span><button type="button" class="gl-v2-act-count" data-show-grp="' + g.id + '" title="Toon de activiteiten van deze groep">' + plural(g.aantal, "activiteit", "activiteiten") + "</button></span>" +
                    '<div class="gl-v2-act-actions">' + (canWrite ?
                        '<button type="button" class="gl-v2-row-action gl-v2-row-action-primary" data-edit-grp-btn="' + g.id + '" title="Bewerken" aria-label="Bewerk ' + esc(g.naam) + '"><i class="ph ph-pencil-simple" aria-hidden="true"></i></button>' +
                        '<button type="button" class="gl-v2-row-action gl-v2-row-action-danger" data-del-grp="' + g.id + '"' + (kanWeg ? "" : " disabled") + ' title="' + (kanWeg ? "Verwijderen" : "Verplaats eerst de activiteiten uit deze groep") + '" aria-label="Verwijder ' + esc(g.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i></button>' : "") + "</div></div>";
            }
        });
        $("grp-rows").innerHTML = html;
        $("grp-info").textContent = plural(stand.groepen.length, "groep", "groepen") + " · " + plural(stand.activiteiten.length, "activiteit", "activiteiten");
        $("grp-count-tab").textContent = stand.groepen.length;
        if ($("grp-add-lot")) { if (!$("grp-add-lot").value || $("grp-add-lot").dataset.auto === "1") { $("grp-add-lot").value = nextLot(); $("grp-add-lot").dataset.auto = "1"; } }
    }

    $("grp-rows").addEventListener("click", function (e) {
        var show = e.target.closest("[data-show-grp]");
        if (show) { ui.lot = parseInt(show.getAttribute("data-show-grp"), 10); ui.q = ""; $("act-search").value = ""; ui.page = 1; showTab("activiteiten"); renderAll(); return; }
        var ed = e.target.closest("[data-edit-grp-btn]");
        if (ed) { ui.editGrp = parseInt(ed.getAttribute("data-edit-grp-btn"), 10); renderGroups(); var f = $("grp-rows").querySelector("form input[name=naam]"); if (f) { f.focus(); f.select(); } return; }
        if (e.target.closest("[data-cancel-grp]")) { ui.editGrp = 0; renderGroups(); return; }
        var del = e.target.closest("[data-del-grp]");
        if (del && !del.disabled) verwijderGroep(parseInt(del.getAttribute("data-del-grp"), 10));
    });
    $("grp-rows").addEventListener("keydown", function (e) { if (e.key === "Escape" && ui.editGrp) { ui.editGrp = 0; renderGroups(); } });
    $("grp-rows").addEventListener("submit", function (e) {
        e.preventDefault();
        var f = e.target, id = parseInt(f.getAttribute("data-edit-grp"), 10);
        post(urls.grpSave, { id: id, naam: f.elements.naam.value, lot: f.elements.lot.value }).then(function (r) {
            if (afhandelen(r, "Activiteitgroep opgeslagen")) ui.editGrp = 0;
            renderAll();
        }).catch(netwerkFout);
    });

    function verwijderGroep(id) {
        var g = groupById(id); if (!g) return;
        var vraag = window.GlV2Bevestig ? window.GlV2Bevestig({ title: "Activiteitgroep verwijderen?", desc: "Lot " + g.lot + " · " + g.naam + " verdwijnt. De groep is leeg, dus er gaan geen activiteiten verloren.", ok: "Verwijderen", tone: "danger" }) : Promise.resolve(confirm("Verwijderen?"));
        vraag.then(function (ok) {
            if (!ok) return;
            post(urls.grpDelete, { id: id }).then(function (r) { if (ui.lot === id) ui.lot = 0; afhandelen(r, "Activiteitgroep verwijderd"); renderAll(); }).catch(netwerkFout);
        });
    }

    if (canWrite) {
        $("grp-add-lot").addEventListener("input", function () { this.dataset.auto = "0"; });
        $("grp-add").addEventListener("submit", function (e) {
            e.preventDefault();
            var naam = $("grp-add-naam").value.trim();
            if (!naam) { toast("warning", "Naam ontbreekt", "Geef de naam van de groep in."); $("grp-add-naam").focus(); return; }
            post(urls.grpSave, { id: 0, naam: naam, lot: $("grp-add-lot").value }).then(function (r) {
                if (afhandelen(r, "Activiteitgroep toegevoegd")) { $("grp-add-naam").value = ""; $("grp-add-lot").dataset.auto = "1"; $("grp-add-lot").value = ""; }
                renderAll();
            }).catch(netwerkFout);
        });
    }

    function renderAll() { renderLots(); renderActs(); renderGroups(); }

    showTab(ui.tab);
    renderAll();
})();
