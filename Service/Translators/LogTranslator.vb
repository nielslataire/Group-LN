Imports BO
Imports DAL

Public Class LogTranslator
    Public Shared Function TranslateEntityToBO(_entity As LogList, bo As LogBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.id
        bo.Text = _entity.Text
        bo.Value = _entity.Value
        bo.Datum = _entity.DateTime
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As LogList, bo As LogBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Text = bo.Text
        _entity.Value = bo.Value
        _entity.DateTime = DateTime.Now()

        Return ErrorCode.Success
    End Function

End Class
