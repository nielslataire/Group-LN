// gl-v2 — Instellingen/IndexV2 (design-handoff punt 24a). Twee dingen: zoeken (titel+omschrijving+
// chips van elke kaart doorzoekt de hele kaarttekst, groepen zonder match verdwijnen mee, "Geen
// instelling gevonden"-staat toont enkel bij een echte, lege zoekopdracht) en de
// "ONDERDELEN"-navigatie (klik scrollt naar de sectie, IntersectionObserver houdt de actieve link
// bij tijdens scrollen). De kaart-brede klik zelf hoort hier niet bij — dat is
// initClickableRows()/data-detail-url, shell-breed (gl-v2-shell.js), niets pagina-eigens voor nodig.
(function () {
    "use strict";

    var searchInput = document.getElementById("gl-v2-set-search");
    var clearBtn = document.getElementById("gl-v2-set-search-clear");
    var emptyState = document.getElementById("gl-v2-set-empty");
    var emptyQuery = document.getElementById("gl-v2-set-empty-query");
    var groups = Array.prototype.slice.call(document.querySelectorAll("[data-set-group]"));
    var navItems = Array.prototype.slice.call(document.querySelectorAll("[data-set-nav-item]"));

    var navById = {};
    navItems.forEach(function (link) { navById[link.getAttribute("data-target")] = link; });

    function normalize(s) { return (s || "").toLowerCase(); }

    function applySearch() {
        var q = normalize(searchInput ? searchInput.value.trim() : "");
        var anyVisible = false;
        groups.forEach(function (group) {
            var cards = Array.prototype.slice.call(group.querySelectorAll("[data-set-card]"));
            var groupHasMatch = false;
            cards.forEach(function (card) {
                var match = !q || normalize(card.textContent).indexOf(q) !== -1;
                card.classList.toggle("is-search-hidden", !match);
                if (match) { groupHasMatch = true; anyVisible = true; }
            });
            group.classList.toggle("is-search-hidden", !groupHasMatch);

            // Het menu volgt de zoekopdracht: een groep zonder match verdwijnt ook uit "ONDERDELEN" en
            // de teller toont het aantal zichtbare kaarten — anders scrolt een klik naar een verborgen sectie.
            var link = navById[group.id];
            if (link) {
                link.hidden = !groupHasMatch;
                var visible = cards.filter(function (c) { return !c.classList.contains("is-search-hidden"); }).length;
                var count = link.querySelector("[data-set-nav-count]");
                if (count) count.textContent = visible;
            }
        });
        if (emptyState) {
            emptyState.hidden = !q || anyVisible;
            if (emptyQuery && q) emptyQuery.textContent = searchInput.value.trim();
        }
    }

    function clearSearch() {
        searchInput.value = "";
        searchInput.focus();
        applySearch();
    }

    if (searchInput) searchInput.addEventListener("input", applySearch);
    if (clearBtn && searchInput) clearBtn.addEventListener("click", clearSearch);

    // "Zoekterm wissen" in de lege staat — zelfde uitweg als het kruisje in het veld, maar op de plek
    // waar je kijkt wanneer er niets gevonden is.
    var emptyClear = document.getElementById("gl-v2-set-empty-clear");
    if (emptyClear && searchInput) emptyClear.addEventListener("click", clearSearch);

    // "/" springt naar de zoekbalk (zoals in GitHub/Slack) — enkel buiten een invoerveld.
    document.addEventListener("keydown", function (e) {
        if (e.key !== "/" || e.ctrlKey || e.metaKey || e.altKey || !searchInput) return;
        var t = e.target;
        if (t && (t.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName))) return;
        e.preventDefault();
        searchInput.focus();
    });

    // ── "ONDERDELEN"-navigatie: klik scrollt (smooth) naar de sectie, IntersectionObserver houdt de
    //    actieve link bij terwijl je scrollt — zelfde "welke sectie staat nu bovenaan"-benadering als
    //    een gewone in-pagina-anchor-nav, geen scroll-event-throttling zelf nodig. ──────────────────
    navItems.forEach(function (link) {
        link.addEventListener("click", function (e) {
            var targetId = link.getAttribute("data-target");
            var target = targetId && document.getElementById(targetId);
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({ behavior: "smooth", block: "start" });
            history.replaceState(null, "", "#" + targetId);
        });
    });

    function setActive(link) {
        navItems.forEach(function (l) { l.classList.remove("is-active"); });
        if (link) link.classList.add("is-active");
    }

    if (groups.length && "IntersectionObserver" in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var link = navById[entry.target.id];
                if (!link || !entry.isIntersecting) return;
                setActive(link);
            });
        }, { rootMargin: "-20% 0px -70% 0px" });
        groups.forEach(function (g) { observer.observe(g); });

        // Onderaan de pagina haalt een korte laatste groep het observer-venster (20%–30% van boven) nooit:
        // daar activeren we expliciet de laatste zichtbare groep.
        window.addEventListener("scroll", function () {
            if (window.innerHeight + window.scrollY < document.documentElement.scrollHeight - 2) return;
            for (var i = groups.length - 1; i >= 0; i--) {
                if (!groups[i].classList.contains("is-search-hidden")) { setActive(navById[groups[i].id]); return; }
            }
        }, { passive: true });
    } else if (navItems.length) {
        navItems[0].classList.add("is-active");
    }
})();
