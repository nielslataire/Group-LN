@modeltype DetailContractsModel
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
                    @Html.Partial("Contracts", Model)
                </div>
            </div>
        </section>



        <!-- end: page -->

    </div>
</div>
<div id="ModalDeleteContract" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-contract-container"></div>
</div>
@section scripts

    <script>

                    $('.deleteContract').click(function () {
                        var url = "/Projecten/ModalDeleteContract"; // the url to the controller
                        var id = $(this).attr('data-id'); // the id that's given to each button in the list
                        $.get(url + '/' + id, function (data) {
                            $('#delete-contract-container').html(data);
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
                        $('.deleteContract').magnificPopup({
                            type: 'inline',
                            src: 'deleteContract',
                        });

                    });

    </script>

    <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
    <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
    <script src="~/Scripts/admin/pages/examples.mediagallery.js"></script>
    <script src="~/vendor/admin/bootstrap-table/bootstrap-table.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
    <script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>

end section

