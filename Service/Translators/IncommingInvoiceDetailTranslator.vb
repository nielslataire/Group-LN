Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class IncommingInvoiceDetailTranslator
    Friend Shared Function TranslateEntityToBO(_entity As IncommingInvoiceDetail, bo As IncommingInvoiceDetailBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.IncommingInvoiceType = _entity.Type
        If Not _entity.IncommingInvoices Is Nothing Then bo.IncommingInvoiceID = _entity.IncommingInvoices.ID
        If Not _entity.ContractActivity Is Nothing Then
            bo.ContractActivityID = _entity.ContractActID
            bo.ContractActivityText = _entity.ContractActivity.Activity.Omschrijving
        End If
        If Not _entity.Activity Is Nothing Then
            bo.ActivityID = _entity.Activity.ActivityID
            bo.ContractActivityText = _entity.Activity.Omschrijving
        End If
        If Not _entity.ChangeOrder Is Nothing Then bo.ChangeOrderId = _entity.ChangeOrderID
        If Not _entity.Description Is Nothing Then bo.Description = _entity.Description
        If Not _entity.Price Is Nothing Then bo.Price = _entity.Price

        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As IncommingInvoiceDetail, bo As IncommingInvoiceDetailBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        If bo.ContractActivityID <> 0 Then _entity.ContractActID = bo.ContractActivityID
        If bo.ChangeOrderId <> 0 Then _entity.ChangeOrderID = bo.ChangeOrderId
        If bo.IncommingInvoiceID <> 0 Then _entity.IncommingInvoiceID = bo.IncommingInvoiceID
        If bo.ActivityID <> 0 Then _entity.ActID = bo.ActivityID
        _entity.Type = bo.IncommingInvoiceType
        _entity.Price = bo.Price
        _entity.Description = bo.Description


        Return ErrorCode.Success
    End Function
End Class
