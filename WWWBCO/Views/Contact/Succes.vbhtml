@MOdeltype WWWBCO.MailModel 
@Code
    ViewData("Title") = "BCO - Contacteer ons"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code

<section class="page-header page-header-light page-header-more-padding">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <h1>Contact</h1>
                <ul class="breadcrumb breadcrumb-valign-mid">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">Contact</li>
                </ul>
            </div>
        </div>
    </div>
</section>
@Html.Partial("_ValidationSummary", ViewData.ModelState)

<div id="googlemaps" class="google-map"></div>

<div class="container">

    <div class="row">
        <div class="col-md-6">

            @*<div class="alert alert-success hidden" id="contactSuccess">
                <strong>Success!</strong> Your message has been sent to us.
            </div>

            <div class="alert alert-danger hidden" id="contactError">
                <strong>Error!</strong> There was an error sending your message.
            </div>*@

            <h2 class="mb-sm mt-sm"><strong>Contacteer</strong> Ons</h2>
            <p>Uw vraag is goed ontvangen, wij nemen zo snel mogelijk contact met u!</p>


        </div>
        <div class="col-md-6">

            <h4 class="heading-primary mt-lg"><strong>Ligging</strong></h4>
            <p>Onze burelen zijn gelegen in het centrum van Drongen, vlot bereikbaar via de autosnelweg E40 afrit 13 - Drongen.</p>

            <hr>

            <h4 class="heading-primary">Ons <strong>Kantoor</strong></h4>
            <ul class="list list-icons list-icons-style-3 mt-xlg">
                <li><i class="fa fa-map-marker"></i> <strong>Adres:</strong> Klaverdries 53 , 9031 Drongen</li>
                <li><i class="fa fa-phone"></i> <strong>Telefoon:</strong> +32 (0)9 216 49 50</li>
                <li><i class="fa fa-envelope"></i> <strong>Email:</strong> <a href="mailto:info@bouwenconstructie.be">info@bouwenconstructie.be</a></li>
            </ul>

            <hr>

            <h4 class="heading-primary"><strong>Openingsuren</strong></h4>
            <ul class="list list-icons list-dark mt-xlg">
                <li><i class="fa fa-clock-o"></i> Maandag tot Vrijdag van 8u30 tot 12u30 en van 13u30 tot 17u30</li>
                @*<li><i class="fa fa-clock-o"></i> Saturday - 9am to 2pm</li>
                <li><i class="fa fa-clock-o"></i> Sunday - Closed</li>*@
            </ul>

        </div>

    </div>

    </div>

@section scripts
<script src="https://www.google.com/recaptcha/api.js" async defer></script>
<script src="https://maps.googleapis.com/maps/api/js?key=AIzaSyBixojVqE0nNXAPAjgQ9Q5Gnvk5K4zEcLM"></script>
<script>
            function onSubmit(token) {
                document.getElementById("FormMail").submit();
            }
			/*
			Map Settings

				Find the Latitude and Longitude of your address:
					- http://universimmedia.pagesperso-orange.fr/geo/loc.htm
					- http://www.findlatitudeandlongitude.com/find-address-from-latitude-and-longitude/

			*/
            var icon = {
                url: "http://www.groupln.be/content/img/icons/map-marker.gif", // url
                scaledSize: new google.maps.Size(29, 43), // scaled size
                origin: new google.maps.Point(0, 0), // origin
                anchor: new google.maps.Point(14.5, 40) // anchor
            };
			// Map Markers
			var mapMarkers = [{
				address: "Klaverdries 53, 9031 Drongen",
				html: "<strong>BCO</strong><br>Klaverdries 53, Drongen",
				icon: icon,
				popup: true
			}];


			var address = 'Klaverdries 53, 9031 Drongen';

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
    '<h5 class="mb-xs">BCO</h5>' +
    'Klaverdries 53<br/>9031 Drongen' +
    '</div>';

			var infowindow = new google.maps.InfoWindow({
			    content: contentString
			});
			
			geocoder.geocode({
			    'address': address
			},
			function (results, status) {
			    if (status == google.maps.GeocoderStatus.OK) {
			        marker = new google.maps.Marker({
			            position: results[0].geometry.location,
			            title:'BCO',
			            popup: false,
			            icon: icon,
			            address:'Klaverdries 53, 9031 Drongen',
			            map: map

			        });
			        map.setCenter(results[0].geometry.location);
			        google.maps.event.addListener(marker, 'click', function () {
			            infowindow.open(map, marker);
			        });
			        infowindow.open(map, marker);
			    }
			});


			$(window).load(function () {
			    //Berichtencentrum
               @If Not TempData("Message") Is Nothing Then
              @<text>

               new PNotify({
                   title: '@TempData("MessageTitle")',
                   text: '@TempData("Message")',
                   type: '@TempData("MessageType")'
               });
			    </text>      End If
			});

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