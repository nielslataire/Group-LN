@modeltype DetailInsurancesModel 
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
<style>
#toolbar {
    vertical-align: bottom;
}
</style>
  <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />

End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu content-with-menu-has-toolbar media-gallery">
            <div class="content-with-menu-container">
                <div class="inner-menu-toggle">
                    <a href="#" class="inner-menu-expand" data-open="inner-menu">
                        Toon Menu <i class="fa fa-chevron-right"></i>
                    </a>
                </div>

                <menu id="content-menu" class="inner-menu" role="menu">
                    <div class="nano">
                        <div class="nano-content">

                            <div class="inner-menu-toggle-inside">
                                <a href="#" class="inner-menu-collapse">
                                    <i class="fa fa-chevron-up visible-xs-inline"></i><i class="fa fa-chevron-left hidden-xs-inline"></i> Verberg Menu
                                </a>
                                <a href="#" class="inner-menu-expand" data-open="inner-menu">
                                    Toon Menu <i class="fa fa-chevron-down"></i>
                                </a>
                            </div>
                            <div class="inner-menu-content">
                                <div class="sidebar-widget m-none">
                                    <div class="widget-header clearfix">
                                        <a href="#Project" data-toggle="tab"> <h5 class="title pull-left mt-xs">Projectmenu</h5></a>
                                    </div>
                                    <div class="widget-content">
                                        <ul class="mg-folders">
                                            @Html.Partial("DetailMenu", Model.ProjectId)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>

                <div class="inner-body mg-main">
                    @Html.Partial("Insurances", Model)
                </div>




            </div>
        </section>



        <!-- end: page -->

    </div>
</div>
<div id="modaleditinsurance" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-insurance-container"></div>
</div>
<div id="modaldeleteinsurance" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-insurance-container"></div>
</div>
<div id="modalendinsurance" class="modal-block modal-block-warning mfp-hide">
    <div id="end-insurance-container"></div>
</div>
<div id="ModalAddInsurance" class="modal-block modal-block-primary mfp-hide">
    <div id="add-insurance-container"></div>
</div>


@section scripts

<script>
    $('.editinsurance').click(function () {
        var url = "/Projecten/ModalEditInsurance"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-insurance-container').html(data);
        });
    });
    $('.deleteinsurance').click(function () {
        var url = "/Projecten/ModalDeleteInsurance"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-insurance-container').html(data);
        });
    });
    $('.endinsurance').click(function () {
        var url = "/Projecten/ModalEndInsurance"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#end-insurance-container').html(data);
        });
    });
    $('.addInsurance').click(function () {
        var url = "/Projecten/ModalAddInsurance"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#add-insurance-container').html(data);
        });
    });
      
    $(window).load(function () {
        @If Not TempData("Message") Is Nothing Then
        @<text>

        new PNotify({
            title:      '@TempData("MessageTitle")',
            text:       '@TempData("Message")',
            type:       '@TempData("MessageType")'
        });
        </text>
        End If
    });


    $(document).ready(function () {
        $('.addInsurance').magnificPopup({
            type: 'inline',
            src: 'addInsurance',
            modal: true
        });
    });
    
</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-table/bootstrap-table.js"></script>



end section

