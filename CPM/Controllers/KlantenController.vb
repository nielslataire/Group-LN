Imports System.Web.Mvc
Imports BO
Imports MvcSiteMapProvider
Imports MvcSiteMapProvider.Web.Mvc.Filters
Imports CPM.Controllers.Extensions
Imports Rotativa
Imports System.IO
Imports postal

Namespace Controllers
    <Authorize>
    Public Class KlantenController
        Inherits Controllers.ApplicationBaseController

        ' GET: Klanten
        Function Index() As ActionResult
            Return View()
        End Function


        '--------------DETAIL------------
        <SiteMapTitle("Client.DisplayName")>
        Function Detail(clientid As Integer, Optional projectid As Integer = 0) As ActionResult

            Dim model As New ClientModel()
            Dim cservice = ServiceFactory.GetClientService()
            Dim uservice = ServiceFactory.GetUnitService
            Dim pservice = ServiceFactory.GetProjectService
            'Get Client
            Dim cresponse = cservice.GetClientAccountById(clientid)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Client = cresponse.Value
            End If

            'Get Units for Client
            Dim response = uservice.GetGroupedUnitsByAccountId(clientid)
            model.UnitsGrouped = response.Values
            'Get Units with PaymentStages
            Dim invoicingresponse = uservice.GetClientUnitsWithStages(clientid)
            model.UnitsWithStages = invoicingresponse.Values
            'Get Invoices
            Dim invoiceresponse = pservice.GetInvoicesByUnitIds(model.UnitsWithStages.Select(Function(m) m.Unit.Id).ToList())
            model.Invoices = invoiceresponse.Values
            If projectid = 0 Then
                model.ProjectId = model.UnitsGrouped(0).Units(0).ProjectId
            Else
                model.ProjectId = projectid
            End If
            ''Get ProjectFolder
            model.Folder = pservice.GetProjectFolderById(model.ProjectId) & My.Settings.DeliveryDocLocalURL
            'Get Gifts for Client
            Dim response2 = cservice.GetClientGiftByAccountId(clientid)
            model.Gifts = response2.Values
            Dim response3 = cservice.GetClientPoaByAccountId(clientid)
            model.Poas = response3.Values
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode IsNot Nothing) Then
                node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
            End If
            If model.Client.ExecutionDays = 0 Then
                model.ExecutionDays = pservice.GetProjectExecutionDays(model.ProjectId)
            Else
                model.ExecutionDays = model.Client.ExecutionDays
            End If
            If Not model.Client.StartDateConstruction Is Nothing Then
                model.StartDate = model.Client.StartDateConstruction
            Else
                model.StartDate = pservice.GetProjectStartDateConstruction(model.ProjectId)
            End If
            model.WorkingDaysLeft = -9999
            If Not model.ExecutionDays AndAlso Not model.StartDate = DateTime.MinValue Then
                model.FinalConstructionDate = pservice.GetFinalConstructionDay(model.ProjectId, model.StartDate, model.ExecutionDays)
                If Not model.FinalConstructionDate = DateTime.MinValue Then
                    model.WorkingDaysLeft = pservice.GetWorkingDaysLeft(model.FinalConstructionDate, model.ProjectId)
                End If
            End If
            Dim response5 = pservice.GetLatestClientDocs(5, clientid)
            If (response5.Success) Then model.LatestDocs = response5.Values

            'Get ChangeOrders
            Dim response6 = pservice.GetClientChangeOrders(clientid)
            If (response6.Success) Then model.ChangeOrders = response6.Values


            Return View(model)
        End Function
        <HttpGet>
        Function SetDelivery(id As Integer, projectid As Integer) As ActionResult
            Dim viewModel = New DeliveryModel
            viewModel.ClientId = id
            Return PartialView("_ModalSetDelivery", viewModel)
        End Function
        <HttpPost>
        Function SetDelivery(model As DeliveryModel, file As HttpPostedFileBase) As ActionResult

            If (file Is Nothing OrElse file.ContentLength <= 0) Then
                ModelState.AddModelError("PdfUpload", "U moet een bestand kiezen")
            End If
            If ModelState.IsValid Then
                'Local Upload directory
                Dim service = ServiceFactory.GetClientService
                Dim pservice = ServiceFactory.GetProjectService
                Dim clientaccount As ClientAccountBO = service.GetClientAccountById(model.ClientId).Value


                Dim filename As String = DateTime.Now.ToString("yyyyMMddHHmmssfff") & Path.GetExtension(file.FileName)
                Dim Uploaddir = My.Settings.DocLocalURL
                'Uploadname per directory
                Dim imagepath = Path.Combine(Uploaddir, filename)
                'Check if directories exists
                CheckDir(Uploaddir)
                'save image to directories
                file.SaveAs(imagepath)
                model.DeliveryDoc = filename
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed"
                clientaccount.DeliveryDate = model.DeliveryDate
                clientaccount.DeliveryDoc = model.DeliveryDoc
                Dim response = service.InsertUpdate(clientaccount)
                'Projectdocument aanmaken
                Dim projectdoc As New ProjectDocBO
                projectdoc.ClientAccountId = clientaccount.Id
                projectdoc.ProjectId = model.ProjectId
                projectdoc.Filename = filename
                projectdoc.Type = ProjectDocType.Delivery
                projectdoc.SortOrder = 0
                projectdoc.DocDate = model.DeliveryDate
                If response.Success = True Then
                    Dim response2 = pservice.InsertUpdateProjectDoc(projectdoc)
                    If response2.Success = True Then
                        Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .clientid = model.ClientId})
                        AddMessage("succes", "Het opleveringsverslag is toegevoegd / bijgewerkt", "Gelukt!")
                    Else
                        AddMessage("error", "Het opleveringsverslag is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                        Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .clientid = model.ClientId})
                    End If


                Else
                    AddMessage("error", "Het opleveringsverslag is NIET toegevoegd / bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .clientid = model.ClientId})

                End If
            End If
            Return RedirectToAction("Detail", New With {.projectid = model.ProjectId, .clientid = model.ClientId})
        End Function

        'KLANT TOEVOEGEN
        <HttpGet>
        Public Function AddClientAccount(id As Integer) As ActionResult

            Dim model As New AddClientAccountModel
            Dim service = ServiceFactory.GetProjectService
            model.ProjectName = service.GetProjectNameById(id)
            model.ProjectId = id
            model.ClientAccount.OwnerPercentage = 100
            model.ClientAccount.OwnerType.Id = 1
            FillInAddSelectLists(model)
            Return View(model)

        End Function
        <HttpPostAttribute>
        <ValidateInput(False)>
        Public Function AddClientAccount(model As AddClientAccountModel, contacts As List(Of ClientContactBO), coowners As List(Of ClientContactBO), units As List(Of UnitBO)) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next
            If units Is Nothing Then
                ModelState.AddModelError("CustomError", "U dient minstens één eenheid te kiezen voor deze klant")
            End If


            If (ModelState.IsValid) Then
                'Fill in clientaccount
                model.ClientAccount.Postalcode.PostcodeId = model.SelectedPostalcode
                model.ClientAccount.InvoicePostalcode.PostcodeId = model.SelectedInvoicePostalcode
                model.ClientAccount.CoOwners = coowners
                model.ClientAccount.Contacts = contacts
                Dim Service = ServiceFactory.GetClientService
                Dim response = Service.InsertUpdate(model.ClientAccount)
                If response.Success Then
                    AddMessage("success", "De klantenaccount " & model.ClientAccount.Name & " is toegevoegd", "Geslaagd!")
                    model.ClientAccount.Id = response.Messages(0).Message
                    'Add units to clientaccount
                    Dim unitService = ServiceFactory.GetUnitService
                    For Each item In units
                        Dim unitResponse = unitService.GetUnitById(item.Id)
                        If unitResponse.Success Then
                            Dim bo As UnitBO = unitResponse.Value
                            bo.ClientAccountId = model.ClientAccount.Id
                            bo.ConstructionValueSold = item.ConstructionValueSold
                            bo.LandValueSold = item.LandValueSold
                            Dim response2 = unitService.InsertUpdateUnit(bo)
                            If response2.Success Then
                                '---Update Constructionvalues for Unit ---
                                For Each coitem In item.ConstructionValues
                                    Dim coresponse = unitService.GetConstructionValue(coitem.Id)
                                    If coresponse.Success Then
                                        Dim covalue As UnitConstructionValueBO = coresponse.Value
                                        covalue.ValueSold = coitem.ValueSold
                                        Dim response3 = unitService.InsertUpdateConstructionValue(covalue)
                                        If response3.Success Then
                                            AddMessage("success", "De verkochte bouwwaardes van " & item.Name & " zijn geupdated", "Geslaagd!")
                                        Else
                                            AddMessage("error", "De verkochte bouwwaardes van " & item.Name & " zijn NIET geupdated", "Fout!")
                                        End If
                                    End If
                                Next
                                '-----------------------------------------
                                AddMessage("success", "De eenheid " & item.Name & " is toegevoegd aan de klantenaccount " & model.ClientAccount.Name, "Geslaagd!")
                            Else
                                AddMessage("error", "De eenheid " & item.Name & " is NIET toegevoegd aan de klantenaccount " & model.ClientAccount.Name, "Fout!")
                            End If
                        End If
                    Next
                    For Each contact In model.ClientAccount.Contacts
                        If Not contact.Email Is Nothing Then
                            Dim email As Object = New Email("WelcomeMail")
                            email.[To] = contact.Email
                            email.[From] = "info@groupln.be"
                            email.Send()
                        End If
                    Next
                    Return RedirectToAction("DetailClients", "Projecten", New With {.projectid = model.ProjectId})
                Else
                    AddMessage("error", "De klantenaccount " & model.ClientAccount.Name & " is NIET toegevoegd", "Fout!")
                    Return View(model)
                End If
            Else
                FillInAddSelectLists(model)
                Return View(model)
            End If
        End Function
        <HttpPost>
        Public Function AddCoOwner(Name As String, Forename As String, Salutation As String, Street As String, Housenumber As String, Busnumber As String, Zipcode As Integer, Phone As String, Cellphone As String, Email As String, OwnerType As Integer, OwnerPercentage As String, VatNumber As String, CompanyName As String, InvoiceAddress As String, InvoiceStreet As String, InvoiceHousenumber As String, InvoiceBusnumber As String, InvoiceZipcode As String) As PartialViewResult
            Dim nCoOwner As New ClientContactBO
            'ophalen postcode
            'Dim pservice = ServiceFactory.GetPostalcodeService()
            'Dim presponse = pservice.GetPostalcodeById(Zipcode)
            'If (presponse.Success) Then nCoOwner.Postalcode = presponse.Values.FirstOrDefault
            nCoOwner.Name = Name
            nCoOwner.Firstname = Forename
            nCoOwner.Salutation = Salutation
            nCoOwner.Street = Street
            nCoOwner.Housenumber = Housenumber
            nCoOwner.Busnumber = Busnumber
            nCoOwner.Postalcode.PostcodeId = Zipcode
            nCoOwner.VATnumber = VatNumber
            nCoOwner.CompanyName = CompanyName
            nCoOwner.InvoiceStreet = InvoiceStreet
            nCoOwner.InvoiceHousenumber = InvoiceHousenumber
            nCoOwner.InvoiceBusnumber = InvoiceBusnumber
            nCoOwner.InvoicePostalcode.PostcodeId = InvoiceZipcode
            If Not Phone Is Nothing Then nCoOwner.Phone = Regex.Replace(Phone, "[^0-9]", "")
            If Not Cellphone Is Nothing Then nCoOwner.Cellphone = Regex.Replace(Cellphone, "[^0-9]", "")
            nCoOwner.Email = Email
            Dim sservice = ServiceFactory.GetClientService
            Dim sresponse = sservice.GetClientOwnerTypeById(OwnerType)
            nCoOwner.CoOwnerType = sresponse.Value
            Try
                nCoOwner.CoOwnerPercentage = Decimal.Parse(OwnerPercentage)

            Catch ex As Exception
                Try
                    OwnerPercentage = OwnerPercentage.Replace(".", ",")
                    nCoOwner.CoOwnerPercentage = Decimal.Parse(OwnerPercentage)
                Catch ex2 As Exception

                End Try
            End Try
            nCoOwner.CoOwnerPercentage = Decimal.Parse(OwnerPercentage)
            ViewData("mode") = "add"
            Return PartialView("_CoOwnerRow", nCoOwner)

        End Function
        <HttpPost>
        Public Function AddSelectedUnits(UnitId As String, UnitName As String, UnitGroup As String) As PartialViewResult
            Dim nUnit As New UnitBO
            'nUnit.Id = UnitId
            'nUnit.Name = UnitName
            Dim service = ServiceFactory.GetUnitService
            Dim unitresponse = service.GetUnitById(UnitId)
            If unitresponse.Success Then nUnit = unitresponse.Value
            nUnit.LandValueSold = nUnit.LandValue
            For Each item In nUnit.ConstructionValues
                item.ValueSold = item.Value
            Next
            'Dim response = service.GetConstructionValues(UnitId)
            'If response.Success Then nUnit.ConstructionValues = response.Values
            'nActivity.Group.Name = ActGroup
            ViewData("mode") = "add"
            Return PartialView("_UnitRow", nUnit)

        End Function
        Private Sub FillInAddSelectLists(ByRef model As AddClientAccountModel)
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then model.Countries = cresponse.Values
            Dim defCountry = model.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If model.SelectedCountry = 0 Then
                If (defCountry IsNot Nothing) Then model.SelectedCountry = defCountry.ID
            End If
            Dim oservice = ServiceFactory.GetClientService()
            Dim oresponse = oservice.GetOwnerTypesForSelect()
            If (oresponse.Success) Then model.OwnerTypes = oresponse.Values
            Dim uservice = ServiceFactory.GetUnitService()
            Dim uresponse = uservice.GetAvailableUnitsByProjectId(model.ProjectId)
            If (uresponse.Success) Then model.AvailableUnits = uresponse.Values


        End Sub
        Public Function BlankContactRow() As PartialViewResult
            Return PartialView("_ContactRow", New ClientContactBO())
        End Function
        'KLANT BEWERKEN
        Function Edit(projectid As Integer, clientid As Integer, activetab As Integer) As ActionResult
            Dim model As New EditClientModel()

            If Not clientid = 0 Then
                Dim cservice = ServiceFactory.GetClientService()
                Dim uservice = ServiceFactory.GetUnitService
                'Get Client
                Dim cresponse = cservice.GetClientAccountById(clientid)
                If (cresponse.Success) Then
                    cresponse.Value = cresponse.Values.FirstOrDefault
                    model.Client = cresponse.Value
                    model.SelectedPostalcode.CountryId = model.Client.Postalcode.Country.CountryID
                    model.SelectedPostalcode.PostalCodeId = model.Client.Postalcode.PostcodeId
                    If Not model.Client.InvoicePostalcode.PostcodeId = 0 Then
                        model.SelectedInvoicePostalcode.CountryId = model.Client.InvoicePostalcode.Country.CountryID
                        model.SelectedInvoicePostalcode.PostalCodeId = model.Client.InvoicePostalcode.PostcodeId
                    End If

                    Dim s As String = model.Client.Postalcode.Postcode & " - " & model.Client.Postalcode.Gemeente
                    ViewData("PostcodeDisplayName") = s
                    ViewData("activetab") = activetab
                End If
                Dim title As String = "Klant bewerken"
                If model.Client.CompanyName Is Nothing Then
                    title = title & " - " & model.Client.Salutation.GetDisplayName() & " " & model.Client.DisplayName
                Else
                    title = title & " - " & model.Client.DisplayName
                End If

                ViewData("Title") = title

                'Get Units for Client
                Dim response = uservice.GetUnitsByAccountId(clientid)
                model.Units = response.Values.OrderBy(Function(m) m.Type.GroupId).OrderBy(Function(m) m.Type.Id).ToList

                'Get Gifts for Client
                Dim response2 = cservice.GetClientGiftByAccountId(clientid)
                model.Gifts = response2.Values

                'Get Poa for Client
                Dim response3 = cservice.GetClientPoaByAccountId(clientid)
                model.Poas = response3.Values
            End If
            FillInAddSelectListsEdit(model)
            model.ProjectId = projectid
            Dim node = SiteMaps.Current.CurrentNode
            If (node IsNot Nothing And node.ParentNode IsNot Nothing) Then
                node.ParentNode.Title = model.Client.DisplayName
                If (node.ParentNode.ParentNode IsNot Nothing AndAlso node.ParentNode.ParentNode.ParentNode IsNot Nothing) Then
                    node.ParentNode.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService.GetProjectNameById(model.ProjectId)
                End If
            End If

                Return View(model)
        End Function

        <HttpPost>
        Function Edit(viewmodel As EditClientModel) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            For Each key In ModelState.Keys
                If ModelState(key).Errors.Count > 0 Then
                    errors(key) = ModelState(key).Errors()
                End If
            Next

            If (Not ModelState.IsValid) Then Return View(viewmodel)
            If (viewmodel.Client.Id = 0) Then Return View(viewmodel)
            If (ModelState.IsValid) Then
                viewmodel.Client.Postalcode.PostcodeId = viewmodel.SelectedPostalcode.PostalCodeId
                viewmodel.Client.InvoicePostalcode.PostcodeId = viewmodel.SelectedInvoicePostalcode.PostalCodeId
                Dim Service = ServiceFactory.GetClientService
                If viewmodel.IsCompany = True Then
                    viewmodel.Client.Name = Nothing
                    viewmodel.Client.Salutation = 0
                Else
                    viewmodel.Client.CompanyName = Nothing
                    viewmodel.Client.VATnumber = Nothing
                End If
                Dim response = Service.InsertUpdate(viewmodel.Client)
                If response.Success = True Then
                    AddMessage("success", "Account " & viewmodel.Client.DisplayName & " is bijgewerkt", "Geslaagd!")
                    Return RedirectToAction("Edit", New With {Key .projectid = viewmodel.ProjectId, .clientid = viewmodel.Client.Id, .activetab = 0})
                Else

                    AddMessage("error", "Contact " & viewmodel.Client.DisplayName & " is niet bijgewerkt", "Fout!")
                    FillInAddSelectListsEdit(viewmodel)
                    Return View(viewmodel)

                End If
            Else
                FillInAddSelectListsEdit(viewmodel)
                Return View(viewmodel)
            End If

        End Function

        Private Sub FillInAddSelectListsEdit(ByRef model As EditClientModel)

            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then
                model.SelectedPostalcode.Countries = cresponse.Values
                model.SelectedInvoicePostalcode.Countries = cresponse.Values
            End If
            Dim defCountry = model.SelectedPostalcode.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then model.SelectedPostalcode.CountryId = defCountry.ID
            Dim oservice = ServiceFactory.GetClientService()
            Dim oresponse = oservice.GetOwnerTypesForSelect()
            If (oresponse.Success) Then model.OwnerTypes = oresponse.Values

        End Sub
        '----------CONTACTEN-----------
        <HttpGet>
        Function AddContactModal(id As Integer) As ActionResult
            Dim viewModel = New ClientContactBO
            viewModel.AccountId = id
            Return PartialView("_AddUpdateContactModal", viewModel)
        End Function
        <HttpGet>
        Function EditContactModal(id As Integer) As ActionResult
            Dim viewModel = New ClientContactBO
            If Not id = 0 Then
                Dim cservice = ServiceFactory.GetClientService()
                Dim response = cservice.GetClientContactById(id)
                If response.Success Then viewModel = response.Value
                Return PartialView("_AddUpdateContactModal", viewModel)
            End If
        End Function
        <HttpPost>
        Function AddUpdateContact(viewmodel As ClientContactBO) As ActionResult
            If ModelState.IsValid Then
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.InsertUpdateClientContact(viewmodel)
                If response.Success = True Then
                    If viewmodel.Id = 0 Then
                        AddMessage("success", viewmodel.Name & " " & viewmodel.Firstname & " is toegevoegd als contact", "Geslaagd!")
                    Else
                        AddMessage("success", "Contact " & viewmodel.Name & " " & viewmodel.Firstname & " is bijgewerkt", "Geslaagd!")

                    End If

                Else
                    If viewmodel.Id = 0 Then
                        AddMessage("error", viewmodel.Name & " " & viewmodel.Firstname & " is niet toegevoegd als contact", "Fout!")
                    Else
                        AddMessage("error", "Contact " & viewmodel.Name & " " & viewmodel.Firstname & " is niet bijgewerkt", "Fout!")

                    End If
                End If
                Return Redirect(Request.UrlReferrer.ToString())
                'Return Json(New With {.success = True, .url = Url.Action("Edit", New With {Key .clientid = viewmodel.AccountId, .activetab = 0})})
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function DeleteContactModal(id As Integer) As ActionResult
            Dim viewModel = New ClientContactBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                viewModel = dservice.GetClientContactById(id).Value
            End If
            Return PartialView("_DeleteContactModal", viewModel)
        End Function
        <HttpGet>
        Function DeleteContact(id As Integer, accountid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            Idlist.Add(id)
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.DeleteClientContact(Idlist)
                If response.Success = True Then
                    AddMessage("", "Het contact is verwijderd", "Geslaagd!")
                    Return Redirect(Request.UrlReferrer.ToString())
                Else
                    AddMessage("error", "Het contact is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If

            End If

        End Function

        '---------MEDE-EIGENAARS-------------
        <HttpGet>
        Function AddCoOwnerModal(id As Integer) As ActionResult
            Dim viewModel = New AddUpdateClientCoOwnerModel
            viewModel.CoOwner.AccountId = id
            viewModel.CoOwner.IsCoOwner = True
            Dim cservice = ServiceFactory.GetCountryService()
            Dim cresponse = cservice.GetVisibleCountriesForSelect()
            If (cresponse.Success) Then viewModel.SelectedCoOwnerPostalCode.Countries = cresponse.Values
            Dim defCountry = viewModel.SelectedCoOwnerPostalCode.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
            If (defCountry IsNot Nothing) Then viewModel.SelectedCoOwnerPostalCode.CountryId = defCountry.ID
            Dim oservice = ServiceFactory.GetClientService()
            Dim oresponse = oservice.GetOwnerTypesForSelect()
            If (oresponse.Success) Then viewModel.OwnerTypes = oresponse.Values
            viewModel.MaxCoOwnerPercentage = oservice.GetMaxOwnerPercentage(id, 0).Value
            Return PartialView("_AddUpdateCoOwnerModal", viewModel)
        End Function
        <HttpGet>
        Function EditCoOwnerModal(id As Integer) As ActionResult
            If Not id = 0 Then
                Dim viewModel = New AddUpdateClientCoOwnerModel
                Dim service = ServiceFactory.GetClientService()
                Dim response = service.GetClientContactById(id)
                If response.Success = True Then viewModel.CoOwner = response.Value
                Dim cservice = ServiceFactory.GetCountryService()
                Dim cresponse = cservice.GetVisibleCountriesForSelect()
                If (cresponse.Success) Then
                    viewModel.SelectedCoOwnerPostalCode.Countries = cresponse.Values
                    viewModel.SelectedCoOwnerInvoicePostalCode.Countries = cresponse.Values
                End If

                If Not viewModel.CoOwner.Postalcode.PostcodeId = 0 Then viewModel.SelectedCoOwnerPostalCode.PostalCodeId = viewModel.CoOwner.Postalcode.PostcodeId
                If Not viewModel.CoOwner.Postalcode.Country.CountryID = 0 Then

                    viewModel.SelectedCoOwnerPostalCode.CountryId = viewModel.CoOwner.Postalcode.Country.CountryID
                Else
                    Dim defCountry = viewModel.SelectedCoOwnerPostalCode.Countries.Where(Function(m) m.Group = "19").FirstOrDefault()
                    If (defCountry IsNot Nothing) Then viewModel.SelectedCoOwnerPostalCode.CountryId = defCountry.ID
                End If

                If Not viewModel.CoOwner.InvoicePostalcode.PostcodeId = 0 Then viewModel.SelectedCoOwnerInvoicePostalCode.PostalCodeId = viewModel.CoOwner.InvoicePostalcode.PostcodeId
                If Not viewModel.CoOwner.InvoicePostalcode.Country.CountryID = 0 Then
                    viewModel.SelectedCoOwnerInvoicePostalCode.CountryId = viewModel.CoOwner.InvoicePostalcode.Country.CountryID
                End If

                If Not viewModel.CoOwner.CoOwnerType.Id = 0 Then viewModel.SelectedCoOwnerType = viewModel.CoOwner.CoOwnerType.Id
                Dim oresponse = service.GetOwnerTypesForSelect()
                If (oresponse.Success) Then viewModel.OwnerTypes = oresponse.Values
                viewModel.MaxCoOwnerPercentage = service.GetMaxOwnerPercentage(viewModel.CoOwner.AccountId, id).Value

                Return PartialView("_AddUpdateCoOwnerModal", viewModel)
            Else

            End If
        End Function
        <HttpPostAttribute>
        Function AddUpdateCoOwner(viewmodel As AddUpdateClientCoOwnerModel) As ActionResult

            If viewmodel.IsCompany = True Then
                viewmodel.CoOwner.Name = Nothing
                viewmodel.CoOwner.Firstname = Nothing
                viewmodel.CoOwner.Salutation = Nothing
            Else
                viewmodel.CoOwner.CompanyName = Nothing
                viewmodel.CoOwner.VATnumber = Nothing
            End If
            If (Not ModelState.IsValid) Then Return View(viewmodel)
            If ModelState.IsValid Then
                If Not viewmodel.SelectedCoOwnerPostalCode.PostalCodeId = 0 Then viewmodel.CoOwner.Postalcode.PostcodeId = viewmodel.SelectedCoOwnerPostalCode.PostalCodeId
                If Not viewmodel.SelectedCoOwnerPostalCode.CountryId = 0 Then viewmodel.CoOwner.Postalcode.Country.CountryID = viewmodel.SelectedCoOwnerPostalCode.CountryId
                If Not viewmodel.SelectedCoOwnerInvoicePostalCode.PostalCodeId = 0 Then viewmodel.CoOwner.InvoicePostalcode.PostcodeId = viewmodel.SelectedCoOwnerInvoicePostalCode.PostalCodeId
                If Not viewmodel.SelectedCoOwnerInvoicePostalCode.CountryId = 0 Then viewmodel.CoOwner.InvoicePostalcode.Country.CountryID = viewmodel.SelectedCoOwnerInvoicePostalCode.CountryId

                viewmodel.CoOwner.CoOwnerType.Id = viewmodel.SelectedCoOwnerType
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.InsertUpdateClientContact(viewmodel.CoOwner)
                If response.Success = True Then
                    If viewmodel.CoOwner.Id = 0 Then
                        AddMessage("success", viewmodel.CoOwner.Name & " " & viewmodel.CoOwner.Firstname & " is toegevoegd als mede-eigenaar", "Geslaagd!")
                    Else
                        AddMessage("success", "Mede-eigenaar " & viewmodel.CoOwner.Name & " " & viewmodel.CoOwner.Firstname & " is bijgewerkt", "Geslaagd!")

                    End If

                Else
                    If viewmodel.CoOwner.Id = 0 Then
                        AddMessage("error", viewmodel.CoOwner.Name & " " & viewmodel.CoOwner.Firstname & " is niet toegevoegd als mede-eigenaar", "Fout!")
                    Else
                        AddMessage("error", "Mede-eigenaar " & viewmodel.CoOwner.Name & " " & viewmodel.CoOwner.Firstname & " is niet bijgewerkt", "Fout!")

                    End If
                End If
                Return Redirect(Request.UrlReferrer.ToString())

            Else
                Return View(viewmodel)
            End If
        End Function
        Function DeleteCoOwnerModal(id As Integer) As ActionResult
            Dim viewModel = New ClientContactBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                viewModel = dservice.GetClientContactById(id).Value
            End If
            Return PartialView("_DeleteCoOwnerModal", viewModel)
        End Function
        <HttpGet>
        Function DeleteCoOwner(id As Integer, accountid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            Idlist.Add(id)
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.DeleteClientContact(Idlist)
                If response.Success = True Then
                    AddMessage("", "De mede-eigenaar is verwijderd", "Geslaagd!")
                    Return Redirect(Request.UrlReferrer.ToString())
                Else
                    AddMessage("error", "De mede-eigenaar is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If
            End If
        End Function
        '--------EENHEDEN--------------
        <HttpGet>
        Function AddUnitModal(id As Integer) As ActionResult
            Dim viewModel = New AddUnitToClientModel
            viewModel.Unit.ClientAccountId = id
            Dim pservice = ServiceFactory.GetProjectService
            Dim presponse = pservice.GetProjectsWithAvailableUnits()
            If (presponse.Success) Then viewModel.AvailableProjects = presponse.Values
            If Not viewModel.AvailableProjects.Count = 0 Then
                Dim uservice = ServiceFactory.GetUnitService
                Dim uresponse = uservice.GetAvailableUnitsByProjectId(viewModel.AvailableProjects.Item(0).ID)
                If (uresponse.Success) Then viewModel.AvailableUnits = uresponse.Values
            End If
            'Dim uresponse = uservice.GetAvailableUnitsByProjectId(projectid)
            'If (uresponse.Success) Then viewModel.AvailableUnits = uresponse.Values
            Return PartialView("_AddUnitModal", viewModel)
        End Function
        <HttpPost>
        Function AddUnit(viewmodel As AddUnitToClientModel) As ActionResult
            If ModelState.IsValid Then
                viewmodel.Unit.Id = viewmodel.SelectedUnit
                Dim dservice = ServiceFactory.GetUnitService
                Dim response = dservice.InsertUpdateUnitToClientAccount(viewmodel.Unit)
                If response.Success = True Then
                    AddMessage("success", "De eenheid is toegevoegd aan de klant", "Geslaagd!")
                Else

                    AddMessage("error", "De eenheid is niet toegevoegd aan de klant probeer later opnieuw", "Fout!")

                End If
                Return Redirect(Request.UrlReferrer.ToString())
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function EditUnitModal(id As Integer) As ActionResult
            If Not id = 0 Then
                Dim viewModel = New UnitBO
                Dim service = ServiceFactory.GetUnitService()
                Dim response = service.GetUnitById(id)
                If response.Success = True Then viewModel = response.Value
                Return PartialView("_UpdateUnitModal", viewModel)
            Else

            End If
        End Function
        <HttpPostAttribute>
        Function UpdateUnit(viewmodel As UnitBO) As ActionResult
            If ModelState.IsValid And Not viewmodel.Id = 0 Then
                Dim dservice = ServiceFactory.GetUnitService
                Dim response = dservice.InsertUpdateUnitToClientAccount(viewmodel)
                If response.Success = True Then
                    AddMessage("success", "De eenheid is bijgewerkt bij de klant", "Geslaagd!")
                Else
                    AddMessage("error", "De eenheid is niet bijgewerkt bij de klant, probeer later opnieuw", "Fout!")
                End If
                Return Redirect(Request.UrlReferrer.ToString())
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        Function DeleteUnitModal(id As Integer) As ActionResult
            Dim viewModel = New UnitBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetUnitService
                viewModel = dservice.GetUnitById(id).Value
            End If
            Return PartialView("_DeleteUnitModal", viewModel)
        End Function
        <HttpGet>
        Function DeleteUnit(id As Integer, accountid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            If Not id = 0 Then
                Idlist.Add(id)
            End If
            If Not Idlist.Count = 0 Then
                Dim dservice = ServiceFactory.GetUnitService
                Dim response = dservice.DeleteUnitFromClientAccountByUnitId(Idlist)
                If response.Success = True Then
                    AddMessage("", "De eenheid is verwijderd van de klant", "Geslaagd!")
                    Return Redirect(Request.UrlReferrer.ToString())
                Else
                    AddMessage("error", "De eenheid is niet verwijderd van de klant, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If

            End If

        End Function
        '--------TOEGIFTEN--------------
        <HttpGet>
        Function AddGiftModal(id As Integer) As ActionResult
            Dim viewModel = New AddGiftToClientModel
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()
            If (response.Success) Then viewModel.ListActivities = response.Values
            viewModel.Gift.AccountId = id
            Return PartialView("_AddGiftModal", viewModel)
        End Function
        <HttpPost>
        Function AddUpdateGift(viewmodel As AddGiftToClientModel) As ActionResult
            If ModelState.IsValid Then
                Dim dservice = ServiceFactory.GetClientService
                Dim aservice = ServiceFactory.GetActivityService
                viewmodel.Gift.Activities.Clear()
                For Each i In viewmodel.SelectedActivities
                    Dim act As ActivityBO = aservice.GetActivitybyId(i).Value
                    viewmodel.Gift.Activities.Add(act)
                Next
                Dim response = dservice.InsertUpdateClientGift(viewmodel.Gift)

                If response.Success = True Then
                    AddMessage("success", "De toegift is toegevoegd/bewerkt bij de klant", "Geslaagd!")
                Else

                    AddMessage("error", "De toegift is niet toegevoegd/bewerkt bij de klant, probeer later opnieuw", "Fout!")

                End If
                Return Redirect(Request.UrlReferrer.ToString())
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function EditGiftModal(id As Integer) As ActionResult

            If Not id = 0 Then
                Dim viewModel = New AddGiftToClientModel
                Dim cservice = ServiceFactory.GetClientService()
                Dim cresponse = cservice.GetClientGiftById(id)
                If (cresponse.Success) Then viewModel.Gift = cresponse.Value
                For Each act In viewModel.Gift.Activities
                    viewModel.SelectedActivities.Add(act.ID)
                Next
                Dim service = ServiceFactory.GetActivityService()
                Dim response = service.GetActivitiesForSelect()
                If (response.Success) Then viewModel.ListActivities = response.Values
                Return PartialView("_AddGiftModal", viewModel)
            Else

            End If
        End Function

        Function DeleteGiftModal(id As Integer) As ActionResult
            Dim viewModel = New ClientGiftBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                viewModel = dservice.GetClientGiftById(id).Value
            End If
            Return PartialView("_DeleteGiftModal", viewModel)
        End Function
        <HttpGet>
        Function DeleteGift(id As Integer, accountid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            If Not id = 0 Then
                Idlist.Add(id)
            End If
            If Not Idlist.Count = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.DeleteClientGift(Idlist)
                If response.Success = True Then
                    AddMessage("", "De toegift is verwijderd van de klant", "Geslaagd!")
                    Return Redirect(Request.UrlReferrer.ToString())
                Else
                    AddMessage("error", "De toegift is niet verwijderd van de klant, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If

            End If

        End Function
        '--------AANDACHTSPUNTEN--------------
        <HttpGet>
        Function AddPoaModal(id As Integer) As ActionResult
            Dim viewModel = New AddPoaToClientModel
            Dim service = ServiceFactory.GetActivityService()
            Dim response = service.GetActivitiesForSelect()
            If (response.Success) Then viewModel.ListActivities = response.Values
            viewModel.POA.AccountId = id
            Return PartialView("_AddPoaModal", viewModel)
        End Function
        <HttpPost>
        Function AddUpdatePoa(viewmodel As AddPoaToClientModel) As ActionResult
            If ModelState.IsValid Then
                Dim dservice = ServiceFactory.GetClientService
                Dim aservice = ServiceFactory.GetActivityService
                viewmodel.POA.Activities.Clear()
                For Each i In viewmodel.SelectedActivities
                    Dim act As ActivityBO = aservice.GetActivitybyId(i).Value
                    viewmodel.POA.Activities.Add(act)
                Next
                Dim response = dservice.InsertUpdateClientPoa(viewmodel.POA)

                If response.Success = True Then
                    AddMessage("success", "Het aandachtspunt is toegevoegd/bewerkt bij de klant", "Geslaagd!")
                Else

                    AddMessage("error", "Het aandachtspunt is niet toegevoegd/bewerkt bij de klant, probeer later opnieuw", "Fout!")

                End If
                Return Redirect(Request.UrlReferrer.ToString())
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        <HttpGet>
        Function EditPoaModal(id As Integer) As ActionResult

            If Not id = 0 Then
                Dim viewModel = New AddPoaToClientModel
                Dim cservice = ServiceFactory.GetClientService()
                Dim cresponse = cservice.GetClientPoaById(id)
                If (cresponse.Success) Then viewModel.POA = cresponse.Value
                For Each act In viewModel.POA.Activities
                    viewModel.SelectedActivities.Add(act.ID)
                Next
                Dim service = ServiceFactory.GetActivityService()
                Dim response = service.GetActivitiesForSelect()
                If (response.Success) Then viewModel.ListActivities = response.Values
                Return PartialView("_AddPoaModal", viewModel)
            Else

            End If
        End Function

        Function DeletePoaModal(id As Integer) As ActionResult
            Dim viewModel = New ClientPoaBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                viewModel = dservice.GetClientPoaById(id).Value
            End If
            Return PartialView("_DeletePoaModal", viewModel)
        End Function
        <HttpGet>
        Function DeletePoa(id As Integer, accountid As Integer) As ActionResult
            Dim Idlist As New List(Of Integer)
            If Not id = 0 Then
                Idlist.Add(id)
            End If
            If Not Idlist.Count = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                Dim response = dservice.DeleteClientPoa(Idlist)
                If response.Success = True Then
                    AddMessage("", "Het aandachtspunt is verwijderd van de klant", "Geslaagd!")
                    Return Redirect(Request.UrlReferrer.ToString())
                Else
                    AddMessage("error", "Het aandachtspunt is niet verwijderd van de klant, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If

            End If

        End Function
        'KLANT VERWIJDEREN
        Function DeleteClientModal(id As Integer) As ActionResult
            Dim viewModel = New IdNameBO
            If Not id = 0 Then
                Dim dservice = ServiceFactory.GetClientService
                viewModel.Display = dservice.GetClientAccountNameById(id)
                viewModel.ID = id
            End If
            Return PartialView("_DeleteClientModal", viewModel)
        End Function
        <HttpGet>
        Function DeleteClient(id As Integer) As ActionResult
            Dim stri As String = Request.UrlReferrer.ToString()
            Dim Idlist As New List(Of Integer)
            Idlist.Add(id)
            If Not id = 0 Then
                Dim uservice = ServiceFactory.GetUnitService
                Dim response = uservice.DeleteUnitFromClientAccountByAccountId(Idlist)
                Dim dservice = ServiceFactory.GetClientService
                If response.Success = True Then
                    response = dservice.Delete(Idlist)
                    If response.Success = True Then
                        AddMessage("", "De klant is verwijderd", "Geslaagd!")
                        Return Redirect(Request.UrlReferrer.ToString())
                    Else
                        AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                        Return Redirect(Request.UrlReferrer.ToString())
                    End If
                Else
                    AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!")
                    Return Redirect(Request.UrlReferrer.ToString())
                End If
            Else
                Return Redirect(Request.UrlReferrer.ToString())
            End If
        End Function
        'DOCS
        <HttpGet>
        Function DetailDocs(projectid As Integer, clientid As Integer) As ActionResult

            Dim model As New DetailDocsModel
            Dim service = ServiceFactory.GetProjectService
            Dim cservice = ServiceFactory.GetClientService
            Dim response = service.GetClientDocs(clientid)
            If (response.Success) Then model.Docs = response.Values
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            model.ClientName = cservice.GetClientAccountNameById(clientid)
            SiteMaps.Current.CurrentNode.ParentNode.ParentNode.ParentNode.Title = model.ProjectName
            SiteMaps.Current.CurrentNode.ParentNode.Title = model.ClientName
            Return View(model)
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
                    Return RedirectToAction("Detail", "Klanten", New With {.projectid = viewModel.ProjectId, .clientid = viewModel.ClientAccountId})
                Else
                    AddMessage("error", "Het document is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
                    Return RedirectToAction("Detail", "Klanteen", New With {.projectid = viewModel.ProjectId, .clientid = viewModel.ClientAccountId})
                End If

            End If
            AddMessage("error", "Het document is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!")
            Return RedirectToAction("Detail", "Klanten", New With {.projectid = viewModel.ProjectId, .clientid = viewModel.ClientAccountId})
        End Function
        'INVOICING
        <HttpGet>
        Function DetailInvoicing(projectid As Integer, clientid As Integer) As ActionResult
            'UpdateInvoices()
            Dim model As New DetailInvoicingModel
            Dim service = ServiceFactory.GetProjectService
            Dim cservice = ServiceFactory.GetClientService
            Dim cresponse = cservice.GetClientAccountById(clientid)
            If (cresponse.Success) Then model.Client = cresponse.Value
            Dim iservice = ServiceFactory.GetInvoicingService
            Dim response = iservice.GetClientInvoices(clientid, ClientType.Klant)
            If (response.Success) Then model.Invoices = response.Values
            For Each coowner In model.Client.CoOwners
                Dim coresponse = iservice.GetClientInvoices(coowner.Id, ClientType.Medeeigenaar)
                If (coresponse.Success) Then
                    For Each inv In coresponse.Values
                        model.Invoices.Add(inv)
                    Next
                End If
            Next
            model.ProjectId = projectid
            model.ProjectName = service.GetProjectNameById(projectid)
            model.ClientName = cservice.GetClientAccountNameById(clientid)
            SiteMaps.Current.CurrentNode.ParentNode.ParentNode.ParentNode.Title = model.ProjectName
            SiteMaps.Current.CurrentNode.ParentNode.Title = model.ClientName
            Return View(model)
        End Function
        'SHARED
        Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
            TempData("Message") = message
            TempData("MessageType") = messagetype
            TempData("MessageTitle") = messagetitle
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
        Public Sub CheckDir(path As String)
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
        End Sub
        <HttpPost>
        Function GetCountryIsoCode(countryid As Integer) As String
            Dim pservice = ServiceFactory.GetCountryService
            Dim presponse = pservice.GetCountryById(countryid)
            Dim iPostcode As New CountryBO
            If (presponse.Success) Then iPostcode = presponse.Values.FirstOrDefault
            Return iPostcode.ISOCode
        End Function
        Function DetailExportToPdf(id As Integer, projectid As Integer) As ActionResult
            Dim model As New ClientModel()
            Dim cservice = ServiceFactory.GetClientService()
            Dim uservice = ServiceFactory.GetUnitService
            Dim pservice = ServiceFactory.GetProjectService
            'Get Client
            Dim cresponse = cservice.GetClientAccountById(id)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Client = cresponse.Value
            End If

            'Get Units for Client
            Dim response = uservice.GetGroupedUnitsByAccountId(id)
            model.UnitsGrouped = response.Values
            'Get Units with PaymentStages
            Dim invoicingresponse = uservice.GetClientUnitsWithStages(id)
            model.UnitsWithStages = invoicingresponse.Values
            'Get Invoices
            Dim invoiceresponse = pservice.GetInvoicesByUnitIds(model.UnitsWithStages.Select(Function(m) m.Unit.Id).ToList())
            model.Invoices = invoiceresponse.Values
            If projectid = 0 Then
                model.ProjectId = model.UnitsGrouped(0).Units(0).ProjectId
            Else
                model.ProjectId = projectid
            End If

            'Get Gifts for Client
            Dim response2 = cservice.GetClientGiftByAccountId(id)
            model.Gifts = response2.Values

            If model.Client.ExecutionDays = 0 Then
                model.ExecutionDays = pservice.GetProjectExecutionDays(model.ProjectId)
            Else
                model.ExecutionDays = model.Client.ExecutionDays
            End If
            If Not model.Client.StartDateConstruction Is Nothing Then
                model.StartDate = model.Client.StartDateConstruction
            Else
                model.StartDate = pservice.GetProjectStartDateConstruction(model.ProjectId)
            End If
            model.WorkingDaysLeft = -9999
            If Not model.ExecutionDays AndAlso Not model.StartDate = DateTime.MinValue Then
                model.FinalConstructionDate = pservice.GetFinalConstructionDay(model.ProjectId, model.StartDate, model.ExecutionDays)
                If Not model.FinalConstructionDate = DateTime.MinValue Then
                    model.WorkingDaysLeft = pservice.GetWorkingDaysLeft(model.FinalConstructionDate, model.ProjectId)
                End If
            End If
            Dim a = New ViewAsPdf()
            a.ViewName = "DetailPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Portrait
            a.PageSize = Options.Size.A4
            a.IsGrayScale = False
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Dim ms As New MemoryStream(pdfBytes)
            Return New FileStreamResult(ms, "application/pdf")
        End Function
        Function InvoiceToPdf(id As Integer) As ActionResult

            Dim invoice As New InvoiceBO()
            Dim service = ServiceFactory.GetInvoicingService()
            Dim uservice = ServiceFactory.GetUnitService
            Dim pservice = ServiceFactory.GetProjectService
            'Get Client
            Dim response = service.GetInvoiceByID(id)
            If (response.Success) Then invoice = response.Value
            Dim switches As String = String.Format("--image-quality 100 --no-pdf-compression --allow {0} --footer-html {0}", Url.Action("InvoiceFooter", "Klanten", New With {.area = "", .id = invoice.Id}, "http"))
            Dim a = New ViewAsPdf()
            a.ViewName = "InvoicePDF"
            a.Model = invoice
            a.PageOrientation = Options.Orientation.Portrait
            a.PageSize = Options.Size.A4
            a.PageMargins = New Options.Margins(15, 15, 70, 15)
            a.IsGrayScale = False
            a.IsLowQuality = False
            a.CustomSwitches = switches
            a.FileName = invoice.PublicId
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Dim ms As New MemoryStream(pdfBytes)
            Return New FileStreamResult(ms, "application/pdf")

        End Function

        <HttpGet>
        Function CalendarToPdf(id As Integer, projectid As Integer) As ActionResult
            Dim model As New ClientCalendarModel()
            model.ProjectId = projectid

            Dim cservice = ServiceFactory.GetClientService()
            Dim uservice = ServiceFactory.GetUnitService
            Dim pservice = ServiceFactory.GetProjectService
            'Get Client
            Dim cresponse = cservice.GetClientAccountById(id)
            If (cresponse.Success) Then
                cresponse.Value = cresponse.Values.FirstOrDefault
                model.Client = cresponse.Value
            End If
            model.WeatherStationId = pservice.GetProjectWeatherstation(model.ProjectId)

            If model.Client.ExecutionDays = 0 Then
                model.ExecutionDays = pservice.GetProjectExecutionDays(model.ProjectId)
            Else
                model.ExecutionDays = model.Client.ExecutionDays
            End If
            If Not model.Client.StartDateConstruction Is Nothing Then
                model.StartDate = model.Client.StartDateConstruction
            Else
                model.StartDate = pservice.GetProjectStartDateConstruction(model.ProjectId)
            End If
            model.Days = GetWeatherDays(model.WeatherStationId, model.Startdate, Date.Now)
            'Return New ViewAsPdf("CalendarPdf", model)
            'Return View("CalendarPdf", model)

            Dim a = New ViewAsPdf()
            a.ViewName = "CalendarPDF"
            a.Model = model
            a.PageOrientation = Options.Orientation.Portrait
            a.PageSize = Options.Size.A4
            a.IsGrayScale = False
            a.IsBackgroundDisabled = False
            a.IsJavaScriptDisabled = False
            Dim pdfBytes = a.BuildPdf(ControllerContext)
            Dim ms As New MemoryStream(pdfBytes)
            Return New FileStreamResult(ms, "application/pdf")
        End Function
        Function GetWeatherDays(weatherstationid As Integer, startdate As Date, enddate As Date) As List(Of CalendarDayBO)
            Dim Service = ServiceFactory.GetProjectService
            Dim response = Service.GetClientWeatherDays(weatherstationid, startdate, enddate)
            Dim days As New List(Of CalendarDayBO)
            If (response.Success) Then
                For Each bwd In response.Values
                    Dim day As New CalendarDayBO
                    If bwd.Type = 0 Then
                        day.Color = "#009336"
                        day.Title = "regen/vorst"
                    Else
                        day.Color = "red"
                        day.Title = "wind"
                    End If
                    day.Id = bwd.Id
                    day.Year = bwd.BWDate.Year
                    day.Month = bwd.BWDate.Month
                    day.Day = bwd.BWDate.Day
                    days.Add(day)
                Next


                Dim response2 = Service.GetVacationDays()
                If response2.Success Then
                    For Each vd In response2.Values
                        Dim day As New CalendarDayBO

                        day.Color = "#777"
                        day.Title = "verlofdag"
                        day.Id = vd.Id
                        day.Year = vd.VacationDay.Year
                        day.Month = vd.VacationDay.Month
                        day.Day = vd.VacationDay.Day
                        days.Add(day)
                    Next
                End If
            End If


            Return days
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
        <AllowAnonymous>
        Public Function InvoiceFooter(id As Integer) As ActionResult
            Dim invoice As New InvoiceBO()
            Dim service = ServiceFactory.GetInvoicingService()
            ViewBag.CompanyStreetNr = My.Settings.CompanyStreetNr
            ViewBag.CompanyBankaccount = My.Settings.CompanyBankaccount
            ViewBag.CompanyEmail = My.Settings.CompanyEmail
            ViewBag.CompanyFax = My.Settings.CompanyFax
            ViewBag.CompanyName = My.Settings.CompanyName
            ViewBag.CompanyPhone = My.Settings.CompanyPhone
            ViewBag.CompanyPostalcode = My.Settings.CompanyPostalcode
            ViewBag.CompanyRPR = My.Settings.CompanyRPR
            ViewBag.CompanyVAT = My.Settings.CompanyVAT
            ViewBag.CompanyWWW = My.Settings.CompanyWWW
            Dim Response = service.GetInvoiceByID(id)
            If (response.Success) Then invoice = response.Value
            Return View(invoice)
        End Function




        'LATER TE VERWIJDEREN Sub - DIENT VOOR HET UPDATEN VAN FACTUURGEGEVENS
        Public Sub UpdateInvoices()
            Dim service = ServiceFactory.GetInvoicingService()
            Dim projectserv = ServiceFactory.GetProjectService()
            Dim invoices As New List(Of InvoiceBO)
            Dim response = service.GetInvoices
            If response.Success Then invoices = response.Values

            For Each item In invoices
                If item.Id = 103 Then
                    Dim test As String = ""
                End If
                Dim OwnerPercentage As Decimal
                If item.ClientType = ClientType.Klant Then
                    Dim clientwu As New ClientAccountWithUnitsBO
                    Dim clientservice = ServiceFactory.GetClientService
                    '    Dim projectservice = ServiceFactory.GetProjectService()
                    Dim clientresponse = clientservice.GetClientAccountsByIdWithUnits(item.ClientId)
                    If clientresponse.Success Then clientwu = clientresponse.Value
                    '    Dim salessettings As New ProjectSalesSettingsBO
                    '    Dim salesresponse = projectservice.GetSalesSettings(clientwu.Units.FirstOrDefault.ProjectId)
                    '    Dim project As New ProjectBO
                    '    Dim projectresponse = projectservice.GetProjectByID(clientwu.Units.FirstOrDefault.ProjectId)
                    '    If projectresponse.Success Then project = projectresponse.Value
                    '    If salesresponse.Success Then salessettings = salesresponse.Value

                    '    Dim FunctionResponse As New Response
                    '    'Dim path As String = My.Settings.InvoiceURL & Date.Now.Year & "\"
                    '    'CheckDir(path)
                    '    Dim pservice = ServiceFactory.GetProjectService
                    OwnerPercentage = 100
                    If Not clientwu.Client.CoOwners Is Nothing Then
                        OwnerPercentage -= clientwu.Client.CoOwners.Sum(Function(m) m.CoOwnerPercentage)
                    End If
                    '    Dim account As String
                    '    If Not clientwu.Client.BankAccountNumber Is Nothing Or Not clientwu.Client.BankAccountNumber = "" Then
                    '        account = clientwu.Client.BankAccountNumber
                    '    Else
                    '        account = salessettings.BankAccountNumber
                    '    End If
                    '    If clientwu.Client.InvoiceStreet = String.Empty Then
                    '        item.PublicId = item.Filename.Substring(0, item.Filename.IndexOf(" "))
                    '        item.ExpirationDate = item.Invoicedate.AddDays(14)
                    '        If Not clientwu.Client.VATnumber Is Nothing Or Not clientwu.Client.VATnumber = "" Then item.VatNumber = clientwu.Client.VATnumber
                    '        If Not clientwu.Client.Name Is Nothing Or Not clientwu.Client.Name = "" Then
                    '            If clientwu.Client.Salutation = Salutation.Dhr Or clientwu.Client.Salutation = Salutation.Mevr Then
                    '                item.ClientName = clientwu.Client.Salutation.GetDisplayName() & " " & clientwu.Client.Name & " " & ""
                    '            Else
                    '                item.ClientName = clientwu.Client.Salutation.GetDisplayName() & " " & clientwu.Client.Name
                    '            End If
                    '        Else
                    '            item.ClientName = clientwu.Client.CompanyName
                    '        End If
                    '        If clientwu.Client.Busnumber Is Nothing Or clientwu.Client.Busnumber = "" Then
                    '            item.Adress = clientwu.Client.Street & " " & clientwu.Client.Housenumber
                    '        Else
                    '            item.Adress = clientwu.Client.Street & " " & clientwu.Client.Housenumber & "/" & clientwu.Client.Busnumber
                    '        End If
                    '        Dim pc As New PostalCodeBO
                    '        pc.PostcodeId = clientwu.Client.Postalcode.PostcodeId
                    '        item.PostalCode = pc
                    '        item.BankAccount = account
                    '        If Not clientwu.Client.InvoiceExtra Is Nothing Or Not clientwu.Client.InvoiceExtra = "" Then
                    '            item.ExtraInfo = clientwu.Client.InvoiceExtra
                    '        Else
                    '            item.ExtraInfo = ""
                    '        End If

                    '    Else
                    '        item.PublicId = item.Filename.Substring(0, item.Filename.IndexOf(" "))
                    '        item.ExpirationDate = item.Invoicedate.AddDays(14)
                    '        If Not clientwu.Client.VATnumber Is Nothing Or Not clientwu.Client.VATnumber = "" Then item.VatNumber = clientwu.Client.VATnumber
                    '        If Not clientwu.Client.Name Is Nothing Or Not clientwu.Client.Name = "" Then
                    '            If clientwu.Client.Salutation = Salutation.Dhr Or clientwu.Client.Salutation = Salutation.Mevr Then
                    '                item.ClientName = clientwu.Client.Salutation.GetDisplayName() & " " & clientwu.Client.Name & " " & ""
                    '            Else
                    '                item.ClientName = clientwu.Client.Salutation.GetDisplayName() & " " & clientwu.Client.Name
                    '            End If
                    '        Else
                    '            item.ClientName = clientwu.Client.CompanyName
                    '        End If
                    '        If clientwu.Client.InvoiceBusnumber Is Nothing Or clientwu.Client.InvoiceBusnumber = "" Then
                    '            item.Adress = clientwu.Client.InvoiceStreet & " " & clientwu.Client.InvoiceHousenumber
                    '        Else
                    '            item.Adress = clientwu.Client.InvoiceStreet & " " & clientwu.Client.InvoiceHousenumber & "/" & clientwu.Client.InvoiceBusnumber
                    '        End If
                    '        Dim pc As New PostalCodeBO
                    '        pc.PostcodeId = clientwu.Client.Postalcode.PostcodeId
                    '        item.PostalCode = pc
                    '        item.BankAccount = account
                    '        If Not clientwu.Client.InvoiceExtra Is Nothing Or Not clientwu.Client.InvoiceExtra = "" Then
                    '            item.ExtraInfo = clientwu.Client.InvoiceExtra
                    '        Else
                    '            item.ExtraInfo = ""
                    '        End If
                    '    End If


                    Dim Text As String = ""
                    Dim eenheidsprijzen As String = ""
                    If OwnerPercentage = 100 Then
                        Text = "Voor de bouwwaarde van "
                    Else
                        Text = "Voor " & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van "
                    End If

                    Dim count As Integer = 0
                    Dim iu As List(Of UnitBO) = clientwu.Units.ToList()

                    iu = iu.OrderBy(Function(m) m.Type.GroupId).ToList()
                    Dim rowcount As Integer = 0
                    Dim TotalPrice0 As Decimal = 0
                    Dim TotalPrice6 As Decimal = 0
                    Dim TotalPrice21 As Decimal = 0
                    For Each iunit In iu
                        'FACTUURTEKST OPMAKEN
                        Text = Text & iunit.Type.Name.ToLower & " " & iunit.Name
                        If iunit.ConstructionValues.Sum(Function(m) m.ValueSold) > 0 Then
                            eenheidsprijzen = eenheidsprijzen & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van " & iunit.Type.Name.ToLower & " " & iunit.Name & " : " & FormatCurrency(iunit.ConstructionValues.Sum(Function(m) m.ValueSold) * OwnerPercentage / 100)
                            If Not iunit Is iu.Last Then
                                eenheidsprijzen &= "\n"
                                If Not iunit Is iu(iu.Count - 2) Then
                                    Text &= ", "
                                Else
                                    Text &= " en "
                                End If
                            End If
                            count += 1


                        End If
                    Next

                    'Text &= " in project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."

                    'item.Text = Text
                    item.DetailText = eenheidsprijzen




                Else


                End If
                If item.ClientType = ClientType.Medeeigenaar Then
                    Dim coowner As New ClientContactBO
                    Dim clientwu As New ClientAccountWithUnitsBO
                    Dim clientservice = ServiceFactory.GetClientService
                    Dim projectservice = ServiceFactory.GetProjectService()
                    Dim coownerresponse = clientservice.GetClientContactById(item.ClientId)
                    If coownerresponse.Success Then coowner = coownerresponse.Value

                    Dim clientresponse = clientservice.GetClientAccountsByIdWithUnits(coowner.AccountId)
                    If clientresponse.Success Then clientwu = clientresponse.Value
                    '    Dim salessettings As New ProjectSalesSettingsBO
                    '    Dim salesresponse = projectservice.GetSalesSettings(clientwu.Units.FirstOrDefault.ProjectId)
                    '    Dim project As New ProjectBO
                    '    Dim projectresponse = projectservice.GetProjectByID(clientwu.Units.FirstOrDefault.ProjectId)
                    '    If projectresponse.Success Then project = projectresponse.Value
                    '    If salesresponse.Success Then salessettings = salesresponse.Value

                    '    Dim FunctionResponse As New Response
                    '    'Dim path As String = My.Settings.InvoiceURL & Date.Now.Year & "\"
                    '    'CheckDir(path)
                    '    Dim pservice = ServiceFactory.GetProjectService
                    OwnerPercentage = coowner.CoOwnerPercentage
                    '    Dim account As String
                    '    If Not clientwu.Client.BankAccountNumber Is Nothing Or Not clientwu.Client.BankAccountNumber = "" Then
                    '        account = clientwu.Client.BankAccountNumber
                    '    Else
                    '        account = salessettings.BankAccountNumber
                    '    End If
                    '    If coowner.InvoiceStreet = String.Empty Then
                    '        item.PublicId = item.Filename.Substring(0, item.Filename.IndexOf(" "))
                    '        item.ExpirationDate = item.Invoicedate.AddDays(14)
                    '        If Not coowner.VATnumber Is Nothing Or Not coowner.VATnumber = "" Then item.VatNumber = coowner.VATnumber
                    '        If Not coowner.Name Is Nothing Or Not coowner.Name = "" Then
                    '            If coowner.Salutation = Salutation.Dhr Or coowner.Salutation = Salutation.Mevr Then
                    '                item.ClientName = coowner.Salutation.GetDisplayName() & " " & coowner.Name & " " & coowner.Firstname
                    '            Else
                    '                item.ClientName = coowner.Salutation.GetDisplayName() & " " & coowner.Name
                    '            End If
                    '        Else
                    '            item.ClientName = coowner.CompanyName
                    '        End If
                    '        If coowner.Busnumber Is Nothing Or coowner.Busnumber = "" Then
                    '            item.Adress = coowner.Street & " " & coowner.Housenumber
                    '        Else
                    '            item.Adress = coowner.Street & " " & coowner.Housenumber & "/" & coowner.Busnumber
                    '        End If
                    '        Dim pc As New PostalCodeBO
                    '        pc.PostcodeId = coowner.Postalcode.PostcodeId
                    '        item.PostalCode = pc
                    '        item.BankAccount = account
                    '        If Not clientwu.Client.InvoiceExtra Is Nothing Or Not clientwu.Client.InvoiceExtra = "" Then
                    '            item.ExtraInfo = clientwu.Client.InvoiceExtra
                    '        Else
                    '            item.ExtraInfo = ""
                    '        End If

                    '    Else
                    '        item.PublicId = item.Filename.Substring(0, item.Filename.IndexOf(" "))
                    '        item.ExpirationDate = item.Invoicedate.AddDays(14)
                    '        If Not coowner.VATnumber Is Nothing Or Not coowner.VATnumber = "" Then item.VatNumber = coowner.VATnumber
                    '        If Not coowner.Name Is Nothing Or Not coowner.Name = "" Then
                    '            If coowner.Salutation = Salutation.Dhr Or coowner.Salutation = Salutation.Mevr Then
                    '                item.ClientName = coowner.Salutation.GetDisplayName() & " " & coowner.Name & " " & coowner.Firstname
                    '            Else
                    '                item.ClientName = coowner.Salutation.GetDisplayName() & " " & coowner.Name
                    '            End If
                    '        Else
                    '            item.ClientName = coowner.CompanyName
                    '        End If
                    '        If coowner.InvoiceBusnumber Is Nothing Or coowner.InvoiceBusnumber = "" Then
                    '            item.Adress = coowner.InvoiceStreet & " " & coowner.InvoiceHousenumber
                    '        Else
                    '            item.Adress = coowner.InvoiceStreet & " " & coowner.InvoiceHousenumber & "/" & coowner.InvoiceBusnumber
                    '        End If
                    '        Dim pc As New PostalCodeBO
                    '        pc.PostcodeId = coowner.Postalcode.PostcodeId
                    '        item.PostalCode = pc
                    '        item.BankAccount = account
                    '        If Not clientwu.Client.InvoiceExtra Is Nothing Or Not clientwu.Client.InvoiceExtra = "" Then
                    '            item.ExtraInfo = clientwu.Client.InvoiceExtra
                    '        Else
                    '            item.ExtraInfo = ""
                    '        End If
                    '    End If


                    Dim Text As String = ""
                    Dim eenheidsprijzen As String = ""
                    If OwnerPercentage = 100 Then
                        Text = "Voor de bouwwaarde van "
                    Else
                        Text = "Voor " & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van "
                    End If

                    Dim count As Integer = 0
                    Dim iu As List(Of UnitBO) = clientwu.Units.ToList()

                    iu = iu.OrderBy(Function(m) m.Type.GroupId).ToList()
                    Dim rowcount As Integer = 0
                    Dim TotalPrice0 As Decimal = 0
                    Dim TotalPrice6 As Decimal = 0
                    Dim TotalPrice21 As Decimal = 0
                    For Each iunit In iu
                        'FACTUURTEKST OPMAKEN
                        Text = Text & iunit.Type.Name.ToLower & " " & iunit.Name
                        If iunit.ConstructionValues.Sum(Function(m) m.ValueSold) > 0 Then
                            eenheidsprijzen = eenheidsprijzen & OwnerPercentage.ToString("#0.##") & " % van de bouwwaarde van " & iunit.Type.Name.ToLower & " " & iunit.Name & " : " & FormatCurrency(iunit.ConstructionValues.Sum(Function(m) m.ValueSold) * OwnerPercentage / 100)
                            If Not iunit Is iu.Last Then
                                eenheidsprijzen &= "\n"
                                If Not iunit Is iu(iu.Count - 2) Then
                                    Text &= ", "
                                Else
                                    Text &= " en "
                                End If
                            End If
                            count += 1


                        End If
                    Next

                    'Text &= " in project " & project.Name & ", " & project.Street & " " & project.HouseNumber & " te " & project.Postalcode.Gemeente & " ingevolge verkoopsovereenkomst."

                    'item.Text = Text
                    item.DetailText = eenheidsprijzen
                End If
                'For Each row In item.Rows
                '    If item.ClientType = ClientType.Klant Then
                '        Dim clientwu As New ClientAccountBO
                '        Dim clientservice = ServiceFactory.GetClientService
                '        Dim clientresponse = clientservice.GetClientAccountById(item.ClientId)
                '        If clientresponse.Success Then clientwu = clientresponse.Value
                '        Dim unit As New UnitBO
                '        Dim CV As New UnitConstructionValueBO
                '        Dim projectservice = ServiceFactory.GetProjectService()
                '        Dim unitservice = ServiceFactory.GetUnitService()
                '        Dim uresponse = unitservice.GetUnitById(row.UnitId)
                '        If uresponse.Success Then unit = uresponse.Value
                '        Dim psresponse = projectservice.GetProjectPaymentStage(row.StageId)
                '        Dim stage As New ProjectPaymentStageBO
                '        If psresponse.Success Then stage = psresponse.Value
                '        Dim cvresponse = unitservice.GetConstructionValue(row.ConstructionValueId)
                '        If cvresponse.Success Then CV = cvresponse.Value
                '        If clientwu.CoOwners.Count = 0 Then
                '            row.Price = CV.ValueSold * stage.Percentage / 100
                '        Else
                '            Dim perc As Decimal = 100 - clientwu.CoOwners.Sum(Function(m) m.CoOwnerPercentage)
                '            row.Price = CV.ValueSold * perc / 100 * stage.Percentage / 100
                '        End If

                '    Else
                '        Dim clientwu As New ClientContactBO
                '        Dim clientservice = ServiceFactory.GetClientService
                '        Dim clientresponse = clientservice.GetClientContactById(item.ClientId)
                '        If clientresponse.Success Then clientwu = clientresponse.Value
                '        Dim unit As New UnitBO
                '        Dim CV As New UnitConstructionValueBO
                '        Dim projectservice = ServiceFactory.GetProjectService()
                '        Dim unitservice = ServiceFactory.GetUnitService()
                '        Dim uresponse = unitservice.GetUnitById(row.UnitId)
                '        If uresponse.Success Then unit = uresponse.Value
                '        Dim psresponse = projectservice.GetProjectPaymentStage(row.StageId)
                '        Dim stage As New ProjectPaymentStageBO
                '        If psresponse.Success Then stage = psresponse.Value
                '        Dim cvresponse = unitservice.GetConstructionValue(row.ConstructionValueId)
                '        If cvresponse.Success Then CV = cvresponse.Value
                '        row.Price = CV.ValueSold * clientwu.CoOwnerPercentage / 100 * stage.Percentage / 100
                '    End If
                'Next
                Dim err = projectserv.InsertUpdateProjectInvoice(item)

            Next

        End Sub

    End Class
End Namespace