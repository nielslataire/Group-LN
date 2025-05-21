@modeltype ProjectInvoicingModel
@imports bo
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">
<link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />

End Section

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu media-gallery">
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
                    <div class="row">
                        <div class="col-md-10 col-sm-12">
                            @* Vordering der werken *@
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">Schijven vordering der werken</h2>
                                </header>
                                <div id="LoadingOverlayApi2" class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>

                                    <div class="table-responsive">
                                        <table class="table mb-none">
                                            <thead>
                                                <tr>
                                                    <th width="100%">Schijf</th>
                                                    <th style="white-space: nowrap;" class="text-right">Percentage</th>
                                                    <th style="white-space: nowrap;" class="text-right">Factuur opmaken</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each item In Model.ClientAccounts
                                                    @<text>
                                                        <tr class="primary">
                                                            <td colspan="3" class="text-weight-bold">
                                                                @If item.Client.Name IsNot Nothing Then
                                                                    @item.Client.Salutation.GetDisplayName() @Html.Raw(" ") @item.Client.DisplayName
                                                                Else
                                                                    @item.Client.DisplayName
                                                                End If
                                                            </td>
                                                        </tr>
                                                    </text>
                                                    @for Each iu In item.Units
                                                        @<text>
                                                            <tr class="active">
                                                                <td colspan="3">
                                                                    @Html.DisplayFor(Function(m) iu.Unit.Type.Name) @Html.Raw(" ") @Html.DisplayFor(Function(m) iu.Unit.Name) @Html.Raw("<br/>")
                                                                </td>
                                                            </tr>
                                                        </text>
                                                        For Each stagegroup In iu.PaymentStages.Select(Function(x) x.GroupId).Distinct()

                                                            @<text>
                                                                <tr class="secondary">
                                                                    <td colspan="3">
                                                                        @iu.PaymentStages.Where(Function(m) m.GroupId = stagegroup).FirstOrDefault.GroupName
                                                                    </td>
                                                                </tr>
                                                            </text>
                                                            For Each stage In iu.PaymentStages.Where(Function(l) l.GroupId = stagegroup)
                                                                @<text>
                                                                    <tr>
                                                                        <td width="100%" class="cap-first-letter"><i class="fa fa-level-down ml-md mr-md "></i>@Html.DisplayFor(Function(m) stage.Name)</td>
                                                                        <td style="white-space: nowrap;" class="text-right">@stage.Percentage.ToString("0.00") @Html.Raw("% ")</td>
                                                                        <td style="white-space: nowrap;" class="text-right">@If User.IsInRole("Boekhouding") Or User.IsInRole("Admin") Then@<text><input type="checkbox" class="chkInvoice" data-id="@stage.Id" data-unitid="@iu.Unit.Id" data-clientaccountid="@item.Client.Id"></text>End If</td>
                                                                    </tr>

                                                                </Text>

                                                            Next
                                                        next
                                                    Next
                                                Next
                                            </tbody>
                                        </table>
                                    </div>
                                    <div class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                </div>
                            </section>
                            @* Meerwerken *@
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">Meerwerken / Minwerken</h2>
                                </header>
                                <div id="LoadingOverlayApi" class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>

                                    <div class="table-responsive">
                                        <table class="table mb-none">
                                            <thead>
                                                <tr>
                                                    <th width="100%">Wijzigingsopdracht</th>
                                                    <th style="white-space: nowrap;" class="text-right">Bedrag</th>
                                                    <th style="white-space: nowrap;" class="text-right">Factuur opmaken</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each item In Model.ClientChangeOrders
                                                    @<text>
                                                        <tr class="primary">
                                                            <td colspan="3" class="text-weight-bold">
                                                                @If item.Client.Name IsNot Nothing Then
                                                                    @item.Client.Salutation.GetDisplayName() @Html.Raw(" ") @item.Client.DisplayName
                                                                Else
                                                                    @item.Client.DisplayName
                                                                End If
                                                            </td>
                                                        </tr>
                                                    </text>
                                                    @for Each co In item.ChangeOrders
                                                        @<text>
                                                            <tr class="active">
                                                                <td colspan="3">
                                                                    @Html.DisplayFor(Function(m) co.Description)@Html.Raw("<br/>")
                                                                </td>
                                                            </tr>
                                                        </text>

                                                        For Each cod In co.Details.Where(Function(i) i.Invoicable = True AndAlso i.Invoiced = False)
                                                            @<text>
                                                                <tr>
                                                                    <td width="100%" class="cap-first-letter"><i class="fa fa-level-down ml-md mr-md "></i>@Html.DisplayFor(Function(m) cod.Description)</td>
                                                                    <td style="white-space: nowrap;" class="text-right">@String.Format("{0:C}", cod.Totaal)</td>
                                                                    <td style="white-space: nowrap;" class="text-right">@If User.IsInRole("Boekhouding") Or User.IsInRole("Admin") Then@<text><input type="checkbox" class="chkInvoiceCO" data-id="@cod.Id" data-coid="@co.Id" data-clientaccountid="@item.Client.Id"></text>End If</td>
                                                                </tr>

                                                            </Text>

                                                        Next
                                                    Next
                                                Next
                                            </tbody>
                                        </table>
                                    </div>
                                    <div class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                </div>
                            </section>
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">Nutsaansluitingen</h2>
                                </header>
                                <div id="LoadingOverlayApi" class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>

                                    <div class="table-responsive">
                                        <table class="table mb-none">
                                            <thead>
                                                <tr>
                                                    <th width="100%">Klant</th>
                                                    <th style="white-space: nowrap;" class="text-right">Bedrag</th>
                                                    <th style="white-space: nowrap;" class="text-right">Factuur opmaken</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each item In Model.ClientUtilityCosts
                                                    @<text>
                                                        <tr class="active">
                                                            <td width="100%" class="cap-first-letter">@item.Clientname</td>
                                                            <td style="white-space: nowrap;" class="text-right">
                                                                @String.Format("{0:C}", item.UtilityCost.Sum(Function(s) s.Price / 100 * s.Percentage))
                                                            </td>
                                                            <td style="white-space: nowrap;" class="text-right"></td>
                                                        </tr>
                                                    </text>

                                                Next
                                            </tbody>
                                        </table>
                                    </div>
                                    <div class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                </div>
                            </section>

                        </div>
                        <div class="col-md-2 col-sm-12 text-right">
                            @If User.IsInRole("Boekhouding") Or User.IsInRole("Admin") Then @<text>
                                <a class="btn btn-primary btn-block" id="btnInvoicing" href="#" data-toggle="tooltip" data-placement="top" title="Facturen opmaken"><i class="fa fa-eur"></i> Facturen opmaken</a>
                            </text>End If
                            <a class="btn btn-default btn-block" href="@Url.Action("PaymentStages", "Projecten", New With {.projectid = Model.ProjectId})" data-toggle="tooltip" data-placement="top" title="Betalingsschijven" data-id="@Model.ProjectId"><i class="fa fa-pie-chart"></i> Betalingsschijven</a>
                            <a class="btn btn-default btn-block modal-with-form visible-xs-block visible-sm visible-md visible-lg" href="#modalprintinvoicelist" id="btnPrintInvoiceList" data-toggle="tooltip" data-placement="top" title="Facturatie overzicht" data-id="@Model.ProjectId"><i class="fa fa-bars"></i> Facturatie overzicht</a>


                        </div>
                    </div>





                                <!-- end Page -->

                        </div>
