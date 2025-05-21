@modeltype ProjectPaymentStagesModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />

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
                    <div class="inner-toolbar clearfix">
                        <ul>
                            <li>
                                <a href="@Url.Action("PaymentStagesAddUpdate", "Projecten", New With {.projectid = Model.ProjectId})" class="btn  " type="button" id="btnAddPaymentStages"><i class="fa fa-plus"></i> Toevoegen</a>
                            </li>
                            <li>
                                <a href="@Url.Action("PaymentGroupLink", "Projecten", New With {.projectid = Model.ProjectId})" class="btn  " type="button" id="btnLinkPaymentGroup"><i class="fa fa-link"></i> Koppelen</a>
                            </li>
                        </ul>
                    </div>
                    <div class="row mt-xl">
                        <h3>Betalingsschijven</h3>
                    </div>
                    <div class="row">
                            
                            @for Each group In Model.Groups
                                @<text>
                                    <Table Class="table table-responsive ">
                                        <thead>
                                            <tr class="primary">
                                                <td colspan="3" class="text-weight-bold text-capitalize">@group.Name - @group.VatPercentage.ToString("0") % BTW</td>
                                                <td class="text-right" width="10%"><a href="@Url.Action("PaymentStagesAddUpdate", "Projecten", New With {.projectid = Model.ProjectId, .groupid = group.Id})"><i class="fa fa-edit"></i></a></td>
                                            </tr>
                                            <tr>

                                                <th width="60%"> Schijf</th>
                                                <th width="10%"> Percentage</th>
                                                <th width="10%"> Factureerbaar</th>
                                                <th></th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            @for Each stage In group.PaymentStages
                                                @<text>
                                                    <tr Class="active">
                                                        <td >@Html.DisplayFor(Function(m) stage.Name)</td>
                                                        <td>@stage.Percentage.ToString("0.00") @Html.Raw("% ")</td>
                                                       
                                                        <td>@if stage.InvoiceCount > 0 Then  @Html.CheckBoxFor(Function(m) stage.Invoicable, New With {.data_id = stage.Id, .class = "chkInvoicable", .disabled = ""})Else @Html.CheckBoxFor(Function(m) stage.Invoicable, New With {.data_id = stage.Id, .class = "chkInvoicable"})End If</td>
                                                        @if stage.Doc Is Nothing Then
                                                            @<text>
                                                                <td Class="text-right"><a href="#modaladddoc" class="modal-with-form btnAddStageDoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Attest toevoegen" type="button" id="btnAddStageDoc" data-id="@stage.Id"><i class="el el-icon-file-new"></i></a> &nbsp;<a href="#modalselectdoc" class="modal-with-form btnSelectStageDoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Attest Selecteren" type="button" id="btnSelectStageDoc" data-id="@stage.Id"><i class="el el-icon-file"></i></a></td> 
                                                        </text>
                                                        Else
                                                            @<text>
                                                            <td Class="text-right"><a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & stage.Doc.Filename)" target="_blank" data-toggle="tooltip" data-placement="top" title="" data-original-title="Attest openen" type="button"><i class="fa fa-file-pdf-o"></i></a> &nbsp;&nbsp;<a href="#modaldeletedoc" class="modal-with-form btnDeleteStageDoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Attest verwijderen" type="button" data-id="@stage.Doc.Docid" data-stageid="@stage.Id"><i class="fa fa-trash"></i></a></td>

                                                            </text>
                                                        End If
                                                    </tr>

                                                </text>
                                            Next
                                        </tbody>
                                    </Table>
                                </text>
                            Next



                    </div>







        <!-- end Page -->

    </div>
</div>

            <div id="modaladddoc" Class="modal-block modal-block-primary mfp-hide">
                <div id="add-doc-container"></div>
            </div>
            <div id="modalselectdoc" Class="modal-block modal-block-primary mfp-hide">
                <div id="select-doc-container"></div>
            </div>
            <div id="modaldeletedoc" Class="modal-block modal-block-warning  mfp-hide">
                <div id="delete-doc-container"></div>
            </div>
@section scripts
    <script>
        //toevoegen van een attest aan een betalingsschijf
        $('.btnAddStageDoc').click(function () {

            $.ajax({
                url: '/Projecten/ModalAddStageDoc',
                data: { id: @Model.ProjectId, stageid: $(this).attr('data-id') },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#add-doc-container').html(result);
                },
            });
        });
        //selecteren van een attest aan een betalingsschijf
        $('.btnSelectStageDoc').click(function () {
            $.ajax({
                url: '/Projecten/ModalSelectStageDoc',
                data: { id: @Model.ProjectId, stageid: $(this).attr('data-id') },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#select-doc-container').html(result);
                },
            });
        });
        //verwijderen van een attest van een betalingsschijf
        $('.btnDeleteStageDoc').click(function () {
            $.ajax({
                url: '/Projecten/ModalDeleteStageDoc',
                data: { id:@Model.ProjectId, stageid: $(this).attr('data-stageid'),docid: $(this).attr('data-id')},
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    $('#delete-doc-container').html(result);
                },
            });
        });
        $('.chkInvoicable').change(function () {

            $.ajax({
                url: '/Projecten/PaymentStagesInvoicable',
                data: { stageid: $(this).attr('data-id'), value: $(this).prop('checked') },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {

                },

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


    </script>
            <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
            <script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
            <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
            <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
End Section
