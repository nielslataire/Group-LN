(function () {
    "use strict";

    // ===== Alle-dossiers-tabel: DataTable + zoekveld/colvis (zelfde patroon als de
    // Mijlpalen-tabel, traject.index.js) + kind-filter pills =====
    if (window.DataTable && document.getElementById("datatable-dossiers")) {
        var table = new DataTable("#datatable-dossiers", {
            order: [[4, "desc"]],
            pageLength: 25,
            language: {
                info: "_START_ tot _END_ van _TOTAL_ dossiers",
                infoEmpty: "0 dossiers",
                infoFiltered: "(gefilterd uit _MAX_)",
                zeroRecords: "Geen dossiers gevonden",
                emptyTable: "Nog geen dossiers",
                paginate: {
                    first: '<i class="ph ph-caret-double-left" aria-hidden="true"></i><span class="visually-hidden">Eerste</span>',
                    previous: '<i class="ph ph-caret-left" aria-hidden="true"></i><span class="visually-hidden">Vorige</span>',
                    next: '<i class="ph ph-caret-right" aria-hidden="true"></i><span class="visually-hidden">Volgende</span>',
                    last: '<i class="ph ph-caret-double-right" aria-hidden="true"></i><span class="visually-hidden">Laatste</span>'
                }
            },
            layout: {
                topStart: {
                    buttons: [
                        {
                            extend: "colvis",
                            text: '<i class="ph ph-columns me-2"></i><span>Kolommen</span>',
                            titleAttr: "Selecteer kolommen",
                            init: function (api, node) { $(node).removeClass("btn-secondary").addClass("btn btn-default"); }
                        }
                    ]
                }
            }
        });
        var dossSearchWrap = document.querySelector("#datatable-dossiers").closest(".gl-form-shell__panel");
        if (dossSearchWrap) {
            var dtSearch = dossSearchWrap.querySelector(".dt-search");
            if (dtSearch) dtSearch.style.display = "none";
        }
        table.buttons(0, null).containers().appendTo("#doss-colvis-container");
        var dossSearchInput = document.getElementById("doss-search-term");
        if (dossSearchInput) {
            dossSearchInput.addEventListener("keyup", function () { table.search(dossSearchInput.value).draw(); });
        }

        var kindFilterSelect = document.getElementById("doss-kind-filter");
        if (kindFilterSelect) {
            kindFilterSelect.addEventListener("change", function () {
                var kind = kindFilterSelect.value;
                jQuery.fn.dataTable.ext.search = jQuery.fn.dataTable.ext.search.filter(function (fn) {
                    return fn.__dossierKindFilter !== true;
                });
                if (kind !== "" && kind != null) {
                    var filterFn = function (settings, data, index) {
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

    // ===== Nuts-matrix (Nutsaanvragen-tab): mobiele eenheid-select toggelt de juiste kaart =====
    var numSelect = document.getElementById("nuts-um-unit-select");
    if (numSelect) {
        var numCards = document.querySelectorAll(".gl-um-mobile-card[data-unit-id]");
        var toggleNumCard = function () {
            var id = numSelect.value;
            numCards.forEach(function (c) { c.hidden = c.dataset.unitId !== id; });
        };
        numSelect.addEventListener("change", toggleNumCard);
        toggleNumCard();
    }

    // ===== Nuts-matrix: "+" op een lege cel opent de aanmaakmodal, vooringevuld met eenheid+type =====
    document.querySelectorAll(".js-nuts-matrix-add").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var modal = document.getElementById("modalNuts");
            if (!modal) return;
            var form = modal.querySelector("form");
            if (form) form.reset(); // schone lei: de modal is één gedeeld element, hergebruikt over meerdere "+"-klikken
            var unitSelect = modal.querySelector(".js-nuts-unit");
            var typeSelect = modal.querySelector(".js-nuts-type");
            if (unitSelect) {
                unitSelect.value = btn.dataset.unitId || "";
                unitSelect.dispatchEvent(new Event("change"));
            }
            if (typeSelect) {
                typeSelect.value = btn.dataset.nutsType || "0";
                typeSelect.dispatchEvent(new Event("change"));
            }
        });
    });

    // ===== Bulk-aanmaakmodal: "Alles"/"Geen" op de eenhedenlijst + live telling vóór het aanmaken =====
    document.querySelectorAll(".js-nuts-bulk-all, .js-nuts-bulk-none").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var checked = btn.classList.contains("js-nuts-bulk-all");
            var modal = btn.closest(".modal");
            if (!modal) return;
            modal.querySelectorAll(".gl-nuts-bulk-units input[type=checkbox]").forEach(function (cb) { cb.checked = checked; });
            updateBulkCount(modal);
        });
    });

    function updateBulkCount(modal) {
        var countEl = modal.querySelector(".js-nuts-bulk-count");
        if (!countEl) return;
        var n = modal.querySelectorAll(".gl-nuts-bulk-units input[type=checkbox]:checked").length;
        countEl.textContent = n === 0 ? "Geen eenheden geselecteerd"
            : n === 1 ? "Dit maakt 1 dossier aan"
            : "Dit maakt dossiers aan voor " + n + " eenheden";
    }

    document.querySelectorAll(".gl-nuts-bulk-units").forEach(function (list) {
        var modal = list.closest(".modal");
        if (!modal) return;
        list.addEventListener("change", function (e) {
            if (e.target.matches('input[type=checkbox]')) updateBulkCount(modal);
        });
    });

    // ===== Nuts-matrix: bulk-selectie van meerdere lege cellen -> vult de bulk-aanmaakmodal
    // vooraf in i.p.v. de eenheden daar nog eens los te moeten aanvinken (/impeccable critique
    // projects/dossiers/index: de matrix toont al exact welke cellen leeg zijn, "Bulk per eenheid"
    // liet je dat tot nu toe met het blote oog opnieuw opzoeken in een losse checkboxlijst). =====
    (function () {
        var region = document.getElementById("nuts-um-matrix-region");
        var toggleBtn = document.getElementById("nuts-um-select-toggle");
        var bar = document.getElementById("nuts-um-selection-bar");
        var countEl = document.getElementById("nuts-um-selection-count");
        var clearBtn = document.getElementById("nuts-um-selection-clear");
        var bulkBtn = document.getElementById("nuts-um-selection-bulk");
        var modalBulk = document.getElementById("modalNutsBulk");
        if (!region || !toggleBtn || !bar) return;

        var selectedType = null; // string, of null zolang niets aangevinkt is

        function allCheckboxes() {
            return Array.prototype.slice.call(region.querySelectorAll(".js-nuts-cell-select"));
        }
        function checkedBoxes() {
            return allCheckboxes().filter(function (cb) { return cb.checked; });
        }
        function cellOf(cb) {
            return cb.closest("td, li");
        }

        function refresh() {
            var checked = checkedBoxes();
            selectedType = checked.length ? checked[0].dataset.nutsType : null;

            // Bulk aanmaken werkt server-side per één type tegelijk (NutsAansluitingBulkCreateBO)
            // -> zodra één type geselecteerd is, sluiten we de andere kolommen tijdelijk af i.p.v.
            // stil een gemengde selectie te laten ontstaan die de gebruiker niet kan aanmaken.
            allCheckboxes().forEach(function (cb) {
                var locked = selectedType !== null && cb.dataset.nutsType !== selectedType;
                cb.disabled = locked;
                var cell = cellOf(cb);
                if (!cell) return;
                cell.classList.toggle("is-selected", cb.checked);
                cell.classList.toggle("is-type-locked", locked);
            });

            if (checked.length === 0) {
                bar.classList.add("d-none");
                return;
            }
            bar.classList.remove("d-none");
            var typeLabel = checked[0].dataset.nutsTypeLabel || "";
            countEl.textContent = (checked.length === 1 ? "1 geselecteerd" : checked.length + " geselecteerd") + " (" + typeLabel + ")";
        }

        function clearSelection() {
            allCheckboxes().forEach(function (cb) { cb.checked = false; });
            refresh();
        }

        toggleBtn.addEventListener("click", function () {
            var on = region.classList.toggle("is-selecting");
            toggleBtn.querySelector("span").textContent = on ? "Selectie annuleren" : "Meerdere selecteren voor bulk aanmaken";
            toggleBtn.querySelector("i").className = on ? "ph ph-x me-1" : "ph ph-check-square-offset me-1";
            if (!on) clearSelection();
        });

        region.addEventListener("change", function (e) {
            if (e.target.matches(".js-nuts-cell-select")) refresh();
        });

        if (clearBtn) clearBtn.addEventListener("click", clearSelection);

        if (modalBulk && bulkBtn) {
            modalBulk.addEventListener("show.bs.modal", function (e) {
                if (e.relatedTarget !== bulkBtn) return; // enkel voorvullen als ónze knop de modal opende
                var ids = checkedBoxes().map(function (cb) { return cb.value; });
                var typeSelect = modalBulk.querySelector('select[name="NutsType"]');
                if (typeSelect && selectedType !== null) typeSelect.value = selectedType;
                modalBulk.querySelectorAll(".gl-nuts-bulk-units input[type=checkbox]").forEach(function (cb) {
                    cb.checked = ids.indexOf(cb.value) !== -1;
                });
                updateBulkCount(modalBulk);
            });
        }
    })();

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
