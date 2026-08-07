@ModelType WWWCOPRO.ProjectModel
@Imports wwwcopro.extensions
@Imports System.Globalization
@Code
    Dim isWoonproject As Boolean = Model.Projects.Any() AndAlso Model.Projects.First().ProjectType = BO.ProjectType.Woonproject
    ViewData("Title") = If(isWoonproject, "Woonprojecten | Group LN", "Commerciële projecten | Group LN")
    ViewData("MetaDescription") = If(isWoonproject,
        "Ontdek de nieuwbouwappartementen en woningen van Group LN in Vlaanderen — kwalitatieve afwerking, tijdloos ontwerp, één aanspreekpunt.",
        "Ontdek de commerciële projecten en handelspanden van Group LN in Vlaanderen, zorgvuldig ontwikkeld met oog voor kwaliteit en locatie.")
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim belgianCulture = New CultureInfo("nl-BE")
    Dim imgBase As String = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")

    ' Uitgelicht project variabelen
    Dim featuredSetting = Model.SalesSettings.FirstOrDefault(Function(s) s.IsFeatured.GetValueOrDefault(False))
    Dim fp = If(featuredSetting IsNot Nothing, Model.Projects.FirstOrDefault(Function(p) p.Id = featuredSetting.ProjectId), If(Model.Projects.Any(), Model.Projects.First(), Nothing))
    Dim fpSales = If(fp IsNot Nothing, Model.SalesData.FirstOrDefault(Function(m) m.ProjectId = fp.Id), Nothing)
    Dim fpSettings = If(fp IsNot Nothing, Model.SalesSettings.FirstOrDefault(Function(m) m.ProjectId = fp.Id), Nothing)
    Dim fpPrice As Decimal = If(fpSales IsNot Nothing, fpSales.StartingPrice, 0D)
    Dim fpAppts As Integer = If(fpSales IsNot Nothing, fpSales.NumberAppartments, 0)
    Dim fpHouses As Integer = If(fpSales IsNot Nothing, fpSales.NumberHouses, 0)
    Dim fpComm As Integer = If(fpSales IsNot Nothing, fpSales.NumberCommercial, 0)
    Dim fpLivingUnits As Integer = If(fpSales IsNot Nothing, fpSales.LivingUnits, 0)
    Dim fpLivingUnitsSold As Integer = If(fpSales IsNot Nothing, fpSales.LivingUnitsSold, 0)
    Dim fpLivingUnitsBeschikbaar As Integer = fpLivingUnits - fpLivingUnitsSold
    Dim fpPercentageSold As Decimal = If(fpSales IsNot Nothing, fpSales.PercentageLivingUnitsSold, 0D)
    Dim fpSaleVisible As Boolean = If(fpSettings IsNot Nothing, fpSettings.SaleVisible, True)
    Dim fpExplicitStatus As Integer? = If(fpSettings IsNot Nothing AndAlso fpSettings.SalesDisplayStatus.HasValue, fpSettings.SalesDisplayStatus, Nothing)
    Dim fpIsNieuw As Boolean = fpExplicitStatus.HasValue AndAlso fpExplicitStatus.Value = 1
    Dim fpIsBinnenkort As Boolean = fpExplicitStatus.HasValue AndAlso fpExplicitStatus.Value = 2
    Dim fpIsLancering As Boolean = fpExplicitStatus.HasValue AndAlso fpExplicitStatus.Value = 3
    Dim fpIsUitverkocht As Boolean = (fpPercentageSold = 100) OrElse (fpExplicitStatus.HasValue AndAlso fpExplicitStatus.Value = 8)

    ' Type label uitgelicht — enkelvoud als count = 1
    Dim fpTypes As New List(Of String)
    If fpAppts = 1 Then
        fpTypes.Add("Appartement")
    ElseIf fpAppts > 1 Then
        fpTypes.Add("Appartementen")
    End If
    If fpHouses = 1 Then
        fpTypes.Add("Woning")
    ElseIf fpHouses > 1 Then
        fpTypes.Add("Woningen")
    End If
    If fpComm = 1 Then
        fpTypes.Add("Handelspand")
    ElseIf fpComm > 1 Then
        fpTypes.Add("Handelspanden")
    End If
    Dim fpTypeLabel As String = If(fpTypes.Any(), String.Join(" · ", fpTypes), "Project")

    Dim videoExts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {".mp4", ".webm", ".mov", ".avi"}
    Dim fpIsVideo As Boolean = fp IsNot Nothing AndAlso fp.DefaultPicture IsNot Nothing AndAlso
        (fp.DefaultPicture.MediaType = 1 OrElse videoExts.Contains(System.IO.Path.GetExtension(fp.DefaultPicture.Name)))
    Dim fpImgSrc As String = If(fp IsNot Nothing AndAlso fp.DefaultPicture IsNot Nothing AndAlso Not fpIsVideo, Url.Content(imgBase & "pictures/800/" & fp.DefaultPicture.Name), Url.Content("~/Content/img/no_image.jpg"))
    Dim fpVideoSrc As String = If(fpIsVideo, Url.Content(imgBase & "videos/" & fp.DefaultPicture.Name), "")
    Dim fpImgAlt As String = If(fp IsNot Nothing AndAlso fp.DefaultPicture IsNot Nothing AndAlso Not String.IsNullOrEmpty(fp.DefaultPicture.Caption), fp.DefaultPicture.Caption, If(fp IsNot Nothing, fp.Name, ""))
    Dim fpTitel As String = If(fp IsNot Nothing AndAlso Not String.IsNullOrEmpty(fp.CommercialTitleNL), fp.CommercialTitleNL, If(fp IsNot Nothing, fp.Name, ""))

    ' CTA vulling: bereken vrije slots in laatste rij (3 kolommen)
    Dim gridProjectCount As Integer = Math.Max(0, Model.Projects.Count() - 1)
    Dim remainingSlots As Integer = If(gridProjectCount > 0, (3 - (gridProjectCount Mod 3)) Mod 3, 0)

    ' ItemList JSON-LD: beschrijft de projecten die op deze
    ' overzichtspagina worden getoond en verwijst naar hun detailpagina's.

    Dim _ser As New System.Web.Script.Serialization.JavaScriptSerializer()

    Dim _listName As String =
        If(isWoonproject,
           "Woonprojecten",
           "Commerciële projecten")

    Dim _typeValue As String =
        If(isWoonproject,
           "Woonproject",
           "Commercieel")

    Dim _listUrl As String =
        "https://www.groupln.be/woonprojecten?Type=" &
        Uri.EscapeDataString(_typeValue)

    ViewData("canonical") = _listUrl

    Dim _itemsJson As New System.Text.StringBuilder()
    Dim _pos As Integer = 0

    For Each p In Model.Projects

        _pos += 1

        Dim pName As String =
            If(Not String.IsNullOrWhiteSpace(p.CommercialTitleNL),
               p.CommercialTitleNL,
               p.Name)

        Dim pUrl As String =
            "https://www.groupln.be/woonprojecten/" &
            Uri.EscapeDataString(p.Slug)

        If _itemsJson.Length > 0 Then
            _itemsJson.Append(","c)
            _itemsJson.Append(Environment.NewLine)
        End If

        _itemsJson.Append(
            "    {" &
            " ""@type"": ""ListItem""," &
            " ""position"": " & _pos & "," &
            " ""item"": {" &
            " ""name"": " & _ser.Serialize(pName) & "," &
            " ""url"": " & _ser.Serialize(pUrl) &
            " }" &
            " }"
        )

    Next

    Dim _itemListJson As String =
        "{" & Environment.NewLine &
        "  ""@context"": ""https://schema.org""," & Environment.NewLine &
        "  ""@type"": ""ItemList""," & Environment.NewLine &
        "  ""name"": " & _ser.Serialize(_listName) & "," & Environment.NewLine &
        "  ""url"": " & _ser.Serialize(_listUrl) & "," & Environment.NewLine &
        "  ""numberOfItems"": " & _pos & "," & Environment.NewLine &
        "  ""itemListOrder"": ""https://schema.org/ItemListOrderAscending""," &
        Environment.NewLine &
        "  ""itemListElement"": [" & Environment.NewLine &
        _itemsJson.ToString() & Environment.NewLine &
        "  ]" & Environment.NewLine &
        "}"
