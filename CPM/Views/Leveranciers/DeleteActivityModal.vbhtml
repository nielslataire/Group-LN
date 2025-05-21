@modeltype CompanyActivityModel

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
                <h4>Verwijderen @Model.Activity.Name </h4>
                <p>U staat op het punt om de activiteit @Model.Activity.Name te verwijderen van leverancier @Model.CompanyName .</p>
                <p>Bent u zeker dat u deze wilt verwijderen?</p>
            </div>
        </div>
    </div>
    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                <a href="@Url.Action("DeleteCompanyActivity", "Leveranciers", New With {.id = Model.Activity.ID, .companyid = Model.CompanyId})" class="btn btn-warning  btn-block">Verwijderen</a>
                <button class="btn btn-default modal-dismiss btn-block">Annuleren</button>
            </div>
        </div>
    </footer>
</section>
