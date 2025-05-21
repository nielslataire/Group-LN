Imports BO
Imports DAL

Public Class ActivityTranslator
    Public Shared Function TranslateEntityToBO(_entity As Activity, bo As ActivityBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.ID = _entity.ActivityID
        bo.Name = _entity.Omschrijving
        If _entity.ActivityGroup IsNot Nothing Then
            bo.Group.ID = _entity.ActivityGroup.GroupID
            bo.Group.Name = _entity.ActivityGroup.Name
            bo.Group.Lot = _entity.ActivityGroup.Lot
        End If

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As Activity, bo As ActivityBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Omschrijving = bo.Name
        If bo.Group IsNot Nothing Then
            'If _entity.ActivityGroup Is Nothing Then
            '    _entity.ActivityGroup = New ActivityGroup()
            '    _entity.ActivityGroup.Lot = bo.Group.Lot
            '    _entity.ActivityGroup.Name = bo.Group.Name
            'Else
            _entity.GroupID = bo.Group.ID
            'End If

            'If _entity.ActivityGroup.GroupID = 0 Then
            '    _entity.ActivityGroup = New ActivityGroup()
            '    _entity.ActivityGroup.Lot = bo.Group.Lot
            '    _entity.ActivityGroup.Name = bo.Group.Name
            'End If
        End If
        Return ErrorCode.Success
    End Function
End Class
