// gl-v2 layout-pilot — Projecten pagina-specifieke JS. Enkel geladen door
// Views/Projecten/IndexV2.cshtml. Combineert twee bestaande recepten:
// - het zoekveld/status-select/filters-paneel-gedrag van gl-v2-leveranciers.js/gl-v2-klanten.js
//   (initSelect(), chips, mobiele filtersheet) — hier zonder DataTable, gewoon
//   client-side hidden-toggle op de kaarten (net als het inline script dat voorheen in
//   Views/Projecten/Index.cshtml stond);
// - diezelfde Index.cshtml se eigen "Laad meer"-infinite-scroll tegen LoadMoreProjects.
// De kaarten zelf (GlV2/_ProjectCardV2) dragen dezelfde .gl-werf-col/data-status/data-search-hooks
// als de legacy _ProjectWerfCard, dus die twee stukken werken hier ongewijzigd tegen.
(function () {
    "use strict";

    var grid = document.getElementById("project-grid");
    if (!grid) return;

    var searchInput = document.getElementById("search-term");
    var searchClear = document.getElementById("search-term-clear");
    var liveStatus = document.getElementById("project-filter-status");
    var emptyAll = document.getElementById("projecten-empty-all");
    var emptyFiltered = document.getElementById("projecten-empty-filtered");
    var emptyQuery = document.getElementById("projecten-empty-query");
    var clearFilterBtn = document.getElementById("projecten-clear-filter");
    var loadMoreContainer = document.getElementById("load-more-container");
    var loadMoreBtn = document.getElementById("load-more-projects");
    var loader = document.getElementById("projecten-loader");
    var loadedCountEl = document.getElementById("loaded-count");
    var totalCountEl = document.getElementById("total-count");

    var totalCount = parseInt(grid.dataset.totalCount, 10) || 0;
    var loadedCount = parseInt(grid.dataset.loadedCount, 10) || 0;
    var batchSize = parseInt(grid.dataset.batchSize, 10) || 0;
    var loadUrl = loadMoreBtn ? loadMoreBtn.dataset.url : null;

    var query = "";
    var selectedStatus = document.getElementById("status-select-value")?.value || "";
    var isLoading = false;
    var exhausted = !(batchSize > 0 && loadedCount < totalCount);
    var failed = false;

    function cards() {
        return Array.prototype.slice.call(grid.querySelectorAll(".gl-werf-col"));
    }

    function filtering() {
        return query.trim() !== "" || selectedStatus !== "";
    }

    // ── Filteren (zoekterm + status) op de reeds geladen kaarten ────────────────────────────────
    function applyFilter() {
        var q = query.trim().toLowerCase();
        var shown = 0;
        cards().forEach(function (card) {
            var okStatus = selectedStatus === "" || card.dataset.status === selectedStatus;
            var okText = q === "" || (card.dataset.search || "").indexOf(q) !== -1;
            var visible = okStatus && okText;
            card.hidden = !visible;
            if (visible) shown++;
        });

        var isFiltering = filtering();
        var noneAtAll = cards().length === 0;

        emptyAll.classList.toggle("d-none", !noneAtAll);
        emptyFiltered.classList.toggle("d-none", !(isFiltering && shown === 0 && !noneAtAll));
        if (isFiltering && shown === 0 && !noneAtAll) {
            emptyQuery.textContent = q !== "" ? '"' + query.trim() + '"' : "deze status";
        }

        // Bij client-side filteren slaat de teller niet meer op wat je ziet — verberg het
        // laadblok tot het filter weer los is.
        if (loadMoreContainer) {
            loadMoreContainer.classList.toggle("d-none", isFiltering || exhausted);
        }

        if (liveStatus) {
            liveStatus.textContent = isFiltering
                ? shown + (shown === 1 ? " project" : " projecten") + " gevonden."
                : "";
        }

        updateFiltersShowButton();
        updateFilterChips();
    }

    // ── Filterbadges — één badge voor de gekozen status (geen badge zolang "Alle projecten"
    // actief is, dat is de standaardselectie). "Wissen" reset alles ineens. ─────────────────────
    function statusOptionLabel(value) {
        var opt = document.querySelector('#status-select-panel .gl-v2-select-option[data-value="' + value + '"]');
        return opt ? opt.getAttribute("data-label") : value;
    }

    function updateFilterChips() {
        var chips = [];
        if (selectedStatus !== "") chips.push({ type: "status", label: statusOptionLabel(selectedStatus) });

        var chipList = document.getElementById("filters-chip-list");
        if (chipList) {
            chipList.innerHTML = "";
            chips.forEach(function (chip) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-filters-chip";
                btn.setAttribute("data-filter-type", chip.type);
                var span = document.createElement("span");
                span.textContent = chip.label;
                btn.appendChild(span);
                var icon = document.createElement("i");
                icon.className = "ph ph-x";
                icon.setAttribute("aria-hidden", "true");
                btn.appendChild(icon);
                chipList.appendChild(btn);
            });
        }

        [document.getElementById("filters-toggle-count"), document.getElementById("mobile-filters-toggle-count")]
            .forEach(function (el) {
                if (!el) return;
                el.textContent = chips.length;
                el.hidden = chips.length === 0;
            });
        [document.getElementById("filters-toggle"), document.getElementById("mobile-filters-toggle")]
            .forEach(function (el) { if (el) el.classList.toggle("has-active", chips.length > 0); });
        var clearBtn = document.getElementById("filters-clear");
        if (clearBtn) clearBtn.hidden = chips.length === 0;
        var sheetBadge = document.getElementById("filters-sheet-badge");
        if (sheetBadge) {
            sheetBadge.textContent = chips.length + " actief";
            sheetBadge.hidden = chips.length === 0;
        }
    }

    document.addEventListener("click", function (e) {
        var chip = e.target.closest(".gl-v2-filters-chip");
        if (!chip) return;
        if (chip.getAttribute("data-filter-type") === "status") statusSelectApi.select("");
    });

    function resetFilters() {
        query = "";
        if (searchInput) searchInput.value = "";
        statusSelectApi.select("", false);
        selectedStatus = "";
        applyFilter();
        searchInput && searchInput.focus();
        maybeContinue();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-filters-clear") || e.target === clearFilterBtn) resetFilters();
    });

    // ── Filters-knop — klapt het filterveld-/badges-blok open/dicht. Twee triggers sturen hetzelfde
    // paneel aan: de inline "Filters"-knop in de toolbar en de topbar-filtericoon (<768px). ────────
    var filtersToggle = document.getElementById("filters-toggle");
    var mobileFiltersToggle = document.getElementById("mobile-filters-toggle");
    var filtersPanel = document.getElementById("filters-panel");
    var filtersPanelBackdrop = document.getElementById("filters-panel-backdrop");
    var filtersToggleButtons = [filtersToggle, mobileFiltersToggle].filter(Boolean);

    function setFiltersPanelOpen(isOpen) {
        filtersPanel.hidden = !isOpen;
        if (filtersPanelBackdrop) filtersPanelBackdrop.hidden = !isOpen;
        filtersToggleButtons.forEach(function (btn) {
            btn.classList.toggle("is-open", isOpen);
            btn.setAttribute("aria-expanded", isOpen.toString());
        });
    }

    if (filtersPanel && filtersToggleButtons.length) {
        filtersToggleButtons.forEach(function (btn) {
            btn.addEventListener("click", function () { setFiltersPanelOpen(filtersPanel.hidden); });
        });
        if (filtersPanelBackdrop) filtersPanelBackdrop.addEventListener("click", function () { setFiltersPanelOpen(false); });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !filtersPanel.hidden) setFiltersPanelOpen(false);
        });
    }

    // ── "Toon N projecten" (mobiele filtersheet) — telt de op dit moment zichtbare kaarten. ───────
    var filtersShowCount = document.getElementById("filters-show-count");
    function updateFiltersShowButton() {
        if (filtersShowCount) filtersShowCount.textContent = cards().filter(function (c) { return !c.hidden; }).length;
    }
    var filtersShowBtn = document.getElementById("filters-show-btn");
    if (filtersShowBtn) filtersShowBtn.addEventListener("click", function () { setFiltersPanelOpen(false); });

    // ── Status-select — zelfde BASIS-paneelvariant (optie 4h) als Leveranciers/Klanten. ───────────
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
            var option = panel.querySelector('.gl-v2-select-option[data-value="' + value + '"]') || panel.querySelector('.gl-v2-select-option[data-value=""]');
            if (!option) return;
            var lbl = option.getAttribute("data-label") || "";
            panel.querySelectorAll(".gl-v2-select-option").forEach(function (o) { o.classList.toggle("is-selected", o === option); });
            if (label) label.textContent = lbl;
            trigger.classList.toggle("is-filled", value !== "");
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

    var statusSelectApi = initSelect("status-select", function (value) {
        selectedStatus = value || "";
        applyFilter();
        maybeContinue();
    });

    // ── Zoekveld ─────────────────────────────────────────────────────────────────────────────────
    function debounce(fn, delay) {
        var t = null;
        return function () {
            var ctx = this, args = arguments;
            clearTimeout(t);
            t = setTimeout(function () { fn.apply(ctx, args); }, delay);
        };
    }

    searchInput && searchInput.addEventListener("input", debounce(function () {
        query = searchInput.value || "";
        applyFilter();
        maybeContinue();
    }, 150));

    searchClear && searchClear.addEventListener("click", function () {
        query = "";
        if (searchInput) { searchInput.value = ""; searchInput.focus(); }
        applyFilter();
        maybeContinue();
    });

    // ── "Laad meer" / infinite scroll — zelfde recept als het vorige inline script in
    // Views/Projecten/Index.cshtml, enkel de nieuwe kaarten komen nu via _ProjectGridItemsV2
    // (ProjectenController.LoadMoreProjects herkent UseGlV2Layout zelf). ──────────────────────────
    function canLoad() {
        return !isLoading && !exhausted && !failed && loadUrl && !filtering();
    }

    function loadMore() {
        if (!canLoad()) return;
        isLoading = true;
        if (loader) loader.hidden = false;
        if (loadMoreBtn) { loadMoreBtn.disabled = true; loadMoreBtn.textContent = "Laden…"; }

        fetch(loadUrl + "?skip=" + loadedCount + "&take=" + batchSize, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) {
                if (!r.ok) throw new Error("HTTP " + r.status);
                return r.text();
            })
            .then(function (html) {
                var tpl = document.createElement("template");
                tpl.innerHTML = (html || "").trim();
                var added = tpl.content.querySelectorAll(".gl-werf-col");

                if (!added.length) {
                    exhausted = true;
                } else {
                    var frag = document.createDocumentFragment();
                    Array.prototype.forEach.call(added, function (n) { frag.appendChild(n); });
                    grid.appendChild(frag);
                    loadedCount += added.length;
                    if (loadedCountEl) loadedCountEl.textContent = Math.min(loadedCount, totalCount);
                    if (totalCountEl) totalCountEl.textContent = totalCount;
                    if (loadedCount >= totalCount) exhausted = true;
                }

                isLoading = false;
                if (loader) loader.hidden = true;
                applyFilter();

                if (exhausted) {
                    loadMoreContainer && loadMoreContainer.classList.add("d-none");
                } else if (loadMoreBtn) {
                    loadMoreBtn.disabled = false;
                    loadMoreBtn.textContent = "Laad meer";
                }
                maybeContinue();
            })
            .catch(function () {
                isLoading = false;
                failed = true;
                if (loader) loader.hidden = true;
                if (loadMoreBtn) {
                    loadMoreBtn.disabled = false;
                    loadMoreBtn.textContent = "Opnieuw proberen";
                }
                if (window.GlV2Toast) {
                    window.GlV2Toast.show({ tone: "danger", title: "Laden mislukt", body: "Kon meer projecten niet laden. Probeer opnieuw." });
                }
            });
    }

    function nearBottom(margin) {
        if (!loadMoreContainer) return false;
        var r = loadMoreContainer.getBoundingClientRect();
        return r.top <= (window.innerHeight || document.documentElement.clientHeight) + margin;
    }

    function maybeContinue() {
        if (!canLoad()) return;
        var notScrollable = document.documentElement.scrollHeight <= window.innerHeight + 4;
        if (notScrollable || nearBottom(800)) {
            loadMore();
        }
    }

    loadMoreBtn && loadMoreBtn.addEventListener("click", function () {
        failed = false;
        loadMore();
    });

    var ticking = false;
    function onScroll() {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(function () {
            ticking = false;
            maybeContinue();
        });
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });

    if ("IntersectionObserver" in window && loadMoreContainer) {
        new IntersectionObserver(function (entries) {
            if (entries.some(function (e) { return e.isIntersecting; })) maybeContinue();
        }, { rootMargin: "800px 0px" }).observe(loadMoreContainer);
    }

    applyFilter();
    maybeContinue();
})();
