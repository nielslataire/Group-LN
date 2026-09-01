@ModelType WWWCOPRO.Models.Vacatures.VacatureModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    Dim kicker As String = If(Not String.IsNullOrWhiteSpace(Model.Categorie), "VACATURE — " & Model.Categorie.ToUpperInvariant(), "VACATURE")
End Code

@section PageMeta
    @Code
        Dim _ser As New System.Web.Script.Serialization.JavaScriptSerializer()
        Dim _canonicalSlug As String = If(Model.Slug, "").ToLowerInvariant()
        Dim _canonicalUrl As String = "https://www.groupln.be/vacatures/" & _canonicalSlug
        Dim _jobDescription As String = If(Not String.IsNullOrWhiteSpace(Model.Beschrijving), Model.Beschrijving, Model.KorteBeschrijving)

        ' Google/schema.org verwachten een van de vaste JobPosting-employmentType-waarden — het
        ' vrije-tekstveld Dienstverband wordt hier best-effort gemapt; bij een onherkende waarde
        ' laten we het veld gewoon weg i.p.v. iets te verzinnen.
        Dim _employmentType As String = Nothing
        If Not String.IsNullOrWhiteSpace(Model.Dienstverband) Then
            Dim _dv As String = Model.Dienstverband.ToLowerInvariant()
            If _dv.Contains("voltijds") OrElse _dv.Contains("full") Then
                _employmentType = "FULL_TIME"
            ElseIf _dv.Contains("deeltijds") OrElse _dv.Contains("part") Then
                _employmentType = "PART_TIME"
            ElseIf _dv.Contains("freelance") OrElse _dv.Contains("zelfstandig") OrElse _dv.Contains("contractor") Then
                _employmentType = "CONTRACTOR"
            ElseIf _dv.Contains("interim") OrElse _dv.Contains("tijdelijk") OrElse _dv.Contains("temporary") Then
                _employmentType = "TEMPORARY"
            ElseIf _dv.Contains("stage") OrElse _dv.Contains("intern") Then
                _employmentType = "INTERN"
            End If
        End If
        Dim _employmentTypeField As String = If(Not String.IsNullOrEmpty(_employmentType), """employmentType"": """ & _employmentType & """," & Environment.NewLine & "  ", String.Empty)

        Dim _jobPostingJson As String = "{" & Environment.NewLine &
            "  ""@context"": ""https://schema.org""," & Environment.NewLine &
            "  ""@type"": ""JobPosting""," & Environment.NewLine &
            "  ""title"": " & _ser.Serialize(Model.Titel) & "," & Environment.NewLine &
            "  ""description"": " & _ser.Serialize(_jobDescription) & "," & Environment.NewLine &
            "  ""datePosted"": """ & Model.AangemaaktOp.ToString("yyyy-MM-dd") & """," & Environment.NewLine &
            "  " & _employmentTypeField &
            "  ""identifier"": {" & Environment.NewLine &
            "    ""@type"": ""PropertyValue""," & Environment.NewLine &
            "    ""name"": ""Group LN""," & Environment.NewLine &
            "    ""value"": """ & Model.ID & """" & Environment.NewLine &
            "  }," & Environment.NewLine &
            "  ""hiringOrganization"": {" & Environment.NewLine &
            "    ""@type"": ""Organization""," & Environment.NewLine &
            "    ""name"": ""Group LN""," & Environment.NewLine &
            "    ""sameAs"": ""https://www.groupln.be""," & Environment.NewLine &
            "    ""logo"": ""https://www.groupln.be/Content/img/logoimg.jpg""" & Environment.NewLine &
            "  }," & Environment.NewLine &
            "  ""jobLocation"": {" & Environment.NewLine &
            "    ""@type"": ""Place""," & Environment.NewLine &
            "    ""address"": {" & Environment.NewLine &
            "      ""@type"": ""PostalAddress""," & Environment.NewLine &
            "      ""addressLocality"": " & _ser.Serialize(If(Model.Locatie, "")) & "," & Environment.NewLine &
            "      ""addressCountry"": ""BE""" & Environment.NewLine &
            "    }" & Environment.NewLine &
            "  }" & Environment.NewLine &
            "}"

        Dim _breadcrumbJson As String = "{" & Environment.NewLine &
            "  ""@context"": ""https://schema.org""," & Environment.NewLine &
            "  ""@type"": ""BreadcrumbList""," & Environment.NewLine &
            "  ""itemListElement"": [" & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 1, ""name"": ""Home"", ""item"": ""https://www.groupln.be/"" }," & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 2, ""name"": ""Vacatures"", ""item"": ""https://www.groupln.be/vacatures"" }," & Environment.NewLine &
            "    { ""@type"": ""ListItem"", ""position"": 3, ""name"": " & _ser.Serialize(Model.Titel) & ", ""item"": """ & _canonicalUrl & """ }" & Environment.NewLine &
            "  ]" & Environment.NewLine &
            "}"
    End Code
    <script type="application/ld+json">@Html.Raw(_jobPostingJson)</script>
    <script type="application/ld+json">@Html.Raw(_breadcrumbJson)</script>
End Section

@section PageStyle
    <link rel="stylesheet" href="~/Content/contact.css" />
    <link rel="stylesheet" href="~/Content/vacatures.css" />
End Section

@* ── Voorvertoning banner ── *@
@If ViewData("IsVoorvertoning") IsNot Nothing Then
    @<div style="background:#fef3c7;border-bottom:2px solid #d97706;padding:12px 0;position:sticky;top:0;z-index:9999;">
        <div class="container">
            <div style="display:flex;align-items:center;gap:10px;font-size:13px;font-weight:600;color:#92400e;">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="flex-shrink:0;"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                VOORVERTONING &mdash; Deze vacature is nog niet gepubliceerd. Enkel zichtbaar via deze beveiligde link.
            </div>
        </div>
    </div>
End If

<section class="vac-detail-header">
    <div class="container">
        <ul class="breadcrumb">
            <li><a href="@(Url.Action("Index", "Home"))">Home</a></li>
            <li><a href="@Url.RouteUrl("Vacatures")">Vacatures</a></li>
            <li class="active">@Model.Titel</li>
        </ul>
        <span class="vac-detail-kicker">@kicker</span>
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
        </div>
        @If Not String.IsNullOrWhiteSpace(Model.KorteBeschrijving) Then
            @<p class="vac-detail-intro">@Model.KorteBeschrijving</p>
        End If
        <div class="vac-detail-hero-acties">
            <a href="#solliciteren" class="vac-detail-apply-btn">
                Solliciteer nu <i class="fa fa-arrow-right"></i>
            </a>
            <a href="@Url.RouteUrl("Vacatures")" class="vac-detail-outline-btn">Alle vacatures</a>
        </div>
    </div>
</section>

<div class="vac-detail-wrap">
    <div class="container">
        <div class="vac-detail-layout">
            <div class="vac-detail-body reveal">
                @If Not String.IsNullOrWhiteSpace(Model.Beschrijving) Then
                    @<div class="vac-detail-section">
                        <h2>@Model.Titel bij Group LN</h2>
                        @Html.Raw(Model.Beschrijving)
                    </div>
                ElseIf Not String.IsNullOrWhiteSpace(Model.KorteBeschrijving) Then
                    @<div class="vac-detail-section">
                        <h2>@Model.Titel bij Group LN</h2>
                        <p>@Model.KorteBeschrijving</p>
                    </div>
                End If

                @If Model.Takenpakket.Any() Then
                    @<div class="vac-detail-section">
                        <h2>Zo ziet jou werkdag eruit</h2>
                        <ul class="vac-detail-check-list">
                            @For Each taak In Model.Takenpakket
                                @<li>@taak</li>
                            Next
                        </ul>
                    </div>
                End If

                @If Model.MustHaves.Any() OrElse Model.MooiMeegenomen.Any() Then
                    @<div class="vac-detail-section">
                        <h2>Wie zoeken we</h2>
                        <div class="vac-detail-must-mooi-grid">
                            @If Model.MustHaves.Any() Then
                                @<div>
                                    <h3>Must-haves</h3>
                                    <ul>
                                        @For Each punt In Model.MustHaves
                                            @<li>@punt</li>
                                        Next
                                    </ul>
                                </div>
                            End If
                            @If Model.MooiMeegenomen.Any() Then
                                @<div>
                                    <h3>Mooi meegenomen</h3>
                                    <ul>
                                        @For Each punt In Model.MooiMeegenomen
                                            @<li>@punt</li>
                                        Next
                                    </ul>
                                </div>
                            End If
                        </div>
                    </div>
                End If

                @If Model.Voordelen.Any() Then
                    @<div class="vac-detail-section">
                        <h2>Onze voordelen voor jou</h2>
                        <ul class="vac-detail-check-list">
                            @For Each voordeel In Model.Voordelen
                                @<li>@voordeel</li>
                            Next
                        </ul>
                    </div>
                End If

                @If Model.SollicitatieStappen.Any() Then
                    @<div class="vac-detail-section">
                        <h2>Zo verloopt jouw sollicitatie</h2>
                        <div class="vac-detail-stappen">
                            @For i As Integer = 0 To Model.SollicitatieStappen.Count - 1
                                Dim stap = Model.SollicitatieStappen(i)
                                @<div class="vac-detail-stap">
                                    <span class="vac-detail-stap-nummer">@((i + 1).ToString("00"))</span>
                                    <div class="vac-detail-stap-inhoud">
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
            </div>

            <aside class="vac-detail-sidebar reveal">
                @If Not String.IsNullOrWhiteSpace(Model.VideoBestand) Then
                    @<div class="vac-detail-video" data-video>
                        @If Not String.IsNullOrWhiteSpace(Model.VideoPosterBestand) Then
                            @<video src="@Model.VideoBestand" poster="@Model.VideoPosterBestand" preload="metadata" playsinline></video>
                        Else
                            @<video src="@Model.VideoBestand" preload="metadata" playsinline></video>
                        End If
                        <button type="button" class="vac-detail-video-play" aria-label="Afspelen of pauzeren">
                            <svg class="vac-detail-video-ico vac-detail-video-ico--play" viewBox="0 0 24 24" width="24" height="24" fill="currentColor" aria-hidden="true"><path d="M8 5v14l11-7z"/></svg>
                            <svg class="vac-detail-video-ico vac-detail-video-ico--pause" viewBox="0 0 24 24" width="22" height="22" fill="currentColor" aria-hidden="true"><path d="M6 5h4v14H6zm8 0h4v14h-4z"/></svg>
                        </button>
                    </div>
                End If
                <div class="vac-detail-sidebar-card">
                    <span class="vac-detail-sidebar-title">Vacature in een oogopslag</span>
                    @If Not String.IsNullOrWhiteSpace(Model.Locatie) Then
                        @<div class="vac-detail-sidebar-row">
                            <span>Locatie</span>
                            <strong>@Model.Locatie</strong>
                        </div>
                    End If
                    @If Not String.IsNullOrWhiteSpace(Model.Dienstverband) Then
                        @<div class="vac-detail-sidebar-row">
                            <span>Tewerkstelling</span>
                            <strong>@Model.Dienstverband</strong>
                        </div>
                    End If
                    @If Not String.IsNullOrWhiteSpace(Model.Opleiding) Then
                        @<div class="vac-detail-sidebar-row">
                            <span>Niveau</span>
                            <strong>@Model.Opleiding</strong>
                        </div>
                    End If
                    @If Not String.IsNullOrWhiteSpace(Model.Start) Then
                        @<div class="vac-detail-sidebar-row">
                            <span>Start</span>
                            <strong>@Model.Start</strong>
                        </div>
                    End If
                </div>

                <div class="vac-detail-sidebar-cta">
                    <h3>Interesse gewekt?</h3>
                    <p>Solliciteren duurt minder dan 5 minuten. We antwoorden binnen de week.</p>
                    <a href="#solliciteren" class="vac-detail-sidebar-cta-btn">Solliciteer nu</a>
                </div>

                <div class="vac-detail-sidebar-contact">
                    <span class="vac-detail-sidebar-contact-label">Vragen over deze vacature?</span>
                    <a href="mailto:info@groupln.be">info@groupln.be</a>
                </div>
            </aside>
        </div>
    </div>
</div>

<section class="vac-solliciteer-section reveal" id="solliciteren">
    <div class="container">
        <div class="vac-detail-layout">
        <div class="vac-solliciteer-inner">
            <div id="solliciteerFormWrap">
                <h2 class="contact-form-title">Solliciteer voor deze functie</h2>
                <p class="contact-form-sub">Vul het formulier in en stuur je cv mee. We nemen binnen de week contact met je op.</p>

                <div class="vac-solliciteer-error-banner" id="solliciteerErrorBanner" style="display:none;"></div>

                <div id="solliciteerFormFieldsWrap">
                @Using Html.BeginForm("Solliciteren", "Vacatures", New With {.slug = Model.Slug}, FormMethod.Post, New With {.id = "solliciterenForm", .enctype = "multipart/form-data"})
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
                                <input type="text" id="Voornaam" name="Voornaam" class="form-control" autocomplete="given-name" required="required" />
                                <span class="contact-field-error" data-valmsg-for="Voornaam"></span>
                            </div>
                            <div class="contact-field">
                                <label for="Achternaam">Achternaam<span class="req">*</span></label>
                                <input type="text" id="Achternaam" name="Achternaam" class="form-control" autocomplete="family-name" required="required" />
                                <span class="contact-field-error" data-valmsg-for="Achternaam"></span>
                            </div>
                            <div class="contact-field">
                                <label for="Email">E-mailadres<span class="req">*</span></label>
                                <input type="email" id="Email" name="Email" class="form-control" autocomplete="email" required="required" />
                                <span class="contact-field-error" data-valmsg-for="Email"></span>
                            </div>
                            <div class="contact-field">
                                <label for="Telefoon">Telefoonnummer<span class="req">*</span></label>
                                <input type="tel" id="Telefoon" name="Telefoon" class="form-control" autocomplete="tel" required="required" />
                                <span class="contact-field-error" data-valmsg-for="Telefoon"></span>
                            </div>
                            <div class="contact-field contact-field-full">
                                <label for="Motivatie">Motivatie</label>
                                <textarea id="Motivatie" name="Motivatie" rows="4" class="form-control" placeholder="Waarom past deze job bij jou? (optioneel, maar we lezen het graag)"></textarea>
                                <span class="contact-field-error" data-valmsg-for="Motivatie"></span>
                            </div>
                            <div class="contact-field contact-field-full">
                                <label for="cvBestandInput">Cv<span class="req">*</span></label>
                                <label class="vac-cv-dropzone" for="cvBestandInput" id="cvDropzoneLabel">
                                    <span class="vac-cv-dropzone-icon">
                                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
                                    </span>
                                    <span class="vac-cv-dropzone-text">
                                        <strong id="cvDropzoneLabelText">Upload je cv</strong>
                                        <small>PDF of Word, max. 5MB</small>
                                    </span>
                                </label>
                                <input type="file" id="cvBestandInput" name="cvBestand" accept=".pdf,.doc,.docx" required="required" style="position:absolute;left:-9999px;opacity:0;" />
                                <span class="contact-field-error" data-valmsg-for="CvBestand"></span>
                            </div>
                        </div>

                        <div class="contact-privacy">
                            <input type="checkbox" id="PrivacyAkkoord" name="PrivacyAkkoord" value="true" />
                            <label for="PrivacyAkkoord">Ik ga akkoord dat Group LN mijn gegevens gebruikt om mijn sollicitatie te behandelen, conform het <a href="@Url.Action("Privacybeleid", "Legal")" target="_blank" rel="noopener">privacybeleid</a>.</label>
                        </div>
                        <span class="contact-field-error" data-valmsg-for="PrivacyAkkoord"></span>

                        <button type="submit" class="contact-submit-btn vac-solliciteer-submit-btn">Sollicitatie versturen <i class="fa fa-arrow-right"></i></button>
                    </text>
                End Using
                </div>

                <div id="solliciteerLoading" class="vac-solliciteer-loading" style="display:none;">
                    <div class="vac-bricks-anim">
                        <span></span><span></span><span></span><span></span>
                    </div>
                    <p>Bezig met bouwen aan jouw kans... één moment</p>
                </div>

                <div id="solliciteerSuccess" class="contact-success" style="display:none;">
                    <div class="contact-success-check">
                        <svg viewBox="0 0 64 64" width="56" height="56">
                            <circle class="contact-success-check-circle" cx="32" cy="32" r="28" fill="none" stroke-width="3" />
                            <path class="contact-success-check-mark" fill="none" stroke-width="4" d="M20 33l8 8 16-17" />
                        </svg>
                    </div>
                    <p class="contact-form-sub" id="solliciteerSuccessText"></p>
                </div>
            </div>
        </div>
        </div>
    </div>
</section>

@If Model.AndereVacatures.Any() Then
    @<section class="vac-detail-andere-section reveal">
        <div class="container">
            <h2>Andere vacatures</h2>
            <div class="vac-detail-andere-grid">
                @For Each andere In Model.AndereVacatures
                    @<a href="@Url.RouteUrl("VacatureDetail", New With {.slug = andere.Slug})" class="vac-detail-other-card">
                        @If Not String.IsNullOrWhiteSpace(andere.Categorie) Then
                            @<span class="vac-detail-other-categorie">@andere.Categorie</span>
                        End If
                        <h3>@andere.Titel</h3>
                        <div class="vac-detail-other-meta">
                            @If Not String.IsNullOrWhiteSpace(andere.Locatie) Then
                                @<span>@andere.Locatie</span>
                            End If
                            @If Not String.IsNullOrWhiteSpace(andere.Dienstverband) Then
                                @<span>@andere.Dienstverband</span>
                            End If
                        </div>
                    </a>
                Next
            </div>
        </div>
    </section>
End If

@section scripts
    <script src="https://www.google.com/recaptcha/api.js?render=@ViewBag.ReCaptchaSiteKey"></script>
    <script>
        $(document).ready(function () {
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });

        // Sollicitatieformulier: AJAX-verzending met laad-/succes-/foutstatus, geen paginaherlaad
        // (zo blijven ingevulde velden en scrollpositie behouden bij een fout).
        (function () {
            var form = document.getElementById('solliciterenForm');
            if (!form) return;

            var recaptchaSiteKey = '@ViewBag.ReCaptchaSiteKey';
            var recaptchaAction = '@ViewBag.ReCaptchaAction';
            var recaptchaInput = document.getElementById('gRecaptchaResponse');

            var fieldsWrap = document.getElementById('solliciteerFormFieldsWrap');
            var loadingEl = document.getElementById('solliciteerLoading');
            var successEl = document.getElementById('solliciteerSuccess');
            var successTextEl = document.getElementById('solliciteerSuccessText');
            var errorBanner = document.getElementById('solliciteerErrorBanner');
            var submitBtn = form.querySelector('.vac-solliciteer-submit-btn');

            function clearFieldErrors() {
                form.querySelectorAll('.contact-field-error[data-valmsg-for]').forEach(function (el) {
                    el.textContent = '';
                });
                form.querySelectorAll('.form-control.vac-field-invalid').forEach(function (el) {
                    el.classList.remove('vac-field-invalid');
                });
            }

            function showValidationErrors(errors) {
                var firstField = null;
                Object.keys(errors || {}).forEach(function (key) {
                    var msgEl = form.querySelector('.contact-field-error[data-valmsg-for="' + key + '"]');
                    if (msgEl) msgEl.textContent = errors[key];
                    var fieldEl = document.getElementById(key === 'CvBestand' ? 'cvBestandInput' : key);
                    if (fieldEl) {
                        fieldEl.classList.add('vac-field-invalid');
                        if (!firstField) firstField = fieldEl;
                    }
                });
                if (firstField) firstField.focus();
            }

            function submitForm() {
                var voornaam = (document.getElementById('Voornaam') || {}).value || '';
                var email = (document.getElementById('Email') || {}).value || '';

                fieldsWrap.style.display = 'none';
                errorBanner.style.display = 'none';
                loadingEl.style.display = '';

                var formData = new FormData(form);

                fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(function (response) { return response.json(); })
                    .then(function (data) {
                        loadingEl.style.display = 'none';
                        if (data && data.success) {
                            successTextEl.textContent = 'De eerste steen is gelegd, ' + voornaam + '! Wij bouwen dit verder af en laten snel iets horen via ' + email + '.';
                            successEl.style.display = '';
                        } else {
                            fieldsWrap.style.display = '';
                            if (submitBtn) submitBtn.disabled = false;
                            clearFieldErrors();
                            if (data && data.errors && Object.keys(data.errors).length > 0) {
                                errorBanner.textContent = 'Oeps, hier mist nog een steentje. Vul de rode velden even aan.';
                                errorBanner.style.display = '';
                                showValidationErrors(data.errors);
                            } else {
                                errorBanner.textContent = (data && data.generalError) || 'Er ging iets mis bij het versturen. Probeer het opnieuw of mail naar info@groupln.be.';
                                errorBanner.style.display = '';
                            }
                        }
                    })
                    .catch(function () {
                        loadingEl.style.display = 'none';
                        fieldsWrap.style.display = '';
                        if (submitBtn) submitBtn.disabled = false;
                        errorBanner.textContent = 'Er ging iets mis bij het versturen. Probeer het opnieuw of mail naar info@groupln.be.';
                        errorBanner.style.display = '';
                    });
            }

            form.addEventListener('submit', function (e) {
                e.preventDefault();
                if (submitBtn) submitBtn.disabled = true;

                if (recaptchaSiteKey && typeof grecaptcha !== 'undefined') {
                    grecaptcha.ready(function () {
                        grecaptcha.execute(recaptchaSiteKey, { action: recaptchaAction || 'sollicitatie' }).then(function (token) {
                            recaptchaInput.value = token;
                            submitForm();
                        }, function () {
                            submitForm();
                        });
                    });
                } else {
                    submitForm();
                }
            });
        })();

        // Cv-dropzone: toont de gekozen bestandsnaam
        (function () {
            var input = document.getElementById('cvBestandInput');
            var labelText = document.getElementById('cvDropzoneLabelText');
            if (!input || !labelText) return;
            input.addEventListener('change', function () {
                if (input.files && input.files.length > 0) {
                    labelText.textContent = input.files[0].name;
                } else {
                    labelText.textContent = 'Upload je cv';
                }
            });
        })();

        @If Not TempData("Message") Is Nothing Then
            @<text>
                new PNotify({
                    title: '@TempData("MessageTitle")',
                    text: '@TempData("Message")',
                    type: '@TempData("MessageType")'
                });
            </text>
        End If

        // Vacaturevideo: eigen centrale knop die play/pauze wisselt. Geen autoplay.
        // Na de eerste start blijft ook de native videobalk beschikbaar (scrubben, volume, fullscreen).
        (function () {
            var wrap = document.querySelector('[data-video]');
            if (!wrap) return;
            var video = wrap.querySelector('video');
            var btn = wrap.querySelector('.vac-detail-video-play');
            if (!video || !btn) return;

            var started = false, hideTimer;

            function toonKnop() {
                wrap.classList.add('show-btn');
                clearTimeout(hideTimer);
                if (!video.paused) {
                    hideTimer = setTimeout(function () { wrap.classList.remove('show-btn'); }, 2000);
                }
            }
            function verbergKnop() {
                clearTimeout(hideTimer);
                wrap.classList.remove('show-btn');
            }

            btn.addEventListener('click', function () {
                if (!started) { started = true; video.controls = true; }
                if (video.paused) {
                    var p = video.play();
                    if (p && typeof p.catch === 'function') { p.catch(function () { }); }
                } else {
                    video.pause();
                }
            });

            wrap.addEventListener('mousemove', toonKnop);
            wrap.addEventListener('mouseleave', verbergKnop);
            wrap.addEventListener('click', toonKnop);

            video.addEventListener('play', function () {
                wrap.classList.add('is-playing');
                verbergKnop();               // meteen verbergen zodra de video speelt
            });
            video.addEventListener('pause', function () {
                wrap.classList.remove('is-playing');
                wrap.classList.add('show-btn');   // gepauzeerd: knop blijft staan
                clearTimeout(hideTimer);
            });
            video.addEventListener('ended', function () {
                wrap.classList.remove('is-playing');
                wrap.classList.add('show-btn');
            });
        })();
    </script>
End Section
