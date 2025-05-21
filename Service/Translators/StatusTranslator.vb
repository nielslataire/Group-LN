Imports BO
Imports DAL

Public Class StatusTranslator
    Public Shared Function TranslateEntityToBO(_entity As ProjectStatus, bo As ProjectStatusBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.StatusID
        bo.Name = _entity.StatusName
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ProjectStatus, bo As ProjectStatusBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.StatusName = bo.Name
        Return ErrorCode.Success
    End Function
End Class
