@ModelType LeverancierModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Detail Afdrukken</title>
  
    <!-- Web Fonts  -->
    <link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css"  />
</head>
<body>
    <div class="invoice">
        <header class="clearfix">
            <div class="row">
                <div class="col-sm-6 mt-md">
                    <h2 class="h2 mt-none mb-sm text-dark text-weight-bold">
                        @Html.ValueFor(Function(m) m.Company.Bedrijfsnaam)
                </h2>
                <h4 class="h4 m-none text-dark text-weight-bold">#@Html.ValueFor(Function(m) m.Company.CompanyId)</h4>
            </div>
            <div class="col-sm-6 text-right mt-md mb-md">
                <address class="ib mr-xlg">
                    @Html.ValueFor(Function(m) m.Company.Straat) @Html.ValueFor(Function(m) m.Company.Huisnummer)@Html.ValueFor(Function(m) m.Company.Toevoeging) @If Model.Company.Busnummer IsNot Nothing Then
                        @: /
                        @Html.ValueFor(Function(m) m.Company.Busnummer)
                    End If

                    <br />
                    @Html.ValueFor(Function(m) m.Company.Postcode.Postcode) @Html.ValueFor(Function(m) m.Company.Postcode.Gemeente).ToString.ToUpper
                    <br />
                    @Html.ValueFor(Function(m) m.Company.Postcode.Provincie.Name)
                    <br />
                    @Html.ValueFor(Function(m) m.Company.Postcode.Country.Name).ToString.ToUpper
                    <br />
                    @If Model.FormattedTelefoon IsNot Nothing Then
                        @:Telefoon :
                        @Html.DisplayFor(Function(m) m.FormattedTelefoon)
                        @:<br />
                            End If
                    @If Model.FormattedGSM IsNot Nothing Then
                        @:GSM :
                        @Html.DisplayFor(Function(m) m.FormattedGSM)
                        @:<br />
                            End If
                    @Html.ValueFor(Function(m) m.Company.Email)
                </address>

            </div>
        </div>
    </header>
    <div class="bill-info">
        <div class="row">
            <div class="col-md-6">
                <div class="bill-to">
                    <p class="h5 mb-xs text-dark text-weight-semibold">Detail:</p>
                    <address>
                        @If Model.Company.URL IsNot Nothing Then
                            @:Website :
                            @:@Html.ValueFor(Function(m) m.Company.URL)
                            @:<br />
                            End If
                        @If Model.Company.OndNr IsNot Nothing Then
                            @:Ondernemingsnummer :
                            @Html.ValueFor(Function(m) m.FormattedONDNR)
                            @:<br />
                            End If
                        @If Model.Company.Opmerking IsNot Nothing Then
                            @: <p class="h5 mb-xs text-dark text-weight-semibold">Opmerkingen:</p>
                            @Html.ValueFor(Function(m) m.Company.Opmerking)
                            @:<br />
                            End If
                    </address>

                </div>
            </div>

        </div>
    </div>
    @If Not Model.Company.Activities.Count = 0 Then
        @<text>
            <div class="table-responsive">
                <p class="h4 mb-xs text-dark text-weight-semibold">Activiteiten:</p>
                <br />
                <table class="table invoice-items  mb-none ">
                    <thead>
                        <tr class="h5 text-dark">
                            <th id="cell-id" class="text-weight-semibold">#</th>
                            <th id="cell-desc" class="text-weight-semibold">Activiteit</th>

                        </tr>
                    </thead>
                    <tbody>
                        @For Each item In Model.Company.Activities
                        Html.RenderPartial("ActivityRow", item, New ViewDataDictionary() From {{"mode", "print"}})
                        Next

                    </tbody>
                </table>
            </div>
        </text>
    End If
    @If Not Model.Company.Departments.Count = 0 Then
        @<text>
            <div class="table-responsive">
                <p class="h4 mb-xs text-dark text-weight-semibold">Afdelingen:</p>
                <br />
                <table class="table invoice-items mb-none ">
                    <thead>
                        <tr class="h5 text-dark">

                            <th id="cell-item" class="text-weight-semibold">Naam</th>
                            <th id="cell-item" class="text-weight-semibold">Straat</th>
                            <th id="cell-desc" class="text-weight-semibold">Gemeente</th>
                            <th id="cell-desc" class="text-weight-semibold">Land</th>
                            <th id="cell-desc" class="text-weight-semibold">Telefoon</th>
                            <th id="cell-desc" class="text-weight-semibold">GSM</th>
                            <th id="cell-item" class="text-weight-semibold">Email</th>
                        </tr>
                    </thead>
                    <tbody>
                        @For Each item In Model.Company.Departments
                        Html.RenderPartial("DepartmentRow", item, New ViewDataDictionary() From {{"mode", "print"}})
                        Next

                    </tbody>
                </table>
            </div>
        </text>
    End If
    @If Not Model.Company.Contacts.Count = 0 Then
        @<text>
            <div class="table-responsive">
                <p class="h4 mb-xs text-dark text-weight-semibold">Contacten:</p>
                <br />
                <table class="table invoice-items mb-none ">
                    <thead>
                        <tr class="h5 text-dark">


                            <th id="cell-desc" class="text-weight-semibold">Naam</th>
                            <th id="cell-desc" class="text-weight-semibold">Functie</th>
                            <th id="cell-desc" class="text-weight-semibold">Afdeling</th>
                            <th id="cell-desc" class="text-weight-semibold">Telefoon</th>
                            <th id="cell-desc" class="text-weight-semibold">GSM</th>
                            <th id="cell-item" class="text-weight-semibold">Email</th>

                        </tr>
                    </thead>
                    <tbody>
                        @For Each item In Model.Company.Contacts
                        Html.RenderPartial("ContactRow", item, New ViewDataDictionary() From {{"mode", "print"}})
                        Next

                    </tbody>
                </table>
            </div>
        </text>
    End If

</div>
    <script>
			window.print();
    </script>
</body>
</html>
