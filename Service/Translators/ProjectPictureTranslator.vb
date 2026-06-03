Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ProjectPictureTranslator
    Friend Shared Function TranslateEntityToBO(_entity As ProjectPictures, bo As ProjectPictureBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id              = _entity.Id
        bo.Name            = _entity.Name
        bo.Caption         = _entity.Caption
        bo.ProjectId       = If(_entity.ProjectId.HasValue, _entity.ProjectId.Value, 0)
        bo.Type            = If(_entity.Type.HasValue, CType(_entity.Type.Value, PictureType), PictureType.Nevenfoto)
        bo.DateTimeUploaded = If(_entity.Datetimeuploaded.HasValue, _entity.Datetimeuploaded.Value, Date.MinValue)
        bo.FacebookIdCopro = _entity.FacebookIdCopro
        bo.SectionId       = _entity.SectionId
        bo.IsPublic        = _entity.IsPublic
        bo.SortOrder       = _entity.SortOrder
        bo.MediaType       = _entity.MediaType
        bo.FileSizeBytes   = _entity.FileSizeBytes
        bo.WidthPx         = _entity.WidthPx
        bo.HeightPx        = _entity.HeightPx
        bo.DurationSeconds = _entity.DurationSeconds
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ProjectPictures, bo As ProjectPictureBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name            = bo.Name
        _entity.Caption         = bo.Caption
        _entity.ProjectId       = bo.ProjectId
        _entity.Type            = CInt(bo.Type)
        _entity.Datetimeuploaded = bo.DateTimeUploaded
        _entity.FacebookIdCopro = bo.FacebookIdCopro
        _entity.SectionId       = bo.SectionId
        _entity.IsPublic        = bo.IsPublic
        _entity.SortOrder       = bo.SortOrder
        _entity.MediaType       = bo.MediaType
        _entity.FileSizeBytes   = bo.FileSizeBytes
        _entity.WidthPx         = bo.WidthPx
        _entity.HeightPx        = bo.HeightPx
        _entity.DurationSeconds = bo.DurationSeconds
        Return ErrorCode.Success
    End Function

End Class
