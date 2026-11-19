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
    var canWriteBasis = root.getAttribute("data-can-write") === "1";
    var GLOBAAL = -1;           // planning-id van het globale overzicht (alle plannings samen, enkel lezen)
    var canWrite = canWriteBasis;   // in het globale overzicht uit: render() zet dit telkens opnieuw
    var canDelete = root.getAttribute("data-can-delete") === "1";
    var canSjabloon = root.getAttribute("data-can-sjabloon") === "1";
    var canSjabloonDelete = root.getAttribute("data-can-sjabloon-delete") === "1";
    var canLink = root.getAttribute("data-can-link") === "1";   // schrijfrecht op het Traject: nodig om een mijlpaal te koppelen
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {};

    var state = cfg.staat || { rijen: [], kop: {}, meldingen: [], gebruikers: [], lots: [], nietWerkdagen: [] };
    var shifted = {};          // rij-sleutels die door de laatste wijziging verschoven zijn
    var selected = null;
    var lastUndo = null;       // momentopname van vóór de laatste wijziging
    var undoIsRedo = false;
    var gantt = null;
    var firstRender = true;

    var UI_KEY = "gl-v2-pl-ui-" + cfg.projectId;
    var ui = { scale: "week", kritiek: false, basis: false, collapsed: {}, listW: 0, noticesOpen: false };
    try { var raw = window.localStorage.getItem(UI_KEY); if (raw) { var p = JSON.parse(raw); ui.scale = p.scale || ui.scale; ui.kritiek = !!p.kritiek; ui.basis = !!p.basis; ui.collapsed = p.collapsed || {}; ui.listW = +p.listW || 0; ui.noticesOpen = !!p.noticesOpen; } } catch (e) { /* geen opslag: gewoon de standaard */ }
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

    // ── Tabs: de plannings van het project ──
    function actief() {
        var lijst = state.planningen || [];
        if (state.actiefPlanningId === GLOBAAL) return { id: GLOBAAL, naam: "Globaal overzicht", isHoofd: false, global: true, aantalTaken: lijst.reduce(function (n, p) { return n + (p.aantalTaken || 0); }, 0) };
        for (var i = 0; i < lijst.length; i++) if (lijst[i].id === state.actiefPlanningId) return lijst[i];
        return lijst[0] || null;
    }
    function renderTabs() {
        var host = $("gl-v2-pl-tabs"), lijst = state.planningen || [];
        if (lijst.length <= 1 && !canWriteBasis) { host.hidden = true; return; }
        host.hidden = false;
        var html = lijst.map(function (p) {
            var on = p.id === state.actiefPlanningId;
            return "<button type=\"button\" class=\"gl-v2-pl-tab\" role=\"tab\" aria-selected=\"" + (on ? "true" : "false") + "\" data-planning=\"" + p.id + "\" title=\"" + esc(p.eenhedenTekst ? "Voor: " + p.eenhedenTekst : (p.isHoofd ? "Geldt voor het hele project" : "Geen woningen gekoppeld")) + "\">" +
                "<i class=\"ph " + (p.isHoofd ? "ph-house-line" : "ph-buildings") + "\" aria-hidden=\"true\"></i><span>" + esc(p.naam) + "</span>" +
                (p.aantalTaken ? "<small>" + p.aantalTaken + "</small>" : "") + "</button>";
        }).join("");
        if (lijst.length > 1) {
            var gOn = state.actiefPlanningId === GLOBAAL;
            html += "<button type=\"button\" class=\"gl-v2-pl-tab is-globaal\" role=\"tab\" aria-selected=\"" + (gOn ? "true" : "false") + "\" data-planning=\"" + GLOBAAL + "\" title=\"Alle plannings samen in één overzicht (enkel lezen)\"><i class=\"ph ph-stack\" aria-hidden=\"true\"></i><span>Globaal</span></button>";
        }
        if (canWriteBasis) {
            html += "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-add-planning\"><i class=\"ph ph-plus\" aria-hidden=\"true\"></i>Planning</button>";
            html += "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-import\" title=\"Een planning importeren uit MS Project (XML)\"><i class=\"ph ph-upload-simple\" aria-hidden=\"true\"></i>Importeren</button>";
            html += "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-sj-toepas\" title=\"Een sjabloon toepassen op woningen of gebouwen\"><i class=\"ph ph-copy\" aria-hidden=\"true\"></i>Sjabloon</button>";
            var act = actief();
            if (state.actiefPlanningId === GLOBAAL) { host.innerHTML = html; return; }
            if (canSjabloon && act && act.aantalTaken) html += "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-sj-bewaar\" title=\"Deze planning bewaren als sjabloon\"><i class=\"ph ph-floppy-disk\" aria-hidden=\"true\"></i>Als sjabloon</button>";
            html += "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-edit-planning\" title=\"Naam, woningen en startdatum wijzigen, of de planning verwijderen\"><i class=\"ph ph-gear-six\" aria-hidden=\"true\"></i>Instellen</button>";
        }
        host.innerHTML = html;
    }
    function zetUrl() {
        try {
            var a = actief(), u = new URL(window.location.href);
            if (a && !a.isHoofd && a.id) u.searchParams.set("planningId", a.id); else u.searchParams.delete("planningId");
            window.history.replaceState(null, "", u);
        } catch (e) { /* geen URL-geschiedenis: de pagina werkt ook zonder */ }
    }
    // Een andere planning: selectie, wijzigingsbalk en ongedaan maken horen bij de vorige
    function resetView() {
        selected = null; shifted = {}; lastUndo = null; undoIsRedo = false; firstRender = true;
        $("gl-v2-pl-changebar").hidden = true;
        zetUrl();
    }
    function switchPlanning(id) {
        if (id === state.actiefPlanningId) return;
        fetch(cfg.staatUrl + (cfg.staatUrl.indexOf("?") < 0 ? "?" : "&") + "planningId=" + id, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("De planning kon niet geladen worden."); return r.json(); })
            .then(function (s) { state = s; resetView(); render(); })
            .catch(function (e) { toast("danger", "Laden mislukt", e.message || "Controleer je verbinding en probeer opnieuw."); });
    }
    $("gl-v2-pl-kop").addEventListener("click", function (e) {
        if (e.target.closest(".js-pl-edit-planning")) openPlanning(true);
    });
    $("gl-v2-pl-tabs").addEventListener("dblclick", function (e) {
        var t = e.target.closest("[data-planning]");
        if (t && parseInt(t.getAttribute("data-planning"), 10) === state.actiefPlanningId) openPlanning(true);
    });
    $("gl-v2-pl-tabs").addEventListener("click", function (e) {
        var t = e.target.closest("[data-planning]");
        if (t) { switchPlanning(parseInt(t.getAttribute("data-planning"), 10)); return; }
        if (e.target.closest(".js-pl-add-planning")) openPlanning(false);
        else if (e.target.closest(".js-pl-edit-planning")) openPlanning(true);
        else if (e.target.closest(".js-pl-import")) openImport();
    });

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
        var pl = actief(), meerdere = (state.planningen || []).length > 1;
        var plEenh = pl && !pl.isHoofd && !pl.global ? "<div class=\"gl-v2-pl-meta-eenheden\"><dt>Geldt voor</dt><dd>" + (pl.eenhedenTekst ? esc(pl.eenhedenTekst) : "<span class=\"is-empty\">geen woningen gekoppeld</span>") + "</dd></div>" : "";
        var plStart = pl && pl.startDatum ? "<div><dt>Startdatum planning</dt><dd>" + fmtFull(pl.startDatum) + "</dd></div>" : "";
        el.innerHTML =
            "<div class=\"gl-v2-pl-kop-title\"><h2>" + (pl && pl.global ? "Globaal overzicht" : (meerdere && pl ? "Bouwplanning · " + esc(pl.naam) : "Bouwplanning")) + "</h2><span class=\"gl-v2-pl-chip\"><i class=\"ph " + (canWrite ? "ph-floppy-disk" : "ph-lock-simple") + "\" aria-hidden=\"true\"></i>" +
            (canWrite ? "Wijzigingen worden meteen bewaard" : "Alleen lezen") + "</span>" +
            (canWrite && pl ? "<button type=\"button\" class=\"gl-v2-pl-tab-add js-pl-edit-planning\" title=\"Naam, woningen en startdatum wijzigen, of de planning verwijderen\"><i class=\"ph ph-pencil-simple\" aria-hidden=\"true\"></i>Naam wijzigen of verwijderen</button>" : "") + "</div>" +
            "<dl class=\"gl-v2-pl-meta\">" +
            plEenh + plStart + "<div><dt>Start</dt><dd>" + fmtFull(k.start) + "</dd></div>" +
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
                "<div class=\"gl-v2-notice-body\"><div class=\"gl-v2-pl-notice-head\"><span class=\"gl-v2-notice-title\">" + (perRow.length === 1 ? "1 aandachtspunt" : perRow.length + " aandachtspunten") + " in de planning</span><button type=\"button\" class=\"gl-v2-pl-notice-toggle\" data-notices-toggle aria-expanded=\"" + (ui.noticesOpen ? "true" : "false") + "\">" + (ui.noticesOpen ? "Verbergen" : "Tonen") + "</button></div><ul class=\"gl-v2-pl-notice-list\"" + (ui.noticesOpen ? "" : " hidden") + ">" + lijst + "</ul></div></div>";
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
            "<span class=\"gl-v2-pl-c is-num c-dur" + (!fase && canWrite ? " is-edit" : " is-muted") + "\" role=\"cell\" " + (!fase && canWrite ? "data-edit=\"duur\"" : "") + ">" + duurTxt + "</span>" +
            "<span class=\"gl-v2-pl-c c-start\" role=\"cell\" data-act=\"open\">" + fmt(r.start) + "</span>" +
            "<span class=\"gl-v2-pl-c c-einde\" role=\"cell\" data-act=\"open\">" + fmt(r.einde) + afw + "</span>" +
            "<span class=\"gl-v2-pl-c c-vg" + (!fase && canWrite ? " is-edit" : "") + "\" role=\"cell\" " + (!fase && canWrite ? "data-edit=\"voorgangers\" title=\"Klik om de voorgangers te wijzigen\"" : "") + ">" + (fase ? "" : (r.voorgangers ? esc(r.voorgangers) : "<span style=\"color:var(--gl-v2-muted-light)\">—</span>")) + "</span>" +
            "<span class=\"gl-v2-pl-c is-num c-pct is-muted\" role=\"cell\">" + (r.voortgang > 0 || r.afgerond ? r.voortgang + "%" : "") + "</span>" +
            "<span class=\"gl-v2-pl-c\" role=\"cell\">" + (canWrite ? "<button type=\"button\" class=\"gl-v2-pl-rowmenu js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-menu\" aria-label=\"Acties voor " + esc(r.naam) + "\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button>" : "") + "</span>" +
            "</div>";
    }

    function renderList(vis) {
        var head = "<div class=\"gl-v2-pl-lh\" role=\"row\"><span role=\"columnheader\">WBS</span><span role=\"columnheader\">Taak</span><span class=\"c-dur\" role=\"columnheader\" style=\"text-align:right\">Duur</span><span class=\"c-start\" role=\"columnheader\">Start</span><span class=\"c-einde\" role=\"columnheader\">Einde</span><span class=\"c-vg\" role=\"columnheader\">Voorgangers</span><span class=\"c-pct\" role=\"columnheader\" style=\"text-align:right\">%</span><span></span></div>";
        $("gl-v2-pl-list").innerHTML = head + vis.map(rowHtml).join("") + "<div class=\"gl-v2-pl-split\" role=\"separator\" aria-orientation=\"vertical\" tabindex=\"0\" aria-label=\"Breedte van de lijst aanpassen\" title=\"Sleep om de lijst breder of smaller te maken (dubbelklik: compact)\"></div>";
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
        var anchor = gantt ? gantt.dateAt(scroller.scrollLeft) : null; // de datum die nu links in beeld staat, zodat zoomen/herladen niet wegspringt
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
                if (!van) return; // voorganger uit de hoofdplanning: staat niet in deze grafiek
                links.push({ from: "t" + l.taakId, to: r.sleutel, type: l.type, critical: !!(r.kritiek && van && van.kritiek) });
            });
        });
        gantt = window.GlV2Gantt.render($("gl-v2-pl-gantt"), {
            rows: rows, links: links, nonWorking: state.nietWerkdagen || [], today: todayIso(), deadline: (state.kop || {}).deadline || null,
            scale: ui.scale, fitWidth: Math.max(320, scroller.clientWidth - curListW - 24), showCritical: ui.kritiek, showBase: ui.basis, rowHeight: 34
        }, {
            onOpen: function (key) { selected = key; markSelected(); if (canWrite) openEdit(key); }
        });
        var todayBtn = $("gl-v2-pl-today");
        todayBtn.disabled = !gantt.hasToday;
        todayBtn.title = gantt.hasToday ? "Naar vandaag" : "Vandaag valt buiten de planning";
        if (firstRender) {
            firstRender = false;
            // Eerst openen op vandaag, en valt vandaag buiten de planning dan op het begin ervan (niet in een lege zone ernaast)
            if (!gantt.scrollToToday(scroller, true)) gantt.scrollToStart(scroller, true);
        } else if (anchor) {
            scroller.scrollLeft = ui.scale === "alles" ? 0 : gantt.xOf(anchor);
        }
        gantt.select(selected);
    }

    function markSelected() {
        document.querySelectorAll("#gl-v2-pl-list .gl-v2-pl-r").forEach(function (el) { el.classList.toggle("is-selected", el.getAttribute("data-key") === selected); });
        if (gantt) gantt.select(selected);
    }

    // ── Alles tekenen ──
    function render() {
        canWrite = canWriteBasis && state.actiefPlanningId !== GLOBAAL;
        var heeft = !!state.heeftPlanning && state.rijen.length > 0;
        renderTabs();
        var pl0 = actief(), emptyTitle = $("gl-v2-pl-empty-title");
        emptyTitle.textContent = pl0 && !pl0.isHoofd ? "Nog geen taken in " + pl0.naam : "Nog geen planning voor " + emptyTitle.getAttribute("data-project");
        $("gl-v2-pl-voor-help-hoofd").hidden = !pl0 || pl0.isHoofd;
        var anderen = (state.planningen || []).filter(function (p) { return !p.isHoofd && p.id !== state.actiefPlanningId; });
        $("gl-v2-pl-voor-help-andere").hidden = anderen.length === 0;
        if (anderen.length) $("gl-v2-pl-voor-help-voorbeeld").textContent = anderen[0].naam + ":3.2";
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
        var scr = $("gl-v2-pl-scroll"), keepTop = scr.scrollTop;
        ensureListW();
        var vis = visibleRows();
        hoverKey = null;
        renderList(vis);
        renderGantt(vis);
        scr.scrollTop = keepTop; // een herteken mag de verticale positie niet verspringen
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
        if (cmd.planningId === undefined) cmd.planningId = state.actiefPlanningId || 0;
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
            if (!res.ok) { if (opts.onError) opts.onError(res.fout); else toonFout("Dat kan niet", res.fout || "De wijziging is niet bewaard."); return false; }
            verbergFout();
            var vorige = {};
            state.rijen.forEach(function (r) { vorige[r.sleutel] = true; });
            res._nieuw = res.staat.rijen.filter(function (r) { return !vorige[r.sleutel]; });
            state = res.staat;
            if (cmd.type.indexOf("planning") === 0 || cmd.type === "sjabloonToepassen" || cmd.type === "importeren") resetView();
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
            if (opts.onError) opts.onError(e.message); else toonFout("Opslaan mislukt", e.message || "Controleer je verbinding en probeer opnieuw.");
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
            send({ type: "wijzig", soort: r.soort, id: r.id, velden: velden }, { onError: function (f) { cell.innerHTML = original; toonFout("Dat kan niet", f); } });
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
        if (gantt) gantt.scrollToKey(key, $("gl-v2-pl-scroll"));
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
        if (e.target.closest("[data-notices-toggle]")) { ui.noticesOpen = !ui.noticesOpen; saveUi(); renderNotices(); return; }
        var b = e.target.closest("[data-toon]"); if (!b) return;
        var key = b.getAttribute("data-toon"), r = rowByKey(key); if (!r) return;
        // een ingeklapte fase openen
        state.rijen.forEach(function (x) { if (x.soort === "fase" && ui.collapsed[x.sleutel] && r.wbs.indexOf(x.wbs + ".") === 0) ui.collapsed[x.sleutel] = false; });
        selected = key; render();
        if (gantt) gantt.scrollToKey(key, $("gl-v2-pl-scroll"));
    });

    // ── Scrollvlak: lijstbreedte, rijmarkering, verschuiven met de muis, volledig scherm ──
    var scrollEl = $("gl-v2-pl-scroll"), listEl = $("gl-v2-pl-list"), card = $("gl-v2-pl-card");
    var curListW = 0, hoverKey = null, pan = null, suppressClick = false;
    function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }
    function defaultListW() { return clamp(Math.round(scrollEl.clientWidth * 0.42), 340, 640); }
    function applyListW(w, persist) {
        curListW = clamp(Math.round(w), 240, Math.max(260, scrollEl.clientWidth - 240));
        scrollEl.style.setProperty("--pl-list-w", curListW + "px");
        if (persist) { ui.listW = curListW; saveUi(); }
    }
    function ensureListW() { if (!curListW && scrollEl.clientWidth) applyListW(ui.listW || defaultListW(), false); }
    function widthChanged(refocus) {
        if (ui.scale !== "alles") return;
        render(); // "Alles" past de planning in de breedte naast de lijst: dan moet de grafiek opnieuw
        if (refocus) { var sp = listEl.querySelector(".gl-v2-pl-split"); if (sp) sp.focus(); }
    }

    // Scheiding tussen lijst en grafiek: slepen, dubbelklik (compact of standaard) en pijltjestoetsen
    scrollEl.addEventListener("pointerdown", function (e) {
        var sp = e.target.closest(".gl-v2-pl-split");
        if (sp) {
            e.preventDefault();
            var x0 = e.clientX, w0 = curListW;
            sp.classList.add("is-drag");
            var mv = function (ev) { applyListW(w0 + ev.clientX - x0, false); };
            var up = function () {
                document.removeEventListener("pointermove", mv); document.removeEventListener("pointerup", up); document.removeEventListener("pointercancel", up);
                sp.classList.remove("is-drag"); ui.listW = curListW; saveUi(); widthChanged(false);
            };
            document.addEventListener("pointermove", mv); document.addEventListener("pointerup", up); document.addEventListener("pointercancel", up);
            return;
        }
        // De grafiek vastpakken en verschuiven (enkel met de muis; aanraking scrolt zelf). Een balk aanklikken blijft een klik.
        if (e.button !== 0 || e.pointerType !== "mouse" || !e.target.closest(".gl-v2-pl-chart") || e.target.closest(".gl-v2-gantt-bar")) return;
        pan = { x: e.clientX, y: e.clientY, l: scrollEl.scrollLeft, t: scrollEl.scrollTop, moved: false };
    });
    function endPan() {
        if (!pan) return;
        var moved = pan.moved; pan = null; scrollEl.classList.remove("is-panning");
        if (moved) { suppressClick = true; window.setTimeout(function () { suppressClick = false; }, 0); }
    }
    document.addEventListener("pointermove", function (e) {
        if (!pan) return;
        if (e.buttons === 0) { endPan(); return; }
        var dx = e.clientX - pan.x, dy = e.clientY - pan.y;
        if (!pan.moved && Math.abs(dx) + Math.abs(dy) < 4) return;
        pan.moved = true; scrollEl.classList.add("is-panning");
        scrollEl.scrollLeft = pan.l - dx; scrollEl.scrollTop = pan.t - dy;
    });
    document.addEventListener("pointerup", endPan);
    document.addEventListener("pointercancel", endPan);
    scrollEl.addEventListener("click", function (e) { if (suppressClick) { e.stopPropagation(); e.preventDefault(); } }, true);
    scrollEl.addEventListener("dblclick", function (e) {
        if (!e.target.closest(".gl-v2-pl-split")) return;
        applyListW(curListW > 360 ? 320 : defaultListW(), true); widthChanged(false);
    });
    scrollEl.addEventListener("keydown", function (e) {
        if (!e.target.closest(".gl-v2-pl-split")) return;
        var d = e.key === "ArrowLeft" ? -24 : e.key === "ArrowRight" ? 24 : 0;
        if (!d) return;
        e.preventDefault(); applyListW(curListW + d, true); widthChanged(true);
    });

    // Rijmarkering: wie met de muis op een rij in de lijst of in de grafiek staat, kleurt de hele rij in beide
    function setHover(key) {
        if (key === hoverKey) return;
        var prev = listEl.querySelector(".gl-v2-pl-r.is-hover"); if (prev) prev.classList.remove("is-hover");
        hoverKey = key;
        if (gantt) gantt.hover(key);
    }
    scrollEl.addEventListener("pointermove", function (e) {
        if (pan) return;
        var row = e.target.closest(".gl-v2-pl-r"), key = null, fromChart = false;
        if (row) key = row.getAttribute("data-key");
        else if (gantt && e.target.closest(".gl-v2-pl-chart")) { key = gantt.keyAt(e.clientY); fromChart = true; }
        if (key === hoverKey) return;
        setHover(key);
        if (fromChart && key) { var el = listEl.querySelector(".gl-v2-pl-r[data-key=\"" + key + "\"]"); if (el) el.classList.add("is-hover"); }
    });
    scrollEl.addEventListener("pointerleave", function () { setHover(null); });

    // Navigatie
    $("gl-v2-pl-start").addEventListener("click", function () { if (gantt) gantt.scrollToStart(scrollEl); });

    // Volledig scherm
    var fsBtn = $("gl-v2-pl-fs");
    function setFs(on) {
        card.classList.toggle("is-fullscreen", on); document.documentElement.classList.toggle("gl-v2-pl-fs", on);
        fsBtn.setAttribute("aria-pressed", on ? "true" : "false");
        fsBtn.querySelector("i").className = "ph " + (on ? "ph-arrows-in" : "ph-arrows-out");
        fsBtn.querySelector("span").textContent = on ? "Sluiten" : "Volledig scherm";
    }
    fsBtn.addEventListener("click", function () { setFs(!card.classList.contains("is-fullscreen")); });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && card.classList.contains("is-fullscreen") && !panel && !document.querySelector(".gl-v2-select-panel.is-open")) setFs(false);
    });

    // Het scrollvlak wordt breder of smaller (venster, volledig scherm): lijstbreedte bijstellen, en bij "Alles" opnieuw passen
    if (window.ResizeObserver) {
        var lastW = 0;
        new ResizeObserver(function () {
            var w = scrollEl.clientWidth;
            if (!w || Math.abs(w - lastW) < 24) return;
            lastW = w;
            if (curListW) applyListW(ui.listW || defaultListW(), false);
            if (ui.scale === "alles" && gantt) render();
        }).observe(scrollEl);
    }

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
    // Blokkerende fout: gecentreerde inline melding (design-handoff 4g) in plaats van een toast rechtsonder.
    function toonFout(titel, tekst) {
        var el = $("gl-v2-pl-fout"); if (!el) { toast("danger", titel, tekst); return; }
        $("gl-v2-pl-fout-titel").textContent = titel || "Dat kan niet";
        $("gl-v2-pl-fout-tekst").textContent = tekst || "";
        el.hidden = false;
    }
    function verbergFout() { var el = $("gl-v2-pl-fout"); if (el) el.hidden = true; }
    $("gl-v2-pl-fout-close").addEventListener("click", verbergFout);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") verbergFout(); });

    function formError(msg) { var el = $("gl-v2-pl-form-error"); el.querySelector("span").textContent = msg; el.hidden = false; el.scrollIntoView({ block: "nearest" }); }

    function zetVorm(soort) {
        $("gl-v2-pl-wrap-estafette").hidden = true;
        sluitKies();
        document.querySelector(".gl-v2-pl-only-taak").hidden = soort !== "taak";
        document.querySelector(".gl-v2-pl-only-fase").hidden = soort !== "fase";
        document.querySelector(".gl-v2-pl-only-planning").hidden = soort !== "planning";
        zetUitvoerderSelect("gl-v2-pl-f-uitv", null); zetUitvoerderSelect("gl-v2-pl-f-uitvf", null);   // nieuw paneel: geen keuze van een vorige rij overhouden
    }
    function taakVorm(on) { zetVorm(on ? "taak" : "fase"); }
    // Uitvoerder: doorzoekbare keuzelijst (gl-v2-combo) met enkel aannemers die een contract hebben in het project.
    // `leeg` = de tekst als er geen eigen keuze is (placeholder, bv. "Van de fase: …").
    function zetUitvoerderSelect(id, waarde, leeg) {
        var hidden = $(id), root = hidden && hidden.closest("[data-gl-v2-combo]"), cmb = window.GlV2Combo;
        if (!root || !cmb) return;
        var lijst = (state.uitvoerders || []).filter(function (u) { return u.inProject; });
        cmb.setOptions(root, lijst.map(function (u) { return { id: String(u.id), text: u.naam }; }));
        var gekozen = waarde != null ? lijst.filter(function (u) { return u.id === waarde; })[0] : null;
        var inp = root.querySelector(".gl-v2-combo-input"); if (inp) inp.placeholder = leeg || "Geen";
        if (gekozen) cmb.setValue(root, String(gekozen.id), gekozen.naam); else cmb.setValue(root, "", "");
    }

    // ── Planning (hoofdplanning of bouwdeel) toevoegen of instellen ──
    function openPlanning(bestaand) {
        if (!canWrite) return;
        var p = bestaand ? actief() : null;
        if (bestaand && !p) return;
        panel = { mode: bestaand ? "planning-bewerken" : "planning-nieuw", id: p ? p.id : null, isHoofd: !!(p && p.isHoofd) };
        zetVorm("planning");
        var info = $("gl-v2-pl-info");
        info.hidden = !(bestaand && p && !p.isHoofd && !canDelete);
        info.innerHTML = info.hidden ? "" : "<span>Verwijderen van een planning vraagt het recht Verwijderen op Planning. Je rol heeft dat niet.</span>";
        $("gl-v2-pl-f-naam").value = p ? p.naam : "";
        var eigen = p ? (p.eenheidIds || []) : [];
        var planNaam = {}; (state.planningen || []).forEach(function (x) { planNaam[x.id] = x.naam; });
        $("gl-v2-pl-f-eenheden").innerHTML = (state.eenheden || []).length ? (state.eenheden || []).map(function (e) {
            var inAndere = e.planningId != null && eigen.indexOf(e.id) < 0;
            return "<label class=\"gl-v2-pl-een" + (inAndere ? " is-uit" : "") + "\"><input type=\"checkbox\" value=\"" + e.id + "\"" + (eigen.indexOf(e.id) >= 0 ? " checked" : "") + (inAndere ? " disabled" : "") + " /><span>" + esc(e.naam) + "</span>" +
                "<small>" + (inAndere ? "zit in " + esc(planNaam[e.planningId] || "een andere planning") : esc(e.groepNaam || "")) + "</small></label>";
        }).join("") : "<span class=\"gl-v2-field-help\" style=\"padding:8px 12px\">Dit project heeft nog geen woningen of gebouwen.</span>";
        $("gl-v2-pl-wrap-bouwdeel").hidden = panel.isHoofd;
        setDate("gl-v2-pl-f-pstart", p ? p.startDatum : "");
        var del = $("gl-v2-pl-panel-delete"); if (del) del.hidden = !(canDelete && p && !p.isHoofd && p.id);
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = bestaand ? "Opslaan" : "Planning toevoegen";
        showPanel("PLANNING", bestaand ? p.naam : "Nieuwe planning", bestaand ? (p.isHoofd ? "Hoofdplanning: geldt voor het hele project" : (p.eenhedenTekst ? "Voor " + p.eenhedenTekst : "")) : "Een planning voor een of meer woningen of een gebouw, naast de hoofdplanning");
    }
    function vraagVerwijderPlanning(p) {
        var n = p.aantalTaken || 0;
        var desc = "De planning " + p.naam + (n ? " en haar " + n + (n === 1 ? " taak" : " taken") : "") + " worden verwijderd. Taken in andere plannings die erop wachten verliezen deze voorganger. Dit kan je niet ongedaan maken.";
        var run = function () { send({ type: "planningVerwijder", planningId: p.id }); closePanel(); };
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: "Planning verwijderen?", desc: desc, ok: "Verwijderen", tone: "danger", icon: "ph-trash" }).then(function (ok) { if (ok) run(); });
        else if (window.confirm(desc)) run();
    }

    // ── Sjablonen: toepassen op woningen of gebouwen, en een planning bewaren als sjabloon ──
    function modal(id) { var el = $(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }
    function postJson(url, body) {
        return fetch(url, {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(body)
        }).then(function (r) {
            if (r.status === 403) throw new Error("Je hebt hier geen recht toe.");
            if (!r.ok) throw new Error("Opslaan mislukt. Probeer het zo opnieuw.");
            return r.json();
        });
    }
    function herlaadStaat() {
        return fetch(cfg.staatUrl + (cfg.staatUrl.indexOf("?") < 0 ? "?" : "&") + "planningId=" + (state.actiefPlanningId || 0), { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.json(); }).then(function (s) { state = s; render(); });
    }
    function sjabloonGekozen() {
        var id = parseInt($("gl-v2-pl-sj-kies").value, 10);
        return (state.sjablonen || []).filter(function (s) { return s.id === id; })[0] || null;
    }
    var sjRijen = [];   // { id, naam, aan, verdiepingen, kelder, dak } in de volgorde van de lijst
    var sjAlle = { verdiepingen: 1, kelder: false, dak: "" };
    function sjOpties(s) { return (s.heeftVerdiepingen ? 1 : 0) + (s.heeftKelder ? 1 : 0) + (s.heeftDak ? 1 : 0); }
    // Dak per rij: zelfde markup als GlV2/_SelectList (basisvariant); GlV2Select.init bedraadt ze na het tekenen.
    function dakKeuze(waarde, id) {
        var items = [{ v: "", t: "Geen" }, { v: "hellend", t: "Hellend" }, { v: "plat", t: "Plat" }];
        var cur = items.filter(function (i) { return i.v === (waarde || ""); })[0];
        return "<div class=\"gl-v2-select\" data-gl-v2-select data-placeholder=\"Geen\"><input type=\"hidden\" data-veld=\"dak\" data-id=\"" + id + "\" value=\"" + esc(waarde || "") + "\" />" +
            "<div class=\"gl-v2-select-trigger is-filled\" role=\"button\" tabindex=\"0\" aria-haspopup=\"listbox\" aria-expanded=\"false\" aria-label=\"Dak\"><span class=\"gl-v2-select-trigger-label\">" + cur.t + "</span><i class=\"ph ph-caret-down gl-v2-select-trigger-caret\" aria-hidden=\"true\"></i></div>" +
            "<div class=\"gl-v2-select-panel\" role=\"listbox\">" + items.map(function (i) {
                return "<button type=\"button\" class=\"gl-v2-select-option" + (i.v === (waarde || "") ? " is-selected" : "") + "\" role=\"option\" data-value=\"" + i.v + "\"><i class=\"ph ph-check\" aria-hidden=\"true\"></i><span>" + i.t + "</span></button>";
            }).join("") + "</div></div>";
    }
    function renderSjTabel() {
        var s = sjabloonGekozen(), wrap = $("gl-v2-pl-sj-bouwdelen-wrap");
        if (!s) { wrap.hidden = true; return; }
        wrap.hidden = false;
        $("gl-v2-pl-sj-bouwdelen-help").textContent = sjRijen.length
            ? "Zet een vinkje bij de woningen of plannings waarvoor je het sjabloon wilt gebruiken. Een woning zonder planning krijgt een nieuwe planning; wil je meerdere woningen in één planning, maak die dan eerst met Planning toevoegen. De volgorde in de lijst bepaalt de estafette" + (s.heeftEstafette ? ": taken met de estafettevlag wachten op dezelfde taak in de woning erboven." : ".")
            : "Dit project heeft nog geen woningen of gebouwen. Maak ze eerst aan bij Facturatie.";
        // Enkel de kolommen die dit sjabloon gebruikt; de naam krijgt de rest van de breedte.
        var cols = "style=\"grid-template-columns: 26px minmax(0, 1fr)" + (s.heeftVerdiepingen ? " 86px" : "") + (s.heeftKelder ? " 56px" : "") + (s.heeftDak ? " 112px" : "") + " 58px\"";
        var kolommen = "<span></span><span>Naam</span>" + (s.heeftVerdiepingen ? "<span>Verdiepingen</span>" : "") + (s.heeftKelder ? "<span>Kelder</span>" : "") + (s.heeftDak ? "<span>Dak</span>" : "") + "<span></span>";
        function velden(r, alle) {
            var id = alle ? "alle" : r.id;
            return (s.heeftVerdiepingen ? "<span><input type=\"number\" min=\"1\" max=\"30\" value=\"" + r.verdiepingen + "\" data-veld=\"verdiepingen\" data-id=\"" + id + "\" aria-label=\"Verdiepingen\" /></span>" : "") +
                (s.heeftKelder ? "<span><input type=\"checkbox\" " + (r.kelder ? "checked" : "") + " data-veld=\"kelder\" data-id=\"" + id + "\" aria-label=\"Kelder\" /></span>" : "") +
                (s.heeftDak ? "<span>" + dakKeuze(r.dak, id) + "</span>" : "");
        }
        var html = "<div class=\"gl-v2-pl-sj-rij is-kop\" " + cols + ">" + kolommen + "</div>";
        if (sjOpties(s) > 0 && sjRijen.length > 1)
            html += "<div class=\"gl-v2-pl-sj-rij is-alle\" " + cols + "><span></span><span class=\"gl-v2-pl-sj-naam\"><b>Alle gekozen</b></span>" + velden(sjAlle, true) + "<span></span></div>";
        sjRijen.forEach(function (r, n) {
            html += "<div class=\"gl-v2-pl-sj-rij" + (r.aan ? "" : " is-uit") + "\" " + cols + "><span><input type=\"checkbox\" " + (r.aan ? "checked" : "") + " data-veld=\"aan\" data-id=\"" + r.id + "\" aria-label=\"" + esc(r.naam) + " plannen\" /></span>" +
                "<span class=\"gl-v2-pl-sj-naam\" title=\"" + esc(r.naam) + "\"><span class=\"gl-v2-pl-sj-n\">" + esc(r.naam) + "</span>" + (r.taken > 0 ? "<small>Bestaande planning · vervangt " + r.taken + (r.taken === 1 ? " taak" : " taken") + "</small>" : (r.nieuw ? "<small>Nieuwe planning</small>" : "<small>Bestaande lege planning</small>")) + "</span>" + velden(r, false) +
                "<span class=\"gl-v2-pl-sj-volgorde\"><button type=\"button\" data-op=\"-1\" data-id=\"" + r.id + "\" aria-label=\"Eerder\"" + (n === 0 ? " disabled" : "") + "><i class=\"ph ph-caret-up\" aria-hidden=\"true\"></i></button>" +
                "<button type=\"button\" data-op=\"1\" data-id=\"" + r.id + "\" aria-label=\"Later\"" + (n === sjRijen.length - 1 ? " disabled" : "") + "><i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button></span></div>";
        });
        $("gl-v2-pl-sj-tabel").innerHTML = html;
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init($("gl-v2-pl-sj-tabel"));
    }
    function sjInfo() {
        var s = sjabloonGekozen(), el = $("gl-v2-pl-sj-info");
        if (!s) { el.textContent = ""; $("gl-v2-pl-sj-ok").disabled = true; return; }
        var v = []; if (s.heeftVerdiepingen) v.push("verdiepingen"); if (s.heeftKelder) v.push("kelder"); if (s.heeftDak) v.push("dak");
        el.textContent = (s.beschrijving ? s.beschrijving + " · " : "") + s.aantalBlokken + (s.aantalBlokken === 1 ? " blok" : " blokken") + ", " + s.aantalTaken + (s.aantalTaken === 1 ? " taak" : " taken") + (v.length ? ". Vraagt naar: " + v.join(", ") + "." : ".");
        $("gl-v2-pl-sj-ok").disabled = false;
        $("gl-v2-pl-sj-verwijder").hidden = !canSjabloonDelete;
    }
    function openSjToepas() {
        if (!canWrite) return;
        var lijst = state.sjablonen || [];
        $("gl-v2-pl-sj-fout").hidden = true;
        setSelect("gl-v2-pl-sj-kies", lijst.map(function (s) { return { value: String(s.id), text: s.naam }; }), lijst.length ? String(lijst[0].id) : "");
        sjAlle = { verdiepingen: 1, kelder: false, dak: "" };
        // Rijen: de bestaande plannings van woningen (vullen of vervangen) en elke eenheid die nog in geen planning zit (nieuwe planning).
        sjRijen = (state.planningen || []).filter(function (p) { return !p.isHoofd; })
            .map(function (p) { return { id: "p" + p.id, planningId: p.id, naam: p.naam, taken: p.aantalTaken || 0, aan: false, verdiepingen: 1, kelder: false, dak: "" }; })
            .concat((state.eenheden || []).filter(function (e) { return e.planningId == null; })
                .map(function (e) { return { id: "e" + e.id, eenheidId: e.id, naam: e.naam, nieuw: true, taken: 0, aan: false, verdiepingen: 1, kelder: false, dak: "" }; }));
        sjInfo(); renderSjTabel();
        if (!lijst.length) { $("gl-v2-pl-sj-info").textContent = canSjabloon ? "Bewaar eerst een bestaande planning als sjabloon (het diskette-icoon naast de tabs)." : "Er zijn nog geen sjablonen. Iemand met het recht Planningssjablonen kan er een bewaren."; $("gl-v2-pl-sj-ok").disabled = true; $("gl-v2-pl-sj-bouwdelen-wrap").hidden = true; }
        var m = modal("gl-v2-pl-sj-toepas"); if (m) m.show();
    }
    $("gl-v2-pl-sj-kies").addEventListener("change", function () { sjInfo(); renderSjTabel(); });
    $("gl-v2-pl-sj-tabel").addEventListener("change", function (e) {
        var el = e.target, veld = el.getAttribute("data-veld"), id = el.getAttribute("data-id"); if (!veld) return;
        var waarde = el.type === "checkbox" ? el.checked : (veld === "verdiepingen" ? Math.max(1, parseInt(el.value, 10) || 1) : el.value);
        if (id === "alle") { sjAlle[veld] = waarde; sjRijen.forEach(function (r) { if (veld !== "aan") r[veld] = waarde; }); }
        else { var r = sjRijen.filter(function (x) { return String(x.id) === id; })[0]; if (r) r[veld] = waarde; }
        renderSjTabel();
    });
    $("gl-v2-pl-sj-tabel").addEventListener("click", function (e) {
        var b = e.target.closest("button[data-op]"); if (!b) return;
        var id = b.getAttribute("data-id"), op = parseInt(b.getAttribute("data-op"), 10);
        var n = sjRijen.findIndex(function (x) { return String(x.id) === id; }), m = n + op;
        if (n < 0 || m < 0 || m >= sjRijen.length) return;
        var t = sjRijen[n]; sjRijen[n] = sjRijen[m]; sjRijen[m] = t; renderSjTabel();
    });
    $("gl-v2-pl-sj-toepas").querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        var s = sjabloonGekozen(), fout = $("gl-v2-pl-sj-fout"); fout.hidden = true;
        if (!s) return;
        var gekozen = sjRijen.filter(function (r) { return r.aan; });
        if (!gekozen.length) { fout.textContent = "Kies minstens één woning of gebouw."; fout.hidden = false; return; }
        var cmd = { type: "sjabloonToepassen", planningId: 0, toepassing: { sjabloonId: s.id, planningen: gekozen.map(function (r) {
            return { planningId: r.planningId || null, eenheidIds: r.eenheidId ? [r.eenheidId] : [], verdiepingen: s.heeftVerdiepingen ? r.verdiepingen : 1, kelder: s.heeftKelder ? !!r.kelder : false, dak: s.heeftDak && r.dak ? r.dak : null, vervang: r.taken > 0 };
        }) } };
        var vervangt = gekozen.filter(function (r) { return r.taken > 0; });
        var stuur = function () {
        $("gl-v2-pl-sj-ok").disabled = true;
        send(cmd, { silent: true, onError: function (f) { fout.textContent = f || "Het sjabloon is niet toegepast."; fout.hidden = false; $("gl-v2-pl-sj-ok").disabled = false; },
            after: function (res) {
                var m = modal("gl-v2-pl-sj-toepas"); if (m) m.hide();
                $("gl-v2-pl-sj-ok").disabled = false;
                var n = gekozen.length, w = res.waarschuwingen || [];
                if (w.length) toast("warning", n + (n === 1 ? " planning aangemaakt" : " plannings aangemaakt"), w.slice(0, 3).join(" ") + (w.length > 3 ? " (en " + (w.length - 3) + " meer)" : ""));
                else toast("success", n + (n === 1 ? " planning aangemaakt" : " plannings aangemaakt"), "Controleer de duur en voorgangers, de data zijn doorgerekend.");
            } });
        };
        if (!vervangt.length) { stuur(); return; }
        var desc = "De bestaande planning van " + vervangt.map(function (r) { return r.naam; }).join(", ") + " wordt vervangen door het sjabloon " + s.naam + ". De fases en taken die er nu in staan gaan verloren.";
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: "Bestaande planning vervangen?", desc: desc, ok: "Vervangen", tone: "danger", icon: "ph-arrows-clockwise" }).then(function (ok) { if (ok) stuur(); });
        else if (window.confirm(desc)) stuur();
    });
    $("gl-v2-pl-sj-verwijder").addEventListener("click", function () {
        var s = sjabloonGekozen(); if (!s || !canSjabloonDelete) return;
        var desc = "Het sjabloon " + s.naam + " wordt verwijderd. Plannings die er al mee gemaakt zijn blijven bestaan. Dit kan je niet ongedaan maken.";
        var run = function () {
            postJson(cfg.sjabloonVerwijderUrl, { id: s.id }).then(function (res) {
                if (!res.ok) { toonFout("Verwijderen mislukt", res.fout || "Het sjabloon is niet verwijderd."); return; }
                var m = modal("gl-v2-pl-sj-toepas"); if (m) m.hide();
                herlaadStaat(); toast("success", "Sjabloon verwijderd", s.naam);
            }).catch(function (e) { toonFout("Verwijderen mislukt", e.message); });
        };
        if (window.GlV2Bevestig) window.GlV2Bevestig({ title: "Sjabloon verwijderen?", desc: desc, ok: "Verwijderen", tone: "danger", icon: "ph-trash" }).then(function (ok) { if (ok) run(); });
        else if (window.confirm(desc)) run();
    });

    // ── Planning importeren (MS Project-XML): Bestand · Controle · Opties · Resultaat ──
    var imp = null;
    var IMP_STAPPEN = ["Bestand", "Controle", "Uitvoerders", "Opties", "Resultaat"];
    function impReset() {
        imp = { koppelingen: {}, stap: 0, data: null, filter: "alles", doel: "nieuw", modus: "toevoegen", start: "bestand", naam: "", naamAangepast: false, eenheden: [], resultaat: null, bezig: false };
    }
    function impFout(t) { var f = $("gl-v2-pl-imp-fout"); f.textContent = t || ""; f.hidden = !t; }
    function impHuidig() { return state.actiefPlanningId === GLOBAAL ? null : actief(); }
    function radioWaarde(naam) { var r = document.querySelector("input[name=\"" + naam + "\"]:checked"); return r ? r.value : ""; }
    function zetRadio(naam, waarde) { document.querySelectorAll("input[name=\"" + naam + "\"]").forEach(function (r) { r.checked = r.value === waarde; }); }

    function openImport() {
        if (!canWriteBasis) return;
        impReset();
        impFout("");
        $("gl-v2-pl-imp-file").value = "";
        var huidig = impHuidig();
        $("gl-v2-pl-imp-k-huidig").hidden = !huidig;
        if (huidig) $("gl-v2-pl-imp-k-huidig-naam").textContent = huidig.isHoofd ? "In de hoofdplanning" : "In " + huidig.naam;
        zetRadio("gl-v2-pl-imp-doel", "nieuw"); zetRadio("gl-v2-pl-imp-modus", "toevoegen"); zetRadio("gl-v2-pl-imp-start", "bestand");
        renderImp();
        var m = modal("gl-v2-pl-imp"); if (m) m.show();
    }

    function impBestandKiezen(file) {
        if (!file) return;
        impFout("");
        if (!/\.xml$/i.test(file.name)) {
            impFout(/\.mpp$/i.test(file.name)
                ? "Een .mpp-bestand kan niet rechtstreeks. Open het in MS Project, kies Bestand › Opslaan als › XML-indeling (*.xml) en kies dat bestand."
                : "Enkel MS Project-XML (.xml) kan nu ingelezen worden.");
            return;
        }
        if (file.size > 10 * 1024 * 1024) { impFout("Het bestand is groter dan 10 MB."); return; }
        var fd = new FormData(); fd.append("bestand", file);
        $("gl-v2-pl-imp-drop").classList.add("is-bezig");
        fetch(cfg.importControleUrl, { method: "POST", credentials: "same-origin", headers: { "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" }, body: fd })
            .then(function (r) { if (r.status === 403) throw new Error("Je hebt hier geen recht toe."); if (!r.ok) throw new Error("Het bestand kon niet gelezen worden."); return r.json(); })
            .then(function (d) {
                $("gl-v2-pl-imp-drop").classList.remove("is-bezig");
                if (!d.ok) { impFout(d.fout || "Het bestand kon niet gelezen worden."); return; }
                imp.data = d; imp.stap = 1; imp.filter = "alles"; imp.koppelingen = {}; imp.resGezien = false;
                imp.naam = (d.projectnaam || d.bestandsnaam || "").replace(/\.xml$/i, "").replace(/[:;,]/g, " ").replace(/\s+/g, " ").trim();
                imp.naamAangepast = false;
                renderImp();
            })
            .catch(function (e) { $("gl-v2-pl-imp-drop").classList.remove("is-bezig"); impFout(e.message || "Het bestand kon niet gelezen worden."); });
    }

    function impHeeftRes() { return !!(imp.data && imp.data.resources && imp.data.resources.length); }

    // Voorstel voor een koppeling: de woorden uit de resourcenaam komen in de bedrijfsnaam voor (het best passende bedrijf, liefst uit het project).
    function impWoorden(t) {
        return String(t || "").toLowerCase().replace(/[^a-z0-9 ]+/g, " ").split(/\s+/).filter(function (w) { return w.length >= 3 && ["bvba", "nv", "bv", "van", "der", "den", "het", "and"].indexOf(w) < 0; })
            .map(function (w) { return w.replace(/(werken|werk)$/, "").slice(0, 6); }).filter(function (w) { return w.length >= 3; });
    }
    function impSuggestie(naam) {
        var wn = impWoorden(naam); if (!wn.length) return null;
        var beste = null, score = 0;
        (imp.data.bedrijven || []).forEach(function (b) {
            var wb = impWoorden(b.naam); if (!wb.length) return;
            if (!b.inProject) return;   // enkel aannemers met een contract in dit project
            var gedeeld = wn.filter(function (w) { return wb.indexOf(w) >= 0; }).length;
            var sc = gedeeld / Math.max(wn.length, wb.length);
            if (gedeeld > 0 && sc > score) { score = sc; beste = b; }
        });
        return score >= 0.5 ? beste.id : null;
    }
    function renderImpRes() {
        var d = imp.data, res = d.resources || [];
        if (!imp.resGezien) {
            imp.resGezien = true;
            res.forEach(function (r) { var id = impSuggestie(r.naam); if (id != null && imp.koppelingen[r.naam] == null) imp.koppelingen[r.naam] = id; });
        }
        // Enkel aannemers met een contract in dit project; doorzoekbare keuzelijst (gl-v2-combo) per resource.
        var comboOpties = (d.bedrijven || []).filter(function (b) { return b.inProject; }).map(function (b) { return { id: String(b.id), text: b.naam }; });
        var optiesJson = esc(JSON.stringify(comboOpties));
        function comboHtml(n, r) {
            return "<div class=\"gl-v2-combo\" data-gl-v2-combo data-gl-v2-search-select data-options=\"" + optiesJson + "\" data-min-chars=\"0\" data-icon=\"ph-hard-hat\" data-empty-value=\"\" data-allow-new=\"false\" data-list-label=\"AANNEMERS IN DIT PROJECT\">" +
                "<input type=\"hidden\" class=\"gl-v2-pl-imp-rsel\" data-n=\"" + n + "\" data-role=\"id-hidden\" value=\"\" />" +
                "<div class=\"gl-v2-combo-row\"><div class=\"gl-v2-combo-field\"><i class=\"ph ph-hard-hat gl-v2-combo-lead\" aria-hidden=\"true\"></i>" +
                "<input type=\"text\" class=\"gl-v2-combo-input\" role=\"combobox\" aria-autocomplete=\"list\" aria-expanded=\"false\" aria-haspopup=\"listbox\" autocomplete=\"off\" placeholder=\"Zoek een aannemer…\" aria-label=\"Aannemer voor " + esc(r.naam) + "\" />" +
                "<button type=\"button\" class=\"gl-v2-combo-clear\" data-role=\"clear\" aria-label=\"Wissen\" hidden><i class=\"ph ph-x\" aria-hidden=\"true\"></i></button>" +
                "<span class=\"gl-v2-combo-spinner\" data-role=\"spinner\" hidden></span><i class=\"ph ph-caret-down gl-v2-combo-caret\" aria-hidden=\"true\"></i></div></div>" +
                "<div class=\"gl-v2-combo-panel\" data-role=\"panel\" role=\"listbox\"></div></div>";
        }
        var gekoppeld = res.filter(function (r) { return imp.koppelingen[r.naam] != null; }).length;
        $("gl-v2-pl-imp-res-info").textContent = gekoppeld + " van " + res.length + " resources gekoppeld";
        $("gl-v2-pl-imp-res").innerHTML = "<div class=\"gl-v2-pl-imp-th gl-v2-pl-imp-rth\" role=\"row\"><span>Resource in het bestand</span><span>Taken</span><span>Bedrijf</span></div>" +
            res.map(function (r, n) {
                return "<div class=\"gl-v2-pl-imp-tr gl-v2-pl-imp-rtr\" role=\"row\"><span>" + esc(r.naam) + "</span><span class=\"is-mono\">" + r.aantalTaken + "</span>" +
                    "<span>" + comboHtml(n, r) + "</span></div>";
            }).join("");
        // Bedraad de keuzelijsten (ook die van een vorige render) en zet de bestaande/voorgestelde koppelingen.
        var cmb = window.GlV2Combo;
        if (cmb) {
            cmb.init($("gl-v2-pl-imp-res"));
            res.forEach(function (r, n) {
                var id = imp.koppelingen[r.naam]; if (id == null) return;
                var b = comboOpties.filter(function (o) { return o.id === String(id); })[0];
                var hidden = $("gl-v2-pl-imp-res").querySelector(".gl-v2-pl-imp-rsel[data-n=\"" + n + "\"]");
                if (b && hidden) cmb.setValue(hidden.closest("[data-gl-v2-combo]"), b.id, b.text);
            });
        }
    }

    function impKpi(label, n, waarschuwing) { return "<div class=\"gl-v2-pl-imp-kpi" + (waarschuwing ? " is-warn" : "") + "\"><span>" + label + "</span><b>" + n + "</b></div>"; }
    var IMP_STATUS = { ok: ["ph-check", "In orde"], waarschuwing: ["ph-warning", "Aandachtspunt"], fout: ["ph-x-circle", "Fout"] };

    function renderImp() {
        var s = imp.stap, d = imp.data;
        $("gl-v2-pl-imp-stappen").innerHTML = IMP_STAPPEN.map(function (l, k) {
            return "<li class=\"" + (k < s ? "is-klaar" : k === s ? "is-actief" : "") + "\"" + (k === s ? " aria-current=\"step\"" : "") + "><i>" + (k + 1) + "</i>" + l + "</li>";
        }).join("");
        document.querySelectorAll("#gl-v2-pl-imp .gl-v2-pl-imp-stap").forEach(function (el) { el.hidden = parseInt(el.getAttribute("data-stap"), 10) !== s; });
        $("gl-v2-pl-imp-terug").hidden = s === 0 || s === 4;
        $("gl-v2-pl-imp-annuleer").hidden = s === 4;
        $("gl-v2-pl-imp-nogeen").hidden = s !== 4;
        var ok = $("gl-v2-pl-imp-ok");
        // De generieke "bezig"-knopstaat van de modal-formulieren mag deze knop nooit vergrendeld achterlaten.
        ok.classList.remove("is-loading"); if (ok.dataset) delete ok.dataset.glV2Loading;
        ok.hidden = s === 0;
        ok.disabled = imp.bezig;
        ok.textContent = s === 3 ? "Importeren" : s === 4 ? "Planning openen" : "Volgende →";

        if (s === 1 && d) {
            $("gl-v2-pl-imp-bestand").innerHTML = "<span class=\"gl-v2-pl-imp-ty\">XML</span><div><b>" + esc(d.bestandsnaam) + "</b><small>MS Project-XML · " + d.rijen.length + " regels gevonden</small></div><button type=\"button\" class=\"gl-v2-btn gl-v2-btn-text\" id=\"gl-v2-pl-imp-ander\">Ander bestand</button>";
            $("gl-v2-pl-imp-kpis").innerHTML = impKpi("Taken", d.aantalTaken) + impKpi("Fases", d.aantalFases) + impKpi("Mijlpalen", d.aantalMijlpalen) + impKpi("Afhankelijkheden", d.aantalAfhankelijkheden) + impKpi("Aandachtspunten", d.aantalAandachtspunten, d.aantalAandachtspunten > 0);
            $("gl-v2-pl-imp-filter").innerHTML = "<button type=\"button\" data-f=\"alles\" class=\"" + (imp.filter === "alles" ? "is-active" : "") + "\" aria-pressed=\"" + (imp.filter === "alles") + "\">Alles</button>" +
                "<button type=\"button\" data-f=\"let\" class=\"" + (imp.filter === "let" ? "is-active" : "") + "\" aria-pressed=\"" + (imp.filter === "let") + "\">Aandachtspunten (" + d.aantalAandachtspunten + ")</button>";
            var rijen = d.rijen.filter(function (r) { return imp.filter === "alles" || r.status !== "ok"; });
            var toon = rijen.slice(0, 200);
            $("gl-v2-pl-imp-filter-info").textContent = rijen.length > toon.length ? "Voorbeeld van de eerste " + toon.length + " van " + rijen.length + " regels" : "";
            var uidWbs = {}; d.rijen.forEach(function (r) { uidWbs[r.uid] = r.wbs; });
            $("gl-v2-pl-imp-tabel").innerHTML = "<div class=\"gl-v2-pl-imp-th\" role=\"row\"><span>Status</span><span>WBS</span><span>Taak</span><span>Soort</span><span>Duur</span><span>Voorgangers</span><span>Melding</span></div>" +
                (toon.length ? toon.map(function (r) {
                    var st = IMP_STATUS[r.status] || IMP_STATUS.ok;
                    var vg = (r.voorgangers || []).map(function (v) { return (uidWbs[v.uid] || v.uid) + (v.type === "FS" ? "" : v.type) + (v.wacht ? (v.wacht > 0 ? "+" : "−") + Math.abs(v.wacht) + "d" : ""); }).join("; ");
                    return "<div class=\"gl-v2-pl-imp-tr is-" + r.status + (r.soort === "fase" ? " is-fase" : "") + "\" role=\"row\"><span><span class=\"gl-v2-pl-imp-chip\"><i class=\"ph " + st[0] + "\" aria-hidden=\"true\"></i>" + st[1] + "</span></span><span class=\"is-mono\">" + esc(r.wbs) + "</span>" +
                        "<span><span style=\"padding-left:" + Math.max(0, r.niveau - 1) * 14 + "px\">" + esc(r.naam) + "</span></span><span>" + r.soort + "</span><span class=\"is-mono\">" + (r.soort === "fase" ? "" : r.duur + " d") + "</span><span class=\"is-mono\">" + esc(vg) + "</span><span class=\"is-melding\">" + esc(r.melding || "") + "</span></div>";
                }).join("") : "<div class=\"gl-v2-pl-imp-leeg\">Geen regels met een aandachtspunt.</div>");
        }

        if (s === 2 && d) renderImpRes();

        if (s === 3 && d) {
            var nieuw = imp.doel === "nieuw";
            $("gl-v2-pl-imp-o-nieuw").hidden = !nieuw;
            $("gl-v2-pl-imp-o-huidig").hidden = nieuw;
            $("gl-v2-pl-imp-s-geen").hidden = nieuw;
            if (nieuw && imp.start === "geen") { imp.start = "bestand"; zetRadio("gl-v2-pl-imp-start", "bestand"); }
            $("gl-v2-pl-imp-naam").value = imp.naam;
            var vrij = (state.eenheden || []).filter(function (e) { return e.planningId == null; });
            $("gl-v2-pl-imp-eenheden").innerHTML = vrij.length ? vrij.map(function (e) {
                return "<label class=\"gl-v2-pl-een\"><input type=\"checkbox\" value=\"" + e.id + "\"" + (imp.eenheden.indexOf(e.id) >= 0 ? " checked" : "") + " /><span>" + esc(e.naam) + "</span><small>" + esc(e.groepNaam || "") + "</small></label>";
            }).join("") : "<span class=\"gl-v2-field-help\" style=\"padding:8px 12px\">Alle woningen en gebouwen zitten al in een planning.</span>";
            $("gl-v2-pl-imp-s-bestand-info").textContent = d.projectStart ? "Het project in het bestand start op " + fmtFull(d.projectStart) + "." : "Het bestand heeft geen startdatum; de planning start dan op de aanvangsdatum van het project.";
            $("gl-v2-pl-imp-o-datum").hidden = imp.start !== "nieuw";
        }

        if (s === 4 && imp.resultaat) {
            var r = imp.resultaat;
            $("gl-v2-pl-imp-resultaat").innerHTML = "<div class=\"gl-v2-pl-imp-ok\"><i class=\"ph ph-check-circle ph-fill\" aria-hidden=\"true\"></i><div><b>" + esc(r.titel) + "</b><span>" + esc(r.tekst) + "</span></div></div>" +
                (r.waarschuwingen.length ? "<div class=\"gl-v2-notice gl-v2-notice-compact is-warning\"><span class=\"gl-v2-notice-icon\"><i class=\"ph ph-warning\" aria-hidden=\"true\"></i></span><span class=\"gl-v2-notice-text\">" + esc(r.waarschuwingen.join(" ")) + "</span></div>" : "") +
                "<p class=\"gl-v2-pl-sj-uitleg\">Controleer de duur en de voorgangers: de datums zijn doorgerekend met de werkkalender van dit project, dus ze kunnen afwijken van die in MS Project.</p>";
        }
    }

    function impVolgende() {
        impFout("");
        if (imp.stap === 1) { imp.stap = impHeeftRes() ? 2 : 3; renderImp(); return; }
        if (imp.stap === 2) { imp.stap = 3; renderImp(); return; }
        if (imp.stap === 4) { var m = modal("gl-v2-pl-imp"); if (m) m.hide(); return; }
        if (imp.stap !== 3) return;

        var d = imp.data, nieuw = imp.doel === "nieuw";
        var opdracht = { uitvoerders: imp.koppelingen, rijen: d.rijen, nieuw: nieuw, startModus: imp.start, bestandStart: d.projectStart, modus: imp.modus };
        if (nieuw) {
            if (!imp.naam.trim()) { impFout("Geef de planning een naam."); return; }
            opdracht.naam = imp.naam.trim(); opdracht.eenheidIds = imp.eenheden;
        }
        if (imp.start === "nieuw") {
            var sd = getDate("gl-v2-pl-imp-startdatum");
            if (!sd) { impFout("Kies de nieuwe startdatum."); return; }
            opdracht.startDatum = sd;
        }
        var run = function () {
            imp.bezig = true; renderImp();
            var huidig = impHuidig();
            var klaar = send({ type: "importeren", planningId: nieuw || !huidig ? 0 : huidig.id, import: opdracht }, {
                silent: true,
                onError: function (f) { imp.bezig = false; renderImp(); impFout(f || "De import is niet gelukt."); },
                after: function (res) {
                    imp.bezig = false;
                    var n = d.aantalTaken + d.aantalMijlpalen;
                    var plan = (res.staat.planningen || []).filter(function (p) { return p.id === res.staat.actiefPlanningId; })[0];
                    imp.resultaat = {
                        titel: n + (n === 1 ? " taak" : " taken") + " en " + d.aantalFases + (d.aantalFases === 1 ? " fase" : " fases") + " geïmporteerd",
                        tekst: (plan ? "In de planning " + plan.naam + ". " : "") + d.aantalAfhankelijkheden + (d.aantalAfhankelijkheden === 1 ? " voorganger" : " voorgangers") + " overgenomen.",
                        waarschuwingen: res.waarschuwingen || []
                    };
                    imp.stap = 4; renderImp();
                }
            });
            // Een conflict (iemand wijzigde de planning intussen) of een fout laat de knop anders vergrendeld staan.
            if (klaar && klaar.then) klaar.then(function (gelukt) { if (!gelukt && imp.bezig) { imp.bezig = false; renderImp(); } });
        };
        if (!nieuw && imp.modus === "vervangen" && window.GlV2Bevestig) {
            var p = impHuidig();
            window.GlV2Bevestig({ title: "Huidige planning vervangen?", desc: "De fases en taken van " + (p ? p.naam : "deze planning") + " worden vervangen door de import. Dit kan je niet ongedaan maken.", ok: "Vervangen", tone: "danger", icon: "ph-arrows-clockwise" }).then(function (ok) { if (ok) run(); });
        } else run();
    }

    (function bedraadImport() {
        var drop = $("gl-v2-pl-imp-drop"), file = $("gl-v2-pl-imp-file"), el = $("gl-v2-pl-imp");
        if (!drop || !file || !el) return;
        $("gl-v2-pl-imp-kies").addEventListener("click", function () { file.click(); });
        file.addEventListener("change", function () { if (file.files && file.files[0]) impBestandKiezen(file.files[0]); });
        drop.addEventListener("dragover", function (e) { e.preventDefault(); drop.classList.add("is-over"); });
        drop.addEventListener("dragleave", function () { drop.classList.remove("is-over"); });
        drop.addEventListener("drop", function (e) { e.preventDefault(); drop.classList.remove("is-over"); if (e.dataTransfer.files && e.dataTransfer.files[0]) impBestandKiezen(e.dataTransfer.files[0]); });
        el.querySelector("form").addEventListener("submit", function (e) { e.preventDefault(); if (!imp.bezig && imp.stap > 0) impVolgende(); });
        $("gl-v2-pl-imp-ok").addEventListener("click", function () { if (!imp.bezig) impVolgende(); });
        $("gl-v2-pl-imp-terug").addEventListener("click", function () { impFout(""); imp.stap = imp.stap === 3 && !impHeeftRes() ? 1 : Math.max(0, imp.stap - 1); if (imp.stap === 0) { imp.data = null; file.value = ""; } renderImp(); });
        $("gl-v2-pl-imp-nogeen").addEventListener("click", function () { var h = imp.doel; impReset(); imp.doel = h; file.value = ""; renderImp(); });
        el.addEventListener("click", function (e) {
            if (e.target.closest("#gl-v2-pl-imp-ander")) { imp.stap = 0; imp.data = null; file.value = ""; renderImp(); return; }
            var f = e.target.closest("#gl-v2-pl-imp-filter [data-f]");
            if (f) { imp.filter = f.getAttribute("data-f"); renderImp(); }
        });
        el.addEventListener("change", function (e) {
            var t = e.target;
            if (t.classList && t.classList.contains("gl-v2-pl-imp-rsel")) {
                var rr = (imp.data.resources || [])[parseInt(t.getAttribute("data-n"), 10)];
                if (rr) { if (t.value) imp.koppelingen[rr.naam] = parseInt(t.value, 10); else delete imp.koppelingen[rr.naam]; }
                var gk = (imp.data.resources || []).filter(function (x) { return imp.koppelingen[x.naam] != null; }).length;
                $("gl-v2-pl-imp-res-info").textContent = gk + " van " + imp.data.resources.length + " resources gekoppeld";
                return;
            }
            if (t.name === "gl-v2-pl-imp-doel") { imp.doel = t.value; renderImp(); }
            else if (t.name === "gl-v2-pl-imp-modus") imp.modus = t.value;
            else if (t.name === "gl-v2-pl-imp-start") { imp.start = t.value; $("gl-v2-pl-imp-o-datum").hidden = imp.start !== "nieuw"; }
            else if (t.closest("#gl-v2-pl-imp-eenheden")) {
                imp.eenheden = Array.prototype.map.call($("gl-v2-pl-imp-eenheden").querySelectorAll("input:checked"), function (i) { return parseInt(i.value, 10); });
                // Eén woning gekozen en de naam nog niet aangepast: de planning krijgt de naam van die woning.
                if (!imp.naamAangepast && imp.eenheden.length === 1) {
                    var een = (state.eenheden || []).filter(function (x) { return x.id === imp.eenheden[0]; })[0];
                    if (een) { imp.naam = een.naam; $("gl-v2-pl-imp-naam").value = imp.naam; }
                }
            }
        });
        $("gl-v2-pl-imp-naam").addEventListener("input", function (e) { imp.naam = e.target.value; imp.naamAangepast = true; });
    })();

    function openSjBewaar() {
        var p = actief(); if (!canSjabloon || !p) return;
        $("gl-v2-pl-sj-bewaar-fout").hidden = true;
        $("gl-v2-pl-sj-bewaar-uitleg").textContent = "De fases, taken, duur en voorgangers van " + p.naam + " worden bewaard. Je kunt het sjabloon daarna op andere woningen of gebouwen toepassen. Voorgangers uit een andere planning nemen we niet mee.";
        $("gl-v2-pl-sj-naam").value = ""; $("gl-v2-pl-sj-besch").value = "";
        var m = modal("gl-v2-pl-sj-bewaar"); if (m) { m.show(); window.setTimeout(function () { $("gl-v2-pl-sj-naam").focus(); }, 300); }
    }
    $("gl-v2-pl-sj-bewaar").querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        var fout = $("gl-v2-pl-sj-bewaar-fout"); fout.hidden = true;
        var naam = $("gl-v2-pl-sj-naam").value.trim();
        if (!naam) { fout.textContent = "Geef het sjabloon een naam."; fout.hidden = false; return; }
        $("gl-v2-pl-sj-bewaar-ok").disabled = true;
        postJson(cfg.sjabloonBewaarUrl, { planningId: state.actiefPlanningId || 0, naam: naam, beschrijving: $("gl-v2-pl-sj-besch").value.trim() }).then(function (res) {
            $("gl-v2-pl-sj-bewaar-ok").disabled = false;
            if (!res.ok) { fout.textContent = res.fout || "Het sjabloon is niet bewaard."; fout.hidden = false; return; }
            var m = modal("gl-v2-pl-sj-bewaar"); if (m) m.hide();
            herlaadStaat();
            toast("success", "Sjabloon bewaard", res.blokken + (res.blokken === 1 ? " blok" : " blokken") + ", " + res.taken + (res.taken === 1 ? " taak" : " taken") + (res.overgeslagen ? ". " + res.overgeslagen + " voorganger(s) uit een andere planning zijn niet meegenomen." : "."));
        }).catch(function (err) { $("gl-v2-pl-sj-bewaar-ok").disabled = false; fout.textContent = err.message; fout.hidden = false; });
    });
    $("gl-v2-pl-tabs").addEventListener("click", function (e) {
        if (e.target.closest(".js-pl-sj-toepas")) openSjToepas();
        else if (e.target.closest(".js-pl-sj-bewaar")) openSjBewaar();
    });

    function updateBeperkingVeld() { showField("gl-v2-pl-f-bepdatum", getSelect("gl-v2-pl-f-bep") !== "0" && getSelect("gl-v2-pl-f-bep") !== ""); }
    function standaardVoorganger(faseId, naId, naSoort) {
        // Standaard: de taak erboven (de gekozen "na"-taak, anders de laatste taak in de fase)
        var kinderen = kinderenVan(faseId).filter(function (r) { return r.soort === "taak"; });
        if (naId && naSoort === "taak") { var r0 = rowByKey("t" + naId); return r0 ? r0.wbs : ""; }
        return kinderen.length ? kinderen[kinderen.length - 1].wbs : "";
    }

    // ── Koppeling met een trajectmijlpaal (optioneel; de mijlpaal in het Traject volgt dan de planning) ──
    function trajItems(sleutel) {
        return (state.trajectMijlpalen || []).map(function (m) {
            var tekst = m.naam + (m.faseNaam ? " · " + m.faseNaam : "");
            if (m.gekoppeldAanSleutel && m.gekoppeldAanSleutel !== sleutel) tekst += " (nu aan " + m.gekoppeldAanTekst + ")";
            return { value: String(m.id), text: tekst };
        });
    }
    function trajHuidig(sleutel) {
        var m = (state.trajectMijlpalen || []).filter(function (x) { return x.gekoppeldAanSleutel === sleutel; })[0];
        return m ? m.id : null;
    }
    // Toont de keuzelijst in het paneel (enkel bij bewerken, met Traject-schrijfrecht en als er trajectmijlpalen zijn)
    function toonTraj(wrapId, selectId, sleutel) {
        var wrap = $(wrapId), items = trajItems(sleutel);
        var aan = !!sleutel && canLink && items.length > 0;
        wrap.hidden = !aan;
        if (panel) panel.trajOud = aan ? trajHuidig(sleutel) : null;
        if (aan) setSelect(selectId, items, panel.trajOud != null ? String(panel.trajOud) : "", "Geen koppeling");
    }
    // Wat moet na het bewaren nog gekoppeld worden? null = niets veranderd of niet toegestaan.
    function koppelKeuze() {
        if (!panel || !panel.key) return null;
        var fase = panel.mode === "fase-bewerken";
        var wrap = $(fase ? "gl-v2-pl-wrap-trajf" : "gl-v2-pl-wrap-traj");
        if (!canLink || wrap.hidden) return null;
        var v = getSelect(fase ? "gl-v2-pl-f-trajf" : "gl-v2-pl-f-traj");
        var nieuw = v ? parseInt(v, 10) : null;
        if (nieuw === (panel.trajOud == null ? null : panel.trajOud)) return null;
        return { soort: fase ? "fase" : "taak", id: panel.id, mijlpaalId: nieuw, vorigeMijlpaalId: panel.trajOud == null ? null : panel.trajOud };
    }
    function koppelNa(kz) {
        if (!kz) return;
        fetch(cfg.koppelUrl, {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(Object.assign({ planningId: state.actiefPlanningId || 0 }, kz))
        }).then(function (r) {
            if (r.status === 403) throw new Error("Je hebt geen schrijfrecht op het Traject.");
            if (!r.ok) throw new Error("Koppelen mislukt. Probeer het zo opnieuw.");
            return r.json();
        }).then(function (res) {
            if (!res.ok) { toonFout("Koppelen mislukt", res.fout || "De koppeling is niet bewaard."); return; }
            state = res.staat; render();
            toast("success", kz.mijlpaalId ? "Mijlpaal gekoppeld" : "Koppeling weggehaald", kz.mijlpaalId ? "De mijlpaal in het Traject volgt nu de planning." : "De mijlpaal in het Traject volgt de planning niet meer.");
        }).catch(function (e) { toonFout("Koppelen mislukt", e.message || "Controleer je verbinding en probeer opnieuw."); });
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
        $("gl-v2-pl-wrap-traj").hidden = true; // een nieuwe taak heeft nog geen id om aan te koppelen
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
        $("gl-v2-pl-wrap-trajf").hidden = true;
        setSelect("gl-v2-pl-f-lot", (state.lots || []).map(function (g) { return { value: g.id, text: g.naam }; }), "", "Geen lot");
        $("gl-v2-pl-f-teltmee").checked = false; $("gl-v2-pl-f-teltmee").disabled = true;
        $("gl-v2-pl-panel-delete") && ($("gl-v2-pl-panel-delete").hidden = true);
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = "Fase toevoegen";
        showPanel("FASE", "Nieuwe fase", "Zoals een samenvattingstaak in MS Project");
    }

    // Estafette per taak: de schakelaar zet het voorstel (dezelfde taak in de vorige planning) in of uit het veld Voorgangers.
    function delenVan(tekst) { return (tekst || "").split(/[;,]/).map(function (x) { return x.trim(); }).filter(Boolean); }
    function sleutelVan(deel) { return deel.replace(/\s*:\s*/, ":").replace(/(FS|SS|FF|SF)?\s*([+-]\s*\d+)?\s*d?\s*$/i, "").toLowerCase(); }
    // Estafette: een keuzelijst met de andere plannings en chips voor wat al gekozen is (schaalt naar tientallen woningen).
    var estafetteVoorstellen = [];
    function estafetteStanden() {
        var gekozen = {}; delenVan($("gl-v2-pl-f-voor").value).forEach(function (d) { gekozen[sleutelVan(d)] = true; });
        var chips = "", opties = [];
        estafetteVoorstellen.forEach(function (v) {
            if (gekozen[v.verwijzing.toLowerCase()])
                chips += "<span class=\"gl-v2-pl-chip\">" + esc(v.planning) + "<button type=\"button\" data-ref=\"" + esc(v.verwijzing) + "\" aria-label=\"" + esc(v.planning) + " weghalen\"><i class=\"ph ph-x\" aria-hidden=\"true\"></i></button></span>";
            else opties.push({ value: v.verwijzing, text: v.planning });
        });
        $("gl-v2-pl-estafette-chips").innerHTML = chips;
        setSelect("gl-v2-pl-estafette-kies", opties, "");
        var kz = fieldOf("gl-v2-pl-estafette-kies"); if (kz) kz.hidden = opties.length === 0;
    }
    function updateEstafette(voorstellen) {
        estafetteVoorstellen = voorstellen || [];
        var wrap = $("gl-v2-pl-wrap-estafette");
        wrap.hidden = estafetteVoorstellen.length === 0;
        if (!wrap.hidden) estafetteStanden();
    }
    function estafetteZet(ref, aan) {
        var sl = ref.toLowerCase(), veld = $("gl-v2-pl-f-voor");
        var delen = delenVan(veld.value).filter(function (d) { return sleutelVan(d) !== sl; });
        if (aan) delen.push(ref);
        veld.value = delen.join("; ");
        if (panel) panel.voorDirty = true;
        estafetteStanden();
    }
    $("gl-v2-pl-estafette-kies").addEventListener("change", function () { if (this.value) estafetteZet(this.value, true); });
    $("gl-v2-pl-estafette-chips").addEventListener("click", function (e) { var b = e.target.closest("button"); if (b) estafetteZet(b.getAttribute("data-ref"), false); });
    $("gl-v2-pl-f-voor").addEventListener("input", estafetteStanden);

    // Keuzelijst: zoek een taak of fase in alle plannings en klik ze aan of uit. Het veld Voorgangers blijft de bron.
    function sluitKies() {
        var k = $("gl-v2-pl-kies"); if (!k) return;
        k.hidden = true; $("gl-v2-pl-kies-open").setAttribute("aria-expanded", "false");
    }
    function renderKies() {
        var lijst = $("gl-v2-pl-kies-lijst"), q = $("gl-v2-pl-kies-zoek").value.trim().toLowerCase();
        var gekozen = {}; delenVan($("gl-v2-pl-f-voor").value).forEach(function (d) { gekozen[sleutelVan(d)] = true; });
        var eigenId = panel && panel.mode === "taak-bewerken" ? panel.id : null;
        var scroll = lijst.scrollTop, html = "", groep = null, aantal = 0;
        (state.keuzes || []).forEach(function (k) {
            if (eigenId != null && k.taakId === eigenId) return;
            if (q && (k.naam.toLowerCase().indexOf(q) < 0 && k.wbs.toLowerCase().indexOf(q) !== 0 && k.verwijzing.toLowerCase().indexOf(q) < 0)) return;
            if (k.planningId !== groep) {
                groep = k.planningId;
                html += "<div class=\"gl-v2-pl-kies-groep\">" + esc(k.planning) + (k.isEigen ? " (deze planning)" : "") + "</div>";
            }
            var aan = !!gekozen[k.verwijzing.toLowerCase()]; aantal++;
            html += "<button type=\"button\" class=\"gl-v2-pl-kies-item" + (aan ? " is-gekozen" : "") + "\" role=\"option\" aria-selected=\"" + aan + "\" data-ref=\"" + esc(k.verwijzing) + "\">" +
                "<span class=\"gl-v2-pl-kies-wbs\">" + esc(k.wbs) + "</span>" +
                "<span class=\"gl-v2-pl-kies-naam\" style=\"padding-left:" + (k.isEigen ? Math.max(0, k.niveau - 1) * 12 : 0) + "px\">" + esc(k.naam) + "</span>" +
                (k.isFase ? "<span class=\"gl-v2-pl-kies-fase\">Fase</span>" : "") +
                (aan ? "<i class=\"ph ph-check\" aria-hidden=\"true\"></i>" : "") + "</button>";
        });
        lijst.innerHTML = aantal ? html : "<div class=\"gl-v2-pl-kies-leeg\">Niets gevonden.</div>";
        lijst.scrollTop = scroll;
    }
    $("gl-v2-pl-kies-open").addEventListener("click", function () {
        var k = $("gl-v2-pl-kies"), open = k.hidden;
        k.hidden = !open; this.setAttribute("aria-expanded", String(open));
        if (open) { $("gl-v2-pl-kies-zoek").value = ""; renderKies(); $("gl-v2-pl-kies-zoek").focus(); }
    });
    $("gl-v2-pl-kies-zoek").addEventListener("input", renderKies);
    $("gl-v2-pl-kies-zoek").addEventListener("keydown", function (e) { if (e.key === "Escape") { e.stopPropagation(); sluitKies(); } });
    $("gl-v2-pl-kies-lijst").addEventListener("click", function (e) {
        var b = e.target.closest(".gl-v2-pl-kies-item"); if (!b) return;
        var ref = b.getAttribute("data-ref"), s = ref.toLowerCase(), veld = $("gl-v2-pl-f-voor");
        var delen = delenVan(veld.value), had = delen.some(function (d) { return sleutelVan(d) === s; });
        delen = delen.filter(function (d) { return sleutelVan(d) !== s; });
        if (!had) delen.push(ref);
        veld.value = delen.join("; ");
        if (panel) panel.voorDirty = true;
        veld.dispatchEvent(new Event("input", { bubbles: true }));
        renderKies();
    });

    function openEdit(key) {
        var r = rowByKey(key); if (!r || !canWrite) return;
        if (r.soort === "fase") return openEditFase(r);
        panel = { mode: "taak-bewerken", key: key, id: r.id };
        taakVorm(true);
        $("gl-v2-pl-f-naam").value = r.naam; $("gl-v2-pl-f-duur").value = r.duur; $("gl-v2-pl-f-notitie").value = r.notitie || "";
        showField("gl-v2-pl-f-fase", false);
        $("gl-v2-pl-wrap-plaats").hidden = true;
        $("gl-v2-pl-f-voor").value = r.voorgangers || "";
        updateEstafette(r.estafetteVoorstellen);
        setSelect("gl-v2-pl-f-bep", BEPERKINGEN, String(r.beperking || 0)); setDate("gl-v2-pl-f-bepdatum", r.beperkingDatum); updateBeperkingVeld();
        setDate("gl-v2-pl-f-deadline", r.deadline);
        $("gl-v2-pl-wrap-werkelijk").hidden = false; setDate("gl-v2-pl-f-wstart", r.werkelijkStart); setDate("gl-v2-pl-f-weinde", r.werkelijkEinde);
        showField("gl-v2-pl-f-pct", true); $("gl-v2-pl-f-pct").value = r.voortgang;
        setSelect("gl-v2-pl-f-wie", (state.gebruikers || []).map(function (g) { return { value: g.id, text: g.naam }; }), r.verantwoordelijkeId || "", "Niemand");
        zetUitvoerderSelect("gl-v2-pl-f-uitv", r.uitvoerderId, r.uitvoerderGeerfd && r.uitvoerderNaam ? "Van de fase: " + r.uitvoerderNaam : "Geen");
        toonTraj("gl-v2-pl-wrap-traj", "gl-v2-pl-f-traj", key);

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
        zetUitvoerderSelect("gl-v2-pl-f-uitvf", r.uitvoerderId, r.uitvoerderGeerfd && r.uitvoerderNaam ? "Van de bovenliggende fase: " + r.uitvoerderNaam : "Geen");
        toonTraj("gl-v2-pl-wrap-trajf", "gl-v2-pl-f-trajf", r.sleutel);
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
        v.uitvoerder = getSelect("gl-v2-pl-f-uitv") ? parseInt(getSelect("gl-v2-pl-f-uitv"), 10) : null;
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
        } else if (panel.mode === "planning-nieuw" || panel.mode === "planning-bewerken") {
            var pv = { naam: naam, startDatum: getDate("gl-v2-pl-f-pstart") };
            if (!panel.isHoofd) pv.eenheden = Array.prototype.map.call($("gl-v2-pl-f-eenheden").querySelectorAll("input:checked"), function (i) { return parseInt(i.value, 10); });
            if (panel.mode === "planning-nieuw") send({ type: "planningToevoegen", planningId: 0, velden: pv }, { onError: onError, after: closePanel });
            else send({ type: "planningWijzig", planningId: panel.id, velden: pv }, { onError: onError, after: closePanel });
        } else if (panel.mode === "taak-bewerken") {
            var w = verzamelTaakVelden(false);
            if (isNaN(w.duur) || w.duur < 0) { formError("De duur moet een aantal werkdagen zijn (0 of meer)."); return; }
            if (w.beperking && !w.beperkingDatum) { formError("Kies een datum bij de beperking."); return; }
            var kzTaak = koppelKeuze();
            send({ type: "wijzig", soort: "taak", id: panel.id, velden: w }, { onError: onError, after: function () { koppelNa(kzTaak); closePanel(); } });
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
            var fv = { naam: naam, uitvoerder: getSelect("gl-v2-pl-f-uitvf") ? parseInt(getSelect("gl-v2-pl-f-uitvf"), 10) : null, lotId: getSelect("gl-v2-pl-f-lot") ? parseInt(getSelect("gl-v2-pl-f-lot"), 10) : null, teltMee: $("gl-v2-pl-f-teltmee").checked };
            var kzFase = koppelKeuze();
            send({ type: "wijzig", soort: "fase", id: panel.id, velden: fv }, { onError: onError, after: function () { koppelNa(kzFase); closePanel(); } });
        }
    }

    $("gl-v2-pl-form").addEventListener("submit", function (e) { e.preventDefault(); opslaan(false); });
    $("gl-v2-pl-panel-again").addEventListener("click", function () { opslaan(true); });
    $("gl-v2-pl-panel-cancel").addEventListener("click", closePanel);
    $("gl-v2-pl-panel-close").addEventListener("click", closePanel);
    $("gl-v2-pl-backdrop").addEventListener("click", closePanel);
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && panel && !document.querySelector(".gl-v2-select-panel.is-open")) closePanel(); });
    var delBtn = $("gl-v2-pl-panel-delete");
    if (delBtn) delBtn.addEventListener("click", function () {
        if (panel && panel.mode === "planning-bewerken") { var p = actief(); if (p && !p.isHoofd) vraagVerwijderPlanning(p); return; }
        if (panel && panel.key) { var r = rowByKey(panel.key); if (r) vraagVerwijder(r); }
    });

    // Knoppen in de kop en de lege staat
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-pl-add-taak")) { e.preventDefault(); openNieuwTaak({}); }
        else if (e.target.closest(".js-pl-add-fase")) { e.preventDefault(); openNieuwFase({}); }
    });

    render();
})();
