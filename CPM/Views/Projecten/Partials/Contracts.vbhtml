@imports bo
@modeltype DetailContractsModel

@section PageStyle

End Section

<div class="inner-toolbar clearfix">
    <ul>
        <li>
            <a href="@Url.Action("ContractAdd", "Projecten", New With {.projectid = Model.ProjectId})" class="btn" type="button" id="btnAddContract"><i class="fa fa-plus"></i> Contract toevoegen</a>
        </li>
    </ul>
</div>


        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
        <h2 class="panel-title">Overzicht contracten</h2>
        <br />
        <section class="panel">
            <div class="panel-body">

                    <table class="table table-no-bordered table-no-more table-striped">
                        <thead>
                            <tr>
                                <th data-sortable="true">Bedrijf</th>
                                <th data-sortable="true" class="text-right">Contractprijs</th>
                                <th class="hidden-xs text-right">BTW %</th>
                                <th class="hidden-xs text-right">Betaaltermijn</th>
                                <th class="hidden-xs text-right">Korting contant</th>    
                                <th class="hidden-xs text-right">Borg</th>
                                <th class="hidden-xs">Acties</th>
                            </tr>
                        </thead>
                        <tbody>

                       @For Each item In Model.Contracts.OrderBy(Function(o) o.Company.Display)
                                            @<text>

                                                <tr>
                                                    <td>
                                                         @item.Company.Display
                                            
                                                            </td>
                                                   <td class="text-right">
                                                       @String.Format("{0:C}", item.Activities.Sum(Function(m) m.Price))
                                                   </td>
                                                    <td class="text-right">
                                                    @If Not item.VatPercentage Is Nothing Then
                                                        @<text>
                                                        @Html.DisplayFor(Function(m) item.VatPercentage)                                                </text>
                                                    Else
                                                        @<text>
                                                            -
                                                        </text>

                                                    End If
                                                    </td>
                                                    <td class="text-right">
                                                        @If Not item.PaymentTerm Is Nothing Then
                                                            @<text>
                                                        @Html.DisplayFor(Function(m) item.PaymentTerm) dagen                                           </text>
                                                        Else
                                                            @<text>
                                                                -
                                                            </text>
                                                        End If
                                                    </td>
                                                    <td class="text-right">
                                                        @if Not item.CashDiscount = False Then
                                                            @<text>
                                                            @Html.DisplayFor(Function(m) item.CashDiscountPercentage) binnen de @Html.DisplayFor(Function(m) item.CashDiscountPaymentTerm) dagen
                                                        </text>
                                                        Else
                                                            @<text>
                                                               -
                                                            </text>
                                                        End If
                                                    </td>
                                                    <td class="text-right">
                                                        @if item.GuaranteeType = ContractGuaranteeType.FinancialGuarantee Then
                                                            @<text>
                                                                @Html.DisplayFor(Function(m) item.GuaranteePercentage) @item.GuaranteeType.GetDisplayName()
                                                            </text>
                                                        Else
                                                            @<text>
                                                                @item.GuaranteeType.GetDisplayName()
                                                            </text>

                                                        End If
                                                    </td>
                                                    <td>
                                                        <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" id="editContract" onclick="location.href='@Url.Action("ContractAdd", "Projecten", New With {.projectid = Model.ProjectId, .contractid = item.Id})'"><i class="fa fa-edit"></i></a>
                                                        <a class="deleteContract" data-id="@item.Id" href="#ModalDeleteContract" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen"><i class="fa fa-remove"></i></a>
                                                    </td>
                                                </tr>
                                            </text>
                       Next
                    </tbody>
                    </table>

            </div>
</section>
