@ModelType WWWBCO.ReferenceDetailModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
@Imports wwwcopro.extensions
<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>@Model.Data.Name te @Model.Data.Postalcode.Gemeente <span>@Model.Data.Street @Model.Data.HouseNumber <a href="#map" data-hash data-hash-offset="100">(locatie op kaart)</a></span></h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "References", New With {.id = UrlParameter.Optional}))">Realisaties</a></li>
                    <li class="active">@Model.Data.Name</li>
                </ul>
            </div>
            @*<div class="col-md-12">
                <ul class="breadcrumb">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li><a href="@(Url.Action("Index", "References", New With {.id = UrlParameter.Optional}))">Realisaties</a></li>
                    <li class="active">@Model.Data.Name</li>
                </ul>
            </div>*@
        </div>
        @*<div class="row">
            <div class="col-md-12">
                <h1>Projectgegevens</h1>
            </div>
        </div>*@
    </div>
</section>

<div class="container">

    <div class="row">
        <div class="col-md-12">
            <div class="portfolio-title">
                <div class="row">
                    <div class="portfolio-nav-all col-md-1">
                        <a href="@(Url.Action("Index", "References", New With {.id = UrlParameter.Optional}))" data-tooltip data-original-title="Terug naar onze realisaties"><i class="fa fa-th"></i></a>
                    </div>
                    <div class="col-md-10 center">
                        <h2 class="mb-none">@Model.Data.Name te @System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Model.Data.Postalcode.Gemeente.ToLower()) </h2>
                    </div>
                    @*<div class="portfolio-nav col-md-1">
                            <a href="portfolio-single-project.html" class="portfolio-nav-prev" data-tooltip data-original-title="Previous"><i class="fa fa-chevron-left"></i></a>
                            <a href="portfolio-single-project.html" class="portfolio-nav-next" data-tooltip data-original-title="Next"><i class="fa fa-chevron-right"></i></a>
                        </div>*@
                </div>
            </div>

            <hr class="tall">
        </div>
    </div>

    <div class="row">
        <div class="col-md-9">
            <div class="col-md-5">


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


                </div>



            </div>

            <div class="col-md-7">



                <h4 class="heading-primary">@Model.Data.CommercialTitleNL</h4>
                <p class="mt-xlg">@Model.Data.CommercialTextNL</p>


            </div>

        </div>
        <div class="col-md-3">
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
                        Else
                            @<text>
                                <td colspan="2" Class="background-color-primary text-uppercase text-center  text-light font-weight-bold  pt-md">
                                    Uitverkocht
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

            @*<aside class="sidebar">
                    <h5 class="heading-primary text-center">Ligging</h5>
                    <div class="divider">
                        <i class="fa fa-chevron-down"></i>
                    </div>
                    <div class="col-md-offset-1 col-md-11">
                        <ul class="list list-icons">
                            <li class="mb-none">
                                <i class="fa fa-map-marker"></i>@Model.Data.Street @Model.Data.HouseNumber <br />@Model.Data.Postalcode.Postcode @Model.Data.Postalcode.Gemeente
                            </li>
                        </ul>
                    </div>
                    <br />
                    <h5 class="heading-primary text-center">Bouwdirectie</h5>
                    <div class="divider">
                        <i class="fa fa-chevron-down"></i>
                    </div>
                    <div class="toggle toggle-primary mt-lg" data-plugin-toggle>
                        @If Not Model.Developer Is Nothing Then
                            @<text>
                                <section class="toggle">
                                    @Html.LabelFor(Function(m) m.Developer)
                                    <div class="toggle-content">
                                        @Html.Partial("Adressblock", Model.Developer)
                                    </div>
                                </section>
                            </text>
                        End If
                        @If Not Model.Builder Is Nothing Then
                            @<text>
                                <section class="toggle">
                                    @Html.LabelFor(Function(m) m.Builder)
                                    <div class="toggle-content">
                                        @Html.Partial("Adressblock", Model.Builder)
                                    </div>
                                </section>
                            </text>
                        End If
                        @If Not Model.Architect Is Nothing Then
                            @<text>
                                <section class="toggle">
                                    @Html.LabelFor(Function(m) m.Architect)
                                    <div class="toggle-content">
                                        @Html.Partial("Adressblock", Model.Architect)
                                    </div>
                                </section>
                            </text>
                        End If


                    </div>

                </aside>*@
        </div>
    </div>
    <div class="row">
        <hr Class="solid tall">

        <h4 Class="mt-md mb-md" id="map">Locatie op kaart</h4>
        <div id="googlemaps" Class="google-map m-none mb-xlg"></div>

    </div>
</div>

@section scripts

    <script src="~/Scripts/examples.mediagallery.js"></script>


    <script>
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