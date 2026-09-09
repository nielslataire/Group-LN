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

    // ── Autosave-status in de wizardkop ──────────────────────────────────────
    // state: 'saved' | 'saving' | 'error'. Stuurt #autosave-label (bw-autosave):
    // het data-bw-save-attribuut kleurt de stip, de tekst is de boodschap.
    var SAVE_TEXT = {
        saved:  'Alle wijzigingen opgeslagen',
        saving: 'Opslaan…',
        error:  'Niet opgeslagen — probeer opnieuw'
    };
    function setSaveStatus(state, message) {
        var el = document.getElementById('autosave-label');
        if (!el) return;
        el.dataset.bwSave = state;
        el.textContent = message || SAVE_TEXT[state] || '';
    }

    // ── Bevestigingsdialoog (thema-Magnific-popup, zoals projecten/edit) ────
    // BudgetWizard.confirmModal({ title, body, okLabel, okClass }) → Promise<bool>.
    // Valt terug op window.confirm als de popup niet geladen is.
    function confirmModal(opts) {
        opts = opts || {};
        var $ = window.jQuery;
        var el = document.getElementById('bw-confirm-modal');
        if (!el || !$ || !$.magnificPopup) {
            return Promise.resolve(window.confirm(opts.body || 'Ben je zeker?'));
        }
        el.querySelector('[data-bw-confirm-title]').textContent = opts.title || 'Verwijderen';
        el.querySelector('[data-bw-confirm-body]').textContent = opts.body || 'Ben je zeker dat je dit wil verwijderen?';
        var okBtn = el.querySelector('[data-bw-confirm-ok]');
        okBtn.textContent = opts.okLabel || 'Verwijderen';
        okBtn.className = 'btn ' + (opts.okClass || 'btn-danger');

        return new Promise(function (resolve) {
            var confirmed = false;
            function onOk() { confirmed = true; $.magnificPopup.close(); }
            okBtn.addEventListener('click', onOk, { once: true });
            $.magnificPopup.open({
                items: { src: '#bw-confirm-modal' },
                type: 'inline',
                preloader: false,
                callbacks: {
                    close: function () {
                        okBtn.removeEventListener('click', onOk);
                        resolve(confirmed);
                    }
                }
            });
        });
    }

    // ── Stap-navigatie met opslaan + overgang ──────────────────────────────
    // Elke navigatie tussen wizardstappen (stepper, Vorige/Volgende):
    //   1. autosave-status → "Opslaan…"
    //   2. optioneel: POST naar het opslaan-endpoint (form[data-bw-save-url])
    //   3. voortgangslijn onder de tabstrip + huidige inhoud vervaagt omhoog
    //   4. navigeren naar de nieuwe stap
    // Tabelstappen slaan per rij op; daar volstaat de korte statusflits zodat
    // een net-getypte waarde nog wegschrijft vóór het verlaten van de pagina.
    var _navBusy = false;
    var _loaderTimer = null;

    // Dunne merkgroene voortgangslijn onder de vaste tabstrip.
    function _progressBar() {
        var bar = document.querySelector('.bw-nav-progress');
        if (!bar) {
            bar = document.createElement('div');
            bar.className = 'bw-nav-progress';
            bar.setAttribute('aria-hidden', 'true');
            document.body.appendChild(bar);
        }
        return bar;
    }

    // Centrale laad-indicatie: verschijnt pas als de overgang langer duurt
    // dan ~450ms, en blijft dan in lus animeren tot de nieuwe stap laadt.
    function _loader() {
        var el = document.querySelector('.bw-nav-loader');
        if (!el) {
            el = document.createElement('div');
            el.className = 'bw-nav-loader';
            el.setAttribute('role', 'status');
            el.setAttribute('aria-live', 'polite');
            var wall = document.createElement('div');
            wall.className = 'bw-nav-loader__wall';
            wall.setAttribute('aria-hidden', 'true');
            for (var i = 0; i < 8; i++) {
                var brick = document.createElement('span');
                // per rij van links naar rechts metselen, dan de rij erboven
                brick.style.animationDelay = ((i % 4) * 0.12 + Math.floor(i / 4) * 0.5) + 's';
                wall.appendChild(brick);
            }
            var label = document.createElement('span');
            label.className = 'bw-nav-loader__label';
            label.textContent = 'Volgende stap laden…';
            el.appendChild(wall);
            el.appendChild(label);
            document.body.appendChild(el);
        }
        return el;
    }

    function _showLoaderSoon() {
        _clearLoaderTimer();
        _loaderTimer = setTimeout(function () {
            _loader().classList.add('bw-nav-loader--on');
        }, 450);
    }

    function _clearLoaderTimer() {
        if (_loaderTimer) { clearTimeout(_loaderTimer); _loaderTimer = null; }
    }

    function _hideLoader() {
        _clearLoaderTimer();
        var el = document.querySelector('.bw-nav-loader');
        if (el) el.classList.remove('bw-nav-loader--on');
    }

    function _startLeaving() {
        document.documentElement.classList.add('bw-leaving');
        var bar = _progressBar();
        bar.getBoundingClientRect(); // eigen frame forceren
        requestAnimationFrame(function () { bar.classList.add('bw-nav-progress--go'); });
        _showLoaderSoon();
    }

    function _abortLeaving() {
        _navBusy = false;
        document.documentElement.classList.remove('bw-leaving');
        var bar = document.querySelector('.bw-nav-progress');
        if (bar) bar.classList.remove('bw-nav-progress--go');
        _hideLoader();
    }

    async function navigateWithSave(targetUrl) {
        if (_navBusy || !targetUrl) return;
        _navBusy = true;

        var saveForm = document.querySelector('form[data-bw-save-url]');
        var saveUrl = saveForm ? saveForm.getAttribute('data-bw-save-url') : null;

        setSaveStatus('saving');
        _startLeaving();

        if (saveUrl && saveForm) {
            try {
                var resp = await fetch(saveUrl, { method: 'POST', body: new FormData(saveForm) });
                var json;
                try { json = await resp.json(); } catch (e) { json = { success: resp.ok }; }
                if (!json.success) {
                    setSaveStatus('error');
                    _abortLeaving();
                    return;
                }
            } catch (e) {
                setSaveStatus('error');
                _abortLeaving();
                return;
            }
            window.location.href = targetUrl;
        } else {
            // tabelstap: even ademen zodat een debounced rij-save nog afrondt
            setTimeout(function () { window.location.href = targetUrl; }, 220);
        }
    }

    function initStepNav() {
        // alleen op wizardpagina's
        if (!document.querySelector('.bw-tab-bar')) return;
        _progressBar();

        document.querySelectorAll(
            'a.bw-step[href], a.bw-bottom-nav__back[href], a.bw-bottom-nav__next[href]'
        ).forEach(function (link) {
            link.addEventListener('click', function (e) {
                if (e.metaKey || e.ctrlKey || e.shiftKey || e.button === 1) return; // nieuw tabblad met rust laten
                e.preventDefault();
                navigateWithSave(link.href);
            });
        });

        // formulier-submit stappen (Parameters): overgang bij verzenden,
        // de server redirect zelf naar de volgende stap
        document.querySelectorAll('form.gl-project-form, form#bw-params-form').forEach(function (form) {
            form.addEventListener('submit', function () {
                setSaveStatus('saving');
                _startLeaving();
            });
        });

        // terug via bfcache: alles resetten
        window.addEventListener('pageshow', function (e) {
            if (e.persisted) _abortLeaving();
        });
    }

    // ── Auto-init ──────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initCalcToggle('bw-calc-toggle');
        initStepNav();
    });

    // ── Publieke API ──────────────────────────────────────────────────────────
    return {
        fmtNL:                 fmtNL,
        COEFF:                 COEFF,
        calcGered:             calcGered,
        initCalcToggle:        initCalcToggle,
        fillGroupTypeDropdown: fillGroupTypeDropdown,
        fillSubTypeDropdown:   fillSubTypeDropdown,
        updateKpiCards:        updateKpiCards,
        setSaveStatus:         setSaveStatus,
        navigateWithSave:      navigateWithSave,
        beginTransition:       _startLeaving,
        cancelTransition:      _abortLeaving,
        confirmModal:          confirmModal
    };

})();
