// gl-v2 layout-pilot — Weerverlet/jaarkalender (design-handoff optie 10a). Enkel geladen door
// Views/Projecten/WeatherV2.cshtml. Zelfde AJAX-endpoints als de legacy FullCalendar-versie
// (Weather.cshtml se eigen inline script) — GetCalendarBundle/AddBadWeatherDay/DeleteBadWeatherDay
// ongewijzigd — maar het hele jaarraster (12 maandkaarten, dag-status, klik/sleep-interactie) is
// hier eigen JS i.p.v. FullCalendar, zodat het 10a se eigen kleuren-/vorm-regels kan volgen (zie
// gl-v2-weerverlet.css se intro-comment).
//
// GetCalendarBundle negeert zijn eigen `year`-parameter server-side (retourneert alle jaren voor
// het station) — dus één fetch per stationwissel volstaat, jaarnavigatie filtert/tekent enkel
// opnieuw uit de al-opgehaalde bundel (state.rain/state.wind/state.vacation), geen extra request.
(function () {
    "use strict";

    var config = window.glV2WeerverletConfig || {};
    var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

    var monthsEl = document.getElementById("weer-months");
    if (!monthsEl) return;


    var state = {
        stationId: parseInt(config.initialStationId, 10) || 0,
        year: parseInt(config.initialYear, 10) || new Date().getFullYear(),
        armedType: "rain",
        canWrite: !!config.canWrite,
        rain: new Map(),
        wind: new Map(),
        vacation: new Set()
    };

    function dateKey(y, m, d) { return y + "-" + String(m).padStart(2, "0") + "-" + String(d).padStart(2, "0"); }
    function setText(id, val) { var el = document.getElementById(id); if (el) el.textContent = val; }
    function mapFor(type) { return type === "wind" ? state.wind : state.rain; }


    // ── Weerstation-select — zelfde BASIS-paneelvariant (optie 4h) als Leveranciers/Klanten/
    // Projecten. ─────────────────────────────────────────────────────────────────────────────────
    function initSelect(prefix, onChange) {
        var trigger = document.getElementById(prefix + "-trigger");
        var panel = document.getElementById(prefix + "-panel");
        var backdrop = document.getElementById(prefix + "-backdrop");
        var label = document.getElementById(prefix + "-label");
        var close = document.getElementById(prefix + "-close");
        var valueEl = document.getElementById(prefix + "-value");
        if (!trigger || !panel) return { select: function () {} };

        function closeSelect() {
            panel.classList.remove("is-open");
            if (backdrop) backdrop.classList.remove("is-open");
            trigger.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
        }
        function positionPanel() {
            if (window.innerWidth < 768) return;
            var rect = trigger.getBoundingClientRect();
            var panelWidth = panel.offsetWidth || 240;
            var left = Math.min(rect.left, window.innerWidth - panelWidth - 12);
            panel.style.left = Math.max(12, left) + "px";
            var top = rect.bottom + 6;
            var maxTop = window.innerHeight - panel.offsetHeight - 12;
            panel.style.top = Math.max(12, Math.min(top, maxTop)) + "px";
        }
        function openSelect() {
            panel.classList.add("is-open");
            if (backdrop) backdrop.classList.add("is-open");
            trigger.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            positionPanel();
        }
        function selectValue(value, fireOnChange) {
            var option = panel.querySelector('.gl-v2-select-option[data-value="' + value + '"]') || panel.querySelector(".gl-v2-select-option");
            if (!option) return;
            var lbl = option.getAttribute("data-label") || "";
            panel.querySelectorAll(".gl-v2-select-option").forEach(function (o) { o.classList.toggle("is-selected", o === option); });
            if (label) label.textContent = lbl;
            trigger.classList.toggle("is-filled", true);
            if (valueEl) valueEl.value = value;
            if (fireOnChange !== false) onChange(value);
        }
        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            if (panel.classList.contains("is-open")) closeSelect(); else openSelect();
        });
        if (backdrop) backdrop.addEventListener("click", closeSelect);
        if (close) close.addEventListener("click", closeSelect);
        panel.querySelectorAll(".gl-v2-select-option").forEach(function (option) {
            option.addEventListener("click", function () {
                selectValue(option.getAttribute("data-value") || "");
                closeSelect();
            });
        });
        document.addEventListener("click", function (e) {
            if (!panel.classList.contains("is-open")) return;
            if (e.target.closest("#" + prefix)) return;
            closeSelect();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) closeSelect();
        });
        window.addEventListener("resize", function () {
            if (panel.classList.contains("is-open")) positionPanel();
        });

        return { select: selectValue };
    }

    initSelect("weerstation-select", function (value) {
        state.stationId = parseInt(value, 10) || 0;
        loadBundle();
    });

    // ── "Wat zet je bij een klik"-schakelaar. ────────────────────────────────────────────────────
    var segmentedButtons = document.querySelectorAll("#weer-type-segmented .gl-v2-weer-segmented-option");
    segmentedButtons.forEach(function (btn) {
        btn.addEventListener("click", function () {
            state.armedType = btn.dataset.type === "wind" ? "wind" : "rain";
            segmentedButtons.forEach(function (b) {
                var active = b === btn;
                b.classList.toggle("is-active", active);
                b.setAttribute("aria-pressed", active.toString());
            });
            cal.setArmed(state.armedType);
        });
    });


    // ── Bundel ophalen (per stationwissel). ──────────────────────────────────────────────────────
    function loadBundle() {
        monthsEl.setAttribute("aria-busy", "true");
        if (!state.stationId) {
            cal.setMessage('Geen weerstation geselecteerd.');
            return;
        }
        cal.setMessage('<i class="ph ph-circle-notch gl-v2-spin" aria-hidden="true"></i> Kalender laden…');

        var url = config.bundleUrl + "?weatherstationid=" + state.stationId + "&year=" + state.year;
        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (bundle) {
                state.rain = new Map((bundle.rain || []).map(function (d) { return [dateKey(d.year, d.month, d.day), d.id]; }));
                state.wind = new Map((bundle.wind || []).map(function (d) { return [dateKey(d.year, d.month, d.day), d.id]; }));
                state.vacation = new Set((bundle.vacation || []).map(function (d) { return dateKey(d.year, d.month, d.day); }));
                renderAll();
            })
            .catch(function () {
                cal.setMessage('Kon de kalender niet laden.');
            });
    }

    // ── Jaarkalender: gedeeld component (js/gl-v2-yearcal.js, DESIGN.md "Jaarkalender (GlV2YearCal)"). Het component tekent de
    // 12 maandkaarten, telt en vangt klik/sleep op; deze pagina bewaart de dagen (state.rain/wind/vacation) en praat met de server.
    var cal = window.GlV2YearCal.create(monthsEl, {
        year: state.year,
        readonly: !state.canWrite,
        armed: state.armedType,
        categories: {
            rain: { label: "Regendag", bg: "#D7E6EF", fg: "#2E5F7E" },
            wind: { label: "Winddag", bg: "#F6E9CE", fg: "#8A5F12" }
        },
        locks: { vacation: { label: "Verlofdag", bg: "var(--gl-v2-primary-tint)", fg: "var(--gl-v2-primary)" } },
        getCats: function (iso) {
            var cats = [];
            if (state.rain.has(iso)) cats.push("rain");
            if (state.wind.has(iso)) cats.push("wind");
            return cats;
        },
        getLock: function (iso) { return state.vacation.has(iso) ? "vacation" : null; },
        onToggle: function (iso, armedKey) { state.armedType = armedKey; toggleDay(iso); },
        onRange: function (isos, armedKey) {
            state.armedType = armedKey;
            isos.forEach(function (iso) { if (!mapFor(armedKey).has(iso)) addDay(iso); });
        },
        onRender: function (info) {
            state.year = info.year;
            var stationLabel = document.getElementById("weerstation-select-label")?.textContent || "";
            setText("weer-year-value", info.year);
            var stationYearEl = document.getElementById("weer-year-station");
            if (stationYearEl) stationYearEl.textContent = (stationLabel ? stationLabel + " " : "") + info.year + " ·";
            setText("weer-year-rain", info.counts.rain || 0);
            setText("weer-year-wind", info.counts.wind || 0);
            setText("weer-year-total", info.total);
            setText("weer-count-rain-btn", info.counts.rain || 0);
            setText("weer-count-wind-btn", info.counts.wind || 0);
        }
    });
    function renderAll() { cal.render(); }

    // Jaarnavigatie: hertekent enkel uit de al-opgehaalde bundel, geen nieuwe fetch.
    document.getElementById("weer-year-prev")?.addEventListener("click", function () { cal.prev(); });
    document.getElementById("weer-year-next")?.addEventListener("click", function () { cal.next(); });

    var legend = document.getElementById("weer-legend");
    if (legend) window.GlV2YearCal.renderLegend(legend, [
        { text: "14", label: "regendag", bg: "#D7E6EF", fg: "#2E5F7E" },
        { text: "15", label: "winddag", bg: "#F6E9CE", fg: "#8A5F12" },
        { text: "13", label: "beide", bg: "linear-gradient(135deg, #D7E6EF 0 50%, #F6E9CE 50% 100%)" },
        { text: "9", label: "verlofdag", bg: "var(--gl-v2-primary-tint)", fg: "var(--gl-v2-primary)" },
        { text: "17", label: "weekend, telt niet mee", kind: "weekend" }
    ].concat(state.canWrite ? [{ text: "18", label: "hover in de gekozen modus", kind: "hover", fg: "#2E5F7E" }] : []),
        state.canWrite ? "klik zet · nog eens klikken wist · slepen over meerdere dagen zet een reeks" : "");

    function toggleDay(key) {
        var type = state.armedType;
        var map = mapFor(type);
        if (!map.has(key)) { addDay(key); return; }

        var id = map.get(key);
        if (id === true) {
            // Zeldzaam: de dag staat nog met de sentinelwaarde (zie addDay/resolveRealId) omdat de
            // achtergrond-opzoeking nog niet klaar was toen hierop geklikt werd — wacht die stil af
            // (geen zichtbare laadstaat) en verwijder dan met het echte id.
            resolveRealId(type).then(function () {
                var resolved = mapFor(type).get(key);
                if (typeof resolved === "number") deleteDay(key, resolved);
            });
            return;
        }
        deleteDay(key, id);
    }

    function addDay(key) {
        var body = new URLSearchParams();
        body.set("dag", key);
        body.set("weatherstationid", String(state.stationId));
        body.set("type", state.armedType === "wind" ? "1" : "0");
        body.set("__RequestVerificationToken", token);

        var type = state.armedType;

        fetch(config.addUrl, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: body })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.text(); })
            .then(function (text) {
                var id = parseInt(text, 10);
                var hasRealId = Number.isFinite(id) && id !== 0;
                // Optimistisch: de dag telt meteen mee, ongeacht of er al een bruikbaar id meekwam —
                // zelfde onmiddellijke feedback als deleteDay() hieronder.
                mapFor(type).set(key, hasRealId ? id : true);
                renderAll();
                if (!hasRealId) resolveRealId(type, key);
            })
            .catch(function () {
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Opslaan mislukt", body: "Kon de dag niet opslaan." });
            });
    }

    // ── Achtergrond-opzoeking van het echte id voor een dag die (nog) met de sentinelwaarde `true`
    // staat gemarkeerd (zie addDay). AddBadWeatherDay geeft niet altijd een bruikbaar id terug, ook
    // niet wanneer de insert zelf wél lukte — bestaande server-eigenaardigheid, de legacy
    // FullCalendar-weergave had hier al een eigen probeId()-fallback voor. In plaats van daarvoor de
    // hele kalender zichtbaar te herladen (was hier eerder geprobeerd, voelde als een volle
    // paginaherlading aan): dit haalt enkel de bundel op en vult de Map-waarde bij, zonder
    // renderAll() aan te roepen — er verandert visueel niets (de kleur staat door addDay() al goed),
    // dit repareert enkel het id dat later nodig is om de dag weer te kunnen verwijderen. Eén fetch
    // per type tegelijk (pendingResyncs), zodat een korte reeks kliks niet elk hun eigen verzoek
    // starten. ────────────────────────────────────────────────────────────────────────────────────
    var pendingResyncs = new Map(); // "rain" | "wind" -> Promise
    function resolveRealId(type) {
        var existing = pendingResyncs.get(type);
        if (existing) return existing;

        var url = config.bundleUrl + "?weatherstationid=" + state.stationId + "&year=" + state.year;
        var promise = fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
            .then(function (bundle) {
                var list = type === "wind" ? bundle.wind : bundle.rain;
                (list || []).forEach(function (d) {
                    mapFor(type).set(dateKey(d.year, d.month, d.day), d.id);
                });
            })
            .finally(function () { pendingResyncs.delete(type); });

        pendingResyncs.set(type, promise);
        return promise;
    }

    function deleteDay(key, id) {
        var body = new URLSearchParams();
        body.set("id", String(id));
        body.set("__RequestVerificationToken", token);

        fetch(config.deleteUrl, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: body })
            .then(function (r) { return r.text(); })
            .then(function (text) {
                if (text.trim() !== "true") throw new Error("delete failed");
                mapFor(state.armedType).delete(key);
                renderAll();
            })
            .catch(function () {
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Verwijderen mislukt", body: "Kon de dag niet verwijderen." });
            });
    }

    loadBundle();
})();
