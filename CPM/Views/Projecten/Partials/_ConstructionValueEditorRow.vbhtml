@imports BO
@modeltype UnitConstructionValueBO 

<tr>
@Using (Html.BeginCollectionItem("ConstructionValues"))
    @<text>
    @Html.HiddenFor(Function(m) m.Id)
    @Html.HiddenFor(Function(m) m.UnitId)
    <td data-title='ConstructionValue' width="20%">
        @Html.EditorFor(Function(m) m.Value, New With {.class = "form-control", .id = "txtValue"})
    </td>
    <td data-title='ConstructionValueSold' width="20%">
        @Html.EditorFor(Function(m) m.ValueSold, New With {.class = "form-control", .id = "txtValueSold"})
    </td>
    <td data-title='Description' width="30%">
        @Html.TextBoxFor(Function(m) m.Description, New With {.class = "form-control", .id = "txtDescription"})
    </td>
    <td data-title='Type' width="20%">
        @Html.DropDownListFor(Function(m) m.PaymentGroupId, New SelectList(ViewBag.Paymentgroups, "ID", "Display", "Group", Model.PaymentGroupId), "Selecteer ...", New With {.class = "form-control populate", .id = "lstpaymentgroups"})

    </td>
    <td Class="actions" data-title="Verwijderen">
        <a href = "#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" Class="deleteConstructionRow"><i Class="fa fa-remove"></i></a>
    </td>
    </text> End Using
</tr>
