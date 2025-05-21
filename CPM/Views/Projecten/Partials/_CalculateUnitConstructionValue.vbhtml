@Modeltype BO.UnitConstructionValueBO  


@Using (Html.BeginCollectionItem("ConstructionValues"))
@<text>
<tr>
    <td></td>
    <td> @Model.Description</td>
        <td Class="text-right">@Html.EditorFor(Function(m) Model.Reduction, New With {.class = "form-control input-sm text-right"})@Html.HiddenFor(Function(m) Model.Id)</td>
    <td Class="text-right">@String.Format("{0:C}", Model.Value)</td>
</tr>
</text>
End Using

