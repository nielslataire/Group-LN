@imports bo
@ModelType InvoiceBO
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Factuur @Model.PublicId - @Model.ClientName </title>

    <!-- Web Fonts  -->
    @*<link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">*@

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoicePDF.css" />
</head>
<body>
    <div>

        <header class="clearfix">
            <div class="row"></div>
        </header>
        <div class="">
            <table class="table " style="border:0px;">
                <tr class="b-top-none sm">
                    <td colspan="5"><img src="~/img/logos/LN-GROUP-BCO.jpg" height="110"><br /><br /></td>
                    
                </tr>
                <tr class="b-top-none sm" style="padding:2px 5px 2px 5px;">
                    <td colspan="3" style="padding:2px 5px 2px 5px;"></td>
                    <td colspan="2" style="letter-spacing:normal;font-family:Avenir-Heavy;padding:2px 5px 2px 5px;">@Model.ClientName.ToUpper</td>
                </tr>
                <tr class="b-top-none" style="padding:2px 5px 2px 5px;">
                    <td colspan="3" style="padding:2px 5px 2px 5px;"></td>
                    <td colspan="2" style="padding:2px 5px 2px 5px;">@Model.Adress</td>

                </tr>
                <tr class="b-top-none" style="padding:2px 5px 2px 5px;">
                    <td colspan="3" style="padding:2px 5px 2px 5px;"></td>
                    <td colspan="2" style="padding:2px 5px 2px 5px;">@Model.PostalCode.Postcode - @Model.PostalCode.Gemeente</td>

                </tr>
                <tr class="b-top-none" style="padding:2px 5px 2px 5px;">

                    <td class="color-primary" style="font-size: 1.2em;font-family: Avenir-Black;color:#009336" ">FACTUUR</td>
                    <td class="color-primary" style="font-size: 1.2em;font-family: Avenir-Black;" colspan="2">@Model.PublicId</td>
                    @If Model.PostalCode.Country.CountryID = 19 Then
                        @<text>
                            <td colspan="2" style="padding:2px 5px 2px 5px;"></td>
                        </text>
                    Else
                        @<text>
                            <td colspan="2" style="padding:2px 5px 2px 5px;">@Model.PostalCode.Country.Name.ToUpper</td>
                        </text>
                    End If

                </tr>
                <tr class="b-top-none">

                    <td style="font-family:Avenir-Heavy;">DATUM</td>
                    <td colspan="2">@String.Format("{0:d MMMM yyyy}", Model.Invoicedate)</td>
                    <td colspan="2"></td>

                </tr>

                <tr class="b-top-none">

                    <td style="font-family:Avenir-Heavy;">UW BTW NR.</td>
                    <td colspan="2">@Model.VatNumber</td>
                    <td colspan="2"></td>

                </tr>
                <tr class="b-top-none">

                    <td style="font-family:Avenir-Heavy;">VERVALDATUM</td>
                    <td colspan="2">@String.Format("{0:d MMMM yyyy}", Model.ExpirationDate)</td>
                    <td colspan="2"></td>

                </tr>
            </table>
        </div>
        <br />
        <br />
        <div>
            <table class="table invoice-items">
                <colgroup>
                    <col span="1" style="width: 70%;">
                    <col span="1" style="width: 10%;">
                    <col span="1" style="width: 20%;">
                </colgroup>
                <tbody>
                    <tr class="b-top-none">

                        <td colspan="3">@Model.Text</td>

                    </tr>

                    <tr class="b-top-none" style="font-family:Avenir-Heavy;">

                        <td colspan="3">@Html.Raw(Model.DetailText)</td>

                    </tr>

                    <tr Class="b-top-none" style="font-family:Avenir-Heavy;">

                        <td colspan = "3" >&nbsp;</td>
                    </tr>
                    <tr Class="b-top-none" style="font-family:Avenir-Heavy;">

                        <td> STAND VAN DE WERKEN</td>
                        <td> BTW %</td>
                        <td style = "text-align:right;" > PRIJS</td>

                    </tr>
                    @For Each grouprow In Model.Rows.GroupBy(Function(m) m.GroupName)
                        @<text>
                            <tr class="b-top-none">
                                <td style="text-decoration-style:solid;"><u>@grouprow.Key.ToUpper</u></td>
                                <td></td>
                                <td style="text-align:right;"></td>
                                                                </tr>
                        </text>
                        @For Each row In Model.Rows.Where(Function(i) i.GroupName = grouprow.Key).OrderBy(Function(o) o.Id)
                            @<text>
                                <tr class="b-top-none m-none p-none">
                                    <td class="b-top-none m-none p-none" style="padding:2px 5px 2px 5px;">@row.Text</td>
                                    <td class="b-top-none m-none p-none" style="padding:2px 5px 2px 5px;">@Html.DisplayFor(Function(m) row.VatPercentage)</td>
                                    <td class="b-top-none m-none p-none" style="text-align:right;padding:2px 5px 2px 5px;">@Html.DisplayFor(Function(m) row.Price)</td>
                                </tr>
                            </text>
                        Next
                                                    Next


                </tbody>
            </table>
        </div>


    </div>
</body>
</html>