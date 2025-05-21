@modeltype DetailClientsModel 
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
    <link rel="stylesheet" href="~/Content/theme-blog.css">
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-table/bootstrap-table.css" />

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
                    <div id="toolbar">
                        <div class="btn-group">
                            <button type="button" class="mr-xs ml-xs btn btn-default dropdown-toggle" data-toggle="dropdown"><i class="fa fa-file-pdf-o"></i> <span class="caret"></span></button>
                            <ul class="dropdown-menu" role="menu">
                                <li><a href="#modalgiftspdf" id="btnGiftsPDF" data-id="@Model.ProjectId">Toegiften</a></li>
                                <li><a href="@Url.Action("DetailClientsListPdf", "Projecten", New With {.id = Model.ProjectId})">Klantenlijst</a></li>
                                @*<li><a href="@Url.Action("DetailClientsListPdf", "Leveranciers", New With {.id = Model.ProjectId})">Toegiften</a></li>*@
                            </ul>
                            <button type="button" data-toggle="tooltip" data-placement="top" title="Adrukken" onclick="window.open('@Url.Action("DetailClientsListPrint", "Projecten", New With {.id = Model.ProjectId})')" class="mr-xs btn btn-default hidden-tablet hidden-phone hidden-xs "><i class="fa fa-print"></i></button>
                        </div>
                        @*<button type="button" data-toggle="tooltip" data-placement="top" title="Naar PDF" onclick="location.href='@Url.Action("DetailClientsListPdf", "Projecten", New With {.id = Model.ProjectId})'" class="mb-xs mt-xs mr-xs btn btn-default"><i class="fa fa-file-pdf-o"></i></button>*@
                    </div>
                    @Html.Partial("Clients", Model)

                    

                </div>




            </div>
        </section>



        <!-- end: page -->

    </div>
</div>

<div id="ModalDeleteUnit" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-unit-container"></div>
</div>
<div id="ModalAddLevel" class="modal-block modal-block-primary mfp-hide">
    <div id="add-level-container"></div>
</div>
<div id="ModalEditNews" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-news-container"></div>
</div>
<div id="modaldeleteclient" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-client-container"></div>
</div>  
<div id="modalgiftspdf" class="modal-block modal-block-warning mfp-hide">
    <div id="gifts-pdf-container"></div>
</div> 
@section scripts

<script>

        $('#lstGroupTypes').on('change', function () {
          
            var val = $(this).val();
            if(val){
               
                var subitems = "";
                $.getJSON("@Url.Action("GetSubType","Projecten")", { id: val }, function (data) {
                    $.each(data,function(index, item){
                        subitems+="<option value='"+item.Value+"'>"+item.Text+"</option>"
                    });
                    $('#lstTypes').html(subitems);
                    $('#lstTypes').attr("disabled", false);
                });
            }
            else {
               
                $('#lstTypes').attr("disabled", true);
            }
           
        });

    $('.deleteUnit').click(function () {
        var url = "/Projecten/ModalDeleteUnit"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-unit-container').html(data);
        });
    });
    $('.editNews').click(function () {
        var url = "/Projecten/ModalEditNews"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-news-container').html(data);
        });
    });
    $('#addLevel').click(function () {
        var url = "/Projecten/ModalAddLevel"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#add-level-container').html(data);
        });
    });
    $('.deleteClient').click(function () {
        alert(test);
        var url = "/Klanten/DeleteClientModal"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {

            $('#delete-client-container').html(data);
        });
    });
    $('#btnGiftsPDF').click(function () {
        var url = "/Projecten/ModalDetailClientsGiftsPdf"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#gifts-pdf-container').html(data);
        });

    });
    $('.facebookNews').click(function () {
        $.ajax({
            url: '/Projecten/PlaceFacebookNews',
            data: { newsid: $(this).attr('data-id') },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {
                //$('#txtCountryIsoCode').val(result);
            },

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
        $('.deleteUnit').magnificPopup({
            type: 'inline',
            src: 'deleteUnit',
        });
        $('.editNews').magnificPopup({
            type: 'inline',
            src: 'editNews',
        });
        $('#addLevel').magnificPopup({
            type: 'inline',
            src: 'addLevel',
        });
        $('.deleteClient').magnificPopup({
            type: 'inline',
            src: 'deleteClient',
        });
        $('#btnGiftsPDF').magnificPopup({
            type: 'inline',
            src: 'btnGiftsPDF',
        });
        
    });
    
</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-table/bootstrap-table.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
@*<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
<script src="~/scripts/admin/tables/examples.datatables.default.js"></script>*@


end section

