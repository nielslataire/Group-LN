@imports bo
@modeltype DetailInvoicingModel

@section PageStyle

    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

End Section

<div class="inner-toolbar clearfix">
    <ul>
        <li>
            <a href="#modaladddoc" class="btn modal-with-form " type="button" id="btnAddDoc"><i class="fa fa-plus"></i> Toevoegen</a>
        </li>
    </ul>
</div>


@Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
@Html.HiddenFor(Function(m) m.Client.Id, New With {.id = "txtClientAccountId"})
<br />
<section class="panel">
    <header class="panel-heading">
        <div class="panel-actions">
            <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
            <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
        </div>

        <h2 class="panel-title">Overzicht Facturatie - @Model.ClientName</h2>
    </header>
    <div class="panel-body">
        <div class="table-responsive">
            <table class="table table-bordered table-striped  mb-none">
                <thead>
                    <tr>
                        <td>Docnr.</td>
                        <td>Factuurnummer</td>
                        <td>Factuurdatum</td>
                        <td>Vervaldatum</td>
                        <td>Bedrag</td>
                        <td>Acties</td>
                    </tr>

                </thead>
                <tbody>
                    @for Each invoice In Model.Invoices.Where(Function(m) m.ClientId = Model.Client.Id AndAlso m.ClientType =  ClientType.klant)
                        @<text>
                            <tr>
                                <td>@invoice.Id</td>
                                <td>@Html.DisplayFor(Function(m) invoice.PublicId)</td>
                                <td>
                                    @Html.DisplayFor(Function(m) invoice.Invoicedate)
                                </td>
                                <td>
                                    @Html.DisplayFor(Function(m) invoice.ExpirationDate)
                                </td>
                                <td>
                                    @String.Format("{0:C}", invoice.Rows.Sum(Function(m) (m.Price / 100 * m.VatPercentage) + m.Price))
                                </td>
                                <td>
                                    <a href="@Url.Action("InvoiceToPdf", "Klanten", New With {.id = invoice.Id})" data-toggle="tooltip" data-placement="top" title="" data-original-title="PDF openen" data-id="@invoice.Id"><i class="fa fa-file-pdf-o fa-lg "></i></a>
                                </td>
                                @*<td><a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Filename </span></a></td>
                                    <td><a href="#modaldeletedoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deletedoc modal-with-form" data-id="@doc.Docid"><i class="fa fa-remove "></i></a></td>*@
                            </tr>
                        </text>
                    Next
                </tbody>
            </table>
        </div>
    </div>

</section>
@For Each coowner In Model.Client.CoOwners
    @<text>
        <section class="panel">
            <header class="panel-heading">
                <div class="panel-actions">
                    <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                    <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
                </div>
                @If coowner.CompanyName Is Nothing Or coowner.CompanyName = "" Then

                End If
                <h2 class="panel-title">Overzicht Facturatie - @coowner.Name</h2>
            </header>
            <div class="panel-body">
                <div class="table-responsive">
                    <table class="table table-bordered table-striped  mb-none">
                        <thead>
                            <tr>
                                <td>Docnr.</td>
                                <td>Factuurnummer</td>
                                <td>Factuurdatum</td>
                                <td>Vervaldatum</td>
                                <td>Bedrag</td>
                                <td>Acties</td>
                            </tr>

                        </thead>
                        <tbody>
                            @for Each invoice In Model.Invoices.Where(Function(m) m.ClientId = coowner.Id andalso m.ClientType =  ClientType.Medeeigenaar)
                                @<text>
                                    <tr>
                                        <td>@invoice.Id</td>
                                        <td>@Html.DisplayFor(Function(m) invoice.PublicId)</td>
                                        <td>
                                            @Html.DisplayFor(Function(m) invoice.Invoicedate)
                                        </td>
                                        <td>
                                            @Html.DisplayFor(Function(m) invoice.ExpirationDate)
                                        </td>
                                        <td>
                                            @String.Format("{0:C}", invoice.Rows.Sum(Function(m) (m.Price / 100 * m.VatPercentage) + m.Price))
                                        </td>
                                        <td>
                                            <a href="@Url.Action("InvoiceToPdf", "Klanten", New With {.id = invoice.Id})" data-toggle="tooltip" data-placement="top" title="" data-original-title="PDF openen" data-id="@invoice.Id"><i class="fa fa-file-pdf-o fa-lg "></i></a>
                                        </td>
                                        @*<td><a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Filename </span></a></td>
                                    <td><a href="#modaldeletedoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deletedoc modal-with-form" data-id="@doc.Docid"><i class="fa fa-remove "></i></a></td>*@
                                    </tr>
                                </text>
                            Next
                        </tbody>
                    </table>
                </div>
            </div>

        </section>
    </text>
Next

@section scripts


End Section