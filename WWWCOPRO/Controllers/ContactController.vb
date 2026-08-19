Imports BO
Imports System.Configuration
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Web.Hosting
Imports System.Web.Mvc
Public Class ContactController
    Inherits System.Web.Mvc.Controller

    Private Const ReCaptchaActionName As String = "contact"
    Private Const ReCaptchaMinimumScore As Double = 0.5

    ' Herkent URL's/domeinnamen in vrije-tekstvelden — de meeste spam-bots proberen een
    ' promotionele link te slijten via naam- of berichtvelden (bv. "diarshop.com").
    Private Shared ReadOnly SpamUrlPattern As New Regex(
        "https?://|www\.|\b[a-z0-9-]+\.(com|net|org|be|nl|shop|info|xyz|biz|club|online|top|site)\b",
        RegexOptions.IgnoreCase Or RegexOptions.Compiled)

    Private Shared ReadOnly OnderwerpOpties As List(Of SelectListItem) = New List(Of SelectListItem) From {
        New SelectListItem With {.Text = "Algemene vraag", .Value = "Algemene vraag"},
        New SelectListItem With {.Text = "Vraag over een project", .Value = "Vraag over een project"},
        New SelectListItem With {.Text = "Prijsofferte aanvragen", .Value = "Prijsofferte aanvragen"},
        New SelectListItem With {.Text = "Grond of pand aanbieden", .Value = "Grond of pand aanbieden"},
        New SelectListItem With {.Text = "Klacht of opmerking", .Value = "Klacht of opmerking"},
        New SelectListItem With {.Text = "Andere", .Value = "Andere"}
    }

    ' GET: /Contact
    <Route("Contact")>
    Function Index() As ActionResult
        Dim model As New MailModel
        ApplyRecaptchaSettings()
        ViewBag.OnderwerpOpties = OnderwerpOpties
        Return View(model)
    End Function
    <HttpPost>
    <Route("Contact")>
    Function Index(model As MailModel) As ActionResult
        ApplyRecaptchaSettings()
        ViewBag.OnderwerpOpties = OnderwerpOpties
        Return View(model)
    End Function
    <Route("Contact/Send")>
    <HttpPost>
    <ValidateInput(False)>
    Function Send(model As MailModel) As ActionResult
        ApplyRecaptchaSettings()
        ViewBag.OnderwerpOpties = OnderwerpOpties

        If Not model.PrivacyAkkoord Then
            ModelState.AddModelError("PrivacyAkkoord", "Gelieve akkoord te gaan met het privacybeleid.")
        End If

        If (Not ModelState.IsValid) Then Return View("index", model)

        ' Honeypot: verborgen veld dat enkel bots invullen
        Dim isHoneypotTriggered = Not String.IsNullOrEmpty(Request.Form("website_url"))

        ' reCAPTCHA v3: onzichtbare score-gebaseerde controle
        Dim captchaResponse As String = Request.Form("g-recaptcha-response")
        Dim captchaResult = ReCaptchaValidator.ValidateV3(captchaResponse, ReCaptchaActionName, ReCaptchaMinimumScore)

        ' Inhoudsfilter: URL's/domeinen in naam- of berichtvelden zijn zo goed als altijd spam
        Dim bevatVerdachteLink = SpamUrlPattern.IsMatch(model.Voornaam & " " & model.Achternaam & " " & model.Title & " " & model.Message)

        If isHoneypotTriggered OrElse Not captchaResult.Success OrElse bevatVerdachteLink Then
            LogError("CONTACT: SPAM GEWEERD | honeypot=" & isHoneypotTriggered & " | captcha=" & captchaResult.Success & " | verdachteLink=" & bevatVerdachteLink & " | email=" & model.EmailTo)
            ' Bewust geen foutmelding: een generiek succesbericht ontmoedigt bots niet om te blijven proberen
            ViewBag.SubmitSuccess = True
            ViewBag.SubmittedNaam = model.Voornaam
            ViewBag.SubmittedEmail = model.EmailTo
            Return View("index", New MailModel())
        End If

        Dim externalMailStatus As String = "Niet verzonden"
        Dim internalMailStatus As String = "Niet verzonden"
        Dim externalMailSent As Boolean = False

        ' Zelfde SMTP-pad als het (bewezen werkende) documentaanvraag-formulier
        ' (ProjectsController.SendDoc) — expliciete Office365-relay met credentials
        ' uit environment variables, i.p.v. de onbetrouwbare Postal/<system.net><mailSettings>-relay.
        Try
            ViewBag.To = model.EmailTo
            ViewBag.ContactName = model.FullName
            ViewBag.Title = model.Title
            ViewBag.Message = model.Message

            Dim emailHtml As String = ViewRenderHelper.RenderViewToString(Me.ControllerContext, "~/Views/Emails/ContactMail.vbhtml", Nothing)

            Dim msg As New System.Net.Mail.MailMessage()
            msg.To.Add(model.EmailTo)
            msg.From = New System.Net.Mail.MailAddress("info@groupln.be")
            msg.Subject = "Group LN - uw contactvraag"
            msg.Body = emailHtml
            msg.IsBodyHtml = True

            SmtpMailHelper.SendWithRetry(msg)

            externalMailStatus = "Verzonden"
            externalMailSent = True
        Catch ex As Exception
            LogError("CONTACT: MAIL TO CUSTOMER FAILED", ex)
            externalMailStatus = "Mislukt"
        End Try

        Try
            ViewBag.To = model.EmailTo
            ViewBag.ContactName = model.FullName
            ViewBag.Title = model.Title
            ViewBag.Message = model.Message
            ViewBag.Phone = model.Phone

            Dim internalHtml As String = ViewRenderHelper.RenderViewToString(Me.ControllerContext, "~/Views/Emails/InternalMail.vbhtml", Nothing)

            Dim msg2 As New System.Net.Mail.MailMessage()
            msg2.To.Add("niels.lataire@groupln.be")
            msg2.From = New System.Net.Mail.MailAddress("info@groupln.be")
            msg2.Subject = "Website Group LN : " & model.Title
            msg2.Body = internalHtml
            msg2.IsBodyHtml = True

            SmtpMailHelper.SendWithRetry(msg2)

            internalMailStatus = "Verzonden"
        Catch ex As Exception
            LogError("CONTACT: INTERNAL MAIL FAILED", ex)
            internalMailStatus = "Mislukt"
        End Try

        Dim contactRequest As New ContactRequestBO With {
            .Fullname = model.FullName,
            .Email = model.EmailTo,
            .Phone = model.Phone,
            .Subject = model.Title,
            .Question = model.Message,
            .RequestType = "Contact",
            .Origin = ResolveOrigin("ContactController.Send"),
            .SourceSite = "Group LN",
            .ExternalMailStatus = externalMailStatus,
            .InternalMailStatus = internalMailStatus
        }
        SaveContactRequest(contactRequest)

        If Not externalMailSent Then
            AddMessage("error", "Uw bericht kon niet worden verstuurd. Probeer het opnieuw.", "Fout!")
            Return View("index", model)
        End If

        ViewBag.SubmitSuccess = True
        ViewBag.SubmittedNaam = model.Voornaam
        ViewBag.SubmittedEmail = model.EmailTo
        Return View("index", New MailModel())
    End Function

    Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
        TempData("Message") = message
        TempData("MessageType") = messagetype
        TempData("MessageTitle") = messagetitle
    End Sub
    Private Function ResolveOrigin(fallback As String) As String
        Try
            Dim referrer = Request.UrlReferrer
            If referrer IsNot Nothing Then
                Dim url = referrer.AbsoluteUri
                If Not String.IsNullOrWhiteSpace(url) Then Return url
            End If
        Catch
        End Try

        Return fallback
    End Function

    Private Sub SaveContactRequest(request As ContactRequestBO)
        Try
            If request Is Nothing Then Return
            If request.CreatedAt = DateTime.MinValue Then request.CreatedAt = DateTime.UtcNow
            request.Origin = TrimValue(request.Origin, 150)
            Dim response = ServiceFactory.GetProjectService.InsertProjectContactRequest(request)
            If response IsNot Nothing AndAlso response.HasErrors Then
                LogError("CONTACTREQUEST SAVE FAILED: " & String.Join(" | ", response.Messages.Select(Function(m) m.Message)))
            End If
        Catch ex As Exception
            LogError("CONTACTREQUEST SAVE FAILED", ex)
        End Try
    End Sub

    Private Function TrimValue(value As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then Return value
        If value.Length <= maxLength Then Return value
        Return value.Substring(0, maxLength)
    End Function

    Private Sub LogError(message As String, Optional ex As Exception = Nothing)
        Try
            Dim folder = HostingEnvironment.MapPath("~/App_Data/")
            If Not Directory.Exists(folder) Then
                Directory.CreateDirectory(folder)
            End If

            Dim logFile = Path.Combine(folder, "error-log.txt")

            Using writer As New StreamWriter(logFile, True)
                writer.WriteLine("--------------------------------------------------")
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                writer.WriteLine(message)

                If ex IsNot Nothing Then
                    writer.WriteLine("EXCEPTION:")
                    writer.WriteLine(ex.ToString())
                End If

                writer.WriteLine()
            End Using

        Catch
        End Try
    End Sub

    Private Sub ApplyRecaptchaSettings()
        ViewBag.ReCaptchaSiteKey = ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")
        ViewBag.ReCaptchaAction = ReCaptchaActionName
    End Sub

End Class