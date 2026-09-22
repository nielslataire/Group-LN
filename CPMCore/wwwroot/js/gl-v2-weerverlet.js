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

    var DOW_LABELS = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
    var MONTH_NAMES = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var ARMED_COLOR = { rain: "#2E5F7E", wind: "#8A5F12" };

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

    function setArmedColor() {
        monthsEl.style.setProperty("--weer-armed-color", ARMED_COLOR[state.armedType]);
        monthsEl.dataset.armedType = state.armedType;
    }

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
            setArmedColor();
        });
    });
    setArmedColor();

    // ── Jaarnavigatie — hertekent enkel uit de al-opgehaalde bundel, geen nieuwe fetch. ──────────
    document.getElementById("weer-year-prev")?.addEventListener("click", function () { state.year--; renderAll(); });
    document.getElementById("weer-year-next")?.addEventListener("click", function () { state.year++; renderAll(); });

    // ── Bundel ophalen (per stationwissel). ──────────────────────────────────────────────────────
    function loadBundle() {
        monthsEl.setAttribute("aria-busy", "true");
        if (!state.stationId) {
            monthsEl.innerHTML = '<div class="gl-v2-weer-loading">Geen weerstation geselecteerd.</div>';
            return;
        }
        monthsEl.innerHTML = '<div class="gl-v2-weer-loading"><i class="ph ph-circle-notch gl-v2-spin" aria-hidden="true"></i> Kalender laden…</div>';

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
                monthsEl.innerHTML = '<div class="gl-v2-weer-loading">Kon de kalender niet laden.</div>';
            });
    }

    // ── Renderen — 12 maandkaarten volledig uit state opgebouwd (geen server-round-trip per
    // jaarwissel/type-wissel). ───────────────────────────────────────────────────────────────────
    function renderAll() {
        monthsEl.removeAttribute("aria-busy");

        var stationLabel = document.getElementById("weerstation-select-label")?.textContent || "";
        setText("weer-year-value", state.year);
        var stationYearEl = document.getElementById("weer-year-station");
        if (stationYearEl) stationYearEl.textContent = (stationLabel ? stationLabel + " " : "") + state.year + " ·";

        var yearRain = 0, yearWind = 0;
        var frag = document.createDocumentFragment();
        for (var m = 0; m < 12; m++) {
            var counts = renderMonth(frag, m);
            yearRain += counts.rain;
            yearWind += counts.wind;
        }
        monthsEl.innerHTML = "";
        monthsEl.appendChild(frag);

        setText("weer-year-rain", yearRain);
        setText("weer-year-wind", yearWind);
        setText("weer-year-total", yearRain + yearWind);
        setText("weer-count-rain-btn", yearRain);
        setText("weer-count-wind-btn", yearWind);
    }

    function renderMonth(frag, monthIndex) {
        var card = document.createElement("div");
        card.className = "gl-v2-weer-month-card";

        var head = document.createElement("div");
        head.className = "gl-v2-weer-month-head";
        var nameSpan = document.createElement("span");
        nameSpan.className = "gl-v2-weer-month-name";
        nameSpan.textContent = MONTH_NAMES[monthIndex];
        var rainCount = document.createElement("span");
        rainCount.className = "gl-v2-weer-month-count is-rain";
        var windCount = document.createElement("span");
        windCount.className = "gl-v2-weer-month-count is-wind";
        head.appendChild(nameSpan);
        head.appendChild(rainCount);
        head.appendChild(windCount);
        card.appendChild(head);

        var grid = document.createElement("div");
        grid.className = "gl-v2-weer-day-grid";
        DOW_LABELS.forEach(function (lbl) {
            var dow = document.createElement("div");
            dow.className = "gl-v2-weer-dow";
            dow.textContent = lbl;
            grid.appendChild(dow);
        });

        var year = state.year;
        var first = new Date(year, monthIndex, 1);
        var offset = (first.getDay() + 6) % 7; // maandag-first: 0=ma..6=zo
        var daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
        var mRain = 0, mWind = 0;

        for (var i = 0; i < 42; i++) {
            var dayNum = i - offset + 1;
            var cell = document.createElement("div");
            cell.className = "gl-v2-weer-day";

            if (dayNum < 1 || dayNum > daysInMonth) {
                cell.classList.add("is-blank");
                grid.appendChild(cell);
                continue;
            }

            var d = new Date(year, monthIndex, dayNum);
            var isWeekend = d.getDay() === 0 || d.getDay() === 6;
            var key = dateKey(year, monthIndex + 1, dayNum);
            var hasRain = state.rain.has(key);
            var hasWind = state.wind.has(key);
            var hasVacation = state.vacation.has(key);

            cell.textContent = String(dayNum);
            cell.dataset.date = key;

            if (hasVacation) cell.classList.add("is-vacation");
            else if (isWeekend) cell.classList.add("is-weekend");
            else if (hasRain && hasWind) cell.classList.add("is-both");
            else if (hasRain) cell.classList.add("is-rain");
            else if (hasWind) cell.classList.add("is-wind");

            if (!isWeekend && !hasVacation) {
                if (hasRain) mRain++;
                if (hasWind) mWind++;
            }

            grid.appendChild(cell);
        }

        rainCount.textContent = String(mRain);
        windCount.textContent = String(mWind);
        card.appendChild(grid);
        frag.appendChild(card);
        return { rain: mRain, wind: mWind };
    }

    // ── Klik/sleep-interactie (enkel met schrijfrechten, design-handoff 10a se eigen legende: "klik
    // zet · nog eens klikken wist · slepen over meerdere dagen zet een reeks"). Slepen enkel via
    // muis/pen (pointerType "touch" laat de normale paginascroll met rust en valt terug op de
    // gewone klik-toggle hieronder). ─────────────────────────────────────────────────────────────
    function isEligible(cell) {
        return !!cell && !cell.classList.contains("is-blank") && !cell.classList.contains("is-weekend") && !cell.classList.contains("is-vacation");
    }

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

    if (state.canWrite) {
        var suppressNextClick = false;
        var drag = null; // { pointerId, cells: string[] }

        monthsEl.addEventListener("click", function (e) {
            if (suppressNextClick) { suppressNextClick = false; return; }
            var cell = e.target.closest(".gl-v2-weer-day");
            if (!cell || !isEligible(cell)) return;
            toggleDay(cell.dataset.date);
        });

        monthsEl.addEventListener("pointerdown", function (e) {
            if (e.pointerType === "touch") return;
            var cell = e.target.closest(".gl-v2-weer-day");
            if (!cell || !isEligible(cell)) return;
            e.preventDefault();
            drag = { pointerId: e.pointerId, cells: [cell.dataset.date] };
            cell.classList.add("is-drag-preview");
            monthsEl.setPointerCapture(e.pointerId);
        });

        monthsEl.addEventListener("pointermove", function (e) {
            if (!drag || drag.pointerId !== e.pointerId) return;
            var target = document.elementFromPoint(e.clientX, e.clientY);
            var cell = target && target.closest ? target.closest(".gl-v2-weer-day") : null;
            if (!cell || !isEligible(cell)) return;
            var key = cell.dataset.date;
            if (drag.cells.indexOf(key) === -1) {
                drag.cells.push(key);
                cell.classList.add("is-drag-preview");
            }
        });

        function endDrag(e) {
            if (!drag || drag.pointerId !== e.pointerId) return;
            var cells = drag.cells;
            drag = null;
            document.querySelectorAll(".gl-v2-weer-day.is-drag-preview").forEach(function (c) { c.classList.remove("is-drag-preview"); });
            // `e.preventDefault()` in pointerdown (needed so text selection doesn't fight the drag)
            // stops the browser from firing the trailing synthetic "click" for mouse/pen input, so
            // a plain single click never reached the "click" listener above — do the toggle here
            // directly instead of depending on that event. suppressNextClick stays as a defensive
            // guard for browsers where "click" fires anyway.
            suppressNextClick = true;
            if (cells.length > 1) {
                cells.forEach(function (key) {
                    var map = mapFor(state.armedType);
                    if (!map.has(key)) addDay(key);
                });
            } else if (cells.length === 1) {
                toggleDay(cells[0]);
            }
        }
        monthsEl.addEventListener("pointerup", endDrag);
        monthsEl.addEventListener("pointercancel", endDrag);
    }

    loadBundle();
})();
