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
        <h2 class="panel-title">Overzicht projectdocumenten</h2>
<br />
       <section class="panel">

    <div class="panel-body">

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
                            <td><a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("DocWebURL") & "Docs/" & doc.Filename)" target="_blank"> <span Class="title">@doc.Type.GetDisplayName()</span></a></td>
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