@modeltype DepartmentModel

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
                <h4>Verwijderen @Model.Department.Name</h4>
                <p>U staat op het punt om de afdeling @Model.Department.Name te verwijderen.</p>
                <p>Bent u zeker dat u deze wilt verwijderen?</p>
            </div>
        </div>
    </div>
    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                <a href="@Url.Action("DeleteDepartment", "Leveranciers", New With {.id = Model.Department.ID, .companyid = Model.Department.Company.ID})" class="btn btn-warning btn-block">Verwijderen</a>
                <button class="btn btn-default modal-dismiss btn-block">Annuleren</button>
            </div>
        </div>
    </footer>
</section>
