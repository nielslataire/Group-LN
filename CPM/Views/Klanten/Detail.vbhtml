@Imports BO
@ModelType ClientModel 

@Code
    ViewData("Title") = "Klant Detail"
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />
End Section

<div class="row">
    <div class="col-sm-12 text-right ">
        <div class="btn-group ">
            <button type="button" data-toggle="tooltip" data-placement="top" title="Bewerken" onclick="location.href='@Url.Action("Edit", "Klanten", New With {.projectid = Model.ProjectId, .clientid = Model.Client.Id, .activetab = 0})'" class="btn btn-default ml-xs mb-xs"><i class="fa fa-edit"></i></button>
            <button type="button" data-toggle="tooltip" data-placement="top" title="Naar PDF" onclick="location.href='@Url.Action("DetailExportToPdf", "Klanten", New With {.id = Model.Client.Id, .projectid = Model.ProjectId})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-file-pdf-o"></i></button>

            @*<button   type="button" data-toggle="tooltip" data-placement="top" title="Afdrukken" onclick="location.href='@Url.Action("DetailPrint", "Klanten", New With {.id = Model.Client.Id})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-print"></i></button>
            <button  type="button" data-toggle="tooltip" data-placement="top" title="Naar PDF" onclick="location.href='@Url.Action("DetailExportToPdf", "Klanten", New With {.id = Model.Client.Id})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-file-pdf-o"></i></button>*@
        </div>
    </div>
