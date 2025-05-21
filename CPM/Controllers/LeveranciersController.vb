Imports System.Web.Mvc
Imports BO
Imports DAL
Imports Facade
Imports System.Web.Script.Serialization
Imports System.Web
Imports System.Linq
Imports System.Text.RegularExpressions
Imports CPM.Controllers.Extensions
Imports Rotativa
Imports System.IO
Imports System.Net
Imports System.Text
Imports Newtonsoft.Json.Linq

Namespace Controllers
    <Authorize>
    Public Class LeveranciersController
        Inherits Controllers.ApplicationBaseController

        ' GET: Leveranciers
        Function Index() As ActionResult
            Return View()
        End Function
        '--------------ZOEKEN--------------
        Function Zoeken() As ActionResult
            Dim model As New LeverancierSearchModel()
            FillInSearchSelectLists(model)
            Return View(model)
        End Function
        'httppostattribute tells mvc that this methode can only be called from a post action
        <HttpPostAttribute>
        Function Zoeken(model As LeverancierSearchModel) As ActionResult
            'if you check here you'll see that the model.selected properties are filled in with the selected ids and the model.companyname is filled in with the entered text
            'refill the activities and provinces properties
            FillInSearchSelectLists(model)
            'if not valid then there where errors (required property not filled in or such) so return to show them
            If (Not ModelState.IsValid) Then Return View(model)
            If Not model.Filter.SelectedActivities Is Nothing Then
                ViewData("selectedactivities") = String.Join(",", model.Filter.SelectedActivities.ToArray())
            Else
                ViewData("selectedactivities") = ""
            End If
            If Not model.Filter.SelectedProvince Is Nothing Then
                ViewData("selectedprovinces") = String.Join(",", model.Filter.SelectedProvince.ToArray())
            Else
                ViewData("selectedprovinces") = ""
            End If
            ViewData("FilterCompanyName") = model.Filter.CompanyName

            'go to the service with the selected search options
            'the service gives back a list of company
            'you put that list op companies in the model
            Dim service = ServiceFactory.GetCompanyService()
            Dim response = service.GetCompanyBySearchfilter(model.Filter)
            If (response.Success) Then model.Companies = response.Values
            If model.Companies.Count > 0 Then
                model.Searchempty = False
            Else
                model.Searchempty = True
            End If
            'return to the view again where you display that list of companies
            Return View(model)
        End Function
        Function ZoekenPrint(name As String, act As String, prov As String) As ActionResult

            'go to the service with the selected search options
            'the service gives back a list of company
            'you put that list op companies in the model
            Dim model As New LeverancierSearchModel
            model.Filter.CompanyName = name
            If Not act Is Nothing Then model.Filter.SelectedActivities = New List(Of Integer)(act.Split(",").Select(Function(m) Integer.Parse(m)))
            If Not prov Is Nothing Then model.Filter.SelectedProvince = New List(Of Integer)(prov.Split(",").Select(Function(m) Integer.Parse(m)))
            Dim service = ServiceFactory.GetCompanyService()
            Dim response = service.GetCompanyBySearchfilter(model.Filter)
            If (response.Success) Then model.Companies = response.Values
            If model.Companies.Count > 0 Then
                model.Searchempty = False
            Else
                model.Searchempty = True
            End If
            'return to the view again where you display that list of companies
            Return View(model)
        End Function
        Function ZoekenExportToExcel(name As String, act As String, prov As String) As ActionResult

            'go to the service with the selected search options
            'the service gives back a list of company
            'you put that list op companies in the model
            Dim model As New LeverancierSearchModel
            model.Filter.CompanyName = name
            If Not act Is Nothing Then model.Filter.SelectedActivities = New List(Of Integer)(act.Split(",").Select(Function(m) Integer.Parse(m)))
            If Not prov Is Nothing Then model.Filter.SelectedProvince = New List(Of Integer)(prov.Split(",").Select(Function(m) Integer.Parse(m)))
            FillInSearchSelectLists(model)
            Dim service = ServiceFactory.GetCompanyService()
            Dim response2 = service.GetCompanyBySearchfilter(model.Filter)
            If (response2.Success) Then model.Companies = response2.Values
            If model.Companies.Count > 0 Then
                model.Searchempty = False
            Else
                model.Searchempty = True
            End If

            Dim Headers As String() = New String(6) {"Bedrijfsnaam", "Straat", "Huisnr.", "Busnr.", "GSM", "Telefoon", "Email"}
            Dim rowKeys As String() = New String(6) {"h1", "h2", "h3", "h4", "h5", "h6", "h7"}
            'Dim rows = From m In model.Companies.AsQueryable() Select , m.Straat, m.Huisnummer, m.Busnummer, m.FormattedGSM, m.FormattedTelefoon, m.Email
            Dim rows = From m In model.Companies.AsQueryable() Select New With { _
                      .h1 = If(m.Bedrijfsnaam IsNot Nothing, m.Bedrijfsnaam, ""), _
                      .h2 = If(m.Straat IsNot Nothing, m.Straat, ""), _
                      .h3 = If(m.Huisnummer IsNot Nothing, m.Huisnummer, ""), _
                      .h4 = If(m.Busnummer IsNot Nothing, m.Busnummer, ""), _
                      .h5 = If(m.FormattedGSM IsNot Nothing, "'" & m.FormattedGSM, ""), _
                      .h6 = If(m.FormattedTelefoon IsNot Nothing, "'" & m.FormattedTelefoon, ""), _
                      .h7 = If(m.Email IsNot Nothing, m.Email, "")}
            Return Me.Excel("ExportBedrijven.xlsx", "Bedrijven", rows, Headers, rowKeys)
            'Dim companies = New System.Data.DataTable("Export")
            'companies.Columns.Add("Bedrijfsnaam")
            'companies.Columns.Add("Straat")
            'companies.Columns.Add("Huisnr.")
            'companies.Columns.Add("Busnr.")
            'companies.Columns.Add("GSM")
            'companies.Columns.Add("Telefoon")
            'companies.Columns.Add("Email")
            'For Each company In model.Companies
            '    companies.Rows.Add(company.Bedrijfsnaam, company.Straat, company.Huisnummer, company.Busnummer, "'" & company.FormattedGSM, "'" & company.FormattedTelefoon, company.Email)
            'Next

            'ExcelExport.ExportDatatable(companies, "Export bedrijven")
            'Dim grid = New GridView()
            'grid.DataSource = companies
            'grid.DataBind()
            'Response.ClearContent()
            'Response.Buffer = True
            'Response.AddHeader("content-disposition", "attachment;filename=export.xls")
            'Response.ContentType = "application/ms-excel"
            'Response.Charset = ""
            'Dim sw As New System.IO.StringWriter()
            'Dim htw As New HtmlTextWriter(sw)
            'grid.RenderControl(htw)
            'Response.Output.Write(sw.ToString())
            'Response.Flush()
            'Response.End()

            'return to the view again where you display that list of companies
            'Return View("Zoeken", "Leveranciers", model)
        End Function
        Function ZoekenExportToPdf(name As String, act As String, prov As String) As ActionResult
            Dim model As New LeverancierSearchModel
            model.Filter.CompanyName = name
            If Not act Is Nothing Then model.Filter.SelectedActivities = New List(Of Integer)(act.Split(",").Select(Function(m) Integer.Parse(m)))
            If Not prov Is Nothing Then model.Filter.SelectedProvince = New List(Of Integer)(prov.Split(",").Select(Function(m) Integer.Parse(m)))
            Dim service = ServiceFactory.GetCompanyService()
            Dim response = service.GetCompanyBySearchfilter(model.Filter)
            If (response.Success) Then model.Companies = response.Values
            If model.Companies.Count > 0 Then
                model.Searchempty = False
            Else
                model.Searchempty = True
            End If
            'return to the view again where you display that list of companies
            Dim a = New ViewAsPdf()
            a.ViewName = "ZoekenPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A4
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Dim ms As New MemoryStream(pdfBytes)
            Return New FileStreamResult(ms, "application/pdf")

        End Function
        Private Sub FillInSearchSelectLists(ByRef model As LeverancierSearchModel)
            'get the activities
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()
            If (response.Success) Then model.Filter.Activities = response.Values
            'If (response.Success) Then
            '    model.Filter.Activities = response.Values
            '    ViewBag.Activities = New MultiSelectList(model.Filter.Activities, "ID", "Name", "GroupName",new() {model.Filter.SelectedActivities })
            'End If



            'get the provinces
            Dim pService = ServiceFactory.GetProvinceService()
            Dim presponse = pService.GetProvinces()
            If (presponse.Success) Then model.Filter.Provinces = presponse.Values
        End Sub

        '--------------LIJSTEN-------------
        Function Lijsten() As ActionResult
            Return View()
        End Function

        '--------------TOEVOEGEN-----------
        Function Toevoegen() As ActionResult
            Dim model As New LeverancierModel
            'dit moet nog veranderd worden
            model.Company.Postcode.Country.CountryID = "19"
            model.Company.Postcode.Country.ISOCode = "BE"
            FillInAddSelectLists(model)
            Return View(model)
        End Function
        <HttpPostAttribute>
        Function Toevoegen(model As LeverancierModel, contacts As List(Of ContactBO), activities As List(Of ActivityBO), departments As List(Of DepartmentBO)) As ActionResult
            'if you check here you'll see that the model.selected properties are filled in with the selected ids and the model.companyname is filled in with the entered text
            'refill the activities and provinces properties
            FillInAddSelectLists(model)
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next

            If (Not ModelState.IsValid) Then Return View(model)
            If (ModelState.IsValid) Then
                model.Company.Postcode.PostcodeId = model.SelectedPostalcode
                If Not contacts Is Nothing Then model.Company.Contacts = contacts
                If Not departments Is Nothing Then model.Company.Departments = departments
                If Not activities Is Nothing Then model.Company.Activities = activities
                Dim service = ServiceFactory.GetCompanyService()
                Dim response = service.InsertUpdate(model.Company)
                If response.Success Then

                    AddMessage("success", model.Company.Bedrijfsnaam & " is toegevoegd als leverancier", "Geslaagd!")
                    model = New LeverancierModel()
                    RedirectToAction("Index", "Home")
                Else
                    AddMessage("error", model.Company.Bedrijfsnaam & " is NIET toegevoegd als leverancier", "Fout!")
                    Return View(model)
                End If
            Else
                Return View(model)
            End If
        End Function
        <HttpPost>
        Public Function AddSelectedActivitiy(ActId As String, Actname As String, ActGroup As String) As PartialViewResult
            Dim nActivity As New ActivityBO
            nActivity.ID = ActId
            nActivity.Name = Actname
            'nActivity.Group.Name = ActGroup
            ViewData("mode") = "add"
            Return PartialView("ActivityRow", nActivity)

        End Function
        <HttpPost>
        Public Function AddDepartment(Name As String, Street As String, Housenumber As String, Busnumber As String, Zipcode As Integer, Phone As String, Cellphone As String, Email As String) As PartialViewResult
            Dim nDepartment As New DepartmentBO
            'ophalen postcode
            Dim pservice = ServiceFactory.GetPostalcodeService()
            Dim presponse = pservice.GetPostalcodeById(Zipcode)
            If (presponse.Success) Then nDepartment.Postalcode = presponse.Values.FirstOrDefault
            nDepartment.Name = Name
            nDepartment.Street = Street
            nDepartment.Housenumber = Housenumber
            nDepartment.Busnumber = Busnumber
            nDepartment.Postalcode.PostcodeId = Zipcode
            If Not Phone Is Nothing Then nDepartment.Phone = Regex.Replace(Phone, "[^0-9]", "")
            If Not Cellphone Is Nothing Then nDepartment.CellPhone = Regex.Replace(Cellphone, "[^0-9]", "")
            nDepartment.Email = Email
            ViewData("mode") = "add"
            Return PartialView("DepartmentRow", nDepartment)

        End Function
        <HttpPost>
        Public Function AddContact(Salutation As String, Name As String, Firstname As String, JobFunction As String, Phone As String, Cellphone As String, Email As String) As PartialViewResult
            Dim nContact As New ContactBO
            nContact.Name = Name
            nContact.Firstname = Firstname
            nContact.Salutation = Salutation
            nContact.JobFunction = JobFunction
            If Not Phone Is Nothing Then nContact.Phone = Regex.Replace(Phone, "[^0-9]", "")
            If Not Cellphone Is Nothing Then nContact.CellPhone = Regex.Replace(Cellphone, "[^0-9]", "")
            nContact.Email = Email
            ViewData("mode") = "add"
            Return PartialView("ContactRow", nContact)

        End Function

        '----- EDIT COMPANY -----
        Function Bewerken(id As Integer, activetab As Integer) As ActionResult
            Dim model As New LeverancierModel
            If Not id = 0 Then

                Dim service = ServiceFactory.GetCompanyService
                Dim response = service.GetCompanyByID(id)
                If (response.Success) Then
                    model.Company = response.Value
                    model.AddedDepartments = model.Company.Departments
                    model.AddedContacts = model.Company.Contacts
                    model.AddedActivities = model.Company.Activities
                    model.SelectedCountry = model.Company.Postcode.Country.CountryID
                    model.SelectedPostalcode = model.Company.Postcode.PostcodeId
                    Dim s As String = model.Company.Postcode.Postcode & " - " & model.Company.Postcode.Gemeente
                    ViewData("PostcodeDisplayName") = s
                    ViewData("activetab") = activetab
                End If
            End If

            FillInAddSelectLists(model)
            Return View("Bewerken", model)
        End Function
        <HttpPost>
        Function Bewerken(model As LeverancierModel) As ActionResult
            If ModelState.IsValid Then
                If Not model.Company.CompanyId = 0 Then
                    'The selectedpostalcodeid and countryid are in temp variables in the model, so you have to reenter them into the bo here
                    model.Company.Postcode.PostcodeId = model.SelectedPostalcode
                    model.Company.Postcode.Country.CountryID = model.SelectedCountry
                    '---------------------------------------------------------------------------------

                    Dim cservice = ServiceFactory.GetCompanyService
                    Dim response = cservice.InsertUpdate(model.Company)
                    If response.Success Then
                        AddMessage("success", "De leverancier " & model.Company.Bedrijfsnaam & " is bijgewerkt.", "Geslaagd!")
                    Else
                        AddMessage("error", "De leverancier " & model.Company.Bedrijfsnaam & " is niet bijgewerkt, proberen het opnieuw of neem contact op met de administrator", "Fout!")
                    End If
                End If
                Return Json(New With {.success = True, .url = Url.Action("Bewerken", New With {Key .id = model.Company.CompanyId, .activetab = 0})})
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If



        End Function
        '----- EDIT COMPANY - ACTIVITIES -----
        Function EditAddActivity(model As LeverancierModel) As ActionResult
            Dim cservice = ServiceFactory.GetCompanyService
            Dim counter As Integer
            For Each x As Integer In model.SelectedActivities
                Dim cresponse = cservice.AddCompanyActivity(model.Company.CompanyId, x)
                If cresponse.Success Then
                    counter = counter + 1
                End If
            Next
            If counter = model.SelectedActivities.Count Then


                AddMessage("success", "Alle activiteiten zijn toegevoegd aan leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(model.Company.CompanyId), "Geslaagd!")
            ElseIf counter = 0 Then
                AddMessage("error", "Geen van de activiteiten zijn toegevoegd aan leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(model.Company.CompanyId), "Fout!")
            Else
                AddMessage("error", "Niet alle activiteiten zijn toegevoegd aan leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(model.Company.CompanyId), "Fout!")
            End If
            Return RedirectToAction("Bewerken", New With {Key .id = model.Company.CompanyId, .activetab = 1})
        End Function
        <HttpGet>
        Function DeleteActivityModal(id As Integer, companyid As Integer) As ActionResult
            Dim viewModel = New CompanyActivityModel
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetActivityService
                viewModel.Activity = dservice.GetActivitybyId(id).Value
                Dim cservice = ServiceFactory.GetCompanyService
                viewModel.CompanyName = cservice.GetCompanyNameById(companyid)
                viewModel.CompanyId = companyid
            End If
            Return PartialView("DeleteActivityModal", viewModel)
        End Function
        Function DeleteCompanyActivity(ByVal id As Integer, ByVal companyid As Integer) As ActionResult

            If Not id = 0 And Not companyid = 0 Then
                Dim dservice = ServiceFactory.GetCompanyService
                Dim response = dservice.DeleteCompanyActivity(companyid, id)
                If response.Success = True Then
                    AddMessage("", "De activiteit is verwijderd", "Geslaagd!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 1})

                Else
                    AddMessage("error", "De activiteit is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 1})
                End If
            End If
            Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 1})
        End Function
        '----- EDIT COMPANY - DEPARTMENTS -----
        <HttpGet>
        Function UpdateDepartment(id As Integer) As ActionResult
            Dim viewModel = New DepartmentModel
            FillInAddSelectListsDepartment(viewModel)
            If Not id = 0 Then
                viewModel.Department.ID = id
                Dim dservice = ServiceFactory.GetDepartmentService
                viewModel.Department = dservice.GetDepartmentById(viewModel.Department.ID).Value
                viewModel.SelectedCountry = viewModel.Department.Postalcode.Country.CountryID
                viewModel.SelectedPostalcode = viewModel.Department.Postalcode.PostcodeId
            End If

            Return PartialView("AddUpdateDepartment", viewModel)
        End Function
        <HttpGet>
        Function AddDepartment(id As Integer) As ActionResult
            Dim viewModel = New DepartmentModel
            FillInAddSelectListsDepartment(viewModel)
            viewModel.Department.Company.ID = id
            Return PartialView("AddUpdateDepartment", viewModel)
        End Function
        <HttpPost>
        Function AddUpdateDepartment(viewmodel As DepartmentModel) As ActionResult
            If ModelState.IsValid Then
                Dim dservice = ServiceFactory.GetDepartmentService
                Dim BO As New DepartmentBO
                If Not viewmodel.Department.ID = 0 Then
                    BO = dservice.GetDepartmentById(viewmodel.Department.ID).Value
                End If
                BO.Company.ID = viewmodel.Department.Company.ID
                BO.Busnumber = viewmodel.Department.Busnumber
                BO.CellPhone = viewmodel.Department.CellPhone
                BO.Email = viewmodel.Department.Email
                BO.Housenumber = viewmodel.Department.Housenumber
                BO.Name = viewmodel.Department.Name
                BO.Phone = viewmodel.Department.Phone
                BO.Postalcode.PostcodeId = viewmodel.SelectedPostalcode
                BO.Street = viewmodel.Department.Street
                Dim response = dservice.InsertUpdate(BO)
                Dim nMessage As New NotificationMessage()

                If response.Success = True Then
                    If viewmodel.Department.ID = 0 Then
                        AddMessage("success", "De afdeling " & BO.Name & " is toegevoegd aan leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(BO.Company.ID), "Geslaagd!")
                    Else
                        AddMessage("success", "De afdeling " & BO.Name & " is bijgewerkt bij leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(BO.Company.ID), "Geslaagd!")
                    End If

                Else
                    If viewmodel.Department.ID = 0 Then
                        AddMessage("error", "De afdeling " & BO.Name & " is niet toegevoegd aan leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(BO.Company.ID), "Fout!")
                    Else
                        AddMessage("error", "De afdeling " & BO.Name & " is niet bijgewerkt bij leverancier " & ServiceFactory.GetCompanyService.GetCompanyNameById(BO.Company.ID), "Fout!")
                    End If

                End If

                Return Json(New With {.success = True, .url = Url.Action("Bewerken", New With {Key .id = viewmodel.Department.Company.ID, .activetab = 2})})


            Else

                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function DeleteDepartment(id As Integer, companyid As Integer) As ActionResult
            Dim bos As New List(Of DepartmentBO)
            Dim bo As New DepartmentBO
            bo.ID = id
            bos.Add(bo)
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetDepartmentService
                Dim response = dservice.Delete(bos)
                If response.Success = True Then
                    AddMessage("", "De afdeling is verwijderd", "Geslaagd!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 2})
                Else
                    AddMessage("error", "De afdeling is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 2})
                End If

            End If

        End Function
        <HttpGet>
        Function DeleteDepartmentModal(id As Integer) As ActionResult
            Dim viewModel = New DepartmentModel
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetDepartmentService
                viewModel.Department = dservice.GetDepartmentById(id).Value

            End If

            Return PartialView("DeleteDepartmentModal", viewModel)
        End Function
        Private Sub FillInAddSelectListsDepartment(ByRef model As DepartmentModel)

            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            'Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            'If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID

        End Sub
        '----- EDIT COMPANY - CONTACTS -----
        <HttpGet>
        Function UpdateContact(id As Integer) As ActionResult
            Dim viewModel = New ContactModel
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetContactService
                viewModel.Contact = dservice.GetContactById(id).Value
                viewModel.SelectableDepartments = GetDepartmentList(viewModel.Contact.Company.ID)
            End If
            ViewData("SalutationList") = GetSalutationList()
            Return PartialView("AddUpdateContact", viewModel)
        End Function
        <HttpGet>
        Function AddContact(id As Integer) As ActionResult
            Dim viewModel = New ContactModel

            viewModel.Contact.Company.ID = id
            viewModel.Contact.Id = 0
            viewModel.SelectableDepartments = GetDepartmentList(id)
            ViewData("SalutationList") = GetSalutationList()
            Return PartialView("AddUpdateContact", viewModel)
        End Function
        <HttpPost>
        Function AddUpdateContact(viewmodel As ContactModel) As ActionResult
            If ModelState.IsValid Then
                Dim dservice = ServiceFactory.GetContactService
                Dim response = dservice.InsertUpdate(viewmodel.Contact)
                If response.Success = True Then
                    If viewmodel.Contact.Id = 0 Then
                        AddMessage("success", viewmodel.Contact.Name & " " & viewmodel.Contact.Firstname & " is toegevoegd als contact aan leverancier " & viewmodel.Contact.Company.Display, "Geslaagd!")
                    Else
                        AddMessage("success", "Contact " & viewmodel.Contact.Name & " " & viewmodel.Contact.Firstname & " is bijgewerkt bij leverancier " & viewmodel.Contact.Company.Display, "Geslaagd!")

                    End If

                Else
                    If viewmodel.Contact.Id = 0 Then
                        AddMessage("error", viewmodel.Contact.Name & " " & viewmodel.Contact.Firstname & " is niet toegevoegd als contact aan leverancier " & viewmodel.Contact.Company.Display, "Fout!")
                    Else
                        AddMessage("error", "Contact " & viewmodel.Contact.Name & " " & viewmodel.Contact.Firstname & " is niet bijgewerkt bij leverancier " & viewmodel.Contact.Company.Display, "Fout!")

                    End If
                End If
                Return Json(New With {.success = True, .url = Url.Action("Bewerken", New With {Key .id = viewmodel.Contact.Company.ID, .activetab = 3})})
                'Return Json(Url.Action("Bewerken", New With {Key .id = viewmodel.Department.Company.ID, .activetab = 2}))

            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function DeleteContact(id As Integer, companyid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            Idlist.Add(id)
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetContactService
                Dim response = dservice.Delete(Idlist)
                If response.Success = True Then
                    AddMessage("", "De contact is verwijderd", "Geslaagd!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 3})
                Else
                    AddMessage("error", "De contact is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Bewerken", New With {Key .id = companyid, .activetab = 3})
                End If

            End If

        End Function
        <HttpGet>
        Function DeleteContactModal(id As Integer) As ActionResult
            Dim viewModel = New ContactBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetContactService
                viewModel = dservice.GetContactById(id).Value

            End If

            Return PartialView("DeleteContactModal", viewModel)
        End Function
        Function GetDepartmentList(id As Integer) As List(Of IdNameBO)
            Dim dep As List(Of DepartmentBO) = ServiceFactory.GetCompanyService.GetCompanyByID(id).Value.Departments
            Dim itemlist As New List(Of IdNameBO)
            Dim emptyitem As New IdNameBO
            emptyitem.ID = 0
            emptyitem.Display = "Geen Afdeling"
            itemlist.Add(emptyitem)
            For Each x In dep
                Dim item As New IdNameBO
                item.ID = x.ID
                item.Display = x.Name
                itemlist.Add(item)
            Next
            Return itemlist
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
        End Class

        '--------------DETAIL------------
        Function Detail(id As Integer) As ActionResult

            Dim model As New LeverancierModel()
            Dim cservice = ServiceFactory.GetCompanyService()
            Dim cresponse = cservice.GetCompanyByID(id)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Company = cresponse.Value
            End If
            Return View(model)
        End Function
        Function DetailPrint(id As Integer) As ActionResult
            Dim model As New LeverancierModel()
            Dim cservice = ServiceFactory.GetCompanyService()
            Dim cresponse = cservice.GetCompanyByID(id)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Company = cresponse.Value
            End If
            Return View(model)

        End Function
        Function DetailExportToPdf(id As Integer) As ActionResult
            Dim model As New LeverancierModel()
            Dim cservice = ServiceFactory.GetCompanyService()
            Dim cresponse = cservice.GetCompanyByID(id)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Company = cresponse.Value
            End If
            Dim a = New ViewAsPdf()
            a.ViewName = "DetailPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Landscape
            a.PageSize = Options.Size.A4
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Dim ms As New MemoryStream(pdfBytes)
            Return New FileStreamResult(ms, "application/pdf")
        End Function

        '--------------SHARED---------------
        Function DeleteCompany(id As Integer, ByVal SearchName As String, ByVal SelectedActivities As String, ByVal SelectedProvinces As String) As ActionResult
            Dim Service = ServiceFactory.GetCompanyService()
            Dim Naam = Service.GetCompanyNameById(id)
            'verwijderen van bedrijf
            Dim ids As New List(Of Integer)
            ids.Add(id)
            Dim cResponse = Service.Delete(ids)
            If cResponse.Success Then
                AddMessage("info", "De leverancier " & Naam & " is verwijderd.", "Geslaagd!")
            Else
                AddMessage("error", "De leverancier " & Naam & " kon niet verwijderd worden, gelieve opnieuw te proberen of contact op te nemen met de administrator.", "Fout!")
            End If
            'zoeken terug opvragen na verwijderen
            Dim model As New LeverancierSearchModel
            model.Filter.CompanyName = SearchName

            If Not SelectedActivities = "" Then
                Dim lselectedactivities As List(Of String) = SelectedActivities.Split(",").ToList
                model.Filter.SelectedActivities = lselectedactivities.ConvertAll(Function(str) Int32.Parse(str))
            End If
            If Not SelectedProvinces = "" Then
                Dim lselectedprovinces As List(Of String) = SelectedProvinces.Split(",").ToList
                model.Filter.SelectedProvince = lselectedprovinces.ConvertAll(Function(str) Int32.Parse(str))
            End If
            If Not model.Filter.SelectedActivities Is Nothing Then
                ViewData("selectedactivities") = String.Join(",", model.Filter.SelectedActivities.ToArray())
            Else
                ViewData("selectedactivities") = ""
            End If
            If Not model.Filter.SelectedProvince Is Nothing Then
                ViewData("selectedprovinces") = String.Join(",", model.Filter.SelectedProvince.ToArray())
            Else
                ViewData("selectedprovinces") = ""
            End If
            FillInSearchSelectLists(model)
            Dim response = Service.GetCompanyBySearchfilter(model.Filter)
            If (response.Success) Then model.Companies = response.Values
            Return View("Zoeken", model)
        End Function
        <HttpPost>
        Function GetCountryIsoCode(countryid As Integer) As String
            Dim pservice = ServiceFactory.GetCountryService
            Dim presponse = pservice.GetCountryById(countryid)
            Dim iPostcode As New CountryBO
            If (presponse.Success) Then iPostcode = presponse.Values.FirstOrDefault
            Return iPostcode.ISOCode
        End Function
        <HttpPost>
        Function GetCompanyName(companyid As Integer) As String
            Dim pservice = ServiceFactory.GetCompanyService
            Dim presponse = pservice.GetCompanyNameById(companyid)
            Return presponse.FirstOrDefault.ToString()
        End Function
        <HttpPost>
        Function GetPostcodeDisplayName(postcodeId As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetPostalcodeService()
            Dim presponse = pservice.GetPostalcodeById(postcodeId)
            Dim iPostcode As New PostalCodeBO
            If (presponse.Success) Then iPostcode = presponse.Values.FirstOrDefault
            Dim singlePostalcode As New Select2DTO
            singlePostalcode.id = iPostcode.PostcodeId
            singlePostalcode.text = iPostcode.Postcode & " - " & iPostcode.Gemeente
            Return Json(singlePostalcode, JsonRequestBehavior.AllowGet)
        End Function
        <HttpPost>
        Function CheckCompany(Countryid As String, VAT As String) As JsonResult
            VAT = VAT.Replace(".", "")

            Dim workspace
            ServicePointManager.Expect100Continue = True
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12 Or SecurityProtocolType.Ssl3
            Dim request As WebRequest = WebRequest.Create("https://controleerbtwnummer.eu/api/validate/" & Countryid & VAT & ".json")
            request.Method = "GET"
            'ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Dim response As WebResponse = request.GetResponse()

            Dim inputstream1 As Stream = response.GetResponseStream()
            Dim reader As New StreamReader(inputstream1)
            workspace = reader.ReadToEnd
            inputstream1.Dispose()
            reader.Close()
            response.Close()

            'Dim valid As Boolean = JObject.Parse(workspace)("valid")



            Return Json(workspace, JsonRequestBehavior.AllowGet)
        End Function
        Private Sub FillInAddSelectLists(ByRef model As LeverancierModel)
            'get the activities
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()
            If (response.Success) Then model.ListActivities = response.Values
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID

        End Sub
        <HttpPost>
        Public Function GetPostcodesByCountry(term As String, CountryId As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetPostalcodeService()
            Dim presponse = pservice.GetPostalcodeByCountryAndSearchstring(CountryId, term)
            Dim iList As New List(Of PostalCodeBO)
            If (presponse.Success) Then iList = presponse.Values
            Dim PostalcodeList As New List(Of Select2DTO)()
            Dim singlePostalcode As Select2DTO
            For Each selectedPostalcode As PostalCodeBO In iList
                singlePostalcode = New Select2DTO()
                singlePostalcode.id = selectedPostalcode.PostcodeId
                singlePostalcode.text = selectedPostalcode.Postcode & " - " & selectedPostalcode.Gemeente
                PostalcodeList.Add(singlePostalcode)
            Next

            Return Json(PostalcodeList, JsonRequestBehavior.AllowGet)
        End Function
        <HttpPost>
        Public Function GetPostalCode(Postalcode As String, City As String, CountryCode As String) As JsonResult
            'Dim pservice = ServiceFactory.GetPostalcodeService()
            'Dim presponse = pservice.GetPostalcodeByCountryAndPostalcode(CountryCode, Postalcode)
            'Dim iList As New List(Of PostalCodeBO)
            'If (presponse.Success) Then iList = presponse.Values
            'Dim PostalcodeList As New List(Of Select2DTO)()
            'Dim singlePostalcode As Select2DTO
            'For Each selectedPostalcode As PostalCodeBO In iList
            '    singlePostalcode = New Select2DTO()
            '    singlePostalcode.id = selectedPostalcode.PostcodeId
            '    singlePostalcode.text = selectedPostalcode.Postcode & " - " & selectedPostalcode.Gemeente
            '    PostalcodeList.Add(singlePostalcode)
            'Next

            'Return Json(PostalcodeList, JsonRequestBehavior.AllowGet)
        End Function
        Public Function GetSalutationList() As List(Of SelectListItem)
            Dim list As New List(Of SelectListItem)
            Dim item1 As New SelectListItem
            item1.Text = "Dhr."
            item1.Value = "Dhr."
            Dim item2 As New SelectListItem
            item2.Text = "Mevr."
            item2.Value = "Mevr."
            Dim item3 As New SelectListItem
            item3.Text = "Familie"
            item3.Value = "Familie"
            list.Add(item1)
            list.Add(item2)
            list.Add(item3)
            Return list

        End Function

        Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
            TempData("Message") = message
            TempData("MessageType") = messagetype
            TempData("MessageTitle") = messagetitle
        End Sub
        Private Function SendRequest(uri As Uri, jsonDataBytes As Byte(), contentType As String, method As String) As String
            Dim response As String
            Dim request As WebRequest

            request = WebRequest.Create(uri)
            request.ContentLength = jsonDataBytes.Length
            request.ContentType = contentType
            request.Method = method

            Using requestStream = request.GetRequestStream
                requestStream.Write(jsonDataBytes, 0, jsonDataBytes.Length)
                requestStream.Close()

                Using responseStream = request.GetResponse.GetResponseStream
                    Using reader As New StreamReader(responseStream)
                        response = reader.ReadToEnd()
                    End Using
                End Using
            End Using

            Return response
        End Function


    End Class
End Namespace