End Code
@section PageMeta
    @If Model.Projects.Any() Then
        @<script type="application/ld+json">@Html.Raw(_itemListJson)</script>
    End If
End Section
@section PageStyle
    <link rel="stylesheet" href="~/Content/projects-index.css" />
end section

<section class="projecten-page-header">
    <div class="container reveal">
        <ul class="breadcrumb">
            <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
            <li class="active">
                @If isWoonproject Then @<text>Woonprojecten</text> Else @<text>Commerciële projecten</text> End If
            </li>
        </ul>
        <h1>@If isWoonproject Then @<text>Woonprojecten</text> Else @<text>Commerciële projecten</text> End If</h1>
        <p class="page-subtitle">
            @If isWoonproject Then
                @<text>Ontdek ons aanbod van kwalitatieve nieuwbouwappartementen en woningen.</text>
            Else
                @<text>Ontdek ons aanbod van commerciële projecten en handelspanden.</text>
            End If
        </p>
        @*<span class="page-accent"></span>*@
    </div>
</section>

<div class="container" style="padding-top: 12px; padding-bottom: 64px;">

    @If Not Model.Projects.Any() Then
        @<text>
            <div class="projecten-leeg">
                <p>Momenteel zijn er geen projecten beschikbaar.</p>
            </div>
        </text>
    End If

    @If fp IsNot Nothing Then
        @<text>
            <div class="sectie-kop">Uitgelicht project</div>
            <a href="@(Url.RouteUrl("ProjectBySlug", New With {.slug = fp.Slug}))" class="uitgelicht-project reveal">
                <div class="uitgelicht-foto">
                    @If fpIsVideo Then
                        @<video src="@fpVideoSrc" muted loop playsinline data-autoplay="true"></video>
                    Else
                        @<img src="@fpImgSrc" alt="@fpImgAlt">
                    End If
                    <div class="uitgelicht-foto-overlay"></div>
                    @If fpIsUitverkocht Then
                        @<text><span class="uitgelicht-foto-badge uitgelicht-foto-badge-uitverkocht">Uitverkocht</span></text>
                    ElseIf fpIsNieuw Then
                        @<text><span class="uitgelicht-foto-badge uitgelicht-foto-badge-nieuw">Nieuw</span></text>
                    ElseIf fpIsBinnenkort Then
                        @<text><span class="uitgelicht-foto-badge uitgelicht-foto-badge-binnenkort">Binnenkort</span></text>
                    ElseIf fpIsLancering Then
                        @<text><span class="uitgelicht-foto-badge uitgelicht-foto-badge-lancering">Lancering</span></text>
                    End If
                    @If fpPercentageSold >= 50 AndAlso Not fpIsUitverkocht AndAlso Not fpIsBinnenkort Then
                        Dim fpPercentageSoldRounded As Integer = Math.Min(100, CInt(Math.Round(fpPercentageSold)))
                        @<text>
                            <div class="uitgelicht-verkocht-badge">
                                <div class="verkocht-badge-label">Verkocht</div>
                                <div class="verkocht-badge-value">@fpPercentageSoldRounded%</div>
                                <div class="verkocht-badge-bar"><div class="verkocht-badge-bar-fill" style="width:@(fpPercentageSoldRounded)%"></div></div>
                            </div>
                        </text>
                    End If
                </div>
                <div class="uitgelicht-info">
                    <div class="uitgelicht-type">@fpTypeLabel</div>
                    <h2 class="uitgelicht-naam">@fpTitel</h2>
                    <div class="uitgelicht-locatie">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>
                        @If Not String.IsNullOrEmpty(fp.Street) Then @<text>@fp.Street @fp.HouseNumber, </text> End If
                        @fp.Postalcode.Gemeente.ToUpper()
                    </div>
                    @If Not String.IsNullOrEmpty(fp.CommercialTextNL) Then
                        @<text><div class="uitgelicht-tekst">@Html.Raw(fp.CommercialTextNL)</div></text>
                    End If
                    @If fpAppts > 0 OrElse fpHouses > 0 OrElse fpComm > 0 Then
                        @<text>
                            <div class="uitgelicht-specs">
                                @If fpAppts = 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                            Appartement
                                        </span>
                                    </text>
                                ElseIf fpAppts > 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                            @fpAppts appartementen
                                        </span>
                                    </text>
                                End If
                                @If fpHouses = 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                            Woning
                                        </span>
                                    </text>
                                ElseIf fpHouses > 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                            @fpHouses woningen
                                        </span>
                                    </text>
                                End If
                                @If fpComm = 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                            Handelspand
                                        </span>
                                    </text>
                                ElseIf fpComm > 1 Then
                                    @<text>
                                        <span class="uitgelicht-spec-item">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                            @fpComm handelspanden
                                        </span>
                                    </text>
                                End If
                            </div>
                        </text>
                    End If
                    @If fpLivingUnitsBeschikbaar > 0 AndAlso Not fpIsUitverkocht AndAlso Not fpIsBinnenkort Then
                        @<text>
                            <span class="beschikbaar-pill">
                                <span class="beschikbaar-dot"></span>
                                @fpLivingUnitsBeschikbaar @(If(fpLivingUnitsBeschikbaar = 1, "wooneenheid", "wooneenheden")) beschikbaar
                            </span>
                        </text>
                    End If
                    @If fpPrice > 0 Then
                        @<text>
                            <div class="uitgelicht-prijs">
                                <span>Vanaf</span>
                                @WWWCOPRO.Extensions.ToEuroCurrency(fpPrice)
                            </div>
                        </text>
                    End If
                    <span class="uitgelicht-cta">
                        Bekijk project
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                    </span>
                </div>
            </a>
        </text>
    End If

    @If Model.Projects.Count() > 1 Then
        @<text><div class="sectie-kop" style="margin-top: 8px;">Alle projecten</div></text>
    End If

    <div class="projecten-grid">
    @For Each project In Model.Projects.Where(Function(p) fp Is Nothing OrElse p.Id <> fp.Id)
        Dim sales = Model.SalesData.FirstOrDefault(Function(m) m.ProjectId = project.Id)
        Dim settings = Model.SalesSettings.FirstOrDefault(Function(m) m.ProjectId = project.Id)
        Dim livingUnits As Integer = If(sales IsNot Nothing, sales.LivingUnits, 0)
        Dim livingUnitsSold As Integer = If(sales IsNot Nothing, sales.LivingUnitsSold, 0)
        Dim livingUnitsBeschikbaar As Integer = livingUnits - livingUnitsSold
        Dim percentageSold As Decimal = If(sales IsNot Nothing, sales.PercentageLivingUnitsSold, 0D)
        Dim startingPrice As Decimal = If(sales IsNot Nothing, sales.StartingPrice, 0D)
        Dim numberAppartments As Integer = If(sales IsNot Nothing, sales.NumberAppartments, 0)
        Dim numberCommercial As Integer = If(sales IsNot Nothing, sales.NumberCommercial, 0)
        Dim numberHouses As Integer = If(sales IsNot Nothing, sales.NumberHouses, 0)
        Dim saleVisible As Boolean = If(settings IsNot Nothing, settings.SaleVisible, True)

        Dim explicitStatus As Integer? = If(settings IsNot Nothing AndAlso settings.SalesDisplayStatus.HasValue, settings.SalesDisplayStatus, Nothing)
        Dim isNieuw As Boolean = explicitStatus.HasValue AndAlso explicitStatus.Value = 1
        Dim isBinnenkort As Boolean = explicitStatus.HasValue AndAlso explicitStatus.Value = 2
        Dim isLancering As Boolean = explicitStatus.HasValue AndAlso explicitStatus.Value = 3
        Dim isUitverkocht As Boolean = (percentageSold = 100) OrElse (explicitStatus.HasValue AndAlso explicitStatus.Value = 8)

        ' Type label — enkelvoud als count = 1
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
        Dim typeLabel As String = If(typeList.Any(), String.Join(" · ", typeList), "Woonproject")

        Dim cardIsVideo As Boolean = project.DefaultPicture IsNot Nothing AndAlso
            (project.DefaultPicture.MediaType = 1 OrElse videoExts.Contains(System.IO.Path.GetExtension(project.DefaultPicture.Name)))
        Dim cardImgSrc As String = If(project.DefaultPicture IsNot Nothing AndAlso Not cardIsVideo, Url.Content(imgBase & "pictures/447/" & project.DefaultPicture.Name), Url.Content("~/Content/img/no_image.jpg"))
        Dim cardVideoSrc As String = If(cardIsVideo, Url.Content(imgBase & "videos/" & project.DefaultPicture.Name), "")
        Dim cardImgAlt As String = If(project.DefaultPicture IsNot Nothing AndAlso Not String.IsNullOrEmpty(project.DefaultPicture.Caption), project.DefaultPicture.Caption, project.Name)
        Dim cardTitel As String = If(Not String.IsNullOrEmpty(project.CommercialTitleNL), project.CommercialTitleNL, project.Name)

        @If isBinnenkort Then
            @<text>
                <a href="@(Url.Action("Inschrijving", "Projects", New With {.slug = project.Slug}))" class="project-kaart reveal">
                    <div class="kaart-foto">
                        @If cardIsVideo Then
                            @<video src="@cardVideoSrc" muted loop playsinline data-autoplay="true"></video>
                        Else
                            @<img src="@cardImgSrc" alt="@cardImgAlt">
                        End If
                        <div class="kaart-foto-overlay"></div>
                        <div class="kaart-verkocht-overlay">
                            <span class="kaart-verkocht-label">Binnenkort</span>
                        </div>
                        <div class="kaart-pijl">
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                        </div>
                    </div>
                    <div class="kaart-body">
                        <div class="kaart-type">@typeLabel</div>
                        <div class="kaart-naam">@cardTitel</div>
                        <div class="kaart-specs">
                            @If numberAppartments = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                        Appartement
                                    </span>
                                </text>
                            ElseIf numberAppartments > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                        @numberAppartments appartementen
                                    </span>
                                </text>
                            End If
                            @If numberHouses = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                        Woning
                                    </span>
                                </text>
                            ElseIf numberHouses > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                        @numberHouses woningen
                                    </span>
                                </text>
                            End If
                            @If numberCommercial = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                        Handelspand
                                    </span>
                                </text>
                            ElseIf numberCommercial > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                        @numberCommercial handelspanden
                                    </span>
                                </text>
                            End If
                        </div>
                    </div>
                    <div class="kaart-footer">
                        <span style="color:var(--tekst-sub)">Schrijf mij in</span>
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color:var(--tekst-sub);opacity:0.35"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                    </div>
                </a>
            </text>
        Else
            @<text>
                <a href="@(Url.RouteUrl("ProjectBySlug", New With {.slug = project.Slug}))" class="project-kaart reveal">
                    <div class="kaart-foto">
                        @If cardIsVideo Then
                            @<video src="@cardVideoSrc" muted loop playsinline data-autoplay="true"></video>
                        Else
                            @<img src="@cardImgSrc" alt="@cardImgAlt">
                        End If
                        <div class="kaart-foto-overlay"></div>
                        @If isUitverkocht Then
                            @<text>
                                <div class="kaart-verkocht-overlay">
                                    <span class="kaart-verkocht-label">Uitverkocht</span>
                                </div>
                            </text>
                        End If
                        @If isNieuw Then
                            @<text><span class="kaart-foto-badge kaart-foto-badge-nieuw">Nieuw</span></text>
                        ElseIf isLancering Then
                            @<text><span class="kaart-foto-badge kaart-foto-badge-lancering">Lancering</span></text>
                        End If
                        @If percentageSold >= 50 AndAlso Not isUitverkocht Then
                            Dim percentageSoldRounded As Integer = Math.Min(100, CInt(Math.Round(percentageSold)))
                            @<text>
                                <div class="kaart-verkocht-badge">
                                    <div class="verkocht-badge-label">Verkocht</div>
                                    <div class="verkocht-badge-value">@percentageSoldRounded%</div>
                                    <div class="verkocht-badge-bar"><div class="verkocht-badge-bar-fill" style="width:@(percentageSoldRounded)%"></div></div>
                                </div>
                            </text>
                        End If
                        @If startingPrice > 0 AndAlso Not isUitverkocht Then
                            @<text>
                                <div class="kaart-prijs">
                                    <span class="kaart-prijs-label">Vanaf</span>
                                    @WWWCOPRO.Extensions.ToEuroCurrency(startingPrice)
                                </div>
                            </text>
                        End If
                        <div class="kaart-pijl">
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                        </div>
                    </div>
                    <div class="kaart-body">
                        <div class="kaart-type">@typeLabel</div>
                        <div class="kaart-naam">@cardTitel</div>
                        <div class="kaart-locatie">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>
                            @If Not String.IsNullOrEmpty(project.Street) Then @<text>@project.Street @project.HouseNumber, </text> End If
                            @project.Postalcode.Gemeente.ToUpper()
                        </div>
                        <div class="kaart-specs">
                            @If numberAppartments = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                        Appartement
                                    </span>
                                </text>
                            ElseIf numberAppartments > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                                        @numberAppartments appartementen
                                    </span>
                                </text>
                            End If
                            @If numberHouses = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                        Woning
                                    </span>
                                </text>
                            ElseIf numberHouses > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                                        @numberHouses woningen
                                    </span>
                                </text>
                            End If
                            @If numberCommercial = 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                        Handelspand
                                    </span>
                                </text>
                            ElseIf numberCommercial > 1 Then
                                @<text>
                                    <span class="kaart-spec">
                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                                        @numberCommercial handelspanden
                                    </span>
                                </text>
                            End If
                        </div>
                        @If livingUnitsBeschikbaar > 0 AndAlso Not isUitverkocht Then
                            @<text>
                                <span class="beschikbaar-pill">
                                    <span class="beschikbaar-dot"></span>
                                    @livingUnitsBeschikbaar @(If(livingUnitsBeschikbaar = 1, "wooneenheid", "wooneenheden")) beschikbaar
                                </span>
                            </text>
                        End If
                    </div>
                    <div class="kaart-footer">
                        @If isUitverkocht Then
                            @<text>
                                <span class="status-badge status-badge-uitverkocht">
                                    <span class="status-dot status-dot-uitverkocht"></span>
                                    Uitverkocht
                                </span>
                            </text>
                        ElseIf isNieuw Then
                            @<text>
                                <span class="status-badge status-badge-nieuw">
                                    <span class="status-dot status-dot-nieuw"></span>
                                    Nieuw
                                </span>
                            </text>
                        ElseIf isLancering Then
                            @<text>
                                <span class="status-badge status-badge-lancering">
                                    <span class="status-dot status-dot-lancering"></span>
                                    Lancering
                                </span>
                            </text>
                        Else
                            @<text>
                                <span style="color:var(--tekst-sub)">
                                    Meer Info
                                </span>
                            </text>
                        End If
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color:var(--tekst-sub);opacity:0.35"><line x1="5" y1="12" x2="19" y2="12" /><polyline points="12 5 19 12 12 19" /></svg>
                    </div>
                </a>
            </text>
        End If

    Next

    @* Vul resterende lege slots op met CTA-kaarten *@
    @If remainingSlots = 2 Then
        @<text>
            <div class="cta-kaart cta-kaart-grond reveal">
                <div class="cta-kaart-label">Grondpositie</div>
                <div class="cta-kaart-titel">Projectgrond<br />te koop?</div>
                <p class="cta-kaart-tekst">Heeft u een perceel of pand te koop? Wij bekijken graag de mogelijkheden samen met u.</p>
                <a href="@Url.Action("Index", "Contact")" class="cta-kaart-btn">
                    Neem contact op
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                </a>
            </div>
            <div class="cta-kaart cta-kaart-contact reveal">
                <div class="cta-kaart-label">Meer weten?</div>
                <div class="cta-kaart-titel">Informatie<br />aanvragen</div>
                <p class="cta-kaart-tekst">Interesse in een van onze projecten? Wij beantwoorden al uw vragen.</p>
                <a href="@Url.Action("Index", "Contact")" class="cta-kaart-btn">
                    Stuur een bericht
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                </a>
            </div>
        </text>
    ElseIf remainingSlots = 1 Then
        @<text>
            <div class="cta-kaart cta-kaart-grond reveal">
                <div class="cta-kaart-label">Grondpositie</div>
                <div class="cta-kaart-titel">Projectgrond<br />te koop?</div>
                <p class="cta-kaart-tekst">Heeft u een perceel of pand te koop? Wij bekijken graag de mogelijkheden samen met u.</p>
                <a href="@Url.Action("Index", "Contact")" class="cta-kaart-btn">
                    Neem contact op
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>
                </a>
            </div>
        </text>
    End If
    </div>

</div>

@section scripts
    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });

        (function () {
            var videos = document.querySelectorAll('video[data-autoplay]');
            if (!videos.length || !window.IntersectionObserver) return;
            var obs = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.play().catch(function () {});
                    } else {
                        entry.target.pause();
                    }
                });
            }, { threshold: 0.4 });
            videos.forEach(function (v) { obs.observe(v); });
        })();
    </script>
End section

