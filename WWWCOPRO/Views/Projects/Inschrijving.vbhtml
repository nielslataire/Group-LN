@ModelType WWWCOPRO.ProjectDetailModel
@Imports wwwcopro.extensions
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim imgBase As String = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")

    Dim sales = Model.SalesData
    Dim settings = Model.SalesSetttings
    Dim numberAppartments As Integer = If(sales IsNot Nothing, sales.NumberAppartments, 0)
    Dim numberHouses As Integer = If(sales IsNot Nothing, sales.NumberHouses, 0)
    Dim numberCommercial As Integer = If(sales IsNot Nothing, sales.NumberCommercial, 0)
    Dim livingUnits As Integer = If(sales IsNot Nothing, sales.LivingUnits, 0)

    Dim typeList As New List(Of String)
    If numberAppartments = 1 Then
        typeList.Add("Appartement")
    ElseIf numberAppartments > 1 Then
        typeList.Add("Appartementen")
    End If
    If numberHouses = 1 Then
        typeList.Add("Woning")
    ElseIf numberHouses > 1 Then
        typeList.Add("Woningen")
    End If
    If numberCommercial = 1 Then
        typeList.Add("Handelspand")
    ElseIf numberCommercial > 1 Then
        typeList.Add("Handelspanden")
    End If
    Dim typeLabel As String = If(typeList.Any(), String.Join(" · ", typeList), "Project")

    Dim imgSrc As String = If(Model.Data.DefaultPicture IsNot Nothing, Url.Content(imgBase & "pictures/800/" & Model.Data.DefaultPicture.Name), Url.Content("~/Content/img/no_image.jpg"))
    Dim imgAlt As String = If(Model.Data.DefaultPicture IsNot Nothing AndAlso Not String.IsNullOrEmpty(Model.Data.DefaultPicture.Caption), Model.Data.DefaultPicture.Caption, Model.Data.Name)
    Dim titel As String = If(Not String.IsNullOrEmpty(Model.Data.CommercialTitleNL), Model.Data.CommercialTitleNL, Model.Data.Name)
    Dim tekst As String = Model.Data.CommercialTextNL

    Dim totalUnits As Integer = If(livingUnits > 0, livingUnits, numberAppartments + numberHouses + numberCommercial)

    Dim interestOptions As New List(Of String)
    If numberAppartments > 0 Then interestOptions.Add(If(numberAppartments = 1, "Appartement", "Appartementen"))
    If numberHouses > 0 Then interestOptions.Add(If(numberHouses = 1, "Woning", "Woningen"))
    If numberCommercial > 0 Then interestOptions.Add(If(numberCommercial = 1, "Handelspand", "Handelspanden"))
    If interestOptions.Count > 1 Then interestOptions.Add("Beide opties")
    If interestOptions.Count = 0 Then interestOptions.Add("Dit project")
    Dim showInterest As Boolean = interestOptions.Count > 1
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
    <link rel="stylesheet" href="~/Content/project-detail.css" />
    <link rel="stylesheet" href="~/Content/project-inschrijving.css" />
end section

<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <ul class="breadcrumb">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional}))">Woonprojecten</a></li>
                    <li class="active">@Model.Data.Name</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <h1 class="mb-none pb-none">Inschrijvingspagina</h1>
                <p class="project-locatie-sub mb-none">
                    <i class="fa fa-map-marker"></i>@Model.Data.Postalcode.Gemeente
                </p>
            </div>
        </div>
    </div>
</section>

