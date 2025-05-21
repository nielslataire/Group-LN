@modeltype ProjectContractsModel
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
                        <div class="row mb-lg">
                            <div class="col-sm-12">
                                <div class="btn-group">
                                    <button type="button" class="mb-xs mt-xs mr-xs btn btn-default btn-lg  dropdown-toggle" data-toggle="dropdown" data-toggle-second="tooltip" data-placement="top" title="Afdrukken"><i class="fa fa-print"></i> <span class="caret"></span></button>
                                    <ul class="dropdown-menu" role="menu">
                                        <li><a href="@Url.Action("PrintRecalculation", "Projecten", New With {.projectid = Model.ProjectId, .details = 0})">Overzicht</a></li>
                                        <li><a href="@Url.Action("PrintRecalculation", "Projecten", New With {.projectid = Model.ProjectId, .details = 1})">Overzicht met details</a></li>
                                    </ul>
                                </div>
                                <div class="btn-group">
                                    <button type="button" class="mb-xs mt-xs mr-xs btn btn-default btn-lg  dropdown-toggle" data-toggle="dropdown" data-toggle-second="tooltip"  data-placement="top" title="Toevoegen"><i class="fa fa-plus"></i> <span class="caret"></span></button>
                                    <ul class="dropdown-menu" role="menu">
                                        <li><a href="@Url.Action("ContractAdd", "Projecten", New With {.projectid = Model.ProjectId})">Contract</a></li>
                                        <li class="divider"></li>
                                        <li><a href="@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = "0"})">Factuur onder contract</a></li>
                                        <li><a href="@Url.Action("IncommingInvoiceAdd", "Projecten", New With {.projectid = Model.ProjectId, .type = "1"})">Factuur zonder contract</a></li>
                                    </ul>
                                </div>
                                <a class="btn btn-default btn-lg salesSettings" href="@Url.Action("CalculationSettings", "Projecten", New With {.projectid = Model.ProjectId})" id="btnSettings" data-toggle="tooltip" data-placement="top" title="Instellingen" data-id="@Model.ProjectId"><i class="fa fa-gear"></i></a>

                            </div>
                        </div>
                        @Html.Partial("ModalAddContract", New ProjectAddContractModel With {.ProjectId = Model.ProjectId})
                        <div class="row">
                            <div class="col-md-12 col-sm-12">
                                <section class="panel">
                                    <header class="panel-heading">
                                        <h2 class="panel-title">Nacalculatie</h2>
                                    </header>
                                    <div id="LoadingOverlayApi" class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>

                                        <div class="table-responsive">
                                            <table class="table mb-none">
                                                <thead>
                                                    <tr>
                                                        <th>Lot</th>
                                                        <th>Omschrijving</th>
                                                        <th class="text-right">Budget</th>
                                                        <th class="text-right">Contract</th>
                                                        <th class="text-right">Gefactureerd</th>
                                                        <th class="text-right">Verschil Budget - Contract</th>
                                                        <th class="text-right">Verschil Budget - Facturatie</th>
                                                    </tr>
                                                </thead>

                                                @For Each group In Model.ActivityGroups
                                                    @<text>
                                                        <tbody>
                                                            <tr class="active clickable" data-toggle="collapse" data-target="#@group.id" aria-expanded="false" aria-controls="@group.ID">
                                                                <td>
                                                                    <i class="fa fa-plus-square primary "></i>
                                                                    @group.Lot
                                                                </td>
                                                                <td class="text-weight-bold">
                                                                    @group.Name
                                                                </td>
                                                                <td class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(s) s.Price))</td>
                                                                <td class="text-right" width="10%">@String.Format("{0:C}", Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.Group.ID = group.ID)).Sum(Function(s) s.Price))</td>
                                                                <td class="text-right" width="10%">@String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price))</td>
                                                                @If Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.Group.ID = group.ID)).Sum(Function(s) s.Price) >= 0 Then
                                                                    @<text>
                                                                        <td Class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.Group.ID = group.ID)).Sum(Function(s) s.Price))</td>
                                                                    </text>
                                                                Else
                                                                    @<text>
                                                                        <td Class="text-right text-warning" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.Group.ID = group.ID)).Sum(Function(s) s.Price))</td>
                                                                    </text>
                                                                End If
                                                                @If Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) >= 0 Then
                                                                    @<text>
                                                                        <td class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price))</td>
                                                                    </text>
                                                                Else
                                                                    @<text>
                                                                        <td class="text-right text-warning" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price))</td>
                                                                    </text>
                                                                End If

                                                            </tr>
                                                        </tbody>

                                                    </text>
                                                    @<text>
                                                        <tbody id="@group.id" class="collapse">
                                                            @for Each activity In group.Activities.Where(Function(m) Model.Contracts.Any(Function(a) a.Activities.Any(Function(i) i.Activity.ID = m.ID)) Or Model.BudgetActivities.Any(Function(l) l.Activity.ID = m.ID) Or Model.IncommingInvoicesActivities.Any(Function(l) l.Activity.ID = m.ID)).OrderBy(Function(o) o.Name)
                                                                @<text>

                                                                    <tr>
                                                                        <td></td>
                                                                        <td>
                                                                            <a href="@Url.Action("RecalculationDetail", "Projecten", New With {.projectid = Model.ProjectId, .activityid = activity.ID})">@activity.Name</a>
                                                                        </td>
                                                                        <td class="text-right" width="10%">@if Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Count > 0 Then@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).FirstOrDefault.Price)End If</td>
                                                                        <td class="text-right" width="10%">@String.Format("{0:C}", Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.ID = activity.ID)).GroupBy(Function(g) g.ContractId).Sum(Function(s) s.Sum(Function(t) t.Price)))</td>
                                                                        <td Class="text-right" width="10%">@String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price))</td>
                                                                        @If Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.ID = activity.ID)).GroupBy(Function(g) g.ContractId).Sum(Function(s) s.Sum(Function(t) t.Price)) >= 0 Then
                                                                            @<text>
                                                                                <td Class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.ID = activity.ID)).GroupBy(Function(g) g.ContractId).Sum(Function(s) s.Sum(Function(t) t.Price)))</td>
                                                                            </text>
                                                                        Else
                                                                            @<text>
                                                                                <td Class="text-right text-warning" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.ID = activity.ID)).GroupBy(Function(g) g.ContractId).Sum(Function(s) s.Sum(Function(t) t.Price)))</td>

                                                                            </text>
                                                                        End If
                                                                        @If Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) >= 0 Then
                                                                            @<text>
                                                                                <td Class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price))</td>
                                                                            </text>
                                                                        Else
                                                                            @<text>
                                                                                <td Class="text-right text-warning" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price))</td>
                                                                            </text>
                                                                        End If

                                                                    </tr>

                                                                </text>

                                                            Next

                                                        </tbody>
                                                    </text>
                                                Next




                                                <tfoot>
                                                    <tr>
                                                        <td></td>
                                                        <td></td>
                                                        <td class="text-right"></td>
                                                        <td></td>
                                                        <td></td>
                                                        <td></td>
                                                        <td></td>
                                                    </tr>
                                                    <tr class="primary">
                                                        <td>TOTAAL</td>
                                                        <td></td>
                                                        <td class="text-right">@String.Format("{0:C}", Model.BudgetActivities.Sum(Function(s) s.Price))</td>
                                                        <td class="text-right">@String.Format("{0:C}", Model.Contracts.Sum(Function(s) s.Activities.Sum(Function(f) f.Price)))</td>
                                                        <td class="text-right">@String.Format("{0:C}", Model.IncommingInvoicesActivities.Sum(Function(s) s.Price))</td>
                                                        <td class="text-right">@String.Format("{0:C}", Model.BudgetActivities.Sum(Function(s) s.Price) - Model.Contracts.Sum(Function(s) s.Activities.Sum(Function(f) f.Price)))</td>
                                                        <td class="text-right">@String.Format("{0:C}", Model.BudgetActivities.Sum(Function(s) s.Price) - Model.IncommingInvoicesActivities.Sum(Function(s) s.Price))</td>
                                                    </tr>

                                                </tfoot>
                                            </table>
                                        </div>
                                        <div Class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                    </div>
                                </section>
                            </div>
                        </div>





                        <!-- end Page -->

                    </div>
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
            </text>                      End If
        });
        $(document).ready(function () {
            //Select lijst van bedrijven
            $("#txtCompany").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
placeholder: "Selecteer het bedrijf",
                ajax: {

                    url: '@Url.Action("GetCompanys", "Projecten")',
cache: false,
                    traditional: true,
                    type: 'POST',
                    data: function (term) {
                        return {
                            term: term,
                        };
                    },

                    results: function (data, page) {
                        return { results: data };
                    },
                    initSelection: function (element, callback) {

                    }
                },
            });
        });
        $('#txtCompany').on('change', function (e) {
            var data = $('#txtCompany').val();
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
