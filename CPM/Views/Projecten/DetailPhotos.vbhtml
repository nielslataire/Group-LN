
@modeltype DetailPhotosModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

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
                    @Html.Partial("Photos", Model)
                    
                    @Html.Partial("ModalAddPhoto", New BO.ProjectPictureBO With {.ProjectId = Model.ProjectId, .Type = BO.PictureType.Werffoto})
                </div>
            </div>
        </section>

                @*<div class="tabs">
                    <ul class="nav nav-tabs" id="mytabs">
                        <li class="active">
                            <a href="#Project" data-toggle="tab">Project</a>
                        </li>
                        <li>
                            <a href="#Photos" data-toggle="tab">Foto's</a>
                        </li>

                        <li>
                            <a href="#New" data-toggle="tab">Nieuws</a>
                        </li>

                      

                    </ul>


                </div>*@

        <!-- end: page -->

    </div>
</div>
<div id="ModalDeletePhoto" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-photo-container"></div>
</div>
@*<div id="modaladdpicture" class="modal-block modal-block-primary mfp-hide">
        @Html.Partial("ModalAddPicture")

    </div>*@

@section scripts

<script>
    $('.deletePhoto').click(function () {
        var url = "/Projecten/ModalDeletePhoto"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-photo-container').html(data);
        });
    });
    $('.FacebookPhoto').click(function () {
        var url = "/Projecten/FacebookPhoto"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.post(url + '/' + id, function (data) {
          
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

    function EditGeneralData(val)
    {
        document.getElementById('txtEditGeneralData').value = val;

        //and probably call document.forms[0].submit();
    }

 
    $("#GeneralDataSave").click(function (e) {
        e.preventDefault();
        //Show loading display here
        var form = $("#FormGeneralData");
        $.ajax({
            url: '@Url.Action("SaveGeneralData")',
            data: form.serialize(),
            type: 'POST',
            success: function (response) {
                window.location.href = response.Url;
            }
        });
    });
    $(document).ready(function () {
        $('.deletePhoto').magnificPopup({
            type: 'inline',
            src: 'deletePhoto',
        });
        $(".lightbox").each(function () {
            if ($(this).css('display') == "block") {
                $(this).hide();
            }
        });
    });
    
</script>

<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
end section

