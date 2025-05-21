@Modeltype ApplicationUser

@Code
    ViewBag.Title = "Gebruiker - Details"
End Code

<div>
    <h4>Gebruiker</h4>
    <hr />
    <dl class="dl-horizontal">
        <dt>
            @Html.DisplayNameFor(Function(m) m.UserName)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.UserName)
        </dd>
        <dt>
            @Html.DisplayNameFor(Function(m) m.Name)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.Name)
        </dd>
        <dt>
            @Html.DisplayNameFor(Function(m) m.Forename)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.Forename)
        </dd>
        <dt>
            @Html.DisplayNameFor(Function(m) m.Email)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.Email)
        </dd>
        <dt>
            @Html.DisplayNameFor(Function(m) m.Cellphone)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.Cellphone)
        </dd>
        <dt>
            @Html.DisplayNameFor(Function(m) m.JobFunction)
        </dt>
        <dd>
            @Html.DisplayFor(Function(m) m.JobFunction)
        </dd>
    </dl>
</div>
<h4>List of roles for this user</h4>
@If (ViewBag.RoleNames.Count = 0) Then
@<text>
    <hr />
    <p>No roles found for this user.</p>
</text>
End if

<table class="table">

    @For Each item In ViewBag.RoleNames
    @<text>
        <tr>
            <td>
    @item
            </td>
        </tr>
    </text>
    Next
</table>
<p>
    @Html.ActionLink("Bewerken", "Edit", New With {.id = Model.Id}) |
    @Html.ActionLink("Terug naar de lijst", "Index")
</p>




