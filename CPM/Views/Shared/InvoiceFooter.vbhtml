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
        <table class="table" style="border:0px;">
            <tr class="b-top-none">
                <td>BTW-tarief</td>
                <td class="border color-primary-inverted text-bold" style="text-align: right; border-top: 1px solid black;">0 %</td>
                <td class="border color-primary-inverted text-bold" style="text-align: right; border-top: 1px solid black;">6 %</td>
                <td class="border color-primary-inverted text-bold" style="text-align: right; border-top: 1px solid black;">21 %</td>
                <td class="border color-primary-inverted text-bold" style="text-align:right;border-top:1px solid black;">TOTAAL</td>
            </tr>
            <tr class="b-top-none">
                <td>MVH</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">@String.Format("{0:C}", Model.Rows.Where(Function(i) i.VatPercentage = 0).Sum(Function(l) l.Price))</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">@String.Format("{0:C}", Model.Rows.Where(Function(i) i.VatPercentage = 6).Sum(Function(l) l.Price))</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">@String.Format("{0:C}", Model.Rows.Where(Function(i) i.VatPercentage = 21).Sum(Function(l) l.Price))</td>
                <td class="border" style="text-align:right;border-top:1px solid black;">@String.Format("{0:C}", Model.Rows.Sum(Function(l) l.Price))</td>
            </tr>
            <tr class="b-top-none">
                <td>BTW</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">-</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">@String.Format("{0:C}", Model.Rows.Where(Function(i) i.VatPercentage = 6).Sum(Function(l) l.Price) * 0.06)</td>
                <td class="border" style="text-align: right; border-top: 1px solid black;">@String.Format("{0:C}", Model.Rows.Where(Function(i) i.VatPercentage = 21).Sum(Function(l) l.Price) * 0.21)</td>
                <td class="border" style="text-align:right;border-top:1px solid black;">@String.Format("{0:C}", (Model.Rows.Where(Function(i) i.VatPercentage = 21).Sum(Function(l) l.Price) * 0.21) + (Model.Rows.Where(Function(i) i.VatPercentage = 6).Sum(Function(l) l.Price) * 0.06))</td>
            </tr>
            <tr class="b-top-none">
                <td>&nbsp;</td>
                <td></td>
                <td></td>
                <td></td>
                <td class="border text-bolder" style="text-align:right;border-top:1px solid black;">@String.Format("{0:C}", Model.Rows.Sum(Function(l) l.Price) + (Model.Rows.Where(Function(i) i.VatPercentage = 21).Sum(Function(l) l.Price) * 0.21) + (Model.Rows.Where(Function(i) i.VatPercentage = 6).Sum(Function(l) l.Price) * 0.06))</td>
            </tr>
            <tr class="b-top-none"><td></td></tr>
            <tr class="b-top-none">
                <td colspan="5">Betaalbaar uiterlijk @String.Format("{0:d MMMM yyyy}", Model.ExpirationDate) op rekening <span class="color-primary text-bolder">@Model.BankAccount.ToUpper</span></td>
            </tr>
            @If Model.ExtraInfo = "" Then
                @<text>
                    <tr class="b-top-none">
                        <td colspan="5"><br /></td>
                    </tr>
                </text>
            Else
                @<text>
                    <tr class="b-top-none">
                        <td colspan="5">@Model.ExtraInfo</td>
                    </tr>
                </text>

            End If
        </table>
        <br />
              <table class="table" style="border:0px;font-size:0.7em;padding:1px 5px 1px 5px;">
                  <colgroup>
                      <col span="1" style="width: 15%;">
                      <col span="1" style="width: 20%;">
                      <col span="1" style="width: 20%;">
                      <col span="1" style="width: 45%;">
                  </colgroup>
                  <tbody>
                      <tr class="b-top-none">
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyStreetNr</td>
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyPhone</td>
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyEmail</td>
                          <td colspan="2" style="text-align:right;padding: 1px 5px 1px 5px;">@ViewBag.CompanyName - BTW @ViewBag.CompanyVat - @ViewBag.CompanyRPR</td>
                      </tr>
                      <tr class="b-top-none">
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyPostalcode</td>
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyFax</td>
                          <td style="padding: 1px 5px 1px 5px;">@ViewBag.CompanyWWW</td>
                          <td colspan="2" style="text-align:right;padding: 1px 5px 1px 5px;">IBAN @ViewBag.CompanyBankaccount</td>
                      </tr>
                      </tbody>
              </table>
    </div>

   
</body>
</html>


