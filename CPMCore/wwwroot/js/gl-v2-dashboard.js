// gl-v2 dashboard (design-handoff 7b, "Meldingenscherm") — pagina-eigen JS voor Home/Index.
// Meldingenpaneel openen/sluiten (bel-trigger, tablet-popover/mobiel-volledig-scherm), snooze-menu
// (opties + eigen-datum-kalender), AJAX snooze/unsnooze, toast-bevestiging, mobiele filterchips.
// Generieke chrome (contextmenu-mechanica, toasts, modal-knoppen) leeft in gl-v2-shell.js — dit
// bestand vult enkel de meldingenscherm-specifieke inhoud/gedrag in, zelfde scheiding als
// gl-v2-invoices.js voor Facturen.
(function () {
    "use strict";

    var config = window.glV2DashboardConfig || {};
    var snelactiesConfig = window.glV2SnelactiesConfig || { pinnedProjects: [] };

    // ── Actief project (design-handoff 7f) ───────────────────────────────────────────────────────
    // Bewust client-side (localStorage op dit toestel), met terugval op het eerst-vastgezette project
    // (snelactiesConfig.pinnedProjects, server-gerenderd — zie _DashboardProjectleider.cshtml) zodra
    // localStorage leeg is (nieuw toestel/browser, cache gewist). Geen backend-kolom voor "laatst
    // actief project" — een bewuste vereenvoudiging, zie DESIGN.md-draft. Publiceert een event zodat
    // de Snelacties-kaart, de tablet-rail-tegel en de "OP DIT PROJECT"-rijen synchroon meebewegen.
    // Staat vóór de initXxx()-aanroepen hieronder (i.p.v. verderop in dit bestand, waar de functie
    // ooit stond): een `var`-toewijzing wordt niet mee-gehesen zoals een functiedeclaratie — met de
    // toewijzing verderop was GlV2ActiveProject hier nog `undefined` op het moment dat
    // initActiveProject() haar meteen probeerde te gebruiken ("Cannot read properties of undefined
    // (reading 'get')").
    var GlV2ActiveProject = (function () {
        var STORAGE_KEY = "gl-v2-active-project";

        function readStorage() {
            try {
                var raw = window.localStorage.getItem(STORAGE_KEY);
                return raw ? JSON.parse(raw) : null;
            } catch (e) {
                return null; // privévenster/geblokkeerde opslag — negeer, val terug op pin
            }
        }

        var current = readStorage() || (snelactiesConfig.pinnedProjects || [])[0] || null;

        function get() { return current; }
        function set(project) {
            current = project;
            try { window.localStorage.setItem(STORAGE_KEY, JSON.stringify(project)); } catch (e) { /* zie hierboven */ }
            document.dispatchEvent(new CustomEvent("glv2:active-project-changed", { detail: project }));
        }
        return { get: get, set: set };
    })();
    window.GlV2ActiveProject = GlV2ActiveProject;

    initNotificationsBell();
    initFilterChips();
    initSnoozeMenus();
    initUnsnoozeButtons();
    initActiveProject();
    initSearchModals();
    initProjectPickerModal();

    // ── Belletje (topbar) → paneel openen/sluiten ──────────────────────────────────────────────
    // Desktop: het paneel zit al vast in de kolom, deze knop bestaat daar niet eens (gl-v2-shell.css,
    // .gl-v2-topbar-bell enkel <1024px). Tablet: popover, positie via het generieke contextmenu-
    // mechanisme (window.GlV2Menu.reposition — zelfde functie die elk ander gl-v2-menu gebruikt).
    // Mobiel: CSS maakt er een volledig scherm van.
    //
    // reposition() wordt hieronder ALTIJD aangeroepen, nooit voorafgegaan door een eigen breedte-
    // check — reposition() beslist zelf clear-vs-positioneren (gl-v2-shell.js). Eerst hier ook nog
    // op tablet-breedte filteren voordat reposition() aangeroepen werd, liet een venster dat ooit op
    // tablet-breedte al gepositioneerd was (inline top/left) die oude waarden gewoon staan zodra je
    // smaller resizete zonder herlaad — reposition() werd dan nooit meer aangeroepen om ze te wissen,
    // en inline style wint altijd van de mobiele bottom-sheet-CSS. Nu roept resize/open 'm altijd aan
    // en reposition() zelf ruimt op zodra <768px.
    function initNotificationsBell() {
        var trigger = document.getElementById("gl-v2-notifications-bell");
        var panel = document.getElementById("glV2Meldingen");
        if (!trigger || !panel) return;

        function openPanel() {
            panel.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            if (window.innerWidth < 768) document.body.style.overflow = "hidden";
            if (window.GlV2Menu) window.GlV2Menu.reposition(trigger, panel);
        }
        function closePanel() {
            panel.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
            document.body.style.overflow = "";
        }

        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (panel.classList.contains("is-open")) closePanel();
            else openPanel();
        });

        panel.querySelectorAll(".js-gl-v2-mc-close").forEach(function (btn) {
            btn.addEventListener("click", closePanel);
        });

        document.addEventListener("click", function (e) {
            if (window.innerWidth >= 1024) return;
            if (!panel.classList.contains("is-open")) return;
            if (e.target.closest("#glV2Meldingen") || e.target.closest("#gl-v2-notifications-bell")) return;
            closePanel();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) closePanel();
        });
        window.addEventListener("resize", function () {
            if (window.innerWidth >= 1024) { closePanel(); return; }
            if (panel.classList.contains("is-open") && window.GlV2Menu) window.GlV2Menu.reposition(trigger, panel);
        });
    }

    // ── Mobiele filterchips (Alles/Actie/Info) — vervangen de groepskoppen niet echt, filteren
    // gewoon welke .gl-v2-mc-group-blokken zichtbaar zijn (design-handoff 7b). ─────────────────────
    function initFilterChips() {
        var panel = document.getElementById("glV2Meldingen");
        if (!panel) return;
        var chips = panel.querySelectorAll(".gl-v2-mc-chip");
        if (!chips.length) return;
        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                chips.forEach(function (c) {
                    var active = c === chip;
                    c.classList.toggle("is-active", active);
                    c.setAttribute("aria-selected", active ? "true" : "false");
                });
                var filter = chip.getAttribute("data-mc-filter");
                panel.querySelectorAll(".gl-v2-mc-group[data-mc-filter-group]").forEach(function (group) {
                    var groupFilter = group.getAttribute("data-mc-filter-group");
                    group.hidden = filter !== "alle" && filter !== groupFilter;
                });
            });
        });
    }

    // ── Snooze ──────────────────────────────────────────────────────────────────────────────────
    var DUTCH_DAYS_SHORT = ["zo", "ma", "di", "wo", "do", "vr", "za"];
    var DUTCH_MONTHS = ["januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december"];
    var DUTCH_MONTHS_SHORT = ["jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

    function initSnoozeMenus() {
        document.querySelectorAll(".gl-v2-snooze-menu").forEach(initSnoozeMenu);
    }

    function initSnoozeMenu(menu) {
        var trigger = document.querySelector('.js-gl-v2-menu-trigger[aria-controls="' + menu.id + '"]');
        var row = trigger ? trigger.closest(".gl-v2-mc-item") : null;
        if (!trigger || !row) return;

        var optionsPage = menu.querySelector(".gl-v2-snooze-page-options");
        var calendarPage = menu.querySelector(".gl-v2-snooze-page-calendar");
        var monthLabel = menu.querySelector('[data-role="month"]');
        var headEl = menu.querySelector('[data-role="head"]');
        var gridEl = menu.querySelector('[data-role="grid"]');
        var confirmBtn = menu.querySelector('[data-role="confirm"]');
        var selectedTime = "08:00";
        var viewYear, viewMonth, selectedDate = null;

        // Sub-labels ("ma 22 sep · 08:00") kloppen enkel op het moment dat je het menu OPENT, niet op
        // paginalaad-moment — een pagina die uren openstaat zou anders een verkeerde "morgenochtend"
        // tonen. Daarom herberekend bij elke klik op de trigger, niet eenmalig bij init.
        trigger.addEventListener("click", refreshOptionLabels);

        function refreshOptionLabels() {
            menu.querySelectorAll(".js-gl-v2-snooze-option").forEach(function (btn) {
                var date = computeOffsetDate(btn.getAttribute("data-snooze-offset"));
                var sub = btn.querySelector('[data-role="sub"]');
                if (sub) sub.textContent = formatShortDate(date) + " · " + formatTime(date);
            });
        }

        menu.querySelectorAll(".js-gl-v2-snooze-option").forEach(function (btn) {
            btn.addEventListener("click", function () {
                submitSnooze(computeOffsetDate(btn.getAttribute("data-snooze-offset")));
            });
        });

        var customBtn = menu.querySelector(".js-gl-v2-snooze-custom");
        if (customBtn) {
            customBtn.addEventListener("click", function () {
                optionsPage.classList.remove("is-active");
                calendarPage.classList.add("is-active");
                var now = new Date();
                viewYear = now.getFullYear();
                viewMonth = now.getMonth();
                selectedDate = null;
                renderCalendar();
                updateConfirmLabel();
                if (window.GlV2Menu) window.GlV2Menu.reposition(trigger, menu);
            });
        }

        var backBtn = menu.querySelector(".js-gl-v2-snooze-back");
        if (backBtn) {
            backBtn.addEventListener("click", function () {
                calendarPage.classList.remove("is-active");
                optionsPage.classList.add("is-active");
                if (window.GlV2Menu) window.GlV2Menu.reposition(trigger, menu);
            });
        }

        var prevBtn = menu.querySelector(".js-gl-v2-snooze-cal-prev");
        if (prevBtn) {
            prevBtn.addEventListener("click", function () {
                viewMonth--; if (viewMonth < 0) { viewMonth = 11; viewYear--; }
                renderCalendar();
            });
        }
        var nextBtn = menu.querySelector(".js-gl-v2-snooze-cal-next");
        if (nextBtn) {
            nextBtn.addEventListener("click", function () {
                viewMonth++; if (viewMonth > 11) { viewMonth = 0; viewYear++; }
                renderCalendar();
            });
        }

        menu.querySelectorAll(".gl-v2-snooze-time-chip").forEach(function (chip) {
            chip.addEventListener("click", function () {
                menu.querySelectorAll(".gl-v2-snooze-time-chip").forEach(function (c) { c.classList.toggle("is-active", c === chip); });
                selectedTime = chip.getAttribute("data-snooze-time");
            });
        });

        if (confirmBtn) {
            confirmBtn.addEventListener("click", function () {
                if (!selectedDate) return;
                var parts = selectedTime.split(":");
                submitSnooze(new Date(selectedDate.getFullYear(), selectedDate.getMonth(), selectedDate.getDate(), parseInt(parts[0], 10), parseInt(parts[1], 10), 0, 0));
            });
        }

        function renderCalendar() {
            monthLabel.textContent = DUTCH_MONTHS[viewMonth] + " " + viewYear;

            headEl.innerHTML = "";
            // Maandag-eerste week (nl-BE-conventie, zelfde als design-handoff calHead M/D/W/D/V/Z/Z).
            [1, 2, 3, 4, 5, 6, 0].forEach(function (dayIdx) {
                var span = document.createElement("span");
                span.textContent = DUTCH_DAYS_SHORT[dayIdx].charAt(0).toUpperCase();
                headEl.appendChild(span);
            });

            gridEl.innerHTML = "";
            var firstOfMonth = new Date(viewYear, viewMonth, 1);
            var firstWeekday = (firstOfMonth.getDay() + 6) % 7; // 0=maandag
            var daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
            var today = new Date();
            today.setHours(0, 0, 0, 0);
            var totalCells = Math.ceil((firstWeekday + daysInMonth) / 7) * 7;

            for (var i = 0; i < totalCells; i++) {
                var dayNum = i - firstWeekday + 1;
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-snooze-cal-day";
                if (dayNum < 1 || dayNum > daysInMonth) {
                    btn.disabled = true;
                    btn.setAttribute("aria-hidden", "true");
                } else {
                    (function (cellDate, dayLabel) {
                        btn.textContent = dayLabel;
                        if (cellDate.getTime() < today.getTime()) {
                            btn.disabled = true;
                        } else {
                            if (cellDate.getTime() === today.getTime()) btn.classList.add("is-today");
                            if (selectedDate && cellDate.getTime() === selectedDate.getTime()) btn.classList.add("is-selected");
                            btn.addEventListener("click", function () {
                                selectedDate = cellDate;
                                renderCalendar();
                                updateConfirmLabel();
                            });
                        }
                    })(new Date(viewYear, viewMonth, dayNum), String(dayNum));
                }
                gridEl.appendChild(btn);
            }
        }

        function updateConfirmLabel() {
            if (!confirmBtn) return;
            confirmBtn.textContent = selectedDate
                ? "Snooze tot " + selectedDate.getDate() + " " + DUTCH_MONTHS_SHORT[selectedDate.getMonth()]
                : "Snooze";
        }

        function submitSnooze(date) {
            ajaxSnooze(row, date, function (result) {
                if (window.GlV2Menu) window.GlV2Menu.closeAll();
                handleSnoozeSuccess(row, result);
            });
        }
    }

    function computeOffsetDate(offset) {
        var now = new Date();
        if (offset === "3-days") return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 3, 8, 0, 0, 0);
        if (offset === "next-week") return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 7, 8, 0, 0, 0);
        // "tomorrow-morning" en onbekende offsets vallen terug op morgenvroeg.
        return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 8, 0, 0, 0);
    }

    function formatShortDate(date) {
        return DUTCH_DAYS_SHORT[date.getDay()] + " " + date.getDate() + " " + DUTCH_MONTHS_SHORT[date.getMonth()];
    }
    function formatTime(date) {
        return String(date.getHours()).padStart(2, "0") + ":" + String(date.getMinutes()).padStart(2, "0");
    }

    function getAntiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function ajaxSnooze(row, date, onSuccess) {
        var body = new URLSearchParams();
        body.set("projectId", row.getAttribute("data-project-id") || "0");
        body.set("category", row.getAttribute("data-category") || "");
        body.set("tekst", row.getAttribute("data-text") || "");
        body.set("until", date.toISOString());
        var token = getAntiForgeryToken();
        if (token) body.set("__RequestVerificationToken", token);

        fetch(config.snoozeUrl, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" },
            body: body.toString(),
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (result) {
                if (result && result.success) { onSuccess(result); return; }
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Snoozen mislukt", body: "Probeer opnieuw." });
            })
            .catch(function () {
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Snoozen mislukt", body: "Geen verbinding." });
            });
    }

    function ajaxUnsnooze(meldingKey, onSuccess) {
        var body = new URLSearchParams();
        body.set("meldingKey", meldingKey);
        var token = getAntiForgeryToken();
        if (token) body.set("__RequestVerificationToken", token);

        fetch(config.unsnoozeUrl, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" },
            body: body.toString(),
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (result) { if (result && result.success && onSuccess) onSuccess(); });
    }

    // Rij meteen uit de lijst halen (geen page reload nodig om de melding zelf te laten verdwijnen)
    // + toast met "Ongedaan maken" (design-handoff 7b: "Toast bevestigt de keuze"). De groep-teller
    // wordt hier bewust niet live herberekend (blijft één tik stale tot de volgende paginalaad) —
    // kleine, aanvaarde vereenvoudiging voor deze pas.
    function handleSnoozeSuccess(row, result) {
        row.remove();
        if (!window.GlV2Toast) return;
        window.GlV2Toast.show({
            tone: "info",
            icon: "ph-clock",
            title: result.label || "Gesnoozed",
            body: "Toast bevestigt de keuze.",
            action: "Ongedaan maken",
            onAction: function () {
                ajaxUnsnooze(result.meldingKey, function () { window.location.reload(); });
            }
        });
    }

    // "Nu tonen" op een al-gesnoozede rij (onderaan het paneel, design-handoff 7b) — un-snoozen en
    // herladen, zodat de melding weer in haar echte dringendheidsgroep verschijnt i.p.v. de hele
    // groepsindeling hier client-side te moeten nabouwen.
    function initUnsnoozeButtons() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".js-gl-v2-mc-unsnooze");
            if (!btn) return;
            var row = btn.closest(".gl-v2-mc-item-snoozed");
            var key = row ? row.getAttribute("data-melding-key") : null;
            if (!key) return;
            ajaxUnsnooze(key, function () { window.location.reload(); });
        });
    }

    // Kop van de Snelacties-kaart + de "OP DIT PROJECT"-rijen (URL herschreven per actief project,
    // gedimd/naar-de-kiezer zonder project) + de tablet-rail-tegel (puur tonend, geen klikactie in
    // deze pas — design-handoff 7g, "actief project staat bovenaan als eerste tegel").
    function initActiveProject() {
        var activeTextEl = document.getElementById("gl-v2-sa-active-text");
        var actionRows = Array.prototype.slice.call(document.querySelectorAll(".js-gl-v2-sa-project-action"));
        var railTile = document.getElementById("gl-v2-qa-rail-active");
        if (!activeTextEl && !actionRows.length && !railTile) return;

        function render() {
            var project = GlV2ActiveProject.get();

            if (activeTextEl) {
                activeTextEl.innerHTML = "";
                if (project) {
                    var name = document.createElement("span");
                    name.className = "gl-v2-snelacties-active-name";
                    name.textContent = project.name;
                    activeTextEl.appendChild(name);
                    if (project.loc) {
                        var loc = document.createElement("span");
                        loc.className = "gl-v2-snelacties-active-loc";
                        loc.textContent = project.loc.toUpperCase();
                        activeTextEl.appendChild(loc);
                    }
                } else {
                    var placeholder = document.createElement("span");
                    placeholder.className = "gl-v2-snelacties-active-placeholder";
                    placeholder.textContent = "Kies een project";
                    activeTextEl.appendChild(placeholder);
                }
            }

            actionRows.forEach(function (row) {
                if (project) {
                    row.classList.remove("is-dimmed");
                    row.setAttribute("href", row.getAttribute("data-url-template").replace("{id}", project.id));
                } else {
                    row.classList.add("is-dimmed");
                    row.setAttribute("href", "#");
                }
            });

            if (railTile) {
                railTile.hidden = !project;
                if (project) railTile.querySelector('[data-role="rail-active-name"]').textContent = project.name;
            }
        }

        // Zonder actief project openen de "OP DIT PROJECT"-rijen eerst de kiezer i.p.v. ergens
        // fout/leeg naartoe te navigeren (design-handoff 7f: "klikken erop opent eerst de
        // projectkiezer").
        actionRows.forEach(function (row) {
            row.addEventListener("click", function (e) {
                if (!GlV2ActiveProject.get()) {
                    e.preventDefault();
                    var modalEl = document.getElementById("glV2ProjectPickerModal");
                    if (modalEl && window.bootstrap) bootstrap.Modal.getOrCreateInstance(modalEl).show();
                }
            });
        });

        document.addEventListener("glv2:active-project-changed", render);
        render();
    }

    // ── Zoekmodal (design-handoff 7e) — generiek, hergebruikt voor Klant/Leverancier zoeken ────────
    function initSearchModals() {
        document.querySelectorAll(".gl-v2-modal-search:not(.gl-v2-modal-picker)").forEach(initSearchModal);
    }

    function searchInitials(text) {
        var parts = (text || "").trim().split(/\s+/).filter(Boolean);
        if (!parts.length) return "?";
        return (parts[0].charAt(0) + (parts.length > 1 ? parts[parts.length - 1].charAt(0) : "")).toUpperCase();
    }

    function initSearchModal(modal) {
        var lookupUrl = modal.getAttribute("data-lookup-url");
        var detailTemplate = modal.getAttribute("data-detail-url-template");
        var input = modal.querySelector(".gl-v2-modal-search-input");
        var clearBtn = modal.querySelector(".gl-v2-modal-search-clear");
        var searchBtn = modal.querySelector(".gl-v2-modal-search-btn");
        var emptyEl = modal.querySelector('[data-role="empty"]');
        var loadingEl = modal.querySelector('[data-role="loading"]');
        var noneEl = modal.querySelector('[data-role="none"]');
        var noneTitleEl = modal.querySelector('[data-role="none-title"]');
        var noneSubEl = modal.querySelector('[data-role="none-sub"]');
        var noneCreateEl = modal.querySelector('[data-role="none-create"]');
        var resultsEl = modal.querySelector('[data-role="results"]');
        var resultsLabelEl = modal.querySelector('[data-role="results-label"]');
        var resultsListEl = modal.querySelector('[data-role="results-list"]');
        var footerEl = modal.querySelector('[data-role="footer"]');
        if (!lookupUrl || !input) return;

        function showState(state) {
            if (emptyEl) emptyEl.hidden = state !== "empty";
            if (loadingEl) loadingEl.hidden = state !== "loading";
            if (noneEl) noneEl.hidden = state !== "none";
            if (resultsEl) resultsEl.hidden = state !== "results";
            if (footerEl) footerEl.hidden = state !== "results";
        }

        function doSearch() {
            var q = input.value.trim();
            if (!q) return;
            showState("loading");
            fetch(lookupUrl + "?term=" + encodeURIComponent(q) + "&take=15", { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    var items = (data && data.results) || data || [];
                    if (!items.length) {
                        if (noneTitleEl) noneTitleEl.textContent = "Niets gevonden";
                        if (noneSubEl) noneSubEl.textContent = "Controleer de schrijfwijze.";
                        if (noneCreateEl) noneCreateEl.hidden = false;
                        showState("none");
                        return;
                    }
                    if (resultsLabelEl) resultsLabelEl.textContent = items.length + (items.length === 1 ? " resultaat" : " resultaten");
                    resultsListEl.innerHTML = "";
                    items.forEach(function (item) {
                        var row = document.createElement("a");
                        row.className = "gl-v2-modal-search-row";
                        row.href = detailTemplate.replace("{id}", item.id);
                        row.innerHTML =
                            '<span class="gl-v2-modal-search-row-avatar">' + searchInitials(item.text) + "</span>" +
                            '<span class="gl-v2-modal-search-row-name"></span>' +
                            '<i class="ph ph-caret-right gl-v2-modal-search-row-chevron" aria-hidden="true"></i>';
                        row.querySelector(".gl-v2-modal-search-row-name").textContent = item.text;
                        resultsListEl.appendChild(row);
                    });
                    showState("results");
                })
                .catch(function () {
                    if (noneTitleEl) noneTitleEl.textContent = "Zoeken mislukt";
                    if (noneSubEl) noneSubEl.textContent = "Probeer het opnieuw.";
                    if (noneCreateEl) noneCreateEl.hidden = true;
                    showState("none");
                });
        }

        if (searchBtn) searchBtn.addEventListener("click", doSearch);
        input.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); doSearch(); } });
        input.addEventListener("input", function () { if (clearBtn) clearBtn.hidden = !input.value; });
        if (clearBtn) {
            clearBtn.addEventListener("click", function () {
                input.value = "";
                clearBtn.hidden = true;
                showState("empty");
                input.focus();
            });
        }
        modal.addEventListener("shown.bs.modal", function () { input.focus(); });
        modal.addEventListener("hidden.bs.modal", function () {
            input.value = "";
            if (clearBtn) clearBtn.hidden = true;
            showState("empty");
        });
    }

    // ── Projectkiezer-modal (design-handoff 7f, "PROJECTKIEZER (POPOVER)") ──────────────────────────
    // RECENT (client-filter, zelfde patroon als het legacy #gl-punt-sheet) + aanvullende AJAX via
    // QuickSearch + "Dit project vastzetten" (PinProject, ongewijzigd endpoint) op het project dat op
    // dat moment actief staat. Gedeeld door #mw-add-project, de mobiele balk se "Vastzetten"-tegel en
    // de Snelacties-kaart se "ACTIEF PROJECT"-knop — allemaal data-bs-toggle op dezelfde modal-id.
    function initProjectPickerModal() {
        var modal = document.getElementById("glV2ProjectPickerModal");
        if (!modal) return;

        var quickSearchUrl = modal.getAttribute("data-quicksearch-url");
        var pinUrl = modal.getAttribute("data-pin-url");
        var input = document.getElementById("gl-v2-picker-input");
        var recentLabel = document.getElementById("gl-v2-picker-recent-label");
        var recentItems = Array.prototype.slice.call(modal.querySelectorAll(".js-gl-v2-picker-recent"));
        var extraLabel = document.getElementById("gl-v2-picker-extra-label");
        var extraList = document.getElementById("gl-v2-picker-extra-list");
        var pinBtn = document.getElementById("gl-v2-picker-pin-btn");
        var searchTimer = null;

        function selectProject(id, name, loc) {
            GlV2ActiveProject.set({ id: id, name: name, loc: loc || "" });
            bootstrap.Modal.getOrCreateInstance(modal).hide();
        }

        recentItems.forEach(function (item) {
            item.addEventListener("click", function () {
                selectProject(item.getAttribute("data-pid"), item.getAttribute("data-pname"), item.getAttribute("data-ploc"));
            });
        });

        if (input) {
            input.addEventListener("input", function () {
                var q = input.value.trim().toLowerCase();
                clearTimeout(searchTimer);
                extraList.innerHTML = "";
                if (extraLabel) extraLabel.classList.add("d-none");

                var anyVisible = false;
                recentItems.forEach(function (item) {
                    var match = q === "" || (item.getAttribute("data-name") || "").indexOf(q) !== -1;
                    item.hidden = !match;
                    if (match) anyVisible = true;
                });
                if (recentLabel) recentLabel.hidden = q !== "" && !anyVisible;

                if (q.length < 2 || !quickSearchUrl) return;
                searchTimer = setTimeout(function () {
                    fetch(quickSearchUrl + "?q=" + encodeURIComponent(q), { credentials: "same-origin" })
                        .then(function (r) { return r.json(); })
                        .then(function (data) {
                            var ownIds = recentItems.map(function (item) { return item.getAttribute("data-pid"); });
                            var items = (data || []).filter(function (p) { return ownIds.indexOf(String(p.id)) === -1; });
                            extraList.innerHTML = "";
                            if (!items.length) return;
                            if (extraLabel) extraLabel.classList.remove("d-none");
                            items.forEach(function (p) {
                                var row = document.createElement("button");
                                row.type = "button";
                                row.className = "gl-v2-modal-search-row";
                                row.innerHTML =
                                    '<span class="gl-v2-modal-search-row-avatar"><i class="ph ph-map-pin" aria-hidden="true"></i></span>' +
                                    '<span class="gl-v2-modal-search-row-name"></span>' +
                                    '<i class="ph ph-caret-right gl-v2-modal-search-row-chevron" aria-hidden="true"></i>';
                                row.querySelector(".gl-v2-modal-search-row-name").textContent = p.name;
                                row.addEventListener("click", function () { selectProject(p.id, p.name, ""); });
                                extraList.appendChild(row);
                            });
                        });
                }, 300);
            });
        }

        if (pinBtn) {
            pinBtn.addEventListener("click", function () {
                var project = GlV2ActiveProject.get();
                if (!project) return;
                pinBtn.disabled = true;
                var body = new URLSearchParams();
                body.set("projectId", project.id);
                var token = getAntiForgeryToken();
                if (token) body.set("__RequestVerificationToken", token);
                fetch(pinUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" },
                    body: body.toString(),
                    credentials: "same-origin"
                })
                    .then(function (r) { return r.json(); })
                    .then(function (result) {
                        pinBtn.disabled = false;
                        if (result && result.success && window.GlV2Toast) {
                            window.GlV2Toast.show({ tone: "info", icon: "ph-push-pin", title: "Project vastgezet", body: project.name + " staat nu vast op je dashboard." });
                        }
                    })
                    .catch(function () { pinBtn.disabled = false; });
            });
        }

        modal.addEventListener("shown.bs.modal", function () { if (input) { input.value = ""; input.focus(); input.dispatchEvent(new Event("input")); } });
    }
})();
