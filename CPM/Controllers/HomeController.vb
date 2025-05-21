Imports BO
Imports Microsoft.AspNet.Identity

<Authorize>
Public Class HomeController
    Inherits Controllers.ApplicationBaseController

    Function Index() As ActionResult
        Dim model As New HomeModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectsForList(0, ProjectStatusType.Uitvoering, User.Identity.GetUserId())
        If (response.Success) Then model.Projects = response.Values
        response = service.GetProjectsForList(0, ProjectStatusType.Opgeleverd, User.Identity.GetUserId())
        If (response.Success) Then model.OldProjects = response.Values
        Dim service2 = ServiceFactory.GetClientService
        Dim response2 = service2.GetClientAccountsByDateDeedofSale()
        If (response2.Success) Then model.DeedofSaleWarnings = response2.Values
        If Not User.IsInRole("Admin") Then
            Dim iservice = ServiceFactory.GetInsuranceService
            Dim iresponse = iservice.CheckInsurances(User.Identity.GetUserId())
            If (iresponse.Success) Then model.InsuranceWarnings = iresponse.Values
        Else
            Dim iservice = ServiceFactory.GetInsuranceService
            Dim iresponse = iservice.CheckInsurances()
            If (iresponse.Success) Then model.InsuranceWarnings = iresponse.Values
        End If
        If Not User.IsInRole("Admin") Then
            Dim iresponse = service.CheckProjectFinished(User.Identity.GetUserId())
            If (iresponse.Success) Then model.ProjectInfo = iresponse.Values
        Else

            Dim iresponse = service.CheckProjectFinished()
            If (iresponse.Success) Then model.ProjectInfo = iresponse.Values
        End If
        Return View(model)
    End Function
    <HttpPost>
    Public Function GetGeneralSearchList(term As String) As JsonResult
        Dim pservice = ServiceFactory.GetCompanyService
        Dim presponse = pservice.GetCompanyForSearchList(term)
        Dim iList As New List(Of SelectBO)
        If (presponse.Success) Then iList = presponse.Values
        Dim cservice = ServiceFactory.GetContactService
        Dim cresponse = cservice.GetContactsForSearchList(term)
        If (cresponse.Success) Then iList.AddRange(cresponse.Values)
        Dim clientservice = ServiceFactory.GetClientService
        Dim clientresponse = clientservice.GetClientAccountsForSearchList(term)
        If (clientresponse.Success) Then iList.AddRange(clientresponse.Values)
        Dim projectservice = ServiceFactory.GetProjectService
        Dim projectresponse = projectservice.GetProjectsForSearchList(term)
        If (clientresponse.Success) Then iList.AddRange(projectresponse.Values)
        Return Json(iList, JsonRequestBehavior.AllowGet)
    End Function
    <HttpPost>
    Public Function SelectSearchItem(id As Integer, type As String)
        If type = "Company" Then
            Return Json(Url.Action("Detail", "Leveranciers", New With {Key .id = id}))
        ElseIf type = "Contact" Then
            Return Json(Url.Action("Detail", "Leveranciers", New With {Key .id = id}))
        ElseIf type = "Client" Then
            Return Json(Url.Action("Detail", "Klanten", New With {Key .clientid = id}))
        ElseIf type = "Project" Then
            Return Json(Url.Action("Detail", "Projecten", New With {Key .projectid = id}))

        End If

    End Function

    Function About() As ActionResult
        ViewData("Message") = "Your application description page."

        Return View()
    End Function

    Function Contact() As ActionResult
        ViewData("Message") = "Your contact page."

        Return View()
    End Function
End Class
