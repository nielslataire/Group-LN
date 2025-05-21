@MOdeltype wwwcopro.MailModel 
@Code
    ViewData("Title") = "Group LN - Contacteer ons"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
<section class="page-header page-header-light">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <ul class="breadcrumb">
                    <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
                    <li class="active">Contact</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <h1>Contact</h1>
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
            @Using Html.BeginForm("Send", "Contact", FormMethod.Post, New With {.id = "FormMail", .class = "form-horizontal"})
                @<text>
                    <div class="row">
                        <div class="form-group">
                            <div class="col-md-6">
                                <label>Uw naam *</label>
                                @Html.TextBoxFor(Function(m) m.ContactName, New With {.class = "form-control"})
                            </div>
                            <div class="col-md-6">
                                <label>Uw email adres *</label>
                                @Html.TextBoxFor(Function(m) m.EmailTo, New With {.class = "form-control"})
                            </div>
                           </div>
                    </div>
                    <div class="row">
                        <div class="form-group">
                            <div class="col-md-12">
                                <label>Uw telefoonnummer/GSM</label>
                                @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control"})
                                </div>
                            </div>
                    </div>
                            <div class="row">
                                <div class="form-group">
                                    <div class="col-md-12">
                                        <label>Onderwerp</label>
                                        @Html.TextBoxFor(Function(m) m.Title, New With {.class = "form-control"})
                                        @*<input type="text" value="" data-msg-required="Please enter the subject." maxlength="100" class="form-control" name="subject" id="subject" required>*@
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="form-group">
                                    <div class="col-md-12">
                                        <label>Bericht *</label>
                                        @Html.TextAreaFor(Function(m) m.Message, New With {.rows = "10", .id = "message", .class = "form-control"})


                                        @*<textarea maxlength="5000" data-msg-required="Please enter your message." rows="10" class="form-control" name="message" id="message" required></textarea>*@
                                    </div>
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col">
                                    <div class="g-recaptcha" data-sitekey="6LfcFIoUAAAAANVEZIKVDzRsrGbRflktDHkaBPrW"></div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <button class="btn btn-primary btn-lg mb-xlg g-recaptch" data-sitekey="6Lc7Ym0UAAAAAA_D2oTuEqUSZilBdJYgdRVbtISx" data-callback='onSubmit'>Verstuur bericht</button>
                                </div>
                            </div>
                </text>
            End Using


        </div>
        <div class="col-md-6">

            <h4 class="heading-primary mt-lg"><strong>Ligging</strong></h4>
            <p>Onze burelen zijn gelegen in het centrum van Drongen, vlot bereikbaar via de autosnelweg E40 afrit 13 - Drongen.</p>

            <hr>

            <h4 class="heading-primary">Ons <strong>Kantoor</strong></h4>
            <ul class="list list-icons list-icons-style-3 mt-xlg">
                <li><i class="fa fa-map-marker"></i> <strong>Adres:</strong> Klaverdries 53 , 9031 Drongen</li>
                <li><i class="fa fa-phone"></i> <strong>Telefoon:</strong> +32 (0)9 223 03 27</li>
                <li><i class="fa fa-envelope"></i> <strong>Email:</strong> <a href="mailto:info@groupln.be">info@groupln.be</a></li>
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

			// Map Markers
			var mapMarkers = [{
				address: "Klaverdries 53, 9031 Drongen",
				html: "<strong>Group LN</strong><br>Klaverdries 53, Drongen",
				icon: {
					image: "~content/img/pin.png",
					iconsize: [26, 46],
					iconanchor: [12, 46]
				},
				popup: true
			}];

			// Map Initial Location
			var initLatitude = 51.0524766;
			var initLongitude = 3.6569679;

			// Map Extended Settings
			var mapSettings = {
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
			    markers: mapMarkers,
			    latitude: initLatitude,
			    longitude: initLongitude,
			    zoom: 15
              
			};

			var map = $("#googlemaps").gMap(mapSettings);

			// Map Center At
			var mapCenterAt = function(options, e) {
				e.preventDefault();
				$("#googlemaps").gMap("centerAt", options);
			}
			$(window).load(function () {
			    //Berichtencentrum
               @If Not TempData("Message") Is Nothing Then
              @<text>

               new PNotify({
                   title: '@TempData("MessageTitle")',
                   text: '@TempData("Message")',
                   type: '@TempData("MessageType")'
               });
			    </text>
			    End If
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