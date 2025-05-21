@Modeltype ApplicationUser

@Code
    ViewBag.Title = "Gebruiker verwijderen"
End Code


<h3>Bent u zeker dat u deze gebruiker wilt verwijderen ?</h3>
<div>
    <h4>Gebruiker.</h4>
    <hr />
    <dl class="dl-horizontal">
        <dt>
            @Html.DisplayNameFor(Function(m) m.UserName)
        </dt>

        <dd>
            @Html.DisplayFor(Function(m) m.UserName)
        </dd>
        <dd>
            @Html.DisplayFor(Function(m) m.Name)
        </dd>
    </dl>

    @Using (Html.BeginForm())
        @<text>
    
        @Html.AntiForgeryToken()

        <div class="form-actions no-color">
            <input type="submit" value="Delete" class="btn btn-default" /> |
        @Html.ActionLink("Terug naar de lijst", "Index")
        </div>
    </text>
    End Using
</div>

