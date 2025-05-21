Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ClientGiftTranslator

    Friend Shared Function TranslateEntityToBO(_entity As ClientGift, bo As ClientGiftBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.AccountId = _entity.ClientAccountId
        bo.Description = _entity.Description
        If _entity.Activity IsNot Nothing Then
            For Each item In _entity.Activity
                Dim Activity As New ActivityBO
                Dim err = ActivityTranslator.TranslateEntityToBO(item, Activity)
                If err <> ErrorCode.Success Then Return err
                bo.Activities.Add(Activity)
            Next
        End If
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ClientGift, bo As ClientGiftBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ClientAccountId = bo.AccountId
        _entity.Description = bo.Description

        Dim err = HandleActivities(_entity, bo.Activities, uow)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleActivities(_entity As ClientGift, activities As List(Of ActivityBO), uow As UnitOfWork) As ErrorCode
        If (activities.Count = 0) Then Return ErrorCode.Success
        For Each x In activities
            If (x.ID = 0) Then
                'should never happen
            Else
                'add the activity to the clientgift
                If (Not _entity.Activity.Any(Function(m) m.ActivityID = x.ID)) Then
                    Dim act = uow.GetActivityDAO().GetById(x.ID)
                    _entity.Activity.Add(act)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of Activity)
        For Each x In _entity.Activity
            If (Not activities.Any(Function(f) f.ID = x.ActivityID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.Activity.Remove(x)
        Next
        Return ErrorCode.Success
    End Function


End Class