</div>
<div class="col-xl-3 col-lg-6">
    <h4>
        Klantgegevens
    </h4>
    <div class="panel-group" id="accordion">
        <div class="panel panel-accordion">
            <div class="panel-heading">
                <h4 class="panel-title">
                    <a class="accordion-toggle text-weight-bold" data-toggle="collapse" data-parent="#accordion" href="#collapse1One">
                     @if Model.Client.CompanyName Is Nothing Then
                        @Model.Client.Salutation.GetDisplayName().ToUpper()
                     End If
                         @Html.Raw(" ") @Model.Client.DisplayName.ToUpper()
                </a>
                </h4>
            </div>
            <div id="collapse1One" class="accordion-body collapse in">
                <div class="panel-body">
                            
                            <ul class="list list-icons">
                                <li>
                                    <i class="fa fa-map-marker"></i>
                                    <strong>Adres: </strong>@Model.Client.Street @Model.Client.Housenumber
                                    @If Model.Client.Busnumber IsNot Nothing Then
                                    @<text>
                                        / @Model.Client.Busnumber
                                    </text>
                                    End If
                                    <br />
                                    @Model.Client.Postalcode.Postcode @Model.Client.Postalcode.Gemeente.ToUpper<br />
                                    @Model.Client.Postalcode.Country.Name

                                </li>
                                @if Model.Client.VATnumber IsNot Nothing Then
                                        @<text>
                                    <li>
                                <i Class="fa fa-pie-circle"></i>
                                    <strong> BTW-nummer : </strong>@Model.Client.VATnumber
                                </li>
                                </text>
                                End If
                                

                                <li>
                                    <i Class="fa fa-pie-chart"></i>
                                    <strong> @Model.Client.OwnerType.Name : </strong>@(100 - Model.Client.CoOwners.Select(Function(m) m.CoOwnerPercentage).Sum) @Html.Raw("% ")
                                </li>
                                @If Not Model.Client.DateDeedOfSale Is Nothing Then
                                    @<text>
                                        
                                        <li>
                                            <i Class="fa fa-briefcase "></i>
                                            <strong> Aktedatum : </strong>@Html.DisplayFor(Function(m) m.Client.DateDeedOfSale)
                                        </li>
                                    </text>
                                End If
                                @If Not Model.Client.DeliveryDate Is Nothing Then
                                    @<text>
                                        <li>
                                            <i Class="fa fa-key"></i>
                                            <strong> Opleverdatum : </strong>                                 
                                            @If Not Model.Client.DeliveryDoc Is Nothing Or Not Model.Client.DeliveryDoc = "" Then
                                                    @<text>
                                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "docs/" & Model.Client.DeliveryDoc)" target="_blank">@Html.DisplayFor(Function(m) m.Client.DeliveryDate)</a>
                                                    </text>
                                            Else
                                                    @Html.DisplayFor(Function(m) m.Client.DeliveryDate)
                                            End If
                                   
                                        </li>
                                    </text>
                                Else
                                    @<text>
                                        <li>
                                            <a href="#modaldelivery" class="modal-with-form" data-toggle="tooltip" data-placement="top" title="" data-original-title="Oplevergegevens wijzigen" type="button" id="btnDelivery" data-id="@Model.Client.Id"><i class="fa fa-key"></i></a>
                                            <strong> Opleverdatum :</strong> - nog niet opgeleverd -
                                        </li>
                                    </text>
                                End If


                            </ul>
                            @If Model.Client.Contacts IsNot Nothing Then
                                @<text>
                                    <hr>
                                            <h5 Class="text-primary mb-md text-center">Contactpersonen</h5>
                                </text>
                            End If
                            @For Each contact In Model.Client.Contacts

                                @<text>

                                    <ul Class="list list-icons  ">
                                        <li>
                                                    <i Class="fa fa-user"></i>
                                            <strong> Naam :   </strong>@contact.Salutation.GetDisplayName() @Html.Raw(" ") @contact.Name @Html.Raw(" ") @contact.Firstname
                                        </li>

                                        @If contact.Phone IsNot Nothing Then
                                            @<text>
                                                <li>
                                                        <i Class="fa fa-phone"></i>
                                                    <strong> Telefoon :   </strong>@Html.DisplayFor(Function(m) contact.FormattedTelefoon)
                                                </li>

                                            </text>
                                        End If
                                        @If contact.Cellphone IsNot Nothing Then
                                            @<text>
                                                <li>
                                                            <i Class="fa fa-mobile-phone"></i>
                                                    <strong> Mobiel :   </strong>@Html.DisplayFor(Function(m) contact.FormattedGSM)
                                                </li>

                                            </text>
                                        End If
                                        @If contact.Email IsNot Nothing Then
                                            @<text>

                                                <li>
                                                                <i Class="fa fa-envelope"></i>
                                                    <strong> Email :   </strong>@Html.DisplayFor(Function(m) contact.Email)
                                                </li>

                                            </text>
                                        End If
                                    </ul>
                                </text>
                            Next
                            </div>
            </div>
        </div>
        @For Each coowner In Model.Client.CoOwners
            @<text>
        <div Class="panel panel-accordion">
            <div Class="panel-heading">
                <h4 Class="panel-title">
                    <a Class="accordion-toggle text-weight-bold" data-toggle="collapse" data-parent="#accordion" href="#@coowner.Id">
                      @If coowner.Name IsNot Nothing Then
                        @coowner.Salutation.GetDisplayName().ToUpper @Html.Raw(" ") @coowner.Name.ToUpper @Html.Raw(" ") @coowner.Firstname.ToUpper
                      Else
                          @coowner.CompanyName
                      End If

                    </a>
                </h4>
            </div>
            <div id = "@coowner.Id" Class="accordion-body collapse">
                <div Class="panel-body">
                    <ul Class="list list-icons  ">
                        <li>
                                                                                            <i Class="fa fa-map-marker"></i>
                            <strong> Adres :   </strong>@coowner.Street @coowner.Housenumber
                            @If coowner.Busnumber IsNot Nothing Then
                                @<text>
                                    / @coowner.Busnumber
                                </text>
                            End If
                                                                                                <br />
                            @coowner.Postalcode.Postcode @coowner.Postalcode.Gemeente.ToUpper<Br />
                            @coowner.Postalcode.Country.Name
                        </li>
                        @if coowner.VATnumber IsNot Nothing Then
                            @<text>
                                <li>
                                                                                                <i Class="fa fa-pie-circle"></i>
                                    <strong> BTW-nummer : </strong>@coowner.VATnumber
                                </li>
                            </text>
                        End If



                                                                                                    <li>
                                                                                                    <i Class="fa fa-pie-chart"></i>
                                    <strong> @coowner.CoOwnerType.Name : </strong>@coowner.CoOwnerPercentage @Html.Raw("% ")
                                </li>



                        @If coowner.Phone IsNot Nothing Then
                            @<text>
                                <li>
                                                                                                        <i Class="fa fa-phone"></i>
                                    <strong> Telefoon :   </strong>@Html.DisplayFor(Function(m) coowner.FormattedTelefoon)
                                </li>

                            </text>
                        End If
                        @If coowner.Cellphone IsNot Nothing Then
                            @<text>
                                <li>
                                                                                                            <i Class="fa fa-mobile-phone"></i>
                                    <strong> Mobiel :   </strong>@Html.DisplayFor(Function(m) coowner.FormattedGSM)
                                </li>

                            </text>
                        End If
                        @If coowner.Email IsNot Nothing Then
                            @<text>

                                <li>
                                                                                                                <i Class="fa fa-envelope"></i>
                                    <strong> Email :   </strong>@Html.DisplayFor(Function(m) coowner.Email)
                                </li>

                            </text>
                        End If
                    </ul>

                </div>
            </div>
        </div>
        </text>
        Next

    </div>


