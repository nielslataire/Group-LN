@modeltype ActivityDeleteModel 

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
                @If Model.ActivityCount = 0 Then
                    @<text>
                     <h4>Verwijderen @Model.Activity.Name  </h4>
                <p>U staat op het punt om de activiteit @Model.Activity.Name te verwijderen.</p>
                <p>Bent u zeker dat u deze wilt verwijderen?</p>
                </text>
                Else
                    @<text>
                        <h4>Verwijderen @Model.Activity.Name  </h4>
                        <p>U wil de activiteit @Model.Activity.Name verwijderen, maar er zijn nog @Model.ActivityCount leveranciers met deze activiteit</p>
                        <p>Gelieve eerst de activiteit te verwijderen bij deze leveranciers</p>
                    </text>
                End If
               
            </div>
        </div>
    </div>
    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                @If Model.ActivityCount = 0 Then
                    @<text>
                        <a href="@Url.Action("DeleteActivity", "Admin", New With {.id = Model.Activity.ID})" class="btn btn-warning">Verwijderen</a>
                        <button class="btn btn-default modal-dismiss">Annuleren</button>
                    </text>
                Else
                    @<text>
                         <button class="btn btn-default modal-dismiss">OK</button>
                    </text>
                End If
              
            </div>
        </div>
    </footer>
</section>

