@Imports BO
@ModelType ClientModel
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
    <style>
        thead{display:table-header-group;}
        tfoot{display:table-row-group;}
        tr{page-break-inside:avoid;}
    </style>
</head>
<body>
    <div class="invoice">
        <header class="clearfix">
            <div class="row">
                <div class="col-sm-12 mt-md text-center">
                    <h2 Class="h2 m-none text-dark text-weight-bold">KLANTENFICHE</h2>
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-6 mt-md">
                        <h4 class="mt-none mb-sm text-dark text-weight-bold">
                            @if Model.Client.CompanyName Is Nothing Then
                        @Model.Client.Salutation.GetDisplayName().ToUpper()
                            End If
                            @Html.Raw(" ") @Model.Client.DisplayName.ToUpper()
                        </h4>
                        @for Each group In Model.UnitsGrouped
                    @<text>
                        <h5 Class="h5 m-none text-dark text-weight-bold">
                            @for Each unit In group.Units
                            @<text>
                                @Html.ValueFor(Function(m) unit.Type.Name) @Html.Raw(" ") @Html.ValueFor(Function(m) unit.Name)
                            </text>         
                                If Not unit Is group.Units.Last Then
                                    @Html.Raw(" - ")
                                End If
                            Next
                        </h5>                  
                    </text>

                    Next
                    </div>
                    <div class="col-sm-6 text-right text-dark mt-md mb-md">

                        @Html.ValueFor(Function(m) m.Client.Street) @Html.ValueFor(Function(m) m.Client.Housenumber)
                        @If Model.Client.Busnumber IsNot Nothing Then
                @<text>
                    / @Html.ValueFor(Function(m) m.Client.Busnumber)
                </text>
                        End If
                        <br />
                        @Html.ValueFor(Function(m) m.Client.Postalcode.Postcode) @Html.ValueFor(Function(m) m.Client.Postalcode.Gemeente).ToString.ToUpper <br />
                        @Html.ValueFor(Function(m) m.Client.Postalcode.Country.Name)

                        <br />
                        @if Model.Client.VATnumber IsNot Nothing Then
                @<text>

                    <strong> BTW-nummer : </strong>@Html.ValueFor(Function(m) m.Client.VATnumber)<br />
                </text>
                        End If

                        <strong> @Html.ValueFor(Function(m) m.Client.OwnerType.Name) :   </strong>@(100 - Model.Client.CoOwners.Select(Function(m) m.CoOwnerPercentage).Sum) @Html.Raw("% ")<br />

                        @If Not Model.Client.DateDeedOfSale Is Nothing Then
                @<text>
                    <strong> Aktedatum :                 </strong>@Html.DisplayFor(Function(m) m.Client.DateDeedOfSale)<br />
                </text>
                        Else
                @<text>
                    <strong> Verkoopdatum :                 </strong>@Html.DisplayFor(Function(m) m.Client.DateSalesAgreement)<br />
                </text>
                        End If


                        <br />

                    </div>
                </div>
