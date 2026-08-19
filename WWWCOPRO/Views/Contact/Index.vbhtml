@ModelType wwwcopro.MailModel
@Imports System.Web.Mvc
@Code
    ViewData("Title") = "Contacteer ons | Group LN"
    ViewData("MetaDescription") = "Neem contact op met Group LN voor vragen over een project, of het aanbieden van een grond of pand. Wij antwoorden snel en persoonlijk."
    Layout = "~/Views/Shared/_Layout.vbhtml"

    ' Logo als base64 voor de custom kaart-pin (een SVG die als icoon-URL dient mag geen
    ' extern beeld inladen, dus wordt het logo hier zelf mee ingebed)
    Dim glnLogoBase64 As String = ""
    Try
        Dim logoPath = Server.MapPath("~/Content/img/logo.png")
        glnLogoBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(logoPath))
    Catch
    End Try
End Code
@section PageStyle
    <link rel="stylesheet" href="~/Content/contact.css" />
End Section

<section class="contact-page-header">
    <div class="container reveal">
        <ul class="breadcrumb">
            <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
            <li class="active">Contact</li>
        </ul>
        <h1>Contacteer ons</h1>
        <p class="page-subtitle">Heeft u een vraag over een project of wil u een grond laten ontwikkelen? Laat het ons weten.</p>
    </div>
</section>

