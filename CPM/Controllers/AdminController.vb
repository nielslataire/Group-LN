Imports System.Web.Mvc
Imports BO

Namespace Controllers
    <Authorize>
    Public Class AdminController
        Inherits Controllers.ApplicationBaseController

        ' GET: Admin
        Function Index() As ActionResult
            Return View()
        End Function
        Function Settings() As ActionResult
            Return View()
        End Function
        Function Activiteiten() As ActionResult
            Dim viewmodel As New ActivitiesModel
            viewmodel.Activities = GetActivitiesList()
            viewmodel = FillInSelectLists(viewmodel)
            Return View(viewmodel)

        End Function
        Function DetailActivity(id As Integer) As ActionResult

            Dim viewModel = New ActivitiesModel
            Dim aservice = ServiceFactory.GetActivityService
            viewModel = FillInSelectLists(viewModel)
            viewModel.SelectedActivity = ServiceFactory.GetActivityService.GetActivitybyId(id).Value
            viewModel.SelectedGroup = viewModel.SelectedActivity.Group.ID
            Return PartialView("ActivityDetail", viewModel)


        End Function
        Function SaveActivity(actId As Integer, actName As String, ActGroupId As Integer) As ActionResult
            Dim dservice = ServiceFactory.GetActivityService
            Dim bo As New BO.ActivityBO
            bo.ID = actId
            bo.Name = actName
            bo.Group.ID = ActGroupId
            Dim response = dservice.InsertUpdate(bo)
            If response.Success = True Then
                AddMessage("success", "Activiteit " & bo.Name & " is bijgewerkt.", "Geslaagd!")
            Else
                AddMessage("error", "Bijwerken van activiteit " & bo.Name & " is mislukt.", "Fout!")

            End If

            Return RedirectToAction("Activiteiten", "Admin")

        End Function
        <HttpGet>
        Function DeleteActivityModal(id As Integer) As ActionResult
            Dim viewModel = New ActivityDeleteModel
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetActivityService
                viewModel.Activity = dservice.GetActivitybyId(id).Value
                Dim filter As New BO.CompanyFilter
                Dim idlist As New List(Of Integer)
                idlist.Add(id)
                filter.SelectedActivities = idlist
                Dim cservice = ServiceFactory.GetCompanyService
                viewModel.ActivityCount = cservice.GetCompanyBySearchfilter(filter).Values.Count
            End If
            Return PartialView("DeleteActivityModal", viewModel)
        End Function
        Function DeleteActivity(Id As Integer) As ActionResult
            Dim dservice = ServiceFactory.GetActivityService
            Dim idlist As New List(Of Integer)
            idlist.Add(Id)
            Dim response = dservice.Delete(idlist)
            If response.Success = True Then

                AddMessage("", "De activiteit is verwijderd.", "Geslaagd!")
            Else
                AddMessage("error", "De activiteit is niet verwijderd, gelieve later opnieuw te proberen of neem contact op met de administrator.", "Fout!")

            End If

            Return RedirectToAction("Activiteiten", "Admin")

        End Function
        Function GetActivitiesList() As List(Of BO.ActivityGroupBO)
            Dim aservice = ServiceFactory.GetActivityService
            Return aservice.GetActivityGroups().Values.OrderBy(Function(m) m.Lot).ToList()
        End Function
        Function DetailGroup(id As Integer) As ActionResult

            Dim viewModel = New ActivitiesModel
            Dim aservice = ServiceFactory.GetActivityService
            viewModel = FillInSelectLists(viewModel)
            viewModel.SelectedActivityGroup = ServiceFactory.GetActivityService.GetActivityGroupbyId(id).Values.FirstOrDefault
            Return PartialView("GroupDetail", viewModel)


        End Function
        Function SaveGroup(groupId As Integer, groupName As String, groupLot As String) As ActionResult
            Dim dservice = ServiceFactory.GetActivityService
            Dim bo As New BO.ActivityGroupBO
            bo.ID = groupId
            bo.Name = groupName
            bo.Lot = groupLot
            Dim response = dservice.InsertUpdateGroup(bo)
            If response.Success = True Then
                AddMessage("success", "De groep " & bo.Name & " is bijgewerkt.", "Geslaagd!")
            Else
                AddMessage("error", "Bijwerken van groep " & bo.Name & " is mislukt.", "Fout!")

            End If

            Return RedirectToAction("Activiteiten", "Admin")

        End Function
        <HttpGet>
        Function DeleteGroupModal(id As Integer) As ActionResult
            Dim viewModel = New BO.ActivityGroupBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetActivityService
                viewModel = dservice.GetActivityGroupbyId(id).Value
            End If
            Return PartialView("DeleteGroupModal", viewModel)
        End Function
        Function DeleteGroup(id As Integer) As ActionResult
            Dim dservice = ServiceFactory.GetActivityService
            Dim idlist As New List(Of Integer)
            idlist.Add(id)
            Dim response = dservice.DeleteGroup(idlist)
            If response.Success = True Then

                AddMessage("", "De groep is verwijderd.", "Geslaagd!")
            Else
                AddMessage("error", "De Groep is niet verwijderd, gelieve later opnieuw te proberen of neem contact op met de administrator.", "Fout!")

            End If

            Return RedirectToAction("Activiteiten", "Admin")

        End Function


        'VACATIONDAYS
        Function VacationDays() As ActionResult
            Return View()
        End Function
        <HttpGet>
        Function GetVacationDays() As JsonResult
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetVacationDaysGeneral()
            Dim rows
            If (response.Success) Then

                Dim dataSource = From b In response.Values Select New With {
                                                               .id = b.Id,
                                                               .title = "verlofdag",
                                                               .year = b.VacationDay.Year,
                                                               .month = b.VacationDay.Month,
                                                               .day = b.VacationDay.Day
                }
                rows = dataSource.ToArray()
            End If
            Dim retVal = Json(rows, JsonRequestBehavior.AllowGet)
            Return retVal
        End Function
        <HttpPost>
        Function AddVacationDay(dag As Date) As Integer
            Dim day As New VacationDayBO
            day.VacationDay = dag
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.InsertUpdateVacationDay(day)
            If response.Success = True Then
                Return response.Messages(0).Message
            Else
                Return 0
            End If

        End Function
        <HttpPost>
        Function DeleteVacationDay(id As Integer) As Boolean
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


        Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
            TempData("Message") = message
            TempData("MessageType") = messagetype
            TempData("MessageTitle") = messagetitle
        End Sub
        Function FillInSelectLists(viewmodel As ActivitiesModel) As ActivitiesModel
            Dim aservice = ServiceFactory.GetActivityService
            Dim response = aservice.GetActivitiesForSelect
            If (response.Success) Then viewmodel.SelectListActivities = response.Values

            Dim response2 = aservice.GetActivityGroupsForSelect
            If (response2.Success) Then
                viewmodel.SelectListGroupsForActivity = response2.Values
                viewmodel.SelectListGroups = response2.Values
            End If


            Return viewmodel
        End Function

    End Class
End Namespace