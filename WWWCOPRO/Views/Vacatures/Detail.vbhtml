@ModelType WWWCOPRO.Models.Vacatures.VacatureModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

@section PageStyle
    <link rel="stylesheet" href="~/Content/vacatures.css" />
End Section

<section class="vac-detail-header">
    <div class="container">
        <ul class="breadcrumb">
            <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
            <li><a href="@Url.RouteUrl("Vacatures")">Vacatures</a></li>
            <li class="active">@Model.Titel</li>
        </ul>
        @If Not String.IsNullOrWhiteSpace(Model.Categorie) Then
            @<span class="vac-detail-categorie">@Model.Categorie</span>
        End If
        <h1>@Model.Titel</h1>
        <div class="vac-detail-meta">
            @If Not String.IsNullOrWhiteSpace(Model.Locatie) Then
                @<span class="vac-detail-meta-item">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
                    @Model.Locatie
                </span>
            End If
            @If Not String.IsNullOrWhiteSpace(Model.Dienstverband) Then
                @<span class="vac-detail-meta-item">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                    @Model.Dienstverband
                </span>
            End If
            @If Not String.IsNullOrWhiteSpace(Model.Opleiding) Then
                @<span class="vac-detail-meta-item">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 10v6M2 10l10-5 10 5-10 5-10-5z"/><path d="M6 12v5c0 1.66 2.69 3 6 3s6-1.34 6-3v-5"/></svg>
                    @Model.Opleiding
                </span>
            End If
            @If Not String.IsNullOrWhiteSpace(Model.Start) Then
                @<span class="vac-detail-meta-item">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>
                    Start: @Model.Start
                </span>
            End If
        </div>
    </div>
</section>

<div class="vac-detail-wrap">
    <div class="vac-detail-body reveal">
        @If Not String.IsNullOrWhiteSpace(Model.Beschrijving) Then
            @Html.Raw(Model.Beschrijving)
        ElseIf Not String.IsNullOrWhiteSpace(Model.KorteBeschrijving) Then
            @<p>@Model.KorteBeschrijving</p>
        End If

        @If Model.Takenpakket.Any() Then
            @<div class="vac-detail-section">
                <h2>Takenpakket</h2>
                <ul>
                    @For Each taak In Model.Takenpakket
                        @<li>@taak</li>
                    Next
                </ul>
            </div>
        End If

        @If Model.MustHaves.Any() OrElse Model.MooiMeegenomen.Any() Then
            @<div class="vac-detail-section">
                <h2>Wie zoeken we</h2>
                @If Model.MustHaves.Any() Then
                    @<text>
                        <h3>Must-haves</h3>
                        <ul>
                            @For Each punt In Model.MustHaves
                                @<li>@punt</li>
                            Next
                        </ul>
                    </text>
                End If
                @If Model.MooiMeegenomen.Any() Then
                    @<text>
                        <h3>Mooi meegenomen</h3>
                        <ul>
                            @For Each punt In Model.MooiMeegenomen
                                @<li>@punt</li>
                            Next
                        </ul>
                    </text>
                End If
            </div>
        End If

        @If Model.Voordelen.Any() Then
            @<div class="vac-detail-section">
                <h2>Wat bieden we</h2>
                <ul>
                    @For Each voordeel In Model.Voordelen
                        @<li>@voordeel</li>
                    Next
                </ul>
            </div>
        End If

        @If Model.SollicitatieStappen.Any() Then
            @<div class="vac-detail-section">
                <h2>Hoe verloopt de sollicitatie</h2>
                <div class="vac-detail-stappen">
                    @For i As Integer = 0 To Model.SollicitatieStappen.Count - 1
                        Dim stap = Model.SollicitatieStappen(i)
                        @<div class="vac-detail-stap">
                            <span class="vac-detail-stap-nummer">@(i + 1)</span>
                            <div>
                                @If Not String.IsNullOrWhiteSpace(stap.Titel) Then
                                    @<div class="vac-detail-stap-titel">@stap.Titel</div>
                                End If
                                @If Not String.IsNullOrWhiteSpace(stap.Tekst) Then
                                    @<div class="vac-detail-stap-tekst">@stap.Tekst</div>
                                End If
                            </div>
                        </div>
                    Next
                </div>
            </div>
        End If

        <div class="vac-detail-cta">
            <div class="vac-detail-cta-text">
                <h3>Interesse in deze functie?</h3>
                <p>Stuur ons je motivatie en cv, we nemen snel contact met je op.</p>
            </div>
            <a href="@("mailto:info@groupln.be?subject=" & Uri.EscapeDataString("Sollicitatie - " & Model.Titel))" class="vac-detail-apply-btn">
                Solliciteer nu <i class="fa fa-arrow-right"></i>
            </a>
        </div>

        <a href="@Url.RouteUrl("Vacatures")" class="vac-detail-back">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/></svg>
            Alle vacatures
        </a>
    </div>
</div>

@section scripts
    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });
    </script>
End Section
