@Modeltype BO.UnitBO

<tr>
    @Using (Html.BeginCollectionItem("units"))
        @<text>
    <td data-title="Nummer">
        @Html.DisplayFor(Function(m) m.Id)
    @Html.HiddenFor(Function(m) m.Id)
</td>
    @*<td>@Html.DisplayFor(Function(m) m.Group.Name )
    @Html.HiddenFor(Function(m) m.Group.Name)</td>*@
    <td data-title="Naam">
        @Html.DisplayFor(Function(m) m.Name)
    @Html.HiddenFor(Function(m) m.Name)
</td>
    <td data-title="Grondwaarde">
        @Html.EditorFor(Function(m) m.LandValueSold)
    </td>
    <td data-title="Bouwwaarde">

        @*@If Model.ConstructionValues.Count = 1 Then
            @For Each item In Model.ConstructionValues
                @Html.EditorFor(Function(m) m.ConstructionValues(item.Id).ValueSold)
            Next
        End If*@
        @For j As Integer = 0 To Model.ConstructionValues.Count - 1
            @<text>
                <div class="col-md-4">@Html.HiddenFor(Function(m) m.ConstructionValues(j).Id)@Html.DisplayFor(Function(m) m.ConstructionValues(j).Description)</div>
            </text>
            @<text>
                <div class="col-md-8">@Html.EditorFor(Function(m) m.ConstructionValues(j).ValueSold)</div>
            </text>
        Next
       
       @*@Html.EditorFor(Function(m) m.ConstructionValueSold)*@
    </td>
    
    @If ViewData("mode") = "edit" Then
        @<text>
    <td class="actions col-md-2" data-title="Verwijderen">
        <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@Model.ID"><i class="fa fa-remove "></i></a>
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

