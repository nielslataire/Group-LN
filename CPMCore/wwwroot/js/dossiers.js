(function () {
    "use strict";

    // ===== DataTable + kind-filter pills (Index) =====
    if (window.jQuery && jQuery.fn.DataTable) {
        var $t = jQuery("#datatable-dossiers");
        if ($t.length) {
            var table = $t.DataTable({
                order: [[4, "desc"]],
                pageLength: 25,
                language: {
                    search: "Zoeken:",
                    lengthMenu: "Toon _MENU_",
                    info: "_START_ tot _END_ van _TOTAL_ dossiers",
                    infoEmpty: "0 dossiers",
                    infoFiltered: "(gefilterd uit _MAX_)",
                    zeroRecords: "Geen dossiers gevonden",
                    emptyTable: "Nog geen dossiers",
                    paginate: { first: "Eerste", last: "Laatste", next: "Volgende", previous: "Vorige" }
                }
            });

            jQuery(".js-kind-filter").on("click", function (e) {
                e.preventDefault();
                jQuery(".js-kind-filter").removeClass("active");
                jQuery(this).addClass("active");
                var kind = jQuery(this).data("kind");
                table.column(1).search("").draw(); // reset text search on type col if any
                jQuery.fn.dataTable.ext.search = jQuery.fn.dataTable.ext.search.filter(function (fn) {
                    return fn.__dossierKindFilter !== true;
                });
                if (kind !== "" && kind != null) {
                    var filterFn = function (settings, data, index, rowData, counter) {
                        var row = table.row(index).node();
                        return String(jQuery(row).data("kind")) === String(kind);
                    };
                    filterFn.__dossierKindFilter = true;
                    jQuery.fn.dataTable.ext.search.push(filterFn);
                }
                table.draw();
            });
        }
    }

    // ===== Nuts-modal: netbeheerder select2 =====
    if (window.jQuery && jQuery.fn.select2) {
        jQuery(".js-netbeheerder-select").each(function () {
            var $sel = jQuery(this);
            var url = $sel.data("url");
            var selId = $sel.data("selected-id");
            var selText = $sel.data("selected-text");
            var $modal = $sel.closest(".modal");

            $sel.select2({
                theme: "bootstrap",
                minimumInputLength: 2,
                width: "100%",
                placeholder: "Typ om een netbeheerder te zoeken...",
                allowClear: true,
                language: "nl",
                // Nodig binnen een Bootstrap-modal: zonder dropdownParent wordt de dropdown aan
                // <body> gehangen, buiten de focus-trap van de modal — dan kan je niets typen.
                dropdownParent: $modal.length ? $modal : undefined,
                ajax: {
                    url: url,
                    type: "POST",
                    dataType: "json",
                    delay: 250,
                    data: function (params) { return { term: params.term, activeOnly: true }; },
                    processResults: function (data) {
                        return { results: jQuery.map(data, function (item) { return { id: item.id, text: item.text }; }) };
                    },
                    cache: true
                }
            });

            if (selId) {
                var opt = new Option(selText || ("#" + selId), selId, true, true);
                $sel.append(opt).trigger("change");
            }
        });
    }

    // ===== Nuts-modal: EAN/meternummer voorvullen uit de eenheid =====
    document.querySelectorAll('[id^="modalNuts"]').forEach(function (modal) {
        var template = modal.getAttribute("data-meterdata-url-template");
        var unitSelect = modal.querySelector(".js-nuts-unit");
        var typeSelect = modal.querySelector(".js-nuts-type");
        var eanInput = modal.querySelector(".js-nuts-ean");
        var meterInput = modal.querySelector(".js-nuts-meter");
        if (!template || !unitSelect) return;

        function vulIn() {
            var unitId = unitSelect.value;
            if (!unitId) return;
            var url = template.replace(/\/Units\/\d+\/Meterdata/, "/Units/" + unitId + "/Meterdata");
            fetch(url).then(function (r) { return r.ok ? r.json() : null; }).then(function (data) {
                if (!data) return;
                var type = typeSelect ? parseInt(typeSelect.value, 10) : 0;
                if (eanInput && !eanInput.value) {
                    if (type === 0) eanInput.value = data.eanElek || "";       // Elektriciteit
                    else if (type === 1) eanInput.value = data.eanGas || "";   // Gas
                }
                if (meterInput && !meterInput.value && type === 2) {          // Water
                    meterInput.value = data.watermeter || "";
                }
            }).catch(function () { /* stil negeren — voorvullen is optioneel */ });
        }

        unitSelect.addEventListener("change", vulIn);
        if (typeSelect) typeSelect.addEventListener("change", vulIn);
    });
})();
