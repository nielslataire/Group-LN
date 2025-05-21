@Modeltype RegisterViewModel
@Code
    ViewBag.Title = "Gebruiker"
End Code

@*<h2>@ViewBag.Title.</h2>*@

@Using (Html.BeginForm("Create", "UserAdmin", FormMethod.Post, New With {.class = "form-horizontal", .role = "form"}))
@<text>
    @Html.AntiForgeryToken()
    <h4>Maak een nieuwe gebruiker aan</h4>
    <hr />
    @Html.ValidationSummary("", New With {.class = "text-error"})
<div class="form-group">
    @Html.LabelFor(Function(m) m.Username, New With {.class = "col-md-2 control-label"})
    <div class="col-md-10">
        @Html.TextBoxFor(Function(m) m.Username, New With {.class = "form-control"})
    </div>
</div>
    <div class="form-group">
    @Html.LabelFor(Function(m) m.Email, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
    @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control"})
        </div>
    </div>
<div class="form-group">
    @Html.LabelFor(Function(m) m.Name, New With {.class = "col-md-2 control-label"})
    <div class="col-md-10">
        @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control"})
    </div>
</div>
<div class="form-group">
    @Html.LabelFor(Function(m) m.Forename, New With {.class = "col-md-2 control-label"})
    <div class="col-md-10">
        @Html.TextBoxFor(Function(m) m.Forename, New With {.class = "form-control"})
    </div>
</div>
<div class="form-group">
    @Html.LabelFor(Function(m) m.JobFunction, New With {.class = "col-md-2 control-label"})
    <div class="col-md-10">
        @Html.TextBoxFor(Function(m) m.JobFunction, New With {.class = "form-control"})
    </div>
</div>
<div class="form-group">
    @Html.LabelFor(Function(m) m.Cellphone, New With {.class = "col-md-2 control-label"})
    <div class="col-md-10">
        @Html.TextBoxFor(Function(m) m.Cellphone, New With {.class = "form-control"})
    </div>
</div>
    @*<div class="form-group">
    @Html.LabelFor(Function(m) m.Password, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
    @Html.PasswordFor(Function(m) m.Password, New With {.class = "form-control"})
        </div>
    </div>
    <div class="form-group">
    @Html.LabelFor(Function(m) m.ConfirmPassword, New With {.class = "col-md-2 control-label"})
        <div class="col-md-10">
    @Html.PasswordFor(Function(m) m.ConfirmPassword, New With {.class = "form-control"})
        </div>
    </div>*@
    <div class="form-group">
        <label class="col-md-2 control-label">
            Select User Role
        </label>
        <div class="col-md-10">
    @For Each item In DirectCast(ViewBag.RoleId, SelectList)
            @<text>
                <input type="checkbox" name="SelectedRoles" value="@item.Value" class="checkbox-inline" />
    @Html.Label(item.Value, New With {.class = "control-label"})
            </text>
    Next
        </div>
    </div>
    <div class="form-group">
        <div class="col-md-offset-2 col-md-10">
            <input type="submit" class="btn btn-default" value="Aanmaken" />
        </div>
    </div>
</text>
End Using
@section Scripts
    @Scripts.Render("~/bundles/jqueryval")
End section

