Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ClientOwnerTypeTranslator

    Friend Shared Function TranslateEntityToBO(_entity As ClientOwnerType, bo As ClientOwnerTypeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.Name = _entity.Name
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ClientOwnerType, bo As ClientOwnerTypeBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        Return ErrorCode.Success
    End Function


End Class
