// Pagina-initialisatie, select2/pickers, mode-toggling, resets
$(function () {
    const $hostStages = $('#stagesList');
    const getPartyType = () => $('input[name="PartyType"]').val(); // "1","2","3"
    const getPartyId = () => $('input[name="PartyId"]').val();
    const isSupplier = () => getPartyType() === '3';
    const isCustomer = () => getPartyType() === '1' || getPartyType() === '2';

    // --- Helpers: defaults op basis van issuer ---
    async function fetchIssuerDefaults(issuerId) {
        try {
            const url = window.InvoicesEndpoints.issuerDefaults + '?issuerId=' + encodeURIComponent(issuerId);
            return await $.getJSON(url); // { defaultPaymentTermId, defaultVatTypeId }
        } catch {
            return { defaultPaymentTermId: null, defaultVatTypeId: null };
        }
    }

    function setPaymentTermIfPresent(termId) {
        if (termId != null && termId !== '') {
            $('#PaymentTermId').val(String(termId));
            if ($('#PaymentTermId').data('select2')) $('#PaymentTermId').trigger('change.select2');
            $('#PaymentTermId').trigger('change');
        }
    }

    function setVatTypeIfPresent(vatTypeId) {
        if (vatTypeId != null && vatTypeId !== '') {
            $('#VatTypeId').val(String(vatTypeId));
            if ($('#VatTypeId').data('select2')) $('#VatTypeId').trigger('change.select2');
            $('#VatTypeId').trigger('change');
        }
    }


    async function applyIssuerDefaultsFromServer() {
        const issuerId = $('#IssuerCompanyId').val();
        if (!issuerId) return;
        const d = await fetchIssuerDefaults(issuerId);
        setPaymentTermIfPresent(d.defaultPaymentTermId);
        setVatTypeIfPresent(d.defaultVatTypeId);
    }
    function getHeader() {
        const $hdr = $('#HeaderDescription');
        return $hdr.length ? $hdr : $();
    }

    function getSelectedTermOption() {
        const $opt = $('#PaymentTermId option:selected');
        if ($opt.length === 0) return null;
        const days = parseInt($opt.data('days'), 10) || 0;
        const name = ($opt.data('name') || $opt.text() || '').toString();
        return { days, name };
    }

    function clearPreview() {
        $('#pvTables').empty();
        $('#pvSub,#pvVat,#pvTotal').text('0,00');
        $('#pvHeaderText').text('');
        $('#pvTitleMode').text('');
        $('#pvMode').text('—');
        $('#pvPayTerm').text('—');
        $('#pvIssueDate').text('—');
        $('#pvDueDate').text('—');
        $('#previewCard').hide();
    }

    function clearBlocks() {
        $('#stagesBlock, #coBlock, #utlBlock, #freeLineBlock').hide();
        $('#stagesList').empty();
        $('#coList').empty();
        $('#utlList').empty();
        if (window.freeLines && window.freeLines.dt) {
            window.freeLines.clearAll(); // alle vrije lijnen leeg + redraw
        }
        $('#HeaderDescription').prop('disabled', false).val('');
    }

    function hardResetUI() {
        clearBlocks();
        clearPreview();
        $hostStages.find('input[type=checkbox]').prop('checked', false);
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        if (window.updateHeaderLock) window.updateHeaderLock();
        $('#Mode').val('1');
        $('#freeLineBlock').show();
        if (window.freeLines) { window.freeLines.clearAll(); window.freeLines.ensureOne(); }
    }

    function enforceModeByParty() {
        const $mode = $('#Mode');
        if (isSupplier()) {
            $mode.val('1');
            $mode.find('option[value!="1"]').prop('disabled', true).hide();
            $('#stagesBlock, #coBlock, #utlBlock').hide();
            $('#freeLineBlock').show();
        } else {
            $mode.find('option').prop('disabled', false).show();
        }
    }

    function toggleProjectContract() {
        if (isSupplier()) {
            $('#projectRow, #contractRow').show();
        } else if (isCustomer()) {
            $('#projectRow, #contractRow').hide();
            $('#projectSelect').val(null).trigger('change'); $('input[name="ProjectId"]').val('');
            $('#contractSelect').val(null).trigger('change'); $('input[name="SupplierContractId"]').val('');
        } else {
            $('#projectRow, #contractRow').hide();
        }
    }

    function updateHeaderLock() {
        const $header = getHeader();
        if (!$header.length) return;
        const anySelected =
            $hostStages.find('.js-stage-row:checked').length > 0 ||
            $hostStages.find('.js-co-row:checked').length > 0 ||
            $hostStages.find('.js-utl-row:checked').length > 0;
        $header.prop('disabled', anySelected);
    }
    window.updateHeaderLock = window.updateHeaderLock || updateHeaderLock;

    async function loadStageLines() {
        const pt = getPartyType(), clientId = getPartyId();
        if (!clientId || !(pt === '1' || pt === '2') || $('#Mode').val() !== '2') {
            $('#stagesList').html('<div class="text-muted small">Kies “Schijven”.</div>');
            return;
        }
        $('#stagesList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = window.InvoicesEndpoints.composeStageLines
                + '?clientId=' + encodeURIComponent(clientId)
                + '&projectId=' + encodeURIComponent(projectId);
            const html = await $.get(url);
            $('#stagesList').html(html);
            if ($('#stagesList').find('.js-stage-row').length === 0) {
                $('#stagesList').html('<div class="alert alert-default mb-0">Geen factureerbare schijven gevonden voor deze klant/project.</div>');
            }
            if (window.rebuildHeaderFromSelection) window.rebuildHeaderFromSelection();
            if (window.updateHeaderLock) window.updateHeaderLock();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        } catch {
            $('#stagesList').html('<div class="text-danger small">Kon schijven niet laden.</div>');
        }
    }

    async function loadChangeOrderLines() {
        const pt = getPartyType(), clientId = getPartyId();
        if (!clientId || !(pt === '1' || pt === '2') || $('#Mode').val() !== '3') {
            $('#coList').html('<div class="text-muted small">Kies “Wijzigingsopdrachten”.</div>');
            return;
        }
        $('#coList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = window.InvoicesEndpoints.composeChangeOrderLines
                + '?clientId=' + encodeURIComponent(clientId)
                + '&projectId=' + encodeURIComponent(projectId);
            const html = await $.get(url);
            $('#coList').html(html);
            const noMasters = $('#coList').find('.js-co-master').length === 0;
            const noRows = $('#coList').find('.js-co-pct').length === 0;
            if (noMasters || noRows) {
                $('#coList').html('<div class="alert alert-default mb-0">Geen wijzigingsopdrachten gevonden om te factureren.</div>');
            } else {
                if (window.initCoUi) window.initCoUi();
            }
            if (window.rebuildHeaderFromSelection) window.rebuildHeaderFromSelection();
            if (window.updateHeaderLock) window.updateHeaderLock();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        } catch {
            $('#coList').html('<div class="text-danger small">Kon wijzigingsopdrachten niet laden.</div>');
        }
    }

    async function loadUtilityLines() {
        const clientId = getPartyId();
        if (!clientId || $('#Mode').val() !== '4') {
            $('#utlList').html('<div class="text-muted small">Kies “Nutsaansluitingen”.</div>');
            return;
        }
        $('#utlList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = window.InvoicesEndpoints.composeUtilityLines
                + '?clientId=' + encodeURIComponent(clientId)
                + '&projectId=' + encodeURIComponent(projectId);
            const html = await $.get(url);
            $('#utlList').html(html);
            if ($('#utlList').find('.js-utl-row').length === 0) {
                $('#utlList').html('<div class="alert alert-default mb-0">Geen nutsaansluitingen gevonden om te factureren.</div>');
            }
            if (window.rebuildHeaderFromSelection) window.rebuildHeaderFromSelection();
            if (window.updateHeaderLock) window.updateHeaderLock();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        } catch {
            $('#utlList').html('<div class="text-danger small">Kon nutsaansluitingen niet laden.</div>');
        }
    }

    function toggleBlocks() {
        const mode = $('#Mode').val();
        const showStages = (mode === '2' && isCustomer());
        const showCO = (mode === '3' && isCustomer());
        const showUtl = (mode === '4' && isCustomer());
        const showFree = !(showStages || showCO || showUtl);

        $('#stagesBlock').toggle(showStages);
        $('#coBlock').toggle(showCO);
        $('#utlBlock').toggle(showUtl);
        $('#freeLineBlock').toggle(showFree);

        if (showStages) loadStageLines();
        if (showCO) loadChangeOrderLines();
        if (showUtl) loadUtilityLines();
        if (showFree && window.freeLines) window.freeLines.ensureOne();

        updateHeaderLock();
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    }

    // Select2 & pickers
    $('#partySelect')
        .select2({
            ajax: {
                url: window.InvoicesEndpoints.partyLookup,
                dataType: 'json',
                delay: 250,
                data: params => ({ term: params.term || '', take: 20 }),
                processResults: data => data
            },
            theme: 'bootstrap',
            language: 'nl',
            placeholder: 'Zoek de klant/leverancier ...',
            minimumInputLength: 1,
            dropdownAutoWidth: true,
            templateResult: function (item) {
                if (!item.id) return item.text;
                let badge = '';
                if (item.type === 'ClientAccount') badge = '<span class="badge bg-primary me-2">klant</span>';
                else if (item.type === 'ClientContact') badge = '<span class="badge bg-secondary me-2">co-owner</span>';
                else if (item.type === 'Supplier') badge = '<span class="badge bg-accent me-2">leverancier</span>';
                return $('<span>').html(badge + item.text);
            }
        })
        .on('select2:select', function (e) {
            const v = e.params.data.id || '';
            const [p, idStr = ''] = v.split(':');
            let typeVal = null;
            if (p === 'ca') typeVal = 1;
            if (p === 'cc') typeVal = 2;
            if (p === 'su') typeVal = 3;
            $('input[name="PartyId"]').val(idStr);
            $('input[name="PartyType"]').val(typeVal || '');
            enforceModeByParty();
            toggleProjectContract();
            hardResetUI();
            if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        });

    $('.js-datepicker').datepicker({
        format: 'dd/MM/yyyy',
        todayHighlight: false,
        autoclose: true,
        language: 'nl-BE'
    });

    $('#projectSelect')
        .select2({
            ajax: {
                url: window.InvoicesEndpoints.projectLookup,
                dataType: 'json',
                delay: 250,
                data: params => ({
                    term: params.term || '',
                    take: 20,
                    clientId: isCustomer() ? (getPartyId() || null) : null
                }),
                processResults: data => data
            },
            theme: 'bootstrap',
            language: 'nl',
            placeholder: 'Zoek project ...',
            allowClear: true,
            minimumInputLength: 1,
            dropdownAutoWidth: true
        })
        .on('select2:select', function (e) {
            $('input[name="ProjectId"]').val(e.params.data.id);
            $('#contractSelect').val(null).trigger('change'); $('input[name="SupplierContractId"]').val('');
            const m = $('#Mode').val();
            if (m === '2' && isCustomer()) loadStageLines();
            if (m === '3' && isCustomer()) loadChangeOrderLines();
            if (m === '4' && isCustomer()) loadUtilityLines();
        })
        .on('select2:clear', function () {
            $('input[name="ProjectId"]').val('');
            const m = $('#Mode').val();
            if (m === '2' && isCustomer()) loadStageLines();
            if (m === '3' && isCustomer()) loadChangeOrderLines();
            if (m === '4' && isCustomer()) loadUtilityLines();
        });

    $('#contractSelect')
        .select2({
            ajax: {
                url: window.InvoicesEndpoints.supplierContractLookup,
                dataType: 'json',
                delay: 250,
                data: params => ({
                    term: params.term || '',
                    take: 20,
                    supplierCompanyId: isSupplier() ? (getPartyId() || null) : null
                }),
                processResults: data => data
            },
            theme: 'bootstrap',
            language: 'nl',
            placeholder: 'Zoek contract (op projectnaam) ...',
            allowClear: true,
            minimumInputLength: 1,
            dropdownAutoWidth: true
        })
        .on('select2:select', function (e) {
            $('input[name="SupplierContractId"]').val(e.params.data.id);
            $('#projectSelect').val(null).trigger('change'); $('input[name="ProjectId"]').val('');
        })
        .on('select2:clear', function () {
            $('input[name="SupplierContractId"]').val('');
        });

    // Facturatiebedrijf wijzigt
    $(document).on('change', '#IssuerCompanyId', async function () {
        hardResetUI();
        await applyIssuerDefaultsFromServer();
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });

    // BTW-type wijziging → zet BTW% op lege vrije-lijnen velden
    $(document).on('change', '#VatTypeId', function () {
        const pct = parseFloat(String($('#VatTypeId option:selected').data('pct') || '0').replace(',', '.')) || 0;
        if (window.freeLines && window.freeLines.dt) {
            window.freeLines.dt.rows().every(function () {
                const $tr = $(this.node());
                const $vat = $tr.find('.js-fl-vat');
                const val = ($vat.val() || '').trim();
                if (val === '' || val === '0' || isNaN(parseFloat(val.replace(',', '.')))) {
                    $vat.val(String(pct));
                }
            });
        }
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });


    // Mode toggling
    $('#Mode').on('change', function () {
        clearBlocks();
        toggleBlocks();
        if (String($(this).val()) === '1' && window.freeLines) {
            window.freeLines.clearAll();
            window.freeLines.ensureOne();
        }
    });

    // Delegated stage/CO/UTL events
    $(document).on('change', '#stagesList .js-check-all', function () {
        const g = $(this).data('group');
        const checked = this.checked;
        $hostStages.find(".js-stage-row[data-group='" + g + "']").prop('checked', checked).trigger('change');
        $hostStages.find(".js-co-row[data-group='" + g + "']").prop('checked', checked).trigger('change');
        $hostStages.find(".js-utl-row[data-group='" + g + "']").prop('checked', checked).trigger('change');
    });

    $(document).on('change', '#stagesList .js-stage-row, #stagesList .js-co-row, #stagesList .js-utl-row', function () {
        const g = $(this).data('group');
        const $rows = $hostStages.find("input[type='checkbox'][data-group='" + g + "']");
        const $master = $hostStages.find(".js-check-all[data-group='" + g + "']");
        const allChecked = $rows.length > 0 && $rows.filter(':checked').length === $rows.length;
        $master.prop('checked', allChecked);
        if (window.rebuildHeaderFromSelection) window.rebuildHeaderFromSelection();
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    });

    // Form guard
    $('#invoiceForm').on('submit', function (e) {
        const partyType = getPartyType();
        const projectId = $('input[name="ProjectId"]').val();
        const contractId = $('input[name="SupplierContractId"]').val();
        const mode = $('#Mode').val();

        if (partyType === '3' && !projectId && !contractId) {
            e.preventDefault(); alert("Kies minstens een project of een contract voor de leveranciersfactuur."); return false;
        }
        if (isCustomer() && mode === '2') {
            if ($('#stagesList input[type="checkbox"][name$=".IsSelected"]:checked').length === 0) {
                e.preventDefault(); alert("Kies minstens één schijf-lijn."); return false;
            }
        }
        if (isCustomer() && mode === '3') {
            let ok = false;
            $('#stagesList .js-co-master:checked').each(function () {
                const g = $(this).data('group');
                $('#stagesList .js-co-pct[data-group="' + g + '"]').each(function () {
                    const pct = parseFloat(String($(this).val() || '0').replace(',', '.')) || 0;
                    if (pct > 0) ok = true;
                });
            });
            if (!ok) { e.preventDefault(); alert("Kies minstens één wijzigingsopdracht met een percentage > 0%."); return false; }
        }
        prepareFreeLinesForPost($(this));
        return true;
    });

    // Init
    enforceModeByParty();
    toggleProjectContract();
    hardResetUI();
    setIssuerDefaultPaymentTerm();
    if ($('#Mode').val() === '1' && window.freeLines) { window.freeLines.ensureOne(); }
    if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
});

