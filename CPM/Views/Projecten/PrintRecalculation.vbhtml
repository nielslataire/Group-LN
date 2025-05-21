
@ModelType ProjectContractsModel
@imports bo
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Nacalculatie @Model.ProjectName</title>

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
            <h1>Nacalculatie @Model.ProjectName</h1>
            <h5>@Date.Now().ToLongDateString() </h5>
            <hr />
            <section id="pnlList">
                <div>
                    <table class="table mb-none" style="page-break-after: always;">

                        <tbody>
                            <tr>
                                <td>Lot</td>
                                <td>Omschrijving</td>
                                <td class="text-right">Budget</td>
                                <td class="text-right">Contract</td>
                                <td class="text-right">Gefactureerd</td>
                                <td class="text-right">Verschil Budget - Contract</td>
                                <td class="text-right">Verschil Budget - Facturatie</td>
                            </tr>
                            @For Each group In Model.ActivityGroups
                                @<text>

                                    <tr class="active">
                                        <td>
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
                                                <td Class="text-right text-danger" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.Group.ID = group.ID)).Sum(Function(s) s.Price))</td>
                                            </text>
                                        End If
                                        @If Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) >= 0 Then
                                            @<text>
                                                <td class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price))</td>
                                            </text>
                                        Else
                                            @<text>
                                                <td class="text-right text-danger" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.Group.ID = group.ID).Sum(Function(t) t.Price))</td>
                                            </text>
                                        End If

                                    </tr>
                                    @for Each activity In group.Activities.Where(Function(m) Model.Contracts.Any(Function(a) a.Activities.Any(Function(i) i.Activity.ID = m.ID)) Or Model.BudgetActivities.Any(Function(l) l.Activity.ID = m.ID) Or Model.IncommingInvoicesActivities.Any(Function(l) l.Activity.ID = m.ID)).OrderBy(Function(o) o.Name)
                                        @<text>

                                            <tr>
                                                <td></td>
                                                <td>
                                                    @activity.Name
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
                                                        <td Class="text-right text-danger" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.Contracts.SelectMany(Function(s) s.Activities.Where(Function(w) w.Activity.ID = activity.ID)).GroupBy(Function(g) g.ContractId).Sum(Function(s) s.Sum(Function(t) t.Price)))</td>

                                                    </text>
                                                End If
                                                @If Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) >= 0 Then
                                                    @<text>
                                                        <td Class="text-right" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price))</td>
                                                    </text>
                                                Else
                                                    @<text>
                                                        <td Class="text-right text-danger" width="10%">@String.Format("{0:C}", Model.BudgetActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price) - Model.IncommingInvoicesActivities.Where(Function(l) l.Activity.ID = activity.ID).Sum(Function(t) t.Price))</td>
                                                    </text>
                                                End If

                                            </tr>

                                        </text>

                                    Next
                                </text>
                            Next
                        </tbody>






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
                    @If ViewBag.detail = 1 Then
                        @For Each group In Model.ActivityGroups
                            @for Each activity In group.Activities.Where(Function(m) Model.IncommingInvoicesActivities.Any(Function(l) l.Activity.ID = m.ID)).OrderBy(Function(o) o.Name)
                                @<text>

                                    <h1>Nacalculatie @Model.ProjectName - @activity.Name</h1>
                                    <hr />
                                </text>
                                @For Each comp In Model.IncommingInvoicesActivities.Where(Function(i) i.Activity.ID = activity.ID).GroupBy(Function(m) m.Company.ID).Select(Function(l) l.First()).ToList()
                                    @<text>

                                        <h3 class="mt-lg">@comp.Company.Display </h3>
                                        @For Each type In Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID).GroupBy(Function(l) l.IncommingInvoiceType).Select(Function(s) s.First()).ToList().OrderBy(Function(o) o.IncommingInvoiceType)
                                            @<text>
                                                <h4 class="text-weight-bold text-uppercase">@type.IncommingInvoiceType.GetDisplayName()</h4>
                                                <Table Class="table mb-lg ">
                                                    <thead>
                                                        <tr>
                                                            <th width="10%"> FACT NR.</th>
                                                            <th width="10%"> DATUM</th>
                                                            <th> OMSCHRIJVING</th>
                                                            <th Class="text-right" width="10%">PRIJS</th>
                                                            
                                                        </tr>
                                                    </thead>
                                                    <tbody>



                                                        @For Each invoice In Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType AndAlso m.Activity.ID = activity.ID).OrderBy(Function(o) o.Invoicedate)
                                                            @<text>
                                                                <tr>
                                                                    <td >@invoice.ExternalInvoiceId</td>
                                                                    <td>@Html.DisplayFor(Function(m) invoice.Invoicedate)  </td>
                                                                    <td>@invoice.Description</td>
                                                                    <td Class="text-right">@Html.DisplayFor(Function(m) invoice.Price)</td>
                                                                </tr>
                                                            </text>

                                                        Next

                                                    </tbody>
                                                    <tfoot>
                                                        <tr Class="active">
                                                            <td colspan="3">TOTAAL</td>
                                                            <td Class="text-right">
                                                                @String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(m) m.Company.ID = comp.Company.ID AndAlso m.IncommingInvoiceType = type.IncommingInvoiceType AndAlso m.Activity.ID = activity.ID).Sum(Function(s) s.Price))
                                                            </td>
                                                        </tr>
                                                    </tfoot>

                                                </Table>
                                            </text>
                                        Next


                                    </text>

                                Next

                                @<text>
                                    <br />
                                    <Table Class="table mb-lg" style="page-break-after: always;">
                                       
                                            <tr class="primary">
                                                <td colspan="3" class="text-uppercase">TOTAAL @activity.Name.ToUpper() </td>
                                                <td Class="text-right mr-lg pr-lg " width="10%">
                                                    @String.Format("{0:C}", Model.IncommingInvoicesActivities.Where(Function(i) i.Activity.ID = activity.ID).Sum(Function(s) s.Price))</td>
                                            
                                            </tr>
                                        </Table>
                                </text>

                            Next
                       next
                       End If
</div>
</section>

</div>
</div>
</body>
</html>
