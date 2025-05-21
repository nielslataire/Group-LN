@ModelType ProjectSalesCalculatePrice
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Raming aankoopprijs @Model.ProjectName</title>

    <!-- Web Fonts  -->
    @*<link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">*@

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body style="font-size:12px;line-height:1;">
    <div class="invoice">

        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <div>
                        <table>
                            <tr>
                               <td><img src="~/img/logoimg.jpg" width="50" height="50"></td> <td style="font-size:16px;font-weight:700;">Raming aankoopprijs @Model.ProjectName</td>
                            </tr>
                        </table>
                    </div>
                </div>
                <hr />
                <div>
                    <div>
                        <table class="table mb-none">
                            @For Each unit In Model.Units
                                @<text>
                                    <tr class="primary text-weight-semibold">
                                        <td colspan="2" width="80%">
                                            @unit.Base.Type.Name - @unit.Base.Name.ToLower()
                                        </td>
                                        <td class="text-right">
                                            @String.Format("{0:C}", unit.Base.TotalValue)

                                        </td>
                                    </tr>


                                    <tr>
                                        <td width="5%"></td>
                                        <td>Grondwaarde</td>
                                        <td class="text-right">@String.Format("{0:C}", unit.Base.LandValue)</td>
                                    </tr>
                                @for Each cv In unit.Base.ConstructionValues
                                    @<text> 
                                    <tr>
                                        <td></td>
                                        <td>@cv.Description</td>
                                        <td Class="text-right">@String.Format("{0:C}", cv.Value)</td>
                                    </tr>
                                    </text>
                                Next

                                </text>

                            Next
                            @If Model.SalesSettings.MixedVatRegistration = False Then
                                @<text>
                                    <tr class="primary text-weight-semibold">
                                        <td colspan="2">
                                             Registratierechten
                                        </td>
                                        <td class="text-right">
                                           @String.Format("{0:C}", CalculatePrice.CalculateTotalRegistration(Model.Units.Sum(Function(m) m.Base.LandValue), Model.SalesSettings.RegistrationPercentage, Model.OneAndOwnHome))
                                        </td>
                                    </tr>
                                </text>

                                @For Each unit In Model.Units
                                    @<text>
                                        <tr>
                                            <td></td>
                                            <td>Registratie grondwaarde @Unit.Base.Type.Name.ToLower()  </td>
                                            <td class="text-right">@String.Format("{0:C}", (unit.Base.LandValue) / 100 * Model.SalesSettings.RegistrationPercentage)</td>
                                        </tr>
                                    </text>
                                Next
                                @if Model.OneAndOwnHome = True Then
                                    @<text>
                                        <tr>
                                               <td></td>
                                            <td>Korting enige eigen woning</td>
                                            <td class="text-right">@String.Format("{0:C}", -((Model.Units.Sum(Function(m) m.Base.LandValue) / 100 * Model.SalesSettings.RegistrationPercentage) - CalculatePrice.CalculateTotalRegistration(Model.Units.Sum(Function(m) m.Base.LandValue), Model.SalesSettings.RegistrationPercentage, Model.OneAndOwnHome))) </td>
                                        </tr>
                                    </text>

                                End If
                                                   End If
                                                   <tr class="primary text-weight-semibold">
                                <td colspan="2">Notariskosten</td>
                                <td class="text-right ">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration) + Model.SalesSettings.FixedCertificateCost + Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count + Model.SalesSettings.MortageRegistrationCost)</td>
                            </tr>
                            <tr>
                                                           <td></td>
                                <td>Notariskosten</td>
                                <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration))</td>
                            </tr>
                            <tr>
                                                               <td></td>

                                <td> Vaste Aktekost</td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.FixedCertificateCost)</td>
                            </tr>
                            <tr>
                                                                   <td></td>
                                <td> Aandeel Basisakte</td>
                                @If Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count > 1 Then
                                    @<text>
                                        <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                                    </text>
                                Else
                                    @<text>
                                        <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.BaseCertificateCost)</td>
                                    </text>
                                End If

                            </tr>
                            <tr>
                                                                           <td></td>
                                <td> Hypotheekkantoor</td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.MortageRegistrationCost)</td>
                            </tr>
                            <tr class="primary text-weight-semibold">
                                <td colspan="2">@Html.LabelFor(Function(m) m.SalesSettings.SurveyorCost)</td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.SurveyorCost)</td>
                            </tr>
                            <tr class="primary text-weight-semibold">
                                <td colspan="2">Raming Aansluitkosten</td>
                                @If Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count > 1 Then
                                    @<text>
                                        <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.ConnectionFees * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                                    </text>
                                Else
                                    @<text>
                                        <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.ConnectionFees)</td>
                                    </text>
                                End If

                            </tr>

                                <tr class="primary text-weight-semibold">
                                    <td colspan="2">
                                        BTW
                                    </td>
                                            <td class="text-right">
                                                @String.Format("{0:C}", CalculatePrice.CalculateTotalVat(Model.Units, Model.SalesSettings))
                                            </td>                                


                                </tr>

                            @For Each unit In Model.Units
                            @for each constructionvalue In unit.Base.ConstructionValues
                                @<text>
                                    <tr>
                                                                                                       <td></td>
                                        <td>BTW op @constructionvalue.Description - @unit.Base.Type.Name.ToLower() @unit.Base.Name.ToLower()</td>
                                        @if Model.SalesSettings.MixedVatRegistration = True Then
                                            @<text>
                                                <td Class="text-right">@String.Format("{0:C}", (constructionvalue.Value + unit.Base.LandValue) / 100 * Model.SalesSettings.VatPercentage)</td>
                                            </text>
                                        Else
                                            @<text>
                                                <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateConstructionPriceVat(constructionvalue))</td>
                                            </text>
                                        End If
                                    </tr>
                                </text>
                            Next
                                                                                                               Next
                                                                                                               <tr>
                                                                                                               <td></td>
                                <td>BTW op notariskosten</td>
                                <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration) / 100 * Model.SalesSettings.VatPercentage)</td>
                            </tr>
                            <tr>
                                                                                                                   <td></td>
                                <td>BTW op vaste aktekost</td>
                                <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.FixedCertificateCost / 100 * Model.SalesSettings.VatPercentage)</td>
                            </tr>
                            <tr>
                                                                                                                       <td></td>
                                <td>BTW op aandeel basisakte</td>
                                <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                            </tr>
                            <tr>
                                                                                                                           <td></td>
                                <td>BTW op aansluitkosten</td>
                                <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.ConnectionFees * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                            </tr>
                            <tr>
                                <td></td>
                                <td>BTW op @Html.LabelFor(Function(m) m.SalesSettings.SurveyorCost)</td>
                                <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.SurveyorCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                            </tr>
                            <text>
                                                                                                                               <tr class="primary text-weight-bold">
                                    <td colspan="2">
                                        Totale aankoopprijs
                                    </td>
                                    <td class="text-right">
                                        @String.Format("{0:C}", CalculatePrice.CalculateTotalPrice(Model.Units, Model.SalesSettings, Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count(), Model.OneAndOwnHome))
                                    </td>
                                </tr>
                            </text>
                        </table>
                       
                    </div>
                </div>
            </section>

        </div>
    </div>
</body>
</html>