Imports BO
Imports DAL

Public Class UnitConstructionValueTranslator
    Public Shared Function TranslateEntityToBO(_entity As UnitConstructionValue, bo As UnitConstructionValueBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Description = _entity.Description
        If Not _entity.ValueSold Is Nothing Then bo.ValueSold = _entity.ValueSold
        If Not _entity.Value Is Nothing Then bo.Value = _entity.Value
        If Not _entity.InvoicingPaymentGroup Is Nothing Then bo.PaymentGroupId = _entity.InvoicingPaymentGroup.Id
        If Not _entity.Units Is Nothing Then bo.UnitId = _entity.Units.Id
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As UnitConstructionValue, bo As UnitConstructionValueBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Description = bo.Description
        _entity.Value = bo.Value
        _entity.ValueSold = bo.ValueSold
        If bo.PaymentGroupId = 0 Then
            _entity.PaymentGroupId = Nothing
        End If
        _entity.PaymentGroupId = bo.PaymentGroupId
        If (bo.UnitId <> 0) Then
            _entity.UnitId = bo.UnitId
        End If
        Return ErrorCode.Success
    End Function
End Class
