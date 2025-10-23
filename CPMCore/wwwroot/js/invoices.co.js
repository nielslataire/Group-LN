// Wijzigingsopdrachten (CO) UI-logica
(function () {
    const $root = $('#coList');
    const nf = new Intl.NumberFormat('nl-BE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function clampPct(val) {
        let n = parseFloat(String(val).replace(',', '.'));
        if (isNaN(n)) n = 0;
        return Math.min(100, Math.max(0, n));
    }

    function recalcRowByPctInput($pctInput) {
        const base = parseFloat($pctInput.data('base')) || 0;
        const pct = clampPct($pctInput.val());
        const price = Math.round(base * (pct / 100.0) * 100) / 100;
        const $row = $pctInput.closest('tr');
        $row.find('.js-co-price-view').val(nf.format(price));
        $row.find('.js-co-price-post').val(price.toString().replace(',', '.'));
        $row.find('.js-co-pct-post').val(pct.toString().replace(',', '.'));
    }

    function refreshPreview() {
        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    }

    $root.on('change', '.js-co-master', function () {
        const coid = $(this).data('coid');
        const checked = this.checked;
        const $block = $('#co_block_' + coid);
        const $wrap = $root.find('.js-co-master-pct-wrap[data-coid="' + coid + '"]');
        const $masterPct = $root.find('.js-co-master-pct[data-coid="' + coid + '"]');
        const masterPct = clampPct($masterPct.val());

        $wrap.toggleClass('d-none', !checked);

        if (checked) {
            $block.show();
            $block.find('.js-is-selected').val('true');
            $block.find('.js-co-ov').each(function () {
                const $pct = $(this).closest('.input-group').find('.js-co-pct');
                if (!$(this).is(':checked')) {
                    $pct.val(masterPct);
                    recalcRowByPctInput($pct);
                }
            });
        } else {
            $block.hide();
            $block.find('.js-is-selected').val('false');
            $block.find('.js-co-price-view').val(nf.format(0));
            $block.find('.js-co-price-post').val('0');
            $block.find('.js-co-pct-post').val('0');
        }
        refreshPreview();
    });

    $root.on('input change', '.js-co-master-pct', function () {
        const coid = $(this).data('coid');
        const pct = clampPct($(this).val());
        $(this).val(pct);
        const $block = $('#co_block_' + coid);
        if ($block.is(':visible')) {
            $block.find('.js-co-ov').each(function () {
                const $pct = $(this).closest('.input-group').find('.js-co-pct');
                if (!$(this).is(':checked')) {
                    $pct.val(pct);
                    recalcRowByPctInput($pct);
                }
            });
        }
        refreshPreview();
    });

    $root.on('change', '.js-co-ov', function () {
        const coid = $(this).data('coid');
        const $pct = $(this).closest('.input-group').find('.js-co-pct');
        const masterPct = clampPct($root.find('.js-co-master-pct[data-coid="' + coid + '"]').val());
        if (this.checked) {
            $pct.prop('disabled', false).focus().select();
            recalcRowByPctInput($pct);
        } else {
            $pct.val(masterPct).prop('disabled', true);
            recalcRowByPctInput($pct);
        }
        refreshPreview();
    });

    $root.on('input change', '.js-co-pct', function () {
        recalcRowByPctInput($(this));
        refreshPreview();
    });

    function initCoUi() {
        $root.find('.js-co-master').each(function () {
            const coid = $(this).data('coid');
            const checked = this.checked;
            $('#co_block_' + coid).toggle(checked);
            $root.find('.js-co-master-pct-wrap[data-coid="' + coid + '"]').toggleClass('d-none', !checked);
        });

        $root.find('.co-block').each(function () {
            const coid = $(this).data('coid');
            const masterPct = clampPct($root.find('.js-co-master-pct[data-coid="' + coid + '"]').val());
            $(this).find('.js-co-ov').prop('checked', false);
            $(this).find('.js-co-pct').each(function () {
                const $pct = $(this);
                $pct.prop('disabled', true).val(masterPct);
                recalcRowByPctInput($pct);
            });
        });

        if (window.rebuildInvoicePreview) window.rebuildInvoicePreview();
    }

    window.initCoUi = initCoUi;
})();
