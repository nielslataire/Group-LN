@ModelType LeverancierSearchModel
@Code
    ViewData("Title") = "Leverancier zoeken"
End Code
@section PageStyle


    <link rel="stylesheet" href="~/vendor/admin/jquery-ui/css/ui-lightness/jquery-ui-1.10.4.custom.css" />
    <link rel="stylesheet" href="~/vendor/admin/morris/morris.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
    <link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-colorpicker/css/bootstrap-colorpicker.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-timepicker/css/bootstrap-timepicker.css" />
    <link rel="stylesheet" href="~/vendor/admin/dropzone/css/basic.css" />
    <link rel="stylesheet" href="~/vendor/admin/dropzone/css/dropzone.css" />
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-markdown/css/bootstrap-markdown.min.css" />
    <link rel="stylesheet" href="~/vendor/admin/summernote/summernote.css" />
    <link rel="stylesheet" href="~/vendor/admin/summernote/summernote-bs3.css" />
    <link rel="stylesheet" href="~/vendor/admin/codemirror/lib/codemirror.css" />
    <link rel="stylesheet" href="~/vendor/admin/codemirror/theme/monokai.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

end section

<div class="row">
    <div class="col-md-12">
        @Using Html.BeginForm("Zoeken", "Leveranciers", FormMethod.Post, New With {.class = "form-horizontal"})
            @<text>
                <section class="panel" id="pnlSearch">
                    <header class="panel-heading">
                        <div class="panel-actions">
                            <a href="#" class="panel-action panel-action-toggle" data-panel-toggle></a>
                            <a href="#" class="panel-action panel-action-dismiss" data-panel-dismiss></a>
                        </div>

                        <h2 class="panel-title">Zoeken op ....</h2>

                    </header>
                    <div class="panel-body">
                        <div class="form-group">
                            <label class="col-lg-3 control-label">
                                @Html.LabelFor(Function(m) m.Filter.CompanyName)
                            </label>
                            <div class="col-lg-9">
                                @*if you check the model you'll see there is a <required> attribute, that tells mvc that this is a required property*@
                                @Html.TextBoxFor(Function(m) m.Filter.CompanyName, New With {.placeholder = "vb. Copro", .class = "form-control"})
                                @Html.ValidationMessageFor(Function(m) m.Filter.CompanyName)
                                @Html.HiddenFor(Function(m) m.Filter.CompanyName)
                                @*<input type="text" name="Bedrijfsnaam" class="form-control" placeholder="vb. Copro" required />*@
                            </div>
                        </div>
                        <div class="form-group">
                            <label class="col-lg-3 control-label">Activiteiten</label>
                            <div class="col-lg-9">
                                @Html.ListBoxFor(Function(m) m.Filter.SelectedActivities, New SelectList(Model.Filter.Activities, "ID", "Display", "Group", Model.Filter.SelectedActivities, Nothing, Nothing), New With {.class = "form-control", .multiple = "", .data_plugin_selecttwo = ""})

                            </div>
                        </div>
                        <div class="form-group">
                            <label class="col-md-3 control-label">Provincies</label>
                            <div class="col-md-9">
                                @Html.DropDownListFor(Function(m) m.Filter.SelectedProvince, New SelectList(Model.Filter.Provinces, "ID", "Display", "Group", Model.Filter.SelectedProvince, Nothing, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = ""})

                            </div>
                        </div>
                    </div>
                    <footer class="panel-footer">
                        <div class="row">
                            <div class="col-sm-9 col-sm-offset-3">
                                <button class="btn btn-primary" type="submit">Zoeken</button>
                                <button type="reset" class="btn btn-default">Reset</button>
                            </div>
                        </div>
                    </footer>
                </section>
            </text>
        End Using

    </div>
    @If (Model.Companies IsNot Nothing AndAlso Model.Companies.Count > 0) Then

        @<text>

    <div class="col-lg-12">
        <div class="row">
            <div class="col-sm-12 text-right ">
                <div class="btn-group">
                    <button type="button" class="mb-xs mt-xs mr-xs btn btn-default  dropdown-toggle" data-toggle="dropdown"><i class="glyphicon glyphicon-export"></i> <span class="caret"></span></button>
                    <ul class="dropdown-menu" role="menu">
                        <li><a href="@Url.Action("ZoekenExportToPdf", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")})">PDF</a></li>
                        <li><a href="@Url.Action("ZoekenExportToExcel", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")})">Excel</a></li>
                    </ul>
                </div>
                <button type="button" value="Create" onclick="location.href='@Url.Action("ZoekenPrint", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")})'" class="mb-xs mt-xs mr-xs btn btn-default hidden-tablet hidden-phone"><i class="fa fa-print"></i></button>
                   @*@Html.ActionLink("Afdrukken", "ZoekenPrint", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")}, New With {.class = "mb-xs mt-xs mr-xs btn btn-default hidden-tablet hidden-phone"})*@
                   @*@Html.ActionLink("Exporteren", "ZoekenExportToExcel", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")}, New With {.class = "btn btn-primary ml-xs mb-xs hidden-tablet hidden-phone"})
                @Html.ActionLink("PDF", "ZoekenExportToPdf", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")}, New With {.class = "btn btn-primary ml-xs mb-xs hidden-tablet hidden-phone"})*@


            </div>
        </div>
        <section class="panel" id="pnlList">
            <header class="panel-heading">
                <h2 class="panel-title">Overzicht</h2>
                <div class="panel-actions ">
                    @*<a href="@Url.Action( "ZoekenPrint", "Leveranciers", New With {.name = ViewData("FilterCompanyName"), .act = ViewData("selectedactivities"), .prov = ViewData("selectedprovinces")})" class="hidden-tablet hidden-phone" data-toggle="tooltip" data-placement="bottom" title="" data-original-title="Afdrukken" target="_blank"><i class="fa fa-print "></i></a>*@
                </div>
            </header>
            <div class="panel-body">
                <table class="table table-no-more table-bordered table-striped mb-none">
                    <thead>
                        <tr>
                            <th>Bedrijfsnaam</th>
                            <th>Adres</th>
                            <th>Gemeente</th>
                            <th>Telefoon</th>
                            <th>GSM</th>
                            <th>Email</th>
                            <th class="text-right">Acties</th>
                        </tr>
                    </thead>
                    <tbody>
                        @* Do a for each on the Model.companies here and display a line per found company *@

                        @For Each company In Model.Companies
                            @<text>
                                <tr>
                                    <td data-title="Naam">
                                        @Html.ActionLink(company.Bedrijfsnaam, "Detail", "Leveranciers", New With {.id = company.CompanyId}, Nothing)
                                        @*@Html.DisplayFor(Function(m) company.Bedrijfsnaam)*@
                                    </td>
                                    <td data-title="Adres">@Html.DisplayFor(Function(m) company.Straat) @Html.DisplayFor(Function(m) company.Huisnummer)@Html.DisplayFor(Function(m) company.Toevoeging) @Html.DisplayFor(Function(m) company.Busnummer) </td>
                                    <td data-title="Gemeente">@Html.DisplayFor(Function(m) company.Postcode.Gemeente)</td>
                                    <td data-title="GSM">
                                        @If Not company.Telefoon1 Is Nothing Then
                                            @Html.DisplayFor(Function(m) company.FormattedTelefoon)
                                        Else
                                            @:-
                                        End If
                                    </td>
                                    <td data-title="GSM">
                                        @If Not company.GSM Is Nothing Then
                                            @Html.DisplayFor(Function(m) company.FormattedGSM)
                                        Else
                                            @:-
                                        End If
                                    </td>
                                    <td data-title="Email">
                                        @If Not company.Email Is Nothing Then

                                            @<text>
                                                <div class="hidden-xs"><a href='mailto:@Html.ValueFor(Function(m) company.Email)'>@Html.ValueFor(Function(m) company.Email)</a></div>
                                                <div class="visible-xs actions ">
                                                    <a href='mailto:@Html.ValueFor(Function(m) company.Email)' data-toggle="tooltip" data-placement="top" title="" data-original-title="Mail"><i class="fa fa-envelope"></i></a>
                                                </div>
                                            </text>

                                        Else
                                            @:-
                                        End If

                                    </td>
                                    <td class="text-right " data-title="Acties"><a data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" href="@Url.Action("Bewerken", "Leveranciers", New With {.id = company.CompanyId, .activetab = 0})" class="editCompany"><i class="fa fa-edit "></i></a> <a data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="modal-basic" href="#modalwarning@(company.companyid)"><i class="fa fa-remove red "></i></a>@*<a href="@Url.Action("DeleteCompany", "Leveranciers", New With {.id = company.CompanyId, .SearchName = Model.Filter.CompanyName, .SelectedActivities = ViewData("SelectedActivities").ToString(), .SelectedProvinces = ViewData("selectedprovinces").ToString()})" class="deleteCompany"><i class="fa fa-remove "></i></a>*@</td>
                                </tr>

                            </text>
                        Next
                    </tbody>
                </table>
                @For Each company In Model.Companies
                    @<text>
                        <div id="modalwarning@(company.companyid)" class="modal-block modal-block-warning mfp-hide">
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">Opgelet!</h2>
                                </header>
                                <div class="panel-body">
                                    <div class="modal-wrapper">
                                        <div class="modal-icon">
                                            <i class="fa fa-warning"></i>
                                        </div>
                                        <div class="modal-text">
                                            <h4>Verwijderen @company.Bedrijfsnaam</h4>
                                            <p>U staat op het punt om de leverancier @company.Bedrijfsnaam met al zijn afdelingen, activiteiten en contacten te verwijderen.</p>
                                            <p>Bent u zeker dat u deze wilt verwijderen?</p>
                                        </div>
                                    </div>
                                </div>
                                <footer class="panel-footer">
                                    <div class="row">
                                        <div class="col-md-12 text-right">
                                            <a href="@Url.Action("DeleteCompany", "Leveranciers", New With {.id = company.CompanyId, .SearchName = Model.Filter.CompanyName, .SelectedActivities = ViewData("SelectedActivities").ToString(), .SelectedProvinces = ViewData("selectedprovinces").ToString()})" class="btn btn-warning">Verwijderen</a>
                                            <button class="btn btn-default modal-dismiss">Annuleren</button>
                                        </div>
                                    </div>
                                </footer>
                            </section>
                        </div>
                    </text>
                Next
            </div>
        </section>

    </div>
        </text>

    End If
    @If Model.Searchempty = True Then
        @<text>
            <div class="col-lg-12">
                <h3>Geen zoekresultaten gevonden ...</h3>
            </div>
        </text>
    End If
    </div>
    @section scripts
        <script src="~/vendor/admin/jquery-ui/js/jquery-ui-1.10.4.custom.js"></script>
        <script src="~/vendor/admin/jquery-ui-touch-punch/jquery.ui.touch-punch.js"></script>
        <script src="~/vendor/admin/select2/select2.js"></script>
        <script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
        <script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
        <script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
        <script src="~/vendor/admin/bootstrap-colorpicker/js/bootstrap-colorpicker.js"></script>
        <script src="~/vendor/admin/bootstrap-timepicker/js/bootstrap-timepicker.js"></script>
        <script src="~/vendor/admin/fuelux/js/spinner.js"></script>
        <script src="~/vendor/admin/dropzone/dropzone.js"></script>
        <script src="~/vendor/admin/bootstrap-markdown/js/markdown.js"></script>
        <script src="~/vendor/admin/bootstrap-markdown/js/to-markdown.js"></script>
        <script src="~/vendor/admin/bootstrap-markdown/js/bootstrap-markdown.js"></script>
        <script src="~/vendor/admin/codemirscript/javascript.js"></script>
        <script src="~/vendor/admin/codemirror/mode/xml/xml.js"></script>
        <script src="~/vendor/admin/codemirror/mode/htmlmixed/htmlmixed.js"></script>
        <script src="~/vendor/admin/codemirror/mode/css/css.js"></script>
        <script src="~/vendor/admin/summernoteror/lib/codemirror.js"></script>
        <script src="~/vendor/admin/codemirror/addon/selection/active-line.js"></script>
        <script src="~/vendor/admin/codemirror/addon/edit/matchbrackets.js"></script>
        <script src="~/vendor/admin/codemirror/mode/java/summernote.js"></script>
        <script src="~/vendor/admin/bootstrap-maxlength/bootstrap-maxlength.js"></script>
        <script src="~/vendor/admin/ios7-switch/ios7-switch.js"></script>
        <script src="~/vendor/admin/bootstrap-confirmation/bootstrap-confirmation.js"></script>
        <script src="~/scripts/admin/forms/examples.advanced.form.js"></script>
        <script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
        <script>
            $(window).load(function () {
                @If Not TempData("Message") Is Nothing Then
@<text>

                new PNotify({
                    title: '@TempData("MessageTitle")',
                    text: '@TempData("Message")',
                    type: '@TempData("MessageType")'
                });
                </text>
            End If
            });
        </script>
    end section
