Imports BO
Imports Facade
Imports DAL
Imports System.Text.RegularExpressions

Public Class ProjectService
    Implements IProjectService
    Public Function GetProjectByID(id As Integer) As GetResponse(Of ProjectBO) Implements IProjectService.GetProjectByID
        Dim response As New GetResponse(Of ProjectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim _entity = dao.GetById(id)
        Dim project As New ProjectBO

        Dim err = ProjectTranslator.TranslateEntityToBO(_entity, project)
        If err = ErrorCode.Success Then
            response.Value = project
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetProjectBySlug(slug As String) As GetResponse(Of ProjectBO) Implements IProjectService.GetProjectBySlug
        Dim response As New GetResponse(Of ProjectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Slug = slug).FirstOrDefault
        Dim project As New ProjectBO

        Dim err = ProjectTranslator.TranslateEntityToBO(_entity, project)
        If err = ErrorCode.Success Then
            response.Value = project
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetProjects() As GetResponse(Of ProjectBO) Implements IProjectService.GetProjects
        'TODO
    End Function
    Public Function GetProjectsForList(Optional Type As ProjectType = 0, Optional StatusId As Integer = 0, Optional UserId As String = Nothing, Optional BuilderId As Integer = 0, Optional TrimCommercialText As Boolean = False) As GetResponse(Of ProjectBO) Implements IProjectService.GetProjectsForList
        Dim response As New GetResponse(Of ProjectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim entities
        If Not Type = 0 Then
            If Not StatusId = 0 Then
                If Not UserId = Nothing Then
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.UserId = UserId AndAlso m.BuilderId = BuilderId AndAlso m.ProjectType = Type)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.UserId = UserId AndAlso m.ProjectType = Type)
                    End If
                Else
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.BuilderId = BuilderId AndAlso m.ProjectType = Type)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.ProjectType = Type)
                    End If
                End If
            Else
                If Not UserId = Nothing Then
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.UserId = UserId AndAlso m.BuilderId = BuilderId AndAlso m.ProjectType = Type)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .ProjectType = m.ProjectType}).Where(Function(m) m.UserId = UserId AndAlso m.ProjectType = Type)
                    End If
                Else
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.BuilderId = BuilderId AndAlso m.ProjectType = Type)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .ProjectType = m.ProjectType}).Where(Function(m) m.ProjectType = Type)
                    End If
                End If
            End If
        Else
            If Not StatusId = 0 Then
                If Not UserId = Nothing Then
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.UserId = UserId AndAlso m.BuilderId = BuilderId)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.UserId = UserId)
                    End If
                Else
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId AndAlso m.BuilderId = BuilderId)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .ProjectType = m.ProjectType}).Where(Function(m) m.status = StatusId)
                    End If
                End If
            Else
                If Not UserId = Nothing Then
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.UserId = UserId AndAlso m.BuilderId = BuilderId)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .UserId = m.AspNetUserID, .ProjectType = m.ProjectType}).Where(Function(m) m.UserId = UserId)
                    End If
                Else
                    If Not BuilderId = 0 Then
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .BuilderId = m.BuilderID, .ProjectType = m.ProjectType}).Where(Function(m) m.BuilderId = BuilderId)
                    Else
                        entities = dao.GetNoTracking().Select(Function(m) New With {.id = m.ProjectID, .Name = m.ProjectName, .status = m.ProjectStatus.StatusID, .location = m.PostalCode.Gemeente, .Defaultpic = m.DefaultPicture.Name, .DefaultpicCaption = m.DefaultPicture.Caption, .DeliveryDate = m.DeliveryDate, .CommercialTitleNL = m.CommercialTitleNL, .CommercialTextNL = m.CommercialTextNL, .slug = m.Slug, .ProjectType = m.ProjectType})
                    End If
                End If
            End If
        End If
        For Each _entity In entities
            Dim bo As New ProjectBO()
            bo.Id = _entity.id
            If TrimCommercialText = True And _entity.CommercialTextNL.ToString.Length > 150 Then bo.CommercialTextNL = _entity.CommercialTextNL.ToString.Substring(0, 150) & " ..." Else bo.CommercialTextNL = _entity.CommercialTextNL
            bo.CommercialTitleNL = _entity.CommercialTitleNL
            bo.Name = _entity.Name
            bo.Slug = _entity.slug
            bo.Postalcode.Gemeente = _entity.location
            bo.Status.Id = _entity.status
            If Not _entity.deliverydate Is Nothing Then
                bo.DeliveryDate = _entity.deliverydate
            End If
            If Not _entity.Defaultpic = "" Then
                bo.DefaultPicture.Name = _entity.Defaultpic
                bo.DefaultPicture.Caption = _entity.DefaultpicCaption
            Else
                bo.DefaultPicture.Name = Nothing
            End If
            bo.ProjectType = _entity.ProjectType
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetProjectNameById(id As Integer) As String Implements IProjectService.GetProjectNameById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Return dao.GetById(id).ProjectName
    End Function
    Public Function GetProjectCityById(id As Integer) As String Implements IProjectService.GetProjectCityById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Return dao.GetById(id).PostalCode.Gemeente
    End Function
    Public Function GetProjectSlugById(id As Integer) As String Implements IProjectService.GetProjectSlugById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Return dao.GetById(id).Slug
    End Function
    Public Function GetProjectLandshareById(id As Integer) As Integer Implements IProjectService.GetProjectLandshareById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim landshare As Integer = 0
        If Not dao.GetById(id).TotalLandShare Is Nothing Then landshare = dao.GetById(id).TotalLandShare
        Return landshare
    End Function
    Public Function GetProjectWeatherstation(projectid As Integer) As Integer Implements IProjectService.GetProjectWeatherstation
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim i As Integer = 0
        If dao.GetById(projectid).WheaterStationID.HasValue Then
            i = dao.GetById(projectid).WheaterStationID
        End If
        Return i
    End Function
    Public Function GetProjectsForSearchList(searchterm As String) As GetResponse(Of SelectBO) Implements IProjectService.GetProjectsForSearchList
        Dim response As New GetResponse(Of SelectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO
        Dim entitys = dao.GetNoTracking().Where(ProjectQuery.GetNameQuery(searchterm)).OrderBy(Function(m) m.ProjectName).Select(Function(m) New SelectBO With {.id = m.ProjectID, .text = m.ProjectName, .extra = "Project"})
        response.Values = entitys.ToList()
        Return response
    End Function
    Public Function GetProjectsWithAvailableUnits() As GetResponse(Of IdNameBO) Implements IProjectService.GetProjectsWithAvailableUnits
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.Any(Function(i) i.ClientAccountID IsNot Nothing Or i.ClientAccountID <> 0))
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetProjectStartDateConstruction(projectid As Integer) As Date Implements IProjectService.GetProjectStartDateConstruction
        Dim uow As New UnitOfWork(False)
        Dim StartDate As Date
        Dim dao = uow.GetProjectDAO()
        If dao.GetById(projectid).StartDateConstruction.HasValue Then
            StartDate = dao.GetById(projectid).StartDateConstruction
        End If
        Return StartDate
    End Function
    Public Function GetProjectExecutionDays(projectid As Integer) As Integer Implements IProjectService.GetProjectExecutionDays
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Return dao.GetById(projectid).ExecutionDays
    End Function
    Public Function GetWorkingDaysLeft(finalconstructiondate As Date, projectid As Integer) As Integer Implements IProjectService.GetWorkingDaysLeft
        If Not finalconstructiondate = DateTime.MinValue Then
            Dim uow As New UnitOfWork(False)
            Dim dao = uow.GetVacationDaysDAO()
            Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId Is Nothing Or m.ProjectId = projectid)
            Dim VDS As New List(Of Date)
            For Each _entity In entities
                VDS.Add(_entity.VacationDay)
            Next
            Return BusinessDaysUntil(Date.Now, finalconstructiondate, VDS.ToArray)
        Else
            Return -9999
        End If

    End Function
    Public Function GetFinalConstructionDay(projectid As Integer, startdate As Date, executiondays As Integer) As Date Implements IProjectService.GetFinalConstructionDay
        Dim weatherstationid As Integer
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetVacationDaysDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId Is Nothing Or m.ProjectId = projectid)
        Dim VDS As New List(Of Date)
        For Each _entity In entities
            VDS.Add(_entity.VacationDay)
        Next
        weatherstationid = GetProjectWeatherstation(projectid)
        If Not weatherstationid = 0 Then
            Dim BWDdao = uow.GetBadWeatherDaysDAO()
            Dim BWDentities = BWDdao.GetNoTracking().Where(Function(m) m.WeatherstationId = weatherstationid).GroupBy(Function(m) m.Date)
            Dim BWDS As New List(Of Date)
            For Each ent In BWDentities
                BWDS.Add(ent.Key)
            Next

            Return AddWorkDays(startdate, executiondays, BWDS.ToArray, VDS.ToArray)
        Else
            Return DateTime.MinValue
        End If

    End Function
    Public Function GetProjectFolderById(id As Integer) As String Implements IProjectService.GetProjectFolderById
        Dim Folder As String = ""
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Folder = dao.GetNoTracking().Where(Function(m) m.ProjectID = id).FirstOrDefault.ProjectFolder
        Return Folder
    End Function
    Function CheckProjectFinished(Optional userid As String = "") As GetResponse(Of WarningBO) Implements IProjectService.CheckProjectFinished
        Dim response As New GetResponse(Of WarningBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        If userid = "" Then
            Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectStatus.StatusID = 1)
            For Each _entity In entities
                If _entity.DeliveryDate Is Nothing And _entity.DocDelivery = True Then
                    Dim bo As New WarningBO
                    bo.ID = _entity.ProjectID
                    bo.ProjectId = _entity.ProjectID
                    bo.Display = "De voorlopige opleverdatum van project " & _entity.ProjectName & " is niet ingevuld."
                    bo.Type = "danger"
                    response.AddValue(bo)
                Else
                    'Check ProjectDocs
                    Dim boDocs As WarningBO = CheckProjectDocs(_entity)
                    If Not boDocs.Display = "" Then response.AddValue(boDocs)

                    If _entity.DeliveryDateDef Is Nothing And _entity.DocDefDelivery = True Then
                        If _entity.DeliveryDate.Value.AddYears(10).CompareTo(Date.Today) > 0 Then
                            If _entity.DeliveryDate.Value.AddMonths(12).CompareTo(Date.Today) <= 0 Then
                                Dim bo As New WarningBO
                                bo.ID = _entity.ProjectID
                                bo.ProjectId = _entity.ProjectID
                                bo.Display = "De definitieve oplevering van project " & _entity.ProjectName & " is nog niet gebeurd, gelieve deze aan te vragen!"
                                bo.Type = "danger"
                                response.AddValue(bo)
                            ElseIf _entity.DeliveryDate.Value.AddMonths(11).CompareTo(Date.Today) <= 0 Then
                                Dim bo As New WarningBO
                                bo.ID = _entity.ProjectID
                                bo.ProjectId = _entity.ProjectID
                                bo.Display = "De definitieve oplevering van project " & _entity.ProjectName & " kan gebeuren vanaf " & _entity.DeliveryDate.Value.AddMonths(11).Date & " , gelieve deze aan te vragen!"
                                bo.Type = "warning"
                                response.AddValue(bo)
                            End If
                        End If
                    End If
                End If
            Next
        Else
            Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectStatus.StatusID = 1 AndAlso m.AspNetUserID = userid)
            For Each _entity In entities
                If _entity.DeliveryDate Is Nothing And _entity.DocDelivery = True Then
                    Dim bo As New WarningBO
                    bo.ID = _entity.ProjectID
                    bo.ProjectId = _entity.ProjectID
                    bo.Display = "De voorlopige opleverdatum van project " & _entity.ProjectName & " is niet ingevuld."
                    bo.Type = "danger"
                    response.AddValue(bo)
                ElseIf _entity.DeliveryDateDef Is Nothing And _entity.DocDefDelivery = True Then
                    'Check ProjectDocs
                    Dim boDocs As WarningBO = CheckProjectDocs(_entity)
                    If Not boDocs.Display = "" Then response.AddValue(boDocs)

                    If _entity.DeliveryDate.Value.AddYears(10).CompareTo(Date.Today) > 0 Then
                        If _entity.DeliveryDate.Value.AddMonths(12).CompareTo(Date.Today) <= 0 Then
                            Dim bo As New WarningBO
                            bo.ID = _entity.ProjectID
                            bo.ProjectId = _entity.ProjectID
                            bo.Display = "De definitieve oplevering van project " & _entity.ProjectName & " is nog niet gebeurd, gelieve deze aan te vragen!"
                            bo.Type = "danger"
                            response.AddValue(bo)
                        ElseIf _entity.DeliveryDate.Value.AddMonths(11).CompareTo(Date.Today) <= 0 Then
                            Dim bo As New WarningBO
                            bo.ID = _entity.ProjectID
                            bo.ProjectId = _entity.ProjectID
                            bo.Display = "De definitieve oplevering van project " & _entity.ProjectName & " kan gebeuren vanaf " & _entity.DeliveryDate.Value.AddMonths(11).Date & " , gelieve deze aan te vragen!"
                            bo.Type = "warning"
                            response.AddValue(bo)
                        End If

                    End If
                End If
            Next
        End If

        Return response
    End Function
    Function CheckProjectDocs(_entity As Project) As WarningBO
        Dim boDocs As New WarningBO
        If _entity.DeliveryDate.Value.AddYears(10).CompareTo(Date.Today) > 0 Then
            boDocs.ID = _entity.ProjectID
            boDocs.ProjectId = _entity.ProjectID
            boDocs.Type = "warning"
            'If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.EPB).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : EPB Dossier" Else boDocs.Display = boDocs.Display & " , EPB Dossier"
            If (_entity.DocElectricalInspection = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.Electrical_inspection).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : Elektrische keuring" Else boDocs.Display = boDocs.Display & " , Elektrische keuring"
            If (_entity.DocWaterInspection = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.Water_inspection).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : Waterkeuring" Else boDocs.Display = boDocs.Display & " , Waterkeuring"
            If (_entity.DocSewerInspection = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.Sewer_inspection).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : Rioolkeuring" Else boDocs.Display = boDocs.Display & " , Rioolkeuring"
            If (_entity.DocFireInspection = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.Fire_inspection).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : Brandkeuring" Else boDocs.Display = boDocs.Display & " , Brandkeuring"
            If (_entity.DocDelivery = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.Delivery).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : Voorlopige oplevering" Else boDocs.Display = boDocs.Display & " , Voorlopige oplevering"
            If (_entity.DocPID = True) Then If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.PID).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : PID" Else boDocs.Display = boDocs.Display & " , PID"
            End If
            Return boDocs
    End Function
    Public Function InsertUpdate(project As ProjectBO) As Response Implements IProjectService.InsertUpdate
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(project.Name)) Then
            response.AddError("Projectnaam is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As Project = Nothing
        If (project.Id = 0) Then
            _entity = uow.GetProjectDAO.GetNew()
        Else
            _entity = uow.GetProjectDAO().GetById(project.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectTranslator.TranslateBOToEntity(_entity, project, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("project not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Public Function Delete(ids As List(Of Integer)) As Response Implements IProjectService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetProjectDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function Delete(bos As List(Of ProjectBO)) As Response Implements IProjectService.Delete
        Return Delete(bos.Select(Function(s) s.Id).ToList())
    End Function

    'Wheaterstations
    Public Function GetWheaterstations() As GetResponse(Of WheaterStationBO) Implements IProjectService.GetWheaterstations
        'TODO   
    End Function

    Public Function GetWheaterstationsSelect() As GetResponse(Of IdNameBO) Implements IProjectService.GetWheaterstationsSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetWheaterstationsDAO()
        Dim entities = dao.GetNoTracking()
        entities = entities.OrderBy(Function(m) m.Name)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetWheaterstations(searchterm As String) As GetResponse(Of WheaterStationBO) Implements IProjectService.GetWheaterstations
        Dim response As New GetResponse(Of WheaterStationBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetWheaterstationsDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.Name.Contains(searchterm)).OrderBy(Function(m) m.Name)
        For Each _entity In entitys
            Dim bo As New WheaterStationBO
            Dim err = WheaterstationTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function

    'Badweatherdays
    Public Function GetBadWeatherDays(weatherstationid As Integer, type As Integer) As GetResponse(Of BadWeatherDayBO) Implements IProjectService.GetBadWeatherDays
        Dim response As New GetResponse(Of BadWeatherDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetBadWeatherDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.WeatherstationId = weatherstationid AndAlso m.Type = type)
        For Each _entity In entitys
            Dim bo As New BadWeatherDayBO
            Dim err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetBadWeatherDays(weatherstationid As Integer, type As Integer, year As Integer) As GetResponse(Of BadWeatherDayBO) Implements IProjectService.GetBadWeatherDays
        Dim response As New GetResponse(Of BadWeatherDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetBadWeatherDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.WeatherstationId = weatherstationid AndAlso m.Type = type AndAlso m.Date.Year = year)
        For Each _entity In entitys
            Dim bo As New BadWeatherDayBO
            Dim err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetClientWeatherDays(weatherstationid As Integer, startdate As Date, enddate As Date) As GetResponse(Of BadWeatherDayBO) Implements IProjectService.GetClientWeatherDays
        Dim response As New GetResponse(Of BadWeatherDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetBadWeatherDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.WeatherstationId = weatherstationid AndAlso m.Date >= startdate AndAlso m.Date <= enddate)
        For Each _entity In entitys
            Dim bo As New BadWeatherDayBO
            Dim err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function InsertUpdateBadWeatherDay(BWD As BadWeatherDayBO) As Response Implements IProjectService.InsertUpdateBadWeatherDay
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(BWD.BWDate)) Then
            response.AddError("Datum is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As BadWeatherDays = Nothing
        If (BWD.Id = 0) Then
            _entity = uow.GetBadWeatherDaysDAO.GetNew()
        Else
            _entity = uow.GetBadWeatherDaysDAO().GetById(BWD.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = BadWeatherDayTranslator.TranslateBOToEntity(_entity, BWD)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Badweatherday not found")
        End If
        response.AddError(uow.SaveChanges())
        Dim m As New Message
        m.Message = _entity.Id
        m.Type = MessageType.Value
        response.Messages.Add(m)
        Return response
    End Function
    Public Function DeleteBadWeatherDays(ids As List(Of Integer)) As Response Implements IProjectService.DeleteBadWeatherDays
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetBadWeatherDaysDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteBadWeatherDays(bos As List(Of BadWeatherDayBO)) As Response Implements IProjectService.DeleteBadWeatherDays
        Return DeleteBadWeatherDays(bos.Select(Function(s) s.Id).ToList())
    End Function

    'Vacationdays
    Public Function GetVacationDays() As GetResponse(Of VacationDayBO) Implements IProjectService.GetVacationDays
        Dim response As New GetResponse(Of VacationDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetVacationDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.ProjectId Is Nothing)
        For Each _entity In entitys
            Dim bo As New VacationDayBO
            Dim err = VacationDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetProjectVacationDays(projectid As Integer) As GetResponse(Of VacationDayBO) Implements IProjectService.GetProjectVacationDays
        Dim response As New GetResponse(Of VacationDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetVacationDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.ProjectId = projectid)
        For Each _entity In entitys
            Dim bo As New VacationDayBO
            Dim err = VacationDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetVacationDaysGeneral() As GetResponse(Of VacationDayBO) Implements IProjectService.GetVacationDaysGeneral
        Dim response As New GetResponse(Of VacationDayBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetVacationDaysDAO()
        Dim entitys = dao.GetNoTracking().Where(Function(m) m.ProjectId Is Nothing Or m.ProjectId = 0)
        For Each _entity In entitys
            Dim bo As New VacationDayBO
            Dim err = VacationDayTranslator.TranslateEntityToBO(_entity, bo)
            If (err = ErrorCode.Success) Then response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function InsertUpdateVacationDay(vacationday As VacationDayBO) As Response Implements IProjectService.InsertUpdateVacationDay
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(vacationday.ToString)) Then
            response.AddError("Datum is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As VacationDays = Nothing
        If (vacationday.Id = 0) Then
            _entity = uow.GetVacationDaysDAO.GetNew()
        Else
            _entity = uow.GetVacationDaysDAO().GetById(vacationday.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = VacationDayTranslator.TranslateBOToEntity(_entity, vacationday)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Vacationday not found")
        End If
        response.AddError(uow.SaveChanges())
        Dim m As New Message
        m.Message = _entity.Id
        m.Type = MessageType.Value
        response.Messages.Add(m)
        Return response
    End Function
    Public Function DeleteVacationDays(ids As List(Of Integer)) As Response Implements IProjectService.DeleteVacationDays
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetVacationDaysDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteVacationDays(bos As List(Of VacationDayBO)) As Response Implements IProjectService.DeleteVacationDays
        Return DeleteVacationDays(bos.Select(Function(s) s.Id).ToList())
    End Function

    'Statuses

    Public Function GetStatuses() As GetResponse(Of ProjectStatusBO) Implements IProjectService.GetStatuses
        Dim response As New GetResponse(Of ProjectStatusBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectStatusDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New ProjectStatusBO()
            Dim err = StatusTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function GetStatusesForSelect() As GetResponse(Of IdNameBO) Implements IProjectService.GetStatusesForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectStatusDAO()
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function


    'Pictures
    Public Function GetPictureById(id As Integer) As GetResponse(Of ProjectPictureBO) Implements IProjectService.GetPictureById
        Dim response As New GetResponse(Of ProjectPictureBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPicturesDAO()
        Dim _entity = dao.GetById(id)
        Dim picture As New ProjectPictureBO

        Dim err = ProjectPictureTranslator.TranslateEntityToBO(_entity, picture)
        If err = ErrorCode.Success Then
            response.Value = picture
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetPicturesByProjectId(id As Integer) As GetResponse(Of ProjectPictureBO) Implements IProjectService.GetPicturesByProjectId
        Dim response As New GetResponse(Of ProjectPictureBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPicturesDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = id)
        For Each _entity In entities
            Dim bo As New ProjectPictureBO
            Dim err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Public Function GetPicturesByProjectSlug(slug As String) As GetResponse(Of ProjectPictureBO) Implements IProjectService.GetPicturesByProjectSlug
        Dim response As New GetResponse(Of ProjectPictureBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPicturesDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.Project1.Slug = slug)
        For Each _entity In entities
            Dim bo As New ProjectPictureBO
            Dim err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Function GetLatestPictures(number As Integer) As GetResponse(Of ProjectPictureBO) Implements IProjectService.GetLatestPictures
        Dim response As New GetResponse(Of ProjectPictureBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPicturesDAO()
        Dim entities = dao.GetNoTracking.OrderByDescending(Function(m) m.Datetimeuploaded).Take(number)
        For Each _entity In entities
            Dim bo As New ProjectPictureBO
            Dim err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Function GetLatestProjectPictures(number As Integer, projectid As Integer) As GetResponse(Of ProjectPictureBO) Implements IProjectService.GetLatestProjectPictures
        Dim response As New GetResponse(Of ProjectPictureBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPicturesDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.Type <> 3).OrderByDescending(Function(m) m.Datetimeuploaded).Take(number)
        For Each _entity In entities
            Dim bo As New ProjectPictureBO
            Dim err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Public Function InsertUpdatePicture(picture As ProjectPictureBO) As Response Implements IProjectService.InsertUpdatePicture
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(picture.Name)) Then
            response.AddError("Bestandsnaam is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As ProjectPictures = Nothing
        If (picture.Id = 0) Then
            _entity = uow.GetProjectPicturesDAO.GetNew()
        Else
            _entity = uow.GetProjectPicturesDAO().GetById(picture.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectPictureTranslator.TranslateBOToEntity(_entity, picture, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("project not found")
        End If
        response.AddError(uow.SaveChanges())
        Dim PictureId As New BO.Message
        PictureId.Type = MessageType.Value
        PictureId.Message = _entity.Id
        response.Messages.Add(PictureId)
        Return response
    End Function
    Public Function DeletePicture(ids As List(Of Integer)) As Response Implements IProjectService.DeletePicture
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetProjectPicturesDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function SetDefaultProjectPicture(projectid As Integer, pictureid As Integer) As Response Implements IProjectService.SetDefaultProjectPicture
        Dim response As New Response
        Dim uow As New UnitOfWork()
        Dim _entity As Project = Nothing
        If (projectid = 0) Then
            response.AddError("Er moet een project of foto geselecteerd zijn.")
        End If
        If (Not response.Success) Then Return response

        If pictureid = 0 Then
            _entity = uow.GetProjectDAO().GetById(projectid)
            If (_entity IsNot Nothing) Then
                If _entity.DefaultPicture IsNot Nothing Then
                    _entity.DefaultPictureId = Nothing
                End If
            Else
                response.AddError("project not found")
            End If
            response.AddError(uow.SaveChanges())

        Else
            _entity = uow.GetProjectDAO().GetById(projectid)

            If (_entity IsNot Nothing) Then
                For Each picture In _entity.ProjectPictures
                    If picture.Type = BO.PictureType.Hoofdfoto Then
                        picture.Type = BO.PictureType.Nevenfoto
                    End If
                Next
                If _entity.DefaultPicture IsNot Nothing Then
                    Dim picture As ProjectPictures = Nothing
                    picture = uow.GetProjectPicturesDAO().GetById(_entity.DefaultPictureId)
                    picture.Type = BO.PictureType.Nevenfoto

                End If
                _entity.DefaultPictureId = pictureid

            Else
                response.AddError("project not found")
            End If
            response.AddError(uow.SaveChanges())
        End If

      

        Return response
    End Function
    Public Function GetFacebookAlbumIdCoproByProjectId(id As Integer) As String Implements IProjectService.GetFacebookAlbumIdCoproByProjectId
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDAO()
        Return dao.GetById(id).FacebookAlbumID
    End Function


    'News
    Public Function GetNewsById(id As Integer) As GetResponse(Of ProjectNewsBO) Implements IProjectService.GetNewsById
        Dim response As New GetResponse(Of ProjectNewsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectNewsDAO()
        Dim _entity = dao.GetById(id)
        Dim news As New ProjectNewsBO

        Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, news)
        If err = ErrorCode.Success Then
            response.Value = news
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetNewsByProjectId(id As Integer) As GetResponse(Of ProjectNewsBO) Implements IProjectService.GetNewsByProjectId
        Dim response As New GetResponse(Of ProjectNewsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectNewsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = id).OrderByDescending(Function(m) m.Date)
        For Each _entity In entities
            Dim bo As New ProjectNewsBO
            Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function GetNewsByProjectSlug(slug As String) As GetResponse(Of ProjectNewsBO) Implements IProjectService.GetNewsByProjectSlug
        Dim response As New GetResponse(Of ProjectNewsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectNewsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.Project.Slug = slug).OrderByDescending(Function(m) m.Date)
        For Each _entity In entities
            Dim bo As New ProjectNewsBO
            Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Function GetLatestNews(number As Integer, Optional BuilderId As Integer = 0) As GetResponse(Of ProjectNewsBO) Implements IProjectService.GetLatestNews
        Dim response As New GetResponse(Of ProjectNewsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectNewsDAO()
        If Not BuilderId = 0 Then
            Dim entities = dao.GetNoTracking.Where(Function(m) m.Project.BuilderID = BuilderId).OrderByDescending(Function(m) m.Date).Take(number)
            For Each _entity In entities
                Dim bo As New ProjectNewsBO
                Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo)
                If err = ErrorCode.Success Then
                    response.AddValue(bo)
                Else
                    response.AddError(err.ToString())
                End If

            Next
        Else
            Dim entities = dao.GetNoTracking.OrderByDescending(Function(m) m.Date).Take(number)
            For Each _entity In entities
                Dim bo As New ProjectNewsBO
                Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo)
                If err = ErrorCode.Success Then
                    response.AddValue(bo)
                Else
                    response.AddError(err.ToString())
                End If

            Next
        End If

        Return response
    End Function
    Function GetLatestProjectNews(number As Integer, projectid As Integer) As GetResponse(Of ProjectNewsBO) Implements IProjectService.GetLatestProjectNews
        Dim response As New GetResponse(Of ProjectNewsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectNewsDAO()

        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid).OrderByDescending(Function(m) m.Date).Take(number)
        For Each _entity In entities
                Dim bo As New ProjectNewsBO
                Dim err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo)
                If err = ErrorCode.Success Then
                    response.AddValue(bo)
                Else
                    response.AddError(err.ToString())
                End If

            Next


        Return response
    End Function
    Public Function InsertUpdateNews(NewsItem As ProjectNewsBO) As Response Implements IProjectService.InsertUpdateNews
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(NewsItem.TitleNL)) Then
            response.AddError("Titel is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As ProjectNews = Nothing
        If (NewsItem.Id = 0) Then
            _entity = uow.GetProjectNewsDAO().GetNew()
            If Not NewsItem.Picture Is Nothing Then
                _entity.ProjectPictures = uow.GetProjectPicturesDAO.GetNew()
            End If
        Else
            _entity = uow.GetProjectNewsDAO().GetById(NewsItem.Id)
            If Not NewsItem.Picture Is Nothing And NewsItem.Picture.Id = 0 Then
                _entity.ProjectPictures = uow.GetProjectPicturesDAO.GetNew()
            Else
                Dim err = ProjectPictureTranslator.TranslateEntityToBO(uow.GetProjectPicturesDAO.GetById(NewsItem.Picture.Id), NewsItem.Picture)
                If (err <> ErrorCode.Success) Then
                    response.AddError(err.ToString())
                End If
            End If
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectNewsTranslator.TranslateBOToEntity(_entity, NewsItem, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Newsitem not found")
        End If
        response.AddError(uow.SaveChanges())
        
        Return response
    End Function
    Public Function DeleteNews(ids As List(Of Integer)) As Response Implements IProjectService.DeleteNews
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetProjectNewsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function

    'Levels
    Public Function GetLevelsByProjectId(id As Integer) As GetResponse(Of ProjectLevelBO) Implements IProjectService.GetLevelsByProjectId
        Dim response As New GetResponse(Of ProjectLevelBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectLevelsDAO
        Dim entities = dao.GetNoTracking.Where(Function(x) x.ProjectId = id)
        For Each _entity In entities
            Dim level As New ProjectLevelBO

            Dim err = ProjectLevelTranslator.TranslateEntityToBO(_entity, level)
            If err = ErrorCode.Success Then
                response.AddValue(level)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return Response
    End Function
    Public Function InsertUpdateLevel(Level As ProjectLevelBO) As Response Implements IProjectService.InsertUpdateLevel
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(Level.Name)) Then
            response.AddError("Naam is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As ProjectLevels = Nothing
        If (Level.Id = 0) Then
            _entity = uow.GetProjectLevelsDAO().GetNew()
        Else
            _entity = uow.GetProjectLevelsDAO().GetById(Level.Id)

        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectLevelTranslator.TranslateBOToEntity(_entity, Level, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Level not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function

    'Sales
    Public Function GetSalesSettings(projectid As Integer) As GetResponse(Of ProjectSalesSettingsBO) Implements IProjectService.GetSalesSettings
        Dim response As New GetResponse(Of ProjectSalesSettingsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectSalesSettingsDAO()
        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Projectid = projectid).FirstOrDefault
        Dim settings As New ProjectSalesSettingsBO

        Dim err = ProjectSalesSettingsTranslator.TranslateEntityToBO(_entity, settings)
        If err = ErrorCode.Success Then
            response.Value = settings
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetSalesSettings(ids As List(Of Integer)) As GetResponse(Of ProjectSalesSettingsBO) Implements IProjectService.GetSalesSettings
        Dim response As New GetResponse(Of ProjectSalesSettingsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectSalesSettingsDAO()
        For Each id In ids
            Dim _entity = dao.GetNoTracking.Where(Function(m) m.Projectid = id).FirstOrDefault
            Dim settings As New ProjectSalesSettingsBO

            Dim err = ProjectSalesSettingsTranslator.TranslateEntityToBO(_entity, settings)
            If err = ErrorCode.Success Then
                response.AddValue(settings)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetProjectSalesData(ids As List(Of Integer)) As GetResponse(Of ProjectSalesDataBO) Implements IProjectService.GetProjectSalesData
        Dim response As New GetResponse(Of ProjectSalesDataBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()

        For Each id In ids

            Dim salesdata As New ProjectSalesDataBO
            salesdata.ProjectId = id
            salesdata.LivingUnits = dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing).Where(Function(i) i.UnitTypes.GroupID = 1 Or i.UnitTypes.GroupID = 4).Count
            salesdata.LivingUnitsSold = dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.ClientAccountID IsNot Nothing).Where(Function(i) i.UnitTypes.GroupID = 1 Or i.UnitTypes.GroupID = 4).Count
            If Not salesdata.LivingUnits = 0 Then
                salesdata.PercentageLivingUnitsSold = salesdata.LivingUnitsSold / salesdata.LivingUnits * 100
            Else
                salesdata.PercentageLivingUnitsSold = 100
            End If
            salesdata.ValueForSale = If(dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.ClientAccountID Is Nothing AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing).Sum(Function(m) If(m.LandValue.HasValue, m.LandValue, 0) + If(m.UnitConstructionValue.Sum(Function(x) x.Value) > 0, m.UnitConstructionValue.Sum(Function(x) x.Value), 0)), 0)
            salesdata.ValueSold = If(dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing).Sum(Function(m) If(m.LandValueSold.HasValue, m.LandValueSold, 0) + If(m.UnitConstructionValue.Sum(Function(x) x.ValueSold) > 0, m.UnitConstructionValue.Sum(Function(x) x.ValueSold), 0)), 0)
            If Not (salesdata.ValueSold + salesdata.ValueForSale) = 0 Then
                salesdata.PercentageSold = salesdata.ValueSold / (salesdata.ValueSold + salesdata.ValueForSale) * 100
            Else
                salesdata.PercentageSold = 100
            End If

            salesdata.NumberAppartments = dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.UnitTypes.ID = 1).Count
            salesdata.NumberHouses = dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.UnitTypes.ID = 2).Count
            salesdata.NumberCommercial = dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.UnitTypes.ID = 10).Count
            salesdata.StartingPrice = If(dao.GetNoTracking().Where(Function(m) m.ProjectId = id AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.ClientAccountID Is Nothing).Where(Function(i) i.UnitTypes.GroupID = 1 Or i.UnitTypes.GroupID = 4).Min(Function(i) i.UnitConstructionValue.Where(Function(l) l.UnitId = i.Id).Sum(Function(x) x.Value) + i.LandValue), 0)
            response.AddValue(salesdata)
        Next
        Return response
    End Function
    Public Function InsertUpdateSalesSettings(salessettings As ProjectSalesSettingsBO) As Response Implements IProjectService.InsertUpdateSalesSettings
        Dim response As New Response
        If (salessettings.ProjectId <= 0) Then
            response.AddError("ProjectID is verplicht")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As ProjectSalesSettings = Nothing
        If (salessettings.SettingsId = 0) Then
            _entity = uow.GetProjectSalesSettingsDAO.GetNew()
        Else
            _entity = uow.GetProjectSalesSettingsDAO().GetById(salessettings.SettingsId)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectSalesSettingsTranslator.TranslateBOToEntity(_entity, salessettings, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("SalesSettings not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function DeleteSalesSettings(ids As List(Of Integer)) As Response Implements IProjectService.DeleteSalesSettings
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetProjectSalesSettingsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteSalesSettings(bos As List(Of ProjectSalesSettingsBO)) As Response Implements IProjectService.DeleteSalesSettings
        Return DeleteSalesSettings(bos.Select(Function(s) s.SettingsId).ToList())
    End Function
    Function GetProjectVatPercentage(projectid As Integer) As Integer Implements IProjectService.GetProjectVatPercentage
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectSalesSettingsDAO()
        Return dao.GetNoTracking().Where(Function(m) m.Projectid = projectid).FirstOrDefault.VATPercentage
    End Function
    'Docs
    Public Function GetProjectDocs(projectid As Integer, Optional type As ProjectDocType = 0) As GetResponse(Of ProjectDocBO) Implements IProjectService.GetProjectDocs
        Dim response As New GetResponse(Of ProjectDocBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()
        Dim entities
        If type <> 0 Then
            entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.ClientAccountId Is Nothing AndAlso m.Type = type).OrderByDescending(Function(m) m.Name)
        Else
            entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.ClientAccountId Is Nothing).OrderByDescending(Function(m) m.Name)
        End If
        For Each _entity In entities
            Dim bo As New ProjectDocBO
            Dim err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetProjectDocsForSelect(projectid As Integer, Optional type As ProjectDocType = ProjectDocType.Sales) As GetResponse(Of IdNameBO) Implements IProjectService.GetProjectDocsForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.Type = type AndAlso m.ClientAccountId Is Nothing).OrderByDescending(Function(m) m.Name)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetProjectDoc(docid As Integer) As GetResponse(Of ProjectDocBO) Implements IProjectService.GetProjectDoc
        Dim response As New GetResponse(Of ProjectDocBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()
        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Id = docid).FirstOrDefault

        Dim bo As New ProjectDocBO
        Dim err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If

        Return response
    End Function
    Public Function GetClientDocs(clientaccountid As Integer) As GetResponse(Of ProjectDocBO) Implements IProjectService.GetClientDocs
        Dim response As New GetResponse(Of ProjectDocBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ClientAccountId = clientaccountid).OrderByDescending(Function(m) m.Name)
        For Each _entity In entities
            Dim bo As New ProjectDocBO
            Dim err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Function GetLatestProjectDocs(number As Integer, projectid As Integer) As GetResponse(Of ProjectDocBO) Implements IProjectService.GetLatestProjectDocs
        Dim response As New GetResponse(Of ProjectDocBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()

        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.ClientAccountId Is Nothing).OrderByDescending(Function(m) m.Date).Take(number)
        For Each _entity In entities
            Dim bo As New ProjectDocBO
            Dim err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next


        Return response
    End Function
    Function GetLatestClientDocs(number As Integer, clientaccountid As Integer) As GetResponse(Of ProjectDocBO) Implements IProjectService.GetLatestClientDocs
        Dim response As New GetResponse(Of ProjectDocBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectDocsDAO()

        Dim entities = dao.GetNoTracking.Where(Function(m) m.ClientAccountId = clientaccountid).OrderByDescending(Function(m) m.Date).Take(number)
        For Each _entity In entities
            Dim bo As New ProjectDocBO
            Dim err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If

        Next


        Return response
    End Function
    Public Function InsertUpdateProjectDoc(ProjectDoc As ProjectDocBO) As Response Implements IProjectService.InsertUpdateProjectDoc
        Dim response As New Response
        If (ProjectDoc.ProjectId <= 0) Then
            response.AddError("ProjectID is verplicht")
        End If
        If (ProjectDoc.Filename Is Nothing OrElse ProjectDoc.Filename = "") Then
            response.AddError("Bestandsnaame is verplicht")
        End If
        'If (ProjectDoc.Name Is Nothing OrElse ProjectDoc.Name = "") Then
        '    response.AddError("Naam is verplicht")
        'End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As ProjectDocs = Nothing
        Dim _delentity As IdNameBO = Nothing
        If (ProjectDoc.Docid = 0) Then
            If ProjectDoc.Type = ProjectDocType.keycombinationcertificate Then
                _delentity = GetProjectDocsForSelect(ProjectDoc.ProjectId, ProjectDocType.keycombinationcertificate).Values.FirstOrDefault
            End If
            _entity = uow.GetProjectDocsDAO.GetNew()
                ProjectDoc.SortOrder = uow.GetProjectDocsDAO.GetNoTracking().Where(Function(m) m.ProjectId = ProjectDoc.ProjectId).Select(Function(a) a.SortOrder).DefaultIfEmpty(0).Max() + 1

            Else
                _entity = uow.GetProjectDocsDAO().GetById(ProjectDoc.Docid)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectDocsTranslator.TranslateBOToEntity(_entity, ProjectDoc, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            Else
                If Not _delentity Is Nothing Then
                    uow.GetProjectDocsDAO().DeleteObject(_delentity.ID)
                End If
            End If
        Else
            response.AddError("ProjectDoc not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function DeleteProjectDoc(ids As List(Of Integer)) As Response Implements IProjectService.DeleteProjectDoc
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetProjectDocsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteProjectDoc(bos As List(Of ProjectDocBO)) As Response Implements IProjectService.DeleteProjectDoc
        Return DeleteProjectDoc(bos.Select(Function(s) s.Docid).ToList())
    End Function


    'PaymentGroups
    Public Function GetProjectPaymentGroups(projectid As Integer) As GetResponse(Of ProjectPaymentGroupBO) Implements IProjectService.GetProjectPaymentGroups
        Dim response As New GetResponse(Of ProjectPaymentGroupBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPaymentGroupsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid).OrderByDescending(Function(m) m.Name)
        For Each _entity In entities
            Dim bo As New ProjectPaymentGroupBO
            Dim err = ProjectPaymentGroupTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        response.Values = response.Values.OrderBy(Function(m) m.Name).ToList
        Return response
    End Function
    Public Function GetProjectPaymentGroup(groupid As Integer) As GetResponse(Of ProjectPaymentGroupBO) Implements IProjectService.GetProjectPaymentGroup
        Dim response As New GetResponse(Of ProjectPaymentGroupBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPaymentGroupsDAO()
        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Id = groupid).FirstOrDefault

        Dim bo As New ProjectPaymentGroupBO
        Dim err = ProjectPaymentGroupTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetProjectPaymentGroupsForSelect(projectid As Integer) As GetResponse(Of IdNameBO) Implements IProjectService.GetProjectPaymentGroupsForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPaymentGroupsDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        response.Values = response.Values.OrderBy(Function(m) m.Display).ToList
        Return response
    End Function
    Public Function InsertUpdateProjectPaymentGroup(ProjectPaymentGroup As ProjectPaymentGroupBO) As Response Implements IProjectService.InsertUpdateProjectPaymentGroup
        Dim response As New Response
        If (ProjectPaymentGroup.ProjectId <= 0) Then
            response.AddError("ProjectID is verplicht")
        End If
        If (ProjectPaymentGroup.Name Is Nothing OrElse ProjectPaymentGroup.Name = "") Then
            response.AddError("Naam is verplicht")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As InvoicingPaymentGroup = Nothing
        If (ProjectPaymentGroup.Id = 0) Then
            _entity = uow.GetProjectPaymentGroupsDAO.GetNew()
        Else
            _entity = uow.GetProjectPaymentGroupsDAO().GetById(ProjectPaymentGroup.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectPaymentGroupTranslator.TranslateBOToEntity(_entity, ProjectPaymentGroup, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("ProjectPaymentGroup not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Sub LinkPaymentGroupToUnit(unitid As Integer, paymentgroupid As Integer) Implements IProjectService.LinkPaymentGroupToUnit
        Dim uow As New UnitOfWork()
        Dim _entity As Units = Nothing
        _entity = uow.GetUnitsDAO().GetById(unitid)
        If (_entity IsNot Nothing) Then
            _entity.PaymentGroupId = paymentgroupid
        End If
        uow.SaveChanges()

    End Sub
    Public Function DeleteProjectPaymentGroup(ids As List(Of Integer)) As Response Implements IProjectService.DeleteProjectPaymentGroup
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetProjectPaymentGroupsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteProjectPaymentGroup(bos As List(Of ProjectPaymentGroupBO)) As Response Implements IProjectService.DeleteProjectPaymentGroup
        Return DeleteProjectPaymentGroup(bos.Select(Function(s) s.Id).ToList())
    End Function

    'PaymentStagessq
    Public Function GetProjectPaymentStages(groupid As Integer) As GetResponse(Of ProjectPaymentStageBO) Implements IProjectService.GetProjectPaymentStages

    End Function
    Public Function GetProjectPaymentStage(stageid As Integer) As GetResponse(Of ProjectPaymentStageBO) Implements IProjectService.GetProjectPaymentStage
        Dim response As New GetResponse(Of ProjectPaymentStageBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPaymentStagesDAO()
        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Id = stageid).FirstOrDefault

        Dim bo As New ProjectPaymentStageBO
        Dim err = ProjectPaymentStageTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetProjectInvoicableUnits(projectid As Integer) As GetResponse(Of UnitWithStagesBO) Implements IProjectService.GetProjectInvoicableUnits
        Dim response As New GetResponse(Of UnitWithStagesBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim dao2 = uow.GetProjectPaymentStagesDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.UnitConstructionValue.Any(Function(l) l.InvoicingPaymentGroup.InvoicingPaymentStages.Any(Function(i) i.Invoicable = True)) AndAlso m.ClientAccountID > 0 AndAlso Not m.ClientAccount.DateDeedOfSale Is Nothing)
        For Each _entity In entities
            If _entity.Id = 138 Then
                Dim test As String = "test"
            End If

            Dim bo As New UnitWithStagesBO
            'Dim stages = _entity.InvoicingPaymentGroup.InvoicingPaymentStages.Where(Function(m) m.InvoicesDetails.Where(Function(i) i.UnitId = _entity.Id).Count = 0 AndAlso m.Invoicable = True)
            Dim stages = dao2.GetNoTracking.Where(Function(m) m.InvoicesDetails.Where(Function(i) i.UnitId = _entity.Id).Count = 0 AndAlso m.Invoicable = True AndAlso m.InvoicingPaymentGroup.UnitConstructionValue.Any(Function(l) l.UnitId = _entity.Id))
            If stages.Count > 0 Then
                Dim unitbo As New UnitBO
                For Each stage In stages
                    Dim stagebo As New ProjectPaymentStageBO
                    Dim err2 = ProjectPaymentStageTranslator.TranslateEntityToBO(stage, stagebo)
                    If err2 = ErrorCode.Success Then
                        bo.PaymentStages.Add(stagebo)
                    Else
                        response.AddError(err2.ToString())
                    End If
                Next
                Dim err = UnitTranslator.TranslateEntityToBO(_entity, unitbo)
                If err = ErrorCode.Success Then
                    bo.Unit = unitbo
                    response.AddValue(bo)
                Else
                    response.AddError(err.ToString())
                End If
            End If


        Next
        Return response
    End Function

    Public Function InsertUpdateProjectPaymentStage(ProjectPaymentStage As ProjectPaymentStageBO) As Response Implements IProjectService.InsertUpdateProjectPaymentStage
        Dim response As New Response
        If (ProjectPaymentStage.GroupId <= 0) Then
            response.AddError("GroupID is verplicht")
        End If
        If (ProjectPaymentStage.Name Is Nothing OrElse ProjectPaymentStage.Name = "") Then
            response.AddError("Naam is verplicht")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As InvoicingPaymentStages = Nothing
        If (ProjectPaymentStage.Id = 0) Then
            _entity = uow.GetProjectPaymentStagesDAO.GetNew()
        Else
            _entity = uow.GetProjectPaymentStagesDAO().GetById(ProjectPaymentStage.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ProjectPaymentStageTranslator.TranslateBOToEntity(_entity, ProjectPaymentStage, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("ProjectPaymentStage not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function UpdateProjectPaymentStageInvoicable(stageid As Integer, invoicable As Boolean) As Response Implements IProjectService.UpdateProjectPaymentStageInvoicable
        Dim response As New Response
        If (stageid <= 0) Then
            response.AddError("StageId is verplicht")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As InvoicingPaymentStages = Nothing
        _entity = uow.GetProjectPaymentStagesDAO().GetById(stageid)
        If (_entity IsNot Nothing) Then
            _entity.Invoicable = invoicable
        Else
            response.AddError("ProjectPaymentStage not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteProjectPaymentStage(ids As List(Of Integer)) As Response Implements IProjectService.DeleteProjectPaymentStage

    End Function
    Public Function DeleteProjectPaymentStage(bos As List(Of ProjectPaymentStageBO)) As Response Implements IProjectService.DeleteProjectPaymentStage

    End Function
    Public Function CheckProjectPaymentStageDocInUse(docid As Integer) As Boolean Implements IProjectService.CheckProjectPaymentStageDocInUse
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProjectPaymentStagesDAO()
        Dim int As Integer = dao.GetNoTracking.Where(Function(m) m.DocId = docid).Count
        If int = 0 Then
            Return False
        Else
            Return True
        End If
    End Function

    'INVOICING
    Public Function GetProjectInvoicableChangeOrders(projectid As Integer) As GetResponse(Of ChangeOrderBO) Implements IProjectService.GetProjectInvoicableChangeOrders
        Dim response As New GetResponse(Of ChangeOrderBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetChangeOrderDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ContractActivity.Contract.ProjectID = projectid AndAlso m.ChangeOrderDetail.Any(Function(i) i.Invoicable = True AndAlso i.Invoiced = False) AndAlso m.ClientAccountID > 0 AndAlso Not m.ClientAccount.DateDeedOfSale Is Nothing AndAlso m.ChangeOrderDetail.Where(Function(i) i.Invoiced = True).Count < m.ChangeOrderDetail.Count)
        For Each _entity In entities
            Dim bo As New ChangeOrderBO
            Dim err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        '
        '
        Return response
    End Function
    Public Function GetProjectInvoiceableLandValue(projectid As Integer) As GetResponse(Of UnitBO) Implements IProjectService.GetProjectInvoiceableLandValue
        'Dim response As New GetResponse(Of ChangeOrderBO)
        'Dim uow As New UnitOfWork(False)
        'Dim dao = uow.GetUnitsDAO()
        'Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectId = projectid AndAlso m.ClientAccountID > 0 AndAlso Not m.ClientAccount.DateDeedOfSale Is Nothing AndAlso m.ChangeOrderDetail.Where(Function(i) i.Invoiced = True).Count < m.ChangeOrderDetail.Count)
        'For Each _entity In entities
        '    Dim bo As New ChangeOrderBO
        '    Dim err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo)
        '    If err = ErrorCode.Success Then
        '        response.AddValue(bo)
        '    Else
        '        response.AddError(err.ToString())
        '    End If
        'Next
        ''
        ''
        'Return response
    End Function
    Public Function GetInvoicesByUnitIds(UnitIds As List(Of Integer)) As GetResponse(Of InvoiceBO) Implements IProjectService.GetInvoicesByUnitIds
        Dim response As New GetResponse(Of InvoiceBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInvoicesDAO()
        Dim entities = dao.GetNoTracking.Where(InvoicesQuery.GetUnitsQuery(UnitIds))
        For Each _entity In entities
            Dim bo As New InvoiceBO
            Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next

        Return response
    End Function
    Public Function GetProjectUtilityCost(projectid As Integer, clientid As Integer) As GetResponse(Of UtilityCostBO) Implements IProjectService.GetProjectUtilityCost
        Dim response As New GetResponse(Of UtilityCostBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractActivityDAO()
        Dim dao2 = uow.GetIncommingInvoicesDetailDAO()
        Dim udao = uow.GetUnitsDAO()
        Dim utilitydao = uow.GetUtilityPercentageDAO()
        Dim numberofunits = udao.GetNoTracking.Where(Function(m) m.ProjectId = projectid And (m.UnitTypes.GroupID = 1 Or m.UnitTypes.GroupID = 4)).Count
        Dim entities = dao.GetNoTracking.Where(Function(m) m.Contract.ProjectID = projectid And m.ActivityID = 280)
        'Alle contractlijnen die nutsaansluitingen zijn
        For Each _entity In entities
            Dim price As Decimal = 0
            For Each fact In dao2.GetNoTracking.Where(Function(m) m.ContractActivity.ActivityID = 280 And m.Type = IncommingInvoiceType.Contract And m.IncommingInvoices.ContractID = _entity.ContractID)
                price = price + fact.Price
            Next
            'price = dao2.GetNoTracking.Where(Function(m) m.ContractID = _entity.ContractID).Sum(Function(l) l.IncommingInvoiceDetail.Where(Function(s) s.ContractActivity.ActivityID = 280 And s.Type = IncommingInvoiceType.Contract).Count)
            'price = dao2.GetNoTracking.Where(Function(m) m.ContractID = _entity.ContractID).Sum(Function(l) l.IncommingInvoiceDetail.Where(Function(s) s.ContractActivity.ActivityID = 280 And s.Type = IncommingInvoiceType.Contract).Sum(Function(i) i.Price))

            If Not price > _entity.Price Then
                Dim percentage As Decimal = 0
                Dim percentageother As Decimal = 0
                If (utilitydao.GetNoTracking.Where(Function(m) m.ContractId = _entity.ContractID And m.ClientAccountId = clientid).Count > 0) Then percentage = utilitydao.GetNoTracking.Where(Function(m) m.ContractId = _entity.ContractID And m.ClientAccountId = clientid).Sum(Function(s) s.Percentage)
                If (utilitydao.GetNoTracking.Where(Function(m) m.ContractId = _entity.ContractID).Count > 0) Then percentageother = utilitydao.GetNoTracking.Where(Function(m) m.ContractId = _entity.ContractID).Sum(Function(s) s.Percentage)
                Dim otherunits = utilitydao.GetNoTracking.Where(Function(m) m.ContractId = _entity.ContractID).Count
                Dim clientpercentage As Decimal
                If percentage = 0 Then
                    clientpercentage = (100 - percentageother) / (numberofunits - otherunits)
                Else
                    clientpercentage = percentage
                End If

                Dim bo As New UtilityCostBO
                bo.ProjectId = projectid
                bo.Price = _entity.Price - price
                bo.Description = "- NOG TE FACTUREREN -"
                bo.CompanyName = _entity.Contract.CompanyInfo.BedrijfsNaam
                bo.Percentage = clientpercentage
                response.AddValue(bo)

            End If
        Next
        Dim dao3 = uow.GetIncommingInvoicesDetailDAO()
        Dim entities3 = dao3.GetNoTracking.Where(Function(m) m.IncommingInvoices.ProjectID = projectid And (m.ActID = 280 Or m.ContractActivity.ActivityID = 280))
        For Each _entity In entities3
            Dim percentage As Decimal = 0
            Dim percentageother As Decimal = 0
            If (utilitydao.GetNoTracking.Where(Function(m) m.IncommingInvoiceDetailId = _entity.ID And m.ClientAccountId = clientid).Count > 0) Then percentage = utilitydao.GetNoTracking.Where(Function(m) m.IncommingInvoiceDetailId = _entity.ID And m.ClientAccountId = clientid).Sum(Function(s) s.Percentage)
            If (utilitydao.GetNoTracking.Where(Function(m) m.IncommingInvoiceDetailId = _entity.ID).Count > 0) Then percentageother = utilitydao.GetNoTracking.Where(Function(m) m.IncommingInvoiceDetailId = _entity.ID).Sum(Function(s) s.Percentage)
            Dim otherunits = utilitydao.GetNoTracking.Where(Function(m) m.IncommingInvoiceDetailId = _entity.ID).Count
            Dim clientpercentage As Decimal
            If percentage = 0 Then
                clientpercentage = (100 - percentageother) / (numberofunits - otherunits)
            Else
                clientpercentage = percentage
            End If
            Dim bo As New UtilityCostBO
            bo.ProjectId = projectid
            bo.Price = _entity.Price
            bo.Description = _entity.Description
            If _entity.IncommingInvoices.CompanyID Is Nothing Then
                bo.CompanyName = _entity.IncommingInvoices.Contract.CompanyInfo.BedrijfsNaam
            Else
                bo.CompanyName = _entity.IncommingInvoices.CompanyInfo.BedrijfsNaam
            End If
            bo.Percentage = clientpercentage
            response.AddValue(bo)
        Next
        Return response
    End Function

    Public Function InsertUpdateProjectInvoice(Invoice As InvoiceBO) As Response Implements IProjectService.InsertUpdateProjectInvoice
        Dim response As New Response


        If (Invoice.Filename Is Nothing Or Invoice.Filename = "") Then
            response.AddError("Bestandsnaam is verplicht")
        End If
        If Invoice.Rows.Count = 0 Then
            response.AddError("Er is minstens één detailrij nodig")
        End If


        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As Invoices = Nothing
        If (Invoice.Id = 0) Then
            _entity = uow.GetInvoicesDAO.GetNew()
        Else
            _entity = uow.GetInvoicesDAO().GetById(Invoice.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = InvoiceTranslator.TranslateBOToEntity(_entity, Invoice, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Invoice not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function InsertUpdateProjectInvoices(Invoices As List(Of InvoiceBO)) As Response Implements IProjectService.InsertUpdateProjectInvoices
        Dim response As New Response
        For Each invoice In Invoices
            If (invoice.Filename Is Nothing Or invoice.Filename = "") Then
                response.AddError("Bestandsnaam is verplicht")
            End If
            If invoice.Rows.Count = 0 Then
                response.AddError("Er is minstens één detailrij nodig")
            End If
            If (Not response.Success) Then Return response
            Dim uow As New UnitOfWork()
            Dim _entity As Invoices = Nothing
            If (invoice.Id = 0) Then
                _entity = uow.GetInvoicesDAO.GetNew()
            Else
                _entity = uow.GetInvoicesDAO().GetById(invoice.Id)
            End If
            If (_entity IsNot Nothing) Then
                Dim err = InvoiceTranslator.TranslateBOToEntity(_entity, invoice, uow)
                If (err <> ErrorCode.Success) Then
                    response.AddError(err.ToString())
                End If
            Else
                response.AddError("Invoice not found")
            End If
            response.AddError(uow.SaveChanges())
        Next
        Return response
    End Function

    'Contracts
    Public Function GetProjectContracts(projectid As Integer) As GetResponse(Of ContractBO) Implements IProjectService.GetProjectContracts
        Dim response As New GetResponse(Of ContractBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New ContractBO
            Dim err = ContractTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetProjectContractsForSelect(projectid As Integer) As GetResponse(Of IdNameBO) Implements IProjectService.GetProjectContractsForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetProjectContractActivitiesForSelect(projectid As Integer) As GetResponse(Of IdNameBO) Implements IProjectService.GetProjectContractActivitiesForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid)
        For Each _entity In entities
            For Each act As ContractActivity In _entity.ContractActivity
                response.AddValue(act.GetIdName())
            Next

        Next
        Return response
    End Function
    Public Function InsertUpdateProjectContract(Contract As ContractBO) As Response Implements IProjectService.InsertUpdateProjectContract
        Dim response As New Response


        If (Contract.Company.ID = 0) Then
            response.AddError("Bedrijf selecteren is verplicht")
        End If
        If Contract.Activities.Count = 0 Then
            response.AddError("Er is minstens één lot nodig")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As Contract = Nothing
        If (Contract.Id = 0) Then
            _entity = uow.GetContractDAO.GetNew()
        Else
            _entity = uow.GetContractDAO().GetById(Contract.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ContractTranslator.TranslateBOToEntity(_entity, Contract, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Contract not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function GetContract(contractid As Integer) As GetResponse(Of ContractBO) Implements IProjectService.GetContract
        Dim response As New GetResponse(Of ContractBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractDAO()
        Dim _entity = dao.GetById(contractid)
        Dim bo As New ContractBO
        Dim err = ContractTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
            Return response
        Else
            response.AddError(err.ToString())
        End If

    End Function

    Public Function DeleteContracts(ids As List(Of Integer)) As Response Implements IProjectService.DeleteContracts
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetContractDAO.DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function GetContractChangeOrdersForSelect(contractid As Integer) As GetResponse(Of IdNameBO) Implements IProjectService.GetContractChangeOrdersForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetChangeOrderDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ContractActivity.ContractID = contractid)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function

    Public Function GetProjectContractsWithoutInvoices(projectid As Integer, Optional activityid As Integer = 0) As GetResponse(Of ContractBO) Implements IProjectService.GetProjectContractsWithoutInvoices
        Dim response As New GetResponse(Of ContractBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractDAO()
        Dim entities
        If activityid = 0 Then
            entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid AndAlso m.IncommingInvoices.Count = 0)
        Else
            entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid AndAlso m.ContractActivity.Where(Function(s) s.ActivityID = activityid).Count > 0 AndAlso m.IncommingInvoices.Where(Function(l) l.IncommingInvoiceDetail.Where(Function(i) i.ContractActivity.ActivityID = activityid).Count > 0).Count = 0)
        End If

        For Each _entity In entities
            Dim bo As New ContractBO
            Dim err = ContractTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetContractActivityPrice(contractactid As Integer) As Decimal Implements IProjectService.GetContractActivityPrice
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractActivityDAO()
        Return dao.GetById(contractactid).Price
    End Function
    Public Function GetProjectContractActivitiesByActivityId(projectid As Integer, activityid As Integer) As GetResponse(Of ContractActivityBO) Implements IProjectService.GetProjectContractActivitiesByActivityId
        Dim response As New GetResponse(Of ContractActivityBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetContractActivityDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.Contract.ProjectID = projectid And m.ActivityID = activityid)
        For Each _entity In entities
            Dim bo As New ContractActivityBO
            Dim err = ContractActivityTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    'Budget
    Public Function GetProjectBudget(projectid As Integer) As GetResponse(Of BudgetActivityBO) Implements IProjectService.GetProjectBudget
        Dim response As New GetResponse(Of BudgetActivityBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetBudgetDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New BudgetActivityBO
            Dim err = BudgetTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function InsertUpdateProjectBudgetActivity(BudgetActivity As BudgetActivityBO) As Response Implements IProjectService.InsertUpdateProjectBudgetActivity
        Dim response As New Response
        If (BudgetActivity.ProjectId = 0) Then
            response.AddError("Project is niet geselecteerd")
        End If
        If BudgetActivity.Activity.ID = 0 Then
            response.AddError("Er is geen activiteit geselecteerd")
        End If

        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ProjectBudget = Nothing
        If (BudgetActivity.Id = 0) Then
            _entity = uow.GetBudgetDAO.GetNew()
        Else
            _entity = uow.GetBudgetDAO().GetById(BudgetActivity.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = BudgetTranslator.TranslateBOToEntity(_entity, BudgetActivity, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("BudgetActivity not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function InsertUpdateProjectBudgetActivities(BudgetActivities As List(Of BudgetActivityBO), projectid As Integer) As Response Implements IProjectService.InsertUpdateProjectBudgetActivities
        Dim response As New Response
        For Each BudgetActivity In BudgetActivities

            If (BudgetActivity.ProjectId = 0) Then
                response.AddError("Project is niet geselecteerd")
            End If
            If BudgetActivity.Activity.ID = 0 Then
                response.AddError("Er is geen activiteit geselecteerd")
            End If

            If (Not response.Success) Then Return response
            Dim uow As New UnitOfWork()
            Dim _entity As ProjectBudget = Nothing
            If (BudgetActivity.Id = 0) Then
                _entity = uow.GetBudgetDAO.GetNew()
            Else
                _entity = uow.GetBudgetDAO().GetById(BudgetActivity.Id)
            End If
            If (_entity IsNot Nothing) Then
                Dim err = BudgetTranslator.TranslateBOToEntity(_entity, BudgetActivity, uow)
                If (err <> ErrorCode.Success) Then
                    response.AddError(err.ToString())
                End If
            Else
                response.AddError("BudgetActivity not found")
            End If
            response.AddError(uow.SaveChanges())
        Next
        'Verwijderen oude loten
        Dim uow2 As New UnitOfWork(False)
        Dim dao = uow2.GetBudgetDAO()
        Dim _entities = dao.GetNoTracking.Where(Function(m) m.ProjectID = projectid)
        Dim delList As New List(Of ProjectBudget)
        For Each x In _entities
            If (Not BudgetActivities.Any(Function(f) f.Id = x.ID) AndAlso Not BudgetActivities.Any(Function(f) f.Id = 0)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            uow2.GetBudgetDAO().DeleteObject(x.ID)
        Next
        response.Messages.AddRange(uow2.SaveChanges())
        Return response

    End Function
    'ChangeOrders
    Public Function GetProjectChangeOrders(projectid As Integer) As GetResponse(Of ChangeOrderBO) Implements IProjectService.GetProjectChangeOrders
        Dim response As New GetResponse(Of ChangeOrderBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetChangeOrderDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ContractActivity.Contract.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New ChangeOrderBO
            Dim err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientChangeOrders(clientaccountid As Integer) As GetResponse(Of ChangeOrderBO) Implements IProjectService.GetClientChangeOrders
        Dim response As New GetResponse(Of ChangeOrderBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetChangeOrderDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ClientAccountID = clientaccountid)
        For Each _entity In entities
            Dim bo As New ChangeOrderBO
            Dim err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetChangeOrder(changeorderid As Integer) As GetResponse(Of ChangeOrderBO) Implements IProjectService.GetChangeOrder
        Dim response As New GetResponse(Of ChangeOrderBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetChangeOrderDAO()
        Dim _entity = dao.GetById(changeorderid)
        Dim bo As New ChangeOrderBO
            Dim err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
            Return response
        Else
            response.AddError(err.ToString())
        End If

    End Function
    Public Function InsertUpdateProjectChangeOrder(changeorder As ChangeOrderBO) As Response Implements IProjectService.InsertUpdateProjectChangeOrder
        Dim response As New Response
        If (changeorder.ClientAccountID = 0) Then
            response.AddError("ClientAccount is niet geselecteerd")
        End If
        If changeorder.ContractActivityID = 0 Then
            response.AddError("Er is geen activiteit geselecteerd")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ChangeOrder = Nothing
        If (changeorder.Id = 0) Then
            _entity = uow.GetChangeOrderDAO.GetNew()
        Else
            _entity = uow.GetChangeOrderDAO().GetById(changeorder.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ChangeOrderTranslator.TranslateBOToEntity(_entity, changeorder, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Change Order not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function InsertUpdateProjectChangeOrders(changeorders As List(Of ChangeOrderBO), projectid As Integer) As Response Implements IProjectService.InsertUpdateProjectChangeOrders

    End Function
    Public Function DeleteChangeOrders(ids As List(Of Integer)) As Response Implements IProjectService.DeleteChangeOrders
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetChangeOrderDAO.DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function UpdateProjectChangeOrderInvoicable(COid As Integer, invoicable As Boolean) As Response Implements IProjectService.UpdateProjectChangeOrderInvoicable
        Dim response As New Response
        If (COid <= 0) Then
            response.AddError("COid is verplicht")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetChangeOrderDetailDAO()
        Dim entities = dao.GetNormal.Where(Function(m) m.ChangeOrderID = COid)
        For Each _entity In entities
            _entity.Invoicable = invoicable

        Next
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function UpdateProjectChangeOrderDetailInvoicable(CODetailid As Integer, invoicable As Boolean) As Response Implements IProjectService.UpdateProjectChangeOrderDetailInvoicable
        Dim response As New Response
        If (CODetailid <= 0) Then
            response.AddError("CODetailid is verplicht")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ChangeOrderDetail = Nothing
        _entity = uow.GetChangeOrderDetailDAO().GetById(CODetailid)
        If (_entity IsNot Nothing) Then
            _entity.Invoicable = invoicable
        Else
            response.AddError("ChangeOrderDetail not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function SetChangeOrderDetailInvoiced(codid As Integer) As Response Implements IProjectService.SetChangeOrderDetailInvoiced
        Dim response As New Response
        If (codid = 0) Then
            response.AddError("Geen OrderDetail ingegeven")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetChangeOrderDetailDAO()
        Dim _entity = dao.GetById(codid)
        If (_entity IsNot Nothing) Then
            _entity.Invoiced = True
        Else
            response.AddError("Change Order not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function

    'Incomming Invoices
    Public Function GetIncommingInvoice(invoiceid As Integer) As GetResponse(Of IncommingInvoiceBO) Implements IProjectService.GetIncommingInvoice
        Dim response As New GetResponse(Of IncommingInvoiceBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetIncommingInvoicesDAO()
        Dim _entity = dao.GetById(invoiceid)
        Dim bo As New IncommingInvoiceBO
        Dim err = IncommingInvoiceTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
            Return response
        Else
            response.AddError(err.ToString())
        End If

    End Function
    Public Function InsertUpdateProjectIncommingInvoice(invoice As IncommingInvoiceBO) As Response Implements IProjectService.InsertUpdateProjectIncommingInvoice
        Dim response As New Response
        If (invoice.ContractID = 0 AndAlso invoice.CompanyId = 0) Then
            response.AddError("De leverancier is niet geselecteerd")
        End If

        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As IncommingInvoices = Nothing
        If (invoice.Id = 0) Then
            _entity = uow.GetIncommingInvoicesDAO.GetNew()
        Else
            _entity = uow.GetIncommingInvoicesDAO().GetById(invoice.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = IncommingInvoiceTranslator.TranslateBOToEntity(_entity, invoice, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Incomming Invoice not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Public Function GetProjectIncommingInvoicesForRecalculation(projectid As Integer) As GetResponse(Of IncommingInvoiceActivityBO) Implements IProjectService.GetProjectIncommingInvoicesForRecalculation
        Dim response As New GetResponse(Of IncommingInvoiceActivityBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetIncommingInvoicesDetailDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.IncommingInvoices.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New IncommingInvoiceActivityBO
            Dim err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetProjectIncommingInvoicesByActivity(projectid As Integer, activityid As Integer) As GetResponse(Of IncommingInvoiceActivityBO) Implements IProjectService.GetProjectIncommingInvoicesByActivity
        Dim response As New GetResponse(Of IncommingInvoiceActivityBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetIncommingInvoicesDetailDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ActID = activityid And m.IncommingInvoices.ProjectID = projectid Or m.ContractActivity.ActivityID = activityid And m.IncommingInvoices.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New IncommingInvoiceActivityBO
            Dim err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Public Function DeleteIncommingInvoices(ids As List(Of Integer)) As Response Implements IProjectService.DeleteIncommingInvoices
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetIncommingInvoicesDAO.DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function


    'Insurances
    Public Function GetProjectInsurances(projectid As Integer) As GetResponse(Of InsuranceBO) Implements IProjectService.GetProjectInsurances
        Dim response As New GetResponse(Of InsuranceBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInsuranceDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.ContractActivity.Contract.ProjectID = projectid)
        For Each _entity In entities
            Dim bo As New InsuranceBO
            Dim err = InsuranceTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function



    'Helpers

    Public Function GenerateSlug(phrase As String) As String
        Dim str As String = RemoveAccent(phrase).ToLower()
        str = Regex.Replace(str, "[^a-z0-9\s-]", "")
        str = Regex.Replace(str, "\s+", " ").Trim()
        str.Substring(0, If(str.Length <= 45, str.Length, 45)).Trim()
        str = Regex.Replace(str, "\s", "-")
        Return str
    End Function
    Public Function RemoveAccent(txt As String) As String
        Dim bytes As Byte() = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt)
        Return System.Text.Encoding.ASCII.GetString(bytes)

    End Function
    Public Function AddWorkDays([date] As Date, workingDays As Integer, BWDS As Array, VDS As Array) As Date

        Dim direction As Integer = If(workingDays < 0, -1, 1)
        Dim newDate As DateTime = [date]
        ' If a working day count of Zero is passed, return the date passed
        If workingDays = 0 Then

            newDate = [date]
        Else
            While workingDays <> 0
                If newDate.DayOfWeek <> DayOfWeek.Saturday AndAlso newDate.DayOfWeek <> DayOfWeek.Sunday AndAlso Array.IndexOf(BWDS, newDate) < 0 AndAlso Array.IndexOf(VDS, newDate) < 0 Then
                    workingDays -= 1
                Else
                    Dim dag As Date = newDate
                End If
                ' if the original return date falls on a weekend or holiday, this will take it to the previous / next workday, but the "if" statement keeps it from going a day too far.

                If workingDays <> 0 Then
                    newDate = newDate.AddDays(1)
                End If
            End While
        End If
        Return newDate
    End Function
    Public Function BusinessDaysUntil(start As DateTime, [end] As DateTime, VDS As Array) As Integer
        Dim tld As Integer = CInt(([end].Date - start.Date).TotalDays)
        Dim direction As Integer = If(tld < 0, -1, 1)
        Dim newDate As DateTime = start.AddDays(1)
        Dim workingdays As Integer = 0
        ' If a working day count of Zero is passed, return the date passed
        If tld = 0 Then
            newDate = start
        ElseIf tld < 0 Then
            newDate = start
        Else
            While tld <> 0
                If newDate.DayOfWeek <> DayOfWeek.Saturday AndAlso newDate.DayOfWeek <> DayOfWeek.Sunday AndAlso Array.IndexOf(VDS, newDate.Date) < 0 Then

                    workingdays += 1
                Else
                    'Dim dag As Date = newDate
                End If
                ' if the original return date falls on a weekend or holiday, this will take it to the previous / next workday, but the "if" statement keeps it from going a day too far.
                tld -= 1
                If tld <> 0 Then
                    newDate = newDate.AddDays(1)
                End If
            End While
        End If
        Return workingdays




        'Dim tld As Integer = CInt(([end] - start).TotalDays) + 1
        ''including end day
        'Dim not_buss_day As Integer = 2 * (tld / 7)
        ''Saturday and Sunday
        'Dim rest As Integer = tld Mod 7
        ''rest.
        'If rest > 0 Then
        '    Dim tmp As Integer = CInt(start.DayOfWeek) - 1 + rest
        '    If tmp = 6 OrElse start.DayOfWeek = DayOfWeek.Sunday Then
        '        not_buss_day += 1
        '    ElseIf tmp > 6 Then
        '        not_buss_day += 2
        '    End If
        'End If

        'For Each vacationday As Date In VDS
        '    Dim bh As Date = vacationday.[Date]
        '    If Not (bh.DayOfWeek = DayOfWeek.Saturday OrElse bh.DayOfWeek = DayOfWeek.Sunday) AndAlso (start <= bh AndAlso bh <= [end]) Then
        '        not_buss_day += 1
        '    End If
        'Next
        'Return tld - not_buss_day
    End Function

    Public Function COPYIDS() As Response Implements IProjectService.Copyids
        Dim response As New Response
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetInvoicesDetailsDAO()
        Dim dao2 = uow.GetUnitConstructionValuesDAO()
        Dim entities = dao.GetNormal()
        For Each _entity In entities
            Dim ent2 = dao2.GetNoTracking.Where(Function(m) m.UnitId = _entity.UnitId And m.PaymentGroupId = _entity.InvoicingPaymentStages.GroupId).FirstOrDefault()
            _entity.ConstructionValueId = ent2.Id
        Next
        response.AddError(uow.SaveChanges())
    End Function



End Class
