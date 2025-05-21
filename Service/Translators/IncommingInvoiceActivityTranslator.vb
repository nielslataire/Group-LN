Imports BO
Imports DAL
Imports System.Text.RegularExpressions
Public Class IncommingInvoiceActivityTranslator
    Public Shared Function TranslateEntityToBO(_entity As IncommingInvoiceDetail, bo As IncommingInvoiceActivityBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.InvoiceId = _entity.IncommingInvoiceID
        bo.InvoiceDetailId = _entity.ID
        bo.Price = _entity.Price
        bo.ExternalInvoiceId = _entity.IncommingInvoices.ExternalID
        bo.Invoicedate = _entity.IncommingInvoices.Date
        bo.Description = _entity.Description
        bo.IncommingInvoiceType = _entity.Type
        If _entity.IncommingInvoices.ContractID <> 0 Then bo.ContractId = _entity.IncommingInvoices.ContractID
        If Not _entity.ContractActivity Is Nothing Then
            Dim act As New ActivityBO
            Dim err2 = ActivityTranslator.TranslateEntityToBO(_entity.ContractActivity.Activity, act)
            If err2 <> ErrorCode.Success Then Return err2
            bo.Activity = act
            Dim comp As New IdNameBO
            comp.ID = _entity.ContractActivity.Contract.CompanyInfo.CompanyID
            comp.Display = _entity.ContractActivity.Contract.CompanyInfo.BedrijfsNaam
            bo.Company = comp
        End If
        If Not _entity.Activity Is Nothing Then
            Dim act As New ActivityBO
            Dim err2 = ActivityTranslator.TranslateEntityToBO(_entity.Activity, act)
            If err2 <> ErrorCode.Success Then Return err2
            bo.Activity = act
            Dim comp As New IdNameBO
            comp.ID = _entity.IncommingInvoices.CompanyInfo.CompanyID
            comp.Display = _entity.IncommingInvoices.CompanyInfo.BedrijfsNaam
            bo.Company = comp
        End If
        Return ErrorCode.Success
    End Function


End Class
