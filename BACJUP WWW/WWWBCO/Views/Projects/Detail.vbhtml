@ModelType WWWBCO.ProjectDetailModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@Imports wwwbco.extensions
@Imports System.Text.RegularExpressions
@section PageStyle
    <link rel="stylesheet" href="~/Content/real-estate.css" />
    <link rel="stylesheet" href="~/vendor/magnific-popup/magnific-popup.css" />
end section


<div class="container">


</div>

<section class="page-header page-header-light page-header-more-padding">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>@Model.Data.Postalcode.Gemeente <span>@Model.Data.Street @Model.Data.HouseNumber <a href="#map" data-hash data-hash-offset="100">(locatie op kaart)</a></span></h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional}))">Woonprojecten</a></li>
                    <li class="active">@Model.Data.Name</li>
                </ul>
            </div>
        </div>
    </div>
</section>

<div class="container">

    <div class="row pb-xl pt-md">
        <div class="col-md-9">

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
                            @If Not Model.Data.DefaultPicture Is Nothing Or Model.Data.DefaultPicture.Id = 0 Then
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

                                                @Html.DisplayFor(Function(m) m.SalesData.StartingPrice)
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
                                            <td>@Model.Units.Where(Function(m) m.Type.Id = 1).Count() @If Model.Units.Where(Function(m) m.Type.Id = 1).Count() > 1 Then@<text> <span style="position:relative;left:15px;">appartementen</span></text>Else @<text> <span style="position:relative;left:15px;">appartement</span></text> End if </td>
                                        </tr>
                                    </text>
                            End If
                                @If Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 0 Then
                                    @<text>
                                        <tr>
                                            <td>
                                                <i Class="fa fa-home"></i>
                                            </td>
                                            <td>@Model.Units.Where(Function(m) m.Type.Id = 2).Count() @If Model.Units.Where(Function(m) m.Type.Id = 2).Count() > 1 Then@<text> <span style="position:relative;left:15px;">woningen</span></text>Else @<text> <span style="position:relative;left:15px;">woning</span></text> End if </td>
                                        </tr>
                                    </text>
                            End If
                                @If Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 0 Then
                                    @<text>
                                        <tr>
                                            <td>
                                                <i Class="fa fa-shopping-cart"></i>
                                            </td>
                                            <td>@Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() @If Model.Units.Where(Function(m) m.Type.GroupId = 4).Count() > 1 Then@<text> <span style="position:relative;left:15px;">handelspanden</span></text>Else @<text> <span style="position:relative;left:15px;">handelspand</span></text> End if </td>
                                        </tr>
                                    </text>
                            End If
                                @If Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 0 Then
                                    @<text>
                                        <tr>
                                            <td>
                                                <i Class="fa fa-archive"></i>
                                            </td>
                                            <td>@Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() @If Model.Units.Where(Function(m) m.Type.GroupId = 2).Count() > 1 Then@<text> <span style="position:relative;left:15px;">bergingen</span></text>Else @<text> <span style="position:relative;left:15px;">berging</span></text> End if </td>
                                        </tr>
                                    </text>
                            End If

                                @If Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 0 Then
                                    @<text>
                                        <tr>
                                            <td>
                                                <i Class="fa fa-road"></i>
                                            </td>
                                            <td>@Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() @If Model.Units.Where(Function(m) m.Type.Id = 5 Or m.Type.Id = 6).Count() > 1 Then@<text> <span style="position:relative;left:15px;">parkeerplaatsen</span></text>Else @<text> <span style="position:relative;left:15px;">parkeerplaats</span></text> End if </td>
                                        </tr>
                                    </text>
                            End If
                                @If Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 0 Then
                                    @<text>
                                        <tr>
                                            <td>
                                                <i Class="fa fa-car"></i>
                                            </td>
                                            <td>@Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() @If Model.Units.Where(Function(m) m.Type.Id = 7 Or m.Type.Id = 8).Count() > 1 Then@<text> <span style="position:relative;left:15px;">garages</span></text>Else @<text> <span style="position:relative;left:15px;">garage</span></text> End if </td>
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
                    
                    
                    <div class="feature-box feature-box-light feature-box-style-5 background-color-primary p-sm m-none" style="height:70px;">
                        <div class="feature-box-icon" style="height:50px;">
                            <i class="fa fa-phone"></i>
                        </div>
                        <div class="feature-box-info text-light">
                            <h4 class="mb-none text-light">+32 (0)9 216 49 50</h4>
                            <p class="text-light"><small>Neem telefonisch contact op</small></p>

                        </div>

                    </div>
                    <div class="feature-box feature-box-light feature-box-style-5 background-color-primary p-sm m-none" style="height:70px;">
                        <a href="#modalsendmail" data-id="@Model.Data.Id" class="modal-with-form btnsendmail">
                            <div class="feature-box-icon">
                                <i class="fa fa-envelope-o"></i>
                            </div>
                            <div class="feature-box-info text-light">
                                <h4 class="mb-none text-light">
                                    Neem contact op
                                </h4>
                                <p class="text-light"><small>Stuur ons een email</small></p>

                            </div>
                        </a>
                    </div>
                        </div>
            </div>
            <h4 Class="mt-md mb-md">@Model.Data.CommercialTitleNL</h4>
            @Model.Data.CommercialTextNL
        </div>
        <div Class="col-md-3">
            <aside Class="sidebar">
                @if Model.Docs.Count > 0 AndAlso Model.SalesSetttings.SaleVisible = True Then
                @<text>
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
                <div class="recent-posts">
                    @If Not Model.News.Count = 0 AndAlso Model.SalesSetttings.SaleVisible = True Then
                            @<text>
                    <h4 Class="pt-none mb-md text-color-dark">Laatste nieuws</h4>
                    </text>
                    End if
                    @If Model.News.Count > 1 AndAlso Model.SalesSetttings.SaleVisible = True Then
                    @For Each newsitem In Model.News.GetRange(0, 1)
                    @<text>

                        <article>
                            <div class="date">
                                <span class="day">@newsitem.NewsDate.Day </span>
                                <span class="month">@newsitem.NewsDate.ToString("MMM")</span>
                            </div>
                            <h5 class="heading-primary"><a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))">@newsitem.TitleNL </a></h5>
                            <p>@TrimTo(newsitem.TextNL, 200) <a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))" class="read-more">lees meer <i class="fa fa-angle-right"></i></a></p>
                            <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/news/800/" & newsitem.Picture.Name)" />
                        </article>

                    </Text>
                    Next
                    ElseIf Model.News.Count = 1 AndAlso Model.SalesSetttings.SaleVisible = True Then
                    @For Each newsitem In Model.News.GetRange(0, Model.News.Count)
                    @<text>

                        <article>
                            <div class="date">
                                <span class="day">@newsitem.NewsDate.Day </span>
                                <span class="month">@newsitem.NewsDate.ToString("MMM")</span>
                            </div>
                            <h5 class="heading-primary"><a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))">@newsitem.TitleNL </a></h5>
                            <p>@TrimTo(newsitem.TextNL, 200)  <a href="@(Url.Action("News", "Projects", New With {.id = Model.Data.Id}))" class="read-more">lees meer <i class="fa fa-angle-right"></i></a></p>
                            <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/news/" & newsitem.Picture.Name)" />

                        </article>

                    </Text>
                    Next
                                    End If


