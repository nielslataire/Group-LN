@Modeltype BO.BudgetActivityBO

<tr>
    @Using (Html.BeginCollectionItem("budgetactivities"))
        @<text>
    <td data-title="Nummer" style="color:white;">
        @Html.DisplayFor(Function(m) m.Id)
    @Html.HiddenFor(Function(m) m.Id)
    @Html.HiddenFor(Function(m) m.Activity.ID)
</td>

    <td data-title="Naam">
        @Html.DisplayFor(Function(m) m.Activity.Name)
    @Html.HiddenFor(Function(m) m.Activity.Name)
</td>
    <td data-title="Prijs">
        @Html.EditorFor(Function(m) m.Price)
    </td>
    @If ViewData("mode") = "edit" Then
        @<text>
    <td class="actions text-right" data-title="Verwijderen">
        <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@Model.ID"><i class="fa fa-remove "></i></a>
    </td>
        </text>
    ElseIf ViewData("mode") = "add" Then
        @<text>
    <td class="actions text-right" data-title="Verwijderen">
        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow"><i class="fa fa-remove"></i></a>
    </td>
        </text>
        End If
</text>
    End Using
</tr>

