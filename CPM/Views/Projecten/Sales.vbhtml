@modeltype ProjectSalesModel 
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
                        <div class="col-sm-12 text-right">
                            <div class="btn-group">
                                <button type="button" class="mb-xs mt-xs mr-xs btn btn-default  dropdown-toggle" data-toggle="dropdown"><i class="fa fa-file-pdf-o"></i> <span class="caret"></span></button>
                                <ul class="dropdown-menu" role="menu">
                                   
                                    <li><a href="@Url.Action("SalesListPDF", "Projecten", New With {.projectid = Model.ProjectId})">Verkooplijst</a></li>
                                    @*<li><a href="@Url.Action("DetailClientsListPdf", "Leveranciers", New With {.id = Model.ProjectId})">Toegiften</a></li>*@
                                </ul>
                            </div>
                            <button type="button" data-toggle="tooltip" data-placement="top" title="Adrukken" onclick="window.open('@Url.Action("SalesListPrint", "Projecten", New With {.projectid = Model.ProjectId})')" class="mb-xs mt-xs mr-xs btn btn-default hidden-tablet hidden-phone"><i class="fa fa-print"></i></button>
                            <a class="btn btn-default calculatePrice" href="#modalselectforprice" id="btnCalculatePrice" data-toggle="tooltip" data-placement="top" title="Prijs berekenen" data-id="@Model.ProjectId"><i class="fa fa-calculator"></i></a>
                            <a class="btn btn-default salesSettings" href="#modalsalessettings" id="btnSettings" data-toggle="tooltip" data-placement="top" title="Instellingen" data-id="@Model.ProjectId"><i class="fa fa-gear"></i></a> 
                            

                        </div>
                    </div>
                    <section class="panel">
                        <header class="panel-heading">
                            <h2 class="panel-title">Overzicht eenheden te koop</h2>
                        </header>
                        <div class="panel-body">
                            <div class="table-responsive">
                                <table class="table mb-none">
                                    <thead>
                                        <tr>
                                            <th>Naam</th>
                                            <th>Verdieping</th>
                                            <th class="text-right">Verkoopprijs</th>
                                            <th class="text-right">Grondwaarde</th>
                                            <th class="text-right">Bouwwaarde</th>
                                            <th class="text-right">Tienduiz.</th>
                                            <th class="text-right">Predkad</th>
                                            <th class="text-right">Acties</th>
                                        </tr>
                                    </thead>
                                    <tbody>


                                        @*@code
                                Dim Types = Model.Units.GroupBy(Function(m) m.Type.Id)

                            End Code*@
                                       @For Each Group In Model.UnitsGrouped

                                        @<text>
                                            <tr class="primary">
                                                <td colspan="8" class="text-weight-bold">@Group.Units(0).Unit.Type.Name </td>
                                            </tr>



                                        </text>

                                        @code Dim Levels = Group.Units.GroupBy(Function(m) m.Unit.Level)

                                        End Code
                                            For Each Level In Levels
                                                For Each item In Level

                                                        @<text>
                                                            <tr class="active">
                                                                <td><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = item.Unit.ProjectId, .unitid = item.Unit.Id})" class="editCompany">@if item.Unit.Type.Id <> 11 Then @item.Unit.Type.Name End If @item.Unit.Name</a></td>
                                                                <td>@if item.Unit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @item.Unit.Level</text> end If</td>
                                                                <td Class="text-right">@Html.DisplayFor(Function(m) item.Unit.TotalValue)</td>
                                                                <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.LandValue)</td>
                                                                <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.TotalConstructionValues)</td>
                                                                <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.Landshare)</td>
                                                                @If item.Unit.LinkedUnits.Count > 0 Then
                                                                    @<text>
                                                                        <td>
                                                                            @for each lu In item.Unit.LinkedUnits
                                                                            @lu.PreKad
                                                                            @If Not lu Is item.Unit.LinkedUnits.Last Then
                                                                                @Html.Raw(" - ")
                                                                            End If
                                                                        Next

                                                                    </td>
                                                                    </text>
                                                                Else
                                                                    @<text>
                                                                        <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.PreKad)</td>
                                                                    </text>
                                                                End If
                                                                <td class="text-right " data-title="Acties"><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = item.Unit.ProjectId, .unitid = item.Unit.Id})" class="editCompany"><i class="fa fa-edit "></i></a> <a class="deleteUnit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@item.Unit.Id" href="#ModalDeleteUnit"><i class="fa fa-remove red "></i></a>@*<a href="@Url.Action("DeleteCompany", "Leveranciers", New With {.id = company.CompanyId, .SearchName = Model.Filter.CompanyName, .SelectedActivities = ViewData("SelectedActivities").ToString(), .SelectedProvinces = ViewData("selectedprovinces").ToString()})" class="deleteCompany"><i class="fa fa-remove "></i></a>*@</td>
                                                            </tr>
                                                        </text>
                                                    For Each attachedunit In item.AttachedUnits
                                                            @<text>
                                                                <tr>
                                                                    <td><i class="fa fa-level-down ml-md mr-md "></i><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = attachedunit.ProjectId, .unitid = attachedunit.Id})" class="editCompany">@if attachedunit.Type.Id <> 11 Then @attachedunit.Type.Name End If @attachedunit.Name</a></td>
                                                                    <td>@if item.Unit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @item.Unit.Level</text> end If</td>
                                                                    <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.TotalValue)</td>
                                                                    <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.LandValue)</td>
                                                                    <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.ConstructionValue)</td>
                                                                    <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.Landshare)</td>
                                                                    @If attachedunit.LinkedUnits.Count > 0 Then
                                                                        @<text>
                                                                            <td>
                                                                                @for each lu In attachedunit.LinkedUnits
                                                                                    @lu.PreKad
                                                                                    @If Not lu Is attachedunit.LinkedUnits.Last Then
                                                                                        @Html.Raw(" - ")
                                                                                    End If
                                                                                Next

                                                                            </td>
                                                                        </text>
                                                                    Else
                                                                        @<text>
                                                                            <td>@Html.DisplayFor(Function(m) attachedunit.PreKad)</td>
                                                                        </text>
                                                                    End If
                                                                    <td class="text-right " data-title="Acties"><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = attachedunit.ProjectId, .unitid = attachedunit.Id})" class="editCompany"><i class="fa fa-edit "></i></a> <a class="deleteUnit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@attachedunit.Id" href="#ModalDeleteUnit"><i class="fa fa-remove red "></i></a>@*<a href="@Url.Action("DeleteCompany", "Leveranciers", New With {.id = company.CompanyId, .SearchName = Model.Filter.CompanyName, .SelectedActivities = ViewData("SelectedActivities").ToString(), .SelectedProvinces = ViewData("selectedprovinces").ToString()})" class="deleteCompany"><i class="fa fa-remove "></i></a>*@</td>
                                                                </tr>
                                                            </text>
                                                        Next
                                                    Next


                                                Next
                                            Next
                                       



                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </section>

                    

                </div>




            </div>
        </section>



        <!-- end: page -->

    </div>
