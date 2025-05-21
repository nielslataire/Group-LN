@ModelType LeverancierModel 

@Code
    ViewData("Title") = "Leveranciers Detail"
End Code

<div class="row">
    <div class="col-sm-12 text-right ">
        <div class="btn-group ">
            <button type="button" data-toggle="tooltip" data-placement="top" title="Bewerken" onclick="location.href='@Url.Action("Bewerken", "Leveranciers", New With {.id = Model.Company.CompanyId, .activetab = 0})'" class="btn btn-default ml-xs mb-xs"><i class="fa fa-edit"></i></button>
            <button type="button" data-toggle="tooltip" data-placement="top" title="Afdrukken" onclick="location.href='@Url.Action("DetailPrint", "Leveranciers", New With {.id = Model.Company.CompanyId})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-print"></i></button>
            <button type="button" data-toggle="tooltip" data-placement="top" title="Naar PDF" onclick="location.href='@Url.Action("DetailExportToPdf", "Leveranciers", New With {.id = Model.Company.CompanyId})'" class="btn btn-default ml-xs mb-xs  hidden-tablet hidden-phone"><i class="fa fa-file-pdf-o"></i></button>

            @*@Html.ActionLink("Bewerken", "Bewerken", "Leveranciers", New With {.id = Model.Company.CompanyId, .activetab = 0}, New With {.class = "btn btn-default ml-xs mb-xs"})
            @Html.ActionLink("Afdrukken", "DetailPrint", "Leveranciers", New With {.id = Model.Company.CompanyId}, New With {.class = "btn btn-primary ml-xs mb-xs hidden-tablet hidden-phone"})*@
        </div>
    </div>
</div>
<section class="panel">
    <div class="panel-body">

        <div class="invoice">
            <header class="clearfix">
                <div class="row">
                    <div class="col-sm-6 mt-md">
                        <h2
                            class="h2 mt-none mb-sm text-dark text-weight-bold">@Html.ValueFor(Function(m) m.Company.Bedrijfsnaam)</h2>
                        <h4 class="h4 m-none text-dark text-weight-bold">#@Html.ValueFor(Function(m) m.Company.CompanyId)</h4>
                    </div>
                    <div class="col-sm-6 text-right mt-md mb-md">
                        <address class="ib mr-xlg">
                            @Html.ValueFor(Function(m) m.Company.Straat) @Html.ValueFor(Function(m) m.Company.Huisnummer)@Html.ValueFor(Function(m) m.Company.Toevoeging) @If Model.Company.Busnummer IsNot Nothing Then
                                                                                                                            @: /
                                                                                                                            @Html.ValueFor(Function(m) m.Company.Busnummer)
                                                                                                                          End If
                            
                            <br />
                           @Html.ValueFor(Function(m) m.Company.Postcode.Postcode ) @Html.ValueFor(Function(m) m.Company.Postcode.Gemeente).ToString.ToUpper 
                            <br />
                            @Html.ValueFor(Function(m) m.Company.Postcode.Provincie.Name)
                            <br />
                            @Html.ValueFor(Function(m) m.Company.Postcode.Country.Name).ToString.ToUpper 
                            <br />
                            @If Model.FormattedTelefoon IsNot Nothing Then
                                @:Telefoon :
                                @Html.DisplayFor(Function(m) m.FormattedTelefoon)
                                @:<br />
                            End if
                            @If Model.FormattedGSM IsNot Nothing Then
                                @:GSM :
                                @Html.DisplayFor(Function(m) m.FormattedGSM)
                                @:<br />
                            End If
                            <a href="mailto:@Html.ValueFor(Function(m) m.Company.Email)">@Html.ValueFor(Function(m) m.Company.Email)</a>
