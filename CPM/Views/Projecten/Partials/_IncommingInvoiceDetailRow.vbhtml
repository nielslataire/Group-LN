@Modeltype BO.IncommingInvoiceDetailBO

<tr>
    @Using (Html.BeginCollectionItem("details"))
        @<text>
            @*<td>@Html.DisplayFor(Function(m) m.Group.Name )
                @Html.HiddenFor(Function(m) m.Group.Name)</td>*@
    <td data-title="Activiteit" width="200px">
        <label class="control-label">@Model.ContractActivityText</label>
        
    </td>
    <td data-title="Omschrijving">
        @Html.HiddenFor(Function(m) m.Id)
        @Html.HiddenFor(Function(m) m.ContractActivityID)
        @Html.HiddenFor(Function(m) m.ContractActivityText)
        @Html.HiddenFor(Function(m) m.ActivityID)
        @Html.HiddenFor(Function(m) m.IncommingInvoiceID)
        @Html.EditorFor(Function(m) m.Description)
    </td>
    <td data-title="Prijs" width="150px">

            @Html.EditorFor(Function(m) m.Price)

    </td>

            <td data-title="Type" width="150px">
                @Html.EnumDropDownListFor(Function(m) m.IncommingInvoiceType, New With {.class = "form-control type", .onchange = "change(this)"})
            </td>
            <td data-title="WO" width="200px">
                @Html.DropDownListFor(Function(m) m.ChangeOrderId, New SelectList(Model.ChangeOrders, "ID", "Display", Model.ChangeOrderId), New With {.class = "form-control populate wo", .data_plugin_selecttwo = "", .id = "lstchangeorders", .disabled = "disabled"})

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