</div>
</aside>
        </div>
        
  </div>
    @If Model.SalesSetttings.SaleVisible = True Then
            @<text>

    <div Class="row">
        <div Class="col-md-12">


            <hr Class="solid tall">
            @If Model.Units.Where(Function(m) m.Type.Id = 1).Count > 0 Then
                @<text>
                
                                      <h4 Class="mt-md mb-md">Appartementen</h4>

                                <Table Class="table table-striped table-hover">
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
                        @for Each unit In Model.Units.Where(Function(m) m.Type.Id = 1)
                            If unit.ClientAccountId = 0 Then
                        @<text>
                            <tr>
                                <td Class="text-center">@unit.Name</td>
                                <td class="hidden-xs text-center">@unit.Level</td>
                                <td class="text-center hidden-xs">@If unit.ClientAccountId = 0 Then @<text>@String.Format("{0:n0}", unit.Surface) m²</text> Else @<text>-</text> end if</td>
                                <td Class="text-center hidden-xs">@If unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Terras Or m.Type = BO.RoomType.Dakterras).Count > 0 AndAlso unit.ClientAccountId = 0 Then @<text> @String.Format("{0:n0}", unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Terras Or m.Type = BO.RoomType.Dakterras).Sum(Function(i) i.Surface)) m² </text>else @<text>-</text> end If</td>
                                <td class="text-center hidden-xs">@If unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Tuin).Count > 0 AndAlso unit.ClientAccountId = 0 Then @<text>@String.Format("{0:n0}", unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Tuin).Sum(Function(i) i.Surface)) m² </text>else @<text>-</text>    End If</td>
                                <td class="text-center hidden-xs">@If unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).Count > 0 AndAlso unit.ClientAccountId = 0 Then @unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault.Number end If</td>
                                @If unit.ClientAccountId = 0 Then @<text>
                                                            <td Class="text-center"> @Html.DisplayFor(Function(m) unit.TotalValue)</td></text> Else @<text>
                                                            <td Class="text-center"> Verkocht</td></text>end if
                                <td class="text-center">@If unit.Plan IsNot Nothing AndAlso unit.ClientAccountId = 0 Then @<text><a href="http://www.groupln.be/uploads/plans/@unit.Plan" target="_blank" class="fa fa-download" data-toggle="tooltip" data-placement="top" title="downloaden" data-original-title="downloaden" type="button" data-id="@unit.Id"></a></text>Else @<text></text>End if </td>

                            </tr>
                        </text>
                                                                        Else
                                                                        @<text>
                                                                            <tr style="color:lightgray">
                                                                                <td Class="text-center">@unit.Name</td>
                                                                                <td class="hidden-xs text-center">@unit.Level</td>
                                                                                <td class="text-center hidden-xs">-</td>
                                                                                <td Class="text-center hidden-xs">-</td>
                                                                                <td class="text-center hidden-xs">-</td>
                                                                                <td class="text-center hidden-xs">-</td>
                                                                                <td Class="text-center">Verkocht</td>
                                                                                <td class="text-center"></td>

                                                                            </tr>
                                                                        </text>
                            End If

                        Next
                                        </tbody>
                                    </table>
                </text>
                End If
            @If Model.Units.Where(Function(m) m.Type.Id = 2).Count > 0 Then
                @<text>
            <h4 Class="mt-md mb-md">Woningen</h4>
            <Table Class="table table-striped table-hover">
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
                        If unit.ClientAccountId = 0 Then
                            @<text>
                                <tr>
                                    <td Class="text-center">@unit.Name</td>
                                    <td class="text-center hidden-xs">@If unit.ClientAccountId = 0 Then @<text>@String.Format("{0:n0}", unit.Surface) m²</text> Else @<text>-</text> end if</td>
                                    <td Class="text-center hidden-xs">@If unit.ClientAccountId = 0 AndAlso unit.GroundSurface > 0 Then @<text> @String.Format("{0:n0}", unit.GroundSurface) m²</text>else @<text>-</text> end If</td>
                                    <td class="text-center hidden-xs">@if unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).Count > 0 AndAlso unit.ClientAccountId = 0 Then @unit.Rooms.Where(Function(m) m.Type = BO.RoomType.Slaapkamer).FirstOrDefault.Number end If</td>
                                    @If unit.ClientAccountId = 0 Then @<text>
                                        <td Class="text-center"> @Html.DisplayFor(Function(m) unit.TotalValue)</td></text> Else @<text>
                                        <td Class="text-center"> Verkocht</td></text>end if
                                    <td class="text-center">@If unit.Plan IsNot Nothing AndAlso unit.ClientAccountId = 0 Then @<text><a href="http://www.groupln.be/uploads/plans/@unit.Plan" target="_blank" class="fa fa-download" data-toggle="tooltip" data-placement="top" title="downloaden" data-original-title="downloaden" type="button" data-id="@unit.Id"></a></text>Else @<text></text>End if </td>

                                </tr>
                            </text>
                        Else
                            @<text>
                                <tr style="color:lightgray">
                                    <td Class="text-center">@unit.Name</td>
                                    <td class="text-center hidden-xs">-</td>
                                    <td Class="text-center hidden-xs">-</td>
                                    <td class="text-center hidden-xs">-</td>
                                    <td Class="text-center">Verkocht</td>
                                    <td class="text-center"></td>

                                </tr>
                            </text>
                        End If

                    Next
                </tbody>
            </Table>
                </text>
            End If
            @If Model.Units.Where(Function(m) m.Type.Id = 10).Count > 0 Then
                @<text>
                    <h4 Class="mt-md mb-md">Handel</h4>
                    <Table Class="table table-striped table-hover">
                        <thead>
                            <tr Class="font-weight-bold">
                                <td Class="text-center">lot</td>
                                <td class="text-center hidden-xs">verdiep</td>
                                <td Class="text-center hidden-xs">opp (m²)</td>
                                <td Class="text-center">prijs</td>
                                <td Class="text-center">plan</td>
                            </tr>

                        </thead>
                        <tbody>
                            @For Each unit In Model.Units.Where(Function(m) m.Type.Id = 10)
                        If unit.ClientAccountId = 0 Then
                                    @<text>
                                        <tr>
                                            <td Class="text-center">@unit.Name</td>
                                            <td class="hidden-xs text-center">@unit.Level</td>
                                            <td class="text-center hidden-xs">@If unit.ClientAccountId = 0 Then @<text>@String.Format("{0:n0}", unit.Surface) m²</text> Else @<text>-</text> end if</td>
                                            @If unit.ClientAccountId = 0 Then @<text>
                                        <td Class="text-center"> @Html.DisplayFor(Function(m) unit.TotalValue)</td></text> Else @<text>
                                        <td Class="text-center"> Verkocht</td></text>end if
                                            <td class="text-center">@If unit.Plan IsNot Nothing AndAlso unit.ClientAccountId = 0 Then @<text><a href="http://www.groupln.be/uploads/plans/@unit.Plan" target="_blank" class="fa fa-download" data-toggle="tooltip" data-placement="top" title="downloaden" data-original-title="downloaden" type="button" data-id="@unit.Id"></a></text>Else @<text></text>End if </td>

                                        </tr>
                                    </text>
                                Else
                                    @<text>
                                        <tr style="color:lightgray">
                                            <td Class="text-center">@unit.Name</td>
                                            <td class="hidden-xs text-center">@unit.Level</td>
                                            <td class="text-center hidden-xs">-</td>
                                            <td Class="text-center">Verkocht</td>
                                            <td class="text-center"></td>

                                        </tr>
                                    </text>
                                End If

                            Next
                        </tbody>
                    </Table>
                </text>
            End If
            <hr Class="solid tall">

            <h4 Class="mt-md mb-md" id="map">Locatie op kaart</h4>
            <div id = "googlemaps" Class="google-map m-none mb-xlg"></div>

            @If Not Model.Data.Pictures.Count = 0 Then

                @<text>


                            <hr class="solid tall">

                            <h4 class="mt-md mb-md">Recentste <strong>Foto's</strong> <a href="@(Url.Action("Photos", "Projects", New With {.slug = Model.Data.Slug}))">(alle foto's)</a></h4>

                            <div class="media-gallery">
                                <div class="row mg-files" data-sort-destination data-sort-id="media-gallery">

                                    @If Model.Data.Pictures.Count > 8 Then
                                        @For Each picture In Model.Data.Pictures.GetRange(0, 8)
                                            @<text>
                                                <div class="isotope-item image col-sm-4 col-md-3 col-lg-3">
                                                    <div class="thumbnail ">
                                                        <div class="thumb-preview">
                                                            <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                                                <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                                                            </a>
                                                            <div class="mg-thumb-options">
                                                                <div class="mg-zoom"><i class="fa fa-search"></i></div>

                                                            </div>
                                                        </div>

                                                    </div>
                                                </div>
                                            </text>
                                        Next
                                                                Else
                                        @For Each picture In Model.Data.Pictures.GetRange(0, Model.Data.Pictures.Count)
                                            @<text>
                                                <div class="isotope-item image col-sm-4 col-md-3 col-lg-3">

                                                    <div class="thumbnail thumbnail-no-borders">
                                                        <div class="thumb-preview">
                                                            <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                                                <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                                                            </a>
                                                            <div class="mg-thumb-options">
                                                                <div class="mg-zoom"><i class="fa fa-search"></i></div>

                                                            </div>
                                                        </div>

                                                    </div>
                                                </div>
                                            </text>
                                        Next
                            End If


                                </div>
                            </div>

                </text>
            End If
        </div>
    </div>
    </text>
    End If
    </div>

