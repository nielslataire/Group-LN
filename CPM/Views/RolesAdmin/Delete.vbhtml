@Modeltype Microsoft.AspNet.Identity.EntityFramework.IdentityRole

@Code
    ViewBag.Title = "Groep - Verwijderen"
End Code


<h5>Bent u zeker dat u deze groep wilt verwijderen? </h5>
<p>Als u deze groep verwijderd worden alle gebruikers uit deze groep verwijderd. De gebruikers zelf worden niet verwijderd.</p>
<div>
    <h4>Groep</h4>
    <hr />
    <dl class="dl-horizontal">
        <dt>
            @Html.DisplayNameFor(Function(m) m.Name)
        </dt>

        <dd>
            @Html.DisplayFor(Function(m) m.Name)
        </dd>
    </dl>
    @Using (Html.BeginForm())
    @<text>
        @Html.AntiForgeryToken()

        <div class="form-actions no-color">
            <input type="submit" value="Verwijderen" class="btn btn-default" /> |
        @Html.ActionLink("Terug naar de lijst", "Index")
        </div>
    </text>
    End Using
</div>
