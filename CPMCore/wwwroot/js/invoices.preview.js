// Voorbeeldfactuur (preview)
(function () {
    const { nf, parseLocaleNumber, esc, calculateDueDate } = window.InvoicesUtil;

    const $card = $('#previewCard');
    const $tables = $('#pvTables');
    const $sub = $('#pvSub');
    const $vat = $('#pvVat');
    const $total = $('#pvTotal');
    const getCreditSign = () => ($('#IsCreditNote').is(':checked') ? -1 : 1);

    function readDateStr() {
        const v = $('#InvoiceDate').val();
        return v && v.trim() ? v.trim() : null;
    }

    function readStartAsText() {
        let v = $('#StartAs').val();
        if (v == null) v = $('input[name="StartAs"]:checked').val();
        v = (v ?? '').toString().toLowerCase();
        const isCredit = $('#IsCreditNote').is(':checked');
        const isInvoice = (v === 'invoice' || v === '1');
        if (isInvoice) return isCredit ? 'Creditnota' : 'Factuur';
        return isCredit ? 'Proforma creditnota' : 'Proforma factuur';
    }

    function getSelectedTermOption() {
        const $opt = $('#PaymentTermId option:selected');
        if ($opt.length === 0) return null;
        const days = parseInt($opt.data('days'), 10) || 0;
        const name = ($opt.data('name') || $opt.text() || '').toString();
        const termType = parseInt($opt.data('type'), 10) || 0;
        const displayMode = parseInt($opt.data('display-mode'), 10) || 0;
        const displayText = ($opt.data('display-text') || '').toString();
        return { days, name, termType, displayMode, displayText };
    }

    const COUNTRY_NAMES = {
        'BE': 'België', 'NL': 'Nederland', 'FR': 'Frankrijk', 'DE': 'Duitsland',
        'LU': 'Luxemburg', 'GB': 'Verenigd Koninkrijk', 'UK': 'Verenigd Koninkrijk',
        'IT': 'Italië', 'ES': 'Spanje', 'PT': 'Portugal', 'AT': 'Oostenrijk',
        'CH': 'Zwitserland', 'PL': 'Polen', 'DK': 'Denemarken', 'SE': 'Zweden',
        'NO': 'Noorwegen', 'FI': 'Finland', 'IE': 'Ierland', 'US': 'Verenigde Staten'
    };

    function buildAddressLines(d) {
        if (!d) return [];
        const lines = [];
        // Issuer-formaat: addressLine1 / addressLine2 (voorgeformateerde regels)
        if (d.addressLine1) {
            lines.push(d.addressLine1);
            if (d.addressLine2) lines.push(d.addressLine2);
        } else {
            // Party-formaat: aparte velden
            const streetParts = [d.street, d.houseNumber, d.busNumber ? ('bus ' + d.busNumber) : null].filter(Boolean);
            if (streetParts.length) lines.push(streetParts.join(' '));
        }
        // Postcode + gemeente
        const cityParts = [d.postalCode, d.city].filter(Boolean);
        if (cityParts.length) lines.push(cityParts.join(' '));
        // Land: ISO-code omzetten naar volledige naam
        if (d.countryCode) {
            const code = d.countryCode.toUpperCase();
            lines.push(COUNTRY_NAMES[code] || d.countryCode);
        }
        return lines;
    }

    function buildPaymentTermsText(term, dt) {
        if ($('#IsPrepaid').is(':checked')) return 'Reeds voldaan';
        if (!term) return '';
        if (term.displayMode === 1 && term.displayText) return term.displayText;
        if (!dt) return '';
        const dueDate = calculateDueDate(dt, term.days, term.termType);
        if (!dueDate || dueDate === '—') return '';
        if (term.days > 0) return `Te betalen binnen ${term.days} dagen (vóór ${dueDate})`;
        return `Te betalen vóór ${dueDate}`;
    }

    function updateHeader() {
        // Titel
        $('#pvStartAs').text(readStartAsText());

        // Facturatiebedrijf
        const issuer = window.currentIssuerDetails;
        $('#pvIssuerName').text(issuer ? (issuer.name || '—') : ($('#IssuerCompanyId option:selected').text() || '—'));
        if (issuer) {
            const addrLines = buildAddressLines(issuer);
            $('#pvIssuerAddr').html(addrLines.map(l => esc(l)).join('<br>'));
            $('#pvIssuerEnterprise').text(issuer.enterpriseNumber ? ('Ondernemingsnr: ' + issuer.enterpriseNumber) : '');
        } else {
            $('#pvIssuerAddr').empty();
            $('#pvIssuerEnterprise').empty();
        }

        // Klant / leverancier
        const party = window.currentPartyDetails;
        if (party) {
            $('#pvPartyName').text(party.name || '');
            const pAddrLines = buildAddressLines(party);
            $('#pvPartyAddr').html(pAddrLines.map(l => esc(l)).join('<br>'));
            $('#pvPartyVat').text(party.vatNumber ? ('BTW: ' + party.vatNumber) : '');
        } else {
            $('#pvPartyName, #pvPartyAddr, #pvPartyVat').empty();
        }

        // Datums
        const dt = readDateStr();
        const term = getSelectedTermOption();
        const dueDate = term ? calculateDueDate(dt, term.days, term.termType) : '—';
        const paymentNote = term && term.displayMode === 1 && term.displayText ? term.displayText : null;
        $('#pvIssueDate').text(dt || '—');
        $('#pvDueDate').text(paymentNote || dueDate || '—');

        // Omschrijvingen
        $('#pvHeaderText').text(($('#HeaderDescription').val() || '').trim());
        $('#pvDetailText').text(($('#DetailDescription').val() || '').trim());

        // Footer + betaalvoorwaarden
        $('#pvFooterText').text(($('#FooterDescription').val() || '').trim());
        $('#pvPayTermText').text(buildPaymentTermsText(term, dt));
    }
    function roundCurrency(value) {
        const n = Number(value) || 0;
        const abs = Math.abs(n);
        const roundedAbs = Math.round((abs + Number.EPSILON) * 100) / 100;
        return n < 0 ? -roundedAbs : roundedAbs;
    }

    function addCurrency(a, b) {
        return roundCurrency((Number(a) || 0) + (Number(b) || 0));
    }

    function sectionTable() {
        const $wrap = $(`
      <div class="table-responsive mb-3">
        <table class="table table-striped mb-0" style="min-width:860px">
          <thead>
            <tr>
              <th>Omschrijving</th>
              <th class="text-end" style="width:100px">Aantal</th>
              <th class="text-end" style="width:140px">Eenheidsprijs</th>
              <th class="text-end" style="width:140px">Excl. BTW</th>
              <th class="text-end" style="width:90px">BTW %</th>
              <th class="text-end" style="width:140px">BTW</th>
              <th class="text-end" style="width:140px">Totaal</th>
            </tr>
          </thead>
          <tbody></tbody>
        </table>
      </div>
    `);
        $tables.append($wrap);
        return {
            pushRow(desc, qty, unit, excl, vatPerc) {
                const vatAmt = roundCurrency(excl * (vatPerc / 100.0));
                const tot = addCurrency(excl, vatAmt);
                const tr = $(`
          <tr>
            <td>${esc(desc)}</td>
            <td class="text-end">${qty ?? '-'}</td>
            <td class="text-end">${unit != null ? nf.format(unit) : '-'}</td>
            <td class="text-end">${nf.format(excl)}</td>
            <td class="text-end">${vatPerc != null ? String(vatPerc).replace('.', ',') : '-'}</td>
            <td class="text-end">${nf.format(vatAmt)}</td>
            <td class="text-end">${nf.format(tot)}</td>
          </tr>
        `);
                $wrap.find('tbody').append(tr);
                return { excl, vatAmt, tot };
            }
        };
    }

    function gatherStages(section, sign) {
        const rows = $('#stagesList input[type="checkbox"][name$=".IsSelected"].js-stage-row:checked');
        if (rows.length === 0) return { sub: 0, vat: 0, tot: 0, hadRows: false };

        let sub = 0, vat = 0, tot = 0;
        rows.each(function () {
            const $tr = $(this).closest('tr');
            const text = $tr.find('input[type="hidden"][name$=".Text"]').val() || '';
            const vatP = parseFloat(($tr.find('input[type="hidden"][name$=".VatPercentage"]').val() || '0').replace(',', '.')) || 0;
            const price = parseFloat(($tr.find('input[type="hidden"][name$=".Price"]').val() || '0').replace(',', '.')) || 0;

            const signedPrice = price * sign;
            const r = section.pushRow(text, null, null, signedPrice, vatP)
            sub = addCurrency(sub, r.excl); vat = addCurrency(vat, r.vatAmt); tot = addCurrency(tot, r.tot);
        });
        return { sub, vat, tot, hadRows: true };
    }

    function gatherChangeOrders(sign) {
        let sub = 0, vat = 0, tot = 0, hadAny = false;
        $('#coList .js-co-master:checked').each(function () {
            const coid = $(this).data('coid');
            const $block = $('#co_block_' + coid);
            const labelText = $(`label[for="${$(this).attr('id')}"]`).text();
            const title = ($(this).data('title') || labelText || `Wijzigingsopdracht #${coid}`).toString();
            const sec = sectionTable(title);
            let hasRows = false;

            $block.find('.js-co-pct').each(function () {
                const $pct = $(this);
                const pct = parseFloat(String($pct.val() || '0').replace(',', '.')) || 0;
                if (pct <= 0) return;

                const $row = $pct.closest('tr');
                const text = $row.find('input[type="hidden"][name$=".Text"]').val()
                    || $row.find('input.form-control[disabled]').val()
                    || '';
                const vatP = parseFloat(String($row.find('input[type="hidden"][name$=".VatPercentage"]').val() || '0').replace(',', '.')) || 0;
                const price = parseFloat(String($row.find('.js-co-price-post').val() || '0').replace(',', '.')) || 0;
                const qty = parseLocaleNumber($row.find('.js-co-qty').val());
                const uprice = parseLocaleNumber($row.find('.js-co-unitprice').val());

                const signedPrice = price * sign;
                const signedUnit = uprice != null ? uprice * sign : null;
                const r = sec.pushRow(text, qty, signedUnit, signedPrice, vatP);
                sub = addCurrency(sub, r.excl); vat = addCurrency(vat, r.vatAmt); tot = addCurrency(tot, r.tot); hasRows = true;
            });

            if (!hasRows) $('#pvTables').children().last().remove();
            else hadAny = true;
        });
        return { sub, vat, tot, hadRows: hadAny };
    }

    function gatherFreeLines(section, sign) {
        if (($('#Mode').val() ?? '1').toString() !== '1') return { sub: 0, vat: 0, tot: 0, hadRows: false };
        const dt = $.fn.dataTable.isDataTable('#freeLinesTable') ? $('#freeLinesTable').DataTable() : null;
        if (!dt) return { sub: 0, vat: 0, tot: 0, hadRows: false };

        let sub = 0, vat = 0, tot = 0, had = false;

        // Sorteer op verborgen volgorde-kolom (kolom 1)
        const idx = dt.rows().indexes().toArray();
        idx.sort((a, b) => {
            const va = parseInt(dt.cell(a, 1).data(), 10) || 0;
            const vb = parseInt(dt.cell(b, 1).data(), 10) || 0;
            return va - vb;
        });

        idx.forEach(i => {
            const $tr = $(dt.row(i).node());
            const text = ($tr.find('.js-fl-text').val() || '').trim();
            const price = parseLocaleNumber($tr.find('.js-fl-price').val());
            let vatP;

            // 1) per-rij select (voorkeur)
            const $sel = $tr.find('.js-fl-vat-select');
            if ($sel.length) {
                const $opt = $sel.find('option:selected');
                vatP = parseFloat(String(($opt.data('pct') ?? '0')).replace(',', '.')) || 0;
            } else {
                // 2) fallback voor oudere markup
                vatP = parseLocaleNumber($tr.find('.js-fl-vat').val());
            }

            // 3) laatste fallback: globaal BTW-type
            if (isNaN(vatP)) {
                vatP = parseFloat(String($('#VatTypeId option:selected').data('pct') || '0').replace(',', '.')) || 0;
            }

            if (!text && price === 0) return;
            const signedPrice = price * sign;
            const r = section.pushRow(text, 1, signedPrice, signedPrice, vatP);
            sub = addCurrency(sub, r.excl); vat = addCurrency(vat, r.vatAmt); tot = addCurrency(tot, r.tot); had = true;
        });

        return { sub, vat, tot, hadRows: had };
    }

    function rebuildPreview() {
        updateHeader();
        $tables.empty();

        let sub = 0, vat = 0, tot = 0, any = false;
        const sign = getCreditSign();

        const secStages = sectionTable();
        const st = gatherStages(secStages, sign);
        if (!st.hadRows) $tables.children().last().remove();
        sub = addCurrency(sub, st.sub); vat = addCurrency(vat, st.vat); tot = addCurrency(tot, st.tot);
        any = any || st.hadRows;

        const secFree = sectionTable();
        const fr = gatherFreeLines(secFree, sign);
        if (!fr.hadRows) $tables.children().last().remove();
        sub = addCurrency(sub, fr.sub); vat = addCurrency(vat, fr.vat); tot = addCurrency(tot, fr.tot);
        any = any || fr.hadRows;

        const co = gatherChangeOrders(sign);
        sub = addCurrency(sub, co.sub); vat = addCurrency(vat, co.vat); tot = addCurrency(tot, co.tot);
        any = any || co.hadRows;

        const roundedSub = roundCurrency(sub);
        const roundedVat = roundCurrency(vat);
        const roundedTotal = addCurrency(roundedSub, roundedVat);

        $sub.text(nf.format(roundedSub));
        $vat.text(nf.format(roundedVat));
        $total.text(nf.format(roundedTotal));

        const hasText = !!($('#HeaderDescription').val() || $('#DetailDescription').val() || $('#FooterDescription').val() || '').trim();
        const isSplitLayout = $('.invoice-compose-wrapper').length > 0;
        if (isSplitLayout) {
            $card.show().removeClass('d-none');
            $('#pvEmpty').toggle(!any && !hasText);
        } else {
            $card.toggle(any || hasText);
        }
    }

    // Triggers
    $(document).on('change input',
        '#IssuerCompanyId, #InvoiceDate, #HeaderDescription, #DetailDescription, #FooterDescription, #PaymentTermId, #Mode, #StartAs, input[name="StartAs"]',
        rebuildPreview
    );
    $(document).on('change', '#stagesList .js-stage-row, #stagesList .js-co-row, #stagesList .js-utl-row', rebuildPreview);
    $(document).on('change input', '#coList .js-co-master, #coList .js-co-pct, #coList .js-co-group-pct, #coList .js-co-override', rebuildPreview);
    $(document).on('change input', '#freeLineBlock input, #freeLineBlock select', rebuildPreview);
    $(document).on('change', '#IsCreditNote, #IsPrepaid', rebuildPreview);


    // Init + export
    rebuildPreview();
    window.rebuildInvoicePreview = rebuildPreview;
})();
