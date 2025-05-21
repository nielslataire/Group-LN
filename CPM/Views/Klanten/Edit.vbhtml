@Imports BO
@ModelType EditClientModel 

@*@Code
    ViewData("Title") = "Klant Bewerken"
End Code*@

@section PageStyle
    <link href="~/vendor/admin/jstree/themes/default/style.css" rel="stylesheet" />
    <link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    

end section

<div class="row">
    <div class="col-md-6 m-none">
        <div class="col-sm-12">
            <section class="panel">
                <header class="panel-heading">
                    <div class="panel-actions">
                        <a class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>
                    <h2 class="panel-title">Algemene gegevens</h2>

                </header>
                <div class="panel-body">
                    @Using Html.BeginForm("Edit", "Klanten", FormMethod.Post, New With {.id = "FormEditClientAccount", .class = "form-horizontal mb-lg"})
                        @*For Each prop In ViewData.ModelMetadata.Properties
                                @Html.Hidden(prop.PropertyName)
                            Next*@

                        @Html.HiddenFor(Function(m) m.Client.Id)
                        @Html.HiddenFor(Function(m) m.Client.OwnerPercentage)
                        @Html.HiddenFor(Function(m) m.ProjectId)
                        @<text>
                            <div Class="form-group">
                                <div Class="col-md-8  col-md-offset-2">
                                    <div Class="checkbox-custom checkbox-default">
                                        @If User.IsInRole("Admin") Then
                                            @<text>
                                                <input type="checkbox" id="checkboxCompany" @If Model.IsCompany = True Then @<text> checked="checked" </text>        End if>
                                                @Html.LabelFor(Function(m) m.IsCompany)
                                            </text>
                                        Else
                                            @<text>
                                                <input type="checkbox" id="checkboxCompany" disabled="disabled" @If Model.IsCompany = True Then @<text> checked="checked" </text>         End if>
                                                @Html.LabelFor(Function(m) m.IsCompany)
                                            </text>
                                        End If
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                @Html.HiddenFor(Function(m) m.ProjectId)
                                @Html.HiddenFor(Function(m) m.ProjectName)
                                <label class="col-md-2 control-label" for="w4-name">@Html.LabelFor(Function(m) m.Client.Name)</label>
                                <div class="col-md-3">
                                    @If User.IsInRole("Admin") Then
                                        @Html.EnumDropDownListFor(Function(m) m.Client.Salutation, New With {.class = "form-control", .id = "lstSalutationAccount"})
                                    Else
                                        @Html.EnumDropDownListFor(Function(m) m.Client.Salutation, New With {.class = "form-control", .id = "lstSalutationAccount", .disabled = "disabled"})
                                    End If
                                </div>
                                <div class="col-md-7">
                                    @If User.IsInRole("Admin") Then
                                        @Html.TextBoxFor(Function(m) m.Client.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtinputname"})
                                    Else
                                        @Html.TextBoxFor(Function(m) m.Client.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtinputname", .disabled = "disabled"})
                                    End If
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label">Bedrijfsgegevens</label>
                                <div class="col-md-6">

                                    @Html.TextBoxFor(Function(m) m.Client.CompanyName, New With {.placeholder = "Bedrijfsnaam", .class = "form-control", .id = "txtCompanyName", .disabled = ""})

                                </div>
                                <div class="col-md-4">
                                    @Html.TextBoxFor(Function(m) m.Client.VATnumber, New With {.placeholder = "BTW nummer", .class = "form-control", .id = "txtVatNumber", .disabled = ""})
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Client.Street)</label>
                                <div class="col-md-6">
                                    @Html.TextBoxFor(Function(m) m.Client.Street, New With {.placeholder = "Straat", .class = "form-control"})
                                </div>
                                <div class="col-md-2">
                                    @Html.TextBoxFor(Function(m) m.Client.Housenumber, New With {.placeholder = "Nr", .class = "form-control"})
                                </div>
                                <div class="col-md-2">
                                    @Html.TextBoxFor(Function(m) m.Client.Busnumber, New With {.placeholder = "Bus", .class = "form-control"})
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-zipcode">Gemeente</label>
                                <div class="col-md-10 p-none">
                                    @Html.EditorFor(Function(m) m.SelectedPostalcode, New With {.PostcodeId = Model.Client.Postalcode.PostcodeId, .Postcode = Model.Client.Postalcode.Postcode, .Gemeente = Model.Client.Postalcode.Gemeente})
                                </div>
                            </div>
                    <hr />
                    <h5 class="col-md-offset-2 ">Facturatiegegevens</h5>
                    <div class="form-group">
                        <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Client.InvoiceStreet)</label>
                        <div class="col-md-6">
                            @Html.TextBoxFor(Function(m) m.Client.InvoiceStreet, New With {.placeholder = "Straat", .class = "form-control"})
                        </div>
                        <div class="col-md-2">
                            @Html.TextBoxFor(Function(m) m.Client.InvoiceHousenumber, New With {.placeholder = "Nr", .class = "form-control"})
                        </div>
                        <div class="col-md-2">
                            @Html.TextBoxFor(Function(m) m.Client.InvoiceBusnumber, New With {.placeholder = "Bus", .class = "form-control"})
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-2 control-label" for="w4-zipcode">Gemeente</label>
                        <div class="col-md-10 p-none">
                            @Html.EditorFor(Function(m) m.SelectedInvoicePostalcode, New With {.PostcodeId = Model.Client.InvoicePostalcode.PostcodeId, .Postcode = Model.Client.InvoicePostalcode.Postcode, .Gemeente = Model.Client.InvoicePostalcode.Gemeente})
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-2 control-label" for="w4-zipcode">Extra info op factuur</label>
                        <div class="col-md-10">
                            @Html.TextBoxFor(Function(m) m.Client.InvoiceExtra, New With {.placeholder = "Extra Info", .class = "form-control"})
                         </div>
                    </div>
                    <hr />
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Client.DateSalesAgreement)</label>
                                <div class="col-md-4">
                                    @Html.EditorFor(Function(m) m.Client.DateSalesAgreement)
                                </div>
                                <label class="col-md-2 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Client.DateDeedOfSale)</label>
                                <div class="col-md-4">
                                    @Html.EditorFor(Function(m) m.Client.DateDeedOfSale)
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="col-md-2 control-label" for="w4-owner">Type</label>
                                <div class="col-md-10">
                                    @If User.IsInRole("Admin") Then
                                        @Html.DropDownListFor(Function(m) m.Client.OwnerType.Id, New SelectList(Model.OwnerTypes, "ID", "Display", Model.Client.OwnerType.Id), New With {.class = "form-control populate", .id = "lstOwnerTypes"})
                                    Else
                                        @Html.DropDownListFor(Function(m) m.Client.OwnerType.Id, New SelectList(Model.OwnerTypes, "ID", "Display", Model.Client.OwnerType.Id), New With {.class = "form-control populate", .id = "lstOwnerTypes", .disabled = "disabled"})
                                    End If
                                </div>
                            </div>
                    <div class="form-group">
                        <label class="col-md-2 control-label">@Html.LabelFor(Function(m) m.Client.StartDateConstruction)</label>
                        <div class="col-md-4">
                            @Html.EditorFor(Function(m) m.Client.StartDateConstruction)
                        
                        </div>
                        <label class="col-md-2 control-label" for="txtExecutiondays">@Html.LabelFor(Function(m) m.Client.ExecutionDays)</label>
                        <div class="col-md-4">
                            <div class="input-group">
                                <span class="input-group-addon">
                                    <i class="fa fa-wrench"></i>
                                </span>
                                @Html.TextBoxFor(Function(m) m.Client.ExecutionDays, New With {.class = "form-control", .id = "txtExecutiondays", .type = "number"})
                            </div>
                        </div>
                    </div>
                    <div class="form-group">
                       
                        <label class="col-md-2 control-label" for="txtExecutiondays">@Html.LabelFor(Function(m) m.Client.BankAccountNumber)</label>
                        <div class="col-md-10">
                            <div class="input-group">
                                <span class="input-group-addon">
                                    <i class="fa fa-bank"></i>
                                </span>
                                @Html.TextBoxFor(Function(m) m.Client.BankAccountNumber, New With {.class = "form-control", .id = "txtBankAccountnumber"})
                            </div>
                        </div>
                    </div>
                            <div class="form-group">
                                <div Class="col-md-12">
                                    <Button Class="btn btn-primary btn-block " type="submit" id="btnSaveData">Opslaan</Button>
                                </div>
                            </div>
                        </text>
                    End Using
                </div>
            </section>
        </div>
        <div Class="col-sm-12">
            <section Class="panel">
                <header Class="panel-heading">
                    <div Class="panel-actions">

                        <a Class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a Class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>

                    <h2 Class="panel-title">Toegiften bij verkoop</h2>

                </header>
                <div Class="panel-body">
                    @If Model.Gifts.Count <> 0 Then
                        @<text>
                            <table id="ContactTable" class="table table-no-more table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Omschrijving</th>
                                        <th>Loten</th>
                                        <th class="text-right">Actie</th>
                                    </tr>
                                </thead>
                                <tbody id="GiftRows">
                                    @For Each item In Model.Gifts
                                        @<Text>
                                        <tr>
                                            <td>
                                                @item.Description

                                            </td>
                                            <td>
                                                @for Each activity In item.Activities
                                                    @activity.Name @Html.Raw("<br/>")
                                                Next
                                            </td>
                                            <td class="actions text-right" data-title="Acties">
                                                <a href="#modaleditgift" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editGift" id="editGift" data-id="@item.Id"><i class="fa fa-edit"></i></a>
                                                <a href="#modaldeletegift" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteGift" data-id="@item.Id"><i class="fa fa-remove "></i></a>
                                            </td>
                                        </tr>

                                        </text>
                                    Next
                                </tbody>
                            </table>
                        </text>
                    Else
                        
                    End If
                    <br />
                    <div class="form-group">
                        <div class="col-sm-12">
                            <a href="#modaladdgift" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Toegift toevoegen" type="button" id="btnAddGift" data-id="@Model.Client.Id "><i class="fa fa-plus"></i> Toevoegen</a>
                        </div>
                    </div>
                </div>
            </section>
            <section Class="panel">
                <header Class="panel-heading">
                    <div Class="panel-actions">

                        <a Class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a Class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>

                    <h2 Class="panel-title">Aandachtspunten</h2>

                </header>
                <div Class="panel-body">
                    @If Model.Poas.Count <> 0 Then
                        @<text>
                            <table id="ContactTable" class="table table-no-more table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Omschrijving</th>
                                        <th>Loten</th>
                                        <th class="text-right">Actie</th>
                                    </tr>
                                </thead>
                                <tbody id="PoaRows">
                                    @For Each item In Model.Poas
                                        @<Text>
                                        <tr>
                                            <td>
                                                @item.Description

                                            </td>
                                            <td>
                                                @for Each activity In item.Activities
                                                    @activity.Name @Html.Raw("<br/>")
                                                Next
                                            </td>
                                            <td class="actions text-right" data-title="Acties">
                                                <a href="#modaleditpoa" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editPoa" id="editPoa" data-id="@item.Id"><i class="fa fa-edit"></i></a>
                                                <a href="#modaldeletepoa" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deletePoa" data-id="@item.Id"><i class="fa fa-remove "></i></a>
                                            </td>
                                        </tr>

                                        </text>
                                    Next
                                </tbody>
                            </table>
                        </text>
                    Else

                    End If
                    <br />
                    <div class="form-group">
                        <div class="col-sm-12">
                            <a href="#modaladdpoa" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Aandachtspunt toevoegen" type="button" id="btnAddPoa" data-id="@Model.Client.Id "><i class="fa fa-plus"></i> Toevoegen</a>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    </div>
    <div Class="col-md-6 m-none">
        <div Class="col-sm-12">
            <section Class="panel">
                <header Class="panel-heading">
                    <div Class="panel-actions">

                        <a Class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a Class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>

                    <h2 Class="panel-title">Contacten</h2></a>

                </header>
                <div Class="panel-body">
                    @If Model.Client.Contacts.Count <> 0 Then
                        @<text>
                            <table id="ContactTable" class="table table-no-more table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Naam</th>
                                        <th>Email</th>
                                        <th>Mobiel</th>
                                        <th>Telefoon</th>
                                        <th class="text-right">Actie</th>
                                    </tr>
                                </thead>
                                <tbody id="ContactRows">
                                    @For Each item In Model.Client.Contacts
                                        @<Text>
                                        <tr>
                                            <td>

                                                @item.Salutation.GetDisplayName @Html.Raw(" ") @item.Name @Html.Raw(" ") @item.Firstname

                                            </td>
                                            <td>
                                                @item.Email
                                            </td>
                                            <td>
                                                @Html.DisplayFor(Function(m) item.FormattedGSM)
                                            </td>
                                            <td>
                                                @Html.DisplayFor(Function(m) item.FormattedTelefoon)
                                            </td>
                                            <td class="actions text-right" data-title="Acties">
                                                <a href="#modaleditcontact" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editContact" id="editContact" data-id="@item.Id"><i class="fa fa-edit"></i></a>
                                                <a href="#modaldeletecontact" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteContact" data-id="@item.Id"><i class="fa fa-remove "></i></a>
                                            </td>
                                        </tr>

                                        </text>
                                    Next
                                </tbody>
                            </table>
                        </text>
                    Else
                        @("Er zijn geen contacten toegevoegd aan deze klant")
                    End If
                    <br />
                    <div class="form-group">
                        <div class="col-sm-12">
                            <a href="#modaladdcontact" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Contact toevoegen" type="button" id="btnAddContact" data-id="@Model.Client.Id "><i class="fa fa-plus"></i> Toevoegen</a>
                        </div>
                    </div>
                </div>
            </section>
        </div>
        <div Class="col-sm-12">
            <section Class="panel">
                <header Class="panel-heading">
                    <div Class="panel-actions">
                        <a Class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a Class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>
                    <h2 Class="panel-title">Mede-Eigenaars</h2>

                </header>
                <div Class="panel-body">
                    @If Model.Client.CoOwners.Count <> 0 Then
                        @<text>

                            <table id="CoOwnerTable" Class="table table-no-more table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Naam</th>
                                        <th>Type</th>
                                        <th Class="text-right">%</th>
                                        @If User.IsInRole("Admin") Then
                                            @<text>
                                                <th Class="text-right">Actie</th>
                                            </text>
                                        End If
                                    </tr>
                                </thead>
                                <tbody id="CoOwnerRows">
                                    @For Each item In Model.Client.CoOwners
                                        @<Text>
                                        <tr>
                                            <td>
                                                @If item.CompanyName Is Nothing Then
                                                    @item.Salutation.GetDisplayName @Html.Raw(" ") @item.Name @Html.Raw(" ") @item.Firstname
                                                Else
                                                    @item.CompanyName
                                                End If
                                            </td>
                                            <td>
                                                @item.CoOwnerType.Name
                                            </td>
                                            <td class="text-right">
                                                @String.Format("{0:F2}", item.CoOwnerPercentage) @Html.Raw(" %")

                                            </td>
                                            @If User.IsInRole("Admin") Then
                                                @<text>
                                                    
                                                    <td Class="actions text-right" data-title="Acties">
                                                        <a href="#modaleditcoowner" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" Class="editCoOwner" id="editCoOwner" data-id="@item.Id"><i Class="fa fa-edit"></i></a>
                                                        <a href="#modaldeletecoowner" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" Class="deleteCoOwner" data-id="@item.Id"><i Class="fa fa-remove "></i></a>
                                                    </td>
                                                </text>
                                            End If

                                        </tr>

                                        </text>
                                    Next
                                </tbody>
                            </table>
                        </text>
                    Else
                        @("Er zijn geen mede-eigenaars toegevoegd aan deze klant")
                    End If
                    @If User.IsInRole("Admin") Then
                        @<text>
                            <br />
                            <div class="form-group">
                                <div class="col-sm-12">
                                    <a href="#modaladdcoowner" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Mede-eigenaar toevoegen" type="button" id="btnAddCoOwner" data-id="@Model.Client.Id "><i class="fa fa-plus"></i> Toevoegen</a>
                                </div>
                            </div>
                        </text>
                    End If


                </div>
            </section>
        </div>
        <div Class="col-sm-12">
            <section Class="panel">
                <header Class="panel-heading">
                    <div Class="panel-actions">
                        <a Class="panel-action panel-action-toggle" href="#" data-panel-toggle=""></a>
                        <a Class="panel-action panel-action-dismiss" href="#" data-panel-dismiss=""></a>
                    </div>
                    <h2 Class="panel-title">Eenheden</h2>

                </header>
                <div Class="panel-body">
                    @If Model.Units.Count <> 0 Then
                        @<text>

                            <table id="UnitsTable" Class="table table-no-more table-striped mb-none">
                                <thead>
                                    <tr>
                                        <th>Project</th>
                                        <th>Eenheid</th>
                                        <th Class="text-right">Prijs</th>
                                        @If User.IsInRole("Admin") Then
                                            @<text>
                                                <th Class="text-right">Actie</th>
                                            </text>
                                        End If

                                    </tr>
                                </thead>
                                <tbody id="UnitRows">
                                    @For Each item In Model.Units
                                        @<Text>
                                        <tr>
                                            <td>
                                                @item.ProjectName
                                            </td>
                                            <td>
                                                @item.Type.Name @Html.Raw(" ") @item.Name
                                            </td>

                                            <td class="text-right ">
                                                @String.Format("{0:C}", item.TotalValueSold)

                                            </td>
                                            @If User.IsInRole("Admin") Then
                                                @<text>
                                                    <td class="actions text-right" data-title="Acties">
                                                        <a href="#modaleditunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" class="editUnit" id="editUnit" data-id="@item.Id"><i class="fa fa-edit"></i></a>
                                                        <a href="#modaldeleteunit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deleteUnit" data-id="@item.Id"><i class="fa fa-remove "></i></a>
                                                    </td>
                                                </text>
                                            End If

                                        </tr>

                                        </text>
                                    Next
                                </tbody>
                            </table>
                        </text>
                    Else
                        @("Er zijn geen eenheden toegevoegd aan deze klant")
                    End If
                    @If User.IsInRole("Admin") Then
                        @<text>
                            <br />
                            <div class="form-group">
                                <div class="col-sm-12">
                                    <a href="#modaladdunit" class="btn btn-default modal-with-form visible-xs-block visible-sm visible-md visible-lg" data-toggle="tooltip" data-placement="top" title="" data-original-title="Eenheid toevoegen" type="button" id="btnAddUnit" data-id="@Model.Client.Id "><i class="fa fa-plus"></i> Toevoegen</a>
                                </div>
                            </div>
                        </text>
                    End If

                </div>
            </section>
        </div>



    </div>
