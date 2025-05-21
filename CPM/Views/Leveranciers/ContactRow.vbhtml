@Modeltype BO.ContactBO
<tr>

    @Using (Html.BeginCollectionItem("contacts"))
        @<text>
            <td data-title="Naam">
                @Html.HiddenFor(Function(m) m.Id)
                @Html.DisplayFor(Function(m) m.Salutation)
                @Html.HiddenFor(Function(m) m.Salutation)

                @Html.DisplayFor(Function(m) m.Name)
                @Html.HiddenFor(Function(m) m.Name)

                @Html.DisplayFor(Function(m) m.Firstname)
                @Html.HiddenFor(Function(m) m.Firstname)
            </td>
            <td data-title="Functie">
                @If Not Model.JobFunction Is Nothing Then
                    @Html.DisplayFor(Function(m) m.JobFunction)
                Else
                    @:-
        End If
                @Html.HiddenFor(Function(m) m.JobFunction)
            </td>
            <td data-title="Afdeling">
                @If Not Model.Department.Display Is Nothing Then
                    @Html.DisplayFor(Function(m) m.Department.Display )
                Else
                    @:-
                End If
                @Html.HiddenFor(Function(m) m.Department.ID)
            </td>

            <td data-title="Telefoon">
                @If Not Model.Phone Is Nothing Then
                    @Html.DisplayFor(Function(m) m.FormattedTelefoon)
                Else
                    @:-
        End If
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
                        <a href="#modaladdcontact" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editContact" id="editContact" data-id="@Model.Id"><i class="fa fa-edit"></i></a>
                        <a href="#modaldeletecontact" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteContact" data-id="@Model.Id"><i class="fa fa-remove "></i></a>
                    </td>
                </text>
            ElseIf ViewData("mode") = "add" Then
                @<text>
                    <td class="actions" data-title="Acties">
                        <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteContactRow"><i class="fa fa-remove "></i></a>
                    </td>
                </text>
            End If


        </text>
    End Using
</tr>
