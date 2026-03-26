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
    <style>
        .modal-block {
            max-width: 600px !important;
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
                <h1 class="mb-none">@Model.Data.Name</h1>
                <p class="text-muted mb-none"><i class="fa fa-map-marker mr-xs"></i>@Model.Data.Street @Model.Data.HouseNumber, @Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente</p>
            </div>
        </div>
    </div>
</section>


<div class="container">
    <div class="row">
        <div class="col-md-7">

            <span class="thumb-info-listing-type thumb-info-listing-type-detail background-color-secondary text-uppercase text-color-light font-weight-semibold p-sm pl-md pr-md">
                @Model.Data.Name
            </span>

            <div class="thumb-gallery">
                <div class="lightbox" data-plugin-options="{'delegate': 'a', 'type': 'image', 'gallery': {'enabled': true}}">
                    <div class="owl-carousel owl-theme manual thumb-gallery-detail show-nav-hover mb-xs" id="thumbGalleryDetail">
                        @If Not Model.Data.DefaultPicture Is Nothing Or Model.Data.DefaultPicture.Id = 0 Then
                            @<text>
                                <div>
                                    <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & Model.Data.DefaultPicture.Name)">
                                        <span class="thumb-info thumb-info-centered-info thumb-info-no-borders font-size-xl">
                                            <span class="thumb-info-wrapper font-size-xl">
                                                <img alt="detailfoto" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.Data.DefaultPicture.Name)" class="img-responsive">
                                                <span class="thumb-info-title font-size-xl">
                                                    <span class="thumb-info-inner font-size-xl"><i class="icon-magnifier icons font-size-xl"></i></span>
                                                </span>
                                            </span>
                                        </span>
                                    </a>
                                </div>
                            </text>
                        End If

                        @For Each picture In Model.Data.Pictures
                            If picture.Type = BO.PictureType.Nevenfoto Then
                                @<text>
                                    <div>
                                        <a href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                            <span class="thumb-info thumb-info-centered-info thumb-info-no-borders font-size-xl">
                                                <span class="thumb-info-wrapper font-size-xl">
                                                    <img alt="detailfoto" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive">
                                                    <span class="thumb-info-title font-size-xl">
                                                        <span class="thumb-info-inner font-size-xl"><i class="icon-magnifier icons font-size-xl"></i></span>
                                                    </span>
                                                </span>
                                            </span>
                                        </a>
                                    </div>

                                </text>
                            End If
                        Next

                    </div>
                </div>

                <div class="owl-carousel owl-theme manual thumb-gallery-thumbs mt" id="thumbGalleryThumbs">
                    @If (Not Model.Data.DefaultPicture Is Nothing) AndAlso Model.Data.DefaultPicture.Id = 0 Then
                        @<text>
                            <img alt="Property Detail" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & Model.Data.DefaultPicture.Name)" class="img-responsive cur-pointer">
                        </text>
                    End If

                    @For Each picture In Model.Data.Pictures
                        If picture.Type = BO.PictureType.Nevenfoto Then
                            @<text>
                                <img alt="Property Detail" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive cur-pointer">
                            </text>
                        End If
                    Next

                </div>
            </div>

        </div>
        <div class="col-md-5">

            <table class="table table-striped">
                <colgroup>
                    <col width="35%">
                    <col width="65%">
                </colgroup>
                <tbody>
                    <tr>
                        @If Model.SalesData.StartingPrice > 0 Then
                            @<text>
                                <td Class="background-color-primary text-light pt-md">
                                    Prijzen vanaf
                                </td>
                                <td Class="font-size-xl font-weight-bold pt-sm pb-sm background-color-primary text-light">

                                    @WWWCOPRO.Extensions.ToEuroCurrency(Model.SalesData.StartingPrice)
                                </td>
                            </text>
                        ElseIf Model.SalesData.PercentageLivingUnitsSold < 15 Then
                            @<text>
                                <td colspan="2" Class="background-color-primary text-uppercase text-center  text-light font-weight-bold  pt-md">
                                    Lancering
                                </td>

                            </text>
                        ElseIf Model.SalesData.PercentageLivingUnitsSold = 100 AndAlso Model.SalesData.LivingUnits > 0 Then
                            @<text>
                                <td colspan="2" Class="background-color-primary text-uppercase text-center  text-light font-weight-bold  pt-md">
                                    Uitverkocht
                                </td>
                            </text>
                        ElseIf Model.SalesData.LivingUnits = 0 Then
                            @<text>
                                <td colspan="2" Class="background-color-primary text-uppercase text-center  text-light font-weight-bold  pt-md">
                                    Binnenkort
                                </td>
                            </text>
                        End If
                    </tr>
                    <tr>
                        <td>
                            Adres
                        </td>
                        <td>
                            @Model.Data.Street @Model.Data.HouseNumber - @Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente<br /><a href="#map" Class="font-size-sm" data-hash data-hash-offset="100">(Locatie op kaart)</a>
                        </td>
                    </tr>

                    @If Model.Units.Where(Function(m) m.Type.Id = 1).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-building"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.Id = 1).Count() @If Model.Units.Where(Function(m) m.Type.Id = 1).Count() > 1 Then@<text> <span style="position:relative;left:15px;">appartementen</span></text>Else @<text> <span style="position:relative;left:15px;">appartement</span></text>End if </td>
                            </tr>
                        </text>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-home"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.Id = 2).Count() @If Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 1 Then@<text> <span style="position:relative;left:15px;">woningen</span></text>Else @<text> <span style="position:relative;left:15px;">woning</span></text>End if </td>
                            </tr>
                        </text>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-shopping-cart"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() @If Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 1 Then@<text> <span style="position:relative;left:15px;">handelspanden</span></text>Else @<text> <span style="position:relative;left:15px;">handelspand</span></text>End if </td>
                            </tr>
                        </text>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-archive"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() @If Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 1 Then@<text> <span style="position:relative;left:15px;">bergingen</span></text>Else @<text> <span style="position:relative;left:15px;">berging</span></text>End if </td>
                            </tr>
                        </text>
                    End If

                    @If Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-road"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() @If Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 1 Then@<text> <span style="position:relative;left:15px;">parkeerplaatsen</span></text>Else @<text> <span style="position:relative;left:15px;">parkeerplaats</span></text>End if </td>
                            </tr>
                        </text>
                    End If
                    @If Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    <i Class="fa fa-car"></i>
                                </td>
                                <td>@Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() @If Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 1 Then@<text> <span style="position:relative;left:15px;">garages</span></text>Else @<text> <span style="position:relative;left:15px;">garage</span></text>End if </td>
                            </tr>
                        </text>
                    End If


                    <tr>
                        <td class="font-weight-bold text-color-primary">
                            Beschikbaar
                        </td>
                        <td class="font-weight-bold text-color-primary">
                            @(Model.SalesData.LivingUnits - Model.SalesData.LivingUnitsSold) <span style="position:relative;left:15px;">wooneenheden</span>
                        </td>
                    </tr>
                    @If Model.Data.Architect.ID > 0 Then
                        @<text>
                            <tr>
                                <td>
                                    Architect
                                </td>
                                <td>
                                    @Model.Data.Architect.Display
                                </td>
                            </tr>
                        </text>
                    End If

                </tbody>
            </table>

            @If Not Model.BrochureDoc Is Nothing AndAlso Model.BrochureDoc.IsBrochure Then
                @<text>
                    <div class="text-center mb-md" style="margin-left: 40px; margin-right: 40px; margin-top: 20px; margin-bottom: 20px;">
                        <a href="#modalsendbrochure" data-id="@Model.BrochureDoc.Docid" class="modal-with-form btnsendbrochure btn btn-primary btn-lg btn-block" style="border-radius:6px;">
                            <i class="fa fa-download mr-sm"></i> DOWNLOAD BROCHURE
                        </a>
                    </div>
                </text>
            End If
            @if Model.Docs.Count > 0 AndAlso Model.SalesSetttings.SaleVisible = True Then
                @<text>
                    <hr />
                    <h4 Class="pt-none mb-md text-color-dark">Documenten</h4>
                    <ul Class="list list-icons list-borders list-primary mb-lg ">
                        @for each doc In Model.Docs
                            @<text>
                                <li>  <a href="#modalsenddoc" class="modal-with-form btnsenddoc" data-toggle="tooltip" data-placement="top" title="Document opvragen" data-original-title="Document opvragen" type="button" data-id="@doc.Docid"><i Class="fa fa-download"></i> @doc.Name</a></li>
                            </text>
                        Next
                    </ul>
                </text>
            End If

            <div class="mt-sm mb-sm">
                <a href="tel:+3292164950" class="contact-cta-btn" style="margin-bottom:8px;">
                    <span class="cta-icon"><i class="fa fa-phone"></i></span>
                    <span class="cta-text">
                        <strong>+32 (0)9 216 49 50</strong>
                        <small>Bel ons direct</small>
                    </span>
                    <span class="cta-arrow"><i class="fa fa-chevron-right"></i></span>
                </a>
                <a href="#modalsendmail" data-id="@Model.Data.Id"
                   class="contact-cta-btn modal-with-form btnsendmail">
                    <span class="cta-icon"><i class="fa fa-envelope-o"></i></span>
                    <span class="cta-text">
                        <strong>Informatie aanvragen</strong>
                        <small>Vrijblijvend &mdash; wij antwoorden snel</small>
                    </span>
                    <span class="cta-arrow"><i class="fa fa-chevron-right"></i></span>
                </a>
            </div>
        </div>
    </div>
    <h4 Class="mt-md mb-md">@Model.Data.CommercialTitleNL</h4>
    @Model.Data.CommercialTextNL

    @If Model.SalesSetttings.SaleVisible = True Then
        @<text>
            <div Class="row">
                        <div Class="col-md-12">
                            <hr Class="solid tall">
                            <!--APPARTEMNTEN-->
                            @If Model.Units.Where(Function(m) m.Type.Id = 1).Count > 0 Then
                                @<text>
                                    <h4 Class="mt-md mb-md">Appartementen</h4>

                                    <table Class="table table-striped table-hover">
                                        <thead>
                                            <tr Class="font-weight-bold">
                                                <td Class="text-center">lot</td>
                                                <td class="text-center hidden-xs">verdiep</td>
                                                <td Class="text-center hidden-xs">opp (m²)</td>
                                                <td Class="text-center hidden-xs">terras (m²)</td>
                                                <td Class="text-center hidden-xs">tuin (m²)</td>
                                                <td class="text-center hidden-xs">slpks</td>
                                                <td Class="text-center">prijs</td>
                                                <td Class="text-center">plan</td>
                                            </tr>

                                        </thead>
                                        <tbody>
                                            @For Each unit In Model.Units.Where(Function(m) m.Type.Id = 1)

                                                Dim isAvailable = (unit.ClientAccountId = 0)
                                                Dim terras = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Terras Or m.Type = BO.RoomType.Dakterras).Sum(Function(i) i.Surface)
                                                Dim tuin = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Tuin).Sum(Function(i) i.Surface)
                                                Dim slpkRoom = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault()
                                                Dim slpk = If(slpkRoom IsNot Nothing, slpkRoom.Number, 0)

                                                @<tr @(If(Not isAvailable, "style=""color:lightgray""", ""))>
                                                    <td class="text-center">@unit.Name</td>
                                                    <td class="hidden-xs text-center">@unit.Level</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable, String.Format("{0:n0}", unit.Surface) & " m²", "-"))</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And terras > 0, String.Format("{0:n0}", terras) & " m²", "-"))</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And tuin > 0, String.Format("{0:n0}", tuin) & " m²", "-"))</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And slpk > 0, slpk, "-"))</td>
                                                    <td class="text-center">@(If(isAvailable, WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue), "Verkocht"))</td>
                                                    <td class="text-center">
                                                        @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                            @<a href="#modalsendplan"
                                                                class="fa fa-download modal-with-form btnsendplan"
                                                                data-toggle="tooltip"
                                                                title="downloaden"
                                                                data-id="@unit.Id"></a>
                                                        End If
                                                    </td>
                                                </tr>

                                            Next
                                        </tbody>
                                    </table>
                                </text>
                            End if
                            <!--WONINGEN-->
                            @If Model.Units.Where(Function(m) m.Type.Id = 2).Count > 0 Then
                                @<text>
                                    <h4 Class="mt-md mb-md">Woningen</h4>

                                    <table Class="table table-striped table-hover">
                                        <thead>
                                            <tr Class="font-weight-bold">
                                                <td Class="text-center">Lot</td>
                                                <td Class="text-center hidden-xs">Bewoonbare opp (m²)</td>
                                                <td Class="text-center hidden-xs">Grond (m²)</td>
                                                <td class="text-center hidden-xs">Slaapkamers</td>
                                                <td Class="text-center">Prijs</td>
                                                <td Class="text-center">Plan</td>
                                            </tr>

                                        </thead>
                                        <tbody>
                                            @For Each unit In Model.Units.Where(Function(m) m.Type.Id = 2)

                                                Dim isAvailable = (unit.ClientAccountId = 0)
                                                Dim surface = unit.Surface
                                                Dim ground = unit.GroundSurface
                                                Dim slpkRoom = unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault()
                                                Dim slpk = If(slpkRoom IsNot Nothing, slpkRoom.Number, 0)

                                                @<tr @(If(Not isAvailable, "style=""color:lightgray""", ""))>
                                                    <td class="text-center">@unit.Name</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And surface > 0, String.Format("{0:n0}", surface) & " m²", "-"))</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And ground > 0, String.Format("{0:n0}", ground) & " m²", "-"))</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And slpk > 0, slpk, "-"))</td>
                                                    <td class="text-center">@(If(isAvailable, WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue), "Verkocht"))</td>
                                                    <td class="text-center">
                                                        @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                            @<a href="#modalsendplan"
                                                                class="fa fa-download modal-with-form btnsendplan"
                                                                data-toggle="tooltip"
                                                                title="downloaden"
                                                                data-id="@unit.Id"></a>
                                                        End If
                                                    </td>
                                                </tr>

                                            Next
                                        </tbody>
                                    </table>
                                </text>
                            End if
                            <!--HANDEL-->
                            @If Model.Units.Where(Function(m) m.Type.Id = 10).Count > 0 Then
                                @<text>
                                    <h4 Class="mt-md mb-md">Woningen</h4>

                                    <table Class="table table-striped table-hover">
                                        <thead>
                                            <tr Class="font-weight-bold">
                                                <td Class="text-center">Lot</td>
                                                <td Class="text-center hidden-xs">Bewoonbare opp (m²)</td>
                                                <td Class="text-center hidden-xs">Grond (m²)</td>
                                                <td class="text-center hidden-xs">Slaapkamers</td>
                                                <td Class="text-center">Prijs</td>
                                                <td Class="text-center">Plan</td>
                                            </tr>

                                        </thead>
                                        <tbody>
                                            @For Each unit In Model.Units.Where(Function(m) m.Type.Id = 10)

                                                Dim isAvailable = (unit.ClientAccountId = 0)
                                                Dim surface = unit.Surface

                                                @<tr @(If(Not isAvailable, "style=""color:lightgray""", ""))>
                                                    <td class="text-center">@unit.Name</td>
                                                    <td class="hidden-xs text-center">@unit.Level</td>
                                                    <td class="text-center hidden-xs">@(If(isAvailable And surface > 0, String.Format("{0:n0}", surface) & " m²", "-"))</td>
                                                    <td class="text-center">@(If(isAvailable, WWWCOPRO.Extensions.ToEuroCurrency(unit.TotalValue), "Verkocht"))</td>
                                                    <td class="text-center">
                                                        @If isAvailable AndAlso unit.Plan IsNot Nothing Then
                                                            @<a href="#modalsendplan"
                                                                class="fa fa-download modal-with-form btnsendplan"
                                                                data-toggle="tooltip"
                                                                title="downloaden"
                                                                data-id="@unit.Id"></a>
                                                        End If
                                                    </td>
                                                </tr>

                                            Next
                                        </tbody>
                                    </table>
                                </text>
                            End if

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
    <script src="~/scripts/examples.modals.js"></script>
    <script src="~/vendor/rs-plugin/js/jquery.themepunch.tools.min.js"></script>
    <script src="~/vendor/rs-plugin/js/jquery.themepunch.revolution.min.js"></script>
    <script src="~/Scripts/examples.mediagallery.js"></script>
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
    $('.btnsendmail').click(function () {
        var url = "/Projects/SendMail"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#send-mail-container').html(data);
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
