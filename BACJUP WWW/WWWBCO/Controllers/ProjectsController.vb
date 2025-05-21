Imports System.Web.Mvc
Imports BO
Imports Facade
Imports Postal
Imports System.Net
Imports System.Net.Mail
Imports System.IO



Public Class ProjectsController
    Inherits System.Web.Mvc.Controller
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
            'Salessettings
            model.SalesSetttings = service.GetSalesSettings(model.Data.Id).Value
            If model.SalesSetttings Is Nothing Then
                model.SalesSetttings = New ProjectSalesSettingsBO
                model.SalesSetttings.SaleVisible = False
            End If
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
            Dim response = service.GetProjectsForList(2, Nothing, 1039, True)
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
        If (Not ModelState.IsValid) Then Return PartialView("ModalFailPlan")
        If (ModelState.IsValid) Then
            Dim unit As New UnitBO
            Dim project As New ProjectBO
            Dim service = ServiceFactory.GetUnitService
            Dim response = service.GetUnitById(model.UnitId)
            If response.Success Then unit = response.Value
            Dim service2 = ServiceFactory.GetProjectService
            Dim response2 = service2.GetProjectByID(unit.ProjectId)
            If response2.Success Then project = response2.Value

            'Mail
            Dim email As Object = New Email("PlanMail")
            email.[To] = model.Email
            email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
            email.Title = project.CommercialTitleNL
            email.Text = project.CommercialTextNL
            email.Image = project.DefaultPicture.Name
            email.Imagecaption = project.DefaultPicture.Caption
            email.Slug = project.Slug
            email.EmailTitle = "BCO - Uw planaanvraag"
            'Local Upload directory
            Dim dir = My.Settings.PlanLocalUrl
            'Uploadname per directory
            Dim planpath = Path.Combine(dir, unit.Plan)
            Dim cd As System.Net.Mime.ContentDisposition
            Dim att As New System.Net.Mail.Attachment(planpath)
            cd = att.ContentDisposition
            cd.FileName = "Plan " & unit.Type.Name & " " & unit.Name & " - " & project.Name & Path.GetExtension(unit.Plan).ToString
            email.Attach(att)
            email.Send()
            Dim internalemail As Object = New Email("PlanInternalMail")
            internalemail.[To] = model.Email
            internalemail.Unit = unit.Type.Name & " " & unit.Name
            internalemail.Project = project.Name
            internalemail.Phone = model.Phone
            internalemail.Send()
            Return PartialView("ModalSuccesPlan")
        Else
            Return PartialView("ModalFailPlan")
        End If
    End Function
    <HttpGet>
    Function SendDoc(id As Integer) As ActionResult
        Dim viewModel = New ProjectSendDocModel
        viewModel.DocId = id
        Return PartialView("ModalSendDoc", viewModel)
    End Function
    <HttpPost>
    Function SendDoc(model As ProjectSendDocModel) As PartialViewResult
        If (Not ModelState.IsValid) Then Return PartialView("ModalFailDoc")
        If (ModelState.IsValid) Then
            Dim Doc As New ProjectDocBO
            Dim project As New ProjectBO

            Dim service2 = ServiceFactory.GetProjectService
            Doc = service2.GetProjectDoc(model.DocId).Value
            Dim response2 = service2.GetProjectByID(Doc.ProjectId)
            If response2.Success Then project = response2.Value

            'Mail
            Dim email As Object = New Email("PlanMail")
            email.[To] = model.Email
            email.Projectname = project.Name & " - " & project.Postalcode.Gemeente
            email.Title = project.CommercialTitleNL
            email.Text = project.CommercialTextNL
            email.Image = project.DefaultPicture.Name
            email.Imagecaption = project.DefaultPicture.Caption
            email.Slug = project.Slug
            email.EmailTitle = "BCO - Uw documentaanvraag"
            'Local Upload directory
            Dim dir = My.Settings.DocLocalUrl
            'Uploadname per directory
            Dim planpath = Path.Combine(dir, Doc.Filename)
            Dim cd As System.Net.Mime.ContentDisposition
            Dim att As New System.Net.Mail.Attachment(planpath)
            cd = att.ContentDisposition
            cd.FileName = Doc.Name & " - " & project.Name & Path.GetExtension(Doc.Filename).ToString
            email.Attach(att)
            email.Send()
            Dim internalemail As Object = New Email("PlanInternalMail")
            internalemail.[To] = model.Email
            internalemail.Project = project.Name
            internalemail.Phone = model.Phone
            internalemail.Send()
            Return PartialView("ModalSuccesDoc")
        Else
            Return PartialView("ModalFailDoc")
        End If
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
            internalemail.Project = project.Name
            internalemail.Phone = model.Phone
            internalemail.Name = model.Name
            internalemail.Firstname = model.Firstname
            internalemail.Question = model.Question
            internalemail.Send()
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