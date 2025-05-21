Imports BO
Imports DAL
Imports System.Text.RegularExpressions
Public Class ContractActivityTranslator
    Public Shared Function TranslateEntityToBO(_entity As ContractActivity, bo As ContractActivityBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.ContractActivityId = _entity.Id
        bo.ContractId = _entity.ContractID
        Dim activity As New ActivityBO
        Dim err = ActivityTranslator.TranslateEntityToBO(_entity.Activity, activity)
        If err <> ErrorCode.Success Then Return err
        bo.Activity = activity
        bo.Price = _entity.Price
        If _entity.Insurances IsNot Nothing Then
            Dim insurance As New InsuranceBO
            Dim err2 = InsuranceTranslator.TranslateEntityToBO(_entity.Insurances, insurance)
            If err2 <> ErrorCode.Success Then Return err2
            bo.InsuranceData = insurance
        End If
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ContractActivity, bo As ContractActivityBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ContractID = bo.ContractId
        _entity.ActivityID = bo.Activity.ID
        _entity.Price = bo.Price
        If bo.Activity.ID = 142 AndAlso bo.InsuranceData IsNot Nothing Then
            Dim insurance As New Insurances
            Dim err = InsuranceTranslator.TranslateBOToEntity(insurance, bo.InsuranceData, uow)
            If err <> ErrorCode.Success Then Return err
            _entity.Insurances = insurance
        End If
        Return ErrorCode.Success
    End Function

End Class
