@Modeltype FacturatieModel 
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"

End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/jquery-datatables-bs3/assets/css/datatables.css" />
end section
<section class="content-with-menu media-gallery  ">
    <div class="content-with-menu-container">
        <div class="inner-menu-toggle">
            <a href="#" class="inner-menu-expand" data-open="inner-menu">
                Show Bar <i class="fa fa-chevron-right"></i>
            </a>
        </div>

        <menu id="content-menu" class="inner-menu" role="menu">
            <div class="nano">
                <div class="nano-content">

                    <div class="inner-menu-toggle-inside">
                        <a href="#" class="inner-menu-collapse">
                            <i class="fa fa-chevron-up visible-xs-inline"></i><i class="fa fa-chevron-left hidden-xs-inline"></i> Hide Bar
                        </a>
                        <a href="#" class="inner-menu-expand" data-open="inner-menu">
                            Toon menu <i class="fa fa-chevron-down"></i>
                        </a>
                    </div>

                    <div class="inner-menu-content">

                        <a class="btn btn-block btn-primary btn-md pt-sm pb-sm text-md">
                            <i class="fa fa-upload mr-xs"></i>
                            Nieuwe factuur
                        </a>
                        <hr class="separator" />

                        <div class="sidebar-widget m-none">
                            <div class="widget-header clearfix">
                                <h6 class="title pull-left mt-xs">Mappen</h6>
                            </div>
                            <div class="widget-content">
                                <ul class="mg-folders">
                                    @for Each folder In Model.Folders
                                        @<text>
                                            <li>
                                                <a href="@(Url.Action("Index", "Facturatie", New With {.company = ViewData("company"), .folder = folder}))" Class="menu-item"><i Class="fa fa-folder"></i>@folder</a>
                                            </li>
                                        </text>
                                    Next
                                    
                                </ul>
                            </div>
                        </div>

                    </div>
                </div>
            </div>
        </menu>
        <div Class="inner-body mg-main">

            <table class="table table-bordered table-striped mb-none pageResize" id="datatable-default">
                <thead>
                    <tr>
                        <th>NR.</th>
                        <th>Fact. nr.</th>
                        <th>Naam</th>
                        <th>Factuurdatum</th>
                        <th>Laatst gewijzigd</th>
                        @*<th></th>*@
                        
                    </tr>
                </thead>
                <tbody>
                    @for Each file In Model.Files
                            @<text>
                                <tr>
                                    <td>@file.InvoiceNumber</td>
                                    <td><a href="@Url.Action("GetInvoicePdf", "Facturatie", New With {.fullpath = file.FullPath, .filename = file.Filename})">@file.InvoiceNumberLong </a></td>
                                    <td>@if Not file.ClientId = 0 Then @<text><a href="@Url.Action("Detail", "Klanten", New With {.clientid = file.ClientId})">@file.InvoiceName </a></text> Else @file.InvoiceName end If</td>
                                    <td>@Html.DisplayFor(Function(m) file.InvoiceDate)</td>
                                    <td>@Html.DisplayFor(Function(m) file.InvoiceDateChanged)</td>
                                    @*<td><a href="@Url.Action("GetInvoicePdf", "Facturatie", New With {.filename = file.FullPath})">@file.InvoiceNumberLong </a></td>
                                    <td><a href="@Url.Action("GetInvoicePdfQuest", "Facturatie", New With {.invoicenumber = file.InvoiceNumberLong})">@file.InvoiceNumberLong </a></td>*@
                                </tr>
                            </text>
                    Next

                </tbody>
            </table>
        </div>
    </div>
</section>

@section scripts
    <script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
    <script src="~/Scripts/admin/tables/examples.datatables.default.js"></script>
<script src="~/vendor/admin/jquery-datatables/extras/pageResize/dataTables.pageResize.js" ></script>
End Section