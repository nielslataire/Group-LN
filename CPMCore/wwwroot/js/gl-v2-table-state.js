// gl-v2 — Tabelstatus onthouden (filters, zoekterm, sortering, pagina). Projectbrede module, geladen in _LayoutV2 naast gl-v2-shell.js.
// Probleem dat dit oplost: na een wijziging (formulier, bulkactie) of een bezoek aan een detail en terug begon de lijst opnieuw met
// de standaardfilters. Contract + gebruik: DESIGN.md, "Tabelstatus onthouden".
//
// Gebruik (na het aanmaken van de DataTable, vóór de eerste draw):
//   var state = GlV2TableState.attach({
//       table: dataTable,                                 // DataTables API-object
//       collect: function () { return { q: ..., f: {...} }; },   // pagina geeft haar filters/zoekterm als gewoon object
//       restore: function (saved) { ...zet filters/zoekterm terug... }
//   });
//   ... eerste draw + paginagrootte instellen ...
//   state.finish();                                        // springt naar de bewaarde pagina en zet het bewaren aan
// Opslag: sessionStorage, per pad (dus per project en per pagina), enkel in deze browsertab. Ontbreekt de opslag, dan werkt de pagina gewoon zonder.
(function () {
    "use strict";
    function attach(opts) {
        var table = opts.table, restoring = true, savedPage = 0;
        var key = "gl-v2-table-state:" + (opts.key || window.location.pathname).toLowerCase();

        function save() {
            if (restoring) return;
            try {
                window.sessionStorage.setItem(key, JSON.stringify({ s: opts.collect ? opts.collect() : {}, o: table.order(), p: table.page() }));
            } catch (e) { /* opslag niet beschikbaar */ }
        }
        try {
            var saved = JSON.parse(window.sessionStorage.getItem(key) || "null");
            if (saved) {
                if (saved.o && saved.o.length) table.order(saved.o);
                savedPage = saved.p || 0;
                if (opts.restore) opts.restore(saved.s || {});
            }
        } catch (e) { /* kapotte opslag negeren */ }

        table.on("draw.dt", save);
        return {
            save: save,
            finish: function () {
                restoring = false;
                if (savedPage > 0 && savedPage < table.page.info().pages) table.page(savedPage).draw("page");
                save();
            }
        };
    }
    window.GlV2TableState = { attach: attach };
})();
