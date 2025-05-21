Imports BO
Imports DAL

Public Class UnitTypeTranslator
    Public Shared Function TranslateEntityToBO(_entity As UnitTypes, bo As UnitTypeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.Name = _entity.Name
        bo.Shortcode = _entity.Shortcode
        bo.GroupId = _entity.GroupID
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As UnitTypes, bo As UnitTypeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.Shortcode = bo.Shortcode
        If (bo.GroupId <> 0) Then
            _entity.GroupID = bo.GroupId
        End If
        Return ErrorCode.Success
    End Function
End Class
