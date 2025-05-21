Imports BO
Imports Facade
Imports DAL
Imports System.Text.RegularExpressions
Public Class UnitService
    Implements IUnitService
    Public Function GetUnitById(Id As Integer) As GetResponse(Of UnitBO) Implements IUnitService.GetUnitById
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim _entity = dao.GetById(Id)
        Dim unit As New UnitBO

        Dim err = UnitTranslator.TranslateEntityToBO(_entity, unit)
        If err = ErrorCode.Success Then
            response.Value = unit
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetUnitsByProjectId(ProjectId As Integer) As GetResponse(Of UnitBO) Implements IUnitService.GetUnitsByProjectId
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.TypeId <> 0)
        For Each _entity In entities
            Dim bo As New UnitBO()
            Dim err = UnitTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetUnitsByProjectId(ProjectId As Integer, UnitTypeId As Integer) As GetResponse(Of UnitBO) Implements IUnitService.GetUnitsByProjectId
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.TypeId = UnitTypeId)
        For Each _entity In entities
            Dim bo As New UnitBO()
            Dim err = UnitTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetUnitsWithDetailsByProjectId(ProjectId As Integer) As GetResponse(Of UnitWithDetailsBO) Implements IUnitService.GetUnitsWithDetailsByProjectId
        Dim response As New GetResponse(Of UnitWithDetailsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId)
        For Each _entity In entities
            Dim bo As New UnitWithDetailsBO()
            Dim err = UnitTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                For Each room In _entity.UnitRooms
                    Dim boroom As New RoomBO
                    err = UnitRoomTranslator.TranslateEntityToBO(room, boroom)
                    If err = ErrorCode.Success Then
                        bo.Rooms.Add(boroom)
                    Else
                        response.AddError(err.ToString())
                    End If
                Next
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        response.Values = response.Values.OrderBy(Function(m) m.Name, New AlphanumComparator).ThenBy(Function(m) m.Level, New AlphanumComparator).ToList
        Return response
    End Function
    Public Function GetUnitsByAccountId(AccountId As Integer) As GetResponse(Of UnitBO) Implements IUnitService.GetUnitsByAccountId
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountID = AccountId)
        For Each _entity In entities
            Dim bo As New UnitBO()
            Dim err = UnitTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetGroupedUnitsByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsBO) Implements IUnitService.GetGroupedUnitsByProjectId
        Dim response As New GetResponse(Of GroupUnitsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.LinkedUnit_linkedunit Is Nothing).OrderBy(Function(x) x.Name).GroupBy(Function(x) x.TypeId)

        For Each _entity In entities

            Dim bo As New GroupUnitsBO()
            bo.Id = _entity.Key
            For Each item In _entity
                Dim boUnit As New UnitBO()
                Dim err = UnitTranslator.TranslateEntityToBO(item, boUnit)
                If err = ErrorCode.Success Then
                    bo.Units.Add(boUnit)
                Else
                    response.AddError(err.ToString())
                End If
            Next
            bo.Units = bo.Units.OrderBy(Function(m) m.Level).ThenBy(Function(m) m.Name, New AlphanumComparator).ToList
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetGroupedUnitsForSaleByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsWithAttachedUnitsBO) Implements IUnitService.GetGroupedUnitsForSaleByProjectId
        Dim response As New GetResponse(Of GroupUnitsWithAttachedUnitsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.ClientAccountID Is Nothing AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing).OrderBy(Function(x) x.Name).GroupBy(Function(x) x.TypeId)

        For Each _entity In entities

            Dim bo As New GroupUnitsWithAttachedUnitsBO()
            bo.Id = _entity.Key
            For Each item In _entity
                Dim boUnit As New UnitWithAttachedUnitsBO()
                Dim err = UnitTranslator.TranslateEntityToBO(item, boUnit.Unit)
                For Each attachedunit In item.AttachedUnit_Unit
                    Dim boattachedunit As New UnitBO
                    err = UnitTranslator.TranslateEntityToBO(attachedunit, boattachedunit)
                    If err = ErrorCode.Success Then
                        boUnit.AttachedUnits.Add(boattachedunit)
                    Else
                        response.AddError(err.ToString())
                    End If
                    For Each attachedunit2 In attachedunit.AttachedUnit_Unit
                        Dim boattachedunit2 As New UnitBO
                        err = UnitTranslator.TranslateEntityToBO(attachedunit2, boattachedunit2)
                        If err = ErrorCode.Success Then
                            boUnit.AttachedUnits.Add(boattachedunit2)
                        Else
                            response.AddError(err.ToString())
                        End If
                        For Each attachedunit3 In attachedunit2.AttachedUnit_Unit
                            Dim boattachedunit3 As New UnitBO
                            err = UnitTranslator.TranslateEntityToBO(attachedunit3, boattachedunit3)
                            If err = ErrorCode.Success Then
                                boUnit.AttachedUnits.Add(boattachedunit3)
                            Else
                                response.AddError(err.ToString())
                            End If

                        Next
                    Next
                Next
                If err = ErrorCode.Success Then
                    bo.Units.Add(boUnit)
                Else
                    response.AddError(err.ToString())
                End If

            Next
            bo.Units = bo.Units.OrderBy(Function(m) m.Unit.Level).ThenBy(Function(m) m.Unit.Name, New AlphanumComparator).ToList
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetGroupedUnitsForSaleWithDetailsByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsWithAttachedUnitsWithDetailsBO) Implements IUnitService.GetGroupedUnitsForSaleWithDetailsByProjectId
        Dim response As New GetResponse(Of GroupUnitsWithAttachedUnitsWithDetailsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.ClientAccountID Is Nothing AndAlso m.AttachedUnit_attachedunit Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing).OrderBy(Function(x) x.Name).GroupBy(Function(x) x.TypeId)

        For Each _entity In entities

            Dim bo As New GroupUnitsWithAttachedUnitsWithDetailsBO()
            bo.Id = _entity.Key
            For Each item In _entity
                Dim boUnit As New UnitWithAttachedUnitsWithDetailsBO()
                Dim err = UnitTranslator.TranslateEntityToBO(item, boUnit.Unit)
                For Each room In item.UnitRooms
                    Dim boroom As New RoomBO
                    err = UnitRoomTranslator.TranslateEntityToBO(room, boroom)
                    If err = ErrorCode.Success Then
                        boUnit.Unit.Rooms.Add(boroom)
                    Else
                        response.AddError(err.ToString())
                    End If
                Next
                For Each attachedunit In item.AttachedUnit_Unit
                    Dim boattachedunit As New UnitWithDetailsBO
                    err = UnitTranslator.TranslateEntityToBO(attachedunit, boattachedunit)
                    If err = ErrorCode.Success Then
                        boUnit.AttachedUnits.Add(boattachedunit)
                    Else
                        response.AddError(err.ToString())
                    End If
                    For Each room In attachedunit.UnitRooms
                        Dim boroom As New RoomBO
                        err = UnitRoomTranslator.TranslateEntityToBO(room, boroom)
                        If err = ErrorCode.Success Then
                            boattachedunit.Rooms.Add(boroom)
                        Else
                            response.AddError(err.ToString())
                        End If
                    Next
                    For Each attachedunit2 In attachedunit.AttachedUnit_Unit
                        Dim boattachedunit2 As New UnitWithDetailsBO
                        err = UnitTranslator.TranslateEntityToBO(attachedunit2, boattachedunit2)
                        If err = ErrorCode.Success Then
                            boUnit.AttachedUnits.Add(boattachedunit2)
                        Else
                            response.AddError(err.ToString())
                        End If
                        For Each room In attachedunit2.UnitRooms
                            Dim boroom As New RoomBO
                            err = UnitRoomTranslator.TranslateEntityToBO(room, boroom)
                            If err = ErrorCode.Success Then
                                boattachedunit2.Rooms.Add(boroom)
                            Else
                                response.AddError(err.ToString())
                            End If
                        Next
                        For Each attachedunit3 In attachedunit2.AttachedUnit_Unit
                            Dim boattachedunit3 As New UnitWithDetailsBO
                            err = UnitTranslator.TranslateEntityToBO(attachedunit3, boattachedunit3)
                            If err = ErrorCode.Success Then
                                boUnit.AttachedUnits.Add(boattachedunit3)
                            Else
                                response.AddError(err.ToString())
                            End If
                            For Each room In attachedunit3.UnitRooms
                                Dim boroom As New RoomBO
                                err = UnitRoomTranslator.TranslateEntityToBO(room, boroom)
                                If err = ErrorCode.Success Then
                                    boattachedunit3.Rooms.Add(boroom)
                                Else
                                    response.AddError(err.ToString())
                                End If
                            Next

                        Next
                    Next
                Next
                If err = ErrorCode.Success Then
                    bo.Units.Add(boUnit)
                Else
                    response.AddError(err.ToString())
                End If

            Next
            bo.Units = bo.Units.OrderBy(Function(m) m.Unit.Level).ThenBy(Function(m) m.Unit.Name, New AlphanumComparator).ToList
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetGroupedUnitsByAccountId(AccountId As Integer) As GetResponse(Of GroupUnitsBO) Implements IUnitService.GetGroupedUnitsByAccountId
        Dim response As New GetResponse(Of GroupUnitsBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountID = AccountId).OrderBy(Function(x) x.Name).GroupBy(Function(x) x.TypeId)
        For Each _entity In entities

            Dim bo As New GroupUnitsBO()
            bo.Id = _entity.Key
            For Each item In _entity
                Dim boUnit As New UnitBO()
                Dim err = UnitTranslator.TranslateEntityToBO(item, boUnit)
                If err = ErrorCode.Success Then
                    bo.Units.Add(boUnit)
                Else
                    response.AddError(err.ToString())
                End If
            Next
            bo.Units = bo.Units.OrderBy(Function(m) m.Name, New AlphanumComparator).ToList
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetAvailableUnitsByProjectId(ProjectId As Integer) As GetResponse(Of IdNameBO) Implements IUnitService.GetAvailableUnitsByProjectId
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.ClientAccountID Is Nothing AndAlso m.LinkedUnit_linkedunit Is Nothing)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetUnitsByProjectIdForSelect(ProjectId As Integer, WithLinked As Boolean) As GetResponse(Of IdNameBO) Implements IUnitService.GetUnitsByProjectIdForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities
        If WithLinked = True Then
            entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId)
        Else
            entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.UnitTypes.ID <> 11)
        End If

        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetUnitsByProjectIdForSelect(ProjectId As Integer, UnitTypeId As Integer) As GetResponse(Of IdNameBO) Implements IUnitService.GetUnitsByProjectIdForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities
        entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.TypeId = UnitTypeId)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetUnitsByProjectIdForSelectAttachedUnit(ProjectId As Integer) As GetResponse(Of IdNameBO) Implements IUnitService.GetUnitsByProjectIdForSelectAttachedUnit
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities
        entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.LinkedUnit_linkedunit Is Nothing)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetUnitsByProjectIdForSelectAttachedUnit(ProjectId As Integer, UnitId As Integer) As GetResponse(Of IdNameBO) Implements IUnitService.GetUnitsByProjectIdForSelectAttachedUnit
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities
        entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = ProjectId AndAlso m.LinkedUnit_linkedunit Is Nothing AndAlso m.Id <> UnitId)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function

    Public Function GetUniqueUnitTypesInProjectByProjectId(projectid As Integer) As GetResponse(Of UnitTypeBO) Implements IUnitService.GetUniqueUnitTypesInProjectByProjectId
        Dim response As New GetResponse(Of UnitTypeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ProjectId = projectid).Select(Function(m) m.UnitTypes).Distinct()
        For Each _entity In entities
            Dim bo As New UnitTypeBO()
            Dim err = UnitTypeTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientUnitsWithStages(ClientAcccountId As Integer) As GetResponse(Of UnitWithStagesBO) Implements IUnitService.GetClientUnitsWithStages
        Dim response As New GetResponse(Of UnitWithStagesBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountID = ClientAcccountId)
        For Each _entity In entities
            Dim unitwithstages As New UnitWithStagesBO
            Dim bo As New UnitBO()
            Dim err = UnitTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                unitwithstages.Unit = bo
                If Not _entity.InvoicingPaymentGroup Is Nothing Then

                    If Not _entity.InvoicingPaymentGroup.InvoicingPaymentStages Is Nothing Then
                        For Each item In _entity.InvoicingPaymentGroup.InvoicingPaymentStages
                            Dim stage As New ProjectPaymentStageBO
                            Dim err2 = ProjectPaymentStageTranslator.TranslateEntityToBO(item, stage)
                            If err2 = ErrorCode.Success Then
                                unitwithstages.PaymentStages.Add(stage)
                            Else
                                response.AddError(err2.ToString())
                            End If
                        Next

                    End If
                End If
                response.AddValue(unitwithstages)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Public Function InsertUpdateUnit(bo As UnitBO) As Response Implements IUnitService.InsertUpdateUnit
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitsDAO()
        Dim _entity As Units = Nothing

        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = UnitTranslator.TranslateBOToEntity(_entity, bo, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If

        Else
            response.AddError("unit not found")
        End If
        response.AddError(uow.SaveChanges())
        response.InsertedId = _entity.Id
        Return response
    End Function
    Public Function InsertUpdateUnitToClientAccount(bo As UnitBO) As Response Implements IUnitService.InsertUpdateUnitToClientAccount
        Dim response As New Response()
        If (bo.ClientAccountId = 0) Then
            response.AddError("No ClientAccount selected")
        ElseIf (bo.Id = 0) Then
            response.AddError("No Unit selected")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitsDAO()
        Dim _entity As Units = Nothing
        _entity = dao.GetById(bo.Id)

        If (_entity IsNot Nothing) Then
            _entity.ClientAccountID = bo.ClientAccountId
            _entity.ConstructionValueSold = bo.ConstructionValueSold
            _entity.LandValueSold = bo.LandValueSold
        Else
            response.AddError("unit not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Public Function DeleteUnit(ids As List(Of Integer)) As Response Implements IUnitService.DeleteUnit
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitsDAO()
        For Each id In ids
            Dim entities = dao.GetNormal().Where(Function(m) m.AttachedUnitId = id)
            For Each _entity In entities
                _entity.AttachedUnitId = Nothing
            Next
            Dim entities1 = dao.GetNormal().Where(Function(m) m.LinkedUnitId = id)
            For Each _entity In entities1
                _entity.LinkedUnitId = Nothing
            Next
            response.Messages.AddRange(uow.SaveChanges())
            dao.DeleteObject(id)
            Next
            response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteUnitFromClientAccountByUnitId(ids As List(Of Integer)) As Response Implements IUnitService.DeleteUnitFromClientAccountByUnitId
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitsDAO()
        For Each id In ids
            Dim _entity = dao.GetById(id)
            _entity.ConstructionValueSold = Nothing
            _entity.LandValueSold = Nothing
            _entity.ClientAccountID = Nothing
            response.Messages.AddRange(uow.SaveChanges())
        Next

        Return response
    End Function
    Public Function DeleteUnitFromClientAccountByAccountId(ids As List(Of Integer)) As Response Implements IUnitService.DeleteUnitFromClientAccountByAccountID
        Dim response As New GetResponse(Of UnitBO)
        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitsDAO()
        For Each id In ids
            Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountID = id)
            Dim idlist As New List(Of Integer)
            For Each _entity In entities
                idlist.Add(_entity.Id)
            Next
            response = DeleteUnitFromClientAccountByUnitId(idlist)
        Next
        Return response
    End Function


    'UNIT GROUP TYPES
    Public Function GetUnitGroupTypes() As GetResponse(Of UnitGroupTypeBO) Implements IUnitService.GetUnitGroupTypes
        Dim response As New GetResponse(Of UnitGroupTypeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitGroupTypesDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Selectable = True)
        For Each _entity In entities
            Dim bo As New UnitGroupTypeBO()
            Dim err = UnitGroupTypeTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function InsertUpdateUnitGroupType(bo As UnitGroupTypeBO) As Response Implements IUnitService.InsertUpdateUnitGroupType
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitGroupTypesDAO()
        Dim _entity As UnitGroupTypes = Nothing

        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = UnitGroupTypeTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("unitgrouptype not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteUnitGroupType(ids As List(Of Integer)) As Response Implements IUnitService.DeleteUnitGroupType
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetUnitGroupTypesDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function

    'UNIT TYPES
    Public Function GetUnitTypesByGroupId(GroupId As Integer) As GetResponse(Of UnitTypeBO) Implements IUnitService.GetUnitTypesByGroupId
        Dim response As New GetResponse(Of UnitTypeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitTypesDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.GroupID = GroupId AndAlso m.Selectable = True)
        For Each _entity In entities
            Dim bo As New UnitTypeBO
            Dim err = UnitTypeTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function InsertUpdateUnitType(bo As UnitTypeBO) As Response Implements IUnitService.InsertUpdateUnitType
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitTypesDAO()
        Dim _entity As UnitTypes = Nothing

        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = UnitTypeTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("UnitType not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteUnitType(ids As List(Of Integer)) As Response Implements IUnitService.DeleteUnitType
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetUnitTypesDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function

    'UNIT ROOMS
    Public Function GetRooms(UnitId As Integer) As GetResponse(Of RoomBO) Implements IUnitService.GetRooms
        Dim response As New GetResponse(Of RoomBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitRoomsDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.UnitId = UnitId)
        For Each _entity In entities
            Dim bo As New RoomBO
            Dim err = UnitRoomTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetUniqueRoomTypesInProjectByProjectId(projectid As Integer) As GetResponse(Of RoomType) Implements IUnitService.GetUniqueRoomTypesInProjectByProjectId
        Dim response As New GetResponse(Of RoomType)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitRoomsDAO
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.ProjectId = projectid AndAlso m.Units.ClientAccountID Is Nothing).Where(Function(i) i.Type = RoomType.Terras Or i.Type = RoomType.Slaapkamer Or i.Type = RoomType.Dakterras Or i.Type = RoomType.Tuin Or i.Type = RoomType.Zolder).Select(Function(m) m.Type).Distinct()
        For Each _entity In entities
            response.AddValue(_entity.ToString)
        Next
        Return response
    End Function
    Public Function InsertUpdateRoom(bo As RoomBO) As Response Implements IUnitService.InsertUpdateRoom
        Dim response As New Response()
        If (bo.UnitId = 0) Then
            response.AddError("Er moet een eenheid geselecteerd zijn")
        ElseIf (bo.Type = 0) Then
            response.AddError("Er moet een kamertype geselecteerd zijn")
        ElseIf (bo.Number < 1) Then
            response.AddError("Er moet een aantal vermeld zijn")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitRoomsDAO()
        Dim _entity As UnitRooms = Nothing
        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = UnitRoomTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("UnitRoom not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteRooms(ids As List(Of Integer)) As Response Implements IUnitService.DeleteRooms
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetUnitRoomsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function

    'UNIT CONSTRUCTION VALUES
    Public Function GetConstructionValues(unitid As Integer) As GetResponse(Of UnitConstructionValueBO) Implements IUnitService.GetConstructionValues
        Dim response As New GetResponse(Of UnitConstructionValueBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitConstructionValuesDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.UnitId = unitid)
        For Each _entity In entities
            Dim bo As New UnitConstructionValueBO
            Dim err = UnitConstructionValueTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetConstructionValue(id As Integer) As GetResponse(Of UnitConstructionValueBO) Implements IUnitService.GetConstructionValue
        Dim response As New GetResponse(Of UnitConstructionValueBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetUnitConstructionValuesDAO()
        Dim _entity = dao.GetById(id)
        Dim unitcv As New UnitConstructionValueBO
        Dim err = UnitConstructionValueTranslator.TranslateEntityToBO(_entity, unitcv)
        If err = ErrorCode.Success Then
            response.Value = unitcv
        Else
            response.AddError(err.ToString())
        End If
        Return response

    End Function
    Public Function InsertUpdateConstructionValue(bo As UnitConstructionValueBO) As Response Implements IUnitService.InsertUpdateConstructionValue
        Dim response As New Response()
        If (bo.UnitId = 0) Then
            response.AddError("Er moet een unit geselecteerd zijn")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetUnitConstructionValuesDAO()
        Dim _entity As UnitConstructionValue = Nothing
        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = UnitConstructionValueTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("Unitconstructionvalue not found")
        End If
        response.AddError(uow.SaveChanges())
        response.InsertedId = _entity.Id
        Return response
    End Function
    Public Function DeleteConstructionvalues(ids As List(Of Integer)) As Response Implements IUnitService.DeleteConstructionValues
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetUnitConstructionValuesDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function


End Class
