Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class IncommingInvoiceTranslator
    Friend Shared Function TranslateEntityToBO(_entity As IncommingInvoices, bo As IncommingInvoiceBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.ProjectId = _entity.Project.ProjectID
        If Not _entity.Contract Is Nothing Then bo.ContractID = _entity.ContractID
        If Not _entity.CompanyInfo Is Nothing Then bo.CompanyId = _entity.CompanyID
        bo.IncommingInvoiceDate = _entity.Date
        If Not _entity.ExternalID Is Nothing Then bo.InvoiceExternalId = _entity.ExternalID
        bo.InvoicePrice = _entity.Price
        For Each x In _entity.IncommingInvoiceDetail
            Dim bou As New IncommingInvoiceDetailBO
            Dim err = IncommingInvoiceDetailTranslator.TranslateEntityToBO(x, bou)
            bo.Details.Add(bou)
        Next
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As IncommingInvoices, bo As IncommingInvoiceBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        If Not bo.ProjectId = 0 Then _entity.ProjectID = bo.ProjectId
        If Not bo.ContractID = 0 Then _entity.ContractID = bo.ContractID
        If Not bo.CompanyId = 0 Then _entity.CompanyID = bo.CompanyId
        _entity.Date = bo.IncommingInvoiceDate
        _entity.ExternalID = bo.InvoiceExternalId
        _entity.Price = bo.InvoicePrice

        Dim err = HandleRows(_entity, bo.Details)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleRows(_entity As IncommingInvoices, rows As List(Of IncommingInvoiceDetailBO)) As ErrorCode
        If (rows.Count = 0) Then Return ErrorCode.Success
        For Each x In rows
            If (x.Id = 0) Then
                'insert
                Dim row As New IncommingInvoiceDetail
                Dim err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x)
                _entity.IncommingInvoiceDetail.Add(row)
            Else
                'update
                Dim row = _entity.IncommingInvoiceDetail.FirstOrDefault(Function(f) f.ID = x.Id)
                If (row IsNot Nothing) Then
                    Dim err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of IncommingInvoiceDetail)
        For Each x In _entity.IncommingInvoiceDetail
            If (Not rows.Any(Function(f) f.Id = x.ID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.IncommingInvoiceDetail.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