</div>

<div Class="col-xl-4 col-lg-6">
    <h4>
        Eenheden
    </h4>
    <div class="panel-group" id="accordion2">

        @For Each item In Model.UnitsWithStages
            @<text>
                <div Class="panel panel-accordion">
                    <div Class="panel-heading">
                        <h4 Class="panel-title">
                            <a Class="accordion-toggle text-weight-bold" data-toggle="collapse" data-parent="#accordion2" href="#@("u" & item.Unit.Id)">
                                @item.Unit.Type.Name.ToUpper @Html.Raw(" ") @item.Unit.Name.ToUpper 
                                @If item.Unit.Type.GroupId = 1 Or item.Unit.Type.GroupId = 4 Then
                                    If item.Unit.Street IsNot Nothing Then
                                    @Html.Raw(" - ") @item.Unit.Street.ToUpper @Html.Raw(" ") @item.Unit.HouseNumber 
                                        If item.Unit.BusNumber IsNot Nothing Then
                                        @Html.Raw(" BUS ") @item.Unit.BusNumber
                                        End If
                                    End If
                                End If
                            </a>
                        </h4>
                    </div>
                    <div id = "@("u" & item.Unit.Id)" Class="accordion-body collapse">
                        <div Class="panel-body">
                            <ul Class="list list-icons">
                                @for Each stage In item.PaymentStages
                                    @<text>
                                        <li>
                                            @if Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).Count > 0 Then
                                            @if Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).Count = 1 Then
                                            @<text>
                                                <i Class="fa fa-check "></i>
                                                <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("InvoiceWebURL") & Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).FirstOrDefault().Invoicedate.Year() & "\" & Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id)).FirstOrDefault().Filename)" target="_blank"> @stage.Name</a><div class="pull-right">@stage.Percentage.ToString("0.##") % </div>
                                            </text>
                                            Else
                                            @<text>

                                                <i Class="fa fa-check "></i>
                                                 @stage.Name<div class="pull-right">@stage.Percentage.ToString("0.##") % </div> <br />
                                                @for Each invoice In Model.Invoices.Where(Function(m) m.Rows.Any(Function(i) i.UnitId = item.Unit.Id AndAlso i.StageId = stage.Id))
                                            @<text>
                                            <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("InvoiceWebURL") & invoice.Invoicedate.Year() & "\" & invoice.Filename)" target="_blank"> @(invoice.Filename.Substring(12, invoice.Filename.Length - 17)) </a> <br/>
                                            </text>

                                                Next
                                            </text>
                                            End If

                                                    Else
                                            @stage.Name@<text><div Class="pull-right">@stage.Percentage.ToString("0.##") % </div>                                            </text>
                                            End If
           
                                               
                                            
                                        </li>
                                    </text>
