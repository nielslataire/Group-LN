@Modeltype EditUserViewModel

@Code
    ViewBag.Title = "Gebruiker bewerken"
End Code



@Using (Html.BeginForm())
    @<text>

    @Html.AntiForgeryToken()

<div class="form-horizontal">
    <h4>Edit User Form.</h4>
    <hr />
    @Html.ValidationSummary(True)
    @Html.HiddenFor(Function(m) m.Id)
    <div class="form-group">
        @Html.LabelFor(Function(m) m.Username, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.Username, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.Username)
        </div>
    </div>
    <div class="form-group">
        @Html.LabelFor(Function(m) m.Email, New With {.class = "control-label col-md-2"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.Email)
        </div>
    </div>
    <div class="form-group">
        @Html.LabelFor(Function(m) m.Name, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.Name)
        </div>
    </div>
    <div class="form-group">
        @Html.LabelFor(Function(m) m.Forename, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.Forename, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.Forename)
        </div>
    </div>
    <div class="form-group">
        @Html.LabelFor(Function(m) m.JobFunction, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.JobFunction, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.JobFunction)
        </div>
    </div>
    <div class="form-group">
        @Html.LabelFor(Function(m) m.Cellphone, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
            @Html.TextBoxFor(Function(m) m.Cellphone, New With {.class = "form-control"})
            @Html.ValidationMessageFor(Function(m) m.Cellphone)
        </div>
    </div>

    <div class="form-group">
        @Html.Label("Roles", New With {.class = "control-label col-md-2"})
        <span class=" col-md-10">
            @For Each item In Model.RolesList
                @<text>
                    <input type="checkbox" name="SelectedRole" value="@item.Value" checked="@item.Selected" class="checkbox-inline" />
                    @Html.Label(item.Value, New With {.class = "control-label"})
                </text>
            Next
        </span>
    </div>

    <div class="form-group">
        <div class="col-md-offset-2 col-md-10">
            <input type="submit" value="Save" class="btn btn-default" />
        </div>
    </div>
</div>
</text>
End Using

<div>
    @Html.ActionLink("Back to List", "Index")
</div>

    @section Scripts 
        @Scripts.Render("~/bundles/jqueryval")
        End section

