// gl-v2 — Instellingen › Automatisatie: client-side paginering voor de runs-tabellen (puntmeldingen, marktdata-status).
// Container: [data-gl-pager][data-pg-size][data-pg-noun]; rijen: [data-pg-row]. Een filter verbergt rijen via het attribuut data-out
// en meldt dat met het event "glpager:refresh" op de container; de pager bepaalt zelf welke rijen op de huidige pagina `hidden` zijn.
(function () {
    "use strict";
    document.querySelectorAll("[data-gl-pager]").forEach(function (box) {
        var size = parseInt(box.dataset.pgSize, 10) || 10;
        var noun = box.dataset.pgNoun || "resultaten";
        var rows = Array.prototype.slice.call(box.querySelectorAll("[data-pg-row]"));
        var page = 1;
        var foot = document.createElement("div");
        foot.className = "gl-v2-auto-pg";
        foot.setAttribute("role", "navigation");
        foot.setAttribute("aria-label", "Paginering");
        box.appendChild(foot);

        function btn(label, p, opts) {
            var b = document.createElement("button");
            b.type = "button"; b.className = "gl-v2-auto-pg-btn" + (opts && opts.active ? " is-active" : "");
            b.textContent = label; b.disabled = !!(opts && opts.disabled);
            if (opts && opts.aria) b.setAttribute("aria-label", opts.aria);
            if (opts && opts.active) b.setAttribute("aria-current", "page");
            b.addEventListener("click", function () { page = p; render(); });
            return b;
        }
        function render() {
            var actief = rows.filter(function (r) { return !r.hasAttribute("data-out"); });
            var pages = Math.max(1, Math.ceil(actief.length / size));
            if (page > pages) page = pages;
            rows.forEach(function (r) { r.hidden = true; });
            actief.slice((page - 1) * size, page * size).forEach(function (r) { r.hidden = false; });
            foot.hidden = actief.length <= size;
            var shown = Math.min(size, actief.length - (page - 1) * size);
            foot.innerHTML = "";
            var info = document.createElement("span");
            info.className = "gl-v2-auto-pg-info";
            info.textContent = (actief.length === 0 ? 0 : shown) + " van " + actief.length + " " + noun;
            var nav = document.createElement("div");
            nav.className = "gl-v2-auto-pg-nav";
            nav.appendChild(btn("‹", page - 1, { disabled: page === 1, aria: "Vorige pagina" }));
            for (var i = 1; i <= pages; i++) {
                if (pages > 7 && i !== 1 && i !== pages && Math.abs(i - page) > 1) {
                    if (i === 2 || i === pages - 1) { var dots = document.createElement("span"); dots.className = "gl-v2-auto-pg-info"; dots.textContent = "…"; nav.appendChild(dots); }
                    continue;
                }
                nav.appendChild(btn(String(i), i, { active: i === page, aria: "Pagina " + i }));
            }
            nav.appendChild(btn("›", page + 1, { disabled: page === pages, aria: "Volgende pagina" }));
            foot.appendChild(info); foot.appendChild(nav);
        }
        box.addEventListener("glpager:refresh", function () { page = 1; render(); });
        render();
    });
})();
