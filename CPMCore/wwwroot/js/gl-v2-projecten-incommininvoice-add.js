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
    var kindButtons = document.querySelectorAll('#gln-ai-kind-segmented > button');
    kindButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            kindButtons.forEach(function (b) { b.classList.remove('is-active'); });
            btn.classList.add('is-active');
            document.getElementById('gln-ai-kind').value = btn.getAttribute('data-kind');
            updateContractLiveCijfers();
            refreshBorg();
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
        refreshBorg();
    }

    function geld(v) { return '€ ' + (v || 0).toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

    // Som van de reeds toegevoegde factuurregels — "deze factuur" in de voortgangsbalk/het saldo
    // hieronder. Zelfde regels als recomputeSomcontrole() telt, hier apart zodat beide functies
    // onafhankelijk kunnen blijven werken (somcontrole vergelijkt tegen het Factuurbedrag-veld, dit
    // hier telt mee op bij wat het contract al eerder gefactureerd kreeg).
    // "Deze factuur" = het ingevulde Factuurbedrag (zoals in het ontwerp: deze € 29.435 = het hele factuurbedrag, ook al zijn er nog geen regels);
    // pas als dat leeg is, de som van de regels. Een creditnota telt negatief.
    function dezeFactuurBedrag() {
        var som = 0;
        document.querySelectorAll('#DetailRows [data-row]').forEach(function (r) {
            var priceInput = r.querySelector('.gln-ai-row-price input');
            if (priceInput) som += parseCurrency(priceInput.value);
        });
        var bedragInput = document.getElementById('IncommingInvoice_InvoicePrice');
        var bedrag = bedragInput ? Math.abs(parseCurrency(bedragInput.value)) : 0;
        var basis = bedrag > 0 ? bedrag : Math.abs(som);
        var kind = document.getElementById('gln-ai-kind');
        return kind && kind.value === '2' ? -basis : basis;
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

        refreshBorg(); // zet ook de borg-hint in de contractkaart

        // Korting-hint ("€X als betaald vóór DD/MM") is zowel datum- als "deze"-afhankelijk — die
        // berekening zit al in recomputeVervaldatum() (heeft de factuurdatum nodig), hier enkel
        // opnieuw aanroepen zodat 'm ook bij een regelwijziging (niet enkel een datumwijziging) klopt.
        recomputeVervaldatum();
    }

    function laadContractInfo(contractId, vanGebruiker) {
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

                if (vanGebruiker) { borgBeslist = false; borgZelfGezet = false; }
                recomputeVervaldatum(vanGebruiker);
                updateContractLiveCijfers();
                refreshBorg();
            })
            .catch(toonGeenContract);
    }

    // ── Vervaldatum/korting (42e punt 03) — berekend, niet opgeslagen ───────────────────────────
    function parseIsoDate(iso) { if (!iso) return null; var p = iso.split('-'); return new Date(Number(p[0]), Number(p[1]) - 1, Number(p[2])); }
    function fmtDate(d) { return d.getDate().toString().padStart(2, '0') + '/' + (d.getMonth() + 1).toString().padStart(2, '0') + '/' + d.getFullYear(); }

    // Vervaldatum: met contract (betaaltermijn bekend) berekend en niet bewerkbaar; zonder contract of zonder
    // betaaltermijn zelf in te vullen. Een bestaande waarde (bewerken) blijft staan tot de gebruiker de
    // factuurdatum of het contract wijzigt. De waarde wordt gepost via het verborgen veld gln-ai-duedate-post.
    var vervalEl = document.getElementById('gln-ai-vervaldatum');
    var duePost = document.getElementById('gln-ai-duedate-post');
    var dueBehouden = !!(vervalEl && vervalEl.value);
    function isoDate(d) { return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'); }
    function syncDuePost() { if (duePost && vervalEl) duePost.value = vervalEl.value || ''; }
    if (vervalEl) vervalEl.addEventListener('input', function () { dueBehouden = true; syncDuePost(); });

    function recomputeVervaldatum(vanGebruiker) {
        var kortingHint = document.getElementById('gln-ai-korting-hint');
        var vervalHint = document.getElementById('gln-ai-vervaldatum-hint');
        var kaartTermijnHint = document.getElementById('gln-ai-contract-termijn-hint');
        var kaartKortingHint = document.getElementById('gln-ai-contract-korting-hint');
        var dateHidden = document.getElementById('IncommingInvoice_IncommingInvoiceDate');
        var factuurdatum = dateHidden ? parseIsoDate(dateHidden.value) : null;
        if (vanGebruiker) dueBehouden = false;
        var auto = !!(laatsteContractInfo && laatsteContractInfo.paymentTerm != null);
        if (vervalEl) vervalEl.readOnly = auto;
        var box = document.getElementById('gln-ai-vervaldatum-box');
        if (box) box.classList.toggle('is-readonly', auto);
        if (vervalHint) vervalHint.textContent = auto
            ? 'automatisch: factuurdatum + ' + laatsteContractInfo.paymentTerm + ' dagen (contract)'
            : (laatsteContractInfo ? 'dit contract heeft geen betaaltermijn — zelf in te vullen' : 'zelf in te vullen — geen contract');
        if (!factuurdatum || !auto) {
            if (kortingHint) kortingHint.textContent = '';
            if (kaartTermijnHint) kaartTermijnHint.textContent = '';
            if (kaartKortingHint) kaartKortingHint.textContent = '';
            syncDuePost();
            return;
        }
        var vervaldatum = new Date(factuurdatum.getTime());
        vervaldatum.setDate(vervaldatum.getDate() + Number(laatsteContractInfo.paymentTerm));
        if (vervalEl && !dueBehouden) vervalEl.value = isoDate(vervaldatum);
        syncDuePost();
        var toon = vervalEl && vervalEl.value ? parseIsoDate(vervalEl.value) : vervaldatum;
        if (kaartTermijnHint) kaartTermijnHint.textContent = 'vervalt ' + fmtDate(toon);
        if (kaartKortingHint) {
            if (laatsteContractInfo.cashDiscount && laatsteContractInfo.cashDiscountPaymentTerm != null && laatsteContractInfo.cashDiscountPercentage != null) {
                var kortingDatumKaart = new Date(factuurdatum.getTime());
                kortingDatumKaart.setDate(kortingDatumKaart.getDate() + Number(laatsteContractInfo.cashDiscountPaymentTerm));
                var kortingBedrag = factuurBedrag() * laatsteContractInfo.cashDiscountPercentage / 100;
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
        if (e.target && e.target.id === 'IncommingInvoice_IncommingInvoiceDate') { recomputeVervaldatum(true); refreshBorg(); }
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
        rows.forEach(function (r) { var priceInput = r.querySelector('.gln-ai-row-price input'); if (priceInput) som += parseCurrency(priceInput.value); });
        var bedragInput = document.getElementById('IncommingInvoice_InvoicePrice');
        var bedrag = bedragInput ? parseCurrency(bedragInput.value) : 0;
        var gelijk = Math.abs(som - bedrag) < 0.01;
        el.className = 'gln-ai-somcontrole ' + (gelijk ? 'is-ok' : 'is-off');
        tekstEl.textContent = rows.length + ' regel' + (rows.length === 1 ? '' : 's') + ' = € ' + som.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
            + (gelijk ? ' · gelijk aan factuurbedrag' : ' · factuurbedrag is € ' + bedrag.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        updateContractLiveCijfers();
    }
    document.addEventListener('input', function (e) {
        if (e.target && ((e.target.closest && e.target.closest('.gln-ai-row-price')) || e.target.id === 'IncommingInvoice_InvoicePrice')) {
            recomputeSomcontrole();
            updateContractLiveCijfers(); // balk, saldo en borg-hint van de contractkaart volgen het factuurbedrag, ook zonder regels
            // De borg hangt af van het factuurbedrag, ook als er nog geen factuurregels zijn (recomputeSomcontrole stopt dan vroeg).
            refreshBorg();
            recomputeVervaldatum(); // de korting contant in de contractkaart rekent op het factuurbedrag
        }
    });

    // ── Borg en extra inhouding (design-handoff 42h, 42c, 42k) ───────────────────────────────────────
    // Borg: standaard aan met het contractpercentage (financiële waarborg), standaard uit zonder contract.
    // Afwijken van het contract kan, met verplichte reden. Extra inhouding: een bedrag dat tijdelijk niet
    // wordt uitbetaald; de borg wordt berekend op factuurbedrag min extra inhouding.
    var borgToggle = document.getElementById('gln-ai-borg-toggle');
    var borgAanHidden = document.getElementById('gln-ai-borg-aan');
    var borgBody = document.getElementById('gln-ai-borg-body');
    var borgPct = document.getElementById('gln-ai-borg-pct');
    var borgBedragEl = document.getElementById('gln-ai-borg-bedrag');
    var borgBedragPost = document.getElementById('gln-ai-borg-bedrag-post');
    var inhToggle = document.getElementById('gln-ai-inhouding-toggle');
    var inhBody = document.getElementById('gln-ai-inhouding-body');
    var inhBedragEl = document.getElementById('gln-ai-inhouding-bedrag');
    var betalenEl = document.getElementById('gln-ai-betalen');
    // true = de server-waarde (bewerken van een factuur die al een borgkeuze heeft) blijft staan tot de gebruiker het contract wijzigt.
    var borgBeslist = borgAanHidden ? borgAanHidden.getAttribute('data-start') !== '-1' : false;
    var borgZelfGezet = false;
    function dutch(n) { return (Math.round(n * 100) / 100).toFixed(2).replace('.', ','); }
    function parsePct(t) { var v = parseFloat(String(t || '').replace(/[^\d,.-]/g, '').replace(',', '.')); return isNaN(v) ? 0 : v; }
    function fmtPct(v) { return Number(v).toLocaleString('nl-BE', { maximumFractionDigits: 2 }); }
    function factuurBedrag() { var el = document.getElementById('IncommingInvoice_InvoicePrice'); return el ? parseCurrency(el.value) : 0; }
    function inhoudingBedrag() { return inhToggle && inhToggle.checked && inhBedragEl ? parseCurrency(inhBedragEl.value) : 0; }
    function contractBorgPct() { return laatsteContractInfo && laatsteContractInfo.guaranteeType === 'FinancialGuarantee' && laatsteContractInfo.guaranteePercentage != null ? Number(laatsteContractInfo.guaranteePercentage) : null; }
    function vrijgaveTekst(vo) {
        vo = Number(vo);
        if (vo >= 100) return '100 % bij VO';
        if (vo <= 0) return '100 % bij DO';
        return fmtPct(vo) + ' % bij VO · ' + fmtPct(100 - vo) + ' % bij DO';
    }

    function refreshBorg() {
        if (!borgToggle) return;
        var cpct = contractBorgPct();
        var metContractInfo = !!laatsteContractInfo;
        var gt = laatsteContractInfo ? laatsteContractInfo.guaranteeType : null;

        // Standaardstand: aan met een financiële waarborg, anders uit — zolang de gebruiker/server niets besliste.
        if (!borgBeslist) {
            borgToggle.checked = cpct != null;
            if (cpct != null && borgPct && !borgZelfGezet) borgPct.value = dutch(cpct);
            if (cpct == null && borgPct && !borgZelfGezet) borgPct.value = '';
        } else if (borgBeslist && borgAanHidden.getAttribute('data-start') === '1' && !borgToggle.dataset.init) {
            borgToggle.checked = true;
        } else if (!borgToggle.dataset.init) {
            borgToggle.checked = false;
        }
        borgToggle.dataset.init = '1';

        var aan = borgToggle.checked;
        borgAanHidden.value = aan ? 'true' : 'false';
        if (borgBody) borgBody.hidden = !aan;

        var netto = factuurBedrag() - inhoudingBedrag();
        var pct = aan ? parsePct(borgPct.value) : 0;
        var bedrag = aan ? Math.round(netto * pct) / 100 : 0;
        if (borgBedragEl) borgBedragEl.value = aan ? bedrag.toLocaleString('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '';
        if (borgBedragPost) borgBedragPost.value = aan ? dutch(bedrag) : '';
        var basis = document.getElementById('gln-ai-borg-basis');
        if (basis) basis.textContent = inhoudingBedrag() > 0 ? 'op ' + geld(netto) + ' (factuur – extra inhouding)' : 'op bedrag excl. btw';
        var amountEl = document.getElementById('gln-ai-borg-amount');
        if (amountEl) amountEl.textContent = aan && bedrag > 0 ? '– ' + geld(bedrag) : '';

        // Afwijking t.o.v. het contract
        var afwijkt = aan && cpct != null && Math.abs(pct - cpct) > 0.0001;
        var afwijkBlok = document.getElementById('gln-ai-borg-afwijk');
        if (afwijkBlok) afwijkBlok.hidden = !afwijkt;
        var afwijkTekst = document.getElementById('gln-ai-borg-afwijk-tekst');
        if (afwijkTekst && cpct != null) afwijkTekst.textContent = 'Het contract zegt ' + fmtPct(cpct) + ' %. Je wijkt af voor deze factuur — reden verplicht bij opslaan.';
        var redenEl = document.getElementById('gln-ai-borg-reden');
        if (redenEl) { redenEl.required = afwijkt; if (!afwijkt) redenEl.value = ''; }

        // Ondertitel
        var sub = document.getElementById('gln-ai-borg-sub');
        if (sub) {
            var label = laatsteContractInfo && laatsteContractInfo.contractLabel ? laatsteContractInfo.contractLabel : null;
            if (!metContractInfo) sub.textContent = aan ? 'manueel aangezet — geen contract' : 'geen contract — standaard uit · aanzetten kan';
            else if (cpct != null) sub.textContent = afwijkt ? 'afwijkend van contract (' + fmtPct(cpct) + ' %)' : 'uit contract' + (label ? ' ' + label : '') + ' — standaard aan';
            else if (gt === 'BankGuarantee') sub.textContent = 'dit contract heeft een bankwaarborg — er wordt niets ingehouden op de factuur' + (aan ? ' (manueel aangezet)' : '');
            else sub.textContent = aan ? 'manueel aangezet — dit contract heeft geen waarborg' : 'dit contract heeft geen waarborg — aanzetten kan';
        }

        // Vrijgave: uit het contract, of zelf te kiezen zonder (financieel) contract
        var vrijContract = document.getElementById('gln-ai-borg-vrijgave-contract');
        var vrijZelf = document.getElementById('gln-ai-borg-vrijgave-zelf');
        var uitContract = cpct != null;
        if (vrijContract) vrijContract.hidden = !uitContract;
        if (vrijZelf) vrijZelf.hidden = uitContract;
        var voSelect = document.getElementById('gln-ai-borg-vo');
        if (voSelect) voSelect.disabled = uitContract; // niet posten als het uit het contract komt
        var vrijTxt = document.getElementById('gln-ai-borg-vrijgave');
        var regel = uitContract ? vrijgaveTekst(laatsteContractInfo.guaranteeReleaseVoPercentage != null ? laatsteContractInfo.guaranteeReleaseVoPercentage : 50) : vrijgaveTekst(voSelect ? voSelect.value : 100);
        if (vrijTxt) vrijTxt.value = regel;

        // Extra inhouding
        var inhAan = inhToggle && inhToggle.checked;
        if (inhBody) inhBody.hidden = !inhAan;
        var inh = inhoudingBedrag();
        var inhAmount = document.getElementById('gln-ai-inhouding-amount');
        if (inhAmount) inhAmount.textContent = inh > 0 ? '– ' + geld(inh) : '';
        var inhTekst = document.getElementById('gln-ai-inhouding-borgtekst');
        if (inhTekst) inhTekst.textContent = aan && pct > 0
            ? 'Geen borg op dit bedrag: de borg wordt berekend op ' + geld(netto) + '. Bij vrijgave gaat er ' + fmtPct(pct) + ' % naar de borg.'
            : 'Geen borg op dit bedrag.';

        // Contractkaart: borg-hint met de live waarden
        var borgHint = document.getElementById('gln-ai-contract-waarborg-hint');
        if (borgHint) borgHint.textContent = cpct != null
            ? (aan ? geld(bedrag) + ' op deze factuur · vrij: ' + regel.replace(' · ', ', ') : 'niet ingehouden op deze factuur')
            : '';

        // Kaart "Te betalen" rechts (42c): factuurbedrag, extra inhouding, borg, en de korting contant als de termijn nog loopt
        var payCard = document.getElementById('gln-ai-betalen-card');
        if (payCard) {
            var fbPay = factuurBedrag();
            payCard.hidden = !(fbPay > 0); // ook zonder contract: borg, extra inhouding en een zelf ingevulde korting tellen mee
            if (!payCard.hidden) {
                var borgNu = aan ? bedrag : 0;
                var teBetalen = fbPay - inh - borgNu;
                document.getElementById('gln-pay-factuur').textContent = geld(fbPay);
                document.getElementById('gln-pay-inh-row').hidden = !(inh > 0);
                document.getElementById('gln-pay-inh').textContent = '– ' + geld(inh);
                document.getElementById('gln-pay-borg-row').hidden = !(aan && borgNu > 0);
                document.getElementById('gln-pay-borg-label').textContent = 'Borg ' + fmtPct(pct) + ' %';
                document.getElementById('gln-pay-borg-sub').textContent = 'vrij ' + regel;
                document.getElementById('gln-pay-borg').textContent = '– ' + geld(borgNu);
                document.getElementById('gln-pay-totaal').textContent = geld(teBetalen);

                // Korting contant: uit het contract, of (zonder contract) uit de eigen velden onder de borg.
                var kRow = document.getElementById('gln-ai-korting-row');
                var kToggle = document.getElementById('gln-ai-korting-toggle');
                var kBody = document.getElementById('gln-ai-korting-body');
                var dk = null;
                if (laatsteContractInfo) dk = laatsteContractInfo;
                else if (kToggle && kToggle.checked) {
                    var kp = parsePct(document.getElementById('gln-ai-korting-pct').value);
                    var kdg = parseInt(document.getElementById('gln-ai-korting-dagen').value, 10);
                    if (kp > 0 && kdg > 0) dk = { cashDiscount: true, cashDiscountPercentage: kp, cashDiscountPaymentTerm: kdg };
                }
                var heeftKorting = !!(dk && dk.cashDiscount && dk.cashDiscountPercentage != null && dk.cashDiscountPaymentTerm != null);
                var kortingBlok = document.getElementById('gln-pay-korting-blok');
                var datumHidden = document.getElementById('IncommingInvoice_IncommingInvoiceDate');
                var fd = datumHidden ? parseIsoDate(datumHidden.value) : null;
                kortingBlok.hidden = !(heeftKorting && fd);
                if (!kortingBlok.hidden) {
                    var kd = new Date(fd.getTime()); kd.setDate(kd.getDate() + Number(dk.cashDiscountPaymentTerm));
                    var vandaag = new Date(); vandaag.setHours(0, 0, 0, 0);
                    var dagen = Math.round((kd.getTime() - vandaag.getTime()) / 86400000);
                    var kortingBedrag2 = Math.round(fbPay * Number(dk.cashDiscountPercentage)) / 100;
                    var verlopen = dagen < 0;
                    document.getElementById('gln-pay-korting-label').textContent = 'Korting contant ' + fmtPct(dk.cashDiscountPercentage) + ' %';
                    document.getElementById('gln-pay-korting-sub').textContent = verlopen ? 'termijn verstreken' : (dagen === 0 ? 'laatste dag' : 'nog ' + dagen + (dagen === 1 ? ' dag' : ' dagen'));
                    document.getElementById('gln-pay-korting-datum').innerHTML = 'korting bij betaling vóór <b>' + fmtDate(kd) + '</b>';
                    document.getElementById('gln-pay-korting').textContent = '– ' + geld(kortingBedrag2);
                    document.getElementById('gln-pay-metkorting').textContent = geld(teBetalen - kortingBedrag2);
                    document.getElementById('gln-pay-korting-rij').classList.toggle('is-verlopen', verlopen);
                    document.getElementById('gln-pay-korting-sub').classList.toggle('is-verlopen', verlopen);
                    document.getElementById('gln-pay-metkorting-rij').hidden = verlopen;
                }
            }
        }

        var kortingRij = document.getElementById('gln-ai-korting-row');
        if (kortingRij) {
            kortingRij.hidden = !!laatsteContractInfo; // met contract komt de korting uit het contract
            var kTog = document.getElementById('gln-ai-korting-toggle');
            var kLichaam = document.getElementById('gln-ai-korting-body');
            if (kLichaam) kLichaam.hidden = !(kTog && kTog.checked);
            var kBedrag = document.getElementById('gln-ai-korting-amount');
            if (kBedrag) {
                var kpct = kTog && kTog.checked ? parsePct(document.getElementById('gln-ai-korting-pct').value) : 0;
                kBedrag.textContent = kpct > 0 && factuurBedrag() > 0 ? '– ' + geld(Math.round(factuurBedrag() * kpct) / 100) : '';
            }
        }

        // Actiebalk: wat er netto te betalen is
        if (betalenEl) {
            var fb = factuurBedrag();
            betalenEl.hidden = !(fb > 0);
            if (fb > 0) {
                var delen = [];
                if (inh > 0) delen.push('extra inhouding – ' + geld(inh));
                if (aan && bedrag > 0) delen.push('borg – ' + geld(bedrag));
                delen.push('te betalen ' + geld(fb - inh - (aan ? bedrag : 0)) + ' excl. btw');
                betalenEl.textContent = '· ' + delen.join(' · ');
            }
        }
    }

    if (borgToggle) {
        borgToggle.addEventListener('change', function () {
            borgBeslist = true; borgZelfGezet = true;
            if (borgToggle.checked && borgPct && !parsePct(borgPct.value)) { var c = contractBorgPct(); borgPct.value = c != null ? dutch(c) : ''; }
            refreshBorg();
            if (borgToggle.checked && borgPct && !contractBorgPct()) borgPct.focus();
        });
        borgPct.addEventListener('input', function () { borgBeslist = true; borgZelfGezet = true; refreshBorg(); });
        borgPct.addEventListener('blur', function () { if (borgPct.value) borgPct.value = dutch(parsePct(borgPct.value)); });
        var terugBtn = document.getElementById('gln-ai-borg-terug');
        if (terugBtn) terugBtn.addEventListener('click', function () { var c = contractBorgPct(); if (c != null) { borgPct.value = dutch(c); refreshBorg(); } });
        var voSel = document.getElementById('gln-ai-borg-vo');
        if (voSel) voSel.addEventListener('change', refreshBorg);
    }
    var kTogEl = document.getElementById('gln-ai-korting-toggle');
    if (kTogEl) {
        kTogEl.addEventListener('change', function () { if (kTogEl.checked) document.getElementById('gln-ai-korting-pct').focus(); refreshBorg(); });
        ['gln-ai-korting-pct', 'gln-ai-korting-dagen'].forEach(function (id) { document.getElementById(id).addEventListener('input', refreshBorg); });
    }
    if (inhToggle) {
        inhToggle.addEventListener('change', function () {
            if (!inhToggle.checked && inhBedragEl) inhBedragEl.value = '';
            if (inhToggle.checked && inhBedragEl) inhBedragEl.focus();
            refreshBorg();
        });
        inhBedragEl.addEventListener('input', refreshBorg);
        inhBedragEl.addEventListener('blur', function () { if (inhBedragEl.value && !inhBedragEl.readOnly) inhBedragEl.value = dutch(parseCurrency(inhBedragEl.value)); });
    }
    var hoofdForm = document.getElementById('gln-ai-form');
    if (hoofdForm) hoofdForm.addEventListener('submit', function () {
        // Netjes in Nederlandse notatie posten (komma, geen duizendtallen) zodat de model binder het niet verkeerd leest.
        if (borgPct && borgPct.value) borgPct.value = dutch(parsePct(borgPct.value));
        if (inhBedragEl && inhBedragEl.value) inhBedragEl.value = dutch(parseCurrency(inhBedragEl.value));
        if (inhToggle && !inhToggle.checked && inhBedragEl) inhBedragEl.value = '';
        // Korting contant: enkel zonder contract en enkel als ze aanstaat; anders niets posten (met contract komt ze uit het contract).
        var kTogS = document.getElementById('gln-ai-korting-toggle');
        var kPctS = document.getElementById('gln-ai-korting-pct');
        var kDagS = document.getElementById('gln-ai-korting-dagen');
        if (kPctS && kDagS) {
            if (laatsteContractInfo || !(kTogS && kTogS.checked)) { kPctS.value = ''; kDagS.value = ''; }
            else if (kPctS.value) kPctS.value = dutch(parsePct(kPctS.value));
        }
        syncDuePost();
    });

    // ── Met/zonder contract (design-handoff 42c: "je kiest in het formulier", geen twee aparte
    // 42a-menu-items meer) + Contract/Leverancier/Activiteiten — gl-v2-native componenten
    // (GlV2/_SelectList → window.GlV2Select, GlV2/_GlV2Combo → window.GlV2Combo), geen select2/jQuery
    // meer nodig voor deze drie velden. ──────────────────────────────────────────────────────────
    var contractField = document.getElementById('gln-ai-contract-field');
    var companyField = document.getElementById('gln-ai-company-field');
    var contractTypeButtons = document.querySelectorAll('#gln-ai-contracttype-segmented > button');
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
            laadContractInfo(contractId, true);
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
    refreshBorg();

    // "Factuur inlezen (PDF)" uit het 42a-menu (focusUpload=true) — meteen naar de dropzone i.p.v.
    // een aparte intake-pagina: dezelfde bestemming, enkel de aandacht gaat eerst naar de upload.
    if (cfg.focusUpload && dropzone) {
        dropzone.scrollIntoView({ behavior: 'smooth', block: 'center' });
        dropzone.classList.add('is-dragover');
        setTimeout(function () { dropzone.classList.remove('is-dragover'); }, 1200);
    }
})();
