@Modeltype BO.UnitWithReductionBO 
@Using (Html.BeginCollectionItem("Units"))
@<text>
<tr Class="active text-weight-semibold">
    <td colspan="2" width="60%">
        @Html.HiddenFor(Function(m) Model.Base.Id)
        @Html.HiddenFor(Function(m) Model.Base)
        @Model.Base.Type.Name @Model.Base.Name
    </td>
    <td Class="text-right" width="25%"></td>
    <td Class="text-right">
        @String.Format("{0:C}", Model.Base.TotalValue)

    </td>
</tr>

<tr>
    <td width="5%"></td>
    <td>Registratie</td>
    <td Class="text-right" width="20%">@Html.EditorFor(Function(m) Model.ReductionLandValue, New With {.class = "form-control input-sm text-right", .id = "reductionlandvalue"})</td>
    <td Class="text-right">@String.Format("{0:C}", Model.Base.LandValue)<input type="hidden"/></td>
</tr>
@For Each item In Model.Base.ConstructionValues
    Html.RenderPartial("_CalculateUnitConstructionValue", item)
Next
</text>
End Using


