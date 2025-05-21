/* Add here all your JS customizations */
$(document).on('submit', '#UploadImageForm', function () {
    $('#UploadImageForm').trigger('loading-overlay:show').find('.loading-overlay').css('background', '#FFF').css('z-index', 2);
});