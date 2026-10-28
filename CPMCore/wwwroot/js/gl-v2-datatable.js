// gl-v2 — gedeelde DataTables-instellingen voor de lijstpagina's Facturen, Klanten en Leveranciers (consolidatie 26/10/2026).
// language(entity, zeroRecordsHtml): Nederlandse teksten; entity = { singular, plural }; zeroRecordsHtml = de lege-staat-HTML van de pagina.
// De infoCallback ("N van M klanten"), kolomdefinities, sortering en stateSave blijven per pagina (andere kolommen/entiteit).
(function () {
    "use strict";
    function cap(s) { return s.charAt(0).toUpperCase() + s.slice(1); }

    window.GlV2DataTable = {
        language: function (entity, zeroRecordsHtml) {
            var capSing = cap(entity.singular), capPlur = cap(entity.plural);
            return {
                processing: "Bezig met verwerken…",
                search: "Zoeken:",
                lengthMenu: "Toon _MENU_",
                info: capSing + " _START_ tot _END_ van _TOTAL_ " + capPlur,
                infoEmpty: capSing + " 0 tot 0 van 0 " + capPlur,
                infoFiltered: "(gefilterd uit _MAX_ totaal)",
                loadingRecords: "Bezig met laden…",
                zeroRecords: zeroRecordsHtml,
                emptyTable: "Er zijn geen " + entity.plural + " om weer te geven",
                // pagination-pijltjes als kale chevrons i.p.v. de "Previous"/"Next"-tekst
                paginate: { previous: "‹", next: "›" },
                aria: {
                    sortAscending: ": activeer om oplopend te sorteren",
                    sortDescending: ": activeer om aflopend te sorteren"
                }
            };
        }
    };
})();
