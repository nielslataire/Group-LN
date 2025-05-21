@modeltype DetailUnitsModel

@*<div class="inner-toolbar clearfix">
        <ul>
            <li>
                <a href="#modaladdphoto" class="btn modal-with-form " type="button" id="btnAddPhoto"><i class="fa fa-plus"></i> Toevoegen</a>
            </li>
            <li>
                    @Html.HtmlActionLink("<i class='fa fa-remove'></i> Verwijderen</a>", "DeleteSelectedPhotos", "Projecten", New With {.model = Model}, New With {.id = "GeneralDataSave", .class = "btn"})
                </li>
        </ul>
    </div>*@
@Using Html.BeginForm("Detail", "Projecten", FormMethod.Post, New With {.id = "FormGeneralData", .class = "form-horizontal"})
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})

        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Overzicht eenheden</h2>
            </header>
            <div class="panel-body">
                <div class="table-responsive">
                    <table class="table mb-none">
                        <thead>
                            <tr>
                                <th width="20px"></th>
                                <th>Naam</th>
                                <th>Verdieping</th>
                                <th>Verdeling Basisakte</th>
                                <th>Acties</th>
                            </tr>
                        </thead>
                        <tbody>
                            @For Each Group In Model.UnitsGrouped

                            @<text>
                                <tr class="active">
                                    <td></td>
                                    <td colspan="4" class="text-weight-bold">@Group.Units(0).Type.Name </td>
                                </tr>
                            </text>

                            @code Dim Levels = Group.Units.GroupBy(Function(m) m.Level)

                            End Code
                                For Each Level In Levels
                                    For Each Unit In Level

                                        @<text>
                                            <tr>
                                                <td>@If (Unit.IsLink) Then @<text><a data-toggle="collapse" data-target=".row@(Unit.Id)"><i class="fa fa-plus"></i></a></text>end If</td>
                                                <td><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = Unit.ProjectId, .unitid = Unit.Id})" class="editCompany">@if Not Unit.Type.Id = 11 Then @Unit.Type.Name End If @Unit.Name</a></td>
                                                <td>@if Unit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @Unit.Level</text> end If</td>
                                                <td>@Unit.Landshare</td>
                                                <td class="text-right " data-title="Acties">@If Not Unit.IsLink Then @<text><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Koppelen" data-id="@Unit.Id" href="#ModalAddLink" class="addLink"><i class="fa fa-chain mr-md"></i></a><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = Unit.ProjectId, .unitid = Unit.Id})" class="editCompany"><i class="fa fa-edit "></i></a></text>End If <a class="deleteUnit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@Unit.Id" href="#ModalDeleteUnit"><i class="fa fa-remove red "></i></a></td>
                                            </tr>
                                        </text>
                                            For Each linkedunit In Unit.LinkedUnits
                                            @<text>
                                                <tr class="collapse row@(Unit.Id)">
                                                    <td></td>
                                                    <td><i class="fa fa-level-down ml-md mr-md"></i><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = linkedunit.ProjectId, .unitid = linkedunit.Id})" class="editCompany">@If Not linkedunit.Type.Id = 11 Then @linkedunit.Type.Name End If @linkedunit.Name</a></td>
                                                    <td>@if Unit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @Unit.Level</text> end If</td>
                                                    <td>@linkedunit.Landshare</td>
                                                    <td class="text-right " data-title="Acties">
                                                        <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("EditUnit", "Projecten", New With {.projectid = linkedunit.ProjectId, .unitid = linkedunit.Id})" class="editCompany"><i class="fa fa-edit "></i></a> <a class="deleteUnit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" data-id="@linkedunit.Id" href="#ModalDeleteUnit"><i class="fa fa-remove red "></i></a>@*<a href="@Url.Action("DeleteCompany", "Leveranciers", New With {.id = company.CompanyId, .SearchName = Model.Filter.CompanyName, .SelectedActivities = ViewData("SelectedActivities").ToString(), .SelectedProvinces = ViewData("selectedprovinces").ToString()})" class="deleteCompany"><i class="fa fa-remove "></i></a>*@
                                                    </td>
                                                </tr>
                                            </text>
                                                Next






                                        Next
                                    Next
                                Next
                            @If Model.UnitsGrouped.Sum(Function(m) m.Units.Sum(Function(x) x.Landshare)) <= Model.ProjectLandShare Then
                            @<text>
                                <tr class="dark">
                                    <td></td>
                                    <td colspan="2">Som verdeling basisakte</td>

                                    <td>@Model.UnitsGrouped.Sum(Function(m) m.Units.Sum(Function(x) x.Landshare)) / @Model.ProjectLandShare</td>
                                    <td></td>
                                </tr>
                            </text>
                            Else
                            @<text>
                                <tr class="danger">
                                    <td></td>
                                    <td colspan="2">Som verdeling basisakte</td>

                                    <td>@Model.UnitsGrouped.Sum(Function(m) m.Units.Sum(Function(x) x.Landshare)) / @Model.ProjectLandShare</td>
                                    <td></td>
                                </tr>
                            </text>
                            End If



                        </tbody>
                    </table>
                </div>
            </div>
        </section>

    </text>
End Using
@section scripts

End Section
