Imports DAL
Imports Facade
Imports BO

Public Class LogService
    Implements ILogService
    Public Function InsertUpdateLog(bo As LogBO) As Response Implements ILogService.InsertUpdateLog
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Text)) Then
            response.AddError("test is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetLogDAO()
        Dim _entity As LogList = Nothing

        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = LogTranslator.TranslateBOToEntity(_entity, bo, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If

        Else
            response.AddError("log not found")
        End If
        response.AddError(uow.SaveChanges())
        response.InsertedId = _entity.id
        Return response
    End Function

End Class
