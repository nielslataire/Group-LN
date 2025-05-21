@modeltype IEnumerable(of applicationuser)

    @Code
        ViewBag.Title = "Gebruikers"
    End Code


    <p>
        @Html.ActionLink("Gebruiker aanmaken", "Create")
    </p>
    <table class="table">
        <tr>
            <th>
                @Html.DisplayNameFor(Function(m) m.UserName)
            </th>
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
                @item.UserName 
            </td>
            <td>
                @item.Name @item.Forename 
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

