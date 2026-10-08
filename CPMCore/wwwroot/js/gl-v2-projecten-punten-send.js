// gl-v2 — Punten: Goedkeuren & doorsturen (design-handoff 40d). Het filter "Welke punten" en de aannemerselectie bepalen live
// de aantallen, de samenvatting en het mailvoorbeeld; het echte versturen (en goedkeuren) doet de server (SendV2Post).
(function () {
    "use strict";
    var form = document.getElementById("ps-form"), dataEl = document.getElementById("ps-data");
    if (!form || !dataEl) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var groups = JSON.parse(dataEl.textContent || "[]");
    var byKey = {}; groups.forEach(function (g) { byKey[g.key] = g; });
    var scope = $("#ps-scope"), activeKey = null;
    var projectName = form.dataset.projectName || "";

    function included(issue) {
        var s = scope.value;
        return s === "all" || (s === "new" && issue.isNew) || (s === "approved" && !issue.isNew);
    }
    function issuesOf(g) { return g.issues.filter(included); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }

    function render() {
        var points = 0, contractors = 0;
        $$(".gl-v2-ps-row").forEach(function (tr) {
            var g = byKey[tr.dataset.key], list = issuesOf(g), cb = $(".js-ps-group", tr);
            $(".js-ps-total", tr).textContent = list.length;
            $(".js-ps-new", tr).textContent = list.filter(function (i) { return i.isNew; }).length || "—";
            tr.classList.toggle("is-zero", list.length === 0);
            if (cb && cb.checked && list.length) { points += list.length; contractors++; }
            tr.classList.toggle("is-active", tr.dataset.key === activeKey);
        });
        $("#ps-summary").textContent = points + (points === 1 ? " punt" : " punten") + " naar " + contractors + (contractors === 1 ? " aannemer" : " aannemers");
        var send = $("#ps-send");
        if (send) { send.disabled = points === 0; $("span", send).textContent = "Verzenden (" + points + ")"; }
        preview();
    }

    function preview() {
        var box = $("#ps-preview"), title = $("#ps-prev-title");
        var g = activeKey ? byKey[activeKey] : null;
        if (!g) { title.textContent = "Voorbeeld"; box.innerHTML = '<p class="gl-v2-ps-empty">Kies een aannemer om het voorbeeld te zien.</p>'; return; }
        var list = issuesOf(g), n = list.filter(function (i) { return i.isNew; }).length;
        title.textContent = "Voorbeeld voor " + g.name;
        var html = '<div class="gl-v2-ps-subject"><span>Onderwerp: </span>' + esc(projectName) + " — " + list.length + (list.length === 1 ? " werfpunt" : " werfpunten") + (n ? " (" + n + " nieuw)" : "") + "</div>";
        html += "<div>Beste, hieronder de openstaande punten voor " + esc(projectName) + '. Meld per punt "uitgevoerd" met een foto via de knop.</div><ul>';
        list.slice(0, 6).forEach(function (i) { html += "<li><b>" + esc(i.title) + "</b><small>" + esc(i.where) + " · deadline " + esc(i.due || "—") + "</small></li>"; });
        html += "</ul>";
        if (list.length > 6) html += '<div class="gl-v2-ps-hint">+ ' + (list.length - 6) + " andere punten in de PDF en het portaal</div>";
        html += '<span class="gl-v2-ps-portal"><i class="ph ph-arrow-square-out" aria-hidden="true"></i>Punten openen in portaal</span>';
        html += '<div class="gl-v2-ps-hint">Aannemer meldt uitgevoerd → status "Gemeld uitgevoerd" → werfleider controleert en sluit af.</div>';
        box.innerHTML = html;
    }

    $("#ps-body").addEventListener("click", function (e) {
        var tr = e.target.closest(".gl-v2-ps-row"); if (!tr || e.target.closest("input")) return;
        activeKey = tr.dataset.key; render();
    });
    form.addEventListener("change", function (e) {
        if (e.target === scope || e.target.classList.contains("js-ps-group")) render();
        if (e.target.id === "ps-all") { $$(".js-ps-group:not(:disabled)").forEach(function (cb) { cb.checked = e.target.checked; }); render(); }
    });
    var first = $(".js-ps-group:checked");
    if (first) activeKey = first.closest("tr").dataset.key;
    render();
})();
