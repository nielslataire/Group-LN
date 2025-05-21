@Modeltype IEnumerable(Of Microsoft.aspnet.identity.entityframework.IdentityRole)

    @Code
        ViewBag.Title = "Index"
    End Code

    <h2>Groepen</h2>

    <p>
        @Html.ActionLink("Nieuwe groep aanmaken", "Create")
    </p>
    <table class="table">
        <tr>
            <th>
                
                @Html.DisplayNameFor(Function(m) m.Name)
            </th>
            <th>

            </th>
        </tr>
        @For Each item In Model
        @<text>
        <tr>
            <td>
                @item.Name
            </td>
            <td>
                @Html.ActionLink("Bewerken", "Edit", New With {.id = item.Id}) |
                @Html.ActionLink("Details", "Details", New With {.id = item.Id}) |
                @Html.ActionLink("Verwijderen", "Delete", New With {.id = item.Id})
            </td>
        </tr>
        </text>
        Next

    </table>

