(function () {
    const initial = window.InitialInvoice || null;
    if (!initial) return;

    function setPartyHiddenFields(value) {
        if (!value) {
            $('input[name="PartyId"]').val('');
            $('input[name="PartyType"]').val('');
            return;
        }
        const parts = String(value).split(':');
        const prefix = parts[0] || '';
        const id = parts[1] || '';
        $('input[name="PartyId"]').val(id);
        let typeVal = '';
        if (prefix === 'su') typeVal = '3';
        else if (prefix === 'cc') typeVal = '2';
        else typeVal = '1';
        $('input[name="PartyType"]').val(typeVal);
    }

    if (initial.party && initial.party.value) {
        const $party = $('#partySelect');
        if ($party.length) {
            const option = new Option(initial.party.text || initial.party.value, initial.party.value, true, true);
            window.skipPartySelectOnce = true;
            $party.append(option).trigger('change');
            setPartyHiddenFields(initial.party.value);
        }
    }

    if (Array.isArray(initial.lines)) {
        if (window.freeLines && typeof window.freeLines.loadInitial === 'function') {
            window.freeLines.loadInitial(initial.lines);
        } else {
            window.pendingInitialLines = initial.lines.slice();
        }
    }

    if (initial.paymentTermId != null) {
        $('#PaymentTermId').val(String(initial.paymentTermId));
    }
    if (initial.vatTypeId != null) {
        $('#VatTypeId').val(String(initial.vatTypeId));
    }
    if (initial.issuerBankAccountId != null) {
        $('#IssuerBankAccountId').val(String(initial.issuerBankAccountId));
    }
    if (initial.mode != null) {
        $('#Mode').val(String(initial.mode));
    }

    $(function () {
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
        if (window.updateSaveButtonState) window.updateSaveButtonState();
    });
})();