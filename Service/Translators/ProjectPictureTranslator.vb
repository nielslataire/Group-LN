Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ProjectPictureTranslator
    Friend Shared Function TranslateEntityToBO(_entity As ProjectPictures, bo As ProjectPictureBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        bo.Caption = _entity.Caption
        bo.ProjectId = _entity.ProjectId
        bo.Type = _entity.Type
        bo.DateTimeUploaded = _entity.Datetimeuploaded
        bo.FacebookIdCopro = _entity.FacebookIdCopro
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ProjectPictures, bo As ProjectPictureBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.Caption = bo.Caption
        _entity.ProjectId = bo.ProjectId
        _entity.Type = bo.Type
        _entity.Datetimeuploaded = bo.DateTimeUploaded
        _entity.FacebookIdCopro = bo.FacebookIdCopro
        Return ErrorCode.Success
    End Function

End Class
