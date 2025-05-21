@imports bo
@modeltype ProjectChangeOrderModel 
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
<style>
#toolbar {
    vertical-align: bottom;
}
    table .collapse.in {
	display:table-row;
}
</style>
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-table/bootstrap-table.css" />
    <link rel="stylesheet" href="~/vendor/admin/jquery-datatables-bs3/assets/css/datatables.css" />

End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu  content-with-menu-has-toolbar media-gallery">
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

                    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
                    <div class="inner-toolbar clearfix">
                        <ul>
                                    <li>
                                        <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Toevoegen wijzigingsopdracht" href="@Url.Action("ChangeOrderAddUpdate", "Projecten", New With {.projectid = Model.ProjectId})" class="ChangeOrderDetail"><i class="fa fa-plus"></i></a>
                                    </li>
                        </ul>
                    </div>
                    <section class="panel">
                        <header class="panel-heading">
                            <h2 class="panel-title">Wijzigingsopdrachten</h2>
                        </header>
                        <div class="panel-body">
                                <table class="table table-bordered table-striped mb-none" id="datatable-details">
                                    <thead>
                                        <tr>
                                            <th width="20px">&nbsp;</th>
                                            <th>Klant</th>
                                            <th width="100%">Omschrijving</th>
                                            <th>Datum</th>
                                            <th>Vervaldatum</th>
                                            <th>Verzonden</th>
                                            <th>Akkoord</th>
                                            <th>Factuur&nbsp;door&nbsp;bouwheer</th>
                                            <th>Te Fact.</th>
                                            <th class="text-right ">Acties</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        @For Each Order In Model.ChangeOrders

                                        @<text>
                                            <tr class="clickable active" data-toggle="collapse" id="@Order.Id" data-target=".@Order.Id">
                                                <td class="text-center" width="20px"><img src="~/img/details_open.png" alt="expand/collapse" rel="@Order.ID"/></td>
                                                <td style="white-space: nowrap;">@Order.ClientName </td>
                                                <td width="100%">@Order.Description</td>
                                                <td style="white-space: nowrap;">@Html.DisplayFor(Function(m) Order.ChangeOrderDate)</td>
                                                <td style="white-space: nowrap;">@Html.DisplayFor(Function(m) Order.ExpirationDate)</td>
                                                <td style="white-space: nowrap;">@if Not Order.DateSendToClient Is Nothing Then @Html.DisplayFor(Function(m) Order.DateSendToClient) end If </td>
                                                <td style="white-space: nowrap;">@if Not Order.DateAgreement Is Nothing Then @Html.DisplayFor(Function(m) Order.DateAgreement) end If </td>
                                                <td style="white-space: nowrap;">@Html.DisplayFor(Function(m) Order.Invoiceable)</td>
                                                <td style="white-space: nowrap;">@If Order.Invoiceable = True AndAlso Not Order.DateAgreement Is Nothing AndAlso Order.Details.Where(Function(m) m.Invoicable = True).Count = Order.Details.Count Then@<Text> <input type="checkbox" data-groupid="@Order.Id" data-state="checked" class="chkbox" checked="checked"/></text> elseIf Order.Invoiceable = True AndAlso Not Order.DateAgreement Is Nothing AndAlso Order.Details.Where(Function(m) m.Invoicable = True).Count > 0 Then@<Text> <input type="checkbox" data-groupid="@Order.Id" data-state="indeterminate" class="chkbox"/></text>elseIf Order.Invoiceable = True AndAlso Not Order.DateAgreement Is Nothing Then@<Text> <input type="checkbox" data-groupid="@Order.Id" data-state="unchecked" class="chkbox" /></text>  End If </td>
                                                <td style="white-space: nowrap;" class="text-right actions" data-title="Acties"><a data-toggle="tooltip" data-placement="top" title="" data-original-title="PDF" href="@Url.Action("ChangeOrderPDF", "Projecten", New With {.changeorderid = Order.Id})"><i Class="fa fa-file-pdf-o fa-lg"></i></a>&nbsp;<a data-toggle="tooltip" data-placement="top" title="" data-original-title="Dupliceren" href="@Url.Action("ChangeOrderAddUpdate", "Projecten", New With {.projectid = Model.ProjectId, .changeorderid = Order.Id, .duplicate = True})" Class="duplicateChangeOrder"><i Class="fa fa-copy fa-lg"></i></a>@if User.IsInRole("Admin") Then @<text>&nbsp;<a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("ChangeOrderAddUpdate", "Projecten", New With {.projectid = Model.ProjectId, .changeorderid = Order.Id})" Class="editChangeOrder"><i Class="fa fa-edit fa-lg"></i></a>&nbsp;<a Class="deleteChangeOrder" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@Order.Id" href="#ModalDeleteChangeOrder"><i Class="fa fa-remove red fa-lg"></i></a></text> ElseIf Order.DateAgreement Is Nothing Then @<text>&nbsp;<a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("ChangeOrderAddUpdate", "Projecten", New With {.projectid = Model.ProjectId, .changeorderid = Order.Id})" Class="editChangeOrder"><i Class="fa fa-edit fa-lg"></i></a>&nbsp;<a Class="deleteChangeOrder" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@Order.Id" href="#ModalDeleteChangeOrder"><i Class="fa fa-remove red fa-lg"></i></a></text>end If</td>

                                            </tr>
                                        </text>
                                            @*If Not Order.Details.Count = 0 Then
    @<text>
    <tr class="collapse @Order.Id">
        <td></td>
        <td colspan="8">
            <table Class="table mb-none">
                <thead>
                    <tr>
                        <th> Omschrijving</th>
                        <th colspan="2"> Eenheid</th>
                        <th> Aantal</th>
                        <th> Eenheidsprijs</th>
                        <th> Commissie</th>
                        <th> Totaalprijs</th>
                    </tr>
                </thead>
                <tbody>

                    @For Each detail In Order.Details
                        @<text>
                            <tr>
                                <td>@Html.DisplayFor(Function(m) detail.Description) </td>
                                <td>@detail.MeasurementType.GetDisplayName()</td>
                                <td>@detail.MeasurementUnit.GetDisplayName()</td>
                                <td>@Html.DisplayFor(Function(m) detail.Number)</td>
                                <td>@Html.DisplayFor(Function(m) detail.Price)</td>
                                <td>@Html.DisplayFor(Function(m) detail.Commision)</td>
                                <td>@String.Format("{0:C}", detail.Totaal)</td>
                            </tr>
                        </text>
                    Next*@
                                                        @*<tr>
                                                            
                                                            <td><strong>Totaal</strong></td>
                                                            <td colspan="5"></td>
                                                            <td><strong>@String.Format("{0:C}", Order.Totaal)</strong></td>
                                                        </tr>
                                                        <tr>
                                                            
                                                            <td>BTW @Html.DisplayFor(Function(m) Model.VatPercentage)</td>
                                                            <td colspan="5"></td>
                                                            <td>@String.Format("{0:C}", (Order.Totaal * Model.VatPercentage / 100))</td>
                                                        </tr>
                                                        <tr>
                                                            
                                                            <td><strong>Totaal incl. btw</strong></td>
                                                            <td colspan="5"></td>
                                                            <td><strong>@String.Format("{0:C}", (Order.Totaal * Model.VatPercentage / 100) + Order.Totaal)</strong></td>
                                                        </tr>*@
                                                    @*</tbody>
                                                </table>*@
                                                @*</td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                            <td style="display: none"></td>
                                                </tr>
                                                </text>
                                            End If*@
                                        Next
                                </tbody>
                            </table>
                      
                    </div>
                </section>



        <!-- end Page -->

    </div>
