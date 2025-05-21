Imports System.Web.Mvc
Imports Microsoft.AspNet.Identity
Imports Microsoft.AspNet.Identity.Owin
Imports Microsoft.AspNet.Identity.EntityFramework
Imports BO
Imports Facade
Imports DAL
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports Rotativa
Imports System.Net.Mail
Imports MvcSiteMapProvider.Web.Mvc.Filters
Imports MvcSiteMapProvider
Imports NetOffice
Imports System.ValueTuple
Imports System.Net
Imports System.Net.FtpClient


Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Wordprocessing
Imports Spire.DataExport
Imports System.Net.WebRequestMethods
Imports DocumentFormat.OpenXml.Vml.Office
Imports Spire.XLS
Imports AngleSharp.Css.Values

'Imports Microsoft.Office.Interop


Namespace Controllers
    <Authorize>
    Public Class ProjectenController
        Inherits Controllers.ApplicationBaseController

        ' GET: Projecten
        Function Index() As ActionResult
            Dim model As New ShowProjectsModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectsForList()
            If (response.Success) Then model.Projects = response.Values.OrderByDescending(Function(m) m.DeliveryDate Is Nothing).ThenByDescending(Function(m) m.DeliveryDate).ToList()

            Dim response2 = service.GetStatuses()
            If (response2.Success) Then model.Statuses = response2.Values
            ViewData("Title") = "Projecten"
            Return View(model)
        End Function
        Function ProjectsByUserId(UserId As String) As ActionResult
            Dim model As New ShowProjectsModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectsForList(0, 0, UserId)
            If (response.Success) Then model.Projects = response.Values
            Dim response2 = service.GetStatuses()
            If (response2.Success) Then model.Statuses = response2.Values
            ViewData("Title") = "Eigen projecten"
            Return View("Index", model)
        End Function
        'PROJECT DETAIL
        <HttpGet>
        <SiteMapTitle("Project.Name")>
        Function Detail(projectid As Integer, Optional EditGeneralData As Boolean = False) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ShowProjectDetail
            Dim Service = ServiceFactory.GetProjectService
            Dim cservice = ServiceFactory.GetClientService
            Dim response = Service.GetProjectByID(projectid)
            If (response.Success) Then model.Project = response.Value
            model.Project.Postalcode.Country.CountryID = "19"
            model.Project.Postalcode.Country.ISOCode = "BE"
            FillInAddSelectListsDetail(model)
            model.GeneralDataEditMode = EditGeneralData
            model.SelectedPostalcode = model.Project.Postalcode.PostcodeId
            model.Docs = Service.GetProjectDocs(projectid).Values
            Dim Usermanager As ApplicationUserManager = HttpContext.GetOwinContext().GetUserManager(Of ApplicationUserManager)()
            model.Users = Usermanager.Users.ToList()
            model.Users = model.Users.OrderBy(Function(m) m.Displayname).ToList()
            If model.Project.ExecutionDays = 0 Then
                model.ExecutionDays = Service.GetProjectExecutionDays(model.Project.Id)
            Else
                model.ExecutionDays = model.Project.ExecutionDays
            End If
            If Not model.Project.StartDateConstruction Is Nothing Then
                model.StartDate = model.Project.StartDateConstruction
            Else
                model.StartDate = Service.GetProjectStartDateConstruction(model.Project.Id)
            End If
            model.WorkingDaysLeft = -9999
            If Not model.ExecutionDays AndAlso Not model.StartDate = DateTime.MinValue Then
                model.FinalConstructionDate = Service.GetFinalConstructionDay(model.Project.Id, model.StartDate, model.ExecutionDays)
                If Not model.FinalConstructionDate = DateTime.MinValue Then
                    model.WorkingDaysLeft = Service.GetWorkingDaysLeft(model.FinalConstructionDate, model.Project.Id)
                End If
            End If
            Dim response2 = cservice.GetClientAccountsByProjectIdLast5(projectid)
            If (response2.Success) Then model.RecentClients = response2.Values
            Dim response3 = Service.GetLatestProjectNews(1, projectid)
            If (response3.Success) Then model.LatestNews = response3.Values.FirstOrDefault
            If Not model.LatestNews Is Nothing Then
                If Not model.LatestNews.TextNL Is Nothing And model.LatestNews.TextNL.Length > 250 Then
                    model.LatestNews.TextNL = model.LatestNews.TextNL.Substring(0, 250).ToString & " ..."
                End If
            End If
            Dim response4 = Service.GetLatestProjectPictures(1, projectid)
            If (response4.Success) Then model.LatestPicture = response4.Values.FirstOrDefault
            Dim response5 = Service.GetLatestProjectDocs(5, projectid)
            If (response5.Success) Then model.LatestDocs = response5.Values
            Return View(model)
        End Function

        'PROJECT TOEVOEGEN
        Function Toevoegen() As ActionResult
            Dim model As New ProjectModel
            model.Project.Postalcode.Country.CountryID = "19"
            model.Project.Postalcode.Country.ISOCode = "BE"
            FillInAddSelectLists(model)
            Return View(model)
        End Function
        <HttpPostAttribute>
        <ValidateInput(False)>
        Function Toevoegen(model As ProjectModel) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next

            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                model.Project.Postalcode.PostcodeId = model.SelectedPostalcode
                model.Project.Status.Id = 1
                model.Project.Slug = GetSlugForPostcodeId(model.SelectedPostalcode, model.Project.Name)

                Dim service = ServiceFactory.GetProjectService
                Dim response = service.InsertUpdate(model.Project)
                If response.Success Then
                    AddMessage("success", "Het project " & model.Project.Name & " is toegevoegd", "Geslaagd!")
                    model = New ProjectModel
                    Return RedirectToAction("Index", "Home")
                Else
                    AddMessage("error", "Het project " & model.Project.Name & " is NIET toegevoegd", "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If


        End Function
        'PROJECT EDIT
        <HttpGet>
        <SiteMapTitle("Project.Name")>
        Function Edit(projectid As Integer, Optional EditGeneralData As Boolean = False) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New EditProjectDetail
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetProjectByID(projectid)
            If (response.Success) Then model.Project = response.Value
            model.Project.Postalcode.Country.CountryID = "19"
            model.Project.Postalcode.Country.ISOCode = "BE"
            FillInAddSelectListsDetailEdit(model)
            model.GeneralDataEditMode = EditGeneralData
            model.SelectedPostalcode = model.Project.Postalcode.PostcodeId
            model.Docs = Service.GetProjectDocs(projectid).Values
            Dim Usermanager As ApplicationUserManager = HttpContext.GetOwinContext().GetUserManager(Of ApplicationUserManager)()
            model.Users = Usermanager.Users.ToList()
            model.Users = model.Users.OrderBy(Function(m) m.Displayname).ToList()

            Return View(model)
        End Function
        Function EditGeneralData(projectid As Integer) As ActionResult
            Return RedirectToAction("Edit", New With {.projectid = projectid, .EditGeneralData = True})
        End Function
        <HttpPost>
        Function SaveGeneralData(model As EditProjectDetail) As ActionResult
            If model.GeneralDataEditMode = True Then
                Dim errors As New ArrayList
                'if not valid then there where errors (required property not filled in or such) so return to show them
                For Each key In ModelState.Keys
                    If ModelState(key).Errors.Count > 0 Then
                        errors(key) = ModelState(key).Errors()
                    End If
                Next
                If (Not ModelState.IsValid) Then Return View(model)
                If (ModelState.IsValid) Then
                    model.Project.Postalcode.PostcodeId = model.SelectedPostalcode
                    model.Project.Status.Id = model.SelectedStatus
                    model.Project.Slug = GetSlugForPostcodeId(model.SelectedPostalcode, model.Project.Name)
                    Dim service = ServiceFactory.GetProjectService
                    Dim response = service.InsertUpdate(model.Project)
                    If response.Success Then
                        AddMessage("success", model.Project.Name & " is bijgewerkt", "Geslaagd!")
                        Dim redirectUrl = New UrlHelper(Request.RequestContext).Action("Detail", "Projecten", New With {.projectid = model.Project.Id})
                        Return Json(New With {.Url = redirectUrl})
                        'Return RedirectToAction("Detail", New With {.id = model.Project.Id, .EditGeneralData = False})
                    Else
                        AddMessage("error", model.Project.Name & " is NIET bijgewerkt", "Fout!")
                        Return RedirectToAction("Detail", New With {.projectid = model.Project.Id, .EditGeneralData = True})
                    End If
                End If
            End If

        End Function
        Function CancelEditGeneralData(id As Integer) As ActionResult
            Return RedirectToAction("Detail", New With {.projectid = id, .EditGeneralData = False})
        End Function
        <HttpGet>
        Function AddDocument(id As Integer) As ActionResult
            Dim viewModel = New ProjectDocBO
            viewModel.ProjectId = id
            Return PartialView("ModalAddDocument", viewModel)
        End Function
        <HttpPost>
        Function AddDocument(model As ProjectDocBO, file As HttpPostedFileBase) As ActionResult

            Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & Path.GetExtension(file.FileName)
            If (file Is Nothing OrElse file.ContentLength <= 0) Then
                ModelState.AddModelError("PdfUpload", "U moet een bestand kiezen")
            End If
            If ModelState.IsValid Then

                'Local Upload directory
                Dim Uploaddir = My.Settings.DocLocalURL
                'Uploadname per directory
                Dim imagepath = Path.Combine(Uploaddir, filename)
                'Check if directories exists
                CheckDir(Uploaddir)
                'save image to directories
                file.SaveAs(imagepath)
                model.Filename = filename

                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Dim service = ServiceFactory.GetProjectService
                Dim response = service.InsertUpdateProjectDoc(model)
                If response.Success = True Then
                    If model.ClientAccountId Is Nothing Then
                        Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .EditGeneralData = False})
                    Else
                        Return RedirectToAction("Detail", "Klanten", New With {.clientid = model.ClientAccountId, .projectid = model.ProjectId})
                    End If

                    AddMessage("succes", "Het document is toegevoegd / bijgewerkt", "Gelukt!")

                Else
                    AddMessage("error", "Het document is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    If model.ClientAccountId Is Nothing Then
                        Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .EditGeneralData = False})
                    Else
                        Return RedirectToAction("Detail", "Klanten", New With {.clientid = model.ClientAccountId, .projectid = model.ProjectId})
                    End If

                End If
            End If
            Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .EditGeneralData = False})
        End Function
        <HttpGet>
        Function ModalDeleteDoc(id As Integer) As ActionResult
            Dim viewModel = New ProjectDocBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetProjectDoc(id).Value

            End If
            Return PartialView("ModalDeleteDoc", viewModel)
        End Function
        Function DeleteDoc(id As Integer) As ActionResult
            Dim viewModel = New ProjectDocBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetProjectDoc(id).Value
                'Local Upload directory
                Dim Uploaddir = My.Settings.DocLocalURL
                'Uploadname per directory
                Dim docpath = Path.Combine(Uploaddir, viewModel.Filename)
                If My.Computer.FileSystem.FileExists(docpath) Then
                    My.Computer.FileSystem.DeleteFile(docpath)
                End If
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = dservice.DeleteProjectDoc(ids)
                If response.Success = True Then
                    AddMessage("success", "Het document is verwijderd", "Geslaagd!")
                    Return RedirectToAction("Detail", "Projecten", New With {.projectid = viewModel.ProjectId})
                Else
                    AddMessage("error", "Het document is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Detail", "Projecten", New With {.projectid = viewModel.ProjectId})
                End If

            End If
            AddMessage("error", "Het document is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
            Return RedirectToAction("Detail", "Projecten", New With {.projectid = viewModel.ProjectId})
        End Function

        'PROJECT DETAIL FOTOS
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailPhotos(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailPhotosModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetPicturesByProjectId(projectid)
            If (response.Success) Then model.Photos = response.Values.OrderByDescending(Function(m) m.DateTimeUploaded).ToList
            model.ProjectId = projectid

            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpGet>
        Function ModalDeletePhoto(id As Integer) As ActionResult
            Dim viewModel = New ProjectPictureBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetPictureById(id).Value

            End If
            Return PartialView("ModalDeletePhoto", viewModel)
        End Function
        Function DeletePhoto(id As Integer, projectid As Integer, type As BO.PictureType) As ActionResult

            If Not id = 0 And Not projectid = 0 Then
                If type = PictureType.Hoofdfoto Then
                    Dim service = ServiceFactory.GetProjectService()
                    Dim ids As New List(Of Integer)
                    ids.Add(id)
                    Dim response1 = service.SetDefaultProjectPicture(projectid, 0)
                    If response1.Success = True Then
                        DeletePictureFile(id)
                        Dim response2 = service.DeletePicture(ids)
                        If response2.Success = True Then
                            AddMessage("success", "De foto is verwijderd", "Geslaagd!")
                            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
                        Else
                            AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
                        End If
                    Else
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
                    End If
                Else
                    DeletePictureFile(id)
                    Dim service = ServiceFactory.GetProjectService()
                    Dim ids As New List(Of Integer)
                    ids.Add(id)
                    Dim response = service.DeletePicture(ids)
                    If response.Success = True Then
                        AddMessage("success", "De foto is verwijderd", "Geslaagd!")
                        Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
                    Else
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
                    End If
                End If

            End If
            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = projectid})
        End Function
        Public Function UpdatePhotoType(id As Integer, type As PictureType) As ActionResult
            Dim service = ServiceFactory.GetProjectService()
            Dim picture = service.GetPictureById(id).Value
            If Not picture Is Nothing Then
                If Not type = PictureType.Hoofdfoto Then
                    picture.Type = type
                    Dim response = service.InsertUpdatePicture(picture)
                    If response.Success = True Then
                        AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!")
                        Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})
                    Else
                        AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!")
                        Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})
                    End If
                Else
                    picture.Type = type
                    Dim response1 = service.SetDefaultProjectPicture(picture.ProjectId, picture.Id)
                    If response1.Success = True Then
                        Dim response = service.InsertUpdatePicture(picture)
                        If response.Success = True Then
                            AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!")
                            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})

                        Else
                            AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!")
                            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})
                        End If
                    End If

                End If
            End If
            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})

        End Function

        'PROJECT DETAIL NIEUWS
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailNews(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailNewsModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetNewsByProjectId(projectid)
            If (response.Success) Then model.News = response.Values
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpPost>
        Public Function AddNews(NewsItem As ProjectNewsBO, file As HttpPostedFileBase) As ActionResult
            Dim response As New Response
            Dim validimagetypes As New StringCollection
            validimagetypes.Add("image/jpeg")
            Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & ".jpg"


            'If (IsNothing(file) Or file.ContentLength = 0) Then
            '    ModelState.AddModelError("ImageUpload", "This field is required")
            'ElseIf (Not validimagetypes.Contains(file.ContentType)) Then
            '    ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png")
            'End If

            If ModelState.IsValid Then
                If (file IsNot Nothing) Then
                    If file.ContentLength > 0 Then
                        Dim Uploaddir = My.Settings.ImageLocalURL
                        'Uploadname per directory
                        Dim imagepath = Path.Combine(Uploaddir & "News/", filename)
                        Dim imagepath2 = Path.Combine(Uploaddir & "News/Original/", filename)
                        Dim imagepath3 = Path.Combine(Uploaddir & "News/800/", filename)
                        CheckDir(Uploaddir & "News/")
                        CheckDir(Uploaddir & "News/Original/")
                        CheckDir(Uploaddir & "News/800/")
                        file.SaveAs(imagepath)
                        file.SaveAs(imagepath2)
                        file.SaveAs(imagepath3)
                        ScaleAndCropImage(imagepath, 1280, 500, filename)
                        ScaleImage(imagepath3, 800, 800, filename)
                        Dim picture As New ProjectPictureBO
                        picture.Name = filename
                        picture.Caption = NewsItem.TitleNL
                        picture.ProjectId = NewsItem.ProjectId
                        picture.Type = PictureType.Nieuws
                        picture.DateTimeUploaded = DateTime.Now()
                        'save image to database
                        NewsItem.Picture = picture
                    End If
                    NewsItem.Author = ViewData("fullname")

                End If
                Dim service = ServiceFactory.GetProjectService
                response = service.InsertUpdateNews(NewsItem)
            End If
            If response.Success = True Then
                AddMessage("success", "Het nieuwsbericht is toegevoegd", "Geslaagd!")
                Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})
            Else
                AddMessage("error", "Het nieuwsbericht is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})
            End If
            Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})

        End Function
        <HttpGet>
        Function ModalDeleteNews(id As Integer) As ActionResult
            Dim viewModel = New ProjectNewsBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetNewsById(id).Value

            End If
            Return PartialView("ModalDeleteNews", viewModel)
        End Function
        Function DeleteNews(id As Integer, projectid As Integer, pictureid As Integer) As ActionResult

            If Not id = 0 And Not projectid = 0 Then
                If pictureid = 0 Then
                    Dim service = ServiceFactory.GetProjectService()
                    Dim ids As New List(Of Integer)
                    ids.Add(id)
                    Dim response = service.DeleteNews(ids)
                    If response.Success = True Then
                        AddMessage("success", "Het nieuwsitem is verwijderd", "Geslaagd!")
                        Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
                    Else
                        AddMessage("error", "Het nieuwsitem is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
                    End If
                Else
                    Dim service = ServiceFactory.GetProjectService()
                    Dim ids As New List(Of Integer)
                    ids.Add(id)
                    Dim response = service.DeleteNews(ids)
                    If response.Success = True Then
                        DeletePictureFile(pictureid)
                        Dim idspic As New List(Of Integer)
                        idspic.Add(pictureid)
                        Dim responsepic = service.DeletePicture(idspic)
                        If responsepic.Success = True Then
                            AddMessage("success", "Het nieuwsitem is verwijderd", "Geslaagd!")
                            Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
                        Else
                            AddMessage("error", "Het nieuwsitem is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                            Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
                        End If
                    Else
                        AddMessage("error", "Het nieuwsitem is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
                    End If

                End If



            End If
            Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = projectid})
        End Function
        <HttpGet>
        Function ModalEditNews(id As Integer) As ActionResult
            Dim viewModel = New ProjectNewsBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetNewsById(id).Value

            End If
            Return PartialView("ModalEditNews", viewModel)
        End Function
        <HttpPost>
        Public Function EditNews(NewsItem As ProjectNewsBO, file As HttpPostedFileBase) As ActionResult
            Dim response As New Response
            Dim validimagetypes As New StringCollection
            validimagetypes.Add("image/jpeg")
            Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & ".jpg"


            'If (IsNothing(file) Or file.ContentLength = 0) Then
            '    ModelState.AddModelError("ImageUpload", "This field is required")
            'ElseIf (Not validimagetypes.Contains(file.ContentType)) Then
            '    ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png")
            'End If

            If ModelState.IsValid Then
                If (file IsNot Nothing) Then
                    If file.ContentLength > 0 Then
                        Dim Uploaddir = My.Settings.ImageLocalURL
                        'Uploadname per directory
                        Dim imagepath = Path.Combine(Uploaddir & "News/", filename)
                        Dim imagepath2 = Path.Combine(Uploaddir & "News/Original/", filename)
                        CheckDir(Uploaddir & "News/")
                        CheckDir(Uploaddir & "News/Original/")
                        file.SaveAs(imagepath)
                        file.SaveAs(imagepath2)
                        ScaleAndCropImage(imagepath, 1280, 500, filename)
                        Dim picture As New ProjectPictureBO
                        picture.Name = filename
                        picture.Caption = NewsItem.TitleNL
                        picture.ProjectId = NewsItem.ProjectId
                        picture.Type = PictureType.Nieuws
                        picture.DateTimeUploaded = DateTime.Now()
                        'save image to database
                        NewsItem.Picture = picture
                    End If



                End If
                Dim service = ServiceFactory.GetProjectService
                response = service.InsertUpdateNews(NewsItem)
            End If
            If response.Success = True Then
                AddMessage("success", "Het nieuwsbericht is toegevoegd", "Geslaagd!")
                Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})
            Else
                AddMessage("error", "Het nieuwsbericht is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})
            End If
            Return RedirectToAction("DetailNews", "Projecten", New With {.projectid = NewsItem.ProjectId})

        End Function
        <HttpPost>
        Public Sub PlaceFacebookNews(newsid As Integer)
            Dim response = UploadNewsToFacebookCopro(newsid)
            If response.Success = True Then
                AddMessage("success", "Het nieuwsbericht is op facebook geplaatst", "Geslaagd!")

            Else
                AddMessage("error", "Het nieuwsbericht is NIET op faceboook geplaatst, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
            End If
        End Sub
        <HttpPost>
        <ValidateAntiForgeryToken>
        Public Function UploadImage(UploadedBO As BO.ProjectPictureBO, files As IEnumerable(Of HttpPostedFileBase)) As ActionResult
            For Each doc In files


                Dim validimagetypes As New StringCollection
                validimagetypes.Add("image/jpeg")
                Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & ".jpg"


                If (IsNothing(doc) Or doc.ContentLength = 0) Then
                    ModelState.AddModelError("ImageUpload", "This field is required")
                ElseIf (Not validimagetypes.Contains(doc.ContentType)) Then
                    ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png")
                End If

                If ModelState.IsValid Then
                    If (doc IsNot Nothing And doc.ContentLength > 0) Then
                        'Local Upload directory
                        Dim Uploaddir = My.Settings.ImageLocalURL
                        'Uploadname per directory
                        Dim imagepath = Path.Combine(Uploaddir, filename)
                        Dim imagepath2 = Path.Combine((Uploaddir & "447/"), filename)
                        Dim imagepath3 = Path.Combine((Uploaddir & "800/"), filename)
                        'Check if directories exists
                        CheckDir(Uploaddir)
                        CheckDir(Uploaddir & "447/")
                        CheckDir(Uploaddir & "800/")
                        'save image to directories
                        doc.SaveAs(imagepath)
                        doc.SaveAs(imagepath2)
                        doc.SaveAs(imagepath3)
                        'scale and crop images in directories
                        ScaleAndCropImage(imagepath2, 447, 447, filename)
                        ScaleAndCropImage(imagepath3, 800, 800, filename)
                        ScaleImage(imagepath, 1280, 960, filename)

                        'Define new Picture BO
                        Dim picture As New ProjectPictureBO
                        picture.Name = filename
                        picture.Caption = UploadedBO.Caption
                        picture.ProjectId = UploadedBO.ProjectId
                        picture.Type = UploadedBO.Type
                        picture.DateTimeUploaded = DateTime.Now()
                        'Upload Picture to Facebook
                        Dim responseFacebook = UploadToFacebookCopro(picture)
                        If responseFacebook.Success = True Then
                            For Each message In responseFacebook.Messages
                                If message.Type = MessageType.Value Then
                                    picture.FacebookIdCopro = message.Message
                                End If
                            Next
                        End If
                        'Save image to database
                        Dim service = ServiceFactory.GetProjectService
                        Dim response = service.InsertUpdatePicture(picture)
                        If picture.Type = PictureType.Hoofdfoto Then
                            For Each Message In response.Messages
                                If Message.Type = MessageType.Value Then
                                    'set image as default image if type = hoofd
                                    'Dim response2 = service.GetPicturesByProjectId(UploadedBO.ProjectId)

                                    'If (response.Success) Then model.Photos = response.Values
                                    Dim response2 = service.SetDefaultProjectPicture(UploadedBO.ProjectId, Message.Message)

                                End If
                            Next
                        End If



                    End If
                End If
            Next
            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = UploadedBO.ProjectId})
        End Function

        'PROJECTVACATIONDAYS
        'PROJECT DETAIL ALGEMEEN
        <HttpGet>
        <SiteMapTitle("ProjectName")>
        Function ProjectVacationDays(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectVacationDaysModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectID = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpGet>
        Function GetProjectVacationDays(projectid As Integer) As JsonResult
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetProjectVacationDays(projectid)
            Dim rows As Array
            Dim vacationrows As Array
            If (response.Success) Then
                Dim dataSource = From b In response.Values Select New With {
                                                               .id = b.Id,
                                                               .title = "verlofdag",
                                                                   .year = b.VacationDay.Year,
                                                                   .month = b.VacationDay.Month,
                                                                   .day = b.VacationDay.Day,
                                                               .color = "#009336"
                    }
                rows = dataSource.ToArray()
                Dim response2 = Service.GetVacationDaysGeneral()
                If response2.Success Then
                    dataSource = From b In response2.Values Select New With {
                                                               .id = b.Id,
                                                               .title = "verlofdag",
                                                               .year = b.VacationDay.Year,
                                                               .month = b.VacationDay.Month,
                                                               .day = b.VacationDay.Day,
                                                               .color = "#777"
                }
                    vacationrows = dataSource.ToArray()
                End If
            End If

            Dim AllResults(rows.Length + vacationrows.Length - 1)
            rows.CopyTo(AllResults, 0)
            vacationrows.CopyTo(AllResults, rows.Length)
            Dim retVal = Json(AllResults, JsonRequestBehavior.AllowGet)
            Return retVal
        End Function
        <HttpPost>
        Function AddProjectVacationDay(dag As Date, projectid As Integer) As Integer
            Dim day As New VacationDayBO
            day.VacationDay = dag
            day.ProjectId = projectid
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.InsertUpdateVacationDay(day)
            If response.Success = True Then
                Return response.Messages(0).Message
            Else
                Return 0
            End If

        End Function
        <HttpPost>
        Function DeleteProjectVacationDay(id As Integer) As Boolean
            Dim ilist As New List(Of Integer)
            ilist.Add(id)
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.DeleteVacationDays(ilist)
            If response.Success = True Then
                Return True
            Else
                Return False
            End If
        End Function

        'WEATHER
        Function Weather() As ActionResult
            Dim model As New BWDModel
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetWheaterstationsSelect()
            If response.Success Then model.WeatherStations = response.Values
            Return View(model)
        End Function
        <HttpGet>
        Function GetBadWeatherDays(type As Integer, weatherstationid As Integer, year As Integer) As JsonResult
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetBadWeatherDays(weatherstationid, type)
            Dim rows As Array
            Dim vacationrows As Array
            If (response.Success) Then

                Dim dataSource = From b In response.Values Select New With {
                                                               .id = b.Id,
                                                               .title = "vorst",
                                                               .year = b.BWDate.Year,
                                                               .month = b.BWDate.Month,
                                                               .day = b.BWDate.Day,
                                                               .color = "#009336"
                }
                rows = dataSource.ToArray()
                Dim response2 = Service.GetVacationDays()
                If response2.Success Then
                    dataSource = From b In response2.Values Select New With {
                                                               .id = b.Id,
                                                               .title = "verlofdag",
                                                               .year = b.VacationDay.Year,
                                                               .month = b.VacationDay.Month,
                                                               .day = b.VacationDay.Day,
                                                               .color = "#777"
                }
                    vacationrows = dataSource.ToArray()
                End If
            End If

            Dim AllResults(rows.Length + vacationrows.Length - 1)
            rows.CopyTo(AllResults, 0)
            vacationrows.CopyTo(AllResults, rows.Length)


            Dim retVal = Json(AllResults, JsonRequestBehavior.AllowGet)

            Return retVal
        End Function
        <HttpPost>
        Function AddBadWeatherDay(dag As Date, weatherstationid As Integer, type As Integer) As Integer
            Dim bwd As New BadWeatherDayBO
            bwd.BWDate = dag
            bwd.WeatherStationId = weatherstationid
            bwd.Type = type
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.InsertUpdateBadWeatherDay(bwd)
            If response.Success = True Then
                Return response.Messages(0).Message
            Else
                Return 0
            End If

        End Function
        <HttpPost>
        Function DeleteBadWeatherDay(id As Integer) As Boolean
            Dim ilist As New List(Of Integer)
            ilist.Add(id)
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.DeleteBadWeatherDays(ilist)
            If response.Success = True Then
                Return True
            Else
                Return False
            End If
        End Function
        <HttpGet>
        Function GetVacationDays() As JsonResult
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetVacationDays()
            Dim rows As Array
            If (response.Success) Then
                Dim dataSource = From b In response.Values Select New With {
                                                               .year = b.VacationDay.Year,
                                                               .month = b.VacationDay.Month,
                                                               .day = b.VacationDay.Day
                }
                rows = dataSource.ToArray()
            End If
            Dim retVal = Json(rows, JsonRequestBehavior.AllowGet)

            Return retVal
        End Function

        'PROJECT SALES
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function Sales(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectSalesModel
            Dim service = ServiceFactory.GetProjectService
            Dim uservice = ServiceFactory.GetUnitService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            'Get Units
            Dim response = uservice.GetGroupedUnitsForSaleByProjectId(projectid)
            model.UnitsGrouped = response.Values
            Return View(model)

        End Function
        <HttpGet>
        Public Function SalesListPdf(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectSalesExportModel
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService

            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            'Get Units
            Dim response = service3.GetGroupedUnitsForSaleWithDetailsByProjectId(projectid)
            model.UnitsGrouped = response.Values
            'Get SurfaceTypes
            model.SurfaceTypes = service3.GetUniqueRoomTypesInProjectByProjectId(projectid).Values
            'return to the view again where you display that list of companies
            Dim a = New ViewAsPdf()
            a.ViewName = "SalesListPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A3
            a.FileName = "Verkooplijst - " & model.ProjectName & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
            'Dim ms As New MemoryStream(pdfBytes)
            'Return New FileStreamResult(ms, "application/pdf")
        End Function
        <HttpGet>
        Function ModalSalesSettings(projectid As Integer) As ActionResult
            Dim viewModel = New ProjectSalesSettingsBO
            If Not projectid = 0 Then
                viewModel.ProjectId = projectid
            End If
            Dim service = ServiceFactory.GetProjectService()
            Dim response = service.GetSalesSettings(projectid)
            If (response.Success) Then viewModel = response.Value
            Return PartialView("ModalSalesSettings", viewModel)
        End Function
        <HttpPost>
        Public Function SalesSettings(model As ProjectSalesSettingsBO) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next

            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                Dim service = ServiceFactory.GetProjectService
                Dim response = service.InsertUpdateSalesSettings(model)
                If response.Success Then
                    AddMessage("success", "De instellingen voor de verkoop zijn aangepast", "Geslaagd!")
                    Return RedirectToAction("Sales", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De instellingen voor de verkoop zijn NIET aangepast", "Fout!")
                    Return RedirectToAction("Sales", "Projecten", New With {.projectid = model.ProjectId})
                End If
            Else
                Return RedirectToAction("Sales", "Projecten", New With {.projectid = model.ProjectId})
            End If
        End Function
        <HttpGet>
        Function ModalSelectForPrice(projectid As Integer) As ActionResult
            Dim viewModel = New ProjectSalesSelectForPriceModel
            If Not projectid = 0 Then
                viewModel.ProjectId = projectid
            End If
            Dim service = ServiceFactory.GetUnitService()
            Dim response = service.GetAvailableUnitsByProjectId(projectid)
            If (response.Success) Then viewModel.Units = response.Values
            Return PartialView("ModalSelectForPrice", viewModel)
        End Function
        <HttpPost>
        Function ModalCalculatePrice(projectid As Integer, unitids As List(Of Integer)) As ActionResult
            Dim viewModel = New ProjectSalesCalculatePrice
            If Not projectid = 0 Then
                viewModel.ProjectId = projectid
            End If
            Dim service = ServiceFactory.GetUnitService()
            For Each id In unitids
                Dim unit As New UnitWithReductionBO
                Dim response = service.GetUnitById(id)
                If (response.Success) Then
                    unit.Base = response.Value
                    For Each cv In unit.Base.ConstructionValues
                        Dim cvr As New ConstructionReductionBO
                        cvr.ConstructionValueId = cv.Id
                        unit.ConstructionReductions.Add(cvr)
                    Next
                    viewModel.Units.Add(unit)
                End If
            Next
            Dim projectservice = ServiceFactory.GetProjectService()
            Dim response2 = projectservice.GetSalesSettings(projectid)
            If (response2.Success) Then viewModel.SalesSettings = response2.Value
            viewModel.ProjectName = projectservice.GetProjectNameById(projectid)


            'Dim service = ServiceFactory.GetUnitService()
            'Dim response = service.GetAvailableUnitsByProjectId(projectid)
            'If (response.Success) Then viewModel.Units = response.Values
            Return PartialView("ModalCalculatePrice", viewModel)
        End Function
        <HttpPost>
        Function PrintPrice(model As ProjectSalesCalculatePrice) As ActionResult
            'For Each reduction In model.Reductions
            '    model.Units.Where(Function(m) m.Base.Type.Id = reduction.Type).FirstOrDefault().Base.ConstructionValue = model.Units.Where(Function(m) m.Base.Type.Id = reduction.Type).FirstOrDefault().Base.ConstructionValue + reduction.ReductionConstructionValue
            '    model.Units.Where(Function(m) m.Base.Type.Id = reduction.Type).FirstOrDefault().Base.LandValue = model.Units.Where(Function(m) m.Base.Type.Id = reduction.Type).FirstOrDefault().Base.LandValue + reduction.ReductionLandValue
            'Next
            For Each unit In model.Units
                unit.Base.LandValue -= unit.ReductionLandValue
                For Each cv In unit.Base.ConstructionValues
                    cv.Value -= cv.Reduction
                Next
            Next
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService
            model.ProjectName = service2.GetProjectNameById(model.ProjectId)
            Dim footer As String = "--footer-center ""Datum: [date]"" --footer-font-size ""9"" --footer-font-name ""calibri light"""
            Dim a = New ViewAsPdf()
            a.ViewName = "CalculatePricePDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Portrait
            a.PageSize = Options.Size.A4
            a.IsGrayScale = False
            a.CustomSwitches = footer
            a.FileName = "Raming aankoopprijs - " & model.ProjectName & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
        End Function
        <HttpGet>
        Public Function SalesListPrint(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectSalesExportModel
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService

            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            'Get Units
            Dim response = service3.GetGroupedUnitsForSaleWithDetailsByProjectId(projectid)
            model.UnitsGrouped = response.Values
            'Get SurfaceTypes
            model.SurfaceTypes = service3.GetUniqueRoomTypesInProjectByProjectId(projectid).Values
            'return to the view again where you display that list of units for sale
            Return View(model)
        End Function
        'INVOICING
        '<OutputCache(Duration:=0)>
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function Invoicing(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectInvoicingModel
            Dim service = ServiceFactory.GetProjectService
            Dim clientservice = ServiceFactory.GetClientService

            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)

            Dim response = service.GetProjectInvoicableUnits(projectid)
            If response.Success Then
                Dim response2 = clientservice.GetClientAccountByIds(response.Values.Select(Function(m) m.Unit.ClientAccountId).Distinct().ToList())
                If response2.Success Then
                    For Each client In response2.Values
                        Dim bo As New ClientAccountWithInvoicableBO
                        bo.Client = client
                        For Each iu In response.Values.Where(Function(m) m.Unit.ClientAccountId = client.Id)
                            bo.Units.Add(iu)
                        Next
                        model.ClientAccounts.Add(bo)
                    Next

                End If

            End If
            Dim response3 = service.GetProjectInvoicableChangeOrders(projectid)
            If response3.Success Then
                Dim response2 = clientservice.GetClientAccountByIds(response3.Values.Select(Function(m) m.ClientAccountID).Distinct().ToList())
                If response2.Success Then
                    For Each client In response2.Values
                        Dim bo As New ClientAccountWithInvoicableChangeOrderBO
                        bo.Client = client
                        For Each iu In response3.Values.Where(Function(m) m.ClientAccountID = client.Id)
                            bo.ChangeOrders.Add(iu)
                        Next
                        model.ClientChangeOrders.Add(bo)
                    Next

                End If

            End If
            model.ClientAccounts = model.ClientAccounts.OrderBy(Function(m) If(m.Units.Where(Function(a) a.Unit.Type.GroupId = 1).Count > 0, m.Units.Where(Function(a) a.Unit.Type.GroupId = 1).FirstOrDefault.Unit.Name, m.Units.FirstOrDefault.Unit.Name), New Service.AlphanumComparator).ToList

            Dim clientresponse = clientservice.GetClientAccountsByProjectIdWithUnits(projectid)
            If clientresponse.Success Then
                For Each client In clientresponse.Values
                    If client.Units.Where(Function(m) m.Type.GroupId = 1 Or m.Type.GroupId = 4).Count > 0 Then
                        model.ClientUtilityCosts.Add(GetClientUtilityCost(client.Client.Id, projectid))
                    End If
                Next

            End If

            Return View(model)

        End Function
        <HttpPost>
        Function MakeInvoices(invoices As List(Of ClientAccountUnitInvoiceBO)) As JsonResult
            If Not invoices Is Nothing Then


                'SERVICES AANMAKEN
                Dim service = ServiceFactory.GetClientService()
                Dim unitservice = ServiceFactory.GetUnitService()
                Dim projectservice = ServiceFactory.GetProjectService()
                Dim response As New Response
                Dim projectid As Integer
                'CLIENTACCOUNTS OPVRAGEN
                Dim clientaccounts As New List(Of ClientAccountBO)
                Dim clientreponse = service.GetClientAccountByIds(invoices.Select(Function(m) m.ClientAccountId).Distinct().ToList())
                If clientreponse.Success Then clientaccounts = clientreponse.Values


                'CLIENTACCOUNTS OVERLOPEN
                For Each client In clientaccounts
                    Dim iu As New List(Of UnitWithStagesBO)
                    For Each unit In invoices.Where(Function(m) m.ClientAccountId = client.Id).Select(Function(m) m.Unitid).Distinct
                        Dim unitbo As New UnitWithStagesBO
                        Dim responseUnit = unitservice.GetUnitById(unit)
                        If responseUnit.Success Then
                            unitbo.Unit = responseUnit.Value
                            For Each item In invoices.Where(Function(m) m.Unitid = unitbo.Unit.Id)
                                Dim responseStage = projectservice.GetProjectPaymentStage(item.StageId)
                                If responseStage.Success Then unitbo.PaymentStages.Add(responseStage.Value)
                            Next
                            iu.Add(unitbo)
                        End If
                    Next
                    Dim project As New ProjectBO
                    Dim salessettings As New ProjectSalesSettingsBO
                    If iu.Count > 0 Then
                        Dim projectresponse = projectservice.GetProjectByID(iu.FirstOrDefault().Unit.ProjectId)
                        If projectresponse.Success Then
                            project = projectresponse.Value
                            Dim salesresponse = projectservice.GetSalesSettings(project.Id)
                            If salesresponse.Success Then salessettings = salesresponse.Value
                        End If

                    End If

                    Dim returnresponse = MakeNewWordInvoiceFTP(client, iu, project, salessettings)
                    'Dim returnresponse = MakeNewWordInvoice(client, iu, project, salessettings)
                    If Not returnresponse.Success Then
                        response.AddError(returnresponse.Messages)
                    End If
                    projectid = project.Id
                Next
                If response.Success = True Then

                    AddMessage("success", "De facturen zijn aangemaakt", "Gelukt!")
                    Return Json(New With {.projectid = projectid})
                Else
                    AddMessage("Error", "Niet alle facturen zijn aangemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    Return Json(New With {.projectid = projectid})
                End If
            Else
                Return Json(New With {.projectid = 0})
            End If

        End Function

        <HttpPost>
        Function MakeInvoicesCO(invoices As List(Of ClientAccountChangeOrderInvoiceBO)) As JsonResult

            If Not invoices Is Nothing Then
                'SERVICES AANMAKEN
                Dim service = ServiceFactory.GetClientService()
                Dim unitservice = ServiceFactory.GetUnitService()
                Dim projectservice = ServiceFactory.GetProjectService()
                Dim response As New Response
                Dim projectid As Integer
                'CLIENTACCOUNTS OPVRAGEN
                Dim clientaccounts As New List(Of ClientAccountBO)
                Dim clientreponse = service.GetClientAccountByIds(invoices.Select(Function(m) m.ClientAccountId).Distinct().ToList())
                If clientreponse.Success Then clientaccounts = clientreponse.Values


                'CLIENTACCOUNTS OVERLOPEN
                For Each client In clientaccounts
                    Dim responseUnit = unitservice.GetUnitsByAccountId(client.Id)
                    Dim units As New List(Of UnitBO)
                    If response.Success Then units = responseUnit.Values
                    Dim co As New List(Of ChangeOrderBO)
                    For Each item In invoices.Where(Function(m) m.ClientAccountId = client.Id).Select(Function(m) m.ChangeOrderId).Distinct
                        Dim ChangeOrder As New ChangeOrderBO
                        Dim ResponseCO = projectservice.GetChangeOrder(item)
                        If ResponseCO.Success Then
                            ChangeOrder = ResponseCO.Value
                            For i = ChangeOrder.Details.Count - 1 To 0 Step -1
                                If invoices.FindIndex(Function(l) l.ChangeOrderDetailId = ChangeOrder.Details(i).Id) >= 0 Then
                                Else
                                    ChangeOrder.Details.RemoveAt(i)
                                End If
                            Next i
                            co.Add(ChangeOrder)

                        End If
                    Next
                    Dim project As New ProjectBO
                    Dim salessettings As New ProjectSalesSettingsBO
                    If co.Count > 0 Then
                        Dim projectresponse = projectservice.GetProjectByID(co.FirstOrDefault().ProjectId)
                        If projectresponse.Success Then
                            project = projectresponse.Value
                            Dim salesresponse = projectservice.GetSalesSettings(project.Id)
                            If salesresponse.Success Then salessettings = salesresponse.Value
                        End If

                    End If
                    Dim returnresponse = MakeNewWordInvoiceCOFTP(client, co, units, project, salessettings)
                    If Not returnresponse.Success Then
                        response.AddError(returnresponse.Messages)
                    End If
                    projectid = project.Id
                Next
                If response.Success = True Then

                    AddMessage("success", "De facturen zijn aangemaakt", "Gelukt!")
                    Return Json(New With {.projectid = projectid})
                Else
                    AddMessage("Error", "Niet alle facturen zijn aangemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    Return Json(New With {.projectid = projectid})
                End If
            Else
                Return Json(New With {.projectid = 0})
            End If



        End Function
        <HttpGet>
        Function PaymentStages(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectPaymentStagesModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Dim response = service.GetProjectPaymentGroups(projectid)
            If (response.Success) Then model.Groups = response.Values
            'Set sitemap node
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If


            Return View(model)

        End Function
        <HttpGet>
        Function PaymentStagesAddUpdate(projectid As Integer, Optional groupid As Integer = 0) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectPaymentStagesAddUpdateModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            'Set sitemap node
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If

            If groupid = 0 Then
                model.Stages.Add(New ProjectPaymentStageBO)
            Else
                Dim response = service.GetProjectPaymentGroup(groupid)
                If response.Success Then model.Group = response.Value

                For Each stage In model.Group.PaymentStages
                    model.Stages.Add(stage)
                Next
                If model.Stages.Count = 0 Then
                    model.Stages.Add(New ProjectPaymentStageBO)
                End If
            End If
            Return View(model)

        End Function
        <HttpPost>
        Function PaymentStagesAddUpdate(model As ProjectPaymentStagesAddUpdateModel) As ActionResult
            'Dim errors As New ArrayList
            ''if not valid then there where errors (required property not filled in or such) so return to show them
            'For Each key In ModelState.Keys
            '    If ModelState(key).Errors.Count > 0 Then
            '        errors(key) = ModelState(key).Errors()
            '    End If
            'Next

            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                For Each stage In model.Stages
                    stage.GroupId = model.Group.Id
                    model.Group.PaymentStages.Add(stage)
                Next

                Dim service = ServiceFactory.GetProjectService
                model.Group.ProjectId = model.ProjectId
                Dim response = service.InsertUpdateProjectPaymentGroup(model.Group)
                If response.Success Then
                    AddMessage("success", "De betalingsschijven zijn met succes aan het project " & model.ProjectName & " toegevoegd", "Geslaagd!")
                    Return RedirectToAction("PaymentStages", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De betalingsschijven zijn NIET aan het project " & model.ProjectName & "  toegevoegd", "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If



        End Function
        <HttpPost>
        Function BlankStageRow() As PartialViewResult
            Return PartialView("_PaymentStageRow", New ProjectPaymentStageBO)
        End Function
        <HttpGet>
        Function PaymentGroupLink(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectPaymentGroupLinkModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Dim response = service.GetProjectPaymentGroupsForSelect(projectid)
            If (response.Success) Then model.PaymentGroups = response.Values
            Dim service2 = ServiceFactory.GetUnitService
            Dim response2 = service2.GetUnitsByProjectId(projectid)
            If (response.Success) Then model.Units = response2.Values
            'Set sitemap node
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If


            Return View(model)

        End Function
        <HttpPost>
        Function PaymentGroupLink(model As ProjectPaymentGroupLinkModel) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                Dim service = ServiceFactory.GetProjectService
                For Each unit In model.Units
                    If Not unit.PaymentGroupId Is Nothing Or unit.PaymentGroupId = 0 Then
                        service.LinkPaymentGroupToUnit(unit.Id, unit.PaymentGroupId)
                    End If
                Next
                AddMessage("success", "De betalingsschijven zijn met succes gelinkt", "Geslaagd!")
                Return RedirectToAction("PaymentStages", "Projecten", New With {.projectid = model.ProjectId})
            Else
                AddMessage("Error", "De betalingsschijven zijn NIET gelinkt", "Fout!")
                Return View(model)
            End If

            Return View(model)

        End Function
        <HttpPost>
        Sub PaymentStagesInvoicable(stageid As Integer, value As Boolean)
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.UpdateProjectPaymentStageInvoicable(stageid, value)
            If response.Success Then
            Else
                AddMessage("Error", "De betalingsschijf Is niet aangepast, probeer het later opnieuw", "Fout!")
            End If
        End Sub
        <HttpPost>
        Function ModalAddStageDoc(id As Integer, stageid As Integer) As ActionResult
            Dim viewModel = New AddStageDocModel
            viewModel.ProjectId = id
            viewModel.StageId = stageid
            viewModel.Doc.ProjectId = id
            viewModel.Doc.Type = ProjectDocType.Invoicing
            Return PartialView("ModalAddStageDoc", viewModel)
        End Function
        <HttpPost>
        Function AddStageDoc(model As AddStageDocModel, file As HttpPostedFileBase) As ActionResult

            Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & Path.GetExtension(file.FileName)
            If (file Is Nothing OrElse file.ContentLength <= 0) Then
                ModelState.AddModelError("PdfUpload", "U moet een bestand kiezen")
            End If
            If ModelState.IsValid Then

                'Local Upload directory
                Dim Uploaddir = My.Settings.DocLocalURL
                'Uploadname per directory
                Dim imagepath = Path.Combine(Uploaddir, filename)
                'Check if directories exists
                CheckDir(Uploaddir)
                'save image to directories
                file.SaveAs(imagepath)
                model.Doc.Filename = filename
                model.Doc.Type = ProjectDocType.Invoicing
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Dim service = ServiceFactory.GetProjectService
                Dim Stage As New ProjectPaymentStageBO
                Dim resp = service.GetProjectPaymentStage(model.StageId)
                If resp.Success Then
                    Stage = resp.Value
                    Stage.Doc = model.Doc
                    Dim response = service.InsertUpdateProjectPaymentStage(Stage)
                    If response.Success = True Then
                        AddMessage("success", "Het document Is toegevoegd / bijgewerkt", "Gelukt!")
                        Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                    Else
                        AddMessage("Error", "Het document Is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                    End If
                Else
                    AddMessage("Error", "Het document Is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                End If
            End If

            Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
        End Function
        <HttpPost>
        Function ModalSelectStageDoc(id As Integer, stageid As Integer) As ActionResult
            Dim viewModel = New SelectStageDocModel
            viewModel.ProjectId = id
            viewModel.StageId = stageid
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectDocsForSelect(id, ProjectDocType.Invoicing)
            If response.Success Then viewModel.Docs = response.Values

            Return PartialView("ModalSelectStageDoc", viewModel)
        End Function
        <HttpPost>
        Function SelectStageDoc(model As SelectStageDocModel) As ActionResult
            If ModelState.IsValid Then
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Dim service = ServiceFactory.GetProjectService
                Dim Stage As New ProjectPaymentStageBO
                Dim resp = service.GetProjectPaymentStage(model.StageId)
                If resp.Success Then
                    Stage = resp.Value
                    Dim resp2 = service.GetProjectDoc(model.DocId)
                    If resp2.Success Then
                        Stage.Doc = resp2.Value
                        Dim response = service.InsertUpdateProjectPaymentStage(Stage)
                        If response.Success = True Then
                            AddMessage("success", "Het document Is toegevoegd / bijgewerkt", "Gelukt!")
                            Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                        Else
                            AddMessage("Error", "Het document Is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                            Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                        End If
                    Else
                        AddMessage("Error", "Het document Is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                    End If

                Else
                    AddMessage("Error", "Het document Is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
                End If
            End If

            Return RedirectToAction("PaymentStages", New With {.projectid = model.ProjectId})
        End Function
        <HttpPost>
        Function ModalDeleteStageDoc(id As Integer, stageid As Integer, docid As Integer) As ActionResult
            Dim viewModel = New DeleteStageDocModel
            viewModel.ProjectId = id
            viewModel.StageId = stageid
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectDoc(docid)
            If response.Success Then viewModel.Doc = response.Value
            Return PartialView("ModalDeleteStageDoc", viewModel)
        End Function
        <HttpGet>
        Function DeleteStageDoc(projectid As Integer, stageid As Integer, docid As Integer) As ActionResult
            If ModelState.IsValid Then
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Dim service = ServiceFactory.GetProjectService
                Dim Stage As New ProjectPaymentStageBO
                Dim resp = service.GetProjectPaymentStage(stageid)
                If resp.Success Then
                    Stage = resp.Value
                    Stage.Doc = Nothing
                    Dim response = service.InsertUpdateProjectPaymentStage(Stage)
                    If response.Success = True Then
                        Dim bool As Boolean = service.CheckProjectPaymentStageDocInUse(docid)
                        If bool = False Then
                            Dim doc = service.GetProjectDoc(docid).Values.FirstOrDefault
                            Dim ids As New List(Of Integer)
                            ids.Add(docid)
                            Dim response2 = service.DeleteProjectDoc(ids)
                            If response2.Success Then
                                Dim dir = My.Settings.DocLocalURL
                                'Uploadname per directory
                                Dim localpath = Path.Combine(dir, doc.Filename)
                                'Check if directories exists
                                If My.Computer.FileSystem.FileExists(localpath) Then
                                    My.Computer.FileSystem.DeleteFile(localpath)
                                End If
                                AddMessage("success", "Het document Is verwijderd", "Gelukt!")
                                Return RedirectToAction("PaymentStages", New With {.projectid = projectid})
                            Else
                                AddMessage("Error", "Het document Is NIET verwijderd, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                                Return RedirectToAction("PaymentStages", New With {.projectid = projectid})
                            End If
                        Else
                            AddMessage("success", "Het document Is verwijderd", "Gelukt!")
                            Return RedirectToAction("PaymentStages", New With {.projectid = projectid})

                        End If

                    Else
                        AddMessage("Error", "Het document Is NIET verwijderd, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("PaymentStages", New With {.projectid = projectid})
                    End If
                Else
                    AddMessage("Error", "Het document Is NIET verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("PaymentStages", New With {.projectid = projectid})
                End If
            End If

            Return RedirectToAction("PaymentStages", New With {.projectid = projectid})
        End Function
        <HttpGet>
        Function ModalPrintInvoiceList(projectid As Integer) As ActionResult
            Dim viewModel = New ModalPrintInvoiceListModel
            If Not projectid = 0 Then
                viewModel.ProjectId = projectid
            End If
            Dim service = ServiceFactory.GetClientService
            Dim response = service.GetClientAccountsByProjectIdForSelect(projectid)
            If response.Success Then
                viewModel.Client = response.Values
            End If
            Return PartialView("ModalPrintInvoiceList", viewModel)
        End Function
        <HttpPost>
        Function PrintInvoiceList(model As ModalPrintInvoiceListModel) As ActionResult
            Dim viewmodel As New PrintInvoiceListModel
            viewmodel.ProjectId = model.ProjectId
            Dim pservice = ServiceFactory.GetProjectService
            Dim cservice = ServiceFactory.GetClientService
            Dim uservice = ServiceFactory.GetUnitService
            viewmodel.ProjectName = pservice.GetProjectNameById(model.ProjectId)
            viewmodel.Client = cservice.GetClientAccountById(model.SelectedClient).Value
            'Get Client Units with stages
            Dim invoicingresponse = uservice.GetClientUnitsWithStages(model.SelectedClient)
            viewmodel.UnitsWithStages = invoicingresponse.Values
            'Get Unit Invoices
            Dim invoiceresponse = pservice.GetInvoicesByUnitIds(viewmodel.UnitsWithStages.Select(Function(m) m.Unit.Id).ToList())
            viewmodel.Invoices = invoiceresponse.Values
            viewmodel.SalesSettings = pservice.GetSalesSettings(model.ProjectId).Value
            Dim footer As String = "--footer-center ""Datum: [date]"" --footer-font-size ""9"" --footer-font-name ""Avenir-book"""
            Dim a = New ViewAsPdf()
            a.ViewName = "PrintInvoiceList"
            a.Model = viewmodel
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A4
            a.IsGrayScale = False
            a.CustomSwitches = footer
            a.FileName = "Betalingsoverzicht - " & viewmodel.Client.Name & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
        End Function
        'DOCS
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailDocs(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailDocsModel
            Dim service = ServiceFactory.GetProjectService

            Dim response = service.GetProjectDocs(projectid)
            If (response.Success) Then model.Docs = response.Values


            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function




        'Image handlers
        Public Sub ScaleAndCropImage(imagepath As String, maxwidth As Integer, maxheight As Integer, filename As String)

            Dim image1 = System.Drawing.Image.FromFile(imagepath)
            Dim ratioX = CDbl(maxwidth) / image1.Width
            Dim ratioY = CDbl(maxheight) / image1.Height
            Dim ratio = Math.Max(ratioX, ratioY)
            Dim newWidth = CInt(image1.Width * ratio)
            Dim newHeight = CInt(image1.Height * ratio)
            Dim newImage As New Bitmap(newWidth, newHeight)

            Using graphics1 = Graphics.FromImage(newImage)

                graphics1.DrawImage(image1, 0, 0, newWidth, newHeight)
                image1.Dispose()
            End Using
            Dim croppedImage = New Bitmap(maxwidth, maxheight)
            Dim CropRect As New Rectangle((newWidth - maxwidth) / 2, (newHeight - maxheight) / 2, maxwidth, maxheight)
            Using graphics2 = Graphics.FromImage(croppedImage)
                graphics2.DrawImage(newImage, New Rectangle(0, 0, maxwidth, maxheight), CropRect, GraphicsUnit.Pixel)
                graphics2.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                graphics2.CompositingQuality = Drawing2D.CompositingQuality.HighQuality
                graphics2.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
                newImage.Dispose()
            End Using
            CompressAndSaveImage(croppedImage, imagepath, 65)
            'croppedImage.Save(imagepath)

        End Sub
        Public Sub ScaleImage(imagepath As String, maxwidth As Integer, maxheight As Integer, filename As String)

            Dim image1 = System.Drawing.Image.FromFile(imagepath)
            Dim ratioX = CDbl(maxwidth) / image1.Width
            Dim ratioY = CDbl(maxheight) / image1.Height
            Dim ratio = Math.Max(ratioX, ratioY)
            Dim newWidth = CInt(image1.Width * ratio)
            Dim newHeight = CInt(image1.Height * ratio)
            Dim newImage As New Bitmap(newWidth, newHeight)

            Using graphics1 = Graphics.FromImage(newImage)

                graphics1.DrawImage(image1, 0, 0, newWidth, newHeight)
                graphics1.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                graphics1.CompositingQuality = Drawing2D.CompositingQuality.HighQuality
                graphics1.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
                image1.Dispose()
            End Using
            CompressAndSaveImage(newImage, imagepath, 65)
            'newImage.Save(imagepath)

        End Sub
        Public Sub CheckDir(path As String)
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
        End Sub
        Public Sub CheckDirFTP(path As String, ftp As Chilkat.Ftp2)
            Dim dirExists As Boolean
            Dim success As Boolean
            dirExists = ftp.ChangeRemoteDir(path)
            If (dirExists = True) Then
                success = ftp.ChangeRemoteDir("..")
                If (success <> True) Then
                    MakeLog("Change Remote Dir to top", ftp.LastErrorText)
                    Exit Sub
                End If
            Else
                success = ftp.CreateRemoteDir(path)
                If (success <> True) Then
                    MakeLog("Change Remote Dir to top", ftp.LastErrorText)
                    Exit Sub
                End If
            End If
        End Sub
        Private Shared Function GetCodecInfo(mimetype As String) As ImageCodecInfo
            For Each Encoder As ImageCodecInfo In ImageCodecInfo.GetImageEncoders()
                If Encoder.MimeType = mimetype Then
                    Return Encoder
                End If
            Next Encoder
            Throw New ArgumentOutOfRangeException(String.Format("'{0}' not supported", mimetype))

        End Function
        Private Sub CompressAndSaveImage(img As System.Drawing.Image, filename As String, quality As Long)
            Dim parameters As New EncoderParameters(1)
            parameters.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality)
            img.Save(filename, GetCodecInfo("image/jpeg"), parameters)
        End Sub
        Public Sub DeletePictureFile(id As Integer)
            Dim service = ServiceFactory.GetProjectService()
            Dim pic As ProjectPictureBO = service.GetPictureById(id).Value
            Select Case pic.Type
                Case PictureType.Hoofdfoto, PictureType.Nevenfoto, PictureType.Werffoto
                    System.IO.File.Delete(My.Settings.ImageLocalURL & pic.Name)
                    System.IO.File.Delete(My.Settings.ImageLocalURL & "447/" & pic.Name)
                    System.IO.File.Delete(My.Settings.ImageLocalURL & "800/" & pic.Name)
                Case PictureType.Nieuws
                    System.IO.File.Delete(My.Settings.ImageLocalURL & "news/" & pic.Name)
                    System.IO.File.Delete(My.Settings.ImageLocalURL & "news/original/" & pic.Name)
            End Select


        End Sub

        'Facebook
        <HttpPost>
        Public Function FacebookPhoto(id As Integer) As ActionResult
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetPictureById(id)
            Dim picture As New ProjectPictureBO
            If (response.Success) Then picture = response.Value
            If Not picture Is Nothing Then
                UploadToFacebookCopro(picture)
            End If
            Return RedirectToAction("DetailPhotos", "Projecten", New With {.projectid = picture.ProjectId})
        End Function
        '<HttpPost>
        'Public Function GetFacebookPlaces(term As String) As JsonResult
        '    Dim pservice = ServiceFactory.GetFacebookService()
        '    Dim presponse = pservice.fa
        '    Dim iList As New List(Of SelectBO)
        '    If (presponse.Success) Then
        '        For Each x In presponse.Values
        '            Dim bo As New SelectBO
        '            bo.id = x.Id
        '            bo.text = x.Name
        '            bo.extra = x.Visible
        '            iList.Add(bo)
        '        Next
        '    End If
        '    Return Json(iList, JsonRequestBehavior.AllowGet)
        'End Function
        Public Function UploadToFacebookCopro(picture As ProjectPictureBO) As Response
            Dim service = ServiceFactory.GetProjectService()
            Dim AlbumId As String = service.GetFacebookAlbumIdCoproByProjectId(picture.ProjectId)
            Dim facebookservice = ServiceFactory.GetFacebookService()
            Dim fbalbum As New FacebookAlbumBO
            If AlbumId Is Nothing Or AlbumId = "0" Then
                Dim project As ProjectBO = service.GetProjectByID(picture.ProjectId).Value

                fbalbum.Name = project.Name & " te " & project.Postalcode.Gemeente
                If Not project.CommercialTextNL Is Nothing Or Not project.CommercialTextNL = "" Then
                    fbalbum.Description = StripTags(project.CommercialTextNL)
                Else
                    fbalbum.Description = ""
                End If

                Dim response = facebookservice.FacebookAlbum(fbalbum, My.Settings.FacebookAccessTokenCopro, My.Settings.FacebookIDCopro)
                If response.Success = True Then
                    For Each Message In response.Messages
                        If Message.Type = MessageType.Value Then
                            project.FacebookAlbumId = Message.Message

                        End If
                    Next
                    service.InsertUpdate(project)
                    fbalbum.Id = project.FacebookAlbumId
                Else
                    Return response
                End If

            Else
                fbalbum.Id = AlbumId
            End If
            Dim response2 = facebookservice.FacebookPhoto(picture, fbalbum, My.Settings.FacebookAccessTokenCopro, My.Settings.FacebookIDCopro, My.Settings.ImageLocalURL)
            Return response2


        End Function
        Public Function UploadNewsToFacebookCopro(newsid As Integer) As Response
            'NOG UIT TE WERKEN
            Dim service = ServiceFactory.GetProjectService()
            'Dim AlbumId As String = service.GetFacebookAlbumIdCoproByProjectId(picture.ProjectId)
            Dim facebookservice = ServiceFactory.GetFacebookService()
            Dim fbfeed As New FacebookFeedBO
            Dim news As ProjectNewsBO = service.GetNewsById(newsid).Value
            Dim Uploaddir = My.Settings.ImageLocalURL
            'Uploadname per directory
            fbfeed.Message = news.TextNL

            'fbfeed.Action = service.GetProjectNameById(news.ProjectId) & " te " & System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(service.GetProjectCityById(news.ProjectId).ToLower) & " - " & news.TitleNL
            fbfeed.Link = "http://www.groupln.be/projects/" & news.ProjectId & "/News?id=" & news.ProjectId & "&newsid=" & newsid
            'fbfeed.Name = service.GetProjectNameById(news.ProjectId) & " te " & System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(service.GetProjectCityById(news.ProjectId).ToLower) & " - " & news.TitleNL
            'fbfeed.Caption = news.TitleNL
            'fbfeed.Description = news.TextNL
            fbfeed.Picture = Path.Combine(My.Settings.ImageWebURL & "Pictures/News/800/", news.Picture.Name)
            Dim response2 = facebookservice.FacebookFeed(fbfeed, My.Settings.FacebookAccessTokenCopro, My.Settings.FacebookIDCopro)

            Return response2
        End Function
        Public Function StripTags(ByVal html As String) As String
            Return Regex.Replace(html, "<.*?>", "")
        End Function

        'Units
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailUnits(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"

            Return View(FillDetailUnitModel(projectid))
        End Function
        <HttpPost>
        Public Function AddUnit(Model As DetailUnitsModel) As ActionResult
            If Model.SelectedType = 0 Then
                Return RedirectToAction("DetailUnits", Model)
            End If
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Model.AddUnit.ProjectId = Model.ProjectId
            Model.AddUnit.Type.Id = Model.SelectedType

            If Model.AddUnit.AttachedUnitsId = 0 Then
                Model.AddUnit.AttachedUnitsId = Nothing
            End If
            Dim service = ServiceFactory.GetUnitService
            Dim service2 = ServiceFactory.GetProjectService
            Dim response = service.InsertUpdateUnit(Model.AddUnit)
            If response.Success = True Then
                For Each item In Model.ConstructionValues
                    item.UnitId = response.InsertedId
                    Dim responseConst = service.InsertUpdateConstructionValue(item)
                Next
                Model.AddUnit.Name = ""
                Dim response2 = service.GetGroupedUnitsByProjectId(Model.ProjectId)
                Model.UnitsGrouped = response2.Values
                'Get GroupTypes
                Dim responsegroup = service.GetUnitGroupTypes()
                If (responsegroup.Success) Then Model.GroupTypes = responsegroup.Values
                'Get Subtypes
                Dim responsetypes = service.GetUnitTypesByGroupId(Model.SelectedGroupType)
                If (responsetypes.Success) Then Model.Types = responsetypes.Values

                Model.ProjectName = service2.GetProjectNameById(Model.ProjectId)

                AddMessage("success", "De eenheid is aan het project toegevoegd", "Geslaagd!")
                Return RedirectToAction("DetailUnits", New With {Key .projectid = Model.ProjectId})

            Else
                AddMessage("error", "De eenheid is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailUnits", New With {Key .projectid = Model.ProjectId})
            End If
        End Function
        <HttpGet>
        Public Function EditUnit(projectid As Integer, unitid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New EditUnitModel
            Dim service = ServiceFactory.GetUnitService
            Dim service2 = ServiceFactory.GetProjectService

            'Get Unit
            Dim response = service.GetUnitById(unitid)
            If (response.Success) Then model.Unit = response.Value
            'linkedunits
            For Each u In model.Unit.LinkedUnits
                model.SelectedUnits.Add(u.Id)
            Next
            If model.Unit.IsLink = True Then
                model.Type = EditUnitModel.EnumType.Koppeling
            Else
                model.Type = EditUnitModel.EnumType.Eenheid
            End If
            'Get Units for select
            Dim uresponse = service.GetUnitsByProjectIdForSelect(model.Unit.ProjectId, model.Unit.Type.Id)
            If (uresponse.Success) Then model.Units = uresponse.Values
            'Get Units for attached unit select
            Dim u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(model.Unit.ProjectId, unitid)
            If (u2response.Success) Then model.AttachableUnits = u2response.Values
            'Get GroupTypes
            Dim responsegroup = service.GetUnitGroupTypes()
            If (responsegroup.Success) Then model.GroupTypes = responsegroup.Values
            model.SelectedGroupType = model.Unit.Type.GroupId
            'Get Rooms
            Dim responserooms = service.GetRooms(unitid)
            If (responserooms.Success) Then model.Rooms = responserooms.Values
            model.Rooms = model.Rooms.OrderBy(Function(m) m.Type).ToList
            'Get Constructionvalues
            Dim responseconstructionvalues = service.GetConstructionValues(unitid)
            If (responseconstructionvalues.Success) Then model.ConstructionValues = responseconstructionvalues.Values

            'Get Subtypes
            Dim responsetypes = service.GetUnitTypesByGroupId(model.Unit.Type.GroupId)
            If (responsetypes.Success) Then model.Types = responsetypes.Values
            model.SelectedType = model.Unit.Type.Id

            'Get PaymentGroups
            Dim responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid)
            If (responsepaymentgroups.Success) Then model.PaymentGroups = responsepaymentgroups.Values
            ViewBag.paymentgroups = model.PaymentGroups
            If Not model.Unit.PaymentGroupId Is Nothing Then model.SelectedPaymentGroup = model.Unit.PaymentGroupId Else model.SelectedPaymentGroup = 0

            model.ProjectId = model.Unit.ProjectId
            model.ProjectName = service2.GetProjectNameById(model.Unit.ProjectId)
            model.ProjectLandShare = service2.GetProjectLandshareById(model.Unit.ProjectId)


            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            Return View(model)
        End Function
        <HttpPost>
        Public Function EditUnit(Model As EditUnitModel, file As HttpPostedFileBase) As ActionResult

            Dim validtypes As New StringCollection
            validtypes.Add("application/pdf")
            Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & ".pdf"
            If (file IsNot Nothing AndAlso file.ContentLength > 0) Then
                If (Not validtypes.Contains(file.ContentType)) Then
                    ModelState.AddModelError("PdfUpload", "Verkeerd type gekozen, kies een pdf")
                End If
            End If
            If ModelState.IsValid Then
                If (file IsNot Nothing AndAlso file.ContentLength > 0) Then
                    'Local Upload directory
                    Dim Uploaddir = My.Settings.PlanLocalURL
                    'Uploadname per directory
                    Dim imagepath = Path.Combine(Uploaddir, filename)
                    'Check if directories exists
                    CheckDir(Uploaddir)
                    'save image to directories
                    file.SaveAs(imagepath)
                    Model.Unit.Plan = filename
                End If



                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Model.Unit.ProjectId = Model.ProjectId
                Model.Unit.Type.Id = Model.SelectedType
                If Not Model.SelectedPaymentGroup = 0 Then Model.Unit.PaymentGroupId = Model.SelectedPaymentGroup
                If (Model.Unit.IsLink) Then
                    For Each i In Model.SelectedUnits
                        Dim bo As New UnitBO
                        bo.Id = i
                        Model.Unit.LinkedUnits.Add(bo)
                    Next
                    Model.Unit.Name = "KOPPELING"
                End If
                Dim service = ServiceFactory.GetUnitService
                Dim service2 = ServiceFactory.GetProjectService
                Try
                    Dim response = service.InsertUpdateUnit(Model.Unit)
                    If response.Success = False Then
                        Throw New ApplicationException(response.Messages.SingleOrDefault.Message)
                    End If
                    Dim response2 As New Response
                    For Each room In Model.Rooms
                        response2 = service.InsertUpdateRoom(room)
                    Next
                    If response2.Success = False Then
                        For Each message In response2.Messages
                            Throw New ApplicationException(message.Message)
                        Next
                    End If
                    Dim response3 As New Response
                    For Each constructionvalue In Model.ConstructionValues
                        response3 = service.InsertUpdateConstructionValue(constructionvalue)
                        If response3.Success Then constructionvalue.Id = response3.InsertedId
                    Next
                    If response3.Success = False Then
                        For Each message2 In response3.Messages
                            Throw New ApplicationException(message2.Message)
                        Next
                    End If
                    Dim tableresult As New List(Of UnitConstructionValueBO)
                    Dim responsetable = service.GetConstructionValues(Model.Unit.Id)
                    If (responsetable.Success) Then tableresult = responsetable.Values
                    Dim deleteids As New List(Of Integer)
                    For Each result In tableresult
                        If Model.ConstructionValues.Exists(Function(m) m.Id = result.Id) Then
                        Else
                            deleteids.Add(result.Id)
                        End If

                    Next
                    Dim response4 = service.DeleteConstructionValues(deleteids)
                    If response4.Success = False Then
                        Throw New ApplicationException(response4.Messages.SingleOrDefault.Message)
                    End If
                Catch ex As Exception
                    AddMessage("error", "De eenheid is NIET volledig bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Finally
                    AddMessage("success", "De eenheid is met succes bijgewerkt", "Geslaagd!")
                End Try
                Return Redirect(Request.UrlReferrer.ToString())
            End If
        End Function
        <HttpGet>
        Function ModalDeleteUnit(id As Integer) As ActionResult
            Dim viewModel = New UnitBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetUnitService
                viewModel = dservice.GetUnitById(id).Value

            End If
            Return PartialView("ModalDeleteUnit", viewModel)
        End Function
        Function DeleteUnit(id As Integer, projectid As Integer) As ActionResult

            If Not id = 0 And Not projectid = 0 Then

                Dim service = ServiceFactory.GetUnitService()
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = service.DeleteUnit(ids)
                If response.Success = True Then
                    AddMessage("success", "De eenheid is verwijderd", "Geslaagd!")
                    Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = projectid})
                Else
                    AddMessage("error", "De eenheid is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = projectid})
                End If
            End If
            Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = projectid})
        End Function
        <HttpGet>
        Function ModalAddUnitLink(id As Integer) As ActionResult
            Dim viewModel = New AddUnitLinkModel
            Dim service = ServiceFactory.GetUnitService
            'Get Unit
            Dim response = service.GetUnitById(id)
            If (response.Success) Then viewModel.SelectedUnit = response.Value
            Dim ids As New List(Of Integer)
            ids.Add(id)
            Dim response2 = service.GetUnitsByProjectIdForSelect(viewModel.SelectedUnit.ProjectId, viewModel.SelectedUnit.Type.Id)
            If (response2.Success) Then viewModel.Units = response2.Values
            viewModel.Units.Remove(viewModel.Units.Find(Function(m) m.ID = id))

            Return PartialView("ModalAddLink", viewModel)
        End Function
        <HttpPost>
        Public Function AddUnitLink(model As AddUnitLinkModel) As ActionResult
            Dim response As New Response
            If ModelState.IsValid Then
                Dim service = ServiceFactory.GetUnitService
                model.SelectedUnits.Add(model.SelectedUnit.Id)
                For Each i In model.SelectedUnits
                    Dim bo As New UnitBO
                    bo.Id = i
                    model.Unit.LinkedUnits.Add(bo)
                Next
                model.Unit.Name = "KOPPELING"
                model.Unit.ProjectId = model.SelectedUnit.ProjectId
                model.Unit.IsLink = True

                response = service.InsertUpdateUnit(model.Unit)
            End If
            If response.Success = True Then
                AddMessage("success", "De koppeling is geslaagd", "Geslaagd!")
                Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = model.SelectedUnit.ProjectId})
            Else
                AddMessage("error", "De koppeling is NIET geslaagd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = model.SelectedUnit.ProjectId})
            End If
            Return RedirectToAction("DetailUnits", "Projecten", New With {.projectid = model.SelectedUnit.ProjectId})

        End Function
        <HttpPost>
        Public Function BlankRoomRow(unitid As Integer) As PartialViewResult
            Dim bo As New RoomBO
            bo.UnitId = unitid
            bo.Number = 1
            Return PartialView("_RoomEditorRow", bo)
        End Function
        <HttpPost>
        Public Function BlankConstructionValueRow(unitid As Integer, projectid As Integer) As PartialViewResult
            Dim bo As New UnitConstructionValueBO
            bo.UnitId = unitid
            bo.PaymentGroupId = 0
            Dim service2 = ServiceFactory.GetProjectService
            Dim responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid)
            ViewBag.paymentgroups = responsepaymentgroups.Values
            Return PartialView("_ConstructionValueEditorRow", bo)
        End Function
        Function GetSubType(id As Integer) As ActionResult
            Dim items As New List(Of SelectListItem)

            Dim service2 = ServiceFactory.GetUnitService

            Dim responselevels = service2.GetUnitTypesByGroupId(id)
            If (responselevels.Success) Then
                For Each type In responselevels.Values
                    items.Add(New SelectListItem() With {.Text = type.Name, .Value = type.Id})
                Next
            End If
            Return Json(items, JsonRequestBehavior.AllowGet)
        End Function
        Public Function FillDetailUnitModel(id As Integer) As DetailUnitsModel
            Dim model As New DetailUnitsModel
            Dim service = ServiceFactory.GetUnitService
            Dim service2 = ServiceFactory.GetProjectService
            ''Get Units for select
            'Dim uresponse = service.GetUnitsByProjectIdForSelect(id, False)
            'If (uresponse.Success) Then model.Units = uresponse.Values
            'Get Units for attached unit select
            Dim u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(id)
            If (u2response.Success) Then model.AttachableUnits = u2response.Values
            'Get Units
            Dim response = service.GetGroupedUnitsByProjectId(id)
            model.UnitsGrouped = response.Values
            'Get GroupTypes
            Dim responsegroup = service.GetUnitGroupTypes()
            If (responsegroup.Success) Then model.GroupTypes = responsegroup.Values
            model.SelectedGroupType = 1
            'Get Subtypes
            Dim responsetypes = service.GetUnitTypesByGroupId(model.SelectedGroupType)
            If (responsetypes.Success) Then model.Types = responsetypes.Values

            model.ProjectId = id
            model.ProjectName = service2.GetProjectNameById(id)
            model.ProjectLandShare = service2.GetProjectLandshareById(id)
            Dim constval As New UnitConstructionValueBO
            constval.PaymentGroupId = 0
            model.ConstructionValues.Add(constval)

            Dim responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(id)
            If (responsepaymentgroups.Success) Then model.PaymentGroups = responsepaymentgroups.Values
            ViewBag.paymentgroups = model.PaymentGroups

            Return model

        End Function

        'Clients
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Public Function DetailClients(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailClientsModel
            Dim service = ServiceFactory.GetClientService
            Dim service2 = ServiceFactory.GetProjectService
            Dim response = service.GetClientAccountsByProjectIdWithUnits(projectid)
            If (response.Success) Then model.ClientAccounts = response.Values
            If model.ClientAccounts.SelectMany(Function(m) m.Units.Where(Function(i) i.Type.GroupId = 1)).Count > 0 Then
                model.ClientAccounts = model.ClientAccounts.OrderBy(Function(m) If(m.Units.Where(Function(a) a.Type.GroupId = 1).Count > 0, m.Units.Where(Function(a) a.Type.GroupId = 1).FirstOrDefault.Name, ""), New Service.AlphanumComparator).ToList
            End If
            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpGet>
        Public Function DetailClientsListPrint(id As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailClientsExportModel
            Dim service = ServiceFactory.GetClientService
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService
            Dim response = service.GetClientAccountsByProjectIdWithUnits(id)
            If (response.Success) Then model.ClientAccounts = response.Values
            model.ClientAccounts = model.ClientAccounts.OrderBy(Function(m) m.Units.Where(Function(a) a.Type.GroupId = 1).FirstOrDefault.Name, New Service.AlphanumComparator).ToList
            model.ProjectId = id
            model.ProjectName = service2.GetProjectNameById(id)
            Dim response2 = service3.GetUniqueUnitTypesInProjectByProjectId(id)
            If (response2.Success) Then model.UnitTypes = response2.Values
            'return to the view again where you display that list of companies
            Return View(model)
        End Function
        <HttpGet>
        Public Function DetailClientsListPdf(id As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailClientsExportModel
            Dim service = ServiceFactory.GetClientService
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService
            Dim response = service.GetClientAccountsByProjectIdWithUnits(id)
            If (response.Success) Then model.ClientAccounts = response.Values
            model.ClientAccounts = model.ClientAccounts.OrderByDescending(Function(i) i.Units.Where(Function(l) l.Type.GroupId = 1).FirstOrDefault IsNot Nothing).ThenBy(Function(m) If(m.Units.Where(Function(i) i.Type.GroupId = 1).Count = 0, m.Units.FirstOrDefault.Name, m.Units.Where(Function(i) i.Type.GroupId = 1).FirstOrDefault.Name), New Service.AlphanumComparator).ToList
            model.ProjectId = id
            model.ProjectName = service2.GetProjectNameById(id)
            Dim response2 = service3.GetUniqueUnitTypesInProjectByProjectId(id)
            If (response2.Success) Then model.UnitTypes = response2.Values
            'return to the view again where you display that list of companies
            Dim a = New ViewAsPdf()
            a.ViewName = "DetailClientsListPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A3
            a.FileName = "Klantenlijst - " & model.ProjectName & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
            'Dim ms As New MemoryStream(pdfBytes)
            'Return New FileStreamResult(ms, "application/pdf")
        End Function
        <HttpGet>
        Function ModalDetailClientsGiftsPdf(id As Integer) As ActionResult
            Dim viewModel = New ExportGiftsToPdfModel
            If Not id = 0 Then
                viewModel.ProjectId = id
            End If
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()
            If (response.Success) Then viewModel.ListActivities = response.Values
            Return PartialView("ModalExportGiftsToPdf", viewModel)
        End Function
        <HttpPost>
        Public Function DetailClientsGiftsPdf(viewmodel As ExportGiftsToPdfModel) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailClientsGiftsModel
            Dim service = ServiceFactory.GetClientService
            Dim service2 = ServiceFactory.GetActivityService
            Dim service3 = ServiceFactory.GetProjectService
            If viewmodel.SelectedActivities Is Nothing Then
                Dim response = service.GetClientsGifts(viewmodel.ProjectId)
                If (response.Success) Then model.ClientGifts = response.Values
                Dim list As New List(Of Integer)
                For Each act In model.ClientGifts.SelectMany(Function(m) m.Activities)
                    If Not list.Contains(act.ID) Then
                        list.Add(act.ID)
                    End If
                Next
                Dim response2 = service2.GetActivitiesbyId(list)
                If (response2.Success) Then model.SelectedActivities = response2.Values
            ElseIf viewmodel.SelectedActivities.Count > 0 Then
                Dim response = service.GetClientsGifts(viewmodel.ProjectId, viewmodel.SelectedActivities)
                If (response.Success) Then model.ClientGifts = response.Values
                Dim response2 = service2.GetActivitiesbyId(viewmodel.SelectedActivities)
                If (response2.Success) Then model.SelectedActivities = response2.Values
            End If

            model.ProjectId = viewmodel.ProjectId
            model.ProjectName = service3.GetProjectNameById(model.ProjectId)
            'return to the view again where you display that list of companies
            Dim a = New ViewAsPdf()
            a.ViewName = "DetailClientsGiftsPdf"
            a.Model = model
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A4
            a.FileName = "Toegiften - " & model.ProjectName & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
            Return RedirectToAction("DetailClients", New With {Key .projectid = viewmodel.ProjectId})
            'Dim ms As New MemoryStream(pdfBytes)
            'Return New FileStreamResult(ms, "application/pdf")
        End Function

        'Contracts
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function Recalculation(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectContractsModel
            Dim service = ServiceFactory.GetProjectService
            Dim aservice = ServiceFactory.GetActivityService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            'Get Units
            Dim response = aservice.GetActivityGroups
            model.ActivityGroups = response.Values
            Dim response2 = service.GetProjectContracts(projectid)
            model.Contracts = response2.Values
            Dim response3 = service.GetProjectBudget(projectid)
            model.BudgetActivities = response3.Values
            Dim response4 = service.GetProjectIncommingInvoicesForRecalculation(projectid)
            model.IncommingInvoicesActivities = response4.Values
            Return View(model)

        End Function
        <HttpGet>
        Function ContractAdd(projectid As Integer, Optional contractid As Integer = 0) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectAddContractModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectId = projectid

            If contractid = 0 Then
                model.Contract.ProjectId = projectid
                model.Contract.GuaranteeType = ContractGuaranteeType.NoGuarantee
            Else
                Dim cresponse = service.GetContract(contractid)
                If cresponse.Success Then model.Contract = cresponse.Value
                'For Each contractact In model.Contract.Activities
                '    model.SelectedActivities.Add(contractact.Activity.ID)
                'Next
            End If
            model.ProjectName = service.GetProjectNameById(projectid)
            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            model.Insurance.Startdate = Date.Now()
            Dim iservice = ServiceFactory.GetInsuranceService
            Dim response = iservice.GetInsuranceCompaniesForSelect()
            If response.Success Then model.InsuranceCompanies = response.Values

            ViewBag.returnUrl = Request.UrlReferrer
            Return View(model)
        End Function
        <HttpPost>
        Function ContractAdd(model As ProjectAddContractModel, activities As List(Of ContractActivityBO), additionalorders As List(Of ContractAdditionalOrderBO), returnUrl As String) As ActionResult
            Dim errors As New ArrayList
            'If Not activities Is Nothing And activities.Count > 0 Then
            '    For Each act In activities
            '        model.SelectedActivities.Add(act.Activity.ID)
            '    Next
            'End If
            'If Not additionalorders Is Nothing Then
            '    If additionalorders.Count > 0 Then
            '        For Each order In additionalorders
            '            model.SelectedActivitiesAddOrders.Add(order.ContractActivityId)
            '        Next
            '    End If
            'End If

            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next

            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                For Each contractactivity In activities

                    If contractactivity.Activity.ID = 142 AndAlso contractactivity.ContractId = 0 Then
                        Dim i As New InsuranceBO
                        i.Startdate = Date.Today
                        contractactivity.InsuranceData = i
                    End If
                    model.Contract.Activities.Add(contractactivity)

                Next

                Dim service = ServiceFactory.GetProjectService
                Dim response = service.InsertUpdateProjectContract(model.Contract)
                If response.Success Then
                    AddMessage("success", "Het contract is toegevoegd aan het project " & model.ProjectName, "Geslaagd!")
                    Return Redirect(returnUrl)
                    'Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "Het contract is NIET toegevoegd aan het project " & model.ProjectName, "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If
        End Function
        <HttpGet>
        Function ModalDeleteContract(id As Integer) As ActionResult
            Dim viewModel = New ContractBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetContract(id).Values.FirstOrDefault
            End If
            Return PartialView("ModalDeleteContract", viewModel)
        End Function
        Function DeleteContract(id As Integer, projectid As Integer) As ActionResult
            If Not id = 0 And Not projectid = 0 Then
                Dim service = ServiceFactory.GetProjectService()
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = service.DeleteContracts(ids)
                If response.Success = True Then
                    AddMessage("success", "Het contract is verwijderd", "Geslaagd!")
                    Return RedirectToAction("DetailContracts", "Projecten", New With {.projectid = projectid})
                Else
                    AddMessage("error", "Het contract is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("DetailContracts", "Projecten", New With {.projectid = projectid})
                End If
            End If
            Return RedirectToAction("DetailContracts", "Projecten", New With {.projectid = projectid})
        End Function
        <HttpPost>
        Public Function GetCompanyActivities(companyid As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetCompanyService()
            Dim presponse = pservice.GetCompanyActivities(companyid)
            Dim iList As New List(Of ActivityBO)
            If (presponse.Success) Then iList = presponse.Values
            Dim ActivitiesList As New List(Of Select2DTO)()
            Dim singleActivity As Select2DTO
            For Each selectedActivity As ActivityBO In iList
                singleActivity = New Select2DTO()
                singleActivity.id = selectedActivity.ID
                singleActivity.text = selectedActivity.Name
                singleActivity.group = "-Bedrijfsactiviteit-"
                ActivitiesList.Add(singleActivity)
            Next

            Return Json(ActivitiesList, JsonRequestBehavior.AllowGet)
        End Function
        <HttpPost>
        Public Function GetContractActitvities(contractid As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetProjectService()
            Dim presponse = pservice.GetContract(contractid)
            Dim iList As New List(Of ContractActivityBO)
            If (presponse.Success) Then iList = presponse.Value.Activities
            Dim ActivitiesList As New List(Of Select2DTO)()
            Dim singleActivity As Select2DTO
            For Each selectedActivity As ContractActivityBO In iList
                singleActivity = New Select2DTO()
                singleActivity.id = selectedActivity.ContractActivityId
                singleActivity.text = selectedActivity.Activity.Name
                ActivitiesList.Add(singleActivity)
            Next

            Return Json(ActivitiesList, JsonRequestBehavior.AllowGet)
        End Function
        <HttpPost>
        Public Function AddSelectedActivities(ActivityId As String, ActivityName As String) As PartialViewResult
            Dim nContractActivity As New ContractActivityBO
            Dim nActivity As New ActivityBO
            nActivity.ID = ActivityId
            nActivity.Name = ActivityName
            nContractActivity.Activity = nActivity
            ViewData("mode") = "add"
            Return PartialView("_ActivityRow", nContractActivity)

        End Function
        <HttpPost>
        Public Function AddAdditionalOrders(ContractActivityId As String, ActivityName As String) As PartialViewResult
            Dim nAdditionalOrder As New ContractAdditionalOrderBO
            nAdditionalOrder.ContractActivityId = ContractActivityId
            nAdditionalOrder.ActivityName = ActivityName
            ViewData("mode") = "add"
            Return PartialView("_AdditionalOrderRow", nAdditionalOrder)

        End Function
        <HttpGet>
        Function CalculationSettings(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectCalculationSettings
            Dim service = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Dim aservice = ServiceFactory.GetActivityService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            'Get Activities
            Dim response = aservice.GetActivityGroups
            model.ActivityGroups = response.Values
            Dim response2 = service.GetProjectBudget(projectid)
            model.BudgetActivities = response2.Values
            Dim response3 = aservice.GetActivitiesForSelect()
            If (response3.Success) Then model.ListActivities = response3.Values
            If Not model.BudgetActivities.Count = 0 Then
                Dim ilist As New List(Of Integer)
                For Each act In model.BudgetActivities
                    ilist.Add(act.Activity.ID)
                Next
                model.SelectedActivities = ilist
            End If

            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            Return View(model)
        End Function
        <HttpPost>
        Function CalculationSettings(model As ProjectCalculationSettings, budgetactivities As List(Of BudgetActivityBO)) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next
            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                Dim service = ServiceFactory.GetProjectService
                Dim response As New Response
                For Each budgetactivity In budgetactivities
                    budgetactivity.ProjectId = model.ProjectId
                Next
                response = service.InsertUpdateProjectBudgetActivities(budgetactivities, model.ProjectId)
                If response.Success Then
                    AddMessage("success", "De activiteiten zijn aan het budget toegevoegd", "Geslaagd!")
                    Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De activiteiten zijn NIET aan het budget toegevoegd", "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If
        End Function
        <HttpPost>
        Public Function AddBudgetActivitiy(ActId As String, Actname As String, ActGroupID As String) As PartialViewResult
            Dim BudgetActivity As New BudgetActivityBO
            Dim aservice = ServiceFactory.GetActivityService
            Dim response = aservice.GetActivitybyId(ActId)
            Dim act As New ActivityBO
            act = response.Value
            BudgetActivity.Activity = act
            ViewData("mode") = "add"
            Return PartialView("_BudgetActivityRow", BudgetActivity)
        End Function
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailContracts(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailContractsModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectContracts(projectid)
            If (response.Success) Then model.Contracts = response.Values
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpGet>
        Function RecalculationDetail(projectid As Integer, activityid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectRecalculationDetailModel
            Dim service = ServiceFactory.GetProjectService
            Dim aservice = ServiceFactory.GetActivityService
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            'Get Units
            Dim response = aservice.GetActivitybyId(activityid)
            model.Activity = response.Value
            node.Title = model.Activity.Name
            Dim response4 = service.GetProjectIncommingInvoicesByActivity(projectid, activityid)
            model.IncommingInvoicesActivities = response4.Values
            Dim response5 = service.GetProjectContractsWithoutInvoices(projectid, activityid)
            model.ContractsWithoutInvoices = response5.Values
            Dim response6 = service.GetProjectContractActivitiesByActivityId(projectid, activityid)
            If response6.Success Then model.ContractActivities = response6.Values
            Return View(model)

        End Function
        <HttpGet>
        Function PrintRecalculation(projectid As Integer, details As Integer) As ActionResult
            Dim viewmodel As New ProjectContractsModel

            Dim pservice = ServiceFactory.GetProjectService
            Dim aservice = ServiceFactory.GetActivityService
            ViewBag.detail = details
            viewmodel.ProjectId = projectid
            viewmodel.ProjectName = pservice.GetProjectNameById(projectid)
            'Get Units
            Dim response = aservice.GetActivityGroups
            viewmodel.ActivityGroups = response.Values
            Dim response2 = pservice.GetProjectContracts(projectid)
            viewmodel.Contracts = response2.Values
            Dim response3 = pservice.GetProjectBudget(projectid)
            viewmodel.BudgetActivities = response3.Values
            Dim response4 = pservice.GetProjectIncommingInvoicesForRecalculation(projectid)
            viewmodel.IncommingInvoicesActivities = response4.Values

            Dim a = New ViewAsPdf()
            a.ViewName = "PrintRecalculation"
            a.Model = viewmodel
            a.PageOrientation = Options.Orientation.Portrait
            a.PageSize = Options.Size.A4
            a.IsGrayScale = False
            a.FileName = "Nacalculatie - " & viewmodel.ProjectName & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & ".pdf"
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
        End Function

        'CHANGEORDERS
        <HttpGet>
        Function ChangeOrder(projectid As Integer, Optional clientaccountid As Integer = 0) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectChangeOrderModel
            Dim service2 = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            Dim response = service2.GetProjectChangeOrders(projectid)
            If (response.Success) Then model.ChangeOrders = response.Values
            model.VatPercentage = service2.GetProjectVatPercentage(projectid)
            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            Return View(model)

        End Function
        <HttpGet>
        Function ChangeOrderAddUpdate(projectid As Integer, Optional changeorderid As Integer = 0, Optional duplicate As Boolean = False) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectChangeOrderAddUpdateModel
            Dim service2 = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            If Not changeorderid = 0 Then
                Dim chresponse = service2.GetChangeOrder(changeorderid)
                If (chresponse.Success) Then model.ChangeOrder = chresponse.Values.FirstOrDefault
                If duplicate = True Then
                    model.ChangeOrder.Id = 0
                    For Each Detail As ChangeOrderDetailBO In model.ChangeOrder.Details
                        If model.ChangeOrder.ChangeOrderConditions = Nothing Then
                            model.ChangeOrder.ChangeOrderConditions = "Het bedrag zal verrekend worden bij de laatste facturatieschijf 'voorlopige oplevering'"
                        End If
                        Detail.Id = 0
                        Detail.ChangeOrderID = 0
                    Next
                    model.ChangeOrder.DateAgreement = Nothing
                    model.ChangeOrder.DateSendToClient = Nothing
                End If
            Else
                model.ChangeOrder.ChangeOrderConditions = "Het bedrag zal verrekend worden bij de laatste facturatieschijf 'voorlopige oplevering'"
                model.ChangeOrder.ChangeOrderDate = Date.Now()
                model.ChangeOrder.ExpirationDate = Date.Now.AddDays(30)
            End If

            If model.ChangeOrder.Details.Count = 0 Then
                Dim detailrow As New ChangeOrderDetailBO
                detailrow.MeasurementType = 1
                detailrow.MeasurementUnit = 1
                detailrow.Number = 1
                model.ChangeOrder.Details.Add(detailrow)
            End If
            ChangeOrderFillInSelectList(model)
            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            Return View(model)

        End Function
        <ValidateInput(False)>
        <HttpPostAttribute>
        Function ChangeOrderAddUpdate(model As ProjectChangeOrderAddUpdateModel, details As List(Of ChangeOrderDetailBO)) As ActionResult
            Dim errors As New ArrayList
            For Each Detail As ChangeOrderDetailBO In details
                model.ChangeOrder.Details.Add(Detail)
            Next
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors.Add(ModelState(key).Errors())
                End If
            Next
            If (Not ModelState.IsValid) Then
                ChangeOrderFillInSelectList(model)
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Return View(model)
            End If
            If (ModelState.IsValid) Then

                Dim service = ServiceFactory.GetProjectService
                Dim response = service.InsertUpdateProjectChangeOrder(model.ChangeOrder)
                If response.Success Then
                    AddMessage("success", "De wijzigingsopdracht is toegevoegd", "Geslaagd!")
                    Return RedirectToAction("ChangeOrder", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De wijzigingsopdracht is NIET toegevoegd", "Fout!")
                    ChangeOrderFillInSelectList(model)
                    ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                    Return View(model)
                End If
            Else
                ChangeOrderFillInSelectList(model)
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                Return View(model)
            End If


        End Function
        <HttpPost>
        Public Function AddChangeOrderDetailRow() As PartialViewResult
            Dim nChangeOrderDetail As New ChangeOrderDetailBO
            nChangeOrderDetail.MeasurementType = 1
            nChangeOrderDetail.MeasurementUnit = 1
            nChangeOrderDetail.Number = 1
            ViewData("mode") = "add"
            Return PartialView("_ChangeOrderDetailRow", nChangeOrderDetail)
        End Function
        <HttpGet>
        Function ModalDeleteChangeOrder(id As Integer) As ActionResult
            Dim viewModel = New ChangeOrderBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetChangeOrder(id).Values.FirstOrDefault
            End If
            Return PartialView("ModalDeleteChangeOrder", viewModel)
        End Function
        Function DeleteChangeOrder(id As Integer, projectid As Integer) As ActionResult
            If Not id = 0 And Not projectid = 0 Then
                Dim service = ServiceFactory.GetProjectService()
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = service.DeleteChangeOrders(ids)
                If response.Success = True Then
                    AddMessage("success", "De wijzigingsopdracht is verwijderd", "Geslaagd!")
                    Return RedirectToAction("ChangeOrder", "Projecten", New With {.projectid = projectid})
                Else
                    AddMessage("error", "De wijzigingsopdracht is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("ChangeOrder", "Projecten", New With {.projectid = projectid})
                End If
            End If
            Return RedirectToAction("ChangeOrder", "Projecten", New With {.projectid = projectid})
        End Function
        Public Function ChangeOrderDetailTable(ByVal ChangeOrderID As Integer?) As ActionResult
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetChangeOrder(ChangeOrderID)
            Dim changeOrder = response.Values.FirstOrDefault
            Return PartialView(changeOrder)
        End Function
        <HttpGet>
        Public Function ChangeOrderPDF(changeorderid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectChangeOrderExportModel
            Dim service = ServiceFactory.GetClientService
            Dim service2 = ServiceFactory.GetProjectService
            Dim service3 = ServiceFactory.GetUnitService
            Dim response = service2.GetChangeOrder(changeorderid)
            If (response.Success) Then model.ChangeOrder = response.Values.FirstOrDefault
            Dim response2 = service2.GetProjectByID(model.ChangeOrder.ProjectId)
            If (response2.Success) Then model.Project = response2.Values.FirstOrDefault
            Dim response3 = service2.GetSalesSettings(model.Project.Id)
            If (response3.Success) Then model.ProjectSalesSettings = response3.Values.FirstOrDefault
            Dim response4 = service.GetClientAccountById(model.ChangeOrder.ClientAccountID)
            If (response4.Success) Then model.ClientAccount = response4.Values.FirstOrDefault
            model.Units = service.GetClientAccountUnitsNameById(model.ChangeOrder.ClientAccountID)

            Dim a = New ViewAsPdf()
            Dim customSwitches As String = String.Format("--footer-html {0} --footer-spacing 0", Url.Action("ChangeOrderFooter", "Projecten", New With {Key .text = model.ChangeOrder.ChangeOrderConditions}, "http"))
            a.ViewName = "ChangeOrderPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Portrait
            a.PageMargins = New Options.Margins(10, 5, 40, 5)
            a.PageSize = Options.Size.A4
            a.FileName = "Wijzigingsopdracht - " & model.Project.Name & " " & Date.Now.Year & Date.Now.Month & Date.Now.Day & "_" & model.ChangeOrder.Id & ".pdf"
            a.CustomSwitches = customSwitches

            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Return a
            'Dim ms As New MemoryStream(pdfBytes)
            'Return New FileStreamResult(ms, "application/pdf")
        End Function
        <AllowAnonymous>
        <HttpGet>
        Public Function ChangeOrderFooter(text As String) As PartialViewResult
            Return PartialView("ChangeOrderFooter", text)
        End Function
        <HttpPost>
        Sub ChangeOrderDetailInvoicable(CODetailid As Integer, value As Boolean)
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.UpdateProjectChangeOrderDetailInvoicable(CODetailid, value)
            If response.Success Then
            Else
                AddMessage("Error", "De wijzigingsopdracht Is niet aangepast, probeer het later opnieuw", "Fout!")
            End If
        End Sub
        <HttpPost>
        Sub ChangeOrderInvoicable(COid As Integer, value As Boolean)
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.UpdateProjectChangeOrderInvoicable(COid, value)
            If response.Success Then
            Else
                AddMessage("Error", "De wijzigingsopdracht Is niet aangepast, probeer het later opnieuw", "Fout!")
            End If
        End Sub

        'INCOMMING INVOICES
        <HttpGet>
        Function IncommingInvoiceAdd(projectid As Integer, type As Integer, Optional invoiceid As Integer = 0) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New ProjectIncommingInvoiceAddUpdateModel
            Dim service2 = ServiceFactory.GetProjectService
            model.ProjectId = projectid
            model.ProjectName = service2.GetProjectNameById(projectid)
            'get the activities
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()

            If (response.Success) Then model.ListActivities = response.Values
            model.ListActivities.OrderBy(Function(m) m.Group)
            If type = 0 Then
                Dim i As New IdNameBO
                i.Group = "Contractactiviteiten"
                model.ListActivities.Insert(0, i)
            ElseIf type = 1 Then

                Dim i As New IdNameBO
                i.Group = "Bedrijfsactiviteiten"
                model.ListActivities.Insert(0, i)
            End If

            If Not invoiceid = 0 Then

                Dim chresponse = service2.GetIncommingInvoice(invoiceid)
                If (chresponse.Success) Then model.IncommingInvoice = chresponse.Values.FirstOrDefault
                model.Type = type
            Else
                model.Type = type
                model.IncommingInvoice.IncommingInvoiceDate = Date.Now()
            End If
            IncommingInvoiceFillInSelectList(model)
            'Set sitemap names
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If
            ViewBag.returnUrl = Request.UrlReferrer
            Return View(model)
        End Function
        <ValidateInput(False)>
        <HttpPostAttribute>
        Function IncommingInvoiceAdd(model As ProjectIncommingInvoiceAddUpdateModel, details As List(Of IncommingInvoiceDetailBO), returnUrl As String) As ActionResult
            Dim errors As New ArrayList
            For Each invoicerow In details
                invoicerow.IncommingInvoiceID = model.IncommingInvoice.Id
                model.IncommingInvoice.Details.Add(invoicerow)
            Next
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next
            If model.IncommingInvoice.InvoicePrice <> details.Sum(Function(m) m.Price) Then
                ModelState.AddModelError("CustomError", "De prijs van de factuuronderdelen komt niet overeen met de totale factuurprijs")
            End If

            If (Not ModelState.IsValid) Then
                IncommingInvoiceFillInSelectList(model)
                If model.IncommingInvoice.ContractID <> 0 Then
                    'Dim pservice = ServiceFactory.GetProjectService()
                    'Dim presponse = pservice.GetContract(model.IncommingInvoice.ContractID)
                    'Dim iList As New List(Of ContractActivityBO)
                    'If (presponse.Success) Then iList = presponse.Value.Activities
                    'Dim singleActivity As CPM.Select2DTO
                    'For Each selectedActivity As ContractActivityBO In iList
                    '    singleActivity = New CPM.Select2DTO()
                    '    singleActivity.id = selectedActivity.ContractActivityId
                    '    singleActivity.text = selectedActivity.Activity.Name
                    '    model.Activities.Add(singleActivity)
                    'Next

                End If
                Dim service = ServiceFactory.GetActivityService()
                Dim response = service.GetActivitiesForSelect()
                If (response.Success) Then model.ListActivities = response.Values
                model.ListActivities.OrderBy(Function(m) m.Group)
                If model.Type = 0 Then
                    Dim i As New IdNameBO
                    i.Group = "Contractactiviteiten"
                    model.ListActivities.Insert(0, i)
                ElseIf model.Type = 1 Then

                    Dim i As New IdNameBO
                    i.Group = "Bedrijfsactiviteiten"
                    model.ListActivities.Insert(0, i)
                End If
                ViewBag.returnurl = returnUrl
                Return View(model)
            End If
            If (ModelState.IsValid) Then


                Dim service = ServiceFactory.GetProjectService
                model.IncommingInvoice.ProjectId = model.ProjectId
                Dim response = service.InsertUpdateProjectIncommingInvoice(model.IncommingInvoice)
                If response.Success Then
                    AddMessage("success", "De factuur is toegevoegd aan het project " & model.ProjectName, "Geslaagd!")
                    Return Redirect(returnUrl)
                    'Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De factuur is NIET toegevoegd aan het project " & model.ProjectName, "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If
        End Function
        <HttpPost>
        Public Function AddIncommingInvoiceDetailRow(ActivityId As Integer, ActivityName As String, ContractId As Integer, CompanyId As Integer) As PartialViewResult
            Dim nIncommingInvoiceDetail As New IncommingInvoiceDetailBO
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetContractChangeOrdersForSelect(ContractId)
            If (response.Success) Then nIncommingInvoiceDetail.ChangeOrders = response.Values
            If ContractId = 0 Then
                nIncommingInvoiceDetail.ActivityID = ActivityId
                nIncommingInvoiceDetail.ContractActivityText = ActivityName
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Geen_Contract
            End If
            If CompanyId = 0 Then
                nIncommingInvoiceDetail.ContractActivityID = ActivityId
                nIncommingInvoiceDetail.ContractActivityText = ActivityName
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Contract
            End If

            ViewData("mode") = "add"

            Return PartialView("_IncommingInvoiceDetailRow", nIncommingInvoiceDetail)
        End Function
        <HttpGet>
        Function ModalDeleteIncommingInvoice(id As Integer) As ActionResult
            Dim viewModel = New IncommingInvoiceBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetProjectService
                viewModel = dservice.GetIncommingInvoice(id).Values.FirstOrDefault
            End If
            Return PartialView("ModalDeleteIncommingInvoice", viewModel)
        End Function
        Function DeleteIncommingInvoice(id As Integer, projectid As Integer) As ActionResult
            If Not id = 0 And Not projectid = 0 Then
                Dim service = ServiceFactory.GetProjectService()
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = service.DeleteIncommingInvoices(ids)
                If response.Success = True Then
                    AddMessage("success", "De factuur is verwijderd", "Geslaagd!")
                    Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = projectid})
                Else
                    AddMessage("error", "De factuur is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = projectid})
                End If
            End If
            Return RedirectToAction("Recalculation", "Projecten", New With {.projectid = projectid})
        End Function

        'INSURANCES
        <HttpGet>
        <SiteMapTitle("ProjectName", Target:=AttributeTarget.ParentNode)>
        Function DetailInsurances(projectid As Integer) As ActionResult
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
            Dim model As New DetailInsurancesModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectInsurances(projectid)
            If (response.Success) Then model.Insurances = response.Values
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            Return View(model)
        End Function
        <HttpGet>
        Function ModalAddInsurance(id As Integer) As ActionResult
            Dim viewModel = New ProjectAddInsurancesModel
            viewModel.ProjectId = id
            viewModel.Insurance.Startdate = Date.Now()
            Dim service = ServiceFactory.GetInsuranceService
            Dim cservice = ServiceFactory.GetCompanyService
            Dim cresponse = cservice.GetCompanyForSelectByActivity(142)
            If cresponse.Success Then viewModel.Brokers = cresponse.Values
            Dim response = service.GetInsuranceCompaniesForSelect()
            If response.Success Then viewModel.Companies = response.Values

            Return PartialView("ModalAddInsurance", viewModel)
        End Function
        <HttpPost>
        Function AddInsurance(viewmodel As ProjectAddInsurancesModel) As ActionResult

            Dim response As New Response
            If ModelState.IsValid Then
                viewmodel.Insurance.ProjectID = viewmodel.ProjectId
                Dim service = ServiceFactory.GetInsuranceService
                response = service.InsertUpdate(viewmodel.Insurance)
            End If
            If response.Success = True Then
                AddMessage("success", "De verzekering is toegevoegd", "Geslaagd!")
                Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.Insurance.ProjectID})
            Else
                AddMessage("error", "De verzekering is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.Insurance.ProjectID})
            End If
            Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.Insurance.ProjectID})

        End Function
        <HttpGet>
        Function ModalDeleteInsurance(id As Integer) As ActionResult
            Dim viewModel = New InsuranceBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetInsuranceService
                viewModel = dservice.GetInsuranceById(id).Value
            End If
            Return PartialView("ModalDeleteInsurance", viewModel)
        End Function
        Function DeleteInsurance(id As Integer, projectid As Integer) As ActionResult

            If Not id = 0 And Not projectid = 0 Then
                Dim service = ServiceFactory.GetInsuranceService()
                Dim ids As New List(Of Integer)
                ids.Add(id)
                Dim response = service.Delete(ids)
                If response.Success = True Then
                    AddMessage("success", "De verzekering is verwijderd", "Geslaagd!")
                    Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = projectid})
                Else
                    AddMessage("error", "De verzekering is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = projectid})
                End If
            End If
            Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = projectid})
        End Function
        <HttpGet>
        Function ModalEndInsurance(id As Integer) As ActionResult
            Dim viewModel = New InsuranceBO

            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetInsuranceService
                viewModel = dservice.GetInsuranceById(id).Value
                If viewModel.Type = InsuranceType.ABR Then
                    viewModel.Enddate = viewModel.Startdate.AddMonths(viewModel.Period + viewModel.ExtensionPeriod + viewModel.GuaranteePeriod)
                Else
                    viewModel.Enddate = Date.Now()
                End If
            End If
            Return PartialView("ModalEndInsurance", viewModel)
        End Function
        <HttpPost>
        Function EndInsurance(viewmodel As InsuranceBO) As ActionResult
            Dim response As New Response
            If ModelState.IsValid Then
                Dim service = ServiceFactory.GetInsuranceService
                response = service.InsertUpdate(viewmodel)
            End If
            If response.Success = True Then
                AddMessage("success", "De verzekering is beëindigd", "Geslaagd!")
                Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.ProjectID})
            Else
                AddMessage("error", "De verzekering is NIET beëindigd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.ProjectID})
            End If
            Return RedirectToAction("DetailInsurances", "Projecten", New With {.projectid = viewmodel.ProjectID})

        End Function
        <HttpGet>
        Function ModalEditInsurance(id As Integer) As ActionResult

            Dim viewModel = New ProjectAddInsurancesModel
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetInsuranceService
                viewModel.Insurance = dservice.GetInsuranceById(id).Value
            End If
            viewModel.ProjectId = viewModel.Insurance.ProjectID
            Dim service = ServiceFactory.GetInsuranceService
            Dim cservice = ServiceFactory.GetCompanyService
            Dim cresponse = cservice.GetCompanyForSelectByActivity(142)
            If cresponse.Success Then viewModel.Brokers = cresponse.Values
            Dim response = service.GetInsuranceCompaniesForSelect()
            If response.Success Then viewModel.Companies = response.Values
            Return PartialView("ModalAddInsurance", viewModel)
        End Function
        'UTILITY COST
        Public Function GetClientUtilityCost(clientid As Integer, projectid As Integer) As ClientUtilityCostBO
            Dim clientUC As New ClientUtilityCostBO
            Dim client As New ClientAccountBO
            Dim utilitycost As New List(Of UtilityCostBO)
            'SERVICES
            Dim service = ServiceFactory.GetClientService
            Dim pservice = ServiceFactory.GetProjectService
            Dim uservice = ServiceFactory.GetUnitService
            'GET CLIENT
            Dim response = service.GetClientAccountById(clientid)
            If (response.Success) Then client = response.Value
            'GET UTILITYCOST
            Dim presponse = pservice.GetProjectUtilityCost(projectid, clientid)
            If (presponse.Success) Then utilitycost = presponse.Values
            clientUC.ClientAccountId = client.Id
            clientUC.Clientname = client.DisplayName
            clientUC.UtilityCost = utilitycost
            Return clientUC
        End Function


        'CONTRACTS


        'SHARED
        Public Sub ChangeOrderFillInSelectList(model As ProjectChangeOrderAddUpdateModel)
            Dim service2 = ServiceFactory.GetProjectService
            Dim service = ServiceFactory.GetClientService
            Dim cresponse = service.GetClientAccountsByProjectIdForSelect(model.ProjectId)
            If (cresponse.Success) Then model.ClientAccounts = cresponse.Values
            Dim aresponse = service2.GetProjectContractActivitiesForSelect(model.ProjectId)
            If (aresponse.Success) Then model.ProjectContractActivities = aresponse.Values
        End Sub
        Public Sub IncommingInvoiceFillInSelectList(model As ProjectIncommingInvoiceAddUpdateModel)
            Dim service2 = ServiceFactory.GetProjectService
            'Dim service = ServiceFactory.GetClientService
            ''Dim cresponse = service.GetClientAccountsByProjectIdForSelect(model.ProjectId)
            ''If (cresponse.Success) Then model.ClientAccounts = cresponse.Values
            Dim aresponse = service2.GetProjectContractsForSelect(model.ProjectId)
            If (aresponse.Success) Then model.ProjectContracts = aresponse.Values
        End Sub
        Public Function GetCompanyName(companyid As Integer) As String
            Dim pservice = ServiceFactory.GetCompanyService
            Dim presponse = pservice.GetCompanyNameById(companyid)
            Return presponse.ToString()
        End Function
        'SHARED
        Private Sub FillInAddSelectLists(ByRef model As ProjectModel)
            'get the activities
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID

        End Sub
        Private Sub FillInAddSelectListsDetail(ByRef model As ShowProjectDetail)
            'get the activities
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID
            'Get statuses
            Dim service = ServiceFactory.GetProjectService()
            Dim response = service.GetStatusesForSelect()
            If (response.Success) Then model.Statuses = response.Values
            model.SelectedStatus = model.Project.Status.Id

        End Sub
        Private Sub FillInAddSelectListsDetailEdit(ByRef model As EditProjectDetail)
            'get the activities
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID
            'Get statuses
            Dim service = ServiceFactory.GetProjectService()
            Dim response = service.GetStatusesForSelect()
            If (response.Success) Then model.Statuses = response.Values
            model.SelectedStatus = model.Project.Status.Id

        End Sub

        <HttpPost>
        Function GetCountryIsoCode(countryid As Integer) As String
            Dim pservice = ServiceFactory.GetCountryService
            Dim presponse = pservice.GetCountryById(countryid)
            Dim iPostcode As New CountryBO
            If (presponse.Success) Then iPostcode = presponse.Values.FirstOrDefault
            Return iPostcode.ISOCode
        End Function

        <HttpPost>
        Public Function GetPostcodesByCountry(term As String, CountryId As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetPostalcodeService()
            Dim presponse = pservice.GetPostalcodeByCountryAndSearchstring(CountryId, term)
            Dim iList As New List(Of PostalCodeBO)
            If (presponse.Success) Then iList = presponse.Values
            Dim PostalcodeList As New List(Of SelectBO)()
            Dim singlePostalcode As SelectBO
            For Each selectedPostalcode As PostalCodeBO In iList
                singlePostalcode = New SelectBO()
                singlePostalcode.id = selectedPostalcode.PostcodeId
                singlePostalcode.text = selectedPostalcode.Postcode & " - " & selectedPostalcode.Gemeente
                PostalcodeList.Add(singlePostalcode)
            Next

            Return Json(PostalcodeList, JsonRequestBehavior.AllowGet)
        End Function

        <HttpPost>
        Public Function GetCompanys(term As String) As JsonResult
            Dim pservice = ServiceFactory.GetCompanyService
            Dim presponse = pservice.GetCompanyForSearchList(term)
            Dim iList As New List(Of SelectBO)
            If (presponse.Success) Then iList = presponse.Values
            Return Json(iList, JsonRequestBehavior.AllowGet)
        End Function

        <HttpPost>
        Public Function GetWheaterstations(term As String) As JsonResult
            Dim pservice = ServiceFactory.GetProjectService()
            Dim presponse = pservice.GetWheaterstations(term)
            Dim iList As New List(Of SelectBO)
            If (presponse.Success) Then
                For Each x In presponse.Values
                    Dim bo As New SelectBO
                    bo.id = x.Id
                    bo.text = x.Name
                    bo.extra = x.Visible
                    iList.Add(bo)
                Next
            End If
            Return Json(iList, JsonRequestBehavior.AllowGet)
        End Function
        Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
            TempData("Message") = message
            TempData("MessageType") = messagetype
            TempData("MessageTitle") = messagetitle
        End Sub
        Public Function GetSlugForPostcodeId(id As Integer, name As String) As String
            Dim sCity As String = ""
            If Not id = 0 Then
                Dim cityService = ServiceFactory.GetPostalcodeService
                Dim Postalcode As PostalCodeBO = cityService.GetPostalcodeById(id).Value
                sCity = Postalcode.Gemeente
            End If
            Return GenerateSlug(name & " " & sCity)
        End Function
        Public Function GetNewInvoiceId() As Integer
            Dim path As String = My.Settings.InvoiceURL & Date.Now.Year & "\"
            CheckDir(path)
            Dim lastFileNo As Integer = 0
            Dim files = IO.Directory.GetFiles(path, "*.doc*").Where(Function(f) (New FileInfo(f).Attributes And FileAttributes.Hidden) = 0).ToArray()
            For Each file As String In files
                Dim number As Integer
                number = GetNumberFromFilename(IO.Path.GetFileName(file))
                If number > 0 And number > lastFileNo Then
                    lastFileNo = number
                End If
            Next
            Return lastFileNo + 1
        End Function
        Public Function GetNewInvoiceIdFTP() As Integer
            Dim ftp As Chilkat.Ftp2 = ConnectToFtp()
            Dim path As String = My.Settings.InvoiceURLFTP & Date.Now.Year
            CheckDirFTP(path, ftp)
            Dim lastFileNo As Integer = 0
            ' Retrieve (in XML format) the HOME directory of this FTP account.
            Dim xmlListing As String = ftp.GetXmlDirListing(path)
            If (ftp.LastMethodSuccess <> True) Then
                MakeLog("Get xmlListing", ftp.LastErrorText)
                Exit Function
            End If
            ' Now load the XML and parse it..
            Dim xml As New Chilkat.Xml
            xml.LoadXml(xmlListing)
            Debug.WriteLine(xml.GetXml())
            Dim i As Integer = 0
            Dim numEntries As Integer = xml.NumChildren
            While i < numEntries
                Dim xEntry As Chilkat.Xml = xml.GetChild(i)
                'Dim sz As Integer = xEntry.GetChildIntValue("size")
                'Debug.WriteLine("File: " & xEntry.GetChildContent("name") & ", size: " & sz)
                Dim filename As String = xEntry.GetChildContent("name")
                Dim fileExtPos As Integer = filename.LastIndexOf(".")
                If fileExtPos >= 0 Then
                    filename = filename.Substring(0, fileExtPos)
                End If
                Dim number As Integer
                number = GetNumberFromFilename(filename)
                If number > 0 And number > lastFileNo Then
                    lastFileNo = number
                End If
                i = i + 1
            End While
            ftp.Disconnect()
            Return lastFileNo + 1
        End Function
        Public Function GetNewInvoiceFilename(invoiceid As Integer, salutation As Salutation, name As String, firstname As String, companyname As String) As String
            Dim NewFilename As String = ""
            If salutation = Salutation.Dhr Or salutation = Salutation.Mevr Then
                NewFilename = invoiceid.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & name & " " & firstname & ".docx"
            Else
                If name Is Nothing Or name = "" Then
                    NewFilename = invoiceid.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & companyname & ".docx"
                Else
                    NewFilename = invoiceid.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & name & ".docx"
                End If
            End If
            Return NewFilename
        End Function
        Public Function MakeInvoiceWordDoc(filename As String) As WordprocessingDocument
            System.IO.File.Copy(My.Settings.TemplateInvoiceBCO, My.Settings.InvoiceURL & Date.Now.Year & "\" & filename)
            Dim document As WordprocessingDocument = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & filename, True)
            document.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document)
            Return document
        End Function
        Public Function MakeInvoiceWordDocFTP(filename As String, ftp As Chilkat.Ftp2) As WordprocessingDocument
            Dim success As Boolean
            success = ftp.ChangeRemoteDir(My.Settings.InvoiceURLFTP & Date.Now.Year)
            If (success <> True) Then
                MakeLog("Change ftp directory to " & My.Settings.InvoiceURLFTP & Date.Now.Year, ftp.LastErrorText)
                Exit Function
            End If
            success = ftp.PutFile(My.Settings.TemplateInvoiceBCO, filename)
            If (success <> True) Then
                MakeLog("Copy file from " & My.Settings.TemplateInvoiceBCO & " to ftp " & filename, ftp.LastErrorText)
                Exit Function
            End If
            System.IO.File.Copy(My.Settings.TemplateInvoiceBCO, My.Settings.localTempPath & filename)
            Dim document As WordprocessingDocument = WordprocessingDocument.Open(My.Settings.localTempPath & filename, True)
            document.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document)
            Return document
        End Function
        Public Function InvoiceFillGeneralInfo(doc As WordprocessingDocument, invoiceid As Integer, vatnumber As String, name As String, firstname As String, salutation As Salutation, companyname As String, street As String, housenumber As String, busnumber As String, postalcode As PostalCodeBO, bankaccountnumber As String, extrainfo As String, invoice As InvoiceBO) As (returndoc As WordprocessingDocument, invoice As InvoiceBO)
            'GEGEVENS INVULLEN
            invoice.PublicId = invoiceid.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year
            invoice.Invoicedate = Date.Now()
            invoice.ExpirationDate = Date.Now.AddDays(14)
            If Not vatnumber Is Nothing Or Not vatnumber = "" Then
                invoice.VatNumber = vatnumber
            Else
                invoice.VatNumber = ""
            End If
            If Not name Is Nothing Or Not name = "" Then
                If salutation = Salutation.Dhr Or salutation = Salutation.Mevr Then
                    invoice.ClientName = salutation.GetDisplayName() & " " & name & " " & firstname
                Else
                    invoice.ClientName = salutation.GetDisplayName() & " " & name
                End If
            Else
                invoice.ClientName = companyname
            End If
            If busnumber Is Nothing Or busnumber = "" Then
                invoice.Adress = street & " " & housenumber
            Else
                invoice.Adress = street & " " & housenumber & "/" & busnumber
            End If
            invoice.PostalCode = postalcode
            If Not bankaccountnumber Is Nothing Or Not bankaccountnumber = "" Then
                invoice.BankAccount = bankaccountnumber
            Else
                invoice.BankAccount = "BE68 0015 1882 9434"
            End If
            If Not extrainfo Is Nothing Or Not extrainfo = "" Then
                invoice.ExtraInfo = extrainfo
            Else
                invoice.ExtraInfo = ""
            End If


            SearchAndReplace(doc, "#FACTUURNUMMER#", invoice.PublicId)
            SearchAndReplace(doc, "#FACTUURDATUM#", invoice.Invoicedate.ToString("dd MMMM yyyy"))
            SearchAndReplace(doc, "#VERVALDATUM#", invoice.ExpirationDate.ToString("dd MMMM yyyy"))
            SearchAndReplace(doc, "#BTWNUMMER#", invoice.VatNumber)
            SearchAndReplace(doc, "#KLANTNAAM#", invoice.ClientName)
            SearchAndReplace(doc, "#STRAAT#", invoice.Adress)

            SearchAndReplace(doc, "#GEMEENTE#", postalcode.Postcode & " " & postalcode.Gemeente)
            If Not postalcode.Country.CountryID = 19 Then
                SearchAndReplace(doc, "#LAND#", postalcode.Country.Name)
            Else
                SearchAndReplace(doc, "#LAND#", "")
            End If
            SearchAndReplace(doc, "#REKENINGNUMMER#", invoice.BankAccount)
            SearchAndReplace(doc, "#EXTRAINFO#", invoice.ExtraInfo)

            Return (doc, invoice)
        End Function
        Public Function InvoiceFillDetails(doc As WordprocessingDocument, ownerpercentage As Decimal, iu As List(Of UnitWithStagesBO), project As ProjectBO, invoice As InvoiceBO) As (returndoc As WordprocessingDocument, invoice As InvoiceBO)
            Dim Tekst As String = ""
            Dim eenheidsprijzen As String = ""
            Dim detailtext As String = ""
            If ownerpercentage = 100 Then
                Tekst = "Voor de bouwwaarde van "
            Else
                Tekst = "Voor " & ownerpercentage.ToString("#0.##") & " % van de bouwwaarde van "
            End If

            Dim count As Integer = 0
            Dim tblSchijven As DocumentFormat.OpenXml.Wordprocessing.Table = doc.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
            Dim theRow As TableRow = tblSchijven.Elements(Of TableRow)().ElementAt(1)
            iu = iu.OrderBy(Function(m) m.Unit.Type.GroupId).ToList()
            Dim rowcount As Integer = 0
            Dim TotalPrice0 As Decimal = 0
            Dim TotalPrice6 As Decimal = 0
            Dim TotalPrice21 As Decimal = 0
            For Each iunit In iu
                'FACTUURTEKST OPMAKEN
                Tekst = Tekst & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name
                If iunit.Unit.ConstructionValues.Sum(Function(m) m.ValueSold) > 0 Then
                    eenheidsprijzen = eenheidsprijzen & ownerpercentage.ToString("#0.##") & " % van de bouwwaarde van " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name & " : " & FormatCurrency(iunit.Unit.ConstructionValues.Sum(Function(m) m.ValueSold) * ownerpercentage / 100)
                    detailtext = detailtext & ownerpercentage.ToString("#0.##") & " % van de bouwwaarde van " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name & " : " & FormatCurrency(iunit.Unit.ConstructionValues.Sum(Function(m) m.ValueSold) * ownerpercentage / 100)
                    If Not iunit Is iu.Last Then
                        eenheidsprijzen &= vbLf
                        detailtext &= "\n"
                        If Not iunit Is iu(iu.Count - 2) Then
                            Tekst &= ", "
                        Else
                            Tekst &= " en "
                        End If
                    End If
                    count += 1
                    'SCHIJVEN OPMAKEN
                    Dim rowCopy As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                    Dim run As New Run
                    Dim runProperties = GetRunPropertyFromTableCell(rowCopy, 0)
                    runProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
                    run.AppendChild(Of RunProperties)(runProperties)
                    run.AppendChild(New Text("Voor " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name))
                    rowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                    rowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run))
                    tblSchijven.InsertAfter(rowCopy, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                    rowcount += 1
                    For Each cv In iunit.Unit.ConstructionValues.Where(Function(m) m.ValueSold > 0)
                        For Each stage In iunit.PaymentStages.Where(Function(x) x.GroupId = cv.PaymentGroupId)

                            Dim rowCopy2 As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                            Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
                            Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
                            Dim run2 = New Run(New Text(stage.Percentage.ToString("0.##") & " % - " & stage.Name))
                            run2.PrependChild(Of RunProperties)(runProperties2)
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
                            Dim runProperties3 = GetRunPropertyFromTableCell(rowCopy2, 0)
                            Dim run3 = New Run(New Text(stage.VatPercentage.ToString("0.##") & " %"))
                            run3.PrependChild(Of RunProperties)(runProperties3)
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(New Run(run3)))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
                            Dim stagePrice As Decimal = (cv.ValueSold * ownerpercentage / 100) * stage.Percentage / 100
                            Dim runValue As New Run
                            runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
                            runValue.AppendChild(Of RunProperties)(runPropertiesValue)
                            runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
                            tblSchijven.InsertAfter(rowCopy2, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                            rowcount += 1
                            If stage.VatPercentage = 0 Then
                                TotalPrice0 = TotalPrice0 + stagePrice
                            ElseIf stage.VatPercentage = 6 Then
                                TotalPrice6 = TotalPrice6 + stagePrice
                            ElseIf stage.VatPercentage = 21 Then
                                TotalPrice21 = TotalPrice21 + stagePrice
                            End If
                            Dim invoicerow As New InvoiceRowBO
                            invoicerow.StageId = stage.Id
                            invoicerow.UnitId = iunit.Unit.Id
                            invoicerow.ConstructionValueId = cv.Id
                            invoicerow.VatPercentage = stage.VatPercentage
                            invoicerow.Text = stage.Percentage.ToString("0.##") & " % - " & stage.Name
                            invoicerow.Price = (cv.ValueSold * ownerpercentage / 100) * stage.Percentage / 100
                            invoicerow.GroupName = Char.ToUpper(iunit.Unit.Type.Name(0)) & iunit.Unit.Type.Name.Substring(1) & " " & iunit.Unit.Name
                            invoice.Rows.Add(invoicerow)

                        Next
                    Next

                End If
            Next

            doc.MainDocumentPart.Document.Save()
            doc.Close()
            doc = WordprocessingDocument.Open(My.Settings.localTempPath & invoice.Filename, True)
            Tekst &= " in project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."
            SearchAndReplace(doc, "#FACTUURTEKST#", Tekst)
            SearchAndReplaceWithNewLines(doc, "#EENHEIDSPRIJS#", eenheidsprijzen)
            invoice.Text = Tekst
            invoice.DetailText = detailtext
            doc.MainDocumentPart.Document.Save()
            doc.Close()
            doc = WordprocessingDocument.Open(My.Settings.localTempPath & invoice.Filename, True)
            Dim TotalVAT6 As Decimal = 0
            If Not TotalPrice6 = 0 Then
                TotalVAT6 = TotalPrice6 * 6 / 100
            End If
            Dim TotalVAT21 As Decimal = 0
            If Not TotalPrice21 = 0 Then
                TotalVAT21 = TotalPrice21 * 21 / 100
            End If
            Dim Total As Decimal = 0
            Dim TotalExVat As Decimal = 0
            Dim TotalVat As Decimal = 0
            TotalExVat = TotalPrice0 + TotalPrice6 + TotalPrice21
            TotalVat = TotalVAT6 + TotalVAT21
            Total = TotalExVat + TotalVat
            'Fill in Totals table
            SearchAndReplace(doc, "#TOTAALEXBTW#", FormatCurrency(TotalExVat))
            If TotalPrice0 = 0 Then
                SearchAndReplace(doc, "#MVH0#", "")
                SearchAndReplace(doc, "#BTW0#", "")
            Else
                SearchAndReplace(doc, "#MVH0#", FormatCurrency(TotalPrice0))
                SearchAndReplace(doc, "#BTW0#", "")
            End If
            If TotalPrice6 = 0 Then
                SearchAndReplace(doc, "#MVH6#", "")
                SearchAndReplace(doc, "#BTW6#", "")
            Else
                SearchAndReplace(doc, "#MVH6#", FormatCurrency(TotalPrice6))
                SearchAndReplace(doc, "#BTW6#", FormatCurrency(TotalVAT6))
            End If
            If TotalPrice21 = 0 Then
                SearchAndReplace(doc, "#MVH21#", "")
                SearchAndReplace(doc, "#BTW21#", "")
            Else
                SearchAndReplace(doc, "#MVH21#", FormatCurrency(TotalPrice21))
                SearchAndReplace(doc, "#BTW21#", FormatCurrency(TotalVAT21))
            End If
            SearchAndReplace(doc, "#TOTAALBTW#", FormatCurrency(TotalVat))
            SearchAndReplace(doc, "#TOTAALINBTW#", FormatCurrency(Total))
            Return (doc, invoice)
        End Function
        Public Function GetNumberFromFilename(file As String) As Integer
            Return (file.Substring(0, file.IndexOf(".")))
        End Function
        Public Function MakeNewWordInvoice(client As ClientAccountBO, iu As List(Of UnitWithStagesBO), project As ProjectBO, salessettings As ProjectSalesSettingsBO) As Response

            Dim FunctionResponse As New Response
            Dim path As String = My.Settings.InvoiceURL & Date.Now.Year & "\"
            CheckDir(path)
            Dim service = ServiceFactory.GetProjectService
            Dim OwnerPercentage As Decimal = 100.0
            Dim account As String
            If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                account = client.BankAccountNumber
            Else
                account = salessettings.BankAccountNumber
            End If

            For Each coOwner In client.CoOwners
                Dim COinvoice As New InvoiceBO
                'PERCENTAGE HOOFDEIGENAAR BEREKENEN
                OwnerPercentage = OwnerPercentage - coOwner.CoOwnerPercentage
                'BESTANDSNAAM AANMAKEN
                Dim CoNewInvoiceId As Integer = GetNewInvoiceId()
                Dim CONewFilename As String = GetNewInvoiceFilename(CoNewInvoiceId, coOwner.Salutation, coOwner.Name, coOwner.Firstname, coOwner.CompanyName)
                'INVOICE BO OPVULLEN
                COinvoice.Filename = CONewFilename
                COinvoice.Invoicedate = Date.Now()
                COinvoice.ClientId = coOwner.Id
                COinvoice.ClientType = ClientType.Medeeigenaar
                'NIEUW WORD DOC
                Dim document As WordprocessingDocument = MakeInvoiceWordDoc(CONewFilename)

                'GEGEVENS INVULLEN
                If coOwner.InvoiceStreet = String.Empty Then
                    Dim docs = InvoiceFillGeneralInfo(document, CoNewInvoiceId, coOwner.VATnumber, coOwner.Name, coOwner.Firstname, coOwner.Salutation, coOwner.CompanyName, coOwner.Street, coOwner.Housenumber, coOwner.Busnumber, coOwner.Postalcode, account, client.InvoiceExtra, COinvoice)
                    document = docs.returndoc
                    COinvoice = docs.invoice
                Else
                    Dim docs = InvoiceFillGeneralInfo(document, CoNewInvoiceId, coOwner.VATnumber, coOwner.Name, coOwner.Firstname, coOwner.Salutation, coOwner.CompanyName, coOwner.InvoiceStreet, coOwner.InvoiceHousenumber, coOwner.InvoiceBusnumber, coOwner.InvoicePostalcode, account, client.InvoiceExtra, COinvoice)
                    document = docs.returndoc
                    COinvoice = docs.invoice
                End If
                'DETAIL INVULLEN
                Dim COdetail = InvoiceFillDetails(document, coOwner.CoOwnerPercentage, iu, project, COinvoice)
                document = COdetail.returndoc
                COinvoice = COdetail.invoice

                'DOC OPSLAAN
                Dim response2 = service.InsertUpdateProjectInvoice(COinvoice)
                If response2.Success = True Then
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document.Dispose()
                Else
                    System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename)
                    AddMessage("Error", "De factuur is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    FunctionResponse.AddError(response2.Messages)
                    document.Close()
                    document.Dispose()
                    Return FunctionResponse
                End If

            Next
            '------------------------------------------------------------------------------------------------------------------------
            'HOOFDFACTUUR OPMAKEN
            Dim invoice As New InvoiceBO
            'BESTANDSNAAM AANMAKEN
            Dim NewInvoiceId As Integer = GetNewInvoiceId()
            Dim NewFilename As String = GetNewInvoiceFilename(NewInvoiceId, client.Salutation, client.Name, "", client.CompanyName)
            'INVOICE BO OPVULLEN
            invoice.Filename = NewFilename
            invoice.Invoicedate = Date.Now()
            invoice.ClientId = client.Id
            invoice.ClientType = ClientType.Klant
            'NIEUW WORD DOC
            Dim hoofdfact As WordprocessingDocument = MakeInvoiceWordDoc(NewFilename)
            'GEGEVENS INVULLEN
            If client.InvoiceStreet = String.Empty Then
                Dim docs = InvoiceFillGeneralInfo(hoofdfact, NewInvoiceId, client.VATnumber, client.Name, "", client.Salutation, client.CompanyName, client.Street, client.Housenumber, client.Busnumber, client.Postalcode, account, client.InvoiceExtra, invoice)
                hoofdfact = docs.returndoc
                invoice = docs.invoice
            Else
                Dim docs = InvoiceFillGeneralInfo(hoofdfact, NewInvoiceId, client.VATnumber, client.Name, "", client.Salutation, client.CompanyName, client.InvoiceStreet, client.InvoiceHousenumber, client.InvoiceBusnumber, client.InvoicePostalcode, account, client.InvoiceExtra, invoice)
                hoofdfact = docs.returndoc
                invoice = docs.invoice
            End If
            'DETAIL INVULLEN
            Dim detail = InvoiceFillDetails(hoofdfact, OwnerPercentage, iu, project, invoice)
            hoofdfact = detail.returndoc
            invoice = detail.invoice






            'Dim invoice As New InvoiceBO
            'Dim NewInvoiceId As Integer = GetNewInvoiceId()
            'Dim NewFilename As String = ""
            'NewFilename = NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & client.Name & ".docx"
            ''NIEUW WORD DOC
            'System.IO.File.Copy(My.Settings.TemplateInvoiceBCO, My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
            'Dim hoofdfact As WordprocessingDocument = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
            'hoofdfact.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document)
            ''GEGEVENS INVULLEN
            'SearchAndReplace(hoofdfact, "#FACTUURNUMMER#", NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year)
            'SearchAndReplace(hoofdfact, "#FACTUURDATUM#", Date.Now.ToString("dd MMMM yyyy"))
            'SearchAndReplace(hoofdfact, "#VERVALDATUM#", Date.Now().AddDays(14).ToString("dd MMMM yyyy"))
            'If Not client.VATnumber Is Nothing Or Not client.VATnumber = "" Then
            '    SearchAndReplace(hoofdfact, "#BTWNUMMER#", client.VATnumber)
            'Else
            '    SearchAndReplace(hoofdfact, "#BTWNUMMER#", " ")
            'End If
            'If Not client.Name Is Nothing Or Not client.Name = "" Then
            '    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.Salutation.GetDisplayName() & " " & client.Name)
            'Else
            '    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.CompanyName)
            'End If
            'If client.InvoiceStreet = String.Empty Then
            '    If client.Busnumber Is Nothing Or client.Busnumber = "" Then
            '        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
            '    Else
            '        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
            '    End If
            '    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
            '    If Not client.Postalcode.Country.CountryID = 19 Then
            '        SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
            '    Else
            '        SearchAndReplace(hoofdfact, "#LAND#", " ")
            '    End If
            'Else
            '    If client.InvoiceBusnumber Is Nothing Or client.InvoiceBusnumber = "" Then
            '        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber)
            '    Else
            '        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber & "/" & client.InvoiceBusnumber)
            '    End If
            '    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.InvoicePostalcode.Postcode & " " & client.InvoicePostalcode.Gemeente)
            '    If Not client.InvoicePostalcode.Country.CountryID = 19 Then
            '        SearchAndReplace(hoofdfact, "#LAND#", client.InvoicePostalcode.Country.Name)
            '    Else
            '        SearchAndReplace(hoofdfact, "#LAND#", " ")
            '    End If
            'End If
            'If client.Busnumber Is Nothing Or client.Busnumber = "" Then
            '    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
            'Else
            '    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
            'End If
            'SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
            'If Not client.Postalcode.Country.CountryID = 19 Then
            '    SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
            'Else
            '    SearchAndReplace(hoofdfact, "#LAND#", " ")
            'End If
            'Dim tekst As String = ""
            'Dim eenheidsprijzen As String = ""
            'If OwnerPercentage = 100 Then
            '    tekst = "Voor de bouwwaarde van "
            'Else
            '    tekst = "Voor " & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van "
            'End If

            'hoofdfact.MainDocumentPart.Document.Save()
            'hoofdfact.Close()
            'hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
            'Dim count As Integer = 0
            'Dim tblSchijven As DocumentFormat.OpenXml.Wordprocessing.Table
            'tblSchijven = hoofdfact.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
            'Dim CopyRow As TableRow = tblSchijven.Elements(Of TableRow)().ElementAt(1)
            'iu = iu.OrderBy(Function(m) m.Unit.Type.GroupId).ToList()
            'Dim rowcount As Integer = 0
            'Dim TotalPrice0 As Decimal = 0
            'Dim TotalPrice6 As Decimal = 0
            'Dim TotalPrice21 As Decimal = 0
            'For Each iunit In iu

            '    'FACTUURTEKST OPMAKEN
            '    tekst = tekst & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name
            '    If iunit.Unit.ConstructionValueSold > 0 Then
            '        If OwnerPercentage = 100 Then
            '            eenheidsprijzen = eenheidsprijzen & "De bouwwaarde van " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name & " : " & FormatCurrency(iunit.Unit.ConstructionValueSold * OwnerPercentage / 100)
            '        Else
            '            eenheidsprijzen = eenheidsprijzen & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name & " : " & FormatCurrency(iunit.Unit.ConstructionValueSold * OwnerPercentage / 100)
            '        End If

            '        If Not iunit Is iu.Last Then
            '            eenheidsprijzen = eenheidsprijzen & vbLf
            '            If Not iunit Is iu(iu.Count - 2) Then
            '                tekst = tekst & ", "
            '            Else
            '                tekst = tekst & " en "
            '            End If
            '        End If
            '        count = count + 1
            '        'SCHIJVEN OPMAKEN
            '        Dim rowCopy As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
            '        Dim run As New Run
            '        Dim runProperties = GetRunPropertyFromTableCell(rowCopy, 0)
            '        runProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
            '        runProperties.AppendChild(New RunFonts With {.Ascii = "Avenir-Book"})
            '        runProperties.AppendChild(New FontSize With {.Val = "18"})
            '        run.AppendChild(Of RunProperties)(runProperties)
            '        run.AppendChild(New Text("Voor " & iunit.Unit.Type.Name.ToLower & " " & iunit.Unit.Name))
            '        rowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
            '        rowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run))
            '        tblSchijven.InsertAfter(rowCopy, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
            '        rowcount = rowcount + 1
            '        For Each stage In iunit.PaymentStages
            '            Dim rowCopy2 As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
            '            Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
            '            Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
            '            runProperties2.AppendChild(New RunFonts With {.Ascii = "Avenir-Book"})
            '            runProperties2.AppendChild(New FontSize With {.Val = "18"})
            '            Dim run2 = New Run(New Text(stage.Percentage.ToString("0.##") & " % - " & stage.Name))
            '            run2.PrependChild(Of RunProperties)(runProperties2)
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
            '            Dim runProperties3 = GetRunPropertyFromTableCell(rowCopy2, 0)
            '            runProperties3.AppendChild(New RunFonts With {.Ascii = "Avenir-Book"})
            '            runProperties3.AppendChild(New FontSize With {.Val = "18"})
            '            Dim run3 = New Run(New Text(stage.VatPercentage.ToString("0.##") & " %"))
            '            run3.PrependChild(Of RunProperties)(runProperties3)
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(run3))
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
            '            Dim stagePrice As Decimal = (iunit.Unit.ConstructionValueSold * OwnerPercentage / 100) * stage.Percentage / 100
            '            Dim runValue As New Run
            '            runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
            '            runPropertiesValue.AppendChild(New RunFonts With {.Ascii = "Avenir-Book"})
            '            runPropertiesValue.AppendChild(New FontSize With {.Val = "18"})
            '            runValue.AppendChild(Of RunProperties)(runPropertiesValue)
            '            runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
            '            rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
            '            tblSchijven.InsertAfter(rowCopy2, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
            '            rowcount = rowcount + 1
            '            If stage.VatPercentage = 0 Then
            '                TotalPrice0 = TotalPrice0 + stagePrice
            '            ElseIf stage.VatPercentage = 6 Then
            '                TotalPrice6 = TotalPrice6 + stagePrice
            '            ElseIf stage.VatPercentage = 21 Then
            '                TotalPrice21 = TotalPrice21 + stagePrice
            '            End If

            '            Dim invoicerow As New InvoiceRowBO
            '            invoicerow.StageId = stage.Id
            '            invoicerow.UnitId = iunit.Unit.Id
            '            invoice.Rows.Add(invoicerow)
            '        Next
            '    End If
            'Next
            'invoice.Filename = NewFilename
            'invoice.Invoicedate = Date.Now()
            'invoice.ClientId = client.Id
            'invoice.ClientType = ClientType.Klant
            'tekst = tekst & " In project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."
            'hoofdfact.MainDocumentPart.Document.Save()
            'hoofdfact.Close()
            'hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
            'SearchAndReplace(hoofdfact, "#FACTUURTEKST#", tekst)
            'SearchAndReplaceWithNewLines(hoofdfact, "#EENHEIDSPRIJS#", eenheidsprijzen)
            'hoofdfact.MainDocumentPart.Document.Save()
            'hoofdfact.Close()
            'hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
            ''Calculate VAT en totalprice
            'Dim TotalVAT6 As Decimal = 0
            'If Not TotalPrice6 = 0 Then
            '    TotalVAT6 = TotalPrice6 * 6 / 100
            'End If
            'Dim TotalVAT21 As Decimal = 0
            'If Not TotalPrice21 = 0 Then
            '    TotalVAT21 = TotalPrice21 * 21 / 100
            'End If
            'Dim Total As Decimal = 0
            'Dim TotalExVat As Decimal = 0
            'Dim TotalVat As Decimal = 0
            'TotalExVat = TotalPrice0 + TotalPrice6 + TotalPrice21
            'TotalVat = TotalVAT6 + TotalVAT21
            'Total = TotalExVat + TotalVat
            ''Fill in Totals table
            'SearchAndReplace(hoofdfact, "#TOTAALEXBTW#", FormatCurrency(TotalExVat))
            'If TotalPrice0 = 0 Then
            '    SearchAndReplace(hoofdfact, "#MVH0#", "")
            '    SearchAndReplace(hoofdfact, "#BTW0#", "")
            'Else
            '    SearchAndReplace(hoofdfact, "#MVH0#", FormatCurrency(TotalPrice0))
            '    SearchAndReplace(hoofdfact, "#BTW0#", "")
            'End If
            'If TotalPrice6 = 0 Then
            '    SearchAndReplace(hoofdfact, "#MVH6#", "")
            '    SearchAndReplace(hoofdfact, "#BTW6#", "")
            'Else
            '    SearchAndReplace(hoofdfact, "#MVH6#", FormatCurrency(TotalPrice6))
            '    SearchAndReplace(hoofdfact, "#BTW6#", FormatCurrency(TotalVAT6))
            'End If
            'If TotalPrice21 = 0 Then
            '    SearchAndReplace(hoofdfact, "#MVH21#", "")
            '    SearchAndReplace(hoofdfact, "#BTW21#", "")
            'Else
            '    SearchAndReplace(hoofdfact, "#MVH21#", FormatCurrency(TotalPrice21))
            '    SearchAndReplace(hoofdfact, "#BTW21#", FormatCurrency(TotalVAT21))
            'End If
            'SearchAndReplace(hoofdfact, "#TOTAALBTW#", FormatCurrency(TotalVat))
            'SearchAndReplace(hoofdfact, "#TOTAALINBTW#", FormatCurrency(Total))

            'If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
            '    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", client.BankAccountNumber)
            'Else
            '    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", salessettings.BankAccountNumber)
            'End If
            'If Not client.InvoiceExtra Is Nothing Or Not client.InvoiceExtra = "" Then
            '    SearchAndReplace(hoofdfact, "#EXTRAINFO#", client.InvoiceExtra)
            'Else
            '    SearchAndReplace(hoofdfact, "#EXTRAINFO#", "")
            'End If
            'DOC OPSLAAN
            Dim response = service.InsertUpdateProjectInvoice(invoice)
            If response.Success = True Then
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact.Dispose()
                Return FunctionResponse
            Else
                System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
                AddMessage("Error", "De factuur Is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                FunctionResponse.AddError(response.Messages)
                hoofdfact.Dispose()
                Return FunctionResponse
            End If
            hoofdfact.MainDocumentPart.Document.Save()
            hoofdfact.Close()
            hoofdfact.Dispose()


        End Function
        Public Function MakeNewWordInvoiceFTP(client As ClientAccountBO, iu As List(Of UnitWithStagesBO), project As ProjectBO, salessettings As ProjectSalesSettingsBO) As Response

            Dim FunctionResponse As New Response
            Dim path As String = My.Settings.InvoiceURLFTP & Date.Now.Year
            Dim ftp As Chilkat.Ftp2 = ConnectToFtp()
            CheckDirFTP(path, ftp)
            Dim service = ServiceFactory.GetProjectService
            Dim OwnerPercentage As Decimal = 100.0
            Dim account As String
            If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                account = client.BankAccountNumber
            Else
                account = salessettings.BankAccountNumber
            End If

            For Each coOwner In client.CoOwners
                Dim COinvoice As New InvoiceBO
                'PERCENTAGE HOOFDEIGENAAR BEREKENEN
                OwnerPercentage = OwnerPercentage - coOwner.CoOwnerPercentage
                'BESTANDSNAAM AANMAKEN
                Dim CoNewInvoiceId As Integer = GetNewInvoiceIdFTP()
                Dim CONewFilename As String = GetNewInvoiceFilename(CoNewInvoiceId, coOwner.Salutation, coOwner.Name, coOwner.Firstname, coOwner.CompanyName)
                'INVOICE BO OPVULLEN
                COinvoice.Filename = CONewFilename
                COinvoice.Invoicedate = Date.Now()
                COinvoice.ClientId = coOwner.Id
                COinvoice.ClientType = ClientType.Medeeigenaar
                'NIEUW WORD DOC
                Dim document As WordprocessingDocument = MakeInvoiceWordDocFTP(CONewFilename, ftp)
                'GEGEVENS INVULLEN
                If coOwner.InvoiceStreet = String.Empty Then
                    Dim docs = InvoiceFillGeneralInfo(document, CoNewInvoiceId, coOwner.VATnumber, coOwner.Name, coOwner.Firstname, coOwner.Salutation, coOwner.CompanyName, coOwner.Street, coOwner.Housenumber, coOwner.Busnumber, coOwner.Postalcode, account, client.InvoiceExtra, COinvoice)
                    document = docs.returndoc
                    COinvoice = docs.invoice
                Else
                    Dim docs = InvoiceFillGeneralInfo(document, CoNewInvoiceId, coOwner.VATnumber, coOwner.Name, coOwner.Firstname, coOwner.Salutation, coOwner.CompanyName, coOwner.InvoiceStreet, coOwner.InvoiceHousenumber, coOwner.InvoiceBusnumber, coOwner.InvoicePostalcode, account, client.InvoiceExtra, COinvoice)
                    document = docs.returndoc
                    COinvoice = docs.invoice
                End If
                'DETAIL INVULLEN
                Dim COdetail = InvoiceFillDetails(document, coOwner.CoOwnerPercentage, iu, project, COinvoice)
                document = COdetail.returndoc
                COinvoice = COdetail.invoice


                'DOC OPSLAAN
                Dim response2 = service.InsertUpdateProjectInvoice(COinvoice)
                If response2.Success = True Then
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document.Dispose()
                    Dim success = UploadFileToFtp(path, CONewFilename, ftp)
                    If success = True Then
                        DeleteTempFile(CONewFilename)
                    End If
                Else
                    DeleteFtpFile(path, CONewFilename, ftp)
                    'System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename)
                    AddMessage("Error", "De factuur is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    FunctionResponse.AddError(response2.Messages)
                    document.Close()
                    document.Dispose()
                    Return FunctionResponse
                End If

            Next
            '------------------------------------------------------------------------------------------------------------------------
            'HOOFDFACTUUR OPMAKEN
            Dim invoice As New InvoiceBO
            'BESTANDSNAAM AANMAKEN
            Dim NewInvoiceId As Integer = GetNewInvoiceIdFTP()
            Dim NewFilename As String = GetNewInvoiceFilename(NewInvoiceId, client.Salutation, client.Name, "", client.CompanyName)
            'INVOICE BO OPVULLEN
            invoice.Filename = NewFilename
            invoice.Invoicedate = Date.Now()
            invoice.ClientId = client.Id
            invoice.ClientType = ClientType.Klant
            'NIEUW WORD DOC
            Dim hoofdfact As WordprocessingDocument = MakeInvoiceWordDocFTP(NewFilename, ftp)
            'GEGEVENS INVULLEN
            If client.InvoiceStreet = String.Empty Then
                Dim docs = InvoiceFillGeneralInfo(hoofdfact, NewInvoiceId, client.VATnumber, client.Name, "", client.Salutation, client.CompanyName, client.Street, client.Housenumber, client.Busnumber, client.Postalcode, account, client.InvoiceExtra, invoice)
                hoofdfact = docs.returndoc
                invoice = docs.invoice
            Else
                Dim docs = InvoiceFillGeneralInfo(hoofdfact, NewInvoiceId, client.VATnumber, client.Name, "", client.Salutation, client.CompanyName, client.InvoiceStreet, client.InvoiceHousenumber, client.InvoiceBusnumber, client.InvoicePostalcode, account, client.InvoiceExtra, invoice)
                hoofdfact = docs.returndoc
                invoice = docs.invoice
            End If
            'DETAIL INVULLEN
            Dim detail = InvoiceFillDetails(hoofdfact, OwnerPercentage, iu, project, invoice)
            hoofdfact = detail.returndoc
            invoice = detail.invoice

            'DOC OPSLAAN
            Dim response = service.InsertUpdateProjectInvoice(invoice)
            If response.Success = True Then
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact.Dispose()
                Dim success = UploadFileToFtp(path, NewFilename, ftp)
                ftp.Disconnect()
                If success = True Then
                    DeleteTempFile(NewFilename)
                End If
                Return FunctionResponse
            Else
                DeleteFtpFile(path, NewFilename, ftp)
                'System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
                AddMessage("Error", "De factuur Is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                FunctionResponse.AddError(response.Messages)
                hoofdfact.Dispose()
                ftp.Disconnect()
                Return FunctionResponse
            End If
            'hoofdfact.MainDocumentPart.Document.Save()
            'hoofdfact.Close()
            'hoofdfact.Dispose()


        End Function
        Public Function MakeNewWordInvoiceCO(client As ClientAccountBO, co As List(Of ChangeOrderBO), units As List(Of UnitBO), project As ProjectBO, salessettings As ProjectSalesSettingsBO) As Response
            Dim service = ServiceFactory.GetProjectService
            Dim ResponseFin As New Response
            Try
                Dim FunctionResponse As New Response
                Dim path As String = My.Settings.InvoiceURL & Date.Now.Year & "\"
                CheckDir(path)

                Dim OwnerPercentage As Decimal = 100.0
                units = units.OrderBy(Function(m) m.Type.GroupId).ToList()
                For Each coOwner In client.CoOwners
                    Dim COinvoice As New InvoiceBO
                    'PERCENTAGE HOOFDEIGENAAR BEREKENEN
                    OwnerPercentage = OwnerPercentage - coOwner.CoOwnerPercentage
                    'BESTANDSNAAM AANMAKEN
                    Dim CONewInvoiceId As Integer = GetNewInvoiceId()
                    Dim CONewFilename As String = ""
                    If coOwner.Salutation = Salutation.Dhr Or coOwner.Salutation = Salutation.Mevr Then
                        CONewFilename = CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & coOwner.Name & " " & coOwner.Firstname & ".docx"

                    Else
                        CONewFilename = CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & coOwner.Name & ".docx"

                    End If
                    'NIEUW WORD DOC
                    System.IO.File.Copy(My.Settings.TemplateInvoiceBCO, My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename)
                    Dim document As WordprocessingDocument = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename, True)
                    document.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document)
                    'GEGEVENS INVULLEN
                    SearchAndReplace(document, "#FACTUURNUMMER#", CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year)
                    SearchAndReplace(document, "#FACTUURDATUM#", Date.Now.ToString("dd MMMM yyyy"))
                    SearchAndReplace(document, "#VERVALDATUM#", Date.Now().AddDays(14).ToString("dd MMMM yyyy"))
                    If Not coOwner.VATnumber Is Nothing Or Not coOwner.VATnumber = "" Then
                        SearchAndReplace(document, "#BTWNUMMER#", client.VATnumber)
                    Else
                        SearchAndReplace(document, "#BTWNUMMER#", "")
                    End If
                    If Not coOwner.Name Is Nothing Or Not coOwner.Name = "" Then
                        If coOwner.Salutation = Salutation.Dhr Or coOwner.Salutation = Salutation.Mevr Then
                            SearchAndReplace(document, "#KLANTNAAM#", coOwner.Salutation.GetDisplayName() & " " & coOwner.Name & " " & coOwner.Firstname)
                        Else
                            SearchAndReplace(document, "#KLANTNAAM#", coOwner.Salutation.GetDisplayName() & " " & coOwner.Name)
                        End If
                    Else
                        SearchAndReplace(document, "#KLANTNAAM#", coOwner.CompanyName)
                    End If
                    If coOwner.InvoiceStreet = String.Empty Then
                        If coOwner.Busnumber Is Nothing Or coOwner.Busnumber = "" Then
                            SearchAndReplace(document, "#STRAAT#", coOwner.Street & " " & coOwner.Housenumber)
                        Else
                            SearchAndReplace(document, "#STRAAT#", coOwner.Street & " " & coOwner.Housenumber & "/" & coOwner.Busnumber)
                        End If
                        SearchAndReplace(document, "#GEMEENTE#", coOwner.Postalcode.Postcode & " " & coOwner.Postalcode.Gemeente)
                        If Not coOwner.Postalcode.Country.CountryID = 19 Then
                            SearchAndReplace(document, "#LAND#", coOwner.Postalcode.Country.Name)
                        Else
                            SearchAndReplace(document, "#LAND#", "")
                        End If
                    Else
                        If coOwner.InvoiceBusnumber Is Nothing Or coOwner.InvoiceBusnumber = "" Then
                            SearchAndReplace(document, "#STRAAT#", coOwner.InvoiceStreet & " " & coOwner.InvoiceHousenumber)
                        Else
                            SearchAndReplace(document, "#STRAAT#", coOwner.InvoiceStreet & " " & coOwner.InvoiceHousenumber & "/" & coOwner.InvoiceBusnumber)
                        End If
                        SearchAndReplace(document, "#GEMEENTE#", coOwner.InvoicePostalcode.Postcode & " " & coOwner.InvoicePostalcode.Gemeente)
                        If Not coOwner.InvoicePostalcode.Country.CountryID = 19 Then
                            SearchAndReplace(document, "#LAND#", coOwner.InvoicePostalcode.Country.Name)
                        Else
                            SearchAndReplace(document, "#LAND#", "")
                        End If
                    End If

                    Dim COtekst As String = ""
                    Dim COeenheidsprijzen As String = ""
                    If coOwner.CoOwnerPercentage = 100 Then
                        COtekst = "Voor de meerwerken/minwerken van "
                    Else
                        COtekst = "Voor " & coOwner.CoOwnerPercentage.ToString("#0.##") & " % van de meerwerken/minwerken van "
                    End If

                    Dim COcount As Integer = 0
                    Dim COtblSchijven2 As DocumentFormat.OpenXml.Wordprocessing.Table
                    COtblSchijven2 = document.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
                    Dim theRow As TableRow = COtblSchijven2.Elements(Of TableRow)().ElementAt(1)
                    co = co.OrderBy(Function(m) m.ChangeOrderDate).ToList()
                    Dim COrowcount As Integer = 0
                    Dim COTotalPrice As Decimal = 0
                    For Each unit In units
                        COtekst = COtekst & unit.Type.Name.ToLower & " " & unit.Name
                        If Not unit Is units.Last Then
                            If Not unit Is units(units.Count - 2) Then
                                COtekst = COtekst & ", "
                            Else
                                COtekst = COtekst & " en "
                            End If
                        End If
                        COcount = COcount + 1
                    Next
                    'MEERWERKEN OPMAKEN
                    Dim COrowCopy As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                    Dim COrun As New Run
                    Dim COrunProperties = GetRunPropertyFromTableCell(COrowCopy, 0)
                    COrunProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
                    COrun.AppendChild(Of RunProperties)(COrunProperties)
                    COrun.AppendChild(New Text("Meerwerken/minwerken"))
                    COrowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                    COrowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(COrun))
                    COtblSchijven2.InsertAfter(COrowCopy, COtblSchijven2.Elements(Of TableRow)().ElementAt(COrowcount))
                    COrowcount = COrowcount + 1
                    For Each COrder In co
                        For Each COrderDetail In COrder.Details
                            Dim rowCopy2 As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                            Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
                            Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
                            Dim run2 = New Run(New Text(COrderDetail.Description))
                            run2.PrependChild(Of RunProperties)(runProperties2)
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(New Run(New Text(salessettings.VatPercentage.ToString("0.##") & " %"))))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
                            Dim stagePrice As Decimal = (COrderDetail.Totaal * coOwner.CoOwnerPercentage / 100)
                            Dim runValue As New Run
                            runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
                            runValue.AppendChild(Of RunProperties)(runPropertiesValue)
                            runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
                            COtblSchijven2.InsertAfter(rowCopy2, COtblSchijven2.Elements(Of TableRow)().ElementAt(COrowcount))
                            COrowcount = COrowcount + 1
                            COTotalPrice = COTotalPrice + stagePrice
                            Dim invoicerow As New InvoiceRowBO
                            invoicerow.ChangeOrderDetailId = COrderDetail.Id
                            COinvoice.Rows.Add(invoicerow)
                        Next
                    Next


                    COinvoice.Filename = CONewFilename
                    COinvoice.Invoicedate = Date.Now()
                    COinvoice.ClientId = coOwner.Id
                    COinvoice.ClientType = ClientType.Medeeigenaar
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename, True)
                    COtekst = COtekst & " in project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."

                    SearchAndReplace(document, "#FACTUURTEKST#", COtekst)
                    SearchAndReplaceWithNewLines(document, "#EENHEIDSPRIJS#", COeenheidsprijzen)
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename, True)
                    Dim COTotalVAT As Decimal = 0
                    COTotalVAT = COTotalPrice * salessettings.VatPercentage / 100
                    Dim COTotal As Decimal = 0
                    COTotal = COTotalPrice + COTotalVAT
                    SearchAndReplace(document, "#TOTAALEXBTW#", FormatCurrency(COTotalPrice))
                    SearchAndReplace(document, "#TOTAALBTW#", FormatCurrency(COTotalVAT))
                    SearchAndReplace(document, "#TOTAALINBTW#", FormatCurrency(COTotal))
                    SearchAndReplace(document, "#BTWPERC#", "BTW " & salessettings.VatPercentage.ToString("0.##") & " %")
                    If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                        SearchAndReplace(document, "#REKENINGNUMMER#", client.BankAccountNumber)
                    Else
                        SearchAndReplace(document, "#REKENINGNUMMER#", salessettings.BankAccountNumber)
                    End If

                    'DOC OPSLAAN
                    Dim response2 = service.InsertUpdateProjectInvoice(COinvoice)
                    If response2.Success = True Then
                        document.MainDocumentPart.Document.Save()
                        document.Close()
                        document.Dispose()
                    Else
                        System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename)
                        AddMessage("Error", "De factuur is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                        FunctionResponse.AddError(response2.Messages)
                        document.Close()
                        document.Dispose()
                        Return FunctionResponse
                    End If

                Next
                '------------------------------------------------------------------------------------------------------------------------
                'HOOFDFACTUUR OPMAKEN
                Dim invoice As New InvoiceBO
                Dim NewInvoiceId As Integer = GetNewInvoiceId()
                Dim NewFilename As String = ""
                NewFilename = NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & client.Name & ".docx"
                'NIEUW WORD DOC
                System.IO.File.Copy(My.Settings.TemplateInvoiceBCO, My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
                Dim hoofdfact As WordprocessingDocument = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
                hoofdfact.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document)
                'GEGEVENS INVULLEN
                SearchAndReplace(hoofdfact, "#FACTUURNUMMER#", NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year)
                SearchAndReplace(hoofdfact, "#FACTUURDATUM#", Date.Now.ToString("dd MMMM yyyy"))
                SearchAndReplace(hoofdfact, "#VERVALDATUM#", Date.Now().AddDays(14).ToString("dd MMMM yyyy"))
                If Not client.VATnumber Is Nothing Or Not client.VATnumber = "" Then
                    SearchAndReplace(hoofdfact, "#BTWNUMMER#", client.VATnumber)
                Else
                    SearchAndReplace(hoofdfact, "#BTWNUMMER#", " ")
                End If
                If Not client.Name Is Nothing Or Not client.Name = "" Then
                    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.Salutation.GetDisplayName() & " " & client.Name)
                Else
                    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.CompanyName)
                End If
                If client.InvoiceStreet = String.Empty Then
                    If client.Busnumber Is Nothing Or client.Busnumber = "" Then
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
                    Else
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
                    End If
                    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
                    If Not client.Postalcode.Country.CountryID = 19 Then
                        SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
                    Else
                        SearchAndReplace(hoofdfact, "#LAND#", " ")
                    End If
                Else
                    If client.InvoiceBusnumber Is Nothing Or client.InvoiceBusnumber = "" Then
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber)
                    Else
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber & "/" & client.InvoiceBusnumber)
                    End If
                    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.InvoicePostalcode.Postcode & " " & client.InvoicePostalcode.Gemeente)
                    If Not client.InvoicePostalcode.Country.CountryID = 19 Then
                        SearchAndReplace(hoofdfact, "#LAND#", client.InvoicePostalcode.Country.Name)
                    Else
                        SearchAndReplace(hoofdfact, "#LAND#", " ")
                    End If
                End If
                If client.Busnumber Is Nothing Or client.Busnumber = "" Then
                    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
                Else
                    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
                End If
                SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
                If Not client.Postalcode.Country.CountryID = 19 Then
                    SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
                Else
                    SearchAndReplace(hoofdfact, "#LAND#", " ")
                End If
                Dim tekst As String = ""
                Dim eenheidsprijzen As String = ""
                If OwnerPercentage = 100 Then
                    tekst = "Voor de meerwerken/minwerken van "
                Else
                    tekst = "Voor " & OwnerPercentage.ToString("#0.##") & " % van de meerwerken/minwerken van "
                End If

                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
                Dim count As Integer = 0
                Dim tblSchijven As DocumentFormat.OpenXml.Wordprocessing.Table
                tblSchijven = hoofdfact.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
                Dim CopyRow As TableRow = tblSchijven.Elements(Of TableRow)().ElementAt(1)
                Dim rowcount As Integer = 0
                Dim TotalPrice As Decimal = 0
                For Each unit In units
                    tekst = tekst & unit.Type.Name.ToLower & " " & unit.Name
                    If Not unit Is units.Last Then
                        If Not unit Is units(units.Count - 2) Then
                            tekst = tekst & ", "
                        Else
                            tekst = tekst & " en "
                        End If
                    End If
                    count = count + 1
                Next

                'MEERWERKEN OPMAKEN
                Dim rowCopy As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
                Dim run As New Run
                Dim runProperties = GetRunPropertyFromTableCell(rowCopy, 0)
                runProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
                run.AppendChild(Of RunProperties)(runProperties)
                run.AppendChild(New Text("Meerwerken/minwerken"))
                rowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                rowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run))
                tblSchijven.InsertAfter(rowCopy, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                rowcount = rowcount + 1
                For Each COrder In co
                    For Each COrderDetail In COrder.Details
                        Dim rowCopy2 As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
                        Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
                        Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
                        Dim run2 = New Run(New Text(COrderDetail.Description))
                        run2.PrependChild(Of RunProperties)(runProperties2)
                        rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                        rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
                        rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(New Run(New Text(salessettings.VatPercentage.ToString("0.##") & " %"))))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
                        Dim stagePrice As Decimal = (COrderDetail.Totaal * OwnerPercentage / 100)
                        Dim runValue As New Run
                        runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
                        runValue.AppendChild(Of RunProperties)(runPropertiesValue)
                        runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
                        tblSchijven.InsertAfter(rowCopy2, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                        rowcount = rowcount + 1
                        TotalPrice = TotalPrice + stagePrice
                        Dim invoicerow As New InvoiceRowBO
                        invoicerow.ChangeOrderDetailId = COrderDetail.Id
                        invoice.Rows.Add(invoicerow)
                    Next
                Next

                invoice.Filename = NewFilename
                invoice.Invoicedate = Date.Now()
                invoice.ClientId = client.Id
                invoice.ClientType = ClientType.Klant
                tekst = tekst & " In project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)

                SearchAndReplace(hoofdfact, "#FACTUURTEKST#", tekst)
                SearchAndReplaceWithNewLines(hoofdfact, "#EENHEIDSPRIJS#", eenheidsprijzen)
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename, True)
                Dim TotalVAT As Decimal = 0
                TotalVAT = TotalPrice * salessettings.VatPercentage / 100
                Dim Total As Decimal = 0
                Total = TotalPrice + TotalVAT
                SearchAndReplace(hoofdfact, "#TOTAALEXBTW#", FormatCurrency(TotalPrice))
                SearchAndReplace(hoofdfact, "#TOTAALBTW#", FormatCurrency(TotalVAT))
                SearchAndReplace(hoofdfact, "#TOTAALINBTW#", FormatCurrency(Total))
                SearchAndReplace(hoofdfact, "#BTWPERC#", "BTW " & salessettings.VatPercentage.ToString("0.##") & " %")
                If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", client.BankAccountNumber)
                Else
                    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", salessettings.BankAccountNumber)
                End If
                'DOC OPSLAAN
                Dim response = service.InsertUpdateProjectInvoice(invoice)
                If response.Success = True Then
                    hoofdfact.MainDocumentPart.Document.Save()
                    hoofdfact.Close()
                    hoofdfact.Dispose()
                    Return FunctionResponse
                Else
                    System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
                    AddMessage("Error", "De factuur Is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    FunctionResponse.AddError(response.Messages)
                    hoofdfact.Dispose()
                    Return FunctionResponse
                End If
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact.Dispose()


            Catch ex As Exception
            Finally
                For Each order In co
                    For Each det In order.Details
                        ResponseFin = service.SetChangeOrderDetailInvoiced(det.Id)

                    Next
                Next

            End Try
            Return ResponseFin
        End Function
        Public Function MakeNewWordInvoiceCOFTP(client As ClientAccountBO, co As List(Of ChangeOrderBO), units As List(Of UnitBO), project As ProjectBO, salessettings As ProjectSalesSettingsBO) As Response
            Dim service = ServiceFactory.GetProjectService
            Dim ResponseFin As New Response
            Try
                Dim FunctionResponse As New Response
                Dim path As String = My.Settings.InvoiceURLFTP & Date.Now.Year
                Dim ftp As Chilkat.Ftp2 = ConnectToFtp()
                CheckDirFTP(path, ftp)

                Dim OwnerPercentage As Decimal = 100.0
                units = units.OrderBy(Function(m) m.Type.GroupId).ToList()
                For Each coOwner In client.CoOwners
                    Dim COinvoice As New InvoiceBO
                    'PERCENTAGE HOOFDEIGENAAR BEREKENEN
                    OwnerPercentage = OwnerPercentage - coOwner.CoOwnerPercentage
                    'BESTANDSNAAM AANMAKEN
                    Dim CONewInvoiceId As Integer = GetNewInvoiceIdFTP()
                    Dim CONewFilename As String = ""
                    If coOwner.Salutation = Salutation.Dhr Or coOwner.Salutation = Salutation.Mevr Then
                        CONewFilename = CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & coOwner.Name & " " & coOwner.Firstname & ".docx"

                    Else
                        CONewFilename = CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & coOwner.Name & ".docx"

                    End If
                    'NIEUW WORD DOC
                    Dim document As WordprocessingDocument = MakeInvoiceWordDocFTP(CONewFilename, ftp)
                    'GEGEVENS INVULLEN
                    SearchAndReplace(document, "#FACTUURNUMMER#", CONewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year)
                    SearchAndReplace(document, "#FACTUURDATUM#", Date.Now.ToString("dd MMMM yyyy"))
                    SearchAndReplace(document, "#VERVALDATUM#", Date.Now().AddDays(14).ToString("dd MMMM yyyy"))
                    If Not coOwner.VATnumber Is Nothing Or Not coOwner.VATnumber = "" Then
                        SearchAndReplace(document, "#BTWNUMMER#", client.VATnumber)
                    Else
                        SearchAndReplace(document, "#BTWNUMMER#", "")
                    End If
                    If Not coOwner.Name Is Nothing Or Not coOwner.Name = "" Then
                        If coOwner.Salutation = Salutation.Dhr Or coOwner.Salutation = Salutation.Mevr Then
                            SearchAndReplace(document, "#KLANTNAAM#", coOwner.Salutation.GetDisplayName() & " " & coOwner.Name & " " & coOwner.Firstname)
                        Else
                            SearchAndReplace(document, "#KLANTNAAM#", coOwner.Salutation.GetDisplayName() & " " & coOwner.Name)
                        End If
                    Else
                        SearchAndReplace(document, "#KLANTNAAM#", coOwner.CompanyName)
                    End If
                    If coOwner.InvoiceStreet = String.Empty Then
                        If coOwner.Busnumber Is Nothing Or coOwner.Busnumber = "" Then
                            SearchAndReplace(document, "#STRAAT#", coOwner.Street & " " & coOwner.Housenumber)
                        Else
                            SearchAndReplace(document, "#STRAAT#", coOwner.Street & " " & coOwner.Housenumber & "/" & coOwner.Busnumber)
                        End If
                        SearchAndReplace(document, "#GEMEENTE#", coOwner.Postalcode.Postcode & " " & coOwner.Postalcode.Gemeente)
                        If Not coOwner.Postalcode.Country.CountryID = 19 Then
                            SearchAndReplace(document, "#LAND#", coOwner.Postalcode.Country.Name)
                        Else
                            SearchAndReplace(document, "#LAND#", "")
                        End If
                    Else
                        If coOwner.InvoiceBusnumber Is Nothing Or coOwner.InvoiceBusnumber = "" Then
                            SearchAndReplace(document, "#STRAAT#", coOwner.InvoiceStreet & " " & coOwner.InvoiceHousenumber)
                        Else
                            SearchAndReplace(document, "#STRAAT#", coOwner.InvoiceStreet & " " & coOwner.InvoiceHousenumber & "/" & coOwner.InvoiceBusnumber)
                        End If
                        SearchAndReplace(document, "#GEMEENTE#", coOwner.InvoicePostalcode.Postcode & " " & coOwner.InvoicePostalcode.Gemeente)
                        If Not coOwner.InvoicePostalcode.Country.CountryID = 19 Then
                            SearchAndReplace(document, "#LAND#", coOwner.InvoicePostalcode.Country.Name)
                        Else
                            SearchAndReplace(document, "#LAND#", "")
                        End If
                    End If

                    Dim COtekst As String = ""
                    Dim COeenheidsprijzen As String = ""
                    If coOwner.CoOwnerPercentage = 100 Then
                        COtekst = "Voor de meerwerken/minwerken van "
                    Else
                        COtekst = "Voor " & coOwner.CoOwnerPercentage.ToString("#0.##") & " % van de meerwerken/minwerken van "
                    End If

                    Dim COcount As Integer = 0
                    Dim COtblSchijven2 As DocumentFormat.OpenXml.Wordprocessing.Table
                    COtblSchijven2 = document.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
                    Dim theRow As TableRow = COtblSchijven2.Elements(Of TableRow)().ElementAt(1)
                    co = co.OrderBy(Function(m) m.ChangeOrderDate).ToList()
                    Dim COrowcount As Integer = 0
                    Dim COTotalPrice As Decimal = 0
                    For Each unit In units
                        COtekst = COtekst & unit.Type.Name.ToLower & " " & unit.Name
                        If Not unit Is units.Last Then
                            If Not unit Is units(units.Count - 2) Then
                                COtekst = COtekst & ", "
                            Else
                                COtekst = COtekst & " en "
                            End If
                        End If
                        COcount = COcount + 1
                    Next
                    'MEERWERKEN OPMAKEN
                    Dim COrowCopy As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                    Dim COrun As New Run
                    Dim COrunProperties = GetRunPropertyFromTableCell(COrowCopy, 0)
                    COrunProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
                    COrun.AppendChild(Of RunProperties)(COrunProperties)
                    COrun.AppendChild(New Text("Meerwerken/minwerken"))
                    COrowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                    COrowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(COrun))
                    COtblSchijven2.InsertAfter(COrowCopy, COtblSchijven2.Elements(Of TableRow)().ElementAt(COrowcount))
                    COrowcount = COrowcount + 1
                    For Each COrder In co
                        For Each COrderDetail In COrder.Details
                            Dim rowCopy2 As TableRow = DirectCast(theRow.CloneNode(True), TableRow)
                            Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
                            Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
                            Dim run2 = New Run(New Text(COrderDetail.Description))
                            run2.PrependChild(Of RunProperties)(runProperties2)
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                            rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
                            rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(New Run(New Text(salessettings.VatPercentage.ToString("0.##") & " %"))))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
                            Dim stagePrice As Decimal = (COrderDetail.Totaal * coOwner.CoOwnerPercentage / 100)
                            Dim runValue As New Run
                            runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
                            runValue.AppendChild(Of RunProperties)(runPropertiesValue)
                            runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
                            rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
                            COtblSchijven2.InsertAfter(rowCopy2, COtblSchijven2.Elements(Of TableRow)().ElementAt(COrowcount))
                            COrowcount = COrowcount + 1
                            COTotalPrice = COTotalPrice + stagePrice
                            Dim invoicerow As New InvoiceRowBO
                            invoicerow.ChangeOrderDetailId = COrderDetail.Id
                            COinvoice.Rows.Add(invoicerow)
                        Next
                    Next


                    COinvoice.Filename = CONewFilename
                    COinvoice.Invoicedate = Date.Now()
                    COinvoice.ClientId = coOwner.Id
                    COinvoice.ClientType = ClientType.Medeeigenaar
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document = WordprocessingDocument.Open(My.Settings.localTempPath & CONewFilename, True)
                    COtekst = COtekst & " in project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."

                    SearchAndReplace(document, "#FACTUURTEKST#", COtekst)
                    SearchAndReplaceWithNewLines(document, "#EENHEIDSPRIJS#", COeenheidsprijzen)
                    document.MainDocumentPart.Document.Save()
                    document.Close()
                    document = WordprocessingDocument.Open(My.Settings.localTempPath & CONewFilename, True)
                    Dim COTotalVAT As Decimal = 0
                    COTotalVAT = COTotalPrice * salessettings.VatPercentage / 100
                    Dim COTotal As Decimal = 0
                    COTotal = COTotalPrice + COTotalVAT
                    SearchAndReplace(document, "#TOTAALEXBTW#", FormatCurrency(COTotalPrice))
                    SearchAndReplace(document, "#TOTAALBTW#", FormatCurrency(COTotalVAT))
                    SearchAndReplace(document, "#TOTAALINBTW#", FormatCurrency(COTotal))
                    SearchAndReplace(document, "#BTWPERC#", "BTW " & salessettings.VatPercentage.ToString("0.##") & " %")
                    If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                        SearchAndReplace(document, "#REKENINGNUMMER#", client.BankAccountNumber)
                    Else
                        SearchAndReplace(document, "#REKENINGNUMMER#", salessettings.BankAccountNumber)
                    End If

                    'DOC OPSLAAN
                    Dim response2 = service.InsertUpdateProjectInvoice(COinvoice)
                    If response2.Success = True Then
                        document.MainDocumentPart.Document.Save()
                        document.Close()
                        document.Dispose()
                        Dim success = UploadFileToFtp(path, CONewFilename, ftp)
                        If success = True Then
                            DeleteTempFile(CONewFilename)
                        End If
                        Return FunctionResponse
                    Else
                        DeleteFtpFile(path, CONewFilename, ftp)
                        'System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & CONewFilename)
                        AddMessage("Error", "De factuur is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                        FunctionResponse.AddError(response2.Messages)
                        document.Close()
                        document.Dispose()
                        Return FunctionResponse
                    End If

                Next
                '------------------------------------------------------------------------------------------------------------------------
                'HOOFDFACTUUR OPMAKEN
                Dim invoice As New InvoiceBO
                Dim NewInvoiceId As Integer = GetNewInvoiceIdFTP()
                Dim NewFilename As String = ""
                NewFilename = NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year & " " & client.Name & ".docx"
                'NIEUW WORD DOC
                Dim hoofdfact As WordprocessingDocument = MakeInvoiceWordDocFTP(NewFilename, ftp)

                'GEGEVENS INVULLEN
                SearchAndReplace(hoofdfact, "#FACTUURNUMMER#", NewInvoiceId.ToString("0000") & "." & Date.Now.Month.ToString("00") & "." & Date.Now.Year)
                SearchAndReplace(hoofdfact, "#FACTUURDATUM#", Date.Now.ToString("dd MMMM yyyy"))
                SearchAndReplace(hoofdfact, "#VERVALDATUM#", Date.Now().AddDays(14).ToString("dd MMMM yyyy"))
                If Not client.VATnumber Is Nothing Or Not client.VATnumber = "" Then
                    SearchAndReplace(hoofdfact, "#BTWNUMMER#", client.VATnumber)
                Else
                    SearchAndReplace(hoofdfact, "#BTWNUMMER#", " ")
                End If
                If Not client.Name Is Nothing Or Not client.Name = "" Then
                    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.Salutation.GetDisplayName() & " " & client.Name)
                Else
                    SearchAndReplace(hoofdfact, "#KLANTNAAM#", client.CompanyName)
                End If
                If client.InvoiceStreet = String.Empty Then
                    If client.Busnumber Is Nothing Or client.Busnumber = "" Then
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
                    Else
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
                    End If
                    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
                    If Not client.Postalcode.Country.CountryID = 19 Then
                        SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
                    Else
                        SearchAndReplace(hoofdfact, "#LAND#", " ")
                    End If
                Else
                    If client.InvoiceBusnumber Is Nothing Or client.InvoiceBusnumber = "" Then
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber)
                    Else
                        SearchAndReplace(hoofdfact, "#STRAAT#", client.InvoiceStreet & " " & client.InvoiceHousenumber & "/" & client.InvoiceBusnumber)
                    End If
                    SearchAndReplace(hoofdfact, "#GEMEENTE#", client.InvoicePostalcode.Postcode & " " & client.InvoicePostalcode.Gemeente)
                    If Not client.InvoicePostalcode.Country.CountryID = 19 Then
                        SearchAndReplace(hoofdfact, "#LAND#", client.InvoicePostalcode.Country.Name)
                    Else
                        SearchAndReplace(hoofdfact, "#LAND#", " ")
                    End If
                End If
                If client.Busnumber Is Nothing Or client.Busnumber = "" Then
                    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber)
                Else
                    SearchAndReplace(hoofdfact, "#STRAAT#", client.Street & " " & client.Housenumber & "/" & client.Busnumber)
                End If
                SearchAndReplace(hoofdfact, "#GEMEENTE#", client.Postalcode.Postcode & " " & client.Postalcode.Gemeente)
                If Not client.Postalcode.Country.CountryID = 19 Then
                    SearchAndReplace(hoofdfact, "#LAND#", client.Postalcode.Country.Name)
                Else
                    SearchAndReplace(hoofdfact, "#LAND#", " ")
                End If
                Dim tekst As String = ""
                Dim eenheidsprijzen As String = ""
                If OwnerPercentage = 100 Then
                    tekst = "Voor de meerwerken/minwerken van "
                Else
                    tekst = "Voor " & OwnerPercentage.ToString("#0.##") & " % van de meerwerken/minwerken van "
                End If

                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.localTempPath & NewFilename, True)
                Dim count As Integer = 0
                Dim tblSchijven As DocumentFormat.OpenXml.Wordprocessing.Table
                tblSchijven = hoofdfact.MainDocumentPart.RootElement.Descendants(Of Table)().ElementAt(0)
                Dim CopyRow As TableRow = tblSchijven.Elements(Of TableRow)().ElementAt(1)
                Dim rowcount As Integer = 0
                Dim TotalPrice As Decimal = 0
                For Each unit In units
                    tekst = tekst & unit.Type.Name.ToLower & " " & unit.Name
                    If Not unit Is units.Last Then
                        If Not unit Is units(units.Count - 2) Then
                            tekst = tekst & ", "
                        Else
                            tekst = tekst & " en "
                        End If
                    End If
                    count = count + 1
                Next

                'MEERWERKEN OPMAKEN
                Dim rowCopy As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
                Dim run As New Run
                Dim runProperties = GetRunPropertyFromTableCell(rowCopy, 0)
                runProperties.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.Underline() With {.Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single})
                run.AppendChild(Of RunProperties)(runProperties)
                run.AppendChild(New Text("Meerwerken/minwerken"))
                rowCopy.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                rowCopy.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run))
                tblSchijven.InsertAfter(rowCopy, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                rowcount = rowcount + 1
                For Each COrder In co
                    For Each COrderDetail In COrder.Details
                        Dim rowCopy2 As TableRow = DirectCast(CopyRow.CloneNode(True), TableRow)
                        Dim runProperties2 = GetRunPropertyFromTableCell(rowCopy2, 0)
                        Dim runPropertiesValue = GetRunPropertyFromTableCell(rowCopy2, 2)
                        Dim run2 = New Run(New Text(COrderDetail.Description))
                        run2.PrependChild(Of RunProperties)(runProperties2)
                        rowCopy2.Descendants(Of TableCell)().ElementAt(0).RemoveAllChildren(Of Paragraph)()
                        rowCopy2.Descendants(Of TableCell)().ElementAt(0).Append(New Paragraph(run2))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(1).RemoveAllChildren(Of Paragraph)()
                        rowCopy2.Descendants(Of TableCell)().ElementAt(1).Append(New Paragraph(New Run(New Text(salessettings.VatPercentage.ToString("0.##") & " %"))))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(2).RemoveAllChildren(Of Paragraph)()
                        Dim stagePrice As Decimal = (COrderDetail.Totaal * OwnerPercentage / 100)
                        Dim runValue As New Run
                        runPropertiesValue.AppendChild(New DocumentFormat.OpenXml.Wordprocessing.TextAlignment() With {.Val = DocumentFormat.OpenXml.Wordprocessing.HorizontalAlignmentValues.Right})
                        runValue.AppendChild(Of RunProperties)(runPropertiesValue)
                        runValue.AppendChild(New Text(FormatCurrency(stagePrice)))
                        rowCopy2.Descendants(Of TableCell)().ElementAt(2).Append(New Paragraph(New ParagraphProperties(New Justification With {.Val = JustificationValues.Right}), runValue))
                        tblSchijven.InsertAfter(rowCopy2, tblSchijven.Elements(Of TableRow)().ElementAt(rowcount))
                        rowcount = rowcount + 1
                        TotalPrice = TotalPrice + stagePrice
                        Dim invoicerow As New InvoiceRowBO
                        invoicerow.ChangeOrderDetailId = COrderDetail.Id
                        invoice.Rows.Add(invoicerow)
                    Next
                Next

                invoice.Filename = NewFilename
                invoice.Invoicedate = Date.Now()
                invoice.ClientId = client.Id
                invoice.ClientType = ClientType.Klant
                tekst = tekst & " In project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.localTempPath & NewFilename, True)

                SearchAndReplace(hoofdfact, "#FACTUURTEKST#", tekst)
                SearchAndReplaceWithNewLines(hoofdfact, "#EENHEIDSPRIJS#", eenheidsprijzen)
                hoofdfact.MainDocumentPart.Document.Save()
                hoofdfact.Close()
                hoofdfact = WordprocessingDocument.Open(My.Settings.localTempPath & NewFilename, True)
                Dim TotalVAT As Decimal = 0
                TotalVAT = TotalPrice * salessettings.VatPercentage / 100
                Dim Total As Decimal = 0
                Total = TotalPrice + TotalVAT
                SearchAndReplace(hoofdfact, "#TOTAALEXBTW#", FormatCurrency(TotalPrice))
                SearchAndReplace(hoofdfact, "#TOTAALBTW#", FormatCurrency(TotalVAT))
                SearchAndReplace(hoofdfact, "#TOTAALINBTW#", FormatCurrency(Total))
                SearchAndReplace(hoofdfact, "#BTWPERC#", "BTW " & salessettings.VatPercentage.ToString("0.##") & " %")
                If Not client.BankAccountNumber Is Nothing Or Not client.BankAccountNumber = "" Then
                    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", client.BankAccountNumber)
                Else
                    SearchAndReplace(hoofdfact, "#REKENINGNUMMER#", salessettings.BankAccountNumber)
                End If
                'DOC OPSLAAN
                Dim response = service.InsertUpdateProjectInvoice(invoice)
                If response.Success = True Then
                    hoofdfact.MainDocumentPart.Document.Save()
                    hoofdfact.Close()
                    hoofdfact.Dispose()
                    Dim success = UploadFileToFtp(path, NewFilename, ftp)
                    If success = True Then
                        DeleteTempFile(NewFilename)
                    End If
                    Return FunctionResponse
                Else
                    DeleteFtpFile(path, NewFilename, ftp)
                    'System.IO.File.Delete(My.Settings.InvoiceURL & Date.Now.Year & "\" & NewFilename)
                    AddMessage("Error", "De factuur Is niet opgemaakt, gelieve opnieuw tot proberen Of contact op te nemen met de administrator", "Fout!")
                    FunctionResponse.AddError(response.Messages)
                    hoofdfact.Dispose()
                    Return FunctionResponse
                End If
                'hoofdfact.MainDocumentPart.Document.Save()
                'hoofdfact.Close()
                'hoofdfact.Dispose()


            Catch ex As Exception
            Finally
                For Each order In co
                    For Each det In order.Details
                        ResponseFin = service.SetChangeOrderDetailInvoiced(det.Id)

                    Next
                Next

            End Try
            Return ResponseFin
        End Function
        Public Shared Sub AddTextToBookmark(bookmark As String, textToAdd As String, document As WordprocessingDocument)
            Dim bookmarkMap As IDictionary(Of [String], BookmarkStart) = New Dictionary(Of [String], BookmarkStart)()

            Dim doc = document.MainDocumentPart
            For Each bookmarkStart As BookmarkStart In doc.RootElement.Descendants(Of BookmarkStart)()
                bookmarkMap(bookmarkStart.Name) = bookmarkStart
            Next
            For Each bookmarkStart As BookmarkStart In bookmarkMap.Values
                If bookmarkStart.Name = bookmark Then
                    bookmarkStart.InsertAfterSelf(New Run(New Text(textToAdd)))
                End If
            Next

        End Sub
        Public Shared Sub SearchAndReplace(wordDoc As WordprocessingDocument, bookmark As String, text As String)
            text = System.Security.SecurityElement.Escape(text)
            Dim docText As String = Nothing
            Using SR As New StreamReader(wordDoc.MainDocumentPart.GetStream())
                docText = SR.ReadToEnd()
            End Using

            Dim regexText As New Regex(bookmark)
            docText = regexText.Replace(docText, text)

            Using sw As New StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create))

                sw.Write(docText)
            End Using


            For Each footerPart In wordDoc.MainDocumentPart.FooterParts
                For Each currentText In footerPart.RootElement.Descendants(Of DocumentFormat.OpenXml.Wordprocessing.Text)()
                    If (currentText.Text.Contains(bookmark)) Then
                        currentText.Text = currentText.Text.Replace(bookmark, text)
                    End If

                Next
            Next
        End Sub
        Public Shared Sub SearchAndReplaceWithNewLines(wordDoc As WordprocessingDocument, bookmark As String, text As String)

            'get all the text elements
            Dim texts As IEnumerable(Of Text) = wordDoc.MainDocumentPart.Document.Body.Descendants(Of Text)()
            'filter them to the ones that contain the QuoteLeft char
            Dim tokenTexts = texts.Where(Function(t) t.Text.Contains(bookmark))

            For Each token As Text In tokenTexts
                'get the parent element
                Dim parent = token.Parent
                'deep clone this Text element
                Dim newToken = token.CloneNode(True)

                'split the text into an array using a regex of all line terminators
                Dim lines = Regex.Split(text, vbCr & vbLf & "|" & vbCr & "|" & vbLf)

                'change the original text element to the first line
                DirectCast(newToken, Text).Text = lines(0)
                'if more than one line
                For i As Integer = 1 To lines.Length - 1
                    'append a break to the parent
                    parent.AppendChild(Of Break)(New Break())
                    'then append the next line
                    parent.AppendChild(Of Text)(New Text(lines(i)))
                Next

                'insert it after the token element
                token.InsertAfterSelf(newToken)
                'remove the token element
                token.Remove()
            Next

            wordDoc.MainDocumentPart.Document.Save()


        End Sub
        Private Shared Function GetRunPropertyFromTableCell(rowCopy As TableRow, cellIndex As Integer) As RunProperties
            Dim runProperties = New RunProperties()
            Dim fontname = "Avenir-Book"
            Dim fontSize = "18"
            'Try
            '    fontname = rowCopy.Descendants(Of TableCell)().ElementAt(cellIndex).GetFirstChild(Of Paragraph)().GetFirstChild(Of ParagraphProperties)().GetFirstChild(Of ParagraphMarkRunProperties)().GetFirstChild(Of RunFonts)().Ascii
            '    'swallow
            'Catch
            'End Try
            'Try
            '    fontSize = rowCopy.Descendants(Of TableCell)().ElementAt(cellIndex).GetFirstChild(Of Paragraph)().GetFirstChild(Of ParagraphProperties)().GetFirstChild(Of ParagraphMarkRunProperties)().GetFirstChild(Of FontSize)().Val
            '    'swallow
            'Catch
            'End Try
            runProperties.AppendChild(New RunFonts() With {.Ascii = fontname})
            runProperties.AppendChild(New FontSize() With {.Val = fontSize})
            Return runProperties
        End Function
        Public Shared Function GetContractActivityPrice(contractactid As Integer) As Decimal
            Dim service = ServiceFactory.GetProjectService()
            Return service.GetContractActivityPrice(contractactid)
        End Function
        Public Shared Function ConnectToFtp() As Chilkat.Ftp2
            Dim ftp As New Chilkat.Ftp2()
            ftp.Hostname = My.Settings.FTPHost
            ftp.Username = My.Settings.FTPUser
            ftp.Password = My.Settings.FTPPassword
            Dim success As Boolean = ftp.Connect()
            If (success <> True) Then
                MakeLog("Connect to ftp value", ftp.LastErrorText)
                Exit Function
            End If
            Return ftp
        End Function
        Public Shared Function UploadFileToFtp(path As String, filename As String, ftp As Chilkat.Ftp2) As Boolean
            ftp.ChangeRemoteDir(".")
            Dim success As Boolean = ftp.ChangeRemoteDir(path)
            If (success <> True) Then
                Debug.WriteLine(ftp.LastErrorText)
                Exit Function
            End If
            success = ftp.PutFile(My.Settings.localTempPath & filename, filename)
            If (success <> True) Then
                Debug.WriteLine(ftp.LastErrorText)
                Exit Function
            End If
            Return success
        End Function
        Public Shared Function DeleteTempFile(filename As String) As Boolean
            Try
                System.IO.File.Delete(My.Settings.localTempPath & filename)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function
        Public Shared Function DeleteFtpFile(path As String, filename As String, ftp As Chilkat.Ftp2) As Boolean
            ftp.ChangeRemoteDir(".")
            Dim success As Boolean = ftp.ChangeRemoteDir(path)
            If (success <> True) Then
                Debug.WriteLine(ftp.LastErrorText)
                Exit Function
            End If
            success = ftp.DeleteRemoteFile(filename)
            If (success <> True) Then
                Debug.WriteLine(ftp.LastErrorText)
                Exit Function
            End If
            Return success
        End Function

        Public Shared Function MakeLog(text As String, value As String)
            Dim bo As New LogBO
            bo.Text = text
            bo.Value = value
            Dim lservice = ServiceFactory.GetLogService
            lservice.InsertUpdateLog(bo)
        End Function
        Public Class Select2DTO
            ' as select2 is formed like id and text so we used DTO
            Public Property id() As Integer
                Get
                    Return m_id
                End Get
                Set(value As Integer)
                    m_id = value
                End Set
            End Property
            Private m_id As Integer
            Public Property text() As String
                Get
                    Return m_text
                End Get
                Set(value As String)
                    m_text = value
                End Set
            End Property
            Private m_text As String
            Public Property group() As String
                Get
                    Return m_group
                End Get
                Set(value As String)
                    m_group = value
                End Set
            End Property
            Private m_group As String
        End Class

    End Class


End Namespace