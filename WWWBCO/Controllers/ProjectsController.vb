Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Mail
Imports System.Web.Hosting
Imports System.Web.Mvc
Imports BO
Imports Facade
Imports Postal
Imports System.Web.Configuration
Imports System.Web.Script.Serialization



Public Class ProjectsController
    Inherits System.Web.Mvc.Controller
    Private Function RenderViewToString(viewName As String, model As Object) As String
        Return ViewRenderHelper.RenderViewToString(Me.ControllerContext, viewName, model)
    End Function
    ' GET: /Projects
    <Route("Projects/{id?}", Name:="ProjectById")>
    Function Index(Optional id As Integer = 0) As ActionResult
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("nl-BE")
        If Not id = 0 Then
            ViewData("LatestNews") = GetLatestNews(4)
            Dim model As New ProjectDetailModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectByID(id)
            If (response.Success) Then model.Data = response.Values.FirstOrDefault
            'Sort pictures
            model.Data.Pictures = model.Data.Pictures.Where(Function(m) Not m.Type = PictureType.Nieuws).ToList
            model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList
            model.News = service.GetNewsByProjectId(id).Values.OrderByDescending(Function(m) m.NewsDate).ToList
            'Units
            Dim unitservice = ServiceFactory.GetUnitService
            Dim response3 = unitservice.GetUnitsWithDetailsByProjectId(model.Data.Id)
            If (response3.Success) Then model.Units = response3.Values
            'Docs
            model.Docs = service.GetProjectDocs(id, ProjectDocType.Sales).Values
            If model.Docs Is Nothing Then model.Docs = New List(Of ProjectDocBO)
            model.BrochureDoc = model.Docs.FirstOrDefault(Function(d) d.IsBrochure)
            'Salessettings
            model.SalesSetttings = service.GetSalesSettings(model.Data.Id).Value
            If model.SalesSetttings Is Nothing Then
                model.SalesSetttings = New ProjectSalesSettingsBO
                model.SalesSetttings.SaleVisible = False
            End If
            Dim companyservice = ServiceFactory.GetCompanyService
            model.Developer = GetCompanySafe(companyservice, model.Data.Developer)
            model.Builder = GetCompanySafe(companyservice, model.Data.Builder)
            model.Architect = GetCompanySafe(companyservice, model.Data.Architect)
            model.Engineer = GetCompanySafe(companyservice, model.Data.Engineer)
            model.SecurityCoordinator = GetCompanySafe(companyservice, model.Data.SecurityCoordinator)
            model.EpbReporter = GetCompanySafe(companyservice, model.Data.EpbReporter)
            'ViewData("title") = "BCO - " & model.Data.Name
            'Metatags
            ViewBag.Metatitle = "BCO - " & model.Data.Postalcode.Gemeente & " - " & model.Data.Street & " - " & model.Data.Name
            ViewBag.MetaSubtitle = "Vanaf " & FormatCurrency(model.SalesData.StartingPrice, 0,,, TriState.True)
            ViewBag.MetaDescription = model.Data.Postalcode.Gemeente & " - " & model.Data.Street & " - " & model.Data.Name
            ViewBag.MetaURL = "http://www.bouwenconstructie.be/woonprojecten/" & model.Data.Slug
            ViewBag.MetaImageUrl = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & model.Data.DefaultPicture.Name

            Return View("Detail", model)
        Else
            ViewData("LatestNews") = GetLatestNews(4)
            Dim model As New ProjectModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectsForList(Type:=1, StatusId:=2, UserId:=Nothing, BuilderId:=1039, TrimCommercialText:=True)
            If (response.Success) Then model.Projects = response.Values
            model.Projects = model.Projects.OrderByDescending(Function(m) m.Id).ToList
            Dim response2 = service.GetProjectSalesData(model.Projects.Select(Function(m) m.Id).ToList())
            If (response2.Success) Then model.SalesData = response2.Values
            Dim response3 = service.GetSalesSettings(model.Projects.Select(Function(m) m.Id).ToList())
            If (response3.Success) Then model.SalesSettings = response3.Values
            'Metatags
            ViewBag.Metatitle = "BCO - Woonprojecten"
            ViewBag.MetaDescription = "Woonprojecten"
            ViewBag.MetaURL = "http://www.bouwenconstructie.be/woonprojecten"
            ViewBag.MetaImageUrl = "http://www.bouwenconstructie.be/content/img/slides/slide2.jpg"
            Return View(model)
        End If

    End Function
    <Route("Projects/ProjectBySlug/{slug}", Name:="ProjectBySlug")>
    Function ProjectBySlug(slug As String) As ActionResult
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("nl-BE")
        ViewData("LatestNews") = GetLatestNews(4)
        Dim model As New ProjectDetailModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectBySlug(slug)
        If (response.Success) Then model.Data = response.Values.FirstOrDefault
        'sort pictures
        model.Data.Pictures = model.Data.Pictures.Where(Function(m) Not m.Type = PictureType.Nieuws).ToList
        model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

        model.News = service.GetNewsByProjectId(model.Data.Id).Values.OrderByDescending(Function(m) m.NewsDate).ToList
        'Docs
        model.Docs = service.GetProjectDocs(model.Data.Id, ProjectDocType.Sales).Values
        If model.Docs Is Nothing Then model.Docs = New List(Of ProjectDocBO)
        model.BrochureDoc = model.Docs.FirstOrDefault(Function(d) d.IsBrochure)
        'sort news
        'ViewData("title") = "BCO - " & model.Data.Name
        'Units
        Dim unitservice = ServiceFactory.GetUnitService
        Dim response3 = unitservice.GetUnitsWithDetailsByProjectId(model.Data.Id)
        If (response3.Success) Then model.Units = response3.Values
        Dim ids As New List(Of Integer)
        ids.Add(model.Data.Id)
        'Salesdata
        model.SalesData = service.GetProjectSalesData(ids).Values.FirstOrDefault
        'Salessettings
        model.SalesSetttings = service.GetSalesSettings(model.Data.Id).Value
        If model.SalesSetttings Is Nothing Then
            model.SalesSetttings = New ProjectSalesSettingsBO
            model.SalesSetttings.SaleVisible = False
        End If
        Dim companyservice = ServiceFactory.GetCompanyService
        model.Developer = GetCompanySafe(companyservice, model.Data.Developer)
        model.Builder = GetCompanySafe(companyservice, model.Data.Builder)
        model.Architect = GetCompanySafe(companyservice, model.Data.Architect)
        model.Engineer = GetCompanySafe(companyservice, model.Data.Engineer)
        model.SecurityCoordinator = GetCompanySafe(companyservice, model.Data.SecurityCoordinator)
        model.EpbReporter = GetCompanySafe(companyservice, model.Data.EpbReporter)
        'Metatags
        ViewBag.Metatitle = "BCO - " & model.Data.Postalcode.Gemeente & " - " & model.Data.Street & " - " & model.Data.Name
        ViewBag.MetaSubtitle = "Vanaf " & FormatCurrency(model.SalesData.StartingPrice, 0,,, TriState.True)
        ViewBag.MetaDescription = model.Data.Postalcode.Gemeente & " - " & model.Data.Street & " - " & model.Data.Name
        ViewBag.MetaURL = "http://www.bouwenconstructie.be/woonprojecten/" & model.Data.Slug
        ViewBag.MetaImageUrl = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & model.Data.DefaultPicture.Name
        Return View("Detail", model)


    End Function
    Function Detail(model As ProjectDetailModel) As ActionResult
        Return View(model)
    End Function
    <HttpGet>
    Function SendPlan(id As Integer) As ActionResult
        Dim viewModel = New ProjectSendPlanModel
        viewModel.UnitId = id
        Return PartialView("ModalSendPlan", viewModel)
    End Function
    <HttpPost>
    Function SendPlan(model As ProjectSendPlanModel) As PartialViewResult

        If Not ModelState.IsValid Then
            Return PartialView("ModalFailPlan")
        End If

        Try
            Dim externalMailStatus As String = "Niet verzonden"
            Dim internalMailStatus As String = "Niet verzonden"
            Dim externalMailSent As Boolean = False
            ' ---------------------------------------------------------
            ' 1. Ophalen unit + project
            ' ---------------------------------------------------------
            Dim unit As UnitBO = Nothing
            Dim project As ProjectBO = Nothing

            Dim unitService = ServiceFactory.GetUnitService()
            Dim unitResp = unitService.GetUnitById(model.UnitId)
            If unitResp.Success = False Then Return PartialView("ModalFailPlan")
            unit = unitResp.Value

            Dim projectService = ServiceFactory.GetProjectService()
            Dim projectResp = projectService.GetProjectByID(unit.ProjectId)
            If projectResp.Success = False Then Return PartialView("ModalFailPlan")
            project = projectResp.Value

            Dim planRequest As New ContactRequestBO With {
                    .ProjectId = project.Id,
                    .UnitId = unit.Id,
                    .DocumentName = "Plan " & unit.Type.Name & " " & unit.Name,
                    .DocumentFileName = unit.Plan,
                    .RequestType = "Plan",
                    .Firstname = model.Firstname,
                    .Lastname = model.Name,
                    .Email = model.Email,
                    .Phone = model.Phone,
                    .Origin = ResolveOrigin("ProjectsController.SendPlan"),
                    .SourceSite = "BCO"
                }

            ' ---------------------------------------------------------
            ' 2. Download plan via TLS 1.2
            ' ---------------------------------------------------------
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

            Dim fileBytes As Byte()

            Try
                fileBytes = DownloadAssetBytes("Plans", unit.Plan)
            Catch ex As Exception
                LogError("PLAN DOWNLOAD FAILED: " & unit.Plan, ex)
                planRequest.ExternalMailStatus = "Mislukt"
                planRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(planRequest)
                Return PartialView("ModalFailPlan")
            End Try


            ' ---------------------------------------------------------
            ' 3. MAIL NAAR KLANT – HTML TEMPLATE (PlanMail)
            ' ---------------------------------------------------------
            Try
                Dim msg As New Net.Mail.MailMessage()
                msg.To.Add(model.Email)
                msg.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg.Subject = "BCO - Uw planaanvraag"

                Dim emailHtml As String = RenderViewToString("~/Views/Emails/PlanMail.vbhtml", New With {
                .To = model.Email,
                .Projectname = project.Name & " - " & project.Postalcode.Gemeente,
                .Title = project.CommercialTitleNL,
                .Text = project.CommercialTextNL,
                .Image = project.DefaultPicture.Name,
                .Imagecaption = project.DefaultPicture.Caption,
                .Slug = project.Slug,
                .EmailTitle = "BCO - Uw planaanvraag",
                .Firstname = model.Firstname,
                .Name = model.Name
            })

                msg.Body = emailHtml
                msg.IsBodyHtml = True

                ' Attachment
                Dim stream As New MemoryStream(fileBytes)
                Dim attName As String = "Plan " & unit.Type.Name & " " & unit.Name & " - " & project.Name & Path.GetExtension(unit.Plan)
                Dim att As New Net.Mail.Attachment(stream, attName)
                msg.Attachments.Add(att)

                ' SMTP
                Dim smtp As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp.EnableSsl = True
                smtp.Credentials = New Net.NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp.Send(msg)
                externalMailStatus = "Verzonden"
                externalMailSent = True
            Catch ex As Exception
                LogError("SENDPLAN: MAIL TO CUSTOMER FAILED", ex)
                externalMailStatus = "Mislukt"
                planRequest.ExternalMailStatus = externalMailStatus
                planRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(planRequest)
                Return PartialView("ModalFailPlan")
            End Try


            ' ---------------------------------------------------------
            ' 4. INTERNE MAIL (PlanInternalMail)
            ' ---------------------------------------------------------
            Try
                Dim msg2 As New Net.Mail.MailMessage()
                msg2.To.Add("niels.lataire@groupln.be")
                msg2.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg2.Subject = "Nieuwe planaanvraag"

                Dim internalHtml As String = RenderViewToString("~/Views/Emails/PlanInternalMail.vbhtml", New With {
                .Project = project.Name,
                .Phone = model.Phone,
                .Name = model.Name,
                .Firstname = model.Firstname,
                .RequestType = "planaanvraag",
                .RequestTitle = "Website BCO - Nieuwe planaanvraag",
                .Unit = unit.Type.Name & " " & unit.Name,
                .Question = "",
                .To = model.Email
            })

                msg2.Body = internalHtml
                msg2.IsBodyHtml = True

                Dim smtp2 As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp2.EnableSsl = True
                smtp2.Credentials = New NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp2.Send(msg2)
                internalMailStatus = "Verzonden"

            Catch ex As Exception
                LogError("SENDPLAN: INTERNAL MAIL FAILED", ex)
                internalMailStatus = "Mislukt"
            End Try


            ' ---------------------------------------------------------
            ' 5. Succes
            ' ---------------------------------------------------------
            planRequest.ExternalMailStatus = externalMailStatus
            planRequest.InternalMailStatus = internalMailStatus
            SaveContactRequest(planRequest)
            If Not externalMailSent Then Return PartialView("ModalFailPlan")
            Return PartialView("ModalSuccesPlan")

        Catch ex As Exception
            LogError("SENDPLAN: UNEXPECTED ERROR", ex)
            Return PartialView("ModalFailPlan")
        End Try






        'If (Not ModelState.IsValid) Then Return PartialView("ModalFailPlan")
        'If (ModelState.IsValid) Then
        '    Dim unit As New UnitBO
        '    Dim project As New ProjectBO
        '    Dim service = ServiceFactory.GetUnitService
        '    Dim response = service.GetUnitById(model.UnitId)
        '    If response.Success Then unit = response.Value
        '    Dim service2 = ServiceFactory.GetProjectService
        '    Dim response2 = service2.GetProjectByID(unit.ProjectId)
        '    If response2.Success Then project = response2.Value

        '    'Mail
        '    Dim email As Object = New Email("PlanMail")
        '    email.[To] = model.Email
        '    email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
        '    email.Title = project.CommercialTitleNL
        '    email.Text = project.CommercialTextNL
        '    email.Image = project.DefaultPicture.Name
        '    email.Imagecaption = project.DefaultPicture.Caption
        '    email.Slug = project.Slug
        '    email.EmailTitle = "BCO - Uw planaanvraag"
        '    email.Firstname = model.Firstname
        '    email.Name = model.Name

        '    Dim cd As System.Net.Mime.ContentDisposition
        '    Dim planBytes = DownloadAssetBytes("Plans", unit.Plan)
        '    Using planStream As New MemoryStream(planBytes)
        '        Dim att As New System.Net.Mail.Attachment(planStream, "Plan " & unit.Type.Name & " " & unit.Name & " - " & project.Name & Path.GetExtension(unit.Plan).ToString)
        '        cd = att.ContentDisposition
        '        cd.FileName = "Plan " & unit.Type.Name & " " & unit.Name & " - " & project.Name & Path.GetExtension(unit.Plan).ToString
        '        email.Attach(att)
        '        email.Send()
        '    End Using
        '    Dim internalemail As Object = New Email("PlanInternalMail")
        '    internalemail.[To] = model.Email
        '    internalemail.[From] = "niels.lataire@groupln.be"
        '    internalemail.Unit = unit.Type.Name & " " & unit.Name
        '    internalemail.Project = project.Name
        '    internalemail.Phone = model.Phone
        '    internalemail.Name = model.Name
        '    internalemail.Firstname = model.Firstname
        '    internalemail.RequestType = "planaanvraag"
        '    internalemail.RequestTitle = "Nieuwe planaanvraag"
        '    internalemail.Send()
        '    Dim planRequest As New ContactRequestBO With {
        '        .ProjectId = project.Id,
        '        .UnitId = unit.Id,
        '        .DocumentName = "Plan " & unit.Type.Name & " " & unit.Name,
        '        .DocumentFileName = unit.Plan,
        '        .RequestType = "Plan",
        '        .Firstname = model.Firstname,
        '        .Lastname = model.Name,
        '        .Email = model.Email,
        '        .Phone = model.Phone,
        '        .Origin = ResolveOrigin("ProjectsController.SendPlan"),
        '        .SourceSite = "BCO"
        '    }
        '    SaveContactRequest(planRequest)
        '    Return PartialView("ModalSuccesPlan")
        'Else
        '    Return PartialView("ModalFailPlan")
        'End If
    End Function
    <HttpGet>
    Function SendDoc(id As Integer) As ActionResult
        Dim viewModel = New ProjectSendDocModel
        viewModel.DocId = id
        Return PartialView("ModalSendDoc", viewModel)
    End Function
    <HttpGet>
    Function SendBrochure(id As Integer) As ActionResult
        Dim viewModel = New ProjectSendBrochureModel
        viewModel.DocId = id
        Return PartialView("ModalSendBrochure", viewModel)
    End Function
    <HttpPost>
    Function SendDoc(model As ProjectSendDocModel) As PartialViewResult
        If Not ModelState.IsValid Then
            Return PartialView("ModalFailDoc")
        End If

        Try
            Dim externalMailStatus As String = "Niet verzonden"
            Dim internalMailStatus As String = "Niet verzonden"
            Dim externalMailSent As Boolean = False
            ' ---------------------------------------------------------
            ' 1. Ophalen document + project
            ' ---------------------------------------------------------
            Dim service = ServiceFactory.GetProjectService()

            Dim Doc = service.GetProjectDoc(model.DocId).Value
            If Doc Is Nothing Then Return PartialView("ModalFailDoc")

            Dim project As ProjectBO = Nothing
            Dim resp = service.GetProjectByID(Doc.ProjectId)
            If resp.Success Then project = resp.Value Else Return PartialView("ModalFailDoc")

            Dim contactRequest As New ContactRequestBO With {
                    .ProjectId = project.Id,
                    .DocumentId = Doc.Docid,
                    .DocumentName = Doc.Name,
                    .DocumentFileName = Doc.Filename,
                    .DocumentType = If(Doc.IsBrochure, "Brochure", Doc.Type.ToString()),
                    .Firstname = model.Firstname,
                    .Lastname = model.Name,
                    .Email = model.Email,
                    .Phone = model.Phone,
                    .RequestType = "Document",
                    .Origin = ResolveOrigin("ProjectsController.SendDoc"),
                    .SourceSite = "BCO"
                }

            ' ---------------------------------------------------------
            ' 2. Download bestand (document) via TLS 1.2
            ' ---------------------------------------------------------
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12

            Dim fileBytes As Byte()

            Try
                fileBytes = DownloadAssetBytes("docs", Doc.Filename)
            Catch ex As Exception
                LogError("DOC DOWNLOAD FAILED: " & Doc.Filename, ex)
                contactRequest.ExternalMailStatus = "Mislukt"
                contactRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(contactRequest)
                Return PartialView("ModalFailDoc")
            End Try


            ' ---------------------------------------------------------
            ' 3. MAIL NAAR KLANT – HTML TEMPLATE (PlanMail)
            ' ---------------------------------------------------------
            Try
                Dim msg As New Net.Mail.MailMessage()
                msg.To.Add(model.Email)
                msg.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg.Subject = "BCO - Uw documentaanvraag"

                ' HTML body van jouw template
                Dim emailHtml As String = RenderViewToString("~/Views/Emails/PlanMail.vbhtml", New With {
                .To = model.Email,
                .Projectname = project.Name & " - " & project.Postalcode.Gemeente,
                .Title = project.CommercialTitleNL,
                .Text = project.CommercialTextNL,
                .Image = project.DefaultPicture.Name,
                .Imagecaption = project.DefaultPicture.Caption,
                .Slug = project.Slug,
                .EmailTitle = "BCO - Uw documentaanvraag",
                .Firstname = model.Firstname,
                .Name = model.Name
            })

                msg.Body = emailHtml
                msg.IsBodyHtml = True

                ' Attachment toevoegen
                Dim stream As New MemoryStream(fileBytes)
                Dim att As New Net.Mail.Attachment(stream, Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename))
                msg.Attachments.Add(att)

                ' SMTP
                Dim smtp As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp.EnableSsl = True
                smtp.Credentials = New Net.NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp.Send(msg)
                externalMailStatus = "Verzonden"
                externalMailSent = True
            Catch ex As Exception
                LogError("SENDDOC: MAIL TO CUSTOMER FAILED", ex)
                externalMailStatus = "Mislukt"
                contactRequest.ExternalMailStatus = externalMailStatus
                contactRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(contactRequest)
                Return PartialView("ModalFailDoc")
            End Try


            ' ---------------------------------------------------------
            ' 4. INTERNE MAIL (PlanInternalMail)
            ' ---------------------------------------------------------
            Try
                Dim msg2 As New Net.Mail.MailMessage()
                msg2.To.Add("niels.lataire@groupln.be")
                msg2.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg2.Subject = "Nieuwe documentaanvraag"

                Dim internalHtml As String = RenderViewToString("~/Views/Emails/PlanInternalMail.vbhtml", New With {
                .Project = project.Name,
                .Phone = model.Phone,
                .Name = model.Name,
                .Firstname = model.Firstname,
                .RequestType = "documentaanvraag",
                .RequestTitle = "Website BCO - Nieuwe documentaanvraag",
                .Unit = "",
                .Question = "",
                .To = model.Email
            })

                msg2.Body = internalHtml
                msg2.IsBodyHtml = True

                Dim smtp2 As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp2.EnableSsl = True
                smtp2.Credentials = New Net.NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp2.Send(msg2)
                internalMailStatus = "Verzonden"
            Catch ex As Exception
                LogError("SENDDOC: INTERNAL MAIL FAILED", ex)
                internalMailStatus = "Mislukt"
                ' interne mail mag falen → maar klantmail is al verstuurd
            End Try


            ' ---------------------------------------------------------
            ' 5. Succes
            ' ---------------------------------------------------------
            contactRequest.ExternalMailStatus = externalMailStatus
            contactRequest.InternalMailStatus = internalMailStatus
            SaveContactRequest(contactRequest)
            If Not externalMailSent Then Return PartialView("ModalFailDoc")
            Return PartialView("ModalSuccesDoc")

        Catch ex As Exception
            LogError("SENDDOC: UNEXPECTED ERROR", ex)
            Return PartialView("ModalFailDoc")
        End Try





        'If (Not ModelState.IsValid) Then Return PartialView("ModalFailDoc")
        'If (ModelState.IsValid) Then
        '    Dim Doc As New ProjectDocBO
        '    Dim project As New ProjectBO

        '    Dim service2 = ServiceFactory.GetProjectService
        '    Doc = service2.GetProjectDoc(model.DocId).Value
        '    If Doc Is Nothing Then Return PartialView("ModalFailDoc")
        '    Dim response2 = service2.GetProjectByID(Doc.ProjectId)
        '    If response2.Success Then project = response2.Value

        '    'Mail
        '    Dim email As Object = New Email("PlanMail")
        '    email.[To] = model.Email
        '    email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
        '    email.Title = project.CommercialTitleNL
        '    email.Text = project.CommercialTextNL
        '    email.Image = project.DefaultPicture.Name
        '    email.Imagecaption = project.DefaultPicture.Caption
        '    email.Slug = project.Slug
        '    email.EmailTitle = "BCO - Uw documentaanvraag"
        '    email.Firstname = model.Firstname
        '    email.Name = model.Name
        '    Dim cd As System.Net.Mime.ContentDisposition
        '    Dim docBytes = DownloadAssetBytes("docs", Doc.Filename)
        '    Using docStream As New MemoryStream(docBytes)
        '        Dim att As New System.Net.Mail.Attachment(docStream, Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename).ToString)
        '        cd = att.ContentDisposition
        '        cd.FileName = Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename).ToString
        '        email.Attach(att)
        '        email.Send()
        '    End Using

        '    Dim internalemail As Object = New Email("PlanInternalMail")
        '    internalemail.[To] = model.Email
        '    internalemail.[From] = "niels.lataire@groupln.be"
        '    internalemail.Project = project.Name
        '    internalemail.Phone = model.Phone
        '    internalemail.Name = model.Name
        '    internalemail.Firstname = model.Firstname
        '    internalemail.RequestType = "documentaanvraag"
        '    internalemail.RequestTitle = "Nieuwe documentaanvraag"
        '    internalemail.Send()
        '    Dim contactRequest As New ContactRequestBO With {
        '        .ProjectId = project.Id,
        '        .DocumentId = Doc.Docid,
        '        .DocumentName = Doc.Name,
        '        .DocumentFileName = Doc.Filename,
        '        .DocumentType = If(Doc.IsBrochure, "Brochure", Doc.Type.ToString()),
        '        .Firstname = model.Firstname,
        '        .Lastname = model.Name,
        '        .Email = model.Email,
        '        .Phone = model.Phone,
        '        .RequestType = "Document",
        '        .Origin = ResolveOrigin("ProjectsController.SendDoc"),
        '        .SourceSite = "BCO"
        '    }
        '    SaveContactRequest(contactRequest)
        '    Return PartialView("ModalSuccesDoc")
        'Else
        '    Return PartialView("ModalFailDoc")
        'End If
    End Function
    <HttpPost>
    Function SendBrochure(model As ProjectSendBrochureModel) As PartialViewResult
        If Not ModelState.IsValid Then
            Return PartialView("ModalFailDoc")
        End If

        Try
            Dim externalMailStatus As String = "Niet verzonden"
            Dim internalMailStatus As String = "Niet verzonden"
            Dim externalMailSent As Boolean = False
            ' ---------------------------------------------------------
            ' 1. Ophalen brochure + project
            ' ---------------------------------------------------------
            Dim service = ServiceFactory.GetProjectService()

            Dim Doc = service.GetProjectDoc(model.DocId).Value
            If Doc Is Nothing Then Return PartialView("ModalFailDoc")

            Dim project As ProjectBO = Nothing
            Dim resp = service.GetProjectByID(Doc.ProjectId)
            If resp.Success Then project = resp.Value Else Return PartialView("ModalFailDoc")
            Dim contactRequest As New ContactRequestBO With {
                    .ProjectId = project.Id,
                    .DocumentId = Doc.Docid,
                    .DocumentName = Doc.Name,
                    .DocumentFileName = Doc.Filename,
                    .DocumentType = "Brochure",
                    .Firstname = model.Firstname,
                    .Lastname = model.Name,
                    .Email = model.Email,
                    .Phone = model.Phone,
                    .RequestType = "Brochure",
                    .Origin = ResolveOrigin("ProjectsController.SendBrochure"),
                    .SourceSite = "BCO"
                }

            ' ---------------------------------------------------------
            ' 2. Download brochure via TLS 1.2
            ' ---------------------------------------------------------
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12

            Dim fileBytes As Byte()

            Try
                fileBytes = DownloadAssetBytes("docs", Doc.Filename)
            Catch ex As Exception
                LogError("BROCHURE DOWNLOAD FAILED: " & Doc.Filename, ex)
                contactRequest.ExternalMailStatus = "Mislukt"
                contactRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(contactRequest)
                Return PartialView("ModalFailDoc")
            End Try


            ' ---------------------------------------------------------
            ' 3. MAIL NAAR KLANT – HTML TEMPLATE (PlanMail)
            ' ---------------------------------------------------------
            Try
                Dim msg As New Net.Mail.MailMessage()
                msg.To.Add(model.Email)
                msg.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg.Subject = "BCO - Uw brochureaanvraag"

                Dim emailHtml As String = RenderViewToString("~/Views/Emails/PlanMail.vbhtml", New With {
                .To = model.Email,
                .Projectname = project.Name & " - " & project.Postalcode.Gemeente,
                .Title = project.CommercialTitleNL,
                .Text = project.CommercialTextNL,
                .Image = project.DefaultPicture.Name,
                .Imagecaption = project.DefaultPicture.Caption,
                .Slug = project.Slug,
                .EmailTitle = "BCO - Uw brochureaanvraag",
                .Firstname = model.Firstname,
                .Name = model.Name
            })

                msg.Body = emailHtml
                msg.IsBodyHtml = True

                ' Attachment toevoegen
                Dim stream As New MemoryStream(fileBytes)
                Dim att As New Net.Mail.Attachment(stream, Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename))
                msg.Attachments.Add(att)

                ' SMTP
                Dim smtp As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp.EnableSsl = True
                smtp.Credentials = New Net.NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp.Send(msg)
                externalMailStatus = "Verzonden"
                externalMailSent = True
            Catch ex As Exception
                LogError("SENDBROCHURE: MAIL TO CUSTOMER FAILED", ex)
                externalMailStatus = "Mislukt"
                contactRequest.ExternalMailStatus = externalMailStatus
                contactRequest.InternalMailStatus = internalMailStatus
                SaveContactRequest(contactRequest)
                Return PartialView("ModalFailDoc")
            End Try


            ' ---------------------------------------------------------
            ' 4. INTERNE MAIL (PlanInternalMail)
            ' ---------------------------------------------------------
            Try
                Dim msg2 As New Net.Mail.MailMessage()
                msg2.To.Add("niels.lataire@groupln.be")
                msg2.From = New Net.Mail.MailAddress("info@bouwenconstructie.be")
                msg2.Subject = "Nieuwe brochureaanvraag"

                Dim internalHtml As String = RenderViewToString("~/Views/Emails/PlanInternalMail.vbhtml", New With {
                .Project = project.Name,
                .Phone = model.Phone,
                .Name = model.Name,
                .Firstname = model.Firstname,
                .RequestType = "brochureaanvraag",
                .RequestTitle = "Website BCO - Nieuwe brochureaanvraag",
                .Unit = "",
                .Question = "",
                .To = model.Email
            })

                msg2.Body = internalHtml
                msg2.IsBodyHtml = True

                Dim smtp2 As New Net.Mail.SmtpClient("smtp.office365.com", 587)
                smtp2.EnableSsl = True
                smtp2.Credentials = New Net.NetworkCredential("niels.lataire@groupln.be", "840683P@s")

                smtp2.Send(msg2)
                internalMailStatus = "Verzonden"
            Catch ex As Exception
                LogError("SENDBROCHURE: INTERNAL MAIL FAILED", ex)
                internalMailStatus = "Mislukt"
                ' interne mail mag falen → maar klantmail is al verstuurd
            End Try


            ' ---------------------------------------------------------
            ' 5. Succes
            ' ---------------------------------------------------------
            contactRequest.ExternalMailStatus = externalMailStatus
            contactRequest.InternalMailStatus = internalMailStatus
            SaveContactRequest(contactRequest)
            If Not externalMailSent Then Return PartialView("ModalFailDoc")
            Return PartialView("ModalSuccesDoc")

        Catch ex As Exception
            LogError("SENDBROCHURE: UNEXPECTED ERROR", ex)
            Return PartialView("ModalFailDoc")
        End Try


        'If (Not ModelState.IsValid) Then Return PartialView("ModalFailDoc")

        'Try
        '    Dim Doc As New ProjectDocBO
        '    Dim project As New ProjectBO

        '    Dim service2 = ServiceFactory.GetProjectService
        '    Doc = service2.GetProjectDoc(model.DocId).Value
        '    If Doc Is Nothing Then Return PartialView("ModalFailDoc")
        '    Dim response2 = service2.GetProjectByID(Doc.ProjectId)
        '    If response2.Success Then
        '        project = response2.Value
        '    Else
        '        Return PartialView("ModalFailDoc")
        '    End If


        '    Dim email As Object = New Email("PlanMail")
        '    email.[To] = model.Email
        '    email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
        '    email.Title = project.CommercialTitleNL
        '    email.Text = project.CommercialTextNL
        '    email.Image = project.DefaultPicture.Name
        '    email.Imagecaption = project.DefaultPicture.Caption
        '    email.Slug = project.Slug
        '    email.EmailTitle = "BCO - Uw brochureaanvraag"
        '    email.Firstname = model.Firstname
        '    email.Name = model.Name
        '    email.Phone = model.Phone
        '    Dim cd As System.Net.Mime.ContentDisposition
        '    Dim brochureBytes = DownloadAssetBytes("docs", Doc.Filename)
        '    Using brochureStream As New MemoryStream(brochureBytes)
        '        Dim att As New System.Net.Mail.Attachment(brochureStream, Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename).ToString)
        '        cd = att.ContentDisposition
        '        cd.FileName = Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename).ToString
        '        email.Attach(att)
        '        email.Send()
        '    End Using

        '    Dim internalemail As Object = New Email("PlanInternalMail")
        '    internalemail.[To] = model.Email
        '    internalemail.[From] = "niels.lataire@groupln.be"
        '    internalemail.Project = project.Name
        '    internalemail.Phone = model.Phone
        '    internalemail.Name = model.Name
        '    internalemail.Firstname = model.Firstname
        '    internalemail.RequestType = "brochureaanvraag"
        '    internalemail.RequestTitle = "Nieuwe brochureaanvraag"
        '    internalemail.Send()
        '    Dim brochureRequest As New ContactRequestBO With {
        '        .ProjectId = project.Id,
        '        .DocumentId = Doc.Docid,
        '        .DocumentName = Doc.Name,
        '        .DocumentFileName = Doc.Filename,
        '        .DocumentType = "Brochure",
        '        .Firstname = model.Firstname,
        '        .Lastname = model.Name,
        '        .Email = model.Email,
        '        .Phone = model.Phone,
        '        .RequestType = "Brochure",
        '        .Origin = ResolveOrigin("ProjectsController.SendBrochure"),
        '        .SourceSite = "BCO"
        '    }
        '    SaveContactRequest(brochureRequest)
        '    Return PartialView("ModalSuccesDoc")
        'Catch ex As Exception
        '    LogError("SENDBROCHURE FAILED", ex)
        '    Return PartialView("ModalFailDoc")
        'End Try
    End Function
    <HttpGet>
    Function SendMail(id As Integer) As ActionResult
        Dim viewModel = New ProjectSendMailModel
        viewModel.ProjectId = id
        Return PartialView("ModalSendMail", viewModel)
    End Function
    <HttpPost>
    Function SendMail(model As ProjectSendMailModel) As PartialViewResult
        If (Not ModelState.IsValid) Then Return PartialView("ModalFailMail")
        If (ModelState.IsValid) Then
            Dim project As New ProjectBO
            Dim service2 = ServiceFactory.GetProjectService
            Dim response2 = service2.GetProjectByID(model.ProjectId)
            If response2.Success Then project = response2.Value

            'Mail
            Dim email As Object = New Email("ProjectMail")
            email.[To] = model.Email
            email.[From] = "niels.lataire@groupln.be"
            email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
            email.Title = project.CommercialTitleNL
            email.Text = project.CommercialTextNL
            email.Image = project.DefaultPicture.Name
            email.Imagecaption = project.DefaultPicture.Caption
            email.Slug = project.Slug
            email.EmailTitle = "BCO - Uw informatieaanvraag"
            email.Phone = model.Phone
            email.Firstname = model.Firstname
            email.Name = model.Name
            email.Question = model.Question
            email.Send()
            'Internalmail
            Dim internalemail As Object = New Email("ProjectInternalMail")
            internalemail.[To] = model.Email
            internalemail.[From] = "niels.lataire@groupln.be"
            internalemail.Project = project.Name
            internalemail.Phone = model.Phone
            internalemail.Name = model.Name
            internalemail.Firstname = model.Firstname
            internalemail.Question = model.Question
            internalemail.Send()
            Dim mailRequest As New ContactRequestBO With {
                .ProjectId = project.Id,
                .Firstname = model.Firstname,
                .Lastname = model.Name,
                .Email = model.Email,
                .Phone = model.Phone,
                .RequestType = "ProjectContact",
                .Question = model.Question,
                .Subject = "Projectcontact",
                .Origin = ResolveOrigin("ProjectsController.SendMail"),
                .SourceSite = "BCO"
            }
            SaveContactRequest(mailRequest)
            Return PartialView("ModalSuccesMail")
        Else
            Return PartialView("ModalFailMail")
        End If
    End Function
    <Route("Projects/Photos/{slug}", Name:="ProjectPhotosBySlug")>
    Function Photos(slug As String) As ActionResult

        ViewData("LatestNews") = GetLatestNews(4)
        Dim model As New ProjectPhotosModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetPicturesByProjectSlug(slug)
        If (response.Success) Then model.Photos = response.Values
        'sort pictures
        model.Photos = model.Photos.Where(Function(m) Not m.Type = PictureType.Nieuws).ToList
        model.Photos = model.Photos.OrderByDescending(Function(m) m.DateTimeUploaded).ToList
        model.ProjectId = model.Photos.FirstOrDefault.ProjectId
        model.ProjectName = service.GetProjectNameById(model.ProjectId)
        model.ProjectCity = service.GetProjectCityById(model.ProjectId)
        model.ProjectSlug = service.GetProjectSlugById(model.ProjectId)

        ViewData("title") = "BCO - " & model.ProjectName & " - Foto's"

        Return View(model)

    End Function
    <Route("Projects/News/{slug}", Name:="ProjectNewsBySlug")>
    Function News(slug As String) As ActionResult
        ViewData("LatestNews") = GetLatestNews(4)
        Dim model As New ProjectNewsModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetNewsByProjectSlug(slug)
        If (response.Success) Then model.News = response.Values

        'sort news
        model.ProjectId = model.News.FirstOrDefault.ProjectId
        model.News = model.News.OrderByDescending(Function(m) m.NewsDate).ToList
        model.ProjectName = service.GetProjectNameById(model.ProjectId)
        model.ProjectCity = service.GetProjectCityById(model.ProjectId)
        model.ProjectSlug = service.GetProjectSlugById(model.ProjectId)

        ViewData("title") = "BCO - " & model.ProjectName & " - Nieuws"

        Return View(model)

    End Function
    <Route("Projects/{id}/News", Name:="ProjectNewsById")>
    Function News(id As Integer) As ActionResult
        ViewData("LatestNews") = GetLatestNews(4)
        Dim model As New ProjectNewsModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetNewsByProjectId(id)
        If (response.Success) Then model.News = response.Values

        'sort news
        model.ProjectId = model.News.FirstOrDefault.ProjectId
        model.News = model.News.OrderByDescending(Function(m) m.NewsDate).ToList
        model.ProjectName = service.GetProjectNameById(model.ProjectId)
        model.ProjectCity = service.GetProjectCityById(model.ProjectId)
        model.ProjectSlug = service.GetProjectSlugById(model.ProjectId)

        ViewData("title") = "BCO - " & model.ProjectName & " - Nieuws"

        Return View(model)

    End Function
    Private Function GetCompanySafe(companyService As ICompanyService, company As IdNameBO) As CompanyBO
        If companyService Is Nothing Then Return New CompanyBO
        If company Is Nothing OrElse company.ID = 0 Then Return New CompanyBO
        Dim response = companyService.GetCompanyByID(company.ID)
        If response.Success Then Return response.Values.FirstOrDefault
        Return New CompanyBO
    End Function
    Private Sub SaveContactRequest(request As ContactRequestBO)
        Try
            If request Is Nothing Then Return
            request.SourceSite = "BCO"
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

    Private Function DownloadAssetBytes(folder As String, fileName As String) As Byte()
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
        Dim assetUrl = ResolveAssetDownloadUrl(folder, fileName)

        Try
            Using webClient As New System.Net.WebClient
                Return webClient.DownloadData(assetUrl)
            End Using
        Catch ex As WebException
            Dim legacyUrl = BuildLegacyAssetUrl(folder, fileName)
            If Not String.Equals(assetUrl, legacyUrl, StringComparison.OrdinalIgnoreCase) Then
                Try
                    Using webClient As New System.Net.WebClient
                        Return webClient.DownloadData(legacyUrl)
                    End Using
                Catch
                    Throw
                End Try
            End If

            Throw
        End Try
    End Function

    Private Function ResolveAssetDownloadUrl(folder As String, fileName As String) As String
        Dim storageBaseUrl = WebConfigurationManager.AppSettings("StorageApiBaseUrl")
        If Not String.IsNullOrWhiteSpace(storageBaseUrl) Then
            Dim normalizedStorageBase = NormalizeStorageApiBaseUrl(storageBaseUrl)
            Dim signEndpoint = normalizedStorageBase & "/api/assets/" & folder & "/" & Uri.EscapeDataString(fileName) & "/sign"

            Try
                Using web As New System.Net.WebClient()
                    web.Headers(HttpRequestHeader.ContentType) = "application/x-www-form-urlencoded"
                    Dim apiKey = ResolveStorageReadApiKey()
                    If Not String.IsNullOrWhiteSpace(apiKey) Then
                        web.Headers("X-Api-Key") = apiKey
                    End If

                    Dim response = web.UploadString(signEndpoint, "POST", String.Empty)
                    Dim serializer As New JavaScriptSerializer()
                    Dim payload = serializer.DeserializeObject(response)
                    Dim dictionary = TryCast(payload, Dictionary(Of String, Object))
                    If dictionary IsNot Nothing Then
                        Dim signedPath = TryCast(dictionary("url"), String)
                        If String.IsNullOrWhiteSpace(signedPath) Then
                            signedPath = TryCast(dictionary("downloadUrl"), String)
                        End If

                        If Not String.IsNullOrWhiteSpace(signedPath) Then
                            If signedPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                                Return signedPath
                            End If

                            Return normalizedStorageBase & "/" & signedPath.TrimStart("/"c)
                        End If
                    End If
                End Using
            Catch
                ' Fallback naar legacy URL
            End Try
        End If

        Return BuildLegacyAssetUrl(folder, fileName)
    End Function


    Private Function NormalizeStorageApiBaseUrl(rawBaseUrl As String) As String
        Dim baseUrl = rawBaseUrl.Trim().TrimEnd("/"c)
        If baseUrl.EndsWith("/uploads", StringComparison.OrdinalIgnoreCase) Then
            baseUrl = baseUrl.Substring(0, baseUrl.Length - "/uploads".Length)
        End If

        Return baseUrl
    End Function

    Private Function ResolveStorageReadApiKey() As String
        Dim readKey = WebConfigurationManager.AppSettings("StorageReadApiKey")
        If Not String.IsNullOrWhiteSpace(readKey) Then Return readKey

        Return WebConfigurationManager.AppSettings("StorageApiKey")
    End Function

    Private Function BuildLegacyAssetUrl(folder As String, fileName As String) As String
        Dim imageBaseUrl = WebConfigurationManager.AppSettings("ImageWebURL")
        If String.IsNullOrWhiteSpace(imageBaseUrl) Then
            Dim storageBaseUrl = WebConfigurationManager.AppSettings("StorageApiBaseUrl")
            If String.IsNullOrWhiteSpace(storageBaseUrl) Then
                Throw New InvalidOperationException("StorageApiBaseUrl of ImageWebURL ontbreekt in appSettings.")
            End If

            imageBaseUrl = storageBaseUrl
        End If

        Dim normalizedBase = imageBaseUrl.TrimEnd("/"c)
        Return normalizedBase & "/" & folder.Trim("/"c) & "/" & Uri.EscapeDataString(fileName)
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

    Public Function GetLatestNews(number As Integer) As List(Of LatestNews)
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetLatestNews(4, 1039)
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
End Class