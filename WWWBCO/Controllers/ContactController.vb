Imports Postal
Imports Facade
Imports cpm
Public Class ContactController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Contact
    <Route("Contact")>
    Function Index() As ActionResult
        Dim model As New MailModel
        ViewData("LatestNews") = GetLatestNews(4)
        'Metatags
        ViewBag.Metatitle = "BCO - Contact"
        Return View(model)
    End Function
    <HttpPost>
    <Route("Contact")>
    Function Index(model As MailModel) As ActionResult
        ViewData("LatestNews") = GetLatestNews(4)
        'Metatags
        ViewBag.Metatitle = "BCO - Contact"
        ViewBag.MetaDescription = "Ontdek het volledige aanbod nieuwbouwwoningen en nieuwbouwappartementen die te koop staan in Vlaanderen."
        ViewBag.MetaURL = "http://www.bouwenconstructie.be/contact"
        ViewBag.MetaImageUrl = "http://www.bouwenconstructie.be/content/img/logo-default.png"
        Return View(model)
    End Function
    ' GET: /Contact
    <Route("Contact")>
    Function Succes() As ActionResult
        Dim model As New MailModel
        ViewData("LatestNews") = GetLatestNews(4)
        'Metatags
        ViewBag.Metatitle = "BCO - Contact gelukt"
        Return View(model)
    End Function
    <Route("Contact/Send")>
    <HttpPost>
    <ValidateInput(False)>
    Function Send(model As MailModel) As ActionResult
        Dim errors As New ArrayList
        'if not valid then there where errors (required property not filled in or such) so return to show them
        'For Each key In ModelState.Keys
        '    If ModelState(key).Errors.Count > 0 Then
        '        errors(key) = ModelState(key).Errors()
        '    End If
        'Next
        ViewData("LatestNews") = GetLatestNews(4)
        If (Not ModelState.IsValid) Then Return View("index", model)
        If (ModelState.IsValid) Then
            Dim email As Object = New Email("ContactMail")
            email.[To] = model.EmailTo
            email.ContactName = model.ContactName
            email.Title = model.Title
            email.Message = model.Message
            email.Send()
            Dim internalemail As Object = New Email("InternalMail")
            internalemail.[To] = model.EmailTo
            internalemail.ContactName = model.ContactName
            internalemail.Title = model.Title
            internalemail.Message = model.Message
            internalemail.Phone = model.Phone
            internalemail.Send()
            AddMessage("success", "Uw bericht is verstuurd naar BCO, wij nemen zo snel mogelijk contact met u op.", "Geslaagd!")
            Return RedirectToAction("Succes", "Contact")
        Else
            Return View("index", model)
        End If
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

    Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
        TempData("Message") = message
        TempData("MessageType") = messagetype
        TempData("MessageTitle") = messagetitle
    End Sub
  
End Class