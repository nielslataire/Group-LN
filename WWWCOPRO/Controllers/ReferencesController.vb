Public Class ReferencesController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /References
    <Route("References/{id?}")>
    Function Index(Optional id As Integer = 0) As ActionResult
        If Not id = 0 Then
            ViewData("LatestNews") = getlatestNews(4)

            Dim model As New ReferenceDetailModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectByID(id)
            If (response.Success) Then model.Data = response.Values.FirstOrDefault
            'sort pictures
            model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

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
            ViewData("Title") = "Group LN - " & model.Data.Name
            Return View("detail", model)
        Else
            ViewData("LatestNews") = GetLatestNews(4)
            Dim model As New ReferencesModel
            Dim service = ServiceFactory.GetProjectService

            Dim response = service.GetProjectsForList(0, 1)
            If (response.Success) Then model.Projects = response.Values
            model.Projects = model.Projects.OrderByDescending(Function(m) m.DeliveryDate).ToList
            Return View(model)
        End If

    End Function
    <Route("References/ReferenceBySlug/{slug}", name:="ReferenceBySlug")>
    Function ReferenceBySlug(slug As String) As ActionResult
        ViewData("LatestNews") = GetLatestNews(4)
        Dim model As New ReferenceDetailModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectBySlug(slug)
        If (response.Success) Then model.Data = response.Values.FirstOrDefault
        'sort pictures
        model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

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
        ViewData("Title") = "Group LN - " & model.Data.Name
        Return View("detail", model)


    End Function
    Function Detail(model As ReferenceDetailModel) As ActionResult
      

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