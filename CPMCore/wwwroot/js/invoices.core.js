$(function () {
    const $hostStages = $('#stagesList');

    const getPartyType = () => $('input[name="PartyType"]').val();
    const getPartyId = () => $('input[name="PartyId"]').val();
    const isSupplier = () => getPartyType() === '3';
    const isCustomer = () => getPartyType() === '1' || getPartyType() === '2';

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
    window.getSelectedTermOption = getSelectedTermOption; // preview gebruikt dit

    function setIssuerDefaultPaymentTerm() {
        const $opt = $('#IssuerCompanyId option:selected');
        const defId = $opt.data('defaultterm');
        if (defId != null && defId !== '') {
            $('#PaymentTermId').val(String(defId));
            if ($('#PaymentTermId').data('select2')) $('#PaymentTermId').trigger('change.select2');
            $('#PaymentTermId').trigger('change');
        }
    }

    function clearPreview() {
        $('#pvTables').empty();
        $('#pvSub,#pvVat,#pvTotal').text('0,00');
        $('#pvHeaderText').text('');
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
        $('#freeLineBlock').find('input[type="text"],input[type="number"]').val('');
        $('#HeaderDescription').prop('disabled', false).val('');
    }
    function hardResetUI() {
        clearBlocks();
        clearPreview();
        $hostStages.find('input[type=checkbox]').prop('checked', false);
        CPM.util.safeCall(window.rebuildInvoicePreview);
        CPM.util.safeCall(window.updateHeaderLock);
        $('#Mode').val('1');
        $('#freeLineBlock').show();
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
            $('#stagesList').html('<div class="text-muted small">Kies “Schijven”.</div>'); return;
        }
        $('#stagesList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = $('#ComposeStageLinesUrl').val() || ''; // optioneel hidden met Url.Action
            const q = url ? `${url}?clientId=${encodeURIComponent(clientId)}&projectId=${encodeURIComponent(projectId)}` : '';
            const html = q ? await $.get(q) : '';
            $('#stagesList').html(html || '<div class="alert alert-default mb-0">Geen factureerbare schijven.</div>');
            CPM.util.safeCall(window.rebuildHeaderFromSelection);
            CPM.util.safeCall(window.updateHeaderLock);
            CPM.util.safeCall(window.rebuildInvoicePreview);
        } catch {
            $('#stagesList').html('<div class="text-danger small">Kon schijven niet laden.</div>');
        }
    }

    async function loadChangeOrderLines() {
        const pt = getPartyType(), clientId = getPartyId();
        if (!clientId || !(pt === '1' || pt === '2') || $('#Mode').val() !== '3') {
            $('#coList').html('<div class="text-muted small">Kies “Wijzigingsopdrachten”.</div>'); return;
        }
        $('#coList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = $('#ComposeChangeOrderLinesUrl').val() || '';
            const q = url ? `${url}?clientId=${encodeURIComponent(clientId)}&projectId=${encodeURIComponent(projectId)}` : '';
            const html = q ? await $.get(q) : '';
            $('#coList').html(html || '<div class="alert alert-default mb-0">Geen wijzigingsopdrachten gevonden.</div>');
            if (window.initCoUi) window.initCoUi();
            CPM.util.safeCall(window.rebuildHeaderFromSelection);
            CPM.util.safeCall(window.updateHeaderLock);
            CPM.util.safeCall(window.rebuildInvoicePreview);
        } catch {
            $('#coList').html('<div class="text-danger small">Kon wijzigingsopdrachten niet laden.</div>');
        }
    }

    async function loadUtilityLines() {
        const clientId = getPartyId();
        if (!clientId || $('#Mode').val() !== '4') {
            $('#utlList').html('<div class="text-muted small">Kies “Nutsaansluitingen”.</div>'); return;
        }
        $('#utlList').html('<div class="text-muted">Laden…</div>');
        try {
            const projectId = $('input[name="ProjectId"]').val() || '';
            const url = $('#ComposeUtilityLinesUrl').val() || '';
            const q = url ? `${url}?clientId=${encodeURIComponent(clientId)}&projectId=${encodeURIComponent(projectId)}` : '';
            const html = q ? await $.get(q) : '';
            $('#utlList').html(html || '<div class="alert alert-default mb-0">Geen nutsaansluitingen gevonden.</div>');
            CPM.util.safeCall(window.rebuildHeaderFromSelection);
            CPM.util.safeCall(window.updateHeaderLock);
            CPM.util.safeCall(window.rebuildInvoicePreview);
        } catch {
            $('#utlList').html('<div class="text-danger small">Kon nutsaansluitingen niet laden.</div>');
        }
    }

    // Select2 & pickers
    $('#partySelect').select2({
        ajax: {
            url: $('#PartyLookupUrl').val() || '',
            dataType: 'json', delay: 250,
            data: params => ({ term: params.term || '', take: 20 }),
            processResults: data => data
        },
        theme: 'bootstrap', language: 'nl',
        placeholder: 'Zoek de klant/leverancier ...',
        minimumInputLength: 1, dropdownAutoWidth: true
    }).on('select2:select', function (e) {
        const v = e.params.data.id || ''; const [p, idStr = ''] = v.split(':');
        let typeVal = null; if (p === 'ca') typeVal = 1; if (p === 'cc') typeVal = 2; if (p === 'su') typeVal = 3;
        $('input[name="PartyId"]').val(idStr);
        $('input[name="PartyType"]').val(typeVal || '');
        enforceModeByParty();
        toggleProjectContract();
        hardResetUI();
        CPM.util.safeCall(window.rebuildInvoicePreview);
    });

    $('.js-datepicker').datepicker({ format: 'dd/MM/yyyy', todayHighlight: false, autoclose: true, language: 'nl-BE' });

    $('#projectSelect').select2({
        ajax: {
            url: $('#ProjectLookupUrl').val() || '', dataType: 'json', delay: 250,
            data: params => ({ term: params.term || '', take: 20, clientId: isCustomer() ? (getPartyId() || null) : null }),
            processResults: data => data
        },
        theme: 'bootstrap', language: 'nl', placeholder: 'Zoek project ...', allowClear: true,
        minimumInputLength: 1, dropdownAutoWidth: true
    }).on('select2:select', function (e) {
        $('input[name="ProjectId"]').val(e.params.data.id);
        $('#contractSelect').val(null).trigger('change'); $('input[name="SupplierContractId"]').val('');
        const m = $('#Mode').val();
        if (m === '2' && isCustomer()) loadStageLines();
        if (m === '3' && isCustomer()) loadChangeOrderLines();
        if (m === '4' && isCustomer()) loadUtilityLines();
    }).on('select2:clear', function () {
        $('input[name="ProjectId"]').val('');
        const m = $('#Mode').val();
        if (m === '2' && isCustomer()) loadStageLines();
        if (m === '3' && isCustomer()) loadChangeOrderLines();
        if (m === '4' && isCustomer()) loadUtilityLines();
    });

    $('#contractSelect').select2({
        ajax: {
            url: $('#SupplierContractLookupUrl').val() || '', dataType: 'json', delay: 250,
            data: params => ({ term: params.term || '', take: 20, supplierCompanyId: isSupplier() ? (getPartyId() || null) : null }),
            processResults: data => data
        },
        theme: 'bootstrap', language: 'nl', placeholder: 'Zoek contract (op projectnaam) ...', allowClear: true,
        minimumInputLength: 1, dropdownAutoWidth: true
    }).on('select2:select', function (e) {
        $('input[name="SupplierContractId"]').val(e.params.data.id);
        $('#projectSelect').val(null).trigger('change'); $('input[name="ProjectId"]').val('');
    }).on('select2:clear', function () {
        $('input[name="SupplierContractId"]').val('');
    });

    $(document).on('change', '#IssuerCompanyId', function () {
        hardResetUI();
        setIssuerDefaultPaymentTerm();
        CPM.util.safeCall(window.rebuildInvoicePreview);
    });

    function toggleBlocks() {
        const mode = $('#Mode').val();
        const showStages = (mode === '2' && isCustomer());
        const showCO = (mode === '3' && isCustomer());
        const showUtl = (mode === '4' && isCustomer());

        $('#stagesBlock').toggle(showStages);
        $('#coBlock').toggle(showCO);
        $('#utlBlock').toggle(showUtl);
        $('#freeLineBlock').toggle(!(showStages || showCO || showUtl));

        if (showStages) loadStageLines();
        if (showCO) loadChangeOrderLines();
        if (showUtl) loadUtilityLines();

        updateHeaderLock();
        CPM.util.safeCall(window.rebuildInvoicePreview);
    }

    $('#Mode').on('change', function () {
        clearBlocks();
        toggleBlocks();
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
                $(`#stagesList .js-co-pct[data-group='${g}']`).each(function () {
                    const pct = parseFloat(String($(this).val() || '0').replace(',', '.')) || 0;
                    if (pct > 0) ok = true;
                });
            });
            if (!ok) { e.preventDefault(); alert("Kies minstens één wijzigingsopdracht met een percentage > 0%."); return false; }
        }
        return true;
    });

    // Init
    enforceModeByParty();
    toggleProjectContract();
    hardResetUI();
    setIssuerDefaultPaymentTerm();
    CPM.util.safeCall(window.rebuildInvoicePreview);
});
