Imports BO
Imports DAL

Public Class ActivityGroupTranslator
    'NAZIEN AUB
    Public Shared Function TranslateEntityToBO(_entity As ActivityGroup, bo As ActivityGroupBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.ID = _entity.GroupID
        bo.Name = _entity.Name
        bo.Lot = _entity.Lot
        For Each x In _entity.Activity
            Dim activity As New ActivityBO
            activity.ID = x.ActivityID
            activity.Name = x.Omschrijving

            bo.Activities.Add(activity)
        Next
        Return ErrorCode.Success
    End Function
    'NAZIEN AUB
    Friend Shared Function TranslateBOToEntity(_entity As ActivityGroup, bo As ActivityGroupBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Lot = bo.Lot
        _entity.Name = bo.Name
        Dim err = HandleActivities(_entity, bo.Activities)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleActivities(_entity As ActivityGroup, activities As List(Of ActivityBO)) As ErrorCode
        If (activities.Count = 0) Then Return ErrorCode.Success
        For Each x In activities
            If (x.ID = 0) Then
                'insert
                Dim activity As New Activity
                Dim err = ActivityTranslator.TranslateBOToEntity(activity, x)
                If (err <> ErrorCode.Success) Then Return err
                _entity.Activity.Add(activity)
            Else
                'update
                Dim activity = _entity.Activity.FirstOrDefault(Function(f) f.ActivityID = x.ID)
                If (activity IsNot Nothing) Then
                    Dim err = ActivityTranslator.TranslateBOToEntity(activity, x)
                    If (err <> ErrorCode.Success) Then Return err
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
