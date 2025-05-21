@modeltype ProjectCalculationSettings
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
                        <div class="col-md-12 col-sm-12">
                            <div class="tabs tabs-primary">
                                <ul class="nav nav-tabs nav-justified">
                                    <li class="active">
                                        <a href="#Budget" data-toggle="tab">Budget</a>
                                    </li>
                                </ul>
                                <div class="tab-content">
                                    @Using Html.BeginForm("CalculationSettings", "Projecten", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
                                        @<text>
                                    @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                                    <div id="Budget" class="tab-pane active">
                                        <div id="LoadingOverlayApi" class="panel-body" style="position: relative; min-height: 150px;" data-loading-overlay="" data-loading-overlay-options='{ "css": { "backgroundColor": "#FFFFFF" } }'>
                                            <div class="form-group">
                                                <label class="col-md-3 control-label">Activiteiten toevoegen</label>
                                                <div class="col-md-6">
                                                    @Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.ListActivities, "ID", "Display", "Group", Model.SelectedActivities, Model.SelectedActivities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})
                                                </div>
                                                <div class="col-md-3">
                                                    <button class="btn btn-primary btn-block " type="button" id="btnAddActivities">Toevoegen</button>
                                                </div>
                                            </div>
                                            <hr />
                                            <div class="table-responsive">
                                                <table class="table mb-none" id="BudgetTable">
                                                    <thead>
                                                        <tr>
                                                            <th>Lot</th>
                                                            <th>Omschrijving</th>
                                                            <th class="text-right">Budget</th>
                                                            <th class="text-right">Verwijderen</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody id="BudgetActivityRows">
                                                        @For Each group In Model.ActivityGroups
                                                            @<text>
                                                                <tr class="active" id="@group.Name">
                                                                    <td>@group.Lot</td>
                                                                    <td colspan="4" class="text-weight-bold">
                                                                        @group.Name
                                                                    </td>
                                                                </tr>
                                                            </text>

                                                            @for Each budget In Model.BudgetActivities.Where(Function(m) m.Activity.Group.ID = group.ID)
                                                                Html.RenderPartial("_BudgetActivityRow", budget, New ViewDataDictionary() From {{"mode", "add"}})
                                                            Next
                                                                                Next
                                                    </tbody>
                                                </table>
                                            </div>
                                            <hr />
                                            <div class="form-group">
                                                <div class="col-md-12">
                                                    <button class="btn btn-block btn-primary" type="submit">Opslaan</button>
                                                </div>
                                                </div>
                                                <div Class="loading-overlay" style="background-color: #FFFFFF;"><div class="loader black"></div></div>

                                            </div>
                                        
                                        </div>
                                        </text>
End Using
                                </div>
                            </div>
                        </div>
                    </div>





                                <!-- end Page -->

                        </div>
</div>
            </section>
</div>
    </div>


@section scripts
    <Script>

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
        $('#btnAddActivities').click(function () {

            var el = document.getElementById('lstActivities');
            var result = [];
            var options = el && el.options;
            var opt;

            for (var i = 0, iLen = options.length; i < iLen; i++) {
                opt = options[i];
                
                if (opt.selected) {
                    var myString = $(opt).parent().attr("label").substr($(opt).parent().attr("label").indexOf("-") + 2);
                    //$("#BudgetTable td").filter(function () {
                    //    return $(this).text() == myString;
                    //}).css('background-color', 'red');
                   var indexrow = $("td:contains('" + myString + "')").closest("tr").index();
                    $.ajax({
                        url: 'AddBudgetActivitiy',
                        data: { ActId: opt.value, ActName: opt.text, ActGroup : $(opt).parent().attr("label") },
                        cache: false,
                        traditional: true,
                        type: 'POST',
                        async:false,
                        success: function (result) {
                            //alert($("#BudgetTable td").filter(function () {
                            //    return $(this).text() == myString;
                            //}).text());
                            $('#BudgetActivityRows > tr').eq(indexrow).after(result);
                            //$('#BudgetActivityRows').append(result);
                        },

                    });

                }
            }
            $("#lstActivities option:selected").attr('disabled', 'disabled');
            $("#lstActivities").val(null).trigger("change");
        });
        $(document).on('click', 'a.deleterow', function () { // <-- changes
            var $row = $(this).closest('tr')
            var $id = $row.find('td').eq(0).text();
            $("#lstActivities option[value=" + $id + "]").removeAttr('disabled').change();
            $(this).closest('tr').remove();
            return false;
        });
    </script>
<Script src="~/vendor/admin/isotope/jquery.isotope.js" ></script>
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>
<Script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js" ></script>
<Script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<Script src="~/scripts/admin/ui-elements/examples.modals.js" ></script>
<Script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js" ></script>
<Script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js" ></script>
<Script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js" ></script>
<Script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js" ></script>
<Script src="~/scripts/admin/tables/examples.datatables.default.js" ></script>
<Script src="~/vendor/admin/select2/select2.js" ></script>
<Script src="~/vendor/admin/select2/select2_locale_nl.js" ></script>
<Script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js" ></script>
<Script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js" ></script>
End Section
