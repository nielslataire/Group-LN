@Modeltype RoleViewModel

@Code
    ViewBag.Title = "Groep aanmaken"
End Code


@Using (Html.BeginForm())
@<text>
    @Html.AntiForgeryToken()

    <div class="form-horizontal">
        <h4>Nieuwe groep aanmaken</h4>
        <hr />
    @Html.ValidationSummary(True)

        <div class="form-group">
    @Html.LabelFor(Function(m) m.Name, New With {.class = "control-label col-md-2"})
            <div class="col-md-10">
    @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control"})
    @Html.ValidationMessageFor(Function(m) m.Name)
            </div>
        </div>

        <div class="form-group">
            <div class="col-md-offset-2 col-md-10">
                <input type="submit" value="Aanmaken" class="btn btn-default" />
            </div>
        </div>
    </div>
</text>
End using

<div>
    @Html.ActionLink("Terug naar de lijst", "Index")
</div>

@section Scripts
    @Scripts.Render("~/bundles/jqueryval")
End Section