</div>
<div id="modalsalessettings" class="modal-block modal-block-primary mfp-hide">
    <div id="sales-settings-container"></div>
</div> 
<div id="modalselectforprice" class="modal-block modal-block-primary mfp-hide">
    <div id="select-forprice-container"></div>
</div> 
<div id="modalcalculateprice" class="modal-block modal-block-primary mfp-hide">
    <div id="calculate-price-container"></div>
</div> 

@section scripts
    <script>
        $('#btnSettings').click(function () {
            var url = "/Projecten/ModalSalesSettings"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '?projectid=' + id, function (data) {
                $('#sales-settings-container').html(data);
            });

        });
        $('#btnCalculatePrice').click(function () {
            var url = "/Projecten/ModalSelectForPrice"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '?projectid=' + id, function (data) {
                $('#select-forprice-container').html(data);
            });

        });
        //$('#btnOpenCalculate').click(function () {
        //    var url = "/Projecten/ModalCalculatePrice"; // the url to the controller
        //    var id = $(this).attr('data-id'); // the id that's given to each button in the list
        //    $.get(url + '?projectid=' + id, function (data) {
        //        $('#select-forprice-container').html(data);
        //    });

        //});
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
            $('.salesSettings').magnificPopup({
                type: 'inline',
                src: 'salesSettings',
            });
            $('.calculatePrice').magnificPopup({
                type: 'inline',
                src: 'calculatePrice',
            });
            $('.openCalculate').magnificPopup({
                type: 'inline',
                src: 'openCalculate',
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