Next
                            </ul>

                        </div>
                    </div>
                </div>
            </text>
        Next
        @If Model.Gifts.Count > 0 Then
            @<text>
                
                    <section Class="panel">
                        <div Class="panel-heading">
                            <h5 Class="panel-title text-primary text-weight-bold " style="font-size:12px;font-weight:700;">
                                TOEGIFTEN
                            </h5>
                        </div>
                        <div Class="panel-body">
                            <div Class="col-sm-1"></div>
                            <div Class="col-sm-11">

                                <ul Class="list">
                                    @For Each gift In Model.Gifts
                                        @<text>
                                            <li>
                                                @gift.Description
                                            </li>
                                        </text>
                                    Next
                                </ul>
                            </div>
                        </div>
                    </section>
                
            </text>
        End If
        @If Model.Poas.Count > 0 Then
            @<text>

                <section Class="panel">
                    <div Class="panel-heading">
                        <h5 Class="panel-title text-primary text-weight-bold " style="font-size:12px;font-weight:700;">
                            AANDACHTSPUNTEN 
                        </h5>
                    </div>
                    <div Class="panel-body">
                        <div Class="col-sm-1"></div>
                        <div Class="col-sm-11">

                            <ul Class="list">
                                @For Each POA In Model.Poas
                                    @<text>
                                        <li>
                                            @POA.Description
                                        </li>
                                    </text>
                                Next
                            </ul>
                        </div>
                    </div>
                </section>

            </text>
        End If
    </div>
    <section class="panel">
        <header class="panel-heading">
            <div class="panel-actions">
                <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
            </div>

            <h2 class="panel-title">
                <span class="va-middle">Facturatie</span>
            </h2>
        </header>
        <div class="panel-body">
            <div class="content">
                <ul class="simple-">
                    @*@For Each doc In Model.LatestDocs
                        @<text>
                            <li>

                                @if  doc.Name Is Nothing Or doc.Name = "" Then
                                    @<text>
                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"><span Class="title">@doc.Type.GetDisplayName()</span></a>
                                        @If doc.DocDate.HasValue Then
                                            @<text>
                                                <br /><span Class="message truncate">@doc.DocDate.Value.ToLongDateString</span>
                                            </text>
                                        Else

                                        End If

                                    </text>
                                Else
                                    @<text>
                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Name</span></a>
                                        @If doc.DocDate.HasValue Then
                                            @<text>
                                                <br /><span Class="message truncate">@doc.Type.GetDisplayName() - @doc.DocDate.Value.ToLongDateString</span>
                                            </text>
                                        Else
                                            @<text>
                                                <br /><span Class="message truncate">@doc.Type.GetDisplayName()</span>
                                            </text>
                                        End If

                                    </text>

                                End If
                            </li>
                        </text>
                    Next*@

                </ul>


                <div Class="text-right">
                    <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Documenten" href="@Url.Action("DetailInvoicing", "Klanten", New With {.projectid = Model.ProjectId, .clientid = Model.Client.Id})">
                        (Ga naar facturatie)
                    </a>
                </div>
                @*<hr Class="dotted short">
                <div class="social-icons-list">
                    <a rel="tooltip" data-placement="bottom" class="modal-with-form" id="btnAddPhoto" href="#modaladddoc" data-original-title="Document toevoegen"><i class="fa fa-plus"></i><span>Document toevoegen</span></a>
                </div>*@
            </div>
        </div>

    </section>

