@Modeltype BO.ContractActivityBO

<tr>
    @Using (Html.BeginCollectionItem("activities"))
        If Model.Activity.ID = 142 Then
            @<text>
                <td colspan="4" class="m-none" style="border-spacing:0px;padding:0px;">
                    <table class="table table-bordered m-none">
                        <tr>
                            <td data-title="Nummer"  width="10%">
                            @Html.HiddenFor(Function(m) m.ContractActivityId)
                            @Model.Activity.ID
                            @Html.HiddenFor(Function(m) m.Activity.ID)
                                @Html.HiddenFor(Function(m) m.ContractId)
                            </td>
                            <td data-title="Naam" width="30%">
                            @Model.Activity.Name
                            @Html.HiddenFor(Function(m) m.Activity.Name)
                            </td>
                            <td data-title="Prijs">
                            @Html.EditorFor(Function(m) m.Price)
                            </td>
                            @If ViewData("mode") = "edit" Then
                                @<text>
                                    <td class="actions" data-title="Verwijderen" width="20%">
                                        <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit ms-2" data-id="@Model.ContractActivityId"><i class="fa fa-remove "></i></a>
                                    </td>
                                </text>
                            ElseIf ViewData("mode") = "add" Then
                                @<text>
                                    <td class="actions" data-title="Verwijderen" width="20%">
                                        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow ms-2"><i class="fa fa-remove"></i></a>
                                    </td>
                                </text>
                                End If
                                
                        </tr>
                    </table>
                </td>
            </text>
    Else
            @<text>
                <td data-title="Nummer" width="10%">
                    @Model.Activity.ID
                    @Html.HiddenFor(Function(m) m.Activity.ID)
                    @Html.HiddenFor(Function(m) m.ContractActivityId)
                    @Html.HiddenFor(Function(m) m.ContractId)
                </td>
                <td data-title="Naam" width="30%">
                    @Model.Activity.Name
                    @Html.HiddenFor(Function(m) m.Activity.Name)
                </td>
                <td data-title="Prijs">
                    @Html.EditorFor(Function(m) m.Price)
                </td>
                @If ViewData("mode") = "edit" Then
                    @<text>
                        <td class="actions" data-title="Verwijderen" width="20%">
                            <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@Model.ContractActivityId"><i class="fa fa-remove "></i></a>
                        </td>
                    </text>
                ElseIf ViewData("mode") = "add" Then
                    @<text>
                        <td class="actions" data-title="Verwijderen" width="20%">
                            <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow"><i class="fa fa-remove"></i></a>
                        </td>
                    </text>
                End If
            </text>
    End If

End Using
</tr>

