@modeltype BO.InsuranceBO
@imports bo
<section class="panel">
<header class="panel-heading">
    <h2 class="panel-title">Verzekering verwijderen!</h2>
</header>
<div class="panel-body">
    <div class="modal-wrapper">
        <div class="modal-icon">
            <i class="fa fa-warning"></i>
        </div>
        <div class="modal-text">
          
            <p>U staat op het punt om de @Model.Type.GetDisplayName() van de maatschappij @Model.InsuranceCompany.Name  te verwijderen.</p>
            <p>Bent u zeker dat u deze wilt verwijderen?</p>
        </div>
    </div>
</div>
<footer class="panel-footer">
    <div class="row">
        <div class="col-md-12 text-right">
            <a href="@Url.Action("DeleteInsurance", "Projecten", New With {.id = Model.ContractActivityID, .projectid = Model.ProjectID})" class="btn btn-warning btn-block">Verwijderen</a>
            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
        </div>
    </div>
</footer>
</section>
