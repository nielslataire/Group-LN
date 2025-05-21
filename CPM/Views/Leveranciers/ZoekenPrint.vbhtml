@ModelType LeverancierSearchModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Overzicht Afdrukken</title>

    <!-- Web Fonts  -->
    <link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body style="font-size:10px;line-height:1;">
    <div class="invoice">
        @If (Model.Companies IsNot Nothing AndAlso Model.Companies.Count > 0) Then
            @*@Html.ActionLink("Afdrukken", "ZoekenPrint", "Leveranciers", New With {.id = Model.Company.CompanyId}, New With {.class = "btn btn-primary ml-xs mb-xs hidden-tablet hidden-phone"})*@

            @<text>

        <div class="col-sm-12 mt-md">
            <section class="panel" id="pnlList">
                <header class="panel-heading">
                    <h2 class="panel-title">Overzicht</h2>
                </header>
                <div class="panel-body">
                    <div>
                        <table class="table table-bordered table-striped mb-none">
                            <thead>
                                <tr>
                                    <th>Bedrijfsnaam</th>
                                    <th>Adres</th>
                                    <th>Gemeente</th>
                                    <th>Telefoon</th>
                                    <th>GSM</th>
                                    <th>Email</th>
                                </tr>
                            </thead>
                            <tbody>
                                @* Do a for each on the Model.companies here and display a line per found company *@

                                @For Each company In Model.Companies
                                    @<text>
                                        <tr class="text-sm">
                                            <td data-title="Naam">
                                                @Html.ValueFor(Function(m) company.Bedrijfsnaam)
                                            </td>
                                            <td data-title="Adres">@Html.ValueFor(Function(m) company.Straat) @Html.ValueFor(Function(m) company.Huisnummer)@Html.ValueFor(Function(m) company.Toevoeging) @Html.ValueFor(Function(m) company.Busnummer) </td>
                                            <td data-title="Gemeente">@Html.ValueFor(Function(m) company.Postcode.Gemeente)</td>
                                            <td data-title="GSM">
                                                @If Not company.Telefoon1 Is Nothing Then
                                                    @Html.ValueFor(Function(m) company.FormattedTelefoon)
                                                Else
                                                    @:-
                                        End If
                                            </td>
                                            <td data-title="GSM">
                                                @If Not company.GSM Is Nothing Then
                                                    @Html.ValueFor(Function(m) company.FormattedGSM)
                                                Else
                                                    @:-
                                        End If
                                            </td>
                                            <td data-title="Email">
                                                @If Not company.Email Is Nothing Then

                                                    @<text>
                                                        <div class="hidden-xs">@Html.ValueFor(Function(m) company.Email)</div>

                                                    </text>

                                                Else
                                                    @:-
                                        End If

                                            </td>
                                        </tr>

                                    </text>
                                Next
                            </tbody>
                        </table>
                    </div>
                </div>
            </section>

        </div>

    </text>

        End If
    </div>



        <script>
            window.print();
        </script>
</body>
</html>