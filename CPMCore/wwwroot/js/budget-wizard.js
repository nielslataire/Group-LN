/**
 * budget-wizard.js — gedeelde hulpfuncties voor alle stappen van de Budget Wizard.
 * Geladen via @section PageScripts in elke wizard-pagina.
 */

window.BudgetWizard = (function () {

    // ── Getallenpmaak (Belgisch/NL) ───────────────────────────────────────────
    function fmtNL(val, decimals) {
        if (val == null || isNaN(val)) return '0,00';
        return Number(val).toLocaleString('nl-BE', {
            minimumFractionDigits: decimals != null ? decimals : 2,
            maximumFractionDigits: decimals != null ? decimals : 2
        });
    }

    // ── VMSW reductiefactoren ─────────────────────────────────────────────────
    var COEFF = {
        bewoonbareOpp:           1.00,
        tuin:                    0.05,
        terrasPrefab:            1.00,
        terrasGelijkvloers:      0.10,
        dakterras:               0.33,
        garagesParkingsBovenGr:  0.90,
        garBergOndergronds:      0.50,
        bergGelijkvloers:        0.40,
        carports:                0.30,
        doorritGVL:              0.60,
        zolder:                  0.30,
        gemeenschappelijkeDelen: 1.00,
        wegenis:                 0.10
    };

    // ── Berekening gereduceerde oppervlakte ───────────────────────────────────
    function calcGered(data) {
        var sum = 0;
        for (var key in COEFF) {
            if (COEFF.hasOwnProperty(key)) {
                sum += (parseFloat(data[key]) || 0) * COEFF[key];
            }
        }
        return sum;
    }

    // ── Toggle "Toon berekeningen" voor een gegeven checkbox id ───────────────
    function initCalcToggle(checkboxId) {
        var cb = document.getElementById(checkboxId);
        if (!cb) return;
        cb.addEventListener('change', function () {
            document.querySelectorAll('.calc-detail').forEach(function (el) {
                el.style.display = cb.checked ? 'block' : 'none';
            });
        });
    }

    // ── Dropdowns vullen (hoofdtype → subtype) ────────────────────────────────
    function fillGroupTypeDropdown(selectEl, groupTypes, selectedId) {
        groupTypes.forEach(function (g) {
            var opt = document.createElement('option');
            opt.value = g.id;
            opt.text  = g.name;
            if (g.id === selectedId) opt.selected = true;
            selectEl.appendChild(opt);
        });
    }

    function fillSubTypeDropdown(selectEl, allTypes, groupId, selectedId) {
        selectEl.innerHTML = '<option value="">—</option>';
        allTypes
            .filter(function (t) { return t.groupId === groupId; })
            .forEach(function (t) {
                var opt = document.createElement('option');
                opt.value = t.id;
                opt.text  = t.name;
                if (t.id === selectedId) opt.selected = true;
                selectEl.appendChild(opt);
            });
    }

    // ── KPI-kaarten bijwerken ─────────────────────────────────────────────────
    function updateKpiCards(totalen) {
        _setText('kpi-wooneenheden',    totalen.aantalWooneenheden);
        _setText('kpi-parkeerplaatsen', totalen.aantalParkeerplaatsen);
        _setText('kpi-commercieel',     totalen.aantalCommercieel);
        _setText('kpi-totaal',          totalen.aantalTotaal);
        _setText('kpi-totaal-bew',      fmtNL(totalen.totaalBewoonbaar)        + ' m²');
        _setText('kpi-totaal-gered',    fmtNL(totalen.totaalGereduceerd)       + ' m²');
        _setText('kpi-gem-m2',          fmtNL(totalen.gemiddeldeM2PerEenheid)  + ' m²');
        _setText('kpi-totaal-grond',    fmtNL(totalen.totaalGrondopp)          + ' m²');
    }

    function _setText(id, value) {
        var el = document.getElementById(id);
        if (el) el.textContent = value;
    }

    // ── Auto-init wizard calc toggle ─────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initCalcToggle('bw-calc-toggle');
    });

    // ── Publieke API ──────────────────────────────────────────────────────────
    return {
        fmtNL:                 fmtNL,
        COEFF:                 COEFF,
        calcGered:             calcGered,
        initCalcToggle:        initCalcToggle,
        fillGroupTypeDropdown: fillGroupTypeDropdown,
        fillSubTypeDropdown:   fillSubTypeDropdown,
        updateKpiCards:        updateKpiCards
    };

})();
