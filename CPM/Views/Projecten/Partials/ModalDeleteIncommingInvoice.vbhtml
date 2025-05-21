@modeltype BO.IncommingInvoiceBO 
<section class="panel">
<header class="panel-heading">
    <h2 class="panel-title">Factuur verwijderen!</h2>
</header>
<div class="panel-body">
    <div class="modal-wrapper">
        <div class="modal-icon">
            <i class="fa fa-warning"></i>
        </div>
        <div class="modal-text">
            @If Not Model.CompanyId = 0 Then
                @<text>
                    <p>U staat op het punt om de factuur van @Html.Action("GetCompanyName", New With {.companyid = Model.CompanyId}) te verwijderen.</p>
                </text>
            Else
                @<text>
                    <p>U staat op het punt om de factuur te verwijderen.</p>
                </text>
            End If

            <p>Ook alle andere lijnen verbonden aan deze factuur worden verwijderd.</p>
            <p>Bent u zeker dat u deze wilt verwijderen?</p>
        </div>
    </div>
</div>
<footer class="panel-footer">
    <div class="row">
        <div class="col-md-12 text-right">
            <a href="@Url.Action("DeleteIncommingInvoice", "Projecten", New With {.id = Model.Id, .projectid = Model.ProjectId})" class="btn btn-warning btn-block">Verwijderen</a>
            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
        </div>
    </div>
</footer>
</section>
