///* Add here all your JS customizations */
//jQuery(function ($) {
//    $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults
//});

jQuery(function ($) {
    const iconMap = {
        'fa-eye': 'bx-eye',
        'fa-eye-slash': 'bx-hide',
        'fa-search': 'bx-show',
        'fa-edit': 'bx-edit',
        'fa-pencil': 'bx-pencil',
        'fa-pen': 'bx-pencil',
        'fa-trash': 'bx-trash',
        'fa-trash-alt': 'bx-trash',
        'fa-times': 'bx-x',
        'fa-times-circle': 'bx-x-circle',
        'fa-check': 'bx-check',
        'fa-check-circle': 'bx-check-circle',
        'fa-plus': 'bx-plus',
        'fa-plus-circle': 'bx-plus-circle',
        'fa-minus': 'bx-minus',
        'fa-download': 'bx-download',
        'fa-upload': 'bx-upload',
        'fa-file': 'bx-file',
        'fa-file-pdf': 'bx-file',
        'fa-file-excel': 'bx-file',
        'fa-print': 'bx-printer',
        'fa-info-circle': 'bx-info-circle',
        'fa-exclamation-triangle': 'bx-error',
        'fa-user': 'bx-user',
        'fa-users': 'bx-group',
        'fa-envelope': 'bx-envelope',
        'fa-phone': 'bx-phone',
        'fa-calendar': 'bx-calendar',
        'fa-sync': 'bx-refresh',
        'fa-redo': 'bx-redo',
        'fa-undo': 'bx-undo'
    };

    const normalizeIcon = (iconClass) => iconClass?.trim().toLowerCase();

    const replaceWithBoxIcon = ($icon) => {
        const classList = ($icon.attr('class') || '').split(/\s+/);
        const hasBoxIcon = classList.some((cls) => cls.startsWith('bx'));

        if (hasBoxIcon) {
            $icon.addClass('fs-5');
            return;
        }

        const faClass = classList.find((cls) => cls.startsWith('fa-'));
        const normalizedFa = normalizeIcon(faClass);
        const boxIcon = iconMap[normalizedFa] || 'bx-dots-horizontal-rounded';

        $icon.attr('class', `bx ${boxIcon} fs-5`);
    };

    const applyActionColumnStyles = (table) => {
        const $table = $(table);
        const $headers = $table.find('thead th');
        const actionIndex = $headers.toArray().findIndex((header) => $(header).text().trim().toLowerCase() === 'acties');

        if (actionIndex === -1) {
            return;
        }

        $headers.eq(actionIndex).addClass('datatable-actions');
        $table.find('tbody tr').each((_, row) => {
            const $cell = $(row).children().eq(actionIndex);
            $cell.addClass('datatable-actions');
            $cell.find('i').each((_, icon) => replaceWithBoxIcon($(icon)));
        });
    };

    const applyToAllTables = () => {
        $('table').each((_, table) => applyActionColumnStyles(table));
    };

    applyToAllTables();
    $(document).on('draw.dt', 'table.dataTable', function () {
        applyActionColumnStyles(this);
    });
});