</div>
            </section>
        </div>
    </div>

<div id = "ModalDeleteChangeOrder" Class="modal-block modal-block-warning mfp-hide">
    <div id = "delete-changeorder-container" ></div>
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
    var oTable;
    $('#datatable-details tbody td img').click(function () {
        var nTr = this.parentNode.parentNode;
        if (this.src.match('details_close')) {
            /* This row is already open - close it */
            this.src = "/img/details_open.png";
            oTable.fnClose(nTr);
        }
        else {
            /* Open this row */
            this.src = "/img/details_close.png";
            var changeorderid = $(this).attr("rel");
            $.get("ChangeOrderDetailTable?ChangeOrderID=" + changeorderid, function (details) {
                oTable.fnOpen(nTr, details, 'details');
            });
        }
    });
   
    $(document).ready(function () {
        oTable = $('#datatable-details').dataTable({
            language: {
                processing: "Bezig...",
                search: "Zoeken:",
                lengthMenu: "_MENU_ resultaten weergeven",
                info: "_START_ tot _END_ van _TOTAL_ resultaten",
                infoEmpty: "Geen resultaten om weer te geven",
                infoFiltered: " (gefilterd uit _MAX_ resultaten)",
                infoPostFix: "",

                loadingRecords: "Een moment geduld aub - bezig met laden...",
                zeroRecords: "Geen resultaten gevonden",
                emptyTable: "Geen resultaten aanwezig in de tabel",
                paginate: {
                    first: "Eerste",
                    previous: "Vorige",
                    next: "Volgende",
                    last: "Laatste"
                },
                aria: {
                    sortAscending: ": activeer om kolom oplopend te sorteren",
                    sortDescending: ": activeer om kolom aflopend te sorteren"
                }
            }

        });



        //$('#datatable-details').DataTable();
        $('.deleteChangeOrder').magnificPopup({
            type: 'inline',
            src: 'deleteChangeOrder',
        });
        //var $table = $('#datatable-details');
        //var datatable = $table.dataTable({
        //    aoColumnDefs: [{
        //        bSortable: false,
        //        aTargets: [ 0 ]
        //    }],
        //    aaSorting: [
		//		[1, 'asc']
        //    ]
        //});

        //// add a listener
        //$table.on('click', 'i[data-toggle]', function() {
        //    var $this = $(this),
		//		tr = $(this).closest( 'tr' ).get(0);

        //    if ( datatable.fnIsOpen(tr) ) {
        //        $this.removeClass( 'fa-minus-square-o' ).addClass( 'fa-plus-square-o' );
        //        datatable.fnClose( tr );
        //    } else {
        //        $this.removeClass( 'fa-plus-square-o' ).addClass( 'fa-minus-square-o' );
        //        datatable.fnOpen( tr, fnFormatDetails( datatable, tr), 'details' );
        //    }
        //});
        $('.chkbox:checkbox').each(function () {
            if ($(this).data('state') == "indeterminate") {
                $(this).prop("indeterminate", true);
            };
       
        });
    });

    $('.deleteChangeOrder').click(function () {
        var url = "/Projecten/ModalDeleteChangeOrder"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-changeorder-container').html(data);
        });
    });

    $('.chkbox').change(function () {
        var value  = $(this).prop('checked')
        $('.chkInvoicable:checkbox').data("groupid", $(this).attr('data-groupid')).each(function () {
            $(this).prop("checked", value);
        });
        $.ajax({
            url: '/Projecten/ChangeOrderInvoicable',
            data: { COid: $(this).attr('data-groupid'), value: $(this).prop('checked') },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {

            },

        });
    });

</script>

<script src="~/vendor/admin/isotope/jquery.isotope.js" ></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js" ></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js" ></script>
<script src="~/vendor/admin/bootstrap-table/bootstrap-table.js" ></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js" ></script>
<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>

End section

