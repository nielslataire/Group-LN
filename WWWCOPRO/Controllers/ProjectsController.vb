Imports System.Web.Mvc
Imports BO
Imports Facade
Namespace Controllers
    Public Class ProjectsController
        Inherits System.Web.Mvc.Controller

        '
        ' GET: /Projects
        <Route("Projects/{id?}", Name:="ProjectById")>
        Function Index(Optional Type As ProjectType = 0, Optional id As Integer = 0) As ActionResult
            If Not id = 0 Then
                ViewData("LatestNews") = GetLatestNews(4)
                Dim model As New ProjectDetailModel
                Dim service = ServiceFactory.GetProjectService
                Dim response = service.GetProjectByID(id)
                If (response.Success) Then model.Data = response.Values.FirstOrDefault
                'sort pictures
                model.Data.Pictures = model.Data.Pictures.Where(Function(m) Not m.Type = PictureType.Nieuws).ToList
                model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

                model.News = service.GetNewsByProjectId(id).Values.OrderByDescending(Function(m) m.NewsDate).ToList
                'sort news

                'Developer
                Dim companyservice = ServiceFactory.GetCompanyService
                Dim response2 = companyservice.GetCompanyByID(model.Data.Developer.ID)
                If (response.Success) Then model.Developer = response2.Values.FirstOrDefault
                'Builder
                response2 = companyservice.GetCompanyByID(model.Data.Builder.ID)
                If (response.Success) Then model.Builder = response2.Values.FirstOrDefault
                'Architect
                response2 = companyservice.GetCompanyByID(model.Data.Architect.ID)
                If (response.Success) Then model.Architect = response2.Values.FirstOrDefault
                'Engineer
                response2 = companyservice.GetCompanyByID(model.Data.Engineer.ID)
                If (response.Success) Then model.Engineer = response2.Values.FirstOrDefault
                'Securitycoordinator
                response2 = companyservice.GetCompanyByID(model.Data.SecurityCoordinator.ID)
                If (response.Success) Then model.SecurityCoordinator = response2.Values.FirstOrDefault
                'EPB Reporter
                response2 = companyservice.GetCompanyByID(model.Data.EpbReporter.ID)
                If (response.Success) Then model.EpbReporter = response2.Values.FirstOrDefault
                ViewData("title") = "Group LN - " & model.Data.Name
                Return View("Detail", model)
            Else
                ViewData("LatestNews") = GetLatestNews(4)
                If Type = ProjectType.Woonproject Then
                    ViewData("Title") = "Group LN - Woonprojecten"
                Else
                    ViewData("Title") = "Group LN - Commercieel"
                End If

                Dim model As New ProjectModel
                Dim service = ServiceFactory.GetProjectService
                Dim response = service.GetProjectsForList(Type, 2)
                If (response.Success) Then model.Projects = response.Values
                Return View(model)
            End If

        End Function
        <Route("Projects/ProjectBySlug/{slug}", name:="ProjectBySlug")>
        Function ProjectBySlug(slug As String) As ActionResult

            ViewData("LatestNews") = GetLatestNews(4)
            Dim model As New ProjectDetailModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectBySlug(slug)
            If (response.Success) Then model.Data = response.Values.FirstOrDefault
            'sort pictures
            model.Data.Pictures = model.Data.Pictures.Where(Function(m) Not m.Type = PictureType.Nieuws).ToList
            model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

            model.News = service.GetNewsByProjectId(model.Data.Id).Values.OrderByDescending(Function(m) m.NewsDate).ToList
            'sort news

            'Developer
            Dim companyservice = ServiceFactory.GetCompanyService
            Dim response2 = companyservice.GetCompanyByID(model.Data.Developer.ID)
            If (response.Success) Then model.Developer = response2.Values.FirstOrDefault
            'Builder
            response2 = companyservice.GetCompanyByID(model.Data.Builder.ID)
            If (response.Success) Then model.Builder = response2.Values.FirstOrDefault
            'Architect
            response2 = companyservice.GetCompanyByID(model.Data.Architect.ID)
            If (response.Success) Then model.Architect = response2.Values.FirstOrDefault
            'Engineer
            response2 = companyservice.GetCompanyByID(model.Data.Engineer.ID)
            If (response.Success) Then model.Engineer = response2.Values.FirstOrDefault
            'Securitycoordinator
            response2 = companyservice.GetCompanyByID(model.Data.SecurityCoordinator.ID)
            If (response.Success) Then model.SecurityCoordinator = response2.Values.FirstOrDefault
            'EPB Reporter
            response2 = companyservice.GetCompanyByID(model.Data.EpbReporter.ID)
            If (response.Success) Then model.EpbReporter = response2.Values.FirstOrDefault
            ViewData("title") = "Group LN - " & model.Data.Name
            Return View("Detail", model)


        End Function
        Function Detail(model As ProjectDetailModel) As ActionResult
          

            Return View(model)

        End Function
        <Route("Projects/Photos/{slug}", name:="ProjectPhotosBySlug")>
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

            ViewData("title") = "Group LN - " & model.ProjectName & " - Foto's"

            Return View(model)

        End Function
        <Route("Projects/News/{slug}", Name:="ProjectNewsBySlug")>
        Function News(slug As String, Optional newsid As Integer = 0) As ActionResult
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

            ViewData("title") = "Group LN - " & model.ProjectName & " - Nieuws"

            If Not newsid = 0 Then

                model.NewsId = newsid
                Dim newsitem As ProjectNewsBO = model.News.Where(Function(m) m.Id = newsid).FirstOrDefault
                ViewData("ogtitle") = model.ProjectName & " - " & newsitem.TitleNL
                ViewData("ogtype") = "website"
                ViewData("ogdescription") = newsitem.TextNL
                ViewData("ogimage") = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & newsitem.Picture.Name
                'ViewData("ogurl") = Url.Action("News", "Projects", New With {.id = model.ProjectId, .newsid = newsitem.Id})
            Else

            End If
            Return View(model)

        End Function
        <Route("Projects/{id}/News", Name:="ProjectNewsById")>
        Function News(id As Integer, Optional newsid As Integer = 0) As ActionResult
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

            ViewData("title") = "Group LN - " & model.ProjectName & " - Nieuws"
            If Not newsid = 0 Then

                model.NewsId = newsid
                Dim newsitem As ProjectNewsBO = model.News.Where(Function(m) m.Id = newsid).FirstOrDefault
                ViewData("ogtitle") = model.ProjectName & " - " & newsitem.TitleNL
                ViewData("ogtype") = "website"
                ViewData("ogdescription") = newsitem.TextNL
                ViewData("ogimage") = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & newsitem.Picture.Name
                'ViewData("ogurl") = Url.Action("News", "Projects", New With {.id = model.ProjectId, .newsid = newsitem.Id})
            Else

            End If

            Return View(model)

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
    End Class


End Namespace
