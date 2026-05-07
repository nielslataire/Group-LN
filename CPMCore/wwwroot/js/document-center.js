/* ============================================================
   DocumentenCentrum — document-center.js
   Afhankelijkheden: jQuery, Bootstrap 5
   ============================================================ */

(function ($) {
    'use strict';

    /* ── Bulk selection ──────────────────────────────────────── */

    var DC = window.DC = window.DC || {};

    DC.initBulkSelect = function () {
        var $checkAll = $('#checkAll');
        var $bar = $('#bulkActionBar');
        var $label = $('#bulkCountLabel');
        var $idsField = $('#bulkIds');

        function getChecked() {
            return $('.js-row-check:checked');
        }

        function updateBar() {
            var $checked = getChecked();
            var count = $checked.length;

            if (count > 0) {
                $bar.removeClass('d-none').addClass('d-flex');
                $label.text(count + ' geselecteerd');
                var ids = $checked.map(function () { return this.value; }).get().join(',');
                $idsField.val(ids);
            } else {
                $bar.addClass('d-none').removeClass('d-flex');
                $idsField.val('');
            }

            var total = $('.js-row-check').length;
            if ($checkAll.length) {
                $checkAll.prop('indeterminate', count > 0 && count < total);
                $checkAll.prop('checked', count > 0 && count === total);
            }
        }

        $checkAll.on('change', function () {
            $('.js-row-check').prop('checked', this.checked);
            updateBar();
        });

        $(document).on('change', '.js-row-check', updateBar);

        // Klikbare rijen
        $(document).on('click', '.js-doc-row', function (e) {
            if ($(e.target).closest('.js-no-row-click').length) return;
            var href = $(this).data('href');
            if (href) window.location.href = href;
        });
    };

    /* ── Bulk actie forms ────────────────────────────────────── */

    DC.initBulkForms = function () {
        $('#btnBulkApprove').on('click', function () {
            var ids = $('#bulkIds').val();
            if (!ids) { alert('Geen documenten geselecteerd.'); return; }
            var notes = prompt('Optionele opmerking (enter voor geen):') || '';
            $('#bulkApproveIdsInput').val(ids);
            $('#bulkApproveNotesInput').val(notes);
            $('#formBulkApprove').submit();
        });

        $('#btnBulkReject').on('click', function () {
            var ids = $('#bulkIds').val();
            if (!ids) { alert('Geen documenten geselecteerd.'); return; }
            var notes = prompt('Reden voor afkeuring (verplicht):');
            if (notes === null) return;
            $('#bulkRejectIdsInput').val(ids);
            $('#bulkRejectNotesInput').val(notes);
            $('#formBulkReject').submit();
        });
    };

    /* ── Toewijzen modal ─────────────────────────────────────── */

    DC.initAssignModal = function () {
        $(document).on('click', '.js-btn-assign', function (e) {
            e.stopPropagation();
            var id = $(this).data('id');
            $('#assignInvoiceId').val(id);
            var modal = new bootstrap.Modal(document.getElementById('assignModal'));
            modal.show();
        });

        // Gebruikersfilter in modal
        $('#assignUserSearch').on('input', function () {
            var q = $(this).val().toLowerCase();
            $('#assignUserList .dc-user-item').each(function () {
                var name = $(this).data('name').toLowerCase();
                $(this).toggle(name.indexOf(q) >= 0);
            });
        });

        // Gebruikerskeuze
        $(document).on('click', '.dc-user-item', function () {
            var userId = $(this).data('id');
            var userName = $(this).data('name');
            $('#assignUserId').val(userId);
            $('#assignUserName').val(userName);
            $('.dc-user-item').removeClass('active');
            $(this).addClass('active');
            $('#assignSelectedName').text(userName);
        });
    };

    /* ── Upload modal ────────────────────────────────────────── */

    DC.initUploadModal = function () {
        var $dropzone = $('.dc-dropzone');
        var $fileInput = $('#uploadFileInput');
        var $fileName = $('#uploadFileName');

        $dropzone.on('click', function () { $fileInput.trigger('click'); });

        $fileInput.on('change', function () {
            var file = this.files[0];
            if (file) {
                $fileName.text(file.name);
                $dropzone.addClass('has-file');
                $dropzone.find('.dc-dz-icon').hide();
                $dropzone.find('.dc-dz-label').text('Bestand geselecteerd');
            }
        });

        $dropzone.on('dragover', function (e) {
            e.preventDefault();
            $(this).addClass('drag-over');
        }).on('dragleave', function () {
            $(this).removeClass('drag-over');
        }).on('drop', function (e) {
            e.preventDefault();
            $(this).removeClass('drag-over');
            var file = e.originalEvent.dataTransfer.files[0];
            if (file) {
                var dt = new DataTransfer();
                dt.items.add(file);
                $fileInput[0].files = dt.files;
                $fileName.text(file.name);
            }
        });
    };

    /* ── Cost context toggle (detail) ────────────────────────── */

    DC.initCostContextToggle = function () {
        function showPanel(ctx) {
            $('.dc-ctx-panel').hide();
            $('.dc-ctx-panel[data-ctx="' + ctx + '"]').show();
            $('.dc-ctx-btn').removeClass('active');
            $('.dc-ctx-btn[data-ctx="' + ctx + '"]').addClass('active');
        }

        var initial = $('#costContextCurrent').val() || 'Unknown';
        showPanel(initial);

        $(document).on('click', '.dc-ctx-btn', function () {
            showPanel($(this).data('ctx'));
        });
    };

    /* ── Attachment switcher (detail) ────────────────────────── */

    DC.initAttachmentSwitcher = function () {
        $(document).on('click', '.dc-att-thumb', function () {
            var url = $(this).data('url');
            if (!url) return;
            $('#mainAttachmentFrame').attr('src', url);
            $('.dc-att-thumb').removeClass('active');
            $(this).addClass('active');
        });
    };

    /* ── Sticky action bar padding ───────────────────────────── */

    DC.initStickyBar = function () {
        var $bar = $('.dc-sticky-bar');
        if (!$bar.length) return;
        $('body').css('padding-bottom', $bar.outerHeight() + 16 + 'px');
    };

    /* ── Initialiseer alles op DOMContentLoaded ──────────────── */

    $(function () {
        if ($('#documentsTable').length) {
            DC.initBulkSelect();
            DC.initBulkForms();
            DC.initAssignModal();
            DC.initUploadModal();
        }
        if ($('.dc-cost-context-toggle').length) {
            DC.initCostContextToggle();
        }
        if ($('.dc-att-thumb').length) {
            DC.initAttachmentSwitcher();
        }
        if ($('.dc-sticky-bar').length) {
            DC.initStickyBar();
            DC.initAssignModal();
        }
    });

}(jQuery));