function prepareFreeLinesForPost($form) {
    const $host = $('#freeLinesHidden');
    if ($host.length === 0) return;

    $host.empty();

    if ($('#Mode').val() !== '1') return;

    if (!$.fn.dataTable || !$.fn.dataTable.isDataTable || !$.fn.dataTable.isDataTable('#freeLinesTable')) return;

    const parseLocaleNumber = (window.InvoicesUtil && window.InvoicesUtil.parseLocaleNumber)
        ? window.InvoicesUtil.parseLocaleNumber
        : function (val) {
            if (val == null) return 0;
            let s = String(val).trim();
            if (!s) return 0;
            s = s.replace(/[\s\u00A0\u202F\u2009]/g, '');
            const hasComma = s.includes(','), hasDot = s.includes('.');
            if (hasComma && hasDot) {
                const lc = s.lastIndexOf(','), ld = s.lastIndexOf('.');
                const dec = lc > ld ? ',' : '.';
                const idx = dec === ',' ? lc : ld;
                const left = s.slice(0, idx).replace(/[.,]/g, '');
                const right = s.slice(idx + 1).replace(/[.,]/g, '');
                s = left + '.' + right;
            } else if (hasComma) {
                s = s.replace(/\./g, '').replace(',', '.');
            } else {
                s = s.replace(/,/g, '');
            }
            s = s.replace(/[^\d.\-]/g, '');
            const n = parseFloat(s);
            return isNaN(n) ? 0 : n;
        };

    const dt = $('#freeLinesTable').DataTable();
    const indexes = dt.rows().indexes().toArray();
    indexes.sort((a, b) => {
        const va = parseInt(dt.cell(a, 1).data(), 10) || 0;
        const vb = parseInt(dt.cell(b, 1).data(), 10) || 0;
        return va - vb;
    });

    let postIndex = 0;

    const addHidden = (base, field, value) => {
        $('<input>', { type: 'hidden', name: `${base}.${field}`, value }).appendTo($host);
    };
    const addIndexMarker = idx => {
        $('<input>', { type: 'hidden', name: 'Lines.index', value: idx }).appendTo($host);
    };
    const formatDecimal = (val, digits = 2) => {
        if (val == null || Number.isNaN(val)) {
            return (0).toFixed(digits).replace('.', ',');
        }
        return Number(val).toFixed(digits).replace('.', ',');
    };

    indexes.forEach(i => {
        const $tr = $(dt.row(i).node());
        const text = ($tr.find('.js-fl-text').val() || '').trim();
        const priceVal = parseLocaleNumber($tr.find('.js-fl-price').val());

        let vatPct = NaN;
        const $sel = $tr.find('.js-fl-vat-select');
        if ($sel.length) {
            const $opt = $sel.find('option:selected');
            const pctData = $opt.data('pct');
            vatPct = parseFloat(String(pctData != null ? pctData : $opt.text()).replace(',', '.'));
        }
        if (isNaN(vatPct)) {
            const fallback = $('#VatTypeId option:selected').data('pct');
            vatPct = parseFloat(String(fallback != null ? fallback : '0').replace(',', '.')) || 0;
        }

        if (!text && priceVal === 0) return;

        const base = `Lines[${postIndex}]`;
        addIndexMarker(postIndex);
        addHidden(base, 'Text', text);
        addHidden(base, 'Price', formatDecimal(priceVal));
        addHidden(base, 'VatPercentage', formatDecimal(isNaN(vatPct) ? 0 : vatPct));
        addHidden(base, 'IsSelected', 'true');
        addHidden(base, 'LineType', 'Free');
        addHidden(base, 'GroupName', 'Vrije lijnen');
        addHidden(base, 'UtilityCost', 'false');

        postIndex += 1;
    });

    if (postIndex === 0) {
        $host.empty();
    }
}
