// Voorbeeldfactuur (preview)
(function () {
    const { nf, parseLocaleNumber, esc, addDays } = window.InvoicesUtil;

    const $card = $('#previewCard');
    const $tables = $('#pvTables');
    const $sub = $('#pvSub');
    const $vat = $('#pvVat');
    const $total = $('#pvTotal');

    function readDateStr() {
        const v = $('#InvoiceDate').val();
        return v && v.trim() ? v.trim() : null;
    }

    function readStartAsText() {
        let v = $('#StartAs').val();
        if (v == null) v = $('input[name="StartAs"]:checked').val();
        v = (v ?? '').toString().toLowerCase();
        if (v === 'invoice' || v === '1') return 'Factuur';
        if (v === 'draft' || v === '0' || v === 'concept') return 'Concept';
        return 'Concept';
    }

    function getSelectedTermOption() {
        const $opt = $('#PaymentTermId option:selected');
        if ($opt.length === 0) return null;
        const days = parseInt($opt.data('days'), 10) || 0;
        const name = ($opt.data('name') || $opt.text() || '').toString();
        return { days, name };
    }

    function updateHeader() {
        const issuerTxt = $('#IssuerCompanyId option:selected').text() || '';
        $('#pvIssuer').text(issuerTxt || '—');

        const mode = $('#Mode').val();
        $('#pvMode').text({ '1': 'Vrije lijnen', '2': 'Schijven', '3': 'Wijzigingsopdrachten', '4': 'Nutsaansluitingen' }[mode] || '—');

        const dt = readDateStr();
        const term = getSelectedTermOption();

        $('#pvIssueDate').text(dt || '—');
        $('#pvPayTerm').text(term ? term.name : '—');
        $('#pvDueDate').text(addDays(dt, term ? term.days : 0));

        const hdr = ($('#HeaderDescription').val() || '').trim();
        $('#pvHeaderText').text(hdr);

        $('#pvStartAs').text(`${readStartAsText()}`);
    }

    function sectionTable(title) {
        const $wrap = $(`
      <div class="table-responsive mb-3">
        <div class="d-flex justify-content-between align-items-center">
          <h5 class="mb-2">${esc(title)}</h5>
        </div>
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
                const vatAmt = Math.round((excl * (vatPerc / 100.0)) * 100) / 100;
                const tot = excl + vatAmt;
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

    function gatherStages(section) {
        const rows = $('#stagesList input[type="checkbox"][name$=".IsSelected"].js-stage-row:checked');
        if (rows.length === 0) return { sub: 0, vat: 0, tot: 0, hadRows: false };

        let sub = 0, vat = 0, tot = 0;
        rows.each(function () {
            const $tr = $(this).closest('tr');
            const text = $tr.find('input[type="hidden"][name$=".Text"]').val() || '';
            const vatP = parseFloat(($tr.find('input[type="hidden"][name$=".VatPercentage"]').val() || '0').replace(',', '.')) || 0;
            const price = parseFloat(($tr.find('input[type="hidden"][name$=".Price"]').val() || '0').replace(',', '.')) || 0;

            const r = section.pushRow(text, null, null, price, vatP);
            sub += r.excl; vat += r.vatAmt; tot += r.tot;
        });
        return { sub, vat, tot, hadRows: true };
    }

    function gatherChangeOrders() {
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

                const r = sec.pushRow(text, qty, uprice, price, vatP);
                sub += r.excl; vat += r.vatAmt; tot += r.tot; hasRows = true;
            });

            if (!hasRows) $('#pvTables').children().last().remove();
            else hadAny = true;
        });
        return { sub, vat, tot, hadRows: hadAny };
    }

    function gatherFreeLines(section) {
        if (!$('#freeLineBlock').is(':visible')) return { sub: 0, vat: 0, tot: 0, hadRows: false };
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
            const vatP = parseLocaleNumber($tr.find('.js-fl-vat').val());
            if (!text && price === 0) return;
            const r = section.pushRow(text, 1, price, price, vatP);
            sub += r.excl; vat += r.vatAmt; tot += r.tot; had = true;
        });

        return { sub, vat, tot, hadRows: had };
    }

    function rebuildPreview() {
        updateHeader();
        $tables.empty();

        let sub = 0, vat = 0, tot = 0, any = false;

        const secStages = sectionTable('Schijven');
        const st = gatherStages(secStages);
        if (!st.hadRows) $tables.children().last().remove();
        sub += st.sub; vat += st.vat; tot += st.tot;
        any = any || st.hadRows;

        const secFree = sectionTable('Vrije lijnen');
        const fr = gatherFreeLines(secFree);
        if (!fr.hadRows) $tables.children().last().remove();
        sub += fr.sub; vat += fr.vat; tot += fr.tot;
        any = any || fr.hadRows;

        const co = gatherChangeOrders();
        sub += co.sub; vat += co.vat; tot += co.tot;
        any = any || co.hadRows;

        $sub.text(nf.format(sub));
        $vat.text(nf.format(vat));
        $total.text(nf.format(tot));

        const hdr = ($('#HeaderDescription').val() || '').trim();
        $card.toggle(any || hdr.length > 0);
    }

    // Triggers
    $(document).on('change input',
        '#IssuerCompanyId, #InvoiceDate, #HeaderDescription, #PaymentTermId, #Mode, #StartAs, input[name="StartAs"]',
        rebuildPreview
    );
    $(document).on('change', '#stagesList .js-stage-row, #stagesList .js-co-row, #stagesList .js-utl-row', rebuildPreview);
    $(document).on('change input', '#coList .js-co-master, #coList .js-co-pct, #coList .js-co-group-pct, #coList .js-co-override', rebuildPreview);
    $(document).on('change input', '#freeLineBlock input', rebuildPreview);

    // Init + export
    rebuildPreview();
    window.rebuildInvoicePreview = rebuildPreview;
})();
