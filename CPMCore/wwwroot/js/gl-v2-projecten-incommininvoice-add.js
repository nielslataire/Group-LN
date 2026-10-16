// gl-v2 — Projecten/AddIncommingInvoiceV2 (design-handoff 42c). Herbruikt dezelfde AJAX-acties als de
// legacy AddIncommingInvoice.cshtml (GetContractActivities/GetCompanyActivities/GetCompanys) —
// enkel de view-eigen stukken (dropzone/OCR, Kind-segmented, contractinfo-zijbalk, somcontrole,
// WO-kolom-toggle) zijn hier nieuw. window.GlV2IncommingInvoiceAdd.onTypeChange wordt rechtstreeks
// vanuit _IncommingInvoiceDetailRowV2.cshtml se inline onchange aangeroepen (zelfde patroon als de
// legacy rij se eigen globale change(this)-functie).
(function () {
    var cfg = window.glV2AddIncommingInvoiceConfig;
    if (!cfg) return;

    function parseCurrency(text) {
        if (!text) return 0;
        var cleaned = String(text).replace(/[^\d,.-]/g, '').replace(/\./g, '').replace(',', '.');
        return parseFloat(cleaned) || 0;
    }

    // ── Kind-segmented (Vordering/Eindvordering/Creditnota) ─────────────────────────────────────
    var kindButtons = document.querySelectorAll('#gln-ai-kind-segmented .gln-ai-segmented-btn');
    kindButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            kindButtons.forEach(function (b) { b.classList.remove('is-active'); });
            btn.classList.add('is-active');
            document.getElementById('gln-ai-kind').value = btn.getAttribute('data-kind');
        });
    });

    // ── Dropzone / PDF-intake (42e punt 06) ─────────────────────────────────────────────────────
    var dropzone = document.getElementById('gln-ai-dropzone');
    var fileInput = document.getElementById('gln-ai-file-input');
    var statusEl = document.getElementById('gln-ai-dropzone-status');

    function showStatus(text, cls) {
        statusEl.textContent = text;
        statusEl.className = 'gln-ai-dropzone-status' + (cls ? ' ' + cls : '');
        statusEl.hidden = false;
    }

    function extractPdf(file) {
        if (!file) return;
        showStatus('Bezig met inlezen…', '');
        var form = new FormData();
        form.append('file', file);
        var tokenEl = document.querySelector('#gln-ai-form input[name="__RequestVerificationToken"]');
        if (tokenEl) form.append('__RequestVerificationToken', tokenEl.value);
        fetch(cfg.extractPdfUrl, { method: 'POST', body: form })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.success) { showStatus(data.message || 'Kon de factuur niet lezen — vul de velden handmatig in.', 'is-error'); return; }
                if (data.invoiceNumber) document.getElementById('IncommingInvoice_InvoiceExternalId').value = data.invoiceNumber;
                if (data.invoiceDate) {
                    var dateHidden = document.getElementById('IncommingInvoice_IncommingInvoiceDate');
                    var dateText = document.getElementById('IncommingInvoice_IncommingInvoiceDate_text');
                    if (dateHidden) { dateHidden.value = data.invoiceDate; dateHidden.dispatchEvent(new Event('change', { bubbles: true })); }
                    if (dateText) { var d = data.invoiceDate.split('-'); dateText.value = d[2] + '/' + d[1] + '/' + d[0]; }
                }
                if (data.amount) document.getElementById('IncommingInvoice_InvoicePrice').value = Number(data.amount).toFixed(2).replace('.', ',');
                if (data.isCreditNote) { kindButtons[2].click(); }
                showStatus('Leverancier ' + (data.vendorName || '?') + ' — velden ingevuld, controleer ze.', 'is-success');
                recomputeSomcontrole();
            })
            .catch(function () { showStatus('Kon de factuur niet versturen.', 'is-error'); });
    }

    if (dropzone && fileInput) {
        dropzone.querySelector('.gln-ai-dropzone-link').addEventListener('click', function () { fileInput.click(); });
        fileInput.addEventListener('change', function () { extractPdf(fileInput.files[0]); });
        ['dragenter', 'dragover'].forEach(function (ev) { dropzone.addEventListener(ev, function (e) { e.preventDefault(); dropzone.classList.add('is-dragover'); }); });
        ['dragleave', 'drop'].forEach(function (ev) { dropzone.addEventListener(ev, function (e) { e.preventDefault(); dropzone.classList.remove('is-dragover'); }); });
        dropzone.addEventListener('drop', function (e) { var f = e.dataTransfer.files && e.dataTransfer.files[0]; if (f) extractPdf(f); });
    }

    // ── Contractinfo-zijbalk (42e punt 02) ──────────────────────────────────────────────────────
    var contractCard = document.getElementById('gln-ai-contract-card');
    var geenContractNotice = document.getElementById('gln-ai-geencontract-notice');
    var laatsteContractInfo = null;

    function toonGeenContract() {
        if (contractCard) contractCard.hidden = true;
        if (geenContractNotice) geenContractNotice.hidden = false;
        laatsteContractInfo = null;
        recomputeVervaldatum();
        updateContractLiveCijfers();
    }

    function geld(v) { return '€ ' + (v || 0).toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

    // Som van de reeds toegevoegde factuurregels — "deze factuur" in de voortgangsbalk/het saldo
    // hieronder. Zelfde regels als recomputeSomcontrole() telt, hier apart zodat beide functies
    // onafhankelijk kunnen blijven werken (somcontrole vergelijkt tegen het Factuurbedrag-veld, dit
    // hier telt mee op bij wat het contract al eerder gefactureerd kreeg).
    function dezeFactuurBedrag() {
        var som = 0;
        document.querySelectorAll('#DetailRows [data-row]').forEach(function (r) {
            var priceInput = r.querySelector('[data-row-price], .gln-ai-row-price');
            if (priceInput) som += parseCurrency(priceInput.value);
        });
        return som;
    }

    // Contractkaart (design-handoff 42c) — voortgangsbalk + saldo/borg-hint hangen af van de live
    // "deze factuur"-som, dus herrekend bij elke regelwijziging (recomputeSomcontrole) én zodra de
    // contractinfo zelf (opnieuw) geladen wordt — niet enkel bij het laden van de contractinfo.
    function updateContractLiveCijfers() {
        var progress = document.getElementById('gln-ai-contract-progress');
        if (!progress) return;
        if (!laatsteContractInfo) { progress.hidden = true; return; }
        var d = laatsteContractInfo;
        var deze = dezeFactuurBedrag();
        var totaalContract = (d.contractBedrag || 0) + (d.meerwerkBedrag || 0);
        var eerder = d.gefactureerdBedrag || 0;
        var pct = totaalContract > 0 ? (eerder + deze) / totaalContract * 100 : 0;
        progress.hidden = totaalContract <= 0;
        document.getElementById('gln-ai-contract-pct').textContent = pct.toLocaleString('nl-BE', { maximumFractionDigits: 1 }) + ' %';
        document.getElementById('gln-ai-contract-fill-prior').style.width = Math.max(0, Math.min(100, totaalContract > 0 ? eerder / totaalContract * 100 : 0)) + '%';
        document.getElementById('gln-ai-contract-fill-current').style.width = Math.max(0, Math.min(100 - (totaalContract > 0 ? eerder / totaalContract * 100 : 0), totaalContract > 0 ? deze / totaalContract * 100 : 0)) + '%';
        document.getElementById('gln-ai-contract-eerder').textContent = geld(eerder);
        document.getElementById('gln-ai-contract-deze').textContent = geld(deze);
        document.getElementById('gln-ai-contract-saldo').textContent = geld(totaalContract - eerder - deze);

        var borgHint = document.getElementById('gln-ai-contract-waarborg-hint');
        if (borgHint) borgHint.textContent = d.guaranteeType === 'FinancialGuarantee' && d.guaranteePercentage != null
            ? geld(deze * d.guaranteePercentage / 100) + ' op deze factuur' : '';

        // Korting-hint ("€X als betaald vóór DD/MM") is zowel datum- als "deze"-afhankelijk — die
        // berekening zit al in recomputeVervaldatum() (heeft de factuurdatum nodig), hier enkel
        // opnieuw aanroepen zodat 'm ook bij een regelwijziging (niet enkel een datumwijziging) klopt.
        recomputeVervaldatum();
    }

    function laadContractInfo(contractId) {
        if (!contractId || contractId === '0') { toonGeenContract(); return; }
        fetch(cfg.contractInfoUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: 'contractId=' + encodeURIComponent(contractId) + '&invoiceId=' + encodeURIComponent(cfg.invoiceId || 0) })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (!d.success) { toonGeenContract(); return; }
                laatsteContractInfo = d;
                if (geenContractNotice) geenContractNotice.hidden = true;
                if (contractCard) contractCard.hidden = false;
                // Leverancier + contractnaam op één regel, zoals de 42c-kaart — geen "Contract #id"
                // verzinnen als er geen echte naam is (zie GetContractInfoForInvoiceFormV2).
                document.getElementById('gln-ai-contract-name').textContent = d.contractNaam ? (d.companyNaam || '—') + ' · ' + d.contractNaam : (d.companyNaam || '—');
                document.getElementById('gln-ai-contract-bedrag').textContent = geld(d.contractBedrag);
                document.getElementById('gln-ai-contract-meerwerk').textContent = geld(d.meerwerkBedrag);
                document.getElementById('gln-ai-contract-reedsgefactureerd').textContent = geld(d.gefactureerdBedrag);
                document.getElementById('gln-ai-contract-btw').textContent = d.vatPercentage != null ? d.vatPercentage + ' %' : 'niet opgegeven';

                var kortingRow = document.getElementById('gln-ai-contract-row-korting');
                document.getElementById('gln-ai-contract-termijn').textContent = d.paymentTerm != null ? (d.paymentTerm + ' dagen na factuurdatum') : '—';
                if (kortingRow) kortingRow.hidden = !(d.cashDiscount && d.cashDiscountPercentage != null && d.cashDiscountPaymentTerm != null);
                if (d.cashDiscount && d.cashDiscountPercentage != null && d.cashDiscountPaymentTerm != null)
                    document.getElementById('gln-ai-contract-korting').textContent = d.cashDiscountPercentage + ' % binnen ' + d.cashDiscountPaymentTerm + ' dagen';

                var borgRow = document.getElementById('gln-ai-contract-row-borg');
                if (borgRow) borgRow.hidden = d.guaranteeType === 'NoGuarantee' || !d.guaranteeType;
                document.getElementById('gln-ai-contract-waarborg').textContent = d.guaranteeType === 'FinancialGuarantee' && d.guaranteePercentage != null
                    ? d.guaranteePercentage + ' % ingehouden'
                    : (d.guaranteeType === 'BankGuarantee' ? 'Bankwaarborg' : '—');

                var signedBadge = document.getElementById('gln-ai-contract-signed');
                if (signedBadge) signedBadge.hidden = d.contractSigned !== true;

                var openBtn = document.getElementById('gln-ai-contract-open');
                if (openBtn) openBtn.href = cfg.contractOpenUrl.replace('contractid=0', 'contractid=' + contractId);

                recomputeVervaldatum();
                updateContractLiveCijfers();
            })
            .catch(toonGeenContract);
    }

    // ── Vervaldatum/korting (42e punt 03) — berekend, niet opgeslagen ───────────────────────────
    function parseIsoDate(iso) { if (!iso) return null; var p = iso.split('-'); return new Date(Number(p[0]), Number(p[1]) - 1, Number(p[2])); }
    function fmtDate(d) { return d.getDate().toString().padStart(2, '0') + '/' + (d.getMonth() + 1).toString().padStart(2, '0') + '/' + d.getFullYear(); }

    function recomputeVervaldatum() {
        var vervaldatumEl = document.getElementById('gln-ai-vervaldatum');
        var kortingHint = document.getElementById('gln-ai-korting-hint');
        var kaartTermijnHint = document.getElementById('gln-ai-contract-termijn-hint');
        var kaartKortingHint = document.getElementById('gln-ai-contract-korting-hint');
        var dateHidden = document.getElementById('IncommingInvoice_IncommingInvoiceDate');
        var factuurdatum = dateHidden ? parseIsoDate(dateHidden.value) : null;
        if (!factuurdatum || !laatsteContractInfo || laatsteContractInfo.paymentTerm == null) {
            if (vervaldatumEl) vervaldatumEl.value = '';
            if (kortingHint) kortingHint.textContent = '';
            if (kaartTermijnHint) kaartTermijnHint.textContent = '';
            if (kaartKortingHint) kaartKortingHint.textContent = '';
            return;
        }
        var vervaldatum = new Date(factuurdatum.getTime());
        vervaldatum.setDate(vervaldatum.getDate() + Number(laatsteContractInfo.paymentTerm));
        if (vervaldatumEl) vervaldatumEl.value = fmtDate(vervaldatum) + ' (automatisch: + ' + laatsteContractInfo.paymentTerm + ' dagen)';
        if (kaartTermijnHint) kaartTermijnHint.textContent = 'vervalt ' + fmtDate(vervaldatum);
        if (kaartKortingHint) {
            if (laatsteContractInfo.cashDiscount && laatsteContractInfo.cashDiscountPaymentTerm != null && laatsteContractInfo.cashDiscountPercentage != null) {
                var kortingDatumKaart = new Date(factuurdatum.getTime());
                kortingDatumKaart.setDate(kortingDatumKaart.getDate() + Number(laatsteContractInfo.cashDiscountPaymentTerm));
                var kortingBedrag = dezeFactuurBedrag() * laatsteContractInfo.cashDiscountPercentage / 100;
                kaartKortingHint.textContent = geld(kortingBedrag) + ' als betaald vóór ' + fmtDate(kortingDatumKaart);
            } else {
                kaartKortingHint.textContent = '';
            }
        }
        if (kortingHint) {
            if (laatsteContractInfo.cashDiscount && laatsteContractInfo.cashDiscountPaymentTerm != null) {
                var kortingDatum = new Date(factuurdatum.getTime());
                kortingDatum.setDate(kortingDatum.getDate() + Number(laatsteContractInfo.cashDiscountPaymentTerm));
                kortingHint.textContent = 'Korting ' + laatsteContractInfo.cashDiscountPercentage + ' % tot ' + fmtDate(kortingDatum);
            } else {
                kortingHint.textContent = '';
            }
        }
    }
    document.addEventListener('change', function (e) {
        if (e.target && e.target.id === 'IncommingInvoice_IncommingInvoiceDate') recomputeVervaldatum();
    });

    // ── Rij-type-toggle (WO-kolom enkel actief bij Meerwerk klant, value "3") ───────────────────
    window.GlV2IncommingInvoiceAdd = window.GlV2IncommingInvoiceAdd || {};
    window.GlV2IncommingInvoiceAdd.onTypeChange = function (select) {
        var row = select.closest('[data-row]');
        if (!row) return;
        var wo = row.querySelector('[data-row-wo]');
        if (wo) wo.disabled = select.value !== '3';
    };

    // ── Somcontrole (42e punt 05) ────────────────────────────────────────────────────────────────
    function recomputeSomcontrole() {
        var el = document.getElementById('gln-ai-somcontrole');
        var tekstEl = document.getElementById('gln-ai-somcontrole-tekst');
        if (!el) return;
        var rows = document.querySelectorAll('#DetailRows [data-row]');
        if (!rows.length) { el.className = 'gln-ai-somcontrole'; tekstEl.textContent = 'Nog geen regels'; return; }
        var som = 0;
        rows.forEach(function (r) { var priceInput = r.querySelector('[data-row-price], .gln-ai-row-price'); if (priceInput) som += parseCurrency(priceInput.value); });
        var bedragInput = document.getElementById('IncommingInvoice_InvoicePrice');
        var bedrag = bedragInput ? parseCurrency(bedragInput.value) : 0;
        var gelijk = Math.abs(som - bedrag) < 0.01;
        el.className = 'gln-ai-somcontrole ' + (gelijk ? 'is-ok' : 'is-off');
        tekstEl.textContent = rows.length + ' regel' + (rows.length === 1 ? '' : 's') + ' = € ' + som.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
            + (gelijk ? ' · gelijk aan factuurbedrag' : ' · factuurbedrag is € ' + bedrag.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        updateContractLiveCijfers();
    }
    document.addEventListener('input', function (e) {
        if (e.target && (e.target.classList.contains('gln-ai-row-price') || e.target.id === 'IncommingInvoice_InvoicePrice')) recomputeSomcontrole();
    });

    // ── Met/zonder contract (design-handoff 42c: "je kiest in het formulier", geen twee aparte
    // 42a-menu-items meer) + Contract/Leverancier/Activiteiten — gl-v2-native componenten
    // (GlV2/_SelectList → window.GlV2Select, GlV2/_GlV2Combo → window.GlV2Combo), geen select2/jQuery
    // meer nodig voor deze drie velden. ──────────────────────────────────────────────────────────
    var contractField = document.getElementById('gln-ai-contract-field');
    var companyField = document.getElementById('gln-ai-company-field');
    var contractTypeButtons = document.querySelectorAll('#gln-ai-contracttype-segmented .gln-ai-segmented-btn');
    var contractHidden = document.getElementById('lstProjectContracts');
    var companyHidden = document.getElementById('txtCompanyID');
    var activiteitenWrap = document.getElementById('lstActivities_select');
    var activiteitenTrigger = document.getElementById('lstActivities_trigger');
    var addActivitiesBtn = document.getElementById('btnAddActivities');
    var huidigeActiviteiten = [];

    function isMetContract() { return contractField && !contractField.hidden; }

    // Zolang er nog geen leverancier/contract gekozen is, heeft "activiteit toevoegen" sowieso niks
    // om uit te kiezen (de lijst komt net uit dat contract/die leverancier) — veld en knop dan uit.
    function setActiviteitenDisabled(disabled) {
        if (activiteitenTrigger) {
            activiteitenTrigger.setAttribute('aria-disabled', disabled ? 'true' : 'false');
            activiteitenTrigger.tabIndex = disabled ? -1 : 0;
        }
        if (addActivitiesBtn) addActivitiesBtn.disabled = disabled;
    }

    function leegActiviteiten() {
        huidigeActiviteiten = [];
        if (activiteitenWrap && window.GlV2Select) window.GlV2Select.setItems(activiteitenWrap, [], '');
        setActiviteitenDisabled(true);
        document.getElementById('DetailRows').innerHTML = '';
        recomputeSomcontrole();
    }

    function refreshActiviteiten(url, body) {
        fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body })
            .then(function (r) { return r.json(); })
            .then(function (items) {
                huidigeActiviteiten = (items || []).map(function (it) { return { value: String(it.id), text: it.text, group: it.group || null }; });
                if (activiteitenWrap && window.GlV2Select) window.GlV2Select.setItems(activiteitenWrap, huidigeActiviteiten, '');
                setActiviteitenDisabled(false);
            });
    }

    setActiviteitenDisabled(true);

    // Segmented "Met contract" / "Zonder contract" — toont/verbergt de twee velden en reset het
    // andere veld se verborgen waarde (anders blijft bv. een eerder gekozen ContractID meesturen
    // ook al koos de gebruiker nadien "Zonder contract"). Beide velden zijn nu GlV2/_GlV2Combo
    // (zelfde component, Contract met een statische lijst, Leverancier met AJAX) — zelfde reset-pad.
    function wisCombo(fieldEl) {
        var comboRoot = fieldEl ? fieldEl.querySelector('[data-gl-v2-combo]') : null;
        if (comboRoot && window.GlV2Combo) window.GlV2Combo.setValue(comboRoot, '', '', '');
    }

    contractTypeButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            contractTypeButtons.forEach(function (b) { b.classList.remove('is-active'); });
            btn.classList.add('is-active');
            var metContract = btn.getAttribute('data-contracttype') === '0';
            if (contractField) contractField.hidden = !metContract;
            if (companyField) companyField.hidden = metContract;
            if (metContract) wisCombo(companyField); else wisCombo(contractField);
            toonGeenContract();
            leegActiviteiten();
        });
    });

    if (contractHidden) {
        contractHidden.addEventListener('change', function () {
            if (!isMetContract()) return;
            var contractId = contractHidden.value;
            if (!contractId) { toonGeenContract(); leegActiviteiten(); return; }
            refreshActiviteiten(cfg.getContractActivitiesUrl, 'contractid=' + encodeURIComponent(contractId));
            document.getElementById('DetailRows').innerHTML = '';
            recomputeSomcontrole();
            laadContractInfo(contractId);
        });
    }
    if (companyHidden) {
        companyHidden.addEventListener('change', function () {
            if (isMetContract()) return;
            var companyId = companyHidden.value;
            toonGeenContract();
            if (!companyId || companyId === '0') { leegActiviteiten(); return; }
            refreshActiviteiten(cfg.getCompanyActivitiesUrl, 'companyid=' + encodeURIComponent(companyId));
            document.getElementById('DetailRows').innerHTML = '';
            recomputeSomcontrole();
        });
    }

    if (isMetContract()) {
        if (cfg.contractId) laadContractInfo(cfg.contractId); else toonGeenContract();
        if (cfg.contractId) refreshActiviteiten(cfg.getContractActivitiesUrl, 'contractid=' + cfg.contractId);
    } else {
        toonGeenContract();
        if (cfg.companyId) refreshActiviteiten(cfg.getCompanyActivitiesUrl, 'companyid=' + cfg.companyId);
    }

    // ── "Toevoegen" — één activiteit per klik (design-handoff 42c se eigen toevoegrij: één dashed
    // select + knop, geen meervoudige selectie zoals de legacy pagina had). ────────────────────────
    if (addActivitiesBtn) {
        addActivitiesBtn.addEventListener('click', function () {
            var activiteitId = document.getElementById('lstActivities').value;
            if (!activiteitId) return;
            var gekozen = huidigeActiviteiten.find(function (it) { return it.value === activiteitId; });
            var activiteitNaam = gekozen ? gekozen.text : '';
            var contractId = isMetContract() ? (contractHidden ? contractHidden.value : '0') : '0';
            var companyId = isMetContract() ? '0' : (companyHidden ? companyHidden.value : '0');
            var body = new URLSearchParams();
            body.set('ActivityId', activiteitId);
            body.set('ActivityName', activiteitNaam);
            body.set('ContractId', contractId || '0');
            body.set('CompanyId', companyId || '0');
            fetch(cfg.addRowUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body.toString() })
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    var tpl = document.createElement('template');
                    tpl.innerHTML = html.trim();
                    var row = tpl.content.firstElementChild;
                    if (!row) return;
                    document.getElementById('DetailRows').appendChild(row);
                    if (window.CurrencyMask) window.CurrencyMask.init(row.querySelectorAll('.Currencymask'));
                    recomputeSomcontrole();
                });
            if (activiteitenWrap && window.GlV2Select) window.GlV2Select.setItems(activiteitenWrap, huidigeActiviteiten, '');
        });
    }

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-row-delete]')) {
            var row = e.target.closest('[data-row]');
            if (row) { row.remove(); recomputeSomcontrole(); }
        }
    });

    if (window.CurrencyMask) window.CurrencyMask.init();
    recomputeSomcontrole();

    // "Factuur inlezen (PDF)" uit het 42a-menu (focusUpload=true) — meteen naar de dropzone i.p.v.
    // een aparte intake-pagina: dezelfde bestemming, enkel de aandacht gaat eerst naar de upload.
    if (cfg.focusUpload && dropzone) {
        dropzone.scrollIntoView({ behavior: 'smooth', block: 'center' });
        dropzone.classList.add('is-dragover');
        setTimeout(function () { dropzone.classList.remove('is-dragover'); }, 1200);
    }
})();
