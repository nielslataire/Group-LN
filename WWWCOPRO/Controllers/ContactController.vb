Imports Postal
Imports BO
Imports System.IO
Imports System.Linq
Imports System.Web.Hosting
Public Class ContactController
    Inherits System.Web.Mvc.Controller

    'Private Const ReCaptchaActionName As String = "contact"
    'Private Const ReCaptchaMinimumScore As Double = 0.5

    ' GET: /Contact
    <Route("Contact")>
    Function Index() As ActionResult
        Dim model As New MailModel
        'ApplyRecaptchaSettings()
        ViewData("LatestNews") = GetLatestNews(4)
        Return View(model)
    End Function
    <HttpPost>
    <Route("Contact")>
    Function Index(model As MailModel) As ActionResult
        'ApplyRecaptchaSettings()
        ViewData("LatestNews") = GetLatestNews(4)
        Return View(model)
    End Function
    <Route("Contact/Send")>
    <HttpPost>
    <ValidateInput(False)>
    Function Send(model As MailModel) As ActionResult
        Dim errors As New ArrayList
        'ApplyRecaptchaSettings()
        ViewData("LatestNews") = GetLatestNews(4)
        'if not valid then there where errors (required property not filled in or such) so return to show them
        'For Each key In ModelState.Keys
        '    If ModelState(key).Errors.Count > 0 Then
        '        errors(key) = ModelState(key).Errors()
        '    End If
        'Next

        'Dim captchaResponse As String = Request.Form("g-recaptcha-response")
        'Dim actionFromForm As String = Request.Form("recaptcha-action")
        'Dim result As ReCaptchaValidationResult = ReCaptchaValidator.ValidateV3(captchaResponse, actionFromForm, ReCaptchaMinimumScore)
        'If Not result.Success Then
        '    If result.ErrorCodes IsNot Nothing AndAlso result.ErrorCodes.Count > 0 Then
        '        For Each err As String In result.ErrorCodes
        '            ModelState.AddModelError("", err)
        '        Next
        '    Else
        '        ModelState.AddModelError("", "Invalid reCAPTCHA response")
        '    End If
        '    Return View("index", model)
        'End If
        If (Not ModelState.IsValid) Then Return View("index", model)
        If (ModelState.IsValid) Then
            Dim externalMailStatus As String = "Niet verzonden"
            Dim internalMailStatus As String = "Niet verzonden"
            Dim externalMailSent As Boolean = False

            Try
                Dim email As Object = New Email("ContactMail")
                email.[To] = model.EmailTo
                email.ContactName = model.ContactName
                email.Title = model.Title
                email.Message = model.Message
                email.Send()
                externalMailStatus = "Verzonden"
                externalMailSent = True
            Catch ex As Exception
                LogError("CONTACT: MAIL TO CUSTOMER FAILED", ex)
                externalMailStatus = "Mislukt"
            End Try

            Try
                Dim internalemail As Object = New Email("InternalMail")
                internalemail.[To] = model.EmailTo
                internalemail.ContactName = model.ContactName
                internalemail.Title = model.Title
                internalemail.Message = model.Message
                internalemail.Phone = model.Phone
                internalemail.Send()
                internalMailStatus = "Verzonden"
            Catch ex As Exception
                LogError("CONTACT: INTERNAL MAIL FAILED", ex)
                internalMailStatus = "Mislukt"
            End Try

            Dim contactRequest As New ContactRequestBO With {
                .Fullname = model.ContactName,
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

            model = New MailModel()
            AddMessage("success", "Uw bericht is verstuurd naar Group LN, wij nemen zo snel mogelijk contact met u op.", "Geslaagd!")
            Return View("index", model)
        Else
            Return View("index", model)
        End If
    End Function

    Public Function GetLatestNews(number As Integer) As List(Of LatestNews)
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetLatestNews(4)
        Dim news As New List(Of LatestNews)
        If (response.Success) Then
            For Each value In response.Values
                Dim newsitem As New LatestNews
                newsitem.News = value
                newsitem.ProjectCity = service.GetProjectCityById(value.ProjectId)
                newsitem.ProjectName = service.GetProjectNameById(value.ProjectId)
                newsitem.ProjectSlug = service.GetProjectSlugById(value.ProjectId)
                news.Add(newsitem)
            Next
        End If
        Return news
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

    'Private Sub ApplyRecaptchaSettings()
    '    ViewBag.ReCaptchaSiteKey = ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")
    '    ViewBag.ReCaptchaAction = ReCaptchaActionName
    'End Sub

End Class