</div>
<div Class="col-xl-4 col-lg-6">
@If Model.Client.DeliveryDate Is Nothing Then
    @<text>

    <h4>
        Termijnen
    </h4>
    <section Class="panel">

        <div Class="panel-body bg-primary">
            <div Class="widget-summary widget-summary-md">
                <div Class="widget-summary-col widget-summary-col-icon">
                    <div Class="summary-icon">
                        <i Class="fa fa-building"></i>
                    </div>
                </div>
                <div Class="widget-summary-col">
                    <div Class="summary">
                        <h4 Class="title">Werkdagen resterend</h4>
                        <div Class="info">
                            @If Model.WorkingDaysLeft = -9999 Then
                                @<text>
                                    Kan nog geen werkdagen berekenen, gelieve de startdatum en werkdagen In te geven
                                </text>
                            Else
                                @<text>
                                    <strong Class="amount">@Model.WorkingDaysLeft</strong>
                                </text>
                            End If

                        </div>
                    </div>
                    @*<div class="summary-footer">
                        <a class="text-uppercase">(view all)</a>
                    </div>*@
                </div>
            </div>
        </div>
    </section>

    <section class="panel">
        <div class="panel-body bg-primary">
            <div class="widget-summary widget-summary-md">
                <div class="widget-summary-col widget-summary-col-icon">
                    <div class="summary-icon">
                        <i class="fa fa-calendar"></i>
                    </div>
                </div>
                <div class="widget-summary-col">
                    <div class="summary">
                        <h4 class="title">Uiterste einddatum</h4>
                        <div class="info">
                            @If Model.FinalConstructionDate = DateTime.MinValue Then
                                @<text>
                                    Kan nog geen datum berekenen, gelieve de startdatum en werkdagen in te geven
                                </text>
                            Else
                                    @<text>
                                 <strong Class="amount">@Model.FinalConstructionDate.ToString("ddd dd MMMM yyyy")</strong>
                            </text>
                            End If
                           
                        </div>
                    </div>
                    @*<div class="summary-footer">
                            <a class="text-uppercase">(view all)</a>
                        </div>*@
                </div>
            </div>
        </div>
        @If Not Model.FinalConstructionDate = DateTime.MinValue Then
            @<text>
                <div class="panel-footer panel-footer-btn-group">
                    <a href="@Url.Action("CalendarToPdf", "Klanten", New With {.id = Model.Client.Id, .projectid = Model.ProjectId})" target="_blank" ><i class="fa fa-file-pdf-o mr-xs"></i> Kalender afdrukken</a>
                </div>
            </text>
        End If
    </section>
    </text>
