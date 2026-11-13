/* gl-v2 — Projecten › Planning (design-handoff "CRM Planning" 59a–59c). Markup: Views/Projecten/Planning/IndexV2.cshtml.
   De pagina tekent alles uit één JSON-staat (FacadeCore.PlanningStaat). Elke wijziging gaat als commando naar
   ProjectPlanningController.Commando; het antwoord bevat de volledig herberekende staat (ServiceCore/Planning).
   Lijst: eigen rijen van 34px. Grafiek: het projectwijde component GlV2Gantt (js/gl-v2-gantt.js). */
(function () {
    "use strict";

    var cfgEl = document.getElementById("gl-v2-pl-config");
    var root = document.getElementById("gl-v2-pl-root");
    if (!cfgEl || !root) return;
    var cfg;
    try { cfg = JSON.parse(cfgEl.textContent); } catch (e) { return; }

    var $ = function (id) { return document.getElementById(id); };
    var canWrite = root.getAttribute("data-can-write") === "1";
    var canDelete = root.getAttribute("data-can-delete") === "1";
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {};

    var state = cfg.staat || { rijen: [], kop: {}, meldingen: [], gebruikers: [], lots: [], nietWerkdagen: [] };
    var shifted = {};          // rij-sleutels die door de laatste wijziging verschoven zijn
    var selected = null;
    var lastUndo = null;       // momentopname van vóór de laatste wijziging
    var undoIsRedo = false;
    var gantt = null;
    var firstRender = true;

    var UI_KEY = "gl-v2-pl-ui-" + cfg.projectId;
    var ui = { scale: "week", kritiek: false, basis: false, collapsed: {} };
    try { var raw = window.localStorage.getItem(UI_KEY); if (raw) { var p = JSON.parse(raw); ui.scale = p.scale || ui.scale; ui.kritiek = !!p.kritiek; ui.basis = !!p.basis; ui.collapsed = p.collapsed || {}; } } catch (e) { /* geen opslag: gewoon de standaard */ }
    function saveUi() { try { window.localStorage.setItem(UI_KEY, JSON.stringify(ui)); } catch (e) { /* idem */ } }

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[c]; }); }
    function fmt(iso) { if (!iso) return "—"; var p = iso.split("-"); return p[2] + "/" + p[1] + "/" + p[0].slice(2); }
    function fmtFull(iso) { if (!iso) return "—"; var p = iso.split("-"); return p[2] + "/" + p[1] + "/" + p[0]; }
    function todayIso() { var d = new Date(); return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2); }
    function dagen(n) { return n === 1 ? "1 werkdag" : n + " werkdagen"; }
    function rowByKey(k) { for (var i = 0; i < state.rijen.length; i++) if (state.rijen[i].sleutel === k) return state.rijen[i]; return null; }

    // ── Rijen die zichtbaar zijn (ingeklapte fases verbergen hun kinderen) ──
    function visibleRows() {
        var out = [], skip = null;
        state.rijen.forEach(function (r) {
            if (skip !== null) { if (r.niveau > skip) return; skip = null; }
            out.push(r);
            if (r.soort === "fase" && ui.collapsed[r.sleutel]) skip = r.niveau;
        });
        return out;
    }

    // ── Kop ──
    function renderKop() {
        var k = state.kop || {};
        var el = $("gl-v2-pl-kop");
        if (!state.heeftPlanning) { el.hidden = true; return; }
        el.hidden = false;
        var deadline = k.deadline
            ? "<dd class=\"" + (k.marge != null && k.marge < 0 ? "is-late" : "") + "\">" + fmtFull(k.deadline)
              + (k.marge != null ? "<small>" + (k.marge < 0 ? dagen(-k.marge) + " te laat" : dagen(k.marge) + " marge") + "</small>" : "") + "</dd>"
            : "<dd class=\"is-empty\">" + (k.geenStartdatum ? "geen aanvangsdatum" : "geen uitvoeringstermijn") + "</dd>";
        var basis = k.basisOp
            ? "<div><dt>Basisplanning</dt><dd>" + esc(k.basisOp) + (k.basisAfwijking ? "<small class=\"" + (k.basisAfwijking > 0 ? "is-late" : "") + "\">" + (k.basisAfwijking > 0 ? "+" : "−") + Math.abs(k.basisAfwijking) + " d</small>" : "") + "</dd></div>"
            : "";
        el.innerHTML =
            "<div class=\"gl-v2-pl-kop-title\"><h2>Bouwplanning</h2><span class=\"gl-v2-pl-chip\"><i class=\"ph " + (canWrite ? "ph-floppy-disk" : "ph-lock-simple") + "\" aria-hidden=\"true\"></i>" +
            (canWrite ? "Wijzigingen worden meteen bewaard" : "Alleen lezen") + "</span></div>" +
            "<dl class=\"gl-v2-pl-meta\">" +
            "<div><dt>Start</dt><dd>" + fmtFull(k.start) + "</dd></div>" +
            "<div><dt>Einde</dt><dd class=\"" + (k.marge != null && k.marge < 0 ? "is-late" : "") + "\">" + fmtFull(k.einde) + "</dd></div>" +
            "<div><dt>Duur</dt><dd>" + dagen(k.duurWerkdagen || 0) + "</dd></div>" +
            "<div><dt>Deadline oplevering</dt>" + deadline + "</div>" +
            "<div><dt>Verletdagen</dt><dd>" + (k.verletMeetellen === false ? "uitgezet" : (k.verletDagen || 0)) + (k.geenWeerstation ? "<small>geen weerstation</small>" : "") + "</dd></div>" +
            basis + "</dl>";
    }

    // ── Meldingen ──
    var ICON = { danger: "ph-x-circle", warning: "ph-warning", info: "ph-info" };
    function renderNotices() {
        var host = $("gl-v2-pl-notices");
        var html = "";
        var top = [], perRow = [];
        (state.meldingen || []).forEach(function (m) { (m.rijSleutel ? perRow : top).push(m); });
        top.forEach(function (m) {
            html += "<div class=\"gl-v2-notice gl-v2-notice-expanded is-" + esc(m.ernst) + "\" role=\"status\"><span class=\"gl-v2-notice-expanded-icon\"><i class=\"ph " + (ICON[m.ernst] || ICON.info) + "\" aria-hidden=\"true\"></i></span>" +
                "<div class=\"gl-v2-notice-body\"><span class=\"gl-v2-notice-title\">" + esc(m.titel) + "</span>" + (m.tekst ? "<span class=\"gl-v2-notice-text\">" + esc(m.tekst) + "</span>" : "") + "</div></div>";
        });
        if (perRow.length) {
            var ernst = perRow.some(function (m) { return m.ernst !== "info"; }) ? "warning" : "info";
            var lijst = perRow.slice(0, 6).map(function (m) {
                return "<li><b>" + esc(m.titel) + "</b><span>" + esc(m.tekst) + "</span><button type=\"button\" data-toon=\"" + esc(m.rijSleutel) + "\">Toon taak</button></li>";
            }).join("");
            if (perRow.length > 6) lijst += "<li>en nog " + (perRow.length - 6) + " andere</li>";
            html += "<div class=\"gl-v2-notice gl-v2-notice-expanded is-" + ernst + "\" role=\"status\"><span class=\"gl-v2-notice-expanded-icon\"><i class=\"ph " + ICON[ernst] + "\" aria-hidden=\"true\"></i></span>" +
                "<div class=\"gl-v2-notice-body\"><span class=\"gl-v2-notice-title\">" + (perRow.length === 1 ? "1 aandachtspunt" : perRow.length + " aandachtspunten") + " in de planning</span><ul class=\"gl-v2-pl-notice-list\">" + lijst + "</ul></div></div>";
        }
        host.innerHTML = html;
    }

    // ── Lijst ──
    function rowHtml(r) {
        var fase = r.soort === "fase";
        var cls = "gl-v2-pl-r" + (fase ? " is-fase" : "") + (r.afgerond ? " is-done" : "") + (shifted[r.sleutel] ? " is-shifted" : "") + (selected === r.sleutel ? " is-selected" : "");
        var naam = "";
        if (fase) naam += "<button type=\"button\" class=\"gl-v2-pl-caret\" data-act=\"caret\" aria-expanded=\"" + (ui.collapsed[r.sleutel] ? "false" : "true") + "\" aria-label=\"" + (ui.collapsed[r.sleutel] ? "Uitklappen" : "Inklappen") + "\"><i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>";
        else if (canWrite) naam += "<button type=\"button\" class=\"gl-v2-pl-tick" + (r.afgerond ? " is-on" : "") + "\" data-act=\"tick\" role=\"checkbox\" aria-checked=\"" + (r.afgerond ? "true" : "false") + "\" aria-label=\"Voltooid\"><i class=\"ph " + (r.afgerond ? "ph-check-circle ph-fill" : "ph-circle") + "\" aria-hidden=\"true\"></i></button>";
        else naam += "<span class=\"gl-v2-pl-spacer\"></span>";
        naam += "<span class=\"gl-v2-pl-name-text\" " + (canWrite ? "data-edit=\"naam\" title=\"Klik om de naam te wijzigen\"" : "") + ">" + esc(r.naam) + "</span>";
        if (ui.kritiek && r.kritiek) naam += "<span class=\"gl-v2-pl-badge\" title=\"Kritiek pad: " + (fase ? "bevat kritieke taken" : "geen speling") + "\">K</span>";
        if (shifted[r.sleutel]) naam += "<span class=\"gl-v2-pl-badge is-gold\">verschoven</span>";
        if (r.beperking === 1 && !fase) naam += "<i class=\"ph ph-push-pin\" title=\"Moet beginnen op " + fmtFull(r.beperkingDatum) + "\" aria-label=\"vastgepind\"></i>";
        if (fase && canWrite) naam += "<button type=\"button\" class=\"gl-v2-pl-add-in-fase\" data-act=\"add-in\">+ Taak</button>";

        var afw = ui.basis && r.afwijking ? " <small class=\"gl-v2-pl-afw " + (r.afwijking > 0 ? "is-plus" : "is-min") + "\">" + (r.afwijking > 0 ? "+" : "−") + Math.abs(r.afwijking) + "</small>" : "";
        var duurTxt = r.isMijlpaal ? "◆" : r.duur + " d";
        return "<div class=\"" + cls + "\" role=\"row\" data-key=\"" + esc(r.sleutel) + "\">" +
            "<span class=\"gl-v2-pl-c is-wbs\" role=\"cell\" data-act=\"open\">" + esc(r.wbs) + "</span>" +
            "<span class=\"gl-v2-pl-c\" role=\"cell\"><span class=\"gl-v2-pl-name\" style=\"--pl-level:" + r.niveau + "\">" + naam + "</span></span>" +
            "<span class=\"gl-v2-pl-c is-num x-sec x-tab" + (!fase && canWrite ? " is-edit" : " is-muted") + "\" role=\"cell\" " + (!fase && canWrite ? "data-edit=\"duur\"" : "") + ">" + duurTxt + "</span>" +
            "<span class=\"gl-v2-pl-c x-sec\" role=\"cell\" data-act=\"open\">" + fmt(r.start) + "</span>" +
            "<span class=\"gl-v2-pl-c x-sec\" role=\"cell\" data-act=\"open\">" + fmt(r.einde) + afw + "</span>" +
            "<span class=\"gl-v2-pl-c x-sec x-tab" + (!fase && canWrite ? " is-edit" : "") + "\" role=\"cell\" " + (!fase && canWrite ? "data-edit=\"voorgangers\" title=\"Klik om de voorgangers te wijzigen\"" : "") + ">" + (fase ? "" : (r.voorgangers ? esc(r.voorgangers) : "<span style=\"color:var(--gl-v2-muted-light)\">—</span>")) + "</span>" +
            "<span class=\"gl-v2-pl-c is-num x-sec x-tab is-muted\" role=\"cell\">" + (r.voortgang > 0 || r.afgerond ? r.voortgang + "%" : "") + "</span>" +
            "<span class=\"gl-v2-pl-c\" role=\"cell\">" + (canWrite ? "<button type=\"button\" class=\"gl-v2-pl-rowmenu js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-menu\" aria-label=\"Acties voor " + esc(r.naam) + "\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button>" : "") + "</span>" +
            "</div>";
    }

    function renderList(vis) {
        var head = "<div class=\"gl-v2-pl-lh\" role=\"row\"><span role=\"columnheader\">WBS</span><span role=\"columnheader\">Taak</span><span class=\"x-sec x-tab\" role=\"columnheader\" style=\"text-align:right\">Duur</span><span class=\"x-sec\" role=\"columnheader\">Start</span><span class=\"x-sec\" role=\"columnheader\">Einde</span><span class=\"x-sec x-tab\" role=\"columnheader\">Voorgangers</span><span class=\"x-sec x-tab\" role=\"columnheader\" style=\"text-align:right\">%</span><span></span></div>";
        $("gl-v2-pl-list").innerHTML = head + vis.map(rowHtml).join("");
    }

    // ── Gantt ──
    function ariaFor(r) {
        var s = r.wbs + " " + r.naam + ", " + (r.soort === "fase" ? "fase" : r.isMijlpaal ? "mijlpaal" : "taak") + ", van " + fmtFull(r.start) + " tot " + fmtFull(r.einde);
        if (r.soort === "taak" && !r.isMijlpaal) s += ", " + r.duur + " werkdagen";
        if (r.voortgang) s += ", " + r.voortgang + " procent klaar";
        if (r.kritiek && ui.kritiek) s += ", kritiek pad";
        if (shifted[r.sleutel]) s += ", verschoven";
        return s;
    }

    function renderGantt(vis) {
        var scroller = $("gl-v2-pl-scroll");
        var left = scroller.scrollLeft;
        var rows = vis.map(function (r) {
            return {
                key: r.sleutel, type: r.soort === "fase" ? "phase" : (r.isMijlpaal ? "milestone" : "task"),
                start: r.start, end: r.einde, progress: r.voortgang, critical: r.kritiek, done: r.afgerond,
                pinned: r.soort === "taak" && r.beperking === 1, shifted: !!shifted[r.sleutel], label: r.naam,
                baseStart: r.basisStart, baseEnd: r.basisEinde, aria: ariaFor(r)
            };
        });
        var links = [];
        vis.forEach(function (r) {
            (r.voorgangerLinks || []).forEach(function (l) {
                var van = rowByKey("t" + l.taakId);
                links.push({ from: "t" + l.taakId, to: r.sleutel, type: l.type, critical: !!(r.kritiek && van && van.kritiek) });
            });
        });
        gantt = window.GlV2Gantt.render($("gl-v2-pl-gantt"), {
            rows: rows, links: links, nonWorking: state.nietWerkdagen || [], today: todayIso(), deadline: (state.kop || {}).deadline || null,
            scale: ui.scale, showCritical: ui.kritiek, showBase: ui.basis, rowHeight: 34
        }, {
            onOpen: function (key) { selected = key; markSelected(); if (canWrite) openEdit(key); }
        });
        if (firstRender && gantt) { gantt.scrollToToday(scroller); firstRender = false; } else { scroller.scrollLeft = left; }
    }

    function markSelected() {
        document.querySelectorAll("#gl-v2-pl-list .gl-v2-pl-r").forEach(function (el) { el.classList.toggle("is-selected", el.getAttribute("data-key") === selected); });
    }

    // ── Alles tekenen ──
    function render() {
        var heeft = !!state.heeftPlanning && state.rijen.length > 0;
        $("gl-v2-pl-empty").hidden = heeft;
        $("gl-v2-pl-card").hidden = !heeft;
        renderKop();
        renderNotices();
        if (!heeft) return;
        var hasBasis = state.rijen.some(function (r) { return r.basisStart; });
        $("gl-v2-pl-toggle-basis").hidden = !hasBasis;
        if (!hasBasis) ui.basis = false;
        setPressed("gl-v2-pl-toggle-kritiek", ui.kritiek);
        setPressed("gl-v2-pl-toggle-basis", ui.basis);
        setPressed("gl-v2-pl-toggle-verlet", (state.kop || {}).verletMeetellen !== false);
        document.querySelectorAll("#gl-v2-pl-zoom button").forEach(function (b) {
            var on = b.getAttribute("data-scale") === ui.scale;
            b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false");
        });
        var anyOpen = state.rijen.some(function (r) { return r.soort === "fase" && !ui.collapsed[r.sleutel]; });
        $("gl-v2-pl-collapse").querySelector("span").textContent = anyOpen ? "Fases inklappen" : "Alles uitklappen";
        var vis = visibleRows();
        renderList(vis);
        renderGantt(vis);
    }
    function setPressed(id, on) { var b = $(id); b.setAttribute("aria-pressed", on ? "true" : "false"); }

    // ── Wijzigingsbalk ──
    function showChangeBar(text, undoLabel) {
        var bar = $("gl-v2-pl-changebar");
        $("gl-v2-pl-changebar-text").textContent = text;
        $("gl-v2-pl-undo").textContent = undoLabel || "Ongedaan maken";
        $("gl-v2-pl-undo").hidden = !lastUndo;
        bar.hidden = false;
    }
    $("gl-v2-pl-changebar-close").addEventListener("click", function () { $("gl-v2-pl-changebar").hidden = true; });
    $("gl-v2-pl-undo").addEventListener("click", function () {
        if (!lastUndo) return;
        var snap = lastUndo, wasRedo = undoIsRedo;
        send({ type: "herstel", momentopname: snap }, { silent: true }).then(function (ok) {
            if (!ok) return;
            undoIsRedo = !wasRedo;
            showChangeBar(wasRedo ? "Opnieuw uitgevoerd." : "Ongedaan gemaakt.", wasRedo ? "Ongedaan maken" : "Opnieuw");
        });
    });

    // ── Commando's ──
    function token() { var t = document.querySelector("input[name=\"__RequestVerificationToken\"]"); return t ? t.value : ""; }

    function send(cmd, opts) {
        opts = opts || {};
        cmd.versie = state.versie || 0;
        return fetch(cfg.commandoUrl, {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(cmd)
        }).then(function (r) {
            if (r.status === 403) throw new Error("Je hebt hier geen recht toe.");
            if (!r.ok) throw new Error("Opslaan mislukt. Probeer het zo opnieuw.");
            return r.json();
        }).then(function (res) {
            if (res.conflict) {
                state = res.staat; shifted = {}; render();
                toast("warning", "Planning herladen", res.fout || "Iemand anders wijzigde de planning intussen.");
                return false;
            }
            if (!res.ok) { if (opts.onError) opts.onError(res.fout); else toast("danger", "Dat kan niet", res.fout || "De wijziging is niet bewaard."); return false; }
            var vorige = {};
            state.rijen.forEach(function (r) { vorige[r.sleutel] = true; });
            res._nieuw = res.staat.rijen.filter(function (r) { return !vorige[r.sleutel]; });
            state = res.staat;
            shifted = {};
            (res.verschoven || []).forEach(function (k) { shifted[k] = true; });
            if (cmd.type !== "herstel") { lastUndo = res.undo; undoIsRedo = false; }
            else lastUndo = res.undo;
            render();
            if (!opts.silent) {
                if (res.samenvatting) showChangeBar(res.samenvatting);
                else if (lastUndo) showChangeBar("Wijziging bewaard.");
            }
            if (opts.after) opts.after(res);
            return true;
        }).catch(function (e) {
            if (opts.onError) opts.onError(e.message); else toast("danger", "Opslaan mislukt", e.message || "Controleer je verbinding en probeer opnieuw.");
            return false;
        });
    }

    // ── Inline bewerken (naam, duur, voorgangers) ──
    function startEdit(cell, key, field) {
        var r = rowByKey(key); if (!r || cell.querySelector("input")) return;
        var oud = field === "duur" ? String(r.duur) : field === "voorgangers" ? (r.voorgangers || "") : r.naam;
        var input = document.createElement("input");
        input.type = field === "duur" ? "number" : "text";
        if (field === "duur") { input.min = "0"; input.max = "2000"; input.inputMode = "numeric"; }
        input.className = "gl-v2-pl-inline"; input.value = oud; input.setAttribute("aria-label", field === "duur" ? "Duur in werkdagen" : field === "voorgangers" ? "Voorgangers" : "Naam");
        var original = cell.innerHTML;
        cell.innerHTML = ""; cell.appendChild(input); input.focus(); input.select();
        var done = false;
        function stop(save) {
            if (done) return; done = true;
            var nieuw = input.value.trim();
            if (!save || nieuw === oud.trim()) { cell.innerHTML = original; return; }
            if (field === "naam" && !nieuw) { cell.innerHTML = original; return; }
            var velden = {}; velden[field] = field === "duur" ? parseInt(nieuw, 10) : nieuw;
            if (field === "duur" && isNaN(velden.duur)) { cell.innerHTML = original; return; }
            send({ type: "wijzig", soort: r.soort, id: r.id, velden: velden }, { onError: function (f) { cell.innerHTML = original; toast("danger", "Dat kan niet", f); } });
        }
        input.addEventListener("keydown", function (e) {
            if (e.key === "Enter") { e.preventDefault(); stop(true); }
            else if (e.key === "Escape") { e.preventDefault(); stop(false); }
        });
        input.addEventListener("blur", function () { stop(true); });
    }

    // ── Lijst-interactie ──
    $("gl-v2-pl-list").addEventListener("click", function (e) {
        var rowEl = e.target.closest(".gl-v2-pl-r"); if (!rowEl) return;
        var key = rowEl.getAttribute("data-key"), r = rowByKey(key); if (!r) return;
        if (e.target.closest(".gl-v2-pl-rowmenu")) return;
        var caret = e.target.closest("[data-act=caret]");
        if (caret) { ui.collapsed[key] = !ui.collapsed[key]; saveUi(); render(); return; }
        var tick = e.target.closest("[data-act=tick]");
        if (tick && canWrite) { send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: r.afgerond ? 0 : 100 } }); return; }
        var add = e.target.closest("[data-act=add-in]");
        if (add && canWrite) { openNieuwTaak({ sectieId: r.id }); return; }
        var ed = e.target.closest("[data-edit]");
        if (ed && canWrite && !(r.soort === "fase" && ed.getAttribute("data-edit") !== "naam")) { selected = key; markSelected(); startEdit(ed, key, ed.getAttribute("data-edit")); return; }
        selected = key; markSelected();
        if (canWrite) openEdit(key);
    });

    // ── Rijmenu (⋯) ──
    var menuKey = null;
    document.addEventListener("pointerdown", function (e) {
        var trig = e.target.closest(".gl-v2-pl-rowmenu"); if (!trig) return;
        var rowEl = trig.closest(".gl-v2-pl-r"); if (!rowEl) return;
        menuKey = rowEl.getAttribute("data-key");
        buildMenu(rowByKey(menuKey));
    }, true);
    document.addEventListener("keydown", function (e) {
        if ((e.key === "Enter" || e.key === " ") && e.target.closest && e.target.closest(".gl-v2-pl-rowmenu")) {
            var rowEl = e.target.closest(".gl-v2-pl-r"); if (rowEl) { menuKey = rowEl.getAttribute("data-key"); buildMenu(rowByKey(menuKey)); }
        }
    }, true);

    function menuItem(act, icon, label, danger) {
        return "<button type=\"button\" class=\"gl-v2-menu-item" + (danger ? " is-danger" : "") + "\" role=\"menuitem\" data-menu=\"" + act + "\"><i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span></span></button>";
    }
    function buildMenu(r) {
        if (!r) return;
        var fase = r.soort === "fase", h = "";
        h += menuItem("edit", "ph-pencil-simple", "Bewerken…");
        h += menuItem(fase ? "add-in" : "add-after", "ph-plus", fase ? "Taak toevoegen in de fase" : "Taak eronder toevoegen");
        if (fase) h += menuItem("add-fase", "ph-stack-plus", "Fase eronder toevoegen");
        if (!fase) h += menuItem("tick", r.afgerond ? "ph-circle" : "ph-check-circle", r.afgerond ? "Niet meer voltooid" : "Markeren als voltooid");
        h += menuItem("omhoog", "ph-arrow-up", "Omhoog");
        h += menuItem("omlaag", "ph-arrow-down", "Omlaag");
        h += menuItem("inspringen", "ph-arrow-line-right", "Inspringen");
        h += menuItem("uitspringen", "ph-arrow-line-left", "Uitspringen");
        if (canDelete) h += menuItem("verwijder", "ph-trash", "Verwijderen…", true);
        $("gl-v2-pl-menu").innerHTML = h;
    }
    $("gl-v2-pl-menu").addEventListener("click", function (e) {
        var b = e.target.closest("[data-menu]"); if (!b) return;
        var r = rowByKey(menuKey); if (!r) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        var act = b.getAttribute("data-menu");
        if (act === "edit") openEdit(r.sleutel);
        else if (act === "add-in") openNieuwTaak({ sectieId: r.id });
        else if (act === "add-after") openNieuwTaak({ sectieId: r.ouderFaseId, naId: r.id, naSoort: "taak" });
        else if (act === "add-fase") openNieuwFase({ faseplaats: "na:" + r.id });
        else if (act === "tick") send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: r.afgerond ? 0 : 100 } });
        else if (act === "omhoog" || act === "omlaag" || act === "inspringen" || act === "uitspringen") send({ type: "verplaats", soort: r.soort, id: r.id, actie: act });
        else if (act === "verwijder") vraagVerwijder(r);
    });

    function vraagVerwijder(r) {
        var fase = r.soort === "fase";
        var kinderen = 0;
        if (fase) { var i = state.rijen.indexOf(r); for (var j = i + 1; j < state.rijen.length && state.rijen[j].niveau > r.niveau; j++) if (state.rijen[j].soort === "taak") kinderen++; }
        var gekoppeld = (r.mijlpalen || []).length;
        var desc = fase ? ("De fase " + r.wbs + " " + r.naam + (kinderen ? " en haar " + kinderen + (kinderen === 1 ? " taak" : " taken") : "") + " worden verwijderd. Je kan dit ongedaan maken.")
                        : ("De taak " + r.wbs + " " + r.naam + " wordt verwijderd. Taken die erop wachten verliezen deze voorganger. Je kan dit ongedaan maken.");
        if (gekoppeld) desc += " Een gekoppelde mijlpaal in het traject verliest zijn datum uit de planning.";
        var run = function () { send({ type: "verwijder", items: [{ soort: r.soort, id: r.id }] }); closePanel(); };
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: (fase ? "Fase" : "Taak") + " verwijderen?", desc: desc, ok: "Verwijderen", tone: "danger", icon: "ph-trash" }).then(function (ok) { if (ok) run(); });
        else if (window.confirm(desc)) run();
    }

    // ── Werkbalk ──
    $("gl-v2-pl-zoom").addEventListener("click", function (e) {
        var b = e.target.closest("button[data-scale]"); if (!b) return;
        ui.scale = b.getAttribute("data-scale"); saveUi(); render();
    });
    $("gl-v2-pl-toggle-kritiek").addEventListener("click", function () { ui.kritiek = !ui.kritiek; saveUi(); render(); });
    $("gl-v2-pl-toggle-basis").addEventListener("click", function () { ui.basis = !ui.basis; saveUi(); render(); });
    $("gl-v2-pl-toggle-verlet").addEventListener("click", function () {
        if (!canWrite) return;
        send({ type: "verletMeetellen", velden: { aan: (state.kop || {}).verletMeetellen === false } });
    });
    $("gl-v2-pl-today").addEventListener("click", function () { if (gantt) gantt.scrollToToday($("gl-v2-pl-scroll")); });
    $("gl-v2-pl-collapse").addEventListener("click", function () {
        var anyOpen = state.rijen.some(function (r) { return r.soort === "fase" && !ui.collapsed[r.sleutel]; });
        state.rijen.forEach(function (r) { if (r.soort === "fase") ui.collapsed[r.sleutel] = anyOpen; });
        saveUi(); render();
    });
    var basisBtn = $("gl-v2-pl-basis-set");
    if (basisBtn) basisBtn.addEventListener("click", function () {
        var heeft = state.rijen.some(function (r) { return r.basisStart; });
        var run = function () { send({ type: "basisVastleggen" }).then(function (ok) { if (ok) { ui.basis = true; saveUi(); render(); toast("success", "Basisplanning vastgelegd", "Afwijkingen worden vanaf nu hiertegen gemeten."); } }); };
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: "Basisplanning vastleggen?", desc: heeft ? "De bestaande basisplanning wordt overschreven met de huidige planning." : "De huidige planning wordt de basis waartegen afwijkingen gemeten worden.", ok: "Vastleggen", tone: "warning", icon: "ph-bookmark-simple" }).then(function (ok) { if (ok) run(); });
        else run();
    });
    $("gl-v2-pl-notices").addEventListener("click", function (e) {
        var b = e.target.closest("[data-toon]"); if (!b) return;
        var key = b.getAttribute("data-toon"), r = rowByKey(key); if (!r) return;
        // een ingeklapte fase openen
        state.rijen.forEach(function (x) { if (x.soort === "fase" && ui.collapsed[x.sleutel] && r.wbs.indexOf(x.wbs + ".") === 0) ui.collapsed[x.sleutel] = false; });
        selected = key; render();
        var el = document.querySelector("#gl-v2-pl-list [data-key=\"" + key + "\"]");
        if (el && el.scrollIntoView) el.scrollIntoView({ block: "center", behavior: "smooth" });
    });

    // ══════════ Zijpaneel ══════════
    var panel = null; // { mode, key, id, sectieId, naId, naSoort }
    var BEPERKINGEN = [
        { value: "0", text: "Zo vroeg mogelijk" }, { value: "1", text: "Moet beginnen op" }, { value: "2", text: "Niet eerder beginnen dan" },
        { value: "3", text: "Niet later beginnen dan" }, { value: "4", text: "Niet eerder eindigen dan" }, { value: "5", text: "Niet later eindigen dan" }, { value: "6", text: "Moet eindigen op" }
    ];

    function wrapOf(id) { return $(id + "_select"); }
    function setSelect(id, items, value, emptyText) { if (window.GlV2Select) window.GlV2Select.setItems(wrapOf(id), items, value, emptyText); }
    function getSelect(id) { var el = $(id); return el ? el.value : ""; }
    function fieldOf(id) { var el = $(id + "_picker") || wrapOf(id) || $(id); return el ? el.closest(".gl-v2-field") : null; }
    function showField(id, on) { var f = fieldOf(id); if (f) f.hidden = !on; }
    function setDate(id, iso) { if (window.GlV2DatePicker) window.GlV2DatePicker.setValue(id, iso || ""); }
    function getDate(id) { var el = $(id); return el && el.value ? el.value : null; }
    function indent(r) { return new Array(r.niveau + 1).join("– ") + r.wbs + " " + r.naam; }

    function faseItems() { return state.rijen.filter(function (r) { return r.soort === "fase"; }).map(function (r) { return { value: String(r.id), text: indent(r) }; }); }
    function kinderenVan(faseId) { return state.rijen.filter(function (r) { return r.ouderFaseId === faseId; }); }

    function plaatsItems(faseId) {
        var items = [{ value: "", text: "Achteraan in de fase" }];
        kinderenVan(faseId).forEach(function (r) { items.push({ value: r.soort === "fase" ? "f" + r.id : "t" + r.id, text: "Na " + r.wbs + " " + r.naam }); });
        return items;
    }

    function showPanel(kicker, title, sub) {
        $("gl-v2-pl-panel-kicker").textContent = kicker; $("gl-v2-pl-panel-title").textContent = title; $("gl-v2-pl-panel-sub").textContent = sub || "";
        $("gl-v2-pl-form-error").hidden = true;
        $("gl-v2-pl-backdrop").hidden = false; var p = $("gl-v2-pl-panel"); p.hidden = false; p.setAttribute("aria-hidden", "false");
        window.setTimeout(function () { $("gl-v2-pl-f-naam").focus(); }, 30);
    }
    function closePanel() {
        panel = null;
        $("gl-v2-pl-backdrop").hidden = true; var p = $("gl-v2-pl-panel"); p.hidden = true; p.setAttribute("aria-hidden", "true");
    }
    function formError(msg) { var el = $("gl-v2-pl-form-error"); el.querySelector("span").textContent = msg; el.hidden = false; el.scrollIntoView({ block: "nearest" }); }

    function taakVorm(on) {
        document.querySelector(".gl-v2-pl-only-taak").hidden = !on;
        document.querySelector(".gl-v2-pl-only-fase").hidden = on;
    }

    function updateBeperkingVeld() { showField("gl-v2-pl-f-bepdatum", getSelect("gl-v2-pl-f-bep") !== "0" && getSelect("gl-v2-pl-f-bep") !== ""); }
    function standaardVoorganger(faseId, naId, naSoort) {
        // Standaard: de taak erboven (de gekozen "na"-taak, anders de laatste taak in de fase)
        var kinderen = kinderenVan(faseId).filter(function (r) { return r.soort === "taak"; });
        if (naId && naSoort === "taak") { var r0 = rowByKey("t" + naId); return r0 ? r0.wbs : ""; }
        return kinderen.length ? kinderen[kinderen.length - 1].wbs : "";
    }

    function openNieuwTaak(ctx) {
        if (!canWrite) return;
        ctx = ctx || {};
        var fases = faseItems();
        var sectieId = ctx.sectieId != null ? ctx.sectieId : (fases.length ? parseInt(fases[fases.length - 1].value, 10) : null);
        panel = { mode: "taak-nieuw", sectieId: sectieId, naId: ctx.naId || null, naSoort: ctx.naSoort || null, voorDirty: false };
        taakVorm(true);
        $("gl-v2-pl-info").hidden = true;
        $("gl-v2-pl-f-naam").value = ""; $("gl-v2-pl-f-duur").value = "1"; $("gl-v2-pl-f-notitie").value = "";
        setSelect("gl-v2-pl-f-fase", fases, sectieId != null ? String(sectieId) : "");
        showField("gl-v2-pl-f-fase", fases.length > 0);
        var plaatsWaarde = ctx.naId ? (ctx.naSoort === "fase" ? "f" : "t") + ctx.naId : "";
        $("gl-v2-pl-wrap-plaats").hidden = sectieId == null;
        if (sectieId != null) setSelect("gl-v2-pl-f-plaats", plaatsItems(sectieId), plaatsWaarde);
        $("gl-v2-pl-f-voor").value = sectieId != null ? standaardVoorganger(sectieId, ctx.naId, ctx.naSoort) : "";
        setSelect("gl-v2-pl-f-bep", BEPERKINGEN, "0"); setDate("gl-v2-pl-f-bepdatum", ""); updateBeperkingVeld();
        setDate("gl-v2-pl-f-deadline", "");
        $("gl-v2-pl-wrap-werkelijk").hidden = true; showField("gl-v2-pl-f-pct", false);
        setSelect("gl-v2-pl-f-wie", (state.gebruikers || []).map(function (g) { return { value: g.id, text: g.naam }; }), "", "Niemand");
        $("gl-v2-pl-panel-delete") && ($("gl-v2-pl-panel-delete").hidden = true);
        $("gl-v2-pl-panel-again").hidden = false;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = "Toevoegen";
        showPanel("TAAK", "Nieuwe taak", "Een duur van 0 maakt er een mijlpaal van");
    }

    function openNieuwFase(ctx) {
        if (!canWrite) return;
        ctx = ctx || {};
        panel = { mode: "fase-nieuw" };
        taakVorm(false);
        $("gl-v2-pl-info").hidden = true;
        $("gl-v2-pl-f-naam").value = "";
        var items = [{ value: "einde", text: "Als hoofdfase, achteraan" }];
        state.rijen.filter(function (r) { return r.soort === "fase"; }).forEach(function (r) {
            items.push({ value: "na:" + r.id, text: "Na " + r.wbs + " " + r.naam });
        });
        state.rijen.filter(function (r) { return r.soort === "fase"; }).forEach(function (r) {
            items.push({ value: "in:" + r.id, text: "Deelfase in " + r.wbs + " " + r.naam });
        });
        setSelect("gl-v2-pl-f-faseplaats", items, ctx.faseplaats || "einde");
        $("gl-v2-pl-wrap-faseplaats").hidden = false;
        $("gl-v2-pl-wrap-eerste").hidden = false;
        $("gl-v2-pl-f-eerste").value = ""; $("gl-v2-pl-f-eerste-duur").value = "1"; $("gl-v2-pl-f-startna").value = "";
        setSelect("gl-v2-pl-f-lot", (state.lots || []).map(function (g) { return { value: g.id, text: g.naam }; }), "", "Geen lot");
        $("gl-v2-pl-f-teltmee").checked = false; $("gl-v2-pl-f-teltmee").disabled = true;
        $("gl-v2-pl-panel-delete") && ($("gl-v2-pl-panel-delete").hidden = true);
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = "Fase toevoegen";
        showPanel("FASE", "Nieuwe fase", "Zoals een samenvattingstaak in MS Project");
    }

    function openEdit(key) {
        var r = rowByKey(key); if (!r || !canWrite) return;
        if (r.soort === "fase") return openEditFase(r);
        panel = { mode: "taak-bewerken", key: key, id: r.id };
        taakVorm(true);
        $("gl-v2-pl-f-naam").value = r.naam; $("gl-v2-pl-f-duur").value = r.duur; $("gl-v2-pl-f-notitie").value = r.notitie || "";
        showField("gl-v2-pl-f-fase", false);
        $("gl-v2-pl-wrap-plaats").hidden = true;
        $("gl-v2-pl-f-voor").value = r.voorgangers || "";
        setSelect("gl-v2-pl-f-bep", BEPERKINGEN, String(r.beperking || 0)); setDate("gl-v2-pl-f-bepdatum", r.beperkingDatum); updateBeperkingVeld();
        setDate("gl-v2-pl-f-deadline", r.deadline);
        $("gl-v2-pl-wrap-werkelijk").hidden = false; setDate("gl-v2-pl-f-wstart", r.werkelijkStart); setDate("gl-v2-pl-f-weinde", r.werkelijkEinde);
        showField("gl-v2-pl-f-pct", true); $("gl-v2-pl-f-pct").value = r.voortgang;
        setSelect("gl-v2-pl-f-wie", (state.gebruikers || []).map(function (g) { return { value: g.id, text: g.naam }; }), r.verantwoordelijkeId || "", "Niemand");

        var info = "<span><b>" + esc(r.wbs) + "</b> · " + fmtFull(r.start) + " tot " + fmtFull(r.einde) + (r.isMijlpaal ? "" : " · " + dagen(r.duur)) + "</span>";
        if (r.speling != null) info += "<span>" + (r.kritiek ? "<span class=\"is-crit\">Kritiek pad</span>, geen speling" : "Speling: " + dagen(r.speling)) + "</span>";
        if (r.bepalendeVoorganger) info += "<span>Start bepaald door taak " + esc(r.bepalendeVoorganger) + "</span>";
        if (r.basisEinde) info += "<span>Basisplanning: " + fmtFull(r.basisStart) + " tot " + fmtFull(r.basisEinde) + (r.afwijking ? " (" + (r.afwijking > 0 ? "+" : "−") + Math.abs(r.afwijking) + " d)" : "") + "</span>";
        (r.mijlpalen || []).forEach(function (m) { info += "<span>Bepaalt de streefdatum van mijlpaal <b>" + esc(m.naam) + "</b> in het traject</span>"; });
        (r.waarschuwingen || []).forEach(function (w) { info += "<span class=\"is-crit\">" + esc(w) + "</span>"; });
        var infoEl = $("gl-v2-pl-info"); infoEl.innerHTML = info; infoEl.hidden = false;

        var del = $("gl-v2-pl-panel-delete"); if (del) del.hidden = !canDelete;
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = "Opslaan";
        showPanel(r.isMijlpaal ? "MIJLPAAL" : "TAAK", r.naam, r.wbs);
    }

    function openEditFase(r) {
        panel = { mode: "fase-bewerken", key: r.sleutel, id: r.id };
        taakVorm(false);
        $("gl-v2-pl-f-naam").value = r.naam;
        $("gl-v2-pl-wrap-faseplaats").hidden = true; $("gl-v2-pl-wrap-eerste").hidden = true;
        setSelect("gl-v2-pl-f-lot", (state.lots || []).map(function (g) { return { value: g.id, text: g.naam }; }), r.lotId != null ? String(r.lotId) : "", "Geen lot");
        $("gl-v2-pl-f-teltmee").checked = !!r.teltMee; $("gl-v2-pl-f-teltmee").disabled = r.lotId == null;
        var info = "<span><b>" + esc(r.wbs) + "</b> · " + fmtFull(r.start) + " tot " + fmtFull(r.einde) + " · " + dagen(r.duur) + " · " + r.voortgang + "% klaar</span>";
        (r.mijlpalen || []).forEach(function (m) { info += "<span>Bepaalt de streefdatum van mijlpaal <b>" + esc(m.naam) + "</b> in het traject</span>"; });
        var infoEl = $("gl-v2-pl-info"); infoEl.innerHTML = info; infoEl.hidden = false;
        var del = $("gl-v2-pl-panel-delete"); if (del) del.hidden = !canDelete;
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = "Opslaan";
        showPanel("FASE", r.naam, r.wbs);
    }

    // Afhankelijke velden in het paneel
    $("gl-v2-pl-f-fase").addEventListener("change", function () {
        if (!panel || panel.mode !== "taak-nieuw") return;
        var id = parseInt(this.value, 10); if (isNaN(id)) return;
        panel.sectieId = id; panel.naId = null; panel.naSoort = null;
        $("gl-v2-pl-wrap-plaats").hidden = false;
        setSelect("gl-v2-pl-f-plaats", plaatsItems(id), "");
        if (!panel.voorDirty) $("gl-v2-pl-f-voor").value = standaardVoorganger(id, null, null);
    });
    $("gl-v2-pl-f-plaats").addEventListener("change", function () {
        if (!panel || panel.mode !== "taak-nieuw") return;
        var v = this.value; panel.naId = v ? parseInt(v.slice(1), 10) : null; panel.naSoort = v ? (v[0] === "f" ? "fase" : "taak") : null;
        if (!panel.voorDirty) $("gl-v2-pl-f-voor").value = standaardVoorganger(panel.sectieId, panel.naId, panel.naSoort);
    });
    $("gl-v2-pl-f-voor").addEventListener("input", function () { if (panel) panel.voorDirty = true; });
    $("gl-v2-pl-f-bep").addEventListener("change", updateBeperkingVeld);
    $("gl-v2-pl-f-lot").addEventListener("change", function () {
        var heeft = !!this.value; var cb = $("gl-v2-pl-f-teltmee"); cb.disabled = !heeft; if (!heeft) cb.checked = false;
    });

    function verzamelTaakVelden(nieuw) {
        var v = {};
        v.naam = $("gl-v2-pl-f-naam").value.trim();
        v.duur = parseInt($("gl-v2-pl-f-duur").value, 10);
        v.voorgangers = $("gl-v2-pl-f-voor").value.trim();
        var bep = parseInt(getSelect("gl-v2-pl-f-bep") || "0", 10);
        v.beperking = bep; v.beperkingDatum = bep ? getDate("gl-v2-pl-f-bepdatum") : null;
        v.deadline = getDate("gl-v2-pl-f-deadline");
        v.verantwoordelijke = getSelect("gl-v2-pl-f-wie") || null;
        v.notitie = $("gl-v2-pl-f-notitie").value.trim() || null;
        if (!nieuw) {
            v.werkelijkStart = getDate("gl-v2-pl-f-wstart");
            var we = getDate("gl-v2-pl-f-weinde");
            var pct = parseInt($("gl-v2-pl-f-pct").value, 10);
            var r = rowByKey(panel.key);
            // Werkelijk einde en voortgang hangen samen: het ingevulde einde wint, anders de voortgang
            if (we !== (r.werkelijkEinde || null)) v.werkelijkEinde = we;
            else if (!isNaN(pct) && pct !== r.voortgang) v.voortgang = pct;
        }
        return v;
    }

    function opslaan(again) {
        if (!panel) return;
        $("gl-v2-pl-form-error").hidden = true;
        var naam = $("gl-v2-pl-f-naam").value.trim();
        if (!naam) { formError("Geef een naam in."); $("gl-v2-pl-f-naam").focus(); return; }
        var onError = function (f) { formError(f || "De wijziging is niet bewaard."); };

        if (panel.mode === "taak-nieuw") {
            var v = verzamelTaakVelden(true);
            if (isNaN(v.duur) || v.duur < 0) { formError("De duur moet een aantal werkdagen zijn (0 of meer)."); return; }
            if (v.beperking && !v.beperkingDatum) { formError("Kies een datum bij de beperking."); return; }
            send({ type: "taakToevoegen", sectieId: panel.sectieId, naId: panel.naId, naSoort: panel.naSoort, velden: v }, {
                onError: onError,
                after: function (res) {
                    var nieuw = (res._nieuw || []).filter(function (r) { return r.soort === "taak"; })[0];
                    if (again && nieuw) {
                        panel.naId = nieuw.id; panel.naSoort = "taak"; panel.voorDirty = false;
                        setSelect("gl-v2-pl-f-plaats", plaatsItems(panel.sectieId), "t" + nieuw.id);
                        $("gl-v2-pl-f-naam").value = ""; $("gl-v2-pl-f-voor").value = nieuw.wbs; $("gl-v2-pl-f-duur").value = "1"; $("gl-v2-pl-f-notitie").value = "";
                        $("gl-v2-pl-f-naam").focus();
                        toast("success", "Taak toegevoegd", nieuw.wbs + " " + nieuw.naam);
                    } else closePanel();
                }
            });
        } else if (panel.mode === "taak-bewerken") {
            var w = verzamelTaakVelden(false);
            if (isNaN(w.duur) || w.duur < 0) { formError("De duur moet een aantal werkdagen zijn (0 of meer)."); return; }
            if (w.beperking && !w.beperkingDatum) { formError("Kies een datum bij de beperking."); return; }
            send({ type: "wijzig", soort: "taak", id: panel.id, velden: w }, { onError: onError, after: closePanel });
        } else if (panel.mode === "fase-nieuw") {
            var plaats = getSelect("gl-v2-pl-f-faseplaats") || "einde", cmd = { type: "faseToevoegen", velden: { naam: naam } };
            if (plaats.indexOf("in:") === 0) cmd.sectieId = parseInt(plaats.slice(3), 10);
            else if (plaats.indexOf("na:") === 0) { var na = rowByKey("f" + plaats.slice(3)); cmd.naId = na.id; cmd.naSoort = "fase"; cmd.sectieId = na.ouderFaseId; }
            var lot = getSelect("gl-v2-pl-f-lot");
            if (lot) { cmd.velden.lotId = parseInt(lot, 10); cmd.velden.teltMee = $("gl-v2-pl-f-teltmee").checked; }
            cmd.eersteTaakNaam = $("gl-v2-pl-f-eerste").value.trim();
            cmd.eersteTaakDuur = parseInt($("gl-v2-pl-f-eerste-duur").value, 10);
            if (isNaN(cmd.eersteTaakDuur)) cmd.eersteTaakDuur = 1;
            cmd.startNa = $("gl-v2-pl-f-startna").value.trim();
            send(cmd, { onError: onError, after: closePanel });
        } else if (panel.mode === "fase-bewerken") {
            var fv = { naam: naam, lotId: getSelect("gl-v2-pl-f-lot") ? parseInt(getSelect("gl-v2-pl-f-lot"), 10) : null, teltMee: $("gl-v2-pl-f-teltmee").checked };
            send({ type: "wijzig", soort: "fase", id: panel.id, velden: fv }, { onError: onError, after: closePanel });
        }
    }

    $("gl-v2-pl-form").addEventListener("submit", function (e) { e.preventDefault(); opslaan(false); });
    $("gl-v2-pl-panel-again").addEventListener("click", function () { opslaan(true); });
    $("gl-v2-pl-panel-cancel").addEventListener("click", closePanel);
    $("gl-v2-pl-panel-close").addEventListener("click", closePanel);
    $("gl-v2-pl-backdrop").addEventListener("click", closePanel);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && panel && !document.querySelector(".gl-v2-select-panel.is-open")) closePanel(); });
    var delBtn = $("gl-v2-pl-panel-delete");
    if (delBtn) delBtn.addEventListener("click", function () { if (panel && panel.key) { var r = rowByKey(panel.key); if (r) vraagVerwijder(r); } });

    // Knoppen in de kop en de lege staat
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-pl-add-taak")) { e.preventDefault(); openNieuwTaak({}); }
        else if (e.target.closest(".js-pl-add-fase")) { e.preventDefault(); openNieuwFase({}); }
    });

    render();
})();
