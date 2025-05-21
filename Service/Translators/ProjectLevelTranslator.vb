Imports BO
Imports DAL

Public Class ProjectLevelTranslator
    Public Shared Function TranslateEntityToBO(_entity As ProjectLevels, bo As ProjectLevelBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Level
        bo.ProjectId = _entity.ProjectId
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ProjectLevels, bo As ProjectLevelBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Level = bo.Name
        _entity.ProjectId = bo.ProjectId
        Return ErrorCode.Success
    End Function
End Class
