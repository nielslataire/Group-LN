@modeltype List(Of BO.ActivityBO)
@For Each item In Model
    Html.RenderPartial("ActivityRow", item, New ViewDataDictionary() From {{"mode", "edit"}})
Next
