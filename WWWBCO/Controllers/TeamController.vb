Public Class TeamController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Team
    <Route("Team")>
    Function Index() As ActionResult
        ViewData("LatestNews") = GetLatestNews(4)
        'Metatags
        ViewBag.Metatitle = "BCO - Team"
        ViewBag.MetaURL = "http://www.bouwenconstructie.be/team"
        ViewBag.MetaDescription = "Ontdek het volledige aanbod nieuwbouwwoningen en nieuwbouwappartementen die te koop staan in Vlaanderen."
        ViewBag.MetaImageUrl = "http://www.bouwenconstructie.be/content/img/logo-default.png"
        Return View()
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