</address>
                       
                    </div>
                </div>
            </header>
            <div class="bill-info">
                <div class="row">
                    <div class="col-md-6">
                        <div class="bill-to">
                            <p class="h5 mb-xs text-dark text-weight-semibold">Detail:</p>
                            <address>
                                @If Model.Company.URL IsNot Nothing Then
                                    @:Website :
                                    @:<a href="http://@Html.ValueFor(Function(m) m.Company.URL)" target="_blank">@Html.ValueFor(Function(m) m.Company.URL)</a>
                                    @:<br />
                            End If
                                @If Model.Company.OndNr IsNot Nothing Then
                                    @:Ondernemingsnummer :
                                   @Html.ValueFor(Function(m) m.FormattedONDNR )
                                    @:<br />
                            End If
                                @If Model.Company.Opmerking IsNot Nothing Then
                                    @: <p class="h5 mb-xs text-dark text-weight-semibold">Opmerkingen:</p>
                                    @Html.ValueFor(Function(m) m.Company.Opmerking)
                                    @:<br />
                            End If
                            </address>

                        </div>
                    </div>
                   
                </div>
            </div>
            @If Not Model.Company.Activities.Count = 0 Then
                @<text>
            
                <p class="h4 mb-xs text-dark text-weight-semibold">Activiteiten:</p>
                <br />
                <table class="table table-no-more table-striped mb-none">
                    <thead>
                        <tr class="h5 text-dark">
                            <th id="cell-id" class="text-weight-semibold">#</th>
                            <th id="cell-desc" class="text-weight-semibold">Activiteit</th>

                        </tr>
                    </thead>
                    <tbody>
                        @For Each item In Model.Company.Activities
                        Html.RenderPartial("ActivityRow", item, New ViewDataDictionary() From {{"mode", "view"}})
                        Next

                    </tbody>
                </table>
            
            </text>
                End If
            @If Not Model.Company.Departments.Count = 0 Then
                @<text>
                    
                        <p class="h4 mb-xs text-dark text-weight-semibold">Afdelingen:</p>
                        <br />
            <table class="table table-no-more table-striped mb-none">
                <thead>
                    <tr class="h5 text-dark">

                        <th id="cell-item" class="text-weight-semibold">Naam</th>
                        <th id="cell-item" class="text-weight-semibold">Straat</th>
                        <th id="cell-desc" class="text-weight-semibold">Gemeente</th>
                        <th id="cell-desc" class="text-weight-semibold">Land</th>
                        <th id="cell-desc" class="text-weight-semibold">Telefoon</th>
                        <th id="cell-desc" class="text-weight-semibold">GSM</th>
                        <th id="cell-item" class="text-weight-semibold">Email</th>
                    </tr>
                </thead>
                <tbody>
                    @For Each item In Model.Company.Departments
                    Html.RenderPartial("DepartmentRow", item, New ViewDataDictionary() From {{"mode", "view"}})
                    Next

                </tbody>
            </table>


                   
                </text>
            End If
            @If Not Model.Company.Contacts.Count = 0 Then
                @<text>
                    
                        <p class="h4 mb-xs text-dark text-weight-semibold">Contacten:</p>
                        <br />
            <table class="table table-no-more table-striped mb-none">
                <thead>
                    <tr class="h5 text-dark">


                        <th id="cell-desc" class="text-weight-semibold">Naam</th>
                        <th id="cell-desc" class="text-weight-semibold">Functie</th>
                        <th id="cell-desc" class="text-weight-semibold">Afdeling</th>
                        <th id="cell-desc" class="text-weight-semibold">Telefoon</th>
                        <th id="cell-desc" class="text-weight-semibold">GSM</th>
                        <th id="cell-item" class="text-weight-semibold">Email</th>

                    </tr>
                </thead>
                <tbody>
                    @For Each item In Model.Company.Contacts
                    Html.RenderPartial("ContactRow", item, New ViewDataDictionary() From {{"mode", "view"}})
                    Next

                </tbody>
            </table>

                   
                </text>
            End If
           
        </div>

        
    </div>
</section>
