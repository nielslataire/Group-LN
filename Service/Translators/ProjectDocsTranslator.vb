Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ProjectDocsTranslator
    Friend Shared Function TranslateEntityToBO(_entity As ProjectDocs, bo As ProjectDocBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Docid = _entity.Id
        bo.Name = _entity.Name
        bo.ProjectId = _entity.ProjectId
        bo.ClientAccountId = _entity.ClientAccountId
        bo.Filename = _entity.Filename
        bo.SortOrder = _entity.SortOrder
        bo.Type = _entity.Type
        bo.DocDate = _entity.Date

        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ProjectDocs, bo As ProjectDocBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.Filename = bo.Filename
        _entity.ProjectId = bo.ProjectId
        _entity.ClientAccountId = bo.ClientAccountId
        _entity.SortOrder = bo.SortOrder
        _entity.Type = bo.Type
        _entity.Date = bo.DocDate
        Return ErrorCode.Success
    End Function

End Class