</header>

    @If Not Model.Client.Contacts.Count = 0 Then
        @<text>
            <div>
                <p Class="h4 mb-xs text-dark text-weight-semibold">Contacten:</p>
                <br />
                <Table Class="table invoice-items  mb-none ">
                    <thead>
                        <tr Class="h5 text-dark">
                            <th Class="text-weight-semibold" width="40%">Naam</th>
                            <th Class="text-weight-semibold" width="20%">Telefoon</th>
                            <th Class="text-weight-semibold" width="25%">Email</th>
                            <th Class="text-weight-semibold">Mobiel</th>

                        </tr>
                    </thead>
                    <tbody>
                        @For Each contact In Model.Client.Contacts
                        @<text>
                            <tr>
                                <td>@contact.Salutation.GetDisplayName() @Html.Raw(" ") @contact.Name @Html.Raw(" ") @contact.Firstname</td>
                                <td>@Html.DisplayFor(Function(m) contact.FormattedTelefoon)</td>
                                <td>@Html.ValueFor(Function(m) contact.Email)</td>
                                <td>@Html.DisplayFor(Function(m) contact.FormattedGSM)</td>

                            </tr>
                        </text>
                        Next
                    </tbody>
                </table>
                <br />
            </div>
        </text>
    End If
    @If Not Model.Client.CoOwners.Count = 0 Then
        @<text>
            <div>
                <p Class="h4 mb-xs text-dark text-weight-semibold">Mede-Eigenaars:</p>
                <br />
                <Table Class="table invoice-items  mb-none ">
                    <thead>
                        <tr Class="h5 text-dark">
                            <th Class="text-weight-semibold" width="20%">Naam</th>
                            <th Class="text-weight-semibold" width="20%">Adres</th>
                            <th Class="text-weight-semibold">Telefoon</th>
                            <th Class="text-weight-semibold">Mobiel</th>
                            <th Class="text-weight-semibold" width="25%">Email</th>
                            <th Class="text-weight-semibold" width="15%">%</th>
                        </tr>
                    </thead>
                    <tbody>
                        @For Each owner In Model.Client.CoOwners
                            @<text>
                                <tr>
                                    <td>@owner.Salutation.GetDisplayName() @Html.Raw(" ") @owner.Name @Html.Raw(" ") @owner.Firstname</td>
                                    <td>@owner.Street @owner.Housenumber
                                    @If owner.Busnumber IsNot Nothing Then
                                        @<text>
                                            / @owner.Busnumber
                                        </text>
                                    End If
                                    <br />
                                    @owner.Postalcode.Postcode @owner.Postalcode.Gemeente.ToUpper<br />
                                    @owner.Postalcode.Country.Name</td>
                                    <td>@Html.DisplayFor(Function(m) owner.FormattedTelefoon)</td>
                                    <td>@Html.DisplayFor(Function(m) owner.FormattedGSM)</td>
                                    <td>@Html.ValueFor(Function(m) owner.Email)</td>
                                    <td>@owner.CoOwnerPercentage @Html.Raw("% ")<br /> @owner.CoOwnerType.Name</td>
                                </tr>
                            </text>
                        Next
                    </tbody>
                </Table>
                <br />
            </div>
        </text>
    End If
        @If Not Model.Gifts.Count = 0 Then
            @<text>
                <div>
                    <p Class="h4 mb-xs text-dark text-weight-semibold">Toegiften:</p>
                    <br />
                    <Table Class="table invoice-items  mb-none ">
                        <thead>
                            <tr Class="h5 text-dark">
                                <th id="cell-id" Class="text-weight-semibold">Loten</th>
                                <th id="cell-id" Class="text-weight-semibold">Omschrijving</th>
                               
                            </tr>
                        </thead>
                        <tbody>
                           @For Each item In Model.Gifts
                            @<Text>
                            <tr>
                                <td>
                                    @for Each activity In item.Activities
                                        @activity.Name @Html.Raw("<br/>")
                                    Next
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
                </div>
            </text>
        End If
        @If Not Model.Poas.Count = 0 Then
            @<text>
                <div>
                    <p Class="h4 mb-xs text-dark text-weight-semibold">Aandachtspunten:</p>
                    <br />
                    <Table Class="table invoice-items  mb-none ">
                        <thead>
                            <tr Class="h5 text-dark">
                                <th id="cell-id" Class="text-weight-semibold">Loten</th>
                                <th id="cell-id" Class="text-weight-semibold">Omschrijving</th>

                            </tr>
                        </thead>
                        <tbody>
                            @For Each item In Model.Poas
                                @<Text>
                                <tr>
                                    <td>
                                        @for Each activity In item.Activities
                                            @activity.Name @Html.Raw("<br/>")
                                        Next
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
                </div>
            </text>
        End If
</div>
</body>
</html>