<div class="container">

    <div class="row" style="margin-top: 30px;">

        <div class="col-md-7">
            <div class="inschrijving-foto-wrap">
                <img src="@imgSrc" alt="@imgAlt" class="inschrijving-foto img-responsive">
                <div class="inschrijving-foto-overlay"></div>
                <div class="inschrijving-foto-content">
                    <span class="inschrijving-foto-badge">
                        <span class="inschrijving-badge-dot"></span>
                        Binnenkort beschikbaar
                    </span>
                    @If Not String.IsNullOrEmpty(titel) Then
                        @<text><p class="inschrijving-foto-titel">@titel</p></text>
                    End If
                    <p class="inschrijving-foto-sub">Meer details volgen binnenkort</p>
                </div>
            </div>
        </div>

        <div class="col-md-5">
            <div class="info-kaart inschrijving-panel-kaart">

                <div class="inschrijving-panel-kop-wrap">
                    <div class="inschrijving-panel-kop">Interesse in dit project?</div>
                    <h2 class="inschrijving-panel-titel">Houd mij op de hoogte</h2>
                    <p class="inschrijving-panel-tekst">Vul uw gegevens in en wij contacteren u zodra meer informatie beschikbaar is over dit project.</p>
                </div>

                <div id="inschrijving-form-wrap">
                    @Using Html.BeginForm("Inschrijving", "Projects", New With {.slug = Model.Data.Slug}, FormMethod.Post, New With {.id = "inschrijving-form", .class = "inschrijving-form"})
                        @Html.AntiForgeryToken()
                        @<text>
                            <div class="inschrijving-rij-2">
                                <div class="inschrijving-veld">
                                    <label class="inschrijving-label">Voornaam</label>
                                    <input type="text" name="Firstname" placeholder="Jan" class="inschrijving-input" />
                                </div>
                                <div class="inschrijving-veld">
                                    <label class="inschrijving-label">Naam</label>
                                    <input type="text" name="Name" placeholder="Janssen" class="inschrijving-input" />
                                </div>
                            </div>
                            <div class="inschrijving-veld">
                                <label class="inschrijving-label">E-mailadres</label>
                                <input type="email" name="Email" placeholder="jan.janssen@email.be" class="inschrijving-input" />
                            </div>
                            <div class="inschrijving-veld">
                                <label class="inschrijving-label">Telefoonnummer</label>
                                <input type="tel" name="Phone" placeholder="+32 4xx xx xx xx" class="inschrijving-input" />
                            </div>
                            @If showInterest Then
                                @<text>
                                    <div class="inschrijving-veld">
                                        <label class="inschrijving-label">Interesse in</label>
                                        <div class="inschrijving-checkboxes">
                                            @For Each opt In interestOptions
                                                @<text>
                                                    <label class="inschrijving-checkbox-label">
                                                        <input type="checkbox" name="Interest" value="@opt" class="inschrijving-checkbox" />
                                                        @opt
                                                    </label>
                                                </text>
                                            Next
                                        </div>
                                    </div>
                                </text>
                            End If
                            <div class="inschrijving-veld">
                                <label class="inschrijving-label">Opmerkingen <span class="inschrijving-optioneel">(optioneel)</span></label>
                                <textarea name="Remarks" class="inschrijving-input inschrijving-textarea" placeholder="Eventuele vragen of specifieke wensen..."></textarea>
                            </div>
                            <button type="submit" class="inschrijving-btn">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>
                                Schrijf mij in
                            </button>
                        </text>
                    End Using
                </div>
                <div id="inschrijving-result" style="display:none;"></div>

                <div class="inschrijving-panel-links">
                    <a href="tel:+3292164950" class="info-cta-sec">
                        <div class="cta-k-inner">
                            <span class="cta-k-ico"><i class="fa fa-phone"></i></span>
                            <div>
                                <span class="cta-k-title">+32 (0)9 216 49 50</span>
                                <span class="cta-k-sub">Bel ons direct</span>
                            </div>
                        </div>
                        <span class="cta-k-arrow">&rsaquo;</span>
                    </a>
                </div>

            </div>
        </div>

    </div>

    <div class="inschrijving-specs-balk">
        <div class="inschrijving-spec">
            <div class="inschrijving-spec-label">Type</div>
            <div class="inschrijving-spec-waarde">@typeLabel</div>
        </div>
        <div class="inschrijving-spec">
            <div class="inschrijving-spec-label">Eenheden</div>
            <div class="inschrijving-spec-waarde">@If totalUnits > 0 Then @<text>@totalUnits</text> Else @<text>&mdash;</text> End If</div>
            @If totalUnits > 0 Then @<text><div class="inschrijving-spec-sub">Wooneenheden</div></text> End If
        </div>
        <div class="inschrijving-spec">
            <div class="inschrijving-spec-label">Slaapkamers</div>
            <div class="inschrijving-spec-waarde">&mdash;</div>
        </div>
        <div class="inschrijving-spec">
            <div class="inschrijving-spec-label">Bewoonbaar</div>
            <div class="inschrijving-spec-waarde">&mdash;</div>
        </div>
        <div class="inschrijving-spec">
            <div class="inschrijving-spec-label">Locatie</div>
            <div class="inschrijving-spec-waarde">@Model.Data.Postalcode.Gemeente</div>
        </div>
    </div>

    @If Not String.IsNullOrEmpty(titel) OrElse Not String.IsNullOrEmpty(tekst) Then
        @<text>
            <div class="commercial-sectie">
                @If Not String.IsNullOrEmpty(titel) Then
                    @<text><p class="commercial-titel">@titel</p></text>
                End If
                <div class="commercial-accent"></div>
                @If Not String.IsNullOrEmpty(tekst) Then
                    @<text><div class="project-tekst">@Html.Raw(tekst)</div></text>
                End If
            </div>
        </text>
    End If

</div>

@section scripts
    <script>
        $(document).ready(function () {
            $('#inschrijving-form').on('submit', function (e) {
                e.preventDefault();

                var interestChecked = [];
                $('input[name="Interest"]:checked').each(function () {
                    interestChecked.push($(this).val());
                });

                var formData = $(this).serializeArray();
                formData = formData.filter(function (f) { return f.name !== 'Interest'; });
                formData.push({ name: 'Interest', value: interestChecked.join(', ') });

                var $btn = $(this).find('.inschrijving-btn');
                $btn.prop('disabled', true).text('Bezig...');

                $.ajax({
                    url: $(this).attr('action'),
                    type: 'POST',
                    data: formData,
                    success: function () {
                        $('#inschrijving-form-wrap').hide();
                        $('#inschrijving-result').html(
                            '<div class="inschrijving-succes">' +
                            '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--color-primary)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>' +
                            '<h3>Inschrijving ontvangen</h3>' +
                            '<p>Bedankt voor uw interesse. Wij nemen zo snel mogelijk contact met u op zodra meer informatie beschikbaar is.</p>' +
                            '</div>'
                        ).show();
                    },
                    error: function () {
                        $btn.prop('disabled', false).html('<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg> Schrijf mij in');
                        $('#inschrijving-result').html(
                            '<p class="inschrijving-fout">Er is een fout opgetreden. Probeer het later opnieuw of neem telefonisch contact op.</p>'
                        ).show();
                    }
                });
            });
        });
    </script>
End section

@section LatestNews
    <h4>Recente <strong>berichten</strong></h4>
    <ul class="nav nav-list mb-xl">
        @For Each news In ViewData("LatestNews")
            @<text>
                <li><a title="@news.news.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.news.TitleNL</a></li>
            </text>
        Next
    </ul>
End Section
