@modeltype ActivitiesModel 

        <div class="form-group">
            <label class="col-md-3 control-label" for="ActivityName">Naam </label>
            @Html.TextBoxFor(Function(m) m.SelectedActivity.ID, New With {.style = "display:none;", .class = "form-control", .disabled = "", .id = "txtActId"})            
            <div class="col-md-9">
                @Html.TextBoxFor(Function(m) m.SelectedActivity.Name, New With {.placeholder = "Naam activiteit", .class = "form-control", .disabled = "", .id = "txtActName"})      
            </div>
        </div>
        <div class="form-group">
            <label class="col-md-3 control-label" for="Category">Categorie</label>
            <div class="col-md-9">
                @Html.TextBoxFor(Function(m) m.SelectedActivity.Group.ID, New With {.style = "display:none;", .class = "form-control", .disabled = "", .id = "txtGroupID"})
                @Html.DropDownListFor(Function(m) m.SelectedGroup, New SelectList(Model.SelectListGroupsForActivity, "ID", "Display", Model.SelectedGroup), New With {.class = "form-control populate", .disabled = "", .data_plugin_selecttwo = "", .id = "lstActivityGroups"})
            </div>
        </div>
