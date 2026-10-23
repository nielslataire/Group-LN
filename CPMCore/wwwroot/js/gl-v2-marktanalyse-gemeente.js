// gl-v2 — Marktanalyse › Gemeenteanalyse (design-handoff punt 46a). Enkel geladen door Views/Marktanalyse/GemeenteanalyseV2.cshtml.
// Doet vier dingen: (1) de gekozen locatie onthouden en terugzetten als de pagina zonder filter opent, (2) filters meteen toepassen,
// (3) de twee tabellen als DataTables met één zoekveld en een schakelaar, (4) de maandgrafiek (Chart.js) in de kleuren van gl-v2.
(function () {
    "use strict";

    var LS_MUNI = "ma_last_municipality_id";
    var LS_SECTION = "ma_last_section_id";
    var LS_LABEL = "ma_last_label";
    var LS_TYPE = "ma_last_type";
    var LS_VIEW = "ma_tabelview";

    var root = document.getElementById("gl-v2-ma");
    var form = document.getElementById("ma-filter");
    if (!root || !form) return;

    var hidMuni = document.getElementById("ma-hidden-muni");
    var hidSection = document.getElementById("ma-hidden-section");
    var hasFilter = root.getAttribute("data-has-filter") === "true";

    function store(key, value) { try { if (value === null || value === "") localStorage.removeItem(key); else localStorage.setItem(key, value); } catch (e) { /* opslag mag ontbreken */ } }
    function load(key) { try { return localStorage.getItem(key); } catch (e) { return null; } }

    // ── Locatie onthouden; zonder filter terug naar de laatst gebruikte locatie ───────────────────────────────────────────────
    var typeChecked = form.querySelector('input[name="type"]:checked');
    if (hasFilter) {
        store(LS_MUNI, hidMuni.value);
        store(LS_SECTION, hidSection.value);
        var input = document.getElementById("ma-loc_input");
        if (input && input.value) store(LS_LABEL, input.value);
        if (typeChecked) store(LS_TYPE, typeChecked.value);
    } else {
        var lastMuni = load(LS_MUNI), lastSection = load(LS_SECTION), lastType = load(LS_TYPE) || "Alles";
        if (lastMuni || lastSection) {
            var url = form.getAttribute("action") + "?type=" + encodeURIComponent(lastType);
            url += lastSection ? "&geoMunicipalSectionId=" + encodeURIComponent(lastSection) : "&geoMunicipalityId=" + encodeURIComponent(lastMuni);
            window.location.replace(url);
            return;
        }
    }

    // ── Een locatie kiezen in de keuzelijst: "m12" = hele gemeente, "s5" = één deelgemeente ───────────────────────────────────
    document.addEventListener("gl-v2:combo-select", function (e) {
        var d = e.detail || {};
        if (!e.target.closest || !e.target.closest(".gl-v2-ma-loc")) return;
        var id = String(d.id || "");
        if (id.charAt(0) === "m") { hidMuni.value = id.substring(1); hidSection.value = ""; }
        else if (id.charAt(0) === "s") { hidMuni.value = ""; hidSection.value = id.substring(1); }
        else return;
        store(LS_MUNI, hidMuni.value);
        store(LS_SECTION, hidSection.value);
        store(LS_LABEL, d.text || "");
        form.requestSubmit();
    });

    // ── Type, aanbod en periode passen meteen toe ──────────────────────────────────────────────────────────────────────────────
    form.querySelectorAll('input[type="radio"]').forEach(function (radio) {
        radio.addEventListener("change", function () {
            if (hidMuni.value || hidSection.value) form.requestSubmit();
        });
    });

    // ── Tabellen ───────────────────────────────────────────────────────────────────────────────────────────────────────────────
    var paneP = document.getElementById("ma-pane-projecten");
    var paneE = document.getElementById("ma-pane-eenheden");
    var searchInput = document.getElementById("ma-table-search");
    var hint = document.getElementById("ma-table-hint");
    var switcher = document.getElementById("ma-view-switch");
    var tables = {};

    function dtOptions(order, lastColumnIndex) {
        return {
            order: order,
            pageLength: 15,
            layout: { topStart: null, topEnd: null, bottomStart: "info", bottomEnd: "paging" },
            columnDefs: [{ orderable: false, targets: lastColumnIndex }],
            language: {
                info: "_START_ tot _END_ van _TOTAL_",
                infoEmpty: "Geen resultaten",
                infoFiltered: "(gefilterd uit _MAX_)",
                zeroRecords: "Niets gevonden voor deze zoekterm.",
                emptyTable: "Geen gegevens in deze selectie.",
                paginate: { previous: "‹", next: "›" }
            }
        };
    }

    if (paneP && typeof DataTable !== "undefined") {
        tables.Projecten = new DataTable("#ma-tbl-projecten", dtOptions([[6, "desc"]], 9));
        tables["Losse eenheden"] = new DataTable("#ma-tbl-eenheden", dtOptions([[0, "asc"]], 10));
    }

    function open(url, nieuwTabblad) {
        if (!url) return;
        if (nieuwTabblad) window.open(url, "_blank", "noopener"); else window.location.href = url;
    }
    [["ma-tbl-projecten", false], ["ma-tbl-eenheden", true]].forEach(function (cfg) {
        var table = document.getElementById(cfg[0]);
        if (!table) return;
        table.addEventListener("click", function (e) {
            if (e.target.closest("a")) return;
            var tr = e.target.closest("tr.is-clickable");
            if (tr) open(tr.getAttribute("data-url"), cfg[1]);
        });
    });

    function setView(view) {
        if (!tables[view]) view = "Projecten";
        store(LS_VIEW, view);
        var isProjecten = view === "Projecten";
        if (paneP) paneP.hidden = !isProjecten;
        if (paneE) paneE.hidden = isProjecten;
        if (switcher) switcher.querySelectorAll("button").forEach(function (b) {
            var active = b.getAttribute("data-view") === view;
            b.classList.toggle("is-active", active);
            b.setAttribute("aria-pressed", active ? "true" : "false");
        });
        if (searchInput) {
            searchInput.placeholder = isProjecten ? "Zoek project of ontwikkelaar" : "Zoek adres of gemeente";
            searchInput.value = "";
        }
        if (hint) hint.textContent = isProjecten ? "klik een rij voor het projectdetail" : "klik een rij om de advertentie te openen";
        Object.keys(tables).forEach(function (k) { tables[k].search("").draw(); });
    }

    if (switcher) {
        switcher.addEventListener("click", function (e) {
            var btn = e.target.closest("button[data-view]");
            if (btn) setView(btn.getAttribute("data-view"));
        });
        setView(load(LS_VIEW) || "Projecten");
    }
    if (searchInput) {
        searchInput.addEventListener("input", function () {
            var view = switcher && switcher.querySelector("button.is-active");
            var table = tables[view ? view.getAttribute("data-view") : "Projecten"];
            if (table) table.search(this.value).draw();
        });
    }

    // ── Maandgrafiek: lijn = te koop op het einde van de maand, staven = verkocht in de maand ──────────────────────────────────
    var canvas = document.getElementById("ma-chart-aanbod");
    var data = window.glV2MaData;
    if (canvas && data && typeof Chart !== "undefined") {
        var css = getComputedStyle(document.documentElement);
        var primary = (css.getPropertyValue("--gl-v2-primary") || "#00532D").trim() || "#00532D";
        var gold = (css.getPropertyValue("--gl-v2-gold") || "#C9A96E").trim() || "#C9A96E";
        var tick = "#7a8a78", grid = "rgba(44,59,42,.08)";
        Chart.defaults.font.family = "'IBM Plex Sans', sans-serif";
        new Chart(canvas, {
            data: {
                labels: data.labels,
                datasets: [
                    { type: "bar", label: "Verkocht in de maand", data: data.verkocht, backgroundColor: gold, borderRadius: 4, maxBarThickness: 44, order: 2 },
                    { type: "line", label: "Te koop (einde maand)", data: data.aanbod, borderColor: primary, backgroundColor: "#fff", borderWidth: 2.5, tension: 0, pointRadius: 3.5, pointBorderWidth: 2, pointBorderColor: primary, pointBackgroundColor: "#fff", order: 1 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: "index", intersect: false },
                plugins: {
                    legend: { display: false },
                    tooltip: { backgroundColor: "#2C3B2A", padding: 10, titleFont: { weight: "600" }, boxPadding: 4 }
                },
                scales: {
                    x: { grid: { display: false }, border: { display: false }, ticks: { color: tick, font: { size: 10.5 } } },
                    y: { beginAtZero: true, border: { display: false }, grid: { color: grid }, ticks: { color: tick, precision: 0, font: { size: 10 } } }
                }
            }
        });
    }
})();
