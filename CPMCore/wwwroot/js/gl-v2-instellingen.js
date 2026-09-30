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
        });
        if (emptyState) {
            emptyState.hidden = !q || anyVisible;
            if (emptyQuery && q) emptyQuery.textContent = searchInput.value.trim();
        }
    }

    if (searchInput) searchInput.addEventListener("input", applySearch);
    if (clearBtn && searchInput) {
        clearBtn.addEventListener("click", function () {
            searchInput.value = "";
            searchInput.focus();
            applySearch();
        });
    }

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

    if (groups.length && "IntersectionObserver" in window) {
        var byId = {};
        navItems.forEach(function (link) { byId[link.getAttribute("data-target")] = link; });
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var link = byId[entry.target.id];
                if (!link || !entry.isIntersecting) return;
                navItems.forEach(function (l) { l.classList.remove("is-active"); });
                link.classList.add("is-active");
            });
        }, { rootMargin: "-20% 0px -70% 0px" });
        groups.forEach(function (g) { observer.observe(g); });
    } else if (navItems.length) {
        navItems[0].classList.add("is-active");
    }
})();
