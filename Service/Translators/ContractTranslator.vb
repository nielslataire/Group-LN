Imports BO
Imports DAL
Imports System.Text.RegularExpressions
Public Class ContractTranslator
    Public Shared Function TranslateEntityToBO(_entity As Contract, bo As ContractBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.ProjectId = _entity.ProjectID
        If Not _entity.CompanyInfo Is Nothing Then
            bo.Company.ID = _entity.CompanyInfo.CompanyID
            bo.Company.Display = _entity.CompanyInfo.BedrijfsNaam
        End If
        bo.CashDiscount = _entity.CashDiscount
        bo.CashDiscountPaymentTerm = _entity.CashDiscountPaymentTerm
        bo.CashDiscountPercentage = _entity.CashDiscountPercentage
        bo.PaymentTerm = _entity.PaymentTerm
        bo.VatPercentage = _entity.VatPercentage
        bo.GuaranteeType = _entity.GuaranteeType
        bo.GuaranteePercentage = _entity.GuaranteePercentage
        bo.ContractSigned = _entity.ContractSigned
        If _entity.ContractActivity IsNot Nothing Then
            For Each item In _entity.ContractActivity
                Dim activity As New ContractActivityBO
                Dim err = ContractActivityTranslator.TranslateEntityToBO(item, activity)
                If err <> ErrorCode.Success Then Return err
                bo.Activities.Add(activity)
            Next
        End If
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As Contract, bo As ContractBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ProjectID = bo.ProjectId
        _entity.CashDiscount = bo.CashDiscount
        _entity.CashDiscountPaymentTerm = bo.CashDiscountPaymentTerm
        _entity.CashDiscountPercentage = bo.CashDiscountPercentage
        _entity.CompanyID = bo.Company.ID
        _entity.PaymentTerm = bo.PaymentTerm
        _entity.VatPercentage = bo.VatPercentage
        _entity.ContractSigned = bo.ContractSigned
        _entity.GuaranteePercentage = bo.GuaranteePercentage
        _entity.GuaranteeType = bo.GuaranteeType
        Dim Err = HandleActivities(_entity, bo.Activities, uow)
        If (Err <> ErrorCode.Success) Then Return Err
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleActivities(_entity As Contract, activities As List(Of ContractActivityBO), uow As UnitOfWork) As ErrorCode
        If (activities Is Nothing) Then Return ErrorCode.Success
        If (activities.Count = 0) Then Return ErrorCode.Success
        For Each x In activities
            If (x.ContractActivityId = 0) Then
                'insert
                Dim activity As New ContractActivity
                Dim err = ContractActivityTranslator.TranslateBOToEntity(activity, x, uow)
                If err <> ErrorCode.Success Then Return err
                _entity.ContractActivity.Add(activity)
            Else
                'update
                Dim activity = _entity.ContractActivity.FirstOrDefault(Function(f) f.Id = x.ContractActivityId)
                If (activity IsNot Nothing) Then
                    Dim err = ContractActivityTranslator.TranslateBOToEntity(activity, x, uow)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of ContractActivity)
        For Each x In _entity.ContractActivity
            If (Not activities.Any(Function(f) f.ContractActivityId = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.ContractActivity.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
