@modeltype ActivitiesModel 

        <div class="form-group">
            <label class="col-md-3 control-label" for="ActivityName">Lot </label>
            @Html.TextBoxFor(Function(m) m.SelectedActivityGroup.ID, New With {.style = "display:none;", .class = "form-control", .disabled = "", .id = "txtGroupId"})            
            <div class="col-md-9">
                @Html.TextBoxFor(Function(m) m.SelectedActivityGroup.Lot, New With {.placeholder = "Lot", .class = "form-control", .disabled = "", .id = "txtGroupLot"})      
            </div>
        </div>
        <div class="form-group">
            <label class="col-md-3 control-label" for="Category">Naam</label>
            <div class="col-md-9">
                @Html.TextBoxFor(Function(m) m.SelectedActivityGroup.Name, New With {.placeholder = "Groep naam", .class = "form-control", .disabled = "", .id = "txtGroupName"})
            </div>
        </div>
