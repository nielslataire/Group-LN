Imports BO
Imports DAL

Public Class ChangeOrderTranslator
    Public Shared Function TranslateEntityToBO(_entity As ChangeOrder, bo As ChangeOrderBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.ClientAccountID = _entity.ClientAccount.Id
        bo.Description = _entity.Description
        bo.ChangeOrderDate = _entity.Date
        bo.ExpirationDate = _entity.ExpirationDate
        bo.Comment = _entity.Comment
        bo.DateSendToClient = _entity.DateSendToClient
        bo.DateAgreement = _entity.DateAgreement
        bo.Invoiceable = _entity.Invoiceable
        bo.ContractActivityID = _entity.ContractActivity.Id
        bo.ProjectId = _entity.ContractActivity.Contract.ProjectID
        bo.ChangeOrderConditions = _entity.ChangeOrderConditions
        If _entity.ClientAccount.Name IsNot Nothing Then

            bo.ClientName = _entity.ClientAccount.Name
        Else
            bo.ClientName = _entity.ClientAccount.CompanyName
        End If
        If _entity.ChangeOrderDetail IsNot Nothing Then
            For Each item In _entity.ChangeOrderDetail

                Dim detail As New ChangeOrderDetailBO
                Dim err = TranslateDetailEntityToBO(item, detail)
                If err <> ErrorCode.Success Then Return err
                bo.Details.Add(detail)

            Next
        End If


        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As ChangeOrder, bo As ChangeOrderBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ClientAccountID = bo.ClientAccountID
        _entity.Description = bo.Description
        _entity.Date = bo.ChangeOrderDate
        _entity.ExpirationDate = bo.ExpirationDate
        _entity.Comment = bo.Comment
        _entity.DateSendToClient = bo.DateSendToClient
        _entity.DateAgreement = bo.DateAgreement
        _entity.Invoiceable = bo.Invoiceable
        _entity.ContractActivityID = bo.ContractActivityID
        _entity.ChangeOrderConditions = bo.ChangeOrderConditions

        Dim err = HandleDetails(_entity, bo.Details, uow)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function
    Public Shared Function TranslateDetailEntityToBO(_entity As ChangeOrderDetail, bo As ChangeOrderDetailBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.ChangeOrderID = _entity.ChangeOrder.ID
        bo.Description = _entity.Description
        bo.MeasurementType = _entity.MeasurementType
        bo.MeasurementUnit = _entity.MeasurementUnit
        bo.Number = _entity.Number
        bo.Price = _entity.Price
        bo.Commision = _entity.Commission
        If Not _entity.Invoicable Is Nothing Then bo.Invoicable = _entity.Invoicable
        If Not _entity.Invoiced Is Nothing Then bo.Invoiced = _entity.Invoiced
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateDetailBOToEntity(_entity As ChangeOrderDetail, bo As ChangeOrderDetailBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ChangeOrderID = bo.ChangeOrderID
        _entity.Description = bo.Description
        _entity.MeasurementType = bo.MeasurementType
        _entity.MeasurementUnit = bo.MeasurementUnit
        _entity.Number = bo.Number
        _entity.Price = bo.Price
        _entity.Commission = bo.Commision
        _entity.Invoicable = bo.Invoicable
        _entity.Invoiced = bo.Invoiced
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleDetails(_entity As ChangeOrder, details As List(Of ChangeOrderDetailBO), uow As UnitOfWork) As ErrorCode
        If (details Is Nothing) Then Return ErrorCode.Success
        If (details.Count = 0) Then Return ErrorCode.Success
        For Each x In details
            If (x.Id = 0) Then
                'insert
                Dim detail As New ChangeOrderDetail
                Dim err = TranslateDetailBOToEntity(detail, x, uow)
                If err <> ErrorCode.Success Then Return err
                _entity.ChangeOrderDetail.Add(detail)
            Else
                'update
                Dim detail = _entity.ChangeOrderDetail.FirstOrDefault(Function(f) f.ID = x.Id)
                If (detail IsNot Nothing) Then
                    Dim err = TranslateDetailBOToEntity(detail, x, uow)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of ChangeOrderDetail)
        For Each x In _entity.ChangeOrderDetail
            If (Not details.Any(Function(f) f.Id = x.ID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.ChangeOrderDetail.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
