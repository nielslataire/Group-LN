@modeltype ProjectAddInsurancesModel 
@imports bo

    @Using Html.BeginForm("AddInsurance", "Projecten", FormMethod.Post, New With {.id = "FormAddPhoto", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
    @Html.AntiForgeryToken()
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
@Html.HiddenFor(Function(m) m.Insurance.Id, New With {.id = "id"})
        @Html.HiddenFor(Function(m) m.Insurance.ContractActivityID, New With {.id = "contractactivityid"})
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Verzekering toevoegen :</h2>
            </header>
            <div class="panel-body">
                @*<div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Brokers)</label>
                    <div class="col-md-9">
                        @Html.DropDownListFor(Function(m) m.Insurance.InsuranceBrokerID, New SelectList(Model.Brokers, "ID", "Display", Model.Insurance.InsuranceBrokerID), New With {.class = "form-control populate", .id = "lstBrokers"})
                    </div>
                 </div>*@
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Companies)</label>
                    <div class="col-md-9">
                        @Html.DropDownListFor(Function(m) m.Insurance.InsuranceCompany.Id, New SelectList(Model.Companies, "ID", "Display", Model.Insurance.InsuranceCompany.Id), New With {.class = "form-control populate", .id = "lstCompanies"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.Type)</label>
                    <div class="col-md-9">
                        @Html.EnumDropDownListFor(Function(m) m.Insurance.Type, New With {.class = "form-control", .id = "lstType"})
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.Startdate)</label>
                    <div class="col-md-9">
                        
                            @Html.EditorFor(Function(m) m.Insurance.Startdate)
                         
                        
                    </div>
                </div>
               
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.Period)</label>
                    <div class="col-md-9">
                        <div class="input-group">
                            @Html.TextBoxFor(Function(m) m.Insurance.Period, New With {.class = "form-control", .data_toggle = "title", .title = "maanden", .id = "txtPeriod"}).DisableIf(Function() Model.Insurance.Type <> InsuranceType.ABR)
                            <span class="input-group-addon ">maanden</span>
                        </div>
                        </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.ExtensionPeriod)</label>
                    <div class="col-md-9">
                        <div class="input-group">
                            @Html.TextBoxFor(Function(m) m.Insurance.ExtensionPeriod, New With {.class = "form-control", .data_toggle = "title", .title = "maanden", .id = "txtExtension"}).DisableIf(Function() Model.Insurance.Type <> InsuranceType.ABR)
                            <span class="input-group-addon ">maanden</span>
                        </div>
                        </div>
                </div>
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.GuaranteePeriod)</label>
                    <div class="col-md-9">
                        <div class="input-group">
                            @Html.TextBoxFor(Function(m) m.Insurance.GuaranteePeriod, New With {.class = "form-control", .data_toggle = "title", .title = "maanden", .id = "txtGuarantee"}).DisableIf(Function() Model.Insurance.Type <> InsuranceType.ABR)
                            <span class="input-group-addon ">maanden</span>
                        </div>  
                    </div>
             
                </div>
                @If Not Model.Insurance.ContractActivityID = 0 Then
                    @<text>
                        <div Class="form-group">
                            <Label Class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Insurance.Enddate)</Label>
                            <div class="col-md-9">
                                @Html.EditorFor(Function(m) m.Insurance.Enddate)
                            </div>
                        </div>
                    </text>
                End If

                </div>

    <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block ">
                            @If Not Model.Insurance.ContractActivityID = 0 Then
                                @<text>
                                   Bewerken
                                </text>
                            Else
                                @<text>
                                 Toevoegen
                                </text>
                            End If
                            
                        </button>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
End Using
<script>
    $(document).ready(function () { $("#lstCompanies").select2(); });
    $('#lstType').on('change', function () {
          
        var val = $(this).val();
        if(val==1){
            $('#txtPeriod').attr("disabled", false);
            $('#txtExtension').attr("disabled", false);
            $('#txtGuarantee').attr("disabled", false);
        }
        else {
            $('#txtPeriod').attr("disabled", true);
            $('#txtExtension').attr("disabled", true);
            $('#txtGuarantee').attr("disabled", true);
        }
           
    });
</script>