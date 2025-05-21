Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class InvoiceDetailTranslator
    Friend Shared Function TranslateEntityToBO(_entity As InvoicesDetails, bo As InvoiceRowBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        bo.Id = _entity.Id
        If Not _entity.Invoices Is Nothing Then bo.InvoiceId = _entity.InvoiceId
        If Not _entity.InvoicingPaymentStages Is Nothing Then bo.StageId = _entity.PaymentStageId
        If Not _entity.Units Is Nothing Then bo.UnitId = _entity.UnitId
        If Not _entity.ChangeOrderDetail Is Nothing Then bo.ChangeOrderDetailId = _entity.ChangeOrderDetailId
        If Not _entity.ConstructionValueId Is Nothing Then bo.ConstructionValueId = _entity.ConstructionValueId
        bo.Text = _entity.Text
        If Not _entity.VatPercentage Is Nothing Then bo.VatPercentage = _entity.VatPercentage
        If Not _entity.Price Is Nothing Then bo.Price = _entity.Price
        If Not _entity.GroupName Is Nothing Then bo.GroupName = _entity.GroupName
        If Not _entity.UtilityCost Is Nothing Then bo.UtilityCost = _entity.UtilityCost
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As InvoicesDetails, bo As InvoiceRowBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        If bo.StageId <> 0 Then _entity.PaymentStageId = bo.StageId
        If bo.UnitId <> 0 Then _entity.UnitId = bo.UnitId
        If bo.InvoiceId <> 0 Then _entity.InvoiceId = bo.InvoiceId
        If bo.ChangeOrderDetailId <> 0 Then _entity.ChangeOrderDetailId = bo.ChangeOrderDetailId
        If bo.ConstructionValueId <> 0 Then _entity.ConstructionValueId = bo.ConstructionValueId
        _entity.Text = bo.Text
        _entity.VatPercentage = bo.VatPercentage
        _entity.Price = bo.Price
        _entity.GroupName = bo.GroupName
        _entity.UtilityCost = bo.UtilityCost

        Return ErrorCode.Success
    End Function
End Class
