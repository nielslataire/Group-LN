Imports BO
Imports DAL

Public Class ProjectSalesSettingsTranslator
    Public Shared Function TranslateEntityToBO(_entity As ProjectSalesSettings, bo As ProjectSalesSettingsBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.SettingsId = _entity.Id
        bo.ProjectId = _entity.Projectid
        bo.MixedVatRegistration = _entity.MixedVATRegistration
        If Not _entity.SaleVisible Is Nothing Then bo.SaleVisible = _entity.SaleVisible
        If Not _entity.ConnectionFees Is Nothing Then bo.ConnectionFees = _entity.ConnectionFees
        If Not _entity.VATPercentage Is Nothing Then bo.VatPercentage = _entity.VATPercentage
        If Not _entity.RegistrationPercentage Is Nothing Then bo.RegistrationPercentage = _entity.RegistrationPercentage
        If Not _entity.BaseCertificateCost Is Nothing Then bo.BaseCertificateCost = _entity.BaseCertificateCost
        If Not _entity.FixedCertificateCost Is Nothing Then bo.FixedCertificateCost = _entity.FixedCertificateCost
        If Not _entity.MortageRegistrationCost Is Nothing Then bo.MortageRegistrationCost = _entity.MortageRegistrationCost
        If Not _entity.BankAccountNumber Is Nothing Then bo.BankAccountNumber = _entity.BankAccountNumber
        If Not _entity.RegistrationType Is Nothing Then bo.RegistrationType = _entity.RegistrationType
        If Not _entity.SurveyorCost Is Nothing Then bo.SurveyorCost = _entity.SurveyorCost
        If Not _entity.ParcelCost Is Nothing Then bo.ParcelCost = _entity.ParcelCost

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ProjectSalesSettings, bo As ProjectSalesSettingsBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Projectid = bo.ProjectId
        _entity.MixedVATRegistration = bo.MixedVatRegistration
        _entity.VATPercentage = bo.VatPercentage
        _entity.ConnectionFees = bo.ConnectionFees
        _entity.RegistrationPercentage = bo.RegistrationPercentage
        _entity.BaseCertificateCost = bo.BaseCertificateCost
        _entity.FixedCertificateCost = bo.FixedCertificateCost
        _entity.MortageRegistrationCost = bo.MortageRegistrationCost
        _entity.BankAccountNumber = bo.BankAccountNumber
        _entity.SaleVisible = bo.SaleVisible
        _entity.RegistrationType = bo.RegistrationType
        _entity.SurveyorCost = bo.SurveyorCost
        _entity.ParcelCost = bo.ParcelCost

        Return ErrorCode.Success
    End Function
End Class
