@modeltype DeleteStageDocModel 
<section class="panel">
<header class="panel-heading">
    <h2 class="panel-title">Document verwijderen!</h2>
</header>
<div class="panel-body">
    <div class="modal-wrapper">
        <div class="modal-icon">
            <i class="fa fa-warning"></i>
        </div>
        <div class="modal-text">
            @Html.HiddenFor(Function(m) m.ProjectId)
            @Html.HiddenFor(Function(m) m.StageId)
            @Html.HiddenFor(Function(m) m.Doc.Docid)
            <p>U staat op het punt om het document @Model.Doc.Name te verwijderen.</p>
            <p>Bent u zeker dat u deze wilt verwijderen?</p>
        </div>
    </div>
</div>
<footer class="panel-footer">
    <div class="row">
        <div class="col-md-12 text-right">
            <a href="@Url.Action("DeleteStageDoc", "Projecten", New With {.projectid = Model.ProjectId, .stageid = Model.StageId, .docid = Model.Doc.Docid})" class="btn btn-warning btn-block">Verwijderen</a>
            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
        </div>
    </div>
</footer>
</section>
