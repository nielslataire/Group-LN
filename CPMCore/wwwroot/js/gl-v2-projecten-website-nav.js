// gl-v2 — Projecten/EditV2, tab "SEO & website": navigatie links (springt naar het volgende blok) en het inklappen van
// blokken die met de schuif in "Blokken op de pagina" uitgezet zijn. Een uitgeklapte kaart toont alles; een ingeklapte kaart
// toont enkel de kop (beperkte ruimte) en kan met een klik op de kop toch geopend worden.
(function () {
    "use strict";

    var layout = document.getElementById("webLayout");
    if (!layout) return;

    var navItems = Array.prototype.slice.call(layout.querySelectorAll("[data-web-nav]"));
    var cards = Array.prototype.slice.call(layout.querySelectorAll("[data-web-block]"));
    var navByCard = {};
    navItems.forEach(function (a) { navByCard[a.getAttribute("data-web-nav")] = a; });

    function visibleOf(key) {
        var row = document.querySelector('#webBlocksList .web-block-row[data-key="' + key + '"] .web-block-visible');
        return !row || row.checked;
    }

    function setCollapsed(card, collapsed) {
        card.classList.toggle("is-collapsed", collapsed);
        var header = card.querySelector(".gl-v2-section-card-header");
        if (header) header.setAttribute("aria-expanded", collapsed ? "false" : "true");
    }

    function refresh(card) {
        var key = card.getAttribute("data-web-block");
        var on = visibleOf(key);
        setCollapsed(card, !on);
        var nav = navByCard[card.id];
        if (nav) {
            nav.classList.toggle("is-off", !on);
            var off = nav.querySelector(".web-nav-item-off");
            if (off) off.hidden = on;
        }
    }

    // chevron in de kop + klik op de kop klapt open/dicht
    cards.forEach(function (card) {
        var header = card.querySelector(".gl-v2-section-card-header");
        if (!header) return;
        header.classList.add("web-card-header");
        header.setAttribute("role", "button");
        header.setAttribute("tabindex", "0");
        var chevron = document.createElement("i");
        chevron.className = "ph ph-caret-down web-card-chevron";
        chevron.setAttribute("aria-hidden", "true");
        header.appendChild(chevron);
        var toggle = function () { setCollapsed(card, !card.classList.contains("is-collapsed")); };
        header.addEventListener("click", toggle);
        header.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") { e.preventDefault(); toggle(); }
        });
        refresh(card);
    });

    // de schuif aan/uit zet de kaart meteen open/dicht
    document.addEventListener("change", function (e) {
        if (!e.target.classList || !e.target.classList.contains("web-block-visible")) return;
        var row = e.target.closest(".web-block-row");
        var key = row && row.getAttribute("data-key");
        cards.forEach(function (card) { if (card.getAttribute("data-web-block") === key) refresh(card); });
    });

    // navigatie: klik scrollt naar de kaart; het actieve onderdeel volgt het scrollen
    navItems.forEach(function (a) {
        a.addEventListener("click", function (e) {
            var target = document.getElementById(a.getAttribute("data-web-nav"));
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({ behavior: "smooth", block: "start" });
        });
    });

    function setActive(id) {
        navItems.forEach(function (a) { a.classList.toggle("is-active", a.getAttribute("data-web-nav") === id); });
    }
    setActive(navItems.length ? navItems[0].getAttribute("data-web-nav") : null);

    if ("IntersectionObserver" in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) { if (entry.isIntersecting) setActive(entry.target.id); });
        }, { rootMargin: "-10% 0px -75% 0px", threshold: 0 });
        navItems.forEach(function (a) {
            var card = document.getElementById(a.getAttribute("data-web-nav"));
            if (card) observer.observe(card);
        });
    }
})();
