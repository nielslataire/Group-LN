@imports bo
@ModelType ProjectChangeOrderExportModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html style="height:100%">
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Wijzigingsopdracht @Model.Project.Name - @Model.ClientAccount.DisplayName </title>

    <!-- Web Fonts  -->
    @*<link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">*@

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body style="font-size:10px;line-height:1">
    <div class="invoice">
   

        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <table class="table table-bordered" style="height:100%;margin-top:40px;">
                        <tr class="primary"><td style="font-size:24px;text-align:center;font-weight:bold;margin-top:40px;" colspan="3">Wijzigingsopdracht - WO @Html.DisplayFor(Function(m) Model.ChangeOrder.ChangeOrderDate) _ @Model.ChangeOrder.Id</td></tr>
                        <tr style="background-color:#e1e7ac;"><td style="font-size:16px;text-align:center;" colspan="3"><strong>Project : </strong>@Model.Project.Name , @Model.Project.Street @Model.Project.HouseNumber te @Model.Project.Postalcode.Postcode @Model.Project.Postalcode.Gemeente </td></tr>
                        <tr><td style="font-size:16px;text-align:center;" colspan="3"><strong>Opdracht : </strong>@Model.ChangeOrder.Description </td></tr>
                        <tr><td style="font-size:16px;text-align:center;" colspan="3"><strong>Klant : </strong>@Model.ClientAccount.Salutation.GetDisplayName() @Model.ClientAccount.DisplayName<br />@Model.Units </td></tr>
                        <tr>
                        <td colspan="3">
                            <table style="border:none;width:100%;font-size:16px;margin-top:40px;">
                            <tr style="border-bottom:1px solid;height:25px">
                                <td width="20px">&nbsp;</td>
                                <td style="border-right:1px dotted;padding-left:5px;">Omschrijving</td>
                                <td colspan="2"width="20%" style="border-right:1px dotted;padding-left:5px;">eenheid</td>
                                
                                <td width="10%" style="border-right:1px dotted;padding-left:5px;">hoev.</td>
                                <td width="15%" style="border-right:1px dotted;padding-right:5px;text-align:right;">EH-prijs</td>
                                <td width="15%" style="padding-left:5px;text-align:right;">Totaal</td>
                            </tr>
                                <tr>
                                    <td></td>
                                    <td style="border-right:1px dotted;padding-left:5px;"></td>
                                    <td style="border-right:1px dotted;padding-left:5px;"></td>
                                    <td style="border-right:1px dotted;padding-left:5px;"></td>
                                    <td style="border-right:1px dotted;padding-left:5px;"></td>
                                    <td style="border-right:1px dotted;padding-left:5px;"></td>
                                    <td style="padding-left:5px;"></td>
                                </tr>
                                @for Each detail As ChangeOrderDetailBO In Model.ChangeOrder.Details
                                    @<text>
                                <tr style="height:50px;">
                                    <td></td>
                                    <td style="border-right:1px dotted;padding-left:5px;">@detail.Description </td>
                                    <td width="10%" style="border-right:1px dotted;padding-left:5px;">@detail.MeasurementUnit.GetDisplayName()</td>
                                    <td width="10%" style="border-right:1px dotted;padding-left:5px;">@detail.MeasurementType.GetDisplayName()</td>
                                    <td width="10%" style="border-right:1px dotted;padding-left:5px;" >@detail.Number </td>
                                    <td width="15%" style="border-right:1px dotted;padding-right:5px;text-align:right;">@String.Format("{0:C}", ((detail.Price * detail.Commision / 100) + detail.Price)) </td>
                                    <td width="15%" style="padding-left:5px;text-align:right;">@String.Format("{0:C}", (((detail.Price * detail.Commision / 100) + detail.Price) * detail.Number))</td>
                                </tr>
                                </text>
                                Next

                            </table>
                        </td>
                        </tr>
                        @if Not Model.ChangeOrder.Comment Is Nothing Then
                            @<text>
                                        <tr><td style = "font-size:14px;text-align:left;padding-left:25px" colspan="3"><strong>Opmerkingen : </strong><br/><br/>@Html.Raw(Model.ChangeOrder.Comment)</td></tr>
                        </text>
                        End If
                        <tr><td style="font-size:16px;text-align:center;" colspan="3">
                                <table style="border:none;width:100%;font-size:16px;margin-top:40px;">
                                    <tr style="height:25px">
                                <td width="20px">&nbsp;</td>
                                <td style="padding-left:5px;text-align:left;"  colspan="3">Totale som van de opdracht (excl. BTW) :</td>
                                <td width="10%"></td>
                                <td width="15%"></td>
                                <td width="15%" style="padding-left:5px;text-align:right;">@String.Format("{0:C}", Model.ChangeOrder.Totaal)</td>
                                    </tr>
                                    <tr style="height:25px">
                                        <td width="20px">&nbsp;</td>
                                        <td style="padding-left:5px;text-align:left;"  colspan="3">BTW bedrag :</td>
                                        <td width="10%"></td>
                                        <td width="15%"></td>
                                        <td width="15%" style="padding-left:5px;text-align:right;">@String.Format("{0:C}", Model.ProjectSalesSettings.VatPercentage * Model.ChangeOrder.Totaal / 100)</td>
                                    </tr>
                                    <tr style="height:25px">
                                        <td width="20px">&nbsp;</td>
                                        <td style="padding-left:5px;text-align:left;" colspan="3"><strong>Totale som van de opdracht (incl. BTW) :</strong></td>
                                        <td width="10%"></td>
                                        <td width="15%"></td>
                                        <td width="15%" style="padding-left:5px;text-align:right;"><strong>@String.Format("{0:C}", (Model.ProjectSalesSettings.VatPercentage * Model.ChangeOrder.Totaal / 100) + Model.ChangeOrder.Totaal)</strong></td>
                                    </tr>
                                    <tr style="height:25px">
                                        <td colspan="7">&nbsp;</td>  
                                    </tr>
                                    <tr style="height:25px;margin-top:100px;text-align:left;">
                                        <td></td>
                                        <td colspan="6" style="padding-left:5px;font-size:14px;" class="text-warning"><strong>Gelieve, indien u akkoord gaat, deze wijzigingsopdracht voor akkoord ondertekend terug te bezorgen tegen ten laatste @Html.DisplayFor(Function(m) Model.ChangeOrder.ExpirationDate) .</strong></td>
                                    </tr>
                                    </table>
</td></tr>
                    </table>
                    </div>



            </section>

        </div>


    </div>
</body>
</html>