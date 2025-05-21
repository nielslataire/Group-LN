@Modeltype BO.DepartmentBO
<tr>
    @Using (Html.BeginCollectionItem("departments"))
        @<text>
    <td data-title='Naam'>
        @Html.DisplayFor(Function(m) m.Name)
    @Html.HiddenFor(Function(m) m.Name)
</td>
    <td data-title="Adres">
        @Html.DisplayFor(Function(m) m.Street)
    @Html.HiddenFor(Function(m) m.Street)
    @Html.DisplayFor(Function(m) m.Housenumber)
    @Html.HiddenFor(Function(m) m.Housenumber)
    @Html.DisplayFor(Function(m) m.Busnumber)
    @Html.HiddenFor(Function(m) m.Busnumber)
</td>
    <td data-title="Gemeente">
        @Html.DisplayFor(Function(m) m.Postalcode.Postcode)
        @Html.HiddenFor(Function(m) m.Postalcode.Postcode)
        @Html.HiddenFor(Function(m) m.Postalcode.PostcodeId)
        @Html.DisplayFor(Function(m) m.Postalcode.Gemeente)
        @Html.HiddenFor(Function(m) m.Postalcode.Gemeente)

    </td>
    <td data-title="Land">
        @Html.DisplayFor(Function(m) m.Postalcode.Country.Name)
        @Html.HiddenFor(Function(m) m.Postalcode.Country.Name)
    </td>
    <td data-title ="Telefoon">
        @If Not Model.Phone Is Nothing Then
            @Html.DisplayFor(Function(m) m.FormattedTelefoon)
        Else
            @:-
        End If
        @Html.HiddenFor(Function(m) m.Phone)
    </td>
    <td data-title="GSM">
        @If Not Model.CellPhone Is Nothing Then
            @Html.DisplayFor(Function(m) m.FormattedGSM)
        Else
            @:-
        End If
        @Html.HiddenFor(Function(m) m.CellPhone)
    </td>
    <td data-title="Email">
        @If Not Model.Email Is Nothing Then
            @If Not ViewData("mode") = "print" Then
                @<text>
                    <div class="hidden-xs"><a href='mailto:@Html.DisplayFor(Function(m) m.Email)'>@Html.DisplayFor(Function(m) m.Email)</a></div>
                    <div class="visible-xs actions ">
                        <a href='mailto:@Html.DisplayFor(Function(m) m.Email)' data-toggle="tooltip" data-placement="top" title="" data-original-title="Mail"><i class="fa fa-envelope"></i></a>
                    </div>
                </text>
            Else
                @Html.ValueFor(Function(m) m.Email)
            End If
        Else
            @:-
        End If
        @Html.HiddenFor(Function(m) m.Email)
    </td>
    @If ViewData("mode") = "edit" Then
        @<text>
    <td class="actions" data-title="Acties">
        <a href="#modaladddepartment" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editDepartment" id="editDepartment" data-id="@Model.ID"><i class="fa fa-edit"></i></a>
        <a href="#modaldeletedepartment" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteDepartment" data-id="@Model.ID"><i class="fa fa-remove "></i></a>
    </td>
        </text>
        ElseIf ViewData("mode") = "add" Then
        @<text>
    <td class="actions" data-title="Acties">
        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteDepartmentRow"><i class="fa fa-remove "></i></a>
    </td>
        </text>
        End If
   </text>
    End Using

</tr>
        

