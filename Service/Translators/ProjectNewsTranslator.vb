Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ProjectNewsTranslator
    Friend Shared Function TranslateEntityToBO(_entity As ProjectNews, bo As ProjectNewsBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.NewsDate = _entity.Date
        bo.ProjectId = _entity.ProjectId
        bo.TextNL = _entity.TextNL
        bo.TitleNL = _entity.TitleNL
        bo.Author = _entity.Author
        If Not _entity.ProjectPictures Is Nothing Then
            Dim picture As New ProjectPictureBO
            bo.Picture = picture
            ProjectPictureTranslator.TranslateEntityToBO(_entity.ProjectPictures, bo.Picture)
        End If
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ProjectNews, bo As ProjectNewsBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.TitleNL = bo.TitleNL
        _entity.TextNL = bo.TextNL
        _entity.ProjectId = bo.ProjectId
        _entity.Date = bo.NewsDate
        _entity.Author = bo.Author
        If Not bo.Picture Is Nothing Then
            ProjectPictureTranslator.TranslateBOToEntity(_entity.ProjectPictures, bo.Picture, uow)
        End If
        Return ErrorCode.Success
    End Function

End Class
