@Modeltype BO.ChangeOrderDetailBO

<tr>
    @Using (Html.BeginCollectionItem("details"))
        @<text>
    @*<td>@Html.DisplayFor(Function(m) m.Group.Name )
    @Html.HiddenFor(Function(m) m.Group.Name)</td>*@
    <td data-title="Omschrijving" >
        @Html.HiddenFor(Function(m) m.Id)
        @Html.HiddenFor(Function(m) m.ChangeOrderID)
        @Html.EditorFor(Function(m) m.Description)
</td>
    <td data-title="Meetmethode" width="90px">
        @Html.EnumDropDownListFor(Function(m) m.MeasurementType, New With {.class = "form-control"})
    </td>

    <td data-title="Meeteenheid" width="90px">
            @Html.EnumDropDownListFor(Function(m) m.MeasurementUnit, New With {.class = "form-control"})
    </td>
    <td data-title="Aantal" width="90px">
            @Html.TextBoxFor(Function(m) m.Number, New With {.class = "form-control"})
    </td>
    <td data-title="Prijs" width="150px">
            @Html.EditorFor(Function(m) m.Price)
    </td>
    <td data-title="Commissie" width="120px">
            @Html.EditorFor(Function(m) m.Commision)
    </td>
    @If ViewData("mode") = "edit" Then
        @<text>
    <td class="actions" data-title="Verwijderen" width="120px">
        <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@Model.ID"><i class="fa fa-remove "></i></a>
    </td>
        </text>
    ElseIf ViewData("mode") = "add" Then
        @<text>
    <td class="actions" data-title="Verwijderen" width="120px">
        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleterow"><i class="fa fa-remove"></i></a>
    </td>
        </text>
        End If
</text>
    End Using
</tr>

