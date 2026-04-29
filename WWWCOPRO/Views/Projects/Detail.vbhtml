@ModelType WWWCOPRO.ProjectDetailModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@Imports wwwcopro.extensions
@Imports System.Text.RegularExpressions
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
    <link rel="stylesheet" href="~/vendor/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/contact-modal.css" />
    <link rel="stylesheet" href="~/Content/project-detail.css" />
    <style>
        .modal-block {
            max-width: 560px !important;
            margin: 40px auto !important;
        }

        .modal-block-lg {
            max-width: 900px !important;
        }

    </style>
End Section


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
                <h1 class="mb-none pb-none">@Model.Data.Name</h1>
                <p class="project-locatie-sub mb-none"><i class="fa fa-map-marker"></i>@Model.Data.Street @Model.Data.HouseNumber, @Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente</p>
            </div>
        </div>
    </div>
</section>


<div class="container">
    <div class="row">
        <div class="col-md-7">

            <div class="detail-foto-wrap">
            <div class="detail-project-badge">@Model.Data.Name</div>

            @Code
                Dim imgBase = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")
                Dim nevenfotos = Model.Data.Pictures.Where(Function(p) p.Type = BO.PictureType.Nevenfoto).ToList()
                Dim heeftMeerdereFotos = (Model.Data.DefaultPicture IsNot Nothing) AndAlso nevenfotos.Count > 0
            End Code
            <div class="foto-gallerij" id="fotoGallerij">
                @If Model.Data.DefaultPicture IsNot Nothing Then
                    @<a href="@Url.Content(imgBase & "pictures/800/" & Model.Data.DefaultPicture.Name)" class="fg-main-link" id="fgMainLink">
                        <img id="fgMainImg" src="@Url.Content(imgBase & "pictures/800/" & Model.Data.DefaultPicture.Name)" class="img-responsive" alt="projectfoto">
                        <span class="fg-zoom-ico"><i class="icon-magnifier icons font-size-xl"></i></span>
                    </a>
                End If
                @If heeftMeerdereFotos Then
                    @<text>
                        <button class="fg-arrow fg-prev" id="fgPrev"><i class="fa fa-angle-left"></i></button>
                        <button class="fg-arrow fg-next" id="fgNext"><i class="fa fa-angle-right"></i></button>
                    </text>
                End If
                <!-- Foto data voor JS -->
                <div id="fgData" style="display:none">
                    @If Model.Data.DefaultPicture IsNot Nothing Then
                        @<a data-full="@Url.Content(imgBase & "pictures/800/" & Model.Data.DefaultPicture.Name)"
                            data-medium="@Url.Content(imgBase & "pictures/447/" & Model.Data.DefaultPicture.Name)"></a>
                    End If
                    @For Each nf In nevenfotos
                        @<a data-full="@Url.Content(imgBase & "pictures/800/" & nf.Name)"
                            data-medium="@Url.Content(imgBase & "pictures/447/" & nf.Name)"></a>
                    Next
                </div>
            </div>
            <div class="detail-foto-accent"></div>
            </div><!-- /detail-foto-wrap -->

        </div>
        <div class="col-md-5">
            <div class="info-kaart">

                <!-- Prijs / status balk -->
                <div class="info-kaart-prijs">
                    @If Model.SalesData.StartingPrice > 0 Then
                        @<text>
                            <p class="info-prijs-label">Prijzen vanaf</p>
                            <p class="info-prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(Model.SalesData.StartingPrice)</p>
                        </text>
                    ElseIf Model.SalesData.PercentageLivingUnitsSold < 15 Then
                        @<p class="info-prijs-status">Lancering</p>
                    ElseIf Model.SalesData.PercentageLivingUnitsSold = 100 AndAlso Model.SalesData.LivingUnits > 0 Then
                        @<p class="info-prijs-status">Uitverkocht</p>
                    Else
                        @<p class="info-prijs-status">Binnenkort</p>
                    End If
                </div>

                <!-- Info rijen -->
                <div class="info-kaart-rijen">
                    <!-- Adres -->
                    <div class="info-kaart-rij">
                        <span class="ik-sleutel">Adres</span>
                        <div class="ik-waarde">
                            @Model.Data.Street @Model.Data.HouseNumber<br />
                            @Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente
                            <a href="#map" data-hash data-hash-offset="100">Bekijk locatie op kaart &rarr;</a>
                        </div>
                    </div>

                    <!-- Wooneenheden -->
                    @If Model.Units.Where(Function(m) m.Type.Id = 1).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-building"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.Id = 1).Count() @(If(Model.Units.Where(Function(m) m.Type.Id = 1).Count() > 1, "appartementen", "appartement"))</span>
                        </div>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-home"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.Id = 2).Count() @(If(Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 1, "woningen", "woning"))</span>
                        </div>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-shopping-cart"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() @(If(Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 1, "handelspanden", "handelspand"))</span>
                        </div>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-archive"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() @(If(Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 1, "bergingen", "berging"))</span>
                        </div>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-road"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() @(If(Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 1, "parkeerplaatsen", "parkeerplaats"))</span>
                        </div>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 0 Then
                        @<div class="ik-eenheid-rij">
                            <i class="fa fa-car"></i>
                            <span>@Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() @(If(Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 1, "garages", "garage"))</span>
                        </div>
                    End If

                    <!-- Beschikbaar -->
                    <div class="info-kaart-rij">
                        <span class="ik-sleutel">Beschikbaar</span>
                        <div class="ik-waarde">
                            <span class="beschikbaar-pill">
                                <span class="beschikbaar-dot"></span>
                                @(Model.SalesData.LivingUnits - Model.SalesData.LivingUnitsSold) wooneenheden
                            </span>
                        </div>
                    </div>

                    <!-- Architect -->
                    @If Model.Data.Architect.ID > 0 Then
                        @<div class="info-kaart-rij">
                            <span class="ik-sleutel">Architect</span>
                            <div class="ik-waarde">@Model.Data.Architect.Display</div>
                        </div>
                    End If
                </div>

                <!-- Documenten & brochure -->
                @If Not Model.BrochureDoc Is Nothing AndAlso Model.BrochureDoc.IsBrochure Then
                    @<div class="info-kaart-docs">
                        <a href="#modalsendbrochure" data-id="@Model.BrochureDoc.Docid"
                           class="info-brochure-btn modal-with-form btnsendbrochure">
                            <i class="fa fa-download"></i> Download Brochure
                        </a>
                    </div>
                End If
                @If Model.Docs.Count > 0 AndAlso Model.SalesSetttings.SaleVisible = True Then
                    @<div class="info-kaart-docs">
                        <p class="info-docs-kop">Documenten</p>
                        <ul class="info-docs-lijst">
                            @For Each doc In Model.Docs
                                @<li>
                                    <a href="#modalsenddoc" class="modal-with-form btnsenddoc"
                                       data-id="@doc.Docid">
                                        <i class="fa fa-download"></i> @doc.Name
                                    </a>
                                </li>
                            Next
                        </ul>
                    </div>
                End If

                <!-- CTA sectie -->
                <div class="info-kaart-cta">
                    <p class="info-cta-kop">Meer weten over dit project?</p>

                    <!-- Primair: informatie aanvragen -->
                    <a href="#modalsendmail" data-id="@Model.Data.Id"
                       class="info-cta-prim btnsendmail">
                        <div class="cta-k-inner">
                            <span class="cta-k-ico"><i class="fa fa-envelope-o"></i></span>
                            <div>
                                <span class="cta-k-title"" style="padding-top:10px;">Informatie aanvragen</span>
                                <span class="cta-k-sub">Vrijblijvend &mdash; wij antwoorden snel</span>
                            </div>
                        </div>
                        <span class="cta-k-arrow">&rsaquo;</span>
                    </a>

                    <!-- Secundair: telefoon -->
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

            </div><!-- /info-kaart -->
        </div>
    </div>
    <div class="commercial-sectie">
        <p class="commercial-titel">@Model.Data.CommercialTitleNL</p>
        <div class="commercial-accent"></div>
        <div class="project-tekst">@Html.Raw(Model.Data.CommercialTextNL)</div>
    </div>

    @If Model.SalesSetttings.SaleVisible = True Then
        @<text>
            <div Class="row">
                        <div Class="col-md-12">
                            <!-- APPARTEMENTEN -->
                            @If Model.Units.Where(Function(m) m.Type.Id = 1).Count > 0 Then
                                @Code
                                    Dim aptUnits = Model.Units.Where(Function(m) m.Type.Id = 1).ToList()
                                    Dim aptAvail = aptUnits.Where(Function(m) m.ClientAccountId = 0 AndAlso Not m.IsOption).ToList()
                                    Dim aptAllPrices = aptAvail.SelectMany(Function(u) If(u.FinishingOptions.Count > 0, u.FinishingOptions.Select(Function(fo) u.LandValue + fo.TotalValue), New List(Of Decimal) From {u.TotalValue})).Where(Function(p) p > 0).ToList()
                                    Dim aptMinPrice = If(aptAllPrices.Any(), aptAllPrices.Min(), 0)
                                    Dim aptMaxPrice = If(aptAllPrices.Any(), aptAllPrices.Max(), 0)
                                    Dim aptMinSurf = If(aptAvail.Any(), aptAvail.Min(Function(m) m.Surface), 0)
                                    Dim aptMaxSurf = If(aptAvail.Any(), aptAvail.Max(Function(m) m.Surface), 0)
                                    Dim aptSlpkList = aptAvail.Select(Function(m) If(m.Rooms.Where(Function(r) r.Type = BO.RoomType.Slaapkamer).FirstOrDefault() IsNot Nothing, m.Rooms.Where(Function(r) r.Type = BO.RoomType.Slaapkamer).FirstOrDefault().Number, 0)).ToList()
                                    Dim aptMinSlpk = If(aptSlpkList.Any(), aptSlpkList.Min(), 0)
                                    Dim aptMaxSlpk = If(aptSlpkList.Any(), aptSlpkList.Max(), 0)
                                End Code
                                @<div class="eenheid-sectie">
                                    <h2 class="sectie-titel">Appartementen</h2>
                                    <div class="goud-streep"></div>
                                    <div class="tabel-wrap">
                                        <table class="eenheid-tabel">
                                            <thead>
                                                <tr>
                                                    <th>Lot</th>
                                                    <th class="num hidden-xs">Verdiep</th>
                                                    <th class="num hidden-xs">Opp (m²)</th>
                                                    <th class="num hidden-xs">Terras (m²)</th>
                                                    <th class="num hidden-xs">Tuin (m²)</th>
                                                    <th class="hidden-xs">Slaapkamers</th>
                                                    <th>Status</th>
                                                    <th class="num">Prijs</th>
                                                    <th class="center">Plan</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each unit In aptUnits
                                                    Dim isSold = (unit.ClientAccountId <> 0)
                                                    Dim isInOption = (Not isSold AndAlso unit.IsOption)
                                                    Dim isAvailable = (Not isSold AndAlso Not isInOption)
                                                    Dim terras = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Terras Or m.Type = BO.RoomType.Dakterras).Sum(Function(i) i.Surface)
                                                    Dim tuin = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Tuin).Sum(Function(i) i.Surface)
                                                    Dim slpkRoom = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault()
                                                    Dim slpk = If(slpkRoom IsNot Nothing, slpkRoom.Number, 0)
                                                    Dim heeftOpties = (unit.FinishingOptions.Count > 0 AndAlso Not isSold)
                                                    @<tr class="@(If(isSold, "rij-verkocht", "")) @(If(heeftOpties, "heeft-opties", ""))" data-unit-id="@unit.Id">
                                                        <td>
                                                            <div class="lot-badge-wrap">
                                                                <div class="lot-badge">
                                                                    <div class="lot-nr">@unit.Name</div>
                                                                    @unit.Name
                                                                </div>
                                                                @If heeftOpties Then
                                                                    @<span class="opties-badge">@unit.FinishingOptions.Count opties</span>
                                                                End If
                                                            </div>
                                                        </td>
                                                        <td class="num hidden-xs">@unit.Level</td>
                                                        <td class="num hidden-xs">@(If(unit.Surface > 0, String.Format("{0:n0}", unit.Surface) & " m²", "-"))</td>
                                                        <td class="num hidden-xs">@(If(terras > 0, String.Format("{0:n0}", terras) & " m²", "-"))</td>
                                                        <td class="num hidden-xs">@(If(tuin > 0, String.Format("{0:n0}", tuin) & " m²", "-"))</td>
                                                        <td class="hidden-xs">
                                                            @If slpk > 0 Then
                                                                @<div class="slaap-wrap">
                                                                    @For i = 1 To Math.Min(slpk, 4)
                                                                        @<span class="slaap-dot"></span>
                                                                    Next
                                                                    @For i = slpk + 1 To 4
                                                                        @<span class="slaap-dot leeg"></span>
                                                                    Next
                                                                    <span class="slaap-label">@slpk</span>
                                                                </div>
                                                            End If
                                                        </td>
                                                        <td>
                                                            @If isInOption Then
                                                                @<span class="eenheid-status-pill status-in-optie"><span class="status-dot"></span>In optie</span>
                                                            ElseIf isSold Then
                                                                @<span class="eenheid-status-pill status-verkocht"><span class="status-dot"></span>Verkocht</span>
                                                            Else
                                                                @<span class="eenheid-status-pill status-beschikbaar"><span class="status-dot"></span>Beschikbaar</span>
                                                            End If
                                                        </td>
                                                        <td class="num">
                                                            @If Not isSold Then
                                                                @If heeftOpties Then
                                                                    Dim aptMinOptPrice = unit.LandValue + unit.FinishingOptions.Min(Function(fo) fo.TotalValue)
                                                                    @<text>
                                                                        <span class="prijs-vanaf-label">Vanaf</span>
                                                                        <span class="prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(aptMinOptPrice)</span>
                                                                    </text>
                                                                Else
                                                                    @<span class="prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue)</span>
                                                                End If
                                                            Else
                                                                @<span class="prijs-verkocht">—</span>
                                                            End If
                                                        </td>
                                                        <td class="center opties-plan-cel">
                                                            @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                                @<a href="#modalsendplan" class="eenheid-dl-btn modal-with-form btnsendplan" title="Plan downloaden" data-id="@unit.Id">
                                                                    <svg viewBox="0 0 24 24" width="15" height="15" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                                                                </a>
                                                            End If
                                                            @If heeftOpties Then
                                                                @<button type="button" class="opties-toggle-btn" data-unit-id="@unit.Id" aria-expanded="false" title="Afwerkingsopties bekijken">
                                                                    <svg class="toggle-ico toggle-ico-open" viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                                                                    <svg class="toggle-ico toggle-ico-close" viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none"><polyline points="18 15 12 9 6 15"/></svg>
                                                                </button>
                                                            End If
                                                        </td>
                                                    </tr>
                                                    @If heeftOpties Then
                                                        @<tr class="opties-expand-rij" id="opties-apt-@unit.Id" style="display:none;">
                                                            <td colspan="9" class="opties-expand-cel">
                                                                <div class="opties-expand-inner">
                                                                    @For Each opt In unit.FinishingOptions.OrderBy(Function(o) o.SortOrder)
                                                                        Dim optPrijs = unit.LandValue + opt.TotalValue
                                                                        @<div class="optie-kaart @(If(opt.IsDefault, "optie-kaart-default", ""))">
                                                                            <div class="optie-kaart-naam">@opt.Name.ToUpper()</div>
                                                                            <div class="optie-kaart-prijs">@WWWCOPRO.Extensions.ToEuroCurrency(optPrijs)</div>
                                                                            @If isInOption Then
                                                                                @<span class="eenheid-status-pill status-in-optie"><span class="status-dot"></span>In optie</span>
                                                                            Else
                                                                                @<span class="eenheid-status-pill status-beschikbaar"><span class="status-dot"></span>Beschikbaar</span>
                                                                            End If
                                                                        </div>
                                                                    Next
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    End If
                                                Next
                                            </tbody>
                                            @*<tfoot>
                                                <tr>
                                                    <td colspan="7">@aptUnits.Count() @(If(aptUnits.Count() > 1, "appartementen", "appartement"))</td>
                                                    <td class="num total-prijs">@If aptAvail.Any() Then @<text>@If aptMinPrice = aptMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(aptMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(aptMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(aptMaxPrice)</text> End If</text> End If</td>
                                                    <td></td>
                                                </tr>
                                            </tfoot>*@
                                        </table>
                                        <div class="samenvatting">
                                            <div class="samenv-item">
                                                <div class="samenv-label">Appartementen</div>
                                                <div class="samenv-val">@aptUnits.Count()</div>
                                                <div class="samenv-sub">@aptAvail.Count() beschikbaar</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Oppervlakte</div>
                                                <div class="samenv-val">@(If(aptMinSurf = aptMaxSurf, String.Format("{0:n0}", aptMinSurf), String.Format("{0:n0}", aptMinSurf) & " – " & String.Format("{0:n0}", aptMaxSurf))) m²</div>
                                                <div class="samenv-sub">Per appartement</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Slaapkamers</div>
                                                <div class="samenv-val">@(If(aptMinSlpk = aptMaxSlpk, aptMinSlpk.ToString(), aptMinSlpk & " – " & aptMaxSlpk))</div>
                                                <div class="samenv-sub">Per appartement</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Prijsrange</div>
                                                <div class="samenv-val samenv-prijs">@If aptAvail.Any() Then @<text>@If aptMinPrice = aptMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(aptMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(aptMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(aptMaxPrice)</text> End If</text> End If</div>
                                                <div class="samenv-sub">Per eenheid</div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            End If
                            <!-- WONINGEN -->
                            @If Model.Units.Where(Function(m) m.Type.Id = 2).Count > 0 Then
                                @Code
                                    Dim wonUnits = Model.Units.Where(Function(m) m.Type.Id = 2).ToList()
                                    Dim wonAvail = wonUnits.Where(Function(m) m.ClientAccountId = 0 AndAlso Not m.IsOption).ToList()
                                    Dim wonAllPrices = wonAvail.SelectMany(Function(u) If(u.FinishingOptions.Count > 0, u.FinishingOptions.Select(Function(fo) u.LandValue + fo.TotalValue), New List(Of Decimal) From {u.TotalValue})).Where(Function(p) p > 0).ToList()
                                    Dim wonMinPrice = If(wonAllPrices.Any(), wonAllPrices.Min(), 0)
                                    Dim wonMaxPrice = If(wonAllPrices.Any(), wonAllPrices.Max(), 0)
                                    Dim wonMinSurf = If(wonAvail.Any(), wonAvail.Min(Function(m) m.Surface), 0)
                                    Dim wonMaxSurf = If(wonAvail.Any(), wonAvail.Max(Function(m) m.Surface), 0)
                                    Dim wonMinGround = If(wonAvail.Any(), wonAvail.Min(Function(m) m.GroundSurface), 0)
                                    Dim wonMaxGround = If(wonAvail.Any(), wonAvail.Max(Function(m) m.GroundSurface), 0)
                                    Dim wonSlpkList = wonAvail.Select(Function(m) If(m.Rooms.Where(Function(r) r.Type = BO.RoomType.Slaapkamer).FirstOrDefault() IsNot Nothing, m.Rooms.Where(Function(r) r.Type = BO.RoomType.Slaapkamer).FirstOrDefault().Number, 0)).ToList()
                                    Dim wonMinSlpk = If(wonSlpkList.Any(), wonSlpkList.Min(), 0)
                                    Dim wonMaxSlpk = If(wonSlpkList.Any(), wonSlpkList.Max(), 0)
                                End Code
                                @<div class="eenheid-sectie">
                                    <h2 class="sectie-titel">Woningen</h2>
                                    <div class="goud-streep"></div>
                                    <div class="tabel-wrap">
                                        <table class="eenheid-tabel">
                                            <thead>
                                                <tr>
                                                    <th>Lot</th>
                                                    <th class="num hidden-xs">Bewoonbaar (m²)</th>
                                                    <th class="num hidden-xs">Grond (m²)</th>
                                                    <th class="hidden-xs">Slaapkamers</th>
                                                    <th>Status</th>
                                                    <th class="num">Prijs</th>
                                                    <th class="center">Plan</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each unit In wonUnits
                                                    Dim isSold = (unit.ClientAccountId <> 0)
                                                    Dim isInOption = (Not isSold AndAlso unit.IsOption)
                                                    Dim isAvailable = (Not isSold AndAlso Not isInOption)
                                                    Dim surface = unit.Surface
                                                    Dim ground = unit.GroundSurface
                                                    Dim slpkRoom = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault()
                                                    Dim slpk = If(slpkRoom IsNot Nothing, slpkRoom.Number, 0)
                                                    Dim heeftOpties = (unit.FinishingOptions.Count > 0 AndAlso Not isSold)
                                                    @<tr class="@(If(isSold, "rij-verkocht", "")) @(If(heeftOpties, "heeft-opties", ""))" data-unit-id="@unit.Id">
                                                        <td>
                                                            <div class="lot-badge-wrap">
                                                                <div class="lot-badge">
                                                                    @*<div class="lot-nr">@unit.Name</div>*@
                                                                    @unit.Name
                                                                </div>
                                                                @If heeftOpties Then
                                                                    @<span class="opties-badge">@unit.FinishingOptions.Count opties</span>
                                                                End If
                                                            </div>
                                                        </td>
                                                        <td class="num hidden-xs">@(If(surface > 0, String.Format("{0:n0}", surface) & " m²", "-"))</td>
                                                        <td class="num hidden-xs">@(If(ground > 0, String.Format("{0:n0}", ground) & " m²", "-"))</td>
                                                        <td class="hidden-xs">
                                                            @If slpk > 0 Then
                                                                @<div class="slaap-wrap">
                                                                    @For i = 1 To Math.Min(slpk, 4)
                                                                        @<span class="slaap-dot"></span>
                                                                    Next
                                                                    @For i = slpk + 1 To 4
                                                                        @<span class="slaap-dot leeg"></span>
                                                                    Next
                                                                    <span class="slaap-label">@slpk</span>
                                                                </div>
                                                            End If
                                                        </td>
                                                        <td>
                                                            @If isInOption Then
                                                                @<span class="eenheid-status-pill status-in-optie"><span class="status-dot"></span>In optie</span>
                                                            ElseIf isSold Then
                                                                @<span class="eenheid-status-pill status-verkocht"><span class="status-dot"></span>Verkocht</span>
                                                            Else
                                                                @<span class="eenheid-status-pill status-beschikbaar"><span class="status-dot"></span>Beschikbaar</span>
                                                            End If
                                                        </td>
                                                        <td class="num">
                                                            @If Not isSold Then
                                                                @If heeftOpties Then
                                                                    Dim wonMinOptPrice = unit.LandValue + unit.FinishingOptions.Min(Function(fo) fo.TotalValue)
                                                                    @<text>
                                                                        <span class="prijs-vanaf-label">Vanaf</span>
                                                                        <span class="prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(wonMinOptPrice)</span>
                                                                    </text>
                                                                Else
                                                                    @<span class="prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue)</span>
                                                                End If
                                                            Else
                                                                @<span class="prijs-verkocht">—</span>
                                                            End If
                                                        </td>
                                                        <td class="center opties-plan-cel">
                                                            @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                                @<a href="#modalsendplan" class="eenheid-dl-btn modal-with-form btnsendplan" title="Plan downloaden" data-id="@unit.Id">
                                                                    <svg viewBox="0 0 24 24" width="15" height="15" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                                                                </a>
                                                            End If
                                                            @If heeftOpties Then
                                                                @<button type="button" class="opties-toggle-btn" data-unit-id="@unit.Id" aria-expanded="false" title="Afwerkingsopties bekijken">
                                                                    <svg class="toggle-ico toggle-ico-open" viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                                                                    <svg class="toggle-ico toggle-ico-close" viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none"><polyline points="18 15 12 9 6 15"/></svg>
                                                                </button>
                                                            End If
                                                        </td>
                                                    </tr>
                                                    @If heeftOpties Then
                                                        @<tr class="opties-expand-rij" id="opties-won-@unit.Id" style="display:none;">
                                                            <td colspan="7" class="opties-expand-cel">
                                                                <div class="opties-expand-inner">
                                                                    @For Each opt In unit.FinishingOptions.OrderBy(Function(o) o.SortOrder)
                                                                        Dim optPrijs = unit.LandValue + opt.TotalValue
                                                                        @<div class="optie-kaart @(If(opt.IsDefault, "optie-kaart-default", ""))">
                                                                            <div class="optie-kaart-naam">@opt.Name.ToUpper()</div>
                                                                            <div class="optie-kaart-prijs">@WWWCOPRO.Extensions.ToEuroCurrency(optPrijs)</div>
                                                                            @If isInOption Then
                                                                                @<span class="eenheid-status-pill status-in-optie"><span class="status-dot"></span>In optie</span>
                                                                            Else
                                                                                @<span class="eenheid-status-pill status-beschikbaar"><span class="status-dot"></span>Beschikbaar</span>
                                                                            End If
                                                                        </div>
                                                                    Next
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    End If
                                                Next
                                            </tbody>
                                            @*<tfoot>
                                                <tr>
                                                    <td colspan="5">@wonUnits.Count() @(If(wonUnits.Count() > 1, "woningen", "woning"))</td>
                                                    <td class="num total-prijs">@If wonAvail.Any() Then @<text>@If wonMinPrice = wonMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(wonMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(wonMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(wonMaxPrice)</text> End If</text> End If</td>
                                                    <td></td>
                                                </tr>
                                            </tfoot>*@
                                        </table>
                                        <div class="samenvatting">
                                            <div class="samenv-item">
                                                <div class="samenv-label">Woningen</div>
                                                <div class="samenv-val">@wonUnits.Count()</div>
                                                <div class="samenv-sub">@wonAvail.Count() beschikbaar</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Bewoonbaar</div>
                                                <div class="samenv-val">@(If(wonMinSurf = wonMaxSurf, String.Format("{0:n0}", wonMinSurf), String.Format("{0:n0}", wonMinSurf) & " – " & String.Format("{0:n0}", wonMaxSurf))) m²</div>
                                                <div class="samenv-sub">Per woning</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Grond</div>
                                                <div class="samenv-val">@(If(wonMinGround = wonMaxGround, String.Format("{0:n0}", wonMinGround), String.Format("{0:n0}", wonMinGround) & " – " & String.Format("{0:n0}", wonMaxGround))) m²</div>
                                                <div class="samenv-sub">Per perceel</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Slaapkamers</div>
                                                <div class="samenv-val">@(If(wonMinSlpk = wonMaxSlpk, wonMinSlpk.ToString(), wonMinSlpk & " – " & wonMaxSlpk))</div>
                                                <div class="samenv-sub">Per woning</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Prijsrange</div>
                                                <div class="samenv-val samenv-prijs">@If wonAvail.Any() Then @<text>@If wonMinPrice = wonMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(wonMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(wonMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(wonMaxPrice)</text> End If</text> End If</div>
                                                <div class="samenv-sub">Per eenheid</div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            End If
                            <!-- HANDEL -->
                            @If Model.Units.Where(Function(m) m.Type.Id = 10).Count > 0 Then
                                @Code
                                    Dim handUnits = Model.Units.Where(Function(m) m.Type.Id = 10).ToList()
                                    Dim handAvail = handUnits.Where(Function(m) m.ClientAccountId = 0 AndAlso Not m.IsOption).ToList()
                                    Dim handMinPrice = If(handAvail.Any(), handAvail.Min(Function(m) m.TotalValue), 0)
                                    Dim handMaxPrice = If(handAvail.Any(), handAvail.Max(Function(m) m.TotalValue), 0)
                                    Dim handMinSurf = If(handAvail.Any(), handAvail.Min(Function(m) m.Surface), 0)
                                    Dim handMaxSurf = If(handAvail.Any(), handAvail.Max(Function(m) m.Surface), 0)
                                End Code
                                @<div class="eenheid-sectie">
                                    <h2 class="sectie-titel">Handelspanden</h2>
                                    <div class="goud-streep"></div>
                                    <div class="tabel-wrap">
                                        <table class="eenheid-tabel">
                                            <thead>
                                                <tr>
                                                    <th>Lot</th>
                                                    <th class="num hidden-xs">Verdiep</th>
                                                    <th class="num hidden-xs">Opp (m²)</th>
                                                    <th>Status</th>
                                                    <th class="num">Prijs</th>
                                                    <th class="center">Plan</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                @For Each unit In handUnits
                                                    Dim isSold = (unit.ClientAccountId <> 0)
                                                    Dim isInOption = (Not isSold AndAlso unit.IsOption)
                                                    Dim isAvailable = (Not isSold AndAlso Not isInOption)
                                                    Dim surface = unit.Surface
                                                    @<tr class="@(If(isSold, "rij-verkocht", ""))">
                                                        <td>
                                                            <div class="lot-badge">
                                                                <div class="lot-nr">@unit.Name</div>
                                                                @unit.Name
                                                            </div>
                                                        </td>
                                                        <td class="num hidden-xs">@unit.Level</td>
                                                        <td class="num hidden-xs">@(If(surface > 0, String.Format("{0:n0}", surface) & " m²", "-"))</td>
                                                        <td>
                                                            @If isInOption Then
                                                                @<span class="eenheid-status-pill status-in-optie"><span class="status-dot"></span>In optie</span>
                                                            ElseIf isSold Then
                                                                @<span class="eenheid-status-pill status-verkocht"><span class="status-dot"></span>Verkocht</span>
                                                            Else
                                                                @<span class="eenheid-status-pill status-beschikbaar"><span class="status-dot"></span>Beschikbaar</span>
                                                            End If
                                                        </td>
                                                        <td class="num">
                                                            @If Not isSold Then
                                                                @<span class="prijs-val">@WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue)</span>
                                                            Else
                                                                @<span class="prijs-verkocht">—</span>
                                                            End If
                                                        </td>
                                                        <td class="center">
                                                            @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                                @<a href="#modalsendplan" class="eenheid-dl-btn modal-with-form btnsendplan" title="Plan downloaden" data-id="@unit.Id">
                                                                    <svg viewBox="0 0 24 24" width="15" height="15" stroke="currentColor" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                                                                </a>
                                                            End If
                                                        </td>
                                                    </tr>
                                                Next
                                            </tbody>
                                            @*<tfoot>
                                                <tr>
                                                    <td colspan="3">@handUnits.Count() @(If(handUnits.Count() > 1, "handelspanden", "handelspand"))</td>
                                                    <td></td>
                                                    <td class="num total-prijs">@If handAvail.Any() Then @<text>@If handMinPrice = handMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(handMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(handMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(handMaxPrice)</text> End If</text> End If</td>
                                                    <td></td>
                                                </tr>
                                            </tfoot>*@
                                        </table>
                                        <div class="samenvatting">
                                            <div class="samenv-item">
                                                <div class="samenv-label">Handelspanden</div>
                                                <div class="samenv-val">@handUnits.Count()</div>
                                                <div class="samenv-sub">@handAvail.Count() beschikbaar</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Oppervlakte</div>
                                                <div class="samenv-val">@(If(handMinSurf = handMaxSurf, String.Format("{0:n0}", handMinSurf), String.Format("{0:n0}", handMinSurf) & " – " & String.Format("{0:n0}", handMaxSurf))) m²</div>
                                                <div class="samenv-sub">Per pand</div>
                                            </div>
                                            <div class="samenv-item">
                                                <div class="samenv-label">Prijsrange</div>
                                                <div class="samenv-val samenv-prijs">@If handAvail.Any() Then @<text>@If handMinPrice = handMaxPrice Then @<text>@WWWCOPRO.Extensions.ToEuroCurrency(handMinPrice)</text> Else @<text>@WWWCOPRO.Extensions.ToEuroCurrency(handMinPrice) – @WWWCOPRO.Extensions.ToEuroCurrency(handMaxPrice)</text> End If</text> End If</div>
                                                <div class="samenv-sub">Per eenheid</div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            End If

                            <hr Class="solid tall">
                            <!--LOCATIE-->
                            <h4 Class="mt-md mb-md" id="map">Locatie op kaart</h4>
                            <div id="googlemaps" Class="google-map m-none mb-xlg"></div>

                            <!--FOTOS-->

                            @If Model.Data.Pictures.Any() Then

                                Dim totalPictures = Model.Data.Pictures.Count
                                Dim maxPictures = 8
                                Dim picturesToShow = Model.Data.Pictures.Take(maxPictures)
                                @<text>
                                    <hr class="solid tall" />

                                    <h4 class="mt-md mb-md">
                                        Recentste <strong>Foto's</strong>
                                        <a href="@Url.Action("Photos", "Projects", New With {.slug = Model.Data.Slug})">(alle foto's)</a>
                                    </h4>

                                    <div class="media-gallery">
                                        <div class="row mg-files" data-sort-destination data-sort-id="media-gallery">

                                            @For Each picture In picturesToShow
                                                @<text>
                                                    <div Class="isotope-item image col-sm-4 col-md-3 col-lg-3">
                                                        <div class="thumbnail">
                                                            <div class="thumb-preview">
                                                                <a class="thumb-image"
                                                                   href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                                                    <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)"
                                                                         class="img-responsive"
                                                                         alt="@picture.Caption" />
                                                                </a>
                                                                <div class="mg-thumb-options">
                                                                    <div class="mg-zoom"><i class="fa fa-search"></i></div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </text>
                                            Next

                                        </div>
                                    </div>
                                </text>
                                End If


                        </div>
            </div>
        </text>
    End if
</div>



<div id="modalsendplan" class="modal-block modal-block-primary mfp-hide">
    <div id="send-plan-container"></div>
</div>
<div id="modalsenddoc" class="modal-block modal-block-primary mfp-hide">
    <div id="send-doc-container"></div>
</div>
<div id="modalsendbrochure" class="modal-block modal-block-primary mfp-hide">
    <div id="send-brochure-container"></div>
</div>
<div id="modalsendmail" class="modal-block modal-block-primary mfp-hide">
    <div id="send-mail-container"></div>
</div>
@section scripts

    <script src="~/vendor/magnific-popup/jquery.magnific-popup.js"></script>
    <script>
    $(function () {
        var $main = $('#fgMainLink');
        if (!$main.length) return;

        // Bouw items op uit verborgen data-container
        var fgItems = [];
        $('#fgData a').each(function () {
            fgItems.push({
                src: $(this).data('full'),
                medium: $(this).data('medium'),
                type: 'image'
            });
        });
        if (!fgItems.length) return;

        var fgCurrentIndex = 0;

        function fgGoTo(index) {
            fgCurrentIndex = ((index % fgItems.length) + fgItems.length) % fgItems.length;
            $('#fgMainImg').attr('src', fgItems[fgCurrentIndex].src);
            $main.attr('href', fgItems[fgCurrentIndex].src);
        }

        $('#fgPrev').on('click', function (e) {
            e.preventDefault();
            fgGoTo(fgCurrentIndex - 1);
        });

        $('#fgNext').on('click', function (e) {
            e.preventDefault();
            fgGoTo(fgCurrentIndex + 1);
        });

        // Hoofdfoto klik: open lightbox op huidig index
        $main.on('click', function (e) {
            e.preventDefault();
            $.magnificPopup.open({
                items: fgItems,
                type: 'image',
                gallery: { enabled: true },
                startAt: fgCurrentIndex
            });
        });
    });
    </script>
    <script src="~/scripts/examples.modals.js"></script>
    <script src="~/vendor/rs-plugin/js/jquery.themepunch.tools.min.js"></script>
    <script src="~/vendor/rs-plugin/js/jquery.themepunch.revolution.min.js"></script>
    <script src="~/Scripts/examples.mediagallery.js"></script>
    <script>
    $(function () {
        $(document).on('click', '.opties-toggle-btn', function () {
            var unitId = $(this).data('unit-id');
            var $expandRow = $('#opties-won-' + unitId + ', #opties-apt-' + unitId);
            var isOpen = $(this).attr('aria-expanded') === 'true';
            if (isOpen) {
                $expandRow.hide();
                $(this).attr('aria-expanded', 'false');
                $(this).find('.toggle-ico-open').show();
                $(this).find('.toggle-ico-close').hide();
            } else {
                $expandRow.show();
                $(this).attr('aria-expanded', 'true');
                $(this).find('.toggle-ico-open').hide();
                $(this).find('.toggle-ico-close').show();
            }
        });
    });
    </script>
    <script>
    var bcoContactCookieName = 'bco_contact_info';

    function getSavedContactInfo() {
        var match = document.cookie.match(new RegExp('(?:^|; )' + bcoContactCookieName + '=([^;]*)'));
        if (!match) {
            return null;
        }
        try {
            return JSON.parse(decodeURIComponent(match[1]));
        } catch (e) {
            return null;
        }
    }

    function saveContactInfoToCookie(contact) {
        try {
            var serialized = encodeURIComponent(JSON.stringify(contact));
            document.cookie = bcoContactCookieName + '=' + serialized + ';path=/;max-age=' + (60 * 60 * 24 * 365);
        } catch (e) {
            // ignore cookie persistence errors
        }
    }

    function applySavedContact($container) {
        var saved = getSavedContactInfo();
        if (!saved) {
            return null;
        }
        if (saved.firstname) {
            $container.find('#txtFirstname').val(saved.firstname);
        }
        if (saved.name) {
            $container.find('#txtName').val(saved.name);
        }
        if (saved.email) {
            $container.find('#txtEmail').val(saved.email);
        }
        if (saved.phone) {
            $container.find('#txtPhone').val(saved.phone);
        }
        return saved;
    }



    $('.btnsendplan').click(function () {
        var url = "/Projects/SendPlan"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#send-plan-container').html(data);
            applySavedContact($('#send-plan-container'));
        });
    });
    $('.btnsenddoc').click(function () {
        var url = "/Projects/SendDoc"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#send-doc-container').html(data);
            applySavedContact($('#send-doc-container'));
        });
    });
    $('.btnsendbrochure').click(function () {
        var url = "/Projects/SendBrochure";
        var id = $(this).attr('data-id');
        $.get(url + '/' + id, function (data) {
            $('#send-brochure-container').html(data);
            applySavedContact($('#send-brochure-container'));

        });
    });
    $('.btnsendmail').click(function (e) {
        e.preventDefault();
        var id = $(this).attr('data-id');
        $.get('/Projects/SendMail/' + id, function (data) {
            $('#send-mail-container').html(data);
            $.magnificPopup.open({
                items: { src: '#modalsendmail' },
                type: 'inline',
                preloader: false,
                modal: true
            });
        });
    });
    function openBrochureFromHash() {
        if (window.location.hash !== "#brochure" || !$('.btnsendbrochure').length) {
            return;
        }

        var id = $('.btnsendbrochure').attr('data-id');
        $.get('/Projects/SendBrochure/' + id, function (data) {
            $('#send-brochure-container').html(data);
            applySavedContact($('#send-brochure-container'));
            $.magnificPopup.open({
                items: {
                    src: '#modalsendbrochure'
                },
                type: 'inline'
            });
        });
    }

    // Contactmodal direct openen via URL-hash: #contact of #modalsendmail
    // Gebruik: /projects/mijn-project#contact
    function openContactFromHash() {
        var hash = window.location.hash;
        if (hash !== '#contact' && hash !== '#modalsendmail') { return; }
        var $btn = $('.btnsendmail');
        if (!$btn.length) { return; }
        var id = $btn.attr('data-id');
        $.get('/Projects/SendMail/' + id, function (data) {
            $('#send-mail-container').html(data);
            $.magnificPopup.open({
                items: { src: '#modalsendmail' },
                type: 'inline'
            });
        });
    }

    $(document).ready(function () {
        $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        openBrochureFromHash();
        openContactFromHash();
    });

    $(window).on('hashchange', function () {
        openBrochureFromHash();
        openContactFromHash();
    });

    </script>
    <script src="https://maps.googleapis.com/maps/api/js?key=AIzaSyBixojVqE0nNXAPAjgQ9Q5Gnvk5K4zEcLM"></script>
    <script>

                // Map Markers
                var mapMarkers = [{
                    address: "New York, NY 10017",
                    html: "<strong>Porto Real Estate</strong>",
                    icon: {
                        image: "img/demos/real-estate/pin.png",
                        iconsize: [36, 36],
                        iconanchor: [36, 36]
                    },
                    popup: true
                }];

                var address = '@Model.Data.Street @Model.Data.HouseNumber, @Model.Data.Postalcode.Gemeente';

                var map = new google.maps.Map(document.getElementById('googlemaps'), {
        	            controls: {
        		            draggable: (($.browser.mobile) ? false : true),
        		            panControl: true,
        		            zoomControl: true,
        		            mapTypeControl: true,
        		            scaleControl: true,
        		            streetViewControl: true,
        		            overviewMapControl: true
        	            },
        	            scrollwheel: false,
        	            zoom: 15
                });

                var geocoder = new google.maps.Geocoder();
                var contentString = '<div id="content">' +
        '<h5 class="mb-xs">@Model.Data.Name</h5>' +
        '@Model.Data.Street @Model.Data.HouseNumber<br/>@Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente' +
        '</div>';

                var infowindow = new google.maps.InfoWindow({
                    content: contentString
                });
                var icon = {
                    url: "http://www.groupln.be/content/img/icons/map-marker.gif", // url
                    scaledSize: new google.maps.Size(29, 43), // scaled size
                    origin: new google.maps.Point(0, 0), // origin
                    anchor: new google.maps.Point(14.5, 40) // anchor
                };
                geocoder.geocode({
                    'address': address
                },

                function (results, status) {
                    if (status == google.maps.GeocoderStatus.OK) {
                        marker = new google.maps.Marker({
                            position: results[0].geometry.location,
                            title:'@Model.Data.Name',
                            popup: false,
                            icon: icon,
                            address:'@Model.Data.Street @Model.Data.HouseNumber, @Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente',
                            map: map

                        });
                        map.setCenter(results[0].geometry.location);
                        google.maps.event.addListener(marker, 'click', function () {
                            infowindow.open(map, marker);
                        });
                        infowindow.open(map, marker);
                    }
                });

			// Styled map (snazzymaps.com)
			var mapStyles = [{"featureType":"water","elementType":"geometry","stylers":[{"color":"#e9e9e9"},{"lightness":17}]},{"featureType":"landscape","elementType":"geometry","stylers":[{"color":"#f5f5f5"},{"lightness":20}]},{"featureType":"road.highway","elementType":"geometry.fill","stylers":[{"color":"#ffffff"},{"lightness":17}]},{"featureType":"road.highway","elementType":"geometry.stroke","stylers":[{"color":"#ffffff"},{"lightness":29},{"weight":0.2}]},{"featureType":"road.arterial","elementType":"geometry","stylers":[{"color":"#ffffff"},{"lightness":18}]},{"featureType":"road.local","elementType":"geometry","stylers":[{"color":"#ffffff"},{"lightness":16}]},{"featureType":"poi","elementType":"geometry","stylers":[{"color":"#f5f5f5"},{"lightness":21}]},{"featureType":"poi.park","elementType":"geometry","stylers":[{"color":"#dedede"},{"lightness":21}]},{"elementType":"labels.text.stroke","stylers":[{"visibility":"on"},{"color":"#ffffff"},{"lightness":16}]},{"elementType":"labels.text.fill","stylers":[{"saturation":36},{"color":"#333333"},{"lightness":40}]},{"elementType":"labels.icon","stylers":[{"visibility":"off"}]},{"featureType":"transit","elementType":"geometry","stylers":[{"color":"#f2f2f2"},{"lightness":19}]},{"featureType":"administrative","elementType":"geometry.fill","stylers":[{"color":"#fefefe"},{"lightness":20}]},{"featureType":"administrative","elementType":"geometry.stroke","stylers":[{"color":"#fefefe"},{"lightness":17},{"weight":1.2}]}];

			var styledMap = new google.maps.StyledMapType(mapStyles, {
				name: 'Styled Map'
			});

			map.mapTypes.set('map_style', styledMap);
			map.setMapTypeId('map_style');








    </script>
End Section
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
