
@modeltype DetailNewsModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/Content/theme-blog.css" >

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
                    @Html.Partial("News", Model)
                   
                    @Html.Partial("ModalAddNews", New BO.ProjectNewsBO With {.ProjectId = Model.ProjectId, .NewsDate = Date.Now().Date})


                               </div>
            </div>
        </section>



        <!-- end: page -->

    </div>
</div>
<div id="ModalDeleteNews" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-news-container"></div>
</div>
<div id="ModalEditNews" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-news-container"></div>
</div>

@section scripts

<script>
    $('.deleteNews').click(function () {
        var url = "/Projecten/ModalDeleteNews"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-news-container').html(data);
        });
    });
    $('.editNews').click(function () {
        var url = "/Projecten/ModalEditNews"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-news-container').html(data);
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
        $('.deleteNews').magnificPopup({
            type: 'inline',
            src: 'deleteNews',
        });
        $('.editNews').magnificPopup({
            type: 'inline',
            src: 'editNews',
        });
        
    });
    
</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
end section