</div>


<div id="modaladdcontact" class="modal-block modal-block-primary mfp-hide">
    <div id="add-contact-container"></div>
</div>
<div id="modaladdcoowner" class="modal-block modal-block-primary mfp-hide">
    <div id="add-coowner-container"></div>
</div>
<div id="modaladdunit" class="modal-block modal-block-primary mfp-hide">
    <div id="add-unit-container"></div>
</div>
<div id="modaladdgift" class="modal-block modal-block-primary mfp-hide">
    <div id="add-gift-container"></div>
</div>
<div id="modaladdpoa" class="modal-block modal-block-primary mfp-hide">
    <div id="add-poa-container"></div>
</div>
<div id="modaleditcontact" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-contact-container"></div>
</div>
<div id="modaleditcoowner" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-coowner-container"></div>
</div>
<div id="modaleditunit" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-unit-container"></div>
</div>
<div id="modaleditgift" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-gift-container"></div>
</div>
<div id="modaleditpoa" class="modal-block modal-block-primary mfp-hide">
        <div id="edit-poa-container"></div>
    </div>
<div id="modaldeletecontact" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-contact-container"></div>
</div>
<div id="modaldeletecoowner" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-coowner-container"></div>
</div>
<div id="modaldeleteunit" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-unit-container"></div>
</div>
<div id="modaldeletegift" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-gift-container"></div>
</div>
<div id="modaldeletepoa" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-poa-container"></div>
</div>
@section scripts
    <script>

        $('#checkboxCompany').change(function () {
            if ($(this).is(":checked")) {

                $('#lstSalutationAccount').attr("disabled", "disabled");
                $('#txtinputname').attr("disabled", "disabled");
                $('#txtVatNumber').removeAttr("disabled");
                $('#txtCompanyName').removeAttr("disabled");
                return;
            }
            $('#lstSalutationAccount').removeAttr("disabled");
            $('#txtinputname').removeAttr("disabled");
            $('#txtVatNumber').attr("disabled", "disabled");
            $('#txtCompanyName').attr("disabled", "disabled");
        });
    //var iCountryId = jQuery("#lstClientCountries option:selected").val();

    ////toevoegen van een activiteit aan company
    //function loadCountry() {
    //    var iCountryId = jQuery("#lstClientCountries option:selected").val();
    //}
    $(window).load(function () {
        @If Not TempData("Message") Is Nothing Then
@<text>

        new PNotify({
            title:      '@TempData("MessageTitle")',
            text:       '@TempData("Message")',
            type:       '@TempData("MessageType")'
        });
        </text>                          End If

        switch(@ViewData("activetab")){
            case 0:
                $('#mytabs a[href="#clientaccount"]').tab('show');
                break;
            case 1:
                $('#mytabs a[href="#coowners"]').tab('show');
                break;
            case 2:
                $('#mytabs a[href="#units"]').tab('show');
                break;
            default:
                $('#mytabs a[href="#clientaccount"]').tab('show');
        };






    });
    $(function () {
        $('#FormEdit').submit(function () {
            if ($(this).valid()) {
                $.ajax({
                    url: this.action,
                    type: this.method,
                    data: $(this).serialize(),
                    success: function (result) {
                        if(result.success === true){

                            window.location.href = result.url;

                        }
                        else{
                            $('#MainValSummary').html(result);
                        }

                    }

                });
            }
            return false;
        });
    });

    //bij selectie land countryid aanpassen ifv de weer te geven postcodes
    //$('#lstClientCountries').on('change', function () {

    //    iCountryId = this.value;
    //    $.ajax({
    //        url: 'GetCountryISOCode',
    //        data: { countryid: iCountryId },
    //        cache: false,
    //        traditional: true,
    //        type: 'POST',
    //        success: function (result) {
    //            $('#txtCountryIsoCode').val(result);
    //        },

    //    });
    //});

    $(document).ready(function () {

            @If User.IsInRole("Admin") Then
             @<text>
                if ($('#checkboxCompany').is(":checked")) {
                    $('#lstSalutationAccount').attr("disabled", "disabled");
                    $('#txtinputname').attr("disabled", "disabled");
                    $('#txtVatNumber').removeAttr("disabled");
                    $('#txtCompanyName').removeAttr("disabled");
                }
        </text>
            End If
        $('.datepicker').datepicker({
            language: 'nl-BE',
            format: 'dd/mm/yyyy',
            autoclose: true,
        });



        $('.addContact').magnificPopup({
            type:  'inline',
            src:  'addContact',
        });
        $('.addCoOwner').magnificPopup({
            type:  'inline',
            src:  'addCoOwner',
        });
        $('.addUnit').magnificPopup({
            type:  'inline',
            src:  'addUnit',
        });
        $('.addGift').magnificPopup({
            type:  'inline',
            src:  'addGift',
        });
        $('.addPoa').magnificPopup({
            type:  'inline',
            src:  'addPoa',
        });
        $('.editContact').magnificPopup({
            type:  'inline',
            src:  'editContact',
        });
        $('.editCoOwner').magnificPopup({
            type:  'inline',
            src:  'editCoOwner',
        });
        $('.editUnit').magnificPopup({
            type:  'inline',
            src:  'editUnit',
        });
        $('.editGift').magnificPopup({
            type:  'inline',
            src:  'editGift',
        });
        $('.editPoa').magnificPopup({
            type:  'inline',
            src:  'editPoa',
        });
        $('.deleteContact').magnificPopup({
            type:  'inline',
            src:  'deleteContact',
        });
        $('.deleteCoOwner').magnificPopup({
            type:  'inline',
            src:  'deleteCoOwner',
        });
        $('.deleteUnit').magnificPopup({
            type:  'inline',
            src:  'deleteUnit',
        });
        $('.deleteGift').magnificPopup({
            type:  'inline',
            src:  'deleteGift',
        });
        $('.deletePoa').magnificPopup({
            type:  'inline',
            src:  'deletePoa',
        });


    });
        $('#btnAddContact').click(function () {
            var url = "/Klanten/AddContactModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#add-contact-container').html(data);
            });

        });
        $('#btnAddCoOwner').click(function () {
            var url = "/Klanten/AddCoOwnerModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#add-coowner-container').html(data);
            });

        });
        $('#btnAddUnit').click(function () {
            var url = "/Klanten/AddUnitModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id , function (data) {
                $('#add-unit-container').html(data);
            });

        });
        $('#btnAddGift').click(function () {
            var url = "/Klanten/AddGiftModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id , function (data) {
                $('#add-gift-container').html(data);
            });

        });
        $('#btnAddPoa').click(function () {
            var url = "/Klanten/AddPoaModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id , function (data) {
                $('#add-poa-container').html(data);
            });

        });
        //bewerken van een contact van een klant
        $('.editContact').click(function () {
            var url = "/Klanten/EditContactModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-contact-container').html(data);
            });
        });
        //bewerken van een mede-eigenaar van een klant
        $('.editCoOwner').click(function () {
            var url = "/Klanten/EditCoOwnerModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-coowner-container').html(data);
            });
        });
        //bewerken van een eenheid van een klant
        $('.editUnit').click(function () {
            var url = "/Klanten/EditUnitModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-unit-container').html(data);
            });
        });
        //bewerken van toegiften van  een klant
        $('.editGift').click(function () {
            var url = "/Klanten/EditGiftModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-gift-container').html(data);
            });
        });
        //bewerken van aandachtspunten van  een klant
        $('.editPoa').click(function () {
            var url = "/Klanten/EditPoaModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#edit-poa-container').html(data);
            });
        });
        //verwijderen van contact uit klant - show modal before delete
        $('.deleteContact').click(function () {
            var url = "/Klanten/DeleteContactModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {

                $('#delete-contact-container').html(data);
            });
        });
        //verwijderen van mede-eigenaar uit klant - show modal before delete
        $('.deleteCoOwner').click(function () {
            var url = "/Klanten/DeleteCoOwnerModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {

                $('#delete-coowner-container').html(data);
            });
        });
        //verwijderen van eenheid uit klant - show modal before delete
        $('.deleteUnit').click(function () {
            var url = "/Klanten/DeleteUnitModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {

                $('#delete-unit-container').html(data);
            });
        });
        //verwijderen van toegift uit klant - show modal before delete
        $('.deleteGift').click(function () {
            var url = "/Klanten/DeleteGiftModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {

                $('#delete-gift-container').html(data);
            });
        });
        //verwijderen van aandachtspunt uit klant - show modal before delete
        $('.deletePoa').click(function () {
            var url = "/Klanten/DeletePoaModal"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {

                $('#delete-poa-container').html(data);
            });
        });

    </script>
    <script src="~/vendor/admin/pnotify/pnotify.custom.js"></script>
    <script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
    <script src="~/vendor/admin/select2/select2.js"></script>
    <script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
    <script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
    <script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
    <script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
    <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
End Section



