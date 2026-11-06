// gl-v2 — tekstveld met suggesties (Views/Shared/GlV2/_SuggestField.cshtml, wwwroot/css/gl-v2/pickers.css). Elke `[data-gl-v2-suggest]` wordt vanzelf bedraad.
// Je typt vrij; de lijst stelt dingen voor (bv. adressen) en een keuze vult het veld met "naam — adres". Er is geen "onbekend": de tekst blijft altijd geldig.
//
//   data-lookup-url   POST { term } → [{ text, sub, tag }]   (250 ms debounce; ook zonder zoekterm, voor "recent")
//
// Toetsen: ↑↓ lopen, Enter kiest (zonder gekozen rij gebeurt er niets bijzonders), Esc sluit. Events: de gewone `input` en `change` van het tekstveld.
// API: window.GlV2Suggest = { init(scope), setEnabled(root, bool) }  — uitgeschakeld = het veld blijft een gewoon tekstveld zonder lijst.
(function () {
    "use strict";

    var openRoot = null;

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }

    function wire(root) {
        if (!root || root.hasAttribute("data-gl-v2-wired")) return;
        var input = root.querySelector('[data-role="input"]'), panel = root.querySelector('[data-role="panel"]'), box = root.querySelector(".gl-v2-field-box");
        if (!input || !panel || !box) return;
        root.setAttribute("data-gl-v2-wired", "1");

        var url = root.getAttribute("data-lookup-url"), items = [], hi = 0, debounce = 0, seq = 0, enabled = true;

        function position() {
            if (window.innerWidth < 768) { panel.style.top = panel.style.bottom = panel.style.width = panel.style.maxHeight = ""; panel.style.left = "8px"; panel.style.right = "8px"; panel.style.bottom = "8px"; panel.style.maxHeight = "60vh"; return; }
            var r = box.getBoundingClientRect();
            panel.style.right = "";
            panel.style.width = r.width + "px";
            panel.style.left = Math.max(8, r.left) + "px";
            var onder = window.innerHeight - r.bottom - 14, boven = r.top - 14;
            if (Math.min(panel.scrollHeight, 280) > onder && boven > onder) { panel.style.top = ""; panel.style.bottom = (window.innerHeight - r.top + 6) + "px"; panel.style.maxHeight = Math.max(140, Math.min(280, boven)) + "px"; }
            else { panel.style.bottom = ""; panel.style.top = (r.bottom + 6) + "px"; panel.style.maxHeight = Math.max(140, Math.min(280, onder)) + "px"; }
        }
        function open() { if (!enabled || !items.length) return; if (openRoot && openRoot !== root && openRoot._glV2Suggest) openRoot._glV2Suggest.close(); openRoot = root; root.classList.add("is-open"); panel.classList.add("is-open"); input.setAttribute("aria-expanded", "true"); position(); }
        function close() { if (openRoot === root) openRoot = null; root.classList.remove("is-open"); panel.classList.remove("is-open"); input.setAttribute("aria-expanded", "false"); window.clearTimeout(debounce); }
        function render() {
            panel.innerHTML = items.map(function (it, k) {
                return '<button type="button" tabindex="-1" role="option" class="gl-v2-suggest-option' + (k === hi ? " is-active" : "") + '" data-index="' + k + '">' +
                    '<i class="ph ph-map-pin gl-v2-suggest-icon" aria-hidden="true"></i><span class="gl-v2-suggest-text"><span class="gl-v2-suggest-name">' + esc(it.text) + "</span>" +
                    (it.sub ? '<span class="gl-v2-suggest-sub">' + esc(it.sub) + "</span>" : "") + "</span>" +
                    (it.tag ? '<span class="gl-v2-suggest-tag">' + esc(it.tag) + "</span>" : "") + "</button>";
            }).join("");
            if (items.length) open(); else close();
        }
        function search(now) {
            window.clearTimeout(debounce);
            if (!enabled || !url) return;
            var my = ++seq;
            debounce = window.setTimeout(function () {
                var body = new URLSearchParams(); body.set("term", input.value.trim());
                fetch(url, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: body.toString(), credentials: "same-origin" })
                    .then(function (r) { return r.ok ? r.json() : []; }).catch(function () { return []; })
                    .then(function (data) { if (my !== seq) return; items = data || []; hi = 0; render(); });
            }, now ? 0 : 250);
        }
        function choose(it) {
            input.value = it.sub ? it.text + " — " + it.sub : it.text;
            input.dispatchEvent(new Event("input", { bubbles: true }));
            input.dispatchEvent(new Event("change", { bubbles: true }));
            close();
        }

        input.addEventListener("focus", function () { search(true); });
        input.addEventListener("input", function () { search(false); });
        input.addEventListener("blur", function () { window.setTimeout(function () { if (document.activeElement !== input) close(); }, 120); });
        panel.addEventListener("mousedown", function (e) { e.preventDefault(); });
        panel.addEventListener("click", function (e) { var o = e.target.closest(".gl-v2-suggest-option"); if (o && items[+o.getAttribute("data-index")]) { choose(items[+o.getAttribute("data-index")]); input.focus(); } });
        input.addEventListener("keydown", function (e) {
            if (!panel.classList.contains("is-open")) return;
            if (e.key === "ArrowDown") { e.preventDefault(); hi = Math.min(items.length - 1, hi + 1); render(); }
            else if (e.key === "ArrowUp") { e.preventDefault(); hi = Math.max(0, hi - 1); render(); }
            else if (e.key === "Enter") { if (items[hi]) { e.preventDefault(); choose(items[hi]); } }
            else if (e.key === "Escape") { e.preventDefault(); e.stopPropagation(); close(); }
        });
        window.addEventListener("resize", function () { if (panel.classList.contains("is-open")) position(); });
        window.addEventListener("scroll", function (e) { if (panel.classList.contains("is-open") && !panel.contains(e.target)) position(); }, true);

        root._glV2Suggest = { close: close, setEnabled: function (flag) { enabled = !!flag; root.classList.toggle("is-off", !enabled); if (!enabled) { items = []; close(); } } };
    }

    function init(scope) { (scope || document).querySelectorAll("[data-gl-v2-suggest]").forEach(wire); }

    document.addEventListener("click", function (e) { if (openRoot && !e.target.closest(".gl-v2-suggest") && openRoot._glV2Suggest) openRoot._glV2Suggest.close(); });

    window.GlV2Suggest = {
        init: init,
        setEnabled: function (root, flag) { if (root && root._glV2Suggest) root._glV2Suggest.setEnabled(flag); }
    };

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", function () { init(document); }); else init(document);
})();
