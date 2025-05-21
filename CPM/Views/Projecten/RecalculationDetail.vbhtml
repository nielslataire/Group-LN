@modeltype ProjectRecalculationDetailModel
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
@Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
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
                    <h2>Nacalculatie - @Model.Activity.Name  </h2>
                    <hr />
                    <div class="row">
                        <div class="col-md-12 col-sm-12">
                            @For Each comp In Model.IncommingInvoicesActivities.GroupBy(Function(m) m.Company.ID).Select(Function(l) l.First()).ToList()

                                @<text>
                                    <section Class="panel">
                                        <header Class="panel-heading">
                                            <h3 class="panel-title">@comp.Company.Display </h3>

                                        </header>
                                        <div id="LoadingOverlayApi" Class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>
                                            <div Class="table-responsive">
                                                @For Each type In Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID).GroupBy(Function(l) l.IncommingInvoiceType).Select(Function(s) s.First()).ToList().OrderBy(Function(o) o.IncommingInvoiceType)
                                                    If type.IncommingInvoiceType = IncommingInvoiceType.Contract Then
                                                        @<text>
                                                            <h4 class="text-weight-bold text-uppercase">@type.IncommingInvoiceType.GetDisplayName() - @Html.DisplayFor(Function(t) Model.ContractActivities.Where(Function(m) m.ContractId = type.ContractId).FirstOrDefault().Price)  </h4>
                                                            <Table Class="table mb-lg ">
                                                                <thead>
                                                                    <tr>
                                                                        <th class="col-md-1"> FACT NR.</th>
                                                                        <th class="col-md-1"> DATUM</th>
                                                                        <th class="col-md-8"> OMSCHRIJVING</th>
                                                                        <th Class="text-right col-md-1">PRIJS</th>
                                                                        <th class="col-md-1"></th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody>



                                                                    @For Each invoice In Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType).OrderBy(Function(o) o.Invoicedate)
                                                                        @<text>
                                                                            <tr>
                                                                                <td>@invoice.ExternalInvoiceId</td>
                                                                                <td>@Html.DisplayFor(Function(m) invoice.Invoicedate)  </td>
                                                                                <td>@invoice.Description</td>
                                                                                <td Class="text-right">@Html.DisplayFor(Function(m) invoice.Price)</td>
                                                                                <td Class="text-right">
                                                                                    @If invoice.ContractId <> 0 Then
                                                                                        @<text>
                                                                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" id="editInvoice" onclick="location.href='@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = 0, .invoiceid = invoice.InvoiceId})'"><i Class="fa fa-edit"></i></a>
                                                                                        </text>
                                                                                    Else
                                                                                        @<text>
                                                                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" id="editInvoice" onclick="location.href='@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = 1, .invoiceid = invoice.InvoiceId})'"><i Class="fa fa-edit"></i></a>
                                                                                        </text>
                                                                                    End If
                                                                                    <a Class="deleteInvoice" data-id="@invoice.InvoiceId" href="#ModalDeleteInvoice" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen"><i class="fa fa-remove"></i></a>
                                                                                </td>
                                                                            </tr>
                                                                        </text>

                                                                    Next

                                                                </tbody>
                                                                <tfoot>
                                                                    <tr Class="active">
                                                                        <td colspan="3">TOTAAL</td>
                                                                        <td Class="text-right">
                                                                            @String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType).Sum(Function(s) s.Price))
                                                                        </td>
                                                                        <td></td>
                                                                    </tr>
                                                                </tfoot>

                                                            </Table>
                                                        </text>
                                                    Else

                                                        @<text>
                                                            <h4 class="text-weight-bold text-uppercase">@type.IncommingInvoiceType.GetDisplayName()</h4>
                                                            <Table Class="table mb-lg ">
                                                                <thead>
                                                                    <tr>
                                                                        <th class="col-md-1"> FACT NR.</th>
                                                                        <th class="col-md-1"> DATUM</th>
                                                                        <th class="col-md-8"> OMSCHRIJVING</th>
                                                                        <th Class="text-right col-md-1">PRIJS</th>
                                                                        <th class="col-md-1"></th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody>



                                                                    @For Each invoice In Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType).OrderBy(Function(o) o.Invoicedate)
                                                                        @<text>
                                                                            <tr>
                                                                                <td>@invoice.ExternalInvoiceId</td>
                                                                                <td>@Html.DisplayFor(Function(m) invoice.Invoicedate)  </td>
                                                                                <td>@invoice.Description</td>
                                                                                <td Class="text-right">@Html.DisplayFor(Function(m) invoice.Price)</td>
                                                                                <td Class="text-right">
                                                                                    @If invoice.ContractId <> 0 Then
                                                                                        @<text>
                                                                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" id="editInvoice" onclick="location.href='@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = 0, .invoiceid = invoice.InvoiceId})'"><i Class="fa fa-edit"></i></a>
                                                                                        </text>
                                                                                    Else
                                                                                        @<text>
                                                                                            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" id="editInvoice" onclick="location.href='@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = 1, .invoiceid = invoice.InvoiceId})'"><i Class="fa fa-edit"></i></a>
                                                                                        </text>
                                                                                    End If
                                                                                    <a Class="deleteInvoice" data-id="@invoice.InvoiceId" href="#ModalDeleteInvoice" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen"><i class="fa fa-remove"></i></a>
                                                                                </td>
                                                                            </tr>
                                                                        </text>

                                                                    Next

                                                                </tbody>
                                                                <tfoot>
                                                                    <tr Class="active">
                                                                        <td colspan="3">TOTAAL</td>
                                                                        <td Class="text-right">
                                                                            @String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType).Sum(Function(s) s.Price))
                                                                        </td>
                                                                        <td></td>
                                                                    </tr>
                                                                </tfoot>

                                                            </Table>
                                                        </text>
                                                    End If

                                                Next

                                            </div>
                                            <div Class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                        </div>
                                    </section>
                                </text>
                            Next
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12 col-sm-12">
                            @For Each cont In Model.ContractsWithoutInvoices


                                    @<text>
                                        <section Class="panel">
                                            <header Class="panel-heading">
                                                <h3 class="panel-title">@cont.Company.Display </h3>

                                            </header>
                                            <div id="LoadingOverlayApi" Class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>
                                                <div Class="table-responsive">
                                                    @For Each type In cont.Activities.Where(Function(m) m.Activity.ID = Model.Activity.ID)
                                                        @<text>
                                                            <h4 class="text-weight-bold text-uppercase">Contract - @Html.DisplayFor(Function(m) type.Price)</h4>
                                                            <Table Class="table mb-lg ">
                                                                <thead>
                                                                    <tr>
                                                                        <th class="col-md-1"> FACT NR.</th>
                                                                        <th class="col-md-1"> DATUM</th>
                                                                        <th class="col-md-8"> OMSCHRIJVING</th>
                                                                        <th Class="text-right col-md-1">PRIJS</th>
                                                                        <th class="col-md-1"></th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody>




                                                                            <tr>
                                                                                <td colspan="5">- NOG GEEN FACTUREN -</td>
                                                                            </tr>


                                                                </tbody>
                                                            </Table>
                                                        </text>
                                                    Next

                                                </div>
                                                <div Class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                            </div>
                                        </section>
                                    </text>

                            Next
                                                        </div>
                                                    </div>




                                                    <!-- end Page -->

                                                </div>
</div>
                                            <div id = "ModalDeleteInvoice" Class="modal-block modal-block-warning mfp-hide">
                                                <div id = "delete-invoice-container" ></div>
                                                                                                            </div>

                                @section scripts
                                    <script>
        //delete invoice
        $('.deleteInvoice').click(function () {
            var url = "/Projecten/ModalDeleteIncommingInvoice"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#delete-invoice-container').html(data);
            });
        });

        //page load
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

        //page ready
        $(document).ready(function () {
            //initialize popup
            $('.deleteInvoice').magnificPopup({
                type: 'inline',
                src: 'deleteInvoice',
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
