// gl-v2 — rechtenboom (GlV2RightsTree), design-handoff punt 49b/49c. Tekent de functies van de applicatie als een boom met één niveau per functie:
// Geen · Lezen · Bewerken · Verwijderen (elk niveau bevat het vorige). Gebruikt op "Gebruiker · rechten" (rol + afwijkingen, met bron en reset-knop)
// en op "Rollen" (wijzigingen pas actief na Opslaan). Het component bewaart niets: de pagina geeft de niveaus mee en krijgt via onChange/onReset te horen
// wat de gebruiker doet; daarna roept ze update() aan met de nieuwe niveaus.
//
//   var tree = GlV2RightsTree.create(el, {
//       items:  [{ code, naam, ouder, bedrijf }],         // in boomvolgorde (ouder vóór kinderen), zoals de server ze levert
//       levels: { code: 0..3 },                            // huidig effectief niveau
//       base:   { code: 0..3 } | null,                     // niveau uit de rol: verschilt het, dan is het een afwijking (gebruikerspagina)
//       baseLabel: "Projecten",                            // naam van de rol, voor "uit rol Projecten"
//       changed: ["Budget", ...],                          // codes met een niet-opgeslagen wijziging (rollenpagina)
//       readonly: false,
//       onChange: function (codes, level) { },             // klik op een niveau; codes = de functie, of een groep met al haar kinderen
//       onReset:  function (codes) { }                     // terugzetten naar de rol (alleen met `base`)
//   });
//   tree.update({ levels, base, changed });  tree.setFilter({ onlyDeviations, query });  tree.expandAll(bool);
(function () {
    "use strict";

    var LEVELS = ["Geen", "Lezen", "Bewerken", "Verwijderen"];
    var TEXT = ["geen", "lezen", "bewerken", "verwijderen"];

    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }

    function create(root, options) {
        var o = Object.assign({ items: [], levels: {}, base: null, baseLabel: "", changed: [], readonly: false, onChange: null, onReset: null }, options || {});
        var byCode = {}, children = {}, depth = {};
        o.items.forEach(function (it) { byCode[it.code] = it; });
        o.items.forEach(function (it) {
            var p = it.ouder && byCode[it.ouder] ? it.ouder : "";
            (children[p] = children[p] || []).push(it.code);
        });
        function depthOf(code) {
            if (depth[code] != null) return depth[code];
            var it = byCode[code];
            depth[code] = it && it.ouder && byCode[it.ouder] ? depthOf(it.ouder) + 1 : 0;
            return depth[code];
        }
        function descendants(code) {
            var out = [];
            (children[code] || []).forEach(function (c) { out.push(c); out = out.concat(descendants(c)); });
            return out;
        }
        function isGroup(code) { return (children[code] || []).length > 0; }

        var expanded = {};
        var filter = { onlyDeviations: false, query: "" };
        var changedSet = {};

        function setChanged(list) { changedSet = {}; (list || []).forEach(function (c) { changedSet[c] = true; }); }
        setChanged(o.changed);

        function level(code) { return o.levels[code] == null ? 0 : o.levels[code]; }
        function isDev(code) {
            if (changedSet[code]) return true;
            return !!o.base && o.base[code] != null && o.base[code] !== level(code);
        }
        // Een groep toont het gemeenschappelijke niveau van al haar nakomelingen, anders "gemengd" (geen knop actief).
        function groupLevel(code) {
            var d = descendants(code);
            if (!d.length) return level(code);
            var first = level(d[0]);
            for (var i = 1; i < d.length; i++) if (level(d[i]) !== first) return null;
            return first;
        }
        function devCount(code) { return descendants(code).filter(isDev).length + (isDev(code) ? 1 : 0); }

        function visible(code) {
            var q = norm(filter.query);
            if (q || filter.onlyDeviations) {
                var self = matches(code);
                if (self) return true;
                return descendants(code).some(matches);
            }
            return true;
        }
        function matches(code) {
            if (filter.onlyDeviations && !isDev(code)) return false;
            var q = norm(filter.query);
            return !q || norm(byCode[code].naam).indexOf(q) >= 0;
        }
        function ancestorsExpanded(code) {
            var it = byCode[code];
            while (it && it.ouder && byCode[it.ouder]) {
                if (!(expanded[it.ouder] || forceOpen(it.ouder))) return false;
                it = byCode[it.ouder];
            }
            return true;
        }
        function forceOpen(code) { return !!(norm(filter.query) || filter.onlyDeviations); }

        function render() {
            var hasBase = !!o.base;
            var html = '<div class="gl-v2-rt-grid gl-v2-rt-head' + (hasBase ? "" : " no-source") + '"><span>Pagina / functie</span>' + (hasBase ? '<span class="src-head">Bron</span>' : "");
            LEVELS.forEach(function (l) { html += '<span class="c">' + l + "</span>"; });
            html += "</div>";

            var shown = 0;
            o.items.forEach(function (it) {
                if (!visible(it.code) || !ancestorsExpanded(it.code)) return;
                // zonder filter/zoekterm: een rij alleen tonen als alle voorouders open staan
                shown++;
                var group = isGroup(it.code), dev = isDev(it.code);
                var open = !!(expanded[it.code] || forceOpen(it.code));
                var gl = group ? groupLevel(it.code) : level(it.code);
                var groupDev = group ? devCount(it.code) : 0;
                var rowDev = group ? groupDev > 0 : dev;
                var cls = "gl-v2-rt-grid gl-v2-rt-row" + (group ? " is-group" : "") + (rowDev ? (o.base ? " is-override" : " is-changed") : "") + (hasBase ? "" : " no-source");

                html += '<div class="' + cls + '" data-code="' + esc(it.code) + '" role="row"><div class="gl-v2-rt-name" style="--depth:' + depthOf(it.code) + '">';
                html += group
                    ? '<button type="button" class="gl-v2-rt-caret" data-toggle="' + esc(it.code) + '" aria-expanded="' + (open ? "true" : "false") + '" aria-label="' + esc(it.naam) + ' ' + (open ? "inklappen" : "openklappen") + '"><i class="ph ph-caret-down" aria-hidden="true"></i></button>'
                    : '<span class="gl-v2-rt-spacer"></span>';
                html += '<span class="gl-v2-rt-label" title="' + esc(it.naam) + '">' + esc(it.naam) + "</span>";
                if (it.bedrijf) html += '<span class="gl-v2-rt-tag">per bedrijf</span>';
                if (group) html += '<span class="gl-v2-rt-meta">· ' + descendants(it.code).length + " functies</span>";
                if (it.info) html += '<span class="gl-v2-rt-sub">' + esc(it.info) + "</span>";
                html += "</div>";

                if (hasBase) {
                    html += '<div class="gl-v2-rt-source' + (rowDev ? " is-gold" : "") + '">';
                    if (group) {
                        html += gl == null ? '<span>gemengd</span>' : "";
                        if (groupDev > 0) html += '<span>' + groupDev + (groupDev === 1 ? " afwijking" : " afwijkingen") + "</span>";
                        if (groupDev > 0 && !o.readonly) html += '<button type="button" class="gl-v2-rt-reset" data-reset="' + esc(it.code) + '" title="Alles in deze groep terug naar de rol" aria-label="Zet ' + esc(it.naam) + ' terug naar de rol"><i class="ph ph-arrow-counter-clockwise" aria-hidden="true"></i></button>';
                    } else if (dev) {
                        html += '<span>afwijking · rol: ' + TEXT[o.base[it.code]] + "</span>";
                        if (!o.readonly) html += '<button type="button" class="gl-v2-rt-reset" data-reset="' + esc(it.code) + '" title="Terug naar de rol" aria-label="Zet ' + esc(it.naam) + ' terug naar de rol"><i class="ph ph-arrow-counter-clockwise" aria-hidden="true"></i></button>';
                    } else html += "<span>uit rol" + (o.baseLabel ? " " + esc(o.baseLabel) : "") + "</span>";
                    html += "</div>";
                }

                for (var l = 0; l < 4; l++) {
                    var active = gl != null && gl === l, included = gl != null && l > 0 && l < gl;
                    html += '<button type="button" class="gl-v2-rt-lvl' + (active ? " is-active" : included ? " is-included" : "") + '" data-code="' + esc(it.code) + '" data-level="' + l + '"' +
                        (o.readonly ? " disabled" : "") + ' aria-pressed="' + (active ? "true" : "false") + '" title="' + esc(it.naam) + ": " + TEXT[l] + '">' + LEVELS[l] + "</button>";
                }
                html += "</div>";
            });
            if (!shown) html += '<div class="gl-v2-rt-empty">' + (filter.onlyDeviations ? "Geen afwijkingen." : "Geen functie gevonden.") + "</div>";
            root.innerHTML = html;
        }

        root.addEventListener("click", function (e) {
            var tog = e.target.closest("[data-toggle]");
            if (tog) { var c = tog.getAttribute("data-toggle"); expanded[c] = !expanded[c]; render(); var again = root.querySelector('[data-toggle="' + c.replace(/"/g, '\\"') + '"]'); if (again) again.focus(); return; }
            var rs = e.target.closest("[data-reset]");
            if (rs && o.onReset && !o.readonly) {
                var rc = rs.getAttribute("data-reset");
                o.onReset([rc].concat(descendants(rc)).filter(function (c) { return isDev(c); }));
                return;
            }
            var btn = e.target.closest(".gl-v2-rt-lvl");
            if (btn && !o.readonly && o.onChange) {
                var code = btn.getAttribute("data-code"), lv = parseInt(btn.getAttribute("data-level"), 10);
                o.onChange([code].concat(descendants(code)), lv);
            }
        });

        render();

        return {
            update: function (u) {
                if (u.levels) o.levels = u.levels;
                if (u.base !== undefined) o.base = u.base;
                if (u.changed) { o.changed = u.changed; setChanged(u.changed); }
                if (u.baseLabel !== undefined) o.baseLabel = u.baseLabel;
                if (u.readonly !== undefined) o.readonly = u.readonly;
                render();
            },
            setFilter: function (f) { Object.assign(filter, f); render(); },
            expandAll: function (on) { o.items.forEach(function (it) { if (isGroup(it.code)) expanded[it.code] = !!on; }); render(); },
            expandDeviations: function () { o.items.forEach(function (it) { if (isGroup(it.code) && devCount(it.code) > 0) expanded[it.code] = true; }); render(); },
            isDeviation: isDev,
            deviations: function () { return o.items.filter(function (it) { return !isGroup(it.code) && isDev(it.code); }); },
            descendants: descendants,
            levelNames: LEVELS,
            levelText: TEXT
        };
    }

    window.GlV2RightsTree = { create: create, levelNames: LEVELS, levelText: TEXT };
})();
