@Modeltype BO.ContractAdditionalOrderBO

<tr>
    @Using (Html.BeginCollectionItem("additionalorders"))

        @<text>
            <td data-title="Nummer" width="10%">

                @Html.HiddenFor(Function(m) m.ContractActivityId)

            </td>
            <td data-title="Omschrijving" width="20%">
            </td>
            <td data-title="Omschrijving" width="30%">
                @Html.HiddenFor(Function(m) m.Description)
            </td>
            <td data-title="Prijs">
                @Html.EditorFor(Function(m) m.Price)
            </td>
            @If ViewData("mode") = "edit" Then
                @<text>
                    <td class="actions" data-title="Verwijderen" width="20%">
                        @*<a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@Model.ContractActivityId"><i class="fa fa-remove "></i></a>*@
                    </td>
                </text>
            ElseIf ViewData("mode") = "add" Then
                @<text>
                    <td class="actions" data-title="Verwijderen" width="20%">
                        @*<a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow"><i class="fa fa-remove"></i></a>*@
                    </td>
                </text>
            End If
        </text>


    End Using
</tr>

