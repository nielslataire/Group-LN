Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class InsuranceTranslator

    Friend Shared Function TranslateEntityToBO(_entity As Insurances, bo As InsuranceBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        'Algemene gegevens
        bo.Id = _entity.Id
        bo.ExtensionPeriod = _entity.ExtensionPeriod
        bo.GuaranteePeriod = _entity.GuaranteePeriod
        bo.ContractActivityID = _entity.ContractActivityID
        bo.InsuranceBrokerName = _entity.ContractActivity.Contract.CompanyInfo.BedrijfsNaam
        If Not _entity.InsuranceCompanies Is Nothing Then
            Dim err = InsuranceCompanyTranslator.TranslateEntityToBO(_entity.InsuranceCompanies, bo.InsuranceCompany)
            If (err <> ErrorCode.Success) Then Return err
        End If
        bo.Period = _entity.Period
        bo.ProjectID = _entity.ContractActivity.Contract.ProjectID
        bo.Startdate = _entity.Startdate
        bo.Type = _entity.Type
        If Not _entity.Enddate Is Nothing Then bo.Enddate = _entity.Enddate

        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As Insurances, bo As InsuranceBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        If Not bo.InsuranceCompany.Id = 0 Then _entity.InsuranceCompanyID = bo.InsuranceCompany.Id
        If Not bo.ContractActivityID = 0 Then _entity.ContractActivityID = bo.ContractActivityID
        _entity.ExtensionPeriod = bo.ExtensionPeriod
        _entity.GuaranteePeriod = bo.GuaranteePeriod
        _entity.Period = bo.Period
        _entity.Startdate = bo.Startdate
        If Not bo.Type = 0 Then _entity.Type = bo.Type
        _entity.Enddate = bo.Enddate


        Return ErrorCode.Success
    End Function

End Class
