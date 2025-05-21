Public Class HomeController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Home
    <Route("Home/Index", name:="HomeIndex")>
    <Route("Home", name:="Home")>
    <Route("~/", name:="defaultroute")>
    Function Index() As ActionResult
        'Dim model As New ProjectModel
        ViewData("LatestNews") = GetLatestNews(4)
        Return View()
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