// Pagina-initialisatie, select2/pickers, mode-toggling, resets
$(function () {
    const $hostStages = $('#stagesList');
    const getPartyType = () => $('input[name="PartyType"]').val(); // "1","2","3"
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
            const url = $('#stagesList').data('compose-url') // zet desnoods via data-attr
                || ('@Url.Action("ComposeStageLines", "Invoices")' + '?clientId=' + encodeURIComponent(clientId) + '&projectId=' + encodeURIComponent(projectId));
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
            const url = $('#coList').data('compose-url')
                || ('@Url.Action("ComposeChangeOrderLines", "Invoices")' + '?clientId=' + encodeURIComponent(clientId) + '&projectId=' + encodeURIComponent(projectId));
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
            const url = $('#utlList').data('compose-url')
                || ('@Url.Action("ComposeUtilityLines", "Invoices")' + '?clientId=' + encodeURIComponent(clientId) + '&projectId=' + encodeURIComponent(projectId));
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
                url: '@Url.Action("PartyLookup", "Invoices")',
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
                url: '@Url.Action("ProjectLookup", "Invoices")',
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
                url: '@Url.Action("SupplierContractLookup", "Invoices")',
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
    $(document).on('change', '#IssuerCompanyId', function () {
        hardResetUI();
        setIssuerDefaultPaymentTerm();
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
