@Modeltype BO.ActivityBO
<tr>
    @Using (Html.BeginCollectionItem("activities"))
        @<text>
    <td data-title="Nummer">
        @Html.DisplayFor(Function(m) m.ID)
    @Html.HiddenFor(Function(m) m.ID)
</td>
    @*<td>@Html.DisplayFor(Function(m) m.Group.Name )
    @Html.HiddenFor(Function(m) m.Group.Name)</td>*@
    <td data-title="Naam">
        @Html.DisplayFor(Function(m) m.Name)
    @Html.HiddenFor(Function(m) m.Name)
</td>
    @If ViewData("mode") = "edit" Then
        @<text>
    <td class="actions" data-title="Verwijderen">
        <a href="#modaldeleteactivity" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteActivity" data-id="@Model.ID"><i class="fa fa-remove "></i></a>
    </td>
        </text>
        ElseIf ViewData("mode") = "add" Then
        @<text>
    <td class="actions" data-title="Verwijderen">
        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow"><i class="fa fa-remove"></i></a>
    </td>
        </text>
        End If
</text>
    End Using
</tr>