<div class="contact-page-wrap">
<section class="contact-section">
    <div class="contact-info-panel reveal">
        <h2 class="contact-info-title">Onze locatie</h2>
        <div class="contact-info-list">
            <a class="contact-info-item" href="https://www.google.com/maps/search/?api=1&query=Klaverdries+53%2C+9031+Drongen" target="_blank" rel="noopener">
                <span class="contact-info-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
                </span>
                <div>
                    <div class="contact-info-label">Adres</div>
                    <div class="contact-info-value">Klaverdries 53<br />9031 Drongen, België</div>
                </div>
            </a>
            <a class="contact-info-item" href="tel:+3292164950">
                <span class="contact-info-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.61 3.4 2 2 0 0 1 3.6 1.21h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L7.91 8.81a16 16 0 0 0 6 6l.91-.91a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 21.73 16z"/></svg>
                </span>
                <div>
                    <div class="contact-info-label">Telefoon</div>
                    <div class="contact-info-value">+32 (0)9 216 49 50</div>
                </div>
            </a>
            <a class="contact-info-item" href="mailto:info@groupln.be">
                <span class="contact-info-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>
                </span>
                <div>
                    <div class="contact-info-label">E-mail</div>
                    <div class="contact-info-value">info@groupln.be</div>
                </div>
            </a>
        </div>

        <ul class="contact-social">
            <li><a href="https://www.instagram.com/group.ln/" target="_blank" rel="noopener" aria-label="Instagram"><i class="bx bxl-instagram"></i></a></li>
            <li><a href="https://www.linkedin.com/company/group-ln" target="_blank" rel="noopener" aria-label="LinkedIn"><i class="bx bxl-linkedin"></i></a></li>
            <li><a href="https://www.facebook.com/GROUPLN" target="_blank" rel="noopener" aria-label="Facebook"><i class="bx bxl-facebook"></i></a></li>
            <li><a href="@("https://www.tiktok.com/@groupln_")" target="_blank" rel="noopener" aria-label="TikTok"><i class="bx bxl-tiktok"></i></a></li>
            <li><a href="@("https://www.youtube.com/@Group_LN")" target="_blank" rel="noopener" aria-label="YouTube"><i class="bx bxl-youtube"></i></a></li>
        </ul>

        <div class="contact-map" id="googlemaps"></div>
    </div>

    <div class="contact-form-panel">
        <div class="contact-form-inner reveal">
            @If ViewBag.SubmitSuccess IsNot Nothing AndAlso ViewBag.SubmitSuccess Then
                @<div class="contact-success">
                    <div class="contact-success-check">
                        <svg viewBox="0 0 64 64" width="56" height="56">
                            <circle class="contact-success-check-circle" cx="32" cy="32" r="28" fill="none" stroke-width="3" />
                            <path class="contact-success-check-mark" fill="none" stroke-width="4" d="M20 33l8 8 16-17" />
                        </svg>
                    </div>
                    <p class="contact-form-sub">
                        Bericht ontvangen@(If(Not String.IsNullOrWhiteSpace(CStr(ViewBag.SubmittedNaam)), ", " & ViewBag.SubmittedNaam, "")), helemaal volgens plan! We werken het verder uit en laten iets horen op @ViewBag.SubmittedEmail.
                    </p>
                    <a href="@Url.Action("Index", "Contact")" class="contact-success-link">&larr; Nog een bericht versturen</a>
                </div>
            Else
                @<text>
                    <h2 class="contact-form-title">Laten we samen iets bouwen.</h2>
                    <p class="contact-form-sub">Vertel ons kort waar u aan denkt — of het nu een concrete vraag is over een lopend project, of een grond die u wil laten ontwikkelen.</p>

                    @Using Html.BeginForm("Send", "Contact", FormMethod.Post, New With {.id = "contactForm", .autocomplete = "on"})
                        @<text>
                            @Html.AntiForgeryToken()

                            @* Honeypot — enkel bots vullen dit verborgen veld in *@
                            <div style="position:absolute;left:-9999px;top:-9999px;opacity:0;" aria-hidden="true">
                                <input type="text" name="website_url" tabindex="-1" autocomplete="off" />
                            </div>
                            <input type="hidden" name="g-recaptcha-response" id="gRecaptchaResponse" />

                            <div class="contact-form-grid">
                                <div class="contact-field">
                                    <label for="Voornaam">Voornaam<span class="req">*</span></label>
                                    @Html.TextBoxFor(Function(m) m.Voornaam, New With {.class = "form-control", .autocomplete = "given-name", .required = "required"})
                                    @Html.ValidationMessageFor(Function(m) m.Voornaam, "", New With {.class = "contact-field-error"})
                                </div>
                                <div class="contact-field">
                                    <label for="Achternaam">Achternaam<span class="req">*</span></label>
                                    @Html.TextBoxFor(Function(m) m.Achternaam, New With {.class = "form-control", .autocomplete = "family-name", .required = "required"})
                                    @Html.ValidationMessageFor(Function(m) m.Achternaam, "", New With {.class = "contact-field-error"})
                                </div>
                                <div class="contact-field">
                                    <label for="EmailTo">E-mailadres<span class="req">*</span></label>
                                    @Html.TextBoxFor(Function(m) m.EmailTo, New With {.class = "form-control", .type = "email", .autocomplete = "email", .required = "required"})
                                    @Html.ValidationMessageFor(Function(m) m.EmailTo, "", New With {.class = "contact-field-error"})
                                </div>
                                <div class="contact-field">
                                    <label for="Phone">Telefoonnummer</label>
                                    @Html.TextBoxFor(Function(m) m.Phone, New With {.class = "form-control", .autocomplete = "tel"})
                                    @Html.ValidationMessageFor(Function(m) m.Phone, "", New With {.class = "contact-field-error"})
                                </div>
                                <div class="contact-field contact-field-full">
                                    <label for="Title">Onderwerp<span class="req">*</span></label>
                                    @Html.DropDownListFor(Function(m) m.Title, CType(ViewBag.OnderwerpOpties, List(Of SelectListItem)), "Kies een onderwerp", New With {.class = "form-control", .required = "required"})
                                    @Html.ValidationMessageFor(Function(m) m.Title, "", New With {.class = "contact-field-error"})
                                </div>
                                <div class="contact-field contact-field-full">
                                    <label for="Message">Uw bericht<span class="req">*</span></label>
                                    @Html.TextAreaFor(Function(m) m.Message, New With {.rows = "5", .class = "form-control", .placeholder = "Vertel ons kort waarmee we u kunnen helpen", .required = "required"})
                                    @Html.ValidationMessageFor(Function(m) m.Message, "", New With {.class = "contact-field-error"})
                                </div>
                            </div>

                            <div class="contact-privacy">
                                @Html.CheckBoxFor(Function(m) m.PrivacyAkkoord)
                                <label for="PrivacyAkkoord">Ik ga akkoord dat Group LN mijn gegevens gebruikt om mijn vraag te beantwoorden, conform het <a href="#">privacybeleid</a>.</label>
                            </div>
                            @Html.ValidationMessageFor(Function(m) m.PrivacyAkkoord, "", New With {.class = "contact-field-error"})

                            <button type="submit" class="contact-submit-btn">Bericht versturen <i class="fa fa-arrow-right"></i></button>
                        </text>
                    End Using
                </text>
            End If
        </div>
    </div>
</section>
</div>