End If
    @*<section class="panel">
        <div class="panel-heading">
            <h5 class="panel-title text-primary text-weight-bold " style="font-size:12px;font-weight:700;">
                UITVOERINGSTERMIJN
            </h5>
        </div>
        <div class="panel-body">
            <div class="col-sm-1"></div>
            <div class="col-sm-11">
                Werkdagen: @Model.ExecutionDays <br />
                Startdatum werken: @Model.StartDate.ToLongDateString  <br />
                Einde van de werken : @Model.FinalConstructionDate.ToLongDateString
            </div>
        </div>
    </section>*@
    <section class="panel">
        <header class="panel-heading">
            <div class="panel-actions">
                <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
            </div>

            <h2 class="panel-title">
                <span class="va-middle">Documenten</span>
            </h2>
        </header>
        <div class="panel-body">
            <div class="content">
                <ul class="simple-">
                    @For Each doc In Model.LatestDocs
                        @<text>
                            <li>

                                @if  doc.Name Is Nothing Or doc.Name = "" Then
                                    @<text>
                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"><span Class="title">@doc.Type.GetDisplayName()</span></a>
                                        @If doc.DocDate.HasValue Then
                                            @<text>
                                                <br /><span Class="message truncate">@doc.DocDate.Value.ToLongDateString</span>
                                            </text>
                                        Else

                                        End If

                                    </text>
                                Else
                                    @<text>
                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Name</span></a>
                                        @If doc.DocDate.HasValue Then
                                            @<text>
                                <br /><span Class="message truncate">@doc.Type.GetDisplayName() - @doc.DocDate.Value.ToLongDateString</span>
                                            </text>
                                        Else
                                            @<text>
                                <br /><span Class="message truncate">@doc.Type.GetDisplayName()</span>
                                            </text>
                                        End If

                                    </text>

                                End If
                            </li>
                        </text>
                    Next

                </ul>
                @Html.Partial("ModalAddDocument", New BO.ProjectDocBO With {.ProjectId = Model.ProjectId, .ClientAccountId = Model.Client.Id, .Type = BO.ProjectDocType.Sales})


                <div Class="text-right">
                    <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Documenten" href="@Url.Action("DetailDocs", "Klanten", New With {.projectid = Model.ProjectId, .clientid = Model.Client.Id})">
                        (Toon Alle)
                    </a>
                </div>
                <hr Class="dotted short">
                <div class="social-icons-list">
                    <a rel="tooltip" data-placement="bottom" class="modal-with-form" id="btnAddPhoto" href="#modaladddoc" data-original-title="Document toevoegen"><i class="fa fa-plus"></i><span>Document toevoegen</span></a>
                </div>
            </div>
        </div>

    </section>
    <section class="panel">
        <header class="panel-heading">
            <div class="panel-actions">
                <a href="#" class="panel-action panel-action-toggle" data-panel-toggle=""></a>
                <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss=""></a>
            </div>

            <h2 class="panel-title">
                <span class="va-middle">Wijzigingsopdrachten</span>
            </h2>
        </header>
        <div class="panel-body">
            <div class="content">
                <ul class="list list-icons">
                    @For Each order In Model.ChangeOrders
                        @<text>
                        <li>
                            @if order.DateAgreement IsNot Nothing Then
                                @<text>
                                    <i Class="fa fa-check "></i>
                            <a href="@Url.Action("ChangeOrderPDF", "Projecten", New With {.changeorderid = order.Id})" >@order.Description</a><div class="pull-right">@Html.DisplayFor(Function(m) order.DateAgreement)</div>
                                </text>
                            Else
                                @<text>
                                    <a href="@Url.Action("ChangeOrderPDF", "Projecten", New With {.changeorderid = order.Id})" >@order.Description</a><div class="pull-right"><a href="@Url.Action("ChangeOrderAddUpdate", "Projecten", New With {.projectid = Model.ProjectId, .changeorderid = order.Id})">Bewerken</a></div>
                                </text>
                            End If
                        </li>
                        </text>
                    Next

                </ul>
                @Html.Partial("ModalAddDocument", New BO.ProjectDocBO With {.ProjectId = Model.ProjectId, .ClientAccountId = Model.Client.Id, .Type = BO.ProjectDocType.Sales})


                <div Class="text-right">
                    <a Class="text-uppercase text-muted" data-toggle="tooltip" data-placement="top" title="" data-original-title="Documenten" href="@Url.Action("DetailDocs", "Klanten", New With {.projectid = Model.ProjectId, .clientid = Model.Client.Id})">
                        (Toon Alle)
                    </a>
                </div>
                <hr Class="dotted short">
                <div class="social-icons-list">
                    <a rel="tooltip" data-placement="bottom" class="modal-with-form" id="btnAddPhoto" href="#modaladddoc" data-original-title="Document toevoegen"><i class="fa fa-plus"></i><span>Document toevoegen</span></a>
                </div>
            </div>
        </div>

    </section>
</div>

<div id="modaldelivery" class="modal-block modal-block-primary mfp-hide">
    <div id="delivery-container"></div>
</div>
 
@section scripts

    <script>

        $(window).load(function () {
            @If Not TempData("Message") Is Nothing Then
        @<text>

            new PNotify({
                title:      '@TempData("MessageTitle")',
                text:       '@TempData("Message")',
                type:       '@TempData("MessageType")'
            });
            </text>
            End If
        });
      
        //toevoegen van een afdeling aan company
        $('#btnDelivery').click(function () {
            var url = "/Klanten/SetDelivery"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id + "?projectid=" + @Model.ProjectId, function (data) {
                $('#delivery-container').html(data);
            });
        });
       
        $(document).ready(function () {


        });

    </script>
    <script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
    <script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
    <script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
    <script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
end section