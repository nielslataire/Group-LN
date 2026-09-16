(function () {
    "use strict";

    var root = document.querySelector(".content-with-menu");
    if (!root) return;

    // DataTable — zelfde zoekveld + kolommenfilter-patroon als Projecten/DetailClients
    // (Clients.cshtml/DetailClients.cshtml): eigen #mp-search-term i.p.v. de ingebouwde .dt-search,
    // colvis-knop verplaatst naar #mp-colvis-container.
    if (window.DataTable && document.getElementById("datatable-mijlpalen")) {
        var mpTable = new DataTable("#datatable-mijlpalen", {
            order: [[4, "asc"]], // kolom 4 = Streefdatum (Status staat nu vooraan als kolom 0)
            pageLength: 25,
            language: {
                search: "Zoeken:",
                lengthMenu: "Toon _MENU_",
                info: "_START_ tot _END_ van _TOTAL_ mijlpalen",
                infoEmpty: "0 mijlpalen",
                infoFiltered: "(gefilterd uit _MAX_)",
                zeroRecords: "Geen mijlpalen gevonden",
                emptyTable: "Nog geen mijlpalen",
                paginate: { first: "Eerste", last: "Laatste", next: "Volgende", previous: "Vorige" }
            },
            layout: {
                topStart: {
                    buttons: [
                        {
                            extend: "colvis",
                            text: '<i class="bx bx-columns me-2"></i><span>Kolommen</span>',
                            titleAttr: "Selecteer kolommen",
                            columns: ":not(.noVis)",
                            init: function (api, node) { $(node).removeClass("btn-secondary").addClass("btn btn-default"); }
                        }
                    ]
                }
            }
        });
        var mpSearchWrap = document.querySelector("#datatable-mijlpalen").closest(".gl-form-shell__panel");
        if (mpSearchWrap) {
            var dtSearch = mpSearchWrap.querySelector(".dt-search");
            if (dtSearch) dtSearch.style.display = "none";
        }
        mpTable.buttons(0, null).containers().appendTo("#mp-colvis-container");
        var mpSearchInput = document.getElementById("mp-search-term");
        if (mpSearchInput) {
            mpSearchInput.addEventListener("keyup", function () { mpTable.search(mpSearchInput.value).draw(); });
        }
    }

    var projectId = (location.pathname.match(/\/Projects\/(\d+)\/Traject/i) || [])[1];

    // Eén keer geparsed hier zodat zowel de status-modal (triggers tonen vóór opslaan) als
    // initKalender2() (verderop) dezelfde bron gebruiken i.p.v. #kalenderData twee keer te lezen.
    var kalenderRaw = (function () {
        var dataEl = document.getElementById("kalenderData");
        if (!dataEl) return { fases: [], mijlpalen: [] };
        try { return JSON.parse(dataEl.textContent || "{}"); } catch (e) { return { fases: [], mijlpalen: [] }; }
    })();
    var mijlpaalById = {};
    (kalenderRaw.mijlpalen || []).forEach(function (m) { mijlpaalById[m.id] = m; });

    var modalMijlpaalEl = document.getElementById("modalMijlpaal");
    var modalStatusEl = document.getElementById("modalMijlpaalStatus");
    var modalTaakEl = document.getElementById("modalTaakQuickAdd");
    var bsMijlpaal = modalMijlpaalEl && window.bootstrap ? new bootstrap.Modal(modalMijlpaalEl) : null;
    var bsStatus = modalStatusEl && window.bootstrap ? new bootstrap.Modal(modalStatusEl) : null;
    var bsTaak = modalTaakEl && window.bootstrap ? new bootstrap.Modal(modalTaakEl) : null;

    function resetUpsert() {
        if (!modalMijlpaalEl) return;
        modalMijlpaalEl.querySelector("#mp-id").value = "";
        modalMijlpaalEl.querySelector("#mp-title").textContent = "Mijlpaal toevoegen";
        modalMijlpaalEl.querySelectorAll("input[type=text],input[type=date],textarea").forEach(function (i) { i.value = ""; });
        modalMijlpaalEl.querySelector("#mp-volgorde").value = "0";
        modalMijlpaalEl.querySelector("#mp-verplicht").checked = true;
        modalMijlpaalEl.querySelectorAll("select").forEach(function (s) { s.selectedIndex = 0; });
    }

    function setVal(sel, val) {
        var el = (modalMijlpaalEl || document).querySelector(sel) || document.querySelector(sel);
        if (el) el.value = (val === null || val === undefined) ? "" : String(val);
    }

    // Gedeeld tussen de .js-mijlpaal-edit-delegatie hieronder EN de FullCalendar-events in
    // initKalender() — een klik op een mijlpaal in de kalender opent dezelfde bewerk-modal.
    function openMijlpaalEdit(id) {
        if (!id || !projectId || !modalMijlpaalEl) return;
        fetch("/Projects/" + projectId + "/Traject/Mijlpaal/" + id + "/Data")
            .then(function (r) { return r.ok ? r.json() : Promise.reject(); })
            .then(function (m) {
                resetUpsert();
                modalMijlpaalEl.querySelector("#mp-title").textContent = "Mijlpaal bewerken";
                modalMijlpaalEl.querySelector("#mp-id").value = m.id;
                modalMijlpaalEl.querySelector("#mp-naam").value = m.naam || "";
                setVal("#mp-fase", m.projecttrajectFaseId);
                setVal("#mp-unit", m.unitId);
                setVal("#mp-type", m.mijlpaalType);
                setVal("#mp-status", m.status);
                setVal("#mp-rol", m.verantwoordelijkeRol);
                modalMijlpaalEl.querySelector("#mp-doeldatum").value = m.doeldatum || "";
                modalMijlpaalEl.querySelector("#mp-werkelijk").value = m.werkelijkeDatum || "";
                modalMijlpaalEl.querySelector("#mp-volgorde").value = m.volgorde || 0;
                modalMijlpaalEl.querySelector("#mp-verplicht").checked = !!m.isVerplicht;
                modalMijlpaalEl.querySelector("#mp-opmerking").value = m.opmerking || "";
                if (bsMijlpaal) bsMijlpaal.show();
            })
            .catch(function () { alert("Kon de mijlpaal niet laden."); });
    }

    function openMijlpaalStatus(id, naam, status) {
        if (!modalStatusEl) return;
        modalStatusEl.querySelector("#mps-id").value = id || "";
        modalStatusEl.querySelector("#mps-naam").textContent = naam || "";
        setVal("#mps-status", status);
        renderMpsTriggers(id);
        if (bsStatus) bsStatus.show();
    }

    // P1-fix uit /impeccable critique: de statusmodal toonde nooit welke automatische acties
    // (dossier aanmaken, projectstatus wijzigen, fase ontgrendelen, …) al op dit mijlpaal
    // geconfigureerd staan — dat was tot nu toe enkel zichtbaar in het (read-only) kalender-
    // detailpaneel, een pad dat de meeste statuswijzigingen nooit passeren. Toont de volledige
    // triggerlijst vóór opslaan i.p.v. pas achteraf te laten blijken wat er gebeurd is.
    function renderMpsTriggers(id) {
        var panel = document.getElementById("mps-triggers");
        var list = document.getElementById("mps-triggers-list");
        if (!panel || !list) return;
        list.innerHTML = "";
        var m = mijlpaalById[id];
        var triggers = (m && m.triggers) || [];
        if (!triggers.length) { panel.hidden = true; return; }
        triggers.forEach(function (t) {
            var li = document.createElement("li");
            var event = document.createElement("span");
            event.className = "gl-mps-trigger-event";
            event.textContent = t.eventLabel;
            var label = document.createElement("span");
            label.className = "gl-mps-trigger-label";
            label.textContent = t.label;
            li.appendChild(event);
            li.appendChild(label);
            list.appendChild(li);
        });
        panel.hidden = false;
    }

    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-mijlpaal-new")) resetUpsert();

        var editBtn = e.target.closest(".js-mijlpaal-edit");
        if (editBtn) {
            openMijlpaalEdit(editBtn.getAttribute("data-id"));
            return;
        }

        var statusBtn = e.target.closest(".js-mijlpaal-status");
        if (statusBtn) {
            openMijlpaalStatus(statusBtn.getAttribute("data-id"), statusBtn.getAttribute("data-naam"), statusBtn.getAttribute("data-status"));
        }
    });

    var btnMpsTaak = document.getElementById("mps-btn-taak");
    if (btnMpsTaak && modalTaakEl) {
        btnMpsTaak.addEventListener("click", function () {
            var mijlpaalId = modalStatusEl.querySelector("#mps-id").value;
            var mijlpaalNaam = modalStatusEl.querySelector("#mps-naam").textContent || "";
            var mijlpaalIdField = modalTaakEl.querySelector("#modalTaakQuickAdd-mijlpaal-id");
            var titelField = modalTaakEl.querySelector("#modalTaakQuickAdd-titel");
            if (mijlpaalIdField) mijlpaalIdField.value = mijlpaalId;
            if (titelField) titelField.value = mijlpaalNaam ? ("Opvolgen: " + mijlpaalNaam) : "";
            if (bsStatus) bsStatus.hide();
            if (bsTaak) bsTaak.show();
        });
    }

    initKalender2();
    initTabrowPin();

    // ══ Tabbar blijft zichtbaar tijdens scrollen (gl-traject-tabrow) ═════════════════════
    // position:sticky rekent in deze .content-with-menu-schil (.inner-body) zichtbaar fout af
    // (rustpositie schuift, "vastklikken" gebeurt niet). Een hardcoded position:fixed met vaste
    // left-waarden botst op zijn beurt met de extra .inner-menu-kolom van deze pagina — die kolom
    // heeft zelf óók een vaste breedte, verschuift naar een ander punt zodra de sidebar-links wordt
    // ingeklapt, en de theme kent daarnaast nog sidebar-left-sm/-xs-varianten met elk hun eigen
    // getallen: te veel combinaties om hier betrouwbaar te hardcoderen (zie traject.css). Daarom
    // hier zelf een minimale "affix": een sentinel + IntersectionObserver bepaalt wanneer de rij
    // zou wegscrollen (pin/unpin), en de linker rand/breedte van de gepinde rij wordt bij elke pin
    // live afgelezen van .inner-body's eigen gerenderde positie i.p.v. verondersteld.
    function initTabrowPin() {
        var row = document.querySelector(".gl-traject-tabrow");
        var innerBody = document.querySelector(".inner-body");
        var card = row && row.nextElementSibling;
        if (!row || !innerBody || !card) return;

        var mq = window.matchMedia("(min-width: 768px)");
        var sentinel = document.createElement("div");
        sentinel.className = "gl-traject-tabrow-sentinel";
        sentinel.setAttribute("aria-hidden", "true");
        row.parentNode.insertBefore(sentinel, row);
        var spacer = document.createElement("div");
        spacer.className = "gl-traject-tabrow-spacer";
        spacer.setAttribute("aria-hidden", "true");
        row.parentNode.insertBefore(spacer, card);

        var pinned = false;

        function syncBounds() {
            var r = innerBody.getBoundingClientRect();
            row.style.left = r.left + "px";
            row.style.width = r.width + "px";
        }

        function pin() {
            if (pinned || !mq.matches) return;
            // Meten terwijl de rij nog gewoon in-flow staat — de vrijgekomen ruimte is exact het
            // verschil tussen waar de kaart en de rij nu staan, ongeacht de precieze marge-som.
            var gap = card.getBoundingClientRect().top - row.getBoundingClientRect().top;
            spacer.style.height = gap + "px";
            syncBounds();
            row.classList.add("gl-is-pinned");
            pinned = true;
        }

        function unpin() {
            if (!pinned) return;
            row.classList.remove("gl-is-pinned");
            row.style.left = "";
            row.style.width = "";
            spacer.style.height = "0";
            pinned = false;
        }

        var io = "IntersectionObserver" in window ? new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) unpin(); else pin();
            });
        }, { rootMargin: "-" + Math.round(parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--topbar-height")) || 72) + "px 0px 0px 0px", threshold: 0 }) : null;
        if (io) io.observe(sentinel);

        function onLayoutChange() {
            if (!mq.matches) { unpin(); return; }
            if (pinned) syncBounds();
        }
        window.addEventListener("resize", onLayoutChange);
        // Sidebar-inklap/uitklap en het openen/sluiten van .inner-menu wijzigen enkel html's
        // class-attribuut, geen resize-event — MutationObserver vangt die live op.
        new MutationObserver(onLayoutChange).observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
    }

    // ══ Kalender 2.0: Maand / Kwartaal / Jaar / Agenda, volledig custom ══════════════════
    // Geen widget-library — zie traject.css bovenaan de "Kalender 2.0"-sectie voor waarom.
    // Eén databron (#kalenderData), vier renderfuncties die dezelfde state (weergave, anker-
    // datum, statusfilter, verborgen fases, geselecteerde mijlpaal) uitlezen. Init gebeurt ook
    // terwijl het tabblad nog verborgen is; de tabwissel-handler in Index.cshtml dispatcht een
    // window-resize die hier niets hoeft te doen (geen library die daarop moet herberekenen —
    // dit is gewone DOM, geen canvas/SVG-layout die een zichtbare container nodig heeft).
    function initKalender2() {
        var root = document.getElementById("trajectKalender2");
        if (!root) return;

        var raw = kalenderRaw;
        var fases = raw.fases || [];
        var faseById = {};
        fases.forEach(function (f) { faseById[f.id] = f; });

        var MAANDEN = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
        var MAANDEN_KORT = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];
        var DAGEN = ["MA", "DI", "WO", "DO", "VR", "ZA", "ZO"];

        function pad2(n) { return n < 10 ? "0" + n : "" + n; }
        function parseISO(s) {
            if (!s) return null;
            var p = s.split("-");
            return new Date(parseInt(p[0], 10), parseInt(p[1], 10) - 1, parseInt(p[2], 10));
        }
        function fmtISO(d) { return d.getFullYear() + "-" + pad2(d.getMonth() + 1) + "-" + pad2(d.getDate()); }
        function fmtNl(d) { return pad2(d.getDate()) + "/" + pad2(d.getMonth() + 1) + "/" + d.getFullYear(); }
        function sameDay(a, b) { return a && b && a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate(); }
        function addDays(d, n) { var r = new Date(d); r.setDate(r.getDate() + n); return r; }
        function addMonths(d, n) { return new Date(d.getFullYear(), d.getMonth() + n, 1); }
        function startOfMonth(d) { return new Date(d.getFullYear(), d.getMonth(), 1); }
        function startOfQuarter(d) { var q = Math.floor(d.getMonth() / 3); return new Date(d.getFullYear(), q * 3, 1); }
        function startOfYear(d) { return new Date(d.getFullYear(), 0, 1); }
        function mondayOffset(d) { return (d.getDay() + 6) % 7; }
        function startOfWeekMon(d) { return addDays(d, -mondayOffset(d)); }
        function weekNr(d) {
            var t = new Date(d.getFullYear(), d.getMonth(), d.getDate());
            var dayNr = (t.getDay() + 6) % 7;
            t.setDate(t.getDate() - dayNr + 3);
            var firstThursday = new Date(t.getFullYear(), 0, 4);
            var diff = (t - firstThursday) / 86400000;
            return 1 + Math.round((diff - ((firstThursday.getDay() + 6) % 7)) / 7);
        }
        function escapeHtml(s) {
            return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
        }

        var today = new Date(); today.setHours(0, 0, 0, 0);

        var mijlpalen = (raw.mijlpalen || []).map(function (m) {
            return Object.assign({}, m, { _datum: parseISO(m.datum), _streef: parseISO(m.streefdatum || m.datum) });
        });

        function bucket(m) {
            if (m.isNvt) return "nvt";
            if (m.isBereikt) return "bereikt";
            if (m.overdue) return "achterstallig";
            return "nogtedoen";
        }

        var state = { view: "maand", anchor: startOfMonth(today), filter: "alle", hidden: {}, selectedId: null };

        function faseVisible(faseId) { return !state.hidden[faseId]; }
        function passesFilter(m) { return state.filter === "alle" ? true : bucket(m) === state.filter; }
        function visible(m) { return faseVisible(m.faseId) && passesFilter(m); }

        // DOM refs
        var titleEl = document.getElementById("kal2Title");
        var subtitleEl = document.getElementById("kal2Subtitle");
        var mainEl = document.getElementById("kal2Main");
        var statsCountEl = document.getElementById("kal2StatsCount");
        var statBereiktEl = document.getElementById("kal2StatBereikt");
        var statAchterstalligEl = document.getElementById("kal2StatAchterstallig");
        var statNogTeDoenEl = document.getElementById("kal2StatNogTeDoen");
        var statBinnen14El = document.getElementById("kal2StatBinnen14");
        var detailEl = document.getElementById("kal2Detail");
        var optiesBtn = document.getElementById("kal2OptiesBtn");
        var optiesMenu = document.getElementById("kal2OptiesMenu");
        var prevBtn = root.querySelector('[data-nav="prev"]');
        var nextBtn = root.querySelector('[data-nav="next"]');
        var todayBtn = root.querySelector('[data-nav="today"]');

        // ── Periode-berekening per weergave ──────────────────────────────────────────
        function periodRange() {
            if (state.view === "maand") {
                var s = startOfMonth(state.anchor);
                return { start: s, end: addMonths(s, 1) };
            }
            if (state.view === "kwartaal") {
                var qs = startOfQuarter(state.anchor);
                return { start: qs, end: addMonths(qs, 3) };
            }
            if (state.view === "jaar") {
                var ys = startOfYear(state.anchor);
                return { start: ys, end: addMonths(ys, 12) };
            }
            return { start: null, end: null }; // agenda: onbegrensd
        }

        function inRange(d, r) { return d && (!r.start || d >= r.start) && (!r.end || d < r.end); }

        // ── Titel/subtitel + nav-knoppen ─────────────────────────────────────────────
        function updateTitle() {
            var r = periodRange();
            if (state.view === "maand") {
                titleEl.textContent = MAANDEN[state.anchor.getMonth()] + " " + state.anchor.getFullYear();
                subtitleEl.textContent = fmtNl(r.start) + " – " + fmtNl(addDays(r.end, -1));
            } else if (state.view === "kwartaal") {
                var q = Math.floor(state.anchor.getMonth() / 3) + 1;
                titleEl.textContent = "Kwartaal " + q + " " + state.anchor.getFullYear();
                subtitleEl.textContent = fmtNl(r.start) + " – " + fmtNl(addDays(r.end, -1));
            } else if (state.view === "jaar") {
                titleEl.textContent = "" + state.anchor.getFullYear();
                subtitleEl.textContent = fmtNl(r.start) + " – " + fmtNl(addDays(r.end, -1));
            } else {
                titleEl.textContent = "Agenda";
                subtitleEl.textContent = "volledige looptijd";
            }
            var agenda = state.view === "agenda";
            prevBtn.disabled = agenda;
            nextBtn.disabled = agenda;
        }

        // ── Stats (rechterpaneel) ────────────────────────────────────────────────────
        function renderStats() {
            var r = periodRange();
            var scoped = mijlpalen.filter(function (m) { return faseVisible(m.faseId) && (state.view === "agenda" || inRange(m._datum, r)); });
            var bereikt = scoped.filter(function (m) { return bucket(m) === "bereikt"; }).length;
            var achterstallig = scoped.filter(function (m) { return bucket(m) === "achterstallig"; }).length;
            var nogtedoen = scoped.filter(function (m) { return bucket(m) === "nogtedoen"; }).length;
            var in14 = mijlpalen.filter(function (m) {
                return faseVisible(m.faseId) && bucket(m) === "nogtedoen" && m._streef && m._streef >= today && m._streef <= addDays(today, 14);
            }).length;
            statBereiktEl.textContent = bereikt;
            statAchterstalligEl.textContent = achterstallig;
            statNogTeDoenEl.textContent = nogtedoen;
            statBinnen14El.textContent = in14;
            statsCountEl.textContent = scoped.length + (scoped.length === 1 ? " mijlpaal" : " mijlpalen");
        }

        // ── Detailpaneel ──────────────────────────────────────────────────────────────
        function statusBadgeClass(m) {
            var b = bucket(m);
            return "is-" + b;
        }
        function statusLabel(m) {
            if (m.isNvt) return "Niet van toepassing";
            if (m.isBereikt) return "Bereikt";
            if (m.overdue) return "Achterstallig";
            return "Nog te doen";
        }
        function renderDetail() {
            var m = state.selectedId != null ? mijlpalen.filter(function (x) { return x.id === state.selectedId; })[0] : null;
            if (!m) { detailEl.hidden = true; detailEl.innerHTML = ""; return; }
            var fase = faseById[m.faseId];
            var streefTxt = m._streef ? fmtNl(m._streef) : "—";
            if (m.overdue) streefTxt += ' <span class="is-overdue">(' + m.dagenTeLaat + " d te laat)</span>";
            else if (m.isBereikt && m.bereiktOp) streefTxt = fmtNl(parseISO(m.bereiktOp)) + " (bereikt)";

            var actiesHtml = "";
            if (m.triggers && m.triggers.length) {
                actiesHtml = '<div class="gl-kal2-detail-actions-head">Acties bij bereiken</div>' +
                    m.triggers.map(function (t) {
                        return '<div class="gl-kal2-detail-action"><i class="bx bx-bolt-circle" aria-hidden="true"></i>' + escapeHtml(t.label) + "</div>";
                    }).join("");
            }

            detailEl.innerHTML =
                '<div class="gl-kal2-detail-head"><div class="gl-kal2-detail-title">' + escapeHtml(m.naam) + '</div>' +
                '<button type="button" class="gl-kal2-detail-close" id="kal2DetailClose" aria-label="Sluiten">&times;</button></div>' +
                '<span class="gl-kal2-detail-badge ' + statusBadgeClass(m) + '">' + statusLabel(m) + "</span>" +
                '<dl class="gl-kal2-detail-body">' +
                '<div class="gl-kal2-detail-row"><dt>Fase</dt><dd><span class="gl-kal2-detail-fasedot" style="background:' + (fase ? fase.kleur : "#8590a5") + '"></span>' + escapeHtml(fase ? fase.naam : "—") + "</dd></div>" +
                '<div class="gl-kal2-detail-row"><dt>Streefdatum</dt><dd' + (m.overdue ? ' class="is-overdue"' : "") + ">" + streefTxt + "</dd></div>" +
                '<div class="gl-kal2-detail-row"><dt>Verantwoordelijk</dt><dd>' + escapeHtml(m.rol || "—") + "</dd></div>" +
                '<div class="gl-kal2-detail-row"><dt>Geldt voor</dt><dd>' + escapeHtml(m.geldtVoor) + "</dd></div>" +
                "</dl>" + actiesHtml +
                '<div class="gl-kal2-detail-foot">' +
                (m.isBereikt ? "" : '<button type="button" class="btn btn-primary btn-sm" id="kal2BtnBereikt"><i class="bx bx-check me-1" aria-hidden="true"></i>Markeer bereikt</button>') +
                '<button type="button" class="btn btn-default btn-sm" id="kal2BtnDatum"><i class="bx bx-calendar-edit me-1" aria-hidden="true"></i>Datum wijzigen</button>' +
                "</div>";
            detailEl.hidden = false;

            var closeBtn = document.getElementById("kal2DetailClose");
            if (closeBtn) closeBtn.addEventListener("click", function () { selectMijlpaal(null); });
            var bereiktBtn = document.getElementById("kal2BtnBereikt");
            if (bereiktBtn) bereiktBtn.addEventListener("click", function () { openMijlpaalStatus(m.id, m.naam, 2); });
            var datumBtn = document.getElementById("kal2BtnDatum");
            if (datumBtn) datumBtn.addEventListener("click", function () { openMijlpaalEdit(m.id); });
        }
        function selectMijlpaal(id) {
            state.selectedId = id;
            renderDetail();
            renderActive();
        }
        function renderActive() {
            mainEl.querySelectorAll("[data-mp-id]").forEach(function (el) {
                el.classList.toggle("is-selected", state.selectedId != null && el.getAttribute("data-mp-id") === String(state.selectedId));
            });
        }

        // Bindt zowel click als toetsenbord (Enter/Spatie) op elk [data-mp-id]-element — de
        // agenda-rijen en tijdlijn-stippen zijn losse <div>'s (geen native knop, ivm layout/
        // positionering), dus zonder dit zijn ze met het toetsenbord onbereikbaar. Elk element
        // krijgt in de render-functies zelf al tabindex="0" role="button" mee.
        function wireMpClickable(container) {
            container.querySelectorAll("[data-mp-id]").forEach(function (el) {
                el.addEventListener("click", function () { selectMijlpaal(parseInt(el.getAttribute("data-mp-id"), 10)); });
                el.addEventListener("keydown", function (e) {
                    if (e.key !== "Enter" && e.key !== " " && e.key !== "Spacebar") return;
                    e.preventDefault();
                    selectMijlpaal(parseInt(el.getAttribute("data-mp-id"), 10));
                });
            });
        }

        // ── "Fases & opties" dropdown ─────────────────────────────────────────────────
        function renderOptiesMenu() {
            optiesMenu.innerHTML = fases.map(function (f) {
                var checked = !state.hidden[f.id] ? " checked" : "";
                return '<div class="gl-kal2-opties-row"><span class="gl-kal2-dot" style="background:' + f.kleur + '"></span>' +
                    '<label for="kal2Fase' + f.id + '">' + escapeHtml(f.naam) + "</label>" +
                    '<input type="checkbox" id="kal2Fase' + f.id + '" data-fase-toggle="' + f.id + '"' + checked + " /></div>";
            }).join("") +
                '<div class="gl-kal2-opties-foot"><button type="button" id="kal2OptiesAlle">Alles tonen</button><button type="button" id="kal2OptiesGeen">Alles verbergen</button></div>';

            optiesMenu.querySelectorAll("[data-fase-toggle]").forEach(function (cb) {
                cb.addEventListener("change", function () {
                    var id = parseInt(cb.getAttribute("data-fase-toggle"), 10);
                    if (cb.checked) delete state.hidden[id]; else state.hidden[id] = true;
                    render();
                });
            });
            var alleBtn = document.getElementById("kal2OptiesAlle");
            var geenBtn = document.getElementById("kal2OptiesGeen");
            if (alleBtn) alleBtn.addEventListener("click", function () { state.hidden = {}; renderOptiesMenu(); render(); });
            if (geenBtn) geenBtn.addEventListener("click", function () {
                fases.forEach(function (f) { state.hidden[f.id] = true; });
                renderOptiesMenu(); render();
            });
        }

        // ── Weergave: Maand ───────────────────────────────────────────────────────────
        function renderMaand() {
            var r = periodRange();
            var projStart = null, projEnd = null;
            fases.forEach(function (f) {
                var s = parseISO(f.start), e = parseISO(f.eind);
                if (s && (!projStart || s < projStart)) projStart = s;
                if (e && (!projEnd || e > projEnd)) projEnd = e;
            });
            var totalSpan = (projStart && projEnd && projEnd > projStart) ? (projEnd - projStart) : 1;

            var swimHtml = '<div class="gl-kal2-swimlane">' + fases.filter(function (f) { return faseVisible(f.id); }).map(function (f) {
                var s = parseISO(f.start), e = parseISO(f.eind);
                var countInPeriod = mijlpalen.filter(function (m) { return m.faseId === f.id && passesFilter(m) && inRange(m._datum, r); }).length;
                var active = countInPeriod > 0;
                var left = 0, width = 100;
                if (projStart && s && e) {
                    left = Math.max(0, ((s - projStart) / totalSpan) * 100);
                    width = Math.max(3, ((e - s) / totalSpan) * 100);
                    width = Math.min(width, 100 - left);
                }
                var label = active ? (escapeHtml(f.naam) + '<span class="gl-kal2-swimlane-count">' + countInPeriod + " deze " + (state.view === "maand" ? "maand" : "periode") + "</span>")
                    : (escapeHtml(f.naam) + '<span class="gl-kal2-swimlane-count">' + (s && e ? (MAANDEN_KORT[s.getMonth()] + " ’" + (s.getFullYear() % 100) + " – " + MAANDEN_KORT[e.getMonth()] + " ’" + (e.getFullYear() % 100)) : "") + "</span>");
                return '<div class="gl-kal2-swimlane-row"><div class="gl-kal2-swimlane-bar' + (active ? "" : " is-muted") + '" style="left:' + left.toFixed(2) + "%;width:" + width.toFixed(2) + "%;" + (active ? "background:" + f.kleur : "") + '">' + label + "</div></div>";
            }).join("") + "</div>";

            var totalCount = mijlpalen.filter(function (m) { return faseVisible(m.faseId) && passesFilter(m) && inRange(m._datum, r); }).length;
            var faseCountLopen = fases.filter(function (f) { return faseVisible(f.id); }).length;
            var summary = '<div class="d-flex justify-content-end" style="font-size:.75rem;color:var(--tsa-muted-aa,#5b6472);margin:-8px 0 8px">' + totalCount + " mijlpalen deze " + (state.view === "maand" ? "maand" : "periode") + " · " + faseCountLopen + " fases lopen</div>";

            var gridStart = startOfWeekMon(r.start);
            var gridEnd = addDays(startOfWeekMon(addDays(r.end, 6)), 0);
            while (gridEnd < r.end || mondayOffset(gridEnd) !== 0) gridEnd = addDays(gridEnd, 1);
            var days = [];
            for (var d = new Date(gridStart); d < gridEnd; d = addDays(d, 1)) days.push(new Date(d));

            var head = '<div class="gl-kal2-grid-head">' + DAGEN.map(function (d) { return "<span>" + d + "</span>"; }).join("") + "</div>";
            var body = '<div class="gl-kal2-grid-body">' + days.map(function (day) {
                var outside = day < r.start || day >= r.end;
                var isToday = sameDay(day, today);
                var dayItems = mijlpalen.filter(function (m) { return m._datum && sameDay(m._datum, day) && visible(m); });
                var chips = dayItems.map(function (m) {
                    var icon = m.isBereikt ? "bx-check-circle" : (bucket(m) === "achterstallig" ? "bx-error-circle" : "bx-circle");
                    return '<button type="button" class="gl-kal2-chip is-' + bucket(m) + (m.triggers && m.triggers.length ? " has-trigger" : "") + '" data-mp-id="' + m.id + '" title="' + escapeHtml(m.naam) + '">' +
                        '<i class="bx ' + icon + '" aria-hidden="true"></i><span>' + escapeHtml(m.naam) + "</span></button>";
                }).join("");
                return '<div class="gl-kal2-day' + (outside ? " is-outside" : "") + (isToday ? " is-today" : "") + '"><span class="gl-kal2-daynum">' + day.getDate() + "</span>" + chips + "</div>";
            }).join("") + "</div>";

            mainEl.innerHTML = swimHtml + summary + '<div class="gl-kal2-grid">' + head + body + "</div>";
            wireMpClickable(mainEl);
            renderActive();
        }

        // ── Weergave: Kwartaal / Jaar (Gantt-achtige tijdlijn) ─────────────────────────
        // Kolommen verdelen zich procentueel over de volledige breedte. De fase-lijst (links) en
        // de tijdlijn (rechts) zijn cellen van ÉÉN CSS Grid (.gl-kal2-tl, grid-template-columns:
        // 230px 1fr) i.p.v. twee onafhankelijke kolom-blokken — Grid lijnt rijen dan vanzelf op
        // gelijke hoogte uit, ook als de linkertekst (bv. lange datumrange) meer regels nodig heeft
        // dan de rechterkant. Twee losse blokken met een JS-berekende min-height (vorige aanpak)
        // liepen uit sync zodra die tekst breder was dan verwacht.
        var LABEL_ROW_H = 26, LABEL_TOP0 = 34, ROW_MIN_H = 62, DOT_TOP = 20;
        function renderTimeline() {
            var r = periodRange();
            var isJaar = state.view === "jaar";
            var cols = [];
            if (isJaar) {
                for (var i = 0; i < 12; i++) cols.push({ label: MAANDEN_KORT[(r.start.getMonth() + i) % 12] });
            } else {
                var wk = startOfWeekMon(r.start);
                while (wk < r.end) { cols.push({ label: "w" + weekNr(wk) }); wk = addDays(wk, 7); }
            }
            function xFor(d) {
                if (!d) return null;
                var clamped = d < r.start ? r.start : (d > r.end ? r.end : d);
                return ((clamped - r.start) / (r.end - r.start)) * 100;
            }

            var visFases = fases.filter(function (f) { return faseVisible(f.id); });
            if (visFases.length === 0) {
                mainEl.innerHTML = '<div class="gl-kal2-tl-empty">Geen fases zichtbaar — pas de filters in "Fases &amp; opties" aan.</div>';
                return;
            }

            var axisHtml = '<div class="gl-kal2-tl-axis-spacer"></div><div class="gl-kal2-tl-axis">' + cols.map(function (c) { return '<span>' + c.label + "</span>"; }).join("") + "</div>";

            var gridHtml = visFases.map(function (f) {
                var items = mijlpalen.filter(function (m) { return m.faseId === f.id && passesFilter(m) && m._datum && inRange(m._datum, r); })
                    .sort(function (a, b) { return a._datum - b._datum; });
                var s = parseISO(f.start), e = parseISO(f.eind);

                // Fase-metacel (links)
                var inPeriodCount = items.length;
                var achterstalligCount = items.filter(function (m) { return bucket(m) === "achterstallig"; }).length;
                var metaHtml = '<div class="gl-kal2-tl-fase-row"><div class="gl-kal2-tl-fase-naam"><span class="gl-kal2-dot" style="background:' + f.kleur + '"></span>' + escapeHtml(f.naam) + "</div>" +
                    '<div class="gl-kal2-tl-fase-meta">' + inPeriodCount + " in deze periode" + (achterstalligCount > 0 ? ' · <span class="is-achterstallig">' + achterstalligCount + " achterstallig</span>" : "") +
                    (s && e ? "<br/>" + fmtNl(s) + " – " + fmtNl(e) : "") + "</div></div>";

                // Tijdlijncel (rechts)
                var barHtml = "";
                if (s && e && e >= r.start && s <= r.end) {
                    var x1 = xFor(s < r.start ? r.start : s), x2 = xFor(e > r.end ? r.end : e);
                    barHtml = '<div class="gl-kal2-tl-bar" style="left:' + x1 + "%;width:" + Math.max(0.6, x2 - x1) + "%;background:" + f.kleur + '"></div>';
                }
                // Botsingsdetectie: mijlpalen op (bijna) dezelfde datum clusteren op ~3.5% van de
                // periodebreedte. De stippen zelf blijven op DOT_TOP (met een kleine horizontale
                // jitter zodat ze niet exact op elkaar liggen); enkel de labels erónder stapelen
                // verticaal — anders schuift een latere stip dwars door de tekst van een eerdere
                // label (precies de bug uit de vorige screenshot).
                var clusters = [];
                items.forEach(function (m) {
                    var x = xFor(m._datum);
                    var cluster = clusters.filter(function (c) { return Math.abs(c.x - x) < 3.5; })[0];
                    if (!cluster) { cluster = { x: x, items: [] }; clusters.push(cluster); }
                    cluster.items.push(m);
                });
                var rowStack = 0;
                var pointsHtml = clusters.map(function (cluster) {
                    rowStack = Math.max(rowStack, cluster.items.length);
                    return cluster.items.map(function (m, stackIdx) {
                        var x = xFor(m._datum);
                        var jitter = (stackIdx % 2 === 0 ? 1 : -1) * Math.ceil(stackIdx / 2) * 5;
                        var kleur = m.isBereikt ? "var(--kal2-bereikt)" : (bucket(m) === "achterstallig" ? "var(--kal2-achterstallig)" : "var(--kal2-nogtedoen)");
                        return '<div class="gl-kal2-tl-point' + (m.triggers && m.triggers.length ? " has-trigger" : "") + (state.selectedId === m.id ? " is-selected" : "") + '" data-mp-id="' + m.id + '" style="left:calc(' + x + "% + " + jitter + "px);top:" + DOT_TOP + "px;background:" + kleur + '" title="' + escapeHtml(m.naam) + '" tabindex="0" role="button" aria-label="' + escapeHtml(m.naam + ", " + fmtNl(m._datum)) + '"></div>' +
                            '<div class="gl-kal2-tl-label" style="left:' + x + "%;top:" + (LABEL_TOP0 + stackIdx * LABEL_ROW_H) + 'px"><strong>' + escapeHtml(m.naam.length > 26 ? m.naam.slice(0, 24) + "…" : m.naam) + "</strong><span>" + fmtNl(m._datum) + "</span></div>";
                    }).join("");
                }).join("");
                var todayLine = (today >= r.start && today < r.end) ? '<div class="gl-kal2-tl-today" style="left:' + xFor(today) + '%"></div>' : "";
                var rowMinH = Math.max(ROW_MIN_H, LABEL_TOP0 + rowStack * LABEL_ROW_H + 12);
                var rowHtml = '<div class="gl-kal2-tl-row" style="min-height:' + rowMinH + 'px">' + barHtml + pointsHtml + todayLine + "</div>";

                return metaHtml + rowHtml;
            }).join("");

            mainEl.innerHTML = '<div class="gl-kal2-tl">' + axisHtml + gridHtml + "</div>";

            wireMpClickable(mainEl);
            renderActive();
        }

        // ── Weergave: Agenda ────────────────────────────────────────────────────────
        function renderAgenda() {
            var items = mijlpalen.filter(visible).filter(function (m) { return m._datum; })
                .sort(function (a, b) { return a._datum - b._datum; });

            if (items.length === 0) {
                mainEl.innerHTML = '<div class="gl-kal2-agenda-empty">Geen mijlpalen voor deze filter.</div>';
                return;
            }

            var groups = [];
            var curKey = null, curGroup = null;
            items.forEach(function (m) {
                var key = m._datum.getFullYear() + "-" + m._datum.getMonth();
                if (key !== curKey) {
                    curGroup = { label: MAANDEN[m._datum.getMonth()] + " " + m._datum.getFullYear(), items: [] };
                    groups.push(curGroup);
                    curKey = key;
                }
                curGroup.items.push(m);
            });

            mainEl.innerHTML = groups.map(function (g) {
                var achterstallig = g.items.filter(function (m) { return bucket(m) === "achterstallig"; }).length;
                var head = '<div class="gl-kal2-agenda-group-head"><span>' + escapeHtml(g.label) + "</span><span>" + g.items.length + (g.items.length === 1 ? " mijlpaal" : " mijlpalen") + (achterstallig > 0 ? " · " + achterstallig + " achterstallig" : "") + "</span></div>";
                var rows = g.items.map(function (m) {
                    var fase = faseById[m.faseId];
                    var b = bucket(m);
                    var metaParts = [fase ? fase.naam : "", m.rol || ""].filter(Boolean);
                    var dateTxt = m.isBereikt ? "bereikt op " + fmtNl(m._datum) : (m.overdue ? m.dagenTeLaat + " dagen te laat" : "");
                    if (dateTxt) metaParts.push(dateTxt);
                    var triggerBadge = m.triggers && m.triggers.length ? '<span class="gl-kal2-agenda-trigger"><i class="bx bx-bolt-circle" aria-hidden="true"></i>' + m.triggers.length + "</span>" : "";
                    return '<div class="gl-kal2-agenda-row is-' + b + (state.selectedId === m.id ? " is-selected" : "") + '" data-mp-id="' + m.id + '" tabindex="0" role="button" aria-label="' + escapeHtml(m.naam + ", " + statusLabel(m)) + '">' +
                        '<div class="gl-kal2-agenda-date">' + fmtNl(m._datum) + "</div>" +
                        '<span class="gl-kal2-dot is-' + b + '"></span>' +
                        '<div class="gl-kal2-agenda-main"><div class="gl-kal2-agenda-naam">' + escapeHtml(m.naam) + '</div><div class="gl-kal2-agenda-meta">' + escapeHtml(metaParts.join(" · ")) + "</div></div>" +
                        '<div class="gl-kal2-agenda-right">' + triggerBadge + '<span class="gl-kal2-agenda-badge is-' + b + '">' + statusLabel(m) + "</span></div></div>";
                }).join("");
                return '<div class="gl-kal2-agenda-group">' + head + rows + "</div>";
            }).join("");

            wireMpClickable(mainEl);
            renderActive();
        }

        // ── Render-dispatch + navigatie ─────────────────────────────────────────────
        function render() {
            updateTitle();
            if (state.view === "maand") renderMaand();
            else if (state.view === "kwartaal") renderTimeline();
            else if (state.view === "jaar") renderTimeline();
            else renderAgenda();
            renderStats();
            renderDetail();
        }

        prevBtn.addEventListener("click", function () { step(-1); });
        nextBtn.addEventListener("click", function () { step(1); });
        todayBtn.addEventListener("click", function () {
            state.anchor = state.view === "maand" ? startOfMonth(today) : state.view === "kwartaal" ? startOfQuarter(today) : startOfYear(today);
            render();
        });
        function step(dir) {
            if (state.view === "maand") state.anchor = addMonths(state.anchor, dir);
            else if (state.view === "kwartaal") state.anchor = addMonths(state.anchor, dir * 3);
            else if (state.view === "jaar") state.anchor = addMonths(state.anchor, dir * 12);
            render();
        }

        // De view-switch draagt role="tab" (_Kalender.cshtml) maar had tot nu toe enkel een
        // click-handler — geassisteerde technologie kondigde tablist-gedrag aan dat er niet was
        // (/impeccable critique P3-fix). Roving tabindex + pijltjes/Home/End, zoals een echte
        // tablist hoort te werken; aria-selected volgt de actieve weergave.
        var viewBtns = root.querySelectorAll(".gl-kal2-viewswitch [data-view]");
        function setActiveViewBtn(btn) {
            viewBtns.forEach(function (b) {
                var active = b === btn;
                b.classList.toggle("is-active", active);
                b.setAttribute("aria-selected", active ? "true" : "false");
                b.tabIndex = active ? 0 : -1;
            });
        }
        function activateView(btn) {
            state.view = btn.getAttribute("data-view");
            setActiveViewBtn(btn);
            if (state.view === "maand") state.anchor = startOfMonth(state.anchor);
            else if (state.view === "kwartaal") state.anchor = startOfQuarter(state.anchor);
            else if (state.view === "jaar") state.anchor = startOfYear(state.anchor);
            render();
        }
        viewBtns.forEach(function (btn, idx) {
            btn.addEventListener("click", function () { activateView(btn); });
            btn.addEventListener("keydown", function (e) {
                var target = null;
                if (e.key === "ArrowRight") target = viewBtns[(idx + 1) % viewBtns.length];
                else if (e.key === "ArrowLeft") target = viewBtns[(idx - 1 + viewBtns.length) % viewBtns.length];
                else if (e.key === "Home") target = viewBtns[0];
                else if (e.key === "End") target = viewBtns[viewBtns.length - 1];
                else return;
                e.preventDefault();
                target.focus();
                activateView(target);
            });
        });
        setActiveViewBtn(root.querySelector(".gl-kal2-viewswitch [data-view].is-active") || viewBtns[0]);
        root.querySelectorAll(".gl-kal2-filters [data-filter]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                state.filter = btn.getAttribute("data-filter");
                root.querySelectorAll(".gl-kal2-filters [data-filter]").forEach(function (b) { b.classList.toggle("is-active", b === btn); });
                render();
            });
        });
        optiesBtn.addEventListener("click", function () {
            var open = optiesMenu.hidden;
            optiesMenu.hidden = !open;
            optiesBtn.setAttribute("aria-expanded", open ? "true" : "false");
        });
        document.addEventListener("click", function (e) {
            if (!optiesMenu.hidden && !e.target.closest(".gl-kal2-opties")) { optiesMenu.hidden = true; optiesBtn.setAttribute("aria-expanded", "false"); }
        });

        renderOptiesMenu();
        render();
    }
})();
