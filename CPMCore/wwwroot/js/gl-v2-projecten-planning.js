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
    var ui = { scale: "week", kritiek: false, basis: false, collapsed: {}, listW: 0, noticesOpen: false, mview: "week", mopen: {}, inklapGedaan: {} };
    try { var raw = window.localStorage.getItem(UI_KEY); if (raw) { var p = JSON.parse(raw); ui.inklapGedaan = p.inklapGedaan || {}; ui.scale = p.scale || ui.scale; ui.kritiek = !!p.kritiek; ui.basis = !!p.basis; ui.collapsed = p.collapsed || {}; ui.listW = +p.listW || 0; ui.noticesOpen = !!p.noticesOpen; ui.mview = p.mview === "lijst" || p.mview === "tijdlijn" ? p.mview : "week"; ui.mopen = p.mopen || {}; } } catch (e) { /* geen opslag: gewoon de standaard */ }
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
    var AANN = -2;              // pseudo-tab "Per aannemer": alle taken van één uitvoerder uit alle plannings (enkel lezen)
    var aannActief = false, aannData = null, aannSel = "";
    function renderTabs() {
        var host = $("gl-v2-pl-tabs"), lijst = state.planningen || [];
        if (lijst.length <= 1 && !canWriteBasis) { host.hidden = true; return; }
        host.hidden = false;
        // Links de plannings als schakelaar (59i), rechts de acties: Importeren, Sjablonen ▾ en Instellen. De lege hoofdplanning van een project met een bouwdeel-planning krijgt geen knop (tenzij ze net open staat).
        var seg = lijst.filter(function (p) { return !p.verborgen || p.id === state.actiefPlanningId; }).map(function (p) {
            var on = p.id === state.actiefPlanningId && !aannActief, n = (p.eenheidIds || []).length;
            return "<button type=\"button\" class=\"" + (on ? "is-active" : "") + "\" aria-pressed=\"" + (on ? "true" : "false") + "\" data-planning=\"" + p.id + "\" title=\"" + esc(p.eenhedenTekst ? "Voor: " + p.eenhedenTekst : (p.isHoofd ? "Geldt voor het hele project" : "Geen woningen gekoppeld")) + "\">" +
                "<i class=\"ph " + (p.isHoofd ? "ph-house-line" : "ph-buildings") + "\" aria-hidden=\"true\"></i><span>" + esc(p.naam) + "</span>" + (n > 0 ? "<em>" + n + "</em>" : "") + "</button>";
        }).join("");
        if (lijst.length > 1) {
            var gOn = state.actiefPlanningId === GLOBAAL && !aannActief;
            seg += "<button type=\"button\" class=\"" + (gOn ? "is-active" : "") + "\" aria-pressed=\"" + (gOn ? "true" : "false") + "\" data-planning=\"" + GLOBAAL + "\" title=\"Alle plannings samen in één overzicht (enkel lezen)\"><i class=\"ph ph-stack\" aria-hidden=\"true\"></i><span>Globaal</span></button>";
        }
        if (state.heeftPlanning) {
            seg += "<button type=\"button\" class=\"" + (aannActief ? "is-active" : "") + "\" aria-pressed=\"" + (aannActief ? "true" : "false") + "\" data-planning=\"" + AANN + "\" title=\"Welke taken moet welke aannemer wanneer uitvoeren (enkel lezen)\"><i class=\"ph ph-hard-hat\" aria-hidden=\"true\"></i><span>Per aannemer</span></button>";
        }
        var html = "<div class=\"gl-v2-segmented gl-v2-pl-segpl\" role=\"group\" aria-label=\"Plannings van dit project\">" + seg + "</div>";
        if (canWriteBasis) html += "<button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary js-pl-add-planning\" title=\"Een nieuwe planning maken\"><i class=\"ph ph-plus\" aria-hidden=\"true\"></i>Planning</button>";
        html += "<span class=\"gl-v2-pl-toolbar-spacer\"></span>";
        if (canWriteBasis) {
            html += "<button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary js-pl-import\" title=\"Een planning importeren uit MS Project (XML)\"><i class=\"ph ph-upload-simple\" aria-hidden=\"true\"></i>Importeren</button>";
            html += "<button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-sjmenu\" title=\"Een sjabloon toepassen of deze planning bewaren\"><i class=\"ph ph-copy\" aria-hidden=\"true\"></i>Sjablonen<i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>";
            if (state.actiefPlanningId !== GLOBAAL && !aannActief) html += "<button type=\"button\" class=\"gl-v2-icon-btn js-pl-edit-planning\" title=\"Instellen: naam, woningen en startdatum wijzigen, of de planning verwijderen\" aria-label=\"Planning instellen\"><i class=\"ph ph-gear-six\" aria-hidden=\"true\"></i></button>";
        }
        host.innerHTML = html;
    }
    // Menu "Sjablonen": Sjabloon, Projectsjabloon en Als sjabloon (gedeeld .gl-v2-menu; de shell opent het, wij vullen het eerst in de capture-fase)
    function sjMenuItem(act, icon, label, sub) {
        return "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-menu=\"" + act + "\"><i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span>" + (sub ? "<span class=\"gl-v2-menu-item-sub\">" + esc(sub) + "</span>" : "") + "</span></button>";
    }
    document.addEventListener("click", function (e) {
        var tr = e.target.closest(".js-gl-v2-menu-trigger[aria-controls=\"gl-v2-pl-sjmenu\"]"); if (!tr) return;
        var act = actief(), h = sjMenuItem("sj", "ph-copy", "Sjabloon", "Een planningssjabloon toepassen op woningen of gebouwen") + sjMenuItem("pj", "ph-buildings", "Projectsjabloon", "Planning, traject en schijven in één keer, aangepast aan het gebouw");
        if (canSjabloon && act && act.aantalTaken && state.actiefPlanningId !== GLOBAAL && !aannActief) h += sjMenuItem("bewaar", "ph-floppy-disk", "Als sjabloon", "Deze planning bewaren als sjabloon");
        $("gl-v2-pl-sjmenu").innerHTML = h;
    }, true);
    $("gl-v2-pl-sjmenu").addEventListener("click", function (e) {
        var b = e.target.closest("[data-menu]"); if (!b) return;
        var a = b.getAttribute("data-menu");
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        if (a === "sj") openSjToepas(); else if (a === "pj") openPj(); else if (a === "bewaar") openSjBewaar();
    });
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
        if (id === AANN) { openAann(); return; }
        if (aannActief) { aannActief = false; if (id === state.actiefPlanningId) { render(); return; } }
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

    // ── Per aannemer: alle taken van één uitvoerder uit alle plannings ──
    function openAann() {
        fetch(cfg.staatUrl + (cfg.staatUrl.indexOf("?") < 0 ? "?" : "&") + "planningId=" + GLOBAAL, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("Het overzicht kon niet geladen worden."); return r.json(); })
            .then(function (s) { aannData = s; aannActief = true; aannSel = ""; aann.sel = null; aann.open = false; aann.alle0 = false; render(); })
            .catch(function (e) { toast("danger", "Laden mislukt", e.message || "Controleer je verbinding en probeer opnieuw."); });
    }
    // ── Per aannemer (59e/59f): dezelfde planning, gegroepeerd per uitvoerder, als Gantt ──
    var aann = { sel: null, modus: "taak", schaal: "week", voorgaand: true, vanaf: false, zoek: "", open: false, andere: false, alle0: false };
    var AANN_MAX0 = 8, MND = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    function fmtDM(iso) { return iso ? iso.slice(8, 10) + "/" + iso.slice(5, 7) : "—"; }
    function isoWeek(n) { var d = new Date(n * DAG), dag = (d.getUTCDay() + 6) % 7; d.setUTCDate(d.getUTCDate() - dag + 3); var j = new Date(Date.UTC(d.getUTCFullYear(), 0, 4)); return 1 + Math.round(((d - j) / DAG - 3 + ((j.getUTCDay() + 6) % 7)) / 7); }
    // Groepen per effectieve uitvoerder, taken (geen fases) op volgorde van start
    function aannGroepen() {
        var rijen = (aannData && aannData.rijen) || [], planning = "", fases = [], groepen = {}, volgorde = [], byKey = {};
        var plId = {}; (state.planningen || []).forEach(function (p) { plId[p.naam] = p.id; });
        rijen.forEach(function (r) {
            if (r.soort === "fase") {
                fases.length = r.niveau; fases[r.niveau] = r.naam;
                if (r.niveau === 0) planning = r.naam;
                return;
            }
            var sl = r.uitvoerderEffectiefId == null ? "0" : String(r.uitvoerderEffectiefId);
            if (!groepen[sl]) { groepen[sl] = { id: sl, naam: r.uitvoerderNaam || "Geen uitvoerder", taken: [] }; volgorde.push(sl); }
            var fase = fases.length > 1 ? fases[fases.length - 1] : "";
            var t = { id: r.id, sleutel: r.sleutel, naam: r.naam, wbs: r.wbs, planning: planning, planningId: plId[planning], fase: fase === planning ? "" : fase, start: r.start, einde: r.einde, duur: r.duur, mijlpaal: r.isMijlpaal, voortgang: r.voortgang, afgerond: !!r.afgerond, links: r.voorgangerLinks || [], lot: r.lotNaam || "" };
            groepen[sl].taken.push(t); byKey[r.sleutel] = { t: t, g: groepen[sl] };
        });
        var lijst = volgorde.map(function (k) {
            var g = groepen[k];
            g.taken.sort(function (a, b) { return (a.start || "") < (b.start || "") ? -1 : (a.start || "") > (b.start || "") ? 1 : 0; });
            g.start = g.taken.reduce(function (m, t) { return t.start && (!m || t.start < m) ? t.start : m; }, null);
            g.einde = g.taken.reduce(function (m, t) { return t.einde && (!m || t.einde > m) ? t.einde : m; }, null);
            return g;
        });
        // zonder uitvoerder helemaal achteraan
        lijst.sort(function (a, b) { return (a.id === "0") - (b.id === "0") || a.naam.localeCompare(b.naam, "nl"); });
        var k = 0; lijst.forEach(function (g) { g.k = g.id === "0" ? 0 : (k++ % 8) + 1; g.contract = contractVan(g.id); });
        lijst.byKey = byKey;
        return lijst;
    }
    function contractVan(id) {
        if (id === "0") return null;
        var u = ((aannData && aannData.uitvoerders) || state.uitvoerders || []).filter(function (x) { return String(x.id) === String(id); })[0];
        return u && u.inProject ? (u.contractStatus || "niet getekend") : "geen contract";
    }
    function contractChip(g) {
        if (!g.contract) return "";
        var c = g.contract === "getekend" ? "g" : g.contract === "geen contract" ? "red" : "gold";
        return "<span class=\"gl-v2-pa-chip " + c + "\">" + (g.contract === "getekend" ? "contract getekend" : g.contract === "niet getekend" ? "niet getekend" : "geen contract") + "</span>";
    }
    function aannZichtbaar(groepen) {
        var s = aann.sel; return groepen.filter(function (g) { return s ? s.indexOf(g.id) >= 0 : true; });
    }
    function aannTabel(g, meerPlannings) {
        return "<table class=\"gl-v2-pl-aann-tabel\"><thead><tr><th>Taak</th>" + (meerPlannings ? "<th>Planning</th>" : "") + "<th>Fase</th><th>Start</th><th>Einde</th><th class=\"is-num\">Duur</th></tr></thead><tbody>" +
            g.taken.map(function (t) {
                return "<tr><td><span class=\"is-mono\">" + esc(t.wbs) + "</span> " + esc(t.naam) + "</td>" + (meerPlannings ? "<td>" + esc(t.planning) + "</td>" : "") + "<td>" + esc(t.fase) + "</td><td>" + fmtFull(t.start) + "</td><td>" + fmtFull(t.einde) + "</td><td class=\"is-num\">" + (t.mijlpaal ? "mijlpaal" : dagen(t.duur)) + "</td></tr>";
            }).join("") + "</tbody></table>";
    }
    function aannStatus(t, today) { return t.afgerond ? "done" : (t.start && t.start <= today && t.einde >= today ? "now" : "plan"); }
    function aannTaakIcoon(st) { return "<span class=\"gl-v2-pa-st is-" + st + "\">" + (st === "done" ? "<i class=\"ph ph-check\" aria-hidden=\"true\"></i>" : "") + "</span>"; }
    // Selectielijst onder het veld Aannemers: eerst wie taken heeft in deze planning, dan de andere leveranciers van het project
    function aannDropdownHtml(groepen) {
        var zoek = aann.zoek.trim().toLowerCase(), sel = aann.sel;
        var met = groepen.filter(function (g) { return !zoek || g.naam.toLowerCase().indexOf(zoek) >= 0; });
        var ids = groepen.map(function (g) { return g.id; });
        var zonder = ((aannData && aannData.uitvoerders) || state.uitvoerders || []).filter(function (u) { return u.inProject && ids.indexOf(String(u.id)) < 0 && (!zoek || u.naam.toLowerCase().indexOf(zoek) >= 0); });
        var alleZonder = ((aannData && aannData.uitvoerders) || state.uitvoerders || []).filter(function (u) { return u.inProject && ids.indexOf(String(u.id)) < 0; }).length;
        var h = "<div class=\"gl-v2-pa-dg\">MET TAKEN IN DEZE PLANNING</div>";
        met.forEach(function (g) {
            var aan = !sel || sel.indexOf(g.id) >= 0;
            h += "<div class=\"gl-v2-pa-do\"><span class=\"gl-v2-check\"><input type=\"checkbox\" id=\"gl-v2-aa-" + esc(g.id) + "\" data-aann-opt=\"" + esc(g.id) + "\"" + (aan ? " checked" : "") + " /><label for=\"gl-v2-aa-" + esc(g.id) + "\"></label></span><label class=\"gl-v2-pa-dl\" for=\"gl-v2-aa-" + esc(g.id) + "\"><span class=\"gl-v2-pa-sw c" + g.k + "\"></span><span class=\"gl-v2-pa-dn\"><b>" + esc(g.naam) + "</b><small>" + g.taken.length + (g.taken.length === 1 ? " taak" : " taken") + (g.start ? " · " + fmtDM(g.start) + " – " + fmtDM(g.einde) : "") + "</small></span></label></div>";
        });
        if (!met.length) h += "<div class=\"gl-v2-pa-leeg\">Geen aannemer gevonden.</div>";
        if (zonder.length) {
            h += "<div class=\"gl-v2-pa-dg\">ZONDER TAKEN · " + alleZonder + " LEVERANCIERS VAN DIT PROJECT</div>";
            (aann.andere || zoek ? zonder : zonder.slice(0, 5)).forEach(function (u) {
                h += "<div class=\"gl-v2-pa-do is-off\"><span class=\"gl-v2-pa-cb\"></span><span class=\"gl-v2-pa-dn\"><b>" + esc(u.naam) + "</b><small>0 taken · wijs taken toe in de planning</small></span></div>";
            });
            if (!aann.andere && !zoek && zonder.length > 5) h += "<button type=\"button\" class=\"gl-v2-pa-dg is-knop\" data-aann-andere>+ " + (zonder.length - 5) + " andere</button>";
        }
        return h;
    }
    function aannPickerLabel(groepen, zichtbaar) {
        if (zichtbaar.length === 1) return "<span class=\"gl-v2-pa-sw c" + zichtbaar[0].k + "\"></span><b>" + esc(zichtbaar[0].naam) + "</b>";
        return "<b>" + zichtbaar.length + " van " + groepen.length + "</b><span class=\"gl-v2-pa-plus\">met taken</span>";
    }
    function renderAann() {
        var host = $("gl-v2-pl-aann"), groepen = aannGroepen(), meer = (state.planningen || []).length > 1, today = todayIso();
        if (aann.sel) aann.sel = aann.sel.filter(function (id) { return groepen.some(function (g) { return g.id === id; }); });
        var zichtbaar = aannZichtbaar(groepen), lane = aann.modus === "rij";
        if (lane) zichtbaar = zichtbaar.filter(function (g) { return g.id !== "0"; });
        var act = actief(), nBouw = act && act.aantalTaken ? act.aantalTaken : 0;
        var heeftUitv = groepen.some(function (g) { return g.id !== "0"; });
        var schalen = lane ? [["week", "Week"], ["maand", "Maand"], ["alles", "Alles"]] : [["dag", "Dag"], ["week", "Week"], ["maand", "Maand"], ["alles", "Alles"]];
        if (!schalen.some(function (s) { return s[0] === aann.schaal; })) aann.schaal = "alles";
        var kop =
            "<div class=\"gl-v2-pa-vw\"><div class=\"gl-v2-segmented\" role=\"group\" aria-label=\"Weergave\"><button type=\"button\" data-aann-terug>Bouwplanning" + (nBouw ? "<em>" + nBouw + "</em>" : "") + "</button><button type=\"button\" class=\"is-active\" aria-pressed=\"true\">Per aannemer</button></div>" +
            "<span class=\"gl-v2-pa-vt\">Per aannemer is dezelfde planning, gegroepeerd per uitvoerder. Wijzigen kan hier ook: het past de bouwplanning aan.</span></div>";
        var tool =
            "<div class=\"gl-v2-pa-tool\"><div class=\"gl-v2-pa-pickw\"><button type=\"button\" class=\"gl-v2-pa-pick" + (aann.open ? " is-open" : "") + "\" data-aann-pick aria-haspopup=\"listbox\" aria-expanded=\"" + aann.open + "\"><span class=\"gl-v2-pa-pl\">Aannemers</span>" + aannPickerLabel(groepen, aannZichtbaar(groepen)) + "<i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>" +
            (aann.open ? "<div class=\"gl-v2-pa-dd\" role=\"listbox\" aria-multiselectable=\"true\"><label class=\"gl-v2-pa-ds\"><i class=\"ph ph-magnifying-glass\" aria-hidden=\"true\"></i><input type=\"search\" data-aann-zoek value=\"" + esc(aann.zoek) + "\" placeholder=\"Zoek aannemer\" autocomplete=\"off\" aria-label=\"Zoek aannemer\" /></label><div id=\"gl-v2-pa-dd-lijst\">" + aannDropdownHtml(groepen) + "</div><div class=\"gl-v2-pa-df\"><button type=\"button\" class=\"gl-v2-pa-lnk\" data-aann-alles>Alles</button><button type=\"button\" class=\"gl-v2-pa-lnk\" data-aann-geen>Geen</button><span style=\"flex:1\"></span><button type=\"button\" class=\"gl-v2-btn gl-v2-btn-primary\" data-aann-klaar>Tonen (" + aannZichtbaar(groepen).length + ")</button></div></div>" : "") + "</div>" +
            "<div class=\"gl-v2-segmented is-sm\" role=\"group\" aria-label=\"Weergave van de taken\"><button type=\"button\" data-aann-modus=\"taak\" class=\"" + (lane ? "" : "is-active") + "\">Per taak</button><button type=\"button\" data-aann-modus=\"rij\" class=\"" + (lane ? "is-active" : "") + "\">1 rij per aannemer</button></div>" +
            "<div class=\"gl-v2-segmented is-sm\" role=\"group\" aria-label=\"Tijdschaal\">" + schalen.map(function (s) { return "<button type=\"button\" data-aann-schaal=\"" + s[0] + "\" class=\"" + (aann.schaal === s[0] ? "is-active" : "") + "\">" + s[1] + "</button>"; }).join("") + "</div>" +
            (lane ? "" : "<label class=\"gl-v2-pa-tg\"><input type=\"checkbox\" class=\"gl-v2-toggle-switch\" data-aann-tg=\"voorgaand\"" + (aann.voorgaand ? " checked" : "") + " />Voorgaande taken van anderen tonen</label><label class=\"gl-v2-pa-tg\"><input type=\"checkbox\" class=\"gl-v2-toggle-switch\" data-aann-tg=\"vanaf\"" + (aann.vanaf ? " checked" : "") + " />Enkel vanaf vandaag</label>") +
            "<span style=\"flex:1\"></span><button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary\" id=\"gl-v2-pl-aann-print\"><i class=\"ph ph-printer\" aria-hidden=\"true\"></i>Afdrukken</button></div>";
        var melding = heeftUitv ? "" : "<div class=\"gl-v2-notice gl-v2-notice-compact is-info\"><span class=\"gl-v2-notice-icon\"><i class=\"ph ph-info\" aria-hidden=\"true\"></i></span><span class=\"gl-v2-notice-text\">Nog geen taak heeft een uitvoerder. Kies een uitvoerder bij een fase of taak (of koppel resources bij het importeren), dan verschijnt hier de planning per aannemer.</span></div>";
        host.innerHTML = kop + "<section class=\"gl-v2-card gl-v2-pa-card\">" + tool + melding + (zichtbaar.length ? aannChart(groepen, zichtbaar, lane, today, host) : "<p class=\"gl-v2-pa-leeg\">Kies minstens één aannemer.</p>") + "</section>";
    }
    // De grafiek: elke rij heeft links een vaste kolom en rechts de balken op een gemeenschappelijke tijdas
    function aannChart(groepen, zichtbaar, lane, today, host) {
        var DAYMS = DAG, tn = isoN(today);
        var taken = []; zichtbaar.forEach(function (g) { g.taken.forEach(function (t) { if (t.start && t.einde) taken.push(t); }); });
        if (!taken.length) return "<p class=\"gl-v2-pa-leeg\">De gekozen aannemers hebben nog geen taken met datums.</p>";
        var minN = Math.min.apply(null, taken.map(function (t) { return isoN(t.start); })), maxN = Math.max.apply(null, taken.map(function (t) { return isoN(t.einde); }));
        if (!lane && aann.voorgaand) { /* voorgaande taken van anderen vallen binnen de planning: de as blijft hetzelfde */ }
        var labelW = lane ? 380 : 560, dagen_ = maxN - minN + 1;
        var px = aann.schaal === "dag" ? 22 : aann.schaal === "week" ? 5 : aann.schaal === "maand" ? 1.6 : Math.max(0.8, (Math.max(520, host.clientWidth - labelW - 44)) / (dagen_ + 14));
        var W = Math.round((dagen_ + 14) * px), x = function (n) { return (n - minN) * px; };
        // as: maanden (en weken zolang ze leesbaar zijn)
        var m1 = new Date(minN * DAYMS), y = m1.getUTCFullYear(), mo = m1.getUTCMonth(), maanden = "", weken = "";
        for (var i = 0; i < 240; i++) {
            var sN = Math.round(Date.UTC(y, mo, 1) / DAYMS), eN = Math.round(Date.UTC(y, mo + 1, 1) / DAYMS); if (sN > maxN + 14) break;
            var l = Math.max(0, x(sN)), r = Math.min(W, x(eN));
            if (r > l) maanden += "<span style=\"left:" + l.toFixed(1) + "px;width:" + (r - l).toFixed(1) + "px\">" + MND[mo % 12] + (mo === 0 || i === 0 ? " " + y : "") + "</span>";
            mo++; if (mo > 11) { mo = 0; y++; }
        }
        if (px * 7 >= 22) for (var n = minN; n <= maxN + 14; n++) if ((new Date(n * DAYMS).getUTCDay() + 6) % 7 === 0) weken += "<i style=\"left:" + x(n).toFixed(1) + "px\"><b>w" + isoWeek(n) + "</b></i>";
        var kopRij = "<div class=\"gl-v2-pa-r is-h\"><div class=\"gl-v2-pa-l\" style=\"width:" + labelW + "px\"><span class=\"gl-v2-pa-hn\">" + (lane ? "Aannemer" : "Taak") + "</span>" + (lane ? "" : "<span class=\"gl-v2-pa-f\">Fase</span><span class=\"gl-v2-pa-d\">Start</span><span class=\"gl-v2-pa-d\">Einde</span><span class=\"gl-v2-pa-du\">Duur</span>") + "</div><div class=\"gl-v2-pa-ax\" style=\"width:" + W + "px\"><div class=\"gl-v2-pa-mh\">" + maanden + "</div><div class=\"gl-v2-pa-wk\">" + weken + "</div></div></div>";
        var rijen = "";
        if (lane) {
            var blokken = {};
            zichtbaar.forEach(function (g) {
                var bl = [];
                g.taken.forEach(function (t) { var s = isoN(t.start), e = isoN(t.einde), laatste = bl[bl.length - 1]; if (laatste && s - laatste.e <= 5) { laatste.e = Math.max(laatste.e, e); laatste.t.push(t); } else bl.push({ s: s, e: e, t: [t] }); });
                blokken[g.id] = bl;
                var volgende = bl.filter(function (b) { return b.s >= tn; })[0], eerste = g.taken[0];
                rijen += "<div class=\"gl-v2-pa-r is-lane\"><div class=\"gl-v2-pa-l\" style=\"width:" + labelW + "px\"><span class=\"gl-v2-pa-sw c" + g.k + "\"></span><span class=\"gl-v2-pa-ln\"><b>" + esc(g.naam) + "</b><small>" + esc((eerste && eerste.fase ? eerste.fase + " · " : "") + bl.length + (bl.length === 1 ? " blok" : " blokken") + (volgende ? " · volgende: " + fmtDM(nIso(volgende.s)) : " · afgerond")) + "</small></span>" + contractChip(g) + "</div><div class=\"gl-v2-pa-g\" style=\"width:" + W + "px\">" +
                    bl.map(function (b) { return "<button type=\"button\" class=\"gl-v2-pa-bar c" + g.k + "\" style=\"left:" + x(b.s).toFixed(1) + "px;width:" + Math.max(5, (b.e - b.s + 1) * px).toFixed(1) + "px\" data-aann-blok=\"" + esc(g.id) + "\" title=\"" + esc(g.naam + ": " + b.t.length + (b.t.length === 1 ? " taak" : " taken") + " van " + fmtDM(nIso(b.s)) + " tot " + fmtDM(nIso(b.e))) + "\" aria-label=\"" + esc(g.naam + ", blok van " + fmtDM(nIso(b.s)) + " tot " + fmtDM(nIso(b.e))) + "\"></button>"; }).join("") + "</div></div>";
            });
            // Gouden zone: drie of meer aannemers tegelijk op de werf
            var tel = {}; zichtbaar.forEach(function (g) { blokken[g.id].forEach(function (b) { for (var d = b.s; d <= b.e; d++) { tel[d] = tel[d] || {}; tel[d][g.id] = (tel[d][g.id] || []).concat(b.t.filter(function (t) { return isoN(t.start) <= d && isoN(t.einde) >= d; })); } }); });
            var zones = [], cur = null;
            for (var d2 = minN; d2 <= maxN + 1; d2++) {
                var c = tel[d2] ? Object.keys(tel[d2]).length : 0;
                if (c >= 3) { if (!cur) cur = { s: d2, e: d2, max: c, fases: {} }; cur.e = d2; cur.max = Math.max(cur.max, c); Object.keys(tel[d2]).forEach(function (id) { tel[d2][id].forEach(function (t) { if (t.fase) cur.fases[t.fase] = (cur.fases[t.fase] || 0) + 1; }); }); }
                else if (cur) { if (cur.e - cur.s >= 4) zones.push(cur); cur = null; }
            }
            var zoneHtml = zones.map(function (zn) { var top = Object.keys(zn.fases).sort(function (p, q) { return zn.fases[q] - zn.fases[p]; })[0]; return "<span class=\"gl-v2-pa-ov\" style=\"left:" + (labelW + x(zn.s)).toFixed(1) + "px;width:" + Math.max(20, (zn.e - zn.s + 1) * px).toFixed(1) + "px\"><b>" + zn.max + " aannemers tegelijk" + (top ? " · " + esc(top.toLowerCase()) : "") + "</b></span>"; }).join("");
            rijen = "<div class=\"gl-v2-pa-rows\">" + todayLijn(tn, labelW, x, minN, maxN) + zoneHtml + rijen + "</div>";
        } else {
            var uit = "";
            zichtbaar.forEach(function (g) {
                var sum = g.start ? "<span class=\"gl-v2-pa-sum c" + g.k + "\" style=\"left:" + x(isoN(g.start)).toFixed(1) + "px;width:" + Math.max(6, (isoN(g.einde) - isoN(g.start) + 1) * px).toFixed(1) + "px\"></span>" : "";
                var wd = g.taken.reduce(function (s, t) { return s + (t.mijlpaal ? 0 : (t.duur || 0)); }, 0);
                uit += "<div class=\"gl-v2-pa-r is-grp\"><div class=\"gl-v2-pa-l\" style=\"width:" + labelW + "px\"><span class=\"gl-v2-pa-sw c" + g.k + "\"></span><span class=\"gl-v2-pa-gn\"><b>" + esc(g.naam) + "</b><span class=\"gl-v2-pa-gs\">" + g.taken.length + (g.taken.length === 1 ? " taak" : " taken") + (g.start ? " · " + fmtDM(g.start) + " – " + fmtDM(g.einde) + " · " + wd + " werkdagen" : "") + "</span></span>" + contractChip(g) +
                    (g.id === "0" && canWriteBasis ? "<button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary gl-v2-pa-btn\" data-aann-toewijs>Toewijzen</button>" : "") + "</div><div class=\"gl-v2-pa-g\" style=\"width:" + W + "px\">" + sum + "</div></div>";
                var lijst = g.taken.filter(function (t) { return t.start && t.einde && (!aann.vanaf || t.einde >= today); });
                var verborgen = 0;
                if (g.id === "0" && !aann.alle0 && lijst.length > AANN_MAX0) { verborgen = lijst.length - AANN_MAX0; lijst = lijst.slice(0, AANN_MAX0); }
                var getoond = {};
                lijst.forEach(function (t) {
                    if (aann.voorgaand) t.links.forEach(function (l) {
                        var v = groepen.byKey["t" + l.taakId]; if (!v || v.g.id === g.id || getoond[v.t.sleutel] || !v.t.start) return; getoond[v.t.sleutel] = true;
                        var ve = isoN(v.t.einde);
                        uit += "<div class=\"gl-v2-pa-r is-ctx\"><div class=\"gl-v2-pa-l\" style=\"width:" + labelW + "px\">" + "<span class=\"gl-v2-pa-st is-ctx\"><i class=\"ph ph-arrow-bend-down-right\" aria-hidden=\"true\"></i></span><code>" + esc(v.t.wbs) + "</code><span class=\"gl-v2-pa-tn\">" + esc(v.t.naam) + "</span><span class=\"gl-v2-pa-f\">vóór " + esc(t.wbs) + " · " + esc(v.g.naam) + "</span><span class=\"gl-v2-pa-d\">" + fmtDM(v.t.start) + "</span><span class=\"gl-v2-pa-d\">" + fmtDM(v.t.einde) + "</span><span class=\"gl-v2-pa-du\">" + (v.t.mijlpaal ? "◆" : (v.t.duur || 0) + " d") + "</span></div><div class=\"gl-v2-pa-g\" style=\"width:" + W + "px\"><span class=\"gl-v2-pa-bar is-ctx\" style=\"left:" + x(isoN(v.t.start)).toFixed(1) + "px;width:" + Math.max(5, (ve - isoN(v.t.start) + 1) * px).toFixed(1) + "px\"></span><span class=\"gl-v2-pa-arr\" style=\"left:" + (x(ve + 1)).toFixed(1) + "px\"></span></div></div>";
                    });
                    var st = aannStatus(t, today), s = isoN(t.start), e = isoN(t.einde);
                    uit += "<div class=\"gl-v2-pa-r is-" + st + (g.id === "0" ? " is-grey" : "") + "\"><div class=\"gl-v2-pa-l\" style=\"width:" + labelW + "px\">" + aannTaakIcoon(st) + "<code>" + esc(t.wbs) + "</code><span class=\"gl-v2-pa-tn\">" + esc(t.naam) + "</span><span class=\"gl-v2-pa-f\" title=\"" + esc((t.fase || "") + (t.lot ? " · " + t.lot : "")) + "\">" + esc((t.fase || t.planning || "") + (t.lot ? " · " + t.lot : "")) + "</span><span class=\"gl-v2-pa-d\">" + fmtDM(t.start) + "</span><span class=\"gl-v2-pa-d" + (st === "plan" && t.einde < today ? " is-late" : "") + "\">" + fmtDM(t.einde) + "</span><span class=\"gl-v2-pa-du\">" + (t.mijlpaal ? "◆" : (t.duur || 0) + " d") + "</span>" +
                        (g.id === "0" && canWriteBasis ? "<button type=\"button\" class=\"gl-v2-pa-asg js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-aannmenu\" data-aann-taak=\"" + esc(t.sleutel) + "\">Kiezen<i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>" : "") + "</div><div class=\"gl-v2-pa-g\" style=\"width:" + W + "px\"><span class=\"gl-v2-pa-bar is-" + st + " c" + g.k + "\" style=\"left:" + x(s).toFixed(1) + "px;width:" + Math.max(5, (e - s + 1) * px).toFixed(1) + "px;--vg:" + (t.voortgang || 0) + "%\">" + (g.id === "0" ? "" : "<em>" + esc(t.naam) + "</em>") + "</span></div></div>";
                });
                if (verborgen) uit += "<div class=\"gl-v2-pa-more\">nog " + verborgen + " taken zonder uitvoerder · <button type=\"button\" class=\"gl-v2-pa-lnk\" data-aann-alle0>alles tonen</button></div>";
            });
            rijen = "<div class=\"gl-v2-pa-rows\">" + todayLijn(tn, labelW, x, minN, maxN) + uit + "</div>";
        }
        var legende = lane ? "<span>Elk blok = aaneensluitende taken van die aannemer. Klik een blok: de taken staan dan in Per taak.</span>"
            : "<span><i class=\"gl-v2-pa-lg is-done\"></i>klaar</span><span><i class=\"gl-v2-pa-lg is-now\"></i>bezig</span><span><i class=\"gl-v2-pa-lg is-plan\"></i>gepland</span><span><i class=\"gl-v2-pa-lg is-ctx\"></i>voorgaande taak van een ander</span><span><i class=\"gl-v2-pa-lg is-today\"></i>vandaag</span>";
        return "<div class=\"gl-v2-pa-gantt\" id=\"gl-v2-pa-gantt\">" + kopRij + rijen + "</div><div class=\"gl-v2-pa-leg\">" + legende + "</div>";
    }
    function todayLijn(tn, labelW, x, minN, maxN) {
        if (tn < minN || tn > maxN + 14) return "";
        return "<span class=\"gl-v2-pa-tdy\" style=\"left:" + (labelW + x(tn)).toFixed(1) + "px\"><b>vandaag</b></span>";
    }
    function aannRefresh() { fetch(cfg.staatUrl + (cfg.staatUrl.indexOf("?") < 0 ? "?" : "&") + "planningId=" + GLOBAAL, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (s) { aannData = s; render(); }); }
    $("gl-v2-pl-aann").addEventListener("click", function (e) {
        var t;
        if (e.target.closest("[data-aann-terug]")) { aannActief = false; render(); return; }
        if (e.target.closest("[data-aann-pick]")) { aann.open = !aann.open; renderAann(); if (aann.open) { var z = $("gl-v2-pl-aann").querySelector("[data-aann-zoek]"); if (z) z.focus(); } return; }
        if (e.target.closest("[data-aann-klaar]")) { aann.open = false; renderAann(); return; }
        if (e.target.closest("[data-aann-alles]")) { aann.sel = null; renderAann(); return; }
        if (e.target.closest("[data-aann-geen]")) { aann.sel = []; renderAann(); return; }
        if (e.target.closest("[data-aann-andere]")) { aann.andere = true; $("gl-v2-pa-dd-lijst").innerHTML = aannDropdownHtml(aannGroepen()); return; }
        if (e.target.closest("[data-aann-alle0]")) { aann.alle0 = true; renderAann(); return; }
        if ((t = e.target.closest("[data-aann-modus]"))) { aann.modus = t.getAttribute("data-aann-modus"); renderAann(); return; }
        if ((t = e.target.closest("[data-aann-schaal]"))) { aann.schaal = t.getAttribute("data-aann-schaal"); renderAann(); return; }
        if ((t = e.target.closest("[data-aann-blok]"))) { aann.sel = [t.getAttribute("data-aann-blok")]; aann.modus = "taak"; renderAann(); return; }
        if (e.target.closest("[data-aann-toewijs]")) { openVraag("toewijzen"); return; }
        if (e.target.closest("#gl-v2-pl-aann-print")) aannAfdrukken();
    });
    $("gl-v2-pl-aann").addEventListener("change", function (e) {
        var t;
        if ((t = e.target.closest("[data-aann-tg]"))) { aann[t.getAttribute("data-aann-tg")] = t.checked; renderAann(); return; }
        if ((t = e.target.closest("[data-aann-opt]"))) {
            var groepen = aannGroepen(), id = t.getAttribute("data-aann-opt"), sel = aann.sel ? aann.sel.slice() : groepen.map(function (g) { return g.id; });
            var i = sel.indexOf(id); if (t.checked && i < 0) sel.push(id); else if (!t.checked && i >= 0) sel.splice(i, 1);
            aann.sel = sel.length === groepen.length ? null : sel; renderAann();
        }
    });
    $("gl-v2-pl-aann").addEventListener("input", function (e) {
        if (!e.target.matches("[data-aann-zoek]")) return;
        aann.zoek = e.target.value; $("gl-v2-pa-dd-lijst").innerHTML = aannDropdownHtml(aannGroepen());
    });
    document.addEventListener("click", function (e) { if (aann.open && aannActief && !e.target.closest(".gl-v2-pa-pickw")) { aann.open = false; renderAann(); } });
    function aannAfdrukken() {
        var groepen = aannGroepen(), zichtbaar = aannZichtbaar(groepen), meer = (state.planningen || []).length > 1;
        var w = window.open("", "_blank");
        if (!w) { toast("danger", "Afdrukken geblokkeerd", "Sta pop-ups toe voor deze site en probeer opnieuw."); return; }
        w.document.write("<!doctype html><html lang=\"nl\"><head><meta charset=\"utf-8\"><title>Planning per aannemer</title><style>body{font:13px/1.4 Arial,sans-serif;margin:24px;color:#111}h1{font-size:18px;margin:0 0 16px}h2{font-size:15px;margin:22px 0 2px}.s{color:#555;margin-bottom:6px}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:4px 8px;border-bottom:1px solid #ddd}th{background:#f3f3f3}section{break-inside:avoid-page}</style></head><body><h1>Planning per aannemer</h1>" +
            zichtbaar.map(function (g) { return "<section><h2>" + esc(g.naam) + "</h2><div class=\"s\">" + g.taken.length + " taken" + (g.start ? " · " + fmtFull(g.start) + " tot " + fmtFull(g.einde) : "") + "</div>" + aannTabel(g, meer) + "</section>"; }).join("") + "</body></html>");
        w.document.close(); w.focus(); setTimeout(function () { w.print(); }, 300);
    }
    // Uitvoerder kiezen bij één taak (menu) of bij meerdere tegelijk (modal)
    var aannTaak = null;
    document.addEventListener("click", function (e) {
        var tr = e.target.closest(".js-gl-v2-menu-trigger[aria-controls=\"gl-v2-pl-aannmenu\"]"); if (!tr) return;
        aannTaak = tr.getAttribute("data-aann-taak");
        var lijst = (state.uitvoerders || []).filter(function (u) { return u.inProject; });
        $("gl-v2-pl-aannmenu").innerHTML = lijst.length ? lijst.map(function (u) { return "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-menu=\"" + u.id + "\"><i class=\"ph ph-hard-hat\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + esc(u.naam) + "</span></span></button>"; }).join("") : "<span class=\"gl-v2-menu-item\" aria-disabled=\"true\"><span class=\"gl-v2-menu-item-text\">Geen aannemers met een contract in dit project</span></span>";
    }, true);
    $("gl-v2-pl-aannmenu").addEventListener("click", function (e) {
        var b = e.target.closest("[data-menu]"); if (!b || !aannTaak) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
        var v = aannGroepen().byKey[aannTaak]; if (!v) return;
        aannWijsToe([v.t], parseInt(b.getAttribute("data-menu"), 10), function (ok) { if (ok) toast("success", "Uitvoerder gekozen", "De bouwplanning is aangepast."); });
    });
    function aannWijsToe(taken, uitvoerderId, klaar) {
        var keten = Promise.resolve(true);
        taken.forEach(function (t) { keten = keten.then(function (ok) { return ok === false ? false : send({ type: "wijzig", soort: "taak", id: t.id, planningId: t.planningId, velden: { uitvoerder: uitvoerderId } }, { silent: true }); }); });
        keten.then(function (ok) { aannRefresh(); if (klaar) klaar(ok !== false); });
    }

    // ── Kop (59i): titel, "Geldt voor", kerncijfers op één lijn ──
    var geldtOpen = false;
    function mv(naam, n) { var l = String(naam || "").toLowerCase(); return n === 1 ? l : (/(en|s)$/.test(l) ? l : l + "en"); }
    function geldtVoor(pl) {
        var eenh = state.eenheden || [], lijst;
        if (!pl || pl.global) lijst = eenh;
        else if (pl.isHoofd) lijst = eenh.filter(function (e) { return e.planningId == null || e.planningId === pl.id; });
        else lijst = eenh.filter(function (e) { return e.planningId === pl.id; });
        if (!lijst.length) return "";
        var perType = {}, volgorde = [];
        lijst.forEach(function (e) { var n = e.typeNaam || "eenheid"; if (!perType[n]) { perType[n] = 0; volgorde.push(n); } perType[n]++; });
        var h = "<span class=\"gl-v2-pl-k\">GELDT VOOR</span><b>" + lijst.length + (lijst.length === 1 ? " eenheid" : " eenheden") + "</b>";
        if (volgorde.length > 1 || (volgorde.length === 1 && volgorde[0] !== "eenheid")) volgorde.forEach(function (n) { h += "<span class=\"gl-v2-pl-u\"><b>" + perType[n] + "</b> " + esc(mv(n, perType[n])) + "</span>"; });
        h += "<button type=\"button\" class=\"gl-v2-pl-lnk\" data-geldt-alle aria-expanded=\"" + geldtOpen + "\">" + (geldtOpen ? "minder tonen" : "alle tonen") + "</button>";
        return h + (geldtOpen ? "<div class=\"gl-v2-pl-geldt-lijst\">" + lijst.map(function (e) { return esc(e.naam); }).join(", ") + "</div>" : "");
    }
    function renderKop() {
        var k = state.kop || {};
        var el = $("gl-v2-pl-kop");
        if (!state.heeftPlanning) { el.hidden = true; return; }
        el.hidden = false;
        var pl = actief(), meerdere = (state.planningen || []).length > 1, today = todayIso();
        var marge = k.marge != null
            ? "<span class=\"gl-v2-pl-chip2 " + (k.marge < 0 ? "is-late" : "is-ok") + "\">" + (k.marge < 0 ? dagen(-k.marge) + " te laat" : dagen(k.marge) + " marge") + "</span>" : "";
        var deadline = k.deadline ? "<b>" + fmtFull(k.deadline) + "</b>" + marge : "<b class=\"is-empty\">" + (k.geenStartdatum ? "geen aanvangsdatum" : "geen uitvoeringstermijn") + "</b>";
        // Balk tot vandaag: hoever staat vandaag tussen start en einde van de planning
        var pct = null;
        if (k.start && k.einde && k.einde > k.start) { var a1 = isoN(k.start), b1 = isoN(k.einde), c1 = isoN(today); pct = Math.max(0, Math.min(100, Math.round((c1 - a1) / (b1 - a1) * 1000) / 10)); }
        var duur = "<b>" + dagen(k.duurWerkdagen || 0) + "</b>" + (pct != null ? "<span class=\"gl-v2-pl-pb\"><i style=\"width:" + pct + "%\"></i></span><small>vandaag ± " + Math.round(pct) + " % van de doorlooptijd</small>" : "");
        var verlet = k.verletMeetellen === false ? "uitgezet" : String(k.verletDagen || 0);
        var weer = k.geenWeerstation
            ? "<span class=\"gl-v2-pl-wx\"><i class=\"ph ph-cloud-slash\" aria-hidden=\"true\"></i>geen weerstation — " + (canWriteBasis ? "<a class=\"gl-v2-pl-lnk\" href=\"" + esc(cfg.projectEditUrl || "#") + "\">kiezen</a>" : "kiezen") + "</span><small>werkdagen en deadline rekenen zonder weerverlet</small>" : "";
        var basis = k.basisOp ? "<div><span class=\"gl-v2-pl-k\">BASISPLANNING</span><b>" + esc(k.basisOp) + "</b>" + (k.basisAfwijking ? "<small class=\"" + (k.basisAfwijking > 0 ? "is-late" : "") + "\">" + (k.basisAfwijking > 0 ? "+" : "−") + Math.abs(k.basisAfwijking) + " d</small>" : "") + "</div>" : "";
        var plStart = pl && pl.startDatum ? "<div><span class=\"gl-v2-pl-k\">STARTDATUM PLANNING</span><b>" + fmtFull(pl.startDatum) + "</b></div>" : "";
        var geldt = geldtVoor(pl);
        el.innerHTML =
            "<div class=\"gl-v2-pl-kop-title\"><h2>" + (pl && pl.global ? "Globaal overzicht" : (meerdere && pl ? "Bouwplanning · " + esc(pl.naam) : "Bouwplanning")) + "</h2>" +
            (canWrite && pl ? "<button type=\"button\" class=\"gl-v2-icon-btn js-pl-edit-planning\" title=\"Naam wijzigen of verwijderen\" aria-label=\"Naam wijzigen of verwijderen\"><i class=\"ph ph-dots-three\" aria-hidden=\"true\"></i></button>" : "") + "</div>" +
            "<div class=\"gl-v2-pl-geldt\">" + geldt + "<span class=\"gl-v2-pl-saved" + (canWrite ? "" : " is-ro") + "\"><i class=\"ph " + (canWrite ? "ph-check-circle" : "ph-lock-simple") + "\" aria-hidden=\"true\"></i>" + (canWrite ? "wijzigingen worden meteen bewaard" : "alleen lezen") + "</span></div>" +
            "<div class=\"gl-v2-pl-kpi\">" + plStart +
            "<div><span class=\"gl-v2-pl-k\">START</span><b>" + fmtFull(k.start) + "</b></div>" +
            "<div><span class=\"gl-v2-pl-k\">EINDE</span><b class=\"" + (k.marge != null && k.marge < 0 ? "is-late" : "") + "\">" + fmtFull(k.einde) + "</b></div>" +
            "<div class=\"is-duur\"><span class=\"gl-v2-pl-k\">DUUR</span>" + duur + "</div>" +
            "<div><span class=\"gl-v2-pl-k\">DEADLINE OPLEVERING</span>" + deadline + "</div>" +
            "<div class=\"" + (k.geenWeerstation ? "is-gold" : "") + "\"><span class=\"gl-v2-pl-k\">VERLETDAGEN</span><b>" + verlet + "</b>" + weer + "</div>" +
            basis + "</div>";
    }
    $("gl-v2-pl-kop").addEventListener("click", function (e) {
        if (e.target.closest("[data-geldt-alle]")) { geldtOpen = !geldtOpen; renderKop(); }
    });

    // ── Vraagt actie (59i): wat volgens de planning gebeurd is maar in CPM nog niet staat ──
    function achterstand() { var today = todayIso(); return state.rijen.filter(function (r) { return r.soort === "taak" && !r.afgerond && r.einde && r.einde < today; }); }
    function klaarStages(klaar) {
        var uniek = {}, lijst = [];
        klaar.forEach(function (k) { if (!uniek[k.s.stageId]) { uniek[k.s.stageId] = true; lijst.push(k); } });
        return lijst;
    }
    function vraagtActieHtml(klaar) {
        if (state.actiefPlanningId === GLOBAAL) return "";
        var late = achterstand(), stages = klaarStages(klaar);
        if (!late.length && !stages.length) return "";
        var klaarTaken = state.rijen.filter(function (r) { return r.soort === "taak" && r.afgerond; }).length;
        var titel = late.length ? "Planning loopt achter op de werkelijkheid" : (stages.length === 1 ? "Een schijf is bereikt" : "Schijven zijn bereikt");
        var sub = "Volgens de planning is vandaag " + fmtFull(todayIso()) + "; " + (klaarTaken ? klaarTaken + (klaarTaken === 1 ? " taak is" : " taken zijn") + " afgevinkt." : "niets is afgevinkt.");
        var eenh = 0, bereikt = 0; stages.forEach(function (k) { eenh += k.s.aantalEenheden || 0; bereikt += k.s.aantalBereikt || 0; });
        return "<section class=\"gl-v2-pl-va\" aria-label=\"Vraagt actie\"><span class=\"gl-v2-pl-va-i\"><i class=\"ph ph-warning\" aria-hidden=\"true\"></i></span><div class=\"gl-v2-pl-va-t\"><b>" + titel + "</b><small>" + esc(sub) + "</small></div>" +
            (late.length ? "<div class=\"gl-v2-pl-va-x\"><span><b>" + late.length + (late.length === 1 ? " taak" : " taken") + "</b> voorbij hun einddatum, niet afgevinkt</span><button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary\" data-vr=\"afvinken\">Afvinken tot vandaag…</button></div>" : "") +
            (stages.length ? "<div class=\"gl-v2-pl-va-x\"><span><b>" + stages.length + (stages.length === 1 ? " schijf" : " schijven") + "</b> bereikt volgens de planning, " + bereikt + "/" + eenh + " eenheden gemarkeerd</span><button type=\"button\" class=\"gl-v2-btn gl-v2-btn-secondary\" data-vr=\"markeren\">Markeren…</button></div>" : "") + "</section>";
    }

    // ── Meldingen ──
    var ICON = { danger: "ph-x-circle", warning: "ph-warning", info: "ph-info" };
    function renderNotices() {
        var host = $("gl-v2-pl-notices");
        var html = "";
        var top = [], perRow = [];
        var klaar = [];
        state.rijen.forEach(function (r) { (r.schijven || []).forEach(function (s) { if (s.klaarOmTeMarkeren) klaar.push({ r: r, s: s }); }); });
        // "Geen weerstation" staat sinds 59i in het vak Verletdagen van de kop (met "kiezen"): geen tweede melding erboven
        (state.meldingen || []).forEach(function (m) { if (m.titel === "Geen weerstation") return; (m.rijSleutel ? perRow : top).push(m); });
        top.forEach(function (m) {
            html += "<div class=\"gl-v2-notice gl-v2-notice-expanded is-" + esc(m.ernst) + "\" role=\"status\"><span class=\"gl-v2-notice-expanded-icon\"><i class=\"ph " + (ICON[m.ernst] || ICON.info) + "\" aria-hidden=\"true\"></i></span>" +
                "<div class=\"gl-v2-notice-body\"><span class=\"gl-v2-notice-title\">" + esc(m.titel) + "</span>" + (m.tekst ? "<span class=\"gl-v2-notice-text\">" + esc(m.tekst) + "</span>" : "") + "</div></div>";
        });
        if (canWrite) html = vraagtActieHtml(klaar) + html;
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
    // Hoeveel taken onder een fase voorbij hun einddatum zijn en niet afgevinkt (voor het goudkleurige label bij een ingeklapte fase)
    function nietAfgevinkt(fase) {
        var i = state.rijen.indexOf(fase), n = 0, vandaag = todayIso();
        for (var j = i + 1; j < state.rijen.length && state.rijen[j].niveau > fase.niveau; j++) { var x = state.rijen[j]; if (x.soort === "taak" && !x.afgerond && x.einde && x.einde < vandaag) n++; }
        return n;
    }
    function rowHtml(r) {
        var fase = r.soort === "fase";
        var vandaag = todayIso(), laat = !fase && !r.afgerond && !!r.einde && r.einde < vandaag, loopt = !fase && !r.afgerond && !!r.start && r.start <= vandaag && r.einde >= vandaag;
        var cls = "gl-v2-pl-r" + (fase ? " is-fase" : "") + (laat ? " is-late" : "") + (loopt ? " is-now" : "") + (r.afgerond ? " is-done" : "") + (shifted[r.sleutel] ? " is-shifted" : "") + (selected === r.sleutel ? " is-selected" : "");
        var naam = "";
        if (fase) naam += "<button type=\"button\" class=\"gl-v2-pl-caret\" data-act=\"caret\" aria-expanded=\"" + (ui.collapsed[r.sleutel] ? "false" : "true") + "\" aria-label=\"" + (ui.collapsed[r.sleutel] ? "Uitklappen" : "Inklappen") + "\"><i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>";
        else if (canWrite) naam += "<button type=\"button\" class=\"gl-v2-pl-tick" + (r.afgerond ? " is-on" : "") + (laat ? " is-late" : "") + (loopt ? " is-now" : "") + "\" data-act=\"tick\" role=\"checkbox\" aria-checked=\"" + (r.afgerond ? "true" : "false") + "\" aria-label=\"Voltooid\"><i class=\"ph " + (r.afgerond ? "ph-check-circle ph-fill" : "ph-circle") + "\" aria-hidden=\"true\"></i></button>";
        else naam += "<span class=\"gl-v2-pl-spacer\"></span>";
        if (canWrite) naam = "<span class=\"gl-v2-pl-grip\" data-grip title=\"Sleep om de rij te verplaatsen\" aria-hidden=\"true\"><i class=\"ph ph-dots-six-vertical\"></i></span>" + naam;
        naam += "<span class=\"gl-v2-pl-name-text\" " + (canWrite ? "data-edit=\"naam\" title=\"Klik om de naam te wijzigen\"" : "") + ">" + esc(r.naam) + "</span>";
        // Eén label per schijfnummer (dezelfde schijf in meerdere betalingsgroepen telt samen): de naamkolom is smal, de taaknaam moet leesbaar blijven.
        var perNummer = {}, volgorde = [];
        (r.schijven || []).forEach(function (s) { if (!perNummer[s.nummer]) { perNummer[s.nummer] = []; volgorde.push(s.nummer); } perNummer[s.nummer].push(s); });
        volgorde.forEach(function (nr) {
            var l = perNummer[nr], eenh = 0, bereikt = 0, klaar = [], teMarkeren = 0;
            l.forEach(function (s) { eenh += s.aantalEenheden || 0; bereikt += s.aantalBereikt || 0; if (s.klaarOmTeMarkeren) { klaar.push(s.stageId); teMarkeren += s.teMarkeren; } });
            var alles = l.every(function (s) { return s.allesBereikt; });
            var tip = l.map(function (s) { return s.groepNaam + " · " + s.naam + " (" + s.percentage + " %)"; }).join("\n");
            var verschuldigd = !alles && (klaar.length > 0 || r.afgerond || laat);
            if (klaar.length && canWrite) naam += "<button type=\"button\" class=\"gl-v2-pl-schijf is-klaar is-due\" data-act=\"schijf-markeer\" data-stage=\"" + klaar.join(",") + "\" title=\"" + esc(tip) + "\nMarkeer als bereikt voor " + teMarkeren + (teMarkeren === 1 ? " eenheid" : " eenheden") + "\"><i class=\"ph ph-coins\" aria-hidden=\"true\"></i>" + nr + (eenh ? " · " + bereikt + "/" + eenh : "") + "</button>";
            else naam += "<span class=\"gl-v2-pl-schijf" + (alles ? " is-bereikt" : "") + (verschuldigd ? " is-due" : "") + "\" title=\"" + esc(tip) + "\"><i class=\"ph " + (alles ? "ph-seal-check" : "ph-coins") + "\" aria-hidden=\"true\"></i>" + nr + (eenh ? " · " + bereikt + "/" + eenh : "") + "</span>";
        });
        if (ui.kritiek && r.kritiek) naam += "<span class=\"gl-v2-pl-badge\" title=\"Kritiek pad: " + (fase ? "bevat kritieke taken" : "geen speling") + "\">K</span>";
        if (shifted[r.sleutel]) naam += "<span class=\"gl-v2-pl-badge is-gold\">verschoven</span>";
        if (r.beperking === 1 && !fase) naam += "<i class=\"ph ph-push-pin\" title=\"Moet beginnen op " + fmtFull(r.beperkingDatum) + "\" aria-label=\"vastgepind\"></i>";
        if (fase && ui.collapsed[r.sleutel]) { var nv = nietAfgevinkt(r); if (nv) naam += "<span class=\"gl-v2-pl-badge is-gold\" title=\"Taken in deze fase die voorbij hun einddatum zijn en niet afgevinkt\">" + nv + " niet afgevinkt</span>"; }
        if (fase && canWrite) naam += "<button type=\"button\" class=\"gl-v2-pl-add-in-fase\" data-act=\"add-in\">+ Taak</button>";

        var afw = ui.basis && r.afwijking ? " <small class=\"gl-v2-pl-afw " + (r.afwijking > 0 ? "is-plus" : "is-min") + "\">" + (r.afwijking > 0 ? "+" : "−") + Math.abs(r.afwijking) + "</small>" : "";
        var duurTxt = r.isMijlpaal ? "◆" : r.duur + " d";
        return "<div class=\"" + cls + "\" role=\"row\" data-key=\"" + esc(r.sleutel) + "\">" +
            "<span class=\"gl-v2-pl-c is-wbs\" role=\"cell\" data-act=\"open\">" + esc(r.wbs) + "</span>" +
            "<span class=\"gl-v2-pl-c\" role=\"cell\"><span class=\"gl-v2-pl-name\" style=\"--pl-level:" + r.niveau + "\">" + naam + "</span></span>" +
            "<span class=\"gl-v2-pl-c is-num c-dur" + (!fase && canWrite ? " is-edit" : " is-muted") + "\" role=\"cell\" " + (!fase && canWrite ? "data-edit=\"duur\"" : "") + ">" + duurTxt + "</span>" +
            "<span class=\"gl-v2-pl-c c-start\" role=\"cell\" data-act=\"open\">" + fmt(r.start) + "</span>" +
            "<span class=\"gl-v2-pl-c c-einde" + (laat ? " is-late" : "") + "\" role=\"cell\" data-act=\"open\">" + fmt(r.einde) + afw + "</span>" +
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

    // De schijf als genummerd muntje aan het einde van de balk (goud als ze volgens de planning bereikt zou moeten zijn)
    function muntje(r) {
        if (!(r.schijven || []).length) return null;
        var nr = r.schijven[0].nummer, alles = r.schijven.every(function (s) { return s.allesBereikt; }), laat = !r.afgerond && !!r.einde && r.einde < todayIso();
        return { nr: nr, due: !alles && (r.schijven.some(function (s) { return s.klaarOmTeMarkeren; }) || r.afgerond || laat), done: alles };
    }
    function renderGantt(vis) {
        var scroller = $("gl-v2-pl-scroll");
        var anchor = gantt ? gantt.dateAt(scroller.scrollLeft) : null; // de datum die nu links in beeld staat, zodat zoomen/herladen niet wegspringt
        var rows = vis.map(function (r) {
            return {
                key: r.sleutel, type: r.soort === "fase" ? "phase" : (r.isMijlpaal ? "milestone" : "task"),
                start: r.start, end: r.einde, progress: r.voortgang, critical: r.kritiek, done: r.afgerond,
                pinned: r.soort === "taak" && r.beperking === 1, shifted: !!shifted[r.sleutel], label: r.naam,
                late: r.soort === "taak" && !r.afgerond && !!r.einde && r.einde < todayIso(), now: r.soort === "taak" && !r.afgerond && !!r.start && r.start <= todayIso() && r.einde >= todayIso(),
                coin: muntje(r),
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
    function render() { renderMain(); renderMobile(); }
    function renderMain() {
        canWrite = canWriteBasis && state.actiefPlanningId !== GLOBAAL;
        var heeft = !!state.heeftPlanning && state.rijen.length > 0;
        renderTabs();
        $("gl-v2-pl-aann").hidden = !aannActief;
        if (aannActief) {
            $("gl-v2-pl-empty").hidden = true; $("gl-v2-pl-card").hidden = true; $("gl-v2-pl-kop").hidden = true;
            renderAann();
            return;
        }
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
        standaardInklappen();
        var scr = $("gl-v2-pl-scroll"), keepTop = scr.scrollTop;
        ensureListW();
        var vis = visibleRows();
        hoverKey = null;
        renderList(vis);
        renderGantt(vis);
        scr.scrollTop = keepTop; // een herteken mag de verticale positie niet verspringen
    }
    // Bij het eerste openen van een planning: de hoofdfases zijn ingeklapt behalve wat nu loopt (59i). Loopt er vandaag niets, dan blijft alles zoals het was.
    function standaardInklappen() {
        var sleutel = String(state.actiefPlanningId || 0);
        if (ui.inklapGedaan[sleutel] || aannActief) return;
        var vandaag = todayIso(), top = state.rijen.filter(function (r) { return r.soort === "fase" && r.niveau === 0; });
        var lopend = top.filter(function (r) { return r.start && r.start <= vandaag && r.einde >= vandaag; });
        ui.inklapGedaan[sleutel] = true;
        if (lopend.length && lopend.length < top.length) top.forEach(function (r) { if (lopend.indexOf(r) >= 0) delete ui.collapsed[r.sleutel]; else ui.collapsed[r.sleutel] = true; });
        saveUi();
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
        if (e.target.closest(".gl-v2-pl-rowmenu") || e.target.closest("[data-grip]")) return;
        var caret = e.target.closest("[data-act=caret]");
        if (caret) { ui.collapsed[key] = !ui.collapsed[key]; saveUi(); render(); return; }
        var sm = e.target.closest("[data-act=schijf-markeer]");
        if (sm && canWrite) { schijfMarkeerLijst(String(sm.getAttribute("data-stage")).split(",").map(function (x) { return parseInt(x, 10); }).filter(function (x) { return x > 0; })); return; }
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

    // Rechtsklik op een rij in de lijst of op een balk/rij in de grafiek: hetzelfde menu als ⋯, bij de muis
    $("gl-v2-pl-scroll").addEventListener("contextmenu", function (e) {
        if (!canWrite || !window.GlV2Menu || e.target.closest(".gl-v2-pl-split, input, textarea")) return;
        var rowEl = e.target.closest(".gl-v2-pl-r"), key = null;
        if (rowEl) key = rowEl.getAttribute("data-key");
        else if (gantt && e.target.closest(".gl-v2-pl-chart")) key = gantt.keyAt(e.clientY);
        var r = key ? rowByKey(key) : null;
        if (!r) return;
        e.preventDefault();
        openRijMenu(key, e.clientX, e.clientY);
    });
    function openRijMenu(key, x, y) {
        var r = rowByKey(key); if (!r || !window.GlV2Menu) return;
        window.GlV2Menu.closeAll();
        menuKey = key; selected = key; markSelected();
        buildMenu(r);
        var menu = $("gl-v2-pl-menu"), backdrop = document.getElementById("gl-v2-menu-backdrop");
        menu.classList.add("is-open");
        if (backdrop) backdrop.classList.add("is-open");
        if (window.innerWidth >= 768) {
            var w = menu.offsetWidth || 224, h = menu.offsetHeight || 300;
            menu.style.left = Math.max(12, Math.min(x, window.innerWidth - w - 12)) + "px";
            menu.style.top = Math.max(12, Math.min(y, window.innerHeight - h - 12)) + "px";
        }
    }

    // Scrollen (pagina of planning) sluit het menu: het staat op vaste coördinaten en zou anders naast de rij blijven hangen
    window.addEventListener("scroll", function (e) {
        var menu = $("gl-v2-pl-menu");
        if (!menu || !menu.classList.contains("is-open")) return;
        if (e.target && e.target.nodeType === 1 && menu.contains(e.target)) return;
        if (window.GlV2Menu) window.GlV2Menu.closeAll();
    }, true);

    // ── Balken slepen (verschuiven, duur, voortgang), toetsenbord op een balk en rijen slepen ──
    var DAG = 86400000;
    function isoN(iso) { var p = iso.split("-"); return Math.round(Date.UTC(+p[0], +p[1] - 1, +p[2]) / DAG); }
    function nIso(n) { var d = new Date(n * DAG), z = function (v) { return (v < 10 ? "0" : "") + v; }; return d.getUTCFullYear() + "-" + z(d.getUTCMonth() + 1) + "-" + z(d.getUTCDate()); }
    function nietWerk() {
        if (state._nwRef !== state.nietWerkdagen) { state._nw = {}; (state.nietWerkdagen || []).forEach(function (n) { state._nw[n.datum] = true; }); state._nwRef = state.nietWerkdagen; }
        return state._nw;
    }
    function isWerkdag(n) { var w = (new Date(n * DAG).getUTCDay() + 6) % 7; return w < 5 && !nietWerk()[nIso(n)]; }
    function volgendeWerkdag(n, richting) { var g = 0; while (!isWerkdag(n) && g++ < 400) n += richting || 1; return n; }
    function werkdagenTot(startN, eindN) { var c = 0; for (var n = startN; n <= eindN; n++) if (isWerkdag(n)) c++; return c; }
    function verschuifWerkdagen(startN, k) { var n = startN, stap = k < 0 ? -1 : 1; for (var i = 0; i < Math.abs(k); i++) { n += stap; n = volgendeWerkdag(n, stap); } return n; }

    var dragLabel = null, drag = null;
    function zetDragLabel(x, y, tekst) {
        if (!dragLabel) { dragLabel = document.createElement("div"); dragLabel.className = "gl-v2-pl-draglabel"; $("gl-v2-pl-root").appendChild(dragLabel); }
        dragLabel.textContent = tekst; dragLabel.style.left = (x + 14) + "px"; dragLabel.style.top = (y - 34) + "px";
    }
    function wisDragLabel() { if (dragLabel && dragLabel.parentNode) dragLabel.parentNode.removeChild(dragLabel); dragLabel = null; }

    // Een taak naar een nieuwe startdatum: "niet eerder beginnen dan" (of "moet beginnen op" als ze al vastgepind is)
    function schuifTaak(r, nieuweStartIso, refocus) {
        if (nieuweStartIso === r.start) return;
        send({ type: "wijzig", soort: "taak", id: r.id, velden: { beperking: r.beperking === 1 ? 1 : 2, beperkingDatum: nieuweStartIso } }, {
            silent: false,
            after: function () {
                var nu = rowByKey(r.sleutel);
                if (nu && nu.start !== nieuweStartIso) toast("info", "De taak start op " + fmtFull(nu.start), "Een voorganger of een andere beperking bepaalt de start. Pas de voorganger of de wachttijd aan om vroeger te starten.");
                if (refocus) { var b = document.querySelector("#gl-v2-pl-gantt .gl-v2-gantt-bar[data-key=\"" + r.sleutel + "\"]"); if (b) b.focus(); }
            }
        });
    }
    function zetVoortgang(r, pct, refocus) {
        if (pct === r.voortgang) return;
        send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: pct } }, {
            after: function () { if (refocus) { var b = document.querySelector("#gl-v2-pl-gantt .gl-v2-gantt-bar[data-key=\"" + r.sleutel + "\"]"); if (b) b.focus(); } }
        });
    }

    // Waar op de balk pak je ze vast: rechterrand = duur, voortgangsstreepje = voortgang, de rest = verschuiven
    function dragModeBij(x, rect, r) {
        if (!r.isMijlpaal && !r.afgerond) {
            if (rect.right - x <= 6 && rect.width > 18) return "duur";
            if (r.voortgang > 0 && r.voortgang < 100 && Math.abs(x - (rect.left + rect.width * r.voortgang / 100)) <= 4) return "voortgang";
        }
        return "move";
    }
    // Aanwijzer boven een balk: pijltje links-rechts op de rand en het streepje, anders de handpalm (zie CSS)
    $("gl-v2-pl-gantt").addEventListener("pointermove", function (e) {
        if (drag || e.pointerType !== "mouse") return;
        var bar = e.target.closest ? e.target.closest(".gl-v2-gantt-bar") : null; if (!bar) return;
        var r = canWrite ? rowByKey(bar.getAttribute("data-key")) : null;
        var m = r && r.soort !== "fase" && r.start ? dragModeBij(e.clientX, bar.getBoundingClientRect(), r) : "move";
        if (bar.getAttribute("data-drag") !== m) bar.setAttribute("data-drag", m);
    });
    $("gl-v2-pl-gantt").addEventListener("pointerdown", function (e) {
        if (!canWrite || !gantt || e.button !== 0 || e.pointerType === "touch") return;
        var bar = e.target.closest(".gl-v2-gantt-bar"); if (!bar) return;
        var r = rowByKey(bar.getAttribute("data-key")); if (!r || r.soort === "fase" || !r.start) return;
        var rect = bar.getBoundingClientRect(), mode = dragModeBij(e.clientX, rect, r);
        if (r.afgerond && mode === "move") return;   // een afgeronde taak schuift niet
        drag = { r: r, bar: bar, mode: mode, x0: e.clientX, rect: rect, px: gantt.px, moved: false, bereik: null };
    });
    document.addEventListener("pointermove", function (e) {
        if (!drag) return;
        if (e.buttons === 0) { eindeDrag(false); return; }
        var dx = e.clientX - drag.x0;
        if (!drag.moved && Math.abs(dx) < 4) return;
        if (!drag.moved) { drag.moved = true; document.body.classList.add("gl-v2-pl-is-dragging"); document.body.setAttribute("data-pl-drag", drag.mode); }
        var r = drag.r, px = drag.px || 1, tekst = "";
        if (drag.mode === "move") {
            var dd = Math.round(dx / px), s = volgendeWerkdag(isoN(r.start) + dd, dd < 0 ? -1 : 1);
            drag.bereik = nIso(s);
            drag.bar.style.transform = "translateX(" + (dd * px) + "px)";
            tekst = "Start " + fmtFull(drag.bereik);
        } else if (drag.mode === "duur") {
            var sN = isoN(r.start), eN = Math.max(sN, isoN(r.einde) + Math.round(dx / px));
            drag.bereik = werkdagenTot(sN, eN) || 1;
            drag.bar.style.width = ((eN - sN + 1) * px) + "px";
            tekst = dagen(drag.bereik);
        } else {
            var pct = Math.max(0, Math.min(100, Math.round(((e.clientX - drag.rect.left) / drag.rect.width * 100) / 5) * 5));
            drag.bereik = pct;
            var pg = drag.bar.querySelector(".gl-v2-gantt-progress"); if (pg) pg.style.width = pct + "%";
            tekst = pct + " % klaar";
        }
        zetDragLabel(e.clientX, e.clientY, tekst);
    });
    function eindeDrag(commit) {
        if (!drag) return;
        var d = drag; drag = null;
        document.body.classList.remove("gl-v2-pl-is-dragging"); document.body.removeAttribute("data-pl-drag"); wisDragLabel();
        if (!d.moved) return;
        suppressClick = true; window.setTimeout(function () { suppressClick = false; }, 0);
        d.bar.style.transform = ""; d.bar.style.width = "";
        if (!commit || d.bereik == null) { render(); return; }
        if (d.mode === "move") schuifTaak(d.r, d.bereik, false);
        else if (d.mode === "duur") { if (d.bereik === d.r.duur) render(); else send({ type: "wijzig", soort: "taak", id: d.r.id, velden: { duur: d.bereik } }); }
        else zetVoortgang(d.r, d.bereik, false);
    }
    document.addEventListener("pointerup", function () { eindeDrag(true); });
    document.addEventListener("pointercancel", function () { eindeDrag(false); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && drag) eindeDrag(false); });

    // Toetsenbord op een balk: 0-4 = voortgang, pijltjes = een werkdag verschuiven, Delete = verwijderen, menutoets of Shift+F10 = menu
    $("gl-v2-pl-gantt").addEventListener("keydown", function (e) {
        var bar = e.target.closest(".gl-v2-gantt-bar"); if (!bar || !canWrite) return;
        var r = rowByKey(bar.getAttribute("data-key")); if (!r) return;
        if (e.key === "ContextMenu" || (e.shiftKey && e.key === "F10")) {
            e.preventDefault(); var rc = bar.getBoundingClientRect(); openRijMenu(r.sleutel, rc.left + 20, rc.bottom + 4); return;
        }
        if (e.ctrlKey || e.metaKey || e.altKey) return;
        if (e.key === "Delete") { e.preventDefault(); selected = r.sleutel; vraagVerwijder(r); return; }
        if (r.soort === "fase") return;
        if (!r.isMijlpaal && /^[0-4]$/.test(e.key)) { e.preventDefault(); zetVoortgang(r, [0, 25, 50, 75, 100][+e.key], true); return; }
        if ((e.key === "ArrowLeft" || e.key === "ArrowRight") && !r.afgerond && r.start) {
            e.preventDefault();
            schuifTaak(r, nIso(verschuifWerkdagen(isoN(r.start), e.key === "ArrowLeft" ? -1 : 1)), true);
        }
    });

    // Rijen slepen aan het ⋮⋮-handvat: naar een andere plaats of fase
    var rijDrag = null, dropLijn = null, dropInto = null;
    function wisDrop() { if (dropLijn && dropLijn.parentNode) dropLijn.parentNode.removeChild(dropLijn); dropLijn = null; if (dropInto) dropInto.classList.remove("is-dropinto"); dropInto = null; }
    function dropSpec(r0, T, rel) {
        if (!T || T.sleutel === r0.sleutel) return null;
        if (r0.soort === "fase" && T.wbs.indexOf(r0.wbs + ".") === 0) return null;   // niet in zichzelf
        var kinderen = state.rijen.some(function (x) { return x.ouderFaseId === T.id && T.soort === "fase"; });
        var open = T.soort === "fase" && kinderen && !ui.collapsed[T.sleutel];
        var into = T.soort === "fase" && ((rel > 0.3 && rel < 0.7) || (open && rel >= 0.5));
        if (into) return { into: T, naSoort: open && rel >= 0.5 ? "begin" : null, mode: "into" };
        var voor = rel < 0.5;
        var bovenId = T.ouderFaseId;
        if (bovenId == null && r0.soort === "taak") return { into: T, naSoort: voor ? "begin" : null, mode: "into" };   // een taak hoort in een fase
        var sibs = state.rijen.filter(function (x) { return x.ouderFaseId === T.ouderFaseId && x.niveau === T.niveau; });
        var i = sibs.indexOf(T);
        if (voor) {
            var P = i > 0 ? sibs[i - 1] : null;
            if (P && P.sleutel === r0.sleutel) return null;
            return { ouder: bovenId, na: P, naSoort: P ? null : "begin", mode: "voor", T: T };
        }
        return { ouder: bovenId, na: T, mode: "na", T: T };
    }
    $("gl-v2-pl-list").addEventListener("pointerdown", function (e) {
        var grip = e.target.closest("[data-grip]"); if (!grip || !canWrite || e.button !== 0) return;
        var rowEl = grip.closest(".gl-v2-pl-r"); var r = rowEl && rowByKey(rowEl.getAttribute("data-key")); if (!r) return;
        e.preventDefault();
        rijDrag = { r: r, spec: null, moved: false, x0: e.clientX, y0: e.clientY };
    });
    document.addEventListener("pointermove", function (e) {
        if (!rijDrag) return;
        if (e.buttons === 0) { eindeRijDrag(false); return; }
        if (!rijDrag.moved && Math.abs(e.clientY - rijDrag.y0) + Math.abs(e.clientX - rijDrag.x0) < 5) return;
        if (!rijDrag.moved) { rijDrag.moved = true; document.body.classList.add("gl-v2-pl-is-dragging"); }
        wisDrop();
        var el = document.elementFromPoint(e.clientX, e.clientY), rowEl = el && el.closest ? el.closest("#gl-v2-pl-list .gl-v2-pl-r") : null;
        rijDrag.spec = null;
        if (!rowEl) return;
        var T = rowByKey(rowEl.getAttribute("data-key")), rc = rowEl.getBoundingClientRect();
        var spec = dropSpec(rijDrag.r, T, (e.clientY - rc.top) / rc.height);
        rijDrag.spec = spec;
        if (!spec) return;
        if (spec.mode === "into") { dropInto = rowEl; rowEl.classList.add("is-dropinto"); }
        else {
            dropLijn = document.createElement("div"); dropLijn.className = "gl-v2-pl-dropline";
            dropLijn.style.left = rc.left + "px"; dropLijn.style.width = rc.width + "px"; dropLijn.style.top = (spec.mode === "voor" ? rc.top : rc.bottom) + "px";
            $("gl-v2-pl-root").appendChild(dropLijn);
        }
    });
    function eindeRijDrag(commit) {
        if (!rijDrag) return;
        var d = rijDrag; rijDrag = null;
        document.body.classList.remove("gl-v2-pl-is-dragging"); wisDrop();
        if (!d.moved || !commit || !d.spec) return;
        suppressClick = true; window.setTimeout(function () { suppressClick = false; }, 0);
        var s = d.spec, cmd = { type: "verplaats", soort: d.r.soort, id: d.r.id, actie: "naar" };
        if (s.mode === "into") { cmd.sectieId = s.into.id; if (s.naSoort) cmd.naSoort = s.naSoort; }
        else { cmd.sectieId = s.ouder; if (s.na) { cmd.naId = s.na.id; cmd.naSoort = s.na.soort; } else cmd.naSoort = "begin"; }
        send(cmd);
    }
    document.addEventListener("pointerup", function () { eindeRijDrag(true); });
    document.addEventListener("pointercancel", function () { eindeRijDrag(false); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && rijDrag) eindeRijDrag(false); });

    function menuItem(act, icon, label, danger) {
        return "<button type=\"button\" class=\"gl-v2-menu-item" + (danger ? " is-danger" : "") + "\" role=\"menuitem\" data-menu=\"" + act + "\"><i class=\"ph " + icon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + label + "</span></span></button>";
    }
    function buildMenu(r) {
        if (!r) return;
        var fase = r.soort === "fase", h = "";
        if (!fase && !r.isMijlpaal) {
            h += "<div class=\"gl-v2-pl-menu-pct\" role=\"group\" aria-label=\"Voortgang\"><span>Voortgang</span>" +
                [0, 25, 50, 75, 100].map(function (p) { return "<button type=\"button\" role=\"menuitem\" class=\"gl-v2-pl-menu-pctbtn" + (r.voortgang === p ? " is-on" : "") + "\" data-menu=\"pct-" + p + "\">" + p + "</button>"; }).join("") + "</div>";
        }
        if (!fase) {
            if (!r.afgerond && !r.werkelijkStart) h += menuItem("vandaag-gestart", "ph-play-circle", "Vandaag gestart");
            if (!r.afgerond) h += menuItem("volgens-schema", "ph-calendar-check", "Op schema zetten");
        }
        h += menuItem("edit", "ph-pencil-simple", "Bewerken…");
        h += menuItem(fase ? "add-in" : "add-after", "ph-plus", fase ? "Taak toevoegen in de fase" : "Taak eronder toevoegen");
        if (fase) h += menuItem("add-fase", "ph-stack-plus", "Fase eronder toevoegen");
        if (!fase) h += menuItem("tick", r.afgerond ? "ph-circle" : "ph-check-circle", r.afgerond ? "Niet meer voltooid" : "Markeren als voltooid");
        if (!fase && !r.afgerond) h += menuItem(r.beperking === 1 ? "losmaken" : "vastpinnen", "ph-push-pin", r.beperking === 1 ? "Vastpinnen opheffen" : "Vastpinnen op de startdatum");
        h += menuItem("omhoog", "ph-arrow-up", "Omhoog");
        h += menuItem("omlaag", "ph-arrow-down", "Omlaag");
        h += menuItem("inspringen", "ph-arrow-line-right", "Inspringen");
        h += menuItem("uitspringen", "ph-arrow-line-left", "Uitspringen");
        (r.schijven || []).forEach(function (s) {
            if (s.teMarkeren > 0) h += menuItem("schijf-markeer-" + s.stageId, "ph-seal-check", "Schijf " + esc(s.naam) + " bereikt: markeren");
            if (s.aantalBereikt > 0) h += menuItem("schijf-terug-" + s.stageId, "ph-arrow-counter-clockwise", "Schijf " + esc(s.naam) + " terugzetten");
        });
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
        else if (act.indexOf("schijf-markeer-") === 0) schijfMarkeer(parseInt(act.slice(15), 10), false);
        else if (act.indexOf("schijf-terug-") === 0) schijfMarkeer(parseInt(act.slice(13), 10), true);
        else if (act.indexOf("pct-") === 0) send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: parseInt(act.slice(4), 10) } });
        else if (act === "vandaag-gestart") send({ type: "wijzig", soort: "taak", id: r.id, velden: { werkelijkStart: todayIso(), voortgang: Math.max(r.voortgang || 0, 1) } });
        else if (act === "volgens-schema") send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: voortgangVolgensSchema(r) } });
        else if (act === "vastpinnen") send({ type: "wijzig", soort: "taak", id: r.id, velden: { beperking: 1, beperkingDatum: r.start } });
        else if (act === "losmaken") send({ type: "wijzig", soort: "taak", id: r.id, velden: { beperking: 0 } });
        else if (act === "tick") send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: r.afgerond ? 0 : 100 } });
        else if (act === "omhoog" || act === "omlaag" || act === "inspringen" || act === "uitspringen") send({ type: "verplaats", soort: r.soort, id: r.id, actie: act });
        else if (act === "verwijder") vraagVerwijder(r);
    });

    // Voortgang volgens het schema: aandeel van de geplande periode dat al voorbij is (op kalenderdagen), afgerond naar 5 %.
    function voortgangVolgensSchema(r) {
        var t = todayIso();
        if (!r.start || t <= r.start) return 0;
        if (!r.einde || t > r.einde) return 100;
        var d = function (s) { var p = s.split("-"); return Date.UTC(+p[0], +p[1] - 1, +p[2]) / 86400000; };
        var tot = d(r.einde) - d(r.start) + 1, gedaan = d(t) - d(r.start);
        return Math.max(0, Math.min(95, Math.round(gedaan / tot * 20) * 5));
    }

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
    var vr = { soort: null, bezig: false };
    function vrLijst(items) {
        $("gl-v2-pl-vr-lijst").innerHTML = items.map(function (x, i) {
            return "<div class=\"gl-v2-pl-vr-rij\"><span class=\"gl-v2-check\"><input type=\"checkbox\" id=\"gl-v2-pl-vr-" + i + "\" data-i=\"" + i + "\" checked /><label for=\"gl-v2-pl-vr-" + i + "\"></label></span><label class=\"gl-v2-pl-vr-tekst\" for=\"gl-v2-pl-vr-" + i + "\"><b>" + esc(x.titel) + "</b><small>" + esc(x.sub) + "</small></label></div>";
        }).join("");
        vr.items = items; vrTel();
    }
    function vrGekozen() { return Array.prototype.map.call(document.querySelectorAll("#gl-v2-pl-vr-lijst input:checked"), function (c) { return vr.items[parseInt(c.getAttribute("data-i"), 10)]; }); }
    function vrTel() {
        var n = vrGekozen().length, alle = $("gl-v2-pl-vr-alle");
        alle.checked = n === vr.items.length; alle.indeterminate = n > 0 && n < vr.items.length;
        $("gl-v2-pl-vr-ok").disabled = n === 0 || vr.bezig;
        $("gl-v2-pl-vr-ok-t").textContent = (vr.soort === "afvinken" ? "Afvinken" : vr.soort === "toewijzen" ? "Toewijzen" : "Markeren") + " (" + n + ")";
    }
    function openVraag(soort) {
        if (!canWrite) return;
        vr.soort = soort; vr.bezig = false; $("gl-v2-pl-vr-fout").hidden = true;
        var extra = $("gl-v2-pl-vr-extra"); extra.hidden = true; extra.innerHTML = "";
        if (soort === "toewijzen") {
            var g0 = aannGroepen().filter(function (g) { return g.id === "0"; })[0], zonder = g0 ? g0.taken : [];
            $("gl-v2-pl-vr-titel").textContent = "Uitvoerder toewijzen";
            $("gl-v2-pl-vr-uitleg").textContent = "Kies de aannemer en de taken die hij uitvoert. De bouwplanning wordt meteen aangepast.";
            var opties = (state.uitvoerders || []).filter(function (u) { return u.inProject; });
            extra.hidden = false;
            extra.innerHTML = "<span class=\"gl-v2-field-label\">Aannemer</span><div class=\"gl-v2-select\" data-gl-v2-select data-placeholder=\"Kies een aannemer\"><input type=\"hidden\" id=\"gl-v2-pl-vr-uitv\" value=\"\" /><div class=\"gl-v2-select-trigger\" role=\"button\" tabindex=\"0\" aria-haspopup=\"listbox\" aria-expanded=\"false\" aria-label=\"Aannemer\"><span class=\"gl-v2-select-trigger-label\">Kies een aannemer</span><i class=\"ph ph-caret-down gl-v2-select-trigger-caret\" aria-hidden=\"true\"></i></div><div class=\"gl-v2-select-panel\" role=\"listbox\">" + opties.map(function (u) { return "<button type=\"button\" class=\"gl-v2-select-option\" role=\"option\" data-value=\"" + u.id + "\"><i class=\"ph ph-check\" aria-hidden=\"true\"></i><span>" + esc(u.naam) + "</span></button>"; }).join("") + "</div></div>";
            if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(extra);
            vrLijst(zonder.map(function (t) { return { titel: t.wbs + " " + t.naam, sub: (t.fase || t.planning || "") + " · " + fmt(t.start) + " – " + fmt(t.einde), t: t }; }));
        } else if (soort === "afvinken") {
            var late = achterstand();
            $("gl-v2-pl-vr-titel").textContent = "Taken afvinken tot vandaag";
            $("gl-v2-pl-vr-uitleg").textContent = "Deze taken zijn volgens de planning voorbij hun einddatum maar niet afgevinkt. Vink uit wat nog niet klaar is: enkel de gekozen taken worden op 100 % gezet.";
            vrLijst(late.map(function (r) { return { titel: r.wbs + " " + r.naam, sub: "einde " + fmt(r.einde), r: r }; }));
        } else {
            var st = klaarStages(state.rijen.reduce(function (a, r) { (r.schijven || []).forEach(function (s) { if (s.klaarOmTeMarkeren) a.push({ r: r, s: s }); }); return a; }, []));
            $("gl-v2-pl-vr-titel").textContent = "Schijven markeren als bereikt";
            $("gl-v2-pl-vr-uitleg").textContent = "Deze schijven zijn volgens de planning bereikt. Vink uit wat nog niet klopt: enkel de gekozen schijven worden gemarkeerd (niet verkochte eenheden worden overgeslagen) en zijn dan factureerbaar.";
            vrLijst(st.map(function (k) { return { titel: k.s.groepNaam + " · " + k.s.naam, sub: k.r.wbs + " " + k.r.naam + " is klaar · " + k.s.teMarkeren + (k.s.teMarkeren === 1 ? " eenheid te markeren" : " eenheden te markeren"), s: k.s }; }));
        }
        var m = modal("gl-v2-pl-vr"); if (m) m.show();
    }
    $("gl-v2-pl-vr-alle").addEventListener("change", function () { var aan = this.checked; document.querySelectorAll("#gl-v2-pl-vr-lijst input").forEach(function (c) { c.checked = aan; }); vrTel(); });
    $("gl-v2-pl-vr-lijst").addEventListener("change", vrTel);
    $("gl-v2-pl-vr").querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        var gekozen = vrGekozen(); if (!gekozen.length || vr.bezig) return;
        vr.bezig = true; $("gl-v2-pl-vr-ok").disabled = true;
        var klaar = function (titel, tekst) { vr.bezig = false; var m = modal("gl-v2-pl-vr"); if (m) m.hide(); toast("success", titel, tekst); };
        if (vr.soort === "toewijzen") {
            var uid = parseInt($("gl-v2-pl-vr-uitv").value, 10);
            if (!uid) { vr.bezig = false; vrTel(); var f = $("gl-v2-pl-vr-fout"); f.textContent = "Kies eerst een aannemer."; f.hidden = false; return; }
            aannWijsToe(gekozen.map(function (x) { return x.t; }), uid, function (ok) { if (ok) klaar("Uitvoerder toegewezen", gekozen.length + (gekozen.length === 1 ? " taak" : " taken") + " toegewezen. De bouwplanning is aangepast."); else { vr.bezig = false; vrTel(); } });
        } else if (vr.soort === "afvinken") {
            var keten = Promise.resolve(true);
            gekozen.forEach(function (x) { keten = keten.then(function (ok) { return ok === false ? false : send({ type: "wijzig", soort: "taak", id: x.r.id, velden: { voortgang: 100 } }, { silent: true }); }); });
            keten.then(function (ok) { if (ok === false) { vr.bezig = false; vrTel(); return; } klaar("Taken afgevinkt", gekozen.length + (gekozen.length === 1 ? " taak staat" : " taken staan") + " op 100 %. De planning is opnieuw berekend."); });
        } else {
            var rest = gekozen.slice(), totaal = 0; gekozen.forEach(function (x) { totaal += x.s.teMarkeren; });
            (function volgende() {
                var x = rest.shift();
                schijfPost(cfg.schijfMarkeerUrl, { stageId: x.s.stageId, terugzetten: false }, function () {
                    if (rest.length) volgende(); else klaar(gekozen.length === 1 ? "Schijf bereikt" : gekozen.length + " schijven bereikt", "Gemarkeerd voor " + totaal + (totaal === 1 ? " eenheid" : " eenheden") + ". De schijven zijn nu factureerbaar.");
                });
            })();
        }
    });
    $("gl-v2-pl-notices").addEventListener("click", function (e) {
        var va = e.target.closest("[data-vr]"); if (va) { openVraag(va.getAttribute("data-vr")); return; }
        if (e.target.closest("[data-notices-toggle]")) { ui.noticesOpen = !ui.noticesOpen; saveUi(); renderNotices(); return; }
        var sb = e.target.closest("[data-schijf-markeer]"); if (sb) { schijfMarkeer(parseInt(sb.getAttribute("data-schijf-markeer"), 10), false); return; }
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
        var hoofd = panel.isHoofd;
        var eigen = p ? (p.eenheidIds || []) : [];
        var planNaam = {}; (state.planningen || []).forEach(function (x) { planNaam[x.id] = x.naam; });
        // Hoofdplanning zonder eigen eenheden = alle eenheden zonder andere planning; met eigen eenheden = enkel die
        var hoofdVast = hoofd && !p.id;                                   // nog niet bewaard: niets te kiezen
        var hoofdAlgemeen = hoofd && eigen.length === 0;
        panel.eenhedenStart = null;
        $("gl-v2-pl-f-eenheden-label").textContent = hoofd ? "Eenheden in de hoofdplanning" : "Woningen en gebouwen";
        $("gl-v2-pl-f-eenheden-help").textContent = hoofd
            ? "Kies voor welke eenheden de hoofdplanning geldt. Zolang je niets kiest, geldt ze voor alle eenheden die geen eigen planning hebben. Eenheden in een andere planning kan je hier niet kiezen."
            : "Optioneel. Kies de woningen of het gebouw waar deze planning voor geldt. Een planning mag meerdere eenheden bundelen; een eenheid zit in hoogstens één planning, ook als de betalingsgroep meerdere eenheden heeft.";
        $("gl-v2-pl-f-eenheden").innerHTML = (state.eenheden || []).length ? (state.eenheden || []).map(function (e) {
            if (hoofd) {
                var vrijVoorHoofd = e.planningId == null || e.planningId === p.id;
                var aan = vrijVoorHoofd && (hoofdAlgemeen || eigen.indexOf(e.id) >= 0);
                return "<label class=\"gl-v2-pl-een" + (vrijVoorHoofd ? "" : " is-uit") + "\"><input type=\"checkbox\" value=\"" + e.id + "\"" + (aan ? " checked" : "") + (vrijVoorHoofd && !hoofdVast ? "" : " disabled") + " /><span>" + esc(e.naam) + "</span>" +
                    "<small>" + (vrijVoorHoofd ? esc(e.groepNaam || "") : "zit in " + esc(planNaam[e.planningId] || "een andere planning")) + "</small></label>";
            }
            var inAndere = e.planningId != null && eigen.indexOf(e.id) < 0;
            return "<label class=\"gl-v2-pl-een" + (inAndere ? " is-uit" : "") + "\"><input type=\"checkbox\" value=\"" + e.id + "\"" + (eigen.indexOf(e.id) >= 0 ? " checked" : "") + (inAndere ? " disabled" : "") + " /><span>" + esc(e.naam) + "</span>" +
                "<small>" + (inAndere ? "zit in " + esc(planNaam[e.planningId] || "een andere planning") : esc(e.groepNaam || "")) + "</small></label>";
        }).join("") : "<span class=\"gl-v2-field-help\" style=\"padding:8px 12px\">Dit project heeft nog geen woningen of gebouwen.</span>";
        $("gl-v2-pl-wrap-bouwdeel").hidden = false;
        if (hoofd) panel.eenhedenStart = hoofdVast ? null : gekozenEenheden().join(",");
        setDate("gl-v2-pl-f-pstart", p ? p.startDatum : "");
        var del = $("gl-v2-pl-panel-delete"); if (del) del.hidden = !(canDelete && p && !p.isHoofd && p.id);
        $("gl-v2-pl-panel-again").hidden = true;
        $("gl-v2-pl-panel-save").querySelector("span").textContent = bestaand ? "Opslaan" : "Planning toevoegen";
        showPanel("PLANNING", bestaand ? p.naam : "Nieuwe planning", bestaand ? (p.isHoofd ? "Hoofdplanning: geldt voor het hele project" : (p.eenhedenTekst ? "Voor " + p.eenhedenTekst : "")) : "Een planning voor een of meer woningen of een gebouw, naast de hoofdplanning");
    }
    function gekozenEenheden() {
        return Array.prototype.map.call($("gl-v2-pl-f-eenheden").querySelectorAll("input:checked:not(:disabled)"), function (i) { return parseInt(i.value, 10); });
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
    // Bouwlagen zoals wij ze noemen: 1 = gelijkvloers, 2 = 1ste verdieping, 3 = 2de verdieping, ...
    function laagNaam(n) { return n <= 1 ? "gelijkvloers" : (n - 1) + (n - 1 === 1 || n - 1 === 8 ? "ste" : "de") + " verdieping"; }
    function laagKort(n) { return n <= 1 ? "GV" : String(n - 1); }
    // Keuzeknoppen per bouwlaag (terrassen of luifels bij de afdek). data = attributen die de klik herkent.
    function lagenHtml(aantal, gekozen, data) {
        var html = "";
        for (var n = 1; n <= Math.max(1, aantal); n++) {
            var aan = (gekozen || []).indexOf(n) >= 0;
            html += "<button type=\"button\" class=\"gl-v2-pl-laag\" " + data + " data-laag=\"" + n + "\" aria-pressed=\"" + aan + "\" title=\"Afdek " + laagNaam(n) + "\" aria-label=\"Afdek " + laagNaam(n) + "\">" + laagKort(n) + "</button>";
        }
        return html;
    }
    function schoonLagen(lijst, aantal) { return (lijst || []).filter(function (n, i, a) { return n >= 1 && n <= aantal && a.indexOf(n) === i; }).sort(function (a, b) { return a - b; }); }
    function wisselLaag(lijst, n) { var i = lijst.indexOf(n); if (i >= 0) lijst.splice(i, 1); else lijst.push(n); return lijst; }

    // ── Projectsjabloon: planning, traject en schijven in één keer, aangepast aan het gebouw (design-handoff 59g/59h) ──
    // Links de antwoorden (de verborgen velden hieronder zijn de bron van waarheid, de knoppen tonen ze), rechts "Wat er gebeurt" per onderdeel.
    var pj = { gevels: [], indeel: false, nrs: {}, keuzes: [], gekozen: null, aanvraag: 0, timer: null, bezig: false, terrassen: [], ai: {}, vorige: null };
    var PJ_VELDEN = ["verd", "kelder", "dak", "lagen", "een"];
    function pjAantal() { return Math.max(1, Math.min(20, parseInt($("gl-v2-pl-pj-verd").value, 10) || 1)); }
    function pjLagen() {
        var aantal = pjAantal();
        pj.terrassen = schoonLagen(pj.terrassen, aantal);
        $("gl-v2-pl-pj-lagen").innerHTML = lagenHtml(aantal, pj.terrassen, "data-pj=\"1\"");
        $("gl-v2-pl-pj-verd-t").textContent = aantal;
        $("gl-v2-pl-pj-min").disabled = aantal <= 1; $("gl-v2-pl-pj-plus").disabled = aantal >= 20;
    }
    function pjAntwoorden() {
        return {
            verdiepingen: pjAantal(),
            kelder: $("gl-v2-pl-pj-kelder").checked, dak: $("gl-v2-pl-pj-dak").value || null,
            terrassen: schoonLagen(pj.terrassen, pjAantal()),
            eenPlanningVoorAlles: $("gl-v2-pl-pj-een").checked,
            inHoofdplanning: $("gl-v2-pl-pj-een").checked && $("gl-v2-pl-pj-hoofd").checked,
            woningenStartNa: $("gl-v2-pl-pj-q-startna").hidden ? null : $("gl-v2-pl-pj-startna").value,
            groepGrootte: $("gl-v2-pl-pj-een").checked || pj.indeel ? 0 : Math.max(0, parseInt($("gl-v2-pl-pj-groep").value, 10) || 0),
            groepen: $("gl-v2-pl-pj-een").checked || !pj.indeel ? [] : pjGroepen(),
            vervangPlanning: canDelete && $("gl-v2-pl-pj-vervang").checked,
            gevelafwerkingen: $("gl-v2-pl-pj-q-gevel").hidden ? [] : pj.gevels.slice()
        };
    }
    // Zelf indelen: per woning het nummer van de planning; hetzelfde nummer = dezelfde planning
    function pjVrij() { return (state.eenheden || []).filter(function (e) { return e.planningId == null; }); }
    function pjGroepen() {
        var perNr = {};
        pjVrij().forEach(function (e) { var n = pj.nrs[e.id]; if (n >= 1) (perNr[n] = perNr[n] || []).push(e.id); });
        return Object.keys(perNr).map(Number).sort(function (a, b) { return a - b; }).map(function (n) { return perNr[n]; });
    }
    function pjIndeelVul() {
        var n = Math.max(1, parseInt($("gl-v2-pl-pj-groep").value, 10) || 1);
        pj.nrs = {}; pjVrij().forEach(function (e, i) { pj.nrs[e.id] = Math.floor(i / n) + 1; });
    }
    function pjIndeelToon() {
        var aan = !!pj.indeel;
        $("gl-v2-pl-pj-indeel").hidden = !aan;
        $("gl-v2-pl-pj-indeel-knop").lastChild.textContent = aan ? "Terug naar vast aantal per planning" : "Woningen zelf indelen";
        if (!aan) return;
        $("gl-v2-pl-pj-indeel-lijst").innerHTML = pjVrij().map(function (e) {
            return "<label class=\"gl-v2-field\"><span class=\"gl-v2-field-label\">" + esc(e.naam) + "</span><span class=\"gl-v2-field-box\"><input type=\"number\" class=\"gl-v2-field-input\" min=\"1\" max=\"500\" step=\"1\" data-pj-nr=\"" + e.id + "\" value=\"" + (pj.nrs[e.id] || "") + "\" aria-label=\"Planning voor " + esc(e.naam) + "\" /></span></label>";
        }).join("");
    }
    // De knoppen (gl-v2-segmented) volgen de verborgen velden
    function pjSegmenten() {
        var kelder = $("gl-v2-pl-pj-kelder").checked, dak = $("gl-v2-pl-pj-dak").value || "", een = $("gl-v2-pl-pj-een").checked;
        $("gl-v2-pl-pj-kelder-seg").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-active", (b.getAttribute("data-v") === "ja") === kelder); });
        $("gl-v2-pl-pj-dak-seg").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-v") === dak); });
        $("gl-v2-pl-pj-een-seg").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-active", (b.getAttribute("data-v") === "een") === een); });
        $("gl-v2-pl-pj-hoofd-rij").hidden = !een;
        $("gl-v2-pl-pj-groep-rij").hidden = een;
        $("gl-v2-pl-pj-indeel-rij").hidden = een || pjVrij().length < 2;
        var gebouw = !!(pj.gekozen && pj.gekozen.isGebouw);
        $("gl-v2-pl-pj-indeel-rij").hidden = een || gebouw || pjVrij().length < 2;
        $("gl-v2-pl-pj-groep-rij").hidden = een || gebouw || pj.indeel;
        var vrij = (state.eenheden || []).filter(function (e) { return e.planningId == null; }).length;
        $("gl-v2-pl-pj-elk").textContent = "Per eenheid" + (vrij ? " (" + vrij + ")" : "");
        $("gl-v2-pl-pj-een-help").textContent = gebouw
            ? "Bij een appartementsgebouw is één planning de standaard: de werf loopt per bouwlaag, niet per appartement. “Per eenheid” maakt " + (vrij ? vrij + " planningen" : "een planning per eenheid") + "."
            : "Bij woningen krijgt elke woning een planning, of je zet er meerdere in één planning (hieronder). “Eén planning” zet alles samen in één planning.";
    }
    function pjHoofdRij() { pjSegmenten(); }
    // Wat de AI invulde is blauw met een AI-label tot jij het zelf wijzigt
    function pjAiMarks() {
        var kaart = { verd: "gl-v2-pl-pj-q-verd", kelder: "gl-v2-pl-pj-q-kelder", dak: "gl-v2-pl-pj-q-dak", lagen: "gl-v2-pl-pj-q-lagen", een: "gl-v2-pl-pj-q-een" };
        PJ_VELDEN.forEach(function (v) {
            var el = $(kaart[v]), aan = !!pj.ai[v]; if (!el) return;
            el.classList.toggle("is-ai", aan);
            var lbl = el.querySelector(".gl-v2-pl-pj-aib"); if (lbl) lbl.hidden = !aan;
        });
        $("gl-v2-pl-pj-lijst").classList.toggle("is-ai", !!pj.ai.sjabloon);
        $("gl-v2-pl-pj-ta-box").classList.toggle("is-ai", false);
    }
    function pjAiUit(veld) { if (pj.ai[veld]) { pj.ai[veld] = false; pjAiMarks(); } }
    function pjFout(tekst) { var el = $("gl-v2-pl-pj-fout"); el.textContent = tekst || ""; el.hidden = !tekst; }
    function nl2(n) { return String(Math.round(n * 100) / 100).replace(".", ","); }
    // "Wat er gebeurt": een kaart per onderdeel (planning, traject, schijven), wat ongemoeid blijft en een samenvatting onderaan
    function pjToon(res) {
        var host = $("gl-v2-pl-pj-voorbeeld"), fs = $("gl-v2-pl-pj-fs"), ok = $("gl-v2-pl-pj-ok");
        if (res.fout) { pjFout(res.fout); ok.disabled = true; return; }
        pjFout("");
        var stappen = res.stappen || [], opm = res.opmerkingen || [], schijven = res.schijven || [];
        var planning = stappen.filter(function (s) { return /^Planning /.test(s); })[0], traject = stappen.filter(function (s) { return /^Traject /.test(s); })[0];
        var schijfStap = stappen.filter(function (s) { return /^Schijven: /.test(s); })[0];
        var rest = stappen.filter(function (s) { return s !== planning && s !== traject && s !== schijfStap && !/hangen? nu aan/.test(s) && !/hangt nu aan/.test(s); });
        var koppel = stappen.filter(function (s) { return /nu aan een taak of fase/.test(s); })[0];
        var blijft = [], waarschuw = [];
        opm.forEach(function (o) { (/blijft|blijven|bestaat al|al een|al schijven|ongewijzigd|ongemoeid|geen eenheden|geen betalingsgroep/i.test(o) ? blijft : waarschuw).push(o); });
        var kaarten = "";
        if (planning) {
            var m = planning.match(/^Planning “(.*?)”(?: toegepast)?: (.*?)\.?$/);
            kaarten += "<div class=\"gl-v2-pl-pj-kaart\"><div class=\"gl-v2-pl-pj-kk\"><i class=\"ph ph-calendar-check\" aria-hidden=\"true\"></i><b>Planning</b><span class=\"gl-v2-pl-pj-nieuw\">nieuw</span></div><p>" +
                (m ? "<b>" + esc(m[1]) + "</b> · " + esc(m[2]) : esc(planning)) + "</p></div>";
        }
        if (traject) {
            var mt = traject.match(/^Traject “(.*?)”/);
            kaarten += "<div class=\"gl-v2-pl-pj-kaart\"><div class=\"gl-v2-pl-pj-kk\"><i class=\"ph ph-git-branch\" aria-hidden=\"true\"></i><b>Traject</b><span class=\"gl-v2-pl-pj-nieuw\">nieuw</span></div><p>" + (mt ? "“" + esc(mt[1]) + "”" : esc(traject)) + "</p></div>";
        }
        if (schijven.length) {
            var groepen = schijfStap ? (schijfStap.match(/ in (.*?)\.?$/) || [])[1] : "";
            var aantalGroepen = groepen ? groepen.split(/, | en /).length : 0;
            kaarten += "<div class=\"gl-v2-pl-pj-kaart\"><div class=\"gl-v2-pl-pj-kk\"><i class=\"ph ph-list-checks\" aria-hidden=\"true\"></i><b>Schijven</b><span class=\"gl-v2-pl-pj-nieuw\">" + schijven.length + (aantalGroepen ? " · " + aantalGroepen + (aantalGroepen === 1 ? " groep" : " groepen") : "") + "</span></div>" +
                (groepen ? "<p>in " + esc(groepen) + "</p>" : "") +
                "<ol class=\"gl-v2-pl-pj-sl\">" + schijven.map(function (s) {
                    var laag = waarschuw.some(function (w) { return w.indexOf(s.naam) >= 0 && /minimum|onder/i.test(w); });
                    return "<li" + (laag ? " class=\"is-laag\"" : "") + "><span>" + esc(s.naam) + "</span><b>" + nl2(s.percentage) + " %</b></li>";
                }).join("") + "</ol>" +
                (koppel ? "<p class=\"gl-v2-pl-pj-sub2\">" + esc(koppel) + "</p>" : "") + "</div>";
        }
        rest.forEach(function (s) { kaarten += "<div class=\"gl-v2-pl-pj-kaart\"><p>" + esc(s) + "</p></div>"; });
        if (waarschuw.length) kaarten += "<div class=\"gl-v2-notice gl-v2-notice-compact is-warning\"><i class=\"ph ph-warning\" aria-hidden=\"true\"></i><span>" + waarschuw.map(esc).join("<br>") + "</span></div>";
        if (blijft.length) kaarten += "<div class=\"gl-v2-pl-pj-keep\"><i class=\"ph ph-lock-simple\" aria-hidden=\"true\"></i><span><b>Blijft ongemoeid:</b> " + blijft.map(esc).join(" ") + "</span></div>";
        host.innerHTML = kaarten || "<p class=\"gl-v2-pl-pj-leeg\">Er valt niets toe te passen.</p>";
        var delen = [];
        if (planning) delen.push("<b>1 planning</b>");
        if (traject) delen.push("<b>1 traject</b>");
        if (schijven.length) delen.push("<b>" + schijven.length + " schijven</b>");
        fs.innerHTML = delen.length ? "Maakt " + (delen.length > 1 ? delen.slice(0, -1).join(", ") + " en " + delen[delen.length - 1] : delen[0]) : "";
        ok.disabled = !stappen.length || pj.bezig;
    }
    function pjVoorbeeld() {
        if (!pj.gekozen) return;
        var nr = ++pj.aanvraag;
        postJson(cfg.projectSjabloonVoorbeeldUrl, { sjabloonId: pj.gekozen.id, antwoorden: pjAntwoorden() })
            .then(function (res) { if (nr === pj.aanvraag) pjToon(res); })
            .catch(function (e) { if (nr === pj.aanvraag) pjFout(e.message || "Het voorbeeld kon niet geladen worden."); });
    }
    function pjPlanVoorbeeld() { pjControleWis(); window.clearTimeout(pj.timer); pj.timer = window.setTimeout(pjVoorbeeld, 300); }
    // Wat de AI aannam, vroeg en doorrekende: enkel tonen na een voorstel. De doorrekening vervalt zodra jij iets wijzigt.
    function pjControleWis() { var c = $("gl-v2-pl-pj-ai-controle"); if (c) c.textContent = ""; }
    function pjAiDetails(res) {
        var vul = function (id, lijst) { var ul = $(id); ul.innerHTML = ""; (lijst || []).forEach(function (x) { var li = document.createElement("li"); li.textContent = x; ul.appendChild(li); }); };
        vul("gl-v2-pl-pj-ai-aannames", res.aannames); vul("gl-v2-pl-pj-ai-vragen", res.vragen);
        $("gl-v2-pl-pj-ai-vragen-kop").hidden = !(res.vragen && res.vragen.length);
        $("gl-v2-pl-pj-ai-controle").textContent = res.controle || "";
        $("gl-v2-pl-pj-ai-details").hidden = !((res.aannames && res.aannames.length) || (res.vragen && res.vragen.length) || res.controle);
    }
    function pjZetAntwoorden(a) {
        $("gl-v2-pl-pj-verd").value = a.verdiepingen || 1; $("gl-v2-pl-pj-kelder").checked = !!a.kelder; $("gl-v2-pl-pj-dak").value = a.dak || "";
        $("gl-v2-pl-pj-een").checked = !!a.eenPlanningVoorAlles; pj.terrassen = (a.terrassen || []).slice(); pjLagen(); pjSegmenten();
    }
    function pjKaartRadio(id) { document.querySelectorAll("input[name=\"gl-v2-pl-pj-keuze\"]").forEach(function (r) { r.checked = parseInt(r.value, 10) === id; }); }
    function pjKies(k) {
        pj.gekozen = k; pj.ai = {};
        // Een sjabloon voor een gebouw (blokken per bouwlaag) start met één planning voor het gebouw; woningen krijgen er elk een
        pjZetAntwoorden({ verdiepingen: k.verdiepingen, kelder: k.kelder, dak: k.dak, terrassen: [], eenPlanningVoorAlles: !!k.isGebouw });
        $("gl-v2-pl-pj-hoofd").checked = true; $("gl-v2-pl-pj-groep").value = ""; pj.indeel = false; pj.nrs = {}; pjIndeelToon(); pjSegmenten();
        $("gl-v2-pl-pj-vervang").checked = false; $("gl-v2-pl-pj-q-vervang").hidden = !canDelete;
        pj.gevels = k.heeftGevel ? ["steen"] : []; $("gl-v2-pl-pj-q-gevel").hidden = !k.heeftGevel; pjGevelToon();   // de stenen gevel is de gewone keuze; alles is optioneel
        // Startmoment van de woningen: enkel bij een sjabloon met de planning van het project zelf; standaard wat het sjabloon zegt
        $("gl-v2-pl-pj-q-startna").hidden = !k.woningenStartNa;
        var startItems = [
            { value: "GEEN", text: "Meteen, op de aanvangsdatum van de bouwwerken" }, { value: "WEGENIS_FUNDERING", text: "Na de wegenfundering" },
            { value: "WEGENIS_NUTSVOORZIENINGEN", text: "Na de nutsvoorzieningen" }, { value: "WEGENIS_VERHARDING", text: "Na de wegverharding" },
            { value: "WEGENIS_VOORLOPIGE_OPLEVERING", text: "Na de voorlopige oplevering van de wegenis" }
        ];
        var startWaarde = k.woningenStartNa || "GEEN";
        if (!startItems.some(function (o) { return o.value === startWaarde; })) startItems.push({ value: startWaarde, text: "Na " + startWaarde });
        setSelect("gl-v2-pl-pj-startna", startItems, startWaarde);
        $("gl-v2-pl-pj-ainote").hidden = true; pjAiMarks();
        $("gl-v2-pl-pj-rest").hidden = false;
        $("gl-v2-pl-pj-leeg") && ($("gl-v2-pl-pj-leeg").textContent = "Even rekenen …");
        pjVoorbeeld();
    }
    function openPj() {
        if (!canWriteBasis) return;
        pj.gekozen = null; pj.ai = {}; pj.vorige = null; pjFout("");
        $("gl-v2-pl-pj-rest").hidden = true; $("gl-v2-pl-pj-ok").disabled = true; $("gl-v2-pl-pj-beschrijving").value = "";
        $("gl-v2-pl-pj-ai-details").hidden = true; $("gl-v2-pl-pj-ainote").hidden = true;
        $("gl-v2-pl-pj-ai-uitleg").textContent = "Geen namen, adressen, telefoonnummers of bedragen: die gaan niet naar de AI.";
        $("gl-v2-pl-pj-fs").textContent = ""; pjAiMarks();
        $("gl-v2-pl-pj-voorbeeld").innerHTML = "<p class=\"gl-v2-pl-pj-leeg\" id=\"gl-v2-pl-pj-leeg\">Kies links een projectsjabloon om te zien wat er gebeurt.</p>";
        var eenh = (state.eenheden || []).length;
        $("gl-v2-pl-pj-sub").textContent = ($("gl-v2-pl-pj-sub").getAttribute("data-project") || "") + (eenh ? " · " + eenh + (eenh === 1 ? " eenheid" : " eenheden") : "") + " · je ziet eerst wat er gebeurt; wat al bestaat blijft ongemoeid";
        var lijst = $("gl-v2-pl-pj-lijst"); lijst.innerHTML = "<span class=\"gl-v2-field-help\">Laden…</span>";
        $("gl-v2-pl-pj-info").textContent = "";
        var m = modal("gl-v2-pl-pj"); if (m) m.show();
        fetch(cfg.projectSjablonenUrl, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
            .then(function (l) {
                pj.keuzes = l || [];
                if (!pj.keuzes.length) { lijst.innerHTML = ""; $("gl-v2-pl-pj-info").textContent = "Er zijn nog geen projectsjablonen. Iemand met het recht Planningssjablonen maakt ze aan onder Instellingen › Projectsjablonen."; return; }
                lijst.innerHTML = pj.keuzes.map(function (k) {
                    return "<label class=\"gl-v2-radio-card\"><input type=\"radio\" name=\"gl-v2-pl-pj-keuze\" value=\"" + k.id + "\" /><span><b>" + esc(k.naam) + "</b><small>" + esc(k.kern || k.beschrijving || "") + "</small></span></label>";
                }).join("");
                if (pj.keuzes.length === 1) { var r = lijst.querySelector("input"); r.checked = true; pjKies(pj.keuzes[0]); }
                pjBewaardeKeuze(lijst);
            })
            .catch(function () { lijst.innerHTML = ""; $("gl-v2-pl-pj-info").textContent = "De projectsjablonen konden niet geladen worden. Probeer het zo opnieuw."; });
    }
    // Het sjabloon en de antwoorden die op het project bewaard zijn (bij het aanmaken of de vorige keer toepassen), vooraf ingevuld
    function pjBewaardeKeuze(lijst) {
        if (!cfg.projectKeuzeUrl) return;
        fetch(cfg.projectKeuzeUrl, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (bk) {
                if (!bk || !bk.sjabloonId) return;
                var k = pj.keuzes.filter(function (x) { return x.id === bk.sjabloonId; })[0];
                if (!k) return;
                var radio = lijst.querySelector("input[value=\"" + k.id + "\"]"); if (radio) radio.checked = true;
                pjKies(k);
                var a = bk.antwoorden || {};
                pjZetAntwoorden({ verdiepingen: a.verdiepingen || k.verdiepingen, kelder: !!a.kelder, dak: a.dak || "", terrassen: a.terrassen || [], eenPlanningVoorAlles: a.eenPlanningVoorAlles != null ? !!a.eenPlanningVoorAlles : !!k.isGebouw });
                pj.gevels = k.heeftGevel ? (a.gevelafwerkingen || []).slice() : [];
                pjGevelToon(); pjPlanVoorbeeld();
            })
            .catch(function () { /* geen bewaarde keuze: niets te doen */ });
    }
    $("gl-v2-pl-pj").addEventListener("change", function (e) {
        if (e.target.name === "gl-v2-pl-pj-keuze") { var id = parseInt(e.target.value, 10); var k = pj.keuzes.filter(function (x) { return x.id === id; })[0]; if (k) pjKies(k); return; }
        if (e.target.id === "gl-v2-pl-pj-hoofd" || e.target.id === "gl-v2-pl-pj-vervang") pjPlanVoorbeeld();
        if (e.target.id === "gl-v2-pl-pj-startna" || e.target.id === "gl-v2-pl-pj-groep") pjPlanVoorbeeld();
        if (e.target.hasAttribute && e.target.hasAttribute("data-pj-nr")) { pj.nrs[e.target.getAttribute("data-pj-nr")] = Math.max(0, parseInt(e.target.value, 10) || 0); pjPlanVoorbeeld(); }
    });
    // Gevelafwerkingen: meerdere tegelijk (steen, pleister, hout, aluminium), allemaal optioneel
    function pjGevelToon() {
        $("gl-v2-pl-pj-gevel-seg").querySelectorAll("button").forEach(function (b) { var aan = pj.gevels.indexOf(b.getAttribute("data-v")) >= 0; b.classList.toggle("is-active", aan); b.setAttribute("aria-pressed", aan ? "true" : "false"); });
    }
    $("gl-v2-pl-pj-gevel-seg").addEventListener("click", function (e) {
        var b = e.target.closest("button[data-v]"); if (!b) return;
        var v = b.getAttribute("data-v"), i = pj.gevels.indexOf(v);
        if (i >= 0) pj.gevels.splice(i, 1); else pj.gevels.push(v);
        pjGevelToon(); pjPlanVoorbeeld();
    });
    $("gl-v2-pl-pj-indeel-knop").addEventListener("click", function () {
        pj.indeel = !pj.indeel;
        if (pj.indeel) pjIndeelVul();
        pjIndeelToon(); pjSegmenten(); pjPlanVoorbeeld();
    });
    // Antwoorden: de knoppen zetten de verborgen velden, wissen het AI-label van die vraag en rekenen opnieuw
    function pjWijzig(veld, zet) { zet(); pjAiUit(veld); pjSegmenten(); pjPlanVoorbeeld(); }
    $("gl-v2-pl-pj-min").addEventListener("click", function () { pjWijzig("verd", function () { $("gl-v2-pl-pj-verd").value = Math.max(1, pjAantal() - 1); pjLagen(); }); });
    $("gl-v2-pl-pj-plus").addEventListener("click", function () { pjWijzig("verd", function () { $("gl-v2-pl-pj-verd").value = Math.min(20, pjAantal() + 1); pjLagen(); }); });
    $("gl-v2-pl-pj-kelder-seg").addEventListener("click", function (e) { var b = e.target.closest("button[data-v]"); if (!b) return; pjWijzig("kelder", function () { $("gl-v2-pl-pj-kelder").checked = b.getAttribute("data-v") === "ja"; }); });
    $("gl-v2-pl-pj-dak-seg").addEventListener("click", function (e) { var b = e.target.closest("button[data-v]"); if (!b) return; pjWijzig("dak", function () { $("gl-v2-pl-pj-dak").value = b.getAttribute("data-v"); }); });
    $("gl-v2-pl-pj-een-seg").addEventListener("click", function (e) { var b = e.target.closest("button[data-v]"); if (!b) return; pjWijzig("een", function () { $("gl-v2-pl-pj-een").checked = b.getAttribute("data-v") === "een"; }); });
    $("gl-v2-pl-pj-lagen").addEventListener("click", function (e) {
        var b = e.target.closest("button[data-laag]"); if (!b) return;
        pjWijzig("lagen", function () { wisselLaag(pj.terrassen, parseInt(b.getAttribute("data-laag"), 10)); pjLagen(); });
    });
    $("gl-v2-pl-pj-ai").addEventListener("click", function () {
        var tekst = $("gl-v2-pl-pj-beschrijving").value.trim(), uitleg = $("gl-v2-pl-pj-ai-uitleg");
        if (!tekst) { uitleg.textContent = "Beschrijf eerst het gebouw."; return; }
        var knop = $("gl-v2-pl-pj-ai"); knop.disabled = true; uitleg.textContent = "De AI denkt na…";
        postJson(cfg.projectSjabloonAiUrl, { beschrijving: tekst }).then(function (res) {
            knop.disabled = false;
            if (!res.ok) { uitleg.textContent = res.fout || "De AI kon geen voorstel doen."; return; }
            var a = res.antwoorden;
            // Voor "Ongedaan maken": de toestand van vóór het voorstel
            var voor = { gekozen: pj.gekozen, antwoorden: pj.gekozen ? pjAntwoorden() : null, een: $("gl-v2-pl-pj-een").checked };
            var vorigSjabloon = pj.gekozen ? pj.gekozen.id : null;
            // Eerst het sjabloon dat de AI koos (dat zet de standaardwaarden), daarna de antwoorden uit de beschrijving.
            if (res.sjabloonId) {
                var gekozen = pj.keuzes.filter(function (x) { return x.id === res.sjabloonId; })[0];
                if (gekozen) { pjKaartRadio(gekozen.id); pjKies(gekozen); }
            } else if (!pj.gekozen) {
                pjAiDetails(res);
                uitleg.textContent = (res.sjabloonReden || "Geen projectsjabloon past.") + " Kies zelf een sjabloon.";
                return;
            }
            var basis = pjAntwoorden();
            pjZetAntwoorden({ verdiepingen: a.verdiepingen, kelder: a.kelder, dak: a.dak, terrassen: a.terrassen, eenPlanningVoorAlles: a.eenPlanningVoorAlles });
            // Wat de AI invulde (en wat afwijkt van de standaard van het sjabloon) wordt blauw
            pj.ai = { sjabloon: !!res.sjabloonId && res.sjabloonId !== vorigSjabloon, verd: a.verdiepingen !== basis.verdiepingen, kelder: !!a.kelder !== !!basis.kelder, dak: (a.dak || "") !== (basis.dak || ""), lagen: (a.terrassen || []).length > 0, een: !!a.eenPlanningVoorAlles };
            pj.vorige = voor;
            var aantal = PJ_VELDEN.filter(function (v) { return pj.ai[v]; }).length + (pj.ai.sjabloon ? 1 : 0);
            var gekozenNaam = res.sjabloonId && pj.gekozen ? pj.gekozen.naam : null;
            $("gl-v2-pl-pj-ainote-tekst").innerHTML = "<b>Voorstel van de AI</b> — " + (gekozenNaam ? "sjabloon “" + esc(gekozenNaam) + "”" + (res.sjabloonReden ? " (" + esc(res.sjabloonReden) + ")" : "") + " en " : "") + aantal + (aantal === 1 ? " antwoord" : " antwoorden") + " ingevuld (blauw gemarkeerd). Kijk ze na; je kan alles nog wijzigen." + (res.toelichting ? " " + esc(res.toelichting) : "");
            $("gl-v2-pl-pj-ainote").hidden = false; uitleg.textContent = "";
            pjAiMarks(); pjAiDetails(res); pjVoorbeeld();
        }).catch(function (e) { knop.disabled = false; uitleg.textContent = e.message || "De AI kon niet bereikt worden."; });
    });
    $("gl-v2-pl-pj-ongedaan").addEventListener("click", function () {
        var v = pj.vorige; pj.ai = {}; $("gl-v2-pl-pj-ainote").hidden = true; $("gl-v2-pl-pj-ai-details").hidden = true; pj.vorige = null;
        if (v && v.gekozen) { pjKaartRadio(v.gekozen.id); pjKies(v.gekozen); if (v.antwoorden) { pjZetAntwoorden(v.antwoorden); pjVoorbeeld(); } }
        else { pj.gekozen = null; pjKaartRadio(-1); $("gl-v2-pl-pj-rest").hidden = true; $("gl-v2-pl-pj-ok").disabled = true; $("gl-v2-pl-pj-fs").textContent = ""; $("gl-v2-pl-pj-voorbeeld").innerHTML = "<p class=\"gl-v2-pl-pj-leeg\">Kies links een projectsjabloon om te zien wat er gebeurt.</p>"; }
        pjAiMarks();
    });
    $("gl-v2-pl-pj").querySelector("form").addEventListener("submit", function (e) {
        e.preventDefault();
        if (!pj.gekozen || pj.bezig) return;
        var voerUit = function () {
        pj.bezig = true; $("gl-v2-pl-pj-ok").disabled = true; pjFout("");
        postJson(cfg.projectSjabloonToepassenUrl, { sjabloonId: pj.gekozen.id, antwoorden: pjAntwoorden() }).then(function (res) {
            pj.bezig = false;
            if (res.fout) { pjFout(res.fout); $("gl-v2-pl-pj-ok").disabled = false; return; }
            var m = modal("gl-v2-pl-pj"); if (m) m.hide();
            herlaadStaat();
            toast("success", "Projectsjabloon toegepast", (res.stappen || []).join(" ") + ((res.opmerkingen || []).length ? " Let op: " + res.opmerkingen.join(" ") : ""));
        }).catch(function (err) { pj.bezig = false; $("gl-v2-pl-pj-ok").disabled = false; pjFout(err.message || "Toepassen mislukt. Probeer het zo opnieuw."); });
        };
        if (canDelete && $("gl-v2-pl-pj-vervang").checked && window.GlV2Bevestig)
            window.GlV2Bevestig({ title: "Bestaande planning verwijderen?", desc: "Alle plannings van de woningen en de fases en taken van de hoofdplanning worden verwijderd, met alle voortgang. Daarna komt de nieuwe planning. Dit kan je niet ongedaan maken.", ok: "Verwijderen en toepassen", tone: "danger", icon: "ph-trash" }).then(function (ok) { if (ok) voerUit(); });
        else voerUit();
    });

    function sjabloonGekozen() {
        var id = parseInt($("gl-v2-pl-sj-kies").value, 10);
        return (state.sjablonen || []).filter(function (s) { return s.id === id; })[0] || null;
    }
    var sjRijen = [];   // { id, naam, aan, verdiepingen, kelder, dak, terrassen } in de volgorde van de lijst
    var sjAlle = { verdiepingen: 1, kelder: false, dak: "", terrassen: [] };
    function sjOpties(s) { return (s.heeftVerdiepingen ? 1 : 0) + (s.heeftKelder ? 1 : 0) + (s.heeftDak ? 1 : 0) + (s.heeftTerras ? 1 : 0); }
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
        var cols = "style=\"grid-template-columns: 26px minmax(0, 1fr)" + (s.heeftVerdiepingen ? " 86px" : "") + (s.heeftKelder ? " 56px" : "") + (s.heeftDak ? " 112px" : "") + (s.heeftTerras ? " minmax(150px, 1.2fr)" : "") + " 58px\"";
        var kolommen = "<span></span><span>Naam</span>" + (s.heeftVerdiepingen ? "<span>Bouwlagen</span>" : "") + (s.heeftKelder ? "<span>Kelder</span>" : "") + (s.heeftDak ? "<span>Dak</span>" : "") + (s.heeftTerras ? "<span>Terrassen of luifels bij afdek van</span>" : "") + "<span></span>";
        function velden(r, alle) {
            var id = alle ? "alle" : r.id;
            return (s.heeftVerdiepingen ? "<span><input type=\"number\" min=\"1\" max=\"30\" value=\"" + r.verdiepingen + "\" data-veld=\"verdiepingen\" data-id=\"" + id + "\" aria-label=\"Bouwlagen (gelijkvloers + verdiepingen)\" /></span>" : "") +
                (s.heeftKelder ? "<span><input type=\"checkbox\" " + (r.kelder ? "checked" : "") + " data-veld=\"kelder\" data-id=\"" + id + "\" aria-label=\"Kelder\" /></span>" : "") +
                (s.heeftDak ? "<span>" + dakKeuze(r.dak, id) + "</span>" : "") +
                (s.heeftTerras ? "<span class=\"gl-v2-pl-lagen\">" + lagenHtml(s.heeftVerdiepingen ? r.verdiepingen : 1, r.terrassen, "data-sj=\"" + id + "\"") + "</span>" : "");
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
        var v = []; if (s.heeftVerdiepingen) v.push("bouwlagen"); if (s.heeftKelder) v.push("kelder"); if (s.heeftDak) v.push("dak"); if (s.heeftTerras) v.push("terrassen of luifels");
        el.textContent = (s.beschrijving ? s.beschrijving + " · " : "") + s.aantalBlokken + (s.aantalBlokken === 1 ? " blok" : " blokken") + ", " + s.aantalTaken + (s.aantalTaken === 1 ? " taak" : " taken") + (v.length ? ". Vraagt naar: " + v.join(", ") + "." : ".");
        $("gl-v2-pl-sj-ok").disabled = false;
        $("gl-v2-pl-sj-verwijder").hidden = !canSjabloonDelete;
    }
    function openSjToepas() {
        if (!canWrite) return;
        var lijst = state.sjablonen || [];
        $("gl-v2-pl-sj-fout").hidden = true;
        setSelect("gl-v2-pl-sj-kies", lijst.map(function (s) { return { value: String(s.id), text: s.naam }; }), lijst.length ? String(lijst[0].id) : "");
        sjAlle = { verdiepingen: 1, kelder: false, dak: "", terrassen: [] };
        // Rijen: de bestaande plannings van woningen (vullen of vervangen) en elke eenheid die nog in geen planning zit (nieuwe planning).
        sjRijen = (state.planningen || []).filter(function (p) { return !p.isHoofd; })
            .map(function (p) { return { id: "p" + p.id, planningId: p.id, naam: p.naam, taken: p.aantalTaken || 0, aan: false, verdiepingen: 1, kelder: false, dak: "", terrassen: [] }; })
            .concat((state.eenheden || []).filter(function (e) { return e.planningId == null; })
                .map(function (e) { return { id: "e" + e.id, eenheidId: e.id, naam: e.naam, nieuw: true, taken: 0, aan: false, verdiepingen: 1, kelder: false, dak: "", terrassen: [] }; }));
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
        if (veld === "verdiepingen") { sjAlle.terrassen = schoonLagen(sjAlle.terrassen, sjAlle.verdiepingen); sjRijen.forEach(function (r) { r.terrassen = schoonLagen(r.terrassen, r.verdiepingen); }); }
        renderSjTabel();
    });
    $("gl-v2-pl-sj-tabel").addEventListener("click", function (e) {
        var lb = e.target.closest("button[data-laag]");
        if (lb) {
            var lid = lb.getAttribute("data-sj"), ln = parseInt(lb.getAttribute("data-laag"), 10);
            if (lid === "alle") { wisselLaag(sjAlle.terrassen, ln); sjRijen.forEach(function (r) { r.terrassen = sjAlle.terrassen.slice(); }); }
            else { var lr = sjRijen.filter(function (x) { return String(x.id) === lid; })[0]; if (lr) wisselLaag(lr.terrassen, ln); }
            renderSjTabel(); return;
        }
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
            return { planningId: r.planningId || null, eenheidIds: r.eenheidId ? [r.eenheidId] : [], verdiepingen: s.heeftVerdiepingen ? r.verdiepingen : 1, kelder: s.heeftKelder ? !!r.kelder : false, dak: s.heeftDak && r.dak ? r.dak : null, terrassen: s.heeftTerras ? schoonLagen(r.terrassen, s.heeftVerdiepingen ? r.verdiepingen : 1) : [], vervang: r.taken > 0 };
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
        imp = { koppelingen: {}, stap: 0, data: null, filter: "alles", doel: "nieuw", modus: "toevoegen", start: "bestand", naam: "", naamAangepast: false, eenheden: [], eenhedenAangepast: false, resultaat: null, bezig: false };
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
            var hu = impHuidig();
            var vrij = (state.eenheden || []).filter(function (e) { return e.planningId == null || (!nieuw && hu && e.planningId === hu.id); });
            if (!nieuw && hu && !imp.eenhedenAangepast) imp.eenheden = (hu.eenheidIds || []).slice();
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
            opdracht.naam = imp.naam.trim();
        }
        opdracht.eenheidIds = imp.eenheden;
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
                imp.eenhedenAangepast = true;
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
        if (e.target.closest(".js-pl-pj")) openPj();
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
            var tekst = m.naam + (m.eenheidNaam ? " · " + m.eenheidNaam : "") + (m.faseNaam ? " · " + m.faseNaam : "");
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

    // ── Koppeling met een betalingsschijf: de eenheden van deze planning bereiken de schijf zodra de taak of fase klaar is ──
    function schijfItems(sleutel) {
        return (state.schijfOpties || []).map(function (o) {
            var tekst = o.tekst;
            if (o.gekoppeldAanSleutel && o.gekoppeldAanSleutel !== sleutel) tekst += " (nu aan " + o.gekoppeldAanTekst + ")";
            return { value: String(o.stageId), text: tekst };
        });
    }
    function schijfHuidig(sleutel) {
        return (state.schijfOpties || []).filter(function (x) { return x.gekoppeldAanSleutel === sleutel; }).map(function (x) { return x.stageId; });
    }
    // Meerdere schijven per taak of fase: een lijst met vinkjes en een zoekveld
    function toonSchijf(wrapId, sleutel) {
        var wrap = $(wrapId), items = schijfItems(sleutel), lijst = $(wrapId + "-lijst"), zoek = $(wrapId + "-zoek");
        // Altijd tonen (behalve in de globale weergave), ook zonder schijven, zodat duidelijk is waarom er niets te kiezen valt
        wrap.hidden = !sleutel || state.actiefPlanningId === GLOBAAL;
        var huidig = items.length ? schijfHuidig(sleutel) : [];
        if (panel) panel.schijfOud = huidig;
        zoek.value = ""; zoek.disabled = !items.length;
        if (wrap.hidden) { lijst.innerHTML = ""; return; }
        lijst.innerHTML = items.length ? items.map(function (it) {
            var aan = huidig.indexOf(parseInt(it.value, 10)) >= 0;
            return "<label class=\"gl-v2-pl-een\" data-zoek=\"" + esc(it.text.toLowerCase()) + "\"><input type=\"checkbox\" value=\"" + it.value + "\"" + (aan ? " checked" : "") + " /><span>" + esc(it.text) + "</span></label>";
        }).join("") : "<span class=\"gl-v2-field-help\" style=\"padding:8px 12px\">Geen schijven: de eenheden van deze planning hebben geen betalingsgroep.</span>";
    }
    function schijfZoek(wrapId) {
        var q = $(wrapId + "-zoek").value.trim().toLowerCase(), n = 0, lijst = $(wrapId + "-lijst");
        Array.prototype.forEach.call(lijst.querySelectorAll(".gl-v2-pl-een"), function (l) {
            var toon = !q || (l.getAttribute("data-zoek") || "").indexOf(q) >= 0;
            l.hidden = !toon; if (toon) n++;
        });
        var leeg = lijst.querySelector(".gl-v2-pl-zoek-leeg");
        if (!n && q && !leeg) { var s = document.createElement("span"); s.className = "gl-v2-field-help gl-v2-pl-zoek-leeg"; s.style.padding = "8px 12px"; s.textContent = "Geen schijf gevonden."; lijst.appendChild(s); }
        else if ((n || !q) && leeg) leeg.remove();
    }
    ["gl-v2-pl-wrap-schijf", "gl-v2-pl-wrap-schijff"].forEach(function (id) {
        $(id + "-zoek").addEventListener("input", function () { schijfZoek(id); });
        $(id + "-zoek").addEventListener("keydown", function (e) { if (e.key === "Enter") e.preventDefault(); });
    });
    function schijfKeuze() {
        if (!panel || !panel.key) return null;
        var fase = panel.mode === "fase-bewerken";
        var wrapId = fase ? "gl-v2-pl-wrap-schijff" : "gl-v2-pl-wrap-schijf";
        if ($(wrapId).hidden) return null;
        var nu = Array.prototype.map.call($(wrapId + "-lijst").querySelectorAll("input:checked"), function (i) { return parseInt(i.value, 10); });
        var oud = panel.schijfOud || [];
        var toe = nu.filter(function (x) { return oud.indexOf(x) < 0; }), weg = oud.filter(function (x) { return nu.indexOf(x) < 0; });
        if (!toe.length && !weg.length) return null;
        return { soort: fase ? "fase" : "taak", id: panel.id, toe: toe, weg: weg };
    }
    function schijfPost(url, body, ok) {
        return fetch(url, {
            method: "POST", credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest" },
            body: JSON.stringify(Object.assign({ planningId: state.actiefPlanningId || 0 }, body))
        }).then(function (r) {
            if (r.status === 403) throw new Error("Je hebt hiervoor geen schrijfrecht op de facturatie.");
            if (!r.ok) throw new Error("Dat lukte niet. Probeer het zo opnieuw.");
            return r.json();
        }).then(function (res) {
            if (!res.ok) { toonFout("Dat kan niet", res.fout || "De wijziging is niet bewaard."); return; }
            state = res.staat; render(); if (ok) ok();
        }).catch(function (e) { toonFout("Dat lukte niet", e.message || "Controleer je verbinding en probeer opnieuw."); });
    }
    // Eerst de schijf koppelen, daarna (in volgorde) de rest
    function schijfNa(sz, daarna) {
        if (!sz) { if (daarna) daarna(); return; }
        // Eerst de weggehaalde, dan de nieuwe koppelingen, na elkaar (elke oproep geeft de nieuwe staat terug)
        var stappen = sz.weg.map(function (s) { return { soort: sz.soort, id: sz.id, stageId: null, vorigeStageId: s }; })
            .concat(sz.toe.map(function (s) { return { soort: sz.soort, id: sz.id, stageId: s }; }));
        var keten = Promise.resolve();
        stappen.forEach(function (b) { keten = keten.then(function () { return schijfPost(cfg.schijfKoppelUrl, b); }); });
        keten.then(function () {
            if (sz.toe.length) toast("success", sz.toe.length === 1 ? "Schijf gekoppeld" : sz.toe.length + " schijven gekoppeld", "Je krijgt de vraag om de schijf te markeren zodra dit klaar is.");
            else toast("success", sz.weg.length === 1 ? "Koppeling weggehaald" : "Koppelingen weggehaald", "De schijf hangt niet meer aan deze plek.");
            if (daarna) daarna();
        });
    }
    // Dezelfde schijf in meerdere betalingsgroepen: één voor één markeren (elke aanroep levert een nieuwe staat op).
    function schijfMarkeerLijst(ids) {
        if (!ids.length) return;
        if (ids.length === 1) { schijfMarkeer(ids[0], false); return; }
        var totaal = 0;
        state.rijen.forEach(function (r) { (r.schijven || []).forEach(function (s) { if (ids.indexOf(s.stageId) >= 0) totaal += s.teMarkeren; }); });
        var rest = ids.slice();
        (function volgende() {
            var id = rest.shift();
            schijfPost(cfg.schijfMarkeerUrl, { stageId: id, terugzetten: false }, function () {
                if (rest.length) volgende();
                else toast("success", "Schijf bereikt", "Gemarkeerd voor " + totaal + (totaal === 1 ? " eenheid" : " eenheden") + ". De schijf is nu factureerbaar.");
            });
        })();
    }
    function schijfMarkeer(stageId, terugzetten) {
        var link = null;
        state.rijen.forEach(function (r) { (r.schijven || []).forEach(function (s) { if (s.stageId === stageId) link = s; }); });
        var n = link ? (terugzetten ? link.aantalBereikt : link.teMarkeren) : 0;
        schijfPost(cfg.schijfMarkeerUrl, { stageId: stageId, terugzetten: !!terugzetten }, function () {
            toast("success", terugzetten ? "Schijf teruggezet" : "Schijf bereikt", terugzetten ? "Gefactureerde eenheden blijven bereikt." : "Gemarkeerd voor " + n + (n === 1 ? " eenheid" : " eenheden") + ". De schijf is nu factureerbaar.");
        });
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
        toonSchijf("gl-v2-pl-wrap-schijf", key);

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
        toonSchijf("gl-v2-pl-wrap-schijff", r.sleutel);
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
            // Hoofdplanning: enkel meesturen als de keuze gewijzigd is (anders blijft "alle eenheden zonder planning" gelden)
            if (!panel.isHoofd || (panel.eenhedenStart != null && gekozenEenheden().join(",") !== panel.eenhedenStart)) pv.eenheden = gekozenEenheden();
            if (panel.mode === "planning-nieuw") send({ type: "planningToevoegen", planningId: 0, velden: pv }, { onError: onError, after: closePanel });
            else send({ type: "planningWijzig", planningId: panel.id, velden: pv }, { onError: onError, after: closePanel });
        } else if (panel.mode === "taak-bewerken") {
            var w = verzamelTaakVelden(false);
            if (isNaN(w.duur) || w.duur < 0) { formError("De duur moet een aantal werkdagen zijn (0 of meer)."); return; }
            if (w.beperking && !w.beperkingDatum) { formError("Kies een datum bij de beperking."); return; }
            var kzTaak = koppelKeuze(), szTaak = schijfKeuze();
            send({ type: "wijzig", soort: "taak", id: panel.id, velden: w }, { onError: onError, after: function () { schijfNa(szTaak, function () { koppelNa(kzTaak); }); closePanel(); } });
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
            var kzFase = koppelKeuze(), szFase = schijfKeuze();
            send({ type: "wijzig", soort: "fase", id: panel.id, velden: fv }, { onError: onError, after: function () { schijfNa(szFase, function () { koppelNa(kzFase); }); closePanel(); } });
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

    // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════
    // Gsm (design-handoff "CRM Mobiel" 64p–64r). Op een gsm geen Gantt maar drie weergaven uit dezelfde staat: Week (standaard), Lijst
    // en Tijdlijn; planningskeuze als één regel (blad), taakblad bij een tik, Meer-acties in de snelactiebalk. Het desktopdeel
    // (#gl-v2-pl-tabs/-kop/-card) blijft in de DOM maar is op gsm verborgen (CSS); alle wijzigingen lopen via dezelfde send().
    // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════
    var M_DAG = ["zo", "ma", "di", "wo", "do", "vr", "za"];
    var M_DAGL = ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];
    var M_MND = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
    var M_MNDL = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var mTlInit = false, mShift = 0, mTaskKey = null;

    function mDate(iso) { var p = iso.split("-"); return new Date(Date.UTC(+p[0], +p[1] - 1, +p[2])); }
    function mDag(iso) { return M_DAG[mDate(iso).getUTCDay()]; }
    function mDM(iso) { if (!iso) return "—"; var p = iso.split("-"); return p[2] + "/" + p[1]; }
    function mDMY(iso) { if (!iso) return "—"; var p = iso.split("-"); return p[2] + "/" + p[1] + "/" + p[0].slice(2); }
    function mNr(n, een, meer) { return n + " " + (n === 1 ? een : meer); }
    function mFaseVan(r) {
        if (!r.ouderFaseId) return null;
        for (var i = 0; i < state.rijen.length; i++) if (state.rijen[i].soort === "fase" && state.rijen[i].id === r.ouderFaseId) return state.rijen[i];
        return null;
    }
    function mVoorgangers(r) {
        var out = [];
        (r.voorgangerLinks || []).forEach(function (l) { var v = rowByKey("t" + l.taakId); if (v) out.push(v); });
        return out;
    }
    function mOpvolgers(r) {
        return state.rijen.filter(function (x) { return (x.voorgangerLinks || []).some(function (l) { return l.taakId === r.id && r.soort === "taak"; }); });
    }
    // Taken die nog open staan, in volgorde van start
    function mOpenTaken() {
        return state.rijen.filter(function (r) { return r.soort === "taak" && !r.afgerond && r.start && r.einde; })
            .sort(function (a, b) { return a.start < b.start ? -1 : a.start > b.start ? 1 : (a.wbs < b.wbs ? -1 : 1); });
    }
    function mTeLaat(r, today) { return !r.afgerond && r.einde && r.einde < today; }
    function mLaatDagen(r, today) { return werkdagenTot(isoN(r.einde) + 1, isoN(today)); }
    // Wie houdt deze taak tegen: de voorganger die nog niet klaar is en al loopt of te laat is (de laatst eindigende)
    function mBlokkeert(r, today) {
        var best = null;
        mVoorgangers(r).forEach(function (v) { if (!v.afgerond && v.start && v.start <= today && (!best || v.einde > best.einde)) best = v; });
        return best;
    }

    function mMetaHtml(r, today, soort) {
        var dur = r.isMijlpaal ? "mijlpaal" : r.duur + " d", delen = [];
        if (soort === "late") {
            var n = mLaatDagen(r, today);
            delen.push("<em class=\"is-red\">" + (n > 0 ? mNr(n, "werkdag", "werkdagen") + " te laat" : "te laat") + "</em>");
            delen.push(dur + " · " + mDM(r.start) + " → " + mDM(r.einde));
        } else if (soort === "today") {
            if (r.einde === today) delen.push("laatste dag · " + dur);
            else if (r.start === today) delen.push("start vandaag · " + dur);
            else delen.push(dur + " · tot " + mDM(r.einde));
        } else {
            var bl = mBlokkeert(r, today);
            if (bl) delen.push(dur + " · start na " + esc(bl.naam));
            else delen.push(!r.isMijlpaal && r.duur > 5 ? dur + " · tot " + mDM(r.einde) : dur);
        }
        return delen.join(" · ");
    }

    function mTaakRij(r, today, soort, metCheck) {
        var fase = mFaseVan(r), late = soort === "late";
        var datum = late ? "<small>" + mDag(r.einde) + "</small><b>" + mDM(r.einde) + "</b>" : "<small>" + mDag(r.start) + "</small><b>" + r.start.slice(8, 10) + "</b>";
        var meta = mMetaHtml(r, today, soort);
        return "<div class=\"gl-v2-pl-m-t is-" + soort + "\">" +
            "<button type=\"button\" class=\"gl-v2-pl-m-tmain js-gl-v2-menu-trigger\" data-pl-open=\"" + esc(r.sleutel) + "\" aria-haspopup=\"dialog\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-m-task\">" +
            "<span class=\"gl-v2-pl-m-d\">" + datum + "</span>" +
            "<span class=\"gl-v2-m-rt\"><b>" + esc(r.naam) + "</b>" + (fase ? "<small>" + esc(fase.naam) + "</small>" : "") + "<span class=\"gl-v2-pl-m-meta\">" + meta + "</span></span></button>" +
            (metCheck && canWrite ? "<button type=\"button\" class=\"gl-v2-pl-m-ck\" data-pl-tick=\"" + esc(r.sleutel) + "\" aria-label=\"Klaar markeren: " + esc(r.naam) + "\"><i></i></button>" : "") +
            "</div>";
    }
    function mGroep(cls, label, aantal, inhoud, attr) {
        return "<section class=\"gl-v2-m-card gl-v2-pl-m-list\"" + (attr || "") + "><div class=\"gl-v2-pl-m-g " + cls + "\"><span>" + label + "</span>" + (aantal != null ? "<i>" + aantal + "</i>" : "") + "</div>" + inhoud + "</section>";
    }

    // ── Week: kopkaart, bereikte schijven, dan te laat · vandaag · volgende week · daarna ──
    function mSomHtml() {
        var k = state.kop || {}, today = todayIso(), totaal = k.duurWerkdagen || 0, klaar = 0;
        if (k.start && totaal) {
            var s = isoN(k.start), t = isoN(today), e = k.einde ? isoN(k.einde) : t;
            klaar = t < s ? 0 : Math.min(totaal, werkdagenTot(s, Math.min(t, e)));
        }
        var pct = totaal > 0 ? Math.min(100, Math.round(klaar / totaal * 1000) / 10) : 0;
        var verlet = k.verletMeetellen === false ? "verlet uitgezet" : mNr(k.verletDagen || 0, "verletdag", "verletdagen");
        var regels = "";
        if (k.deadline) regels += "<span class=\"" + (k.marge != null && k.marge < 0 ? "is-red" : "") + "\">Deadline " + fmtFull(k.deadline) + (k.marge != null ? " · " + (k.marge < 0 ? dagen(-k.marge) + " te laat" : dagen(k.marge) + " marge") : "") + "</span>";
        if (k.basisOp) regels += "<span>Basisplanning " + esc(k.basisOp) + (k.basisAfwijking ? " · " + (k.basisAfwijking > 0 ? "+" : "−") + Math.abs(k.basisAfwijking) + " d" : "") + "</span>";
        var ontbreekt = "";
        if (canWriteBasis && cfg.projectEditUrl) {
            var vlag = "<i class=\"ph ph-flag-banner\" aria-hidden=\"true\"></i>";
            if (k.geenStartdatum) ontbreekt += "<a href=\"" + esc(cfg.projectEditUrl) + "\">" + vlag + "Aanvangsdatum instellen</a>";
            else if (!k.deadline) ontbreekt += "<a href=\"" + esc(cfg.projectEditUrl) + "\">" + vlag + "Deadline oplevering instellen</a>";
            if (k.geenWeerstation) ontbreekt += "<a href=\"" + esc(cfg.projectEditUrl) + "\"><i class=\"ph ph-cloud-rain\" aria-hidden=\"true\"></i>Weerstation kiezen</a>";
        }
        return "<section class=\"gl-v2-m-card gl-v2-pl-m-sum\"><div class=\"gl-v2-pl-m-sh\"><b>Bouwplanning</b><span>" + fmtFull(k.start) + " → " + fmtFull(k.einde) + "</span></div>" +
            "<div><span class=\"gl-v2-pl-m-bar\" role=\"img\" aria-label=\"" + klaar + " van " + totaal + " werkdagen voorbij\"><i style=\"width:" + pct + "%\"></i><b style=\"left:" + pct + "%\"></b></span>" +
            "<div class=\"gl-v2-pl-m-pl\"><span><b>" + klaar + "</b> van " + totaal + " werkdagen</span><span>nog <b>" + Math.max(0, totaal - klaar) + "</b> · " + verlet + "</span></div></div>" +
            (regels ? "<div class=\"gl-v2-pl-m-regels\">" + regels + "</div>" : "") +
            (ontbreekt ? "<div class=\"gl-v2-pl-m-miss\">" + ontbreekt + "</div>" : "") + "</section>";
    }
    function mSchijvenHtml() {
        if (!canWrite) return "";
        var h = "";
        state.rijen.forEach(function (r) {
            (r.schijven || []).forEach(function (s) {
                if (!s.klaarOmTeMarkeren) return;
                h += "<section class=\"gl-v2-m-card gl-v2-pl-m-schijf\"><span class=\"gl-v2-pl-m-si\"><i class=\"ph ph-seal-check\" aria-hidden=\"true\"></i></span>" +
                    "<span class=\"gl-v2-m-rt\"><span class=\"gl-v2-pl-m-sk\">SCHIJF BEREIKT</span><b>" + esc(s.naam) + (s.percentage ? " " + s.percentage + " %" : "") + "</b>" +
                    "<span class=\"gl-v2-pl-m-st\">" + esc(r.naam) + " is klaar · " + mNr(s.teMarkeren, "eenheid", "eenheden") + " te markeren" + (s.overgeslagen ? ", " + s.overgeslagen + " overgeslagen (niet verkocht of akte nog niet verleden)" : "") + "</span></span>" +
                    "<button type=\"button\" class=\"gl-v2-pl-m-mk\" data-pl-markeer=\"" + s.stageId + "\">Markeren</button></section>";
            });
        });
        return h;
    }
    function mWeekHtml() {
        var today = todayIso(), tN = isoN(today), dow = (new Date(tN * DAG).getUTCDay() + 6) % 7;
        var weekEinde = tN + (6 - dow), volgStart = weekEinde + 1, volgEinde = volgStart + 4, volgZo = weekEinde + 7;
        var taken = mOpenTaken(), laat = [], vandaag = [], dezeWeek = [], volgende = [], daarna = [];
        taken.forEach(function (r) {
            var s = isoN(r.start);
            if (mTeLaat(r, today)) laat.push(r);
            else if (r.start <= today) vandaag.push(r);
            else if (s <= weekEinde) dezeWeek.push(r);
            else if (s <= volgZo) volgende.push(r);
            else daarna.push(r);
        });
        var h = mSomHtml() + mSchijvenHtml();
        var rij = function (soort, check) { return function (r) { return mTaakRij(r, today, soort, check); }; };
        if (laat.length) h += mGroep("is-red", "TE LAAT", laat.length, laat.map(rij("late", true)).join(""));
        if (vandaag.length) h += mGroep("is-gold", "VANDAAG · " + M_DAGL[new Date(tN * DAG).getUTCDay()] + " " + mDate(today).getUTCDate() + " " + M_MNDL[mDate(today).getUTCMonth()], null, vandaag.map(rij("today", true)).join(""), " data-pl-today");
        if (dezeWeek.length) h += mGroep("", "REST VAN DEZE WEEK", dezeWeek.length, dezeWeek.map(rij("next", true)).join(""));
        if (volgende.length) {
            var a = mDate(nIso(volgStart)), b = mDate(nIso(volgEinde));
            var lbl = a.getUTCMonth() === b.getUTCMonth() ? a.getUTCDate() + " – " + b.getUTCDate() + " " + M_MND[a.getUTCMonth()] : a.getUTCDate() + " " + M_MND[a.getUTCMonth()] + " – " + b.getUTCDate() + " " + M_MND[b.getUTCMonth()];
            h += mGroep("", "VOLGENDE WEEK · " + lbl, volgende.length, volgende.map(rij("next", true)).join(""));
        }
        if (daarna.length) {
            var toon = daarna.slice(0, 5);
            h += mGroep("", "DAARNA", null, toon.map(rij("fut", false)).join("") +
                "<button type=\"button\" class=\"gl-v2-m-all\" data-pl-view=\"lijst\">Volledige lijst<i class=\"ph ph-caret-right\" aria-hidden=\"true\"></i></button>");
        } else if (!laat.length && !vandaag.length && !dezeWeek.length && !volgende.length) {
            h += "<section class=\"gl-v2-m-card gl-v2-pl-m-leeg\"><i class=\"ph ph-check-circle\" aria-hidden=\"true\"></i><b>Alle taken zijn klaar</b><span>Er staat niets meer open in deze planning.</span></section>";
        }
        return h;
    }

    // ── Lijst: fases openklappen ──
    function mDefaultOpen() {
        var today = todayIso(), open = {}, eerste = null;
        state.rijen.forEach(function (r) {
            if (r.soort === "fase" && eerste === null) eerste = r.sleutel;
            if (r.soort !== "taak" || r.afgerond || !r.einde) return;
            var f = mFaseVan(r);
            if (f && (mTeLaat(r, today) || (r.start <= today && r.einde >= today))) open[f.sleutel] = true;
        });
        if (!Object.keys(open).length && eerste) open[eerste] = true;
        return open;
    }
    function mIsOpen(sleutel, def) { return Object.prototype.hasOwnProperty.call(ui.mopen, sleutel) ? !!ui.mopen[sleutel] : !!def[sleutel]; }
    function mListHtml() {
        var today = todayIso(), def = mDefaultOpen(), fases = state.rijen.filter(function (r) { return r.soort === "fase"; });
        var kinderen = {}, aantalTaken = 0;
        state.rijen.forEach(function (r) { if (r.soort === "taak") { aantalTaken++; (kinderen[r.ouderFaseId] = kinderen[r.ouderFaseId] || []).push(r); } });
        var anyOpen = fases.some(function (f) { return mIsOpen(f.sleutel, def); });
        var h = "<div class=\"gl-v2-m-countline\"><span>" + mNr(fases.length, "fase", "fases") + " · " + mNr(aantalTaken, "taak", "taken") + "</span><button type=\"button\" class=\"gl-v2-m-link\" data-pl-allopen=\"" + (anyOpen ? "0" : "1") + "\">" + (anyOpen ? "Alles inklappen" : "Alles uitklappen") + "</button></div>";
        h += "<section class=\"gl-v2-m-card gl-v2-pl-m-fl\">" + fases.map(function (f) {
            var open = mIsOpen(f.sleutel, def), pct = Math.round(f.voortgang || 0), sub = "";
            if (open) {
                var lijst = kinderen[f.id] || [];
                sub = "<div class=\"gl-v2-pl-m-sub\">" + (lijst.length ? lijst.map(function (r) {
                    var soort = r.afgerond ? "ok" : mTeLaat(r, today) ? "late" : (r.start <= today && r.einde >= today ? "tod" : "");
                    var chip = !r.afgerond && soort === "late" ? "<i class=\"gl-v2-m-chip is-red\">te laat</i>" : (!r.afgerond && soort === "tod" ? "<i class=\"gl-v2-m-chip is-gold\">vandaag</i>" : "");
                    return "<button type=\"button\" class=\"gl-v2-pl-m-sr is-" + (soort || "open") + " js-gl-v2-menu-trigger\" data-pl-open=\"" + esc(r.sleutel) + "\" aria-haspopup=\"dialog\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-m-task\">" +
                        "<span class=\"gl-v2-pl-m-sc\">" + (r.afgerond ? "<i class=\"ph ph-check\" aria-hidden=\"true\"></i>" : "") + "</span><span class=\"gl-v2-pl-m-sn\">" + esc(r.wbs) + "</span>" +
                        "<span class=\"gl-v2-m-rt\"><b>" + esc(r.naam) + "</b><small>" + (r.isMijlpaal ? mDM(r.start) + " · mijlpaal" : mDM(r.start) + " → " + mDM(r.einde)) + "</small></span>" + chip + "</button>";
                }).join("") : "<p class=\"gl-v2-pl-m-geen\">Nog geen taken in deze fase.</p>") +
                    (canWrite ? "<button type=\"button\" class=\"gl-v2-m-link gl-v2-pl-m-addin\" data-pl-addin=\"" + f.id + "\"><i class=\"ph ph-plus\" aria-hidden=\"true\"></i>Taak in deze fase</button>" : "") + "</div>";
            }
            return "<button type=\"button\" class=\"gl-v2-pl-m-ph" + (open ? " is-open" : "") + "\" data-pl-fase=\"" + esc(f.sleutel) + "\" aria-expanded=\"" + (open ? "true" : "false") + "\">" +
                "<span class=\"gl-v2-pl-m-pn\">" + esc(f.wbs) + "</span><span class=\"gl-v2-m-rt\"><b>" + esc(f.naam) + "</b><small>" + mDMY(f.start) + " → " + mDMY(f.einde) + "</small></span>" +
                "<span class=\"gl-v2-pl-m-pp\"><b>" + pct + " %</b><span class=\"gl-v2-pl-m-mini\"><i style=\"width:" + pct + "%\"></i></span></span><i class=\"ph ph-caret-" + (open ? "up" : "down") + "\" aria-hidden=\"true\"></i></button>" + sub;
        }).join("") + "</section>";
        return h;
    }

    // ── Tijdlijn: één balk per fase, maanden van 34 px, veegbaar; de naam blijft links staan ──
    function mTimelineHtml() {
        var MW = 34, today = todayIso(), fases = state.rijen.filter(function (r) { return r.soort === "fase" && r.start && r.einde; });
        if (!fases.length) return "<section class=\"gl-v2-m-card gl-v2-pl-m-leeg\"><b>Geen fases met een datum</b></section>";
        var min = fases.reduce(function (m, f) { return f.start < m ? f.start : m; }, fases[0].start);
        var max = fases.reduce(function (m, f) { return f.einde > m ? f.einde : m; }, fases[0].einde);
        var a = mDate(min), b = mDate(max), y0 = a.getUTCFullYear(), m0 = a.getUTCMonth();
        var aantal = (b.getUTCFullYear() - y0) * 12 + b.getUTCMonth() - m0 + 1;
        function pos(iso) { var d = mDate(iso), mi = (d.getUTCFullYear() - y0) * 12 + d.getUTCMonth() - m0, dim = new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 0)).getUTCDate(); return (mi + (d.getUTCDate() - 1) / dim) * MW; }
        var breedte = aantal * MW + 16, kop = "";
        for (var i = 0; i < aantal; i++) { var mm = (m0 + i) % 12, jj = y0 + Math.floor((m0 + i) / 12); kop += "<span style=\"width:" + MW + "px\"" + (mm === 0 ? " class=\"is-y\"" : "") + " title=\"" + M_MNDL[mm] + " " + jj + "\">" + M_MND[mm] + "</span>"; }
        var rijen = fases.map(function (f) {
            var l = pos(f.start), w = Math.max(6, pos(nIso(isoN(f.einde) + 1)) - l), pct = Math.round(f.voortgang || 0);
            return "<button type=\"button\" class=\"gl-v2-pl-m-tr\" data-pl-tl=\"" + esc(f.sleutel) + "\" aria-label=\"" + esc(f.naam) + ", " + mDMY(f.start) + " tot " + mDMY(f.einde) + ", " + pct + " procent klaar\">" +
                "<span class=\"gl-v2-pl-m-tn\">" + esc(f.naam.replace(/^\s*\d+(\.\d+)*\s*[-–.]?\s*/, "")) + "</span><span class=\"gl-v2-pl-m-tb\" style=\"left:" + l.toFixed(1) + "px;width:" + w.toFixed(1) + "px\"><i style=\"width:" + pct + "%\"></i></span></button>";
        }).join("");
        var tx = today >= min && today <= max ? pos(today) : null;
        return "<section class=\"gl-v2-m-card gl-v2-pl-m-tl\"><div class=\"gl-v2-pl-m-tlsc\"><div class=\"gl-v2-pl-m-tlin\" style=\"width:" + breedte + "px\"><div class=\"gl-v2-pl-m-tlh\">" + kop + "</div>" +
            "<div class=\"gl-v2-pl-m-tlrows\">" + rijen + (tx != null ? "<span class=\"gl-v2-pl-m-today\" style=\"left:" + tx.toFixed(1) + "px\"><b>vandaag</b></span>" : "") + "</div></div></div>" +
            "<div class=\"gl-v2-pl-m-tlg\"><span><i class=\"a\"></i>klaar</span><span><i class=\"b\"></i>gepland</span><span><i class=\"t\"></i>vandaag</span><span class=\"is-hint\"><i class=\"ph ph-hand-swipe-left\" aria-hidden=\"true\"></i>veeg voor de rest</span></div></section>" +
            "<div class=\"gl-v2-m-info\"><i class=\"ph ph-info\" aria-hidden=\"true\"></i><span>Fases slepen, verbinden en de basisplanning vastleggen doe je op tablet of computer. Op gsm: kijken, klaar markeren en verschuiven.</span></div>";
    }

    // ── Planningskeuze (blad), Sjablonen (blad) ──
    function mPickBlad() {
        var el = $("gl-v2-pl-m-pick"); if (!el) return;
        var lijst = state.planningen || [], h = "<div class=\"gl-v2-menu-sheet-title\">Planning kiezen</div>";
        function item(attr, icoon, label, sub, aan) {
            return "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" " + attr + "><i class=\"ph " + icoon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + esc(label) + "</span>" +
                (sub ? "<span class=\"gl-v2-menu-item-sub\">" + esc(sub) + "</span>" : "") + "</span>" + (aan ? "<i class=\"ph ph-check gl-v2-pl-m-aan\" aria-hidden=\"true\"></i>" : "") + "</button>";
        }
        lijst.forEach(function (p) {
            h += item("data-pl-pick=\"" + p.id + "\"", p.isHoofd ? "ph-house-line" : "ph-buildings", p.naam, mNr(p.aantalTaken || 0, "taak", "taken") + (p.eenhedenTekst ? " · voor " + p.eenhedenTekst : (p.isHoofd ? " · hele project" : "")), p.id === state.actiefPlanningId && !aannActief);
        });
        if (lijst.length > 1) h += item("data-pl-pick=\"" + GLOBAAL + "\"", "ph-stack", "Globaal overzicht", "alle plannings samen · enkel lezen", state.actiefPlanningId === GLOBAAL && !aannActief);
        if (state.heeftPlanning) h += item("data-pl-pick=\"" + AANN + "\"", "ph-hard-hat", "Per aannemer", "planning gefilterd per leverancier", aannActief);
        if (canWriteBasis) h += "<div class=\"gl-v2-menu-divider\"></div>" + item("data-pl-pick=\"nieuw\"", "ph-plus", "Planning toevoegen", "voor een woning of gebouw", false);
        el.innerHTML = h;
    }
    function mSjBlad() {
        var el = $("gl-v2-pl-m-sj"); if (!el) return;
        var act = actief(), h = "<div class=\"gl-v2-menu-sheet-title\">Sjablonen</div>";
        h += "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-pl-sj=\"toepas\"><i class=\"ph ph-copy\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">Sjabloon toepassen</span><span class=\"gl-v2-menu-item-sub\">op woningen of gebouwen</span></span></button>";
        h += "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-pl-sj=\"project\"><i class=\"ph ph-buildings\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">Projectsjabloon toepassen</span><span class=\"gl-v2-menu-item-sub\">planning, traject en schijven in één keer</span></span></button>";
        if (canSjabloon && act && act.aantalTaken && state.actiefPlanningId !== GLOBAAL) h += "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-pl-sj=\"bewaar\"><i class=\"ph ph-floppy-disk\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">Deze planning bewaren</span><span class=\"gl-v2-menu-item-sub\">als sjabloon voor een volgend project</span></span></button>";
        el.innerHTML = h;
    }

    function renderMobile() {
        var host = $("gl-v2-pl-m"); if (!host) return;
        var lijst = state.planningen || [];
        if (!lijst.length && !state.heeftPlanning) { host.hidden = true; host.innerHTML = ""; return; }
        host.hidden = false;
        var heeft = !!state.heeftPlanning && state.rijen.length > 0, pl = actief();
        var oudTl = host.querySelector(".gl-v2-pl-m-tlsc"), oudLinks = oudTl ? oudTl.scrollLeft : null;
        var naam = aannActief ? "Per aannemer" : (pl ? (pl.global ? "Globaal overzicht" : pl.naam) : "Planning");
        var aantal = aannActief ? ((aannData && aannData.rijen) || []).filter(function (r) { return r.soort === "taak"; }).length : (pl ? (pl.aantalTaken || 0) : 0);
        var sub = mNr(aantal, "taak", "taken") + " · " + (aannActief || !canWrite ? "alleen lezen" : "bewaart meteen");
        var h = "<div class=\"gl-v2-m-tool gl-v2-pl-m-tool\"><button type=\"button\" class=\"gl-v2-pl-m-pick js-gl-v2-menu-trigger\" aria-haspopup=\"true\" aria-expanded=\"false\" aria-controls=\"gl-v2-pl-m-pick\">" +
            "<i class=\"ph ph-stack\" aria-hidden=\"true\"></i><span><b>" + esc(naam) + "</b><small>" + sub + "</small></span><i class=\"ph ph-caret-down\" aria-hidden=\"true\"></i></button>";
        if (heeft && !aannActief) {
            h += "<div class=\"gl-v2-pl-m-seg\" role=\"group\" aria-label=\"Weergave\">" + [["week", "Week"], ["lijst", "Lijst"], ["tijdlijn", "Tijdlijn"]].map(function (v) {
                return "<button type=\"button\" data-pl-view=\"" + v[0] + "\" class=\"" + (ui.mview === v[0] ? "is-on" : "") + "\" aria-pressed=\"" + (ui.mview === v[0] ? "true" : "false") + "\">" + v[1] + "</button>";
            }).join("") + "</div>";
        }
        h += "</div>";
        if (heeft && !aannActief) h += "<div class=\"gl-v2-m-stack\">" + (ui.mview === "lijst" ? mListHtml() : ui.mview === "tijdlijn" ? mTimelineHtml() : mWeekHtml()) + "</div>";
        host.innerHTML = h;
        mPickBlad(); mSjBlad();
        var tl = host.querySelector(".gl-v2-pl-m-tlsc");
        if (tl) {
            var tdag = host.querySelector(".gl-v2-pl-m-today");
            if (oudLinks != null && mTlInit) tl.scrollLeft = oudLinks;
            else { tl.scrollLeft = tdag ? Math.max(0, tdag.offsetLeft - 90) : 0; mTlInit = true; }
        } else mTlInit = false;
    }

    // ── Taakblad (64r) ──
    function mTaakBlad(key) {
        var el = $("gl-v2-pl-m-task"), r = rowByKey(key);
        if (!el || !r) return false;
        mTaskKey = key;
        var today = todayIso(), fase = mFaseVan(r), late = mTeLaat(r, today), laatsteDag = !r.afgerond && r.einde === today;
        var chip = r.afgerond ? "<i class=\"gl-v2-m-chip is-ok\">Klaar</i>" : late ? "<i class=\"gl-v2-m-chip is-red\">Te laat</i>" : laatsteDag ? "<i class=\"gl-v2-m-chip is-gold\">Laatste dag</i>" : (r.isMijlpaal ? "<i class=\"gl-v2-m-chip\">Mijlpaal</i>" : (r.start <= today && r.einde >= today ? "<i class=\"gl-v2-m-chip is-ok\">Loopt</i>" : ""));
        var vo = mVoorgangers(r), op = mOpvolgers(r), dep = [];
        if (vo.length) dep.push("Na <b>" + esc(vo[0].naam) + "</b>" + (vo.length > 1 ? " en " + (vo.length - 1) + " andere" : ""));
        if (op.length) dep.push("daarna <b>" + esc(op[0].naam) + "</b>" + (op.length > 1 ? " en " + (op.length - 1) + " andere" : ""));
        var h = "<div class=\"gl-v2-pl-m-tsh\"><span class=\"gl-v2-m-rt\"><small>" + esc((fase ? fase.naam + " · " : "") + r.wbs) + "</small><b>" + esc(r.naam) + "</b></span>" + chip + "</div>" +
            "<dl class=\"gl-v2-pl-m-tdl\"><div><dt>Start</dt><dd>" + mDag(r.start) + " " + mDM(r.start) + "</dd></div><div><dt>Einde</dt><dd>" + mDag(r.einde) + " " + mDM(r.einde) + "</dd></div>" +
            "<div><dt>Duur</dt><dd>" + (r.isMijlpaal ? "mijlpaal" : dagen(r.duur)) + "</dd></div><div><dt>Aannemer</dt><dd>" + (r.uitvoerderNaam ? esc(r.uitvoerderNaam) : "—") + "</dd></div></dl>";
        if (dep.length) h += "<div class=\"gl-v2-pl-m-dep\"><i class=\"ph ph-arrows-left-right\" aria-hidden=\"true\"></i><span>" + dep.join(" · ") + "</span></div>";
        if (canWrite) {
            h += "<button type=\"button\" class=\"gl-v2-m-lv-pri\" data-pl-sa=\"klaar\"><i class=\"ph " + (r.afgerond ? "ph-arrow-counter-clockwise" : "ph-check-circle") + "\" aria-hidden=\"true\"></i>" + (r.afgerond ? "Niet meer voltooid" : "Klaar markeren") + "</button>";
            if (!r.afgerond) h += "<div class=\"gl-v2-pl-m-shift\"><span class=\"gl-v2-m-rt\"><b>Verschuiven</b><small>volgende taken schuiven mee</small></span>" +
                "<button type=\"button\" class=\"gl-v2-m-roundbtn\" data-pl-sa=\"min\" aria-label=\"1 werkdag vroeger\"><i class=\"ph ph-minus\" aria-hidden=\"true\"></i></button><b class=\"gl-v2-pl-m-sv\">" + (mShift > 0 ? "+" : mShift < 0 ? "−" : "") + Math.abs(mShift) + " d</b>" +
                "<button type=\"button\" class=\"gl-v2-m-roundbtn\" data-pl-sa=\"plus\" aria-label=\"1 werkdag later\"><i class=\"ph ph-plus\" aria-hidden=\"true\"></i></button></div>";
            var rij = function (a, icoon, titel, sub) { return "<button type=\"button\" class=\"gl-v2-menu-item\" role=\"menuitem\" data-pl-sa=\"" + a + "\"><i class=\"ph " + icoon + "\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">" + titel + "</span>" + (sub ? "<span class=\"gl-v2-menu-item-sub\">" + sub + "</span>" : "") + "</span></button>"; };
            h += rij("wstart", "ph-calendar-check", "Werkelijke start invullen", r.werkelijkStart ? "gestart op " + fmtFull(r.werkelijkStart) : "gestart op een andere dag dan gepland");
            if (cfg.puntenUrl) h += "<a href=\"" + esc(cfg.puntenUrl) + "\" class=\"gl-v2-menu-item\" role=\"menuitem\"><i class=\"ph ph-list-checks\" aria-hidden=\"true\"></i><span class=\"gl-v2-menu-item-text\"><span class=\"gl-v2-menu-item-label\">Werfpunt aanmaken</span><span class=\"gl-v2-menu-item-sub\">opent de punten van dit project</span></span></a>";
            h += rij("edit", "ph-pencil-simple", "Taak bewerken", "naam, duur, aannemer, beperkingen");
        }
        el.setAttribute("role", "dialog"); el.setAttribute("aria-label", r.naam);
        el.innerHTML = h;
        return true;
    }

    // ── Bladen openen/sluiten ──
    function mSluit() { if (window.GlV2Menu) window.GlV2Menu.closeAll(); }
    function mOpenBlad(el) {
        if (!el || !window.GlV2Menu) return;
        window.GlV2Menu.closeAll();
        if (!el.querySelector(".gl-v2-menu-cancel")) {
            var c = document.createElement("button"); c.type = "button"; c.className = "gl-v2-menu-cancel"; c.textContent = "Annuleren";
            c.addEventListener("click", mSluit); el.appendChild(c);
        }
        el.classList.add("is-open");
        var bd = document.getElementById("gl-v2-menu-backdrop"); if (bd) bd.classList.add("is-open");
        el.style.top = ""; el.style.left = "";
    }
    function mTikKlaar(r, na) {
        send({ type: "wijzig", soort: "taak", id: r.id, velden: { voortgang: r.afgerond ? 0 : 100 } }, { after: function () { toast("success", r.afgerond ? "Weer open gezet" : "Klaar gemarkeerd", r.naam); if (na) na(); } });
    }

    var mHost = $("gl-v2-pl-m");
    if (mHost) mHost.addEventListener("click", function (e) {
        var t;
        if ((t = e.target.closest("[data-pl-view]"))) { ui.mview = t.getAttribute("data-pl-view"); saveUi(); mTlInit = false; renderMobile(); window.scrollTo({ top: 0 }); return; }
        if ((t = e.target.closest("[data-pl-tick]"))) { var rt = rowByKey(t.getAttribute("data-pl-tick")); if (rt && canWrite) mTikKlaar(rt); return; }
        if ((t = e.target.closest("[data-pl-markeer]"))) { schijfMarkeer(parseInt(t.getAttribute("data-pl-markeer"), 10), false); return; }
        if ((t = e.target.closest("[data-pl-open]"))) { mShift = 0; mTaakBlad(t.getAttribute("data-pl-open")); return; }   // de schaduwknop-klasse opent het blad (shell)
        if ((t = e.target.closest("[data-pl-fase]"))) {
            var def = mDefaultOpen(), k = t.getAttribute("data-pl-fase");
            ui.mopen[k] = !mIsOpen(k, def); saveUi(); renderMobile(); return;
        }
        if ((t = e.target.closest("[data-pl-allopen]"))) {
            var aan = t.getAttribute("data-pl-allopen") === "1";
            state.rijen.forEach(function (r) { if (r.soort === "fase") ui.mopen[r.sleutel] = aan; });
            saveUi(); renderMobile(); return;
        }
        if ((t = e.target.closest("[data-pl-addin]"))) { if (canWrite) openNieuwTaak({ sectieId: parseInt(t.getAttribute("data-pl-addin"), 10) }); return; }
        if ((t = e.target.closest("[data-pl-tl]"))) {
            // een fase op de tijdlijn aantikken springt naar die fase in de lijst
            var fk = t.getAttribute("data-pl-tl");
            ui.mview = "lijst"; ui.mopen[fk] = true; saveUi(); renderMobile();
            var el = mHost.querySelector("[data-pl-fase=\"" + fk + "\"]");
            if (el) el.scrollIntoView({ block: "center" });
        }
    });

    var mPick = $("gl-v2-pl-m-pick");
    if (mPick) mPick.addEventListener("click", function (e) {
        var b = e.target.closest("[data-pl-pick]"); if (!b) return;
        var v = b.getAttribute("data-pl-pick");
        mSluit();
        if (v === "nieuw") openPlanning(false);
        else switchPlanning(parseInt(v, 10));
    });
    var mSj = $("gl-v2-pl-m-sj");
    if (mSj) mSj.addEventListener("click", function (e) {
        var b = e.target.closest("[data-pl-sj]"); if (!b) return;
        var v = b.getAttribute("data-pl-sj"); mSluit();
        if (v === "toepas") openSjToepas(); else if (v === "project") openPj(); else if (v === "bewaar") openSjBewaar();
    });
    var mTask = $("gl-v2-pl-m-task");
    if (mTask) mTask.addEventListener("click", function (e) {
        var b = e.target.closest("[data-pl-sa]"); if (!b) return;
        var r = rowByKey(mTaskKey); if (!r) { mSluit(); return; }
        var a = b.getAttribute("data-pl-sa");
        if (a === "klaar") { mSluit(); mTikKlaar(r); }
        else if (a === "edit") { mSluit(); openEdit(r.sleutel); }
        else if (a === "wstart") {
            mSluit(); openEdit(r.sleutel);
            window.setTimeout(function () { var f = fieldOf("gl-v2-pl-f-wstart"); if (f) f.scrollIntoView({ block: "center" }); }, 350);
        } else if (a === "min" || a === "plus") {
            var dir = a === "plus" ? 1 : -1, nieuw = nIso(verschuifWerkdagen(isoN(r.start), dir));
            send({ type: "wijzig", soort: "taak", id: r.id, velden: { beperking: r.beperking === 1 ? 1 : 2, beperkingDatum: nieuw } }, {
                after: function () {
                    var nu = rowByKey(r.sleutel);
                    if (nu && nu.start !== nieuw) toast("info", "De taak start op " + fmtFull(nu.start), "Een voorganger of een andere beperking bepaalt de start.");
                    else mShift += dir;
                    if (nu) { mTaakBlad(r.sleutel); } else mSluit();
                }
            });
        }
    });

    // Snelactiebalk (65j): Vandaag · Verletdag · Taak · Meer
    function mVandaag() {
        var host = $("gl-v2-pl-m");
        if (!host || host.hidden || aannActief || !state.heeftPlanning) { toast("info", "Geen planning om naar vandaag te springen"); return; }
        if (ui.mview === "tijdlijn") {
            var sc = host.querySelector(".gl-v2-pl-m-tlsc"), td = host.querySelector(".gl-v2-pl-m-today");
            if (sc && td) sc.scrollTo({ left: Math.max(0, td.offsetLeft - 90), behavior: "smooth" }); else toast("info", "Vandaag valt buiten de planning");
            return;
        }
        if (ui.mview === "lijst") {
            var today = todayIso();
            state.rijen.forEach(function (r) {
                if (r.soort === "taak" && !r.afgerond && r.start <= today && r.einde >= today) { var f = mFaseVan(r); if (f) ui.mopen[f.sleutel] = true; }
            });
            saveUi(); renderMobile();
            var el = host.querySelector(".gl-v2-pl-m-sr.is-tod");
            if (el) el.scrollIntoView({ behavior: "smooth", block: "center" }); else toast("info", "Vandaag loopt er geen taak");
            return;
        }
        var doel = host.querySelector("[data-pl-today]");
        if (doel) doel.scrollIntoView({ behavior: "smooth", block: "center" }); else toast("info", "Vandaag loopt er geen taak");
    }
    document.addEventListener("click", function (e) {
        var q = e.target.closest(".js-pl-q-vandaag, .js-pl-q-aann, .js-pl-q-basis, .js-pl-q-import, .js-pl-q-sj");
        if (!q) return;
        e.preventDefault();
        if (q.classList.contains("js-pl-q-vandaag")) mVandaag();
        else if (q.classList.contains("js-pl-q-aann")) openAann();
        else if (q.classList.contains("js-pl-q-basis")) { if (basisBtn) basisBtn.click(); }
        else if (q.classList.contains("js-pl-q-import")) openImport();
        else if (q.classList.contains("js-pl-q-sj")) window.setTimeout(function () { mOpenBlad($("gl-v2-pl-m-sj")); }, 0);
    });
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-pl-q-instel")) { e.preventDefault(); if (actief() && state.actiefPlanningId !== GLOBAAL) openPlanning(true); }
    });

    render();
})();
