Imports System.Data.SqlClient
Imports System.Configuration
Imports System.IO
Imports System.Net.Mail
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Mvc
Imports WWWCOPRO.Models.Vacatures

Public Class VacaturesController
    Inherits System.Web.Mvc.Controller

    Private Const ReCaptchaActionName As String = "sollicitatie"
    Private Const ReCaptchaMinimumScore As Double = 0.5
    Private Const MaxCvBytes As Integer = 5 * 1024 * 1024
    Private Shared ReadOnly ToegestaneExtensies As String() = {".pdf", ".doc", ".docx"}

    Private Shared ReadOnly SpamUrlPattern As New Regex(
        "https?://|www\.|\b[a-z0-9-]+\.(com|net|org|be|nl|shop|info|xyz|biz|club|online|top|site)\b",
        RegexOptions.IgnoreCase Or RegexOptions.Compiled)

    ' GET: /vacatures
    <Route("vacatures", Name:="Vacatures")>
    Function Index() As ActionResult
        ViewData("HeroHeader") = True
        ViewData("Title") = "Vacatures | Group LN"
        ViewData("MetaDescription") = "Werken bij Group LN? Bekijk onze openstaande vacatures in projectontwikkeling, verkoop en marketing in Gent."
        ViewData("canonical") = "https://www.groupln.be/vacatures"
        ViewData("twittercard") = "summary_large_image"
        ViewData("twittertitle") = ViewData("Title")
        ViewData("twitterdescription") = ViewData("MetaDescription")
        ViewData("twitterimage") = "https://www.groupln.be/Content/img/logoimg.jpg"

        Dim vacatures = GetGepubliceerdeVacatures()
        Return View(vacatures)
    End Function

    ' GET: /vacatures/{slug}  (ook preview via ?prev=token)
    <Route("vacatures/{slug}", Name:="VacatureDetail")>
    Function Detail(slug As String, prev As String) As ActionResult
        Dim verwachtToken = ConfigurationManager.AppSettings("PreviewToken")
        Dim isPreview As Boolean = Not String.IsNullOrEmpty(verwachtToken) AndAlso
                                   Not String.IsNullOrEmpty(prev) AndAlso
                                   prev = verwachtToken

        Dim vacature = GetVacatureBySlug(slug, inclConcept:=isPreview)
        If vacature Is Nothing Then
            Return HttpNotFound()
        End If

        If isPreview Then
            ViewData("IsVoorvertoning") = True
        End If

        Dim canonicalSlug = If(vacature.Slug, "").ToLowerInvariant()
        ViewData("Title") = vacature.Titel & " | Group LN"
        ViewData("MetaDescription") = If(Not String.IsNullOrWhiteSpace(vacature.KorteBeschrijving), vacature.KorteBeschrijving, "Bekijk deze vacature bij Group LN.")
        ViewData("canonical") = "https://www.groupln.be/vacatures/" & canonicalSlug
        ViewData("twittercard") = "summary_large_image"
        ViewData("twittertitle") = ViewData("Title")
        ViewData("twitterdescription") = ViewData("MetaDescription")
        ViewData("twitterimage") = "https://www.groupln.be/Content/img/logoimg.jpg"
        ApplyRecaptchaSettings()

        Return View(vacature)
    End Function

    ' POST: /vacatures/{slug}/solliciteren
    <HttpPost>
    <Route("vacatures/{slug}/solliciteren")>
    <ValidateInput(False)>
    <ValidateAntiForgeryToken>
    Function Solliciteren(slug As String, model As SollicitatieModel, cvBestand As HttpPostedFileBase) As ActionResult
        Dim vacature = GetVacatureBySlug(slug)
        If vacature Is Nothing Then
            Return HttpNotFound()
        End If

        ApplyRecaptchaSettings()
        model.VacatureId = vacature.ID
        model.VacatureSlug = vacature.Slug
        model.VacatureTitel = vacature.Titel

        If Not model.PrivacyAkkoord Then
            ModelState.AddModelError("PrivacyAkkoord", "Gelieve akkoord te gaan met het privacybeleid.")
        End If

        If cvBestand Is Nothing OrElse cvBestand.ContentLength = 0 Then
            ModelState.AddModelError("CvBestand", "Gelieve een cv toe te voegen.")
        Else
            Dim extensie = Path.GetExtension(cvBestand.FileName)
            If String.IsNullOrWhiteSpace(extensie) OrElse Not ToegestaneExtensies.Contains(extensie.ToLowerInvariant()) Then
                ModelState.AddModelError("CvBestand", "Enkel PDF- of Word-bestanden zijn toegelaten.")
            ElseIf cvBestand.ContentLength > MaxCvBytes Then
                ModelState.AddModelError("CvBestand", "Het cv-bestand mag maximaal 5 MB groot zijn.")
            End If
        End If

        If Not ModelState.IsValid Then
            Dim errors As New Dictionary(Of String, String)
            For Each key In ModelState.Keys
                Dim fieldState = ModelState(key)
                If fieldState.Errors.Count > 0 Then
                    errors(key) = fieldState.Errors(0).ErrorMessage
                End If
            Next
            Return Json(New With {.success = False, .errors = errors})
        End If

        ' Honeypot + reCAPTCHA v3 — zelfde spambeveiliging als het contactformulier
        Dim isHoneypotTriggered = Not String.IsNullOrEmpty(Request.Form("website_url"))
        Dim captchaResponse As String = Request.Form("g-recaptcha-response")
        Dim captchaResult = ReCaptchaValidator.ValidateV3(captchaResponse, ReCaptchaActionName, ReCaptchaMinimumScore)
        Dim bevatVerdachteLink = SpamUrlPattern.IsMatch(model.Voornaam & " " & model.Achternaam & " " & model.Motivatie)

        If isHoneypotTriggered OrElse Not captchaResult.Success OrElse bevatVerdachteLink Then
            LogError("SOLLICITATIE: SPAM GEWEERD | honeypot=" & isHoneypotTriggered & " | captcha=" & captchaResult.Success & " | verdachteLink=" & bevatVerdachteLink & " | email=" & model.Email)
            ' Bewust geen foutmelding: een generiek succesbericht ontmoedigt bots niet om te blijven proberen
            Return Json(New With {.success = True})
        End If

        Dim cvBytes As Byte()
        Using ms As New MemoryStream()
            cvBestand.InputStream.CopyTo(ms)
            cvBytes = ms.ToArray()
        End Using
        Dim cvBestandsnaam = Path.GetFileName(cvBestand.FileName)
        Dim cvBestandType = If(String.IsNullOrWhiteSpace(cvBestand.ContentType), "application/octet-stream", cvBestand.ContentType)

        Dim mailVerzonden = VerstuurSollicitatieMail(model, cvBytes, cvBestandsnaam, cvBestandType)
        SlaSollicitatieOp(model, cvBytes, cvBestandsnaam, cvBestandType)

        If Not mailVerzonden Then
            Return Json(New With {.success = False, .generalError = "Uw sollicitatie kon niet worden verstuurd. Probeer het later opnieuw of mail rechtstreeks naar info@groupln.be."})
        End If

        Return Json(New With {.success = True})
    End Function

    ' ── private helpers ────────────────────────────────────────────────

    Private Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString
    End Function

    Private Sub ApplyRecaptchaSettings()
        ViewBag.ReCaptchaSiteKey = ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")
        ViewBag.ReCaptchaAction = ReCaptchaActionName
    End Sub

    Private Function VerstuurSollicitatieMail(model As SollicitatieModel, cvBytes As Byte(), cvBestandsnaam As String, cvBestandType As String) As Boolean
        Try
            ' RenderViewToString hergebruikt Me.ViewData/ViewBag — dus die moeten gezet zijn vóór de render-aanroep.
            ViewBag.VacatureTitel = model.VacatureTitel
            ViewBag.FullName = model.FullName
            ViewBag.Email = model.Email
            ViewBag.Telefoon = model.Telefoon
            ViewBag.Motivatie = model.Motivatie
            ViewBag.CvBestandsnaam = cvBestandsnaam

            Dim emailHtml As String = ViewRenderHelper.RenderViewToString(Me.ControllerContext, "~/Views/Emails/SollicitatieMail.vbhtml", Nothing)

            Dim msg As New MailMessage()
            msg.To.Add("info@groupln.be")
            msg.From = New MailAddress("info@groupln.be", "Group LN - Website")
            msg.Subject = "Group LN - Nieuwe sollicitatie: " & model.VacatureTitel
            msg.Body = emailHtml
            msg.IsBodyHtml = True

            Using ms As New MemoryStream(cvBytes)
                Dim att As New Attachment(ms, cvBestandsnaam, cvBestandType)
                msg.Attachments.Add(att)

                SmtpMailHelper.SendWithRetry(msg)
            End Using

            Return True
        Catch ex As Exception
            LogError("SOLLICITATIE: MAIL FAILED", ex)
            Return False
        End Try
    End Function

    Private Sub SlaSollicitatieOp(model As SollicitatieModel, cvBytes As Byte(), cvBestandsnaam As String, cvBestandType As String)
        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim cmd As New SqlCommand(
                    "INSERT INTO VacatureSollicitatie
                        (VacatureId, VacatureTitelSnapshot, Voornaam, Achternaam, Email, Telefoon, Motivatie, CvBestandsnaam, CvBestandType, CvBestand)
                     VALUES
                        (@VacatureId, @VacatureTitelSnapshot, @Voornaam, @Achternaam, @Email, @Telefoon, @Motivatie, @CvBestandsnaam, @CvBestandType, @CvBestand)", conn)

                cmd.Parameters.AddWithValue("@VacatureId", model.VacatureId)
                cmd.Parameters.AddWithValue("@VacatureTitelSnapshot", model.VacatureTitel)
                cmd.Parameters.AddWithValue("@Voornaam", model.Voornaam)
                cmd.Parameters.AddWithValue("@Achternaam", model.Achternaam)
                cmd.Parameters.AddWithValue("@Email", model.Email)
                cmd.Parameters.AddWithValue("@Telefoon", model.Telefoon)
                cmd.Parameters.AddWithValue("@Motivatie", If(CObj(model.Motivatie), DBNull.Value))
                cmd.Parameters.AddWithValue("@CvBestandsnaam", cvBestandsnaam)
                cmd.Parameters.AddWithValue("@CvBestandType", cvBestandType)
                cmd.Parameters.AddWithValue("@CvBestand", cvBytes)

                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            LogError("SOLLICITATIE: DB OPSLAG FAILED", ex)
        End Try
    End Sub

    Private Function GetGepubliceerdeVacatures() As List(Of VacatureModel)
        Dim result As New List(Of VacatureModel)

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim cmd As New SqlCommand(
                    "SELECT Id, Titel, Slug, Categorie, Locatie, Dienstverband, KorteBeschrijving, SortOrder
                       FROM Vacature
                      WHERE IsGepubliceerd = 1
                      ORDER BY SortOrder, Id", conn)

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        result.Add(New VacatureModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .Categorie = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .Locatie = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .Dienstverband = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .KorteBeschrijving = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .SortOrder = reader.GetInt32(7)
                        })
                    End While
                End Using
            End Using
        Catch ex As Exception
            LogError("VACATURES INDEX", ex)
            ' retourneer lege lijst zodat de pagina niet crasht
        End Try

        Return result
    End Function

    Private Function GetVacatureBySlug(slug As String, Optional inclConcept As Boolean = False) As VacatureModel
        If String.IsNullOrWhiteSpace(slug) Then Return Nothing

        Dim vacature As VacatureModel = Nothing

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim whereClause = If(inclConcept, "Slug = @slug", "Slug = @slug AND IsGepubliceerd = 1")
                Dim cmd As New SqlCommand(
                    "SELECT Id, Titel, Slug, Categorie, Locatie, Dienstverband, Opleiding, Start, KorteBeschrijving, Beschrijving, SortOrder, AangemaaktOp
                       FROM Vacature
                      WHERE " & whereClause, conn)
                cmd.Parameters.AddWithValue("@slug", slug)

                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        vacature = New VacatureModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .Categorie = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .Locatie = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .Dienstverband = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .Opleiding = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .Start = If(reader.IsDBNull(7), Nothing, reader.GetString(7)),
                            .KorteBeschrijving = If(reader.IsDBNull(8), Nothing, reader.GetString(8)),
                            .Beschrijving = If(reader.IsDBNull(9), Nothing, reader.GetString(9)),
                            .SortOrder = reader.GetInt32(10),
                            .AangemaaktOp = reader.GetDateTime(11)
                        }
                    End If
                End Using

                If vacature IsNot Nothing Then
                    Dim taakCmd As New SqlCommand("SELECT Tekst FROM VacatureTaak WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    taakCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = taakCmd.ExecuteReader()
                        While reader.Read()
                            vacature.Takenpakket.Add(reader.GetString(0))
                        End While
                    End Using

                    Dim vereisteCmd As New SqlCommand("SELECT Categorie, Tekst FROM VacatureVereiste WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    vereisteCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = vereisteCmd.ExecuteReader()
                        While reader.Read()
                            If reader.GetString(0) = "MooiMeegenomen" Then
                                vacature.MooiMeegenomen.Add(reader.GetString(1))
                            Else
                                vacature.MustHaves.Add(reader.GetString(1))
                            End If
                        End While
                    End Using

                    Dim voordeelCmd As New SqlCommand("SELECT Tekst FROM VacatureVoordeel WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    voordeelCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = voordeelCmd.ExecuteReader()
                        While reader.Read()
                            vacature.Voordelen.Add(reader.GetString(0))
                        End While
                    End Using

                    Dim stapCmd As New SqlCommand("SELECT Titel, Tekst FROM VacatureSollicitatieStap WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    stapCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = stapCmd.ExecuteReader()
                        While reader.Read()
                            vacature.SollicitatieStappen.Add(New VacatureSollicitatieStapModel With {
                                .Titel = If(reader.IsDBNull(0), Nothing, reader.GetString(0)),
                                .Tekst = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                            })
                        End While
                    End Using

                    Dim andereCmd As New SqlCommand(
                        "SELECT TOP 2 Id, Titel, Slug, Categorie, Locatie, Dienstverband
                           FROM Vacature
                          WHERE IsGepubliceerd = 1 AND Id <> @id
                          ORDER BY SortOrder, Id", conn)
                    andereCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = andereCmd.ExecuteReader()
                        While reader.Read()
                            vacature.AndereVacatures.Add(New VacatureModel With {
                                .ID = reader.GetInt32(0),
                                .Titel = reader.GetString(1),
                                .Slug = reader.GetString(2),
                                .Categorie = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                                .Locatie = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                                .Dienstverband = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            })
                        End While
                    End Using
                End If
            End Using
        Catch ex As Exception
            LogError("VACATURE DETAIL | slug=" & slug, ex)
        End Try

        Return vacature
    End Function

    Private Sub LogError(context As String, Optional ex As Exception = Nothing)
        Try
            Dim details = If(ex IsNot Nothing, ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.StackTrace, "")
            System.IO.File.AppendAllText(
                System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/vacature-error.txt"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | " & context & " | " & details & Environment.NewLine & "---" & Environment.NewLine)
        Catch
        End Try
    End Sub

    Private Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
        TempData("Message") = message
        TempData("MessageType") = messagetype
        TempData("MessageTitle") = messagetitle
    End Sub

End Class
