@imports bo
@ModelType DetailClientsGiftsModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Klantenlijst @Model.ProjectName</title>

    <!-- Web Fonts  -->
    @*<link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">*@

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body style="font-size:10px;line-height:1;">
    <div class="invoice">
        @If (Model.ClientGifts IsNot Nothing AndAlso Model.ClientGifts.Count > 0) Then
        @<text>

            <div class="col-sm-12 mt-md">
                <section id="pnlList">

                    <div>
                        <div>
                            <table>
                                <tr>
                                    <td><img src="~/img/logoimg.jpg" width="50" height="50"></td><td style="font-size:16px;font-weight:700;">Toegiften @Model.ProjectName</td>
                                </tr>
                            </table>
                        </div>
                    </div>
                    <hr />
                    <div>
                        <div>
                            @for Each act In Model.SelectedActivities
                        @<text>
                            <p class="h5 mb-xs text-dark text-weight-semibold">Toegiften - @act.Name</p>
                            <br />
                            <Table Class="table table-bordered table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Klantnaam</th>
                                        <th>Wooneenheid</th>
                                        <th>toegift</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @For Each item In Model.ClientGifts.Where(Function(m) m.Activities.Any(Function(i) i.ID = act.ID))
                                @<text>
                                    <tr class="text-sm m-sm">
                                        <td data-title="Naam">
                                            @item.AccountName
                                        </td>
                                        <td>
                                            @item.LivingUnit
                                        </td>
                                        <td>
                                            @item.Description
                                        </td>
                                    </tr>
                                </text>
                                    Next
                                </tbody>
                            </Table>
                            <br />
                        </text>
                            Next
                        </div>
                    </div>
                </section>

            </div>

        </text>

        End If
    </div>
</body>
</html>