<div id="modalsendplan" class="modal-block modal-block-primary mfp-hide">
    <div id="send-plan-container"></div>
</div>
<div id="modalsenddoc" class="modal-block modal-block-primary mfp-hide">
    <div id="send-doc-container"></div>
</div>
<div id="modalsendmail" class="modal-block modal-block-lg modal-block-primary mfp-hide">
    <div id="send-mail-container"></div>
</div>
@section scripts

<script src="~/scripts/admin/theme.admin.extension.js" ></script>
<script src="~/scripts/admin/theme.js" ></script>
<script src="~/vendor/magnific-popup/jquery.magnific-popup.js" ></script>
<script src="~/scripts/examples.modals.js"></script>
<script src="~/vendor/rs-plugin/js/jquery.themepunch.tools.min.js"></script>
<script src="~/vendor/rs-plugin/js/jquery.themepunch.revolution.min.js"></script>
<script src="~/Scripts/examples.mediagallery.js"></script>
<script>
    $('.btnsendplan').click(function () {
            var url = "/Projects/SendPlan"; // the url to the controller
            var id = $(this).attr('data-id'); // the id that's given to each button in the list
            $.get(url + '/' + id, function (data) {
                $('#send-plan-container').html(data);
            });
    });
    $('.btnsenddoc').click(function () {
        var url = "/Projects/SendDoc"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#send-doc-container').html(data);
        });
    });
    $('.btnsendmail').click(function () {
        var url = "/Projects/SendMail"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#send-mail-container').html(data);
        });
    });
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
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

			mapRef = $('#googlemaps').data('gMap.reference');

			// Create an array of styles.
			var mapColor = "#cfa968";

			var styles = [{
				stylers: [{
					hue: mapColor
				}]
			}, {
				featureType: "road",
				elementType: "geometry",
				stylers: [{
					lightness: 0
				}, {
					visibility: "simplified"
				}]
			}, {
				featureType: "road",
				elementType: "labels",
				stylers: [{
					visibility: "off"
				}]
			}];

			// Styles from https://snazzymaps.com/
			var styles = [{"featureType":"water","elementType":"geometry","stylers":[{"color":"#e9e9e9"},{"lightness":17}]},{"featureType":"landscape","elementType":"geometry","stylers":[{"color":"#f5f5f5"},{"lightness":20}]},{"featureType":"road.highway","elementType":"geometry.fill","stylers":[{"color":"#ffffff"},{"lightness":17}]},{"featureType":"road.highway","elementType":"geometry.stroke","stylers":[{"color":"#ffffff"},{"lightness":29},{"weight":0.2}]},{"featureType":"road.arterial","elementType":"geometry","stylers":[{"color":"#ffffff"},{"lightness":18}]},{"featureType":"road.local","elementType":"geometry","stylers":[{"color":"#ffffff"},{"lightness":16}]},{"featureType":"poi","elementType":"geometry","stylers":[{"color":"#f5f5f5"},{"lightness":21}]},{"featureType":"poi.park","elementType":"geometry","stylers":[{"color":"#dedede"},{"lightness":21}]},{"elementType":"labels.text.stroke","stylers":[{"visibility":"on"},{"color":"#ffffff"},{"lightness":16}]},{"elementType":"labels.text.fill","stylers":[{"saturation":36},{"color":"#333333"},{"lightness":40}]},{"elementType":"labels.icon","stylers":[{"visibility":"off"}]},{"featureType":"transit","elementType":"geometry","stylers":[{"color":"#f2f2f2"},{"lightness":19}]},{"featureType":"administrative","elementType":"geometry.fill","stylers":[{"color":"#fefefe"},{"lightness":20}]},{"featureType":"administrative","elementType":"geometry.stroke","stylers":[{"color":"#fefefe"},{"lightness":17},{"weight":1.2}]}];

			var styledMap = new google.maps.StyledMapType(styles, {
				name: 'Styled Map'
			});

			mapRef.mapTypes.set('map_style', styledMap);
			mapRef.setMapTypeId('map_style');

   






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