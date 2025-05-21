@modeltype BO.ProjectSalesSettingsBO
@imports bo
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

    @Using Html.BeginForm("SalesSettings", "Projecten", FormMethod.Post, New With {.id = "FormSalesSettings", .class = "form-horizontal mb-lg"})
    @<text>
        @Html.HiddenFor(Function(m) m.SettingsId)
        @Html.HiddenFor(Function(m) m.ProjectId)
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Instellingen verkoop :</h2>
            </header>
            <div class="panel-body">
                <div class="form-group">
                    <label class="col-md-4 control-label" for="salevisible">@Html.LabelFor(Function(m) m.SaleVisible)</label>
                    <div class="col-md-8">
                        <div class="checkbox">
                            <label>
                                @Html.CheckBoxFor(Function(m) m.SaleVisible, New With {.id = "salevisible"})
                            </label>
                        </div>

                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="registrationtype">@Html.LabelFor(Function(m) m.RegistrationType)</label>
                    <div class="col-md-8">
                        @Html.EnumDropDownListFor(Function(m) m.RegistrationType, New With {.class = "form-control", .id = "lstType"})

                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="mixedvatregistration">@Html.LabelFor(Function(m) m.MixedVatRegistration)</label>
                    <div class="col-md-8">
                        <div class="checkbox">
                            <label>
                                @Html.CheckBoxFor(Function(m) m.MixedVatRegistration, New With {.id = "mixedvatregistration"})
                            </label>
                        </div>

                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtvatpercentage">@Html.LabelFor(Function(m) m.VatPercentage)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.VatPercentage, New With {.class = "form-control", .id = "txtvatpercentage", .type = "numeric"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtregistrationpercentage">@Html.LabelFor(Function(m) m.RegistrationPercentage)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.RegistrationPercentage, New With {.class = "form-control", .id = "txtregistrationpercentage", .type = "numeric"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtconnectionfees">@Html.LabelFor(Function(m) m.ConnectionFees)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.ConnectionFees, New With {.class = "form-control", .id = "txtconnectionfees"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtbasecertificatecost">@Html.LabelFor(Function(m) m.BaseCertificateCost)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.BaseCertificateCost, New With {.class = "form-control", .id = "txtbasecertificatecost"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtbasecertificatecost">@Html.LabelFor(Function(m) m.ParcelCost)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.ParcelCost, New With {.class = "form-control", .id = "txtparcelcost"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtfixedcertificatecost">@Html.LabelFor(Function(m) m.FixedCertificateCost)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.FixedCertificateCost, New With {.class = "form-control", .id = "txtfixedcertificatecost"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtMortageRegistrationCost">@Html.LabelFor(Function(m) m.MortageRegistrationCost)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.MortageRegistrationCost, New With {.class = "form-control", .id = "txtMortageRegistrationCost"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtMortageRegistrationCost">@Html.LabelFor(Function(m) m.SurveyorCost)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.SurveyorCost, New With {.class = "form-control"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label" for="txtMortageRegistrationCost">@Html.LabelFor(Function(m) m.BankAccountNumber)</label>
                    <div class="col-md-8">
                        @Html.EditorFor(Function(m) m.BankAccountNumber, New With {.class = "form-control", .id = "txtBankaccountnumber"})
                    </div>
                </div>

            </div>

            <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block ">Opslaan</button>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
    End Using

