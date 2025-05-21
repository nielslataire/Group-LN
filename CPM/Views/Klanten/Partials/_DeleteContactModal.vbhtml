@modeltype BO.ClientContactBO 

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
                <h4>Verwijderen @Model.Name @Model.Firstname </h4>
                <p>U staat op het punt om het contact @Model.Name @Model.Firstname te verwijderen.</p>
                <p>Bent u zeker dat u deze wilt verwijderen?</p>
            </div>
        </div>
    </div>
    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                <a href="@Url.Action("DeleteContact", "Klanten", New With {.id = Model.Id, .accountid = Model.AccountId})" class="btn btn-warning btn-block">Verwijderen</a>
                <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
            </div>
        </div>
    </footer>
</section>
