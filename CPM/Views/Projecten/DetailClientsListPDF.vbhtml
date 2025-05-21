@imports bo
@ModelType DetailClientsExportModel
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
        @If (Model.ClientAccounts IsNot Nothing AndAlso Model.ClientAccounts.Count > 0) Then
            @<text>

        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <div>
                        <table>
                            <tr>
                                <td><img src="~/img/logoimg.jpg" width="50" height="50"></td><td style="font-size:16px;font-weight:700;">Klantenlijst @Model.ProjectName</td>
                            </tr>
                        </table>
                    </div>
                </div>
                <hr />
                <div>
                    <div>
                        <table class="table table-bordered table-striped mb-none">
                            <thead >
                                <tr>
                                    <th width="15%">Klantnaam</th>
                                    <th width="13%">Adres</th>
                                    <th width="10%">Gemeente</th>
                                    <th width="6%">Telefoon</th>
                                    <th width="6%">Mobiel</th>
                                    <th width="16%">Email</th>
                                    <th width="7%">Verkoopdatum</th>
                                    <th width="7%">Aktedatum</th>
                                    <th width="3%">Tienduiz.</th>
                                    @for Each type In Model.UnitTypes
                                        @<text>
                                         <th>@type.Shortcode</th>
                                    </text>
                                    Next
                                </tr>
                            </thead>
                            <tbody>

                            @For Each item In Model.ClientAccounts
                                                            @<text>
                                                                <tr class="text-sm m-sm">
                                                                    <td data-title="Naam">
                                                                        @If item.Client.Name IsNot Nothing Then
                                                                            @item.Client.Salutation.GetDisplayName() @Html.Raw(" ") @Html.ValueFor(Function(m) item.Client.DisplayName)
                                                                        Else
                                                                            @Html.ValueFor(Function(m) item.Client.DisplayName)
                                                                        End If
                                                                    </td>
                                                                    <td data-title="Adres">@Html.ValueFor(Function(m) item.Client.Street) @Html.ValueFor(Function(m) item.Client.Housenumber) @Html.ValueFor(Function(m) item.Client.Busnumber) </td>
                                                                    <td data-title="Gemeente">@Html.ValueFor(Function(m) item.Client.Postalcode.Postcode) - @Html.ValueFor(Function(m) item.Client.Postalcode.Gemeente)</td>
                                                                    @If item.Client.Contacts.Count = 1 Then
                                                                        @<text>
                                                                            <td>
                                                                                @Html.DisplayFor(Function(m) item.Client.Contacts.First.FormattedTelefoon)
                                                                            </td>
                                                                            <td>
                                                                                @Html.DisplayFor(Function(m) item.Client.Contacts.First.FormattedGSM)
                                                                            </td>
                                                                            <td>
                                                                                @Html.ValueFor(Function(m) item.Client.Contacts.First.Email)
                                                                            </td>
                                                                        </text>
                                                                    Else
                                                                        @<text>
                                                                            <td></td><td></td><td></td>
                                                                        </text>
                                                                    End If
                                                                    <td data-title="Verkoopdatum">@Html.DisplayFor(Function(m) item.Client.DateSalesAgreement)</td>
                                                                    <td data-title="Aktedatum">
                                                                    @if Not item.Client.DateDeedOfSale Is Nothing Then
                                                                            @Html.DisplayFor(Function(m) item.Client.DateDeedOfSale)
                                                                    End If
                                                                    </td>
                                                                    <td data-title="Tienduiz.">@item.Units.Sum(Function(i) i.Landshare)</td>


                                                                    @for Each type In Model.UnitTypes
                                                                  @<text>
                                                                        <td>


                                                                                @for each unit In item.Units.Where(Function(m) m.Type.Id = type.Id)

                                                                                        @unit.Name
                                                                                    If Not unit Is item.Units.Where(Function(m) m.Type.Id = type.Id).Last Then
                                                                                        @Html.Raw("-")
                                                                                    End If
                                                                                Next
                                                                               
                                                                                </td>

                                                                  </text>
                                                                    Next
             


                                                                </tr>
                                                                @if item.Client.Contacts.Count > 1 Then
                                                                    @<text>
                                                                    
                                                                     <tr >
                                                                         <td colspan="6"  style="padding:0px;">
                                                                             <table class="table m-none p-none mb-none">
                                                                             <tbody>
                                                                                 @for Each contact In item.Client.Contacts
                                                                                    @<text>
                                                                                 <tr Class="b-top-none">
                                                                                     <td></td>
                                                                                     <td>
                                                                                         <Label Class="mb-none">Contactnaam:</Label>
                                                                                     </td>
                                                                                     <td> @contact.Salutation @contact.Name @contact.Firstname</td>
                                                                                     <td> @Html.DisplayFor(Function(m) contact.FormattedTelefoon)</td>
                                                                                     <td> @Html.DisplayFor(Function(m) contact.FormattedGSM)</td>
                                                                                     <td> @contact.Email</td>
                                                                                 </tr>
                                                                                 </text>
                                                                                 Next
                                                                                 
                                                                                 </tbody>
                                                                             </table>
                                                                         </td>
                                                                         <td colspan="@(Model.UnitTypes.Count)"></td>
                                                                     </tr>

                                                                    </text>
                                                                End If

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
</body>
</html>