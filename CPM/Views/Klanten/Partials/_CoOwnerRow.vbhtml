@Modeltype BO.ClientContactBO 
<tr>
    @Using (Html.BeginCollectionItem("coowners"))
        @<text>
            <td data-title='Naam'>
                @Html.DisplayFor(Function(m) m.Name) @Html.DisplayFor(Function(m) m.Firstname)               
                @Html.HiddenFor(Function(m) m.Name)
                @Html.HiddenFor(Function(m) m.Firstname)

                @Html.HiddenFor(Function(m) m.Street)
                @Html.HiddenFor(Function(m) m.Housenumber)
                @Html.HiddenFor(Function(m) m.Busnumber)
                @Html.HiddenFor(Function(m) m.Postalcode.Postcode)
                @Html.HiddenFor(Function(m) m.Postalcode.PostcodeId)
                @Html.HiddenFor(Function(m) m.Postalcode.Gemeente)
                @Html.HiddenFor(Function(m) m.Postalcode.Country.Name)
                @Html.HiddenFor(Function(m) m.Phone)
                @Html.HiddenFor(Function(m) m.Cellphone)
                @Html.HiddenFor(Function(m) m.Email)
                @Html.HiddenFor(Function(m) m.CompanyName)
                @Html.HiddenFor(Function(m) m.VATnumber)
            </td>
    <td data-title="Type">
        @Html.DisplayFor(Function(m) m.CoOwnerType.Name)
        @Html.HiddenFor(Function(m) m.CoOwnerType.Id)
    </td>
    <td data-title="%" class="percentage">
        @Html.DisplayFor(Function(m) m.CoOwnerPercentage)
        @Html.HiddenFor(Function(m) m.CoOwnerPercentage)
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
                        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteCoOwnerRow"><i class="fa fa-remove "></i></a>
                    </td>
                </text>
            End If
        </text>
    End Using

</tr>

