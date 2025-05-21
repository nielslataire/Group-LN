@imports bo
@modeltype DetailClientsModel

@section PageStyle

    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

End Section

<div class="inner-toolbar clearfix">
    <ul>
        @If User.IsInRole("Admin") Then
        @<text>
            <li>
                <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Toevoegen klant" href="@Url.Action("AddClientAccount", "Klanten", New With {.id = Model.ProjectId})" class="AddClientAccount"><i class="fa fa-user-plus"></i></a>
            </li>
           
        </text>
        End If
        <li>
            <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Wijzigingsopdrachten" href="@Url.Action("ChangeOrder", "Projecten", New With {.projectid = Model.ProjectId})" Class="ChangeOrder"><i Class="fa fa-shopping-cart"></i></a>
        </li>
    </ul>
</div>


        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
        <h2 class="panel-title">Overzicht klanten</h2>
<br />
       <section class="panel">

    <div class="panel-body">

            <table class="table table-no-bordered table-no-more table-striped" data-toggle="table" data-show-columns="true" data-toolbar="#toolbar" data-toolbar-align="right">
                <thead>
                    <tr>
                        <th>Naam</th>
                       


                        @If Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 1).Count) > 0 Then

                            @<text>
                        <th>Wooneenheid</th>
                         </text>
                        Else

                        End If
                        @if Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 4).Count) > 0 Then
                            @<text>
                                <th class="hidden-xs">Commerciële ruimte</th>
                            </text>

                        End If
                        @if Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 3).Count) > 0 Then
                            @<text>
                                <th class="hidden-xs">Parkeergelegenheid</th>
                            </text>

                        End If
                        @if Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 2).Count) > 0 Then
                            @<text>
                                <th class="hidden-xs">Berging</th>
                            </text>

                        End If   
                        <th class="hidden-xs" data-visible="false" data-sortable="true">Verkoopdatum</th>
                        <th class="hidden-xs" data-visible="false" data-sortable="true">Aktedatum</th>
                        <th class="hidden-xs" data-visible="false" data-sortable="true">Opleverdatum</th>                 
                        <th class="hidden-xs">Acties</th>
                    </tr>
                </thead>
                <tbody>

               @For Each item In Model.ClientAccounts

                                    @<text>

                                        <tr>
                                            <td>
                                                <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Klantgegevens" href="@Url.Action("Detail", "Klanten", New With {.clientid = item.Client.Id, .projectid = Model.ProjectId})">
                                                    @If item.Client.Name IsNot Nothing Then
                                                    @item.Client.Salutation.GetDisplayName() @Html.Raw(" ") @item.Client.DisplayName
                                                    Else
                                                    @item.Client.DisplayName
                                                    End If
                                            </a>
                                                    </td>
                                            @If Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 1).Count) > 0 Then
                                                @<text>
                                                    <td>

                                                        @for Each unit In item.Units.Where(Function(m) m.Type.GroupId = 1)
                                                        @<text>
                                                            @unit.Type.Name @unit.Name
                                                        </text>
                                                            If Not unit Is item.Units.Where(Function(m) m.Type.GroupId = 1).Last Then
                                                        @Html.Raw(" - ")
                                                            End If
                                                        Next


                                                    </td>
                                                </text>
                                            End If
                                            @If Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 4).Count) > 0 Then
                                                @<text>
                                                    <td class="hidden-xs">

                                                        @for Each unit In item.Units.Where(Function(m) m.Type.GroupId = 4)
                                                            @<text>
                                                                @unit.Type.Name @unit.Name
                                                            </text>
                                                            If Not unit Is item.Units.Where(Function(m) m.Type.GroupId = 4).Last Then
                                                                @Html.Raw("-")
                                                            End If
                                                        Next


                                                    </td>
                                                </text>
                                            End If
                                            @If Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 3).Count) > 0 Then
                                                @<text>
                                                    <td class="hidden-xs">

                                                        @for Each unit In item.Units.Where(Function(m) m.Type.GroupId = 3)
                                                            @<text>
                                                                @unit.Type.Name @unit.Name
                                                            </text>
                                                            If Not unit Is item.Units.Where(Function(m) m.Type.GroupId = 3).Last Then
                                                                @Html.Raw("-")
                                                            End If
                                                        Next


                                                    </td>
                                                </text>
                                            End If       
                                            @If Model.ClientAccounts.Sum(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 2).Count) > 0 Then
                                                @<text>
                                                    <td class="hidden-xs">

                                                        @for Each unit In item.Units.Where(Function(m) m.Type.GroupId = 2)
                                                            @<text>
                                                                @unit.Type.Name @unit.Name
                                                            </text>
                                                            If Not unit Is item.Units.Where(Function(m) m.Type.GroupId = 2).Last Then
                                                                @Html.Raw("-")
                                                            End If
                                                        Next


                                                    </td>
                                                </text>
                                            End If
                                            <td class="hidden-xs">@Html.DisplayFor(Function(m) item.Client.DateSalesAgreement)</td>
                                            <td class="hidden-xs">@if Not item.Client.DateDeedOfSale Is Nothing Then @Html.DisplayFor(Function(m) item.Client.DateDeedOfSale) end If</td>
                                            <td Class="hidden-xs">@If Not item.Client.DeliveryDate Is Nothing Then @Html.DisplayFor(Function(m) item.Client.DeliveryDate) End If</td>
                                            <td class="hidden-xs">
                                                <a  data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken"  id="editAccount" onclick="location.href='@Url.Action("Edit", "Klanten", New With {.projectid = Model.ProjectId, .clientid = item.Client.Id, .activetab = 0})'"><i class="fa fa-edit"></i></a>
                                                @If User.IsInRole("Admin") Then
                                                    @<text>
                                                <a href="#modaldeleteclient" data-toggle="tooltip" id="deleteClient" data-placement="top" title="" data-original-title="Verwijderen" class="deleteClient" data-id="@item.Client.Id"><i class="fa fa-remove "></i></a>

                                                    </text>
                                                End If

                                            </td>
                                        </tr>

                                    </text>


               Next





            </tbody>
            </table>

    </div>
</section>
    
        
@section scripts


End Section