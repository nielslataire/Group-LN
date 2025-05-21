Imports BO
Imports DAL
Imports Facade
Imports System.Threading

Public Class ActivityService
    Implements IActivityService

    Public Function GetActivitiesForSelect() As GetResponse(Of IdNameBO) Implements IActivityService.GetActivitiesForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityDAO()
        Dim entities = dao.GetNoTracking()
        entities = entities.OrderBy(Function(m) m.Omschrijving)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        response.Values.OrderBy(Function(m) m.Display)
        Return response
    End Function

    Public Function GetActivities() As GetResponse(Of ActivityBO) Implements IActivityService.GetActivities
        Dim response As New GetResponse(Of ActivityBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityDAO()
        Dim entities = dao.GetNoTracking().OrderBy(Function(m) m.Omschrijving)
        For Each _entity In entities
            Dim bo As New ActivityBO()
            bo.ID = _entity.ActivityID
            bo.Name = _entity.Omschrijving
            If (_entity.ActivityGroup IsNot Nothing) Then
                bo.Group.Name = _entity.ActivityGroup.Name
                bo.Group.ID = _entity.ActivityGroup.GroupID
                bo.Group.Lot = _entity.ActivityGroup.Lot
            End If
            'bo.GroupName = _entity.ActivityGroup.Name

            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetActivitiesbyId(IdList As List(Of Integer)) As GetResponse(Of ActivityBO) Implements IActivityService.GetActivitiesById
        Dim response As New GetResponse(Of ActivityBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityDAO()

        Dim entities = dao.GetNoTracking.Where(Function(m) IdList.Contains(m.ActivityID))
        For Each _entity In entities
            Dim bo As New ActivityBO()
            bo.ID = _entity.ActivityID
            bo.Name = _entity.Omschrijving
            If (_entity.ActivityGroup IsNot Nothing) Then
                bo.Group.Name = _entity.ActivityGroup.Name
                bo.Group.ID = _entity.ActivityGroup.GroupID
                bo.Group.Lot = _entity.ActivityGroup.Lot
            End If
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetActivitybyId(id As Integer) As GetResponse(Of ActivityBO) Implements IActivityService.GetActivitybyId
        Dim response As New GetResponse(Of ActivityBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityDAO()

        Dim _entity = dao.GetById(id)
        'For Each _entity In entities
        Dim bo As New ActivityBO()
        bo.ID = _entity.ActivityID
        bo.Name = _entity.Omschrijving
        If (_entity.ActivityGroup IsNot Nothing) Then
            bo.Group.Name = _entity.ActivityGroup.Name
            bo.Group.ID = _entity.ActivityGroup.GroupID
            bo.Group.Lot = _entity.ActivityGroup.Lot
        End If
        response.AddValue(bo)
        'Next
        Return response
    End Function
    Public Function GetActivityGroups() As GetResponse(Of ActivityGroupBO) Implements IActivityService.GetActivityGroups
        Dim response As New GetResponse(Of ActivityGroupBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityGroupDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New ActivityGroupBO()
            Dim err = ActivityGroupTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetActivityGroupsForSelect() As GetResponse(Of IdNameBO) Implements IActivityService.GetActivityGroupsForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityGroupDAO()
        Dim entities = dao.GetNoTracking()
        entities = entities.OrderBy(Function(m) m.Lot)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetActivityGroupbyId(id As Integer) As GetResponse(Of ActivityGroupBO) Implements IActivityService.GetActivityGroupbyId
        Dim response As New GetResponse(Of ActivityGroupBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetActivityGroupDAO()

        Dim _entity = dao.GetById(id)
        'For Each _entity In entities
        Dim bo As New ActivityGroupBO
        bo.ID = _entity.GroupID
        bo.Name = _entity.Name
        bo.Lot = _entity.Lot
        For Each Activity In _entity.Activity
            Dim act As New ActivityBO
            act.Name = Activity.Omschrijving
            act.ID = Activity.ActivityID
            bo.Activities.Add(act)
        Next
        response.AddValue(bo)
        'Next
        Return response
    End Function


    Function InsertUpdate(bo As ActivityBO) As Response Implements IActivityService.InsertUpdate
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetActivityDAO()
        Dim _entity As Activity = Nothing

        If (bo.ID = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.ID)

        End If

        If (_entity IsNot Nothing) Then

            Dim err = ActivityTranslator.TranslateBOToEntity(_entity, bo)

            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("activity not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function
    Function InsertUpdateGroup(bo As ActivityGroupBO) As Response Implements IActivityService.InsertUpdateGroup
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetActivityGroupDAO()
        Dim _entity As ActivityGroup = Nothing

        If (bo.ID = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.ID)

        End If

        If (_entity IsNot Nothing) Then

            Dim err = ActivityGroupTranslator.TranslateBOToEntity(_entity, bo)

            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("activity not found")
        End If
        response.AddError(uow.SaveChanges())

        Return response
    End Function

    Function Delete(ids As List(Of Integer)) As Response Implements IActivityService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetActivityDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function

    Function Delete(bos As List(Of ActivityBO)) As Response Implements IActivityService.Delete
        Return Delete(bos.Select(Function(s) s.ID).ToList())
    End Function
    Function DeleteGroup(ids As List(Of Integer)) As Response Implements IActivityService.DeleteGroup
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetActivityGroupDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function

    Function DeleteGroup(bos As List(Of ActivityBO)) As Response Implements IActivityService.DeleteGroup
        Return Delete(bos.Select(Function(s) s.ID).ToList())
    End Function
End Class
