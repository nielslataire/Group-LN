@imports BO
@modeltype RoomBO

<tr>
@Using (Html.BeginCollectionItem("rooms"))
    @<text>
    @Html.HiddenFor(Function(m) m.Id)
    @Html.HiddenFor(Function(m) m.UnitId)
    <td data-title='Type' width="30%">
        @Html.EnumDropDownListFor(Function(m) m.Type, New With {.class = "form-control", .id = "lstType"})
    </td>
    <td data-title='Number' width="10%">
        @Html.TextBoxFor(Function(m) m.Number, New With {.type = "number", .class = "form-control", .id = "txtNumber"})
    </td>
    <td data-title='Surface' width="10%">
        @Html.EditorFor(Function(m) m.Surface, New With {.class = "form-control", .id = "txtSurface"})
    </td>
    <td data-title='Remark'>
        @Html.TextBoxFor(Function(m) m.Remark, New With {.class = "form-control", .id = "txtRemark"})
    </td>
    <td Class="actions" data-title="Verwijderen">
        <a href = "#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" Class="deleteRow"><i Class="fa fa-remove"></i></a>
    </td>
    </text>
End Using
</tr>
