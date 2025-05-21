@modeltype ProjectAddContractModel
@imports bo
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName & " - Contract toevoegen"
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
                    @Using Html.BeginForm("ContractAdd", "Projecten", FormMethod.Post, New With {.id = "FormAdd", .class = "form-horizontal"})
                        @<text>
                            <input type="hidden" name="returnUrl" value="@ViewBag.returnUrl" />
                            @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                            @Html.HiddenFor(Function(m) m.Contract.ProjectId, New With {.id = "contractprojectid"})
                            @Html.HiddenFor(Function(m) m.Contract.Id, New With {.id = "contractid"})
                            @Html.HiddenFor(Function(m) m.ProjectName, New With {.id = "projectname"})

                            <div Class="row">

                                <section Class="panel col-md-12" id="pnlAdd">
                                    <header class="panel-heading">
                                        <h2 class="panel-title">
                                            <span class="va-middle">Info</span>
                                        </h2>
                                    </header>
                                    <div class="panel-body">
                                        <div class="form-group">
                                            <label class="col-md-2 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Companies)</label>
                                            <div class="col-md-10">
                                                @If Model.Contract.Company.ID > 0 Then
                                                    @<text>
                                                        <p class="form-control-static">
                                                            <strong>
                                                                @Model.Contract.Company.Display 
                                                                @Html.HiddenFor(Function(m) m.Contract.Company.ID, New With {.id = "txtCompany", .class = "form-control"})


                                                            </strong>
                                                        </p>
                                                    </text>
                                                Else
                                                    @Html.HiddenFor(Function(m) m.Contract.Company.ID, New With {.id = "txtCompany", .class = "form-control"})
                                                End If
                                            </div>
                                        </div>
                                        <div Class="form-group">
                                            <Label Class="col-md-2 control-label" for="txtVatPercentage">@Html.LabelFor(Function(m) m.Contract.VatPercentage)</Label>
                                            <div Class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Contract.VatPercentage, New With {.id = "txtVatPercentage", .class = "form-control"})
                                            </div>
                                        </div>
                                        <div Class="form-group">
                                            <Label Class="col-md-2 control-label" for="txtPaymentTerm">@Html.LabelFor(Function(m) m.Contract.PaymentTerm)</Label>
                                            <div Class="col-md-4">
                                                @Html.TextBoxFor(Function(m) m.Contract.PaymentTerm, New With {.id = "txtPaymentTerm", .class = "form-control", .placeholder = "Aantal dagen"})
                                            </div>

                                        </div>
                                        <div Class="form-group">
                                            <Label Class="col-md-2 control-label" for="txtCashDiscountPaymentTerm">@Html.LabelFor(Function(m) m.Contract.CashDiscountPaymentTerm)</Label>
                                            <div Class="col-md-4">
                                                <div Class="input-group">
                                                    <span Class="input-group-addon">
                                                        @Html.CheckBoxFor(Function(m) m.Contract.CashDiscount, New With {.id = "chkCashDiscount"})
                                                    </span>
                                                    @Html.TextBoxFor(Function(m) m.Contract.CashDiscountPaymentTerm, New With {.id = "txtCashDiscountPaymentTerm", .class = "form-control", .type = "text", .placeholder = "Aantal dagen"}).DisableIf(Function() Model.Contract.CashDiscount = False)
                                                </div>
                                            </div>
                                            <Label Class="col-md-2 control-label" for="txtCashDiscountPercentage">@Html.LabelFor(Function(m) m.Contract.CashDiscountPercentage)</Label>
                                            <div Class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Contract.CashDiscountPercentage, New With {.id = "txtCashDiscountPercentage", .class = "form-control"}).DisableIf(Function() Model.Contract.CashDiscount = False)
                                            </div>
                                        </div>
                                        <div Class="form-group">
                                            <Label Class="col-md-2 control-label" for="txtGuaranteeType">@Html.LabelFor(Function(m) m.Contract.GuaranteeType)</Label>
                                            <div Class="col-md-4">

                                                    @Html.EnumDropDownListFor(Function(m) m.Contract.GuaranteeType, New With {.id = "lstGuaranteeType", .class = "form-control"})

                                            </div>
                                            <Label Class="col-md-2 control-label" for="txtCashDiscountPercentage">@Html.LabelFor(Function(m) m.Contract.GuaranteePercentage)</Label>
                                            <div Class="col-md-4">
                                                @Html.EditorFor(Function(m) m.Contract.GuaranteePercentage, New With {.id = "txtGuaranteePercentage", .class = "form-control"}).DisableIf(Function() Model.Contract.GuaranteeType <> ContractGuaranteeType.FinancialGuarantee)
                                            </div>
                                        </div>
                                        <div Class="form-group">
                                            <Label Class="col-md-2 control-label" for="txtGuaranteeType">@Html.LabelFor(Function(m) m.Contract.ContractSigned)</Label>
                                            <div Class="col-md-8">
                                                
                                                        @Html.CheckBoxFor(Function(m) m.Contract.ContractSigned, New With {.id = "chkContractSigned"})
                                                
                                            </div>
                                        
                                        </div>
                                    </div>

                                </section>



                            </div>
                            <div Class="row">
                                <section Class="panel col-md-12">
                                    <header class="panel-heading">
                                        <h2 class="panel-title">
                                            <span class="va-middle">Loten</span>
                                        </h2>
                                    </header>
                                    <div Class="panel-body">
                                        <div class="row">
                                            <div Class="col-md-6">
                                                <div class="input-group mb-md">
                                                    @Html.HiddenFor(Function(m) m.SelectedActivities, New With {.id = "lstActivities", .class = "form-control", .multiple = "multiple"})
                                                    @*@Html.ListBoxFor(Function(m) m.SelectedActivities, New SelectList(Model.Activities, "ID", "Display", "Group", Model.SelectedActivities, Model.Activities, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstActivities"})*@
                                                    <span class="input-group-btn">
                                                        <button id="btnAddActivities" class="btn btn-primary">Toevoegen</button>
                                                    </span>
</div>
                                                </div>
                                        </div>
                                       
                                        <hr>

                                        <Table Class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                <tr>
                                                    <th width="10%">#</th>
                                                    <th width="30%"> Lot</th>
                                                    <th> Prijs</th>
                                                    <th width="20%"> Acties</th>
                                                </tr>
                                            </thead>
                                            <tbody id="ActivityRows">

                                                @For Each item In Model.Contract.Activities
                                                    Html.RenderPartial("_ActivityRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                                Next


                                            </tbody>
                                        </Table>

                                    </div>
                                </section>

                            </div>
                            <div Class="row" >
                                <section Class="panel col-md-12">
                                    <header class="panel-heading">
                                        <h2 class="panel-title">
                                            <span class="va-middle">Bijbestellingen</span>
                                        </h2>
                                    </header>
                                    <div Class="panel-body">
                                        <div class="row">
                                            <div class="col-sm-6">
                                                <div class="input-group mb-md">
                                                    @Html.HiddenFor(Function(m) m.SelectedActivitiesAddOrders, New With {.id = "lstActivitiesOrders", .class = "form-control", .multiple = "multiple"})
                                                    <span class="input-group-btn">
                                                        <button id="btnAddAddtionalOrder" class="btn btn-primary">Toevoegen</button>
                                                    </span>
                                                   
                                                </div>
                                            </div>
                                        </div>
                                        <Table Class="table table-no-more table-bordered table-striped mb-none">
                                            <thead>
                                                <tr>
                                                    <th width="10%">#</th>
                                                    <th width="30%"> Lot</th>
                                                    <th width="30%"> Omschrijving</th>
                                                    <th> Prijs</th>
                                                    <th width="20%"> Acties</th>
                                                </tr>
                                            </thead>
                                            <tbody id="AdditionalOrderRows">
                                                @For Each item In Model.Contract.Activities
                                                    For Each addorder In item.AdditionalOrders
                                                        Html.RenderPartial("_AdditionalOrderRow", item, New ViewDataDictionary() From {{"mode", "add"}})
                                                    Next

                                                Next

                                            </tbody>
                                        </Table>

                                    </div>
                                </section>

                            </div>
                            <Button Class="btn btn-primary btn-block">Opslaan</Button>
                            <a href="@Url.Action("Contracts", "Projecten", New With {.projectid = Model.ProjectId})" class="btn btn-default btn-block">Annuleren</a>

                        </text>
                    End Using
                </div>
            </div>
            </section>
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
                    $("#lstActivitiesOrders").select2('disable');
                    //$("#Contract_CashDiscountPercentage").attr("disabled", "disabled");
                    //$("#txtCashDiscountPaymentTerm").attr("disabled", "disabled");
                    $("#lstActivities").select2({
                        placeholder: 'Selecteer het lot',
                        data:[]
                    });
                    $("#lstActivitiesOrders").select2({
                        placeholder: 'Selecteer het lot',
                        data: []
                    });
                    if ($('#txtCompany').val() != 0) {
                        var data = $('#txtCompany').val();
                        GetActivities(data);

                    } else {
                         $("#txtCompany").select2({

                        minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                        width: 'resolve',   // to adjust proper width of select2 wrapped elements
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
                        $("#lstActivities").select2('disable');
                        $("#lstActivitiesOrders").select2('disable');
                    };
                
                    if ($('#contractid').val() != 0) {
                        var data = $('#contractid').val();
                        //GetContractActitvities(data);
                        
                    };
                    //
                    //Select lijst van bedrijven



                });
                //$('#lstType').on('change', function () {

                //    var val = $(this).val();
                //    if (val == 1) {
                //        $('#txtPeriod').attr("disabled", false);
                //        $('#txtExtension').attr("disabled", false);
                //        $('#txtGuarantee').attr("disabled", false);
                //    }
                //    else {
                //        $('#txtPeriod').attr("disabled", true);
                //        $('#txtExtension').attr("disabled", true);
                //        $('#txtGuarantee').attr("disabled", true);
                //    }

                //});
                $('#txtCompany').on('change', function (e) {
                    var data = $('#txtCompany').val();
                    GetActivities(data);
                });
                $('#chkCashDiscount').on('change', function (e) {
                    if ($('#chkCashDiscount').prop('checked')) {
                        $("#Contract_CashDiscountPercentage").removeAttr("disabled");
                        $("#txtCashDiscountPaymentTerm").removeAttr("disabled");
                    } else {
                        $("#Contract_CashDiscountPercentage").attr("disabled", "disabled");
                        $("#txtCashDiscountPaymentTerm").attr("disabled", "disabled");
                    }
                });
                    $('#lstGuaranteeType').on('change', function (e) {

                        if ($('#lstGuaranteeType').val()==2) {
                            $("#Contract_GuaranteePercentage").removeAttr("disabled");
                            
                        } else {
                            $("#Contract_GuaranteePercentage").attr("disabled", "disabled");
                        }
                    });
                function GetActivities(companyid) {


                    $.ajax({
                        type: 'POST',
                        url: '@Url.Action("GetCompanyActivities", "Projecten")',
                        data: { "companyid": companyid },
                        dataType: 'json',
                        success: function (data) {
                            $("#lstActivities").empty();
                            //array = data;
                            $("#lstActivities").select2({
                                data: data
                            });
                            $("#lstActivitiesOrders").empty();
                            //array = data;
                            $("#lstActivitiesOrders").select2({
                                data: data
                            });
                        },
                        error: function () {
                        }
                    });
                   
                    $("#lstActivities").select2('enable');
                    //$("#lstActivitiesOrders").select2('enable');
                    };


                    function GetContractActitvities(contractid) {


                    $.ajax({
                        type: 'POST',
                        url: '@Url.Action("GetContractActitvities", "Projecten")',
                        data: { "contractid": contractid },
                        dataType: 'json',
                        success: function (data) {

                            $("#lstActivitiesOrders").empty();
                            //array = data;
                            $("#lstActivitiesOrders").select2({
                                data: data
                            });
                        },
                        error: function () {
                        }
                    });
                        
                        //$("#lstActivitiesOrders").select2('enable');
                        
                    };
                //toevoegen van een activiteit
                $('#btnAddActivities').click(function () {


                    var data = $('#lstActivities').select2('data');
                    if (data) {
                            $.ajax({
                                url: '@Url.Action("AddSelectedActivities", "Projecten")',
                                data: { ActivityId: data.id, ActivityName: data.text },
                                cache: false,
                                traditional: true,
                                type: 'POST',
                                success: function (result) {
                                    $('#ActivityRows').append(result);
                                },

                            });

                    }

                    $("#lstActivities option:selected").attr('disabled', 'disabled');
                    $("#lstActivities").val(null).trigger("change");
                    return false;
                });
                //verwijderen van activiteit
                $(document).on('click', 'a.deleterow', function () { // <-- changes
                    var $row = $(this).closest('tr')
                    var $id = $row.find('td').eq(0).text();
                    $("#lstActivities option[value=" + $id + "]").removeAttr('disabled').change();
                    $(this).closest('tr').remove();
                    return false;
                });
                    //Toevoegen Bijbestelling rij
                    $('#btnAddAddtionalOrder').click(function () {


                    var data = $('#lstActivitiesOrders').select2('data');
                    if (data) {
                            $.ajax({
                                url: '@Url.Action("AddAdditionalOrders", "Projecten")',
                                data: { ActivityId: data.id, ActivityName: data.text },
                                cache: false,
                                traditional: true,
                                type: 'POST',
                                success: function (result) {
                                    $('#AdditionalOrderRows').append(result);
                                },

                            });

                        }
                        return false;
                });
                </script>
                <script src="~/scripts/autoNumeric/autoNumeric.js"></script>
                <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
                <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
                <script src="~/Scripts/admin/pages/examples.mediagallery.js"></script>
                <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
                <script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
                <script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
                <script src="~/vendor/admin/jquery-datatables/extras/TableTools/js/dataTables.tableTools.min.js"></script>
                <script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
                <script src="~/scripts/admin/tables/examples.datatables.default.js"></script>
                <script src="~/vendor/admin/select2/select2.js" ></script>
                <script src="~/vendor/admin/select2/select2_locale_nl.js" ></script>
                <script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
                <script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
            End Section
