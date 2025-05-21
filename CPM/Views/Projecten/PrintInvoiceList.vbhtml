
@ModelType PrintInvoiceListModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Betalingsschijven @Model.ProjectName</title>

    <!-- Web Fonts  -->
    @*<link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">*@

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
    <style>
        .row {
            height: 20px !important;
            padding-top: 0px !important;
            padding-bottom: 0px !important;
        }
    </style>
</head>
<body style="font-size:12px;line-height:1;">
    <div class="invoice">

        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <div>
                        <table>
                            <tr>
                                <td><img src="~/img/logoimg.jpg" width="50" height="50"></td>
                                <td style="font-size:16px;font-weight:700;">Betalingsschijven @Model.ProjectName - @Model.Client.DisplayName</td>
                            </tr>
                        </table>
                    </div>
                </div>
                <hr />
                <div>
                    <div>


                        @For Each item In Model.UnitsWithStages

                            @<text>
                                <br><table class="table table-bordered table-striped mb-none">
                                    <colgroup>
                                        <col style="min-width:30%;" />
                                        <col style="width: 5%;" />

                                    </colgroup>
                                    <thead>

                                        <tr>
                                            <th colspan="2">@item.Unit.Type.Name.ToUpper @Html.Raw(" ") @item.Unit.Name.ToUpper - @String.Format("{0:C}", item.Unit.ConstructionValueSold)</th>
                                            <th colspan="3" width="20%">@Model.Client.DisplayName</th>
                                            @For Each coowner In Model.Client.CoOwners
                                                @<text>
                                                    <th colspan="3" width="20%">@coowner.Name @coowner.Firstname </th>
                                                </text>
                                            Next
                                            <th></th>
                                        </tr>
                                    </thead>
                                    @For Each cv In item.Unit.ConstructionValues
                                        @<text>
                                            <thead>
                                                <tr>
                                                    <th width="30%"> Betalingsschijven - @cv.Description</th>
                                                    <th width="5%">%</th>
                                                    <th>Schijf</th>
                                                    <th>BTW</th>
                                                    <th>Totaal</th>
                                                    @For Each coowner In Model.Client.CoOwners
                                                        @<text>
                                                            <th>Schijf</th>
                                                            <th>BTW</th>
                                                            <th>Totaal</th>
                                                        </text>
                                                    Next
                                                    <th>Factuurnr</th>

                                                </tr>
                                            </thead>

                                            <tbody>



                                                @for Each stage In item.PaymentStages.Where(Function(m) m.GroupId = cv.PaymentGroupId)

                                                    @<text>
                                                        <tr Class="text-xs mb-none mt-none" style="height:5px !important;padding-top:0px !important;padding-bottom:0px !important;">
                                                            <td class="row">
                                                                @stage.Name
                                                            </td>
                                                            <td class="row">
                                                                @stage.Percentage.ToString("0.##") %
                                                            </td>
                                                            @If Model.Client.OwnerType.Id = 1 Then
                                                                @<text>
                                                                    <td class="row">@String.Format("{0:C}", cv.ValueSold * stage.Percentage / 100)</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100) / 100 * stage.VatPercentage)</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100) + ((cv.ValueSold * stage.Percentage / 100) / 100 * stage.VatPercentage))</td>
                                                                </text>
                                                            Else
                                                                @<text>
                                                                    <td class="row">@String.Format("{0:C}", cv.ValueSold * stage.Percentage / 100 / 100 * (100 - Model.Client.CoOwners.Sum(Function(m) m.CoOwnerPercentage)))</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100) / 100 * (100 - Model.Client.CoOwners.Sum(Function(m) m.CoOwnerPercentage)) / 100 * stage.VatPercentage)</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100 / 100 * (100 - Model.Client.CoOwners.Sum(Function(m) m.CoOwnerPercentage))) + ((cv.ValueSold * stage.Percentage / 100) / 100 * (100 - Model.Client.CoOwners.Sum(Function(m) m.CoOwnerPercentage)) / 100 * stage.VatPercentage))</td>
                                                                </text>
                                                            End If
                                                            @For Each coowner In Model.Client.CoOwners
                                                                @<text>
                                                                    <td class="row">@String.Format("{0:C}", cv.ValueSold * stage.Percentage / 100 / 100 * coowner.CoOwnerPercentage)</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100) / 100 * coowner.CoOwnerPercentage / 100 * stage.VatPercentage)</td>
                                                                    <td Class="row">@String.Format("{0:C}", (cv.ValueSold * stage.Percentage / 100 / 100 * coowner.CoOwnerPercentage) + ((cv.ValueSold * stage.Percentage / 100) / 100 * coowner.CoOwnerPercentage / 100 * stage.VatPercentage))</td>
                                                                </text>
                                                            Next
                                                            <td Class="row">
                                                                @For Each invoice In Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id AndAlso i.ConstructionValueId = cv.Id))
                                                                    If invoice Is Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id AndAlso i.ConstructionValueId = cv.Id)).Last Then
                                                                        'do something with your last item'

                                                                        @<text>
                                                                            @(invoice.Filename.Substring(0, 12))

                                                                        </text>
                                                                    Else
                                                                        @<text>
                                                                            @(invoice.Filename.Substring(0, 12)) /

                                                                        </text>
                                                                    End If
                                                                Next
                                                            </td>
                                                        </tr>
                                                        @*@If Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).Count > 0 Then
                                                             @if Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).Count = 1 Then
                                                             @<text>


                                                             </text>       Else
                                                             @<text>

                                                                 <td>@stage.Name</td>
                                                                 <td>@stage.Percentage.ToString("0.##") % ) % </td>
                                                                 <td>

                                                                     @For Each invoice In Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id))
                                                                                             @<text>
                                                                                                 @(invoice.Filename.Substring(0, 12))
                                                                                                 <br />
                                                                                             </text>

                                                                     Next
                                                                 </td>

                                                             </text>
                                                             End If

                                                                     Else
                                                             @<text>
                                                                 <td>@stage.Name

                                                                 <td>@stage.Percentage.ToString("0.##") % </td>
                                                                 <td></td>
                                                             </text>
                                                                     End If



                                                                 </tr>
                                                            </tr>*@
                                                    </text>

                                                Next
                                            </tbody>
                                        </text>
                                    Next
                                </table>
                                <br />
                            </text>
                        Next
                    </div>


                </div>
            </section>

        </div>
    </div>
</body>
</html>