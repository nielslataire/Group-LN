@modeltype DetailDocsModel 
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
            <div class="mt-xlg">
                @*<div id="toolbar">
                    <div class="btn-group">
                        <button type="button" class="mr-xs ml-xs btn btn-default dropdown-toggle" data-toggle="dropdown"><i class="fa fa-file-pdf-o"></i> <span class="caret"></span></button>
                        <ul class="dropdown-menu" role="menu">
                            <li><a href="#modalgiftspdf" id="btnGiftsPDF" data-id="@Model.ProjectId">Toegiften</a></li>
                            <li><a href="@Url.Action("DetailClientsListPdf", "Projecten", New With {.id = Model.ProjectId})">Klantenlijst</a></li>
                        </ul>
                    </div>
                    <button type="button" data-toggle="tooltip" data-placement="top" title="Adrukken" onclick="window.open('@Url.Action("DetailClientsListPrint", "Projecten", New With {.id = Model.ProjectId})')" class="mr-xs btn btn-default hidden-tablet hidden-phone"><i class="fa fa-print"></i></button>
                </div>*@
                @Html.Partial("Docs", Model)
                @Html.Partial("ModalAddDocument", New BO.ProjectDocBO With {.ProjectId = Model.ProjectId, .ClientAccountId = Model.ClientAccountId, .Type = BO.ProjectDocType.Sales})


            </div>








        <!-- end: page -->

    </div>
</div>
<div id="modaldeletedoc" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-doc-container"></div>
</div>

@section scripts

<script>

      
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
      
        
    });
    $('.deletedoc').click(function () {
        var url = "/Klanten/ModalDeleteDoc"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-doc-container').html(data);
        });
    });
    
</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-table/bootstrap-table.js"></script>



end section

