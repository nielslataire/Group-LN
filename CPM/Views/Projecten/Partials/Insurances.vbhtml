@imports bo
@modeltype DetailInsurancesModel

@section PageStyle

    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

End Section

<div class="inner-toolbar clearfix">
    <ul>
            @*<li>
                <a href="#ModalAddInsurance" class="addInsurance" type="button" id="addInsurance" data-id="@Model.ProjectId"><i class="fa fa-plus"></i> Toevoegen</a>
            </li>*@
    </ul>
</div>


        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
        <h2 class="panel-title">Overzicht verzekeringen</h2>
<br />
       <section class="panel">

    <div class="panel-body">

        <table class="table table-no-more table-bordered table-striped mb-none">
            <thead>
                <tr>
                   
                    <td>Makelaar</td>
                    <td>Maatschappij</td>
                    <td>Type</td>
                    <td>Startdatum</td>
                    <td>Termijn</td>
                    <td>Verlenging</td>
                    <td>Waarborgperiode</td>
                    <td>Einddatum</td>
                    <td>Acties</td>
                </tr>

            </thead>
            <tbody>
                @for Each insurance In Model.Insurances
                    @<text>
                        <tr>
                           
                            <td>@Html.DisplayFor(Function(m) insurance.InsuranceBrokerName)</td>
                            <td>@Html.DisplayFor(Function(m) insurance.InsuranceCompany.Name)</td>
                            <td>
                                @if Not insurance.Type = 0 Then
                            @<text>
                                @insurance.Type.GetDisplayName()
                            </text>
                            end If
                            </td>
                            <td>@Html.DisplayFor(Function(m) insurance.Startdate)</td>
                            @If insurance.Type = InsuranceType.ABR Then
                                @<text>
                            <td>@insurance.Startdate.AddMonths(insurance.Period).ToString("dd/MM/yyyy")</td>
                            <td>@insurance.Startdate.AddMonths(insurance.Period).ToString("dd/MM/yyyy") - @insurance.Startdate.AddMonths(insurance.Period + insurance.ExtensionPeriod).ToString("dd/MM/yyyy")</td>
                            <td>@insurance.Startdate.AddMonths(insurance.Period + insurance.ExtensionPeriod).ToString("dd/MM/yyyy") - @insurance.Startdate.AddMonths(insurance.Period + insurance.ExtensionPeriod + insurance.GuaranteePeriod).ToString("dd/MM/yyyy")</td>

                                </text>
                            Else
                                @<text>
                                    <td>-</td>
                                    <td>-</td>
                                    <td>-</td>

                                </text>
                            End If
                            @If insurance.Enddate Is Nothing Then
                                @<text>
                                    <td>-</td>
                                </text>
                            Else
                                @<text>
                                 <td>@Html.DisplayFor(Function(m) insurance.Enddate)</td>

                                </text>
                            End If
                            <td>
                                <a href="#modaleditinsurance" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editinsurance modal-with-form" data-id="@insurance.ContractActivityID"><i class="fa fa-edit "></i></a>
                                @*<a href="#modaldeleteinsurance" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteinsurance modal-with-form" data-id="@insurance.ContractActivityID"><i class="fa fa-remove "></i></a>*@
                               @If insurance.Enddate Is Nothing Then
                               @<text>
                                 <a href = "#modalendinsurance" data-toggle="tooltip" data-placement="top" title="" data-original-title="Beeindigen" Class="endinsurance modal-with-form" data-id="@insurance.ContractActivityID"><i Class="fa fa-stop "></i></a>
                                </text>
                               End If
                            </td>
                        </tr>
                    </text>
                Next
            </tbody>
        </table>
    </div>
</section>
@section scripts

End Section