@section scripts
    <script src="https://www.google.com/recaptcha/api.js?render=@ViewBag.ReCaptchaSiteKey"></script>
    <script src="https://maps.googleapis.com/maps/api/js?key=AIzaSyBixojVqE0nNXAPAjgQ9Q5Gnvk5K4zEcLM"></script>
    <script>
        // reCAPTCHA v3-token ophalen vlak vóór het versturen (onzichtbaar voor de gebruiker)
        (function () {
            var form = document.getElementById('contactForm');
            var recaptchaSiteKey = '@ViewBag.ReCaptchaSiteKey';
            var recaptchaAction = '@ViewBag.ReCaptchaAction';
            var recaptchaInput = document.getElementById('gRecaptchaResponse');
            if (!form || !recaptchaSiteKey || typeof grecaptcha === 'undefined') return;

            form.addEventListener('submit', function (e) {
                if (form.dataset.captchaReady === '1') return;
                e.preventDefault();
                grecaptcha.ready(function () {
                    grecaptcha.execute(recaptchaSiteKey, { action: recaptchaAction || 'contact' }).then(function (token) {
                        recaptchaInput.value = token;
                        form.dataset.captchaReady = '1';
                        form.submit();
                    }, function () {
                        form.dataset.captchaReady = '1';
                        form.submit();
                    });
                });
            });
        })();

        // Custom pin: groene marker met het Group LN-logo erin verwerkt (base64 ingebed,
        // want een SVG die als icoon-URL dient mag geen extern beeld inladen)
        var glnLogoBase64 = "data:image/png;base64,@Html.Raw(glnLogoBase64)";
        var glnPinSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="40" height="50" viewBox="0 0 40 50">' +
            '<polygon points="8,28 32,28 20,48" fill="#00532D"/>' +
            '<circle cx="20" cy="18" r="17" fill="#00532D"/>' +
            '<circle cx="20" cy="18" r="13" fill="#00532D"/>' +
            '<clipPath id="glnLogoClip"><circle cx="20" cy="18" r="11"/></clipPath>' +
            '<image href="' + glnLogoBase64 + '" x="7" y="5" width="26" height="26" clip-path="url(#glnLogoClip)" preserveAspectRatio="xMidYMid slice"/>' +
            '</svg>';
        var glnPinIconUrl = 'data:image/svg+xml;base64,' + btoa(glnPinSvg);

        // Kaartstijl in de kleuren van de site (groen/mist-tinten i.p.v. standaard Google-kleuren),
        // zonder de standaard Google-iconen voor omliggende zaken (Aldi, Pizzahof, ...)
        var mapStyles = [
            { elementType: "geometry", stylers: [{ color: "#f2f5ef" }] },
            { elementType: "labels.text.fill", stylers: [{ color: "#5a6b58" }] },
            { elementType: "labels.text.stroke", stylers: [{ color: "#f2f5ef" }] },
            { featureType: "administrative", elementType: "geometry", stylers: [{ color: "#c9d3c4" }] },
            { featureType: "poi", stylers: [{ visibility: "off" }] },
            { featureType: "road", elementType: "geometry", stylers: [{ color: "#ffffff" }] },
            { featureType: "road", elementType: "geometry.stroke", stylers: [{ color: "#e2e7de" }] },
            { featureType: "road", elementType: "labels.icon", stylers: [{ visibility: "off" }] },
            { featureType: "road.highway", elementType: "geometry", stylers: [{ color: "#eef1ec" }] },
            { featureType: "transit", stylers: [{ visibility: "off" }] },
            { featureType: "water", elementType: "geometry", stylers: [{ color: "#a9c9b8" }] }
        ];

        $(window).load(function () {
            var mapEl = document.getElementById('googlemaps');
            if (mapEl && typeof google !== 'undefined') {
                var map = new google.maps.Map(mapEl, {
                    center: { lat: 51.0524766, lng: 3.6569679 },
                    zoom: 15,
                    draggable: !$.browser.mobile,
                    panControl: true,
                    zoomControl: true,
                    mapTypeControl: false,
                    scaleControl: false,
                    streetViewControl: false,
                    fullscreenControl: false,
                    scrollwheel: false,
                    styles: mapStyles
                });

                var geocoder = new google.maps.Geocoder();
                geocoder.geocode({ address: "Klaverdries 53, 9031 Drongen" }, function (results, status) {
                    if (status !== 'OK' || !results || !results.length) return;

                    var position = results[0].geometry.location;
                    map.setCenter(position);

                    new google.maps.Marker({
                        map: map,
                        position: position,
                        icon: {
                            url: glnPinIconUrl,
                            scaledSize: new google.maps.Size(40, 50),
                            anchor: new google.maps.Point(20, 48)
                        }
                    });
                });
            }

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
