(function () {
    "use strict";

    var root = document.querySelector(".content-with-menu");
    if (!root) return;

    // DataTable
    if (window.jQuery && jQuery.fn.DataTable) {
        var $t = jQuery("#datatable-mijlpalen");
        if ($t.length) {
            $t.DataTable({
                order: [[3, "asc"]],
                pageLength: 25,
                language: {
                    search: "Zoeken:",
                    lengthMenu: "Toon _MENU_",
                    info: "_START_ tot _END_ van _TOTAL_ mijlpalen",
                    infoEmpty: "0 mijlpalen",
                    infoFiltered: "(gefilterd uit _MAX_)",
                    zeroRecords: "Geen mijlpalen gevonden",
                    emptyTable: "Nog geen mijlpalen",
                    paginate: { first: "Eerste", last: "Laatste", next: "Volgende", previous: "Vorige" }
                }
            });
        }
    }

    var projectId = (location.pathname.match(/\/Projects\/(\d+)\/Traject/i) || [])[1];

    var modalMijlpaalEl = document.getElementById("modalMijlpaal");
    var modalStatusEl = document.getElementById("modalMijlpaalStatus");
    var modalTaakEl = document.getElementById("modalTaakQuickAdd");
    var bsMijlpaal = modalMijlpaalEl && window.bootstrap ? new bootstrap.Modal(modalMijlpaalEl) : null;
    var bsStatus = modalStatusEl && window.bootstrap ? new bootstrap.Modal(modalStatusEl) : null;
    var bsTaak = modalTaakEl && window.bootstrap ? new bootstrap.Modal(modalTaakEl) : null;

    function resetUpsert() {
        if (!modalMijlpaalEl) return;
        modalMijlpaalEl.querySelector("#mp-id").value = "";
        modalMijlpaalEl.querySelector("#mp-title").textContent = "Mijlpaal toevoegen";
        modalMijlpaalEl.querySelectorAll("input[type=text],input[type=date],textarea").forEach(function (i) { i.value = ""; });
        modalMijlpaalEl.querySelector("#mp-volgorde").value = "0";
        modalMijlpaalEl.querySelector("#mp-verplicht").checked = true;
        modalMijlpaalEl.querySelectorAll("select").forEach(function (s) { s.selectedIndex = 0; });
    }

    var btnNew = document.getElementById("btnNieuweMijlpaal");
    if (btnNew) btnNew.addEventListener("click", resetUpsert);

    document.addEventListener("click", function (e) {
        var editBtn = e.target.closest(".js-mijlpaal-edit");
        if (editBtn && projectId) {
            var id = editBtn.getAttribute("data-id");
            fetch("/Projects/" + projectId + "/Traject/Mijlpaal/" + id + "/Data")
                .then(function (r) { return r.ok ? r.json() : Promise.reject(); })
                .then(function (m) {
                    resetUpsert();
                    modalMijlpaalEl.querySelector("#mp-title").textContent = "Mijlpaal bewerken";
                    modalMijlpaalEl.querySelector("#mp-id").value = m.id;
                    modalMijlpaalEl.querySelector("#mp-naam").value = m.naam || "";
                    setVal("#mp-fase", m.projecttrajectFaseId);
                    setVal("#mp-unit", m.unitId);
                    setVal("#mp-type", m.mijlpaalType);
                    setVal("#mp-status", m.status);
                    setVal("#mp-rol", m.verantwoordelijkeRol);
                    modalMijlpaalEl.querySelector("#mp-doeldatum").value = m.doeldatum || "";
                    modalMijlpaalEl.querySelector("#mp-werkelijk").value = m.werkelijkeDatum || "";
                    modalMijlpaalEl.querySelector("#mp-volgorde").value = m.volgorde || 0;
                    modalMijlpaalEl.querySelector("#mp-verplicht").checked = !!m.isVerplicht;
                    modalMijlpaalEl.querySelector("#mp-opmerking").value = m.opmerking || "";
                    if (bsMijlpaal) bsMijlpaal.show();
                })
                .catch(function () { alert("Kon de mijlpaal niet laden."); });
            return;
        }

        var statusBtn = e.target.closest(".js-mijlpaal-status");
        if (statusBtn && modalStatusEl) {
            modalStatusEl.querySelector("#mps-id").value = statusBtn.getAttribute("data-id");
            modalStatusEl.querySelector("#mps-naam").textContent = statusBtn.getAttribute("data-naam") || "";
            setVal("#mps-status", statusBtn.getAttribute("data-status"));
            if (bsStatus) bsStatus.show();
        }
    });

    var btnMpsTaak = document.getElementById("mps-btn-taak");
    if (btnMpsTaak && modalTaakEl) {
        btnMpsTaak.addEventListener("click", function () {
            var mijlpaalId = modalStatusEl.querySelector("#mps-id").value;
            var mijlpaalNaam = modalStatusEl.querySelector("#mps-naam").textContent || "";
            var mijlpaalIdField = modalTaakEl.querySelector("#modalTaakQuickAdd-mijlpaal-id");
            var titelField = modalTaakEl.querySelector("#modalTaakQuickAdd-titel");
            if (mijlpaalIdField) mijlpaalIdField.value = mijlpaalId;
            if (titelField) titelField.value = mijlpaalNaam ? ("Opvolgen: " + mijlpaalNaam) : "";
            if (bsStatus) bsStatus.hide();
            if (bsTaak) bsTaak.show();
        });
    }

    function setVal(sel, val) {
        var el = (modalMijlpaalEl || document).querySelector(sel) || document.querySelector(sel);
        if (el) el.value = (val === null || val === undefined) ? "" : String(val);
    }
})();
