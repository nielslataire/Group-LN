@modeltype bo.ActivityGroupBO 

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
                @If Model.Activities.Count = 0 Then
                    @<text>
                        <h4>Verwijderen @Model.Name  </h4>
                        <p>U staat op het punt om de groep @Model.Name te verwijderen.</p>
                        <p>Bent u zeker dat u deze wilt verwijderen?</p>
                    </text>
                Else
                    @<text>
                        <h4>Verwijderen @Model.Name  </h4>
                        <p>U wil de groep @Model.Name verwijderen, maar er zijn nog @Model.Activities.Count activiteiten gekoppeld met deze groep.</p>
                        <p>Gelieve eerst de activiteiten te verwijderen</p>
                    </text>
                End If

            </div>
        </div>
    </div>
    <footer class="panel-footer">
        <div class="row">
            <div class="col-md-12 text-right">
                @If Model.Activities.Count = 0 Then
                    @<text>
                        <a href="@Url.Action("DeleteGroup", "Admin", New With {.id = Model.ID})" class="btn btn-warning">Verwijderen</a>
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
