@modeltype InsuranceBO 
@imports bo

    @Using Html.BeginForm("EndInsurance", "Projecten", FormMethod.Post, New With {.id = "FormEndInsurance", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
    @Html.AntiForgeryToken()
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectID, New With {.id = "projectid"})
        @Html.HiddenFor(Function(m) m.ExtensionPeriod)
@Html.HiddenFor(Function(m) m.GuaranteePeriod)
@Html.HiddenFor(Function(m) m.ContractActivityID)
@Html.HiddenFor(Function(m) m.InsuranceBrokerName)
@Html.HiddenFor(Function(m) m.InsuranceCompany.Id)
@Html.HiddenFor(Function(m) m.Period)
@Html.HiddenFor(Function(m) m.Startdate)
@Html.HiddenFor(Function(m) m.Type)
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Verzekering beëindigen :</h2>
            </header>
            <div class="panel-body">
                <div class="form-group">
                    <label class="col-md-3 control-label" for="txtCompany">@Html.LabelFor(Function(m) m.Enddate)</label>
                    <div class="col-md-9">
                        @Html.EditorFor(Function(m) m.Enddate)
                    </div>
                </div>
                </div>

    <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <button class="btn btn-primary btn-block ">Verzekering beëindigen</button>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
    End Using
<script>
   
</script>