</div>
            </section>
        </div>
    </div>

<div id="modalprintinvoicelist" Class="modal-block modal-block-primary mfp-hide">
    <div id="invoice-list-container"></div>
                                                                                    </div> 
@section scripts
    <script>
        $('#modalprintinvoicelist').on('shown.bs.modal', function () {
            $('#lstClients').select2('focus');
        })
        $('#btnInvoicing').click(function () {
            var $el = $('#LoadingOverlayApi');
            $el.trigger('loading-overlay:show');
            var $el2 = $('#LoadingOverlayApi2');
            $el2.trigger('loading-overlay:show');
            var list = new Array();
            $('.chkInvoice:checked').each(function(i, e) {
                list.push({ ClientAccountId: $(this).attr('data-clientaccountid'), UnitId: $(this).attr('data-unitid'), StageId: $(this).attr('data-id'), CompanyId:'1060' });
            });
            var listCO = new Array();
            $('.chkInvoiceCO:checked').each(function (i, e) {
                listCO.push({ ClientAccountId: $(this).attr('data-clientaccountid'), ChangeOrderId: $(this).attr('data-coid'), ChangeOrderDetailId: $(this).attr('data-id') });
            });
            var result1;
            var result2;
            $.when(

                $.ajax({
                    url: '@Url.Action("MakeInvoices", "Projecten")', // dont hardcode url's!
                    type: "POST",
                    traditional: true,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ invoices: list }),
                    dataType: 'json',
success: function(data){
                        result1 = data.projectid;
                        
                    }
                }),

                $.ajax({
                    url: '@Url.Action("MakeInvoicesCO", "Projecten")', // dont hardcode url's!
type: "POST",
                    traditional: true,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ invoices: listCO }),
                    dataType: 'json',
success: function(data){
                        result2 = data.projectid;
                    }
                })

            ).then(function() {
                if(result1 != 0) {

                    window.location.href = '@(Url.Action("Invoicing"))?projectid=' + result1;

                }
                if(result2 != 0) {
                    window.location.href = '@(Url.Action("Invoicing"))?projectid=' + result2;
                }

            });

            @*window.location.href = '@(Url.Action("Invoicing"))?projectid=' + @Model.ProjectId;*@

              @*.done(function (data) {
                   .done(function (data) {
                        window.location.href = '@(Url.Action("Invoicing"))?projectid=' + data.projectid;
                    });*@
                    @*window.location.href = '@(Url.Action("Invoicing"))?projectid=' + data.projectid;*@


        });

        $('#btnPrintInvoiceList').click(function () {


            var url = "/Projecten/ModalPrintInvoiceList"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '?projectid=' + id, function (data) {
                $('#invoice-list-container').html(data);
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
            </text>                      End If
        });
        $(document).ready(function () {
            //$('.salesSettings').magnificPopup({
            //    type: 'inline',
            //    src: 'salesSettings',
            //});
            //$('.calculatePrice').magnificPopup({
            //    type: 'inline',
            //    src: 'calculatePrice',
            //});
            $('.printInvoiceList').magnificPopup({
                type: 'inline',
                src: 'printInvoiceList',
            });

        });

    </script>
<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
<script src="~/scripts/admin/tables/examples.datatables.default.js"></script>
<script src="~/vendor/admin/select2/select2.js"></script>
<script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
<script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
<script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
End Section
