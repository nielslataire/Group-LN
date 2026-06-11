@ModelType WWWCOPRO.Models.Blog.BlogArtikelModel
@Code
    ViewData("Title") = Model.Titel
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@* ── Paginakoptekst ── *@
<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>@If Not String.IsNullOrEmpty(Model.DetailTitel) Then @Model.DetailTitel Else @Model.Titel End If</h1>
                @If Not String.IsNullOrEmpty(Model.DetailTitelTekst) Then
                    @<p class="lead">@Model.DetailTitelTekst</p>
                End If
                <p class="text-muted">@Model.Datum.ToString("d-MM-yyyy")</p>
            </div>
        </div>
    </div>
</section>

@* ── Artikel inhoud ── *@
<div class="container">
    <div class="row mt-4">
        <div class="col-md-8">

            @* Omslagfoto *@
            @If Not String.IsNullOrEmpty(Model.FotoBestand) Then
                @<img src="@Model.FotoBestand" alt="@Model.Titel" class="img-fluid mb-4" />
            End If

            @* Inhoudsblokken *@
            @For Each blok In Model.Blokken
                @<section class="mb-4">
                    @If Not String.IsNullOrEmpty(blok.Titel) Then
                        @<h2>@blok.Titel</h2>
                    End If
                    @If Not String.IsNullOrEmpty(blok.FotoBestand) Then
                        @<img src="@blok.FotoBestand" alt="@blok.Titel" class="img-fluid mb-3" />
                    End If
                    @If Not String.IsNullOrEmpty(blok.RijkeTekst) Then
                        @Html.Raw(blok.RijkeTekst)
                    End If
                </section>
            Next

            <div class="mt-4">
                <a href="@Url.RouteUrl("Blog")">&larr; Terug naar overzicht</a>
            </div>
        </div>
    </div>
</div>
