@imports bo
@modeltype DetailDocsModel

@section PageStyle

    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />

End Section

<div class="inner-toolbar clearfix">
    <ul>
            <li>
                <a href="#modaladddoc" class="btn modal-with-form " type="button" id="btnAddDoc"><i class="fa fa-plus"></i> Toevoegen</a>
            </li>
    </ul>
</div>


        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})
        @Html.HiddenFor(Function(m) m.ClientAccountId, New With {.id = "txtClientAccountId"})
        <h2 class="panel-title">Overzicht documenten</h2>
<br />
       <section class="panel">

    <div class="panel-body">
        @*<div class="row mg-files" data-sort-destination="" data-sort-id="media-gallery" style="position: relative;">
@For Each doc In Model.Docs
    @<text>
            <div class="isotope-item document col-sm-6 col-md-4 col-lg-3" style="position: absolute; left: 0px; top: 5px;">
                <div class="thumbnail">
                    <div class="thumb-preview">
                        <a class="thumb-image" href="img/projects/project-1.jpg">
                            <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Pictures/Pdf/" & Path.GetFileNameWithoutExtension(doc.Filename) & ".jpg")" class="img-fluid" alt="Project">
                        </a>
                        <div class="mg-thumb-options">
                            <div class="mg-zoom"><i class="fas fa-search"></i></div>
                            <div class="mg-toolbar">
                                <div class="mg-option checkbox-custom checkbox-inline">
                                    <input type="checkbox" id="file_1" value="1">
                                    <label for="file_1">SELECT</label>
                                </div>
                                <div class="mg-group float-right">
                                    <a href="#">EDIT</a>
                                    <button class="dropdown-toggle mg-toggle" data-toggle="dropdown"><span class="caret"></span></button>
                                    <div class="dropdown-menu mg-dropdown" role="menu">
                                        <a class="dropdown-item text-1" href="#"><i class="fas fa-download"></i> Download</a>
                                        <a class="dropdown-item text-1" href="#"><i class="far fa-trash-alt"></i> Delete</a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <h5 class="mg-title font-weight-semibold">SEO<small>.png</small></h5>
                    <div class="mg-description">
                        <small class="float-left text-muted">Design, Websites</small>
                        <small class="float-right text-muted">07/10/2017</small>
                    </div>
                </div>
            </div>

    </text> Next
            





        </div>*@


        <table class="table table-no-more table-bordered table-striped mb-none">
            <thead>
                <tr>
                    <td>Docnr.</td>
                    <td>Type</td>
                    <td>Naam</td>
                    <td>Documentdatum</td>
                    <td>Bestandsnaam</td>
                    <td>Acties</td>
                </tr>

            </thead>
            <tbody>
                @for Each doc In Model.Docs
                    @<text>
                        <tr>
                            <td>@doc.Docid</td>
                            <td>
                                <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Type.GetDisplayName()</span></a></td>
                            <td>@doc.Name</td>
                            @if Not doc.DocDate Is Nothing Then
                                @<text>
                                 <td>@doc.DocDate.Value.ToLongDateString </td>
                                </text>
                            Else
                                @<text>
                                    <td>-</td>
                                </text>
                            End If
                            <td><a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Filename </span></a></td>
                            <td><a href="#modaldeletedoc" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" class="deletedoc modal-with-form" data-id="@doc.Docid"><i class="fa fa-remove "></i></a></td>
                        </tr>
                    </text>
                Next
            </tbody>
        </table>



    </div>
</section>
    
        
@section scripts


End Section