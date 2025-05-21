@Modeltype Microsoft.AspNet.Identity.EntityFramework.IdentityRole

@Code
    ViewBag.Title = "Groep - Details"
End Code


<div>
    <h4>@Html.DisplayFor(Function(m) m.Name)</h4>
    <hr />
    @*<dl class="dl-horizontal">
        <dt>
            @Html.DisplayNameFor(Function(m) m.Name)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.Name)
        </dd>
    </dl>*@
</div>
<h5>Lijst van gebruikers in deze groep</h5>
@If (ViewBag.UserCount = 0) Then
@<text>
    <hr />
    <p>Geen gebruikers gevonden in deze groep.</p>
</text>
End if

<table class="table">

    @For Each item In ViewBag.Users
    @<text>
        <tr>
            <td>
    @item.UserName
            </td>
            <td>
                @item.Name @item.forename
            </td>
            <td>
                @item.email
            </td>
        </tr>
    </text>
    Next
</table>
<p>
    @Html.ActionLink("Bewerken", "Edit", New With {.id = Model.Id}) |
    @Html.ActionLink("Terug naar lijst", "Index")
</p>



